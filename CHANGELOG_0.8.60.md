# Ascendance Unity 0.8.60 — Full Hero & Hero Card Runtime Execution

Built from the runtime-verified **0.8.59b — Full Enemy Runtime Execution** baseline.

## Scope
- Audits all 16 authored Heroes and all 323 authored Hero-card definitions.
- Preserves each Hero's 30-card physical deck and all 49 authored Reaction definitions.
- Extends the existing generic Hero/card runtime with shared execution hooks for choice-heavy movement, conditional Attack bonuses, post-Armor damage, Hero-created pieces, entry/forced-movement Reactions, targeted Enemy-ability reflection, and card-chain timing.

## Runtime additions
- Conditional Attack text is no longer accidentally treated as an unconditional base bonus for cards such as Backstab, Radiant Strike, Arcane Bolt, Smite Evil, Flying Kick, Precision Bolt, Bloodied Strike, Duelist's Cut, and Totemic Strike.
- Hundred Blows tracks the third Attack card played this turn before ignoring Armor.
- Master’s Hunt requires a Marked target; Damnation requires an afflicted target.
- Time Stop now prevents affected Enemies from moving while active.
- Overwatch, Automatic Countermeasure, Command the Fallen, Protective Roots, Spell Reflection, and Vanish Without a Trace resolve through live runtime hooks.
- Mira Turrets, Mechanical Traps, Arcane Wards, Bone Servants, and Spirit Totems remain persistent Hero-created game-state objects.
- Deterministic legal target/movement selection is used for authored optional/choice-heavy effects until multiplayer choice UI owns those decisions.

## Persistence
0.8.60 reuses already save-persistent Hero Status, hand/discard, UsedTurnAbilities, and HeroCreatedPieces state. Card-play/Attack-play tracking is stored in UsedTurnAbilities, so no incompatible save schema is introduced.

## Validation
Play Mode menu:

**Ascendance → Validation → Test 0.8.60 - Full Hero & Hero Card Runtime Execution (Play Mode)**

Expected:

`[ASCENDANCE][0.8.60 TEST] PASS — Full Hero & Hero Card Runtime Execution completed: 85 passed, 0 failed.`
