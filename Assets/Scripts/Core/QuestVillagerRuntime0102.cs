using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    public sealed partial class AscendanceGameManager
    {
        // Website parity: four civilian tokens, one loss per Goblin attack in
        // Stonebrook, and failure at three losses when two rescues are impossible.
        // Tokens have no invented combat Health or Armor.
        public int VillagersLost => CurrentQuestId0829 == "Q001" ? Q001VillagerView0102().Count(v => v.Destroyed) : 0;
        public int VillagersAvailable => CurrentQuestId0829 == "Q001" ? Q001VillagerView0102().Count(v => !v.Rescued && !v.Destroyed) : 0;
        public int VillagersSurviving => CurrentQuestId0829 == "Q001" ? Q001VillagerView0102().Count(v => !v.Destroyed) : 0;

        private IReadOnlyList<QuestObjectState0829> Q001VillagerView0102()
        {
            var stored = QuestRuntime0829?.Objects;
            if (stored != null && stored.Any(o => o != null && o.Type == "Villager"))
                return stored.Where(o => o != null && o.Type == "Villager").ToArray();
            // Older saves contain only the rescued counter. Reading or presenting
            // them must not change canonical save data during restore inspection.
            return Enumerable.Range(1, 4).Select(i => new QuestObjectState0829
            {
                ObjectId = "Q001_VILLAGER_" + i, QuestId = "Q001", Type = "Villager",
                DisplayName = "Villager " + i, LocationId = "L000", Rescued = i <= VillagersRescued
            }).ToArray();
        }

        private IReadOnlyList<QuestObjectState0829> QuestObjectView0102()
        {
            var stored = QuestRuntime0829?.Objects;
            if (CurrentQuestId0829 != "Q001" || stored != null && stored.Any(o => o != null && o.Type == "Villager"))
                return stored != null ? stored : Array.Empty<QuestObjectState0829>();
            return (stored ?? Enumerable.Empty<QuestObjectState0829>()).Concat(Q001VillagerView0102()).ToArray();
        }

        private void MaterializeQ001Villagers0102()
        {
            if (CurrentQuestId0829 != "Q001" || QuestRuntime0829 == null) return;
            if (QuestRuntime0829.Objects == null) QuestRuntime0829.Objects = new List<QuestObjectState0829>();
            if (QuestRuntime0829.Objects.Any(o => o != null && o.Type == "Villager")) return;
            QuestRuntime0829.Objects.AddRange(Q001VillagerView0102());
        }

        private bool HasQ001VillagerTarget0102(EnemyState enemy) =>
            CurrentQuestId0829 == "Q001" && !QuestComplete && !QuestFailed &&
            enemy != null && !enemy.Defeated && IsQ002Goblin0829(enemy) &&
            enemy.LocationId == "L000" && VillagersAvailable > 0;

        private bool TryResolveQ001VillagerAttack0102(EnemyState enemy)
        {
            if (!HasQ001VillagerTarget0102(enemy)) return false;
            MaterializeQ001Villagers0102();
            var victim = QuestRuntime0829.Objects.First(v => v.Type == "Villager" && !v.Rescued && !v.Destroyed);
            victim.Destroyed = true;
            AddLog($"Q001 — VILLAGER PRIORITY: {enemy.DisplayName} attacks {victim.DisplayName} in Stonebrook. 1 Villager is lost. {VillagersAvailable} unrescued remain ({VillagersLost}/3 lost).");
            EvaluateQuestOutcome0829();
            StateChanged?.Invoke();
            return true;
        }

#if UNITY_EDITOR
        public System.Collections.IEnumerator DebugRunSingleEnemyActivation0102(string enemyId)
        {
            if (Enemies.TryGetValue(enemyId, out var enemy))
                yield return ResolveSingleEnemyActivation(enemy, "VALIDATION");
        }
#endif
    }
}
