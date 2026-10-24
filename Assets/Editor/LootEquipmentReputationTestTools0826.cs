#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class LootEquipmentReputationTestTools0826
    {
        [MenuItem("Ascendance/Validation/Test 0.8.26 - Loot Equipment & Reputation (Play Mode)")]
        public static void TestLootEquipmentReputation()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.26 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.26 TEST] Game manager not found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.26 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.26 TEST] FAIL — {message}");}}
            var hero=gm.ParticipatingHeroes.Where(h=>!h.IsDefeated).OrderBy(h=>h.PartyIndex).FirstOrDefault();
            if(hero==null){Debug.LogError("[ASCENDANCE][0.8.26 TEST] FAIL — Need at least 1 living Hero.");yield break;}
            var forest=gm.Locations["L002"];var stonebrook=gm.Locations["L000"];forest.Revealed=stonebrook.Revealed=true;

            void PutEncounterTop(string id){gm.DebugSetEncounterDeckTop(id);}
            void PutLootTop(string id)
            {
                int i=gm.LootDeck.FindIndex(x=>x==id);if(i>=0)gm.LootDeck.RemoveAt(i);else{i=gm.LootDiscard.FindIndex(x=>x==id);if(i>=0)gm.LootDiscard.RemoveAt(i);}
                gm.LootDeck.Insert(0,id);
            }
            void PutEquipmentTop(string id)
            {
                int i=gm.EquipmentDeck.FindIndex(x=>x==id);if(i>=0)gm.EquipmentDeck.RemoveAt(i);else{i=gm.EquipmentDiscard.FindIndex(x=>x==id);if(i>=0)gm.EquipmentDiscard.RemoveAt(i);}
                gm.EquipmentDeck.Insert(0,id);
            }
            void KillEncounter(string instanceId)
            {
                var encounter=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==instanceId);if(encounter==null)return;
                foreach(var id in encounter.SpawnedEnemyIds.ToList())if(gm.Enemies.TryGetValue(id,out var enemy)&&!enemy.Defeated)gm.DealDirectDamage(enemy,enemy.Health,"0.8.26 validation",hero);
            }

            Check(RewardContent0826.Loot.Count==190&&RewardContent0826.LootPhysicalCopyCount==254,"Workbook Loot catalog contains 190 definitions / 254 physical copies.");
            Check(RewardContent0826.Equipment.Count==144,"Workbook Equipment catalog contains 144 authored cards.");
            Check(gm.LootDeck.Count==254&&gm.EquipmentDeck.Count==144,"New campaign initializes the real Loot and Equipment decks.");

            // EN004 — automatic Loot reward belongs to the triggering Hero.
            PutLootTop("LT001");int lootBefore=hero.LootInventory.Count;PutEncounterTop("EN004");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string wolfId=gm.DebugLatestActiveEncounterId();KillEncounter(wolfId);
            Check(hero.LootInventory.Count==lootBefore+1&&hero.LootInventory.Last()=="LT001","EN004 victory draws the actual top Loot card into the triggering Hero's Inventory.");
            Check(gm.LootDeck.FirstOrDefault()!="LT001","A gained Loot card leaves the live Loot deck.");

            // EN005 — real reward choice instead of pending text.
            PutLootTop("LT002");int goldBeforeChoice=gm.PartyGold;lootBefore=hero.LootInventory.Count;PutEncounterTop("EN005");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string wargId=gm.DebugLatestActiveEncounterId();KillEncounter(wargId);
            Check(gm.HasPendingRewardChoice&&gm.DebugPendingRewardChoiceMode()=="Gold25OrLoot1","EN005 opens a player-facing 25 Gold or 1 Item reward choice.");
            Check(gm.DebugChooseRewardChoice("LOOT")&&!gm.HasPendingRewardChoice&&hero.LootInventory.Count==lootBefore+1&&hero.LootInventory.Last()=="LT002"&&gm.PartyGold==goldBeforeChoice,"Choosing EN005's Item option draws Loot without also granting Gold.");

            // EN008 severity 3-4 — pay 10 Gold for +1 General Reputation.
            gm.DebugSetPartyGold(50);gm.DebugSetGeneralReputation(0);PutEncounterTop("EN008");gm.DebugResolveTravelEncounter(stonebrook.Id,hero.Id,2);
            Check(gm.HasPendingRewardChoice&&gm.DebugPendingRewardChoiceMode()=="Pay10RepOrContinue","EN008 Severity 3-4 opens the authored pay-or-continue choice.");
            Check(gm.DebugChooseRewardChoice("PAY")&&gm.PartyGold==40&&gm.GeneralReputation==1,"EN008 payment spends 10 Party Gold and grants exactly +1 General Reputation.");

            // Campaign I legacy Reputation clamp.
            gm.DebugSetGeneralReputation(6);PutEncounterTop("EN019");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            Check(gm.GeneralReputation==6,"General Reputation is clamped at the authored +6 maximum.");
            gm.DebugSetGeneralReputation(0);

            // EN009 Severity 7 — immediate Loot + Equipment before Saboteur combat.
            PutLootTop("LT003");PutEquipmentTop("EQ001");int eqBefore=hero.AcquiredEquipment.Count;lootBefore=hero.LootInventory.Count;PutEncounterTop("EN009");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,6);
            string campId=gm.DebugLatestActiveEncounterId();
            Check(hero.LootInventory.Count==lootBefore+1&&hero.LootInventory.Last()=="LT003","EN009 Severity 7 immediately draws its Loot reward.");
            Check(hero.AcquiredEquipment.Count==eqBefore+1&&hero.AcquiredEquipment.Last()=="EQ001","EN009 Severity 7 draws Equipment into Inventory unequipped.");
            KillEncounter(campId);

            // EN017 Severity 7 — reversible Equipment reward if both Elite Goblins escape.
            PutEquipmentTop("EQ002");eqBefore=hero.AcquiredEquipment.Count;PutEncounterTop("EN017");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,6);
            string cacheId=gm.DebugLatestActiveEncounterId();var cache=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==cacheId);
            Check(hero.AcquiredEquipment.Count==eqBefore+1&&hero.AcquiredEquipment.Last()=="EQ002"&&cache!=null&&cache.GainedEquipmentIds.Contains("EQ002"),"EN017 tracks the exact Equipment gained by its encounter instance.");
            if(cache!=null)
            {
                foreach(var id in cache.SpawnedEnemyIds.ToList())gm.DebugRemoveEnemyFromPlayWithoutDefeat(id);
            }
            Check(hero.AcquiredEquipment.Count==eqBefore&&gm.EquipmentDiscard.Contains("EQ002"),"EN017 discards the gained Equipment when both Elite Goblins leave play undefeated.");

            // EN024 — conditional Reputation reward is now real.
            gm.DebugSetGeneralReputation(0);int goldBeforeFire=gm.PartyGold;PutEncounterTop("EN024");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,2);
            string fireId=gm.DebugLatestActiveEncounterId();var fire=gm.DebugFirstEncounterObject(fireId,"Fire Token");
            string priorFireTestLocation=hero.LocationId;hero.LocationId=forest.Id;bool extinguished=fire!=null&&gm.DebugInteractEncounterObject(hero.Id,fire.ObjectId);hero.LocationId=priorFireTestLocation;
            if(!extinguished)Debug.LogError("[ASCENDANCE][0.8.26 TEST] SETUP ERROR — EN024 Fire Token could not be extinguished through the real Location-gated interaction path.");
            KillEncounter(fireId);
            Check(gm.GeneralReputation==1,"EN024 awards +1 General Reputation when all Fire Tokens were removed.");
            Check(gm.PartyGold==goldBeforeFire+30,"EN024 still awards its authored 30 Gold victory reward.");

            // EN025 — compound Gold + Loot reward.
            PutLootTop("LT004");int goldBeforeBeasts=gm.PartyGold;lootBefore=hero.LootInventory.Count;PutEncounterTop("EN025");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,2);
            string beastId=gm.DebugLatestActiveEncounterId();KillEncounter(beastId);
            Check(gm.PartyGold==goldBeforeBeasts+35&&hero.LootInventory.Count==lootBefore+1&&hero.LootInventory.Last()=="LT004","EN025 awards 35 Gold and draws 1 Loot card as one resolved reward.");

            Check(!gm.HasPendingRewardChoice,"All 0.8.26 reward choices used by the validator resolve cleanly.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.26 TEST] PASS — Loot, Equipment & Reputation Integration completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.26 TEST] FAIL — Loot, Equipment & Reputation Integration completed: {pass} passed, {fail} failed.");
            yield break;
        }
    }
}
#endif
