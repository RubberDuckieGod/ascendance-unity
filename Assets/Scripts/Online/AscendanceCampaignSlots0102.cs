using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance.Online
{
    [Serializable] public sealed class ServerCampaignSlotView0102
    {
        public string Id, Name, UpdatedAtUtc;
        public bool IsDraft, IsActive;
        public List<string> PartyHeroIds = new();
    }

    [Serializable] public sealed class ServerCampaignLobby0102
    {
        public bool Supported, IsDraft, CanCreate, CanStart;
        public string CreatorPlayerId, CampaignId, CampaignName;
        public List<string> AvailableHeroIds = new();
        public List<ServerCampaignSlotView0102> Slots = new();
    }

    public sealed class AscendanceCampaignActionResult0102
    {
        public bool Accepted;
        public string Code, Message;
        public ServerStateSnapshot0100 Snapshot;
    }

    public static class AscendanceCampaignSlots0102
    {
        public const int MaxSlots = 16;
        private static string Folder(string roomId) => Path.Combine(AscendanceServerRoomPersistence0100.RootPath,
            "CampaignSlots", Path.GetFileNameWithoutExtension(AscendanceServerRoomPersistence0100.PathFor(roomId)));
        private static string SlotPath(string roomId, string id)
        {
            if (!Regex.IsMatch(id ?? "", "^[A-Za-z0-9_-]{1,64}$")) throw new InvalidOperationException("Invalid campaign slot.");
            return Path.Combine(Folder(roomId), id + ".json");
        }
        public static string Id(PersistedServerRoom0100 room) => string.IsNullOrEmpty(room.CampaignId0102) ? "original" : room.CampaignId0102;
        public static string Name(PersistedServerRoom0100 room) => string.IsNullOrEmpty(room.CampaignName0102) ? "Original campaign" : room.CampaignName0102;

        public static bool Save(PersistedServerRoom0100 room, out string message)
        {
            try
            {
                string path = SlotPath(room.RoomId, Id(room));
                string json = JsonUtility.ToJson(room, true);
                if (!AscendanceServerRoomPersistence0100.SaveFileAtomic0102(path, json, out message)) return false;
                var loaded = Read(path, room.RoomId);
                if (loaded == null || JsonUtility.ToJson(loaded) != JsonUtility.ToJson(room))
                    throw new IOException("Campaign archive verification failed.");
                message = "Campaign preserved in its saved slot.";
                return true;
            }
            catch (Exception) { message = "Campaign could not be safely archived. The active campaign was not replaced."; return false; }
        }

        public static PersistedServerRoom0100 Load(string roomId, string id) => Read(SlotPath(roomId, id), roomId);

        private static PersistedServerRoom0100 Read(string path, string roomId)
        {
            if (!File.Exists(path)) throw new IOException("Saved campaign is unavailable.");
            AscendanceServerRoomPersistence0100.RequireRegularPath0102(path);
            var room = JsonUtility.FromJson<PersistedServerRoom0100>(File.ReadAllText(path));
            if (room == null || room.RoomId != roomId || room.DurableCampaign?.Heroes == null || room.DurableCampaign.Heroes.Count == 0)
                throw new IOException("Saved campaign is incomplete.");
            AscendanceOptionalSaveState0102.Normalize(room.DurableCampaign);
            room.Players ??= new List<ServerPlayerSession0100>();
            return room;
        }

        public static List<ServerCampaignSlotView0102> List(PersistedServerRoom0100 active)
        {
            var result = new List<ServerCampaignSlotView0102>();
            string folder = Folder(active.RoomId);
            if (Directory.Exists(folder))
            {
                AscendanceServerRoomPersistence0100.RequireRegularPath0102(folder);
                foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(x => x).Take(MaxSlots))
                {
                    try { result.Add(View(Read(path, active.RoomId), Id(active))); }
                    catch { /* Corrupt archives are never advertised or loaded as fresh campaigns. */ }
                }
            }
            result.RemoveAll(x => x.Id == Id(active));
            result.Insert(0, View(active, Id(active)));
            return result;
        }

        private static ServerCampaignSlotView0102 View(PersistedServerRoom0100 room, string activeId) => new()
        {
            Id = Id(room), Name = Name(room), UpdatedAtUtc = room.UpdatedAtUtc,
            IsDraft = room.CampaignDraft0102, IsActive = Id(room) == activeId,
            PartyHeroIds = room.DurableCampaign?.PartyHeroIds?.ToList() ?? new List<string>()
        };
    }

    public sealed partial class AscendanceRoomAuthority0100
    {
        public bool IsCampaignDraft0102 => Room.CampaignDraft0102;
        private bool OwnerMatches0102(string token) => Guid.TryParseExact(token, "N", out _)
            && string.Equals(token, Room.CampaignOwnerToken0102, StringComparison.Ordinal);
        private bool SafeCampaignBoundary0102 => AscendanceSaveSystem.CanSave(gm);
        private bool OtherConnectedPlayers0102(IEnumerable<string> ownIds) => Room.Players.Any(p => p != null && p.Connected
            && !(ownIds ?? Array.Empty<string>()).Contains(p.PlayerId, StringComparer.OrdinalIgnoreCase));
        private bool MayManage0102(string ownerToken, IEnumerable<string> ownIds) => SafeCampaignBoundary0102
            && !OtherConnectedPlayers0102(ownIds)
            && (string.IsNullOrEmpty(Room.CampaignOwnerToken0102) || OwnerMatches0102(ownerToken));

        public ServerCampaignLobby0102 CampaignLobbySnapshot0102(string ownerToken, IEnumerable<string> ownIds)
        {
            var own = (ownIds ?? Array.Empty<string>()).ToList();
            return new ServerCampaignLobby0102
            {
                Supported = true,
                IsDraft = Room.CampaignDraft0102,
                CampaignId = AscendanceCampaignSlots0102.Id(Room), CampaignName = AscendanceCampaignSlots0102.Name(Room),
                CreatorPlayerId = Room.CampaignCreatorPlayerId0102,
                CanCreate = MayManage0102(ownerToken, own),
                CanStart = Room.CampaignDraft0102 && OwnerMatches0102(ownerToken)
                    && own.Contains(Room.CampaignCreatorPlayerId0102) && Room.Players.Count >= 1 && Room.Players.Count <= 4
                    && Room.Players.All(p => p != null && p.Connected && p.ReadyForCampaign0102),
                AvailableHeroIds = Room.CampaignDraft0102 ? HeroDatabase.Heroes.OrderBy(h => h.heroCardId).Select(h => h.id).ToList() : gm.PartyHeroIds,
                Slots = AscendanceCampaignSlots0102.List(Room)
            };
        }

        public void IdentifyCampaignCreator0102(string ownerToken, string playerId)
        {
            if (!Room.CampaignDraft0102 || !OwnerMatches0102(ownerToken) || string.IsNullOrEmpty(playerId)) return;
            if (Room.CampaignCreatorPlayerId0102 == playerId) return;
            Room.CampaignCreatorPlayerId0102 = playerId;
            CommitRoomMetadata();
        }

        public ServerJoinResult0100 ChangeDraftHero0102(string playerId, string heroId, string credential)
        {
            if (!Room.CampaignDraft0102) return JoinReject("PARTY_LOCKED", "Hero changes are only available before starting a new campaign.");
            var player = FindPlayer(playerId);
            if (player == null || !player.Connected) return JoinReject("PLAYER_NOT_CONNECTED", "Join this draft first.");
            if (!gm.Heroes.ContainsKey(heroId ?? "") || !Guid.TryParseExact(credential, "N", out _))
                return JoinReject("HERO_UNAVAILABLE", "Choose an authored Hero with a valid local credential.");
            if (Room.Players.Any(p => p != null && p.PlayerId != playerId && string.Equals(p.HeroId, heroId, StringComparison.OrdinalIgnoreCase)))
                return JoinReject("HERO_CLAIMED", "That Hero already belongs to another player in this lobby.");
            if (Room.Players.Any(p => p != null && p.PlayerId != playerId && p.ReconnectToken == credential))
                return JoinReject("BAD_RECONNECT_TOKEN", "This credential belongs to another player.");
            player.HeroId = gm.Heroes[heroId].Id; player.ReconnectToken = credential; player.ReadyForCampaign0102 = false;
            CommitRoomMetadata();
            return new ServerJoinResult0100 { Accepted = true, Code = "HERO_CHANGED", Message = "Your new Hero selection is confirmed.", Player = ClonePlayer(player), Snapshot = Snapshot() };
        }

        public AscendanceCampaignActionResult0102 SetDraftReady0102(string playerId, bool ready)
        {
            var player = FindPlayer(playerId);
            if (!Room.CampaignDraft0102 || player == null || !player.Connected) return CampaignResult0102(false, "DRAFT_REQUIRED", "Join a Hero in the new campaign lobby first.");
            player.ReadyForCampaign0102 = ready; CommitRoomMetadata();
            return CampaignResult0102(true, "READY_CHANGED", ready ? "Ready for the campaign creator to start." : "Readiness cleared.");
        }

        public AscendanceCampaignActionResult0102 LeaveDraft0102(string playerId)
        {
            if (!Room.CampaignDraft0102 || !Room.Players.Any(p => p != null && p.PlayerId == playerId))
                return CampaignResult0102(false, "DRAFT_REQUIRED", "Only a player in the unstarted lobby may leave its draft.");
            Room.Players.RemoveAll(p => p != null && p.PlayerId == playerId);
            if (Room.CampaignCreatorPlayerId0102 == playerId) Room.CampaignCreatorPlayerId0102 = null;
            CommitRoomMetadata();
            return CampaignResult0102(true, "DRAFT_LEFT", "Your draft Hero is available to other players again.");
        }

        public AscendanceCampaignActionResult0102 CreateCampaign0102(string ownerToken, string actionId, IEnumerable<string> ownIds)
        {
            if (!Guid.TryParseExact(ownerToken, "N", out _) || !Guid.TryParseExact(actionId, "N", out _))
                return CampaignResult0102(false, "CAMPAIGN_CREDENTIAL_REQUIRED", "A private creator credential and operation identity are required.");
            if (OwnerMatches0102(ownerToken) && Room.LastCampaignActionId0102 == actionId && Room.LastCampaignActionCode0102 == "CAMPAIGN_CREATED")
                return CampaignResult0102(true, "CAMPAIGN_CREATED", "This new campaign request was already applied.");
            if (!MayManage0102(ownerToken, ownIds)) return CampaignResult0102(false, "CAMPAIGN_SWITCH_BLOCKED", "Only this campaign's creator can switch campaigns. Other players must return to the menu, and pending gameplay must finish first.");
            if (AscendanceCampaignSlots0102.List(Room).Count >= AscendanceCampaignSlots0102.MaxSlots)
                return CampaignResult0102(false, "CAMPAIGN_SLOTS_FULL", "All saved campaign slots are occupied. No campaign was replaced.");
            try
            {
                var fresh = AscendanceGameManager.CreateFreshOnlineCampaign0102(HeroDatabase.DefaultParty);
                var candidate = new PersistedServerRoom0100
                {
                    RoomId = Room.RoomId, BuildVersion = AscendanceGameManager.BuildVersion,
                    Revision = Room.Revision + 1, GameplayRevision = Room.GameplayRevision + 1,
                    CreatedAtUtc = DateTime.UtcNow.ToString("O"), UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
                    CampaignId0102 = "campaign-" + Guid.NewGuid().ToString("N"),
                    CampaignName0102 = "New campaign " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                    CampaignDraft0102 = true, CampaignOwnerToken0102 = ownerToken,
                    LastCampaignActionId0102 = actionId, LastCampaignActionCode0102 = "CAMPAIGN_CREATED", DurableCampaign = fresh
                };
                return ActivateCampaign0102(candidate, ownerToken, "CAMPAIGN_CREATED", "New campaign lobby created. Every player may choose one of all 16 Heroes.");
            }
            catch { return CampaignResult0102(false, "CAMPAIGN_CREATE_FAILED", "The new campaign could not be prepared. Your current campaign remains active."); }
        }

        public AscendanceCampaignActionResult0102 LoadCampaign0102(string ownerToken, string actionId, string slotId, IEnumerable<string> ownIds)
        {
            if (!Guid.TryParseExact(actionId, "N", out _) || !OwnerMatches0102(ownerToken) || !MayManage0102(ownerToken, ownIds))
                return CampaignResult0102(false, "CAMPAIGN_SWITCH_BLOCKED", "The creator must reconnect on this computer. Other players must return to the menu before loading a saved campaign.");
            if (Room.LastCampaignActionId0102 == actionId) return CampaignResult0102(true, "CAMPAIGN_LOADED", "This saved campaign is already active.");
            if (slotId == AscendanceCampaignSlots0102.Id(Room)) return CampaignResult0102(true, "CAMPAIGN_LOADED", "This saved campaign is already active.");
            try
            {
                var candidate = AscendanceCampaignSlots0102.Load(Room.RoomId, slotId);
                if (!string.Equals(candidate.CampaignOwnerToken0102, ownerToken, StringComparison.Ordinal))
                    return CampaignResult0102(false, "CAMPAIGN_OWNER_REQUIRED", "This saved campaign belongs to a different creator credential.");
                foreach (var p in candidate.Players) if (p != null) p.Connected = false;
                candidate.Revision = Room.Revision + 1; candidate.GameplayRevision = Room.GameplayRevision + 1;
                candidate.LastCampaignActionId0102 = actionId; candidate.LastCampaignActionCode0102 = "CAMPAIGN_LOADED";
                return ActivateCampaign0102(candidate, ownerToken, "CAMPAIGN_LOADED", "Saved campaign restored with its original Heroes and reconnect identities.");
            }
            catch { return CampaignResult0102(false, "CAMPAIGN_LOAD_FAILED", "The saved campaign could not be restored. The current campaign remains active."); }
        }

        public AscendanceCampaignActionResult0102 StartCampaign0102(string ownerToken, string actionId, IEnumerable<string> ownIds)
        {
            if (OwnerMatches0102(ownerToken) && Room.LastCampaignActionId0102 == actionId && Room.LastCampaignActionCode0102 == "CAMPAIGN_STARTED")
                return CampaignResult0102(true, "CAMPAIGN_STARTED", "The campaign has already started.");
            if (!Guid.TryParseExact(actionId, "N", out _) || !CampaignLobbySnapshot0102(ownerToken, ownIds).CanStart)
                return CampaignResult0102(false, "CAMPAIGN_NOT_READY", "Only the creator may start after 1–4 connected players have chosen different Heroes and marked Ready.");
            var candidate = JsonUtility.FromJson<PersistedServerRoom0100>(JsonUtility.ToJson(Room));
            try { candidate.DurableCampaign = AscendanceGameManager.CreateFreshOnlineCampaign0102(candidate.Players.Select(p => p.HeroId)); }
            catch { return CampaignResult0102(false, "CAMPAIGN_START_FAILED", "Fresh Hero setup failed. The lobby remains available."); }
            candidate.CampaignDraft0102 = false;
            candidate.Revision++; candidate.GameplayRevision++;
            candidate.LastCampaignActionId0102 = actionId; candidate.LastCampaignActionCode0102 = "CAMPAIGN_STARTED";
            return ActivateCampaign0102(candidate, ownerToken, "CAMPAIGN_STARTED", "Your selected party begins a fresh campaign at Level 1.", false);
        }

        private AscendanceCampaignActionResult0102 ActivateCampaign0102(PersistedServerRoom0100 candidate, string ownerToken, string code, string message, bool archive = true)
        {
            var previous = JsonUtility.FromJson<PersistedServerRoom0100>(JsonUtility.ToJson(Room));
            previous.DurableCampaign = SafeCapture();
            var previousEnemies = gm.Enemies.ToDictionary(entry => entry.Key,
                entry => JsonUtility.FromJson<EnemyState>(JsonUtility.ToJson(entry.Value)), StringComparer.OrdinalIgnoreCase);
            if (archive)
            {
                if (string.IsNullOrEmpty(previous.CampaignOwnerToken0102)) previous.CampaignOwnerToken0102 = ownerToken;
                if (!AscendanceCampaignSlots0102.Save(previous, out string archiveMessage)) return CampaignResult0102(false, "CAMPAIGN_ARCHIVE_FAILED", archiveMessage);
            }
            suppressStateEvents = true;
            try
            {
                gm.ApplyOnlineCampaignSlot0102(candidate.DurableCampaign);
                candidate.DurableCampaign = SafeCapture();
                if (persistenceEnabled && !AscendanceServerRoomPersistence0100.Save(candidate, out _))
                    throw new IOException("New active campaign could not be persisted.");
                Room = candidate;
                committedGameplayFingerprint = LiveFingerprint();
                ServerInstanceId = Guid.NewGuid().ToString("N");
                commandCache.Clear(); commandOrder.Clear();
            }
            catch
            {
                gm.ApplyOnlineCampaignSlot0102(previous.DurableCampaign, previousEnemies);
                return CampaignResult0102(false, "CAMPAIGN_ACTIVATION_FAILED", "Campaign activation failed. The previous campaign remains active and archived.");
            }
            finally { suppressStateEvents = false; }
            return CampaignResult0102(true, code, message);
        }

        private AscendanceCampaignActionResult0102 CampaignResult0102(bool accepted, string code, string message) => new()
        { Accepted = accepted, Code = code, Message = message, Snapshot = Snapshot() };
    }
}
