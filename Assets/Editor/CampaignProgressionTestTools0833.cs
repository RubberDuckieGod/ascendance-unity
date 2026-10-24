#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CampaignProgressionTestTools0833
    {
        [MenuItem("Ascendance/Validation/Test 0.8.33 - Campaign I World Events (Play Mode)")]
        public static void TestCampaignProgression()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.33 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.33 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.33 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.33 TEST] FAIL — "+label);}}
            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugResetCampaignProgress0833();yield return null;

            Check(gm.CurrentWorldEventId0833=="WE001"&&gm.WorldEventDay0833==1&&gm.TotalCampaignDay0833==1&&gm.CurrentQuestId0829=="Q001","Campaign I starts in WE001 on Campaign Day 1 with Q001 active.");
            Check(!gm.CanAdvanceWorldEvent0833&&gm.AvailableCampaignQuests0833.Count==0,"An unresolved active Quest blocks campaign Quest selection and World Event advancement.");

            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.DebugCampaignCompletedQuest0833("Q001")&&gm.RecommendedMainCampaignQuest0833=="Q002"&&gm.OptionalCampaignQuest0833=="Q003","After Q001, Q002 is the required path and Q003 is available as the optional Quest.");
            Check(gm.BeginOptionalCampaignQuest0833()&&gm.CurrentQuestId0829=="Q003"&&gm.WorldEventDay0833==2,"Starting optional Q003 uses the campaign router and advances WE001 to Day 2.");
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q002"&&gm.WorldEventDay0833==3,"Q002 starts through the campaign router after optional Q003 and advances WE001 to Day 3.");
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.CanAdvanceWorldEvent0833&&gm.DebugCampaignCompletedQuest0833("Q001")&&gm.DebugCampaignCompletedQuest0833("Q002")&&gm.DebugCampaignCompletedQuest0833("Q003"),"WE001 can advance after both required Quests while preserving the completed optional Quest.");
            int we1xp=gm.Heroes["Garruk"].XP,we1morale=gm.PartyMorale0831;
            Check(gm.AdvanceWorldEvent0833()&&gm.CurrentWorldEventId0833=="WE002"&&gm.WorldEventDay0833==0,"WE001 advances to WE002 and resets the chapter-day counter.");
            Check(gm.Heroes["Garruk"].XP==we1xp+50&&gm.PartyMorale0831==we1morale+1&&gm.RemoveScoutFromQ004Setup0833,"WE001 all-quests reward grants +50 XP each, +1 Party Morale, and the Q004 Scout reduction.");
            Check(gm.RecommendedMainCampaignQuest0833=="Q004"&&gm.OptionalCampaignQuest0833=="Q006","WE002 exposes Q004 as the required path and Q006 as optional.");

            Check(gm.BeginOptionalCampaignQuest0833()&&gm.CurrentQuestId0829=="Q006"&&gm.WorldEventDay0833==1,"WE002 optional Q006 may be taken before the required path.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q004"&&gm.WorldEventDay0833==2,"WE002 required Q004 starts after the optional Quest.");
            gm.DebugRevealQuestLocation0829("L006","Garruk");yield return null;
            Check(gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q004"&&e.TemplateId=="E004")==1,"WE001 completion bonus mechanically removes one Scout from Q004's first patrol.");
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q005"&&gm.WorldEventDay0833==3,"Q005 unlocks after Q004 and starts through campaign progression.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.RemoveEliteFromQ007Setup0830,"Q005's real Quest reward banks the authored remove-1-Elite consequence for Q007.");
            int we2xp=gm.Heroes["Garruk"].XP,we2rep=gm.GeneralReputation,we2gear=gm.Heroes["Garruk"].AcquiredEquipment.Count;
            Check(gm.CanAdvanceWorldEvent0833&&gm.AdvanceWorldEvent0833()&&gm.CurrentWorldEventId0833=="WE003","Completing Q004/Q005 allows WE002 to advance to WE003.");
            bool we2rare=gm.Heroes["Garruk"].AcquiredEquipment.Skip(we2gear).Any(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&e.Rarity=="Rare");
            Check(gm.Heroes["Garruk"].XP==we2xp+75&&gm.GeneralReputation==Mathf.Min(6,we2rep+1)&&we2rare&&gm.RemoveEliteFromQ007Setup0830,"WE002 all-quests reward grants +75 XP, Rare Equipment, +1 Reputation, and removes Q007's opening Elite.");

            Check(gm.BeginOptionalCampaignQuest0833()&&gm.CurrentQuestId0829=="Q009"&&gm.WorldEventDay0833==1,"WE003 optional Q009 is campaign-selectable on Day 1.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.CaptainRolandAssistsQ0100832,"Q009's real Quest reward banks Captain Roland's additional-Ally allowance for Q010.");
            Check(gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q007"&&gm.WorldEventDay0833==2,"WE003 required Q007 starts after optional Q009.");
            gm.DebugRevealQuestLocation0829("L005","Garruk");yield return null;
            Check(gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q007"&&e.TemplateId=="E013")==0,"WE002 all-quests reward mechanically removes the initial Q007 Wolf Rider/Elite.");
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.RemoveSiegeEngineFromQ008Setup0831,"Q007's real Quest reward banks the authored remove-1-Siege-Engine consequence for Q008.");
            Check(gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q008"&&gm.WorldEventDay0833==3,"Q008 unlocks after Q007 and advances WE003 to Day 3 when started.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            int we3xp=gm.Heroes["Garruk"].XP,we3rep=gm.GeneralReputation,we3gear=gm.Heroes["Garruk"].AcquiredEquipment.Count;
            Check(gm.AdvanceWorldEvent0833()&&gm.CurrentWorldEventId0833=="WE004","WE003 advances to the final WE004 chapter after Q007/Q008.");
            bool we3epic=gm.Heroes["Garruk"].AcquiredEquipment.Skip(we3gear).Any(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&e.Rarity=="Epic");
            Check(gm.Heroes["Garruk"].XP==we3xp+100&&gm.GeneralReputation==Mathf.Min(6,we3rep+2)&&we3epic&&gm.Q010SiegePressureModifier0833==-1&&gm.DamagedBuildings0833==0,"WE003 all-quests reward grants +100 XP, Epic Equipment, +2 Reputation, restores buildings, and banks Siege Pressure -1.");
            Check(gm.RecommendedMainCampaignQuest0833=="Q010"&&gm.OptionalCampaignQuest0833==null,"WE004 begins with Q010 and has no optional Quest branch.");

            Check(gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q010"&&gm.WorldEventDay0833==1,"Q010 starts as WE004 Day 1 through campaign progression.");
            Check(gm.CaptainRolandAssistsQ0100832,"Captain Roland's Q009 assistance flag survives the WE003→WE004 transition into Q010.");gm.DebugRevealQuestLocation0829("L017","Garruk");yield return null;
            Check(gm.QuestObjects0829.Count(o=>o.Type=="Ballista")==2&&gm.Q010SiegePressureModifier0833==-1,"WE003 Siege Pressure is retained without rewriting Q010's print-locked two Ballistas.");
            gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.RecommendedMainCampaignQuest0833=="Q011"&&gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q011"&&gm.WorldEventDay0833==2,"Q011 unlocks only after Q010 and starts on WE004 Day 2.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.RecommendedMainCampaignQuest0833=="Q012"&&gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q012"&&gm.WorldEventDay0833==3,"Q012 unlocks only after Q011 and starts on WE004 Day 3.");gm.DebugResolveCampaignQuest0833(true);yield return null;
            Check(gm.CampaignCompleted0833&&!gm.CampaignDefeated0833&&gm.DebugCampaignCompletedQuest0833("Q012")&&gm.DebugDefenderUnlocked0832,"Q012 completion records Campaign I Victory and Defender of Stonebrook.");
            Check(gm.AvailableCampaignQuests0833.Count==0&&!gm.CanAdvanceWorldEvent0833,"Campaign Victory closes Quest selection and WE004 has no further World Event.");

            AscendanceSaveSystem.SaveCurrent(gm,out _);var save=AscendanceSaveSystem.Load();
            Check(save?.CampaignProgress0833!=null&&save.CampaignProgress0833.CampaignCompleted&&save.CampaignProgress0833.CurrentWorldEventId=="WE004"&&save.CampaignProgress0833.CompletedQuestIds.Contains("Q012"),"Campaign World Event, Quest history, and victory persist through the real save system.");
            gm.DebugResetCampaignProgress0833();gm.ApplySaveData(save);yield return null;
            Check(gm.CampaignCompleted0833&&gm.CurrentWorldEventId0833=="WE004"&&gm.DebugCampaignCompletedQuest0833("Q012"),"Applying the save restores Campaign I World Event progression and completion state.");

            // Pressure route: prove Campaign Days can advance a chapter before required Quests are complete.
            gm.DebugResetCampaignProgress0833();int penaltyRep=gm.GeneralReputation;gm.DebugResolveCampaignQuest0833(false);gm.DebugAdvanceCampaignDay0833(4);yield return null;
            Check(gm.WorldEventDay0833==5&&gm.CanAdvanceWorldEvent0833,"WE001 may advance on its authored 5-Day threshold even without both required Quests.");
            Check(gm.AdvanceWorldEvent0833()&&gm.CurrentWorldEventId0833=="WE002"&&gm.AddRaiderToQ004FirstEncounter0833&&gm.GeneralReputation==Mathf.Max(-5,penaltyRep-1),"WE001 incomplete penalty banks the extra Q004 Raider and loses 1 Reputation.");
            Check(gm.DebugCampaignMissedQuest0833("Q001")&&gm.DebugCampaignMissedQuest0833("Q002")&&gm.DebugCampaignMissedQuest0833("Q003"),"Advancing a chapter records its unfinished Quests as missed.");
            gm.DebugAdvanceCampaignDay0833(6);int we2PenaltyRep=gm.GeneralReputation;Check(gm.CanAdvanceWorldEvent0833&&gm.AdvanceWorldEvent0833(),"WE002 may advance on its authored 6-Day threshold without required Quest completion.");
            Check(gm.CurrentWorldEventId0833=="WE003"&&gm.AddEliteToQ007Setup0833&&gm.WE003TravelSeverityPenalty0833==1&&gm.GeneralReputation==Mathf.Max(-5,we2PenaltyRep-1),"WE002 incomplete penalty adds a Q007 Elite, WE003 Travel +1, and -1 Reputation.");
            Check(gm.WorldEventTravelSeverityModifier0833==1,"WE003 campaign penalty is exposed as the active +1 Travel Encounter severity modifier.");
            gm.DebugAdvanceCampaignDay0833(6);int we3PenaltyRep=gm.GeneralReputation;Check(gm.CanAdvanceWorldEvent0833&&gm.AdvanceWorldEvent0833(),"WE003 may advance on its authored 6-Day threshold without required Quest completion.");
            Check(gm.CurrentWorldEventId0833=="WE004"&&gm.Q010SiegePressureModifier0833==1&&gm.Q010WoundPenalty0833&&gm.StonebrookEquipmentPurchaseLocked0833&&gm.GeneralReputation==Mathf.Max(-5,we3PenaltyRep-2),"WE003 incomplete penalty records Siege Pressure +1, Q010 Wounds, purchasing lock, and -2 Reputation.");
            Check(gm.BeginRecommendedCampaignQuest0833()&&gm.CurrentQuestId0829=="Q010","Penalty-route WE004 still begins Q010 through the campaign router.");yield return null;
            Check(gm.ParticipatingHeroes.All(h=>h.Statuses.Count(s=>s.Name=="Wound Token")==1),"WE003 incomplete penalty gives each Hero exactly one persistent Wound Token at Q010 start.");
            gm.DebugRevealQuestLocation0829("L017","Garruk");yield return null;
            Check(gm.QuestObjects0829.Count(o=>o.Type=="Ballista")==2&&gm.Q010SiegePressureModifier0833==1,"Penalty Siege Pressure remains recorded while Q010's two-Ballista print lock stays intact.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.33 TEST] PASS — Campaign I World Event progression completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.33 TEST] FAIL — Campaign I World Event progression completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
