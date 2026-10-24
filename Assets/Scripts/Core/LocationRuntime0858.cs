using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    // 0.8.58 — catalog-driven execution for the full authored L000-L235 Location set.
    // The runtime intentionally executes only rules whose trigger/result is stated by the authored row.
    // Missing dice tables / future resource systems are logged as authored no-ops rather than invented.
    public sealed partial class AscendanceGameManager
    {
        private const string Location0858Prefix="LOC0858:";
        public readonly List<string> RegionalEncounterDeck0858=new();
        public readonly List<string> RegionalEncounterDiscard0858=new();
        public string RegionalEncounterDeckKey0858;
        private string DeferredLocationPostSearchLocationId0858;
        private string DeferredLocationPostSearchHeroId0858;

        public bool IsLocationExecutable0858(string authoredId)
        {
            if(!FullContentCatalog0850.TryGetLocation(authoredId,out var d)||d==null)return false;
            return !string.IsNullOrWhiteSpace(d.revealEffect)&&!string.IsNullOrWhiteSpace(d.firstVisitEffect)
                &&!string.IsNullOrWhiteSpace(d.ongoingEffect)&&!string.IsNullOrWhiteSpace(d.search);
        }

        private FullLocation0850 LocationDef0858(LocationState location)
        {
            if(location==null)return null;
            string id=string.IsNullOrWhiteSpace(location.AuthoredLocationId)?location.Id:location.AuthoredLocationId;
            return FullContentCatalog0850.TryGetLocation(id,out var d)?d:null;
        }

        private bool HasLocationKey0858(LocationState location,string key)
            => location!=null&&location.UsedKeys0858.Contains(key??string.Empty,StringComparer.OrdinalIgnoreCase);
        private bool UseLocationKey0858(LocationState location,string key)
        {
            if(location==null||string.IsNullOrWhiteSpace(key)||HasLocationKey0858(location,key))return false;
            location.UsedKeys0858.Add(key);return true;
        }
        private static string LocationTokenTag0858(string kind)=>Location0858Prefix+"TOKEN:"+(kind??string.Empty).ToUpperInvariant();
        private bool HasLocationToken0858(LocationState location,string kind)=>location!=null&&location.TemporaryTags.Any(t=>string.Equals(t.Tag,LocationTokenTag0858(kind),StringComparison.OrdinalIgnoreCase));
        private void PlaceLocationToken0858(LocationState location,string kind,string source)
        {
            if(location==null||HasLocationToken0858(location,kind))return;location.TemporaryTags.Add(new LocationTagInstance{Tag=LocationTokenTag0858(kind),Source=source,ExpiryTiming=StatusExpiryTiming.Persistent});AddLog($"LOCATION MARKER — {kind} placed at {location.Name} ({source}).");
        }
        private void RemoveLocationToken0858(LocationState location,string kind)
        {if(location!=null)location.TemporaryTags.RemoveAll(t=>string.Equals(t.Tag,LocationTokenTag0858(kind),StringComparison.OrdinalIgnoreCase));}

        private static int FirstNumber0858(string text,int fallback=1)
        {
            var m=Regex.Match(text??string.Empty,@"(?<![A-Za-z])(?<!\d)(\d+)(?!\d)");
            return m.Success&&int.TryParse(m.Groups[1].Value,out var n)?n:fallback;
        }
        private static bool Has0858(string text,string token)=>!string.IsNullOrWhiteSpace(text)&&text.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0;
        private static int SignedModifier0858(string text,string noun)
        {
            if(string.IsNullOrWhiteSpace(text))return 0;
            var m=Regex.Match(text,$@"([+-]\s*\d+)\s+{Regex.Escape(noun)}",RegexOptions.IgnoreCase);
            if(!m.Success)return 0;
            return int.TryParse(m.Groups[1].Value.Replace(" ",string.Empty),out var n)?n:0;
        }

        private bool LocationConditionActive0858(string text)
        {
            if(string.IsNullOrWhiteSpace(text))return true;
            if(Has0858(text,"Sunfall 6+")&&Sunfall0857<6)return false;
            if(Has0858(text,"Sunfall 0–5")&&Sunfall0857>5)return false;
            if(Has0858(text,"Sunfall 0-5")&&Sunfall0857>5)return false;
            if(Has0858(text,"Blight 6+")&&Blight0857<6)return false;
            if(Has0858(text,"Doomtide 6+")&&Doomtide0857<6)return false;
            if(Has0858(text,"Instability 6+")&&Instability0857<6)return false;
            return true;
        }


        private bool TryAdvanceQuestNamedGoal0858(string token,int amount,string source)
        {
            if(QuestRuntime0856==null||string.IsNullOrWhiteSpace(token))return false;
            var g=QuestRuntime0856.Goals.FirstOrDefault(x=>!x.Complete&&x.Label.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0);
            if(g==null)return false;AdvanceGoal0856(g,Mathf.Max(1,amount),source);return true;
        }
        private void GainSupply0858(int amount,string source)
        {
            int n=Mathf.Max(0,amount);if(n<=0)return;NextQuestSupply0829+=n;AddLog($"LOCATION RESOURCE — party gains {n} Supply ({source}). Banked Supply: {NextQuestSupply0829}.");
        }
        private void DrawSearchFromLocation0858(LocationState location,HeroState hero,int count=1)
        {
            if(location==null||hero==null)return;
            if(count<=1){ResolveSearchDraw(location,hero);return;}
            var ids=new List<string>();for(int i=0;i<count;i++){var c=DrawEligibleSearchCard(location);if(c!=null)ids.Add(c.Id);}
            if(ids.Count==0){AddLog($"LOCATION SEARCH — {location.Name}: no eligible Search card remains.");return;}
            if(ids.Count==1){var c=SearchEncounterContent0820.SearchCards[ids[0]];ResolveSearchCard(c,location,hero);SearchDiscard.Add(c.Id);return;}
            BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationSearchKeepOne0858","Choose 1 Search card to resolve; discard the other.",ids);
        }

        private bool IsLegacyRevealHandled0858(LocationState location)
        {
            string id=location?.AuthoredLocationId??string.Empty;
            return id=="L000"||id=="L001"||id=="L002"||id=="L004"||id=="L005";
        }

        private void ResolveLocationReveal0858(LocationState location,HeroState hero)
        {
            if(location==null||hero==null||IsLegacyRevealHandled0858(location))return;
            var d=LocationDef0858(location);string text=d?.revealEffect??location.RevealEffect??string.Empty;
            if(string.IsNullOrWhiteSpace(text)||Has0858(text,"None"))return;
            string source=$"{location.Name} Reveal";

            // Explicit choices first so optional authored benefits remain player-controlled.
            var healOrCleanse=Regex.Match(text,@"recover\s+(\d+)\s+Health\s+or\s+remove\s+(?:1|one)\s+(?:negative\s+)?Condition",RegexOptions.IgnoreCase);
            if(healOrCleanse.Success)
            {
                BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationHealOrCleanse0858:{healOrCleanse.Groups[1].Value}","Choose the authored Reveal benefit: recover Health or remove 1 negative Condition.");return;
            }
            if(Regex.IsMatch(text,@"chooses?\s+\+1\s+Attack\s+or\s+\+1\s+Armor",RegexOptions.IgnoreCase))
            {
                BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationAttackOrArmor0858","Choose +1 Attack or +1 Armor until the end of the round.");return;
            }
            var goldOrSearch=Regex.Match(text,@"Gain\s+(\d+)\s+Gold\s+or\s+draw\s+1\s+Search",RegexOptions.IgnoreCase);
            if(goldOrSearch.Success)
            {
                BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationGoldOrSearch0858:{goldOrSearch.Groups[1].Value}","Choose the authored Reveal benefit: Gold or draw 1 Search card.",new[]{location.Id});return;
            }
            var goldOrEvidence=Regex.Match(text,@"Gain\s+(\d+)\s+Gold\s+or\s+(?:place|gain)\s+1\s+Evidence",RegexOptions.IgnoreCase);
            if(goldOrEvidence.Success&&QuestRuntime0856!=null&&QuestRuntime0856.Goals.Any(g=>!g.Complete&&g.Label.IndexOf("Evidence",StringComparison.OrdinalIgnoreCase)>=0)){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationGoldOrEvidence0858:{goldOrEvidence.Groups[1].Value}","Choose Gold or advance the active Evidence objective.");return;}
            if(Regex.IsMatch(text,@"Gain\s+1\s+Evidence\s+or\s+inspect\s+the\s+top\s+Encounter",RegexOptions.IgnoreCase)){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationEvidenceOrEncounter0858","Choose Evidence or inspect the top Encounter card.");return;}
            var evidenceOrGold=Regex.Match(text,@"gain\s+1\s+Evidence\s+or\s+(\d+)\s+Gold",RegexOptions.IgnoreCase);
            if(evidenceOrGold.Success){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationEvidenceOrGold0858:{evidenceOrGold.Groups[1].Value}","Choose Evidence or Gold.");return;}
            if(Regex.IsMatch(text,@"Gain\s+1\s+Evidence/Lore[^.;]*or\s+draw\s+2\s+Search",RegexOptions.IgnoreCase)){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationEvidenceOrSearch20858","Choose the authored objective clue or draw 2 Search cards and choose 1.",new[]{location.Id});return;}
            if(Regex.IsMatch(text,@"Gain\s+1\s+Supply\s+or\s+inspect\s+the\s+top\s+Encounter",RegexOptions.IgnoreCase)){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationSupplyOrEncounter0858","Choose 1 Supply or inspect the top Encounter card.");return;}
            var goldOrItem=Regex.Match(text,@"Gain\s+(\d+)\s+Gold\s+or\s+1\s+Item",RegexOptions.IgnoreCase);
            if(goldOrItem.Success){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationGoldOrItem0858:{goldOrItem.Groups[1].Value}","Choose Gold or 1 Item.");return;}
            if(Regex.IsMatch(text,@"Reveal\s+one\s+Hidden\s+enemy\s+or\s+inspect\s+the\s+top\s+Encounter",RegexOptions.IgnoreCase)){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationRevealHiddenOrEncounter0858","Choose: reveal one Hidden Enemy or inspect the top Encounter card.");return;}
            if(Regex.IsMatch(text,@"suffer\s+1\s+Damage\s+or\s+end\s+their\s+movement",RegexOptions.IgnoreCase))
            {
                BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationDamageOrStop0858","Choose: suffer 1 Damage, or end movement for this turn.");return;
            }
            var supplyOrGold=Regex.Match(text,@"Gain\s+(\d+)\s+Supply\s+or\s+(\d+)\s+Gold",RegexOptions.IgnoreCase);
            if(supplyOrGold.Success){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationSupplyOrGold0858:{supplyOrGold.Groups[1].Value}:{supplyOrGold.Groups[2].Value}","Choose Supply or Gold.");return;}
            var goldOrSupply=Regex.Match(text,@"Gain\s+(\d+)\s+Gold\s+or\s+(\d+)\s+Supply",RegexOptions.IgnoreCase);
            if(goldOrSupply.Success){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationSupplyOrGold0858:{goldOrSupply.Groups[2].Value}:{goldOrSupply.Groups[1].Value}","Choose Supply or Gold.");return;}
            var supplyOrHeal=Regex.Match(text,@"Gain\s+(\d+)\s+Supply\s+or\s+recover\s+(\d+)\s+Health",RegexOptions.IgnoreCase);
            if(supplyOrHeal.Success){BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,$"LocationSupplyOrHeal0858:{supplyOrHeal.Groups[1].Value}:{supplyOrHeal.Groups[2].Value}","Choose Supply or Health recovery.");return;}
            if(Regex.IsMatch(text,@"remove\s+1\s+harmful\s+Condition\s+or\s+draw\s+1\s+Search",RegexOptions.IgnoreCase))
            {BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationCleanseOrSearch0858","Choose: remove 1 harmful Condition or draw 1 Search card.",new[]{location.Id});return;}
            if(Regex.IsMatch(text,@"Gain\s+1\s+Item\s+or\s+remove\s+1\s+harmful\s+Condition",RegexOptions.IgnoreCase))
            {BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationItemOrCleanse0858","Choose: gain 1 Item or remove 1 harmful Condition.");return;}
            if(Regex.IsMatch(text,@"discard\s+1\s+card[^.;]*if\s+they\s+do\s+not[^.;]*suffer\s+1\s+damage",RegexOptions.IgnoreCase))
            {BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,Has0858(text,"gain 1 Loot")?"LocationDiscardOrDamageForLoot0858":"LocationDiscardOrDamage0858","Choose: discard 1 Hero card, or suffer 1 damage.");return;}

            bool did=false;
            var recover=Regex.Match(text,@"recover\s+(\d+)\s+Health",RegexOptions.IgnoreCase);
            if(recover.Success&&int.TryParse(recover.Groups[1].Value,out int heal))
            {
                if(Has0858(text,"Each Hero here"))foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(h,heal,source,hero);
                else HealHero(hero,heal,source,hero);did=true;
            }
            var gold=Regex.Match(text,@"gains?\s+(\d+)\s+Gold",RegexOptions.IgnoreCase);
            if(gold.Success&&int.TryParse(gold.Groups[1].Value,out int gp)){AddPartyGold(gp,source);did=true;}
            var supply=Regex.Match(text,@"Gain\s+(\d+)\s+Supply",RegexOptions.IgnoreCase);if(supply.Success&&int.TryParse(supply.Groups[1].Value,out int sp)){GainSupply0858(sp,source);did=true;}
            if(Regex.IsMatch(text,@"(?:gain|place)\s+1\s+Evidence",RegexOptions.IgnoreCase)){if(TryAdvanceQuestNamedGoal0858("Evidence",1,source))did=true;else AddLog($"LOCATION REVEAL — {location.Name}: Evidence is not used by the active Quest; no Evidence state is invented.");}
            if(Regex.IsMatch(text,@"gain\s+1\s+(?:Lore|clue)",RegexOptions.IgnoreCase)){if(TryAdvanceQuestNamedGoal0858("Lore",1,source)||TryAdvanceQuestNamedGoal0858("Evidence",1,source))did=true;else AddLog($"LOCATION REVEAL — {location.Name}: the active Quest has no matching Lore/Evidence goal.");}
            if(Regex.IsMatch(text,@"Draw\s+1\s+(?:source-matched\s+)?Loot",RegexOptions.IgnoreCase)||Regex.IsMatch(text,@"gain\s+1\s+Item",RegexOptions.IgnoreCase)){DrawLootToHero(hero,source);did=true;}
            if(Regex.IsMatch(text,@"Draw\s+2\s+Search(?:\s+cards?)?\s*[,;:—-]?\s*(?:and\s+)?(?:choose|keep)\s+1",RegexOptions.IgnoreCase)){DrawSearchFromLocation0858(location,hero,2);did=true;}
            else if(Regex.IsMatch(text,@"Draw\s+1\s+Search\s+card",RegexOptions.IgnoreCase)){DrawSearchFromLocation0858(location,hero,1);did=true;}
            if(Regex.IsMatch(text,@"immediately\s+Search(?:\s+this\s+Location)?\s+without\s+spending\s+an\s+Action",RegexOptions.IgnoreCase))
            { hero.FreeSearchLocationId=location.Id;AddLog($"LOCATION REVEAL — {hero.DisplayName} gains an immediate free Search at {location.Name}.");did=true; }
            if(Regex.IsMatch(text,@"immediately\s+Move\s+1\s+Location\s+without\s+spending\s+an\s+Action",RegexOptions.IgnoreCase))
            { hero.FreeMoveReady=true;AddLog($"LOCATION REVEAL — {hero.DisplayName} gains one free connected Move from {location.Name}.");did=true; }
            if(Has0858(text,"may Hide")){AddStatus(hero,"Hidden",1,0,source,true,hero,"until the start of your next turn");did=true;}
            if(Has0858(text,"gains Inspired")){AddStatus(hero,"Inspired",0,1,source,true,hero,"until the end of the round");did=true;}
            if(Has0858(text,"gains Guarded")){AddStatus(hero,"Guarded",0,1,source,true,hero,"until the end of the round");did=true;}
            if(Regex.IsMatch(text,@"remove\s+1\s+(?:(?:negative|harmful)\s+)?Condition",RegexOptions.IgnoreCase)){RemoveFirstHarmfulStatus(hero,source);did=true;}

            var nextAttack=Regex.Match(text,@"next\s+Attack[^.]*?\+(\d+)\s+Attack",RegexOptions.IgnoreCase);
            if(nextAttack.Success&&int.TryParse(nextAttack.Groups[1].Value,out int nab)){hero.NextAttackBonus+=nab;AddLog($"LOCATION REVEAL — {hero.DisplayName}'s next Attack gains +{nab} Attack ({location.Name}).");did=true;}
            else
            {
                var attack=Regex.Match(text,@"gains?\s+\+(\d+)\s+Attack\s+until\s+the\s+end\s+of\s+the\s+round",RegexOptions.IgnoreCase);
                if(attack.Success&&int.TryParse(attack.Groups[1].Value,out int ab)){AddStatus(hero,$"Location Attack +{ab}",0,1,source,true,hero,"until the end of the round");did=true;}
            }
            var armor=Regex.Match(text,@"gains?\s+\+(\d+)\s+Armor",RegexOptions.IgnoreCase);
            if(armor.Success&&int.TryParse(armor.Groups[1].Value,out int ar)){AddStatus(hero,$"Location Armor +{ar}",1,0,source,true,hero,"until the start of your next turn");did=true;}
            if(Regex.IsMatch(text,@"next\s+(?:voluntary\s+)?Move\s+Action[^.]*costs\s+1\s+additional\s+Action",RegexOptions.IgnoreCase))
            {AddStatus(hero,"Location Move Surcharge",0,0,source,false,hero,"until consumed");did=true;}
            if(Regex.IsMatch(text,@"cannot\s+recover\s+Health\s+until\s+the\s+end\s+of\s+the\s+round",RegexOptions.IgnoreCase)){AddStatus(hero,"No Healing",0,1,source,false,hero,"until the end of the round");did=true;}
            if(Regex.IsMatch(text,@"cannot\s+perform\s+a\s+free\s+Move\s+Action",RegexOptions.IgnoreCase)){hero.FreeMoveReady=false;AddStatus(hero,"No Free Move",0,0,source,false,hero,"until end of turn");did=true;}
            if(Has0858(text,"place 1 Cave-In Token here")&&(!Has0858(text,"During WE009")||string.Equals(CurrentWorldEventId0833,"WE009",StringComparison.OrdinalIgnoreCase))&&Instability0857>=6){PlaceLocationToken0858(location,"Cave-In",source);did=true;}
            var directDamage=Regex.Match(text,@"suffers?\s+(\d+)\s+(?:Fire\s+)?Damage",RegexOptions.IgnoreCase);if(directDamage.Success&&int.TryParse(directDamage.Groups[1].Value,out int dd)){ApplyHeroDamageNow(hero,dd,source);did=true;}
            var range=Regex.Match(text,@"gains?\s+\+(\d+)\s+Range",RegexOptions.IgnoreCase);if(range.Success&&int.TryParse(range.Groups[1].Value,out int rb)){AddStatus(hero,$"Location Range +{rb}",0,1,source,true,hero,"until the end of the round");did=true;}
            if(Regex.IsMatch(text,@"may\s+gain\s+Hidden",RegexOptions.IgnoreCase)){AddStatus(hero,"Hidden",1,0,source,true,hero,"until you attack or leave this Location");did=true;}
            if(Has0858(text,"record Valewatch Discovered")){AddQuestCampaignFlag0856("Valewatch Discovered");did=true;}

            if(Has0858(text,"Look at 1 unrevealed")||Has0858(text,"Look at one unrevealed")){var peek=ExplorationDeck0828.FirstOrDefault();AddLog(peek==null?$"LOCATION SCOUT — {location.Name}: no unrevealed Location is available to inspect.":$"LOCATION SCOUT — {location.Name} inspects {ExplorationLocationLabel0828(peek)} without revealing it.");did=true;}
            bool inspectLocationTop=Has0858(text,"Look at the top Location Tile")||Has0858(text,"Read the Route")||Has0858(text,"Inspect the top Location Tile")||Has0858(text,"top Location Tile");
            bool inspectLocationTwo=Regex.IsMatch(text,@"Inspect\s+the\s+top\s+2\s+Location",RegexOptions.IgnoreCase)||Has0858(text,"top 2 Location Tiles");
            bool inspectEncounter=Has0858(text,"top Encounter card")||Has0858(text,"top 2 Encounter cards");
            if(inspectLocationTop&&inspectEncounter&&Has0858(text,"each to the top or bottom")){BeginLocationDualDeckInspect0858(location,hero);did=true;}
            else
            {
                if(inspectLocationTop){BeginLocationExplorationInspect0858(location,hero,1,true);did=true;}
                else if(inspectLocationTwo){BeginLocationExplorationInspect0858(location,hero,2,false);did=true;}
                if(inspectEncounter){BeginLocationEncounterInspect0858(location,hero,Has0858(text,"top 2")?2:1);did=true;}
            }

            if(!did)AddLog($"LOCATION REVEAL — {location.Name} resolves its authored narrative/quest-facing effect with no additional standalone state change: {text}");
        }

        private List<string> ActiveEncounterDeckForLocation0858(LocationState location)
        {
            if(RegionalEncounterStart0858(location)==1)return EncounterDeck;
            EnsureRegionalEncounterDeck0858(location);return RegionalEncounterDeck0858;
        }
        private List<string> ActiveEncounterDiscardForLocation0858(LocationState location)
        {
            if(RegionalEncounterStart0858(location)==1)return EncounterDiscard;
            EnsureRegionalEncounterDeck0858(location);return RegionalEncounterDiscard0858;
        }
        private void BeginLocationExplorationInspect0858(LocationState location,HeroState hero,int count,bool topOrBottom)
        {
            var ids=ExplorationDeck0828.Take(Mathf.Max(1,count)).ToList();if(ids.Count==0){AddLog($"LOCATION SCOUT — {location.Name}: no Exploration cards remain to inspect.");return;}
            string mode=topOrBottom?"LocationExploreTopBottom0858":"LocationExploreReorder0858";
            string prompt=topOrBottom?$"Inspect {ExplorationLocationLabel0828(ids[0])}; leave it on top or place it on the bottom.":$"Inspect and reorder: {string.Join(" | ",ids.Select(ExplorationLocationLabel0828))}.";
            BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,mode,prompt,ids);
        }
        private void BeginLocationEncounterInspect0858(LocationState location,HeroState hero,int count)
        {
            var deck=ActiveEncounterDeckForLocation0858(location);var ids=deck.Take(Mathf.Max(1,count)).ToList();if(ids.Count==0){AddLog($"LOCATION SCOUT — {location.Name}: no Encounter cards remain to inspect.");return;}
            BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,count>1?"LocationEncounterReorder0858":"LocationEncounterTopBottom0858",$"Inspect: {string.Join(" | ",ids.Select(EncounterCardLabel))}.",ids);
        }
        private void BeginLocationEncounterDiscardInspect0858(LocationState location,HeroState hero,int count)
        {
            var deck=ActiveEncounterDeckForLocation0858(location);var ids=deck.Take(Mathf.Max(1,count)).ToList();if(ids.Count==0){AddLog($"LOCATION SCOUT — {location.Name}: no Encounter cards remain to inspect.");return;}
            BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,count==1?"LocationEncounterKeepDiscard0858":"LocationEncounterDiscardOne0858",$"Inspect: {string.Join(" | ",ids.Select(EncounterCardLabel))}. Choose the authored discard/return result.",ids);
        }
        private void BeginLocationDualDeckInspect0858(LocationState location,HeroState hero)
        {
            string loc=ExplorationDeck0828.FirstOrDefault();var deck=ActiveEncounterDeckForLocation0858(location);string enc=deck.FirstOrDefault();
            if(string.IsNullOrEmpty(loc)&&string.IsNullOrEmpty(enc)){AddLog($"LOCATION SCOUT — {location.Name}: both inspected decks are empty.");return;}
            if(string.IsNullOrEmpty(enc)){BeginLocationExplorationInspect0858(location,hero,1,true);return;}
            if(string.IsNullOrEmpty(loc)){BeginLocationEncounterInspect0858(location,hero,1);return;}
            BeginRewardChoice0827(location.AuthoredLocationId,location.Name,hero,"LocationDualDeckTopBottom0858",$"Inspect {ExplorationLocationLabel0828(loc)} and {EncounterCardLabel(enc)}; return each to the top or bottom.",new[]{loc,enc});
        }

        private void Append0858LocationChoiceOptions(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(state==null||string.IsNullOrEmpty(state.Mode))return;var hero=RewardHeroFor(state.HeroId);var ids=state.RevealedCardIds??new List<string>();
            if(state.Mode.StartsWith("LocationHealOrCleanse0858:",StringComparison.OrdinalIgnoreCase)){int n=FirstNumber0858(state.Mode,1);options.Add(new RewardChoiceOption{Key="HEAL",Label=$"Recover {n} Health"});options.Add(new RewardChoiceOption{Key="CLEANSE",Label="Remove 1 negative Condition"});}
            else if(state.Mode=="LocationAttackOrArmor0858"){options.Add(new RewardChoiceOption{Key="ATTACK",Label="Gain +1 Attack this round"});options.Add(new RewardChoiceOption{Key="ARMOR",Label="Gain +1 Armor this round"});}
            else if(state.Mode.StartsWith("LocationGoldOrSearch0858:",StringComparison.OrdinalIgnoreCase)){int n=FirstNumber0858(state.Mode,10);options.Add(new RewardChoiceOption{Key="GOLD",Label=$"Gain {n} Gold"});options.Add(new RewardChoiceOption{Key="SEARCH",Label="Draw 1 Search card"});}
            else if(state.Mode=="LocationDamageOrStop0858"){options.Add(new RewardChoiceOption{Key="DAMAGE",Label="Suffer 1 Damage"});options.Add(new RewardChoiceOption{Key="STOP",Label="End movement for this turn"});}
            else if(state.Mode.StartsWith("LocationGoldOrEvidence0858:",StringComparison.OrdinalIgnoreCase)){int n=FirstNumber0858(state.Mode,10);options.Add(new RewardChoiceOption{Key="GOLD",Label=$"Gain {n} Gold"});options.Add(new RewardChoiceOption{Key="EVIDENCE",Label="Gain 1 Evidence"});}
            else if(state.Mode=="LocationEvidenceOrEncounter0858"){options.Add(new RewardChoiceOption{Key="EVIDENCE",Label="Gain 1 Evidence"});options.Add(new RewardChoiceOption{Key="ENCOUNTER",Label="Inspect top Encounter card"});}
            else if(state.Mode.StartsWith("LocationEvidenceOrGold0858:",StringComparison.OrdinalIgnoreCase)){int n=FirstNumber0858(state.Mode,10);options.Add(new RewardChoiceOption{Key="EVIDENCE",Label="Gain 1 Evidence"});options.Add(new RewardChoiceOption{Key="GOLD",Label=$"Gain {n} Gold"});}
            else if(state.Mode=="LocationEvidenceOrSearch20858"){options.Add(new RewardChoiceOption{Key="EVIDENCE",Label="Gain 1 Evidence/Lore"});options.Add(new RewardChoiceOption{Key="SEARCH",Label="Draw 2 Search • choose 1"});}
            else if(state.Mode=="LocationSupplyOrEncounter0858"){options.Add(new RewardChoiceOption{Key="SUPPLY",Label="Gain 1 Supply"});options.Add(new RewardChoiceOption{Key="ENCOUNTER",Label="Inspect top Encounter card"});}
            else if(state.Mode.StartsWith("LocationGoldOrItem0858:",StringComparison.OrdinalIgnoreCase)){int n=FirstNumber0858(state.Mode,25);options.Add(new RewardChoiceOption{Key="GOLD",Label=$"Gain {n} Gold"});options.Add(new RewardChoiceOption{Key="ITEM",Label="Gain 1 Item"});}
            else if(state.Mode=="LocationRevealHiddenOrEncounter0858"){options.Add(new RewardChoiceOption{Key="REVEAL",Label="Reveal one Hidden Enemy"});options.Add(new RewardChoiceOption{Key="ENCOUNTER",Label="Inspect top Encounter card"});}
            else if(state.Mode.StartsWith("LocationSupplyOrGold0858:",StringComparison.OrdinalIgnoreCase)){var p=state.Mode.Split(':');int a=p.Length>1&&int.TryParse(p[1],out var aa)?aa:1;int b=p.Length>2&&int.TryParse(p[2],out var bb)?bb:10;options.Add(new RewardChoiceOption{Key="SUPPLY",Label=$"Gain {a} Supply"});options.Add(new RewardChoiceOption{Key="GOLD",Label=$"Gain {b} Gold"});}
            else if(state.Mode.StartsWith("LocationSupplyOrHeal0858:",StringComparison.OrdinalIgnoreCase)){var p=state.Mode.Split(':');int a=p.Length>1&&int.TryParse(p[1],out var aa)?aa:1;int b=p.Length>2&&int.TryParse(p[2],out var bb)?bb:1;options.Add(new RewardChoiceOption{Key="SUPPLY",Label=$"Gain {a} Supply"});options.Add(new RewardChoiceOption{Key="HEAL",Label=$"Recover {b} Health"});}
            else if(state.Mode=="LocationCleanseOrSearch0858"){options.Add(new RewardChoiceOption{Key="CLEANSE",Label="Remove 1 harmful Condition"});options.Add(new RewardChoiceOption{Key="SEARCH",Label="Draw 1 Search card"});}
            else if(state.Mode=="LocationItemOrCleanse0858"){options.Add(new RewardChoiceOption{Key="ITEM",Label="Gain 1 Item"});options.Add(new RewardChoiceOption{Key="CLEANSE",Label="Remove 1 harmful Condition"});}
            else if(state.Mode=="LocationSanctuary0858"){options.Add(new RewardChoiceOption{Key="HEAL",Label="Recover 2 Health"});options.Add(new RewardChoiceOption{Key="CLEANSE",Label="Remove 1 harmful Condition"});}
            else if(state.Mode=="LocationDiscardOrDamage0858"||state.Mode=="LocationDiscardOrDamageForLoot0858"){if(hero!=null&&hero.Hand.Count>0)options.Add(new RewardChoiceOption{Key="DISCARD",Label="Discard 1 Hero card"});options.Add(new RewardChoiceOption{Key="DAMAGE",Label="Suffer 1 Damage"});}
            else if(state.Mode=="LocationExploreTopBottom0858"&&ids.Count>0){options.Add(new RewardChoiceOption{Key="TOP",Label=$"Keep {ExplorationLocationLabel0828(ids[0])} on top"});options.Add(new RewardChoiceOption{Key="BOTTOM",Label=$"Place {ExplorationLocationLabel0828(ids[0])} on bottom"});}
            else if(state.Mode=="LocationExploreReorder0858"&&ids.Count>=2)AppendPermutationOptions0858(options,ids,true);
            else if(state.Mode=="LocationEncounterTopBottom0858"&&ids.Count>0){options.Add(new RewardChoiceOption{Key="TOP",Label=$"Keep {EncounterCardLabel(ids[0])} on top"});options.Add(new RewardChoiceOption{Key="BOTTOM",Label=$"Place {EncounterCardLabel(ids[0])} on bottom"});}
            else if(state.Mode=="LocationEncounterReorder0858"&&ids.Count>=2)AppendPermutationOptions0858(options,ids,false);
            else if(state.Mode=="LocationEncounterKeepDiscard0858"&&ids.Count==1){options.Add(new RewardChoiceOption{Key="KEEP",Label=$"Keep {EncounterCardLabel(ids[0])} on top"});options.Add(new RewardChoiceOption{Key="DISCARD",Label=$"Discard {EncounterCardLabel(ids[0])}"});}
            else if(state.Mode=="LocationEncounterDiscardOne0858"&&ids.Count>=2)
            {
                if(ids.Count==2){options.Add(new RewardChoiceOption{Key="D0",Label=$"Discard {EncounterCardLabel(ids[0])}; keep {EncounterCardLabel(ids[1])} on top"});options.Add(new RewardChoiceOption{Key="D1",Label=$"Discard {EncounterCardLabel(ids[1])}; keep {EncounterCardLabel(ids[0])} on top"});}
                else for(int d=0;d<3;d++){var keep=Enumerable.Range(0,3).Where(i=>i!=d).ToArray();options.Add(new RewardChoiceOption{Key=$"D{d}O{keep[0]}_{keep[1]}",Label=$"Discard {EncounterCardLabel(ids[d])}; top {EncounterCardLabel(ids[keep[0]])} → {EncounterCardLabel(ids[keep[1]])}"});options.Add(new RewardChoiceOption{Key=$"D{d}O{keep[1]}_{keep[0]}",Label=$"Discard {EncounterCardLabel(ids[d])}; top {EncounterCardLabel(ids[keep[1]])} → {EncounterCardLabel(ids[keep[0]])}"});}
            }
            else if(state.Mode=="LocationDualDeckTopBottom0858"&&ids.Count>=2){options.Add(new RewardChoiceOption{Key="TT",Label="Keep both inspected cards on top"});options.Add(new RewardChoiceOption{Key="TB",Label="Location top; Encounter bottom"});options.Add(new RewardChoiceOption{Key="BT",Label="Location bottom; Encounter top"});options.Add(new RewardChoiceOption{Key="BB",Label="Place both inspected cards on bottom"});}
            else if(state.Mode=="LocationSearchKeepOne0858")for(int i=0;i<ids.Count;i++)if(SearchEncounterContent0820.SearchCards.TryGetValue(ids[i],out var c))options.Add(new RewardChoiceOption{Key=$"KEEP{i}",Label=$"Resolve {c.Id} {c.Name}"});
        }

        private void AppendPermutationOptions0858(List<RewardChoiceOption> options,List<string> ids,bool locations)
        {
            if(options==null||ids==null||ids.Count<2)return;
            var usable=ids.Take(3).ToList();
            var orders=new List<int[]>();
            if(usable.Count==2)
            {
                orders.Add(new[]{0,1});
                orders.Add(new[]{1,0});
            }
            else
            {
                orders.Add(new[]{0,1,2});
                orders.Add(new[]{0,2,1});
                orders.Add(new[]{1,0,2});
                orders.Add(new[]{1,2,0});
                orders.Add(new[]{2,0,1});
                orders.Add(new[]{2,1,0});
            }
            foreach(var order in orders)
            {
                string key="ORDER"+string.Join("_",order);
                string label=string.Join(" → ",order.Select(i=>locations?ExplorationLocationLabel0828(usable[i]):EncounterCardLabel(usable[i])));
                options.Add(new RewardChoiceOption{Key=key,Label=$"Top: {label}"});
            }
        }
        private bool ApplyReorderChoice0858(List<string> deck,List<string> ids,string key)
        {
            if(deck==null||ids==null||ids.Count<2||string.IsNullOrWhiteSpace(key)||!key.StartsWith("ORDER",StringComparison.OrdinalIgnoreCase))return false;
            var parts=key.Substring(5).Split(new[]{'_'},StringSplitOptions.RemoveEmptyEntries);var order=new List<string>();foreach(var part in parts){if(!int.TryParse(part,out int i)||i<0||i>=ids.Count)return false;order.Add(ids[i]);}if(order.Count!=ids.Take(3).Count())return false;
            deck.RemoveAll(x=>ids.Contains(x,StringComparer.OrdinalIgnoreCase));deck.InsertRange(0,order);return true;
        }

        private bool TryResolve0858LocationChoice(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state==null||string.IsNullOrEmpty(state.Mode))return false;var ids=state.RevealedCardIds??new List<string>();
            if(state.Mode.StartsWith("LocationHealOrCleanse0858:",StringComparison.OrdinalIgnoreCase))
            {int n=FirstNumber0858(state.Mode,1);if(key=="HEAL")HealHero(hero,n,state.SourceCardName,hero);else if(key=="CLEANSE")RemoveFirstHarmfulStatus(hero,state.SourceCardName);else return true;}
            else if(state.Mode=="LocationAttackOrArmor0858")
            {if(key=="ATTACK")AddStatus(hero,"Location Attack +1",0,1,state.SourceCardName,true,hero,"until the end of the round");else if(key=="ARMOR")AddStatus(hero,"Location Armor +1",0,1,state.SourceCardName,true,hero,"until the end of the round");else return true;}
            else if(state.Mode.StartsWith("LocationGoldOrSearch0858:",StringComparison.OrdinalIgnoreCase))
            {int n=FirstNumber0858(state.Mode,10);if(key=="GOLD")AddPartyGold(n,state.SourceCardName);else if(key=="SEARCH"&&ids.Count>0&&Locations.TryGetValue(ids[0],out var l)){PendingRewardChoice=null;ResolveSearchDraw(l,hero);}else return true;}
            else if(state.Mode.StartsWith("LocationGoldOrEvidence0858:",StringComparison.OrdinalIgnoreCase))
            {int n=FirstNumber0858(state.Mode,10);if(key=="GOLD")AddPartyGold(n,state.SourceCardName);else if(key=="EVIDENCE")TryAdvanceQuestNamedGoal0858("Evidence",1,state.SourceCardName);else return true;}
            else if(state.Mode=="LocationEvidenceOrEncounter0858")
            {if(key=="EVIDENCE")TryAdvanceQuestNamedGoal0858("Evidence",1,state.SourceCardName);else if(key=="ENCOUNTER"){var l=DebugLocationByAuthored0858Internal(state.SourceCardId);string top=l==null?null:ActiveEncounterDeckForLocation0858(l).FirstOrDefault();AddLog(top==null?$"LOCATION SCOUT — {state.SourceCardName}: no Encounter card remains.":$"LOCATION SCOUT — {state.SourceCardName} inspects {EncounterCardLabel(top)}.");}else return true;}
            else if(state.Mode.StartsWith("LocationEvidenceOrGold0858:",StringComparison.OrdinalIgnoreCase))
            {int n=FirstNumber0858(state.Mode,10);if(key=="EVIDENCE")TryAdvanceQuestNamedGoal0858("Evidence",1,state.SourceCardName);else if(key=="GOLD")AddPartyGold(n,state.SourceCardName);else return true;}
            else if(state.Mode=="LocationEvidenceOrSearch20858")
            {if(key=="EVIDENCE"){if(!TryAdvanceQuestNamedGoal0858("Evidence",1,state.SourceCardName))TryAdvanceQuestNamedGoal0858("Lore",1,state.SourceCardName);}else if(key=="SEARCH"){var l=DebugLocationByAuthored0858Internal(state.SourceCardId);PendingRewardChoice=null;if(l!=null)DrawSearchFromLocation0858(l,hero,2);result=true;return true;}else return true;}
            else if(state.Mode=="LocationSupplyOrEncounter0858")
            {if(key=="SUPPLY")GainSupply0858(1,state.SourceCardName);else if(key=="ENCOUNTER"){var l=DebugLocationByAuthored0858Internal(state.SourceCardId);string top=l==null?null:ActiveEncounterDeckForLocation0858(l).FirstOrDefault();AddLog(top==null?$"LOCATION SCOUT — {state.SourceCardName}: no Encounter card remains.":$"LOCATION SCOUT — {state.SourceCardName} inspects {EncounterCardLabel(top)}.");}else return true;}
            else if(state.Mode.StartsWith("LocationGoldOrItem0858:",StringComparison.OrdinalIgnoreCase))
            {int n=FirstNumber0858(state.Mode,25);if(key=="GOLD")AddPartyGold(n,state.SourceCardName);else if(key=="ITEM")DrawLootToHero(hero,state.SourceCardName);else return true;}
            else if(state.Mode=="LocationRevealHiddenOrEncounter0858")
            {if(key=="REVEAL"){var e=Enemies.Values.FirstOrDefault(x=>!x.Defeated&&x.Statuses.Any(st=>string.Equals(st.Name,"Hidden",StringComparison.OrdinalIgnoreCase)));if(e!=null)RemoveStatus(e.Statuses,"Hidden",e.DisplayName,state.SourceCardName);}else if(key=="ENCOUNTER"){var l=DebugLocationByAuthored0858Internal(state.SourceCardId);string top=l==null?null:ActiveEncounterDeckForLocation0858(l).FirstOrDefault();AddLog(top==null?$"LOCATION SCOUT — {state.SourceCardName}: no Encounter card remains.":$"LOCATION SCOUT — {state.SourceCardName} inspects {EncounterCardLabel(top)}.");}else return true;}
            else if(state.Mode=="LocationDamageOrStop0858")
            {if(key=="DAMAGE")ApplyHeroDamageNow(hero,1,state.SourceCardName);else if(key=="STOP")AddStatus(hero,"Movement Ended",0,0,state.SourceCardName,false,hero,"until end of turn");else return true;}
            else if(state.Mode.StartsWith("LocationSupplyOrGold0858:",StringComparison.OrdinalIgnoreCase))
            {var p=state.Mode.Split(':');int a=p.Length>1&&int.TryParse(p[1],out var aa)?aa:1;int b=p.Length>2&&int.TryParse(p[2],out var bb)?bb:10;if(key=="SUPPLY")GainSupply0858(a,state.SourceCardName);else if(key=="GOLD")AddPartyGold(b,state.SourceCardName);else return true;}
            else if(state.Mode.StartsWith("LocationSupplyOrHeal0858:",StringComparison.OrdinalIgnoreCase))
            {var p=state.Mode.Split(':');int a=p.Length>1&&int.TryParse(p[1],out var aa)?aa:1;int b=p.Length>2&&int.TryParse(p[2],out var bb)?bb:1;if(key=="SUPPLY")GainSupply0858(a,state.SourceCardName);else if(key=="HEAL")HealHero(hero,b,state.SourceCardName,hero);else return true;}
            else if(state.Mode=="LocationCleanseOrSearch0858")
            {if(key=="CLEANSE")RemoveFirstHarmfulStatus(hero,state.SourceCardName);else if(key=="SEARCH"&&ids.Count>0&&Locations.TryGetValue(ids[0],out var l)){PendingRewardChoice=null;ResolveSearchDraw(l,hero);}else return true;}
            else if(state.Mode=="LocationItemOrCleanse0858")
            {if(key=="ITEM")DrawLootToHero(hero,state.SourceCardName);else if(key=="CLEANSE")RemoveFirstHarmfulStatus(hero,state.SourceCardName);else return true;}
            else if(state.Mode=="LocationSanctuary0858")
            {if(key=="HEAL")HealHero(hero,2,state.SourceCardName,hero);else if(key=="CLEANSE")RemoveFirstHarmfulStatus(hero,state.SourceCardName);else return true;}
            else if(state.Mode=="LocationDiscardOrDamage0858"||state.Mode=="LocationDiscardOrDamageForLoot0858")
            {if(key=="DAMAGE")ApplyHeroDamageNow(hero,1,state.SourceCardName);else if(key=="DISCARD"&&hero!=null&&hero.Hand.Count>0){var c=hero.Hand[0];hero.Hand.RemoveAt(0);hero.Discard.Add(c.Id);AddLog($"LOCATION COST — {hero.DisplayName} discards {c.Name} ({state.SourceCardName}).");}else return true;if(state.Mode=="LocationDiscardOrDamageForLoot0858")DrawLootToHero(hero,state.SourceCardName);}
            else if(state.Mode=="LocationExploreTopBottom0858"&&ids.Count>0)
            {ExplorationDeck0828.RemoveAll(x=>string.Equals(x,ids[0],StringComparison.OrdinalIgnoreCase));if(key=="TOP")ExplorationDeck0828.Insert(0,ids[0]);else if(key=="BOTTOM")ExplorationDeck0828.Add(ids[0]);else return true;}
            else if(state.Mode=="LocationExploreReorder0858"&&ids.Count>=2)
            {if(!ApplyReorderChoice0858(ExplorationDeck0828,ids,key))return true;}
            else if(state.Mode=="LocationEncounterTopBottom0858"&&ids.Count>0)
            {var l=Locations.Values.FirstOrDefault(v=>string.Equals(v.AuthoredLocationId,state.SourceCardId,StringComparison.OrdinalIgnoreCase))??(hero!=null&&Locations.TryGetValue(hero.LocationId,out var here)?here:null);if(l==null)return true;var deck=ActiveEncounterDeckForLocation0858(l);deck.RemoveAll(x=>string.Equals(x,ids[0],StringComparison.OrdinalIgnoreCase));if(key=="TOP")deck.Insert(0,ids[0]);else if(key=="BOTTOM")deck.Add(ids[0]);else return true;}
            else if(state.Mode=="LocationEncounterReorder0858"&&ids.Count>=2)
            {var l=Locations.Values.FirstOrDefault(v=>string.Equals(v.AuthoredLocationId,state.SourceCardId,StringComparison.OrdinalIgnoreCase))??(hero!=null&&Locations.TryGetValue(hero.LocationId,out var here)?here:null);if(l==null||!ApplyReorderChoice0858(ActiveEncounterDeckForLocation0858(l),ids,key))return true;}
            else if(state.Mode=="LocationEncounterKeepDiscard0858"&&ids.Count==1)
            {var l=DebugLocationByAuthored0858Internal(state.SourceCardId);if(l==null)return true;var deck=ActiveEncounterDeckForLocation0858(l);if(key=="KEEP"){}else if(key=="DISCARD"){deck.RemoveAll(x=>string.Equals(x,ids[0],StringComparison.OrdinalIgnoreCase));ActiveEncounterDiscardForLocation0858(l).Add(ids[0]);}else return true;}
            else if(state.Mode=="LocationEncounterDiscardOne0858"&&ids.Count>=2)
            {
                var l=DebugLocationByAuthored0858Internal(state.SourceCardId);if(l==null)return true;var deck=ActiveEncounterDeckForLocation0858(l);var discard=ActiveEncounterDiscardForLocation0858(l);var m=Regex.Match(key??string.Empty,@"^D(\d)(?:O(\d)_(\d))?$");if(!m.Success||!int.TryParse(m.Groups[1].Value,out int di)||di<0||di>=ids.Count)return true;
                var kept=ids.Where((x,i)=>i!=di).ToList();if(m.Groups[2].Success&&int.TryParse(m.Groups[2].Value,out int a)&&int.TryParse(m.Groups[3].Value,out int b)&&a>=0&&a<ids.Count&&b>=0&&b<ids.Count)kept=new List<string>{ids[a],ids[b]};deck.RemoveAll(x=>ids.Contains(x,StringComparer.OrdinalIgnoreCase));deck.InsertRange(0,kept);discard.Add(ids[di]);
            }
            else if(state.Mode=="LocationDualDeckTopBottom0858"&&ids.Count>=2)
            {
                var l=Locations.Values.FirstOrDefault(v=>string.Equals(v.AuthoredLocationId,state.SourceCardId,StringComparison.OrdinalIgnoreCase))??(hero!=null&&Locations.TryGetValue(hero.LocationId,out var here)?here:null);if(l==null||key.Length!=2)return true;var deck=ActiveEncounterDeckForLocation0858(l);
                ExplorationDeck0828.RemoveAll(x=>string.Equals(x,ids[0],StringComparison.OrdinalIgnoreCase));if(key[0]=='T')ExplorationDeck0828.Insert(0,ids[0]);else if(key[0]=='B')ExplorationDeck0828.Add(ids[0]);else return true;
                deck.RemoveAll(x=>string.Equals(x,ids[1],StringComparison.OrdinalIgnoreCase));if(key[1]=='T')deck.Insert(0,ids[1]);else if(key[1]=='B')deck.Add(ids[1]);else return true;
            }
            else if(state.Mode=="LocationSearchKeepOne0858")
            {
                if(!key.StartsWith("KEEP")||!int.TryParse(key.Substring(4),out int i)||i<0||i>=ids.Count)return true;
                var l=Locations.Values.FirstOrDefault(v=>string.Equals(v.AuthoredLocationId,state.SourceCardId,StringComparison.OrdinalIgnoreCase))??(hero!=null&&Locations.TryGetValue(hero.LocationId,out var here)?here:null);
                PendingRewardChoice=null; // allow the chosen Search card to open its own follow-up choice without being blocked by this picker
                for(int x=0;x<ids.Count;x++)
                {
                    if(x==i&&l!=null&&SearchEncounterContent0820.SearchCards.TryGetValue(ids[x],out var c))ResolveSearchCard(c,l,hero);
                    SearchDiscard.Add(ids[x]);
                }
            }
            else return false;
            AddLog($"LOCATION CHOICE RESOLVED — {state.SourceCardName}: {key}.");
            if(object.ReferenceEquals(PendingRewardChoice,state))PendingRewardChoice=null;
            StateChanged?.Invoke();result=true;return true;
        }

        private int RegionalEncounterStart0858(LocationState location)
        {
            var d=LocationDef0858(location);string deck=d?.encounterDeck??string.Empty,region=d?.region??string.Empty;
            if(Has0858(deck,"Ironspine"))return 46;if(Has0858(deck,"Verdant"))return 61;if(Has0858(deck,"Storm Coast"))return 76;if(Has0858(deck,"Sunscar"))return 91;
            if(Has0858(region,"Valewatch")||Has0858(region,"Crownlands")||Has0858(region,"Buried Valewatch")||Has0858(region,"Underways"))return 31;
            if(Has0858(region,"Ironspine"))return 46;
            if(Has0858(region,"Verdant"))return 61;
            if(Has0858(region,"Storm Coast"))return 76;
            if(Has0858(region,"Sunscar"))return 91;
            return 1;
        }
        private string RegionalEncounterKeyFor0858(LocationState location)
        {
            int start=RegionalEncounterStart0858(location);return start==1?"Stonebrook":start==31?"Valewatch":start==46?"Ironspine":start==61?"Verdant":start==76?"StormCoast":"Sunscar";
        }
        private void EnsureRegionalEncounterDeck0858(LocationState location)
        {
            string key=RegionalEncounterKeyFor0858(location);if(string.Equals(RegionalEncounterDeckKey0858,key,StringComparison.OrdinalIgnoreCase)&&RegionalEncounterDeck0858.Count+RegionalEncounterDiscard0858.Count>0)return;
            RegionalEncounterDeckKey0858=key;RegionalEncounterDeck0858.Clear();RegionalEncounterDiscard0858.Clear();int start=RegionalEncounterStart0858(location),end=start==1?30:start+14;
            foreach(var c in SearchEncounterContent0820.EncounterCards.Values.Where(c=>{var m=Regex.Match(c.Id??string.Empty,@"^EN(\d{3})$");return m.Success&&int.TryParse(m.Groups[1].Value,out int n)&&n>=start&&n<=end;}).OrderBy(c=>c.Id))
                for(int i=0;i<Mathf.Max(1,c.Copies);i++)RegionalEncounterDeck0858.Add(c.Id);
            Shuffle(RegionalEncounterDeck0858);AddLog($"REGIONAL ENCOUNTER DECK — {key}: {RegionalEncounterDeck0858.Count} authored card copies ready.");
        }
        private void ResolveRegionalEncounter0858(LocationState location,HeroState hero,int level,string source)
        {
            EnsureRegionalEncounterDeck0858(location);
            string chosen=null;
            for(int pass=0;pass<2&&chosen==null;pass++)
            {
                for(int i=0;i<RegionalEncounterDeck0858.Count;i++)if(SearchEncounterContent0820.EncounterCards.TryGetValue(RegionalEncounterDeck0858[i],out var c)&&SearchEncounterContent0820.MatchesLocation(location,c.Terrain)){chosen=RegionalEncounterDeck0858[i];RegionalEncounterDeck0858.RemoveAt(i);break;}
                if(chosen==null&&RegionalEncounterDiscard0858.Count>0){RegionalEncounterDeck0858.AddRange(RegionalEncounterDiscard0858);RegionalEncounterDiscard0858.Clear();Shuffle(RegionalEncounterDeck0858);}
            }
            if(chosen==null){AddLog($"REGIONAL ENCOUNTER — no eligible {RegionalEncounterDeckKey0858} Encounter remains for {location.Name}.");return;}
            var savedDeck=EncounterDeck.ToList();var savedDiscard=EncounterDiscard.ToList();EncounterDeck.Clear();EncounterDiscard.Clear();EncounterDeck.Add(chosen);
            ResolveTravelEncounter(location,hero,level,0,source);
            RegionalEncounterDiscard0858.Add(chosen);
            EncounterDeck.Clear();EncounterDeck.AddRange(savedDeck);EncounterDiscard.Clear();EncounterDiscard.AddRange(savedDiscard);
        }

        private void ResolveLocationFirstVisit0858(LocationState location,HeroState hero)
        {
            if(location==null||hero==null)return;var d=LocationDef0858(location);string text=d?.firstVisitEffect??location.FirstVisitEffect??string.Empty;
            if(string.IsNullOrWhiteSpace(text)){AddLog($"LOCATION FIRST VISIT — {location.Name}: no random Encounter is triggered by the authored rule.");return;}
            // Some authored rows suppress an Encounter only in a named Quest and explicitly define an otherwise-fallback.
            // Honor that fallback rather than treating the leading "None" as unconditional.
            var conditionalNone=Regex.Match(text,@"^(?:None|No\s+(?:normal\s+)?(?:random\s+)?(?:Travel\s+)?Encounter)\s+during\s+(Q\d{3})[^.;]*(?:;|,)\s*otherwise\s+draw\s+1\s+[^.;]*Encounter(?:\s+at)?\s+(?:Encounter\s+)?Level\s+(\d+)",RegexOptions.IgnoreCase);
            if(conditionalNone.Success)
            {
                if(string.Equals(CurrentQuestId0829,conditionalNone.Groups[1].Value,StringComparison.OrdinalIgnoreCase)){AddLog($"LOCATION FIRST VISIT — {location.Name}: random Encounter suppressed during {conditionalNone.Groups[1].Value}.");return;}
                text=$"Draw 1 Encounter at Encounter Level {conditionalNone.Groups[2].Value}.";
            }
            else if(Regex.IsMatch(text,@"^(None|No\s+(?:normal\s+)?(?:random\s+)?(?:Travel\s+)?Encounter|No\s+Encounter)",RegexOptions.IgnoreCase)){AddLog($"LOCATION FIRST VISIT — {location.Name}: no random Encounter is triggered by the authored rule.");return;}
            if(RandomTravelEncountersSuppressed0833||WorldEventRandomTravelSuppressed0857(location)){AddLog($"{CurrentWorldEventId0833} — random Encounter suppressed at {location.Name}.");return;}
            bool callsEncounter=Has0858(text,"Encounter");if(!callsEncounter)return;
            int level=ExtractNumber(text,@"(?:Encounter\s+)?Level\s+(\d+)");if(level<=0)level=Mathf.Max(1,d?.encounterLevel??1);
            location.TravelEncounterRequests++;
            int start=RegionalEncounterStart0858(location);
            if(start==1)ResolveTravelEncounter(location,hero,level,0,$"Location First Visit {location.AuthoredLocationId}");
            else ResolveRegionalEncounter0858(location,hero,level,$"Location First Visit {location.AuthoredLocationId}");
            TravelEncounterRequested?.Invoke(location,hero,level);
        }

        private void ResolveLocationPostEncounter0858(EncounterRuntimeState encounter)
        {
            if(encounter==null||string.IsNullOrWhiteSpace(encounter.Source)||!encounter.Source.StartsWith("Location First Visit",StringComparison.OrdinalIgnoreCase))return;
            if(!Locations.TryGetValue(encounter.LocationId??string.Empty,out var location))return;var d=LocationDef0858(location);string text=d?.firstVisitEffect??string.Empty;
            int at=text.IndexOf("After the Encounter resolves",StringComparison.OrdinalIgnoreCase);if(at<0)return;
            string post=text.Substring(at);string key=$"{Location0858Prefix}POST:{CurrentQuestId0829}";if(!UseLocationKey0858(location,key))return;
            var hero=RewardHeroFor(encounter.SourceHeroId);if(hero==null)return;bool did=false;
            var gold=Regex.Match(post,@"gain\s+(\d+)\s+Gold",RegexOptions.IgnoreCase);if(gold.Success&&int.TryParse(gold.Groups[1].Value,out int gp)){AddPartyGold(gp,$"{location.Name} First Visit");did=true;}
            if(Regex.IsMatch(post,@"draw\s+(?:1|one)\s+(?:source-matched\s+)?Loot",RegexOptions.IgnoreCase)||Regex.IsMatch(post,@"draw\s+(?:1|one)\s+Item",RegexOptions.IgnoreCase)){DrawLootToHero(hero,$"{location.Name} First Visit");did=true;}
            if(Has0858(post,"Search card")){ResolveSearchDraw(location,hero);did=true;}
            if(Has0858(post,"remove 1 negative Condition")){RemoveFirstHarmfulStatus(hero,$"{location.Name} First Visit");did=true;}
            if(!did)AddLog($"LOCATION FIRST VISIT FOLLOW-UP — {location.Name}: authored follow-up requires no standalone state beyond its printed/Quest rule: {post}");
        }

        private bool HasUnresolvedLocationQuestObjective0858(LocationState location)
        {
            if(location==null||QuestRuntime0856==null)return false;
            string authored=location.AuthoredLocationId??location.Id;
            return QuestRuntime0856.Goals.Any(g=>!g.Complete&&g.LocationIds!=null&&g.LocationIds.Contains(authored,StringComparer.OrdinalIgnoreCase));
        }
        private bool CanSearchLocation0858(HeroState hero,LocationState location,bool granted)
        {
            var d=LocationDef0858(location);if(d==null)return true;string s=d.search??string.Empty;if(string.IsNullOrWhiteSpace(s))return true;
            bool currentQuestNamed=Regex.Matches(s,@"Q\d{3}",RegexOptions.IgnoreCase).Cast<Match>().Any(m=>string.Equals(m.Value,CurrentQuestId0829,StringComparison.OrdinalIgnoreCase));
            bool namesQuest=Regex.IsMatch(s,@"Q\d{3}",RegexOptions.IgnoreCase);
            if(namesQuest&&!currentQuestNamed&&Has0858(s,"during"))return d.searchLimit>0;
            if(Has0858(s,"No normal Search"))return granted;
            if(Regex.IsMatch(s,@"^No Search\.?$",RegexOptions.IgnoreCase))return false;
            if(Has0858(s,"No Search")||Has0858(s,"No normal Search"))
            {
                if(Has0858(s,"until clear")||Has0858(s,"after clear"))return !Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==location.Id);
                if(Has0858(s,"until")&&HasUnresolvedLocationQuestObjective0858(location))return false;
                if(currentQuestNamed)return granted;
            }
            return d.searchLimit>0||granted;
        }

        private void QueueLocationPostSearch0858(LocationState location,HeroState hero)
        {
            if(location==null||hero==null)return;
            if(HasPendingRewardChoice)
            {
                DeferredLocationPostSearchLocationId0858=location.Id;
                DeferredLocationPostSearchHeroId0858=hero.Id;
                return;
            }
            OnLocationSearchResolved0858(location,hero);
        }
        private void TryResolveDeferredLocationPostSearch0858()
        {
            if(HasPendingRewardChoice||string.IsNullOrWhiteSpace(DeferredLocationPostSearchLocationId0858)||string.IsNullOrWhiteSpace(DeferredLocationPostSearchHeroId0858))return;
            string locationId=DeferredLocationPostSearchLocationId0858,heroId=DeferredLocationPostSearchHeroId0858;
            DeferredLocationPostSearchLocationId0858=null;DeferredLocationPostSearchHeroId0858=null;
            if(Locations.TryGetValue(locationId,out var location)&&Heroes.TryGetValue(heroId,out var hero))OnLocationSearchResolved0858(location,hero);
        }
        private void ClearDeferredLocationPostSearch0858(){DeferredLocationPostSearchLocationId0858=null;DeferredLocationPostSearchHeroId0858=null;}

        private bool ResolveLocationSearch0858(LocationState location,HeroState hero)
        {
            var d=LocationDef0858(location);if(d==null)return false;string s=d.search??string.Empty,ongoing=d.ongoingEffect??string.Empty;
            bool firstQuestDrawTwo=Regex.IsMatch(ongoing,@"first\s+Search[^.]*each\s+Quest[^.]*draw\s+2\s+Search",RegexOptions.IgnoreCase)||Regex.IsMatch(ongoing,@"first\s+Search[^.]*draws?\s+2\s+Search",RegexOptions.IgnoreCase)||Regex.IsMatch(ongoing,@"Search\s+draws?\s+2\s+keep\s*1\s+once\s+per\s+Quest",RegexOptions.IgnoreCase);
            if(firstQuestDrawTwo&&UseLocationKey0858(location,$"{Location0858Prefix}SEARCH2:{CurrentQuestId0829}")){DrawSearchFromLocation0858(location,hero,2);return true;}
            if(Regex.IsMatch(s,@"Draw\s+2\s+Search(?:\s+cards?)?[,;]?\s*(?:and\s+choose|keep)\s+1",RegexOptions.IgnoreCase)||Regex.IsMatch(s,@"Draw\s+2\s+Search\s+keep\s*1",RegexOptions.IgnoreCase))
            {DrawSearchFromLocation0858(location,hero,2);return true;}
            return false;
        }
        private void OnLocationSearchResolved0858(LocationState location,HeroState hero)
        {
            if(location==null||hero==null)return;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;
            if(Has0858(text,"first Hero to search draws 1 additional Item")&&UseLocationKey0858(location,$"{Location0858Prefix}SEARCHITEM:{CurrentQuestId0829}"))DrawLootToHero(hero,$"{location.Name} first Search");
            if(Has0858(text,"first Hero to search gains +1 Morale")&&UseLocationKey0858(location,$"{Location0858Prefix}SEARCHMORALE:{CurrentQuestId0829}"))
            {PartyMorale0831=Mathf.Min(5,PartyMorale0831+1);AddLog($"LOCATION EFFECT — {location.Name}: party Morale increases by 1.");}
            if(Regex.IsMatch(text,@"(?:after\s+Search|after\s+Searching)[^.]*once\s+per\s+Quest[^.]*recover\s+1\s+Health",RegexOptions.IgnoreCase)||Regex.IsMatch(text,@"once\s+per\s+Quest\s+after\s+Search[^.]*recover\s+1\s+Health",RegexOptions.IgnoreCase))
            {if(!Has0858(text,"unless Sunfall 6+")||Sunfall0857<6)if(UseLocationKey0858(location,$"{Location0858Prefix}POSTSEARCHHEAL:{CurrentQuestId0829}"))HealHero(hero,1,$"{location.Name} Search",hero);}
            if(Regex.IsMatch(text,@"after[^.]*Search[^.]*remove\s+(?:one|1)\s+harmful\s+(?:Status|Condition)",RegexOptions.IgnoreCase)&&UseLocationKey0858(location,$"{Location0858Prefix}POSTSEARCHCLEANSE:{CurrentQuestId0829}"))RemoveFirstHarmfulStatus(hero,$"{location.Name} Search");
            if(Regex.IsMatch(text,@"once\s+per\s+Quest\s+after\s+Search[^.]*draw\s+2\s+Search[^.]*(?:keep|choose)\s*1",RegexOptions.IgnoreCase)&&UseLocationKey0858(location,$"{Location0858Prefix}POSTSEARCH2:{CurrentQuestId0829}"))DrawSearchFromLocation0858(location,hero,2);
            var encounterInspect=Regex.Match(text,@"(?:after\s+Search|after\s+Searching)[^.]*inspect\s+(?:the\s+)?(?:next|top)\s*(\d+)?[^.]*Encounter",RegexOptions.IgnoreCase);
            if(!encounterInspect.Success)encounterInspect=Regex.Match(text,@"once\s+per\s+Quest\s+after\s+Search[^.]*(?:next|top)\s*(\d+)?[^.]*Encounter",RegexOptions.IgnoreCase);
            if(encounterInspect.Success&&UseLocationKey0858(location,$"{Location0858Prefix}POSTSEARCHENCOUNTER:{CurrentQuestId0829}"))
            {int ec=encounterInspect.Groups[1].Success&&int.TryParse(encounterInspect.Groups[1].Value,out int parsed)?parsed:1;if(Has0858(text,"discard 1")||Has0858(text,"either keep or discard"))BeginLocationEncounterDiscardInspect0858(location,hero,Mathf.Clamp(ec,1,3));else BeginLocationEncounterInspect0858(location,hero,Mathf.Clamp(ec,1,3));}
            if(Has0858(text,"then active Hero suffers 1 damage at Blight 6+")&&Blight0857>=6&&UseLocationKey0858(location,$"{Location0858Prefix}POSTSEARCHBLIGHT:{CurrentQuestId0829}"))ApplyHeroDamageNow(hero,1,location.Name);
            var topLocation=Regex.Match(text,@"(?:after\s+Search|after\s+Searching)[^.]*inspect\s+(?:the\s+)?top\s+(\d+)[^.]*Location",RegexOptions.IgnoreCase);
            if(topLocation.Success&&int.TryParse(topLocation.Groups[1].Value,out int lc)&&UseLocationKey0858(location,$"{Location0858Prefix}POSTSEARCHEXPLORATION:{CurrentQuestId0829}"))BeginLocationExplorationInspect0858(location,hero,Mathf.Clamp(lc,1,3),lc==1);
        }

        private void OnLocationHeroEntered0858(HeroState hero,LocationState location)
        {
            if(hero==null||location==null)return;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(text))return;
            var heal=Regex.Match(text,@"first\s+time\s+each\s+Hero\s+enters[^.]*recover\s+(\d+)\s+Health",RegexOptions.IgnoreCase);
            if(heal.Success&&int.TryParse(heal.Groups[1].Value,out int n)&&UseLocationKey0858(location,$"{Location0858Prefix}ENTERHEAL:{CurrentQuestId0829}:{hero.Id}"))HealHero(hero,n,location.Name,hero);
            if(Has0858(text,"first Hero entering each Round loses Inspired")&&hero.HasStatus("Inspired")&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:LOSEINSPIRED"))RemoveStatus(hero.Statuses,"Inspired",hero.DisplayName,location.Name);
            if(Has0858(text,"first Hero entering each activation may Hide")&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:{hero.Id}:HIDE"))AddStatus(hero,"Hidden",1,0,location.Name,true,hero,"until the start of your next turn");
            if(Has0858(text,"first Hero entering each activation")&&Has0858(text,"+1 Armor")&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:{hero.Id}:ENTRYARMOR"))AddStatus(hero,"Location Armor +1",1,0,location.Name,true,hero,"until the start of your next turn");
        }

        private void ResolveLocationEndHeroActivation0858(HeroState hero)
        {
            if(hero==null||!Locations.TryGetValue(hero.LocationId??string.Empty,out var location))return;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;
            if(Has0858(text,"first Hero ending activation here each round")&&Has0858(text,"remove 1 negative Condition")&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:ENDCLEANSE"))RemoveFirstHarmfulStatus(hero,location.Name);
            var endHeal=Regex.Match(text,@"end\s+of\s+Hero\s+activation[^.]*recover\s+(\d+)\s+Health",RegexOptions.IgnoreCase);if(endHeal.Success&&int.TryParse(endHeal.Groups[1].Value,out int eh)&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:{hero.Id}:ENDHEAL"))HealHero(hero,eh,location.Name,hero);
            if((Has0858(text,"remove 1 Fatigue")||Has0858(text,"recover 1 Stamina"))&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:{hero.Id}:RESOURCE"))AddLog($"LOCATION EFFECT — {location.Name}: Fatigue/Stamina recovery is authored but the current Hero model has no Fatigue/Stamina pool; no substitute resource is invented.");
        }

        private void OnLocationHeroTurnStarted0858(HeroState hero)
        {
            if(hero==null||!Locations.TryGetValue(hero.LocationId??string.Empty,out var location))return;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(text))return;
            var beginHeal=Regex.Match(text,@"beginning\s+(?:their\s+)?activation[^.]*recover\s+(\d+)\s+Health",RegexOptions.IgnoreCase);
            if(beginHeal.Success&&int.TryParse(beginHeal.Groups[1].Value,out int n)&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:{hero.Id}:STARTHEAL"))HealHero(hero,n,location.Name,hero);
        }
        private int ModifyLocationHealing0858(HeroState target,int amount,string source)
        {
            if(target==null||amount<=0||!Locations.TryGetValue(target.LocationId??string.Empty,out var location))return amount;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(text))return amount;
            bool divine=Has0858(source,"Divine")||Has0858(source,"Holy");
            if(Regex.IsMatch(text,@"Heroes\s+cannot\s+recover\s+Health\s+here",RegexOptions.IgnoreCase))return 0;
            bool genericMinus=Regex.IsMatch(text,@"healing\s+(?:here\s+)?is\s+reduced\s+by\s+1",RegexOptions.IgnoreCase)||Regex.IsMatch(text,@"Heroes\s+recovering\s+Health\s+here\s+recover\s+1\s+less",RegexOptions.IgnoreCase);
            if(genericMinus&&Has0858(text,"while any Blight Node/Patch remains"))genericMinus=HasUnresolvedLocationQuestObjective0858(location)&&!HasMarker0857(location,"Blight Patch");
            if(genericMinus&&!(Has0858(text,"unless the effect is Divine")&&divine))amount=Mathf.Max(0,amount-1);
            bool firstPlus=Regex.IsMatch(text,@"first\s+(?:Hero\s+to\s+)?recover(?:y|\s+Health)[^.]*each\s+Round[^.]*\+1",RegexOptions.IgnoreCase)||Regex.IsMatch(text,@"first\s+recovery\s+here\s+each\s+Round\s+gains\s+\+1",RegexOptions.IgnoreCase)||Regex.IsMatch(text,@"Once\s+per\s+Round[^.]*first\s+Hero\s+to\s+recover\s+Health[^.]*\+1",RegexOptions.IgnoreCase);
            if(firstPlus&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:HEALPLUS"))amount+=1;
            bool firstMinus=Regex.IsMatch(text,@"first\s+healing\s+here\s+each\s+Round\s+is\s+reduced\s+by\s+1",RegexOptions.IgnoreCase);
            if(firstMinus&&(!Has0858(text,"while Flooded")||HasMarker0857(location,"Flood"))&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:HEALMINUS"))amount=Mathf.Max(0,amount-1);
            return amount;
        }
        private void OnLocationHealingResolved0858(HeroState target,int healed)
        {
            if(target==null||healed<=0||!Locations.TryGetValue(target.LocationId??string.Empty,out var location))return;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;
            if(Has0858(text,"after a Hero here recovers Health")&&Has0858(text,"remove 1 temporary harmful Status")&&!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==location.Id)&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:{target.Id}:POSTHEALCLEANSE"))RemoveFirstHarmfulStatus(target,location.Name);
        }
        private void OnLocationHeroStatusAdded0858(HeroState hero,string statusName)
        {
            if(hero==null||string.IsNullOrWhiteSpace(statusName)||!Locations.TryGetValue(hero.LocationId??string.Empty,out var location))return;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;
            if((statusName.Equals("Inspired",StringComparison.OrdinalIgnoreCase)||statusName.Equals("Guarded",StringComparison.OrdinalIgnoreCase))&&Has0858(text,"first time each Round a Hero gains Inspired or Guarded here")&&UseLocationKey0858(location,$"{Location0858Prefix}R{Round}:{hero.Id}:STATUSHEAL"))HealHero(hero,1,location.Name,hero);
        }
        private int LocationRangeBonus0858(HeroState hero,int baseRange)
        {
            if(hero==null||baseRange<=0)return 0;int bonus=LocationStatusModifier0858(hero,"Location Range +");
            if(!Locations.TryGetValue(hero.LocationId??string.Empty,out var location))return bonus;string text=LocationDef0858(location)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(text))return bonus;
            var m=Regex.Match(text,@"gain\s+\+(\d+)\s+Range\s+with\s+ranged\s+attacks",RegexOptions.IgnoreCase);if(m.Success&&int.TryParse(m.Groups[1].Value,out int n))bonus+=n;
            return bonus;
        }

        private int QuestGoalValue0858(string label)
        {
            if(QuestRuntime0856==null||string.IsNullOrWhiteSpace(label))return 0;var g=QuestRuntime0856.Goals.FirstOrDefault(x=>x.Label.IndexOf(label,StringComparison.OrdinalIgnoreCase)>=0);return g?.Current??0;
        }
        private bool LocationMovementCondition0858(LocationState location,string text,HeroState hero)
        {
            if(location==null||!LocationConditionActive0858(text))return false;
            if(Has0858(text,"while a Cave-In Token is here")&&!HasLocationToken0858(location,"Cave-In"))return false;
            if(Has0858(text,"while Flood Marker present")&&!HasMarker0857(location,"Flood"))return false;
            if(Has0858(text,"while Ascent Track is 6+")&&QuestGoalValue0858("Ascent")<6)return false;
            if(Has0858(text,"unless Guarded")&&hero!=null&&hero.HasStatus("Guarded"))return false;
            return true;
        }
        private bool CanLocationUtility0858(HeroState hero)
        {
            if(hero==null||GeneralActions(hero)<1||!Locations.TryGetValue(hero.LocationId??string.Empty,out var l)||Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==l.Id))return false;string id=l.AuthoredLocationId??l.Id;
            if(HasLocationToken0858(l,"Cave-In"))return true;
            if(id=="L067"&&!HasLocationKey0858(l,$"{Location0858Prefix}R{Round}:SANCTUARY"))return true;
            if(id=="L044"&&!HasLocationKey0858(l,$"{Location0858Prefix}R{Round}:{hero.Id}:CLIMB"))return true;
            return false;
        }
        private string LocationUtilityLabel0858(HeroState hero)
        {
            if(hero==null||!Locations.TryGetValue(hero.LocationId??string.Empty,out var l))return "Location Action";string id=l.AuthoredLocationId??l.Id;if(HasLocationToken0858(l,"Cave-In"))return "Clear Cave-In";if(id=="L067")return "Sanctuary";if(id=="L044")return "Climb Trunk";return "Location Action";
        }
        private bool ResolveLocationUtility0858(HeroState hero)
        {
            if(!CanLocationUtility0858(hero)||!Locations.TryGetValue(hero.LocationId??string.Empty,out var l))return false;string id=l.AuthoredLocationId??l.Id;hero.Actions--;
            if(HasLocationToken0858(l,"Cave-In")){RemoveLocationToken0858(l,"Cave-In");AddLog($"LOCATION ACTION — {hero.DisplayName} clears the Cave-In at {l.Name}.");StateChanged?.Invoke();return true;}
            if(id=="L067"){UseLocationKey0858(l,$"{Location0858Prefix}R{Round}:SANCTUARY");BeginRewardChoice0827(id,l.Name,hero,"LocationSanctuary0858","Choose the Sanctuary benefit: recover 2 Health or remove 1 harmful Condition.");return true;}
            if(id=="L044"){UseLocationKey0858(l,$"{Location0858Prefix}R{Round}:{hero.Id}:CLIMB");AddStatus(hero,"Location Range +1",1,0,l.Name,true,hero,"until the start of your next turn");AddStatus(hero,"Location Attack +1",1,0,l.Name,true,hero,"until the start of your next turn");AddLog($"LOCATION ACTION — {hero.DisplayName} climbs the Fallen Giant Tree and gains +1 Range/+1 Attack with ranged attacks until the start of the next turn.");StateChanged?.Invoke();return true;}
            return false;
        }

        private int LocationMoveSurcharge0858(HeroState hero,string destinationId)
        {
            if(hero==null)return 0;int n=hero.HasStatus("Location Move Surcharge")?1:0;
            if(Locations.TryGetValue(hero.LocationId??string.Empty,out var from))
            {string t=LocationDef0858(from)?.ongoingEffect??string.Empty;if(LocationMovementCondition0858(from,t,hero)&&Regex.IsMatch(t,@"(?:Leaving\s+costs\s+\+1\s+Action|Costs\s+\+1\s+Action\s+to\s+leave)",RegexOptions.IgnoreCase))n++;}
            if(Locations.TryGetValue(destinationId??string.Empty,out var to))
            {string t=LocationDef0858(to)?.ongoingEffect??string.Empty;if(LocationMovementCondition0858(to,t,hero)&&Regex.IsMatch(t,@"Entering\s+costs\s+\+1\s+Action",RegexOptions.IgnoreCase))n++;}
            return n;
        }
        private void ConsumeLocationMoveSurcharge0858(HeroState hero)
        {if(hero!=null&&hero.HasStatus("Location Move Surcharge"))RemoveStatus(hero.Statuses,"Location Move Surcharge",hero.DisplayName,"Location movement surcharge consumed");}
        private bool LocationDifficultTerrain0858(string locationId)
        {
            if(!Locations.TryGetValue(locationId??string.Empty,out var l))return false;string text=LocationDef0858(l)?.ongoingEffect??l.OngoingEffect??string.Empty;
            return Has0858(text,"Difficult Terrain");
        }

        private int LocationStatusModifier0858(HeroState hero,string prefix)
        {
            if(hero==null)return 0;int total=0;
            foreach(var s in hero.Statuses.Where(s=>s!=null&&s.Name!=null&&s.Name.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)))
            {var m=Regex.Match(s.Name,@"([+-]?\d+)$");if(m.Success&&int.TryParse(m.Groups[1].Value,out int n))total+=n;}
            return total;
        }
        private int ConsumeLocationAttackModifier0858(HeroState hero,EnemyState enemy,bool ranged)
        {
            if(hero==null||enemy==null)return 0;int mod=LocationStatusModifier0858(hero,"Location Attack +");
            if(!Locations.TryGetValue(hero.LocationId??string.Empty,out var from))return mod;string t=LocationDef0858(from)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(t))return mod;
            bool legacy=Has0858(t,"High Ground")||Has0858(t,"Open Sightlines")||Has0858(t,"Limited Visibility");
            if(ranged&&!legacy)
            {
                bool firstPlus=Regex.IsMatch(t,@"first\s+ranged\s+Attack[^.]*gain\s+\+1\s+Attack",RegexOptions.IgnoreCase);
                bool firstMinus=Regex.IsMatch(t,@"first\s+ranged\s+Attack[^.]*suffer\s+-1\s+Attack",RegexOptions.IgnoreCase);
                if(firstPlus){if(UseLocationKey0858(from,$"{Location0858Prefix}R{Round}:RANGEDPLUS"))mod++;}
                else if(Regex.IsMatch(t,@"ranged\s+attacks?[^.]*gain\s+\+1\s+Attack",RegexOptions.IgnoreCase))mod++;
                if(firstMinus){if(UseLocationKey0858(from,$"{Location0858Prefix}R{Round}:RANGEDMINUS"))mod--;}
                else if(Regex.IsMatch(t,@"ranged\s+attacks?[^.]*suffer\s+-1\s+Attack",RegexOptions.IgnoreCase))mod--;
            }
            return mod;
        }
        private int LocationHeroArmorModifier0858(HeroState hero)
        {
            int mod=LocationStatusModifier0858(hero,"Location Armor +");if(hero==null||!Locations.TryGetValue(hero.LocationId??string.Empty,out var l))return mod;string t=LocationDef0858(l)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(t))return mod;
            if(Regex.IsMatch(t,@"Heroes?\s+here\s+gain\s+\+1\s+Armor",RegexOptions.IgnoreCase)&&!Has0858(t,"first Enemy Attack"))mod++;
            return mod;
        }
        private bool EnemyMatches0858(EnemyState enemy,params string[] tokens)
        {
            if(enemy==null)return false;string text=(enemy.DisplayName??string.Empty)+" ";if(SearchEncounterContent0820.EnemyTemplates.TryGetValue(enemy.TemplateId??string.Empty,out var d))text+=(d.Traits??string.Empty)+" "+(d.Name??string.Empty);return tokens.Any(x=>Has0858(text,x));
        }
        private bool PartyHasLoot0858(string lootId)=>ParticipatingHeroes.Any(h=>h.LootInventory.Contains(lootId,StringComparer.OrdinalIgnoreCase));
        private int LocationEnemyAttackModifier0858(EnemyState enemy)
        {
            if(enemy==null||!Locations.TryGetValue(enemy.LocationId??string.Empty,out var l))return 0;string t=LocationDef0858(l)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(t))return 0;int mod=0;
            if(Regex.IsMatch(t,@"Enemies?\s+here\s+gain\s+\+1\s+[Aa]ttack",RegexOptions.IgnoreCase))mod++;
            if(Has0858(t,"Goblins here gain +1 attack")&&EnemyMatches0858(enemy,"Goblin"))mod++;
            if(Has0858(t,"Warg/Wolf enemies here gain +1 Attack")&&Round==1&&EnemyMatches0858(enemy,"Warg","Wolf"))mod++;
            if(Has0858(t,"Cult and Veil enemies here gain +1 Attack")&&EnemyMatches0858(enemy,"Cult","Veil")&&QuestRuntime0856!=null&&QuestRuntime0856.Goals.Any(g=>!g.Complete&&g.Label.IndexOf("Ritual",StringComparison.OrdinalIgnoreCase)>=0))mod++;
            if((Has0858(t,"Undead")||Has0858(t,"Demonic"))&&Has0858(t,"-1 Attack")&&EnemyMatches0858(enemy,"Undead","Demon"))mod--;
            return mod;
        }
        private int LocationEnemyAttackVsTargetModifier0858(EnemyState enemy,HeroState target)
        {
            if(enemy==null||target==null||!Locations.TryGetValue(enemy.LocationId??string.Empty,out var l))return 0;string t=LocationDef0858(l)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(t))return 0;int mod=0;
            if(Has0858(t,"first Enemy Attack here each Round gains +1 Attack")&&Sunfall0857>=6&&!target.HasStatus("Guarded")&&UseLocationKey0858(l,$"{Location0858Prefix}R{Round}:FIRSTENEMYATTACK"))mod++;
            if(Has0858(t,"first Enemy targeting a Hero here each round suffers -1 Attack")&&UseLocationKey0858(l,$"{Location0858Prefix}R{Round}:FEYGLAMOUR"))mod--;
            if(Has0858(t,"+1 Armor against the first Undead Attack each Round")&&EnemyMatches0858(enemy,"Undead")&&UseLocationKey0858(l,$"{Location0858Prefix}R{Round}:{target.Id}:UNDEADARMOR"))mod--;
            return mod;
        }
        private int LocationEnemyArmorModifier0858(EnemyState enemy)
        {
            if(enemy==null||!Locations.TryGetValue(enemy.LocationId??string.Empty,out var l))return 0;string t=LocationDef0858(l)?.ongoingEffect??string.Empty;if(!LocationConditionActive0858(t))return 0;int mod=0;
            if(Regex.IsMatch(t,@"Enemies?\s+here\s+gain\s+\+1\s+Armor",RegexOptions.IgnoreCase))mod++;
            if(Has0858(t,"Goblins here gain +1 Armor")&&EnemyMatches0858(enemy,"Goblin"))mod++;
            if(Has0858(t,"Drowned enemies here gain +1 Armor")&&EnemyMatches0858(enemy,"Drowned")&&!l.Searched&&HasUnresolvedLocationQuestObjective0858(l))mod++;
            if(Has0858(t,"Stone Echo enemies here have -1 Armor")&&EnemyMatches0858(enemy,"Stone Echo")&&!PartyHasLoot0858("LT169"))mod--;
            return mod;
        }

        private void RestoreRegionalEncounterState0858(AscendanceSaveData save)
        {
            RegionalEncounterDeckKey0858=save?.RegionalEncounterDeckKey0858;RegionalEncounterDeck0858.Clear();RegionalEncounterDiscard0858.Clear();
            if(save?.RegionalEncounterDeckIds0858!=null)RegionalEncounterDeck0858.AddRange(save.RegionalEncounterDeckIds0858.Where(SearchEncounterContent0820.EncounterCards.ContainsKey));
            if(save?.RegionalEncounterDiscardIds0858!=null)RegionalEncounterDiscard0858.AddRange(save.RegionalEncounterDiscardIds0858.Where(SearchEncounterContent0820.EncounterCards.ContainsKey));
        }

        // Validator/debug helpers.
        private LocationState FindLocationByAuthored0858Internal(string authoredId)
        {
            if(string.IsNullOrWhiteSpace(authoredId))return null;
            // Authored IDs are canonical content IDs. Stonebrook keeps legacy runtime graph IDs
            // for save compatibility, so runtime Id == authoredId is not sufficient.
            return Locations.Values.FirstOrDefault(l=>string.Equals(l.AuthoredLocationId,authoredId,StringComparison.OrdinalIgnoreCase));
        }

        private LocationState DebugLocationByAuthored0858Internal(string authoredId)
        {
            if(string.IsNullOrWhiteSpace(authoredId))return null;
            var authored=FindLocationByAuthored0858Internal(authoredId);
            if(authored!=null)return authored;

            // The full Location validator intentionally exercises authored tiles that are not part of
            // the legacy Stonebrook vertical-slice graph. Materialize a validator-only runtime state
            // from the catalog instead of falling through to an unrelated colliding legacy runtime ID.
            if(!FullContentCatalog0850.TryGetLocation(authoredId,out var d)||d==null)return null;
            string runtimeId=$"DBG_{authoredId.ToUpperInvariant()}";
            if(Locations.TryGetValue(runtimeId,out var existing))return existing;
            int n=0;int.TryParse(Regex.Match(authoredId,@"\d+").Value,out n);
            var l=new LocationState
            {
                Id=runtimeId,AuthoredLocationId=authoredId.ToUpperInvariant(),Name=d.name,Terrain=d.terrain,Traits=d.traits,Keywords=d.keywords,
                RevealEffect=d.revealEffect,FirstVisitEffect=d.firstVisitEffect,OngoingEffect=d.ongoingEffect,
                Position=new Vector3(80f+(n%12)*6f,0f,80f+(n/12)*6f),Revealed=true,RevealResolved=true
            };
            Locations[runtimeId]=l;
            return l;
        }

        public LocationState DebugLocationByAuthored0858(string authoredId)=>DebugLocationByAuthored0858Internal(authoredId);
        public int DebugLocationCount0858()=>FullContentCatalog0850.Locations.Count(l=>l!=null&&IsLocationExecutable0858(l.id));
        public bool DebugEnsureQuestLocations0858(string questId){EnsureQuestRuntimeLocations0856(questId);return FullContentCatalog0850.QuestLocationPoolIds(questId).All(id=>FindLocationByAuthored0858Internal(id)!=null);}
        public int DebugLocationMoveSurcharge0858(string heroId,string destinationAuthoredId){var l=DebugLocationByAuthored0858Internal(destinationAuthoredId);return Heroes.TryGetValue(heroId??string.Empty,out var h)&&l!=null?LocationMoveSurcharge0858(h,l.Id):-1;}
        public bool DebugCanSearchLocation0858(string heroId,string authoredId,bool granted=false){var l=DebugLocationByAuthored0858Internal(authoredId);return Heroes.TryGetValue(heroId??string.Empty,out var h)&&l!=null&&CanSearchLocation0858(h,l,granted);}
        public void DebugResolveLocationReveal0858(string authoredId,string heroId){var l=DebugLocationByAuthored0858Internal(authoredId);if(l!=null&&Heroes.TryGetValue(heroId??string.Empty,out var h))ResolveLocationReveal0858(l,h);}
        public void DebugResolveLocationFirstVisit0858(string authoredId,string heroId){var l=DebugLocationByAuthored0858Internal(authoredId);if(l!=null&&Heroes.TryGetValue(heroId??string.Empty,out var h))ResolveLocationFirstVisit(l,h);}
        public bool DebugResolveLocationSearch0858(string authoredId,string heroId){var l=DebugLocationByAuthored0858Internal(authoredId);return l!=null&&Heroes.TryGetValue(heroId??string.Empty,out var h)&&ResolveLocationSearch0858(l,h);}
        public void DebugLocationSearchResolved0858(string authoredId,string heroId){var l=DebugLocationByAuthored0858Internal(authoredId);if(l!=null&&Heroes.TryGetValue(heroId??string.Empty,out var h))OnLocationSearchResolved0858(l,h);}
        public void DebugLocationEnter0858(string heroId,string authoredId){var l=DebugLocationByAuthored0858Internal(authoredId);if(l!=null&&Heroes.TryGetValue(heroId??string.Empty,out var h)){h.LocationId=l.Id;OnLocationHeroEntered0858(h,l);}}
        public void DebugLocationTurnStart0858(string heroId){if(Heroes.TryGetValue(heroId??string.Empty,out var h))OnLocationHeroTurnStarted0858(h);}
        public void DebugLocationEndActivation0858(string heroId){if(Heroes.TryGetValue(heroId??string.Empty,out var h))ResolveLocationEndHeroActivation0858(h);}
        public int DebugLocationHeroArmor0858(string heroId)=>Heroes.TryGetValue(heroId??string.Empty,out var h)?LocationHeroArmorModifier0858(h):-999;
        public int DebugLocationRangeBonus0858(string heroId,int baseRange)=>Heroes.TryGetValue(heroId??string.Empty,out var h)?LocationRangeBonus0858(h,baseRange):-999;
        public int DebugModifyLocationHealing0858(string heroId,int amount,string source="Validation")=>Heroes.TryGetValue(heroId??string.Empty,out var h)?ModifyLocationHealing0858(h,amount,source):-999;
        public bool DebugLocationDifficultTerrain0858(string authoredId){var l=DebugLocationByAuthored0858Internal(authoredId);return l!=null&&LocationDifficultTerrain0858(l.Id);}
        public bool DebugHasLocationToken0858(string authoredId,string kind)=>HasLocationToken0858(DebugLocationByAuthored0858Internal(authoredId),kind);
        public void DebugPlaceLocationToken0858(string authoredId,string kind){var l=DebugLocationByAuthored0858Internal(authoredId);if(l!=null)PlaceLocationToken0858(l,kind,"0.8.58 validation");}
        public bool DebugCanLocationUtility0858(string heroId)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&CanLocationUtility0858(h);
        public bool DebugResolveLocationUtility0858(string heroId)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&ResolveLocationUtility0858(h);
        public int DebugLocationUsedKeyCount0858(string authoredId)=>DebugLocationByAuthored0858Internal(authoredId)?.UsedKeys0858.Count??-1;
        public string DebugRegionalDeckKey0858()=>RegionalEncounterDeckKey0858??string.Empty;
        public int DebugRegionalDeckCount0858(string authoredId){var l=DebugLocationByAuthored0858Internal(authoredId);if(l==null)return -1;EnsureRegionalEncounterDeck0858(l);return RegionalEncounterDeck0858.Count;}
        public string DebugActiveEncounterTop0858(string authoredId){var l=DebugLocationByAuthored0858Internal(authoredId);if(l==null)return null;return ActiveEncounterDeckForLocation0858(l).FirstOrDefault();}
        public int DebugLocationEnemyAttackVsTarget0858(string enemyId,string heroId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)&&Heroes.TryGetValue(heroId??string.Empty,out var h)?LocationEnemyAttackVsTargetModifier0858(e,h):0;
    }
}
