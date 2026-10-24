# Ascendance Unity 0.8.8 — Dynamic Reaction Revalidation

Baseline: **0.8.7 Reaction Input Stability Fix**.

## Fixes

- Revalidates the complete pre-Attack Reaction option list after every resolved Reaction or target-changing Hero ability.
- Enforces printed `another Hero` / adjacent-Hero target relationships, so **Interpose** cannot remain legal after **Improved Shield Wall** changes the Attack target to Sir Aldric.
- Recomputes pending Attack damage with already-applied Armor bonuses and damage reductions before evaluating `would suffer damage` and lethal Reaction triggers.
- An Attack that has already been canceled no longer offers additional pre-Attack Reactions.
- Revalidates generalized `would suffer damage` options after each damage Reaction, including when pending damage reaches 0.
- Adventure Log now records `REACTION REVALIDATE — ...` after each state-changing Reaction so stale-option regressions are visible in logs.

## Test helpers

Editor Play Mode commands remain non-production test helpers and move existing physical cards rather than creating copies:

- **Ascendance → Validation → Test - Give Aldric Interpose (Play Mode)**
- **Ascendance → Validation → Test - Give Garruk Ignore the Pain (Play Mode)**

## Preserved

- 0.8.5 player-chosen Hero activation order.
- 0.8.6 generalized Hero-damage Reaction transaction.
- 0.8.7 stable clickable Reaction modal/input behavior.
- 16 authored Heroes / 323 Hero-card definitions / 30-card personal decks.
