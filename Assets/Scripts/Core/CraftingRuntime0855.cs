using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    public enum CraftingOutputKind0855 { Loot, Equipment, Quest, Campaign, Upgrade, Consumable, LegendaryTreasure, Special }

    [Serializable]
    public sealed class CraftingMaterialRequirement0855
    {
        public string Id;
        public string Name;
        public int Count;
        public bool Equipment;
    }

    [Serializable]
    public sealed class CraftingRecipeRuntime0855
    {
        public string Id;
        public string Name;
        public string AuthoredProfession;
        public DowntimeProfession0836 CoreProfession;
        public int RequiredLevel;
        public int ProfessionXp;
        public string Tier;
        public string RequiredTool;
        public int CraftActions;
        public int GoldCost;
        public string ResultId;
        public string ResultName;
        public CraftingOutputKind0855 OutputKind;
        public string UnlockRequirement;
        public string FlavorText;
        public List<CraftingMaterialRequirement0855> Materials=new();
    }

    /// <summary>
    /// 0.8.55 adapter over the full v10.52 200-row Crafting Recipe catalog.
    /// The four locked player progression tracks remain authoritative. Additional authored
    /// profession labels are preserved as specialties and mapped into one of those four tracks.
    /// </summary>
    public static class CraftingContent0855
    {
        private static List<CraftingRecipeRuntime0855> recipes;
        private static Dictionary<string,CraftingRecipeRuntime0855> byId;
        public static IReadOnlyList<CraftingRecipeRuntime0855> Recipes { get { Ensure(); return recipes; } }
        public static CraftingRecipeRuntime0855 Get(string id){Ensure();return byId.TryGetValue(id??string.Empty,out var r)?r:null;}
        public static IReadOnlyList<CraftingRecipeRuntime0855> ForProfession(DowntimeProfession0836 p){Ensure();return recipes.Where(r=>r.CoreProfession==p).OrderBy(r=>r.RequiredLevel).ThenBy(r=>r.Id).ToList();}

        private static void Ensure()
        {
            if(recipes!=null)return;
            FullContentCatalog0850.EnsureLoaded();
            recipes=new List<CraftingRecipeRuntime0855>();byId=new Dictionary<string,CraftingRecipeRuntime0855>(StringComparer.OrdinalIgnoreCase);
            foreach(var authored in FullContentCatalog0850.CraftingRecipes)
            {
                if(authored==null||string.IsNullOrWhiteSpace(authored.id))continue;
                var r=new CraftingRecipeRuntime0855
                {
                    Id=authored.id,Name=authored.name,AuthoredProfession=authored.profession,
                    CoreProfession=MapProfession(authored.profession),RequiredLevel=TierLevel(authored.tier),
                    ProfessionXp=TierXp(TierLevel(authored.tier)),Tier=authored.tier,RequiredTool=authored.requiredTool,
                    CraftActions=ParseActionCost(authored.craftTime),GoldCost=Math.Max(0,authored.goldCost),
                    ResultId=authored.resultId,ResultName=authored.result,OutputKind=OutputKind(authored.resultId),
                    UnlockRequirement=authored.unlockRequirement,FlavorText=authored.flavorText
                };
                foreach(var part in (authored.requiredMaterials??string.Empty).Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries))
                {
                    var m=Regex.Match(part,@"\b((?:LT|EQ)\d{3})\b.*?\bx\s*(\d+)\b",RegexOptions.IgnoreCase);
                    if(!m.Success)continue;
                    string id=m.Groups[1].Value.ToUpperInvariant();int count=Math.Max(1,int.TryParse(m.Groups[2].Value,out var n)?n:1);
                    string name=RewardContent0826.Loot.TryGetValue(id,out var loot)?loot.Name:RewardContent0826.Equipment.TryGetValue(id,out var gear)?gear.Name:id;
                    r.Materials.Add(new CraftingMaterialRequirement0855{Id=id,Name=name,Count=count,Equipment=id.StartsWith("EQ",StringComparison.OrdinalIgnoreCase)});
                }
                recipes.Add(r);byId[r.Id]=r;
            }
        }

        public static DowntimeProfession0836 MapProfession(string authored)
        {
            switch((authored??string.Empty).Trim().ToUpperInvariant())
            {
                case "ALCHEMY": return DowntimeProfession0836.Alchemy;
                case "ENCHANTING": case "SACRED CRAFTING": case "ARTIFACT RESTORATION": case "LEGENDARY CRAFTING": return DowntimeProfession0836.Enchanting;
                case "TINKERING": case "CAMPCRAFT": case "SETTLEMENT CRAFTING": return DowntimeProfession0836.Artificing;
                default: return DowntimeProfession0836.Blacksmithing; // Blacksmithing, Woodworking, Leatherworking, Tailoring, Jewelry, Trophy Crafting.
            }
        }

        private static int TierLevel(string tier)
        {
            switch((tier??string.Empty).Trim().ToUpperInvariant())
            { case "II":return 2;case "III":return 3;case "IV":return 4;case "V":return 5;default:return 1; }
        }
        private static int TierXp(int level)=>level<=1?40:level==2?60:level==3?80:level==4?100:120;
        private static int ParseActionCost(string text){var m=Regex.Match(text??string.Empty,@"\d+");return m.Success&&int.TryParse(m.Value,out var n)?Math.Max(1,n):1;}
        private static CraftingOutputKind0855 OutputKind(string id)
        {
            string x=(id??string.Empty).Trim();
            if(Regex.IsMatch(x,@"^EQ\d{3}$",RegexOptions.IgnoreCase))return CraftingOutputKind0855.Equipment;
            if(Regex.IsMatch(x,@"^LT\d{3}$",RegexOptions.IgnoreCase))return CraftingOutputKind0855.Loot;
            if(Regex.IsMatch(x,@"^Q\d{3}$",RegexOptions.IgnoreCase)||string.Equals(x,"Quest Item",StringComparison.OrdinalIgnoreCase))return CraftingOutputKind0855.Quest;
            if(string.Equals(x,"Campaign",StringComparison.OrdinalIgnoreCase))return CraftingOutputKind0855.Campaign;
            if(string.Equals(x,"Upgrade",StringComparison.OrdinalIgnoreCase))return CraftingOutputKind0855.Upgrade;
            if(string.Equals(x,"Consumable",StringComparison.OrdinalIgnoreCase))return CraftingOutputKind0855.Consumable;
            if(string.Equals(x,"Legendary Treasure",StringComparison.OrdinalIgnoreCase))return CraftingOutputKind0855.LegendaryTreasure;
            return CraftingOutputKind0855.Special;
        }
    }

    public sealed partial class AscendanceGameManager
    {
        private readonly List<string> craftingSpecialResults0855=new();
        private readonly HashSet<string> craftingUnlockFlags0855=new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> defeatedEnemyTemplateIds0855=new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<string> CraftingSpecialResults0855=>craftingSpecialResults0855;
        public IReadOnlyCollection<string> CraftingUnlockFlagsForSave0855=>craftingUnlockFlags0855;
        public IReadOnlyCollection<string> DefeatedEnemyTemplateIdsForSave0855=>defeatedEnemyTemplateIds0855;

        public IReadOnlyList<CraftingRecipeRuntime0855> CraftingRecipes0855(DowntimeProfession0836 profession)=>CraftingContent0855.ForProfession(profession);
        public CraftingRecipeRuntime0855 CraftingRecipe0855(string recipeId)=>CraftingContent0855.Get(recipeId);
        public int DowntimeProfessionMaterialCount0855(string heroId,DowntimeProfession0836 profession)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return 0;
            var ids=new HashSet<string>(CraftingContent0855.ForProfession(profession).SelectMany(r=>r.Materials).Select(m=>m.Id),StringComparer.OrdinalIgnoreCase);
            return h.LootInventory.Count(ids.Contains)+h.AcquiredEquipment.Count(ids.Contains);
        }
        public string DowntimeProfessionSummary0855(string heroId,DowntimeProfession0836 profession)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return "No Hero selected.";
            var recipes=CraftingContent0855.ForProfession(profession);var specialties=recipes.Select(r=>r.AuthoredProfession).Distinct().OrderBy(x=>x).ToList();
            int ready=recipes.Count(r=>CanCraftRecipe0855(r.Id,heroId));
            return $"{h.DisplayName.ToUpperInvariant()} • {profession.ToString().ToUpperInvariant()} • {ProfessionProgress0837(h.Id,profession)} • {recipes.Count} RECIPES • {ready} READY\nSPECIALTIES — {string.Join(", ",specialties)}";
        }

        public bool IsCraftingRecipeExecutable0855(string recipeId)
        {
            var r=CraftingContent0855.Get(recipeId);if(r==null||r.Materials.Count==0||r.RequiredLevel<1||r.RequiredLevel>5)return false;
            if(r.Materials.Any(m=>string.IsNullOrWhiteSpace(m.Id)||m.Count<1))return false;
            return !string.IsNullOrWhiteSpace(r.ResultId)&&Enum.IsDefined(typeof(CraftingOutputKind0855),r.OutputKind);
        }

        public int CraftingMaterialCount0855(string heroId,string id)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h))return 0;
            if((id??string.Empty).StartsWith("EQ",StringComparison.OrdinalIgnoreCase))return h.AcquiredEquipment.Count(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));
            return h.LootInventory.Count(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));
        }

        public bool CanCraftRecipe0855(string recipeId,string heroId)
        {
            if(!CanUseDowntimeActions0836||!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return false;
            var r=CraftingContent0855.Get(recipeId);if(r==null)return false;
            if(ProfessionLevel0837(heroId,r.CoreProfession)<r.RequiredLevel)return false;
            if(PartyGold<r.GoldCost)return false;
            if(!CraftingToolAvailable0855(r,heroId))return false;
            if(!CraftingUnlockSatisfied0855(r,heroId))return false;
            foreach(var m in r.Materials)if(CraftingMaterialCount0855(heroId,m.Id)<m.Count)return false;
            return true;
        }

        public string CraftingRecipeStatus0855(string recipeId,string heroId)
        {
            var r=CraftingContent0855.Get(recipeId);if(r==null)return "Unknown recipe.";
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return "Select a participating Hero.";
            int level=ProfessionLevel0837(heroId,r.CoreProfession);
            if(level<r.RequiredLevel)return $"LOCKED — {r.CoreProfession} Level {r.RequiredLevel} required.";
            if(!CraftingToolAvailable0855(r,heroId))return $"LOCKED — requires {r.RequiredTool}.";
            if(!CraftingUnlockSatisfied0855(r,heroId))return $"LOCKED — {r.UnlockRequirement}.";
            if(PartyGold<r.GoldCost)return $"MISSING — {r.GoldCost-PartyGold} more Party Gold.";
            var missing=r.Materials.Where(m=>CraftingMaterialCount0855(heroId,m.Id)<m.Count).ToList();
            if(missing.Count>0)return "MISSING — "+string.Join(", ",missing.Select(m=>$"{m.Count}× {m.Name}"));
            return "READY";
        }

        public string CraftingRecipeLabel0855(CraftingRecipeRuntime0855 r,string heroId)
        {
            if(r==null)return string.Empty;
            string mats=string.Join(" + ",r.Materials.Select(m=>$"{m.Count}× {m.Name}"));
            string specialty=string.Equals(r.AuthoredProfession,r.CoreProfession.ToString(),StringComparison.OrdinalIgnoreCase)?string.Empty:$" • {r.AuthoredProfession}";
            return $"{r.Id} • {r.Name} • TIER {r.Tier}{specialty}\n{mats} • {r.GoldCost} GOLD • {r.RequiredTool} • {r.CraftActions}A\n{CraftingRecipeStatus0855(r.Id,heroId)}";
        }

        public bool CraftRecipe0855(string recipeId,string heroId)
        {
            if(!CanCraftRecipe0855(recipeId,heroId)||!Heroes.TryGetValue(heroId,out var h))return false;
            var r=CraftingContent0855.Get(recipeId);if(r==null)return false;
            foreach(var m in r.Materials)for(int n=0;n<m.Count;n++)if(!ConsumeCraftingMaterial0855(h,m.Id))return false;
            PartyGold=Mathf.Max(0,PartyGold-r.GoldCost);
            string output=GrantCraftingOutput0855(h,r);
            AddProfessionXp0837(h,r.CoreProfession,r.ProfessionXp);
            AddLog($"CRAFTING 0.8.55 — {h.DisplayName} crafts {output} via {r.AuthoredProfession} ({r.CoreProfession} track). -{r.GoldCost} Gold. +{r.ProfessionXp} {r.CoreProfession} XP ({ProfessionProgress0837(h.Id,r.CoreProfession)}). Authored time: {r.CraftActions} Action(s). Party Gold: {PartyGold}.");
            StateChanged?.Invoke();return true;
        }

        private bool ConsumeCraftingMaterial0855(HeroState h,string id)
        {
            if((id??string.Empty).StartsWith("EQ",StringComparison.OrdinalIgnoreCase))
            { int at=h.AcquiredEquipment.FindIndex(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));if(at<0)return false;h.AcquiredEquipment.RemoveAt(at);return true; }
            int lootAt=h.LootInventory.FindIndex(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));if(lootAt<0)return false;h.LootInventory.RemoveAt(lootAt);return true;
        }

        private string GrantCraftingOutput0855(HeroState h,CraftingRecipeRuntime0855 r)
        {
            if(r.OutputKind==CraftingOutputKind0855.Equipment)
            {
                h.AcquiredEquipment.Add(r.ResultId);int at=EquipmentDeck.FindIndex(x=>string.Equals(x,r.ResultId,StringComparison.OrdinalIgnoreCase));if(at>=0)EquipmentDeck.RemoveAt(at);
                return RewardContent0826.EquipmentLabel(r.ResultId);
            }
            if(r.OutputKind==CraftingOutputKind0855.Loot)
            {
                h.LootInventory.Add(r.ResultId);OnLootAcquired0854(h,r.ResultId,$"crafted by {r.Id}");return RewardContent0826.LootLabel(r.ResultId);
            }
            string token=$"{r.Id}|{r.ResultId}|{r.ResultName}|{h.Id}";craftingSpecialResults0855.Add(token);
            return string.IsNullOrWhiteSpace(r.ResultName)?r.ResultId:r.ResultName;
        }

        private bool CraftingToolAvailable0855(CraftingRecipeRuntime0855 r,string heroId)
        {
            string tool=(r.RequiredTool??string.Empty).Trim();if(string.IsNullOrEmpty(tool))return true;
            if(craftingUnlockFlags0855.Contains("TOOL:"+tool))return true;
            if(tool.Equals("Forge",StringComparison.OrdinalIgnoreCase)||tool.Equals("Workbench",StringComparison.OrdinalIgnoreCase)||tool.Equals("Alchemy Kit",StringComparison.OrdinalIgnoreCase)||tool.Equals("Enchanting Table",StringComparison.OrdinalIgnoreCase)||tool.Equals("Bowyer Bench",StringComparison.OrdinalIgnoreCase)||tool.Equals("Camp Kit",StringComparison.OrdinalIgnoreCase)||tool.Equals("Loom",StringComparison.OrdinalIgnoreCase)||tool.Equals("Jeweler's Bench",StringComparison.OrdinalIgnoreCase)||tool.Equals("Stonebrook",StringComparison.OrdinalIgnoreCase))return true;
            int level=ProfessionLevel0837(heroId,r.CoreProfession);
            if(tool.Equals("Master Forge",StringComparison.OrdinalIgnoreCase)||tool.Equals("Master Workbench",StringComparison.OrdinalIgnoreCase))return level>=3;
            if(tool.StartsWith("Sacred",StringComparison.OrdinalIgnoreCase))return level>=3;
            if(tool.StartsWith("Arcane",StringComparison.OrdinalIgnoreCase)||tool.Equals("Enchanter's Loom",StringComparison.OrdinalIgnoreCase))return level>=4;
            if(tool.Equals("Legendary Forge",StringComparison.OrdinalIgnoreCase))return level>=5;
            if(tool.Equals("Fallen Watchtower",StringComparison.OrdinalIgnoreCase))return CompletedQuest0855("Q003")||LocationRevealed0855("L017");
            if(tool.Equals("Merchant Caravan",StringComparison.OrdinalIgnoreCase))return string.Equals(CurrentQuestId0829,"Q002",StringComparison.OrdinalIgnoreCase)||CompletedQuest0855("Q002")||LocationRevealed0855("L047");
            if(tool.Equals("Ancient Portal",StringComparison.OrdinalIgnoreCase))return LocationRevealed0855("L049");
            if(tool.Equals("Ancient Crypt",StringComparison.OrdinalIgnoreCase))return LocationRevealed0855("L032");
            return false;
        }

        private bool CraftingUnlockSatisfied0855(CraftingRecipeRuntime0855 r,string heroId)
        {
            string req=(r.UnlockRequirement??string.Empty).Trim();if(string.IsNullOrEmpty(req)||req.Equals("None",StringComparison.OrdinalIgnoreCase))return true;
            if(craftingUnlockFlags0855.Contains(req))return true;
            if(req.StartsWith("Crafting Tier",StringComparison.OrdinalIgnoreCase))return ProfessionLevel0837(heroId,r.CoreProfession)>=r.RequiredLevel;
            if(req.IndexOf("or Stonebrook upgrade phase",StringComparison.OrdinalIgnoreCase)>=0&&IsDowntimeAvailable0836)return true;
            if(req.IndexOf("Complete Q003 or restore Fallen Watchtower",StringComparison.OrdinalIgnoreCase)>=0)return CompletedQuest0855("Q003")||LocationRevealed0855("L017");
            if(req.IndexOf("Access to Sacred",StringComparison.OrdinalIgnoreCase)>=0)return ProfessionLevel0837(heroId,r.CoreProfession)>=3||craftingUnlockFlags0855.Contains(req);

            var ids=Regex.Matches(req,@"\bLT\d{3}\b",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value.ToUpperInvariant()).Distinct().ToList();
            if(ids.Count>0&&!ids.All(id=>HeroCarries0855(heroId,id)))return false;

            foreach(Match q in Regex.Matches(req,@"\bQ\d{3}\b",RegexOptions.IgnoreCase))
            {
                string id=q.Value.ToUpperInvariant();
                if(req.IndexOf("active or completed",StringComparison.OrdinalIgnoreCase)>=0)
                { if(!string.Equals(CurrentQuestId0829,id,StringComparison.OrdinalIgnoreCase)&&!CompletedQuest0855(id))return false; }
                else if(req.IndexOf("active",StringComparison.OrdinalIgnoreCase)>=0)
                { if(!string.Equals(CurrentQuestId0829,id,StringComparison.OrdinalIgnoreCase))return false; }
                else if(req.IndexOf("Complete",StringComparison.OrdinalIgnoreCase)>=0)
                { if(!CompletedQuest0855(id))return false; }
            }

            if(req.IndexOf("Stonebrook upgrade phase",StringComparison.OrdinalIgnoreCase)>=0&&IsDowntimeAvailable0836)return true;
            if(req.IndexOf("restore Fallen Watchtower",StringComparison.OrdinalIgnoreCase)>=0&&LocationRevealed0855("L017"))return true;
            if(req.IndexOf("Goblin War Camp revealed",StringComparison.OrdinalIgnoreCase)>=0)return LocationRevealed0855("L019");
            if(req.IndexOf("Goblin Mines revealed",StringComparison.OrdinalIgnoreCase)>=0)return LocationRevealed0855("L031");
            if(req.IndexOf("Ritual Circle revealed",StringComparison.OrdinalIgnoreCase)>=0)return LocationRevealed0855("L024");
            if(req.IndexOf("Ancient Portal revealed",StringComparison.OrdinalIgnoreCase)>=0)return LocationRevealed0855("L049");
            if(req.IndexOf("Ancient Crypt revealed",StringComparison.OrdinalIgnoreCase)>=0)return LocationRevealed0855("L032");
            if(req.IndexOf("Complete Campaign finale",StringComparison.OrdinalIgnoreCase)>=0||req.IndexOf("Complete Goblin Uprising",StringComparison.OrdinalIgnoreCase)>=0||req.IndexOf("Complete all Main Quests",StringComparison.OrdinalIgnoreCase)>=0)return CampaignCompleted0833;
            if(req.IndexOf("Complete Q001",StringComparison.OrdinalIgnoreCase)>=0)return CompletedQuest0855("Q001");

            if(req.IndexOf("Defeat a Goblin Captain",StringComparison.OrdinalIgnoreCase)>=0)return defeatedEnemyTemplateIds0855.Contains("E006")||CompletedQuest0855("Q001")||CompletedQuest0855("Q006");
            if(req.IndexOf("Defeat a Goblin War Chief",StringComparison.OrdinalIgnoreCase)>=0||req.IndexOf("Defeat the Goblin War Chief",StringComparison.OrdinalIgnoreCase)>=0)return defeatedEnemyTemplateIds0855.Contains("E010")||CompletedQuest0855("Q004")||CompletedQuest0855("Q005");
            if(req.IndexOf("Defeat a Goblin Shaman",StringComparison.OrdinalIgnoreCase)>=0)return defeatedEnemyTemplateIds0855.Contains("E005");
            if(req.IndexOf("Defeat a Warg",StringComparison.OrdinalIgnoreCase)>=0)return defeatedEnemyTemplateIds0855.Contains("E029");
            if(req.IndexOf("Defeat a Spider",StringComparison.OrdinalIgnoreCase)>=0)return defeatedEnemyTemplateIds0855.Contains("E022")||defeatedEnemyTemplateIds0855.Contains("E023");
            if(req.IndexOf("Defeat an Owlbear",StringComparison.OrdinalIgnoreCase)>=0)return defeatedEnemyTemplateIds0855.Contains("E028");
            if(req.IndexOf("Defeat a Cave Bear",StringComparison.OrdinalIgnoreCase)>=0)return defeatedEnemyTemplateIds0855.Contains("E025");
            if(req.IndexOf("Defeat at least 3 Goblins",StringComparison.OrdinalIgnoreCase)>=0&&Heroes.TryGetValue(heroId,out var hero)&&hero.KillCount>=3)return true;

            // Requirements with an explicit LT possession clause are satisfied once their carried IDs pass,
            // unless they also state a separate Campaign permission that future campaign runtimes must grant.
            if(ids.Count>0&&req.IndexOf("Campaign permits",StringComparison.OrdinalIgnoreCase)<0)return true;
            return craftingUnlockFlags0855.Contains(req);
        }

        private bool HeroCarries0855(string heroId,string id)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&h.LootInventory.Any(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));
        private bool CompletedQuest0855(string id)=>CampaignProgress0833?.CompletedQuestIds?.Contains(id,StringComparer.OrdinalIgnoreCase)??false;
        private bool LocationRevealed0855(string id)=>Locations.TryGetValue(id??string.Empty,out var l)&&l.Revealed;

        public void RecordEnemyDefeat0855(EnemyState enemy)
        {
            string id=enemy?.TemplateId;if(string.IsNullOrWhiteSpace(id)&&enemy!=null&&Regex.IsMatch(enemy.Id??string.Empty,@"^E\d{3}$",RegexOptions.IgnoreCase))id=enemy.Id;
            if(!string.IsNullOrWhiteSpace(id))defeatedEnemyTemplateIds0855.Add(id.ToUpperInvariant());
        }

        public bool GrantCraftingUnlock0855(string requirementOrTool)
        { if(string.IsNullOrWhiteSpace(requirementOrTool))return false;bool added=craftingUnlockFlags0855.Add(requirementOrTool.Trim());if(added)StateChanged?.Invoke();return added; }

        public void RestoreCraftingState0855(AscendanceSaveData save)
        {
            craftingSpecialResults0855.Clear();craftingUnlockFlags0855.Clear();defeatedEnemyTemplateIds0855.Clear();
            if(save?.CraftingSpecialResults0855!=null)craftingSpecialResults0855.AddRange(save.CraftingSpecialResults0855.Where(x=>!string.IsNullOrWhiteSpace(x)));
            if(save?.CraftingUnlockFlags0855!=null)foreach(var x in save.CraftingUnlockFlags0855)if(!string.IsNullOrWhiteSpace(x))craftingUnlockFlags0855.Add(x);
            if(save?.DefeatedEnemyTemplateIds0855!=null)foreach(var x in save.DefeatedEnemyTemplateIds0855)if(!string.IsNullOrWhiteSpace(x))defeatedEnemyTemplateIds0855.Add(x);
        }

        // Deterministic validator helpers.
        public bool DebugGrantCraftingUnlock0855(string key)=>GrantCraftingUnlock0855(key);
        public void DebugRecordEnemyTemplateDefeat0855(string id){if(!string.IsNullOrWhiteSpace(id))defeatedEnemyTemplateIds0855.Add(id.ToUpperInvariant());}
        public bool DebugGiveCraftingInput0855(string heroId,string id,int count=1)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||count<1)return false;
            if((id??string.Empty).StartsWith("EQ",StringComparison.OrdinalIgnoreCase)){for(int i=0;i<count;i++)h.AcquiredEquipment.Add(id);return true;}
            if(!RewardContent0826.Loot.ContainsKey(id??string.Empty))return false;for(int i=0;i<count;i++)h.LootInventory.Add(id);return true;
        }
        public void DebugSetPartyGold0855(int value){PartyGold=Mathf.Max(0,value);}
    }
}
