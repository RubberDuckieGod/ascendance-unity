# Ascendance Unity 0.8.47a — Interactive Campaign Hub Validator Compile Hotfix

Built from 0.8.47.

- Fixes `InteractiveCampaignHubTestTools0847.cs` compile error at the authored Quest-plan count assertion.
- `ValewatchQuestPlan` is an `IReadOnlyDictionary`; validator now uses `ValewatchQuestPlan.Count == 12` rather than `ValewatchQuestPlan.Values.Count == 12`.
- No Campaign II UI behavior, campaign progression, save/load, quest selection, or gameplay rules changed.
