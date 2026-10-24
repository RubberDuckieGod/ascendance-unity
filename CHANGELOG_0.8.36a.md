# Ascendance Unity 0.8.36a — Downtime Save/Load Quest Routing Hotfix

Built from the runtime-tested 0.8.36 candidate after its validator reported 17 passed / 2 failed.

## Fix
- `ApplySaveData()` now calls the existing `CloseReactionWindow()` normalization before resuming play.
- Reaction windows are not serialized and saves are already blocked while a Reaction window is open, so an old in-memory reaction gate must never survive a load.
- This prevents an invisible stale `HasPendingReactionWindow` from blocking `CanBeginCampaignQuest0833()` after reloading a resolved Quest during Stonebrook downtime.
- No Campaign availability, Quest reward, Merchant, Crafting, recovery, or save payload rules were changed.

## Validation
`Ascendance > Validation > Test 0.8.36a - Downtime Save/Load Quest Routing (Play Mode)`

The validator retains every 0.8.36 downtime check and additionally:
1. primes a stale non-serialized Reaction gate before applying the downtime save;
2. verifies load clears that gate;
3. verifies Q002 is legal after reloading completed Q001;
4. starts Q002 through the authoritative Campaign router;
5. verifies the Downtime overlay closes once Q002 is active.

Expected final result: **22 passed, 0 failed.**
