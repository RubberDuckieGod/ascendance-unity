# Ascendance Unity 0.8.24 — Encounter Enemy Movement & Initiative Rules

## Implemented
- Base Enemy movement runtime: 1 Movement per activation when no Hero is in the Enemy's current Location.
- Enemies path deterministically through revealed connected Locations toward the nearest living participating Hero.
- Enemy Difficult Terrain rule: leaving Difficult Terrain costs 2 Movement.
- Forest Path / Fresh Tracks `-1 Movement` is now mechanically enforced.
- Encounter `+1 Movement` is mechanically enforced and expires at End of Round.
- EN025 Beastmaster Hunt: Beast templates (E020–E029) gain +1 Movement in the Encounter's first Round; Goblin Beastmaster does not.
- EN016 War Drums:
  - Severity 3–4 queues +1 Movement for the next Goblin Encounter's first Round.
  - Severity 5–6 queues exactly one E008 Goblin War Drummer for the next Goblin Encounter instead of spawning it immediately.
  - Both queued states survive save/load and are consumed only by an actual Goblin Encounter.
- EN023 Wolf Rider Sweep Severity 5+: spawned Encounter Enemies perform an immediate priority activation unless at least one participating Hero is Hidden.
- Priority-activated Enemies are marked `ActivatedThisRound` and are skipped in the later World Phase to prevent double activation.
- `ActivatedThisRound` now survives save/load.
- EN003 / EN026 Scout non-defeat leave-play consequence is live through a shared removal path: +1 Severity to the next Travel Encounter.
- A Scout that leaves play without being Defeated is tracked separately, so normal victory rewards do not incorrectly resolve/cancel the failure modifier.

## Intentionally not invented
The authored Encounter cards specify the consequence if a Scout leaves play without being Defeated, but do not define an autonomous flee route or board-edge escape rule. 0.8.24 therefore enforces the consequence whenever a rule/effect removes a Scout from play without Defeating it; it does not invent a new Scout flee AI.

## Validation menu
`Ascendance → Validation → Test 0.8.24 - Encounter Enemy Movement & Initiative (Play Mode)`
