using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ascendance.Online
{
    /// <summary>
    /// Transport-agnostic authoritative room. No player is the host: the room owns the
    /// campaign state, validates Hero ownership and optimistic revisions, de-duplicates
    /// commands, and keeps a durable safe-boundary campaign snapshot.
    /// </summary>
    public sealed partial class AscendanceRoomAuthority0100 : IDisposable
    {
        private readonly AscendanceGameManager gm;
        private readonly bool persistenceEnabled;
        private readonly Dictionary<string, ServerCommandResult0100> commandCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> commandOrder = new();
        private const int MaxCachedCommands0100 = 512;
        private bool suppressStateEvents;
        private bool disposed;
        private string committedGameplayFingerprint;

        public PersistedServerRoom0100 Room { get; private set; }
        public string ServerInstanceId { get; private set; } = Guid.NewGuid().ToString("N");
        public bool RestoredFromPersistence0102d { get; }
        public string PersistencePath0102d => AscendanceServerRoomPersistence0100.PathFor(Room?.RoomId);

        public AscendanceRoomAuthority0100(AscendanceGameManager gameManager, string roomId, bool persistence = true, PersistedServerRoom0100 restored = null)
        {
            gm = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
            persistenceEnabled = persistence;
            string requestedRoomId = NormalizeRoomId(roomId);
            if (restored != null)
            {
                // Wire room routing is case-insensitive, but Linux save names
                // are not. Refuse before constructor persistence can overwrite
                // the active file with a stale case-variant room.
                if (!string.Equals(restored.RoomId, requestedRoomId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Saved room identity differs from its requested filename.");
                if (restored.DurableCampaign == null)
                    throw new InvalidOperationException("Restored room has no durable campaign.");
                AscendanceOptionalSaveState0102.Normalize(restored.DurableCampaign);
                // Persisted authority construction owns the complete restore,
                // including Enemy activation order and removal of stale state.
                gm.ApplyOnlineCampaignSlot0102(restored.DurableCampaign);
            }
            else if (persistenceEnabled && AscendanceServerRoomPersistence0100.SavePathExists(requestedRoomId))
            {
                throw new InvalidOperationException("An existing room save must be restored before authority creation.");
            }
            RestoredFromPersistence0102d = restored != null;
            Room = restored ?? new PersistedServerRoom0100
            {
                RoomId = requestedRoomId,
                BuildVersion = AscendanceGameManager.BuildVersion,
                Revision = 0,
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
                DurableCampaign = SafeCapture()
            };
            Room.RoomId = NormalizeRoomId(Room.RoomId);
            Room.BuildVersion = AscendanceGameManager.BuildVersion;
            Room.Players ??= new List<ServerPlayerSession0100>();
            foreach (var p in Room.Players) if (p != null) p.Connected = false; // process restart requires reconnect proof.
            committedGameplayFingerprint = LiveFingerprint();
            gm.StateChanged += OnGameStateChanged;
            if (!PersistIfSafe())
            {
                gm.StateChanged -= OnGameStateChanged;
                throw new IOException("Room authority could not establish a durable initial save.");
            }
        }

        public ServerJoinResult0100 Join(string displayName, string heroId, string reconnectToken = null)
        {
            if (!string.IsNullOrWhiteSpace(reconnectToken))
            {
                if (!Guid.TryParseExact(reconnectToken, "N", out _))
                    return JoinReject("BAD_RECONNECT_TOKEN", "JOIN requires a random 32-character session credential.");
                var previous = Room.Players.FirstOrDefault(p => p != null && string.Equals(p.ReconnectToken, reconnectToken, StringComparison.Ordinal));
                if (previous != null)
                {
                    if (!string.Equals(previous.HeroId, heroId, StringComparison.OrdinalIgnoreCase))
                        return JoinReject("HERO_OWNERSHIP", "This session credential belongs to another Hero.");
                    if (!previous.Connected) return Reconnect(reconnectToken);
                    return new ServerJoinResult0100 { Accepted = true, Code = "JOINED", Message = "Returning the existing Hero session.", Player = ClonePlayer(previous), Snapshot = Snapshot() };
                }
            }
            if (!gm.Heroes.TryGetValue(heroId ?? string.Empty, out var hero) || (!Room.CampaignDraft0102 && !hero.Participating))
                return JoinReject("HERO_UNAVAILABLE", "That Hero is not part of this room's active party.");
            if (Room.Players.Any(p => p != null && string.Equals(p.HeroId, hero.Id, StringComparison.OrdinalIgnoreCase)))
                return JoinReject("HERO_CLAIMED", "That Hero already belongs to another player session.");
            if (Room.Players.Count(p => p != null) >= 4)
                return JoinReject("ROOM_FULL", "This room already has four player sessions.");

            var now = DateTime.UtcNow.ToString("O");
            var player = new ServerPlayerSession0100
            {
                PlayerId = Guid.NewGuid().ToString("N"),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim(),
                HeroId = hero.Id,
                ReconnectToken = string.IsNullOrWhiteSpace(reconnectToken) ? Guid.NewGuid().ToString("N") : reconnectToken,
                Connected = true,
                JoinedAtUtc = now,
                LastSeenUtc = now
            };
            Room.Players.Add(player);
            CommitRoomMetadata();
            return new ServerJoinResult0100 { Accepted = true, Code = "JOINED", Message = "Joined server-owned room.", Player = ClonePlayer(player), Snapshot = Snapshot() };
        }

        public bool Disconnect(string playerId)
        {
            var player = FindPlayer(playerId);
            if (player == null || !player.Connected) return false;
            player.Connected = false;
            player.LastSeenUtc = DateTime.UtcNow.ToString("O");
            CommitRoomMetadata();
            return true;
        }

        public ServerJoinResult0100 Reconnect(string reconnectToken, string heroId = null)
        {
            var player = Room.Players.FirstOrDefault(p => p != null && !string.IsNullOrWhiteSpace(reconnectToken) && string.Equals(p.ReconnectToken, reconnectToken, StringComparison.Ordinal));
            if (player == null) return JoinReject("RECONNECT_DENIED", "Reconnect token was not recognized.");
            if (!string.IsNullOrWhiteSpace(heroId) && !string.Equals(player.HeroId, heroId, StringComparison.OrdinalIgnoreCase))
                return JoinReject("HERO_OWNERSHIP", "This session credential belongs to another Hero.");
            if (player.Connected)
                return new ServerJoinResult0100 { Accepted = true, Code = "RECONNECTED", Message = "Reconnected to the same Hero session.", Player = ClonePlayer(player), Snapshot = Snapshot() };
            player.Connected = true;
            player.LastSeenUtc = DateTime.UtcNow.ToString("O");
            CommitRoomMetadata();
            return new ServerJoinResult0100 { Accepted = true, Code = "RECONNECTED", Message = "Reconnected to the same Hero session.", Player = ClonePlayer(player), Snapshot = Snapshot() };
        }

        public ServerCommandResult0100 Execute(ServerCommandEnvelope0100 command)
        {
            if (command == null) return Reject("BAD_COMMAND", "Command payload is missing.");
            if (Room.CampaignDraft0102) return Reject("CAMPAIGN_NOT_STARTED", "Choose your Heroes and let the creator start the campaign first.");
            if (!string.IsNullOrWhiteSpace(command.ExpectedServerInstanceId) && !string.Equals(command.ExpectedServerInstanceId, ServerInstanceId, StringComparison.Ordinal))
                return Reject("SERVER_RESTARTED", "Server restarted; review the current state before issuing a new command.");
            string commandKey = (command.PlayerId ?? string.Empty) + ":" + (command.CommandId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(command.CommandId) && commandCache.TryGetValue(commandKey, out var cached))
            {
                var duplicate = CloneResult(cached);
                duplicate.Duplicate = true;
                duplicate.Code = "DUPLICATE";
                duplicate.Message = "Command already processed; returning its outcome with the current authoritative state.";
                duplicate.Revision = Room.Revision;
                duplicate.Snapshot = Snapshot();
                return duplicate;
            }
            if (string.IsNullOrWhiteSpace(command.CommandId)) return Reject("COMMAND_ID_REQUIRED", "Every gameplay command requires an idempotency CommandId.");
            if (!string.Equals(command.RoomId, Room.RoomId, StringComparison.OrdinalIgnoreCase)) return Cache(commandKey, Reject("ROOM_MISMATCH", "Command targets a different room."));
            var player = FindPlayer(command.PlayerId);
            if (player == null || !player.Connected) return Cache(commandKey, Reject("PLAYER_NOT_CONNECTED", "Player session is not connected."));
            if (command.ExpectedRevision != Room.Revision && !(command.HasExpectedGameplayRevision && command.ExpectedGameplayRevision == Room.GameplayRevision))
                return Cache(commandKey, Reject("STALE_REVISION", $"Expected room revision {command.ExpectedRevision}, authoritative revision is {Room.Revision}. Review the updated state before trying another action."));

            string actorId = string.IsNullOrWhiteSpace(command.ActorHeroId) ? player.HeroId : command.ActorHeroId;
            if (!string.Equals(actorId, player.HeroId, StringComparison.OrdinalIgnoreCase))
                return Cache(commandKey, Reject("HERO_OWNERSHIP", "A player may only issue gameplay commands for their assigned Hero."));
            if (!gm.Heroes.TryGetValue(player.HeroId, out var actor) || !actor.Participating)
                return Cache(commandKey, Reject("HERO_UNAVAILABLE", "Assigned Hero is not active in the authoritative room."));

            // Selected Hero/Enemy are client presentation inputs, not room-owned gameplay state.
            // Capture the authoritative fingerprint before installing the temporary execution
            // context, then restore the client's presentation selection after the command on
            // both success and rejection. This prevents one routed Hero action from silently
            // changing which Hero the local client is viewing/controlling next.
            string oldHero = gm.SelectedHeroId;
            string oldEnemy = gm.SelectedEnemyId;
            string before = LiveFingerprint();
            long revisionBefore = Room.Revision;

            suppressStateEvents = true;
            gm.SetAuthorityCommandContext0100(actor.Id, command.TargetId);
            suppressStateEvents = false;

            bool routed;
            try { routed = RouteCommand(command, actor); }
            finally
            {
                suppressStateEvents = true;
                try { gm.SetAuthorityCommandContext0100(oldHero, oldEnemy); }
                finally { suppressStateEvents = false; }
            }

            string after = LiveFingerprint();

            if (!routed || string.Equals(before, after, StringComparison.Ordinal))
            {
                return Cache(commandKey, Reject("RULE_REJECTED", "The authoritative rules engine rejected the command or it produced no legal state change."));
            }

            // Most rules paths raise StateChanged. Also commit a final mutation after an
            // earlier notification, so a presentation-only event cannot hide that mutation.
            if (revisionBefore == Room.Revision || !string.Equals(committedGameplayFingerprint, after, StringComparison.Ordinal))
                CommitLiveState();
            player.LastSeenUtc = DateTime.UtcNow.ToString("O");
            var result = new ServerCommandResult0100 { Accepted = true, Code = "ACCEPTED", Message = "Command committed by server authority.", Revision = Room.Revision, Snapshot = Snapshot() };
            Cache(commandKey, result);
            return result;
        }

        public ServerStateSnapshot0100 Snapshot()
        {
            var campaign = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            return new ServerStateSnapshot0100
            {
                RoomId = Room.RoomId,
                BuildVersion = AscendanceGameManager.BuildVersion,
                Revision = Room.Revision,
                GameplayRevision = Room.GameplayRevision,
                ServerInstanceId = ServerInstanceId,
                RoomStateOrigin0102d = RestoredFromPersistence0102d ? "RESTORED_FROM_SERVER_DISK" : "FRESH_SERVER_PROCESS_STATE",
                RoomCreatedAtUtc0102d = Room.CreatedAtUtc,
                RoomUpdatedAtUtc0102d = Room.UpdatedAtUtc,
                PersistencePath0102d = PersistencePath0102d,
                Campaign = AscendanceSaveSystem.CloneForAuthority0100(campaign),
                Transient = BuildTransient(),
                Players = Room.Players.Where(p => p != null).Select(p => new ServerPlayerView0100 { PlayerId = p.PlayerId, DisplayName = p.DisplayName, HeroId = p.HeroId, Connected = p.Connected, Ready = p.ReadyForCampaign0102 }).ToList(),
                CampaignLobby = CampaignLobbySnapshot0102(null, null)
            };
        }

        public bool ForceDurableSave(out string message)
        {
            if (!AscendanceSaveSystem.CanSave(gm)) { message = "Room is not at a durable safe boundary."; return false; }
            Room.DurableCampaign = AscendanceSaveSystem.CloneForAuthority0100(AscendanceSaveSystem.CaptureForAuthority0100(gm));
            return Persist(out message);
        }

        private bool RouteCommand(ServerCommandEnvelope0100 c, HeroState actor)
        {
            switch (c.Type)
            {
                case ServerCommandType0100.StartTurn: return gm.TryStartHeroTurn(actor);
                case ServerCommandType0100.EndTurn:
                    if (!gm.IsHeroTakingTurn(actor)) return false; gm.EndHeroTurn(); return true;
                case ServerCommandType0100.Move:
                    if (string.IsNullOrWhiteSpace(c.TargetId) || !gm.CanMoveTo(c.TargetId)) return false; gm.RequestMove(c.TargetId); return true;
                case ServerCommandType0100.BasicAttack:
                    if (string.IsNullOrWhiteSpace(c.TargetId)) return false; gm.BasicAttack(c.TargetId); return true;
                case ServerCommandType0100.PlayCard:
                    var card = actor.Hand.FirstOrDefault(x => string.Equals(x.Id, c.CardId, StringComparison.OrdinalIgnoreCase) || string.Equals(x.HeroCardId, c.CardId, StringComparison.OrdinalIgnoreCase));
                    if (card == null) return false; gm.PlayCard(card, c.TargetId); return true;
                case ServerCommandType0100.Search:
                    if (!gm.CanSearchCurrentLocation(actor)) return false; gm.SearchCurrentLocation(); return true;
                case ServerCommandType0100.Explore:
                    if (!gm.CanUseTurnActions(actor)) return false; gm.Explore(); return true;
                case ServerCommandType0100.QuestPrimaryAction:
                    if (!gm.CanQuestPrimaryAction0829(actor)) return false; gm.QuestPrimaryAction0829(); return true;
                case ServerCommandType0100.QuestSecondaryAction:
                    if (!gm.CanQuestSecondaryAction0829(actor)) return false; gm.QuestSecondaryAction0829(); return true;
                case ServerCommandType0100.UseHealingPotion:
                    if (string.IsNullOrWhiteSpace(c.TargetId)) { gm.UseHealingPotion(); return true; }
                    return gm.TryUseHealingPotionOn(c.TargetId);
                case ServerCommandType0100.UseReaction:
                    if (!gm.HasPendingReactionWindow || !gm.PendingReactionOptions.Any(o => string.Equals(o.HeroId, actor.Id, StringComparison.OrdinalIgnoreCase) && string.Equals(o.CardId, c.CardId, StringComparison.OrdinalIgnoreCase))) return false;
                    gm.UseReaction(actor.Id, c.CardId); return true;
                case ServerCommandType0100.ResolveReaction:
                    if (!gm.HasPendingReactionWindow) return false; gm.ResolveReactionWindow(); return true;
                case ServerCommandType0100.ChooseRewardChoice:
                    if (!gm.HasPendingRewardChoice) return false;
                    if (!string.IsNullOrWhiteSpace(gm.PendingRewardChoice.HeroId) && !string.Equals(gm.PendingRewardChoice.HeroId, actor.Id, StringComparison.OrdinalIgnoreCase)) return false;
                    return gm.ChooseRewardChoice(c.OptionKey);
                case ServerCommandType0100.ChooseEncounterDeckControl:
                    return gm.HasPendingEncounterDeckControl && gm.ChooseEncounterDeckControl(c.OptionKey);
                case ServerCommandType0100.SetQuestReady:
                    return gm.SetHeroQuestReady0841(actor.Id, c.BoolValue);
                case ServerCommandType0100.ConfirmQuestBriefing:
                    return gm.ConfirmBriefedQuest0841();
                case ServerCommandType0100.BeginCampaignQuest:
                    return gm.BeginCampaignQuest0833(c.StringValue);
                case ServerCommandType0100.AdvanceWorldEvent:
                    return gm.AdvanceWorldEvent0833();
                case ServerCommandType0100.UseLocationRevealOption:
                    if (!gm.CanUseLocationRevealOption(actor)) return false; gm.UseLocationRevealOption(c.OptionKey); return true;
                case ServerCommandType0100.InteractEncounterObject:
                    if (!gm.CanInteractEncounterObject(actor)) return false; gm.InteractEncounterObject(); return true;
                case ServerCommandType0100.BeginInventoryManagement:
                    if (!gm.CanOpenInventory0854(actor)) return false; gm.BeginInventoryManagement0854(); return true;
                case ServerCommandType0100.BeginEquipmentManagement:
                    if (!gm.CanOpenEquipmentManagement(actor)) return false; gm.BeginEquipmentManagement(); return true;
                case ServerCommandType0100.OpenQuestBriefing:
                    return gm.OpenQuestBriefing0841(c.StringValue);
                case ServerCommandType0100.CancelQuestBriefing:
                    if (!gm.IsQuestBriefingOpen0841) return false; gm.CancelQuestBriefing0841(); return true;
                case ServerCommandType0100.RecoverHero:
                    return gm.RecoverHero0836(actor.Id);
                case ServerCommandType0100.RecoverParty:
                    if (!gm.CanUseDowntimeActions0836) return false; gm.RecoverAllHeroes0836(); return true;
                case ServerCommandType0100.WorkDowntime:
                    return gm.WorkDowntime0839(actor.Id);
                case ServerCommandType0100.GatherResources:
                    if (!Enum.TryParse<DowntimeProfession0836>(c.OptionKey, true, out var gatherProfession)) return false;
                    return gm.GatherResources0839(actor.Id, gatherProfession);
                case ServerCommandType0100.EquipDowntimeEquipment:
                    return gm.EquipDowntimeEquipment0840(actor.Id, c.TargetId, c.IntValue);
                case ServerCommandType0100.UnequipDowntimeEquipment:
                    return gm.UnequipDowntimeEquipment0840(actor.Id, c.TargetId);
                case ServerCommandType0100.PurchaseMerchantEquipment:
                    return gm.PurchaseMerchantEquipment0836(c.TargetId, actor.Id);
                case ServerCommandType0100.CraftRecipe:
                    return gm.CraftRecipe0855(c.TargetId, actor.Id);
                case ServerCommandType0100.TransferHealingPotion:
                    return gm.TransferHealingPotion0838(actor.Id, c.SecondaryTargetId);
                case ServerCommandType0100.TransferHeroLoot:
                    return gm.TransferHeroLoot0838(actor.Id, c.SecondaryTargetId, c.TargetId, Math.Max(1, c.IntValue));
                case ServerCommandType0100.TransferAllHeroLoot:
                    return gm.TransferAllHeroLoot0838(actor.Id, c.SecondaryTargetId, c.TargetId);
                case ServerCommandType0100.TransferEquipment:
                    return gm.TransferEquipment0838(actor.Id, c.SecondaryTargetId, c.TargetId);
                case ServerCommandType0100.ContinueCampaign:
                    return gm.CurrentCampaignId0845 == CampaignContent0845.CampaignI ? gm.BeginValewatchContinuation0845() : gm.BeginNextCampaign0856();
                default: return false;
            }
        }

        private void OnGameStateChanged()
        {
            if (disposed || suppressStateEvents) return;
            CommitLiveState();
        }

        private void CommitLiveState()
        {
            string fingerprint = LiveFingerprint();
            Room.Revision++;
            // Rejected actions and local selection can still notify presentation listeners.
            // Publish their room revision/logs without invalidating another player's decision.
            if (!string.Equals(committedGameplayFingerprint, fingerprint, StringComparison.Ordinal))
            {
                Room.GameplayRevision++;
                committedGameplayFingerprint = fingerprint;
            }
            Room.BuildVersion = AscendanceGameManager.BuildVersion;
            Room.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            PersistIfSafe();
        }

        private void CommitRoomMetadata()
        {
            Room.Revision++;
            Room.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            if (persistenceEnabled) Persist(out _);
        }

        private bool PersistIfSafe()
        {
            if (!persistenceEnabled) return true;
            if (!AscendanceSaveSystem.CanSave(gm)) return false;
            Room.DurableCampaign = AscendanceSaveSystem.CloneForAuthority0100(SafeCapture());
            return Persist(out _);
        }

        private bool Persist(out string message) => !persistenceEnabled ? ReturnFalse(out message, "Persistence disabled for this authority instance.") : AscendanceServerRoomPersistence0100.Save(Room, out message);

        private AscendanceSaveData SafeCapture()
        {
            var data = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            if (data != null) data.Version = AscendanceGameManager.BuildVersion;
            return data;
        }

        private string LiveFingerprint()
        {
            var save = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            if (save != null)
            {
                save.SavedAtUtc = null;
                save.SelectedHeroId = null;
                save.SelectedEnemyId = null;
            }
            var transient = BuildTransient();
            transient.SelectedHeroId = null;
            transient.SelectedEnemyId = null;
            transient.RecentLog.Clear();
            // Keep gameplay windows, movement destinations, turn state, and complete campaign
            // data in the comparison; only timestamps and local presentation are excluded.
            return JsonUtility.ToJson(save) + "|" + JsonUtility.ToJson(transient);
        }

        private ServerTransientState0100 BuildTransient()
        {
            var state = new ServerTransientState0100
            {
                TurnHeroId = gm.TurnHeroId,
                SelectedHeroId = gm.SelectedHeroId,
                SelectedEnemyId = gm.SelectedEnemyId,
                IsResolving = gm.IsResolving,
                HasPendingReactionWindow = gm.HasPendingReactionWindow,
                PendingReactionKind = gm.PendingReactionKind.ToString(),
                PendingReactionText = gm.PendingReactionText,
                HasPendingRewardChoice = gm.HasPendingRewardChoice,
                PendingRewardChoiceText = gm.PendingRewardChoiceText,
                HasPendingEncounterDeckControl = gm.HasPendingEncounterDeckControl,
                PendingEncounterDeckControlText = gm.PendingEncounterDeckControlText,
                MovingHeroIds = gm.ParticipatingHeroes.Where(h => h.Moving).Select(h => h.Id).ToList(),
                HeroMovement = gm.ParticipatingHeroes.Where(h => h.Moving).Select(h => new ServerHeroMovement0102 { HeroId = h.Id, PendingLocationId = h.PendingLocationId }).ToList(),
                RecentLog = gm.Log.Skip(Math.Max(0, gm.Log.Count - 40)).ToList()
            };
            state.ReactionOptions = gm.PendingReactionOptions.Select(o => $"{o.HeroId}|{o.CardId}|{o.Label}").ToList();
            state.RewardOptions = gm.RewardChoiceOptions.Select(o => $"{o.Key}|{o.Label}").ToList();
            state.EncounterDeckControlOptions = gm.EncounterDeckControlOptions.Select(o => $"{o.Key}|{o.Label}").ToList();
            return state;
        }

        private ServerPlayerSession0100 FindPlayer(string playerId) => Room.Players.FirstOrDefault(p => p != null && string.Equals(p.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        private ServerJoinResult0100 JoinReject(string code, string message) => new() { Accepted = false, Code = code, Message = message, Snapshot = Snapshot() };
        private ServerCommandResult0100 Reject(string code, string message) => new() { Accepted = false, Code = code, Message = message, Revision = Room.Revision, Snapshot = Snapshot() };
        private ServerCommandResult0100 Cache(string key, ServerCommandResult0100 value)
        {
            if (!commandCache.ContainsKey(key)) commandOrder.Enqueue(key);
            commandCache[key] = CloneResult(value);
            while (commandOrder.Count > MaxCachedCommands0100) commandCache.Remove(commandOrder.Dequeue());
            return value;
        }
        private static ServerCommandResult0100 CloneResult(ServerCommandResult0100 value) => value == null ? null : JsonUtility.FromJson<ServerCommandResult0100>(JsonUtility.ToJson(value));
        private static ServerPlayerSession0100 ClonePlayer(ServerPlayerSession0100 value) => value == null ? null : JsonUtility.FromJson<ServerPlayerSession0100>(JsonUtility.ToJson(value));
        private static bool ReturnFalse(out string message, string value) { message = value; return false; }
        private static string NormalizeRoomId(string roomId) => string.IsNullOrWhiteSpace(roomId) ? "LOCAL-ROOM" : roomId.Trim();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            gm.StateChanged -= OnGameStateChanged;
        }
    }
}
