# Ascendance Unity 0.2.2 — NavMesh Preview Compatibility Fix

- Fixed Unity 6.6 `InitializeNavMeshPath` exception by creating `NavMeshPath` in `Awake()` instead of a MonoBehaviour field initializer.
- Added null guards around the movement preview.
- Samples both path endpoints onto the NavMesh before calling `NavMesh.CalculatePath`.
- Falls back to a straight legal/illegal preview line while the runtime NavMesh is unavailable or still baking.
- Preserves Ascendance Location-graph movement legality; the NavMesh remains presentation/pathfinding only.
- Based on the working 0.2.1 input-compatibility build.
