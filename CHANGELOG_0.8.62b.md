# Ascendance Unity 0.8.62b — Enemy Presentation Lifecycle Hotfix

Built from runtime-verified 0.8.62a master regression candidate.

## Fix
- `StonebrookWorldBuilder.ValidateAndRepairCharacterSpawns()` now removes any registered `EnemyAvatar` whose ID is no longer present in the authoritative `AscendanceGameManager.Enemies` dictionary before spawning or validating current Quest presentation.
- Prevents stale debug/previous-Quest Enemy presentation registrations from accumulating across nested regression suites and Quest transitions.
- Eliminates false `ENEMY SPAWN INCOMPLETE` LogErrors such as `Enemies 7/1`, `8/2`, and `9/3` when logical runtime Enemy counts are already correct.
- No Enemy rules, combat math, Quest logic, save schema, or authored content changed.

## Regression
- Master validator now explicitly requires Enemy presentation registration count/IDs to match the authoritative Enemy runtime after nested-suite reset.
- Expected master result: **54 passed, 0 failed**, with no `ENEMY SPAWN INCOMPLETE` errors.
