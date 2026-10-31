#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ascendance.Online;
using UnityEngine;

namespace Ascendance.Diagnostics
{
    // Opted-in development-player test driver. Shipping builds contain no driver.
    // Every gameplay request goes through the same router and TCP transport as the HUD.
    public sealed class NativeClientAcceptance0102 : MonoBehaviour
    {
        [Serializable] public sealed class Request
        {
            public string Id, Operation, Type, ActorHeroId, TargetId, SecondaryTargetId, CardId, OptionKey, StringValue;
            public int IntValue;
            public bool BoolValue;
        }
        [Serializable] public sealed class HeroView
        {
            public string Id, LocationId, PendingLocationId;
            public int Health, MaxHealth, Actions, AttackOnlyActions, Potions;
            public bool Moving, TurnCompleted, Downed, Dead;
            public string[] Hand, Discard;
        }
        [Serializable] public sealed class LocationView
        {
            public string Id;
            public bool Revealed;
            public string[] Connections;
        }
        [Serializable] public sealed class EnemyView
        {
            public string Id, LocationId;
            public int Health, Armor;
        }
        [Serializable] public sealed class AvatarScreenView
        {
            public string HeroId;
            public float[] ScreenPosition;
        }
        [Serializable] public sealed class QuestObjectView
        {
            public string Id, Type, LocationId;
            public bool Rescued, Destroyed;
        }
        [Serializable] public sealed class ButtonView
        {
            public string Name, Label;
            public bool Active, Interactable;
        }
        [Serializable] public sealed class LocationArtView
        {
            public string RuntimeLocationId, AuthoredLocationId, ProfileId, EffectiveCoverage;
            public int PartsInstantiated, PartsMissing, EnabledRenderers, VisibleRenderers;
            public bool RootActive, InteractionCenterClear, DecorativeCollidersDisabled;
            public string[] Failures, MissingLandmarks;
        }
        [Serializable] public sealed class ArtView
        {
            public bool PresenterInstalled;
            public int ActiveLocations, SuppressedLegacyHouses;
            public string BindingFailure;
            public LocationArtView[] Locations;
        }
        [Serializable] public sealed class State
        {
            public string HeroId, PlayerId, SelectedHeroId, TurnHeroId, ServerInstanceId, RoomId, Build, PersistentDataPath, ConnectionError;
            public long Revision, GameplayRevision, CommandsSent, CommandsAccepted;
            public int Frame, Round, Gold, AvatarCount, VisibleWorldRenderers, VisibleHeroRenderers, ScreenWidth, ScreenHeight, VillagersRescued, VillagersAvailable, VillagersLost;
            public bool Ready, Connected, Dedicated, HasHud, IsResolving, PendingReaction, PendingReward, PendingDeckControl, RejectedConnect, RunInBackground, IsFocused, QuestComplete, QuestFailed;
            public float[] CameraPosition, PointerPosition;
            public HeroView[] Heroes;
            public LocationView[] Locations;
            public EnemyView[] Enemies;
            public AvatarScreenView[] AvatarScreens;
            public QuestObjectView[] QuestObjects;
            public ButtonView[] Buttons;
            public string[] Errors;
            public ArtView Art;
        }
        [Serializable] public sealed class Reply
        {
            public string Id, Code, Message;
            public bool Accepted;
            public State State;
        }

        private string directory, hero;
        private readonly List<string> errors = new();
        private float nextState, commandDeadline;
        private Request pending;
        private Action<ServerCommandResult0100> completion;
        private bool reconnecting, rejectionProbe, rejectedConnect;
        private string connectionError;

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            string output = Argument("-asc-native-output");
            if (string.IsNullOrWhiteSpace(output)) return;
            string config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string endpoint = Argument("-ascendance-connect");
            if (Environment.GetEnvironmentVariable("ASCENDANCE_NATIVE_ACCEPTANCE") != "1"
                || !Path.IsPathRooted(output) || string.IsNullOrWhiteSpace(config) || !Path.IsPathRooted(config)
                || Path.GetFullPath(config) != Path.Combine(Path.GetFullPath(output), "config")
                || !File.Exists(Path.Combine(output, ".ascendance-native-client"))
                || File.ReadAllText(Path.Combine(output, ".ascendance-native-client")).Trim() != "isolated-native-0.10.2h"
                || !AscendanceRemoteClientBootstrap0102.TryParseEndpoint(endpoint, out var host, out int port)
                || host != "127.0.0.1" || port < 1024
                || !(Argument("-room") ?? "").StartsWith("NATIVE-ACCEPTANCE-", StringComparison.Ordinal)
                || !Application.persistentDataPath.StartsWith(Path.GetFullPath(config) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Native acceptance requires an isolated, marked development-player directory and loopback test server.");
            var runner = new GameObject("ASCENDANCE_NATIVE_CLIENT_ACCEPTANCE").AddComponent<NativeClientAcceptance0102>();
            runner.directory = output;
            runner.hero = Argument("-asc-native-hero") ?? "Aldric";
            runner.rejectionProbe = Argument("-asc-native-reject-probe") == "1";
            DontDestroyOnLoad(runner.gameObject);
            // Normal settings API, scoped to the validated temporary PlayerPrefs.
            // MainMenu will apply these saved windowed settings instead of the
            // default fullscreen mode, which hides a peer in the private X display.
            AscendanceSettings.SaveAndApply(false, 800, 600, .8f);
            Application.targetFrameRate = 30;
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += CaptureError;
            Directory.CreateDirectory(Path.Combine(directory, "requests"));
            Directory.CreateDirectory(Path.Combine(directory, "responses"));
            yield return null;
            AscendanceSession.StartNewGame(HeroDatabase.DefaultParty, hero);
            yield return ReadyAndJoin(null);
        }

        private IEnumerator ReadyAndJoin(Request request)
        {
            reconnecting = true;
            float deadline = Time.realtimeSinceStartup + 25f;
            if (rejectionProbe)
            {
                while (AscendanceClientCommandRouter0101.Instance?.Transport is not AscendanceTcpCommandTransport0102
                    && Time.realtimeSinceStartup < deadline) yield return null;
                if (AscendanceClientCommandRouter0101.Instance?.Transport is AscendanceTcpCommandTransport0102 probe)
                {
                    rejectedConnect = !probe.TryConnect(out connectionError);
                    if (!rejectedConnect) errors.Add("Negative native handshake probe unexpectedly connected.");
                }
                else errors.Add("Negative native handshake probe has no production TCP transport.");
                reconnecting = false; WriteState(); yield break;
            }
            while (AscendanceClientCommandRouter0101.Instance?.IsReady != true && Time.realtimeSinceStartup < deadline) yield return null;
            var router = AscendanceClientCommandRouter0101.Instance;
            if (router?.IsReady != true || router.Transport is not AscendanceTcpCommandTransport0102 remote)
            {
                errors.Add("Native client did not receive its initial authoritative snapshot.");
                if (request != null) Respond(request, false, "READY_TIMEOUT", errors.Last());
            }
            else
            {
                bool joined = remote.TryResolvePlayerForHero(hero, out _);
                if (!joined) errors.Add("Native client could not bind its own Hero session.");
                if (request != null) Respond(request, joined, joined ? "RECONNECTED" : "JOIN_FAILED", "Production transport Hero ownership result.");
            }
            reconnecting = false;
            WriteState();
        }

        private void CaptureError(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(directory)) return;
            if (Time.realtimeSinceStartup >= nextState) { nextState = Time.realtimeSinceStartup + .15f; WriteState(); }
            if (pending != null && Time.realtimeSinceStartup > commandDeadline)
            {
                var failed = pending; ClearPending(); Respond(failed, false, "COMMAND_TIMEOUT", "The real router did not complete its command.");
            }
            if (pending != null || reconnecting) return;
            string path = Directory.GetFiles(Path.Combine(directory, "requests"), "*.json").OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            if (path == null) return;
            Request request;
            try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); File.Delete(path); }
            catch (Exception ex) { errors.Add("Control request: " + ex.Message); return; }
            if (request == null || !Guid.TryParseExact(request.Id, "N", out _)) { errors.Add("Control request ID is missing."); return; }
            var router = AscendanceClientCommandRouter0101.Instance;
            if (request.Operation == "quit") { Respond(request, true, "QUIT", "Native player exit requested."); Application.Quit(0); return; }
            if (request.Operation == "state") { Respond(request, true, "STATE", "Observed production replica."); return; }
            if (request.Operation == "select-view")
            {
                var manager=AscendanceGameManager.Instance;
                if(manager==null||!manager.Heroes.ContainsKey(request.TargetId??string.Empty))
                {Respond(request,false,"HERO_NOT_FOUND","A known Hero is required.");return;}
                manager.SelectHero(request.TargetId);
                Respond(request,manager.SelectedHeroId==request.TargetId,"VIEW_SELECTED","Production Hero inspection selection.");return;
            }
            if (request.Operation == "campaign-create" || request.Operation == "campaign-ready" || request.Operation == "campaign-start")
            { StartCoroutine(CampaignAction(request)); return; }
            if (request.Operation == "reconnect")
            {
                router.SetTransport(new AscendanceTcpCommandTransport0102(AscendanceRemoteClientBootstrap0102.Host,
                    AscendanceRemoteClientBootstrap0102.Port, AscendanceRemoteClientBootstrap0102.RoomId, AscendanceRemoteClientBootstrap0102.DisplayName,
                    new AscendanceClientTransportSecurity0102(AscendanceRemoteClientBootstrap0102.TlsEnabled,
                        AscendanceRemoteClientBootstrap0102.CertificateSha256, Environment.GetEnvironmentVariable("ASCENDANCE_ROOM_ACCESS_KEY"))));
                _ = ((AscendanceTcpCommandTransport0102)router.Transport).ConnectAsync();
                StartCoroutine(ReadyAndJoin(request)); return;
            }
            bool hudButton=request.Operation=="hud-button";
            ServerCommandType0100 type=default;
            if (!hudButton && (request.Operation != "send" || !Enum.TryParse(request.Type, out type)))
            { Respond(request, false, "BAD_DRIVER_REQUEST", "A known production command is required."); return; }
            UnityEngine.UI.Button button=null;
            if(hudButton)
            {
                // Click the actual rendered HUD control, including its production
                // callback and ownership wrapper. Never bypass disabled controls.
                if(request.TargetId!="EndTurn"&&request.TargetId!="ResolveReaction"&&request.TargetId!="Rescue")
                {Respond(request,false,"BAD_HUD_BUTTON","Only combat acceptance controls are allowed.");return;}
                button=FindHudButton(request.TargetId);
                if(button==null||!button.gameObject.activeInHierarchy||!button.IsInteractable())
                {Respond(request,false,"HUD_BUTTON_UNAVAILABLE","The real HUD control is absent or disabled.");return;}
            }
            pending = request; commandDeadline = Time.realtimeSinceStartup + 20f;
            completion = result =>
            {
                var finished = pending; ClearPending();
                Respond(finished, result?.Accepted == true, result?.Code ?? "NO_RESULT", result?.Message);
            };
            router.CommandCompleted += completion;
            if(hudButton)button.onClick.Invoke();
            else router.Send(type, request.ActorHeroId ?? hero, request.TargetId, request.SecondaryTargetId, request.CardId,
                    request.OptionKey, request.StringValue, request.IntValue, request.BoolValue);
        }

        private IEnumerator CampaignAction(Request request)
        {
            reconnecting=true;
            var remote=AscendanceClientCommandRouter0101.Instance?.Transport as AscendanceTcpCommandTransport0102;
            if(remote==null){reconnecting=false;Respond(request,false,"NO_REMOTE","A real remote transport is required.");yield break;}
            var task=request.Operation=="campaign-create"?remote.CreateCampaignAsync():request.Operation=="campaign-start"?remote.StartCampaignAsync():remote.SetReadyAsync(true);
            float deadline=Time.realtimeSinceStartup+20f;
            while(!task.IsCompleted&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!task.IsCompleted||task.IsFaulted||task.IsCanceled)
            {reconnecting=false;Respond(request,false,"CAMPAIGN_ACTION_FAILED","Production campaign action did not complete.");yield break;}
            var result=task.Result;
            bool accepted=result.Accepted;
            if(accepted&&request.Operation=="campaign-create")accepted=remote.TryResolvePlayerForHero(hero,out _);
            reconnecting=false;
            Respond(request,accepted,accepted?result.Code:"CAMPAIGN_ACTION_REJECTED",result.Message);
        }

        private static UnityEngine.UI.Button FindHudButton(string name)
        {
            var hud=AscendanceGameManager.Instance?.GetComponent<AscendanceHUD>();
            return hud==null?null:hud.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b=>b.gameObject.name==name);
        }

        private void ClearPending()
        {
            if (completion != null && AscendanceClientCommandRouter0101.Instance != null)
                AscendanceClientCommandRouter0101.Instance.CommandCompleted -= completion;
            completion = null; pending = null;
        }

        private State Observe()
        {
            var gm = AscendanceGameManager.Instance; var router = AscendanceClientCommandRouter0101.Instance;
            var remote = router?.Transport as AscendanceTcpCommandTransport0102; var snapshot = router?.LastSnapshot;
            var camera = Camera.main;
            var world = GameObject.Find("Stonebrook_World");
            return new State
            {
                HeroId = hero, PlayerId = remote?.PlayerId, SelectedHeroId = gm?.SelectedHeroId, TurnHeroId = gm?.TurnHeroId,
                ServerInstanceId = snapshot?.ServerInstanceId, RoomId = snapshot?.RoomId, Build = AscendanceGameManager.BuildVersion,
                PersistentDataPath = Application.persistentDataPath, Frame = Time.frameCount,
                ConnectionError = connectionError, RejectedConnect = rejectedConnect,
                Revision = snapshot?.Revision ?? -1, GameplayRevision = snapshot?.GameplayRevision ?? -1,
                CommandsSent = router?.CommandsSent ?? 0, CommandsAccepted = router?.CommandsAccepted ?? 0,
                Round = gm?.Round ?? 0, Gold = gm?.PartyGold ?? 0, AvatarCount = gm?.HeroAvatars?.Count ?? 0,
                VillagersRescued=gm?.VillagersRescued??0,VillagersAvailable=gm?.VillagersAvailable??0,VillagersLost=gm?.VillagersLost??0,
                QuestComplete=gm?.QuestComplete==true,QuestFailed=gm?.QuestFailed==true,
                Ready = router?.IsReady == true, Connected = remote?.IsConnected == true,
                Dedicated = AscendanceDedicatedServerBootstrap0100.IsDedicatedServerProcess,
                HasHud = gm != null && gm.GetComponent<AscendanceHUD>() != null,
                RunInBackground = Application.runInBackground,
                IsFocused = Application.isFocused,
                ScreenWidth = Screen.width, ScreenHeight = Screen.height,
                PointerPosition = new[] { Input.mousePosition.x, Input.mousePosition.y },
                CameraPosition = camera == null ? Array.Empty<float>() : new[] { camera.transform.position.x, camera.transform.position.y, camera.transform.position.z },
                VisibleWorldRenderers = world == null ? 0 : world.GetComponentsInChildren<Renderer>().Count(r => r.enabled && r.isVisible),
                VisibleHeroRenderers = gm?.HeroAvatars?.Values.Where(a => a != null).SelectMany(a => a.GetComponentsInChildren<Renderer>()).Count(r => r.enabled && r.isVisible) ?? 0,
                AvatarScreens = gm == null || camera == null ? Array.Empty<AvatarScreenView>() : gm.HeroAvatars.Where(pair => pair.Value != null).Select(pair =>
                {
                    Vector3 point = camera.WorldToScreenPoint(pair.Value.transform.position + Vector3.up);
                    return new AvatarScreenView { HeroId = pair.Key, ScreenPosition = new[] { point.x, point.y, point.z } };
                }).ToArray(),
                IsResolving = gm?.IsResolving == true, PendingReaction = gm?.HasPendingReactionWindow == true,
                PendingReward = gm?.HasPendingRewardChoice == true, PendingDeckControl = gm?.HasPendingEncounterDeckControl == true,
                Heroes = gm?.ParticipatingHeroes.Select(h => new HeroView { Id = h.Id, Health = h.Health, MaxHealth = h.MaxHealth,
                    Actions = h.Actions, AttackOnlyActions = h.AttackOnlyActions, Potions = h.HealingPotions,
                    LocationId = h.LocationId, PendingLocationId = h.PendingLocationId, Moving = h.Moving,
                    TurnCompleted = h.TurnCompleted, Downed = h.Downed, Dead = h.Dead,
                    Hand = h.Hand.Select(c => c.Id).ToArray(), Discard = h.Discard.ToArray() }).ToArray() ?? Array.Empty<HeroView>(),
                Locations = gm?.Locations.Values.Select(l => new LocationView { Id = l.Id, Revealed = l.Revealed, Connections = l.Connections.ToArray() }).ToArray() ?? Array.Empty<LocationView>(),
                Enemies = gm?.Enemies.Values.Select(e => new EnemyView { Id = e.Id, Health = e.Health, Armor = e.Armor, LocationId = e.LocationId }).ToArray() ?? Array.Empty<EnemyView>(),
                QuestObjects=gm?.QuestObjects0829.Select(o=>new QuestObjectView{Id=o.ObjectId,Type=o.Type,LocationId=o.LocationId,Rescued=o.Rescued,Destroyed=o.Destroyed}).ToArray()??Array.Empty<QuestObjectView>(),
                Buttons=new[]{"EndTurn","ResolveReaction","Rescue"}.Select(name=>
                {var button=FindHudButton(name);return new ButtonView{Name=name,Active=button!=null&&button.gameObject.activeInHierarchy,Interactable=button!=null&&button.IsInteractable(),Label=button?.GetComponentInChildren<UnityEngine.UI.Text>()?.text};}).ToArray(),
                Art = ObserveArt(world, gm),
                Errors = errors.ToArray()
            };
        }

        // Read-only diagnostics, compiled only into explicitly opted-in Editor
        // and development players. An installed component is not a full art review.
        private static ArtView ObserveArt(GameObject world, AscendanceGameManager game)
        {
            var presenter = world == null ? null : world.GetComponent<LocationArtPresenter0103>();
            return new ArtView
            {
                PresenterInstalled = presenter != null,
                ActiveLocations = presenter?.ActiveLocationCount ?? 0,
                SuppressedLegacyHouses = presenter?.SuppressedLegacyHouseCount ?? 0,
                BindingFailure = presenter?.BindingFailure,
                Locations = presenter == null ? Array.Empty<LocationArtView>() : presenter.BindingAttempts.Select(view =>
                {
                    var root = presenter.RootForRuntimeLocation(view.RuntimeLocationId);
                    bool hasAnchor = game != null && game.Locations.TryGetValue(view.RuntimeLocationId, out _);
                    var anchor = hasAnchor ? game.Locations[view.RuntimeLocationId].Position : Vector3.zero;
                    var renderers = root == null ? Array.Empty<Renderer>() : root.GetComponentsInChildren<Renderer>(true);
                    return new LocationArtView
                    {
                        RuntimeLocationId = view.RuntimeLocationId, AuthoredLocationId = view.AuthoredLocationId,
                        ProfileId = view.ProfileId, EffectiveCoverage = view.EffectiveCoverage,
                        PartsInstantiated = view.PartsInstantiated, PartsMissing = view.PartsMissing,
                        EnabledRenderers = renderers.Count(r => r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy),
                        VisibleRenderers = renderers.Count(r => r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy && r.isVisible),
                        RootActive = root != null && root.activeInHierarchy,
                        InteractionCenterClear = root != null && hasAnchor && LocationArtPresenter0103.LeavesInteractionCenterClear(root, anchor),
                        DecorativeCollidersDisabled = root != null && root.GetComponentsInChildren<Collider>(true).All(c => !c.enabled),
                        Failures = view.Failures ?? Array.Empty<string>(), MissingLandmarks = view.MissingLandmarks ?? Array.Empty<string>()
                    };
                }).ToArray()
            };
        }

        private void WriteState() => AtomicWrite(Path.Combine(directory, "state.json"), JsonUtility.ToJson(Observe(), true));
        private void Respond(Request request, bool accepted, string code, string message)
        {
            if (request == null) return;
            AtomicWrite(Path.Combine(directory, "responses", request.Id + ".json"), JsonUtility.ToJson(new Reply
            { Id = request.Id, Accepted = accepted, Code = code, Message = message, State = Observe() }, true));
            WriteState();
        }
        private static void AtomicWrite(string path, string data)
        {
            File.WriteAllText(path + ".tmp", data);
            if (File.Exists(path)) File.Replace(path + ".tmp", path, null);
            else File.Move(path + ".tmp", path);
        }
        private void OnDestroy() { ClearPending(); Application.logMessageReceived -= CaptureError; }
    }
}
#endif
