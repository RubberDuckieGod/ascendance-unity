using System;

namespace Ascendance
{
    /// <summary>
    /// JsonUtility can materialize an absent serializable reference as an object
    /// containing only default fields. Such an object is not a pending choice.
    /// Honor explicit f/g presence flags, and preserve meaningful schema-0 extension payloads.
    /// </summary>
    public static class AscendanceOptionalSaveState0102
    {
        public static AscendanceSaveData Normalize(AscendanceSaveData save) =>
            AscendanceSaveSystem.NormalizePendingStatePresence0102f(save);

        public static bool IsEmpty(RewardChoiceSaveData state) => state == null ||
            (string.IsNullOrWhiteSpace(state.SourceCardId) && string.IsNullOrWhiteSpace(state.SourceCardName)
             && string.IsNullOrWhiteSpace(state.HeroId) && string.IsNullOrWhiteSpace(state.Mode)
             && string.IsNullOrWhiteSpace(state.Prompt)
             && (state.RevealedCardIds == null || state.RevealedCardIds.Count == 0));

        public static bool IsEmpty(EncounterDeckControlSaveData state) => state == null ||
            (string.IsNullOrWhiteSpace(state.SourceCardId) && string.IsNullOrWhiteSpace(state.SourceCardName)
             && string.IsNullOrWhiteSpace(state.Mode)
             && (state.RevealedCardIds == null || state.RevealedCardIds.Count == 0));
    }
}
