# Full Enemy Runtime — 0.8.59

## Runtime ownership

0.8.59 turns the complete authored **E001-E125** enemy catalog into live runtime behavior on top of the verified Quest, World Event, Location, Encounter, Loot/Equipment, Search, and Crafting layers.

Shared mechanics are implemented as reusable runtime systems rather than one script per Enemy. Unique bosses and named enemies use narrow ID-specific clauses only where their authored rules require them.

## Activation lifecycle

The generalized Enemy lifecycle now supports:

1. Enemy Phase start effects and phase target selection.
2. Authored Movement allowance and terrain exceptions.
3. Ranged repositioning / hold-position behavior.
4. Start-of-activation heals, buffs, summons, and target locks.
5. Replacement abilities and hazard placement when authored instead of a basic Attack.
6. Target priority based on the authored Behavior text.
7. Enemy attack resolution through the locked d6 damage formula.
8. Post-hit conditions, forced movement, debuffs, and protection stripping.
9. End-of-activation summons/retreat behavior.
10. End-of-Enemy-Phase delayed hazard resolution.

## Damage and objective bindings

The runtime supports authored first-hit defenses, objective-dependent Armor, Fire immunity, retaliation/reflection, and Health floors. Later-campaign bosses can therefore remain bound to their Quest objectives, including Mountain Wards, Solar Seals, Mirror Wards, Eclipse Anchors, and Solar Crown Wards.

Q012's existing specialized Goblin King protections remain authoritative and are not replaced by the generalized layer.

## Enemy hazards

Enemy-created hazards are real state objects with source Enemy, Location, creation Round, and optional trigger Round. They currently cover Snare, Fire, Barricade, Web Zone, Flood, Cave-In, Charge, and Shard-style effects used by authored enemies.

## Persistence

Campaign saves now retain:
- Enemy runtime once/round keys,
- movement already taken during the current activation,
- focused/marked Hero ID,
- active Enemy-created hazards and their trigger timing.

Older saves remain compatible because the new fields default safely when absent.

## Validation

Run in Play Mode:

**Ascendance → Validation → Test 0.8.59 - Full Enemy Runtime Execution (Play Mode)**

Expected:

`[ASCENDANCE][0.8.59 TEST] PASS — Full Enemy Runtime Execution completed: 64 passed, 0 failed.`
