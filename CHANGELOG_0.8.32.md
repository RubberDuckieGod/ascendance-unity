# Ascendance Unity 0.8.32 — Campaign I Final Quest Batch (Q009–Q012)

Built from the runtime-verified 0.8.31a baseline.

## Q009 — Rescue Captain Roland
- Adds the authored underground approach and Prison Pens objective.
- Captain Roland is a persistent Quest object/ally with 5 Health and Attack 2.
- Execution Track starts at 5 and falls at the end of each Enemy Phase until Roland is freed.
- Freeing Roland costs 1 Action, stops the clock, and triggers the prison-break reinforcement wave.
- Roland follows his rescuer and must reach Stonebrook; Roland reaching 0 Health after rescue fails the Quest.
- Victory grants 125 XP per Hero, 100 Gold, +2 General Reputation, and persists Roland's additional-Ally allowance for Q010.

## Q010 — Assault the Fortress Gate
- Fortress Gate: 12 Health, 4 Armor; Armor becomes 2 after both Ballistas are destroyed.
- Ballistas: 8 Health, 2 Armor, Attack 4, Range 3.
- Supports Ballista attacks, one-time Siege Engineer field repair, Archer reactions to Ballista destruction, the Gate-at-6 Champion/Bodyguard wave, Gate reinforcements, and the four-round abandoned-assault failure clock.
- Victory grants 225 XP per Hero, 200 Gold, and 1 Epic Equipment.

## Q011 — Through the Stronghold
- Starts at the breached Fortress Gate with 3 Reinforcement Tokens.
- Uses authored Fortress/Goblin Stronghold Location IDs for qualifying reveals so legacy metadata cannot consume reinforcement tokens incorrectly.
- Reinforcement Tokens resolve the printed three waves.
- Goblin Throne Room cannot be revealed/entered until two other qualifying stronghold Locations are revealed.
- Goblin High Shaman remains single-copy.
- Victory grants 250 XP per Hero, 200 Gold, and 1 Epic Equipment.

## Q012 — The Goblin King
- Uses dedicated E032 The Goblin King: 26 Health, 5 Attack, 3 Armor.
- Uses the printed 4-Banner shield and Banner reinforcement waves.
- Banner protection is enforced in the common Hero/direct-damage pipeline so card/direct damage cannot bypass it.
- Implements King of the Horde (+1 Attack once), Crushing Command, Call the Horde, and the printed Last Stand through the real enemy activation pipeline.
- Does not add a second Last Stand on top of E032's authored ability.
- Victory grants 300 XP per Hero, 1 Legendary Equipment per Hero, +3 General Reputation, Defender of Stonebrook, and Campaign I Victory.
- WE004 adds no second tangible finale reward; Q012 is the reward source.
- Elite Raider/Archer Banner spawns use the current E001/E002 runtime chassis with Elite display naming because no separate printed elite E001/E002 templates are authored in the current runtime catalog.

## Campaign I / World behavior
- Adds the late-Campaign-I cave/mine/fortress graph branch without renumbering existing graph IDs.
- Q010–Q012 suppress random Travel Encounters under WE004.
- Saves/restores all new Q009–Q012 objective, boss, progression, unlock, and carryover state.

## Validation
- Deterministic Play Mode validator: **Ascendance → Validation → Test 0.8.32 - Campaign I Q009-Q012 (Play Mode)**.
- Expected validator result: **35 passed, 0 failed**.
- Static verifier: **503/503 checks passed**.
- Runtime verification is pending the Unity Play Mode validator.
