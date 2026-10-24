# Ascendance Unity 0.8.61 — Full Campaign End-to-End Integration

Built from runtime-verified 0.8.60c.

## Campaign integration
- Runs the authored six-campaign saga as one continuous runtime: C001–C006, Q001–Q072, WE001–WE024.
- Fixes continuation Campaign Day initialization: Campaigns II–VI now enter at World Event Day 0 / Total Campaign Day 0 and become Day 1 only when their first Quest actually begins.
- Preserves Campaign I's existing Q001 Day-1 start.
- Archives each completed campaign with its 12 completed Quests, final outcome, campaign days, and campaign-specific title unlock.
- Preserves Hero progression, inventories, professions, Party Gold, and Reputation across campaign handoffs.
- Migrates legacy saves whose `AwaitingCampaignRuntime0845` flag was written when later campaigns were framework-only; now-playable campaigns automatically clear that stale lock on restore.

## Cross-campaign presentation
- Quest Results and Campaign End presentation are campaign-generic rather than Campaign-I-only.
- Campaign End uses authored campaign-specific victory/defeat titles.
- Downtime merchant/work text and the Downtime heading use the active campaign hub.
- Valewatch launchpad no longer says Q013+ runtime is unimplemented; locked Quests are described as campaign-progression locks.
- Continue summaries preserve legitimate Day-0 campaign handoff state.

## Validation
Play Mode menu:

`Ascendance → Validation → Test 0.8.61 - Full Campaign End-to-End Integration (Play Mode)`

The validator drives all 72 Quests through the live campaign router, advances all 24 World Events, exercises a Q013 fail/retry, validates all five campaign handoffs, checks archive/title state, performs save/load at each handoff, verifies legacy Campaign-II save migration, and saves/loads the final Campaign-VI victory.

Expected final line:

`[ASCENDANCE][0.8.61 TEST] PASS — Full Campaign End-to-End Integration completed: 96 passed, 0 failed.`
