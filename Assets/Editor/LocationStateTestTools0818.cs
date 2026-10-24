#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class LocationStateTestTools0818
    {
        [MenuItem("Ascendance/Validation/Test 0.8.18 - Location Tags & State (Play Mode)")]
        public static void TestLocationState()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.18 TEST] Enter Play Mode before running the Location Tags & State test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.TurnHero != null || gm.ParticipatingHeroes.Any(h => h.Moving))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.18 TEST] Run this at the beginning of a Hero Phase with no turn, movement, or Reaction window in progress.");
                return;
            }

            var party = gm.ParticipatingHeroes.ToList();
            if (party.Count < 2 || !gm.Enemies.Values.Any(e => !e.Defeated))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.18 TEST] Use a party of at least 2 Heroes with at least one living Enemy.");
                return;
            }

            bool pass = true;
            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.18 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.18 TEST] FAIL — " + label); }
            }

            foreach (var h in party)
            {
                h.Statuses.Clear(); h.Health=h.MaxHealth; h.Downed=false; h.Dead=false; h.Actions=0; h.AttackOnlyActions=0;
                h.TurnCompleted=false; h.FreeMoveReady=false; h.FreeMoveIgnoresEngagement=false; h.FreeMoveIgnoresDifficultTerrain=false;
                h.UsedRoundAbilities.Clear(); h.UsedTurnAbilities.Clear(); h.LocationId="L000";
            }
            foreach (var e in gm.Enemies.Values) { e.Statuses.Clear(); e.Health=e.MaxHealth; e.LocationId="L000"; e.ActivatedThisRound=false; }

            var stonebrook=gm.Locations["L000"];
            var river=gm.Locations["L001"];
            var forest=gm.Locations["L002"];
            var ruins=gm.Locations["L003"];
            var dense=gm.Locations["L004"];
            forest.Revealed=true; river.Revealed=true; dense.Revealed=true;

            // v10.52 metadata is bound by Location name while legacy vertical-slice graph IDs remain stable.
            Check(dense.Name=="Dense Woods" && dense.AuthoredLocationId=="L002" && dense.Terrain=="Forest", "Legacy L004 keeps its graph ID while binding to authored L002 Dense Woods metadata.");
            Check(river.Name=="River Crossing" && river.AuthoredLocationId=="L004" && gm.IsDifficultTerrain(river.Id), "River Crossing binds to authored L004 and exposes Difficult Terrain as a real Location tag.");
            Check(gm.IsDifficultTerrain(dense.Id) && dense.Keywords.IndexOf("Concealment",StringComparison.OrdinalIgnoreCase)>=0, "Dense Woods exposes Difficult Terrain and Concealment from authored Keywords.");
            Check(gm.LocationHasTag(ruins.Id,"Cover") && gm.LocationHasTag(stonebrook.Id,"Safe Haven"), "Printed Traits/Keywords are queryable as runtime Location tags.");

            // Find a current party Hero whose own Equipment/innate does not bypass Difficult Terrain.
            foreach(var h in party){h.LocationId=dense.Id;h.FreeMoveReady=false;h.UsedRoundAbilities.Clear();}
            var mover=party.FirstOrDefault(h=>gm.DebugRequiredPaidActionsToMove(h.Id,forest.Id)==2);
            if(mover==null)
            {
                Debug.LogError("[ASCENDANCE][0.8.18 TEST] FAIL — Party has no Hero available for the unmodified Difficult Terrain exit test.");
                pass=false;
                mover=party[0];
            }

            gm.SelectHero(mover.Id);
            Check(gm.TryStartHeroTurn(mover), "Hero starts a normal turn for Difficult Terrain validation.");
            mover.Actions=1; mover.AttackOnlyActions=0;
            Check(!gm.CanMoveTo(forest.Id), "A Hero with only 1 general Action cannot voluntarily leave Difficult Terrain.");
            mover.Actions=2;
            Check(gm.CanMoveTo(forest.Id) && gm.DebugRequiredPaidActionsToMove(mover.Id,forest.Id)==2, "Leaving Difficult Terrain requires 2 Move Actions / 2 paid Actions when no free Move or ignore applies.");
            mover.Actions=1; mover.FreeMoveReady=true;
            Check(gm.CanMoveTo(forest.Id) && gm.DebugRequiredPaidActionsToMove(mover.Id,forest.Id)==1, "A free Move Action can satisfy one of the two Difficult Terrain Move requirements.");
            mover.FreeMoveReady=false; mover.Actions=0; gm.EndHeroTurn();

            // Entering Difficult Terrain has no inherent surcharge.
            mover.TurnCompleted=false; mover.LocationId=forest.Id; mover.Actions=0; mover.UsedRoundAbilities.Clear();
            gm.SelectHero(mover.Id); Check(gm.TryStartHeroTurn(mover), "Hero starts a fresh turn for Difficult Terrain entry validation.");
            mover.Actions=1; mover.AttackOnlyActions=0;
            Check(gm.CanMoveTo(dense.Id) && gm.DebugRequiredPaidActionsToMove(mover.Id,dense.Id)==1, "Entering Difficult Terrain still requires only the normal 1 Move Action.");
            mover.Actions=0; gm.EndHeroTurn();

            // Reveal is a placement event; First Visit remains a separate entry event.
            ruins.Revealed=false; ruins.RevealResolved=false; ruins.FirstVisitResolved=false;
            mover.TurnCompleted=false; mover.LocationId=river.Id; mover.Actions=0;
            gm.SelectHero(mover.Id); Check(gm.TryStartHeroTurn(mover), "Hero starts a turn for Reveal/First Visit timing validation.");
            gm.Explore();
            Check(ruins.Revealed && ruins.RevealResolved, "Explore reveals Ancient Ruins and resolves its Reveal Effect immediately.");
            Check(!ruins.FirstVisitResolved, "Reveal does not consume the separate First Visit event.");
            gm.EndHeroTurn();

            var enemy=gm.Enemies.Values.First(e=>!e.Defeated);
            enemy.LocationId=ruins.Id; mover.LocationId=ruins.Id; ruins.FirstVisitResolved=false;
            gm.DebugResolveLocationFirstVisit(ruins.Id,mover.Id);
            Check(!ruins.FirstVisitResolved, "First Visit remains pending when the entering Hero finds an Enemy present, per the locked First Visit rule.");
            enemy.LocationId="L000";
            gm.DebugResolveLocationFirstVisit(ruins.Id,mover.Id);
            Check(ruins.FirstVisitResolved, "First Visit resolves once a Hero enters/qualifies with no Enemy present.");

            // Temporary Arcane tags drive real card prerequisites and expire on their source clock.
            gm.DebugAddLocationTag(ruins.Id,"Arcane",mover.Id,"0.8.18 Arcane test",StatusExpiryTiming.StartOfSourceTurn,1);
            Check(gm.IsArcaneLocation(ruins.Id), "Temporary Arcane is a real queryable Location tag.");
            enemy.LocationId=ruins.Id; enemy.Statuses.Clear(); mover.LocationId=ruins.Id;
            gm.DebugApplyCardStatuses(mover.Id,"H004-006",enemy.Id,true);
            Check(enemy.Statuses.Any(s=>s.Name=="Burning"), "Meteor Shard now applies its Burning prerequisite from actual Arcane Location state.");
            enemy.Statuses.Clear();
            gm.DebugAdvanceLocationTagClock(mover.Id,StatusExpiryTiming.StartOfSourceTurn);
            Check(!gm.IsArcaneLocation(ruins.Id), "Source-timed Arcane expires at the start of the creating Hero's next turn.");

            // Consecrated is also real state: first recovery for each Hero in the Location each Round gets +1.
            var healTarget=party.First(h=>h.Id!=mover.Id);
            healTarget.LocationId=ruins.Id; healTarget.Health=Mathf.Max(1,healTarget.MaxHealth-5); healTarget.UsedRoundAbilities.Clear();
            gm.DebugAddLocationTag(ruins.Id,"Consecrated",mover.Id,"0.8.18 Consecrated test",StatusExpiryTiming.StartOfSourceTurn,1);
            int before=healTarget.Health;
            gm.HealHero(healTarget,1,"0.8.18 Consecrated heal #1",mover);
            Check(healTarget.Health==before+2, "Consecrated adds +1 Health to that Hero's first recovery in the Location this Round.");
            int afterFirst=healTarget.Health;
            gm.HealHero(healTarget,1,"0.8.18 Consecrated heal #2",mover);
            Check(healTarget.Health==afterFirst+1, "Consecrated does not grant its extra Health twice to the same Hero in one Round.");

            if (pass) Debug.Log("[ASCENDANCE][0.8.18 TEST] PASS — Location Tags & Location-State Enforcement test completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.18 TEST] FAIL — one or more Location Tags & Location-State checks failed.");
        }
    }
}
#endif
