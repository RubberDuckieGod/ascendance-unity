using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Lightweight ambient motes/fireflies without adding a Particle System package dependency.
    /// </summary>
    public sealed class StonebrookAmbientFX : MonoBehaviour
    {
        private readonly List<Transform> motes = new();
        private readonly List<Vector3> origins = new();
        private readonly List<float> phases = new();

        public static void Create(Transform parent)
        {
            var host = new GameObject("Stonebrook Ambient Motes");
            host.transform.SetParent(parent, false);
            host.AddComponent<StonebrookAmbientFX>().Build();
        }

        private void Build()
        {
            var shader = Shader.Find("Standard");
            var material = new Material(shader) { color = new Color(0.86f, 0.67f, 0.20f) };
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.85f, 0.46f, 0.07f) * 1.8f);

            for (int i = 0; i < 18; i++)
            {
                float a = i * 1.73f;
                float r = 4.8f + (i % 5) * 2.1f;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, 1.2f + (i % 4) * 0.48f, Mathf.Sin(a) * r + 2.5f);
                var mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mote.name = "Firefly " + (i + 1);
                mote.transform.SetParent(transform, false);
                mote.transform.localPosition = p;
                mote.transform.localScale = Vector3.one * 0.055f;
                var collider = mote.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                var renderer = mote.GetComponent<Renderer>();
                renderer.material = material;
                motes.Add(mote.transform);
                origins.Add(p);
                phases.Add(i * 0.81f);
            }
        }

        private void Update()
        {
            for (int i = 0; i < motes.Count; i++)
            {
                if (motes[i] == null) continue;
                float t = Time.time * (0.55f + (i % 3) * 0.08f) + phases[i];
                Vector3 offset = new Vector3(Mathf.Sin(t * 0.73f) * 0.34f, Mathf.Sin(t * 1.31f) * 0.22f, Mathf.Cos(t * 0.91f) * 0.32f);
                motes[i].localPosition = origins[i] + offset;
            }
        }
    }
}
