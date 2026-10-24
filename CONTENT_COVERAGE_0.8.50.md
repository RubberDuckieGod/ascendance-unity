# Ascendance 0.8.50 Content Coverage

Source of truth: `Ascendance_v10.52_Location_Ongoing_Effect_Clarification.xlsx`.

## Loaded into the runtime catalog

| Content | Authored rows | 0.8.50 state |
|---|---:|---|
| Heroes | 16 | Existing live Hero database |
| Hero cards | 323 | Existing live personal decks |
| Loot | 190 | Live reward/Loot catalog |
| Physical Loot cards | 254 | Live Loot deck |
| Crafting Materials | 79 | Live Loot rows flagged as Crafting Material |
| Equipment | 144 | Live Equipment catalog/deck; ability text retained |
| Search cards | 100 | Live Search catalog; 104 physical cards |
| Encounters | 105 | All definitions loaded; active deck is region-scoped |
| Enemies | 125 | Live Enemy template catalog |
| Locations | 236 | Full authored definition catalog |
| Crafting recipes | 200 | Full authored recipe catalog |
| Quests | 72 | Full authored Quest catalog |
| World Events | 24 | Full authored World Event catalog |
| Status rows | 102 | Full authored catalog; existing status runtime retained |
| Keyword/reference rows | 65 | Full authored catalog |
| World Region framework rows | 13 | Full authored catalog |
| Faction Reputation framework rows | 58 | Full authored catalog |

## Important runtime boundary

0.8.50 makes the authored data available without pretending unfinished mechanics are complete. Campaign I's existing specialized Quest/Encounter/Location mechanics remain the executable vertical slice. Later-campaign rows are available to the runtime catalog for the next implementation milestones.

- Stonebrook Travel uses EN001–EN030 (35 physical cards). Later regional Encounter rows do not leak into Campaign I.
- The existing four digital profession systems retain their 20 currently executable crafting recipes while all 200 authored recipes are available through the full catalog. The remaining authored professions need their mechanics implemented rather than being silently mapped into the wrong profession.
- Campaign I preserves stable legacy runtime Location IDs for save compatibility. Those runtime tiles bind to v10.52 `AuthoredLocationId` metadata and Quest pools. All 236 Location definitions are in the catalog for later region/map runtime expansion.
- Q013–Q072 authored Quest rows are loaded as data. This does not bypass the authoritative `CanBeginCampaignQuest` gates; Q013 remains launch-locked until its scenario runtime is implemented.
