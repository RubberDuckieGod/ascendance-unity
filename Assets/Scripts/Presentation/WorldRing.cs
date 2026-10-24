using UnityEngine;

namespace Ascendance
{
    public sealed class WorldRing : MonoBehaviour
    {
        private LineRenderer line;
        private float radius = 1f;

        public static WorldRing Create(Transform parent, string name, float radius, Color color, float width = 0.08f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            var ring = go.AddComponent<WorldRing>();
            ring.radius = radius;
            ring.line = go.AddComponent<LineRenderer>();
            ring.line.useWorldSpace = false;
            ring.line.loop = true;
            ring.line.positionCount = 64;
            ring.line.widthMultiplier = width;
            ring.line.numCapVertices = 4;
            ring.line.numCornerVertices = 4;
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader != null) ring.line.material = new Material(shader);
            ring.SetColor(color);
            ring.Rebuild();
            return ring;
        }

        public void SetColor(Color color)
        {
            if (line == null) return;
            line.startColor = color;
            line.endColor = color;
            if (line.material != null) line.material.color = color;
        }

        public void SetVisible(bool visible)
        {
            if (line != null) line.enabled = visible;
        }

        private void Rebuild()
        {
            if (line == null) return;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }
}
