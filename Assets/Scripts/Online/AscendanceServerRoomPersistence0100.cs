using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ascendance.Online
{
    public static class AscendanceServerRoomPersistence0100
    {
        public static string RootPath => Path.Combine(AscendanceStoragePaths0102.PersistentDataPath, "AscendanceServerRooms0100");

        public static string PathFor(string roomId)
        {
            string safe = new string((roomId ?? "room").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (string.IsNullOrWhiteSpace(safe)) safe = "room";
            return Path.Combine(RootPath, safe + ".json");
        }

        public static bool Save(PersistedServerRoom0100 room, out string message)
        {
            if (room == null || string.IsNullOrWhiteSpace(room.RoomId)) { message = "Room is invalid."; return false; }
            try
            {
                Directory.CreateDirectory(RootPath);
                room.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
                File.WriteAllText(PathFor(room.RoomId), JsonUtility.ToJson(room, true));
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

        public static bool TryLoad(string roomId, out PersistedServerRoom0100 room, out string message)
        {
            room = null;
            string path = PathFor(roomId);
            if (!File.Exists(path)) { message = "No server room save found."; return false; }
            try
            {
                room = JsonUtility.FromJson<PersistedServerRoom0100>(File.ReadAllText(path));
                if (room == null || string.IsNullOrWhiteSpace(room.RoomId) || room.DurableCampaign == null)
                {
                    room = null; message = "Server room save is incomplete."; return false;
                }
                AscendanceOptionalSaveState0102.Normalize(room.DurableCampaign);
                room.Players ??= new System.Collections.Generic.List<ServerPlayerSession0100>();
                message = "Server room save ready.";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ASCENDANCE][SERVER] Room load rejected: " + ex.Message);
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
