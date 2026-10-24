using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendance
{
    /// <summary>
    /// Campaign II hub. 0.8.49 is a studio-style code-first UGUI composition:
    /// one region background painting, then every functional/decorative UI element
    /// (frames, panels, quest rows, buttons, chips, dividers and ornaments) is built
    /// as live Unity objects. Campaign authority stays in the campaign/save/quest runtimes.
    /// </summary>
    public sealed class CampaignLaunchpadUI0845 : MonoBehaviour
    {
        private sealed class QuestRowVisual0849
        {
            public string QuestId;
            public Button Button;
            public Image Fill;
            public Image Border;
            public Text IdText;
            public Text TitleText;
            public Text RoleText;
            public Text StatusText;
        }

        public static CampaignLaunchpadUI0845 Instance { get; private set; }
        public bool IsOpen => root!=null&&root.activeSelf;

        // Compatibility/debug contract from earlier Campaign II milestones.
        public string DebugHeading0845=>heading?.text??string.Empty;
        public string DebugOverview0845=>overview?.text??string.Empty;
        public string DebugPlan0845=>plan?.text??string.Empty;
        public string DebugCarryover0845=>carryover?.text??string.Empty;
        public bool DebugQ013Disabled0845=>continueQuestButton!=null&&!continueQuestButton.interactable;

        // 0.8.47 compatibility.
        public string DebugTheme0847=>"INTERACTIVE_HIGH_FANTASY";
        public bool DebugHeaderArtLoaded0847=>backgroundArt!=null&&backgroundArt.texture!=null;
        public int DebugWorldEventPanelCount0847=>worldEventPanels.Count;
        public int DebugQuestButtonCount0847=>questRows.Count;
        public string DebugSelectedQuestId0847=>selectedQuestId??string.Empty;
        public string DebugSelectedQuestRole0847=>selectedQuestRole?.text??string.Empty;
        public string DebugSelectedWorldEvent0847=>selectedQuestWorldEvent?.text??string.Empty;
        public bool DebugContinueInteractable0847=>continueQuestButton!=null&&continueQuestButton.interactable;
        public bool DebugEveryQuestInspectable0847=>questRows.Count==12&&questRows.Values.All(x=>x?.Button!=null&&x.Button.interactable);
        public bool DebugSequentialBlocks0847=>ValidateSequentialBlocks();

        // 0.8.48 compatibility now reports live code-built controls instead of sprite skins.
        public string DebugArtKit0848=>"RETIRED_BY_CODE_FIRST_0849";
        public bool DebugBackgroundArtLoaded0848=>backgroundArt!=null&&backgroundArt.texture!=null;
        public bool DebugSelectedQuestArtLoaded0848=>false;
        public bool DebugQuestRowSkinsLoaded0848=>questRows.Count==12&&questRows.Values.All(x=>x?.Fill!=null&&x.Border!=null);
        public bool DebugActionButtonSkinsLoaded0848=>saveButton!=null&&continueQuestButton!=null&&returnButton!=null;

        // 0.8.49 code-first validation surface.
        public string DebugArchitecture0849=>"CODE_FIRST_UGUI";
        public bool DebugUsesOnlyBackgroundArt0849=>backgroundArt!=null&&backgroundArt.texture!=null&&FunctionalSpriteCount0849()==0;
        public int DebugFunctionalSpriteCount0849=>FunctionalSpriteCount0849();
        public Vector2 DebugReferenceResolution0849=>canvasScaler!=null?canvasScaler.referenceResolution:Vector2.zero;
        public float DebugCanvasMatch0849=>canvasScaler!=null?canvasScaler.matchWidthOrHeight:-1f;
        public bool DebugSafeMargins0849=>safeAreaRoot!=null;
        public bool DebugReadableTypeScale0849=>selectedQuestTitle!=null&&selectedQuestTitle.fontSize>=32&&questRows.Values.All(x=>x.TitleText!=null&&x.TitleText.fontSize>=17);

        private static readonly string[] ValewatchWorldEvents={"WE005","WE006","WE007","WE008"};
        private readonly Dictionary<string,QuestRowVisual0849> questRows=new(StringComparer.OrdinalIgnoreCase);
        private readonly List<GameObject> worldEventPanels=new();
        private readonly Dictionary<string,Image> worldEventAccent=new(StringComparer.OrdinalIgnoreCase);

        private GameObject root,safeAreaRoot;
        private CanvasScaler canvasScaler;
        private RawImage backgroundArt;
        private Text heading,overview,plan,carryover,status;
        private Text selectedQuestTitle,selectedQuestRole,selectedQuestWorldEvent,selectedQuestDescription,selectedQuestAvailability;
        private Button continueQuestButton,saveButton,returnButton;
        private string selectedQuestId="Q013";

        private void Awake(){Instance=this;Build();SetVisible(false);}
        private void OnDestroy(){if(Instance==this)Instance=null;}

        public void Open()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||!gm.CampaignLaunchpadAvailable0845)return;
            QuestResultsUI0835.Instance?.Close();DowntimeUI0836.Instance?.Close();CampaignHubUI0834.Instance?.Close();CampaignEndUI0844.Instance?.Close();QuestBriefingUI0841.Instance?.CloseForResume0842();
            SetVisible(true);Refresh();SelectQuest0847(string.IsNullOrWhiteSpace(selectedQuestId)?"Q013":selectedQuestId,false);
        }

        public void Close()=>SetVisible(false);

        public void SaveCampaign()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(gm,out var m);
            SetStatus(m,ok?StudioFantasyUI0849.GoldBright:StudioFantasyUI0849.Red);
        }

        public void ReturnToMainMenu()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(gm,out var m);
            if(!ok){SetStatus(m,StudioFantasyUI0849.Red);return;}
            AscendanceSession.ReturnToMainMenu();
        }

        public void SelectQuest0847(string questId)=>SelectQuest0847(questId,true);

        public void ContinueSelectedQuest0847()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm==null||string.IsNullOrWhiteSpace(selectedQuestId))return;
            if(!gm.CanBeginCampaignQuest0833(selectedQuestId))
            {
                SetStatus($"{selectedQuestId} is selected, but it is not currently available from live campaign progression.",StudioFantasyUI0849.Muted);
                RefreshSelectedQuest();return;
            }
            Close();QuestBriefingUI0841.Instance?.Open(selectedQuestId);
        }

        private void Build()
        {
            var canvas=ShellUIFactory.CreateCanvas("CampaignLaunchpadCanvas_0849_CodeFirstStudio",580);canvas.transform.SetParent(transform,false);
            canvasScaler=canvas.GetComponent<CanvasScaler>();
            if(canvasScaler!=null){canvasScaler.referenceResolution=new Vector2(1920f,1080f);canvasScaler.matchWidthOrHeight=0.5f;}

            root=StudioFantasyUI0849.Rect(canvas.transform,"ValewatchStudioHub0849",Vector2.zero,Vector2.one,new Color(0.002f,0.006f,0.014f,1f));
            backgroundArt=StudioFantasyUI0849.Background(root.transform);backgroundArt.transform.SetAsFirstSibling();
            StudioFantasyUI0849.Rect(root.transform,"CinematicVignette",Vector2.zero,Vector2.one,new Color(0.002f,0.006f,0.014f,0.44f));
            StudioFantasyUI0849.Rect(root.transform,"LowerReadabilityWash",new Vector2(0f,0f),new Vector2(1f,0.86f),new Color(0.002f,0.006f,0.014f,0.12f));

            // Safe visual margin. The CanvasScaler handles resolution; this keeps content away from monitor/notch edges.
            safeAreaRoot=StudioFantasyUI0849.Rect(root.transform,"SafeAreaRoot0849",new Vector2(0.022f,0.026f),new Vector2(0.978f,0.974f),new Color(0f,0f,0f,0f));

            BuildTitle();BuildLeftColumn();BuildSelectedQuest();BuildProgression();BuildBottomBar();
            plan=StudioFantasyUI0849.Text(safeAreaRoot.transform,"DebugCampaignPlan0849","",1,TextAnchor.UpperLeft,Vector2.zero,new Vector2(0.001f,0.001f),new Color(1f,1f,1f,0f),FontStyle.Normal,false);
        }

        private void BuildTitle()
        {
            var bar=StudioFantasyUI0849.FramedPanel(safeAreaRoot.transform,"CampaignTitleFrame0849",new Vector2(0.17f,0.855f),new Vector2(0.83f,0.985f),true,new Color(0.013f,0.021f,0.038f,0.92f));
            StudioFantasyUI0849.Divider(bar.Content,0.48f,0.08f,0.92f,0.45f);
            heading=StudioFantasyUI0849.Text(bar.Content,"CampaignTitle0849","CAMPAIGN II",52,TextAnchor.MiddleCenter,new Vector2(0.06f,0.47f),new Vector2(0.94f,0.98f),StudioFantasyUI0849.GoldBright,FontStyle.Bold);
            StudioFantasyUI0849.Fit(heading,34,52);
            var sub=StudioFantasyUI0849.Text(bar.Content,"CampaignSubtitle0849","SHADOWS OVER VALEWATCH",29,TextAnchor.MiddleCenter,new Vector2(0.08f,0.05f),new Vector2(0.92f,0.48f),StudioFantasyUI0849.Ivory,FontStyle.Bold);
            StudioFantasyUI0849.Fit(sub,19,29);
            StudioFantasyUI0849.Diamond(bar.Content,new Vector2(0.04f,0.50f),0.014f,StudioFantasyUI0849.GoldBright);
            StudioFantasyUI0849.Diamond(bar.Content,new Vector2(0.96f,0.50f),0.014f,StudioFantasyUI0849.GoldBright);
        }

        private void BuildLeftColumn()
        {
            var overviewFrame=StudioFantasyUI0849.FramedPanel(safeAreaRoot.transform,"CampaignOverviewPanel0849",new Vector2(0.005f,0.465f),new Vector2(0.267f,0.825f),true);
            StudioFantasyUI0849.SectionHeader(overviewFrame.Content,"CAMPAIGN OVERVIEW",24);
            overview=StudioFantasyUI0849.Text(overviewFrame.Content,"CampaignOverviewText0849","",18,TextAnchor.UpperLeft,new Vector2(0.035f,0.055f),new Vector2(0.965f,0.81f),StudioFantasyUI0849.Ivory);
            StudioFantasyUI0849.Fit(overview,13,18);

            var carryFrame=StudioFantasyUI0849.FramedPanel(safeAreaRoot.transform,"ContinuationPanel0849",new Vector2(0.005f,0.155f),new Vector2(0.267f,0.445f),false);
            StudioFantasyUI0849.SectionHeader(carryFrame.Content,"PARTY & CONTINUATION",22);
            carryover=StudioFantasyUI0849.Text(carryFrame.Content,"ContinuationText0849","",17,TextAnchor.UpperLeft,new Vector2(0.035f,0.055f),new Vector2(0.965f,0.81f),StudioFantasyUI0849.Ivory);
            StudioFantasyUI0849.Fit(carryover,12,17);
        }

        private void BuildSelectedQuest()
        {
            var panel=StudioFantasyUI0849.FramedPanel(safeAreaRoot.transform,"SelectedQuestPanel0849",new Vector2(0.282f,0.155f),new Vector2(0.653f,0.825f),true,new Color(0.017f,0.026f,0.045f,0.97f));
            StudioFantasyUI0849.SectionHeader(panel.Content,"SELECTED QUEST",24);

            var titleFrame=StudioFantasyUI0849.FramedPanel(panel.Content,"QuestIdentity0849",new Vector2(0.035f,0.61f),new Vector2(0.965f,0.81f),false,new Color(0.024f,0.037f,0.061f,0.98f));
            selectedQuestTitle=StudioFantasyUI0849.Text(titleFrame.Content,"SelectedQuestTitle0849","Q013 — THE MISSING CARAVAN",35,TextAnchor.MiddleLeft,new Vector2(0.045f,0.38f),new Vector2(0.955f,0.94f),StudioFantasyUI0849.Ivory,FontStyle.Bold);
            StudioFantasyUI0849.Fit(selectedQuestTitle,23,35);
            selectedQuestRole=StudioFantasyUI0849.Text(titleFrame.Content,"SelectedQuestRole0849","MAIN QUEST",17,TextAnchor.MiddleLeft,new Vector2(0.045f,0.06f),new Vector2(0.39f,0.37f),StudioFantasyUI0849.GoldBright,FontStyle.Bold);
            selectedQuestWorldEvent=StudioFantasyUI0849.Text(titleFrame.Content,"SelectedQuestWorldEvent0849","WE005 — KNIVES IN THE NIGHT",16,TextAnchor.MiddleRight,new Vector2(0.37f,0.06f),new Vector2(0.955f,0.37f),StudioFantasyUI0849.Muted,FontStyle.Bold);
            StudioFantasyUI0849.Fit(selectedQuestWorldEvent,11,16);

            var briefHead=StudioFantasyUI0849.Text(panel.Content,"QuestBriefHeader0849","QUEST BRIEF",20,TextAnchor.MiddleLeft,new Vector2(0.045f,0.535f),new Vector2(0.955f,0.595f),StudioFantasyUI0849.GoldBright,FontStyle.Bold);
            StudioFantasyUI0849.Divider(panel.Content,0.525f,0.045f,0.955f,0.45f);
            selectedQuestDescription=StudioFantasyUI0849.Text(panel.Content,"SelectedQuestDescription0849","",20,TextAnchor.UpperLeft,new Vector2(0.045f,0.245f),new Vector2(0.955f,0.515f),StudioFantasyUI0849.Ivory);
            StudioFantasyUI0849.Fit(selectedQuestDescription,14,20);

            var availability=StudioFantasyUI0849.FramedPanel(panel.Content,"QuestAvailability0849",new Vector2(0.045f,0.055f),new Vector2(0.955f,0.195f),false,new Color(0.010f,0.017f,0.030f,0.99f));
            selectedQuestAvailability=StudioFantasyUI0849.Text(availability.Content,"SelectedQuestAvailability0849","",18,TextAnchor.MiddleCenter,new Vector2(0.03f,0.12f),new Vector2(0.97f,0.88f),StudioFantasyUI0849.Muted,FontStyle.Bold);
            StudioFantasyUI0849.Fit(selectedQuestAvailability,12,18);
        }

        private void BuildProgression()
        {
            var panel=StudioFantasyUI0849.FramedPanel(safeAreaRoot.transform,"CampaignProgressionPanel0849",new Vector2(0.668f,0.155f),new Vector2(0.995f,0.825f),true,new Color(0.012f,0.020f,0.035f,0.97f));
            StudioFantasyUI0849.SectionHeader(panel.Content,"CAMPAIGN PROGRESSION",24);
            worldEventPanels.Clear();questRows.Clear();worldEventAccent.Clear();
            float top=0.815f;
            for(int i=0;i<ValewatchWorldEvents.Length;i++)
            {
                float cardTop=top-i*0.197f,cardBottom=cardTop-0.183f;
                BuildWorldEventCard(panel.Content,ValewatchWorldEvents[i],new Vector2(0.025f,cardBottom),new Vector2(0.975f,cardTop));
            }
        }

        private void BuildWorldEventCard(Transform parent,string worldEventId,Vector2 min,Vector2 max)
        {
            var def=CampaignContent0833.WorldEvents[worldEventId];
            var card=StudioFantasyUI0849.FramedPanel(parent,"WorldEventCard_"+worldEventId,min,max,false,new Color(0.020f,0.030f,0.049f,0.985f));worldEventPanels.Add(card.Root);
            var accent=StudioFantasyUI0849.Rect(card.Content,"WorldEventAccent",new Vector2(0.002f,0.05f),new Vector2(0.010f,0.95f),StudioFantasyUI0849.GoldDim);worldEventAccent[worldEventId]=accent.GetComponent<Image>();
            string day=def.DayLimit>0?$"DAY LIMIT {def.DayLimit}":"FINAL CHAPTER";
            var title=StudioFantasyUI0849.Text(card.Content,"WorldEventTitle",$"{worldEventId} — {def.Name.ToUpperInvariant()}",18,TextAnchor.MiddleLeft,new Vector2(0.035f,0.71f),new Vector2(0.74f,0.97f),StudioFantasyUI0849.GoldBright,FontStyle.Bold);
            StudioFantasyUI0849.Fit(title,12,18);
            var dayText=StudioFantasyUI0849.Text(card.Content,"WorldEventDayLimit",day,13,TextAnchor.MiddleRight,new Vector2(0.72f,0.71f),new Vector2(0.96f,0.97f),StudioFantasyUI0849.Muted,FontStyle.Bold);
            StudioFantasyUI0849.Fit(dayText,9,13);

            var ordered=OrderedQuestIds(worldEventId);float[] lows={0.485f,0.275f,0.065f};
            for(int i=0;i<ordered.Count&&i<3;i++)BuildQuestRow(card.Content,ordered[i],lows[i],lows[i]+0.18f);
        }

        private void BuildQuestRow(Transform parent,string questId,float y0,float y1)
        {
            var q=CampaignContent0845.ValewatchQuestPlan[questId];
            var button=StudioFantasyUI0849.QuestRow(parent,"QuestButton_"+questId,new Vector2(0.035f,y0),new Vector2(0.965f,y1),()=>SelectQuest0847(questId),out var fill,out var border);
            var id=StudioFantasyUI0849.Text(button.transform,"QuestId",questId,18,TextAnchor.MiddleLeft,new Vector2(0.065f,0.07f),new Vector2(0.19f,0.93f),StudioFantasyUI0849.GoldBright,FontStyle.Bold,false);
            var title=StudioFantasyUI0849.Text(button.transform,"QuestTitle",q.Name,18,TextAnchor.MiddleLeft,new Vector2(0.19f,0.07f),new Vector2(0.62f,0.93f),StudioFantasyUI0849.Ivory,FontStyle.Normal,false);
            var role=StudioFantasyUI0849.Text(button.transform,"QuestRole",ShortRole(q.Role),14,TextAnchor.MiddleCenter,new Vector2(0.62f,0.07f),new Vector2(0.81f,0.93f),RoleColor(q.Role),FontStyle.Bold,false);
            var st=StudioFantasyUI0849.Text(button.transform,"QuestStatus","LOCKED",13,TextAnchor.MiddleRight,new Vector2(0.81f,0.07f),new Vector2(0.96f,0.93f),StudioFantasyUI0849.Locked,FontStyle.Bold,false);
            StudioFantasyUI0849.Fit(id,12,18);StudioFantasyUI0849.Fit(title,12,18);StudioFantasyUI0849.Fit(role,9,14);StudioFantasyUI0849.Fit(st,9,13);
            questRows[questId]=new QuestRowVisual0849{QuestId=questId,Button=button,Fill=fill,Border=border,IdText=id,TitleText=title,RoleText=role,StatusText=st};
        }

        private void BuildBottomBar()
        {
            var plate=StudioFantasyUI0849.FramedPanel(safeAreaRoot.transform,"BottomActionBar0849",new Vector2(0.13f,0.018f),new Vector2(0.87f,0.125f),true,new Color(0.010f,0.017f,0.030f,0.975f));
            saveButton=StudioFantasyUI0849.Button(plate.Content,"SaveCampaignIIButton0849","SAVE CAMPAIGN II",new Vector2(0.018f,0.12f),new Vector2(0.315f,0.88f),SaveCampaign,false);
            continueQuestButton=StudioFantasyUI0849.Button(plate.Content,"ContinueQuestButton0849","BEGIN SELECTED QUEST",new Vector2(0.35f,0.12f),new Vector2(0.65f,0.88f),ContinueSelectedQuest0847,true);
            returnButton=StudioFantasyUI0849.Button(plate.Content,"ReturnMainMenuButton0849","RETURN TO MAIN MENU",new Vector2(0.685f,0.12f),new Vector2(0.982f,0.88f),ReturnToMainMenu,false);
            status=StudioFantasyUI0849.Text(safeAreaRoot.transform,"CampaignHubStatus0849","CAMPAIGN II READY • SELECT A QUEST TO INSPECT IT",14,TextAnchor.MiddleCenter,new Vector2(0.16f,0.126f),new Vector2(0.84f,0.148f),StudioFantasyUI0849.Muted,FontStyle.Bold);
            StudioFantasyUI0849.Fit(status,10,14);
        }

        private void Refresh()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            heading.text="CAMPAIGN II";overview.text=BuildOverviewText(gm);plan.text=BuildSequentialPlan();carryover.text=BuildCarryoverText(gm);
            foreach(var id in ValewatchWorldEvents)
            {
                bool active=string.Equals(id,gm.CurrentWorldEventId0833,StringComparison.OrdinalIgnoreCase);
                if(worldEventAccent.TryGetValue(id,out var img)&&img!=null)img.color=active?StudioFantasyUI0849.GoldBright:StudioFantasyUI0849.GoldDim;
            }
            foreach(var kv in questRows)RefreshQuestRow(kv.Key,kv.Value,gm);
        }

        private void SelectQuest0847(string questId,bool announce)
        {
            if(string.IsNullOrWhiteSpace(questId)||!CampaignContent0845.ValewatchQuestPlan.ContainsKey(questId))return;
            selectedQuestId=questId;RefreshSelectedQuest();RefreshQuestSelectionVisuals();
            if(announce)SetStatus($"Selected {questId} — {CampaignContent0845.ValewatchQuestPlan[questId].Name}.",StudioFantasyUI0849.Muted);
        }

        private void RefreshSelectedQuest()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||!CampaignContent0845.ValewatchQuestPlan.TryGetValue(selectedQuestId,out var q))return;
            var we=CampaignContent0833.WorldEvents[q.WorldEventId];
            selectedQuestTitle.text=$"{q.QuestId} — {q.Name.ToUpperInvariant()}";
            selectedQuestRole.text=ShortRole(q.Role);selectedQuestRole.color=RoleColor(q.Role);
            selectedQuestWorldEvent.text=$"{q.WorldEventId} — {we.Name.ToUpperInvariant()}";
            selectedQuestDescription.text=QuestDescription(q,we);
            bool canLaunch=gm.CanBeginCampaignQuest0833(q.QuestId);bool completed=gm.CompletedCampaignQuests0833.Contains(q.QuestId,StringComparer.OrdinalIgnoreCase);
            if(completed){selectedQuestAvailability.text="COMPLETED • Select another quest to continue the campaign.";selectedQuestAvailability.color=StudioFantasyUI0849.Green;}
            else if(canLaunch){selectedQuestAvailability.text="AVAILABLE • Ready for quest briefing and party ready-up.";selectedQuestAvailability.color=StudioFantasyUI0849.Green;}
            else {selectedQuestAvailability.text="LOCKED • Campaign progression has not unlocked this Quest yet.";selectedQuestAvailability.color=StudioFantasyUI0849.Muted;}
            continueQuestButton.interactable=canLaunch&&!completed;
            var label=continueQuestButton.GetComponentInChildren<Text>();if(label!=null)label.text=canLaunch?$"BEGIN {q.QuestId}":completed?$"{q.QuestId} • COMPLETED":$"{q.QuestId} • LOCKED";
        }

        private void RefreshQuestSelectionVisuals()
        {
            foreach(var kv in questRows)StudioFantasyUI0849.SetQuestRowSelected(kv.Value.Fill,kv.Value.Border,string.Equals(kv.Key,selectedQuestId,StringComparison.OrdinalIgnoreCase));
        }

        private static void RefreshQuestRow(string questId,QuestRowVisual0849 row,AscendanceGameManager gm)
        {
            if(row==null||gm==null)return;
            bool completed=gm.CompletedCampaignQuests0833.Contains(questId,StringComparer.OrdinalIgnoreCase);bool available=gm.CanBeginCampaignQuest0833(questId);
            if(completed){row.StatusText.text="DONE";row.StatusText.color=StudioFantasyUI0849.Green;}
            else if(available){row.StatusText.text="READY";row.StatusText.color=StudioFantasyUI0849.Green;}
            else {row.StatusText.text="LOCKED";row.StatusText.color=StudioFantasyUI0849.Locked;}
            row.Button.interactable=true;
        }

        private static string BuildOverviewText(AscendanceGameManager gm)
        {
            var c=CampaignContent0845.Campaigns[CampaignContent0845.CampaignII];
            return $"CROWNLANDS  •  HUB: VALEWATCH\n\n{c.CorePremise}\n\nENTRY\n{c.EntryBand}\n\nCURRENT CHAPTER\n{gm.CurrentWorldEventId0833} — {gm.CurrentWorldEventName0833}  •  DAY {gm.WorldEventDay0833}";
        }

        private static string BuildCarryoverText(AscendanceGameManager gm)
        {
            var lines=gm.ParticipatingHeroes.OrderBy(h=>h.PartyIndex).Select(h=>$"◆ {h.DisplayName}    LV {h.Level}    XP {h.XP}    KILLS {h.KillCount}").ToList();
            string unlock=gm.CampaignArchive0845.FirstOrDefault(x=>x!=null&&x.CampaignId==CampaignContent0845.CampaignI)?.TitleUnlock??"None";
            lines.Add("");lines.Add($"PARTY GOLD  {gm.PartyGold}     •     REPUTATION  {gm.GeneralReputation}");lines.Add($"CAMPAIGN I TITLE  {unlock}");return string.Join("\n",lines);
        }

        private static string BuildSequentialPlan()
        {
            var blocks=new List<string>();foreach(var we in ValewatchWorldEvents){var def=CampaignContent0833.WorldEvents[we];var qs=OrderedQuestIds(we).Select(id=>$"{id} {CampaignContent0845.ValewatchQuestPlan[id].Name}");blocks.Add($"{we} — {def.Name}: {string.Join(" • ",qs)}");}return string.Join("\n",blocks);
        }

        private static string QuestDescription(CampaignQuestPlan0845 q,WorldEventDefinition0833 we)
        {
            string role=ShortRole(q.Role);string chapter=we.DayLimit>0?$"This World Event has a {we.DayLimit}-day limit.":"This is the final Campaign II World Event.";
            if(q.QuestId=="Q013")return "A merchant caravan bound for Valewatch has vanished on the eastern road. Investigate the disappearance, follow the first signs of the Quiet Crown, and open Campaign II's street-level mystery.\n\n"+chapter;
            return $"{role} in {q.WorldEventId} — {we.Name}. Select this quest to inspect its place in Campaign II. Its authored scenario map, objectives, enemies, and briefing are live when campaign progression unlocks it.\n\n{chapter}";
        }

        private static string ShortRole(string role){if(string.Equals(role,"Final Quest",StringComparison.OrdinalIgnoreCase))return "FINAL QUEST";if(string.Equals(role,"Optional",StringComparison.OrdinalIgnoreCase))return "OPTIONAL";return "MAIN QUEST";}
        private static Color RoleColor(string role){if(string.Equals(role,"Final Quest",StringComparison.OrdinalIgnoreCase))return new Color(1f,0.75f,0.28f,1f);if(string.Equals(role,"Optional",StringComparison.OrdinalIgnoreCase))return new Color(0.58f,0.75f,1f,1f);return StudioFantasyUI0849.Ivory;}
        private static List<string> OrderedQuestIds(string worldEventId)=>CampaignContent0845.ValewatchQuestPlan.Values.Where(q=>q!=null&&string.Equals(q.WorldEventId,worldEventId,StringComparison.OrdinalIgnoreCase)).OrderBy(q=>QuestNumber(q.QuestId)).Select(q=>q.QuestId).ToList();
        private static int QuestNumber(string questId){if(string.IsNullOrWhiteSpace(questId))return int.MaxValue;string digits=new string(questId.Where(char.IsDigit).ToArray());return int.TryParse(digits,out var n)?n:int.MaxValue;}
        private static bool ValidateSequentialBlocks(){string[][] expected={new[]{"Q013","Q014","Q015"},new[]{"Q016","Q017","Q018"},new[]{"Q019","Q020","Q021"},new[]{"Q022","Q023","Q024"}};for(int i=0;i<ValewatchWorldEvents.Length;i++)if(!OrderedQuestIds(ValewatchWorldEvents[i]).SequenceEqual(expected[i],StringComparer.OrdinalIgnoreCase))return false;return true;}

        private int FunctionalSpriteCount0849()
        {
            if(root==null)return -1;return root.GetComponentsInChildren<Image>(true).Count(x=>x!=null&&x.sprite!=null);
        }

        private void SetStatus(string message,Color color){if(status==null)return;status.text=message??string.Empty;status.color=color;}
        private void SetVisible(bool v){if(root!=null)root.SetActive(v);}
    }
}
