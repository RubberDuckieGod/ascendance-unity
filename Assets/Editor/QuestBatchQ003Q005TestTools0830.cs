#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class QuestBatchQ003Q005TestTools0830
    {
        [MenuItem("Ascendance/Validation/Test 0.8.30 - Campaign I Q003-Q005 (Play Mode)")]
        public static void TestCampaignIQuestBatch()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.30 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.30 TEST] GameManager not found.");return;}gm.StartCoroutine(Run(gm));
        }
        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;void Check(bool ok,string text){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.30 TEST] PASS — "+text);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.30 TEST] FAIL — "+text);}}
            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",false);

            // Q003
            gm.DebugStartQuest0829("Q003");yield return null;
            Check(gm.CurrentQuestId0829=="Q003"&&gm.ExplorationDeck0828.Contains("L008"),"Q003 starts through the generalized Quest framework with Fallen Watchtower in its live Exploration pool.");
            gm.DebugRevealQuestLocation0829("L008","Garruk");yield return null;
            Check(gm.QuestRuntime0829.ObjectiveLocationRevealed&&gm.QuestObjects0829.Any(o=>o.Type=="Beacon")&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E004"&&!e.Defeated)==2&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E002"&&!e.Defeated)==1,"Q003 Fallen Watchtower reveal places Beacon + authored two-Hero defenders.");
            string scout=gm.DebugQuestEnemyId0829("E004");Check(gm.DebugEnemyRange0830(scout)==3,"Q003 unlit Watchtower Scout receives base ranged reach + Broken High Ground + quest +1 Range.");
            gm.DebugSetHeroAt0829("Garruk","L008",3);gm.DebugQuestEndEnemyPhase0829();yield return null;
            Check(gm.QuestRuntime0829.ReinforcementCount==1&&gm.Enemies.Values.Any(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated),"Q003 first Watchtower reinforcement adds the recurring Scout plus Goblin Raider.");
            string raider=gm.DebugQuestEnemyId0829("E001");gm.DebugDefeatQuestEnemy0829(raider,"Garruk");yield return null;
            Check(gm.QuestRuntime0829.VaultKeyGranted&&gm.DebugHasLoot0829("Garruk","LT118"),"Q003 first defeated Goblin Raider grants LT118 Watchtower Vault Key.");
            int act=gm.Heroes["Garruk"].Actions;gm.QuestPrimaryAction0829();yield return null;
            Check(gm.DebugBeaconLit0830&&gm.Heroes["Garruk"].Actions==act-2&&gm.DebugEnemyMovementAllowance(scout)==1,"Q003 Light Beacon costs 2 Actions, stops the unlit state, and removes 1 Scout Movement.");
            int gear=gm.Heroes["Garruk"].AcquiredEquipment.Count;gm.QuestSecondaryAction0829();yield return null;
            Check(gm.QuestRuntime0829.VaultOpened&&!gm.DebugHasLoot0829("Garruk","LT118")&&gm.Heroes["Garruk"].AcquiredEquipment.Count==gear+1,"Q003 sealed armory consumes LT118 and awards 1 Equipment.");
            int xp=gm.Heroes["Garruk"].XP,gold=gm.PartyGold,rep=gm.GeneralReputation;
            foreach(var e in gm.Enemies.Values.Where(e=>!e.Defeated&&e.LocationId=="L008").ToList())gm.DebugDefeatQuestEnemy0829(e.Id,"Garruk");yield return null;
            Check(gm.QuestComplete&&gm.Heroes["Garruk"].XP==xp+75&&gm.PartyGold==gold+50&&gm.GeneralReputation==rep+1&&gm.ReduceFirstWE002TravelScout0830,"Q003 completion grants 75 XP, 50 Gold, +1 Reputation, and banks the WE002 Scout reduction.");
            gm.DebugStartQuest0829("Q003");gm.DebugRevealQuestLocation0829("L008","Garruk");gm.DebugSetHeroAt0829("Garruk","L000",3);gm.DebugSetHeroAt0829("Aldric","L000",3);gm.DebugQuestEndEnemyPhase0829();gm.DebugQuestEndEnemyPhase0829();gm.DebugQuestEndEnemyPhase0829();
            Check(gm.QuestFailed&&gm.QuestRuntime0829.ObjectiveEmptyRounds>=3,"Q003 fails after 3 consecutive unoccupied rounds once Fallen Watchtower is revealed.");

            // Q004
            gm.DebugStartQuest0829("Q004");yield return null;
            Check(gm.CurrentQuestId0829=="Q004"&&gm.ExplorationDeck0828.Contains("L014"),"Q004 starts with Goblin War Camp in the authored quest pool.");
            gm.DebugRevealQuestLocation0829("L004","Garruk");yield return null;
            Check(gm.QuestRuntime0829.InitialPatrolSpawned&&gm.DebugTrailMarkers0830==1&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E004"&&!e.Defeated)>=2,"Q004 first revealed non-Stonebrook Forest places initial Scouts and Trail Marker 1.");
            string q4scout=gm.DebugQuestEnemyId0829("E004");Check(gm.DebugEnemyMovementAllowance(q4scout)==3,"Q004 Goblin Scouts gain +1 Movement while War Camp remains unrevealed.");
            gm.DebugRevealQuestLocation0829("L006","Garruk");gm.DebugRevealQuestLocation0829("L011","Garruk");yield return null;
            int campIndex=gm.ExplorationDeck0828.IndexOf("L014");Check(gm.DebugTrailMarkers0830==3&&campIndex>=0&&campIndex<3,"Q004 third Forest Trail Marker shuffles Goblin War Camp into the top 3 Exploration cards.");
            Check(gm.Enemies.Values.Any(e=>e.QuestSpawned&&e.TemplateId=="E013"&&!e.Defeated)&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated)>=2,"Q004 Trail Marker patrols spawn the authored Raider/Archer/Wolf Rider pressure.");
            gm.DebugRevealQuestLocation0829("L014","Garruk");yield return null;
            string chief=gm.DebugQuestEnemyId0829("E010");Check(gm.QuestRuntime0829.WarCampRevealed&&!string.IsNullOrEmpty(chief)&&gm.DebugEnemyMovementAllowance(q4scout)==2,"Q004 War Camp reveal spawns the War Chief and ends the Scout movement bonus.");
            gm.DebugSetHeroAt0829("Garruk","L014",3);int q4xp=gm.Heroes["Garruk"].XP,q4gold=gm.PartyGold,q4gear=gm.Heroes["Garruk"].AcquiredEquipment.Count;gm.DebugDefeatQuestEnemy0829(chief,"Garruk");yield return null;
            string q4reward=gm.Heroes["Garruk"].AcquiredEquipment.LastOrDefault();bool rare4=!string.IsNullOrEmpty(q4reward)&&RewardContent0826.Equipment.TryGetValue(q4reward,out var q4def)&&q4def.Rarity=="Rare";
            Check(gm.QuestComplete&&gm.Heroes["Garruk"].XP==q4xp+150&&gm.PartyGold==q4gold+125&&gm.Heroes["Garruk"].AcquiredEquipment.Count==q4gear+1&&rare4,"Q004 completes on War Chief defeat and grants 150 XP, 125 Gold, and a Rare Equipment card.");
            gm.DebugStartQuest0829("Q004");gm.DebugSetExplorationEmpty0830();Check(gm.QuestFailed,"Q004 fails if the Exploration deck is exhausted before Goblin War Camp is revealed.");

            // Q005
            gm.DebugStartQuest0829("Q005");gm.DebugRevealQuestLocation0829("L014","Garruk");yield return null;
            Check(gm.DebugSupplyCratesRemaining0830==4&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated)==2&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E002"&&!e.Defeated)==1,"Q005 War Camp reveal places 4 Supply Crates and the authored two-Hero opening defenders.");
            int raidersBefore=gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated);gm.DebugQuestEndEnemyPhase0829();yield return null;
            Check(gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated)==raidersBefore+1,"Q005 Camp Reinforcement adds 1 Raider while at least one Supply Crate remains.");
            gm.DebugSetHeroAt0829("Garruk","L014",3);gm.QuestPrimaryAction0829();gm.DebugSetHeroAt0829("Garruk","L014",3);gm.QuestPrimaryAction0829();yield return null;
            Check(gm.QuestRuntime0829.AlarmTokens==1&&gm.QuestRuntime0829.SupplyCratesDestroyed==2&&gm.Enemies.Values.Any(e=>e.QuestSpawned&&e.TemplateId=="E013"&&!e.Defeated),"Q005 second destroyed Crate places Alarm 1 and spawns Archer + Wolf Rider.");
            gm.DebugSetHeroAt0829("Garruk","L014",3);gm.QuestPrimaryAction0829();yield return null;string q5chief=gm.DebugQuestEnemyId0829("E010");
            Check(gm.QuestRuntime0829.AlarmTokens==2&&gm.QuestRuntime0829.SupplyCratesDestroyed==3&&!string.IsNullOrEmpty(q5chief)&&gm.Enemies.Values.Any(e=>e.QuestSpawned&&e.TemplateId=="E016"&&!e.Defeated),"Q005 third destroyed Crate places Alarm 2 and spawns War Chief + Siege Engineer + Raider.");
            gm.DebugSetHeroAt0829("Garruk","L014",3);gm.QuestPrimaryAction0829();yield return null;
            Check(gm.QuestRuntime0829.SupplyCratesDestroyed==4&&gm.DebugSupplyCratesRemaining0830==0,"Q005 fourth destroyed Crate clears the final Supply objective and triggers Final Defense.");
            int afterCrates=gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated);gm.DebugQuestEndEnemyPhase0829();Check(gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated)==afterCrates,"Q005 Camp Reinforcements stop immediately once all Supply Crates are destroyed.");
            Check(gm.DebugEffectiveEnemyAttack(q5chief)>=6,"Q005 Goblin War Chief receives the authored War Camp Organized Defense +1 Attack while there.");
            int q5xp=gm.Heroes["Garruk"].XP,q5gold=gm.PartyGold,q5gear=gm.Heroes["Garruk"].AcquiredEquipment.Count;gm.DebugDefeatQuestEnemy0829(q5chief,"Garruk");yield return null;
            string q5reward=gm.Heroes["Garruk"].AcquiredEquipment.LastOrDefault();bool rare5=!string.IsNullOrEmpty(q5reward)&&RewardContent0826.Equipment.TryGetValue(q5reward,out var q5def)&&q5def.Rarity=="Rare";
            Check(gm.QuestComplete&&gm.Heroes["Garruk"].XP==q5xp+175&&gm.PartyGold==q5gold+150&&gm.Heroes["Garruk"].AcquiredEquipment.Count==q5gear+1&&rare5&&gm.RemoveEliteFromQ007Setup0830,"Q005 completes after Crates + War Chief and grants 175 XP, 150 Gold, Rare Equipment, and banks the Q007 Elite reduction.");
            bool saved=AscendanceSaveSystem.SaveCurrent(gm,out var msg);var data=saved?AscendanceSaveSystem.Load():null;
            Check(saved&&data!=null&&data.QuestId=="Q005"&&data.QuestRuntime0829!=null&&data.QuestRuntime0829.SupplyCratesDestroyed==4&&data.RemoveEliteFromQ007Setup0830&&data.ReduceFirstWE002TravelScout0830,"0.8.30 save state persists Q003–Q005 objective state and cross-Quest campaign effects.");
            Check(gm.HeroAvatars.Count==gm.PartySize,"Q003–Q005 transitions keep world Hero avatars reconciled to the active party.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.30 TEST] PASS — Campaign I Q003-Q005 completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.30 TEST] FAIL — Campaign I Q003-Q005 completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
