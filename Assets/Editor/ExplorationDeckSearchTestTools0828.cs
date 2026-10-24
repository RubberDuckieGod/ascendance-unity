#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class ExplorationDeckSearchTestTools0828
    {
        [MenuItem("Ascendance/Validation/Test 0.8.28 - Exploration Deck Preview Reorder & Search (Play Mode)")]
        public static void TestExplorationDeckSearch()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.28 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.28 TEST] Game manager not found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.28 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.28 TEST] FAIL — {message}");}}
            var hero=gm.ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated);if(hero==null){Debug.LogError("[ASCENDANCE][0.8.28 TEST] FAIL — Need a living Hero.");yield break;}
            var river=gm.Locations["L001"];var ruins=gm.Locations["L003"];var woods=gm.Locations["L004"];

            gm.DebugSetLocationHidden0828(ruins.Id);gm.DebugSetLocationHidden0828(woods.Id);gm.DebugSetExplorationDeck0828("L003","L004");
            Check(gm.ExplorationDeck0828.SequenceEqual(new[]{"L003","L004"}),"Exploration deck can hold the real hidden graph Location cards in a deterministic order.");

            gm.DebugResolveSearchCard0827("SD010",river.Id,hero.Id);
            Check(gm.HasPendingRewardChoice&&gm.DebugPendingRewardChoiceMode()=="ExplorationTopTwoOrder"&&gm.PendingRewardChoice.RevealedCardIds.SequenceEqual(new[]{"L003","L004"}),"SD010 exposes the actual top two Exploration cards without consuming them.");
            Check(gm.RewardChoiceOptions.Count==2&&gm.RewardChoiceOptions.Any(o=>o.Key=="ORDER10"),"SD010 presents both legal top-two orders.");
            gm.DebugChooseRewardChoice("ORDER10");
            Check(gm.ExplorationDeck0828.SequenceEqual(new[]{"L004","L003"}),"SD010 reorders the live Exploration deck exactly as chosen.");

            gm.DebugSetLocationHidden0828(ruins.Id);gm.DebugSetLocationHidden0828(woods.Id);gm.DebugSetExplorationDeck0828("L004","L003");
            gm.DebugResolveSearchCard0827("SD005",river.Id,hero.Id);
            Check(ruins.Revealed&&!ruins.RevealResolved&&ruins.RevealEffectDeferred,"SD005 skips the illegal top card, reveals the connected Location, and defers its Reveal Effect.");
            Check(!gm.ExplorationDeck0828.Contains("L003")&&gm.ExplorationDeck0828.Contains("L004"),"SD005 consumes only the revealed legal Exploration card and returns the illegal set-aside card.");
            gm.DebugEnterLocation0828(hero.Id,ruins.Id);
            Check(ruins.RevealResolved&&!ruins.RevealEffectDeferred,"Entering an SD005-revealed Location resolves its deferred authored Reveal Effect exactly once.");

            gm.DebugSetLocationHidden0828(ruins.Id);gm.DebugSetExplorationDeck0828("L003","L004");
            gm.DebugResolveSearchCard0827("SD006",river.Id,hero.Id);
            Check(gm.HasPendingRewardChoice&&gm.DebugPendingRewardChoiceMode()=="ExplorationInspectConnected"&&gm.PendingRewardChoice.RevealedCardIds.SequenceEqual(new[]{"L003"}),"SD006 inspects a connected unrevealed Location without consuming its Exploration card.");
            Check(!ruins.Revealed&&gm.ExplorationDeck0828.Contains("L003"),"SD006 inspection alone leaves the Location hidden and the card in the live deck.");
            gm.DebugChooseRewardChoice("LEAVE");
            Check(!ruins.Revealed&&gm.ExplorationDeck0828.Contains("L003")&&!gm.HasPendingRewardChoice,"SD006 Leave keeps the Location unrevealed and closes the choice cleanly.");

            gm.DebugResolveSearchCard0827("SD006",river.Id,hero.Id);gm.DebugChooseRewardChoice("REVEAL");
            Check(ruins.Revealed&&ruins.RevealResolved&&!ruins.RevealEffectDeferred,"SD006 Reveal resolves the Location's normal Reveal Effect immediately.");
            Check(!gm.ExplorationDeck0828.Contains("L003"),"SD006 Reveal removes that Location from the Exploration deck.");

            gm.DebugSetLocationHidden0828(ruins.Id);gm.DebugSetLocationHidden0828(woods.Id);gm.DebugSetExplorationDeck0828("L003","L004");
            Check(gm.DebugBeginRowanTrailblazer0828("Rowan")&&gm.DebugPendingRewardChoiceMode()=="ExplorationTrailblazer","Rowan Improved Trailblazer now opens real top-two Exploration control.");
            Check(gm.RewardChoiceOptions.Any(o=>o.Key=="TOP0")&&gm.RewardChoiceOptions.Any(o=>o.Key=="TOP1"),"Improved Trailblazer lets Rowan choose which card stays on top.");
            gm.DebugChooseRewardChoice("TOP1");
            Check(gm.ExplorationDeck0828.First()=="L004"&&gm.ExplorationDeck0828.Last()=="L003","Improved Trailblazer keeps the chosen card on top and places the other on the bottom.");

            Check(!gm.HasPendingRewardChoice,"All 0.8.28 Exploration choices resolve cleanly.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.28 TEST] PASS — Exploration Deck Preview, Reorder & Search Integration completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.28 TEST] FAIL — Exploration Deck Preview, Reorder & Search Integration completed: {pass} passed, {fail} failed.");
            yield break;
        }
    }
}
#endif
