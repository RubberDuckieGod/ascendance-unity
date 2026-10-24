# Ascendance Unity 0.8.25a — Validator Compile Hotfix

- Fixed `EncounterDeckControlTestTools0825.cs` calling a nonexistent editor helper `DebugCompleteEnemyActivation`.
- The validator now uses the existing editor-only helper `DebugAdvanceEnemyActivationStatusClock`, which invokes the same internal `CompleteEnemyActivation` lifecycle used by the runtime.
- No gameplay/runtime behavior changed.
