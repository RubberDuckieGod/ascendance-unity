#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class HeroParityAudit083
    {
        private static readonly string[] LegacyRepairSet =
        {
            "H003-009","H003-010","H003-020","H004-009","H004-011","H004-021","H005-002","H005-011","H005-013","H005-019",
            "H007-016","H007-018","H008-017","H008-018","H009-010","H009-018","H009-020","H010-012","H010-013","H010-019",
            "H011-007","H011-009","H011-011","H011-012","H011-014","H011-015","H011-016","H011-020","H012-011","H012-013",
            "H012-014","H012-015","H012-017","H012-018","H012-020","H013-006","H013-008","H013-015","H013-018","H014-005",
            "H014-006","H014-009","H014-012","H014-013","H014-014","H014-019","H014-020","H015-015","H015-016","H015-019",
            "H015-020","H016-007","H016-009","H016-014","H016-015","H016-019"
        };

        [MenuItem("Ascendance/Validation/Run 0.8.3 Hero Parity Audit")]
        public static void Run()
        {
            HeroDatabase.EnsureLoaded();
            var errors = new List<string>();
            var warnings = new List<string>();
            var heroes = HeroDatabase.Heroes.OrderBy(h => h.id).ToList();

            if (heroes.Count != 16) errors.Add($"Expected 16 Heroes, found {heroes.Count}.");
            if (HeroDatabase.CardDefinitionCount != 323) errors.Add($"Expected 323 Hero-card definitions, found {HeroDatabase.CardDefinitionCount}.");

            foreach (var hero in heroes)
            {
                var deck = HeroDatabase.BuildPhysicalDeck(hero.id);
                if (deck.Count != 30) errors.Add($"{hero.displayName}: expected 30 physical cards, found {deck.Count}.");
                if (deck.Any(id => HeroDatabase.GetCard(id) == null)) errors.Add($"{hero.displayName}: physical deck contains an unresolved card ID.");
            }

            foreach (var id in LegacyRepairSet)
                if (HeroDatabase.GetCard(id) == null) errors.Add($"Legacy 3.36.11 repair-set card is missing: {id}.");

            string[] questLimited = { "H004-020","H004-021","H011-020","H012-014","H012-020","H013-020","H014-020","H015-020","H016-020" };
            foreach (var id in questLimited)
                if (HeroDatabase.GetCard(id) == null) errors.Add($"Quest/campaign-limit card missing: {id}.");

            warnings.Add("Non-Attack incoming-damage Reaction windows are not yet generalized for every Encounter/Search/Condition damage source.");
            warnings.Add("Enemy non-Attack ability/Spell Reaction windows remain gated on the later full Enemy ability engine.");
            warnings.Add("Some forced-movement and optional movement-choice cards still use deterministic/partial resolution until the full board-choice UI migrates.");
            warnings.Add("Status durations that key off a specific Hero turn or Enemy activation need the phase-aware duration tracker planned after this audit.");

            string summary = $"0.8.3 HERO PARITY AUDIT — Heroes {heroes.Count}/16 · Definitions {HeroDatabase.CardDefinitionCount}/323 · " +
                             $"30-card decks {(errors.Any(e => e.Contains("physical cards")) ? "FAIL" : "16/16")} · Legacy repair IDs {LegacyRepairSet.Length}/56 · " +
                             $"Result {(errors.Count == 0 ? "DATA PASS" : "FAIL")}.";
            Debug.Log("[ASCENDANCE] " + summary);
            foreach (var e in errors) Debug.LogError("[ASCENDANCE AUDIT] " + e);
            foreach (var w in warnings) Debug.LogWarning("[ASCENDANCE AUDIT BOUNDARY] " + w);
            EditorUtility.DisplayDialog("Ascendance 0.8.3 Hero Parity Audit", summary + "\n\n" +
                (errors.Count == 0 ? "Authoritative Hero/card/deck data checks passed.\n" : string.Join("\n", errors) + "\n") +
                "\nSee HERO_GAMEPLAY_PARITY_AUDIT_0.8.3.md for executable coverage and known migration boundaries.", "OK");
        }
    }
}
#endif
