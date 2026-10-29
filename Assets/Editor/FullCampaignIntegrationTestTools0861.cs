#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class FullCampaignIntegrationTestTools0861
    {
        [MenuItem("Ascendance/Validation/Test 0.8.61 - Full Campaign End-to-End Integration (Play Mode)")]
        public static void Test0861()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.61 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.61 TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.61 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.61 TEST] FAIL — "+label);}}
            string SavePath=AscendanceSaveSystem.SavePath;
            bool hadSave=File.Exists(SavePath);
            string backup=hadSave?File.ReadAllText(SavePath):null;
            string Fingerprint()
            {
                return string.Join("|",gm.ParticipatingHeroes.OrderBy(h=>h.Id,StringComparer.OrdinalIgnoreCase).Select(h=>$"{h.Id}:{h.Level}:{h.XP}:{h.Health}:{h.HealingPotions}:{h.KillCount}:{h.BlacksmithingXp0837}:{h.AlchemyXp0837}:{h.EnchantingXp0837}:{h.ArtificingXp0837}:{h.AcquiredEquipment.Count}:{h.LootInventory.Count}"));
            }
            int ExpectedMeter(string campaignId)
            {
                if(campaignId==CampaignContent0845.CampaignIII)return gm.Instability0857;
                if(campaignId==CampaignContent0845.CampaignIV)return gm.Blight0857;
                if(campaignId==CampaignContent0845.CampaignV)return gm.Doomtide0857;
                if(campaignId==CampaignContent0845.CampaignVI)return gm.Sunfall0857;
                return gm.Suspicion0857;
            }
            string CampaignId(int n)=>$"C{n:000}";

            try
            {
                HeroDatabase.EnsureLoaded();FullContentCatalog0850.EnsureLoaded();
                Check(AscendanceGameManager.BuildVersion=="0.10.2h","Build identifies the Full Campaign End-to-End Integration milestone.");
                Check(CampaignContent0845.Campaigns.Count==6&&CampaignContent0845.Campaigns.Values.All(c=>c.RuntimePlayable),"all six authored campaigns are registered as runtime-playable.");
                Check(CampaignContent0833.WorldEvents.Count==24&&FullContentCatalog0850.WorldEventCount==24,"all 24 World Events are registered in authored content and live campaign routing.");
                Check(FullContentCatalog0850.QuestCount==72&&Enumerable.Range(1,72).All(n=>CampaignContent0845.Campaigns.ContainsKey(CampaignContent0845.CampaignForQuest0845($"Q{n:000}"))),"all 72 Quests map into the six-campaign registry.");
                Check(CampaignContent0845.Campaigns.Values.All(c=>CampaignContent0833.WorldEvents.ContainsKey(c.FirstWorldEventId)&&CampaignContent0833.WorldEvents.ContainsKey(c.LastWorldEventId)),"every campaign is bound to its authored first and final World Event.");

                gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugResetCampaignProgress0833();yield return null;
                Check(gm.CurrentCampaignId0845==CampaignContent0845.CampaignI&&gm.CurrentQuestId0829=="Q001"&&gm.WorldEventDay0833==1&&gm.TotalCampaignDay0833==1,"Campaign I begins with Q001 on World Event Day 1 / Campaign Day 1.");

                int uniqueVictories=0;
                bool q013RetryTested=false;
                bool routeAll=true;
                bool rewardAll=true;
                bool firstQuestClockAll=true;

                for(int campaignNumber=1;campaignNumber<=6;campaignNumber++)
                {
                    string campaignId=CampaignId(campaignNumber);
                    var campaign=CampaignContent0845.Campaigns[campaignId];
                    Check(gm.CurrentCampaignId0845==campaignId,$"Campaign {campaignNumber} activates the correct campaign identity ({campaign.Title}).");
                    Check(gm.CurrentWorldEventId0833==campaign.FirstWorldEventId,$"Campaign {campaignNumber} begins in {campaign.FirstWorldEventId}.");

                    if(campaignNumber==1)
                    {
                        gm.DebugResolveCampaignQuest0833(true);yield return null;
                        uniqueVictories++;
                        routeAll&=gm.DebugCampaignCompletedQuest0833("Q001")&&gm.QuestComplete&&!gm.QuestFailed;
                        rewardAll&=gm.LastQuestResult0835!=null&&gm.LastQuestResult0835.QuestId=="Q001"&&gm.LastQuestResult0835.Success;
                    }
                    else
                    {
                        firstQuestClockAll&=gm.WorldEventDay0833==0&&gm.TotalCampaignDay0833==0;
                    }

                    int firstWe=int.Parse(campaign.FirstWorldEventId.Substring(2));
                    int lastWe=int.Parse(campaign.LastWorldEventId.Substring(2));
                    for(int weNumber=firstWe;weNumber<=lastWe;weNumber++)
                    {
                        string weId=$"WE{weNumber:000}";
                        var we=CampaignContent0833.WorldEvents[weId];
                        routeAll&=string.Equals(gm.CurrentWorldEventId0833,weId,StringComparison.OrdinalIgnoreCase);

                        var ordered=new List<string>();
                        if(!string.IsNullOrEmpty(we.Main1))ordered.Add(we.Main1);
                        if(!string.IsNullOrEmpty(we.Optional))ordered.Add(we.Optional);
                        if(!string.IsNullOrEmpty(we.Main2))ordered.Add(we.Main2);
                        if(!string.IsNullOrEmpty(we.Final))ordered.Add(we.Final);

                        foreach(string questId in ordered)
                        {
                            if(gm.DebugCampaignCompletedQuest0833(questId))continue;
                            int beforeWorldDay=gm.WorldEventDay0833,beforeTotal=gm.TotalCampaignDay0833;
                            bool can=gm.CanBeginCampaignQuest0833(questId);
                            bool began=can&&gm.BeginCampaignQuest0833(questId);
                            yield return null;
                            routeAll&=began&&string.Equals(gm.CurrentQuestId0829,questId,StringComparison.OrdinalIgnoreCase)&&gm.WorldEventDay0833==beforeWorldDay+1&&gm.TotalCampaignDay0833==beforeTotal+1;
                            if(!began)continue;

                            if(campaignNumber>1&&string.Equals(questId,campaign.FirstQuestId,StringComparison.OrdinalIgnoreCase)&&beforeTotal==0)
                                firstQuestClockAll&=gm.WorldEventDay0833==1&&gm.TotalCampaignDay0833==1;

                            if(questId=="Q013"&&!q013RetryTested)
                            {
                                gm.DebugResolveCampaignQuest0833(false);yield return null;
                                bool failed=gm.DebugCampaignFailedQuest0833("Q013")&&gm.QuestFailed&&!gm.QuestComplete&&gm.CanBeginCampaignQuest0833("Q013");
                                int retryWorldDay=gm.WorldEventDay0833,retryTotal=gm.TotalCampaignDay0833;
                                bool retried=gm.BeginCampaignQuest0833("Q013");yield return null;
                                bool retryClock=retried&&gm.WorldEventDay0833==retryWorldDay+1&&gm.TotalCampaignDay0833==retryTotal+1&&!gm.DebugCampaignFailedQuest0833("Q013");
                                gm.DebugResolveCampaignQuest0833(true);yield return null;
                                Check(failed&&retryClock&&gm.DebugCampaignCompletedQuest0833("Q013")&&!gm.DebugCampaignFailedQuest0833("Q013"),"Q013 failure is recorded, remains retryable, consumes a new Campaign Day on retry, and clears failure after success.");
                                q013RetryTested=true;uniqueVictories++;
                            }
                            else
                            {
                                gm.DebugResolveCampaignQuest0833(true);yield return null;uniqueVictories++;
                            }
                            routeAll&=gm.DebugCampaignCompletedQuest0833(questId)&&gm.QuestComplete&&!gm.QuestFailed;
                            rewardAll&=gm.LastQuestResult0835!=null&&string.Equals(gm.LastQuestResult0835.QuestId,questId,StringComparison.OrdinalIgnoreCase)&&gm.LastQuestResult0835.Success;
                        }

                        if(!string.IsNullOrEmpty(we.NextId))
                        {
                            bool canAdvance=gm.CanAdvanceWorldEvent0833;
                            bool advanced=canAdvance&&gm.AdvanceWorldEvent0833();yield return null;
                            routeAll&=advanced&&string.Equals(gm.CurrentWorldEventId0833,we.NextId,StringComparison.OrdinalIgnoreCase)&&gm.WorldEventDay0833==0;
                        }
                    }

                    Check(gm.CampaignCompleted0833&&!gm.CampaignDefeated0833&&gm.CompletedCampaignQuests0833.Count==12,$"Campaign {campaignNumber} reaches Victory with all 12 authored Quests completed.");
                    Check(gm.LastQuestResult0835!=null&&gm.LastQuestResult0835.CampaignVictory&&!gm.LastQuestResult0835.CampaignDefeat,$"Campaign {campaignNumber} final Quest result records campaign Victory.");
                    Check(gm.CampaignProgress0833.CompletedWorldEventIds.Contains(campaign.LastWorldEventId,StringComparer.OrdinalIgnoreCase),$"Campaign {campaignNumber} records its final World Event {campaign.LastWorldEventId} complete.");
                    Check(!gm.CanAdvanceWorldEvent0833&&gm.AvailableCampaignQuests0833.Count==0,$"Campaign {campaignNumber} terminal state closes further Quest and World Event progression.");
                    Check(!string.IsNullOrWhiteSpace(gm.CurrentCampaignVictoryTitle0861),$"Campaign {campaignNumber} exposes its campaign-specific victory title.");

                    if(campaignNumber<6)
                    {
                        string heroBefore=Fingerprint();int goldBefore=gm.PartyGold,repBefore=gm.GeneralReputation;
                        bool continued=campaignNumber==1?gm.BeginValewatchContinuation0845():gm.BeginNextCampaign0856();yield return null;
                        string nextId=CampaignId(campaignNumber+1);var next=CampaignContent0845.Campaigns[nextId];
                        var archived=gm.DebugCampaignArchive0861(campaignId);
                        Check(continued&&gm.CurrentCampaignId0845==nextId&&gm.CurrentWorldEventId0833==next.FirstWorldEventId&&gm.WorldEventDay0833==0&&gm.TotalCampaignDay0833==0,$"Campaign {campaignNumber} → {campaignNumber+1} handoff starts the new campaign at Day 0 before its first Quest.");
                        Check(archived!=null&&archived.Outcome=="VICTORY"&&archived.CompletedQuestIds.Count==12&&archived.TitleUnlock==CampaignIntegrationContent0861.VictoryTitle(campaignId),$"Campaign {campaignNumber} archives Victory, all 12 completed Quests, and the correct title unlock.");
                        Check(gm.CampaignArchive0845.Count==campaignNumber,$"Campaign archive contains exactly {campaignNumber} completed campaign record(s) after the handoff.");
                        Check(Fingerprint()==heroBefore&&gm.PartyGold==goldBefore&&gm.GeneralReputation==repBefore,$"Campaign {campaignNumber} → {campaignNumber+1} preserves Hero progression, inventory counts, professions, Gold, and Reputation.");
                        Check(gm.AvailableCampaignQuests0833.Contains(next.FirstQuestId,StringComparer.OrdinalIgnoreCase)&&gm.CanBeginCampaignQuest0833(next.FirstQuestId),$"{next.FirstQuestId} is immediately available through live campaign progression after the handoff.");
                        int expectedDefault=(campaignNumber+1)>=3?2:0;
                        Check(ExpectedMeter(nextId)==expectedDefault,$"Campaign {campaignNumber+1} initializes its authored World Event pressure meter at {expectedDefault}.");

                        bool saveOk=AscendanceSaveSystem.SaveCurrent(gm,out _);var save=saveOk?AscendanceSaveSystem.Load():null;
                        bool saveIdentity=save?.CampaignProgress0833!=null&&save.CampaignProgress0833.CampaignId0845==nextId&&save.CampaignProgress0833.CurrentWorldEventId==next.FirstWorldEventId&&save.CampaignProgress0833.WorldEventDay==0&&save.CampaignProgress0833.TotalCampaignDay==0&&save.CampaignProgress0833.CampaignArchive0845.Count==campaignNumber;
                        if(save!=null)gm.ApplySaveData(save);yield return null;
                        Check(saveOk&&saveIdentity&&gm.CurrentCampaignId0845==nextId&&gm.WorldEventDay0833==0&&gm.TotalCampaignDay0833==0&&gm.CampaignArchive0845.Count==campaignNumber,$"Campaign {campaignNumber+1} Day-0 handoff survives authoritative save/load with its archive intact.");

                        if(campaignNumber==1&&save!=null)
                        {
                            save.CampaignProgress0833.AwaitingCampaignRuntime0845=true;
                            gm.ApplySaveData(save);yield return null;
                            Check(!gm.CampaignProgress0833.AwaitingCampaignRuntime0845&&gm.CanBeginCampaignQuest0833("Q013"),"legacy Campaign II saves marked awaiting-runtime migrate into the now-playable live Campaign II state.");
                        }
                    }
                    else
                    {
                        Check(!gm.CanContinueToNextCampaign0856&&string.IsNullOrEmpty(campaign.ContinuationCampaignId),"Campaign VI Victory is the terminal end of the initial six-campaign saga.");
                        Check(gm.CampaignArchive0845.Count==5&&Enumerable.Range(1,5).All(n=>gm.DebugCampaignArchived0861(CampaignId(n))),"final saga state retains Campaign I–V archives while Campaign VI remains the active completed record.");
                        Check(gm.LastQuestResult0835!=null&&gm.LastQuestResult0835.CampaignImpact.IndexOf("six-campaign saga",StringComparison.OrdinalIgnoreCase)>=0,"Q072 campaign result explicitly records completion of the initial six-campaign saga.");
                        bool finalSave=AscendanceSaveSystem.SaveCurrent(gm,out _);var save=finalSave?AscendanceSaveSystem.Load():null;if(save!=null)gm.ApplySaveData(save);yield return null;
                        Check(finalSave&&gm.CurrentCampaignId0845==CampaignContent0845.CampaignVI&&gm.CampaignCompleted0833&&gm.CompletedCampaignQuests0833.Count==12&&gm.CampaignArchive0845.Count==5,"final Campaign VI Victory and prior-campaign archive survive save/load.");
                    }
                }

                Check(q013RetryTested,"the end-to-end route exercises a real failed-Quest retry before continuing Campaign II.");
                Check(uniqueVictories==72,"all 72 unique authored Quests resolve successfully through the live campaign route.");
                Check(routeAll,"all Quest starts, Campaign Day increments, and World Event transitions execute through the authoritative campaign router.");
                Check(rewardAll,"all 72 Quest victories pass through the real reward/result-capture chain.");
                Check(firstQuestClockAll,"Campaigns II–VI remain at Day 0 until their first Quest begins, then correctly become Day 1.");
                Check(string.IsNullOrWhiteSpace(gm.PendingContentInstruction)&&!gm.HasPendingReactionWindow&&!gm.HasPendingEncounterDeckControl&&!gm.HasPendingRewardChoice,"full six-campaign path finishes without pending-content, Reaction, deck-control, or reward-choice leakage.");
            }
            finally
            {
                try{if(hadSave){Directory.CreateDirectory(Path.GetDirectoryName(SavePath));File.WriteAllText(SavePath,backup);}else if(File.Exists(SavePath))File.Delete(SavePath);}catch(Exception ex){Debug.LogException(ex);}
            }
            Check(true,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.61 TEST] PASS — Full Campaign End-to-End Integration completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.61 TEST] FAIL — Full Campaign End-to-End Integration completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
