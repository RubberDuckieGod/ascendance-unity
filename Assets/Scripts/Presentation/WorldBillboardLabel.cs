using UnityEngine;

namespace Ascendance
{
    public sealed class WorldBillboardLabel : MonoBehaviour
    {
        private Camera targetCamera;

        public static WorldBillboardLabel Create(Transform parent, string text, Vector3 localPosition, Color color, int fontSize = 42)
        {
            var go = new GameObject("WorldLabel_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.08f;
            mesh.fontSize = fontSize;
            mesh.color = color;
            var label = go.AddComponent<WorldBillboardLabel>();
            return label;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position, Vector3.up);
        }
    }
}
