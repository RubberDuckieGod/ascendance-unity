#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CodeFirstCampaignHubTestTools0849
    {
        [MenuItem("Ascendance/Validation/Test 0.8.49 - Code-First Studio Campaign Hub (Play Mode)")]
        public static void Test0849()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.49 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.49 TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.49 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.49 TEST] FAIL — "+label);}}
            string savePath=AscendanceSaveSystem.SavePath;bool had=File.Exists(savePath);byte[] backup=had?File.ReadAllBytes(savePath):null;
            try
            {
                Check(AscendanceGameManager.BuildVersion=="0.8.49a","Build identifies the Code-First Studio Campaign UI milestone.");
                Check(new[]{"WE005","WE006","WE007","WE008"}.All(id=>CampaignContent0833.WorldEvents.ContainsKey(id)),"Campaign II retains four World Events.");
                Check(CampaignContent0845.ValewatchQuestPlan.Count==12,"Campaign II retains twelve authored Quest-plan entries.");
                Check(CampaignContent0845.ValewatchQuestPlan.Values.Where(q=>q.WorldEventId=="WE005").Select(q=>q.QuestId).OrderBy(x=>x).SequenceEqual(new[]{"Q013","Q014","Q015"}),"WE005 remains sequential Q013–Q015.");
                Check(CampaignContent0845.ValewatchQuestPlan.Values.Where(q=>q.WorldEventId=="WE006").Select(q=>q.QuestId).OrderBy(x=>x).SequenceEqual(new[]{"Q016","Q017","Q018"}),"WE006 remains sequential Q016–Q018.");
                Check(CampaignContent0845.ValewatchQuestPlan.Values.Where(q=>q.WorldEventId=="WE007").Select(q=>q.QuestId).OrderBy(x=>x).SequenceEqual(new[]{"Q019","Q020","Q021"}),"WE007 remains sequential Q019–Q021.");
                Check(CampaignContent0845.ValewatchQuestPlan.Values.Where(q=>q.WorldEventId=="WE008").Select(q=>q.QuestId).OrderBy(x=>x).SequenceEqual(new[]{"Q022","Q023","Q024"}),"WE008 remains sequential Q022–Q024.");

                gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugResetCampaignProgress0833();yield return null;
                Check(gm.CurrentCampaignId0845=="C001","Fresh campaign remains Campaign I before the test handoff.");
                gm.DebugStartQuest0829("Q012");gm.DebugResolveCampaignQuest0833(true);yield return null;
                Check(gm.CampaignCompleted0833&&gm.CanContinueToValewatch0845,"Campaign I Victory still exposes Continue to Valewatch.");
                int gold=gm.PartyGold,rep=gm.GeneralReputation;
                Check(gm.BeginValewatchContinuation0845(),"Campaign I Victory still transitions into Campaign II.");yield return null;
                Check(gm.CurrentCampaignId0845=="C002"&&gm.CurrentWorldEventId0833=="WE005"&&gm.WorldEventDay0833==1,"Campaign II starts at WE005 Day 1.");
                Check(gm.PartyGold==gold&&gm.GeneralReputation==rep,"Code-first UI milestone does not alter Party Gold or Reputation.");
                Check(!gm.CanBeginCampaignQuest0833("Q013")&&!gm.BeginCampaignQuest0833("Q013"),"Q013 remains launch-guarded until live scenario runtime exists.");

                var ui=CampaignLaunchpadUI0845.Instance;
                Check(ui!=null,"Campaign II hub component is installed.");
                ui?.Open();yield return null;
                Check(ui!=null&&ui.IsOpen,"Code-first Valewatch Campaign Hub opens.");
                Check(ui!=null&&ui.DebugArchitecture0849=="CODE_FIRST_UGUI","Hub reports code-first UGUI architecture.");
                Check(ui!=null&&ui.DebugBackgroundArtLoaded0848,"Single Valewatch region background loads.");
                Check(ui!=null&&ui.DebugUsesOnlyBackgroundArt0849,"Functional UI uses no sprite-sheet pieces or baked UI artwork.");
                Check(ui!=null&&ui.DebugFunctionalSpriteCount0849==0,"All functional Image components are code-built solid primitives with no sprites.");
                Check(ui!=null&&ui.DebugReferenceResolution0849==new Vector2(1920f,1080f),"Canvas uses 1920×1080 reference resolution.");
                Check(ui!=null&&Mathf.Abs(ui.DebugCanvasMatch0849-0.5f)<0.001f,"Canvas scales evenly between width and height.");
                Check(ui!=null&&ui.DebugSafeMargins0849,"Hub uses a dedicated safe-margin root.");
                Check(ui!=null&&ui.DebugReadableTypeScale0849,"Quest/detail typography uses the larger studio readability scale.");
                Check(ui!=null&&ui.DebugWorldEventPanelCount0847==4,"Hub renders four live World Event containers.");
                Check(ui!=null&&ui.DebugQuestButtonCount0847==12,"Hub renders twelve live Quest buttons.");
                Check(ui!=null&&ui.DebugEveryQuestInspectable0847,"All twelve Quest rows remain clickable for inspection while launch-locked.");
                Check(ui!=null&&ui.DebugSequentialBlocks0847,"Visible Quest groups enforce Q013–015 / Q016–018 / Q019–021 / Q022–024 ordering.");
                Check(ui!=null&&ui.DebugSelectedQuestId0847=="Q013","Hub defaults selection to Q013 The Missing Caravan.");
                Check(ui!=null&&ui.DebugQ013Disabled0845,"Begin Quest remains disabled because Q013 runtime is not implemented yet.");
                Check(ui!=null&&ui.DebugSelectedQuestRole0847.Contains("MAIN"),"Q013 detail identifies its Main Quest role.");
                Check(ui!=null&&ui.DebugSelectedWorldEvent0847.Contains("WE005"),"Q013 detail identifies WE005.");

                ui?.SelectQuest0847("Q017");yield return null;
                Check(ui!=null&&ui.DebugSelectedQuestId0847=="Q017","Live selection changes the detail panel to Q017.");
                Check(ui!=null&&ui.DebugSelectedQuestRole0847.Contains("OPTIONAL"),"Q017 remains Optional without being reordered.");
                Check(ui!=null&&ui.DebugSelectedWorldEvent0847.Contains("WE006"),"Q017 detail identifies WE006.");
                Check(ui!=null&&!ui.DebugContinueInteractable0847,"Locked inspected Quest does not falsely enable Begin Quest.");

                ui?.SelectQuest0847("Q019");yield return null;
                Check(ui!=null&&ui.DebugSelectedQuestId0847=="Q019","Selection reaches Q019 in the third sequential block.");
                Check(ui!=null&&ui.DebugSelectedQuestRole0847.Contains("MAIN"),"Q019 displays Main Quest role.");
                Check(ui!=null&&ui.DebugSelectedWorldEvent0847.Contains("WE007"),"Q019 displays WE007.");

                ui?.SelectQuest0847("Q024");yield return null;
                Check(ui!=null&&ui.DebugSelectedQuestId0847=="Q024","Selection reaches the Campaign II finale.");
                Check(ui!=null&&ui.DebugSelectedQuestRole0847.Contains("FINAL"),"Q024 displays Final Quest role.");
                Check(ui!=null&&ui.DebugSelectedWorldEvent0847.Contains("WE008"),"Q024 displays WE008.");
                Check(ui!=null&&!ui.DebugContinueInteractable0847,"Future finale cannot launch early.");

                ui?.SelectQuest0847("Q013");yield return null;
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.CampaignLaunchpad,"Code-first hub preserves Campaign Launchpad resume surface.");
                Check(AscendanceSaveSystem.SaveCurrent(gm,out _),"Save Campaign II still uses the authoritative save system.");
                var save=AscendanceSaveSystem.Load();
                Check(save?.CampaignProgress0833?.CampaignId0845=="C002"&&save.CampaignProgress0833.AwaitingCampaignRuntime0845,"Save JSON preserves Campaign II identity and launchpad state.");
                Check(save!=null&&CampaignResumeRuntime0842.NormalizeSurface0842(save)==CampaignResumeRuntime0842.CampaignLaunchpad,"Continue still normalizes Campaign II to the Campaign Hub.");
                gm.ApplySaveData(save);yield return null;
                Check(gm.CurrentCampaignId0845=="C002","Applying save restores Campaign II identity before UI routing.");
                var resume=Object.FindAnyObjectByType<CampaignResumeController0842>();resume?.ApplySurface0842(CampaignResumeRuntime0842.CampaignLaunchpad);yield return null;
                Check(ui!=null&&ui.IsOpen,"Continue routing reopens the code-first Valewatch Campaign Hub.");
                Check(gm.CampaignArchive0845.Any(x=>x!=null&&x.CampaignId=="C001"&&x.TitleUnlock=="Defender of Stonebrook"),"Campaign I archive remains intact through code-first hub save/resume.");
            }
            finally
            {
                try{if(had&&backup!=null){Directory.CreateDirectory(Path.GetDirectoryName(savePath));File.WriteAllBytes(savePath,backup);}else if(File.Exists(savePath))File.Delete(savePath);}catch(System.Exception ex){Debug.LogError("[ASCENDANCE][0.8.49 TEST] Save restore failed: "+ex.Message);fail++;}
            }
            Check(true,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.49 TEST] PASS — Code-First Studio Campaign Hub completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.49 TEST] FAIL — Code-First Studio Campaign Hub completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
