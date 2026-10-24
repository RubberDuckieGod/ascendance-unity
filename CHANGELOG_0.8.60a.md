# Ascendance Unity 0.8.60a — EnemyState Status Compile Hotfix

Built from the 0.8.60 Full Hero & Hero Card Runtime candidate.

## Fix
- Added `EnemyState.HasStatus(string)` using the same case-insensitive status lookup already used by `HeroState.HasStatus(string)`.
- Resolves all eight CS1061 compiler errors introduced by 0.8.60 Hero/card runtime calls that query Enemy statuses.
- No intended gameplay behavior changes beyond enabling the authored 0.8.60 Enemy-status checks to compile.

## Validation
Run in Play Mode:

**Ascendance → Validation → Test 0.8.60a - Full Hero & Hero Card Runtime Execution (Play Mode)**

Target: `85 passed, 0 failed`.
