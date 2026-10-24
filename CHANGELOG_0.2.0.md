# 0.2.0 — CRPG Controls & Presentation Foundation

## Added
- CRPGInputController with Hero selection, Enemy targeting, Location movement, and hotkeys.
- MovementPathPreview using NavMesh path corners.
- RuntimeNavMeshBaker using UnityEngine.AI (no extra navigation package).
- WorldRing selection/target/hover presentation.
- WorldBillboardLabel for Location labels.
- Selected Enemy state and target cycling.
- Async logical move completion: Action spent at movement start; Location updates on arrival.
- BG3-style camera control foundation.
- Target panel and improved HUD layout.

## Changed
- GameBootstrap uses FindAnyObjectByType instead of deprecated FindFirstObjectByType.
- World clicks are centralized instead of individual OnMouseDown handlers.
- Hero roots now carry NavMeshAgent components; visible capsules are child bodies.
- Combat rotates Heroes toward their targets and adds hit feedback.
- Stonebrook lighting/fog/blockout presentation improved.

## Preserved
- Q001 rules/objectives.
- Hero automatic-hit combat.
- Enemy d6 attack formula.
- Garruk Battle Roar, Bloodrage, Crushing Blow, Bloodied Strike, Groundbreaker, and Avatar of Rage prototype logic.
