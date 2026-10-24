# Ascendance 0.5.0 Production Art Pass

This build includes authored OBJ/MTL environment meshes and runtime surface
textures so Stonebrook no longer depends on cubes/spheres for its main village
silhouette.

The four Heroes and goblins still retain the articulated in-project fallback rigs
until rigged production character assets are imported. The production character
bridge from 0.4.x remains active: exact Hero/Enemy prefabs placed in the documented
Resources slots automatically replace the fallback rigs without touching gameplay.

## Character upgrade slots
- Resources/Ascendance/Characters/Heroes/Garruk
- Resources/Ascendance/Characters/Heroes/Rowan
- Resources/Ascendance/Characters/Heroes/Aldric
- Resources/Ascendance/Characters/Heroes/Elara
- Resources/Ascendance/Characters/Enemies/GoblinRaider
- Resources/Ascendance/Characters/Enemies/GoblinCaptain

Imported rigged characters should use Unity Humanoid where possible and may expose:
Speed, Combat, AttackType, Attack, Hit, Death, Revive, Downed.

Animation Events:
- AscendanceImpact
- AscendanceAttackFinished
