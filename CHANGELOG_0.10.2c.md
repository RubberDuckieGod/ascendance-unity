# Ascendance Unity 0.10.2c — Persistent TCP Session Hotfix

Runtime testing against the live DigitalOcean dedicated server proved the initial remote HELLO connection succeeded, but the server closed an otherwise idle TCP connection before the player's first gameplay command. The 0.10.2b server configured accepted clients with a 35-second receive timeout, which is incompatible with a turn-based game where players can spend longer than 35 seconds reading cards, inspecting the board, or deciding on an action.

## Fixes

- Dedicated-server client sockets now use an infinite receive timeout (`ReceiveTimeout = 0`) so thinking time does not disconnect a player.
- TCP keepalive is enabled on both accepted server sockets and remote client sockets.
- The remote client now treats `TcpClient.Connected` as advisory rather than proof that the peer is alive.
- Hero session resolution reconnects and reuses the server-issued reconnect token when a stale socket is discovered.
- Gameplay command sending retries once after reconnect using the same authoritative `CommandId`. Room-authority idempotency prevents duplicate actions when the original command committed but its response was lost.
- Snapshot requests also reconnect/retry once after a dropped socket.
- No gameplay rule, authored content, room-authority ownership rule, or wire-protocol version changed.

## Live failure this fixes

The first live Windows client connected successfully to `ASC-PRIMARY` on DigitalOcean, then its first START TURN failed with `EndOfStreamException: Ascendance network peer closed the connection`. Every subsequent routed action hit the already-dead socket. ITEMS uses the same authoritative command transport, which is why Inventory also appeared nonfunctional.

## Validation targets

- Master Save/Load + Full Regression: 54 / 0
- Dedicated Server Authority Foundation: 24 / 0
- Client Command Routing: 29 / 0
- Real Network Transport: 27 / 0
