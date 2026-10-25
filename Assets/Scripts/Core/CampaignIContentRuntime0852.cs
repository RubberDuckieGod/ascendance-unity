using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    // 0.8.52a — executes all authored Search cards. Campaign I Location-card-owned effects remain dormant until 0.8.52b. Existing specialized runtimes remain authoritative.
    public sealed partial class AscendanceGameManager
    {
        private int searchTravelSeverityModifier0852;
        private int searchTravelSeverityRound0852 = -1;
        private int searchGoblinSeverityModifier0852;
        private int searchGoblinSeverityRound0852 = -1;
        private bool searchConditionWard0852;
        private int searchConditionWardRound0852 = -1;
        private string pendingEncounterPostResolve0852;
        private bool pendingEncounterRequireBeast0852;
        private bool pendingEncounterGuardIfBeast0852;
        private bool pendingEncounterDrawHeroIfScouts0852;
        private bool pendingEncounterAmbushPlusOne0852;
        private readonly Dictionary<string,int> exploreBlockedAtLocationRound0852 = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> rubbleLocations0852 = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> deferredPostEncounter0852 = new();

        public bool IsSearchCardExecutable0852(string id)
        {
            if(string.IsNullOrWhiteSpace(id))return false;
            var m=Regex.Match(id,@"^SD(\d{3})$",RegexOptions.IgnoreCase);
            return m.Success&&int.TryParse(m.Groups[1].Value,out int n)&&n>=1&&n<=100;
        }

        private bool ResolveCampaignISearch0852(SearchCardDefinition card,LocationState location,HeroState hero)
        {
            if(card==null||hero==null)return false;
            if(!Regex.IsMatch(card.Id??string.Empty,@"^SD(?:0[2-9]\d|100)$",RegexOptions.IgnoreCase))return false;
            int n=int.Parse(card.Id.Substring(2));
            if(n<20||n>100)return false;
            switch(card.Id.ToUpperInvariant())
            {
                case "SD020": BeginHeroRecoveryChoice0852(card,location,hero,2,true); break;
                case "SD021": BeginConnectedRevealChoice0852(card,location,hero,"SearchRevealConnected0852",true); break;
                case "SD022":
                    if(!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==hero.LocationId))
                    { hero.FreeMoveReady=true; AddLog($"SEARCH EFFECT — {card.Name}: {hero.DisplayName}'s next connected Move this turn costs no additional Action."); }
                    else AddLog($"SEARCH EFFECT — {card.Name}: {hero.DisplayName} is Engaged, so the immediate free Move is unavailable.");
                    break;
                case "SD023": AddPartyGold(15,card.Name); if(HasLocationText0852(location,"Ancient","Ruins"))DrawLootToHero(hero,card.Name); break;
                case "SD024": DrawCards(hero,1); if(hero.Hand.Count<=5)HealHero(hero,1,card.Name,hero); break;
                case "SD025": AddPartyGold(20,card.Name); BeginEncounterDeckControl(card.Id,card.Name,"MayBottomTop",1); break;
                case "SD026": HealHero(hero,2,card.Name,hero); BeginExploreTopChoice0852(card,hero,"SearchExploreTopDiscard0852",1,"Look at the top Exploration card. Keep it on top or discard it."); break;
                case "SD027": KeepSearchEffect0852(hero,card.Id,card.Name); break;
                case "SD028": DrawLootToHero(hero,card.Name); DrawLootToHero(hero,card.Name); AddPartyGold(25,card.Name); break;
                case "SD029": AddPartyGold(25,card.Name); DrawLootToHero(hero,card.Name); if(!QuestComplete&&!QuestFailed&&(CurrentQuestId0829=="Q001"||IsSiegeQuest0852()))AddPartyGold(10,card.Name+" — Stonebrook under attack"); break;
                case "SD030": DrawEquipmentToHero0852(hero,card.Name); break;
                case "SD031": BeginRewardChoice0827(card.Id,card.Name,hero,"SearchDrawOrHeal0852","Choose: draw 1 Hero card or recover 1 Health."); break;
                case "SD032": InspectConnectedLocations0852(location,2,card.Name); break;
                case "SD033": hero.FreeMoveReady=true; AddLog($"SEARCH EFFECT — {card.Name}: the next Move Action by {hero.DisplayName} this turn costs 0 base Actions (terrain surcharges still apply)."); break;
                case "SD034": BeginRewardChoice0827(card.Id,card.Name,hero,"SearchHeal2OrPoison0852","Choose: recover 2 Health or remove Poisoned."); break;
                case "SD035": ResolveTargetedCleanse0852(location,hero,card.Name,new[]{"Poisoned","Bleeding"},1); break;
                case "SD036": foreach(var h in HeroesAt0852(location))HealHero(h,1,card.Name,hero); exploreBlockedAtLocationRound0852[location.Id]=Round; AddLocationTag(location.Id,"Explore Blocked",card.Name,hero,StatusExpiryTiming.EndOfRound,1); AddLog($"SEARCH EFFECT — {location.Name} cannot be used as the origin of another normal Explore this round."); break;
                case "SD037": BeginExploreTopChoice0852(card,hero,"SearchExploreTop3Order0852",3,"Put the top 3 Exploration cards back in any order."); break;
                case "SD038": AddStatus(hero,"Guarded",0,1,card.Name,true,hero,"until the end of the round"); break;
                case "SD039": AddStatus(hero,"Inspired",0,0,card.Name,true,hero,"until the end of their next turn"); break;
                case "SD040": if(hero.HasStatus("Guarded"))DrawCards(hero,1); else AddStatus(hero,"Guarded",0,1,card.Name,true,hero,"until the end of the round"); break;
                case "SD041": BeginEncounterDeckControl(card.Id,card.Name,"MayBottomTop",1); break;
                case "SD042": QueueRoundTravelSeverity0852(-1,false,card.Name); break;
                case "SD043": foreach(var h in HeroesAt0852(location))AddStatus(h,"Prepared Position",0,1,card.Name,true,hero,"until the end of the round"); break;
                case "SD044": TryGrantAdditionalHeroAction(hero,1,card.Name); break;
                case "SD045": DrawCards(hero,1); AddLog($"SEARCH EFFECT — {card.Name} will be placed on the bottom of the Search deck instead of discarded."); break;
                case "SD046": AddStatus(hero,"Rooted",0,0,card.Name,false,hero,"until the end of their turn"); break;
                case "SD047": ApplyHeroDamageNow(hero,1,card.Name); break;
                case "SD048": AddStatus(hero,"Poisoned",0,0,card.Name,false,hero); break;
                case "SD049": ApplyHeroDamageNow(hero,1,card.Name); if(hero.Actions>0){hero.Actions--;AddLog($"SEARCH EFFECT — {hero.DisplayName} loses 1 remaining Action ({card.Name}).");} break;
                case "SD050": ApplyHeroDamageNow(hero,1,card.Name); ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD051": foreach(var h in HeroesAt0852(location).ToList())ApplyHeroDamageNow(h,1,card.Name); break;
                case "SD052": foreach(var h in HeroesAt0852(location))AddStatus(h,"Slowed",0,0,card.Name,false,hero,"until the end of their next turn"); break;
                case "SD053": AddStatus(hero,"Rooted",0,0,card.Name,false,hero); AddLog($"SEARCH UTILITY — {hero.DisplayName} may spend 1 Action using the Location Option button to remove Rooted immediately."); break;
                case "SD054": AddStatus(hero,"Search No Move",0,0,card.Name,false,hero,"until the end of their turn"); break;
                case "SD055": AddStatus(hero,"Burning",0,0,card.Name,false,hero); break;
                case "SD056": AddStatus(hero,"Weakened",0,0,card.Name,false,hero); DrawLootToHero(hero,card.Name); break;
                case "SD057": AddPartyGold(10,card.Name); ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD058": rubbleLocations0852.Add(location.Id); AddLocationTag(location.Id,"Search Rubble",card.Name,hero,StatusExpiryTiming.Persistent); AddLog($"SEARCH HAZARD — the next Hero leaving {location.Name} pays +1 Action; the Rubble is then removed."); break;
                case "SD059": ApplyHeroDamageNow(hero,1,card.Name); AddStatus(hero,"No Healing",0,0,card.Name,false,hero,"until the end of their turn"); break;
                case "SD060": ResolveSearchTravelEncounter0852(location,hero,1,card.Name); break;
                case "SD061": ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD062": pendingEncounterAmbushPlusOne0852=true; ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD063": ResolveSearchTravelEncounter0852(location,hero,1,card.Name); break;
                case "SD064": ResolveSearchTravelEncounter0852(location,hero,0,card.Name,"SEARCH:BEAST_GOLD10"); break;
                case "SD065": ResolveSearchTravelEncounter0852(location,hero,0,card.Name,"SEARCH:GOBLIN_GOLD10"); break;
                case "SD066": ResolveSearchTravelEncounter0852(location,hero,-1,card.Name); break;
                case "SD067": ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD068": pendingEncounterGuardIfBeast0852=true; ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD069": ResolveSearchTravelEncounter0852(location,hero,1,card.Name,"SEARCH:LOOT1"); break;
                case "SD070": ResolveSearchTravelEncounter0852(location,hero,0,card.Name,"SEARCH:GOLD15"); break;
                case "SD071": foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))DrawCards(h,1); ResolveSearchTravelEncounter0852(location,hero,1,card.Name); break;
                case "SD072": ResolveSearchTravelEncounter0852(location,hero,2,card.Name); break;
                case "SD073": pendingEncounterRequireBeast0852=true; ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD074": pendingEncounterDrawHeroIfScouts0852=true; ResolveSearchTravelEncounter0852(location,hero,0,card.Name); break;
                case "SD075": ResolveSearchTravelEncounter0852(location,hero,1,card.Name); AddLog("SEARCH EFFECT — voluntary Retreat is unavailable until an Enemy from this Encounter is defeated (the current digital rules have no separate voluntary Retreat action)."); break;
                case "SD076": BeginRewardChoice0827(card.Id,card.Name,hero,"SearchHelpHunter0852","Choose: spend 1 additional Action to help for 20 Gold and inspect the top Encounter card, or leave them."); break;
                case "SD077": BeginRewardChoice0827(card.Id,card.Name,hero,"SearchTinker0852","Pay 15 Gold to draw 2 Loot and keep 1, or decline."); break;
                case "SD078": BeginRewardChoice0827(card.Id,card.Name,hero,"SearchRefugee0852","Choose: escort the Refugee to Stonebrook later for 10 Gold, or recover 1 Health now."); break;
                case "SD079": BeginConnectedRevealChoice0852(card,location,hero,"SearchScoutReveal0852",false); break;
                case "SD080": BeginMerchantDiscount0852(card,hero); break;
                case "SD081": QueueRoundTravelSeverity0852(-1,false,card.Name); break;
                case "SD082": BeginRewardChoice0827(card.Id,card.Name,hero,"SearchDeserter0852","Choose: gain 25 Gold, or resolve a Travel Encounter at -1 Severity and gain Loot afterward."); break;
                case "SD083": ResolveTargetedCleanse0852(location,hero,card.Name,new[]{"Poisoned","Bleeding","Burning"},2); break;
                case "SD084": BeginHeroStatusChoice0852(card,location,hero,"Inspired"); break;
                case "SD085": BeginExploreTopChoice0852(card,hero,"SearchExploreChoose2Resolve0852",2,"Choose 1 of the top 2 Exploration cards to resolve; discard the other."); break;
                case "SD086": AddPartyGold(10,card.Name); if(CurrentWorldEventId0833=="WE001"||CurrentWorldEventId0833=="WE002")BeginEncounterDeckControl(card.Id,card.Name,"MayBottomTop",1); break;
                case "SD087": if(IsSiegeQuest0852()){AddPartyGold(20,card.Name);} else foreach(var h in HeroesAt0852(location))HealHero(h,1,card.Name,hero); break;
                case "SD088": DrawLootToHero(hero,card.Name); AddStatus(hero,"Inspired",0,0,card.Name,true,hero,"until the end of their next turn"); break;
                case "SD089": KeepSearchEffect0852(hero,card.Id,card.Name); break;
                case "SD090": ResolveSearchTravelEncounter0852(location,hero,0,card.Name,"SEARCH:GOLD25"); break;
                case "SD091": if(CurrentQuestId0829=="Q006"||CurrentQuestId0829=="Q009")TryGrantAdditionalHeroAction(hero,1,card.Name); else DrawLootToHero(hero,card.Name); break;
                case "SD092": searchConditionWard0852=true; searchConditionWardRound0852=Round; AddLog($"SEARCH EFFECT — {card.Name}: the next Burning, Poisoned, or Weakened that would be gained by a Hero this round is ignored."); break;
                case "SD093": AddPartyGold(20,card.Name); QueueRoundTravelSeverity0852(-1,true,card.Name); break;
                case "SD094": BeginEncounterDeckControl(card.Id,card.Name,"ReorderTop3",3); AddPartyGold(10,card.Name); break;
                case "SD095": DrawCards(hero,1); HealHero(hero,1,card.Name,hero); break;
                case "SD096": BeginConnectedRevealChoice0852(card,location,hero,"SearchRevealStrategic0852",false); break;
                case "SD097": AddPartyGold(15,card.Name); if(CurrentQuestId0829=="Q002")AddPartyGold(10,card.Name+" — Q002 bonus"); break;
                case "SD098": if(CurrentQuestId0829=="Q003"){AddStatus(hero,"Beacon Instructions",0,1,card.Name,true,hero,"until the end of the round");AddLog("SEARCH EFFECT — Q003 Beacon may be lit for 1 fewer Action this round.");} else DrawCards(hero,1); break;
                case "SD099": AddPartyGold(30,card.Name); DrawLootToHero(hero,card.Name); if(CurrentWorldEventId0833=="WE004")foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))DrawCards(h,1); break;
                case "SD100": foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead)){HealHero(h,2,card.Name,hero);RemoveFirstHarmfulStatus(h,card.Name);} break;
            }
            return true;
        }

        private IEnumerable<HeroState> HeroesAt0852(LocationState location)=>location==null?Enumerable.Empty<HeroState>():ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id);
        private static bool HasLocationText0852(LocationState location,params string[] tokens)
        {
            if(location==null)return false;string all=$"{location.Name};{location.Terrain};{location.Traits};{location.Keywords}";
            return tokens.Any(t=>all.IndexOf(t,StringComparison.OrdinalIgnoreCase)>=0);
        }
        private bool IsSiegeQuest0852()=>CurrentQuestId0829=="Q007"||CurrentQuestId0829=="Q008"||CurrentQuestId0829=="Q009"||CurrentQuestId0829=="Q010";

        private void DrawEquipmentToHero0852(HeroState hero,string source)
        {
            string id=TakeTopEquipmentForChoice(source);if(!string.IsNullOrEmpty(id))KeepHeldEquipment(hero,id,source);
        }
        private void BeginHeroRecoveryChoice0852(SearchCardDefinition card,LocationState location,HeroState hero,int heal,bool cleanse)
        {
            var ids=HeroesAt0852(location).Select(h=>h.Id).ToList();
            BeginRewardChoice0827(card.Id,card.Name,hero,$"SearchHeroRecovery{heal}0852",$"Choose a Hero here to recover {heal} Health{(cleanse?" or remove 1 negative Condition":"") }.",ids);
        }
        private void BeginHeroStatusChoice0852(SearchCardDefinition card,LocationState location,HeroState hero,string status)
        {
            BeginRewardChoice0827(card.Id,card.Name,hero,$"SearchHeroStatus:{status}", $"Choose one Hero here to gain {status}.",HeroesAt0852(location).Select(h=>h.Id));
        }
        private void ResolveTargetedCleanse0852(LocationState location,HeroState actor,string source,string[] statuses,int fallbackHeal)
        {
            var targets=HeroesAt0852(location).Where(h=>statuses.Any(s=>h.HasStatus(s))).ToList();
            if(targets.Count==0){HealHero(actor,fallbackHeal,source,actor);return;}
            var target=targets[0];var status=statuses.First(s=>target.HasStatus(s));RemoveStatus(target.Statuses,status,target.DisplayName,source);
        }

        private void KeepSearchEffect0852(HeroState hero,string id,string source)
        {
            if(hero==null)return;
            if(hero.KeptSearchEffects0852==null)hero.KeptSearchEffects0852=new List<string>();
            if(!hero.KeptSearchEffects0852.Contains(id,StringComparer.OrdinalIgnoreCase))hero.KeptSearchEffects0852.Add(id);
            AddLog($"SEARCH EFFECT KEPT — {hero.DisplayName} keeps {id} {source} for its later discard effect.");
        }

        private void QueueRoundTravelSeverity0852(int modifier,bool goblinOnly,string source)
        {
            if(goblinOnly){searchGoblinSeverityModifier0852+=modifier;searchGoblinSeverityRound0852=Round;}
            else {searchTravelSeverityModifier0852+=modifier;searchTravelSeverityRound0852=Round;}
            AddLog($"SEARCH EFFECT — {source}: next {(goblinOnly?"Goblin ":string.Empty)}Travel Encounter this round gets {modifier:+#;-#;0} Severity (minimum 1).");
        }

        private void ResolveSearchTravelEncounter0852(LocationState location,HeroState hero,int modifier,string source,string postResolve=null)
        {
            pendingEncounterPostResolve0852=postResolve;
            int level=ExtractNumber(location?.FirstVisitEffect,@"Encounter Level\s+(\d+)");if(level<=0)level=1;
            ResolveTravelEncounter(location,hero,level,modifier,source);
        }

        private EncounterCardDefinition AdjustEncounterDrawForSearch0852(EncounterCardDefinition card,LocationState location)
        {
            if(card==null)return null;
            if(pendingEncounterRequireBeast0852)
            {
                pendingEncounterRequireBeast0852=false;
                if(!IsBeastEncounter0852(card))
                {
                    EncounterDeck.Add(card.Id);AddLog($"SEARCH EFFECT — {card.Id} {card.Name} is not a Beast encounter; it is placed on the bottom and one replacement is drawn.");
                    var replacement=DrawEligibleEncounterCard(location);if(replacement!=null)card=replacement;
                }
            }
            return card;
        }

        private int ConsumeSearchEncounterSeverityModifier0852(EncounterCardDefinition card)
        {
            int mod=0;
            if(searchTravelSeverityRound0852==Round){mod+=searchTravelSeverityModifier0852;searchTravelSeverityModifier0852=0;searchTravelSeverityRound0852=-1;}
            if(searchGoblinSeverityRound0852==Round&&EncounterContainsGoblins0852(card)){mod+=searchGoblinSeverityModifier0852;searchGoblinSeverityModifier0852=0;searchGoblinSeverityRound0852=-1;}
            if(pendingEncounterAmbushPlusOne0852){if(string.Equals(card?.EncounterType,"Ambush",StringComparison.OrdinalIgnoreCase))mod+=1;pendingEncounterAmbushPlusOne0852=false;}
            return mod;
        }
        private static bool IsBeastEncounter0852(EncounterCardDefinition card)=>string.Equals(card?.EncounterType,"Beast",StringComparison.OrdinalIgnoreCase);
        private static bool EncounterContainsScouts0852(EncounterCardDefinition card)=>card!=null&&($"{card.Name};{card.Severity12};{card.Severity34};{card.Severity56};{card.Severity7Plus}").IndexOf("Scout",StringComparison.OrdinalIgnoreCase)>=0;
        private static bool EncounterContainsGoblins0852(EncounterCardDefinition card)
        {
            if(card==null)return false;if((card.Name??string.Empty).IndexOf("Goblin",StringComparison.OrdinalIgnoreCase)>=0)return true;
            string text=$"{card.Severity12};{card.Severity34};{card.Severity56};{card.Severity7Plus}";
            foreach(Match m in Regex.Matches(text,@"E\d{3}",RegexOptions.IgnoreCase))if(SearchEncounterContent0820.EnemyTemplates.TryGetValue(m.Value,out var e)&&(e.Name??string.Empty).IndexOf("Goblin",StringComparison.OrdinalIgnoreCase)>=0)return true;
            return false;
        }
        private void ApplySearchEncounterPreSetup0852(EncounterCardDefinition card,HeroState sourceHero)
        {
            if(pendingEncounterGuardIfBeast0852){if(IsBeastEncounter0852(card))foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))AddStatus(h,"Guarded",0,1,"Scattered Bones",true,sourceHero,"until the end of the round");pendingEncounterGuardIfBeast0852=false;}
            if(pendingEncounterDrawHeroIfScouts0852){if(EncounterContainsScouts0852(card)&&sourceHero!=null)DrawCards(sourceHero,1);pendingEncounterDrawHeroIfScouts0852=false;}
        }
        private string ConsumePendingEncounterPost0852(){string value=pendingEncounterPostResolve0852;pendingEncounterPostResolve0852=null;return value;}

        private void ResolvePostEncounter0852(EncounterRuntimeState encounter)
        {
            if(encounter==null||string.IsNullOrWhiteSpace(encounter.PostResolveEffect0852))return;
            string key=encounter.PostResolveEffect0852;encounter.PostResolveEffect0852=null;
            if(HasPendingRewardChoice){deferredPostEncounter0852.Add(key+"|"+(encounter.SourceHeroId??string.Empty));AddLog($"LOCATION/SEARCH REWARD QUEUED — {key} waits for the current reward choice to resolve.");return;}
            var hero=RewardHeroFor(encounter.SourceHeroId);
            if(key.StartsWith("SEARCH:",StringComparison.OrdinalIgnoreCase))
            {
                switch(key.Substring(7).ToUpperInvariant())
                {
                    case "BEAST_GOLD10": if(SearchEncounterContent0820.EncounterCards.TryGetValue(encounter.CardId,out var beast)&&IsBeastEncounter0852(beast))AddPartyGold(10,"Search Beast reward"); break;
                    case "GOBLIN_GOLD10": if(SearchEncounterContent0820.EncounterCards.TryGetValue(encounter.CardId,out var goblin)&&EncounterContainsGoblins0852(goblin))AddPartyGold(10,"Search Goblin reward"); break;
                    case "LOOT1": DrawLootToHero(hero,"Search post-Encounter reward"); break;
                    case "GOLD15": AddPartyGold(15,"Search post-Encounter reward"); break;
                    case "GOLD25": AddPartyGold(25,"Search post-Encounter reward"); break;
                    case "LOOT_AFTER_MINUS1": DrawLootToHero(hero,"Goblin Deserter post-Encounter reward"); break;
                }
                return;
            }
        }

        private void TryResolveDeferredPostEncounter0852()
        {
            if(HasPendingRewardChoice||deferredPostEncounter0852.Count==0)return;
            string entry=deferredPostEncounter0852[0];
            deferredPostEncounter0852.RemoveAt(0);
            int split=entry.LastIndexOf('|');
            string key=split>=0?entry.Substring(0,split):entry;
            string heroId=split>=0?entry.Substring(split+1):string.Empty;
            var fake=new EncounterRuntimeState{PostResolveEffect0852=key,SourceHeroId=heroId};
            ResolvePostEncounter0852(fake);
        }

        private void BeginConnectedRevealChoice0852(SearchCardDefinition card,LocationState location,HeroState hero,string mode,bool goblinBonus)
        {
            var candidates=location?.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&!l.Revealed&&IsLocationEligibleForCurrentQuest0829(id)).ToList()??new List<string>();
            var ids=string.Equals(mode,"SearchScoutReveal0852",StringComparison.OrdinalIgnoreCase)?candidates.Take(2).ToList():candidates;
            if(ids.Count==0){AddLog($"SEARCH EFFECT — {card.Name}: no connected unrevealed Location is available.");return;}
            if(ids.Count==1){ResolveConnectedReveal0852(ids[0],hero,card.Name,goblinBonus,mode=="SearchRevealStrategic0852");return;}
            BeginRewardChoice0827(card.Id,card.Name,hero,mode,"Choose a connected unrevealed Location.",ids);
        }
        private void ResolveConnectedReveal0852(string runtimeId,HeroState hero,string source,bool goblinBonus,bool strategicBonus)
        {
            if(!Locations.TryGetValue(runtimeId??string.Empty,out var l)||l.Revealed)return;
            RevealExplorationLocation0828(l,hero,true,source);
            if(goblinBonus&&HasLocationText0852(l,"Goblin","Fortress"))DrawLootToHero(hero,source+" — Goblin Structure bonus");
            if(strategicBonus&&(string.Equals(l.AuthoredLocationId,"L023",StringComparison.OrdinalIgnoreCase)||string.Equals(l.AuthoredLocationId,"L031",StringComparison.OrdinalIgnoreCase)||HasLocationText0852(l,"Fortress")))TryGrantAdditionalHeroAction(hero,1,source);
            AddLog($"SEARCH REVEAL — {source} reveals {l.Name} without an additional Action.");
        }
        private void InspectConnectedLocations0852(LocationState location,int count,string source)
        {
            var list=location?.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&!l.Revealed&&IsLocationEligibleForCurrentQuest0829(id)).Take(Mathf.Max(1,count)).Select(ExplorationLocationLabel0828).ToList()??new List<string>();
            AddLog(list.Count==0?$"SEARCH SCOUT — {source}: no connected unrevealed Location is available.":$"SEARCH SCOUT — {source} inspects: {string.Join(" | ",list)}. They remain unrevealed.");
        }
        private void BeginExploreTopChoice0852(SearchCardDefinition card,HeroState hero,string mode,int count,string prompt)
        {
            var ids=ExplorationDeck0828.Take(Mathf.Max(1,count)).ToList();if(ids.Count==0){AddLog($"{card.Name} — Exploration deck is empty.");return;}BeginRewardChoice0827(card.Id,card.Name,hero,mode,prompt,ids);
        }
        private void BeginMerchantDiscount0852(SearchCardDefinition card,HeroState hero)
        {
            var ids=new List<string>();
            ids.AddRange(LootDeck.Distinct(StringComparer.OrdinalIgnoreCase).Where(id=>RewardContent0826.Loot.TryGetValue(id,out var d)&&string.Equals(d.Rarity,"Common",StringComparison.OrdinalIgnoreCase)).Take(3).Select(id=>"L:"+id));
            ids.AddRange(EquipmentDeck.Distinct(StringComparer.OrdinalIgnoreCase).Where(id=>RewardContent0826.Equipment.TryGetValue(id,out var d)&&string.Equals(d.Rarity,"Common",StringComparison.OrdinalIgnoreCase)).Take(3).Select(id=>"E:"+id));
            BeginRewardChoice0827(card.Id,card.Name,hero,"SearchMerchantDiscount0852","Buy one shown Common Item/Equipment for 10 Gold less (minimum 0), or decline.",ids);
        }

        private void Append0852SearchChoiceOptions(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(state==null||string.IsNullOrEmpty(state.Mode))return;var hero=RewardHeroFor(state.HeroId);var ids=state.RevealedCardIds??new List<string>();
            if(state.Mode.StartsWith("SearchHeroRecovery",StringComparison.OrdinalIgnoreCase))
            {
                foreach(var id in ids)if(Heroes.TryGetValue(id,out var h)){options.Add(new RewardChoiceOption{Key=$"HEAL:{id}",Label=$"{h.DisplayName}: recover Health"});if(h.Statuses.Any(s=>s!=null&&!s.Beneficial))options.Add(new RewardChoiceOption{Key=$"CLEANSE:{id}",Label=$"{h.DisplayName}: remove 1 negative Condition"});}return;
            }
            if(state.Mode.StartsWith("SearchHeroStatus:",StringComparison.OrdinalIgnoreCase)){foreach(var id in ids)if(Heroes.TryGetValue(id,out var h))options.Add(new RewardChoiceOption{Key=id,Label=h.DisplayName});return;}
            switch(state.Mode)
            {
                case "SearchDrawOrHeal0852": options.Add(new RewardChoiceOption{Key="DRAW",Label="Draw 1 Hero card"});options.Add(new RewardChoiceOption{Key="HEAL",Label="Recover 1 Health"});break;
                case "SearchHeal2OrPoison0852": options.Add(new RewardChoiceOption{Key="HEAL",Label="Recover 2 Health"});if(hero?.HasStatus("Poisoned")==true)options.Add(new RewardChoiceOption{Key="CLEANSE",Label="Remove Poisoned"});break;
                case "SearchHelpHunter0852": if(hero!=null&&GeneralActions(hero)>=1)options.Add(new RewardChoiceOption{Key="HELP",Label="Spend 1 additional Action • gain 20 Gold • inspect Encounter"});options.Add(new RewardChoiceOption{Key="LEAVE",Label="Leave them"});break;
                case "SearchTinker0852": if(PartyGold>=15)options.Add(new RewardChoiceOption{Key="PAY",Label="Pay 15 Gold • draw 2 Loot, keep 1"});options.Add(new RewardChoiceOption{Key="DECLINE",Label="Decline"});break;
                case "SearchTinkerKeep0852": for(int i=0;i<ids.Count;i++)options.Add(new RewardChoiceOption{Key=$"KEEP{i}",Label=$"Keep {RewardContent0826.LootLabel(ids[i])}"});break;
                case "SearchRefugee0852": options.Add(new RewardChoiceOption{Key="ESCORT",Label="Escort to Stonebrook later • gain 10 Gold on arrival"});options.Add(new RewardChoiceOption{Key="HEAL",Label="Recover 1 Health now"});break;
                case "SearchDeserter0852": options.Add(new RewardChoiceOption{Key="GOLD",Label="Let deserter flee • gain 25 Gold"});options.Add(new RewardChoiceOption{Key="FIGHT",Label="Travel Encounter -1 Severity • Loot after"});break;
                case "SearchRevealConnected0852": case "SearchScoutReveal0852": case "SearchRevealStrategic0852": for(int i=0;i<ids.Count;i++)if(Locations.TryGetValue(ids[i],out var l))options.Add(new RewardChoiceOption{Key=$"REVEAL{i}",Label=$"Reveal {l.Name}"});break;
                case "SearchExploreTopDiscard0852": if(ids.Count>0){options.Add(new RewardChoiceOption{Key="KEEP",Label=$"Keep {ExplorationLocationLabel0828(ids[0])} on top"});options.Add(new RewardChoiceOption{Key="DISCARD",Label=$"Discard {ExplorationLocationLabel0828(ids[0])}"});}break;
                case "SearchExploreTop3Order0852": AddPermutationOptions0852(options,ids);break;
                case "SearchExploreChoose2Resolve0852": for(int i=0;i<ids.Count;i++)options.Add(new RewardChoiceOption{Key=$"CHOOSE{i}",Label=$"Resolve {ExplorationLocationLabel0828(ids[i])}"});break;
                case "SearchMerchantDiscount0852":
                    foreach(var raw in ids){if(raw.StartsWith("L:")&&RewardContent0826.Loot.TryGetValue(raw.Substring(2),out var ld))options.Add(new RewardChoiceOption{Key=raw,Label=$"Buy {ld.Name} • {Mathf.Max(0,ld.Value-10)} Gold"});else if(raw.StartsWith("E:")&&RewardContent0826.Equipment.TryGetValue(raw.Substring(2),out var ed))options.Add(new RewardChoiceOption{Key=raw,Label=$"Buy {EquipmentChoiceLabel0850(ed.Id)} • {Mathf.Max(0,ed.Value-10)} Gold"});}options.Add(new RewardChoiceOption{Key="DECLINE",Label="Decline purchase"});break;
            }
        }
        private static void AddPermutationOptions0852(List<RewardChoiceOption> options,List<string> ids)
        {
            if(ids.Count<2){if(ids.Count==1)options.Add(new RewardChoiceOption{Key="ORDER0",Label="Keep current order"});return;}
            if(ids.Count==2){options.Add(new RewardChoiceOption{Key="ORDER01",Label="Keep current order"});options.Add(new RewardChoiceOption{Key="ORDER10",Label="Reverse order"});return;}
            int[][] p={new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}};
            foreach(var a in p)options.Add(new RewardChoiceOption{Key="ORDER"+string.Join("",a),Label=$"{a[0]+1} → {a[1]+1} → {a[2]+1}"});
        }

        private bool TryResolve0852SearchChoice(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state==null||string.IsNullOrEmpty(state.Mode))return false;var ids=state.RevealedCardIds??new List<string>();
            if(state.Mode.StartsWith("SearchHeroRecovery",StringComparison.OrdinalIgnoreCase))
            {
                int heal=2;var m=Regex.Match(state.Mode,@"SearchHeroRecovery(\d+)");if(m.Success)int.TryParse(m.Groups[1].Value,out heal);
                var parts=(key??string.Empty).Split(':');if(parts.Length!=2||!Heroes.TryGetValue(parts[1],out var target))return true;
                if(parts[0]=="HEAL")HealHero(target,Mathf.Max(1,heal),state.SourceCardName,hero);else if(parts[0]=="CLEANSE")RemoveFirstHarmfulStatus(target,state.SourceCardName);else return true;FinishChoice0852(state,key);result=true;return true;
            }
            if(state.Mode.StartsWith("SearchHeroStatus:",StringComparison.OrdinalIgnoreCase))
            {if(!Heroes.TryGetValue(key??string.Empty,out var target))return true;string status=state.Mode.Substring("SearchHeroStatus:".Length);AddStatus(target,status,0,0,state.SourceCardName,true,hero,"until the end of their next turn");FinishChoice0852(state,key);result=true;return true;}
            switch(state.Mode)
            {
                case "SearchDrawOrHeal0852": if(key=="DRAW")DrawCards(hero,1);else if(key=="HEAL")HealHero(hero,1,state.SourceCardName,hero);else return true;break;
                case "SearchHeal2OrPoison0852": if(key=="HEAL")HealHero(hero,2,state.SourceCardName,hero);else if(key=="CLEANSE")RemoveStatus(hero.Statuses,"Poisoned",hero.DisplayName,state.SourceCardName);else return true;break;
                case "SearchHelpHunter0852": if(key=="HELP"){if(hero==null||GeneralActions(hero)<1)return true;hero.Actions--;AddPartyGold(20,state.SourceCardName);BeginEncounterDeckControl(state.SourceCardId,state.SourceCardName,"MayBottomTop",1);} else if(key!="LEAVE")return true;break;
                case "SearchTinker0852":
                    if(key=="PAY")
                    {if(PartyGold<15)return true;AddPartyGold(-15,state.SourceCardName);var held=new List<string>();for(int i=0;i<2;i++){var id=TakeTopLootForChoice(state.SourceCardName);if(!string.IsNullOrEmpty(id))held.Add(id);}state.Mode="SearchTinkerKeep0852";state.RevealedCardIds=held;state.Prompt="Choose 1 Loot to keep.";StateChanged?.Invoke();result=true;return true;}
                    if(key!="DECLINE")return true;break;
                case "SearchTinkerKeep0852": if(!key.StartsWith("KEEP")||!int.TryParse(key.Substring(4),out int ti)||ti<0||ti>=ids.Count)return true;for(int i=0;i<ids.Count;i++){if(i==ti)KeepHeldLoot(hero,ids[i],state.SourceCardName);else DiscardHeldLoot(ids[i],state.SourceCardName);}break;
                case "SearchRefugee0852": if(key=="ESCORT")AddStatus(hero,"Escorting Refugee",0,0,state.SourceCardName,true,hero);else if(key=="HEAL")HealHero(hero,1,state.SourceCardName,hero);else return true;break;
                case "SearchDeserter0852": if(key=="GOLD")AddPartyGold(25,state.SourceCardName);else if(key=="FIGHT"){if(Locations.TryGetValue(hero.LocationId,out var l))ResolveSearchTravelEncounter0852(l,hero,-1,state.SourceCardName,"SEARCH:LOOT_AFTER_MINUS1");}else return true;break;
                case "SearchRevealConnected0852": case "SearchScoutReveal0852": case "SearchRevealStrategic0852":
                    if(!key.StartsWith("REVEAL")||!int.TryParse(key.Substring(6),out int ri)||ri<0||ri>=ids.Count)return true;ResolveConnectedReveal0852(ids[ri],hero,state.SourceCardName,state.Mode=="SearchRevealConnected0852",state.Mode=="SearchRevealStrategic0852");break;
                case "SearchExploreTopDiscard0852": if(ids.Count<1)return true;if(key=="DISCARD"){ExplorationDeck0828.RemoveAll(x=>string.Equals(x,ids[0],StringComparison.OrdinalIgnoreCase));AddLog($"EXPLORATION DECK — {ExplorationLocationLabel0828(ids[0])} discarded by {state.SourceCardName}.");}else if(key!="KEEP")return true;break;
                case "SearchExploreTop3Order0852": if(!ResolveExplorationOrder0852(ids,key))return true;break;
                case "SearchExploreChoose2Resolve0852":
                    if(!key.StartsWith("CHOOSE")||!int.TryParse(key.Substring(6),out int ci)||ci<0||ci>=ids.Count)return true;string chosen=ids[ci];foreach(var id in ids)ExplorationDeck0828.RemoveAll(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));foreach(var id in ids.Where((x,i)=>i!=ci))AddLog($"EXPLORATION DECK — {ExplorationLocationLabel0828(id)} discarded by {state.SourceCardName}.");if(Locations.TryGetValue(chosen,out var chosenLocation)){if(Locations.TryGetValue(hero.LocationId,out var current)&&current.Connections.Contains(chosen)){RevealExplorationLocation0828(chosenLocation,hero,true,state.SourceCardName);}else AddLog($"SEARCH EFFECT — {chosenLocation.Name} cannot legally connect here, so the chosen Exploration card resolves with no placement (no redraw).");}break;
                case "SearchMerchantDiscount0852":
                    if(key=="DECLINE")break;
                    if(key.StartsWith("L:")&&RewardContent0826.Loot.TryGetValue(key.Substring(2),out var ld)){int cost=Mathf.Max(0,ld.Value-10);if(PartyGold<cost)return true;int at=LootDeck.FindIndex(x=>x==ld.Id);if(at<0)return true;AddPartyGold(-cost,state.SourceCardName);LootDeck.RemoveAt(at);KeepHeldLoot(hero,ld.Id,state.SourceCardName);}
                    else if(key.StartsWith("E:")&&RewardContent0826.Equipment.TryGetValue(key.Substring(2),out var ed)){int cost=Mathf.Max(0,ed.Value-10);if(PartyGold<cost)return true;int at=EquipmentDeck.FindIndex(x=>x==ed.Id);if(at<0)return true;AddPartyGold(-cost,state.SourceCardName);EquipmentDeck.RemoveAt(at);KeepHeldEquipment(hero,ed.Id,state.SourceCardName);}else return true;break;
                default:return false;
            }
            FinishChoice0852(state,key);result=true;return true;
        }
        private bool ResolveExplorationOrder0852(List<string> ids,string key)
        {
            if(ids.Count==1&&key=="ORDER0")return true;if(!key.StartsWith("ORDER",StringComparison.OrdinalIgnoreCase))return false;
            string digits=key.Substring(5);if(digits.Length!=ids.Count)return false;var order=new List<string>();foreach(char c in digits){int i=c-'0';if(i<0||i>=ids.Count||order.Contains(ids[i]))return false;order.Add(ids[i]);}
            ExplorationDeck0828.RemoveAll(x=>ids.Contains(x,StringComparer.OrdinalIgnoreCase));ExplorationDeck0828.InsertRange(0,order);AddLog("EXPLORATION DECK CONTROL — top order updated by Search effect.");return true;
        }
        private void FinishChoice0852(RewardChoiceState state,string key){AddLog($"REWARD CHOICE RESOLVED — {state.SourceCardId} {state.SourceCardName}: {key}.");PendingRewardChoice=null;TryResolveDeferredPostEncounter0852();StateChanged?.Invoke();}

        // ===== Campaign I Location execution =====
        private bool CanSearchLocation0852(HeroState hero,LocationState location,bool granted)
        {
            if(hero==null||location==null)return false;string id=location.AuthoredLocationId??string.Empty;
            bool enemies=Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==location.Id);
            if(id=="L023"&&enemies&&!CanBypassLocationRestriction0852(hero,"Locked"))return false;
            if(id=="L025"&&enemies&&!CanBypassLocationRestriction0852(hero,"Locked"))return false;
            if(id=="L024"&&RitualTokenCount0852(location)>0&&!CanBypassLocationRestriction0852(hero,"Sealed"))return false;
            return true;
        }
        private void ConsumeForgottenKeyBypassAfterSearch0852(HeroState hero,LocationState location)
        {
            if(hero==null||location==null||!hero.HasStatus("Forgotten Key Bypass"))return;
            RemoveStatus(hero.Statuses,"Forgotten Key Bypass",hero.DisplayName,"restriction bypass consumed");
            AddLog($"SEARCH UTILITY — Forgotten Key bypass is consumed by the Search at {location.Name}.");
        }
        private bool CanBypassLocationRestriction0852(HeroState hero,string kind)=>hero?.HasStatus("Forgotten Key Bypass")==true;
        private bool LocationHasSearchRestriction0852(LocationState location)
        {
            if(location==null)return false;bool enemies=Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==location.Id);string id=location.AuthoredLocationId??string.Empty;
            return (id=="L023"&&enemies)||(id=="L025"&&enemies)||(id=="L024"&&RitualTokenCount0852(location)>0);
        }

        public bool CanUseSearchUtility0852(HeroState hero)
        {
            if(hero==null||hero.IsDefeated)return false;
            if(hero.HasStatus("Rooted")&&hero.Statuses.Any(s=>s.Name=="Rooted"&&s.Source!=null&&s.Source.IndexOf("Webbed",StringComparison.OrdinalIgnoreCase)>=0))return CanUseTurnActions(hero)&&GeneralActions(hero)>=1;
            if(hero.KeptSearchEffects0852?.Contains("SD089",StringComparer.OrdinalIgnoreCase)==true)return true;
            if(hero.KeptSearchEffects0852?.Contains("SD027",StringComparer.OrdinalIgnoreCase)==true&&Locations.TryGetValue(hero.LocationId,out var l)&&LocationHasSearchRestriction0852(l))return true;
            if(CanUseEncounterUtility0853(hero))return true;
            return false;
        }
        public string SearchUtilityLabel0852(HeroState hero)
        {
            if(hero==null)return "Location Option";
            if(hero.HasStatus("Rooted")&&hero.Statuses.Any(s=>s.Name=="Rooted"&&s.Source!=null&&s.Source.IndexOf("Webbed",StringComparison.OrdinalIgnoreCase)>=0))return "Clear Webs (1)";
            if(hero.KeptSearchEffects0852?.Contains("SD027",StringComparer.OrdinalIgnoreCase)==true&&Locations.TryGetValue(hero.LocationId,out var l)&&LocationHasSearchRestriction0852(l))return "Use Forgotten Key";
            if(hero.KeptSearchEffects0852?.Contains("SD089",StringComparer.OrdinalIgnoreCase)==true)return "Use War Plans";
            string encounterLabel=EncounterUtilityLabel0853(hero);if(!string.IsNullOrEmpty(encounterLabel))return encounterLabel;
            return "Location Option";
        }
        public bool UseSearchUtility0852()
        {
            var hero=SelectedHero;if(!CanUseSearchUtility0852(hero))return false;
            if(hero.HasStatus("Rooted")&&hero.Statuses.Any(s=>s.Name=="Rooted"&&s.Source!=null&&s.Source.IndexOf("Webbed",StringComparison.OrdinalIgnoreCase)>=0))
            {if(!RequireHeroTurn(hero,"Clear Webs"))return false;hero.Actions--;RemoveStatus(hero.Statuses,"Rooted",hero.DisplayName,"Webbed Passage");AddLog($"SEARCH UTILITY — {hero.DisplayName} spends 1 Action to clear Webbed Passage.");StateChanged?.Invoke();return true;}
            if(hero.KeptSearchEffects0852.Contains("SD027",StringComparer.OrdinalIgnoreCase))
            {hero.KeptSearchEffects0852.RemoveAll(x=>string.Equals(x,"SD027",StringComparison.OrdinalIgnoreCase));AddStatus(hero,"Forgotten Key Bypass",0,0,"SD027 Forgotten Key",true,hero,"until the end of their turn");AddLog("SEARCH UTILITY — Forgotten Key discarded; the next applicable Locked/Sealed/Gate/Trap restriction this turn is bypassed.");StateChanged?.Invoke();return true;}
            if(hero.KeptSearchEffects0852.Contains("SD089",StringComparer.OrdinalIgnoreCase))
            {hero.KeptSearchEffects0852.RemoveAll(x=>string.Equals(x,"SD089",StringComparison.OrdinalIgnoreCase));QueueNextTravelEncounterSeverity(-2,"SD089 Goblin War Plans");AddLog("SEARCH UTILITY — Goblin War Plans discarded; next Travel Encounter gets -2 Severity (minimum 1).");StateChanged?.Invoke();return true;}
            if(UseEncounterUtility0853(hero))return true;
            return false;
        }

        private bool IsExploreBlocked0852(string locationId)=>!string.IsNullOrEmpty(locationId)&&((exploreBlockedAtLocationRound0852.TryGetValue(locationId,out int r)&&r==Round)||LocationHasTag(locationId,"Explore Blocked"));
        private int SearchMoveSurcharge0852(HeroState hero)=>hero!=null&&(rubbleLocations0852.Contains(hero.LocationId)||LocationHasTag(hero.LocationId,"Search Rubble"))?1:(hero!=null&&hero.HasStatus("Blocked Route")?1:0);
        private void ConsumeSearchMoveSurcharge0852(HeroState hero)
        {
            if(hero==null)return;
            bool rubble=rubbleLocations0852.Remove(hero.LocationId)||LocationHasTag(hero.LocationId,"Search Rubble");
            if(rubble&&Locations.TryGetValue(hero.LocationId,out var l)){l.TemporaryTags.RemoveAll(t=>string.Equals(t.Tag,"Search Rubble",StringComparison.OrdinalIgnoreCase));AddLog("SEARCH HAZARD — Rubble movement surcharge is consumed and the marker is removed.");}
            RemoveStatus(hero.Statuses,"Blocked Route",hero.DisplayName,"movement surcharge consumed");
        }

        private bool TryBlockConditionBySearchWard0852(HeroState target,string name)
        {
            if(!searchConditionWard0852||searchConditionWardRound0852!=Round||target==null)return false;
            if(name!="Burning"&&name!="Poisoned"&&name!="Weakened")return false;
            searchConditionWard0852=false;searchConditionWardRound0852=-1;AddLog($"SEARCH WARD — Shamanic Markings prevents {target.DisplayName} from gaining {name}.");return true;
        }

        private int RitualTokenCount0852(LocationState location)=>location==null?0:EncounterObjects.Count(o=>o.Type=="Ritual Token"&&o.LocationId==location.Id);
        private void ResolveSearchEndRound0852A()
        {
            // Search-card round state only. Location-card-owned end-round rules are intentionally deferred to 0.8.52b.
            AdvanceLocationTagClock(null,StatusExpiryTiming.EndOfRound);
            exploreBlockedAtLocationRound0852.Clear();
            if(searchTravelSeverityRound0852==Round){searchTravelSeverityModifier0852=0;searchTravelSeverityRound0852=-1;}
            if(searchGoblinSeverityRound0852==Round){searchGoblinSeverityModifier0852=0;searchGoblinSeverityRound0852=-1;}
            if(searchConditionWardRound0852==Round){searchConditionWard0852=false;searchConditionWardRound0852=-1;}
        }

        private void ResolveSearchArrivalEffects0852A(LocationState location,HeroState hero)
        {
            // SD078 Frightened Refugee is a Search-card effect that resolves when the escort reaches Stonebrook.
            if(location==null||hero==null)return;
            if(hero.HasStatus("Escorting Refugee")&&string.Equals(location.AuthoredLocationId,"L000",StringComparison.OrdinalIgnoreCase))
            {
                RemoveStatus(hero.Statuses,"Escorting Refugee",hero.DisplayName,"Refugee escorted to Stonebrook");
                AddPartyGold(10,"Frightened Refugee escorted to Stonebrook");
                AddLog($"SEARCH EFFECT — {hero.DisplayName} escorts the Frightened Refugee to Stonebrook and gains 10 Gold.");
            }
        }
        public int SearchTravelSeverityModifierForSave0852=>searchTravelSeverityModifier0852;
        public int SearchTravelSeverityRoundForSave0852=>searchTravelSeverityRound0852;
        public int SearchGoblinSeverityModifierForSave0852=>searchGoblinSeverityModifier0852;
        public int SearchGoblinSeverityRoundForSave0852=>searchGoblinSeverityRound0852;
        public bool SearchConditionWardForSave0852=>searchConditionWard0852;
        public int SearchConditionWardRoundForSave0852=>searchConditionWardRound0852;
        public IReadOnlyList<string> DeferredPostEncounterForSave0852=>deferredPostEncounter0852;
        private void RestoreCampaignISearchState0852(AscendanceSaveData save)
        {
            if(save==null)return;
            searchTravelSeverityModifier0852=save.SearchTravelSeverityModifier0852;searchTravelSeverityRound0852=save.SearchTravelSeverityRound0852;
            searchGoblinSeverityModifier0852=save.SearchGoblinSeverityModifier0852;searchGoblinSeverityRound0852=save.SearchGoblinSeverityRound0852;
            searchConditionWard0852=save.SearchConditionWard0852;searchConditionWardRound0852=save.SearchConditionWardRound0852;
            deferredPostEncounter0852.Clear();if(save.DeferredPostEncounter0852!=null)deferredPostEncounter0852.AddRange(save.DeferredPostEncounter0852);
        }

#if UNITY_EDITOR
        public bool DebugResolveSearchCard0852(string cardId,string locationId,string heroId)
        {
            if(!SearchEncounterContent0820.SearchCards.TryGetValue(cardId??string.Empty,out var card)||!Locations.TryGetValue(locationId??string.Empty,out var location)||!Heroes.TryGetValue(heroId??string.Empty,out var hero))return false;
            ResolveSearchCard(card,location,hero);StateChanged?.Invoke();return true;
        }
        public bool DebugSearchWardActive0852=>searchConditionWard0852&&searchConditionWardRound0852==Round;
        public bool DebugExploreBlocked0852(string locationId)=>IsExploreBlocked0852(locationId);
        public int DebugApplyEnemyAttackDamage0852(string heroId,string enemyId,int amount=1)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero)||!Enemies.TryGetValue(enemyId??string.Empty,out var enemy))return 0;
            return ApplyHeroDamageNow(hero,Mathf.Max(0,amount),$"{enemy.DisplayName} Attack (0.8.52a validator)",false,enemy);
        }
#endif
    }
}
