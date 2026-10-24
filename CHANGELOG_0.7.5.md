# Ascendance Unity 0.7.5 — Standalone Avatar Spawn Safety

- Fixed standalone builds showing the party/enemy state in HUD while world avatars failed to register.
- HeroAvatar and EnemyAvatar now register with the authoritative GameManager before optional world-space cosmetics are created.
- Missing/stripped cosmetic shaders can no longer remove gameplay pieces.
- World rings/path preview use player-safe shader fallbacks.
- World health bars no longer depend on `Unlit/Color` being retained in the player build.
- Added `SPAWN READY` diagnostic after a complete 4/4 Hero + 3/3 Enemy spawn.
- Preserves 0.7.4 menu/save/Continue and Q001 behavior.
