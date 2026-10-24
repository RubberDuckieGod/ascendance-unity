#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class HeroTradingTestTools0838
    {
        [MenuItem("Ascendance/Validation/Test 0.8.38 - Hero Trading & Material Transfer (Play Mode)")]
        public static void TestTrading()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.38 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.38 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.38 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.38 TEST] FAIL — "+label);}}

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
                g.LootInventory.Clear();a.LootInventory.Clear();g.AcquiredEquipment.Clear();a.AcquiredEquipment.Clear();
                g.HealingPotions=2;a.HealingPotions=1;
                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing,0);
                gm.DebugSetProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing,0);
                int goldBefore=gm.PartyGold;

                Check(gm.IsDowntimeAvailable0836&&gm.CanTradeBetweenHeroes0838(g.Id,a.Id),"Hero-to-Hero trading is available during verified Stonebrook Downtime.");
                Check(!gm.CanTradeBetweenHeroes0838(g.Id,g.Id),"A Hero cannot trade with themself.");

                g.LootInventory.AddRange(new[]{"LT031","LT031","LT031","LT119"});
                var stacks=gm.TradeableHeroLoot0838(g.Id);
                Check(stacks.Any(x=>x.Id=="LT031"&&x.Count==3&&x.CraftingMaterial),"Trade list exposes the source Hero's personal crafting-material stack.");
                Check(!stacks.Any(x=>x.Id=="LT119")&&!gm.CanTransferHeroLoot0838(g.Id,a.Id,"LT119"),"Quest Items are protected and cannot be traded.");

                bool one=gm.TransferHeroLoot0838(g.Id,a.Id,"LT031",1);
                Check(one&&gm.HeroMaterialCount0837(g.Id,"LT031")==2&&gm.HeroMaterialCount0837(a.Id,"LT031")==1,"A single material moves from the source Hero to the receiving Hero.");
                Check(gm.PartyGold==goldBefore,"Material transfer does not change shared Party Gold.");
                bool all=gm.TransferAllHeroLoot0838(g.Id,a.Id,"LT031");
                Check(all&&gm.HeroMaterialCount0837(g.Id,"LT031")==0&&gm.HeroMaterialCount0837(a.Id,"LT031")==3,"GIVE ALL transfers the remainder of one personal material stack.");
                Check(g.LootInventory.Contains("LT119"),"Protected Quest Item remains with its original Hero after other transfers.");

                int gx=gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing),ax=gm.ProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing);
                Check(gx==0&&ax==0,"Trading materials does not award or transfer profession XP.");

                g.LootInventory.AddRange(new[]{"LT038","LT038","LT049"});
                gm.TransferAllHeroLoot0838(g.Id,a.Id,"LT038");gm.TransferAllHeroLoot0838(g.Id,a.Id,"LT049");
                Check(!gm.CanCraftRecipe0837("CR-BS-02",g.Id)&&gm.CanCraftRecipe0837("CR-BS-02",a.Id),"Transferred ingredients immediately become craftable only for the receiving Hero.");
                bool crafted=gm.CraftRecipe0837("CR-BS-02",a.Id);
                Check(crafted&&a.AcquiredEquipment.Contains("EQ037")&&gm.ProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing)==40,"Receiving Hero can craft with transferred materials and earns their own profession XP.");
                Check(gm.ProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing)==0,"Source Hero's profession progression remains unchanged when another Hero crafts transferred materials.");

                g.AcquiredEquipment.Add("EQ053");
                bool gear=gm.TransferEquipment0838(g.Id,a.Id,"EQ053");
                Check(gear&&!g.AcquiredEquipment.Contains("EQ053")&&a.AcquiredEquipment.Contains("EQ053"),"Unequipped Equipment transfers between Heroes without duplicating the card.");
                Check(gm.PartyGold==goldBefore,"Equipment transfer does not change shared Party Gold.");

                string equippedId=g.StartingEquipment.FirstOrDefault()?.Id;
                Check(string.IsNullOrEmpty(equippedId)||!gm.CanTransferEquipment0838(g.Id,a.Id,equippedId),"Equipped gear is protected from trading until it is returned to unequipped inventory.");

                bool potion=gm.TransferHealingPotion0838(g.Id,a.Id);
                Check(potion&&g.HealingPotions==1&&a.HealingPotions==2,"Healing Potions can be deliberately handed from one Hero to another.");
                Check(gm.PartyGold==goldBefore,"Healing Potion transfer leaves shared Party Gold unchanged.");

                bool reverse=gm.TransferHeroLoot0838(a.Id,g.Id,"LT031",1);
                Check(reverse&&gm.HeroMaterialCount0837(g.Id,"LT031")==1&&gm.HeroMaterialCount0837(a.Id,"LT031")==2,"Transfers can move personal materials in either direction during Downtime.");

                int savedGIron=gm.HeroMaterialCount0837(g.Id,"LT031"),savedAIron=gm.HeroMaterialCount0837(a.Id,"LT031");
                int savedGPotions=g.HealingPotions,savedAPotions=a.HealingPotions;
                bool saveOk=AscendanceSaveSystem.SaveCurrent(gm,out _);var loaded=AscendanceSaveSystem.Load();
                Check(saveOk&&loaded!=null,"Post-trade ownership saves through the real campaign save system.");
                if(loaded!=null)
                {
                    g.LootInventory.Clear();a.LootInventory.Clear();g.AcquiredEquipment.Clear();a.AcquiredEquipment.Clear();g.HealingPotions=0;a.HealingPotions=0;
                    gm.ApplySaveData(loaded);yield return null;
                    var rg=gm.Heroes["Garruk"];var ra=gm.Heroes["Aldric"];
                    Check(gm.HeroMaterialCount0837(rg.Id,"LT031")==savedGIron&&gm.HeroMaterialCount0837(ra.Id,"LT031")==savedAIron,"Save/load preserves transferred material ownership per Hero.");
                    Check(ra.AcquiredEquipment.Contains("EQ053")&&!rg.AcquiredEquipment.Contains("EQ053"),"Save/load preserves transferred unequipped Equipment ownership.");
                    Check(rg.HealingPotions==savedGPotions&&ra.HealingPotions==savedAPotions,"Save/load preserves Healing Potion transfers.");
                    Check(rg.LootInventory.Contains("LT119")&&!ra.LootInventory.Contains("LT119"),"Save/load keeps protected Quest Items with their original Hero.");
                }

                gm.SelectHero("Garruk");DowntimeUI0836.Instance?.OpenTrade();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen&&DowntimeUI0836.Instance.DebugTab0836=="TRADE","Trade is integrated as a dedicated Stonebrook Downtime tab.");
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.DebugTradeRecipient0838=="Aldric","Trade UI selects another participating Hero as the receiving Hero.");
                Check(gm.TradeSummary0838("Garruk","Aldric").Contains("Gold is party-owned"),"Trade UI explicitly communicates that Gold is party-owned and not transferable.");

                bool began=gm.BeginRecommendedCampaignQuest0833();yield return null;
                Check(began&&gm.CurrentQuestId0829=="Q002","Next campaign Quest still starts normally after trading.");
                Check(!gm.CanTradeBetweenHeroes0838("Garruk","Aldric"),"Hero trading is disabled immediately once a Quest becomes active.");
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
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.38 TEST] PASS — Hero Trading & Material Transfer completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.38 TEST] FAIL — Hero Trading & Material Transfer completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
