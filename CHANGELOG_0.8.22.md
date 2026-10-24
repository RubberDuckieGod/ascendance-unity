# Changelog — 0.8.22 Encounter Hazards & Persistent Objects

Built from runtime-verified 0.8.21.

## Added
- Persistent `EncounterObjectState` runtime model.
- Save/load for Encounter objects, object serials, and encounter-owned object IDs.
- Runtime world presentation through `EncounterObjectAvatar`.
- HUD `Encounter Object` action and hazard summary.
- Blocking-object participation in Travel Encounter completion.
- EN006 Snare placement and on-entry trigger/removal.
- EN007 Fire Token placement, 1-Action extinguish interaction, remaining-Fire end consequence, and conditional Item bonus gating.
- EN012 Spider Web blocking objective, Rooted application, 1-Action clear, and Rooted removal.
- Shared object interaction/removal helpers prepared for later encounter/quest objects.

## Preserved
- 0.8.21 exact encounter ownership and delayed reward resolution.
- Search and Travel Encounter deck state.
- Location state, turn lifecycle, status timing, Downed/Dead rules, post-quest exploration, and reaction reserve rules.

## Deliberately still pending
- EN018 Shamanic Totem removal mechanics. The authored encounter defines its effect while the Totem remains but does not provide a concrete Action cost or Health value for destroying it, so 0.8.22 does not invent one.
- Loot, Equipment, Item, Reputation, Merchant, escort, and broader campaign reward systems.
