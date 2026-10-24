# Ascendance Unity 0.8.62a — Master Regression Isolation Hotfix

Built from the runtime-tested 0.8.62 Master Save/Load + Full Regression candidate.

## What the 0.8.62 run proved
- NavMesh startup cleanup passed.
- Master save/load persistence checks passed.
- The inherited 0.8.60c Hero/Card suite passed.
- The inherited 0.8.61 Full Campaign End-to-End Integration suite passed 96/96.
- The final master result was 51 passed / 2 failed only because two inherited suites were launched from the active Q013 state.

## Root cause
The 0.8.62 master validator intentionally ended its campaign-boundary persistence section by loading active Q013 Day 1, then immediately launched older standalone validators.

Those older validators were originally authored against a fresh Q001 runtime:
- The Location validator encountered a still-live pending reward choice, so later Location reward-choice tests were blocked with `Resolve the existing reward choice first`.
- The Enemy validator retained non-debug Q013 enemies. Its `Clean()` helper only removes validator-created `DBG_*` enemies, so E001 Reckless Assault and E036 Hidden Blade incorrectly saw extra enemies at the validation Location.

This was validator state contamination, not a gameplay-rule regression.

## Fix
Before each inherited full-runtime suite, the 0.8.62a master validator now:
1. closes Results / Downtime / Campaign Hub / Quest Briefing surfaces,
2. cancels any outstanding briefing state,
3. calls the authoritative Campaign debug reset back to fresh Q001,
4. clears pending content text,
5. yields one frame before launching the inherited validator.

`DebugResetCampaignProgress0833()` routes through `StartQuest0829("Q001")`, which clears pending reward/deck-control/content state and rebuilds the known-good Q001 enemy setup.

## Runtime scope
No gameplay rules, authored content, combat math, Location effects, Enemy abilities, save schema, or NavMesh behavior were changed by this hotfix.

## Validation
In Play Mode run:

**Ascendance → Validation → Test 0.8.62a - Master Save Load + Full Regression (Play Mode)**

Target final line:

`[ASCENDANCE][0.8.62a TEST] PASS — Master Save/Load + Full Regression completed: 53 passed, 0 failed.`

The inherited suites should also finish cleanly, including:
- Full Location Runtime Execution
- Full Enemy Runtime Execution
- Full Hero & Hero Card Runtime Execution
- Full Campaign End-to-End Integration (96 passed, 0 failed)
