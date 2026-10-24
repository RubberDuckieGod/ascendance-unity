#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class AscendanceStandaloneBuilder
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Stonebrook.unity"
        };

        [MenuItem("Ascendance/Build/Build Windows Standalone 0.10.2c")]
        public static void BuildWindowsStandalone()
        {
            PlayerSettings.companyName = "Ascendance";
            PlayerSettings.productName = "Ascendance";
            PlayerSettings.bundleVersion = AscendanceGameManager.BuildVersion;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

            string output = Path.GetFullPath("Builds/Windows/Ascendance.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[ASCENDANCE] Windows build complete: {output} ({summary.totalSize / (1024f * 1024f):F1} MB)");
                EditorUtility.RevealInFinder(output);
            }
            else
            {
                Debug.LogError($"[ASCENDANCE] Windows build failed: {summary.result}");
            }
        }
    }
}
#endif
