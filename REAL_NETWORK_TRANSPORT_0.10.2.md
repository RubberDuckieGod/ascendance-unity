# Real Network Transport — 0.10.2

## Architecture

Client presentation/UI -> `AscendanceClientCommandRouter0101` -> `IAscendanceCommandTransport0101`

- Local game: `AscendanceLoopbackTransport0101`
- Remote game: `AscendanceTcpCommandTransport0102`

Remote commands cross a persistent TCP connection to `AscendanceTcpServer0102`, which queues each request and executes it on the Unity main thread through `AscendanceRoomAuthority0100`. No socket worker mutates game state.

## Wire contract

Each message is a 4-byte network-order length followed by UTF-8 JSON. Maximum frame size is 8 MiB. Protocol version is 1.

Operations:
- HELLO
- JOIN
- RECONNECT
- SNAPSHOT
- COMMAND
- PING

## Dedicated server

Headless server startup example:

`AscendanceServer.x86_64 -batchmode -nographics -ascendance-server -room ASC-PRIMARY -port 7777`

Default port is 7777/TCP. `ASCENDANCE_PORT` can supply the server port when `-port` is omitted.

## Remote client

`Ascendance.exe -ascendance-connect SERVER_IP:7777 -room ASC-PRIMARY -player PlayerName`

The first authoritative gameplay command resolves or creates the selected Hero's server-owned player session. A reconnect token is retained locally for later reconnects.

## State reconciliation

Every command response can include a complete authoritative campaign snapshot. The client applies that campaign state after remote responses while restoring its local Hero/enemy presentation selection afterward.

Transient reaction/modal presentation is carried in `ServerTransientState0100` and remains part of later full synchronization work; 0.10.2 establishes the real transport and authoritative command path.
