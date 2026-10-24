# Ascendance Unity 0.8.38 — Hero-to-Hero Trading & Material Transfer

Built from runtime-verified 0.8.37.

## Added
- Dedicated Stonebrook Downtime TRADE tab.
- Hero-to-Hero transfer of personal crafting materials and other non-Quest Loot.
- Single-item and full-stack material/Loot transfer.
- Unequipped Equipment transfer between participating Heroes.
- Healing Potion transfer between participating Heroes.
- Explicit recipient selection and transfer summaries.
- Runtime validation menu: Ascendance → Validation → Test 0.8.38 - Hero Trading & Material Transfer (Play Mode).

## Ownership rules
- Materials remain owned by the Hero carrying them.
- Quest Items cannot be voluntarily traded.
- Equipped Equipment cannot be traded; it must first return to unequipped inventory.
- Profession XP never transfers and is never awarded by trading.
- Gold is party-owned and is not part of Hero-to-Hero trading.
- Trading is available only during verified between-Quest Stonebrook Downtime.

## Persistence
No parallel trade inventory was added. Transfers mutate the existing per-Hero LootInventory, AcquiredEquipment, and HealingPotions state, so the existing campaign save format persists ownership naturally.
