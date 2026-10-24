# Ascendance Unity 0.10.1a — Client Presentation Context Hotfix

Built from 0.10.1 Client Command Routing.

## Fix
- Authoritative command execution now treats `SelectedHeroId` and `SelectedEnemyId` as temporary execution context only.
- The server room authority captures its before/after fingerprint outside that temporary context and restores the client's prior presentation selection after both accepted and rejected commands.
- This fixes local hot-seat routing where ending Aldric's turn could overwrite a locally selected Lyra and cause the next START TURN to reuse the wrong player/Hero session.
- The routing validator adds a direct regression check that successful authoritative commands preserve the local Hero presentation selection.

## Expected Play Mode results
- Master Save/Load + Full Regression: 54 passed, 0 failed
- Dedicated Server Authority Foundation: 24 passed, 0 failed
- Client Command Routing: 28 passed, 0 failed
