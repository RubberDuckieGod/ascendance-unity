#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CraftingRecipeRuntimeTestTools0855
    {
        [MenuItem("Ascendance/Validation/Test 0.8.55 - Full Crafting Recipe Runtime Execution (Play Mode)")]
        public static void Test0855()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.55 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.55 TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.55 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.55 TEST] FAIL — "+label);}}

            Check(AscendanceGameManager.BuildVersion=="0.8.55","Build identifies the Full Crafting Recipe Runtime Execution milestone.");
            FullContentCatalog0850.EnsureLoaded();
            Check(FullContentCatalog0850.CraftingRecipeCount==200,"full authored catalog contains all 200 Crafting Recipe definitions.");
            Check(CraftingContent0855.Recipes.Count==200,"live 0.8.55 Crafting catalog exposes all 200 recipes.");
            Check(Enumerable.Range(1,200).Select(i=>$"CR{i:000}").All(id=>CraftingContent0855.Get(id)!=null),"Crafting Recipe IDs are contiguous CR001-CR200.");
            Check(CraftingContent0855.Recipes.All(r=>gm.IsCraftingRecipeExecutable0855(r.Id)),"all CR001-CR200 have executable ingredient/output runtime paths.");
            Check(CraftingContent0855.Recipes.Select(r=>r.AuthoredProfession).Distinct().Count()==14,"all 14 authored crafting specialties are preserved.");
            Check(CraftingContent0855.Recipes.Select(r=>r.CoreProfession).Distinct().Count()==4,"all authored specialties map into the four locked player profession tracks.");
            Check(CraftingContent0855.Recipes.All(r=>r.RequiredLevel>=1&&r.RequiredLevel<=5&&r.ProfessionXp>0),"all authored tiers map to Level 1-5 progression and profession XP.");
            Check(CraftingContent0855.Recipes.All(r=>r.Materials.Count>0&&r.Materials.All(m=>m.Count>0&&(RewardContent0826.Loot.ContainsKey(m.Id)||RewardContent0826.Equipment.ContainsKey(m.Id)))),"every authored ingredient resolves to a live Loot or Equipment definition.");
            Check(CraftingContent0837.Recipes.Count==20,"legacy 0.8.37 recipe adapter remains intact for backward compatibility.");
            Check(RewardContent0826.Loot.Count==190&&RewardContent0826.Equipment.Count==144,"0.8.54a Loot/Equipment catalogs remain intact.");
            Check(gm.DebugCampaignIEncounterDeckDefinitionCount0853()==30,"0.8.53 Campaign I Travel Encounter boundary remains intact.");

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugStartQuest0829("Q001");gm.DebugResolveCampaignQuest0833(true);yield return null;
            var g=gm.Heroes["Garruk"];var a=gm.Heroes["Aldric"];
            g.LootInventory.Clear();g.AcquiredEquipment.Clear();a.LootInventory.Clear();a.AcquiredEquipment.Clear();
            gm.DebugSetPartyGold0855(1000);
            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing,0);
            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Alchemy,0);
            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Enchanting,0);
            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Artificing,0);

            gm.DebugGiveCraftingInput0855(a.Id,"LT031",2);gm.DebugGiveCraftingInput0855(a.Id,"LT034",1);
            Check(!gm.CanCraftRecipe0855("CR001",g.Id),"Hero-owned crafting does not borrow another Hero's materials.");
            gm.DebugGiveCraftingInput0855(g.Id,"LT031",2);gm.DebugGiveCraftingInput0855(g.Id,"LT034",1);int gold0=gm.PartyGold;
            Check(gm.CanCraftRecipe0855("CR001",g.Id),"CR001 becomes READY with the crafter's personal materials and Party Gold.");
            Check(gm.CraftRecipe0855("CR001",g.Id)&&g.AcquiredEquipment.Contains("EQ002")&&gm.CraftingMaterialCount0855(g.Id,"LT031")==0&&gm.PartyGold==gold0-10,"CR001 consumes exact materials/Gold and creates Iron Shortsword Equipment.");
            Check(gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing)==40,"successful Tier I crafting grants 40 profession XP.");
            Check(!gm.CanCraftRecipe0855("CR010",g.Id),"Tier II recipe remains level-gated below Blacksmithing Level 2.");

            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Alchemy,0);gm.DebugGiveCraftingInput0855(g.Id,"LT048",1);gm.DebugGiveCraftingInput0855(g.Id,"LT034",1);int oilBefore=g.LootInventory.Count(x=>x=="LT024");
            Check(gm.CraftRecipe0855("CR141",g.Id)&&g.LootInventory.Count(x=>x=="LT024")==oilBefore+1,"CR141 creates its authored LT024 Lantern Oil Loot output.");

            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing,1000);gm.DebugGiveCraftingInput0855(g.Id,"EQ078",1);gm.DebugGiveCraftingInput0855(g.Id,"LT039",1);gm.DebugGiveCraftingInput0855(g.Id,"LT087",2);
            Check(gm.CraftRecipe0855("CR101",g.Id)&&g.AcquiredEquipment.Contains("EQ080")&&!g.AcquiredEquipment.Contains("EQ078"),"CR101 consumes unequipped Equipment as an authored ingredient and creates EQ080 Spiked Shield.");

            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Artificing,100);gm.DebugGiveCraftingInput0855(g.Id,"LT043",3);gm.DebugGiveCraftingInput0855(g.Id,"LT038",1);int special0=gm.CraftingSpecialResults0855.Count;
            Check(gm.CraftRecipe0855("CR159",g.Id)&&gm.CraftingSpecialResults0855.Count==special0+1&&gm.CraftingSpecialResults0855.Last().StartsWith("CR159|Q001|"),"CR159 produces persistent Q001 Stonebrook Barricade crafting state.");

            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Enchanting,1000);gm.DebugGiveCraftingInput0855(g.Id,"LT037",1);gm.DebugGiveCraftingInput0855(g.Id,"LT044",2);gm.DebugGiveCraftingInput0855(g.Id,"LT105",1);gm.DebugGiveCraftingInput0855(g.Id,"LT159",1);
            Check(!gm.CanCraftRecipe0855("CR121",g.Id),"Legendary Forge recipe remains locked without LT155 Eternal Forge Ember.");
            gm.DebugGiveCraftingInput0855(g.Id,"LT155",1);Check(gm.CanCraftRecipe0855("CR121",g.Id),"carrying LT155 unlocks the Legendary Forge requirement when all other costs are met.");

            gm.DebugGiveCraftingInput0855(g.Id,"LT157",1);gm.DebugGiveCraftingInput0855(g.Id,"LT070",2);gm.DebugGiveCraftingInput0855(g.Id,"LT103",1);int relicState=gm.CraftingSpecialResults0855.Count;
            Check(gm.CraftRecipe0855("CR191",g.Id)&&gm.CraftingSpecialResults0855.Count==relicState+1&&gm.CraftingSpecialResults0855.Last().Contains("Phoenix Heart Reliquary"),"CR191 Artifact Restoration executes into persistent Legendary Treasure result state.");
            Check(g.LootInventory.Contains("LT155"),"unlock/tool catalyst LT155 is retained when the authored material list does not consume it.");

            gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Alchemy,250);gm.DebugGiveCraftingInput0855(g.Id,"LT087",1);gm.DebugGiveCraftingInput0855(g.Id,"LT055",1);gm.DebugGiveCraftingInput0855(g.Id,"LT085",1);
            Check(!gm.CanCraftRecipe0855("CR177",g.Id),"enemy-defeat-gated CR177 remains locked before its Warg prerequisite is recorded.");
            gm.DebugRecordEnemyTemplateDefeat0855("E029");Check(gm.CanCraftRecipe0855("CR177",g.Id),"recorded Warg defeat unlocks CR177 Warg-Hunter Salve.");

            int savedSpecial=gm.CraftingSpecialResults0855.Count;gm.DebugGrantCraftingUnlock0855("Campaign permits Ascendance crafting");
            var save=new AscendanceSaveData{CraftingSpecialResults0855=gm.CraftingSpecialResults0855.ToList(),CraftingUnlockFlags0855=gm.CraftingUnlockFlagsForSave0855.ToList(),DefeatedEnemyTemplateIds0855=gm.DefeatedEnemyTemplateIdsForSave0855.ToList()};
            gm.RestoreCraftingState0855(new AscendanceSaveData());
            Check(gm.CraftingSpecialResults0855.Count==0,"0.8.55 crafting state can be cleared during load replacement.");
            gm.RestoreCraftingState0855(save);
            Check(gm.CraftingSpecialResults0855.Count==savedSpecial&&gm.CraftingUnlockFlagsForSave0855.Contains("Campaign permits Ascendance crafting")&&gm.DefeatedEnemyTemplateIdsForSave0855.Contains("E029"),"special results, campaign unlocks, and defeat prerequisites restore from save-facing state.");

            Check(CraftingContent0855.ForProfession(DowntimeProfession0836.Blacksmithing).Count+CraftingContent0855.ForProfession(DowntimeProfession0836.Alchemy).Count+CraftingContent0855.ForProfession(DowntimeProfession0836.Enchanting).Count+CraftingContent0855.ForProfession(DowntimeProfession0836.Artificing).Count==200,"the four Crafting UI tabs collectively expose all 200 authored recipes.");
            Check(string.IsNullOrEmpty(gm.PendingContentInstruction),"0.8.55 validation path finishes without a Crafting CONTENT PENDING fallback.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.55 TEST] PASS — Full Crafting Recipe Runtime Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.55 TEST] FAIL — Full Crafting Recipe Runtime Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
