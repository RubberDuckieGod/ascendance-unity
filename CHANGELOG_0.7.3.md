# Ascendance Unity 0.7.3 — Scene Lifecycle Bootstrap Fix

## Fixed
- Main Menu -> Stonebrook now boots the full gameplay runtime on every scene transition.
- Stonebrook -> Main Menu now rebuilds the menu shell on return.
- Replaced one-shot `AfterSceneLoad` gameplay/menu bootstraps with `SceneManager.sceneLoaded` listeners installed before the first scene loads.
- Prevents the blue-screen state where Stonebrook contained only `Main Camera` and never created `ASCENDANCE_RUNTIME`.
- Listener registration removes any prior handler before subscribing, preventing duplicate bootstraps when Unity Enter Play Mode options alter domain reload behavior.

## Expected Stonebrook hierarchy in Play Mode
- Main Camera
- ASCENDANCE_RUNTIME
- EventSystem
- Stonebrook_World

0.7.2 camera fix and 0.7.1 save/JSON fixes are preserved.
