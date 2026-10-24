#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ascendance;

namespace Ascendance.EditorTools
{
    public static class StatusEffectEnforcementTestTools0816
    {
        [MenuItem("Ascendance/Validation/Test 0.8.16 - Core Status Effect Enforcement (Play Mode)")]
        public static void TestStatusEffectEnforcement()
        {
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.16 TEST] Enter Play Mode before running the Core Status Effect Enforcement test.");
                return;
            }

            var gm = AscendanceGameManager.Instance;
            if (gm.IsResolving || gm.HasPendingReactionWindow || gm.TurnHero != null || gm.ParticipatingHeroes.Any(h => h.Moving))
            {
                Debug.LogWarning("[ASCENDANCE][0.8.16 TEST] Run this at the beginning of a Hero Phase with no turn, movement, or Reaction window in progress.");
                return;
            }

            var party = gm.ParticipatingHeroes.ToList();
            if (party.Count < 2)
            {
                Debug.LogWarning("[ASCENDANCE][0.8.16 TEST] Use a party of at least 2 Heroes.");
                return;
            }

            bool pass = true;
            void Check(bool condition, string label)
            {
                if (condition) Debug.Log("[ASCENDANCE][0.8.16 TEST] PASS — " + label);
                else { pass = false; Debug.LogError("[ASCENDANCE][0.8.16 TEST] FAIL — " + label); }
            }

            var source = party[0];
            var target = party[1];
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
            }
            foreach (var e in gm.Enemies.Values) { e.Statuses.Clear(); e.Health = e.MaxHealth; e.ActivatedThisRound = false; }

            // Rooted: blocks voluntary movement, while the condition itself remains persistent.
            gm.AddStatus(target, "Rooted", 1, 0, "0.8.16 Rooted test", false, source);
            Check(!gm.CanHeroVoluntarilyMove(target), "Rooted blocks voluntary Hero movement.");
            Check(target.HasStatus("Rooted"), "Rooted remains active until explicitly removed.");
            target.Statuses.RemoveAll(s => s.Name == "Rooted");
            Check(gm.CanHeroVoluntarilyMove(target), "Removing Rooted restores voluntary movement permission.");

            // Stunned: the Hero's next activation is consumed with 0 Actions, then the activation-timed status expires.
            gm.AddStatus(target, "Stunned", 1, 0, "0.8.16 Stunned test", false, source,
                "The Hero gains Stunned until the end of their next activation.");
            gm.SelectHero(target.Id);
            Check(gm.TryStartHeroTurn(target), "A Stunned Hero can enter the activation lifecycle so the skipped activation can be consumed.");
            Check(target.TurnCompleted && gm.TurnHero == null && target.Actions == 0,
                "Stunned immediately ends the Hero activation with 0 Actions.");
            Check(!target.HasStatus("Stunned"), "Stunned expires at the end of the skipped activation.");

            // Slowed: normal 3-Action activation becomes 2 Actions, minimum 1 by rule.
            target.TurnCompleted = false;
            gm.AddStatus(target, "Slowed", 1, 0, "0.8.16 Slowed test", false, source);
            gm.SelectHero(target.Id);
            Check(gm.TryStartHeroTurn(target), "Slowed Hero starts a normal activation.");
            Check(target.Actions == 2, "Slowed reduces the normal 3-Action grant to 2 Actions.");
            gm.EndHeroTurn();
            target.Statuses.RemoveAll(s => s.Name == "Slowed");

            // Core stat conditions.
            int heroAttack = target.Attack;
            int heroArmor = target.Armor;
            gm.AddStatus(target, "Inspired", 1, 0, "0.8.16 Inspired test", true, source);
            Check(gm.DebugEffectiveHeroAttack(target.Id) == heroAttack + 1, "Inspired grants +1 Hero Attack.");
            target.Statuses.RemoveAll(s => s.Name == "Inspired");
            gm.AddStatus(target, "Weakened", 1, 0, "0.8.16 Weakened test", false, source);
            Check(gm.DebugEffectiveHeroAttack(target.Id) == Mathf.Max(0, heroAttack - 1), "Weakened reduces Hero Attack by 1.");
            target.Statuses.RemoveAll(s => s.Name == "Weakened");
            gm.AddStatus(target, "Guarded", 1, 0, "0.8.16 Guarded test", true, source);
            Check(gm.DebugEffectiveHeroArmor(target.Id) == heroArmor + 1, "Guarded grants +1 Hero Armor.");
            target.Statuses.RemoveAll(s => s.Name == "Guarded");
            gm.AddStatus(target, "Vulnerable", 1, 0, "0.8.16 Vulnerable test", false, source);
            Check(gm.DebugEffectiveHeroArmor(target.Id) == Mathf.Max(0, heroArmor - 1), "Vulnerable reduces Hero Armor by 1, minimum 0.");
            target.Statuses.RemoveAll(s => s.Name == "Vulnerable");

            int enemyAttack = enemy.Attack;
            int enemyArmor = enemy.Armor;
            gm.AddStatus(enemy, "Weakened", 1, 0, "0.8.16 Enemy Weakened test", false, source);
            Check(gm.DebugEffectiveEnemyAttack(enemy.Id) == Mathf.Max(0, enemyAttack - 1), "Weakened reduces Enemy Attack by 1.");
            enemy.Statuses.RemoveAll(s => s.Name == "Weakened");
            gm.AddStatus(enemy, "Vulnerable", 1, 0, "0.8.16 Enemy Vulnerable test", false, source);
            Check(gm.DebugEffectiveEnemyArmor(enemy.Id) == Mathf.Max(0, enemyArmor - 1), "Vulnerable reduces Enemy Armor by 1, minimum 0.");
            enemy.Statuses.RemoveAll(s => s.Name == "Vulnerable");

            // Marked + Exposed: source-scoped Marked grants +1; Exposed grants the next +1 then removes itself.
            gm.AddStatus(enemy, "Marked", 1, 0, "0.8.16 Marked test", false, source,
                "The target gains Marked until the end of the source's next turn.");
            Check(gm.DebugMarkedAttackBonus(source.Id, enemy.Id) == 1, "Marked grants +1 Attack to the Hero who applied it.");
            Check(gm.DebugMarkedAttackBonus(target.Id, enemy.Id) == 0, "Source-scoped Marked does not grant its +1 Attack to a different Hero.");
            gm.AddStatus(enemy, "Exposed", 1, 0, "0.8.16 Exposed test", false, source);
            int consumed = gm.DebugConsumeTargetStatusAttackBonus(source.Id, enemy.Id);
            Check(consumed == 2, "Marked + Exposed contribute +2 total Attack to the triggering Attack.");
            Check(!enemy.Statuses.Any(s => s.Name == "Exposed"), "Exposed removes itself after the next Attack consumes it.");
            Check(enemy.Statuses.Any(s => s.Name == "Marked"), "Consuming Exposed does not remove Marked.");
            enemy.Statuses.Clear();

            // Stealth: authored as a state only. It must not silently alter Attack/Armor by itself.
            int stealthAttackBefore = gm.DebugEffectiveHeroAttack(source.Id);
            int stealthArmorBefore = gm.DebugEffectiveHeroArmor(source.Id);
            gm.AddStatus(source, "Stealth", 1, 0, "0.8.16 Stealth test", true, source);
            Check(gm.DebugEffectiveHeroAttack(source.Id) == stealthAttackBefore && gm.DebugEffectiveHeroArmor(source.Id) == stealthArmorBefore,
                "Stealth has no inherent Attack/Armor modifier; card text supplies Stealth benefits.");
            source.Statuses.RemoveAll(s => s.Name == "Stealth");

            // Regenerating: recover 1 Health at the start of the Hero's activation.
            source.TurnCompleted = false;
            source.Health = Mathf.Max(1, source.MaxHealth - 2);
            int beforeRegen = source.Health;
            gm.AddStatus(source, "Regenerating", 1, 0, "0.8.16 Regenerating test", true, source);
            gm.SelectHero(source.Id);
            Check(gm.TryStartHeroTurn(source), "Regenerating Hero starts an activation normally.");
            Check(source.Health == Mathf.Min(source.MaxHealth, beforeRegen + 1), "Regenerating recovers exactly 1 Health at activation start.");
            gm.EndHeroTurn();

            if (pass) Debug.Log("[ASCENDANCE][0.8.16 TEST] PASS — Core Status Effect Enforcement test completed successfully.");
            else Debug.LogError("[ASCENDANCE][0.8.16 TEST] FAIL — one or more Core Status Effect Enforcement checks failed.");
        }
    }
}
#endif
