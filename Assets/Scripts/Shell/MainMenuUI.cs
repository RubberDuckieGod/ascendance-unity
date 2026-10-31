using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ascendance.Online;

namespace Ascendance
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject mainPanel;
        private GameObject heroPanel;
        private SettingsPanelUI settingsPanel;
        private Text heroDetail;
        private Text partySummary;
        private readonly List<string> chosenParty = new();
        private readonly Dictionary<string,Button> rosterButtons = new();
        private string chosenHero = "Garruk";
        private Button continueButton;
        private Button beginButton;
        private Text continueSummary;
        private ShellConfirmationDialog0843 confirmation0843;
        private AscendanceOnlineLobby0102 onlineLobby;
        private readonly List<UnityEngine.UI.Button> onlineHeroButtons = new();
        private readonly List<UnityEngine.UI.Text> onlineHeroNames = new();
        private readonly List<UnityEngine.UI.Text> onlineHeroStatuses = new();
        private UnityEngine.UI.Button onlineRetryButton;
        private UnityEngine.UI.Text onlineRoomSummary;
        private UnityEngine.UI.Text onlineRosterTitle;
        private UnityEngine.UI.Button onlineReadyButton;
        private UnityEngine.UI.Button onlineStartButton;
        private UnityEngine.UI.Button onlineCreateButton;
        private GameObject onlineCampaignPanel;
        private readonly List<UnityEngine.UI.Button> onlineCampaignButtons = new();
        private UnityEngine.UI.Text onlineCampaignDetail;
        private UnityEngine.UI.Text onlineCampaignStatus;
        private UnityEngine.UI.Text onlineCampaignPageText;
        private UnityEngine.UI.Button onlineLoadButton;
        private UnityEngine.UI.Button onlinePreviousPageButton;
        private UnityEngine.UI.Button onlineNextPageButton;
        private string selectedOnlineCampaignId;
        private int onlineCampaignPage;
        private bool onlineLoadPending;
        private bool IsOnline => AscendanceRemoteClientBootstrap0102.RemoteRequested;
        public AscendanceOnlineLobby0102 OnlineLobby => onlineLobby;
        public bool OnlineLobbyVisible => IsOnline && heroPanel != null && heroPanel.activeSelf;
        public bool OnlineSavedCampaignsVisible => IsOnline && onlineCampaignPanel != null && onlineCampaignPanel.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneListener(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;}
        private static void OnSceneLoaded(Scene scene,LoadSceneMode mode){if(scene.name=="MainMenu")BootMenu();}
        private static void BootMenu(){AscendanceSettings.ApplySaved();ShellCameraBootstrap.EnsureCamera(true);ShellUIFactory.EnsureEventSystem();if(FindAnyObjectByType<MainMenuUI>()!=null)return;new GameObject("ASCENDANCE_MAIN_MENU").AddComponent<MainMenuUI>().Build();}

        private void Build()
        {
            HeroDatabase.EnsureLoaded();ShellCameraBootstrap.EnsureCamera(true);canvas=ShellUIFactory.CreateCanvas("MainMenuCanvas",100);
            var backdrop=ShellUIFactory.PanelRect(canvas.transform,"Backdrop",Vector2.zero,Vector2.one,new Color(0.012f,0.018f,0.028f,1f));
            ShellUIFactory.TextRect(backdrop.transform,"ASCENDANCE",76,TextAnchor.MiddleCenter,new Vector2(0.18f,0.78f),new Vector2(0.82f,0.92f),ShellUIFactory.Gold);
            ShellUIFactory.TextRect(backdrop.transform,"A FANTASY CAMPAIGN RPG",22,TextAnchor.MiddleCenter,new Vector2(0.25f,0.735f),new Vector2(0.75f,0.79f),new Color(0.72f,0.70f,0.64f));
            ShellUIFactory.TextRect(backdrop.transform,"UNITY "+AscendanceGameManager.BuildVersion,16,TextAnchor.LowerRight,new Vector2(0.75f,0.02f),new Vector2(0.97f,0.08f),new Color(0.45f,0.48f,0.52f));
            BuildMain(backdrop.transform);BuildHeroSelect(backdrop.transform);settingsPanel=SettingsPanelUI.Create(backdrop.transform,()=>mainPanel.SetActive(true));settingsPanel.SetVisible(false);heroPanel.SetActive(false);
            confirmation0843=ShellConfirmationDialog0843.Create(canvas.transform,"MainMenuConfirmation0843");
            if(AscendanceSession.TryConsumeNewCampaignSetup0844())OpenHeroSelect();
        }

        private void BuildMain(Transform parent)
        {
            mainPanel=ShellUIFactory.PanelRect(parent,"MainButtons",new Vector2(0.36f,0.20f),new Vector2(0.64f,0.68f),new Color(0.018f,0.025f,0.034f,0.96f));
            if (IsOnline)
            {
                var frame=mainPanel.GetComponent<RectTransform>();
                frame.anchorMin=new Vector2(0.33f,0.13f);frame.anchorMax=new Vector2(0.67f,0.70f);
                ShellUIFactory.TextRect(mainPanel.transform,"ONLINE CAMPAIGNS\nJoin your party or choose a fresh party from all 16 Heroes.",18,TextAnchor.MiddleCenter,new Vector2(0.07f,0.82f),new Vector2(0.93f,0.98f));
                var join=ShellUIFactory.ButtonRect(mainPanel.transform,"JOIN ONLINE CAMPAIGN",new Vector2(0.10f,0.67f),new Vector2(0.90f,0.80f),OpenOnlineLobby0102);
                var fresh=ShellUIFactory.ButtonRect(mainPanel.transform,"NEW ONLINE CAMPAIGN",new Vector2(0.10f,0.52f),new Vector2(0.90f,0.65f),RequestNewOnlineCampaign0102);
                var saves=ShellUIFactory.ButtonRect(mainPanel.transform,"SAVED CAMPAIGNS",new Vector2(0.10f,0.37f),new Vector2(0.90f,0.50f),OpenOnlineCampaigns0102);
                foreach(var button in new[]{join,fresh,saves})
                {
                    var label=button.GetComponentInChildren<UnityEngine.UI.Text>();
                    label.resizeTextForBestFit=true;label.resizeTextMinSize=14;label.resizeTextMaxSize=22;
                }
                ShellUIFactory.ButtonRect(mainPanel.transform,"SETTINGS",new Vector2(0.10f,0.22f),new Vector2(0.90f,0.35f),OpenSettings);
                ShellUIFactory.ButtonRect(mainPanel.transform,"QUIT",new Vector2(0.10f,0.07f),new Vector2(0.90f,0.20f),RequestQuit0843);
                return;
            }
            continueButton=ShellUIFactory.ButtonRect(mainPanel.transform,"CONTINUE CAMPAIGN",new Vector2(0.10f,0.66f),new Vector2(0.90f,0.80f),ContinueGame);continueButton.interactable=AscendanceSaveSystem.HasValidSave;
            if(AscendanceSaveSystem.TryLoad(out var save0843,out _)&&save0843!=null)continueButton.GetComponentInChildren<Text>().text="CONTINUE • "+CampaignResumeRuntime0842.DescribeSurface0842(CampaignResumeRuntime0842.NormalizeSurface0842(save0843)).ToUpperInvariant();
            var continueLabel0843=continueButton.GetComponentInChildren<Text>();continueLabel0843.resizeTextForBestFit=true;continueLabel0843.resizeTextMinSize=10;continueLabel0843.resizeTextMaxSize=20;
            ShellUIFactory.ButtonRect(mainPanel.transform,"NEW GAME",new Vector2(0.10f,0.48f),new Vector2(0.90f,0.62f),RequestNewGame0843);
            ShellUIFactory.ButtonRect(mainPanel.transform,"SETTINGS",new Vector2(0.10f,0.30f),new Vector2(0.90f,0.44f),OpenSettings);
            ShellUIFactory.ButtonRect(mainPanel.transform,"QUIT",new Vector2(0.10f,0.12f),new Vector2(0.90f,0.26f),RequestQuit0843);
            continueSummary=ShellUIFactory.TextRect(mainPanel.transform,AscendanceSaveSystem.MainMenuSummary0842,13,TextAnchor.MiddleCenter,new Vector2(0.07f,0.82f),new Vector2(0.93f,0.98f),AscendanceSaveSystem.HasValidSave?new Color(0.58f,0.72f,0.56f):new Color(0.72f,0.48f,0.42f));
            continueSummary.resizeTextForBestFit=true;continueSummary.resizeTextMinSize=9;continueSummary.resizeTextMaxSize=13;
        }

        private void BuildHeroSelect(Transform parent)
        {
            heroPanel=ShellUIFactory.PanelRect(parent,"HeroSelection",new Vector2(0.06f,0.08f),new Vector2(0.94f,0.78f),new Color(0.018f,0.025f,0.034f,0.985f));
            if (IsOnline) { BuildOnlineHeroSelect0102(); return; }
            ShellUIFactory.TextRect(heroPanel.transform,"CHOOSE YOUR PARTY — UP TO 4 HEROES",32,TextAnchor.MiddleCenter,new Vector2(0.03f,0.90f),new Vector2(0.97f,0.98f),ShellUIFactory.Gold);
            var heroes=HeroDatabase.Heroes.OrderBy(h=>h.heroCardId).ToList();
            for(int i=0;i<heroes.Count;i++)
            {
                var def=heroes[i];string id=def.id;int col=i%4,row=i/4;float x0=0.035f+col*0.145f,x1=x0+0.135f;float y1=0.84f-row*0.155f,y0=y1-0.115f;
                var b=ShellUIFactory.ButtonRect(heroPanel.transform,def.displayName.ToUpperInvariant(),new Vector2(x0,y0),new Vector2(x1,y1),()=>ToggleHero(id));
                b.GetComponentInChildren<Text>().fontSize=12;rosterButtons[id]=b;
            }
            heroDetail=ShellUIFactory.TextRect(heroPanel.transform,"",18,TextAnchor.UpperLeft,new Vector2(0.63f,0.35f),new Vector2(0.965f,0.84f));
            partySummary=ShellUIFactory.TextRect(heroPanel.transform,"",17,TextAnchor.MiddleLeft,new Vector2(0.63f,0.25f),new Vector2(0.965f,0.34f),ShellUIFactory.Gold);
            beginButton=ShellUIFactory.ButtonRect(heroPanel.transform,"BEGIN ADVENTURE",new Vector2(0.63f,0.12f),new Vector2(0.965f,0.23f),BeginAdventure);
            ShellUIFactory.ButtonRect(heroPanel.transform,"REMOVE SELECTED HERO",new Vector2(0.31f,0.05f),new Vector2(0.56f,0.13f),RemoveControlledHero);
            ShellUIFactory.ButtonRect(heroPanel.transform,"BACK",new Vector2(0.035f,0.05f),new Vector2(0.25f,0.13f),CloseHeroSelect);
            chosenParty.Clear();chosenParty.AddRange(HeroDatabase.DefaultParty);chosenHero="Garruk";RefreshRosterButtons();ShowHero("Garruk");
        }

        private void ToggleHero(string id)
        {
            ShowHero(id);if(chosenParty.Contains(id))chosenHero=id;else{if(chosenParty.Count>=4)return;chosenParty.Add(id);chosenHero=id;}RefreshRosterButtons();
        }

        private void RemoveControlledHero(){if(chosenParty.Count<=1)return;chosenParty.Remove(chosenHero);chosenHero=chosenParty[0];ShowHero(chosenHero);RefreshRosterButtons();}
        private void ShowHero(string id)
        {
            var def=HeroDatabase.GetHero(id);if(def==null)return;var abilities=def.abilities==null?"":string.Join("\n",def.abilities.Select(a=>$"• {a.name}: {a.text}"));
            heroDetail.text=$"{def.displayName.ToUpperInvariant()}\n{def.className} — {def.primaryRole} / {def.secondaryRole}\n\nHP {def.maxHealth}  •  Attack {def.attack}  •  Armor {def.armor}\n\n{abilities}\n\nStarting Equipment: {def.startingEquipmentText}\n30-card Hero Deck • 5-card opening hand";
        }
        private void RefreshRosterButtons(){foreach(var kv in rosterButtons)kv.Value.GetComponent<Image>().color=chosenParty.Contains(kv.Key)?new Color(0.26f,0.19f,0.06f,1f):new Color(0.055f,0.065f,0.075f,0.97f);partySummary.text=$"PARTY {chosenParty.Count}/4: {string.Join(" • ",chosenParty)}\nInitial view: {chosenHero}";beginButton.interactable=chosenParty.Count>0;}

        private void RequestNewGame0843()
        {
            if (IsOnline) { OpenOnlineLobby0102(); return; }
            if(!AscendanceSaveSystem.HasValidSave){OpenHeroSelect();return;}
            confirmation0843.Show("START A NEW CAMPAIGN?","A valid campaign save already exists. Starting a new campaign will not delete it immediately, but the next manual save or safe autosave from the new campaign will replace that save.","START NEW GAME",()=>{confirmation0843.Hide();OpenHeroSelect();},"",null,"Your current save remains available until the new campaign is saved.",true,"CANCEL");
        }

        private void RequestQuit0843()
        {
            confirmation0843.Show("QUIT ASCENDANCE?","Exit the game and return to your desktop?","QUIT",()=>{confirmation0843.Hide();Application.Quit();},"",null,"",true,"CANCEL");
        }

        private void OpenHeroSelect(){if(IsOnline){OpenOnlineLobby0102();return;}mainPanel.SetActive(false);heroPanel.SetActive(true);}private void CloseHeroSelect(){onlineLobby?.Close();onlineLoadPending=false;heroPanel.SetActive(false);if(onlineCampaignPanel!=null)onlineCampaignPanel.SetActive(false);mainPanel.SetActive(true);}
        private void BeginAdventure(){if(IsOnline){_ = onlineLobby?.ClaimSelectedAsync();return;}Time.timeScale=1f;AscendanceSession.StartNewGame(chosenParty,chosenHero);}
        private void ContinueGame()
        {
            if (IsOnline) { OpenOnlineLobby0102(); return; }
            Time.timeScale=1f;if(AscendanceSession.ContinueGame())return;continueButton.interactable=false;
            if(continueSummary!=null){continueSummary.text=string.IsNullOrWhiteSpace(AscendanceSession.LastContinueError0842)?"SAVE FILE UNREADABLE — CONTINUE DISABLED":AscendanceSession.LastContinueError0842.ToUpperInvariant();continueSummary.color=new Color(0.90f,0.42f,0.34f);}
        }
        private void OpenSettings(){mainPanel.SetActive(false);settingsPanel.SetVisible(true);}

        private void BuildOnlineHeroSelect0102()
        {
            onlineRosterTitle=ShellUIFactory.TextRect(heroPanel.transform,"CHOOSE YOUR HERO",32,TextAnchor.MiddleCenter,new Vector2(0.03f,0.90f),new Vector2(0.97f,0.98f),ShellUIFactory.Gold);
            for(int i=0;i<16;i++)
            {
                int slot=i;
                var button=ShellUIFactory.ButtonRect(heroPanel.transform,"",Vector2.zero,Vector2.one,()=>SelectOnlineSlot0102(slot));
                button.name="OnlineHeroSlot_"+i;
                var title=button.GetComponentInChildren<UnityEngine.UI.Text>();
                title.rectTransform.anchorMin=new Vector2(0.03f,0.48f);title.rectTransform.anchorMax=new Vector2(0.97f,0.96f);
                title.alignment=TextAnchor.MiddleLeft;title.resizeTextForBestFit=true;title.resizeTextMinSize=13;title.resizeTextMaxSize=24;title.raycastTarget=false;title.supportRichText=false;
                var detail=ShellUIFactory.TextRect(button.transform,"",16,TextAnchor.MiddleLeft,new Vector2(0.03f,0.04f),new Vector2(0.97f,0.48f));
                detail.resizeTextForBestFit=true;detail.resizeTextMinSize=10;detail.resizeTextMaxSize=16;detail.raycastTarget=false;detail.supportRichText=false;
                onlineHeroButtons.Add(button);onlineHeroNames.Add(title);onlineHeroStatuses.Add(detail);
            }
            heroDetail=ShellUIFactory.TextRect(heroPanel.transform,"",20,TextAnchor.UpperLeft,new Vector2(0.63f,0.47f),new Vector2(0.965f,0.84f));
            heroDetail.raycastTarget=false;heroDetail.supportRichText=false;
            partySummary=ShellUIFactory.TextRect(heroPanel.transform,"",17,TextAnchor.UpperLeft,new Vector2(0.63f,0.265f),new Vector2(0.965f,0.44f),ShellUIFactory.Gold);
            partySummary.resizeTextForBestFit=true;partySummary.resizeTextMinSize=12;partySummary.resizeTextMaxSize=17;partySummary.raycastTarget=false;partySummary.supportRichText=false;
            onlineRoomSummary=ShellUIFactory.TextRect(heroPanel.transform,"",14,TextAnchor.MiddleLeft,new Vector2(0.035f,0.14f),new Vector2(0.585f,0.20f));
            onlineRoomSummary.resizeTextForBestFit=true;onlineRoomSummary.resizeTextMinSize=10;onlineRoomSummary.resizeTextMaxSize=14;onlineRoomSummary.raycastTarget=false;onlineRoomSummary.supportRichText=false;
            beginButton=ShellUIFactory.ButtonRect(heroPanel.transform,"CHOOSE A HERO",new Vector2(0.63f,0.12f),new Vector2(0.965f,0.23f),BeginAdventure);
            beginButton.name="OnlineClaimHeroButton";
            onlineReadyButton=ShellUIFactory.ButtonRect(heroPanel.transform,"MARK READY",new Vector2(0.74f,0.13f),new Vector2(0.975f,0.205f),()=>{_ = onlineLobby.SetReadyAsync(!onlineLobby.IsReady);});
            onlineReadyButton.name="OnlineReadyButton";
            onlineStartButton=ShellUIFactory.ButtonRect(heroPanel.transform,"START CAMPAIGN",new Vector2(0.74f,0.04f),new Vector2(0.975f,0.115f),()=>{_ = onlineLobby.StartCampaignAsync();});
            onlineStartButton.name="OnlineStartCampaignButton";
            onlineCreateButton=ShellUIFactory.ButtonRect(heroPanel.transform,"NEW ONLINE CAMPAIGN",new Vector2(0.63f,0.245f),new Vector2(0.975f,0.32f),RequestNewOnlineCampaign0102);
            onlineCreateButton.name="OnlineCreateCampaignButton";
            foreach(var button in new[]{beginButton,onlineReadyButton,onlineStartButton,onlineCreateButton})
            {
                var label=button.GetComponentInChildren<UnityEngine.UI.Text>();
                label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=20;
            }
            onlineRetryButton=ShellUIFactory.ButtonRect(heroPanel.transform,"RETRY CONNECTION",new Vector2(0.19f,0.045f),new Vector2(0.40f,0.12f),OpenOnlineLobby0102);
            onlineRetryButton.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=16;
            var saves=ShellUIFactory.ButtonRect(heroPanel.transform,"SAVED CAMPAIGNS",new Vector2(0.42f,0.045f),new Vector2(0.70f,0.12f),OpenOnlineCampaigns0102);
            saves.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=16;
            ShellUIFactory.ButtonRect(heroPanel.transform,"BACK",new Vector2(0.035f,0.045f),new Vector2(0.17f,0.12f),CloseHeroSelect);
            onlineLobby=new AscendanceOnlineLobby0102();
            onlineLobby.Changed+=RefreshOnlineLobby0102;
            onlineLobby.Claimed+=EnterOnlineCampaign0102;
            BuildOnlineCampaignList0102(heroPanel.transform.parent);
            RefreshOnlineLobby0102();
        }

        public void OpenOnlineLobby0102()
        {
            if(!IsOnline||onlineLobby==null)return;
            mainPanel.SetActive(false);heroPanel.SetActive(true);onlineCampaignPanel.SetActive(false);
            onlineLoadPending=false;
            onlineLobby.Open();
        }

        public void RequestNewOnlineCampaign0102()
        {
            confirmation0843.Show("CREATE A FRESH ONLINE CAMPAIGN?",
                "Create a new party lobby with all 16 Heroes. Your current campaign is kept in Saved Campaigns. Each player claims one Hero and marks READY before the creator starts.",
                "CREATE LOBBY",ConfirmNewOnlineCampaign0102,"",null,"All players must return to the main menu before switching campaigns.",true,"CANCEL");
        }

        public void ConfirmNewOnlineCampaign0102()
        {
            confirmation0843.Hide();
            OpenOnlineLobby0102();
            onlineLobby.CreateAfterConnect();
        }

        public void OpenOnlineCampaigns0102()
        {
            if(!IsOnline||onlineLobby==null)return;
            mainPanel.SetActive(false);heroPanel.SetActive(false);onlineCampaignPanel.SetActive(true);
            selectedOnlineCampaignId=null;onlineCampaignPage=0;onlineLoadPending=false;
            onlineLobby.Open();
        }

        private void SelectOnlineSlot0102(int slot)
        {
            if(onlineLobby==null||slot<0||slot>=onlineLobby.HeroChoices.Count)return;
            onlineLobby.SelectHero(onlineLobby.HeroChoices[slot].HeroId);
        }

        private static void OnlineAnchors0102(Component element,Vector2 min,Vector2 max)
        {
            var rect=element.GetComponent<RectTransform>();
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        }

        private void RefreshOnlineLobby0102()
        {
            if(onlineLobby==null)return;
            bool draft=onlineLobby.IsDraft;
            onlineRosterTitle.text=draft?"NEW CAMPAIGN — CHOOSE FROM ALL 16 HEROES":"CHOOSE YOUR HERO";
            for(int i=0;i<onlineHeroButtons.Count;i++)
            {
                bool present=i<onlineLobby.HeroChoices.Count;
                onlineHeroButtons[i].gameObject.SetActive(present);
                if(!present)continue;
                var choice=onlineLobby.HeroChoices[i];
                if(draft)
                {
                    int column=i%4,row=i/4;float left=0.03f+column*0.1725f,top=0.84f-row*0.15f;
                    OnlineAnchors0102(onlineHeroButtons[i],new Vector2(left,top-0.132f),new Vector2(left+0.16f,top));
                }
                else
                {
                    float top=0.84f-i*0.165f;
                    OnlineAnchors0102(onlineHeroButtons[i],new Vector2(0.035f,top-0.145f),new Vector2(0.585f,top));
                }
                onlineHeroButtons[i].name="OnlineHero_"+choice.HeroId;
                onlineHeroStatuses[i].name="OnlineHeroStatus_"+choice.HeroId;
                onlineHeroNames[i].resizeTextMaxSize=draft?20:24;
                onlineHeroStatuses[i].resizeTextMaxSize=draft?14:16;
                onlineHeroNames[i].text=choice.DisplayName.ToUpperInvariant();
                onlineHeroStatuses[i].text=choice.Status;
                onlineHeroButtons[i].interactable=choice.CanChoose;
                bool selected=string.Equals(choice.HeroId,onlineLobby.SelectedHeroId,System.StringComparison.OrdinalIgnoreCase);
                onlineHeroButtons[i].GetComponent<UnityEngine.UI.Image>().color=selected?new Color(0.26f,0.19f,0.06f,1f):ShellUIFactory.Button;
            }
            OnlineAnchors0102(heroDetail,new Vector2(draft?0.74f:0.63f,draft?0.50f:0.47f),new Vector2(0.975f,0.84f));
            OnlineAnchors0102(partySummary,new Vector2(draft?0.74f:0.63f,draft?0.315f:0.335f),new Vector2(0.975f,draft?0.49f:0.44f));
            OnlineAnchors0102(beginButton,new Vector2(draft?0.74f:0.63f,draft?0.22f:0.12f),new Vector2(0.975f,draft?0.295f:0.23f));
            OnlineAnchors0102(onlineRoomSummary,new Vector2(0.035f,0.14f),new Vector2(draft?0.70f:0.585f,draft?0.235f:0.20f));
            var selectedChoice=onlineLobby.HeroChoices.FirstOrDefault(h=>string.Equals(h.HeroId,onlineLobby.SelectedHeroId,System.StringComparison.OrdinalIgnoreCase));
            if(selectedChoice==null)
                heroDetail.text=draft?"ALL 16 HEROES\n\nEach player chooses and claims one Hero. Up to four players can join the party.\n\nMark READY when your choice is final. The creator then starts at Level 1.":
                    "ONE HERO PER PLAYER\n\nChoose an available Hero from your campaign.\n\nYour friends use the same invitation and choose their own Heroes.";
            else
            {
                var definition=HeroDatabase.GetHero(selectedChoice.HeroId);
                var saved=onlineLobby.Snapshot?.Campaign?.Heroes?.FirstOrDefault(h=>h.Id==selectedChoice.HeroId);
                heroDetail.text=selectedChoice.DisplayName.ToUpperInvariant()+"\n"+(definition?.className??"")+"\n\n"+
                    (draft?$"New Hero · Level 1\nHealth {definition?.maxHealth} · Attack {definition?.attack} · Armor {definition?.armor}\n\n":saved==null?"":$"Level {saved.Level} · Health {saved.Health}\n\n")+
                    (draft?"Claim this Hero, then mark READY. Other players choose their own Heroes.":"Your saved campaign progress stays on the server. This connection controls only this Hero.");
            }
            partySummary.text=onlineLobby.Status;
            beginButton.interactable=selectedChoice?.CanChoose==true&&!onlineLobby.IsBusy;
            beginButton.GetComponentInChildren<UnityEngine.UI.Text>().text=onlineLobby.IsBusy?"PLEASE WAIT…":selectedChoice==null?"CHOOSE A HERO":draft?"CLAIM HERO":"JOIN CAMPAIGN";
            onlineReadyButton.gameObject.SetActive(draft);onlineStartButton.gameObject.SetActive(draft);
            onlineCreateButton.gameObject.SetActive(!draft&&onlineLobby.SupportsCampaignSetup);
            onlineCreateButton.interactable=onlineLobby.CanCreateCampaign;
            onlineReadyButton.interactable=draft&&onlineLobby.IsClaimed&&!onlineLobby.IsBusy;
            onlineReadyButton.GetComponentInChildren<UnityEngine.UI.Text>().text=onlineLobby.IsReady?"NOT READY":"MARK READY";
            onlineStartButton.interactable=onlineLobby.CanStartCampaign;
            onlineRetryButton.interactable=!onlineLobby.IsBusy;
            int connected=onlineLobby.Snapshot?.Players?.Count(p=>p!=null&&p.Connected)??0;
            int ready=onlineLobby.Snapshot?.Players?.Count(p=>p!=null&&p.Connected&&p.Ready)??0;
            onlineRoomSummary.text=onlineLobby.Snapshot==null?"Waiting for the server's party…":draft?
                $"{onlineLobby.HeroChoices.Count} Heroes available · {connected}/4 players · {ready} ready\n{onlineLobby.CampaignLobby.CampaignName}\nBACK releases your lobby selection.":
                $"{onlineLobby.HeroChoices.Count} campaign Heroes · {connected}/4 players connected";
            RefreshOnlineCampaignList0102();
            if(onlineLoadPending&&!onlineLobby.IsBusy)
            {
                onlineLoadPending=false;
                if(onlineLobby.LastCampaignResult?.Accepted==true)
                {
                    onlineCampaignPanel.SetActive(false);heroPanel.SetActive(true);
                }
            }
        }

        private void BuildOnlineCampaignList0102(Transform parent)
        {
            onlineCampaignPanel=ShellUIFactory.PanelRect(parent,"OnlineCampaignSelection",new Vector2(0.06f,0.08f),new Vector2(0.94f,0.78f),ShellUIFactory.Panel);
            ShellUIFactory.TextRect(onlineCampaignPanel.transform,"SAVED ONLINE CAMPAIGNS",32,TextAnchor.MiddleCenter,new Vector2(0.03f,0.90f),new Vector2(0.97f,0.98f),ShellUIFactory.Gold);
            for(int i=0;i<8;i++)
            {
                int slot=i,column=i%2,row=i/2;float left=0.035f+column*0.34f,top=0.84f-row*0.165f;
                var button=ShellUIFactory.ButtonRect(onlineCampaignPanel.transform,"",new Vector2(left,top-0.145f),new Vector2(left+0.325f,top),()=>SelectOnlineCampaignSlot0102(slot));
                button.name="OnlineCampaignSlot_"+i;
                var label=button.GetComponentInChildren<UnityEngine.UI.Text>();label.fontSize=20;label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=20;label.supportRichText=false;
                onlineCampaignButtons.Add(button);
            }
            onlineCampaignDetail=ShellUIFactory.TextRect(onlineCampaignPanel.transform,"Choose a saved campaign.",20,TextAnchor.UpperLeft,new Vector2(0.74f,0.40f),new Vector2(0.975f,0.84f));
            onlineCampaignDetail.raycastTarget=false;onlineCampaignDetail.supportRichText=false;
            onlineCampaignStatus=ShellUIFactory.TextRect(onlineCampaignPanel.transform,"",16,TextAnchor.UpperLeft,new Vector2(0.74f,0.23f),new Vector2(0.975f,0.39f),ShellUIFactory.Gold);
            onlineCampaignStatus.raycastTarget=false;onlineCampaignStatus.supportRichText=false;
            onlineLoadButton=ShellUIFactory.ButtonRect(onlineCampaignPanel.transform,"LOAD CAMPAIGN",new Vector2(0.74f,0.10f),new Vector2(0.975f,0.20f),RequestLoadOnlineCampaign0102);
            onlineLoadButton.name="OnlineLoadCampaignButton";onlineLoadButton.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=18;
            ShellUIFactory.ButtonRect(onlineCampaignPanel.transform,"BACK",new Vector2(0.035f,0.045f),new Vector2(0.17f,0.12f),CloseHeroSelect);
            onlinePreviousPageButton=ShellUIFactory.ButtonRect(onlineCampaignPanel.transform,"PREVIOUS",new Vector2(0.22f,0.045f),new Vector2(0.36f,0.12f),()=>ChangeOnlineCampaignPage0102(-1));
            onlineNextPageButton=ShellUIFactory.ButtonRect(onlineCampaignPanel.transform,"NEXT",new Vector2(0.54f,0.045f),new Vector2(0.68f,0.12f),()=>ChangeOnlineCampaignPage0102(1));
            foreach(var button in new[]{onlinePreviousPageButton,onlineNextPageButton})button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=16;
            onlineCampaignPageText=ShellUIFactory.TextRect(onlineCampaignPanel.transform,"",16,TextAnchor.MiddleCenter,new Vector2(0.37f,0.045f),new Vector2(0.53f,0.12f));
            onlineCampaignPageText.raycastTarget=false;
            onlineCampaignPanel.SetActive(false);
        }

        private void SelectOnlineCampaignSlot0102(int slot)
        {
            var slots=onlineLobby?.CampaignLobby?.Slots;
            int index=onlineCampaignPage*8+slot;
            if(slots==null||index<0||index>=slots.Count||onlineLobby.IsBusy)return;
            selectedOnlineCampaignId=slots[index].Id;
            RefreshOnlineCampaignList0102();
        }

        private void ChangeOnlineCampaignPage0102(int delta)
        {
            int count=onlineLobby?.CampaignLobby?.Slots?.Count??0;
            onlineCampaignPage=Mathf.Clamp(onlineCampaignPage+delta,0,Mathf.Max(0,(count-1)/8));
            RefreshOnlineCampaignList0102();
        }

        private void RefreshOnlineCampaignList0102()
        {
            if(onlineCampaignPanel==null)return;
            var slots=onlineLobby?.CampaignLobby?.Slots;
            int count=slots?.Count??0,pages=Mathf.Max(1,(count+7)/8);
            onlineCampaignPage=Mathf.Clamp(onlineCampaignPage,0,pages-1);
            for(int i=0;i<onlineCampaignButtons.Count;i++)
            {
                int index=onlineCampaignPage*8+i;bool present=index<count;
                var button=onlineCampaignButtons[i];button.gameObject.SetActive(present);
                if(!present)continue;
                var slot=slots[index];button.name="OnlineCampaign_"+slot.Id;
                button.GetComponentInChildren<UnityEngine.UI.Text>().text=(slot.IsActive?"CURRENT — ":"")+slot.Name+"\n"+(slot.IsDraft?"Party selection":string.Join(" · ",slot.PartyHeroIds??new List<string>()));
                button.interactable=!onlineLobby.IsBusy;
                button.GetComponent<UnityEngine.UI.Image>().color=slot.Id==selectedOnlineCampaignId?new Color(0.26f,0.19f,0.06f,1f):ShellUIFactory.Button;
            }
            var selected=slots?.FirstOrDefault(s=>s!=null&&s.Id==selectedOnlineCampaignId);
            onlineCampaignDetail.text=selected==null?"SAVED CAMPAIGNS\n\nYour previous campaign remains available here when a new online campaign is created.":
                selected.Name+"\n\n"+(selected.IsDraft?"This campaign is choosing its party.":"Party: "+string.Join(" · ",selected.PartyHeroIds??new List<string>()))+"\n\nThe current campaign is saved separately before switching.";
            onlineCampaignStatus.text=onlineLobby.Status;
            if(!onlineLobby.IsBusy&&onlineLobby.IsConnected&&!onlineLobby.SupportsCampaignSetup)
                onlineCampaignStatus.text="This server needs the campaign-setup update to list or create campaigns.";
            onlineLoadButton.interactable=selected!=null&&!onlineLobby.IsBusy&&(selected.IsActive||onlineLobby.CanCreateCampaign);
            onlineLoadButton.GetComponentInChildren<UnityEngine.UI.Text>().text=selected?.IsActive==true?"JOIN CURRENT":"LOAD CAMPAIGN";
            onlinePreviousPageButton.interactable=!onlineLobby.IsBusy&&onlineCampaignPage>0;
            onlineNextPageButton.interactable=!onlineLobby.IsBusy&&onlineCampaignPage+1<pages;
            onlineCampaignPageText.text=$"{onlineCampaignPage+1} / {pages}";
        }

        public void RequestLoadOnlineCampaign0102()
        {
            var selected=onlineLobby.CampaignLobby?.Slots?.FirstOrDefault(s=>s!=null&&s.Id==selectedOnlineCampaignId);
            if(selected==null)return;
            if(selected.IsActive){onlineCampaignPanel.SetActive(false);heroPanel.SetActive(true);return;}
            string id=selected.Id;
            confirmation0843.Show("LOAD SAVED ONLINE CAMPAIGN?",$"Load {selected.Name}? The current campaign is saved separately before switching.",
                "LOAD CAMPAIGN",()=>{confirmation0843.Hide();onlineLoadPending=true;_ = onlineLobby.LoadCampaignAsync(id);},"",null,
                "All players must return to the main menu before switching campaigns.",true,"CANCEL");
        }

        private void EnterOnlineCampaign0102()
        {
            string hero=onlineLobby.SelectedHeroId;
            var party=onlineLobby.Snapshot?.Campaign?.PartyHeroIds?.ToList();
            if (party==null || !onlineLobby.TryHandOff()) return;
            Time.timeScale=1f;
            AscendanceSession.StartNewGame(party,hero);
        }

        private void Update() { if (OnlineLobbyVisible||OnlineSavedCampaignsVisible) onlineLobby?.Tick(); }
        private void OnDestroy() { onlineLobby?.Dispose(); }
    }
}
