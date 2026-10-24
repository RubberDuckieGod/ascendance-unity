# Ascendance Unity 0.8.23 — Campaign I Encounter Consequences & Travel Control

Built from runtime-verified 0.8.22b.

## Implemented
- Persistent next-Travel-Encounter Severity modifier state, saved/restored.
- Persistent one-use Skip Next Travel Encounter state, saved/restored.
- EN030 Severity 7+ now queues +1 Severity for the next resolved Travel Encounter and consumes it exactly once.
- EN028 Severity 7+ now skips exactly one future Travel Encounter without drawing a card.
- EN026 victory now cancels any queued next-Encounter Severity modifier.
- EN002 Severity 5+ now deals its authored pre-setup 1 damage to every participating Hero, not only Heroes on the triggering tile.
- EN013 Severity 5+ pre-combat damage resolves against the first living Hero in party activation order before enemy setup.
- EN010 Severity 7+ applies Slowed to every participating Hero while spawning its authored crossing enemies.
- EN021 Severity 3–4 performs the authored per-Hero d6 hazard roll; Severity 7+ applies Slowed to every participating Hero while spawning E027.
- EN024 now creates persistent Fire Tokens through the shared 0.8.22 object framework. Remaining Fire at encounter end costs 5 Party Gold each; extinguished Fire does not. Victory awards 30 Gold, and the Reputation remainder is retained only if all Fire Tokens were removed.

## Intentionally still pending
Rules requiring systems not yet migrated remain explicit CONTENT PENDING rather than simulated: Merchant/Equipment choices, Reputation award application, escort choices, actual Enemy movement bonuses (EN016/EN025), encounter initiative overrides (EN023), Scout escape/leaves-play triggers, and card-reorder choices that require a dedicated player-choice UI.
