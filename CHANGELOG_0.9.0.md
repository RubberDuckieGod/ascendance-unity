# CHANGELOG — Ascendance Unity 0.9.0 — Core Game Complete

0.9.0 promotes the runtime-verified 0.8.62b baseline into the first Core Game Complete release candidate. This milestone intentionally adds no new gameplay mechanics. It locks the complete authored core game and adds a production-readiness gate around the already-verified runtime.

## Locked core scope
- 16 authored Heroes
- 323 Hero-card definitions
- 30-card personal deck for every Hero
- 190 Loot
- 144 Equipment
- 100 Search cards
- 105 Encounters
- 125 Enemies
- 236 Locations
- 200 Crafting Recipes
- 72 Quests
- 24 World Events
- 6 runtime-playable campaigns
- full campaign save/load, retry, archive, World Event, downtime, and terminal-campaign persistence

## 0.9.0 promotion work
- Promotes `AscendanceGameManager.BuildVersion` to `0.9.0`.
- Promotes project PlayerSettings to `Ascendance` / bundle version `0.9.0`.
- Keeps MainMenu -> Stonebrook as the production build scene order.
- Re-runs the full 54-check master regression under the 0.9.0 identity.
- Adds a Core Game Complete release gate that checks production identity, build scenes, required Resources data, catalog counts/IDs, 30-card Hero decks, six-campaign structure, Windows build tooling, zero nested Unity errors, and zero CONTENT PENDING leakage.

## Unity validation
Enter Play Mode and run:

**Ascendance → Validation → Test 0.9.0 - Core Game Complete Gate (Play Mode)**

The gate first runs the complete master regression. Expected master line:

`[ASCENDANCE][0.9.0 TEST] PASS — Master Save/Load + Full Regression completed: 54 passed, 0 failed.`

Expected final release line:

`[ASCENDANCE][0.9.0 RELEASE] PASS — Core Game Complete Gate completed: 16 passed, 0 failed.`

0.9.0 is a candidate until Unity reports both lines with 0 failed and the release gate reports zero nested Unity Error/Exception/Assert entries.

After 0.9.0 is locked, major new work should branch from this baseline rather than altering the verified core ruleset.
