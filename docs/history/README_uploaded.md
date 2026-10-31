# Ascendance Unity 0.10.2c — Real Network Transport + Linux Dedicated Server

**Locked parent:** 0.10.1a Client Presentation Context Hotfix.

0.10.2c replaces the loopback-only deployment ceiling with a real persistent TCP transport. Dedicated Linux/Windows builds expose the room-owned authority on a configurable TCP port; clients can swap the existing command router to a remote server with `-ascendance-connect HOST:PORT`.

Run in Play Mode:

**Ascendance → Validation → Test 0.10.2c - Real Network Transport (Play Mode)**

For DigitalOcean deployment, see `Deploy/DigitalOcean/README.md`.

Expected 0.10.2c Play Mode totals: master **54/0**, server foundation **24/0**, client routing **29/0**, real network transport **25/0**. The network gate includes stable-idle and stable-start preconditions plus the real localhost TCP HELLO and START TURN socket command.

# Ascendance Unity 0.10.1a — Client Presentation Context Hotfix

**Locked core baseline:** 0.9.0 Core Game Complete.  
**Networking parent:** 0.10.1 Client Command Routing.

0.10.1a fixes the remaining local hot-seat routing regression in the 0.10.1 command boundary. Server execution may temporarily select the authoritative actor/target, but that execution context is now restored after every accepted or rejected command so it cannot overwrite the client's local Hero presentation selection.

Run in Play Mode:

**Ascendance → Validation → Test 0.10.1a - Client Command Routing (Play Mode)**

Expected final routing result: `28 passed, 0 failed`, after the inherited 54/0 master and 24/0 server foundation gates.

# Ascendance Unity 0.10.1 — Client Command Routing

0.10.1 moves player-facing gameplay requests behind a transport-agnostic client command router. Local play uses an in-process loopback transport to the same server-owned room authority; the next milestone can replace loopback with a real DigitalOcean/Linux network transport without rewriting gameplay UI call sites.

# Ascendance Unity 0.10.0 — Dedicated Server Authority Foundation

**Locked parent baseline:** 0.9.0 Core Game Complete.

0.10.0 introduces the server-owned authority boundary without changing the proven core gameplay rules. Dedicated server processes can run headless, players retain Hero ownership across disconnect/reconnect, commands are revisioned/idempotent, and room campaign state can be persisted independently of the local campaign save.

# Ascendance Unity 0.9.0 — Core Game Complete

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

## 0.10.2c live-network hotfix

0.10.2c removes the 35-second server receive timeout that disconnected live clients while players were thinking. Accepted server sockets now remain persistent, TCP keepalive is enabled, and the remote client reconnects/rejoins and retries one idempotent command after a stale socket is detected.
