using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Runtime surface library for the bundled 0.5.0 Stonebrook texture pass.
    /// Materials are created in code so the project stays portable without fragile .mat GUIDs.
    /// </summary>
    public static class StonebrookSurfaceMaterials
    {
        private const string TextureRoot = "Ascendance/Environment/Stonebrook/Textures/";
        private static readonly Dictionary<string, Material> Cache = new();

        public static Material Ground() => Get("ground", "grass_albedo", new Color(0.82f, 0.90f, 0.76f), new Vector2(9f, 9f), 0.10f);
        public static Material Road() => Get("road", "dirt_albedo", new Color(0.95f, 0.88f, 0.74f), new Vector2(4.5f, 2.3f), 0.08f);
        public static Material Stone() => Get("stone", "stone_albedo", new Color(0.92f, 0.92f, 0.90f), new Vector2(2.8f, 2.8f), 0.16f);

        public static Material Water()
        {
            var material = Get("water", "water_albedo", new Color(0.65f, 0.82f, 1f), new Vector2(1.8f, 4.8f), 0.72f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.012f, 0.055f, 0.11f));
            return material;
        }

        private static Material Get(string key, string textureName, Color tint, Vector2 tiling, float smoothness)
        {
            if (Cache.TryGetValue(key, out var existing) && existing != null) return existing;

            var shader = Shader.Find("Standard");
            var material = new Material(shader) { name = "Stonebrook_" + key };
            material.color = tint;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0f);

            var texture = Resources.Load<Texture2D>(TextureRoot + textureName);
            if (texture != null)
            {
                material.mainTexture = texture;
                material.mainTextureScale = tiling;
            }

            Cache[key] = material;
            return material;
        }
    }
}
