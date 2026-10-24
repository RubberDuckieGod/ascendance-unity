# Ascendance Unity 0.4.3 — NavMesh Obstacle + State Initialization Fix

- Fixed `MissingComponentException` from `NavMeshObstacle` on Timber House.
- Replaced dynamic environment `NavMeshObstacle` components with static Not Walkable geometry in the runtime NavMesh bake.
- Game manager now initializes authoritative state before Stonebrook world construction.
- Q001 HUD now safely checks for the Goblin Captain with `TryGetValue`.
- Preserves 0.4.2 production asset/animation foundation and 0.3.2 stable HUD/camera behavior.
