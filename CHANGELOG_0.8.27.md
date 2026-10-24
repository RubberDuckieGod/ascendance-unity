# Ascendance Unity 0.8.27 — Search Rewards & Equipment Management

Built from runtime-verified 0.8.26a.

## Search reward migration
Real Loot/Equipment execution now covers SD001, SD003, SD004, SD007, SD008, SD009, SD011, SD012, SD014, SD017, SD018, and SD019.

Choice-heavy Search cards hold the exact removed deck cards until the player resolves the choice. Those held card IDs persist through save/resume.

- SD003: draw Loot, then discard a Hero card or lose 5 Gold.
- SD004: choose 20 Gold or 1 Loot.
- SD009: reveal one Equipment; Common/Uncommon may be kept, otherwise it is bottomed for 25 Gold.
- SD012: draw two Loot, keep one, discard one.
- SD017: choose 30 Gold or draw Equipment and keep it only if Common.
- SD019: draw two Equipment; keep one qualifying Common/Uncommon and bottom the other(s); if neither qualifies, gain 30 Gold.

SD005, SD006, and SD010 remain pending because they require the future Exploration preview/reorder runtime rather than Loot/Equipment systems.

## Equipment management
- Added player-facing GEAR control.
- Acquired Equipment remains unequipped until explicitly equipped.
- Equipped loadout persists through save/resume as physical Equipment IDs/copies.
- Replaced Equipment returns to inventory.
- Attack and Armor recalculate from Base stats plus current equipped loadout.
- Kael's Unarmed +2 occupies the Weapon slot only while no Equipment Weapon is equipped.
- Two-Handed / Dual-Wield Weapons force an equipped Shield back to inventory.
- A Shield cannot be equipped while the active Weapon occupies both hands.
- Accessory replacement is player-selected when capacity is full.

## Validation
Use:
Ascendance -> Validation -> Test 0.8.27 - Search Rewards & Equipment Management (Play Mode)
