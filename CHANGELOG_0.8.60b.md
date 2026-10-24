# Ascendance Unity 0.8.60b — Enemy-Entry Reaction Validator Graph Hotfix

- Built from 0.8.60a after Unity completed 82 passed / 3 failed.
- No Hero-card rules were weakened or broadened.
- Fixes the validator setup for Rowan **Overwatch** and Lyra **Vanish Without a Trace**.
- The 0.8.60a validator had placed those Heroes on catalog-only Crownlands Locations while Q001 was active; those debug-materialized Locations are intentionally not connected into the live Stonebrook graph.
- The validator now selects a real connected, revealed, Quest-enterable Location pair from the active runtime map.
- Overwatch now validates the granted basic Attack, discard, and 1 held-Action Reaction cost on a legitimate graph target.
- Vanish now validates Stealth/untargetability, connected escape, discard, and 2 held-Action Reaction cost on a legitimate graph destination.
- Validator remains 85 checks.
