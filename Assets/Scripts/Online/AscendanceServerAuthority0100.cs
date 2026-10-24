using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ascendance.Online
{
    public sealed class AscendanceServerAuthority0100 : MonoBehaviour
    {
        public static AscendanceServerAuthority0100 Instance { get; private set; }
        public AscendanceRoomAuthority0100 Authority { get; private set; }
        public bool IsDedicatedServerProcess => AscendanceDedicatedServerBootstrap0100.IsDedicatedServerProcess;
        public string RoomId => Authority?.Room?.RoomId;

        private void Awake() => Instance = this;

        public void Initialize(AscendanceGameManager gm)
        {
            if (Authority != null || gm == null) return;
            string roomId = AscendanceDedicatedServerBootstrap0100.RequestedRoomId;
            PersistedServerRoom0100 restored = null;
            if (IsDedicatedServerProcess && AscendanceServerRoomPersistence0100.TryLoad(roomId, out var loaded, out _))
            {
                restored = loaded;
                if (loaded.DurableCampaign != null) gm.ApplySaveData(loaded.DurableCampaign);
            }
            Authority = new AscendanceRoomAuthority0100(gm, roomId, IsDedicatedServerProcess, restored);
            gm.AddLog($"SERVER AUTHORITY — room {Authority.Room.RoomId} owns campaign state at revision {Authority.Room.Revision}. No player-host authority is used.");
            if (IsDedicatedServerProcess) Debug.Log($"[ASCENDANCE][SERVER] Dedicated authority online. Room={Authority.Room.RoomId} Build={AscendanceGameManager.BuildVersion}");
        }

        private void OnDestroy()
        {
            Authority?.Dispose();
            Authority = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }

    public static class AscendanceDedicatedServerBootstrap0100
    {
        public static bool IsDedicatedServerProcess { get; private set; }
        public static string RequestedRoomId { get; private set; } = "LOCAL-ROOM";
        public static int RequestedPort { get; private set; } = 7777;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            string[] args = Environment.GetCommandLineArgs() ?? Array.Empty<string>();
            // Batch-mode Editors still need the normal presentation fixture for validation.
            // Dedicated player builds identify themselves even without a -batchmode flag.
#if UNITY_SERVER
            const bool dedicatedBuild = true;
#else
            const bool dedicatedBuild = false;
#endif
            IsDedicatedServerProcess = dedicatedBuild || (!Application.isEditor && Application.isBatchMode)
                || args.Any(a => string.Equals(a, "-ascendance-server", StringComparison.OrdinalIgnoreCase));
            string envPort = Environment.GetEnvironmentVariable("ASCENDANCE_PORT");
            if (int.TryParse(envPort, out var parsedEnvPort)) RequestedPort = Mathf.Clamp(parsedEnvPort, 1, 65535);
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-room", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args[i + 1])) RequestedRoomId = args[i + 1].Trim();
                if (string.Equals(args[i], "-port", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out var parsedPort)) RequestedPort = Mathf.Clamp(parsedPort, 1, 65535);
            }
            if (!IsDedicatedServerProcess) return;
            SceneManager.sceneLoaded -= ForceServerScene;
            SceneManager.sceneLoaded += ForceServerScene;
        }

        private static void ForceServerScene(Scene scene, LoadSceneMode mode)
        {
            if (!IsDedicatedServerProcess || scene.name == "Stonebrook") return;
            SceneManager.LoadScene("Stonebrook");
        }
    }
}
