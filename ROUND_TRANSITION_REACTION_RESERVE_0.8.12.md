# Ascendance Unity 0.8.12 — Round Transition & Reaction Reserve Enforcement

## Purpose

0.8.11 established the correct Hero-turn start transaction. 0.8.12 closes the other half of that lifecycle: what happens to unused Actions after End Turn, how Reactions spend them during World/End Phase, and exactly when those reserves disappear before the next Round.

## Canonical Action lifecycle

For each Hero:

1. Before Start Turn: **0 Actions**.
2. Start Turn: Draw Step occurs for that Hero, then gain **3 Actions**.
3. During the normal turn: spend Actions normally.
4. End Turn: remove any attack-card-only temporary Actions; keep remaining **general Actions** as the Hero's Reaction reserve.
5. World Phase: legal Reactions may spend that reserve even though the Hero's normal turn is complete.
6. End Phase status effects: legal Reactions may still spend the reserve while End-Phase damage/effects resolve.
7. End Phase completes: all remaining held Actions expire.
8. New Hero Phase: every Hero is unfinished, has **0 Actions**, and no Hero owns the turn.
9. The next chosen Hero independently performs Draw Step + gains 3 Actions at Start Turn.

## Reaction payment authority

`CanPayReactionCost(...)` is now the Reaction-specific payment gate.

A Reaction card may be offered/paid only when:

- the reactor is participating,
- the reactor is not Downed/Dead,
- the card is actually a Reaction,
- the Hero has enough **general Actions** remaining.

This intentionally does not require the Hero to be the current `TurnHero`; completed Heroes must be able to react during another Hero's turn, World Phase, and End Phase. It also prevents temporary attack-only Action grants from being converted into Reaction currency.

## End Phase authority

All held-Action expiry now goes through `ResolveEndPhaseReactionReserves()` / `ResetHeroAfterEndPhase(...)`.

The order is deliberate:

**World Phase → End-Phase statuses/reactions → expire held Actions → reset round state → increment Round → new Hero Phase**.

The game logs unspent reserve expiry and enforces a clean next-Round state. `BeginNextHeroPhase()` also contains a defensive repair check so stale Actions or `TurnCompleted=true` cannot leak into the new Hero Phase.

## Draw authority

There is still no party-wide draw at Round transition.

- Round 1: each Hero already has the 5-card opening hand and receives no duplicate Start-Turn draw.
- Round 2+: only the Hero actually starting a turn draws `DrawValue`.
- Heroes who have not started yet remain at their previous hand size and 0 Actions.

## Play Mode validation

From a fresh Hero Phase using a party containing Garruk plus at least one other Hero, run:

**Ascendance → Validation → Test 0.8.12 - Round Transition + Reaction Reserve (Play Mode)**

The destructive validation scenario:

- keeps only the Goblin Captain alive for the test World Phase,
- isolates Garruk with the Captain,
- gives Garruk one physical copy of **Ignore the Pain**,
- completes every Hero's normal turn while banking 3 unused general Actions,
- verifies Garruk can spend 1 held Action on Ignore the Pain after the Enemy Attack roll,
- verifies the remaining Actions expire only after End Phase,
- verifies the Round increments exactly once,
- verifies every Hero begins the new Hero Phase at 0 Actions with no turn owner,
- verifies there is no Round-wide draw,
- starts Garruk and then another Hero and verifies each independently draws their Draw Value and receives exactly 3 fresh Actions.

The test intentionally changes combat state, so run it on a fresh Play Mode session rather than a campaign state you want to keep.

## Preserved systems

0.8.12 does not redesign:

- 0.8.10 Enemy Attack timing,
- 0.8.9 Interpose retargeting,
- 0.8.8 dynamic Reaction revalidation,
- 0.8.7 Reaction input stability,
- 0.8.11 explicit Start Turn lifecycle,
- full status-duration semantics beyond preserving Reaction reserves through End-Phase status resolution,
- future online/dedicated-server networking authority.
