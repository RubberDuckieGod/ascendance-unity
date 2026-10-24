# Ascendance Unity 0.8.6 — Damage Reaction Pipeline Test

## Rules target

A Reaction whose printed Trigger is that a Hero **would suffer damage** must be offered before that damage changes Health, provided the reacting Hero can pay the Action cost and satisfies the printed target/range requirements. Saved Actions remain the Reaction resource.

0.8.6 extends that timing beyond Enemy Attacks to migrated Hero-card self-damage and Condition/direct damage.

## Recommended live regression

1. Start from the confirmed 0.8.5 turn-order flow and verify `CHOOSE TURN` still appears between Hero turns.
2. Put **Garruk** in the party and get **Ignore the Pain** (`H012-013`) into his hand.
3. Play **Feed the Rage** or **Reckless Swing** with enough Actions left to pay the Reaction.
4. Before the self-damage changes Garruk's Health, confirm a damage Reaction window opens and **Ignore the Pain** is offered.
5. Use Ignore the Pain. Confirm the pending 1 damage becomes 0, the Reaction card is discarded, its Action is spent, and the rest of the triggering Hero card continues resolving.
6. With Garruk at 1 Health and **Too Angry to Fall** (`H012-014`) available, trigger migrated self-damage. Confirm the card is offered only for lethal damage and Garruk remains at 1 Health after it resolves.
7. With **Mordren** and a Bone Servant in the correct Location, trigger damage to Mordren and confirm **Bone Shield** is offered; using it must remove the Servant before reducing damage.
8. With a Bone Servant in a Hero's Location and Mordren legally able to target that Hero, confirm **Dead Interpose** is offered and consumes that Servant.
9. Apply **Burning** to a Hero, save at least 1 general Action, finish the Round, and confirm End Phase pauses before Burning damage if a legal `would suffer damage` Reaction is available.
10. Decline/resolve the window and confirm End Phase continues normally into the next Round.
11. Test **Volatile Magic** or Malachar's migrated Arcane Overload self-damage after an Attack; the damage should resolve only after the Attack and should use the same reaction window.
12. Re-run a normal Enemy Attack and confirm the existing pre-Attack and post-Attack Reaction behavior still works.

## Expected log examples

- `REACTION WINDOW — Garruk Stoneblood would suffer 1 damage (Feed the Rage); ...`
- `REACTION — Garruk Stoneblood uses Ignore the Pain; pending damage is now 0.`
- `DAMAGE — <Hero> suffers <N> damage (<source>).`

## Known deliberate boundary

`Phase Shift`'s damage reduction is migrated into the generalized damage window, but its optional connected-Location movement still waits for the full player-choice movement UI. Enemy non-Attack ability/Spell windows remain a later parity pass.
