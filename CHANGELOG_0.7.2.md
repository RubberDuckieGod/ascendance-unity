# Ascendance Unity 0.7.2 — Shell Camera Fix

- Added a scene-load camera bootstrap for MainMenu and Stonebrook.
- Display 1 now receives an enabled Main Camera immediately when either game scene loads.
- Reuses the same camera when Stonebrook configures the CRPG camera, avoiding duplicate cameras.
- Forces camera target display, target texture, enabled state, clipping, and culling mask to safe defaults.
- MainMenuUI and StonebrookWorldBuilder now share the same camera bootstrap path.
- Prevents Unity's "No cameras rendering" Game-view overlay during Hero Select / scene transitions.
- Preserves 0.7.1 save JSON, shell menus, gameplay, navigation, combat, and standalone build configuration.
