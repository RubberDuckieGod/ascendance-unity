using System;
using System.Collections.Concurrent;
using System.IO;
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

    /// <summary>Public lobby outcome deliberately excludes reconnect credentials.</summary>
    public sealed class AscendanceHeroClaimResult0102
    {
        public bool Accepted;
        public string Code;
        public string Message;
        public string HeroId;
        public ServerStateSnapshot0100 Snapshot;
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
        private readonly AscendanceClientTransportSecurity0102 security;
        private readonly object sync = new();
        private readonly ConcurrentDictionary<string, string> credentials = new(StringComparer.OrdinalIgnoreCase);
        private readonly CancellationTokenSource lifetime = new();
        private int pollingStarted;
        private volatile bool disposed;
        private TcpClient client;
        private Stream stream;
        private volatile bool connected;
        private long revision;
        private string playerId;
        private string heroId;
        private ServerStateSnapshot0100 lastSnapshot;
        private volatile string lastConnectionError = string.Empty;

        public event Action<ServerStateSnapshot0100> SnapshotAvailable;
        public bool IsConnected => connected && !disposed;
        public string RoomId => roomId;
        public long Revision => Interlocked.Read(ref revision);
        public string Endpoint => host + ":" + port;
        public string PlayerId => playerId;
        public string HeroId => heroId;
        public ServerStateSnapshot0100 LastSnapshot => lastSnapshot;
        public ServerStateSnapshot0100 LatestSnapshot => lastSnapshot;
        public string LastConnectionError => lastConnectionError;

        public AscendanceTcpCommandTransport0102(string hostName, int serverPort, string requestedRoomId, string playerDisplayName, AscendanceClientTransportSecurity0102 transportSecurity = null)
        {
            host = string.IsNullOrWhiteSpace(hostName) ? "127.0.0.1" : hostName.Trim();
            port = Math.Max(1, Math.Min(65535, serverPort <= 0 ? 7777 : serverPort));
            roomId = string.IsNullOrWhiteSpace(requestedRoomId) ? "LOCAL-ROOM" : requestedRoomId.Trim();
            displayName = string.IsNullOrWhiteSpace(playerDisplayName) ? "Player" : playerDisplayName.Trim();
            security = transportSecurity ?? new AscendanceClientTransportSecurity0102();
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
                if (disposed) { message = lastConnectionError = "Transport is disposed."; return false; }
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
                    stream = security.Open(client.GetStream(), host);
                    if (security.Enabled) { stream.ReadTimeout = IoTimeoutMs; stream.WriteTimeout = IoTimeoutMs; }
                    connected = true;
                    var response = Exchange(NewRequest("HELLO"));
                    if (!response.Accepted)
                    {
                        message = lastConnectionError = response.Message;
                        CloseSocket();
                        return false;
                    }
                    CaptureSnapshot(response.Snapshot);
                    EnsurePolling();
                    lastConnectionError = string.Empty;
                    message = $"Connected to {Endpoint} room {roomId} at revision {Revision}.";
                    return true;
                }
                catch (Exception ex)
                {
                    message = lastConnectionError = ex.GetBaseException().Message;
                    CloseSocket();
                    return false;
                }
            }
        }

        // Main-thread only. Persist before JOIN: a lost reply can recover the same claim.
        private void PrepareHeroCredential(string requestedHeroId)
        {
            if (string.IsNullOrWhiteSpace(requestedHeroId) || credentials.ContainsKey(requestedHeroId)) return;
            if (!string.IsNullOrWhiteSpace(heroId) && !string.Equals(heroId, requestedHeroId, StringComparison.OrdinalIgnoreCase)) return;
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

        // Main-thread only. This is an availability hint, not proof of ownership:
        // the server must verify a saved credential again when the user claims.
        public bool HasSavedHeroCredential(string requestedHeroId)
        {
            if (string.IsNullOrWhiteSpace(requestedHeroId)) return false;
            if (credentials.TryGetValue(requestedHeroId, out var prepared))
                return Guid.TryParseExact(prepared, "N", out _);
            return Guid.TryParseExact(PlayerPrefs.GetString(ReconnectKey(requestedHeroId), string.Empty), "N", out _);
        }

        // Prepare/persist the credential on Unity's thread before dispatching I/O.
        // Neither gameplay nor a new campaign is needed to acquire the Hero.
        public Task<AscendanceHeroClaimResult0102> ClaimHeroAsync(string requestedHeroId)
        {
            PrepareHeroCredential(requestedHeroId);
            return Task.Run(() =>
            {
                lock (sync)
                {
                    TryResolvePrepared(requestedHeroId, out _, out var result);
                    return result;
                }
            });
        }

        public bool TryResolvePlayerForHero(string requestedHeroId, out string resolvedPlayerId)
        {
            PrepareHeroCredential(requestedHeroId);
            lock (sync) return TryResolvePrepared(requestedHeroId, out resolvedPlayerId, out _);
        }

        private bool TryResolvePrepared(string requestedHeroId, out string resolvedPlayerId, out AscendanceHeroClaimResult0102 result)
        {
            resolvedPlayerId = null;
            result = ClaimResult(false, "TRANSPORT_OFFLINE", "The server connection is not available.", requestedHeroId);
            if (disposed) return false;
            if (string.IsNullOrWhiteSpace(requestedHeroId))
            {
                result = ClaimResult(false, "HERO_REQUIRED", "Choose a Hero before joining the campaign.", requestedHeroId);
                return false;
            }
            // Keep the assigned Hero across socket reconnects. Another active
            // Hero's turn must never silently change this connection's owner.
            if (!string.IsNullOrWhiteSpace(heroId) && !string.Equals(heroId, requestedHeroId, StringComparison.OrdinalIgnoreCase))
            {
                result = ClaimResult(false, "HERO_OWNERSHIP", "This player session already belongs to another Hero.", requestedHeroId);
                return false;
            }
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    if (!IsConnected && !TryConnect(out var connectionMessage))
                    {
                        result = ClaimResult(false, "TRANSPORT_OFFLINE", connectionMessage, requestedHeroId);
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(playerId))
                    {
                        resolvedPlayerId = playerId;
                        result = ClaimResult(true, "JOINED", "This Hero is ready to enter the campaign.", heroId);
                        return true;
                    }
                    if (!credentials.TryGetValue(requestedHeroId, out var token))
                    {
                        result = ClaimResult(false, "PLAYER_SESSION_REQUIRED", "The local Hero credential could not be prepared.", requestedHeroId);
                        return false;
                    }
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
                    if (!response.Accepted)
                    {
                        result = ClaimResult(false, response.Code ?? "JOIN_DENIED", response.Message ?? "The Hero could not be claimed.", requestedHeroId);
                        return false;
                    }
                    if (response.Player == null || string.IsNullOrWhiteSpace(response.Player.PlayerId) ||
                        !string.Equals(response.Player.HeroId, requestedHeroId, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(response.Player.ReconnectToken, token, StringComparison.Ordinal))
                    {
                        result = ClaimResult(false, "INVALID_HERO_SESSION", "The server did not confirm the requested Hero session.", requestedHeroId);
                        CloseSocket();
                        return false;
                    }
                    playerId = response.Player.PlayerId;
                    heroId = response.Player.HeroId;
                    resolvedPlayerId = playerId;
                    result = ClaimResult(true, response.Code ?? "JOINED", response.Message, heroId);
                    return true;
                }
                catch (Exception ex)
                {
                    lastConnectionError = ex.GetBaseException().Message;
                    result = ClaimResult(false, "TRANSPORT_ERROR", lastConnectionError, requestedHeroId);
                    CloseSocket();
                    if (attempt > 0) return false;
                }
            }
            return false;
        }

        private AscendanceHeroClaimResult0102 ClaimResult(bool accepted, string code, string message, string requestedHeroId) => new()
        {
            Accepted = accepted, Code = code, Message = message,
            HeroId = requestedHeroId, Snapshot = lastSnapshot
        };

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
                        if (!TryResolvePrepared(command.ActorHeroId, out _, out var claim))
                            return Reject(claim.Code, claim.Message);
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
                        lastError = lastConnectionError = ex.GetBaseException().Message;
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
                        if (!response.Accepted)
                        {
                            lastConnectionError = response.Message ?? "The server refused the room snapshot.";
                            CloseSocket();
                            return response;
                        }
                        lastConnectionError = string.Empty;
                        CaptureSnapshot(response.Snapshot);
                        return response;
                    }
                    catch (Exception ex)
                    {
                        lastConnectionError = ex.GetBaseException().Message;
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
            RequestId = Guid.NewGuid().ToString("N"), Operation = operation, RoomId = roomId, RoomAccessKey = security.RoomAccessKey
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
            // heroId remains the assigned identity while its socket reconnects.
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
        public static bool TlsEnabled { get; private set; }
        public static string CertificateSha256 { get; private set; } = string.Empty;
        private static string RoomAccessKey = string.Empty;
        private static AscendanceTcpCommandTransport0102 stagedTransport;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Parse()
        {
            ClearStagedTransport();
            string[] args = Environment.GetCommandLineArgs() ?? Array.Empty<string>();
            string endpoint = Environment.GetEnvironmentVariable("ASCENDANCE_SERVER_ENDPOINT");
            RoomId = FirstNonBlank(Environment.GetEnvironmentVariable("ASCENDANCE_ROOM"), "LOCAL-ROOM");
            DisplayName = FirstNonBlank(Environment.GetEnvironmentVariable("ASCENDANCE_PLAYER"), "Player");
            TlsEnabled = string.Equals(Environment.GetEnvironmentVariable("ASCENDANCE_TLS"), "1", StringComparison.OrdinalIgnoreCase) || string.Equals(Environment.GetEnvironmentVariable("ASCENDANCE_TLS"), "true", StringComparison.OrdinalIgnoreCase);
            CertificateSha256 = Environment.GetEnvironmentVariable("ASCENDANCE_CERT_SHA256")?.Trim() ?? string.Empty;
            RoomAccessKey = Environment.GetEnvironmentVariable("ASCENDANCE_ROOM_ACCESS_KEY")?.Trim() ?? string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-ascendance-connect", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) endpoint = args[++i];
                else if (string.Equals(args[i], "-room", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) RoomId = FirstNonBlank(args[++i], RoomId);
                else if (string.Equals(args[i], "-player", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) DisplayName = FirstNonBlank(args[++i], DisplayName);
                else if (string.Equals(args[i], "-ascendance-tls", StringComparison.OrdinalIgnoreCase)) TlsEnabled = true;
                else if (string.Equals(args[i], "-ascendance-cert-sha256", StringComparison.OrdinalIgnoreCase))
                {
                    TlsEnabled = true;
                    CertificateSha256 = i + 1 < args.Length ? args[++i] : string.Empty;
                }
            }

            if (CertificateSha256.Length != 0 || RoomAccessKey.Length != 0) TlsEnabled = true;
            if (string.IsNullOrWhiteSpace(endpoint)) { RemoteRequested = false; return; }
            RemoteRequested = TryParseEndpoint(endpoint, out var parsedHost, out var parsedPort);
            if (RemoteRequested)
            {
                Host = parsedHost; Port = parsedPort;
                // Remote snapshots are installed on Unity's main thread even when
                // another player's window is focused or this client is minimized.
                Application.runInBackground = true;
            }
        }

        public static bool TryInstall(AscendanceClientCommandRouter0101 router, AscendanceGameManager gm)
        {
            if (!RemoteRequested || router == null) return false;
            var remote = stagedTransport ?? CreateLobbyTransport();
            stagedTransport = null;
            router.SetTransport(remote);
            // The router consumes the initial snapshot on Unity's thread before remote input.
            _ = remote.ConnectAsync();
            return true;
        }

        public static AscendanceTcpCommandTransport0102 CreateLobbyTransport() => new(
            Host, Port, RoomId, DisplayName,
            new AscendanceClientTransportSecurity0102(TlsEnabled, CertificateSha256, RoomAccessKey));

        // Stage only a server-confirmed session. The menu passes the same socket
        // to Stonebrook so the claim cannot race another player's first action.
        public static bool StageClaimedTransport(AscendanceTcpCommandTransport0102 transport)
        {
            if (transport == null || !transport.IsConnected || string.IsNullOrWhiteSpace(transport.PlayerId) || string.IsNullOrWhiteSpace(transport.HeroId)) return false;
            var snapshot = transport.LatestSnapshot;
            if (snapshot?.Campaign?.PartyHeroIds == null || snapshot.Players == null ||
                !string.Equals(snapshot.RoomId, transport.RoomId, StringComparison.OrdinalIgnoreCase) ||
                !snapshot.Campaign.PartyHeroIds.Exists(id => string.Equals(id, transport.HeroId, StringComparison.OrdinalIgnoreCase)) ||
                !snapshot.Players.Exists(player => player != null && player.Connected &&
                    string.Equals(player.PlayerId, transport.PlayerId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(player.HeroId, transport.HeroId, StringComparison.OrdinalIgnoreCase))) return false;
            if (!ReferenceEquals(stagedTransport, transport)) ClearStagedTransport();
            stagedTransport = transport;
            return true;
        }

        public static void ClearStagedTransport()
        {
            var previous = stagedTransport;
            stagedTransport = null;
            previous?.Dispose();
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
