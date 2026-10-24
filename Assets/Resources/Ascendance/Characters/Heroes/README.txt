ASCENDANCE PRODUCTION HERO PREFAB SLOTS

Drop production Hero prefabs here with these exact names:
  Garruk.prefab
  Rowan.prefab
  Aldric.prefab
  Elara.prefab

The runtime automatically uses a prefab when present. If a prefab is absent, the 0.3.x procedural model is used.

Recommended Animator parameters (all optional):
  Speed (float)
  Combat (bool)
  AttackType (int: 0 melee, 1 heavy, 2 ranged, 3 spell, 4 shield)
  Attack (trigger)
  Hit (trigger)
  Death (trigger)
  Revive (trigger)
  Downed (bool)

Recommended Animation Events on attack clips:
  AscendanceImpact
  AscendanceAttackFinished

If the events are missing, the runtime uses a safe timed impact fallback.
