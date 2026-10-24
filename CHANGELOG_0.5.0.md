# Ascendance Unity 0.5.0 — Stonebrook Production Visual Pass

Built from locked stable baseline **0.4.4**.

## Included visual assets
- Added authored OBJ/MTL Stonebrook environment models instead of relying on primitive-only fallbacks.
- Three house variants with plaster, timber framing, gabled roofs, windows, doors, chimneys/porch details.
- Three tree variants, two bushes, three rocks.
- Dedicated bridge, well, wagon, crate/barrel cluster, brazier, and fence meshes.
- Added runtime grass, dirt-road, stone, and water surface textures.
- Added deterministic environment variant selection.
- Exact user production slots still override the bundled variants.

## Presentation
- Warm window emission is applied automatically to imported/bundled houses.
- Production brazier meshes receive emissive flame treatment and dynamic flickering light.
- Added lightweight ambient firefly/mote field without adding a Particle System package dependency.
- Refined the fallback Hero/goblin rigs with faces and class-specific head/armor silhouette details.

## Systems preserved
- 0.4.4 rules/state baseline.
- CRPG camera, selection, targeting, navigation, Q001, Explore, Action economy.
- Hero automatic-hit / Enemy 1d6 combat.
- Animation-impact damage pipeline.
- Production character prefab/Animator/socket architecture.
- Legacy Input Manager compatibility path.

## Character art note
0.5.0 improves the built-in fallback characters but does not bundle third-party rigged character binaries.
The existing production prefab slots remain the path for final rigged Hero/goblin models.
