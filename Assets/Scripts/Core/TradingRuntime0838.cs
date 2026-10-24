using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    public sealed class HeroTransferStack0838
    {
        public string Id;
        public string Label;
        public int Count;
        public bool CraftingMaterial;
        public bool Consumable;
    }

    // 0.8.38 — explicit Hero-to-Hero ownership transfer during Stonebrook Downtime.
    // Gold remains party-owned and is intentionally absent from this system.
    public sealed partial class AscendanceGameManager
    {
        public bool CanTradeBetweenHeroes0838(string fromHeroId,string toHeroId)
        {
            if(!CanUseDowntimeActions0836)return false;
            if(string.IsNullOrWhiteSpace(fromHeroId)||string.IsNullOrWhiteSpace(toHeroId)||string.Equals(fromHeroId,toHeroId,StringComparison.OrdinalIgnoreCase))return false;
            return Heroes.TryGetValue(fromHeroId,out var from)&&from.Participating
                &&Heroes.TryGetValue(toHeroId,out var to)&&to.Participating;
        }

        public IReadOnlyList<HeroTransferStack0838> TradeableHeroLoot0838(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return Array.Empty<HeroTransferStack0838>();
            return h.LootInventory
                .Where(IsTradeableHeroLoot0838)
                .GroupBy(x=>x,StringComparer.OrdinalIgnoreCase)
                .Select(g=>
                {
                    RewardContent0826.Loot.TryGetValue(g.Key,out var d);
                    return new HeroTransferStack0838
                    {
                        Id=g.Key,
                        Label=d==null?g.Key:$"{d.Id} {d.Name}",
                        Count=g.Count(),
                        CraftingMaterial=d?.CraftingMaterial??false,
                        Consumable=d?.Consumable??false
                    };
                })
                .OrderByDescending(x=>x.CraftingMaterial)
                .ThenBy(x=>x.Label,StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<HeroTransferStack0838> TradeableHeroEquipment0838(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return Array.Empty<HeroTransferStack0838>();
            return h.AcquiredEquipment
                .Where(RewardContent0826.Equipment.ContainsKey)
                .GroupBy(x=>x,StringComparer.OrdinalIgnoreCase)
                .Select(g=>new HeroTransferStack0838{Id=g.Key,Label=RewardContent0826.EquipmentLabel(g.Key),Count=g.Count()})
                .OrderBy(x=>x.Label,StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool CanTransferHeroLoot0838(string fromHeroId,string toHeroId,string lootId,int count=1)
        {
            if(!CanTradeBetweenHeroes0838(fromHeroId,toHeroId)||count<1||!IsTradeableHeroLoot0838(lootId))return false;
            return Heroes[fromHeroId].LootInventory.Count(x=>string.Equals(x,lootId,StringComparison.OrdinalIgnoreCase))>=count;
        }

        public bool TransferHeroLoot0838(string fromHeroId,string toHeroId,string lootId,int count=1)
        {
            if(!CanTransferHeroLoot0838(fromHeroId,toHeroId,lootId,count))return false;
            var from=Heroes[fromHeroId];var to=Heroes[toHeroId];
            for(int i=0;i<count;i++)
            {
                int at=from.LootInventory.FindIndex(x=>string.Equals(x,lootId,StringComparison.OrdinalIgnoreCase));
                if(at<0)return false;
                from.LootInventory.RemoveAt(at);to.LootInventory.Add(lootId);
            }
            RewardContent0826.Loot.TryGetValue(lootId,out var d);
            string kind=d?.CraftingMaterial==true?"MATERIAL":d?.Consumable==true?"ITEM":"LOOT";
            AddLog($"DOWNTIME TRADE — {from.DisplayName} gives {count}× {RewardContent0826.LootLabel(lootId)} to {to.DisplayName}. {kind} ownership transferred; Party Gold unchanged: {PartyGold}.");
            StateChanged?.Invoke();return true;
        }

        public bool TransferAllHeroLoot0838(string fromHeroId,string toHeroId,string lootId)
        {
            if(!Heroes.TryGetValue(fromHeroId??string.Empty,out var from))return false;
            int count=from.LootInventory.Count(x=>string.Equals(x,lootId,StringComparison.OrdinalIgnoreCase));
            return count>0&&TransferHeroLoot0838(fromHeroId,toHeroId,lootId,count);
        }

        public bool CanTransferEquipment0838(string fromHeroId,string toHeroId,string equipmentId)
        {
            if(!CanTradeBetweenHeroes0838(fromHeroId,toHeroId)||!RewardContent0826.Equipment.ContainsKey(equipmentId??string.Empty))return false;
            return Heroes[fromHeroId].AcquiredEquipment.Any(x=>string.Equals(x,equipmentId,StringComparison.OrdinalIgnoreCase));
        }

        public bool TransferEquipment0838(string fromHeroId,string toHeroId,string equipmentId)
        {
            if(!CanTransferEquipment0838(fromHeroId,toHeroId,equipmentId))return false;
            var from=Heroes[fromHeroId];var to=Heroes[toHeroId];
            int at=from.AcquiredEquipment.FindIndex(x=>string.Equals(x,equipmentId,StringComparison.OrdinalIgnoreCase));
            if(at<0)return false;
            from.AcquiredEquipment.RemoveAt(at);to.AcquiredEquipment.Add(equipmentId);
            AddLog($"DOWNTIME TRADE — {from.DisplayName} gives unequipped {RewardContent0826.EquipmentLabel(equipmentId)} to {to.DisplayName}. Party Gold unchanged: {PartyGold}.");
            StateChanged?.Invoke();return true;
        }

        public bool CanTransferHealingPotion0838(string fromHeroId,string toHeroId)
        {
            return CanTradeBetweenHeroes0838(fromHeroId,toHeroId)&&Heroes[fromHeroId].HealingPotions>0;
        }

        public bool TransferHealingPotion0838(string fromHeroId,string toHeroId)
        {
            if(!CanTransferHealingPotion0838(fromHeroId,toHeroId))return false;
            var from=Heroes[fromHeroId];var to=Heroes[toHeroId];
            from.HealingPotions--;to.HealingPotions++;
            AddLog($"DOWNTIME TRADE — {from.DisplayName} gives 1 Healing Potion to {to.DisplayName}. Party Gold unchanged: {PartyGold}.");
            StateChanged?.Invoke();return true;
        }

        public string TradeSummary0838(string fromHeroId,string toHeroId)
        {
            if(!Heroes.TryGetValue(fromHeroId??string.Empty,out var from)||!from.Participating)return "Select a participating source Hero.";
            if(!Heroes.TryGetValue(toHeroId??string.Empty,out var to)||!to.Participating||string.Equals(fromHeroId,toHeroId,StringComparison.OrdinalIgnoreCase))return $"TRADING FROM {from.DisplayName.ToUpperInvariant()} — choose another participating Hero to receive items.";
            int materials=TradeableHeroLoot0838(fromHeroId).Where(x=>x.CraftingMaterial).Sum(x=>x.Count);
            int otherLoot=TradeableHeroLoot0838(fromHeroId).Where(x=>!x.CraftingMaterial).Sum(x=>x.Count);
            int gear=from.AcquiredEquipment.Count;
            return $"{from.DisplayName.ToUpperInvariant()}  →  {to.DisplayName.ToUpperInvariant()}\nTransferable: {materials} Materials • {otherLoot} Loot/Items • {gear} Unequipped Gear • {from.HealingPotions} Healing Potions\nGold is party-owned and is never transferred between Heroes.";
        }

        public bool IsTradeableHeroLoot0838(string lootId)
        {
            return RewardContent0826.Loot.TryGetValue(lootId??string.Empty,out var d)&&!d.QuestItem;
        }

        public bool DebugHasEquippedEquipment0838(string heroId,string equipmentId)
        {
            return Heroes.TryGetValue(heroId??string.Empty,out var h)&&h.StartingEquipment.Any(e=>string.Equals(e.Id,equipmentId,StringComparison.OrdinalIgnoreCase));
        }
    }
}
