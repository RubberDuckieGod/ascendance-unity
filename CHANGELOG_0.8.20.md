# Ascendance Unity 0.8.20 — Search & Travel Encounter Runtime

Built from the runtime-verified 0.8.19a Stonebrook Location Effect Execution baseline.

## Search runtime
- Replaces the 0.8.19 Search hook-only behavior with a real draw pile / discard pile runtime.
- Migrates the first authored Search batch, SD001–SD020, including physical copy counts and Terrain eligibility.
- Search remains a separate Hero Action (normally 1 Action, once per Location); the stale `Explore` label in the v10.52 Search sheet does not replace the verified Search rule.
- Immediately resolves supported effects such as Party Gold, Health recovery, harmful-Condition removal, and Search→Travel chaining.
- Effects that require not-yet-migrated Loot, Equipment, Merchant, Reputation, or broader choice systems are retained verbatim as `PendingContentInstruction` and logged as `CONTENT PENDING` instead of disappearing.

## Travel Encounter runtime
- Migrates the full Campaign I EN001–EN030 Travel Encounter deck with physical copy counts, Terrain filtering, and Severity bands.
- Severity uses the authored Campaign I formula: `1d6 + WE001 modifier (+1) + explicit card modifier`.
- Location Encounter Level is retained/logged but is not incorrectly added to the Severity roll.
- Combat results can spawn authored Campaign I E### templates into the triggering Location as real EnemyState objects.
- Encounter-spawned enemies receive unique runtime IDs while retaining their authored TemplateId.
- Runtime spawns request presentation repair from StonebrookWorldBuilder and use the authored TemplateId for production asset lookup/fallback presentation.
- Supported immediate effects include enemy setup, Gold, Health recovery, deterministic damage hooks, and EN006 next-turn Action loss.
- Unmigrated encounter choices/rewards/consequences are retained as explicit pending content.

## Persistence
Save data now includes:
- Party Gold.
- Search draw/discard order.
- Encounter draw/discard order.
- Last Search card.
- Last Encounter card and Severity.
- Pending content instructions.
- Encounter spawn serial.
- Next-turn Action penalties.
- Dynamic encounter enemy template/runtime state, including exact Max Health, Health, Attack, Armor, boss flag, Location, and statuses.

## UI
- Round header now shows Party Gold.
- Pending unmigrated content is surfaced in the Quest panel rather than silently ignored.

## Validation
- `Ascendance → Validation → Test 0.8.20 - Search & Travel Runtime (Play Mode)`
- Static verification: 378/378 checks passed before packaging.
- 0.8.19 regression differs only on expected old version/banner assertions.
- Runtime verification remains pending until executed in Unity.

## Intentional scope boundary
0.8.20 migrates SD001–SD020 and the complete Campaign I EN001–EN030 deck. SD021–SD100 and full Loot/Equipment/Merchant/Reputation choice resolution remain future migration work.
