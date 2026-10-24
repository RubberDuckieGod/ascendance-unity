#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class QuestFrameworkTestTools0829
    {
        [MenuItem("Ascendance/Validation/Test 0.8.29 - Generalized Quest Framework & Q002 (Play Mode)")]
        public static void TestQuestFrameworkQ002()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.29 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.29 TEST] GameManager not found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string text){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.29 TEST] PASS — "+text);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.29 TEST] FAIL — "+text);}}

            Check(gm.CurrentQuestId0829=="Q001"&&gm.CurrentQuestDefinition0829!=null,"Generalized Quest runtime starts with Q001 definition active.");
            Check(!gm.ExplorationDeck0828.Contains("L005"),"Q001 Exploration deck excludes Q002 Merchant Caravan.");
            Check(gm.Enemies.ContainsKey("Captain")&&gm.Enemies["Captain"].MaxHealth==18,"Q001 keeps the runtime-verified fixed Captain instead of replacing it with a generic template.");

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",false);
            gm.DebugStartQuest0829("Q002"); yield return null;
            Check(gm.CurrentQuestId0829=="Q002"&&!gm.QuestComplete&&!gm.QuestFailed,"Starting Q002 resets outcome state and changes the active Quest identity.");
            Check(gm.ExplorationDeck0828.Contains("L005"),"Q002 Exploration deck includes Merchant Caravan.");
            Check(!gm.Enemies.ContainsKey("Captain")&&!gm.Enemies.ContainsKey("RaiderA"),"Q002 removes Q001 fixed scenario enemies.");

            gm.DebugRevealQuestLocation0829("L005","Garruk"); yield return null;
            Check(gm.QuestRuntime0829.CaravanRevealed&&gm.QuestObjects0829.Count(o=>o.Type=="Wagon")==2&&gm.DebugWagonSupply0829(0)==4&&gm.DebugWagonSupply0829(1)==4,"Merchant Caravan reveal creates exactly 2 Wagons with the locked 4 Supply each.");
            Check(gm.HeroAvatars.Count==gm.PartySize&&gm.HeroAvatars.Keys.All(id=>gm.Heroes.TryGetValue(id,out var h)&&h.Participating),"Quest transition presentation removes stale non-participating Hero avatars.");
            Check(gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E001"&&!e.Defeated)==2&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E004"&&!e.Defeated)==1,"Two-Hero Q002 reveal scaling spawns 2 Raiders + 1 Scout as authored.");
            gm.DebugSetWagon0829(0,"L005",0,false,true);gm.DebugSetWagon0829(1,"L005",0,false,true);gm.DebugEvaluateQuestOutcome0829();
            Check(gm.QuestFailed&&!gm.QuestComplete,"Q002 fails when both Wagons are destroyed.");
            gm.DebugStartQuest0829("Q002");gm.DebugRevealQuestLocation0829("L005","Garruk");yield return null;

            string raider=gm.DebugQuestEnemyId0829("E001");
            gm.DebugSetHeroAt0829("Garruk","L000");gm.DebugSetHeroAt0829("Aldric","L000");
            int before=gm.DebugWagonSupply0829(0);gm.DebugQuestEnemyStartActivation0829(raider);
            Check(gm.DebugWagonSupply0829(0)==before-1&&gm.DebugWagonSupply0829(1)==4,"An unattended Wagon loses exactly 1 Supply at Goblin activation start.");
            gm.DebugSetHeroAt0829("Garruk","L005");before=gm.DebugWagonSupply0829(0);gm.DebugQuestEnemyStartActivation0829(raider);
            Check(gm.DebugWagonSupply0829(0)==before,"A living Hero sharing the Wagon Location suppresses the Supply loss.");

            gm.DebugSetHeroAt0829("Garruk","L005",3);int actions=gm.Heroes["Garruk"].Actions;gm.QuestPrimaryAction0829();
            var move=gm.RewardChoiceOptions.FirstOrDefault(o=>o.Key.Contains("|L002"));
            Check(gm.HasPendingRewardChoice&&move!=null,"Move Wagon opens a player-facing choice over connected revealed destinations.");
            if(move!=null)gm.DebugChooseRewardChoice(move.Key);yield return null;
            Check(gm.DebugWagonLocation0829(0)=="L002"&&gm.Heroes["Garruk"].Actions==actions-1,"Moving a Wagon uses the real 1-Action escort rule.");

            gm.DebugSetHeroAt0829("Garruk","L005",3);int lootBefore=gm.Heroes["Garruk"].LootInventory.Count;gm.SearchCurrentLocation();yield return null;
            Check(gm.DebugHasLoot0829("Garruk","LT123")&&gm.Heroes["Garruk"].LootInventory.Count==lootBefore+1,"First Merchant Caravan Search grants LT123 Caravan Manifest instead of a normal Search reward.");

            gm.DebugQuestEndEnemyPhase0829();yield return null;
            Check(gm.QuestRuntime0829.ReinforcementCount==1&&gm.Enemies.Values.Any(e=>e.QuestSpawned&&e.TemplateId=="E002"&&!e.Defeated),"Q002 reinforcement wave 1 spawns the far-distance Raider plus Merchant Caravan Archer.");
            gm.DebugQuestEndEnemyPhase0829();yield return null;
            Check(gm.QuestRuntime0829.ReinforcementCount==2&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.TemplateId=="E004"&&!e.Defeated)>=2,"Q002 reinforcement wave 2 adds the Merchant Caravan Scout.");
            gm.DebugQuestEndEnemyPhase0829();yield return null;
            string captain=gm.DebugQuestEnemyId0829("E006");
            Check(gm.QuestRuntime0829.ReinforcementCount==3&&gm.QuestRuntime0829.CaptainSpawned&&!string.IsNullOrEmpty(captain),"Q002 reinforcement wave 3 spawns the Goblin Captain and stops recurring waves.");
            int wave=gm.QuestRuntime0829.ReinforcementCount;gm.DebugQuestEndEnemyPhase0829();
            Check(gm.QuestRuntime0829.ReinforcementCount==wave,"Q002 recurring reinforcements stop once the Goblin Captain enters play.");

            gm.DebugDefeatQuestEnemy0829(captain,"Garruk");yield return null;
            Check(gm.QuestRuntime0829.StrongboxKeyGranted&&gm.DebugHasLoot0829("Garruk","LT119"),"Defeating the Q002 Goblin Captain grants LT119 Merchant Strongbox Key.");
            gm.DebugSetHeroAt0829("Garruk","L005",3);int invBefore=gm.Heroes["Garruk"].LootInventory.Count;gm.QuestSecondaryAction0829();yield return null;
            string newest=gm.Heroes["Garruk"].LootInventory.LastOrDefault();bool sourceMatched=!string.IsNullOrEmpty(newest)&&newest!="LT119"&&RewardContent0826.Loot.TryGetValue(newest,out var ld)&&!ld.QuestItem&&((ld.DropTags??"").IndexOf("Merchant",System.StringComparison.OrdinalIgnoreCase)>=0||(ld.DropTags??"").IndexOf("Caravan",System.StringComparison.OrdinalIgnoreCase)>=0);
            Check(gm.QuestRuntime0829.StrongboxOpened&&!gm.DebugHasLoot0829("Garruk","LT119")&&sourceMatched,"Strongbox consumes LT119 and awards source-matched non-Quest Loot.");

            // Rescue both Wagons through the real move-choice resolver from Forest Path into Stonebrook.
            gm.DebugSetWagon0829(0,"L002",2);gm.DebugSetWagon0829(1,"L002",2);
            int gold0=gm.PartyGold,xp0=gm.Heroes["Garruk"].XP,gear0=gm.Heroes["Garruk"].AcquiredEquipment.Count;
            for(int i=0;i<2;i++)
            {
                gm.DebugSetHeroAt0829("Garruk","L002",3);gm.QuestPrimaryAction0829();var toTown=gm.RewardChoiceOptions.FirstOrDefault(o=>o.Key.Contains("|L000"));if(toTown!=null)gm.DebugChooseRewardChoice(toTown.Key);yield return null;
            }
            Check(gm.QuestRuntime0829.DeliveredSupply==4&&gm.DebugWagonRescued0829(0)&&gm.DebugWagonRescued0829(1),"Rescuing both Wagons through Stonebrook records exactly 4 delivered Supply.");
            Check(gm.QuestComplete&&!gm.QuestFailed,"Q002 completes only after both Wagons are rescued with at least 4 delivered Supply.");
            Check(gm.PartyGold==gold0+100&&gm.Heroes["Garruk"].XP==xp0+125&&gm.Heroes["Garruk"].AcquiredEquipment.Count==gear0+1&&gm.NextQuestSupply0829==1,"Q002 one-time reward grants 125 XP, 100 Gold, 1 Equipment, and banks +1 Supply.");
            Check(!gm.DebugHasLoot0829("Garruk","LT123")&&gm.LootDeck.Contains("LT123"),"LT123 Caravan Manifest returns to the Loot Deck when Q002 ends.");

            bool saved=AscendanceSaveSystem.SaveCurrent(gm,out var message);var data=saved?AscendanceSaveSystem.Load():null;
            Check(saved&&data!=null&&data.QuestId=="Q002"&&data.QuestRuntime0829!=null&&data.QuestRuntime0829.Objects.Count==2&&data.NextQuestSupply0829==1,"Save data persists active Quest identity, Quest objects, and cross-Quest Supply state.");
            Check(gm.Enemies.Values.Where(e=>e.QuestSpawned).All(e=>e.QuestId=="Q002"),"Dynamic Q002 enemies carry explicit Quest ownership instead of being mistaken for Travel Encounter enemies.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.29 TEST] PASS — Generalized Quest Framework & Q002 completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.29 TEST] FAIL — Generalized Quest Framework & Q002 completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
