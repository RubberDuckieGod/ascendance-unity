using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using Ascendance.Online;

namespace Ascendance
{
    public sealed class DowntimeUI0836 : MonoBehaviour
    {
        public static DowntimeUI0836 Instance { get; private set; }
        public bool IsOpen => rootPanel!=null&&rootPanel.activeSelf;
        public string DebugTab0836 => currentTab;
        public string DebugResourceText0836 => resourceText?.text??string.Empty;
        public string DebugProfessionText0836 => professionText?.text??string.Empty;
        public string DebugTradeRecipient0838 => selectedTradeRecipientId??string.Empty;
        public string DebugActivityText0839 => activityText?.text??string.Empty;
        public string DebugLoadoutText0840 => loadoutText?.text??string.Empty;
        public string DebugSaveStatus0843 => saveStatusText0843?.text??string.Empty;

        private GameObject rootPanel;
        private Transform heroListRoot;
        private Transform contentRoot;
        private Text titleText;
        private Text resourceText;
        private Text stateText;
        private Text heroDetailText;
        private Text inventoryText;
        private Text merchantStatusText;
        private Text professionText;
        private Text activityText;
        private Text loadoutText;
        private Text saveStatusText0843;
        private Button recoverButton;
        private Button equipmentButton;
        private readonly List<GameObject> dynamicContent=new();
        private readonly List<Button> heroButtons=new();
        private string currentTab="OVERVIEW";
        private DowntimeProfession0836 selectedProfession=DowntimeProfession0836.Blacksmithing;
        private int craftingPage0855;
        private DowntimeProfession0836 selectedActivityProfession=DowntimeProfession0836.Blacksmithing;
        private string selectedTradeRecipientId;

        private static readonly Color Backdrop=new(0.004f,0.007f,0.011f,0.93f);
        private static readonly Color Card=new(0.030f,0.037f,0.045f,0.99f);

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
            Refresh();
        }

        private void OnDestroy()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm!=null)gm.StateChanged-=OnStateChanged;
            if(Instance==this)Instance=null;
        }

        public void Open()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm==null||!gm.IsDowntimeAvailable0836)return;
            QuestResultsUI0835.Instance?.Close();
            CampaignHubUI0834.Instance?.Close();
            currentTab="OVERVIEW";
            SetVisible(true);
            Refresh();
        }

        public void Close()=>SetVisible(false);
        public void OpenMerchant(){if(!IsOpen)Open();currentTab="MERCHANT";Refresh();}
        public void OpenActivity(){if(!IsOpen)Open();currentTab="ACTIVITY";Refresh();}
        public void OpenCrafting(){if(!IsOpen)Open();currentTab="CRAFTING";craftingPage0855=0;Refresh();}
        public void OpenTrade(){if(!IsOpen)Open();currentTab="TRADE";Refresh();}
        public void OpenLoadout(){if(!IsOpen)Open();currentTab="LOADOUT";Refresh();}
        public void OpenOverview(){if(!IsOpen)Open();currentTab="OVERVIEW";Refresh();}

        public void ChooseNextQuest()
        {
            Close();
            CampaignHubUI0834.Instance?.Open();
        }

        public void SaveCampaign()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;
            bool ok=CampaignSaveUx0843.SaveWithFeedback0843(gm,out var message);
            if(saveStatusText0843!=null){saveStatusText0843.text=message;saveStatusText0843.color=ok?new Color(0.72f,0.78f,0.70f):new Color(0.95f,0.48f,0.38f);}
            Refresh();
        }

        private void OnStateChanged()
        {
            var gm=AscendanceGameManager.Instance;
            if(gm==null)return;
            if(IsOpen&&!gm.IsDowntimeAvailable0836){Close();return;}
            if(IsOpen)Refresh();
        }

        private void Build()
        {
            var canvas=ShellUIFactory.CreateCanvas("DowntimeCanvas_0836",525);
            canvas.transform.SetParent(transform,false);
            rootPanel=ShellUIFactory.PanelRect(canvas.transform,"DowntimeBackdrop",Vector2.zero,Vector2.one,Backdrop);
            var frame=ShellUIFactory.PanelRect(rootPanel.transform,"DowntimeFrame",new Vector2(0.055f,0.05f),new Vector2(0.945f,0.95f),new Color(0.016f,0.022f,0.030f,0.998f));

            titleText=ShellUIFactory.TextRect(frame.transform,"DOWNTIME",34,TextAnchor.MiddleLeft,new Vector2(0.025f,0.91f),new Vector2(0.56f,0.975f),ShellUIFactory.Gold);Fit(titleText,18,34);
            resourceText=ShellUIFactory.TextRect(frame.transform,"",17,TextAnchor.MiddleRight,new Vector2(0.55f,0.91f),new Vector2(0.84f,0.975f),ShellUIFactory.Ivory);Fit(resourceText,10,17);
            ShellUIFactory.ButtonRect(frame.transform,"CLOSE",new Vector2(0.855f,0.925f),new Vector2(0.975f,0.968f),Close);

            stateText=ShellUIFactory.TextRect(frame.transform,"",14,TextAnchor.MiddleLeft,new Vector2(0.025f,0.86f),new Vector2(0.975f,0.91f),new Color(0.72f,0.76f,0.80f));Fit(stateText,9,14);

            var heroPanel=ShellUIFactory.PanelRect(frame.transform,"DowntimeHeroes",new Vector2(0.025f,0.145f),new Vector2(0.245f,0.85f),Card);
            ShellUIFactory.TextRect(heroPanel.transform,"PARTY",16,TextAnchor.MiddleLeft,new Vector2(0.06f,0.92f),new Vector2(0.94f,0.985f),ShellUIFactory.Gold);
            var hl=new GameObject("HeroList");hl.transform.SetParent(heroPanel.transform,false);var hrt=hl.AddComponent<RectTransform>();hrt.anchorMin=new Vector2(0.04f,0.06f);hrt.anchorMax=new Vector2(0.96f,0.91f);hrt.offsetMin=hrt.offsetMax=Vector2.zero;heroListRoot=hl.transform;

            var tabs=ShellUIFactory.PanelRect(frame.transform,"DowntimeTabs",new Vector2(0.26f,0.79f),new Vector2(0.975f,0.85f),new Color(0.024f,0.030f,0.038f,1f));
            var overview=ShellUIFactory.ButtonRect(tabs.transform,"OVERVIEW",new Vector2(0.01f,0.12f),new Vector2(0.14f,0.88f),OpenOverview);Fit(overview.GetComponentInChildren<Text>(),7,12);
            var activity=ShellUIFactory.ButtonRect(tabs.transform,"ACTIVITY",new Vector2(0.145f,0.12f),new Vector2(0.275f,0.88f),OpenActivity);Fit(activity.GetComponentInChildren<Text>(),7,12);
            var merchant=ShellUIFactory.ButtonRect(tabs.transform,"MERCHANT",new Vector2(0.28f,0.12f),new Vector2(0.41f,0.88f),OpenMerchant);Fit(merchant.GetComponentInChildren<Text>(),7,12);
            var crafting=ShellUIFactory.ButtonRect(tabs.transform,"CRAFTING",new Vector2(0.415f,0.12f),new Vector2(0.545f,0.88f),OpenCrafting);Fit(crafting.GetComponentInChildren<Text>(),7,12);
            var trade=ShellUIFactory.ButtonRect(tabs.transform,"TRADE",new Vector2(0.55f,0.12f),new Vector2(0.68f,0.88f),OpenTrade);Fit(trade.GetComponentInChildren<Text>(),7,12);
            var loadout=ShellUIFactory.ButtonRect(tabs.transform,"LOADOUT",new Vector2(0.685f,0.12f),new Vector2(0.815f,0.88f),OpenLoadout);Fit(loadout.GetComponentInChildren<Text>(),7,12);
            var campaign=ShellUIFactory.ButtonRect(tabs.transform,"CHOOSE QUEST",new Vector2(0.82f,0.12f),new Vector2(0.99f,0.88f),ChooseNextQuest);Fit(campaign.GetComponentInChildren<Text>(),7,12);

            var content=ShellUIFactory.PanelRect(frame.transform,"DowntimeContent",new Vector2(0.26f,0.145f),new Vector2(0.975f,0.78f),Card);
            contentRoot=content.transform;

            var footer=ShellUIFactory.PanelRect(frame.transform,"DowntimeFooter",new Vector2(0.025f,0.035f),new Vector2(0.975f,0.125f),new Color(0.024f,0.030f,0.038f,1f));
            var recoverAll=ShellUIFactory.ButtonRect(footer.transform,"RECOVER PARTY",new Vector2(0.01f,0.17f),new Vector2(0.23f,0.83f),()=>AscendanceClientCommandRouter0101.Instance?.RequestRecoverParty());Fit(recoverAll.GetComponentInChildren<Text>(),9,15);
            var save=ShellUIFactory.ButtonRect(footer.transform,"SAVE CAMPAIGN",new Vector2(0.75f,0.17f),new Vector2(0.99f,0.83f),SaveCampaign);Fit(save.GetComponentInChildren<Text>(),9,15);
            var note=ShellUIFactory.TextRect(footer.transform,"Each Hero gets 1 Activity per Downtime: Work or Gather. Crafting and Trading remain unlimited. Gold is shared; materials stay with the Hero carrying them.",12,TextAnchor.MiddleCenter,new Vector2(0.25f,0.40f),new Vector2(0.73f,0.90f),new Color(0.60f,0.64f,0.68f));Fit(note,8,12);
            saveStatusText0843=ShellUIFactory.TextRect(footer.transform,"",11,TextAnchor.MiddleCenter,new Vector2(0.25f,0.08f),new Vector2(0.73f,0.40f),new Color(0.70f,0.74f,0.78f));Fit(saveStatusText0843,8,11);
        }

        private void Refresh()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||rootPanel==null)return;
            if(titleText!=null)titleText.text=$"{gm.CurrentCampaignHub0845.ToUpperInvariant()}  •  DOWNTIME";
            resourceText.text=gm.DowntimeResourceSummary0836;
            stateText.text=gm.IsDowntimeAvailable0836?$"BETWEEN QUESTS  •  {gm.CurrentWorldEventId0833} {gm.CurrentWorldEventName0833}  •  WORLD EVENT DAY {gm.WorldEventDay0833}  •  TOTAL DAY {gm.TotalCampaignDay0833}":"DOWNTIME UNAVAILABLE WHILE A QUEST IS ACTIVE";
            RebuildHeroes(gm);
            RebuildContent(gm);
        }

        private void RebuildHeroes(AscendanceGameManager gm)
        {
            foreach(var b in heroButtons)if(b!=null)Destroy(b.gameObject);heroButtons.Clear();
            var heroes=gm.ParticipatingHeroes.ToList();int n=Mathf.Max(1,heroes.Count);float h=1f/n;
            for(int i=0;i<heroes.Count;i++)
            {
                var hero=heroes[i];float top=1f-i*h;float bottom=top-h+0.02f;
                var b=ShellUIFactory.ButtonRect(heroListRoot,hero.DisplayName,new Vector2(0.01f,bottom),new Vector2(0.99f,top-0.01f),()=>{gm.SelectHero(hero.Id);Refresh();});
                b.GetComponentInChildren<Text>().text=$"{hero.DisplayName}\nHP {hero.Health}/{hero.MaxHealth}  •  Lv {hero.Level}\nGear {hero.AcquiredEquipment.Count}  •  Loot {hero.LootInventory.Count}";
                Fit(b.GetComponentInChildren<Text>(),8,13);heroButtons.Add(b);
                if(string.Equals(gm.SelectedHeroId,hero.Id,StringComparison.OrdinalIgnoreCase))b.GetComponent<Image>().color=new Color(0.20f,0.14f,0.055f,1f);
            }
        }

        private void RebuildContent(AscendanceGameManager gm)
        {
            foreach(var go in dynamicContent)if(go!=null)Destroy(go);dynamicContent.Clear();
            if(currentTab=="ACTIVITY")BuildActivity(gm);
            else if(currentTab=="MERCHANT")BuildMerchant(gm);
            else if(currentTab=="CRAFTING")BuildCrafting(gm);
            else if(currentTab=="TRADE")BuildTrade(gm);
            else if(currentTab=="LOADOUT")BuildLoadout(gm);
            else BuildOverview(gm);
        }

        private void BuildOverview(AscendanceGameManager gm)
        {
            var hero=gm.SelectedHero??gm.ParticipatingHeroes.FirstOrDefault();if(hero==null)return;
            heroDetailText=AddText("HeroDetail",gm.DowntimeHeroSummary0836(hero.Id),20,TextAnchor.UpperLeft,new Vector2(0.035f,0.53f),new Vector2(0.53f,0.94f),ShellUIFactory.Ivory,11,20);
            inventoryText=AddText("Inventory",gm.DowntimeInventorySummary0836(hero.Id),15,TextAnchor.UpperLeft,new Vector2(0.55f,0.20f),new Vector2(0.965f,0.94f),new Color(0.76f,0.80f,0.84f),9,15);
            recoverButton=AddButton("RECOVER "+hero.DisplayName.ToUpperInvariant(),new Vector2(0.035f,0.33f),new Vector2(0.50f,0.47f),()=>AscendanceClientCommandRouter0101.Instance?.RequestRecoverHero(hero.Id));
            recoverButton.interactable=gm.CanRecoverHero0836(hero.Id);
            equipmentButton=AddButton("PREPARE LOADOUT",new Vector2(0.035f,0.17f),new Vector2(0.50f,0.30f),OpenLoadout);
            equipmentButton.interactable=gm.CanManageDowntimeLoadout0840(hero.Id);
            AddText("OverviewNote","Recovery restores full Health, clears negative Conditions, removes Downed/Dead state, and restores at least 1 Healing Potion. It grants no XP.",13,TextAnchor.UpperLeft,new Vector2(0.035f,0.035f),new Vector2(0.50f,0.15f),new Color(0.62f,0.66f,0.70f),8,13);
        }

        private void BuildActivity(AscendanceGameManager gm)
        {
            var hero=gm.SelectedHero??gm.ParticipatingHeroes.FirstOrDefault();if(hero==null)return;
            activityText=AddText("ActivityStatus",gm.DowntimeActivityStatus0839(hero.Id),16,TextAnchor.UpperLeft,new Vector2(0.035f,0.78f),new Vector2(0.965f,0.95f),ShellUIFactory.Ivory,9,16);
            AddText("ActivityRule","ONE ACTIVITY PER HERO PER DOWNTIME  •  Crafting and Trading do not consume this Activity.",13,TextAnchor.MiddleLeft,new Vector2(0.035f,0.70f),new Vector2(0.965f,0.77f),ShellUIFactory.Gold,8,13);

            var work=AddButton("WORK  •  +25 PARTY GOLD",new Vector2(0.035f,0.55f),new Vector2(0.47f,0.67f),()=>AscendanceClientCommandRouter0101.Instance?.RequestWorkDowntime(hero.Id));
            work.interactable=gm.CanTakeDowntimeActivity0839(hero.Id);

            AddText("GatherHeader","GATHER RESOURCES  •  choose a profession  •  find 1–3 total materials  •  mixed materials possible",14,TextAnchor.MiddleLeft,new Vector2(0.035f,0.46f),new Vector2(0.965f,0.54f),new Color(0.76f,0.80f,0.84f),8,14);
            var profs=(DowntimeProfession0836[])Enum.GetValues(typeof(DowntimeProfession0836));
            for(int i=0;i<profs.Length;i++)
            {
                var p=profs[i];float left=0.035f+i*0.232f;
                int available=gm.GatherableResources0839(p).Count;
                var b=AddButton($"{p}\n{available} TYPES IN DECK",new Vector2(left,0.33f),new Vector2(left+0.215f,0.445f),()=>{selectedActivityProfession=p;Refresh();});
                if(p==selectedActivityProfession)b.GetComponent<Image>().color=new Color(0.20f,0.14f,0.055f,1f);
            }
            string examples=string.Join("  •  ",gm.GatherableResources0839(selectedActivityProfession).Take(6).Select(RewardContent0826.LootLabel));
            if(string.IsNullOrWhiteSpace(examples))examples="No matching Crafting Materials remain in the live Loot Deck.";
            AddText("GatherExamples",$"{selectedActivityProfession.ToString().ToUpperInvariant()} GATHER POOL\n{examples}",13,TextAnchor.UpperLeft,new Vector2(0.035f,0.17f),new Vector2(0.72f,0.31f),ShellUIFactory.Ivory,8,13);
            var gather=AddButton("GATHER 1–3 MATERIALS",new Vector2(0.74f,0.17f),new Vector2(0.965f,0.31f),()=>AscendanceClientCommandRouter0101.Instance?.RequestGatherResources(hero.Id,selectedActivityProfession));
            gather.interactable=gm.CanGatherResources0839(hero.Id,selectedActivityProfession);
            AddText("ActivityNote","Gather removes the found resource cards from the live Loot Deck and places them only in this Hero's inventory. It grants no profession XP and never changes Party Gold.",12,TextAnchor.UpperLeft,new Vector2(0.035f,0.035f),new Vector2(0.965f,0.14f),new Color(0.62f,0.66f,0.70f),8,12);
        }

        private void BuildLoadout(AscendanceGameManager gm)
        {
            var hero=gm.SelectedHero??gm.ParticipatingHeroes.FirstOrDefault();if(hero==null)return;
            loadoutText=AddText("LoadoutSummary",gm.DowntimeLoadoutSummary0840(hero.Id),15,TextAnchor.UpperLeft,new Vector2(0.035f,0.835f),new Vector2(0.965f,0.95f),ShellUIFactory.Ivory,9,15);
            AddText("LoadoutRule","FREE DOWNTIME SERVICE  •  Equipping and unequipping never consumes this Hero's Work/Gather Activity. Equipped gear cannot be traded until unequipped.",12,TextAnchor.MiddleLeft,new Vector2(0.035f,0.765f),new Vector2(0.965f,0.83f),ShellUIFactory.Gold,8,12);

            AddText("EquippedHeader","EQUIPPED LOADOUT",13,TextAnchor.MiddleLeft,new Vector2(0.035f,0.71f),new Vector2(0.965f,0.755f),ShellUIFactory.Gold,8,13);
            var equipped=gm.DowntimeEquippedGear0840(hero.Id).Take(5).ToList();
            for(int i=0;i<equipped.Count;i++)
            {
                var gear=equipped[i];string equipmentId=gear.Id;float top=0.70f-i*0.083f;float bottom=top-0.071f;
                var panel=ShellUIFactory.PanelRect(contentRoot,"Equipped_"+equipmentId+"_"+i,new Vector2(0.035f,bottom),new Vector2(0.965f,top),new Color(0.038f,0.045f,0.054f,1f));dynamicContent.Add(panel);
                string equippedAbility=string.Join(" • ",new[]{gear.Ability1,gear.Ability2}.Where(x=>!string.IsNullOrWhiteSpace(x)));
                var label=ShellUIFactory.TextRect(panel.transform,$"{gear.Label}\n{gear.Slot} • {gear.Hands} • ATK +{gear.Attack} • ARM +{gear.Armor}{(string.IsNullOrWhiteSpace(equippedAbility)?string.Empty:$"\nABILITY — {equippedAbility}")}",12,TextAnchor.MiddleLeft,new Vector2(0.02f,0.06f),new Vector2(0.79f,0.94f),ShellUIFactory.Ivory);Fit(label,7,12);
                var unequip=ShellUIFactory.ButtonRect(panel.transform,"UNEQUIP",new Vector2(0.81f,0.18f),new Vector2(0.98f,0.82f),()=>AscendanceClientCommandRouter0101.Instance?.RequestUnequipDowntimeEquipment(hero.Id,equipmentId));
                unequip.interactable=gm.CanUnequipDowntimeEquipment0840(hero.Id,equipmentId);Fit(unequip.GetComponentInChildren<Text>(),7,12);
            }
            if(equipped.Count==0)AddText("EquippedEmpty","No Equipment equipped.",12,TextAnchor.MiddleLeft,new Vector2(0.04f,0.48f),new Vector2(0.965f,0.69f),new Color(0.58f,0.62f,0.66f),8,12);

            AddText("InventoryGearHeader","UNEQUIPPED INVENTORY  •  comparison is against the currently equipped matching slot",13,TextAnchor.MiddleLeft,new Vector2(0.035f,0.265f),new Vector2(0.965f,0.31f),ShellUIFactory.Gold,8,13);
            var inventory=gm.DowntimeUnequippedGear0840(hero.Id).Take(5).ToList();
            for(int i=0;i<inventory.Count;i++)
            {
                var gear=inventory[i];string equipmentId=gear.Id;float top=0.255f-i*0.047f;float bottom=top-0.041f;
                bool accessory=string.Equals(gear.Slot,"Accessory",StringComparison.OrdinalIgnoreCase);
                int accessoryCount=gm.DowntimeEquippedGear0840(hero.Id).Count(x=>string.Equals(x.Slot,"Accessory",StringComparison.OrdinalIgnoreCase));
                int accessoryCap=string.Equals(hero.Id,"Seraphine",StringComparison.OrdinalIgnoreCase)?1:2;
                string compare=gm.DowntimeEquipmentComparison0840(hero.Id,equipmentId);
                string inventoryAbility=string.Join(" • ",new[]{gear.Ability1,gear.Ability2}.Where(x=>!string.IsNullOrWhiteSpace(x)));
                AddText("InvGear_"+equipmentId+"_"+i,$"{gear.Label}  •  {gear.Slot}  •  ATK +{gear.Attack} ARM +{gear.Armor}{(string.IsNullOrWhiteSpace(inventoryAbility)?string.Empty:$"  •  ABILITY: {inventoryAbility}")}\n{compare}",10,TextAnchor.MiddleLeft,new Vector2(0.04f,bottom),new Vector2(accessory&&accessoryCount>=accessoryCap?0.68f:0.80f,top),ShellUIFactory.Ivory,7,10);
                if(accessory&&accessoryCount>=accessoryCap)
                {
                    var equippedAccessories=gm.DowntimeEquippedGear0840(hero.Id).Where(x=>string.Equals(x.Slot,"Accessory",StringComparison.OrdinalIgnoreCase)).ToList();
                    for(int r=0;r<equippedAccessories.Count&&r<2;r++)
                    {
                        int replaceIndex=r;string shortName=equippedAccessories[r].Label;
                        var b=AddButton("REPLACE "+(r+1),new Vector2(0.70f+r*0.135f,bottom),new Vector2(0.825f+r*0.135f,top),()=>AscendanceClientCommandRouter0101.Instance?.RequestEquipDowntimeEquipment(hero.Id,equipmentId,replaceIndex));
                        b.interactable=gm.CanEquipDowntimeEquipment0840(hero.Id,equipmentId,replaceIndex);Fit(b.GetComponentInChildren<Text>(),6,9);
                    }
                }
                else
                {
                    var equip=AddButton("EQUIP",new Vector2(0.82f,bottom),new Vector2(0.965f,top),()=>AscendanceClientCommandRouter0101.Instance?.RequestEquipDowntimeEquipment(hero.Id,equipmentId));
                    equip.interactable=gm.CanEquipDowntimeEquipment0840(hero.Id,equipmentId);Fit(equip.GetComponentInChildren<Text>(),7,11);
                }
            }
            if(inventory.Count==0)AddText("InventoryGearEmpty","No unequipped Equipment in this Hero's personal inventory.",11,TextAnchor.MiddleLeft,new Vector2(0.04f,0.035f),new Vector2(0.965f,0.25f),new Color(0.58f,0.62f,0.66f),7,11);
        }

        private void BuildMerchant(AscendanceGameManager gm)
        {
            merchantStatusText=AddText("MerchantStatus",gm.DowntimeMerchantStatus0836,15,TextAnchor.UpperLeft,new Vector2(0.035f,0.82f),new Vector2(0.965f,0.95f),gm.StonebrookEquipmentPurchaseLocked0836?new Color(0.95f,0.48f,0.38f):ShellUIFactory.Ivory,9,15);
            var hero=gm.SelectedHero??gm.ParticipatingHeroes.FirstOrDefault();
            AddText("MerchantHero",hero==null?"No Hero selected.":$"BUYING FOR  •  {hero.DisplayName}\nPurchased Equipment enters this Hero's unequipped inventory.",15,TextAnchor.UpperLeft,new Vector2(0.035f,0.69f),new Vector2(0.965f,0.81f),ShellUIFactory.Gold,9,15);
            var stock=gm.DowntimeMerchantStock0836;
            for(int i=0;i<3;i++)
            {
                float top=0.65f-i*0.20f;float bottom=top-0.17f;
                if(i>=stock.Count)
                {
                    AddText("EmptyStock","EMPTY STOCK SLOT",15,TextAnchor.MiddleCenter,new Vector2(0.035f,bottom),new Vector2(0.965f,top),new Color(0.45f,0.48f,0.52f),9,15);continue;
                }
                string id=stock[i];RewardContent0826.Equipment.TryGetValue(id,out var d);int price=gm.MerchantPrice0836(id);
                var panel=ShellUIFactory.PanelRect(contentRoot,"Merchant_"+id,new Vector2(0.035f,bottom),new Vector2(0.965f,top),new Color(0.038f,0.045f,0.054f,1f));dynamicContent.Add(panel);
                string ability=RewardContent0826.EquipmentAbilityText(id);
                var label=ShellUIFactory.TextRect(panel.transform,$"{RewardContent0826.EquipmentLabel(id)}\n{d?.Type} • {d?.Slot} • ATK {d?.Attack??0} • ARM {d?.Armor??0}   |   {price} GOLD{(string.IsNullOrWhiteSpace(ability)?string.Empty:$"\nABILITY — {ability}")}",16,TextAnchor.MiddleLeft,new Vector2(0.025f,0.08f),new Vector2(0.75f,0.92f),ShellUIFactory.Ivory);Fit(label,8,16);
                var buy=ShellUIFactory.ButtonRect(panel.transform,"BUY",new Vector2(0.77f,0.18f),new Vector2(0.97f,0.82f),()=>{if(hero!=null)AscendanceClientCommandRouter0101.Instance?.RequestPurchaseMerchantEquipment(hero.Id,id);});
                buy.interactable=hero!=null&&gm.CanPurchaseMerchantEquipment0836(id,hero.Id);Fit(buy.GetComponentInChildren<Text>(),9,15);
            }
        }

        private void BuildCrafting(AscendanceGameManager gm)
        {
            var hero=gm.SelectedHero??gm.ParticipatingHeroes.FirstOrDefault();
            AddText("CraftingStatus",gm.DowntimeCraftingStatus0836,13,TextAnchor.UpperLeft,new Vector2(0.035f,0.875f),new Vector2(0.965f,0.95f),new Color(0.72f,0.76f,0.80f),8,13);
            AddText("CraftingHero",hero==null?"No Hero selected.":$"CRAFTING AS  •  {hero.DisplayName}  •  only this Hero's materials may be used",14,TextAnchor.MiddleLeft,new Vector2(0.035f,0.80f),new Vector2(0.965f,0.87f),ShellUIFactory.Gold,9,14);
            var profs=(DowntimeProfession0836[])Enum.GetValues(typeof(DowntimeProfession0836));
            for(int i=0;i<profs.Length;i++)
            {
                var p=profs[i];float left=0.035f+i*0.232f;
                int materialCount=hero==null?0:gm.DowntimeProfessionMaterialCount0855(hero.Id,p);
                int level=hero==null?1:gm.ProfessionLevel0837(hero.Id,p);
                var b=AddButton($"{p}\nLV {level}  •  {materialCount} MAT",new Vector2(left,0.685f),new Vector2(left+0.215f,0.79f),()=>{selectedProfession=p;craftingPage0855=0;Refresh();});
                if(p==selectedProfession)b.GetComponent<Image>().color=new Color(0.20f,0.14f,0.055f,1f);
            }
            professionText=AddText("ProfessionSummary",hero==null?"No Hero selected.":gm.DowntimeProfessionSummary0855(hero.Id,selectedProfession),13,TextAnchor.UpperLeft,new Vector2(0.035f,0.61f),new Vector2(0.965f,0.67f),ShellUIFactory.Ivory,8,13);
            if(hero==null)return;
            var recipes=gm.CraftingRecipes0855(selectedProfession);
            int pageCount=Mathf.Max(1,Mathf.CeilToInt(recipes.Count/5f));craftingPage0855=Mathf.Clamp(craftingPage0855,0,pageCount-1);
            var prev=AddButton("◀ PREV",new Vector2(0.035f,0.557f),new Vector2(0.18f,0.605f),()=>{craftingPage0855=Mathf.Max(0,craftingPage0855-1);Refresh();});prev.interactable=craftingPage0855>0;
            AddText("CraftPage",$"{selectedProfession.ToString().ToUpperInvariant()} RECIPES  •  PAGE {craftingPage0855+1}/{pageCount}  •  {recipes.Count} AUTHORED",11,TextAnchor.MiddleCenter,new Vector2(0.19f,0.557f),new Vector2(0.81f,0.605f),ShellUIFactory.Gold,7,11);
            var next=AddButton("NEXT ▶",new Vector2(0.82f,0.557f),new Vector2(0.965f,0.605f),()=>{craftingPage0855=Mathf.Min(pageCount-1,craftingPage0855+1);Refresh();});next.interactable=craftingPage0855<pageCount-1;
            var page=recipes.Skip(craftingPage0855*5).Take(5).ToList();
            for(int i=0;i<page.Count;i++)
            {
                var recipe=page[i];string recipeId=recipe.Id;float top=0.545f-i*0.101f;float bottom=top-0.088f;
                var panel=ShellUIFactory.PanelRect(contentRoot,"Recipe_"+recipe.Id,new Vector2(0.035f,bottom),new Vector2(0.965f,top),new Color(0.038f,0.045f,0.054f,1f));dynamicContent.Add(panel);
                var label=ShellUIFactory.TextRect(panel.transform,gm.CraftingRecipeLabel0855(recipe,hero.Id),11,TextAnchor.MiddleLeft,new Vector2(0.02f,0.04f),new Vector2(0.79f,0.96f),ShellUIFactory.Ivory);Fit(label,6,11);
                var craft=ShellUIFactory.ButtonRect(panel.transform,"CRAFT",new Vector2(0.815f,0.18f),new Vector2(0.98f,0.82f),()=>AscendanceClientCommandRouter0101.Instance?.RequestCraftRecipe(hero.Id,recipeId));
                craft.interactable=gm.CanCraftRecipe0855(recipeId,hero.Id);Fit(craft.GetComponentInChildren<Text>(),8,13);
            }
        }

        private void BuildTrade(AscendanceGameManager gm)
        {
            var from=gm.SelectedHero??gm.ParticipatingHeroes.FirstOrDefault();if(from==null)return;
            var partners=gm.ParticipatingHeroes.Where(h=>!string.Equals(h.Id,from.Id,StringComparison.OrdinalIgnoreCase)).ToList();
            if(partners.Count==0)
            {
                AddText("TradeSolo","HERO-TO-HERO TRADE requires at least two participating Heroes.",18,TextAnchor.MiddleCenter,new Vector2(0.05f,0.25f),new Vector2(0.95f,0.80f),ShellUIFactory.Ivory,10,18);
                return;
            }
            if(string.IsNullOrWhiteSpace(selectedTradeRecipientId)||!partners.Any(h=>string.Equals(h.Id,selectedTradeRecipientId,StringComparison.OrdinalIgnoreCase)))selectedTradeRecipientId=partners[0].Id;
            var to=partners.First(h=>string.Equals(h.Id,selectedTradeRecipientId,StringComparison.OrdinalIgnoreCase));
            AddText("TradeTitle",gm.TradeSummary0838(from.Id,to.Id),14,TextAnchor.UpperLeft,new Vector2(0.035f,0.80f),new Vector2(0.965f,0.95f),ShellUIFactory.Ivory,9,14);

            for(int i=0;i<partners.Count;i++)
            {
                var partner=partners[i];string partnerId=partner.Id;float left=0.035f+i*(0.93f/partners.Count);float right=0.035f+(i+1)*(0.93f/partners.Count)-0.01f;
                var b=AddButton("TO "+partner.DisplayName.ToUpperInvariant(),new Vector2(left,0.705f),new Vector2(right,0.79f),()=>{selectedTradeRecipientId=partnerId;Refresh();});
                if(string.Equals(partner.Id,to.Id,StringComparison.OrdinalIgnoreCase))b.GetComponent<Image>().color=new Color(0.20f,0.14f,0.055f,1f);
            }

            var potion=AddButton($"GIVE 1 HEALING POTION  •  {from.HealingPotions} CARRIED",new Vector2(0.035f,0.615f),new Vector2(0.965f,0.69f),()=>AscendanceClientCommandRouter0101.Instance?.RequestTransferHealingPotion(from.Id,to.Id));
            potion.interactable=gm.CanTransferHealingPotion0838(from.Id,to.Id);

            AddText("TradeLootHeader","MATERIALS / LOOT / ITEMS  •  Quest Items are protected and cannot be traded",13,TextAnchor.MiddleLeft,new Vector2(0.035f,0.565f),new Vector2(0.965f,0.61f),ShellUIFactory.Gold,8,13);
            var loot=gm.TradeableHeroLoot0838(from.Id).Take(5).ToList();
            for(int i=0;i<loot.Count;i++)
            {
                var stack=loot[i];string lootId=stack.Id;float top=0.555f-i*0.07f;float bottom=top-0.06f;
                AddText("TradeLoot_"+lootId,$"{stack.Count}× {stack.Label}{(stack.CraftingMaterial?"  •  MATERIAL":"")}",12,TextAnchor.MiddleLeft,new Vector2(0.04f,bottom),new Vector2(0.70f,top),ShellUIFactory.Ivory,8,12);
                var one=AddButton("GIVE 1",new Vector2(0.72f,bottom+0.003f),new Vector2(0.845f,top-0.003f),()=>AscendanceClientCommandRouter0101.Instance?.RequestTransferHeroLoot(from.Id,to.Id,lootId,1));one.interactable=gm.CanTransferHeroLoot0838(from.Id,to.Id,lootId,1);
                var all=AddButton("GIVE ALL",new Vector2(0.855f,bottom+0.003f),new Vector2(0.965f,top-0.003f),()=>AscendanceClientCommandRouter0101.Instance?.RequestTransferAllHeroLoot(from.Id,to.Id,lootId));all.interactable=gm.CanTransferHeroLoot0838(from.Id,to.Id,lootId,stack.Count);
            }
            if(loot.Count==0)AddText("TradeLootEmpty","No transferable materials or loot carried by this Hero.",12,TextAnchor.MiddleLeft,new Vector2(0.04f,0.25f),new Vector2(0.965f,0.54f),new Color(0.58f,0.62f,0.66f),8,12);

            AddText("TradeGearHeader","UNEQUIPPED EQUIPMENT  •  Equipped gear must be unequipped before transfer",13,TextAnchor.MiddleLeft,new Vector2(0.035f,0.155f),new Vector2(0.965f,0.205f),ShellUIFactory.Gold,8,13);
            var gear=gm.TradeableHeroEquipment0838(from.Id).Take(3).ToList();
            for(int i=0;i<gear.Count;i++)
            {
                var stack=gear[i];string equipmentId=stack.Id;float top=0.145f-i*0.045f;float bottom=top-0.038f;
                AddText("TradeGear_"+equipmentId,$"{stack.Count}× {stack.Label}",11,TextAnchor.MiddleLeft,new Vector2(0.04f,bottom),new Vector2(0.78f,top),ShellUIFactory.Ivory,7,11);
                var give=AddButton("GIVE",new Vector2(0.80f,bottom),new Vector2(0.965f,top),()=>AscendanceClientCommandRouter0101.Instance?.RequestTransferEquipment(from.Id,to.Id,equipmentId));give.interactable=gm.CanTransferEquipment0838(from.Id,to.Id,equipmentId);
            }
            if(gear.Count==0)AddText("TradeGearEmpty","No unequipped Equipment available to transfer.",11,TextAnchor.MiddleLeft,new Vector2(0.04f,0.035f),new Vector2(0.965f,0.14f),new Color(0.58f,0.62f,0.66f),7,11);
        }

        private Text AddText(string name,string value,int size,TextAnchor anchor,Vector2 min,Vector2 max,Color color,int fitMin,int fitMax)
        {
            var t=ShellUIFactory.TextRect(contentRoot,value,size,anchor,min,max,color);t.gameObject.name=name;Fit(t,fitMin,fitMax);dynamicContent.Add(t.gameObject);return t;
        }

        private Button AddButton(string label,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {
            var b=ShellUIFactory.ButtonRect(contentRoot,label,min,max,action);Fit(b.GetComponentInChildren<Text>(),8,15);dynamicContent.Add(b.gameObject);return b;
        }

        private void SetVisible(bool visible){if(rootPanel!=null)rootPanel.SetActive(visible);}
        private static void Fit(Text text,int min,int max){if(text==null)return;text.resizeTextForBestFit=true;text.resizeTextMinSize=min;text.resizeTextMaxSize=max;}
    }
}
