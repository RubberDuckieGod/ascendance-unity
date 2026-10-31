using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public sealed partial class AscendanceGameManager
    {
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
