# Ascendance Unity 0.8.21 — Encounter Lifecycle & Reward Resolution

Built from the runtime-verified 0.8.20a baseline.

## Added
- Explicit Travel Encounter instances with unique IDs.
- Exact ownership of dynamically spawned enemies per Encounter.
- Active Encounter HUD state and remaining-enemy count.
- Encounter completion only after all enemies spawned by that Encounter are Defeated.
- Deferred victory reward resolution.
- Immediate Gold rewards for supported authored Campaign-I Encounter rewards.
- EN001 Elite bonus (+5 Gold per defeated Elite) based on authored enemy classification.
- EN006 conditional 15 Gold reward only when its E012 is actually Defeated.
- Active Encounter state, ownership, serials, and completion counters persist through save/load.

## Scope boundary
Loot, Equipment, Reputation, Merchant access, Escort choices, Encounter-control choices, Fire/Snare objective tokens, and other systems not yet migrated remain explicit Pending Content. They are not silently awarded or invented.
