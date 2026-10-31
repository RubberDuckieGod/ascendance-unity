#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendance.EditorTools
{
    /// <summary>Actual rules, TCP sessions and the same reaction request used by the HUD.</summary>
    public static class ReactionAuthorityRegressionTestTools0102
    {
        private const string Prefix = "[ASCENDANCE][REACTION AUTHORITY]";

        [MenuItem("Ascendance/Validation/Test Reaction Authority + Multiplayer Enemy Phase (Play Mode)")]
        public static void TestReactions()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
                throw new InvalidOperationException("Open the isolated Stonebrook Play Mode fixture first.");
            AscendanceGameManager.Instance.StartCoroutine(Run(AscendanceGameManager.Instance));
        }

        private sealed class Replica
        {
            public GameObject Object;
            public AscendanceGameManager Game;
            public AscendanceClientCommandRouter0101 Router;
        }

        private static Replica MakeReplica(AscendanceTcpCommandTransport0102 transport)
        {
            // An inactive, manually pumped replica does not replace scene singletons,
            // animate authority avatars, or execute a local rules coroutine.
            var root = new GameObject("ASCENDANCE_REACTION_REPLICA");
            root.SetActive(false);
            var game = root.AddComponent<AscendanceGameManager>();
            game.Initialize();
            var router = root.AddComponent<AscendanceClientCommandRouter0101>();
            router.Initialize(game, null);
            router.SetTransport(transport);
            return new Replica { Object = root, Game = game, Router = router };
        }

        private static void Pump(IEnumerable<Replica> replicas)
        {
            foreach (var replica in replicas) replica.Router.PumpRemoteSnapshots0102();
        }

        private static IEnumerator Await(Task task, IEnumerable<Replica> replicas)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
            { Pump(replicas); yield return null; }
            if (!task.IsCompleted) throw new TimeoutException("Reaction socket request did not complete.");
            task.GetAwaiter().GetResult();
            Pump(replicas);
        }

        private static IEnumerator WaitFor(Func<bool> condition, IEnumerable<Replica> replicas, string reason)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            { Pump(replicas); yield return null; }
            Pump(replicas);
            if (!condition()) throw new TimeoutException(reason);
        }

        private static IEnumerator Request(Replica replica, Func<bool> request,
            IEnumerable<Replica> replicas, Action<ServerCommandResult0100> completed)
        {
            ServerCommandResult0100 result = null;
            void Finished(ServerCommandResult0100 value) { result = value; }
            replica.Router.CommandCompleted += Finished;
            try
            {
                request(); // Remote HUD requests return COMMAND_PENDING before their real reply.
                yield return WaitFor(() => result != null, replicas, "HUD request received no authoritative reply.");
                completed(result);
            }
            finally { replica.Router.CommandCompleted -= Finished; }
        }

        private static ServerCommandEnvelope0100 Command(ServerCommandType0100 type,
            string heroId, string playerId, ServerStateSnapshot0100 snapshot) => new()
        {
            CommandId = Guid.NewGuid().ToString("N"), RoomId = snapshot.RoomId,
            PlayerId = playerId, ActorHeroId = heroId, Type = type,
            ExpectedRevision = snapshot.Revision, HasExpectedGameplayRevision = true,
            ExpectedGameplayRevision = snapshot.GameplayRevision,
            ExpectedServerInstanceId = snapshot.ServerInstanceId
        };

        private static bool RemoteHudOffersDecline(Replica replica, AscendanceHUD hud)
        {
            // Refresh the actual HUD against the already installed real TCP snapshot.
            // Restore both singleton references synchronously, before any Unity frame,
            // callback or authoritative gameplay command can run.
            const BindingFlags statics = BindingFlags.Static | BindingFlags.NonPublic;
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var managerInstance = typeof(AscendanceGameManager).GetField("<Instance>k__BackingField", statics);
            var routerInstance = typeof(AscendanceClientCommandRouter0101).GetField("<Instance>k__BackingField", statics);
            object originalManager = managerInstance.GetValue(null), originalRouter = routerInstance.GetValue(null);
            var refresh = typeof(AscendanceHUD).GetMethod("Refresh", fields);
            try
            {
                managerInstance.SetValue(null, replica.Game); routerInstance.SetValue(null, replica.Router);
                refresh.Invoke(hud, null);
                var panel = (GameObject)typeof(AscendanceHUD).GetField("reactionPanel", fields).GetValue(hud);
                var button = (Button)typeof(AscendanceHUD).GetField("resolveReactionButton", fields).GetValue(hud);
                return panel.activeSelf && button.interactable;
            }
            finally
            {
                managerInstance.SetValue(null, originalManager); routerInstance.SetValue(null, originalRouter);
                refresh.Invoke(hud, null);
            }
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int checks = 0, port = 0;
            void Check(bool ok, string label)
            {
                if (!ok) throw new InvalidOperationException(Prefix + " FAIL — " + label);
                checks++; Debug.Log(Prefix + " PASS — " + label);
            }
            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            var sceneRouter = AscendanceClientCommandRouter0101.Instance;
            var replicas = new List<Replica>();
            var transports = new List<AscendanceTcpCommandTransport0102>();
            var prefs = new Dictionary<string, string>();
            string room = "REACTION-EDITOR-" + Guid.NewGuid().ToString("N");
            GameObject listener = null;
            AscendanceTcpServer0102 server = null;
            AscendanceRoomAuthority0100 authority = null;
            string Key(string hero) => $"ASCENDANCE_0102_RECONNECT_127.0.0.1_{port}_{room}_{hero}";
            try
            {
                yield return WaitFor(() => !gm.IsResolving && !gm.HasPendingReactionWindow
                    && !gm.ParticipatingHeroes.Any(h => h.Moving), replicas, "Inherited gameplay did not settle.");
                gm.Initialize();
                gm.SetParty(new[] { "Aldric", "Rowan" }, "Aldric");
                gm.DebugResetCampaignProgress0833();
                gm.DebugRemoveEnemies0859();
                // DebugRemoveEnemies0859 only clears its own DBG_ fixtures. Remove the
                // real Q001 opening force explicitly before installing this isolated foe.
                gm.Enemies.Clear();
                // A non-Goblin tests Hero attacks independently of Q001's villager priority.
                var wolf = gm.DebugSpawnEnemy0859("E020", "L000", "REACTION_WOLF");
                if (wolf != null)
                {
                    // Production snapshots recreate Quest/Encounter-owned enemies. Give this
                    // fixture the same ownership metadata instead of relying on Editor-only IDs.
                    wolf.QuestSpawned = true; wolf.QuestId = "Q001";
                    gm.QuestRuntime0829.QuestOwnedEnemyIds.Clear();
                    gm.QuestRuntime0829.QuestOwnedEnemyIds.Add(wolf.Id);
                }
                foreach (var hero in gm.ParticipatingHeroes)
                {
                    hero.Hand.Clear(); hero.Deck.Clear(); hero.Discard.Clear();
                    hero.Statuses.Clear(); hero.Health = hero.MaxHealth;
                    hero.Actions = 0; hero.TurnCompleted = false;
                }
                Check(wolf != null && gm.DebugChooseEnemyAttackTarget(wolf.Id) == "Rowan",
                    "An authored Wolf has a legal, vulnerable Hero target before activation.");
                authority = new AscendanceRoomAuthority0100(gm, room, false);
                listener = new GameObject("ASCENDANCE_REACTION_SOCKET");
                server = listener.AddComponent<AscendanceTcpServer0102>();
                Check(server.Initialize(authority, 0) && server.BoundPort > 0,
                    "The reaction fixture opens an isolated ephemeral TCP listener.");
                port = server.BoundPort;
                foreach (string hero in new[] { "Aldric", "Rowan" })
                {
                    prefs[Key(hero)] = PlayerPrefs.HasKey(Key(hero)) ? PlayerPrefs.GetString(Key(hero)) : null;
                    PlayerPrefs.DeleteKey(Key(hero));
                    var transport = new AscendanceTcpCommandTransport0102("127.0.0.1", port, room, "Reaction " + hero);
                    transports.Add(transport);
                    var connect = transport.ConnectAsync(); yield return Await(connect, replicas);
                    Check(connect.Result, hero + " connects through the production TCP transport.");
                    var claim = transport.ClaimHeroAsync(hero); yield return Await(claim, replicas);
                    Check(claim.Result.Accepted && transport.HeroId == hero, hero + " owns a distinct authenticated session.");
                    replicas.Add(MakeReplica(transport));
                }
                var a = replicas[0]; var b = replicas[1];
                Check(AscendanceGameManager.Instance == gm && AscendanceClientCommandRouter0101.Instance == sceneRouter
                    && replicas.All(r => r.Router.IsReady && r.Router.HasAuthoritativeSnapshot),
                    "Independent live replicas leave the authority and scene singletons intact.");
                var authoritativeEnemies = authority.Snapshot().Campaign.Enemies;
                string fixtureMetadata = "authority=" + string.Join(",", authoritativeEnemies.Select(e => e.Id + ":quest=" + e.QuestSpawned))
                    + "; replicas=" + string.Join(" / ", replicas.Select(r => string.Join(",", r.Game.Enemies.Values.Select(e => e.Id + ":quest=" + e.QuestSpawned))));
                Check(authoritativeEnemies.Count == 1 && authoritativeEnemies[0].Id == wolf.Id
                    && replicas.All(r => r.Game.Enemies.Count == 1 && r.Game.Enemies.ContainsKey(wolf.Id)),
                    "The authoritative campaign and both joined replicas contain exactly the intended Wolf fixture (" + fixtureMetadata + ").");

                for (int round = 1; round <= 2; round++)
                {
                    yield return WaitFor(() => replicas.All(r => r.Game.Round == round
                        && !r.Game.IsResolving && !r.Game.HasPendingReactionWindow), replicas,
                        "Peers did not observe the next Hero Phase.");
                    foreach (var replica in replicas)
                    {
                        replica.Game.SelectHero(replica == a ? "Rowan" : "Aldric");
                        ServerCommandResult0100 result = null;
                        yield return Request(replica, replica.Router.RequestStartOrEndTurn, replicas, value => result = value);
                        Check(result.Accepted && gm.TurnHeroId == (replica == a ? "Aldric" : "Rowan"),
                            $"Round {round}: the HUD start request activates its owned Hero while another portrait is viewed.");
                        yield return Request(replica, replica.Router.RequestStartOrEndTurn, replicas, value => result = value);
                        Check(result.Accepted, $"Round {round}: the owned Hero's HUD End Turn is accepted.");
                        long endedRevision = result.Snapshot.GameplayRevision;
                        yield return WaitFor(() => replicas.All(r => string.IsNullOrEmpty(r.Game.TurnHeroId)
                            && r.Router.LastSnapshot.GameplayRevision >= endedRevision), replicas,
                            "The next player's replica did not observe the completed Hero turn.");
                    }
                    yield return WaitFor(() => gm.HasPendingReactionWindow && !gm.PendingReactionResolutionQueued,
                        replicas, "All finished Heroes did not open the real Enemy Phase reaction window.");
                    Check(gm.PendingReactionKind == ReactionWindowKind.BeforeEnemyAttack && string.IsNullOrEmpty(gm.TurnHeroId)
                        && gm.ParticipatingHeroes.All(h => h.TurnCompleted),
                        $"Round {round}: the actual Enemy Phase pauses after every Hero completes their turn.");
                    int healthBefore = gm.ParticipatingHeroes.Sum(h => h.Health);

                    if (round == 1)
                    {
                        // No frame elapses between these commands: verify the immediate queued
                        // mutation, idempotency and fresh no-op rejection separately from animation.
                        var command = Command(ServerCommandType0100.ResolveReaction, "Aldric", transports[0].PlayerId, authority.Snapshot());
                        long before = authority.Room.GameplayRevision;
                        var resolved = authority.Execute(command);
                        Check(resolved.Accepted && resolved.Code == "ACCEPTED" && gm.PendingReactionResolutionQueued
                            && authority.Room.GameplayRevision == before + 1,
                            "A legal pre-attack decline immediately commits its queued gameplay transition.");
                        long queuedRevision = authority.Room.GameplayRevision;
                        var duplicate = authority.Execute(command);
                        Check(duplicate.Accepted && duplicate.Duplicate && duplicate.Code == "DUPLICATE"
                            && authority.Room.GameplayRevision == queuedRevision && gm.ParticipatingHeroes.Sum(h => h.Health) == healthBefore,
                            "Replaying the accepted decline ID returns its outcome without repeating damage or mutation.");
                        var repeated = authority.Execute(Command(ServerCommandType0100.ResolveReaction,
                            "Aldric", transports[0].PlayerId, authority.Snapshot()));
                        Check(!repeated.Accepted && repeated.Code == "RULE_REJECTED" && authority.Room.GameplayRevision == queuedRevision,
                            "A new decline after resolution is already queued remains an illegal no-op.");
                        var option = gm.PendingReactionOptions.First(o => o.HeroId == "Aldric");
                        var lateReaction = Command(ServerCommandType0100.UseReaction, "Aldric", transports[0].PlayerId, authority.Snapshot());
                        lateReaction.CardId = option.CardId;
                        var late = authority.Execute(lateReaction);
                        Check(!late.Accepted && late.Code == "RULE_REJECTED" && !gm.Heroes["Aldric"].UsedRoundAbilities.Contains("ShieldWall"),
                            "A stale reaction option cannot be used after the trigger has been declined.");
                    }
                    else
                    {
                        yield return WaitFor(() => a.Game.HasPendingReactionWindow, replicas,
                            "The real client did not receive the reaction prompt.");
                        Check(!a.Game.PendingReactionResolutionQueued,
                            "Installing a real remote reaction snapshot does not mistake the replica's cleared private flag for a queued response.");
                        Check(RemoteHudOffersDecline(a, gm.GetComponent<AscendanceHUD>()),
                            "The production HUD keeps Resolve / Decline enabled for a real unresolved remote prompt.");
                        a.Game.SelectHero("Rowan");
                        ServerCommandResult0100 result = null;
                        yield return Request(a, a.Router.RequestResolveReaction, replicas, value => result = value);
                        Check(result.Accepted && result.Code == "ACCEPTED" && a.Game.SelectedHeroId == "Rowan"
                            && transports[0].HeroId == "Aldric",
                            "The production HUD decline wrapper uses Aldric's session while Rowan's portrait stays selected.");
                    }

                    // Fail if a future card introduces another legal window that the fixture
                    // did not handle; do not call GM.ResolveReactionWindow behind the network.
                    yield return WaitFor(() => gm.Round == round + 1 && !gm.IsResolving
                        && !gm.HasPendingReactionWindow, replicas, "Enemy attacks did not reach a stable next Hero Phase.");
                    Check(gm.ParticipatingHeroes.Sum(h => h.Health) < healthBefore
                        && gm.Log.Any(l => l.StartsWith("ENEMY ATTACK RESOLVED") && l.Contains(wolf.DisplayName)),
                        $"Round {round}: a real authored Enemy Attack reduces actual Hero HP.");
                    yield return WaitFor(() => replicas.All(r => r.Game.Round == gm.Round
                        && !r.Game.HasPendingReactionWindow && !r.Game.IsResolving
                        && r.Game.Heroes["Aldric"].Health == gm.Heroes["Aldric"].Health
                        && r.Game.Heroes["Rowan"].Health == gm.Heroes["Rowan"].Health), replicas,
                        "TCP peers did not converge after Enemy Phase damage.");
                    Check(replicas.All(r => r.Game.ParticipatingHeroes.All(h => h.Actions == 0 && !h.TurnCompleted)),
                        $"Round {round}: both connected replicas see expired held Actions and the next round's unfinished turns.");
                }
                long unchangedRevision = authority.Room.GameplayRevision;
                var noWindow = authority.Execute(Command(ServerCommandType0100.ResolveReaction,
                    "Aldric", transports[0].PlayerId, authority.Snapshot()));
                Check(!noWindow.Accepted && noWindow.Code == "RULE_REJECTED" && authority.Room.GameplayRevision == unchangedRevision,
                    "Declining outside a reaction window is still rejected without a gameplay mutation.");
            }
            finally
            {
                server?.StopServer();
                foreach (var replica in replicas)
                {
                    replica.Router.SetTransport(null);
                    UnityEngine.Object.DestroyImmediate(replica.Object);
                }
                foreach (var transport in transports) transport.Dispose();
                authority?.Dispose();
                if (listener != null) UnityEngine.Object.DestroyImmediate(listener);
                // A failed gate must not leave its World Phase coroutine alive while restoring
                // the surrounding validation fixture. Successful rounds have already settled.
                if (gm.IsResolving || gm.HasPendingReactionWindow) gm.StopAllCoroutines();
                gm.ApplySaveData(restore);
                foreach (var pref in prefs)
                    if (pref.Value == null) PlayerPrefs.DeleteKey(pref.Key); else PlayerPrefs.SetString(pref.Key, pref.Value);
                PlayerPrefs.Save();
            }
            Debug.Log($"{Prefix} PASS — Reaction Authority completed: {checks} passed, 0 failed.");
        }
    }
}
#endif
