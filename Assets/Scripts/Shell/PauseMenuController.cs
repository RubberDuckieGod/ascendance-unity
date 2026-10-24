using UnityEngine;
using UnityEngine.UI;

namespace Ascendance
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject pausePanel;
        private SettingsPanelUI settingsPanel;
        private Text statusText;
        private bool paused;
        private ShellConfirmationDialog0843 confirmation0843;

        public bool DebugConfirmationVisible0843 => confirmation0843!=null&&confirmation0843.IsOpen;
        public string DebugConfirmationTitle0843 => confirmation0843?.DebugTitle0843??string.Empty;
        public string DebugConfirmationBody0843 => confirmation0843?.DebugBody0843??string.Empty;
        public bool DebugConfirmationPrimaryEnabled0843 => confirmation0843!=null&&confirmation0843.DebugPrimaryInteractable0843;
        public string DebugStatusText0843 => statusText?.text??string.Empty;

        private void Start(){Build();SetPaused(false);}

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(confirmation0843!=null&&confirmation0843.IsOpen){confirmation0843.Hide();return;}
                if(paused&&pausePanel!=null&&!pausePanel.activeSelf){settingsPanel?.SetVisible(false);pausePanel.SetActive(true);return;}
                SetPaused(!paused);
            }
            if(!paused&&Input.GetKeyDown(KeyCode.F5))Save();
        }

        private void Build()
        {
            canvas=ShellUIFactory.CreateCanvas("PauseMenuCanvas",600);canvas.gameObject.transform.SetParent(transform,false);
            pausePanel=ShellUIFactory.PanelRect(canvas.transform,"PausePanel",new Vector2(0.36f,0.16f),new Vector2(0.64f,0.84f),new Color(0.012f,0.018f,0.026f,0.985f));
            ShellUIFactory.TextRect(pausePanel.transform,"PAUSED",44,TextAnchor.MiddleCenter,new Vector2(0.10f,0.85f),new Vector2(0.90f,0.96f),ShellUIFactory.Gold);
            ShellUIFactory.ButtonRect(pausePanel.transform,"RESUME",new Vector2(0.10f,0.69f),new Vector2(0.90f,0.80f),()=>SetPaused(false));
            ShellUIFactory.ButtonRect(pausePanel.transform,"SAVE GAME",new Vector2(0.10f,0.54f),new Vector2(0.90f,0.65f),Save);
            ShellUIFactory.ButtonRect(pausePanel.transform,"SETTINGS",new Vector2(0.10f,0.39f),new Vector2(0.90f,0.50f),OpenSettings);
            ShellUIFactory.ButtonRect(pausePanel.transform,"QUIT TO MENU",new Vector2(0.10f,0.24f),new Vector2(0.90f,0.35f),RequestQuitToMenu0843);
            ShellUIFactory.ButtonRect(pausePanel.transform,"QUIT GAME",new Vector2(0.10f,0.09f),new Vector2(0.90f,0.20f),RequestQuitGame0843);
            statusText=ShellUIFactory.TextRect(pausePanel.transform,"ESC pause  •  F5 quick save",15,TextAnchor.MiddleCenter,new Vector2(0.08f,0.01f),new Vector2(0.92f,0.075f),new Color(0.58f,0.62f,0.66f));
            settingsPanel=SettingsPanelUI.Create(canvas.transform,()=>pausePanel.SetActive(true));settingsPanel.SetVisible(false);
            confirmation0843=ShellConfirmationDialog0843.Create(canvas.transform,"PauseConfirmation0843");
        }

        private void SetPaused(bool value){paused=value;pausePanel.SetActive(value);if(!value&&settingsPanel!=null)settingsPanel.SetVisible(false);if(!value)confirmation0843?.Hide();Time.timeScale=value?0f:1f;}

        private void Save()
        {
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(AscendanceGameManager.Instance,out string message);
            SetStatus0843(message,!ok);
        }

        private void SetStatus0843(string message,bool error)
        {
            if(statusText==null)return;statusText.text=message??string.Empty;statusText.color=error?new Color(0.95f,0.48f,0.38f):new Color(0.72f,0.78f,0.70f);
            if(error)Debug.LogWarning("[ASCENDANCE] "+message);
        }

        private void OpenSettings(){pausePanel.SetActive(false);settingsPanel.SetVisible(true);}

        private void RequestQuitToMenu0843()=>ShowQuitConfirmation0843(false);
        private void RequestQuitGame0843()=>ShowQuitConfirmation0843(true);

        private void ShowQuitConfirmation0843(bool quitApplication)
        {
            var gm=AscendanceGameManager.Instance;bool safe=AscendanceSaveSystem.CanSave(gm);
            string title=quitApplication?"QUIT ASCENDANCE?":"QUIT TO MAIN MENU?";
            string destination=quitApplication?"exit the game":"return to the Main Menu";
            string body=safe
                ?$"Choose SAVE & QUIT to update your campaign before you {destination}.\n\nQUIT WITHOUT SAVING keeps the previous campaign save exactly as it is."
                :$"The campaign cannot be saved safely right now because movement, a Reaction window, or resolution is active.\n\nYou may cancel, or explicitly {destination} WITHOUT SAVING. The previous safe campaign save will not be overwritten.";
            confirmation0843.Show(title,body,"SAVE & QUIT",()=>SaveAndQuit0843(quitApplication),"QUIT WITHOUT SAVING",()=>QuitWithoutSaving0843(quitApplication),CampaignSaveUx0843.SaveSafetySummary0843(gm),safe,"CANCEL");
        }

        private void SaveAndQuit0843(bool quitApplication)
        {
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(AscendanceGameManager.Instance,out var message);
            if(!ok){confirmation0843.SetStatus(message,true);SetStatus0843(message,true);return;}
            SetStatus0843(message,false);confirmation0843.Hide();
            Time.timeScale=1f;
            if(quitApplication){CampaignAutoSave0842.SuppressNextQuitSave0843();Application.Quit();}
            else AscendanceSession.ReturnToMainMenu();
        }

        private void QuitWithoutSaving0843(bool quitApplication)
        {
            confirmation0843.Hide();Time.timeScale=1f;
            if(quitApplication){CampaignAutoSave0842.SuppressNextQuitSave0843();Application.Quit();}
            else AscendanceSession.ReturnToMainMenu();
        }

        public void DebugOpenQuitToMenuConfirmation0843(){if(!paused)SetPaused(true);RequestQuitToMenu0843();}
        public void DebugOpenQuitGameConfirmation0843(){if(!paused)SetPaused(true);RequestQuitGame0843();}
        public void DebugCancelConfirmation0843()=>confirmation0843?.DebugCancel0843();
        public void DebugResetPause0843()=>SetPaused(false);
    }
}
