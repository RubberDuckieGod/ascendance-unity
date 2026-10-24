using System;
using System.Linq;

namespace Ascendance
{
    // 0.8.51 — Explicit card/item/location/Quest/Encounter effects may grant Search or Explore
    // without being blocked by the normal player-action gate. Normal Search/Explore remain unchanged.
    public sealed partial class AscendanceGameManager
    {
        private LocationState ResolveGrantedSearchTarget0851(HeroState hero,string locationId)
        {
            if(hero==null)return null;
            if(!string.IsNullOrWhiteSpace(locationId))
            {
                if(Locations.TryGetValue(locationId,out var direct))return direct;
                return Locations.Values.FirstOrDefault(l=>string.Equals(l.AuthoredLocationId,locationId,StringComparison.OrdinalIgnoreCase));
            }
            return SearchTargetFor(hero);
        }

        public bool CanResolveGrantedSearch0851(HeroState hero,string locationId=null)
        {
            if(hero==null||hero.IsDefeated||hero.Moving)return false;
            var location=ResolveGrantedSearchTarget0851(hero,locationId);
            // Search remains once per Location unless the granting effect explicitly implements a different rule.
            return location!=null&&location.Revealed&&!location.Searched&&CanSearchLocation0852(hero,location,true)&&CanSearchLocation0858(hero,location,true);
        }

        public bool ResolveGrantedSearch0851(HeroState hero,string source,string locationId=null)
        {
            var location=ResolveGrantedSearchTarget0851(hero,locationId);
            if(!CanResolveGrantedSearch0851(hero,locationId))
            {
                AddLog($"EFFECT SEARCH — {source}: no legal unsearched revealed Location is available for {hero?.DisplayName??"the Hero"}.");
                StateChanged?.Invoke();
                return false;
            }
            if(string.Equals(hero.FreeSearchLocationId,location.Id,StringComparison.OrdinalIgnoreCase))hero.FreeSearchLocationId=null;
            location.Searched=true;location.SearchRequests++;
            AddLog($"EFFECT SEARCH — {hero.DisplayName} searches {location.Name} because of {source} (no additional Action; normal engagement/action gate bypassed).");
            if(!TryResolveQuestSearchOverride0829(location,hero)&&!ResolveLocationSearch0858(location,hero))ResolveSearchDraw(location,hero);
            QueueLocationPostSearch0858(location,hero);
            ConsumeForgottenKeyBypassAfterSearch0852(hero,location);
            SearchRequested?.Invoke(location,hero);StateChanged?.Invoke();
            return true;
        }

        private LocationState ResolveGrantedExploreOrigin0851(HeroState hero,string originLocationId)
        {
            if(hero==null)return null;
            string id=string.IsNullOrWhiteSpace(originLocationId)?hero.LocationId:originLocationId;
            if(Locations.TryGetValue(id??string.Empty,out var direct))return direct;
            return Locations.Values.FirstOrDefault(l=>string.Equals(l.AuthoredLocationId,id,StringComparison.OrdinalIgnoreCase));
        }

        public bool CanResolveGrantedExplore0851(HeroState hero,string originLocationId=null)
        {
            if(hero==null||hero.IsDefeated||hero.Moving)return false;
            var origin=ResolveGrantedExploreOrigin0851(hero,originLocationId);
            return origin!=null&&origin.Revealed&&!IsExploreBlocked0852(origin.Id)&&HasLegalExplorationLocation0828(origin);
        }

        public bool ResolveGrantedExplore0851(HeroState hero,string source,string originLocationId=null,bool resolveRevealEffect=true)
        {
            var origin=ResolveGrantedExploreOrigin0851(hero,originLocationId);
            if(!CanResolveGrantedExplore0851(hero,originLocationId))
            {
                AddLog($"EFFECT EXPLORE — {source}: no legal connected unrevealed Location is available for {hero?.DisplayName??"the Hero"}.");
                StateChanged?.Invoke();
                return false;
            }
            var hidden=DrawLegalExplorationLocation0828(origin,source??"granted Explore");
            if(hidden==null)
            {
                AddLog($"EFFECT EXPLORE — {source}: no legal Exploration card could be drawn.");
                StateChanged?.Invoke();
                return false;
            }
            RevealExplorationLocation0828(hidden,hero,resolveRevealEffect,source??"granted Explore");
            AddLog($"EFFECT EXPLORE — {source} lets {hero.DisplayName} reveal {hidden.Name} (no additional Action; normal engagement/action gate bypassed). {hero.DisplayName} stays in {origin.Name}.");
            TryBeginRowanTrailblazer0828(hero,source??"granted Explore");
            StateChanged?.Invoke();
            return true;
        }
    }
}
