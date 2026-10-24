using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    [Serializable]
    public sealed class QuestGoalState0856
    {
        public string Key;
        public string Label;
        public string Kind; // Interact, Search, Enemy, Reach, Clear, Loot, Hold, Choice
        public int Current;
        public int Target=1;
        public bool Complete;
        public List<string> LocationIds=new();
        public List<string> EnemyTemplateIds=new();
        public List<string> LootIds=new();
    }

    [Serializable]
    public sealed class FactionReputationState0856
    {
        public string Faction;
        public int Score;
    }

    [Serializable]
    public sealed class QuestRevealSpawnState0856
    {
        public string EnemyTemplateId;
        public string TriggerLocationId;
        public string SpawnLocationId;
        public int Count=1;
        public bool Resolved;
    }

    [Serializable]
    public sealed class QuestRuntimeState0856
    {
        public string QuestId;
        public string QuestName;
        public bool SetupComplete;
        public string PressureName;
        public int Pressure;
        public int PressureLimit;
        public bool PressureIncrementsAtEnemyPhase;
        public bool PressureEveryOtherEnemyPhaseSolo;
        public bool FinalChoiceResolved;
        public string FinalChoice;
        public List<string> FinalChoiceOptions=new();
        public List<string> DelayedEnemyTemplateIds=new();
        public List<QuestRevealSpawnState0856> RevealSpawns=new();
        public List<QuestGoalState0856> Goals=new();
    }

    /// <summary>
    /// 0.8.56 — authored Quest runtime for Q013-Q072.
    /// Q001-Q012 remain owned by the verified 0.8.29-0.8.32 specialized runtimes.
    /// This layer turns later authored Quest rows into executable scenario state without
    /// inventing campaign rules that are not present in the authored catalog.
    /// </summary>
    public sealed partial class AscendanceGameManager
    {
        public QuestRuntimeState0856 QuestRuntime0856 { get; private set; }
        private readonly List<string> questCampaignFlags0856=new();
        private readonly List<FactionReputationState0856> factionReputation0856=new();

        public IReadOnlyList<string> QuestCampaignFlags0856 => questCampaignFlags0856;
        public IReadOnlyList<FactionReputationState0856> FactionReputation0856 => factionReputation0856;
        public bool IsGenericQuestRuntime0856 => QuestNumber0856(CurrentQuestId0829)>=13;
        public bool IsQuestExecutable0856(string questId)
        {
            int n=QuestNumber0856(questId);
            return n>=1&&n<=72&&FullContentCatalog0850.TryGetQuest(questId,out _);
        }

        private static int QuestNumber0856(string id)
        {
            var m=Regex.Match(id??string.Empty,@"^Q(\d{3})$",RegexOptions.IgnoreCase);
            return m.Success&&int.TryParse(m.Groups[1].Value,out var n)?n:0;
        }

        private static QuestDefinition0829 ConvertQuestDefinition0856(FullQuest0850 q)
            => q==null?null:new QuestDefinition0829{Id=q.id,Name=q.name,Story=q.story,Victory=q.winningConditions,Failure=q.losingConditions,Reward=q.rewards};

        public QuestDefinition0829 QuestDefinition0856(string questId)
        {
            if(QuestContent0829.Definitions.TryGetValue(questId??string.Empty,out var d))return d;
            return FullContentCatalog0850.TryGetQuest(questId,out var q)?ConvertQuestDefinition0856(q):null;
        }

        private void EnsureQuestRuntimeLocations0856(string questId)
        {
            if(QuestNumber0856(questId)<13)return;
            if(!FullContentCatalog0850.TryGetQuest(questId,out var q))return;
            var ids=FullContentCatalog0850.QuestLocationPoolIds(questId);
            ids.Add(FullContentCatalog0850.StartingLocationId(questId));
            var ordered=ids.Where(x=>!string.IsNullOrWhiteSpace(x)).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
            int cols=Mathf.Max(3,Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1,ordered.Count))));
            for(int i=0;i<ordered.Count;i++)
            {
                string id=ordered[i];
                if(Locations.ContainsKey(id))continue;
                if(!FullContentCatalog0850.TryGetLocation(id,out var authored))continue;
                int row=i/cols,col=i%cols;
                var l=new LocationState
                {
                    Id=id,AuthoredLocationId=id,Name=authored.name,Terrain=authored.terrain,Traits=authored.traits,Keywords=authored.keywords,
                    RevealEffect=authored.revealEffect,FirstVisitEffect=authored.firstVisitEffect,OngoingEffect=authored.ongoingEffect,
                    Position=new Vector3(-24f+col*8f,0f,-8f+row*8f),Revealed=false,RevealResolved=false
                };
                Locations[id]=l;
            }
            foreach(var id in ordered)
            {
                if(!Locations.TryGetValue(id,out var l)||!FullContentCatalog0850.TryGetLocation(id,out var authored))continue;
                foreach(Match m in Regex.Matches(authored.canConnectTo??string.Empty,@"L\d{3}",RegexOptions.IgnoreCase))
                {
                    string other=m.Value.ToUpperInvariant();
                    if(Locations.ContainsKey(other)&&!l.Connections.Contains(other,StringComparer.OrdinalIgnoreCase))l.Connections.Add(other);
                }
            }
            // Authored rows occasionally omit a scenario-local edge. Keep the runtime navigable by
            // connecting isolated quest nodes in deterministic ID order, without altering metadata.
            for(int i=0;i<ordered.Count;i++)
            {
                if(!Locations.TryGetValue(ordered[i],out var l)||l.Connections.Any(x=>ids.Contains(x)))continue;
                if(i>0)AddQuestConnection0856(l.Id,ordered[i-1]);
                else if(i+1<ordered.Count)AddQuestConnection0856(l.Id,ordered[i+1]);
            }
            UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>()?.EnsureRuntimeLocationSpawns0856();
        }

        private void AddQuestConnection0856(string a,string b)
        {
            if(!Locations.TryGetValue(a,out var la)||!Locations.TryGetValue(b,out var lb))return;
            if(!la.Connections.Contains(b,StringComparer.OrdinalIgnoreCase))la.Connections.Add(b);
            if(!lb.Connections.Contains(a,StringComparer.OrdinalIgnoreCase))lb.Connections.Add(a);
        }

        private void InitializeQuestRuntime0856(string questId)
        {
            QuestRuntime0856=null;
            if(QuestNumber0856(questId)<13||!FullContentCatalog0850.TryGetQuest(questId,out var q))return;
            EnsureQuestRuntimeLocations0856(questId);
            var s=new QuestRuntimeState0856{QuestId=q.id,QuestName=q.name};
            BuildQuestGoals0856(q,s);
            ConfigurePressure0856(q,s);
            ConfigureFinalChoice0856(q,s);
            QuestRuntime0856=s;
            SetupGenericQuestEnemies0856(q,s);
            s.SetupComplete=true;
            AddLog($"QUEST 0.8.56 — {q.id} {q.name}: {s.Goals.Count} authored objective goal(s) active"+(s.PressureLimit>0?$" • {s.PressureName} {s.Pressure}/{s.PressureLimit}":string.Empty)+".");
        }

        private void BuildQuestGoals0856(FullQuest0850 q,QuestRuntimeState0856 s)
        {
            string win=q.winningConditions??string.Empty;
            int serial=0;
            void Add(string kind,string label,int target=1,IEnumerable<string> locs=null,IEnumerable<string> enemies=null,IEnumerable<string> loot=null)
            {
                var g=new QuestGoalState0856{Key=$"G{++serial:00}",Kind=kind,Label=label,Target=Mathf.Max(1,target)};
                if(locs!=null)g.LocationIds=locs.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if(enemies!=null)g.EnemyTemplateIds=enemies.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if(loot!=null)g.LootIds=loot.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                s.Goals.Add(g);
            }

            // Explicit numeric resource/evidence clauses.
            var ev=Regex.Match(win,@"(?:gain|collect)\s+(?:at\s+least\s+)?(\d+)\s+Evidence",RegexOptions.IgnoreCase);
            if(ev.Success)Add((q.questRules??string.Empty).IndexOf("Search",StringComparison.OrdinalIgnoreCase)>=0?"Search":"Interact","Gain Evidence",int.Parse(ev.Groups[1].Value));
            if(Regex.IsMatch(win,@"\bSeal\s+has\s+broken\b",RegexOptions.IgnoreCase))Add("Interact","Break the Seal",1,LocationIdsNearText0856(q.questRules,"Seal breaks|Seal Track|DISRUPT RITUAL"));

            string[] nouns={"Survivor","Cult Suspect","Supply Cache","Ritualist","Waystone","Seal Lock","Ritual Focus","District","Crown Ward","Trapped Crew","Survey Cache","Relay Anchor","Waymark Beacon","Memory Seal","Chain Lock","Assembly Line","Tone Token","Forge Node","facilit(?:y|ies)","Mountain Ward","Blight Node","Cure Ingredient","Root Vein","Spirit Waystone","Spirit Anchor","Thorn Ward","Path Marker","Binding Totem","Feeding Root","Site","Bloom Root","Tide-?Seal","Supply Crate","Bell Lock","Breakwater Section","Memory Anchor","Choir Bell","Lens Segment","Chain Anchor","Deep Chain","Road Ward","Wagon supply cache","Solar Seal","Vault Lock","Echo Shrine","Mirror Ward","Solar Crucible","Dawn Prism","Eclipse Anchor","Solar Crown Ward"};
            foreach(var noun in nouns)
            {
                var rx=new Regex($@"(?:destroy|restore|resolve|activate|remove|break|cleanse|save|rescue|recover|expose|secure|hold)\s+(?:all\s+|remaining\s+|placed\s+)?(?:at\s+least\s+)?(?:(\d+)\s+(?:of\s+the\s+\d+\s+)?)?(?:both\s+)?[^.;]*?{noun}s?",RegexOptions.IgnoreCase);
                var m=rx.Match(win);if(!m.Success)continue;
                int target=1;
                if(m.Groups[1].Success)int.TryParse(m.Groups[1].Value,out target);
                else if(m.Value.IndexOf("both",StringComparison.OrdinalIgnoreCase)>=0)target=2;
                else target=InferObjectiveCount0856(q,noun);
                string label=m.Value.Trim().TrimEnd(',');
                // Specific noun patterns (for example Spirit Waystone / Wagon supply cache / Solar Crown Ward)
                // can overlap a broader noun pattern. Treat the authored objective sentence as one goal, not two.
                if(s.Goals.Any(g=>g.Kind=="Interact"&&string.Equals(g.Label,label,StringComparison.OrdinalIgnoreCase)))continue;
                Add("Interact",label,target,LocationIdsNearText0856(q.startingEnemiesTokens+" "+q.questRules,noun));
            }

            // Enemy-defeat goals: prefer explicit E### IDs, then exact authored Enemy names appearing in Victory text.
            var enemyIds=Regex.Matches(win,@"E\d{3}",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value.ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if(enemyIds.Count==0&&win.IndexOf("defeat",StringComparison.OrdinalIgnoreCase)>=0)
                enemyIds=SearchEncounterContent0820.EnemyTemplates.Values.Where(e=>!string.IsNullOrWhiteSpace(e.Name)&&win.IndexOf(e.Name,StringComparison.OrdinalIgnoreCase)>=0).Select(e=>e.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach(var eid in enemyIds)Add("Enemy",$"Defeat {EnemyLabel0856(eid)}",1,null,new[]{eid});

            // Reach / escort / escape destinations.
            if(Regex.IsMatch(win,@"\b(reach|escort|escape|escapes)\b",RegexOptions.IgnoreCase))
            {
                var locs=Regex.Matches(win,@"L\d{3}",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value.ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if(locs.Count>0)Add("Reach","Reach authored destination",1,locs);
                else if(win.IndexOf("Urban Location",StringComparison.OrdinalIgnoreCase)>=0)Add("Reach","Escape to an Urban Location",1,new[]{"TAG:Urban"});
            }

            // Clearing a destination is separate from merely reaching it. Support the authored
            // “there”, “in L###”, and “at the last <objective>” phrasings used across Campaigns II–VI.
            if(Regex.IsMatch(win,@"defeat\s+(?:all\s+)?enemies\s+(?:there|in\s+L\d{3}|at\s+the\s+last)",RegexOptions.IgnoreCase))
            {
                var clearMatch=Regex.Match(win,@"defeat\s+(?:all\s+)?enemies\s+(?:there|in\s+(L\d{3})|at\s+the\s+last[^.;]*)",RegexOptions.IgnoreCase);
                var locs=new List<string>();
                if(clearMatch.Success&&clearMatch.Groups[1].Success)locs.Add(clearMatch.Groups[1].Value.ToUpperInvariant());
                if(locs.Count==0)
                {
                    // “there” refers to the nearest explicit destination in the victory text.
                    var before=clearMatch.Success?win.Substring(0,clearMatch.Index):win;
                    var ids=Regex.Matches(before,@"L\d{3}",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value.ToUpperInvariant()).ToList();
                    if(ids.Count>0)locs.Add(ids.Last());
                }
                Add("Clear","Clear the authored objective Location",1,locs);
            }

            // Required Loot / Campaign Quest Items may be authored by LT### or by printed name.
            // Name matching deliberately requires the victory text to reference the item (or a two-word
            // identifying phrase whose full authored name is supplied by the Quest rules), preventing
            // incidental setup/reward items from becoming extra victory requirements.
            var requiredLoot=new HashSet<string>(Regex.Matches(win,@"LT\d{3}",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value.ToUpperInvariant()),StringComparer.OrdinalIgnoreCase);
            string ruleText=q.questRules??string.Empty;
            var named=RewardContent0826.Loot.Values.Where(d=>!string.IsNullOrWhiteSpace(d.Name)).OrderByDescending(d=>d.Name.Length).ToList();
            var matchedLootNames=new List<string>();
            foreach(var d in named)
            {
                bool matched=win.IndexOf(d.Name,StringComparison.OrdinalIgnoreCase)>=0;
                if(!matched&&ruleText.IndexOf(d.Name,StringComparison.OrdinalIgnoreCase)>=0)
                {
                    var words=Regex.Matches(d.Name,@"[A-Za-z0-9]+",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value).Where(w=>w.Length>2&&!string.Equals(w,"the",StringComparison.OrdinalIgnoreCase)&&!string.Equals(w,"and",StringComparison.OrdinalIgnoreCase)&&!string.Equals(w,"of",StringComparison.OrdinalIgnoreCase)).ToList();
                    matched=words.Count>=2&&win.IndexOf(words[0]+" "+words[1],StringComparison.OrdinalIgnoreCase)>=0;
                }
                if(!matched||matchedLootNames.Any(longer=>longer.IndexOf(d.Name,StringComparison.OrdinalIgnoreCase)>=0))continue;
                requiredLoot.Add(d.Id);matchedLootNames.Add(d.Name);
            }
            foreach(var loot in requiredLoot)Add("Loot",$"Recover {RewardContent0826.LootLabel(loot)}",1,null,null,new[]{loot});

            if(Regex.IsMatch(win,@"secure\s+the\s+Crownshard\s+Cache",RegexOptions.IgnoreCase))Add("Interact","Secure the Crownshard Cache",1);

            var hold=Regex.Match(win,@"through\s+end\s+Round\s+(\d+)",RegexOptions.IgnoreCase);
            if(hold.Success)Add("Hold",$"Hold through end Round {hold.Groups[1].Value}",int.Parse(hold.Groups[1].Value));

            if(Regex.IsMatch(win,@"resolve\s+.+?(outcome|choice)",RegexOptions.IgnoreCase))Add("Choice","Resolve the authored final choice",1);

            if(s.Goals.Count==0)Add("Interact",win.Length>0?win:"Resolve the Quest objective",1);
        }

        private int InferObjectiveCount0856(FullQuest0850 q,string nounPattern)
        {
            string text=(q.startingEnemiesTokens??string.Empty)+" "+(q.questRules??string.Empty)+" "+(q.winningConditions??string.Empty);
            var m=Regex.Match(text,$@"(?<![A-Za-z0-9])(\d+)\s+[^.;]{{0,28}}{nounPattern}s?",RegexOptions.IgnoreCase);
            if(m.Success&&int.TryParse(m.Groups[1].Value,out var n))return Mathf.Clamp(n,1,8);
            if(text.IndexOf("both",StringComparison.OrdinalIgnoreCase)>=0)return 2;
            return 1;
        }

        private IEnumerable<string> LocationIdsNearText0856(string text,string nounPattern)
        {
            if(string.IsNullOrWhiteSpace(text))return Array.Empty<string>();
            var parts=Regex.Split(text,@"[.;]");
            foreach(var p in parts)
                if(Regex.IsMatch(p,nounPattern,RegexOptions.IgnoreCase))
                {
                    var ids=Regex.Matches(p,@"L\d{3}",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value.ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if(ids.Count>0)return ids;
                }
            return Array.Empty<string>();
        }

        private string EnemyLabel0856(string id)=>SearchEncounterContent0820.EnemyTemplates.TryGetValue(id??string.Empty,out var e)?$"{id} {e.Name}":id;

        private void ConfigurePressure0856(FullQuest0850 q,QuestRuntimeState0856 s)
        {
            string loss=q.losingConditions??string.Empty;
            string rules=q.questRules??string.Empty;
            string[] names={"Murder Track","Suspicion","Witness Harm","Harm","Collapse Track","Collapse","Crisis Track","Crisis","Storm","Pursuit","Descent","Ascent","Execution Track"};
            foreach(var name in names)
            {
                var m=Regex.Match(loss,$@"{Regex.Escape(name)}[^0-9]{{0,18}}(\d+)",RegexOptions.IgnoreCase);
                if(!m.Success)m=Regex.Match(loss,$@"(\d+)[^.;]{{0,18}}{Regex.Escape(name)}",RegexOptions.IgnoreCase);
                if(!m.Success)continue;
                s.PressureName=name;s.PressureLimit=int.Parse(m.Groups[1].Value);break;
            }
            s.PressureIncrementsAtEnemyPhase=s.PressureLimit>0&&Regex.IsMatch(rules,@"(end|at end).*Enemy Phase.*(\+\s*1|increase|reduce)",RegexOptions.IgnoreCase);
            s.PressureEveryOtherEnemyPhaseSolo=(q.heroCountScaling??string.Empty).IndexOf("every other Enemy Phase",StringComparison.OrdinalIgnoreCase)>=0;
        }

        private void ConfigureFinalChoice0856(FullQuest0850 q,QuestRuntimeState0856 s)
        {
            var m=Regex.Match(q.questRules??string.Empty,@"(?:after victory|when the last[^.]*|HEART'S CHOICE[^:]*:)[^.]*?choose(?: and record)?\s+([^\.]+)",RegexOptions.IgnoreCase);
            if(!m.Success)return;
            var options=Regex.Split(m.Groups[1].Value,@"\s*,\s*|\s+or\s+|/")
                .Select(x=>Regex.Replace(Regex.Replace(x.Trim(),@"^(?:and|or)\s+",string.Empty,RegexOptions.IgnoreCase),@"\s+after\b.*$",string.Empty,RegexOptions.IgnoreCase).Trim())
                .Where(x=>x.Length>0&&x.Length<80).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if(options.Count>=2)s.FinalChoiceOptions=options;
        }

        private void SetupGenericQuestEnemies0856(FullQuest0850 q,QuestRuntimeState0856 s)
        {
            string start=FullContentCatalog0850.StartingLocationId(q.id);
            string startRuntime=RuntimeLocationIdForAuthored0850(start)??start;
            string priorLocation=null;
            foreach(var segment in Regex.Split(q.startingEnemiesTokens??string.Empty,@"[.;]").Where(x=>!string.IsNullOrWhiteSpace(x)))
            {
                var anyLoc=Regex.Match(segment,@"L\d{3}",RegexOptions.IgnoreCase);
                if(anyLoc.Success)priorLocation=anyLoc.Value.ToUpperInvariant();
                var refs=Regex.Matches(segment,@"(E\d{3})(?:\s*[×x]\s*(\d+))?",RegexOptions.IgnoreCase).Cast<Match>().ToList();
                if(refs.Count==0)continue;
                bool revealTriggered=Regex.IsMatch(segment,@"\bwhen\b[^.;]*(?:revealed|accessed)|\bon\s+reveal\b",RegexOptions.IgnoreCase);
                bool delayed=!revealTriggered&&Regex.IsMatch(segment,@"\b(when|after|once|until)\b|out of play|begins? out|\bout\b",RegexOptions.IgnoreCase);
                string authoredTarget=anyLoc.Success?anyLoc.Value.ToUpperInvariant():(revealTriggered&&!string.IsNullOrWhiteSpace(priorLocation)?priorLocation:start);
                string target=RuntimeLocationIdForAuthored0850(authoredTarget)??authoredTarget;
                foreach(var r in refs)
                {
                    string eid=r.Groups[1].Value.ToUpperInvariant();int count=r.Groups[2].Success&&int.TryParse(r.Groups[2].Value,out var n)?Mathf.Clamp(n,1,6):1;
                    if(revealTriggered)
                    {
                        s.RevealSpawns.Add(new QuestRevealSpawnState0856{EnemyTemplateId=eid,TriggerLocationId=authoredTarget,SpawnLocationId=authoredTarget,Count=count});
                        continue;
                    }
                    if(delayed){for(int i=0;i<count;i++)s.DelayedEnemyTemplateIds.Add(eid);continue;}
                    for(int i=0;i<count;i++)if(Locations.ContainsKey(target))SpawnQuestEnemy0829(eid,target,"0.8.56 authored setup");
                }
            }

            // Some objective enemies are introduced only in Quest Rules (for example Q013 E036
            // after the second Survivor, or Q018 E045 when the Seal breaks). Keep those enemies
            // out of play until the non-enemy authored prerequisites have advanced.
            foreach(var g in s.Goals.Where(x=>x.Kind=="Enemy"))
                foreach(var eid in g.EnemyTemplateIds)
                {
                    if(s.DelayedEnemyTemplateIds.Contains(eid,StringComparer.OrdinalIgnoreCase)||s.RevealSpawns.Any(x=>string.Equals(x.EnemyTemplateId,eid,StringComparison.OrdinalIgnoreCase)))continue;
                    var sentence=Regex.Split(q.questRules??string.Empty,@"[.;]").FirstOrDefault(x=>x.IndexOf(eid,StringComparison.OrdinalIgnoreCase)>=0&&Regex.IsMatch(x,@"\b(spawn|place|replace)\b",RegexOptions.IgnoreCase)&&Regex.IsMatch(x,@"\b(when|after|once|until|immediately after)\b",RegexOptions.IgnoreCase));
                    if(!string.IsNullOrWhiteSpace(sentence))s.DelayedEnemyTemplateIds.Add(eid);
                }
            EnsureRequiredEnemyGoalsSpawned0856(false);
        }

        private void ResolveQuestRevealSpawns0856(LocationState location)
        {
            if(QuestRuntime0856==null||location==null||QuestRuntime0856.RevealSpawns==null)return;
            foreach(var x in QuestRuntime0856.RevealSpawns.Where(x=>x!=null&&!x.Resolved&&LocationMatchesGoal0856(location.Id,x.TriggerLocationId)).ToList())
            {
                string target=RuntimeLocationIdForAuthored0850(x.SpawnLocationId)??x.SpawnLocationId;
                if(!Locations.ContainsKey(target))target=location.Id;
                for(int i=0;i<Mathf.Max(1,x.Count);i++)SpawnQuestEnemy0829(x.EnemyTemplateId,target,"0.8.56 authored reveal setup");
                x.Resolved=true;
                AddLog($"QUEST REVEAL — {x.EnemyTemplateId} authored setup resolves at {location.Name}.");
            }
        }

        private void EnsureRequiredEnemyGoalsSpawned0856(bool afterProgress)
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null)return;
            bool prerequisites=QuestRuntime0856.Goals.Where(g=>g.Kind!="Enemy"&&g.Kind!="Reach"&&g.Kind!="Clear"&&g.Kind!="Choice"&&g.Kind!="Hold").All(g=>g.Complete);
            if(afterProgress&&!prerequisites)return;
            string loc=QuestRuntime0856.Goals.SelectMany(g=>g.LocationIds).FirstOrDefault(x=>!x.StartsWith("TAG:",StringComparison.OrdinalIgnoreCase)&&Locations.ContainsKey(x))??(RuntimeLocationIdForAuthored0850(FullContentCatalog0850.StartingLocationId(CurrentQuestId0829))??FullContentCatalog0850.StartingLocationId(CurrentQuestId0829));
            foreach(var g in QuestRuntime0856.Goals.Where(x=>x.Kind=="Enemy"&&!x.Complete))
                foreach(var eid in g.EnemyTemplateIds)
                {
                    bool delayed=QuestRuntime0856.DelayedEnemyTemplateIds.Contains(eid,StringComparer.OrdinalIgnoreCase);
                    bool waitsForReveal=QuestRuntime0856.RevealSpawns!=null&&QuestRuntime0856.RevealSpawns.Any(x=>x!=null&&!x.Resolved&&string.Equals(x.EnemyTemplateId,eid,StringComparison.OrdinalIgnoreCase));
                    if(waitsForReveal)continue;
                    if(delayed&&!prerequisites)continue;
                    bool phaseBossAlive=Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId==CurrentQuestId0829&&e.IsBoss&&!string.Equals(e.TemplateId,eid,StringComparison.OrdinalIgnoreCase));
                    if(delayed&&phaseBossAlive)continue;
                    if(!Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId==CurrentQuestId0829&&e.TemplateId==eid)&&Locations.ContainsKey(loc))
                    {
                        SpawnQuestEnemy0829(eid,loc,delayed?"0.8.56 delayed objective enemy":"0.8.56 objective enemy");
                        QuestRuntime0856.DelayedEnemyTemplateIds.RemoveAll(x=>string.Equals(x,eid,StringComparison.OrdinalIgnoreCase));
                    }
                }
            if(prerequisites&&QuestRuntime0856.DelayedEnemyTemplateIds.Count>0)
            {
                var livingBoss=Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId==CurrentQuestId0829&&e.IsBoss);
                if(!livingBoss)
                {
                    foreach(var eid in QuestRuntime0856.DelayedEnemyTemplateIds.ToList())if(Locations.ContainsKey(loc))SpawnQuestEnemy0829(eid,loc,"0.8.56 delayed authored setup");
                    QuestRuntime0856.DelayedEnemyTemplateIds.Clear();
                }
            }
        }

        private string QuestObjectiveSummary0856()
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null||!FullContentCatalog0850.TryGetQuest(CurrentQuestId0829,out var q))return null;
            var rows=QuestRuntime0856.Goals.Select(g=>$"{(g.Complete?"[OK]":"[ ]")} {g.Label}"+(g.Target>1?$" ({Mathf.Min(g.Current,g.Target)}/{g.Target})":string.Empty));
            string pressure=QuestRuntime0856.PressureLimit>0?$"\n{QuestRuntime0856.PressureName}: {QuestRuntime0856.Pressure}/{QuestRuntime0856.PressureLimit}":string.Empty;
            return $"QUEST {q.id} — {q.name.ToUpperInvariant()}\n\n{string.Join("\n",rows)}{pressure}\n\nAUTHORED FAILURE: {q.losingConditions}";
        }

        private string QuestPrimaryActionLabel0856()
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null)return null;
            var g=QuestRuntime0856.Goals.FirstOrDefault(x=>!x.Complete&&x.Kind=="Interact");return g==null?"Quest Action":ShortGoalLabel0856(g.Label);
        }
        private string QuestSecondaryActionLabel0856()
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null)return null;
            if(CanWorldEventLocationAction0857(SelectedHero))return WorldEventLocationActionLabel0857(SelectedHero);
            return QuestRuntime0856.Goals.Any(g=>!g.Complete&&g.Kind=="Choice")?"Final Choice":"Quest Utility";
        }
        private static string ShortGoalLabel0856(string s)
        {
            if(string.IsNullOrWhiteSpace(s))return "Quest Action";
            string x=Regex.Replace(s,@"^(Destroy|Restore|Resolve|Activate|Remove|Break|Cleanse|Save|Rescue|Recover|Expose|Secure|Hold)\s+",string.Empty,RegexOptions.IgnoreCase);
            return x.Length<=22?x:x.Substring(0,22);
        }

        private bool CanQuestPrimaryAction0856(HeroState hero)
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null||hero==null||!CanUseTurnActions(hero)||GeneralActions(hero)<1)return false;
            var g=QuestRuntime0856.Goals.FirstOrDefault(x=>!x.Complete&&x.Kind=="Interact");
            if(g==null)return false;
            if(g.LocationIds.Count==0)return true;
            return g.LocationIds.Any(id=>LocationMatchesGoal0856(hero.LocationId,id));
        }
        private bool CanQuestSecondaryAction0856(HeroState hero)
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null||hero==null||!CanUseTurnActions(hero)||GeneralActions(hero)<1)return false;
            if(CanWorldEventLocationAction0857(hero))return true;
            return QuestRuntime0856.Goals.Any(g=>!g.Complete&&g.Kind=="Choice")&&QuestRuntime0856.Goals.Where(g=>g.Kind!="Choice").All(g=>g.Complete);
        }
        private bool LocationMatchesGoal0856(string runtimeId,string goalId)
        {
            if(!Locations.TryGetValue(runtimeId??string.Empty,out var l))return false;
            if(goalId.StartsWith("TAG:",StringComparison.OrdinalIgnoreCase))return l.HasPrintedTag(goalId.Substring(4));
            return string.Equals(runtimeId,goalId,StringComparison.OrdinalIgnoreCase)||string.Equals(l.AuthoredLocationId,goalId,StringComparison.OrdinalIgnoreCase);
        }

        private void QuestPrimaryAction0856()
        {
            var hero=SelectedHero;if(!CanQuestPrimaryAction0856(hero)||!RequireHeroTurn(hero,"Quest Objective"))return;
            var g=QuestRuntime0856.Goals.First(x=>!x.Complete&&x.Kind=="Interact");hero.Actions--;AdvanceGoal0856(g,1,$"{hero.DisplayName} resolves {g.Label}");
            if(g.Label.IndexOf("Cult Suspect",StringComparison.OrdinalIgnoreCase)>=0)
            {
                var evidence=QuestRuntime0856.Goals.FirstOrDefault(x=>!x.Complete&&x.Label.IndexOf("Evidence",StringComparison.OrdinalIgnoreCase)>=0);if(evidence!=null)AdvanceGoal0856(evidence,1,"Cult Suspect exposed");
            }
            EnsureRequiredEnemyGoalsSpawned0856(true);EvaluateQuestOutcome0829();StateChanged?.Invoke();
        }
        private void QuestSecondaryAction0856()
        {
            var hero=SelectedHero;if(!CanQuestSecondaryAction0856(hero))return;
            if(CanWorldEventLocationAction0857(hero)){if(!RequireHeroTurn(hero,"World Event Location Action"))return;ResolveWorldEventLocationAction0857(hero);return;}
            if(!RequireHeroTurn(hero,"Final Quest Choice"))return;hero.Actions--;
            var g=QuestRuntime0856.Goals.FirstOrDefault(x=>!x.Complete&&x.Kind=="Choice");if(g==null)return;
            var opts=QuestRuntime0856.FinalChoiceOptions.Count>0?QuestRuntime0856.FinalChoiceOptions:new List<string>{"Resolve authored outcome"};
            BeginRewardChoice0827(CurrentQuestId0829,CurrentQuestName0829,hero,"QuestFinalChoice0856","Choose the authored final Quest outcome.",opts);
        }

        private void AdvanceGoal0856(QuestGoalState0856 g,int amount,string source)
        {
            if(g==null||g.Complete)return;g.Current=Mathf.Min(g.Target,g.Current+Mathf.Max(0,amount));g.Complete=g.Current>=g.Target;
            AddLog($"QUEST OBJECTIVE — {source}: {Mathf.Min(g.Current,g.Target)}/{g.Target}{(g.Complete?" COMPLETE":"")}.");
            OnWorldEventQuestGoalProgress0857(g);
        }

        private bool TryResolveGenericQuestChoice0856(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state?.Mode!="QuestFinalChoice0856")return false;
            if(QuestRuntime0856==null||hero==null||string.IsNullOrWhiteSpace(key))return true;
            var g=QuestRuntime0856.Goals.FirstOrDefault(x=>!x.Complete&&x.Kind=="Choice");if(g==null)return true;
            QuestRuntime0856.FinalChoice=key;QuestRuntime0856.FinalChoiceResolved=true;AdvanceGoal0856(g,1,$"Final choice: {key}");
            AddQuestCampaignFlag0856($"{CurrentQuestId0829}: {key}");PendingRewardChoice=null;EvaluateQuestOutcome0829();StateChanged?.Invoke();result=true;return true;
        }

        private void AppendGenericQuestChoiceOptions0856(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(state?.Mode!="QuestFinalChoice0856")return;
            foreach(var option in state.RevealedCardIds??new List<string>())options.Add(new RewardChoiceOption{Key=option,Label=option});
        }

        private bool TryResolveGenericQuestSearch0856(LocationState location,HeroState hero)
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null||location==null||hero==null)return false;
            var g=QuestRuntime0856.Goals.FirstOrDefault(x=>!x.Complete&&x.Kind=="Search"&&(x.LocationIds.Count==0||x.LocationIds.Any(id=>LocationMatchesGoal0856(location.Id,id))));
            if(g==null)return false;AdvanceGoal0856(g,1,$"{hero.DisplayName} successfully Searches {location.Name}");EnsureRequiredEnemyGoalsSpawned0856(true);EvaluateQuestOutcome0829();return false; // normal Search reward still resolves
        }

        private bool OnGenericQuestLocationRevealed0856(LocationState location,HeroState hero)
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null||location==null)return false;
            ResolveQuestRevealSpawns0856(location);EvaluateAutomaticGoals0856(hero);EnsureRequiredEnemyGoalsSpawned0856(true);EvaluateQuestOutcome0829();return true;
        }
        private void OnGenericQuestHeroMoved0856(HeroState hero)
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null||hero==null)return;EvaluateAutomaticGoals0856(hero);EvaluateQuestOutcome0829();
        }
        private void OnGenericQuestEnemyDefeated0856(EnemyState enemy,HeroState killer)
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null||enemy==null||enemy.QuestId!=CurrentQuestId0829)return;
            foreach(var g in QuestRuntime0856.Goals.Where(x=>!x.Complete&&x.Kind=="Enemy"&&x.EnemyTemplateIds.Contains(enemy.TemplateId,StringComparer.OrdinalIgnoreCase)))AdvanceGoal0856(g,1,$"{enemy.DisplayName} defeated");
            EvaluateAutomaticGoals0856(killer);EnsureRequiredEnemyGoalsSpawned0856(true);EvaluateQuestOutcome0829();
        }

        private void EvaluateAutomaticGoals0856(HeroState hero=null)
        {
            if(QuestRuntime0856==null)return;
            foreach(var g in QuestRuntime0856.Goals.Where(x=>!x.Complete).ToList())
            {
                if(g.Kind=="Reach")
                {
                    if(ParticipatingHeroes.Any(h=>!h.IsDefeated&&g.LocationIds.Any(id=>LocationMatchesGoal0856(h.LocationId,id)))){g.Current=g.Target;g.Complete=true;AddLog($"QUEST OBJECTIVE — {g.Label} COMPLETE.");}
                }
                else if(g.Kind=="Loot")
                {
                    bool held=g.LootIds.All(id=>ParticipatingHeroes.Any(h=>h.LootInventory.Contains(id,StringComparer.OrdinalIgnoreCase)));
                    if(!held)
                    {
                        bool authoredPrereqs=QuestRuntime0856.Goals.Where(x=>x!=g&&x.Kind!="Loot"&&x.Kind!="Reach"&&x.Kind!="Choice"&&x.Kind!="Hold").All(x=>x.Complete);
                        if(authoredPrereqs)
                        {
                            var receiver=hero??SelectedHero??ParticipatingHeroes.FirstOrDefault();
                            if(receiver!=null)foreach(var id in g.LootIds.Where(id=>!ParticipatingHeroes.Any(h=>h.LootInventory.Contains(id,StringComparer.OrdinalIgnoreCase))))GrantSpecificLootQuestItem0829(receiver,id,$"{CurrentQuestId0829} authored objective");
                            held=g.LootIds.All(id=>ParticipatingHeroes.Any(h=>h.LootInventory.Contains(id,StringComparer.OrdinalIgnoreCase)));
                        }
                    }
                    if(held){g.Current=g.Target;g.Complete=true;AddLog($"QUEST OBJECTIVE — {g.Label} COMPLETE.");}
                }
                else if(g.Kind=="Clear")
                {
                    var locs=g.LocationIds.Where(id=>!id.StartsWith("TAG:",StringComparison.OrdinalIgnoreCase)).ToList();
                    bool heroThere=locs.Count==0?ParticipatingHeroes.Any(h=>!h.IsDefeated):ParticipatingHeroes.Any(h=>!h.IsDefeated&&locs.Any(id=>LocationMatchesGoal0856(h.LocationId,id)));
                    bool enemies=locs.Count==0?Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId==CurrentQuestId0829):Enemies.Values.Any(e=>!e.Defeated&&e.QuestSpawned&&e.QuestId==CurrentQuestId0829&&locs.Any(id=>LocationMatchesGoal0856(e.LocationId,id)));
                    if(heroThere&&!enemies){g.Current=g.Target;g.Complete=true;AddLog($"QUEST OBJECTIVE — {g.Label} COMPLETE.");}
                }
                else if(g.Kind=="Hold"&&Round>=g.Target){g.Current=g.Target;g.Complete=true;AddLog($"QUEST OBJECTIVE — {g.Label} COMPLETE.");}
            }
        }

        private bool ResolveGenericQuestEndEnemyPhase0856()
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null)return false;
            if(QuestRuntime0856.PressureIncrementsAtEnemyPhase&&QuestRuntime0856.PressureLimit>0)
            {
                bool skip=PartySize<=1&&QuestRuntime0856.PressureEveryOtherEnemyPhaseSolo&&Round%2!=0;
                bool evidenceDone=QuestRuntime0856.Goals.Any(g=>g.Label.IndexOf("Evidence",StringComparison.OrdinalIgnoreCase)>=0&&g.Complete);
                if(CurrentQuestId0829=="Q014"&&evidenceDone)skip=true;
                if(!skip){QuestRuntime0856.Pressure=Mathf.Min(QuestRuntime0856.PressureLimit,QuestRuntime0856.Pressure+1);AddLog($"{CurrentQuestId0829} PRESSURE — {QuestRuntime0856.PressureName} {QuestRuntime0856.Pressure}/{QuestRuntime0856.PressureLimit}.");}
            }
            EvaluateAutomaticGoals0856();EvaluateQuestOutcome0829();return true;
        }

        private bool EvaluateGenericQuestOutcome0856()
        {
            if(!IsGenericQuestRuntime0856||QuestRuntime0856==null)return false;
            if(QuestRuntime0856.PressureLimit>0&&QuestRuntime0856.Pressure>=QuestRuntime0856.PressureLimit&&!QuestRuntime0856.Goals.All(g=>g.Complete))
            {QuestFailed=true;AddLog($"{CurrentQuestId0829} FAILED — {QuestRuntime0856.PressureName} reached {QuestRuntime0856.PressureLimit} before the objectives were completed.");RecordCampaignQuestFailed0833(CurrentQuestId0829);return true;}
            EvaluateAutomaticGoals0856();
            if(QuestRuntime0856.Goals.All(g=>g.Complete)&&!QuestComplete)
            {QuestComplete=true;AddLog($"{CurrentQuestId0829} COMPLETE — all authored victory requirements resolved.");ResolveQuestReward0829();}
            return true;
        }

        private bool ResolveGenericQuestReward0856(HeroState receiver)
        {
            if(!IsGenericQuestRuntime0856||!FullContentCatalog0850.TryGetQuest(CurrentQuestId0829,out var q))return false;
            string reward=q.rewards??string.Empty;
            var xp=Regex.Match(reward,@"(\d+)\s*XP",RegexOptions.IgnoreCase);if(xp.Success&&int.TryParse(xp.Groups[1].Value,out int xpValue))foreach(var h in ParticipatingHeroes)h.XP+=xpValue;
            var gold=Regex.Match(reward,@"(\d+)\s*Gold",RegexOptions.IgnoreCase);if(gold.Success&&int.TryParse(gold.Groups[1].Value,out int goldValue))AddPartyGold(goldValue,$"{q.id} victory reward");
            var gr=Regex.Match(reward,@"([+-]\d+)\s*General Reputation",RegexOptions.IgnoreCase);if(gr.Success&&int.TryParse(gr.Groups[1].Value,out int rep))AddGeneralReputation(rep,$"{q.id} victory reward");
            foreach(Match m in Regex.Matches(reward,@"([+-]\d+)\s+([^;,.]+?)\s+Reputation",RegexOptions.IgnoreCase))
            {
                if(m.Groups[2].Value.IndexOf("General",StringComparison.OrdinalIgnoreCase)>=0)continue;
                if(int.TryParse(m.Groups[1].Value,out int delta))AddFactionReputation0856(m.Groups[2].Value.Trim(),delta,$"{q.id} victory reward");
            }
            foreach(var loot in Regex.Matches(reward,@"LT\d{3}",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>m.Value.ToUpperInvariant()).Distinct(StringComparer.OrdinalIgnoreCase))
                if(receiver!=null)GrantSpecificLootQuestItem0829(receiver,loot,$"{q.id} victory reward");
            if(reward.IndexOf("Legendary",StringComparison.OrdinalIgnoreCase)>=0||reward.IndexOf("Mythic",StringComparison.OrdinalIgnoreCase)>=0)
            {
                if(reward.IndexOf("per Hero",StringComparison.OrdinalIgnoreCase)>=0||reward.IndexOf("each Hero",StringComparison.OrdinalIgnoreCase)>=0)foreach(var h in ParticipatingHeroes)DrawLegendaryEquipment0832(h,$"{q.id} victory reward");
                else if(receiver!=null)DrawLegendaryEquipment0832(receiver,$"{q.id} victory reward");
            }
            else if(reward.IndexOf("Epic Equipment",StringComparison.OrdinalIgnoreCase)>=0){if(receiver!=null)DrawEpicEquipment0831(receiver,$"{q.id} victory reward");}
            else if(reward.IndexOf("Rare Equipment",StringComparison.OrdinalIgnoreCase)>=0){if(receiver!=null)DrawRareEquipment0830(receiver,$"{q.id} victory reward");}
            else if(reward.IndexOf("Equipment Card",StringComparison.OrdinalIgnoreCase)>=0||reward.IndexOf("Equipment.",StringComparison.OrdinalIgnoreCase)>=0){if(receiver!=null)DrawEquipmentToHero(receiver,$"{q.id} victory reward");}
            AddQuestCampaignFlag0856($"{q.id} COMPLETE");
            foreach(Match m in Regex.Matches(reward,@"\b(?:unlock|unlocks)\s+([^;,.]+)",RegexOptions.IgnoreCase))
                if(!string.IsNullOrWhiteSpace(m.Groups[1].Value))AddQuestCampaignFlag0856($"UNLOCK — {m.Groups[1].Value.Trim()}");
            if(reward.IndexOf("complete Campaign",StringComparison.OrdinalIgnoreCase)>=0||reward.IndexOf("completes Campaign",StringComparison.OrdinalIgnoreCase)>=0)
            {
                AddQuestCampaignFlag0856($"CAMPAIGN {CurrentCampaignDefinition0845.Number} COMPLETE");
                AddQuestCampaignFlag0856($"{CurrentCampaignTitle0845} COMPLETE");
                var authoredCampaign=Regex.Match(reward,@"\bCampaign\s+([IVXLCDM]+|\d+)\b",RegexOptions.IgnoreCase);
                if(authoredCampaign.Success)AddQuestCampaignFlag0856($"Campaign {authoredCampaign.Groups[1].Value.ToUpperInvariant()}");
            }
            if(!string.IsNullOrWhiteSpace(q.campaignFollowOn))AddQuestCampaignFlag0856(q.campaignFollowOn.Trim());
            AddLog($"{q.id} REWARD — {reward}");return true;
        }

        private void AddQuestCampaignFlag0856(string flag)
        {
            if(string.IsNullOrWhiteSpace(flag)||questCampaignFlags0856.Contains(flag,StringComparer.OrdinalIgnoreCase))return;questCampaignFlags0856.Add(flag);AddLog($"WORLD STATE — {flag}");
        }
        private void AddFactionReputation0856(string faction,int delta,string source)
        {
            if(string.IsNullOrWhiteSpace(faction)||delta==0)return;var r=factionReputation0856.FirstOrDefault(x=>string.Equals(x.Faction,faction,StringComparison.OrdinalIgnoreCase));if(r==null){r=new FactionReputationState0856{Faction=faction};factionReputation0856.Add(r);}r.Score=Mathf.Clamp(r.Score+delta,-5,6);AddLog($"FACTION REPUTATION — {faction} {delta:+#;-#;0} ({source}). Total: {r.Score}.");
        }

        private void RestoreQuestRuntime0856(AscendanceSaveData save)
        {
            questCampaignFlags0856.Clear();if(save?.QuestCampaignFlags0856!=null)questCampaignFlags0856.AddRange(save.QuestCampaignFlags0856.Distinct(StringComparer.OrdinalIgnoreCase));
            factionReputation0856.Clear();if(save?.FactionReputation0856!=null)foreach(var r in save.FactionReputation0856)if(r!=null&&!string.IsNullOrWhiteSpace(r.Faction))factionReputation0856.Add(new FactionReputationState0856{Faction=r.Faction,Score=Mathf.Clamp(r.Score,-5,6)});
            QuestRuntime0856=null;
            if(QuestNumber0856(CurrentQuestId0829)<13)return;
            EnsureQuestRuntimeLocations0856(CurrentQuestId0829);
            var x=save?.QuestRuntime0856;
            if(x==null||!string.Equals(x.QuestId,CurrentQuestId0829,StringComparison.OrdinalIgnoreCase)){InitializeQuestRuntime0856(CurrentQuestId0829);return;}
            QuestRuntime0856=new QuestRuntimeState0856{QuestId=x.QuestId,QuestName=x.QuestName,SetupComplete=x.SetupComplete,PressureName=x.PressureName,Pressure=x.Pressure,PressureLimit=x.PressureLimit,PressureIncrementsAtEnemyPhase=x.PressureIncrementsAtEnemyPhase,PressureEveryOtherEnemyPhaseSolo=x.PressureEveryOtherEnemyPhaseSolo,FinalChoiceResolved=x.FinalChoiceResolved,FinalChoice=x.FinalChoice,FinalChoiceOptions=x.FinalChoiceOptions?.ToList()??new List<string>(),DelayedEnemyTemplateIds=x.DelayedEnemyTemplateIds?.ToList()??new List<string>(),RevealSpawns=x.RevealSpawns?.Select(r=>new QuestRevealSpawnState0856{EnemyTemplateId=r.EnemyTemplateId,TriggerLocationId=r.TriggerLocationId,SpawnLocationId=r.SpawnLocationId,Count=r.Count,Resolved=r.Resolved}).ToList()??new List<QuestRevealSpawnState0856>(),Goals=x.Goals?.Select(g=>new QuestGoalState0856{Key=g.Key,Label=g.Label,Kind=g.Kind,Current=g.Current,Target=g.Target,Complete=g.Complete,LocationIds=g.LocationIds?.ToList()??new List<string>(),EnemyTemplateIds=g.EnemyTemplateIds?.ToList()??new List<string>(),LootIds=g.LootIds?.ToList()??new List<string>()}).ToList()??new List<QuestGoalState0856>()};
        }

        public QuestRuntimeState0856 ExportQuestRuntime0856()
        {
            var x=QuestRuntime0856;if(x==null)return null;
            return new QuestRuntimeState0856{QuestId=x.QuestId,QuestName=x.QuestName,SetupComplete=x.SetupComplete,PressureName=x.PressureName,Pressure=x.Pressure,PressureLimit=x.PressureLimit,PressureIncrementsAtEnemyPhase=x.PressureIncrementsAtEnemyPhase,PressureEveryOtherEnemyPhaseSolo=x.PressureEveryOtherEnemyPhaseSolo,FinalChoiceResolved=x.FinalChoiceResolved,FinalChoice=x.FinalChoice,FinalChoiceOptions=x.FinalChoiceOptions?.ToList()??new List<string>(),DelayedEnemyTemplateIds=x.DelayedEnemyTemplateIds?.ToList()??new List<string>(),RevealSpawns=x.RevealSpawns?.Select(r=>new QuestRevealSpawnState0856{EnemyTemplateId=r.EnemyTemplateId,TriggerLocationId=r.TriggerLocationId,SpawnLocationId=r.SpawnLocationId,Count=r.Count,Resolved=r.Resolved}).ToList()??new List<QuestRevealSpawnState0856>(),Goals=x.Goals?.Select(g=>new QuestGoalState0856{Key=g.Key,Label=g.Label,Kind=g.Kind,Current=g.Current,Target=g.Target,Complete=g.Complete,LocationIds=g.LocationIds?.ToList()??new List<string>(),EnemyTemplateIds=g.EnemyTemplateIds?.ToList()??new List<string>(),LootIds=g.LootIds?.ToList()??new List<string>()}).ToList()??new List<QuestGoalState0856>()};
        }
        public List<FactionReputationState0856> ExportFactionReputation0856()=>factionReputation0856.Select(r=>new FactionReputationState0856{Faction=r.Faction,Score=r.Score}).ToList();

        // Validator/debug hooks — deterministic and deliberately separate from normal player input.
        public int DebugQuestGoalCount0856()=>QuestRuntime0856?.Goals?.Count??0;
        public bool DebugGenericQuestHasPressure0856()=>QuestRuntime0856?.PressureLimit>0;
        public int DebugFactionReputation0856(string faction)=>factionReputation0856.FirstOrDefault(x=>string.Equals(x.Faction,faction,StringComparison.OrdinalIgnoreCase))?.Score??0;
        public bool DebugHasQuestCampaignFlag0856(string text)=>questCampaignFlags0856.Any(x=>x.IndexOf(text??string.Empty,StringComparison.OrdinalIgnoreCase)>=0);
        public void DebugAdvanceAllGenericGoals0856()
        {
            if(QuestRuntime0856==null)return;
            foreach(var g in QuestRuntime0856.Goals){g.Current=g.Target;g.Complete=true;}
            QuestRuntime0856.FinalChoiceResolved=true;QuestRuntime0856.FinalChoice=QuestRuntime0856.FinalChoiceOptions.FirstOrDefault()??"Resolved";EvaluateQuestOutcome0829();
        }
        public void DebugSetGenericPressure0856(int value){if(QuestRuntime0856!=null)QuestRuntime0856.Pressure=Mathf.Clamp(value,0,Mathf.Max(0,QuestRuntime0856.PressureLimit));}
        public void DebugRestoreQuestState0856(AscendanceSaveData save)=>RestoreQuestRuntime0856(save);
        public void DebugClearQuestPersistentState0856(){questCampaignFlags0856.Clear();factionReputation0856.Clear();QuestRuntime0856=null;}
        public bool DebugQuestBlueprintExecutable0856(string questId)
        {
            if(!FullContentCatalog0850.TryGetQuest(questId,out var q))return false;
            int n=QuestNumber0856(questId);if(n<=12)return QuestContent0829.Definitions.ContainsKey(questId);
            var temp=new QuestRuntimeState0856{QuestId=q.id,QuestName=q.name};BuildQuestGoals0856(q,temp);ConfigurePressure0856(q,temp);ConfigureFinalChoice0856(q,temp);
            return temp.Goals.Count>0&&FullContentCatalog0850.QuestLocationPoolIds(questId).Count>0&&!string.IsNullOrWhiteSpace(FullContentCatalog0850.StartingLocationId(questId));
        }
    }
}
