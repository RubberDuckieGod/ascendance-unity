using System;
using System.Collections.Generic;
using System.Linq;
using Ascendance.Online;
using UnityEngine;
using UnityEngine.AI;

namespace Ascendance
{
    public sealed class LocationArtLegacyRendererGroup0103
    {
        public string Id { get; }
        public Bounds Footprint { get; }
        private readonly Renderer[] renderers;
        private readonly bool[] originalEnabled;
        public bool IsSuppressed { get; private set; }
        public LocationArtLegacyRendererGroup0103(string id, Bounds footprint, IEnumerable<Renderer> source)
        {
            Id = id; Footprint = footprint; renderers = source.Where(r => r != null).Distinct().ToArray();
            originalEnabled = renderers.Select(r => r.enabled).ToArray();
        }
        public void SetSuppressed(bool value)
        {
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = !value && originalEnabled[i];
            IsSuppressed = value;
        }
    }

    public sealed class LocationArtInstanceView0103
    {
        public string RuntimeLocationId, AuthoredLocationId, ProfileId, DeclaredCoverage, EffectiveCoverage;
        public int PartsInstantiated, PartsMissing;
        public string[] MissingLandmarks, Failures;
    }

    // Read-only scenery reconciliation. Existing LocationMarkers keep all input
    // and reveal authority; these roots never contain a gameplay component.
    public sealed class LocationArtPresenter0103 : MonoBehaviour
    {
        public const float InteractionClearRadius = 1.9f;
        private sealed class Instance
        {
            public GameObject Root;
            public LocationArtInstanceView0103 View;
        }
        private readonly Dictionary<string, Instance> instances = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LocationArtLegacyRendererGroup0103> legacyGroups = new(StringComparer.Ordinal);
        private AscendanceGameManager manager;
        private Transform sceneryParent;
        private ILocationArtLibrary0103 library;
        private bool ownsLibrary;
        private bool reconciling;
        public string BindingFailure { get; private set; }
        public int ActiveLocationCount => instances.Values.Count(i => i.Root != null);
        public int SuppressedLegacyHouseCount => legacyGroups.Values.Count(g => g.IsSuppressed);
        public IReadOnlyList<LocationArtInstanceView0103> ActiveViews => instances.Values.Where(i => i.Root != null).Select(i => i.View).ToArray();
        public IReadOnlyList<LocationArtInstanceView0103> BindingAttempts => instances.Values.Select(i => i.View).ToArray();
        public GameObject RootForRuntimeLocation(string runtimeId) => instances.TryGetValue(runtimeId ?? "", out var instance) ? instance.Root : null;

        public static LocationArtPresenter0103 Install(AscendanceGameManager game, Transform parent, IEnumerable<LocationArtLegacyRendererGroup0103> legacy = null)
        {
            // Check before touching files or adding a component. Server builds
            // receive neither a manifest nor client art bundles.
            if (AscendanceDedicatedServerBootstrap0100.IsDedicatedServerProcess || game == null || parent == null) return null;
            var presenter = parent.GetComponent<LocationArtPresenter0103>();
            if (presenter != null) { presenter.Reconcile(); return presenter; }
            presenter = parent.gameObject.AddComponent<LocationArtPresenter0103>();
            LocationArtBundleLibrary0103 source = null; string failure = null;
            try { LocationArtBundleLibrary0103.TryOpen(LocationArtBundleLibrary0103.DefaultManifestPath(), out source, out failure); }
            catch (Exception ex) when (ex is ArgumentException || ex is System.IO.IOException || ex is System.IO.InvalidDataException) { failure = ex.Message; }
            presenter.Initialize(game, parent, source, true, legacy);
            presenter.BindingFailure = failure;
            return presenter;
        }

        public void Initialize(AscendanceGameManager game, Transform parent, ILocationArtLibrary0103 source, bool takeOwnership = false, IEnumerable<LocationArtLegacyRendererGroup0103> legacy = null)
        {
            var incomingGroups = (legacy ?? Array.Empty<LocationArtLegacyRendererGroup0103>()).ToArray();
            if (manager != null) manager.StateChanged -= Reconcile;
            ClearInstances();
            RestoreLegacy(); legacyGroups.Clear();
            foreach (var group in incomingGroups) legacyGroups.Add(group.Id, group);
            if (ownsLibrary) library?.Dispose();
            manager = game; sceneryParent = parent; library = source; ownsLibrary = takeOwnership; BindingFailure = null;
            if (manager != null) manager.StateChanged += Reconcile;
            Reconcile();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Private asset preview only; shipping players use their packaged file.
        public bool ReloadDevelopmentBindings(string absoluteManifestPath)
        {
            if (!LocationArtBundleLibrary0103.TryOpen(absoluteManifestPath, out var source, out var failure))
            { BindingFailure = failure; return false; }
            Initialize(manager, sceneryParent, source, true, legacyGroups.Values.ToArray());
            return true;
        }
#endif

        public static IReadOnlyList<LocationState> VisibleLocations(AscendanceGameManager game)
        {
            if (game == null) return Array.Empty<LocationState>();
            var active = FullContentCatalog0850.QuestLocationPoolIds(game.CurrentQuestId0829);
            active.Add(FullContentCatalog0850.StartingLocationId(game.CurrentQuestId0829));
            var occupied = new HashSet<string>(game.ParticipatingHeroes.Select(h => h.LocationId), StringComparer.OrdinalIgnoreCase);
            foreach (var obj in game.QuestObjects0829.Where(o => o != null && !o.Rescued && !o.Destroyed)) occupied.Add(obj.LocationId);
            foreach (var enemy in game.Enemies.Values.Where(e => !e.Defeated)) occupied.Add(enemy.LocationId);
            return game.Locations.Values.Where(l => l != null && l.Revealed && LocationArtCatalog0103.AuthoredIdFor(l) != null
                && (active.Contains(l.AuthoredLocationId) || occupied.Contains(l.Id)))
                .OrderBy(l => l.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public void Reconcile()
        {
            if (reconciling || manager == null || sceneryParent == null || AscendanceDedicatedServerBootstrap0100.IsDedicatedServerProcess) return;
            reconciling = true;
            try
            {
                var desired = VisibleLocations(manager).ToDictionary(l => l.Id, StringComparer.OrdinalIgnoreCase);
                foreach (string old in instances.Keys.Where(k => !desired.ContainsKey(k)).ToArray()) Remove(old);
                if (library == null) { ClearInstances(); return; }
                foreach (var state in desired.Values)
                {
                    string authored = LocationArtCatalog0103.AuthoredIdFor(state);
                    if (!library.TryGetBinding(authored, out var binding, out var profile)) { Remove(state.Id); continue; }
                    if (instances.TryGetValue(state.Id, out var existing))
                    {
                        if (existing.View.AuthoredLocationId == authored && existing.View.ProfileId == profile.id)
                        {
                            if (existing.Root != null) existing.Root.transform.position = state.Position;
                            continue;
                        }
                        Remove(state.Id);
                    }
                    Create(state, binding, profile);
                }
            }
            catch (Exception ex)
            {
                // Optional scenery must not throw through StateChanged into a
                // valid gameplay command when an art file is unavailable.
                BindingFailure = "Location art reconciliation failed: " + ex.GetType().Name;
                ClearInstances();
            }
            finally
            {
                try { UpdateLegacySuppression(); }
                catch (Exception ex) { BindingFailure = "Legacy art replacement refused: " + ex.GetType().Name; RestoreLegacy(); }
                reconciling = false;
            }
        }

        private void Create(LocationState state, LocationArtBinding0103 binding, LocationArtProfile0103 profile)
        {
            string canonicalId = LocationArtCatalog0103.AuthoredIdFor(state);
            var root = new GameObject("LocationArt_" + canonicalId + "_" + state.Id);
            root.SetActive(false);
            root.transform.SetParent(sceneryParent, false); root.transform.position = state.Position;
            var view = new LocationArtInstanceView0103
            {
                RuntimeLocationId = state.Id, AuthoredLocationId = canonicalId, ProfileId = profile.id,
                DeclaredCoverage = binding.coverage, EffectiveCoverage = LocationArtCoverage0103.Unbound,
                MissingLandmarks = LocationArtBundleLibrary0103.MissingLandmarks(binding, profile)
            };
            var failures = new List<string>();
            foreach (var part in profile.parts)
            {
                GameObject visual = null;
                try
                {
                    var prefab = library.LoadPart(part, out string failure);
                    if (prefab == null || !LocationArtBundleLibrary0103.IsPresentationPrefab(prefab, out failure))
                    { view.PartsMissing++; failures.Add(failure ?? "Art prefab is unavailable."); continue; }
                    // The inactive parent prevents physics/nav components from
                    // entering the scene before sanitation. Script assets were
                    // rejected before Instantiate, not after their Awake.
                    visual = Instantiate(prefab, root.transform, false);
                    visual.name = "ArtPart_" + view.PartsInstantiated;
                    visual.transform.localPosition = part.position; visual.transform.localRotation = Quaternion.Euler(part.rotation); visual.transform.localScale = part.scale;
                    SanitizeDecorativeInstance(visual);
                    visual.SetActive(true); // Imported prefabs may have been saved inactive during curation.
                    if (!HasEnabledRendererHierarchy(visual))
                    { view.PartsMissing++; failures.Add("Art part has no enabled renderer hierarchy."); DestroyVisual(visual); continue; }
                    if (!LeavesInteractionCenterClear(visual, state.Position))
                    { view.PartsMissing++; failures.Add("Art part overlaps the marker/pawn interaction area."); DestroyVisual(visual); continue; }
                    view.PartsInstantiated++;
                }
                catch (Exception ex)
                { DestroyVisual(visual); view.PartsMissing++; failures.Add("Art part could not be instantiated: " + ex.GetType().Name); }
            }
            view.Failures = failures.ToArray();
            if (view.PartsInstantiated > 0)
                view.EffectiveCoverage = binding.coverage == LocationArtCoverage0103.FullReviewed && view.PartsMissing == 0 && view.MissingLandmarks.Length == 0
                    ? LocationArtCoverage0103.FullReviewed : LocationArtCoverage0103.ComponentsOnly;
            if (view.PartsInstantiated == 0) { DestroyVisual(root); root = null; }
            instances.Add(state.Id, new Instance { Root = root, View = view });
            if (root != null) root.SetActive(true);
        }

        public static void SanitizeDecorativeInstance(GameObject visual)
        {
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
            { rigidbody.isKinematic = true; rigidbody.useGravity = false; rigidbody.detectCollisions = false; }
            foreach (var agent in visual.GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
            foreach (var obstacle in visual.GetComponentsInChildren<NavMeshObstacle>(true)) obstacle.enabled = false;
#pragma warning disable 0618 // Legacy native links may be present in imported model prefabs.
            foreach (var link in visual.GetComponentsInChildren<OffMeshLink>(true)) link.enabled = false;
#pragma warning restore 0618
            foreach (var animator in visual.GetComponentsInChildren<Animator>(true)) { animator.applyRootMotion = false; animator.enabled = false; }
            foreach (var animation in visual.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            foreach (var camera in visual.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in visual.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            foreach (var audio in visual.GetComponentsInChildren<AudioSource>(true)) { audio.playOnAwake = false; audio.enabled = false; }
            foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) if (script != null) script.enabled = false;
            foreach (var transform in visual.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 2; // Ignore Raycast.
        }

        public static bool LeavesInteractionCenterClear(GameObject visual, Vector3 anchor)
        {
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsEnabledWithinVisual(renderer, visual)) continue;
                var bounds = renderer.bounds;
                if (bounds.max.y <= anchor.y + 0.15f) continue; // Flat ground dressing may pass beneath the marker.
                float x = Mathf.Clamp(anchor.x, bounds.min.x, bounds.max.x) - anchor.x;
                float z = Mathf.Clamp(anchor.z, bounds.min.z, bounds.max.z) - anchor.z;
                if (x * x + z * z < InteractionClearRadius * InteractionClearRadius) return false;
            }
            return true;
        }

        private static bool HasEnabledRendererHierarchy(GameObject visual)
        {
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                if (IsEnabledWithinVisual(renderer, visual)) return true;
            return false;
        }

        private static bool IsEnabledWithinVisual(Renderer renderer, GameObject visual)
        {
            if (!renderer.enabled || renderer.forceRenderingOff) return false;
            for (var node = renderer.transform; node != null; node = node.parent)
            {
                if (!node.gameObject.activeSelf) return false;
                if (node == visual.transform) return true;
            }
            return false;
        }

        private void UpdateLegacySuppression()
        {
            foreach (var group in legacyGroups.Values)
            {
                bool covered = instances.Values.Any(instance => instance.Root != null && instance.View.AuthoredLocationId == "L000" && instance.View.PartsMissing == 0
                    && library != null && library.TryGetBinding("L000", out var binding, out _) && (binding.replaceLegacy ?? Array.Empty<string>()).Contains(group.Id)
                    && instance.Root.GetComponentsInChildren<Renderer>(true).Any(renderer => IsEnabledWithinVisual(renderer, instance.Root) && OverlapsFootprint(renderer.bounds, group.Footprint)));
                group.SetSuppressed(covered);
            }
        }
        private static bool OverlapsFootprint(Bounds current, Bounds legacy) => current.size.y >= Mathf.Max(0.5f, legacy.size.y * 0.5f)
            && current.max.y > legacy.min.y + 0.5f && current.size.x >= 0.25f && current.size.z >= 0.25f
            && current.max.x > legacy.min.x && current.min.x < legacy.max.x && current.max.z > legacy.min.z && current.min.z < legacy.max.z;
        private void RestoreLegacy() { foreach (var group in legacyGroups.Values) group.SetSuppressed(false); }

        private static void DestroyVisual(GameObject visual)
        {
            if (visual == null) return;
            visual.SetActive(false);
            if (Application.isPlaying) Destroy(visual); else DestroyImmediate(visual);
        }
        private void Remove(string runtimeId)
        {
            if (!instances.TryGetValue(runtimeId, out var instance)) return;
            DestroyVisual(instance.Root); instances.Remove(runtimeId);
        }
        private void ClearInstances() { foreach (var instance in instances.Values) DestroyVisual(instance.Root); instances.Clear(); }
        private void OnDestroy()
        {
            if (manager != null) manager.StateChanged -= Reconcile;
            ClearInstances();
            RestoreLegacy(); legacyGroups.Clear();
            if (ownsLibrary) library?.Dispose();
            manager = null; library = null;
        }
    }
}
