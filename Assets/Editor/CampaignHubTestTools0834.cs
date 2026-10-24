#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CampaignHubTestTools0834
    {
        [MenuItem("Ascendance/Validation/Test 0.8.34 - Campaign Hub UI (Play Mode)")]
        public static void TestCampaignHub()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.34 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.34 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.34 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.34 TEST] FAIL — "+label);}}
            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugResetCampaignProgress0833();yield return null;

            var hub=Object.FindAnyObjectByType<CampaignHubUI0834>();
            Check(hub!=null,"Campaign Hub UI is present in Stonebrook without scene wiring.");
            if(hub!=null){hub.Open();yield return null;Check(hub.IsOpen,"Campaign Hub opens as a modal overlay.");hub.Close();Check(!hub.IsOpen,"Campaign Hub closes cleanly.");}

            Check(gm.GetCampaignQuestStatus0834("Q001")==CampaignQuestStatus0834.Active,"Active Q001 is reported as ACTIVE QUEST.");
            Check(gm.GetCampaignQuestStatus0834("Q002")==CampaignQuestStatus0834.Locked&&gm.GetCampaignQuestStatus0834("Q003")==CampaignQuestStatus0834.Locked,"Future WE001 quests remain locked while Q001 is unresolved.");

            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.GetCampaignQuestStatus0834("Q001")==CampaignQuestStatus0834.Completed,"Resolved Q001 is reported Completed.");
            Check(gm.GetCampaignQuestStatus0834("Q002")==CampaignQuestStatus0834.Available&&gm.GetCampaignQuestStatus0834("Q003")==CampaignQuestStatus0834.Available,"Q002 required and Q003 optional are both exposed as Available after Q001.");
            Check(gm.GetCampaignQuestPathLabel0834("Q002")=="REQUIRED II"&&gm.GetCampaignQuestPathLabel0834("Q003")=="OPTIONAL","Campaign Hub route labels distinguish required and optional paths.");

            Check(gm.BeginOptionalCampaignQuest0833(),"Optional Q003 can still be started through the authoritative campaign router.");yield return null;
            Check(gm.GetCampaignQuestStatus0834("Q003")==CampaignQuestStatus0834.Active,"Started Q003 becomes Active in Hub state.");
            gm.DebugResolveCampaignQuest0833(false);yield return null;
            Check(gm.GetCampaignQuestStatus0834("Q003")==CampaignQuestStatus0834.Failed&&gm.CanBeginCampaignQuest0833("Q003"),"Failed optional Quest is shown Failed while remaining retryable.");
            Check(gm.BeginCampaignQuest0833("Q003"),"Campaign Hub retry route can restart a failed available Quest.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.BeginRecommendedCampaignQuest0833(),"Required Q002 starts through the campaign router after optional recovery.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.CanAdvanceWorldEvent0833&&gm.CurrentWorldEventOutcomePreview0834.Contains("PERFECT CLEAR"),"Perfect WE001 completion exposes the real chapter reward before advancement.");

            Check(gm.AdvanceWorldEvent0833(),"Hub-facing state allows WE001 advance only through the runtime gate.");yield return null;
            Check(gm.CurrentWorldEventId0833=="WE002"&&gm.GetCampaignQuestStatus0834("Q004")==CampaignQuestStatus0834.Available&&gm.GetCampaignQuestStatus0834("Q006")==CampaignQuestStatus0834.Available,"WE002 Hub state exposes Q004 required and Q006 optional after transition.");

            gm.DebugResetCampaignProgress0833();gm.DebugResolveCampaignQuest0833(false);gm.DebugAdvanceCampaignDay0833(4);yield return null;
            Check(gm.CanAdvanceWorldEvent0833&&gm.CurrentWorldEventOutcomePreview0834.Contains("DAY LIMIT REACHED"),"Day-limit route visibly warns about the real incomplete-WE001 penalty before advancing.");
            Check(gm.CurrentWorldEventDayPressure0834>=0.99f,"Day-pressure meter reaches full at the authored WE001 threshold.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.34 TEST] PASS — Campaign Hub UI state routing completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.34 TEST] FAIL — Campaign Hub UI state routing completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
