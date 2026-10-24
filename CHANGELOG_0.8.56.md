# Ascendance Unity 0.8.56 — Full Quest Runtime Execution

Built from the runtime-verified **0.8.55 — Full Crafting Recipe Runtime Execution** baseline.

## Quest catalog
- Expands the live Quest registry from Q001–Q012 to the full authored **Q001–Q072** catalog.
- Preserves the verified bespoke Campaign I runtimes for Q001–Q012.
- Adds `QuestRuntime0856` as the generalized authored execution layer for Q013–Q072.
- Dynamically builds later-campaign scenario Location graphs from the 236 authored Location definitions.
- Keeps all six 12-Quest campaign arcs and all 24 World Events registered.

## Q013–Q072 runtime
- Parses authored victory requirements into live Interact, Search, Enemy, Reach, Clear, Loot, Hold, and Choice goals.
- Preserves numeric objective counts such as Evidence, Survivors, Wards, Anchors, Nodes, facilities, Sites, and other authored tokens.
- Supports authored failure-pressure tracks (Murder, Harm, Collapse, Crisis, Storm, Pursuit, Descent, Ascent, etc.).
- Supports reveal-triggered enemy setup and delayed objective/boss spawns.
- Resolves named Quest Items as well as explicit LT### objectives.
- Preserves authored final-choice outcomes for Campaign III–VI finales.
- Applies authored XP, Gold, General Reputation, faction Reputation, Loot, Equipment, campaign unlocks, and campaign follow-on flags.

## Campaign progression
- Registers WE001–WE024 and the six campaign identities through Campaign VI.
- Campaign finale Quests are modeled as explicit `Final` quests: Q012, Q024, Q036, Q048, Q060, Q072.
- Campaign end/hub/continuation UI is generalized beyond Campaign I.
- Later campaign continuation can proceed from one campaign to the next while preserving the campaign archive.

## Persistence
- Saves/restores Q013–Q072 generalized objective state.
- Saves/restores reveal-spawn/delayed-spawn state.
- Saves/restores Quest campaign flags and faction Reputation.
- Older saves remain compatible because all new fields are optional/defaultable.

## Validation
Play Mode menu:

**Ascendance → Validation → Test 0.8.56 - Full Quest Runtime Execution (Play Mode)**

Expected final line:

`[ASCENDANCE][0.8.56a TEST] PASS — Full Quest Runtime Execution completed: 46 passed, 0 failed.`

0.8.56 is **not runtime-verified until this validator passes in Unity**.


## 0.8.56a hotfix
- Preserve authored campaign completion marker such as `Campaign VI` in Quest world-state flags.
- Correct the Q072 finale validator false-negative.
