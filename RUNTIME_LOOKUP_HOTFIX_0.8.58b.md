# Ascendance Unity 0.8.58b — Authored Location Lookup Hotfix

## Root cause
Stonebrook preserves legacy runtime graph IDs for save compatibility. Those runtime IDs do not always match the printable/authored Location IDs. The 0.8.58 validator helper used one `FirstOrDefault` predicate that accepted either field, so an early runtime-ID collision could be returned before the correct `AuthoredLocationId`.

Examples from the failed 0.8.58a run:
- authored `L003` Woodland Clearing collided with runtime `L003` Ancient Ruins;
- authored `L006` Goblin Lookout collided with runtime `L006` Woodland Clearing;
- authored `L007` Abandoned Campsite collided with runtime `L007` Rocky Pass;
- authored `L008` Spider Nest collided with runtime `L008` Fallen Watchtower.

That produced false behavioral failures and eventually a validator null reference when a later authored Location could not be resolved correctly.

## Fix
`DebugLocationByAuthored0858Internal` now resolves exact `AuthoredLocationId` first and only falls back to the runtime dictionary ID when no authored match exists. No Location gameplay rule was intentionally changed.

The validator now also contains a regression assertion proving the canonical authored lookup for the known Stonebrook collisions.

## Validation target
Run **Ascendance → Validation → Test 0.8.58b - Full Location Runtime Execution (Play Mode)**.
Expected final line: `Full Location Runtime Execution completed: 75 passed, 0 failed.`
