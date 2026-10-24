#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class TravelEncounterDeckExecutionTestTools0853
    {
        [MenuItem("Ascendance/Validation/Test 0.8.53 - Full Travel Encounter Deck Execution (Play Mode)")]
        public static void Test0853()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.53 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.53 TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static LocationState MatchingLocation(AscendanceGameManager gm,string cardId)
        {
            var location=gm.Locations.Values.FirstOrDefault(l=>gm.DebugMatchesEncounterLocation(cardId,l.Id));
            if(location!=null)location.Revealed=true;
            return location;
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.53 TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.53 TEST] FAIL — "+label);}}

            Check(AscendanceGameManager.BuildVersion=="0.8.53","Build identifies the Full Travel Encounter Deck Execution milestone.");
            FullContentCatalog0850.EnsureLoaded();
            Check(FullContentCatalog0850.EncounterCount==105,"full authored catalog still contains all 105 Encounter definitions.");
            Check(SearchEncounterContent0820.EncounterCards.Count==105,"runtime Encounter catalog exposes all 105 authored definitions.");
            Check(gm.DebugCampaignIEncounterDeckDefinitionCount0853()==30,"Stonebrook Campaign I scope is exactly EN001-EN030.");
            Check(gm.DebugCampaignIEncounterPhysicalCount0853()==35,"physical Campaign I Travel deck contains 35 authored copies.");
            var campaignI=Enumerable.Range(1,30).Select(i=>$"EN{i:000}").ToArray();
            Check(campaignI.All(id=>SearchEncounterContent0820.EncounterCards.ContainsKey(id)),"Campaign I Encounter IDs are contiguous EN001-EN030.");
            Check(campaignI.All(gm.IsEncounterCardExecutable0853),"all EN001-EN030 have executable runtime paths.");
            Check(Enumerable.Range(31,75).Select(i=>$"EN{i:000}").All(id=>!FullContentCatalog0850.IsCampaignIEncounterId(id)),"EN031-EN105 remain excluded from the Stonebrook live Travel deck.");
            Check(SearchEncounterContent0820.BuildSearchDeck().Count==104&&SearchEncounterContent0820.SearchCards.Count==100,"0.8.52a Full Search Deck Execution remains intact.");
            Check(HeroDatabase.Heroes.Count==16&&HeroDatabase.CardDefinitionCount==323,"0.8.51 Hero content remains intact.");

            gm.SetParty(new[]{"Garruk"},"Garruk",true);gm.DebugStartQuest0829("Q001");yield return null;
            var hero=gm.Heroes["Garruk"];

            // EN010 — player-facing Broken Bridge choice.
            var en010=MatchingLocation(gm,"EN010");Check(en010!=null,"EN010 has an eligible Campaign I Location.");
            if(en010!=null)
            {
                gm.DebugSetHeroAt0829("Garruk",en010.Id,3);gm.DebugSetEncounterDeckTop("EN010");gm.DebugResolveFixedTravelEncounter(en010.Id,"Garruk",3);
                Check(gm.DebugPendingRewardChoiceMode()=="EncounterBrokenBridgeHeroes0853","EN010 Severity 3-4 opens the per-Hero Action-or-damage choice.");
                var option=gm.RewardChoiceOptions.FirstOrDefault();Check(option!=null&&gm.DebugChooseRewardChoice(option.Key),"EN010 authored choice resolves through the live choice UI.");
            }

            // EN015 — pay-or-fight is held as a live Encounter choice instead of CONTENT PENDING.
            var en015=MatchingLocation(gm,"EN015");Check(en015!=null,"EN015 has an eligible Campaign I Location.");
            if(en015!=null)
            {
                gm.DebugSetPartyGold(20);gm.DebugSetHeroAt0829("Garruk",en015.Id,3);gm.DebugSetEncounterDeckTop("EN015");gm.DebugResolveFixedTravelEncounter(en015.Id,"Garruk",1);
                Check(gm.DebugPendingRewardChoiceMode()?.StartsWith("EncounterToll0853")==true&&gm.DebugActiveEncounterCount()>0,"EN015 remains active while awaiting Pay-or-Fight.");
                int before=gm.PartyGold;Check(gm.DebugChooseRewardChoice("PAY")&&gm.PartyGold==before-5,"EN015 Severity 1 payment spends exactly 5 Gold and resolves without combat.");
            }

            // EN020 — next-Quest Inspired carryover is real and survives scenario reset.
            var en020=MatchingLocation(gm,"EN020");Check(en020!=null,"EN020 has an eligible Campaign I Location.");
            if(en020!=null)
            {
                gm.DebugSetHeroAt0829("Garruk",en020.Id,3);hero.Health=Mathf.Max(1,hero.MaxHealth-4);gm.DebugSetEncounterDeckTop("EN020");gm.DebugResolveFixedTravelEncounter(en020.Id,"Garruk",7);
                Check(hero.Health>=hero.MaxHealth-2,"EN020 Severity 7 heals 2 Health.");
                gm.DebugStartQuest0829("Q002");yield return null;hero=gm.Heroes["Garruk"];
                Check(hero.HasStatus("Inspired"),"EN020 Severity 7 carries Inspired into the first Round of the next Quest.");
            }

            // EN019 — delayed Lost Child search becomes a usable one-Action next-Quest utility.
            gm.DebugStartQuest0829("Q001");yield return null;hero=gm.Heroes["Garruk"];
            var en019=MatchingLocation(gm,"EN019");Check(en019!=null,"EN019 has an eligible Campaign I Location.");
            if(en019!=null)
            {
                gm.DebugSetHeroAt0829("Garruk",en019.Id,3);gm.DebugSetEncounterDeckTop("EN019");gm.DebugResolveFixedTravelEncounter(en019.Id,"Garruk",3);
                Check(!gm.DebugCanUseEncounterUtility0853("Garruk"),"EN019 delayed search cannot be used in the Quest where it was found.");
                gm.DebugStartQuest0829("Q002");yield return null;hero=gm.Heroes["Garruk"];gm.DebugSetHeroAt0829("Garruk","L000",3);
                Check(gm.DebugCanUseEncounterUtility0853("Garruk")&&gm.SearchUtilityLabel0852(hero).Contains("Lost Child"),"EN019 delayed search exposes a live one-Action utility in the next Quest.");
                int gold=gm.PartyGold,rep=gm.GeneralReputation,actions=hero.Actions;gm.SelectHero("Garruk");Check(gm.UseSearchUtility0852()&&hero.Actions==actions-1&&gm.PartyGold==gold+10&&gm.GeneralReputation>=rep,"EN019 delayed search spends 1 Action and grants its authored Gold/Reputation reward.");
            }

            // EN029 — Traveling Merchant is an actual buy/sell session.
            gm.DebugStartQuest0829("Q001");yield return null;hero=gm.Heroes["Garruk"];var en029=MatchingLocation(gm,"EN029");Check(en029!=null,"EN029 has an eligible Campaign I Location.");
            if(en029!=null)
            {
                gm.DebugSetHeroAt0829("Garruk",en029.Id,3);gm.DebugSetEncounterDeckTop("EN029");gm.DebugResolveFixedTravelEncounter(en029.Id,"Garruk",5);
                Check(gm.DebugPendingRewardChoiceMode()?.StartsWith("EncounterMerchant0853")==true,"EN029 opens the live Traveling Merchant session.");
                Check(gm.RewardChoiceOptions.Any(o=>o.Key=="CLOSE"),"EN029 merchant session exposes a player-controlled Close action.");
                gm.DebugChooseRewardChoice("CLOSE");
            }

            // EN030 — Quiet Road's free Rest is player-selected and executable.
            gm.DebugStartQuest0829("Q001");yield return null;hero=gm.Heroes["Garruk"];var en030=MatchingLocation(gm,"EN030");Check(en030!=null,"EN030 has an eligible Campaign I Location.");
            if(en030!=null)
            {
                gm.DebugSetHeroAt0829("Garruk",en030.Id,3);hero.Health=Mathf.Max(1,hero.MaxHealth-3);int before=hero.Health;gm.DebugSetEncounterDeckTop("EN030");gm.DebugResolveFixedTravelEncounter(en030.Id,"Garruk",3);
                Check(gm.DebugPendingRewardChoiceMode()=="EncounterFreeRestHero0853","EN030 Severity 3-4 opens the free-Rest Hero choice.");
                var option=gm.RewardChoiceOptions.FirstOrDefault(o=>o.Key.StartsWith("HERO:"));if(option!=null)gm.DebugChooseRewardChoice(option.Key);
                Check(hero.Health>before,"EN030 free Rest resolves recovery without spending a Hero Action.");
            }

            // EN018 — Shamanic Totem's first-Hero-Attack penalty is live once per Round.
            gm.DebugStartQuest0829("Q001");yield return null;hero=gm.Heroes["Garruk"];var en018=MatchingLocation(gm,"EN018");Check(en018!=null,"EN018 has an eligible Campaign I Location.");
            if(en018!=null)
            {
                gm.DebugSetHeroAt0829("Garruk",en018.Id,3);gm.DebugSetEncounterDeckTop("EN018");gm.DebugResolveFixedTravelEncounter(en018.Id,"Garruk",3);string inst=gm.DebugLatestActiveEncounterId();var enemy=gm.DebugEncounterEnemy(inst);
                Check(enemy!=null&&gm.DebugEncounterAttackModifier0853("Garruk",enemy.Id)==-1,"EN018 applies -1 Attack to the first Hero Attack while the Totem remains.");
                Check(gm.DebugEncounterAttackModifier0853("Garruk",enemy.Id)==0,"EN018 attack penalty is consumed after the first Hero Attack that Round.");
            }

            Check(string.IsNullOrEmpty(gm.PendingContentInstruction),"0.8.53 validation path finishes without a Campaign I CONTENT PENDING fallback.");

            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.53 TEST] PASS — Full Travel Encounter Deck Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.53 TEST] FAIL — Full Travel Encounter Deck Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
