#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class DowntimeLoadoutTestTools0840
    {
        [MenuItem("Ascendance/Validation/Test 0.8.40 - Downtime Loadout Preparation (Play Mode)")]
        public static void TestLoadout()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.40 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.40 TEST] No AscendanceGameManager found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.40 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.40 TEST] FAIL — "+label);}}

            string path=AscendanceSaveSystem.SavePath;
            bool hadSave=File.Exists(path);byte[] previousSave=hadSave?File.ReadAllBytes(path):null;
            try
            {
                gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);
                gm.DebugResetCampaignProgress0833();
                Check(!gm.CanManageDowntimeLoadout0840("Garruk"),"Loadout preparation is unavailable while a Quest is active.");
                gm.DebugResolveCampaignQuest0833(true);yield return null;
                var g=gm.Heroes["Garruk"];
                g.AcquiredEquipment.Clear();
                g.LastDowntimeActivityKey0839=null;g.LastDowntimeActivityLabel0839=null;

                Check(gm.IsDowntimeAvailable0836&&gm.CanManageDowntimeLoadout0840(g.Id),"Resolved Q001 opens free Stonebrook loadout preparation.");
                string activityKeyBefore=gm.CurrentDowntimePeriodKey0839;
                int goldBefore=gm.PartyGold;
                Check(gm.WorkDowntime0839(g.Id)&&gm.PartyGold==goldBefore+25,"Garruk can spend his one Activity on Work before preparing gear.");
                Check(gm.CanManageDowntimeLoadout0840(g.Id)&&gm.HasUsedDowntimeActivity0839(g.Id),"Using Work/Gather does not disable loadout preparation.");

                // Seed deterministic unequipped gear without touching the live Equipment Deck.
                g.AcquiredEquipment.AddRange(new[]{"EQ078","EQ037","EQ083","EQ102","EQ103"});
                var equippedStart=gm.DowntimeEquippedGear0840(g.Id);
                var inventoryStart=gm.DowntimeUnequippedGear0840(g.Id);
                Check(equippedStart.Any(x=>x.Id=="EQ130")&&equippedStart.Any(x=>x.Id=="EQ137")&&equippedStart.Any(x=>x.Id=="EQ140"),"Loadout screen reads Garruk's persistent equipped weapon, armor, and accessory.");
                Check(inventoryStart.Count>=5&&inventoryStart.Any(x=>x.Id=="EQ078")&&inventoryStart.Any(x=>x.Id=="EQ037"),"Loadout screen reads the Hero's personal unequipped Equipment inventory.");
                Check(gm.DowntimeEquipmentComparison0840(g.Id,"EQ037").Contains("Raider's Greataxe"),"Unequipped gear exposes comparison text against the matching equipped slot.");

                Check(!gm.CanEquipDowntimeEquipment0840(g.Id,"EQ078"),"A Shield cannot be equipped while Garruk's two-handed Greataxe occupies both hands.");
                int attackWithGreataxe=g.Attack;
                Check(gm.UnequipDowntimeEquipment0840(g.Id,"EQ130"),"UNEQUIP returns the two-handed Weapon to Garruk's personal inventory.");
                Check(!gm.DebugIsEquipmentEquipped0840(g.Id,"EQ130")&&g.AcquiredEquipment.Contains("EQ130")&&g.Attack<attackWithGreataxe,"Unequipping immediately recalculates Attack and preserves ownership.");
                Check(gm.CanTransferEquipment0838(g.Id,"Aldric","EQ130"),"Once unequipped, former equipped gear immediately becomes eligible for Hero-to-Hero Trading.");

                int armorBeforeShield=g.Armor;
                Check(gm.EquipDowntimeEquipment0840(g.Id,"EQ078"),"Garruk can equip the Shield after freeing his Weapon hands.");
                Check(gm.DebugIsEquipmentEquipped0840(g.Id,"EQ078")&&!g.AcquiredEquipment.Contains("EQ078")&&g.Armor==armorBeforeShield+2,"Equipping removes the card from inventory and recalculates Armor.");

                Check(gm.EquipDowntimeEquipment0840(g.Id,"EQ037"),"A one-handed Weapon can be equipped alongside the Shield.");
                Check(gm.DebugIsEquipmentEquipped0840(g.Id,"EQ037")&&gm.DebugIsEquipmentEquipped0840(g.Id,"EQ078"),"One-handed Weapon and Shield remain equipped together.");
                Check(g.Attack==g.BaseAttack+1,"Weapon replacement recalculates Attack from the equipped Iron Dagger.");

                Check(gm.EquipDowntimeEquipment0840(g.Id,"EQ083"),"Armor can be replaced from the unequipped inventory.");
                Check(gm.DebugIsEquipmentEquipped0840(g.Id,"EQ083")&&!gm.DebugIsEquipmentEquipped0840(g.Id,"EQ137")&&g.AcquiredEquipment.Contains("EQ137"),"Replaced Armor returns to personal unequipped inventory without duplication.");

                int accessoryBefore=gm.DowntimeEquippedGear0840(g.Id).Count(x=>x.Slot=="Accessory");
                Check(accessoryBefore==1&&gm.EquipDowntimeEquipment0840(g.Id,"EQ102"),"An Accessory equips into Garruk's open second Accessory slot.");
                Check(gm.DowntimeEquippedGear0840(g.Id).Count(x=>x.Slot=="Accessory")==2,"Garruk respects the two-Accessory capacity.");
                Check(!gm.CanEquipDowntimeEquipment0840(g.Id,"EQ103"),"A third Accessory requires an explicit replacement choice when capacity is full.");
                Check(gm.CanEquipDowntimeEquipment0840(g.Id,"EQ103",0)&&gm.EquipDowntimeEquipment0840(g.Id,"EQ103",0),"Accessory replacement can explicitly target one equipped Accessory.");
                Check(gm.DebugIsEquipmentEquipped0840(g.Id,"EQ103")&&g.AcquiredEquipment.Contains("EQ140"),"The replaced Accessory returns to Garruk's personal inventory.");

                Check(gm.UnequipDowntimeEquipment0840(g.Id,"EQ078"),"Shield can be deliberately unequipped during loadout preparation.");
                Check(!gm.DebugIsEquipmentEquipped0840(g.Id,"EQ078")&&g.AcquiredEquipment.Contains("EQ078"),"Unequipped Shield returns to inventory and is no longer part of the active loadout.");
                Check(gm.CanTransferEquipment0838(g.Id,"Aldric","EQ078"),"Unequipped Shield becomes tradeable immediately.");
                Check(gm.HasUsedDowntimeActivity0839(g.Id)&&gm.CurrentDowntimePeriodKey0839==activityKeyBefore,"Repeated equip/unequip operations never refund or consume a Downtime Activity.");

                var equippedIds=gm.DowntimeEquippedGear0840(g.Id).Select(x=>x.Id).OrderBy(x=>x).ToArray();
                var inventoryIds=g.AcquiredEquipment.OrderBy(x=>x).ToArray();
                int savedAttack=g.Attack,savedArmor=g.Armor,savedGold=gm.PartyGold;
                bool saveOk=AscendanceSaveSystem.SaveCurrent(gm,out _);var loaded=AscendanceSaveSystem.Load();
                Check(saveOk&&loaded!=null,"Prepared loadout saves through the real campaign save system.");
                if(loaded!=null)
                {
                    g.StartingEquipment.Clear();g.AcquiredEquipment.Clear();g.Attack=0;g.Armor=0;
                    gm.ApplySaveData(loaded);yield return null;g=gm.Heroes["Garruk"];
                    Check(gm.DowntimeEquippedGear0840(g.Id).Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(equippedIds),"Save/load restores the exact equipped loadout.");
                    Check(g.AcquiredEquipment.OrderBy(x=>x).SequenceEqual(inventoryIds),"Save/load restores the exact unequipped Equipment inventory.");
                    Check(g.Attack==savedAttack&&g.Armor==savedArmor,"Save/load restores equipment-derived Attack and Armor.");
                    Check(gm.PartyGold==savedGold&&gm.HasUsedDowntimeActivity0839(g.Id),"Loadout save/load does not alter shared Gold or the Hero's Activity state.");
                }

                gm.SelectHero("Garruk");DowntimeUI0836.Instance?.OpenLoadout();yield return null;
                Check(DowntimeUI0836.Instance!=null&&DowntimeUI0836.Instance.IsOpen&&DowntimeUI0836.Instance.DebugTab0836=="LOADOUT","Downtime UI exposes a dedicated LOADOUT tab.");
                string loadoutText=DowntimeUI0836.Instance?.DebugLoadoutText0840??string.Empty;
                Check(loadoutText.Contains("Garruk Stoneblood")&&loadoutText.Contains("ATTACK")&&loadoutText.Contains("ARMOR"),"LOADOUT UI shows the selected Hero's live combat stats and slot summary.");

                bool began=gm.BeginRecommendedCampaignQuest0833();yield return null;
                Check(began&&gm.CurrentQuestId0829=="Q002"&&!gm.IsDowntimeAvailable0836,"Next campaign Quest launches normally after loadout preparation.");
                Check(!gm.CanManageDowntimeLoadout0840("Garruk"),"Loadout preparation closes once a Quest becomes active.");
                Check(gm.DebugIsEquipmentEquipped0840("Garruk","EQ037"),"The prepared equipped loadout carries into the next Quest.");
            }
            finally
            {
                try{if(hadSave){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,previousSave);}else if(File.Exists(path))File.Delete(path);}catch(System.Exception ex){Debug.LogException(ex);}
            }
            Check(File.Exists(path)==hadSave,"Validator restores the pre-test campaign save file.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.40 TEST] PASS — Downtime Equipment Management & Loadout Preparation completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.40 TEST] FAIL — Downtime Equipment Management & Loadout Preparation completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
