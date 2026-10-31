#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Ascendance.EditorTools
{
    // Framework checks use an explicit primitive fixture, not Store art and not
    // an assertion of realistic/complete art for any of the 236 Locations.
    public static class LocationArtRegressionTestTools0103
    {
        private const string Prefix = "[ASCENDANCE][LOCATION ART]";

        private sealed class FixtureLibrary : ILocationArtLibrary0103
        {
            public LocationArtManifest0103 Manifest;
            public GameObject Prefab;
            public FixtureLibrary(LocationArtManifest0103 manifest, GameObject prefab)
            { LocationArtBundleLibrary0103.Validate(manifest, LocationArtBundleLibrary0103.CurrentPlatform); Manifest = manifest; Prefab = prefab; }
            public bool TryGetBinding(string id, out LocationArtBinding0103 binding, out LocationArtProfile0103 profile)
            {
                var match = Manifest.locations.FirstOrDefault(l => string.Equals(l.authoredLocationId, id, StringComparison.OrdinalIgnoreCase));
                binding = match;
                profile = match == null ? null : Manifest.profiles.FirstOrDefault(p => p.id == match.profileId);
                return binding != null && binding.coverage != LocationArtCoverage0103.Unbound && profile != null;
            }
            public GameObject LoadPart(LocationArtPart0103 part, out string failure)
            { failure = Prefab == null ? "Fixture deliberately has no asset." : null; return Prefab; }
            public void Dispose() { }
        }

        private static LocationArtManifest0103 FixtureManifest()
        {
            return new LocationArtManifest0103
            {
                format = 1, platform = LocationArtBundleLibrary0103.CurrentPlatform,
                bundles = new[] { new LocationArtBundle0103 { id = "fixture", file = "fixture.bundle", sha256 = new string('0', 64) } },
                profiles = new[] { new LocationArtProfile0103
                {
                    id = "fixture-components", coverage = LocationArtCoverage0103.ComponentsOnly,
                    parts = new[] { new LocationArtPart0103 { bundleId = "fixture", assetName = "assets/artfixture/cube.prefab", position = new Vector3(3.2f, 0, 0), scale = Vector3.one } }
                } },
                locations = new[]
                {
                    new LocationArtBinding0103 { authoredLocationId = "L000", profileId = "fixture-components", coverage = LocationArtCoverage0103.ComponentsOnly },
                    new LocationArtBinding0103 { authoredLocationId = "L003", profileId = "fixture-components", coverage = LocationArtCoverage0103.ComponentsOnly }
                }
            };
        }
        private static bool Rejected(Action<LocationArtManifest0103> change)
        {
            var manifest = FixtureManifest(); change(manifest);
            try { LocationArtBundleLibrary0103.Validate(manifest, LocationArtBundleLibrary0103.CurrentPlatform); return false; }
            catch (InvalidDataException) { return true; }
        }
        private static string Gameplay(AscendanceGameManager game)
        {
            var save = AscendanceSaveSystem.CaptureForAuthority0100(game); save.SavedAtUtc = null;
            return JsonUtility.ToJson(save);
        }
        private static void RequireFixture(out AscendanceGameManager game)
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            game = AscendanceGameManager.Instance;
            var router = AscendanceClientCommandRouter0101.Instance;
            if (!Application.isPlaying || game == null || game.IsResolving || game.HasPendingReactionWindow || router == null || !router.IsLoopback)
                throw new InvalidOperationException("Use the idle isolated Stonebrook Play Mode fixture.");
        }

        [MenuItem("Ascendance/Validation/Test Location Art Framework (Play Mode)")]
        public static void TestFramework()
        {
            RequireFixture(out var game);
            int passed = 0;
            void Check(bool ok, string label)
            { if (!ok) throw new InvalidOperationException(Prefix + " FAIL — " + label); passed++; Debug.Log(Prefix + " PASS — " + label); }
            var restore = AscendanceSaveSystem.CaptureForAuthority0100(game);
            string directory = Path.Combine(AscendanceStoragePaths0102.RequireIsolatedValidationDirectory(), "location-art-" + Guid.NewGuid().ToString("N"));
            GameObject host = null, prefab = null, unsafePrefab = null, legacyHouse = null;
            LocationArtBundleLibrary0103 fileLibrary = null;
            try
            {
                Check(LocationArtCatalog0103.Definitions.Count == 236 && LocationArtCatalog0103.Definitions.Select(d => d.AuthoredLocationId).Distinct().Count() == 236,
                    "The read-only art catalog contains all 236 unique authored Location IDs.");
                Check(FullContentCatalog0850.Locations.All(l => LocationArtCatalog0103.TryGet(l.id, out var d) && d.Name == l.name && d.Region == l.region && d.Terrain == l.terrain),
                    "Every catalog entry preserves its actual authored name, region and terrain.");
                Check(LocationArtCatalog0103.Definitions.All(d => d.ThemeFamily != "unclassified"), "Every current terrain has an explicit component family without claiming imported art.");
                Check(!LocationArtCatalog0103.TryGet("L999", out _) && LocationArtCatalog0103.AuthoredIdFor(new LocationState { Id = "L006" }) == null,
                    "Unknown content and a bare colliding runtime ID do not silently select another Location's art.");
                var emptyCounts = LocationArtCatalog0103.CountBindings(null);
                Check(emptyCounts.CatalogLocations == 236 && emptyCounts.BoundLocations == 0 && emptyCounts.UnboundLocations == 236 && emptyCounts.FullReviewedDeclared == 0,
                    "A complete metadata catalog with no bindings still reports all 236 art presets unbound.");

                Check(Rejected(m => m.bundles[0].file = "../fixture.bundle"), "Bundle traversal is refused.");
                Check(Rejected(m => m.bundles[0].sha256 = "not-a-sha256"), "Unverified bundle hashes are refused.");
                Check(Rejected(m => m.bundles = new[] { m.bundles[0], m.bundles[0] }), "Duplicate bundle identities are refused.");
                Check(Rejected(m => m.platform = "WrongPlatform"), "A bundle manifest for another platform is refused.");
                Check(Rejected(m => m.locations[0].authoredLocationId = "L999"), "Bindings cannot invent an authored Location ID.");
                Check(Rejected(m => m.locations = new[] { m.locations[0], m.locations[0] }), "Duplicate Location bindings are refused.");
                Check(Rejected(m => m.locations[1].replaceLegacy = new[] { "stonebrook-house-0" }), "A profile reused outside Stonebrook cannot hide Stonebrook's legacy houses.");
                Check(Rejected(m => m.profiles[0].parts[0].assetName = "assets/../hidden.prefab"), "Prefab names cannot use path traversal.");
                Check(Rejected(m => m.profiles[0].parts[0].scale = new Vector3(float.NaN, 1, 1)), "Non-finite decorative transforms are refused.");
                Check(Rejected(m => { m.profiles[0].coverage = LocationArtCoverage0103.FullReviewed; m.locations[0].coverage = LocationArtCoverage0103.FullReviewed; }),
                    "Declaring full review without evidence and authored landmarks is refused.");
                var partial = FixtureManifest();
                Check(LocationArtBundleLibrary0103.MissingLandmarks(partial.locations[0], partial.profiles[0]).Length == 4,
                    "An empty partial gap list cannot hide Stonebrook's four missing authored landmarks.");

                Directory.CreateDirectory(directory);
                Check(!LocationArtBundleLibrary0103.TryOpen(Path.Combine(directory, "absent.json"), out _, out var absentFailure) && absentFailure == null,
                    "A missing optional art manifest remains a quiet fallback, not a visual pass.");
                string manifestPath = Path.Combine(directory, "location-art.json");
                File.WriteAllText(manifestPath, JsonUtility.ToJson(partial));
                Check(LocationArtBundleLibrary0103.TryOpen(manifestPath, out fileLibrary, out _) && LocationArtCatalog0103.CountBindings(fileLibrary).BoundLocations == 2,
                    "A valid explicit two-Location metadata manifest binds exactly two entries, not the whole catalog.");
                File.WriteAllText(Path.Combine(directory, "fixture.bundle"), "deliberately not the reviewed bundle bytes");
                Check(fileLibrary.LoadPart(partial.profiles[0].parts[0], out var wrongBytes) == null && wrongBytes.Contains("SHA-256"),
                    "Actual file bytes with a mismatched hash are refused before Unity tries to load a bundle.");

                prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); prefab.name = "SyntheticLocationArtFixture"; prefab.SetActive(false);
                prefab.AddComponent<Rigidbody>(); prefab.AddComponent<NavMeshAgent>(); prefab.AddComponent<NavMeshObstacle>();
#pragma warning disable 0618
                prefab.AddComponent<OffMeshLink>();
#pragma warning restore 0618
                prefab.AddComponent<Animator>().applyRootMotion = true;
                prefab.AddComponent<Camera>(); prefab.AddComponent<AudioSource>(); prefab.AddComponent<AudioListener>();
                var hiddenAlternative = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hiddenAlternative.name = "HiddenAlternativeAtInteractionCenter"; hiddenAlternative.transform.SetParent(prefab.transform, false);
                hiddenAlternative.transform.localPosition = new Vector3(-3.2f, 0, 0); hiddenAlternative.SetActive(false);
                Check(LocationArtBundleLibrary0103.IsPresentationPrefab(prefab, out _), "A synthetic renderer fixture is eligible for presentation safety tests.");
                unsafePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube); unsafePrefab.SetActive(false); unsafePrefab.AddComponent<FireFlicker>();
                Check(!LocationArtBundleLibrary0103.IsPresentationPrefab(unsafePrefab, out _), "A prefab with an uncurated script is rejected before Instantiate/Awake.");

                game.ApplyOnlineCampaignSlot0102(AscendanceGameManager.CreateFreshOnlineCampaign0102(new[] { "Aldric", "Rowan" }));
                var clearing = game.Locations.Values.First(l => l.AuthoredLocationId == "L003");
                Check(clearing.Id != "L003" && LocationArtCatalog0103.AuthoredIdFor(clearing) == "L003", "The actual legacy Woodland Clearing node resolves authored L003 instead of its colliding runtime ID.");
                host = new GameObject("LOCATION_ART_VALIDATION_ONLY");
                var presenter = host.AddComponent<LocationArtPresenter0103>();
                var source = new FixtureLibrary(partial, prefab);
                string before = Gameplay(game); string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
                int navVertices = NavMesh.CalculateTriangulation().vertices.Length;
                var marker = UnityEngine.Object.FindObjectsByType<LocationMarker>().First(m => m.LocationId == "L000");
                var markerCollider = marker.GetComponentInChildren<Collider>(); bool markerEnabled = markerCollider.enabled;
                presenter.Initialize(game, host.transform, source);
                Check(presenter.ActiveLocationCount == 1 && presenter.ActiveViews.Single().AuthoredLocationId == "L000", "Only the revealed starting Location receives the explicitly bound synthetic decoration.");
                var root = presenter.RootForRuntimeLocation("L000");
                Check(root != null && root.transform.position == game.Locations["L000"].Position
                    && root.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy),
                    "Art roots use the existing authoritative Location position and activate an initially inactive prefab's renderer.");
                Check(root.GetComponentsInChildren<Collider>(true).All(c => !c.enabled) && root.GetComponentsInChildren<Rigidbody>(true).All(r => r.isKinematic && !r.detectCollisions && !r.useGravity),
                    "Decorative colliders and rigidbodies cannot block selection or move game pieces.");
                Check(root.GetComponentsInChildren<NavMeshAgent>(true).All(a => !a.enabled) && root.GetComponentsInChildren<NavMeshObstacle>(true).All(o => !o.enabled)
                    && root.GetComponentsInChildren<Animator>(true).All(a => !a.enabled && !a.applyRootMotion),
                    "Decorative navigation and root-motion controllers are disabled before activation.");
#pragma warning disable 0618
                Check(root.GetComponentsInChildren<OffMeshLink>(true).All(l => !l.enabled), "Imported native navigation links cannot register new routes when scenery activates.");
#pragma warning restore 0618
                Check(root.GetComponentsInChildren<Camera>(true).All(c => !c.enabled) && root.GetComponentsInChildren<AudioListener>(true).All(l => !l.enabled)
                    && root.GetComponentsInChildren<AudioSource>(true).All(a => !a.enabled && !a.playOnAwake),
                    "Imported demo cameras and audio cannot take over the game's presentation.");
                var alternate = root.GetComponentsInChildren<Renderer>(true).Single(r => r.gameObject.name == "HiddenAlternativeAtInteractionCenter");
                Check(!alternate.gameObject.activeInHierarchy && Mathf.Abs(alternate.bounds.center.x) < 0.01f && presenter.ActiveViews.Single().PartsMissing == 0,
                    "An inactive alternate mesh at the anchor does not falsely reject the safe visible off-center model.");
                Physics.SyncTransforms();
                var hits = Physics.RaycastAll(new Ray(new Vector3(3.2f, 5, 0), Vector3.down), 10);
                Check(root.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == 2)
                    && !hits.Any(h => h.collider.transform.IsChildOf(root.transform)) && markerCollider.enabled == markerEnabled,
                    "Actual raycasts ignore scenery while the original gameplay marker collider remains unchanged.");
                Check(Gameplay(game) == before && JsonUtility.ToJson(UnityEngine.Random.state) == randomBefore && NavMesh.CalculateTriangulation().vertices.Length == navVertices,
                    "Art reconciliation changes no canonical gameplay field, gameplay RNG state or baked navigation geometry.");
                Check(presenter.ActiveViews[0].EffectiveCoverage == LocationArtCoverage0103.ComponentsOnly && presenter.ActiveViews[0].MissingLandmarks.Length == 4,
                    "Instantiating a generic component does not certify Stonebrook's missing landmark composition.");

                clearing.Revealed = true; presenter.Reconcile();
                Check(presenter.ActiveLocationCount == 2 && presenter.RootForRuntimeLocation(clearing.Id).transform.position == clearing.Position,
                    "A newly revealed legacy-mapped Location receives its own correctly anchored root.");
                var visibleCounts = LocationArtCatalog0103.CountBindings(source);
                Check(visibleCounts.BoundLocations == 2 && visibleCounts.UnboundLocations == 234 && visibleCounts.FullReviewedDeclared == 0,
                    "Two bound component profiles leave 234 Locations explicitly unbound and zero fully reviewed.");
                clearing.Revealed = false; presenter.Reconcile();
                Check(presenter.ActiveLocationCount == 1 && presenter.RootForRuntimeLocation(clearing.Id) == null,
                    "Hiding a Location removes only its decorative root.");
                clearing.Revealed = true; presenter.Reconcile(); game.DebugStartQuest0829("Q002");
                Check(presenter.ActiveLocationCount == 1 && presenter.RootForRuntimeLocation(clearing.Id) == null,
                    "A real quest transition releases art from the previous revealed quest map.");
                string afterQuest = Gameplay(game); presenter.Reconcile();
                Check(Gameplay(game) == afterQuest && game.CurrentQuestId0829 == "Q002", "Quest visibility reconciliation cannot rewrite quest setup, rewards or progress.");

                var missing = new FixtureLibrary(partial, null); presenter.Initialize(game, host.transform, missing);
                Check(presenter.ActiveLocationCount == 0 && presenter.BindingAttempts.Single().EffectiveCoverage == LocationArtCoverage0103.Unbound
                    && presenter.BindingAttempts.Single().PartsMissing == 1,
                    "A missing actual prefab reports an unbound failed attempt instead of passing its metadata profile.");
                var overlap = FixtureManifest(); overlap.profiles[0].parts[0].position = Vector3.zero;
                presenter.Initialize(game, host.transform, new FixtureLibrary(overlap, prefab));
                Check(presenter.ActiveLocationCount == 0 && presenter.BindingAttempts.Single().Failures.Any(f => f.Contains("interaction")),
                    "A tall decoration over the marker/pawn center is refused rather than hiding interaction.");
                partial.profiles[0].landmarks = LocationArtCatalog0103.Definitions.First(d => d.AuthoredLocationId == "L000").RequiredLandmarks.ToArray();
                presenter.Initialize(game, host.transform, new FixtureLibrary(partial, prefab));
                Check(presenter.ActiveViews.Single().MissingLandmarks.Length == 0 && presenter.ActiveViews.Single().EffectiveCoverage == LocationArtCoverage0103.ComponentsOnly,
                    "Even a complete landmark declaration stays components-only until review is explicitly supplied.");

                legacyHouse = GameObject.CreatePrimitive(PrimitiveType.Cube); legacyHouse.name = "OWNED_LEGACY_HOUSE_FIXTURE";
                legacyHouse.transform.position = new Vector3(3.2f, 0, 0);
                var legacyRenderer = legacyHouse.GetComponent<Renderer>(); var legacyCollider = legacyHouse.GetComponent<Collider>();
                var originallyHidden = GameObject.CreatePrimitive(PrimitiveType.Cube); originallyHidden.transform.SetParent(legacyHouse.transform, false);
                originallyHidden.GetComponent<Renderer>().enabled = false; originallyHidden.GetComponent<Collider>().enabled = false;
                var group = new LocationArtLegacyRendererGroup0103("stonebrook-house-0", new Bounds(legacyHouse.transform.position, Vector3.one),
                    new[] { legacyRenderer, originallyHidden.GetComponent<Renderer>() });
                presenter.Initialize(game, host.transform, new FixtureLibrary(partial, prefab), false, new[] { group });
                Check(legacyRenderer.enabled && presenter.SuppressedLegacyHouseCount == 0, "Matching new art does not automatically hide any legacy house without a Location-level opt-in.");
                partial.locations[0].replaceLegacy = new[] { "stonebrook-house-0" };
                partial.locations[0].authoredLocationId = "l000"; // Valid case-insensitive input must keep canonical scene identity.
                presenter.Initialize(game, host.transform, new FixtureLibrary(partial, prefab), false, new[] { group });
                var canonicalRoot = presenter.RootForRuntimeLocation("L000"); presenter.Reconcile();
                Check(!legacyRenderer.enabled && !originallyHidden.GetComponent<Renderer>().enabled && legacyCollider.enabled && markerCollider.enabled == markerEnabled
                    && presenter.SuppressedLegacyHouseCount == 1 && presenter.ActiveViews.Single().AuthoredLocationId == "L000"
                    && presenter.RootForRuntimeLocation("L000") == canonicalRoot,
                    "An explicit overlapping replacement uses canonical identity and hides only its house renderers, preserving colliders, markers and stable roots.");
                presenter.Initialize(game, host.transform, new FixtureLibrary(partial, null), false, new[] { group });
                Check(legacyRenderer.enabled && !originallyHidden.GetComponent<Renderer>().enabled && presenter.SuppressedLegacyHouseCount == 0,
                    "Missing replacement art restores the exact original visible/hidden renderer states.");
                partial.profiles[0].parts[0].position = new Vector3(20, 0, 0);
                presenter.Initialize(game, host.transform, new FixtureLibrary(partial, prefab), false, new[] { group });
                Check(legacyRenderer.enabled && presenter.ActiveLocationCount == 1 && presenter.SuppressedLegacyHouseCount == 0,
                    "A requested replacement with non-overlapping geometry does not hide the original building.");
                partial.profiles[0].parts[0].position = new Vector3(3.2f, 0, 0); partial.profiles[0].parts[0].scale = new Vector3(1, 0.1f, 1);
                presenter.Initialize(game, host.transform, new FixtureLibrary(partial, prefab), false, new[] { group });
                Check(legacyRenderer.enabled && presenter.ActiveLocationCount == 1 && presenter.SuppressedLegacyHouseCount == 0,
                    "A flat ground-only part overlapping a house footprint cannot suppress the actual building.");
                partial.profiles[0].parts[0].position = new Vector3(3.2f, 0, 0);
                partial.profiles[0].parts[0].scale = Vector3.one;
                presenter.Initialize(game, host.transform, new FixtureLibrary(partial, prefab), false, new[] { group });
                Check(!legacyRenderer.enabled && legacyCollider.enabled, "A valid explicitly requested replacement can be re-enabled after fallback.");
                string beforeDestroy = Gameplay(game); UnityEngine.Object.DestroyImmediate(host); host = null;
                Check(Gameplay(game) == beforeDestroy && markerCollider.enabled == markerEnabled && legacyRenderer.enabled
                    && !originallyHidden.GetComponent<Renderer>().enabled && legacyCollider.enabled,
                    "Presenter teardown restores legacy visibility without changing gameplay state, navigation colliders or marker input.");
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                if (prefab != null) UnityEngine.Object.DestroyImmediate(prefab);
                if (unsafePrefab != null) UnityEngine.Object.DestroyImmediate(unsafePrefab);
                if (legacyHouse != null) UnityEngine.Object.DestroyImmediate(legacyHouse);
                fileLibrary?.Dispose();
                game.ApplyOnlineCampaignSlot0102(restore);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            Debug.Log($"{Prefix} PASS — Location Art Framework completed: {passed} passed, 0 failed.");
        }

        [MenuItem("Ascendance/Validation/Test Installed Location Art Bundle (Play Mode)")]
        public static void TestInstalledBundle()
        {
            RequireFixture(out var game);
            int passed = 0;
            void Check(bool ok, string label)
            { if (!ok) throw new InvalidOperationException(Prefix + " FAIL — " + label); passed++; Debug.Log(Prefix + " PASS — " + label); }
            LocationArtBundleLibrary0103 library = null, secondLibrary = null; GameObject host = null;
            string before = Gameplay(game);
            try
            {
                string path = LocationArtBundleLibrary0103.DefaultManifestPath();
                Check(LocationArtBundleLibrary0103.TryOpen(path, out library, out var failure), "The required private client bundle manifest is available and valid. " + failure);
                Check(library.Manifest.bundles.Length > 0 && library.Manifest.locations.Any(l => l.coverage != LocationArtCoverage0103.Unbound), "The installed art manifest contains real bundle and Location bindings.");
                foreach (var part in library.Manifest.profiles.SelectMany(p => p.parts))
                    if (library.LoadPart(part, out var error) == null) throw new InvalidOperationException(Prefix + " Actual bundle part failed: " + error);
                Check(true, "Every declared part loads from its actual hash-verified platform AssetBundle and passes script inspection.");
                var firstPart = library.Manifest.profiles[0].parts[0];
                var firstAsset = library.LoadPart(firstPart, out _);
                Check(LocationArtBundleLibrary0103.TryOpen(path, out secondLibrary, out _) && secondLibrary.LoadPart(firstPart, out _) == firstAsset,
                    "Overlapping libraries share the same actual immutable bundle asset instead of loading the file twice.");
                library.Dispose(); library = null;
                Check(secondLibrary.LoadPart(firstPart, out _) != null, "Disposing one library does not invalidate another library's actual model.");
                secondLibrary.Dispose(); secondLibrary = null;
                Check(LocationArtBundleLibrary0103.TryOpen(path, out library, out _) && library.LoadPart(firstPart, out _) != null,
                    "A fresh library can reopen the actual client bundle after earlier owners dispose.");
                host = new GameObject("INSTALLED_LOCATION_ART_VALIDATION_ONLY");
                var presenter = host.AddComponent<LocationArtPresenter0103>(); presenter.Initialize(game, host.transform, library);
                Check(presenter.ActiveLocationCount > 0 && presenter.ActiveViews.All(v => v.PartsInstantiated > 0 && v.PartsMissing == 0), "The current revealed map instantiates actual bundle parts with no missing active parts.");
                Check(host.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled) && host.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), "Actual imported models retain renderers while their decorative colliders stay disabled.");
                Check(presenter.ActiveViews.All(v => v.EffectiveCoverage != LocationArtCoverage0103.FullReviewed || v.MissingLandmarks.Length == 0), "Imported components cannot hide incomplete authored landmarks behind a full-review label.");
                Check(Gameplay(game) == before, "Loading and instantiating the actual client art changes no canonical gameplay state.");
            }
            finally { if (host != null) UnityEngine.Object.DestroyImmediate(host); library?.Dispose(); secondLibrary?.Dispose(); }
            Debug.Log($"{Prefix} PASS — Installed Location Art Bundle completed: {passed} passed, 0 failed.");
        }

        [Serializable] private sealed class BindingExerciseRow
        {
            public string authoredLocationId, name, region, profileId, effectiveCoverage, failure;
            public int expectedParts, instantiatedParts, enabledRenderers;
            public string[] missingLandmarks;
            public bool geometryAvailable, interactionCenterClear, canonicalStateUnchanged, passed;
        }
        [Serializable] private sealed class BindingExerciseReport
        {
            public string kind = "actual-installed-location-art-binding-exercise";
            public bool landmarkCompletenessCertified = false, frameRenderingVerified = false;
            public int expectedLocations = 236, passed, failed;
            public List<BindingExerciseRow> locations = new();
        }

        [MenuItem("Ascendance/Validation/Test All Installed Location Art Bindings (Play Mode)")]
        public static void TestAllInstalledBindings()
        {
            RequireFixture(out var game);
            var report = new BindingExerciseReport();
            var spot = game.Locations["L000"];
            string originalAuthored = spot.AuthoredLocationId; bool originalReveal = spot.Revealed;
            LocationArtBundleLibrary0103 library = null; GameObject host = null;
            string output = Path.Combine(AscendanceStoragePaths0102.RequireIsolatedValidationDirectory(), "location-art-binding-exercise.json");
            try
            {
                if (!LocationArtBundleLibrary0103.TryOpen(LocationArtBundleLibrary0103.DefaultManifestPath(), out library, out var error))
                    throw new InvalidOperationException(Prefix + " Required all-Location manifest failed: " + error);
                if (LocationArtCatalog0103.CountBindings(library).BoundLocations != 236)
                    throw new InvalidOperationException(Prefix + " All 236 actual Location bindings are required; no missing binding is skipped.");
                host = new GameObject("ALL_LOCATION_BINDINGS_VALIDATION_ONLY");
                var presenter = host.AddComponent<LocationArtPresenter0103>(); presenter.Initialize(game, host.transform, library);
                foreach (var definition in LocationArtCatalog0103.Definitions.OrderBy(d => d.AuthoredLocationId, StringComparer.Ordinal))
                {
                    // This is a private presentation fixture, not 236 played
                    // quests. Preserve the real runtime ID, graph and position.
                    spot.AuthoredLocationId = definition.AuthoredLocationId; spot.Revealed = true;
                    string before = Gameplay(game);
                    var row = new BindingExerciseRow { authoredLocationId = definition.AuthoredLocationId, name = definition.Name, region = definition.Region };
                    report.locations.Add(row);
                    try
                    {
                        if (!library.TryGetBinding(definition.AuthoredLocationId, out _, out var profile)) throw new InvalidOperationException("Binding unavailable.");
                        row.profileId = profile.id; row.expectedParts = profile.parts.Length;
                        presenter.Reconcile();
                        var root = presenter.RootForRuntimeLocation("L000");
                        var view = presenter.BindingAttempts.SingleOrDefault(v => v.RuntimeLocationId == "L000");
                        row.instantiatedParts = view?.PartsInstantiated ?? 0; row.effectiveCoverage = view?.EffectiveCoverage ?? LocationArtCoverage0103.Unbound;
                        row.missingLandmarks = view?.MissingLandmarks ?? definition.RequiredLandmarks.ToArray();
                        if (root == null || view == null || view.PartsMissing != 0 || row.instantiatedParts != row.expectedParts)
                            throw new InvalidOperationException("Actual parts are missing: " + string.Join("; ", view?.Failures ?? Array.Empty<string>()));
                        var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy).ToArray();
                        row.enabledRenderers = renderers.Length;
                        row.geometryAvailable = renderers.Length > 0 && renderers.All(RenderableGeometry);
                        row.interactionCenterClear = root.transform.Cast<Transform>().All(t => LocationArtPresenter0103.LeavesInteractionCenterClear(t.gameObject, spot.Position));
                        row.canonicalStateUnchanged = Gameplay(game) == before;
                        row.passed = row.geometryAvailable && row.interactionCenterClear && row.canonicalStateUnchanged
                            && view.AuthoredLocationId == definition.AuthoredLocationId && root.transform.position == spot.Position;
                        if (!row.passed) throw new InvalidOperationException("Geometry, placement, interaction clearance or canonical-state check failed.");
                        report.passed++;
                        Debug.Log(Prefix + " PASS — Actual components instantiated for " + definition.AuthoredLocationId + "; landmark gaps remain separately reported.");
                    }
                    catch (Exception ex) { row.failure = ex.Message; row.passed = false; report.failed++; throw; }
                }
            }
            finally
            {
                spot.AuthoredLocationId = originalAuthored; spot.Revealed = originalReveal;
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                library?.Dispose();
                foreach (var presenter in UnityEngine.Object.FindObjectsByType<LocationArtPresenter0103>()) presenter.Reconcile();
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            Debug.Log($"{Prefix} PASS — All Location Art Bindings completed: {report.passed} passed, 0 failed.");
        }

        private static bool RenderableGeometry(Renderer renderer)
        {
            if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(m => m == null || m.shader == null || !m.shader.isSupported || m.shader.name == "Hidden/InternalErrorShader")) return false;
            if (renderer is SkinnedMeshRenderer skin) return skin.sharedMesh != null && skin.sharedMesh.vertexCount > 0;
            if (renderer is MeshRenderer) { var filter = renderer.GetComponent<MeshFilter>(); return filter != null && filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0; }
            return renderer is ParticleSystemRenderer && renderer.GetComponent<ParticleSystem>() != null;
        }
    }
}
#endif
