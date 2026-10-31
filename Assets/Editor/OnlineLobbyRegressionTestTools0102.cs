#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class OnlineLobbyRegressionTestTools0102
    {
        private const string Prefix = "[ASCENDANCE][ONLINE LOBBY]";

        [MenuItem("Ascendance/Validation/Test Online Hero Lobby (Play Mode)")]
        public static void TestLobby()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            var gm = AscendanceGameManager.Instance;
            if (!Application.isPlaying || gm == null) { Debug.LogError(Prefix + " Play Mode with a game manager is required."); return; }
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Await(Task task)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        }

        private static T Completed<T>(Task<T> task) where T : class => task.Status == TaskStatus.RanToCompletion ? task.Result : null;
        private static string Gameplay(AscendanceGameManager gm)
        {
            var save = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            save.SavedAtUtc = null;
            return JsonUtility.ToJson(save);
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int passed = 0, failed = 0;
            void Check(bool ok, string label)
            {
                if (ok) { passed++; Debug.Log(Prefix + " PASS — " + label); }
                else { failed++; Debug.LogError(Prefix + " FAIL — " + label); }
            }
            var restore = AscendanceSaveSystem.CaptureForAuthority0100(gm);
            var clients = new List<AscendanceTcpCommandTransport0102>();
            var originalPrefs = new Dictionary<string, string>();
            AscendanceRoomAuthority0100 authority = null;
            GameObject socketObject = null;
            AscendanceOnlineLobby0102 lobby = null;
            string room = "LOBBY-EDITOR-" + Guid.NewGuid().ToString("N");
            int port = 0;
            string Key(string id) => $"ASCENDANCE_0102_RECONNECT_127.0.0.1_{port}_{room}_{id}";
            AscendanceTcpCommandTransport0102 Client(string name)
            {
                var client = new AscendanceTcpCommandTransport0102("127.0.0.1", port, room, name);
                clients.Add(client);
                return client;
            }
            try
            {
                float settle = Time.realtimeSinceStartup + 15f;
                while ((gm.IsResolving || gm.HasPendingReactionWindow || gm.ParticipatingHeroes.Any(h => h.Moving)) && Time.realtimeSinceStartup < settle) yield return null;
                gm.SetParty(new[] { "Aldric", "Lyra", "Garruk", "Elara" }, "Aldric");
                gm.DebugResetCampaignProgress0833();
                for (int i = 0; i < 10; i++) yield return null;
                authority = new AscendanceRoomAuthority0100(gm, room, false);
                socketObject = new GameObject("ASCENDANCE_EDITOR_LOBBY_SOCKET");
                var server = socketObject.AddComponent<AscendanceTcpServer0102>();
                Check(server.Initialize(authority, 0), "Isolated existing campaign opens a real ephemeral socket.");
                port = server.BoundPort;
                foreach (string id in new[] { "Aldric", "Lyra", "Garruk", "Elara", "Rowan" })
                {
                    string key = Key(id);
                    originalPrefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
                    PlayerPrefs.DeleteKey(key);
                }
                string before = Gameplay(gm);
                long gameplayRevision = authority.Room.GameplayRevision;
                var a = Client("Lobby A");
                var b = Client("Lobby B");
                Check(!a.HasSavedHeroCredential("Aldric") && !PlayerPrefs.HasKey(Key("Aldric")), "Availability inspection does not create a reconnect credential.");
                var ca = a.ConnectAsync(); var cb = b.ConnectAsync();
                yield return Await(Task.WhenAll(ca, cb));
                Check(ca.Status == TaskStatus.RanToCompletion && ca.Result && cb.Status == TaskStatus.RanToCompletion && cb.Result,
                    "Two independent transports receive real authoritative lobby snapshots.");
                Check(authority.Room.Players.Count == 0 && a.HeroId == null && b.HeroId == null,
                    "Opening the lobby claims no Hero or gameplay action.");
                Check(a.LatestSnapshot?.Campaign?.PartyHeroIds?.SequenceEqual(new[] { "Aldric", "Lyra", "Garruk", "Elara" }) == true,
                    "Lobby roster is the saved server party rather than the default solo roster.");
                var claimA = a.ClaimHeroAsync("Aldric");
                var claimB = b.ClaimHeroAsync("Lyra");
                yield return Await(Task.WhenAll(claimA, claimB));
                var ra = Completed(claimA); var rb = Completed(claimB);
                Check(ra?.Accepted == true && rb?.Accepted == true, "Two different Heroes are explicitly claimed before any gameplay.");
                Check(a.HeroId == "Aldric" && b.HeroId == "Lyra" && !string.IsNullOrEmpty(a.PlayerId) && a.PlayerId != b.PlayerId,
                    "Each connection owns exactly its selected Hero and a distinct session.");
                Check(authority.Room.Players.Count == 2, "Selecting one Hero does not reserve the remaining party slots.");
                Check(Gameplay(gm) == before && authority.Room.GameplayRevision == gameplayRevision,
                    "Connect and explicit claims leave the complete campaign and gameplay revision unchanged.");
                Check(a.HasSavedHeroCredential("Aldric") && Guid.TryParseExact(PlayerPrefs.GetString(Key("Aldric")), "N", out _),
                    "Explicit claim persists the original reconnect-key format before network ownership.");
                string originalCredential = PlayerPrefs.GetString(Key("Aldric"));
                Check(ra != null && !JsonUtility.ToJson(ra).Contains(originalCredential),
                    "Public claim results expose no reconnect token.");

                PlayerPrefs.DeleteKey(Key("Aldric"));
                var rival = Client("Other computer");
                var lost = rival.ClaimHeroAsync("Aldric");
                PlayerPrefs.SetString(Key("Aldric"), originalCredential);
                yield return Await(lost);
                var rejected = Completed(lost);
                Check(rejected?.Accepted == false && rejected.Code == "HERO_CLAIMED", "A different client cannot steal an already claimed Hero; exact refusal code survives.");
                Check(rejected?.Message?.Contains("another player") == true && string.IsNullOrEmpty(rival.PlayerId),
                    "Claim refusal remains actionable and creates no rival session.");
                var wrongParty = rival.ClaimHeroAsync("Rowan");
                yield return Await(wrongParty);
                Check(Completed(wrongParty)?.Code == "HERO_UNAVAILABLE", "A Hero outside the saved party cannot be claimed.");
                var switchHero = a.ClaimHeroAsync("Garruk");
                yield return Await(switchHero);
                Check(Completed(switchHero)?.Accepted == false && a.HeroId == "Aldric", "An established connection cannot silently switch its owned Hero.");
                string savedPlayer = a.PlayerId;
                a.Dispose();
                for (int i = 0; i < 10; i++) yield return null;
                var returned = Client("Returned owner");
                var reconnect = returned.ClaimHeroAsync("Aldric");
                yield return Await(reconnect);
                Check(Completed(reconnect)?.Accepted == true && returned.PlayerId == savedPlayer && returned.HeroId == "Aldric",
                    "A fresh connection with the saved credential recovers the same Hero session.");
                Check(authority.Room.Players.Count == 2 && Gameplay(gm) == before,
                    "Reconnect adds no duplicate player and preserves the entire campaign.");

                // Exercise the production lobby model against the same real socket.
                var observer = Client("Lobby observer");
                lobby = new AscendanceOnlineLobby0102();
                lobby.Open(observer);
                float ready = Time.realtimeSinceStartup + 15f;
                while (!lobby.IsConnected && Time.realtimeSinceStartup < ready) { lobby.Tick(); yield return null; }
                lobby.Tick();
                Check(lobby.IsConnected && lobby.HeroChoices.Count == 4, "Production lobby model exposes only the four authoritative party Heroes.");
                Check(lobby.HeroChoices.Any(h => h.HeroId == "Garruk" && h.CanChoose) && !lobby.HeroChoices.Any(h => h.HeroId == "Rowan"),
                    "Unclaimed saved-party Heroes are offered; unrelated authored Heroes are excluded.");
                Check(lobby.HeroChoices.Any(h => h.HeroId == "Aldric" && h.HasSavedCredential),
                    "A saved local credential is presented as a reconnect option without exposing its token.");
                Check(lobby.SelectHero("Garruk") && lobby.SelectedHeroId == "Garruk" && lobby.SelectHero("Elara") && lobby.SelectedHeroId == "Elara",
                    "The online selection holds exactly one Hero, replacing its previous choice.");
                Check(!lobby.SelectHero("Rowan") && lobby.SelectedHeroId == "Elara", "Selection refuses a Hero outside the saved online party.");
                Check(Gameplay(gm) == before && authority.Room.GameplayRevision == gameplayRevision,
                    "Lobby inspection and selection remain read-only for gameplay.");
            }
            finally
            {
                lobby?.Dispose();
                foreach (var client in clients) client.Dispose();
                if (socketObject != null) { socketObject.GetComponent<AscendanceTcpServer0102>()?.StopServer(); UnityEngine.Object.Destroy(socketObject); }
                authority?.Dispose();
                foreach (var pref in originalPrefs) { if (pref.Value == null) PlayerPrefs.DeleteKey(pref.Key); else PlayerPrefs.SetString(pref.Key, pref.Value); }
                PlayerPrefs.Save();
                if (restore != null) gm.ApplySaveData(restore);
            }
            string terminal = $"Online Hero Lobby completed: {passed} passed, {failed} failed.";
            if (failed == 0) Debug.Log(Prefix + " PASS — " + terminal); else Debug.LogError(Prefix + " FAIL — " + terminal);
        }
    }
}
#endif
