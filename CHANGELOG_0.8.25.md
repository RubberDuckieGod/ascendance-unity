# Ascendance Unity 0.8.25 — Encounter Deck Control & Remaining Campaign I Rules

Built from the runtime-verified 0.8.24 baseline.

## Added
- Real, player-facing Encounter deck-control modal backed by the live Encounter deck.
- EN003 Scout Patrol: 10 Gold + inspect top Encounter card and optionally bottom it.
- EN022 Watchtower Patrol: 25 Gold + inspect/reorder the top 2 Encounter cards.
- EN028 Hidden Trail: Severity 3–4 bottoms one of top 2; Severity 5–6 reorders top 3; Severity 7+ still skips the next Travel Encounter.
- Encounter deck-control state persists through campaign save/load and blocks Hero actions until resolved.
- EN009 Abandoned Goblin Camp Severity 7+: spawned E011 gains +1 Attack for its first activation only.
- EN027 Supply Wagon Wreck Severity 7+: mandatory additional Encounter resolves in the 3–4 Severity band. The Equipment reward remains explicitly pending.
- Fixed-severity chained Encounters do not consume a queued modifier intended for the next normal Severity roll.
- EN003 / EN026 Scout escape rules are no longer redundantly listed as CONTENT PENDING now that 0.8.24 executes them.

## Still intentionally pending
- Loot / Equipment / Merchant / Reputation systems and reward choices.
- Choice-driven encounter effects that require a dedicated general encounter-choice UI (for example EN010 supply-vs-damage, EN015 toll choice, EN020 choose-a-Hero cleanse).
- EN018 Totem early-destruction interaction cost because the authored workbook still does not define that cost.
- EN021 Severity 7 end-of-Round follow-up timing; this belongs with the generalized round/quest consequence framework rather than a synchronous card-resolution shortcut.
