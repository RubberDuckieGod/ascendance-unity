#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class RoundTransitionTestTools0812
    {
        [MenuItem("Ascendance/Validation/Test 0.8.12 - Round Transition + Reaction Reserve (Play Mode)")]
        public static void TestRoundTransitionAndReactionReserve()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.12 TEST] Enter Play Mode before running the Round transition test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.ParticipatingHeroes.Any(h => h.Moving) || gm.TurnHero != null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.12 TEST] Run this between Hero turns with no movement/reaction/world resolution in progress.");
                return;
            }

            var party = gm.ParticipatingHeroes.Where(h => !h.IsDefeated).ToList();
            if (party.Count < 2 || !gm.Heroes.TryGetValue("Garruk", out var garruk) || !garruk.Participating || garruk.IsDefeated)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.12 TEST] Use a party with Garruk plus at least one other non-Defeated Hero.");
                return;
            }
            if (party.Any(h => h.TurnCompleted || h.Actions != 0))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.12 TEST] Run this at the beginning of a Hero Phase before any Hero has started a turn.");
                return;
            }

            gm.StartCoroutine(Run(gm, party, garruk));
        }

        private static IEnumerator Run(AscendanceGameManager gm, System.Collections.Generic.List<HeroState> party, HeroState garruk)
        {
            bool pass = true;
            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.12 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.12 TEST] FAIL — " + label); }
            }

            int startingRound = gm.Round;
            var second = party.First(h => h.Id != garruk.Id);

            // Deterministic World Phase: only the Captain is alive and only Garruk shares its Location.
            foreach (var enemy in gm.Enemies.Values)
            {
                enemy.ActivatedThisRound = false;
                if (enemy.Id == "Captain")
                {
                    enemy.Health = enemy.MaxHealth;
                    enemy.LocationId = "L000";
                }
                else enemy.Health = 0;
            }
            garruk.LocationId = "L000";
            garruk.Health = garruk.MaxHealth;
            garruk.Downed = false;
            garruk.Dead = false;
            foreach (var hero in party.Where(h => h.Id != garruk.Id)) hero.LocationId = "L001";

            // Remove unrelated Reaction cards so the test reaches Garruk's damage Reaction deterministically.
            foreach (var hero in party)
            {
                var remove = hero.Hand.Where(c => string.Equals(c.Speed, "Reaction", System.StringComparison.OrdinalIgnoreCase) && !(hero.Id == "Garruk" && c.Id == "H012-013")).ToList();
                foreach (var card in remove)
                {
                    hero.Hand.Remove(card);
                    hero.Discard.Add(card.Id);
                }
            }
            var duplicateIgnore = garruk.Hand.Where(c => c.Id == "H012-013").Skip(1).ToList();
            foreach (var card in duplicateIgnore) { garruk.Hand.Remove(card); garruk.Discard.Add(card.Id); }
            Check(gm.DebugGiveCardToHand("Garruk", "H012-013"), "Ignore the Pain is available for the held-Action Reaction test.");

            int secondHandBeforeWorld = second.Hand.Count;

            // Complete every normal Hero turn without spending Actions. Each Hero should bank 3 general Actions.
            gm.SelectHero(garruk.Id);
            Check(gm.TryStartHeroTurn(garruk), "Garruk starts the normal Hero turn explicitly.");
            Check(garruk.Actions == 3, "Garruk receives exactly 3 normal Actions.");
            gm.EndHeroTurn();
            Check(garruk.TurnCompleted && garruk.Actions == 3, "Garruk ends the turn with all 3 Actions held for Reactions.");

            foreach (var hero in party.Where(h => h.Id != garruk.Id))
            {
                gm.SelectHero(hero.Id);
                Check(gm.TryStartHeroTurn(hero), hero.DisplayName + " starts their turn independently.");
                gm.EndHeroTurn();
                Check(hero.TurnCompleted && hero.Actions == 3, hero.DisplayName + " banks 3 unused Actions for the rest of the Round.");
            }

            // The last End Turn launches World Phase. Wait for Captain -> Garruk post-roll damage timing.
            bool reactionSeen = false;
            bool reactionSpent = false;
            float timeout = 15f;
            while (gm.Round == startingRound && timeout > 0f)
            {
                if (gm.HasPendingReactionWindow)
                {
                    if (gm.PendingReactionKind == ReactionWindowKind.BeforeEnemyAttack)
                    {
                        // No pre-Attack choice is part of this test; advance to the d6 roll.
                        gm.ResolveReactionWindow();
                    }
                    else if (gm.PendingReactionKind == ReactionWindowKind.BeforeHeroDamage)
                    {
                        if (!reactionSeen)
                        {
                            reactionSeen = true;
                            Check(garruk.Actions == 3, "Held Actions are still available during World Phase after Garruk's normal turn ended.");
                            Check(gm.PendingReactionOptions.Any(o => o.HeroId == "Garruk" && o.CardId == "H012-013"), "Ignore the Pain is offered from Garruk's held Reaction reserve after the Enemy Attack roll.");
                            gm.UseReaction("Garruk", "H012-013");
                            reactionSpent = true;
                            Check(garruk.Actions == 2, "The Reaction spends exactly 1 held Action (3 → 2).");
                            gm.ResolveReactionWindow();
                        }
                        else gm.ResolveReactionWindow();
                    }
                    else gm.ResolveReactionWindow();
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Check(reactionSeen, "World Phase reached a post-roll Hero-damage Reaction window.");
            Check(reactionSpent, "A saved Action was actually spent on a legal World Phase Reaction.");
            Check(gm.Round == startingRound + 1, "World Phase and End Phase advance exactly one Round.");
            Check(gm.TurnHero == null, "The new Hero Phase begins with no predetermined turn owner.");
            Check(party.All(h => h.Actions == 0 && h.AttackOnlyActions == 0), "End Phase clears every remaining held Action reserve.");
            Check(party.All(h => !h.TurnCompleted), "All surviving Heroes are unfinished again for the new Hero Phase.");
            Check(second.Hand.Count == secondHandBeforeWorld, "Round transition does not draw cards for Heroes before their own Start Turn.");

            int garrukHandBeforeStart = garruk.Hand.Count;
            int expectedGarrukDraw = Mathf.Min(garruk.DrawValue, garruk.Deck.Count + garruk.Discard.Count);
            gm.SelectHero(garruk.Id);
            Check(gm.TryStartHeroTurn(garruk), "Garruk can be freely chosen first in the new Round.");
            Check(garruk.Hand.Count - garrukHandBeforeStart == expectedGarrukDraw, "Round 2+ Draw Step occurs only when Garruk starts his own turn.");
            Check(garruk.Actions == 3, "New-Round Start Turn grants a fresh 3 Actions, not the old Reaction reserve.");
            Check(second.Actions == 0, "Another unstarted Hero remains at 0 Actions while Garruk owns the turn.");
            Check(second.Hand.Count == secondHandBeforeWorld, "Another Hero still has not drawn before their own Start Turn.");

            gm.EndHeroTurn();
            int secondHandBeforeStart = second.Hand.Count;
            int expectedSecondDraw = Mathf.Min(second.DrawValue, second.Deck.Count + second.Discard.Count);
            gm.SelectHero(second.Id);
            Check(gm.TryStartHeroTurn(second), second.DisplayName + " can be chosen next in the same Round.");
            Check(second.Hand.Count - secondHandBeforeStart == expectedSecondDraw, second.DisplayName + " draws exactly their Draw Value at their own Start Turn.");
            Check(second.Actions == 3, second.DisplayName + " independently gains exactly 3 Actions.");

            if (pass) Debug.Log("[ASCENDANCE][0.8.12 TEST] PASS — Round Transition + Reaction Reserve test completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.12 TEST] FAIL — one or more Round Transition + Reaction Reserve checks failed.");
        }
    }
}
#endif
