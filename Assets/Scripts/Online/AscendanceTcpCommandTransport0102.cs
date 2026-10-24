using System;
using System.Net.Sockets;
using UnityEngine;

namespace Ascendance.Online
{
    /// <summary>
    /// Persistent remote client transport for 0.10.2. The existing client router remains
    /// synchronous; this adapter owns one TCP connection and one server-owned Hero session.
    /// </summary>
    public sealed class AscendanceTcpCommandTransport0102 : IAscendanceCommandTransport0101, IDisposable
    {
        private readonly string host;
        private readonly int port;
        private readonly string roomId;
        private readonly string displayName;
        private readonly object sync = new();

        private TcpClient client;
        private NetworkStream stream;
        private bool connected;
        private long revision;
        private string playerId;
        private string heroId;
        private string reconnectToken;
        private ServerStateSnapshot0100 lastSnapshot;

        public bool IsConnected => connected && client != null && stream != null;
        public string RoomId => roomId;
        public long Revision => revision;
        public string Endpoint => host + ":" + port;
        public string PlayerId => playerId;
        public string HeroId => heroId;
        public ServerStateSnapshot0100 LastSnapshot => lastSnapshot;

        public AscendanceTcpCommandTransport0102(string hostName, int serverPort, string requestedRoomId, string playerDisplayName)
        {
            host = string.IsNullOrWhiteSpace(hostName) ? "127.0.0.1" : hostName.Trim();
            port = Mathf.Clamp(serverPort <= 0 ? 7777 : serverPort, 1, 65535);
            roomId = string.IsNullOrWhiteSpace(requestedRoomId) ? "LOCAL-ROOM" : requestedRoomId.Trim();
            displayName = string.IsNullOrWhiteSpace(playerDisplayName) ? "Player" : playerDisplayName.Trim();
        }

        public bool TryConnect(out string message)
        {
            lock (sync)
            {
                if (IsConnected) { message = "Already connected."; return true; }
                CloseSocket();
                try
                {
                    client = new TcpClient { NoDelay = true, ReceiveTimeout = 10000, SendTimeout = 10000 };
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    client.Connect(host, port);
                    stream = client.GetStream();
                    connected = true;
                    var response = Exchange(new AscendanceNetworkRequest0102
                    {
                        RequestId = Guid.NewGuid().ToString("N"),
                        Operation = "HELLO",
                        RoomId = roomId
                    });
                    if (response == null || !response.Accepted)
                    {
                        message = response?.Message ?? "Server returned no HELLO response.";
                        CloseSocket();
                        return false;
                    }
                    if (!string.Equals(response.ServerBuildVersion, AscendanceGameManager.BuildVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        message = $"Server build {response.ServerBuildVersion} does not match client build {AscendanceGameManager.BuildVersion}.";
                        CloseSocket();
                        return false;
                    }
                    CaptureSnapshot(response.Snapshot);
                    message = $"Connected to {Endpoint} room {roomId} at revision {revision}.";
                    return true;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    CloseSocket();
                    return false;
                }
            }
        }

        public bool TryResolvePlayerForHero(string requestedHeroId, out string resolvedPlayerId)
        {
            lock (sync)
            {
                resolvedPlayerId = null;
                if (string.IsNullOrWhiteSpace(requestedHeroId)) return false;

                // TcpClient.Connected can remain true after the peer has closed the socket.
                // Resolve once, then reconnect/rejoin and retry once if the first exchange discovers
                // a stale connection. Reconnect tokens make that recovery server-authoritative.
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        if (!IsConnected && !TryConnect(out _)) return false;
                        if (ResolvePlayerForHeroConnected(requestedHeroId, out resolvedPlayerId)) return true;
                        return false;
                    }
                    catch (Exception)
                    {
                        CloseSocket();
                        if (attempt > 0) return false;
                    }
                }
                return false;
            }
        }

        private bool ResolvePlayerForHeroConnected(string requestedHeroId, out string resolvedPlayerId)
        {
            resolvedPlayerId = null;
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                if (!string.Equals(heroId, requestedHeroId, StringComparison.OrdinalIgnoreCase)) return false;
                resolvedPlayerId = playerId;
                return true;
            }

            string key = ReconnectKey(requestedHeroId);
            string savedToken = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedToken))
            {
                var rejoin = Exchange(new AscendanceNetworkRequest0102
                {
                    RequestId = Guid.NewGuid().ToString("N"),
                    Operation = "RECONNECT",
                    RoomId = roomId,
                    HeroId = requestedHeroId,
                    ReconnectToken = savedToken
                });
                if (rejoin != null && rejoin.Accepted && rejoin.Player != null && string.Equals(rejoin.Player.HeroId, requestedHeroId, StringComparison.OrdinalIgnoreCase))
                {
                    AdoptPlayer(rejoin.Player, rejoin.Snapshot);
                    resolvedPlayerId = playerId;
                    return true;
                }
                PlayerPrefs.DeleteKey(key);
            }

            var joined = Exchange(new AscendanceNetworkRequest0102
            {
                RequestId = Guid.NewGuid().ToString("N"),
                Operation = "JOIN",
                RoomId = roomId,
                DisplayName = displayName,
                HeroId = requestedHeroId
            });
            if (joined == null || !joined.Accepted || joined.Player == null) return false;
            AdoptPlayer(joined.Player, joined.Snapshot);
            PlayerPrefs.SetString(ReconnectKey(heroId), reconnectToken ?? string.Empty);
            PlayerPrefs.Save();
            resolvedPlayerId = playerId;
            return true;
        }

        public ServerCommandResult0100 Send(ServerCommandEnvelope0100 command)
        {
            lock (sync)
            {
                if (command == null) return Reject("BAD_COMMAND", "Command payload is missing.");

                // Retry the same CommandId once after reconnect. Room authority already caches
                // CommandIds, so this is safe even if the first response was lost after commit.
                string lastError = null;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        if (!IsConnected || string.IsNullOrWhiteSpace(playerId))
                        {
                            if (!TryResolvePlayerForHero(command.ActorHeroId, out _))
                                return Reject("TRANSPORT_OFFLINE", "Unable to establish or reconnect the remote Hero session.");
                        }
                        command.RoomId = roomId;
                        command.PlayerId = playerId;
                        command.ExpectedRevision = revision;
                        var response = Exchange(new AscendanceNetworkRequest0102
                        {
                            RequestId = Guid.NewGuid().ToString("N"),
                            Operation = "COMMAND",
                            RoomId = roomId,
                            Command = command
                        });
                        CaptureSnapshot(response?.Snapshot ?? response?.CommandResult?.Snapshot);
                        if (response?.CommandResult != null) return response.CommandResult;
                        return Reject(response?.Code ?? "NO_RESULT", response?.Message ?? "Server returned no command result.");
                    }
                    catch (Exception ex)
                    {
                        lastError = ex.Message;
                        CloseSocket();
                        if (attempt == 0) continue;
                    }
                }
                return Reject("TRANSPORT_ERROR", lastError ?? "Remote transport failed after reconnect retry.");
            }
        }

        public AscendanceNetworkResponse0102 RequestSnapshot()
        {
            lock (sync)
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        if (!IsConnected && !TryConnect(out _)) return null;
                        var response = Exchange(new AscendanceNetworkRequest0102
                        {
                            RequestId = Guid.NewGuid().ToString("N"),
                            Operation = "SNAPSHOT",
                            RoomId = roomId
                        });
                        CaptureSnapshot(response?.Snapshot);
                        return response;
                    }
                    catch (Exception)
                    {
                        CloseSocket();
                        if (attempt > 0) return null;
                    }
                }
                return null;
            }
        }

        private AscendanceNetworkResponse0102 Exchange(AscendanceNetworkRequest0102 request)
        {
            if (!IsConnected) throw new InvalidOperationException("TCP transport is not connected.");
            string requestJson = AscendanceNetworkCodec0102.SerializeRequest(request);
            AscendanceNetworkCodec0102.WriteFrame(stream, requestJson);
            string responseJson = AscendanceNetworkCodec0102.ReadFrame(stream);
            var response = AscendanceNetworkCodec0102.DeserializeResponse(responseJson);
            if (response == null) throw new InvalidOperationException("Server returned an unreadable response.");
            return response;
        }

        private void AdoptPlayer(ServerPlayerSession0100 player, ServerStateSnapshot0100 snapshot)
        {
            playerId = player.PlayerId;
            heroId = player.HeroId;
            reconnectToken = player.ReconnectToken;
            CaptureSnapshot(snapshot);
        }

        private void CaptureSnapshot(ServerStateSnapshot0100 snapshot)
        {
            if (snapshot == null) return;
            lastSnapshot = snapshot;
            revision = snapshot.Revision;
        }

        private string ReconnectKey(string requestedHeroId) => $"ASCENDANCE_0102_RECONNECT_{host}_{port}_{roomId}_{requestedHeroId}";

        private ServerCommandResult0100 Reject(string code, string message) => new()
        {
            Accepted = false,
            Code = code,
            Message = message,
            Revision = revision,
            Snapshot = lastSnapshot
        };

        private void CloseSocket()
        {
            connected = false;
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            stream = null;
            client = null;
            playerId = null;
            heroId = null;
            reconnectToken = null;
        }

        public void Dispose() => CloseSocket();
    }

    public static class AscendanceRemoteClientBootstrap0102
    {
        public static bool RemoteRequested { get; private set; }
        public static string Host { get; private set; } = "127.0.0.1";
        public static int Port { get; private set; } = 7777;
        public static string RoomId { get; private set; } = "LOCAL-ROOM";
        public static string DisplayName { get; private set; } = "Player";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Parse()
        {
            string[] args = Environment.GetCommandLineArgs() ?? Array.Empty<string>();
            string endpoint = Environment.GetEnvironmentVariable("ASCENDANCE_SERVER_ENDPOINT");
            RoomId = FirstNonBlank(Environment.GetEnvironmentVariable("ASCENDANCE_ROOM"), "LOCAL-ROOM");
            DisplayName = FirstNonBlank(Environment.GetEnvironmentVariable("ASCENDANCE_PLAYER"), "Player");

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-ascendance-connect", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) endpoint = args[++i];
                else if (string.Equals(args[i], "-room", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) RoomId = FirstNonBlank(args[++i], RoomId);
                else if (string.Equals(args[i], "-player", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) DisplayName = FirstNonBlank(args[++i], DisplayName);
            }

            if (string.IsNullOrWhiteSpace(endpoint)) { RemoteRequested = false; return; }
            RemoteRequested = TryParseEndpoint(endpoint, out var parsedHost, out var parsedPort);
            if (RemoteRequested) { Host = parsedHost; Port = parsedPort; }
        }

        public static bool TryInstall(AscendanceClientCommandRouter0101 router, AscendanceGameManager gm)
        {
            if (!RemoteRequested || router == null) return false;
            var remote = new AscendanceTcpCommandTransport0102(Host, Port, RoomId, DisplayName);
            router.SetTransport(remote);
            if (remote.TryConnect(out var message))
            {
                gm?.AddLog($"NETWORK CLIENT — connected to {Host}:{Port} room {RoomId}. Gameplay commands now execute on the remote authoritative server.");
                return true;
            }
            Debug.LogWarning($"[ASCENDANCE][NETWORK] Remote server requested but initial connection failed: {message}. The client will retry on the next gameplay command.");
            return false;
        }

        public static bool TryParseEndpoint(string endpoint, out string host, out int port)
        {
            host = null; port = 7777;
            if (string.IsNullOrWhiteSpace(endpoint)) return false;
            string value = endpoint.Trim();
            int colon = value.LastIndexOf(':');
            if (colon > 0 && colon < value.Length - 1 && int.TryParse(value[(colon + 1)..], out var parsedPort))
            {
                host = value[..colon].Trim();
                port = Mathf.Clamp(parsedPort, 1, 65535);
            }
            else host = value;
            return !string.IsNullOrWhiteSpace(host);
        }

        private static string FirstNonBlank(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
