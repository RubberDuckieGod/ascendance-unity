#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class EncounterEnemyMovementInitiativeTestTools0824
    {
        [MenuItem("Ascendance/Validation/Test 0.8.24 - Encounter Enemy Movement & Initiative (Play Mode)")]
        public static void TestEncounterEnemyMovementInitiative()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.24 TEST] FAIL — Enter Play Mode before running this test.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.24 TEST] FAIL — GameManager not found.");return;}
            if(gm.HasPendingReactionWindow){Debug.LogError("[ASCENDANCE][0.8.24 TEST] FAIL — Resolve the open Reaction window before running this test.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.24 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.24 TEST] FAIL — {message}");}}
            var heroes=gm.ParticipatingHeroes.Where(h=>!h.IsDefeated).OrderBy(h=>h.PartyIndex).ToList();
            if(heroes.Count<2){Debug.LogError("[ASCENDANCE][0.8.24 TEST] FAIL — Need at least 2 living Heroes.");yield break;}
            var hero=heroes[0];var forest=gm.Locations["L002"];var town=gm.Locations["L000"];var river=gm.Locations["L001"];var dense=gm.Locations["L004"];
            forest.Revealed=town.Revealed=river.Revealed=dense.Revealed=true;
            foreach(var h in heroes){h.LocationId=river.Id;h.Statuses.RemoveAll(s=>s.Name=="Hidden");}

            // Base movement: one connection toward the nearest Hero when no Hero shares the Enemy's Location.
            gm.DebugSetEncounterDeckTop("EN001");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1); // Severity 2 => one Raider.
            string baseEncounter=gm.DebugLatestActiveEncounterId();var baseEnemy=gm.DebugEncounterEnemy(baseEncounter);
            Check(baseEnemy!=null&&gm.DebugEnemyMovementAllowance(baseEnemy.Id)==1,"Normal Enemy activation has 1 Movement before encounter modifiers.");
            int baseSteps=baseEnemy==null?0:gm.DebugMoveEnemyForActivation(baseEnemy.Id);
            Check(baseEnemy!=null&&baseSteps==1&&baseEnemy.LocationId==town.Id,"A normal Enemy moves exactly one revealed connection toward the nearest Hero.");

            // Fresh Tracks is no longer a marker-only status: -1 Movement can reduce normal movement to 0.
            gm.DebugSetEncounterDeckTop("EN001");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string tracksEncounter=gm.DebugLatestActiveEncounterId();var tracksEnemy=gm.DebugEncounterEnemy(tracksEncounter);
            if(tracksEnemy!=null)tracksEnemy.Statuses.Add(new StatusInstance{Name="Fresh Tracks Movement -1",Source="Forest Path",ExpiryTiming=StatusExpiryTiming.EndOfRound,RemainingExpiryTriggers=1});
            Check(tracksEnemy!=null&&gm.DebugEnemyMovementAllowance(tracksEnemy.Id)==0,"Fresh Tracks reduces a normal Enemy from 1 Movement to 0.");
            Check(tracksEnemy!=null&&gm.DebugMoveEnemyForActivation(tracksEnemy.Id)==0&&tracksEnemy.LocationId==forest.Id,"Fresh Tracks actually prevents that Enemy from moving this Round.");

            // Difficult Terrain costs 2 Movement to leave for Enemies.
            if(tracksEnemy!=null)
            {
                tracksEnemy.Statuses.RemoveAll(s=>s.Name=="Fresh Tracks Movement -1");tracksEnemy.LocationId=dense.Id;
                Check(gm.DebugEnemyMovementAllowance(tracksEnemy.Id)==1&&gm.DebugMoveEnemyForActivation(tracksEnemy.Id)==0&&tracksEnemy.LocationId==dense.Id,"An Enemy with only 1 Movement cannot leave Difficult Terrain.");
                tracksEnemy.Statuses.Add(new StatusInstance{Name="Encounter Movement +1",Source="0.8.24 validation",ExpiryTiming=StatusExpiryTiming.EndOfRound,RemainingExpiryTriggers=1});
                Check(gm.DebugEnemyMovementAllowance(tracksEnemy.Id)==2&&gm.DebugMoveEnemyForActivation(tracksEnemy.Id)==1&&tracksEnemy.LocationId==forest.Id,"+1 Movement gives the 2 Movement required to leave Difficult Terrain.");
            }

            // EN025 — only Beast templates receive +1 Movement for Round 1.
            gm.DebugSetEncounterDeckTop("EN025");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4); // Severity 5.
            string huntId=gm.DebugLatestActiveEncounterId();var hunt=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==huntId);
            var huntEnemies=hunt==null?new EnemyState[0]:hunt.SpawnedEnemyIds.Where(gm.Enemies.ContainsKey).Select(id=>gm.Enemies[id]).ToArray();
            var beast=huntEnemies.FirstOrDefault(e=>e.TemplateId=="E021");var beastmaster=huntEnemies.FirstOrDefault(e=>e.TemplateId=="E007");
            Check(beast!=null&&gm.DebugEnemyMovementAllowance(beast.Id)==2,"EN025 gives its Dire Wolf +1 Movement during Round 1.");
            Check(beastmaster!=null&&gm.DebugEnemyMovementAllowance(beastmaster.Id)==1,"EN025 does not incorrectly give the Goblin Beastmaster the Beast movement bonus.");
            int beastSteps=beast==null?0:gm.DebugMoveEnemyForActivation(beast.Id);
            Check(beast!=null&&beastSteps==2&&beast.LocationId==river.Id,"An EN025 Beast can spend its 2 Movement across two connections toward a Hero.");
            gm.DebugAdvanceEndRoundStatusClock();
            Check(beast!=null&&gm.DebugEnemyMovementAllowance(beast.Id)==1,"EN025's Round-1 movement bonus expires at End of Round.");

            // EN016 — queues +1 Movement for the next Goblin Encounter, then consumes it once.
            gm.DebugSetEncounterDeckTop("EN016");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,2); // Severity 3.
            Check(gm.NextGoblinEncounterMovementBonus,"EN016 War Drums queues +1 Movement for the next Goblin Encounter.");
            gm.DebugSetEncounterDeckTop("EN001");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string drumsTargetId=gm.DebugLatestActiveEncounterId();var drumsEnemy=gm.DebugEncounterEnemy(drumsTargetId);
            Check(drumsEnemy!=null&&gm.DebugEnemyMovementAllowance(drumsEnemy.Id)==2&&!gm.NextGoblinEncounterMovementBonus,"The next Goblin Encounter receives War Drums +1 Movement and consumes the queued effect.");

            // EN016 Severity 5-6 is a different band: it queues an E008 War Drummer instead of the Movement bonus.
            gm.DebugSetEncounterDeckTop("EN016");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4); // Severity 5.
            Check(gm.NextGoblinEncounterWarDrummer&&!gm.NextGoblinEncounterMovementBonus,"EN016 Severity 5 queues a War Drummer for the next Goblin Encounter without incorrectly queuing +1 Movement.");
            gm.DebugSetEncounterDeckTop("EN001");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string drummerTargetId=gm.DebugLatestActiveEncounterId();var drummerTarget=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==drummerTargetId);
            var drummerEnemies=drummerTarget==null?new EnemyState[0]:drummerTarget.SpawnedEnemyIds.Where(gm.Enemies.ContainsKey).Select(id=>gm.Enemies[id]).ToArray();
            Check(!gm.NextGoblinEncounterWarDrummer&&drummerEnemies.Count(e=>e.TemplateId=="E008")==1,"EN016 Severity 5 adds exactly one E008 War Drummer to the next Goblin Encounter and consumes the queued reinforcement.");

            // EN023 — severity 5+ performs an immediate priority activation unless a Hero is Hidden.
            foreach(var h in heroes){h.LocationId=river.Id;h.Statuses.RemoveAll(s=>s.Name=="Hidden");}
            gm.DebugSetEncounterDeckTop("EN023");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4); // Severity 5.
            string sweepId=gm.DebugLatestActiveEncounterId();var sweep=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==sweepId);
            float guard=0f;while(gm.IsResolving&&guard<3f){guard+=Time.unscaledDeltaTime;yield return null;}
            var sweepEnemies=sweep==null?new EnemyState[0]:sweep.SpawnedEnemyIds.Where(gm.Enemies.ContainsKey).Select(id=>gm.Enemies[id]).ToArray();
            Check(sweepEnemies.Length==2&&sweepEnemies.All(e=>e.ActivatedThisRound),"EN023 Severity 5+ immediately activates every Enemy it spawned before Heroes continue.");
            Check(sweepEnemies.Length==2&&sweepEnemies.All(e=>e.LocationId==town.Id),"EN023 priority activation uses the normal Enemy movement runtime before attacking.");

            // A Hidden Hero suppresses the EN023 first-activation rule.
            hero.Statuses.Add(new StatusInstance{Name="Hidden",Source="0.8.24 validation",Beneficial=true,ExpiryTiming=StatusExpiryTiming.EndOfRound,RemainingExpiryTriggers=1});
            gm.DebugSetEncounterDeckTop("EN023");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4);
            string hiddenSweepId=gm.DebugLatestActiveEncounterId();var hiddenSweep=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==hiddenSweepId);
            var hiddenEnemies=hiddenSweep==null?new EnemyState[0]:hiddenSweep.SpawnedEnemyIds.Where(gm.Enemies.ContainsKey).Select(id=>gm.Enemies[id]).ToArray();
            yield return null;
            Check(hiddenEnemies.Length==2&&hiddenEnemies.All(e=>!e.ActivatedThisRound&&e.LocationId==forest.Id),"A Hidden Hero suppresses EN023's immediate Enemy-first activation.");
            hero.Statuses.RemoveAll(s=>s.Name=="Hidden");

            // EN026 — non-defeat Scout removal applies the failure modifier and does not pay/cancel it as a normal victory.
            int severityBefore=gm.NextTravelEncounterSeverityModifier,goldBefore=gm.PartyGold;
            gm.DebugSetEncounterDeckTop("EN026");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1); // Severity 2, one E004.
            string scoutsId=gm.DebugLatestActiveEncounterId();var scouts=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==scoutsId);var scout=gm.DebugEncounterEnemy(scoutsId);
            bool removed=scout!=null&&gm.DebugRemoveEnemyFromPlayWithoutDefeat(scout.Id);
            Check(removed&&scouts!=null&&scouts.HadEnemyLeavePlayUndefeated,"EN026 records a Scout leaving play without being Defeated separately from a kill.");
            Check(gm.NextTravelEncounterSeverityModifier==severityBefore+1,"A Scout leaving play without being Defeated queues +1 Severity for the next Travel Encounter.");
            Check(gm.PartyGold==goldBefore,"EN026 does not award its normal victory Gold or cancel the escape penalty after a Scout escapes.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.24 TEST] PASS — Encounter Enemy Movement & Initiative completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.24 TEST] FAIL — Encounter Enemy Movement & Initiative completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
