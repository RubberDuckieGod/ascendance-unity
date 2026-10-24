using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Ascendance
{
    public static class AscendanceSession
    {
        public static string StartingHeroId { get; private set; } = "Garruk";
        public static List<string> PartyHeroIds { get; private set; } = new(HeroDatabase.DefaultParty);
        public static bool LoadExistingSave { get; private set; }
        public static string LastContinueError0842 { get; private set; }
        private static string pendingResumeSurface0842;
        private static AscendanceSaveData pendingSave0842;
        private static bool pendingNewCampaignSetup0844;

        public static void StartNewGame(IEnumerable<string> partyHeroIds, string startingHeroId)
        {
            var ids=(partyHeroIds??HeroDatabase.DefaultParty).Where(id=>!string.IsNullOrEmpty(id)).Distinct().Take(4).ToList();
            if(ids.Count==0)ids.AddRange(HeroDatabase.DefaultParty);
            PartyHeroIds=ids;StartingHeroId=!string.IsNullOrEmpty(startingHeroId)&&ids.Contains(startingHeroId)?startingHeroId:ids[0];LoadExistingSave=false;LastContinueError0842=null;pendingResumeSurface0842=null;pendingSave0842=null;SceneManager.LoadScene("Stonebrook");
        }
        public static void StartNewGame(string heroId)=>StartNewGame(HeroDatabase.DefaultParty,heroId);
        public static bool ContinueGame()
        {
            if(!AscendanceSaveSystem.TryLoad(out var save,out var message))
            {
                LoadExistingSave=false;LastContinueError0842=message;pendingResumeSurface0842=null;pendingSave0842=null;return false;
            }
            LoadExistingSave=true;LastContinueError0842=null;pendingSave0842=save;pendingResumeSurface0842=CampaignResumeRuntime0842.NormalizeSurface0842(save);SceneManager.LoadScene("Stonebrook");return true;
        }
        public static void ReturnToMainMenu(){LoadExistingSave=false;pendingResumeSurface0842=null;pendingSave0842=null;pendingNewCampaignSetup0844=false;SceneManager.LoadScene("MainMenu");}
        public static void RequestNewCampaignSetup0844(){LoadExistingSave=false;LastContinueError0842=null;pendingResumeSurface0842=null;pendingSave0842=null;pendingNewCampaignSetup0844=true;SceneManager.LoadScene("MainMenu");}
        public static bool TryConsumeNewCampaignSetup0844(){bool value=pendingNewCampaignSetup0844;pendingNewCampaignSetup0844=false;return value;}

        public static void ApplyTo(AscendanceGameManager gm)
        {
            if(gm==null)return;
            if(LoadExistingSave)
            {
                var save=pendingSave0842??AscendanceSaveSystem.Load();pendingSave0842=null;if(save!=null){pendingResumeSurface0842=CampaignResumeRuntime0842.NormalizeSurface0842(save);gm.ApplySaveData(save);LoadExistingSave=false;return;}
            }
            gm.SetParty(PartyHeroIds,StartingHeroId);gm.SetStartingHero(StartingHeroId);LoadExistingSave=false;pendingResumeSurface0842=null;pendingSave0842=null;pendingNewCampaignSetup0844=false;
        }

        public static bool TryConsumePendingResume0842(out string surface)
        {
            surface=pendingResumeSurface0842;pendingResumeSurface0842=null;return !string.IsNullOrWhiteSpace(surface);
        }

        public static void DebugQueueResume0842(string surface) => pendingResumeSurface0842=surface;
    }
}
