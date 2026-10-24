# Ascendance Unity 0.8.22 — Encounter Hazards & Persistent Objects

Built from runtime-verified 0.8.21.

## Implemented
- Shared persistent Encounter Object state with world presentation and save/load.
- EN006 Trapper's Snare: Severity 3+ places authored Snare markers. A later Hero entering the Location triggers one Snare and becomes Rooted until end of turn; the marker is consumed. Snares do not block encounter victory.
- EN007 Burning Cart: Fire Tokens are persistent objects. Each can be extinguished for 1 Action. Remaining Fire Tokens deal 1 damage per token to each Hero when the encounter ends, then are removed. The 25 Gold victory reward resolves normally; the Item bonus remains Pending Content and is granted only when all Fire Tokens were removed before clear.
- EN012 Spider Webs: creates a blocking Web objective. The first Hero begins Rooted; clearing the Web costs 1 Action, removes that Rooted, and is required before the encounter can clear even at Severity 1-2 with no combatants.
- Generic object types/flags are ready for later rubble, barricades, totems, gates, and quest-specific objectives.

## Still pending
- EN018 Shamanic Totem destruction rules are not invented because the authored card does not yet give a concrete Action/Health removal cost. Its printed ongoing effect remains pending until that removal contract is explicit.
- Loot/Item/Equipment/merchant/reputation systems remain pending as before.
