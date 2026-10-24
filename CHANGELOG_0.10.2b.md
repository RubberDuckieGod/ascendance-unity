# Ascendance Unity 0.10.2b — Validator Coroutine Containment Hotfix

Built from 0.10.2a. No gameplay rules, authored content, wire protocol, or production TCP behavior changed.

## Fix
- Client Command Routing validator now drains the World Phase coroutine created when the second/final Hero ends their turn.
- Any reaction pause created by that World Phase is resolved through the same authoritative client router.
- The router validator requires a stable next-Hero-Phase boundary before restoring its captured production snapshot.
- Real Network Transport validator now requires a sustained idle window before isolated socket testing.
- The isolated Q001 fixture must remain legally startable for Aldric across a sustained pre-command window before the real TCP START TURN is sent.

## Why
0.10.2a proved the TCP request reached `AscendanceTcpServer0102.Update()` and room authority on Unity's main thread, but the inherited router validator had restored a save while its final-Hero World Phase coroutine was still scheduled. That coroutine later resumed in the network validator and opened a Goblin Archer reaction window, correctly blocking START TURN.

Expected Play Mode totals: master 54/0, server 24/0, router 29/0, network 25/0.
