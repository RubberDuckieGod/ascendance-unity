> **SUPERSEDED BY 0.8.5:** The preassigned `ActiveHeroId` model in this historical 0.8.4 document was an incorrect interpretation of the Hero Phase. 0.8.5 restores player-chosen Hero activation order each Round.

# Ascendance Unity 0.8.4 — Active Hero Turn Enforcement

Baseline: **0.8.3 Hero Gameplay Parity Audit**.

## Why this build exists

The 0.8.3 live log proved that turn advancement was being displayed but not actually enforced. A player could select another participating Hero and immediately spend that Hero's Actions/cards before the currently active Hero ended their turn.

## Changes

- Added an authoritative `ActiveHeroId` separate from `SelectedHeroId`.
- Selecting a Hero now changes the viewed Hero only; it does not transfer the active turn.
- Only the active Hero may:
  - Move.
  - Explore.
  - Rescue Villagers.
  - Extinguish Fire.
  - Make Basic Attacks.
  - Play normal Hero cards.
  - Use a Healing Potion / revive.
  - End the Hero turn through the normal HUD flow.
- Inactive Heroes may still be selected to inspect their stats, hand, deck, discard, statuses, and pieces.
- Reaction windows remain party-wide and may still spend saved Actions from Heroes whose normal turn is already complete.
- Required discard selection remains available when an effect instructs a Hero to draw/discard outside their normal turn.
- Added `TURN LOCK` logging for attempted inactive-Hero actions.
- Added `VIEW` logging when a non-active Hero is selected.
- HUD now marks the active Hero and disables normal action/card controls while viewing another Hero.
- Active Hero identity now persists through Save → Continue.
- Older 0.8.3 saves migrate by preferring the saved selected Hero when that Hero is still eligible; otherwise the first unfinished participating Hero becomes active.

## Preserved

- 16 authored Heroes.
- 323 Hero-card definitions.
- 30-card personal decks and 5-card starting hands.
- 0.8.3 attack/modifier/reaction parity work.
- Saved Actions for legal Reactions.
- Bone Servants and Spirit Totems.
- Automatic-hit Hero combat and Enemy d6 combat.
- Q001, game shell, camera, NavMesh presentation, and standalone build pipeline.

## Important Asha clarification

The 0.8.3 log showed Asha, Mordren, and Garruk receiving **Totemic Presence** while sharing Stonebrook with Asha's Spirit Totem. That is correct: Asha's authored ability grants Heroes in the Totem's Location +1 Attack on the first Attack they make each Round. No ownership restriction was added.
