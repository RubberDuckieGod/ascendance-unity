# Ascendance Unity 0.8.7 — Reaction Input Stability Fix

Baseline: **0.8.6 Generalized Hero Damage Reactions**, preserving the confirmed **0.8.5 player-chosen Hero turn order**.

## Fixes

- Fixed reaction-option buttons being destroyed and recreated every rendered frame while the World Phase waited for player input.
- Reaction windows now refresh once when opened, then remain stable until the player chooses a reaction or resolves/declines the trigger.
- UI text labels no longer intercept pointer raycasts from their parent Buttons.
- Reaction choices are validated against the currently legal option list **before** card Action cost is spent.
- Added Adventure Log diagnostics for accepted reaction clicks and stale/unavailable reaction input.
- Added an Editor-only test command: **Ascendance → Validation → Test - Give Garruk Ignore the Pain (Play Mode)**.
  - This moves an existing physical copy from Garruk's Deck or Discard into his Hand; it does not create an extra copy.

## Preserved

- 16 authored Heroes / 323 Hero-card definitions / 30-card physical decks.
- Player-chosen Hero activation order from 0.8.5.
- Saved Action / Reaction economy.
- Generalized Hero-damage reaction transaction from 0.8.6.
- Q001, save/continue, Hero-created pieces, automatic-hit Hero combat, and Enemy 1d6 combat.
