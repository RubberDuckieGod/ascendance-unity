# Ascendance Unity 0.8.16 — Core Status Effect Enforcement

Built from the runtime-verified 0.8.15 Status Duration Lifecycle baseline.

## Runtime enforcement added / verified

- **Stunned (Hero):** the next activation is consumed immediately with 0 Actions; activation-timed Stunned then expires. Stunned Heroes cannot use normal turn Actions or Reaction-card Action costs.
- **Stunned (Enemy):** existing skipped-activation behavior remains and continues to use the 0.8.15 activation clock.
- **Rooted:** blocks voluntary Hero movement, including normal/free Move and the migrated move-then-attack cards.
- **Slowed:** reduces the normal Hero activation grant from 3 Actions to 2 (rule minimum 1).
- **Inspired / Weakened:** +1 / -1 Attack enforcement is covered for Heroes; Weakened also reduces Enemy Attack.
- **Guarded / Vulnerable:** +1 / -1 Armor enforcement is covered for Heroes; Vulnerable also reduces Enemy Armor.
- **Marked:** the Hero who applied Marked gains +1 Attack against that Enemy. Predator's Mark explicitly grants the +1 to all attackers.
- **Exposed:** the next Attack against the target gains +1 Attack, then Exposed is removed.
- **Regenerating:** recovers 1 Health at the start of the Hero activation.
- **Stealth:** intentionally has no universal Attack/Armor/targeting bonus. Only card text that references Stealth grants its benefit.

## Scope

This is the core-condition enforcement pass for the current Q001 / migrated Hero-card runtime. It does **not** claim all 102 authored statuses are mechanically complete. Damage-over-time and specialized control/equipment conditions remain subsequent parity work.

## Play Mode validation

Run:

`Ascendance > Validation > Test 0.8.16 - Core Status Effect Enforcement (Play Mode)`
