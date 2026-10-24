# Ascendance Unity 0.8.58b — Authored Location Lookup / Validator Hotfix

- Fixes the 0.8.58a Location validator selecting legacy runtime graph IDs before canonical `AuthoredLocationId` values.
- Stonebrook keeps legacy runtime graph IDs for save compatibility; those IDs intentionally do not always match printable Location IDs.
- `DebugLocationByAuthored0858` now resolves canonical authored IDs first.
- Authored tiles not currently instantiated on the legacy Stonebrook graph are materialized as validator-only runtime Location states from `full_content_0850.json`, using collision-safe `DBG_L###` runtime IDs.
- `DebugEnsureQuestLocations0858` still validates actual Quest instantiation and does not auto-materialize missing Quest locations.
- Adds a regression check covering known collisions: L003 Woodland Clearing, L006 Goblin Lookout, L007 Abandoned Campsite, and L008 Spider Nest.
- No authored Location gameplay rule is intentionally changed.

Validation target: **75 passed, 0 failed**.
