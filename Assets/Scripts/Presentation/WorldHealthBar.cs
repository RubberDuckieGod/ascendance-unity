using UnityEngine;

namespace Ascendance
{
    public sealed class WorldHealthBar : MonoBehaviour
    {
        private Transform fill;
        private Camera targetCamera;
        private float width;

        public static WorldHealthBar Create(Transform parent, float height, float width, Color fillColor)
        {
            var root = new GameObject("World Health Bar");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);
            var bar = root.AddComponent<WorldHealthBar>();
            bar.width = width;

            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Health Background";
            bg.transform.SetParent(root.transform, false);
            bg.transform.localScale = new Vector3(width + 0.08f, 0.10f, 0.035f);
            var bgRenderer = bg.GetComponent<Renderer>();
            bgRenderer.material.color = new Color(0.035f, 0.025f, 0.025f, 0.95f);
            Destroy(bg.GetComponent<Collider>());

            var fg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fg.name = "Health Fill";
            fg.transform.SetParent(root.transform, false);
            fg.transform.localPosition = new Vector3(0f, 0f, -0.025f);
            fg.transform.localScale = new Vector3(width, 0.065f, 0.04f);
            var fgRenderer = fg.GetComponent<Renderer>();
            fgRenderer.material.color = fillColor;
            Destroy(fg.GetComponent<Collider>());
            bar.fill = fg.transform;
            return bar;
        }

        public void SetFraction(float value)
        {
            if (fill == null) return;
            value = Mathf.Clamp01(value);
            fill.localScale = new Vector3(width * value, fill.localScale.y, fill.localScale.z);
            fill.localPosition = new Vector3(-(width * (1f - value)) * 0.5f, 0f, fill.localPosition.z);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position, Vector3.up);
        }
    }
}
