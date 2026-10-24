# Ascendance Unity 0.8.60c — Enemy-Entry Reaction Validator Reveal Hotfix

Built from 0.8.60b. Gameplay/runtime card rules are unchanged.

## Fix
The 0.8.60b validator attempted to choose an already-revealed connected pair from the active Q001 map. Q001 starts with Stonebrook revealed while its connected exploration Locations remain unrevealed, so the helper could return no pair and the Overwatch/Vanish tests failed before either Reaction was attempted.

0.8.60c now:
- chooses a real existing edge from the active Quest-enterable map graph,
- temporarily reveals only those two already-connected Locations for the isolated test,
- does not create or modify graph connections,
- continues to require real Reaction cost/discard and movement/attack resolution.

Target validator result: 85 passed, 0 failed.
