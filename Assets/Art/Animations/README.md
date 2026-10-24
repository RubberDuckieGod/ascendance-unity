# Ascendance Animation Contract — 0.4.0

Production character controllers can be authored however you prefer, but the runtime understands this optional parameter contract:

- `Speed` float — locomotion magnitude
- `Combat` bool — combat-ready state
- `AttackType` int — 0 melee, 1 heavy, 2 ranged, 3 spell, 4 shield
- `Attack` trigger
- `Hit` trigger
- `Death` trigger
- `Revive` trigger
- `Downed` bool

For frame-accurate rules presentation, add Animation Events named `AscendanceImpact` at the weapon/projectile/cast impact frame and `AscendanceAttackFinished` near the end of the attack clip. If these events are not present, Ascendance uses the configured fallback timing so combat can never hang.
