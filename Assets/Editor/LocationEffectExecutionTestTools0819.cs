using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class LocationEffectExecutionTestTools0819
    {
        [MenuItem("Ascendance/Validation/Test 0.8.19 - Stonebrook Location Effects (Play Mode)")]
        public static void TestLocationEffects()
        {
            if(!EditorApplication.isPlaying){Debug.LogError("[ASCENDANCE][0.8.19 TEST] FAIL — enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.19 TEST] FAIL — GameManager missing.");return;}
            int passed=0,failed=0;
            void Check(bool ok,string text){if(ok){passed++;Debug.Log("[ASCENDANCE][0.8.19 TEST] PASS — "+text);}else{failed++;Debug.LogError("[ASCENDANCE][0.8.19 TEST] FAIL — "+text);}}
            var hero=gm.ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated);if(hero==null){Debug.LogError("[ASCENDANCE][0.8.19 TEST] FAIL — no usable Hero.");return;}
            var ruins=gm.Locations.Values.First(l=>l.Name=="Ancient Ruins");var dense=gm.Locations.Values.First(l=>l.Name=="Dense Woods");var river=gm.Locations.Values.First(l=>l.Name=="River Crossing");

            // Direct validator calls must reproduce the real Explore reveal contract: the Location is
            // already Revealed before ResolveLocationReveal executes. Keeping this explicit prevents
            // the test harness from bypassing the runtime permission guards it is meant to validate.
            dense.Revealed=true;river.Revealed=true;ruins.Revealed=true;

            // Reveal choice: Dense Woods -> explicit Hide permission, not forced until chosen.
            dense.RevealResolved=false;gm.DebugResolveLocationReveal(dense.Id,hero.Id);
            Check(hero.PendingLocationOption=="Hide"&&!hero.HasStatus("Hidden"),"Dense Woods grants an optional Hide permission without forcing Hidden.");
            gm.DebugUseLocationRevealOption(hero.Id);
            Check(hero.HasStatus("Hidden")&&string.IsNullOrEmpty(hero.PendingLocationOption),"Choosing Concealing Brush applies Hidden and consumes the reveal option.");
            hero.Statuses.RemoveAll(s=>string.Equals(s.Name,"Hidden",StringComparison.OrdinalIgnoreCase));

            // River Crossing choice: heal path and clean poison path are executable.
            river.RevealResolved=false;gm.DebugResolveLocationReveal(river.Id,hero.Id);hero.Health=Mathf.Max(1,hero.MaxHealth-2);int before=hero.Health;gm.DebugUseLocationRevealOption(hero.Id,"Heal");
            Check(hero.Health==Mathf.Min(hero.MaxHealth,before+1),"Fresh Water can recover exactly 1 Health by player choice.");
            river.RevealResolved=false;gm.DebugResolveLocationReveal(river.Id,hero.Id);gm.AddStatus(hero,"Poisoned",0,0,"0.8.19 test",false,hero,"until removed");gm.DebugUseLocationRevealOption(hero.Id,"Cleanse");
            Check(!hero.HasStatus("Poisoned"),"Fresh Water can remove Poisoned by player choice.");

            // Ancient Ruins free Search: permission exists, Search is once/location, costs 0, and emits a hook.
            ruins.RevealResolved=false;ruins.Searched=false;gm.DebugResolveLocationReveal(ruins.Id,hero.Id);Check(hero.FreeSearchLocationId==ruins.Id,"Ancient Ruins grants a one-time free Search permission on Reveal.");
            hero.LocationId=ruins.Id;hero.TurnCompleted=false;hero.Actions=0;hero.AttackOnlyActions=0;
            // Search API requires a live turn. Make this hero current through normal start if possible.
            if(gm.TurnHero!=null&&gm.TurnHero.Id==hero.Id)hero.Actions=Mathf.Max(hero.Actions,1);
            else if(gm.TurnHero==null){gm.SelectHero(hero.Id);gm.TryStartHeroTurn(hero);}
            int actions=hero.Actions;bool searchHook=false;Action<LocationState,HeroState> onSearch=(l,h)=>{if(l.Id==ruins.Id&&h.Id==hero.Id)searchHook=true;};gm.SearchRequested+=onSearch;gm.SearchCurrentLocation();gm.SearchRequested-=onSearch;
            Check(ruins.Searched&&hero.Actions==actions&&searchHook,"Forgotten Remains performs a free one-time Search and fires the Search deck hook.");
            Check(!gm.CanSearchCurrentLocation(hero),"A Location cannot be searched a second time after its Search is consumed.");

            // Normal Search on the occupied Location costs exactly 1 Action.
            river.Searched=false;hero.FreeSearchLocationId=null;hero.LocationId=river.Id;int normalBefore=hero.Actions;gm.SearchCurrentLocation();
            Check(river.Searched&&hero.Actions==normalBefore-1,"A normal Location Search costs exactly 1 Action and consumes that Location's Search.");

            // First Visit requests the Travel Encounter only when it actually resolves.
            ruins.FirstVisitResolved=false;int oldReq=ruins.TravelEncounterRequests;foreach(var e in gm.Enemies.Values.Where(e=>e.LocationId==ruins.Id))e.Health=0;bool travelHook=false;int travelLevel=0;Action<LocationState,HeroState,int> onTravel=(l,h,level)=>{if(l.Id==ruins.Id){travelHook=true;travelLevel=level;}};gm.TravelEncounterRequested+=onTravel;gm.DebugResolveLocationFirstVisit(ruins.Id,hero.Id);gm.TravelEncounterRequested-=onTravel;
            Check(ruins.FirstVisitResolved&&ruins.TravelEncounterRequests==oldReq+1&&travelHook&&travelLevel==1,"Ancient Ruins First Visit requests exactly one Level-1 Travel Encounter through the runtime hook.");

            // Exact current combat modifier: Dense Woods imposes -1 on ranged attacks into/out; melee is unchanged.
            Check(gm.DebugLocationAttackModifier(dense.Id,river.Id,true)==-1&&gm.DebugLocationAttackModifier(dense.Id,river.Id,false)==0,"Dense Woods Limited Visibility applies -1 only to ranged Attacks into/out.");
            Check(gm.LocationProvidesCoverAgainstRanged(ruins.Id)&&!gm.LocationProvidesCoverAgainstRanged(river.Id),"Ancient Ruins exposes Cover vs ranged while River Crossing explicitly provides no Cover.");

            Debug.Log($"[ASCENDANCE][0.8.19 TEST] {(failed==0?"PASS":"FAIL")} — Stonebrook Location Effect Execution completed: {passed} passed, {failed} failed.");
        }
    }
}
