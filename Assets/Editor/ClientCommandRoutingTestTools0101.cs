#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class ClientCommandRoutingTestTools0101
    {
        private const string Prefix = "[ASCENDANCE][0.10.2h ROUTER]";

        [MenuItem("Ascendance/Validation/Test 0.10.2h - Client Command Routing (Inherited Play Mode Gate)")]
        public static void Test0101()
        {
            if (!Application.isPlaying) { Debug.LogError(Prefix + " Enter Play Mode first."); return; }
            var gm = AscendanceGameManager.Instance;
            if (gm == null) { Debug.LogError(Prefix + " No AscendanceGameManager found."); return; }
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass = 0, fail = 0;
            void Check(bool ok, string label)
            {
                if (ok) { pass++; Debug.Log(Prefix + " PASS — " + label); }
                else { fail++; Debug.LogError(Prefix + " FAIL — " + label); }
            }

            var router = AscendanceClientCommandRouter0101.Instance;
            Check(AscendanceGameManager.BuildVersion == "0.10.2h", "BuildVersion identifies the Client Command Routing milestone.");
            Check(router != null && router.IsReady, "Stonebrook installs an active client gameplay command router.");
            Check(router != null && router.IsLoopback, "Local presentation uses loopback transport through the server authority instead of bypassing it.");
            Check(typeof(IAscendanceCommandTransport0101).IsInterface && typeof(AscendanceLoopbackTransport0101).GetInterfaces().Contains(typeof(IAscendanceCommandTransport0101)),
                "Client routing is transport-agnostic so a remote network transport can replace loopback without changing UI call sites.");
            Check(Enum.GetValues(typeof(ServerCommandType0100)).Length >= 37,
                "Authoritative command contract now covers combat, Quest, inventory/equipment, briefing, downtime, crafting, trading, and campaign transitions.");
            Check(typeof(ServerCommandEnvelope0100).GetField("SecondaryTargetId") != null && typeof(ServerCommandEnvelope0100).GetField("IntValue") != null,
                "Command envelope carries secondary-target and integer payloads required by authoritative downtime/trading actions.");

            string assets = Application.dataPath;
            string hud = File.ReadAllText(Path.Combine(assets, "Scripts/UI/AscendanceHUD.cs"));
            string input = File.ReadAllText(Path.Combine(assets, "Scripts/Input/CRPGInputController.cs"));
            string downtime = File.ReadAllText(Path.Combine(assets, "Scripts/UI/DowntimeUI0836.cs"));
            string briefing = File.ReadAllText(Path.Combine(assets, "Scripts/UI/QuestBriefingUI0841.cs"));
            string campaignHub = File.ReadAllText(Path.Combine(assets, "Scripts/UI/CampaignHubUI0834.cs"));
            string campaignEnd = File.ReadAllText(Path.Combine(assets, "Scripts/UI/CampaignEndUI0844.cs"));

            string[] hudForbidden =
            {
                "AscendanceGameManager.Instance.AttackSelectedEnemy()", "AscendanceGameManager.Instance.Explore()",
                "AscendanceGameManager.Instance.QuestPrimaryAction0829()", "AscendanceGameManager.Instance.QuestSecondaryAction0829()",
                "AscendanceGameManager.Instance.SearchCurrentLocation()", "AscendanceGameManager.Instance.UseLocationRevealOption()",
                "AscendanceGameManager.Instance.InteractEncounterObject()", "AscendanceGameManager.Instance.UseHealingPotion()",
                "gm.PlayCard(", "gm.UseReaction(", "gm.ChooseEncounterDeckControl(", "gm.ChooseRewardChoice("
            };
            Check(hud.Contains("AscendanceClientCommandRouter0101") && hudForbidden.All(x => !hud.Contains(x)),
                "Main HUD gameplay actions cross the client command router with no direct combat/card/Quest mutation path.");
            Check(input.Contains("RequestMove") && input.Contains("RequestAttackSelectedEnemy") && !input.Contains("AscendanceGameManager.Instance?.RequestMove(hoveredLocation") && !input.Contains("gm.AttackSelectedEnemy()"),
                "World click movement and attack hotkey route through the authoritative client boundary.");
            Check(downtime.Contains("RequestCraftRecipe") && downtime.Contains("RequestTransferEquipment") && downtime.Contains("RequestPurchaseMerchantEquipment")
                && !downtime.Contains("gm.CraftRecipe0855(") && !downtime.Contains("gm.TransferEquipment0838(") && !downtime.Contains("gm.PurchaseMerchantEquipment0836("),
                "Downtime merchant, crafting, loadout, activity, and trading controls no longer mutate gameplay directly.");
            Check(briefing.Contains("RequestOpenQuestBriefing") && briefing.Contains("RequestToggleQuestReady") && briefing.Contains("RequestConfirmQuestBriefing")
                && !briefing.Contains("gm.OpenQuestBriefing0841(") && !briefing.Contains("gm.ConfirmBriefedQuest0841()"),
                "Quest briefing open/ready/confirm controls route through server authority.");
            Check(campaignHub.Contains("RequestAdvanceWorldEvent") && !campaignHub.Contains("Instance?.AdvanceWorldEvent0833()"),
                "Campaign Hub World Event advancement routes through server authority.");
            Check(campaignEnd.Contains("RequestContinueCampaign") && !campaignEnd.Contains("BeginValewatchContinuation0845()") && !campaignEnd.Contains("BeginNextCampaign0856()") && !campaignEnd.Contains("ApplySaveData("),
                "Campaign continuation has no UI-side authoritative mutation or rollback side door.");
            Check(typeof(AscendanceClientCommandRouter0101).GetMethod("RequestTransferHeroLoot", BindingFlags.Public | BindingFlags.Instance) != null
                && typeof(AscendanceClientCommandRouter0101).GetMethod("RequestBeginCampaignQuest", BindingFlags.Public | BindingFlags.Instance) != null,
                "Router exposes gameplay entry points for both Hero-owned transfers and party/campaign actions.");

            bool foundationFinished = false, foundationPassed = false;
            int foundationErrors = 0;
            void OnFoundationLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) foundationErrors++;
                if (string.IsNullOrEmpty(condition) || !condition.Contains("Dedicated Server Authority Foundation completed:")) return;
                foundationFinished = true;
                foundationPassed = condition.Contains("[ASCENDANCE][0.10.2h SERVER] PASS") && condition.Contains("24 passed, 0 failed");
            }
            Application.logMessageReceived += OnFoundationLog;
            try
            {
                DedicatedServerFoundationTestTools0100.Test0100();
                int frames = 0;
                while (!foundationFinished && frames++ < 120000) yield return null;
            }
            finally { Application.logMessageReceived -= OnFoundationLog; }
            Check(foundationFinished && foundationPassed, "Dedicated Server Authority Foundation remains 24 passed / 0 failed under client routing.");
            Check(foundationErrors == 0, "Inherited server/core regression emits zero Unity Error/Exception/Assert entries under 0.10.2h.");

            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            var previousTransport = router?.Transport;
            AscendanceRoomAuthority0100 isolated = null;
            try
            {
                gm.SetParty(new[] { "Aldric", "Lyra" }, "Aldric");
                gm.DebugResetCampaignProgress0833();
                isolated = new AscendanceRoomAuthority0100(gm, "ROUTER-0101-" + Guid.NewGuid().ToString("N").Substring(0, 8), false);
                var loopback = new AscendanceLoopbackTransport0101(isolated);
                router.SetTransport(loopback);

                Check(loopback.IsConnected && isolated.Room.Players.Count == 0, "Isolated loopback client starts with no implicit host or pre-owned Hero session.");
                long sent0 = router.CommandsSent, accepted0 = router.CommandsAccepted;
                bool startedAldric = router.RequestStartOrEndTurn();
                Check(startedAldric && gm.TurnHeroId == "Aldric" && router.CommandsSent == sent0 + 1 && router.CommandsAccepted == accepted0 + 1,
                    "START TURN travels UI/client -> router -> loopback transport -> server authority -> rules engine.");
                Check(isolated.Room.Players.Count == 1 && isolated.Room.Players[0].HeroId == "Aldric" && isolated.Room.Players[0].Connected,
                    "Loopback creates a persistent player/Hero ownership session before the first routed command.");
                Check(router.LastSnapshot != null && router.LastSnapshot.BuildVersion == "0.10.2h" && router.LastSnapshot.Revision == isolated.Room.Revision,
                    "Accepted client command returns an authoritative 0.10.2h snapshot at the committed room revision.");

                long sentBeforeSelection = router.CommandsSent;
                gm.SelectHero("Lyra");
                Check(router.CommandsSent == sentBeforeSelection && gm.SelectedHeroId == "Lyra",
                    "Pure local Hero selection remains presentation input and does not masquerade as a gameplay command.");

                bool firstRescue = router.RequestQuestPrimaryAction();
                bool secondRescue = router.RequestQuestPrimaryAction();
                Check(firstRescue && secondRescue && gm.VillagersRescued == 2 && gm.VillagersLost == 0
                    && gm.Heroes["Aldric"].Actions == 1 && gm.SelectedHeroId == "Lyra",
                    "Two real routed Rescues spend Aldric's Actions while preserving the viewed Hero and keeping Q001 winnable through Enemy Phase.");

                bool endedAldric = router.RequestStartOrEndTurn();
                Check(endedAldric && gm.TurnHeroId == null, "END TURN uses authoritative TurnHero ownership even while the client is viewing another Hero.");
                Check(gm.SelectedHeroId == "Lyra",
                    "Successful authoritative commands restore the client's local Hero presentation selection after temporary server execution context.");
                gm.SelectHero("Aldric"); // Completed selection must not block the next local hot-seat Hero.
                bool startedLyra = router.RequestStartOrEndTurn();
                Check(startedLyra && gm.TurnHeroId == "Lyra" && gm.SelectedHeroId == "Lyra" && gm.Heroes["Lyra"].Actions == 3
                    && isolated.Room.Players.Count == 2 && isolated.Room.Players.Any(p => p.HeroId == "Lyra"),
                    "Local hot-seat handoff skips a completed Hero selection and grants the next Hero 3 Actions through a distinct server-owned session.");
                int roundBeforeWorldPhase = gm.Round;
                bool endedLyra = router.RequestStartOrEndTurn();
                Check(endedLyra && gm.TurnHeroId == null, "Second Hero END TURN is committed through the same authoritative boundary.");

                // END TURN for the final unfinished Hero starts the real World Phase coroutine.
                // Do not restore the saved test snapshot while that coroutine is still alive: doing
                // so can clear IsResolving while the coroutine remains scheduled, allowing it to wake
                // later inside the next validator. Drive reaction pauses through the same router and
                // require a stable next-Hero-Phase boundary before this validator returns.
                int worldPhaseFrames = 0, stableHeroPhaseFrames = 0;
                while (worldPhaseFrames++ < 3600 && stableHeroPhaseFrames < 30)
                {
                    if (gm.HasPendingReactionWindow) router.RequestResolveReaction();
                    bool settled = gm.Round > roundBeforeWorldPhase
                        && gm.TurnHero == null
                        && !gm.IsResolving
                        && !gm.HasPendingReactionWindow
                        && !gm.HasPendingEncounterDeckControl
                        && !gm.HasPendingRewardChoice
                        && !gm.ParticipatingHeroes.Any(h => h.Moving);
                    if (settled) stableHeroPhaseFrames++; else stableHeroPhaseFrames = 0;
                    yield return null;
                }
                Check(stableHeroPhaseFrames >= 30 && !gm.QuestFailed,
                    "Router validator drains its final-Hero World Phase before restoring production state.");

                long revisionBeforeReject = isolated.Room.Revision;
                bool illegalSearch = router.RequestSearch();
                Check(!illegalSearch && router.LastResult != null && router.LastResult.Code == "RULE_REJECTED" && isolated.Room.Revision == revisionBeforeReject,
                    "Illegal routed gameplay is rejected by server rules without advancing authoritative revision.");
                Check(router.CommandsSent >= sent0 + 5 && router.CommandsAccepted >= accepted0 + 4,
                    "Router telemetry distinguishes sent commands from server-accepted commands.");
                Check(router.LastResult != null && router.LastResult.Snapshot != null && router.LastResult.Snapshot.RoomId == isolated.Room.RoomId,
                    "Rejected commands still return the current authoritative room snapshot for client reconciliation.");
            }
            finally
            {
                if (router != null) router.SetTransport(previousTransport);
                isolated?.Dispose();
                if (restore != null) gm.ApplySaveData(restore);
            }

            Check(router != null && router.Transport == previousTransport && router.IsReady,
                "Validator restores the production client transport after isolated routing tests.");

            if (fail == 0) Debug.Log($"{Prefix} PASS — Client Command Routing completed: {pass} passed, 0 failed.");
            else Debug.LogError($"{Prefix} FAIL — Client Command Routing completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
