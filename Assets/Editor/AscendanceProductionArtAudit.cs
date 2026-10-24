#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class AscendanceProductionArtAudit
    {
        private static readonly string[] Heroes = { "Garruk", "Rowan", "Aldric", "Elara" };
        private static readonly string[] Enemies = { "GoblinRaider", "GoblinCaptain" };
        private static readonly string[] Environment =
        {
            "House_A", "House_B", "House_C", "Tree_A", "Tree_B", "Tree_C",
            "Bush_A", "Bush_B", "Rock_A", "Rock_B", "Rock_C", "Bridge_A",
            "Well_A", "Wagon_A", "Crates_A", "Crates_B", "Brazier_A", "Fence_A"
        };

        [MenuItem("Ascendance/Production Art/Audit 0.5.0 Assets")]
        public static void Audit()
        {
            Debug.Log("[ASCENDANCE ART AUDIT] 0.5.0 production art audit starting...");
            foreach (string hero in Heroes)
                AuditCharacter($"Assets/Resources/Ascendance/Characters/Heroes/{hero}.prefab", hero, false);
            foreach (string enemy in Enemies)
                AuditCharacter($"Assets/Resources/Ascendance/Characters/Enemies/{enemy}.prefab", enemy, true);

            int environmentFound = 0;
            foreach (string asset in Environment)
            {
                string path = $"Assets/Resources/Ascendance/Environment/Stonebrook/{asset}.obj";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) environmentFound++;
                else Debug.LogWarning($"[ASCENDANCE ART AUDIT] Missing bundled environment model: {path}");
            }

            Debug.Log($"[ASCENDANCE ART AUDIT] Bundled Stonebrook meshes: {environmentFound}/{Environment.Length}. " +
                      "Missing Hero/enemy prefabs are expected until final rigged characters are imported; fallback rigs remain active.");
        }

        private static void AuditCharacter(string path, string label, bool enemy)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.Log($"[ASCENDANCE ART AUDIT] {label}: no production prefab — articulated fallback remains active.");
                return;
            }

            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null)
                Debug.LogWarning($"[ASCENDANCE ART AUDIT] {label}: production prefab exists but has no Animator. Add an Animator/Controller for production animation timing.");
            else
                Debug.Log($"[ASCENDANCE ART AUDIT] {label}: production prefab + Animator detected.");
        }
    }
}
#endif
