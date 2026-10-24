#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CampaignResumeTestTools0842
    {
        [MenuItem("Ascendance/Validation/Test 0.8.42 - Persistent Campaign Resume & Continue (Play Mode)")]
        public static void TestResume()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.42 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.42 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.42 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.42 TEST] FAIL — "+label);}}
            string path=AscendanceSaveSystem.SavePath;
            bool hadSave=File.Exists(path);byte[] previousSave=hadSave?File.ReadAllBytes(path):null;
            var resume=Object.FindAnyObjectByType<CampaignResumeController0842>();
            try
            {
                Check(resume!=null,"Stonebrook installs the persistent resume controller.");
                Check(Object.FindAnyObjectByType<CampaignAutoSave0842>()!=null,"Stonebrook installs safe quit/mobile-pause autosave support.");

                gm.SetParty(new[]{"Garruk","Aldric","Elara"},"Garruk",true);
                gm.DebugResetCampaignProgress0833();yield return null;
                QuestResultsUI0835.Instance?.Close();DowntimeUI0836.Instance?.Close();CampaignHubUI0834.Instance?.Close();QuestBriefingUI0841.Instance?.CloseForResume0842();gm.CancelQuestBriefing0841();
                Check(!gm.QuestComplete&&!gm.QuestFailed,"Validator begins in a live mid-Quest state.");
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.MidQuest,"Active gameplay captures MID_QUEST as the resume surface.");
                int midRound=gm.Round;string midHero=gm.SelectedHeroId;
                Check(AscendanceSaveSystem.SaveCurrent(gm,out _),"Mid-Quest state saves through the real campaign save system.");
                Check(AscendanceSaveSystem.TryLoad(out var midSave,out var midMsg)&&midSave!=null&&midSave.ResumeSurface0842==CampaignResumeRuntime0842.MidQuest,"Saved JSON persists the MID_QUEST resume surface.");
                Check(AscendanceSaveSystem.HasValidSave&&midMsg.Contains("ready"),"A valid campaign save is recognized as Continue-ready.");
                string midSummary=AscendanceSaveSystem.MainMenuSummary0842;
                Check(midSummary.Contains("MID-QUEST")&&midSummary.Contains("Q001")&&midSummary.Contains("CAMPAIGN DAY"),"Main-menu save summary identifies a mid-Quest campaign and its day.");

                gm.DebugResolveCampaignQuest0833(true);yield return null;
                Check(gm.QuestComplete&&QuestResultsUI0835.Instance!=null&&QuestResultsUI0835.Instance.IsOpen,"Resolved Q001 opens the Quest Results surface.");
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.Results,"Visible Quest Results capture RESULTS as the resume surface.");
                Check(AscendanceSaveSystem.SaveCurrent(gm,out _)&&AscendanceSaveSystem.Load()?.ResumeSurface0842==CampaignResumeRuntime0842.Results,"Results save persists the RESULTS resume surface.");
                resume?.ApplySurface0842(CampaignResumeRuntime0842.Results);yield return null;
                Check(QuestResultsUI0835.Instance.IsOpen&&!DowntimeUI0836.Instance.IsOpen&&!CampaignHubUI0834.Instance.IsOpen,"RESULTS resume restores the Quest Results overlay only.");
                QuestResultsUI0835.Instance.Close();
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.Downtime,"A resolved Quest with Results already dismissed safely defaults new saves to Downtime.");
                QuestResultsUI0835.Instance.Open();

                QuestResultsUI0835.Instance.ReturnToCampaign();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen&&gm.IsDowntimeAvailable0836,"Return to Campaign enters Stonebrook Downtime.");
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.Downtime,"Visible Downtime captures DOWNTIME as the resume surface.");
                int downtimeGold=gm.PartyGold;string downtimeKey=gm.CurrentDowntimePeriodKey0839;
                Check(AscendanceSaveSystem.SaveCurrent(gm,out _),"Downtime state saves through the real campaign save system.");
                var downtimeSave=AscendanceSaveSystem.Load();
                Check(downtimeSave!=null&&downtimeSave.ResumeSurface0842==CampaignResumeRuntime0842.Downtime,"Saved JSON persists the DOWNTIME resume surface.");
                string downtimeSummary=CampaignResumeRuntime0842.BuildMainMenuSummary0842(downtimeSave);
                Check(downtimeSummary.Contains("STONEBROOK DOWNTIME")&&downtimeSummary.Contains("WE001"),"Main-menu summary clearly identifies Stonebrook Downtime and the World Event.");

                // Simulate the legacy results auto-open that can happen during ApplySaveData, then prove explicit resume wins.
                QuestResultsUI0835.Instance.Open();DowntimeUI0836.Instance.Close();
                resume?.ApplySurface0842(CampaignResumeRuntime0842.Downtime);yield return null;
                Check(DowntimeUI0836.Instance.IsOpen&&!QuestResultsUI0835.Instance.IsOpen&&!CampaignHubUI0834.Instance.IsOpen,"DOWNTIME resume overrides legacy Results auto-open and restores Downtime directly.");
                Check(gm.PartyGold==downtimeGold&&gm.CurrentDowntimePeriodKey0839==downtimeKey,"Surface restoration does not mutate Gold or the current Downtime period.");

                DowntimeUI0836.Instance.ChooseNextQuest();yield return null;
                Check(CampaignHubUI0834.Instance!=null&&CampaignHubUI0834.Instance.IsOpen&&!DowntimeUI0836.Instance.IsOpen,"Choose Quest opens the Campaign Hub.");
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.CampaignHub,"Visible quest selection captures CAMPAIGN_HUB as the resume surface.");
                Check(AscendanceSaveSystem.SaveCurrent(gm,out _)&&AscendanceSaveSystem.Load()?.ResumeSurface0842==CampaignResumeRuntime0842.CampaignHub,"Campaign Hub save persists the CAMPAIGN_HUB resume surface.");
                resume?.ApplySurface0842(CampaignResumeRuntime0842.CampaignHub);yield return null;
                Check(CampaignHubUI0834.Instance.IsOpen&&!DowntimeUI0836.Instance.IsOpen&&!QuestResultsUI0835.Instance.IsOpen,"CAMPAIGN_HUB resume restores quest selection without reopening Downtime or Results.");

                Check(QuestBriefingUI0841.Instance!=null&&QuestBriefingUI0841.Instance.Open("Q002"),"Available Q002 can open the pre-Quest briefing before a save.");yield return null;
                gm.SetHeroQuestReady0841("Garruk",true);gm.SetHeroQuestReady0841("Aldric",true);
                Check(gm.ReadyHeroCount0841==2,"Briefing can contain transient ready-up state before saving.");
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.CampaignHub,"Saving from a transient briefing intentionally targets CAMPAIGN_HUB, not the briefing itself.");
                Check(AscendanceSaveSystem.SaveCurrent(gm,out _)&&AscendanceSaveSystem.Load()?.ResumeSurface0842==CampaignResumeRuntime0842.CampaignHub,"Briefing save records the safe Campaign Hub resume destination.");
                resume?.ApplySurface0842(CampaignResumeRuntime0842.CampaignHub);yield return null;
                Check(!gm.IsQuestBriefingOpen0841&&gm.ReadyHeroCount0841==0&&!QuestBriefingUI0841.Instance.IsOpen,"Resume clears transient briefing and READY state.");
                Check(CampaignHubUI0834.Instance.IsOpen,"A briefing save resumes at Campaign Hub so the party can re-brief and ready cleanly.");

                // Real load application: mutate runtime, restore saved hub state, then route it.
                var hubSave=AscendanceSaveSystem.Load();
                int savedGold=hubSave.PartyGold;string savedQuest=hubSave.QuestId;int savedHealth=hubSave.Heroes.First(h=>h.Id=="Garruk").Health;
                gm.Heroes["Garruk"].Health=1;gm.DebugStartQuest0829("Q003");yield return null;
                gm.ApplySaveData(hubSave);yield return null;resume?.ApplySurface0842(CampaignResumeRuntime0842.NormalizeSurface0842(hubSave));yield return null;
                Check(gm.PartyGold==savedGold&&gm.CurrentQuestId0829==savedQuest&&gm.Heroes["Garruk"].Health==savedHealth,"Continue restoration reapplies the authoritative saved campaign data before routing the UI.");
                Check(CampaignHubUI0834.Instance.IsOpen,"Loaded Campaign Hub save returns to the intended quest-selection surface.");

                // Older saves do not contain ResumeSurface0842. Compatibility must be deterministic.
                var legacyResolved=new AscendanceSaveData{QuestId="Q001",QuestComplete=true,Heroes={new HeroSaveData{Id="Garruk"}}};
                var legacyActive=new AscendanceSaveData{QuestId="Q001",QuestComplete=false,QuestFailed=false,Heroes={new HeroSaveData{Id="Garruk"}}};
                Check(CampaignResumeRuntime0842.NormalizeSurface0842(legacyResolved)==CampaignResumeRuntime0842.Results,"Pre-0.8.42 resolved saves safely fall back to Quest Results.");
                Check(CampaignResumeRuntime0842.NormalizeSurface0842(legacyActive)==CampaignResumeRuntime0842.MidQuest,"Pre-0.8.42 active saves safely fall back to mid-Quest gameplay.");

                AscendanceSession.DebugQueueResume0842(CampaignResumeRuntime0842.Downtime);
                Check(AscendanceSession.TryConsumePendingResume0842(out var queued)&&queued==CampaignResumeRuntime0842.Downtime,"Session carries the intended resume surface across the MainMenu → Stonebrook scene load.");
                Check(!AscendanceSession.TryConsumePendingResume0842(out _),"Resume surface is consumed once and cannot be replayed accidentally.");

                // Corrupt-save hardening. Continue must not call a bad file valid.
                File.WriteAllText(path,"{ definitely not valid campaign json");
                Check(AscendanceSaveSystem.HasSave&&!AscendanceSaveSystem.HasValidSave,"A corrupt file may exist but is not considered a valid Continue save.");
                Check(!AscendanceSaveSystem.TryLoad(out _,out var badMessage)&&!string.IsNullOrWhiteSpace(badMessage),"Corrupt save parsing fails cleanly with a player-facing status message.");
                Check(!AscendanceSession.ContinueGame()&&!string.IsNullOrWhiteSpace(AscendanceSession.LastContinueError0842),"Continue refuses an unreadable save without loading Stonebrook as a fake new session.");
                Check(AscendanceSaveSystem.MainMenuSummary0842.Contains("UNREADABLE"),"Main menu explicitly reports an unreadable save instead of pretending Continue is available.");

                // Restore a known valid save and prove summary remains usable.
                File.WriteAllText(path,JsonUtility.ToJson(hubSave,true));
                Check(AscendanceSaveSystem.HasValidSave,"Restoring valid JSON re-enables Continue eligibility.");
                Check(AscendanceSaveSystem.MainMenuSummary0842.Contains("CAMPAIGN HUB")&&AscendanceSaveSystem.MainMenuSummary0842.Contains("SAVED"),"Continue summary reports resume surface and saved timestamp.");

                // Mid-Quest UI routing closes every between-Quest overlay without altering campaign state.
                gm.ApplySaveData(midSave);yield return null;
                int beforeMidGold=gm.PartyGold;int beforeMidRound=gm.Round;
                QuestResultsUI0835.Instance?.Close();DowntimeUI0836.Instance?.Open();CampaignHubUI0834.Instance?.Open();
                resume?.ApplySurface0842(CampaignResumeRuntime0842.MidQuest);yield return null;
                Check(!QuestResultsUI0835.Instance.IsOpen&&!DowntimeUI0836.Instance.IsOpen&&!CampaignHubUI0834.Instance.IsOpen&&!QuestBriefingUI0841.Instance.IsOpen,"MID_QUEST resume closes campaign overlays and returns control to gameplay.");
                Check(gm.Round==beforeMidRound&&gm.PartyGold==beforeMidGold&&gm.SelectedHeroId==midHero,"MID_QUEST surface routing leaves restored gameplay state untouched.");
            }
            finally
            {
                try{if(hadSave){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,previousSave);}else if(File.Exists(path))File.Delete(path);}catch(System.Exception ex){Debug.LogException(ex);}
            }
            Check(File.Exists(path)==hadSave,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.42 TEST] PASS — Persistent Campaign Resume & Continue Flow completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.42 TEST] FAIL — Persistent Campaign Resume & Continue Flow completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
