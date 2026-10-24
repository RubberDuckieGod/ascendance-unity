#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class QuestResultsTestTools0835
    {
        [MenuItem("Ascendance/Validation/Test 0.8.35 - Quest Results Flow (Play Mode)")]
        public static void TestQuestResultsFlow()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.35 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.35 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.35 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.35 TEST] FAIL — "+label);}}

            byte[] priorSave=null;bool hadSave=false;
            try
            {
                hadSave=File.Exists(AscendanceSaveSystem.SavePath);
                if(hadSave)priorSave=File.ReadAllBytes(AscendanceSaveSystem.SavePath);
            }
            catch(System.Exception ex){Debug.LogWarning("[ASCENDANCE][0.8.35 TEST] Save backup skipped: "+ex.Message);}

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugResetCampaignProgress0833();yield return null;
            var results=Object.FindAnyObjectByType<QuestResultsUI0835>();
            var hub=Object.FindAnyObjectByType<CampaignHubUI0834>();
            Check(results!=null,"Quest Results UI is present in Stonebrook without scene wiring.");
            Check(results!=null&&!results.IsOpen,"Quest Results stays hidden while Q001 is active.");

            gm.DebugResolveCampaignQuest0833(true);yield return null;
            var q1=gm.LastQuestResult0835;
            Check(q1!=null&&q1.QuestId=="Q001"&&q1.Success,"Q001 success creates a result snapshot.");
            Check(q1!=null&&q1.PartyGoldDelta==75&&q1.GeneralReputationDelta==1,"Q001 result records actual +75 Gold and +1 Reputation deltas.");
            Check(q1!=null&&q1.HeroXpDeltas.Count==2&&q1.HeroXpDeltas.All(x=>x.Delta==100),"Q001 result records +100 XP for each participating Hero.");
            Check(q1!=null&&q1.GrantedEquipment.Count==1,"Q001 result records the actual Equipment granted by the live deck.");
            Check(results!=null&&results.IsOpen&&results.DebugHeading0835.Contains("QUEST COMPLETE"),"Successful Quest auto-opens the Quest Complete screen.");
            Check(results!=null&&results.DebugRewards0835.Contains("Party Gold +75"),"Results screen renders actual reward deltas rather than only authored copy.");
            Check(results!=null&&results.DebugCampaignImpact0835.Contains("Q002")&&results.DebugCampaignImpact0835.Contains("Q003"),"Results screen shows newly unlocked required and optional Quest paths.");

            if(results!=null)results.ReturnToCampaign();yield return null;
            Check(results!=null&&!results.IsOpen&&hub!=null&&hub.IsOpen,"Return to Campaign closes Results and opens the verified Campaign Hub.");
            hub?.Close();

            Check(gm.BeginOptionalCampaignQuest0833(),"Optional Q003 starts through the authoritative campaign router after Results.");yield return null;
            Check(gm.LastQuestResult0835==null&&results!=null&&!results.IsOpen,"Starting a new Quest clears the prior result and closes the Results overlay.");
            gm.DebugResolveCampaignQuest0833(false);yield return null;
            var failed=gm.LastQuestResult0835;
            Check(failed!=null&&!failed.Success&&failed.QuestId=="Q003","Failed Q003 creates a failure result snapshot.");
            Check(results!=null&&results.IsOpen&&results.DebugHeading0835.Contains("QUEST FAILED"),"Failed Quest auto-opens the Quest Failed screen.");
            Check(results!=null&&results.DebugCampaignImpact0835.Contains("Retry available"),"Failure result exposes retry eligibility from the campaign runtime.");

            Check(gm.BeginCampaignQuest0833("Q003"),"Failed Q003 can be retried through the authoritative campaign router.");yield return null;
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            var recovered=gm.LastQuestResult0835;
            Check(recovered!=null&&recovered.Success&&recovered.ActualRewardSummary.Contains("WE002"),"Recovered Q003 result preserves its banked WE002 Scout-reduction reward text.");

            bool saved=AscendanceSaveSystem.SaveCurrent(gm,out var saveMessage);yield return null;
            var loaded=saved?AscendanceSaveSystem.Load():null;
            Check(saved&&loaded?.QuestResult0835!=null&&loaded.QuestResult0835.QuestId=="Q003"&&loaded.QuestResult0835.Success,"Real save file persists the resolved Quest result snapshot.");
            if(loaded!=null){gm.ApplySaveData(loaded);yield return null;}
            Check(gm.LastQuestResult0835!=null&&gm.LastQuestResult0835.QuestId=="Q003"&&gm.LastQuestResult0835.Success,"Applying the real save restores the Quest result snapshot.");

            gm.DebugStartQuest0829("Q012");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.CampaignCompleted0833&&gm.LastQuestResult0835?.CampaignVictory==true,"Q012 success marks the result as Campaign I Victory.");
            Check(results!=null&&results.IsOpen&&results.DebugHeading0835.Contains("CAMPAIGN I VICTORY"),"Q012 success renders the dedicated Campaign Victory result screen.");

            gm.DebugResetCampaignProgress0833();gm.DebugStartQuest0829("Q012");gm.DebugResolveCampaignQuest0833(false);yield return null;
            Check(gm.CampaignDefeated0833&&gm.LastQuestResult0835?.CampaignDefeat==true,"Q012 failure marks the result as Campaign I Defeat.");
            Check(results!=null&&results.IsOpen&&results.DebugHeading0835.Contains("CAMPAIGN I DEFEAT"),"Q012 failure renders the dedicated Campaign Defeat result screen.");

            try
            {
                if(hadSave&&priorSave!=null){Directory.CreateDirectory(Path.GetDirectoryName(AscendanceSaveSystem.SavePath));File.WriteAllBytes(AscendanceSaveSystem.SavePath,priorSave);}
                else if(File.Exists(AscendanceSaveSystem.SavePath))File.Delete(AscendanceSaveSystem.SavePath);
                Check(true,"Validator restores the pre-test save file after the persistence check.");
            }
            catch(System.Exception ex){Check(false,"Validator restores the pre-test save file after the persistence check. "+ex.Message);}

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.35 TEST] PASS — Quest Results & Return-to-Campaign flow completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.35 TEST] FAIL — Quest Results & Return-to-Campaign flow completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
