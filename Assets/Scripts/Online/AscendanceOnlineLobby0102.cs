using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ascendance.Online
{
    /// <summary>A public lobby row. Saved credentials are only a reconnect hint;
    /// the server always verifies them before confirming a claim.</summary>
    public sealed class AscendanceOnlineHeroChoice0102
    {
        public string HeroId;
        public string DisplayName;
        public string Status;
        public string ClaimedBy;
        public bool Connected;
        public bool HasSavedCredential;
        public bool CanChoose;
    }

    /// <summary>Owns the menu connection until a confirmed Hero claim is handed to
    /// gameplay. Tick is called only on Unity's thread; workers never touch UI.</summary>
    public sealed class AscendanceOnlineLobby0102 : IDisposable
    {
        private readonly List<AscendanceOnlineHeroChoice0102> choices = new();
        private AscendanceTcpCommandTransport0102 transport;
        private Task<bool> connecting;
        private Task<AscendanceHeroClaimResult0102> claiming;
        private ServerStateSnapshot0100 snapshot;
        private bool handedOff;
        private bool wasConnected;
        private string selectedHeroId;
        private string status = "Join the online campaign to choose your Hero.";

        public event Action Changed;
        public event Action Claimed;
        public IReadOnlyList<AscendanceOnlineHeroChoice0102> HeroChoices => choices;
        public ServerStateSnapshot0100 Snapshot => snapshot;
        public string SelectedHeroId => selectedHeroId;
        public string Status => status;
        public bool IsBusy => connecting != null || claiming != null;
        public bool IsConnected => transport?.IsConnected == true;
        public bool IsClaimed { get; private set; }
        public AscendanceHeroClaimResult0102 LastClaimResult { get; private set; }

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

        public void Tick()
        {
            if (transport == null) return;
            bool changed = false, didClaim = false;
            if (connecting?.IsCompleted == true)
            {
                bool connected = false;
                try { connected = connecting.GetAwaiter().GetResult(); }
                catch { /* Transport errors are presented without private request data. */ }
                connecting = null;
                status = connected ? "Choose one Hero. Your friends choose their own Heroes." : ConnectionFailure();
                changed = true;
            }
            if (claiming?.IsCompleted == true)
            {
                try { LastClaimResult = claiming.GetAwaiter().GetResult(); }
                catch { LastClaimResult = Failure("CONNECTION_ERROR", "Unable to confirm this Hero. Retry the connection."); }
                claiming = null;
                IsClaimed = LastClaimResult?.Accepted == true && transport.IsConnected &&
                    string.Equals(transport.HeroId, selectedHeroId, StringComparison.OrdinalIgnoreCase);
                status = IsClaimed ? "Hero confirmed. Joining your campaign…" :
                    (LastClaimResult?.Message ?? "The Hero could not be claimed. Choose another Hero or retry.");
                didClaim = IsClaimed;
                changed = true;
            }
            if (wasConnected != IsConnected)
            {
                wasConnected = IsConnected;
                if (!wasConnected && connecting == null && claiming == null) status = ConnectionFailure();
                changed = true;
            }
            var current = transport.LatestSnapshot;
            if (!ReferenceEquals(current, snapshot))
            {
                snapshot = current;
                changed = true;
            }
            if (changed)
            {
                RebuildChoices();
                Changed?.Invoke();
            }
            if (didClaim) Claimed?.Invoke();
        }

        public bool SelectHero(string heroId)
        {
            if (IsBusy || IsClaimed) return false;
            var choice = choices.FirstOrDefault(h => string.Equals(h.HeroId, heroId, StringComparison.OrdinalIgnoreCase));
            if (choice == null || !choice.CanChoose) return false;
            selectedHeroId = choice.HeroId;
            status = choice.HasSavedCredential && !string.IsNullOrWhiteSpace(choice.ClaimedBy)
                ? "Saved login found. The server will verify your reconnect."
                : "Ready to join as " + choice.DisplayName + ".";
            Changed?.Invoke();
            return true;
        }

        public Task<AscendanceHeroClaimResult0102> ClaimSelectedAsync()
        {
            if (IsBusy) return claiming ?? Task.FromResult(Failure("LOBBY_BUSY", "Wait for the current connection request."));
            var choice = choices.FirstOrDefault(h => string.Equals(h.HeroId, selectedHeroId, StringComparison.OrdinalIgnoreCase));
            if (!IsConnected || choice == null || !choice.CanChoose)
            {
                LastClaimResult = Failure("HERO_SELECTION_REQUIRED", "Choose an available Hero before joining.");
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

        public bool TryHandOff()
        {
            if (!IsClaimed || transport == null || !AscendanceRemoteClientBootstrap0102.StageClaimedTransport(transport))
            {
                status = "The Hero connection is no longer ready. Retry before joining.";
                IsClaimed = false;
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
            if (snapshot?.Campaign?.PartyHeroIds == null) return;
            foreach (string id in snapshot.Campaign.PartyHeroIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Take(4))
            {
                var owner = snapshot.Players?.FirstOrDefault(p => p != null && string.Equals(p.HeroId, id, StringComparison.OrdinalIgnoreCase));
                bool saved = transport?.HasSavedHeroCredential(id) == true;
                bool available = owner == null;
                string ownerName = owner == null ? null : string.IsNullOrWhiteSpace(owner.DisplayName) ? "another player" : owner.DisplayName;
                string rowStatus = available ? "AVAILABLE" : (owner.Connected ? "PLAYING — " : "RESERVED — ") + ownerName;
                if (!available && saved) rowStatus += "\nSaved login found · try reconnect";
                choices.Add(new AscendanceOnlineHeroChoice0102
                {
                    HeroId = id, DisplayName = HeroDatabase.GetHero(id)?.displayName ?? id,
                    ClaimedBy = ownerName, Connected = owner?.Connected == true,
                    HasSavedCredential = saved, Status = rowStatus,
                    CanChoose = IsConnected && !IsBusy && !IsClaimed && (available || saved)
                });
            }
            if (!IsBusy && !IsClaimed && !choices.Any(c => c.CanChoose && string.Equals(c.HeroId, selectedHeroId, StringComparison.OrdinalIgnoreCase)))
                selectedHeroId = null;
        }

        private string ConnectionFailure() => string.IsNullOrWhiteSpace(transport?.LastConnectionError)
            ? "The server connection is unavailable. Select RETRY CONNECTION."
            : "Connection failed: " + transport.LastConnectionError + " Select RETRY CONNECTION.";

        private static AscendanceHeroClaimResult0102 Failure(string code, string message) => new()
        { Accepted = false, Code = code, Message = message };

        public void Close()
        {
            if (!handedOff) transport?.Dispose();
            transport = null;
            connecting = null;
            claiming = null;
            snapshot = null;
            selectedHeroId = null;
            choices.Clear();
            IsClaimed = false;
            wasConnected = false;
            LastClaimResult = null;
            status = "Join the online campaign to choose your Hero.";
        }

        public void Dispose() => Close();
    }
}
