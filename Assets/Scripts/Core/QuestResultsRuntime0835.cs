using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    [Serializable]
    public sealed class HeroXpDelta0835
    {
        public string HeroId;
        public string HeroName;
        public int Delta;
    }

    [Serializable]
    public sealed class QuestResultSnapshot0835
    {
        public string QuestId;
        public string QuestName;
        public bool Success;
        public bool CampaignVictory;
        public bool CampaignDefeat;
        public string WorldEventId;
        public string WorldEventName;
        public int WorldEventDay;
        public int TotalCampaignDay;
        public string ObjectiveText;
        public string FailureText;
        public string ResolutionText;
        public string AuthoredRewardText;
        public string ActualRewardSummary;
        public string CampaignImpact;
        public int PartyGoldDelta;
        public int GeneralReputationDelta;
        public int NextQuestSupplyDelta;
        public int PartyMoraleDelta;
        public List<HeroXpDelta0835> HeroXpDeltas=new();
        public List<string> GrantedEquipment=new();
        public List<string> RewardLogLines=new();
    }

    // 0.8.35 — presentation snapshot of an already-resolved Quest. This layer does not
    // award rewards or change campaign routing. It records what the authoritative runtimes did.
    public sealed partial class AscendanceGameManager
    {
        public QuestResultSnapshot0835 LastQuestResult0835 { get; private set; }

        private bool resultCaptureActive0835;
        private string resultCaptureQuestId0835;
        private int resultCaptureLogIndex0835;
        private int resultCaptureGold0835;
        private int resultCaptureReputation0835;
        private int resultCaptureSupply0835;
        private int resultCaptureMorale0835;
        private readonly Dictionary<string,int> resultCaptureXp0835=new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,HashSet<string>> resultCaptureEquipment0835=new(StringComparer.OrdinalIgnoreCase);

        private void ClearQuestResultForNewQuest0835()
        {
            LastQuestResult0835=null;
            resultCaptureActive0835=false;
            resultCaptureQuestId0835=null;
            resultCaptureXp0835.Clear();
            resultCaptureEquipment0835.Clear();
        }

        private void BeginQuestResultCapture0835()
        {
            if(QuestRuntime0829==null||resultCaptureActive0835)return;
            resultCaptureActive0835=true;
            resultCaptureQuestId0835=CurrentQuestId0829;
            resultCaptureLogIndex0835=Log.Count;
            resultCaptureGold0835=PartyGold;
            resultCaptureReputation0835=GeneralReputation;
            resultCaptureSupply0835=NextQuestSupply0829;
            resultCaptureMorale0835=PartyMorale0831;
            resultCaptureXp0835.Clear();
            resultCaptureEquipment0835.Clear();
            foreach(var h in ParticipatingHeroes)
            {
                resultCaptureXp0835[h.Id]=h.XP;
                resultCaptureEquipment0835[h.Id]=new HashSet<string>(h.AcquiredEquipment,StringComparer.OrdinalIgnoreCase);
            }
        }

        private void FinalizeQuestResultCapture0835(bool success)
        {
            if(!resultCaptureActive0835||!string.Equals(resultCaptureQuestId0835,CurrentQuestId0829,StringComparison.OrdinalIgnoreCase))
            {
                resultCaptureActive0835=false;
                EnsureQuestResultSnapshot0835();
                return;
            }

            SyncCampaignProgress0833();
            var def=CurrentQuestDefinition0829;
            var snapshot=new QuestResultSnapshot0835
            {
                QuestId=CurrentQuestId0829,
                QuestName=CurrentQuestName0829,
                Success=success,
                CampaignVictory=CampaignCompleted0833,
                CampaignDefeat=CampaignDefeated0833,
                WorldEventId=CurrentWorldEventId0833,
                WorldEventName=CurrentWorldEventName0833,
                WorldEventDay=WorldEventDay0833,
                TotalCampaignDay=TotalCampaignDay0833,
                ObjectiveText=def?.Victory??string.Empty,
                FailureText=def?.Failure??string.Empty,
                ResolutionText=FindQuestResolutionLine0835(CurrentQuestId0829,success),
                AuthoredRewardText=def?.Reward??string.Empty,
                PartyGoldDelta=PartyGold-resultCaptureGold0835,
                GeneralReputationDelta=GeneralReputation-resultCaptureReputation0835,
                NextQuestSupplyDelta=NextQuestSupply0829-resultCaptureSupply0835,
                PartyMoraleDelta=PartyMorale0831-resultCaptureMorale0835
            };

            foreach(var h in ParticipatingHeroes)
            {
                int before=resultCaptureXp0835.TryGetValue(h.Id,out var xp)?xp:h.XP;
                int delta=h.XP-before;
                snapshot.HeroXpDeltas.Add(new HeroXpDelta0835{HeroId=h.Id,HeroName=h.DisplayName,Delta=delta});
                var beforeGear=resultCaptureEquipment0835.TryGetValue(h.Id,out var ids)?ids:new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var id in h.AcquiredEquipment.Where(id=>!beforeGear.Contains(id)))
                    snapshot.GrantedEquipment.Add($"{h.DisplayName} — {RewardContent0826.EquipmentLabel(id)}");
            }

            snapshot.RewardLogLines=Log.Skip(Mathf.Clamp(resultCaptureLogIndex0835,0,Log.Count))
                .Where(IsQuestResultLogLine0835).ToList();
            snapshot.ActualRewardSummary=BuildActualRewardSummary0835(snapshot);
            snapshot.CampaignImpact=BuildCampaignImpact0835(snapshot.Success);
            LastQuestResult0835=snapshot;
            resultCaptureActive0835=false;
            resultCaptureQuestId0835=null;
            resultCaptureXp0835.Clear();
            resultCaptureEquipment0835.Clear();
        }

        public QuestResultSnapshot0835 EnsureQuestResultSnapshot0835()
        {
            if(!QuestComplete&&!QuestFailed)return null;
            if(LastQuestResult0835!=null&&string.Equals(LastQuestResult0835.QuestId,CurrentQuestId0829,StringComparison.OrdinalIgnoreCase))
            {
                LastQuestResult0835.CampaignVictory=CampaignCompleted0833;
                LastQuestResult0835.CampaignDefeat=CampaignDefeated0833;
                LastQuestResult0835.CampaignImpact=BuildCampaignImpact0835(LastQuestResult0835.Success);
                return LastQuestResult0835;
            }

            SyncCampaignProgress0833();
            var def=CurrentQuestDefinition0829;
            bool success=QuestComplete&&!QuestFailed;
            var snapshot=new QuestResultSnapshot0835
            {
                QuestId=CurrentQuestId0829,
                QuestName=CurrentQuestName0829,
                Success=success,
                CampaignVictory=CampaignCompleted0833,
                CampaignDefeat=CampaignDefeated0833,
                WorldEventId=CurrentWorldEventId0833,
                WorldEventName=CurrentWorldEventName0833,
                WorldEventDay=WorldEventDay0833,
                TotalCampaignDay=TotalCampaignDay0833,
                ObjectiveText=def?.Victory??string.Empty,
                FailureText=def?.Failure??string.Empty,
                ResolutionText=FindQuestResolutionLine0835(CurrentQuestId0829,success),
                AuthoredRewardText=def?.Reward??string.Empty
            };
            snapshot.ActualRewardSummary=success?("Authored reward (legacy/resumed result): "+(def?.Reward??"Reward resolved.")):"No victory reward granted.";
            snapshot.CampaignImpact=BuildCampaignImpact0835(success);
            LastQuestResult0835=snapshot;
            return snapshot;
        }

        public string CurrentQuestResultCampaignImpact0835 => BuildCampaignImpact0835(QuestComplete&&!QuestFailed);

        private string BuildCampaignImpact0835(bool success)
        {
            SyncCampaignProgress0833();
            if(CampaignCompleted0833)
            {
                string saga=CurrentCampaignId0845==CampaignContent0845.CampaignVI?" The initial six-campaign saga is complete.":string.Empty;
                return $"CAMPAIGN {CurrentCampaignDefinition0845.Number} VICTORY — {CurrentCampaignTitle0845} completed. {CurrentCampaignVictoryTitle0861} unlocked. No further Campaign {CurrentCampaignDefinition0845.Number} Quest is available.{saga}";
            }
            if(CampaignDefeated0833)return $"CAMPAIGN {CurrentCampaignDefinition0845.Number} DEFEAT — {CurrentCampaignDefeatTitle0861} is recorded. Campaign {CurrentCampaignDefinition0845.Number} Quest selection is closed.";

            var parts=new List<string>();
            string status=GetCampaignQuestStatusLabel0834(CurrentQuestId0829);
            parts.Add($"{CurrentQuestId0829}: {status}");
            if(!string.IsNullOrWhiteSpace(CurrentWorldEventProgressLabel0834))parts.Add(CurrentWorldEventProgressLabel0834);
            if(!success&&CanBeginCampaignQuest0833(CurrentQuestId0829))parts.Add("Retry available while this World Event remains active.");

            var available=AvailableCampaignQuests0833;
            if(available.Count>0)parts.Add("Unlocked now: "+string.Join(", ",available.Select(id=>$"{id} {QuestContent0829.Definitions[id].Name}"))+".");
            if(CanAdvanceWorldEvent0833)parts.Add("World Event advancement is available.");
            else parts.Add($"{CurrentWorldEventId0833} {CurrentWorldEventName0833} remains active — Day {WorldEventDay0833}"+(CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w)&&w.DayLimit>0?$"/{w.DayLimit}":string.Empty)+".");

            if(MissedCampaignQuests0833.Count>0)parts.Add("Missed Quests: "+string.Join(", ",MissedCampaignQuests0833)+".");
            if(CanAdvanceWorldEvent0833&&!string.IsNullOrWhiteSpace(CurrentWorldEventOutcomePreview0834))parts.Add(CurrentWorldEventOutcomePreview0834);
            return string.Join("\n",parts);
        }

        private string FindQuestResolutionLine0835(string questId,bool success)
        {
            if(string.IsNullOrEmpty(questId))return string.Empty;
            for(int i=Log.Count-1;i>=0;i--)
            {
                var line=Log[i]??string.Empty;
                if(success&&line.StartsWith(questId+" COMPLETE",StringComparison.OrdinalIgnoreCase))return line;
                if(!success&&line.StartsWith(questId+" FAILED",StringComparison.OrdinalIgnoreCase))return line;
            }
            var def=QuestContent0829.Definitions.TryGetValue(questId,out var d)?d:null;
            return success?"Quest objectives completed.":(def==null?"Quest failed.":"Failure condition: "+def.Failure);
        }

        private static bool IsQuestResultLogLine0835(string line)
        {
            if(string.IsNullOrWhiteSpace(line))return false;
            return line.IndexOf(" REWARD —",StringComparison.OrdinalIgnoreCase)>=0
                ||line.StartsWith("GOLD —",StringComparison.OrdinalIgnoreCase)
                ||line.StartsWith("REPUTATION —",StringComparison.OrdinalIgnoreCase)
                ||line.StartsWith("EQUIPMENT —",StringComparison.OrdinalIgnoreCase)
                ||line.StartsWith("QUEST ITEM RETURN —",StringComparison.OrdinalIgnoreCase)
                ||line.StartsWith("CAMPAIGN I —",StringComparison.OrdinalIgnoreCase);
        }

        private static string Signed0835(int value)=>value>0?"+"+value:value.ToString();

        private string BuildActualRewardSummary0835(QuestResultSnapshot0835 snapshot)
        {
            if(snapshot==null||!snapshot.Success)return "No victory reward granted.";
            var parts=new List<string>();
            var xp=snapshot.HeroXpDeltas.Where(x=>x.Delta!=0).ToList();
            if(xp.Count>0)
            {
                bool same=xp.Select(x=>x.Delta).Distinct().Count()==1&&xp.Count==ParticipatingHeroes.Count();
                parts.Add(same?$"XP {Signed0835(xp[0].Delta)} each":"XP: "+string.Join(", ",xp.Select(x=>$"{x.HeroName} {Signed0835(x.Delta)}")));
            }
            if(snapshot.PartyGoldDelta!=0)parts.Add($"Party Gold {Signed0835(snapshot.PartyGoldDelta)}");
            if(snapshot.GeneralReputationDelta!=0)parts.Add($"General Reputation {Signed0835(snapshot.GeneralReputationDelta)}");
            if(snapshot.NextQuestSupplyDelta!=0)parts.Add($"Next-Quest Supply {Signed0835(snapshot.NextQuestSupplyDelta)}");
            if(snapshot.PartyMoraleDelta!=0)parts.Add($"Party Morale {Signed0835(snapshot.PartyMoraleDelta)}");
            if(snapshot.GrantedEquipment.Count>0)parts.Add("Equipment: "+string.Join("; ",snapshot.GrantedEquipment));
            string runtime=snapshot.RewardLogLines.LastOrDefault(x=>x.IndexOf($"{snapshot.QuestId} REWARD",StringComparison.OrdinalIgnoreCase)>=0);
            if(!string.IsNullOrEmpty(runtime))parts.Add(runtime);
            if(parts.Count==0)parts.Add(string.IsNullOrWhiteSpace(snapshot.AuthoredRewardText)?"Reward resolved.":snapshot.AuthoredRewardText);
            return string.Join("\n",parts);
        }

        public QuestResultSaveData0835 ExportQuestResult0835()
        {
            var s=EnsureQuestResultSnapshot0835();
            if(s==null)return null;
            return new QuestResultSaveData0835
            {
                QuestId=s.QuestId,QuestName=s.QuestName,Success=s.Success,CampaignVictory=s.CampaignVictory,CampaignDefeat=s.CampaignDefeat,
                WorldEventId=s.WorldEventId,WorldEventName=s.WorldEventName,WorldEventDay=s.WorldEventDay,TotalCampaignDay=s.TotalCampaignDay,
                ObjectiveText=s.ObjectiveText,FailureText=s.FailureText,ResolutionText=s.ResolutionText,AuthoredRewardText=s.AuthoredRewardText,
                ActualRewardSummary=s.ActualRewardSummary,CampaignImpact=s.CampaignImpact,PartyGoldDelta=s.PartyGoldDelta,GeneralReputationDelta=s.GeneralReputationDelta,
                NextQuestSupplyDelta=s.NextQuestSupplyDelta,PartyMoraleDelta=s.PartyMoraleDelta,
                HeroXpDeltas=s.HeroXpDeltas.Select(x=>new HeroXpDeltaSaveData0835{HeroId=x.HeroId,HeroName=x.HeroName,Delta=x.Delta}).ToList(),
                GrantedEquipment=s.GrantedEquipment.ToList(),RewardLogLines=s.RewardLogLines.ToList()
            };
        }

        private void RestoreQuestResult0835(AscendanceSaveData save)
        {
            var s=save?.QuestResult0835;
            if(s==null){LastQuestResult0835=null;return;}
            LastQuestResult0835=new QuestResultSnapshot0835
            {
                QuestId=s.QuestId,QuestName=s.QuestName,Success=s.Success,CampaignVictory=s.CampaignVictory,CampaignDefeat=s.CampaignDefeat,
                WorldEventId=s.WorldEventId,WorldEventName=s.WorldEventName,WorldEventDay=s.WorldEventDay,TotalCampaignDay=s.TotalCampaignDay,
                ObjectiveText=s.ObjectiveText,FailureText=s.FailureText,ResolutionText=s.ResolutionText,AuthoredRewardText=s.AuthoredRewardText,
                ActualRewardSummary=s.ActualRewardSummary,CampaignImpact=s.CampaignImpact,PartyGoldDelta=s.PartyGoldDelta,GeneralReputationDelta=s.GeneralReputationDelta,
                NextQuestSupplyDelta=s.NextQuestSupplyDelta,PartyMoraleDelta=s.PartyMoraleDelta,
                HeroXpDeltas=s.HeroXpDeltas?.Select(x=>new HeroXpDelta0835{HeroId=x.HeroId,HeroName=x.HeroName,Delta=x.Delta}).ToList()??new List<HeroXpDelta0835>(),
                GrantedEquipment=s.GrantedEquipment?.ToList()??new List<string>(),RewardLogLines=s.RewardLogLines?.ToList()??new List<string>()
            };
        }
    }
}
