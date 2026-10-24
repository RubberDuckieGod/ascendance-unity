# Ascendance Unity 0.10.0 — Dedicated Server Authority Foundation

Built directly from the runtime-verified 0.9.0 Core Game Complete baseline.

## What changed
- Added a transport-agnostic, server-owned room authority. There is no player-host authority field or host migration requirement.
- Added persistent player identity, one-Hero ownership, disconnect retention, and reconnect-token restoration.
- Added authoritative command envelopes with room id, player id, actor Hero, expected room revision, and idempotent CommandId.
- Added stale-revision, Hero-ownership, room-membership, connection, and duplicate-command enforcement before rules execution.
- Added server snapshots that separate durable campaign state from live transient state (Reaction windows, movement, modal choices, recent log).
- Added dedicated room persistence under Application.persistentDataPath/AscendanceServerRooms0100, using the canonical campaign serializer at safe save boundaries.
- Added a headless dedicated-server process path. Dedicated processes skip world/HUD/input/presentation objects and run the same AscendanceGameManager rules engine.
- Added a Windows Dedicated Server build target and BuildAscendanceWindowsServer.bat.
- Preserved local presentation mode. 0.10.0 is the authority/transport foundation; routing every existing UI click through this gateway is the next networking milestone.

## Validation target
Run `Ascendance > Validation > Test 0.10.0 - Dedicated Server Authority Foundation (Play Mode)`.
The gate first requires the inherited master regression to remain 54 passed / 0 failed, then verifies room ownership, command authorization, revision enforcement, idempotency, disconnect/reconnect, snapshots, and room persistence.
