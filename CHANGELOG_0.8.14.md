# Ascendance Unity 0.8.14 — Post-Quest Free Exploration Contract

Stable baseline: **0.8.13 Downed / Dead / Revival Enforcement** (runtime verified; compile hotfix applied).

## Rules locked in

- Completing Q001 records the Quest victory; it does **not** stop the game world.
- After Q001 completion, Heroes may continue taking normal turns and may continue to Move, Explore, Attack, play legal cards, use Potions, and use legal Reactions.
- Every normal requirement still applies after victory: Actions still cost Actions, engagement can still block Explore, target/range rules still apply, and turn ownership still applies.
- Downed / Dead / Revival rules remain fully active during post-Quest exploration.
- A Hero can therefore still become Downed or Dead after the Quest is already complete.
- A post-Quest Hero defeat does **not** erase or retroactively change a Quest victory that was already earned.
- If every participating Hero later becomes Defeated after victory, the completed Quest remains recorded; the log reports the post-Quest party defeat instead of rewriting Q001 as failed.
- The HUD no longer says `VICTORY LOCKED`; it now explicitly reports `QUEST COMPLETE — FREE EXPLORATION ACTIVE`.

## Important boundary

This build does not fabricate systems that have not migrated to Unity yet. Full Search/Explore decks and broader campaign transition content remain later milestones. When those systems migrate, they must follow this same contract: a recorded Quest outcome by itself is not a global gameplay lock.

## Runtime validation

Start a fresh Play Mode game with at least 2 Heroes, then run:

**Ascendance → Validation → Test 0.8.14 - Post-Quest Free Exploration (Play Mode)**

The deterministic test completes Q001, verifies the finishing Hero can still Explore under normal rules, verifies another Hero can Start Turn afterward and receives the normal 3 Actions, then applies lethal post-Quest damage to an isolated Hero and confirms the normal **Dead** classification still occurs without erasing the completed Quest.
