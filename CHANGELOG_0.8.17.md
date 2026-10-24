# Ascendance Unity 0.8.17 — Live Specialized Condition Parity

Built from the runtime-verified 0.8.16 Core Status Effect Enforcement baseline.

## Added / corrected
- Current card text can now apply Hidden, Knocked Down, Shocked, and Taunted in addition to the previously migrated condition set.
- Hidden ends immediately when its Hero attacks.
- Knocked Down blocks voluntary movement. A Hero automatically spends the first normal Action of the activation to Stand; a modeled Enemy activation is consumed standing.
- Taunted forces the Enemy's next legal attack target to the Hero that applied Taunted when that Hero is targetable.
- Shocked prevents the next additional Hero Action gain, then removes itself.
- Persistent Burning deals 1 damage at the end of each activation and no longer incorrectly ticks in the generic End Phase.
- Bleeding deals 1 damage after an activation in which the affected unit actually performed an Attack; it does not trigger on a skipped/no-attack activation.
- Meteor Shard's special Burning resolves for 1 damage at the start of the affected Enemy's next activation and then removes itself.
- Nature's Blessing now removes Bleeding or Burning from its healed Hero when present.
- Hero-side Burning and Bleeding hooks are present for future enemy/encounter content.

## Arcane Location dependency
Meteor Shard only applies Burning while Vaelis is in an Arcane Location. The current Stonebrook Unity vertical slice does not yet persist Arcane Location tags, so 0.8.17 deliberately does not falsely grant Meteor Shard's conditional Burning during ordinary card resolution. The one-shot Burning runtime is implemented and the validation harness applies it as though the Arcane prerequisite had been satisfied. Arcane Location state remains a later content-system migration.

## Validation
Run in Play Mode at the start of a Hero Phase:

`Ascendance > Validation > Test 0.8.17 - Live Specialized Conditions (Play Mode)`
