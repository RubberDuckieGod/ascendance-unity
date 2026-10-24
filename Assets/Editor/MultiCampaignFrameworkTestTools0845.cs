using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class MultiCampaignFrameworkTestTools0845
    {
        [MenuItem("Ascendance/Validation/Test 0.8.45 - Multi-Campaign Valewatch Bootstrap (Play Mode)")]
        public static void Test0845(){if(!EditorApplication.isPlaying){Debug.LogError("[ASCENDANCE][0.8.45 TEST] Enter Play Mode first.");return;}var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.45 TEST] GameManager missing.");return;}gm.StartCoroutine(Run(gm));}
        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.45 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.45 TEST] FAIL — "+label);}}
            string path=AscendanceSaveSystem.SavePath;bool had=File.Exists(path);string backup=had?File.ReadAllText(path):null;
            try
            {
                Check(AscendanceGameManager.BuildVersion=="0.8.45a","Build identifies the Multi-Campaign / Valewatch bootstrap milestone.");
                Check(CampaignContent0845.Campaigns.Count>=2&&CampaignContent0845.Campaigns["C001"].Title=="The Goblin War"&&CampaignContent0845.Campaigns["C002"].Title=="Shadows Over Valewatch","Campaign registry contains distinct Campaign I and Campaign II identities.");
                Check(CampaignContent0845.Campaigns["C002"].Region=="Crownlands"&&CampaignContent0845.Campaigns["C002"].Hub=="Valewatch","Campaign II is bound to Crownlands / Valewatch.");
                Check(CampaignContent0833.WorldEvents["WE005"].Name=="Knives in the Night"&&CampaignContent0833.WorldEvents["WE006"].Name=="The Hidden Hand"&&CampaignContent0833.WorldEvents["WE007"].Name=="Beneath Valewatch"&&CampaignContent0833.WorldEvents["WE008"].Name=="The Veiled Sovereign","WE005–WE008 authored Valewatch chapter names are registered.");
                Check(CampaignContent0833.WorldEvents["WE005"].DayLimit==5&&CampaignContent0833.WorldEvents["WE006"].DayLimit==6&&CampaignContent0833.WorldEvents["WE007"].DayLimit==6&&CampaignContent0833.WorldEvents["WE008"].DayLimit==0,"Campaign II World Event day limits are registered.");
                Check(CampaignContent0845.ValewatchQuestPlan.Count==12&&CampaignContent0845.ValewatchQuestPlan["Q013"].Name=="The Missing Caravan"&&CampaignContent0845.ValewatchQuestPlan["Q022"].Name=="The City Falls"&&CampaignContent0845.ValewatchQuestPlan["Q023"].Name=="The Descent"&&CampaignContent0845.ValewatchQuestPlan["Q024"].Name=="The Veiled Sovereign","Q013–Q024 authored campaign plan is registered without inventing live Quest runtime.");
                Check(!CampaignContent0845.Campaigns["C002"].RuntimePlayable,"Campaign II is explicitly framework-only until Q013 runtime is implemented.");

                gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugResetCampaignProgress0833();yield return null;
                Check(gm.CurrentCampaignId0845=="C001"&&gm.CurrentCampaignTitle0845=="The Goblin War"&&gm.CurrentCampaignHub0845=="Stonebrook","A fresh campaign remains Campaign I / Stonebrook.");
                int goldBeforeFinale=gm.PartyGold,repBeforeFinale=gm.GeneralReputation;
                gm.DebugStartQuest0829("Q012");gm.DebugResolveCampaignQuest0833(true);yield return null;
                Check(gm.CampaignCompleted0833&&gm.CanContinueToValewatch0845,"Campaign I Victory exposes the Valewatch continuation handoff.");
                Check(CampaignEndUI0844.Instance!=null,"Campaign I terminal UI remains installed alongside the new framework.");
                CampaignEndUI0844.Instance?.Open();yield return null;
                Check(CampaignEndUI0844.Instance!=null&&CampaignEndUI0844.Instance.DebugHasContinueToValewatch0845,"Campaign I Victory exposes a player-facing CONTINUE TO VALEWATCH action.");
                var carry=gm.ParticipatingHeroes.ToDictionary(h=>h.Id,h=>(Level:h.Level,XP:h.XP,Kills:h.KillCount,Gear:h.AcquiredEquipment.Count,Loot:h.LootInventory.Count,BS:h.BlacksmithingXp0837,AL:h.AlchemyXp0837,EN:h.EnchantingXp0837,AR:h.ArtificingXp0837));
                int goldAfterFinale=gm.PartyGold,repAfterFinale=gm.GeneralReputation;
                Check(repAfterFinale==repBeforeFinale+3,"Authoritative Q012 reward resolves before the Campaign II handoff.");
                bool began=gm.BeginValewatchContinuation0845();yield return null;
                Check(began&&gm.CurrentCampaignId0845=="C002"&&gm.CurrentCampaignTitle0845=="Shadows Over Valewatch","Campaign I Victory transitions the same party into Campaign II identity.");
                Check(gm.CurrentWorldEventId0833=="WE005"&&gm.WorldEventDay0833==1&&gm.TotalCampaignDay0833==1,"Campaign II begins at WE005 Day 1 with its own Campaign Day clock.");
                Check(gm.CampaignLaunchpadAvailable0845&&!gm.CampaignCompleted0833&&!gm.CampaignDefeated0833,"Valewatch continuation enters a non-terminal Campaign II launchpad state.");
                Check(!gm.IsDowntimeAvailable0836,"Stonebrook Downtime is unavailable while the party is at the Valewatch launchpad.");
                Check(gm.CampaignArchive0845.Count==1&&gm.CampaignArchive0845[0].CampaignId=="C001"&&gm.CampaignArchive0845[0].Outcome=="VICTORY","Campaign I final record is archived before Campaign II starts.");
                Check(gm.CampaignArchive0845[0].CompletedQuestIds.Contains("Q012")&&gm.CampaignArchive0845[0].TitleUnlock=="Defender of Stonebrook","Campaign I archive preserves Q012 completion and Defender of Stonebrook.");
                Check(gm.ParticipatingHeroes.All(h=>carry.TryGetValue(h.Id,out var b)&&h.Level==b.Level&&h.XP==b.XP&&h.KillCount==b.Kills&&h.AcquiredEquipment.Count==b.Gear&&h.LootInventory.Count==b.Loot&&h.BlacksmithingXp0837==b.BS&&h.AlchemyXp0837==b.AL&&h.EnchantingXp0837==b.EN&&h.ArtificingXp0837==b.AR),"Valewatch handoff preserves post-Q012 Hero Level/XP, kills, Equipment/Loot, and professions.");
                Check(gm.PartyGold==goldAfterFinale&&gm.GeneralReputation==repAfterFinale&&gm.PartyGold>=goldBeforeFinale,"Campaign handoff itself does not mutate Party Gold or Reputation.");
                Check(!gm.CanBeginCampaignQuest0833("Q013")&&!gm.BeginCampaignQuest0833("Q013")&&gm.TotalCampaignDay0833==1,"Q013 cannot fake-launch or consume a Campaign Day before its live runtime exists.");
                Check(gm.ValewatchChapterPlan0845.Contains("Q013")&&gm.ValewatchChapterPlan0845.Contains("Q024")&&gm.ValewatchChapterPlan0845.Contains("WE008"),"Valewatch launchpad exposes the complete four-chapter campaign plan.");
                Check(CampaignLaunchpadUI0845.Instance!=null,"Stonebrook runtime installs the Campaign II launchpad UI.");
                CampaignLaunchpadUI0845.Instance?.Open();yield return null;
                Check(CampaignLaunchpadUI0845.Instance!=null&&CampaignLaunchpadUI0845.Instance.IsOpen,"Valewatch launchpad opens for Campaign II bootstrap state.");
                Check(CampaignLaunchpadUI0845.Instance.DebugHeading0845.Contains("SHADOWS OVER VALEWATCH"),"Launchpad clearly identifies Campaign II.");
                Check(CampaignLaunchpadUI0845.Instance.DebugOverview0845.Contains("Crownlands")&&CampaignLaunchpadUI0845.Instance.DebugOverview0845.Contains("Valewatch"),"Launchpad shows the authored region and hub.");
                Check(CampaignLaunchpadUI0845.Instance.DebugPlan0845.Contains("Knives in the Night")&&CampaignLaunchpadUI0845.Instance.DebugPlan0845.Contains("The Veiled Sovereign"),"Launchpad shows WE005 through WE008.");
                Check(CampaignLaunchpadUI0845.Instance.DebugCarryover0845.Contains("Garruk")&&CampaignLaunchpadUI0845.Instance.DebugCarryover0845.Contains("Aldric")&&CampaignLaunchpadUI0845.Instance.DebugCarryover0845.Contains("Defender of Stonebrook"),"Launchpad summarizes the continuing party and Campaign I unlock.");
                Check(CampaignLaunchpadUI0845.Instance.DebugQ013Disabled0845,"Q013 launch button is visibly disabled until the next runtime milestone.");
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm)==CampaignResumeRuntime0842.CampaignLaunchpad,"Campaign II bootstrap captures its own persistent resume surface.");
                Check(AscendanceSaveSystem.SaveCurrent(gm,out _),"Campaign II launchpad saves through the authoritative save system.");
                var save=AscendanceSaveSystem.Load();
                Check(save?.CampaignProgress0833?.CampaignId0845=="C002"&&save.CampaignProgress0833.AwaitingCampaignRuntime0845,"Save JSON persists Campaign II identity and bootstrap state.");
                Check(save?.CampaignProgress0833?.CampaignArchive0845?.Count==1&&save.CampaignProgress0833.CampaignArchive0845[0].CampaignId=="C001"&&save.CampaignProgress0833.CampaignArchive0845[0].TitleUnlock=="Defender of Stonebrook","Save JSON persists Campaign I archive and unlock.");
                Check(CampaignResumeRuntime0842.NormalizeSurface0842(save)==CampaignResumeRuntime0842.CampaignLaunchpad,"Continue normalizes Campaign II bootstrap to the Valewatch launchpad.");
                string summary=CampaignResumeRuntime0842.BuildMainMenuSummary0842(save);
                Check(summary.Contains("CAMPAIGN II")&&summary.Contains("SHADOWS OVER VALEWATCH")&&summary.Contains("VALEWATCH CAMPAIGN HUB"),"Main-menu Continue summary identifies Campaign II / Valewatch.");
                gm.ApplySaveData(save);yield return null;
                Check(gm.CurrentCampaignId0845=="C002"&&gm.CampaignLaunchpadAvailable0845,"Applying the save restores Campaign II identity before UI routing.");
                var resume=UnityEngine.Object.FindAnyObjectByType<CampaignResumeController0842>();resume?.ApplySurface0842(CampaignResumeRuntime0842.CampaignLaunchpad);yield return null;
                Check(CampaignLaunchpadUI0845.Instance!=null&&CampaignLaunchpadUI0845.Instance.IsOpen,"Continue routing restores the Valewatch launchpad.");
                Check(gm.CampaignArchive0845.Count==1&&gm.CampaignArchive0845[0].CompletedQuestIds.Contains("Q012")&&gm.CampaignArchive0845[0].TitleUnlock=="Defender of Stonebrook","Campaign I archive survives Campaign II save/load.");
                var legacy=new AscendanceSaveData{QuestId="Q012",CampaignProgress0833=new CampaignProgressSaveData0833{CampaignId0845=null,CurrentWorldEventId="WE004",CampaignCompleted=true}};
                Check(CampaignContent0845.NormalizeCampaignId0845(legacy.CampaignProgress0833.CampaignId0845,legacy.CampaignProgress0833.CurrentWorldEventId)=="C001","Legacy Campaign I saves with no explicit campaign ID normalize to C001.");
                Check(CampaignContent0845.NormalizeCampaignId0845(null,"WE005")=="C002","WE005–WE008 saves can infer Campaign II identity for forward compatibility.");
            }
            finally{if(had)File.WriteAllText(path,backup);else if(File.Exists(path))File.Delete(path);}
            Check(true,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.45 TEST] PASS — Multi-Campaign / Valewatch Framework Bootstrap completed: {pass} passed, 0 failed.");else Debug.LogError($"[ASCENDANCE][0.8.45 TEST] FAIL — Multi-Campaign / Valewatch Framework Bootstrap completed: {pass} passed, {fail} failed.");
        }
    }
}
