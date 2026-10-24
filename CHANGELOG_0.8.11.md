# 0.8.11 — True Hero Turn Start Lifecycle

- Hero turns no longer inherit a round-wide Action grant.
- Every unfinished Hero now waits at **0 normal Actions** until that Hero explicitly starts a turn.
- Added explicit `StartSelectedHeroTurn()` / `TryStartHeroTurn(...)` authority.
- Canonical Hero-turn order is now:
  **Choose Hero → Start Turn → Draw → Gain 3 Actions → Perform Actions → End Turn**.
- Round 1 preserves the existing **5-card opening hand** and does not draw again when a Hero starts their first turn.
- From Round 2 onward, each Hero draws exactly their `DrawValue` when their own turn starts.
- `CanUseTurnActions(...)` now requires that Hero to be the current `TurnHero`; normal actions can no longer implicitly claim a turn.
- `RequireHeroTurn(...)` now blocks/logs when Start Turn has not occurred instead of silently assigning turn ownership.
- Ending a Hero turn still preserves unused general Actions for legal Reactions until End Phase.
- End Phase now clears held Actions, resets per-Round state, and begins the next Hero Phase with every unfinished Hero at 0 Actions.
- Save/load preserves an in-progress Hero turn exactly as saved without replaying Draw or Action grant.
- Loading between turns forces unfinished/unstarted Heroes to 0 Actions while preserving completed Heroes' held Reaction Actions.
- HUD now exposes a single **START TURN / END TURN** control and clearly distinguishes `CHOOSE HERO • START TURN` from an active Hero turn.
- Added a deterministic Play Mode lifecycle validator under:
  **Ascendance → Validation → Test 0.8.11 - Hero Turn Start Lifecycle (Play Mode)**.
