using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.31 — Campaign I Quest Batch 2: Q006 Prisoners of the Horde,
    // Q007 Burn the Supply Lines, Q008 Break the Siege.
    public sealed partial class AscendanceGameManager
    {
        public const int Q008SiegeEngineHealth0831=10;
        public const int Q008SiegeEngineArmor0831=2;
        private bool IsQ006Active0831=>CurrentQuestId0829=="Q006"&&!QuestComplete&&!QuestFailed;
        private bool IsQ007Active0831=>CurrentQuestId0829=="Q007"&&!QuestComplete&&!QuestFailed;
        private bool IsQ008Active0831=>CurrentQuestId0829=="Q008"&&!QuestComplete&&!QuestFailed;
        private bool IsQuestBatch20831=>CurrentQuestId0829=="Q006"||CurrentQuestId0829=="Q007"||CurrentQuestId0829=="Q008";
        private int Q008RequiredEngines0831=>RemoveSiegeEngineFromQ008Setup0831?1:2;

        private void InitializeQuestStart0831()
        {
            InitializeQuestStart0832();
            if(CurrentQuestId0829=="Q008")QuestRuntime0829.WallTrack=6;
        }

        private string QuestObjectiveSummary0831()
        {
            string q832=QuestObjectiveSummary0832();if(q832!=null)return q832;
            if(CurrentQuestId0829=="Q006")
                return $"QUEST Q006 — PRISONERS OF THE HORDE\n\n{(QuestRuntime0829?.ObjectiveLocationRevealed==true?"[OK]":"[ ]")} Prison Pens revealed\n{(QuestRuntime0829?.PrisonersRescued>=4?"[OK]":"[ ]")} Prisoners rescued {QuestRuntime0829?.PrisonersRescued??0}/4\nPrisoners freed {QuestRuntime0829?.PrisonersFreed??0}/6 • Executed {QuestRuntime0829?.PrisonersExecuted??0}/4\n{(QuestRuntime0829?.CaptainDefeated==true?"[OK]":"[ ]")} Goblin Captain defeated";
            if(CurrentQuestId0829=="Q007")
                return $"QUEST Q007 — BURN THE SUPPLY LINES\n\n{(QuestRuntime0829?.CaravanRevealed==true?"[OK]":"[ ]")} Merchant Caravan revealed\n{(QuestRuntime0829?.SupplyWagonsDestroyed>=3?"[OK]":"[ ]")} Supply Wagons destroyed {QuestRuntime0829?.SupplyWagonsDestroyed??0}/3\nWagons escaped {QuestRuntime0829?.SupplyWagonsEscaped??0}/2\nActive Wagons {QuestRuntime0829?.Objects?.Count(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued)??0}";
            if(CurrentQuestId0829=="Q008")
                return $"QUEST Q008 — BREAK THE SIEGE\n\nStonebrook Wall {QuestRuntime0829?.WallTrack??6}/6\n{(QuestRuntime0829?.SiegeEnginesDestroyed>=Q008RequiredEngines0831?"[OK]":"[ ]")} Siege Engines destroyed {QuestRuntime0829?.SiegeEnginesDestroyed??0}/{Q008RequiredEngines0831}\nEngines placed {QuestRuntime0829?.SiegeEnginesPlaced??0}/{Q008RequiredEngines0831}\n{(QuestRuntime0829?.ChampionDefeated==true?"[OK]":"[ ]")} Goblin Champion defeated\nSiege Engine stats: {Q008SiegeEngineHealth0831} Health / {Q008SiegeEngineArmor0831} Armor";
            return null;
        }
        private string QuestPrimaryActionLabel0831()
        {
            string q832=QuestPrimaryActionLabel0832();if(q832!=null)return q832;
            if(CurrentQuestId0829=="Q006")return "Free Prisoner";
            if(CurrentQuestId0829=="Q007")return "Sabotage Wagon";
            if(CurrentQuestId0829=="Q008")return "Attack Engine";
            return null;
        }
        private string QuestSecondaryActionLabel0831(){string q832=QuestSecondaryActionLabel0832();return q832??(IsQuestBatch20831?"Quest Action":null);}
        private bool CanQuestPrimaryAction0831(HeroState hero)
        {
            if(IsQuestBatchFinal0832)return CanQuestPrimaryAction0832(hero);
            if(hero==null||!CanUseTurnActions(hero)||GeneralActions(hero)<1)return false;
            if(IsQ006Active0831)return hero.LocationId=="L018"&&QuestRuntime0829.Objects.Any(o=>o.Type=="Prisoner"&&!o.Destroyed&&!o.Rescued&&!o.Resolved);
            if(IsQ007Active0831)return QuestRuntime0829.Objects.Any(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued&&o.LocationId==hero.LocationId);
            if(IsQ008Active0831)return QuestRuntime0829.Objects.Any(o=>o.Type=="SiegeEngine"&&!o.Destroyed&&o.LocationId==hero.LocationId);
            return false;
        }
        private bool CanQuestSecondaryAction0831(HeroState hero)=>IsQuestBatchFinal0832?CanQuestSecondaryAction0832(hero):false;
        private void QuestPrimaryAction0831()
        {
            if(IsQuestBatchFinal0832){QuestPrimaryAction0832();return;}
            if(IsQ006Active0831){FreePrisoner0831();return;}
            if(IsQ007Active0831){SabotageSupplyWagon0831();return;}
            if(IsQ008Active0831){AttackSiegeEngine0831();return;}
        }
        private void QuestSecondaryAction0831(){if(IsQuestBatchFinal0832)QuestSecondaryAction0832();}

        private bool OnQuestLocationRevealed0831(LocationState location,HeroState hero)
        {
            if(OnQuestLocationRevealed0832(location,hero))return true;
            if(location==null||QuestRuntime0829==null||!IsQuestBatch20831)return false;
            if(IsQ006Active0831&&location.Id=="L018"&&!QuestRuntime0829.ObjectiveLocationRevealed)
            {
                QuestRuntime0829.ObjectiveLocationRevealed=true;QuestRuntime0829.Objects.Clear();
                for(int i=1;i<=6;i++){var p=new QuestObjectState0829{ObjectId=$"Q006_PRISONER_{i}",QuestId="Q006",Type="Prisoner",DisplayName=$"Prisoner {i}",LocationId="L018"};QuestRuntime0829.Objects.Add(p);QuestObjectAvatar0829.Create(p);}
                int raiders=PartySize<=1?1:PartySize>=3?3:2;int archers=PartySize>=4?2:1;
                for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L018","Prison Pens revealed");for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002","L018","Prison Pens revealed");SpawnQuestEnemy0829("E006","L018","Prison Pens revealed");QuestRuntime0829.CaptainSpawned=true;
                AddLog("Q006 SETUP — Prison Pens revealed; 6 Prisoners and the Goblin Captain's guards enter play.");return true;
            }
            if(IsQ007Active0831&&location.Id=="L005"&&!QuestRuntime0829.CaravanRevealed)
            {
                QuestRuntime0829.CaravanRevealed=true;QuestRuntime0829.Objects.Clear();
                for(int i=1;i<=3;i++){var w=new QuestObjectState0829{ObjectId=$"Q007_WAGON_{i}",QuestId="Q007",Type="SupplyWagon",DisplayName=$"Supply Wagon {i}",LocationId="L005",Supply=0};QuestRuntime0829.Objects.Add(w);QuestObjectAvatar0829.Create(w);}
                int raiders=PartySize<=1?1:PartySize>=3?3:2;int archers=PartySize>=4?2:1;
                for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L005","Merchant Caravan convoy revealed");for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002","L005","Merchant Caravan convoy revealed");SpawnQuestEnemy0829("E006","L005","Merchant Caravan convoy revealed");
                if(!RemoveEliteFromQ007Setup0830)SpawnQuestEnemy0829("E013","L005","Merchant Caravan convoy revealed");else AddLog("Q007 CAMPAIGN EFFECT — prior campaign rewards remove the initial Goblin Wolf Rider from the convoy setup.");if(AddEliteToQ007Setup0833)SpawnQuestEnemy0829("E013","L005","WE002 incomplete chapter penalty");
                AddLog("Q007 SETUP — 3 Supply Wagons enter play at Merchant Caravan. Each requires 2 Sabotage Tokens to destroy.");return true;
            }
            if(IsQ008Active0831&&IsQ008SiegeLocation0831(location)&&QuestRuntime0829.SiegeEnginesPlaced<Q008RequiredEngines0831&&!QuestRuntime0829.Objects.Any(o=>o.Type=="SiegeEngine"&&o.LocationId==location.Id))
            {
                PlaceSiegeEngine0831(location);return true;
            }
            return IsQuestBatch20831;
        }

        private bool IsQ008SiegeLocation0831(LocationState location)=>location!=null&&(location.HasPrintedTag("Goblin Structure")||location.HasPrintedTag("Goblin Stronghold")||location.AuthoredLocationId=="L020");
        private void PlaceSiegeEngine0831(LocationState location)
        {
            int n=++QuestRuntime0829.SiegeEnginesPlaced;var obj=new QuestObjectState0829{ObjectId=$"Q008_ENGINE_{n}",QuestId="Q008",Type="SiegeEngine",DisplayName=$"Siege Engine {n}",LocationId=location.Id,Health=Q008SiegeEngineHealth0831,MaxHealth=Q008SiegeEngineHealth0831,Armor=Q008SiegeEngineArmor0831};QuestRuntime0829.Objects.Add(obj);QuestObjectAvatar0829.Create(obj);
            int raiders=2;if(PartySize<=1)raiders=1;else if(PartySize>=4)raiders=3;else if(PartySize==3&&n==1)raiders=3;
            SpawnQuestEnemy0829("E016",location.Id,$"Siege Engine {n} revealed");for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001",location.Id,$"Siege Engine {n} revealed");SpawnQuestEnemy0829("E002",location.Id,$"Siege Engine {n} revealed");
            AddLog($"Q008 SIEGE ENGINE — {obj.DisplayName} placed at {location.Name}: {obj.Health} Health / {obj.Armor} Armor. Bombardment is its offensive effect.");
        }

        private void FreePrisoner0831()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0831(hero)||!RequireHeroTurn(hero,"Free Prisoner"))return;var prisoner=QuestRuntime0829.Objects.Where(o=>o.Type=="Prisoner"&&!o.Destroyed&&!o.Rescued&&!o.Resolved).OrderBy(o=>o.ObjectId).First();hero.Actions--;prisoner.Resolved=true;prisoner.OwnerHeroId=hero.Id;prisoner.LocationId=hero.LocationId;QuestRuntime0829.PrisonersFreed++;int n=QuestRuntime0829.PrisonersFreed;AddLog($"Q006 OBJECTIVE — {hero.DisplayName} frees {prisoner.DisplayName} ({n}/6); it now moves with that Hero.");
            int alarmAt=PartySize<=1?2:1;if(!QuestRuntime0829.PrisonAlarmSpawned&&n>=alarmAt){QuestRuntime0829.PrisonAlarmSpawned=true;SpawnQuestEnemy0829("E011","L018","Prison alarm");AddLog("Q006 ALARM — Goblin Saboteur enters the Prison Pens.");}
            if(!QuestRuntime0829.PrisonReinforcementSpawned&&n>=3){QuestRuntime0829.PrisonReinforcementSpawned=true;SpawnQuestEnemy0829("E002","L018","third Prisoner freed");SpawnQuestEnemy0829("E001","L018","third Prisoner freed");if(PartySize>=4)SpawnQuestEnemy0829("E001","L018","4-Hero third-Prisoner scaling");AddLog("Q006 REINFORCEMENTS — third Prisoner freed; Archer and Raider reinforcements arrive.");}
            SyncFreedPrisoners0831();StateChanged?.Invoke();
        }
        private void OnQuestHeroMoved0831(HeroState hero){if(IsGenericQuestRuntime0856){OnGenericQuestHeroMoved0856(hero);return;}OnQuestHeroMoved0832(hero);if(hero!=null&&IsQ006Active0831)SyncFreedPrisoners0831();}
        private void SyncFreedPrisoners0831()
        {
            if(!IsQ006Active0831)return;foreach(var p in QuestRuntime0829.Objects.Where(o=>o.Type=="Prisoner"&&o.Resolved&&!o.Rescued&&!o.Destroyed).ToList())
            {if(string.IsNullOrEmpty(p.OwnerHeroId)||!Heroes.TryGetValue(p.OwnerHeroId,out var owner))continue;p.LocationId=owner.LocationId;if(owner.LocationId=="L000"){p.Rescued=true;QuestRuntime0829.PrisonersRescued++;AddLog($"Q006 RESCUE — {p.DisplayName} reaches Stonebrook with {owner.DisplayName} ({QuestRuntime0829.PrisonersRescued}/4 required).");}}
            EvaluateQuestOutcome0829();
        }
        private void ExecuteOnePrisoner0831(string source)
        {
            var p=QuestRuntime0829.Objects.Where(o=>o.Type=="Prisoner"&&!o.Resolved&&!o.Rescued&&!o.Destroyed).OrderBy(o=>o.ObjectId).FirstOrDefault();if(p==null)return;p.Destroyed=true;QuestRuntime0829.PrisonersExecuted++;AddLog($"Q006 EXECUTION — {p.DisplayName} is executed ({QuestRuntime0829.PrisonersExecuted}/4) [{source}].");EvaluateQuestOutcome0829();
        }

        private void SabotageSupplyWagon0831()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0831(hero)||!RequireHeroTurn(hero,"Sabotage Supply Wagon"))return;var wagon=QuestRuntime0829.Objects.Where(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued&&o.LocationId==hero.LocationId).OrderBy(o=>o.ObjectId).First();hero.Actions--;wagon.Supply=Mathf.Min(2,wagon.Supply+1);AddLog($"Q007 SABOTAGE — {hero.DisplayName} places Sabotage {wagon.Supply}/2 on {wagon.DisplayName}.");if(wagon.Supply>=2)DestroySupplyWagon0831(wagon);StateChanged?.Invoke();
        }
        private void DestroySupplyWagon0831(QuestObjectState0829 wagon)
        {
            if(wagon==null||wagon.Destroyed)return;wagon.Destroyed=true;QuestRuntime0829.SupplyWagonsDestroyed++;int n=QuestRuntime0829.SupplyWagonsDestroyed;AddLog($"Q007 OBJECTIVE — {wagon.DisplayName} destroyed ({n}/3).");var survivors=QuestRuntime0829.Objects.Where(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued).OrderByDescending(o=>GraphDistance("L000",o.LocationId)).ThenBy(o=>o.ObjectId).ToList();
            if(n==1&&!QuestRuntime0829.ConvoyFirstDestroyedWave){QuestRuntime0829.ConvoyFirstDestroyedWave=true;var target=survivors.FirstOrDefault();if(target!=null)SpawnQuestEnemy0829("E002",target.LocationId,"first Supply Wagon destroyed");}
            else if(n==2&&!QuestRuntime0829.ConvoySecondDestroyedWave){QuestRuntime0829.ConvoySecondDestroyedWave=true;var target=survivors.FirstOrDefault();if(target!=null){SpawnQuestEnemy0829("E013",target.LocationId,"second Supply Wagon destroyed");SpawnQuestEnemy0829("E002",target.LocationId,"second Supply Wagon destroyed");}}
            EvaluateQuestOutcome0829();
        }
        private string NextSupplyWagonStep0831(QuestObjectState0829 wagon)
        {
            if(wagon==null||!Locations.TryGetValue(wagon.LocationId,out var current))return null;int cur=GraphDistance("L000",wagon.LocationId);
            int exitNow=GraphDistance(wagon.LocationId,"L016");
            return current.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed&&IsLocationEligibleForCurrentQuest0829(id)&&GraphDistance("L000",id)>=cur&&GraphDistance(id,"L016")<exitNow).OrderBy(id=>GraphDistance(id,"L016")).ThenBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }
        private void MoveSupplyWagons0831()
        {
            foreach(var wagon in QuestRuntime0829.Objects.Where(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued).OrderBy(o=>o.ObjectId).ToList())
            {
                if(wagon.LocationId=="L016"){wagon.Rescued=true;QuestRuntime0829.SupplyWagonsEscaped++;AddLog($"Q007 ESCAPE — {wagon.DisplayName} escapes across Ancient Stone Bridge ({QuestRuntime0829.SupplyWagonsEscaped}/2 before failure).");continue;}
                string next=NextSupplyWagonStep0831(wagon);if(!string.IsNullOrEmpty(next)){string from=Locations[wagon.LocationId].Name;wagon.LocationId=next;AddLog($"Q007 CONVOY — {wagon.DisplayName} moves {from} → {Locations[next].Name} toward the exit path.");}
            }
        }
        private string NextQuestEnemyStep0831(EnemyState enemy)
        {
            string q832=NextQuestEnemyStep0832(enemy);if(!string.IsNullOrEmpty(q832))return q832;
            if(!IsQ007Active0831||enemy==null||enemy.TemplateId!="E013"||!Locations.TryGetValue(enemy.LocationId,out var current))return null;var wagons=QuestRuntime0829.Objects.Where(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued).ToList();if(wagons.Count==0)return null;int best=wagons.Min(w=>GraphDistance(enemy.LocationId,w.LocationId));return current.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed).Select(id=>new{Id=id,D=wagons.Min(w=>GraphDistance(id,w.LocationId))}).Where(x=>x.D<best).OrderBy(x=>x.D).ThenBy(x=>x.Id).Select(x=>x.Id).FirstOrDefault();
        }

        private void AttackSiegeEngine0831()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0831(hero)||!RequireHeroTurn(hero,"Attack Siege Engine"))return;var engine=QuestRuntime0829.Objects.Where(o=>o.Type=="SiegeEngine"&&!o.Destroyed&&o.LocationId==hero.LocationId).OrderBy(o=>o.ObjectId).First();hero.Actions--;BreakHiddenOnAttack(hero);int attack=EffectiveHeroAttack(hero);int damage=Mathf.Max(1,attack-engine.Armor);engine.Health=Mathf.Max(0,engine.Health-damage);AddLog($"Q008 ATTACK — {hero.DisplayName} attacks {engine.DisplayName}: Attack {attack} · Armor {engine.Armor} · Damage {damage} ({engine.Health}/{engine.MaxHealth} Health).");if(engine.Health<=0)DestroySiegeEngine0831(engine);StateChanged?.Invoke();
        }
        private void DestroySiegeEngine0831(QuestObjectState0829 engine)
        {
            if(engine==null||engine.Destroyed)return;engine.Destroyed=true;QuestRuntime0829.SiegeEnginesDestroyed++;AddLog($"Q008 OBJECTIVE — {engine.DisplayName} destroyed ({QuestRuntime0829.SiegeEnginesDestroyed}/{Q008RequiredEngines0831}).");if(QuestRuntime0829.SiegeEnginesDestroyed>=Q008RequiredEngines0831&&!QuestRuntime0829.ChampionSpawned){QuestRuntime0829.ChampionSpawned=true;string loc=NearestRevealedGoblinStructure0831(engine.LocationId);SpawnQuestEnemy0829("E015",loc,"final Siege Engine destroyed");AddLog($"Q008 FINAL DEFENDER — Goblin Champion enters {Locations[loc].Name}.");}EvaluateQuestOutcome0829();
        }
        private string NearestRevealedGoblinStructure0831(string from)
        {
            return Locations.Values.Where(l=>l.Revealed&&IsQ008SiegeLocation0831(l)).OrderBy(l=>GraphDistance(from,l.Id)).ThenBy(l=>l.Id).Select(l=>l.Id).FirstOrDefault()??from;
        }

        private bool ResolveQuestEndEnemyPhase0831()
        {
            if(ResolveQuestEndEnemyPhase0832())return true;
            if(IsQ006Active0831)
            {
                SyncFreedPrisoners0831();if(!ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId=="L018")&&QuestRuntime0829.Objects.Any(o=>o.Type=="Prisoner"&&!o.Resolved&&!o.Rescued&&!o.Destroyed)){int roll=UnityEngine.Random.Range(1,7);AddLog($"Q006 EXECUTION CHECK — d6 = {roll}.");if(roll==6)ExecuteOnePrisoner0831("Enemy Phase roll");}return true;
            }
            if(IsQ007Active0831)
            {
                MoveSupplyWagons0831();EvaluateQuestOutcome0829();if(QuestFailed||QuestComplete)return true;var active=QuestRuntime0829.Objects.Where(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued).ToList();if(active.Count==0)return true;if(PartySize<=1&&Round%2!=0)return true;var far=active.OrderByDescending(o=>GraphDistance("L000",o.LocationId)).ThenBy(o=>o.ObjectId).First();int count=PartySize>=4?2:1;for(int i=0;i<count;i++)SpawnQuestEnemy0829("E001",far.LocationId,"Convoy Reinforcement");QuestRuntime0829.ReinforcementCount++;AddLog($"Q007 REINFORCEMENTS — {count} Raider{(count==1?"":"s")} reinforce {far.DisplayName} after convoy movement.");return true;
            }
            if(IsQ008Active0831)
            {
                var engines=QuestRuntime0829.Objects.Where(o=>o.Type=="SiegeEngine"&&!o.Destroyed).ToList();if(engines.Count==0)return true;QuestRuntime0829.WallTrack=Mathf.Max(0,QuestRuntime0829.WallTrack-engines.Count);AddLog($"Q008 BOMBARDMENT — {engines.Count} active Siege Engine{(engines.Count==1?"":"s")} remove {engines.Count} Wall. Stonebrook Wall: {QuestRuntime0829.WallTrack}/6.");
                if(QuestRuntime0829.WallTrack<=4&&!QuestRuntime0829.Wall4Triggered){QuestRuntime0829.Wall4Triggered=true;SpawnQuestEnemy0829("E002",ChooseSiegeReinforcementLocation0831(engines),"Wall Track 4");}
                if(QuestRuntime0829.WallTrack<=2&&!QuestRuntime0829.Wall2Triggered){QuestRuntime0829.Wall2Triggered=true;SpawnQuestEnemy0829("E002",ChooseSiegeReinforcementLocation0831(engines),"Wall Track 2");}
                EvaluateQuestOutcome0829();if(QuestFailed)return true;if(PartySize<=1&&Round%2!=0)return true;int count=PartySize>=4&&engines.Count>=2?2:1;for(int i=0;i<count;i++)SpawnQuestEnemy0829("E001",ChooseSiegeReinforcementLocation0831(engines),"Siege Reinforcement");QuestRuntime0829.ReinforcementCount++;return true;
            }
            return false;
        }
        private string ChooseSiegeReinforcementLocation0831(List<QuestObjectState0829> engines)
        {
            return engines.OrderBy(e=>Enemies.Values.Count(x=>!x.Defeated&&x.LocationId==e.LocationId&&IsGoblinEnemy(x))).ThenBy(e=>e.ObjectId).Select(e=>e.LocationId).FirstOrDefault()??"L014";
        }
        private int QuestEnemyAttackBonus0831(EnemyState enemy)
        {
            int bonus=QuestEnemyAttackBonus0832(enemy);
            if(enemy!=null&&enemy.QuestSpawned&&(CurrentQuestId0829=="Q007"||CurrentQuestId0829=="Q008"||CurrentQuestId0829=="Q009")&&enemy.TemplateId=="E002")bonus+=1;
            return bonus;
        }

        private void OnQuestEnemyDefeated0831(EnemyState enemy,HeroState killer)
        {
            OnQuestEnemyDefeated0832(enemy,killer);
            if(enemy==null||!enemy.QuestSpawned||enemy.QuestId!=CurrentQuestId0829)return;if(IsQ006Active0831&&enemy.TemplateId=="E006"){QuestRuntime0829.CaptainDefeated=true;AddLog("Q006 OBJECTIVE — Goblin Captain guarding the Pens defeated.");EvaluateQuestOutcome0829();}if(IsQ008Active0831&&enemy.TemplateId=="E015"){QuestRuntime0829.ChampionDefeated=true;AddLog("Q008 OBJECTIVE — Goblin Champion defeated.");EvaluateQuestOutcome0829();}
        }
        private bool EvaluateQuestOutcome0831()
        {
            if(EvaluateQuestOutcome0832())return true;
            if(CurrentQuestId0829=="Q006")
            {
                if(QuestRuntime0829.PrisonersExecuted>=4){QuestFailed=true;AddLog("Q006 FAILED — 4 Prisoners have been executed.");return true;}if(QuestRuntime0829.PrisonersRescued>=4&&QuestRuntime0829.CaptainDefeated){QuestComplete=true;AddLog("Q006 COMPLETE — at least 4 Prisoners rescued and the Goblin Captain defeated.");ResolveQuestReward0829();}return true;
            }
            if(CurrentQuestId0829=="Q007")
            {
                if(QuestRuntime0829.SupplyWagonsEscaped>=2){QuestFailed=true;AddLog("Q007 FAILED — 2 Supply Wagons escaped the Heroes.");return true;}if(QuestRuntime0829.SupplyWagonsDestroyed>=3){QuestComplete=true;AddLog("Q007 COMPLETE — all 3 Supply Wagons destroyed.");ResolveQuestReward0829();}return true;
            }
            if(CurrentQuestId0829=="Q008")
            {
                if(QuestRuntime0829.WallTrack<=0){QuestFailed=true;AddLog("Q008 FAILED — Stonebrook Wall Track reaches 0.");return true;}if(QuestRuntime0829.SiegeEnginesDestroyed>=Q008RequiredEngines0831&&QuestRuntime0829.ChampionDefeated){QuestComplete=true;AddLog("Q008 COMPLETE — all active Siege Engines destroyed and the Goblin Champion defeated.");ResolveQuestReward0829();}return true;
            }
            return false;
        }
        private bool ResolveQuestReward0831(HeroState receiver)
        {
            if(ResolveQuestReward0832(receiver))return true;
            if(CurrentQuestId0829=="Q006")
            {foreach(var h in ParticipatingHeroes)h.XP+=100;AddPartyGold(75,"Q006 victory reward");AddGeneralReputation(2,"Q006 victory reward");PartyMorale0831+=1;AddLog("Q006 REWARD — 100 XP each • 75 Gold • +2 General Reputation • +1 Party Morale for WE003.");return true;}
            if(CurrentQuestId0829=="Q007")
            {foreach(var h in ParticipatingHeroes)h.XP+=175;AddPartyGold(150,"Q007 victory reward");if(receiver!=null)DrawRareEquipment0830(receiver,"Q007 victory reward");RemoveSiegeEngineFromQ008Setup0831=true;AddLog("Q007 REWARD — 175 XP each • 150 Gold • 1 Rare Equipment • remove 1 Siege Engine from Q008 banked.");return true;}
            if(CurrentQuestId0829=="Q008")
            {foreach(var h in ParticipatingHeroes)h.XP+=200;AddPartyGold(175,"Q008 victory reward");if(receiver!=null)DrawEpicEquipment0831(receiver,"Q008 victory reward");AddLog("Q008 REWARD — 200 XP each • 175 Gold • 1 Epic Equipment.");return true;}
            return false;
        }
        private string DrawEpicEquipment0831(HeroState hero,string source)
        {
            if(hero==null)return null;int at=EquipmentDeck.FindIndex(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&string.Equals(e.Rarity,"Epic",StringComparison.OrdinalIgnoreCase));if(at<0){AddLog($"EQUIPMENT — no Epic Equipment remains in the live deck ({source}).");return null;}string id=EquipmentDeck[at];EquipmentDeck.RemoveAt(at);hero.AcquiredEquipment.Add(id);AddLog($"EQUIPMENT — {hero.DisplayName} gains {RewardContent0826.EquipmentLabel(id)} unequipped ({source}).");return id;
        }

        // 0.8.31 deterministic Play Mode hooks.
        public void DebugSetRemoveEliteFromQ007Setup0831(bool value)=>RemoveEliteFromQ007Setup0830=value;
        public int DebugPrisonersFreed0831=>QuestRuntime0829?.PrisonersFreed??0;
        public int DebugPrisonersRescued0831=>QuestRuntime0829?.PrisonersRescued??0;
        public int DebugPrisonersExecuted0831=>QuestRuntime0829?.PrisonersExecuted??0;
        public void DebugForcePrisonExecution0831()=>ExecuteOnePrisoner0831("validator");
        public void DebugSyncPrisoners0831()=>SyncFreedPrisoners0831();
        public int DebugSupplyWagonsDestroyed0831=>QuestRuntime0829?.SupplyWagonsDestroyed??0;
        public int DebugSupplyWagonsEscaped0831=>QuestRuntime0829?.SupplyWagonsEscaped??0;
        public void DebugSetSupplyWagonLocation0831(int index,string location){var w=QuestRuntime0829?.Objects?.Where(o=>o.Type=="SupplyWagon").OrderBy(o=>o.ObjectId).ElementAtOrDefault(index);if(w!=null&&Locations.ContainsKey(location))w.LocationId=location;}
        public int DebugWallTrack0831=>QuestRuntime0829?.WallTrack??-1;
        public int DebugSiegeEnginesPlaced0831=>QuestRuntime0829?.SiegeEnginesPlaced??0;
        public int DebugSiegeEnginesDestroyed0831=>QuestRuntime0829?.SiegeEnginesDestroyed??0;
        public int DebugActiveSiegeEngineHealth0831=>QuestRuntime0829?.Objects?.Where(o=>o.Type=="SiegeEngine"&&!o.Destroyed).OrderBy(o=>o.ObjectId).FirstOrDefault()?.Health??-1;
        public void DebugSetActiveSiegeEngineHealth0831(int hp){var e=QuestRuntime0829?.Objects?.Where(o=>o.Type=="SiegeEngine"&&!o.Destroyed).OrderBy(o=>o.ObjectId).FirstOrDefault();if(e!=null)e.Health=Mathf.Clamp(hp,1,e.MaxHealth);}
        public int DebugQuestEnemyAttack0831(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?EffectiveEnemyAttack(e):-1;
    }
}
