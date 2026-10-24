# Ascendance Unity 0.8.19 — Stonebrook Location Effect Execution

- Executes optional Reveal permissions for Dense Woods (Hide), River Crossing (Fresh Water), and Ancient Ruins (free Search).
- Adds a real Search action with one-Search-per-Location state and a Search deck integration hook.
- First Visit now emits a Travel Encounter request with Encounter Level while preserving the locked enemy-present deferral rule.
- Adds exact ranged Location attack modifiers for Limited Visibility / High Ground / Open Sightlines when those authored effects are present.
- Exposes Cover-vs-ranged as real Location state without inventing an un-authored numeric Cover value.
- Adds a reusable Goblin Home Territory +1 Armor hook for Locations whose authored ongoing effect grants it.
- Persists free Search / optional Reveal permissions plus Search/Travel request counts.
- Adds HUD Search and Location Option controls.
