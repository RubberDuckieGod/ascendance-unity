# Ascendance Unity 0.8.5 — Player-Chosen Hero Turn Order

Baseline: **0.8.3 Hero Gameplay Parity Audit**.

## Correction to 0.8.4

0.8.4 incorrectly interpreted the Hero Phase as having a preassigned authoritative `ActiveHeroId`.

The v3.1 rules instead say:

- Each Hero takes one complete turn during the Hero Phase.
- Players choose the order in which Heroes activate each Round unless a Scenario says otherwise.
- Each Hero completes the entire turn before the next Hero begins.
- After the final Hero completes their turn, the World Phase begins.

## Changes

- Removed the preassigned Active Hero model.
- Added a nullable `TurnHeroId` only for a turn that has actually started.
- `TurnHeroId` is null between turns, allowing the players to choose any unfinished Hero next.
- The first legal normal action/card or End Turn claims the current turn for the selected Hero.
- Once a turn is in progress, another Hero cannot take normal turn actions until that turn ends.
- Selecting another Hero remains available for inspection.
- Ending a turn no longer auto-selects or forces the next Hero.
- World Phase still waits until every eligible participating Hero has completed one turn.
- Legal Reactions from other Heroes remain supported.
- Save/Continue persists an actually in-progress turn via `TurnHeroId`.
- Legacy `ActiveHeroId` JSON is ignored as turn authority.
- Main menu now labels the chosen initial Hero as **Initial view**.

## Preserved

- 16 authored Heroes.
- 323 authored Hero-card definitions.
- 30-card personal decks and 5-card starting hands.
- 0.8.3 attack/modifier/reaction parity work.
- Saved Actions for legal Reactions.
- Bone Servants and Spirit Totems.
- Automatic-hit Hero combat and Enemy d6 combat.
- Q001, game shell, camera, NavMesh presentation, save/continue, and standalone build pipeline.
