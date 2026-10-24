# Ascendance Unity 0.8.62 — Master Save/Load + Full Regression

Built directly from the runtime-verified 0.8.61 Full Campaign End-to-End Integration baseline (96 passed, 0 failed).

## Runtime hardening
- Fixes Stonebrook's NavMesh/presentation startup race.
- Runtime Quest setup may request spawn repair while `AscendanceGameManager.Initialize()` / session application is still running. Those requests are now deferred until world navigation setup finishes.
- Runtime NavMesh data is baked while detached and registered only after the asynchronous bake completes, avoiding an empty NavMesh being exposed to live agents.
- Hero `NavMeshAgent` components are created only after navigation is ready and only after the Hero spawn point has been sampled onto the NavMesh.
- If NavMesh settings/data are unavailable, Hero movement continues through the existing direct fallback path rather than creating an invalid agent.

## Master persistence regression
Adds `Ascendance/Validation/Test 0.8.62 - Master Save Load + Full Regression (Play Mode)`.

The validator checks:
- 0.8.62 build identity.
- Stonebrook presentation/NavMesh readiness and no detached enabled Hero agents.
- Mid-Quest save/load of party resources.
- Hero Health, Level/XP, consumables, kill count, profession XP, active modifiers, statuses, usage locks, Loot, Equipment, and kept Search effects.
- Location counters, temporary tags, and authored once-keys.
- Enemy mutable state, statuses, authored once-keys, movement state, and focus target.
- Persistent Hero-created pieces.
- Destructive-mutation restore proof (load must reconstruct saved state rather than reuse memory).
- Three consecutive idempotent save/load cycles.
- Resume surfaces: MID_QUEST -> RESULTS -> DOWNTIME -> CAMPAIGN_HUB -> CAMPAIGN_END.
- Campaign I terminal Victory persistence.
- Campaign I -> II Day-0 handoff persistence and archive retention.
- Q013 first-Quest Day-1 transition persistence.
- Corrupt-save rejection.
- One-click execution of the inherited full Location, Enemy, Hero/Card, and six-campaign integration validators; each nested suite must reach its own final PASS marker with 0 failed.
- Restoration of the user's pre-test save file.

## Validation target
Run the new 0.8.62 validator in Play Mode. A clean candidate must finish with:

`[ASCENDANCE][0.8.62 TEST] PASS — Master Save/Load + Full Regression completed: <N> passed, 0 failed.`

The prior 0.8.58/0.8.59/0.8.60/0.8.61 full-runtime validators remain included. The 0.8.62 master gate runs them sequentially, and the 0.8.61 campaign integration suite should continue to pass 96/96.
