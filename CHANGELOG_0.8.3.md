# Ascendance Unity 0.8.3 — Hero Gameplay Parity Audit

Baseline: **0.8.2 Hero System Lifecycle Fix**.

## Changes

- Audited the Unity Hero runtime against the stabilized browser 3.36.11/3.36.13 Hero-card architecture.
- Preserves **16 Heroes, 323 Hero-card definitions, and 30 physical cards per Hero**.
- Reworked Hero turns so every active Hero completes a distinct turn before World Phase.
- Saved Actions remain available for valid Reactions until End Phase; Attack-card-only temporary Actions cannot pay unrelated costs.
- Expanded target-locked and next-Attack modifier handling across Basic and Hero-card Attacks.
- Added/strengthened high-risk Hero-card consumers for Garruk, Kael, Rowan, Malachar, Veyra, Lucien, Mordren, and Asha.
- Strengthened pre/post Enemy Attack Reaction timing and Cassian's post-kill Standing Ovation window.
- Added summon/totem prevalidation so impossible placement does not waste a card or Actions.
- Added persistent `Limit 1 per Quest/campaign` usage tracking, including save/load.
- Bone Shield / Dead Interpose now require the correct Bone Servant before the Reaction is offered.
- Added the Editor audit command: **Ascendance → Validation → Run 0.8.3 Hero Parity Audit**.
- Added `HERO_GAMEPLAY_PARITY_AUDIT_0.8.3.md` with explicit remaining migration boundaries; this build does not falsely claim every specialized 323-card board-state interaction is already migrated.

## Preserved

- 0.8.2 clean New Game / Continue lifecycle.
- 0.7.5 standalone Hero/enemy spawn registration fix.
- Q001 rules, automatic-hit Hero combat, Enemy d6 attacks, NavMesh presentation, game shell, save/continue, and production-art bridge.
