#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class EncounterDeckControlTestTools0825
    {
        [MenuItem("Ascendance/Validation/Test 0.8.25 - Encounter Deck Control & Remaining Rules (Play Mode)")]
        public static void TestEncounterDeckControl()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.25 TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.25 TEST] Game manager not found.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool condition,string message){if(condition){pass++;Debug.Log($"[ASCENDANCE][0.8.25 TEST] PASS — {message}");}else{fail++;Debug.LogError($"[ASCENDANCE][0.8.25 TEST] FAIL — {message}");}}
            var heroes=gm.ParticipatingHeroes.Where(h=>!h.IsDefeated).OrderBy(h=>h.PartyIndex).ToList();
            if(heroes.Count<1){Debug.LogError("[ASCENDANCE][0.8.25 TEST] FAIL — Need at least 1 living Hero.");yield break;}
            var hero=heroes[0];var forest=gm.Locations["L002"];var ruins=gm.Locations["L003"];forest.Revealed=ruins.Revealed=true;

            void PutTop(params string[] ids)
            {
                foreach(var id in ids)
                {
                    int at=gm.EncounterDeck.FindIndex(x=>x==id);if(at>=0)gm.EncounterDeck.RemoveAt(at);else{int d=gm.EncounterDiscard.FindIndex(x=>x==id);if(d>=0){gm.EncounterDiscard.RemoveAt(d);} }
                }
                for(int i=ids.Length-1;i>=0;i--)gm.EncounterDeck.Insert(0,ids[i]);
            }

            // EN028 Severity 3-4: reveal top 2 and bottom exactly one chosen card.
            PutTop("EN028","EN001","EN002","EN003");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,2); // d6 2 + WE001 = Severity 3.
            Check(gm.HasPendingEncounterDeckControl&&gm.DebugPendingDeckControlMode()=="BottomOneTop2","EN028 Severity 3-4 opens a real top-two / bottom-one Encounter deck-control choice.");
            Check(gm.PendingEncounterDeckControl!=null&&gm.PendingEncounterDeckControl.RevealedCardIds.SequenceEqual(new[]{"EN001","EN002"}),"EN028 Severity 3-4 reveals the actual next two Encounter cards in deck order.");
            Check(gm.DebugChooseEncounterDeckControl("BOTTOM0")&&gm.DebugEncounterDeckTop(2)=="EN002,EN003","Choosing EN028's first revealed card moves that exact card to the bottom and exposes the other card on top.");

            // EN028 Severity 5-6: choose any permutation of the actual top 3.
            PutTop("EN028","EN001","EN002","EN003");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,4); // Severity 5.
            Check(gm.HasPendingEncounterDeckControl&&gm.DebugPendingDeckControlMode()=="ReorderTop3","EN028 Severity 5-6 opens a real top-three reorder choice.");
            Check(gm.EncounterDeckControlOptions.Count==6,"EN028 top-three control exposes all six legal orderings.");
            Check(gm.DebugChooseEncounterDeckControl("PERM201")&&gm.DebugEncounterDeckTop(3)=="EN003,EN001,EN002","EN028 applies the selected top-three ordering to the live Encounter deck.");

            // EN003 reward: 10 Gold plus optional bottom of the actual top card after victory.
            int goldBeforeScout=gm.PartyGold;PutTop("EN003","EN004","EN005");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,1);
            string scoutEncounter=gm.DebugLatestActiveEncounterId();var scout=gm.DebugEncounterEnemy(scoutEncounter);if(scout!=null)gm.DealDirectDamage(scout,scout.Health,"0.8.25 Scout Patrol validation",hero);
            Check(gm.PartyGold==goldBeforeScout+10&&gm.HasPendingEncounterDeckControl&&gm.DebugPendingDeckControlMode()=="MayBottomTop","EN003 victory awards 10 Gold and opens its may-bottom-top-card reward control.");
            Check(gm.PendingEncounterDeckControl!=null&&gm.PendingEncounterDeckControl.RevealedCardIds.FirstOrDefault()=="EN004","EN003 inspects the actual top Encounter card after the Scout Patrol card leaves the deck.");
            Check(gm.DebugChooseEncounterDeckControl("BOTTOM0")&&gm.EncounterDeck.FirstOrDefault()=="EN005","EN003 can put the inspected top card on the bottom of the live deck.");

            // EN022 reward: 25 Gold plus top-two reorder after victory.
            int goldBeforeTower=gm.PartyGold;PutTop("EN022","EN001","EN002");gm.DebugResolveTravelEncounter(ruins.Id,hero.Id,1);
            string towerEncounter=gm.DebugLatestActiveEncounterId();var tower=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==towerEncounter);
            if(tower!=null)foreach(var id in tower.SpawnedEnemyIds.ToList())if(gm.Enemies.TryGetValue(id,out var enemy)&&!enemy.Defeated)gm.DealDirectDamage(enemy,enemy.Health,"0.8.25 Watchtower validation",hero);
            Check(gm.PartyGold==goldBeforeTower+25&&gm.HasPendingEncounterDeckControl&&gm.DebugPendingDeckControlMode()=="ReorderTop2","EN022 victory awards 25 Gold and opens its top-two reorder reward control.");
            Check(gm.DebugChooseEncounterDeckControl("SWAP")&&gm.DebugEncounterDeckTop(2)=="EN002,EN001","EN022 applies the selected top-two order to the live Encounter deck.");

            // EN009 Severity 7+: E011 gets +1 Attack for exactly its first activation.
            PutTop("EN009");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,6);
            string campEncounter=gm.DebugLatestActiveEncounterId();var camp=gm.ActiveEncounters.FirstOrDefault(e=>e.InstanceId==campEncounter);var saboteur=camp==null?null:camp.SpawnedEnemyIds.Select(id=>gm.Enemies[id]).FirstOrDefault(e=>e.TemplateId=="E011");
            Check(saboteur!=null&&gm.DebugEffectiveEnemyAttack(saboteur.Id)==saboteur.Attack+1,"EN009 Severity 7 gives its spawned Saboteur +1 Attack before its first activation.");
            if(saboteur!=null)gm.DebugAdvanceEnemyActivationStatusClock(saboteur.Id);
            Check(saboteur!=null&&gm.DebugEffectiveEnemyAttack(saboteur.Id)==saboteur.Attack,"EN009 Saboteur +1 Attack expires at the end of that first activation.");

            // EN027 Severity 7+: mandatory follow-up draw at a fixed 3-4 severity band, without consuming a queued normal Severity modifier.
            string originalTraits=forest.Traits;forest.Traits=(forest.Traits??string.Empty)+"; Road";
            PutTop("EN027","EN001");gm.DebugResolveTravelEncounter(forest.Id,hero.Id,6);
            Check(gm.LastEncounterCardId=="EN001"&&gm.LastEncounterSeverity==3,"EN027 Severity 7 immediately resolves its additional Encounter in the fixed Severity 3-4 band.");
            int queuedBefore=gm.NextTravelEncounterSeverityModifier;gm.DebugQueueTravelSeverity(1);PutTop("EN002");gm.DebugResolveFixedTravelEncounter(forest.Id,hero.Id,3);
            Check(gm.LastEncounterCardId=="EN002"&&gm.LastEncounterSeverity==3&&gm.NextTravelEncounterSeverityModifier==queuedBefore+1,"A mandatory fixed-severity Encounter does not consume a queued modifier meant for the next normal Severity roll.");
            forest.Traits=originalTraits;

            Check(!gm.HasPendingEncounterDeckControl,"All 0.8.25 deck-control choices used by the validator resolve cleanly.");
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.25 TEST] PASS — Encounter Deck Control & Remaining Campaign I Rules completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.25 TEST] FAIL — Encounter Deck Control & Remaining Campaign I Rules completed: {pass} passed, {fail} failed.");
            yield break;
        }
    }
}
#endif
