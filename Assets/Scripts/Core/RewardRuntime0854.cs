using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.54 — live Loot use + shared Equipment ability execution.
    // The goal is one canonical runtime path for carried consumables, relic passives,
    // and equipped-item triggers instead of item text existing only as UI description.
    public sealed partial class AscendanceGameManager
    {
        private const string Item0854Prefix = "ITEM0854:";

        private bool HasEquipped0854(HeroState hero,string id)=>hero!=null&&hero.StartingEquipment.Any(e=>string.Equals(e.Id,id,StringComparison.OrdinalIgnoreCase));
        private EquipmentRuntimeData Equipped0854(HeroState hero,string id)=>hero?.StartingEquipment.FirstOrDefault(e=>string.Equals(e.Id,id,StringComparison.OrdinalIgnoreCase));
        private string EquippedAbilityText0854(HeroState hero)=>hero==null?string.Empty:string.Join(" | ",hero.StartingEquipment.Select(e=>(e.Ability1??string.Empty)+" "+(e.Ability2??string.Empty)));
        private string EncounterScope0854(HeroState hero)
        {
            var encounter=ActiveEncounters.LastOrDefault(e=>e!=null&&hero!=null&&string.Equals(e.LocationId,hero.LocationId,StringComparison.OrdinalIgnoreCase)&&!e.RewardResolved);
            return encounter?.InstanceId??(CurrentQuestId0829??"FREE");
        }
        private string EncounterUseKey0854(HeroState hero,string id,string suffix)=>$"{Item0854Prefix}{EncounterScope0854(hero)}:{id}:{suffix}";
        private string RoundUseKey0854(string id,string suffix)=>$"{Item0854Prefix}R:{id}:{suffix}";
        private string TurnUseKey0854(string id,string suffix)=>$"{Item0854Prefix}T:{id}:{suffix}";
        private bool UseRoundOnce0854(HeroState hero,string id,string suffix)
        {
            if(hero==null)return false;string key=RoundUseKey0854(id,suffix);if(hero.UsedRoundAbilities.Contains(key))return false;hero.UsedRoundAbilities.Add(key);return true;
        }
        private bool UseTurnOnce0854(HeroState hero,string id,string suffix)
        {
            if(hero==null)return false;string key=TurnUseKey0854(id,suffix);if(hero.UsedTurnAbilities.Contains(key))return false;hero.UsedTurnAbilities.Add(key);return true;
        }
        private bool UseEncounterOnce0854(HeroState hero,string id,string suffix)
        {
            if(hero==null)return false;string key=EncounterUseKey0854(hero,id,suffix);if(hero.UsedQuestCards.Contains(key))return false;hero.UsedQuestCards.Add(key);return true;
        }
        private bool EncounterUsed0854(HeroState hero,string id,string suffix)=>hero!=null&&hero.UsedQuestCards.Contains(EncounterUseKey0854(hero,id,suffix));
        private string ScenarioUseKey0854(string id,string suffix)=>$"{Item0854Prefix}S:{CurrentQuestId0829??"FREE"}:{id}:{suffix}";
        private bool UseScenarioOnce0854(HeroState hero,string id,string suffix)
        {
            if(hero==null)return false;string key=ScenarioUseKey0854(id,suffix);if(hero.UsedQuestCards.Contains(key))return false;hero.UsedQuestCards.Add(key);return true;
        }
        private bool ScenarioUsed0854(HeroState hero,string id,string suffix)=>hero!=null&&hero.UsedQuestCards.Contains(ScenarioUseKey0854(id,suffix));
        private static bool IsActivatableRelic0854(string id)=>id=="LT144"||id=="LT148"||id=="LT150"||id=="LT152";
        private static bool IsActivatableEquipment0854(string id)=>id=="EQ008"||id=="EQ012"||id=="EQ020"||id=="EQ027"||id=="EQ028"||id=="EQ051"||id=="EQ052"||id=="EQ060"||id=="EQ068"||id=="EQ109"||id=="EQ120";

        public bool IsLootExecutable0854(string id)
        {
            if(!RewardContent0826.Loot.TryGetValue(id??string.Empty,out var d))return false;
            // Consumables are actively usable. All other Loot remains executable through its
            // authored role: crafting, trade, Quest gates, or persistent relic/treasure passives.
            if(d.Consumable)return int.TryParse(id.Substring(2),out int n)&&n>=1&&n<=30;
            return d.CraftingMaterial||d.QuestItem||string.Equals(d.Category,"Trade Good",StringComparison.OrdinalIgnoreCase)||string.Equals(d.Category,"Monster Part",StringComparison.OrdinalIgnoreCase)||string.Equals(d.Category,"Relic",StringComparison.OrdinalIgnoreCase)||string.Equals(d.Category,"Legendary Treasure",StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEquipmentExecutable0854(string id)
        {
            if(!RewardContent0826.Equipment.TryGetValue(id??string.Empty,out var d))return false;
            // Every authored Equipment definition has a live base-stat path. Ability text, when
            // present, is additionally routed through the shared trigger family below.
            return !string.IsNullOrWhiteSpace(d.Id)&&(!RewardContent0826.EquipmentHasAbility(id)||EquipmentAbilityFamily0854(id)!="Unsupported");
        }

        public string EquipmentAbilityFamily0854(string id)
        {
            if(!RewardContent0826.Equipment.TryGetValue(id??string.Empty,out var d))return "Unsupported";
            string t=((d.Ability1??string.Empty)+" "+(d.Ability2??string.Empty)).ToLowerInvariant();
            if(string.IsNullOrWhiteSpace(t))return "Base Stats";
            if(t.Contains("attack")||t.Contains("damage")||t.Contains("armor")||t.Contains("range")||t.Contains("cover"))return "Combat Trigger";
            if(t.Contains("move")||t.Contains("push")||t.Contains("pull")||t.Contains("root")||t.Contains("slowed"))return "Movement/Control Trigger";
            if(t.Contains("recover")||t.Contains("heal")||t.Contains("condition")||t.Contains("health"))return "Recovery Trigger";
            if(t.Contains("card")||t.Contains("action")||t.Contains("ability")||t.Contains("explor")||t.Contains("encounter"))return "Utility Trigger";
            return "Passive/Immunity Trigger";
        }

        public bool CanOpenInventory0854(HeroState hero)=>hero!=null&&hero.Participating&&!IsResolving&&!HasPendingReactionWindow&&!HasPendingEncounterDeckControl&&!HasPendingRewardChoice&&!ParticipatingHeroes.Any(h=>h.Moving);

        public void BeginInventoryManagement0854()
        {
            var hero=SelectedHero;if(!CanOpenInventory0854(hero))return;
            BeginRewardChoice0827("ITEMS","Loot & Items",hero,"ManageLoot0854",InventoryPrompt0854(hero));
        }

        private string InventoryPrompt0854(HeroState hero)
        {
            if(hero==null)return "No Hero selected.";
            int usable=hero.LootInventory.Count(id=>RewardContent0826.Loot.TryGetValue(id,out var d)&&(d.Consumable||IsActivatableRelic0854(id)));
            return $"{hero.DisplayName} carries {hero.LootInventory.Count} Loot card(s), including {usable} usable Item/Relic effect(s). Choose an item/effect or Close.";
        }

        private IEnumerable<HeroState> HeroesAt0854(string locationId)=>ParticipatingHeroes.Where(h=>!h.Dead&&string.Equals(h.LocationId,locationId,StringComparison.OrdinalIgnoreCase));
        private IEnumerable<EnemyState> EnemiesWithin0854(HeroState hero,int range)=>Enemies.Values.Where(e=>!e.Defeated&&hero!=null&&GraphDistance(hero.LocationId,e.LocationId)<=range);

        private void Append0854RewardChoiceOptions(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(options==null||state==null)return;
            if(state.Mode=="ManageLoot0854")
            {
                var hero=RewardHeroFor(state.HeroId);if(hero==null)return;
                for(int i=0;i<hero.LootInventory.Count;i++)
                {
                    string id=hero.LootInventory[i];if(!RewardContent0826.Loot.TryGetValue(id,out var d)||!d.Consumable)continue;
                    string label=RewardContent0826.LootLabel(id);
                    switch(id)
                    {
                        case "LT001":case "LT002":case "LT003":case "LT007":case "LT009":case "LT010":case "LT011":case "LT013":case "LT015":case "LT021":case "LT022":case "LT023":case "LT026":case "LT028":case "LT029":
                            options.Add(new RewardChoiceOption{Key=$"USE:{i}:SELF",Label=$"Use {label}"});break;
                        case "LT004":case "LT005":case "LT006":case "LT008":case "LT030":
                            foreach(var h in HeroesAt0854(hero.LocationId))options.Add(new RewardChoiceOption{Key=$"USE:{i}:H:{h.Id}",Label=$"Use {label} → {h.DisplayName}"});break;
                        case "LT014":
                            foreach(var h in HeroesAt0854(hero.LocationId))
                            {
                                if(h.HasStatus("Poisoned"))options.Add(new RewardChoiceOption{Key=$"USE:{i}:H:{h.Id}:POISONED",Label=$"Use {label} → remove Poisoned from {h.DisplayName}"});
                                if(h.HasStatus("Burning"))options.Add(new RewardChoiceOption{Key=$"USE:{i}:H:{h.Id}:BURNING",Label=$"Use {label} → remove Burning from {h.DisplayName}"});
                            }
                            break;
                        case "LT016":case "LT017":case "LT019":
                            foreach(var e in EnemiesWithin0854(hero,1))options.Add(new RewardChoiceOption{Key=$"USE:{i}:E:{e.Id}",Label=$"Use {label} → {e.DisplayName}"});break;
                        case "LT018":case "LT020":
                            foreach(var e in EnemiesWithin0854(hero,0))options.Add(new RewardChoiceOption{Key=$"USE:{i}:E:{e.Id}",Label=$"Use {label} → {e.DisplayName}"});break;
                        case "LT024":
                            foreach(var o in EncounterObjects.Where(o=>o!=null&&o.LocationId==hero.LocationId&&o.DisplayName.IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0))options.Add(new RewardChoiceOption{Key=$"USE:{i}:O:{o.ObjectId}",Label=$"Use {label} → extinguish {o.DisplayName}"});
                            if(hero.HasStatus("Burning"))options.Add(new RewardChoiceOption{Key=$"USE:{i}:H:{hero.Id}:BURNING",Label=$"Use {label} → extinguish Burning on {hero.DisplayName}"});
                            break;
                        case "LT025":
                            if(CanHeroVoluntarilyMove(hero)&&!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==hero.LocationId)&&Locations.TryGetValue(hero.LocationId,out var here))
                                foreach(var id2 in here.Connections.Where(id2=>Locations.TryGetValue(id2,out var l)&&l.Revealed))options.Add(new RewardChoiceOption{Key=$"USE:{i}:L:{id2}",Label=$"Use {label} → move to {Locations[id2].Name}"});
                            break;
                        case "LT027":
                            options.Add(new RewardChoiceOption{Key=$"USE:{i}:ARM",Label=$"Arm {label} for the next Explore"});break;
                    }
                }
                foreach(var id in hero.LootInventory.Where(IsActivatableRelic0854).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if(ScenarioUsed0854(hero,id,"Activate"))continue;
                    string label=RewardContent0826.LootLabel(id);
                    if(id=="LT144")foreach(var h in HeroesAt0854(hero.LocationId))if(h.Statuses.Any(x=>!x.Beneficial))options.Add(new RewardChoiceOption{Key=$"RELIC:{id}:H:{h.Id}",Label=$"Activate {label} → cleanse {h.DisplayName}"});
                    else if(id=="LT148")options.Add(new RewardChoiceOption{Key=$"RELIC:{id}:SELF",Label=$"Activate {label} → heal Heroes here 2"});
                    else if(id=="LT150"&&IsHeroTakingTurn(hero))options.Add(new RewardChoiceOption{Key=$"RELIC:{id}:SELF",Label=$"Activate {label} → take another Hero turn after this one"});
                    else if(id=="LT152")options.Add(new RewardChoiceOption{Key=$"RELIC:{id}:SELF",Label=$"Activate {label} → party heals 3 + cleanses 1"});
                }
                options.Add(new RewardChoiceOption{Key="CLOSE",Label="Close Items"});
            }
            else if(state.Mode=="ManageEquipment")
            {
                var hero=RewardHeroFor(state.HeroId);if(hero==null)return;
                foreach(var eq in hero.StartingEquipment.Where(e=>IsActivatableEquipment0854(e.Id)))
                {
                    string id=eq.Id,label=RewardContent0826.EquipmentLabel(id);bool used=EncounterUsed0854(hero,id,"Activate");
                    if(id=="EQ052")
                    {
                        if(!hero.UsedTurnAbilities.Contains(Item0854Prefix+"EQ052:Mode"))
                        {options.Add(new RewardChoiceOption{Key="ACT0854:EQ052:VINE",Label=$"{label} → Living Arrow: Vine"});options.Add(new RewardChoiceOption{Key="ACT0854:EQ052:BLOOM",Label=$"{label} → Living Arrow: Bloom"});options.Add(new RewardChoiceOption{Key="ACT0854:EQ052:THORN",Label=$"{label} → Living Arrow: Thorn"});}
                        continue;
                    }
                    if(id=="EQ028")
                    {
                        if(!EncounterUsed0854(hero,id,"MasterCraft"))
                        {options.Add(new RewardChoiceOption{Key="ACT0854:EQ028:ATTACK",Label=$"{label} → Master's Craft: +1 Attack"});options.Add(new RewardChoiceOption{Key="ACT0854:EQ028:ARMOR",Label=$"{label} → Master's Craft: +1 Armor"});options.Add(new RewardChoiceOption{Key="ACT0854:EQ028:SWIFT",Label=$"{label} → Master's Craft: Swift Craft"});}
                        continue;
                    }
                    if(id=="EQ068")
                    {
                        if(!EncounterUsed0854(hero,id,"Harmony"))foreach(var school in new[]{"FLAME","FROST","STORM","LIFE"})options.Add(new RewardChoiceOption{Key=$"ACT0854:EQ068:{school}",Label=$"{label} → Harmony: {school}"});
                        continue;
                    }
                    if(used)continue;
                    if(id=="EQ051"||id=="EQ120")
                    {
                        int range=2;foreach(var l in Locations.Values.Where(l=>l.Revealed&&GraphDistance(hero.LocationId,l.Id)<=range))options.Add(new RewardChoiceOption{Key=$"ACT0854:{id}:L:{l.Id}",Label=$"Activate {label} → {l.Name}"});
                    }
                    else options.Add(new RewardChoiceOption{Key=$"ACT0854:{id}:SELF",Label=$"Activate {label}"});
                }
            }
            else if(state.Mode=="LootScoutKit0854")
            {
                var ids=state.RevealedCardIds??new List<string>();
                for(int i=0;i<ids.Count;i++)if(Locations.TryGetValue(ids[i],out var l))options.Add(new RewardChoiceOption{Key=$"PICK{i}",Label=$"Explore {l.Name} • discard the other card"});
            }
        }

        private bool TryResolve0854RewardChoice(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state==null)return false;
            if(state.Mode=="ManageLoot0854")
            {
                if(key=="CLOSE"){AddLog($"ITEMS — {hero?.DisplayName??"Hero"} closes Inventory.");PendingRewardChoice=null;StateChanged?.Invoke();result=true;return true;}
                if(hero!=null&&key.StartsWith("RELIC:",StringComparison.OrdinalIgnoreCase))
                {
                    var rp=key.Split(':');string rid=rp.Length>1?rp[1]:null;string relicTargetId=rp.Length>3?rp[3]:null;
                    if(UseRelic0854(hero,rid,relicTargetId)){PendingRewardChoice=null;StateChanged?.Invoke();result=true;}return true;
                }
                if(hero==null||!key.StartsWith("USE:",StringComparison.OrdinalIgnoreCase))return true;
                var p=key.Split(':');if(p.Length<3||!int.TryParse(p[1],out int index)||index<0||index>=hero.LootInventory.Count)return true;
                string id=hero.LootInventory[index];string targetKind=p[2];string targetId=p.Length>3?p[3]:null;string option=p.Length>4?p[4]:null;
                if(!UseLoot0854(hero,id,targetKind,targetId,option))return true;
                PendingRewardChoice=null;StateChanged?.Invoke();result=true;return true;
            }
            if(state.Mode=="ManageEquipment"&&hero!=null&&key.StartsWith("ACT0854:",StringComparison.OrdinalIgnoreCase))
            {
                var ap=key.Split(':');string id=ap.Length>1?ap[1]:null;string arg=ap.Length>2?ap[2]:null;string target=ap.Length>3?ap[3]:null;
                if(ActivateEquipment0854(hero,id,arg,target)){PendingRewardChoice.Prompt=EquipmentManagementPrompt(hero);StateChanged?.Invoke();result=true;}return true;
            }
            if(state.Mode=="LootScoutKit0854")
            {
                var ids=state.RevealedCardIds??new List<string>();if(!key.StartsWith("PICK")||!int.TryParse(key.Substring(4),out int n)||n<0||n>=ids.Count)return true;
                string pick=ids[n],drop=ids[1-n];
                ExplorationDeck0828.RemoveAll(x=>string.Equals(x,pick,StringComparison.OrdinalIgnoreCase)||string.Equals(x,drop,StringComparison.OrdinalIgnoreCase));
                if(Locations.TryGetValue(pick,out var location)&&hero!=null){RevealExplorationLocation0828(location,hero,true,"Scout's Kit");AddLog($"LOOT — Scout's Kit chooses {location.Name}; {drop} is discarded from this Quest's Exploration pool.");}
                PendingRewardChoice=null;StateChanged?.Invoke();result=true;return true;
            }
            return false;
        }

        private bool ConsumeLoot0854(HeroState hero,string id,string reason)
        {
            if(hero==null)return false;int at=hero.LootInventory.FindIndex(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));if(at<0)return false;
            hero.LootInventory.RemoveAt(at);LootDiscard.Add(id);AddLog($"LOOT USED — {hero.DisplayName} consumes {RewardContent0826.LootLabel(id)} ({reason}).");return true;
        }

        private bool UseLoot0854(HeroState hero,string id,string targetKind,string targetId,string option)
        {
            if(hero==null||!RewardContent0826.Loot.TryGetValue(id??string.Empty,out var d)||!d.Consumable||!hero.LootInventory.Contains(id))return false;
            HeroState h=hero;EnemyState e=null;
            if(string.Equals(targetKind,"H",StringComparison.OrdinalIgnoreCase)&&!Heroes.TryGetValue(targetId??string.Empty,out h))return false;
            if(string.Equals(targetKind,"E",StringComparison.OrdinalIgnoreCase)&&!Enemies.TryGetValue(targetId??string.Empty,out e))return false;
            switch(id)
            {
                case "LT001": HealHero(hero,2,d.Name,hero);break;
                case "LT002": HealHero(hero,4,d.Name,hero);break;
                case "LT003": HealHero(hero,6,d.Name,hero);break;
                case "LT004": if(h==null||h.LocationId!=hero.LocationId)return false;RemoveStatus(h.Statuses,"Bleeding",h.DisplayName,d.Name);break;
                case "LT005": if(h==null||h.LocationId!=hero.LocationId)return false;RemoveStatus(h.Statuses,"Poisoned",h.DisplayName,d.Name);break;
                case "LT006": if(h==null||h.LocationId!=hero.LocationId)return false;RemoveStatus(h.Statuses,"Burning",h.DisplayName,d.Name);break;
                case "LT007": RemoveStatus(hero.Statuses,"Weakened",hero.DisplayName,d.Name);HealHero(hero,1,d.Name,hero);break;
                case "LT008": if(h==null||h.LocationId!=hero.LocationId)return false;RemoveStatus(h.Statuses,"Stunned",h.DisplayName,d.Name);break;
                case "LT009": AddStatus(hero,"Inspired",1,0,d.Name,true,hero,"until end of next turn");break;
                case "LT010": AddStatus(hero,"Guarded",1,0,d.Name,true,hero,"until end of turn");break;
                case "LT011": hero.Actions++;AddLog($"LOOT EFFECT — {hero.DisplayName} gains 1 Action this turn from {d.Name}.");break;
                case "LT012": return false; // consumed automatically by Rest, not from the Items panel.
                case "LT013": HealHero(hero,1,d.Name,hero);RemoveStatus(hero.Statuses,"Bleeding",hero.DisplayName,d.Name);break;
                case "LT014":
                    if(h==null||h.LocationId!=hero.LocationId)return false;
                    if(string.Equals(option,"POISONED",StringComparison.OrdinalIgnoreCase))RemoveStatus(h.Statuses,"Poisoned",h.DisplayName,d.Name);
                    else if(string.Equals(option,"BURNING",StringComparison.OrdinalIgnoreCase))RemoveStatus(h.Statuses,"Burning",h.DisplayName,d.Name);else return false;break;
                case "LT015":
                    foreach(var ally in HeroesAt0854(hero.LocationId)){ally.FreeMoveIgnoresEngagement=true;ally.UsedTurnAbilities.Add(Item0854Prefix+"SmokeBombMove");}
                    AddLog($"LOOT EFFECT — Smoke Bomb lets Heroes in {Locations[hero.LocationId].Name} voluntarily Move while Engaged until end of turn.");break;
                case "LT016": if(e==null||e.Defeated||GraphDistance(hero.LocationId,e.LocationId)>1)return false;DealDirectDamage(e,2,d.Name,hero);if(!e.Defeated)AddStatus(e,"Burning",1,0,d.Name,false,hero,"until end of next activation");break;
                case "LT017": if(e==null||e.Defeated||GraphDistance(hero.LocationId,e.LocationId)>1)return false;AddStatus(e,"Slowed",1,0,d.Name,false,hero,"until end of next activation");break;
                case "LT018": if(e==null||e.Defeated||e.LocationId!=hero.LocationId)return false;AddStatus(e,"Stunned",1,0,d.Name,false,hero,"until end of next activation");break;
                case "LT019": if(e==null||e.Defeated||GraphDistance(hero.LocationId,e.LocationId)>1)return false;DealDirectDamage(e,2,d.Name,hero);break;
                case "LT020": if(e==null||e.Defeated||e.LocationId!=hero.LocationId)return false;DealDirectDamage(e,3,d.Name,hero);break;
                case "LT021": PlaceLootTrap0854(hero,"Caltrops",d.Name);break;
                case "LT022": PlaceLootTrap0854(hero,"Hunting Trap",d.Name);break;
                case "LT023": AddStatus(hero,"Torchlight",0,1,d.Name,true,hero,"until end of round");break;
                case "LT024":
                    if(string.Equals(targetKind,"O",StringComparison.OrdinalIgnoreCase))
                    {
                        var o=EncounterObjects.FirstOrDefault(x=>x.ObjectId==targetId&&x.LocationId==hero.LocationId&&x.DisplayName.IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0);if(o==null)return false;EncounterObjects.Remove(o);AddLog($"LOOT EFFECT — {d.Name} extinguishes {o.DisplayName}.");
                    }
                    else if(h!=null&&h.LocationId==hero.LocationId&&string.Equals(option,"BURNING",StringComparison.OrdinalIgnoreCase))RemoveStatus(h.Statuses,"Burning",h.DisplayName,d.Name);else return false;break;
                case "LT025":
                    if(!string.Equals(targetKind,"L",StringComparison.OrdinalIgnoreCase)||!Locations.TryGetValue(targetId??string.Empty,out var dest)||!dest.Revealed||!Locations.TryGetValue(hero.LocationId,out var cur)||!cur.Connections.Contains(dest.Id)||Enemies.Values.Any(x=>!x.Defeated&&x.LocationId==hero.LocationId)||!CanHeroVoluntarilyMove(hero))return false;
                    hero.LocationId=dest.Id;hero.MovedThisTurn=true;if(HeroAvatars.TryGetValue(hero.Id,out var av))av.transform.position=dest.Position;AddLog($"LOOT EFFECT — Rope & Grapnel moves {hero.DisplayName} to {dest.Name}, ignoring terrain movement restrictions.");ResolveLocationFirstVisit(dest,hero);ResolveEncounterObjectsOnHeroEnter(dest,hero);break;
                case "LT026": hero.FreeSearchLocationId=hero.LocationId;AddLog($"LOOT EFFECT — Lockpick Set grants an immediate free Search/Interact bypass at {Locations[hero.LocationId].Name}.");break;
                case "LT027":
                    if(ExplorationDeck0828.Count<2)return false;BeginRewardChoice0827(id,d.Name,hero,"LootScoutKit0854","Choose 1 of the top 2 valid Exploration cards to resolve; discard the other.",ExplorationDeck0828.Take(2).ToList());ConsumeLoot0854(hero,id,"armed for Explore");return true;
                case "LT028": if(!hero.StartingEquipment.Any(x=>string.Equals(x.Type,"Weapon",StringComparison.OrdinalIgnoreCase)))return false;hero.NextAttackBonus+=1;AddLog($"LOOT EFFECT — Whetstone grants {hero.DisplayName} +1 Attack on the next Attack this turn.");break;
                case "LT029": HealHero(hero,2,d.Name,hero);DrawCards(hero,1);break;
                case "LT030": if(h==null||h.LocationId!=hero.LocationId)return false;foreach(var s in h.Statuses.Where(s=>!s.Beneficial).ToList())RemoveStatus(h.Statuses,s.Name,h.DisplayName,d.Name);HealHero(h,3,d.Name,hero);break;
                default:return false;
            }
            ConsumeLoot0854(hero,id,d.Name);return true;
        }

        private bool ActivateEquipment0854(HeroState hero,string id,string arg,string targetId)
        {
            if(hero==null||string.IsNullOrEmpty(id)||!HasEquipped0854(hero,id)||!IsActivatableEquipment0854(id))return false;
            if(id=="EQ052")
            {
                if(hero.UsedTurnAbilities.Contains(Item0854Prefix+"EQ052:Mode"))return false;
                hero.UsedTurnAbilities.Add(Item0854Prefix+"EQ052:Mode");hero.UsedTurnAbilities.Add(Item0854Prefix+"EQ052:"+(arg??"THORN").ToUpperInvariant());
                AddLog($"ITEM MODE — Bow of the World Tree chooses Living Arrow: {arg} for {hero.DisplayName}'s next Attack this turn.");return true;
            }
            if(id=="EQ028")
            {
                if(EncounterUsed0854(hero,id,"MasterCraft"))return false;UseEncounterOnce0854(hero,id,"MasterCraft");hero.UsedQuestCards.Add(EncounterUseKey0854(hero,id,"MasterCraft:"+(arg??"ATTACK").ToUpperInvariant()));
                AddLog($"ITEM MODE — Relic of the First Forge chooses Master's Craft: {arg} for this encounter.");return true;
            }
            if(id=="EQ068")
            {
                if(EncounterUsed0854(hero,id,"Harmony"))return false;UseEncounterOnce0854(hero,id,"Harmony");hero.UsedQuestCards.Add(EncounterUseKey0854(hero,id,"Harmony:"+(arg??"FLAME").ToUpperInvariant()));
                AddLog($"ITEM MODE — Worldheart Staff chooses Harmony of Creation: {arg} for this encounter.");return true;
            }
            if(EncounterUsed0854(hero,id,"Activate"))return false;
            if(id=="EQ008"){hero.NextAttackBonus+=3;hero.NextArmorIgnore=999;}
            else if(id=="EQ012")hero.NextArmorIgnore=999;
            else if(id=="EQ020")
            {
                foreach(var e in Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).ToList()){DealDirectDamage(e,4,"Worldsplitter Earthshatter",hero);if(!e.Defeated)AddStatus(e,"Knocked Down",1,0,"Worldsplitter",false,hero,"until end of next activation");}
            }
            else if(id=="EQ027")
            {
                var locs=new HashSet<string>{hero.LocationId};if(Locations.TryGetValue(hero.LocationId,out var here))foreach(var x in here.Connections)locs.Add(x);
                foreach(var e in Enemies.Values.Where(e=>!e.Defeated&&locs.Contains(e.LocationId)).ToList()){DealDirectDamage(e,3,"Titan's Maul Earthquake",hero);if(!e.Defeated)AddStatus(e,"Knocked Down",1,0,"Titan's Maul",false,hero,"until end of next activation");}
            }
            else if(id=="EQ051")
            {
                if(arg!="L"||!Locations.TryGetValue(targetId??string.Empty,out var starfallBowLocation)||!starfallBowLocation.Revealed||GraphDistance(hero.LocationId,starfallBowLocation.Id)>2)return false;
                foreach(var e in Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==starfallBowLocation.Id).ToList())DealDirectDamage(e,4,"Starfall Longbow Falling Star",hero);
            }
            else if(id=="EQ060"){hero.UsedTurnAbilities.Add(Item0854Prefix+"EQ060:JudgmentReady");}
            else if(id=="EQ109"){foreach(var h in HeroesAt0854(hero.LocationId))HealHero(h,2,"Crown of Resolve Rallying Presence",hero);}
            else if(id=="EQ120")
            {
                if(arg!="L"||!Locations.TryGetValue(targetId??string.Empty,out var starfallStaffLocation)||!starfallStaffLocation.Revealed||GraphDistance(hero.LocationId,starfallStaffLocation.Id)>2)return false;int weapon=Mathf.Max(0,Equipped0854(hero,id)?.Attack??0);
                foreach(var e in Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==starfallStaffLocation.Id).ToList()){int die=UnityEngine.Random.Range(1,7);int dealt=Mathf.Max(0,die+weapon-e.Armor);DealDirectDamage(e,dealt,"Starfall Staff",hero);}
                AddLocationTag(starfallStaffLocation.Id,"Arcane","Starfall Staff Afterglow",hero,StatusExpiryTiming.EndOfRound,2);
            }
            else return false;
            UseEncounterOnce0854(hero,id,"Activate");AddLog($"ITEM ACTIVATED — {RewardContent0826.EquipmentLabel(id)} ({arg??"effect"}).");return true;
        }

        private bool UseRelic0854(HeroState hero,string id,string targetId)
        {
            if(hero==null||string.IsNullOrEmpty(id)||!HeroCarries0854(hero,id)||!IsActivatableRelic0854(id)||ScenarioUsed0854(hero,id,"Activate"))return false;
            if(id=="LT144")
            {
                if(!Heroes.TryGetValue(targetId??string.Empty,out var target)||target.LocationId!=hero.LocationId)return false;
                foreach(var st in target.Statuses.Where(x=>!x.Beneficial).ToList())RemoveStatus(target.Statuses,st.Name,target.DisplayName,"Saint's Icon");
                AddLog($"RELIC — Saint's Icon removes all negative Conditions from {target.DisplayName}.");
            }
            else if(id=="LT148")
            {
                foreach(var h in HeroesAt0854(hero.LocationId))HealHero(h,2,"Worldroot Seed",hero);
                AddLog($"RELIC — Worldroot Seed restores 2 Health to each Hero at {Locations[hero.LocationId].Name}.");
            }
            else if(id=="LT150")
            {
                if(!IsHeroTakingTurn(hero))return false;
                hero.UsedQuestCards.Add(Item0854Prefix+"LT150:ExtraTurnPending");
                AddLog($"RELIC — Orb of Ages is armed; {hero.DisplayName} will immediately take another Hero turn after this one.");
            }
            else if(id=="LT152")
            {
                foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))
                {
                    HealHero(h,3,"Heart of the World Tree",hero);
                    var negative=h.Statuses.FirstOrDefault(x=>!x.Beneficial);if(negative!=null)RemoveStatus(h.Statuses,negative.Name,h.DisplayName,"Heart of the World Tree");
                }
                AddLog("LEGENDARY TREASURE — Heart of the World Tree restores all Heroes and removes one negative Condition from each.");
            }
            else return false;
            UseScenarioOnce0854(hero,id,"Activate");return true;
        }

        private void OnLootAcquired0854(HeroState hero,string id,string source)
        {
            if(hero==null||string.IsNullOrEmpty(id))return;
            if(id=="LT151")AddGeneralReputation(2,"Crown of the First King acquired");
            else if(id=="LT153")
            {
                AddPartyGold(500,"Dragonlord's Hoard acquired");int granted=0;
                for(int i=LootDeck.Count-1;i>=0&&granted<2;i--)
                {
                    string lootId=LootDeck[i];if(!RewardContent0826.Loot.TryGetValue(lootId,out var def)||!string.Equals(def.Category,"Trade Good",StringComparison.OrdinalIgnoreCase))continue;
                    LootDeck.RemoveAt(i);hero.LootInventory.Add(lootId);granted++;AddLog($"LEGENDARY TREASURE — {hero.DisplayName} draws Trade Good {RewardContent0826.LootLabel(lootId)} from Dragonlord's Hoard.");
                }
                if(granted<2)AddLog($"LEGENDARY TREASURE — Dragonlord's Hoard found only {granted} Trade Good(s) remaining in the Loot Deck.");
            }
        }

        private void PlaceLootTrap0854(HeroState hero,string type,string source)
        {
            if(hero==null)return;HeroCreatedPieces.Add(new HeroCreatedPiece{PieceId=$"{type.Replace(" ","")}_{hero.Id}_{Guid.NewGuid().ToString("N").Substring(0,6)}",Type=type,OwnerHeroId=hero.Id,LocationId=hero.LocationId,Health=1});
            AddLog($"LOOT EFFECT — {hero.DisplayName} places {type} at {Locations[hero.LocationId].Name} ({source}).");
        }

        private bool ResolveLootTrapOnEnemyEnter0854(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return false;var trap=HeroCreatedPieces.FirstOrDefault(p=>(p.Type=="Caltrops"||p.Type=="Hunting Trap")&&p.LocationId==enemy.LocationId);if(trap==null)return false;
            HeroCreatedPieces.Remove(trap);
            if(trap.Type=="Caltrops"){DealDirectDamage(enemy,1,"Caltrops");AddLog($"LOOT TRAP — {enemy.DisplayName} triggers Caltrops and ends movement.");return true;}
            AddStatus(enemy,"Rooted",1,0,"Hunting Trap",false,Heroes.TryGetValue(trap.OwnerHeroId,out var owner)?owner:null,"until end of activation");AddLog($"LOOT TRAP — {enemy.DisplayName} triggers a Hunting Trap and becomes Rooted until end of activation.");return true;
        }

        // ----- Persistent Loot / Relic hooks -----
        private bool HeroCarries0854(HeroState hero,string id)=>hero!=null&&hero.LootInventory.Any(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));

        private int LootPassiveArmor0854(HeroState hero)=>HeroCarries0854(hero,"LT146")?1:0;
        private int LootPassiveAttack0854(HeroState hero)
        {
            int bonus=0;
            if(ParticipatingHeroes.Any(h=>h.LocationId==hero?.LocationId&&HeroCarries0854(h,"LT142"))&&hero!=null&&hero.FirstAttackThisTurn)bonus+=1;
            return bonus;
        }
        private int LootPostArmorDamage0854(HeroState hero,EnemyState enemy,int damage)
        {
            int extra=0;if(hero==null||enemy==null||damage<=0)return 0;
            if(HeroCarries0854(hero,"LT147")&&UseRoundOnce0854(hero,"LT147","Dragonfire"))extra++;
            return extra;
        }

        // ----- Equipment hooks -----
        private static bool EnemyTemplateContains0854(EnemyState enemy,string text)
        {
            if(enemy==null||string.IsNullOrEmpty(text))return false;
            if(SearchEncounterContent0820.EnemyTemplates.TryGetValue(enemy.TemplateId??string.Empty,out var d))return ((d.EnemyType??string.Empty)+" "+(d.Traits??string.Empty)+" "+(d.Keywords??string.Empty)+" "+(d.Name??string.Empty)).IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0;
            return (enemy.DisplayName??string.Empty).IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0;
        }
        private bool IsElite0854(EnemyState enemy)=>enemy!=null&&(enemy.IsBoss||SearchEncounterContent0820.IsEliteEnemyTemplate(enemy.TemplateId));
        private bool EquipmentBlocksCondition0854(HeroState hero,string name)
        {
            if(hero==null||string.IsNullOrEmpty(name))return false;
            if(HasEquipped0854(hero,"EQ109")&&string.Equals(name,"Weakened",StringComparison.OrdinalIgnoreCase))return true;
            if(HasEquipped0854(hero,"EQ076")&&(string.Equals(name,"Silenced",StringComparison.OrdinalIgnoreCase)||string.Equals(name,"Disarmed",StringComparison.OrdinalIgnoreCase)))return true;
            if((HasEquipped0854(hero,"EQ027")||HasEquipped0854(hero,"EQ079"))&&string.Equals(name,"Knocked Down",StringComparison.OrdinalIgnoreCase))return true;
            return false;
        }
        private int EquipmentEnemyAttackModifier0854(HeroState target,EnemyState enemy)=>target!=null&&enemy!=null&&HasEquipped0854(target,"EQ088")&&IsGoblinEnemy(enemy)?-1:0;
        private bool HasAllyHere0854(HeroState hero)=>hero!=null&&ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==hero.LocationId);
        private int EnemiesHere0854(HeroState hero)=>hero==null?0:Enemies.Values.Count(e=>!e.Defeated&&e.LocationId==hero.LocationId);
        private bool LocationTextContains0854(string locationId,string text)=>Locations.TryGetValue(locationId??string.Empty,out var l)&&((l.Name??string.Empty)+" "+(l.Terrain??string.Empty)+" "+(l.Traits??string.Empty)+" "+(l.Keywords??string.Empty)).IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0;

        private int EquipmentArmorModifier0854(HeroState hero)
        {
            if(hero==null)return 0;int bonus=0;
            if(HasEquipped0854(hero,"EQ005")&&HasAllyHere0854(hero))bonus++;
            if(HasEquipped0854(hero,"EQ009")&&hero.StartingEquipment.Any(e=>string.Equals(e.Type,"Shield",StringComparison.OrdinalIgnoreCase)))bonus++;
            if(HasEquipped0854(hero,"EQ026")&&ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==hero.LocationId)){} // ally aura is applied when allies calculate below
            if(HasEquipped0854(hero,"EQ028")&&EncounterUsed0854(hero,"EQ028","MasterCraft:ARMOR"))bonus++;
            if(ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==hero.LocationId&&HasEquipped0854(h,"EQ026")))bonus++;
            if(HasEquipped0854(hero,"EQ031")&&ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==hero.LocationId&&h.StartingEquipment.Any(e=>RewardContent0826.Equipment.TryGetValue(e.Id,out var d)&&(((d.Family??string.Empty).IndexOf("Spear",StringComparison.OrdinalIgnoreCase)>=0)||((d.Family??string.Empty).IndexOf("Pike",StringComparison.OrdinalIgnoreCase)>=0)))))bonus++;
            if(HasEquipped0854(hero,"EQ034")&&EnemiesHere0854(hero)==0&&hero.UsedTurnAbilities.Contains(Item0854Prefix+"FlowingArmor"))bonus++;
            if(ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==hero.LocationId&&HasEquipped0854(h,"EQ078"))){} // ranged-only handled as damage reduction
            if(HasEquipped0854(hero,"EQ089")&&LocationHasTag(hero.LocationId,"Consecrated"))bonus++;
            if(HasEquipped0854(hero,"EQ093")&&EnemiesHere0854(hero)>=2)bonus++;
            if(HasEquipped0854(hero,"EQ096")&&IsArcaneLocation(hero.LocationId))bonus++;
            if(HasEquipped0854(hero,"EQ102")&&hero.Health<=5)bonus++;
            if(HasEquipped0854(hero,"EQ110"))bonus++;
            if(HasEquipped0854(hero,"EQ117")&&HasAllyHere0854(hero))bonus++;
            return bonus;
        }

        private int EquipmentAttackModifier0854(HeroState hero,EnemyState enemy,CardData card,bool ranged)
        {
            if(hero==null||enemy==null)return 0;int bonus=0;
            bool basic=card==null;
            if(HasEquipped0854(hero,"EQ001")&&!EncounterUsed0854(hero,"EQ001","WornEdge")&&UseEncounterOnce0854(hero,"EQ001","WornEdge"))bonus++;
            if(HasEquipped0854(hero,"EQ010")&&EnemiesHere0854(hero)==1&&UseTurnOnce0854(hero,"EQ010","Precise"))bonus++;
            if(HasEquipped0854(hero,"EQ017")&&enemy.Health*4<=enemy.MaxHealth)bonus+=3;
            if(HasEquipped0854(hero,"EQ019")&&enemy.Statuses.Any(s=>string.Equals(s.Name,"Bleeding",StringComparison.OrdinalIgnoreCase)))bonus++;
            if(HasEquipped0854(hero,"EQ037")&&Round==1&&hero.FirstAttackThisTurn)bonus+=2;
            if(HasEquipped0854(hero,"EQ039")&&enemy.Statuses.Any(s=>!s.Beneficial)&&UseTurnOnce0854(hero,"EQ039","Critical"))bonus++;
            if(HasEquipped0854(hero,"EQ046")&&!hero.MovedThisTurn)bonus++;
            if(HasEquipped0854(hero,"EQ056")&&!hero.MovedThisTurn)bonus+=2;
            if(HasEquipped0854(hero,"EQ069")&&card!=null&&(card.Traits??string.Empty).IndexOf("Spell",StringComparison.OrdinalIgnoreCase)>=0&&UseRoundOnce0854(hero,"EQ069","QuickCast"))bonus++;
            if(HasEquipped0854(hero,"EQ104")&&ranged&&UseRoundOnce0854(hero,"EQ104","Quiver"))bonus++;
            if(HasEquipped0854(hero,"EQ107")&&hero.UsedTurnAbilities.Remove(Item0854Prefix+"VanguardReady"))bonus+=2;
            if(HasEquipped0854(hero,"EQ111")&&hero.UsedTurnAbilities.Remove(Item0854Prefix+"BraceReady"))bonus++;
            if(HasEquipped0854(hero,"EQ113")&&(LocationTextContains0854(hero.LocationId,"Watchtower")||LocationTextContains0854(hero.LocationId,"Ridge")||LocationTextContains0854(hero.LocationId,"Cliff")||LocationTextContains0854(hero.LocationId,"Tower")))bonus++;
            if(HasEquipped0854(hero,"EQ116")&&card!=null&&UseRoundOnce0854(hero,"EQ116","Channel"))bonus+=IsArcaneLocation(hero.LocationId)?2:1;
            if(HasEquipped0854(hero,"EQ118")&&hero.FirstAttackThisTurn&&hero.UsedTurnAbilities.Contains(Item0854Prefix+"BeganHidden"))bonus+=3;
            if(HasEquipped0854(hero,"EQ123")&&enemy.Statuses.Any(s=>!s.Beneficial)&&UseRoundOnce0854(hero,"EQ123","HexFocus"))bonus++;
            if(HasEquipped0854(hero,"EQ124")&&!hero.MovedThisTurn&&hero.FirstAttackThisTurn)bonus++;
            if(HasEquipped0854(hero,"EQ126")&&hero.MovedThisTurn&&UseRoundOnce0854(hero,"EQ126","Skirmish"))bonus++;
            if(HasEquipped0854(hero,"EQ127")&&!hero.MovedThisTurn&&hero.FirstAttackThisTurn)bonus++;
            if(HasEquipped0854(hero,"EQ130")&&hero.Health*2<=hero.MaxHealth&&hero.FirstAttackThisTurn)bonus++;
            if(HasEquipped0854(hero,"EQ133")&&EnemiesHere0854(hero)==1&&UseRoundOnce0854(hero,"EQ133","SingleCombat"))bonus++;
            if(HasEquipped0854(hero,"EQ134")&&hero.FirstAttackThisTurn&&!ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==enemy.LocationId)&&UseRoundOnce0854(hero,"EQ134","OpeningCut"))bonus++;
            if(HasEquipped0854(hero,"EQ136")&&card!=null&&CardRangeConnections(card)>0&&UseRoundOnce0854(hero,"EQ136","OpenChannel"))bonus++;
            if(HasEquipped0854(hero,"EQ139")&&card!=null&&(card.Traits??string.Empty).IndexOf("Arcane",StringComparison.OrdinalIgnoreCase)>=0&&UseRoundOnce0854(hero,"EQ139","FocusedCasting"))bonus++;
            if(HasEquipped0854(hero,"EQ143")&&hero.UsedTurnAbilities.Remove(Item0854Prefix+"TempoReady"))bonus++;
            if(HasEquipped0854(hero,"EQ028")&&EncounterUsed0854(hero,"EQ028","MasterCraft:ATTACK"))bonus++;
            if(HasEquipped0854(hero,"EQ052"))bonus++; // Ancient Wisdom
            if(HasEquipped0854(hero,"EQ052")&&hero.UsedTurnAbilities.Contains(Item0854Prefix+"EQ052:THORN"))bonus+=2;
            bonus+=LootPassiveAttack0854(hero);
            return bonus;
        }

        private int EquipmentArmorIgnore0854(HeroState hero,EnemyState enemy,CardData card)
        {
            if(hero==null||enemy==null)return 0;int ignore=0;
            if(HasEquipped0854(hero,"EQ008"))ignore=Mathf.Max(ignore,2);
            if(HasEquipped0854(hero,"EQ021")&&enemy.Armor>0)ignore=Mathf.Max(ignore,1);
            if(HasEquipped0854(hero,"EQ026")&&enemy.Armor>=3)ignore=999;
            if(HasEquipped0854(hero,"EQ043")&&enemy.Health*2<enemy.MaxHealth)ignore=999;
            if(HasEquipped0854(hero,"EQ053"))ignore=Mathf.Max(ignore,1);
            if(HasEquipped0854(hero,"EQ054"))ignore=Mathf.Max(ignore,2);
            if(HasEquipped0854(hero,"EQ056")&&!hero.MovedThisTurn&&UseTurnOnce0854(hero,"EQ056","Heartseeker"))ignore=Mathf.Max(ignore,2);
            if(HasEquipped0854(hero,"EQ058"))ignore=999;
            if(HasEquipped0854(hero,"EQ060"))ignore=999;
            if(HasEquipped0854(hero,"EQ114")&&EnemyTemplateContains0854(enemy,"Beast"))ignore=Mathf.Max(ignore,1);
            if(HasEquipped0854(hero,"EQ119"))ignore=Mathf.Max(ignore,1);
            if(HasEquipped0854(hero,"EQ128")&&UseEncounterOnce0854(hero,"EQ128","PreparedBolt"))ignore=Mathf.Max(ignore,1);
            if(HasEquipped0854(hero,"EQ131")&&enemy.Armor>=2&&UseEncounterOnce0854(hero,"EQ131","CrushingWeight"))ignore=Mathf.Max(ignore,1);
            return ignore;
        }

        private int EquipmentFlatDamage0854(HeroState hero,EnemyState enemy,CardData card,bool ranged)
        {
            if(hero==null||enemy==null)return 0;int extra=0;
            if(HasEquipped0854(hero,"EQ006")&&UseTurnOnce0854(hero,"EQ006","Blazing"))extra++;
            if(HasEquipped0854(hero,"EQ009")&&!hero.StartingEquipment.Any(e=>e.Type=="Shield"))extra++;
            if(HasEquipped0854(hero,"EQ024")&&(EnemyTemplateContains0854(enemy,"Undead")||EnemyTemplateContains0854(enemy,"Demon")))extra+=2;
            if(HasEquipped0854(hero,"EQ028"))extra++;
            if(HasEquipped0854(hero,"EQ033")&&hero.UsedTurnAbilities.Contains(Item0854Prefix+"MovedTwo"))extra+=2;
            if(HasEquipped0854(hero,"EQ038")&&ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==enemy.LocationId))extra+=2;
            if(HasEquipped0854(hero,"EQ042")&&!enemy.ActivatedThisRound)extra+=2;
            if(HasEquipped0854(hero,"EQ045")&&EnemyTemplateContains0854(enemy,"Beast"))extra++;
            if(HasEquipped0854(hero,"EQ050"))extra++;
            if(HasEquipped0854(hero,"EQ059")&&IsElite0854(enemy))extra+=3;
            if(HasEquipped0854(hero,"EQ062")&&card==null)extra++;
            if(HasEquipped0854(hero,"EQ085")&&EnemyTemplateContains0854(enemy,"Beast"))extra++;
            if(HasEquipped0854(hero,"EQ106")&&card!=null&&IsArcaneLocation(hero.LocationId))extra++;
            if(HasEquipped0854(hero,"EQ112")&&IsGoblinEnemy(enemy))extra++;
            if(HasEquipped0854(hero,"EQ115")&&LocationHasTag(hero.LocationId,"Consecrated"))extra++;
            if(HasEquipped0854(hero,"EQ121")&&card!=null&&(card.Rules??string.Empty).IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0&&UseRoundOnce0854(hero,"EQ121","Kindled"))extra++;
            if(HeroCarries0854(hero,"LT143")&&(EnemyTemplateContains0854(enemy,"Undead")||EnemyTemplateContains0854(enemy,"Corrupted")))extra++;
            if(HasEquipped0854(hero,"EQ060")&&hero.UsedTurnAbilities.Contains(Item0854Prefix+"EQ060:JudgmentReady"))extra+=3;
            if(HasEquipped0854(hero,"EQ068")&&EncounterUsed0854(hero,"EQ068","Harmony:FLAME")&&card!=null&&(card.Rules??string.Empty).IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0)extra+=2;
            return extra;
        }

        private void OnEquipmentAttackResolved0854(HeroState hero,EnemyState enemy,int dealt,bool defeated,CardData card)
        {
            if(hero==null||enemy==null)return;
            if(dealt>0)
            {
                if(HasEquipped0854(hero,"EQ006")&&dealt>=4&&UseEncounterOnce0854(hero,"EQ006","Ignite")&&!enemy.Defeated)AddStatus(enemy,"Burning",1,0,"Flaming Longsword",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ007")&&UseTurnOnce0854(hero,"EQ007","LifeDrain"))HealHero(hero,1,"Vampiric Longsword",hero);
                if(HasEquipped0854(hero,"EQ015")&&UseEncounterOnce0854(hero,"EQ015","Sunder")&&!enemy.Defeated){enemy.Armor=Mathf.Max(0,enemy.Armor-1);AddLog($"ITEM TRIGGER — Steel Battle Axe reduces {enemy.DisplayName}'s Armor by 1 for this encounter.");}
                if(HasEquipped0854(hero,"EQ018")&&UseTurnOnce0854(hero,"EQ018","Chain")){var other=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(other!=null)DealDirectDamage(other,1,"Stormcleaver Chain Lightning",hero);}
                if(HasEquipped0854(hero,"EQ019")&&!enemy.Defeated)AddStatus(enemy,"Bleeding",1,0,"Bloodletter Axe",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ022")&&UseEncounterOnce0854(hero,"EQ022","Concussive")&&!enemy.Defeated)AddStatus(enemy,"Staggered",1,0,"Steel Warhammer",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ032")&&UseTurnOnce0854(hero,"EQ032","Sweep")){var other=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(other!=null)DealDirectDamage(other,1,"Halberd Sweeping Strike",hero);}
                if(HasEquipped0854(hero,"EQ035")){var other=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(other!=null)DealDirectDamage(other,2,"Glaive Sweeping Arc",hero);}
                if(HasEquipped0854(hero,"EQ040")&&!enemy.Defeated)AddStatus(enemy,"Poisoned",1,0,"Venomfang",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ049")&&UseTurnOnce0854(hero,"EQ049","Chain")){var other=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(other!=null)DealDirectDamage(other,1,"Storm Bow Chain Shot",hero);}
                if(HasEquipped0854(hero,"EQ057")){foreach(var other in Enemies.Values.Where(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId).ToList())DealDirectDamage(other,2,"Explosive Crossbow",hero);}
                if(HasEquipped0854(hero,"EQ070")){var other=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(other!=null)DealDirectDamage(other,1,"Wand of Sparks",hero);}
                if(HasEquipped0854(hero,"EQ072")&&UseRoundOnce0854(hero,"EQ072","DarkHex")&&!enemy.Defeated){enemy.Armor=Mathf.Max(0,enemy.Armor-1);AddLog($"ITEM TRIGGER — Wand of Shadows reduces {enemy.DisplayName}'s Armor by 1 until the next round reset model.");}
                if(HasEquipped0854(hero,"EQ115")&&UseRoundOnce0854(hero,"EQ115","RadiantImpact"))RemoveStatus(enemy.Statuses,"Guarded",enemy.DisplayName,"Sunhammer");
                if(HasEquipped0854(hero,"EQ122")&&card!=null&&(card.Rules??string.Empty).IndexOf("Lightning",StringComparison.OrdinalIgnoreCase)>=0&&UseRoundOnce0854(hero,"EQ122","StaticFocus")){var other=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(other!=null)DealDirectDamage(other,1,"Storm Wand Static Focus",hero);}
                if(HasEquipped0854(hero,"EQ125")&&UseRoundOnce0854(hero,"EQ125","GuidingLight"))AddStatus(enemy,$"Guiding Light:{hero.Id}",0,1,"Initiate's Scepter",false,hero,"until end of round");
                if(HasEquipped0854(hero,"EQ068")&&EncounterUsed0854(hero,"EQ068","Harmony:FROST")&&card!=null&&(card.Rules??string.Empty).IndexOf("Cold",StringComparison.OrdinalIgnoreCase)>=0&&!enemy.Defeated)AddStatus(enemy,"Slowed",1,0,"Worldheart Staff — Frost",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ068")&&EncounterUsed0854(hero,"EQ068","Harmony:STORM")&&card!=null&&(card.Rules??string.Empty).IndexOf("Lightning",StringComparison.OrdinalIgnoreCase)>=0){var chain=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(chain!=null)DealDirectDamage(chain,1,"Worldheart Staff — Storm",hero);}
                if(HasEquipped0854(hero,"EQ059")&&UseEncounterOnce0854(hero,"EQ059","Crippling")&&!enemy.Defeated)AddStatus(enemy,"Rooted",1,0,"Dragonkiller Crossbow",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ060")&&hero.UsedTurnAbilities.Remove(Item0854Prefix+"EQ060:JudgmentReady")&&!enemy.Defeated)AddStatus(enemy,"No Reactions",1,0,"Heaven's Judgment",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ052")&&hero.UsedTurnAbilities.Remove(Item0854Prefix+"EQ052:VINE")&&!enemy.Defeated)AddStatus(enemy,"Rooted",1,0,"Bow of the World Tree",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ052")&&hero.UsedTurnAbilities.Remove(Item0854Prefix+"EQ052:BLOOM")){var ally=ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==enemy.LocationId).OrderBy(h=>h.Health).FirstOrDefault();if(ally!=null)HealHero(ally,2,"Bow of the World Tree Bloom",hero);}
                hero.UsedTurnAbilities.Remove(Item0854Prefix+"EQ052:THORN");hero.UsedTurnAbilities.Remove(Item0854Prefix+"EQ052:Mode");
            }
            if(defeated)
            {
                if(HasEquipped0854(hero,"EQ007")){hero.NextAttackBonus+=1;AddLog("ITEM TRIGGER — Blood Hunger grants +1 Attack to the next Attack.");}
                if(HasEquipped0854(hero,"EQ014")){hero.NextAttackBonus+=1;AddLog("ITEM TRIGGER — Cleaving Edge grants +1 Attack to the next Attack this turn.");}
                if(HasEquipped0854(hero,"EQ024")&&(EnemyTemplateContains0854(enemy,"Undead")||EnemyTemplateContains0854(enemy,"Demon"))&&UseEncounterOnce0854(hero,"EQ024","DivineResolve"))HealHero(hero,2,"Blessed Morningstar",hero);
                if(HasEquipped0854(hero,"EQ084"))AddStatus(hero,"Hidden",1,0,"Shadowleather Coat",true,hero,"until attack or engaged");
                if(HasEquipped0854(hero,"EQ101")&&IsGoblinEnemy(enemy)&&UseEncounterOnce0854(hero,"EQ101","TrophyHunter"))AddPartyGold(5,"Goblin Fang Talisman");
                if(HasEquipped0854(hero,"EQ118")&&!ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==enemy.LocationId))AddStatus(hero,"Hidden",1,0,"Nightglass Daggers",true,hero,"until attack or engaged");
                if(HasEquipped0854(hero,"EQ018")&&UseEncounterOnce0854(hero,"EQ018","ThunderCrash"))foreach(var e in Enemies.Values.Where(e=>!e.Defeated&&e.Id!=enemy.Id&&GraphDistance(enemy.LocationId,e.LocationId)<=1).ToList())AddStatus(e,"Staggered",1,0,"Stormcleaver",false,hero,"until end of next activation");
                if(HasEquipped0854(hero,"EQ044")&&UseEncounterOnce0854(hero,"EQ044","DeathsDance")){hero.UsedQuestCards.Add(Item0854Prefix+"EQ044:ExtraTurnPending");AddLog("ITEM TRIGGER — Night's Whisper arms Death's Dance: take another full turn after this turn ends.");}
            }
        }

        private int EquipmentDamageReduction0854(HeroState target,int amount,string source,EnemyState sourceEnemy)
        {
            if(target==null||amount<=0)return 0;int reduction=0;string s=source??string.Empty;
            if(HasEquipped0854(target,"EQ003")&&sourceEnemy!=null&&UseRoundOnce0854(target,"EQ003","Parry"))reduction++;
            if(HasEquipped0854(target,"EQ077")&&sourceEnemy!=null&&UseRoundOnce0854(target,"EQ077","QuickBlock"))reduction++;
            if(HasEquipped0854(target,"EQ081")&&UseRoundOnce0854(target,"EQ081","Padding")&&s.IndexOf("Fire",StringComparison.OrdinalIgnoreCase)<0&&s.IndexOf("Lightning",StringComparison.OrdinalIgnoreCase)<0&&s.IndexOf("Arcane",StringComparison.OrdinalIgnoreCase)<0)reduction++;
            if(HasEquipped0854(target,"EQ083")&&amount==1&&UseRoundOnce0854(target,"EQ083","Reinforced"))reduction++;
            if(HasEquipped0854(target,"EQ087")&&sourceEnemy!=null&&EnemyAttackRangeConnections0830(sourceEnemy)>0&&UseRoundOnce0854(target,"EQ087","Ranged"))reduction++;
            if(HasEquipped0854(target,"EQ090")&&s.IndexOf("Lightning",StringComparison.OrdinalIgnoreCase)>=0)reduction+=2;
            if(HasEquipped0854(target,"EQ094")&&s.IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0)reduction+=2;
            if(HasEquipped0854(target,"EQ096")&&(s.IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0||s.IndexOf("Lightning",StringComparison.OrdinalIgnoreCase)>=0||s.IndexOf("Arcane",StringComparison.OrdinalIgnoreCase)>=0||s.IndexOf("Magical",StringComparison.OrdinalIgnoreCase)>=0)&&UseRoundOnce0854(target,"EQ096","Ward"))reduction+=2;
            if(HasEquipped0854(target,"EQ135")&&(s.IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0||s.IndexOf("Lightning",StringComparison.OrdinalIgnoreCase)>=0||s.IndexOf("Arcane",StringComparison.OrdinalIgnoreCase)>=0||s.IndexOf("Magical",StringComparison.OrdinalIgnoreCase)>=0)&&UseRoundOnce0854(target,"EQ135","Spellguard"))reduction++;
            if(HasEquipped0854(target,"EQ137")&&(s.IndexOf("Hazard",StringComparison.OrdinalIgnoreCase)>=0||(sourceEnemy!=null&&EnemyTemplateContains0854(sourceEnemy,"Beast")))&&UseRoundOnce0854(target,"EQ137","Weathered"))reduction++;
            // allied aura / protection style reductions that require no modal target decision in the digital baseline
            if(sourceEnemy!=null&&ParticipatingHeroes.Any(h=>h.Id!=target.Id&&!h.IsDefeated&&h.LocationId==target.LocationId&&HasEquipped0854(h,"EQ078"))&&EnemyAttackRangeConnections0830(sourceEnemy)>0)reduction++;
            return Mathf.Clamp(reduction,0,amount);
        }

        private bool EquipmentPreventDefeat0854(HeroState hero)
        {
            if(hero==null)return false;
            foreach(var id in new[]{"EQ012","EQ050","EQ094"})if(HasEquipped0854(hero,id)&&UseEncounterOnce0854(hero,id,"DefeatFloor")){AddLog($"ITEM TRIGGER — {RewardContent0826.Equipment[id].Name} prevents {hero.DisplayName} from being Defeated; remain at 1 Health.");return true;}
            if(HasEquipped0854(hero,"EQ075")&&!hero.UsedQuestCards.Contains(Item0854Prefix+"EQ075:CampaignSpent"))
            {
                hero.UsedQuestCards.Add(Item0854Prefix+"EQ075:CampaignSpent");hero.StartingEquipment.RemoveAll(e=>e.Id=="EQ075");EquipmentDiscard.Add("EQ075");RecalculateHeroEquipmentStats(hero);
                foreach(var st in hero.Statuses.Where(x=>!x.Beneficial).ToList())RemoveStatus(hero.Statuses,st.Name,hero.DisplayName,"Wand of the Phoenix — Rebirth");
                hero.Health=Mathf.Max(1,Mathf.CeilToInt(hero.MaxHealth*0.5f));hero.UsedQuestCards.Add(Item0854Prefix+"DefeatRestoreNow");AddLog($"ITEM TRIGGER — Wand of the Phoenix is discarded; {hero.DisplayName} returns at half Health and removes all negative Conditions.");return true;
            }
            if(HeroCarries0854(hero,"LT157")&&!hero.UsedQuestCards.Contains(Item0854Prefix+"LT157:Spent")){hero.UsedQuestCards.Add(Item0854Prefix+"LT157:Spent");hero.Health=hero.MaxHealth;hero.UsedQuestCards.Add(Item0854Prefix+"DefeatRestoreNow");AddLog($"LEGENDARY TREASURE — Phoenix Heart prevents {hero.DisplayName}'s Defeat and restores full Health.");return true;}
            return false;
        }

        private void OnEquipmentHeroDamaged0854(HeroState target,int dealt,string source,EnemyState sourceEnemy)
        {
            if(target==null||dealt<=0)return;
            if(HasEquipped0854(target,"EQ090")&&sourceEnemy!=null&&UseRoundOnce0854(target,"EQ090","Reprisal"))DealDirectDamage(sourceEnemy,1,"Stormwoven Mail Static Reprisal",target);
            if(HasEquipped0854(target,"EQ108")&&UseEncounterOnce0854(target,"EQ108","Vanish")){AddStatus(target,"Hidden",1,0,"Cloak of Vanishing",true,target,"until attack or engaged");target.FreeMoveReady=true;target.FreeMoveIgnoresEngagement=true;AddLog("ITEM TRIGGER — Cloak of Vanishing grants Hidden and a free connected Move.");}
            if(HasEquipped0854(target,"EQ119")&&target.Health*2<=target.MaxHealth&&UseEncounterOnce0854(target,"EQ119","LastEmber")){target.NextAttackBonus+=3;AddLog("ITEM TRIGGER — Last Ember grants +3 Attack to the next Attack.");}
        }

        private int EquipmentHealingBonus0854(HeroState sourceHero,HeroState target,int baseAmount)
        {
            if(sourceHero==null||target==null||baseAmount<=0)return 0;int bonus=0;
            if(sourceHero.Id!=target.Id&&HasEquipped0854(sourceHero,"EQ065"))bonus+=2;
            if(HasEquipped0854(sourceHero,"EQ068")&&EncounterUsed0854(sourceHero,"EQ068","Harmony:LIFE"))bonus+=2;
            return bonus;
        }

        private void OnEquipmentHealingResolved0854(HeroState sourceHero,HeroState target,int healed)
        {
            if(sourceHero==null||target==null||healed<=0)return;
            if(sourceHero.Id!=target.Id&&HasEquipped0854(sourceHero,"EQ142")&&UseRoundOnce0854(sourceHero,"EQ142","Prayer"))AddStatus(target,"Guarded",1,0,"Pilgrim's Holy Symbol",true,sourceHero,"until first damage source this round");
        }

        private void OnEquipmentMoveComplete0854(HeroState hero,string priorLocation)
        {
            if(hero==null)return;
            if(HasEquipped0854(hero,"EQ107")&&EnemiesHere0854(hero)>0)hero.UsedTurnAbilities.Add(Item0854Prefix+"VanguardReady");
            if(HasEquipped0854(hero,"EQ143"))hero.UsedTurnAbilities.Add(Item0854Prefix+"TempoReady");
            if(HasEquipped0854(hero,"EQ105")&&LocationHasTag(hero.LocationId,"Consecrated")&&UseRoundOnce0854(hero,"EQ105","Rosary"))HealHero(hero,1,"Consecrated Rosary",hero);
        }

        private void OnEquipmentTurnStart0854(HeroState hero)
        {
            if(hero==null)return;
            if(hero.HasStatus("Hidden"))hero.UsedTurnAbilities.Add(Item0854Prefix+"BeganHidden");
            if(HasEquipped0854(hero,"EQ066")&&(LocationTextContains0854(hero.LocationId,"Forest")||LocationTextContains0854(hero.LocationId,"Natural")||LocationTextContains0854(hero.LocationId,"Meadow")))HealHero(hero,1,"Staff of the Ancient Grove",hero);
            if(HasEquipped0854(hero,"EQ034")&&EnemiesHere0854(hero)==0)hero.UsedTurnAbilities.Add(Item0854Prefix+"FlowingArmor");
            if(HasEquipped0854(hero,"EQ028")&&EncounterUsed0854(hero,"EQ028","MasterCraft:SWIFT")){hero.FreeMoveReady=true;hero.FreeMoveIgnoresDifficultTerrain=true;}
        }

        private bool IsArcaneCard0854(CardData card)=>card!=null&&(((card.Traits??string.Empty).IndexOf("Arcane",StringComparison.OrdinalIgnoreCase)>=0)||((card.Traits??string.Empty).IndexOf("Spell",StringComparison.OrdinalIgnoreCase)>=0));

        private int EquipmentCardCostModifier0854(HeroState hero,CardData card,int cost)
        {
            if(hero==null||card==null||cost<=0||!IsArcaneCard0854(card))return cost;
            if(HasEquipped0854(hero,"EQ061")&&!EncounterUsed0854(hero,"EQ061","FirstArcane"))cost--;
            if(HasEquipped0854(hero,"EQ067")&&!hero.UsedRoundAbilities.Contains(RoundUseKey0854("EQ067","ArcaneMastery")))cost--;
            if(HasEquipped0854(hero,"EQ074")&&!EncounterUsed0854(hero,"EQ074","Accelerate"))cost=0;
            return Mathf.Max(0,cost);
        }

        private void OnEquipmentCardPlayed0854(HeroState hero,CardData card,int paidCost)
        {
            if(hero==null||card==null)return;
            if(IsArcaneCard0854(card))
            {
                if(HasEquipped0854(hero,"EQ061")&&!EncounterUsed0854(hero,"EQ061","FirstArcane"))UseEncounterOnce0854(hero,"EQ061","FirstArcane");
                if(HasEquipped0854(hero,"EQ067")&&!hero.UsedRoundAbilities.Contains(RoundUseKey0854("EQ067","ArcaneMastery")))UseRoundOnce0854(hero,"EQ067","ArcaneMastery");
                if(HasEquipped0854(hero,"EQ074")&&!EncounterUsed0854(hero,"EQ074","Accelerate"))UseEncounterOnce0854(hero,"EQ074","Accelerate");
            }
            if(HasEquipped0854(hero,"EQ110")&&card.Cost>=2&&UseRoundOnce0854(hero,"EQ110","AscendantWill")){hero.Actions=Mathf.Min(3,hero.Actions+1);AddLog("ITEM TRIGGER — Relic of Ascendance recovers 1 Action after a 2-Action Hero card.");}
        }

        public int EffectiveBasicAttackRange0854(HeroState hero){if(hero==null)return 0;int baseRange=Mathf.Max(hero.NextAttackRangeConnections,EquipmentAttackRange0854(hero));return baseRange+LocationRangeBonus0858(hero,baseRange);}

        private int EquipmentAttackRange0854(HeroState hero)
        {
            if(hero==null)return 0;int range=0;
            foreach(var eq in hero.StartingEquipment)
            {
                if(!RewardContent0826.Equipment.TryGetValue(eq.Id??string.Empty,out var def))continue;
                range=Mathf.Max(range,Mathf.Max(0,def.Range));
                // The current authored Equipment sheet stores Bow/Crossbow range in the weapon
                // family/text rather than the numeric Range column. One connection is the digital
                // tabletop baseline; Bow of the World Tree explicitly adds +1 Range.
                if(string.Equals(def.Family,"Bow",StringComparison.OrdinalIgnoreCase)||string.Equals(def.Family,"Crossbow",StringComparison.OrdinalIgnoreCase))range=Mathf.Max(range,1);
                if(string.Equals(def.Id,"EQ052",StringComparison.OrdinalIgnoreCase))range=Mathf.Max(range,2);
                if(((def.Ability1??string.Empty)+" "+(def.Ability2??string.Empty)).IndexOf("connected Location",StringComparison.OrdinalIgnoreCase)>=0&&string.Equals(def.Type,"Weapon",StringComparison.OrdinalIgnoreCase))range=Mathf.Max(range,1);
            }
            return range;
        }

        private void OnLootOrEquipmentQuestStart0854()
        {
            foreach(var hero in ParticipatingHeroes)
            {
                // SetParty and StartQuest can both participate in initial setup. Keep Quest-start
                // Loot effects idempotent so a reconnect/restart cannot draw the reward twice.
                string shardKey=ScenarioUseKey0854("LT149","QuestStart");
                if(HeroCarries0854(hero,"LT149")&&!hero.UsedQuestCards.Contains(shardKey))
                {
                    hero.UsedQuestCards.Add(shardKey);DrawCards(hero,1);
                    AddLog($"RELIC — Shard of the Ascendant lets {hero.DisplayName} draw 1 Hero card at Quest start.");
                }
            }
        }

        public bool DebugGiveLoot0854(string heroId,string lootId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!RewardContent0826.Loot.ContainsKey(lootId??string.Empty))return false;h.LootInventory.Add(lootId);OnLootAcquired0854(h,lootId,"0.8.54 validator");return true;
        }
        public bool DebugEquipCatalog0854(string heroId,string equipmentId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!RewardContent0826.Equipment.ContainsKey(equipmentId??string.Empty))return false;
            h.StartingEquipment.RemoveAll(e=>string.Equals(e.Id,equipmentId,StringComparison.OrdinalIgnoreCase));var runtime=RuntimeEquipmentFromCatalog(equipmentId);if(runtime==null)return false;h.StartingEquipment.Add(runtime);RecalculateHeroEquipmentStats(h);return true;
        }
        public bool DebugActivateEquipment0854(string heroId,string equipmentId,string arg="SELF",string targetId=null)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&ActivateEquipment0854(h,equipmentId,arg,targetId);
        public bool DebugUseRelic0854(string heroId,string lootId,string targetId=null)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&UseRelic0854(h,lootId,targetId);
        public bool DebugUseLoot0854(string heroId,string lootId,string targetKind="SELF",string targetId=null,string option=null)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&UseLoot0854(h,lootId,targetKind,targetId,option);
        public int DebugEquipmentAttackModifier0854(string heroId,string enemyId,bool ranged=false)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&Enemies.TryGetValue(enemyId??string.Empty,out var e)?EquipmentAttackModifier0854(h,e,null,ranged):0;
        public int DebugEquipmentArmorModifier0854(string heroId)=>Heroes.TryGetValue(heroId??string.Empty,out var h)?EquipmentArmorModifier0854(h):0;
        public int DebugEquipmentArmorIgnore0854(string heroId,string enemyId)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&Enemies.TryGetValue(enemyId??string.Empty,out var e)?EquipmentArmorIgnore0854(h,e,null):0;
        public void DebugRecalculateEquipment0854(string heroId){if(Heroes.TryGetValue(heroId??string.Empty,out var h))RecalculateHeroEquipmentStats(h);}
    }
}
