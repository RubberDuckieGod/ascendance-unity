using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    public sealed partial class AscendanceGameManager
    {
        // Construct through the same production initialization used for a local
        // new game, instead of clearing selected fields on a progressed Hero.
        public static AscendanceSaveData CreateFreshOnlineCampaign0102(IEnumerable<string> heroIds)
        {
            var previous = Instance;
            var root = new GameObject("ASCENDANCE_FRESH_CAMPAIGN_BUILDER");
            root.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var fresh = root.AddComponent<AscendanceGameManager>();
                fresh.Initialize();
                fresh.SetParty(heroIds);
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
