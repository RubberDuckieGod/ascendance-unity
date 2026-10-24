# Ascendance Unity 0.8.57 — Full World Event Runtime Execution

Built from the runtime-verified **0.8.56a — Full Quest Runtime Execution** baseline.

## Full WE001–WE024 execution
- Keeps Campaign I's established WE001–WE003 chapter resolver authoritative while adding the remaining active modifiers needed by those events.
- Adds live execution paths for all **24 authored World Events** across all six campaigns.
- Final World Events WE004 / WE008 / WE012 / WE016 / WE020 / WE024 now record campaign-finale state and enforce their authored Travel/Retreat restrictions.
- Campaign Hub outcome preview now displays later-campaign authored effects/rewards/risks and the active campaign pressure meter instead of returning blank text.

## Persistent campaign pressure
- Campaign II: Suspicion 0–3.
- Campaign III: Instability 0–8 (starts at 2).
- Campaign IV: Blight 0–8 (starts at 2).
- Campaign V: Doomtide 0–8 (starts at 2).
- Campaign VI: Sunfall 0–8 (starts at 2).
- World Event flags and once-keys are serialized through the normal campaign save object.

## Active World Event mechanics
- WE001 Goblin first-activation Movement.
- WE002 Goblin Leader Health and Elite replacement.
- WE003 Archer Attack / banked Supply pressure.
- WE005 Curfew enemy pressure and WE006 Suspicion / Witness Testimony mitigation.
- WE006 Supply Network reward now cancels the first Q021 Ritual Focus reinforcement.
- WE007 Underground severity, Undead Difficult Terrain bypass, and Royal Seal / failed-chapter Crown Ward setup changes for Q024.
- Campaign III–VI hazard severity, first scripted-group Health pressure, route markers, entry damage, healing/status suppression, chapter rewards, faction Reputation, and penalty spawns.
- LT176 Heartseed Ward, LT182 Beacon Lens, and LT188 First Dawn Prism are consumed as authored World Event aids without being discarded.

## Quest-scoped cleanup
- World Event route markers clear on a fresh Quest attempt.
- Quest/Round once-keys clear when that Quest is freshly started, while remaining saveable during a live Quest.
- This prevents retrying a Quest from inheriting already-spent World Event triggers.

## Validation
Play Mode menu:

**Ascendance → Validation → Test 0.8.57 - Full World Event Runtime Execution (Play Mode)**

Expected final line:

`[ASCENDANCE][0.8.57 TEST] PASS — Full World Event Runtime Execution completed: 58 passed, 0 failed.`

0.8.57 is a **candidate** until this validator passes in Unity.
