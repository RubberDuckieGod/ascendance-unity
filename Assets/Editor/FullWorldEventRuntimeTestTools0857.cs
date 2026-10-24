#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class FullWorldEventRuntimeTestTools0857
    {
        [MenuItem("Ascendance/Validation/Test 0.8.57a - Full World Event Runtime Execution (Play Mode)")]
        public static void Test0857()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.57a TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.57a TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.57a TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.57a TEST] FAIL — "+label);}}

            FullContentCatalog0850.EnsureLoaded();
            Check(AscendanceGameManager.BuildVersion=="0.8.57a","Build identifies the Full World Event Runtime Execution milestone.");
            Check(FullContentCatalog0850.WorldEventCount==24,"full authored catalog contains all 24 World Event definitions.");
            Check(CampaignContent0833.WorldEvents.Count==24,"live World Event registry exposes all 24 authored events.");
            Check(Enumerable.Range(1,24).Select(i=>$"WE{i:000}").All(id=>CampaignContent0833.WorldEvents.ContainsKey(id)),"World Event IDs are contiguous WE001-WE024.");
            Check(Enumerable.Range(1,24).Select(i=>$"WE{i:000}").All(gm.IsWorldEventExecutable0857),"all WE001-WE024 have executable live runtime paths.");
            Check(Enumerable.Range(0,6).All(c=>CampaignContent0833.WorldEvents[$"WE{c*4+1:000}"].DayLimit==5&&CampaignContent0833.WorldEvents[$"WE{c*4+2:000}"].DayLimit==6&&CampaignContent0833.WorldEvents[$"WE{c*4+3:000}"].DayLimit==6&&CampaignContent0833.WorldEvents[$"WE{c*4+4:000}"].DayLimit==0),"all six campaigns preserve the authored 5/6/6/finale World Event cadence.");
            Check(CampaignContent0845.Campaigns.Count==6,"all six campaign arcs remain registered.");
            Check(FullContentCatalog0850.QuestCount==72&&FullContentCatalog0850.CraftingRecipeCount==200&&FullContentCatalog0850.LootCount==190&&FullContentCatalog0850.EquipmentCount==144,"0.8.54-0.8.56 authored content catalogs remain intact.");

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);yield return null;

            Check(gm.DebugWorldEventEnemyMovementBonus0857("WE001","E001")==1&&gm.DebugWorldEventEnemyMovementBonus0857("WE001","E001",true)==0,"WE001 gives Goblins +1 Movement only on their first activation each Round.");
            Check(gm.DebugWorldEventEnemyHealthDelta0857("WE002","E006")==1,"WE002 gives authored Goblin Leaders +1 Health.");
            Check(gm.DebugWorldEventEnemyAttackDelta0857("WE003","E002")==1,"WE003 gives Goblin Archers +1 Attack.");
            gm.DebugSetWorldEvent0857("WE004");
            Check(gm.DebugWorldEventTravelSuppressed0857("L001"),"WE004 suppresses random Travel Encounters during the Goblin King finale.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignII);
            gm.DebugApplyWorldEventResolution0857("WE005",false,false,false);
            Check(gm.DebugWorldEventMeter0857("Suspicion")==1&&gm.DebugHasWorldEventFlag0857("Q016 +1 E035"),"WE005 incomplete chapter applies Suspicion and the Q016 enemy penalty.");
            gm.DebugSetWorldEvent0857("WE006");gm.DebugStartQuest0829("Q016");yield return null;
            gm.DebugAddWorldEventFlag0857("Witness Testimony");gm.DebugWorldEventGoalProgress0857("Secure Evidence Ledger");
            Check(gm.DebugWorldEventMeter0857("Suspicion")==1,"WE006 Witness Testimony cancels the first eligible Suspicion gain in a Quest.");
            gm.DebugSetWorldEvent0857("WE007");
            Check(gm.DebugWorldEventEncounterSeverityModifier0857("WE007","EN037","L079",0)>=1,"WE007 raises rolled Underground/Crypt Travel Encounter Severity.");
            Check(gm.DebugWorldEventEncounterSeverityModifier0857("WE007","EN037","L079",4)==0,"WE007 leaves fixed Encounter Severity unchanged.");
            Check(gm.DebugWorldEventEnemyIgnoresDifficultTerrain0857("E040"),"WE007 Undead ignore Difficult Terrain movement cost.");
            Check(gm.DebugWorldEventEnemyHealthDelta0857("WE008","E036")==1,"WE008 gives eligible Elite Cult/Undead/Veil enemies +1 Health.");
            gm.DebugSetWorldEvent0857("WE008");
            Check(gm.DebugWorldEventTravelSuppressed0857("L079"),"WE008 suppresses random Travel Encounters during the Veiled Sovereign finale.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignII);gm.DebugApplyWorldEventResolution0857("WE006",true,true,true);gm.DebugSetWorldEvent0857("WE007");gm.DebugStartQuest0829("Q021");yield return null;
            int e041Before=gm.Enemies.Values.Count(e=>!e.Defeated&&e.TemplateId=="E041");int q021Before=gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestId=="Q021");
            gm.DebugWorldEventGoalProgress0857("Destroy all 3 Ritual Focus Tokens",1,3);
            Check(gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestId=="Q021")==q021Before,"WE006 Supply Network reward cancels the first Q021 Ritual Focus reinforcement.");
            gm.DebugWorldEventGoalProgress0857("Destroy all 3 Ritual Focus Tokens",2,3);
            Check(gm.Enemies.Values.Count(e=>!e.Defeated&&e.TemplateId=="E041")==e041Before+1,"Q021 second Ritual Focus spawns its authored E041 reinforcement after the WE006 cancellation is spent.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignII);gm.DebugStartQuest0829("Q024");yield return null;
            var q024Ward=gm.QuestRuntime0856?.Goals.FirstOrDefault(g=>g.Kind=="Interact"&&(g.Label??string.Empty).IndexOf("Crown Ward",StringComparison.OrdinalIgnoreCase)>=0);
            Check(q024Ward!=null&&q024Ward.Target==3,"Q024 parser preserves the printed three Crown Wards instead of reading L087 as a count.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignII);gm.DebugAddWorldEventFlag0857("Royal Seal Recovered");gm.DebugStartQuest0829("Q024");yield return null;
            q024Ward=gm.QuestRuntime0856?.Goals.FirstOrDefault(g=>g.Kind=="Interact"&&(g.Label??string.Empty).IndexOf("Crown Ward",StringComparison.OrdinalIgnoreCase)>=0);
            Check(q024Ward!=null&&q024Ward.Target==2,"WE007 Royal Seal reward removes one of Q024's three printed Crown Wards.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignII);gm.DebugAddWorldEventFlag0857("Q024 +1 Crown Ward");gm.DebugStartQuest0829("Q024");yield return null;
            q024Ward=gm.QuestRuntime0856?.Goals.FirstOrDefault(g=>g.Kind=="Interact"&&(g.Label??string.Empty).IndexOf("Crown Ward",StringComparison.OrdinalIgnoreCase)>=0);
            Check(q024Ward!=null&&q024Ward.Target==4,"WE007 incomplete-chapter penalty adds one Crown Ward to Q024's printed setup.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIV);gm.DebugStartQuest0829("Q040");yield return null;
            var q040Waystones=gm.QuestRuntime0856?.Goals.Where(g=>g.Kind=="Interact"&&(g.Label??string.Empty).IndexOf("Spirit Waystone",StringComparison.OrdinalIgnoreCase)>=0).ToList();
            Check(q040Waystones!=null&&q040Waystones.Count==1&&q040Waystones[0].Target==3,"Q040 creates one 3-Waystone objective instead of duplicate overlapping goals.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignVI);gm.DebugStartQuest0829("Q062");yield return null;
            var q062Caches=gm.QuestRuntime0856?.Goals.Where(g=>g.Kind=="Interact"&&(g.Label??string.Empty).IndexOf("Wagon supply cache",StringComparison.OrdinalIgnoreCase)>=0).ToList();
            Check(q062Caches!=null&&q062Caches.Count==1&&q062Caches[0].Target==1,"Q062 requires one Wagon supply cache and does not read LT185 as a numeric count or duplicate the goal.");
            gm.DebugStartQuest0829("Q072");yield return null;
            var q072Wards=gm.QuestRuntime0856?.Goals.Where(g=>g.Kind=="Interact"&&(g.Label??string.Empty).IndexOf("Solar Crown Ward",StringComparison.OrdinalIgnoreCase)>=0).ToList();
            Check(q072Wards!=null&&q072Wards.Count==1&&q072Wards[0].Target==3,"Q072 creates one 3-Solar-Crown-Ward objective instead of reading L235 as the count or duplicating the goal.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIII);
            Check(gm.DebugWorldEventMeter0857("Instability")==2,"Campaign III initializes Instability at 2.");
            gm.DebugApplyWorldEventResolution0857("WE009",true,true,true);
            Check(gm.DebugWorldEventMeter0857("Instability")==1&&gm.DebugHasWorldEventFlag0857("Survey Network Restored"),"WE009 perfect clear reduces Instability and restores the Survey Network.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIII);gm.DebugApplyWorldEventResolution0857("WE009",false,false,false);
            Check(gm.DebugWorldEventMeter0857("Instability")==3&&gm.DebugHasWorldEventFlag0857("Q028 +1 blocked-route token"),"WE009 incomplete chapter raises Instability and banks the Q028 route penalty.");
            gm.DebugStartQuest0829("Q028");yield return null;gm.DebugSetWorldEventMeter0857("Instability",2);gm.DebugAddWorldEventFlag0857("Q028 +1 blocked-route token");gm.DebugRevealWorldEventLocation0857("L102","Garruk");
            Check(gm.DebugHasWorldEventMarker0857("L102","Fracture"),"Q028 prior-chapter Fracture penalty places even below the normal Instability threshold.");
            Check(gm.DebugWorldEventMoveSurcharge0857("Garruk","L102")>=1,"WE010 Fracture markers charge the authored movement surcharge.");
            gm.DebugSetWorldEvent0857("WE009");gm.DebugSetWorldEventMeter0857("Instability",6);
            Check(gm.DebugWorldEventEnemyHealthDelta0857("WE009","E062",true,"first construct group")==1,"WE009 high Instability gives the first eligible scripted Construct/Deep group +1 Health.");
            gm.DebugStartQuest0829("Q031");yield return null;gm.DebugSetWorldEventMeter0857("Instability",6);int hpBefore=gm.Heroes["Garruk"].Health;gm.DebugWorldEventHeroEnter0857("Garruk","L114",1);
            Check(gm.Heroes["Garruk"].Health==hpBefore-1,"WE011 high Instability Forge/Magma entry deals 1 damage even without a roll.");
            gm.DebugSetWorldEvent0857("WE012");
            Check(gm.DebugWorldEventTravelSuppressed0857("L122")&&gm.DebugWorldEventTravelSuppressed0857("L124")&&!gm.DebugWorldEventTravelSuppressed0857("L101"),"WE012 suppresses random Travel only in its authored finale locations.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIV);
            Check(gm.DebugWorldEventMeter0857("Blight")==2,"Campaign IV initializes Blight at 2.");
            gm.DebugApplyWorldEventResolution0857("WE013",true,true,true);
            Check(gm.DebugWorldEventMeter0857("Blight")==1&&gm.DebugHasWorldEventFlag0857("First Cure Distilled"),"WE013 perfect clear reduces Blight and records First Cure Distilled.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIV);gm.DebugApplyWorldEventResolution0857("WE013",false,false,false);gm.DebugStartQuest0829("Q040");yield return null;gm.DebugSetWorldEventMeter0857("Blight",2);gm.DebugAddWorldEventFlag0857("Q040 +1 Blight Patch");gm.DebugRevealWorldEventLocation0857("L140","Garruk");
            Check(gm.DebugHasWorldEventMarker0857("L140","Blight Patch"),"Q040 prior-chapter Blight Patch penalty places independently of the normal Blight threshold.");
            gm.Heroes["Garruk"].LocationId="L140";
            Check(gm.DebugModifyWorldEventHealing0857("Garruk",3)==2,"WE014 Blight Patch suppresses the first 1 Health recovery at that Location each Round.");
            gm.DebugStartQuest0829("Q043");yield return null;
            Check(!gm.DebugHasWorldEventMarker0857("L140","Blight Patch"),"World Event route markers clear when a fresh Quest begins instead of leaking between scenarios.");
            gm.DebugSetWorldEventMeter0857("Blight",6);gm.DebugWorldEventHeroEnter0857("Garruk","L147",1);gm.DebugResolveWorldEventStatus0857("Garruk","Inspired",1);
            Check(!gm.Heroes["Garruk"].HasStatus("Inspired"),"WE015 high Blight Hollow Pressure can consume the first beneficial Status on a 1-2 roll.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIV);gm.DebugSetWorldEvent0857("WE015");gm.DebugStartQuest0829("Q043");yield return null;gm.DebugSetWorldEventMeter0857("Blight",6);gm.Heroes["Garruk"].LootInventory.Add("LT176");gm.Heroes["Garruk"].LocationId="L147";gm.DebugResolveWorldEventStatus0857("Garruk","Inspired",1);
            Check(gm.Heroes["Garruk"].HasStatus("Inspired"),"LT176 Heartseed Ward cancels one WE015 Hollow Pressure beneficial-Status loss per Round.");
            gm.DebugSetWorldEvent0857("WE016");
            Check(gm.DebugWorldEventTravelSuppressed0857("L159")&&gm.DebugWorldEventTravelSuppressed0857("L161"),"WE016 suppresses random Travel in its authored finale locations.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignV);
            Check(gm.DebugWorldEventMeter0857("Doomtide")==2,"Campaign V initializes Doomtide at 2.");
            gm.DebugApplyWorldEventResolution0857("WE017",true,true,true);
            Check(gm.DebugWorldEventMeter0857("Doomtide")==1&&gm.DebugHasWorldEventFlag0857("Harbor Channels Secured"),"WE017 perfect clear reduces Doomtide and secures Harbor Channels.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignV);gm.DebugApplyWorldEventResolution0857("WE017",false,false,false);gm.DebugStartQuest0829("Q052");yield return null;gm.DebugSetWorldEventMeter0857("Doomtide",2);gm.DebugAddWorldEventFlag0857("Q052 +1 Flood Marker");gm.DebugRevealWorldEventLocation0857("L168","Garruk");
            Check(gm.DebugHasWorldEventMarker0857("L168","Flood"),"Q052 prior-chapter Flood Marker penalty places independently of the normal Doomtide threshold.");
            Check(gm.DebugWorldEventMoveSurcharge0857("Garruk","L168")>=1,"WE018 Flood markers charge the authored movement surcharge.");
            gm.DebugStartQuest0829("Q055");yield return null;gm.DebugSetWorldEventMeter0857("Doomtide",6);hpBefore=gm.Heroes["Garruk"].Health;gm.DebugWorldEventHeroEnter0857("Garruk","L180",1);
            Check(gm.Heroes["Garruk"].Health==hpBefore-1,"WE019 high Doomtide exposed-route roll deals 1 damage on a 1-2 result when unguarded.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignV);gm.DebugSetWorldEvent0857("WE019");gm.DebugStartQuest0829("Q055");yield return null;gm.DebugSetWorldEventMeter0857("Doomtide",6);gm.Heroes["Garruk"].LootInventory.Add("LT182");hpBefore=gm.Heroes["Garruk"].Health;gm.DebugWorldEventHeroEnter0857("Garruk","L180",1);
            Check(gm.Heroes["Garruk"].Health==hpBefore,"LT182 Beacon Lens suppresses one WE019 Tempest Pressure entry roll per Round.");
            gm.DebugSetWorldEvent0857("WE020");
            Check(gm.DebugWorldEventTravelSuppressed0857("L196")&&gm.DebugWorldEventTravelSuppressed0857("L198"),"WE020 suppresses random Travel in its authored finale locations.");

            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignVI);
            Check(gm.DebugWorldEventMeter0857("Sunfall")==2,"Campaign VI initializes Sunfall at 2.");
            gm.DebugApplyWorldEventResolution0857("WE021",true,true,true);
            Check(gm.DebugWorldEventMeter0857("Sunfall")==1&&gm.DebugHasWorldEventFlag0857("Eastern Roads Secured"),"WE021 perfect clear reduces Sunfall and secures the Eastern Roads.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignVI);gm.DebugApplyWorldEventResolution0857("WE021",false,false,false);gm.DebugStartQuest0829("Q064");yield return null;gm.DebugSetWorldEventMeter0857("Sunfall",2);gm.DebugAddWorldEventFlag0857("Q064 +1 Mirage Marker");gm.DebugRevealWorldEventLocation0857("L213","Garruk");
            Check(gm.DebugHasWorldEventMarker0857("L213","Mirage"),"Q064 prior-chapter Mirage Marker penalty places independently of the normal Sunfall threshold.");
            Check(gm.DebugWorldEventMoveSurcharge0857("Garruk","L213")>=1,"WE022 Mirage markers charge the first-entry movement surcharge each Round.");
            gm.DebugStartQuest0829("Q067");yield return null;gm.DebugSetWorldEventMeter0857("Sunfall",6);gm.Heroes["Garruk"].LocationId="L221";
            Check(gm.DebugModifyWorldEventHealing0857("Garruk",3)==2,"WE023 high Sunfall reduces the first Desert/Solar healing each Round by 1.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignVI);gm.DebugSetWorldEvent0857("WE023");gm.DebugStartQuest0829("Q067");yield return null;gm.DebugSetWorldEventMeter0857("Sunfall",6);gm.Heroes["Garruk"].LootInventory.Add("LT188");gm.Heroes["Garruk"].LocationId="L221";
            Check(gm.DebugModifyWorldEventHealing0857("Garruk",3)==3,"LT188 First Dawn Prism cancels one WE023 Eclipse Pressure recovery reduction per Round.");
            Check(!string.IsNullOrWhiteSpace(gm.CurrentWorldEventAuthoredPreview0857)&&gm.CurrentWorldEventAuthoredPreview0857.Contains("SUNFALL"),"Campaign Hub World Event preview exposes later-campaign authored effects and the active pressure meter.");
            gm.DebugSetWorldEvent0857("WE024");
            Check(gm.DebugWorldEventTravelSuppressed0857("L233")&&gm.DebugWorldEventTravelSuppressed0857("L235"),"WE024 suppresses random Travel in its authored finale locations.");
            gm.DebugResolveFinalWorldEvent0857(true);
            Check(gm.DebugHasWorldEventFlag0857("Dawnbearers of Sunspire")&&gm.DebugHasWorldEventFlag0857("Final Sunfall"),"WE024 victory records the authored Dawnbearers unlock and final Sunfall state.");

            Check(typeof(CampaignProgressState0833).GetField("Instability0857")!=null&&typeof(CampaignProgressState0833).GetField("Blight0857")!=null&&typeof(CampaignProgressState0833).GetField("Doomtide0857")!=null&&typeof(CampaignProgressState0833).GetField("Sunfall0857")!=null,"live Campaign state persists all four long-form Campaign III-VI pressure meters.");
            Check(typeof(CampaignProgressSaveData0833).GetField("WorldEventFlags0857")!=null&&typeof(CampaignProgressSaveData0833).GetField("WorldEventOnceKeys0857")!=null&&typeof(CampaignProgressSaveData0833).GetField("Suspicion0857")!=null,"save-facing Campaign state includes World Event flags, once-keys, and Suspicion.");
            Check(string.IsNullOrWhiteSpace(gm.PendingContentInstruction),"0.8.57a validation path finishes without a World Event CONTENT PENDING fallback.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.57a TEST] PASS — Full World Event Runtime Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.57a TEST] FAIL — Full World Event Runtime Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
