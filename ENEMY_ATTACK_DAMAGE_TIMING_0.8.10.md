# Ascendance Unity 0.8.10 — Enemy Attack Damage Reaction Timing

## Problem found in the 0.8.9 live test

The 0.8.9 Enemy Attack pipeline rolled the d6 before the pre-Attack window and used the predicted damage to offer `would suffer damage` / `would be defeated` cards in the same window as target/retarget reactions.

That made cards such as Garruk's **Ignore the Pain** appear before the attack had actually reached the damage step.

## Canonical 0.8.10 order

1. Enemy selects a target.
2. Open **pre-Attack** reactions only:
   - targeted/declared-target protection,
   - retarget effects such as Interpose / Improved Shield Wall,
   - Armor modifiers,
   - attack cancellation or other attack-declaration effects.
3. Revalidate until the target/protection state is final.
4. If the Attack was canceled, stop before rolling.
5. Roll the Enemy d6.
6. Calculate pending damage:
   `d6 + Enemy Attack + modifiers - Hero Armor - valid pre-Attack reduction`, minimum 0.
7. Open the centralized **Hero damage** reaction transaction with `HeroDamageSourceKind.EnemyAttack`.
8. Offer only legal damage/defeat reactions against the known pending damage.
9. Revalidate after every damage reaction.
10. Apply remaining damage exactly once.
11. Run Downed/Dead handling.
12. Open post-Attack reactions using the actual final damage dealt.

## Important preservation rules

- Interpose and Improved Shield Wall still retarget before the roll.
- Protection that belonged only to the old target still does not transfer after a retarget.
- Pre-Attack Armor/reduction is captured before the pre-Attack window closes.
- A pre-Attack effect that establishes a 1-Health floor carries into the damage transaction.
- Veyra's conditional Soul Shield can inspect the actual attacking Enemy's Statuses.
- Hero damage hooks receive the attacking Enemy ID, not a generic text label.
- Burning/self-damage remain in their existing generalized damage path and are not treated as Enemy Attacks.

## Deterministic Play Mode helper

With Garruk in the current party:

`Ascendance → Validation → Test 0.8.10 - Garruk Enemy-Attack Damage Timing (Play Mode)`

Expected behavior:

- Ignore the Pain is moved from Garruk's Deck/Discard to Hand without creating a duplicate.
- Captain's forced d6 6 is converted to post-Armor pending damage.
- A **Before Hero Damage** window opens.
- Ignore the Pain is a legal option only now, after damage is known.
- Choosing it reduces pending damage by 2.
- Resolve Damage applies the remainder once.

## Runtime validation boundary

This package was statically validated outside the Unity Editor. Unity Play Mode is still required to validate animation callbacks, modal timing, and the complete World Phase coroutine in the actual Editor/runtime.
