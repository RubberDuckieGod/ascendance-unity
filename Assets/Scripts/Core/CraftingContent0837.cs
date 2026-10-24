using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    public enum CraftingOutputKind0837 { Loot, Equipment, HealingPotion }

    [Serializable]
    public sealed class CraftingIngredient0837
    {
        public string LootId;
        public int Count;
        public CraftingIngredient0837(string id,int count){LootId=id;Count=Math.Max(1,count);}
    }

    [Serializable]
    public sealed class CraftingRecipe0837
    {
        public string Id;
        public string Name;
        public DowntimeProfession0836 Profession;
        public int RequiredLevel;
        public int ProfessionXp;
        public CraftingOutputKind0837 OutputKind;
        public string OutputId;
        public int OutputCount=1;
        public string RequiredUnlockLootId;
        public List<CraftingIngredient0837> Ingredients=new();
    }

    // 0.8.37 — first authored digital crafting recipe catalog.
    // Materials are intentionally Hero-owned: the crafter must personally carry every ingredient.
    public static class CraftingContent0837
    {
        public static readonly IReadOnlyList<CraftingRecipe0837> Recipes=Build();
        // Full v10.52 authored recipe table. The existing four digital profession runtimes remain the
        // executable subset until their profession mechanics are migrated; no recipe row is lost.
        public static IReadOnlyList<FullCraftingRecipe0850> AuthoredRecipes0850=>FullContentCatalog0850.CraftingRecipes;
        public static int AuthoredRecipeCount0850=>FullContentCatalog0850.CraftingRecipeCount;

        public static IReadOnlyList<CraftingRecipe0837> ForProfession(DowntimeProfession0836 profession)=>Recipes.Where(r=>r.Profession==profession).ToList();
        public static CraftingRecipe0837 Get(string id)=>Recipes.FirstOrDefault(r=>string.Equals(r.Id,id,StringComparison.OrdinalIgnoreCase));

        private static List<CraftingRecipe0837> Build()
        {
            var r=new List<CraftingRecipe0837>();
            void Add(string id,string name,DowntimeProfession0836 p,int level,int xp,CraftingOutputKind0837 kind,string output,string unlock=null,params CraftingIngredient0837[] ingredients)
                =>r.Add(new CraftingRecipe0837{Id=id,Name=name,Profession=p,RequiredLevel=level,ProfessionXp=xp,OutputKind=kind,OutputId=output,RequiredUnlockLootId=unlock,Ingredients=ingredients.ToList()});
            CraftingIngredient0837 I(string id,int n)=>new CraftingIngredient0837(id,n);

            // Blacksmithing
            Add("CR-BS-01","Forge Whetstone",DowntimeProfession0836.Blacksmithing,1,40,CraftingOutputKind0837.Loot,"LT028",null,I("LT031",1),I("LT034",1));
            Add("CR-BS-02","Forge Iron Dagger",DowntimeProfession0836.Blacksmithing,1,40,CraftingOutputKind0837.Equipment,"EQ037",null,I("LT038",2),I("LT049",1));
            Add("CR-BS-03","Forge Steel Longsword",DowntimeProfession0836.Blacksmithing,2,60,CraftingOutputKind0837.Equipment,"EQ004",null,I("LT039",2),I("LT049",1));
            Add("CR-BS-04","Forge Iron Plate",DowntimeProfession0836.Blacksmithing,3,80,CraftingOutputKind0837.Equipment,"EQ091",null,I("LT038",3),I("LT039",1),I("LT034",1));
            Add("CR-BS-05","Forge Dragonsteel Longsword",DowntimeProfession0836.Blacksmithing,5,120,CraftingOutputKind0837.Equipment,"EQ008","LT155",I("LT037",2),I("LT100",1),I("LT070",1));

            // Alchemy
            Add("CR-AL-01","Brew Healing Potion",DowntimeProfession0836.Alchemy,1,40,CraftingOutputKind0837.HealingPotion,"LT002",null,I("LT055",2),I("LT056",1));
            Add("CR-AL-02","Brew Antidote",DowntimeProfession0836.Alchemy,1,40,CraftingOutputKind0837.Loot,"LT005",null,I("LT056",1),I("LT059",1));
            Add("CR-AL-03","Brew Restorative Tonic",DowntimeProfession0836.Alchemy,2,60,CraftingOutputKind0837.Loot,"LT007",null,I("LT055",1),I("LT056",1),I("LT058",1));
            Add("CR-AL-04","Distill Smoke Bomb",DowntimeProfession0836.Alchemy,3,80,CraftingOutputKind0837.Loot,"LT015",null,I("LT034",1),I("LT048",1),I("LT060",1));
            Add("CR-AL-05","Distill Phoenix Ash",DowntimeProfession0836.Alchemy,5,120,CraftingOutputKind0837.Loot,"LT030",null,I("LT059",2),I("LT067",1),I("LT068",1));

            // Enchanting
            Add("CR-EN-01","Enchant Apprentice Focus",DowntimeProfession0836.Enchanting,1,40,CraftingOutputKind0837.Equipment,"EQ139",null,I("LT061",1),I("LT064",1),I("LT069",1));
            Add("CR-EN-02","Weave Runed Battlerobes",DowntimeProfession0836.Enchanting,2,60,CraftingOutputKind0837.Equipment,"EQ096",null,I("LT063",1),I("LT064",2),I("LT054",1));
            Add("CR-EN-03","Shape Arcane Lens",DowntimeProfession0836.Enchanting,3,80,CraftingOutputKind0837.Equipment,"EQ106",null,I("LT062",1),I("LT061",2),I("LT064",1));
            Add("CR-EN-04","Consecrate Blessed Chain",DowntimeProfession0836.Enchanting,4,100,CraftingOutputKind0837.Equipment,"EQ089",null,I("LT070",1),I("LT068",1),I("LT069",2));
            Add("CR-EN-05","Enchant Starfall Staff",DowntimeProfession0836.Enchanting,5,120,CraftingOutputKind0837.Equipment,"EQ120","LT158",I("LT066",1),I("LT062",2),I("LT067",1));

            // Artificing
            Add("CR-AR-01","Assemble Lockpick Set",DowntimeProfession0836.Artificing,1,40,CraftingOutputKind0837.Loot,"LT026",null,I("LT032",1),I("LT047",1));
            Add("CR-AR-02","Assemble Hunting Trap",DowntimeProfession0836.Artificing,1,40,CraftingOutputKind0837.Loot,"LT022",null,I("LT047",2),I("LT048",1),I("LT077",1));
            Add("CR-AR-03","Build Light Crossbow",DowntimeProfession0836.Artificing,2,60,CraftingOutputKind0837.Equipment,"EQ053",null,I("LT032",1),I("LT043",1),I("LT077",1));
            Add("CR-AR-04","Build Repeating Crossbow",DowntimeProfession0836.Artificing,4,100,CraftingOutputKind0837.Equipment,"EQ055",null,I("LT032",2),I("LT039",1),I("LT043",1),I("LT077",1));
            Add("CR-AR-05","Build Arcane Ballista",DowntimeProfession0836.Artificing,5,120,CraftingOutputKind0837.Equipment,"EQ058",null,I("LT032",2),I("LT062",1),I("LT063",1),I("LT065",1));
            return r;
        }
    }
}
