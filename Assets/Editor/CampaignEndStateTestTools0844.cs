#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CampaignEndStateTestTools0844
    {
        [MenuItem("Ascendance/Validation/Test 0.8.44 - Campaign Completion End-State (Play Mode)")]
        public static void TestEndState()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.44 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.44 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.44 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.44 TEST] FAIL — "+label);}}
            byte[] priorSave=null;bool hadSave=false;
            try{hadSave=File.Exists(AscendanceSaveSystem.SavePath);if(hadSave)priorSave=File.ReadAllBytes(AscendanceSaveSystem.SavePath);}catch(System.Exception ex){Debug.LogWarning("[ASCENDANCE][0.8.44 TEST] Save backup skipped: "+ex.Message);}

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);
            var end=Object.FindAnyObjectByType<CampaignEndUI0844>();
            var results=Object.FindAnyObjectByType<QuestResultsUI0835>();
            var resume=Object.FindAnyObjectByType<CampaignResumeController0842>();
            Check(end!=null,"Stonebrook installs the dedicated Campaign End screen.");
            Check(end!=null&&!end.IsOpen,"Campaign End screen stays hidden while Campaign I is active.");

            gm.DebugResetCampaignProgress0833();gm.DebugSetGeneralReputation(0);gm.DebugStartQuest0829("Q012");
            var xpBefore=gm.ParticipatingHeroes.ToDictionary(h=>h.Id,h=>h.XP);var eqBefore=gm.ParticipatingHeroes.ToDictionary(h=>h.Id,h=>h.AcquiredEquipment.Count);
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.CampaignCompleted0833&&!gm.CampaignDefeated0833,"Q012 success remains the authoritative Campaign I Victory trigger.");
            Check(gm.AvailableCampaignQuests0833.Count==0&&!gm.IsDowntimeAvailable0836,"Victory closes further Campaign I Quest selection and Downtime.");
            Check(gm.ParticipatingHeroes.All(h=>h.XP-xpBefore[h.Id]==300),"Victory still grants exactly 300 XP to each participating Hero.");
            Check(gm.ParticipatingHeroes.All(h=>h.AcquiredEquipment.Count==eqBefore[h.Id]+1),"Victory still grants exactly one Legendary Equipment card per participating Hero.");
            Check(gm.GeneralReputation==3,"Victory applies exactly +3 General Reputation through the existing Q012 reward chain.");
            Check(results!=null&&results.IsOpen&&results.DebugHeading0835.Contains("CAMPAIGN I VICTORY"),"Q012 first presents the verified Campaign Victory result.");

            results?.ReturnToCampaign();yield return null;
            Check(end!=null&&end.IsOpen,"VIEW CAMPAIGN END routes terminal Quest Results to the final Campaign screen.");
            Check(end!=null&&end.DebugHeading0844.Contains("VICTORY"),"Final screen clearly identifies Campaign I Victory.");
            Check(end!=null&&end.DebugTitle0844.Contains("DEFENDER OF STONEBROOK"),"Victory screen displays the Defender of Stonebrook title unlock.");
            Check(end!=null&&end.DebugRewards0844.Contains("300")&&end.DebugRewards0844.ToUpperInvariant().Contains("LEGENDARY"),"Victory screen displays the already-resolved Q012 XP and Legendary rewards.");
            Check(end!=null&&end.DebugParty0844.Contains("Garruk")&&end.DebugParty0844.Contains("Sir Aldric"),"Final screen summarizes every participating Hero.");
            Check(end!=null&&end.DebugOverview0844.Contains("Campaign Days")&&end.DebugOverview0844.Contains("Final General Reputation"),"Final campaign record shows timing and persistent campaign totals.");
            Check(end!=null&&end.DebugQuestRecord0844.Contains("Q012"),"Final campaign record includes Q012 in the completed Quest history.");
            Check(end!=null&&end.DebugHasReturnToMenu0844&&end.DebugHasStartNewCampaign0844,"Final screen exposes Return to Main Menu and Start New Campaign actions.");
            Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.CampaignEnd,"Terminal campaign state captures CAMPAIGN_END as the Continue destination.");

            bool saved=end!=null&&AscendanceSaveSystem.SaveCurrent(gm,out _);var loaded=saved?AscendanceSaveSystem.Load():null;yield return null;
            Check(saved&&loaded!=null&&loaded.ResumeSurface0842==CampaignResumeRuntime0842.CampaignEnd,"Final Victory save persists CAMPAIGN_END.");
            Check(loaded!=null&&CampaignResumeRuntime0842.NormalizeSurface0842(loaded)==CampaignResumeRuntime0842.CampaignEnd,"Final Victory save normalizes to Campaign End on Continue.");
            Check(loaded!=null&&CampaignResumeRuntime0842.BuildMainMenuSummary0842(loaded).Contains("CAMPAIGN I VICTORY"),"Main Menu Continue summary identifies the saved Victory ending.");
            if(loaded!=null){gm.ApplySaveData(loaded);resume?.ApplySurface0842(CampaignResumeRuntime0842.NormalizeSurface0842(loaded));yield return null;}
            Check(end!=null&&end.IsOpen&&resume!=null&&resume.DebugLastAppliedSurface0842==CampaignResumeRuntime0842.CampaignEnd,"Continue routing restores the dedicated Campaign End screen.");

            int goldAfter=gm.PartyGold;var xpAfter=gm.ParticipatingHeroes.ToDictionary(h=>h.Id,h=>h.XP);var eqAfter=gm.ParticipatingHeroes.ToDictionary(h=>h.Id,h=>h.AcquiredEquipment.Count);
            for(int i=0;i<4;i++){end?.Close();end?.Open();gm.SyncCampaignProgress0833();}
            Check(gm.PartyGold==goldAfter&&gm.ParticipatingHeroes.All(h=>h.XP==xpAfter[h.Id]&&h.AcquiredEquipment.Count==eqAfter[h.Id]),"Reopening/refreshing Campaign End never duplicates Q012 rewards.");

            gm.DebugResetCampaignProgress0833();gm.DebugStartQuest0829("Q012");gm.DebugResolveCampaignQuest0833(false);yield return null;
            Check(gm.CampaignDefeated0833&&!gm.CampaignCompleted0833,"Q012 failure remains the authoritative Campaign I Defeat trigger.");
            Check(gm.AvailableCampaignQuests0833.Count==0&&!gm.IsDowntimeAvailable0836,"Defeat closes Campaign I Quest selection and Downtime.");
            Check(results!=null&&results.IsOpen&&results.DebugHeading0835.Contains("CAMPAIGN I DEFEAT"),"Q012 failure first presents the Campaign Defeat result.");
            results?.ReturnToCampaign();yield return null;
            Check(end!=null&&end.IsOpen&&end.DebugHeading0844.Contains("DEFEAT"),"Defeat routes to the dedicated final Campaign screen.");
            Check(end!=null&&end.DebugTitle0844.Contains("STONEBROOK FALLS"),"Defeat screen records Stonebrook Falls.");
            Check(end!=null&&end.DebugRewards0844.Contains("No Q012 victory reward")&&!end.DebugRewards0844.Contains("Defender of Stonebrook"),"Defeat grants neither the Q012 victory reward nor Defender of Stonebrook title.");
            Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.CampaignEnd,"Defeated campaign also captures CAMPAIGN_END.");
            bool defeatSaved=AscendanceSaveSystem.SaveCurrent(gm,out _);var defeatLoad=defeatSaved?AscendanceSaveSystem.Load():null;yield return null;
            Check(defeatSaved&&defeatLoad?.CampaignProgress0833?.CampaignDefeated==true,"Final Defeat state persists through the real save system.");
            Check(defeatLoad!=null&&CampaignResumeRuntime0842.BuildMainMenuSummary0842(defeatLoad).Contains("CAMPAIGN I DEFEAT"),"Main Menu Continue summary identifies the saved Defeat ending.");
            if(defeatLoad!=null){defeatLoad.ResumeSurface0842=CampaignResumeRuntime0842.Results;Check(CampaignResumeRuntime0842.NormalizeSurface0842(defeatLoad)==CampaignResumeRuntime0842.CampaignEnd,"Older terminal saves normalize to Campaign End even if they stored the legacy Results surface.");}

            try
            {
                if(hadSave&&priorSave!=null){Directory.CreateDirectory(Path.GetDirectoryName(AscendanceSaveSystem.SavePath));File.WriteAllBytes(AscendanceSaveSystem.SavePath,priorSave);}else if(File.Exists(AscendanceSaveSystem.SavePath))File.Delete(AscendanceSaveSystem.SavePath);
                Check(true,"Validator restores the pre-test campaign save file.");
            }
            catch(System.Exception ex){Check(false,"Validator restores the pre-test campaign save file. "+ex.Message);}

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.44 TEST] PASS — Campaign Completion / Victory & Defeat End-State Flow completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.44 TEST] FAIL — Campaign Completion / Victory & Defeat End-State Flow completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
