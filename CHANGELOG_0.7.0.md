# Ascendance Unity 0.7.0 — Game Shell & Standalone Foundation

Baseline: 0.6.0 Professional Art Bridge / 0.5.1 player-safe navigation gameplay foundation.

## Added
- MainMenu scene and runtime title screen.
- Continue button that activates only when a Q001 save exists.
- New Game flow with Hero selection for Garruk, Rowan, Aldric, and Elara.
- Scene transition from MainMenu to Stonebrook.
- ESC pause menu in Stonebrook.
- Save Game, Settings, Quit to Menu, and Quit Game controls.
- F5 quick save during safe gameplay states.
- JSON campaign save at `Application.persistentDataPath/campaign_q001.json`.
- Q001 persistence for round, fire tokens, villagers rescued, quest completion, Hero HP/actions/location/status state, Enemy HP/location, revealed Locations, and selected Hero/Enemy.
- Continue restores Hero/Enemy world positions and defeated/downed presentation.
- Display settings: fullscreen/windowed, resolution, master volume.
- Main menu and pause settings share the same saved PlayerPrefs.
- Windows x64 standalone build menu: `Ascendance > Build > Build Windows Standalone`.
- Optional `BuildAscendanceWindows.bat` for batch-mode building with Unity 6000.6.5f1.
- Build output target: `Builds/Windows/Ascendance.exe`.
- Explicit EditorBuildSettings with MainMenu first and Stonebrook second.

## Preserved
- Q001 rules and objectives.
- Hero automatic-hit combat and Enemy d6 attacks.
- CRPG camera/input/navigation.
- Runtime NavMesh and Location legality.
- Animation-timed attack impacts.
- Existing HUD/card prototype.
- Production-art override bridge and placeholder fallback presentation.

## Scope note
0.7.0 is a standalone-game foundation, not the full campaign migration. It packages the currently playable Q001 vertical slice behind a normal game shell so future systems can be added without changing how the player launches, saves, pauses, or configures the game.
