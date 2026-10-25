#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ascendance.Online;
using UnityEngine;

namespace Ascendance.Diagnostics
{
    // Opted-in development-player test driver. Shipping builds contain no driver.
    // Every gameplay request goes through the same router and TCP transport as the HUD.
    public sealed class NativeClientAcceptance0102 : MonoBehaviour
    {
        [Serializable] public sealed class Request
        {
            public string Id, Operation, Type, ActorHeroId, TargetId, SecondaryTargetId, CardId, OptionKey, StringValue;
            public int IntValue;
            public bool BoolValue;
        }
        [Serializable] public sealed class HeroView
        {
            public string Id, LocationId, PendingLocationId;
            public int Health, MaxHealth, Actions, AttackOnlyActions, Potions;
            public bool Moving, TurnCompleted, Downed, Dead;
            public string[] Hand, Discard;
        }
        [Serializable] public sealed class LocationView
        {
            public string Id;
            public bool Revealed;
            public string[] Connections;
        }
        [Serializable] public sealed class EnemyView
        {
            public string Id, LocationId;
            public int Health, Armor;
        }
        [Serializable] public sealed class State
        {
            public string HeroId, PlayerId, SelectedHeroId, TurnHeroId, ServerInstanceId, RoomId, Build, PersistentDataPath, ConnectionError;
            public long Revision, GameplayRevision, CommandsSent, CommandsAccepted;
            public int Frame, Round, Gold, AvatarCount;
            public bool Ready, Connected, Dedicated, HasHud, IsResolving, PendingReaction, PendingReward, PendingDeckControl, RejectedConnect, RunInBackground;
            public HeroView[] Heroes;
            public LocationView[] Locations;
            public EnemyView[] Enemies;
            public string[] Errors;
        }
        [Serializable] public sealed class Reply
        {
            public string Id, Code, Message;
            public bool Accepted;
            public State State;
        }

        private string directory, hero;
        private readonly List<string> errors = new();
        private float nextState, commandDeadline;
        private Request pending;
        private Action<ServerCommandResult0100> completion;
        private bool reconnecting, rejectionProbe, rejectedConnect;
        private string connectionError;

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            string output = Argument("-asc-native-output");
            if (string.IsNullOrWhiteSpace(output)) return;
            string config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string endpoint = Argument("-ascendance-connect");
            if (Environment.GetEnvironmentVariable("ASCENDANCE_NATIVE_ACCEPTANCE") != "1"
                || !Path.IsPathRooted(output) || string.IsNullOrWhiteSpace(config) || !Path.IsPathRooted(config)
                || Path.GetFullPath(config) != Path.Combine(Path.GetFullPath(output), "config")
                || !File.Exists(Path.Combine(output, ".ascendance-native-client"))
                || File.ReadAllText(Path.Combine(output, ".ascendance-native-client")).Trim() != "isolated-native-0.10.2c"
                || !AscendanceRemoteClientBootstrap0102.TryParseEndpoint(endpoint, out var host, out int port)
                || host != "127.0.0.1" || port < 1024
                || !(Argument("-room") ?? "").StartsWith("NATIVE-ACCEPTANCE-", StringComparison.Ordinal)
                || !Application.persistentDataPath.StartsWith(Path.GetFullPath(config) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Native acceptance requires an isolated, marked development-player directory and loopback test server.");
            var runner = new GameObject("ASCENDANCE_NATIVE_CLIENT_ACCEPTANCE").AddComponent<NativeClientAcceptance0102>();
            runner.directory = output;
            runner.hero = Argument("-asc-native-hero") ?? "Aldric";
            runner.rejectionProbe = Argument("-asc-native-reject-probe") == "1";
            DontDestroyOnLoad(runner.gameObject);
            Application.targetFrameRate = 30;
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += CaptureError;
            Directory.CreateDirectory(Path.Combine(directory, "requests"));
            Directory.CreateDirectory(Path.Combine(directory, "responses"));
            yield return null;
            AscendanceSession.StartNewGame(HeroDatabase.DefaultParty, hero);
            yield return ReadyAndJoin(null);
        }

        private IEnumerator ReadyAndJoin(Request request)
        {
            reconnecting = true;
            float deadline = Time.realtimeSinceStartup + 25f;
            if (rejectionProbe)
            {
                while (AscendanceClientCommandRouter0101.Instance?.Transport is not AscendanceTcpCommandTransport0102
                    && Time.realtimeSinceStartup < deadline) yield return null;
                if (AscendanceClientCommandRouter0101.Instance?.Transport is AscendanceTcpCommandTransport0102 probe)
                {
                    rejectedConnect = !probe.TryConnect(out connectionError);
                    if (!rejectedConnect) errors.Add("Negative native handshake probe unexpectedly connected.");
                }
                else errors.Add("Negative native handshake probe has no production TCP transport.");
                reconnecting = false; WriteState(); yield break;
            }
            while (AscendanceClientCommandRouter0101.Instance?.IsReady != true && Time.realtimeSinceStartup < deadline) yield return null;
            var router = AscendanceClientCommandRouter0101.Instance;
            if (router?.IsReady != true || router.Transport is not AscendanceTcpCommandTransport0102 remote)
            {
                errors.Add("Native client did not receive its initial authoritative snapshot.");
                if (request != null) Respond(request, false, "READY_TIMEOUT", errors.Last());
            }
            else
            {
                bool joined = remote.TryResolvePlayerForHero(hero, out _);
                if (!joined) errors.Add("Native client could not bind its own Hero session.");
                if (request != null) Respond(request, joined, joined ? "RECONNECTED" : "JOIN_FAILED", "Production transport Hero ownership result.");
            }
            reconnecting = false;
            WriteState();
        }

        private void CaptureError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(directory)) return;
            if (Time.realtimeSinceStartup >= nextState) { nextState = Time.realtimeSinceStartup + .15f; WriteState(); }
            if (pending != null && Time.realtimeSinceStartup > commandDeadline)
            {
                var failed = pending; ClearPending(); Respond(failed, false, "COMMAND_TIMEOUT", "The real router did not complete its command.");
            }
            if (pending != null || reconnecting) return;
            string path = Directory.GetFiles(Path.Combine(directory, "requests"), "*.json").OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            if (path == null) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); File.Delete(path); }
            catch (Exception ex) { errors.Add("Control request: " + ex.Message); return; }
            if (request == null || !Guid.TryParseExact(request.Id, "N", out _)) { errors.Add("Control request ID is missing."); return; }
            var router = AscendanceClientCommandRouter0101.Instance;
            if (request.Operation == "quit") { Respond(request, true, "QUIT", "Native player exit requested."); Application.Quit(0); return; }
            if (request.Operation == "state") { Respond(request, true, "STATE", "Observed production replica."); return; }
            if (request.Operation == "reconnect")
            {
                router.SetTransport(new AscendanceTcpCommandTransport0102(AscendanceRemoteClientBootstrap0102.Host,
                    AscendanceRemoteClientBootstrap0102.Port, AscendanceRemoteClientBootstrap0102.RoomId, AscendanceRemoteClientBootstrap0102.DisplayName));
                _ = ((AscendanceTcpCommandTransport0102)router.Transport).ConnectAsync();
                StartCoroutine(ReadyAndJoin(request)); return;
            }
            if (request.Operation != "send" || !Enum.TryParse(request.Type, out ServerCommandType0100 type))
            { Respond(request, false, "BAD_DRIVER_REQUEST", "A known production command is required."); return; }
            pending = request; commandDeadline = Time.realtimeSinceStartup + 20f;
            completion = result =>
            {
                var finished = pending; ClearPending();
                Respond(finished, result?.Accepted == true, result?.Code ?? "NO_RESULT", result?.Message);
            };
            router.CommandCompleted += completion;
            router.Send(type, request.ActorHeroId ?? hero, request.TargetId, request.SecondaryTargetId, request.CardId,
                request.OptionKey, request.StringValue, request.IntValue, request.BoolValue);
        }

        private void ClearPending()
        {
            if (completion != null && AscendanceClientCommandRouter0101.Instance != null)
                AscendanceClientCommandRouter0101.Instance.CommandCompleted -= completion;
            completion = null; pending = null;
        }

        private State Observe()
        {
            var gm = AscendanceGameManager.Instance; var router = AscendanceClientCommandRouter0101.Instance;
            var remote = router?.Transport as AscendanceTcpCommandTransport0102; var snapshot = router?.LastSnapshot;
            return new State
            {
                HeroId = hero, PlayerId = remote?.PlayerId, SelectedHeroId = gm?.SelectedHeroId, TurnHeroId = gm?.TurnHeroId,
                ServerInstanceId = snapshot?.ServerInstanceId, RoomId = snapshot?.RoomId, Build = AscendanceGameManager.BuildVersion,
                PersistentDataPath = Application.persistentDataPath, Frame = Time.frameCount,
                ConnectionError = connectionError, RejectedConnect = rejectedConnect,
                Revision = snapshot?.Revision ?? -1, GameplayRevision = snapshot?.GameplayRevision ?? -1,
                CommandsSent = router?.CommandsSent ?? 0, CommandsAccepted = router?.CommandsAccepted ?? 0,
                Round = gm?.Round ?? 0, Gold = gm?.PartyGold ?? 0, AvatarCount = gm?.HeroAvatars?.Count ?? 0,
                Ready = router?.IsReady == true, Connected = remote?.IsConnected == true,
                Dedicated = AscendanceDedicatedServerBootstrap0100.IsDedicatedServerProcess,
                HasHud = gm != null && gm.GetComponent<AscendanceHUD>() != null,
                RunInBackground = Application.runInBackground,
                IsResolving = gm?.IsResolving == true, PendingReaction = gm?.HasPendingReactionWindow == true,
                PendingReward = gm?.HasPendingRewardChoice == true, PendingDeckControl = gm?.HasPendingEncounterDeckControl == true,
                Heroes = gm?.ParticipatingHeroes.Select(h => new HeroView { Id = h.Id, Health = h.Health, MaxHealth = h.MaxHealth,
                    Actions = h.Actions, AttackOnlyActions = h.AttackOnlyActions, Potions = h.HealingPotions,
                    LocationId = h.LocationId, PendingLocationId = h.PendingLocationId, Moving = h.Moving,
                    TurnCompleted = h.TurnCompleted, Downed = h.Downed, Dead = h.Dead,
                    Hand = h.Hand.Select(c => c.Id).ToArray(), Discard = h.Discard.ToArray() }).ToArray() ?? Array.Empty<HeroView>(),
                Locations = gm?.Locations.Values.Select(l => new LocationView { Id = l.Id, Revealed = l.Revealed, Connections = l.Connections.ToArray() }).ToArray() ?? Array.Empty<LocationView>(),
                Enemies = gm?.Enemies.Values.Select(e => new EnemyView { Id = e.Id, Health = e.Health, Armor = e.Armor, LocationId = e.LocationId }).ToArray() ?? Array.Empty<EnemyView>(),
                Errors = errors.ToArray()
            };
        }

        private void WriteState() => AtomicWrite(Path.Combine(directory, "state.json"), JsonUtility.ToJson(Observe(), true));
        private void Respond(Request request, bool accepted, string code, string message)
        {
            if (request == null) return;
            AtomicWrite(Path.Combine(directory, "responses", request.Id + ".json"), JsonUtility.ToJson(new Reply
            { Id = request.Id, Accepted = accepted, Code = code, Message = message, State = Observe() }, true));
            WriteState();
        }
        private static void AtomicWrite(string path, string data)
        {
            File.WriteAllText(path + ".tmp", data);
            File.Move(path + ".tmp", path, true);
        }
        private void OnDestroy() { ClearPending(); Application.logMessageReceived -= CaptureError; }
    }
}
#endif
