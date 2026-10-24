# Ascendance Unity 0.8.58a — Location Runtime Compile Hotfix

Built from the 0.8.58 Full Location Runtime candidate after Unity reported three compile errors in `LocationRuntime0858.cs`.

## Fixes

1. `Append0858LocationChoiceOptions` now resolves its `HeroState` from `RewardChoiceState.HeroId` before checking the Hero hand for the discard-or-damage choice. This fixes the two `CS0103` errors for the undeclared `hero` identifier around line 266.
2. `DebugLocationCount0858` now counts `FullLocation0850` rows with a lambda that passes each row's `id` into `IsLocationExecutable0858(string)`. This fixes the `CS1503` method-group/type mismatch around line 712.
3. Build/validator identity is bumped to **0.8.58a**.

No Location gameplay behavior was intentionally changed beyond making the already-authored choice/count paths compile. The Input Manager notice remains a Unity deprecation warning and is not part of this compile hotfix.

## Unity validation

Enter Play Mode, then run:

**Ascendance → Validation → Test 0.8.58a - Full Location Runtime Execution (Play Mode)**

Expected final line:

`[ASCENDANCE][0.8.58a TEST] PASS — Full Location Runtime Execution completed: 74 passed, 0 failed.`
