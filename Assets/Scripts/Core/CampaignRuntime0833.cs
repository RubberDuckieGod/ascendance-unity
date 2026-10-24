using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    [Serializable]
    public sealed class WorldEventDefinition0833
    {
        public string Id; public string Name; public string Main1; public string Main2; public string Optional; public string Final; public string NextId; public int DayLimit;
    }

    [Serializable]
    public sealed class CampaignProgressState0833
    {
        // 0.8.45 — active campaign identity plus prior-campaign archive.
        public string CampaignId0845="C001";
        public bool AwaitingCampaignRuntime0845;
        public List<CampaignArchiveEntry0845> CampaignArchive0845=new();
        public string CurrentWorldEventId="WE001";
        public int WorldEventDay=1;
        public int TotalCampaignDay=1;
        public bool CampaignCompleted;
        public bool CampaignDefeated;
        public List<string> CompletedQuestIds=new();
        public List<string> FailedQuestIds=new();
        public List<string> MissedQuestIds=new();
        public List<string> CompletedWorldEventIds=new();
        public bool RemoveScoutFromQ004Setup;
        public bool AddRaiderToQ004FirstEncounter;
        public bool AddEliteToQ007Setup;
        public int WE003TravelSeverityPenalty;
        public int DamagedBuildings;
        public int Q010SiegePressureModifier;
        public bool Q010WoundPenalty;
        public bool StonebrookEquipmentPurchaseLocked;
        // 0.8.57 — persistent World Event pressure/meters and exact authored chapter state.
        public bool WorldEventStateInitialized0857;
        public int Suspicion0857;
        public int Instability0857;
        public int Blight0857;
        public int Doomtide0857;
        public int Sunfall0857;
        public List<string> WorldEventFlags0857=new();
        public List<string> WorldEventOnceKeys0857=new();
    }

    public static class CampaignContent0833
    {
        public static readonly IReadOnlyDictionary<string,WorldEventDefinition0833> WorldEvents = new Dictionary<string,WorldEventDefinition0833>(StringComparer.OrdinalIgnoreCase)
        {
            ["WE001"]=new WorldEventDefinition0833{Id="WE001",Name="Goblin Raids",Main1="Q001",Main2="Q002",Optional="Q003",Final=null,NextId="WE002",DayLimit=5},
            ["WE002"]=new WorldEventDefinition0833{Id="WE002",Name="Goblin Warbands",Main1="Q004",Main2="Q005",Optional="Q006",Final=null,NextId="WE003",DayLimit=6},
            ["WE003"]=new WorldEventDefinition0833{Id="WE003",Name="Siege of Stonebrook",Main1="Q007",Main2="Q008",Optional="Q009",Final=null,NextId="WE004",DayLimit=6},
            ["WE004"]=new WorldEventDefinition0833{Id="WE004",Name="The Goblin King",Main1="Q010",Main2="Q011",Optional=null,Final="Q012",NextId=null,DayLimit=0},
            ["WE005"]=new WorldEventDefinition0833{Id="WE005",Name="Knives in the Night",Main1="Q013",Main2="Q014",Optional="Q015",Final=null,NextId="WE006",DayLimit=5},
            ["WE006"]=new WorldEventDefinition0833{Id="WE006",Name="The Hidden Hand",Main1="Q016",Main2="Q018",Optional="Q017",Final=null,NextId="WE007",DayLimit=6},
            ["WE007"]=new WorldEventDefinition0833{Id="WE007",Name="Beneath Valewatch",Main1="Q019",Main2="Q021",Optional="Q020",Final=null,NextId="WE008",DayLimit=6},
            ["WE008"]=new WorldEventDefinition0833{Id="WE008",Name="The Veiled Sovereign",Main1="Q023",Main2=null,Optional="Q022",Final="Q024",NextId=null,DayLimit=0},
            ["WE009"]=new WorldEventDefinition0833{Id="WE009",Name="Shaking Foundations",Main1="Q025",Main2="Q027",Optional="Q026",Final=null,NextId="WE010",DayLimit=5},
            ["WE010"]=new WorldEventDefinition0833{Id="WE010",Name="The Broken Deeps",Main1="Q028",Main2="Q030",Optional="Q029",Final=null,NextId="WE011",DayLimit=6},
            ["WE011"]=new WorldEventDefinition0833{Id="WE011",Name="Furnace Below",Main1="Q031",Main2="Q033",Optional="Q032",Final=null,NextId="WE012",DayLimit=6},
            ["WE012"]=new WorldEventDefinition0833{Id="WE012",Name="Heart of the Mountain",Main1="Q035",Main2=null,Optional="Q034",Final="Q036",NextId=null,DayLimit=0},
            ["WE013"]=new WorldEventDefinition0833{Id="WE013",Name="The First Wilt",Main1="Q037",Main2="Q039",Optional="Q038",Final=null,NextId="WE014",DayLimit=5},
            ["WE014"]=new WorldEventDefinition0833{Id="WE014",Name="Roots of Rot",Main1="Q040",Main2="Q042",Optional="Q041",Final=null,NextId="WE015",DayLimit=6},
            ["WE015"]=new WorldEventDefinition0833{Id="WE015",Name="The Hollow Grove",Main1="Q043",Main2="Q045",Optional="Q044",Final=null,NextId="WE016",DayLimit=6},
            ["WE016"]=new WorldEventDefinition0833{Id="WE016",Name="The Withering Wilds",Main1="Q047",Main2=null,Optional="Q046",Final="Q048",NextId=null,DayLimit=0},
            ["WE017"]=new WorldEventDefinition0833{Id="WE017",Name="Black Tides",Main1="Q049",Main2="Q051",Optional="Q050",Final=null,NextId="WE018",DayLimit=5},
            ["WE018"]=new WorldEventDefinition0833{Id="WE018",Name="The Drowned Bells",Main1="Q052",Main2="Q054",Optional="Q053",Final=null,NextId="WE019",DayLimit=6},
            ["WE019"]=new WorldEventDefinition0833{Id="WE019",Name="Eye of the Tempest",Main1="Q055",Main2="Q057",Optional="Q056",Final=null,NextId="WE020",DayLimit=6},
            ["WE020"]=new WorldEventDefinition0833{Id="WE020",Name="The Drowned Tide",Main1="Q059",Main2=null,Optional="Q058",Final="Q060",NextId=null,DayLimit=0},
            ["WE021"]=new WorldEventDefinition0833{Id="WE021",Name="Ashes at Noon",Main1="Q061",Main2="Q063",Optional="Q062",Final=null,NextId="WE022",DayLimit=5},
            ["WE022"]=new WorldEventDefinition0833{Id="WE022",Name="The Buried Crown",Main1="Q064",Main2="Q066",Optional="Q065",Final=null,NextId="WE023",DayLimit=6},
            ["WE023"]=new WorldEventDefinition0833{Id="WE023",Name="Eclipse Rising",Main1="Q067",Main2="Q069",Optional="Q068",Final=null,NextId="WE024",DayLimit=6},
            ["WE024"]=new WorldEventDefinition0833{Id="WE024",Name="Crown of the Fallen Sun",Main1="Q071",Main2=null,Optional="Q070",Final="Q072",NextId=null,DayLimit=0}
        };
    }

    // 0.8.33 — World Event / Campaign I progression. Quest combat remains owned by 0.8.29–0.8.32;
    // this layer owns availability, Campaign Days, chapter transitions, chapter rewards/penalties, and persistence.
    public sealed partial class AscendanceGameManager
    {
        public CampaignProgressState0833 CampaignProgress0833 { get; private set; }
        public string CurrentWorldEventId0833 => CampaignProgress0833?.CurrentWorldEventId ?? "WE001";
        public string CurrentWorldEventName0833 => CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w)?w.Name:"Goblin Raids";
        public int WorldEventDay0833 => Mathf.Max(0,CampaignProgress0833?.WorldEventDay??0);
        public int TotalCampaignDay0833 => Mathf.Max(0,CampaignProgress0833?.TotalCampaignDay??0);
        public bool CampaignCompleted0833 => CampaignProgress0833?.CampaignCompleted??false;
        public bool CampaignDefeated0833 => CampaignProgress0833?.CampaignDefeated??false;
        public bool RemoveScoutFromQ004Setup0833 => CampaignProgress0833?.RemoveScoutFromQ004Setup??false;
        public bool AddRaiderToQ004FirstEncounter0833 => CampaignProgress0833?.AddRaiderToQ004FirstEncounter??false;
        public bool AddEliteToQ007Setup0833 => CampaignProgress0833?.AddEliteToQ007Setup??false;
        public int WE003TravelSeverityPenalty0833 => Mathf.Max(0,CampaignProgress0833?.WE003TravelSeverityPenalty??0);
        public int DamagedBuildings0833 => Mathf.Max(0,CampaignProgress0833?.DamagedBuildings??0);
        public int Q010SiegePressureModifier0833 => CampaignProgress0833?.Q010SiegePressureModifier??0;
        public bool Q010WoundPenalty0833 => CampaignProgress0833?.Q010WoundPenalty??false;
        public bool StonebrookEquipmentPurchaseLocked0833 => CampaignProgress0833?.StonebrookEquipmentPurchaseLocked??false;
        public IReadOnlyList<string> CompletedCampaignQuests0833 => CampaignProgress0833!=null?CampaignProgress0833.CompletedQuestIds:(IReadOnlyList<string>)Array.Empty<string>();
        public IReadOnlyList<string> FailedCampaignQuests0833 => CampaignProgress0833!=null?CampaignProgress0833.FailedQuestIds:(IReadOnlyList<string>)Array.Empty<string>();
        public IReadOnlyList<string> MissedCampaignQuests0833 => CampaignProgress0833!=null?CampaignProgress0833.MissedQuestIds:(IReadOnlyList<string>)Array.Empty<string>();

        private void InitializeCampaignProgress0833()
        {
            CampaignProgress0833=new CampaignProgressState0833{CampaignId0845=CampaignContent0845.CampaignI,CurrentWorldEventId="WE001",WorldEventDay=1,TotalCampaignDay=1};
            InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignI);
        }

        private static string WorldEventForQuest0833(string questId)
        {
            if(string.IsNullOrWhiteSpace(questId))return "WE001";
            var m=System.Text.RegularExpressions.Regex.Match(questId,@"^Q(\d{3})$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if(!m.Success||!int.TryParse(m.Groups[1].Value,out int q)||q<1||q>72)return "WE001";
            return $"WE{((q-1)/3)+1:000}";
        }

        private void RestoreCampaignProgress0833(AscendanceSaveData save)
        {
            var s=save?.CampaignProgress0833;
            if(s==null)
            {
                CampaignProgress0833=new CampaignProgressState0833{CampaignId0845=CampaignContent0845.CampaignForQuest0845(CurrentQuestId0829),CurrentWorldEventId=WorldEventForQuest0833(CurrentQuestId0829),WorldEventDay=1,TotalCampaignDay=1};
                if(save?.QuestComplete==true)CampaignProgress0833.CompletedQuestIds.Add(CurrentQuestId0829);
                if(save?.QuestFailed==true)CampaignProgress0833.FailedQuestIds.Add(CurrentQuestId0829);
                if(QuestRuntime0829?.CampaignVictory==true){CampaignProgress0833.CampaignCompleted=true;CampaignProgress0833.CompletedWorldEventIds.Add("WE004");}
                EnsureWorldEventCampaignDefaults0857();
                return;
            }
            string restoredCampaignId0845=CampaignContent0845.NormalizeCampaignId0845(s.CampaignId0845,s.CurrentWorldEventId);
            bool awaitingRuntime0845=s.AwaitingCampaignRuntime0845&&!CampaignContent0845.Definition0845(restoredCampaignId0845).RuntimePlayable;
            CampaignProgress0833=new CampaignProgressState0833
            {
                CampaignId0845=restoredCampaignId0845,
                AwaitingCampaignRuntime0845=awaitingRuntime0845,
                CampaignArchive0845=s.CampaignArchive0845?.Select(CampaignArchiveEntry0845.FromSave0845).Where(x=>x!=null).ToList()??new List<CampaignArchiveEntry0845>(),
                CurrentWorldEventId=CampaignContent0833.WorldEvents.ContainsKey(s.CurrentWorldEventId??string.Empty)?s.CurrentWorldEventId:WorldEventForQuest0833(CurrentQuestId0829),
                WorldEventDay=Mathf.Max(0,s.WorldEventDay),TotalCampaignDay=Mathf.Max(0,s.TotalCampaignDay),CampaignCompleted=s.CampaignCompleted,CampaignDefeated=s.CampaignDefeated,
                CompletedQuestIds=s.CompletedQuestIds?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()??new List<string>(),FailedQuestIds=s.FailedQuestIds?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()??new List<string>(),MissedQuestIds=s.MissedQuestIds?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()??new List<string>(),CompletedWorldEventIds=s.CompletedWorldEventIds?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()??new List<string>(),
                RemoveScoutFromQ004Setup=s.RemoveScoutFromQ004Setup,AddRaiderToQ004FirstEncounter=s.AddRaiderToQ004FirstEncounter,AddEliteToQ007Setup=s.AddEliteToQ007Setup,WE003TravelSeverityPenalty=Mathf.Max(0,s.WE003TravelSeverityPenalty),DamagedBuildings=Mathf.Max(0,s.DamagedBuildings),Q010SiegePressureModifier=s.Q010SiegePressureModifier,Q010WoundPenalty=s.Q010WoundPenalty,StonebrookEquipmentPurchaseLocked=s.StonebrookEquipmentPurchaseLocked,
                WorldEventStateInitialized0857=s.WorldEventStateInitialized0857,Suspicion0857=Mathf.Max(0,s.Suspicion0857),Instability0857=Mathf.Clamp(s.Instability0857,0,8),Blight0857=Mathf.Clamp(s.Blight0857,0,8),Doomtide0857=Mathf.Clamp(s.Doomtide0857,0,8),Sunfall0857=Mathf.Clamp(s.Sunfall0857,0,8),WorldEventFlags0857=s.WorldEventFlags0857?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()??new List<string>(),WorldEventOnceKeys0857=s.WorldEventOnceKeys0857?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()??new List<string>()
            };
            EnsureWorldEventCampaignDefaults0857();
        }

        public bool IsWorldEventActive0833(string id)=>string.Equals(CurrentWorldEventId0833,id,StringComparison.OrdinalIgnoreCase);
        public int WorldEventTravelSeverityModifier0833 => IsWorldEventActive0833("WE001")?1:IsWorldEventActive0833("WE003")?WE003TravelSeverityPenalty0833:0;
        public float EquipmentPriceMultiplier0833 => IsWorldEventActive0833("WE002")?1.25f:1f;
        public string MaxPurchasableEquipmentRarity0833 => IsWorldEventActive0833("WE001")?"Common":StonebrookEquipmentPurchaseLocked0833&&!IsWorldEventActive0833("WE004")?"None":"Any";
        public bool RandomTravelEncountersSuppressed0833 => IsWorldEventActive0833("WE004")||IsWorldEventActive0833("WE008");

        public void SyncCampaignProgress0833()
        {
            if(CampaignProgress0833==null)InitializeCampaignProgress0833();
            // A stale terminal Quest from the prior campaign must never leak into the newly active campaign.
            if(!CampaignContent0845.QuestBelongsToCampaign0845(CurrentQuestId0829,CurrentCampaignId0845))return;
            if(QuestComplete)RecordCampaignQuestCompleted0833(CurrentQuestId0829);
            else if(QuestFailed)RecordCampaignQuestFailed0833(CurrentQuestId0829);
        }

        private void RecordCampaignQuestCompleted0833(string questId)
        {
            if(CampaignProgress0833==null)InitializeCampaignProgress0833();
            if(string.IsNullOrEmpty(questId))return;
            bool added=!CampaignProgress0833.CompletedQuestIds.Contains(questId,StringComparer.OrdinalIgnoreCase);
            if(added)CampaignProgress0833.CompletedQuestIds.Add(questId);
            CampaignProgress0833.FailedQuestIds.RemoveAll(x=>string.Equals(x,questId,StringComparison.OrdinalIgnoreCase));
            CampaignProgress0833.MissedQuestIds.RemoveAll(x=>string.Equals(x,questId,StringComparison.OrdinalIgnoreCase));
            if(added)AddLog($"CAMPAIGN — {questId} recorded Completed during {CurrentWorldEventId0833} {CurrentWorldEventName0833}.");
            if(string.Equals(CurrentCampaignDefinition0845.LastQuestId,questId,StringComparison.OrdinalIgnoreCase))
            {
                bool enteredVictory=!CampaignProgress0833.CampaignCompleted||CampaignProgress0833.CampaignDefeated;CampaignProgress0833.CampaignCompleted=true;CampaignProgress0833.CampaignDefeated=false;
                if(!CampaignProgress0833.CompletedWorldEventIds.Contains(CurrentCampaignDefinition0845.LastWorldEventId,StringComparer.OrdinalIgnoreCase))CampaignProgress0833.CompletedWorldEventIds.Add(CurrentCampaignDefinition0845.LastWorldEventId);
                ResolveFinalWorldEvent0857(true);
                if(enteredVictory)AddLog($"CAMPAIGN {CurrentCampaignDefinition0845.Number} — Victory recorded. {CurrentCampaignDefinition0845.LastWorldEventId} complete.");
            }
        }

        private void RecordCampaignQuestFailed0833(string questId)
        {
            if(CampaignProgress0833==null)InitializeCampaignProgress0833();
            if(string.IsNullOrEmpty(questId)||CampaignProgress0833.CompletedQuestIds.Contains(questId,StringComparer.OrdinalIgnoreCase))return;
            if(!CampaignProgress0833.FailedQuestIds.Contains(questId,StringComparer.OrdinalIgnoreCase))
            {
                CampaignProgress0833.FailedQuestIds.Add(questId);AddLog($"CAMPAIGN — {questId} recorded Failed. It may be retried while {CurrentWorldEventId0833} remains active.");
            }
            if(string.Equals(CurrentCampaignDefinition0845.LastQuestId,questId,StringComparison.OrdinalIgnoreCase))
            {
                bool enteredDefeat=!CampaignProgress0833.CampaignDefeated||CampaignProgress0833.CampaignCompleted;CampaignProgress0833.CampaignDefeated=true;CampaignProgress0833.CampaignCompleted=false;
                ResolveFinalWorldEvent0857(false);
                if(enteredDefeat)AddLog($"CAMPAIGN {CurrentCampaignDefinition0845.Number} DEFEAT — {questId} failed; {CurrentCampaignDefinition0845.Title} is over.");
            }
        }

        private bool CampaignQuestResolved0833=>QuestComplete||QuestFailed;
        private bool Completed0833(string id)=>CampaignProgress0833?.CompletedQuestIds?.Contains(id,StringComparer.OrdinalIgnoreCase)??false;
        private bool Failed0833(string id)=>CampaignProgress0833?.FailedQuestIds?.Contains(id,StringComparer.OrdinalIgnoreCase)??false;

        private string MainQuestAvailable0833()
        {
            if(CampaignProgress0833==null||CampaignCompleted0833||CampaignDefeated0833||!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w))return null;
            foreach(var id in new[]{w.Main1,w.Main2,w.Final})if(!string.IsNullOrEmpty(id)&&!Completed0833(id))return id;
            return null;
        }

        public string RecommendedMainCampaignQuest0833 => MainQuestAvailable0833();
        public string OptionalCampaignQuest0833
        {
            get
            {
                if(CampaignProgress0833==null||CampaignCompleted0833||CampaignDefeated0833||!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w))return null;
                string id=w.Optional;return !string.IsNullOrEmpty(id)&&!Completed0833(id)&&!CampaignProgress0833.MissedQuestIds.Contains(id,StringComparer.OrdinalIgnoreCase)?id:null;
            }
        }

        public IReadOnlyList<string> AvailableCampaignQuests0833
        {
            get
            {
                SyncCampaignProgress0833();
                var result=new List<string>();if(CampaignProgress0833?.AwaitingCampaignRuntime0845==true||!CampaignQuestResolved0833||CampaignCompleted0833||CampaignDefeated0833)return result;
                string main=RecommendedMainCampaignQuest0833,optional=OptionalCampaignQuest0833;
                if(!string.IsNullOrEmpty(main)&&!string.Equals(main,CurrentQuestId0829,StringComparison.OrdinalIgnoreCase))result.Add(main);
                else if(!string.IsNullOrEmpty(main)&&QuestFailed)result.Add(main);
                if(!string.IsNullOrEmpty(optional)&&!string.Equals(optional,CurrentQuestId0829,StringComparison.OrdinalIgnoreCase))result.Add(optional);
                else if(!string.IsNullOrEmpty(optional)&&QuestFailed)result.Add(optional);
                return result;
            }
        }

        public bool CanBeginCampaignQuest0833(string questId)
        {
            if(string.IsNullOrEmpty(questId)||CampaignProgress0833?.AwaitingCampaignRuntime0845==true||!QuestContent0829.Definitions.ContainsKey(questId)||IsResolving||HasPendingReactionWindow||HasPendingEncounterDeckControl||HasPendingRewardChoice||!CampaignQuestResolved0833)return false;
            return AvailableCampaignQuests0833.Contains(questId,StringComparer.OrdinalIgnoreCase);
        }

        public bool BeginCampaignQuest0833(string questId)
        {
            SyncCampaignProgress0833();if(!QuestContent0829.Definitions.ContainsKey(questId??string.Empty)||!CanBeginCampaignQuest0833(questId))return false;
            CampaignProgress0833.WorldEventDay++;CampaignProgress0833.TotalCampaignDay++;
            CampaignProgress0833.FailedQuestIds.RemoveAll(x=>string.Equals(x,questId,StringComparison.OrdinalIgnoreCase));
            AddLog($"CAMPAIGN DAY — Day {CampaignProgress0833.WorldEventDay} of {CurrentWorldEventId0833} begins with {questId}. Total Campaign Day {CampaignProgress0833.TotalCampaignDay}.");
            StartQuest0829(questId);ApplyCampaignQuestStartEffects0833(questId);ClearQuestBriefing0841(false);StateChanged?.Invoke();return true;
        }

        public bool BeginRecommendedCampaignQuest0833()=>!string.IsNullOrEmpty(RecommendedMainCampaignQuest0833)&&BeginCampaignQuest0833(RecommendedMainCampaignQuest0833);
        public bool BeginOptionalCampaignQuest0833()=>!string.IsNullOrEmpty(OptionalCampaignQuest0833)&&BeginCampaignQuest0833(OptionalCampaignQuest0833);

        public bool CanAdvanceWorldEvent0833
        {
            get
            {
                SyncCampaignProgress0833();if(CampaignProgress0833==null||CampaignProgress0833.AwaitingCampaignRuntime0845||!CampaignQuestResolved0833||CampaignCompleted0833||CampaignDefeated0833)return false;
                if(!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w)||string.IsNullOrEmpty(w.NextId))return false;
                bool required=Completed0833(w.Main1)&&Completed0833(w.Main2);return required||(w.DayLimit>0&&CampaignProgress0833.WorldEventDay>=w.DayLimit);
            }
        }

        public bool AdvanceWorldEvent0833()
        {
            SyncCampaignProgress0833();if(!CanAdvanceWorldEvent0833||!CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w)||string.IsNullOrEmpty(w.NextId))return false;
            if(!ApplyWorldEventResolution0857(w))ApplyWorldEventResolution0833(w);
            foreach(var q in new[]{w.Main1,w.Main2,w.Optional,w.Final}.Where(x=>!string.IsNullOrEmpty(x)))
                if(!Completed0833(q)&&!CampaignProgress0833.MissedQuestIds.Contains(q,StringComparer.OrdinalIgnoreCase))CampaignProgress0833.MissedQuestIds.Add(q);
            if(!CampaignProgress0833.CompletedWorldEventIds.Contains(w.Id,StringComparer.OrdinalIgnoreCase))CampaignProgress0833.CompletedWorldEventIds.Add(w.Id);
            string old=w.Id;CampaignProgress0833.CurrentWorldEventId=w.NextId;CampaignProgress0833.WorldEventDay=0;
            AddLog($"WORLD EVENT ADVANCE — {old} {w.Name} → {w.NextId} {CampaignContent0833.WorldEvents[w.NextId].Name}.");StateChanged?.Invoke();return true;
        }

        private void ApplyWorldEventResolution0833(WorldEventDefinition0833 w)
        {
            int complete=new[]{w.Main1,w.Main2,w.Optional}.Count(Completed0833);int required=new[]{w.Main1,w.Main2}.Count(Completed0833);var receiver=SelectedHero??ParticipatingHeroes.FirstOrDefault();
            if(w.Id=="WE001")
            {
                if(complete==3){foreach(var h in ParticipatingHeroes)h.XP+=50;PartyMorale0831+=1;CampaignProgress0833.RemoveScoutFromQ004Setup=true;AddLog("WE001 COMPLETE — all 3 Quests: +50 XP each • +1 Party Morale • remove 1 Goblin Scout from Q004.");}
                else if(required<2){CampaignProgress0833.AddRaiderToQ004FirstEncounter=true;AddGeneralReputation(-1,"WE001 incomplete chapter penalty");AddLog("WE001 PENALTY — fewer than two required Quests completed: +1 Raider in Q004 first patrol • -1 General Reputation.");}
                else AddLog("WE001 COMPLETE — both required Quests completed; no additional chapter effect.");
                return;
            }
            if(w.Id=="WE002")
            {
                if(complete==3){foreach(var h in ParticipatingHeroes)h.XP+=75;if(receiver!=null)DrawRareEquipment0830(receiver,"WE002 all-quests reward");RemoveEliteFromQ007Setup0830=true;AddGeneralReputation(1,"WE002 all-quests reward");AddLog("WE002 COMPLETE — all 3 Quests: +75 XP each • 1 Rare Equipment • remove 1 Elite from Q007 • +1 Reputation.");}
                else if(required<2){CampaignProgress0833.AddEliteToQ007Setup=true;CampaignProgress0833.WE003TravelSeverityPenalty=1;AddGeneralReputation(-1,"WE002 incomplete chapter penalty");AddLog("WE002 PENALTY — fewer than two required Quests: +1 Elite in Q007 • WE003 Travel Encounter rolls +1 • -1 Reputation.");}
                else AddLog("WE002 COMPLETE — both required Quests completed; no additional chapter effect.");
                return;
            }
            if(w.Id=="WE003")
            {
                if(complete==3){foreach(var h in ParticipatingHeroes)h.XP+=100;if(receiver!=null)DrawEpicEquipment0831(receiver,"WE003 all-quests reward");AddGeneralReputation(2,"WE003 all-quests reward");CampaignProgress0833.Q010SiegePressureModifier=-1;CampaignProgress0833.DamagedBuildings=0;AddLog("WE003 COMPLETE — all 3 Quests: +100 XP each • 1 Epic Equipment • +2 Reputation • Q010 Siege Pressure -1 banked • all Damaged Buildings restored.");}
                else if(required>=2){CampaignProgress0833.DamagedBuildings=Mathf.Max(1,CampaignProgress0833.DamagedBuildings);AddLog("WE003 COMPLETE — required Quests completed: WE004 begins with 1 Damaged Building.");}
                else {CampaignProgress0833.Q010SiegePressureModifier=1;CampaignProgress0833.Q010WoundPenalty=true;CampaignProgress0833.StonebrookEquipmentPurchaseLocked=true;AddGeneralReputation(-2,"WE003 incomplete chapter penalty");AddLog("WE003 PENALTY — fewer than two required Quests: Q010 Siege Pressure +1 banked • each Hero begins Q010 with 1 Wound Token • -2 Reputation • Stonebrook Equipment purchasing locked before WE004.");}
            }
        }

        private void ApplyCampaignQuestStartEffects0833(string questId)
        {
            if(CampaignProgress0833==null)return;
            if(questId=="Q010")
            {
                foreach(var h in ParticipatingHeroes)h.Statuses.RemoveAll(s=>string.Equals(s.Name,"Wound Token",StringComparison.OrdinalIgnoreCase));
                if(CampaignProgress0833.Q010WoundPenalty)
                    foreach(var h in ParticipatingHeroes)h.Statuses.Add(new StatusInstance{Id="WE003_WOUND",Name="Wound Token",Source="WE003 incomplete chapter penalty",Beneficial=false,ExpiryTiming=StatusExpiryTiming.Persistent});
                if(CampaignProgress0833.Q010SiegePressureModifier!=0)
                    AddLog($"Q010 CAMPAIGN PRESSURE — WE003 recorded Siege Pressure {CampaignProgress0833.Q010SiegePressureModifier:+#;-#;0}. Q010's two Ballistas remain print-locked; the unresolved Siege Engine adjustment is preserved in campaign state rather than rewriting the locked objective.");
            }
            if(IsWorldEventActive0833("WE003")&&PartyMorale0831>0)AddLog($"CAMPAIGN MORALE — party begins {questId} during WE003 with Party Morale {PartyMorale0831}.");
        }

        public string CampaignSummary0833
        {
            get
            {
                SyncCampaignProgress0833();if(CampaignProgress0833==null)return string.Empty;
                string day=CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833,out var w)&&w.DayLimit>0?$"DAY {WorldEventDay0833}/{w.DayLimit}":$"DAY {WorldEventDay0833}";
                string available=AvailableCampaignQuests0833.Count==0?"None":string.Join(", ",AvailableCampaignQuests0833);
                string state=CampaignCompleted0833?"CAMPAIGN VICTORY":CampaignDefeated0833?"CAMPAIGN DEFEAT":CanAdvanceWorldEvent0833?"WORLD EVENT ADVANCE AVAILABLE":"Campaign active";
                return $"{CurrentWorldEventId0833} — {CurrentWorldEventName0833} • {day}\nAvailable: {available}\nCompleted: {CampaignProgress0833.CompletedQuestIds.Count}/12 • Missed: {CampaignProgress0833.MissedQuestIds.Count}\n{state}";
            }
        }

        public void AdvanceCampaignDay0833(string source="Downtime")
        {
            if(CampaignProgress0833==null||CampaignProgress0833.AwaitingCampaignRuntime0845||CampaignCompleted0833||CampaignDefeated0833)return;CampaignProgress0833.WorldEventDay++;CampaignProgress0833.TotalCampaignDay++;AddLog($"CAMPAIGN DAY — {source}: {CurrentWorldEventId0833} Day {CampaignProgress0833.WorldEventDay}; Total Day {CampaignProgress0833.TotalCampaignDay}.");StateChanged?.Invoke();
        }

        private void DebugAlignCampaignWorldEventForQuest0833(string questId)
        {
            if(CampaignProgress0833==null)InitializeCampaignProgress0833();CampaignProgress0833.CurrentWorldEventId=WorldEventForQuest0833(questId);CampaignProgress0833.CampaignId0845=CampaignContent0845.CampaignForQuest0845(questId);CampaignProgress0833.CampaignCompleted=false;CampaignProgress0833.CampaignDefeated=false;CampaignProgress0833.AwaitingCampaignRuntime0845=false;
        }

        // Deterministic Campaign-layer validator helpers. They never call DebugStartQuest0829; campaign starts still go through BeginCampaignQuest0833.
        public void DebugResetCampaignProgress0833()
        {
            InitializeCampaignProgress0833();
            // Reset older cross-Quest campaign flags too so each deterministic route is a genuinely fresh Campaign I.
            ReduceFirstWE002TravelScout0830=false;RemoveEliteFromQ007Setup0830=false;PartyMorale0831=0;RemoveSiegeEngineFromQ008Setup0831=false;CaptainRolandAssistsQ0100832=false;
            StartQuest0829("Q001");CampaignProgress0833.WorldEventDay=1;CampaignProgress0833.TotalCampaignDay=1;StateChanged?.Invoke();
        }
        public void DebugResolveCampaignQuest0833(bool success)
        {
            if(success)
            {
                QuestComplete=true;QuestFailed=false;
                if(CurrentQuestId0829=="Q012"&&QuestRuntime0829!=null){QuestRuntime0829.CampaignVictory=true;QuestRuntime0829.DefenderOfStonebrookUnlocked=true;}
                // Use the real reward chain so authored cross-Quest consequences (Q005→Q007, Q007→Q008, Q009→Q010, etc.) are exercised by the campaign validator.
                ResolveQuestReward0829();
            }
            else {QuestComplete=false;QuestFailed=true;RecordCampaignQuestFailed0833(CurrentQuestId0829);}
            StateChanged?.Invoke();
        }
        public void DebugAdvanceCampaignDay0833(int count){for(int i=0;i<Mathf.Max(0,count);i++)AdvanceCampaignDay0833("0.8.33 validator");}
        public bool DebugCampaignCompletedQuest0833(string id)=>Completed0833(id);
        public bool DebugCampaignFailedQuest0833(string id)=>Failed0833(id);
        public bool DebugCampaignMissedQuest0833(string id)=>CampaignProgress0833?.MissedQuestIds?.Contains(id,StringComparer.OrdinalIgnoreCase)??false;
    }
}
