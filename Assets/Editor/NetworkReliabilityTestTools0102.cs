#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    // Real localhost sockets and the production client/server classes. No simulated rules.
    public static class NetworkReliabilityTestTools0102
    {
        private const string Prefix = "[ASCENDANCE][0.10.2h RELIABILITY]";

        [MenuItem("Ascendance/Validation/Test 0.10.2h - Network Reliability (Play Mode)")]
        public static void Test0102Reliability()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
                throw new InvalidOperationException("Open Stonebrook in Play Mode first.");
            AscendanceGameManager.Instance.StartCoroutine(Run(AscendanceGameManager.Instance));
        }

        private static IEnumerator Await(Task task, float seconds = 15f)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            if (!task.IsCompleted) throw new TimeoutException("Real socket operation exceeded its deadline.");
            task.GetAwaiter().GetResult();
        }

        private static ServerCommandEnvelope0100 Command(ServerCommandType0100 type, string hero, ServerStateSnapshot0100 snapshot) => new()
        {
            CommandId = Guid.NewGuid().ToString("N"), Type = type, ActorHeroId = hero,
            RoomId = snapshot.RoomId, ExpectedRevision = snapshot.Revision,
            HasExpectedGameplayRevision = true, ExpectedGameplayRevision = snapshot.GameplayRevision,
            ExpectedServerInstanceId = snapshot.ServerInstanceId
        };

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            string room = "RELIABILITY-" + Guid.NewGuid().ToString("N");
            int checks = 0, port = 0;
            void Check(bool condition, string label)
            {
                if (!condition) throw new InvalidOperationException(Prefix + " FAIL — " + label);
                checks++;
                Debug.Log(Prefix + " PASS — " + label);
            }
            AscendanceRoomAuthority0100 authority = null;
            AscendanceTcpCommandTransport0102 a = null, b = null;
            RawSession old = null, replacement = null, stranger = null;
            GameObject serverObject = null;
            try
            {
                // Wait for inherited gameplay coroutines, using real time rather than frame counts.
                float deadline = Time.realtimeSinceStartup + 60f;
                while ((gm.IsResolving || gm.HasPendingReactionWindow || gm.ParticipatingHeroes.Any(h => h.Moving)) && Time.realtimeSinceStartup < deadline) yield return null;
                if (gm.IsResolving || gm.HasPendingReactionWindow) throw new TimeoutException("Inherited fixture did not settle.");
                // New Game creates fresh HeroState objects. A campaign-only reset intentionally
                // retains defeats from prior quests, including inactive Heroes used by other gates.
                gm.Initialize();
                gm.SetParty(new[] { "Aldric", "Lyra", "Elara" }, "Aldric");
                gm.DebugResetCampaignProgress0833();
                if (gm.ParticipatingHeroes.Any(h => h.IsDefeated)) throw new InvalidOperationException("Fresh network fixture contains a defeated Hero.");
                deadline = Time.realtimeSinceStartup + 30f;
                while (!gm.CanStartHeroTurn(gm.Heroes["Aldric"]) && Time.realtimeSinceStartup < deadline) yield return null;
                if (!gm.CanStartHeroTurn(gm.Heroes["Aldric"])) throw new TimeoutException("Q001 start-turn fixture did not become legal.");

                authority = new AscendanceRoomAuthority0100(gm, room, false);
                serverObject = new GameObject("ASCENDANCE_RELIABILITY_TEST");
                var server = serverObject.AddComponent<AscendanceTcpServer0102>();
                Check(server.Initialize(authority, 0) && server.BoundPort > 0, "1. Listener binds an isolated ephemeral port.");
                port = server.BoundPort;
                a = new AscendanceTcpCommandTransport0102("127.0.0.1", port, room, "Test A");
                b = new AscendanceTcpCommandTransport0102("127.0.0.1", port, room, "Test B");
                var connectA = a.ConnectAsync(); yield return Await(connectA);
                Check(connectA.Result && a.LatestSnapshot?.RoomId == room, "2. Production client A receives the initial HELLO snapshot.");
                var startA = a.SendAsync(Command(ServerCommandType0100.StartTurn, "Aldric", a.LatestSnapshot)); yield return Await(startA);
                Check(startA.Result.Accepted && gm.TurnHeroId == "Aldric" && gm.Heroes["Aldric"].Actions == 3, "3. A's START TURN changes actual Hero actions to three.");
                var beforePresence = a.LatestSnapshot;
                var endACommand = Command(ServerCommandType0100.EndTurn, "Aldric", beforePresence);
                var connectB = b.ConnectAsync(); yield return Await(connectB);
                Check(connectB.Result && b.LatestSnapshot.Transient.TurnHeroId == "Aldric", "4. Second connected client sees A's active turn.");
                var prematureB = b.SendAsync(Command(ServerCommandType0100.StartTurn, "Lyra", b.LatestSnapshot)); yield return Await(prematureB);
                Check(!prematureB.Result.Accepted && prematureB.Result.Code == "RULE_REJECTED" && authority.Room.Players.Count == 2
                    && authority.Room.GameplayRevision == beforePresence.GameplayRevision && authority.Room.Revision > beforePresence.Revision,
                    "5. B joins without changing gameplay revision; rules reject a concurrent turn.");
                var endA = a.SendAsync(endACommand); yield return Await(endA);
                Check(endA.Result.Accepted && gm.TurnHeroId == null && gm.Heroes["Aldric"].TurnCompleted, "6. Presence-only revision changes do not reject A's legal END TURN.");
                deadline = Time.realtimeSinceStartup + 8f;
                while (b.LatestSnapshot.GameplayRevision < endA.Result.Snapshot.GameplayRevision && Time.realtimeSinceStartup < deadline) yield return null;
                var peerAldric = b.LatestSnapshot.Campaign.Heroes.Single(h => h.Id == "Aldric");
                Check(b.LatestSnapshot.GameplayRevision >= endA.Result.Snapshot.GameplayRevision
                    && string.IsNullOrEmpty(b.LatestSnapshot.Transient.TurnHeroId) && peerAldric.TurnCompleted
                    && peerAldric.Actions == 3 && peerAldric.AttackOnlyActions == 0,
                    $"7. Idle client B sees A's completed turn and three saved Reaction Actions (revision {b.LatestSnapshot.GameplayRevision}/{endA.Result.Snapshot.GameplayRevision}, turn '{b.LatestSnapshot.Transient.TurnHeroId}', completed {peerAldric.TurnCompleted}, actions {peerAldric.Actions}).");
                var startB = b.SendAsync(Command(ServerCommandType0100.StartTurn, "Lyra", b.LatestSnapshot)); yield return Await(startB);
                Check(startB.Result.Accepted && gm.TurnHeroId == "Lyra" && gm.Heroes["Lyra"].Actions == 3,
                    $"8. B starts its own actual Hero turn (accepted {startB.Result.Accepted}, code {startB.Result.Code}, turn '{gm.TurnHeroId}', actions {gm.Heroes["Lyra"].Actions}, resolving {gm.IsResolving}, revision {startB.Result.Revision}, gameplay {authority.Room.GameplayRevision}; {string.Join(" / ", gm.Log.TakeLast(3))}).");
                deadline = Time.realtimeSinceStartup + 8f;
                while (a.LatestSnapshot.GameplayRevision < startB.Result.Snapshot.GameplayRevision && Time.realtimeSinceStartup < deadline) yield return null;
                Check(a.LatestSnapshot.Transient.TurnHeroId == "Lyra" && a.LatestSnapshot.GameplayRevision >= startB.Result.Snapshot.GameplayRevision, "9. Idle client A converges to B's state without sending a command.");
                var duplicate = a.SendAsync(endACommand); yield return Await(duplicate);
                Check(duplicate.Result.Accepted && duplicate.Result.Duplicate && duplicate.Result.Snapshot.Transient.TurnHeroId == "Lyra"
                    && gm.Heroes["Lyra"].Actions == 3, "10. Retried accepted command returns fresh state and does not execute twice.");
                var stale = a.SendAsync(Command(ServerCommandType0100.StartTurn, "Aldric", beforePresence)); yield return Await(stale);
                Check(!stale.Result.Accepted && stale.Result.Code == "STALE_REVISION" && gm.TurnHeroId == "Lyra", "11. Genuine gameplay conflicts remain rejected.");
                stranger = new RawSession(port, room);
                var forgedCommand = Command(ServerCommandType0100.EndTurn, "Lyra", authority.Snapshot()); forgedCommand.PlayerId = b.PlayerId;
                var forged = stranger.ExchangeAsync("COMMAND", command: forgedCommand); yield return Await(forged);
                Check(!forged.Result.Accepted && forged.Result.Code == "PLAYER_NOT_BOUND" && gm.TurnHeroId == "Lyra", "12. An unjoined socket cannot command a disclosed PlayerId.");

                string credential = Guid.NewGuid().ToString("N");
                old = new RawSession(port, room);
                var join = old.ExchangeAsync("JOIN", "Elara", credential); yield return Await(join);
                Check(join.Result.Accepted && join.Result.Player?.HeroId == "Elara" && join.Result.Player.ReconnectToken == credential, "13. JOIN acknowledges the client-persisted session credential.");
                var joinAgain = old.ExchangeAsync("JOIN", "Elara", credential); yield return Await(joinAgain);
                Check(joinAgain.Result.Accepted && joinAgain.Result.Player.PlayerId == join.Result.Player.PlayerId && authority.Room.Players.Count == 3, "14. Lost JOIN replies can be recovered without a second Hero claim.");
                replacement = new RawSession(port, room);
                var rejoin = replacement.ExchangeAsync("RECONNECT", "Elara", credential); yield return Await(rejoin);
                Check(rejoin.Result.Accepted && rejoin.Result.Player.PlayerId == join.Result.Player.PlayerId, "15. A second socket reconnects to the same Hero session.");
                var revokedCommand = Command(ServerCommandType0100.StartTurn, "Elara", authority.Snapshot()); revokedCommand.PlayerId = join.Result.Player.PlayerId;
                var revoked = old.ExchangeAsync("COMMAND", command: revokedCommand); yield return Await(revoked);
                Check(!revoked.Result.Accepted && revoked.Result.Code == "PLAYER_NOT_BOUND", "16. Reconnect revokes the old socket's command lease.");
                long closed = server.ProcessedDisconnects;
                old.Dispose(); old = null;
                deadline = Time.realtimeSinceStartup + 8f;
                while (server.ProcessedDisconnects == closed && Time.realtimeSinceStartup < deadline) yield return null;
                Check(server.ProcessedDisconnects > closed && authority.Room.Players.Single(p => p.HeroId == "Elara").Connected,
                    "17. Old-socket teardown cannot disconnect the replacement session.");
                var ping = replacement.ExchangeAsync("PING"); yield return Await(ping);
                Check(ping.Result.Accepted && ping.Result.Code == "PONG", "18. Replacement connection remains usable after teardown.");
                // Break an actual production-client socket, then require reconnect + one real effect.
                var clientField = typeof(AscendanceTcpCommandTransport0102).GetField("client", BindingFlags.Instance | BindingFlags.NonPublic);
                ((TcpClient)clientField.GetValue(b)).Close();
                var endB = b.SendAsync(Command(ServerCommandType0100.EndTurn, "Lyra", startB.Result.Snapshot)); yield return Await(endB);
                Check(endB.Result.Accepted && gm.TurnHeroId == null && gm.Heroes["Lyra"].TurnCompleted && authority.Room.Players.Count == 3,
                    "19. A broken production-client socket reconnects and commits END TURN once.");
                var previousProcess = Command(ServerCommandType0100.StartTurn, "Aldric", authority.Snapshot()); previousProcess.ExpectedServerInstanceId = Guid.NewGuid().ToString("N");
                var restart = a.SendAsync(previousProcess); yield return Await(restart);
                Check(!restart.Result.Accepted && restart.Result.Code == "SERVER_RESTARTED" && gm.TurnHeroId == null, "20. Commands from a previous server process require a fresh decision.");
                // Unlike polling clients, this socket has no traffic for longer than the old 35s timeout.
                yield return new WaitForSecondsRealtime(36f);
                var idlePing = replacement.ExchangeAsync("PING"); yield return Await(idlePing);
                Check(idlePing.Result.Accepted && idlePing.Result.Code == "PONG", "21. A genuinely idle joined socket survives the former 35-second timeout.");
                server.StopServer();
                Check(!server.IsListening, "22. The isolated listener shuts down cleanly.");
            }
            finally
            {
                a?.Dispose(); b?.Dispose(); old?.Dispose(); replacement?.Dispose(); stranger?.Dispose();
                if (serverObject != null) { serverObject.GetComponent<AscendanceTcpServer0102>()?.StopServer(); UnityEngine.Object.Destroy(serverObject); }
                authority?.Dispose();
                if (restore != null) gm.ApplySaveData(restore);
                if (port > 0)
                {
                    foreach (string hero in new[] { "Aldric", "Lyra" }) PlayerPrefs.DeleteKey($"ASCENDANCE_0102_RECONNECT_127.0.0.1_{port}_{room}_{hero}");
                    PlayerPrefs.Save();
                }
            }
            Debug.Log($"{Prefix} PASS — Network Reliability completed: {checks} passed, 0 failed.");
        }

        private sealed class RawSession : IDisposable
        {
            private readonly int port;
            private readonly string room;
            private readonly object sync = new();
            private TcpClient client;
            public RawSession(int serverPort, string roomId) { port = serverPort; room = roomId; }
            public Task<AscendanceNetworkResponse0102> ExchangeAsync(string op, string hero = null, string token = null, ServerCommandEnvelope0100 command = null)
            {
                var request = new AscendanceNetworkRequest0102 { RequestId = Guid.NewGuid().ToString("N"), Operation = op, RoomId = room,
                    HeroId = hero, DisplayName = "Isolated reliability test", ReconnectToken = token, Command = command };
                return Task.Run(() =>
                {
                    lock (sync)
                    {
                        if (client == null) { client = new TcpClient { NoDelay = true, ReceiveTimeout = 10000, SendTimeout = 10000 }; client.Connect("127.0.0.1", port); }
                        var stream = client.GetStream();
                        AscendanceNetworkCodec0102.WriteFrame(stream, AscendanceNetworkCodec0102.SerializeRequest(request));
                        var response = AscendanceNetworkCodec0102.DeserializeResponse(AscendanceNetworkCodec0102.ReadFrame(stream));
                        if (response == null || response.RequestId != request.RequestId) throw new InvalidOperationException("Mismatched test response.");
                        return response;
                    }
                });
            }
            public void Dispose() { client?.Close(); }
        }
    }
}
#endif
