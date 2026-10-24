using UnityEngine;

namespace Ascendance
{
    public sealed class FireFlicker : MonoBehaviour
    {
        private Light flame;
        private float baseIntensity;
        private Vector3 baseScale;

        public void Initialize(Light source)
        {
            flame = source;
            baseIntensity = source != null ? source.intensity : 2f;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(Time.time * 7.5f, transform.position.x * 0.31f);
            if (flame != null) flame.intensity = baseIntensity * Mathf.Lerp(0.72f, 1.25f, n);
            transform.localScale = baseScale * Mathf.Lerp(0.82f, 1.18f, n);
            transform.Rotate(Vector3.up, Time.deltaTime * 28f, Space.World);
        }
    }
}
