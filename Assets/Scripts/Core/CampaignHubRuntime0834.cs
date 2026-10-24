using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public enum CampaignQuestStatus0834
    {
        Locked,
        Available,
        Active,
        Completed,
        Failed,
        Missed
    }

    // 0.8.34 — Presentation-facing campaign queries. This file deliberately contains no
    // campaign advancement rules; it reads the authoritative 0.8.33 campaign runtime.
    public sealed partial class AscendanceGameManager
    {
        public IReadOnlyList<string> CurrentWorldEventQuestIds0834
        {
            get
            {
                SyncCampaignProgress0833();
                if(!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w))return Array.Empty<string>();
                return new[]{w.Main1,w.Main2,w.Optional,w.Final}.Where(x=>!string.IsNullOrEmpty(x)).ToArray();
            }
        }

        public CampaignQuestStatus0834 GetCampaignQuestStatus0834(string questId)
        {
            SyncCampaignProgress0833();
            if(string.IsNullOrEmpty(questId))return CampaignQuestStatus0834.Locked;
            if(Completed0833(questId))return CampaignQuestStatus0834.Completed;
            if(CampaignProgress0833?.MissedQuestIds?.Contains(questId,StringComparer.OrdinalIgnoreCase)??false)return CampaignQuestStatus0834.Missed;
            if(string.Equals(CurrentQuestId0829,questId,StringComparison.OrdinalIgnoreCase)&&!QuestComplete&&!QuestFailed)return CampaignQuestStatus0834.Active;
            if(Failed0833(questId))return CampaignQuestStatus0834.Failed;
            if(AvailableCampaignQuests0833.Contains(questId,StringComparer.OrdinalIgnoreCase))return CampaignQuestStatus0834.Available;
            return CampaignQuestStatus0834.Locked;
        }

        public string GetCampaignQuestStatusLabel0834(string questId)
        {
            var status=GetCampaignQuestStatus0834(questId);
            if(status==CampaignQuestStatus0834.Failed&&CanBeginCampaignQuest0833(questId))return "FAILED • RETRY AVAILABLE";
            return status switch
            {
                CampaignQuestStatus0834.Available=>"AVAILABLE",
                CampaignQuestStatus0834.Active=>"ACTIVE QUEST",
                CampaignQuestStatus0834.Completed=>"COMPLETED",
                CampaignQuestStatus0834.Failed=>"FAILED",
                CampaignQuestStatus0834.Missed=>"MISSED",
                _=>"LOCKED"
            };
        }

        public string GetCampaignQuestPathLabel0834(string questId)
        {
            if(!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w))return "QUEST";
            if(string.Equals(questId,w.Main1,StringComparison.OrdinalIgnoreCase))return "REQUIRED I";
            if(string.Equals(questId,w.Main2,StringComparison.OrdinalIgnoreCase))return "REQUIRED II";
            if(string.Equals(questId,w.Final,StringComparison.OrdinalIgnoreCase))return "FINAL REQUIRED";
            if(string.Equals(questId,w.Optional,StringComparison.OrdinalIgnoreCase))return "OPTIONAL";
            return "QUEST";
        }

        public float CurrentWorldEventDayPressure0834
        {
            get
            {
                SyncCampaignProgress0833();
                if(!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w))return 0f;
                if(w.DayLimit>0)return Mathf.Clamp01(WorldEventDay0833/(float)w.DayLimit);
                int complete=CurrentWorldEventQuestIds0834.Count(id=>Completed0833(id));
                return Mathf.Clamp01(complete/3f);
            }
        }

        public string CurrentWorldEventProgressLabel0834
        {
            get
            {
                SyncCampaignProgress0833();
                if(!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w))return string.Empty;
                int completed=CurrentWorldEventQuestIds0834.Count(id=>Completed0833(id));
                if(w.DayLimit>0)return $"WORLD EVENT DAY {WorldEventDay0833}/{w.DayLimit}  •  {completed}/3 QUESTS COMPLETE  •  TOTAL CAMPAIGN DAY {TotalCampaignDay0833}";
                return $"FINAL WORLD EVENT  •  {completed}/3 QUESTS COMPLETE  •  TOTAL CAMPAIGN DAY {TotalCampaignDay0833}";
            }
        }

        public string CurrentWorldEventOutcomePreview0834
        {
            get
            {
                SyncCampaignProgress0833();
                if(CampaignCompleted0833)return $"CAMPAIGN {CurrentCampaignDefinition0845.Number} VICTORY — {CurrentCampaignTitle0845} completed.";
                if(CampaignDefeated0833)return $"CAMPAIGN {CurrentCampaignDefinition0845.Number} DEFEAT — {CurrentCampaignTitle0845} has ended in defeat.";
                if(!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w))return string.Empty;

                int required=new[]{w.Main1,w.Main2}.Count(Completed0833);
                int complete=new[]{w.Main1,w.Main2,w.Optional}.Count(x=>!string.IsNullOrEmpty(x)&&Completed0833(x));

                if(!string.IsNullOrEmpty(w.Final))
                    return $"FINAL CHAPTER — complete {w.Main1}"+(string.IsNullOrEmpty(w.Main2)?string.Empty:$" → {w.Main2}")+$" → {w.Final}. {w.Final} resolves Campaign {CurrentCampaignDefinition0845.Number} Victory or Defeat; {w.Id} has no manual World Event advance.";

                if(w.Id=="WE001")
                {
                    if(complete==3)return "PERFECT CLEAR — advancing grants +50 XP each • +1 Party Morale • remove 1 Goblin Scout from Q004.";
                    if(required==2)return "CHAPTER SECURED — you may advance now, or complete Q003 first for the Perfect Clear bonus: +50 XP each • +1 Party Morale • remove 1 Goblin Scout from Q004.";
                    return CanAdvanceWorldEvent0833?"DAY LIMIT REACHED — advancing now applies the WE001 penalty: +1 Raider in Q004 first patrol • -1 General Reputation.":"DAY-LIMIT RISK — if WE001 ends before both required Quests are complete: +1 Raider in Q004 first patrol • -1 General Reputation.";
                }
                if(w.Id=="WE002")
                {
                    if(complete==3)return "PERFECT CLEAR — advancing grants +75 XP each • 1 Rare Equipment • remove 1 Elite from Q007 • +1 Reputation.";
                    if(required==2)return "CHAPTER SECURED — you may advance now, or complete Q006 first for the Perfect Clear bonus: +75 XP each • 1 Rare Equipment • remove 1 Elite from Q007 • +1 Reputation.";
                    return CanAdvanceWorldEvent0833?"DAY LIMIT REACHED — advancing now applies the WE002 penalty: +1 Elite in Q007 • WE003 Travel Encounter rolls +1 • -1 Reputation.":"DAY-LIMIT RISK — if WE002 ends before both required Quests are complete: +1 Elite in Q007 • WE003 Travel Encounter rolls +1 • -1 Reputation.";
                }
                if(w.Id=="WE003")
                {
                    if(complete==3)return "PERFECT CLEAR — advancing grants +100 XP each • 1 Epic Equipment • +2 Reputation • Q010 Siege Pressure -1 • restore all Damaged Buildings.";
                    if(required==2)return "CHAPTER SECURED — advancing begins WE004 with 1 Damaged Building. Complete Q009 first for the Perfect Clear reward and Siege Pressure -1.";
                    return CanAdvanceWorldEvent0833?"DAY LIMIT REACHED — advancing now applies the WE003 penalty: Q010 Siege Pressure +1 • 1 Wound Token per Hero • -2 Reputation • Stonebrook Equipment purchasing locked before WE004.":"DAY-LIMIT RISK — if WE003 ends before both required Quests are complete: Q010 Siege Pressure +1 • 1 Wound Token per Hero • -2 Reputation • purchasing lock before WE004.";
                }
                return CurrentWorldEventAuthoredPreview0857;
            }
        }
    }
}
