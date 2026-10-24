#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendance.EditorTools
{
    // Tests replica installation and existing HUD binding, not simulated gameplay rules.
    public static class RemoteSnapshotRegressionTestTools0102
    {
        private const string Prefix = "[ASCENDANCE TEST 0.10.2 SNAPSHOT]";

        [MenuItem("Ascendance/Validation/Test 0.10.2c - Remote Snapshot Presentation (Play Mode)")]
        public static void ValidateCurrentPlayMode()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            var gm = AscendanceGameManager.Instance;
            var router = AscendanceClientCommandRouter0101.Instance;
            if (!Application.isPlaying || gm == null || router == null || !router.IsLoopback)
                throw new InvalidOperationException("Use the isolated Stonebrook loopback validation fixture.");
            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            var originalTransport = router.Transport;
            var source = new SnapshotFixture();
            int checks = 0, notifications = 0, callbacks = 0;
            void Check(bool ok, string label)
            {
                if (!ok) throw new InvalidOperationException(Prefix + " FAIL — " + label);
                checks++; Debug.Log(Prefix + " PASS — " + label);
            }
            void Changed() { notifications++; }
            gm.StateChanged += Changed;
            try
            {
                string localHero = gm.ParticipatingHeroes.First().Id;
                string serverHero = gm.ParticipatingHeroes.First(h => h.Id != localHero).Id;
                gm.SetAuthorityCommandContext0100(localHero);
                var presentation = gm.HeroAvatars[localHero].GetComponentInChildren<CharacterPresentationController>();
                if (presentation == null) throw new InvalidOperationException("Snapshot fixture requires the actual character presentation.");
                int lateRuleCallbacks = 0;
                presentation.PlayRevive();
                presentation.PlayAttack("Spell", () => lateRuleCallbacks++, () => lateRuleCallbacks++);
                bool attackWasPending = presentation.IsAttacking;
                router.SetTransport(source);
                presentation.NotifyAnimationImpact(); presentation.NotifyAnimationAttackFinished();
                Check(!router.IsReady && !router.HasAuthoritativeSnapshot && attackWasPending && lateRuleCallbacks == 0,
                    "1. Remote input waits for initial state; late local animation events cannot execute gameplay callbacks.");
                GameBootstrap.WhenPresentationReady0102(router, () => callbacks++);
                Check(callbacks == 0, "2. Presentation creation is deferred until state arrives.");
                ServerStateSnapshot0100 Make(long revision, int gold, string instance = "process-a")
                {
                    var campaign = AscendanceSaveSystem.CloneForAuthority0100(restore);
                    campaign.SelectedHeroId = serverHero;
                    campaign.PartyGold = gold;
                    campaign.SearchDeckIds = new List<string>();
                    campaign.Heroes.First(h => h.Id == localHero).Actions = 2;
                    campaign.Heroes.First(h => h.Id == localHero).AttackOnlyActions = 1;
                    return new ServerStateSnapshot0100 { RoomId = source.RoomId, BuildVersion = AscendanceGameManager.BuildVersion,
                        ServerInstanceId = instance, Revision = revision, GameplayRevision = revision, Campaign = campaign,
                        Transient = new ServerTransientState0100() };
                }
                var first = Make(10, 101);
                source.Emit(first); router.PumpRemoteSnapshots0102();
                Check(router.IsReady && router.HasAuthoritativeSnapshot && callbacks == 1, "3. First state installs once and releases presentation creation.");
                Check(gm.PartyGold == 101 && router.LastSnapshot.Revision == 10, "4. Replica campaign values match the server revision.");
                Check(gm.SelectedHeroId == localHero && localHero != serverHero, "5. Server selection does not replace the client's selected Hero.");
                Check(gm.SearchDeck.Count == 0 && gm.Heroes[localHero].Actions == 2 && gm.Heroes[localHero].AttackOnlyActions == 1,
                    "6. Live decks and action counts are exact, without save-load repair.");
                var transient = Make(11, 202);
                transient.Campaign.Round = restore.Round + 1;
                transient.Transient.IsResolving = true;
                transient.Transient.HasPendingReactionWindow = true;
                transient.Transient.PendingReactionKind = ReactionWindowKind.BeforeHeroDamage.ToString();
                transient.Transient.PendingReactionText = "Server damage prompt";
                transient.Transient.ReactionOptions.Add(localHero + "|ABILITY_SHIELD_WALL|Protect ally");
                transient.Transient.MovingHeroIds.Add(localHero);
                string destination = gm.Locations.Keys.First();
                transient.Transient.HeroMovement.Add(new ServerHeroMovement0102 { HeroId = localHero, PendingLocationId = destination });
                transient.Transient.HasPendingRewardChoice = true;
                transient.Transient.PendingRewardChoiceText = "Server reward";
                transient.Transient.RewardOptions.Add("GOLD|Take Gold");
                transient.Transient.HasPendingEncounterDeckControl = true;
                transient.Transient.PendingEncounterDeckControlText = "Server deck prompt";
                transient.Transient.EncounterDeckControlOptions.Add("KEEP|Keep card");
                transient.Transient.RecentLog.Add("Authoritative log entry");
                notifications = 0;
                Task.Run(() => source.Emit(transient)).GetAwaiter().GetResult(); // Worker enqueues only, no Unity calls.
                Check(gm.PartyGold == 101 && router.LastSnapshot.Revision == 10, "7. Worker-thread delivery cannot mutate Unity presentation directly.");
                router.PumpRemoteSnapshots0102();
                Check(gm.PartyGold == 202 && gm.Round == restore.Round + 1 && router.LastSnapshot.Revision == 11, "8. Main-thread pump installs the next complete campaign.");
                Check(notifications == 1 && callbacks == 1, "9. Subscribers see one complete-state event per revision.");
                Check(gm.IsResolving, "10. Resolving state remains visible on a remote replica.");
                Check(gm.HasPendingReactionWindow && gm.PendingReactionKind == ReactionWindowKind.BeforeHeroDamage
                    && gm.PendingReactionText == "Server damage prompt" && gm.PendingReactionOptions.Single().Label == "Protect ally",
                    "11. Reaction kind, prompt and options reproduce server state.");
                var hud = gm.GetComponent<AscendanceHUD>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var panel = hud == null ? null : typeof(AscendanceHUD).GetField("reactionPanel", flags)?.GetValue(hud) as GameObject;
                var title = hud == null ? null : typeof(AscendanceHUD).GetField("reactionTitle", flags)?.GetValue(hud) as Text;
                Check(panel != null && panel.activeSelf && title != null && title.text.Contains("Server damage prompt"), "12. Existing HUD opens the authoritative reaction panel.");
                Check(gm.Heroes[localHero].Moving && gm.Heroes[localHero].PendingLocationId == destination, "13. Movement state includes its authoritative destination.");
                Check(gm.HasPendingRewardChoice && gm.RewardChoiceOptions.Single().Key == "GOLD"
                    && gm.HasPendingEncounterDeckControl && gm.EncounterDeckControlOptions.Single().Key == "KEEP",
                    "14. Reward and deck-control views retain server options.");
                Check(gm.Log.SequenceEqual(new[] { "Authoritative log entry" }), "15. Replica logs match server logs without local rule messages.");
                int hp = gm.Heroes[localHero].Health;
                source.Emit(Make(12, 202)); router.PumpRemoteSnapshots0102();
                Check(!gm.IsResolving && !gm.HasPendingReactionWindow && !panel.activeSelf && !gm.Heroes[localHero].Moving
                    && gm.Heroes[localHero].Health == hp && gm.PartyGold == 202,
                    "16. Closing server windows runs no local damage/reward continuation.");
                source.Emit(Make(11, 303)); source.Emit(Make(12, 404));
                var wrong = Make(99, 505); wrong.RoomId = "WRONG-ROOM"; source.Emit(wrong); router.PumpRemoteSnapshots0102();
                Check(gm.PartyGold == 202 && router.LastSnapshot.Revision == 12, "17. Stale, repeated and wrong-room snapshots cannot overwrite state.");
                source.Emit(Make(1, 606, "process-b")); router.PumpRemoteSnapshots0102();
                Check(gm.PartyGold == 606 && router.LastSnapshot.Revision == 1, "18. A new server instance may restart its revision counter.");
                source.Emit(Make(100, 707, "process-a")); router.PumpRemoteSnapshots0102();
                Check(gm.PartyGold == 606 && router.LastSnapshot.ServerInstanceId == "process-b", "19. Late packets from the retired server instance are ignored.");
                source.Emit(Make(2, 808, "process-b"));
                router.SetTransport(originalTransport); router.PumpRemoteSnapshots0102();
                Check(gm.PartyGold == 606 && router.IsLoopback && !router.HasAuthoritativeSnapshot, "20. A transport switch discards queued callbacks from the previous transport.");
            }
            finally
            {
                gm.StateChanged -= Changed;
                router.SetTransport(originalTransport);
                gm.ApplySaveData(restore);
            }
            Debug.Log($"{Prefix} PASS — {checks} checks.");
        }

        private sealed class SnapshotFixture : IAscendanceCommandTransport0101, IAscendanceSnapshotSource0102
        {
            public event Action<ServerStateSnapshot0100> SnapshotAvailable;
            public bool IsConnected => true;
            public string RoomId => "SNAPSHOT-FIXTURE";
            public long Revision => LatestSnapshot?.Revision ?? 0;
            public ServerStateSnapshot0100 LatestSnapshot { get; private set; }
            public void Emit(ServerStateSnapshot0100 snapshot) { LatestSnapshot = snapshot; SnapshotAvailable?.Invoke(snapshot); }
            public bool TryResolvePlayerForHero(string heroId, out string playerId) { playerId = null; return false; }
            public ServerCommandResult0100 Send(ServerCommandEnvelope0100 command) => throw new InvalidOperationException("Snapshot fixture never executes gameplay.");
        }
    }
}
#endif
