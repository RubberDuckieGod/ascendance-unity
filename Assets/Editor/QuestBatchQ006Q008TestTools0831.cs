#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class QuestBatchQ006Q008TestTools0831
    {
        [MenuItem("Ascendance/Validation/Test 0.8.31 - Campaign I Q006-Q008 (Play Mode)")]
        public static void TestCampaignIQuestBatch2(){if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.31 TEST] Enter Play Mode first.");return;}var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.31 TEST] No AscendanceGameManager found.");return;}gm.StartCoroutine(Run(gm));}
        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.31 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.31 TEST] FAIL — "+label);}}
            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);yield return null;

            gm.DebugStartQuest0829("Q006");yield return null;Check(gm.CurrentQuestId0829=="Q006"&&gm.ExplorationDeck0828.Contains("L018"),"Q006 starts with Prison Pens in the live Exploration pool.");
            gm.DebugRevealQuestLocation0829("L018","Garruk");yield return null;Check(gm.QuestObjects0829.Count(o=>o.Type=="Prisoner")==6&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&!e.Defeated&&e.LocationId=="L018")==4,"Q006 Prison Pens reveal places 6 Prisoners plus printed two-Hero guards.");
            gm.DebugSetHeroAt0829("Garruk","L018",3);gm.QuestPrimaryAction0829();yield return null;Check(gm.DebugPrisonersFreed0831==1&&gm.DebugQuestEnemyId0829("E011")!=null,"Q006 first freed Prisoner costs 1 Action and triggers the Saboteur alarm for 2 Heroes.");
            gm.DebugSetHeroAt0829("Garruk","L018",3);gm.QuestPrimaryAction0829();gm.DebugSetHeroAt0829("Garruk","L018",3);gm.QuestPrimaryAction0829();yield return null;Check(gm.DebugPrisonersFreed0831==3&&gm.Enemies.Values.Any(e=>e.QuestSpawned&&!e.Defeated&&e.TemplateId=="E002")&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&!e.Defeated&&e.TemplateId=="E001")>=3,"Q006 third freed Prisoner triggers Archer + Raider reinforcement.");
            gm.DebugSetHeroAt0829("Garruk","L000",3);gm.DebugSyncPrisoners0831();yield return null;Check(gm.DebugPrisonersRescued0831==3,"Q006 freed Prisoners move with their rescuer and are rescued on reaching Stonebrook.");
            gm.DebugSetHeroAt0829("Garruk","L018",3);gm.QuestPrimaryAction0829();gm.DebugSetHeroAt0829("Garruk","L000",3);gm.DebugSyncPrisoners0831();string cap6=gm.DebugQuestEnemyId0829("E006");gm.DebugDefeatQuestEnemy0829(cap6,"Garruk");yield return null;Check(gm.QuestComplete&&gm.DebugPrisonersRescued0831>=4&&gm.PartyMorale0831>=1,"Q006 completes at 4 rescued + Captain defeated and banks +1 Party Morale.");
            gm.DebugStartQuest0829("Q006");gm.DebugRevealQuestLocation0829("L018","Garruk");gm.DebugForcePrisonExecution0831();gm.DebugForcePrisonExecution0831();gm.DebugForcePrisonExecution0831();gm.DebugForcePrisonExecution0831();yield return null;Check(gm.QuestFailed&&gm.DebugPrisonersExecuted0831==4,"Q006 fails when 4 Prisoners are executed.");

            gm.DebugSetRemoveEliteFromQ007Setup0831(true);gm.DebugStartQuest0829("Q007");yield return null;gm.DebugRevealQuestLocation0829("L005","Garruk");yield return null;Check(gm.QuestObjects0829.Count(o=>o.Type=="SupplyWagon")==3,"Q007 Merchant Caravan reveal places 3 Supply Wagons.");
            Check(!gm.Enemies.Values.Any(e=>e.QuestSpawned&&!e.Defeated&&e.TemplateId=="E013"),"Q007 honors Q005 campaign reward by removing the initial Elite Wolf Rider.");
            gm.DebugSetHeroAt0829("Garruk","L005",3);gm.QuestPrimaryAction0829();gm.DebugSetHeroAt0829("Garruk","L005",3);gm.QuestPrimaryAction0829();yield return null;Check(gm.DebugSupplyWagonsDestroyed0831==1&&gm.Enemies.Values.Any(e=>e.QuestSpawned&&!e.Defeated&&e.TemplateId=="E002"),"Q007 two Sabotage Actions destroy a Wagon and first-destroyed wave adds an Archer.");
            foreach(var id in new[]{"L012","L013","L016"})gm.DebugRevealQuestLocation0829(id,"Garruk");gm.DebugQuestEndEnemyPhase0829();yield return null;Check(gm.QuestObjects0829.Where(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued).Any(o=>o.LocationId!="L005"),"Q007 active Supply Wagons move one connected revealed step toward the designated exit path each Enemy Phase.");
            var live=gm.QuestObjects0829.Where(o=>o.Type=="SupplyWagon"&&!o.Destroyed&&!o.Rescued).OrderBy(o=>o.ObjectId).ToList();foreach(var w in live){gm.DebugSetSupplyWagonLocation0831(gm.QuestObjects0829.Where(o=>o.Type=="SupplyWagon").OrderBy(o=>o.ObjectId).ToList().IndexOf(w),"L005");gm.DebugSetHeroAt0829("Garruk","L005",3);gm.QuestPrimaryAction0829();gm.DebugSetHeroAt0829("Garruk","L005",3);gm.QuestPrimaryAction0829();}yield return null;Check(gm.QuestComplete&&gm.DebugSupplyWagonsDestroyed0831==3&&gm.RemoveSiegeEngineFromQ008Setup0831,"Q007 completes after all 3 Wagons are destroyed and banks removal of 1 Siege Engine from Q008.");
            gm.DebugStartQuest0829("Q007");gm.DebugRevealQuestLocation0829("L005","Garruk");gm.DebugRevealQuestLocation0829("L016","Garruk");gm.DebugSetSupplyWagonLocation0831(0,"L016");gm.DebugSetSupplyWagonLocation0831(1,"L016");gm.DebugQuestEndEnemyPhase0829();yield return null;Check(gm.QuestFailed&&gm.DebugSupplyWagonsEscaped0831>=2,"Q007 fails immediately when any 2 Supply Wagons escape.");

            gm.DebugStartQuest0829("Q008");yield return null;Check(gm.DebugWallTrack0831==6,"Q008 starts Stonebrook Wall Track at 6.");
            gm.DebugRevealQuestLocation0829("L011","Garruk");yield return null;Check(gm.DebugSiegeEnginesPlaced0831==1&&gm.QuestObjects0829.Any(o=>o.Type=="SiegeEngine"&&o.MaxHealth==10&&o.Armor==2),"Q008 places the campaign-reduced single Siege Engine with playtest stats 10 Health / 2 Armor.");
            string archer=gm.DebugQuestEnemyId0829("E002");Check(!string.IsNullOrEmpty(archer)&&gm.DebugQuestEnemyAttack0831(archer)==gm.Enemies[archer].Attack+1,"WE003 Archer modifier gives quest Archers exactly +1 Attack during Q008.");
            int wall=gm.DebugWallTrack0831;gm.DebugQuestEndEnemyPhase0829();yield return null;Check(gm.DebugWallTrack0831==wall-1,"Q008 Bombardment removes 1 Wall per active Siege Engine.");
            gm.DebugSetHeroAt0829("Garruk","L011",3);gm.DebugSetActiveSiegeEngineHealth0831(1);gm.QuestPrimaryAction0829();yield return null;Check(gm.DebugSiegeEnginesDestroyed0831==1&&gm.DebugQuestEnemyId0829("E015")!=null,"Q008 basic objective Attack destroys the last Siege Engine and immediately spawns the Goblin Champion.");
            string champ=gm.DebugQuestEnemyId0829("E015");int xp8=gm.Heroes["Garruk"].XP,gold8=gm.PartyGold,gear8=gm.Heroes["Garruk"].AcquiredEquipment.Count;if(!string.IsNullOrEmpty(champ)&&gm.Enemies.TryGetValue(champ,out var champEnemy)){gm.DebugSetHeroAt0829("Garruk",champEnemy.LocationId,3);gm.DebugDefeatQuestEnemy0829(champ,"Garruk");yield return null;bool epic=gm.Heroes["Garruk"].AcquiredEquipment.Skip(gear8).Any(id=>RewardContent0826.Equipment.TryGetValue(id,out var e)&&e.Rarity=="Epic");Check(gm.QuestComplete&&gm.Heroes["Garruk"].XP==xp8+200&&gm.PartyGold==gold8+175&&epic,"Q008 completes after Engine + Champion and grants 200 XP, 175 Gold, and Epic Equipment.");}else{Check(false,"Q008 completion check requires the spawned Goblin Champion and now fails safely instead of indexing a null enemy ID.");}
            gm.DebugStartQuest0829("Q008");gm.DebugRevealQuestLocation0829("L011","Garruk");for(int i=0;i<6&&!gm.QuestFailed;i++)gm.DebugQuestEndEnemyPhase0829();yield return null;Check(gm.QuestFailed&&gm.DebugWallTrack0831==0,"Q008 fails when Stonebrook Wall Track reaches 0.");

            AscendanceSaveSystem.SaveCurrent(gm,out _);var save=AscendanceSaveSystem.Load();Check(save?.QuestRuntime0829!=null&&save.QuestRuntime0829.WallTrack==gm.DebugWallTrack0831&&save.RemoveSiegeEngineFromQ008Setup0831&&save.PartyMorale0831>=1,"0.8.31 save state persists Q006-Q008 objectives and cross-Quest campaign effects.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.31 TEST] PASS — Campaign I Q006-Q008 completed: {pass} passed, 0 failed.");else Debug.LogError($"[ASCENDANCE][0.8.31 TEST] FAIL — Campaign I Q006-Q008 completed: {pass} passed, {fail} failed.");
            yield break;
        }
    }
}
#endif
