#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class ReactionTimingTestTools0810
    {
        [MenuItem("Ascendance/Validation/Test 0.8.10 - Garruk Enemy-Attack Damage Timing (Play Mode)")]
        public static void TestGarrukEnemyAttackDamageTiming()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE] Enter Play Mode before running the 0.8.10 enemy-attack damage timing test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (!gm.Heroes.TryGetValue("Garruk", out var garruk) || !garruk.Participating)
            {
                Debug.LogWarning("[ASCENDANCE] Garruk must be in the current party for the 0.8.10 timing test.");
                return;
            }

            if (!gm.DebugGiveCardToHand("Garruk", "H012-013"))
            {
                Debug.LogWarning("[ASCENDANCE] Could not move Ignore the Pain into Garruk's hand.");
                return;
            }

            garruk.Actions = Mathf.Max(garruk.Actions, 1);
            if (!gm.DebugOpenEnemyAttackDamageWindow("Captain", "Garruk", 6))
                Debug.LogWarning("[ASCENDANCE] Could not stage the deterministic Captain -> Garruk damage window.");
        }
    }
}
#endif
