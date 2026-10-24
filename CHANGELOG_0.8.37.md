# Ascendance Unity 0.8.37 — Hero-Owned Crafting & Profession Progression

Built from runtime-verified 0.8.36b.

## Ownership correction
- Crafting materials are **not party-shared**.
- Every Loot/Crafting Material remains in the individual Hero's `LootInventory`.
- A Hero can satisfy and consume a recipe only with materials that Hero personally carries.
- Other Heroes' inventories are never searched or consumed by crafting.
- Party Gold remains shared; this crafting release does not charge Gold for recipes.
- Eternal Forge Ember and Codex of Eternity must be carried by the Hero performing the gated craft and are not consumed.

## Crafting catalog
Adds 20 real recipes — 5 each for:
- Blacksmithing
- Alchemy
- Enchanting
- Artificing

Recipes use existing authored Ascendance loot/material and equipment IDs. Outputs enter the crafting Hero's persistent state: Equipment goes to unequipped gear, authored consumables go to that Hero's LootInventory, and crafted Healing Potions increase that Hero's existing usable Healing Potion counter.

## Profession progression
- Profession XP is tracked independently per Hero and per profession.
- Profession Levels 1–5 are derived from XP thresholds: 0 / 100 / 250 / 450 / 700 XP.
- Successful recipes grant profession XP based on recipe tier.
- Level-gated recipes remain locked until the crafting Hero reaches the requirement.
- Legendary Blacksmithing requires LT155 Eternal Forge Ember on the crafting Hero.
- Advanced Enchanting requires LT158 Codex of Eternity on the crafting Hero.

## UI
The Stonebrook Crafting tab now:
- clearly identifies the selected crafting Hero;
- shows that Hero's material count and profession level;
- shows profession XP/progress;
- lists 5 recipes for the selected profession;
- displays ingredients, level/unlock requirements, READY/MISSING/LOCKED state, and CRAFT controls.

## Persistence
Per-Hero Blacksmithing, Alchemy, Enchanting, and Artificing XP now save/load alongside that Hero's already-persistent individual LootInventory and acquired Equipment.

## Validation
Use:
`Ascendance → Validation → Test 0.8.37 - Hero-Owned Crafting & Professions (Play Mode)`

Expected final line:
`[ASCENDANCE][0.8.37 TEST] PASS — Hero-Owned Crafting & Profession Progression completed: 28 passed, 0 failed.`
