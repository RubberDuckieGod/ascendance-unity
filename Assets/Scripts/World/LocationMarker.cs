using UnityEngine;

namespace Ascendance
{
    public sealed class LocationMarker : MonoBehaviour
    {
        public string LocationId { get; private set; }
        private Renderer markerRenderer;
        private Collider markerCollider;
        private bool hovered;

        public void Initialize(string locationId, Renderer renderer, Collider collider)
        {
            LocationId = locationId;
            markerRenderer = renderer;
            markerCollider = collider;
            markerRenderer.material = new Material(Shader.Find("Standard"));
            markerRenderer.material.EnableKeyword("_EMISSION");
            Refresh();
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null || !gm.Locations.TryGetValue(LocationId, out var state) || markerRenderer == null) return;
            markerRenderer.enabled = state.Revealed;
            if (markerCollider != null) markerCollider.enabled = state.Revealed;
            if (!state.Revealed) return;

            bool legal = gm.CanMoveTo(LocationId);
            Color color = legal ? new Color(0.12f, 0.70f, 1f) : new Color(0.34f, 0.37f, 0.40f);
            if (hovered) color = legal ? new Color(0.45f, 0.90f, 1f) : new Color(0.72f, 0.34f, 0.24f);
            markerRenderer.material.color = new Color(color.r, color.g, color.b, hovered ? 0.95f : 0.72f);
            markerRenderer.material.SetColor("_EmissionColor", color * (hovered ? 1.0f : 0.45f));
        }
    }
}
