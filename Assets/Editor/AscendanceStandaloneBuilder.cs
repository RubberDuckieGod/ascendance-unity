#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
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

            string output = AscendanceBuildArguments0102.OutputPath("Builds/Windows/Ascendance.exe");
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
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(output);
            }
            else
            {
                throw new BuildFailedException($"[ASCENDANCE] Windows build failed: {summary.result}");
            }
        }
    }

    internal static class AscendanceBuildArguments0102
    {
        internal static string OutputPath(string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], "-buildOutput", StringComparison.OrdinalIgnoreCase)) continue;
                if (i + 1 == args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException("-buildOutput requires an explicit output file path.");
                return Path.GetFullPath(args[i + 1]);
            }
            return Path.GetFullPath(fallback);
        }
    }
}
#endif
