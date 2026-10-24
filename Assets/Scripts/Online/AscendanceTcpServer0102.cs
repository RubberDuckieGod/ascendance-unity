using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Ascendance.Online
{
    /// <summary>
    /// Real 0.10.2 dedicated-server transport. Socket I/O is handled on background threads;
    /// every room-authority/gameplay operation is marshalled back to Unity's main thread.
    /// </summary>
    public sealed class AscendanceTcpServer0102 : MonoBehaviour
    {
        private sealed class ConnectionContext
        {
            public readonly object Sync = new();
            public readonly HashSet<string> PlayerIds = new(StringComparer.OrdinalIgnoreCase);
            public TcpClient Client;
            public volatile bool Closed;
        }

        private sealed class PendingRequest
        {
            public string Json;
            public ConnectionContext Connection;
            public readonly ManualResetEventSlim Completed = new(false);
            public string ResponseJson;
        }

        public static AscendanceTcpServer0102 Instance { get; private set; }

        private readonly ConcurrentQueue<PendingRequest> pendingRequests = new();
        private readonly ConcurrentQueue<ConnectionContext> disconnectedConnections = new();
        // Accessed only by Unity's main thread. A reconnect transfers the session lease.
        private readonly Dictionary<string, ConnectionContext> playerConnections = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentBag<TcpClient> openClients = new();
        private readonly List<Thread> clientThreads = new();
        private readonly object clientThreadsLock = new();

        private AscendanceRoomAuthority0100 authority;
        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool running;

        public bool IsListening => running && listener != null;
        public long ProcessedDisconnects { get; private set; }
        public int BoundPort { get; private set; }
        public string BoundAddress => "0.0.0.0";

        private void Awake() => Instance = this;

        public bool Initialize(AscendanceServerAuthority0100 serverAuthority, int port = -1)
        {
            return Initialize(serverAuthority?.Authority, port < 0 ? AscendanceDedicatedServerBootstrap0100.RequestedPort : port);
        }

        public bool Initialize(AscendanceRoomAuthority0100 roomAuthority, int port)
        {
            if (running) return true;
            authority = roomAuthority;
            if (authority == null)
            {
                Debug.LogError("[ASCENDANCE][NETWORK] TCP server cannot start without room authority.");
                return false;
            }

            try
            {
                listener = new TcpListener(IPAddress.Any, Mathf.Clamp(port, 0, 65535));
                listener.Start(8);
                BoundPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                running = true;
                acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "Ascendance-TCP-Accept" };
                acceptThread.Start();
                Debug.Log($"[ASCENDANCE][NETWORK] TCP authority listening on 0.0.0.0:{BoundPort} room={authority.Room.RoomId} protocol={AscendanceNetworkCodec0102.ProtocolVersion}.");
                return true;
            }
            catch (Exception ex)
            {
                running = false;
                Debug.LogError("[ASCENDANCE][NETWORK] TCP server failed to start: " + ex.Message);
                return false;
            }
        }

        private void Update()
        {
            while (disconnectedConnections.TryDequeue(out var connection))
            {
                ProcessedDisconnects++;
                List<string> players;
                lock (connection.Sync) players = connection.PlayerIds.ToList();
                foreach (var playerId in players)
                    if (playerConnections.TryGetValue(playerId, out var current) && ReferenceEquals(current, connection))
                    {
                        playerConnections.Remove(playerId);
                        authority?.Disconnect(playerId);
                    }
            }

            int budget = 64;
            while (budget-- > 0 && pendingRequests.TryDequeue(out var pending))
                ProcessPending(pending);
        }

        private void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    client.NoDelay = true;
                    client.ReceiveTimeout = 0; // Persistent game sessions must not expire while a player is thinking.
                    client.SendTimeout = 35000;
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    openClients.Add(client);
                    var context = new ConnectionContext { Client = client };
                    var thread = new Thread(() => ClientLoop(context)) { IsBackground = true, Name = "Ascendance-TCP-Client" };
                    lock (clientThreadsLock) clientThreads.Add(thread);
                    thread.Start();
                }
                catch (SocketException)
                {
                    if (!running) break;
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (running) Debug.LogWarning("[ASCENDANCE][NETWORK] Accept loop warning: " + ex.Message);
                }
            }
        }

        private void ClientLoop(ConnectionContext context)
        {
            try
            {
                using var client = context.Client;
                using NetworkStream stream = client.GetStream();
                while (running && client.Connected)
                {
                    string json = AscendanceNetworkCodec0102.ReadFrame(stream);
                    var pending = new PendingRequest { Json = json, Connection = context };
                    pendingRequests.Enqueue(pending);
                    if (!pending.Completed.Wait(TimeSpan.FromSeconds(30)))
                    {
                        var timeout = RejectNetwork(null, "SERVER_TIMEOUT", "Server main thread did not process the request in time.");
                        AscendanceNetworkCodec0102.WriteFrame(stream, AscendanceNetworkCodec0102.SerializeResponse(timeout));
                        break;
                    }
                    AscendanceNetworkCodec0102.WriteFrame(stream, pending.ResponseJson ?? AscendanceNetworkCodec0102.SerializeResponse(RejectNetwork(null, "EMPTY_RESPONSE", "Server produced no response.")));
                }
            }
            catch (EndOfStreamException) { }
            catch (IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                if (running) Debug.LogWarning("[ASCENDANCE][NETWORK] Client connection warning: " + ex.Message);
            }
            finally
            {
                context.Closed = true;
                disconnectedConnections.Enqueue(context);
            }
        }

        private void ProcessPending(PendingRequest pending)
        {
            try
            {
                AscendanceNetworkRequest0102 request = null;
                try { request = AscendanceNetworkCodec0102.DeserializeRequest(pending.Json); }
                catch (Exception ex)
                {
                    pending.ResponseJson = AscendanceNetworkCodec0102.SerializeResponse(RejectNetwork(null, "BAD_JSON", ex.Message));
                    return;
                }

                var response = pending.Connection.Closed
                    ? RejectNetwork(request, "CONNECTION_CLOSED", "Connection closed before its request was processed.")
                    : HandleRequest(request, pending.Connection);
                pending.ResponseJson = AscendanceNetworkCodec0102.SerializeResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                pending.ResponseJson = AscendanceNetworkCodec0102.SerializeResponse(RejectNetwork(null, "SERVER_ERROR", "Server could not process the request."));
            }
            finally { pending.Completed.Set(); }
        }

        private AscendanceNetworkResponse0102 HandleRequest(AscendanceNetworkRequest0102 request, ConnectionContext connection)
        {
            if (request == null) return RejectNetwork(null, "BAD_REQUEST", "Request payload is missing.");
            if (request.ProtocolVersion != AscendanceNetworkCodec0102.ProtocolVersion)
                return RejectNetwork(request, "PROTOCOL_MISMATCH", $"Client protocol {request.ProtocolVersion} does not match server protocol {AscendanceNetworkCodec0102.ProtocolVersion}.");
            if (!string.Equals(request.RoomId, authority.Room.RoomId, StringComparison.OrdinalIgnoreCase))
                return RejectNetwork(request, "ROOM_MISMATCH", "Request targets a different server room.");

            string op = (request.Operation ?? string.Empty).Trim().ToUpperInvariant();
            switch (op)
            {
                case "HELLO":
                case "SNAPSHOT":
                    return AcceptNetwork(request, op == "HELLO" ? "HELLO" : "SNAPSHOT", op == "HELLO" ? "Connected to Ascendance authoritative server." : "Authoritative snapshot returned.", authority.Snapshot());

                case "JOIN":
                {
                    var join = authority.Join(request.DisplayName, request.HeroId, request.ReconnectToken);
                    if (join.Accepted && join.Player != null) TrackPlayer(connection, join.Player.PlayerId);
                    return new AscendanceNetworkResponse0102
                    {
                        RequestId = request.RequestId,
                        Accepted = join.Accepted,
                        Code = join.Code,
                        Message = join.Message,
                        ServerBuildVersion = AscendanceGameManager.BuildVersion,
                        Player = join.Player,
                        Snapshot = join.Snapshot
                    };
                }

                case "RECONNECT":
                {
                    var join = authority.Reconnect(request.ReconnectToken, request.HeroId);
                    if (join.Accepted && join.Player != null) TrackPlayer(connection, join.Player.PlayerId);
                    return new AscendanceNetworkResponse0102
                    {
                        RequestId = request.RequestId,
                        Accepted = join.Accepted,
                        Code = join.Code,
                        Message = join.Message,
                        ServerBuildVersion = AscendanceGameManager.BuildVersion,
                        Player = join.Player,
                        Snapshot = join.Snapshot
                    };
                }

                case "COMMAND":
                {
                    if (request.Command == null) return RejectNetwork(request, "BAD_COMMAND", "COMMAND request contains no command envelope.");
                    if (string.IsNullOrWhiteSpace(request.Command.PlayerId) || !playerConnections.TryGetValue(request.Command.PlayerId, out var owner) || !ReferenceEquals(owner, connection))
                        return RejectNetwork(request, "PLAYER_NOT_BOUND", "JOIN or RECONNECT on this connection before issuing commands for its Hero.");
                    var result = authority.Execute(request.Command);
                    return new AscendanceNetworkResponse0102
                    {
                        RequestId = request.RequestId,
                        Accepted = result != null && result.Accepted,
                        Code = result?.Code ?? "NO_RESULT",
                        Message = result?.Message ?? "Authority returned no command result.",
                        ServerBuildVersion = AscendanceGameManager.BuildVersion,
                        CommandResult = result,
                        Snapshot = result?.Snapshot ?? authority.Snapshot()
                    };
                }

                case "PING":
                    return AcceptNetwork(request, "PONG", "Ascendance server is reachable.", authority.Snapshot());
                default:
                    return RejectNetwork(request, "UNKNOWN_OPERATION", "Unsupported network operation: " + op);
            }
        }

        private void TrackPlayer(ConnectionContext connection, string playerId)
        {
            if (connection == null || string.IsNullOrWhiteSpace(playerId)) return;
            if (playerConnections.TryGetValue(playerId, out var previous) && !ReferenceEquals(previous, connection))
            {
                lock (previous.Sync) previous.PlayerIds.Remove(playerId);
                // Revoke only this session; another Hero may share the older connection.
            }
            playerConnections[playerId] = connection;
            lock (connection.Sync) connection.PlayerIds.Add(playerId);
        }

        private static AscendanceNetworkResponse0102 AcceptNetwork(AscendanceNetworkRequest0102 request, string code, string message, ServerStateSnapshot0100 snapshot) => new()
        {
            RequestId = request?.RequestId,
            Accepted = true,
            Code = code,
            Message = message,
            ServerBuildVersion = AscendanceGameManager.BuildVersion,
            Snapshot = snapshot
        };

        private static AscendanceNetworkResponse0102 RejectNetwork(AscendanceNetworkRequest0102 request, string code, string message) => new()
        {
            RequestId = request?.RequestId,
            Accepted = false,
            Code = code,
            Message = message,
            ServerBuildVersion = AscendanceGameManager.BuildVersion
        };

        public void StopServer()
        {
            if (!running) return;
            running = false;
            try { listener?.Stop(); } catch { }
            while (openClients.TryTake(out var client)) { try { client?.Close(); } catch { } }
            try { acceptThread?.Join(500); } catch { }
            lock (clientThreadsLock)
            {
                foreach (var thread in clientThreads) { try { thread?.Join(100); } catch { } }
                clientThreads.Clear();
            }
            listener = null;
            playerConnections.Clear();
            BoundPort = 0;
        }

        private void OnDestroy()
        {
            StopServer();
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }
}
