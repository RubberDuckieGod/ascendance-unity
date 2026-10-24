# Ascendance Unity 0.8.35a — Idempotent Campaign Final-State Logging Hotfix

Built from the runtime-verified 0.8.35 Quest Results & Return-to-Campaign Flow baseline.

## Fix
- Q012 Campaign Victory and Campaign Defeat terminal log messages now fire only when the campaign actually transitions into that terminal state.
- Repeated `SyncCampaignProgress0833()` calls and UI/property refreshes no longer re-emit the same terminal Campaign message.
- Campaign state, Quest history, rewards, results UI, save/load, and Campaign Hub routing are unchanged.

## Validation
In Play Mode run:
`Ascendance > Validation > Test 0.8.35a - Final-State Logging (Play Mode)`

Expected final line:
`[ASCENDANCE][0.8.35a TEST] PASS — Idempotent Campaign final-state logging completed: 6 passed, 0 failed.`
