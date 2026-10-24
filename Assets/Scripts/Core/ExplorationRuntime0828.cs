using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.28 — real Exploration deck for hidden graph Locations plus SD005/SD006/SD010 and Rowan Improved Trailblazer.
    public sealed partial class AscendanceGameManager
    {
        public readonly List<string> ExplorationDeck0828 = new();
        public IReadOnlyList<string> ExplorationDeck => ExplorationDeck0828;
        public string ExplorationDeckSummary0828 => ExplorationDeck0828.Count==0?"Empty":string.Join(" → ",ExplorationDeck0828.Select(ExplorationLocationLabel0828));

        private void InitializeExplorationDeck0828()
        {
            ExplorationDeck0828.Clear();
            ExplorationDeck0828.AddRange(Locations.Values.Where(l=>!l.Revealed&&IsLocationEligibleForCurrentQuest0829(l.Id)).OrderBy(l=>l.Id).Select(l=>l.Id));
            Shuffle(ExplorationDeck0828);
            AddLog($"EXPLORATION DECK — {ExplorationDeck0828.Count} unrevealed Location card(s) ready.");
        }

        private void RestoreExplorationDeckState0828(AscendanceSaveData save)
        {
            ExplorationDeck0828.Clear();
            if(save?.ExplorationDeckIds!=null&&save.ExplorationDeckIds.Count>0)
                ExplorationDeck0828.AddRange(save.ExplorationDeckIds.Where(id=>Locations.ContainsKey(id)&&!Locations[id].Revealed&&IsLocationEligibleForCurrentQuest0829(id)));
            foreach(var id in Locations.Values.Where(l=>!l.Revealed&&IsLocationEligibleForCurrentQuest0829(l.Id)).Select(l=>l.Id))
                if(!ExplorationDeck0828.Contains(id,StringComparer.OrdinalIgnoreCase))ExplorationDeck0828.Add(id);
        }

        private string ExplorationLocationLabel0828(string id)
        {
            if(!Locations.TryGetValue(id??string.Empty,out var l))return id??"Unknown";
            return $"{l.Name} [{l.Terrain}]";
        }

        private bool HasLegalExplorationLocation0828(LocationState current)
            => current!=null&&current.Connections.Any(id=>Locations.TryGetValue(id,out var l)&&!l.Revealed&&IsLocationEligibleForCurrentQuest0829(id)&&CanRevealQuestLocation0832(id));

        private LocationState DrawLegalExplorationLocation0828(LocationState current,string source)
        {
            if(current==null)return null;
            var setAside=new List<string>(); LocationState chosen=null;
            while(ExplorationDeck0828.Count>0)
            {
                string id=ExplorationDeck0828[0];ExplorationDeck0828.RemoveAt(0);
                if(Locations.TryGetValue(id,out var candidate)&&!candidate.Revealed&&IsLocationEligibleForCurrentQuest0829(id)&&CanRevealQuestLocation0832(id)&&current.Connections.Contains(id)){chosen=candidate;break;}
                setAside.Add(id);
            }
            if(setAside.Count>0)
            {
                ExplorationDeck0828.AddRange(setAside.Where(id=>Locations.TryGetValue(id,out var l)&&!l.Revealed&&IsLocationEligibleForCurrentQuest0829(id)));
                if(ExplorationDeck0828.Count>1)Shuffle(ExplorationDeck0828);
                AddLog($"EXPLORATION DECK — {setAside.Count} illegal draw(s) set aside while finding a connected Location for {source}; returned and reshuffled afterward.");
            }
            return chosen;
        }

        private void RemoveExplorationLocationFromDeck0828(string locationId)
        {
            int at=ExplorationDeck0828.FindIndex(id=>string.Equals(id,locationId,StringComparison.OrdinalIgnoreCase));
            if(at>=0)ExplorationDeck0828.RemoveAt(at);
        }

        private void RevealExplorationLocation0828(LocationState location,HeroState hero,bool resolveRevealEffect,string source)
        {
            if(location==null)return;
            RemoveExplorationLocationFromDeck0828(location.Id);
            location.Revealed=true;
            OnQuestLocationRevealed0829(location,hero);
            if(resolveRevealEffect)
            {
                location.RevealEffectDeferred=false;
                ResolveLocationReveal(location,hero);
            }
            else
            {
                location.RevealEffectDeferred=!location.RevealResolved;
                AddLog($"REVEAL DEFERRED — {location.Name} is visible from {source}, but its Reveal Effect waits until a Hero enters or another normal reveal resolves it.");
            }
        }

        private void ResolveDeferredRevealEffectOnEntry0828(LocationState location,HeroState hero)
        {
            if(location==null||!location.RevealEffectDeferred||location.RevealResolved)return;
            location.RevealEffectDeferred=false;
            AddLog($"REVEAL DEFERRED RESOLVES — {hero?.DisplayName??"A Hero"} enters {location.Name}.");
            ResolveLocationReveal(location,hero);
            TryBeginRowanTrailblazer0828(hero,"deferred Reveal");
        }

        private bool BeginExplorationTopTwoChoice0828(string sourceCardId,string sourceCardName,HeroState hero,string mode,string prompt)
        {
            var ids=ExplorationDeck0828.Take(2).ToList();if(ids.Count<2)return false;
            BeginRewardChoice0827(sourceCardId,sourceCardName,hero,mode,prompt+" "+string.Join(" | ",ids.Select(ExplorationLocationLabel0828)),ids);
            return true;
        }

        private void TryBeginRowanTrailblazer0828(HeroState hero,string source)
        {
            if(hero==null||!string.Equals(hero.Id,"Rowan",StringComparison.OrdinalIgnoreCase)||hero.UsedTurnAbilities.Contains("Trailblazer")||ExplorationDeck0828.Count<2)return;
            hero.UsedTurnAbilities.Add("Trailblazer");
            BeginExplorationTopTwoChoice0828("ROWAN","Improved Trailblazer",hero,"ExplorationTrailblazer","Choose which card stays on top; the other goes to the bottom.");
            AddLog($"HERO ABILITY — Improved Trailblazer triggers after {source}.");
        }

        private bool ResolveSearchExplorationReward0828(SearchCardDefinition card,LocationState location,HeroState hero)
        {
            if(card==null)return false;
            switch(card.Id)
            {
                case "SD005":
                {
                    var revealed=DrawLegalExplorationLocation0828(location,card.Name);
                    if(revealed==null){AddLog($"{card.Name} — no connected unrevealed Location is available.");return true;}
                    RevealExplorationLocation0828(revealed,hero,false,card.Name);
                    AddLog($"SEARCH DISCOVERY — {card.Name} reveals {revealed.Name}; Reveal Effect deferred.");
                    TryBeginRowanTrailblazer0828(hero,card.Name);
                    return true;
                }
                case "SD006":
                {
                    var connected=location?.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&!l.Revealed&&IsLocationEligibleForCurrentQuest0829(id)).ToList()??new List<string>();
                    if(connected.Count==0){AddLog($"{card.Name} — no connected unrevealed Location is available to inspect.");return true;}
                    if(connected.Count>1)
                    {
                        BeginRewardChoice0827(card.Id,card.Name,hero,"ExplorationChooseInspect","Choose one connected unrevealed Location to inspect.",connected);
                        return true;
                    }
                    string id=connected[0];var target=Locations[id];
                    BeginRewardChoice0827(card.Id,card.Name,hero,"ExplorationInspectConnected",$"Inspect {ExplorationLocationLabel0828(id)}. {target.RevealEffect} Reveal it now, or leave it unrevealed.",new[]{id});
                    AddLog($"SEARCH SCOUT — {card.Name} inspects {target.Name} without revealing it.");
                    return true;
                }
                case "SD010":
                {
                    if(!BeginExplorationTopTwoChoice0828(card.Id,card.Name,hero,"ExplorationTopTwoOrder","Put the top 2 Exploration cards back in either order."))
                        AddLog($"{card.Name} — fewer than 2 Exploration cards remain; no reorder is possible.");
                    return true;
                }
            }
            return false;
        }

        private void Append0828ExplorationChoiceOptions(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(state==null)return;var ids=state.RevealedCardIds??new List<string>();
            switch(state.Mode)
            {
                case "ExplorationChooseInspect":
                    for(int i=0;i<ids.Count;i++)if(Locations.TryGetValue(ids[i],out var inspect)&&!inspect.Revealed)options.Add(new RewardChoiceOption{Key=$"INSPECT{i}",Label=$"Inspect {ExplorationLocationLabel0828(ids[i])}"});
                    break;
                case "ExplorationInspectConnected":
                    if(ids.Count>0&&Locations.TryGetValue(ids[0],out var l)&&!l.Revealed)
                    {
                        options.Add(new RewardChoiceOption{Key="REVEAL",Label=$"Reveal {l.Name} now"});
                        options.Add(new RewardChoiceOption{Key="LEAVE",Label=$"Leave {l.Name} unrevealed"});
                    }
                    break;
                case "ExplorationTopTwoOrder":
                    if(ids.Count>=2)
                    {
                        options.Add(new RewardChoiceOption{Key="ORDER01",Label=$"Top: {ExplorationLocationLabel0828(ids[0])} • then {ExplorationLocationLabel0828(ids[1])}"});
                        options.Add(new RewardChoiceOption{Key="ORDER10",Label=$"Top: {ExplorationLocationLabel0828(ids[1])} • then {ExplorationLocationLabel0828(ids[0])}"});
                    }
                    break;
                case "ExplorationTrailblazer":
                    if(ids.Count>=2)
                    {
                        options.Add(new RewardChoiceOption{Key="TOP0",Label=$"Keep {ExplorationLocationLabel0828(ids[0])} on top • bottom {ExplorationLocationLabel0828(ids[1])}"});
                        options.Add(new RewardChoiceOption{Key="TOP1",Label=$"Keep {ExplorationLocationLabel0828(ids[1])} on top • bottom {ExplorationLocationLabel0828(ids[0])}"});
                    }
                    break;
            }
        }

        private bool TryResolve0828ExplorationChoice(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state==null)return false;var ids=state.RevealedCardIds??new List<string>();
            switch(state.Mode)
            {
                case "ExplorationChooseInspect":
                    if(!key.StartsWith("INSPECT",StringComparison.OrdinalIgnoreCase)||!int.TryParse(key.Substring(7),out int inspectIndex)||inspectIndex<0||inspectIndex>=ids.Count)return true;
                    if(!Locations.TryGetValue(ids[inspectIndex],out var inspect)||inspect.Revealed)return true;
                    state.Mode="ExplorationInspectConnected";
                    state.RevealedCardIds=new List<string>{inspect.Id};
                    state.Prompt=$"Inspect {ExplorationLocationLabel0828(inspect.Id)}. {inspect.RevealEffect} Reveal it now, or leave it unrevealed.";
                    AddLog($"SEARCH SCOUT — {state.SourceCardName} selects {inspect.Name} to inspect without revealing it.");
                    StateChanged?.Invoke();
                    result=true;
                    return true;
                case "ExplorationInspectConnected":
                    if(ids.Count==0||!Locations.TryGetValue(ids[0],out var l))return true;
                    if(key=="REVEAL")
                    {
                        RevealExplorationLocation0828(l,hero,true,state.SourceCardName);AddLog($"SEARCH SCOUT — {l.Name} is revealed normally.");TryBeginRowanTrailblazer0828(hero,state.SourceCardName);
                    }
                    else if(key=="LEAVE")AddLog($"SEARCH SCOUT — {l.Name} remains unrevealed; its Exploration card stays in the deck.");
                    else return true;
                    break;
                case "ExplorationTopTwoOrder":
                    if(ids.Count<2||(key!="ORDER01"&&key!="ORDER10"))return true;
                    var order=key=="ORDER01"?new[]{ids[0],ids[1]}:new[]{ids[1],ids[0]};
                    ExplorationDeck0828.RemoveAll(x=>ids.Contains(x,StringComparer.OrdinalIgnoreCase));ExplorationDeck0828.InsertRange(0,order);
                    AddLog($"EXPLORATION DECK CONTROL — top two are now {ExplorationLocationLabel0828(order[0])} → {ExplorationLocationLabel0828(order[1])}.");
                    break;
                case "ExplorationTrailblazer":
                    if(ids.Count<2||(key!="TOP0"&&key!="TOP1"))return true;
                    string top=key=="TOP0"?ids[0]:ids[1], bottom=key=="TOP0"?ids[1]:ids[0];
                    ExplorationDeck0828.RemoveAll(x=>ids.Contains(x,StringComparer.OrdinalIgnoreCase));ExplorationDeck0828.Insert(0,top);ExplorationDeck0828.Add(bottom);
                    AddLog($"HERO ABILITY — Improved Trailblazer keeps {ExplorationLocationLabel0828(top)} on top and places {ExplorationLocationLabel0828(bottom)} on the bottom.");
                    break;
                default:return false;
            }
            AddLog($"REWARD CHOICE RESOLVED — {state.SourceCardId} {state.SourceCardName}: {key}.");PendingRewardChoice=null;StateChanged?.Invoke();result=true;return true;
        }

        public void DebugSetExplorationDeck0828(params string[] ids)
        {
            ExplorationDeck0828.Clear();ExplorationDeck0828.AddRange((ids??Array.Empty<string>()).Where(Locations.ContainsKey));StateChanged?.Invoke();
        }
        public string DebugExplorationDeck0828()=>string.Join(",",ExplorationDeck0828);
        public bool DebugSetLocationHidden0828(string locationId)
        {
            if(!Locations.TryGetValue(locationId??string.Empty,out var l))return false;l.Revealed=false;l.RevealResolved=false;l.RevealEffectDeferred=false;l.FirstVisitResolved=false;if(!ExplorationDeck0828.Contains(l.Id,StringComparer.OrdinalIgnoreCase))ExplorationDeck0828.Add(l.Id);return true;
        }
        public bool DebugEnterLocation0828(string heroId,string locationId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h)||!Locations.TryGetValue(locationId??string.Empty,out var l))return false;h.LocationId=l.Id;ResolveDeferredRevealEffectOnEntry0828(l,h);return true;
        }
        public bool DebugBeginRowanTrailblazer0828(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h))return false;h.UsedTurnAbilities.Remove("Trailblazer");TryBeginRowanTrailblazer0828(h,"0.8.28 validation");return HasPendingRewardChoice&&PendingRewardChoice.Mode=="ExplorationTrailblazer";
        }
    }
}
