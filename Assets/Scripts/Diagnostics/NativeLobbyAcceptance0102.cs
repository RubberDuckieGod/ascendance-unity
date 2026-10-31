#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ascendance.Online;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ascendance.Diagnostics
{
    // Development-only opt-in driver. It clicks production uGUI controls and observes
    // the real lobby/transport; it never supplies a fake Hero roster or claim result.
    public sealed class NativeLobbyAcceptance0102 : MonoBehaviour
    {
        [Serializable] public sealed class Request { public string Id, Operation, HeroId, SlotId; }
        [Serializable] public sealed class Choice
        {
            public string HeroId, DisplayName, Status, ClaimedBy, RenderedLabel, RenderedName;
            public bool CanChoose, HasSavedCredential, Connected, IsOwnClaim, Ready, VisibleRect;
            public float[] ScreenRect;
        }
        [Serializable] public sealed class ButtonView { public string Name, Label; public bool Interactable; }
        [Serializable] public sealed class State
        {
            public string Scene, Status, SelectedHeroId, ClaimedHeroId, PlayerId, TurnHeroId, RoomId, ClaimCode, ClaimMessage;
            public string Build, PersistentDataPath, CampaignCode, CampaignMessage, ConfirmationTitle;
            public bool LobbyVisible, IsBusy, IsConnected, IsClaimed, GameReady, HasHud, HasLocalSave;
            public bool CampaignSupported, IsDraft, CanCreate, CanStart, IsReady, CampaignListVisible, ConfirmationVisible;
            public ServerCampaignLobby0102 CampaignLobby;
            public string[] PartyHeroIds;
            public int HeroGridVisible;
            public int Frame, AvatarCount, Round, Gold;
            public long CommandsSent, CommandsAccepted, GameplayRevision;
            public Choice[] Choices;
            public ButtonView[] Buttons;
            public ServerPlayerView0100[] Players;
            public string[] Errors;
        }
        [Serializable] public sealed class Reply { public string Id, Code, Message; public bool Accepted; public State State; }
        private string directory;
        private float nextState, deadline;
        private Request pending;
        private Action<ServerCommandResult0100> completion;
        private readonly List<string> errors = new();
        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            string output = Arg("-asc-lobby-output");
            if (string.IsNullOrWhiteSpace(output)) return;
            string config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (Environment.GetEnvironmentVariable("ASCENDANCE_NATIVE_LOBBY_ACCEPTANCE") != "1"
                || !Path.IsPathRooted(output) || string.IsNullOrWhiteSpace(config) || !Path.IsPathRooted(config)
                || Path.GetFullPath(config) != Path.Combine(Path.GetFullPath(output), "config")
                || !File.Exists(Path.Combine(output, ".ascendance-native-lobby"))
                || File.ReadAllText(Path.Combine(output, ".ascendance-native-lobby")).Trim() != "isolated-native-lobby-0102"
                || !AscendanceRemoteClientBootstrap0102.TryParseEndpoint(Arg("-ascendance-connect"), out var host, out int port)
                || host != "127.0.0.1" || port < 1024
                || !(Arg("-room") ?? "").StartsWith("NATIVE-LOBBY-", StringComparison.Ordinal)
                || !Application.persistentDataPath.StartsWith(Path.GetFullPath(config) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Native lobby acceptance requires a marked isolated development player and loopback test room.");
            var driver = new GameObject("ASCENDANCE_NATIVE_LOBBY_ACCEPTANCE").AddComponent<NativeLobbyAcceptance0102>();
            driver.directory = output;
            DontDestroyOnLoad(driver.gameObject);
            AscendanceSettings.SaveAndApply(false, 800, 600, .8f);
            Application.targetFrameRate = 30;
        }
        private void Start()
        {
            Directory.CreateDirectory(Path.Combine(directory, "requests"));
            Directory.CreateDirectory(Path.Combine(directory, "responses"));
            Application.logMessageReceived += CaptureError;
        }
        private void CaptureError(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text);
        }
        private void Update()
        {
            if (Time.realtimeSinceStartup >= nextState) { nextState = Time.realtimeSinceStartup + .15f; WriteState(); }
            if (pending != null && Time.realtimeSinceStartup > deadline)
            {
                var timedOut = pending; ClearPending(); Respond(timedOut, false, "COMMAND_TIMEOUT", "Production turn action did not complete.");
            }
            if (pending != null) return;
            string path = Directory.GetFiles(Path.Combine(directory, "requests"), "*.json").OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (path == null) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); File.Delete(path); }
            catch { errors.Add("Unreadable native lobby control request."); return; }
            if (request == null || !Guid.TryParseExact(request.Id, "N", out _)) { errors.Add("Invalid native lobby control request ID."); return; }
            switch (request.Operation)
            {
                case "quit": Respond(request, true, "QUIT", "Development player exit requested."); Application.Quit(0); return;
                case "state": Respond(request, true, "STATE", "Observed production UI and transport."); return;
                case "open": Click(request, "Button_JOIN_ONLINE_CAMPAIGN"); return;
                case "new": Click(request, "Button_NEW_ONLINE_CAMPAIGN", "OnlineCreateCampaignButton"); return;
                case "confirm-new": ClickLabel(request, "CREATE LOBBY"); return;
                case "ready": Click(request, "OnlineReadyButton"); return;
                case "start": Click(request, "OnlineStartCampaignButton"); return;
                case "saves": Click(request, "Button_SAVED_CAMPAIGNS"); return;
                case "select-save": Click(request, "OnlineCampaign_" + request.SlotId); return;
                case "load-save": Click(request, "OnlineLoadCampaignButton"); return;
                case "confirm-load": ClickLabel(request, "LOAD CAMPAIGN"); return;
                case "cancel": ClickLabel(request, "CANCEL"); return;
                case "next-saves": Click(request, "Button_NEXT"); return;
                case "previous-saves": Click(request, "Button_PREVIOUS"); return;
                case "select": Click(request, "OnlineHero_" + request.HeroId); return;
                case "claim": Click(request, "OnlineClaimHeroButton"); return;
                case "back": Click(request, "Button_BACK"); return;
                case "menu": AscendanceSession.ReturnToMainMenu(); Respond(request, true, "MENU", "Normal session return to menu requested."); return;
                case "turn":
                    var router = AscendanceClientCommandRouter0101.Instance;
                    if (router == null || !router.IsReady) { Respond(request, false, "NOT_READY", "Production router is not ready."); return; }
                    pending = request; deadline = Time.realtimeSinceStartup + 20f;
                    completion = result => { var finished = pending; ClearPending(); Respond(finished, result?.Accepted == true, result?.Code, result?.Message); };
                    router.CommandCompleted += completion;
                    router.RequestStartOrEndTurn(); return;
                default: Respond(request, false, "BAD_DRIVER_REQUEST", "Known UI operation required."); return;
            }
        }
        private void Click(Request request, params string[] names)
        {
            var button = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => names.Contains(b.gameObject.name) && b.gameObject.activeInHierarchy);
            if (button == null || !button.interactable) { Respond(request, false, "BUTTON_UNAVAILABLE", "Production control unavailable: " + string.Join(" / ", names)); return; }
            button.onClick.Invoke();
            Respond(request, true, "CLICKED", "Production control clicked: " + button.gameObject.name);
        }
        private void ClickLabel(Request request, string label)
        {
            var dialog = FindObjectsByType<ShellConfirmationDialog0843>(FindObjectsSortMode.None).FirstOrDefault(d => d.IsOpen);
            var button = dialog?.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b => b.gameObject.activeInHierarchy &&
                b.GetComponentInChildren<UnityEngine.UI.Text>()?.text == label);
            if (button == null || !button.interactable) { Respond(request, false, "BUTTON_UNAVAILABLE", "Production confirmation unavailable: " + label); return; }
            button.onClick.Invoke();
            Respond(request, true, "CLICKED", "Production confirmation clicked: " + label);
        }
        private static float[] ScreenRect(UnityEngine.UI.Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return null;
            var corners = new Vector3[4];
            button.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return new[] { min.x, min.y, max.x, max.y };
        }
        private static bool VisibleRect(float[] rect) => rect != null && rect[2] - rect[0] > 2 && rect[3] - rect[1] > 2 &&
            rect[0] >= 0 && rect[1] >= 0 && rect[2] <= Screen.width && rect[3] <= Screen.height;
        private State Observe()
        {
            var menu = FindAnyObjectByType<MainMenuUI>();
            var lobby = menu?.OnlineLobby;
            var router = AscendanceClientCommandRouter0101.Instance;
            var remote = router?.Transport as AscendanceTcpCommandTransport0102;
            var gm = AscendanceGameManager.Instance;
            if (SceneManager.GetActiveScene().name != "Stonebrook" || gm == null) { gm = null; router = null; remote = null; }
            var snapshot = router?.LastSnapshot ?? lobby?.Snapshot;
            var texts = FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None);
            var buttons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Where(b => b.gameObject.activeInHierarchy).ToArray();
            var dialog = FindObjectsByType<ShellConfirmationDialog0843>(FindObjectsSortMode.None).FirstOrDefault(d => d.IsOpen);
            return new State
            {
                Scene = SceneManager.GetActiveScene().name, Status = lobby?.Status,
                SelectedHeroId = lobby?.SelectedHeroId ?? gm?.SelectedHeroId,
                ClaimedHeroId = remote?.HeroId ?? lobby?.ClaimedHeroId, PlayerId = remote?.PlayerId ?? lobby?.PlayerId, TurnHeroId = gm?.TurnHeroId,
                RoomId = snapshot?.RoomId, Build = AscendanceGameManager.BuildVersion, PersistentDataPath = Application.persistentDataPath,
                ClaimCode = lobby?.LastClaimResult?.Code, ClaimMessage = lobby?.LastClaimResult?.Message,
                CampaignCode = lobby?.LastCampaignResult?.Code, CampaignMessage = lobby?.LastCampaignResult?.Message,
                CampaignSupported = lobby?.SupportsCampaignSetup == true, IsDraft = lobby?.IsDraft == true,
                CanCreate = lobby?.CanCreateCampaign == true, CanStart = lobby?.CanStartCampaign == true,
                IsReady = lobby?.IsReady == true, CampaignListVisible = menu?.OnlineSavedCampaignsVisible == true,
                CampaignLobby = snapshot?.CampaignLobby, PartyHeroIds = snapshot?.Campaign?.PartyHeroIds?.ToArray() ?? Array.Empty<string>(),
                ConfirmationVisible = dialog != null, ConfirmationTitle = dialog?.DebugTitle0843,
                HeroGridVisible = buttons.Count(b => b.name.StartsWith("OnlineHero_", StringComparison.Ordinal) && VisibleRect(ScreenRect(b))),
                LobbyVisible = menu?.OnlineLobbyVisible == true, IsBusy = lobby?.IsBusy == true,
                IsConnected = lobby?.IsConnected == true || remote?.IsConnected == true,
                IsClaimed = lobby?.IsClaimed == true || !string.IsNullOrEmpty(remote?.PlayerId),
                GameReady = router?.IsReady == true, HasHud = gm != null && gm.GetComponent<AscendanceHUD>() != null,
                HasLocalSave = AscendanceSaveSystem.HasSave, Frame = Time.frameCount,
                AvatarCount = gm?.HeroAvatars.Count ?? 0, Round = gm?.Round ?? 0, Gold = gm?.PartyGold ?? 0,
                CommandsSent = router?.CommandsSent ?? 0, CommandsAccepted = router?.CommandsAccepted ?? 0,
                GameplayRevision = snapshot?.GameplayRevision ?? -1,
                Choices = lobby?.HeroChoices.Select(c => new Choice
                { HeroId = c.HeroId, DisplayName = c.DisplayName, Status = c.Status, ClaimedBy = c.ClaimedBy,
                  CanChoose = c.CanChoose, HasSavedCredential = c.HasSavedCredential, Connected = c.Connected,
                  IsOwnClaim = c.IsOwnClaim, Ready = c.Ready,
                  RenderedLabel = texts.FirstOrDefault(t => t.gameObject.name == "OnlineHeroStatus_" + c.HeroId)?.text,
                  RenderedName = buttons.FirstOrDefault(b => b.name == "OnlineHero_" + c.HeroId)?.GetComponentInChildren<UnityEngine.UI.Text>()?.text,
                  ScreenRect = ScreenRect(buttons.FirstOrDefault(b => b.name == "OnlineHero_" + c.HeroId)),
                  VisibleRect = VisibleRect(ScreenRect(buttons.FirstOrDefault(b => b.name == "OnlineHero_" + c.HeroId))) }).ToArray() ?? Array.Empty<Choice>(),
                Buttons = buttons.Select(b => new ButtonView { Name = b.gameObject.name, Label = b.GetComponentInChildren<UnityEngine.UI.Text>()?.text, Interactable = b.interactable }).ToArray(),
                Players = snapshot?.Players?.ToArray() ?? Array.Empty<ServerPlayerView0100>(), Errors = errors.ToArray()
            };
        }
        private void ClearPending()
        {
            if (completion != null && AscendanceClientCommandRouter0101.Instance != null) AscendanceClientCommandRouter0101.Instance.CommandCompleted -= completion;
            completion = null; pending = null;
        }
        private void WriteState() => AtomicWrite(Path.Combine(directory, "state.json"), JsonUtility.ToJson(Observe(), true));
        private void Respond(Request request, bool accepted, string code, string message)
        {
            if (request == null) return;
            AtomicWrite(Path.Combine(directory, "responses", request.Id + ".json"), JsonUtility.ToJson(new Reply { Id = request.Id, Accepted = accepted, Code = code, Message = message, State = Observe() }, true));
            WriteState();
        }
        private static void AtomicWrite(string path, string data)
        {
            File.WriteAllText(path + ".tmp", data);
            if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
        }
        private void OnDestroy() { ClearPending(); Application.logMessageReceived -= CaptureError; }
    }
}
#endif
