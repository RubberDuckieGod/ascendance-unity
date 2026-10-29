using System;
using System.IO;
using UnityEngine;

namespace Ascendance
{
    /// <summary>Player storage is unchanged. Only an explicitly opted-in Editor test can override it.</summary>
    public static class AscendanceStoragePaths0102
    {
        public static string PersistentDataPath
        {
            get
            {
#if UNITY_EDITOR
                if (Environment.GetEnvironmentVariable("ASCENDANCE_TEST_MODE") == "1")
                    return RequireIsolatedValidationDirectory();
#endif
                return Application.persistentDataPath;
            }
        }

#if UNITY_EDITOR
        public const string ValidationSentinel = ".ascendance-validation-data";
        public const string ValidationSentinelContents = "ascendance-unity-validation-0.10.2h";

        public static string RequireIsolatedValidationDirectory()
        {
            string requested = Environment.GetEnvironmentVariable("ASCENDANCE_TEST_DATA_DIR");
            if (Environment.GetEnvironmentVariable("ASCENDANCE_TEST_MODE") != "1"
                || string.IsNullOrWhiteSpace(requested) || !Path.IsPathRooted(requested))
                throw new InvalidOperationException("Validation requires ASCENDANCE_TEST_MODE=1 and an absolute ASCENDANCE_TEST_DATA_DIR.");
            string root = Path.GetFullPath(requested).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string production = Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            StringComparison comparison = Application.platform == RuntimePlatform.WindowsEditor ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (string.IsNullOrEmpty(root) || root == Path.GetPathRoot(root)
                || production.Equals(root, comparison)
                || production.StartsWith(root + Path.DirectorySeparatorChar, comparison)
                || root.StartsWith(production + Path.DirectorySeparatorChar, comparison))
                throw new InvalidOperationException("Validation storage must be separate from normal player saves and filesystem roots.");
            string sentinel = Path.Combine(root, ValidationSentinel);
            if (!Directory.Exists(root) || !File.Exists(sentinel) || File.ReadAllText(sentinel).Trim() != ValidationSentinelContents)
                throw new InvalidOperationException("Validation storage sentinel is missing. Use tools/run_unity_validation.sh or tools/Run-UnityValidation.ps1.");
            return root;
        }
#endif
    }
}
