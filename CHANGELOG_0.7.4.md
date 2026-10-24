# Ascendance Unity 0.7.4 — Party & Enemy Spawn Validation

- Guarantees all four Q001 Hero avatars are spawned after Stonebrook navigation is ready.
- Guarantees every Q001 Enemy state has a world avatar, including defeated enemies restored from save.
- Isolates presentation failures per character so one bad model cannot prevent the rest of the party/enemies from spawning.
- Adds a post-spawn validation/repair pass.
- Adds `SPAWN CHECK — Heroes 4/4 · Enemies 3/3 (N living)` to the Adventure Log.
- Continue still preserves defeated enemies from the save; it does not resurrect them.
- Preserves 0.7.3 scene lifecycle, save/continue, camera, combat, movement, HUD, and Q001 rules.
