using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Convention-based bridge between the rules prototype and production art.
    /// Exact prefab/model slots always win. If an exact Stonebrook slot is absent,
    /// 0.5.0 can pick from bundled _A/_B/_C mesh variants deterministically.
    /// </summary>
    public static class ProductionAssetResolver
    {
        public const string RealHeroRoot = "Ascendance/RealArt/Characters/Heroes/";
        public const string RealEnemyRoot = "Ascendance/RealArt/Characters/Enemies/";
        public const string RealStonebrookRoot = "Ascendance/RealArt/Environment/Stonebrook/";

        public const string HeroRoot = "Ascendance/Characters/Heroes/";
        public const string EnemyRoot = "Ascendance/Characters/Enemies/";
        public const string StonebrookRoot = "Ascendance/Environment/Stonebrook/";

        private static readonly Dictionary<string, string[]> EnvironmentVariants = new()
        {
            ["House"] = new[] { "House_A", "House_B", "House_C" },
            ["Tree"] = new[] { "Tree_A", "Tree_B", "Tree_C" },
            ["Bush"] = new[] { "Bush_A", "Bush_B" },
            ["Rock"] = new[] { "Rock_A", "Rock_B", "Rock_C" },
            ["Bridge"] = new[] { "Bridge_A" },
            ["Well"] = new[] { "Well_A" },
            ["Wagon"] = new[] { "Wagon_A" },
            ["Crates"] = new[] { "Crates_A", "Crates_B" },
            ["Brazier"] = new[] { "Brazier_A" },
            ["Fence"] = new[] { "Fence_A" }
        };

        public static GameObject LoadHeroPrefab(string heroId)
        {
            var real = Resources.Load<GameObject>(RealHeroRoot + heroId);
            return real != null ? real : Resources.Load<GameObject>(HeroRoot + heroId);
        }

        public static GameObject LoadEnemyPrefab(string enemyId, bool boss)
        {
            var realExact = Resources.Load<GameObject>(RealEnemyRoot + enemyId);
            if (realExact != null) return realExact;
            var realFallback = Resources.Load<GameObject>(RealEnemyRoot + (boss ? "GoblinCaptain" : "GoblinRaider"));
            if (realFallback != null) return realFallback;
            var exact = Resources.Load<GameObject>(EnemyRoot + enemyId);
            if (exact != null) return exact;
            return Resources.Load<GameObject>(EnemyRoot + (boss ? "GoblinCaptain" : "GoblinRaider"));
        }

        public static GameObject LoadEnvironmentPrefab(string slot, Vector3 position)
        {
            var realExact = Resources.Load<GameObject>(RealStonebrookRoot + slot);
            if (realExact != null) return realExact;
            var realVariant = LoadVariant(RealStonebrookRoot, slot, position);
            if (realVariant != null) return realVariant;

            var exact = Resources.Load<GameObject>(StonebrookRoot + slot);
            if (exact != null) return exact;
            return LoadVariant(StonebrookRoot, slot, position);
        }

        private static GameObject LoadVariant(string root, string slot, Vector3 position)
        {
            if (!EnvironmentVariants.TryGetValue(slot, out var variants) || variants == null || variants.Length == 0)
                return null;
            int start = PositiveHash(slot, position) % variants.Length;
            for (int i = 0; i < variants.Length; i++)
            {
                string candidate = variants[(start + i) % variants.Length];
                var loaded = Resources.Load<GameObject>(root + candidate);
                if (loaded != null) return loaded;
            }
            return null;
        }

        public static bool TrySpawnEnvironment(string slot, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, out GameObject instance)
        {
            instance = null;
            var prefab = LoadEnvironmentPrefab(slot, position);
            if (prefab == null) return false;

            instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            instance.name = slot + " (Production)";
            instance.transform.localScale = scale;

            var styler = instance.GetComponent<ProductionEnvironmentStyler>();
            if (styler == null) styler = instance.AddComponent<ProductionEnvironmentStyler>();
            styler.Initialize(slot);
            return true;
        }

        private static int PositiveHash(string slot, Vector3 position)
        {
            unchecked
            {
                int hash = 17;
                if (!string.IsNullOrEmpty(slot))
                {
                    for (int i = 0; i < slot.Length; i++)
                        hash = hash * 31 + slot[i];
                }
                hash = hash * 31 + Mathf.RoundToInt(position.x * 10f);
                hash = hash * 31 + Mathf.RoundToInt(position.z * 10f);
                return hash == int.MinValue ? 0 : Math.Abs(hash);
            }
        }
    }
}
