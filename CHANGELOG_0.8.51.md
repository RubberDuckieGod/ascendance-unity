# Ascendance Unity 0.8.51 — Effect-Granted Search & Explore Exceptions

Built from the 0.8.50 Full Authored Content + Playtest Rules candidate.

## Rules
- Normal player-initiated Search remains blocked while Engaged and costs 1 Action.
- Normal player-initiated Explore keeps its normal engagement, turn, and Action restrictions.
- An explicit Hero card, Item/Equipment ability, Location effect, Quest effect, Encounter, or other game effect that grants Search or Explore bypasses the normal action-button gate.
- Granted Search/Explore costs 0 Actions unless the granting effect itself says otherwise.
- Granted Search/Explore still requires a legal target/placement. Search remains once per Location unless an explicit effect implements a repeat-search exception.
- Ancient Ruins / Forgotten Remains free Search can resolve while Engaged.
- Rowan Scout Ahead now uses the shared granted-Explore resolver instead of a one-off reveal implementation.
- Granted Search/Explore activations are explicitly identified in the Adventure Log as EFFECT SEARCH / EFFECT EXPLORE.

## Validation
Run **Ascendance → Validation → Test 0.8.51 - Effect-Granted Search + Explore Exceptions (Play Mode)**.

### Action-cost clarification
- Playing a Hero card or activating an ability still pays that card/ability's normal printed/effective Action cost.
- The Search/Explore granted by that effect does not charge a second Action unless the effect explicitly says it does.
- Adventure Log wording now says `no additional Action` instead of `0 Actions` to make that distinction clear.
