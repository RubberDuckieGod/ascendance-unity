#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Ascendance.Online;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ascendance.EditorTools
{
    /// <summary>
    /// Asynchronous batch entry point. Launch without -quit: this bridge owns the exit
    /// after Play Mode, coroutine completion, and every required terminal verdict.
    /// SessionState survives the domain reload when entering Play Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class AscendanceBatchValidation0102
    {
        private const string Key = "Ascendance.BatchValidation0102.";
        private const string Prefix = "[ASCENDANCE][BATCH VALIDATION]";
        private static readonly ConcurrentQueue<LogEntry> PendingLogs = new ConcurrentQueue<LogEntry>();
        private static readonly List<string> Errors = new List<string>();
        private static readonly Terminal[] Required =
        {
            new Terminal("master", "[ASCENDANCE][0.10.2h TEST]", "Master Save/Load + Full Regression completed:", 54),
            new Terminal("foundation", "[ASCENDANCE][0.10.2h SERVER]", "Dedicated Server Authority Foundation completed:", 24),
            new Terminal("router", "[ASCENDANCE][0.10.2h ROUTER]", "Client Command Routing completed:", 29),
            new Terminal("network", "[ASCENDANCE][0.10.2h NETWORK]", "Real Network Transport completed:", 27),
            new Terminal("reliability", "[ASCENDANCE][0.10.2h RELIABILITY]", "Network Reliability completed:", 22),
            new Terminal("snapshot", "[ASCENDANCE TEST 0.10.2 SNAPSHOT]", " checks.", 20),
            new Terminal("lobby", "[ASCENDANCE][ONLINE LOBBY]", "Online Hero Lobby completed:", 32),
            new Terminal("campaign", "[ASCENDANCE][ONLINE CAMPAIGN]", "Online Campaign Setup completed:", 42)
        };

        static AscendanceBatchValidation0102()
        {
            if (SessionState.GetBool(Key + "active", false)) Subscribe();
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch validation requires a separate batch-mode Editor.");
            if (Application.isPlaying) throw new InvalidOperationException("Start batch validation outside Play Mode.");
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
                if (string.Equals(arg, "-quit", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Do not pass -quit to asynchronous batch validation; the bridge exits after its verdict.");

            string results = Argument(args, "-ascendance-results");
            if (string.IsNullOrWhiteSpace(results)) throw new ArgumentException("-ascendance-results must name the JSON result file.");
            if (!int.TryParse(Argument(args, "-ascendance-timeout") ?? "1800", out int seconds) || seconds < 60 || seconds > 7200)
                throw new ArgumentException("-ascendance-timeout must be between 60 and 7200 seconds.");
            // Fail closed before opening a scene or running any save-corruption checks.
            // Editor authentication/configuration is deliberately left in place.
            string dataRoot = AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();

            SessionState.SetString(Key + "results", Path.GetFullPath(results));
            SessionState.SetString(Key + "storage", Path.GetFullPath(dataRoot));
            SessionState.SetString(Key + "deadline", DateTime.UtcNow.AddSeconds(seconds).Ticks.ToString(CultureInfo.InvariantCulture));
            SessionState.SetInt(Key + "stage", -1);
            SessionState.SetInt(Key + "errors", 0);
            SessionState.SetBool(Key + "enteredPlay", false);
            foreach (Terminal terminal in Required)
            {
                SessionState.SetBool(Key + terminal.Name + ".passed", false);
                SessionState.SetInt(Key + terminal.Name + ".count", 0);
                SessionState.SetString(Key + terminal.Name + ".message", "");
            }
            SessionState.SetBool(Key + "active", true);
            Subscribe();
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Stonebrook.unity", OpenSceneMode.Single);
                Debug.Log(Prefix + " Entering Stonebrook Play Mode; waiting for the complete runtime fixture.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex) { Finish(false, "Could not start Play Mode: " + ex); }
        }

        private static void Subscribe()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) SessionState.SetBool(Key + "enteredPlay", true);
            if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(Key + "enteredPlay", false))
                Finish(false, "Play Mode exited before all required gates completed.");
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            // Unity can log TCP worker errors off the main thread; marshal all Editor
            // and SessionState operations to Tick instead of losing those failures.
            PendingLogs.Enqueue(new LogEntry { Message = message ?? "", Type = type });
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            try
            {
                while (PendingLogs.TryDequeue(out LogEntry entry)) ProcessLog(entry);
                if (SessionState.GetInt(Key + "errors", 0) != 0)
                {
                    Finish(false, "Unity emitted Error, Exception, or Assert entries.");
                    return;
                }
                if (!long.TryParse(SessionState.GetString(Key + "deadline", ""), out long deadline) || DateTime.UtcNow.Ticks >= deadline)
                {
                    Finish(false, "Validation deadline expired while waiting for readiness or required gate completion.");
                    return;
                }
                if (!Application.isPlaying || !SessionState.GetBool(Key + "enteredPlay", false)) return;
                var gm = AscendanceGameManager.Instance;
                var world = UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>();
                var router = AscendanceClientCommandRouter0101.Instance;
                int stage = SessionState.GetInt(Key + "stage", -1);
                if (stage == -1)
                {
                    if (gm == null || world == null || !world.PresentationSpawnReady0862 || router == null || !router.IsReady) return;
                    if (AscendanceDedicatedServerBootstrap0100.IsDedicatedServerProcess)
                        throw new InvalidOperationException("Editor validation incorrectly booted as a dedicated-server player.");
                    string expectedStorage = SessionState.GetString(Key + "storage", "");
                    if (!string.Equals(AscendanceStoragePaths0102.RequireIsolatedValidationDirectory(), expectedStorage, StringComparison.Ordinal))
                        throw new InvalidOperationException("Validation storage changed while entering Play Mode.");
                    SessionState.SetInt(Key + "stage", 0);
                    NetworkTransportTestTools0102.Test0102();
                }
                else if (stage == 0 && Passed(0) && Passed(1) && Passed(2) && Passed(3))
                {
                    SessionState.SetInt(Key + "stage", 1);
                    NetworkReliabilityTestTools0102.Test0102Reliability();
                }
                else if (stage == 1 && Passed(4))
                {
                    SessionState.SetInt(Key + "stage", 2);
                    RemoteSnapshotRegressionTestTools0102.ValidateCurrentPlayMode();
                }
                else if (stage == 2 && Passed(5))
                {
                    SessionState.SetInt(Key + "stage", 3);
                    OnlineLobbyRegressionTestTools0102.TestLobby();
                }
                else if (stage == 3 && Passed(6))
                {
                    SessionState.SetInt(Key + "stage", 4);
                    OnlineCampaignSetupRegressionTestTools0102.TestCampaignSetup();
                }
                else if (stage == 4 && Passed(7)) Finish(true, "Every required inherited, network, lobby, and campaign setup gate completed.");
            }
            catch (Exception ex) { Finish(false, "Batch bridge failure: " + ex); }
        }

        private static bool Passed(int index) => SessionState.GetBool(Key + Required[index].Name + ".passed", false);

        private static void ProcessLog(LogEntry entry)
        {
            if (entry.Type == LogType.Error || entry.Type == LogType.Exception || entry.Type == LogType.Assert)
            {
                SessionState.SetInt(Key + "errors", SessionState.GetInt(Key + "errors", 0) + 1);
                if (Errors.Count < 12) Errors.Add(entry.Message);
            }
            foreach (Terminal terminal in Required)
            {
                if (!entry.Message.Contains(terminal.Prefix) || !entry.Message.Contains(terminal.Completion)) continue;
                Match match = terminal.Name == "snapshot"
                    ? Regex.Match(entry.Message, @"PASS — (\d+) checks\.")
                    : Regex.Match(entry.Message, @"completed: (\d+) passed, 0 failed\.");
                int observed = match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
                bool passed = match.Success && entry.Message.Contains(terminal.Prefix + " PASS")
                    && observed > 0 && (terminal.Expected == 0 || observed == terminal.Expected);
                SessionState.SetInt(Key + terminal.Name + ".count", observed);
                SessionState.SetString(Key + terminal.Name + ".message", entry.Message);
                SessionState.SetBool(Key + terminal.Name + ".passed", passed);
                if (!passed)
                {
                    SessionState.SetInt(Key + "errors", SessionState.GetInt(Key + "errors", 0) + 1);
                    Errors.Add("Required gate returned an unexpected verdict/count: " + entry.Message);
                }
            }
        }

        private static void Finish(bool passed, string reason)
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            SessionState.SetBool(Key + "active", false);
            Application.logMessageReceivedThreaded -= OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            var verdict = new Verdict
            {
                passed = passed,
                reason = reason,
                editorVersion = Application.unityVersion,
                buildVersion = AscendanceGameManager.BuildVersion,
                errorCount = SessionState.GetInt(Key + "errors", 0),
                errors = Errors.ToArray()
            };
            foreach (Terminal terminal in Required)
                verdict.gates.Add(new GateVerdict
                {
                    name = terminal.Name,
                    passed = SessionState.GetBool(Key + terminal.Name + ".passed", false),
                    expectedChecks = terminal.Expected,
                    observedChecks = SessionState.GetInt(Key + terminal.Name + ".count", 0),
                    terminalMessage = SessionState.GetString(Key + terminal.Name + ".message", "")
                });
            try
            {
                string path = SessionState.GetString(Key + "results", "");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(verdict, true));
            }
            catch (Exception ex)
            {
                passed = false;
                Debug.LogError(Prefix + " Failed to write result JSON: " + ex);
            }
            CampaignAutoSave0842.SuppressNextQuitSave0843();
            Debug.Log(Prefix + (passed ? " PASS — " : " FAIL — ") + reason);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        private sealed class Terminal
        {
            internal readonly string Name, Prefix, Completion;
            internal readonly int Expected;
            internal Terminal(string name, string prefix, string completion, int expected)
            { Name = name; Prefix = prefix; Completion = completion; Expected = expected; }
        }
        private struct LogEntry { internal string Message; internal LogType Type; }
        [Serializable] private sealed class Verdict
        {
            public bool passed;
            public string reason, editorVersion, buildVersion;
            public int errorCount;
            public string[] errors;
            public List<GateVerdict> gates = new List<GateVerdict>();
        }
        [Serializable] private sealed class GateVerdict
        {
            public string name, terminalMessage;
            public bool passed;
            public int expectedChecks, observedChecks;
        }
    }
}
#endif
