using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.30 — Campaign I Quest Batch 1: Q003 Clear the Abandoned Watchtower,
    // Q004 Into the Dark Forest, Q005 Destroy the War Camp.
    public sealed partial class AscendanceGameManager
    {
        private bool IsQ003Active0830 => CurrentQuestId0829=="Q003"&&!QuestComplete&&!QuestFailed;
        private bool IsQ004Active0830 => CurrentQuestId0829=="Q004"&&!QuestComplete&&!QuestFailed;
        private bool IsQ005Active0830 => CurrentQuestId0829=="Q005"&&!QuestComplete&&!QuestFailed;

        private string NextQuestId0830()
        {
            if(CurrentQuestId0829=="Q001")return "Q002";
            if(CurrentQuestId0829=="Q002")return "Q003";
            if(CurrentQuestId0829=="Q003")return "Q004";
            if(CurrentQuestId0829=="Q004")return "Q005";
            if(CurrentQuestId0829=="Q005")return "Q006";
            if(CurrentQuestId0829=="Q006")return "Q007";
            if(CurrentQuestId0829=="Q007")return "Q008";
            if(CurrentQuestId0829=="Q008")return "Q009";
            if(CurrentQuestId0829=="Q009")return "Q010";
            if(CurrentQuestId0829=="Q010")return "Q011";
            if(CurrentQuestId0829=="Q011")return "Q012";
            return null;
        }

        private string QuestObjectiveSummary0830()
        {
            string q831=QuestObjectiveSummary0831();if(q831!=null)return q831;
            if(CurrentQuestId0829=="Q002")
            {
                var wagons=QuestRuntime0829?.Objects?.Where(o=>o.Type=="Wagon").ToList()??new List<QuestObjectState0829>();
                int rescued=wagons.Count(w=>w.Rescued),destroyed=wagons.Count(w=>w.Destroyed);
                string captain=QuestRuntime0829?.CaptainDefeated==true?"Defeated":QuestRuntime0829?.CaptainSpawned==true?"In Play":"Not Yet In Play";
                return $"QUEST Q002 — RESCUE THE MERCHANT CARAVAN\n\n{(rescued>=2?"[OK]":"[ ]")} Wagons rescued {rescued}/2\n{(QuestRuntime0829!=null&&QuestRuntime0829.DeliveredSupply>=4?"[OK]":"[ ]")} Supply delivered {QuestRuntime0829?.DeliveredSupply??0}/4\nWagons destroyed {destroyed}/2\nReinforcement wave {QuestRuntime0829?.ReinforcementCount??0}/3\nCaptain: {captain}\nManifest: {(QuestRuntime0829?.ManifestGranted==true?"Claimed":"Not claimed")} • Strongbox: {(QuestRuntime0829?.StrongboxOpened==true?"Opened":QuestRuntime0829?.StrongboxKeyGranted==true?"Key acquired":"Locked")}";
            }
            if(CurrentQuestId0829=="Q003")
            {
                bool clear=QuestRuntime0829?.ObjectiveLocationRevealed==true&&!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId=="L008");
                return $"QUEST Q003 — CLEAR THE ABANDONED WATCHTOWER\n\n{(QuestRuntime0829?.ObjectiveLocationRevealed==true?"[OK]":"[ ]")} Fallen Watchtower revealed\n{(QuestRuntime0829?.BeaconLit==true?"[OK]":"[ ]")} Beacon lit\n{(clear?"[OK]":"[ ]")} Watchtower enemies cleared\nEmpty-watchtower clock {QuestRuntime0829?.ObjectiveEmptyRounds??0}/3\nReinforcements {QuestRuntime0829?.ReinforcementCount??0}\nVault: {(QuestRuntime0829?.VaultOpened==true?"Opened":QuestRuntime0829?.VaultKeyGranted==true?"Key acquired":"Locked")}";
            }
            if(CurrentQuestId0829=="Q004")
            {
                return $"QUEST Q004 — INTO THE DARK FOREST\n\n{(QuestRuntime0829?.TrailMarkers>=3?"[OK]":"[ ]")} Trail Markers {QuestRuntime0829?.TrailMarkers??0}/3\n{(QuestRuntime0829?.WarCampRevealed==true?"[OK]":"[ ]")} Goblin War Camp revealed\n{(QuestRuntime0829?.WarChiefDefeated==true?"[OK]":"[ ]")} Goblin War Chief defeated\nScouts: {(QuestRuntime0829?.WarCampRevealed==true?"normal Movement":"+1 Movement while War Camp is unrevealed")}";
            }
            if(CurrentQuestId0829=="Q005")
            {
                int remaining=QuestRuntime0829?.Objects?.Count(o=>o.Type=="SupplyCrate"&&!o.Destroyed)??0;
                return $"QUEST Q005 — DESTROY THE WAR CAMP\n\n{(QuestRuntime0829?.WarCampRevealed==true?"[OK]":"[ ]")} Goblin War Camp revealed\n{(QuestRuntime0829?.SupplyCratesDestroyed>=4?"[OK]":"[ ]")} Supply Crates destroyed {QuestRuntime0829?.SupplyCratesDestroyed??0}/4\nAlarm Tokens {QuestRuntime0829?.AlarmTokens??0}/2\nCrates remaining {remaining}\n{(QuestRuntime0829?.WarChiefDefeated==true?"[OK]":"[ ]")} Goblin War Chief defeated";
            }
            bool cap=QuestRuntime0829?.CaptainDefeated==true;
            return $"QUEST Q001 — DEFEND STONEBROOK\n\n{(VillagersRescued>=2?"[OK]":"[ ]")} Rescue 2 Villagers ({VillagersRescued}/4)\n{(FireTokens<5?"[OK]":"[ ]")} Fire below 5 ({FireTokens}/5)\n{(cap?"[OK]":"[ ]")} Defeat Goblin Captain";
        }

        private string QuestPrimaryActionLabel0830()
        {
            string q831=QuestPrimaryActionLabel0831();if(q831!=null)return q831;
            if(CurrentQuestId0829=="Q002")return "Move Wagon";
            if(CurrentQuestId0829=="Q003")return "Light Beacon";
            if(CurrentQuestId0829=="Q005")return "Destroy Crate";
            if(CurrentQuestId0829=="Q004")return "Trail Objective";
            return "Rescue";
        }
        private string QuestSecondaryActionLabel0830()
        {
            string q831=QuestSecondaryActionLabel0831();if(q831!=null)return q831;
            if(CurrentQuestId0829=="Q002")return "Open Strongbox";
            if(CurrentQuestId0829=="Q003")return "Open Armory";
            if(CurrentQuestId0829=="Q004"||CurrentQuestId0829=="Q005")return "Quest Action";
            return "Extinguish";
        }
        private bool CanQuestPrimaryAction0830(HeroState hero)
        {
            if(IsQuestBatch20831||IsQuestBatchFinal0832)return CanQuestPrimaryAction0831(hero);
            if(hero==null||!CanUseTurnActions(hero))return false;
            if(CurrentQuestId0829=="Q002")return GeneralActions(hero)>=1&&QuestObjects0829.Any(w=>w.Type=="Wagon"&&!w.Rescued&&!w.Destroyed&&w.LocationId==hero.LocationId);
            if(CurrentQuestId0829=="Q003"){int cost=hero.HasStatus("Beacon Instructions")?1:2;return GeneralActions(hero)>=cost&&hero.LocationId=="L008"&&QuestRuntime0829.ObjectiveLocationRevealed&&!QuestRuntime0829.BeaconLit;}
            if(CurrentQuestId0829=="Q005")return GeneralActions(hero)>=1&&hero.LocationId=="L014"&&QuestRuntime0829.WarCampRevealed&&QuestRuntime0829.Objects.Any(o=>o.Type=="SupplyCrate"&&!o.Destroyed);
            if(CurrentQuestId0829=="Q004")return false;
            return GeneralActions(hero)>=1&&hero.LocationId=="L000"&&VillagersRescued<4;
        }
        private bool CanQuestSecondaryAction0830(HeroState hero)
        {
            if(IsQuestBatch20831||IsQuestBatchFinal0832)return CanQuestSecondaryAction0831(hero);
            if(hero==null||!CanUseTurnActions(hero)||GeneralActions(hero)<1)return false;
            if(CurrentQuestId0829=="Q002")return hero.LocationId=="L005"&&!QuestRuntime0829.StrongboxOpened&&hero.LootInventory.Contains("LT119");
            if(CurrentQuestId0829=="Q003")return hero.LocationId=="L008"&&!QuestRuntime0829.VaultOpened&&hero.LootInventory.Contains("LT118");
            if(CurrentQuestId0829=="Q004"||CurrentQuestId0829=="Q005")return false;
            return hero.LocationId=="L000"&&FireTokens>0;
        }
        private void QuestPrimaryAction0830()
        {
            if(IsQuestBatch20831||IsQuestBatchFinal0832){QuestPrimaryAction0831();return;}
            if(CurrentQuestId0829=="Q002"){BeginWagonMove0829();return;}
            if(CurrentQuestId0829=="Q003"){LightBeacon0830();return;}
            if(CurrentQuestId0829=="Q005"){DestroySupplyCrate0830();return;}
            if(CurrentQuestId0829=="Q004")return;
            RescueVillager();
        }
        private void QuestSecondaryAction0830()
        {
            if(IsQuestBatch20831||IsQuestBatchFinal0832){QuestSecondaryAction0831();return;}
            if(CurrentQuestId0829=="Q002"){OpenStrongbox0829();return;}
            if(CurrentQuestId0829=="Q003"){OpenWatchtowerArmory0830();return;}
            if(CurrentQuestId0829=="Q004"||CurrentQuestId0829=="Q005")return;
            ExtinguishFire();
        }

        private int QuestEnemyBaseMovement0830(EnemyState enemy)
        {
            if(enemy==null)return 0;
            int movement=AuthoredEnemyMovement0859(enemy);
            if(enemy.QuestSpawned&&CurrentQuestId0829=="Q003"&&QuestRuntime0829?.BeaconLit==true&&enemy.TemplateId=="E004")movement--;
            if(enemy.QuestSpawned&&CurrentQuestId0829=="Q004"&&QuestRuntime0829?.WarCampRevealed==false&&enemy.TemplateId=="E004")movement++;
            return Mathf.Max(0,movement);
        }
        private int EnemyAttackRangeConnections0830(EnemyState enemy)
        {
            if(enemy==null)return 0;
            int range=EnemyConditionalRange0859(enemy,AuthoredEnemyRange0859(enemy));
            if(enemy.QuestSpawned&&Locations.TryGetValue(enemy.LocationId??string.Empty,out var loc)&&loc.OngoingEffect?.IndexOf("gain +1 Range",StringComparison.OrdinalIgnoreCase)>=0)range++;
            if(enemy.QuestSpawned&&CurrentQuestId0829=="Q003"&&QuestRuntime0829?.BeaconLit==false&&enemy.TemplateId=="E004")range++;
            if(Locations.TryGetValue(enemy.LocationId??string.Empty,out var fog)&&fog.OngoingEffect?.IndexOf("ranged attacks may only target current or adjacent Location",StringComparison.OrdinalIgnoreCase)>=0)range=Mathf.Min(range,1);
            return Mathf.Max(0,range);
        }

        private void OnQuestLocationRevealed0830(LocationState location,HeroState hero)
        {
            if(OnQuestLocationRevealed0831(location,hero))return;
            if(location==null||QuestRuntime0829==null)return;
            if(IsQ003Active0830&&location.Id=="L008"&&!QuestRuntime0829.ObjectiveLocationRevealed)
            {
                QuestRuntime0829.ObjectiveLocationRevealed=true;QuestRuntime0829.Objects.Clear();
                var beacon=new QuestObjectState0829{ObjectId="Q003_BEACON",QuestId="Q003",Type="Beacon",DisplayName="Beacon",LocationId="L008"};QuestRuntime0829.Objects.Add(beacon);QuestObjectAvatar0829.Create(beacon);
                int scouts=PartySize<=1?1:PartySize>=3?3:2;for(int i=0;i<scouts;i++)SpawnQuestEnemy0829("E004","L008","Fallen Watchtower revealed");SpawnQuestEnemy0829("E002","L008","Fallen Watchtower revealed");if(PartySize>=4)SpawnQuestEnemy0829("E001","L008","4-Hero reveal scaling");
                AddLog("Q003 SETUP — Fallen Watchtower revealed; Beacon placed and occupying Goblins deployed.");EvaluateQuestOutcome0829();return;
            }
            if(IsQ004Active0830)
            {
                if(location.Id!="L000"&&!QuestRuntime0829.InitialPatrolSpawned)
                {
                    QuestRuntime0829.InitialPatrolSpawned=true;int patrolScouts=RemoveScoutFromQ004Setup0833?1:2;for(int i=0;i<patrolScouts;i++)SpawnQuestEnemy0829("E004",location.Id,"first revealed non-Stonebrook Location");if(AddRaiderToQ004FirstEncounter0833)SpawnQuestEnemy0829("E001",location.Id,"WE001 incomplete chapter penalty");AddLog($"Q004 SETUP — {patrolScouts} Goblin Scout{(patrolScouts==1?string.Empty:"s")} establish the first patrol at {location.Name}."+(AddRaiderToQ004FirstEncounter0833?" WE001 penalty adds 1 Goblin Raider.":string.Empty));
                }
                if(!QuestRuntime0829.WarCampRevealed&&location.HasPrintedTag("Forest")&&QuestRuntime0829.TrailMarkers<3)
                {
                    QuestRuntime0829.TrailMarkers++;int m=QuestRuntime0829.TrailMarkers;AddLog($"Q004 TRAIL — Forest Location revealed; Trail Marker {m}/3 placed.");
                    if(m==1){if(PartySize>1)SpawnQuestEnemy0829("E001",location.Id,"Trail Marker 1 patrol");}
                    else if(m==2){SpawnQuestEnemy0829("E002",location.Id,"Trail Marker 2 patrol");SpawnQuestEnemy0829("E001",location.Id,"Trail Marker 2 patrol");}
                    else if(m==3){SpawnQuestEnemy0829("E013",location.Id,"Trail Marker 3 patrol");SpawnQuestEnemy0829("E004",location.Id,"Trail Marker 3 patrol");MoveExplorationLocationIntoTop0830("L014",3);}
                }
                if(location.Id=="L014"&&!QuestRuntime0829.WarCampRevealed)RevealWarCampQ0040830();
                if(!QuestRuntime0829.WarCampRevealed&&ExplorationDeck0828.Count==0){QuestFailed=true;AddLog("Q004 FAILED — Exploration deck exhausted before Goblin War Camp was revealed.");}
                EvaluateQuestOutcome0829();return;
            }
            if(IsQ005Active0830&&location.Id=="L014"&&!QuestRuntime0829.WarCampRevealed){RevealWarCampQ0050830();EvaluateQuestOutcome0829();}
        }

        private void MoveExplorationLocationIntoTop0830(string locationId,int topCount)
        {
            int at=ExplorationDeck0828.IndexOf(locationId);if(at<0)return;ExplorationDeck0828.RemoveAt(at);int max=Mathf.Min(Mathf.Max(1,topCount),ExplorationDeck0828.Count+1);int insert=UnityEngine.Random.Range(0,max);ExplorationDeck0828.Insert(insert,locationId);AddLog($"Q004 TRAIL — {Locations[locationId].Name} is shuffled into the top {topCount} Exploration cards (position {insert+1}).");
        }
        private void RevealWarCampQ0040830()
        {
            QuestRuntime0829.WarCampRevealed=true;
            SpawnQuestEnemy0829("E010","L014","Goblin War Camp revealed");SpawnQuestEnemy0829("E013","L014","Goblin War Camp revealed");
            int raiders=PartySize<=1?1:PartySize>=3?3:2;int archers=PartySize>=4?2:1;for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L014","Goblin War Camp revealed");for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002","L014","Goblin War Camp revealed");
            AddLog("Q004 WAR CAMP — Goblin War Chief and defenders enter play; Scout movement bonus ends.");
        }
        private void RevealWarCampQ0050830()
        {
            QuestRuntime0829.WarCampRevealed=true;QuestRuntime0829.Objects.Clear();
            for(int i=1;i<=4;i++){var crate=new QuestObjectState0829{ObjectId=$"Q005_CRATE_{i}",QuestId="Q005",Type="SupplyCrate",DisplayName=$"Supply Crate {i}",LocationId="L014"};QuestRuntime0829.Objects.Add(crate);QuestObjectAvatar0829.Create(crate);}
            int raiders=PartySize<=1?1:PartySize>=3?3:2;int archers=PartySize>=4?2:1;for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L014","War Camp revealed");for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002","L014","War Camp revealed");
            AddLog("Q005 SETUP — Goblin War Camp revealed; 4 Supply Crates and opening defenders placed.");
        }

        private bool ResolveQuestEndEnemyPhase0830()
        {
            if(ResolveQuestEndEnemyPhase0831())return true;
            if(IsQ003Active0830)
            {
                if(!QuestRuntime0829.ObjectiveLocationRevealed)return true;
                bool occupied=ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId=="L008");QuestRuntime0829.ObjectiveEmptyRounds=occupied?0:QuestRuntime0829.ObjectiveEmptyRounds+1;
                if(!occupied)AddLog($"Q003 LOSS CLOCK — Fallen Watchtower is unoccupied ({QuestRuntime0829.ObjectiveEmptyRounds}/3 consecutive rounds).");
                if(QuestRuntime0829.ObjectiveEmptyRounds>=3){QuestFailed=true;AddLog("Q003 FAILED — Fallen Watchtower was left unoccupied for 3 consecutive rounds.");return true;}
                if(QuestRuntime0829.BeaconLit)return true;if(PartySize<=1&&Round%2!=0)return true;
                QuestRuntime0829.ReinforcementCount++;SpawnQuestEnemy0829("E004","L008",$"Watchtower reinforcement {QuestRuntime0829.ReinforcementCount}");
                if(QuestRuntime0829.ReinforcementCount==1){SpawnQuestEnemy0829("E001","L008","Watchtower reinforcement 1");if(PartySize>=4)SpawnQuestEnemy0829("E004","L008","4-Hero first-reinforcement scaling");}
                else if(QuestRuntime0829.ReinforcementCount==2)SpawnQuestEnemy0829("E002","L008","Watchtower reinforcement 2");
                else if(QuestRuntime0829.ReinforcementCount==3)SpawnQuestEnemy0829("E001","L008","Watchtower reinforcement 3");
                return true;
            }
            if(IsQ004Active0830)return true;
            if(IsQ005Active0830)
            {
                if(!QuestRuntime0829.WarCampRevealed||QuestRuntime0829.Objects.All(o=>o.Type!="SupplyCrate"||o.Destroyed))return true;if(PartySize<=1&&Round%2!=0)return true;
                int count=PartySize>=4?2:1;for(int i=0;i<count;i++)SpawnQuestEnemy0829("E001","L014","Camp reinforcement");QuestRuntime0829.ReinforcementCount++;AddLog($"Q005 REINFORCEMENTS — {count} Goblin Raider{(count==1?"":"s")} reinforce the War Camp while Supply Crates remain.");return true;
            }
            return false;
        }

        private void OnQuestEnemyDefeated0830(EnemyState enemy,HeroState killer)
        {
            OnQuestEnemyDefeated0831(enemy,killer);
            if(enemy==null||!enemy.QuestSpawned||enemy.QuestId!=CurrentQuestId0829)return;
            if(IsQ003Active0830&&enemy.TemplateId=="E001"&&!QuestRuntime0829.VaultKeyGranted){QuestRuntime0829.VaultKeyGranted=true;GrantSpecificLootQuestItem0829(killer??SelectedHero,"LT118","Watchtower Goblin Raider defeated");}
            if((IsQ004Active0830||IsQ005Active0830)&&enemy.TemplateId=="E010"){QuestRuntime0829.WarChiefDefeated=true;AddLog($"{CurrentQuestId0829} OBJECTIVE — Goblin War Chief defeated.");}
            EvaluateQuestOutcome0829();
        }

        private void LightBeacon0830()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0830(hero)||!RequireHeroTurn(hero,"Light Beacon"))return;int cost=hero.HasStatus("Beacon Instructions")?1:2;hero.Actions-=cost;if(cost==1)RemoveStatus(hero.Statuses,"Beacon Instructions",hero.DisplayName,"Beacon discount used");QuestRuntime0829.BeaconLit=true;var beacon=QuestRuntime0829.Objects.FirstOrDefault(o=>o.Type=="Beacon");if(beacon!=null)beacon.Resolved=true;AddLog($"Q003 OBJECTIVE — {hero.DisplayName} spends {cost} Action{(cost==1?string.Empty:"s")} and lights the Beacon. Watchtower reinforcements stop; Goblin Scouts lose 1 Movement.");EvaluateQuestOutcome0829();StateChanged?.Invoke();
        }
        private void OpenWatchtowerArmory0830()
        {
            var hero=SelectedHero;if(!CanQuestSecondaryAction0830(hero)||!RequireHeroTurn(hero,"Open Watchtower Armory"))return;hero.Actions--;hero.LootInventory.Remove("LT118");if(!LootDeck.Contains("LT118")&&!LootDiscard.Contains("LT118"))LootDeck.Add("LT118");QuestRuntime0829.VaultOpened=true;DrawEquipmentToHero(hero,"Q003 sealed armory");AddLog($"Q003 ARMORY — {hero.DisplayName} returns LT118 and opens the sealed armory.");StateChanged?.Invoke();
        }
        private void DestroySupplyCrate0830()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0830(hero)||!RequireHeroTurn(hero,"Destroy Supply Crate"))return;var crate=QuestRuntime0829.Objects.Where(o=>o.Type=="SupplyCrate"&&!o.Destroyed).OrderBy(o=>o.ObjectId).FirstOrDefault();if(crate==null)return;hero.Actions--;crate.Destroyed=true;QuestRuntime0829.SupplyCratesDestroyed++;int n=QuestRuntime0829.SupplyCratesDestroyed;AddLog($"Q005 OBJECTIVE — {hero.DisplayName} destroys {crate.DisplayName} ({n}/4).");
            if(n==2){QuestRuntime0829.AlarmTokens=1;SpawnQuestEnemy0829("E002","L014","First Alarm");SpawnQuestEnemy0829("E013","L014","First Alarm");AddLog("Q005 FIRST ALARM — Goblin Archer + Goblin Wolf Rider enter play.");}
            else if(n==3){QuestRuntime0829.AlarmTokens=2;SpawnQuestEnemy0829("E010","L014","Second Alarm");SpawnQuestEnemy0829("E016","L014","Second Alarm");SpawnQuestEnemy0829("E001","L014","Second Alarm");AddLog("Q005 SECOND ALARM — Goblin War Chief, Siege Engineer, and Raider enter play.");}
            else if(n==4){SpawnQuestEnemy0829("E013","L014","Final Defense");SpawnQuestEnemy0829("E002","L014","Final Defense");AddLog("Q005 FINAL DEFENSE — Wolf Rider + Archer enter play; Camp Reinforcements stop.");}
            EvaluateQuestOutcome0829();StateChanged?.Invoke();
        }

        private bool EvaluateQuestOutcome0830()
        {
            if(EvaluateQuestOutcome0831())return true;
            if(CurrentQuestId0829=="Q003")
            {
                if(QuestRuntime0829.BeaconLit&&QuestRuntime0829.ObjectiveLocationRevealed&&!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId=="L008")){QuestComplete=true;AddLog("Q003 COMPLETE — Beacon lit and Fallen Watchtower cleared.");ResolveQuestReward0829();}return true;
            }
            if(CurrentQuestId0829=="Q004")
            {
                if(!QuestRuntime0829.WarCampRevealed&&ExplorationDeck0828.Count==0){QuestFailed=true;AddLog("Q004 FAILED — Exploration deck exhausted before Goblin War Camp was revealed.");return true;}
                if(QuestRuntime0829.WarCampRevealed&&QuestRuntime0829.WarChiefDefeated){QuestComplete=true;AddLog("Q004 COMPLETE — Goblin War Camp revealed and Goblin War Chief defeated.");ResolveQuestReward0829();}return true;
            }
            if(CurrentQuestId0829=="Q005")
            {
                if(QuestRuntime0829.SupplyCratesDestroyed>=4&&QuestRuntime0829.WarChiefDefeated){QuestComplete=true;AddLog("Q005 COMPLETE — all 4 Supply Crates destroyed and Goblin War Chief defeated.");ResolveQuestReward0829();}return true;
            }
            return false;
        }

        private bool ResolveQuestReward0830(HeroState receiver)
        {
            if(ResolveQuestReward0831(receiver))return true;
            if(CurrentQuestId0829=="Q003")
            {
                foreach(var h in ParticipatingHeroes)h.XP+=75;AddPartyGold(50,"Q003 victory reward");AddGeneralReputation(1,"Q003 victory reward");ReduceFirstWE002TravelScout0830=true;ReturnQuestLootToDeck0829("LT118","Q003 ended");AddLog("Q003 REWARD — 75 XP each • 50 Gold • +1 General Reputation • WE002 first Travel Encounter Scout reduction banked.");return true;
            }
            if(CurrentQuestId0829=="Q004")
            {
                foreach(var h in ParticipatingHeroes)h.XP+=150;AddPartyGold(125,"Q004 victory reward");if(receiver!=null)DrawRareEquipment0830(receiver,"Q004 victory reward");AddLog("Q004 REWARD — 150 XP each • 125 Gold • 1 Rare Equipment.");return true;
            }
            if(CurrentQuestId0829=="Q005")
            {
                foreach(var h in ParticipatingHeroes)h.XP+=175;AddPartyGold(150,"Q005 victory reward");if(receiver!=null)DrawRareEquipment0830(receiver,"Q005 victory reward");RemoveEliteFromQ007Setup0830=true;AddLog("Q005 REWARD — 175 XP each • 150 Gold • 1 Rare Equipment • remove 1 Elite Goblin from Q007 setup banked.");return true;
            }
            return false;
        }
        private string DrawRareEquipment0830(HeroState hero,string source)
        {
            if(hero==null)return null;int at=EquipmentDeck.FindIndex(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&string.Equals(e.Rarity,"Rare",StringComparison.OrdinalIgnoreCase));if(at<0){AddLog($"EQUIPMENT — no Rare Equipment remains in the live deck ({source}).");return null;}string id=EquipmentDeck[at];EquipmentDeck.RemoveAt(at);hero.AcquiredEquipment.Add(id);AddLog($"EQUIPMENT — {hero.DisplayName} gains {RewardContent0826.EquipmentLabel(id)} unequipped ({source}).");return id;
        }

        // 0.8.30 deterministic validator hooks.
        public int DebugEnemyRange0830(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?EnemyAttackRangeConnections0830(e):-1;
        public int DebugTrailMarkers0830=>QuestRuntime0829?.TrailMarkers??0;
        public int DebugSupplyCratesRemaining0830=>QuestRuntime0829?.Objects?.Count(o=>o.Type=="SupplyCrate"&&!o.Destroyed)??0;
        public bool DebugBeaconLit0830=>QuestRuntime0829?.BeaconLit??false;
        public void DebugSetExplorationEmpty0830(){ExplorationDeck0828.Clear();EvaluateQuestOutcome0829();}
    }
}
