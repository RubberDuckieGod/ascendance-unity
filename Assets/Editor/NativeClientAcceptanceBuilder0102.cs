#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Ascendance.EditorTools
{
    public static class NativeClientAcceptanceBuilder0102
    {
        // Development player only; this does not change the shipping Windows builder.
        public static void BuildLinuxClient()
        {
            string output = AscendanceBuildArguments0102.OutputPath("artifacts/native-acceptance/LinuxClient/AscendanceClient.x86_64");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Stonebrook.unity" },
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                locationPathName = output,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Native acceptance Linux client failed: " + report.summary.result);
        }
    }
}
#endif
