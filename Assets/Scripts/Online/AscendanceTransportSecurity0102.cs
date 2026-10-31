using System;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Ascendance.Online
{
    /// <summary>Explicit opt-in TLS settings. Local fixtures retain the original plain transport.</summary>
    public sealed class AscendanceClientTransportSecurity0102
    {
        public bool Enabled { get; }
        public string CertificateSha256 { get; }
        public string RoomAccessKey { get; }

        public AscendanceClientTransportSecurity0102(bool enabled = false, string certificateSha256 = null, string roomAccessKey = null)
        {
            Enabled = enabled;
            CertificateSha256 = certificateSha256?.Trim() ?? string.Empty;
            RoomAccessKey = roomAccessKey?.Trim() ?? string.Empty;
        }

        internal Stream Open(Stream network, string host)
        {
            if (!Enabled)
            {
                if (CertificateSha256.Length != 0 || RoomAccessKey.Length != 0)
                    throw new AuthenticationException("Enable TLS before supplying a certificate pin or room access key.");
                return network;
            }
            byte[] pin = AscendanceTransportSecurity0102.ParseHex64(CertificateSha256, "A valid SHA-256 server certificate pin is required.", true);
            AscendanceTransportSecurity0102.ParseHex64(RoomAccessKey, "A 64-character hexadecimal room access key is required.");
            string failure = null;
            var secured = new SslStream(network, false, (_, certificate, _, errors) =>
            {
                if (certificate == null || (errors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
                { failure = "The server did not provide a TLS certificate."; return false; }
                using var leaf = new X509Certificate2(certificate);
                DateTime now = DateTime.UtcNow;
                if (now < leaf.NotBefore.ToUniversalTime() || now > leaf.NotAfter.ToUniversalTime())
                { failure = "The server TLS certificate is outside its validity period."; return false; }
                if ((errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                { failure = "The server TLS certificate does not match the requested hostname."; return false; }
                using var sha = SHA256.Create();
                if (!AscendanceTransportSecurity0102.EqualBytes(pin, sha.ComputeHash(leaf.RawData)))
                { failure = "Server certificate pin did not match."; return false; }
                // The exact pinned leaf is the trust anchor, including a self-signed leaf.
                return true;
            });
            try
            {
                secured.ReadTimeout = 5000;
                secured.WriteTimeout = 5000;
                secured.AuthenticateAsClient(host, null, SslProtocols.Tls12, false);
                return secured;
            }
            catch
            {
                secured.Dispose();
                throw new AuthenticationException(failure ?? "TLS handshake failed; verify the secure server endpoint.");
            }
        }
    }

    internal sealed class AscendanceServerTransportSecurity0102 : IDisposable
    {
        private readonly X509Certificate2 certificate;
        private readonly byte[] accessKey;
        public bool Enabled => certificate != null;

        private AscendanceServerTransportSecurity0102(X509Certificate2 certificate = null, byte[] accessKey = null)
        { this.certificate = certificate; this.accessKey = accessKey; }

        internal static AscendanceServerTransportSecurity0102 FromEnvironment()
        {
            string pfx = Environment.GetEnvironmentVariable("ASCENDANCE_TLS_PFX_PATH");
            string passwordFile = Environment.GetEnvironmentVariable("ASCENDANCE_TLS_PFX_PASSWORD_FILE");
            string keyFile = Environment.GetEnvironmentVariable("ASCENDANCE_ROOM_ACCESS_KEY_FILE");
            bool any = !string.IsNullOrWhiteSpace(pfx) || !string.IsNullOrWhiteSpace(passwordFile) || !string.IsNullOrWhiteSpace(keyFile);
            if (!any) return new AscendanceServerTransportSecurity0102();
            if (string.IsNullOrWhiteSpace(pfx) || string.IsNullOrWhiteSpace(passwordFile) || string.IsNullOrWhiteSpace(keyFile))
                throw new InvalidOperationException("Secure server configuration requires the PFX, PFX password file and room access key file.");
            string keyText = ReadPrivateFile(keyFile);
            AscendanceTransportSecurity0102.ParseHex64(keyText, "Server room access key file must contain exactly 64 hexadecimal characters.");
            byte[] key = Encoding.UTF8.GetBytes(keyText);
            string password = ReadPrivateFile(passwordFile);
            if (password.Length == 0) throw new InvalidOperationException("Server TLS password file is empty.");
            if (!Path.IsPathRooted(pfx) || (File.GetAttributes(pfx) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Server TLS certificate must be a regular file.");
            X509Certificate2 leaf = null;
            try
            {
                leaf = new X509Certificate2(pfx, password, X509KeyStorageFlags.EphemeralKeySet);
                if (!leaf.HasPrivateKey) throw new InvalidOperationException("Server TLS certificate has no private key.");
                DateTime now = DateTime.UtcNow;
                if (now < leaf.NotBefore.ToUniversalTime() || now > leaf.NotAfter.ToUniversalTime())
                    throw new InvalidOperationException("Server TLS certificate is outside its validity period.");
                return new AscendanceServerTransportSecurity0102(leaf, key);
            }
            catch { leaf?.Dispose(); throw; }
        }

        private static string ReadPrivateFile(string path)
        {
            if (!Path.IsPathRooted(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Server security files require absolute regular-file paths.");
            return File.ReadAllText(path).Trim();
        }

        internal bool Authorizes(string supplied)
        {
            if (!Enabled) return true;
            try
            {
                string candidate = (supplied ?? string.Empty).Trim();
                AscendanceTransportSecurity0102.ParseHex64(candidate, "Invalid room access key.");
                return AscendanceTransportSecurity0102.EqualBytes(accessKey, Encoding.UTF8.GetBytes(candidate));
            }
            catch (AuthenticationException) { return false; }
        }

        internal Stream Open(Stream network)
        {
            if (!Enabled) return network;
            var secured = new SslStream(network, false);
            try
            {
                secured.ReadTimeout = 5000;
                secured.WriteTimeout = 5000;
                secured.AuthenticateAsServer(certificate, false, SslProtocols.Tls12, false);
                // The first authenticated request is bounded too. The server restores
                // an unlimited idle read only after the room access key is accepted.
                secured.ReadTimeout = 5000;
                secured.WriteTimeout = 35000;
                return secured;
            }
            catch { secured.Dispose(); throw; }
        }

        public void Dispose() => certificate?.Dispose();
    }

    internal static class AscendanceTransportSecurity0102
    {
        internal static byte[] ParseHex64(string value, string message, bool allowColons = false)
        {
            string text = (value ?? string.Empty).Trim();
            if (allowColons) text = text.Replace(":", string.Empty);
            if (text.Length != 64) throw new AuthenticationException(message);
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
                if (!byte.TryParse(text.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out bytes[i]))
                    throw new AuthenticationException(message);
            return bytes;
        }

        internal static bool EqualBytes(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
