using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public enum DowntimeProfession0836
    {
        Blacksmithing,
        Alchemy,
        Enchanting,
        Artificing
    }

    // 0.8.36 — Stonebrook between-Quest state. This layer deliberately uses the live
    // campaign inventory/resource state instead of maintaining a parallel downtime economy.
    public sealed partial class AscendanceGameManager
    {
        public bool IsDowntimeAvailable0836 =>
            (QuestComplete || QuestFailed)
            && !CampaignCompleted0833
            && !CampaignDefeated0833
            && !CampaignLaunchpadAvailable0845;

        public bool CanUseDowntimeActions0836 =>
            IsDowntimeAvailable0836
            && !IsResolving
            && !HasPendingReactionWindow
            && !HasPendingEncounterDeckControl
            && !HasPendingRewardChoice
            && !ParticipatingHeroes.Any(h=>h.Moving);

        public bool StonebrookEquipmentPurchaseLocked0836 => CampaignProgress0833?.StonebrookEquipmentPurchaseLocked ?? false;

        public string DowntimeResourceSummary0836 =>
            $"{PartyGold} GOLD  •  REP {GeneralReputation:+#;-#;0}  •  MORALE {PartyMorale0831}  •  SUPPLY {NextQuestSupply0829}";

        public IReadOnlyList<string> DowntimeMerchantStock0836 => EquipmentDeck.Where(id=>RewardContent0826.Equipment.TryGetValue(id,out var d)&&d.Value>0).Take(3).ToList();

        public string DowntimeMerchantStatus0836
        {
            get
            {
                if(!IsDowntimeAvailable0836)return "Merchant unavailable while a Quest is active.";
                if(StonebrookEquipmentPurchaseLocked0836)return $"{CurrentCampaignHub0845.ToUpperInvariant()} EQUIPMENT PURCHASES LOCKED — Campaign consequences prevent Equipment purchases.";
                if(EquipmentDeck.Count==0)return $"The {CurrentCampaignHub0845} Equipment stock is empty.";
                return $"{CurrentCampaignHub0845} merchant stock is drawn from the live Equipment Deck. Purchased cards leave the deck and enter the selected Hero's inventory.";
            }
        }

        public string DowntimeHeroSummary0836(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return string.Empty;
            string state=h.Dead?"DEAD":h.Downed?"DOWNED":h.Health<h.MaxHealth?"INJURED":"READY";
            string equipped=DebugEquippedLoadout(h.Id);
            return $"{h.DisplayName}  •  Lv {h.Level}  •  {h.ClassName}\nHP {h.Health}/{h.MaxHealth}  •  {state}  •  Potion {h.HealingPotions}  •  Kills {h.KillCount}\nAttack {h.Attack}  •  Armor {h.Armor}  •  XP {h.XP}\nEquipped: {equipped}\nLoot {h.LootInventory.Count}  •  Inventory Gear {h.AcquiredEquipment.Count}";
        }

        public string DowntimeInventorySummary0836(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return "No Hero selected.";
            string loot=h.LootInventory.Count==0?"None":string.Join("\n",h.LootInventory.GroupBy(x=>x,StringComparer.OrdinalIgnoreCase).Select(g=>$"{g.Count()}× {RewardContent0826.LootLabel(g.Key)}"));
            string gear=h.AcquiredEquipment.Count==0?"None":string.Join("\n",h.AcquiredEquipment.Select(RewardContent0826.EquipmentLabel));
            return $"LOOT & MATERIALS\n{loot}\n\nUNEQUIPPED GEAR\n{gear}";
        }

        public bool CanRecoverHero0836(string heroId)
        {
            if(!CanUseDowntimeActions0836||!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return false;
            return h.Health<h.MaxHealth||h.Downed||h.Dead||h.HealingPotions<1||h.Statuses.Any(s=>!s.Beneficial);
        }

        public bool RecoverHero0836(string heroId)
        {
            if(!CanRecoverHero0836(heroId)||!Heroes.TryGetValue(heroId,out var h))return false;
            int beforeHp=h.Health;
            int removed=h.Statuses.RemoveAll(s=>!s.Beneficial);
            h.Health=h.MaxHealth;
            h.Downed=false;
            h.Dead=false;
            h.Actions=0;
            h.AttackOnlyActions=0;
            h.HealingPotions=Mathf.Max(1,h.HealingPotions);
            AddLog($"DOWNTIME RECOVERY — {h.DisplayName} recovers {Mathf.Max(0,h.MaxHealth-beforeHp)} Health, clears {removed} negative Condition{(removed==1?string.Empty:"s")}, and is ready for the next Quest. Healing Potion minimum restored to 1.");
            StateChanged?.Invoke();
            return true;
        }

        public int RecoverAllHeroes0836()
        {
            if(!CanUseDowntimeActions0836)return 0;
            int count=0;
            foreach(var h in ParticipatingHeroes.ToList())if(RecoverHero0836(h.Id))count++;
            if(count==0)AddLog("DOWNTIME RECOVERY — all participating Heroes are already ready for the next Quest.");
            return count;
        }

        public int MerchantPrice0836(string equipmentId)
        {
            return RewardContent0826.Equipment.TryGetValue(equipmentId??string.Empty,out var d)?Mathf.Max(0,d.Value):0;
        }

        public bool CanPurchaseMerchantEquipment0836(string equipmentId,string heroId)
        {
            if(!CanUseDowntimeActions0836||StonebrookEquipmentPurchaseLocked0836)return false;
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return false;
            if(!RewardContent0826.Equipment.ContainsKey(equipmentId??string.Empty))return false;
            if(!DowntimeMerchantStock0836.Contains(equipmentId,StringComparer.OrdinalIgnoreCase))return false;
            return PartyGold>=MerchantPrice0836(equipmentId);
        }

        public bool PurchaseMerchantEquipment0836(string equipmentId,string heroId)
        {
            if(!CanPurchaseMerchantEquipment0836(equipmentId,heroId))return false;
            if(!Heroes.TryGetValue(heroId,out var h)||!RewardContent0826.Equipment.TryGetValue(equipmentId,out var d))return false;
            int at=EquipmentDeck.FindIndex(x=>string.Equals(x,equipmentId,StringComparison.OrdinalIgnoreCase));
            if(at<0)return false;
            int price=MerchantPrice0836(equipmentId);
            EquipmentDeck.RemoveAt(at);
            PartyGold=Mathf.Max(0,PartyGold-price);
            h.AcquiredEquipment.Add(equipmentId);
            AddLog($"{CurrentCampaignHub0845.ToUpperInvariant()} MERCHANT — {h.DisplayName} purchases {RewardContent0826.EquipmentLabel(equipmentId)} for {price} Gold. Party Gold: {PartyGold}.");
            StateChanged?.Invoke();
            return true;
        }

        public int DowntimeProfessionMaterialCount0836(DowntimeProfession0836 profession)
        {
            var h=SelectedHero??ParticipatingHeroes.FirstOrDefault();
            return h==null?0:h.LootInventory.Count(id=>IsProfessionMaterial0836(id,profession));
        }

        public string DowntimeProfessionSummary0836(DowntimeProfession0836 profession)
        {
            var h=SelectedHero??ParticipatingHeroes.FirstOrDefault();
            if(h==null)return "No Hero selected.";
            var groups=h.LootInventory.Where(id=>IsProfessionMaterial0836(id,profession))
                .GroupBy(id=>id,StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g=>g.Count()).ThenBy(g=>RewardContent0826.Loot.TryGetValue(g.Key,out var d)?d.Name:g.Key)
                .Take(8).ToList();
            string materials=groups.Count==0?"No matching materials carried by this Hero.":string.Join("\n",groups.Select(g=>$"{g.Count()}× {RewardContent0826.LootLabel(g.Key)}"));
            return $"{h.DisplayName.ToUpperInvariant()} • {profession.ToString().ToUpperInvariant()}  •  {groups.Sum(g=>g.Count())} MATERIALS\n{ProfessionHookText0836(profession)}\n\n{materials}";
        }

        public string DowntimeCraftingStatus0836 =>
            "FULL CRAFTING CR001–CR200 — each Hero uses only their carried materials/unequipped gear; Party Gold is shared. Authored craft-time Actions are shown as recipe metadata; Crafting and Trading remain unlimited Downtime actions.";

        public bool OpenEquipmentManagementFromDowntime0836(string heroId)
        {
            if(!CanUseDowntimeActions0836||!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return false;
            SelectHero(heroId);
            BeginEquipmentManagement();
            return HasPendingRewardChoice&&string.Equals(PendingRewardChoiceMode,"ManageEquipment",StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProfessionMaterial0836(string lootId,DowntimeProfession0836 profession)
        {
            if(!RewardContent0826.Loot.TryGetValue(lootId??string.Empty,out var d))return false;
            if(!d.CraftingMaterial&&string.IsNullOrWhiteSpace(d.UsedBy))return false;
            string used=d.UsedBy??string.Empty;
            return profession switch
            {
                DowntimeProfession0836.Blacksmithing=>used.IndexOf("Blacksmith",StringComparison.OrdinalIgnoreCase)>=0,
                DowntimeProfession0836.Alchemy=>used.IndexOf("Alchemy",StringComparison.OrdinalIgnoreCase)>=0,
                DowntimeProfession0836.Enchanting=>used.IndexOf("Enchant",StringComparison.OrdinalIgnoreCase)>=0,
                DowntimeProfession0836.Artificing=>used.IndexOf("Artific",StringComparison.OrdinalIgnoreCase)>=0||used.IndexOf("Tinker",StringComparison.OrdinalIgnoreCase)>=0||used.IndexOf("Trap",StringComparison.OrdinalIgnoreCase)>=0,
                _=>false
            };
        }

        // 0.8.36a deterministic validator hook: simulate a non-serialized reaction window
        // surviving in memory immediately before ApplySaveData. A valid load must clear it.
        public bool DebugPrimeStaleReactionGate0836()
        {
            var enemy=Enemies.Values.FirstOrDefault(e=>!e.Defeated);
            var hero=ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated);
            if(enemy==null||hero==null)return false;
            PrepareReactionWindow(enemy,hero);
            return HasPendingReactionWindow;
        }


        // 0.8.36b validator hook: deliberately contaminate the current resolved-Quest runtime
        // with every non-durable gate that could block campaign routing after a load. The save used
        // by the validator is captured before this hook runs, so none of these gates belongs to it.
        public bool DebugPrimeStaleDowntimeRoutingGates0836()
        {
            bool reaction=DebugPrimeStaleReactionGate0836();
            PendingEncounterDeckControl=new EncounterDeckControlState
            {
                SourceCardId="DEBUG-0836B",SourceCardName="Stale Deck Control",Mode="KeepOrSwapTop2",
                RevealedCardIds=EncounterDeck.Take(2).ToList()
            };
            PendingRewardChoice=new RewardChoiceState
            {
                SourceCardId="DEBUG-0836B",SourceCardName="Stale Reward Choice",HeroId=SelectedHeroId,
                Mode="Gold25OrLoot1",Prompt="Stale validator-only reward choice"
            };
            IsResolving=true;
            return reaction&&HasPendingEncounterDeckControl&&HasPendingRewardChoice&&IsResolving;
        }

        public string DebugCampaignQuestRoutingState0836(string questId)
        {
            string completed=CampaignProgress0833==null?"<null>":string.Join(",",CampaignProgress0833.CompletedQuestIds);
            string available=string.Join(",",AvailableCampaignQuests0833);
            return $"Quest={CurrentQuestId0829} Complete={QuestComplete} Failed={QuestFailed} WE={CurrentWorldEventId0833} Completed=[{completed}] Recommended={RecommendedMainCampaignQuest0833??"<none>"} Available=[{available}] Resolving={IsResolving} Reaction={HasPendingReactionWindow} DeckControl={HasPendingEncounterDeckControl} RewardChoice={HasPendingRewardChoice} CanBegin({questId})={CanBeginCampaignQuest0833(questId)}";
        }

        private static string ProfessionHookText0836(DowntimeProfession0836 profession)
        {
            return profession switch
            {
                DowntimeProfession0836.Blacksmithing=>"Forge weapons, shields, armor, and metal components from Blacksmithing-tagged materials.",
                DowntimeProfession0836.Alchemy=>"Brew recovery, cleansing, offensive, and utility mixtures from Alchemy-tagged materials.",
                DowntimeProfession0836.Enchanting=>"Use crystals, sacred components, and magical materials for Enchanting recipes.",
                DowntimeProfession0836.Artificing=>"Use Tinkering, trap, tool, and Artificing components for devices and engineered gear.",
                _=>string.Empty
            };
        }
    }
}
