using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ascendance.Online
{
    /// <summary>Public roster data; a saved credential is a hint until verified by the server.</summary>
    public sealed class AscendanceOnlineHeroChoice0102
    {
        public string HeroId;
        public string DisplayName;
        public string Status;
        public string ClaimedBy;
        public bool Connected;
        public bool HasSavedCredential;
        public bool CanChoose;
        public bool IsOwnClaim;
        public bool Ready;
    }

    /// <summary>Menu connection, campaign selection and one authenticated Hero claim.
    /// Tick and all public commands are called on Unity's thread. Socket work stays on workers.</summary>
    public sealed class AscendanceOnlineLobby0102 : IDisposable
    {
        private readonly List<AscendanceOnlineHeroChoice0102> choices = new();
        private readonly Dictionary<string, string> rejectedCredentialOwners = new(StringComparer.OrdinalIgnoreCase);
        private AscendanceTcpCommandTransport0102 transport;
        private Task<bool> connecting;
        private Task<AscendanceHeroClaimResult0102> claiming;
        private Task<AscendanceCampaignActionResult0102> campaignAction;
        private ServerStateSnapshot0100 snapshot;
        private bool handedOff;
        private bool wasConnected;
        private bool entryRaised;
        private bool createWhenConnected;
        private string campaignActionName;
        private string selectedHeroId;
        private string status = "Join the online campaign to choose your Hero.";

        public event Action Changed;
        public event Action Claimed;
        public IReadOnlyList<AscendanceOnlineHeroChoice0102> HeroChoices => choices;
        public ServerStateSnapshot0100 Snapshot => snapshot;
        public ServerCampaignLobby0102 CampaignLobby => snapshot?.CampaignLobby;
        public string SelectedHeroId => selectedHeroId;
        public string ClaimedHeroId => transport?.HeroId;
        public string PlayerId => transport?.PlayerId;
        public string Status => status;
        public bool IsBusy => connecting != null || claiming != null || campaignAction != null;
        public bool IsConnected => transport?.IsConnected == true;
        public bool IsDraft => CampaignLobby?.IsDraft == true;
        public bool SupportsCampaignSetup => CampaignLobby?.Supported == true;
        public bool CanCreateCampaign => IsConnected && !IsBusy && CampaignLobby?.CanCreate == true;
        public bool CanStartCampaign => IsConnected && !IsBusy && CampaignLobby?.CanStart == true;
        public bool IsClaimed { get; private set; }
        public bool IsReady => OwnPlayer()?.Ready == true;
        public AscendanceHeroClaimResult0102 LastClaimResult { get; private set; }
        public AscendanceCampaignActionResult0102 LastCampaignResult { get; private set; }

        public void Open()
        {
            AscendanceRemoteClientBootstrap0102.ClearStagedTransport();
            Open(AscendanceRemoteClientBootstrap0102.CreateLobbyTransport());
        }

        public void Open(AscendanceTcpCommandTransport0102 connection)
        {
            Close();
            transport = connection ?? throw new ArgumentNullException(nameof(connection));
            handedOff = false;
            status = "Connecting to your online campaign…";
            connecting = transport.ConnectAsync();
            Changed?.Invoke();
        }

        /// <summary>Queue only after the user explicitly confirms the archive-and-create screen.</summary>
        public void CreateAfterConnect()
        {
            if (transport == null) throw new InvalidOperationException("Open the online connection first.");
            createWhenConnected = true;
            status = "Connecting before creating the new campaign lobby…";
            Changed?.Invoke();
        }

        public void Tick()
        {
            if (transport == null) return;
            bool changed = false;
            if (connecting?.IsCompleted == true)
            {
                bool connected = false;
                try { connected = connecting.GetAwaiter().GetResult(); }
                catch { /* Only the transport's sanitized connection message is shown. */ }
                connecting = null;
                status = connected ? "Choose one Hero. Your friends choose their own Heroes." : ConnectionFailure();
                changed = true;
            }
            if (claiming?.IsCompleted == true)
            {
                try { LastClaimResult = claiming.GetAwaiter().GetResult(); }
                catch { LastClaimResult = ClaimFailure("CONNECTION_ERROR", "Unable to confirm this Hero. Retry the connection."); }
                claiming = null;
                if (LastClaimResult?.Accepted == false && LastClaimResult.Code == "HERO_CLAIMED")
                {
                    string requested = LastClaimResult.HeroId ?? selectedHeroId;
                    var owner = (LastClaimResult.Snapshot ?? transport.LatestSnapshot)?.Players?.FirstOrDefault(p =>
                        p != null && string.Equals(p.HeroId, requested, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(requested) && owner != null)
                        rejectedCredentialOwners[requested] = owner.PlayerId ?? string.Empty;
                }
                status = LastClaimResult?.Accepted == true ? "Hero confirmed." :
                    LastClaimResult?.Message ?? "The Hero could not be claimed. Choose another Hero or retry.";
                changed = true;
            }
            if (campaignAction?.IsCompleted == true)
            {
                try { LastCampaignResult = campaignAction.GetAwaiter().GetResult(); }
                catch { LastCampaignResult = CampaignFailure("CONNECTION_ERROR", "The campaign request could not be confirmed. Retry the connection."); }
                campaignAction = null;
                if (LastCampaignResult?.Accepted == true)
                {
                    status = campaignActionName == "create" ? "New campaign lobby created. Choose from all 16 Heroes." :
                        campaignActionName == "load" ? "Saved campaign loaded. Choose your Hero." :
                        campaignActionName == "ready" ? "Readiness confirmed by the server." : "Campaign started. Joining your party…";
                    if (campaignActionName == "create" || campaignActionName == "load")
                    {
                        selectedHeroId = null;
                        rejectedCredentialOwners.Clear();
                        LastClaimResult = null;
                        entryRaised = false;
                    }
                }
                else status = LastCampaignResult?.Message ?? "The campaign request was refused.";
                campaignActionName = null;
                changed = true;
            }
            if (wasConnected != IsConnected)
            {
                wasConnected = IsConnected;
                if (!wasConnected && !IsBusy) status = ConnectionFailure();
                else if (wasConnected && !IsBusy && LastClaimResult == null && LastCampaignResult == null)
                    status = "Connected. Choose one Hero. Your friends choose their own Heroes.";
                changed = true;
            }
            var current = transport.LatestSnapshot;
            if (!ReferenceEquals(current, snapshot)) { snapshot = current; changed = true; }
            IsClaimed = IsConnected && OwnPlayer() != null && !string.IsNullOrWhiteSpace(transport.HeroId);
            if (changed)
            {
                RebuildChoices();
                Changed?.Invoke();
            }
            if (createWhenConnected && !IsBusy && snapshot != null)
            {
                createWhenConnected = false;
                _ = CreateNewCampaignAsync();
            }
            if (!entryRaised && IsClaimed && !IsDraft && snapshot?.Campaign?.PartyHeroIds?.Any(id =>
                string.Equals(id, transport.HeroId, StringComparison.OrdinalIgnoreCase)) == true)
            {
                entryRaised = true;
                status = "Hero confirmed. Joining your campaign…";
                Claimed?.Invoke();
            }
        }

        private ServerPlayerView0100 OwnPlayer() => snapshot?.Players?.FirstOrDefault(p => p != null && p.Connected &&
            !string.IsNullOrWhiteSpace(transport?.PlayerId) && string.Equals(p.PlayerId, transport.PlayerId, StringComparison.Ordinal));

        public bool SelectHero(string heroId)
        {
            if (IsBusy || IsClaimed && !IsDraft) return false;
            var choice = choices.FirstOrDefault(h => string.Equals(h.HeroId, heroId, StringComparison.OrdinalIgnoreCase));
            if (choice == null || !choice.CanChoose) return false;
            selectedHeroId = choice.HeroId;
            status = choice.IsOwnClaim ? "This is your Hero. Mark READY when your choice is final." :
                choice.HasSavedCredential && !string.IsNullOrWhiteSpace(choice.ClaimedBy) ?
                "Saved login found. The server will verify your reconnect." : "Ready to claim " + choice.DisplayName + ".";
            Changed?.Invoke();
            return true;
        }

        public Task<AscendanceHeroClaimResult0102> ClaimSelectedAsync()
        {
            if (IsBusy) return claiming ?? Task.FromResult(ClaimFailure("LOBBY_BUSY", "Wait for the current connection request."));
            var choice = choices.FirstOrDefault(h => string.Equals(h.HeroId, selectedHeroId, StringComparison.OrdinalIgnoreCase));
            if (!IsConnected || choice == null || !choice.CanChoose)
            {
                LastClaimResult = ClaimFailure("HERO_SELECTION_REQUIRED", "Choose an available Hero before joining.");
                status = LastClaimResult.Message;
                Changed?.Invoke();
                return Task.FromResult(LastClaimResult);
            }
            status = "Confirming " + choice.DisplayName + " with the server…";
            claiming = transport.ClaimHeroAsync(choice.HeroId);
            RebuildChoices();
            Changed?.Invoke();
            return claiming;
        }

        public Task<AscendanceCampaignActionResult0102> CreateNewCampaignAsync()
        {
            if (!SupportsCampaignSetup) return RefuseCampaign("SERVER_UPDATE_REQUIRED", "This server supports joining its saved party. Update the server to create a new campaign with all 16 Heroes.");
            if (!CanCreateCampaign) return RefuseCampaign("CAMPAIGN_CREATE_UNAVAILABLE", "The server cannot create a new campaign now. Return all players to the main menu and use the campaign owner's original computer.");
            return BeginCampaignAction(transport.CreateCampaignAsync(), "create", "Saving the current campaign and creating a fresh lobby…");
        }

        public Task<AscendanceCampaignActionResult0102> StartCampaignAsync()
        {
            if (!CanStartCampaign) return RefuseCampaign("CAMPAIGN_START_UNAVAILABLE", "The campaign creator can start once one to four players have claimed Heroes and marked READY.");
            return BeginCampaignAction(transport.StartCampaignAsync(), "start", "Starting the campaign with the ready party…");
        }

        public Task<AscendanceCampaignActionResult0102> LoadCampaignAsync(string slotId)
        {
            if (!SupportsCampaignSetup || !CanCreateCampaign) return RefuseCampaign("CAMPAIGN_LOAD_UNAVAILABLE", "Only the campaign owner can load a saved campaign after all players return to the main menu.");
            if (CampaignLobby.Slots == null || !CampaignLobby.Slots.Any(s => s != null && s.Id == slotId))
                return RefuseCampaign("CAMPAIGN_SLOT_UNAVAILABLE", "Choose an existing saved campaign.");
            return BeginCampaignAction(transport.LoadCampaignAsync(slotId), "load", "Saving the current campaign and loading the selected save…");
        }

        public Task<AscendanceCampaignActionResult0102> SetReadyAsync(bool ready)
        {
            if (!IsDraft || !IsClaimed || IsBusy) return RefuseCampaign("HERO_CLAIM_REQUIRED", "Claim one Hero before marking READY.");
            return BeginCampaignAction(transport.SetReadyAsync(ready), "ready", ready ? "Confirming READY…" : "Updating your readiness…");
        }

        private Task<AscendanceCampaignActionResult0102> BeginCampaignAction(Task<AscendanceCampaignActionResult0102> request, string name, string message)
        {
            campaignAction = request;
            campaignActionName = name;
            status = message;
            RebuildChoices();
            Changed?.Invoke();
            return request;
        }

        private Task<AscendanceCampaignActionResult0102> RefuseCampaign(string code, string message)
        {
            LastCampaignResult = CampaignFailure(code, message);
            status = message;
            Changed?.Invoke();
            return Task.FromResult(LastCampaignResult);
        }

        public bool TryHandOff()
        {
            if (IsDraft || !IsClaimed || transport == null || !AscendanceRemoteClientBootstrap0102.StageClaimedTransport(transport))
            {
                status = "The Hero connection is no longer ready. Retry before joining.";
                IsClaimed = false;
                entryRaised = false;
                Changed?.Invoke();
                return false;
            }
            handedOff = true;
            transport = null;
            return true;
        }

        private void RebuildChoices()
        {
            choices.Clear();
            IEnumerable<string> roster = IsDraft ? CampaignLobby.AvailableHeroIds : snapshot?.Campaign?.PartyHeroIds;
            if (roster == null) return;
            foreach (string id in roster.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Take(IsDraft ? 16 : 4))
            {
                var owner = snapshot.Players?.FirstOrDefault(p => p != null && string.Equals(p.HeroId, id, StringComparison.OrdinalIgnoreCase));
                bool own = owner != null && string.Equals(owner.PlayerId, transport?.PlayerId, StringComparison.Ordinal);
                bool saved = transport?.HasSavedHeroCredential(id) == true;
                bool available = owner == null;
                if (rejectedCredentialOwners.TryGetValue(id, out string rejectedOwner) &&
                    (available || !string.Equals(rejectedOwner, owner.PlayerId ?? string.Empty, StringComparison.Ordinal)))
                    rejectedCredentialOwners.Remove(id);
                bool credentialRejected = owner != null && rejectedCredentialOwners.ContainsKey(id);
                string ownerName = owner == null ? null : string.IsNullOrWhiteSpace(owner.DisplayName) ? "another player" : owner.DisplayName;
                string rowStatus = available ? "AVAILABLE" : (owner.Connected ? "PLAYING — " : "RESERVED — ") + ownerName;
                if (IsDraft && owner != null && owner.Connected) rowStatus = (own ? "YOU — " : ownerName + " — ") + (owner.Ready ? "READY" : "CHOOSING");
                if (!available && saved && !credentialRejected && !own) rowStatus += "\nSaved login found · try reconnect";
                choices.Add(new AscendanceOnlineHeroChoice0102
                {
                    HeroId = id, DisplayName = HeroDatabase.GetHero(id)?.displayName ?? id,
                    ClaimedBy = ownerName, Connected = owner?.Connected == true,
                    HasSavedCredential = saved, Status = rowStatus, IsOwnClaim = own, Ready = owner?.Ready == true,
                    CanChoose = IsConnected && !IsBusy && (!IsClaimed || IsDraft) && (available || own || saved && !credentialRejected)
                });
            }
            if (!IsBusy && (!IsClaimed || IsDraft) && !choices.Any(c => c.CanChoose && string.Equals(c.HeroId, selectedHeroId, StringComparison.OrdinalIgnoreCase)))
                selectedHeroId = null;
        }

        private string ConnectionFailure() => string.IsNullOrWhiteSpace(transport?.LastConnectionError)
            ? "The server connection is unavailable. Select RETRY CONNECTION."
            : "Connection failed: " + transport.LastConnectionError + " Select RETRY CONNECTION.";
        private static AscendanceHeroClaimResult0102 ClaimFailure(string code, string message) => new() { Accepted = false, Code = code, Message = message };
        private static AscendanceCampaignActionResult0102 CampaignFailure(string code, string message) => new() { Accepted = false, Code = code, Message = message };

        public void Close()
        {
            if (!handedOff) transport?.Dispose();
            transport = null;
            connecting = null;
            claiming = null;
            campaignAction = null;
            snapshot = null;
            selectedHeroId = null;
            choices.Clear();
            rejectedCredentialOwners.Clear();
            IsClaimed = false;
            wasConnected = false;
            entryRaised = false;
            createWhenConnected = false;
            campaignActionName = null;
            LastClaimResult = null;
            LastCampaignResult = null;
            status = "Join the online campaign to choose your Hero.";
        }

        public void Dispose() => Close();
    }
}
