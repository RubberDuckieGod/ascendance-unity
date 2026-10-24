#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CampaignFinalStateLoggingTestTools0835a
    {
        private const string VictoryMessage="CAMPAIGN I — Victory recorded. WE004 complete; no duplicate tangible reward is awarded.";
        private const string DefeatMessage="CAMPAIGN I DEFEAT — Q012 failed. Stonebrook Falls is recorded; the campaign is over.";

        [MenuItem("Ascendance/Validation/Test 0.8.35a - Final-State Logging (Play Mode)")]
        public static void TestFinalStateLogging()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.35a TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.35a TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.35a TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.35a TEST] FAIL — "+label);}}
            int Count(string message)=>gm.Log.Count(x=>string.Equals(x,message,System.StringComparison.Ordinal));
            void StressTerminalRefresh()
            {
                for(int i=0;i<12;i++)
                {
                    gm.SyncCampaignProgress0833();
                    _=gm.CampaignSummary0833;
                    _=gm.AvailableCampaignQuests0833.Count;
                    _=gm.CanAdvanceWorldEvent0833;
                    _=gm.CurrentWorldEventProgressLabel0834;
                    _=gm.CurrentWorldEventDayPressure0834;
                }
                var hub=Object.FindAnyObjectByType<CampaignHubUI0834>();
                if(hub!=null){hub.Open();hub.Close();hub.Open();hub.Close();}
            }

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);

            gm.DebugResetCampaignProgress0833();
            gm.DebugStartQuest0829("Q012");
            int victoryBefore=Count(VictoryMessage);
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            int victoryAfterResolve=Count(VictoryMessage);
            Check(gm.CampaignCompleted0833&&!gm.CampaignDefeated0833,"Q012 success still records Campaign I Victory state.");
            Check(victoryAfterResolve==victoryBefore+1,"Q012 success writes the Campaign Victory terminal log exactly once when Victory is entered.");
            StressTerminalRefresh();yield return null;
            Check(Count(VictoryMessage)==victoryAfterResolve,"Repeated campaign sync/getter/UI refreshes do not duplicate the Campaign Victory log.");

            gm.DebugResetCampaignProgress0833();
            gm.DebugStartQuest0829("Q012");
            int defeatBefore=Count(DefeatMessage);
            gm.DebugResolveCampaignQuest0833(false);yield return null;
            int defeatAfterResolve=Count(DefeatMessage);
            Check(gm.CampaignDefeated0833&&!gm.CampaignCompleted0833,"Q012 failure still records Campaign I Defeat state.");
            Check(defeatAfterResolve==defeatBefore+1,"Q012 failure writes the Campaign Defeat terminal log exactly once when Defeat is entered.");
            StressTerminalRefresh();yield return null;
            Check(Count(DefeatMessage)==defeatAfterResolve,"Repeated campaign sync/getter/UI refreshes do not duplicate the Campaign Defeat log.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.35a TEST] PASS — Idempotent Campaign final-state logging completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.35a TEST] FAIL — Idempotent Campaign final-state logging completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
