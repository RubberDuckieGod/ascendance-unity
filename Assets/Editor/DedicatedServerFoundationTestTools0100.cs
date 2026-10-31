#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class DedicatedServerFoundationTestTools0100
    {
        private const string Prefix = "[ASCENDANCE][0.10.2h SERVER]";

        [MenuItem("Ascendance/Validation/Test 0.10.2h - Dedicated Server Authority Foundation (Inherited Play Mode Gate)")]
        public static void Test0100()
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

            Check(AscendanceGameManager.BuildVersion == "0.10.2h", "BuildVersion identifies the Dedicated Server Authority Foundation milestone.");
            Check(AscendanceServerAuthority0100.Instance != null && AscendanceServerAuthority0100.Instance.Authority != null,
                "Stonebrook installs a server-owned authority even when running local presentation.");
            Check(AscendanceServerAuthority0100.Instance != null && !string.IsNullOrWhiteSpace(AscendanceServerAuthority0100.Instance.RoomId),
                "Server authority owns a concrete room identity; no player is designated as host.");
            Check(typeof(AscendanceDedicatedServerBuilder0100).GetMethod("BuildWindowsDedicatedServer0100", BindingFlags.Public | BindingFlags.Static) != null,
                "Windows dedicated-server build entry point is installed.");
            Check(Enum.GetValues(typeof(ServerCommandType0100)).Length >= 18,
                "Authoritative command contract covers core turn, movement, combat, card, Quest, Reaction, reward, and campaign actions.");

            bool masterFinished = false, masterPassed = false;
            int masterErrors = 0;
            void OnMasterLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) masterErrors++;
                if (string.IsNullOrEmpty(condition) || !condition.Contains("Master Save/Load + Full Regression completed:")) return;
                masterFinished = true;
                masterPassed = condition.Contains("[ASCENDANCE][0.10.2h TEST] PASS") && condition.Contains("54 passed, 0 failed");
            }
            Application.logMessageReceived += OnMasterLog;
            try
            {
                MasterSaveLoadRegressionTestTools0862.Test0862();
                int frames = 0;
                while (!masterFinished && frames++ < 90000) yield return null;
            }
            finally { Application.logMessageReceived -= OnMasterLog; }
            Check(masterFinished && masterPassed, "Locked Core Game Complete master regression remains 54 passed / 0 failed under 0.10.2h.");
            Check(masterErrors == 0, "Core master regression emits zero Unity Error/Exception/Assert entries under the server foundation.");

            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            AscendanceRoomAuthority0100 authority = null;
            string testRoomId = "VALIDATION-0100-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                gm.SetParty(new[] { "Aldric", "Lyra" }, "Aldric");
                gm.DebugResetCampaignProgress0833();
                gm.SetAuthorityCommandContext0100("Aldric");
                authority = new AscendanceRoomAuthority0100(gm, testRoomId, false);

                Check(authority.Room != null && authority.Room.Players.Count == 0 && authority.Room.Revision == 0,
                    "Fresh server room begins server-owned with no implicit host player.");

                var p1 = authority.Join("Player One", "Aldric");
                var p2 = authority.Join("Player Two", "Lyra");
                Check(p1.Accepted && p2.Accepted && p1.Player.HeroId == "Aldric" && p2.Player.HeroId == "Lyra",
                    "Two players can claim distinct Heroes in the same server-owned room.");
                var duplicateHero = authority.Join("Intruder", "Aldric");
                Check(!duplicateHero.Accepted && duplicateHero.Code == "HERO_CLAIMED",
                    "Server rejects duplicate Hero ownership claims.");

                long revisionBeforeTurn = authority.Room.Revision;
                var start = authority.Execute(new ServerCommandEnvelope0100
                {
                    CommandId = "cmd-start", RoomId = authority.Room.RoomId, PlayerId = p1.Player.PlayerId,
                    ExpectedRevision = revisionBeforeTurn, Type = ServerCommandType0100.StartTurn, ActorHeroId = "Aldric"
                });
                Check(start.Accepted && gm.TurnHeroId == "Aldric" && start.Revision > revisionBeforeTurn,
                    "Start Turn executes through server authority and advances authoritative room revision.");

                var ownership = authority.Execute(new ServerCommandEnvelope0100
                {
                    CommandId = "cmd-steal", RoomId = authority.Room.RoomId, PlayerId = p2.Player.PlayerId,
                    ExpectedRevision = authority.Room.Revision, Type = ServerCommandType0100.EndTurn, ActorHeroId = "Aldric"
                });
                Check(!ownership.Accepted && ownership.Code == "HERO_OWNERSHIP" && gm.TurnHeroId == "Aldric",
                    "Player cannot issue commands for another player's Hero.");

                var stale = authority.Execute(new ServerCommandEnvelope0100
                {
                    CommandId = "cmd-stale", RoomId = authority.Room.RoomId, PlayerId = p1.Player.PlayerId,
                    ExpectedRevision = Math.Max(0, authority.Room.Revision - 1), Type = ServerCommandType0100.EndTurn, ActorHeroId = "Aldric"
                });
                Check(!stale.Accepted && stale.Code == "STALE_REVISION",
                    "Optimistic revision check rejects stale client commands before gameplay mutation.");

                long beforeEnd = authority.Room.Revision;
                var endEnvelope = new ServerCommandEnvelope0100
                {
                    CommandId = "cmd-end", RoomId = authority.Room.RoomId, PlayerId = p1.Player.PlayerId,
                    ExpectedRevision = beforeEnd, Type = ServerCommandType0100.EndTurn, ActorHeroId = "Aldric"
                };
                var end = authority.Execute(endEnvelope);
                long afterEnd = authority.Room.Revision;
                var endDuplicate = authority.Execute(endEnvelope);
                Check(end.Accepted && gm.TurnHeroId == null && afterEnd > beforeEnd,
                    "End Turn commits through the authoritative command boundary.");
                Check(endDuplicate.Duplicate && endDuplicate.Accepted && authority.Room.Revision == afterEnd,
                    "Duplicate CommandId is idempotent and cannot execute the gameplay mutation twice.");

                string reconnect = p2.Player.ReconnectToken;
                Check(authority.Disconnect(p2.Player.PlayerId), "Disconnect marks a player offline without releasing Hero ownership.");
                var blockedClaim = authority.Join("Replacement", "Lyra");
                Check(!blockedClaim.Accepted && blockedClaim.Code == "HERO_CLAIMED",
                    "Disconnected Hero remains reserved for the original player session.");
                var rejoined = authority.Reconnect(reconnect);
                Check(rejoined.Accepted && rejoined.Player.PlayerId == p2.Player.PlayerId && rejoined.Player.HeroId == "Lyra" && rejoined.Player.Connected,
                    "Reconnect token restores the same persistent player and Hero assignment.");

                var snapshot = authority.Snapshot();
                Check(snapshot != null && snapshot.Campaign != null && snapshot.Campaign.Version == "0.10.2h" && snapshot.Revision == authority.Room.Revision,
                    "Server snapshot carries authoritative campaign state and current room revision.");
                Check(snapshot.Players.Count == 2 && snapshot.Players.All(x => !string.IsNullOrWhiteSpace(x.HeroId)) && typeof(ServerPlayerView0100).GetField("ReconnectToken") == null,
                    "Client-facing snapshot exposes player/Hero presence without reconnect-token fields.");
                Check(snapshot.Transient != null && snapshot.Transient.RecentLog.Count <= 40,
                    "Live snapshot carries bounded transient Reaction/movement/log state separately from durable campaign data.");

                Check(!authority.ForceDurableSave(out var disabledMessage) && disabledMessage.Contains("Persistence disabled"),
                    "Validator authority cannot accidentally write a server-room save when persistence is disabled.");

                // Exercise real persistence through an isolated room with no gameplay mutation.
                authority.Dispose(); authority = null;
                using (var persistent = new AscendanceRoomAuthority0100(gm, testRoomId, true))
                {
                    var pp = persistent.Join("Persistent Player", "Aldric");
                    Check(pp.Accepted && persistent.ForceDurableSave(out _), "Server authority can durably persist a safe-boundary room snapshot.");
                }
                bool loaded = AscendanceServerRoomPersistence0100.TryLoad(testRoomId, out var loadedRoom, out _);
                Check(loaded && loadedRoom.DurableCampaign != null && loadedRoom.DurableCampaign.Version == "0.10.2h" && loadedRoom.Players.Count == 1,
                    "Persisted room reload preserves campaign state and persistent Hero ownership metadata.");
            }
            finally
            {
                authority?.Dispose();
                AscendanceServerRoomPersistence0100.Delete(testRoomId);
                if (restore != null) gm.ApplySaveData(restore);
            }

            if (fail == 0) Debug.Log($"{Prefix} PASS — Dedicated Server Authority Foundation completed: {pass} passed, 0 failed.");
            else Debug.LogError($"{Prefix} FAIL — Dedicated Server Authority Foundation completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
