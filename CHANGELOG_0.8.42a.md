# Ascendance Unity 0.8.42a — Campaign Resume Validator Compile Hotfix

## Fix
- Fixed `Assets/Editor/CampaignResumeTestTools0842.cs` compile error caused by calling private `StartQuest0829` from the Editor validator.
- The validator now uses the existing public deterministic test helper `DebugStartQuest0829("Q003")` for its intentional runtime-mutation step before save restoration.

## Scope
- No campaign rules changed.
- No save schema changed.
- No resume behavior changed.
- No combat, downtime, crafting, trading, equipment, or ready-up behavior changed.
