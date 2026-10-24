# Ascendance Unity 0.3.0 — Visual Combat & Stonebrook Art Pass

Built on the stable 0.2.2 CRPG controls/navigation foundation.

## Character presentation

- Replaced capsule-only Heroes with articulated, asset-free humanoid proxy rigs.
- Garruk, Rowan, Aldric, and Elara now have distinct silhouettes, colors, armor/clothing, and weapons.
- Replaced capsule-only Goblins with articulated goblin rigs; Goblin Captain has a larger armored silhouette.
- Added procedural idle/walk animation driven by actual movement speed.
- Added attack wind-up/strike/recovery animation hooks.
- Added hit recoil and death/downed fall animations.
- Character roots retain simple colliders, so 0.2.2 click-selection/targeting behavior remains intact.

## Combat feedback

- World-space health bars for Heroes and Enemies.
- Floating damage numbers.
- Attack/hit flashes and stronger selected/hovered character emission.
- Enemy attacks now visibly animate toward the Hero they damage.
- Defeated enemies fall instead of simply shrinking.
- Downed Heroes fall and lose their active health/selection presentation.

## Stonebrook environment

- Rebuilt houses with timber beams, gabled roofs, doors, and glowing windows.
- Added bridge railings, well, wagon, crates, barrels, bushes, rocks, denser trees, cobblestones, and extra road dressing.
- Improved dusk lighting with warm sun + cool fill, stronger fog, warm windows, and flickering braziers.
- Reduced the visual dominance of Location markers while preserving their gameplay role.

## Rules preserved

- Ascendance Location-to-Location movement remains authoritative.
- NavMesh remains a presentation layer only.
- Hero attacks automatically hit.
- Enemy attacks still roll 1d6.
- Existing Q001, Actions, Engagement, movement, target selection, and Garruk card/ability behavior are unchanged.
- Legacy Unity Input API remains in use for 6000.6.5f1 compatibility; the deprecation notice is a non-blocking warning.

## Art boundary

0.3.0 uses procedural articulated proxy characters, not licensed/imported production meshes. They are deliberately structured so imported humanoid models and Animator Controllers can replace the proxy visuals later without rewriting Ascendance gameplay state.
