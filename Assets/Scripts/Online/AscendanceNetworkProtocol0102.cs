using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ascendance.Online
{
    [Serializable]
    public sealed class AscendanceNetworkRequest0102
    {
        public int ProtocolVersion = AscendanceNetworkCodec0102.ProtocolVersion;
        public string RequestId;
        public string Operation;
        public string RoomId;
        public string DisplayName;
        public string HeroId;
        public string ReconnectToken;
        public ServerCommandEnvelope0100 Command;
    }

    [Serializable]
    public sealed class AscendanceNetworkResponse0102
    {
        public int ProtocolVersion = AscendanceNetworkCodec0102.ProtocolVersion;
        public string RequestId;
        public bool Accepted;
        public string Code;
        public string Message;
        public string ServerBuildVersion;
        public ServerPlayerSession0100 Player;
        public ServerCommandResult0100 CommandResult;
        public ServerStateSnapshot0100 Snapshot;
    }

    /// <summary>
    /// 0.10.2 wire contract. Messages are UTF-8 JSON framed with a four-byte network-order
    /// length prefix. Framing keeps the protocol transport-agnostic above TCP and avoids relying
    /// on newline delimiters inside JSON payloads.
    /// </summary>
    public static class AscendanceNetworkCodec0102
    {
        public const int ProtocolVersion = 1;
        public const int MaxFrameBytes = 8 * 1024 * 1024;

        public static string SerializeRequest(AscendanceNetworkRequest0102 request) => JsonUtility.ToJson(request ?? new AscendanceNetworkRequest0102());
        public static AscendanceNetworkRequest0102 DeserializeRequest(string json) => string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<AscendanceNetworkRequest0102>(json);
        public static string SerializeResponse(AscendanceNetworkResponse0102 response) => JsonUtility.ToJson(response ?? new AscendanceNetworkResponse0102());
        public static AscendanceNetworkResponse0102 DeserializeResponse(string json) => string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<AscendanceNetworkResponse0102>(json);

        public static void WriteFrame(Stream stream, string payload)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            byte[] body = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            if (body.Length > MaxFrameBytes) throw new InvalidDataException($"Ascendance network frame exceeds {MaxFrameBytes} bytes.");
            int networkLength = System.Net.IPAddress.HostToNetworkOrder(body.Length);
            byte[] header = BitConverter.GetBytes(networkLength);
            stream.Write(header, 0, header.Length);
            if (body.Length > 0) stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        public static string ReadFrame(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            byte[] header = ReadExact(stream, 4);
            int length = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
            if (length < 0 || length > MaxFrameBytes) throw new InvalidDataException($"Invalid Ascendance network frame length: {length}.");
            if (length == 0) return string.Empty;
            return Encoding.UTF8.GetString(ReadExact(stream, length));
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) throw new EndOfStreamException("Ascendance network peer closed the connection.");
                offset += read;
            }
            return buffer;
        }
    }
}
