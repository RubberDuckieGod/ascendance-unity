using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ascendance.Online;

namespace Ascendance
{
    /// <summary>
    /// Scene-transition-safe gameplay bootstrap. RuntimeInitializeOnLoadMethod itself only
    /// installs the listener; Stonebrook is built from sceneLoaded every time the scene is entered.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Remove first so Enter Play Mode options/domain-reload settings cannot double-subscribe.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Stonebrook") return;
            BootStonebrook();
        }

        private static void BootStonebrook()
        {
            if (Object.FindAnyObjectByType<AscendanceGameManager>() != null) return;

            AscendanceSettings.ApplySaved();
            ShellCameraBootstrap.EnsureCamera(false);

            var root = new GameObject("ASCENDANCE_RUNTIME");
            var manager = root.AddComponent<AscendanceGameManager>();
            var serverAuthority = root.AddComponent<AscendanceServerAuthority0100>();
            bool dedicatedServer = AscendanceDedicatedServerBootstrap0100.IsDedicatedServerProcess;
            AscendanceClientCommandRouter0101 clientRouter = null;
            AscendanceTcpServer0102 tcpServer = null;
            StonebrookWorldBuilder world = null;

            if (!dedicatedServer)
            {
                world = root.AddComponent<StonebrookWorldBuilder>();
                clientRouter = root.AddComponent<AscendanceClientCommandRouter0101>();
                root.AddComponent<AscendanceHUD>();
                root.AddComponent<CampaignHubUI0834>();
                root.AddComponent<QuestResultsUI0835>();
                root.AddComponent<DowntimeUI0836>();
                root.AddComponent<QuestBriefingUI0841>();
                root.AddComponent<CampaignEndUI0844>();
                root.AddComponent<CampaignLaunchpadUI0845>();
                root.AddComponent<CampaignResumeController0842>();
                root.AddComponent<CampaignAutoSave0842>();
                root.AddComponent<CRPGInputController>();
                root.AddComponent<MovementPathPreview>();
                root.AddComponent<CombatTimeEffects>();
                root.AddComponent<PauseMenuController>();
                EnsureEventSystem();
            }
            else
            {
                tcpServer = root.AddComponent<AscendanceTcpServer0102>();
            }

            // 0.10.2: the same rules engine runs in local presentation or headless server mode.
            // Presentation clients issue gameplay through the client command router. Local play
            // uses loopback; -ascendance-connect swaps in the persistent TCP transport. Dedicated
            // Linux/Windows server builds expose the same room authority over TCP.
            manager.Initialize();
            AscendanceSession.ApplyTo(manager);
            serverAuthority.Initialize(manager);
            if (clientRouter != null)
            {
                clientRouter.Initialize(manager, serverAuthority);
                AscendanceRemoteClientBootstrap0102.TryInstall(clientRouter, manager);
            }
            if (tcpServer != null) tcpServer.Initialize(serverAuthority);
            if (world != null) world.Build();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
