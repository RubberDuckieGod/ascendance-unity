using System;

namespace Ascendance
{
    /// <summary>
    /// JsonUtility can materialize an absent serializable reference as an object
    /// containing only default fields. Such an object is not a pending choice.
    /// Preserve every object carrying any saved information, including extension modes.
    /// </summary>
    public static class AscendanceOptionalSaveState0102
    {
        public static AscendanceSaveData Normalize(AscendanceSaveData save)
        {
            if (save == null) return null;
            if (IsEmpty(save.PendingRewardChoice)) save.PendingRewardChoice = null;
            if (IsEmpty(save.PendingEncounterDeckControl)) save.PendingEncounterDeckControl = null;
            return save;
        }

        public static bool IsEmpty(RewardChoiceSaveData state) => state == null ||
            (string.IsNullOrEmpty(state.SourceCardId) && string.IsNullOrEmpty(state.SourceCardName)
             && string.IsNullOrEmpty(state.HeroId) && string.IsNullOrEmpty(state.Mode)
             && string.IsNullOrEmpty(state.Prompt)
             && (state.RevealedCardIds == null || state.RevealedCardIds.Count == 0));

        public static bool IsEmpty(EncounterDeckControlSaveData state) => state == null ||
            (string.IsNullOrEmpty(state.SourceCardId) && string.IsNullOrEmpty(state.SourceCardName)
             && string.IsNullOrEmpty(state.Mode)
             && (state.RevealedCardIds == null || state.RevealedCardIds.Count == 0));
    }
}
