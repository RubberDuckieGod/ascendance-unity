#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class DefeatRevivalTestTools0813
    {
        [MenuItem("Ascendance/Validation/Test 0.8.13 - Downed Dead Revival (Play Mode)")]
        public static void TestDownedDeadRevival()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.13 TEST] Enter Play Mode before running the Downed/Dead/Revival test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.TurnHero != null || gm.ParticipatingHeroes.Any(h => h.Moving))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.13 TEST] Run this at the beginning of a Hero Phase with no turn, movement, or Reaction window in progress.");
                return;
            }

            var party = gm.ParticipatingHeroes.ToList();
            if (party.Count < 3)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.13 TEST] Use a party of at least 3 Heroes. The default 4-Hero party is recommended.");
                return;
            }

            var reviver = party.FirstOrDefault(h => h.Id != "Mira") ?? party[0];
            var downed = party.First(h => h.Id != reviver.Id);
            var deadHero = party.First(h => h.Id != reviver.Id && h.Id != downed.Id);
            bool pass = true;
            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.13 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.13 TEST] FAIL — " + label); }
            }

            // Fresh deterministic state.
            foreach (var h in party)
            {
                h.Health = h.MaxHealth;
                h.Downed = false;
                h.Dead = false;
                h.Actions = 0;
                h.AttackOnlyActions = 0;
                h.TurnCompleted = false;
                h.Moving = false;
                h.LocationId = "L000";
                h.HealingPotions = Mathf.Max(1, h.HealingPotions);
            }

            // Downed classification requires a non-Defeated ally at the same Location at the instant of defeat.
            gm.DebugApplyHeroDamage(downed.Id, downed.MaxHealth, "0.8.13 Downed classification test");
            Check(downed.Health == 0 && downed.Downed && !downed.Dead, downed.DisplayName + " becomes Downed (not Dead) with a living ally in the Location.");
            Check(!gm.CanStartHeroTurn(downed), "A Downed Hero cannot Start Turn.");
            Check(!gm.QuestFailed, "One Downed Hero does not fail the Quest while another non-Defeated Hero remains.");

            // Classification is sticky: ally departure does not convert Downed to Dead.
            reviver.LocationId = "L001";
            Check(downed.Downed && !downed.Dead, "A Downed Hero remains Downed after the witnessing ally leaves the Location.");
            reviver.LocationId = "L000";

            // A Downed Hero cannot activate their own potion.
            gm.SelectHero(downed.Id);
            int downedPotions = downed.HealingPotions;
            Check(!gm.TryUseHealingPotionOn(downed.Id), "A Downed Hero cannot activate their own Healing Potion.");
            Check(downed.HealingPotions == downedPotions, "Blocked Downed-Hero potion use consumes no Potion.");

            // Another Hero can revive for 1 Action + one Common Healing Potion.
            gm.SelectHero(reviver.Id);
            Check(gm.TryStartHeroTurn(reviver), reviver.DisplayName + " starts a legal turn to perform the revive.");
            int actorActionsBefore = reviver.Actions;
            int actorPotionsBefore = reviver.HealingPotions;
            Check(gm.TryUseHealingPotionOn(downed.Id), reviver.DisplayName + " can target the Downed ally with a Common Healing Potion.");
            Check(reviver.Actions == actorActionsBefore - 1, "Revive costs exactly 1 general Action.");
            Check(reviver.HealingPotions == actorPotionsBefore - 1, "Common Healing Potion is consumed by the revive.");
            Check(downed.Health == Mathf.Min(4, downed.MaxHealth) && !downed.Downed && !downed.Dead, "Common Healing Potion revives the Downed Hero at 4 Health.");
            Check(downed.Actions == 0, "Revival itself grants no Actions.");

            gm.EndHeroTurn();
            gm.SelectHero(downed.Id);
            Check(gm.TryStartHeroTurn(downed), "A revived Hero who had not completed their turn may later Start Turn normally.");
            Check(downed.Actions == 3, "The revived Hero gains the normal 3 Actions from Start Turn, not from revival.");
            gm.EndHeroTurn();

            // A completed Hero keeps held Actions when becoming Downed; revival does not reset turn completion.
            int heldBeforeDefeat = downed.Actions;
            gm.DebugApplyHeroDamage(downed.Id, downed.Health, "0.8.13 held reserve test");
            Check(downed.Downed && downed.Actions == heldBeforeDefeat, "Becoming Downed preserves already-unspent held Actions instead of deleting them.");
            gm.HealHero(downed, 4, "0.8.13 validation heal", reviver);
            Check(!downed.Downed && downed.Health == Mathf.Min(4, downed.MaxHealth), "Healing above 0 immediately ends Downed state.");
            Check(downed.TurnCompleted, "Revival does not reset a Hero's completed-turn state.");
            Check(!gm.CanStartHeroTurn(downed), "A revived Hero who already completed their turn cannot take a second normal turn.");
            Check(downed.Actions == heldBeforeDefeat, "Revival does not create or restore extra Actions beyond the Hero's existing reserve.");

            // Dead classification: no other non-Defeated Hero shares the Location at the instant of defeat.
            foreach (var h in party.Where(h => h.Id != deadHero.Id)) h.LocationId = "L000";
            deadHero.LocationId = "L001";
            deadHero.Health = deadHero.MaxHealth;
            deadHero.Downed = false;
            deadHero.Dead = false;
            deadHero.Actions = 2;
            gm.DebugApplyHeroDamage(deadHero.Id, deadHero.MaxHealth, "0.8.13 Dead classification test");
            Check(deadHero.Health == 0 && deadHero.Dead && !deadHero.Downed, deadHero.DisplayName + " becomes Dead when no non-Defeated ally shares the Location.");
            Check(deadHero.Actions == 0 && deadHero.AttackOnlyActions == 0, "Dead Heroes retain no Action reserve.");
            Check(!gm.CanStartHeroTurn(deadHero), "A Dead Hero cannot Start Turn.");
            gm.HealHero(deadHero, 4, "illegal normal heal", reviver);
            Check(deadHero.Health == 0 && deadHero.Dead, "Normal healing cannot revive a Dead Hero.");

            // Finally prove the explicit all-Heroes-Defeated failure gate without allowing a single Downed Hero to fail early.
            foreach (var h in party)
            {
                if (h.Dead) continue;
                h.LocationId = "L000";
                h.Health = Mathf.Max(1, h.Health);
                h.Downed = false;
                h.Dead = false;
            }
            var live = party.Where(h => !h.Dead).ToList();
            for (int i = 0; i < live.Count; i++)
                gm.DebugApplyHeroDamage(live[i].Id, live[i].Health, "0.8.13 all-Heroes-Defeated test");
            Check(gm.QuestFailed, "Q001 fails only when all participating Heroes are Defeated.");

            if (pass) Debug.Log("[ASCENDANCE][0.8.13 TEST] PASS — Downed / Dead / Revival Enforcement test completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.13 TEST] FAIL — one or more Downed / Dead / Revival checks failed.");
        }
    }
}
#endif
