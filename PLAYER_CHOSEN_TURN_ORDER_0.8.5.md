# Ascendance Unity 0.8.5 — Player-Chosen Hero Turn Order Test

## Rules target

The Hero Phase has no preassigned Active Hero.

Players choose the Hero activation order each Round. Each chosen Hero completes one whole turn before another Hero begins. After the last eligible Hero finishes, the World Phase starts.

## Live test

1. Start a new 4-Hero game.
2. Confirm no Hero is marked `ACTIVE`.
3. The HUD should say `CHOOSE TURN` while no Hero turn is in progress.
4. Click any unfinished Hero, such as Asha. Merely viewing Asha should not force her to take the turn yet.
5. Take one legal normal action or play one legal non-Reaction card with Asha.
6. Confirm Asha is now marked `TURN`, not `ACTIVE`.
7. Click another unfinished Hero, such as Garruk.
8. Confirm Garruk can be inspected but his normal action/card controls are unavailable while Asha's turn is in progress.
9. Return to Asha and End Turn.
10. Confirm Asha is marked `DONE`.
11. Confirm no different Hero is automatically made next. The HUD returns to `CHOOSE TURN`.
12. Select either Garruk, Kael, or Mordren in any order and begin that Hero's turn.
13. Repeat until all four Heroes have completed a turn.
14. Confirm the World Phase begins only after the fourth Hero finishes.
15. During a valid Reaction window, verify a different Hero with saved Actions may still perform a legal Reaction.
16. Begin a Hero turn, save mid-turn, Continue, and confirm that same in-progress turn resumes.
17. Save between Hero turns, Continue, and confirm no Hero is forced as the next turn.

## Expected log examples

Before a turn starts:
- `HERO PHASE — Round 1. Players choose which unfinished Hero takes the next turn.`
- `Selected Asha Spiritwalker; this Hero may take the next turn.`

When the first normal action/card is committed:
- `TURN START — Asha Spiritwalker. Hero activation order is player-chosen this Round.`

While inspecting another Hero:
- `VIEW — Garruk Stoneblood; Asha Spiritwalker's turn is still in progress.`

After ending:
- `TURN END — Asha Spiritwalker ends their turn ...`
- `HERO PHASE — choose the next Hero to take a turn (3 remaining).`

No log should say a Hero is the `active Hero`.
