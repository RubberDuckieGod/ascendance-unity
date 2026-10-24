# Ascendance Unity 0.2.2 — Unity 6.6 Input Compatibility Fix

- Removed `com.unity.inputsystem` package dependency.
- Removed all `UnityEngine.InputSystem` / `InputSystemUIInputModule` references.
- Centralized CRPG controls continue using Unity's built-in legacy `Input` API.
- EventSystem now uses `StandaloneInputModule`.
- Keeps 0.2.0 camera, selection, targeting, NavMesh movement, path preview, combat and HUD features.
- Target editor remains Unity 6000.6.5f1.

The Input Manager deprecation notice is expected and does not prevent Play Mode.
