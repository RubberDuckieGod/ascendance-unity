#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class QuestBatchQ009Q012TestTools0832
    {
        [MenuItem("Ascendance/Validation/Test 0.8.32 - Campaign I Q009-Q012 (Play Mode)")]
        public static void TestCampaignIFinalQuestBatch(){if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.32 TEST] Enter Play Mode first.");return;}var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.32 TEST] No AscendanceGameManager found.");return;}gm.StartCoroutine(Run(gm));}
        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.32 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.32 TEST] FAIL — "+label);}}
            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);yield return null;

            // Q009 — Rescue Captain Roland.
            gm.DebugStartQuest0829("Q009");yield return null;
            Check(gm.CurrentQuestId0829=="Q009"&&gm.ExplorationDeck0828.Contains("L018")&&gm.ExplorationDeck0828.Contains("L020"),"Q009 builds the authored Prison Pens + underground Exploration pool.");
            gm.DebugRevealQuestLocation0829("L018","Garruk");yield return null;
            Check(gm.QuestObjects0829.Any(o=>o.Type=="CaptainRoland"&&o.MaxHealth==5&&o.Attack==2)&&gm.DebugExecutionTrack0832==5,"Q009 Prison Pens reveal places Captain Roland at 5 Health / Attack 2 and Execution Track 5.");
            string q9archer=gm.DebugQuestEnemyId0829("E002");Check(!string.IsNullOrEmpty(q9archer)&&gm.DebugEffectiveEnemyAttack(q9archer)==gm.Enemies[q9archer].Attack+1,"Q009 applies WE003's exact +1 Attack modifier to Goblin Archers.");
            gm.DebugQuestEndEnemyPhase0829();yield return null;Check(gm.DebugExecutionTrack0832==4,"Q009 Execution Track decreases by 1 at the end of the Enemy Phase before Roland is freed.");
            gm.DebugSetHeroAt0829("Garruk","L018",3);int gActions=gm.Heroes["Garruk"].Actions;gm.QuestPrimaryAction0829();yield return null;Check(gm.DebugRolandFreed0832&&gm.Heroes["Garruk"].Actions==gActions-1&&gm.DebugQuestEnemyId0829("E011")!=null,"Q009 freeing Roland costs 1 Action, stops the clock, and triggers the prison-break wave.");
            int q9xp=gm.Heroes["Garruk"].XP,q9gold=gm.PartyGold,q9rep=gm.GeneralReputation;gm.DebugSetHeroAt0829("Garruk","L000",3);gm.DebugSyncRoland0832("Garruk");yield return null;Check(gm.QuestComplete&&gm.DebugRolandRescued0832&&gm.Heroes["Garruk"].XP==q9xp+125&&gm.PartyGold==q9gold+100&&gm.GeneralReputation==q9rep+2&&gm.DebugCaptainRolandAssistsQ0100832,"Q009 escort to Stonebrook completes and grants 125 XP, 100 Gold, +2 Reputation, and Roland's Q010 assist flag.");
            gm.DebugStartQuest0829("Q009");gm.DebugRevealQuestLocation0829("L018","Garruk");for(int i=0;i<5&&!gm.QuestFailed;i++)gm.DebugQuestEndEnemyPhase0829();yield return null;Check(gm.QuestFailed&&gm.DebugExecutionTrack0832==0,"Q009 fails when the Execution Track reaches 0 before Roland is freed.");
            gm.DebugStartQuest0829("Q009");gm.DebugRevealQuestLocation0829("L018","Garruk");gm.DebugSetHeroAt0829("Garruk","L018",3);gm.QuestPrimaryAction0829();gm.DebugDamageRoland0832(5);yield return null;Check(gm.QuestFailed&&gm.DebugRolandHealth0832==0,"Q009 fails if freed Captain Roland is reduced to 0 Health.");

            // Q010 — Assault the Fortress Gate.
            gm.DebugStartQuest0829("Q010");yield return null;Check(gm.CurrentQuestId0829=="Q010"&&gm.ExplorationDeck0828.Contains("L017")&&gm.ExplorationDeck0828.Contains("L020"),"Q010 starts with Fortress Gate and Cave/Mine approach Locations in the Exploration pool.");
            gm.DebugRevealQuestLocation0829("L017","Garruk");yield return null;
            var ballistas=gm.QuestObjects0829.Where(o=>o.Type=="Ballista").OrderBy(o=>o.ObjectId).ToList();
            Check(gm.DebugGateHealth0832==12&&gm.DebugGateArmor0832==4&&gm.QuestObjects0829.Any(o=>o.Type=="FortressGate"&&o.MaxHealth==12),"Q010 Fortress Gate enters play at 12 Health / 4 Armor.");
            Check(ballistas.Count==2&&ballistas.All(b=>b.MaxHealth==8&&b.Armor==2&&b.Attack==4&&b.Range==3),"Q010 places two locked 8 Health / 2 Armor / Attack 4 / Range 3 Ballistas.");
            Check(gm.DebugCaptainRolandAssistsQ0100832,"Q010 retains Captain Roland's earned additional-Ally setup allowance from Q009.");
            if(ballistas.Count>=2)
            {
                ballistas[0].Health=1;gm.DebugSetHeroAt0829("Garruk","L017",3);gm.QuestSecondaryAction0829();yield return null;Check(ballistas[0].Destroyed&&gm.QuestRuntime0829.BallistaRepairPending,"Q010 first destroyed Ballista is set aside for the Siege Engineer's one-time field repair.");
                gm.Heroes["Garruk"].Health=gm.Heroes["Garruk"].MaxHealth;gm.Heroes["Aldric"].Health=gm.Heroes["Aldric"].MaxHealth;gm.DebugQuestEndEnemyPhase0829();yield return null;Check(!ballistas[0].Destroyed&&ballistas[0].Health==8&&gm.QuestRuntime0829.BallistaRepairUsed,"Q010 Siege Engineer restores the first Ballista to full Health at the end of the next Enemy Phase.");
                ballistas[0].Health=1;gm.DebugSetHeroAt0829("Garruk","L017",3);gm.QuestSecondaryAction0829();ballistas[1].Health=1;gm.DebugSetHeroAt0829("Garruk","L017",3);gm.QuestSecondaryAction0829();yield return null;Check(gm.DebugBallistasDestroyed0832==2&&gm.DebugGateArmor0832==2,"Q010 destroying both Ballistas reduces Fortress Gate Armor from 4 to 2.");
            }
            else Check(false,"Q010 Ballista repair/Armor test requires both Ballista objects and fails safely if either is missing.");
            gm.DebugSetHeroAt0829("Garruk","L017",3);gm.QuestPrimaryAction0829();gm.DebugSetHeroAt0829("Garruk","L017",3);gm.QuestPrimaryAction0829();yield return null;
            string champ10=gm.DebugQuestEnemyId0829("E015");Check(gm.DebugGateHealth0832<=6&&!string.IsNullOrEmpty(champ10)&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&!e.Defeated&&e.QuestId=="Q010"&&e.TemplateId=="E018")==2,"Q010 Gate reaching 6 Health or less spawns the Goblin Champion and two Chieftain's Bodyguards.");
            int gateAttackGuard0832=0;while(gm.DebugGateHealth0832>0&&gateAttackGuard0832++<10){int beforeGate0832=gm.DebugGateHealth0832;gm.DebugSetHeroAt0829("Garruk","L017",3);gm.QuestPrimaryAction0829();yield return null;if(gm.DebugGateHealth0832>=beforeGate0832)break;}Check(gm.DebugGateHealth0832==0,"Q010 repeated Gate attacks make progress and destroy the Fortress Gate without a validator hang.");
            int q10xp=gm.Heroes["Garruk"].XP,q10gold=gm.PartyGold,q10gear=gm.Heroes["Garruk"].AcquiredEquipment.Count;if(!string.IsNullOrEmpty(champ10)){gm.DebugDefeatQuestEnemy0829(champ10,"Garruk");yield return null;bool epic=gm.Heroes["Garruk"].AcquiredEquipment.Skip(q10gear).Any(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&e.Rarity=="Epic");Check(gm.QuestComplete&&gm.Heroes["Garruk"].XP==q10xp+225&&gm.PartyGold==q10gold+200&&epic,"Q010 completes after Gate + Champion and grants 225 XP, 200 Gold, and Epic Equipment.");}else Check(false,"Q010 completion requires the threshold Champion and fails safely if the spawn is missing.");
            gm.DebugStartQuest0829("Q010");gm.DebugRevealQuestLocation0829("L017","Garruk");gm.DebugSetHeroAt0829("Garruk","L000",0);gm.Heroes["Aldric"].LocationId="L000";for(int i=0;i<4&&!gm.QuestFailed;i++){gm.Heroes["Garruk"].Health=gm.Heroes["Garruk"].MaxHealth;gm.Heroes["Aldric"].Health=gm.Heroes["Aldric"].MaxHealth;gm.DebugQuestEndEnemyPhase0829();}yield return null;Check(gm.QuestFailed&&gm.QuestRuntime0829.GateEmptyRounds>=4,"Q010 fails after 4 consecutive rounds with no Hero maintaining the Gate assault.");

            // Q011 — Through the Stronghold.
            gm.DebugStartQuest0829("Q011");yield return null;Check(gm.CurrentQuestId0829=="Q011"&&gm.DebugReinforcementTokens0832==3&&gm.ParticipatingHeroes.All(h=>h.LocationId=="L017"),"Q011 begins at the breached Fortress Gate with 3 Reinforcement Tokens.");
            Check(!gm.DebugCanEnterQuestLocation0832("L027"),"Q011 blocks entry to the Goblin Throne Room before two other Fortress/Goblin Stronghold Locations are revealed.");
            gm.DebugRevealQuestLocation0829("L014","Garruk");yield return null;Check(gm.DebugStrongholdReveals0832==1&&gm.DebugReinforcementTokens0832==2,"Q011 first qualifying Stronghold reveal consumes Token #1 and spawns Raider + Archer.");
            gm.DebugRevealQuestLocation0829("L018","Garruk");yield return null;Check(gm.DebugStrongholdReveals0832==2&&gm.DebugReinforcementTokens0832==1&&gm.DebugCanEnterQuestLocation0832("L027"),"Q011 second qualifying reveal consumes Token #2 and unlocks Throne Room entry.");
            gm.DebugRevealQuestLocation0829("L027","Garruk");yield return null;Check(gm.DebugReinforcementTokens0832==0&&gm.QuestRuntime0829.ThroneRoomRevealed&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&!e.Defeated&&e.QuestId=="Q011"&&e.TemplateId=="E019")==1,"Q011 Token #3 and Throne reveal place exactly one Goblin High Shaman plus final defenders.");
            int q11xp=gm.Heroes["Garruk"].XP,q11gold=gm.PartyGold,q11gear=gm.Heroes["Garruk"].AcquiredEquipment.Count;foreach(var id in gm.Enemies.Values.Where(e=>!e.Defeated&&e.LocationId=="L027").Select(e=>e.Id).ToList())gm.DebugDefeatQuestEnemy0829(id,"Garruk");yield return null;bool q11epic=gm.Heroes["Garruk"].AcquiredEquipment.Skip(q11gear).Any(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&e.Rarity=="Epic");Check(gm.QuestComplete&&gm.Heroes["Garruk"].XP==q11xp+250&&gm.PartyGold==q11gold+200&&q11epic,"Q011 completes when the Throne Room is cleared and grants 250 XP, 200 Gold, and Epic Equipment.");

            // Q012 — The Goblin King.
            gm.DebugStartQuest0829("Q012");yield return null;string kingId=gm.DebugQuestEnemyId0829("E032");
            Check(!string.IsNullOrEmpty(kingId)&&gm.Enemies.TryGetValue(kingId,out var king)&&king.MaxHealth==26&&king.Attack==5&&king.Armor==3,"Q012 uses dedicated E032 The Goblin King at 26 Health / 5 Attack / 3 Armor.");
            Check(gm.QuestObjects0829.Count(o=>o.Type=="Banner"&&!o.Destroyed)==4&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&!e.Defeated&&e.QuestId=="Q012"&&e.TemplateId=="E018")==2&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&!e.Defeated&&e.QuestId=="Q012"&&e.TemplateId=="E019")==1,"Q012 printed two-Hero setup has 4 Banners, two Bodyguards, and one High Shaman.");
            string guard12=gm.DebugQuestEnemyId0829("E018");Check(!string.IsNullOrEmpty(guard12)&&gm.DebugEffectiveEnemyAttack(guard12)==gm.Enemies[guard12].Attack+1,"Q012 King of the Horde grants allied Goblins exactly +1 Attack in the King's Location/connected Location.");
            int kingHp=gm.Enemies[kingId].Health;gm.DealDirectDamage(gm.Enemies[kingId],5,"0.8.32 Banner shield validator",gm.Heroes["Garruk"]);Check(gm.Enemies[kingId].Health==kingHp,"Q012 Banner shield prevents common direct-damage effects from reducing the Goblin King's Health while 2 or more Banners remain.");
            gm.DebugSetHeroAt0829("Garruk","L027",3);gm.QuestPrimaryAction0829();gm.DebugSetHeroAt0829("Garruk","L027",3);gm.QuestPrimaryAction0829();yield return null;Check(gm.DebugBannersDestroyed0832==2&&!gm.DebugDamageKing0832(1),"Q012 first two Banner destructions spawn their waves while the King remains protected.");
            gm.DebugSetHeroAt0829("Garruk","L027",3);gm.QuestPrimaryAction0829();yield return null;gm.Heroes["Garruk"].Actions=0;gm.Heroes["Aldric"].Actions=0;gm.DealDirectDamage(gm.Enemies[kingId],12,"0.8.32 Last Stand validator",gm.Heroes["Garruk"]);yield return null;Check(gm.DebugBannersDestroyed0832==3&&gm.Enemies[kingId].Health==14&&gm.DebugKingLastStand0832,"Q012 after the third Banner common damage reaches 14 Health and triggers the printed Last Stand exactly once.");
            float guard=0f;while(gm.DebugKingImmediateRunning0832&&guard<3f){guard+=Time.unscaledDeltaTime;yield return null;}gm.Heroes["Garruk"].Actions=0;gm.Heroes["Aldric"].Actions=0;yield return gm.DebugRunKingLastStandExtra0832();Check(gm.QuestRuntime0829.GoblinKingHordeRound==gm.Round,"Q012 Last Stand executes the King's second Enemy-Phase activation through the live enemy activation engine.");
            gm.DebugSetHeroAt0829("Garruk","L027",1);gm.Heroes["Aldric"].Actions=0;gm.QuestPrimaryAction0829();guard=0f;while(gm.DebugKingImmediateRunning0832&&guard<3f){guard+=Time.unscaledDeltaTime;yield return null;}Check(gm.DebugBannersDestroyed0832==4,"Q012 fourth/final Banner is destroyed and requests the Goblin King's immediate activation.");
            int q12rep=gm.GeneralReputation;var xpBefore=gm.ParticipatingHeroes.ToDictionary(h=>h.Id,h=>h.XP);var gearBefore=gm.ParticipatingHeroes.ToDictionary(h=>h.Id,h=>h.AcquiredEquipment.Count);gm.DealDirectDamage(gm.Enemies[kingId],99,"0.8.32 finale validator",gm.Heroes["Garruk"]);yield return null;bool allLegendary=gm.ParticipatingHeroes.All(h=>h.XP==xpBefore[h.Id]+300&&h.AcquiredEquipment.Skip(gearBefore[h.Id]).Any(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&e.Rarity=="Legendary"));Check(gm.QuestComplete&&gm.DebugCampaignVictory0832&&gm.DebugDefenderUnlocked0832&&gm.GeneralReputation==q12rep+3&&allLegendary,"Q012 defeats the Goblin King after all Banners and grants 300 XP + Legendary Equipment per Hero, +3 Reputation, and Defender of Stonebrook.");

            AscendanceSaveSystem.SaveCurrent(gm,out _);var save=AscendanceSaveSystem.Load();Check(save?.QuestRuntime0829!=null&&save.QuestRuntime0829.CampaignVictory&&save.QuestRuntime0829.DefenderOfStonebrookUnlocked&&save.CaptainRolandAssistsQ0100832,"0.8.32 save state persists finale objectives, Campaign I victory, unlock, and Q009→Q010 carryover.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.32 TEST] PASS — Campaign I Q009-Q012 completed: {pass} passed, 0 failed.");else Debug.LogError($"[ASCENDANCE][0.8.32 TEST] FAIL — Campaign I Q009-Q012 completed: {pass} passed, {fail} failed.");
            yield break;
        }
    }
}
#endif
