# Ascendance Unity 0.8.36b — Resolved-Downtime Load Normalization Hotfix

Built from the Unity-tested 0.8.36a candidate after the 0.8.36a validator reported 19 passed / 3 failed.

## Fix
- Resolved-Quest saves now normalize non-durable modal/combat gates during `ApplySaveData()`:
  - Reaction window / queued reaction state
  - Encounter-deck control prompt
  - Reward-choice prompt
  - `IsResolving`
- Mid-Quest saves still restore serialized deck-control and reward-choice prompts exactly as before.
- Resolved Quest outcome is reconciled back into `CampaignProgress0833` at the end of load, ensuring completed/failed history matches the restored Quest state before UI refresh.

## Validator hardening
The 0.8.36 validator is now the 0.8.36b validator and splits the previous combined Q002-routing assertion into individual checks for:
- restored Q001 completion state
- restored WE001 identity
- Q001 present in campaign CompletedQuestIds
- Q002 recommended
- Q002 listed in AvailableCampaignQuests0833
- no transient modal/combat gate
- Q002 passes CanBeginCampaignQuest0833
- Q002 actually starts via BeginRecommendedCampaignQuest0833
- Downtime closes once Q002 is active

It deliberately contaminates the pre-load runtime with stale reaction, deck-control, reward-choice, and resolving gates after the save is captured, then proves that loading the resolved downtime save clears them.

Expected validator result: **28 passed, 0 failed**.
