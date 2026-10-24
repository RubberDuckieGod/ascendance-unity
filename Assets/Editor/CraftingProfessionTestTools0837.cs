#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class CraftingProfessionTestTools0837
    {
        [MenuItem("Ascendance/Validation/Test 0.8.37 - Hero-Owned Crafting & Professions (Play Mode)")]
        public static void TestCrafting()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.37 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.37 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.37 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.37 TEST] FAIL — "+label);}}

            string path=AscendanceSaveSystem.SavePath;
            bool hadSave=File.Exists(path);
            byte[] previousSave=hadSave?File.ReadAllBytes(path):null;

            try
            {
                gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);
                gm.DebugResetCampaignProgress0833();
                gm.DebugResolveCampaignQuest0833(true);
                yield return null;
                var g=gm.Heroes["Garruk"];var a=gm.Heroes["Aldric"];
                g.LootInventory.Clear();a.LootInventory.Clear();
                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing,0);
                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Alchemy,0);
                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Enchanting,0);
                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Artificing,0);
                gm.DebugSetProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing,0);
                gm.DebugSetProfessionXp0837(a.Id,DowntimeProfession0836.Alchemy,0);
                gm.DebugSetProfessionXp0837(a.Id,DowntimeProfession0836.Enchanting,0);
                gm.DebugSetProfessionXp0837(a.Id,DowntimeProfession0836.Artificing,0);

                Check(gm.IsDowntimeAvailable0836&&gm.CanUseDowntimeActions0836,"Crafting is available only in the verified between-Quest downtime state.");
                Check(CraftingContent0837.Recipes.Count==20,"Crafting catalog contains 20 authored recipes.");
                Check(System.Enum.GetValues(typeof(DowntimeProfession0836)).Cast<DowntimeProfession0836>().All(p=>CraftingContent0837.ForProfession(p).Count==5),"Each of the four professions has exactly 5 recipes.");

                a.LootInventory.AddRange(new[]{"LT055","LT055","LT056"});
                Check(!gm.CanCraftRecipe0837("CR-AL-01",g.Id)&&gm.CanCraftRecipe0837("CR-AL-01",a.Id),"A Hero cannot use another Hero's materials to satisfy a recipe.");
                Check(gm.DowntimeProfessionMaterialCount0837(g.Id,DowntimeProfession0836.Alchemy)==0&&gm.DowntimeProfessionMaterialCount0837(a.Id,DowntimeProfession0836.Alchemy)==3,"Profession material counts are isolated per Hero, not pooled across the party.");
                a.LootInventory.Clear();

                g.LootInventory.AddRange(new[]{"LT055","LT055","LT056"});
                int potionBefore=g.HealingPotions, goldBefore=gm.PartyGold, aldricLootBefore=a.LootInventory.Count;
                bool potionCrafted=gm.CraftRecipe0837("CR-AL-01",g.Id);
                Check(potionCrafted&&g.HealingPotions==potionBefore+1,"Healing Potion recipe creates a usable Healing Potion for the crafting Hero.");
                Check(!g.LootInventory.Contains("LT055")&&!g.LootInventory.Contains("LT056"),"Crafting consumes ingredients only from the crafting Hero's inventory.");
                Check(a.LootInventory.Count==aldricLootBefore,"Crafting leaves every other Hero's inventory unchanged.");
                Check(gm.PartyGold==goldBefore,"Crafting does not consume shared Party Gold; Gold remains the only party-owned resource.");
                Check(gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Alchemy)==40&&gm.ProfessionLevel0837(g.Id,DowntimeProfession0836.Alchemy)==1,"Successful crafting awards profession XP to the crafting Hero only.");
                Check(!gm.CanCraftRecipe0837("CR-AL-03",g.Id),"Level-gated Alchemy recipes remain locked below their required profession level.");

                g.LootInventory.AddRange(new[]{"LT055","LT055","LT056","LT055","LT055","LT056"});
                gm.CraftRecipe0837("CR-AL-01",g.Id);gm.CraftRecipe0837("CR-AL-01",g.Id);
                Check(gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Alchemy)==120&&gm.ProfessionLevel0837(g.Id,DowntimeProfession0836.Alchemy)==2,"Profession XP advances the Hero from Level 1 to Level 2 at the authored threshold.");
                g.LootInventory.AddRange(new[]{"LT055","LT056","LT058"});
                bool tonicCrafted=gm.CraftRecipe0837("CR-AL-03",g.Id);
                Check(tonicCrafted&&g.LootInventory.Contains("LT007"),"Unlocked Alchemy recipe creates its persistent authored consumable output.");

                g.LootInventory.AddRange(new[]{"LT038","LT038","LT049"});
                bool daggerCrafted=gm.CraftRecipe0837("CR-BS-02",g.Id);
                Check(daggerCrafted&&g.AcquiredEquipment.Contains("EQ037"),"Blacksmithing creates authored Equipment in the crafting Hero's unequipped inventory.");
                Check(gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing)==40,"Blacksmithing progression is tracked independently from Alchemy progression.");

                g.LootInventory.AddRange(new[]{"LT032","LT047"});
                bool lockpicksCrafted=gm.CraftRecipe0837("CR-AR-01",g.Id);
                Check(lockpicksCrafted&&g.LootInventory.Contains("LT026")&&gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Artificing)==40,"Artificing creates persistent authored utility items and gains its own profession XP.");

                g.LootInventory.AddRange(new[]{"LT061","LT064","LT069"});
                bool focusCrafted=gm.CraftRecipe0837("CR-EN-01",g.Id);
                Check(focusCrafted&&g.AcquiredEquipment.Contains("EQ139")&&gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Enchanting)==40,"Enchanting creates authored Equipment and advances independently.");

                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing,700);
                g.LootInventory.AddRange(new[]{"LT037","LT037","LT100","LT070"});
                Check(!gm.CanCraftRecipe0837("CR-BS-05",g.Id),"Legendary Blacksmithing remains locked without Eternal Forge Ember even at profession Level 5.");
                g.LootInventory.Add("LT155");
                bool dragonsteel=gm.CraftRecipe0837("CR-BS-05",g.Id);
                Check(dragonsteel&&g.AcquiredEquipment.Contains("EQ008")&&g.LootInventory.Contains("LT155"),"Eternal Forge Ember unlocks Legendary Blacksmithing and is not consumed as an ingredient.");

                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Enchanting,700);
                g.LootInventory.AddRange(new[]{"LT066","LT062","LT062","LT067"});
                Check(!gm.CanCraftRecipe0837("CR-EN-05",g.Id),"Advanced Enchanting remains locked without Codex of Eternity.");
                g.LootInventory.Add("LT158");
                bool starfall=gm.CraftRecipe0837("CR-EN-05",g.Id);
                Check(starfall&&g.AcquiredEquipment.Contains("EQ120")&&g.LootInventory.Contains("LT158"),"Codex of Eternity unlocks Advanced Enchanting and remains with its Hero after crafting.");

                a.LootInventory.Add("LT031");
                int savedAlchemyXp=g.AlchemyXp0837,savedBlacksmithXp=g.BlacksmithingXp0837,savedPotions=g.HealingPotions;
                bool saveOk=AscendanceSaveSystem.SaveCurrent(gm,out _);var loaded=AscendanceSaveSystem.Load();
                Check(saveOk&&loaded!=null,"Crafting and profession state saves through the real campaign save system.");
                if(loaded!=null)
                {
                    g.AlchemyXp0837=0;g.BlacksmithingXp0837=0;g.HealingPotions=0;g.LootInventory.Clear();g.AcquiredEquipment.Clear();a.LootInventory.Clear();
                    gm.ApplySaveData(loaded);yield return null;
                    var rg=gm.Heroes["Garruk"];var ra=gm.Heroes["Aldric"];
                    Check(rg.AlchemyXp0837==savedAlchemyXp&&rg.BlacksmithingXp0837==savedBlacksmithXp,"Save/load restores per-Hero profession XP.");
                    Check(rg.HealingPotions==savedPotions&&rg.AcquiredEquipment.Contains("EQ037")&&rg.AcquiredEquipment.Contains("EQ139"),"Save/load restores crafted consumables and Equipment outputs.");
                    Check(ra.LootInventory.Contains("LT031")&&!rg.LootInventory.Contains("LT031"),"Save/load preserves material ownership per Hero without pooling inventories.");
                }

                gm.SelectHero("Garruk");DowntimeUI0836.Instance?.OpenCrafting();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen&&DowntimeUI0836.Instance.DebugTab0836=="CRAFTING","Crafting remains integrated into the Stonebrook Downtime Hub.");
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.DebugProfessionText0836.Contains("GARRUK")&&DowntimeUI0836.Instance.DebugProfessionText0836.Contains("LEVEL"),"Crafting UI identifies the selected Hero and displays that Hero's profession progression.");
            }
            finally
            {
                try
                {
                    if(hadSave){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,previousSave);}
                    else if(File.Exists(path))File.Delete(path);
                }
                catch(System.Exception ex){Debug.LogException(ex);}
            }

            Check(File.Exists(path)==hadSave,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.37 TEST] PASS — Hero-Owned Crafting & Profession Progression completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.37 TEST] FAIL — Hero-Owned Crafting & Profession Progression completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
