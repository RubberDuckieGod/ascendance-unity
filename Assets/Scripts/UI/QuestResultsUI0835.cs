using UnityEngine;
using UnityEngine.UI;

namespace Ascendance
{
    public sealed class QuestResultsUI0835 : MonoBehaviour
    {
        public static QuestResultsUI0835 Instance { get; private set; }
        public bool IsOpen => rootPanel!=null&&rootPanel.activeSelf;
        public string DebugHeading0835 => heading?.text??string.Empty;
        public string DebugRewards0835 => rewardsText?.text??string.Empty;
        public string DebugCampaignImpact0835 => campaignText?.text??string.Empty;
        public string DebugSaveStatus0843 => saveStatusText?.text??string.Empty;

        private GameObject rootPanel;
        private Text heading;
        private Text questTitle;
        private Text resolutionText;
        private Text objectiveText;
        private Text rewardsText;
        private Text campaignText;
        private Text saveStatusText;
        private Button returnButton;
        private string autoOpenedResolutionKey;
        private string lastObservedQuestId;
        private bool lastObservedResolved;
        private float nextPoll;

        private static readonly Color Backdrop=new(0.004f,0.007f,0.011f,0.93f);
        private static readonly Color Success=new(0.055f,0.15f,0.095f,1f);
        private static readonly Color Failure=new(0.20f,0.075f,0.055f,1f);

        private void Awake()
        {
            Instance=this;
            Build();
            SetVisible(false);
        }

        private void Start()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm!=null)gm.StateChanged+=OnStateChanged;
            lastObservedQuestId=gm?.CurrentQuestId0829;
            lastObservedResolved=gm!=null&&(gm.QuestComplete||gm.QuestFailed);
            if(lastObservedResolved)AutoOpenResolved(gm);
        }

        private void OnDestroy()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm!=null)gm.StateChanged-=OnStateChanged;
            if(Instance==this)Instance=null;
        }

        private void Update()
        {
            if(Time.unscaledTime<nextPoll)return;
            nextPoll=Time.unscaledTime+0.20f;
            ObserveResolution();
        }

        public void Open()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm==null||(!gm.QuestComplete&&!gm.QuestFailed))return;
            gm.EnsureQuestResultSnapshot0835();
            SetVisible(true);
            Refresh();
        }

        public void Close()=>SetVisible(false);

        public void ReturnToCampaign()
        {
            Close();
            var gm=AscendanceGameManager.Instance;
            if(gm!=null&&gm.CampaignEndAvailable0844){CampaignEndUI0844.Instance?.Open();return;}
            if(gm!=null&&gm.IsDowntimeAvailable0836)
            {
                DowntimeUI0836.Instance?.Open();
                return;
            }
            var hub=CampaignHubUI0834.Instance;
            if(hub!=null)hub.Open();
        }

        public void SaveCampaign()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm==null)return;
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(gm,out var message);
            if(saveStatusText!=null)
            {
                saveStatusText.text=message;
                saveStatusText.color=ok?ShellUIFactory.Gold:new Color(0.95f,0.48f,0.38f,1f);
            }
        }

        private void OnStateChanged()
        {
            ObserveResolution();
            if(IsOpen)Refresh();
        }

        private void ObserveResolution()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm==null)return;
            bool resolved=gm.QuestComplete||gm.QuestFailed;
            if(!resolved)
            {
                if(lastObservedResolved||!string.Equals(lastObservedQuestId,gm.CurrentQuestId0829,System.StringComparison.OrdinalIgnoreCase))Close();
                lastObservedQuestId=gm.CurrentQuestId0829;
                lastObservedResolved=false;
                return;
            }

            string key=gm.CurrentQuestId0829+"|"+(gm.QuestComplete?"C":"F");
            if(!string.Equals(autoOpenedResolutionKey,key,System.StringComparison.OrdinalIgnoreCase))AutoOpenResolved(gm);
            lastObservedQuestId=gm.CurrentQuestId0829;
            lastObservedResolved=true;
        }

        private void AutoOpenResolved(AscendanceGameManager gm)
        {
            if(gm==null)return;
            CampaignHubUI0834.Instance?.Close();
            gm.EnsureQuestResultSnapshot0835();
            autoOpenedResolutionKey=gm.CurrentQuestId0829+"|"+(gm.QuestComplete?"C":"F");
            Open();
        }

        private void SetVisible(bool visible)
        {
            if(rootPanel!=null)rootPanel.SetActive(visible);
        }

        private void Build()
        {
            var canvas=ShellUIFactory.CreateCanvas("QuestResultsCanvas_0835",550);
            canvas.transform.SetParent(transform,false);
            rootPanel=ShellUIFactory.PanelRect(canvas.transform,"QuestResultsBackdrop",Vector2.zero,Vector2.one,Backdrop);
            var frame=ShellUIFactory.PanelRect(rootPanel.transform,"QuestResultsFrame",new Vector2(0.14f,0.08f),new Vector2(0.86f,0.92f),new Color(0.018f,0.024f,0.032f,0.998f));

            heading=ShellUIFactory.TextRect(frame.transform,"QUEST COMPLETE",40,TextAnchor.MiddleCenter,new Vector2(0.04f,0.88f),new Vector2(0.96f,0.97f),ShellUIFactory.Gold);Fit(heading,22,40);
            questTitle=ShellUIFactory.TextRect(frame.transform,"",24,TextAnchor.MiddleCenter,new Vector2(0.05f,0.82f),new Vector2(0.95f,0.88f),ShellUIFactory.Ivory);Fit(questTitle,14,24);
            resolutionText=ShellUIFactory.TextRect(frame.transform,"",16,TextAnchor.MiddleCenter,new Vector2(0.06f,0.75f),new Vector2(0.94f,0.82f),new Color(0.78f,0.82f,0.86f));Fit(resolutionText,10,16);

            var objectivePanel=ShellUIFactory.PanelRect(frame.transform,"ObjectiveResult",new Vector2(0.04f,0.57f),new Vector2(0.48f,0.735f),new Color(0.032f,0.038f,0.046f,0.98f));
            ShellUIFactory.TextRect(objectivePanel.transform,"OBJECTIVE OUTCOME",14,TextAnchor.UpperLeft,new Vector2(0.035f,0.70f),new Vector2(0.965f,0.94f),ShellUIFactory.Gold);
            objectiveText=ShellUIFactory.TextRect(objectivePanel.transform,"",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.08f),new Vector2(0.965f,0.70f),ShellUIFactory.Ivory);Fit(objectiveText,9,15);

            var rewardPanel=ShellUIFactory.PanelRect(frame.transform,"RewardsGranted",new Vector2(0.52f,0.57f),new Vector2(0.96f,0.735f),new Color(0.032f,0.038f,0.046f,0.98f));
            ShellUIFactory.TextRect(rewardPanel.transform,"ACTUAL REWARDS",14,TextAnchor.UpperLeft,new Vector2(0.035f,0.70f),new Vector2(0.965f,0.94f),ShellUIFactory.Gold);
            rewardsText=ShellUIFactory.TextRect(rewardPanel.transform,"",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.08f),new Vector2(0.965f,0.70f),ShellUIFactory.Ivory);Fit(rewardsText,9,15);

            var campaignPanel=ShellUIFactory.PanelRect(frame.transform,"CampaignConsequence",new Vector2(0.04f,0.29f),new Vector2(0.96f,0.545f),new Color(0.027f,0.033f,0.041f,0.99f));
            ShellUIFactory.TextRect(campaignPanel.transform,"CAMPAIGN CONSEQUENCE",15,TextAnchor.UpperLeft,new Vector2(0.025f,0.79f),new Vector2(0.975f,0.95f),ShellUIFactory.Gold);
            campaignText=ShellUIFactory.TextRect(campaignPanel.transform,"",16,TextAnchor.UpperLeft,new Vector2(0.025f,0.07f),new Vector2(0.975f,0.79f),ShellUIFactory.Ivory);Fit(campaignText,10,16);

            returnButton=ShellUIFactory.ButtonRect(frame.transform,"RETURN TO CAMPAIGN",new Vector2(0.08f,0.12f),new Vector2(0.48f,0.235f),ReturnToCampaign);
            Fit(returnButton.GetComponentInChildren<Text>(),12,24);
            var saveButton=ShellUIFactory.ButtonRect(frame.transform,"SAVE CAMPAIGN",new Vector2(0.52f,0.12f),new Vector2(0.92f,0.235f),SaveCampaign);
            Fit(saveButton.GetComponentInChildren<Text>(),12,24);
            saveStatusText=ShellUIFactory.TextRect(frame.transform,"",12,TextAnchor.MiddleCenter,new Vector2(0.08f,0.055f),new Vector2(0.92f,0.105f),new Color(0.62f,0.66f,0.70f));Fit(saveStatusText,9,12);
        }

        private void Refresh()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm==null)return;
            var s=gm.EnsureQuestResultSnapshot0835();
            if(s==null)return;

            if(s.CampaignVictory)heading.text=$"CAMPAIGN {gm.CurrentCampaignDefinition0845.Number} VICTORY";
            else if(s.CampaignDefeat)heading.text=$"CAMPAIGN {gm.CurrentCampaignDefinition0845.Number} DEFEAT";
            else heading.text=s.Success?"QUEST COMPLETE":"QUEST FAILED";
            heading.color=s.Success?ShellUIFactory.Gold:new Color(0.95f,0.48f,0.38f,1f);
            questTitle.text=$"{s.QuestId}  •  {s.QuestName}";
            resolutionText.text=s.ResolutionText;
            objectiveText.text=s.Success?"COMPLETED\n"+s.ObjectiveText:"FAILED\n"+(string.IsNullOrWhiteSpace(s.FailureText)?"Quest failure condition was met.":s.FailureText);
            rewardsText.text=s.Success?s.ActualRewardSummary:"No victory reward granted.";
            campaignText.text=gm.CurrentQuestResultCampaignImpact0835;
            returnButton.GetComponentInChildren<Text>().text=s.CampaignVictory||s.CampaignDefeat?"VIEW CAMPAIGN END":$"RETURN TO {gm.CurrentCampaignHub0845.ToUpperInvariant()}";
            returnButton.GetComponent<Image>().color=s.Success?Success:Failure;
        }

        private static void Fit(Text text,int min,int max)
        {
            if(text==null)return;
            text.resizeTextForBestFit=true;
            text.resizeTextMinSize=min;
            text.resizeTextMaxSize=max;
        }
    }
}
