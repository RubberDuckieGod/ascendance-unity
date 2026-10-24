# Ascendance Unity 0.10.2 — Real Network Transport + Linux Dedicated Server

Built from the runtime-verified 0.10.1a Client Presentation Context Hotfix baseline.

## Added
- Versioned length-prefixed UTF-8 JSON TCP protocol (`ProtocolVersion=1`).
- Persistent remote TCP client transport implementing the existing `IAscendanceCommandTransport0101` contract.
- Dedicated-server TCP listener that keeps socket work off-thread and marshals all authority/gameplay execution to Unity's main thread.
- Network operations for HELLO, JOIN, RECONNECT, SNAPSHOT, COMMAND, and PING.
- Client command responses reconcile the returned authoritative campaign snapshot while preserving local presentation selection.
- Remote reconnect-token persistence per endpoint/room/Hero.
- `-ascendance-connect HOST:PORT -room ROOM -player NAME` client startup path.
- `-port PORT` / `ASCENDANCE_PORT` dedicated-server configuration.
- Linux Dedicated Server Unity build target and Windows/Linux build scripts.
- DigitalOcean Ubuntu/systemd deployment package under `Deploy/DigitalOcean/`.
- 0.10.2 Play Mode transport gate with a real localhost TCP HELLO and START TURN command crossing the socket boundary.

## Preserved
- Locked 0.9.0 core gameplay/content behavior.
- 0.10.0 room-owned authority, Hero ownership, revisions, idempotency, persistence, reconnect model.
- 0.10.1/0.10.1a player-facing command routing and presentation-context isolation.

## Validation target
Run in Play Mode:

`Ascendance > Validation > Test 0.10.2 - Real Network Transport (Play Mode)`

The gate first requires the inherited client routing suite to remain 28/0, which itself requires server foundation 24/0 and master regression 54/0. The final 0.10.2 network count is defined by `NetworkTransportTestTools0102`.
