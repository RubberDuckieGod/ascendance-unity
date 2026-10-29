using System;

namespace Ascendance
{
    public sealed partial class AscendanceGameManager
    {
        // Recovered f/g predicates: a schema-1 persisted prompt must expose a
        // real legal choice, and deck control must still refer to the same cards.
        public bool HasPersistableEncounterDeckControl0102e()
        {
            var state = PendingEncounterDeckControl;
            return state != null && !string.IsNullOrWhiteSpace(state.SourceCardId)
                && !string.IsNullOrWhiteSpace(state.Mode) && (state.RevealedCardIds?.Count ?? 0) > 0
                && BuildEncounterDeckControlOptions().Count > 0 && DeckControlSnapshotStillMatches();
        }

        public bool HasPersistableRewardChoice0102e()
        {
            var state = PendingRewardChoice;
            return state != null && !string.IsNullOrWhiteSpace(state.Mode)
                && BuildRewardChoiceOptions().Count > 0;
        }

        private void SanitizeRestoredEncounterDeckControl0102e()
        {
            if (PendingEncounterDeckControl == null || HasPersistableEncounterDeckControl0102e()) return;
            PendingEncounterDeckControl = null;
            AddLog("SAVE RESTORE — invalid schema-1 Encounter deck-control prompt cleared.");
        }

        private void SanitizeRestoredRewardChoice0102e()
        {
            if (PendingRewardChoice == null || HasPersistableRewardChoice0102e()) return;
            PendingRewardChoice = null;
            AddLog("SAVE RESTORE — invalid schema-1 reward prompt cleared.");
        }
    }
}
