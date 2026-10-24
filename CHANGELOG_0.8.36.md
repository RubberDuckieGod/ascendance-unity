# Ascendance Unity 0.8.36 — Stonebrook Downtime Hub

Built from the runtime-verified 0.8.35a baseline.

## Added
- Runtime-created Stonebrook Downtime Hub available only between resolved Campaign I Quests.
- Party resource summary: Gold, General Reputation, Party Morale, and banked Supply.
- Per-Hero overview with Health, level, XP, Attack, Armor, potion, kills, equipped gear, Loot/material inventory, and unequipped Equipment.
- Downtime recovery: restores full Health, clears negative Conditions, clears Downed/Dead state, restores at least one Healing Potion, and grants no XP.
- Stonebrook Merchant: shows the top 3 cards of the real persistent Equipment Deck and purchases them at their authored catalog Value. Purchases spend party Gold, remove the exact card from the Equipment Deck, and add it to the chosen Hero's unequipped Equipment inventory. Campaign equipment-purchase lock is respected.
- Crafting entry point with Blacksmithing, Alchemy, Enchanting, and Artificing material hooks against the real persistent Loot inventory. No unauthored recipes are invented in this milestone.
- Existing equipment-management entry point available from downtime.
- Save Campaign and Choose Quest routes.
- Quest Results now returns to Stonebrook Downtime before opening the Campaign Hub when the campaign is still active.
- Campaign Hub exposes a Stonebrook button while downtime is available.
- HUD Campaign button becomes Stonebrook between Quests.
- Play Mode validator: `Ascendance/Validation/Test 0.8.36 - Stonebrook Downtime Hub (Play Mode)`.

## Persistence
No parallel downtime save format was added. Recovery, Gold, Merchant deck removal, acquired Equipment, Loot/material inventory, Morale, Supply, and Reputation all use state already owned by the real Ascendance campaign save system.

## Intentionally deferred
Authored crafting recipes and profession progression are not invented in 0.8.36. The four profession hooks and persistent material inventory are now ready for a later recipe-runtime milestone.
