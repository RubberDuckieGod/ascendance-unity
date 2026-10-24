# Ascendance Unity 0.8.54 — Full Loot & Equipment Runtime Execution

Built from runtime-verified 0.8.53 Full Travel Encounter Deck Execution.

## Scope
0.8.54 promotes the authored Loot and Equipment catalogs from reward/loadout data into live gameplay runtime paths while preserving the verified Search and Travel Encounter systems.

- 190 authored Loot definitions remain loaded.
- 144 authored Equipment definitions remain loaded.
- LT001–LT030 are the complete authored Consumable set and now route through a live Items runtime.
- Crafting Materials, Monster Parts, Trade Goods, Quest Items, Relics, and Legendary Treasures retain their campaign/crafting/trade roles; Stonebrook-relevant active/passive relic effects are wired into the shared runtime.
- Equipped base Attack/Armor continues to come from the existing loadout system, while Equipment ability hooks now participate in combat, damage prevention, healing, movement, card costs, Conditions, turn start, and player-activated legendary effects.

## Player-facing Items / Gear
- Added **ITEMS** to the main HUD beside **GEAR**.
- ITEMS exposes legal carried Consumables and player-activated Relic effects through the existing Reward Choice panel.
- GEAR keeps the existing equip/swap flow and additionally exposes supported player-activated Equipment abilities.
- Bow/Crossbow families now participate in the basic-Attack range gate as ranged weapons; explicit connected-Location reach text is also honored.

## Consumables
LT001–LT030 now have a runtime route, including healing/condition removal, direct-damage flasks and thrown weapons, Smoke Bomb movement permission, Caltrops/Hunting Trap pieces, Rope & Grapnel movement, Lockpick free Search setup, Scout's Kit Exploration choice, Whetstone attack setup, Emergency Supplies, and Phoenix Ash.

Traveler's Rations remains tied to the Rest resolver and is consumed when its Rest bonus resolves.

## Persistent Loot / Relics
Stonebrook-relevant hooks include carried Armor/Attack/damage passives, acquisition rewards, quest-start draw behavior, once-per-Scenario cleansing/healing/extra-turn effects, legendary treasure acquisition effects, and Phoenix Heart defeat replacement.

Quest-start Loot effects are idempotent across party setup/restart so the same Quest cannot grant the effect twice.

## Equipment runtime
0.8.54 adds shared hooks for:
- attack modifiers and Armor ignore;
- post-Armor bonus damage and on-hit/on-defeat triggers;
- incoming-damage reduction and defeat prevention;
- healing bonuses and post-heal triggers;
- movement/turn-start triggers;
- Arcane card Action-cost modifiers;
- Condition immunities;
- ranged weapon reach;
- selected legendary/active Equipment abilities through GEAR.

The runtime uses the existing equipped-loadout and persisted Hero state rather than introducing a new save schema.

## Regression preservation
- 0.8.52a Full Search Deck Execution remains the Search baseline.
- 0.8.53 Campaign I Travel Encounter boundary remains EN001–EN030.
- No save-format migration is required by the new 0.8.54 item state.

## Validation
Run in Play Mode:

**Ascendance → Validation → Test 0.8.54 - Full Loot & Equipment Runtime Execution (Play Mode)**

The validator checks authored catalog counts, runtime-role coverage, all 30 Consumables, Search/Travel regression boundaries, representative Consumable/Relic/Equipment mechanics, ranged Equipment reach, Items HUD eligibility, and absence of a Loot/Equipment `CONTENT PENDING` fallback on the exercised path.


## 0.8.54a Compile Hotfix
- Fixed CS0136 local-name collision in RewardRuntime0854.
- Replaced deprecated FindFirstObjectByType call in QuestRuntime0829 with FindAnyObjectByType.
- No gameplay-rule changes.
