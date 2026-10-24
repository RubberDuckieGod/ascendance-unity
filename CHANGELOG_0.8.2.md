# Ascendance Unity 0.8.2 — Hero Setup Lifecycle Fix

Baseline: 0.8.1 Unity 6.6 Warning Cleanup.

## Fixes

- Removed the temporary default-party activation from `AscendanceGameManager.Initialize()`.
- Party selection is now applied only by `AscendanceSession` after the core Hero/Enemy databases exist.
- `SetParty(...)` now accepts a `runQuestStartHooks` flag.
- Fresh New Game party setup runs Quest-start Hero hooks exactly once for the selected participating Heroes.
- Continue / save restore applies party membership without rerunning one-time Quest-start Hero hooks.
- Prevents Sir Aldric's Battle Ready / Guarded setup effect, and future Hero setup effects, from firing for Heroes who are not in the loaded party.
- Prevents loaded campaigns from duplicating one-time Quest-start effects before saved state is restored.
- Runtime build marker bumped to 0.8.2.

No intended changes to card definitions, combat, reactions, Downed/Revive, save format, Q001, or presentation.
