# Ascendance Unity 0.8.6 — Generalized Hero Damage Reactions

Baseline: **0.8.5 Player-Chosen Hero Turn Order**.

## Changes

- Preserves the confirmed 0.8.5 player-chosen Hero activation flow unchanged.
- Added a dedicated `BeforeHeroDamage` reaction window for Hero damage that is not an Enemy Attack.
- Broad `would suffer damage` protection Reactions can now respond to migrated self-damage and Condition/direct-damage sources instead of only the Enemy Attack pipeline.
- Damage reductions stack against the pending damage amount before Health is changed.
- Lethal-prevention Reactions such as **Too Angry to Fall**, **Intercession**, and **Divine Intervention** can protect against migrated non-Attack damage when their printed targeting requirements are met.
- **Bone Shield** and **Dead Interpose** still require and consume the correct Bone Servant before reducing damage.
- **Guardian's Blessing** grants Inspired when the pending damage is fully prevented.
- Routed current migrated Hero self-damage through the shared transaction: **Mana Burn**, **Reckless Swing**, **Feed the Rage**, **Dark Bargain**, **Volatile Magic**, **Meteor**, and Malachar's **Arcane Overload** self-damage.
- Hero **Burning** damage at End Phase now uses the same reactionable damage transaction and pauses End Phase until its reaction window is resolved.
- Reaction UI wording is now generic (`RESOLVE / DECLINE`) so the modal correctly serves Attack and damage windows.

## Deliberate boundary still preserved

0.8.6 does not claim the full Enemy ability/Spell engine or optional movement/choice layer is migrated. Secondary choices such as **Phase Shift**'s optional movement remain deferred to the full choice UI. Enemy ability-specific windows such as **Counter Melody** and **Spell Reflection** remain a later migration item.
