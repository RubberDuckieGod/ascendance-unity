# Ascendance Unity 0.8.34 — Campaign Hub & Quest Selection UI

Built directly from the runtime-verified 0.8.33 Campaign I World Event progression baseline.

## Added
- Full-screen Campaign Hub overlay generated entirely at runtime; no scene wiring required.
- Current World Event, World Event Day / authored day limit, Total Campaign Day, and chapter pressure meter.
- Three live Quest cards for the active World Event using the authored Quest names, story, objectives, and rewards from `QuestContent0829`.
- Quest state presentation: Active, Available, Completed, Failed / Retry Available, Missed, and Locked.
- Required / Optional path labels for WE001–WE003 and sequential Required / Final labels for WE004.
- Quest start / retry buttons call `BeginCampaignQuest0833()` directly; there is no duplicate UI quest-routing logic.
- World Event advance button calls `AdvanceWorldEvent0833()` and is interactable only when `CanAdvanceWorldEvent0833` is true.
- Live chapter outcome preview for Perfect Clear rewards, required-path advancement, and day-limit penalties.
- Permanent CAMPAIGN button in the right-side Quest HUD panel.
- 0.8.34 Play Mode validator under `Ascendance/Validation/Test 0.8.34 - Campaign Hub UI (Play Mode)`.

## Architecture
`CampaignHubRuntime0834.cs` is presentation-facing only. The authoritative rules remain in 0.8.33 `CampaignRuntime0833.cs`; 0.8.34 only reads that state and routes player button presses back through its public gates.

## Intentionally unchanged
- Q001–Q012 Quest combat/runtime rules.
- WE001–WE004 authored day thresholds and reward/penalty mechanics.
- Q010 print-locked Fortress Gate / two-Ballista setup.
- Save schema and existing Campaign I persistence.
