using UnityEngine;
using UnityEngine.AI;

namespace Ascendance
{
    public sealed class MovementPathPreview : MonoBehaviour
    {
        public static MovementPathPreview Instance { get; private set; }

        private LineRenderer line;
        private NavMeshPath path;

        private void Awake()
        {
            Instance = this;

            // Unity 6.6 no longer permits native-backed objects such as NavMeshPath
            // to be created in a MonoBehaviour field initializer/constructor.
            // Create it during Awake instead.
            path = new NavMeshPath();

            var go = new GameObject("Movement Path Preview");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
            line.startColor = new Color(0.18f, 0.78f, 1f, 0.95f);
            line.endColor = new Color(0.18f, 0.78f, 1f, 0.35f);
            line.widthMultiplier = 0.10f;
            line.numCapVertices = 5;
            line.positionCount = 0;
            line.enabled = false;
        }

        public void ShowTo(Vector3 destination, bool legal)
        {
            var gm = AscendanceGameManager.Instance;
            if (line == null || path == null || gm == null ||
                !gm.HeroAvatars.TryGetValue(gm.SelectedHeroId, out var hero) || hero == null)
            {
                Hide();
                return;
            }

            Color color = legal
                ? new Color(0.18f, 0.78f, 1f, 0.95f)
                : new Color(0.95f, 0.28f, 0.20f, 0.85f);
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.35f);

            Vector3 rawStart = hero.transform.position;
            Vector3 rawEnd = destination;
            Vector3 start = rawStart + Vector3.up * 0.12f;
            Vector3 end = rawEnd + Vector3.up * 0.12f;

            // Runtime NavMesh baking can still be in progress during the first frames.
            // Only ask Unity for a path when both ends are actually on a NavMesh.
            bool startOnMesh = NavMesh.SamplePosition(rawStart, out var startHit, 4f, NavMesh.AllAreas);
            bool endOnMesh = NavMesh.SamplePosition(rawEnd, out var endHit, 4f, NavMesh.AllAreas);

            if (startOnMesh && endOnMesh &&
                NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path) &&
                path.corners != null && path.corners.Length > 1)
            {
                line.positionCount = path.corners.Length;
                for (int i = 0; i < path.corners.Length; i++)
                    line.SetPosition(i, path.corners[i] + Vector3.up * 0.12f);
            }
            else
            {
                // Safe visual fallback: Ascendance movement legality still comes from
                // the Location graph, not from the NavMesh preview.
                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, end);
            }

            line.enabled = true;
        }

        public void Hide()
        {
            if (line == null) return;
            line.enabled = false;
            line.positionCount = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
