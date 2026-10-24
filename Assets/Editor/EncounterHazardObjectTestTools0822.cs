#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class EncounterHazardObjectTestTools0822
    {
        [MenuItem("Ascendance/Validation/Test 0.8.22 - Encounter Hazards & Objects (Play Mode)")]
        public static void TestEncounterHazards()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.22 TEST] FAIL — Enter Play Mode before running this test.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.22 TEST] FAIL — GameManager not found.");return;}
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.22 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.22 TEST] FAIL — {message}");}}
            var heroes=gm.ParticipatingHeroes.Where(h=>!h.IsDefeated).Take(2).ToList();if(heroes.Count<2){Debug.LogError("[ASCENDANCE][0.8.22 TEST] FAIL — Need at least 2 living Heroes.");return;}
            var hero=heroes[0];var other=heroes[1];var forest=gm.Locations["L002"];forest.Revealed=true;hero.LocationId=forest.Id;other.LocationId="L000";

            // EN006: persistent Snare markers are non-blocking and trigger on later entry.
            gm.DebugSetEncounterDeckTop("EN006");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4); // +1 WE001 = Severity 5
            string snareEncounter=gm.DebugLatestActiveEncounterId();
            Check(gm.DebugEncounterObjectCount(snareEncounter,"Snare")==2,"EN006 Severity 5 places two persistent Snare objects.");
            Check(gm.DebugEncounterBlockingObjectCount(snareEncounter)==0,"EN006 Snares do not block combat Encounter completion.");
            int beforeSnares=gm.DebugEncounterObjectCount(snareEncounter,"Snare");
            other.LocationId=forest.Id;gm.DebugTriggerEncounterEntry(other.Id,forest.Id);
            Check(gm.DebugEncounterObjectCount(snareEncounter,"Snare")==beforeSnares-1,"Entering a Snared Location consumes exactly one Snare marker.");
            Check(other.HasStatus("Rooted"),"The Hero who triggers EN006 Snare becomes Rooted until end of turn.");

            // EN007 is Road / Settlement only. Validate it at Stonebrook instead of Forest Path so terrain filtering is exercised rather than bypassed.
            var settlement=gm.Locations["L000"];settlement.Revealed=true;hero.LocationId=settlement.Id;other.LocationId=settlement.Id;
            Check(gm.DebugMatchesEncounterLocation("EN007",settlement.Id),"EN007 validation uses a Road/Settlement-eligible Location.");

            // EN007: fire tokens exist, can be extinguished, and determine the Item bonus at clear.
            gm.DebugSetEncounterDeckTop("EN007");int goldBefore=gm.PartyGold;gm.DebugResolveTravelEncounter(settlement.Id,hero.Id,4);
            string fireEncounter=gm.DebugLatestActiveEncounterId();
            Check(gm.DebugEncounterObjectCount(fireEncounter,"Fire Token")==2,"EN007 Severity 5 places two Fire Token objects.");
            var f1=gm.DebugFirstEncounterObject(fireEncounter,"Fire Token");Check(f1!=null&&gm.DebugInteractEncounterObject(hero.Id,f1.ObjectId),"A Fire Token can be extinguished through the shared Encounter-object interaction path.");
            var f2=gm.DebugFirstEncounterObject(fireEncounter,"Fire Token");Check(f2!=null&&gm.DebugInteractEncounterObject(hero.Id,f2.ObjectId),"The second Fire Token can also be extinguished independently.");
            var fireEnemy=gm.DebugEncounterEnemy(fireEncounter);if(fireEnemy!=null)gm.DealDirectDamage(fireEnemy,fireEnemy.Health,"0.8.22 fire validation",hero);
            Check(!gm.ActiveEncounters.Any(e=>e.InstanceId==fireEncounter)&&gm.PartyGold>=goldBefore+25,"EN007 clears after its combatant is defeated and awards the 25 Gold victory reward.");
            Check((gm.PendingContentInstruction??string.Empty).Contains("Draw 1 Item because all Fire Tokens were removed"),"EN007 records the authored Item bonus only when all Fire Tokens were extinguished before clear.");

            // EN007 low band: one blocking fire, 10 Gold on the actual extinguish action, then the normal victory reward.
            gm.DebugSetEncounterDeckTop("EN007");goldBefore=gm.PartyGold;gm.DebugResolveTravelEncounter(settlement.Id,hero.Id,1);
            string lowFireEncounter=gm.DebugLatestActiveEncounterId();var lowFire=gm.DebugFirstEncounterObject(lowFireEncounter,"Fire Token");
            Check(gm.DebugEncounterBlockingObjectCount(lowFireEncounter)==1,"EN007 Severity 1-2 stays active until its single Fire object is extinguished.");
            Check(lowFire!=null&&gm.DebugInteractEncounterObject(hero.Id,lowFire.ObjectId)&&gm.PartyGold==goldBefore+35,"EN007 Severity 1-2 pays 10 Gold on extinguish and 25 Gold when the now-cleared encounter resolves.");

            // EN007 consequence: leaving a Fire Token when combat ends damages every Hero and removes the remaining fire.
            gm.DebugSetEncounterDeckTop("EN007");int heroHp=hero.Health,otherHp=other.Health;gm.DebugResolveTravelEncounter(settlement.Id,hero.Id,2); // Severity 3
            string burningEncounter=gm.DebugLatestActiveEncounterId();var burningEnemy=gm.DebugEncounterEnemy(burningEncounter);if(burningEnemy!=null)gm.DealDirectDamage(burningEnemy,burningEnemy.Health,"0.8.22 burning consequence",hero);
            Check(gm.DebugEncounterObjectCount(burningEncounter,"Fire Token")==0,"EN007 removes leftover Fire Tokens when the encounter ends.");
            Check(hero.Health==Mathf.Max(0,heroHp-1)&&other.Health==Mathf.Max(0,otherHp-1),"Each Hero suffers 1 damage when one EN007 Fire Token remains at encounter end.");

            // EN012: Web is a blocking objective and keeps no-combat encounter active until cleared.
            hero.LocationId=forest.Id;
            gm.DebugSetEncounterDeckTop("EN012");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1); // Severity 2, no combat
            string webEncounter=gm.DebugLatestActiveEncounterId();
            Check(gm.ActiveEncounters.Any(e=>e.InstanceId==webEncounter)&&gm.DebugEncounterBlockingObjectCount(webEncounter)==1,"EN012 Severity 1-2 remains active because Spider Webs are a blocking Encounter object.");
            Check(hero.HasStatus("Rooted"),"EN012 begins with the active Hero Rooted by the Web.");
            var web=gm.DebugFirstEncounterObject(webEncounter,"Web");Check(web!=null&&gm.DebugInteractEncounterObject(hero.Id,web.ObjectId),"Spider Webs can be cleared through the shared object interaction path.");
            Check(!hero.HasStatus("Rooted"),"Clearing Spider Webs removes the Rooted condition they created.");
            Check(!gm.ActiveEncounters.Any(e=>e.InstanceId==webEncounter),"The no-combat Spider Webs encounter clears only after its blocking Web is removed.");

            // Object presentation/persistence contract.
            Check(Object.FindObjectsByType<EncounterObjectAvatar>().Length>=gm.EncounterObjects.Count,"Every live Encounter object has a world presentation after runtime spawning.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.22 TEST] PASS — Encounter Hazards & Persistent Objects completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.22 TEST] FAIL — Encounter Hazards & Persistent Objects completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
