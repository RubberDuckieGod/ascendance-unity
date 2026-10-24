using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Optional production VFX hooks. Prefabs are loaded by convention and fall back to the
    /// asset-free sphere flash when a production effect has not been supplied yet.
    /// </summary>
    public static class ProductionVFXResolver
    {
        private const string Root = "Ascendance/VFX/";

        public static bool TrySpawn(string effectName, Vector3 position, Quaternion rotation, float lifetime = 2f)
        {
            var prefab = Resources.Load<GameObject>(Root + effectName);
            if (prefab == null) return false;
            var instance = Object.Instantiate(prefab, position, rotation);
            if (lifetime > 0f) Object.Destroy(instance, lifetime);
            return true;
        }
    }
}
