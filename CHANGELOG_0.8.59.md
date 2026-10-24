# Ascendance Unity 0.8.59 — Full Enemy Runtime Execution

Built directly from the runtime-verified **0.8.58c — Full Location Runtime Execution** baseline.

## Added
- Catalog-driven runtime execution for all authored Enemies **E001-E125**.
- Authored Movement and Range for every Enemy template, including ranged repositioning and hold-position behavior.
- Target-priority behavior for strongest, lowest-Armor, lowest-Health, most-Actions, isolated, and marked targets.
- Formation, commander, pack, objective, low-Health, movement, terrain, and location-dependent Attack/Armor modifiers.
- First-hit damage reduction, Fire immunity, objective-bound Health floors, reflection/retaliation, and Immovable rules.
- Support and replacement activations including heals, hexes, roots, stuns, aura buffs, summons, forced movement, and multi-target attacks.
- Enemy-created Snare, Fire, Barricade, Web Zone, Flood, Cave-In, Charge, and Shard hazard state.
- Post-hit authored conditions including Poisoned, Bleeding, Knocked Down, healing reduction, movement surcharge, and protection removal.
- Save/resume for per-Enemy once keys, activation movement, focus target, and active Enemy hazards.
- Dedicated Play Mode validator covering catalog integrity and representative mechanics across all six campaigns.

## Integration safeguards
- Q001-Q012 specialized Quest logic remains authoritative where already implemented.
- Q012 Goblin King bespoke banner/Last Stand logic is preserved without applying the generalized boss aura twice.
- Existing World Event and Location Enemy modifiers remain layered with the new authored Enemy runtime.
- Hero attacks still automatically hit; Enemy attacks still roll 1d6.

## Validation
Enter Play Mode and run:

**Ascendance → Validation → Test 0.8.59 - Full Enemy Runtime Execution (Play Mode)**

Expected final line:

`[ASCENDANCE][0.8.59 TEST] PASS — Full Enemy Runtime Execution completed: 64 passed, 0 failed.`

0.8.59 remains a candidate until Unity reports 0 failed.
