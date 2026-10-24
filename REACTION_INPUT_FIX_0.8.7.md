# 0.8.7 Reaction Input Fix

## Root cause

During World Phase, the coroutine waiting for a reaction choice invoked `StateChanged` every frame. The HUD responds to `StateChanged` by rebuilding the reaction modal. Therefore every reaction Button was destroyed and replaced between mouse/touch press and release, preventing Unity's Button `onClick` event from completing reliably.

## Correction

The World Phase now publishes state once when a reaction window opens, then waits without rebuilding the HUD every frame. Further state refreshes occur only after an actual reaction choice, decline/resolve, or another genuine state change.

The generic Text elements used inside buttons also set `raycastTarget = false`, keeping the Button image as the stable click target.

## Regression test

1. Enter Play Mode with Garruk plus any other Heroes.
2. End Hero turns with at least one Hero retaining Actions.
3. When an Enemy Attack opens a Reaction Window, click one listed reaction.
4. The Adventure Log must immediately add `REACTION CHOICE — ...` and then the reaction's resolution entry.
5. The chosen card must leave Hand and its Action cost must be paid once.
6. Click **Resolve / Decline** to continue the Enemy Attack if additional reactions are not desired.

### Deterministic Ignore the Pain test

While in Play Mode, choose:

**Ascendance → Validation → Test - Give Garruk Ignore the Pain (Play Mode)**

The command relocates one existing `H012-013 Ignore the Pain` copy from Garruk's Deck/Discard to Hand. It is Editor-test-only and does not alter normal shuffle behavior.
