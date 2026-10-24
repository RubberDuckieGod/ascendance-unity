#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class TurnLifecycleTestTools0811
    {
        [MenuItem("Ascendance/Validation/Test 0.8.11 - Hero Turn Start Lifecycle (Play Mode)")]
        public static void TestHeroTurnStartLifecycle()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.11 TEST] Enter Play Mode before running the Hero turn-start lifecycle test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.ParticipatingHeroes.Any(h => h.Moving))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.11 TEST] Finish the current movement/reaction/resolution before running this test.");
                return;
            }
            if (gm.TurnHero != null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.11 TEST] Run this test between Hero turns, when no Hero currently owns the turn.");
                return;
            }

            var available = gm.ParticipatingHeroes.Where(h => !h.IsDefeated && !h.TurnCompleted).Take(2).ToList();
            if (available.Count < 2)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.11 TEST] Need at least two unfinished, non-Defeated Heroes in the party.");
                return;
            }

            var first = available[0];
            var second = available[1];
            int firstHandBefore = first.Hand.Count;
            int secondHandBefore = second.Hand.Count;
            int expectedFirstDraw = gm.Round == 1 ? 0 : Mathf.Min(first.DrawValue, first.Deck.Count + first.Discard.Count);
            int expectedSecondDraw = gm.Round == 1 ? 0 : Mathf.Min(second.DrawValue, second.Deck.Count + second.Discard.Count);
            bool pass = true;

            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.11 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.11 TEST] FAIL — " + label); }
            }

            Check(first.Actions == 0, first.DisplayName + " begins unstarted with 0 Actions.");
            Check(second.Actions == 0, second.DisplayName + " begins unstarted with 0 Actions.");
            Check(!gm.CanUseTurnActions(first), "Normal Actions are unavailable before Start Turn.");

            gm.SelectHero(first.Id);
            Check(gm.TryStartHeroTurn(first), first.DisplayName + " can explicitly start the turn.");
            Check(gm.TurnHeroId == first.Id, "Turn ownership is assigned only at Start Turn.");
            Check(first.Actions == 3, "Start Turn grants exactly 3 Actions.");
            Check(first.Hand.Count - firstHandBefore == expectedFirstDraw,
                gm.Round == 1 ? "Round 1 keeps the 5-card opening hand without an extra draw." : "Start Turn draws exactly the Hero Draw Value.");
            Check(gm.CanUseTurnActions(first), "The current turn Hero can use normal Actions.");
            Check(!gm.CanUseTurnActions(second), "Off-turn Heroes cannot use normal Actions.");
            Check(!gm.TryStartHeroTurn(second) && gm.TurnHeroId == first.Id, "A second Hero cannot steal an in-progress turn.");

            gm.EndHeroTurn();
            Check(first.TurnCompleted, "End Turn marks the Hero complete for the Hero Phase.");
            Check(gm.TurnHero == null, "End Turn returns the Hero Phase to player choice.");
            Check(first.Actions == 3, "Unused Actions remain held for legal Reactions until End Phase.");
            Check(second.Actions == 0, "The next unstarted Hero still has 0 Actions.");
            Check(!gm.TryStartHeroTurn(first), "A completed Hero cannot start a second normal turn this Hero Phase.");

            gm.SelectHero(second.Id);
            Check(gm.TryStartHeroTurn(second), second.DisplayName + " can be chosen next; order remains player-chosen.");
            Check(second.Actions == 3, "The next Hero independently gains exactly 3 Actions.");
            Check(second.Hand.Count - secondHandBefore == expectedSecondDraw,
                gm.Round == 1 ? "The second Round 1 Hero also receives no duplicate opening-hand draw." : "The second Hero draws only when their own turn starts.");

            if (pass) Debug.Log("[ASCENDANCE][0.8.11 TEST] PASS — Hero Turn Start Lifecycle test completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.11 TEST] FAIL — one or more Hero Turn Start Lifecycle checks failed.");
        }
    }
}
#endif
