using UnityEngine;

namespace Ascendance
{
    public static class CampaignSaveUx0843
    {
        public static string LastAutoSaveStatus0843 { get; private set; } = string.Empty;
        public static bool LastAutoSaveSucceeded0843 { get; private set; }

        public static bool SaveWithFeedback0843(AscendanceGameManager gm,out string message)
        {
            if (AscendanceSaveSystem.RemoteAuthorityOwnsPersistence0102d)
            {
                bool saved = AscendanceSaveSystem.SaveCurrent(gm, out message);
                if (saved) message = "SERVER-AUTHORITATIVE • local save skipped";
                return saved;
            }
            if(!AscendanceSaveSystem.SaveCurrent(gm,out var raw))
            {
                message="SAVE FAILED — "+raw+" Previous campaign save was not replaced.";
                return false;
            }
            if(AscendanceSaveSystem.TryLoad(out var save,out _)&&save!=null)
            {
                string surface=CampaignResumeRuntime0842.DescribeSurface0842(CampaignResumeRuntime0842.NormalizeSurface0842(save));
                string stamp=CampaignResumeRuntime0842.FormatSavedAt0842(save.SavedAtUtc);
                message=$"SAVED • {surface} • {stamp}";
            }
            else message="SAVED • Campaign save updated.";
            return true;
        }

        public static bool TryAutoSave0843(AscendanceGameManager gm,string reason,out string message)
        {
            if(gm==null){message="Autosave skipped — no active campaign.";Record(false,message);return false;}
            if(!AscendanceSaveSystem.CanSave(gm))
            {
                message="Autosave skipped — current state is resolving. Last safe campaign save remains unchanged.";Record(false,message);return false;
            }
            bool ok=SaveWithFeedback0843(gm,out var detail);
            message=ok?$"AUTOSAVE • {reason} • {detail}":detail;
            Record(ok,message);
            if(ok)Debug.Log("[ASCENDANCE] "+message);else Debug.LogWarning("[ASCENDANCE] "+message);
            return ok;
        }

        public static string SaveSafetySummary0843(AscendanceGameManager gm)
        {
            if(gm==null)return "NO ACTIVE CAMPAIGN";
            return AscendanceSaveSystem.CanSave(gm)
                ? "Safe to save now."
                : "Cannot save during movement, a Reaction window, or active resolution. Your previous safe save will remain unchanged.";
        }

        private static void Record(bool ok,string message){LastAutoSaveSucceeded0843=ok;LastAutoSaveStatus0843=message??string.Empty;}
    }
}
