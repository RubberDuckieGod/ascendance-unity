using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.32 — Campaign I final quest batch: Q009 Rescue Captain Roland,
    // Q010 Assault the Fortress Gate, Q011 Through the Stronghold, Q012 The Goblin King.
    public sealed partial class AscendanceGameManager
    {
        public const int Q010BallistaHealth0832=8;
        public const int Q010BallistaArmor0832=2;
        public const int Q010BallistaAttack0832=4;
        public const int Q010BallistaRange0832=3;
        public const int Q010GateHealth0832=12;
        public const int Q010GateArmor0832=4;
        public const int Q010GateBreachedArmor0832=2;
        private bool IsQ009Active0832=>CurrentQuestId0829=="Q009"&&!QuestComplete&&!QuestFailed;
        private bool IsQ010Active0832=>CurrentQuestId0829=="Q010"&&!QuestComplete&&!QuestFailed;
        private bool IsQ011Active0832=>CurrentQuestId0829=="Q011"&&!QuestComplete&&!QuestFailed;
        private bool IsQ012Active0832=>CurrentQuestId0829=="Q012"&&!QuestComplete&&!QuestFailed;
        private bool IsWE004Quest0832=>CurrentQuestId0829=="Q010"||CurrentQuestId0829=="Q011"||CurrentQuestId0829=="Q012";
        private bool IsQuestBatchFinal0832=>CurrentQuestId0829=="Q009"||CurrentQuestId0829=="Q010"||CurrentQuestId0829=="Q011"||CurrentQuestId0829=="Q012";
        private bool goblinKingImmediateActivationRunning0832;

        private void InitializeQuestStart0832()
        {
            if(QuestRuntime0829==null)return;
            QuestRuntime0829.GoblinKingHordeRound=-1;
            if(CurrentQuestId0829=="Q011")
            {
                QuestRuntime0829.ReinforcementTokensRemaining=3;
                if(Locations.TryGetValue("L017",out var gate)){gate.Revealed=true;gate.RevealResolved=true;}
                foreach(var hero in ParticipatingHeroes)hero.LocationId="L017";
                int raiders=PartySize<=1?1:PartySize==2?2:PartySize==3?3:3;
                for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L017","Q011 stronghold entry");
                if(PartySize>=4)SpawnQuestEnemy0829("E002","L017","Q011 4-Hero setup scaling");
                AddLog($"Q011 SETUP — party enters at Goblin Fortress Gate with 3 Reinforcement Tokens and {raiders} Raider{(raiders==1?"":"s")}.");
            }
            else if(CurrentQuestId0829=="Q012")
            {
                if(Locations.TryGetValue("L027",out var throne)){throne.Revealed=true;throne.RevealResolved=true;}
                foreach(var hero in ParticipatingHeroes)hero.LocationId="L027";
                SpawnQuestEnemy0829("E032","L027","Q012 finale setup");
                int bodyguards=PartySize<=1?1:2;
                for(int i=0;i<bodyguards;i++)SpawnQuestEnemy0829("E018","L027","Q012 finale setup");
                if(PartySize>=2)SpawnQuestEnemy0829("E019","L027","Q012 finale setup");
                if(PartySize>=3)SpawnQuestEnemy0829("E002","L027","Q012 3+ Hero setup scaling");
                if(PartySize>=4)SpawnQuestEnemy0829("E001","L027","Q012 4-Hero setup scaling");
                for(int i=1;i<=4;i++){var b=new QuestObjectState0829{ObjectId=$"Q012_BANNER_{i}",QuestId="Q012",Type="Banner",DisplayName=$"Banner {i}",LocationId="L027"};QuestRuntime0829.Objects.Add(b);QuestObjectAvatar0829.Create(b);}
                AddLog("Q012 SETUP — The Goblin King, throne defenders, and 4 Banner Tokens enter play. The King cannot lose Health while 2 or more Banners remain.");
            }
        }

        private string QuestObjectiveSummary0832()
        {
            if(CurrentQuestId0829=="Q009")return $"QUEST Q009 — RESCUE CAPTAIN ROLAND\n\n{(QuestRuntime0829?.RolandRevealed==true?"[OK]":"[ ]")} Prison Pens / Roland found\n{(QuestRuntime0829?.RolandFreed==true?"[OK]":"[ ]")} Roland freed\n{(QuestRuntime0829?.RolandRescued==true?"[OK]":"[ ]")} Roland escorted to Stonebrook\nExecution Track: {(QuestRuntime0829?.RolandFreed==true?"Removed":(QuestRuntime0829?.ExecutionTrack??0).ToString())}\nRoland: 5 Health / Attack 2";
            if(CurrentQuestId0829=="Q010")return $"QUEST Q010 — ASSAULT THE FORTRESS GATE\n\n{(QuestRuntime0829?.FortressGateRevealed==true?"[OK]":"[ ]")} Fortress Gate revealed\nGate: {Mathf.Max(0,QuestRuntime0829?.GateHealth??0)}/{Q010GateHealth0832} Health • Armor {QuestRuntime0829?.GateArmor??Q010GateArmor0832}\nBallistas destroyed: {QuestRuntime0829?.Objects?.Count(o=>o.Type=="Ballista"&&o.Destroyed)??0}/2\n{(QuestRuntime0829?.ChampionDefeated==true?"[OK]":"[ ]")} Goblin Champion defeated\nEmpty-assault clock: {QuestRuntime0829?.GateEmptyRounds??0}/4\nRoland assist: {(CaptainRolandAssistsQ0100832?"Available":"Not earned")}";
            if(CurrentQuestId0829=="Q011")return $"QUEST Q011 — THROUGH THE STRONGHOLD\n\nReinforcement Tokens: {QuestRuntime0829?.ReinforcementTokensRemaining??0}/3\nStronghold reveals: {QuestRuntime0829?.StrongholdQualifyingReveals??0}\nThrone Room: {(QuestRuntime0829?.ThroneRoomRevealed==true?"Revealed":"Locked until 2 other Stronghold/Fortress reveals")}\nHigh Shaman: {(QuestRuntime0829?.HighShamanWaiting==true?"Waiting beside Quest":"In play / not yet queued")}\nLong Rest: prohibited";
            if(CurrentQuestId0829=="Q012")return $"QUEST Q012 — THE GOBLIN KING\n\nBanners destroyed: {QuestRuntime0829?.BannersDestroyed??0}/4\nKing shield: {(QuestRuntime0829?.BannersDestroyed<3?"ACTIVE — cannot lose Health":"DOWN")}\nLast Stand: {(QuestRuntime0829?.GoblinKingLastStandTriggered==true?"ACTIVE — 2 activations / Enemy Phase":"Not triggered")}\n{(QuestRuntime0829?.CampaignVictory==true?"[VICTORY] Defender of Stonebrook unlocked":"Defeat the King after all Banners are destroyed")}";
            return null;
        }
        private string QuestPrimaryActionLabel0832()
        {
            if(CurrentQuestId0829=="Q009")return "Free Roland";
            if(CurrentQuestId0829=="Q010")return "Attack Gate";
            if(CurrentQuestId0829=="Q012")return "Destroy Banner";
            return CurrentQuestId0829=="Q011"?"Quest Action":null;
        }
        private string QuestSecondaryActionLabel0832()=>CurrentQuestId0829=="Q010"?"Attack Ballista":IsQuestBatchFinal0832?"Quest Action":null;
        private bool CanQuestPrimaryAction0832(HeroState hero)
        {
            if(hero==null||!CanUseTurnActions(hero)||GeneralActions(hero)<1)return false;
            if(IsQ009Active0832)return QuestRuntime0829.RolandRevealed&&!QuestRuntime0829.RolandFreed&&hero.LocationId=="L018";
            if(IsQ010Active0832)return QuestRuntime0829.FortressGateRevealed&&!QuestRuntime0829.GateDestroyed&&hero.LocationId=="L017";
            if(IsQ012Active0832)return hero.LocationId=="L027"&&QuestRuntime0829.Objects.Any(o=>o.Type=="Banner"&&!o.Destroyed);
            return false;
        }
        private bool CanQuestSecondaryAction0832(HeroState hero)
        {
            if(hero==null||!CanUseTurnActions(hero)||GeneralActions(hero)<1)return false;
            return IsQ010Active0832&&hero.LocationId=="L017"&&QuestRuntime0829.Objects.Any(o=>o.Type=="Ballista"&&!o.Destroyed);
        }
        private void QuestPrimaryAction0832()
        {
            if(IsQ009Active0832){FreeCaptainRoland0832();return;}
            if(IsQ010Active0832){AttackFortressGate0832();return;}
            if(IsQ012Active0832){DestroyBanner0832();return;}
        }
        private void QuestSecondaryAction0832(){if(IsQ010Active0832)AttackBallista0832();}

        private bool OnQuestLocationRevealed0832(LocationState location,HeroState hero)
        {
            if(location==null||QuestRuntime0829==null||!IsQuestBatchFinal0832)return false;
            if(IsQ009Active0832&&location.Id=="L018"&&!QuestRuntime0829.RolandRevealed)
            {
                QuestRuntime0829.RolandRevealed=true;QuestRuntime0829.ExecutionTrack=5;
                var roland=new QuestObjectState0829{ObjectId="Q009_ROLAND",QuestId="Q009",Type="CaptainRoland",DisplayName="Captain Roland",LocationId="L018",Health=5,MaxHealth=5,Attack=2};QuestRuntime0829.Objects.Add(roland);QuestObjectAvatar0829.Create(roland);
                int raiders=PartySize<=1?1:PartySize>=3?3:2;int archers=PartySize>=4?2:1;
                for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L018","Captain Roland found");for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002","L018","Captain Roland found");SpawnQuestEnemy0829("E006","L018","Captain Roland found");
                AddLog("Q009 SETUP — Captain Roland found in Prison Pens. Execution Track set to 5; guards enter play.");return true;
            }
            if(IsQ010Active0832&&location.Id=="L017"&&!QuestRuntime0829.FortressGateRevealed)
            {
                SetupFortressGate0832();return true;
            }
            if(IsQ011Active0832)
            {
                bool qualifying=IsQ011QualifyingStronghold0832(location)&&location.Id!="L017";
                if(qualifying){QuestRuntime0829.StrongholdQualifyingReveals++;ResolveStrongholdReinforcementToken0832(location);}
                if(location.Id=="L027"&&!QuestRuntime0829.ThroneRoomRevealed)
                {
                    QuestRuntime0829.ThroneRoomRevealed=true;SpawnQuestEnemy0829("E001","L027","Throne Room revealed");SpawnQuestEnemy0829("E002","L027","Throne Room revealed");
                    if(QuestRuntime0829.HighShamanWaiting&&!Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q011"&&e.TemplateId=="E019")){QuestRuntime0829.HighShamanWaiting=false;SpawnQuestEnemy0829("E019","L027","waiting High Shaman enters Throne Room");}
                    AddLog("Q011 THRONE ROOM — final defenders enter play.");EvaluateQuestOutcome0829();
                }
                return true;
            }
            return true;
        }

        private void SetupFortressGate0832()
        {
            QuestRuntime0829.FortressGateRevealed=true;QuestRuntime0829.GateHealth=Q010GateHealth0832;QuestRuntime0829.GateArmor=Q010GateArmor0832;
            var gate=new QuestObjectState0829{ObjectId="Q010_GATE",QuestId="Q010",Type="FortressGate",DisplayName="Fortress Gate",LocationId="L017",Health=Q010GateHealth0832,MaxHealth=Q010GateHealth0832,Armor=Q010GateArmor0832};QuestRuntime0829.Objects.Add(gate);QuestObjectAvatar0829.Create(gate);
            for(int i=1;i<=2;i++){var b=new QuestObjectState0829{ObjectId=$"Q010_BALLISTA_{i}",QuestId="Q010",Type="Ballista",DisplayName=$"Ballista {i}",LocationId="L017",Health=Q010BallistaHealth0832,MaxHealth=Q010BallistaHealth0832,Armor=Q010BallistaArmor0832,Attack=Q010BallistaAttack0832,Range=Q010BallistaRange0832};QuestRuntime0829.Objects.Add(b);QuestObjectAvatar0829.Create(b);}
            int raiders=PartySize<=1?2:PartySize==2?3:4;int archers=PartySize<=1?1:PartySize==2?2:PartySize==3?2:3;
            for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L017","Fortress Gate revealed");for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002","L017","Fortress Gate revealed");SpawnQuestEnemy0829("E016","L017","Fortress Gate revealed");
            if(CaptainRolandAssistsQ0100832)AddLog("Q010 ALLY — Captain Roland's Q009 reward grants the party one additional Ally slot at setup.");
            AddLog($"Q010 SETUP — Fortress Gate {Q010GateHealth0832} Health / {Q010GateArmor0832} Armor; two Ballistas each {Q010BallistaHealth0832} Health / {Q010BallistaArmor0832} Armor / Attack {Q010BallistaAttack0832} / Range {Q010BallistaRange0832}.");
        }

        private void FreeCaptainRoland0832()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0832(hero)||!RequireHeroTurn(hero,"Free Captain Roland"))return;hero.Actions--;QuestRuntime0829.RolandFreed=true;QuestRuntime0829.ExecutionTrack=0;var r=QuestRuntime0829.Objects.FirstOrDefault(o=>o.Type=="CaptainRoland");if(r!=null){r.OwnerHeroId=hero.Id;r.Resolved=true;r.LocationId=hero.LocationId;}
            SpawnQuestEnemy0829("E011","L018","Roland prison break");SpawnQuestEnemy0829("E001","L018","Roland prison break");if(PartySize>=2)SpawnQuestEnemy0829("E002","L018","Roland prison break");if(PartySize>=4)SpawnQuestEnemy0829("E001","L018","Q009 4-Hero prison-break scaling");
            AddLog($"Q009 PRISON BREAK — {hero.DisplayName} frees Captain Roland. Execution Track is removed; Roland now moves with the rescuing Hero.");StateChanged?.Invoke();
        }
        private void OnQuestHeroMoved0832(HeroState hero)
        {
            if(hero==null||!IsQ009Active0832||!QuestRuntime0829.RolandFreed||QuestRuntime0829.RolandRescued)return;var r=QuestRuntime0829.Objects.FirstOrDefault(o=>o.Type=="CaptainRoland"&&!o.Destroyed);if(r==null||r.OwnerHeroId!=hero.Id)return;r.LocationId=hero.LocationId;if(hero.LocationId=="L000"){r.Rescued=true;QuestRuntime0829.RolandRescued=true;AddLog($"Q009 RESCUE — Captain Roland reaches Stonebrook with {hero.DisplayName}.");EvaluateQuestOutcome0829();}
        }

        private void AttackFortressGate0832()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0832(hero)||!RequireHeroTurn(hero,"Attack Fortress Gate"))return;var gate=QuestRuntime0829.Objects.FirstOrDefault(o=>o.Type=="FortressGate"&&!o.Destroyed);if(gate==null)return;hero.Actions--;BreakHiddenOnAttack(hero);int attack=EffectiveHeroAttack(hero);int damage=Mathf.Max(1,attack-QuestRuntime0829.GateArmor);gate.Health=Mathf.Max(0,gate.Health-damage);QuestRuntime0829.GateHealth=gate.Health;AddLog($"Q010 ATTACK — {hero.DisplayName} attacks Fortress Gate: Attack {attack} · Armor {QuestRuntime0829.GateArmor} · Damage {damage} ({gate.Health}/{gate.MaxHealth}).");
            if(gate.Health<=6&&!QuestRuntime0829.GateReinforcementTriggered){QuestRuntime0829.GateReinforcementTriggered=true;SpawnQuestEnemy0829("E015","L017","Fortress Gate at 6 Health");SpawnQuestEnemy0829("E018","L017","Fortress Gate at 6 Health");SpawnQuestEnemy0829("E018","L017","Fortress Gate at 6 Health");QuestRuntime0829.ChampionSpawned=true;AddLog("Q010 FORTRESS REINFORCEMENTS — Goblin Champion and two Chieftain's Bodyguards enter play.");}
            if(gate.Health<=0){gate.Destroyed=true;QuestRuntime0829.GateDestroyed=true;AddLog("Q010 OBJECTIVE — Fortress Gate destroyed. Gate reinforcements stop.");}
            EvaluateQuestOutcome0829();StateChanged?.Invoke();
        }
        private void AttackBallista0832()
        {
            var hero=SelectedHero;if(!CanQuestSecondaryAction0832(hero)||!RequireHeroTurn(hero,"Attack Ballista"))return;var b=QuestRuntime0829.Objects.Where(o=>o.Type=="Ballista"&&!o.Destroyed).OrderBy(o=>o.ObjectId).FirstOrDefault();if(b==null)return;hero.Actions--;BreakHiddenOnAttack(hero);int attack=EffectiveHeroAttack(hero);int damage=Mathf.Max(1,attack-b.Armor);b.Health=Mathf.Max(0,b.Health-damage);AddLog($"Q010 ATTACK — {hero.DisplayName} attacks {b.DisplayName}: Attack {attack} · Armor {b.Armor} · Damage {damage} ({b.Health}/{b.MaxHealth}).");if(b.Health<=0)DestroyBallista0832(b);StateChanged?.Invoke();
        }
        private void DestroyBallista0832(QuestObjectState0829 ballista)
        {
            if(ballista==null||ballista.Destroyed)return;ballista.Destroyed=true;if(!ballista.Resolved){ballista.Resolved=true;SpawnQuestEnemy0829("E002","L017",$"{ballista.DisplayName} destroyed");}
            if(!QuestRuntime0829.BallistaRepairUsed&&!QuestRuntime0829.BallistaRepairPending&&Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q010"&&e.TemplateId=="E016")){QuestRuntime0829.BallistaRepairPending=true;QuestRuntime0829.PendingBallistaObjectId=ballista.ObjectId;AddLog($"Q010 FIELD REPAIRS — {ballista.DisplayName} is set aside; the Siege Engineer will repair it at the end of the next Enemy Phase if still alive.");}
            RefreshBallistaState0832();AddLog($"Q010 OBJECTIVE — {ballista.DisplayName} destroyed. {QuestRuntime0829.BallistasDestroyed}/2 Ballistas currently destroyed.");
        }
        private void RefreshBallistaState0832()
        {
            QuestRuntime0829.BallistasDestroyed=QuestRuntime0829.Objects.Count(o=>o.Type=="Ballista"&&o.Destroyed);QuestRuntime0829.GateArmor=QuestRuntime0829.BallistasDestroyed>=2?Q010GateBreachedArmor0832:Q010GateArmor0832;var gate=QuestRuntime0829.Objects.FirstOrDefault(o=>o.Type=="FortressGate");if(gate!=null)gate.Armor=QuestRuntime0829.GateArmor;if(QuestRuntime0829.BallistasDestroyed>=2)AddLog("Q010 BREACH — both Ballistas are destroyed; Fortress Gate Armor is reduced from 4 to 2.");
        }

        private bool ResolveQuestEndEnemyPhase0832()
        {
            if(IsQ009Active0832)
            {
                if(QuestRuntime0829.RolandRevealed&&!QuestRuntime0829.RolandFreed){QuestRuntime0829.ExecutionTrack=Mathf.Max(0,QuestRuntime0829.ExecutionTrack-1);AddLog($"Q009 EXECUTION TRACK — {QuestRuntime0829.ExecutionTrack}/5 remaining.");if(QuestRuntime0829.ExecutionTrack<=0){QuestFailed=true;AddLog("Q009 FAILED — Execution Track reached 0 before Captain Roland was freed.");}}
                return true;
            }
            if(IsQ010Active0832)
            {
                ResolveBallistaAttacks0832(null);
                if(QuestRuntime0829.BallistaRepairPending){var pending=QuestRuntime0829.Objects.FirstOrDefault(o=>o.ObjectId==QuestRuntime0829.PendingBallistaObjectId);bool engineerAlive=Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q010"&&e.TemplateId=="E016");if(pending!=null&&engineerAlive){pending.Destroyed=false;pending.Health=pending.MaxHealth;QuestObjectAvatar0829.Create(pending);QuestRuntime0829.BallistaRepairUsed=true;AddLog($"Q010 FIELD REPAIRS — Siege Engineer restores {pending.DisplayName} to full Health. This Ballista cannot be repaired again.");}else AddLog("Q010 FIELD REPAIRS — repair fails because the Siege Engineer is no longer in play.");QuestRuntime0829.BallistaRepairPending=false;QuestRuntime0829.PendingBallistaObjectId=null;RefreshBallistaState0832();}
                if(!QuestRuntime0829.GateDestroyed&&QuestRuntime0829.FortressGateRevealed){if(!(PartySize<=1&&Round%2!=0)){int n=PartySize>=4?2:1;for(int i=0;i<n;i++)SpawnQuestEnemy0829("E001","L017","Gate reinforcement");AddLog($"Q010 GATE REINFORCEMENTS — {n} Goblin Raider{(n==1?"":"s")} enter at the Gate.");}}
                if(QuestRuntime0829.FortressGateRevealed&&!QuestRuntime0829.GateDestroyed){bool assault=ParticipatingHeroes.Any(h=>!h.IsDefeated&&(h.LocationId=="L017"||(Locations.TryGetValue("L017",out var g)&&g.Connections.Contains(h.LocationId)&&Locations[h.LocationId].Revealed)));QuestRuntime0829.GateEmptyRounds=assault?0:QuestRuntime0829.GateEmptyRounds+1;if(!assault)AddLog($"Q010 LOSS CLOCK — no Hero maintains the assault ({QuestRuntime0829.GateEmptyRounds}/4).");if(QuestRuntime0829.GateEmptyRounds>=4){QuestFailed=true;AddLog("Q010 FAILED — the fortress assault was abandoned for 4 consecutive rounds.");}}
                return true;
            }
            if(IsQ011Active0832||IsQ012Active0832)return true;
            return false;
        }
        private void ResolveBallistaAttacks0832(int? forcedDie)
        {
            if(!IsQ010Active0832)return;foreach(var b in QuestRuntime0829.Objects.Where(o=>o.Type=="Ballista"&&!o.Destroyed).ToList()){var target=ParticipatingHeroes.Where(h=>!h.IsDefeated&&GraphDistance(b.LocationId,h.LocationId)<=b.Range).OrderBy(h=>GraphDistance(b.LocationId,h.LocationId)).ThenBy(h=>h.Id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();if(target==null){AddLog($"Q010 BALLISTA — {b.DisplayName} has no Hero within Range {b.Range}.");continue;}int die=forcedDie??UnityEngine.Random.Range(1,7);int damage=Mathf.Max(0,die+b.Attack-EffectiveHeroArmor(target));AddLog($"Q010 BALLISTA — {b.DisplayName} attacks {target.DisplayName}: d6 {die} + Attack {b.Attack} − Armor {EffectiveHeroArmor(target)} = {damage} damage.");ApplyHeroDamageNow(target,damage,b.DisplayName);}}

        private bool IsQ011QualifyingStronghold0832(LocationState location)
        {
            if(location==null)return false;
            // Use authored identity, not Can-Connect-To vocabulary that may appear in legacy metadata fields.
            return location.AuthoredLocationId=="L019"||location.AuthoredLocationId=="L020"||location.AuthoredLocationId=="L021"||location.AuthoredLocationId=="L023"||location.AuthoredLocationId=="L025"||location.AuthoredLocationId=="L031";
        }
        private void ResolveStrongholdReinforcementToken0832(LocationState location)
        {
            if(QuestRuntime0829.ReinforcementTokensRemaining<=0)return;int token=4-QuestRuntime0829.ReinforcementTokensRemaining;QuestRuntime0829.ReinforcementTokensRemaining--;
            if(token==1){SpawnQuestEnemy0829("E001",location.Id,"Q011 Reinforcement Token 1");SpawnQuestEnemy0829("E002",location.Id,"Q011 Reinforcement Token 1");}
            else if(token==2){if(PartySize>1)SpawnQuestEnemy0829("E018",location.Id,"Q011 Reinforcement Token 2");SpawnQuestEnemy0829("E017",location.Id,"Q011 Reinforcement Token 2");}
            else if(token==3){SpawnQuestEnemy0829("E018",location.Id,"Q011 Reinforcement Token 3");SpawnQuestEnemy0829("E002",location.Id,"Q011 Reinforcement Token 3");SpawnQuestEnemy0829("E017",location.Id,"Q011 Reinforcement Token 3");if(PartySize>=4)SpawnQuestEnemy0829("E001",location.Id,"Q011 4-Hero Token 3 scaling");if(QuestRuntime0829.ThroneRoomRevealed||location.Id=="L027")SpawnQuestEnemy0829("E019","L027","Q011 Reinforcement Token 3");else QuestRuntime0829.HighShamanWaiting=true;}
            AddLog($"Q011 REINFORCEMENT TOKEN {token} — removed at {location.Name}. {QuestRuntime0829.ReinforcementTokensRemaining} token(s) remain.");
        }
        private bool CanRevealQuestLocation0832(string locationId)
        {
            if(CurrentQuestId0829=="Q011"&&locationId=="L027"&&QuestRuntime0829!=null&&QuestRuntime0829.StrongholdQualifyingReveals<2)return false;return true;
        }
        private bool CanEnterQuestLocation0832(string locationId)
        {
            if(CurrentQuestId0829=="Q011"&&locationId=="L027"&&QuestRuntime0829!=null&&QuestRuntime0829.StrongholdQualifyingReveals<2)return false;return true;
        }
        private string NextQuestEnemyStep0832(EnemyState enemy)
        {
            if(enemy!=null&&IsQ012Active0832&&enemy.TemplateId=="E032")return null;return null;
        }

        private void DestroyBanner0832()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0832(hero)||!RequireHeroTurn(hero,"Destroy Banner"))return;var b=QuestRuntime0829.Objects.Where(o=>o.Type=="Banner"&&!o.Destroyed).OrderBy(o=>o.ObjectId).FirstOrDefault();if(b==null)return;hero.Actions--;b.Destroyed=true;QuestRuntime0829.BannersDestroyed++;int n=QuestRuntime0829.BannersDestroyed;AddLog($"Q012 BANNER — {hero.DisplayName} destroys {b.DisplayName} ({n}/4).");
            if(n==1)SpawnEliteFinaleGoblin0832("E001","Elite Goblin Raider","first Banner destroyed");
            else if(n==2)SpawnEliteFinaleGoblin0832("E002","Elite Goblin Archer","second Banner destroyed");
            else if(n==3)SpawnEliteFinaleGoblin0832("E001","Elite Goblin Raider","third Banner destroyed");
            else if(n==4){if(PartySize>=4)SpawnQuestEnemy0829("E006","L027","4-Hero final Banner scaling");QueueGoblinKingImmediateActivation0832("final Banner destroyed");}
            EvaluateQuestOutcome0829();StateChanged?.Invoke();
        }
        private EnemyState SpawnEliteFinaleGoblin0832(string templateId,string displayName,string source){var e=SpawnQuestEnemy0829(templateId,"L027",source);if(e!=null)e.DisplayName=displayName;return e;}
        private bool ShouldPreventQuestBossDamage0832(EnemyState enemy)=>enemy!=null&&IsQ012Active0832&&enemy.TemplateId=="E032"&&QuestRuntime0829.BannersDestroyed<3;
        private void OnQuestEnemyDamaged0832(EnemyState enemy)
        {
            if(enemy==null||!IsQ012Active0832||enemy.TemplateId!="E032"||enemy.Defeated)return;if(!QuestRuntime0829.GoblinKingLastStandTriggered&&enemy.Health<=14){QuestRuntime0829.GoblinKingLastStandTriggered=true;AddLog("Q012 KING'S LAST STAND — The Goblin King reaches 14 Health or less and immediately activates. He will now activate twice each Enemy Phase.");QueueGoblinKingImmediateActivation0832("King's Last Stand");}
        }
        private int QuestEnemyAttackBonus0832(EnemyState enemy)
        {
            if(enemy==null||!enemy.QuestSpawned)return 0;int bonus=0;if(IsQ012Active0832&&enemy.TemplateId!="E032"&&IsGoblinEnemy(enemy)){var king=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q012"&&e.TemplateId=="E032");if(king!=null&&(enemy.LocationId==king.LocationId||GraphDistance(enemy.LocationId,king.LocationId)<=1))bonus+=1;}return bonus;
        }
        private int QuestEnemyArmorBonus0832(EnemyState enemy)
        {
            if(enemy==null||!enemy.QuestSpawned)return 0;int bonus=0;if(enemy.TemplateId=="E018"&&Enemies.Values.Any(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId&&(e.TemplateId=="E019"||e.TemplateId=="E032"||e.TemplateId=="E010"||e.TemplateId=="E006")))bonus+=1;if(enemy.TemplateId=="E019"&&enemy.LocationId=="L027")bonus+=1;return bonus;
        }
        private void OnQuestEnemyAttackResolved0832(EnemyState enemy,HeroState target,int dealt)
        {
            if(enemy==null||target==null||dealt<=0||!IsQ012Active0832||enemy.TemplateId!="E032")return;if(!Locations.TryGetValue(target.LocationId,out var current))return;string next=current.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed&&CanEnterQuestLocation0832(id)).OrderBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();if(string.IsNullOrEmpty(next))return;string from=target.LocationId;target.LocationId=next;AddLog($"Q012 CRUSHING COMMAND — {target.DisplayName} is driven from {Locations[from].Name} to {Locations[next].Name}.");
        }
        private void OnQuestEnemyActivationCompleted0832(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated||!IsQ012Active0832||enemy.TemplateId!="E032"||QuestRuntime0829.GoblinKingHordeRound==Round)return;QuestRuntime0829.GoblinKingHordeRound=Round;int others=Enemies.Values.Count(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q012"&&e.Id!=enemy.Id&&IsGoblinEnemy(e));if(others>=3)return;bool heroHere=ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId);SpawnEliteFinaleGoblin0832(heroHere?"E002":"E001",heroHere?"Elite Goblin Archer":"Elite Goblin Raider","CALL THE HORDE");AddLog("Q012 CALL THE HORDE — the Goblin King summons an Elite Goblin because fewer than 3 other Goblins remain in play.");
        }
        private void QueueGoblinKingImmediateActivation0832(string source)
        {
            if(goblinKingImmediateActivationRunning0832||!IsQ012Active0832)return;var king=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q012"&&e.TemplateId=="E032");if(king==null)return;StartCoroutine(ResolveGoblinKingImmediateActivation0832(king,source));
        }
        private IEnumerator ResolveGoblinKingImmediateActivation0832(EnemyState king,string source)
        {
            goblinKingImmediateActivationRunning0832=true;king.ActivatedThisRound=false;AddLog($"Q012 IMMEDIATE ACTIVATION — The Goblin King activates now ({source}).");yield return ResolveSingleEnemyActivation(king,"Q012 IMMEDIATE ACTIVATION");goblinKingImmediateActivationRunning0832=false;
        }
        private void PrepareQuestWorldPhase0832()
        {
            if(!IsQ012Active0832)return;var king=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q012"&&e.TemplateId=="E032");if(king!=null)king.ActivatedThisRound=false;
        }
        private IEnumerator ResolveQuestExtraWorldPhaseActivations0832()
        {
            if(!IsQ012Active0832||!QuestRuntime0829.GoblinKingLastStandTriggered)yield break;var king=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q012"&&e.TemplateId=="E032");if(king==null)yield break;king.ActivatedThisRound=false;AddLog("Q012 KING'S LAST STAND — second Goblin King activation this Enemy Phase.");yield return ResolveSingleEnemyActivation(king,"Q012 LAST STAND");
        }

        private void OnQuestEnemyDefeated0832(EnemyState enemy,HeroState killer)
        {
            if(enemy==null||!enemy.QuestSpawned||enemy.QuestId!=CurrentQuestId0829)return;if(CurrentQuestId0829=="Q010"&&enemy.TemplateId=="E015"){QuestRuntime0829.ChampionDefeated=true;AddLog("Q010 OBJECTIVE — Goblin Champion defeated.");}EvaluateQuestOutcome0829();
        }
        private bool EvaluateQuestOutcome0832()
        {
            if(CurrentQuestId0829=="Q009"){if(QuestRuntime0829.RolandDefeated){QuestFailed=true;AddLog("Q009 FAILED — Captain Roland was defeated after being freed.");return true;}if(QuestRuntime0829.RolandRescued&&!QuestComplete){QuestComplete=true;AddLog("Q009 COMPLETE — Captain Roland safely reaches Stonebrook.");ResolveQuestReward0829();}return true;}
            if(CurrentQuestId0829=="Q010"){if(QuestRuntime0829.GateDestroyed&&QuestRuntime0829.ChampionDefeated&&!QuestComplete){QuestComplete=true;AddLog("Q010 COMPLETE — Fortress Gate destroyed and Goblin Champion defeated.");ResolveQuestReward0829();}return true;}
            if(CurrentQuestId0829=="Q011"){if(QuestRuntime0829.ThroneRoomRevealed&&!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId=="L027")&&!QuestComplete){QuestComplete=true;AddLog("Q011 COMPLETE — Goblin Throne Room reached and cleared.");ResolveQuestReward0829();}return true;}
            if(CurrentQuestId0829=="Q012"){bool kingDown=!Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q012"&&e.TemplateId=="E032");if(QuestRuntime0829.BannersDestroyed>=4&&kingDown&&!QuestComplete){QuestComplete=true;QuestRuntime0829.CampaignVictory=true;QuestRuntime0829.DefenderOfStonebrookUnlocked=true;AddLog("Q012 COMPLETE — The Goblin King is defeated. CAMPAIGN I VICTORY: Defender of Stonebrook unlocked.");ResolveQuestReward0829();}return true;}
            return false;
        }
        private bool ResolveQuestReward0832(HeroState receiver)
        {
            if(CurrentQuestId0829=="Q009"){foreach(var h in ParticipatingHeroes)h.XP+=125;AddPartyGold(100,"Q009 victory reward");AddGeneralReputation(2,"Q009 victory reward");CaptainRolandAssistsQ0100832=true;AddLog("Q009 REWARD — 125 XP each • 100 Gold • +2 General Reputation • Captain Roland Q010 Ally allowance banked.");return true;}
            if(CurrentQuestId0829=="Q010"){foreach(var h in ParticipatingHeroes)h.XP+=225;AddPartyGold(200,"Q010 victory reward");if(receiver!=null)DrawEpicEquipment0831(receiver,"Q010 victory reward");AddLog("Q010 REWARD — 225 XP each • 200 Gold • 1 Epic Equipment.");return true;}
            if(CurrentQuestId0829=="Q011"){foreach(var h in ParticipatingHeroes)h.XP+=250;AddPartyGold(200,"Q011 victory reward");if(receiver!=null)DrawEpicEquipment0831(receiver,"Q011 victory reward");AddLog("Q011 REWARD — 250 XP each • 200 Gold • 1 Epic Equipment • Q012 unlocked.");return true;}
            if(CurrentQuestId0829=="Q012"){foreach(var h in ParticipatingHeroes){h.XP+=300;DrawLegendaryEquipment0832(h,"Q012 victory reward");}AddGeneralReputation(3,"Q012 victory reward");AddLog("Q012 REWARD — 300 XP each • 1 Legendary Equipment per Hero • +3 General Reputation • Defender of Stonebrook unlocked. WE004 grants no duplicate tangible reward.");return true;}
            return false;
        }
        private string DrawLegendaryEquipment0832(HeroState hero,string source)
        {
            if(hero==null)return null;int at=EquipmentDeck.FindIndex(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&string.Equals(e.Rarity,"Legendary",StringComparison.OrdinalIgnoreCase));if(at<0){AddLog($"EQUIPMENT — no Legendary Equipment remains in the live deck ({source}).");return null;}string id=EquipmentDeck[at];EquipmentDeck.RemoveAt(at);hero.AcquiredEquipment.Add(id);AddLog($"EQUIPMENT — {hero.DisplayName} gains Legendary {RewardContent0826.EquipmentLabel(id)} unequipped ({source}).");return id;
        }

        // Runtime enforcement / validator hooks.
        public int DebugExecutionTrack0832=>QuestRuntime0829?.ExecutionTrack??-1;
        public int DebugRolandHealth0832=>QuestRuntime0829?.Objects?.FirstOrDefault(o=>o.Type=="CaptainRoland")?.Health??-1;
        public bool DebugRolandFreed0832=>QuestRuntime0829?.RolandFreed??false;
        public bool DebugRolandRescued0832=>QuestRuntime0829?.RolandRescued??false;
        public void DebugSyncRoland0832(string heroId){if(Heroes.TryGetValue(heroId??string.Empty,out var h))OnQuestHeroMoved0832(h);}
        public void DebugDamageRoland0832(int amount){var r=QuestRuntime0829?.Objects?.FirstOrDefault(o=>o.Type=="CaptainRoland"&&!o.Destroyed);if(r==null)return;r.Health=Mathf.Max(0,r.Health-Mathf.Max(0,amount));if(r.Health<=0&&QuestRuntime0829.RolandFreed){r.Destroyed=true;QuestRuntime0829.RolandDefeated=true;EvaluateQuestOutcome0829();}}
        public int DebugGateHealth0832=>QuestRuntime0829?.GateHealth??-1;
        public int DebugGateArmor0832=>QuestRuntime0829?.GateArmor??-1;
        public int DebugBallistasDestroyed0832=>QuestRuntime0829?.Objects?.Count(o=>o.Type=="Ballista"&&o.Destroyed)??0;
        public int DebugBallistaHealth0832(int index)=>QuestRuntime0829?.Objects?.Where(o=>o.Type=="Ballista").OrderBy(o=>o.ObjectId).ElementAtOrDefault(index)?.Health??-1;
        public void DebugSetBallistaHealth0832(int index,int hp){var b=QuestRuntime0829?.Objects?.Where(o=>o.Type=="Ballista"&&!o.Destroyed).OrderBy(o=>o.ObjectId).ElementAtOrDefault(index);if(b!=null)b.Health=Mathf.Clamp(hp,1,b.MaxHealth);}
        public void DebugBallistaAttack0832(int die)=>ResolveBallistaAttacks0832(Mathf.Clamp(die,1,6));
        public bool DebugCanEnterQuestLocation0832(string id)=>CanEnterQuestLocation0832(id);
        public int DebugStrongholdReveals0832=>QuestRuntime0829?.StrongholdQualifyingReveals??0;
        public int DebugReinforcementTokens0832=>QuestRuntime0829?.ReinforcementTokensRemaining??-1;
        public int DebugBannersDestroyed0832=>QuestRuntime0829?.BannersDestroyed??0;
        public bool DebugKingLastStand0832=>QuestRuntime0829?.GoblinKingLastStandTriggered??false;
        public bool DebugKingImmediateRunning0832=>goblinKingImmediateActivationRunning0832;
        public IEnumerator DebugRunKingLastStandExtra0832()=>ResolveQuestExtraWorldPhaseActivations0832();
        public bool DebugCampaignVictory0832=>QuestRuntime0829?.CampaignVictory??false;
        public bool DebugDefenderUnlocked0832=>QuestRuntime0829?.DefenderOfStonebrookUnlocked??false;
        public bool DebugCaptainRolandAssistsQ0100832=>CaptainRolandAssistsQ0100832;
        public bool DebugDamageKing0832(int amount){var king=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q012"&&e.TemplateId=="E032");if(king==null)return false;if(ShouldPreventQuestBossDamage0832(king)){AddLog("Q012 BANNERS OF THE HORDE — damage prevented; 2 or more Banners remain.");return false;}king.Health=Mathf.Max(0,king.Health-Mathf.Max(0,amount));OnQuestEnemyDamaged0832(king);if(king.Defeated){OnQuestEnemyDefeated0829(king,SelectedHero);EvaluateQuestOutcome0829();}return true;}
    }
}
