# Ascendance Unity 0.8.18 — Location Tags & Location-State Enforcement

Baseline: runtime-verified 0.8.17a.

## Implemented
- Preserves the Unity vertical-slice graph IDs for save/test compatibility while binding current Locations by name to the v10.52 authored Location metadata.
- Runtime Location state now carries authored ID, Terrain, Traits, Keywords, Reveal Effect, First Visit Effect, Ongoing Effect, Reveal-resolved state, First-Visit-resolved state, Search state, and temporary Location tags.
- Printed Terrain / Traits / Keywords and temporary tags are queryable through the same Location tag API.
- Temporary Arcane / Consecrated state persists in campaign saves with source and expiry timing.
- Reveal Effect and First Visit are separate lifecycle events. Explore resolves Reveal immediately; entering resolves First Visit when eligible.
- First Visit remains pending when an Enemy is present, preserving the locked Ascendance First Visit rule.
- Difficult Terrain follows the v3.1 movement rule: no entry surcharge; voluntary exit requires 2 Move Actions. A free Move Action can satisfy one requirement. Pathfinder / Sure Footing style effects can ignore the additional requirement.
- Arcane Bolt uses actual Arcane Location state for its +1 Attack.
- Meteor Shard now checks actual Arcane Location state before applying Burning.
- Leyline Convergence creates a source-timed Arcane Location tag; Arcane Cataclysm creates persistent Arcane Location state for the chosen Locations.
- Consecrated is real Location state and grants +1 additional Health to each Hero's first Health recovery in that Location each Round.
- Card-driven movement now also participates in First Visit entry timing.

## Deliberately not claimed complete yet
- Full Search deck resolution / Search-effect UI.
- Full Travel Encounter resolution for First Visit.
- Every v10.52 Location's unique Ongoing Effect (236 Locations are authored; this build establishes the state/runtime foundation and current Stonebrook slice metadata).
- Full Cover / High Ground / every environmental combat modifier.
- Generalized dynamic Exploration Deck placement/rotation/Can Connect To logic beyond the existing Unity vertical slice.

## Validation
Unity menu:
`Ascendance > Validation > Test 0.8.18 - Location Tags & State (Play Mode)`

Run at the beginning of a Hero Phase with at least 2 Heroes, at least 1 living Enemy, and no active turn/movement/reaction window.
