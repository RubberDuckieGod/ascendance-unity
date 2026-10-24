# Ascendance Unity 0.4.2 — Stonebrook Compile Fix

- Fixed C# CS0136 in `StonebrookWorldBuilder.CreateTree()` by renaming the production-prefab NavMesh obstacle local variable so it no longer shadows the fallback tree obstacle variable.
- No gameplay, camera, HUD, animation, combat, or NavMesh behavior was intentionally changed.
- Retains the Animation module manifest fix from 0.4.1.
- The legacy Input Manager deprecation notice remains a warning only.
