using UnityEngine;

namespace Ascendance
{
    public sealed class CampaignAutoSave0842 : MonoBehaviour
    {
        private bool quitSaveAttempted;
        private static bool suppressNextQuitSave0843;

        public static void SuppressNextQuitSave0843()=>suppressNextQuitSave0843=true;

        private void OnApplicationQuit()
        {
            if(suppressNextQuitSave0843){suppressNextQuitSave0843=false;return;}
            if(quitSaveAttempted)return;quitSaveAttempted=true;
            CampaignSaveUx0843.TryAutoSave0843(AscendanceGameManager.Instance,"APPLICATION QUIT",out _);
        }

        private void OnApplicationPause(bool paused)
        {
            if(!paused)return;
            CampaignSaveUx0843.TryAutoSave0843(AscendanceGameManager.Instance,"APP SUSPEND",out _);
        }
    }
}
