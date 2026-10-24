# Ascendance Unity 0.8.40 — Downtime Equipment Management & Loadout Preparation

Built from runtime-verified 0.8.39.

## Added
- Dedicated LOADOUT tab inside Stonebrook Downtime.
- Free per-Hero loadout preparation that does not consume Work/Gather Activity.
- Explicit UNEQUIP support for equipped Weapons, Shields, Armor, and Accessories.
- Unequipped gear immediately returns to the owning Hero's personal Equipment inventory and becomes tradeable.
- Inventory gear comparison text showing projected total Attack/Armor against the active matching slot.
- Existing hand/slot rules retained: two-handed Weapons force Shields off; Shields cannot equip while a two-handed Weapon is active; Accessory capacity remains Hero-specific.
- Explicit Accessory replacement choice when slots are full.
- Dedicated Play Mode validator covering equipment ownership, stat recalculation, Trading integration, Activity independence, save/load, UI, and next-Quest continuity.

## Preserved
- Gold remains party-owned.
- Materials and Equipment remain Hero-owned.
- Crafting and Trading remain unlimited during Downtime.
- Work/Gather remains one Activity per Hero per Downtime.
- Campaign, Quest, crafting, trading, and save schema rules are unchanged.
