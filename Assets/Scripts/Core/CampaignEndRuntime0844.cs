using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    // 0.8.44 — presentation-only Campaign I terminal summary. Campaign completion itself
    // remains authoritative in CampaignRuntime0833 and Q012 rewards remain authoritative in QuestRuntime0832.
    public sealed partial class AscendanceGameManager
    {
        public bool CampaignEndAvailable0844 => CampaignCompleted0833 || CampaignDefeated0833;
        public string CampaignEndOutcome0844 => CampaignCompleted0833 ? "VICTORY" : CampaignDefeated0833 ? "DEFEAT" : "ACTIVE";
        public string CampaignEndTitle0844 => CampaignCompleted0833 ? CurrentCampaignVictoryTitle0861.ToUpperInvariant() : CurrentCampaignDefeatTitle0861.ToUpperInvariant();

        public string CampaignEndOverview0844
        {
            get
            {
                SyncCampaignProgress0833();if(!CampaignEndAvailable0844)return $"{CurrentCampaignTitle0845} is still active.";
                int completed=CompletedCampaignQuests0833.Count,failed=FailedCampaignQuests0833.Count,missed=MissedCampaignQuests0833.Count;
                string ending=CampaignCompleted0833?$"{CurrentCampaignTitle0845} ends in victory.":$"{CurrentCampaignTitle0845} ends in defeat.";
                return $"{ending}\n\nCampaign: {CurrentCampaignDefinition0845.Number} — {CurrentCampaignTitle0845}\nRegion: {CurrentCampaignRegion0845} • Hub: {CurrentCampaignHub0845}\nCampaign Days: {TotalCampaignDay0833}\nQuests Completed: {completed}/12\nFailed Attempts Recorded: {failed}\nMissed Quests: {missed}\nFinal General Reputation: {GeneralReputation}\nFinal Party Gold: {PartyGold}";
            }
        }

        public string CampaignEndQuestRecord0844
        {
            get
            {
                string completed=CompletedCampaignQuests0833.Count>0?string.Join(", ",CompletedCampaignQuests0833):"None";
                string failed=FailedCampaignQuests0833.Count>0?string.Join(", ",FailedCampaignQuests0833):"None";
                string missed=MissedCampaignQuests0833.Count>0?string.Join(", ",MissedCampaignQuests0833):"None";
                return $"COMPLETED\n{completed}\n\nFAILED ATTEMPTS\n{failed}\n\nMISSED\n{missed}";
            }
        }

        public string CampaignEndPartySummary0844
        {
            get
            {
                var heroes=ParticipatingHeroes.OrderBy(h=>h.PartyIndex).ToList();
                if(heroes.Count==0)return "No participating Heroes recorded.";
                return string.Join("\n",heroes.Select(h=>$"{h.DisplayName}  •  Lv {h.Level}  •  XP {h.XP}  •  Kills {h.KillCount}  •  ATK {h.Attack} / ARM {h.Armor}"));
            }
        }

        public string CampaignEndRewardSummary0844
        {
            get
            {
                if(CampaignDefeated0833)return "No final-Quest victory reward granted.";
                var result=EnsureQuestResultSnapshot0835();string actual=result!=null&&!string.IsNullOrWhiteSpace(result.ActualRewardSummary)?result.ActualRewardSummary:(CurrentQuestDefinition0829?.Reward??"Victory recorded");
                string title=string.IsNullOrWhiteSpace(CurrentCampaignVictoryTitle0861)?string.Empty:$"TITLE UNLOCKED\n{CurrentCampaignVictoryTitle0861}\n\n";
                return $"{title}FINAL QUEST REWARDS\n{actual}";
            }
        }
    }
}
