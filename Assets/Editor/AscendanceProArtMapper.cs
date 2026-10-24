#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public sealed class AscendanceProArtMapper : EditorWindow
    {
        private const string HeroRoot = "Assets/Resources/Ascendance/RealArt/Characters/Heroes";
        private const string EnemyRoot = "Assets/Resources/Ascendance/RealArt/Characters/Enemies";
        private const string EnvRoot = "Assets/Resources/Ascendance/RealArt/Environment/Stonebrook";

        private GameObject garruk, rowan, aldric, elara, goblinRaider, goblinCaptain;
        private GameObject houseA, houseB, houseC, bridge, well, wagon, cratesA, cratesB, fence, brazier, treeA, treeB, treeC, bushA, bushB, rockA, rockB, rockC;
        private Vector2 scroll;

        [MenuItem("Ascendance/Professional Art/Map Imported AAA Assets...")]
        public static void Open() => GetWindow<AscendanceProArtMapper>(true, "Ascendance Professional Art Mapper", true).Show();

        [MenuItem("Ascendance/Professional Art/Open Recommended Environment Pack")]
        public static void OpenEnvironment() => Application.OpenURL("https://assetstore.unity.com/packages/3d/environments/fantasy/detailed-medieval-village-156457");

        [MenuItem("Ascendance/Professional Art/Open Recommended Character Pack")]
        public static void OpenCharacters() => Application.OpenURL("https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/medieval-characters-pack-270571");

        [MenuItem("Ascendance/Professional Art/Open Alternate RPG Character Pack")]
        public static void OpenAlternateCharacters() => Application.OpenURL("https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/rpg-character-creation-pack-81757");

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Space(8);
            EditorGUILayout.LabelField("ASCENDANCE 0.6.0 — PROFESSIONAL ART BRIDGE", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This mapper is for licensed, production-quality Unity assets. Import the asset packs into this project first, then drag their prefabs into these slots. " +
                "Ascendance creates wrapper prefabs in Resources/Ascendance/RealArt and the gameplay code automatically prioritizes them over the old prototype art.", MessageType.Info);

            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open DETAILED Medieval Village")) OpenEnvironment();
                if (GUILayout.Button("Open Medieval Characters Pack")) OpenCharacters();
                if (GUILayout.Button("Open RPG Character Creation Pack")) OpenAlternateCharacters();
            }

            GUILayout.Space(12);
            EditorGUILayout.LabelField("Heroes / Enemies", EditorStyles.boldLabel);
            garruk = Slot("Garruk Stoneblood", garruk);
            rowan = Slot("Rowan Greenward", rowan);
            aldric = Slot("Sir Aldric Ironwall", aldric);
            elara = Slot("Sister Elara Dawnbringer", elara);
            goblinRaider = Slot("Goblin Raider", goblinRaider);
            goblinCaptain = Slot("Goblin Captain", goblinCaptain);

            GUILayout.Space(12);
            EditorGUILayout.LabelField("Stonebrook", EditorStyles.boldLabel);
            houseA = Slot("House A", houseA); houseB = Slot("House B", houseB); houseC = Slot("House C", houseC);
            bridge = Slot("Bridge", bridge); well = Slot("Well", well); wagon = Slot("Wagon / Cart", wagon);
            cratesA = Slot("Crates / Barrels A", cratesA); cratesB = Slot("Crates / Barrels B", cratesB);
            fence = Slot("Fence", fence); brazier = Slot("Brazier / Torch", brazier);
            treeA = Slot("Tree A", treeA); treeB = Slot("Tree B", treeB); treeC = Slot("Tree C", treeC);
            bushA = Slot("Bush A", bushA); bushB = Slot("Bush B", bushB);
            rockA = Slot("Rock A", rockA); rockB = Slot("Rock B", rockB); rockC = Slot("Rock C", rockC);

            GUILayout.Space(16);
            if (GUILayout.Button("INSTALL MAPPED ART INTO ASCENDANCE", GUILayout.Height(42))) Install();
            if (GUILayout.Button("Open Installed RealArt Folder"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/Ascendance/RealArt");
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }
            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Nothing in this tool copies or redistributes marketplace files outside your licensed Unity project. It only creates local wrapper prefabs that reference assets you imported yourself.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private static GameObject Slot(string label, GameObject value) =>
            (GameObject)EditorGUILayout.ObjectField(label, value, typeof(GameObject), false);

        private void Install()
        {
            Directory.CreateDirectory(HeroRoot); Directory.CreateDirectory(EnemyRoot); Directory.CreateDirectory(EnvRoot);
            SaveWrapper(garruk, HeroRoot + "/Garruk.prefab", "Garruk");
            SaveWrapper(rowan, HeroRoot + "/Rowan.prefab", "Rowan");
            SaveWrapper(aldric, HeroRoot + "/Aldric.prefab", "Aldric");
            SaveWrapper(elara, HeroRoot + "/Elara.prefab", "Elara");
            SaveWrapper(goblinRaider, EnemyRoot + "/GoblinRaider.prefab", "GoblinRaider");
            SaveWrapper(goblinCaptain, EnemyRoot + "/GoblinCaptain.prefab", "GoblinCaptain");

            SaveWrapper(houseA, EnvRoot + "/House_A.prefab", "House_A");
            SaveWrapper(houseB, EnvRoot + "/House_B.prefab", "House_B");
            SaveWrapper(houseC, EnvRoot + "/House_C.prefab", "House_C");
            SaveWrapper(bridge, EnvRoot + "/Bridge_A.prefab", "Bridge_A");
            SaveWrapper(well, EnvRoot + "/Well_A.prefab", "Well_A");
            SaveWrapper(wagon, EnvRoot + "/Wagon_A.prefab", "Wagon_A");
            SaveWrapper(cratesA, EnvRoot + "/Crates_A.prefab", "Crates_A");
            SaveWrapper(cratesB, EnvRoot + "/Crates_B.prefab", "Crates_B");
            SaveWrapper(fence, EnvRoot + "/Fence_A.prefab", "Fence_A");
            SaveWrapper(brazier, EnvRoot + "/Brazier_A.prefab", "Brazier_A");
            SaveWrapper(treeA, EnvRoot + "/Tree_A.prefab", "Tree_A");
            SaveWrapper(treeB, EnvRoot + "/Tree_B.prefab", "Tree_B");
            SaveWrapper(treeC, EnvRoot + "/Tree_C.prefab", "Tree_C");
            SaveWrapper(bushA, EnvRoot + "/Bush_A.prefab", "Bush_A");
            SaveWrapper(bushB, EnvRoot + "/Bush_B.prefab", "Bush_B");
            SaveWrapper(rockA, EnvRoot + "/Rock_A.prefab", "Rock_A");
            SaveWrapper(rockB, EnvRoot + "/Rock_B.prefab", "Rock_B");
            SaveWrapper(rockC, EnvRoot + "/Rock_C.prefab", "Rock_C");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Ascendance Professional Art", "Mapped art installed. Enter Play Mode and Ascendance will prioritize these prefabs over the prototype art.", "OK");
        }

        private static void SaveWrapper(GameObject source, string path, string wrapperName)
        {
            if (source == null) return;
            var root = new GameObject(wrapperName);
            var child = (GameObject)PrefabUtility.InstantiatePrefab(source);
            if (child == null) child = Instantiate(source);
            child.name = source.name;
            child.transform.SetParent(root.transform, false);
            root.transform.localPosition = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
        }
    }
}
#endif
