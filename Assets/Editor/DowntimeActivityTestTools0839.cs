#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class DowntimeActivityTestTools0839
    {
        [MenuItem("Ascendance/Validation/Test 0.8.39 - Per-Hero Downtime Activities (Play Mode)")]
        public static void TestActivities()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.39 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.39 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.39 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.39 TEST] FAIL — "+label);}}

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
                g.LastDowntimeActivityKey0839=null;g.LastDowntimeActivityLabel0839=null;
                a.LastDowntimeActivityKey0839=null;a.LastDowntimeActivityLabel0839=null;
                g.LootInventory.Clear();a.LootInventory.Clear();
                gm.DebugSetProfessionXp0837(g.Id,DowntimeProfession0836.Blacksmithing,0);
                gm.DebugSetProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing,0);

                Check(gm.IsDowntimeAvailable0836&&!string.IsNullOrWhiteSpace(gm.CurrentDowntimePeriodKey0839),"Resolved Q001 creates a keyed Stonebrook Downtime Activity period.");
                Check(gm.CanTakeDowntimeActivity0839(g.Id)&&gm.CanTakeDowntimeActivity0839(a.Id),"Each participating Hero begins the downtime with one available Activity.");
                Check(gm.CanTradeBetweenHeroes0838(g.Id,a.Id),"Trading remains independently available before any Activity is used.");

                int goldBefore=gm.PartyGold;
                bool worked=gm.WorkDowntime0839(g.Id);
                Check(worked&&gm.PartyGold==goldBefore+25,"WORK consumes Garruk's Activity and adds exactly 25 shared Party Gold.");
                Check(gm.HasUsedDowntimeActivity0839(g.Id)&&!gm.CanTakeDowntimeActivity0839(g.Id),"A Hero cannot take a second Activity in the same downtime period.");
                Check(gm.CanTakeDowntimeActivity0839(a.Id),"One Hero using an Activity does not consume another Hero's Activity.");
                Check(!gm.WorkDowntime0839(g.Id)&&!gm.CanGatherResources0839(g.Id,DowntimeProfession0836.Blacksmithing),"A Hero who already worked cannot also Gather in that same downtime.");
                Check(gm.CanTradeBetweenHeroes0838(g.Id,a.Id),"Trading remains unlimited after a Hero spends their Activity.");

                // Seed a deterministic live Loot Deck containing three different Blacksmithing materials.
                gm.LootDeck.Clear();
                gm.LootDeck.AddRange(new[]{"LT031","LT038","LT039"});
                Check(gm.GatherableResources0839(DowntimeProfession0836.Blacksmithing).Count==3,"Gather reads profession-appropriate Crafting Materials from the live Loot Deck.");
                Check(gm.GatherableResources0839(DowntimeProfession0836.Alchemy).Count==0,"Gather does not treat unrelated profession materials as valid finds.");

                int aBlacksmithXp=gm.ProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing);
                int gatherGold=gm.PartyGold;
                gm.DebugSetNextGatherCount0839(3);
                bool gathered=gm.GatherResources0839(a.Id,DowntimeProfession0836.Blacksmithing);
                var gatheredIds=new[]{"LT031","LT038","LT039"}.Where(id=>a.LootInventory.Contains(id)).ToList();
                Check(gathered,"GATHER succeeds for a Hero whose Activity is still available.");
                Check(gatheredIds.Count==3,"A Gather Activity can find up to 3 total materials.");
                Check(gatheredIds.Distinct().Count()==3,"One Gather Activity may return different profession-appropriate materials.");
                Check(a.LootInventory.Count==3&&g.LootInventory.Count==0,"Gathered materials enter only the gathering Hero's personal inventory.");
                Check(gm.LootDeck.Count==0,"Gathered resource cards are removed from the live finite Loot Deck.");
                Check(gm.PartyGold==gatherGold,"Gather never changes shared Party Gold.");
                Check(gm.ProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing)==aBlacksmithXp,"Gather grants no profession XP.");
                Check(gm.HasUsedDowntimeActivity0839(a.Id)&&!gm.CanTakeDowntimeActivity0839(a.Id),"Gather consumes exactly that Hero's one Activity for the period.");
                Check(!gm.GatherResources0839(a.Id,DowntimeProfession0836.Blacksmithing),"A second Gather is blocked in the same downtime period.");

                // Prove crafting remains independent after the Activity has been spent.
                a.LootInventory.AddRange(new[]{"LT038","LT038","LT049"});
                Check(gm.CanCraftRecipe0837("CR-BS-02",a.Id),"Crafting remains available after the Hero has used their Downtime Activity if personal resources are sufficient.");
                bool crafted=gm.CraftRecipe0837("CR-BS-02",a.Id);
                Check(crafted&&gm.ProfessionXp0837(a.Id,DowntimeProfession0836.Blacksmithing)>aBlacksmithXp,"Crafting still functions normally and awards profession XP after the Activity is spent.");

                string gKey=g.LastDowntimeActivityKey0839,aKey=a.LastDowntimeActivityKey0839;
                string gLabel=g.LastDowntimeActivityLabel0839,aLabel=a.LastDowntimeActivityLabel0839;
                int savedGold=gm.PartyGold;
                int savedAIron=gm.HeroMaterialCount0837(a.Id,"LT031");
                bool saveOk=AscendanceSaveSystem.SaveCurrent(gm,out _);var loaded=AscendanceSaveSystem.Load();
                Check(saveOk&&loaded!=null,"Downtime Activity state saves through the real campaign save system.");
                if(loaded!=null)
                {
                    g.LastDowntimeActivityKey0839=null;a.LastDowntimeActivityKey0839=null;g.LootInventory.Clear();a.LootInventory.Clear();
                    gm.ApplySaveData(loaded);yield return null;
                    g=gm.Heroes["Garruk"];a=gm.Heroes["Aldric"];
                    Check(g.LastDowntimeActivityKey0839==gKey&&a.LastDowntimeActivityKey0839==aKey,"Save/load preserves which Heroes already used their Activity in the current downtime.");
                    Check(g.LastDowntimeActivityLabel0839==gLabel&&a.LastDowntimeActivityLabel0839==aLabel,"Save/load preserves each Hero's Work/Gather Activity summary.");
                    Check(gm.PartyGold==savedGold&&gm.HeroMaterialCount0837(a.Id,"LT031")==savedAIron,"Save/load preserves Work Gold and gathered personal-material ownership.");
                    Check(!gm.CanTakeDowntimeActivity0839(g.Id)&&!gm.CanTakeDowntimeActivity0839(a.Id),"Reloading the same downtime does not grant an extra Activity.");
                }

                gm.SelectHero("Garruk");DowntimeUI0836.Instance?.OpenActivity();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen&&DowntimeUI0836.Instance.DebugTab0836=="ACTIVITY","Downtime UI exposes a dedicated ACTIVITY tab.");
                Check((DowntimeUI0836.Instance?.DebugActivityText0839??string.Empty).Contains("ACTIVITY USED"),"Activity UI shows the selected Hero's used/available state.");

                bool began=gm.BeginRecommendedCampaignQuest0833();yield return null;
                Check(began&&gm.CurrentQuestId0829=="Q002"&&!gm.IsDowntimeAvailable0836,"Next campaign Quest still launches normally after Activities, Crafting, and Trading.");
                Check(!gm.CanTakeDowntimeActivity0839("Garruk")&&!gm.CanTakeDowntimeActivity0839("Aldric"),"Downtime Activities are unavailable while a Quest is active.");

                gm.DebugResolveCampaignQuest0833(false);yield return null;
                Check(gm.IsDowntimeAvailable0836&&gm.CanTakeDowntimeActivity0839("Garruk")&&gm.CanTakeDowntimeActivity0839("Aldric"),"A newly resolved Quest creates a fresh one-Activity allowance for every Hero.");
                Check(!string.Equals(gm.CurrentDowntimePeriodKey0839,gKey,System.StringComparison.OrdinalIgnoreCase),"The next downtime period has a new Activity key, including after a failed Quest/retry opportunity.");
                int newGold=gm.PartyGold;
                Check(gm.WorkDowntime0839("Garruk")&&gm.PartyGold==newGold+25,"The same Hero can Work again in a later downtime period.");
                Check(gm.CanTradeBetweenHeroes0838("Garruk","Aldric"),"Trading remains unlimited in the new downtime after a Hero uses the new Activity.");
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
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.39 TEST] PASS — Per-Hero Downtime Activities completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.39 TEST] FAIL — Per-Hero Downtime Activities completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
