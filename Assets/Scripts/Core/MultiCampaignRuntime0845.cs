using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    [Serializable]
    public sealed class CampaignDefinition0845
    {
        public string Id; public int Number; public string Title; public string Region; public string Hub;
        public string FirstWorldEventId; public string LastWorldEventId; public string FirstQuestId; public string LastQuestId;
        public string ContinuationCampaignId; public string EntryBand; public string CorePremise; public bool RuntimePlayable;
    }

    [Serializable]
    public sealed class CampaignQuestPlan0845
    {
        public string QuestId; public string Name; public string WorldEventId; public string Role;
    }

    [Serializable]
    public sealed class CampaignArchiveEntry0845
    {
        public string CampaignId; public string CampaignTitle; public string Outcome; public string TitleUnlock; public int CampaignDays;
        public List<string> CompletedQuestIds=new(); public List<string> FailedQuestIds=new(); public List<string> MissedQuestIds=new();
        public CampaignArchiveSaveData0845 ToSave0845()=>new CampaignArchiveSaveData0845{CampaignId=CampaignId,CampaignTitle=CampaignTitle,Outcome=Outcome,TitleUnlock=TitleUnlock,CampaignDays=CampaignDays,CompletedQuestIds=CompletedQuestIds.ToList(),FailedQuestIds=FailedQuestIds.ToList(),MissedQuestIds=MissedQuestIds.ToList()};
        public static CampaignArchiveEntry0845 FromSave0845(CampaignArchiveSaveData0845 s)=>s==null?null:new CampaignArchiveEntry0845{CampaignId=s.CampaignId,CampaignTitle=s.CampaignTitle,Outcome=s.Outcome,TitleUnlock=s.TitleUnlock,CampaignDays=s.CampaignDays,CompletedQuestIds=s.CompletedQuestIds?.ToList()??new List<string>(),FailedQuestIds=s.FailedQuestIds?.ToList()??new List<string>(),MissedQuestIds=s.MissedQuestIds?.ToList()??new List<string>()};
    }

    public static class CampaignContent0845
    {
        public const string CampaignI="C001";
        public const string CampaignII="C002";
        public const string CampaignIII="C003"; public const string CampaignIV="C004"; public const string CampaignV="C005"; public const string CampaignVI="C006";
        public static readonly IReadOnlyDictionary<string,CampaignDefinition0845> Campaigns=new Dictionary<string,CampaignDefinition0845>(StringComparer.OrdinalIgnoreCase)
        {
            [CampaignI]=new CampaignDefinition0845{Id=CampaignI,Number=1,Title="The Goblin War",Region="Stonebrook Marches",Hub="Stonebrook",FirstWorldEventId="WE001",LastWorldEventId="WE004",FirstQuestId="Q001",LastQuestId="Q012",ContinuationCampaignId=CampaignII,EntryBand="Campaign start",CorePremise="Defend Stonebrook from the Goblin War and defeat the Goblin King.",RuntimePlayable=true},
            [CampaignII]=new CampaignDefinition0845{Id=CampaignII,Number=2,Title="Shadows Over Valewatch",Region="Crownlands",Hub="Valewatch",FirstWorldEventId="WE005",LastWorldEventId="WE008",FirstQuestId="Q013",LastQuestId="Q024",ContinuationCampaignId=CampaignIII,EntryBand="Level 5–6 continuation • standalone Level 5 / 1400 XP",CorePremise="Valewatch was built over a deliberately buried older city. The Quiet Crown seeks the Veiled Crown, an intelligent relic that consumes its wearer and creates a new Veiled Sovereign.",RuntimePlayable=true},
            [CampaignIII]=new CampaignDefinition0845{Id=CampaignIII,Number=3,Title="Heart of the Mountain",Region="Ironspine Mountains",Hub="Ironhaven",FirstWorldEventId="WE009",LastWorldEventId="WE012",FirstQuestId="Q025",LastQuestId="Q036",ContinuationCampaignId=CampaignIV,EntryBand="Campaign III continuation",CorePremise="Descend beneath Ironhaven as the mountain fractures and the ancient Worldforge wakes.",RuntimePlayable=true},
            [CampaignIV]=new CampaignDefinition0845{Id=CampaignIV,Number=4,Title="The Withering Wilds",Region="Verdant Reach",Hub="Eldergrove",FirstWorldEventId="WE013",LastWorldEventId="WE016",FirstQuestId="Q037",LastQuestId="Q048",ContinuationCampaignId=CampaignV,EntryBand="Campaign IV continuation",CorePremise="Trace the spreading blight through Eldergrove and confront the corruption at the heart of the wilds.",RuntimePlayable=true},
            [CampaignV]=new CampaignDefinition0845{Id=CampaignV,Number=5,Title="The Drowned Tide",Region="Storm Coast",Hub="Ashenport",FirstWorldEventId="WE017",LastWorldEventId="WE020",FirstQuestId="Q049",LastQuestId="Q060",ContinuationCampaignId=CampaignVI,EntryBand="Campaign V continuation",CorePremise="Defend Ashenport as black tides, drowned bells, and the Maelstrom pull the Storm Coast toward ruin.",RuntimePlayable=true},
            [CampaignVI]=new CampaignDefinition0845{Id=CampaignVI,Number=6,Title="Crown of the Fallen Sun",Region="Sunscar Expanse",Hub="Sunspire",FirstWorldEventId="WE021",LastWorldEventId="WE024",FirstQuestId="Q061",LastQuestId="Q072",ContinuationCampaignId=null,EntryBand="Campaign VI continuation",CorePremise="Cross the Sunscar Expanse beneath an advancing eclipse and confront the Crown of the Fallen Sun at Sunspire.",RuntimePlayable=true}
        };

        public static readonly IReadOnlyDictionary<string,CampaignQuestPlan0845> ValewatchQuestPlan=new Dictionary<string,CampaignQuestPlan0845>(StringComparer.OrdinalIgnoreCase)
        {
            ["Q013"]=new CampaignQuestPlan0845{QuestId="Q013",Name="The Missing Caravan",WorldEventId="WE005",Role="Main"},
            ["Q014"]=new CampaignQuestPlan0845{QuestId="Q014",Name="Blood in the Alleys",WorldEventId="WE005",Role="Main"},
            ["Q015"]=new CampaignQuestPlan0845{QuestId="Q015",Name="The Silent Witness",WorldEventId="WE005",Role="Optional"},
            ["Q016"]=new CampaignQuestPlan0845{QuestId="Q016",Name="Masks at Midnight",WorldEventId="WE006",Role="Main"},
            ["Q017"]=new CampaignQuestPlan0845{QuestId="Q017",Name="Beneath the Guildhall",WorldEventId="WE006",Role="Optional"},
            ["Q018"]=new CampaignQuestPlan0845{QuestId="Q018",Name="The Broken Seal",WorldEventId="WE006",Role="Main"},
            ["Q019"]=new CampaignQuestPlan0845{QuestId="Q019",Name="The Forgotten City",WorldEventId="WE007",Role="Main"},
            ["Q020"]=new CampaignQuestPlan0845{QuestId="Q020",Name="Tomb of the First King",WorldEventId="WE007",Role="Optional"},
            ["Q021"]=new CampaignQuestPlan0845{QuestId="Q021",Name="The Black Cathedral",WorldEventId="WE007",Role="Main"},
            ["Q022"]=new CampaignQuestPlan0845{QuestId="Q022",Name="The City Falls",WorldEventId="WE008",Role="Optional"},
            ["Q023"]=new CampaignQuestPlan0845{QuestId="Q023",Name="The Descent",WorldEventId="WE008",Role="Main"},
            ["Q024"]=new CampaignQuestPlan0845{QuestId="Q024",Name="The Veiled Sovereign",WorldEventId="WE008",Role="Final Quest"}
        };

        public static string CampaignForQuest0845(string questId)
        {
            if(string.IsNullOrWhiteSpace(questId))return CampaignI;
            var m=System.Text.RegularExpressions.Regex.Match(questId,@"^Q(\d{3})$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if(!m.Success||!int.TryParse(m.Groups[1].Value,out int q)||q<1||q>72)return CampaignI;
            return $"C{((q-1)/12)+1:000}";
        }
        public static bool QuestBelongsToCampaign0845(string questId,string campaignId)=>string.Equals(CampaignForQuest0845(questId),NormalizeCampaignId0845(campaignId,null),StringComparison.OrdinalIgnoreCase);
        public static string NormalizeCampaignId0845(string campaignId,string worldEventId)
        {
            if(!string.IsNullOrWhiteSpace(campaignId)&&Campaigns.ContainsKey(campaignId))return campaignId;
            var m=System.Text.RegularExpressions.Regex.Match(worldEventId??string.Empty,@"^WE(\d{3})$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if(m.Success&&int.TryParse(m.Groups[1].Value,out int we)&&we>=1&&we<=24)return $"C{((we-1)/4)+1:000}";
            return CampaignI;
        }
        public static bool IsValewatchWorldEvent0845(string id)=>new[]{"WE005","WE006","WE007","WE008"}.Contains(id??string.Empty,StringComparer.OrdinalIgnoreCase);
        public static CampaignDefinition0845 Definition0845(string campaignId)=>Campaigns[NormalizeCampaignId0845(campaignId,null)];
    }

    // 0.8.56 extends the 0.8.45 campaign identity/handoff layer through all six authored campaigns; Q013-Q072 execution is owned by QuestRuntime0856.
    public sealed partial class AscendanceGameManager
    {
        public string CurrentCampaignId0845 => CampaignContent0845.NormalizeCampaignId0845(CampaignProgress0833?.CampaignId0845,CurrentWorldEventId0833);
        public CampaignDefinition0845 CurrentCampaignDefinition0845 => CampaignContent0845.Definition0845(CurrentCampaignId0845);
        public string CurrentCampaignTitle0845 => CurrentCampaignDefinition0845.Title;
        public string CurrentCampaignHub0845 => CurrentCampaignDefinition0845.Hub;
        public string CurrentCampaignRegion0845 => CurrentCampaignDefinition0845.Region;
        public bool CampaignLaunchpadAvailable0845 => CurrentCampaignId0845==CampaignContent0845.CampaignII && !CampaignContent0845.QuestBelongsToCampaign0845(CurrentQuestId0829,CurrentCampaignId0845) && !CampaignCompleted0833 && !CampaignDefeated0833;
        public bool CanContinueToValewatch0845 => CurrentCampaignId0845==CampaignContent0845.CampaignI && CampaignCompleted0833;
        public IReadOnlyList<CampaignArchiveEntry0845> CampaignArchive0845 => CampaignProgress0833!=null ? CampaignProgress0833.CampaignArchive0845 : (IReadOnlyList<CampaignArchiveEntry0845>)Array.Empty<CampaignArchiveEntry0845>();

        public bool BeginValewatchContinuation0845()
        {
            if(!CanContinueToValewatch0845||CampaignProgress0833==null)return false;
            if(!CampaignProgress0833.CampaignArchive0845.Any(x=>x!=null&&string.Equals(x.CampaignId,CampaignContent0845.CampaignI,StringComparison.OrdinalIgnoreCase)))
            {
                var entry=BuildCampaignArchiveEntry0861(CampaignContent0845.Campaigns[CampaignContent0845.CampaignI]);
                if(entry!=null)CampaignProgress0833.CampaignArchive0845.Add(entry);
            }
            var archive=CampaignProgress0833.CampaignArchive0845;
            CampaignProgress0833=CreateFreshContinuationCampaign0861(CampaignContent0845.Campaigns[CampaignContent0845.CampaignII],archive);
            InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignII);
            ClearQuestBriefing0841(false);
            AddLog("CAMPAIGN CONTINUATION — Campaign I archived as Victory with Defender of Stonebrook. The surviving party arrives in Valewatch for Campaign II: Shadows Over Valewatch.");
            AddLog("CAMPAIGN II READY — WE005 Knives in the Night is active. Q013 The Missing Caravan is available from the Valewatch campaign screen.");
            StateChanged?.Invoke();
            return true;
        }

        public bool CanContinueToNextCampaign0856 => CampaignCompleted0833&&!string.IsNullOrWhiteSpace(CurrentCampaignDefinition0845.ContinuationCampaignId);
        public bool BeginNextCampaign0856()
        {
            if(!CanContinueToNextCampaign0856||CampaignProgress0833==null)return false;
            var old=CurrentCampaignDefinition0845;var next=CampaignContent0845.Campaigns[old.ContinuationCampaignId];
            if(!CampaignProgress0833.CampaignArchive0845.Any(x=>x!=null&&string.Equals(x.CampaignId,old.Id,StringComparison.OrdinalIgnoreCase)))
            {
                var entry=BuildCampaignArchiveEntry0861(old);
                if(entry!=null)CampaignProgress0833.CampaignArchive0845.Add(entry);
            }
            var archive=CampaignProgress0833.CampaignArchive0845;CampaignProgress0833=CreateFreshContinuationCampaign0861(next,archive);
            InitializeWorldEventCampaignState0857(next.Id);
            ClearQuestBriefing0841(false);AddLog($"CAMPAIGN CONTINUATION — {old.Title} archived. Campaign {next.Number}: {next.Title} begins in {next.Region}; hub {next.Hub}. {next.FirstQuestId} is now available.");StateChanged?.Invoke();return true;
        }

        public string ValewatchLaunchpadOverview0845 =>
            $"CAMPAIGN II — SHADOWS OVER VALEWATCH\n{CurrentCampaignRegion0845} • Hub: {CurrentCampaignHub0845}\n\n{CampaignContent0845.Campaigns[CampaignContent0845.CampaignII].CorePremise}\n\nENTRY: {CampaignContent0845.Campaigns[CampaignContent0845.CampaignII].EntryBand}";

        public string ValewatchChapterPlan0845
        {
            get
            {
                string Row(string we,string q1,string q2,string opt)=>$"{we} — {CampaignContent0833.WorldEvents[we].Name}\n  MAIN: {q1} {CampaignContent0845.ValewatchQuestPlan[q1].Name}\n  MAIN: {q2} {CampaignContent0845.ValewatchQuestPlan[q2].Name}\n  OPTIONAL: {opt} {CampaignContent0845.ValewatchQuestPlan[opt].Name}";
                return string.Join("\n\n",new[]{Row("WE005","Q013","Q014","Q015"),Row("WE006","Q016","Q018","Q017"),Row("WE007","Q019","Q021","Q020"),Row("WE008","Q023","Q024","Q022")});
            }
        }

        public string ValewatchCarryoverSummary0845
        {
            get
            {
                var heroes=ParticipatingHeroes.OrderBy(h=>h.PartyIndex).Select(h=>$"{h.DisplayName} • Lv {h.Level} • XP {h.XP} • Kills {h.KillCount} • Gear {h.StartingEquipment.Count+h.AcquiredEquipment.Count} • Loot {h.LootInventory.Count}");
                string unlock=CampaignArchive0845.FirstOrDefault(x=>x?.CampaignId==CampaignContent0845.CampaignI)?.TitleUnlock??"None";
                return $"PERSISTENT CONTINUATION\n{string.Join("\n",heroes)}\n\nParty Gold {PartyGold} • General Reputation {GeneralReputation}\nCampaign I unlock: {unlock}";
            }
        }
    }
}
