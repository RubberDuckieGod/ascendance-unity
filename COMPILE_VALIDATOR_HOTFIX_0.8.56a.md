# Ascendance Unity 0.8.56a — Q072 Campaign VI World-State Hotfix

Built from the runtime-tested 0.8.56 candidate.

## Fix
- Q072 already completed correctly, recorded WE024 victory, stored `UNLOCK — Dawnbearers of Sunspire`, and stored `CAMPAIGN 6 COMPLETE`.
- The validator also checked for the authored wording `Campaign VI`, which was not preserved as a separate searchable world-state marker.
- 0.8.56a now preserves the authored campaign marker (`Campaign VI`) whenever a reward contains `complete Campaign <roman-or-number>`, while retaining the existing canonical campaign completion flags.
- Validator labels/build gate updated to 0.8.56a.

No Quest objective, combat, reward amount, campaign progression, or save schema was changed.

## Validation target
Because the representative-campaign check runs once for each of Campaigns III-VI, the validator executes 46 checks total.

Expected final line:
`[ASCENDANCE][0.8.56a TEST] PASS — Full Quest Runtime Execution completed: 46 passed, 0 failed.`
