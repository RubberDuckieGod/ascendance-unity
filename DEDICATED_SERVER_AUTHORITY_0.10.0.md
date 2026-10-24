# Ascendance 0.10.0 — Dedicated Server Authority Foundation

## Authority model
The room, not a player, is authoritative. `AscendanceRoomAuthority0100` owns room revision, persistent player/Hero ownership, command de-duplication, snapshots, and durable campaign persistence. `AscendanceGameManager` remains the single rules engine.

## Player identity and reconnect
A player session receives a stable `PlayerId`, one `HeroId`, and a secret reconnect token. Disconnecting marks the session offline but does not free the Hero. Presenting the reconnect token restores the same persistent session and Hero. Reconnect tokens are stored only in server persistence and are intentionally omitted from client-facing snapshots.

## Command contract
Every gameplay mutation sent to the authority carries a unique `CommandId`, `RoomId`, `PlayerId`, `ExpectedRevision`, command type, and actor/target arguments. The server rejects commands for another Hero, stale revisions, wrong rooms, disconnected sessions, and duplicated Hero claims. Re-sending an already processed `CommandId` returns the cached result without executing gameplay a second time.

## State model
Client snapshots contain a clone of canonical campaign state plus a separate transient section for live Reaction windows, modal choices, movement, and a bounded recent log. The durable server-room campaign snapshot is refreshed only at existing safe-save boundaries, so transient combat/reaction state is never mistaken for a crash-safe save point.

## Headless server
A process started in batch mode or with `-ascendance-server` is treated as a dedicated server. It loads Stonebrook and skips world presentation, HUD, camera/input controllers, and other client-only presentation. Use `-room <ROOM_ID>` to select a persisted room identity.

Example:
`AscendanceServer.exe -batchmode -nographics -ascendance-server -room FAMILY-CAMPAIGN-01`

## 0.10.x roadmap
0.10.0 establishes the authority boundary. The next milestones should route existing client UI actions through the authority gateway, add a real network transport, add lobby/party creation and permanent leave/kick flows, and then verify two-process reconnect/state synchronization before expanding to four remote clients.
