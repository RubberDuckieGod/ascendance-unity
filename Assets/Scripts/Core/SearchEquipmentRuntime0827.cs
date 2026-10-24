using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.27 — Search reward choices plus persistent player-controlled equipment loadouts.
    public sealed partial class AscendanceGameManager
    {

        private bool ResolveSearchReward0827(SearchCardDefinition card,LocationState location,HeroState hero)
        {
            if(card==null)return false;
            if(ResolveSearchExplorationReward0828(card,location,hero))return true;
            switch(card.Id)
            {
                case "SD001": DrawLootToHero(hero,card.Name); return true;
                case "SD003":
                    DrawLootToHero(hero,card.Name);
                    BeginRewardChoice0827(card.Id,card.Name,hero,"SearchDiscardHandOrLose5","Discard 1 Hero card from hand or lose 5 Gold.");
                    return true;
                case "SD004":
                    BeginRewardChoice0827(card.Id,card.Name,hero,"SearchGold20OrLoot1","Choose 20 Gold or draw 1 Loot card.");
                    return true;
                case "SD007": AddPartyGold(30,card.Name);DrawLootToHero(hero,card.Name);return true;
                case "SD008": HealHero(hero,1,card.Name,hero);DrawLootToHero(hero,card.Name);return true;
                case "SD009":
                {
                    string id=TakeTopEquipmentForChoice(card.Name);if(string.IsNullOrEmpty(id))return true;
                    string rarity=RewardContent0826.Equipment.TryGetValue(id,out var d)?d.Rarity:"Unknown";
                    BeginRewardChoice0827(card.Id,card.Name,hero,"SearchEquipmentKeepGate",$"Drew {RewardContent0826.EquipmentLabel(id)}. Common/Uncommon may be kept; otherwise bottom it and gain 25 Gold.",new[]{id});
                    AddLog($"SEARCH EQUIPMENT — {card.Name} reveals {RewardContent0826.EquipmentLabel(id)} ({rarity}).");return true;
                }
                case "SD011": AddPartyGold(10,card.Name);DrawLootToHero(hero,card.Name);return true;
                case "SD012":
                {
                    var ids=new List<string>();for(int i=0;i<2;i++){string id=TakeTopLootForChoice(card.Name);if(!string.IsNullOrEmpty(id))ids.Add(id);}if(ids.Count==0)return true;
                    if(ids.Count==1){KeepHeldLoot(hero,ids[0],card.Name);return true;}
                    BeginRewardChoice0827(card.Id,card.Name,hero,"SearchKeepOneLoot2",$"Choose 1 Loot to keep: {string.Join(" | ",ids.Select(RewardContent0826.LootLabel))}",ids);return true;
                }
                case "SD014":
                    DrawLootToHero(hero,card.Name);AddPartyGold(25,card.Name);
                    int level=ExtractNumber(location.FirstVisitEffect,@"Encounter Level\s+(\d+)");ResolveTravelEncounter(location,hero,level<=0?1:level,0,card.Name);return true;
                case "SD017":
                    BeginRewardChoice0827(card.Id,card.Name,hero,"SearchGold30OrCommonEquipment","Choose 30 Gold or draw 1 Equipment card and keep it only if Common.");return true;
                case "SD018": DrawLootToHero(hero,card.Name);return true;
                case "SD019":
                {
                    var ids=new List<string>();for(int i=0;i<2;i++){string id=TakeTopEquipmentForChoice(card.Name);if(!string.IsNullOrEmpty(id))ids.Add(id);}if(ids.Count==0)return true;
                    var qualifying=Enumerable.Range(0,ids.Count).Where(i=>IsEquipmentRarity(ids[i],"Common","Uncommon")).ToList();
                    if(qualifying.Count==0){foreach(var id in ids)BottomHeldEquipment(id,card.Name);AddPartyGold(30,card.Name);AddLog("SEARCH ARMORY — neither Equipment card qualified; party gains 30 Gold.");return true;}
                    if(qualifying.Count==1){int keep=qualifying[0];for(int i=0;i<ids.Count;i++){if(i==keep)KeepHeldEquipment(hero,ids[i],card.Name);else BottomHeldEquipment(ids[i],card.Name);}return true;}
                    BeginRewardChoice0827(card.Id,card.Name,hero,"SearchArmory2",$"Choose 1 Common/Uncommon Equipment to keep: {string.Join(" | ",ids.Select(RewardContent0826.EquipmentLabel))}",ids);return true;
                }
            }
            return false;
        }

        private static bool IsEquipmentRarity(string id, params string[] rarities)
        {
            if(!RewardContent0826.Equipment.TryGetValue(id??string.Empty,out var def))return false;
            return rarities.Any(r=>string.Equals(def.Rarity,r,StringComparison.OrdinalIgnoreCase));
        }

        private string TakeTopLootForChoice(string source)
        {
            if(LootDeck.Count==0&&LootDiscard.Count>0){LootDeck.AddRange(LootDiscard);LootDiscard.Clear();Shuffle(LootDeck);AddLog("LOOT DECK — discard reshuffled into the draw deck.");}
            if(LootDeck.Count==0){AppendPendingContent(source,"Loot Deck is empty.");return null;}
            string id=LootDeck[0];LootDeck.RemoveAt(0);return id;
        }

        private string TakeTopEquipmentForChoice(string source)
        {
            if(EquipmentDeck.Count==0){AppendPendingContent(source,"Equipment Deck is empty.");return null;}
            string id=EquipmentDeck[0];EquipmentDeck.RemoveAt(0);return id;
        }

        private void KeepHeldLoot(HeroState hero,string id,string source)
        {
            if(hero==null||string.IsNullOrEmpty(id))return;hero.LootInventory.Add(id);
            AddLog($"LOOT — {hero.DisplayName} keeps {RewardContent0826.LootLabel(id)} ({source}). Inventory: {hero.LootInventory.Count} Loot.");
            OnLootAcquired0854(hero,id,source);
        }

        private void DiscardHeldLoot(string id,string source)
        {
            if(string.IsNullOrEmpty(id))return;LootDiscard.Add(id);AddLog($"LOOT DISCARDED — {RewardContent0826.LootLabel(id)} ({source}).");
        }

        private void KeepHeldEquipment(HeroState hero,string id,string source)
        {
            if(hero==null||string.IsNullOrEmpty(id))return;hero.AcquiredEquipment.Add(id);
            AddLog($"EQUIPMENT — {hero.DisplayName} keeps {RewardContent0826.EquipmentLabel(id)} unequipped ({source}). Inventory Equipment: {hero.AcquiredEquipment.Count}.");
        }

        private void BottomHeldEquipment(string id,string source)
        {
            if(string.IsNullOrEmpty(id))return;EquipmentDeck.Add(id);AddLog($"EQUIPMENT DECK — {RewardContent0826.EquipmentLabel(id)} placed on bottom ({source}).");
        }

        private void DiscardHeldEquipment(string id,string source)
        {
            if(string.IsNullOrEmpty(id))return;EquipmentDiscard.Add(id);AddLog($"EQUIPMENT DISCARDED — {RewardContent0826.EquipmentLabel(id)} was drawn but not kept ({source}).");
        }

        private void BeginRewardChoice0827(string sourceCardId,string sourceCardName,HeroState hero,string mode,string prompt,IEnumerable<string> revealed=null)
        {
            if(HasPendingRewardChoice){AppendPendingContent(sourceCardId+" reward choice","Resolve the existing reward choice first.");return;}
            PendingRewardChoice=new RewardChoiceState{SourceCardId=sourceCardId,SourceCardName=sourceCardName,HeroId=hero?.Id,Mode=mode,Prompt=prompt,RevealedCardIds=revealed?.Where(x=>!string.IsNullOrEmpty(x)).ToList()??new List<string>()};
            AddLog($"REWARD CHOICE — {sourceCardId} {sourceCardName}: {prompt}");StateChanged?.Invoke();
        }

        private static string EquipmentChoiceLabel0850(string id)
        {
            if(!RewardContent0826.Equipment.TryGetValue(id??string.Empty,out var d))return id??"Equipment";
            string baseLabel=RewardContent0826.EquipmentLabel(id);
            string ability=RewardContent0826.EquipmentAbilityText(id);
            return string.IsNullOrWhiteSpace(ability)?baseLabel:$"{baseLabel} • ABILITY: {ability}";
        }

        private void Append0827RewardChoiceOptions(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(state==null)return;Append0828ExplorationChoiceOptions(options,state);AppendQuestChoiceOptions0829(options,state);AppendGenericQuestChoiceOptions0856(options,state);Append0852SearchChoiceOptions(options,state);Append0853EncounterChoiceOptions(options,state);Append0854RewardChoiceOptions(options,state);Append0858LocationChoiceOptions(options,state);var hero=RewardHeroFor(state.HeroId);var ids=state.RevealedCardIds??new List<string>();
            switch(state.Mode)
            {
                case "SearchDiscardHandOrLose5":
                    if(hero!=null)for(int i=0;i<hero.Hand.Count;i++)options.Add(new RewardChoiceOption{Key=$"HAND{i}",Label=$"Discard {hero.Hand[i].Name}"});
                    options.Add(new RewardChoiceOption{Key="LOSE5",Label="Lose 5 Gold"});
                    break;
                case "SearchGold20OrLoot1":
                    options.Add(new RewardChoiceOption{Key="GOLD",Label="Take 20 Gold"});options.Add(new RewardChoiceOption{Key="LOOT",Label="Draw 1 Loot"});break;
                case "SearchEquipmentKeepGate":
                    if(ids.Count>0)
                    {
                        string id=ids[0];if(IsEquipmentRarity(id,"Common","Uncommon"))options.Add(new RewardChoiceOption{Key="KEEP",Label=$"Keep {EquipmentChoiceLabel0850(id)}"});
                        options.Add(new RewardChoiceOption{Key="BOTTOM25",Label=$"Bottom {EquipmentChoiceLabel0850(id)} • Gain 25 Gold"});
                    }
                    break;
                case "SearchKeepOneLoot2":
                    for(int i=0;i<ids.Count;i++)options.Add(new RewardChoiceOption{Key=$"KEEP{i}",Label=$"Keep {RewardContent0826.LootLabel(ids[i])}"});break;
                case "SearchGold30OrCommonEquipment":
                    options.Add(new RewardChoiceOption{Key="GOLD",Label="Take 30 Gold"});options.Add(new RewardChoiceOption{Key="EQUIPMENT",Label="Draw Equipment • keep only if Common"});break;
                case "SearchArmory2":
                    for(int i=0;i<ids.Count;i++)if(IsEquipmentRarity(ids[i],"Common","Uncommon"))options.Add(new RewardChoiceOption{Key=$"KEEP{i}",Label=$"Keep {EquipmentChoiceLabel0850(ids[i])}"});break;
                case "ManageEquipment":
                    if(hero!=null)
                    {
                        for(int i=0;i<hero.AcquiredEquipment.Count;i++)
                        {
                            string id=hero.AcquiredEquipment[i];if(!RewardContent0826.Equipment.TryGetValue(id,out var d))continue;
                            if(string.Equals(d.Slot,"Accessory",StringComparison.OrdinalIgnoreCase))
                            {
                                var accessories=hero.StartingEquipment.Where(e=>string.Equals(e.Type,"Accessory",StringComparison.OrdinalIgnoreCase)).ToList();
                                int cap=AccessoryCapacity(hero);
                                if(accessories.Count<cap)options.Add(new RewardChoiceOption{Key=$"EQUIP{i}",Label=$"Equip {EquipmentChoiceLabel0850(id)} → open Accessory slot"});
                                else for(int r=0;r<accessories.Count;r++)options.Add(new RewardChoiceOption{Key=$"EQUIP{i}R{r}",Label=$"Equip {EquipmentChoiceLabel0850(id)} • replace {accessories[r].Name}"});
                            }
                            else options.Add(new RewardChoiceOption{Key=$"EQUIP{i}",Label=$"Equip {EquipmentChoiceLabel0850(id)} → {d.Slot}"});
                        }
                    }
                    options.Add(new RewardChoiceOption{Key="CLOSE",Label="Close Equipment"});
                    break;
            }
        }

        private bool TryResolve0827RewardChoice(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state==null)return false;if(TryResolveGenericQuestChoice0856(state,hero,key,out result))return true;if(TryResolve0858LocationChoice(state,hero,key,out result))return true;if(TryResolve0854RewardChoice(state,hero,key,out result))return true;if(TryResolve0853EncounterChoice(state,hero,key,out result))return true;if(TryResolve0852SearchChoice(state,hero,key,out result))return true;if(TryResolve0828ExplorationChoice(state,hero,key,out result))return true;if(TryResolveQuestChoice0829(state,hero,key,out result))return true;var ids=state.RevealedCardIds??new List<string>();
            switch(state.Mode)
            {
                case "SearchDiscardHandOrLose5":
                    if(key=="LOSE5")AddPartyGold(-5,state.SourceCardName);
                    else if(key.StartsWith("HAND")&&int.TryParse(key.Substring(4),out int hi)&&hero!=null&&hi>=0&&hi<hero.Hand.Count){var c=hero.Hand[hi];hero.Hand.RemoveAt(hi);hero.Discard.Add(c.Id);AddLog($"SEARCH COST — {hero.DisplayName} discards {c.Name} ({state.SourceCardName}).");}
                    else return true;
                    break;
                case "SearchGold20OrLoot1":
                    if(key=="GOLD")AddPartyGold(20,state.SourceCardName);else if(key=="LOOT")DrawLootToHero(hero,state.SourceCardName);else return true;break;
                case "SearchEquipmentKeepGate":
                    if(ids.Count==0)return true;
                    if(key=="KEEP"&&IsEquipmentRarity(ids[0],"Common","Uncommon"))KeepHeldEquipment(hero,ids[0],state.SourceCardName);
                    else if(key=="BOTTOM25"){BottomHeldEquipment(ids[0],state.SourceCardName);AddPartyGold(25,state.SourceCardName);}else return true;break;
                case "SearchKeepOneLoot2":
                    if(!key.StartsWith("KEEP")||!int.TryParse(key.Substring(4),out int li)||li<0||li>=ids.Count)return true;
                    for(int i=0;i<ids.Count;i++){if(i==li)KeepHeldLoot(hero,ids[i],state.SourceCardName);else DiscardHeldLoot(ids[i],state.SourceCardName);}break;
                case "SearchGold30OrCommonEquipment":
                    if(key=="GOLD")AddPartyGold(30,state.SourceCardName);
                    else if(key=="EQUIPMENT")
                    {
                        string id=TakeTopEquipmentForChoice(state.SourceCardName);if(!string.IsNullOrEmpty(id)){if(IsEquipmentRarity(id,"Common"))KeepHeldEquipment(hero,id,state.SourceCardName);else DiscardHeldEquipment(id,state.SourceCardName+" — non-Common draw");}
                    }
                    else return true;break;
                case "SearchArmory2":
                    if(!key.StartsWith("KEEP")||!int.TryParse(key.Substring(4),out int ei)||ei<0||ei>=ids.Count||!IsEquipmentRarity(ids[ei],"Common","Uncommon"))return true;
                    for(int i=0;i<ids.Count;i++){if(i==ei)KeepHeldEquipment(hero,ids[i],state.SourceCardName);else BottomHeldEquipment(ids[i],state.SourceCardName);}break;
                case "ManageEquipment":
                    if(key=="CLOSE")
                    {
                        AddLog($"EQUIPMENT MANAGEMENT — {hero?.DisplayName??"Hero"} closes the loadout screen.");PendingRewardChoice=null;StateChanged?.Invoke();result=true;return true;
                    }
                    if(key.StartsWith("EQUIP")&&hero!=null)
                    {
                        string body=key.Substring(5);int replaceAccessory=-1;int rAt=body.IndexOf('R');string indexText=rAt>=0?body.Substring(0,rAt):body;
                        if(rAt>=0)int.TryParse(body.Substring(rAt+1),out replaceAccessory);
                        if(int.TryParse(indexText,out int gi)&&gi>=0&&gi<hero.AcquiredEquipment.Count)
                        {
                            string id=hero.AcquiredEquipment[gi];result=EquipAcquiredEquipment(hero,id,replaceAccessory);if(result){PendingRewardChoice.Prompt=EquipmentManagementPrompt(hero);StateChanged?.Invoke();}return true;
                        }
                    }
                    return true;
                default:return false;
            }
            AddLog($"REWARD CHOICE RESOLVED — {state.SourceCardId} {state.SourceCardName}: {key}.");PendingRewardChoice=null;TryResolveDeferredPostEncounter0852();StateChanged?.Invoke();result=true;return true;
        }

        private EquipmentRuntimeData RuntimeEquipmentFromCatalog(string id,int copies=1)
        {
            if(!RewardContent0826.Equipment.TryGetValue(id??string.Empty,out var d))return null;
            return new EquipmentRuntimeData{Id=d.Id,Name=d.Name,Type=d.Type,Copies=Mathf.Max(1,copies),Attack=d.Attack,Armor=d.Armor,Ability1=d.Ability1,Ability2=d.Ability2};
        }

        private static bool WeaponOccupiesBothHands(string id)
        {
            if(!RewardContent0826.Equipment.TryGetValue(id??string.Empty,out var d))return false;
            return string.Equals(d.Hands,"Two-Handed",StringComparison.OrdinalIgnoreCase)||string.Equals(d.Hands,"Dual-Wield",StringComparison.OrdinalIgnoreCase);
        }

        private void RecalculateHeroEquipmentStats(HeroState hero)
        {
            if(hero==null)return;int weaponBonus=0,armorBonus=0,maxHealthBonus=0;bool hasWeapon=false;
            foreach(var eq in hero.StartingEquipment)
            {
                int copies=Mathf.Max(1,eq.Copies);weaponBonus+=eq.Attack*copies;armorBonus+=eq.Armor*copies;
                if(string.Equals(eq.Type,"Weapon",StringComparison.OrdinalIgnoreCase))hasWeapon=true;
                if(string.Equals(eq.Id,"EQ097",StringComparison.OrdinalIgnoreCase))maxHealthBonus+=2*copies;
            }
            if(string.Equals(hero.Id,"Kael",StringComparison.OrdinalIgnoreCase)&&!hasWeapon)weaponBonus+=2;
            hero.Attack=Mathf.Max(0,hero.BaseAttack+weaponBonus);hero.Armor=Mathf.Max(0,hero.BaseArmor+armorBonus);
            int authoredMax=HeroDatabase.GetHero(hero.Id)?.maxHealth??Mathf.Max(1,hero.MaxHealth-maxHealthBonus);
            hero.MaxHealth=Mathf.Max(1,authoredMax+maxHealthBonus);hero.Health=Mathf.Min(hero.Health,hero.MaxHealth);
        }

        private void RestoreEquippedLoadout(HeroState hero,IEnumerable<string> equippedIds)
        {
            if(hero==null)return;var groups=(equippedIds??Enumerable.Empty<string>()).Where(RewardContent0826.Equipment.ContainsKey).GroupBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
            hero.StartingEquipment.Clear();foreach(var g in groups){var runtime=RuntimeEquipmentFromCatalog(g.Key,g.Count());if(runtime!=null)hero.StartingEquipment.Add(runtime);}RecalculateHeroEquipmentStats(hero);
        }

        private string EquippedLoadoutSummary(HeroState hero)
        {
            if(hero==null)return "None";var labels=new List<string>();foreach(var eq in hero.StartingEquipment)labels.Add($"{eq.Name}{(RewardContent0826.EquipmentHasAbility(eq.Id)?" [ABILITY]":string.Empty)}{(eq.Copies>1?$"×{eq.Copies}":string.Empty)}");
            if(string.Equals(hero.Id,"Kael",StringComparison.OrdinalIgnoreCase)&&!hero.StartingEquipment.Any(e=>string.Equals(e.Type,"Weapon",StringComparison.OrdinalIgnoreCase)))labels.Insert(0,"Unarmed (+2 Weapon)");
            return labels.Count==0?"None":string.Join(", ",labels);
        }

        private string EquipmentManagementPrompt(HeroState hero)=>hero==null?"No Hero selected.":$"{hero.DisplayName} • Equipped: {EquippedLoadoutSummary(hero)} • Inventory Gear: {hero.AcquiredEquipment.Count}";

        public bool CanOpenEquipmentManagement(HeroState hero)=>hero!=null&&hero.Participating&&!IsResolving&&!HasPendingReactionWindow&&!HasPendingEncounterDeckControl&&!HasPendingRewardChoice&&!ParticipatingHeroes.Any(h=>h.Moving);

        public void BeginEquipmentManagement()
        {
            var hero=SelectedHero;if(!CanOpenEquipmentManagement(hero))return;BeginRewardChoice0827("GEAR","Equipment Management",hero,"ManageEquipment",EquipmentManagementPrompt(hero));
        }

        private static int AccessoryCapacity(HeroState hero)=>hero!=null&&string.Equals(hero.Id,"Seraphine",StringComparison.OrdinalIgnoreCase)?1:2;

        public bool EquipAcquiredEquipment(HeroState hero,string id,int replaceAccessoryIndex=-1)
        {
            if(hero==null||string.IsNullOrEmpty(id)||!RewardContent0826.Equipment.TryGetValue(id,out var next))return false;
            int invIndex=hero.AcquiredEquipment.FindIndex(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));if(invIndex<0)return false;
            if(string.Equals(next.Slot,"Shield",StringComparison.OrdinalIgnoreCase))
            {
                var weapon=hero.StartingEquipment.FirstOrDefault(e=>string.Equals(e.Type,"Weapon",StringComparison.OrdinalIgnoreCase));
                if(weapon!=null&&WeaponOccupiesBothHands(weapon.Id)){AddLog($"EQUIPMENT — {hero.DisplayName} cannot equip {next.Name} while {weapon.Name} occupies both hands.");return false;}
            }
            var replaced=new List<EquipmentRuntimeData>();
            if(string.Equals(next.Slot,"Accessory",StringComparison.OrdinalIgnoreCase))
            {
                var accessories=hero.StartingEquipment.Where(e=>string.Equals(e.Type,"Accessory",StringComparison.OrdinalIgnoreCase)).ToList();
                int cap=AccessoryCapacity(hero);
                if(accessories.Count>=cap)
                {
                    if(replaceAccessoryIndex<0||replaceAccessoryIndex>=accessories.Count){AddLog($"EQUIPMENT — choose which Accessory {next.Name} replaces.");return false;}
                    replaced.Add(accessories[replaceAccessoryIndex]);
                }
            }
            else replaced.AddRange(hero.StartingEquipment.Where(e=>string.Equals(e.Type,next.Slot,StringComparison.OrdinalIgnoreCase)||string.Equals(e.Type,next.Type,StringComparison.OrdinalIgnoreCase)));

            hero.AcquiredEquipment.RemoveAt(invIndex);
            foreach(var old in replaced){hero.StartingEquipment.Remove(old);for(int c=0;c<Mathf.Max(1,old.Copies);c++)hero.AcquiredEquipment.Add(old.Id);}
            if(string.Equals(next.Slot,"Weapon",StringComparison.OrdinalIgnoreCase)&&WeaponOccupiesBothHands(id))
            {
                var shields=hero.StartingEquipment.Where(e=>string.Equals(e.Type,"Shield",StringComparison.OrdinalIgnoreCase)).ToList();
                foreach(var old in shields){hero.StartingEquipment.Remove(old);for(int c=0;c<Mathf.Max(1,old.Copies);c++)hero.AcquiredEquipment.Add(old.Id);AddLog($"EQUIPMENT — {old.Name} returns to inventory because {next.Name} occupies both hands.");}
            }
            var runtime=RuntimeEquipmentFromCatalog(id);if(runtime==null){hero.AcquiredEquipment.Insert(Mathf.Min(invIndex,hero.AcquiredEquipment.Count),id);return false;}hero.StartingEquipment.Add(runtime);RecalculateHeroEquipmentStats(hero);
            AddLog($"EQUIPMENT EQUIPPED — {hero.DisplayName} equips {RewardContent0826.EquipmentLabel(id)}. Attack {hero.Attack} • Armor {hero.Armor}. Replaced gear returned to inventory.");return true;
        }

        public bool DebugResolveSearchCard0827(string cardId,string locationId,string heroId)
        {
            if(!SearchEncounterContent0820.SearchCards.TryGetValue(cardId??string.Empty,out var card)||!Locations.TryGetValue(locationId??string.Empty,out var location)||!Heroes.TryGetValue(heroId??string.Empty,out var hero))return false;
            LastSearchCardId=card.Id;AddLog($"SEARCH CARD — {card.Id} {card.Name}: {card.Effect} (0.8.27 direct validation)");ResolveSearchCard(card,location,hero);StateChanged?.Invoke();return true;
        }

        public string DebugEquippedLoadout(string heroId)=>Heroes.TryGetValue(heroId??string.Empty,out var hero)?EquippedLoadoutSummary(hero):string.Empty;
        public bool DebugEquipAcquiredEquipment(string heroId,string equipmentId)=>Heroes.TryGetValue(heroId??string.Empty,out var hero)&&EquipAcquiredEquipment(hero,equipmentId);
    }
}
