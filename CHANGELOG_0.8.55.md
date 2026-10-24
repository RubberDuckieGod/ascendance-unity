# Ascendance Unity 0.8.55 — Full Crafting Recipe Runtime Execution

Built from runtime-verified 0.8.54a Full Loot & Equipment Runtime Execution.

## Full authored recipe catalog
- Promotes the complete v10.52 Crafting catalog into the live runtime: **CR001–CR200**.
- Preserves all 14 authored specialty labels while keeping the four already-locked player progression tracks:
  - Blacksmithing
  - Alchemy
  - Enchanting
  - Artificing
- Authored specialties are mapped to one of those four progression tracks rather than introducing fourteen new XP/save systems.
- Tier I–V maps to profession Level 1–5 and awards 40 / 60 / 80 / 100 / 120 profession XP on successful craft.

## Costs and ownership
- Crafting remains Hero-owned: only the selected Hero's carried Loot/materials and **unequipped** Equipment can satisfy ingredient costs.
- Equipment listed as an authored ingredient is consumed from that Hero's unequipped Equipment inventory.
- Authored Party Gold costs are now enforced and deducted on success.
- Unlock/catalyst items that are required but are not listed in `requiredMaterials` are checked but not consumed.
- Existing Downtime rule remains intact: Crafting/Trading do not consume the Hero's one Downtime Activity. Authored `craftTime` Actions are shown as recipe metadata for scenario/future use and are not charged as a separate Downtime action pool.

## Tools and unlocks
- Basic Stonebrook tools are live immediately where appropriate.
- Master, Sacred, Arcane, and Legendary tool tiers are gated by the mapped profession tier.
- Location tools (Fallen Watchtower, Merchant Caravan, Ancient Portal, Ancient Crypt) use live Quest/Location state.
- Authored `Own/Possess LT###`, Quest-state, completion, revealed-Location, and selected enemy-defeat requirements have live checks.
- Campaign-specific permissions that do not yet have a current Campaign I state hook are retained as named persistent unlock flags for later campaign runtimes rather than being silently auto-granted.

## Outputs
- `EQ###` results enter the crafting Hero's unequipped Equipment inventory.
- `LT###` results enter the Hero's Loot inventory and route through the 0.8.54 Loot-acquisition hooks.
- Quest constructions, campaign results, upgrades, authored generic consumables, and artifact-restoration results create persistent save-facing special Crafting result state so the output survives save/resume and can be consumed by Quest/Campaign systems.

## UI
- Stonebrook Crafting now reads from the full CR001–CR200 runtime instead of the 20-recipe 0.8.37 subset.
- The four profession tabs collectively expose all 200 recipes.
- Added five-recipes-per-page navigation so large profession catalogs remain usable.
- Recipe rows show authored ID, name, tier/specialty, ingredients, Party Gold, required tool, authored craft time, and READY/MISSING/LOCKED status.

## Persistence
0.8.55 saves/restores:
- crafted special-result records;
- campaign/tool crafting unlock flags;
- defeated Enemy template IDs used by recipe prerequisites.

Older saves remain loadable because all new fields default empty when absent.

## Regression preservation
- 0.8.54a Loot/Equipment catalogs and runtime remain intact.
- 0.8.53 Stonebrook Travel Encounter boundary remains EN001–EN030.
- The original 20-recipe 0.8.37 adapter remains in the project for backward compatibility with older validators/runtime references; the player-facing Crafting UI uses 0.8.55.

## Validation
Run in Play Mode:

**Ascendance → Validation → Test 0.8.55 - Full Crafting Recipe Runtime Execution (Play Mode)**

Expected final line:

`[ASCENDANCE][0.8.55 TEST] PASS — Full Crafting Recipe Runtime Execution completed: 30 passed, 0 failed.`
