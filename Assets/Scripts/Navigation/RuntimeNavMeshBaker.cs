using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ascendance
{
    /// <summary>
    /// Builds the Stonebrook runtime NavMesh from lightweight box proxy geometry.
    /// Visual render meshes are deliberately never passed to NavMeshBuilder so imported
    /// production art does not need Read/Write enabled in player builds.
    /// </summary>
    public sealed class RuntimeNavMeshBaker : MonoBehaviour
    {
        private NavMeshData navMeshData;
        private NavMeshDataInstance navMeshInstance;

        public bool HasBuiltData0862 => navMeshData != null;
        public bool IsActive0862 => navMeshInstance.valid;

        public AsyncOperation Build(IReadOnlyList<GameObject> walkableGeometry, IReadOnlyList<GameObject> obstacleGeometry, Bounds worldBounds)
        {
            var sources = new List<NavMeshBuildSource>();

            if (walkableGeometry != null)
            {
                foreach (var go in walkableGeometry)
                    AddBoundsBoxSource(go, 0, sources, true);
            }

            int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
            if (notWalkableArea < 0) notWalkableArea = 1;

            if (obstacleGeometry != null)
            {
                foreach (var go in obstacleGeometry)
                    AddBoundsBoxSource(go, notWalkableArea, sources, false);
            }

            if (NavMesh.GetSettingsCount() == 0)
            {
                Debug.LogWarning("[ASCENDANCE] No NavMesh agent settings were found. Movement will use direct fallback motion.");
                return null;
            }

            if (navMeshInstance.valid) navMeshInstance.Remove();

            var settings = NavMesh.GetSettingsByIndex(0);
            navMeshData = new NavMeshData(settings.agentTypeID);
            navMeshData.name = "Ascendance_Runtime_NavMesh";

            // 0.8.62: build the data while it is detached. Registering an empty NavMeshData
            // before UpdateNavMeshDataAsync completes can make already-created agents report
            // that they are not close enough to a NavMesh. Activation now happens only after
            // the asynchronous bake has finished.
            return NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, settings, sources, worldBounds);
        }

        public bool Activate0862()
        {
            if (navMeshData == null) return false;
            if (navMeshInstance.valid) return true;
            navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
            return navMeshInstance.valid;
        }

        private static void AddBoundsBoxSource(GameObject go, int area, List<NavMeshBuildSource> sources, bool walkable)
        {
            if (go == null || !TryGetWorldBounds(go, out var bounds)) return;

            Vector3 size = bounds.size;
            Vector3 center = bounds.center;

            // Flat walkable proxies such as the Plane can report zero height.
            // Give them a thin volume whose top surface remains at the original top.
            const float minThickness = 0.08f;
            if (size.y < minThickness)
            {
                float top = bounds.max.y;
                size.y = minThickness;
                center.y = top - minThickness * 0.5f;
            }

            // Collision proxies should never become paper-thin blockers.
            if (!walkable && size.y < 0.25f)
            {
                float bottom = bounds.min.y;
                size.y = 0.25f;
                center.y = bottom + size.y * 0.5f;
            }

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one),
                size = size,
                area = area
            });
        }

        private static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
        {
            bool found = false;
            bounds = new Bounds(go.transform.position, Vector3.zero);

            // Prefer colliders because they represent gameplay/navigation geometry.
            foreach (var collider in go.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || !collider.enabled) continue;
                if (!found)
                {
                    bounds = collider.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (found) return true;

            // Imported visual-only assets may not have colliders. Their aggregate renderer
            // bounds are safe to use as a coarse proxy because no mesh data is read.
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled) continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private void OnDestroy()
        {
            if (navMeshInstance.valid) navMeshInstance.Remove();
        }
    }
}
