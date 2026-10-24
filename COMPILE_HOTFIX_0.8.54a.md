# Ascendance Unity 0.8.54a — Loot & Equipment Compile Hotfix

Built from 0.8.54 Full Loot & Equipment Runtime Execution.

## Fixes
- Fixed CS0136 in `RewardRuntime0854.cs` by renaming the relic-target local so it no longer collides with the later Loot `targetId` local in the same method scope.
- Replaced deprecated `Object.FindFirstObjectByType<StonebrookWorldBuilder>()` with `Object.FindAnyObjectByType<StonebrookWorldBuilder>()` in `QuestRuntime0829.cs`.
- Build marker updated to `0.8.54a`; validator behavior and test count are otherwise unchanged.

The Input Manager deprecation notice is a Unity project-level warning and is intentionally not changed in this gameplay hotfix.
