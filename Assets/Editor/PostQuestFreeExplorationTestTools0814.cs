#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class PostQuestFreeExplorationTestTools0814
    {
        [MenuItem("Ascendance/Validation/Test 0.8.14 - Post-Quest Free Exploration (Play Mode)")]
        public static void TestPostQuestFreeExploration()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.14 TEST] Enter Play Mode before running the Post-Quest Free Exploration test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.TurnHero != null || gm.ParticipatingHeroes.Any(h => h.Moving))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.14 TEST] Run this at the beginning of a Hero Phase with no turn, movement, or Reaction window in progress.");
                return;
            }

            var party = gm.ParticipatingHeroes.ToList();
            if (party.Count < 2)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.14 TEST] Use a party of at least 2 Heroes.");
                return;
            }

            bool pass = true;
            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.14 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.14 TEST] FAIL — " + label); }
            }

            // Deterministic fresh state for the contract test.
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
            }
            foreach (var enemy in gm.Enemies.Values)
            {
                enemy.Health = enemy.MaxHealth;
                enemy.ActivatedThisRound = false;
            }
            gm.Locations["L003"].Revealed = false;
            gm.Locations["L004"].Revealed = false;

            var finisher = party[0];
            var postQuestHero = party[1];

            // Complete Q001 through the normal objective check. The Captain is already Defeated,
            // then the Hero spends two legal Rescue Actions so the second rescue runs CheckVictory().
            gm.Enemies["Captain"].Health = 0;
            gm.SelectHero(finisher.Id);
            Check(gm.TryStartHeroTurn(finisher), finisher.DisplayName + " starts the Quest-finishing turn.");
            gm.RescueVillager();
            gm.RescueVillager();
            Check(gm.QuestComplete && !gm.QuestFailed, "Q001 records completion normally.");
            Check(gm.CanUseTurnActions(finisher), "Quest completion does not terminate the current Hero turn.");

            // The same Hero can still Explore after victory if all normal Explore rules are met.
            // River Crossing connects to unrevealed Ancient Ruins; remove its enemy only for this
            // deterministic validation so engagement does not legally block Explore.
            finisher.LocationId = "L001";
            gm.Enemies["RaiderB"].Health = 0;
            int actionsBeforeExplore = finisher.Actions;
            gm.Explore();
            Check(gm.Locations["L003"].Revealed, "Explore remains legal after Q001 completion.");
            Check(finisher.Actions == actionsBeforeExplore - 1, "Post-Quest Explore still costs exactly 1 Action.");

            gm.EndHeroTurn();
            gm.SelectHero(postQuestHero.Id);
            Check(gm.TryStartHeroTurn(postQuestHero), "Another Hero can Start Turn normally after Q001 completion.");
            Check(postQuestHero.Actions == 3, "Post-Quest Hero turns still gain the normal 3 Actions.");

            // Victory does not grant immunity. Isolate a Hero and deal lethal damage: the exact
            // 0.8.13 Dead rule must still classify that Hero as Dead after the Quest is complete.
            postQuestHero.LocationId = "L004";
            foreach (var ally in party.Where(h => h.Id != postQuestHero.Id)) ally.LocationId = "L000";
            gm.DebugApplyHeroDamage(postQuestHero.Id, postQuestHero.Health, "0.8.14 post-Quest death contract test");
            Check(postQuestHero.Dead && !postQuestHero.Downed && postQuestHero.Health == 0, "A Hero can still become Dead during post-Quest exploration.");
            Check(gm.QuestComplete && !gm.QuestFailed, "Post-Quest death does not erase or retroactively fail an already completed Q001.");

            if (pass) Debug.Log("[ASCENDANCE][0.8.14 TEST] PASS — Post-Quest Free Exploration contract completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.14 TEST] FAIL — one or more Post-Quest Free Exploration checks failed.");
        }
    }
}
#endif
