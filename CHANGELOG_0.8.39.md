# Ascendance Unity 0.8.39 — Per-Hero Downtime Activities

Built from the runtime-verified 0.8.38 baseline.

## Locked downtime rules
- Each participating Hero receives exactly **one dedicated Downtime Activity per resolved-Quest downtime period**.
- **Crafting and Trading are unlimited during Downtime and do not consume the Activity.**
- Activity choice: **Work** or **Gather Resources**.
- **Work:** +25 shared Party Gold.
- **Gather Resources:** choose Blacksmithing, Alchemy, Enchanting, or Artificing and find **1–3 total matching Crafting Materials** from the live Loot Deck.
- A Gather can return **different material types**; the picker favors different types when the live deck has them.
- Gathered materials go only into that Hero's personal `LootInventory`.
- Gather grants no profession XP and does not change Party Gold.
- Quest Items, legendary unlock treasures, and non-Crafting-Material loot are excluded from Gather.
- Found cards are removed from the live Loot Deck, preserving the finite resource economy.
- Work/Gather usage and summary persist through save/load.
- The next resolved Quest creates a fresh Activity period for every Hero, including retry flows.

## UI
Stonebrook Downtime now includes an **ACTIVITY** tab showing the selected Hero's Activity state, Work button, profession selector, live gather pool, and Gather action.

## Validation
Run in Play Mode:
**Ascendance → Validation → Test 0.8.39 - Per-Hero Downtime Activities (Play Mode)**
