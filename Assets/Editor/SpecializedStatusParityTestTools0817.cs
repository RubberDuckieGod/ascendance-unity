#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class SpecializedStatusParityTestTools0817
    {
        [MenuItem("Ascendance/Validation/Test 0.8.17 - Live Specialized Conditions (Play Mode)")]
        public static void TestSpecializedStatusParity()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.17 TEST] Enter Play Mode before running the Live Specialized Conditions test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.TurnHero != null || gm.ParticipatingHeroes.Any(h => h.Moving))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.17 TEST] Run this at the beginning of a Hero Phase with no turn, movement, or Reaction window in progress.");
                return;
            }

            var party = gm.ParticipatingHeroes.ToList();
            if (party.Count < 2 || !gm.Enemies.Values.Any(e => !e.Defeated))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.17 TEST] Use a party of at least 2 Heroes with at least one living Enemy.");
                return;
            }

            bool pass = true;
            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.17 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.17 TEST] FAIL — " + label); }
            }

            var source = party[0];
            var other = party[1];
            var enemy = gm.Enemies.Values.First(e => !e.Defeated);

            foreach (var h in party)
            {
                h.Statuses.Clear();
                h.Health = h.MaxHealth;
                h.Downed = false;
                h.Dead = false;
                h.Actions = 0;
                h.AttackOnlyActions = 0;
                h.TurnCompleted = false;
                h.FreeMoveReady = false;
                h.LocationId = "L000";
            }
            foreach (var e in gm.Enemies.Values)
            {
                e.Statuses.Clear();
                e.Health = e.MaxHealth;
                e.ActivatedThisRound = false;
            }
            enemy.LocationId = "L000";

            // Camouflage: current card text must create Hidden, and Hidden ends early on Attack.
            gm.DebugApplyCardStatuses(source.Id, "H005-010", null, false);
            Check(source.HasStatus("Hidden"), "Camouflage applies Hidden from the authored card text.");
            Check(gm.DebugBreakHiddenOnAttack(source.Id), "A Hero Attack breaks Hidden.");
            Check(!source.HasStatus("Hidden"), "Hidden is removed immediately when that Hero attacks.");

            // Holy Challenge: Taunted must force the source Hero as the next legal attack target.
            other.Health = 1; // make the non-taunter the normal lowest-Health target.
            gm.DebugApplyCardStatuses(source.Id, "H006-010", enemy.Id, false);
            Check(enemy.Statuses.Any(s => s.Name == "Taunted"), "Holy Challenge applies Taunted from the authored card text.");
            Check(gm.DebugChooseEnemyAttackTarget(enemy.Id) == source.Id, "Taunted forces the Enemy to target the Hero who applied Taunted when able.");
            source.LocationId = "L001";
            Check(gm.DebugChooseEnemyAttackTarget(enemy.Id) == other.Id, "Taunted falls back to a legal target when the taunting Hero cannot be targeted.");
            source.LocationId = "L000";
            enemy.Statuses.Clear();
            other.Health = other.MaxHealth;

            // Sweeping Leg: Knocked Down spends the Enemy's modeled activation standing.
            gm.DebugApplyCardStatuses(source.Id, "H009-003", enemy.Id, true);
            Check(enemy.Statuses.Any(s => s.Name == "Knocked Down"), "Sweeping Leg applies Knocked Down after dealing damage.");
            Check(gm.DebugResolveEnemyKnockedDownActivation(enemy.Id), "Knocked Down consumes the Enemy's modeled activation to Stand.");
            Check(!enemy.Statuses.Any(s => s.Name == "Knocked Down") && enemy.ActivatedThisRound,
                "Standing removes Knocked Down and completes that Enemy activation.");
            enemy.ActivatedThisRound = false;

            // Hero-side Knocked Down uses the same authored first-Action-to-Stand rule.
            gm.AddStatus(other, "Knocked Down", 1, 0, "0.8.17 Hero Knocked Down test", false, source);
            gm.SelectHero(other.Id);
            Check(gm.TryStartHeroTurn(other), "A Knocked Down Hero can begin the activation to Stand.");
            Check(other.Actions == 2 && !other.HasStatus("Knocked Down"),
                "Knocked Down automatically spends the first of the normal 3 Actions to Stand, leaving 2.");
            gm.EndHeroTurn();

            // Shocked: next additional Action is prevented and Shocked is consumed.
            other.TurnCompleted = false;
            other.Actions = 0;
            gm.AddStatus(other, "Shocked", 0, 1, "Shock Cartridge", false, source, "The target gains Shocked until the end of the round.");
            Check(!gm.DebugTryGrantAdditionalHeroAction(other.Id, 1, "0.8.17 bonus Action test"),
                "Shocked prevents the next additional Action gain.");
            Check(other.Actions == 0 && !other.HasStatus("Shocked"), "Shocked removes itself after preventing that Action gain.");

            // Explosive Charge: normal Burning is persistent and deals 1 at end of activation.
            enemy.Statuses.Clear(); enemy.Health = enemy.MaxHealth;
            gm.DebugApplyCardStatuses(source.Id, "H010-005", enemy.Id, true);
            Check(enemy.Statuses.Any(s => s.Name == "Burning"), "Explosive Charge applies persistent Burning.");
            int beforeBurn = enemy.Health;
            gm.DebugResolveEnemyEndActivationConditions(enemy.Id, false);
            Check(enemy.Health == beforeBurn - 1, "Persistent Burning deals exactly 1 damage at end of Enemy activation.");
            Check(enemy.Statuses.Any(s => s.Name == "Burning"), "Normal Burning remains until removed after dealing activation-end damage.");

            // Meteor Shard: its explicit card text overrides normal Burning into a one-shot start-of-activation burn.
            enemy.Statuses.Clear(); enemy.Health = enemy.MaxHealth;
            var meteor = HeroDatabase.GetCard("H004-006");
            gm.AddStatus(enemy, "Burning", 1, 0, meteor.Name, false, source, meteor.Rules);
            Check(enemy.Statuses.Any(s => s.Name == "Burning" && s.Source == "Meteor Shard"), "Meteor Shard one-shot Burning records its authored source once the Arcane prerequisite applies it.");
            int beforeMeteor = enemy.Health;
            gm.DebugResolveEnemyStartActivationConditions(enemy.Id);
            Check(enemy.Health == beforeMeteor - 1, "Meteor Shard Burning deals exactly 1 damage at start of the next Enemy activation.");
            Check(!enemy.Statuses.Any(s => s.Name == "Burning"), "Meteor Shard Burning removes itself after the one-shot damage.");

            // Briar Storm: Bleeding only damages an Enemy that actually performs an Attack.
            enemy.Statuses.Clear(); enemy.Health = enemy.MaxHealth;
            gm.DebugApplyCardStatuses(source.Id, "H007-007", enemy.Id, true);
            Check(enemy.Statuses.Any(s => s.Name == "Bleeding"), "Briar Storm applies persistent Bleeding to a damaged Enemy.");
            int beforeNoAttack = enemy.Health;
            gm.DebugResolveEnemyEndActivationConditions(enemy.Id, false);
            Check(enemy.Health == beforeNoAttack, "Bleeding deals no damage when the Enemy did not perform an Attack.");
            gm.DebugResolveEnemyEndActivationConditions(enemy.Id, true);
            Check(enemy.Health == beforeNoAttack - 1, "Bleeding deals exactly 1 damage after the Enemy performs an Attack.");
            Check(enemy.Statuses.Any(s => s.Name == "Bleeding"), "Bleeding remains until removed after triggering.");

            if (pass) Debug.Log("[ASCENDANCE][0.8.17 TEST] PASS — Live Specialized Conditions test completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.17 TEST] FAIL — one or more Live Specialized Conditions checks failed.");
        }
    }
}
#endif
