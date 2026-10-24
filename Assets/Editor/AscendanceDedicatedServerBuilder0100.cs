#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class AscendanceDedicatedServerBuilder0100
    {
        private static readonly string[] ServerScenes = { "Assets/Scenes/Stonebrook.unity" };

        [MenuItem("Ascendance/Build/Build Windows Dedicated Server 0.10.2c")]
        public static void BuildWindowsDedicatedServer0100()
        {
            BuildServer(BuildTarget.StandaloneWindows64, "Builds/WindowsServer/AscendanceServer.exe", "Windows");
        }

        [MenuItem("Ascendance/Build/Build Linux Dedicated Server 0.10.2c")]
        public static void BuildLinuxDedicatedServer0102()
        {
            BuildServer(BuildTarget.StandaloneLinux64, "Builds/LinuxServer/AscendanceServer.x86_64", "Linux");
        }

        private static void BuildServer(BuildTarget target, string relativeOutput, string label)
        {
            PlayerSettings.companyName = "Ascendance";
            PlayerSettings.productName = "Ascendance";
            PlayerSettings.bundleVersion = AscendanceGameManager.BuildVersion;

            string output = AscendanceBuildArguments0102.OutputPath(relativeOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions
            {
                scenes = ServerScenes,
                locationPathName = output,
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[ASCENDANCE][SERVER] {label} dedicated-server build complete: {output} ({report.summary.totalSize / (1024f * 1024f):F1} MB)");
            else
                throw new BuildFailedException($"[ASCENDANCE][SERVER] {label} dedicated-server build failed: {report.summary.result}");
        }
    }
}
#endif
