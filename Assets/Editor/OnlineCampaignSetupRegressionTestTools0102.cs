#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class OnlineCampaignSetupRegressionTestTools0102
    {
        private const string Prefix = "[ASCENDANCE][ONLINE CAMPAIGN]";

        [MenuItem("Ascendance/Validation/Test Online Campaign Creation (Play Mode)")]
        public static void TestCampaignSetup()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            if (!Application.isPlaying || AscendanceGameManager.Instance == null)
                throw new InvalidOperationException("Open the isolated Stonebrook fixture in Play Mode first.");
            AscendanceGameManager.Instance.StartCoroutine(Run(AscendanceGameManager.Instance));
        }

        private static IEnumerator Await(Task task)
        {
            float deadline = Time.realtimeSinceStartup + 25f;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            if (!task.IsCompleted) throw new TimeoutException("Campaign socket request did not complete.");
            task.GetAwaiter().GetResult();
        }

        private static string Gameplay(AscendanceSaveData save)
        {
            var copy = AscendanceSaveSystem.CloneForAuthority0100(save);
            copy.SavedAtUtc = null;
            return JsonUtility.ToJson(copy);
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int checks = 0;
            void Check(bool ok, string label)
            {
                if (!ok) throw new InvalidOperationException(Prefix + " FAIL — " + label);
                checks++;
                Debug.Log(Prefix + " PASS — " + label);
            }
            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            string room = "CAMPAIGN-EDITOR-" + Guid.NewGuid().ToString("N");
            int port = 0;
            var transports = new List<AscendanceTcpCommandTransport0102>();
            var prefs = new Dictionary<string, string>();
            GameObject listener = null;
            AscendanceTcpServer0102 server = null;
            AscendanceRoomAuthority0100 authority = null;
            string slotFolder = Path.Combine(AscendanceServerRoomPersistence0100.RootPath, "CampaignSlots", room);
            string savePath = AscendanceServerRoomPersistence0100.PathFor(room);
            string ReconnectKey(string hero) => $"ASCENDANCE_0102_RECONNECT_127.0.0.1_{port}_{room}_{hero}";
            AscendanceTcpCommandTransport0102 Client(string name)
            {
                var value = new AscendanceTcpCommandTransport0102("127.0.0.1", port, room, name);
                transports.Add(value);
                return value;
            }
            try
            {
                float deadline = Time.realtimeSinceStartup + 20f;
                while ((gm.IsResolving || gm.HasPendingReactionWindow || gm.ParticipatingHeroes.Any(h => h.Moving)) && Time.realtimeSinceStartup < deadline) yield return null;
                gm.Initialize(); gm.SetParty(new[] { "Aldric", "Rowan" }, "Aldric");
                gm.DebugResetCampaignProgress0833();
                var progressed = AscendanceSaveSystem.CaptureForAuthority0100(gm);
                progressed.PartyGold = 73; progressed.Round = 4;
                var aldric = progressed.Heroes.Single(h => h.Id == "Aldric");
                aldric.Level = 3; aldric.XP = 260; aldric.Health = 5;
                gm.ApplySaveData(progressed);
                for (int i = 0; i < 5; i++) yield return null;
                authority = new AscendanceRoomAuthority0100(gm, room, true);
                listener = new GameObject("ASCENDANCE_CAMPAIGN_SETUP_SOCKET");
                server = listener.AddComponent<AscendanceTcpServer0102>();
                Check(server.Initialize(authority, 0) && server.BoundPort > 0, "A persistent isolated room opens a real ephemeral listener.");
                port = server.BoundPort;
                foreach (string hero in HeroDatabase.Heroes.Select(h => h.id))
                {
                    string key = ReconnectKey(hero); prefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null; PlayerPrefs.DeleteKey(key);
                }
                string ownerKey = $"ASCENDANCE_0102_CAMPAIGN_OWNER_127.0.0.1_{port}_{room}";
                prefs[ownerKey] = PlayerPrefs.HasKey(ownerKey) ? PlayerPrefs.GetString(ownerKey) : null; PlayerPrefs.DeleteKey(ownerKey); PlayerPrefs.Save();
                // Construct independent-computer transports before a creator token is generated.
                var a = Client("Campaign creator"); var guest = Client("Old campaign guest");
                var b = Client("Draft B"); var c = Client("Draft C"); var d = Client("Draft D"); var e = Client("Draft fifth computer");
                var oldA = a.ClaimHeroAsync("Aldric"); var oldGuest = guest.ClaimHeroAsync("Rowan");
                yield return Await(Task.WhenAll(oldA, oldGuest));
                Check(oldA.Result.Accepted && oldGuest.Result.Accepted, "The original progressed campaign has two real owned Hero sessions.");
                string oldAId = a.PlayerId, oldACredential = PlayerPrefs.GetString(ReconnectKey("Aldric"));
                string before = Gameplay(AscendanceSaveSystem.CaptureForAuthority0100(gm));
                var blocked = a.CreateCampaignAsync(); yield return Await(blocked);
                Check(!blocked.Result.Accepted && blocked.Result.Code == "CAMPAIGN_SWITCH_BLOCKED", "New campaign cannot replace a campaign while a peer is connected.");
                Check(Gameplay(AscendanceSaveSystem.CaptureForAuthority0100(gm)) == before, "A refused creation leaves all original gameplay unchanged.");
                guest.Dispose();
                deadline = Time.realtimeSinceStartup + 12f;
                while (authority.Room.Players.Any(p => p.HeroId == "Rowan" && p.Connected) && Time.realtimeSinceStartup < deadline) yield return null;
                Check(!authority.Room.Players.Any(p => p.HeroId == "Rowan" && p.Connected), "The old peer is disconnected before the explicit switch.");
                byte[] beforeFile = File.ReadAllBytes(savePath);
                Directory.CreateDirectory(Path.Combine(slotFolder, "original.json"));
                var unsafeArchive = a.CreateCampaignAsync(); yield return Await(unsafeArchive);
                Check(!unsafeArchive.Result.Accepted && unsafeArchive.Result.Code == "CAMPAIGN_ARCHIVE_FAILED", "An occupied archive path refuses creation before active replacement.");
                Check(beforeFile.SequenceEqual(File.ReadAllBytes(savePath)) && Gameplay(AscendanceSaveSystem.CaptureForAuthority0100(gm)) == before,
                    "Failed archiving preserves the previous durable file and complete game.");
                Directory.Delete(Path.Combine(slotFolder, "original.json"));
                var created = a.CreateCampaignAsync(); yield return Await(created);
                Check(created.Result.Accepted && created.Result.Snapshot.CampaignLobby.IsDraft, "Explicit creation produces an unstarted server-owned campaign lobby.");
                Check(string.IsNullOrEmpty(a.PlayerId) && authority.Room.Players.Count == 0, "The new draft starts with no old player claims.");
                var all = HeroDatabase.Heroes.Select(h => h.id).OrderBy(x => x).ToArray();
                Check(all.Length == 16 && created.Result.Snapshot.CampaignLobby.AvailableHeroIds.OrderBy(x => x).SequenceEqual(all), "The new draft advertises exactly all sixteen authored Heroes.");
                var archive = AscendanceCampaignSlots0102.Load(room, "original");
                Check(Gameplay(archive.DurableCampaign) == before && archive.Players.Any(p => p.PlayerId == oldAId && p.ReconnectToken == oldACredential),
                    "The archived original retains complete progression and original private reconnect identity.");
                Check(created.Result.Snapshot.CampaignLobby.Slots.Any(s => s.Id == "original" && !s.IsActive), "The original campaign is listed as a separately loadable slot.");
                string managementCredential = PlayerPrefs.GetString(ownerKey);
                Check(Guid.TryParseExact(managementCredential, "N", out _) && !JsonUtility.ToJson(created.Result).Contains(managementCredential),
                    "The creator token is durable locally and absent from the public creation result.");
                var notReady = a.StartCampaignAsync(); yield return Await(notReady);
                Check(!notReady.Result.Accepted && notReady.Result.Code == "CAMPAIGN_NOT_READY", "An empty unready draft cannot start.");
                var claims = new[] { a.ClaimHeroAsync("Vaelis"), b.ClaimHeroAsync("Mira"), c.ClaimHeroAsync("Thorne"), d.ClaimHeroAsync("Cassian") };
                yield return Await(Task.WhenAll(claims));
                Check(claims.All(t => t.Result.Accepted), "Four different full-roster Heroes outside the old default party can be claimed.");
                Check(authority.Room.Players.Count == 4 && new[] { a, b, c, d }.Select(t => t.HeroId).Distinct().Count() == 4,
                    "One claim per connection produces exactly four distinct player Heroes.");
                string vaelisCredential = PlayerPrefs.GetString(ReconnectKey("Vaelis"));
                PlayerPrefs.DeleteKey(ReconnectKey("Vaelis"));
                var duplicate = e.ClaimHeroAsync("Vaelis");
                PlayerPrefs.SetString(ReconnectKey("Vaelis"), vaelisCredential);
                yield return Await(duplicate);
                Check(!duplicate.Result.Accepted && duplicate.Result.Code == "HERO_CLAIMED" && string.IsNullOrEmpty(e.PlayerId), "Another computer cannot claim the same draft Hero.");
                var fifth = e.ClaimHeroAsync("Elara"); yield return Await(fifth);
                Check(!fifth.Result.Accepted && fifth.Result.Code == "ROOM_FULL" && authority.Room.Players.Count == 4, "A fifth player cannot claim an otherwise available Hero.");
                var foreignStart = b.StartCampaignAsync(); yield return Await(foreignStart);
                Check(!foreignStart.Result.Accepted && foreignStart.Result.Code == "CAMPAIGN_NOT_READY", "A peer without the creator token cannot start the draft.");
                var ready = new[] { a.SetReadyAsync(true), b.SetReadyAsync(true), c.SetReadyAsync(true), d.SetReadyAsync(true) };
                yield return Await(Task.WhenAll(ready));
                Check(ready.All(t => t.Result.Accepted) && authority.Room.Players.All(p => p.ReadyForCampaign0102), "Every claimed player explicitly marks Ready.");
                var ownerView = a.RequestSnapshotAsync(); var peerView = b.RequestSnapshotAsync(); yield return Await(Task.WhenAll(ownerView, peerView));
                Check(ownerView.Result.Snapshot.CampaignLobby.CanStart && !peerView.Result.Snapshot.CampaignLobby.CanStart,
                    "Only the creator sees START enabled for the fully ready party.");
                var begun = a.StartCampaignAsync(); yield return Await(begun);
                Check(begun.Result.Accepted && !begun.Result.Snapshot.CampaignLobby.IsDraft, "Creator START changes the ready draft into a fresh campaign.");
                var fresh = begun.Result.Snapshot.Campaign;
                Check(fresh.PartyHeroIds.OrderBy(x => x).SequenceEqual(new[] { "Cassian", "Mira", "Thorne", "Vaelis" }), "The active party consists only of the four selected Heroes.");
                Check(fresh.QuestId == "Q001" && fresh.Round == 1 && fresh.PartyGold == 0 && string.IsNullOrEmpty(begun.Result.Snapshot.Transient.TurnHeroId),
                    "The new campaign genuinely starts at Q001 Round1 with fresh Gold and no active turn.");
                Check(fresh.Heroes.All(h => h.Level == 1 && h.XP == 0 && h.Health == gm.Heroes[h.Id].MaxHealth && h.Health > 0 && !h.Dead && !h.Downed),
                    "Selected Heroes begin at Level1, zero XP and their own maximum Health.");
                Check(fresh.Heroes.All(h => h.HandIds.Count == 5 && h.DeckIds.Count + h.HandIds.Count + h.DiscardIds.Count == 30),
                    "Every selected Hero has the legal five-card opening hand and thirty-card personal deck.");
                Check(fresh.Heroes.All(h => h.Actions == 0 && h.LocationId == "L000" && h.EquippedEquipmentIds.Count > 0),
                    "All selected Heroes start in Stonebrook with starting gear and no free turn actions.");
                Check(new[] { a, b, c, d }.All(t => !string.IsNullOrEmpty(t.PlayerId)) && authority.Room.Players.Count == 4,
                    "Starting retains the four existing draft socket owners.");
                Check(!JsonUtility.ToJson(begun.Result).Contains(managementCredential) && !JsonUtility.ToJson(begun.Result).Contains(vaelisCredential),
                    "Public start state exposes neither management nor reconnect credentials.");
                var deniedLoad = a.LoadCampaignAsync("original"); yield return Await(deniedLoad);
                Check(!deniedLoad.Result.Accepted && deniedLoad.Result.Code == "CAMPAIGN_SWITCH_BLOCKED", "Original-load cannot interrupt connected friends in the new game.");
                foreach (var client in new[] { a, b, c, d, e }) client.Dispose();
                deadline = Time.realtimeSinceStartup + 12f;
                while (authority.Room.Players.Any(p => p.Connected) && Time.realtimeSinceStartup < deadline) yield return null;
                var manager = Client("Returning creator");
                var connected = manager.ConnectAsync(); yield return Await(connected);
                Check(connected.Result && manager.LatestSnapshot.CampaignLobby.Slots.Any(s => s.Id == "original"), "The creator can reopen saved campaigns after players return to menus.");
                var loaded = manager.LoadCampaignAsync("original"); yield return Await(loaded);
                Check(loaded.Result.Accepted && Gameplay(loaded.Result.Snapshot.Campaign) == before, "Loading the original restores exact HP, Gold, progression and ordered card state.");
                Check(authority.Room.Players.Any(p => p.PlayerId == oldAId && p.ReconnectToken == oldACredential) && authority.Room.Players.All(p => !p.Connected),
                    "Original player identities return disconnected and still require their private reconnect proof.");
                var originalReturn = manager.ClaimHeroAsync("Aldric"); yield return Await(originalReturn);
                Check(originalReturn.Result.Accepted && manager.PlayerId == oldAId && gm.PartyGold == 73 && gm.Heroes["Aldric"].Health == 5,
                    "The original player reconnects to the same progressed Hero rather than a replacement.");
                manager.Dispose();
                deadline = Time.realtimeSinceStartup + 12f;
                while (authority.Room.Players.Any(p => p.Connected) && Time.realtimeSinceStartup < deadline) yield return null;
                server.StopServer(); authority.Dispose();
                Check(AscendanceServerRoomPersistence0100.TryLoad(room, out var disk, out _) && disk.CampaignOwnerToken0102 == managementCredential,
                    "A durable reload retains the creator identity and selected saved campaign.");
                gm.ApplySaveData(disk.DurableCampaign);
                authority = new AscendanceRoomAuthority0100(gm, room, true, disk);
                Check(authority.RestoredFromPersistence0102d && Gameplay(authority.Snapshot().Campaign) == before && AscendanceCampaignSlots0102.List(authority.Room).Count >= 2,
                    "Restart restoration retains the original game and both saved campaign slots.");
            }
            finally
            {
                foreach (var transport in transports) transport.Dispose();
                server?.StopServer(); authority?.Dispose();
                if (listener != null) UnityEngine.Object.Destroy(listener);
                foreach (var pair in prefs) { if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetString(pair.Key, pair.Value); }
                PlayerPrefs.Save();
                if (File.Exists(savePath)) File.Delete(savePath);
                if (Directory.Exists(slotFolder)) Directory.Delete(slotFolder, true);
                gm.ApplySaveData(restore);
            }
            Debug.Log($"{Prefix} PASS — Online Campaign Setup completed: {checks} passed, 0 failed.");
        }
    }
}
#endif
