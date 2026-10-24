#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class SearchDeckExecutionTestTools0852A
    {
        [MenuItem("Ascendance/Validation/Test 0.8.52a - Full Search Deck Execution (Play Mode)")]
        public static void Test0852A()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.52a TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.52a TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.52a TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.52a TEST] FAIL — "+label);}}

            Check(AscendanceGameManager.BuildVersion=="0.8.52a","Build identifies the Full Search Deck Execution milestone.");
            FullContentCatalog0850.EnsureLoaded();
            Check(FullContentCatalog0850.SearchCardCount==100,"v10.52 catalog still loads all 100 Search definitions.");
            Check(SearchEncounterContent0820.SearchCards.Count==100,"live Search catalog contains all 100 Search cards.");
            Check(SearchEncounterContent0820.BuildSearchDeck().Count==104,"physical Search deck contains all 104 authored copies.");
            var allSearchIds=Enumerable.Range(1,100).Select(i=>$"SD{i:000}").ToArray();
            Check(allSearchIds.All(id=>SearchEncounterContent0820.SearchCards.ContainsKey(id)),"Search catalog contains contiguous SD001-SD100.");
            Check(allSearchIds.All(gm.IsSearchCardExecutable0852),"all SD001-SD100 have an executable runtime path.");
            Check(HeroDatabase.Heroes.Count==16&&HeroDatabase.CardDefinitionCount==323,"0.8.51 Hero content remains intact.");
            Check(FullContentCatalog0850.LocationCount==236&&FullContentCatalog0850.EncounterCount==105,"0.8.50 full authored Location/Encounter catalogs remain intact.");

            gm.SetParty(new[]{"Garruk"},"Garruk",true);gm.DebugStartQuest0829("Q001");yield return null;
            var hero=gm.Heroes["Garruk"];gm.DebugSetHeroAt0829("Garruk","L000",3);
            Check(gm.ExplorationDeck.Count==8&&gm.Locations.Values.Count(l=>l.Revealed)==1,"Q001 still starts with Stonebrook only and eight hidden Exploration Locations.");
            Check(hero.HealingPotions==1,"Q001 starting Healing Potion rule remains intact.");
            int earlyLoot=hero.LootInventory.Count; gm.DebugSetPartyGold(0);
            Check(gm.DebugResolveSearchCard0852("SD007","L000","Garruk")&&gm.PartyGold==30&&hero.LootInventory.Count==earlyLoot+1,"legacy SD007 now fully resolves Gold + Loot with no pending-content fallback.");
            Check(!gm.CanSearchCurrentLocation(hero),"normal Search remains blocked while Engaged.");

            var location=gm.Locations["L000"];
            location.Searched=false;hero.FreeSearchLocationId=location.Id;int grantedActions=hero.Actions;
            Check(gm.CanSearchCurrentLocation(hero),"effect-granted Search remains legal while Engaged.");
            gm.SearchCurrentLocation();yield return null;
            Check(location.Searched&&hero.Actions==grantedActions,"effect-granted Search still costs no additional Action.");

            // SD036 — round-scoped Explore lock.
            gm.DebugStartQuest0829("Q001");yield return null;hero=gm.Heroes["Garruk"];gm.DebugSetHeroAt0829("Garruk","L000",3);
            Check(gm.DebugResolveSearchCard0852("SD036","L000","Garruk"),"SD036 Safe Campsite resolves through Search runtime.");
            Check(gm.DebugExploreBlocked0852("L000"),"SD036 blocks another normal Explore from that origin this round.");

            // SD043 — first damaging attack armor protection.
            int hpBefore=hero.Health; int armorBefore=gm.DebugEffectiveHeroArmor("Garruk");
            gm.DebugResolveSearchCard0852("SD043","L000","Garruk");
            Check(hero.HasStatus("Prepared Position")&&gm.DebugEffectiveHeroArmor("Garruk")==armorBefore+1,"SD043 Prepared Position grants +1 Armor against the first damaging Enemy Attack.");
            gm.DebugApplyHeroDamage("Garruk",1,"0.8.52a direct-damage validator");
            Check(hero.HasStatus("Prepared Position")&&hero.Health==hpBefore-1,"non-Attack damage does not consume Prepared Position.");
            var attacker=gm.Enemies.Values.FirstOrDefault(e=>!e.Defeated);
            Check(attacker!=null&&gm.DebugApplyEnemyAttackDamage0852("Garruk",attacker.Id,1)==1&&!hero.HasStatus("Prepared Position"),"the first damaging Enemy Attack consumes Prepared Position.");

            // SD058 — Rubble movement surcharge is real and one-use.
            gm.DebugResolveSearchCard0852("SD058","L000","Garruk");
            gm.Locations["L001"].Revealed=true;
            Check(gm.DebugRequiredPaidActionsToMove("Garruk","L001")>=2,"SD058 Rubble raises the next voluntary Move cost by 1 Action.");

            // SD059 — healing lock.
            hero.Health=Mathf.Max(1,hero.MaxHealth-3);int wounded=hero.Health;
            gm.DebugResolveSearchCard0852("SD059","L000","Garruk");int afterSting=hero.Health;
            gm.HealHero(hero,2,"0.8.52a validator",hero);
            Check(hero.HasStatus("No Healing")&&hero.Health==afterSting&&afterSting<=wounded,"SD059 blocks Health recovery for the rest of the turn.");

            // SD089 — retained utility card.
            gm.DebugResolveSearchCard0852("SD089","L000","Garruk");
            Check(hero.KeptSearchEffects0852.Contains("SD089"),"SD089 Goblin War Plans is retained for later use.");
            Check(gm.CanUseSearchUtility0852(hero)&&gm.SearchUtilityLabel0852(hero).Contains("War Plans"),"retained Goblin War Plans exposes a usable UI action.");
            gm.SelectHero("Garruk");Check(gm.UseSearchUtility0852(),"Goblin War Plans can be discarded through the live utility action.");
            Check(!hero.KeptSearchEffects0852.Contains("SD089")&&gm.NextTravelEncounterSeverityModifier<=-2,"Goblin War Plans queues its -2 next-Encounter Severity modifier.");

            // SD092 — condition ward.
            gm.DebugResolveSearchCard0852("SD092","L000","Garruk");
            Check(gm.DebugSearchWardActive0852,"SD092 Shamanic Markings arms the party condition ward.");
            gm.AddStatus(hero,"Poisoned",0,0,"0.8.52a validator",false,hero);
            Check(!hero.HasStatus("Poisoned")&&!gm.DebugSearchWardActive0852,"Shamanic Markings prevents and consumes the next listed harmful Condition.");

            // SD098 — Q003-specific beacon discount state.
            gm.DebugStartQuest0829("Q003");yield return null;hero=gm.Heroes["Garruk"];
            var q3loc=gm.Locations.Values.FirstOrDefault(l=>l.Revealed)??gm.Locations["L000"];
            gm.DebugResolveSearchCard0852("SD098",q3loc.Id,"Garruk");
            Check(hero.HasStatus("Beacon Instructions"),"SD098 Beacon Instructions creates the Q003 one-Action discount state.");

            // SD100 — party heal + cleanse.
            hero.Health=Mathf.Max(1,hero.MaxHealth-4);gm.AddStatus(hero,"Bleeding",0,0,"validator",false,hero);int preHope=hero.Health;
            gm.DebugResolveSearchCard0852("SD100",q3loc.Id,"Garruk");
            Check(hero.Health>preHope,"SD100 Hope Endures heals the Hero.");
            Check(!hero.HasStatus("Bleeding"),"SD100 Hope Endures removes one negative Condition.");

            // Save-facing state is exposed and bounded to the current round when active.
            Check(gm.SearchTravelSeverityRoundForSave0852==-1||gm.SearchTravelSeverityRoundForSave0852==gm.Round,"round-scoped Search Travel state is save-safe.");
            Check(gm.SearchGoblinSeverityRoundForSave0852==-1||gm.SearchGoblinSeverityRoundForSave0852==gm.Round,"Goblin-only Search Severity state is save-safe.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.52a TEST] PASS — Full Search Deck Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.52a TEST] FAIL — Full Search Deck Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
