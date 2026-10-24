#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    /// <summary>
    /// 0.9.0 promotion gate. This does not add gameplay mechanics; it proves the
    /// runtime-verified 0.8.62b baseline is packaged as a coherent Core Game Complete build.
    /// </summary>
    public static class CoreGameCompleteTestTools0900
    {
        private const string Prefix = "[ASCENDANCE][0.9.0 RELEASE]";

        [MenuItem("Ascendance/Validation/Test 0.9.0 - Core Game Complete Gate (Play Mode)")]
        public static void Test0900()
        {
            if (!Application.isPlaying) { Debug.LogError(Prefix + " Enter Play Mode first."); return; }
            var gm = AscendanceGameManager.Instance;
            if (gm == null) { Debug.LogError(Prefix + " No AscendanceGameManager found."); return; }
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass = 0, fail = 0;
            void Check(bool ok, string label)
            {
                if (ok) { pass++; Debug.Log(Prefix + " PASS — " + label); }
                else { fail++; Debug.LogError(Prefix + " FAIL — " + label); }
            }

            HeroDatabase.EnsureLoaded();
            FullContentCatalog0850.EnsureLoaded();

            Check(AscendanceGameManager.BuildVersion == "0.9.0", "BuildVersion is promoted to 0.9.0 Core Game Complete.");
            Check(PlayerSettings.companyName == "Ascendance" && PlayerSettings.productName == "Ascendance" && PlayerSettings.bundleVersion == "0.9.0",
                "PlayerSettings carry production Ascendance identity and bundle version 0.9.0.");

            string[] enabledScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Check(enabledScenes.SequenceEqual(new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Stonebrook.unity" }),
                "Production build settings contain exactly MainMenu -> Stonebrook in launch order.");

            bool requiredData =
                Resources.Load<TextAsset>("Ascendance/Data/hero_roster") != null &&
                Resources.Load<TextAsset>("Ascendance/Data/hero_cards") != null &&
                Resources.Load<TextAsset>("Ascendance/Data/status_effects") != null &&
                Resources.Load<TextAsset>("Ascendance/Data/full_content_0850") != null;
            Check(requiredData, "All authoritative Resources data manifests are present in the player build.");

            Check(HeroDatabase.Heroes.Count == 16, "Authoritative Hero roster contains all 16 Heroes.");
            Check(HeroDatabase.CardDefinitionCount == 323, "Authoritative Hero-card catalog contains all 323 definitions.");
            Check(HeroDatabase.Heroes.All(h => h != null && !string.IsNullOrWhiteSpace(h.id) && HeroDatabase.BuildPhysicalDeck(h.id).Count == 30),
                "Every authored Hero resolves to an exact 30-card personal deck.");

            Check(
                FullContentCatalog0850.LootCount == 190 &&
                FullContentCatalog0850.EquipmentCount == 144 &&
                FullContentCatalog0850.SearchCardCount == 100 &&
                FullContentCatalog0850.EncounterCount == 105 &&
                FullContentCatalog0850.EnemyCount == 125 &&
                FullContentCatalog0850.LocationCount == 236 &&
                FullContentCatalog0850.CraftingRecipeCount == 200 &&
                FullContentCatalog0850.QuestCount == 72 &&
                FullContentCatalog0850.WorldEventCount == 24,
                "Full authored-content manifest matches the locked Core Game Complete counts.");

            var root = FullContentCatalog0850.Root;
            bool uniqueIds =
                Unique(root.enemies.Select(x => x?.id), 125) &&
                Unique(root.locations.Select(x => x?.id), 236) &&
                Unique(root.craftingRecipes.Select(x => x?.id), 200) &&
                Unique(root.quests.Select(x => x?.id), 72);
            Check(uniqueIds, "Enemy, Location, Crafting, and Quest authored IDs are complete and duplicate-free.");

            bool questCoverage = Enumerable.Range(1, 72).All(n =>
                FullContentCatalog0850.TryGetQuest($"Q{n:000}", out var q) && q != null &&
                CampaignContent0845.Campaigns.ContainsKey(CampaignContent0845.CampaignForQuest0845($"Q{n:000}")));
            Check(questCoverage, "Q001-Q072 are all present and mapped into a registered campaign.");

            bool worldEventCoverage = Enumerable.Range(1, 24).All(n => CampaignContent0833.WorldEvents.ContainsKey($"WE{n:000}"));
            Check(worldEventCoverage, "WE001-WE024 are all present in the live campaign router.");

            bool campaignShape = CampaignContent0845.Campaigns.Count == 6 && CampaignContent0845.Campaigns.Values.All(c =>
            {
                if (c == null || !c.RuntimePlayable) return false;
                if (!int.TryParse((c.FirstQuestId ?? string.Empty).TrimStart('Q'), out int firstQ)) return false;
                if (!int.TryParse((c.LastQuestId ?? string.Empty).TrimStart('Q'), out int lastQ)) return false;
                if (!int.TryParse((c.FirstWorldEventId ?? string.Empty).Replace("WE", string.Empty), out int firstWe)) return false;
                if (!int.TryParse((c.LastWorldEventId ?? string.Empty).Replace("WE", string.Empty), out int lastWe)) return false;
                return lastQ - firstQ == 11 && lastWe - firstWe == 3;
            });
            Check(campaignShape, "All six campaigns are runtime-playable and retain 12-Quest / 4-World-Event structure.");

            MethodInfo windowsBuilder = typeof(AscendanceStandaloneBuilder).GetMethod("BuildWindowsStandalone", BindingFlags.Public | BindingFlags.Static);
            Check(windowsBuilder != null, "Windows Standalone production build entry point is installed.");

            bool masterFinished = false;
            bool masterPassed = false;
            int nestedErrors = 0;
            var nestedErrorMessages = new List<string>();
            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    nestedErrors++;
                    if (nestedErrorMessages.Count < 5) nestedErrorMessages.Add(condition ?? type.ToString());
                }
                if (string.IsNullOrEmpty(condition) || !condition.Contains("Master Save/Load + Full Regression completed:")) return;
                masterFinished = true;
                masterPassed = condition.Contains("[ASCENDANCE][0.9.0 TEST] PASS") && condition.Contains("54 passed, 0 failed");
            }

            Application.logMessageReceived += OnLog;
            try
            {
                MasterSaveLoadRegressionTestTools0862.Test0862();
                int frames = 0;
                while (!masterFinished && frames++ < 90000) yield return null;
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            Check(masterFinished && masterPassed, "The full 54-check master save/load + runtime regression passes under 0.9.0.");
            Check(nestedErrors == 0, nestedErrors == 0
                ? "Master regression emits zero Unity Error/Exception/Assert entries."
                : $"Master regression emits zero Unity Error/Exception/Assert entries (observed {nestedErrors}: {string.Join(" | ", nestedErrorMessages)}).");
            Check(string.IsNullOrWhiteSpace(gm.PendingContentInstruction), "Core runtime finishes the release gate without CONTENT PENDING leakage.");

            if (fail == 0) Debug.Log($"{Prefix} PASS — Core Game Complete Gate completed: {pass} passed, 0 failed.");
            else Debug.LogError($"{Prefix} FAIL — Core Game Complete Gate completed: {pass} passed, {fail} failed.");
        }

        private static bool Unique(IEnumerable<string> ids, int expected)
        {
            var normalized = ids?.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).ToList() ?? new List<string>();
            return normalized.Count == expected && normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() == expected;
        }
    }
}
#endif
