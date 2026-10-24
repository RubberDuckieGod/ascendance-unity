# Ascendance Unity 0.8.11 — True Hero Turn Start Lifecycle

## Why this build exists

0.8.5 correctly removed a fixed Hero activation order and allowed players to choose which unfinished Hero acts next. However, the round transition still granted Actions and drew cards for the whole party before any specific Hero actually began a turn.

That split authority made the runtime vulnerable to future timing bugs: a Hero could hold turn resources before a turn existed, per-turn flags were reset at the round boundary instead of the actual turn boundary, and a normal action could implicitly claim turn ownership.

0.8.11 moves those responsibilities to one explicit Hero-turn start transaction.

## Canonical Hero Phase lifecycle

For each unfinished, non-Defeated Hero:

1. No Hero owns the turn between activations (`TurnHeroId = null`).
2. Player selects any eligible Hero.
3. Player presses **START TURN**.
4. The selected Hero becomes `TurnHero`.
5. Turn-only runtime flags reset.
6. **Draw Step**:
   - Round 1: keep the existing 5-card opening hand; do not draw again.
   - Round 2+: draw exactly `Hero.DrawValue`.
7. **Action Step**: gain exactly 3 Actions.
8. Perform normal Hero actions/cards/movement.
9. Press **END TURN**.
10. The Hero is marked complete for this Hero Phase.
11. Unused general Actions remain available only for valid Reactions for the rest of the Round.
12. `TurnHeroId` returns to null and players choose the next unfinished Hero.
13. Once every eligible Hero has completed a turn, World Phase resolves.
14. End Phase clears all held Actions.
15. The next Hero Phase begins with no selected turn owner and all unfinished Heroes at 0 Actions.

## Authority rules

`TurnHeroId` is the only normal-turn owner.

Selecting a Hero is inspection/choice only. It does **not** begin a turn.

Normal actions require:

- participating Hero,
- Hero is not Downed/Dead,
- Hero has not already completed a turn this Hero Phase,
- Hero is the current `TurnHero`,
- sufficient legal Actions,
- no blocking movement/reaction/world-resolution state.

A normal action no longer auto-claims the turn. If no turn is active, `RequireHeroTurn(...)` logs that the player must press Start Turn.

## Round 1 opening hand

Every Hero's physical 30-card deck is built and shuffled during game initialization, and every Hero receives a 5-card starting hand. Therefore the first Hero turn in Round 1 does not perform another normal Draw Step.

This prevents a 5-card opening hand from becoming 6 before that Hero has taken a single action.

## Round 2+ drawing

The old round-wide draw loop has been removed from `ResolveRound()`.

From Round 2 forward, a Hero draws only when their own turn starts. This means player-chosen activation order remains meaningful and no Hero receives start-of-turn resources early.

## Saved Actions / Reactions

Ending a turn does not zero unused general Actions. They remain on the completed Hero so valid Reaction cards can still spend them later in the Round.

End Phase clears those held Actions before the next Round begins.

Attack-card-only temporary Actions are still stripped at End Turn and cannot become general Reaction Actions.

## Save/load behavior

The save already records:

- `TurnHeroId`,
- each Hero's Actions,
- `TurnCompleted`,
- hand/deck/discard,
- per-turn/per-round ability usage.

0.8.11 uses that state as follows:

- If a save contains a valid in-progress `TurnHeroId`, that Hero resumes with the exact saved hand and Actions. The Start Turn lifecycle is **not replayed**.
- If the game loads between Hero turns, unfinished Heroes are normalized to 0 Actions because their next Draw + 3-Action grant has not happened yet.
- Heroes that already ended their turn retain saved Actions for legal Reactions until End Phase.
- Older 0.8.10 saves that stored round-granted Actions on unfinished Heroes are therefore migrated safely by zeroing those not currently taking a turn.

## Play Mode validation helper

Run this between Hero turns with at least two unfinished Heroes:

**Ascendance → Validation → Test 0.8.11 - Hero Turn Start Lifecycle (Play Mode)**

The helper validates:

- unstarted Heroes have 0 Actions,
- normal Actions are unavailable before Start Turn,
- explicit Start Turn assigns `TurnHeroId`,
- exactly 3 Actions are granted,
- Round 1 does not duplicate the opening draw,
- Round 2+ draws only that Hero's `DrawValue`,
- a second Hero cannot steal an in-progress turn,
- End Turn preserves unused general Actions,
- completed Heroes cannot take a second normal turn,
- the next Hero can be chosen freely and receives their own independent turn-start resources.

The helper intentionally leaves the second Hero's turn in progress so the resulting state can be inspected in the HUD and log.

## What 0.8.11 deliberately does not change

- Enemy Attack timing from 0.8.10 remains unchanged.
- 0.8.9 Interpose retargeting remains unchanged.
- Reaction input/revalidation from 0.8.7/0.8.8 remains unchanged.
- Full phase-aware Status expiration is still a separate migration boundary; this build moves per-turn Hero runtime resets to the correct turn-start boundary but does not redesign all Status duration semantics.
- Online dedicated-server authority is a later networking layer. This build establishes the correct single-player/core turn state machine first.
