using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public sealed partial class AscendanceGameManager
    {
        // ApplySaveData is also used as a presentation merge. Slot activation
        // needs a complete authoritative replacement: Dictionary removal and
        // free-slot reuse otherwise changes Enemy activation/tie-break order.
        public void ApplyOnlineCampaignSlot0102(AscendanceSaveData save, IReadOnlyDictionary<string, EnemyState> fallbackEnemies = null)
        {
            if (save == null || save.Enemies == null) throw new InvalidOperationException("Saved campaign has no Enemy state.");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<EnemyState>();
            foreach (var stored in save.Enemies)
            {
                if (stored == null || string.IsNullOrWhiteSpace(stored.Id) || !ids.Add(stored.Id)
                    || stored.MaxHealth <= 0 || stored.Health < 0 || stored.Health > stored.MaxHealth || stored.Attack < 0 || stored.Armor < 0)
                    throw new InvalidOperationException("Saved campaign Enemy state is incomplete or invalid.");
                SearchEncounterContent0820.EnemyTemplates.TryGetValue(stored.TemplateId ?? string.Empty, out var template);
                EnemyState fallback = null;
                if (fallbackEnemies != null) fallbackEnemies.TryGetValue(stored.Id, out fallback);
                if (template == null && fallback == null) Enemies.TryGetValue(stored.Id, out fallback);
                if (template == null && fallback == null)
                    throw new InvalidOperationException("Saved campaign has an unavailable Enemy definition.");
                ordered.Add(new EnemyState
                {
                    Id = stored.Id, TemplateId = stored.TemplateId, DisplayName = template?.Name ?? fallback?.DisplayName ?? stored.Id,
                    EncounterSpawned = stored.EncounterSpawned, QuestSpawned = stored.QuestSpawned,
                    QuestId = stored.QuestId, EncounterInstanceId = stored.EncounterInstanceId,
                    MaxHealth = stored.MaxHealth, Health = stored.Health, Attack = stored.Attack, Armor = stored.Armor,
                    IsBoss = stored.IsBoss, ActivatedThisRound = stored.ActivatedThisRound, LocationId = stored.LocationId,
                    UsedKeys0859 = stored.UsedKeys0859?.ToList() ?? new List<string>(),
                    MovementStepsThisActivation0859 = stored.MovementStepsThisActivation0859, FocusHeroId0859 = stored.FocusHeroId0859,
                    Statuses = stored.Statuses?.Select(status => RestoreStatusFromSave(status, true)).ToList() ?? new List<StatusInstance>()
                });
            }
            ApplySaveData(save);
            Enemies.Clear();
            foreach (var enemy in ordered) Enemies.Add(enemy.Id, enemy);
            SetAuthorityCommandContext0100(save.SelectedHeroId, save.SelectedEnemyId);
        }

        // Construct through the same production initialization used for a local
        // new game, instead of clearing selected fields on a progressed Hero.
        public static AscendanceSaveData CreateFreshOnlineCampaign0102(IEnumerable<string> heroIds)
        {
            HeroDatabase.EnsureLoaded();
            var ids = heroIds?.ToList() ?? new List<string>();
            if (ids.Count < 1 || ids.Count > 4 || ids.Any(id => string.IsNullOrWhiteSpace(id) || HeroDatabase.GetHero(id) == null)
                || ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
                throw new InvalidOperationException("Choose 1–4 different authored Heroes for a new campaign.");
            var previous = Instance;
            var root = new GameObject("ASCENDANCE_FRESH_CAMPAIGN_BUILDER");
            root.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var fresh = root.AddComponent<AscendanceGameManager>();
                fresh.Initialize();
                // The supplied playtest roster defaults some Heroes to Level 3.
                // A newly created campaign starts at Level 1, before any
                // participating Hero's Quest-start ability hooks execute.
                foreach (var hero in fresh.Heroes.Values) { hero.Level = 1; hero.XP = 0; }
                fresh.SetParty(ids);
                var result = AscendanceSaveSystem.CaptureForAuthority0100(fresh);
                if (result == null || result.Round != 1 || result.PartyHeroIds.Count < 1 || result.PartyHeroIds.Count > 4)
                    throw new InvalidOperationException("New campaign setup did not produce a legal fresh party.");
                return result;
            }
            finally
            {
                Instance = previous;
                if (Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
