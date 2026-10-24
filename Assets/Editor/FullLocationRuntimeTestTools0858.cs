#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class FullLocationRuntimeTestTools0858
    {
        [MenuItem("Ascendance/Validation/Test 0.8.58c - Full Location Runtime Execution (Play Mode)")]
        public static void Test0858()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.58c TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.58c TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.58c TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.58c TEST] FAIL — "+label);}}
            void SafeSearchDeck(){gm.SearchDeck.Clear();gm.SearchDiscard.Clear();gm.SearchDeck.AddRange(new[]{"SD001","SD002","SD003","SD004","SD005","SD006"});}

            FullContentCatalog0850.EnsureLoaded();
            Check(AscendanceGameManager.BuildVersion=="0.10.2c","Build identifies the Full Location Runtime Execution milestone.");
            Check(FullContentCatalog0850.LocationCount==236,"full authored catalog contains 236 Location definitions.");
            Check(gm.DebugLocationCount0858()==236,"all 236 authored Locations have an executable Location runtime path.");
            Check(Enumerable.Range(0,236).Select(i=>$"L{i:000}").All(id=>FullContentCatalog0850.TryGetLocation(id,out _)),"Location IDs are contiguous L000-L235.");
            Check(FullContentCatalog0850.Locations.All(l=>!string.IsNullOrWhiteSpace(l.revealEffect)&&!string.IsNullOrWhiteSpace(l.firstVisitEffect)&&!string.IsNullOrWhiteSpace(l.ongoingEffect)&&!string.IsNullOrWhiteSpace(l.search)),"every authored Location preserves Reveal, First Visit, Ongoing, and Search text.");
            Check(FullContentCatalog0850.Locations.Select(l=>l.region).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()>=6,"Location catalog spans all six campaign regions/hub areas.");
            Check(FullContentCatalog0850.QuestCount==72&&FullContentCatalog0850.WorldEventCount==24&&FullContentCatalog0850.CraftingRecipeCount==200,"0.8.55-0.8.57 authored content catalogs remain intact.");
            Check(typeof(LocationSaveData).GetField("UsedKeys0858")!=null&&typeof(AscendanceSaveData).GetField("RegionalEncounterDeckIds0858")!=null&&typeof(AscendanceSaveData).GetField("RegionalEncounterDiscardIds0858")!=null,"save-facing state includes Location once-keys and regional Encounter deck state.");

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",true);yield return null;
            Check(new[]{"Q013","Q017","Q025","Q037","Q038","Q049","Q052","Q061","Q064","Q066","Q070","Q072"}.All(gm.DebugEnsureQuestLocations0858),"representative Quest pools instantiate authored Locations across Campaigns II-VI.");
            var g=gm.Heroes["Garruk"];var a=gm.Heroes["Aldric"];
            Check(gm.DebugLocationByAuthored0858("L003")?.Name=="Woodland Clearing"&&gm.DebugLocationByAuthored0858("L006")?.Name=="Goblin Lookout"&&gm.DebugLocationByAuthored0858("L007")?.Name=="Abandoned Campsite"&&gm.DebugLocationByAuthored0858("L008")?.Name=="Spider Nest","authored Location lookup wins over legacy runtime-ID collisions in Stonebrook.");

            // Stonebrook-region runtime behaviors.
            gm.DebugLocationEnter0858("Garruk","L003");gm.DebugLocationEnter0858("Aldric","L003");g.Health=Mathf.Max(1,g.MaxHealth-2);a.Health=Mathf.Max(1,a.MaxHealth-2);int gh=g.Health,ah=a.Health;gm.DebugResolveLocationReveal0858("L003","Garruk");
            Check(g.Health==gh+1&&a.Health==ah+1,"L003 Woodland Clearing Reveal recovers 1 Health for each Hero there.");
            int gold=gm.PartyGold;gm.DebugResolveLocationReveal0858("L006","Garruk");Check(gm.PartyGold==gold+10,"L006 Goblin Lookout Reveal grants 10 Gold.");
            int loot=g.LootInventory.Count;gm.DebugResolveLocationReveal0858("L007","Garruk");Check(g.LootInventory.Count>=loot+1,"L007 Abandoned Campsite Reveal draws at least one authored Loot, including valid bonus-Loot chains.");
            g.Statuses.RemoveAll(s=>s.Name=="Location Move Surcharge");gm.DebugResolveLocationReveal0858("L008","Garruk");Check(gm.DebugLocationMoveSurcharge0858("Garruk","L003")>=1,"L008 Spider Nest Reveal makes the next Move cost +1 Action.");g.Statuses.RemoveAll(s=>s.Name=="Location Move Surcharge");
            g.Health=g.MaxHealth;gm.DebugResolveLocationReveal0858("L009","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationDamageOrStop0858","L009 Rocky Pass exposes the authored damage-or-stop choice.");int hp=g.Health;gm.DebugChooseRewardChoice("DAMAGE");Check(g.Health==hp-1&&!gm.HasPendingRewardChoice,"L009 damage choice resolves and closes cleanly.");
            g.Health=Mathf.Max(1,g.MaxHealth-4);gm.DebugResolveLocationReveal0858("L011","Garruk");Check(gm.DebugPendingRewardChoiceMode().StartsWith("LocationHealOrCleanse0858"),"L011 Forgotten Shrine exposes Heal-or-Cleanse.");hp=g.Health;gm.DebugChooseRewardChoice("HEAL");Check(g.Health>=hp+2,"L011 authored Reveal healing resolves.");
            var l11=gm.DebugLocationByAuthored0858("L011");l11.UsedKeys0858.Clear();g.Health=Mathf.Max(1,g.MaxHealth-3);hp=g.Health;gm.DebugLocationEnter0858("Garruk","L011");Check(g.Health==Mathf.Min(g.MaxHealth,hp+2),"L011 Blessed Ground heals each Hero on their first entry.");
            gm.DebugLocationEnter0858("Garruk","L017");Check(gm.DebugLocationRangeBonus0858("Garruk",1)>=1,"L017 Fallen Watchtower grants +1 ranged Attack Range.");
            var l36=gm.DebugLocationByAuthored0858("L036");Check(l36!=null&&gm.LocationProvidesCoverAgainstRanged(l36.Id),"L036 Hunter's Lodge grants Cover against ranged attacks.");

            // Valewatch / Crownlands.
            gm.DebugLocationEnter0858("Garruk","L056");g.Statuses.RemoveAll(s=>s.Name=="Hidden");gm.DebugResolveLocationReveal0858("L056","Garruk");Check(gm.DebugLocationDifficultTerrain0858("L056")&&g.HasStatus("Hidden"),"L056 Briarwood Verge executes Difficult Terrain and Hidden Reveal behavior.");
            gm.DebugLocationEnter0858("Garruk","L059");var l59=gm.DebugLocationByAuthored0858("L059");l59.UsedKeys0858.Clear();Check(gm.DebugModifyLocationHealing0858("Garruk",2)==3&&gm.DebugModifyLocationHealing0858("Garruk",2)==2,"L059 Roadside Shrine grants +1 only to the first recovery each Round.");
            SafeSearchDeck();var l60=gm.DebugLocationByAuthored0858("L060");l60.UsedKeys0858.Clear();Check(gm.DebugResolveLocationSearch0858("L060","Garruk")&&gm.DebugPendingRewardChoiceMode()=="LocationSearchKeepOne0858","L060 first Search each Quest draws 2 Search cards and chooses 1.");gm.DebugChooseRewardChoice("KEEP0");Check(!gm.DebugResolveLocationSearch0858("L060","Garruk"),"L060 special double-Search is consumed after its first use this Quest.");
            SafeSearchDeck();gm.DebugResolveLocationReveal0858("L065","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationSearchKeepOne0858","L065 Grand Market Reveal draws 2 Search cards and chooses 1.");gm.DebugChooseRewardChoice("KEEP0");
            gm.DebugLocationEnter0858("Garruk","L067");g.Actions=3;g.Health=Mathf.Max(1,g.MaxHealth-3);Check(gm.DebugCanLocationUtility0858("Garruk")&&gm.QuestSecondaryActionLabel0829=="Sanctuary","L067 Temple Square exposes its 1-Action Sanctuary Location Action.");hp=g.Health;gm.DebugResolveLocationUtility0858("Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationSanctuary0858","L067 Sanctuary offers Heal-or-Cleanse.");gm.DebugChooseRewardChoice("HEAL");Check(g.Health==Mathf.Min(g.MaxHealth,hp+2),"L067 Sanctuary healing resolves for 2 Health.");
            gm.DebugLocationEnter0858("Garruk","L076");Check(gm.DebugLocationDifficultTerrain0858("L076"),"L076 Sewer Junction executes authored Difficult Terrain.");var l76=gm.DebugLocationByAuthored0858("L076");l76.UsedKeys0858.Clear();Check(gm.DebugModifyLocationHealing0858("Garruk",2,"Potion")==1,"L076 Filthy Channels reduces non-Divine recovery by 1.");l76.UsedKeys0858.Clear();Check(gm.DebugModifyLocationHealing0858("Garruk",2,"Divine Healing")==2,"L076 Divine recovery bypasses the Filthy Channels penalty.");
            Check(!gm.DebugCanSearchLocation0858("Garruk","L088")&&gm.DebugCanSearchLocation0858("Garruk","L088",true),"L088 Ironhaven blocks normal Search while still allowing effect-granted Search exceptions.");

            // Ironspine.
            gm.DebugLocationEnter0858("Garruk","L090");gm.DebugResolveLocationReveal0858("L090","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationAttackOrArmor0858","L090 Liftworks exposes +1 Attack-or-Armor choice.");gm.DebugChooseRewardChoice("ARMOR");Check(gm.DebugLocationHeroArmor0858("Garruk")>=1,"L090 Armor choice creates a live Armor modifier.");
            gm.DebugResolveLocationReveal0858("L093","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationEncounterTopBottom0858"&&gm.DebugRegionalDeckKey0858()=="Ironspine","L093 Clan Ward inspects the Ironspine regional Encounter deck.");gm.DebugChooseRewardChoice("BOTTOM");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIII);gm.DebugSetWorldEvent0857("WE009");gm.DebugSetWorldEventMeter0857("Instability",2);gm.DebugLocationEnter0858("Garruk","L094");var l94=gm.DebugLocationByAuthored0858("L094");l94.UsedKeys0858.Clear();Check(gm.DebugLocationMoveSurcharge0858("Garruk","L090")==0,"L094 Stonegate Pass has no leave surcharge without a Cave-In Token.");gm.DebugSetWorldEventMeter0857("Instability",6);gm.DebugResolveLocationReveal0858("L094","Garruk");Check(gm.DebugHasLocationToken0858("L094","Cave-In"),"L094 high-Instability WE009 Reveal places its Cave-In Token.");Check(gm.DebugLocationMoveSurcharge0858("Garruk","L090")>=1,"L094 Cave-In Token makes leaving cost +1 Action.");g.Actions=3;Check(gm.DebugCanLocationUtility0858("Garruk")&&gm.DebugResolveLocationUtility0858("Garruk")&&!gm.DebugHasLocationToken0858("L094","Cave-In"),"L094 Cave-In can be cleared for its authored Location Action.");
            gm.DebugLocationEnter0858("Garruk","L098");gm.DebugResolveLocationReveal0858("L098","Garruk");var l98=gm.DebugLocationByAuthored0858("L098");Check(g.FreeSearchLocationId==l98.Id,"L098 Survey Chamber grants an immediate free Search.");
            gm.DebugSetExplorationDeck0828("L125","L126","L127");SafeSearchDeck();l98.Revealed=true;l98.Searched=false;l98.UsedKeys0858.Clear();Check(gm.ResolveGrantedSearch0851(g,"0.8.58c validation",l98.Id)&&gm.DebugPendingRewardChoiceMode()=="LocationSearchKeepOne0858","L098 effect-granted Search starts its authored draw-2/keep-1 choice without charging another Action.");gm.DebugChooseRewardChoice("KEEP0");Check(gm.DebugPendingRewardChoiceMode()=="LocationExploreReorder0858","L098 post-Search Location reorder waits for the Search choice instead of overwriting it.");gm.DebugChooseRewardChoice("ORDER0_1_2");
            var l104=gm.DebugLocationByAuthored0858("L104");gm.DebugLocationEnter0858("Garruk","L104");l104.Revealed=true;l104.Searched=false;l104.UsedKeys0858.Clear();gm.SearchDeck.Clear();gm.SearchDiscard.Clear();gm.SearchDeck.AddRange(new[]{"SD031","SD002","SD001","SD002"});Check(gm.ResolveGrantedSearch0851(g,"0.8.58c validation",l104.Id)&&gm.DebugPendingRewardChoiceMode()=="LocationSearchKeepOne0858","L104 Abandoned Clan Hall executes its authored draw-2/keep-1 Search.");gm.DebugChooseRewardChoice("KEEP0");Check(gm.DebugPendingRewardChoiceMode()=="SearchDrawOrHeal0852","L104 preserves a chosen Search card's nested reward choice instead of overwriting it with the post-Search effect.");gm.DebugChooseRewardChoice("DRAW");Check(gm.DebugPendingRewardChoiceMode()=="LocationSearchKeepOne0858","L104 runs its once-per-Quest post-Search draw-2/keep-1 only after the chosen Search card fully resolves.");gm.DebugChooseRewardChoice("KEEP0");

            // Verdant Reach.
            gm.DebugResolveLocationReveal0858("L125","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationExploreReorder0858","L125 Eldergrove Reveal exposes top-2 Location reorder instead of a text-only inspection.");gm.DebugChooseRewardChoice("ORDER0_1");
            gm.DebugLocationEnter0858("Garruk","L138");var l138=gm.DebugLocationByAuthored0858("L138");l138.UsedKeys0858.Clear();g.Health=Mathf.Max(1,g.MaxHealth-3);hp=g.Health;gm.DebugLocationSearchResolved0858("L138","Garruk");Check(g.Health==hp+1,"L138 Moonherb Glade recovers 1 Health once per Quest after Search.");
            gm.DebugLocationEnter0858("Garruk","L149");var l149=gm.DebugLocationByAuthored0858("L149");l149.UsedKeys0858.Clear();gm.DebugLocationSearchResolved0858("L149","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationEncounterDiscardOne0858","L149 Mirror Pool uses discard-one Encounter handling rather than reorder-only handling.");gm.DebugChooseRewardChoice("D0");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignIV);gm.DebugSetWorldEventMeter0857("Blight",6);gm.DebugLocationEnter0858("Garruk","L152");var l152=gm.DebugLocationByAuthored0858("L152");l152.UsedKeys0858.Clear();SafeSearchDeck();hp=g.Health;Check(gm.DebugResolveLocationSearch0858("L152","Garruk")&&gm.DebugPendingRewardChoiceMode()=="LocationSearchKeepOne0858","L152 Blooming Ruin first Search executes draw-2/keep-1.");gm.DebugChooseRewardChoice("KEEP0");gm.DebugLocationSearchResolved0858("L152","Garruk");Check(g.Health==hp-1,"L152 Blight 6+ post-Search penalty deals its authored 1 damage.");

            // Storm Coast.
            Check(!gm.DebugCanSearchLocation0858("Garruk","L162"),"L162 Ashenport blocks normal Search as a Safe Hub.");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignV);gm.DebugSetWorldEvent0857("WE018");gm.DebugSetWorldEventMeter0857("Doomtide",2);gm.DebugLocationEnter0858("Garruk","L168");Check(gm.DebugLocationMoveSurcharge0858("Garruk","L171")==0,"L171 Flooded Warehouse has no entry surcharge without a Flood Marker.");gm.DebugSetWorldEventMeter0857("Doomtide",3);gm.DebugRevealWorldEventLocation0857("L171","Garruk");Check(gm.DebugHasWorldEventMarker0857("L171","Flood")&&gm.DebugLocationMoveSurcharge0858("Garruk","L171")>=1,"L171 entry surcharge activates only while a Flood Marker is present.");
            SafeSearchDeck();Check(gm.DebugResolveLocationSearch0858("L171","Garruk")&&gm.DebugPendingRewardChoiceMode()=="LocationSearchKeepOne0858","L171 executes its draw-2/keep-1 Search rule.");gm.DebugChooseRewardChoice("KEEP0");
            gm.DebugResolveLocationReveal0858("L179","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationExploreReorder0858","L179 Stormwatch Isle Reveal supports Location deck reorder.");gm.DebugChooseRewardChoice("ORDER0_1");var l179=gm.DebugLocationByAuthored0858("L179");l179.UsedKeys0858.Clear();gm.DebugLocationSearchResolved0858("L179","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationEncounterReorder0858"&&gm.DebugRegionalDeckKey0858()=="StormCoast","L179 post-Search effect reorders the Storm Coast Encounter deck.");gm.DebugChooseRewardChoice("ORDER0_1");

            // Sunscar Expanse.
            Check(!gm.DebugCanSearchLocation0858("Garruk","L199"),"L199 Sunspire blocks normal Search as a Safe Hub.");
            gm.DebugResolveLocationReveal0858("L203","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationDualDeckTopBottom0858"&&gm.DebugRegionalDeckKey0858()=="Sunscar","L203 Glasswright Enclave combines Location + regional Encounter inspection into one non-overwriting choice.");gm.DebugChooseRewardChoice("TT");Check(!gm.HasPendingRewardChoice,"L203 dual-deck Reveal resolves without leaving a second hidden pending choice.");
            int bonus=g.NextAttackBonus;gm.DebugResolveLocationReveal0858("L207","Garruk");Check(g.NextAttackBonus==bonus+1,"L207 Salt Dunes Reveal grants +1 to the Hero's next Attack.");
            gm.DebugLocationEnter0858("Garruk","L210");var l210=gm.DebugLocationByAuthored0858("L210");l210.UsedKeys0858.Clear();gm.DebugLocationSearchResolved0858("L210","Garruk");Check(gm.DebugPendingRewardChoiceMode()=="LocationExploreReorder0858","L210 Fallen Obelisk post-Search effect reorders eligible Location cards.");gm.DebugChooseRewardChoice("ORDER0_1");
            gm.InitializeWorldEventCampaignState0857(CampaignContent0845.CampaignVI);gm.DebugSetWorldEventMeter0857("Sunfall",2);gm.DebugLocationEnter0858("Garruk","L211");var l211=gm.DebugLocationByAuthored0858("L211");l211.UsedKeys0858.Clear();g.Health=Mathf.Max(1,g.MaxHealth-3);hp=g.Health;gm.DebugLocationSearchResolved0858("L211","Garruk");Check(g.Health==hp+1,"L211 Ember Wells grants its once-per-Quest post-Search recovery below Sunfall 6.");l211.UsedKeys0858.Clear();gm.DebugSetWorldEventMeter0857("Sunfall",6);g.Health=Mathf.Max(1,g.MaxHealth-3);hp=g.Health;gm.DebugLocationSearchResolved0858("L211","Garruk");Check(g.Health==hp,"L211 Ember Wells correctly shuts off its recovery at Sunfall 6+.");
            gm.DebugSetWorldEventMeter0857("Sunfall",2);gm.DebugLocationEnter0858("Garruk","L218");var l218=gm.DebugLocationByAuthored0858("L218");l218.UsedKeys0858.Clear();Check(gm.DebugModifyLocationHealing0858("Garruk",2)==3&&gm.DebugModifyLocationHealing0858("Garruk",2)==2,"L218 Moonwell Ruins grants +1 only to the first recovery each Round at Sunfall 0-5.");l218.UsedKeys0858.Clear();gm.DebugSetWorldEventMeter0857("Sunfall",6);Check(gm.DebugModifyLocationHealing0858("Garruk",2)==2,"L218 Moonwell recovery bonus is disabled at Sunfall 6+.");
            gm.DebugLocationEnter0858("Garruk","L218");g.Statuses.RemoveAll(s=>s.Name=="Guarded");Check(gm.DebugLocationMoveSurcharge0858("Garruk","L222")>=1,"L222 Burning Causeway costs +1 Action to enter at Sunfall 6+.");gm.AddStatus(g,"Guarded",0,1,"Validation",true,g,"until end of round");Check(gm.DebugLocationMoveSurcharge0858("Garruk","L222")==0,"L222 Guarded Heroes bypass the Sunfall 6+ entry surcharge.");g.Statuses.RemoveAll(s=>s.Name=="Guarded");
            var l228=gm.DebugLocationByAuthored0858("L228");l228.FirstVisitResolved=false;int travel=l228.TravelEncounterRequests;gm.DebugResolveLocationFirstVisit0858("L228","Garruk");Check(l228.FirstVisitResolved&&l228.TravelEncounterRequests==travel,"L228 Fallen Sun Road resolves First Visit without a random Encounter during the finale route.");
            var l235=gm.DebugLocationByAuthored0858("L235");l235.FirstVisitResolved=false;travel=l235.TravelEncounterRequests;gm.DebugResolveLocationFirstVisit0858("L235","Garruk");Check(l235.FirstVisitResolved&&l235.TravelEncounterRequests==travel,"L235 Zenith Throne resolves its authored no-random-Encounter First Visit.");
            Check(gm.DebugRegionalDeckCount0858("L203")>0&&gm.DebugRegionalDeckKey0858()=="Sunscar","Sunscar Locations maintain a separate regional Encounter deck instead of consuming the Campaign I deck.");

            // Locked First Visit rule: enemy presence must not consume the tile's one First Visit.
            var l52=gm.DebugLocationByAuthored0858("L052");l52.FirstVisitResolved=false;var enemy=gm.Enemies.Values.FirstOrDefault(e=>!e.Defeated);string enemyOld=enemy?.LocationId;if(enemy!=null)enemy.LocationId=l52.Id;gm.DebugResolveLocationFirstVisit0858("L052","Garruk");Check(!l52.FirstVisitResolved,"First Visit remains pending when an Enemy is present on entry, preserving the locked Ascendance rule.");if(enemy!=null)enemy.LocationId=enemyOld;

            // Save capture without touching the user's on-disk save.
            l218.UsedKeys0858.Add("LOC0858:VALIDATION:SAVE");gm.DebugRegionalDeckCount0858("L203");
            var capture=typeof(AscendanceSaveSystem).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);var data=capture?.Invoke(null,new object[]{gm}) as AscendanceSaveData;var saved=data?.Locations?.FirstOrDefault(x=>x.Id==l218.Id);
            Check(data!=null&&saved!=null&&saved.UsedKeys0858.Contains("LOC0858:VALIDATION:SAVE"),"Location once-per-Quest/Round keys are captured into campaign save state.");
            Check(data!=null&&data.RegionalEncounterDeckKey0858=="Sunscar"&&data.RegionalEncounterDeckIds0858.Count>0,"regional Encounter deck order is captured into campaign save state.");
            Check(string.IsNullOrWhiteSpace(gm.PendingContentInstruction),"0.8.58c validation path finishes without a Location CONTENT PENDING fallback.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.58c TEST] PASS — Full Location Runtime Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.58c TEST] FAIL — Full Location Runtime Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
