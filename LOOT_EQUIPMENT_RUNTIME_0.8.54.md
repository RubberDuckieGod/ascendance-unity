# Loot & Equipment Runtime — 0.8.54

## Runtime ownership
0.8.54 is additive. Existing verified systems remain authoritative for Hero cards, Search, Travel Encounters, Quests, crafting, merchants, and equipment loadout legality. This version provides a shared execution layer so Loot and Equipment can affect those systems without duplicating them.

## Loot categories
The authored Loot catalog contains 190 definitions across Consumables, Crafting Materials, Monster Parts, Trade Goods, Quest Items, Relics, and Legendary Treasures.

### Consumables
LT001–LT030 are actively usable or event-triggered. The ITEMS panel only presents legal choices for the selected Hero and current state. Targeted Items generate Hero, Enemy, Location, or Encounter-object choices rather than silently selecting a target.

LT012 Traveler's Rations is intentionally not a manual ITEMS activation: it is consumed by the Rest resolver when its +1 recovery applies.

### Non-consumable Loot
Crafting Materials, Monster Parts, Trade Goods, and Quest Items remain persistent inventory resources for their authored systems. Relics and Legendary Treasures can additionally register passive, acquisition, quest-start, defeat-replacement, or player-activated effects.

## Equipment
Base Equipment Attack/Armor remains calculated by the existing loadout runtime. 0.8.54 adds ability hooks around the canonical attack/damage/heal/move/card pipelines rather than creating a second combat resolver.

Player-activated Equipment appears inside GEAR. Automatic Equipment effects resolve at their authored trigger where the current digital rules already expose that trigger.

## Range
The authored Equipment catalog currently stores most Bow/Crossbow reach in family/ability text rather than a numeric Range value. 0.8.54 therefore treats equipped Bow/Crossbow weapons as connected-Location ranged weapons for the basic-Attack gate, preserves any authored numeric range if present, honors explicit connected-Location weapon text, and gives EQ052 Bow of the World Tree its additional Range.

## Save compatibility
0.8.54 stores usage in existing persisted collections (`LootInventory`, equipped loadout, `UsedRoundAbilities`, `UsedTurnAbilities`, and `UsedQuestCards`). No new save-data class or migration is required.

## Validation target
The Play Mode validator is intended to catch integration regressions, not replace manual playtesting of every combinatorial item interaction. A clean baseline result should end with:

`[ASCENDANCE][0.8.54 TEST] PASS — Full Loot & Equipment Runtime Execution completed: <count> passed, 0 failed.`
