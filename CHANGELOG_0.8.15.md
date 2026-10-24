# Ascendance Unity 0.8.15 — Status Duration Lifecycle

Built from the runtime-verified 0.8.14 Post-Quest Free Exploration baseline.

## What changed

- Replaced the old generic End-Phase status countdown with explicit status expiry clocks:
  - Start of target Hero turn
  - End of target Hero turn
  - Start of source Hero turn
  - End of source Hero turn
  - End of target activation
  - End of round
  - Persistent / until explicitly removed
- Card-authored duration text now drives status timing when the status sentence explicitly states a duration.
- Status-definition duration is used as the fallback when a card applies a Status without its own timing override.
- `Marked` can now follow source-turn, activation, or round timing depending on the card/effect that applied it.
- `Stunned` / `Frozen` Enemies lose their activation and activation-timed conditions expire at activation end.
- Persistent conditions such as `Rooted` are no longer deleted by a generic End-Phase decrement.
- `Regenerating` recovery now resolves at Hero turn start so temporary Regenerating effects can actually heal before end-of-turn expiry.
- Status timing metadata persists in campaign saves and old saves retain backward-compatible migration from the legacy counters.

## Important scope note

0.8.15 establishes the status **duration/expiry lifecycle**. It does not claim every one of the 102 authored Status effects has full mechanical behavior implemented yet. Effects not already wired into Unity remain future parity work, but their duration metadata can now be represented correctly.

## Validation

Run in Play Mode:

`Ascendance > Validation > Test 0.8.15 - Status Duration Lifecycle (Play Mode)`

The deterministic test checks target-turn expiry, source-turn expiry, Enemy activation expiry, round expiry, persistent Status survival, and authored Status-definition fallback.
