# Ascendance Unity 0.7.1

## Game Shell Save & Import Cleanup

- Added the built-in Unity JSONSerialize module (`com.unity.modules.jsonserialize` 1.0.0).
- Fixes `JsonUtility` compile errors in `AscendanceSaveSystem`.
- Added explicit normals to all generated Stonebrook OBJ placeholder meshes so Unity no longer has to recalculate missing normals on import.
- Preserves 0.7.0 menu, Hero select, Continue, save/load, pause/settings, standalone builder, Q001 gameplay, camera, combat, and NavMesh behavior.
