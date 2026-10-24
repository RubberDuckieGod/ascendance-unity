# Ascendance Unity 0.8.49a — Windows Player Build Compile Hotfix

Built from runtime-verified 0.8.49.

## Fix
- Fixed Windows standalone Player compilation where runtime UI and Downtime code referenced `DebugPendingRewardChoiceMode()`, a validator helper compiled only under `UNITY_EDITOR`.
- Added runtime-safe `PendingRewardChoiceMode` accessor on `AscendanceGameManager`.
- `AscendanceHUD` and `DowntimeRuntime0836` now use the runtime accessor.
- Editor-only `DebugPendingRewardChoiceMode()` remains available to validators and delegates to the runtime accessor.
- No reward-choice, equipment, Downtime, campaign, quest, combat, save, or UI behavior changed.

## Player-build compatibility scan
- Checked runtime scripts for references to methods defined only inside `#if UNITY_EDITOR`; no remaining leaks were found.
