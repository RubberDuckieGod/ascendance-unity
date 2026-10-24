# Full Travel Encounter Deck Execution — 0.8.53

## Campaign boundary
Campaign I / Stonebrook uses EN001–EN030 only. The full authored catalog contains EN001–EN105, but EN031–EN105 are reserved for later regional campaigns and are intentionally excluded from the Stonebrook Travel deck.

Campaign I live deck:
- 30 unique Encounter definitions
- 35 physical Encounter cards after authored copy counts

## Runtime ownership
0.8.53 is an additive execution layer. Earlier verified encounter systems remain authoritative for mechanics they already own:
- 0.8.21 Encounter lifecycle and held rewards
- 0.8.22 persistent Encounter hazards/objects
- 0.8.23 consequences and Travel modifiers
- 0.8.24 enemy movement/initiative
- 0.8.25 Encounter deck control
- 0.8.26 tangible rewards / Reputation

The 0.8.53 layer fills authored gaps without replacing those systems.

## Save-safe carryover
Lost Child and Forest Shrine use the already-persisted Hero retained-effect list for cross-Quest state. Flooded Crossing's delayed follow-up and Shamanic Totem's per-Round attack penalty are stored on the active Encounter state, which already persists through campaign saves.

## Free Rest representation
The current digital runtime does not otherwise expose a standalone Rest action. For EN030's explicit free Rest, 0.8.53 resolves the baseline recovery directly and applies currently authored Rest bonuses from equipped/carried content.

## Validation target
The 0.8.53 Play Mode validator verifies:
- build version and authored catalog counts;
- exact Stonebrook EN001–EN030 boundary;
- 35-card physical Campaign I Travel deck;
- executable path for every EN001–EN030;
- 0.8.52a Search and 0.8.51 Hero-content regression baselines;
- representative authored choices and carryover behavior;
- no Campaign I CONTENT PENDING fallback on the exercised 0.8.53 paths.
