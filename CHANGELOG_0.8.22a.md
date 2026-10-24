# Ascendance Unity 0.8.22a — Unity API Warning Cleanup

Built from the 0.8.22 Encounter Hazards & Persistent Objects candidate.

## Change
- Replaced deprecated `FindObjectsByType<T>(FindObjectsSortMode.None)` calls with Unity's current parameterless `FindObjectsByType<T>()` overload in:
  - `Assets/Scripts/World/EncounterObjectAvatar.cs`
  - `Assets/Scripts/Core/AscendanceGameManager.cs`
  - `Assets/Editor/EncounterHazardObjectTestTools0822.cs`
- Added a static regression guard so `FindObjectsSortMode` cannot re-enter Scripts/Editor code unnoticed.

## Behavior
No gameplay rules, encounter-object behavior, save format, or Play Mode validation semantics changed. Build version remains 0.8.22.

## Note
Unity's project-wide Input Manager deprecation notice remains intentionally unchanged for a dedicated input-system migration.
