# Ascendance Unity 0.10.1 — Client Command Routing

Built from the 0.10.0 Dedicated Server Authority Foundation candidate and the locked 0.9.0 Core Game Complete rules/content baseline.

## What changed
- Added `AscendanceClientCommandRouter0101`, the single gameplay request boundary used by presentation/input code.
- Added a transport interface (`IAscendanceCommandTransport0101`) and local loopback transport over the server-owned room authority.
- Rewired world-click movement, attack hotkey, turn flow, attacks, cards, Search, Explore, Quest actions, Healing Potion, Reactions, reward/deck choices, and campaign actions through the router.
- Rewired Quest briefing open/cancel/ready/confirm through server commands.
- Rewired inventory/equipment management and Stonebrook Downtime recovery, Work, Gather, loadout, merchant, crafting, potion/loot/equipment trading through server commands.
- Extended the command envelope with secondary-target and integer payloads needed by authoritative trading/loadout actions.
- Extended the authoritative command catalog to cover 37+ gameplay command types.
- Preserved local hot-seat control by creating distinct loopback player/Hero sessions on demand. No player is promoted to host.
- Added `ClientCommandRoutingTestTools0101`, including source audits that fail if routed UI/input surfaces regress to direct gameplay mutation.
- Re-baselined the full master/server regression stack to 0.10.1 without changing authored gameplay content.

## What this milestone deliberately does not add
- No real internet socket/transport yet.
- No DigitalOcean deployment yet.
- No remote snapshot application/interpolation yet.

0.10.2 should replace the loopback transport with the first real Linux dedicated-server network transport while leaving these presentation call sites unchanged.

## Unity validation
Enter Play Mode and run:

**Ascendance → Validation → Test 0.10.1 - Client Command Routing (Play Mode)**

Expected inherited lines include:

`[ASCENDANCE][0.10.1 TEST] PASS — Master Save/Load + Full Regression completed: 54 passed, 0 failed.`

`[ASCENDANCE][0.10.1 SERVER] PASS — Dedicated Server Authority Foundation completed: 24 passed, 0 failed.`

Expected final routing line:

`[ASCENDANCE][0.10.1 ROUTER] PASS — Client Command Routing completed: 27 passed, 0 failed.`
