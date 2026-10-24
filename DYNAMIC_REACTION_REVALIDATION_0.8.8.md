# 0.8.8 Dynamic Reaction Revalidation

## Regression A — target-changing Reaction

1. Start Q001 with **Sir Aldric** plus another Hero in Stonebrook.
2. In Play Mode use **Ascendance → Validation → Test - Give Aldric Interpose (Play Mode)** if needed.
3. Save at least 1 Action on Aldric and advance to an Enemy Attack against the other Hero.
4. Reaction Window should offer **Improved Shield Wall** and, if in hand/legal, **Interpose**.
5. Choose **Improved Shield Wall**.
6. Log must show `REACTION REVALIDATE — target changed by Improved Shield Wall...`.
7. **Interpose must disappear** because the Attack now targets Aldric and its printed trigger requires **another Hero** to be targeted.
8. Aldric self-target reactions such as Raise Shield / Brace remain legal if in hand and affordable.

## Regression B — generalized damage

1. Start with Garruk.
2. Use **Ascendance → Validation → Test - Give Garruk Ignore the Pain (Play Mode)**.
3. Trigger migrated self-damage such as Feed the Rage or Reckless Swing.
4. Choose **Ignore the Pain**.
5. Log must show the Reaction choice/resolution and then `REACTION REVALIDATE` with the reduced pending damage.
6. Health changes only after **Resolve / Decline** finishes the pending damage transaction.

## Expected invariant

Every option shown after a Reaction resolves must be legal against the **current** target, current pending damage, current Armor/damage-reduction modifiers, remaining Actions, card presence, piece requirements, and once-per-Quest/Round usage state.
