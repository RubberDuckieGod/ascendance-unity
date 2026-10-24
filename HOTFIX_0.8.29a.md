# Ascendance Unity 0.8.29a — Quest Party Presentation Reconciliation Hotfix

Validator/runtime hotfix built from 0.8.29. Gameplay quest rules are unchanged.

## Fix
- `StonebrookWorldBuilder.ValidateAndRepairCharacterSpawns()` now removes Hero avatar presentation objects for Heroes who are no longer participating after a party/quest transition.
- Missing participating Hero avatars are still spawned normally.
- This prevents stale Q001 Hero pieces from causing `PARTY SPAWN INCOMPLETE — expected 2, found 4` during Q002 dynamic enemy spawns.

Internal gameplay banner remains 0.8.29.
