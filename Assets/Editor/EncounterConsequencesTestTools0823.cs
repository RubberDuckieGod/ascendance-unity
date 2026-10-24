#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class EncounterConsequencesTestTools0823
    {
        [MenuItem("Ascendance/Validation/Test 0.8.23 - Campaign I Encounter Consequences (Play Mode)")]
        public static void TestEncounterConsequences()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.23 TEST] FAIL — Enter Play Mode before running this test.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.23 TEST] FAIL — GameManager not found.");return;}
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.23 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.23 TEST] FAIL — {message}");}}
            var heroes=gm.ParticipatingHeroes.Where(h=>!h.IsDefeated).OrderBy(h=>h.PartyIndex).ToList();if(heroes.Count<2){Debug.LogError("[ASCENDANCE][0.8.23 TEST] FAIL — Need at least 2 living Heroes.");return;}
            var hero=heroes[0];var forest=gm.Locations["L002"];var river=gm.Locations["L001"];var town=gm.Locations["L000"];forest.Revealed=river.Revealed=town.Revealed=true;

            // EN002 severity 5+: setup damage is party-wide, not limited to Heroes already on the triggering tile.
            hero.LocationId=forest.Id;heroes[1].LocationId=town.Id;
            var hp=heroes.ToDictionary(h=>h.Id,h=>h.Health);
            gm.DebugSetEncounterDeckTop("EN002");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4);
            Check(heroes.All(h=>h.Health==Mathf.Max(0,hp[h.Id]-1)),"EN002 Severity 5+ deals its authored 1 setup damage to every Hero, including Heroes off the triggering tile.");

            // EN010 severity 7+: all Heroes begin Slowed while the E004 + E002 crossing combat is created.
            foreach(var h in heroes){h.LocationId=river.Id;h.Statuses.RemoveAll(s=>s.Name=="Slowed");}
            gm.DebugSetEncounterDeckTop("EN010");gm.DebugResolveTravelEncounter(river.Id,hero.Id,6);
            string bridge=gm.DebugLatestActiveEncounterId();
            Check(heroes.All(h=>h.HasStatus("Slowed")),"EN010 Severity 7+ applies Slowed to every Hero at setup.");
            Check(gm.ActiveEncounters.Any(e=>e.InstanceId==bridge)&&gm.DebugEncounterRemaining(bridge)==2,"EN010 Severity 7+ creates its authored two-enemy crossing encounter.");

            // EN030 severity 7+: queue +1, then consume it exactly once on the next resolved Encounter.
            foreach(var h in heroes)h.LocationId=forest.Id;
            gm.DebugSetEncounterDeckTop("EN030");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,6);
            Check(gm.NextTravelEncounterSeverityModifier==1,"EN030 Severity 7+ queues +1 Severity for the next Travel Encounter.");
            gm.DebugSetEncounterDeckTop("EN001");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            Check(gm.LastEncounterCardId=="EN001"&&gm.LastEncounterSeverity==3,"The queued EN030 modifier changes d6 1 + WE001 1 into Severity 3 on the next Encounter.");
            Check(gm.NextTravelEncounterSeverityModifier==0,"The queued Severity modifier is consumed exactly once.");

            // EN028 severity 7+: skip exactly one future Encounter without consuming the top card.
            gm.DebugSetEncounterDeckTop("EN028");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,6);
            Check(gm.SkipNextTravelEncounter,"EN028 Severity 7+ queues Skip Next Travel Encounter.");
            gm.DebugSetEncounterDeckTop("EN001");string before=gm.LastEncounterCardId;gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            Check(!gm.SkipNextTravelEncounter&&gm.LastEncounterCardId==before,"The next Travel Encounter is skipped without drawing or replacing LastEncounterCardId.");
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            Check(gm.LastEncounterCardId=="EN001","After the one-use skip is consumed, the following Travel Encounter resolves normally.");

            // EN024: remaining Fire costs 5 Gold each in supplies; extinguished Fire does not.
            foreach(var h in heroes)h.LocationId=town.Id;gm.DebugSetPartyGold(20);
            gm.DebugSetEncounterDeckTop("EN024");gm.DebugResolveTravelEncounter(town.Id,hero.Id,4); // Severity 5 => E014 + E001 + 2 Fire
            string raid=gm.DebugLatestActiveEncounterId();
            Check(gm.DebugEncounterObjectCount(raid,"Fire Token")==2,"EN024 Severity 5 places two Fire Tokens.");
            var oneFire=gm.DebugFirstEncounterObject(raid,"Fire Token");if(oneFire!=null)gm.DebugInteractEncounterObject(hero.Id,oneFire.ObjectId);
            while(gm.ActiveEncounters.Any(e=>e.InstanceId==raid))
            {
                var enemy=gm.DebugEncounterEnemy(raid);if(enemy==null)break;gm.DealDirectDamage(enemy,enemy.Health,"0.8.23 Firestarter Raid validation",hero);
            }
            Check(gm.DebugEncounterObjectCount(raid,"Fire Token")==0,"EN024 removes leftover Fire Tokens when the encounter ends.");
            Check(gm.PartyGold==45,"EN024 with one Fire remaining loses 5 Gold in supplies, then awards its 30 Gold victory reward.");
            Check(!(gm.PendingContentInstruction??string.Empty).Contains("EN024 reward remainder: +1 Reputation"),"EN024 does not award the Reputation remainder when Fire remained at clear.");

            // EN026 reward cancels a pending +1 Travel Severity modifier.
            gm.DebugSetEncounterDeckTop("EN026");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string scouts=gm.DebugLatestActiveEncounterId();gm.DebugQueueTravelSeverity(1);
            while(gm.ActiveEncounters.Any(e=>e.InstanceId==scouts)){var enemy=gm.DebugEncounterEnemy(scouts);if(enemy==null)break;gm.DealDirectDamage(enemy,enemy.Health,"0.8.23 Chieftain's Scouts validation",hero);}
            Check(gm.NextTravelEncounterSeverityModifier==0,"EN026 victory cancels the pending modifier to the next Travel Encounter.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.23 TEST] PASS — Campaign I Encounter Consequences completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.23 TEST] FAIL — Campaign I Encounter Consequences completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
