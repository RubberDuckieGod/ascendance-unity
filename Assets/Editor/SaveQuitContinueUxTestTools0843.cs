#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class SaveQuitContinueUxTestTools0843
    {
        [MenuItem("Ascendance/Validation/Test 0.8.43 - Save Quit Continue UX (Play Mode)")]
        public static void TestUx()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.43 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.43 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.43 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.43 TEST] FAIL — "+label);}}
            string path=AscendanceSaveSystem.SavePath;bool hadSave=File.Exists(path);byte[] previous=hadSave?File.ReadAllBytes(path):null;
            try
            {
                Check(AscendanceGameManager.BuildVersion=="0.8.43","Build identifies the Save / Quit / Continue UX milestone.");
                Check(CampaignSaveUx0843.SaveSafetySummary0843(gm).Contains("Safe to save"),"Save UX reports a normal idle runtime as safe to save.");
                int gold=gm.PartyGold;int round=gm.Round;string quest=gm.CurrentQuestId0829;
                Check(CampaignSaveUx0843.SaveWithFeedback0843(gm,out var saveMessage),"Manual player-facing save succeeds through the authoritative save system.");
                Check(saveMessage.Contains("SAVED")&&saveMessage.Contains("•"),"Successful save feedback includes a compact player-facing status line.");
                Check(saveMessage.Contains(CampaignResumeRuntime0842.DescribeSurface0842(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm))),"Save feedback identifies the exact Continue destination.");
                Check(AscendanceSaveSystem.HasValidSave,"Player-facing save produces a valid Continue campaign.");
                Check(gm.PartyGold==gold&&gm.Round==round&&gm.CurrentQuestId0829==quest,"Saving does not mutate authoritative gameplay state.");

                Check(CampaignSaveUx0843.TryAutoSave0843(gm,"VALIDATOR",out var autoMessage),"Safe autosave succeeds in a saveable state.");
                Check(CampaignSaveUx0843.LastAutoSaveSucceeded0843&&CampaignSaveUx0843.LastAutoSaveStatus0843==autoMessage,"Autosave records its latest player/support status.");
                Check(autoMessage.Contains("AUTOSAVE")&&autoMessage.Contains("VALIDATOR"),"Autosave feedback records why the save occurred.");

                var pause=Object.FindAnyObjectByType<PauseMenuController>();
                Check(pause!=null,"Stonebrook has the production Pause menu controller.");
                pause?.DebugOpenQuitToMenuConfirmation0843();yield return null;
                Check(pause!=null&&pause.DebugConfirmationVisible0843,"Quit to Menu opens a confirmation instead of leaving immediately.");
                Check(pause!=null&&pause.DebugConfirmationTitle0843.Contains("MAIN MENU"),"Quit-to-menu confirmation clearly names the destination.");
                Check(pause!=null&&pause.DebugConfirmationBody0843.Contains("SAVE & QUIT")&&pause.DebugConfirmationBody0843.Contains("WITHOUT SAVING"),"Quit confirmation explains both save and no-save choices.");
                Check(pause!=null&&pause.DebugConfirmationPrimaryEnabled0843,"SAVE & QUIT is enabled while the current state is safely saveable.");
                pause?.DebugCancelConfirmation0843();yield return null;
                Check(pause!=null&&!pause.DebugConfirmationVisible0843,"Cancel closes the quit confirmation without leaving gameplay.");

                pause?.DebugOpenQuitGameConfirmation0843();yield return null;
                Check(pause!=null&&pause.DebugConfirmationVisible0843&&pause.DebugConfirmationTitle0843.Contains("QUIT ASCENDANCE"),"Quit Game uses its own explicit confirmation.");
                pause?.DebugCancelConfirmation0843();yield return null;
                Check(pause!=null&&!pause.DebugConfirmationVisible0843,"Quit Game confirmation can be canceled safely.");

                gm.DebugResolveCampaignQuest0833(true);yield return null;
                Check(QuestResultsUI0835.Instance!=null&&QuestResultsUI0835.Instance.IsOpen,"Resolved Quest exposes the production Results surface.");
                QuestResultsUI0835.Instance?.SaveCampaign();yield return null;
                Check(QuestResultsUI0835.Instance!=null&&QuestResultsUI0835.Instance.DebugSaveStatus0843.Contains("SAVED"),"Quest Results shows visible save confirmation.");
                Check(QuestResultsUI0835.Instance!=null&&QuestResultsUI0835.Instance.DebugSaveStatus0843.Contains("Quest Results"),"Quest Results save feedback identifies its resume destination.");

                QuestResultsUI0835.Instance?.ReturnToCampaign();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen,"Return to Stonebrook reaches Downtime for save UX validation.");
                DowntimeUI0836.Instance?.SaveCampaign();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.DebugSaveStatus0843.Contains("SAVED"),"Downtime displays visible save confirmation.");
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.DebugSaveStatus0843.Contains("Stonebrook Downtime"),"Downtime save feedback identifies the exact resume surface.");

                Check(AscendanceSaveSystem.TryLoad(out var finalSave,out _)&&finalSave!=null,"Polished save UX still writes a loadable campaign save.");
                Check(AscendanceSaveSystem.MainMenuSummary0842.Contains("SAVED"),"Continue summary remains available after polished save flows.");
            }
            finally
            {
                try{if(hadSave){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,previous);}else if(File.Exists(path))File.Delete(path);}catch(System.Exception ex){Debug.LogException(ex);}
                Object.FindAnyObjectByType<PauseMenuController>()?.DebugResetPause0843();
                Time.timeScale=1f;
            }
            Check(File.Exists(path)==hadSave,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.43 TEST] PASS — Save / Quit / Continue UX completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.43 TEST] FAIL — Save / Quit / Continue UX completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
