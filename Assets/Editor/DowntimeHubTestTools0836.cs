#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class DowntimeHubTestTools0836
    {
        [MenuItem("Ascendance/Validation/Test 0.8.36b - Resolved Downtime Load Routing (Play Mode)")]
        public static void TestDowntimeHub()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.36b TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.36b TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.36b TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.36b TEST] FAIL — "+label);}}

            string path=AscendanceSaveSystem.SavePath;
            bool hadSave=File.Exists(path);
            byte[] previousSave=hadSave?File.ReadAllBytes(path):null;

            try
            {
                gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);
                gm.DebugResetCampaignProgress0833();
                gm.DebugResolveCampaignQuest0833(true);
                yield return null;

                Check(gm.IsDowntimeAvailable0836&&gm.CanUseDowntimeActions0836,"Resolved Q001 opens the between-Quest downtime state.");
                Check(Object.FindAnyObjectByType<DowntimeUI0836>()!=null,"Stonebrook Downtime UI is present without scene wiring.");

                var results=QuestResultsUI0835.Instance;
                results?.ReturnToCampaign();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen,"Return to Stonebrook routes Quest Results into the Downtime Hub.");
                Check(DowntimeUI0836.Instance.DebugResourceText0836.Contains("GOLD")&&DowntimeUI0836.Instance.DebugResourceText0836.Contains("MORALE")&&DowntimeUI0836.Instance.DebugResourceText0836.Contains("SUPPLY"),"Downtime Hub exposes live Gold, Reputation, Morale, and Supply resources.");

                var g=gm.Heroes["Garruk"];
                int xpBefore=g.XP;
                g.Health=1;g.Downed=true;g.Dead=false;g.HealingPotions=0;
                bool recovered=gm.RecoverHero0836(g.Id);
                Check(recovered&&g.Health==g.MaxHealth&&!g.Downed&&!g.Dead&&g.HealingPotions>=1,"Downtime recovery restores Hero readiness and Healing Potion minimum.");
                Check(g.XP==xpBefore,"Downtime recovery grants no XP.");

                g.LootInventory.Add("LT031");
                g.LootInventory.Add("LT032");
                Check(gm.DowntimeProfessionMaterialCount0836(DowntimeProfession0836.Blacksmithing)>=2,"Blacksmithing hook reads the selected Hero's persistent crafting materials.");
                Check(gm.DowntimeProfessionMaterialCount0836(DowntimeProfession0836.Artificing)>=1,"Artificing hook recognizes authored Tinkering/engineering materials carried by the selected Hero.");

                gm.DebugSetPartyGold(10000);
                string stock=gm.DowntimeMerchantStock0836.FirstOrDefault();
                Check(!string.IsNullOrEmpty(stock),"Stonebrook Merchant exposes stock from the live Equipment Deck.");
                int price=string.IsNullOrEmpty(stock)?0:gm.MerchantPrice0836(stock);
                int goldBefore=gm.PartyGold;
                bool bought=!string.IsNullOrEmpty(stock)&&gm.PurchaseMerchantEquipment0836(stock,g.Id);
                Check(bought&&gm.PartyGold==goldBefore-price&&g.AcquiredEquipment.Contains(stock)&&!gm.DowntimeMerchantStock0836.Contains(stock),"Merchant purchase spends live Gold, removes stock from the Equipment Deck, and grants unequipped gear.");

                DowntimeUI0836.Instance?.OpenMerchant();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen&&DowntimeUI0836.Instance.DebugTab0836=="MERCHANT","Merchant entry point opens inside the Downtime Hub.");
                DowntimeUI0836.Instance?.OpenCrafting();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.DebugTab0836=="CRAFTING"&&DowntimeUI0836.Instance.DebugProfessionText0836.Contains("BLACKSMITHING"),"Crafting entry point exposes the four profession/material hooks without inventing recipes.");

                bool saveOk=AscendanceSaveSystem.SaveCurrent(gm,out _);
                var loadedSave=AscendanceSaveSystem.Load();
                Check(saveOk&&loadedSave!=null,"Downtime state saves through the real campaign save system.");
                if(loadedSave!=null)
                {
                    g.Health=1;
                    g.AcquiredEquipment.Remove(stock);
                    gm.DebugSetPartyGold(0);
                    bool primedStaleGates=gm.DebugPrimeStaleDowntimeRoutingGates0836();
                    Check(primedStaleGates,"Validator primes stale reaction, deck-control, reward-choice, and resolving gates before loading the downtime save.");
                    gm.ApplySaveData(loadedSave);yield return null;
                    var restored=gm.Heroes["Garruk"];
                    Check(!gm.HasPendingReactionWindow&&!gm.HasPendingEncounterDeckControl&&!gm.HasPendingRewardChoice&&!gm.IsResolving,"Loading a resolved-Quest downtime save clears all stale non-durable routing gates.");
                    Check(restored.Health==restored.MaxHealth&&restored.AcquiredEquipment.Contains(stock)&&gm.PartyGold==goldBefore-price,"Save/load restores downtime recovery, Merchant purchase, and live campaign Gold.");
                    Check(restored.LootInventory.Contains("LT031")&&restored.LootInventory.Contains("LT032"),"Save/load preserves crafting materials used by profession hooks.");
                }

                DowntimeUI0836.Instance?.Open();
                Debug.Log("[ASCENDANCE][0.8.36b TEST] ROUTING STATE — "+gm.DebugCampaignQuestRoutingState0836("Q002"));
                Check(gm.QuestComplete&&!gm.QuestFailed,"Reloaded downtime save preserves Q001 as successfully resolved.");
                Check(gm.CurrentWorldEventId0833=="WE001","Reloaded downtime save preserves WE001 as the active World Event.");
                Check(gm.DebugCampaignCompletedQuest0833("Q001"),"Reloaded campaign progression preserves Q001 in CompletedQuestIds.");
                Check(gm.RecommendedMainCampaignQuest0833=="Q002","Reloaded Q001 downtime state recommends Q002 as the next main Quest.");
                Check(gm.AvailableCampaignQuests0833.Contains("Q002"),"Reloaded Q001 downtime state lists Q002 as available.");
                Check(!gm.IsResolving&&!gm.HasPendingReactionWindow&&!gm.HasPendingEncounterDeckControl&&!gm.HasPendingRewardChoice,"No transient modal/combat gate remains after loading resolved downtime.");
                Check(gm.CanBeginCampaignQuest0833("Q002"),"Q002 passes the authoritative campaign CanBegin gate after downtime load.");
                bool started=gm.BeginRecommendedCampaignQuest0833();yield return null;
                Check(started&&gm.CurrentQuestId0829=="Q002"&&!gm.IsDowntimeAvailable0836,"Choosing the next campaign Quest exits downtime and starts through the authoritative campaign router.");
                Check(DowntimeUI0836.Instance==null||!DowntimeUI0836.Instance.IsOpen,"Downtime overlay closes once the next Quest is active.");
                gm.DebugResolveCampaignQuest0833(false);yield return null;
                Check(gm.QuestFailed&&gm.IsDowntimeAvailable0836,"A failed non-final Quest reopens Stonebrook downtime before a retry or different available Quest.");
            }
            finally
            {
                try
                {
                    if(hadSave)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllBytes(path,previousSave);
                    }
                    else if(File.Exists(path))File.Delete(path);
                }
                catch(System.Exception ex){Debug.LogException(ex);}
            }

            Check(File.Exists(path)==hadSave,"Validator restores the pre-test save file after downtime persistence validation.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.36b TEST] PASS — Stonebrook Downtime Hub completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.36b TEST] FAIL — Stonebrook Downtime Hub completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
