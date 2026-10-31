using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ascendance.Online
{
    public static class AscendanceServerRoomPersistence0100
    {
        [DllImport("libc", SetLastError = true, EntryPoint = "chmod")]
        private static extern int Chmod0102(string path, uint mode);

        private static void ProtectUnix0102(string path, bool directory)
        {
            if (Application.platform != RuntimePlatform.LinuxPlayer && Application.platform != RuntimePlatform.LinuxEditor
                && Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor) return;
            if (Chmod0102(path, directory ? 448u : 384u) != 0) // 0700 / 0600
                throw new IOException("Private campaign storage permissions could not be established.");
        }
        public static string RootPath => Path.Combine(AscendanceStoragePaths0102.PersistentDataPath, "AscendanceServerRooms0100");

        public static string PathFor(string roomId)
        {
            string safe = new string((roomId ?? "room").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (string.IsNullOrWhiteSpace(safe)) safe = "room";
            return Path.Combine(RootPath, safe + ".json");
        }

        // Unlike File.Exists, an occupied but unreadable/non-file path must not
        // be mistaken for an absent save and replaced by a fresh campaign.
        public static bool SavePathExists(string roomId)
        {
            try { File.GetAttributes(PathFor(roomId)); return true; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }

        public static bool Save(PersistedServerRoom0100 room, out string message)
        {
            if (room == null || string.IsNullOrWhiteSpace(room.RoomId)) { message = "Room is invalid."; return false; }
            try
            {
                string path = PathFor(room.RoomId);
                if (SavePathExists(room.RoomId)
                    && (File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                {
                    message = "Server room save must be a regular file.";
                    return false;
                }
                room.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
                if (!SaveFileAtomic0102(path, JsonUtility.ToJson(room, true), out message)) return false;
                message = "Server room saved.";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                message = "Server room save failed: " + ex.Message;
                return false;
            }
        }

        public static void RequireRegularPath0102(string path)
        {
            var item = new FileInfo(Path.GetFullPath(path));
            FileSystemInfo component = item.Exists ? item : (FileSystemInfo)new DirectoryInfo(Path.GetFullPath(path));
            while (component != null)
            {
                if (component.Exists && (component.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Campaign storage refuses symbolic links.");
                component = component is FileInfo file ? file.Directory : ((DirectoryInfo)component).Parent;
            }
        }

        public static bool SaveFileAtomic0102(string path, string json, out string message)
        {
            string temporary = null;
            try
            {
                RequireRegularPath0102(path);
                if (Directory.Exists(path)) throw new IOException("Campaign storage requires a regular file.");
                string folder = Path.GetDirectoryName(Path.GetFullPath(path));
                Directory.CreateDirectory(folder);
                RequireRegularPath0102(folder);
                ProtectUnix0102(RootPath, true);
                ProtectUnix0102(folder, true);
                temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ProtectUnix0102(temporary, false);
                    byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                temporary = null;
                message = "Campaign saved atomically.";
                return true;
            }
            catch { message = "Campaign save failed. The previous file was retained."; return false; }
            finally { if (temporary != null) { try { File.Delete(temporary); } catch { } } }
        }

        public static bool TryLoad(string roomId, out PersistedServerRoom0100 room, out string message)
        {
            room = null;
            string path = PathFor(roomId);
            try
            {
                if (!SavePathExists(roomId)) { message = "No server room save found."; return false; }
                if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                {
                    message = "Server room save must be a regular file.";
                    return false;
                }
                room = JsonUtility.FromJson<PersistedServerRoom0100>(File.ReadAllText(path));
                if (room == null || string.IsNullOrWhiteSpace(room.RoomId) || room.DurableCampaign == null
                    || room.DurableCampaign.Heroes == null
                    || !room.DurableCampaign.Heroes.Any(h => h != null && !string.IsNullOrWhiteSpace(h.Id)))
                {
                    room = null; message = "Server room save is incomplete."; return false;
                }
                if (!string.Equals(room.RoomId, roomId?.Trim(), StringComparison.Ordinal))
                {
                    room = null; message = "Saved room identity differs from its requested filename."; return false;
                }
                AscendanceOptionalSaveState0102.Normalize(room.DurableCampaign);
                room.Players ??= new System.Collections.Generic.List<ServerPlayerSession0100>();
                message = "Server room save ready.";
                return true;
            }
            catch (Exception ex)
            {
                room = null;
                Debug.LogWarning("[ASCENDANCE][SERVER] Room load rejected: " + ex.GetType().Name);
                message = "Server room save could not be read.";
                return false;
            }
        }

        public static void Delete(string roomId)
        {
            try { string path = PathFor(roomId); if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}
