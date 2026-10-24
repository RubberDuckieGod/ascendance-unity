using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.39 — each participating Hero receives one dedicated Downtime Activity per
    // resolved-Quest downtime period. Crafting and Trading are deliberately NOT activities
    // and remain available independently through their existing systems.
    public sealed partial class AscendanceGameManager
    {
        private int debugNextGatherCount0839;

        public string CurrentDowntimePeriodKey0839
        {
            get
            {
                if(!IsDowntimeAvailable0836)return string.Empty;
                string result=QuestComplete?"COMPLETE":"FAILED";
                return $"{CurrentWorldEventId0833}|WE-DAY-{WorldEventDay0833}|TOTAL-{TotalCampaignDay0833}|{CurrentQuestId0829}|{result}";
            }
        }

        public bool HasUsedDowntimeActivity0839(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return false;
            string key=CurrentDowntimePeriodKey0839;
            return !string.IsNullOrWhiteSpace(key)&&string.Equals(h.LastDowntimeActivityKey0839,key,StringComparison.OrdinalIgnoreCase);
        }

        public bool CanTakeDowntimeActivity0839(string heroId)
        {
            return CanUseDowntimeActions0836
                && Heroes.TryGetValue(heroId??string.Empty,out var h)
                && h.Participating
                && !HasUsedDowntimeActivity0839(heroId);
        }

        public string DowntimeActivityStatus0839(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!h.Participating)return "No participating Hero selected.";
            if(!IsDowntimeAvailable0836)return "Downtime Activity unavailable while a Quest is active.";
            if(HasUsedDowntimeActivity0839(heroId))
                return $"ACTIVITY USED THIS DOWNTIME — {(string.IsNullOrWhiteSpace(h.LastDowntimeActivityLabel0839)?"completed":h.LastDowntimeActivityLabel0839)}";
            return "ACTIVITY AVAILABLE — choose WORK for 25 Party Gold or GATHER RESOURCES for one profession. Crafting and Trading remain unlimited and do not use this Activity.";
        }

        public bool WorkDowntime0839(string heroId)
        {
            if(!CanTakeDowntimeActivity0839(heroId)||!Heroes.TryGetValue(heroId,out var h))return false;
            PartyGold+=25;
            h.LastDowntimeActivityKey0839=CurrentDowntimePeriodKey0839;
            h.LastDowntimeActivityLabel0839="WORK — +25 Party Gold";
            AddLog($"DOWNTIME ACTIVITY — {h.DisplayName} works in {CurrentCampaignHub0845} and earns 25 Gold for the party. Party Gold: {PartyGold}. Crafting and Trading remain available.");
            StateChanged?.Invoke();
            return true;
        }

        public bool CanGatherResources0839(string heroId,DowntimeProfession0836 profession)
        {
            if(!CanTakeDowntimeActivity0839(heroId))return false;
            return LootDeck.Any(id=>IsGatherableProfessionResource0839(id,profession));
        }

        public IReadOnlyList<string> GatherableResources0839(DowntimeProfession0836 profession)
        {
            return LootDeck.Where(id=>IsGatherableProfessionResource0839(id,profession)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public bool GatherResources0839(string heroId,DowntimeProfession0836 profession)
        {
            if(!CanGatherResources0839(heroId,profession)||!Heroes.TryGetValue(heroId,out var h))return false;

            int desired=debugNextGatherCount0839>0?Mathf.Clamp(debugNextGatherCount0839,1,3):UnityEngine.Random.Range(1,4);
            debugNextGatherCount0839=0;
            var gathered=new List<string>();

            while(gathered.Count<desired)
            {
                var eligibleIndexes=new List<int>();
                for(int i=0;i<LootDeck.Count;i++)if(IsGatherableProfessionResource0839(LootDeck[i],profession))eligibleIndexes.Add(i);
                if(eligibleIndexes.Count==0)break;

                // Prefer a material type not already found during this Gather when possible,
                // allowing mixed resource results while still respecting the live finite Loot Deck.
                var freshTypeIndexes=eligibleIndexes.Where(i=>!gathered.Contains(LootDeck[i],StringComparer.OrdinalIgnoreCase)).ToList();
                var pool=freshTypeIndexes.Count>0?freshTypeIndexes:eligibleIndexes;
                int deckIndex=pool[UnityEngine.Random.Range(0,pool.Count)];
                string id=LootDeck[deckIndex];
                LootDeck.RemoveAt(deckIndex);
                h.LootInventory.Add(id);
                gathered.Add(id);
            }

            if(gathered.Count==0)return false;
            h.LastDowntimeActivityKey0839=CurrentDowntimePeriodKey0839;
            string summary=string.Join(", ",gathered.GroupBy(x=>x,StringComparer.OrdinalIgnoreCase).Select(g=>$"{g.Count()}× {RewardContent0826.LootLabel(g.Key)}"));
            h.LastDowntimeActivityLabel0839=$"GATHER {profession.ToString().ToUpperInvariant()} — {gathered.Count} material{(gathered.Count==1?string.Empty:"s")}";
            AddLog($"DOWNTIME ACTIVITY — {h.DisplayName} gathers {gathered.Count} {profession} material{(gathered.Count==1?string.Empty:"s")}: {summary}. Materials enter only {h.DisplayName}'s inventory. Party Gold unchanged: {PartyGold}.");
            StateChanged?.Invoke();
            return true;
        }

        private static bool IsGatherableProfessionResource0839(string lootId,DowntimeProfession0836 profession)
        {
            if(!RewardContent0826.Loot.TryGetValue(lootId??string.Empty,out var d))return false;
            if(!d.CraftingMaterial||d.QuestItem)return false;
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

        // Validator-only deterministic control so the mixed-material and 3-resource cap can be
        // proven without changing normal gameplay randomness.
        public void DebugSetNextGatherCount0839(int count)=>debugNextGatherCount0839=Mathf.Clamp(count,1,3);
    }
}
