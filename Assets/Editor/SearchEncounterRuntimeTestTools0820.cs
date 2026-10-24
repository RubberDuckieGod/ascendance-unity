#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class SearchEncounterRuntimeTestTools0820
    {
        [MenuItem("Ascendance/Validation/Test 0.8.20 - Search & Travel Runtime (Play Mode)")]
        public static void TestSearchTravelRuntime()
        {
            if(!Application.isPlaying)
            {
                Debug.LogError("[ASCENDANCE][0.8.20 TEST] FAIL — Enter Play Mode before running this test.");
                return;
            }
            var gm=AscendanceGameManager.Instance;
            if(gm==null){Debug.LogError("[ASCENDANCE][0.8.20 TEST] FAIL — GameManager not found.");return;}
            int pass=0,fail=0;
            void Check(bool condition,string message)
            {
                if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.20 TEST] PASS — {message}");}
                else{fail++;Debug.LogError($"[ASCENDANCE][0.8.20 TEST] FAIL — {message}");}
            }

            Check(SearchEncounterContent0820.SearchCards.Count==20,"SD001-SD020 authored Search batch is loaded (20 unique cards).");
            Check(SearchEncounterContent0820.EncounterCards.Count==30,"EN001-EN030 Campaign I Travel Encounter deck is loaded (30 unique cards).");
            Check(gm.DebugMatchesSearchLocation("SD002","L001"),"Any-terrain Search cards are eligible at River Crossing.");
            Check(gm.DebugMatchesEncounterLocation("EN010","L001")&&!gm.DebugMatchesEncounterLocation("EN010","L004"),"Travel Encounter terrain filtering accepts River/Road and rejects Dense Woods for EN010.");

            var hero=gm.ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated);
            if(hero==null){Debug.LogError("[ASCENDANCE][0.8.20 TEST] FAIL — Need at least one living participating Hero.");return;}
            if(gm.TurnHero!=null&&gm.TurnHero!=hero){Debug.LogError("[ASCENDANCE][0.8.20 TEST] FAIL — Run at the beginning of a Hero Phase with no active turn.");return;}

            hero.LocationId="L001";
            gm.Locations["L001"].Revealed=true;
            gm.DebugResetLocationSearch("L001");
            gm.SelectHero(hero.Id);
            if(gm.TurnHero==null)gm.StartSelectedHeroTurn();
            Check(gm.TurnHero==hero&&hero.Actions>0,"Hero starts a legal turn for Search runtime validation.");

            gm.DebugSetSearchDeckTop("SD002");
            int goldBefore=gm.PartyGold, actionsBefore=hero.Actions, searchDiscardBefore=gm.SearchDiscard.Count;
            gm.SearchCurrentLocation();
            Check(gm.LastSearchCardId=="SD002"&&gm.PartyGold==goldBefore+15,"SD002 Forgotten Coin Purse resolves its authored +15 Gold effect.");
            Check(hero.Actions==actionsBefore-1&&gm.Locations["L001"].Searched,"A normal Search still costs exactly 1 Action and consumes the Location Search.");
            Check(gm.SearchDiscard.Count==searchDiscardBefore+1&&gm.SearchDiscard.Contains("SD002"),"Resolved Search card moves from draw pile to Search discard state.");

            var ruins=gm.Locations["L003"];
            ruins.Revealed=true;ruins.Searched=false;
            hero.FreeSearchLocationId=ruins.Id;
            gm.DebugSetSearchDeckTop("SD014");
            gm.DebugSetEncounterDeckTop("EN017");
            actionsBefore=hero.Actions;goldBefore=gm.PartyGold;int encounterDiscardBefore=gm.EncounterDiscard.Count;
            gm.SearchCurrentLocation();
            Check(gm.LastSearchCardId=="SD014"&&gm.PartyGold>=goldBefore+25,"SD014 Smuggler's Stash grants 25 Gold before chaining into Travel Encounter resolution.");
            Check(hero.Actions==actionsBefore&&ruins.Searched,"Ancient Ruins' free Search remains free while resolving a real Search card.");
            Check(gm.LastEncounterCardId=="EN017"&&gm.EncounterDiscard.Count==encounterDiscardBefore+1,"SD014 immediately draws and resolves one terrain-eligible Travel Encounter.");
            Check(!string.IsNullOrEmpty(gm.PendingContentInstruction),"Unmigrated Loot/Equipment/choice payloads are retained as explicit pending content instead of disappearing.");

            var forest=gm.Locations["L002"];
            forest.Revealed=true;hero.LocationId=forest.Id;
            gm.DebugSetEncounterDeckTop("EN001");
            int raidersBefore=gm.DebugEncounterSpawnCount("E001");
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            var spawnedRaider=gm.Enemies.Values.LastOrDefault(e=>e.EncounterSpawned&&e.TemplateId=="E001");
            Check(gm.LastEncounterCardId=="EN001"&&gm.LastEncounterSeverity==2,"WE001 Travel severity is d6 1 + printed World Event modifier 1 = Severity 2.");
            Check(gm.DebugEncounterSpawnCount("E001")==raidersBefore+1&&spawnedRaider!=null,"EN001 Severity 1-2 spawns exactly one authored E001 Goblin Raider.");
            Check(spawnedRaider!=null&&spawnedRaider.LocationId==forest.Id&&spawnedRaider.MaxHealth==6&&spawnedRaider.Health==6&&spawnedRaider.Attack==2&&spawnedRaider.Armor==1,"Encounter-spawned E001 uses authored Health 6 / Attack 2 / Armor 1 at the triggering Location.");

            gm.DebugSetEncounterDeckTop("EN006");
            int penaltyBefore=hero.NextTurnActionPenalty;
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            Check(hero.NextTurnActionPenalty==penaltyBefore+1,"EN006 Severity 1-2 stores the authored 1-Action next-turn penalty.");

            gm.DebugApplyHeroDamage(hero.Id,2,"0.8.20 Quiet Road setup");
            int damagedHealth=hero.Health;
            gm.DebugSetEncounterDeckTop("EN030");
            gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            Check(gm.LastEncounterCardId=="EN030"&&gm.LastEncounterSeverity==2&&hero.Health==Mathf.Min(hero.MaxHealth,damagedHealth+1),"EN030 Quiet Road Severity 1-2 recovers exactly 1 Health for Heroes at the encounter Location.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.20 TEST] PASS — Search & Travel Encounter Runtime completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.20 TEST] FAIL — Search & Travel Encounter Runtime completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
