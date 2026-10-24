# Ascendance Unity 0.8.59a — Enemy Runtime Compile Hotfix

0.8.59a fixes the compile blocker introduced by the 0.8.59 enemy save-state persistence work.

## Fix

`Assets/Scripts/Shell/AscendanceSaveSystem.cs` now imports `System.Collections.Generic`, allowing the existing `List<string>` fallback used by `EnemySaveData.UsedKeys0859` to compile.

No Enemy gameplay behavior is intentionally changed by this hotfix.

## Unity validation

Enter Play Mode and run:

**Ascendance → Validation → Test 0.8.59a - Full Enemy Runtime Execution (Play Mode)**

Expected result:

`[ASCENDANCE][0.8.59a TEST] PASS — Full Enemy Runtime Execution completed: 64 passed, 0 failed.`
