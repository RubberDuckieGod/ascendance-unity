> **SUPERSEDED BY 0.8.5:** The preassigned `ActiveHeroId` model in this historical 0.8.4 document was an incorrect interpretation of the Hero Phase. 0.8.5 restores player-chosen Hero activation order each Round.

# Ascendance Unity 0.8.4 — Active Hero Turn Enforcement Test

## Regression reproduced from 0.8.3

In the 0.8.3 live session, multiple Heroes were selected and allowed to play cards/attack before the first official `TURN END` transition. The turn-state machine later advanced correctly, but action entry points were using `SelectedHero` instead of enforcing the active-turn Hero.

## 0.8.4 model

`SelectedHeroId` = the Hero currently being viewed/targeted by the UI.

`ActiveHeroId` = the only Hero allowed to take normal turn actions.

These values may differ. Selecting another Hero never transfers the turn.

## Live regression test

1. Start a new 4-Hero game.
2. Confirm the party panel marks one Hero `ACTIVE`.
3. With that Hero active, click another Hero.
4. Confirm the control panel says `VIEWING • <Active Hero> ACTIVE`.
5. Confirm Attack / Explore / Rescue / Extinguish / Potion / End Turn are disabled.
6. Confirm the viewed Hero's normal cards are disabled.
7. Click a world Location or press Space while viewing the inactive Hero; no Action may be spent and the Adventure Log should report `TURN LOCK` if the action entry point is reached.
8. Return to the active Hero and spend one Action or play a legal card.
9. End Turn.
10. Confirm the next unfinished Hero becomes `ACTIVE` and is automatically selected.
11. Finish all Hero turns. Confirm World Phase does not begin early and starts only after the final active Hero ends their turn.
12. During World Phase, verify legal Reactions can still be offered to Heroes who saved Actions earlier in the Round.
13. Save mid-Round while viewing a non-active Hero. Continue the save. Confirm the viewed Hero and active Hero remain distinct and the same active Hero retains turn authority.

## Expected log examples

- `TURN — Kael Windstep is active for Round 1.`
- `VIEW — Asha Spiritwalker; active turn remains Kael Windstep.`
- `TURN LOCK — Asha Spiritwalker cannot Attack; Kael Windstep is the active Hero.`
- `TURN END — Kael Windstep ends their turn ...`
- `TURN — Asha Spiritwalker is now active.`

## Totemic Presence sanity check

Do not treat Totemic Presence appearing on another Hero as a turn leak. If that Hero is in Asha's Spirit Totem Location and it is their first Attack of the Round, the +1 Attack is intended.
