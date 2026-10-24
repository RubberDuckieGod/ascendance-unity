#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class FullQuestRuntimeTestTools0856
    {
        [MenuItem("Ascendance/Validation/Test 0.8.56a - Full Quest Runtime Execution (Play Mode)")]
        public static void Test0856()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.56a TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.56a TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.56a TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.56a TEST] FAIL — "+label);}}

            FullContentCatalog0850.EnsureLoaded();
            Check(AscendanceGameManager.BuildVersion=="0.8.56a","Build identifies the Full Quest Runtime Execution milestone.");
            Check(FullContentCatalog0850.QuestCount==72,"full authored catalog contains all 72 Quest definitions.");
            Check(QuestContent0829.Definitions.Count==72,"live Quest registry exposes all 72 authored Quests.");
            Check(Enumerable.Range(1,72).Select(i=>$"Q{i:000}").All(id=>QuestContent0829.Definitions.ContainsKey(id)),"Quest IDs are contiguous Q001-Q072.");
            Check(Enumerable.Range(1,72).Select(i=>$"Q{i:000}").All(gm.IsQuestExecutable0856),"all Q001-Q072 are accepted by the live Quest runtime.");
            Check(Enumerable.Range(1,72).Select(i=>$"Q{i:000}").All(gm.DebugQuestBlueprintExecutable0856),"all Q001-Q072 have executable specialized/generalized Quest blueprints.");
            Check(QuestContent0829.Definitions["Q001"].Name=="Defend Stonebrook"&&QuestContent0829.Definitions["Q012"].Name.Contains("Goblin King"),"Q001-Q012 specialized Goblin War Quest definitions remain authoritative.");
            Check(FullContentCatalog0850.WorldEventCount==24&&CampaignContent0833.WorldEvents.Count==24,"all 24 authored World Events are registered.");
            Check(CampaignContent0845.Campaigns.Count==6,"all six authored campaign arcs are registered.");
            Check(CampaignContent0845.CampaignForQuest0845("Q001")=="C001"&&CampaignContent0845.CampaignForQuest0845("Q013")=="C002"&&CampaignContent0845.CampaignForQuest0845("Q025")=="C003"&&CampaignContent0845.CampaignForQuest0845("Q037")=="C004"&&CampaignContent0845.CampaignForQuest0845("Q049")=="C005"&&CampaignContent0845.CampaignForQuest0845("Q061")=="C006","Quest ranges map correctly across Campaigns I-VI.");
            Check(CampaignContent0833.WorldEvents["WE004"].Final=="Q012"&&CampaignContent0833.WorldEvents["WE008"].Final=="Q024"&&CampaignContent0833.WorldEvents["WE012"].Final=="Q036"&&CampaignContent0833.WorldEvents["WE016"].Final=="Q048"&&CampaignContent0833.WorldEvents["WE020"].Final=="Q060"&&CampaignContent0833.WorldEvents["WE024"].Final=="Q072","all six campaign finale Quests are mapped to their terminal World Events.");
            Check(FullContentCatalog0850.LootCount==190&&FullContentCatalog0850.EquipmentCount==144&&FullContentCatalog0850.SearchCardCount==100&&FullContentCatalog0850.EncounterCount==105&&FullContentCatalog0850.CraftingRecipeCount==200,"0.8.52-0.8.55 authored content catalogs remain intact.");

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);
            gm.DebugStartQuest0829("Q001");yield return null;
            Check(!gm.IsGenericQuestRuntime0856&&gm.QuestRuntime0856==null,"Q001 still runs through the specialized Quest runtime rather than the generalized layer.");
            gm.DebugStartQuest0829("Q012");yield return null;
            Check(!gm.IsGenericQuestRuntime0856&&gm.QuestRuntime0856==null,"Q012 remains on its specialized Goblin King runtime.");

            gm.DebugStartQuest0829("Q013");yield return null;
            Check(gm.IsGenericQuestRuntime0856&&gm.QuestRuntime0856!=null&&gm.CurrentCampaignId0845=="C002"&&gm.CurrentWorldEventId0833=="WE005","Q013 starts through the generalized runtime in Campaign II / WE005.");
            Check(gm.Locations.TryGetValue("L051",out var q13Start)&&q13Start.Revealed&&gm.Heroes["Garruk"].LocationId=="L051","Q013 dynamically builds and reveals authored start Location L051 Valewatch.");
            Check(gm.DebugQuestGoalCount0856()>=3&&gm.QuestObjectiveSummary0829.Contains("Evidence")&&gm.QuestObjectiveSummary0829.Contains("Surviv"),"Q013 exposes its authored Evidence, Survivor, and enemy victory requirements.");
            Check(!gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q013"&&(e.TemplateId=="E033"||e.TemplateId=="E034"||e.TemplateId=="E036")),"Q013 reveal/ambush enemies remain out of play before their authored triggers.");
            gm.DebugRevealQuestLocation0829("L055","Garruk");yield return null;
            Check(gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q013"&&e.TemplateId=="E033")==2&&gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q013"&&e.TemplateId=="E034")==1&&!gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q013"&&e.TemplateId=="E036"),"Q013 L055 reveal spawns its authored 2 Raiders + 1 Ambusher while E036 remains delayed.");
            gm.DebugAdvanceAllGenericGoals0856();yield return null;
            Check(gm.QuestComplete&&gm.DebugHasQuestCampaignFlag0856("Q013 COMPLETE"),"Q013 generalized victory resolves and persists its completion World State.");

            gm.DebugStartQuest0829("Q014");yield return null;
            Check(gm.DebugGenericQuestHasPressure0856()&&gm.QuestRuntime0856.PressureLimit==6,"Q014 Murder Track is a live 6-step failure pressure track.");
            gm.DebugSetGenericPressure0856(5);gm.DebugQuestEndEnemyPhase0829();yield return null;
            Check(gm.QuestFailed&&gm.QuestRuntime0856.Pressure==6,"Q014 fails when Murder Track reaches 6 before objectives are complete.");

            gm.DebugStartQuest0829("Q015");yield return null;
            Check(gm.DebugGenericQuestHasPressure0856()&&gm.QuestRuntime0856.PressureLimit==3,"Q015 Witness Harm is represented as its authored 3-step failure track.");
            int crownBefore=gm.DebugFactionReputation0856("Crown Watch");gm.DebugAdvanceAllGenericGoals0856();yield return null;
            Check(gm.QuestComplete&&gm.DebugFactionReputation0856("Crown Watch")==Mathf.Min(6,crownBefore+1),"Q015 victory applies +1 Crown Watch Reputation through the live faction runtime.");

            var savedQuest=gm.ExportQuestRuntime0856();
            var save=new AscendanceSaveData{QuestRuntime0856=savedQuest,QuestCampaignFlags0856=gm.QuestCampaignFlags0856.ToList(),FactionReputation0856=gm.ExportFactionReputation0856()};
            gm.DebugClearQuestPersistentState0856();
            Check(gm.QuestRuntime0856==null&&gm.QuestCampaignFlags0856.Count==0&&gm.DebugFactionReputation0856("Crown Watch")==0,"0.8.56 Quest persistent state can be cleared for load replacement.");
            gm.DebugRestoreQuestState0856(save);
            Check(gm.QuestRuntime0856!=null&&gm.QuestRuntime0856.QuestId=="Q015"&&gm.DebugFactionReputation0856("Crown Watch")>0&&gm.QuestCampaignFlags0856.Count>0,"generic Quest goals, campaign flags, and faction Reputation restore from save-facing state.");

            gm.DebugStartQuest0829("Q017");yield return null;
            Check(gm.QuestRuntime0856.Goals.Any(g=>g.LootIds.Contains("LT163")),"Q017 resolves the printed Quiet Crown Ledger name to LT163 as a live Quest Item objective.");
            gm.DebugStartQuest0829("Q020");yield return null;
            Check(gm.QuestRuntime0856.Goals.Any(g=>g.LootIds.Contains("LT164"))&&!gm.QuestRuntime0856.Goals.Any(g=>g.LootIds.Contains("LT113")),"Q020 maps the authored Royal Seal objective to LT164 Royal Seal of the First King without the shorter-name collision.");

            gm.DebugStartQuest0829("Q018");yield return null;
            Check(gm.QuestRuntime0856.Goals.Any(g=>g.Kind=="Interact"&&g.Label.Contains("Break the Seal"))&&!gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestId=="Q018"&&e.TemplateId=="E045"),"Q018 represents the Seal-break requirement and keeps E045 out of play until that progression resolves.");

            gm.DebugStartQuest0829("Q024");yield return null;
            Check(gm.CurrentCampaignId0845=="C002"&&gm.CurrentWorldEventId0833=="WE008"&&gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestId=="Q024"&&e.TemplateId=="E049")&&!gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestId=="Q024"&&e.TemplateId=="E050"),"Q024 starts its authored Phase I boss while E050 Veiled Sovereign remains out of play.");
            gm.DebugAdvanceAllGenericGoals0856();yield return null;
            Check(gm.QuestComplete&&gm.DebugHasQuestCampaignFlag0856("Wardens of Valewatch")&&gm.DebugHasQuestCampaignFlag0856("Campaign II"),"Q024 victory preserves its faction unlock and Campaign II completion World State.");

            string[] representative={"Q025","Q037","Q049","Q061"};
            string[] campaigns={"C003","C004","C005","C006"};
            string[] starts={"L088","L125","L162","L199"};
            for(int i=0;i<representative.Length;i++)
            {
                gm.DebugStartQuest0829(representative[i]);yield return null;
                Check(gm.CurrentCampaignId0845==campaigns[i]&&gm.Locations.TryGetValue(starts[i],out var start)&&start.Revealed&&gm.Heroes["Garruk"].LocationId==starts[i],$"{representative[i]} starts in the correct later campaign and authored hub Location {starts[i]}.");
            }

            gm.DebugStartQuest0829("Q034");yield return null;
            Check(gm.QuestRuntime0856.Goals.Any(g=>g.Kind=="Interact"&&g.Target==2&&g.Label.IndexOf("facilit",StringComparison.OrdinalIgnoreCase)>=0),"Q034 preserves 'save at least 2 of the 3 facilities' as a two-step authored objective.");
            gm.DebugStartQuest0829("Q037");yield return null;
            Check(gm.QuestRuntime0856.Goals.Any(g=>g.Kind=="Clear"&&g.LocationIds.Contains("L134")),"Q037 preserves 'defeat all enemies in L134' as a separate clear objective.");
            gm.DebugStartQuest0829("Q064");yield return null;
            Check(gm.QuestRuntime0856.Goals.Any(g=>g.Kind=="Interact"&&g.Label.IndexOf("Crownshard Cache",StringComparison.OrdinalIgnoreCase)>=0),"Q064 preserves securing the Crownshard Cache as an explicit authored objective.");

            gm.DebugStartQuest0829("Q036");yield return null;
            Check(gm.QuestRuntime0856.FinalChoiceOptions.Count>=3&&gm.QuestRuntime0856.FinalChoiceOptions.Any(x=>x.IndexOf("Seal",StringComparison.OrdinalIgnoreCase)>=0),"Q036 exposes the authored Mountain Heart final-choice options.");
            Check(gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestId=="Q036"&&e.TemplateId=="E067")&&!gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestId=="Q036"&&e.TemplateId=="E068"),"Q036 preserves the authored E067 → delayed E068 boss-phase setup.");

            gm.DebugStartQuest0829("Q048");yield return null;
            Check(gm.QuestRuntime0856.FinalChoiceOptions.Count>=3,"Q048 exposes Purge / Seal / Reconcile as an authored finale choice.");
            gm.DebugStartQuest0829("Q060");yield return null;
            Check(gm.QuestRuntime0856.FinalChoiceOptions.Count>=3,"Q060 exposes Bind / Banish / Bargain as an authored finale choice.");
            gm.DebugStartQuest0829("Q072");yield return null;
            Check(gm.CurrentCampaignId0845=="C006"&&gm.CurrentWorldEventId0833=="WE024"&&gm.QuestRuntime0856.FinalChoiceOptions.Count>=3,"Q072 runs as Campaign VI / WE024 and exposes Restore / Shatter / Bear the Crown.");
            Check(gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestId=="Q072"&&e.TemplateId=="E124")&&!gm.Enemies.Values.Any(e=>!e.Defeated&&e.QuestId=="Q072"&&e.TemplateId=="E125"),"Q072 preserves the authored E124 → delayed E125 final boss-phase setup.");
            gm.DebugAdvanceAllGenericGoals0856();yield return null;
            Check(gm.QuestComplete&&gm.DebugHasQuestCampaignFlag0856("Dawnbearers of Sunspire")&&gm.DebugHasQuestCampaignFlag0856("Campaign VI"),"Q072 victory preserves the Dawnbearers unlock and six-campaign finale World State.");

            Check(string.IsNullOrEmpty(gm.PendingContentInstruction),"0.8.56a validation path finishes without a Quest CONTENT PENDING fallback.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.56a TEST] PASS — Full Quest Runtime Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.56a TEST] FAIL — Full Quest Runtime Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
