# 0.8.55 Full Crafting Runtime

The authoritative authored source remains `Assets/Resources/Ascendance/Data/full_content_0850.json`.

0.8.55 converts all CR001–CR200 rows into runtime recipes at load time. It does not replace or rewrite the authored rows. The adapter parses required LT/EQ ingredient IDs and counts, maps Tier I–V into the existing Level 1–5 profession progression, preserves the authored specialty name, and routes outputs according to the authored `resultId`.

## Four player progression tracks
The game continues to expose Blacksmithing, Alchemy, Enchanting, and Artificing as the player profession tracks. Additional authored specialty labels are preserved and grouped as follows:

- **Blacksmithing:** Blacksmithing, Woodworking, Leatherworking, Tailoring, Jewelry, Trophy Crafting.
- **Alchemy:** Alchemy.
- **Enchanting:** Enchanting, Sacred Crafting, Artifact Restoration, Legendary Crafting.
- **Artificing:** Tinkering, Campcraft, Settlement Crafting.

This keeps the previously locked progression/save model while allowing every authored specialty recipe to execute.

## Special outputs
Some authored recipes intentionally output scenario/campaign concepts instead of an LT### or EQ### card. 0.8.55 persists those results as `recipeId|resultId|resultName|heroId` records. This gives Quest and Campaign runtimes a durable, explicit hook without inventing a mechanical bonus not stated in the authored recipe row.
