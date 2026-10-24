using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Ascendance.Online;

namespace Ascendance
{
    public sealed class CampaignHubUI0834 : MonoBehaviour
    {
        public static CampaignHubUI0834 Instance { get; private set; }
        public bool IsOpen => rootPanel!=null&&rootPanel.activeSelf;

        private Canvas canvas;
        private GameObject rootPanel;
        private Transform questRoot;
        private Text campaignTitle;
        private Text worldEventTitle;
        private Text progressText;
        private Text outcomeText;
        private Text campaignStateText;
        private Image progressFill;
        private Button advanceButton;
        private Button downtimeButton;
        private readonly List<GameObject> questCards=new();

        private static readonly Color Backdrop=new(0.005f,0.009f,0.014f,0.90f);
        private static readonly Color Card=new(0.035f,0.043f,0.052f,0.985f);
        private static readonly Color Active=new(0.16f,0.12f,0.045f,1f);
        private static readonly Color Completed=new(0.055f,0.15f,0.095f,1f);
        private static readonly Color Failed=new(0.20f,0.075f,0.055f,1f);
        private static readonly Color Missed=new(0.105f,0.105f,0.105f,1f);
        private static readonly Color Locked=new(0.055f,0.060f,0.068f,1f);

        private void Awake()
        {
            Instance=this;
            Build();
            SetVisible(false);
        }

        private void Start()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm!=null)gm.StateChanged+=Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm!=null)gm.StateChanged-=Refresh;
            if(Instance==this)Instance=null;
        }

        public void Open(){SetVisible(true);Refresh();}
        public void Close(){SetVisible(false);}
        public void Toggle(){if(IsOpen)Close();else Open();}

        private void SetVisible(bool visible)
        {
            if(rootPanel!=null)rootPanel.SetActive(visible);
        }

        private void Build()
        {
            canvas=ShellUIFactory.CreateCanvas("CampaignHubCanvas_0834",500);
            canvas.transform.SetParent(transform,false);

            rootPanel=ShellUIFactory.PanelRect(canvas.transform,"CampaignHubBackdrop",Vector2.zero,Vector2.one,Backdrop);
            var frame=ShellUIFactory.PanelRect(rootPanel.transform,"CampaignHubFrame",new Vector2(0.075f,0.065f),new Vector2(0.925f,0.94f),new Color(0.018f,0.024f,0.032f,0.995f));

            campaignTitle=ShellUIFactory.TextRect(frame.transform,"CAMPAIGN I  •  THE GOBLIN WAR",34,TextAnchor.MiddleLeft,new Vector2(0.035f,0.89f),new Vector2(0.66f,0.97f),ShellUIFactory.Gold);Fit(campaignTitle,18,34);
            downtimeButton=ShellUIFactory.ButtonRect(frame.transform,"STONEBROOK",new Vector2(0.675f,0.905f),new Vector2(0.82f,0.96f),()=>{Close();DowntimeUI0836.Instance?.Open();});
            Fit(downtimeButton.GetComponentInChildren<Text>(),9,16);
            ShellUIFactory.ButtonRect(frame.transform,"CLOSE",new Vector2(0.83f,0.905f),new Vector2(0.965f,0.96f),Close);

            worldEventTitle=ShellUIFactory.TextRect(frame.transform,"",27,TextAnchor.MiddleLeft,new Vector2(0.035f,0.815f),new Vector2(0.965f,0.885f),ShellUIFactory.Ivory);Fit(worldEventTitle,16,27);
            progressText=ShellUIFactory.TextRect(frame.transform,"",16,TextAnchor.MiddleLeft,new Vector2(0.035f,0.755f),new Vector2(0.965f,0.815f),new Color(0.74f,0.78f,0.82f));Fit(progressText,11,16);

            var barBack=ShellUIFactory.PanelRect(frame.transform,"DayPressureBack",new Vector2(0.035f,0.725f),new Vector2(0.965f,0.75f),new Color(0.055f,0.060f,0.068f,1f));
            var fill=new GameObject("DayPressureFill");fill.transform.SetParent(barBack.transform,false);var frt=fill.AddComponent<RectTransform>();frt.anchorMin=Vector2.zero;frt.anchorMax=Vector2.one;frt.offsetMin=frt.offsetMax=Vector2.zero;progressFill=fill.AddComponent<Image>();progressFill.color=ShellUIFactory.Gold;progressFill.type=Image.Type.Filled;progressFill.fillMethod=Image.FillMethod.Horizontal;progressFill.fillOrigin=(int)Image.OriginHorizontal.Left;

            campaignStateText=ShellUIFactory.TextRect(frame.transform,"",14,TextAnchor.MiddleLeft,new Vector2(0.035f,0.67f),new Vector2(0.965f,0.72f),ShellUIFactory.Gold);Fit(campaignStateText,10,14);

            var qr=new GameObject("WorldEventQuests");qr.transform.SetParent(frame.transform,false);var qrt=qr.AddComponent<RectTransform>();qrt.anchorMin=new Vector2(0.035f,0.245f);qrt.anchorMax=new Vector2(0.965f,0.66f);qrt.offsetMin=qrt.offsetMax=Vector2.zero;questRoot=qr.transform;

            var outcomePanel=ShellUIFactory.PanelRect(frame.transform,"OutcomePreview",new Vector2(0.035f,0.075f),new Vector2(0.73f,0.225f),new Color(0.032f,0.038f,0.046f,0.98f));
            ShellUIFactory.TextRect(outcomePanel.transform,"WORLD EVENT OUTCOME",14,TextAnchor.UpperLeft,new Vector2(0.025f,0.72f),new Vector2(0.975f,0.96f),ShellUIFactory.Gold);
            outcomeText=ShellUIFactory.TextRect(outcomePanel.transform,"",15,TextAnchor.UpperLeft,new Vector2(0.025f,0.08f),new Vector2(0.975f,0.72f),ShellUIFactory.Ivory);Fit(outcomeText,10,15);

            advanceButton=ShellUIFactory.ButtonRect(frame.transform,"ADVANCE WORLD EVENT",new Vector2(0.755f,0.115f),new Vector2(0.965f,0.205f),()=>AscendanceClientCommandRouter0101.Instance?.RequestAdvanceWorldEvent());
            Fit(advanceButton.GetComponentInChildren<Text>(),10,18);
            var note=ShellUIFactory.TextRect(frame.transform,"Quest selection and World Event advancement are driven by the authoritative campaign runtime.",12,TextAnchor.MiddleCenter,new Vector2(0.755f,0.075f),new Vector2(0.965f,0.11f),new Color(0.56f,0.60f,0.64f));Fit(note,9,12);
        }

        private void Refresh()
        {
            if(rootPanel==null)return;
            var gm=AscendanceGameManager.Instance;if(gm==null)return;

            campaignTitle.text=$"CAMPAIGN {gm.CurrentCampaignDefinition0845.Number}  •  {gm.CurrentCampaignTitle0845.ToUpperInvariant()}";var hubLabel=downtimeButton?.GetComponentInChildren<Text>();if(hubLabel!=null)hubLabel.text=gm.CurrentCampaignHub0845.ToUpperInvariant();
            worldEventTitle.text=$"{gm.CurrentWorldEventId0833}  •  {gm.CurrentWorldEventName0833}";
            progressText.text=gm.CurrentWorldEventProgressLabel0834;
            progressFill.fillAmount=gm.CurrentWorldEventDayPressure0834;
            campaignStateText.text=gm.CampaignCompleted0833?"CAMPAIGN STATE  •  VICTORY":gm.CampaignDefeated0833?"CAMPAIGN STATE  •  DEFEAT":gm.CanAdvanceWorldEvent0833?"CAMPAIGN STATE  •  WORLD EVENT ADVANCE AVAILABLE":"CAMPAIGN STATE  •  ACTIVE";
            outcomeText.text=gm.CurrentWorldEventOutcomePreview0834;

            foreach(var go in questCards)if(go!=null)Destroy(go);
            questCards.Clear();

            var ids=gm.CurrentWorldEventQuestIds0834;
            int count=Mathf.Max(1,ids.Count);
            for(int i=0;i<ids.Count;i++)BuildQuestCard(gm,ids[i],i,count);

            downtimeButton.gameObject.SetActive(gm.IsDowntimeAvailable0836);
            downtimeButton.interactable=gm.IsDowntimeAvailable0836;

            bool showAdvance=!gm.CampaignCompleted0833&&!gm.CampaignDefeated0833&&CampaignContent0833.WorldEvents.TryGetValue(gm.CurrentWorldEventId0833,out var activeEvent)&&!string.IsNullOrEmpty(activeEvent.NextId);
            advanceButton.gameObject.SetActive(showAdvance);
            advanceButton.interactable=showAdvance&&gm.CanAdvanceWorldEvent0833;
            if(showAdvance)advanceButton.GetComponentInChildren<Text>().text=$"ADVANCE {gm.CurrentWorldEventId0833}";
        }

        private void BuildQuestCard(AscendanceGameManager gm,string questId,int index,int count)
        {
            float gap=0.012f;
            float width=(1f-gap*(count-1))/count;
            float left=index*(width+gap);
            var status=gm.GetCampaignQuestStatus0834(questId);
            Color c=StatusColor(status);
            var card=ShellUIFactory.PanelRect(questRoot,"CampaignQuest_"+questId,new Vector2(left,0f),new Vector2(left+width,1f),Card);
            questCards.Add(card);

            var band=ShellUIFactory.PanelRect(card.transform,"StatusBand",new Vector2(0.02f,0.84f),new Vector2(0.98f,0.98f),c);
            var path=ShellUIFactory.TextRect(band.transform,gm.GetCampaignQuestPathLabel0834(questId),13,TextAnchor.MiddleLeft,new Vector2(0.035f,0.08f),new Vector2(0.47f,0.92f),ShellUIFactory.Ivory);Fit(path,9,13);
            var statusText=ShellUIFactory.TextRect(band.transform,gm.GetCampaignQuestStatusLabel0834(questId),13,TextAnchor.MiddleRight,new Vector2(0.47f,0.08f),new Vector2(0.965f,0.92f),ShellUIFactory.Ivory);Fit(statusText,8,13);

            QuestContent0829.Definitions.TryGetValue(questId,out var def);
            string name=def?.Name??questId;
            string story=def?.Story??string.Empty;
            string victory=def?.Victory??string.Empty;
            string reward=def?.Reward??string.Empty;
            var heading=ShellUIFactory.TextRect(card.transform,$"{questId}  •  {name}",22,TextAnchor.UpperLeft,new Vector2(0.045f,0.70f),new Vector2(0.955f,0.83f),ShellUIFactory.Gold);Fit(heading,13,22);
            var storyText=ShellUIFactory.TextRect(card.transform,story,15,TextAnchor.UpperLeft,new Vector2(0.045f,0.51f),new Vector2(0.955f,0.70f),ShellUIFactory.Ivory);Fit(storyText,10,15);
            var objective=ShellUIFactory.TextRect(card.transform,"OBJECTIVE\n"+victory,14,TextAnchor.UpperLeft,new Vector2(0.045f,0.29f),new Vector2(0.955f,0.50f),new Color(0.77f,0.81f,0.85f));Fit(objective,9,14);
            var rewardText=ShellUIFactory.TextRect(card.transform,"REWARD\n"+reward,13,TextAnchor.UpperLeft,new Vector2(0.045f,0.12f),new Vector2(0.955f,0.29f),new Color(0.70f,0.73f,0.76f));Fit(rewardText,8,13);

            bool canBegin=gm.CanBeginCampaignQuest0833(questId);
            string buttonLabel=status==CampaignQuestStatus0834.Failed&&canBegin?"REVIEW RETRY "+questId:canBegin?"BRIEF "+questId:gm.GetCampaignQuestStatusLabel0834(questId);
            var button=ShellUIFactory.ButtonRect(card.transform,buttonLabel,new Vector2(0.11f,0.025f),new Vector2(0.89f,0.105f),()=>BeginQuest(questId));
            button.interactable=canBegin;
            Fit(button.GetComponentInChildren<Text>(),9,16);
        }

        private void BeginQuest(string questId)
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            if(QuestBriefingUI0841.Instance!=null&&QuestBriefingUI0841.Instance.Open(questId))Close();
            else Refresh();
        }

        private static Color StatusColor(CampaignQuestStatus0834 status)
        {
            return status switch
            {
                CampaignQuestStatus0834.Active=>Active,
                CampaignQuestStatus0834.Available=>Active,
                CampaignQuestStatus0834.Completed=>Completed,
                CampaignQuestStatus0834.Failed=>Failed,
                CampaignQuestStatus0834.Missed=>Missed,
                _=>Locked
            };
        }

        private static void Fit(Text text,int min,int max)
        {
            if(text==null)return;text.resizeTextForBestFit=true;text.resizeTextMinSize=min;text.resizeTextMaxSize=max;
        }
    }
}
