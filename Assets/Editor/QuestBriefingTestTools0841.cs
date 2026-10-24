#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class QuestBriefingTestTools0841
    {
        [MenuItem("Ascendance/Validation/Test 0.8.41 - Quest Briefing & Ready-Up (Play Mode)")]
        public static void TestBriefing()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.41 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.41 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.41 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.41 TEST] FAIL — "+label);}}

            string path=AscendanceSaveSystem.SavePath;
            bool hadSave=File.Exists(path);byte[] previousSave=hadSave?File.ReadAllBytes(path):null;
            try
            {
                gm.SetParty(new[]{"Garruk","Aldric","Elara"},"Garruk",true);
                gm.DebugResetCampaignProgress0833();
                Check(!gm.OpenQuestBriefing0841("Q002"),"A locked/unavailable Quest cannot open a briefing while Q001 is active.");
                gm.DebugResolveCampaignQuest0833(true);yield return null;
                Check(gm.IsDowntimeAvailable0836&&gm.CanBeginCampaignQuest0833("Q002"),"Resolved Q001 makes Q002 available during Stonebrook Downtime.");

                var g=gm.Heroes["Garruk"];
                g.LastDowntimeActivityKey0839=null;g.LastDowntimeActivityLabel0839=null;
                int gold0=gm.PartyGold;
                Check(gm.WorkDowntime0839("Garruk")&&gm.PartyGold==gold0+25,"A Hero may spend their Downtime Activity before opening the Quest briefing.");
                string activityKey=gm.CurrentDowntimePeriodKey0839;int goldBeforeBrief=gm.PartyGold;
                int dayBefore=gm.WorldEventDay0833,totalBefore=gm.TotalCampaignDay0833;
                string currentQuestBefore=gm.CurrentQuestId0829;

                Check(gm.OpenQuestBriefing0841("Q002"),"Available Q002 opens the transient Quest briefing.");
                Check(gm.IsQuestBriefingOpen0841&&gm.PendingQuestBriefingId0841=="Q002","Briefing records the selected Quest without launching it.");
                Check(gm.CurrentQuestId0829==currentQuestBefore&&gm.QuestComplete,"Opening the briefing leaves the resolved current Quest unchanged.");
                Check(gm.WorldEventDay0833==dayBefore&&gm.TotalCampaignDay0833==totalBefore,"Opening the briefing does not advance Campaign Day.");
                Check(gm.PartyGold==goldBeforeBrief&&gm.HasUsedDowntimeActivity0839("Garruk")&&gm.CurrentDowntimePeriodKey0839==activityKey,"Opening the briefing does not alter Gold or Downtime Activity state.");

                var def=gm.PendingQuestDefinition0841;
                Check(def!=null&&def.Id=="Q002"&&def.Name=="Rescue the Merchant Caravan","Briefing resolves the authored Q002 identity.");
                Check(def!=null&&def.Victory.Contains("Rescue both Wagons")&&def.Failure.Contains("Wagons destroyed")&&def.Reward.Contains("125 XP"),"Briefing exposes authored objective, failure, and reward text.");
                string stakes=gm.QuestBriefingWorldEventText0841;
                Check(stakes.Contains("WE001")&&stakes.Contains("Goblin Raids")&&stakes.Contains("WORLD EVENT DAY"),"Briefing exposes current World Event and Campaign Day stakes.");
                Check(stakes.Contains("PERFECT CLEAR")||stakes.Contains("DAY-LIMIT")||stakes.Contains("CHAPTER SECURED"),"Briefing carries the authoritative World Event outcome preview.");

                Check(gm.RequiredReadyHeroCount0841==3&&gm.ReadyHeroCount0841==0&&!gm.CanConfirmBriefedQuest0841,"Every participating Hero starts NOT READY and BEGIN QUEST is locked.");
                Check(gm.SetHeroQuestReady0841("Garruk",true)&&gm.ReadyHeroCount0841==1,"Garruk can mark READY.");
                Check(!gm.CanConfirmBriefedQuest0841,"One ready Hero is insufficient for a three-Hero party.");
                Check(gm.SetHeroQuestReady0841("Aldric",true)&&gm.ReadyHeroCount0841==2,"Aldric can independently mark READY.");
                Check(gm.SetHeroQuestReady0841("Garruk",false)&&gm.ReadyHeroCount0841==1,"A Hero can return to NOT READY before launch.");
                Check(!gm.SetHeroQuestReady0841("Vaelis",true),"A non-participating Hero cannot satisfy ready-up.");
                Check(!gm.ConfirmBriefedQuest0841(),"BEGIN QUEST is rejected until the entire participating party is ready.");
                Check(gm.WorldEventDay0833==dayBefore&&gm.CurrentQuestId0829==currentQuestBefore,"A rejected BEGIN QUEST attempt does not advance or initialize the Quest.");

                gm.CancelQuestBriefing0841();yield return null;
                Check(!gm.IsQuestBriefingOpen0841&&gm.ReadyHeroCount0841==0,"Canceling the briefing clears all transient readiness.");
                Check(gm.WorldEventDay0833==dayBefore&&gm.TotalCampaignDay0833==totalBefore,"Canceling the briefing leaves campaign timing untouched.");

                Check(QuestBriefingUI0841.Instance!=null&&QuestBriefingUI0841.Instance.Open("Q002"),"The dedicated Quest Briefing UI opens for Q002.");yield return null;
                Check(QuestBriefingUI0841.Instance.IsOpen&&QuestBriefingUI0841.Instance.DebugQuestId0841=="Q002","Briefing UI is bound to the selected Quest.");
                Check(QuestBriefingUI0841.Instance.DebugObjectiveText0841.Contains("OBJECTIVE")&&QuestBriefingUI0841.Instance.DebugObjectiveText0841.Contains("FAILURE")&&QuestBriefingUI0841.Instance.DebugObjectiveText0841.Contains("REWARD"),"Briefing UI shows objective, failure, and reward sections.");
                Check(QuestBriefingUI0841.Instance.DebugWorldEventText0841.Contains("Goblin Raids"),"Briefing UI shows World Event stakes.");
                Check(QuestBriefingUI0841.Instance.DebugPartyText0841.Contains("Garruk Stoneblood")&&QuestBriefingUI0841.Instance.DebugPartyText0841.Contains("Sir Aldric Ironwall")&&QuestBriefingUI0841.Instance.DebugPartyText0841.Contains("Sister Elara"),"Briefing UI shows the full participating party.");
                Check(QuestBriefingUI0841.Instance.DebugPartyText0841.Contains("ATTACK")&&QuestBriefingUI0841.Instance.DebugPartyText0841.Contains("ARMOR"),"Party briefing includes each Hero's live loadout combat summary.");
                Check(!QuestBriefingUI0841.Instance.DebugBeginInteractable0841,"BEGIN QUEST button is disabled before ready-up completes.");

                Check(gm.SetHeroQuestReady0841("Garruk",true)&&gm.SetHeroQuestReady0841("Aldric",true)&&gm.SetHeroQuestReady0841("Elara",true),"All three participating Heroes can mark READY.");yield return null;
                Check(gm.AllParticipatingHeroesReady0841&&gm.CanConfirmBriefedQuest0841&&QuestBriefingUI0841.Instance.DebugBeginInteractable0841,"Full-party ready-up enables BEGIN QUEST.");
                Check(gm.PartyGold==goldBeforeBrief&&gm.HasUsedDowntimeActivity0839("Garruk"),"Ready-up itself does not alter Gold or the Hero's used Activity.");

                int beginDay=gm.WorldEventDay0833,beginTotal=gm.TotalCampaignDay0833;
                bool began=gm.ConfirmBriefedQuest0841();yield return null;
                Check(began&&gm.CurrentQuestId0829=="Q002"&&!gm.QuestComplete&&!gm.QuestFailed,"Final confirmation launches Q002 through the authoritative campaign runtime.");
                Check(gm.WorldEventDay0833==beginDay+1&&gm.TotalCampaignDay0833==beginTotal+1,"Campaign Day advances exactly once when BEGIN QUEST succeeds.");
                Check(!gm.IsQuestBriefingOpen0841&&gm.ReadyHeroCount0841==0,"Successful Quest launch clears briefing and ready-up state.");
                Check(QuestBriefingUI0841.Instance!=null&&!QuestBriefingUI0841.Instance.IsOpen,"Briefing UI closes once the Quest becomes active.");
                Check(!gm.IsDowntimeAvailable0836,"Stonebrook Downtime closes after the confirmed Quest launch.");

                gm.DebugResolveCampaignQuest0833(false);yield return null;
                Check(gm.IsDowntimeAvailable0836&&gm.GetCampaignQuestStatus0834("Q002")==CampaignQuestStatus0834.Failed&&gm.CanBeginCampaignQuest0833("Q002"),"Failed Q002 returns to Downtime with a retry available.");
                int retryDay=gm.WorldEventDay0833;
                Check(gm.OpenQuestBriefing0841("Q002")&&gm.PendingQuestBriefingId0841=="Q002","A failed Quest can open the same briefing for a retry.");
                foreach(var h in gm.ParticipatingHeroes.ToList())gm.SetHeroQuestReady0841(h.Id,true);
                Check(gm.CanConfirmBriefedQuest0841,"Retry briefing uses the same full-party ready gate.");
                Check(gm.ConfirmBriefedQuest0841()&&gm.CurrentQuestId0829=="Q002"&&gm.WorldEventDay0833==retryDay+1,"Confirmed retry launches normally and advances the next Campaign Day once.");yield return null;
            }
            finally
            {
                try{if(hadSave){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,previousSave);}else if(File.Exists(path))File.Delete(path);}catch(System.Exception ex){Debug.LogException(ex);}
            }
            Check(File.Exists(path)==hadSave,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.41 TEST] PASS — Quest Briefing & Ready-Up Flow completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.41 TEST] FAIL — Quest Briefing & Ready-Up Flow completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
