#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class ReactionTestTools088
    {

        [MenuItem("Ascendance/Validation/Test - Give Aldric Interpose (Play Mode)")]
        public static void GiveAldricInterpose()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE] Enter Play Mode before using the Interpose test-card command.");
                return;
            }

            if (!AscendanceGameManager.Instance.DebugGiveCardToHand("Aldric", "H001-009"))
                Debug.LogWarning("[ASCENDANCE] Could not move Interpose into Aldric's hand. Both physical copies may already be unavailable from Deck/Discard.");
        }


        [MenuItem("Ascendance/Validation/Test - Give Garruk Ignore the Pain + Feed the Rage (Play Mode)")]
        public static void GiveGarrukDamageReactionPair()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE] Enter Play Mode before using the Garruk damage-reaction test command.");
                return;
            }

            bool reaction = AscendanceGameManager.Instance.DebugGiveCardToHand("Garruk", "H012-013");
            bool trigger = AscendanceGameManager.Instance.DebugGiveCardToHand("Garruk", "H012-012");
            if (!reaction) Debug.LogWarning("[ASCENDANCE] Could not move Ignore the Pain into Garruk's hand.");
            if (!trigger) Debug.LogWarning("[ASCENDANCE] Could not move Feed the Rage into Garruk's hand.");
        }

        [MenuItem("Ascendance/Validation/Test - Give Garruk Ignore the Pain (Play Mode)")]
        public static void GiveGarrukIgnoreThePain()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE] Enter Play Mode before using the Ignore the Pain test-card command.");
                return;
            }

            if (!AscendanceGameManager.Instance.DebugGiveCardToHand("Garruk", "H012-013"))
                Debug.LogWarning("[ASCENDANCE] Could not move Ignore the Pain into Garruk's hand. Both physical copies may already be unavailable from Deck/Discard.");
        }
    }
}
#endif
