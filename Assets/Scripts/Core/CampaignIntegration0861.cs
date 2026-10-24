using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    // 0.8.61 — six-campaign integration helpers shared by campaign transitions,
    // terminal presentation, and the end-to-end validator. No Quest/World Event
    // authored rules are redefined here.
    public static class CampaignIntegrationContent0861
    {
        public static string VictoryTitle(string campaignId)
        {
            return CampaignContent0845.NormalizeCampaignId0845(campaignId,null) switch
            {
                CampaignContent0845.CampaignI => "Defender of Stonebrook",
                CampaignContent0845.CampaignII => "Wardens of Valewatch",
                CampaignContent0845.CampaignIII => "Heartwardens of Ironhaven",
                CampaignContent0845.CampaignIV => "Wardens of the Verdant Reach",
                CampaignContent0845.CampaignV => "Stormwardens of Ashenport",
                CampaignContent0845.CampaignVI => "Dawnbearers of Sunspire",
                _ => string.Empty
            };
        }

        public static string DefeatTitle(string campaignId)
        {
            return CampaignContent0845.NormalizeCampaignId0845(campaignId,null) switch
            {
                CampaignContent0845.CampaignI => "Stonebrook Falls",
                CampaignContent0845.CampaignII => "Valewatch Fallen",
                CampaignContent0845.CampaignIII => "Ironhaven Broken",
                CampaignContent0845.CampaignIV => "Verdant Reach Withered",
                CampaignContent0845.CampaignV => "Ashenport Drowned",
                CampaignContent0845.CampaignVI => "Black Sun Ascendant",
                _ => "Campaign Defeat"
            };
        }
    }

    public sealed partial class AscendanceGameManager
    {
        public string CurrentCampaignVictoryTitle0861 => CampaignIntegrationContent0861.VictoryTitle(CurrentCampaignId0845);
        public string CurrentCampaignDefeatTitle0861 => CampaignIntegrationContent0861.DefeatTitle(CurrentCampaignId0845);

        private CampaignArchiveEntry0845 BuildCampaignArchiveEntry0861(CampaignDefinition0845 campaign)
        {
            if(campaign==null)return null;
            return new CampaignArchiveEntry0845
            {
                CampaignId=campaign.Id,
                CampaignTitle=campaign.Title,
                Outcome=CampaignCompleted0833?"VICTORY":CampaignDefeated0833?"DEFEAT":"ACTIVE",
                TitleUnlock=CampaignCompleted0833?CampaignIntegrationContent0861.VictoryTitle(campaign.Id):string.Empty,
                CampaignDays=TotalCampaignDay0833,
                CompletedQuestIds=CompletedCampaignQuests0833.ToList(),
                FailedQuestIds=FailedCampaignQuests0833.ToList(),
                MissedQuestIds=MissedCampaignQuests0833.ToList()
            };
        }

        private CampaignProgressState0833 CreateFreshContinuationCampaign0861(CampaignDefinition0845 next,List<CampaignArchiveEntry0845> archive)
        {
            return new CampaignProgressState0833
            {
                CampaignId0845=next.Id,
                AwaitingCampaignRuntime0845=false,
                CampaignArchive0845=archive??new List<CampaignArchiveEntry0845>(),
                CurrentWorldEventId=next.FirstWorldEventId,
                // A continuation campaign has not consumed a Campaign Day until its first Quest starts.
                WorldEventDay=0,
                TotalCampaignDay=0,
                CampaignCompleted=false,
                CampaignDefeated=false
            };
        }

        public bool DebugCampaignArchived0861(string campaignId)
        {
            return CampaignArchive0845.Any(x=>x!=null&&string.Equals(x.CampaignId,campaignId,StringComparison.OrdinalIgnoreCase));
        }

        public CampaignArchiveEntry0845 DebugCampaignArchive0861(string campaignId)
        {
            return CampaignArchive0845.FirstOrDefault(x=>x!=null&&string.Equals(x.CampaignId,campaignId,StringComparison.OrdinalIgnoreCase));
        }
    }
}
