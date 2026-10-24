# 0.10.1a — Client Presentation Context Hotfix

## Runtime issue found in 0.10.1
The authoritative room temporarily selected the command actor/target before routing a command. Rejected commands restored the previous client selection, but accepted commands did not. In local hot-seat play this meant ending Aldric's turn while Lyra was selected could leave Aldric selected, causing the next routed START TURN to target Aldric again.

## Fix
- Capture the before-state fingerprint before installing temporary server execution context.
- Route the authoritative command.
- Restore the client's prior Hero/enemy presentation selection on both success and rejection.
- Capture the after-state fingerprint only after presentation context is restored.
- Add an explicit routing regression assertion that Lyra remains locally selected after Aldric's END TURN.

## Validator stability
The inherited L007 Abandoned Campsite check now validates that the reveal grants at least one authored Loot rather than exactly one. This preserves valid Legendary/Mythic bonus-Loot chains such as Dragonlord's Hoard without creating a random false failure.

## Expected Play Mode results
- Master Save/Load + Full Regression: 54 passed, 0 failed
- Dedicated Server Authority Foundation: 24 passed, 0 failed
- Client Command Routing: 28 passed, 0 failed
