# 0.8.20 Search & Travel Encounter Contract

1. Search is a separate Hero Action, normally costing 1 Action and limited to one successful Search per Location per Scenario.
2. Reveal-granted free Search (for example Ancient Ruins / Forgotten Remains) uses the same Search deck runtime without spending an Action.
3. Search draws only cards whose authored Terrain eligibility matches the searched Location.
4. Resolved Search cards enter the Search discard pile; draw/discard order persists through save/load.
5. First Visit Travel Encounters and Search-card-triggered Travel Encounters use the same Campaign I Encounter runtime.
6. Campaign I Severity is 1d6 + the printed WE001 +1 modifier + any explicit card modifier. Location Encounter Level is not itself a Severity modifier.
7. Encounter cards use their printed 1–2 / 3–4 / 5–6 / 7+ band.
8. Encounter combat setup creates real EnemyState objects from authored E### templates at the triggering Location.
9. Encounter-spawned enemies participate in normal combat/world rules and persist through save/load.
10. Effects that depend on systems not yet migrated are never silently dropped; their exact remaining instruction is stored/logged as pending content.
