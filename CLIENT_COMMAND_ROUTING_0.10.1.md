# Client Command Routing — 0.10.1

The client no longer owns gameplay mutation. Presentation code requests actions through `AscendanceClientCommandRouter0101`, which packages a `ServerCommandEnvelope0100` and sends it through an `IAscendanceCommandTransport0101`.

Today the installed transport is `AscendanceLoopbackTransport0101`, which calls the room authority in-process for regression-safe local play. The next networking milestone can install a remote transport without changing HUD, input, Quest briefing, campaign, or Downtime gameplay button call sites.

Flow:

Client UI/Input → Client Command Router → Command Transport → Server Room Authority → AscendanceGameManager rules → authoritative result/snapshot

The router preserves the server-foundation safeguards from 0.10.0: player/Hero ownership, connected-session checks, room identity, optimistic revision checks, idempotent command IDs, server-owned execution, and authoritative snapshots.

Pure presentation selection (viewing a Hero or enemy target) remains local client input. Gameplay mutations are routed.
