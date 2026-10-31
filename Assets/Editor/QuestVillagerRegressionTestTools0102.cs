#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    // Uses the production Enemy activation, rescue, quest reward and save paths.
    // Every fixture is in the caller's required isolated validation directory.
    public static class QuestVillagerRegressionTestTools0102
    {
        private const string Prefix = "[ASCENDANCE][Q001 VILLAGERS]";

        [MenuItem("Ascendance/Validation/Test Q001 Villager Targeting (Play Mode)")]
        public static void TestVillagers()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            var gm = AscendanceGameManager.Instance;
            if (!Application.isPlaying || gm == null || gm.IsResolving || gm.HasPendingReactionWindow)
                throw new InvalidOperationException("Use the idle isolated Stonebrook validation fixture.");
            gm.StartCoroutine(Run(gm));
        }

        private static string Gameplay(AscendanceSaveData save)
        {
            var copy = AscendanceSaveSystem.CloneForAuthority0100(save);
            copy.SavedAtUtc = null;
            return JsonUtility.ToJson(copy);
        }

        // Flatten only game iterators: Unity yield instructions must still wait on
        // the Editor clock. Exceptions propagate to the gate and all iterators are
        // disposed on failure, rather than leaving a detached activation running.
        private static IEnumerator Activate(AscendanceGameManager gm, EnemyState enemy)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(gm.DebugRunSingleEnemyActivation0102(enemy.Id));
            float deadline = Time.realtimeSinceStartup + 15f;
            try
            {
                while (stack.Count > 0)
                {
                    if (Time.realtimeSinceStartup > deadline)
                        throw new TimeoutException(Prefix + " Enemy activation did not finish.");
                    if (gm.HasPendingReactionWindow) gm.ResolveReactionWindow();
                    var iterator = stack.Peek();
                    if (!iterator.MoveNext())
                    {
                        (iterator as IDisposable)?.Dispose();
                        stack.Pop();
                        continue;
                    }
                    var current = iterator.Current;
                    if (current is IEnumerator nested && !(current is CustomYieldInstruction)) stack.Push(nested);
                    else yield return current;
                }
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            }
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int checks = 0;
            void Check(bool ok, string label)
            {
                if (!ok) throw new InvalidOperationException(Prefix + " FAIL — " + label);
                checks++;
                Debug.Log(Prefix + " PASS — " + label);
            }
            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            var restoreEnemies = gm.Enemies.ToDictionary(p => p.Key,
                p => JsonUtility.FromJson<EnemyState>(JsonUtility.ToJson(p.Value)));
            // Connections are authored runtime graph data, not part of the save
            // DTO. Keep an independent copy for the unreachable-target fixture.
            var restoreConnections = gm.Locations.ToDictionary(p => p.Key, p => p.Value.Connections.ToArray());
            void RestoreConnections()
            {
                foreach (var pair in restoreConnections)
                    if (gm.Locations.TryGetValue(pair.Key, out var location))
                    {
                        location.Connections.Clear();
                        location.Connections.AddRange(pair.Value);
                    }
            }
            var fresh = AscendanceGameManager.CreateFreshOnlineCampaign0102(new[] { "Aldric", "Rowan" });
            void Reset()
            {
                RestoreConnections();
                gm.ApplyOnlineCampaignSlot0102(AscendanceSaveSystem.CloneForAuthority0100(fresh));
                gm.Enemies.Clear();
                foreach (var h in gm.ParticipatingHeroes)
                {
                    h.Hand.Clear(); h.Actions = 0; h.AttackOnlyActions = 0;
                    h.Statuses.Clear(); h.TurnCompleted = false;
                }
            }
            int HeroHealth() => gm.ParticipatingHeroes.Sum(h => h.Health);
            QuestObjectState0829[] Villagers() => gm.QuestObjects0829.Where(o => o.Type == "Villager").ToArray();
            EnemyState Spawn(string template = "E001", string location = "L000") => gm.DebugSpawnEnemy0859(template, location);
            void Rescue(int number)
            {
                gm.DebugSetHeroAt0829("Aldric", "L000", number);
                for (int i = 0; i < number; i++) gm.RescueVillager();
            }
            void Finish()
            {
                gm.DebugRevealQuestLocation0829("L006", "Aldric");
                string captain = gm.DebugQuestEnemyId0829("E006");
                if (captain == null) throw new InvalidOperationException(Prefix + " Captain fixture did not spawn.");
                gm.DebugDefeatQuestEnemy0829(captain, "Aldric");
            }
            LocationState StonebrookEntry()
            {
                // Use an existing Q001 edge, rather than fabricating a graph or
                // teleporting the Enemy directly onto a damage trigger.
                var origin = gm.Locations["L000"].Connections.Select(id => gm.Locations[id])
                    .Where(l => !string.IsNullOrWhiteSpace(l.AuthoredLocationId) && gm.IsLocationEligibleForCurrentQuest0829(l.Id)
                        && !gm.IsDifficultTerrain(l.Id))
                    .OrderBy(l => l.Id, StringComparer.OrdinalIgnoreCase).First();
                origin.Revealed = true; origin.RevealResolved = true;
                return origin;
            }
            try
            {
                Reset();
                Check(gm.VillagersAvailable == 4 && gm.VillagersLost == 0 && gm.VillagersRescued == 0 && gm.VillagersSurviving == 4,
                    "A genuine fresh campaign starts with four available Villagers and no losses or rescues.");
                Check(Villagers().Length == 4 && Villagers().All(v => v.LocationId == "L000") && Villagers().Select(v => v.ObjectId).Distinct().Count() == 4,
                    "Four distinct persistent objective tokens are placed in Stonebrook.");
                Check(Villagers().All(v => v.Health == 0 && v.MaxHealth == 0 && v.Armor == 0),
                    "Civilian tokens do not acquire invented combat Health or Armor.");
                var raider = Spawn(); int hp = HeroHealth();
                yield return Activate(gm, raider);
                Check(gm.VillagersLost == 1 && gm.VillagersAvailable == 3 && gm.VillagersSurviving == 3,
                    "One real Goblin activation removes exactly one available Villager.");
                Check(HeroHealth() == hp, "A priority Villager attack deals no simultaneous Hero damage.");
                Check(raider.ActivatedThisRound && raider.LocationId == "L000" && raider.MovementStepsThisActivation0859 == 0,
                    "The priority attack consumes the activation and keeps the Goblin in Stonebrook.");
                Check(!gm.HasPendingReactionWindow && !gm.IsResolving,
                    "A Villager attack finishes without leaving a Hero reaction or resolving lock.");
                yield return Activate(gm, raider);
                Check(gm.VillagersLost == 1 && HeroHealth() == hp,
                    "Re-entering an already completed activation cannot attack a second Villager or Hero.");

                Reset(); raider = Spawn(); int raiderHealth = raider.Health;
                gm.AddStatus(raider, "Bleeding", 0, 0, "Villager regression", false, null, "until removed");
                yield return Activate(gm, raider);
                Check(gm.VillagersLost == 1 && raider.Health == raiderHealth - 1,
                    "A Villager attack counts as an attack for the real Bleeding end-activation consequence.");

                Reset();
                foreach (var hero in gm.ParticipatingHeroes) hero.LocationId = "L001";
                raider = Spawn(); yield return Activate(gm, raider);
                Check(gm.VillagersLost == 1 && raider.LocationId == "L000" && raider.MovementStepsThisActivation0859 == 0,
                    "A Goblin already beside Villagers does not walk away toward remote Heroes.");

                Reset(); raider = Spawn();
                gm.AddStatus(raider, "Taunted", 0, 0, "Villager regression", false, gm.Heroes["Aldric"], "until removed");
                hp = HeroHealth(); yield return Activate(gm, raider);
                Check(gm.VillagersLost == 1 && HeroHealth() == hp,
                    "The Q001 quest priority takes precedence over ordinary Hero Taunt targeting.");

                foreach (string template in new[] { "E002", "E005" })
                {
                    Reset(); var goblin = Spawn(template); hp = HeroHealth();
                    yield return Activate(gm, goblin);
                    Check(gm.VillagersLost == 1 && HeroHealth() == hp && goblin.ActivatedThisRound,
                        template + " uses Villager priority before ranged Hero targeting or replacement abilities.");
                }

                foreach (string condition in new[] { "Stunned", "Frozen", "Knocked Down" })
                {
                    Reset(); raider = Spawn(); hp = HeroHealth();
                    gm.AddStatus(raider, condition, 0, 0, "Villager regression", false, null, "until removed");
                    yield return Activate(gm, raider);
                    Check(gm.VillagersLost == 0 && gm.VillagersAvailable == 4 && HeroHealth() == hp && raider.ActivatedThisRound,
                        condition + " prevents both the Villager attack and Hero attack while completing the activation.");
                }

                Reset(); raider = Spawn(); raider.Health = 0; hp = HeroHealth();
                yield return Activate(gm, raider);
                Check(gm.VillagersLost == 0 && HeroHealth() == hp,
                    "A defeated Goblin cannot attack a Villager.");
                Reset(); raider = Spawn(); raider.Health = 1;
                gm.AddStatus(raider, "Burning", 0, 0, "Meteor Shard", false, null, "until removed");
                yield return Activate(gm, raider);
                Check(raider.Defeated && gm.VillagersLost == 0,
                    "A Goblin killed by its real start-activation condition does not attack a Villager.");

                Reset(); var entry = StonebrookEntry();
                bool grantedCaltrops = gm.DebugGiveLoot0854("Aldric", "LT021");
                bool placedCaltrops = gm.DebugUseLoot0854("Aldric", "LT021");
                Check(grantedCaltrops && placedCaltrops && !gm.Heroes["Aldric"].LootInventory.Contains("LT021")
                    && gm.HeroCreatedPieces.Count(p => p.Type == "Caltrops" && p.LocationId == "L000") == 1,
                    "The actual Caltrops consumable is consumed and places one live trap beside the Heroes.");
                raider = Spawn("E001", entry.AuthoredLocationId); raider.Health = 1; raider.Attack = 20;
                hp = HeroHealth(); yield return Activate(gm, raider);
                Check(raider.Defeated && raider.LocationId == "L000" && raider.MovementStepsThisActivation0859 == 1
                    && !gm.HeroCreatedPieces.Any(p => p.Type == "Caltrops"),
                    "A one-Health Goblin actually moves along the authored edge, triggers Caltrops, dies and consumes the trap.");
                Check(HeroHealth() == hp && gm.VillagersLost == 0 && raider.ActivatedThisRound
                    && !gm.HasPendingReactionWindow && !gm.IsResolving,
                    "A Caltrops movement kill finishes the activation without dead-Enemy Hero damage, a reaction lock or a Villager loss.");

                Reset();
                gm.ApplyOnlineCampaignSlot0102(AscendanceGameManager.CreateFreshOnlineCampaign0102(new[] { "Mira", "Rowan" }));
                gm.Enemies.Clear();
                foreach (var hero in gm.ParticipatingHeroes) { hero.Hand.Clear(); hero.Actions = 0; }
                entry = StonebrookEntry(); gm.Heroes["Mira"].LocationId = entry.Id;
                bool placedMechanicalTrap = gm.DebugResolveHeroCardSpecial0860("Mira", "H010-010");
                Check(placedMechanicalTrap && gm.Locations["L000"].TemporaryTags.Count(t => t.Tag == "Mira Trap" && t.SourceHeroId == "Mira") == 1,
                    "Mira's real Mechanical Trap effect places its token in the connected revealed Stonebrook Location.");
                gm.Heroes["Mira"].LocationId = "L000";
                raider = Spawn("E001", entry.AuthoredLocationId); raider.Health = 1; raider.Attack = 20;
                hp = HeroHealth(); yield return Activate(gm, raider);
                Check(raider.Defeated && raider.LocationId == "L000" && raider.MovementStepsThisActivation0859 == 1
                    && gm.Locations["L000"].TemporaryTags.All(t => t.Tag != "Mira Trap"),
                    "A Goblin actually enters Mira's Mechanical Trap, takes lethal entry damage and removes the token.");
                Check(HeroHealth() == hp && gm.VillagersLost == 0 && raider.ActivatedThisRound
                    && !gm.HasPendingReactionWindow && !gm.IsResolving,
                    "A Mechanical Trap movement kill cannot continue into a Hero or Villager attack or leave a pending reaction.");

                // A deliberately high fixture Attack makes damage deterministic
                // for every d6 while still executing real targeting, Armor and
                // damage resolution. Each case has a separate fresh party.
                Reset(); var wolf = Spawn("E020"); wolf.Attack = 20;
                hp = HeroHealth(); yield return Activate(gm, wolf);
                Check(HeroHealth() < hp && wolf.ActivatedThisRound,
                    "A non-Goblin still resolves an actual normal Hero attack in Stonebrook.");
                Check(gm.VillagersLost == 0 && gm.VillagersAvailable == 4,
                    "Non-Goblin attacks leave all Q001 Villagers untouched.");
                Reset(); var remote = gm.DebugLocationByAuthored0858("L001"); remote.Revealed = true;
                foreach (var hero in gm.ParticipatingHeroes) hero.LocationId = remote.Id;
                raider = Spawn("E001", "L001"); raider.Attack = 20;
                hp = HeroHealth(); yield return Activate(gm, raider);
                Check(HeroHealth() < hp && gm.VillagersLost == 0 && raider.LocationId == remote.Id,
                    "A Goblin outside Stonebrook falls back to a local Hero instead of attacking remote Villagers.");
                Reset(); remote = gm.DebugLocationByAuthored0858("L001"); remote.Revealed = true;
                foreach (var hero in gm.ParticipatingHeroes) hero.LocationId = remote.Id;
                var archer = Spawn("E002", "L001"); archer.Attack = 20;
                hp = HeroHealth(); yield return Activate(gm, archer);
                Check(HeroHealth() < hp && gm.VillagersLost == 0,
                    "A ranged Goblin outside Stonebrook also preserves the website's same-location Villager rule.");
                Reset(); raider = Spawn("E001", "L001");
                foreach (var location in gm.Locations.Values) location.Connections.Clear();
                hp = HeroHealth(); yield return Activate(gm, raider);
                Check(HeroHealth() == hp && gm.VillagersLost == 0 && raider.ActivatedThisRound,
                    "An unreachable Goblin with no legal Hero target cannot attack out-of-range Villagers.");

                Reset(); Rescue(1);
                Check(gm.VillagersRescued == 1 && gm.VillagersAvailable == 3 && gm.VillagersLost == 0 && gm.Heroes["Aldric"].Actions == 0,
                    "The real rescue action marks one available token rescued and spends one Action.");
                raider = Spawn(); hp = HeroHealth(); yield return Activate(gm, raider);
                Check(gm.VillagersRescued == 1 && gm.VillagersLost == 1 && gm.VillagersAvailable == 2 && HeroHealth() == hp,
                    "A Goblin attacks an unrescued token and preserves the already rescued Villager.");
                Rescue(3);
                Check(gm.VillagersRescued == 3 && gm.VillagersLost == 1 && gm.VillagersAvailable == 0 && gm.VillagersSurviving == 3,
                    "Rescue cannot recover the lost Villager or increase total rescues beyond surviving tokens.");
                Check(gm.Heroes["Aldric"].Actions == 1,
                    "Trying to rescue after the last available token does not spend another Action.");
                Reset(); Rescue(4); raider = Spawn(); raider.Attack = 20;
                hp = HeroHealth(); yield return Activate(gm, raider);
                Check(HeroHealth() < hp && raider.ActivatedThisRound,
                    "After all four Villagers are rescued a Goblin resumes actual normal Hero attacks.");
                Check(gm.VillagersRescued == 4 && gm.VillagersAvailable == 0 && gm.VillagersLost == 0 && gm.VillagersSurviving == 4,
                    "Fallback Hero attacks cannot damage rescued tokens or invent additional losses.");

                Reset();
                for (int i = 0; i < 2; i++) { raider = Spawn(); yield return Activate(gm, raider); }
                Check(!gm.QuestFailed && gm.VillagersLost == 2 && gm.VillagersAvailable == 2,
                    "Two losses leave the quest active while the required two rescues remain possible.");
                raider = Spawn(); yield return Activate(gm, raider);
                Check(gm.QuestFailed && !gm.QuestComplete && gm.VillagersLost == 3 && gm.VillagersAvailable == 1,
                    "The third actual Villager loss immediately fails Q001.");
                Check(gm.PartyGold == 0 && gm.QuestRuntime0829.RewardResolved == false,
                    "Villager-loss failure awards no victory Gold or rewards.");
                raider = Spawn(); hp = HeroHealth(); yield return Activate(gm, raider);
                Check(gm.VillagersLost == 3 && HeroHealth() == hp && !raider.ActivatedThisRound,
                    "Further enemy activations stop after quest failure without consuming the last token.");

                Reset(); Rescue(1); raider = Spawn(); yield return Activate(gm, raider);
                var mixed = AscendanceSaveSystem.CaptureForAuthority0100(gm);
                string tokens = JsonUtility.ToJson(mixed.QuestRuntime0829);
                gm.ApplyOnlineCampaignSlot0102(AscendanceSaveSystem.CloneForAuthority0100(mixed));
                Check(gm.VillagersRescued == 1 && gm.VillagersLost == 1 && gm.VillagersAvailable == 2 && gm.VillagersSurviving == 3,
                    "Authority capture and load preserve mixed rescued, lost and available token counts.");
                Check(JsonUtility.ToJson(AscendanceSaveSystem.CaptureForAuthority0100(gm).QuestRuntime0829) == tokens,
                    "The exact persistent quest token identities and flags survive a save/load round trip.");
                hp = HeroHealth(); yield return Activate(gm, gm.Enemies[raider.Id]);
                Check(gm.VillagersLost == 1 && HeroHealth() == hp,
                    "The saved activation-complete flag prevents a duplicate attack after reconnect/load.");

                Reset(); var legacy = AscendanceSaveSystem.CaptureForAuthority0100(gm);
                legacy.VillagersRescued = 1; legacy.QuestRuntime0829.Objects.RemoveAll(v => v.Type == "Villager");
                gm.ApplyOnlineCampaignSlot0102(legacy);
                string before = Gameplay(AscendanceSaveSystem.CaptureForAuthority0100(gm));
                var display = Villagers(); int lost = gm.VillagersLost, available = gm.VillagersAvailable, surviving = gm.VillagersSurviving;
                var secondDisplay = Villagers();
                Check(display.Length == 4 && secondDisplay.Length == 4 && display.Count(v => v.Rescued) == 1
                    && lost == 0 && available == 3 && surviving == 4,
                    "A legacy no-token save presents four objective tokens while preserving its existing rescue count.");
                Check(Gameplay(AscendanceSaveSystem.CaptureForAuthority0100(gm)) == before
                    && gm.QuestRuntime0829.Objects.All(v => v.Type != "Villager"),
                    "Repeated legacy token/count presentation leaves every canonical logical save field unchanged.");
                Rescue(1);
                Check(gm.QuestRuntime0829.Objects.Count(v => v.Type == "Villager") == 4
                    && gm.VillagersRescued == 2 && gm.VillagersAvailable == 2 && gm.VillagersLost == 0,
                    "The first actual rescue materializes legacy tokens exactly once with the correct prior rescue state.");
                var migrated = AscendanceSaveSystem.CaptureForAuthority0100(gm);
                gm.ApplyOnlineCampaignSlot0102(migrated);
                Check(Villagers().Length == 4 && gm.VillagersRescued == 2 && gm.VillagersAvailable == 2,
                    "Materialized legacy tokens remain stable through the next save/load.");
                gm.DebugStartQuest0829("Q001");
                Check(Villagers().Length == 4 && gm.VillagersRescued == 0 && gm.VillagersLost == 0
                    && gm.VillagersAvailable == 4 && !gm.QuestFailed && !gm.QuestComplete,
                    "Explicitly restarting Q001 resets rescued/lost flags and creates four fresh tokens.");
                string progressed = Gameplay(AscendanceSaveSystem.CaptureForAuthority0100(gm));
                var independent = AscendanceGameManager.CreateFreshOnlineCampaign0102(new[] { "Garruk" });
                Check(independent.VillagersRescued == 0 && independent.QuestRuntime0829.Objects.Count(v => v.Type == "Villager" && !v.Rescued && !v.Destroyed) == 4
                    && Gameplay(AscendanceSaveSystem.CaptureForAuthority0100(gm)) == progressed,
                    "A separate fresh campaign creates new Villagers without changing the current campaign.");

                Reset(); Rescue(2); Finish();
                Check(gm.QuestComplete && !gm.QuestFailed && gm.PartyGold == 75,
                    "Website parity: Captain defeated plus two rescues gives 75 Gold, even with two unrescued survivors.");
                Check(gm.VillagersSurviving == 4 && gm.VillagersRescued == 2 && gm.VillagersAvailable == 2,
                    "Completion preserves all four survivors without converting unrescued tokens into rescues.");
                gm.DebugEvaluateQuestOutcome0829();
                Check(gm.PartyGold == 75, "Re-evaluating a completed quest cannot award its reward twice.");
                Reset(); Rescue(4); Finish();
                Check(gm.QuestComplete && gm.PartyGold == 90,
                    "Website parity: rescuing all four and defeating the Captain gives the additional 15 Gold.");
                Reset(); raider = Spawn(); yield return Activate(gm, raider); Rescue(2); Finish();
                Check(gm.QuestComplete && !gm.QuestFailed && gm.VillagersLost == 1 && gm.PartyGold == 75,
                    "A legal victory with one Villager lost gives only the base 75 Gold.");
            }
            finally
            {
                RestoreConnections();
                gm.ApplyOnlineCampaignSlot0102(restore, restoreEnemies);
            }
            Debug.Log(Prefix + $" PASS — Villager gameplay regression completed: {checks} passed, 0 failed.");
        }
    }
}
#endif
