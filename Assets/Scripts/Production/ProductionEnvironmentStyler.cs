using UnityEngine;
using UnityEngine.Rendering;

namespace Ascendance
{
    /// <summary>
    /// Runtime polish for imported/bundled Stonebrook meshes. Keeps imported models
    /// engine-agnostic while applying Unity lighting/emission behavior after instantiation.
    /// </summary>
    public sealed class ProductionEnvironmentStyler : MonoBehaviour
    {
        private string slot;
        private bool initialized;

        public void Initialize(string environmentSlot)
        {
            if (initialized) return;
            initialized = true;
            slot = environmentSlot ?? string.Empty;

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;

                string n = renderer.gameObject.name.ToLowerInvariant();
                if (n.Contains("window"))
                    ApplyEmission(renderer, new Color(1f, 0.30f, 0.055f), 1.55f);
                else if (n.Contains("flame"))
                    ApplyEmission(renderer, new Color(1f, 0.12f, 0.01f), 2.8f);
            }

            if (slot == "Brazier") AddBrazierLight();
        }

        private static void ApplyEmission(Renderer renderer, Color color, float multiplier)
        {
            if (renderer.material == null) return;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", color * multiplier);
        }

        private void AddBrazierLight()
        {
            var lightHost = new GameObject("Production Fire Light");
            lightHost.transform.SetParent(transform, false);
            lightHost.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            var light = lightHost.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9.5f;
            light.intensity = 2.35f;
            light.color = new Color(1f, 0.33f, 0.075f);
            light.shadows = LightShadows.Soft;
            lightHost.AddComponent<FireFlicker>().Initialize(light);
        }
    }
}
