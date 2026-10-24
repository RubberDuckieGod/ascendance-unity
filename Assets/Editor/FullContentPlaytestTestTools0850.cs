#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class FullContentPlaytestTestTools0850
    {
        [MenuItem("Ascendance/Validation/Test 0.8.50 - Full Authored Content + Playtest Rules (Play Mode)")]
        public static void Test0850()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.50 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.50 TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.50 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.50 TEST] FAIL — "+label);}}

            Check(AscendanceGameManager.BuildVersion=="0.8.50","Build identifies the Full Authored Content + Playtest Rules milestone.");
            FullContentCatalog0850.EnsureLoaded();
            Check(FullContentCatalog0850.EquipmentCount==144,"v10.52 catalog loads all 144 Equipment definitions.");
            Check(FullContentCatalog0850.LootCount==190,"v10.52 catalog loads all 190 Loot definitions.");
            Check(FullContentCatalog0850.SearchCardCount==100,"v10.52 catalog loads all 100 Search cards.");
            Check(FullContentCatalog0850.EncounterCount==105,"v10.52 catalog loads all 105 Encounter cards.");
            Check(FullContentCatalog0850.EnemyCount==125,"v10.52 catalog loads all 125 Enemy definitions.");
            Check(FullContentCatalog0850.LocationCount==236,"v10.52 catalog loads all 236 Location definitions.");
            Check(FullContentCatalog0850.CraftingRecipeCount==200,"v10.52 catalog loads all 200 Crafting Recipe definitions.");
            Check(FullContentCatalog0850.QuestCount==72,"v10.52 catalog loads all 72 Quest definitions.");
            Check(FullContentCatalog0850.WorldEventCount==24,"v10.52 catalog loads all 24 World Event definitions.");
            Check(FullContentCatalog0850.StatusCount==102,"v10.52 catalog loads all 102 Status rows.");
            Check(FullContentCatalog0850.KeywordCount==65,"v10.52 catalog loads all 65 Keyword/reference rows.");
            Check(FullContentCatalog0850.WorldRegionRowCount==13,"v10.52 catalog loads all 13 World Region framework rows.");
            Check(FullContentCatalog0850.FactionReputationRowCount==58,"v10.52 catalog loads all 58 Faction Reputation framework rows.");
            Check(HeroDatabase.Heroes.Count==16&&HeroDatabase.CardDefinitionCount==323,"existing complete Hero content remains 16 Heroes and 323 Hero-card definitions.");
            Check(FullContentCatalog0850.TryGetLocation("L235",out _),"late-catalog Location L235 is available.");
            Check(FullContentCatalog0850.TryGetRecipe("CR200",out _),"late-catalog Crafting Recipe CR200 is available.");
            Check(FullContentCatalog0850.TryGetQuest("Q072",out _),"late-catalog Quest Q072 is available.");

            Check(RewardContent0826.Loot.Count==190&&RewardContent0826.Equipment.Count==144,"live reward catalogs use the complete Loot and Equipment tables.");
            Check(RewardContent0826.Loot.Values.Count(x=>x.CraftingMaterial)==79,"all 79 v10.52 Loot rows marked as crafting materials are present.");
            Check(RewardContent0826.BuildLootDeck().Count==254,"physical Loot deck contains all 254 authored copies.");
            Check(RewardContent0826.EquipmentHasAbility("EQ098")&&RewardContent0826.EquipmentLabel("EQ098").Contains("[ABILITY]"),"Equipment with a non-stat ability is visibly marked in UI labels.");
            Check(!string.IsNullOrWhiteSpace(RewardContent0826.EquipmentAbilityText("EQ098")),"Equipment ability text is available to Inventory/Loadout UI.");

            Check(SearchEncounterContent0820.SearchCards.Count==100,"live Search catalog contains all 100 cards.");
            Check(SearchEncounterContent0820.BuildSearchDeck().Count==104,"physical Search deck contains all 104 authored copies.");
            Check(SearchEncounterContent0820.EncounterCards.Count==105,"all 105 Encounter definitions are available to regional deck builders.");
            Check(SearchEncounterContent0820.BuildEncounterDeck().Count==35&&SearchEncounterContent0820.BuildEncounterDeck().All(FullContentCatalog0850.IsCampaignIEncounterId),"Stonebrook active Travel deck remains the 35 physical EN001–EN030 cards only.");
            Check(SearchEncounterContent0820.EnemyTemplates.Count==125,"live Enemy template catalog contains all 125 enemies.");
            Check(CraftingContent0837.AuthoredRecipeCount0850==200,"Crafting system exposes all 200 authored recipe rows without inventing unsupported profession mechanics.");

            var one=new[]{"Garruk"};
            gm.SetParty(one,"Garruk",true);gm.DebugStartQuest0829("Q001");yield return null;
            Check(gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E001")==1&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E002")==1,"Q001 1-Hero opening scales to 1 Raider + 1 Archer.");
            Check(!gm.Enemies.Values.Any(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E006"),"Q001 never starts with the Goblin Captain in Stonebrook.");

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);gm.DebugStartQuest0829("Q001");yield return null;
            Check(gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E001")==2&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E002")==1,"Q001 2-Hero opening uses 2 Raiders + 1 Archer.");

            gm.SetParty(new[]{"Garruk","Aldric","Elara"},"Garruk",true);gm.DebugStartQuest0829("Q001");yield return null;
            Check(gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E001")==3&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E002")==1,"Q001 3-Hero opening uses 3 Raiders + 1 Archer.");

            gm.SetParty(new[]{"Garruk","Aldric","Elara","Rowan"},"Garruk",true);gm.DebugStartQuest0829("Q001");yield return null;
            Check(gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E001")==3&&gm.Enemies.Values.Count(e=>e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E002")==2,"Q001 4-Hero opening uses 3 Raiders + 2 Archers.");
            Check(gm.ParticipatingHeroes.All(h=>h.HealingPotions==1),"every participating Hero begins Q001 with exactly 1 Common Healing Potion.");
            Check(gm.Locations.Values.Count(l=>l.Revealed)==1&&gm.Locations["L000"].Revealed,"Q001 begins with only required starting Location Stonebrook revealed.");
            var q1Pool=FullContentCatalog0850.QuestLocationPoolIds("Q001");
            Check(q1Pool.SetEquals(new[]{"L001","L003","L006","L007","L017","L036","L043","L047"}),"Q001 Exploration pool matches all eight authored v10.52 Locations.");
            Check(gm.ExplorationDeck.Count==8,"Q001 Exploration deck contains the eight authored hidden Locations instead of a prebuilt revealed map.");

            var hero=gm.Heroes["Garruk"];gm.DebugSetHeroAt0829("Garruk","L000",3);
            Check(!gm.CanSearchCurrentLocation(hero),"Search is globally blocked while the acting Hero is Engaged.");

            string lookout=gm.Locations.Values.First(l=>l.AuthoredLocationId=="L006").Id;
            Check(gm.DebugRevealQuestLocation0829(lookout,"Garruk"),"Goblin Lookout can be revealed through Quest/Explore runtime.");yield return null;
            Check(gm.QuestRuntime0829.CaptainSpawned&&gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E006")==1,"Q001 Goblin Captain enters only when authored Goblin Lookout L006 is revealed.");
            Check(gm.Enemies.Values.Count(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId=="Q001"&&e.TemplateId=="E004")==1,"Goblin Lookout reveal also spawns the authored Goblin Scout.");

            // Successful implemented Equipment triggers must identify themselves in the Adventure Log.
            if(RewardContent0826.Equipment.TryGetValue("EQ098",out var charm))
                hero.StartingEquipment.Add(new EquipmentRuntimeData{Id=charm.Id,Name=charm.Name,Type=charm.Type,Copies=1,Attack=charm.Attack,Armor=charm.Armor,Ability1=charm.Ability1,Ability2=charm.Ability2});
            gm.Locations["L004"].Revealed=true;gm.Locations["L006"].Revealed=true;hero.UsedRoundAbilities.Remove("DifficultTerrain:EQ098");
            gm.DebugSetHeroAt0829("Garruk","L004",3);int before=gm.Log.Count;gm.RequestMove("L006");yield return null;
            Check(gm.Log.Skip(before).Any(x=>x.Contains("ITEM TRIGGER")&&x.Contains("Traveler's Charm")),"successful Equipment ability activation writes an explicit ITEM TRIGGER Adventure Log entry.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.50 TEST] PASS — Full Authored Content + Playtest Rules completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.50 TEST] FAIL — Full Authored Content + Playtest Rules completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
