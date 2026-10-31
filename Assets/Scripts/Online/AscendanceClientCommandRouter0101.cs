using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ascendance.Online
{
    /// <summary>
    /// Client-side command boundary introduced in 0.10.1a. Presentation code sends gameplay
    /// requests here instead of mutating AscendanceGameManager directly. The default transport
    /// is a loopback adapter over the server-owned room authority; 0.10.2 can replace it with
    /// a real network transport without changing gameplay UI call sites.
    /// </summary>
    public interface IAscendanceCommandTransport0101
    {
        bool IsConnected { get; }
        string RoomId { get; }
        long Revision { get; }
        bool TryResolvePlayerForHero(string heroId, out string playerId);
        ServerCommandResult0100 Send(ServerCommandEnvelope0100 command);
    }

    public sealed class AscendanceLoopbackTransport0101 : IAscendanceCommandTransport0101
    {
        private readonly AscendanceRoomAuthority0100 authority;
        private readonly Dictionary<string, string> playerByHero = new(StringComparer.OrdinalIgnoreCase);

        public bool IsConnected => authority != null;
        public string RoomId => authority?.Room?.RoomId ?? string.Empty;
        public long Revision => authority?.Room?.Revision ?? 0;
        public AscendanceRoomAuthority0100 Authority => authority;

        public AscendanceLoopbackTransport0101(AscendanceRoomAuthority0100 roomAuthority)
        {
            authority = roomAuthority ?? throw new ArgumentNullException(nameof(roomAuthority));
        }

        public bool TryResolvePlayerForHero(string heroId, out string playerId)
        {
            playerId = null;
            if (authority == null || string.IsNullOrWhiteSpace(heroId)) return false;
            if (playerByHero.TryGetValue(heroId, out var cached))
            {
                var live = authority.Room.Players.FirstOrDefault(p => p != null && string.Equals(p.PlayerId, cached, StringComparison.OrdinalIgnoreCase));
                if (live != null)
                {
                    if (!live.Connected)
                    {
                        var rejoin = authority.Reconnect(live.ReconnectToken);
                        if (!rejoin.Accepted) return false;
                    }
                    playerId = live.PlayerId;
                    return true;
                }
                playerByHero.Remove(heroId);
            }

            var existing = authority.Room.Players.FirstOrDefault(p => p != null && string.Equals(p.HeroId, heroId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (!existing.Connected)
                {
                    var rejoin = authority.Reconnect(existing.ReconnectToken);
                    if (!rejoin.Accepted) return false;
                }
                playerByHero[heroId] = existing.PlayerId;
                playerId = existing.PlayerId;
                return true;
            }

            var joined = authority.Join("Local " + heroId, heroId);
            if (!joined.Accepted || joined.Player == null) return false;
            playerByHero[heroId] = joined.Player.PlayerId;
            playerId = joined.Player.PlayerId;
            return true;
        }

        public ServerCommandResult0100 Send(ServerCommandEnvelope0100 command)
        {
            if (authority == null) return new ServerCommandResult0100 { Accepted = false, Code = "TRANSPORT_OFFLINE", Message = "Loopback authority is not available." };
            return authority.Execute(command);
        }
    }

    public sealed class AscendanceClientCommandRouter0101 : MonoBehaviour
    {
        public static AscendanceClientCommandRouter0101 Instance { get; private set; }

        private AscendanceGameManager gm;
        private IAscendanceCommandTransport0101 transport;
        private IAscendanceSnapshotSource0102 snapshotSource;
        private Action<ServerStateSnapshot0100> snapshotHandler;
        private readonly ConcurrentQueue<Action> mainThreadWork = new();
        private readonly HashSet<string> retiredServerInstances = new(StringComparer.Ordinal);
        private int transportGeneration;
        private int mainThreadId;
        private ServerStateSnapshot0100 lastSnapshot;

        public IAscendanceCommandTransport0101 Transport => transport;
        public bool IsReady => gm != null && transport != null && transport.IsConnected && (snapshotSource == null || HasAuthoritativeSnapshot);
        public bool IsLoopback => transport is AscendanceLoopbackTransport0101;
        public bool IsRemoteTcp => transport is AscendanceTcpCommandTransport0102;
        public long CommandsSent { get; private set; }
        public long CommandsAccepted { get; private set; }
        public ServerCommandResult0100 LastResult { get; private set; }
        public ServerStateSnapshot0100 LastSnapshot => lastSnapshot;
        public bool HasAuthoritativeSnapshot { get; private set; }
        public event Action<ServerStateSnapshot0100> SnapshotApplied;
        public event Action<ServerCommandResult0100> CommandCompleted;

        private void Awake() { Instance = this; mainThreadId = Thread.CurrentThread.ManagedThreadId; }

        private void Update() => PumpRemoteSnapshots0102();

        public void PumpRemoteSnapshots0102()
        {
            RequireMainThread0102();
            while (mainThreadWork.TryDequeue(out var work)) work();
        }

        public void Initialize(AscendanceGameManager manager, AscendanceServerAuthority0100 localAuthority)
        {
            gm = manager;
            if (localAuthority?.Authority != null) SetTransport(new AscendanceLoopbackTransport0101(localAuthority.Authority));
            gm?.AddLog("CLIENT ROUTER — gameplay input now crosses the authoritative command boundary before rules execution.");
        }

        /// <summary>0.10.2 installs either loopback or the persistent remote TCP transport through this method.</summary>
        public void SetTransport(IAscendanceCommandTransport0101 next)
        {
            RequireMainThread0102();
            if (snapshotSource != null && snapshotHandler != null) snapshotSource.SnapshotAvailable -= snapshotHandler;
            if (!ReferenceEquals(transport, next) && transport is IDisposable previous) previous.Dispose();
            int generation = ++transportGeneration;
            transport = next;
            LastResult = null;
            lastSnapshot = null;
            HasAuthoritativeSnapshot = false;
            retiredServerInstances.Clear();
            snapshotSource = next as IAscendanceSnapshotSource0102;
            if (snapshotSource != null)
            {
                gm?.EnterRemotePresentation0102();
                snapshotHandler = snapshot => mainThreadWork.Enqueue(() =>
                {
                    if (generation == transportGeneration) ApplyRemoteSnapshot0102(snapshot);
                });
                snapshotSource.SnapshotAvailable += snapshotHandler;
                ApplyRemoteSnapshot0102(snapshotSource.LatestSnapshot);
            }
            else gm?.ClearRemotePresentation0102();
        }

        public ServerCommandResult0100 Send(ServerCommandType0100 type, string actorHeroId = null, string targetId = null,
            string secondaryTargetId = null, string cardId = null, string optionKey = null, string stringValue = null,
            int intValue = 0, bool boolValue = false)
        {
            if (gm == null || transport == null)
                return Complete(LocalReject("TRANSPORT_OFFLINE", "Client command transport is not installed."));
            if (snapshotSource != null && !IsReady)
                return Complete(LocalReject("SNAPSHOT_REQUIRED", "Waiting for the initial authoritative room snapshot."));

            string actor = ResolveActor(actorHeroId);
            if (string.IsNullOrWhiteSpace(actor))
                return Complete(LocalReject("ACTOR_REQUIRED", "No participating Hero is available for this command."));
            string playerId = null;
            if (!IsRemoteTcp && (!transport.TryResolvePlayerForHero(actor, out playerId) || string.IsNullOrWhiteSpace(playerId)))
                return Complete(LocalReject("PLAYER_SESSION_REQUIRED", "No client player session owns the requested Hero."));

            var command = new ServerCommandEnvelope0100
            {
                CommandId = Guid.NewGuid().ToString("N"),
                RoomId = transport.RoomId,
                PlayerId = playerId,
                ExpectedRevision = IsRemoteTcp ? LastSnapshot?.Revision ?? 0 : transport.Revision,
                HasExpectedGameplayRevision = IsRemoteTcp && LastSnapshot != null,
                ExpectedGameplayRevision = LastSnapshot?.GameplayRevision ?? 0,
                ExpectedServerInstanceId = IsRemoteTcp ? LastSnapshot?.ServerInstanceId : null,
                Type = type,
                ActorHeroId = actor,
                TargetId = targetId,
                SecondaryTargetId = secondaryTargetId,
                CardId = cardId,
                OptionKey = optionKey,
                StringValue = stringValue,
                IntValue = intValue,
                BoolValue = boolValue
            };

            CommandsSent++;
            if (transport is AscendanceTcpCommandTransport0102 remote)
            {
                int generation = transportGeneration;
                _ = CompleteRemoteAsync0102(remote, command, generation);
                return new ServerCommandResult0100 { Accepted = false, Code = "COMMAND_PENDING", Message = "Command submitted; awaiting the authoritative result.", Revision = command.ExpectedRevision, Snapshot = LastSnapshot };
            }
            var result = transport.Send(command) ?? LocalReject("NO_RESULT", "Command transport returned no authoritative result.");
            if (result.Accepted) CommandsAccepted++;
            return Complete(result);
        }

        private async Task CompleteRemoteAsync0102(AscendanceTcpCommandTransport0102 remote, ServerCommandEnvelope0100 command, int generation)
        {
            ServerCommandResult0100 result;
            try { result = await remote.SendAsync(command).ConfigureAwait(false); }
            catch (Exception ex) { result = new ServerCommandResult0100 { Accepted = false, Code = "TRANSPORT_ERROR", Message = ex.Message }; }
            mainThreadWork.Enqueue(() =>
            {
                if (generation != transportGeneration) return;
                if (result?.Accepted == true) CommandsAccepted++;
                Complete(result ?? LocalReject("NO_RESULT", "Command transport returned no authoritative result."));
            });
        }

        public bool RequestStartOrEndTurn()
        {
            if (gm == null) return false;
            bool endingTurn = gm.TurnHero != null;
            if (!endingTurn && !IsRemoteTcp) EnsureStartableHeroSelected0102g(gm.SelectedHeroId);
            string actor = endingTurn ? gm.TurnHeroId : gm.SelectedHeroId;
            if (transport is AscendanceTcpCommandTransport0102 remote)
            {
                string ownedHero = remote.HeroId;
                // Even a legacy direct-boot client must not claim the active
                // Hero by pressing End Turn before it has joined its own Hero.
                if (endingTurn && (string.IsNullOrWhiteSpace(ownedHero) ||
                    !string.Equals(ownedHero, gm.TurnHeroId, StringComparison.OrdinalIgnoreCase)))
                {
                    Complete(LocalReject("OTHER_HERO_TURN", "Wait for the other Hero to finish their turn."));
                    return false;
                }
                if (!endingTurn && !string.IsNullOrWhiteSpace(ownedHero)) actor = ownedHero;
            }
            bool accepted = Accepted(Send(endingTurn ? ServerCommandType0100.EndTurn : ServerCommandType0100.StartTurn, actor));
            if (accepted && endingTurn && !IsRemoteTcp && gm.TurnHero == null) EnsureStartableHeroSelected0102g(actor);
            return accepted;
        }

        private bool EnsureStartableHeroSelected0102g(string afterHeroId)
        {
            // Local hot-seat play can hand control to the next eligible Hero.
            // Remote clients keep their selected/owned actor and server ownership checks.
            if (gm == null || gm.TurnHero != null || IsRemoteTcp) return false;
            if (gm.CanStartHeroTurn(gm.SelectedHero)) return true;
            var eligible = gm.ParticipatingHeroes.Where(gm.CanStartHeroTurn).OrderBy(h => h.PartyIndex).ToList();
            if (eligible.Count == 0) return false;
            int afterIndex = !string.IsNullOrWhiteSpace(afterHeroId) && gm.Heroes.TryGetValue(afterHeroId, out var previous)
                ? previous.PartyIndex : -1;
            var next = eligible.FirstOrDefault(h => h.PartyIndex > afterIndex) ?? eligible[0];
            if (!string.Equals(gm.SelectedHeroId, next.Id, StringComparison.OrdinalIgnoreCase)) gm.SelectHero(next.Id);
            return true;
        }

        public bool RequestMove(string locationId) => Accepted(Send(ServerCommandType0100.Move, gm?.TurnHeroId ?? gm?.SelectedHeroId, targetId: locationId));

        public bool RequestAttackSelectedEnemy()
        {
            if (gm == null) return false;
            var actor = gm.TurnHero ?? gm.SelectedHero;
            var target = gm.SelectedEnemy;
            if (target == null || target.Defeated)
                target = gm.FirstLivingEnemyAtSelectedHeroLocation();
            return actor != null && target != null && Accepted(Send(ServerCommandType0100.BasicAttack, actor.Id, targetId: target.Id));
        }

        public bool RequestPlayCard(CardData card)
        {
            if (gm == null || card == null) return false;
            string actor = gm.TurnHeroId ?? gm.SelectedHeroId;
            return Accepted(Send(ServerCommandType0100.PlayCard, actor, targetId: gm.SelectedEnemyId, cardId: card.Id));
        }

        public bool RequestSearch() => Accepted(Send(ServerCommandType0100.Search));
        public bool RequestExplore() => Accepted(Send(ServerCommandType0100.Explore));
        public bool RequestQuestPrimaryAction() => Accepted(Send(ServerCommandType0100.QuestPrimaryAction));
        public bool RequestQuestSecondaryAction() => Accepted(Send(ServerCommandType0100.QuestSecondaryAction));
        public bool RequestLocationOption(string choice = null) => Accepted(Send(ServerCommandType0100.UseLocationRevealOption, optionKey: choice));
        public bool RequestEncounterObject() => Accepted(Send(ServerCommandType0100.InteractEncounterObject));

        public bool RequestHealingPotion()
        {
            if (gm == null) return false;
            var actor = gm.TurnHero ?? gm.SelectedHero;
            if (actor == null) return false;
            var target = gm.SelectedHero;
            string targetId = target != null && gm.CanUseHealingPotionOn(actor, target) ? target.Id : null;
            return Accepted(Send(ServerCommandType0100.UseHealingPotion, actor.Id, targetId: targetId));
        }

        public bool RequestUseReaction(string heroId, string cardId) => Accepted(Send(ServerCommandType0100.UseReaction, heroId, cardId: cardId));
        public bool RequestResolveReaction() => Accepted(Send(ServerCommandType0100.ResolveReaction, gm?.TurnHeroId ?? gm?.SelectedHeroId));
        public bool RequestRewardChoice(string key) => Accepted(Send(ServerCommandType0100.ChooseRewardChoice, optionKey: key));
        public bool RequestEncounterDeckControl(string key) => Accepted(Send(ServerCommandType0100.ChooseEncounterDeckControl, optionKey: key));
        public bool RequestBeginCampaignQuest(string questId) => Accepted(Send(ServerCommandType0100.BeginCampaignQuest, stringValue: questId));
        public bool RequestAdvanceWorldEvent() => Accepted(Send(ServerCommandType0100.AdvanceWorldEvent));
        public bool RequestOpenQuestBriefing(string questId) => Accepted(Send(ServerCommandType0100.OpenQuestBriefing, stringValue: questId));
        public bool RequestCancelQuestBriefing() => Accepted(Send(ServerCommandType0100.CancelQuestBriefing));
        public bool RequestSetQuestReady(string heroId, bool ready) => Accepted(Send(ServerCommandType0100.SetQuestReady, heroId, boolValue: ready));
        public bool RequestToggleQuestReady(string heroId) => gm != null && RequestSetQuestReady(heroId, !gm.IsHeroReadyForQuest0841(heroId));
        public bool RequestConfirmQuestBriefing() => Accepted(Send(ServerCommandType0100.ConfirmQuestBriefing));
        public bool RequestBeginInventoryManagement() => Accepted(Send(ServerCommandType0100.BeginInventoryManagement));
        public bool RequestBeginEquipmentManagement() => Accepted(Send(ServerCommandType0100.BeginEquipmentManagement));

        public bool RequestRecoverHero(string heroId) => Accepted(Send(ServerCommandType0100.RecoverHero, heroId));
        public bool RequestRecoverParty() => Accepted(Send(ServerCommandType0100.RecoverParty));
        public bool RequestWorkDowntime(string heroId) => Accepted(Send(ServerCommandType0100.WorkDowntime, heroId));
        public bool RequestGatherResources(string heroId, DowntimeProfession0836 profession) => Accepted(Send(ServerCommandType0100.GatherResources, heroId, optionKey: profession.ToString()));
        public bool RequestEquipDowntimeEquipment(string heroId, string equipmentId, int replaceAccessoryIndex = -1) => Accepted(Send(ServerCommandType0100.EquipDowntimeEquipment, heroId, targetId: equipmentId, intValue: replaceAccessoryIndex));
        public bool RequestUnequipDowntimeEquipment(string heroId, string equipmentId) => Accepted(Send(ServerCommandType0100.UnequipDowntimeEquipment, heroId, targetId: equipmentId));
        public bool RequestPurchaseMerchantEquipment(string heroId, string equipmentId) => Accepted(Send(ServerCommandType0100.PurchaseMerchantEquipment, heroId, targetId: equipmentId));
        public bool RequestCraftRecipe(string heroId, string recipeId) => Accepted(Send(ServerCommandType0100.CraftRecipe, heroId, targetId: recipeId));
        public bool RequestTransferHealingPotion(string fromHeroId, string toHeroId) => Accepted(Send(ServerCommandType0100.TransferHealingPotion, fromHeroId, secondaryTargetId: toHeroId));
        public bool RequestTransferHeroLoot(string fromHeroId, string toHeroId, string lootId, int count) => Accepted(Send(ServerCommandType0100.TransferHeroLoot, fromHeroId, targetId: lootId, secondaryTargetId: toHeroId, intValue: count));
        public bool RequestTransferAllHeroLoot(string fromHeroId, string toHeroId, string lootId) => Accepted(Send(ServerCommandType0100.TransferAllHeroLoot, fromHeroId, targetId: lootId, secondaryTargetId: toHeroId));
        public bool RequestTransferEquipment(string fromHeroId, string toHeroId, string equipmentId) => Accepted(Send(ServerCommandType0100.TransferEquipment, fromHeroId, targetId: equipmentId, secondaryTargetId: toHeroId));
        public bool RequestContinueCampaign() => Accepted(Send(ServerCommandType0100.ContinueCampaign));

        private string ResolveActor(string requested)
        {
            if (gm == null) return null;
            if (!string.IsNullOrWhiteSpace(requested) && gm.Heroes.TryGetValue(requested, out var explicitHero) && explicitHero.Participating) return explicitHero.Id;
            if (transport is AscendanceTcpCommandTransport0102 remote && !string.IsNullOrWhiteSpace(remote.HeroId) && gm.Heroes.TryGetValue(remote.HeroId, out var owned) && owned.Participating) return owned.Id;
            if (!string.IsNullOrWhiteSpace(gm.TurnHeroId) && gm.Heroes.TryGetValue(gm.TurnHeroId, out var turn) && turn.Participating) return turn.Id;
            if (!string.IsNullOrWhiteSpace(gm.SelectedHeroId) && gm.Heroes.TryGetValue(gm.SelectedHeroId, out var selected) && selected.Participating) return selected.Id;
            return gm.ParticipatingHeroes.FirstOrDefault()?.Id;
        }

        private ServerCommandResult0100 LocalReject(string code, string message) => new()
        {
            Accepted = false,
            Code = code,
            Message = message,
            Revision = transport?.Revision ?? 0,
            Snapshot = LastSnapshot
        };

        private ServerCommandResult0100 Complete(ServerCommandResult0100 result)
        {
            LastResult = result;
            if (snapshotSource != null && result?.Snapshot?.Campaign != null) ApplyRemoteSnapshot0102(result.Snapshot);
            else if (result?.Snapshot != null) lastSnapshot = result.Snapshot;
            CommandCompleted?.Invoke(result);
            if (result != null && !result.Accepted)
                Debug.LogWarning($"[ASCENDANCE][CLIENT ROUTER] SERVER REJECTED — {result.Code}: {result.Message}");
            return result;
        }

        private void ApplyRemoteSnapshot0102(ServerStateSnapshot0100 snapshot)
        {
            RequireMainThread0102();
            if (gm == null || snapshot?.Campaign == null || snapshot.Transient == null || transport == null) return;
            if (!string.Equals(snapshot.RoomId, transport.RoomId, StringComparison.OrdinalIgnoreCase)) return;
            string instance = snapshot.ServerInstanceId ?? string.Empty;
            if (retiredServerInstances.Contains(instance)) return;
            if (lastSnapshot != null)
            {
                string previousInstance = lastSnapshot.ServerInstanceId ?? string.Empty;
                if (string.Equals(instance, previousInstance, StringComparison.Ordinal))
                {
                    if (snapshot.Revision <= lastSnapshot.Revision) return;
                }
                else retiredServerInstances.Add(previousInstance);
            }
            // The transport owns wire objects. Keep a private snapshot for revision/ownership view state.
            var applied = JsonUtility.FromJson<ServerStateSnapshot0100>(JsonUtility.ToJson(snapshot));
            gm.ApplyAuthoritativeSnapshot0102(applied);
            lastSnapshot = applied;
            HasAuthoritativeSnapshot = true;
            SnapshotApplied?.Invoke(applied);
        }

        private void RequireMainThread0102()
        {
            if (mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId != mainThreadId)
                throw new InvalidOperationException("Client presentation snapshots must be applied on the Unity main thread.");
        }

        private static bool Accepted(ServerCommandResult0100 result) => result != null && result.Accepted;

        private void OnDestroy()
        {
            transportGeneration++;
            if (snapshotSource != null && snapshotHandler != null) snapshotSource.SnapshotAvailable -= snapshotHandler;
            if (transport is IDisposable disposable) disposable.Dispose();
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }
}
