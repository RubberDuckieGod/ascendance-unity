#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class EncounterLifecycleTestTools0821
    {
        [MenuItem("Ascendance/Validation/Test 0.8.21 - Encounter Lifecycle & Rewards (Play Mode)")]
        public static void TestEncounterLifecycle()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.21 TEST] FAIL — Enter Play Mode before running this test.");return;}
            var gm=AscendanceGameManager.Instance;
            if(gm==null){Debug.LogError("[ASCENDANCE][0.8.21 TEST] FAIL — GameManager not found.");return;}
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.21 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.21 TEST] FAIL — {message}");}}

            var hero=gm.ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated);
            if(hero==null){Debug.LogError("[ASCENDANCE][0.8.21 TEST] FAIL — Need at least one living participating Hero.");return;}
            if(gm.HasPendingReactionWindow){Debug.LogError("[ASCENDANCE][0.8.21 TEST] FAIL — Close any Reaction window before running this test.");return;}

            var forest=gm.Locations["L002"];forest.Revealed=true;hero.LocationId=forest.Id;

            // EN001 Severity 2: one E001. Reward must be held until that exact spawned Enemy dies.
            gm.DebugSetEncounterDeckTop("EN001");
            int goldBefore=gm.PartyGold,completedBefore=gm.CompletedEncounterCount,activeBefore=gm.DebugActiveEncounterCount();
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string firstId=gm.DebugLatestActiveEncounterId();
            var firstEnemy=gm.DebugEncounterEnemy(firstId);
            Check(gm.DebugActiveEncounterCount()==activeBefore+1&&firstEnemy!=null,"EN001 creates an explicit active Encounter instance tied to its spawned Enemy.");
            Check(gm.PartyGold==goldBefore,"EN001 victory Gold is held while its Encounter Enemy remains alive.");
            Check(gm.DebugEncounterRemaining(firstId)==1,"Active Encounter reports exactly one remaining spawned Enemy.");
            gm.DealDirectDamage(firstEnemy,firstEnemy.Health,"0.8.21 lifecycle validation",hero);
            Check(gm.DebugActiveEncounterCount()==activeBefore&&gm.CompletedEncounterCount==completedBefore+1,"Defeating the final owned Enemy clears exactly that Encounter instance.");
            Check(gm.PartyGold==goldBefore+15&&gm.LastCompletedEncounterCardId=="EN001","EN001 awards its 15 Gold only after the Encounter is cleared.");

            // EN002: killing an unrelated Quest enemy must not clear the active Encounter.
            gm.DebugSetEncounterDeckTop("EN002");
            goldBefore=gm.PartyGold;activeBefore=gm.DebugActiveEncounterCount();
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string secondId=gm.DebugLatestActiveEncounterId();
            var secondEnemy=gm.DebugEncounterEnemy(secondId);
            var unrelated=gm.Enemies.Values.FirstOrDefault(e=>!e.EncounterSpawned&&!e.Defeated&&e.Id!="Captain");
            if(unrelated!=null)gm.DealDirectDamage(unrelated,unrelated.Health,"0.8.21 unrelated enemy validation",null);
            Check(gm.ActiveEncounters.Any(e=>e.InstanceId==secondId),"Defeating an unrelated Q001 Enemy does not clear the active Travel Encounter.");
            gm.DealDirectDamage(secondEnemy,secondEnemy.Health,"0.8.21 lifecycle validation",hero);
            Check(!gm.ActiveEncounters.Any(e=>e.InstanceId==secondId)&&gm.PartyGold==goldBefore+20,"EN002 clears on its own spawned Enemy and then awards 20 Gold.");

            // EN001 Severity 7 owns three enemies, including Elite E006. Partial kills must not pay early.
            gm.DebugSetEncounterDeckTop("EN001");
            goldBefore=gm.PartyGold;activeBefore=gm.DebugActiveEncounterCount();
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,6);
            string eliteId=gm.DebugLatestActiveEncounterId();
            var owned=gm.Enemies.Values.Where(e=>e.EncounterInstanceId==eliteId&&!e.Defeated).ToList();
            Check(owned.Count==3&&owned.Count(e=>e.TemplateId=="E006")==1,"EN001 Severity 7 tracks its authored E006 + 2 E001 group as one Encounter.");
            gm.DealDirectDamage(owned[0],owned[0].Health,"0.8.21 partial clear",hero);
            gm.DealDirectDamage(owned[1],owned[1].Health,"0.8.21 partial clear",hero);
            Check(gm.ActiveEncounters.Any(e=>e.InstanceId==eliteId)&&gm.PartyGold==goldBefore,"Partial Encounter clearance does not award victory rewards early.");
            var last=owned[2];gm.DealDirectDamage(last,last.Health,"0.8.21 final clear",hero);
            Check(!gm.ActiveEncounters.Any(e=>e.InstanceId==eliteId),"Encounter clears only after all three owned Enemies are Defeated.");
            Check(gm.PartyGold==goldBefore+20,"EN001 Severity 7 awards 15 Gold + 5 Gold for its defeated Elite E006.");

            // EN006 Severity 5-6: reward is conditional on defeating its E012.
            gm.DebugSetEncounterDeckTop("EN006");
            goldBefore=gm.PartyGold;
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4); // + WE001 = Severity 5
            string snareId=gm.DebugLatestActiveEncounterId();
            var trapper=gm.DebugEncounterEnemy(snareId);
            Check(trapper!=null&&trapper.TemplateId=="E012","EN006 Severity 5 creates a lifecycle-owned E012 Goblin Trapper.");
            gm.DealDirectDamage(trapper,trapper.Health,"0.8.21 conditional reward",hero);
            Check(gm.PartyGold==goldBefore+15,"EN006 awards its conditional 15 Gold after its E012 is actually Defeated.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.21 TEST] PASS — Encounter Lifecycle & Reward Resolution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.21 TEST] FAIL — Encounter Lifecycle & Reward Resolution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
