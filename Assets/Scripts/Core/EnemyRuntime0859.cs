using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    [Serializable]
    public sealed class EnemyHazardState0859
    {
        public string HazardId;
        public string Type;
        public string SourceEnemyId;
        public string LocationId;
        public int CreatedRound;
        public int TriggerRound;
    }

    // 0.8.59 — catalog-driven runtime execution for all authored Enemy rows E001-E125.
    public sealed partial class AscendanceGameManager
    {
        private const string Enemy0859Prefix="ENEMY0859:";
        public readonly List<EnemyHazardState0859> EnemyHazards0859=new();

        private EncounterEnemyTemplate EnemyTemplate0859(EnemyState enemy)
            => enemy!=null&&SearchEncounterContent0820.EnemyTemplates.TryGetValue(enemy.TemplateId??enemy.Id??string.Empty,out var t)?t:null;
        private EncounterEnemyTemplate EnemyTemplate0859(string templateId)
            => SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId??string.Empty,out var t)?t:null;
        private string EnemyText0859(EnemyState enemy)
        {
            var t=EnemyTemplate0859(enemy);return t==null?string.Empty:$"{t.Name} {t.EnemyType} {t.Role} {t.BehaviorProfile} {t.Traits} {t.Keywords} {t.Abilities} {t.Behavior}";
        }
        private bool EnemyHas0859(EnemyState enemy,string text)=>enemy!=null&&!string.IsNullOrEmpty(text)&&EnemyText0859(enemy).IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0;
        private bool EnemyTypeHas0859(EnemyState enemy,params string[] tokens)
        {
            var t=EnemyTemplate0859(enemy);if(t==null)return false;string s=$"{t.EnemyType} {t.Role} {t.Traits} {t.Keywords} {t.Name}";
            return tokens.Any(x=>!string.IsNullOrEmpty(x)&&s.IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0);
        }
        private bool EnemyUseKey0859(EnemyState enemy,string key)
        {
            if(enemy==null||string.IsNullOrEmpty(key))return false;enemy.UsedKeys0859??=new List<string>();string k=Enemy0859Prefix+key;if(enemy.UsedKeys0859.Contains(k,StringComparer.OrdinalIgnoreCase))return false;enemy.UsedKeys0859.Add(k);return true;
        }
        private bool EnemyHasKey0859(EnemyState enemy,string key)=>enemy?.UsedKeys0859?.Contains(Enemy0859Prefix+key,StringComparer.OrdinalIgnoreCase)==true;
        private void EnemyClearKey0859(EnemyState enemy,string key)=>enemy?.UsedKeys0859?.RemoveAll(x=>string.Equals(x,Enemy0859Prefix+key,StringComparison.OrdinalIgnoreCase));
        private string EnemyRoundKey0859(string key)=>$"R{Round}:{key}";

        public bool IsEnemyExecutable0859(string templateId)
        {
            var t=EnemyTemplate0859(templateId);return t!=null&&!string.IsNullOrWhiteSpace(t.Id)&&t.Health>0&&t.Movement>=0&&!string.IsNullOrWhiteSpace(t.Range)&&!string.IsNullOrWhiteSpace(t.BehaviorProfile)&&!string.IsNullOrWhiteSpace(t.Abilities);
        }

        private static int ParseEnemyRange0859(string range)
        {
            string r=range??string.Empty;if(r.IndexOf("within 3",StringComparison.OrdinalIgnoreCase)>=0)return 3;if(r.IndexOf("within 2",StringComparison.OrdinalIgnoreCase)>=0)return 2;
            if(r.IndexOf("connected",StringComparison.OrdinalIgnoreCase)>=0||r.IndexOf("Reach",StringComparison.OrdinalIgnoreCase)>=0)return 1;return 0;
        }
        private int AuthoredEnemyMovement0859(EnemyState enemy)
        {var t=EnemyTemplate0859(enemy);return t!=null?Mathf.Max(0,t.Movement):1;}
        private int AuthoredEnemyRange0859(EnemyState enemy)
        {var t=EnemyTemplate0859(enemy);return t!=null?ParseEnemyRange0859(t.Range):0;}

        // Some authored Enemies extend their printed Range conditionally through an Ability.
        // Kraken Spawn (E102) is Melee by default, but Reach allows a connected attack only when no Hero is here.
        private int EnemyConditionalRange0859(EnemyState enemy,int baseRange)
        {
            if(enemy==null)return Mathf.Max(0,baseRange);
            if(enemy.TemplateId=="E102"&&!ParticipatingHeroes.Any(h=>!h.IsDefeated&&!h.Moving&&h.LocationId==enemy.LocationId))
                return Mathf.Max(baseRange,1);
            return Mathf.Max(0,baseRange);
        }

        private bool EnemyAlliedWithin0859(EnemyState enemy,int range,Func<EnemyState,bool> predicate)
            => enemy!=null&&Enemies.Values.Any(e=>e!=enemy&&!e.Defeated&&predicate(e)&&GraphDistance(enemy.LocationId,e.LocationId)<=range);
        private IEnumerable<EnemyState> EnemyAlliesWithin0859(EnemyState enemy,int range,Func<EnemyState,bool> predicate)
            => enemy==null?Enumerable.Empty<EnemyState>():Enemies.Values.Where(e=>e!=enemy&&!e.Defeated&&predicate(e)&&GraphDistance(enemy.LocationId,e.LocationId)<=range);
        private IEnumerable<HeroState> EnemyHeroTargets0859(EnemyState enemy,int range)
            => enemy==null?Enumerable.Empty<HeroState>():ParticipatingHeroes.Where(h=>!h.IsDefeated&&!h.Moving&&GraphDistance(enemy.LocationId,h.LocationId)<=range);
        private bool HeroIsIsolated0859(HeroState hero)=>hero!=null&&!ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==hero.LocationId);

        private int EnemyStatusStatModifier0859(EnemyState enemy,string prefix)
        {
            if(enemy?.Statuses==null)return 0;int n=0;
            foreach(var s in enemy.Statuses.Where(s=>s!=null&&s.Name!=null&&s.Name.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)))
            {var m=Regex.Match(s.Name,@"([+-]?\d+)$");if(m.Success&&int.TryParse(m.Groups[1].Value,out int v))n+=v;}
            return n;
        }

        private int EnemyAttackModifier0859(EnemyState enemy,HeroState target=null)
        {
            if(enemy==null||enemy.Defeated)return 0;int mod=EnemyStatusStatModifier0859(enemy,"Enemy Attack ");var t=EnemyTemplate0859(enemy);if(t==null)return mod;
            string id=t.Id;int sameEnemy=Enemies.Values.Count(e=>e.Id!=enemy.Id&&!e.Defeated&&e.LocationId==enemy.LocationId);
            if(id=="E001"&&sameEnemy==0)mod+=1;
            if(id=="E002"&&EnemyAlliedWithin0859(enemy,0,IsGoblinEnemy))mod+=1;
            if(id=="E004")mod+=Enemies.Values.Count(e=>e.Id!=enemy.Id&&!e.Defeated&&e.LocationId==enemy.LocationId&&IsGoblinEnemy(e));
            if(id=="E009"&&enemy.Health<=4)mod+=2;
            if((id=="E013"||id=="E021"||id=="E024")&&enemy.MovementStepsThisActivation0859>=2)mod+=2;
            if((id=="E052"||id=="E078"||id=="E089"||id=="E107")&&enemy.MovementStepsThisActivation0859>0)mod+=1;
            if(id=="E020"&&EnemyAlliedWithin0859(enemy,1,e=>EnemyTypeHas0859(e,"Beast")))mod+=1;
            if(id=="E025"&&enemy.Health<=10)mod+=2;
            if(id=="E033"&&EnemyAlliedWithin0859(enemy,0,e=>EnemyTypeHas0859(e,"Cult","Criminal")))mod+=1;
            if(id=="E034"&&enemy.MovementStepsThisActivation0859==0)mod+=1;
            if(id=="E036"&&target!=null&&!Enemies.Values.Any(e=>e.Id!=enemy.Id&&!e.Defeated&&e.LocationId==target.LocationId))mod+=2;
            if(id=="E039"&&enemy.MovementStepsThisActivation0859==0&&Locations.TryGetValue(enemy.LocationId,out var underground)&&underground.HasPrintedTag("Underground"))mod+=1;
            if(id=="E041"&&target!=null&&Locations.TryGetValue(target.LocationId,out var open)&&open.HasPrintedTag("Open Ground"))mod+=1;
            if(id=="E047"&&target!=null&&target.Id==enemy.FocusHeroId0859)mod+=2;
            if(id=="E051"&&EnemyAlliedWithin0859(enemy,0,e=>e.TemplateId=="E051"))mod+=1;
            if(id=="E057"&&Locations.TryGetValue(enemy.LocationId,out var mine)&&mine.UsedKeys0858.Any(k=>k.IndexOf("CAVE-IN",StringComparison.OrdinalIgnoreCase)>=0||k.IndexOf("FRACTURE",StringComparison.OrdinalIgnoreCase)>=0))mod+=1;
            if(id=="E070"&&EnemyAlliedWithin0859(enemy,0,e=>EnemyTypeHas0859(e,"Beast","Blighted")))mod+=1;
            if(id=="E088"&&EnemyAlliedWithin0859(enemy,0,e=>EnemyTypeHas0859(e,"Drowned")))mod+=1;
            if(id=="E093"&&!ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId))mod+=1;
            if(id=="E101"&&target!=null&&Locations.TryGetValue(target.LocationId,out var stormTarget)&&(stormTarget.HasPrintedTag("Coast")||stormTarget.HasPrintedTag("Sea")||stormTarget.HasPrintedTag("Storm")))mod+=1;
            if(id=="E102"&&target!=null&&target.LocationId!=enemy.LocationId)mod-=1;
            if(id=="E096"&&EnemyAlliedWithin0859(enemy,0,e=>e.TemplateId=="E096"))mod+=1;
            if(id=="E109"&&Locations.TryGetValue(enemy.LocationId,out var sunfire)&&(sunfire.HasPrintedTag("Solar")||sunfire.HasPrintedTag("Heat")))mod+=1;
            if(id=="E112"&&!ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId))mod+=1;

            // Passive commander auras.
            foreach(var source in Enemies.Values.Where(e=>!e.Defeated&&e.Id!=enemy.Id))
            {
                int d=GraphDistance(source.LocationId,enemy.LocationId);string sid=source.TemplateId??string.Empty;
                if(sid=="E006"&&d==0&&IsGoblinEnemy(enemy))mod+=1;
                else if(sid=="E010"&&d<=1&&IsGoblinEnemy(enemy))mod+=1;
                else if(sid=="E032"&&d<=1&&IsGoblinEnemy(enemy)&&!(CurrentQuestId0829=="Q012"&&source.QuestSpawned))mod+=1;
                else if(sid=="E046"&&d<=1&&EnemyTypeHas0859(enemy,"Cult","Veil"))mod+=1;
                else if(sid=="E054"&&d==0&&Locations.TryGetValue(source.LocationId,out var forge)&&forge.HasPrintedTag("Forge")&&EnemyTypeHas0859(enemy,"Ashen Compact"))mod+=1;
                else if(sid=="E067"&&d<=1&&EnemyTypeHas0859(enemy,"Ashen Compact","Construct"))mod+=1;
                else if(sid=="E085"&&d<=1&&EnemyTypeHas0859(enemy,"Blighted","Spirit"))mod+=1;
                else if(sid=="E099"&&d==0&&EnemyTypeHas0859(enemy,"Drowned"))mod+=1;
                else if(sid=="E104"&&d<=1&&EnemyTypeHas0859(enemy,"Drowned","Sea"))mod+=1;
            }
            return mod;
        }

        private int EnemyArmorModifier0859(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return 0;int mod=EnemyStatusStatModifier0859(enemy,"Enemy Armor ");string id=enemy.TemplateId??string.Empty;
            if(id=="E018"&&EnemyAlliedWithin0859(enemy,0,e=>EnemyTypeHas0859(e,"Leader","Boss")||e.IsBoss))mod+=1;
            if(id=="E059"&&EnemyAlliedWithin0859(enemy,0,e=>e.TemplateId=="E059"))mod+=1;
            // Shieldbearer/Dorga/Barricade support other allies.
            if(Enemies.Values.Any(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId&&e.TemplateId=="E059")&&EnemyTypeHas0859(enemy,"Ashen Compact"))mod+=1;
            if(Enemies.Values.Any(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId&&e.TemplateId=="E060")&&EnemyTypeHas0859(enemy,"Ashen Compact"))mod+=1;
            if(Enemies.Values.Any(e=>!e.Defeated&&e.Id!=enemy.Id&&GraphDistance(e.LocationId,enemy.LocationId)<=1&&e.TemplateId=="E048")&&EnemyTypeHas0859(enemy,"Cult"))mod+=1;
            if(EnemyHazards0859.Any(h=>h.Type=="Barricade"&&h.LocationId==enemy.LocationId)&&IsGoblinEnemy(enemy))mod+=2;
            int remaining=EnemyBoundObjectiveRemainingCount0859(enemy);
            if(id=="E066"&&remaining>0)mod+=1;
            if(id=="E084"&&remaining>0)mod+=remaining;
            if(id=="E123"&&remaining>0)mod+=remaining/2;
            return mod;
        }

        private int EnemyMovementModifier0859(EnemyState enemy)
        {
            if(enemy==null)return 0;int mod=0;
            if(enemy.TemplateId=="E025"&&enemy.Health<=10)mod+=1;
            if(Enemies.Values.Any(e=>!e.Defeated&&e.TemplateId=="E007"&&GraphDistance(e.LocationId,enemy.LocationId)<=1)&&EnemyTypeHas0859(enemy,"Beast"))mod+=1;
            if(Enemies.Values.Any(e=>!e.Defeated&&e.TemplateId=="E008"&&GraphDistance(e.LocationId,enemy.LocationId)<=1)&&IsGoblinEnemy(enemy))mod+=1;
            return mod;
        }
        private bool EnemyIgnoresDifficultTerrain0859(EnemyState enemy)
        {
            if(enemy==null)return false;string id=enemy.TemplateId??string.Empty;
            return id=="E026"||id=="E040"||id=="E042"||id=="E045"||id=="E051"||id=="E065"||id=="E078"||id=="E088"||id=="E094"||id=="E101"||id=="E107"||id=="E121";
        }
        private bool EnemyIgnoresEngagement0859(EnemyState enemy)
        {string id=enemy?.TemplateId??string.Empty;return id=="E026"||id=="E042"||id=="E045"||id=="E083";}
        private bool EnemyCannotBeMoved0859(EnemyState enemy)
        {
            if(enemy==null)return false;string id=enemy.TemplateId??string.Empty;
            if(id=="E084"||id=="E095"||id=="E103"||id=="E114"||id=="E123")return true;
            if((id=="E075"||id=="E111")&&IsGuardingQuestObjective0859(enemy))return true;
            if(id=="E062"&&Locations.TryGetValue(enemy.LocationId,out var l)&&(l.HasPrintedTag("Forge")||l.HasPrintedTag("Worldforge")))return true;
            return false;
        }

        private bool IsGuardingQuestObjective0859(EnemyState enemy)
        {
            if(enemy==null)return false;string authored=Locations.TryGetValue(enemy.LocationId??string.Empty,out var l)?(l.AuthoredLocationId??l.Id):null;
            if(QuestRuntime0856!=null&&QuestRuntime0856.Goals.Any(g=>!g.Complete&&(g.LocationIds==null||g.LocationIds.Count==0||g.LocationIds.Contains(authored,StringComparer.OrdinalIgnoreCase))))return true;
            return QuestRuntime0829?.Objects?.Any(o=>!o.Destroyed&&!o.Resolved&&string.Equals(o.LocationId,enemy.LocationId,StringComparison.OrdinalIgnoreCase))==true;
        }
        private int EnemyBoundObjectiveRemainingCount0859(EnemyState enemy)
        {
            if(enemy==null||QuestRuntime0856==null)return 0;string a=EnemyTemplate0859(enemy)?.Abilities??string.Empty;var qm=Regex.Match(a,@"Q\d{3}",RegexOptions.IgnoreCase);if(qm.Success&&!string.Equals(CurrentQuestId0829,qm.Value,StringComparison.OrdinalIgnoreCase))return 0;
            string[] nouns={"Ward","Root","Chain","Anchor","Node","Seal","Bell","Lock"};string noun=nouns.FirstOrDefault(n=>a.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);
            if(noun==null)return QuestRuntime0856.Goals.Where(g=>!g.Complete).Sum(g=>Mathf.Max(1,g.Target-g.Current));
            return QuestRuntime0856.Goals.Where(g=>!g.Complete&&g.Label.IndexOf(noun,StringComparison.OrdinalIgnoreCase)>=0).Sum(g=>Mathf.Max(1,g.Target-g.Current));
        }
        private int EnemyHealthFloor0859(EnemyState enemy)
        {
            if(enemy==null)return 0;string a=EnemyTemplate0859(enemy)?.Abilities??string.Empty;int remaining=EnemyBoundObjectiveRemainingCount0859(enemy);if(remaining<=0)return 0;
            var m=Regex.Match(a,@"cannot be reduced below\s*(\d+)\s*Health",RegexOptions.IgnoreCase);if(m.Success&&int.TryParse(m.Groups[1].Value,out int n))return n;
            if(a.IndexOf("cannot be finally defeated",StringComparison.OrdinalIgnoreCase)>=0)return 1;return 0;
        }

        private int ModifyEnemyIncomingDamage0859(EnemyState enemy,int damage,HeroState sourceHero,CardData card,bool rangedAttack,string source)
        {
            if(enemy==null||damage<=0)return Mathf.Max(0,damage);int original=damage;string id=enemy.TemplateId??string.Empty;string key=EnemyRoundKey0859("FIRST_DAMAGE");
            bool fire=(source??string.Empty).IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0||(card?.Traits??string.Empty).IndexOf("Fire",StringComparison.OrdinalIgnoreCase)>=0;
            if(id=="E061"&&fire){AddLog($"ENEMY IMMUNITY — {enemy.DisplayName} ignores Fire damage.");return 0;}
            int reduce=0;
            if(id=="E031"&&EnemyUseKey0859(enemy,key))reduce=damage; // Ethereal Form.
            else if(id=="E015"&&EnemyUseKey0859(enemy,key))reduce=2;
            else if((id=="E044"||id=="E055"||id=="E080"||id=="E119")&&IsGuardingQuestObjective0859(enemy)&&EnemyUseKey0859(enemy,key))reduce=2;
            else if(id=="E059"&&IsGuardingQuestObjective0859(enemy)&&EnemyUseKey0859(enemy,key))reduce=1;
            else if((id=="E062"||id=="E082"||id=="E092"||id=="E111"||id=="E117"||id=="E122")&&EnemyUseKey0859(enemy,key))reduce= id=="E062"?1:2;
            else if(id=="E075"&&!rangedAttack&&EnemyUseKey0859(enemy,key))reduce=1;
            else if(id=="E108"&&rangedAttack&&EnemyUseKey0859(enemy,key))reduce=1;
            else if((id=="E110"||id=="E115")&&sourceHero!=null&&EnemyUseKey0859(enemy,key))reduce=1;
            else if(id=="E049"&&damage>=5&&EnemyUseKey0859(enemy,key))reduce=2;
            if(id=="E024")
            {
                string k=EnemyRoundKey0859("THICK_HIDE_COUNT");int used=enemy.UsedKeys0859.Count(x=>x.StartsWith(Enemy0859Prefix+k,StringComparison.OrdinalIgnoreCase));if(used<2){reduce=Mathf.Max(reduce,1);enemy.UsedKeys0859.Add(Enemy0859Prefix+k+":"+used);}
            }
            damage=Mathf.Max(0,damage-reduce);
            if(reduce>0)AddLog($"ENEMY DEFENSE — {enemy.DisplayName} reduces {original} damage to {damage} ({EnemyTemplate0859(enemy)?.Abilities?.Split('—').FirstOrDefault()?.Trim()??"authored defense"}).");
            if(fire)enemy.UsedKeys0859.Add(Enemy0859Prefix+"FIRE_SINCE_ACTIVATION");
            return damage;
        }
        private int ApplyEnemyHealthFloor0859(EnemyState enemy,int proposedHealth)
        {
            int floor=EnemyHealthFloor0859(enemy);if(floor<=0||proposedHealth>=floor)return Mathf.Max(0,proposedHealth);AddLog($"ENEMY BOUND — {enemy.DisplayName} cannot fall below {floor} Health while its authored objective binding remains.");return floor;
        }
        private void OnEnemyDamaged0859(EnemyState enemy,int dealt,HeroState attacker,CardData card,bool rangedAttack)
        {
            if(enemy==null||dealt<=0)return;string id=enemy.TemplateId??string.Empty;
            if(id=="E036"&&EnemyUseKey0859(enemy,"VANISH"))MoveEnemyAwayFromHeroes0859(enemy,"Veiled Agent — Vanish");
            if(id=="E066"&&dealt>=6&&EnemyUseKey0859(enemy,EnemyRoundKey0859("OVERHEAT")))foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId).ToList())ApplyHeroDamageNow(h,1,"Furnace Colossus Overheat");
            if(id=="E071"&&dealt>=5&&EnemyUseKey0859(enemy,EnemyRoundKey0859("SPORE_BURST")))foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId).ToList()){if(h.Hand.Count>0){h.Discard.Add(h.Hand[0].Id);h.Hand.RemoveAt(0);AddLog($"ENEMY RETALIATION — {h.DisplayName} discards 1 card to Spore Burst.");}else ApplyHeroDamageNow(h,1,"Spore Burst");}
            if(id=="E116"&&dealt>=5&&attacker!=null&&EnemyUseKey0859(enemy,EnemyRoundKey0859("GLASS_BODY")))ApplyHeroDamageNow(attacker,1,"Glass Revenant Glass Body");
            if(id=="E120"&&dealt>=6&&attacker!=null&&EnemyUseKey0859(enemy,EnemyRoundKey0859("NOON_REFLECTION")))ApplyHeroDamageNow(attacker,1,"Vizier Sahmet Noon Reflection");
        }
        private void OnEnemyDefeated0859(EnemyState enemy,HeroState killer)
        {
            if(enemy==null)return;string id=enemy.TemplateId??string.Empty;
            if(id=="E069"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("ROT_CLOUD")))
            {var h=ParticipatingHeroes.Where(x=>!x.IsDefeated&&x.LocationId==enemy.LocationId).OrderBy(x=>x.Health).FirstOrDefault();if(h!=null&&!h.HasStatus("Guarded"))ApplyHeroDamageNow(h,1,"Blightling Rot Cloud");}
            if(id=="E116"&&Locations.TryGetValue(enemy.LocationId,out var l)){l.UsedKeys0858.Add(Enemy0859Prefix+"SHARD_HAZARD");AddLog($"ENEMY DEFEAT — {enemy.DisplayName} leaves a Shard Hazard at {l.Name}.");}
        }

        private void PrepareEnemyPhase0859()
        {
            foreach(var enemy in Enemies.Values.Where(e=>!e.Defeated))enemy.MovementStepsThisActivation0859=0;
            foreach(var shaman in Enemies.Values.Where(e=>!e.Defeated&&e.TemplateId=="E019").ToList())
            {
                var ally=EnemyAlliesWithin0859(shaman,1,IsGoblinEnemy).OrderByDescending(e=>e.Attack).FirstOrDefault();if(ally!=null&&EnemyUseKey0859(shaman,EnemyRoundKey0859("DARK_RITUAL")))AddStatus(ally,"Enemy Attack +2",0,1,"Goblin High Shaman — Dark Ritual",true,null,"until end of round");
            }
            foreach(var spirit in Enemies.Values.Where(e=>!e.Defeated&&e.TemplateId=="E031").ToList())
            {
                var beast=EnemyAlliesWithin0859(spirit,1,e=>EnemyTypeHas0859(e,"Beast")).OrderBy(e=>e.Health/(float)Mathf.Max(1,e.MaxHealth)).FirstOrDefault();if(beast!=null&&EnemyUseKey0859(spirit,EnemyRoundKey0859("BLESSING"))){beast.Health=Mathf.Min(beast.MaxHealth,beast.Health+2);AddStatus(beast,"Enemy Armor +1",0,1,"Forest Spirit — Blessing of the Wild",true,null,"until end of round");AddLog($"ENEMY SUPPORT — {spirit.DisplayName} restores 2 Health to {beast.DisplayName}.");}
            }
            foreach(var killer in Enemies.Values.Where(e=>!e.Defeated&&e.TemplateId=="E047"))killer.FocusHeroId0859=ParticipatingHeroes.Where(h=>!h.IsDefeated).OrderBy(h=>h.Health).ThenBy(h=>h.PartyIndex).FirstOrDefault()?.Id;
        }

        private void PrepareEnemyActivation0859(EnemyState enemy)
        {
            if(enemy==null)return;enemy.MovementStepsThisActivation0859=0;string id=enemy.TemplateId??string.Empty;
            if(id=="E030")
            {if(!EnemyHasKey0859(enemy,"FIRE_SINCE_ACTIVATION")&&enemy.Health<enemy.MaxHealth){int before=enemy.Health;enemy.Health=Mathf.Min(enemy.MaxHealth,enemy.Health+3);if(enemy.Health>before)AddLog($"ENEMY REGEN — {enemy.DisplayName} recovers {enemy.Health-before} Health.");}EnemyClearKey0859(enemy,"FIRE_SINCE_ACTIVATION");}
            if(id=="E005")HealEnemyAlly0859(enemy,e=>IsGoblinEnemy(e),1,"Tribal Blessing");
            if(id=="E021")foreach(var e in EnemyAlliesWithin0859(enemy,1,x=>EnemyTypeHas0859(x,"Beast")))AddStatus(e,"Enemy Attack +1",0,1,"Dire Wolf — Alpha's Howl",true,null,"until end of round");
            if(id=="E035"){var e=EnemyAlliesWithin0859(enemy,1,x=>EnemyTypeHas0859(x,"Cult")).OrderByDescending(x=>x.Attack).FirstOrDefault();if(e!=null)AddStatus(e,"Enemy Attack +1",0,1,"Masked Acolyte — Veiled Chant",true,null,"until end of round");}
            if(id=="E043"&&EnemyUseKey0859(enemy,"RAISE_FALLEN")&&Enemies.Values.Count(e=>!e.Defeated&&e.TemplateId=="E040")<2)SpawnEnemyFromAbility0859(enemy,"E040",enemy.LocationId,"Raise the Fallen");
            if(id=="E054")HealEnemyAlly0859(enemy,e=>EnemyTypeHas0859(e,"Construct","Ashen Compact"),2,"Field Repair");
            if(id=="E063"){var e=EnemyAlliesWithin0859(enemy,AuthoredEnemyRange0859(enemy),x=>EnemyTypeHas0859(x,"Construct","Ashen Compact")).OrderByDescending(x=>x.Attack).FirstOrDefault();if(e!=null)AddStatus(e,"Enemy Attack +1",0,0,"Heartstone Adept — Heartstone Channel",true,null,"until end of next activation");}
            if(id=="E064"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("COMPACT_ORDERS"))){var ally=EnemyAlliesWithin0859(enemy,1,x=>EnemyTypeHas0859(x,"Ashen Compact")&&!x.IsBoss).OrderBy(x=>GraphDistance(x.LocationId,ParticipatingHeroes.Where(h=>!h.IsDefeated).Select(h=>h.LocationId).FirstOrDefault())).FirstOrDefault();if(ally!=null){string next=NextEnemyStepTowardHero(ally);if(!string.IsNullOrEmpty(next)&&Locations.ContainsKey(next)){string from=ally.LocationId;ally.LocationId=next;AddLog($"ENEMY COMMAND — {enemy.DisplayName} orders {ally.DisplayName} {Locations[from].Name} → {Locations[next].Name}.");}}}
            if(id=="E072")HealEnemyAlly0859(enemy,e=>EnemyTypeHas0859(e,"Blighted","Plant"),2,"Fungal Mend");
            if(id=="E076"){var e=EnemyAlliesWithin0859(enemy,0,x=>EnemyTypeHas0859(x,"Thorned Circle")).OrderBy(x=>x.Health).FirstOrDefault();if(e!=null)AddStatus(e,"Enemy Armor +1",0,0,"Ash Druid — Cinder Ward",true,null,"until end of next activation");}
            if(id=="E087"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("ROOTMIND")))foreach(var h in EnemyHeroTargets0859(enemy,AuthoredEnemyRange0859(enemy)).ToList())AddStatus(h,"Weakened",0,0,"The Hollow Bloom — Rootmind",false,null,"until end of next turn");
            if(id=="E113")HealEnemyAlly0859(enemy,e=>EnemyTypeHas0859(e,"Construct","Cinder"),2,"Solar Mend");
            if(id=="E118"){var e=EnemyAlliesWithin0859(enemy,1,x=>EnemyTypeHas0859(x,"Cinder","Solar")).OrderByDescending(x=>x.Attack).FirstOrDefault();if(e!=null)AddStatus(e,"Enemy Attack +1",0,0,"Eclipse Priest — Black Blessing",true,null,"until end of next activation");}
            // Authored challenge / target-lock behavior.
            if(id=="E015"||id=="E044"||id=="E119")enemy.FocusHeroId0859=EnemyHeroTargets0859(enemy,Mathf.Max(1,AuthoredEnemyRange0859(enemy))).OrderByDescending(EffectiveHeroAttack).ThenBy(h=>h.PartyIndex).FirstOrDefault()?.Id;
        }
        private void HealEnemyAlly0859(EnemyState source,Func<EnemyState,bool> predicate,int amount,string label)
        {
            var ally=Enemies.Values.Where(e=>e!=source&&!e.Defeated&&e.LocationId==source.LocationId&&predicate(e)&&e.Health<e.MaxHealth).OrderBy(e=>e.Health/(float)Mathf.Max(1,e.MaxHealth)).FirstOrDefault();if(ally==null)return;int before=ally.Health;ally.Health=Mathf.Min(ally.MaxHealth,ally.Health+amount);AddLog($"ENEMY SUPPORT — {source.DisplayName} {label}: {ally.DisplayName} recovers {ally.Health-before} Health.");
        }

        private HeroState ChooseEnemyPriorityTarget0859(EnemyState enemy,IEnumerable<HeroState> candidates)
        {
            var list=candidates?.Where(h=>h!=null&&!h.IsDefeated).ToList()??new List<HeroState>();if(list.Count==0)return null;
            if(!string.IsNullOrEmpty(enemy?.FocusHeroId0859)){var focus=list.FirstOrDefault(h=>h.Id==enemy.FocusHeroId0859);if(focus!=null)return focus;}
            string b=EnemyTemplate0859(enemy)?.Behavior??string.Empty;
            if(b.IndexOf("highest Attack",StringComparison.OrdinalIgnoreCase)>=0||b.IndexOf("strongest Hero",StringComparison.OrdinalIgnoreCase)>=0)return list.OrderByDescending(EffectiveHeroAttack).ThenBy(h=>h.PartyIndex).First();
            if(b.IndexOf("lowest Armor",StringComparison.OrdinalIgnoreCase)>=0)return list.OrderBy(EffectiveHeroArmor).ThenBy(h=>h.PartyIndex).First();
            if(b.IndexOf("lowest Health",StringComparison.OrdinalIgnoreCase)>=0||b.IndexOf("wounded Hero",StringComparison.OrdinalIgnoreCase)>=0)return list.OrderBy(h=>h.Health).ThenBy(h=>h.PartyIndex).First();
            if(b.IndexOf("most remaining Actions",StringComparison.OrdinalIgnoreCase)>=0)return list.OrderByDescending(h=>h.Actions).ThenBy(h=>h.PartyIndex).First();
            if(b.IndexOf("isolated",StringComparison.OrdinalIgnoreCase)>=0){var isolated=list.Where(HeroIsIsolated0859).OrderBy(h=>h.Health).FirstOrDefault();if(isolated!=null)return isolated;}
            return list.OrderBy(h=>GraphDistance(enemy.LocationId,h.LocationId)).ThenBy(h=>h.Health).ThenBy(h=>h.PartyIndex).First();
        }

        private int EnemyAttackArmorIgnore0859(EnemyState enemy,HeroState target)
        {
            if(enemy==null||target==null)return 0;string id=enemy.TemplateId??string.Empty;
            if(id=="E037"&&target.HasStatus("Guarded"))return 1;
            if(id=="E047"&&HeroIsIsolated0859(target)&&EnemyUseKey0859(enemy,EnemyRoundKey0859("EXECUTIONER_OPENING")))return 1;
            if(id=="E073"&&HeroIsIsolated0859(target)&&EnemyUseKey0859(enemy,"BRIAR_SHOT"))return 1;
            if(id=="E070"&&enemy.MovementStepsThisActivation0859>=2)return 1;
            if(id=="E090"&&enemy.MovementStepsThisActivation0859>0&&enemy.LocationId==target.LocationId)return 1;
            if(id=="E112"&&Locations.TryGetValue(target.LocationId,out var l)&&(l.HasPrintedTag("Desert")||l.HasPrintedTag("Solar")))return 1;
            return 0;
        }

        private bool TryResolveEnemyReplacementAbility0859(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return false;string id=enemy.TemplateId??string.Empty;int range=Mathf.Max(0,AuthoredEnemyRange0859(enemy));var targets=EnemyHeroTargets0859(enemy,range).ToList();var target=ChooseEnemyPriorityTarget0859(enemy,targets);
            if(target==null)return false;
            if(id=="E005"){AddStatus(target,"Weakened",0,0,"Goblin Shaman — Crude Hex",false,null,"until end of next activation");return LogEnemyAbility0859(enemy,"Crude Hex",target);}
            if(id=="E006"){var ally=EnemyAlliesWithin0859(enemy,0,IsGoblinEnemy).OrderBy(e=>e.Health).FirstOrDefault();if(ally!=null&&!ally.Statuses.Any(s=>s.Name=="Guarded")){AddStatus(ally,"Guarded",0,0,"Goblin Captain — Hold the Line",true,null,"until start of Captain's next activation");return LogEnemyAbility0859(enemy,"Hold the Line",ally==null?null:target);}}
            if(id=="E007"){var beast=EnemyAlliesWithin0859(enemy,0,e=>EnemyTypeHas0859(e,"Beast")).FirstOrDefault();if(beast!=null){var bt=ChooseEnemyPriorityTarget0859(beast,EnemyHeroTargets0859(beast,AuthoredEnemyRange0859(beast)));if(bt!=null){ResolveEnemyMultiAttack0859(beast,new List<HeroState>{bt},0);return LogEnemyAbility0859(enemy,"Crack the Whip",bt);}}}
            if(id=="E008"){var allies=EnemyAlliesWithin0859(enemy,0,IsGoblinEnemy).Take(2).ToList();if(allies.Count>0){foreach(var a in allies)AddStatus(a,"Enemy Attack +1",0,1,"Goblin War Drummer — Rally the Tribe",true,null,"until end of round");return LogEnemyAbility0859(enemy,"Rally the Tribe",null);}}
            if(id=="E017"){AddStatus(target,"Weakened",0,0,"Goblin Witch Doctor — Vile Hex",false,null,"until end of next activation");AddStatus(target,"Vulnerable",0,0,"Goblin Witch Doctor — Vile Hex",false,null,"until end of next activation");return LogEnemyAbility0859(enemy,"Vile Hex",target);}
            if(id=="E019"){AddStatus(target,"Weakened",0,0,"Goblin High Shaman — Curse of Weakness",false,null,"until end of next activation");AddStatus(target,"Vulnerable",0,0,"Goblin High Shaman — Curse of Weakness",false,null,"until end of next activation");return LogEnemyAbility0859(enemy,"Curse of Weakness",target);}
            if(id=="E022"){AddStatus(target,"Enemy Move Surcharge",0,0,"Giant Spider — Web Shot",false,null,"until consumed");return LogEnemyAbility0859(enemy,"Web Shot",target);}
            if(id=="E023"&&EnemyHasKey0859(enemy,"BROOD_CALL")&&!EnemyHazardPresent0859(enemy.LocationId,"Web Zone")){PlaceEnemyHazard0859(enemy,"Web Zone",enemy.LocationId,0);return LogEnemyAbility0859(enemy,"Massive Web",null);}
            if(id=="E026"&&targets.Count(h=>h.LocationId==enemy.LocationId)>=2){foreach(var h in targets.Where(h=>h.LocationId==enemy.LocationId))AddStatus(h,"Weakened",0,0,"Giant Bat — Echo Screech",false,null,"until end of next activation");return LogEnemyAbility0859(enemy,"Echo Screech",null);}
            if(id=="E031"&&GroupAtLocationInRange0859(enemy,range,out var locId,2)){foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==locId).ToList()){ApplyHeroDamageNow(h,3,"Forest Spirit — Nature's Wrath",false,enemy);if(Locations.TryGetValue(locId,out var l)&&l.HasPrintedTag("Forest"))AddStatus(h,"Rooted",0,0,"Nature's Wrath",false,null,"until end of next activation");}return LogEnemyAbility0859(enemy,"Nature's Wrath",null);}
            if(id=="E035"){AddStatus(target,"Weakened",0,0,"Masked Acolyte — Black Wax Hex",false,null,"until end of next turn");return LogEnemyAbility0859(enemy,"Black Wax Hex",target);}
            if(id=="E038"&&target.Actions<=1){AddStatus(target,"Rooted",0,0,"Corrupt Watchman — Arrest",false,null,"until end of next turn");return LogEnemyAbility0859(enemy,"Arrest",target);}
            if(id=="E046"&&EnemyUseKey0859(enemy,"COMMAND_KNEEL")){AddStatus(target,"Stunned",0,0,"Crown Herald — Command Kneel",false,null,"until end of next activation");return LogEnemyAbility0859(enemy,"Command Kneel",target);}
            if(id=="E053"&&EnemyUseKey0859(enemy,"SET_CHARGE")&&!EnemyHazards0859.Any(h=>h.Type=="Charge"&&h.LocationId==enemy.LocationId)){PlaceEnemyHazard0859(enemy,"Charge",enemy.LocationId,Round+1);return true;}
            if(id=="E055"&&EnemyUseKey0859(enemy,"RUNE_PULSE")){foreach(var h in targets.Where(h=>h.LocationId==enemy.LocationId).ToList()){RemoveStatus(h.Statuses,"Guarded",h.DisplayName,"Runic Sentry Rune Pulse");ApplyHeroDamageNow(h,1,"Runic Sentry — Rune Pulse",false,enemy);}return LogEnemyAbility0859(enemy,"Rune Pulse",null);}
            if(id=="E060"){var connected=ParticipatingHeroes.Where(h=>!h.IsDefeated&&GraphDistance(enemy.LocationId,h.LocationId)==1).OrderBy(h=>h.Health).FirstOrDefault();if(connected!=null){string from=connected.LocationId;connected.LocationId=enemy.LocationId;if(HeroAvatars.TryGetValue(connected.Id,out var av)&&Locations.TryGetValue(enemy.LocationId,out var dl))av.transform.position=dl.Position;AddLog($"ENEMY FORCED MOVE — Dorga Chainforged reels {connected.DisplayName} from {Locations[from].Name} into {Locations[enemy.LocationId].Name}.");return true;}}
            if(id=="E072"){RemoveStatus(target.Statuses,"Guarded",target.DisplayName,"Spore Witch Choking Spores");AddStatus(target,"Weakened",0,0,"Spore Witch — Choking Spores",false,null,"until end of next turn");return LogEnemyAbility0859(enemy,"Choking Spores",target);}
            if(id=="E079"&&targets.Any(h=>h.LocationId==enemy.LocationId)){foreach(var h in targets.Where(h=>h.LocationId==enemy.LocationId))ApplyHeroDamageNow(h,1,"Mycelial Horror — Bloom Burst",false,enemy);return LogEnemyAbility0859(enemy,"Bloom Burst",null);}
            if(id=="E081"){AddStatus(target,"Rooted",0,0,"Bound Dryad — Root Snare",false,null,"until end of next turn");return LogEnemyAbility0859(enemy,"Root Snare",target);}
            if(id=="E085"&&EnemyUseKey0859(enemy,"COMMAND_FLOWERING")){string loc=BestConnectedEnemySpawnLocation0859(enemy,"Blighted","Hollow")??enemy.LocationId;SpawnEnemyFromAbility0859(enemy,"E069",loc,"Command Flowering");return true;}
            if(id=="E091"){RemoveStatus(target.Statuses,"Guarded",target.DisplayName,"Salt Witch Salt Curse");AddStatus(target,"Enemy Healing -1",0,0,"Salt Witch — Salt Curse",false,null,"until end of next turn");return LogEnemyAbility0859(enemy,"Salt Curse",target);}
            if(id=="E113"){RemoveStatus(target.Statuses,"Inspired",target.DisplayName,"Crownscar Mage Crownscar");AddStatus(target,"Weakened",0,0,"Crownscar Mage — Crownscar",false,null,"until end of next turn");return LogEnemyAbility0859(enemy,"Crownscar",target);}
            if(id=="E098"&&TryMoveHeroByEnemy0859(enemy,target,true,"Undertow"))return true;
            if((id=="E106"||id=="E125")&&targets.Count>=2){foreach(var h in targets)AddStatus(h,"Weakened",0,0,$"{enemy.DisplayName} — control pulse",false,null,"until end of next turn");return LogEnemyAbility0859(enemy,id=="E106"?"Deep Pulse":"Solar Gravity",null);}
            if((id=="E082"||id=="E084"||id=="E121"||id=="E066")&&targets.Count(h=>h.LocationId==enemy.LocationId)>=2){ResolveEnemyMultiAttack0859(enemy,targets.Where(h=>h.LocationId==enemy.LocationId).Take(2).ToList(),-1);return true;}
            if((id=="E050"||id=="E068"||id=="E087"||id=="E106"||id=="E123"||id=="E125")&&targets.Count>=2){ResolveEnemyMultiAttack0859(enemy,targets.Take(2).ToList(),-1);return true;}
            return false;
        }
        private bool LogEnemyAbility0859(EnemyState enemy,string ability,HeroState target){AddLog($"ENEMY ABILITY — {enemy.DisplayName} uses {ability}{(target!=null?$" on {target.DisplayName}":string.Empty)}.");return true;}
        private bool GroupAtLocationInRange0859(EnemyState enemy,int range,out string locationId,int minimum)
        {
            locationId=ParticipatingHeroes.Where(h=>!h.IsDefeated&&GraphDistance(enemy.LocationId,h.LocationId)<=range).GroupBy(h=>h.LocationId).Where(g=>g.Count()>=minimum).OrderByDescending(g=>g.Count()).Select(g=>g.Key).FirstOrDefault();return !string.IsNullOrEmpty(locationId);
        }
        private void ResolveEnemyMultiAttack0859(EnemyState enemy,List<HeroState> targets,int attackModifier)
        {
            foreach(var h in targets.Where(h=>h!=null&&!h.IsDefeated).ToList()){int die=UnityEngine.Random.Range(1,7);int atk=Mathf.Max(0,EffectiveEnemyAttack(enemy)+attackModifier);int ignore=EnemyAttackArmorIgnore0859(enemy,h);int armor=Mathf.Max(0,EffectiveHeroArmor(h)-ignore);int dmg=Mathf.Max(0,die+atk-armor);AddLog($"ENEMY MULTIATTACK — {enemy.DisplayName} → {h.DisplayName}: d6 {die} + Attack {atk} − Armor {armor} = {dmg}.");int dealt=ApplyHeroDamageNow(h,dmg,$"{enemy.DisplayName} multiattack",false,enemy);OnEnemyAttackResolved0859(enemy,h,dealt);}
        }

        private string BestConnectedEnemySpawnLocation0859(EnemyState enemy,params string[] preferredTags)
        {
            if(enemy==null||!Locations.TryGetValue(enemy.LocationId,out var current))return null;return current.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed).OrderByDescending(id=>preferredTags.Count(tag=>Locations[id].HasPrintedTag(tag))).ThenBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }
        private EnemyState SpawnEnemyFromAbility0859(EnemyState source,string templateId,string locationId,string reason)
        {
            EnemyState spawned=null;
            if(source?.QuestSpawned==true)spawned=SpawnQuestEnemy0829(templateId,locationId,$"{source.DisplayName} — {reason}");
            else if(source?.EncounterSpawned==true){var encounter=EncounterForEnemy(source);if(encounter!=null&&Locations.TryGetValue(locationId,out var loc))spawned=SpawnEncounterEnemy(templateId,loc,encounter);}
            if(spawned==null&&SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId,out var t))
            {string id=$"{templateId}_0859_{Guid.NewGuid().ToString("N").Substring(0,6)}";spawned=new EnemyState{Id=id,TemplateId=templateId,DisplayName=t.Name,MaxHealth=t.Health,Health=t.Health,Attack=t.Attack,Armor=t.Armor,LocationId=locationId,IsBoss=t.Boss};Enemies[id]=spawned;}
            if(spawned!=null){AddLog($"ENEMY SUMMON — {source?.DisplayName??"Enemy effect"} creates {spawned.DisplayName} at {(Locations.TryGetValue(locationId,out var l)?l.Name:locationId)} ({reason}).");var builder=UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>();if(builder!=null)builder.EnsureRuntimeEnemySpawns();}
            return spawned;
        }

        private void OnEnemyAttackResolved0859(EnemyState enemy,HeroState target,int dealt)
        {
            if(enemy==null||target==null||dealt<=0)return;
            if(!EnemyHasKey0859(enemy,EnemyRoundKey0859("DAMAGED_HERO")))enemy.UsedKeys0859.Add(Enemy0859Prefix+EnemyRoundKey0859("DAMAGED_HERO"));
            string id=enemy.TemplateId??string.Empty;
            if(id=="E010"||id=="E024"||id=="E032")TryMoveHeroByEnemy0859(enemy,target,false,id=="E024"?"Boar Charge":"Crushing Command");
            if(id=="E011"){RemoveStatus(target.Statuses,"Guarded",target.DisplayName,"Goblin Saboteur Dirty Tricks");AddStatus(target,"Defensive Bonuses Suppressed",0,0,"Dirty Tricks",false,null,"until end of next activation");}
            if(id=="E020")AddStatus(target,"Enemy Move Surcharge",0,0,"Wolf — Hamstring Bite",false,null,"until consumed");
            if(id=="E021"&&enemy.MovementStepsThisActivation0859>=2)AddStatus(target,"Knocked Down",0,0,"Dire Wolf — Savage Pounce",false,null,"until end of next activation");
            if(id=="E022")AddStatus(target,"Poisoned",0,0,"Giant Spider — Venomous Bite",false,null,"until end of next turn");
            if(id=="E023"){bool already=target.HasStatus("Poisoned");AddStatus(target,"Poisoned",0,0,"Spider Matriarch — Paralyzing Venom",false,null,"until end of next turn");if(already)AddStatus(target,"Stunned",0,0,"Spider Matriarch — Paralyzing Venom",false,null,"until end of next activation");}
            if(id=="E027"){AddStatus(target,"Rooted",0,0,"Giant Snake — Constrict",false,null,"until end of next activation");AddStatus(target,"Poisoned",0,0,"Giant Snake — Venomous Fangs",false,null,"until end of next turn");}
            if(id=="E028")AddStatus(target,"Bleeding",0,0,"Owlbear — Rending Claws",false,null,"until end of next activation");
            if(id=="E029"){AddStatus(target,"Enemy Move Surcharge",0,0,"Warg — Crippling Bite",false,null,"until consumed");AddStatus(target,"Vulnerable",0,0,"Warg — Crippling Bite",false,null,"until end of next activation");}
            if(id=="E034"){target.FreeMoveReady=false;AddStatus(target,"Free Move Suppressed",0,0,"Crossbow Thug — Suppressing Bolt",false,null,"until end of next turn");}
            if(id=="E039")AddStatus(target,"Poisoned",0,0,"Sewer Stalker — Filth Bite",false,null,"until end of next turn");
            if(id=="E041")AddStatus(target,"Enemy Move Surcharge",0,0,"Crypt Archer — Pinning Shot",false,null,"until consumed");
            if(id=="E042")AddStatus(target,"Slowed",0,0,"Wraith — Cold Touch",false,null,"until end of next turn");
            if(id=="E045"){RemoveStatus(target.Statuses,"Inspired",target.DisplayName,"Veilspawn Unmake Courage");RemoveStatus(target.Statuses,"Guarded",target.DisplayName,"Veilspawn Unmake Courage");}
            if(id=="E052"&&dealt>=4&&Locations.TryGetValue(target.LocationId,out var cave))PlaceEnemyHazard0859(enemy,"Cave-In",cave.Id,0);
            if(id=="E056"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("HAMMERFALL")))PlaceEnemyHazard0859(enemy,"Cave-In",target.LocationId,0);
            if(id=="E074")RemoveStatus(target.Statuses,"Inspired",target.DisplayName,"Blightheart Matron Rotting Slam");
            if(id=="E077"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("GRASPING_CROWN")))AddStatus(target,"Rooted",0,0,"Yselle Thornmother — Grasping Crown",false,null,"until end of next turn");
            if(id=="E080"&&target.HasStatus("Inspired")&&target.Hand.Count>0){target.Discard.Add(target.Hand[0].Id);target.Hand.RemoveAt(0);AddLog($"ENEMY EFFECT — {target.DisplayName} discards 1 card to Hollow Warden Spirit Lash.");}
            if(id=="E083"){var beneficial=target.Statuses.FirstOrDefault(s=>s.Beneficial);if(beneficial!=null){target.Statuses.Remove(beneficial);AddLog($"ENEMY EFFECT — {target.DisplayName} loses {beneficial.Name} to Rot Spirit Withering Touch.");}}
            if(id=="E086"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("WITHER_BLESSING"))){RemoveStatus(target.Statuses,"Guarded",target.DisplayName,"Wither Blessing");RemoveStatus(target.Statuses,"Inspired",target.DisplayName,"Wither Blessing");}
            if(id=="E092")AddStatus(target,"Enemy Move Surcharge",0,0,"Barnacle Brute — Crushing Grip",false,null,"until consumed");
            if(id=="E100"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("CRUSHING_CHORUS")))RemoveStatus(target.Statuses,"Guarded",target.DisplayName,"Crushing Chorus");
            if(id=="E102")AddStatus(target,"Enemy Move Surcharge",0,0,"Kraken Spawn — Grapple",false,null,"until consumed");
            if(id=="E105"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("BLACK_TIDE")))RemoveStatus(target.Statuses,"Inspired",target.DisplayName,"Black-Tide Cutlass");
            if(id=="E108")AddStatus(target,"Enemy Healing -1",0,0,"Glass Scorpion — Venom Tail",false,null,"until end of next turn");
            if(id=="E124"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("SUNFALL_BRAND"))){RemoveStatus(target.Statuses,"Guarded",target.DisplayName,"Sunfall Brand");RemoveStatus(target.Statuses,"Inspired",target.DisplayName,"Sunfall Brand");}
        }

        private bool TryMoveHeroByEnemy0859(EnemyState enemy,HeroState target,bool towardHazard,string source)
        {
            if(enemy==null||target==null||!Locations.TryGetValue(target.LocationId,out var current))return false;if(TryAutoPreventForcedMove0860(enemy,target,source))return true;if(target.HasStatus("Ignore Forced Move")){RemoveStatus(target.Statuses,"Ignore Forced Move",target.DisplayName,source);AddLog($"HERO CARD — {target.DisplayName} ignores the forced movement from {source}.");return true;}var candidates=current.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed&&CanEnterQuestLocation0832(id)).ToList();if(candidates.Count==0)return false;
            string next;
            if(towardHazard)next=candidates.OrderByDescending(id=>Locations[id].HasPrintedTag("Flooded")||Locations[id].HasPrintedTag("Sea")||Locations[id].HasPrintedTag("Coast")).ThenBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            else next=candidates.OrderByDescending(id=>GraphDistance(enemy.LocationId,id)).ThenBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if(string.IsNullOrEmpty(next))return false;string from=target.LocationId;target.LocationId=next;if(HeroAvatars.TryGetValue(target.Id,out var av))av.transform.position=Locations[next].Position;AddLog($"ENEMY FORCED MOVE — {source}: {target.DisplayName} moves {Locations[from].Name} → {Locations[next].Name}.");return true;
        }

        private void PlaceEnemyHazard0859(EnemyState source,string type,string locationId,int triggerRound)
        {
            if(string.IsNullOrEmpty(type)||string.IsNullOrEmpty(locationId))return;if((type=="Barricade"||type=="Snare"||type=="Web Zone")&&EnemyHazards0859.Any(h=>h.Type==type&&h.LocationId==locationId))return;
            var h=new EnemyHazardState0859{HazardId=$"EH0859_{Guid.NewGuid().ToString("N").Substring(0,8)}",Type=type,SourceEnemyId=source?.Id,LocationId=locationId,CreatedRound=Round,TriggerRound=triggerRound};EnemyHazards0859.Add(h);AddLog($"ENEMY HAZARD — {source?.DisplayName??"Enemy"} places {type} at {(Locations.TryGetValue(locationId,out var l)?l.Name:locationId)}.");
        }
        private bool EnemyHazardPresent0859(string locationId,string type)=>EnemyHazards0859.Any(h=>h.LocationId==locationId&&string.Equals(h.Type,type,StringComparison.OrdinalIgnoreCase));
        private int EnemyHeroMoveSurcharge0859(HeroState hero,string destinationId)
        {
            if(hero==null)return 0;int n=hero.HasStatus("Enemy Move Surcharge")?1:0;if(EnemyHazardPresent0859(destinationId,"Barricade"))n++;return n;
        }
        private void ConsumeEnemyHeroMoveSurcharge0859(HeroState hero)
        {if(hero!=null&&hero.HasStatus("Enemy Move Surcharge"))RemoveStatus(hero.Statuses,"Enemy Move Surcharge",hero.DisplayName,"enemy movement penalty consumed");}
        private int EnemyHeroArmorModifier0859(HeroState hero)
        {
            if(hero==null)return 0;int mod=hero.HasStatus("Defensive Bonuses Suppressed")?-1:0;if(EnemyHazardPresent0859(hero.LocationId,"Web Zone"))mod-=1;return mod;
        }
        private int ModifyEnemyHealing0859(HeroState target,int amount)
        {
            if(target==null||amount<=0)return amount;int mod=target.HasStatus("Enemy Healing -1")?-1:0;
            if(Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==target.LocationId&&(e.TemplateId=="E079"||e.TemplateId=="E099"||e.TemplateId=="E117"||e.TemplateId=="E118")))mod-=1;
            return Mathf.Max(0,amount+mod);
        }
        private void OnEnemyHeroEnteredLocation0859(HeroState hero)
        {
            if(hero==null)return;
            foreach(var e in Enemies.Values.Where(x=>!x.Defeated&&x.TemplateId=="E003"&&x.LocationId==hero.LocationId).ToList())
                if(EnemyUseKey0859(e,EnemyRoundKey0859("BRACE_FOR_CHARGE")))AddStatus(e,"Enemy Armor +1",0,0,"Goblin Spearman — Brace for Charge",true,null,"until start of next activation");
        }

        private void OnEnemyHazardHeroEntered0859(HeroState hero)
        {
            if(hero==null)return;var snare=EnemyHazards0859.FirstOrDefault(h=>h.LocationId==hero.LocationId&&h.Type=="Snare");if(snare!=null){ApplyHeroDamageNow(hero,2,"Enemy Snare",false,Enemies.TryGetValue(snare.SourceEnemyId??string.Empty,out var e)?e:null);AddStatus(hero,"Movement Ended",0,0,"Enemy Snare",false,null,"until end of turn");EnemyHazards0859.Remove(snare);}
            if(EnemyHazardPresent0859(hero.LocationId,"Fire"))ApplyHeroDamageNow(hero,2,"Enemy Fire Token",false,null);
            if(EnemyHazardPresent0859(hero.LocationId,"Web Zone"))AddStatus(hero,"Movement Ended",0,0,"Enemy Web Zone",false,null,"until end of turn");
            if(Locations.TryGetValue(hero.LocationId,out var l)&&l.UsedKeys0858.Any(k=>k==Enemy0859Prefix+"SHARD_HAZARD"))ApplyHeroDamageNow(hero,1,"Glass Shard Hazard");
        }
        private void ResolveEnemyEndPhaseHazards0859()
        {
            foreach(var h in EnemyHazards0859.Where(x=>x.Type=="Charge"&&x.TriggerRound>0&&x.TriggerRound<=Round).ToList())
            {foreach(var hero in ParticipatingHeroes.Where(x=>!x.IsDefeated&&x.LocationId==h.LocationId).ToList())ApplyHeroDamageNow(hero,2,"Ashen Compact Charge");EnemyHazards0859.Remove(h);}
        }

        private bool TryEnemyPlaceStandardHazardInsteadOfAttack0859(EnemyState enemy)
        {
            if(enemy==null)return false;string id=enemy.TemplateId??string.Empty;
            if(id=="E012"){string loc=BestConnectedEnemySpawnLocation0859(enemy)??enemy.LocationId;if(!EnemyHazardPresent0859(loc,"Snare")){PlaceEnemyHazard0859(enemy,"Snare",loc,0);return true;}return false;}
            if(id=="E014"){string loc=BestConnectedEnemySpawnLocation0859(enemy)??enemy.LocationId;if(!EnemyHazardPresent0859(loc,"Fire")){PlaceEnemyHazard0859(enemy,"Fire",loc,0);return true;}return false;}
            if(id=="E016"){if(!EnemyHazardPresent0859(enemy.LocationId,"Barricade")){PlaceEnemyHazard0859(enemy,"Barricade",enemy.LocationId,0);return true;}return false;}
            if(id=="E023"&&EnemyUseKey0859(enemy,"BROOD_CALL")){var a=BestConnectedEnemySpawnLocation0859(enemy)??enemy.LocationId;SpawnEnemyFromAbility0859(enemy,"E022",a,"Brood Call");var b=BestConnectedEnemySpawnLocation0859(enemy)??enemy.LocationId;SpawnEnemyFromAbility0859(enemy,"E022",b,"Brood Call");return true;}
            if(id=="E104"&&EnemyUseKey0859(enemy,"STORM_SURGE")){string loc=BestConnectedEnemySpawnLocation0859(enemy)??enemy.LocationId;PlaceEnemyHazard0859(enemy,"Flood",loc,0);return true;}
            return false;
        }


        private bool EnemyShouldHoldPosition0859(EnemyState enemy)
        {
            if(enemy==null)return false;string b=EnemyTemplate0859(enemy)?.Behavior??string.Empty;
            if(b.IndexOf("Hold position",StringComparison.OrdinalIgnoreCase)>=0)return true;
            if((b.IndexOf("remain in",StringComparison.OrdinalIgnoreCase)>=0||b.IndexOf("never voluntarily leave",StringComparison.OrdinalIgnoreCase)>=0)&&IsGuardingQuestObjective0859(enemy))return true;
            return false;
        }
        private int TryRangedReposition0859(EnemyState enemy)
        {
            var t=EnemyTemplate0859(enemy);if(enemy==null||t==null)return 0;string profile=(t.BehaviorProfile??string.Empty)+" "+(t.Role??string.Empty);if(profile.IndexOf("Ranged",StringComparison.OrdinalIgnoreCase)<0&&profile.IndexOf("Artillery",StringComparison.OrdinalIgnoreCase)<0)return 0;
            if(!ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId)||!Locations.TryGetValue(enemy.LocationId,out var cur))return 0;int range=AuthoredEnemyRange0859(enemy);if(range<=0)return 0;
            var next=cur.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed&&!ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==id)&&ParticipatingHeroes.Any(h=>!h.IsDefeated&&GraphDistance(id,h.LocationId)<=range))
                .OrderByDescending(id=>ParticipatingHeroes.Where(h=>!h.IsDefeated).Min(h=>GraphDistance(id,h.LocationId))).ThenBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if(string.IsNullOrEmpty(next))return 0;string from=cur.Name;enemy.LocationId=next;enemy.MovementStepsThisActivation0859++;if(EnemyAvatars.TryGetValue(enemy.Id,out var av))av.transform.position=Locations[next].Position;AddLog($"ENEMY REPOSITION — {enemy.DisplayName} withdraws {from} → {Locations[next].Name} to maintain range.");return 1;
        }
        private bool MoveEnemyAwayFromHeroes0859(EnemyState enemy,string source)
        {
            if(enemy==null||!Locations.TryGetValue(enemy.LocationId,out var cur))return false;var heroes=ParticipatingHeroes.Where(h=>!h.IsDefeated).ToList();if(heroes.Count==0)return false;int here=heroes.Min(h=>GraphDistance(enemy.LocationId,h.LocationId));var next=cur.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed).OrderByDescending(id=>heroes.Min(h=>GraphDistance(id,h.LocationId))).ThenBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault(id=>heroes.Min(h=>GraphDistance(id,h.LocationId))>=here);if(string.IsNullOrEmpty(next))return false;string from=enemy.LocationId;enemy.LocationId=next;if(EnemyAvatars.TryGetValue(enemy.Id,out var av))av.transform.position=Locations[next].Position;AddLog($"ENEMY MOVE — {source}: {enemy.DisplayName} slips from {Locations[from].Name} to {Locations[next].Name}.");return true;
        }

        private void OnEnemyActivationCompleted0859(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return;string id=enemy.TemplateId??string.Empty;
            if(id=="E010"&&EnemyUseKey0859(enemy,"CALL_REINFORCEMENTS")&&Enemies.Values.Count(e=>!e.Defeated&&IsGoblinEnemy(e)&&e.Id!=enemy.Id)<3)
            {string loc=BestConnectedEnemySpawnLocation0859(enemy)??enemy.LocationId;SpawnEnemyFromAbility0859(enemy,"E001",loc,"Call Reinforcements");SpawnEnemyFromAbility0859(enemy,"E001",loc,"Call Reinforcements");}
            if(id=="E032"&&CurrentQuestId0829!="Q012"&&EnemyUseKey0859(enemy,EnemyRoundKey0859("CALL_HORDE"))&&Enemies.Values.Count(e=>!e.Defeated&&IsGoblinEnemy(e)&&e.Id!=enemy.Id)<3)
            {bool heroHere=ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId);SpawnEnemyFromAbility0859(enemy,heroHere?"E002":"E001",enemy.LocationId,"Call the Horde");}
            if(id=="E074"&&Enemies.Values.Count(e=>!e.Defeated&&e.TemplateId=="E069")<2)SpawnEnemyFromAbility0859(enemy,"E069",BestConnectedEnemySpawnLocation0859(enemy)??enemy.LocationId,"Broodcall");
            if(id=="E097"&&!Enemies.Values.Any(e=>!e.Defeated&&e.TemplateId=="E096"))SpawnEnemyFromAbility0859(enemy,"E096",enemy.LocationId,"Summon Thrall");
            if(id=="E013"||id=="E026"||id=="E053"||id=="E058"||id=="E073"||id=="E089")MoveEnemyAwayFromHeroes0859(enemy,id=="E013"?"Hit and Run":"authored retreat");
        }

#if UNITY_EDITOR
        public int DebugEnemyCount0859()=>SearchEncounterContent0820.EnemyTemplates.Values.Count(t=>IsEnemyExecutable0859(t.Id));
        public EnemyState DebugSpawnEnemy0859(string templateId,string authoredLocationId,string runtimeId=null)
        {
            var t=EnemyTemplate0859(templateId);var l=DebugLocationByAuthored0858Internal(authoredLocationId);if(t==null||l==null)return null;string id=runtimeId??$"DBG_{templateId}_{Guid.NewGuid().ToString("N").Substring(0,5)}";var e=new EnemyState{Id=id,TemplateId=t.Id,DisplayName=t.Name,MaxHealth=t.Health,Health=t.Health,Attack=t.Attack,Armor=t.Armor,LocationId=l.Id,IsBoss=t.Boss};Enemies[id]=e;return e;
        }
        public int DebugEnemyRange0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?EnemyAttackRangeConnections0830(e):-1;
        public int DebugEnemyAuthoredMovement0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?AuthoredEnemyMovement0859(e):-1;
        public int DebugEnemyMovement0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?EnemyMovementAllowance(e):-1;
        public int DebugEnemyAttack0859(string enemyId,string heroId=null)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?EffectiveEnemyAttack(e)+(Heroes.TryGetValue(heroId??string.Empty,out var h)?EnemyAttackModifier0859(e,h)-EnemyAttackModifier0859(e,null):0):-999;
        public int DebugEnemyArmor0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?EffectiveEnemyArmor(e):-999;
        public int DebugEnemyHealthFloor0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?EnemyHealthFloor0859(e):-1;
        public bool DebugEnemyIgnoresDifficult0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)&&EnemyIgnoresDifficultTerrain0859(e);
        public bool DebugEnemyImmovable0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)&&EnemyCannotBeMoved0859(e);
        public void DebugPrepareEnemyPhase0859()=>PrepareEnemyPhase0859();
        public void DebugPrepareEnemyActivation0859(string enemyId){if(Enemies.TryGetValue(enemyId??string.Empty,out var e))PrepareEnemyActivation0859(e);}
        public bool DebugEnemyReplacementAbility0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)&&TryResolveEnemyReplacementAbility0859(e);
        public bool DebugEnemyHazardAbility0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)&&TryEnemyPlaceStandardHazardInsteadOfAttack0859(e);
        public int DebugEnemyHazardCount0859(string type=null)=>EnemyHazards0859.Count(h=>string.IsNullOrEmpty(type)||string.Equals(h.Type,type,StringComparison.OrdinalIgnoreCase));
        public bool DebugEnemyHazardPresent0859(string authoredLocationId,string type){var l=DebugLocationByAuthored0858Internal(authoredLocationId);return l!=null&&EnemyHazardPresent0859(l.Id,type);}
        public int DebugEnemyHeroMoveSurcharge0859(string heroId,string authoredDestinationId){var l=DebugLocationByAuthored0858Internal(authoredDestinationId);return Heroes.TryGetValue(heroId??string.Empty,out var h)&&l!=null?EnemyHeroMoveSurcharge0859(h,l.Id):-1;}
        public int DebugModifyEnemyHealing0859(string heroId,int amount)=>Heroes.TryGetValue(heroId??string.Empty,out var h)?ModifyEnemyHealing0859(h,amount):-1;
        public int DebugModifyEnemyIncomingDamage0859(string enemyId,int damage,bool ranged=false,string heroId="Garruk",bool fire=false)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?ModifyEnemyIncomingDamage0859(e,damage,Heroes.TryGetValue(heroId??string.Empty,out var h)?h:null,fire?new CardData{Id="DBG-FIRE",Name="Validation Fire",Traits="Fire"}:null,ranged,fire?"Fire validation":"0.8.59 validation"):-1;
        public void DebugApplyEnemyPostAttack0859(string enemyId,string heroId,int dealt=1){if(Enemies.TryGetValue(enemyId??string.Empty,out var e)&&Heroes.TryGetValue(heroId??string.Empty,out var h))OnEnemyAttackResolved0859(e,h,dealt);}
        public void DebugSetEnemyMovementSteps0859(string enemyId,int steps){if(Enemies.TryGetValue(enemyId??string.Empty,out var e))e.MovementStepsThisActivation0859=Mathf.Max(0,steps);}
        public void DebugSetEnemyHealth0859(string enemyId,int hp){if(Enemies.TryGetValue(enemyId??string.Empty,out var e))e.Health=Mathf.Clamp(hp,0,e.MaxHealth);}
        public void DebugResolveEnemyHazardEntry0859(string heroId){if(Heroes.TryGetValue(heroId??string.Empty,out var h))OnEnemyHazardHeroEntered0859(h);}
        public void DebugEnemyHeroEnteredLocation0859(string heroId){if(Heroes.TryGetValue(heroId??string.Empty,out var h))OnEnemyHeroEnteredLocation0859(h);}
        public void DebugClearEnemyRuntime0859(){EnemyHazards0859.Clear();foreach(var e in Enemies.Values){e.UsedKeys0859?.Clear();e.MovementStepsThisActivation0859=0;e.FocusHeroId0859=null;}}
        public void DebugResolveEnemyEndPhaseHazards0859()=>ResolveEnemyEndPhaseHazards0859();
        public string DebugEnemyFocusHero0859(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)?e.FocusHeroId0859:null;
        public void DebugRemoveEnemies0859(){foreach(var id in Enemies.Keys.Where(id=>id.StartsWith("DBG_",StringComparison.OrdinalIgnoreCase)||id.IndexOf("_0859_",StringComparison.OrdinalIgnoreCase)>=0).ToList())Enemies.Remove(id);EnemyHazards0859.Clear();}
#endif
    }
}
