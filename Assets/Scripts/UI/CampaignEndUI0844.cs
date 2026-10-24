using UnityEngine;
using UnityEngine.UI;

using Ascendance.Online;

namespace Ascendance
{
    public sealed class CampaignEndUI0844 : MonoBehaviour
    {
        public static CampaignEndUI0844 Instance { get; private set; }
        public bool IsOpen => root!=null&&root.activeSelf;
        public string DebugHeading0844 => heading?.text??string.Empty;
        public string DebugTitle0844 => titleText?.text??string.Empty;
        public string DebugOverview0844 => overviewText?.text??string.Empty;
        public string DebugRewards0844 => rewardsText?.text??string.Empty;
        public string DebugParty0844 => partyText?.text??string.Empty;
        public string DebugQuestRecord0844 => questRecordText?.text??string.Empty;
        public string DebugSaveStatus0844 => saveStatusText?.text??string.Empty;
        public bool DebugHasReturnToMenu0844 => returnButton!=null;
        public bool DebugHasStartNewCampaign0844 => newCampaignButton!=null;
        public bool DebugHasContinueToValewatch0845 => continueCampaignButton0845!=null&&continueCampaignButton0845.gameObject.activeSelf;

        private GameObject root;
        private Text heading,titleText,overviewText,rewardsText,partyText,questRecordText,saveStatusText;
        private Button returnButton,newCampaignButton,continueCampaignButton0845;
        private ShellConfirmationDialog0843 confirmation;

        private void Awake(){Instance=this;Build();SetVisible(false);}
        private void OnDestroy(){if(Instance==this)Instance=null;}

        public void Open()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||!gm.CampaignEndAvailable0844)return;
            QuestResultsUI0835.Instance?.Close();DowntimeUI0836.Instance?.Close();CampaignHubUI0834.Instance?.Close();QuestBriefingUI0841.Instance?.CloseForResume0842();
            SetVisible(true);Refresh();
        }
        public void Close(){confirmation?.Hide();SetVisible(false);}

        public void SaveFinalCampaign0844()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(gm,out var message);
            if(saveStatusText!=null){saveStatusText.text=message;saveStatusText.color=ok?ShellUIFactory.Gold:new Color(0.95f,0.48f,0.38f,1f);}
        }

        public void ReturnToMainMenu0844()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(gm,out var message);
            if(!ok){SetSaveStatus0844(message,true);return;}
            SetSaveStatus0844(message,false);Time.timeScale=1f;AscendanceSession.ReturnToMainMenu();
        }

        public void RequestContinueToValewatch0845()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||!gm.CanContinueToNextCampaign0856)return;var next=CampaignContent0845.Campaigns[gm.CurrentCampaignDefinition0845.ContinuationCampaignId];
            confirmation.Show($"CONTINUE TO CAMPAIGN {next.Number}?",$"Archive {gm.CurrentCampaignTitle0845} as Victory and carry this party forward to {next.Hub}? Persistent Hero, Equipment, Inventory, profession, Reputation, and campaign state will carry forward.",$"CONTINUE TO {next.Hub.ToUpperInvariant()}",ContinueToValewatch0845,"",null,$"{next.FirstQuestId} will become available in {next.FirstWorldEventId} — {CampaignContent0833.WorldEvents[next.FirstWorldEventId].Name}.",true,"CANCEL");
        }

        private void ContinueToValewatch0845()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            if(!CampaignSaveUx0843.SaveWithFeedback0843(gm,out var before)){confirmation.SetStatus(before,true);return;}
            bool continued=AscendanceClientCommandRouter0101.Instance?.RequestContinueCampaign()==true;if(!continued){confirmation.SetStatus("The next campaign continuation is not available.",true);return;}
            if(!CampaignSaveUx0843.SaveWithFeedback0843(gm,out var after))
            {
                confirmation.SetStatus(after+" The authoritative campaign transition remains committed; server persistence will retain the room state.",true);SetSaveStatus0844(after,true);Refresh();return;
            }
            confirmation.Hide();Close();if(gm.CampaignLaunchpadAvailable0845)CampaignLaunchpadUI0845.Instance?.Open();else CampaignHubUI0834.Instance?.Open();
        }

        public void RequestNewCampaign0844()
        {
            var gm=AscendanceGameManager.Instance;string label=gm==null?"campaign":$"Campaign {gm.CurrentCampaignDefinition0845.Number}";
            confirmation.Show("START A NEW CAMPAIGN?",$"Save this final {label} record, then return to party selection and begin a fresh campaign?","SAVE & PARTY SELECT",SaveAndStartNewCampaign0844,"",null,"The completed campaign remains your Continue save until the new campaign is actually saved.",true,"CANCEL");
        }

        private void SaveAndStartNewCampaign0844()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(gm,out var message);
            if(!ok){confirmation.SetStatus(message,true);SetSaveStatus0844(message,true);return;}
            confirmation.Hide();SetSaveStatus0844(message,false);Time.timeScale=1f;AscendanceSession.RequestNewCampaignSetup0844();
        }

        private void SetSaveStatus0844(string message,bool error)
        {
            if(saveStatusText==null)return;saveStatusText.text=message??string.Empty;saveStatusText.color=error?new Color(0.95f,0.48f,0.38f,1f):ShellUIFactory.Gold;
        }

        private void Build()
        {
            var canvas=ShellUIFactory.CreateCanvas("CampaignEndCanvas_0844",575);canvas.transform.SetParent(transform,false);
            root=ShellUIFactory.PanelRect(canvas.transform,"CampaignEndBackdrop",Vector2.zero,Vector2.one,new Color(0.003f,0.006f,0.010f,0.97f));
            var frame=ShellUIFactory.PanelRect(root.transform,"CampaignEndFrame",new Vector2(0.07f,0.045f),new Vector2(0.93f,0.955f),new Color(0.016f,0.022f,0.030f,0.998f));
            heading=ShellUIFactory.TextRect(frame.transform,"CAMPAIGN I COMPLETE",44,TextAnchor.MiddleCenter,new Vector2(0.04f,0.88f),new Vector2(0.96f,0.97f),ShellUIFactory.Gold);Fit(heading,24,44);
            titleText=ShellUIFactory.TextRect(frame.transform,"",24,TextAnchor.MiddleCenter,new Vector2(0.08f,0.82f),new Vector2(0.92f,0.88f),ShellUIFactory.Ivory);Fit(titleText,13,24);

            var left=ShellUIFactory.PanelRect(frame.transform,"CampaignSummary",new Vector2(0.035f,0.31f),new Vector2(0.49f,0.80f),new Color(0.028f,0.035f,0.044f,0.99f));
            ShellUIFactory.TextRect(left.transform,"CAMPAIGN RECORD",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.90f),new Vector2(0.965f,0.98f),ShellUIFactory.Gold);
            overviewText=ShellUIFactory.TextRect(left.transform,"",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.48f),new Vector2(0.965f,0.89f),ShellUIFactory.Ivory);Fit(overviewText,9,15);
            questRecordText=ShellUIFactory.TextRect(left.transform,"",13,TextAnchor.UpperLeft,new Vector2(0.035f,0.04f),new Vector2(0.965f,0.47f),new Color(0.74f,0.79f,0.84f));Fit(questRecordText,8,13);

            var right=ShellUIFactory.PanelRect(frame.transform,"FinalPartyRewards",new Vector2(0.51f,0.31f),new Vector2(0.965f,0.80f),new Color(0.028f,0.035f,0.044f,0.99f));
            ShellUIFactory.TextRect(right.transform,"FINAL PARTY",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.90f),new Vector2(0.965f,0.98f),ShellUIFactory.Gold);
            partyText=ShellUIFactory.TextRect(right.transform,"",14,TextAnchor.UpperLeft,new Vector2(0.035f,0.54f),new Vector2(0.965f,0.89f),ShellUIFactory.Ivory);Fit(partyText,8,14);
            ShellUIFactory.TextRect(right.transform,"FINAL REWARD / TITLE",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.45f),new Vector2(0.965f,0.53f),ShellUIFactory.Gold);
            rewardsText=ShellUIFactory.TextRect(right.transform,"",14,TextAnchor.UpperLeft,new Vector2(0.035f,0.04f),new Vector2(0.965f,0.44f),ShellUIFactory.Ivory);Fit(rewardsText,8,14);

            var saveButton=ShellUIFactory.ButtonRect(frame.transform,"SAVE FINAL CAMPAIGN",new Vector2(0.025f,0.17f),new Vector2(0.235f,0.265f),SaveFinalCampaign0844);Fit(saveButton.GetComponentInChildren<Text>(),9,18);
            returnButton=ShellUIFactory.ButtonRect(frame.transform,"RETURN TO MAIN MENU",new Vector2(0.255f,0.17f),new Vector2(0.465f,0.265f),ReturnToMainMenu0844);Fit(returnButton.GetComponentInChildren<Text>(),9,18);
            continueCampaignButton0845=ShellUIFactory.ButtonRect(frame.transform,"CONTINUE TO VALEWATCH",new Vector2(0.485f,0.17f),new Vector2(0.745f,0.265f),RequestContinueToValewatch0845);Fit(continueCampaignButton0845.GetComponentInChildren<Text>(),9,18);
            newCampaignButton=ShellUIFactory.ButtonRect(frame.transform,"START NEW CAMPAIGN",new Vector2(0.765f,0.17f),new Vector2(0.975f,0.265f),RequestNewCampaign0844);Fit(newCampaignButton.GetComponentInChildren<Text>(),9,18);
            saveStatusText=ShellUIFactory.TextRect(frame.transform,"Campaign complete. Save the final record or return to the Main Menu.",12,TextAnchor.MiddleCenter,new Vector2(0.06f,0.09f),new Vector2(0.94f,0.145f),new Color(0.62f,0.67f,0.72f));Fit(saveStatusText,9,12);
            confirmation=ShellConfirmationDialog0843.Create(canvas.transform,"CampaignEndConfirmation0844");
        }

        private void Refresh()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||!gm.CampaignEndAvailable0844)return;
            bool victory=gm.CampaignCompleted0833;
            heading.text=victory?$"CAMPAIGN {gm.CurrentCampaignDefinition0845.Number} VICTORY":$"CAMPAIGN {gm.CurrentCampaignDefinition0845.Number} DEFEAT";
            heading.color=victory?ShellUIFactory.Gold:new Color(0.95f,0.48f,0.38f,1f);
            titleText.text=victory?gm.CampaignEndTitle0844:$"{gm.CurrentCampaignTitle0845.ToUpperInvariant()} • DEFEAT";
            overviewText.text=gm.CampaignEndOverview0844;
            questRecordText.text=gm.CampaignEndQuestRecord0844;
            partyText.text=gm.CampaignEndPartySummary0844;
            rewardsText.text=gm.CampaignEndRewardSummary0844;
            if(saveStatusText!=null)saveStatusText.text=$"Campaign {gm.CurrentCampaignDefinition0845.Number} is over. Save the final record or return to the Main Menu.";
            if(continueCampaignButton0845!=null){continueCampaignButton0845.gameObject.SetActive(victory&&gm.CanContinueToNextCampaign0856);var t=continueCampaignButton0845.GetComponentInChildren<Text>();if(t!=null&&gm.CanContinueToNextCampaign0856){var next=CampaignContent0845.Campaigns[gm.CurrentCampaignDefinition0845.ContinuationCampaignId];t.text=$"CONTINUE TO {next.Hub.ToUpperInvariant()}";}}
        }

        private void SetVisible(bool visible){if(root!=null)root.SetActive(visible);}
        private static void Fit(Text t,int min,int max){if(t==null)return;t.resizeTextForBestFit=true;t.resizeTextMinSize=min;t.resizeTextMaxSize=max;}
    }
}
