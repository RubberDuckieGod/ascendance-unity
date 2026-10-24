# Ascendance Unity 0.8.20a — Unity Warning Cleanup

Built from the 0.8.20 Search & Travel Encounter Runtime candidate.

- Replaced deprecated `UnityEngine.Object.FindObjectOfType<StonebrookWorldBuilder>()`
  with `UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>()` in the
  dynamic Travel Encounter enemy presentation path.
- No gameplay, Search, Encounter, save/load, severity, spawn, or quest logic changed.
- The Unity Input Manager deprecation message is intentionally unchanged; migrating
  the project input backend is a separate system-level task.
