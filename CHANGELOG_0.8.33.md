# Ascendance Unity 0.8.33 — Campaign I World Event & Campaign Progression

Built from the runtime-verified 0.8.32a final-quest baseline.

## Campaign progression
- Adds authoritative Campaign I progression across WE001 Goblin Raids, WE002 Goblin Warbands, WE003 Siege of Stonebrook, and WE004 The Goblin King.
- Adds World Event Day and total Campaign Day state.
- Required and optional Quest availability now comes from the active World Event rather than a hard-coded next-Quest button.
- Unresolved Quests block campaign selection; failed Quests may be retried while their World Event remains active.
- WE001 advances after Q001+Q002 or Day 5; WE002 after Q004+Q005 or Day 6; WE003 after Q007+Q008 or Day 6; WE004 ends with Q012 victory/defeat.
- Advancing by the Day limit records unresolved chapter Quests as missed.

## Authored chapter consequences
- WE001 all 3: +50 XP each, +1 Party Morale, remove 1 Scout from Q004's first patrol.
- WE001 incomplete: +1 Raider in Q004's first patrol, -1 General Reputation.
- WE002 all 3: +75 XP each, 1 Rare Equipment, +1 Reputation, remove 1 Elite from Q007 setup.
- WE002 incomplete: +1 Elite in Q007 setup, WE003 Travel severity +1, -1 Reputation.
- WE003 all 3: +100 XP each, 1 Epic Equipment, +2 Reputation, restore Damaged Buildings, bank Q010 Siege Pressure -1.
- WE003 required only: WE004 begins with 1 Damaged Building.
- WE003 incomplete: bank Q010 Siege Pressure +1, give each Hero one Wound Token at Q010 start, -2 Reputation, and record the Stonebrook pre-WE004 Equipment-purchase restriction.
- WE004 adds no duplicate tangible campaign reward beyond Q012.

## Carry-forward integration
- The campaign validator now resolves Quest rewards through the real reward chain, proving Q005→Q007 Elite removal, Q007→Q008 Siege Engine reduction, and Q009→Q010 Captain Roland assistance survive natural campaign progression.
- Q004 and Q007 setup read the new World Event reward/penalty state directly.
- WE001 Travel +1 and WE003 penalty Travel +1 now come from campaign state.
- WE004 random Travel Encounter suppression now comes from campaign state rather than Quest-ID special casing.

## Q010 source-lock handling
The WE003 chapter result uses the term “Siege Engine” for a Q010 adjustment, while Q010 itself explicitly locks two Ballistas at 8 Health / 2 Armor / Attack 4 / Range 3 and a 12 Health Gate. 0.8.33 preserves the two-Ballista Q010 print lock and persists the WE003 Siege Pressure modifier without silently rewriting the locked objective.

## Persistence / UI
- Saves Current World Event, World Event Day, total Campaign Day, completed/failed/missed Quest history, completed World Events, and chapter consequence flags.
- Quest panel now shows World Event/campaign state and offers separate Main Quest, Optional Quest, and Advance World Event controls.

## Validation
Static verification: **547/547 checks passed**.

Play Mode validator:
**Ascendance → Validation → Test 0.8.33 - Campaign I World Events (Play Mode)**

Expected runtime summary:
**PASS — Campaign I World Event progression completed: 45 passed, 0 failed.**
