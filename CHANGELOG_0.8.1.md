# Ascendance Unity 0.8.1 — Unity 6.6 Warning Cleanup

Baseline: **0.8.0 Full Hero System Migration**. No intentional gameplay or Hero-rule changes.

## Changes
- Replaced deprecated `Object.FindObjectsByType<T>(..., FindObjectsSortMode)` calls with the current Unity 6.6 overloads.
- `ShellCameraBootstrap` now uses `FindObjectsByType<Camera>(FindObjectsInactive.Include)`.
- Hero-created-piece cleanup now uses `FindObjectsByType<HeroPieceAvatar>()`.
- Removed the unused `GameBootstrap.installed` field and assignment.
- Build marker bumped to **0.8.1**.

## Intentionally retained warning
- The project still uses Unity's legacy Input Manager. Unity 6.6 reports that system as deprecated. This remains intentional because the Input System package previously produced package-level compile failures in the user's installed Unity 6000.6.5f1 editor. The warning does not block Play Mode or standalone builds.

## Gameplay baseline
All 0.8.0 Hero-system behavior is preserved: 16-Hero roster, 30-card personal decks, 5-card opening hands, draw/discard/reshuffle, reactions, Hero abilities, potions, Downed/Revive, kill counts, Hero-created pieces, and save persistence.
