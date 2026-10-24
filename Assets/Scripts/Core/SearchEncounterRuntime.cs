using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    [Serializable]
    public sealed class SearchCardDefinition
    {
        public string Id;
        public string Name;
        public string Category;
        public string Terrain;
        public string Rarity;
        public string Trigger;
        public string Effect;
        public string FollowUp;
        public string FlavorText;
        public string Status;
        public int Copies = 1;
    }

    [Serializable]
    public sealed class EncounterCardDefinition
    {
        public string Id;
        public string Name;
        public string EncounterType;
        public string Terrain;
        public string Severity12;
        public string Severity34;
        public string Severity56;
        public string Severity7Plus;
        public string Reward;
        public string Failure;
        public string Trigger;
        public string WorldEventScaling;
        public string FlavorText;
        public string Status;
        public int Copies = 1;

        public string EffectForSeverity(int severity)
        {
            if (severity >= 7) return Severity7Plus;
            if (severity >= 5) return Severity56;
            if (severity >= 3) return Severity34;
            return Severity12;
        }
    }

    [Serializable]
    public sealed class EncounterEnemyTemplate
    {
        public string Id;
        public string Name;
        public int Health;
        public int Attack;
        public int Armor;
        public int Movement;
        public int ThreatRating;
        public string EnemyType;
        public string Role;
        public string BehaviorProfile;
        public string Difficulty;
        public string Traits;
        public string Keywords;
        public string Range;
        public string Abilities;
        public string Behavior;
        public string Status;
        public bool Boss;
    }

    public static class SearchEncounterContent0820
    {
        public static readonly Dictionary<string, SearchCardDefinition> SearchCards = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<string, EncounterCardDefinition> EncounterCards = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<string, EncounterEnemyTemplate> EnemyTemplates = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> EliteEnemyTemplateIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "E006","E007","E008","E009","E011","E012","E013","E014","E015","E016","E017","E018","E019",
            "E021","E023","E025","E028","E029"
        };

        public static bool IsEliteEnemyTemplate(string templateId)
        {
            if(string.IsNullOrEmpty(templateId))return false;
            if(EliteEnemyTemplateIds.Contains(templateId))return true;
            return EnemyTemplates.TryGetValue(templateId,out var e)&&(((e.Difficulty??string.Empty).IndexOf("Elite",StringComparison.OrdinalIgnoreCase)>=0)||((e.Role??string.Empty).IndexOf("Elite",StringComparison.OrdinalIgnoreCase)>=0));
        }

        static SearchEncounterContent0820()
        {
            AddSearch("SD001","Hidden Cache","Discovery","Any","Common","Draw 1 Loot card.","Loot",2);
            AddSearch("SD002","Forgotten Coin Purse","Discovery","Any","Common","Gain 15 Gold.","",2);
            AddSearch("SD003","Abandoned Pack","Discovery","Forest; Road; Plains","Common","Draw 1 Loot card. Then discard 1 card from your hand or lose 5 Gold.","Loot");
            AddSearch("SD004","Old Supply Crate","Discovery","Road; Settlement; Fortress","Common","Gain 20 Gold or draw 1 Loot card.","Loot");
            AddSearch("SD005","Weathered Map","Discovery","Any Outdoor","Uncommon","Reveal 1 unrevealed Location connected to your current Location. Do not resolve its Reveal Effect until it is entered or otherwise revealed normally.","Map");
            AddSearch("SD006","Forgotten Trail Marker","Discovery","Forest; Hills; Road","Common","Look at one unrevealed connected Location. You may leave it unrevealed.","Scout");
            AddSearch("SD007","Buried Strongbox","Discovery","Ruins; Road; Battlefield","Uncommon","Gain 30 Gold and draw 1 Loot card.","Loot");
            AddSearch("SD008","Hunter's Cache","Discovery","Forest; Hills; Cave","Common","Recover 1 Health and draw 1 Loot card.","Loot");
            AddSearch("SD009","Lost Weapon Bundle","Discovery","Road; Battlefield; Fortress","Rare","Draw 1 Equipment card. You may keep it if its rarity is Common or Uncommon; otherwise place it on the bottom of the Equipment deck and gain 25 Gold.","Equipment");
            AddSearch("SD010","Traveler's Journal","Discovery","Any Outdoor","Common","Look at the top 2 Exploration cards. Put them back in any order.","Explore");
            AddSearch("SD011","Collapsed Cart","Discovery","Road; River; Marsh","Common","Gain 10 Gold and draw 1 Loot card.","Loot");
            AddSearch("SD012","Hidden Compartment","Discovery","Ruins; Fortress; Settlement","Uncommon","Draw 2 Loot cards; keep 1 and discard the other.","Loot");
            AddSearch("SD013","Ancient Coin","Discovery","Ruins; Crypt; Shrine","Uncommon","Gain 25 Gold. If the current Location has Ancient, Sacred, or Ruins among its traits or keywords, gain 10 additional Gold.","");
            AddSearch("SD014","Smuggler's Stash","Discovery","Road; Cave; Ruins","Rare","Draw 1 Loot card and gain 25 Gold. Then draw 1 Travel Encounter.","Encounter");
            AddSearch("SD015","Discarded Rations","Discovery","Any Outdoor","Common","Each Hero in this Location may recover 1 Health.","Recovery");
            AddSearch("SD016","Makeshift Bedroll","Discovery","Forest; Campsite; Outpost","Common","Remove 1 negative Condition from the exploring Hero.","Recovery");
            AddSearch("SD017","Old Battlefield Spoils","Discovery","Battlefield; Plains; Ruins","Uncommon","Choose: gain 30 Gold, or draw 1 Equipment card and keep it only if Common.","Equipment");
            AddSearch("SD018","Hidden Alcove","Discovery","Cave; Ruins; Crypt","Common","Draw 1 Loot card. This Explore does not trigger any additional Encounter unless the card specifically says so.","Loot");
            AddSearch("SD019","Forgotten Armory","Discovery","Fortress; Ruins; Mine","Rare","Draw 2 Equipment cards. Keep 1 Common or Uncommon card and place the other card(s) on the bottom of the deck. If neither qualifies, gain 30 Gold.","Equipment");
            AddSearch("SD020","Shrine Offering","Discovery","Sacred; Shrine; Ruins","Uncommon","Choose one Hero in this Location: recover 2 Health or remove 1 negative Condition.","Recovery");

            AddEncounter("EN001","Goblin Roadblock","Combat","Road; Forest; Plains","1 E001","2 E001","2 E001 + 1 E003","1 E006 + 2 E001","15 Gold; +5 Gold per Elite defeated","",2);
            AddEncounter("EN002","Arrows from the Trees","Ambush","Forest; Road","1 E002","2 E002","2 E002 + 1 E004","1 E006 + 2 E002","20 Gold","Severity 5+: Before setup, each Hero suffers 1 damage.",2);
            AddEncounter("EN003","Scout Patrol","Combat","Any Outdoor","1 E004","2 E004","2 E004 + 1 E001","1 E011 + 2 E004","10 Gold; look at top Encounter card and may bottom it","If any Scout leaves play without being Defeated, the next Travel Encounter Severity Roll gains +1.",2);
            AddEncounter("EN004","Wolf Pack","Beast","Forest; Plains","1 E020","2 E020","1 E021 + 2 E020","2 E021 + 2 E020","1 Loot draw","If a Hero is Defeated, that Hero discards 1 carried Consumable before recovering.",2);
            AddEncounter("EN005","Warg Hunt","Beast","Forest; Road","1 E020","1 E029","1 E029 + 1 E020","2 E029 + 1 E013","25 Gold or 1 Item","");
            AddEncounter("EN006","Trapper's Snare","Hazard","Forest; Road","Active Hero loses 1 Action next turn","Place 1 Snare; then 1 E001","Place 2 Snares; 1 E012","Place 2 Snares; 1 E012 + 1 E004","15 Gold if E012 is defeated","The first Hero affected by a Snare begins Rooted until the end of their turn.");
            AddEncounter("EN007","Burning Cart","Hazard / Combat","Road; Settlement","Extinguish: spend 1 Action; gain 10 Gold","1 Fire Token + 1 E001","2 Fire Tokens + 1 E014","2 Fire Tokens + 1 E014 + 1 E002","25 Gold; draw 1 Item if all Fire Tokens removed","When the Encounter ends, each Hero suffers 1 damage for each Fire Token remaining.");
            AddEncounter("EN008","Fleeing Refugees","Story","Road; Settlement","Gain 1 Item","Choose: give 10 Gold to gain +1 Reputation, or continue","Escort them through 1 E001 + 1 E004 to gain +1 Reputation","Escort them through 1 E006 + 2 E001 to gain +2 Reputation","Reputation as listed; 15 Gold if escort succeeds","If the Heroes accept the escort and fail to complete it, lose 1 Reputation.");
            AddEncounter("EN009","Abandoned Goblin Camp","Discovery","Forest; Hills","Gain 10 Gold","Draw 1 Loot card","Draw 1 Loot and 1 Item","Draw 1 Loot and 1 Equipment; then spawn 1 E011","As listed","Severity 7+: The spawned Saboteur gains +1 Attack during its first activation.");
            AddEncounter("EN010","Broken Bridge","Hazard","River; Road","Cross safely","Each Hero: spend 1 extra Action next turn or suffer 1 damage","Choose: lose 15 Gold in supplies or each Hero suffers 1 damage","Spawn 1 E004 + 1 E002 while Heroes cross; Heroes begin Slowed","","" );
            AddEncounter("EN011","Merchant Under Guard","Merchant / Combat","Road; Settlement","May buy one Common Equipment at -10 Gold","1 E001 threatens merchant; defeat it to trade normally","2 E001 + 1 E004; defeat them to buy one Common/Uncommon Equipment at -20 Gold","1 E006 + 2 E001; defeat them to buy one Equipment at -25 Gold","Merchant access and discounts","Severity 3+: Merchant benefits are gained only if all listed Enemies are Defeated.");
            AddEncounter("EN012","Spider Webs","Beast / Hazard","Forest; Cavern","Clear webs; no combat","1 E022","2 E022","1 E023 + 2 E022","1 Loot draw; +1 Item on 5+","The first Hero begins Rooted until 1 Action is spent to clear the webs.");
            AddEncounter("EN013","Boar Charge","Beast","Forest; Plains","1 E024","1 E024 + each Hero may move 1 Location after encounter","2 E024","2 E024 + 1 E020","1 Item or 20 Gold","Severity 5+: Before combat begins, the first Hero in activation order suffers 1 damage.");
            AddEncounter("EN014","Bear Territory","Beast","Forest; Hills; Cavern","Signs only; may avoid encounter","1 E025","1 E025 with +2 Health","1 E025 + 1 E020","30 Gold or 1 Loot draw","");
            AddEncounter("EN015","Goblin Toll Collectors","Choice / Combat","Road","Pay 5 Gold or fight 1 E001","Pay 10 Gold or fight 2 E001","Pay 15 Gold or fight 1 E006 + 1 E001","No payment accepted: fight 1 E006 + 2 E003","If fought, gain the demanded Gold instead","");
            AddEncounter("EN016","War Drums in the Distance","Ongoing","Any Outdoor","No effect","Next Goblin Encounter gains +1 Movement first round","Next Goblin Encounter adds 1 E008","Immediately resolve 1 E008 + 2 E001","If immediate combat is won, gain 20 Gold","The listed ongoing effect remains until the next Goblin Encounter resolves.");
            AddEncounter("EN017","Saboteur's Cache","Discovery / Combat","Ruins; Road; Forest","Gain 1 Item","Gain 20 Gold","Gain 1 Equipment; then 1 E011 appears","Gain 1 Equipment; 1 E011 + 1 E012 appear","Rewards as listed","If both Elite Goblins leave play without being Defeated, discard the Equipment gained from this Encounter.");
            AddEncounter("EN018","Shamanic Totem","Hazard / Combat","Forest; Swamp; Hills","Destroy to gain 10 Gold","1 E005","1 E005 + 2 E001","1 E017 + 1 E005 + 1 E001","25 Gold; remove Weakened from all Heroes","While the Totem remains, the first Hero Attack each Round suffers -1 Attack.");
            AddEncounter("EN019","Lost Child","Story","Forest; Road; Settlement","Return child: +1 Reputation","Search: spend 1 Action next quest to gain +1 Reputation and 10 Gold","1 E020 threatens child; defeat it for +1 Reputation","1 E021 + 1 E020; rescue for +2 Reputation","Reputation and Gold as listed","If the rescue combat is lost or the child is not rescued, lose 1 Reputation.");
            AddEncounter("EN020","Forest Shrine","Discovery","Forest; Sacred","Recover 1 Health each","Choose one Hero to remove a Condition","All Heroes recover 2 Health","All Heroes recover 2 Health and gain Inspired until end of first round of next quest","Healing as listed","");
            AddEncounter("EN021","Flooded Crossing","Hazard","River; Swamp; Road","Cross safely","Each Hero rolls 1d6; on 1 suffers 1 damage","Each Hero suffers 1 damage unless they discard 1 Item total as rope/supplies","1 E027 appears during crossing; all Heroes begin Slowed","","Severity 7+: If E027 is not Defeated by the end of Round 1, draw another Travel Encounter after this one resolves.");
            AddEncounter("EN022","Watchtower Patrol","Combat","Road; Ruins; Hills","1 E004","1 E002 + 1 E004","1 E006 + 1 E002","1 E006 + 2 E002 + 1 E004","25 Gold; look at top 2 Encounter cards and reorder them","");
            AddEncounter("EN023","Wolf Rider Sweep","Combat","Road; Plains; Forest","1 E013 with -1 Health","1 E013","1 E013 + 1 E004","2 E013 + 1 E004","30 Gold","Severity 5+: Enemies activate first unless at least one Hero is Hidden.");
            AddEncounter("EN024","Firestarter Raid","Combat / Hazard","Settlement; Road; Forest","1 E001","1 E014 + 1 Fire Token","1 E014 + 1 E001 + 2 Fire Tokens","1 E014 + 1 E006 + 2 Fire Tokens","30 Gold; +1 Reputation if all Fire Tokens removed","When the Encounter ends, lose 5 Gold in supplies for each Fire Token remaining.");
            AddEncounter("EN025","Beastmaster Hunt","Combat","Forest; Road","1 E020 + 1 E001","1 E007 + 1 E020","1 E007 + 1 E021 + 1 E001","1 E007 + 1 E029 + 2 E001","35 Gold; 1 Loot draw","Beasts gain +1 Movement during Round 1.");
            AddEncounter("EN026","Chieftain's Scouts","Combat / Ongoing","Any Outdoor","1 E004","2 E004","1 E011 + 1 E004","1 E018 + 2 E004","20 Gold; cancel any pending +1 modifier to next Travel Encounter","If any Scout leaves play without being Defeated, the next Travel Encounter Severity Roll gains +1.");
            AddEncounter("EN027","Supply Wagon Wreck","Discovery","Road; Plains","Gain 10 Gold","Gain 1 Item","Gain 1 Item + 20 Gold","Gain 1 Equipment, then draw another Encounter and resolve it at 3-4 severity","Rewards as listed","Severity 7+: The additional Encounter must be resolved.");
            AddEncounter("EN028","Hidden Trail","Discovery","Forest; Hills","No effect","Look at top 2 Encounter cards; bottom one","Look at top 3 Encounter cards; reorder them","Skip the next Travel Encounter this chapter, then discard this effect","Information / travel control","");
            AddEncounter("EN029","Traveling Merchant","Merchant","Road; Settlement","Buy/sell Common Equipment","Buy/sell Common or Uncommon Equipment","Buy/sell up to Rare Equipment; one Item is free","Buy/sell any non-Unique Equipment; one purchase costs 25 Gold less","Merchant access","");
            AddEncounter("EN030","Quiet Road","Respite","Any Outdoor","Recover 1 Health each","One Hero may Rest without spending an Action","All Heroes recover 1 Health and one Hero removes a Condition","Calm before the storm: all Heroes recover 2 Health, then next Travel Encounter roll gains +1","Recovery as listed","Severity 7+: The next Travel Encounter Severity Roll gains +1.",2);

            AddEnemy("E001","Goblin Raider",6,2,1);
            AddEnemy("E002","Goblin Archer",5,2,0);
            AddEnemy("E003","Goblin Spearman",7,2,2);
            AddEnemy("E004","Goblin Scout",4,2,0);
            AddEnemy("E005","Goblin Shaman",5,1,0);
            AddEnemy("E006","Goblin Captain",10,3,2,true);
            AddEnemy("E007","Goblin Beastmaster",8,3,1);
            AddEnemy("E008","Goblin War Drummer",7,2,1);
            AddEnemy("E010","Goblin War Chief",20,5,3,true);
            AddEnemy("E011","Goblin Saboteur",6,3,0);
            AddEnemy("E012","Goblin Trapper",7,2,0);
            AddEnemy("E013","Goblin Wolf Rider",10,4,1);
            AddEnemy("E014","Goblin Firestarter",6,3,0);
            AddEnemy("E015","Goblin Champion",14,4,2);
            AddEnemy("E016","Goblin Siege Engineer",9,3,1);
            AddEnemy("E017","Goblin Witch Doctor",8,2,0);
            AddEnemy("E018","Goblin Chieftain's Bodyguard",16,4,3);
            AddEnemy("E019","Goblin High Shaman",18,3,2,true);
            AddEnemy("E032","The Goblin King",26,5,3,true);
            AddEnemy("E020","Wolf",7,3,0);
            AddEnemy("E021","Dire Wolf",14,5,1);
            AddEnemy("E022","Giant Spider",8,3,1);
            AddEnemy("E023","Spider Matriarch",22,4,2,true);
            AddEnemy("E024","Giant Boar",10,3,1);
            AddEnemy("E025","Cave Bear",18,4,2);
            AddEnemy("E026","Giant Bat",5,2,0);
            AddEnemy("E027","Giant Snake",10,3,1);
            AddEnemy("E028","Owlbear",18,5,2);
            AddEnemy("E029","Warg",18,5,2);
            ImportFullCatalog0850();
        }

        private static void ImportFullCatalog0850()
        {
            FullContentCatalog0850.EnsureLoaded();
            foreach(var c in FullContentCatalog0850.Root.searchCards)
            {
                if(c==null||string.IsNullOrWhiteSpace(c.id))continue;
                SearchCards[c.id]=new SearchCardDefinition
                {
                    Id=c.id,Name=c.name,Category=c.category,Terrain=c.terrain,Rarity=c.rarity,Trigger=c.trigger,
                    Effect=c.effect,FollowUp=c.followUp,FlavorText=c.flavorText,Status=c.status,Copies=Math.Max(1,c.copies)
                };
            }
            foreach(var c in FullContentCatalog0850.Root.encounters)
            {
                if(c==null||string.IsNullOrWhiteSpace(c.id))continue;
                EncounterCards[c.id]=new EncounterCardDefinition
                {
                    Id=c.id,Name=c.name,EncounterType=c.encounterType,Terrain=c.terrain,Trigger=c.trigger,
                    Severity12=c.severity12,Severity34=c.severity34,Severity56=c.severity56,Severity7Plus=c.severity7Plus,
                    Reward=c.reward,Failure=c.failure,WorldEventScaling=c.worldEventScaling,FlavorText=c.flavorText,Status=c.status,Copies=Math.Max(1,c.copies)
                };
            }
            foreach(var e in FullContentCatalog0850.Root.enemies)
            {
                if(e==null||string.IsNullOrWhiteSpace(e.id))continue;
                bool existingBoss=EnemyTemplates.TryGetValue(e.id,out var old)&&old.Boss;
                bool boss=existingBoss
                    ||(e.role??string.Empty).IndexOf("Boss",StringComparison.OrdinalIgnoreCase)>=0
                    ||(e.difficulty??string.Empty).IndexOf("Boss",StringComparison.OrdinalIgnoreCase)>=0
                    ||(e.recommendedUses??string.Empty).IndexOf("Boss",StringComparison.OrdinalIgnoreCase)>=0;
                EnemyTemplates[e.id]=new EncounterEnemyTemplate
                {
                    Id=e.id,Name=e.name,Health=Math.Max(1,e.health),Attack=Math.Max(0,e.attack),Armor=Math.Max(0,e.armor),
                    Movement=Math.Max(0,e.movement),ThreatRating=Math.Max(0,e.threatRating),EnemyType=e.enemyType,Role=e.role,
                    BehaviorProfile=e.behaviorProfile,Difficulty=e.difficulty,Traits=e.traits,Keywords=e.keywords,Range=e.range,
                    Abilities=e.abilities,Behavior=e.behavior,Status=e.status,Boss=boss
                };
            }
        }

        private static void AddSearch(string id,string name,string category,string terrain,string rarity,string effect,string followUp,int copies=1)
            => SearchCards[id]=new SearchCardDefinition{Id=id,Name=name,Category=category,Terrain=terrain,Rarity=rarity,Effect=effect,FollowUp=followUp,Copies=copies};

        private static void AddEncounter(string id,string name,string type,string terrain,string s12,string s34,string s56,string s7,string reward,string failure,int copies=1)
            => EncounterCards[id]=new EncounterCardDefinition{Id=id,Name=name,EncounterType=type,Terrain=terrain,Severity12=s12,Severity34=s34,Severity56=s56,Severity7Plus=s7,Reward=reward,Failure=failure,Copies=copies};

        private static void AddEnemy(string id,string name,int hp,int attack,int armor,bool boss=false)
            => EnemyTemplates[id]=new EncounterEnemyTemplate{Id=id,Name=name,Health=hp,Attack=attack,Armor=armor,Boss=boss};

        public static bool MatchesLocation(LocationState location,string terrainSpec)
        {
            if(location==null||string.IsNullOrWhiteSpace(terrainSpec))return false;
            foreach(var raw in terrainSpec.Split(';'))
            {
                var token=raw.Trim();
                if(string.Equals(token,"Any",StringComparison.OrdinalIgnoreCase))return true;
                if(string.Equals(token,"Any Outdoor",StringComparison.OrdinalIgnoreCase)&&location.HasPrintedTag("Outdoor"))return true;
                if(location.HasPrintedTag(token))return true;
            }
            return false;
        }

        public static List<string> BuildSearchDeck()
        {
            var list=new List<string>();
            foreach(var c in SearchCards.Values.OrderBy(c=>c.Id))for(int i=0;i<c.Copies;i++)list.Add(c.Id);
            return list;
        }

        public static List<string> BuildEncounterDeck()
        {
            var list=new List<string>();
            // Campaign I/Stonebrook currently owns EN001-EN030. Later regional Encounter rows are fully
            // authored in the catalog but do not enter this live Travel deck until their campaign is active.
            foreach(var c in EncounterCards.Values.Where(c=>FullContentCatalog0850.IsCampaignIEncounterId(c.Id)).OrderBy(c=>c.Id))
                for(int i=0;i<c.Copies;i++)list.Add(c.Id);
            return list;
        }
    }
}
