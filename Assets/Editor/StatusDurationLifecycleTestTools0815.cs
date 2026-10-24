#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class StatusDurationLifecycleTestTools0815
    {
        [MenuItem("Ascendance/Validation/Test 0.8.15 - Status Duration Lifecycle (Play Mode)")]
        public static void TestStatusDurationLifecycle()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.15 TEST] Enter Play Mode before running the Status Duration Lifecycle test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.TurnHero != null || gm.ParticipatingHeroes.Any(h => h.Moving))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.15 TEST] Run this at the beginning of a Hero Phase with no turn, movement, or Reaction window in progress.");
                return;
            }

            var party = gm.ParticipatingHeroes.ToList();
            if (party.Count < 2)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.15 TEST] Use a party of at least 2 Heroes.");
                return;
            }

            bool pass = true;
            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.15 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.15 TEST] FAIL — " + label); }
            }

            var source = party[0];
            var target = party[1];
            var enemy = gm.Enemies.Values.First(e => !e.Defeated);

            foreach (var h in party)
            {
                h.Statuses.Clear();
                h.Health = h.MaxHealth;
                h.Downed = false;
                h.Dead = false;
                h.Actions = 0;
                h.AttackOnlyActions = 0;
                h.TurnCompleted = false;
            }
            enemy.Statuses.Clear();
            enemy.ActivatedThisRound = false;

            // Target-turn duration: persists until that Hero actually ends the relevant turn.
            gm.AddStatus(target, "Inspired", 1, 0, "0.8.15 target-turn test", true, source,
                "That Hero gains Inspired until the end of their next turn.");
            var inspired = target.Statuses.FirstOrDefault(s => s.Name == "Inspired");
            Check(inspired != null && inspired.ExpiryTiming == StatusExpiryTiming.EndOfTargetTurn,
                "'until the end of their next turn' binds to the target Hero turn clock.");
            gm.SelectHero(target.Id);
            Check(gm.TryStartHeroTurn(target), target.DisplayName + " starts the timed-status turn.");
            Check(target.HasStatus("Inspired"), "Target-turn status remains active during that Hero's turn.");
            gm.EndHeroTurn();
            Check(!target.HasStatus("Inspired"), "Target-turn status expires exactly when that Hero ends the turn.");

            // Source-start duration: another unit's status follows the source Hero's next turn start.
            gm.AddStatus(target, "Inspired", 1, 0, "0.8.15 source-start test", true, source,
                "Until the start of your next turn, that Hero gains Inspired.");
            inspired = target.Statuses.FirstOrDefault(s => s.Name == "Inspired");
            Check(inspired != null && inspired.ExpiryTiming == StatusExpiryTiming.StartOfSourceTurn && inspired.SourceHeroId == source.Id,
                "'until the start of your next turn' binds to the source Hero clock.");
            gm.DebugAdvanceHeroTurnStartStatusClock(source.Id);
            Check(!target.HasStatus("Inspired"), "Source-start status expires when the source Hero's next turn starts.");

            // Enemy activation duration: Stunned remains until that Enemy completes its next activation.
            gm.AddStatus(enemy, "Stunned", 1, 0, "0.8.15 activation test", false, source,
                "The target gains Stunned until the end of its next activation.");
            var stunned = enemy.Statuses.FirstOrDefault(s => s.Name == "Stunned");
            Check(stunned != null && stunned.ExpiryTiming == StatusExpiryTiming.EndOfTargetActivation,
                "'until the end of its next activation' binds to the Enemy activation clock.");
            gm.DebugAdvanceEnemyActivationStatusClock(enemy.Id);
            Check(!enemy.Statuses.Any(s => s.Name == "Stunned"), "Activation-timed status expires at Enemy activation end.");

            // Round duration: only the End-of-Round clock removes it.
            gm.AddStatus(enemy, "Marked", 0, 1, "0.8.15 round test", false, source,
                "The target becomes Marked until the end of the round.");
            var marked = enemy.Statuses.FirstOrDefault(s => s.Name == "Marked");
            Check(marked != null && marked.ExpiryTiming == StatusExpiryTiming.EndOfRound,
                "'until the end of the round' binds to the round clock.");
            gm.DebugAdvanceEnemyActivationStatusClock(enemy.Id);
            Check(enemy.Statuses.Any(s => s.Name == "Marked"), "Round-timed status does not expire on Enemy activation.");
            gm.DebugAdvanceEndRoundStatusClock();
            Check(!enemy.Statuses.Any(s => s.Name == "Marked"), "Round-timed status expires at End of Round.");

            // Persistent status: legacy turns=1 must not delete an authored 'Until removed' condition.
            gm.AddStatus(enemy, "Rooted", 1, 0, "0.8.15 persistent test", false, source);
            var rooted = enemy.Statuses.FirstOrDefault(s => s.Name == "Rooted");
            Check(rooted != null && rooted.ExpiryTiming == StatusExpiryTiming.Persistent,
                "Authored 'Until removed' status is classified Persistent instead of using the legacy generic timer.");
            gm.DebugAdvanceEnemyActivationStatusClock(enemy.Id);
            gm.DebugAdvanceEndRoundStatusClock();
            Check(enemy.Statuses.Any(s => s.Name == "Rooted"), "Persistent status survives activation and round clocks.");

            // Status-definition fallback: Marked with no card-specific timing follows the source's next turn end.
            enemy.Statuses.RemoveAll(s => s.Name == "Marked");
            gm.AddStatus(enemy, "Marked", 1, 0, "0.8.15 source-end fallback test", false, source);
            marked = enemy.Statuses.FirstOrDefault(s => s.Name == "Marked");
            Check(marked != null && marked.ExpiryTiming == StatusExpiryTiming.EndOfSourceTurn,
                "Marked falls back to its authored source-next-turn duration when a card supplies no override.");
            gm.DebugAdvanceHeroTurnEndStatusClock(source.Id);
            Check(!enemy.Statuses.Any(s => s.Name == "Marked"), "Source-end status expires when the source Hero ends the relevant turn.");

            if (pass) Debug.Log("[ASCENDANCE][0.8.15 TEST] PASS — Status Duration Lifecycle test completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.15 TEST] FAIL — one or more Status Duration Lifecycle checks failed.");
        }
    }
}
#endif
