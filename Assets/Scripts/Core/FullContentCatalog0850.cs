using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    [Serializable] public sealed class FullEquipment0850 { public string id,name,type,family,rarity,slot,hands,damageType,ability1,ability2,status; public int value,attack,armor,range; }
    [Serializable] public sealed class FullLoot0850 { public string id,name,category,rarity,dropTags,usedBy,effect,status; public int value,stackLimit,copies; public bool consumable,craftingMaterial,questItem; }
    [Serializable] public sealed class FullSearchCard0850 { public string id,name,category,terrain,rarity,trigger,effect,followUp,flavorText,status; public int copies; }
    [Serializable] public sealed class FullEncounter0850 { public int copies; public string id,name,encounterType,terrain,trigger,severity12,severity34,severity56,severity7Plus,reward,failure,worldEventScaling,flavorText,status; }
    [Serializable] public sealed class FullEnemy0850 { public string id,name,enemyType,role,behaviorProfile,difficulty,traits,keywords,attackText,attackDice,range,abilities,behavior,flavorText,recommendedUses,status; public int health,attack,armor,movement,threatRating; }
    [Serializable] public sealed class FullLocation0850 { public string id,name,terrain,canConnectTo,difficulty,traits,keywords,specialRules,flavorText,requiredConnection,region,locationType,revealEffect,firstVisitEffect,ongoingEffect,search,searchDeck,encounterDeck,pointsOfInterest,resourceNode,storyEntry,designNotes,recommendedUses,status; public bool north,east,south,west; public int threatRating,searchLimit,encounterLevel; }
    [Serializable] public sealed class FullCraftingRecipe0850 { public string id,name,profession,tier,requiredMaterials,requiredTool,craftTime,resultId,result,unlockRequirement,flavorText,status; public int goldCost; }
    [Serializable] public sealed class FullQuest0850 { public string id,name,worldEvent,questType,story,startingLocation,locationPool,enemyDeck,startingEnemiesTokens,questRules,winningConditions,losingConditions,rewards,campaignFollowOn,status,locationConsistency,enemyConsistency,heroCountScaling,balanceNotes; }
    [Serializable] public sealed class FullWorldEvent0850 { public string Card_ID,Event_Name,Story,Campaign_Effects,Available_Quests,Advance_Condition,Campaign_Rewards,Campaign_Penalties,World_Event_Modifier,Design_Notes,Status; }
    [Serializable] public sealed class FullStatus0850 { public string id; }
    [Serializable] public sealed class FullKeyword0850 { public string id; }
    [Serializable] public sealed class FullWorldRegion0850 { public string id; }
    [Serializable] public sealed class FullFactionReputation0850 { public string id; }

    [Serializable]
    public sealed class FullContentRoot0850
    {
        public List<FullEquipment0850> equipment=new();
        public List<FullLoot0850> loot=new();
        public List<FullSearchCard0850> searchCards=new();
        public List<FullEncounter0850> encounters=new();
        public List<FullEnemy0850> enemies=new();
        public List<FullLocation0850> locations=new();
        public List<FullCraftingRecipe0850> craftingRecipes=new();
        public List<FullQuest0850> quests=new();
        public List<FullWorldEvent0850> worldEvents=new();
        public List<FullStatus0850> statuses=new();
        public List<FullKeyword0850> keywords=new();
        public List<FullWorldRegion0850> worldRegions=new();
        public List<FullFactionReputation0850> factionReputation=new();
    }

    /// <summary>
    /// 0.8.50 authoritative content catalog generated from Ascendance v10.52.
    /// Loading a row means the game knows the authored content. Specialized runtime execution remains
    /// deliberately separate so future campaigns can be imported without silently inventing mechanics.
    /// </summary>
    public static class FullContentCatalog0850
    {
        private const string ResourcePath="Ascendance/Data/full_content_0850";
        private static bool loaded;
        private static FullContentRoot0850 root;
        private static readonly Dictionary<string,FullLocation0850> locationById=new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string,FullQuest0850> questById=new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string,FullCraftingRecipe0850> recipeById=new(StringComparer.OrdinalIgnoreCase);

        public static FullContentRoot0850 Root { get { EnsureLoaded(); return root; } }
        public static IReadOnlyList<FullLocation0850> Locations { get { EnsureLoaded(); return root.locations; } }
        public static IReadOnlyList<FullCraftingRecipe0850> CraftingRecipes { get { EnsureLoaded(); return root.craftingRecipes; } }
        public static IReadOnlyList<FullQuest0850> Quests { get { EnsureLoaded(); return root.quests; } }
        public static int EquipmentCount => Root.equipment.Count;
        public static int LootCount => Root.loot.Count;
        public static int SearchCardCount => Root.searchCards.Count;
        public static int EncounterCount => Root.encounters.Count;
        public static int EnemyCount => Root.enemies.Count;
        public static int LocationCount => Root.locations.Count;
        public static int CraftingRecipeCount => Root.craftingRecipes.Count;
        public static int QuestCount => Root.quests.Count;
        public static int WorldEventCount => Root.worldEvents.Count;
        public static int StatusCount => Root.statuses?.Count??0;
        public static int KeywordCount => Root.keywords?.Count??0;
        public static int WorldRegionRowCount => Root.worldRegions?.Count??0;
        public static int FactionReputationRowCount => Root.factionReputation?.Count??0;

        public static void EnsureLoaded()
        {
            if(loaded)return;
            loaded=true;
            var asset=Resources.Load<TextAsset>(ResourcePath);
            if(asset==null){root=new FullContentRoot0850();Debug.LogError("[ASCENDANCE][0.8.50] Full authored content resource is missing: "+ResourcePath);return;}
            root=JsonUtility.FromJson<FullContentRoot0850>(asset.text)??new FullContentRoot0850();
            root.equipment??=new();root.loot??=new();root.searchCards??=new();root.encounters??=new();root.enemies??=new();root.locations??=new();root.craftingRecipes??=new();root.quests??=new();root.worldEvents??=new();root.statuses??=new();root.keywords??=new();root.worldRegions??=new();root.factionReputation??=new();
            foreach(var x in root.locations)if(x!=null&&!string.IsNullOrWhiteSpace(x.id))locationById[x.id]=x;
            foreach(var x in root.quests)if(x!=null&&!string.IsNullOrWhiteSpace(x.id))questById[x.id]=x;
            foreach(var x in root.craftingRecipes)if(x!=null&&!string.IsNullOrWhiteSpace(x.id))recipeById[x.id]=x;
        }

        public static bool TryGetLocation(string id,out FullLocation0850 value){EnsureLoaded();return locationById.TryGetValue(id??string.Empty,out value);}
        public static bool TryGetQuest(string id,out FullQuest0850 value){EnsureLoaded();return questById.TryGetValue(id??string.Empty,out value);}
        public static bool TryGetRecipe(string id,out FullCraftingRecipe0850 value){EnsureLoaded();return recipeById.TryGetValue(id??string.Empty,out value);}

        public static string StartingLocationId(string questId)
        {
            if(!TryGetQuest(questId,out var q))return "L000";
            var m=Regex.Match(q.startingLocation??string.Empty,@"L\d{3}",RegexOptions.IgnoreCase);
            return m.Success?m.Value.ToUpperInvariant():"L000";
        }

        public static HashSet<string> QuestLocationPoolIds(string questId)
        {
            var set=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if(!TryGetQuest(questId,out var q))return set;
            foreach(Match m in Regex.Matches(q.locationPool??string.Empty,@"L\d{3}",RegexOptions.IgnoreCase))set.Add(m.Value.ToUpperInvariant());
            // The required starting Location is scenario setup, not an Exploration draw.
            set.Remove(StartingLocationId(questId));
            return set;
        }

        public static bool IsCampaignIEncounterId(string id)
        {
            var m=Regex.Match(id??string.Empty,@"^EN(\d{3})$",RegexOptions.IgnoreCase);
            return m.Success&&int.TryParse(m.Groups[1].Value,out int n)&&n>=1&&n<=30;
        }
    }
}
