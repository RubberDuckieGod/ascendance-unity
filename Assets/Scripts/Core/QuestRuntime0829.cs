using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    [Serializable]
    public sealed class QuestDefinition0829
    {
        public string Id; public string Name; public string Story; public string Victory; public string Failure; public string Reward;
    }

    [Serializable]
    public sealed class QuestObjectState0829
    {
        public string ObjectId; public string QuestId; public string Type; public string DisplayName; public string LocationId; public string OwnerHeroId;
        public int Supply; public int Health; public int MaxHealth; public int Armor; public int Attack; public int Range; public bool Rescued; public bool Destroyed; public bool Resolved;
    }

    [Serializable]
    public sealed class QuestRuntimeState0829
    {
        public string QuestId="Q001"; public string QuestName="Defend Stonebrook"; public bool RewardResolved;
        public bool CaravanRevealed; public int ReinforcementCount; public bool CaptainSpawned; public bool CaptainDefeated;
        public bool ManifestGranted; public bool StrongboxKeyGranted; public bool StrongboxOpened; public int DeliveredSupply;
        // 0.8.30 generalized objective fields used by Q003–Q005.
        public bool ObjectiveLocationRevealed; public bool BeaconLit; public int ObjectiveEmptyRounds; public bool VaultKeyGranted; public bool VaultOpened;
        public bool InitialPatrolSpawned; public int TrailMarkers; public bool WarCampRevealed; public bool WarChiefDefeated; public int AlarmTokens; public int SupplyCratesDestroyed;
        // 0.8.31 objective state for Q006-Q008.
        public int PrisonersFreed; public int PrisonersRescued; public int PrisonersExecuted; public bool PrisonAlarmSpawned; public bool PrisonReinforcementSpawned;
        public int SupplyWagonsDestroyed; public int SupplyWagonsEscaped; public bool ConvoyFirstDestroyedWave; public bool ConvoySecondDestroyedWave;
        public int WallTrack; public bool Wall4Triggered; public bool Wall2Triggered; public int SiegeEnginesPlaced; public int SiegeEnginesDestroyed; public bool ChampionSpawned; public bool ChampionDefeated;
        // 0.8.32 objective state for Q009-Q012.
        public bool RolandRevealed; public bool RolandFreed; public bool RolandRescued; public bool RolandDefeated; public int ExecutionTrack;
        public bool FortressGateRevealed; public int GateHealth; public int GateArmor; public bool GateReinforcementTriggered; public bool GateDestroyed; public int BallistasDestroyed; public bool BallistaRepairPending; public bool BallistaRepairUsed; public string PendingBallistaObjectId; public int GateEmptyRounds;
        public int StrongholdQualifyingReveals; public int ReinforcementTokensRemaining; public bool HighShamanWaiting; public bool ThroneRoomRevealed;
        public int BannersDestroyed; public bool GoblinKingLastStandTriggered; public int GoblinKingHordeRound=-1; public bool CampaignVictory; public bool DefenderOfStonebrookUnlocked;
        public List<string> QuestOwnedEnemyIds=new(); public List<QuestObjectState0829> Objects=new();
    }

    public static class QuestContent0829
    {
        public static readonly Dictionary<string,QuestDefinition0829> Definitions = new Dictionary<string,QuestDefinition0829>(StringComparer.OrdinalIgnoreCase)
        {
            ["Q001"]=new QuestDefinition0829{Id="Q001",Name="Defend Stonebrook",Story="Stonebrook comes under attack. Rescue villagers, contain the fires, and defeat the Goblin Captain.",Victory="Defeat the Goblin Captain, rescue at least 2 Villagers, and keep Fire below 5.",Failure="All participating Heroes are Defeated.",Reward="100 XP; 75 Gold; draw 1 Equipment; +1 General Reputation; +15 Gold if all 4 Villagers survive."},
            ["Q002"]=new QuestDefinition0829{Id="Q002",Name="Rescue the Merchant Caravan",Story="Recover the missing merchant caravan before its cargo is lost to the enemy.",Victory="Rescue both Wagons and deliver at least 4 total Supply to Stonebrook.",Failure="Both Wagons destroyed; all participating Heroes Defeated.",Reward="125 XP; 100 Gold; draw 1 Equipment; gain 1 Supply for the next Quest."},
            ["Q003"]=new QuestDefinition0829{Id="Q003",Name="Clear the Abandoned Watchtower",Story="Goblin scouts have occupied an old watchtower overlooking the frontier. Reclaim it and relight its warning beacon.",Victory="Defeat all enemies occupying Fallen Watchtower and light the Beacon.",Failure="All Heroes defeated; after Fallen Watchtower is revealed, 3 consecutive rounds pass with no Hero occupying it.",Reward="75 XP; 50 Gold; +1 General Reputation; during WE002 reduce first Travel Encounter by 1 Goblin Scout."},
            ["Q004"]=new QuestDefinition0829{Id="Q004",Name="Into the Dark Forest",Story="Track the hidden goblin patrols and locate their main war camp.",Victory="Reveal the Goblin War Camp and defeat the Goblin War Chief.",Failure="All Heroes defeated; Location Deck is exhausted before Goblin War Camp is revealed.",Reward="150 XP; 125 Gold; draw 1 Rare Equipment Card."},
            ["Q005"]=new QuestDefinition0829{Id="Q005",Name="Destroy the War Camp",Story="Destroy the war camp supplies, eliminate its commander, and scatter the warband.",Victory="Destroy all 4 Supply Crates and defeat the Goblin War Chief.",Failure="All Heroes defeated.",Reward="175 XP; 150 Gold; draw 1 Rare Equipment Card; remove 1 Elite Goblin from Q007 setup."},
            ["Q006"]=new QuestDefinition0829{Id="Q006",Name="Prisoners of the Horde",Story="Captured villagers are being held by the horde. Free as many prisoners as possible and escape before the camp is overrun.",Victory="Rescue at least 4 Prisoners and defeat the Goblin Captain guarding the Pens.",Failure="All Heroes defeated; 4 or more Prisoners are executed.",Reward="100 XP; 75 Gold; +2 General Reputation; begin WE003 with +1 Party Morale."},
            ["Q007"]=new QuestDefinition0829{Id="Q007",Name="Burn the Supply Lines",Story="Destroy the supply convoy before it reaches the besieging army.",Victory="Destroy all 3 Supply Wagons.",Failure="Any 2 Supply Wagons escape; all Heroes defeated.",Reward="175 XP; 150 Gold; draw 1 Rare Equipment Card; remove 1 Siege Engine from Q008."},
            ["Q008"]=new QuestDefinition0829{Id="Q008",Name="Break the Siege",Story="Stonebrook's defenses are failing under relentless bombardment. Destroy the siege engines before the settlement's defenses collapse.",Victory="Destroy all active Siege Engines and defeat the Goblin Champion.",Failure="Stonebrook Wall Track reaches 0; all Heroes defeated.",Reward="200 XP; 175 Gold; draw 1 Epic Equipment Card."},
            ["Q009"]=new QuestDefinition0829{Id="Q009",Name="Rescue Captain Roland",Story="Captain Roland has been captured. Find and rescue him before the execution clock runs out.",Victory="Escort Captain Roland safely to Stonebrook.",Failure="Execution Track reaches 0 before Roland is freed; Roland is defeated after being freed; all Heroes defeated.",Reward="125 XP; 100 Gold; +2 General Reputation; Captain Roland may assist during Q010."},
            ["Q010"]=new QuestDefinition0829{Id="Q010",Name="Assault the Fortress Gate",Story="Break through the Goblin King's fortress defenses and establish a foothold inside the stronghold.",Victory="Destroy the Fortress Gate and defeat the Goblin Champion.",Failure="All Heroes defeated; 4 consecutive rounds pass after the Gate is revealed with no Hero in the Gate or an adjacent assault Location.",Reward="225 XP; 200 Gold; draw 1 Epic Equipment Card."},
            ["Q011"]=new QuestDefinition0829{Id="Q011",Name="Through the Stronghold",Story="Fight through the fortress toward the throne room while the Goblin King prepares his final defense.",Victory="Reach the Goblin Throne Room and defeat every enemy within it.",Failure="All Heroes defeated.",Reward="250 XP; 200 Gold; draw 1 Epic Equipment Card."},
            ["Q012"]=new QuestDefinition0829{Id="Q012",Name="The Goblin King",Story="Destroy the banners of the horde and defeat the Goblin King upon his throne.",Victory="Destroy all 4 Banner Tokens and defeat the Goblin King.",Failure="All Heroes defeated.",Reward="300 XP to each Hero; 1 Legendary Equipment per Hero; +3 General Reputation; unlock Defender of Stonebrook; complete Campaign I."}
        };

        static QuestContent0829()
        {
            FullContentCatalog0850.EnsureLoaded();
            foreach(var q in FullContentCatalog0850.Quests)
            {
                if(q==null||string.IsNullOrWhiteSpace(q.id)||Definitions.ContainsKey(q.id))continue;
                Definitions[q.id]=new QuestDefinition0829{Id=q.id,Name=q.name,Story=q.story,Victory=q.winningConditions,Failure=q.losingConditions,Reward=q.rewards};
            }
        }
    }

    // 0.8.29 — generalized Quest identity/lifecycle plus the first fully quest-owned scenario, Q002.
    public sealed partial class AscendanceGameManager
    {
        public const int Q002StartingSupply0829 = 4; // designer-locked digital/tabletop ruling; supersedes the stale 3-Supply workbook row.
        public QuestRuntimeState0829 QuestRuntime0829 { get; private set; }
        public string CurrentQuestId0829 => QuestRuntime0829?.QuestId ?? "Q001";
        public string CurrentQuestName0829 => QuestRuntime0829?.QuestName ?? "Defend Stonebrook";
        public int NextQuestSupply0829 { get; private set; }
        public bool ReduceFirstWE002TravelScout0830 { get; private set; }
        public bool RemoveEliteFromQ007Setup0830 { get; private set; }
        public int PartyMorale0831 { get; private set; }
        public bool RemoveSiegeEngineFromQ008Setup0831 { get; private set; }
        public bool CaptainRolandAssistsQ0100832 { get; private set; }
        private int questSpawnSerial0829;
        public int QuestSpawnSerialForSave0829 => questSpawnSerial0829;
        public IReadOnlyList<QuestObjectState0829> QuestObjects0829 => QuestRuntime0829?.Objects != null ? QuestRuntime0829.Objects : Array.Empty<QuestObjectState0829>();
        public bool IsQ002Active0829 => CurrentQuestId0829=="Q002"&&!QuestComplete&&!QuestFailed;
        public QuestDefinition0829 CurrentQuestDefinition0829 => QuestContent0829.Definitions.TryGetValue(CurrentQuestId0829,out var d)?d:null;

        private void InitializeQuestRuntime0829()
        {
            QuestRuntime0829=new QuestRuntimeState0829{QuestId="Q001",QuestName=QuestContent0829.Definitions["Q001"].Name};
            questSpawnSerial0829=0;NextQuestSupply0829=0;ReduceFirstWE002TravelScout0830=false;RemoveEliteFromQ007Setup0830=false;PartyMorale0831=0;RemoveSiegeEngineFromQ008Setup0831=false;CaptainRolandAssistsQ0100832=false;
        }

        private string RuntimeLocationIdForAuthored0850(string authoredId)
        {
            if(string.IsNullOrWhiteSpace(authoredId))return null;
            var mapped=Locations.Values.FirstOrDefault(l=>string.Equals(l.AuthoredLocationId,authoredId,StringComparison.OrdinalIgnoreCase));
            if(mapped!=null)return mapped.Id;
            return Locations.ContainsKey(authoredId)?authoredId:null;
        }

        public bool IsLocationEligibleForCurrentQuest0829(string locationId)
        {
            if(string.IsNullOrEmpty(locationId)||!Locations.TryGetValue(locationId,out var location))return false;
            var pool=FullContentCatalog0850.QuestLocationPoolIds(CurrentQuestId0829);
            if(pool.Count==0)return false;
            string authored=string.IsNullOrWhiteSpace(location.AuthoredLocationId)?location.Id:location.AuthoredLocationId;
            return pool.Contains(authored);
        }

        private void ResetLocationVisibilityForQuest0850(string questId)
        {
            string startAuthored=FullContentCatalog0850.StartingLocationId(questId);
            string startRuntime=RuntimeLocationIdForAuthored0850(startAuthored)??"L000";
            ClearDeferredLocationPostSearch0858();
            foreach(var l in Locations.Values)
            {
                l.Searched=false;l.SearchRequests=0;l.FirstVisitResolved=false;l.RevealEffectDeferred=false;l.UsedKeys0858.Clear();
                bool start=string.Equals(l.Id,startRuntime,StringComparison.OrdinalIgnoreCase);
                l.Revealed=start;l.RevealResolved=start;
            }
            foreach(var h in ParticipatingHeroes)h.LocationId=startRuntime;
        }

        // Called only for a fresh/new-game party, never while applying a save.
        private void ConfigureInitialQuestParty0850()
        {
            if(QuestRuntime0829==null||CurrentQuestId0829!="Q001"||QuestComplete||QuestFailed)return;
            foreach(var id in Enemies.Values.Where(e=>e.QuestSpawned&&e.QuestId=="Q001").Select(e=>e.Id).ToList())RemoveEnemyRuntime0829(id);
            QuestRuntime0829.QuestOwnedEnemyIds.Clear();QuestRuntime0829.CaptainSpawned=false;QuestRuntime0829.CaptainDefeated=false;questSpawnSerial0829=0;
            foreach(var h in ParticipatingHeroes)h.HealingPotions=1;
            ResetLocationVisibilityForQuest0850("Q001");
            BuildEnemies();
            InitializeExplorationDeck0828();
            AddLog($"Q001 SETUP — {PartySize} Hero(s): {Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E001")} Raider(s), {Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E002")} Archer(s), no Captain. Each Hero begins with 1 Common Healing Potion.");
        }

        public string QuestObjectiveSummary0829 => IsGenericQuestRuntime0856 ? QuestObjectiveSummary0856() : QuestObjectiveSummary0830();
        public string QuestPrimaryActionLabel0829 => IsGenericQuestRuntime0856 ? QuestPrimaryActionLabel0856() : QuestPrimaryActionLabel0830();
        public string QuestSecondaryActionLabel0829 => CanLocationUtility0858(SelectedHero) ? LocationUtilityLabel0858(SelectedHero) : (IsGenericQuestRuntime0856 ? QuestSecondaryActionLabel0856() : QuestSecondaryActionLabel0830());
        public bool CanQuestPrimaryAction0829(HeroState hero)=>IsGenericQuestRuntime0856?CanQuestPrimaryAction0856(hero):CanQuestPrimaryAction0830(hero);
        public bool CanQuestSecondaryAction0829(HeroState hero)=>CanLocationUtility0858(hero)||(IsGenericQuestRuntime0856?CanQuestSecondaryAction0856(hero):CanQuestSecondaryAction0830(hero));
        public void QuestPrimaryAction0829(){if(IsGenericQuestRuntime0856)QuestPrimaryAction0856();else QuestPrimaryAction0830();}
        public void QuestSecondaryAction0829(){var h=SelectedHero;if(CanLocationUtility0858(h)){if(!RequireHeroTurn(h,"Location Action"))return;ResolveLocationUtility0858(h);return;}if(IsGenericQuestRuntime0856)QuestSecondaryAction0856();else QuestSecondaryAction0830();}
        public string NextQuestId0829 => NextQuestId0830();
        public bool CanBeginNextQuest0829 => !string.IsNullOrEmpty(NextQuestId0829)&&QuestComplete&&!IsResolving&&!HasPendingReactionWindow&&!HasPendingEncounterDeckControl&&!HasPendingRewardChoice;
        public void BeginNextQuest0829(){if(CanBeginNextQuest0829)StartQuest0829(NextQuestId0829);}

        private void RemoveEnemyRuntime0829(string id)
        {
            if(string.IsNullOrEmpty(id))return;
            if(EnemyAvatars.TryGetValue(id,out var av)&&av!=null){EnemyAvatars.Remove(id);UnityEngine.Object.Destroy(av.gameObject);}
            Enemies.Remove(id);
        }

        private void ResetScenarioRuntimeForQuest0829(string nextQuestId)
        {
            foreach(var q in UnityEngine.Object.FindObjectsByType<QuestObjectAvatar0829>())if(q!=null)UnityEngine.Object.Destroy(q.gameObject);
            PendingContentInstruction=null;PendingRewardChoice=null;PendingEncounterDeckControl=null;ActiveEncounters.Clear();EncounterObjects.Clear();
            foreach(var id in Enemies.Values.Where(e=>e.EncounterSpawned||e.QuestSpawned||e.Id=="RaiderA"||e.Id=="RaiderB"||e.Id=="Captain").Select(e=>e.Id).ToList())RemoveEnemyRuntime0829(id);
            FireTokens=0;VillagersRescued=0;QuestComplete=false;QuestFailed=false;TurnHeroId=null;Round=1;
            foreach(var h in ParticipatingHeroes)
            {
                h.Actions=0;h.AttackOnlyActions=0;h.TurnCompleted=false;h.Moving=false;h.PendingLocationId=null;h.Statuses.Clear();h.UsedRoundAbilities.Clear();h.UsedTurnAbilities.Clear();
                if(string.Equals(nextQuestId,"Q001",StringComparison.OrdinalIgnoreCase))h.HealingPotions=1;
                HeroRulesRuntime.OnQuestStart(this,h);
                ApplyEncounterQuestStartEffects0853(h,nextQuestId);
            }
            if(QuestNumber0856(nextQuestId)>=13)EnsureQuestRuntimeLocations0856(nextQuestId);
            ResetLocationVisibilityForQuest0850(nextQuestId);
            QuestRuntime0856=null;
        }

        private void StartQuest0829(string questId)
        {
            if(!QuestContent0829.Definitions.TryGetValue(questId??string.Empty,out var def))return;
            ClearQuestResultForNewQuest0835();
            ResetScenarioRuntimeForQuest0829(def.Id);
            QuestRuntime0829=new QuestRuntimeState0829{QuestId=def.Id,QuestName=def.Name};questSpawnSerial0829=0;InitializeQuestStart0831();OnLootOrEquipmentQuestStart0854();
            if(def.Id=="Q001")BuildEnemies();
            if(QuestNumber0856(def.Id)>=13)InitializeQuestRuntime0856(def.Id);
            OnWorldEventQuestStarted0857(def.Id);
            InitializeExplorationDeck0828();
            SelectedEnemyId=FirstLivingEnemyAtSelectedHeroLocation()?.Id;
            AddLog($"QUEST START — {def.Id} {def.Name}. {def.Story}");
            AddLog($"QUEST OBJECTIVE — {def.Victory}");StateChanged?.Invoke();
        }

        private EnemyState CreateQuestEnemy0829(string templateId,string runtimeId,string locationId)
        {
            if(!SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId??string.Empty,out var t))return null;
            return new EnemyState{Id=runtimeId,TemplateId=t.Id,QuestSpawned=true,QuestId=CurrentQuestId0829,DisplayName=t.Name,MaxHealth=t.Health,Health=t.Health,Armor=t.Armor,Attack=t.Attack,LocationId=locationId,IsBoss=t.Boss};
        }
        private EnemyState SpawnQuestEnemy0829(string templateId,string locationId,string source)
        {
            string id=$"QST_{CurrentQuestId0829}_{templateId}_{++questSpawnSerial0829:0000}";var enemy=CreateQuestEnemy0829(templateId,id,locationId);if(enemy==null){AppendPendingContent("Quest spawn",$"Enemy {templateId} is not available.");return null;}
            ApplyWorldEventEnemySpawn0857(enemy,source,true);
            Enemies[id]=enemy;QuestRuntime0829.QuestOwnedEnemyIds.Add(id);AddLog($"QUEST SPAWN — {enemy.DisplayName} ({templateId}) enters {Locations[locationId].Name} [{CurrentQuestId0829}] ({source}).");
            TryInjectWorldEventQuestSpawn0857(locationId,source);
            UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>()?.EnsureRuntimeEnemySpawns();return enemy;
        }

        public void OnQuestLocationRevealed0829(LocationState location,HeroState hero)
        {
            OnWorldEventLocationRevealed0857(location,hero);
            if(IsGenericQuestRuntime0856){OnGenericQuestLocationRevealed0856(location,hero);return;}
            OnQuestLocationRevealed0830(location,hero);
            if(CurrentQuestId0829=="Q001"&&location!=null&&!QuestRuntime0829.CaptainSpawned&&string.Equals(location.AuthoredLocationId,"L006",StringComparison.OrdinalIgnoreCase))
            {
                QuestRuntime0829.CaptainSpawned=true;
                SpawnQuestEnemy0829("E006",location.Id,"Goblin Lookout revealed");
                SpawnQuestEnemy0829("E004",location.Id,"Goblin Lookout revealed");
                if(PartySize>=3)SpawnQuestEnemy0829("E001",location.Id,"3+ Hero Q001 scaling");
                AddLog($"Q001 REVEAL — Goblin Lookout: the Goblin Captain and his patrol enter play{(PartySize>=3?" with an extra Raider":"")}.");
                EvaluateQuestOutcome0829();
                return;
            }
            if(!IsQ002Active0829||location==null||!string.Equals(location.AuthoredLocationId,"L047",StringComparison.OrdinalIgnoreCase)||QuestRuntime0829.CaravanRevealed)return;
            QuestRuntime0829.CaravanRevealed=true;QuestRuntime0829.Objects.Clear();
            QuestRuntime0829.Objects.Add(new QuestObjectState0829{ObjectId="Q002_WAGON_1",QuestId="Q002",Type="Wagon",DisplayName="Wagon 1",LocationId=location.Id,Supply=Q002StartingSupply0829});
            QuestRuntime0829.Objects.Add(new QuestObjectState0829{ObjectId="Q002_WAGON_2",QuestId="Q002",Type="Wagon",DisplayName="Wagon 2",LocationId=location.Id,Supply=Q002StartingSupply0829});
            int party=PartySize;int raiders=party<=1?1:party>=3?3:2;int scouts=1;int archers=party>=4?1:0;
            for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001",location.Id,"Merchant Caravan revealed");
            for(int i=0;i<scouts;i++)SpawnQuestEnemy0829("E004",location.Id,"Merchant Caravan revealed");
            for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002",location.Id,"4-Hero scaling");
            foreach(var wagon in QuestRuntime0829.Objects)QuestObjectAvatar0829.Create(wagon);
            AddLog($"Q002 SETUP — Merchant Caravan revealed: 2 Wagons with {Q002StartingSupply0829} Supply each; quest enemies deployed.");EvaluateQuestOutcome0829();
        }

        private void ResolveQuestEnemyStartActivation0829(EnemyState enemy)
        {
            if(!IsQ002Active0829||enemy==null||enemy.Defeated||!IsQ002Goblin0829(enemy))return;
            var wagon=QuestRuntime0829.Objects.Where(w=>w.Type=="Wagon"&&!w.Rescued&&!w.Destroyed&&w.LocationId==enemy.LocationId).OrderBy(w=>w.ObjectId).FirstOrDefault();if(wagon==null)return;
            if(ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId))return;
            wagon.Supply=Mathf.Max(0,wagon.Supply-1);AddLog($"Q002 — UNATTENDED WAGON: {enemy.DisplayName} removes 1 Supply from {wagon.DisplayName}. {wagon.Supply} remain.");
            if(wagon.Supply<=0){wagon.Destroyed=true;AddLog($"Q002 OBJECTIVE — {wagon.DisplayName} is destroyed.");}EvaluateQuestOutcome0829();
        }
        private static bool IsQ002Goblin0829(EnemyState enemy)=>enemy!=null&&(enemy.TemplateId=="E001"||enemy.TemplateId=="E002"||enemy.TemplateId=="E004"||enemy.TemplateId=="E006"||enemy.DisplayName?.IndexOf("Goblin",StringComparison.OrdinalIgnoreCase)>=0);

        private void ResolveQuestEndEnemyPhase0829()
        {
            if(IsGenericQuestRuntime0856){ResolveGenericQuestEndEnemyPhase0856();return;}
            if(CurrentQuestId0829=="Q001")
            {
                if(!QuestRuntime0829.CaptainSpawned)
                {
                    FireTokens=Mathf.Min(5,FireTokens+1);
                    bool spawnRaider=PartySize>1||Round%2==0;
                    if(spawnRaider)SpawnQuestEnemy0829("E001",RuntimeLocationIdForAuthored0850("L000")??"L000","Q001 Stonebrook reinforcement");
                    AddLog($"Q001 PRESSURE — add 1 Fire Token ({FireTokens}/5){(spawnRaider?" and 1 Goblin Raider":"; 1-Hero Raider reinforcement waits until the next even round")}.");
                }
                else if(!QuestRuntime0829.CaptainDefeated)
                {
                    var captain=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E006");
                    string loc=captain?.LocationId??RuntimeLocationIdForAuthored0850("L006")??"L000";
                    int count=PartySize>=4?2:1;
                    for(int i=0;i<count;i++)SpawnQuestEnemy0829("E001",loc,"Q001 Captain reinforcement");
                    AddLog($"Q001 REINFORCEMENTS — Captain in play: {count} Goblin Raider{(count==1?"":"s")} join his position.");
                }
                EvaluateQuestOutcome0829();
                return;
            }
            if(ResolveQuestEndEnemyPhase0830())return;
            if(!IsQ002Active0829||!QuestRuntime0829.CaravanRevealed||QuestRuntime0829.CaptainSpawned)return;
            if(PartySize<=1&&Round%2!=0)return;
            var active=QuestRuntime0829.Objects.Where(w=>w.Type=="Wagon"&&!w.Rescued&&!w.Destroyed).ToList();var heroes=ParticipatingHeroes.Where(h=>!h.IsDefeated).ToList();if(heroes.Count==0)return;
            HeroState farthest=heroes.OrderByDescending(h=>active.Count==0?0:active.Min(w=>GraphDistance(h.LocationId,w.LocationId))).ThenBy(h=>h.PartyIndex).First();
            QuestRuntime0829.ReinforcementCount++;SpawnQuestEnemy0829("E001",farthest.LocationId,$"reinforcement wave {QuestRuntime0829.ReinforcementCount}");
            if(QuestRuntime0829.ReinforcementCount==1)SpawnQuestEnemy0829("E002","L005","reinforcement wave 1");
            else if(QuestRuntime0829.ReinforcementCount==2)SpawnQuestEnemy0829("E004","L005","reinforcement wave 2");
            else if(QuestRuntime0829.ReinforcementCount>=3)
            {
                if(PartySize>=4)SpawnQuestEnemy0829("E001",farthest.LocationId,"4-Hero third-wave scaling");
                SpawnQuestEnemy0829("E006","L005","reinforcement wave 3");SpawnQuestEnemy0829("E002","L005","reinforcement wave 3");QuestRuntime0829.CaptainSpawned=true;
                AddLog("Q002 REINFORCEMENTS — Goblin Captain has entered play; recurring reinforcements stop.");
            }
        }

        private void BeginWagonMove0829()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0829(hero)||!RequireHeroTurn(hero,"Move Wagon"))return;
            var wagons=QuestRuntime0829.Objects.Where(w=>w.Type=="Wagon"&&!w.Rescued&&!w.Destroyed&&w.LocationId==hero.LocationId).OrderBy(w=>w.ObjectId).ToList();
            BeginRewardChoice0827("Q002","Move Wagon",hero,"QuestWagonMove","Choose a Wagon and connected revealed destination.",wagons.Select(w=>w.ObjectId));
        }

        private void AppendQuestChoiceOptions0829(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(state?.Mode!="QuestWagonMove")return;var hero=RewardHeroFor(state.HeroId);if(hero==null||!Locations.TryGetValue(hero.LocationId,out var loc))return;
            foreach(var wid in state.RevealedCardIds??new List<string>())
            {
                var w=QuestRuntime0829.Objects.FirstOrDefault(x=>x.ObjectId==wid&&!x.Rescued&&!x.Destroyed&&x.LocationId==hero.LocationId);if(w==null)continue;
                foreach(var dest in loc.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed))options.Add(new RewardChoiceOption{Key=$"MOVE|{w.ObjectId}|{dest}",Label=$"{w.DisplayName} ({w.Supply} Supply) → {Locations[dest].Name}"});
            }
        }
        private bool TryResolveQuestChoice0829(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state?.Mode!="QuestWagonMove")return false;if(hero==null||!key.StartsWith("MOVE|",StringComparison.OrdinalIgnoreCase))return true;
            var p=key.Split('|');if(p.Length!=3)return true;var w=QuestRuntime0829.Objects.FirstOrDefault(x=>x.ObjectId==p[1]&&!x.Rescued&&!x.Destroyed&&x.LocationId==hero.LocationId);if(w==null||!Locations.TryGetValue(hero.LocationId,out var from)||!from.Connections.Contains(p[2])||!Locations.TryGetValue(p[2],out var dest)||!dest.Revealed||GeneralActions(hero)<1)return true;
            hero.Actions--;w.LocationId=dest.Id;if(dest.Id=="L000"){w.Rescued=true;QuestRuntime0829.DeliveredSupply+=w.Supply;AddLog($"Q002 OBJECTIVE — {hero.DisplayName} escorts {w.DisplayName} into Stonebrook with {w.Supply} Supply. Delivered total: {QuestRuntime0829.DeliveredSupply}.");}else AddLog($"Q002 — {hero.DisplayName} escorts {w.DisplayName} to {dest.Name} ({w.Supply} Supply).");
            PendingRewardChoice=null;EvaluateQuestOutcome0829();StateChanged?.Invoke();result=true;return true;
        }

        private bool TryResolveQuestSearchOverride0829(LocationState location,HeroState hero)
        {
            if(IsGenericQuestRuntime0856)TryResolveGenericQuestSearch0856(location,hero);
            if(!IsQ002Active0829||location?.Id!="L005"||QuestRuntime0829.ManifestGranted)return false;
            GrantSpecificLootQuestItem0829(hero,"LT123","Merchant Caravan first Search");QuestRuntime0829.ManifestGranted=true;AddLog("Q002 QUEST ITEM — first Merchant Caravan Search gains LT123 Caravan Manifest instead of a normal Search reward.");return true;
        }

        private void GrantSpecificLootQuestItem0829(HeroState hero,string id,string source)
        {
            if(hero==null)return;int at=LootDeck.FindIndex(x=>x==id);if(at>=0)LootDeck.RemoveAt(at);else{at=LootDiscard.FindIndex(x=>x==id);if(at>=0)LootDiscard.RemoveAt(at);}
            if(!hero.LootInventory.Contains(id))hero.LootInventory.Add(id);AddLog($"QUEST ITEM — {hero.DisplayName} gains {RewardContent0826.LootLabel(id)} ({source}).");
        }
        private void ReturnQuestLootToDeck0829(string id,string source)
        {
            foreach(var h in Heroes.Values)while(h.LootInventory.Remove(id)){}if(!LootDeck.Contains(id)&&!LootDiscard.Contains(id))LootDeck.Add(id);AddLog($"QUEST ITEM RETURN — {RewardContent0826.LootLabel(id)} returns to the Loot Deck ({source}).");
        }
        private void OnQuestEnemyDefeated0829(EnemyState enemy,HeroState killer)
        {
            if(IsGenericQuestRuntime0856){OnGenericQuestEnemyDefeated0856(enemy,killer);return;}
            OnQuestEnemyDefeated0830(enemy,killer);
            if(CurrentQuestId0829=="Q001"&&enemy!=null&&enemy.QuestId=="Q001"&&enemy.TemplateId=="E006")
            {
                QuestRuntime0829.CaptainDefeated=true;AddLog("Q001 OBJECTIVE — Goblin Captain defeated.");EvaluateQuestOutcome0829();return;
            }
            if(!IsQ002Active0829||enemy==null||enemy.QuestId!="Q002"||enemy.TemplateId!="E006"||QuestRuntime0829.StrongboxKeyGranted)return;
            QuestRuntime0829.CaptainDefeated=true;QuestRuntime0829.StrongboxKeyGranted=true;GrantSpecificLootQuestItem0829(killer??SelectedHero,"LT119","Goblin Captain defeated");
        }

        private void OpenStrongbox0829()
        {
            var hero=SelectedHero;if(!CanQuestSecondaryAction0829(hero)||!RequireHeroTurn(hero,"Open Merchant Strongbox"))return;hero.Actions--;hero.LootInventory.Remove("LT119");if(!LootDeck.Contains("LT119")&&!LootDiscard.Contains("LT119"))LootDeck.Add("LT119");QuestRuntime0829.StrongboxOpened=true;
            string id=LootDeck.FirstOrDefault(x=>RewardContent0826.Loot.TryGetValue(x,out var d)&&!d.QuestItem&&((d.DropTags??string.Empty).IndexOf("Merchant",StringComparison.OrdinalIgnoreCase)>=0||(d.DropTags??string.Empty).IndexOf("Caravan",StringComparison.OrdinalIgnoreCase)>=0));
            if(!string.IsNullOrEmpty(id)){LootDeck.Remove(id);hero.LootInventory.Add(id);AddLog($"Q002 STRONGBOX — {hero.DisplayName} returns LT119 and gains source-matched {RewardContent0826.LootLabel(id)}.");}else AddLog("Q002 STRONGBOX — no Merchant/Caravan source-matched Loot remained in the live deck.");StateChanged?.Invoke();
        }

        private void EvaluateQuestOutcome0829()
        {
            if(QuestComplete||QuestFailed)return;
            if(IsGenericQuestRuntime0856){EvaluateGenericQuestOutcome0856();return;}
            if(CurrentQuestId0829=="Q002")
            {
                var wagons=QuestRuntime0829.Objects.Where(w=>w.Type=="Wagon").ToList();
                if(wagons.Count>=2&&wagons.All(w=>w.Destroyed)){QuestFailed=true;AddLog("Q002 FAILED — both Wagons are destroyed.");return;}
                if(wagons.Count>=2&&wagons.All(w=>w.Rescued)&&QuestRuntime0829.DeliveredSupply>=4){QuestComplete=true;AddLog($"Q002 COMPLETE — both Wagons rescued with {QuestRuntime0829.DeliveredSupply} total Supply delivered.");ResolveQuestReward0829();}
                return;
            }
            if(EvaluateQuestOutcome0830())return;
            bool captainDown=QuestRuntime0829?.CaptainDefeated==true;
            if(FireTokens>=5){QuestFailed=true;AddLog("Q001 FAILED — Stonebrook has reached 5 Fire Tokens.");return;}
            if(captainDown&&VillagersRescued>=2){QuestComplete=true;AddLog("Q001 COMPLETE — Goblin Captain defeated, at least 2 Villagers rescued, and Stonebrook remains below 5 Fire Tokens.");ResolveQuestReward0829();AddLog("POST-QUEST — free exploration continues. Begin Q002 when ready.");}
        }

        private void ResolveQuestReward0829()
        {
            if(QuestRuntime0829==null||QuestRuntime0829.RewardResolved)return;
            BeginQuestResultCapture0835();
            QuestRuntime0829.RewardResolved=true;var receiver=SelectedHero??ParticipatingHeroes.FirstOrDefault();
            RecordCampaignQuestCompleted0833(CurrentQuestId0829);
            bool resolvedByExtendedRuntime=ResolveGenericQuestReward0856(receiver);
            if(!resolvedByExtendedRuntime)resolvedByExtendedRuntime=ResolveQuestReward0830(receiver);
            if(!resolvedByExtendedRuntime&&CurrentQuestId0829=="Q002")
            {
                foreach(var h in ParticipatingHeroes)h.XP+=125;AddPartyGold(100,"Q002 victory reward");if(receiver!=null)DrawEquipmentToHero(receiver,"Q002 victory reward");NextQuestSupply0829+=1;ReturnQuestLootToDeck0829("LT123","Q002 ended");AddLog("Q002 REWARD — 125 XP each • 100 Gold • 1 Equipment • +1 Supply banked for the next Quest.");
            }
            else if(!resolvedByExtendedRuntime)
            {
                foreach(var h in ParticipatingHeroes)h.XP+=100;AddPartyGold(75,"Q001 victory reward");AddGeneralReputation(1,"Q001 victory reward");if(receiver!=null)DrawEquipmentToHero(receiver,"Q001 victory reward");if(VillagersRescued>=4)AddPartyGold(15,"Q001 all Villagers survive bonus");AddLog("Q001 REWARD — 100 XP each • 75 Gold • 1 Equipment • +1 General Reputation.");
            }
            FinalizeQuestResultCapture0835(true);
        }

        private void FailCurrentQuestForPartyDefeat0829()
        {
            if(QuestComplete||QuestFailed)return;QuestFailed=true;AddLog($"{CurrentQuestId0829} FAILED — all participating Heroes are Defeated.");
        }

        private void RestoreQuestRuntime0829(AscendanceSaveData save)
        {
            string q=!string.IsNullOrEmpty(save?.QuestId)?save.QuestId:"Q001";var def=QuestContent0829.Definitions.TryGetValue(q,out var d)?d:QuestContent0829.Definitions["Q001"];
            if(save?.QuestRuntime0829!=null)
            {
                var s=save.QuestRuntime0829;QuestRuntime0829=new QuestRuntimeState0829{QuestId=string.IsNullOrEmpty(s.QuestId)?def.Id:s.QuestId,QuestName=string.IsNullOrEmpty(s.QuestName)?def.Name:s.QuestName,RewardResolved=s.RewardResolved,CaravanRevealed=s.CaravanRevealed,ReinforcementCount=s.ReinforcementCount,CaptainSpawned=s.CaptainSpawned,CaptainDefeated=s.CaptainDefeated,ManifestGranted=s.ManifestGranted,StrongboxKeyGranted=s.StrongboxKeyGranted,StrongboxOpened=s.StrongboxOpened,DeliveredSupply=s.DeliveredSupply,ObjectiveLocationRevealed=s.ObjectiveLocationRevealed,BeaconLit=s.BeaconLit,ObjectiveEmptyRounds=s.ObjectiveEmptyRounds,VaultKeyGranted=s.VaultKeyGranted,VaultOpened=s.VaultOpened,InitialPatrolSpawned=s.InitialPatrolSpawned,TrailMarkers=s.TrailMarkers,WarCampRevealed=s.WarCampRevealed,WarChiefDefeated=s.WarChiefDefeated,AlarmTokens=s.AlarmTokens,SupplyCratesDestroyed=s.SupplyCratesDestroyed,PrisonersFreed=s.PrisonersFreed,PrisonersRescued=s.PrisonersRescued,PrisonersExecuted=s.PrisonersExecuted,PrisonAlarmSpawned=s.PrisonAlarmSpawned,PrisonReinforcementSpawned=s.PrisonReinforcementSpawned,SupplyWagonsDestroyed=s.SupplyWagonsDestroyed,SupplyWagonsEscaped=s.SupplyWagonsEscaped,ConvoyFirstDestroyedWave=s.ConvoyFirstDestroyedWave,ConvoySecondDestroyedWave=s.ConvoySecondDestroyedWave,WallTrack=s.WallTrack,Wall4Triggered=s.Wall4Triggered,Wall2Triggered=s.Wall2Triggered,SiegeEnginesPlaced=s.SiegeEnginesPlaced,SiegeEnginesDestroyed=s.SiegeEnginesDestroyed,ChampionSpawned=s.ChampionSpawned,ChampionDefeated=s.ChampionDefeated,RolandRevealed=s.RolandRevealed,RolandFreed=s.RolandFreed,RolandRescued=s.RolandRescued,RolandDefeated=s.RolandDefeated,ExecutionTrack=s.ExecutionTrack,FortressGateRevealed=s.FortressGateRevealed,GateHealth=s.GateHealth,GateArmor=s.GateArmor,GateReinforcementTriggered=s.GateReinforcementTriggered,GateDestroyed=s.GateDestroyed,BallistasDestroyed=s.BallistasDestroyed,BallistaRepairPending=s.BallistaRepairPending,BallistaRepairUsed=s.BallistaRepairUsed,PendingBallistaObjectId=s.PendingBallistaObjectId,GateEmptyRounds=s.GateEmptyRounds,StrongholdQualifyingReveals=s.StrongholdQualifyingReveals,ReinforcementTokensRemaining=s.ReinforcementTokensRemaining,HighShamanWaiting=s.HighShamanWaiting,ThroneRoomRevealed=s.ThroneRoomRevealed,BannersDestroyed=s.BannersDestroyed,GoblinKingLastStandTriggered=s.GoblinKingLastStandTriggered,GoblinKingHordeRound=s.GoblinKingHordeRound,CampaignVictory=s.CampaignVictory,DefenderOfStonebrookUnlocked=s.DefenderOfStonebrookUnlocked,QuestOwnedEnemyIds=s.QuestOwnedEnemyIds?.ToList()??new List<string>(),Objects=s.Objects?.Select(o=>new QuestObjectState0829{ObjectId=o.ObjectId,QuestId=o.QuestId,Type=o.Type,DisplayName=o.DisplayName,LocationId=o.LocationId,OwnerHeroId=o.OwnerHeroId,Supply=o.Supply,Health=o.Health,MaxHealth=o.MaxHealth,Armor=o.Armor,Attack=o.Attack,Range=o.Range,Rescued=o.Rescued,Destroyed=o.Destroyed,Resolved=o.Resolved}).ToList()??new List<QuestObjectState0829>()};
            }
            else QuestRuntime0829=new QuestRuntimeState0829{QuestId=def.Id,QuestName=def.Name,RewardResolved=save!=null&&save.QuestComplete};
            questSpawnSerial0829=Mathf.Max(0,save?.QuestSpawnSerial0829??0);NextQuestSupply0829=Mathf.Max(0,save?.NextQuestSupply0829??0);ReduceFirstWE002TravelScout0830=save?.ReduceFirstWE002TravelScout0830??false;RemoveEliteFromQ007Setup0830=save?.RemoveEliteFromQ007Setup0830??false;PartyMorale0831=Mathf.Max(0,save?.PartyMorale0831??0);RemoveSiegeEngineFromQ008Setup0831=save?.RemoveSiegeEngineFromQ008Setup0831??false;CaptainRolandAssistsQ0100832=save?.CaptainRolandAssistsQ0100832??false;
            if(QuestRuntime0829.QuestId!="Q001")foreach(var id in new[]{"RaiderA","RaiderB","Captain"})RemoveEnemyRuntime0829(id);
            RestoreQuestRuntime0856(save);
        }

        // deterministic Play Mode hooks
        public void DebugStartQuest0829(string id){DebugAlignCampaignWorldEventForQuest0833(id);StartQuest0829(id);ApplyCampaignQuestStartEffects0833(id);}
        public bool DebugRevealQuestLocation0829(string locationId,string heroId){if(!Locations.TryGetValue(locationId,out var l)||!Heroes.TryGetValue(heroId,out var h))return false;l.Revealed=true;l.RevealResolved=true;RemoveExplorationLocationFromDeck0828(locationId);OnQuestLocationRevealed0829(l,h);return true;}
        public bool DebugQuestEnemyStartActivation0829(string enemyId){if(!Enemies.TryGetValue(enemyId,out var e))return false;ResolveQuestEnemyStartActivation0829(e);return true;}
        public void DebugQuestEndEnemyPhase0829()=>ResolveQuestEndEnemyPhase0829();
        public void DebugSetHeroAt0829(string heroId,string locationId,int actions=3){if(Heroes.TryGetValue(heroId,out var h)&&Locations.ContainsKey(locationId)){SelectedHeroId=heroId;TurnHeroId=heroId;h.LocationId=locationId;h.Actions=actions;h.TurnCompleted=false;}}
        public string DebugQuestEnemyId0829(string templateId)=>Enemies.Values.FirstOrDefault(e=>e.QuestSpawned&&!e.Defeated&&e.TemplateId==templateId)?.Id;
        public int DebugWagonSupply0829(int index)=>QuestRuntime0829?.Objects?.Where(o=>o.Type=="Wagon").OrderBy(o=>o.ObjectId).ElementAtOrDefault(index)?.Supply??-1;
        public string DebugWagonLocation0829(int index)=>QuestRuntime0829?.Objects?.Where(o=>o.Type=="Wagon").OrderBy(o=>o.ObjectId).ElementAtOrDefault(index)?.LocationId;
        public bool DebugWagonRescued0829(int index)=>QuestRuntime0829?.Objects?.Where(o=>o.Type=="Wagon").OrderBy(o=>o.ObjectId).ElementAtOrDefault(index)?.Rescued??false;
        public void DebugSetWagon0829(int index,string locationId,int supply,bool rescued=false,bool destroyed=false){var w=QuestRuntime0829?.Objects?.Where(o=>o.Type=="Wagon").OrderBy(o=>o.ObjectId).ElementAtOrDefault(index);if(w!=null){w.LocationId=locationId;w.Supply=supply;w.Rescued=rescued;w.Destroyed=destroyed;}}
        public void DebugEvaluateQuestOutcome0829()=>EvaluateQuestOutcome0829();
        public void DebugDefeatQuestEnemy0829(string enemyId,string heroId){if(Enemies.TryGetValue(enemyId,out var e)&&Heroes.TryGetValue(heroId,out var h)){e.Health=0;OnQuestEnemyDefeated0829(e,h);EvaluateQuestOutcome0829();}}
        public bool DebugHasLoot0829(string heroId,string lootId)=>Heroes.TryGetValue(heroId,out var h)&&h.LootInventory.Contains(lootId);
    }
}
