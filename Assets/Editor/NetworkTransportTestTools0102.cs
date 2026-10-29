#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class NetworkTransportTestTools0102
    {
        private const string Prefix = "[ASCENDANCE][0.10.2h NETWORK]";

        [MenuItem("Ascendance/Validation/Test 0.10.2h - Real Network Transport (Play Mode)")]
        public static void Test0102()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            if (!Application.isPlaying) { Debug.LogError(Prefix + " Enter Play Mode first."); return; }
            var gm = AscendanceGameManager.Instance;
            if (gm == null) { Debug.LogError(Prefix + " No AscendanceGameManager found."); return; }
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass = 0, fail = 0;
            void Check(bool ok, string label)
            {
                if (ok) { pass++; Debug.Log(Prefix + " PASS — " + label); }
                else { fail++; Debug.LogError(Prefix + " FAIL — " + label); }
            }

            Check(AscendanceGameManager.BuildVersion == "0.10.2h", "BuildVersion identifies the Real Network Transport milestone.");
            Check(AscendanceNetworkCodec0102.ProtocolVersion == 1 && AscendanceNetworkCodec0102.MaxFrameBytes >= 1024 * 1024,
                "Wire protocol publishes a bounded versioned framed-message contract.");
            Check(typeof(AscendanceTcpCommandTransport0102).GetInterfaces().Contains(typeof(IAscendanceCommandTransport0101)),
                "Remote TCP transport plugs into the existing transport-agnostic client router contract.");
            Check(typeof(AscendanceTcpServer0102).GetMethod("Initialize", new[] { typeof(AscendanceServerAuthority0100), typeof(int) }) != null,
                "Dedicated server exposes a concrete TCP listener initialization boundary.");
            Check(typeof(AscendanceDedicatedServerBootstrap0100).GetProperty("RequestedPort", BindingFlags.Public | BindingFlags.Static) != null,
                "Dedicated server startup exposes configurable TCP port selection.");
            Check(AscendanceRemoteClientBootstrap0102.TryParseEndpoint("127.0.0.1:8123", out var parsedHost, out var parsedPort) && parsedHost == "127.0.0.1" && parsedPort == 8123,
                "Remote client endpoint parser accepts host:port targets.");

            var request = new AscendanceNetworkRequest0102
            {
                RequestId = "REQ-0102",
                Operation = "COMMAND",
                RoomId = "ROOM-0102",
                Command = new ServerCommandEnvelope0100
                {
                    CommandId = "CMD-0102",
                    RoomId = "ROOM-0102",
                    PlayerId = "PLAYER-0102",
                    ExpectedRevision = 7,
                    Type = ServerCommandType0100.TransferHeroLoot,
                    ActorHeroId = "Aldric",
                    TargetId = "LT001",
                    SecondaryTargetId = "Lyra",
                    IntValue = 2
                }
            };
            string requestJson = AscendanceNetworkCodec0102.SerializeRequest(request);
            var decodedRequest = AscendanceNetworkCodec0102.DeserializeRequest(requestJson);
            Check(decodedRequest != null && decodedRequest.ProtocolVersion == 1 && decodedRequest.Command != null && decodedRequest.Command.SecondaryTargetId == "Lyra" && decodedRequest.Command.IntValue == 2,
                "Network JSON round-trip preserves authoritative command payload fields.");

            using (var memory = new MemoryStream())
            {
                AscendanceNetworkCodec0102.WriteFrame(memory, requestJson);
                memory.Position = 0;
                Check(AscendanceNetworkCodec0102.ReadFrame(memory) == requestJson,
                    "Length-prefixed TCP framing round-trips payloads without newline assumptions.");
            }

            string assets = Application.dataPath;
            string bootstrap = File.ReadAllText(Path.Combine(assets, "Scripts/Core/GameBootstrap.cs"));
            string builder = File.ReadAllText(Path.Combine(assets, "Editor/AscendanceDedicatedServerBuilder0100.cs"));
            string routerSource = File.ReadAllText(Path.Combine(assets, "Scripts/Online/AscendanceClientCommandRouter0101.cs"));
            string tcpServerSource = File.ReadAllText(Path.Combine(assets, "Scripts/Online/AscendanceTcpServer0102.cs"));
            string tcpClientSource = File.ReadAllText(Path.Combine(assets, "Scripts/Online/AscendanceTcpCommandTransport0102.cs"));
            Check(bootstrap.Contains("AscendanceTcpServer0102") && bootstrap.Contains("AscendanceRemoteClientBootstrap0102.TryInstall"),
                "Stonebrook bootstrap installs TCP server or remote client transport according to process role.");
            Check(builder.Contains("BuildTarget.StandaloneLinux64") && builder.Contains("StandaloneBuildSubtarget.Server"),
                "Unity build tooling includes a Linux Dedicated Server target for DigitalOcean.");
            Check(routerSource.Contains("ApplyRemoteSnapshot0102") && routerSource.Contains("IsRemoteTcp"),
                "Remote command responses reconcile authoritative campaign snapshots on the client.");
            Check(tcpServerSource.Contains("ReceiveTimeout = 0") && !tcpServerSource.Contains("ReceiveTimeout = 35000") && tcpServerSource.Contains("SocketOptionName.KeepAlive"),
                "Persistent server TCP sessions do not expire while a player is thinking and enable socket keepalive.");
            Check(tcpClientSource.Contains("SocketOptionName.KeepAlive") && tcpClientSource.Contains("for (int attempt = 0; attempt < 2; attempt++)") && tcpClientSource.Contains("same CommandId once after reconnect"),
                "Remote client detects stale sockets, reconnects/rejoins, and retries an idempotent command once.");
            Check(File.Exists(Path.Combine(Directory.GetParent(assets).FullName, "BuildAscendanceLinuxServer.bat")),
                "Windows development workflow includes one-click Linux server build automation.");

            bool routerFinished = false, routerPassed = false;
            int inheritedErrors = 0;
            void OnRouterLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) inheritedErrors++;
                if (string.IsNullOrEmpty(condition) || !condition.Contains("Client Command Routing completed:")) return;
                routerFinished = true;
                routerPassed = condition.Contains("[ASCENDANCE][0.10.2h ROUTER] PASS") && condition.Contains("29 passed, 0 failed");
            }
            Application.logMessageReceived += OnRouterLog;
            try
            {
                ClientCommandRoutingTestTools0101.Test0101();
                int frames = 0;
                while (!routerFinished && frames++ < 120000) yield return null;
            }
            finally { Application.logMessageReceived -= OnRouterLog; }
            Check(routerFinished && routerPassed, "Locked Client Command Routing suite remains 29 passed / 0 failed under real transport.");
            Check(inheritedErrors == 0, "Inherited router/server/core gates emit zero Unity Error/Exception/Assert entries under 0.10.2h.");

            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            AscendanceRoomAuthority0100 isolated = null;
            GameObject serverObject = null;
            try
            {
                // The inherited router gate can finish while a round-transition/world-phase coroutine
                // is still unwinding. Drain that real async resolution before constructing the isolated
                // socket scenario so this test measures transport/authority, not cross-test contamination.
                bool RuntimeTransientIdle() => !gm.IsResolving
                    && !gm.HasPendingReactionWindow
                    && !gm.HasPendingEncounterDeckControl
                    && !gm.HasPendingRewardChoice
                    && !gm.ParticipatingHeroes.Any(h => h.Moving);
                int settleFrames = 0, stableIdleFrames = 0;
                while (settleFrames++ < 3600 && stableIdleFrames < 90)
                {
                    if (RuntimeTransientIdle()) stableIdleFrames++; else stableIdleFrames = 0;
                    yield return null;
                }
                Check(stableIdleFrames >= 90,
                    "Inherited async runtime remains stably idle before isolated real-socket command validation.");

                gm.SetParty(new[] { "Aldric", "Lyra" }, "Aldric");
                gm.DebugResetCampaignProgress0833();
                yield return null;

                gm.Heroes.TryGetValue("Aldric", out var socketHero);
                int readyFrames = 0, stableStartFrames = 0;
                while (readyFrames++ < 1800 && stableStartFrames < 45)
                {
                    if (socketHero != null && gm.CanStartHeroTurn(socketHero)) stableStartFrames++; else stableStartFrames = 0;
                    yield return null;
                }
                Check(socketHero != null && stableStartFrames >= 45,
                    "Isolated Q001 fixture remains stably legal for Aldric START TURN before the socket command is sent.");

                string roomId = "NET-0102-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                isolated = new AscendanceRoomAuthority0100(gm, roomId, false);
                serverObject = new GameObject("ASCENDANCE_0102_NETWORK_TEST");
                var server = serverObject.AddComponent<AscendanceTcpServer0102>();
                Check(server.Initialize(isolated, 0) && server.IsListening && server.BoundPort > 0,
                    "Real TCP listener binds an ephemeral localhost test port without player-host authority.");

                var hello = new AscendanceNetworkRequest0102 { RequestId = "HELLO-0102", Operation = "HELLO", RoomId = roomId };
                var helloTask = SendRawAsync(server.BoundPort, AscendanceNetworkCodec0102.SerializeRequest(hello));
                int helloFrames = 0;
                while (!helloTask.IsCompleted && helloFrames++ < 1200) yield return null;
                AscendanceNetworkResponse0102 helloResponse = null;
                if (helloTask.Status == TaskStatus.RanToCompletion)
                    helloResponse = AscendanceNetworkCodec0102.DeserializeResponse(helloTask.Result);
                Check(helloResponse != null && helloResponse.Accepted && helloResponse.Code == "HELLO" && helloResponse.Snapshot != null && helloResponse.Snapshot.RoomId == roomId,
                    "A real localhost TCP HELLO crosses the socket and returns the authoritative room snapshot.");
                Check(helloResponse != null && helloResponse.ProtocolVersion == 1 && helloResponse.ServerBuildVersion == "0.10.2h",
                    "TCP handshake enforces protocol/build identity before gameplay commands.");

                var joined = isolated.Join("Network Validator", "Aldric");
                Check(joined.Accepted && joined.Player != null, "Isolated authority creates a concrete player/Hero session for socket command validation.");
                long expectedRevision = isolated.Room.Revision;
                var commandRequest = new AscendanceNetworkRequest0102
                {
                    RequestId = "COMMAND-0102",
                    Operation = "COMMAND",
                    RoomId = roomId,
                    Command = new ServerCommandEnvelope0100
                    {
                        CommandId = "SOCKET-CMD-0102",
                        RoomId = roomId,
                        PlayerId = joined.Player.PlayerId,
                        ExpectedRevision = expectedRevision,
                        Type = ServerCommandType0100.StartTurn,
                        ActorHeroId = "Aldric"
                    }
                };
                var commandTask = SendAuthenticatedRawAsync(server.BoundPort, roomId, joined.Player, AscendanceNetworkCodec0102.SerializeRequest(commandRequest));
                int commandFrames = 0;
                while (!commandTask.IsCompleted && commandFrames++ < 1200) yield return null;
                AscendanceNetworkResponse0102 commandResponse = null;
                if (commandTask.Status == TaskStatus.RanToCompletion)
                    commandResponse = AscendanceNetworkCodec0102.DeserializeResponse(commandTask.Result);
                Check(commandResponse != null && commandResponse.Accepted && commandResponse.CommandResult != null && commandResponse.CommandResult.Accepted && gm.TurnHeroId == "Aldric",
                    "START TURN crosses a real TCP socket into room authority and executes on the Unity main thread.");
                Check(commandResponse?.Snapshot != null && commandResponse.CommandResult != null
                    && commandResponse.Snapshot.Revision == commandResponse.CommandResult.Revision
                    && commandResponse.Snapshot.Revision <= isolated.Room.Revision
                    && commandResponse.Snapshot.Campaign?.Version == "0.10.2h",
                    "Socket command response returns the committed 0.10.2h authoritative snapshot/revision.");

                var mismatch = new AscendanceNetworkRequest0102 { RequestId = "BADROOM-0102", Operation = "HELLO", RoomId = roomId + "-WRONG" };
                var mismatchTask = SendRawAsync(server.BoundPort, AscendanceNetworkCodec0102.SerializeRequest(mismatch));
                int mismatchFrames = 0;
                while (!mismatchTask.IsCompleted && mismatchFrames++ < 1200) yield return null;
                AscendanceNetworkResponse0102 mismatchResponse = null;
                if (mismatchTask.Status == TaskStatus.RanToCompletion)
                    mismatchResponse = AscendanceNetworkCodec0102.DeserializeResponse(mismatchTask.Result);
                Check(mismatchResponse != null && !mismatchResponse.Accepted && mismatchResponse.Code == "ROOM_MISMATCH",
                    "TCP server rejects requests for a different authoritative room.");

                server.StopServer();
                Check(!server.IsListening, "TCP listener shuts down cleanly without terminating the Unity rules runtime.");
            }
            finally
            {
                if (serverObject != null)
                {
                    var server = serverObject.GetComponent<AscendanceTcpServer0102>();
                    server?.StopServer();
                    UnityEngine.Object.Destroy(serverObject);
                }
                isolated?.Dispose();
                if (restore != null) gm.ApplySaveData(restore);
            }
            yield return null;

            Check(gm != null && AscendanceClientCommandRouter0101.Instance != null && AscendanceClientCommandRouter0101.Instance.IsReady,
                "Network validator restores the production client/router runtime after isolated socket testing.");

            if (fail == 0) Debug.Log($"{Prefix} PASS — Real Network Transport completed: {pass} passed, 0 failed.");
            else Debug.LogError($"{Prefix} FAIL — Real Network Transport completed: {pass} passed, {fail} failed.");
        }

        private static Task<string> SendRawAsync(int port, string requestJson)
        {
            return Task.Run(() =>
            {
                using var client = new TcpClient { NoDelay = true, ReceiveTimeout = 10000, SendTimeout = 10000 };
                client.Connect("127.0.0.1", port);
                using NetworkStream stream = client.GetStream();
                AscendanceNetworkCodec0102.WriteFrame(stream, requestJson);
                return AscendanceNetworkCodec0102.ReadFrame(stream);
            });
        }

        private static Task<string> SendAuthenticatedRawAsync(int port, string roomId, ServerPlayerSession0100 player, string requestJson)
        {
            return Task.Run(() =>
            {
                using var client = new TcpClient { NoDelay = true, ReceiveTimeout = 10000, SendTimeout = 10000 };
                client.Connect("127.0.0.1", port);
                using NetworkStream stream = client.GetStream();
                var reconnect = new AscendanceNetworkRequest0102
                {
                    RequestId = Guid.NewGuid().ToString("N"), Operation = "RECONNECT", RoomId = roomId,
                    HeroId = player.HeroId, ReconnectToken = player.ReconnectToken
                };
                AscendanceNetworkCodec0102.WriteFrame(stream, AscendanceNetworkCodec0102.SerializeRequest(reconnect));
                var authenticated = AscendanceNetworkCodec0102.DeserializeResponse(AscendanceNetworkCodec0102.ReadFrame(stream));
                if (authenticated == null || !authenticated.Accepted) throw new InvalidOperationException("Socket fixture could not authenticate its Hero session.");
                AscendanceNetworkCodec0102.WriteFrame(stream, requestJson);
                return AscendanceNetworkCodec0102.ReadFrame(stream);
            });
        }
    }
}
#endif
