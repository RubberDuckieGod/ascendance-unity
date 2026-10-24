using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public sealed partial class AscendanceGameManager
    {
        private static readonly int[] ProfessionLevelThresholds0837={0,100,250,450,700};

        public int ProfessionXp0837(string heroId,DowntimeProfession0836 profession)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h))return 0;
            return profession switch
            {
                DowntimeProfession0836.Blacksmithing=>Mathf.Max(0,h.BlacksmithingXp0837),
                DowntimeProfession0836.Alchemy=>Mathf.Max(0,h.AlchemyXp0837),
                DowntimeProfession0836.Enchanting=>Mathf.Max(0,h.EnchantingXp0837),
                DowntimeProfession0836.Artificing=>Mathf.Max(0,h.ArtificingXp0837),
                _=>0
            };
        }

        public int ProfessionLevel0837(string heroId,DowntimeProfession0836 profession)
        {
            int xp=ProfessionXp0837(heroId,profession),level=1;
            for(int i=1;i<ProfessionLevelThresholds0837.Length;i++)if(xp>=ProfessionLevelThresholds0837[i])level=i+1;
            return Mathf.Clamp(level,1,5);
        }

        public string ProfessionProgress0837(string heroId,DowntimeProfession0836 profession)
        {
            int xp=ProfessionXp0837(heroId,profession),level=ProfessionLevel0837(heroId,profession);
            if(level>=5)return $"LEVEL 5 • {xp} XP • MASTER";
            return $"LEVEL {level} • {xp}/{ProfessionLevelThresholds0837[level]} XP";
        }

        public IReadOnlyList<CraftingRecipe0837> CraftingRecipes0837(DowntimeProfession0836 profession)=>CraftingContent0837.ForProfession(profession);

        public int HeroMaterialCount0837(string heroId,string lootId)
        {
            return Heroes.TryGetValue(heroId??string.Empty,out var h)?h.LootInventory.Count(x=>string.Equals(x,lootId,StringComparison.OrdinalIgnoreCase)):0;
        }

        public int DowntimeProfessionMaterialCount0837(string heroId,DowntimeProfession0836 profession)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return 0;
            return h.LootInventory.Count(id=>IsProfessionMaterial0836(id,profession));
        }

        public string DowntimeProfessionSummary0837(string heroId,DowntimeProfession0836 profession)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return "No Hero selected.";
            var groups=h.LootInventory.Where(id=>IsProfessionMaterial0836(id,profession)).GroupBy(id=>id,StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g=>g.Count()).ThenBy(g=>RewardContent0826.Loot.TryGetValue(g.Key,out var d)?d.Name:g.Key).Take(10).ToList();
            string materials=groups.Count==0?"No matching materials carried by this Hero.":string.Join("  •  ",groups.Select(g=>$"{g.Count()}× {(RewardContent0826.Loot.TryGetValue(g.Key,out var d)?d.Name:g.Key)}"));
            return $"{h.DisplayName.ToUpperInvariant()} • {profession.ToString().ToUpperInvariant()} • {ProfessionProgress0837(h.Id,profession)}\n{materials}";
        }

        public bool CanCraftRecipe0837(string recipeId,string heroId)
        {
            if(!CanUseDowntimeActions0836||!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return false;
            var r=CraftingContent0837.Get(recipeId);if(r==null)return false;
            if(ProfessionLevel0837(heroId,r.Profession)<r.RequiredLevel)return false;
            if(!string.IsNullOrWhiteSpace(r.RequiredUnlockLootId)&&!h.LootInventory.Any(x=>string.Equals(x,r.RequiredUnlockLootId,StringComparison.OrdinalIgnoreCase)))return false;
            foreach(var i in r.Ingredients)if(HeroMaterialCount0837(heroId,i.LootId)<i.Count)return false;
            return true;
        }

        public string CraftingRecipeStatus0837(string recipeId,string heroId)
        {
            var r=CraftingContent0837.Get(recipeId);if(r==null)return "Unknown recipe.";
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return "Select a participating Hero.";
            if(ProfessionLevel0837(heroId,r.Profession)<r.RequiredLevel)return $"LOCKED — requires {r.Profession} Level {r.RequiredLevel}.";
            if(!string.IsNullOrWhiteSpace(r.RequiredUnlockLootId)&&!h.LootInventory.Any(x=>string.Equals(x,r.RequiredUnlockLootId,StringComparison.OrdinalIgnoreCase)))return $"LOCKED — {RewardContent0826.LootLabel(r.RequiredUnlockLootId)} must be carried by {h.DisplayName}.";
            var missing=r.Ingredients.Where(i=>HeroMaterialCount0837(heroId,i.LootId)<i.Count).ToList();
            if(missing.Count>0)return "MISSING — "+string.Join(", ",missing.Select(i=>$"{i.Count}× {RewardContent0826.LootLabel(i.LootId)}"));
            return "READY";
        }

        public string CraftingRecipeLabel0837(CraftingRecipe0837 r,string heroId)
        {
            if(r==null)return string.Empty;
            string cost=string.Join(" + ",r.Ingredients.Select(i=>$"{i.Count}× {(RewardContent0826.Loot.TryGetValue(i.LootId,out var d)?d.Name:i.LootId)}"));
            string unlock=string.IsNullOrWhiteSpace(r.RequiredUnlockLootId)?string.Empty:$" • REQUIRES {(RewardContent0826.Loot.TryGetValue(r.RequiredUnlockLootId,out var u)?u.Name:r.RequiredUnlockLootId)}";
            return $"{r.Name}  •  Lv {r.RequiredLevel}  •  +{r.ProfessionXp} XP\n{cost}{unlock}\n{CraftingRecipeStatus0837(r.Id,heroId)}";
        }

        public bool CraftRecipe0837(string recipeId,string heroId)
        {
            if(!CanCraftRecipe0837(recipeId,heroId)||!Heroes.TryGetValue(heroId,out var h))return false;
            var r=CraftingContent0837.Get(recipeId);if(r==null)return false;
            foreach(var i in r.Ingredients)for(int n=0;n<i.Count;n++)RemoveOneHeroLoot0837(h,i.LootId);
            string outputLabel;
            switch(r.OutputKind)
            {
                case CraftingOutputKind0837.Equipment:
                    h.AcquiredEquipment.Add(r.OutputId);
                    int at=EquipmentDeck.FindIndex(x=>string.Equals(x,r.OutputId,StringComparison.OrdinalIgnoreCase));
                    if(at>=0)EquipmentDeck.RemoveAt(at);
                    outputLabel=RewardContent0826.EquipmentLabel(r.OutputId);
                    break;
                case CraftingOutputKind0837.HealingPotion:
                    h.HealingPotions+=Mathf.Max(1,r.OutputCount);
                    outputLabel=$"{Mathf.Max(1,r.OutputCount)}× Healing Potion";
                    break;
                default:
                    for(int n=0;n<Mathf.Max(1,r.OutputCount);n++)h.LootInventory.Add(r.OutputId);
                    outputLabel=RewardContent0826.LootLabel(r.OutputId);
                    break;
            }
            AddProfessionXp0837(h,r.Profession,r.ProfessionXp);
            AddLog($"CRAFTING — {h.DisplayName} crafts {outputLabel} with personal materials. {r.Profession} +{r.ProfessionXp} XP ({ProfessionProgress0837(h.Id,r.Profession)}). Party Gold unchanged: {PartyGold}.");
            StateChanged?.Invoke();
            return true;
        }

        private static bool RemoveOneHeroLoot0837(HeroState hero,string lootId)
        {
            int at=hero.LootInventory.FindIndex(x=>string.Equals(x,lootId,StringComparison.OrdinalIgnoreCase));
            if(at<0)return false;hero.LootInventory.RemoveAt(at);return true;
        }

        private void AddProfessionXp0837(HeroState h,DowntimeProfession0836 profession,int delta)
        {
            delta=Mathf.Max(0,delta);
            switch(profession)
            {
                case DowntimeProfession0836.Blacksmithing:h.BlacksmithingXp0837+=delta;break;
                case DowntimeProfession0836.Alchemy:h.AlchemyXp0837+=delta;break;
                case DowntimeProfession0836.Enchanting:h.EnchantingXp0837+=delta;break;
                case DowntimeProfession0836.Artificing:h.ArtificingXp0837+=delta;break;
            }
        }

        // Validator-only deterministic progression setup; does not affect normal gameplay.
        public void DebugSetProfessionXp0837(string heroId,DowntimeProfession0836 profession,int xp)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h))return;
            xp=Mathf.Max(0,xp);
            switch(profession)
            {
                case DowntimeProfession0836.Blacksmithing:h.BlacksmithingXp0837=xp;break;
                case DowntimeProfession0836.Alchemy:h.AlchemyXp0837=xp;break;
                case DowntimeProfession0836.Enchanting:h.EnchantingXp0837=xp;break;
                case DowntimeProfession0836.Artificing:h.ArtificingXp0837=xp;break;
            }
        }
    }
}
