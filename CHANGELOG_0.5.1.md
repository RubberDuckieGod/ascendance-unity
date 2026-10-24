# Ascendance Unity 0.5.1 — Player-Safe Runtime Navigation

## Fixed
- Runtime NavMesh baking no longer reads imported OBJ render meshes.
- Eliminates Unity's `Source mesh default does not allow read access` warnings.
- Navigation is now generated from lightweight world-space box proxies / collider bounds.
- Production houses use explicit footprint blockers rather than visual meshes.
- Production trees block only their trunks instead of their foliage crowns.
- Production bridge uses a dedicated flat walkable navigation proxy.

## Why this matters
0.5.0 worked in Unity Editor Play Mode, but Unity warned that unreadable imported meshes would not be valid runtime NavMesh sources in a built player. 0.5.1 removes that dependency entirely, keeping production art independent from gameplay/navigation geometry and avoiding unnecessary Read/Write memory overhead on final models.

## Preserved
All gameplay, combat, camera, HUD, exploration, production-art loading, animation impact timing, and 0.4.4 foundation behavior are unchanged.
