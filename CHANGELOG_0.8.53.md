# Ascendance Unity 0.8.53 — Full Travel Encounter Deck Execution

Built from runtime-verified 0.8.52a1 Full Search Deck Execution.

## Scope
0.8.53 completes executable Campaign I Travel Encounter behavior for the Stonebrook deck while preserving the full authored 105-Encounter catalog for later campaigns.

- Stonebrook's live Travel deck remains EN001–EN030 only: 30 definitions / 35 physical copies.
- EN031–EN105 remain authored and catalogued, but cannot enter Campaign I's live Travel deck.
- All EN001–EN030 now report an executable runtime path.
- Existing 0.8.21–0.8.26 encounter combat, hazards, objects, initiative, deck control, rewards, and movement rules remain authoritative.

## Newly completed authored behavior
- EN004 Wolf Pack — a defeated Hero discards one carried Consumable before recovery when available.
- EN008 Fleeing Refugees — failed accepted escort/rescue paths now apply the printed Reputation loss.
- EN010 Broken Bridge — player choices for per-Hero next-turn Action penalty vs damage and party Gold loss vs damage.
- EN011 Merchant Under Guard — post-combat merchant access with authored rarity gates and discounts; merchant benefits are lost if a listed Enemy leaves play undefeated.
- EN013 Boar Charge — Severity 3–4 grants each surviving Hero at the encounter Location one free connected Move after the fight.
- EN014 Bear Territory — low-severity signs-only band cleanly resolves as an avoided encounter.
- EN015 Goblin Toll Collectors — live Pay-or-Fight choice; paying resolves without combat, fighting spawns the authored enemies and awards the demanded Gold on victory.
- EN018 Shamanic Totem — first Hero Attack each Round suffers -1 Attack while the encounter remains active.
- EN019 Lost Child — Severity 3–4 creates a save-safe one-Action next-Quest search that grants +1 Reputation and 10 Gold; failed rescue combat applies -1 Reputation.
- EN020 Forest Shrine — authored healing/cleanse choices plus save-safe next-Quest Inspired carryover at Severity 7+.
- EN021 Flooded Crossing — rope/supplies discard choice and delayed extra Travel Encounter when E027 survives Round 1.
- EN029 Traveling Merchant — live buy/sell merchant session with authored rarity bands, free Item, and 25-Gold first-purchase discount.
- EN030 Quiet Road — player-selected free Rest, healing/cleanse bands, and Severity 7+ next-Encounter +1 modifier.
- EN006 low-severity and EN021 Severity 3–4 no longer fall through to legacy CONTENT PENDING after resolving their printed effects.

## Player-choice integration
Encounter choices are routed through the existing Reward Choice UI rather than auto-resolved. This includes bridge choices, tolls, merchant transactions, supply sacrifices, cleanse targets, and free-Rest targets.

## Validation
Run in Play Mode:

**Ascendance → Validation → Test 0.8.53 - Full Travel Encounter Deck Execution (Play Mode)**

The validator checks Campaign I deck boundaries, authored copy counts, Search/Hero regression baselines, and representative live execution for the newly completed encounter systems.
