using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ascendance.Online
{
    public interface IAscendanceSnapshotSource0102
    {
        event Action<ServerStateSnapshot0100> SnapshotAvailable;
        ServerStateSnapshot0100 LatestSnapshot { get; }
    }

    /// <summary>One persistent connection and one Hero session. Async entry points prepare
    /// credentials on Unity's thread and run socket I/O on workers.</summary>
    public sealed class AscendanceTcpCommandTransport0102 : IAscendanceCommandTransport0101, IAscendanceSnapshotSource0102, IDisposable
    {
        private const int ConnectTimeoutMs = 2000;
        private const int IoTimeoutMs = 3000;
        private readonly string host;
        private readonly int port;
        private readonly string roomId;
        private readonly string displayName;
        private readonly object sync = new();
        private readonly ConcurrentDictionary<string, string> credentials = new(StringComparer.OrdinalIgnoreCase);
        private readonly CancellationTokenSource lifetime = new();
        private int pollingStarted;
        private volatile bool disposed;
        private TcpClient client;
        private NetworkStream stream;
        private volatile bool connected;
        private long revision;
        private string playerId;
        private string heroId;
        private ServerStateSnapshot0100 lastSnapshot;

        public event Action<ServerStateSnapshot0100> SnapshotAvailable;
        public bool IsConnected => connected && !disposed;
        public string RoomId => roomId;
        public long Revision => Interlocked.Read(ref revision);
        public string Endpoint => host + ":" + port;
        public string PlayerId => playerId;
        public string HeroId => heroId;
        public ServerStateSnapshot0100 LastSnapshot => lastSnapshot;
        public ServerStateSnapshot0100 LatestSnapshot => lastSnapshot;

        public AscendanceTcpCommandTransport0102(string hostName, int serverPort, string requestedRoomId, string playerDisplayName)
        {
            host = string.IsNullOrWhiteSpace(hostName) ? "127.0.0.1" : hostName.Trim();
            port = Math.Max(1, Math.Min(65535, serverPort <= 0 ? 7777 : serverPort));
            roomId = string.IsNullOrWhiteSpace(requestedRoomId) ? "LOCAL-ROOM" : requestedRoomId.Trim();
            displayName = string.IsNullOrWhiteSpace(playerDisplayName) ? "Player" : playerDisplayName.Trim();
        }

        public Task<bool> ConnectAsync()
        {
            EnsurePolling();
            return Task.Run(() => TryConnect(out _));
        }

        public bool TryConnect(out string message)
        {
            lock (sync)
            {
                if (disposed) { message = "Transport is disposed."; return false; }
                if (IsConnected) { message = "Already connected."; return true; }
                CloseSocket();
                try
                {
                    client = new TcpClient { NoDelay = true, ReceiveTimeout = IoTimeoutMs, SendTimeout = IoTimeoutMs };
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    Task connecting = client.ConnectAsync(host, port);
                    if (!connecting.Wait(ConnectTimeoutMs))
                    {
                        _ = connecting.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                        throw new TimeoutException("Server connection timed out.");
                    }
                    connecting.GetAwaiter().GetResult();
                    stream = client.GetStream();
                    connected = true;
                    var response = Exchange(NewRequest("HELLO"));
                    if (!response.Accepted)
                    {
                        message = response.Message;
                        CloseSocket();
                        return false;
                    }
                    CaptureSnapshot(response.Snapshot);
                    EnsurePolling();
                    message = $"Connected to {Endpoint} room {roomId} at revision {Revision}.";
                    return true;
                }
                catch (Exception ex)
                {
                    message = ex.GetBaseException().Message;
                    CloseSocket();
                    return false;
                }
            }
        }

        // Main-thread only. Persist before JOIN: a lost reply can recover the same claim.
        private void PrepareHeroCredential(string requestedHeroId)
        {
            if (string.IsNullOrWhiteSpace(requestedHeroId) || credentials.ContainsKey(requestedHeroId)) return;
            string key = ReconnectKey(requestedHeroId);
            string token = PlayerPrefs.GetString(key, string.Empty);
            if (!Guid.TryParseExact(token, "N", out _))
            {
                token = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(key, token);
                PlayerPrefs.Save();
            }
            credentials.TryAdd(requestedHeroId, token);
        }

        public bool TryResolvePlayerForHero(string requestedHeroId, out string resolvedPlayerId)
        {
            PrepareHeroCredential(requestedHeroId);
            lock (sync) return TryResolvePrepared(requestedHeroId, out resolvedPlayerId);
        }

        private bool TryResolvePrepared(string requestedHeroId, out string resolvedPlayerId)
        {
            resolvedPlayerId = null;
            if (disposed || string.IsNullOrWhiteSpace(requestedHeroId)) return false;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    if (!IsConnected && !TryConnect(out _)) return false;
                    if (!string.IsNullOrWhiteSpace(playerId))
                    {
                        if (!string.Equals(heroId, requestedHeroId, StringComparison.OrdinalIgnoreCase)) return false;
                        resolvedPlayerId = playerId;
                        return true;
                    }
                    if (!credentials.TryGetValue(requestedHeroId, out var token)) return false;
                    var rejoin = NewRequest("RECONNECT");
                    rejoin.HeroId = requestedHeroId;
                    rejoin.ReconnectToken = token;
                    var response = Exchange(rejoin);
                    if (!response.Accepted && response.Code == "RECONNECT_DENIED")
                    {
                        var join = NewRequest("JOIN");
                        join.DisplayName = displayName;
                        join.HeroId = requestedHeroId;
                        join.ReconnectToken = token;
                        response = Exchange(join);
                    }
                    CaptureSnapshot(response.Snapshot);
                    if (!response.Accepted || response.Player == null ||
                        !string.Equals(response.Player.HeroId, requestedHeroId, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(response.Player.ReconnectToken, token, StringComparison.Ordinal)) return false;
                    playerId = response.Player.PlayerId;
                    heroId = response.Player.HeroId;
                    resolvedPlayerId = playerId;
                    return true;
                }
                catch (Exception)
                {
                    CloseSocket();
                    if (attempt > 0) return false;
                }
            }
            return false;
        }

        public Task<ServerCommandResult0100> SendAsync(ServerCommandEnvelope0100 command)
        {
            PrepareHeroCredential(command?.ActorHeroId);
            return Task.Run(() => SendPrepared(command));
        }

        public ServerCommandResult0100 Send(ServerCommandEnvelope0100 command)
        {
            PrepareHeroCredential(command?.ActorHeroId);
            return SendPrepared(command);
        }

        private ServerCommandResult0100 SendPrepared(ServerCommandEnvelope0100 command)
        {
            lock (sync)
            {
                if (command == null) return Reject("BAD_COMMAND", "Command payload is missing.");
                if (disposed) return Reject("TRANSPORT_OFFLINE", "Transport is disposed.");
                string lastError = null;
                // Retry the same CommandId once after reconnect. Keep its gameplay revision
                // and server instance fixed: conflicts require a fresh user decision.
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        if (!TryResolvePrepared(command.ActorHeroId, out _))
                            return Reject("PLAYER_SESSION_REQUIRED", "Unable to establish this Hero session.");
                        command.RoomId = roomId;
                        command.PlayerId = playerId;
                        if (!command.HasExpectedGameplayRevision && lastSnapshot != null)
                        {
                            command.ExpectedRevision = lastSnapshot.Revision;
                            command.ExpectedGameplayRevision = lastSnapshot.GameplayRevision;
                            command.ExpectedServerInstanceId = lastSnapshot.ServerInstanceId;
                            command.HasExpectedGameplayRevision = true;
                        }
                        var request = NewRequest("COMMAND");
                        request.Command = command;
                        var response = Exchange(request);
                        CaptureSnapshot(response.Snapshot ?? response.CommandResult?.Snapshot);
                        if (response.CommandResult != null) return response.CommandResult;
                        return Reject(response.Code ?? "NO_RESULT", response.Message ?? "Server returned no command result.");
                    }
                    catch (Exception ex)
                    {
                        lastError = ex.GetBaseException().Message;
                        CloseSocket();
                    }
                }
                return Reject("TRANSPORT_ERROR", lastError ?? "Remote transport failed after reconnect retry.");
            }
        }

        public Task<AscendanceNetworkResponse0102> RequestSnapshotAsync() => Task.Run(RequestSnapshot);

        public AscendanceNetworkResponse0102 RequestSnapshot()
        {
            lock (sync)
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        if (disposed || (!IsConnected && !TryConnect(out _))) return null;
                        var response = Exchange(NewRequest("SNAPSHOT"));
                        CaptureSnapshot(response.Snapshot);
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

        private void EnsurePolling()
        {
            if (disposed || Interlocked.CompareExchange(ref pollingStarted, 1, 0) != 0) return;
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!lifetime.IsCancellationRequested)
                    {
                        await Task.Delay(500, lifetime.Token).ConfigureAwait(false);
                        RequestSnapshot();
                    }
                }
                catch (OperationCanceledException) { }
            });
        }

        private AscendanceNetworkRequest0102 NewRequest(string operation) => new()
        {
            RequestId = Guid.NewGuid().ToString("N"), Operation = operation, RoomId = roomId
        };

        private AscendanceNetworkResponse0102 Exchange(AscendanceNetworkRequest0102 request)
        {
            if (!IsConnected) throw new InvalidOperationException("TCP transport is not connected.");
            AscendanceNetworkCodec0102.WriteFrame(stream, AscendanceNetworkCodec0102.SerializeRequest(request));
            var response = AscendanceNetworkCodec0102.DeserializeResponse(AscendanceNetworkCodec0102.ReadFrame(stream));
            if (response == null) throw new InvalidOperationException("Server returned an unreadable response.");
            if (!string.Equals(response.RequestId, request.RequestId, StringComparison.Ordinal))
                throw new InvalidOperationException("Server response does not match the pending request.");
            if (response.ProtocolVersion != AscendanceNetworkCodec0102.ProtocolVersion)
                throw new InvalidOperationException("Server protocol does not match this client.");
            if (!string.Equals(response.ServerBuildVersion, AscendanceGameManager.BuildVersion, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Server build {response.ServerBuildVersion} does not match client build {AscendanceGameManager.BuildVersion}.");
            return response;
        }

        private void CaptureSnapshot(ServerStateSnapshot0100 snapshot)
        {
            if (snapshot == null || !string.Equals(snapshot.RoomId, roomId, StringComparison.OrdinalIgnoreCase)) return;
            var previous = lastSnapshot;
            bool sameInstance = previous != null && string.Equals(previous.ServerInstanceId, snapshot.ServerInstanceId, StringComparison.Ordinal);
            if (sameInstance && snapshot.Revision < previous.Revision) return;
            lastSnapshot = snapshot;
            Interlocked.Exchange(ref revision, snapshot.Revision);
            if (!sameInstance || snapshot.Revision != previous.Revision) SnapshotAvailable?.Invoke(snapshot);
        }

        private string ReconnectKey(string requestedHeroId) => $"ASCENDANCE_0102_RECONNECT_{host}_{port}_{roomId}_{requestedHeroId}";
        private ServerCommandResult0100 Reject(string code, string message) => new()
        {
            Accepted = false, Code = code, Message = message, Revision = Revision, Snapshot = lastSnapshot
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
        }

        public void Dispose()
        {
            disposed = true;
            lifetime.Cancel();
            // Interrupt worker I/O without waiting for its lock from Unity's OnDestroy.
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            connected = false;
        }
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
            // The router consumes the initial snapshot on Unity's thread before remote input.
            _ = remote.ConnectAsync();
            return true;
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
