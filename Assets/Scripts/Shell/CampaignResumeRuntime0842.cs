using System;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public static class CampaignResumeRuntime0842
    {
        public const string MidQuest = "MID_QUEST";
        public const string Results = "RESULTS";
        public const string Downtime = "DOWNTIME";
        public const string CampaignHub = "CAMPAIGN_HUB";
        public const string CampaignEnd = "CAMPAIGN_END";
        public const string CampaignLaunchpad = "CAMPAIGN_LAUNCHPAD";

        public static string DetermineCaptureSurface0842(AscendanceGameManager gm)
        {
            if (gm == null) return MidQuest;
            if (gm.CampaignLaunchpadAvailable0845) return CampaignLaunchpad;
            if (gm.CampaignCompleted0833 || gm.CampaignDefeated0833) return CampaignEnd;
            if (QuestBriefingUI0841.Instance != null && QuestBriefingUI0841.Instance.IsOpen) return CampaignHub; // ready-up is intentionally transient
            if (QuestResultsUI0835.Instance != null && QuestResultsUI0835.Instance.IsOpen) return Results;
            if (CampaignHubUI0834.Instance != null && CampaignHubUI0834.Instance.IsOpen) return CampaignHub;
            if (DowntimeUI0836.Instance != null && DowntimeUI0836.Instance.IsOpen) return Downtime;
            if (gm.QuestComplete || gm.QuestFailed) return Downtime;
            return MidQuest;
        }

        public static string NormalizeSurface0842(AscendanceSaveData save)
        {
            if (save == null) return string.Empty;
            string stored = (save.ResumeSurface0842 ?? string.Empty).Trim().ToUpperInvariant();
            if(save.CampaignProgress0833?.AwaitingCampaignRuntime0845==true&&CampaignContent0845.NormalizeCampaignId0845(save.CampaignProgress0833.CampaignId0845,save.CampaignProgress0833.CurrentWorldEventId)==CampaignContent0845.CampaignII)return CampaignLaunchpad;
            if(save.CampaignProgress0833!=null&&(save.CampaignProgress0833.CampaignCompleted||save.CampaignProgress0833.CampaignDefeated))return CampaignEnd;
            if(save.QuestRuntime0829?.CampaignVictory==true)return CampaignEnd;
            string campaignId=CampaignContent0845.NormalizeCampaignId0845(save.CampaignProgress0833?.CampaignId0845,save.CampaignProgress0833?.CurrentWorldEventId);
            var campaign=CampaignContent0845.Definition0845(campaignId);
            if(string.Equals(save.QuestId,campaign.LastQuestId,StringComparison.OrdinalIgnoreCase)&&save.QuestFailed)return CampaignEnd;
            if (stored == MidQuest || stored == Results || stored == Downtime || stored == CampaignHub || stored == CampaignEnd || stored == CampaignLaunchpad) return stored;
            // Compatibility for 0.8.41-and-earlier saves that predate explicit surface persistence.
            if (save.QuestComplete || save.QuestFailed) return Results;
            return MidQuest;
        }

        public static string DescribeSurface0842(string surface)
        {
            return NormalizeSurfaceName0842(surface) switch
            {
                Results => "Quest Results",
                Downtime => "Campaign Downtime",
                CampaignHub => "Campaign Hub",
                CampaignEnd => "Campaign End",
                CampaignLaunchpad => "Valewatch Campaign Hub",
                _ => "Mid-Quest"
            };
        }

        public static string BuildMainMenuSummary0842(AscendanceSaveData save)
        {
            if (save == null) return "NO VALID CAMPAIGN SAVE";
            string questId = string.IsNullOrWhiteSpace(save.QuestId) ? "UNKNOWN QUEST" : save.QuestId;
            string campaignId=CampaignContent0845.NormalizeCampaignId0845(save.CampaignProgress0833?.CampaignId0845,save.CampaignProgress0833?.CurrentWorldEventId);
            var campaign=CampaignContent0845.Definition0845(campaignId);
            string worldEvent = save.CampaignProgress0833?.CurrentWorldEventId;
            if (string.IsNullOrWhiteSpace(worldEvent)) worldEvent = "WE001";
            int totalDay = Mathf.Max(0, save.CampaignProgress0833?.TotalCampaignDay ?? 0);
            var ids = save.PartyHeroIds != null && save.PartyHeroIds.Count > 0
                ? save.PartyHeroIds.Take(4)
                : (save.Heroes != null ? save.Heroes.Where(h=>h!=null).Select(h=>h.Id).Take(4) : Enumerable.Empty<string>());
            string party = string.Join(" • ", ids.Select(id=>HeroDatabase.GetHero(id)?.displayName ?? id));
            if(string.IsNullOrWhiteSpace(party))party="Unknown Party";
            string surface = DescribeSurface0842(NormalizeSurface0842(save));
            bool savedVictory=save.CampaignProgress0833?.CampaignCompleted==true||save.QuestRuntime0829?.CampaignVictory==true;
            bool savedDefeat=save.CampaignProgress0833?.CampaignDefeated==true||(!savedVictory&&string.Equals(save.QuestId,campaign.LastQuestId,StringComparison.OrdinalIgnoreCase)&&save.QuestFailed);
            string ending = savedVictory?$"CAMPAIGN {campaign.Number} VICTORY • ":savedDefeat?$"CAMPAIGN {campaign.Number} DEFEAT • ":string.Empty;
            string stamp = FormatSavedAt0842(save.SavedAtUtc);
            string campaignPrefix = $"CAMPAIGN {campaign.Number} • {campaign.Title.ToUpperInvariant()} • ";
            return $"{campaignPrefix}{ending}{surface.ToUpperInvariant()}  •  {questId}  •  {worldEvent}  •  CAMPAIGN DAY {totalDay}\n{party}\nSAVED {stamp}";
        }

        public static string FormatSavedAt0842(string utc)
        {
            if (!DateTime.TryParse(utc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) return "UNKNOWN TIME";
            try { return dt.ToLocalTime().ToString("MMM d, yyyy  h:mm tt"); }
            catch { return dt.ToString("MMM d, yyyy  h:mm tt"); }
        }

        private static string NormalizeSurfaceName0842(string surface)
        {
            string s = (surface ?? string.Empty).Trim().ToUpperInvariant();
            return s == Results || s == Downtime || s == CampaignHub || s == CampaignEnd || s == CampaignLaunchpad ? s : MidQuest;
        }
    }

    public sealed class CampaignResumeController0842 : MonoBehaviour
    {
        public string DebugLastAppliedSurface0842 { get; private set; }

        private System.Collections.IEnumerator Start()
        {
            // Let every Stonebrook UI complete Start() first. In particular, the legacy
            // QuestResults auto-open may briefly react to a resolved save before we apply
            // the explicit 0.8.42 resume surface.
            yield return null;
            if (!AscendanceSession.TryConsumePendingResume0842(out var surface)) yield break;
            ApplySurface0842(surface);
        }

        public void ApplySurface0842(string surface)
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null) return;
            string normalized = (surface ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized != CampaignResumeRuntime0842.Results && normalized != CampaignResumeRuntime0842.Downtime && normalized != CampaignResumeRuntime0842.CampaignHub && normalized != CampaignResumeRuntime0842.CampaignEnd && normalized != CampaignResumeRuntime0842.CampaignLaunchpad)
                normalized = CampaignResumeRuntime0842.MidQuest;

            gm.CancelQuestBriefing0841();
            QuestBriefingUI0841.Instance?.CloseForResume0842();
            QuestResultsUI0835.Instance?.Close();
            DowntimeUI0836.Instance?.Close();
            CampaignHubUI0834.Instance?.Close();
            CampaignEndUI0844.Instance?.Close();
            CampaignLaunchpadUI0845.Instance?.Close();

            switch (normalized)
            {
                case CampaignResumeRuntime0842.Results:
                    if (gm.QuestComplete || gm.QuestFailed) QuestResultsUI0835.Instance?.Open();
                    else normalized = CampaignResumeRuntime0842.MidQuest;
                    break;
                case CampaignResumeRuntime0842.Downtime:
                    if (gm.IsDowntimeAvailable0836) DowntimeUI0836.Instance?.Open();
                    else normalized = CampaignResumeRuntime0842.MidQuest;
                    break;
                case CampaignResumeRuntime0842.CampaignHub:
                    if (gm.IsDowntimeAvailable0836) CampaignHubUI0834.Instance?.Open();
                    else normalized = CampaignResumeRuntime0842.MidQuest;
                    break;
                case CampaignResumeRuntime0842.CampaignEnd:
                    if (gm.CampaignEndAvailable0844) CampaignEndUI0844.Instance?.Open();
                    else normalized = CampaignResumeRuntime0842.MidQuest;
                    break;
                case CampaignResumeRuntime0842.CampaignLaunchpad:
                    if (gm.CampaignLaunchpadAvailable0845) CampaignLaunchpadUI0845.Instance?.Open();
                    else normalized = CampaignResumeRuntime0842.MidQuest;
                    break;
            }

            DebugLastAppliedSurface0842 = normalized;
            gm.AddLog($"RESUME — Campaign restored to {CampaignResumeRuntime0842.DescribeSurface0842(normalized)} from the persistent save.");
        }
    }
}
