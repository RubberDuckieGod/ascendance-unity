using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    // Client-only art input. It never downloads packages or reads room/save data.
    public sealed class LocationArtBundleLibrary0103 : ILocationArtLibrary0103
    {
        public const string RelativeManifestPath = "AscendanceArt/location-art.json";
        private readonly string directory;
        private readonly Dictionary<string, LocationArtBundle0103> bundles;
        private readonly Dictionary<string, LocationArtProfile0103> profiles;
        private readonly Dictionary<string, LocationArtBinding0103> bindings;
        private sealed class SharedBundle
        {
            public string Path, Hash;
            public AssetBundle Bundle;
            public int References;
        }
        // Scene preview, validation and actor presentation can share a client
        // library. Unity cannot load the same bundle file twice independently.
        private static readonly Dictionary<string, SharedBundle> Shared = new(Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        private readonly Dictionary<string, SharedBundle> loaded = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> failures = new(StringComparer.Ordinal);
        private bool disposed;
        public LocationArtManifest0103 Manifest { get; }
        private static readonly Regex Identifier = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$");
        private static readonly Regex BundleFilename = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,127}\.bundle$");
        private static readonly Regex Checksum = new(@"^[0-9a-fA-F]{64}$");

        private LocationArtBundleLibrary0103(string path, LocationArtManifest0103 manifest)
        {
            directory = Path.GetDirectoryName(path); Manifest = manifest;
            bundles = manifest.bundles.ToDictionary(b => b.id, StringComparer.Ordinal);
            profiles = manifest.profiles.ToDictionary(p => p.id, StringComparer.Ordinal);
            bindings = manifest.locations.ToDictionary(l => l.authoredLocationId, StringComparer.OrdinalIgnoreCase);
        }

        public static string CurrentPlatform => Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer
            ? "StandaloneWindows64" : Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer
            ? "StandaloneLinux64" : null;

        public static string DefaultManifestPath()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string preview = Environment.GetEnvironmentVariable("ASCENDANCE_ART_MANIFEST");
            if (!string.IsNullOrWhiteSpace(preview))
            {
                if (!Path.IsPathRooted(preview)) throw new InvalidDataException("Art preview manifest must use an absolute path.");
                return Path.GetFullPath(preview);
            }
#endif
            return Path.Combine(Application.streamingAssetsPath, RelativeManifestPath);
        }

        public static bool TryOpen(string path, out LocationArtBundleLibrary0103 library, out string failure)
        {
            library = null; failure = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) throw new InvalidDataException("Art manifest needs an absolute path.");
                path = Path.GetFullPath(path);
                if (!File.Exists(path)) return false; // Missing client art is a quiet prototype fallback.
                RequireRegularPath(path);
                var info = new FileInfo(path);
                if (info.Length <= 0 || info.Length > 4 * 1024 * 1024) throw new InvalidDataException("Art manifest size is invalid.");
                var manifest = JsonUtility.FromJson<LocationArtManifest0103>(File.ReadAllText(path));
                Validate(manifest, CurrentPlatform);
                library = new LocationArtBundleLibrary0103(path, manifest);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException)
            { failure = ex.Message; return false; }
        }

        public static void RequireRegularPath(string path)
        {
            path = Path.GetFullPath(path);
            for (string cursor = path; !string.IsNullOrEmpty(cursor); cursor = Path.GetDirectoryName(cursor))
            {
                if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Art paths cannot contain symbolic links or reparse points.");
                if (string.Equals(cursor, Path.GetPathRoot(cursor), StringComparison.Ordinal)) break;
            }
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.Directory) != 0) throw new InvalidDataException("Art input must be a regular file.");
        }

        public static void Validate(LocationArtManifest0103 manifest, string platform)
        {
            void Require(bool ok, string message) { if (!ok) throw new InvalidDataException(message); }
            Require(manifest != null && manifest.format == 1, "Unsupported location art manifest format.");
            Require(platform != null && manifest.platform == platform, "Art bundle platform differs from this client.");
            Require(manifest.bundles != null && manifest.profiles != null && manifest.locations != null,
                "Art manifest arrays are required.");
            Require(manifest.bundles.Length <= 64 && manifest.profiles.Length <= 1024 && manifest.locations.Length <= LocationArtCatalog0103.Definitions.Count,
                "Art manifest exceeds its supported catalog limits.");
            var bundleIds = new HashSet<string>(StringComparer.Ordinal);
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var bundle in manifest.bundles)
            {
                Require(bundle != null && Identifier.IsMatch(bundle.id ?? "") && bundleIds.Add(bundle.id), "Missing or duplicate art bundle ID.");
                Require(BundleFilename.IsMatch(bundle.file ?? "") && !bundle.file.Contains("..") && files.Add(bundle.file), "Art bundle filename must be a unique leaf .bundle file.");
                Require(Checksum.IsMatch(bundle.sha256 ?? ""), "Art bundle needs its exact SHA-256.");
            }
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in manifest.profiles)
            {
                Require(profile != null && Identifier.IsMatch(profile.id ?? "") && profileIds.Add(profile.id), "Missing or duplicate art profile ID.");
                Require(profile.coverage == LocationArtCoverage0103.ComponentsOnly || profile.coverage == LocationArtCoverage0103.FullReviewed, "An art profile must declare partial components or reviewed coverage.");
                Require(profile.parts != null && profile.parts.Length > 0 && profile.parts.Length <= 96 && profile.landmarks != null && profile.reviewEvidence != null, "Art profile parts and coverage metadata are required.");
                if (profile.coverage == LocationArtCoverage0103.FullReviewed) Require(profile.reviewEvidence.Any(e => !string.IsNullOrWhiteSpace(e)), "Reviewed profiles need review evidence.");
                foreach (var part in profile.parts)
                {
                    Require(part != null && bundleIds.Contains(part.bundleId ?? "") && IsAssetName(part.assetName), "Art part references an unknown bundle or invalid asset name.");
                    Require(Finite(part.position) && Finite(part.rotation) && Finite(part.scale) && part.scale.x > 0 && part.scale.y > 0 && part.scale.z > 0,
                        "Art part transforms must be finite with positive scale.");
                    Require(part.position.magnitude <= 100 && part.scale.x <= 100 && part.scale.y <= 100 && part.scale.z <= 100,
                        "Art part transform exceeds decorative bounds.");
                }
            }
            var locationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var binding in manifest.locations)
            {
                Require(binding != null && LocationArtCatalog0103.TryGet(binding.authoredLocationId, out _) && locationIds.Add(binding.authoredLocationId), "Unknown or duplicate authored Location binding.");
                Require(LocationArtCoverage0103.IsValid(binding.coverage) && binding.missingLandmarks != null && binding.reviewEvidence != null, "Location coverage declaration is invalid.");
                binding.replaceLegacy ??= Array.Empty<string>();
                Require(binding.replaceLegacy.Length <= 4 && binding.replaceLegacy.Distinct(StringComparer.Ordinal).Count() == binding.replaceLegacy.Length
                    && binding.replaceLegacy.All(id => id == "stonebrook-house-0" || id == "stonebrook-house-1" || id == "stonebrook-house-2" || id == "stonebrook-house-3")
                    && (binding.replaceLegacy.Length == 0 || string.Equals(binding.authoredLocationId, "L000", StringComparison.OrdinalIgnoreCase)),
                    "Only Stonebrook may explicitly replace its registered legacy house renderers.");
                if (binding.coverage == LocationArtCoverage0103.Unbound)
                { Require(string.IsNullOrEmpty(binding.profileId) && binding.replaceLegacy.Length == 0, "An unbound Location cannot claim an art profile or legacy replacement."); continue; }
                Require(profileIds.Contains(binding.profileId ?? ""), "Location references an unknown art profile.");
                var profile = manifest.profiles.First(p => p.id == binding.profileId);
                if (binding.coverage == LocationArtCoverage0103.FullReviewed)
                {
                    Require(profile.coverage == LocationArtCoverage0103.FullReviewed && binding.reviewEvidence.Any(e => !string.IsNullOrWhiteSpace(e)) && MissingLandmarks(binding, profile).Length == 0,
                        "Reviewed Location coverage needs evidence and every authored landmark.");
                }
            }
        }

        private static bool Finite(Vector3 value) => !(float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.z));
        private static bool IsAssetName(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 1024 && char.IsLetterOrDigit(name[0])
            && name.All(c => char.IsLetterOrDigit(c) || "_ ./()'&-".IndexOf(c) >= 0)
            && name.Split('/').All(p => !string.IsNullOrWhiteSpace(p) && p != "." && p != "..");

        public static string[] MissingLandmarks(LocationArtBinding0103 binding, LocationArtProfile0103 profile)
        {
            if (!LocationArtCatalog0103.TryGet(binding?.authoredLocationId, out var definition)) return Array.Empty<string>();
            var present = new HashSet<string>(profile?.landmarks ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            return definition.RequiredLandmarks.Where(l => !present.Contains(l))
                .Concat(binding?.missingLandmarks ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public bool TryGetBinding(string authoredId, out LocationArtBinding0103 binding, out LocationArtProfile0103 profile)
        {
            binding = null; profile = null;
            return !string.IsNullOrEmpty(authoredId) && bindings.TryGetValue(authoredId, out binding)
                && binding.coverage != LocationArtCoverage0103.Unbound && profiles.TryGetValue(binding.profileId, out profile);
        }

        public GameObject LoadPart(LocationArtPart0103 part, out string failure)
        {
            failure = null;
            if (disposed) { failure = "Art library is closed."; return null; }
            if (!bundles.TryGetValue(part?.bundleId ?? "", out var definition)) { failure = "Art bundle is not bound."; return null; }
            if (failures.TryGetValue(definition.id, out failure)) return null;
            try
            {
                if (!loaded.TryGetValue(definition.id, out var shared))
                {
                    string path = Path.Combine(directory, definition.file);
                    RequireRegularPath(path);
                    string hash;
                    using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(hash, definition.sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Art bundle SHA-256 differs from its manifest.");
                    if (Shared.TryGetValue(path, out shared))
                    {
                        if (shared.Bundle == null || !string.Equals(shared.Hash, hash, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("Art bundle changed while another presentation library is using it.");
                    }
                    else
                    {
                        var bundle = AssetBundle.LoadFromFile(path);
                        if (bundle == null) throw new InvalidDataException("Art bundle could not be loaded for this client.");
                        shared = new SharedBundle { Path = path, Hash = hash, Bundle = bundle };
                        Shared.Add(path, shared);
                    }
                    shared.References++;
                    loaded.Add(definition.id, shared);
                }
                var prefab = shared.Bundle.LoadAsset<GameObject>(part.assetName);
                if (prefab == null) { failure = "Art prefab was not found in its bundle."; return null; }
                if (!IsPresentationPrefab(prefab, out failure)) return null;
                return prefab;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException)
            { failures[definition.id] = failure = ex.Message; return null; }
        }

        public static bool IsPresentationPrefab(GameObject prefab, out string failure)
        {
            failure = null;
            if (prefab == null) { failure = "Art prefab is missing."; return false; }
            // Inspect the asset before Instantiate: disabling a demo script after
            // its Awake ran would be too late. Curated bundles contain no scripts.
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                if (component == null || component is MonoBehaviour)
                { failure = "Art prefab contains an uncurated script component."; return false; }
            if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0)
            { failure = "Art prefab contains no visible renderer."; return false; }
            return true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var shared in loaded.Values)
            {
                shared.References--;
                if (shared.References > 0) continue;
                if (shared.Bundle != null) shared.Bundle.Unload(false);
                Shared.Remove(shared.Path);
            }
            loaded.Clear(); failures.Clear();
        }
    }
}
