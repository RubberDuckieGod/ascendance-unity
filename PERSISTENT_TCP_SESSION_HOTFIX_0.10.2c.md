# 0.10.2c — Persistent TCP Session Hotfix

The authoritative TCP session must remain connected while a player is thinking. A turn-based decision may legitimately take minutes; server-side inactivity is not a disconnect condition.

The client also performs one reconnect/rejoin retry for an interrupted authoritative command. The retry preserves the original `CommandId`, relying on the room authority's existing command-result cache for idempotency.
