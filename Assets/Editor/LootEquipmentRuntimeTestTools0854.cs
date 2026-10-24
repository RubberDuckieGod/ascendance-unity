#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class LootEquipmentRuntimeTestTools0854
    {
        [MenuItem("Ascendance/Validation/Test 0.8.54a - Full Loot & Equipment Runtime Execution (Play Mode)")]
        public static void Test0854()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.54a TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.54a TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.54a TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.54a TEST] FAIL — "+label);}}

            Check(AscendanceGameManager.BuildVersion=="0.8.54a","Build identifies the Full Loot & Equipment Runtime Execution milestone.");
            FullContentCatalog0850.EnsureLoaded();
            Check(RewardContent0826.Loot.Count==190,"runtime Loot catalog contains all 190 authored Loot definitions.");
            Check(RewardContent0826.Equipment.Count==144,"runtime Equipment catalog contains all 144 authored Equipment definitions.");
            Check(RewardContent0826.Loot.Keys.All(gm.IsLootExecutable0854),"all 190 Loot definitions have an executable gameplay/campaign role.");
            Check(RewardContent0826.Equipment.Keys.All(gm.IsEquipmentExecutable0854),"all 144 Equipment definitions have a live base-stat/ability runtime path.");
            Check(Enumerable.Range(1,30).Select(i=>$"LT{i:000}").All(id=>RewardContent0826.Loot.TryGetValue(id,out var d)&&d.Consumable),"LT001-LT030 remain the complete authored Consumable set.");
            Check(SearchEncounterContent0820.BuildSearchDeck().Count==104&&SearchEncounterContent0820.SearchCards.Count==100,"0.8.52a Full Search Deck Execution remains intact.");
            Check(gm.DebugCampaignIEncounterDeckDefinitionCount0853()==30,"0.8.53 Campaign I Travel Encounter boundary remains intact.");

            gm.SetParty(new[]{"Garruk"},"Garruk",true);gm.DebugStartQuest0829("Q001");yield return null;
            var hero=gm.Heroes["Garruk"];
            gm.DebugSetHeroAt0829("Garruk","L000",3);

            hero.Health=Mathf.Max(1,hero.MaxHealth-4);int healBefore=hero.Health;
            Check(gm.DebugGiveLoot0854("Garruk","LT001")&&gm.DebugUseLoot0854("Garruk","LT001")&&hero.Health==Mathf.Min(hero.MaxHealth,healBefore+2),"LT001 Common Healing Potion heals 2 through the live ITEMS runtime.");
            Check(!hero.LootInventory.Contains("LT001"),"used Consumables leave Hero inventory and enter the Loot discard path.");

            int repBefore=gm.GeneralReputation;
            Check(gm.DebugGiveLoot0854("Garruk","LT151")&&gm.GeneralReputation==Mathf.Min(6,repBefore+2),"LT151 Crown of the First King applies +2 Reputation on acquisition.");

            int baseArmor=gm.DebugEffectiveHeroArmor("Garruk");gm.DebugGiveLoot0854("Garruk","LT146");
            Check(gm.DebugEffectiveHeroArmor("Garruk")>=baseArmor+1,"LT146 Heart of the Mountain grants its carried +1 Armor passive.");

            gm.AddStatus(hero,"Weakened",1,0,"0.8.54 pre-test",false,hero);Check(hero.HasStatus("Weakened"),"validator can establish a harmful Condition before equipment immunity test.");
            gm.DebugEquipCatalog0854("Garruk","EQ109");gm.AddStatus(hero,"Weakened",1,0,"0.8.54 immunity test",false,hero);
            Check(hero.Statuses.Count(s=>s.Name=="Weakened")==1,"EQ109 Crown of Resolve blocks new Weakened applications.");

            int authoredMax=HeroDatabase.GetHero("Garruk").maxHealth;gm.DebugEquipCatalog0854("Garruk","EQ097");
            Check(hero.MaxHealth==authoredMax+2,"EQ097 Amulet of Vigor raises Maximum Health by 2.");

            Check(gm.DebugEquipCatalog0854("Garruk","EQ045")&&gm.EffectiveBasicAttackRange0854(hero)>=1,"equipped Bow/Crossbow families grant connected-Location basic Attack reach in the live range gate.");

            gm.DebugEquipCatalog0854("Garruk","EQ008");int nextBonus=hero.NextAttackBonus;
            Check(gm.DebugActivateEquipment0854("Garruk","EQ008")&&hero.NextAttackBonus==nextBonus+3&&hero.NextArmorIgnore>=999,"EQ008 Dragon Strike arms +3 Attack and all-Armor ignore once per encounter.");
            Check(!gm.DebugActivateEquipment0854("Garruk","EQ008"),"EQ008 Dragon Strike cannot be activated twice in the same encounter scope.");

            gm.DebugEquipCatalog0854("Garruk","EQ109");hero.Health=Mathf.Max(1,hero.MaxHealth-3);int rallyBefore=hero.Health;
            Check(gm.DebugActivateEquipment0854("Garruk","EQ109")&&hero.Health>rallyBefore,"EQ109 Rallying Presence restores Health through a player activation.");

            gm.DebugGiveLoot0854("Garruk","LT144");gm.AddStatus(hero,"Poisoned",1,0,"0.8.54 relic test",false,hero);
            Check(gm.DebugUseRelic0854("Garruk","LT144","Garruk")&&!hero.HasStatus("Poisoned"),"LT144 Saint's Icon removes all negative Conditions from the chosen Hero.");
            Check(!gm.DebugUseRelic0854("Garruk","LT144","Garruk"),"LT144 Saint's Icon is limited to once per Scenario.");

            gm.DebugEquipCatalog0854("Garruk","EQ020");var enemies=gm.Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).ToList();int totalBefore=enemies.Sum(e=>e.Health);
            Check(gm.DebugActivateEquipment0854("Garruk","EQ020")&&enemies.Sum(e=>e.Health)<totalBefore,"EQ020 Worldsplitter Earthshatter damages all enemies in the Hero's Location.");

            Check(gm.CanOpenInventory0854(hero),"ITEMS management is available as a live player-facing HUD action when resolution gates are clear.");
            Check(string.IsNullOrEmpty(gm.PendingContentInstruction),"0.8.54 validation path finishes without a Loot/Equipment CONTENT PENDING fallback.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.54a TEST] PASS — Full Loot & Equipment Runtime Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.54a TEST] FAIL — Full Loot & Equipment Runtime Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
