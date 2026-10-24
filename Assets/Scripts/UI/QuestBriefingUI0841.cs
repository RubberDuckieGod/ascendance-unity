using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Ascendance.Online;

namespace Ascendance
{
    public sealed class QuestBriefingUI0841 : MonoBehaviour
    {
        public static QuestBriefingUI0841 Instance { get; private set; }
        public bool IsOpen => rootPanel!=null&&rootPanel.activeSelf;
        public string DebugQuestId0841 => AscendanceGameManager.Instance?.PendingQuestBriefingId0841??string.Empty;
        public string DebugObjectiveText0841 => objectiveText?.text??string.Empty;
        public string DebugWorldEventText0841 => worldEventText?.text??string.Empty;
        public string DebugPartyText0841 => partyText?.text??string.Empty;
        public bool DebugBeginInteractable0841 => beginButton!=null&&beginButton.interactable;

        private GameObject rootPanel;
        private Text questTitle;
        private Text storyText;
        private Text objectiveText;
        private Text worldEventText;
        private Text partyText;
        private Text readyText;
        private Button beginButton;
        private Transform readyRoot;
        private readonly List<GameObject> readyObjects=new();

        private static readonly Color Backdrop=new(0.003f,0.006f,0.010f,0.95f);
        private static readonly Color Card=new(0.028f,0.035f,0.044f,0.995f);

        private void Awake(){Instance=this;Build();SetVisible(false);}
        private void Start(){var gm=AscendanceGameManager.Instance;if(gm!=null)gm.StateChanged+=OnStateChanged;Refresh();}
        private void OnDestroy(){var gm=AscendanceGameManager.Instance;if(gm!=null)gm.StateChanged-=OnStateChanged;if(Instance==this)Instance=null;}

        public bool Open(string questId)
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||AscendanceClientCommandRouter0101.Instance==null||!AscendanceClientCommandRouter0101.Instance.RequestOpenQuestBriefing(questId))return false;
            CampaignHubUI0834.Instance?.Close();DowntimeUI0836.Instance?.Close();QuestResultsUI0835.Instance?.Close();
            SetVisible(true);Refresh();return true;
        }

        public void CloseForResume0842()=>SetVisible(false);

        public void Cancel()
        {
            var gm=AscendanceGameManager.Instance;if(gm!=null)AscendanceClientCommandRouter0101.Instance?.RequestCancelQuestBriefing();SetVisible(false);CampaignHubUI0834.Instance?.Open();
        }

        public void Confirm()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            if(AscendanceClientCommandRouter0101.Instance?.RequestConfirmQuestBriefing()==true)SetVisible(false);else Refresh();
        }

        private void OnStateChanged()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            if(IsOpen&&!gm.IsQuestBriefingOpen0841){SetVisible(false);return;}
            if(IsOpen)Refresh();
        }

        private void Build()
        {
            var canvas=ShellUIFactory.CreateCanvas("QuestBriefingCanvas_0841",550);canvas.transform.SetParent(transform,false);
            rootPanel=ShellUIFactory.PanelRect(canvas.transform,"QuestBriefingBackdrop",Vector2.zero,Vector2.one,Backdrop);
            var frame=ShellUIFactory.PanelRect(rootPanel.transform,"QuestBriefingFrame",new Vector2(0.06f,0.045f),new Vector2(0.94f,0.955f),new Color(0.014f,0.020f,0.028f,0.998f));

            var kicker=ShellUIFactory.TextRect(frame.transform,"QUEST BRIEFING  •  READY-UP",18,TextAnchor.MiddleLeft,new Vector2(0.03f,0.925f),new Vector2(0.55f,0.975f),ShellUIFactory.Gold);Fit(kicker,11,18);
            ShellUIFactory.ButtonRect(frame.transform,"BACK",new Vector2(0.855f,0.93f),new Vector2(0.97f,0.972f),Cancel);
            questTitle=ShellUIFactory.TextRect(frame.transform,"",32,TextAnchor.MiddleLeft,new Vector2(0.03f,0.855f),new Vector2(0.97f,0.925f),ShellUIFactory.Ivory);Fit(questTitle,18,32);

            var mission=ShellUIFactory.PanelRect(frame.transform,"MissionCard",new Vector2(0.03f,0.49f),new Vector2(0.62f,0.845f),Card);
            storyText=ShellUIFactory.TextRect(mission.transform,"",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.70f),new Vector2(0.965f,0.94f),ShellUIFactory.Ivory);Fit(storyText,9,15);
            objectiveText=ShellUIFactory.TextRect(mission.transform,"",14,TextAnchor.UpperLeft,new Vector2(0.035f,0.06f),new Vector2(0.965f,0.68f),new Color(0.74f,0.79f,0.84f));Fit(objectiveText,8,14);

            var stakes=ShellUIFactory.PanelRect(frame.transform,"StakesCard",new Vector2(0.64f,0.49f),new Vector2(0.97f,0.845f),Card);
            ShellUIFactory.TextRect(stakes.transform,"WORLD EVENT STAKES",15,TextAnchor.UpperLeft,new Vector2(0.04f,0.86f),new Vector2(0.96f,0.97f),ShellUIFactory.Gold);
            worldEventText=ShellUIFactory.TextRect(stakes.transform,"",13,TextAnchor.UpperLeft,new Vector2(0.04f,0.06f),new Vector2(0.96f,0.85f),ShellUIFactory.Ivory);Fit(worldEventText,8,13);

            var party=ShellUIFactory.PanelRect(frame.transform,"PartyCard",new Vector2(0.03f,0.16f),new Vector2(0.97f,0.47f),Card);
            ShellUIFactory.TextRect(party.transform,"PARTY & LOADOUT  •  every participating Hero must be READY",15,TextAnchor.MiddleLeft,new Vector2(0.025f,0.84f),new Vector2(0.975f,0.97f),ShellUIFactory.Gold);
            partyText=ShellUIFactory.TextRect(party.transform,"",12,TextAnchor.UpperLeft,new Vector2(0.025f,0.50f),new Vector2(0.975f,0.83f),new Color(0.70f,0.74f,0.78f));Fit(partyText,7,12);
            var rr=new GameObject("ReadyButtons");rr.transform.SetParent(party.transform,false);var rrt=rr.AddComponent<RectTransform>();rrt.anchorMin=new Vector2(0.02f,0.08f);rrt.anchorMax=new Vector2(0.98f,0.48f);rrt.offsetMin=rrt.offsetMax=Vector2.zero;readyRoot=rr.transform;

            readyText=ShellUIFactory.TextRect(frame.transform,"",14,TextAnchor.MiddleLeft,new Vector2(0.03f,0.075f),new Vector2(0.62f,0.145f),new Color(0.70f,0.74f,0.78f));Fit(readyText,9,14);
            beginButton=ShellUIFactory.ButtonRect(frame.transform,"BEGIN QUEST",new Vector2(0.66f,0.07f),new Vector2(0.97f,0.145f),Confirm);Fit(beginButton.GetComponentInChildren<Text>(),11,20);
            var note=ShellUIFactory.TextRect(frame.transform,"Opening this briefing does not advance Campaign Day. The authoritative Quest launch occurs only when BEGIN QUEST succeeds.",11,TextAnchor.MiddleLeft,new Vector2(0.03f,0.025f),new Vector2(0.97f,0.065f),new Color(0.50f,0.55f,0.60f));Fit(note,7,11);
        }

        private void Refresh()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||rootPanel==null)return;
            var def=gm.PendingQuestDefinition0841;if(def==null){if(IsOpen)SetVisible(false);return;}
            questTitle.text=$"{def.Id}  •  {def.Name}  •  {gm.GetCampaignQuestPathLabel0834(def.Id)}";
            storyText.text=def.Story;
            objectiveText.text=$"OBJECTIVE\n{def.Victory}\n\nFAILURE\n{def.Failure}\n\nREWARD\n{def.Reward}";
            worldEventText.text=gm.QuestBriefingWorldEventText0841;
            partyText.text=string.Join("\n",gm.ParticipatingHeroes.Select(h=>gm.QuestBriefingPartySummary0841(h.Id)));
            readyText.text=$"READY  •  {gm.ReadyHeroCount0841}/{gm.RequiredReadyHeroCount0841} HEROES   |   {(gm.CanConfirmBriefedQuest0841?"PARTY READY":"WAITING FOR PARTY")}";
            beginButton.interactable=gm.CanConfirmBriefedQuest0841;
            beginButton.GetComponentInChildren<Text>().text=gm.GetCampaignQuestStatus0834(def.Id)==CampaignQuestStatus0834.Failed?"RETRY QUEST":"BEGIN QUEST";
            RebuildReadyButtons(gm);
        }

        private void RebuildReadyButtons(AscendanceGameManager gm)
        {
            foreach(var go in readyObjects)if(go!=null)Destroy(go);readyObjects.Clear();
            var heroes=gm.ParticipatingHeroes.ToList();int count=Mathf.Max(1,heroes.Count);
            for(int i=0;i<heroes.Count;i++)
            {
                var hero=heroes[i];string heroId=hero.Id;float width=1f/count;float left=i*width+0.005f;float right=(i+1)*width-0.005f;
                bool ready=gm.IsHeroReadyForQuest0841(hero.Id);
                var b=ShellUIFactory.ButtonRect(readyRoot,$"{hero.DisplayName}\n{(ready?"READY":"MARK READY")}",new Vector2(left,0.05f),new Vector2(right,0.95f),()=>AscendanceClientCommandRouter0101.Instance?.RequestToggleQuestReady(heroId));
                Fit(b.GetComponentInChildren<Text>(),8,13);if(ready)b.GetComponent<Image>().color=new Color(0.055f,0.22f,0.12f,1f);readyObjects.Add(b.gameObject);
            }
        }

        private void SetVisible(bool visible){if(rootPanel!=null)rootPanel.SetActive(visible);}
        private static void Fit(Text text,int min,int max){if(text==null)return;text.resizeTextForBestFit=true;text.resizeTextMinSize=min;text.resizeTextMaxSize=max;}
    }
}
