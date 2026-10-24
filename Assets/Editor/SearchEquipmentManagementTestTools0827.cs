#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class SearchEquipmentManagementTestTools0827
    {
        [MenuItem("Ascendance/Validation/Test 0.8.27 - Search Rewards & Equipment Management (Play Mode)")]
        public static void TestSearchEquipmentManagement()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.27 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.27 TEST] Game manager not found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.27 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.27 TEST] FAIL — {message}");}}
            var hero=gm.ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated);if(hero==null){Debug.LogError("[ASCENDANCE][0.8.27 TEST] FAIL — Need a living Hero.");yield break;}
            var forest=gm.Locations["L002"];var stonebrook=gm.Locations["L000"];var ruins=gm.Locations["L003"];forest.Revealed=stonebrook.Revealed=ruins.Revealed=true;

            void PutLootTop(string id){int i=gm.LootDeck.FindIndex(x=>x==id);if(i>=0)gm.LootDeck.RemoveAt(i);else{i=gm.LootDiscard.FindIndex(x=>x==id);if(i>=0)gm.LootDiscard.RemoveAt(i);else{int inv=hero.LootInventory.FindIndex(x=>x==id);if(inv>=0)hero.LootInventory.RemoveAt(inv);}}gm.LootDeck.Insert(0,id);}
            void PutEquipmentTop(string id){int i=gm.EquipmentDeck.FindIndex(x=>x==id);if(i>=0)gm.EquipmentDeck.RemoveAt(i);else{i=gm.EquipmentDiscard.FindIndex(x=>x==id);if(i>=0)gm.EquipmentDiscard.RemoveAt(i);else{foreach(var h in gm.Heroes.Values){int inv=h.AcquiredEquipment.FindIndex(x=>x==id);if(inv>=0){h.AcquiredEquipment.RemoveAt(inv);break;}}}}gm.EquipmentDeck.Insert(0,id);}

            int gold=gm.PartyGold;gm.DebugResolveSearchCard0827("SD004",stonebrook.Id,hero.Id);
            Check(gm.HasPendingRewardChoice&&gm.DebugPendingRewardChoiceMode()=="SearchGold20OrLoot1","SD004 opens its real 20 Gold or 1 Loot choice.");
            gm.DebugChooseRewardChoice("GOLD");Check(gm.PartyGold==gold+20,"SD004 Gold choice grants exactly 20 Gold.");

            PutLootTop("LT001");int loot=hero.LootInventory.Count;gm.DebugResolveSearchCard0827("SD001",forest.Id,hero.Id);
            Check(hero.LootInventory.Count==loot+1&&hero.LootInventory.Last()=="LT001","SD001 draws the actual top Loot card into Hero inventory.");

            PutLootTop("LT002");gold=gm.PartyGold;loot=hero.LootInventory.Count;gm.DebugResolveSearchCard0827("SD003",forest.Id,hero.Id);
            Check(hero.LootInventory.Count==loot+1&&gm.DebugPendingRewardChoiceMode()=="SearchDiscardHandOrLose5","SD003 draws Loot then opens discard-a-Hero-card-or-lose-5-Gold choice.");
            gm.DebugChooseRewardChoice("LOSE5");Check(gm.PartyGold==Mathf.Max(0,gold-5),"SD003 Lose Gold option applies exactly 5 Gold loss.");

            PutEquipmentTop("EQ001");int eq=hero.AcquiredEquipment.Count;int attackBefore=hero.Attack;gm.DebugResolveSearchCard0827("SD009",stonebrook.Id,hero.Id);
            Check(gm.DebugPendingRewardChoiceMode()=="SearchEquipmentKeepGate"&&gm.PendingRewardChoice.RevealedCardIds.SequenceEqual(new[]{"EQ001"}),"SD009 holds the exact drawn Equipment card while the choice is unresolved.");
            gm.DebugChooseRewardChoice("KEEP");Check(hero.AcquiredEquipment.Count==eq+1&&hero.AcquiredEquipment.Contains("EQ001")&&hero.Attack==attackBefore,"SD009 keeps Common Equipment unequipped without changing combat stats.");

            PutEquipmentTop("EQ008");gold=gm.PartyGold;gm.DebugResolveSearchCard0827("SD009",stonebrook.Id,hero.Id);
            Check(gm.RewardChoiceOptions.Count==1&&gm.RewardChoiceOptions[0].Key=="BOTTOM25","SD009 high-rarity Equipment cannot be kept and exposes only bottom + 25 Gold.");
            gm.DebugChooseRewardChoice("BOTTOM25");Check(gm.EquipmentDeck.Last()=="EQ008"&&gm.PartyGold==gold+25,"SD009 bottoms ineligible Equipment and grants exactly 25 Gold.");

            PutLootTop("LT003");PutLootTop("LT004");loot=hero.LootInventory.Count;int discard=gm.LootDiscard.Count;gm.DebugResolveSearchCard0827("SD012",ruins.Id,hero.Id);
            Check(gm.DebugPendingRewardChoiceMode()=="SearchKeepOneLoot2"&&gm.PendingRewardChoice.RevealedCardIds.Count==2,"SD012 removes the actual top two Loot cards and asks which one to keep.");
            string kept=gm.PendingRewardChoice.RevealedCardIds[1];string dropped=gm.PendingRewardChoice.RevealedCardIds[0];gm.DebugChooseRewardChoice("KEEP1");
            Check(hero.LootInventory.Count==loot+1&&hero.LootInventory.Last()==kept&&gm.LootDiscard.Count==discard+1&&gm.LootDiscard.Last()==dropped,"SD012 keeps exactly one Loot card and discards the other.");

            PutEquipmentTop("EQ002");eq=hero.AcquiredEquipment.Count;gm.DebugResolveSearchCard0827("SD017",ruins.Id,hero.Id);
            Check(gm.DebugPendingRewardChoiceMode()=="SearchGold30OrCommonEquipment","SD017 opens the authored 30 Gold or Common Equipment choice.");
            gm.DebugChooseRewardChoice("EQUIPMENT");Check(hero.AcquiredEquipment.Count==eq+1&&hero.AcquiredEquipment.Contains("EQ002"),"SD017 keeps a drawn Common Equipment card.");

            PutEquipmentTop("EQ003");PutEquipmentTop("EQ004");eq=hero.AcquiredEquipment.Count;gm.DebugResolveSearchCard0827("SD019",ruins.Id,hero.Id);
            Check(gm.DebugPendingRewardChoiceMode()=="SearchArmory2"&&gm.PendingRewardChoice.RevealedCardIds.Count==2,"SD019 reveals two qualifying Equipment cards without adding either to inventory early.");
            string keepEq=gm.PendingRewardChoice.RevealedCardIds[1];string bottomEq=gm.PendingRewardChoice.RevealedCardIds[0];gm.DebugChooseRewardChoice("KEEP1");
            Check(hero.AcquiredEquipment.Count==eq+1&&hero.AcquiredEquipment.Last()==keepEq&&gm.EquipmentDeck.Last()==bottomEq,"SD019 keeps one chosen Common/Uncommon Equipment and bottoms the other.");

            PutEquipmentTop("EQ008");PutEquipmentTop("EQ012");gold=gm.PartyGold;gm.DebugResolveSearchCard0827("SD019",ruins.Id,hero.Id);
            Check(!gm.HasPendingRewardChoice&&gm.PartyGold==gold+30&&gm.EquipmentDeck.TakeLast(2).Contains("EQ008")&&gm.EquipmentDeck.TakeLast(2).Contains("EQ012"),"SD019 bottoms both ineligible cards and grants 30 Gold when neither qualifies.");

            var gearHero=gm.ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated&&h.StartingEquipment.Any(e=>string.Equals(e.Type,"Weapon",StringComparison.OrdinalIgnoreCase)))??hero;
            if(!gearHero.AcquiredEquipment.Contains("EQ004"))gearHero.AcquiredEquipment.Add("EQ004");
            var oldWeapons=gearHero.StartingEquipment.Where(e=>string.Equals(e.Type,"Weapon",StringComparison.OrdinalIgnoreCase)).SelectMany(e=>Enumerable.Repeat(e.Id,Mathf.Max(1,e.Copies))).ToList();
            int nonWeaponAttack=gearHero.StartingEquipment.Where(e=>!string.Equals(e.Type,"Weapon",StringComparison.OrdinalIgnoreCase)).Sum(e=>e.Attack*Mathf.Max(1,e.Copies));
            int expectedAttack=gearHero.BaseAttack+nonWeaponAttack+RewardContent0826.Equipment["EQ004"].Attack;
            Check(gm.DebugEquipAcquiredEquipment(gearHero.Id,"EQ004"),"Acquired Equipment can be equipped through the real loadout runtime.");
            Check(gearHero.StartingEquipment.Any(e=>e.Id=="EQ004")&&oldWeapons.All(id=>gearHero.AcquiredEquipment.Contains(id))&&gearHero.Attack==expectedAttack,"Equipping a Weapon swaps the old Weapon package back to inventory and recalculates Attack from base + equipped gear.");

            var kael=gm.Heroes.TryGetValue("Kael",out var k)?k:null;
            if(kael!=null)
            {
                int original=kael.Attack;if(!kael.AcquiredEquipment.Contains("EQ001"))kael.AcquiredEquipment.Add("EQ001");gm.DebugEquipAcquiredEquipment(kael.Id,"EQ001");
                Check(kael.Attack==kael.BaseAttack+RewardContent0826.Equipment["EQ001"].Attack,"Kael's Unarmed +2 does not stack with an equipped Weapon.");
            }

            gm.SelectHero(gearHero.Id);gm.BeginEquipmentManagement();
            Check(gm.HasPendingRewardChoice&&gm.DebugPendingRewardChoiceMode()=="ManageEquipment"&&gm.RewardChoiceOptions.Any(o=>o.Key=="CLOSE"),"GEAR button runtime opens a player-facing persistent Equipment management panel.");
            gm.DebugChooseRewardChoice("CLOSE");Check(!gm.HasPendingRewardChoice,"Equipment management closes cleanly without consuming a Hero Action.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.27 TEST] PASS — Search Rewards & Equipment Management completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.27 TEST] FAIL — Search Rewards & Equipment Management completed: {pass} passed, {fail} failed.");
            yield break;
        }
    }
}
#endif
