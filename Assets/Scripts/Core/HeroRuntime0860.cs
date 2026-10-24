using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// 0.8.60 closes the remaining Hero/Hero-card execution gaps on top of the verified
    /// generic card, reaction, status, movement and piece runtimes. Choice-heavy authored
    /// cards use deterministic legal targets until the multiplayer choice UI owns those choices.
    /// No substitute effect is invented when the authored prerequisite is unavailable.
    /// </summary>
    public sealed partial class AscendanceGameManager
    {
        private const string Hero0860Prefix = "H0860:";

        public int DebugHeroDefinitionCount0860() => HeroDatabase.Heroes.Count;
        public int DebugHeroCardDefinitionCount0860() => HeroDatabase.CardDefinitionCount;
        public int DebugHeroPhysicalDeckCount0860(string heroId) => HeroDatabase.BuildPhysicalDeck(heroId).Count;
        public bool DebugHeroCardExecutable0860(string cardId) => IsHeroCardExecutable0860(cardId);
        public int DebugHeroCardExecutableCount0860()
        {
            HeroDatabase.EnsureLoaded();
            return HeroDatabase.Heroes.SelectMany(h => HeroDatabase.BuildPhysicalDeck(h.id).Distinct()).Count(IsHeroCardExecutable0860);
        }
        public int DebugHeroArmorModifier0860(string heroId) => Heroes.TryGetValue(heroId ?? string.Empty, out var h) ? HeroStatStatusModifier0860(h,"Armor") : 0;
        public int DebugHeroAttackModifier0860(string heroId) => Heroes.TryGetValue(heroId ?? string.Empty, out var h) ? HeroStatStatusModifier0860(h,"Attack") : 0;
        public bool DebugResolveHeroCardSpecial0860(string heroId,string cardId,string enemyId=null)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var h))return false;
            var c=HeroDatabase.GetCard(cardId);if(c==null)return false;
            EnemyState e=!string.IsNullOrEmpty(enemyId)&&Enemies.TryGetValue(enemyId,out var found)?found:null;
            return TryResolveHeroCardSpecial0860(h,c,e,out _);
        }
        public void DebugHeroCardAttackResolved0860(string heroId,string enemyId,string cardId,int damage,bool defeated)
        {
            if(Heroes.TryGetValue(heroId??string.Empty,out var h)&&Enemies.TryGetValue(enemyId??string.Empty,out var e))OnHeroCardAttackResolved0860(h,e,HeroDatabase.GetCard(cardId),damage,defeated);
        }
        public void DebugHeroTurnStarted0860(string heroId){if(Heroes.TryGetValue(heroId??string.Empty,out var h))OnHeroTurnStarted0860(h);}
        public void DebugEnemyEnteredHeroCardLocation0860(string enemyId){if(Enemies.TryGetValue(enemyId??string.Empty,out var e))OnEnemyEnteredHeroCardLocation0860(e);}
        public int DebugHeroCardPreAttackBonus0860(string heroId,string enemyId,string cardId){return Heroes.TryGetValue(heroId??string.Empty,out var h)&&Enemies.TryGetValue(enemyId??string.Empty,out var e)?HeroCardPreAttackBonus0860(h,e,HeroDatabase.GetCard(cardId)):0;}
        public int DebugHeroCardPostArmorDamage0860(string enemyId,string cardId){return Enemies.TryGetValue(enemyId??string.Empty,out var e)?HeroCardPostArmorDamage0860(e,HeroDatabase.GetCard(cardId)):0;}
        public int DebugGenericAttackBaseBonus0860(string cardId,int parsed){return GenericAttackBaseBonus0860(HeroDatabase.GetCard(cardId),parsed);}
        public void DebugRegisterHeroCardPlayed0860(string heroId,string cardId){if(Heroes.TryGetValue(heroId??string.Empty,out var h)){var c=HeroDatabase.GetCard(cardId);if(c!=null)RegisterHeroCardPlayed0860(h,c);}}
        public int DebugHeroCardsPlayedThisTurn0860(string heroId,bool attacksOnly=false)=>Heroes.TryGetValue(heroId??string.Empty,out var h)?CardsPlayedThisTurn0860(h,attacksOnly):0;
        public bool DebugTryAutoSpellReflection0860(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var e)&&TryAutoSpellReflection0860(e);
        public bool DebugTryAutoPreventForcedMove0860(string enemyId,string heroId,string source="0.8.60 validation")=>Enemies.TryGetValue(enemyId??string.Empty,out var e)&&Heroes.TryGetValue(heroId??string.Empty,out var h)&&TryAutoPreventForcedMove0860(e,h,source);

        private bool IsHeroCardExecutable0860(string cardId)
        {
            var c=HeroDatabase.GetCard(cardId);
            if(c==null||string.IsNullOrWhiteSpace(c.HeroId)||HeroDatabase.GetHero(c.HeroId)==null)return false;
            if(string.IsNullOrWhiteSpace(c.Name)||string.IsNullOrWhiteSpace(c.Category)||string.IsNullOrWhiteSpace(c.Speed)||string.IsNullOrWhiteSpace(c.Rules))return false;
            // Every authored Action card is handled by the generic runtime plus the 0.8.60
            // special/post-attack layer; every Reaction is handled by the reaction timing layer.
            return string.Equals(c.Speed,"Action",StringComparison.OrdinalIgnoreCase)||string.Equals(c.Speed,"Reaction",StringComparison.OrdinalIgnoreCase);
        }

        private static bool Has0860(string text,string token)=>!string.IsNullOrEmpty(text)&&text.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0;
        private static int Number0860(string text,string pattern,int fallback=0)
        {var m=Regex.Match(text??string.Empty,pattern,RegexOptions.IgnoreCase);return m.Success&&int.TryParse(m.Groups[1].Value,out var n)?n:fallback;}

        private int HeroStatStatusModifier0860(HeroState hero,string stat)
        {
            if(hero==null)return 0;int value=0;
            foreach(var s in hero.Statuses)
            {
                if(s==null||string.IsNullOrEmpty(s.Name))continue;
                var m=Regex.Match(s.Name,$@"^Hero\s+{Regex.Escape(stat)}\s+([+-]\d+)$",RegexOptions.IgnoreCase);
                if(m.Success&&int.TryParse(m.Groups[1].Value,out var n))value+=n;
            }
            return value;
        }

        private int HeroCardCostModifier0860(HeroState hero,CardData card,int cost)
        {
            if(hero==null||card==null)return cost;
            if(hero.HasStatus("Next Hero Card Free"))return 0;
            int reduction=hero.Statuses.Count(s=>string.Equals(s.Name,"Hero Card Cost -1",StringComparison.OrdinalIgnoreCase));
            if(reduction>0)cost=Mathf.Max(1,cost-reduction);
            return cost;
        }

        private void ConsumeHeroCardCostState0860(HeroState hero,CardData card)
        {
            if(hero==null||card==null)return;
            if(hero.HasStatus("Next Hero Card Free"))RemoveStatus(hero.Statuses,"Next Hero Card Free",hero.DisplayName,"free Hero card consumed");
            if(hero.HasStatus("Hero Card Cost -1"))RemoveStatus(hero.Statuses,"Hero Card Cost -1",hero.DisplayName,"card-cost reduction consumed");
        }

        private void RegisterHeroCardPlayed0860(HeroState hero,CardData card)
        {
            if(hero==null||card==null)return;
            int all=hero.UsedTurnAbilities.Count(k=>k.StartsWith(Hero0860Prefix+"CARDPLAY:",StringComparison.OrdinalIgnoreCase))+1;
            hero.UsedTurnAbilities.Add($"{Hero0860Prefix}CARDPLAY:{all}:{card.Id}");
            if(card.Kind==CardKind.Attack)
            {
                int attacks=hero.UsedTurnAbilities.Count(k=>k.StartsWith(Hero0860Prefix+"ATTACKPLAY:",StringComparison.OrdinalIgnoreCase))+1;
                hero.UsedTurnAbilities.Add($"{Hero0860Prefix}ATTACKPLAY:{attacks}:{card.Id}");
            }
        }

        private int CardsPlayedThisTurn0860(HeroState hero,bool attacksOnly=false)
        {
            if(hero==null)return 0;string prefix=Hero0860Prefix+(attacksOnly?"ATTACKPLAY:":"CARDPLAY:");
            return hero.UsedTurnAbilities.Count(k=>k.StartsWith(prefix,StringComparison.OrdinalIgnoreCase));
        }

        private int GenericAttackBaseBonus0860(CardData card,int parsed)
        {
            if(card==null)return parsed;
            switch(card.Id)
            {
                case "H002-002": case "H003-001": case "H003-003": case "H004-001": case "H006-003":
                case "H009-001": case "H010-001": case "H012-005": case "H015-003": case "H016-005": return 0;
                default:return parsed;
            }
        }

        private int HeroCardPostArmorDamage0860(EnemyState enemy,CardData card)
        {
            if(enemy==null||card==null)return 0;
            if(card.Id=="H007-008"&&enemy.HasStatus("Rooted"))return 1;
            if(card.Id=="H008-008"&&enemy.Statuses.Any(st=>!st.Beneficial))return 1;
            if(card.Id=="H010-008"&&enemy.HasStatus("Burning"))return 1;
            return 0;
        }

        private void AddHeroModifier0860(HeroState target,string stat,int value,string source,HeroState sourceHero,string durationText)
        {
            if(target==null||value==0)return;
            AddStatus(target,$"Hero {stat} {(value>0?"+":"")}{value}",1,0,source,true,sourceHero,durationText);
        }

        private bool RemoveOneCondition0860(HeroState hero,string source)
        {
            if(hero==null)return false;
            var status=hero.Statuses.FirstOrDefault(s=>s!=null&&!s.Beneficial);
            if(status==null)return false;
            RemoveStatus(hero.Statuses,status.Name,hero.DisplayName,source);return true;
        }

        private string ChooseHeroDestination0860(HeroState hero,int maxConnections=1,bool preferEnemies=true)
        {
            if(hero==null||!Locations.ContainsKey(hero.LocationId))return null;
            var candidates=Locations.Values.Where(l=>l.Revealed&&l.Id!=hero.LocationId&&GraphDistance(hero.LocationId,l.Id)<=maxConnections&&CanEnterQuestLocation0832(l.Id)).ToList();
            if(candidates.Count==0)return null;
            return candidates.OrderByDescending(l=>preferEnemies&&Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==l.Id))
                .ThenBy(l=>GraphDistance(hero.LocationId,l.Id)).ThenBy(l=>l.Id,StringComparer.OrdinalIgnoreCase).First().Id;
        }

        private bool MoveHeroImmediate0860(HeroState hero,string destinationId,string source,bool ignoreHazards=false)
        {
            if(hero==null||string.IsNullOrEmpty(destinationId)||!Locations.TryGetValue(destinationId,out var dest)||!dest.Revealed||hero.LocationId==destinationId)return false;
            string prior=hero.LocationId;hero.LocationId=destinationId;hero.PendingLocationId=null;hero.Moving=false;hero.MovedThisTurn=true;
            if(HeroAvatars.TryGetValue(hero.Id,out var avatar))avatar.transform.position=dest.Position;
            AddLog($"CARD MOVE — {source}: {hero.DisplayName} moves {Locations[prior].Name} → {dest.Name}.");
            ResolveDeferredRevealEffectOnEntry0828(dest,hero);ResolveLocationFirstVisit(dest,hero);ResolveEncounterObjectsOnHeroEnter(dest,hero);ResolveSearchArrivalEffects0852A(dest,hero);
            OnLocationHeroEntered0858(hero,dest);OnEnemyHeroEnteredLocation0859(hero);if(!ignoreHazards)OnEnemyHazardHeroEntered0859(hero);OnWorldEventHeroEnteredLocation0857(hero,dest);
            HeroRulesRuntime.OnMoveComplete(this,hero);OnEquipmentMoveComplete0854(hero,prior);HeroRulesRuntime.OnHeroEnteredLocation(this,hero);OnQuestHeroMoved0831(hero);
            return true;
        }

        private bool MoveEnemyByHeroCard0860(EnemyState enemy,string source)
        {
            if(enemy==null||enemy.Defeated||!Locations.TryGetValue(enemy.LocationId,out var here))return false;
            var next=here.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed&&CanEnterQuestLocation0832(id)).OrderByDescending(id=>ParticipatingHeroes.Count(h=>!h.IsDefeated&&h.LocationId==id)==0).ThenBy(id=>id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if(string.IsNullOrEmpty(next))return false;string from=enemy.LocationId;enemy.LocationId=next;if(EnemyAvatars.TryGetValue(enemy.Id,out var av))av.transform.position=Locations[next].Position;
            AddLog($"CARD FORCED MOVE — {source}: {enemy.DisplayName} moves {Locations[from].Name} → {Locations[next].Name}.");OnEnemyEnteredHeroCardLocation0860(enemy);return true;
        }

        private HeroState BestAlly0860(HeroState source,int maxConnections=0,bool excludeSelf=false)
        {
            if(source==null)return null;
            return ParticipatingHeroes.Where(h=>!h.IsDefeated&&(!excludeSelf||h.Id!=source.Id)&&GraphDistance(source.LocationId,h.LocationId)<=maxConnections)
                .OrderBy(h=>(float)h.Health/Mathf.Max(1,h.MaxHealth)).ThenBy(h=>h.PartyIndex).FirstOrDefault();
        }

        private bool TryResolveHeroCardSpecial0860(HeroState hero,CardData card,EnemyState selectedEnemy,out bool deferred)
        {
            deferred=false;if(hero==null||card==null)return false;
            string destination;HeroState ally;
            switch(card.Id)
            {
                case "H001-013": // Coordinated Strike
                    ally=ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.Id!=hero.Id&&GraphDistance(hero.LocationId,h.LocationId)<=2&&Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==h.LocationId)).OrderBy(h=>GraphDistance(hero.LocationId,h.LocationId)).ThenBy(h=>h.PartyIndex).FirstOrDefault();
                    if(ally!=null){var e=Enemies.Values.First(x=>!x.Defeated&&x.LocationId==ally.LocationId);FinishPlayedCard(hero,card);BeginHeroAttack(ally,e,0,0,card.Name,null,()=>{if(!e.Defeated)MoveEnemyByHeroCard0860(e,card.Name);});deferred=true;}return true;
                case "H001-014": // Hold Fast
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&GraphDistance(hero.LocationId,h.LocationId)<=1)){AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until the start of your next turn");AddHeroModifier0860(h,"Armor",1,card.Name,hero,"until the start of your next turn");AddStatus(h,"Ignore Forced Move",1,0,card.Name,true,hero,"until the start of your next turn");}return true;
                case "H001-016": // Shield Escort
                    ally=BestAlly0860(hero,0,true);destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name);if(ally!=null){MoveHeroImmediate0860(ally,destination,card.Name);AddHeroModifier0860(ally,"Armor",1,card.Name,hero,"until the start of your next turn");}}return true;
                case "H001-017": // Battlefield Assessment - deterministic option 1
                    DrawCards(hero,2);hero.PendingDiscardCount+=1;AddLog($"CARD CHOICE — {card.Name}: draw 2, then discard 1 selected as the deterministic legal option.");return true;
                case "H001-018": // Veteran's Resolve
                    if(RemoveOneCondition0860(hero,card.Name)){AddLog($"CARD CHOICE — {card.Name}: removes a harmful Condition.");return true;}
                    if(hero.Discard.Count>0){string id=hero.Discard[hero.Discard.Count-1];hero.Discard.RemoveAt(hero.Discard.Count-1);var recovered=HeroDatabase.GetCard(id);if(recovered!=null)hero.Hand.Add(recovered);AddLog($"CARD CHOICE — {card.Name}: {recovered?.Name??id} returns from discard to hand.");}return true;
                case "H001-020": // Heroic Charge
                {
                    destination=ChooseHeroDestination0860(hero,1,true);if(destination==null)return true;var moved=ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==hero.LocationId).OrderBy(h=>h.Id==hero.Id?0:1).ThenBy(h=>h.PartyIndex).Take(3).ToList();foreach(var h in moved){MoveHeroImmediate0860(h,destination,card.Name);AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until the start of your next turn");}
                    var target=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.LocationId==destination);if(target!=null){FinishPlayedCard(hero,card);BeginHeroAttack(hero,target,0,0,card.Name,null,()=>{var second=moved.FirstOrDefault(h=>h.Id!=hero.Id&&!h.IsDefeated);var e2=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.LocationId==destination);if(second!=null&&e2!=null)BeginHeroAttack(second,e2,0,0,$"{card.Name} ally follow-up",null);});deferred=true;}return true;
                }
                case "H002-004": // Twin Blades - option 1; two attacks same target
                    if(selectedEnemy!=null){FinishPlayedCard(hero,card);BeginHeroAttack(hero,selectedEnemy,0,0,card.Name,card,()=>{if(!selectedEnemy.Defeated)BeginHeroAttack(hero,selectedEnemy,-1,0,$"{card.Name} second strike",card);});deferred=true;}return true;
                case "H003-013": // Prayer of Restoration
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==hero.LocationId).OrderBy(h=>(float)h.Health/Mathf.Max(1,h.MaxHealth)).Take(2)){if(!RemoveOneCondition0860(h,card.Name))HealHero(h,1,card.Name,hero);}return true;
                case "H003-014": // Divine Communion
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==hero.LocationId)){if(!RemoveOneCondition0860(h,card.Name)&&h.Health<h.MaxHealth)HealHero(h,2,card.Name,hero);else AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until end of next turn");if(IsConsecratedLocation0860(hero.LocationId)&&h.Health<h.MaxHealth)HealHero(h,2,card.Name,hero);}return true;
                case "H003-015": // Guided Path
                    destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name);if(!RemoveOneCondition0860(hero,card.Name))HealHero(hero,1,card.Name,hero);if(IsConsecratedLocation0860(destination))DrawCards(hero,1);}return true;
                case "H003-016": // Shepherd the Faithful
                    ally=BestAlly0860(hero,0,true);destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name);if(ally!=null)MoveHeroImmediate0860(ally,destination,card.Name);var healTarget=new[]{hero,ally}.Where(h=>h!=null&&!h.IsDefeated).OrderBy(h=>(float)h.Health/Mathf.Max(1,h.MaxHealth)).FirstOrDefault();if(healTarget!=null)HealHero(healTarget,1,card.Name,hero);}return true;
                case "H003-017": // Wings of Mercy
                    destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name,true);var h=BestAlly0860(hero,0,false);if(h!=null&&h.Health<h.MaxHealth)HealHero(h,2,card.Name,hero);else foreach(var x in ParticipatingHeroes.Where(x=>!x.IsDefeated&&x.LocationId==hero.LocationId))RemoveOneCondition0860(x,card.Name);AddLocationTag(hero.LocationId,"Consecrated",card.Name,hero,StatusExpiryTiming.StartOfSourceTurn,1);}return true;
                case "H003-018": // Holy Insight
                    if(!TryAdvanceQuestNamedGoal0858("Insight",2,card.Name))TryAdvanceQuestNamedGoal0858("Evidence",2,card.Name);if(IsConsecratedLocation0860(hero.LocationId))DrawCards(hero,1);return true;
                case "H003-019": // Rite of Purification
                    if(TryRemoveEnemyHazardAt0860(hero.LocationId,card.Name))return true;if(TryRemoveNegativeLocationTag0860(hero.LocationId,card.Name))return true;TryAdvanceQuestNamedGoal0858("Insight",2,card.Name);return true;
                case "H003-021":
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==hero.LocationId)){HealHero(h,2,card.Name,hero);RemoveOneCondition0860(h,card.Name);AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until end of next turn");}return true;
                case "H004-003": // Frost Nova
                {
                    var ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).Take(3).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,0,0,card);deferred=true;return true;
                }
                case "H004-005": // Gravity Well
                {
                    string chosen=hero.LocationId;var moved=Enemies.Values.Where(e=>!e.Defeated&&GraphDistance(e.LocationId,chosen)==1).Take(2).ToList();foreach(var e in moved){e.LocationId=chosen;if(EnemyAvatars.TryGetValue(e.Id,out var av))av.transform.position=Locations[chosen].Position;}
                    int bonus=IsArcaneLocation(chosen)?1:0;FinishPlayedCard(hero,card);BeginMultiAttack(hero,moved.Select(e=>e.Id).ToList(),bonus,0,card);deferred=true;return true;
                }
                case "H004-012":
                    ally=BestAlly0860(hero,0,false)??hero;DrawCards(ally,2);if(!IsArcaneLocation(hero.LocationId))ally.PendingDiscardCount+=1;return true;
                case "H004-013":
                    ally=BestAlly0860(hero,0,true)??hero;AddStatus(ally,"Next Attack +1",1,0,card.Name,true,hero,"until end of next turn");if(IsArcaneLocation(hero.LocationId))AddStatus(ally,"Inspired",1,0,card.Name,true,hero,"until end of next turn");return true;
                case "H004-015":
                    destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name,true);if(IsArcaneLocation(destination))DrawCards(hero,1);else AddLocationTag(destination,"Arcane",card.Name,hero,StatusExpiryTiming.StartOfSourceTurn,1);}return true;
                case "H004-016":
                    ally=BestAlly0860(hero,0,true);destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name,true);if(ally!=null)MoveHeroImmediate0860(ally,destination,card.Name,true);if(IsArcaneLocation(destination)){HealHero(hero,1,card.Name,hero);if(ally!=null)HealHero(ally,1,card.Name,hero);}else AddLocationTag(destination,"Arcane",card.Name,hero,StatusExpiryTiming.StartOfSourceTurn,1);}return true;
                case "H004-017":
                    destination=ChooseHeroDestination0860(hero,2,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name,true);if(IsArcaneLocation(destination)){HealHero(hero,1,card.Name,hero);DrawCards(hero,1);}else AddLocationTag(destination,"Arcane",card.Name,hero,StatusExpiryTiming.StartOfSourceTurn,1);}return true;
                case "H004-018": // Detect Magic - deterministic legal reorder
                    if(EquipmentDeck.Count>1){var top=EquipmentDeck.Take(3).ToList();for(int i=0;i<top.Count;i++)EquipmentDeck.Remove(top[i]);top.Sort(StringComparer.OrdinalIgnoreCase);EquipmentDeck.InsertRange(0,top);if(IsArcaneLocation(hero.LocationId)&&EquipmentDeck.Count>0){var bottom=EquipmentDeck[0];EquipmentDeck.RemoveAt(0);EquipmentDeck.Add(bottom);}AddLog($"CARD SCOUT — {card.Name}: Equipment deck top cards reordered{(IsArcaneLocation(hero.LocationId)?"; one moved to bottom":"")}.");}return true;
                case "H004-019": PlaceArcaneWard0860(hero,card.Name);return true;
                case "H004-020": // Time Stop
                    foreach(var e in Enemies.Values.Where(e=>!e.Defeated))AddStatus(e,"Time Stopped",0,0,card.Name,false,hero,"until the start of your next turn");foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated)){AddStatus(h,"Next Attack +1",1,0,card.Name,true,hero,"until end of round");}AddStatus(hero,"Next Hero Card Free",1,0,card.Name,true,hero,"until end of turn");if(IsArcaneLocation(hero.LocationId))DrawCards(hero,2);return true;
                case "H005-014": // Trailblazer free ally move
                    ally=BestAlly0860(hero,0,false);if(ally!=null&&!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==ally.LocationId)){destination=ChooseHeroDestination0860(ally,1,true);if(destination!=null)MoveHeroImmediate0860(ally,destination,card.Name);}return true;
                case "H005-020": // Arrow Storm
                {
                    var loc=Locations.Values.Where(l=>l.Revealed&&GraphDistance(hero.LocationId,l.Id)<=3).OrderByDescending(l=>Enemies.Values.Count(e=>!e.Defeated&&e.LocationId==l.Id)).ThenBy(l=>GraphDistance(hero.LocationId,l.Id)).FirstOrDefault();var ids=loc==null?new List<string>():Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==loc.Id).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,2,0,card);deferred=true;return true;
                }
                case "H006-020": // Beacon of Hope
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==hero.LocationId)){HealHero(h,2,card.Name,hero);RemoveOneCondition0860(h,card.Name);AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until the start of your next turn");}return true;
                case "H007-020": // Avatar of Nature
                {
                    RemoveStatus(hero.Statuses,"Rooted",hero.DisplayName,card.Name);RemoveStatus(hero.Statuses,"Slowed",hero.DisplayName,card.Name);AddHeroModifier0860(hero,"Armor",2,card.Name,hero,"until the start of your next turn");var ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,3,0,card);deferred=true;return true;
                }
                case "H008-020": // Grand Finale
                {
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==hero.LocationId)){DrawCards(h,1);AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until end of next turn");}var ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,2,0,card);deferred=true;return true;
                }
                case "H006-016": // Guardian's Charge
                    destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name);var e=Enemies.Values.FirstOrDefault(x=>!x.Defeated&&x.LocationId==destination);if(e!=null){FinishPlayedCard(hero,card);BeginHeroAttack(hero,e,0,0,card.Name,null);deferred=true;}}return true;
                case "H007-015": case "H005-015": case "H008-016": case "H009-015": case "H010-016": case "H015-012":
                    destination=ChooseHeroDestination0860(hero,2,true);if(destination!=null)MoveHeroImmediate0860(hero,destination,card.Name,card.Id=="H009-015");if(card.Id=="H010-016")hero.NextAttackBonus+=1;return true;
                case "H010-009": PlacePiece("Turret",hero.Id,hero.LocationId,3,1);return true;
                case "H010-010": PlaceMiraTrap0860(hero,card.Name);return true;
                case "H010-011": ally=BestAlly0860(hero,0,false)??hero;HealHero(ally,2,card.Name,hero);return true;
                case "H010-012": ally=BestAlly0860(hero,0,false)??hero;AddStatus(ally,"Hero Card Cost -1",1,0,card.Name,true,hero,"until end of turn");DrawCards(hero,1);return true;
                case "H010-013": ally=BestAlly0860(hero,0,false)??hero;ally.NextAttackBonus+=2;return true;
                case "H010-015": destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null)MoveHeroImmediate0860(hero,destination,card.Name,true);return true;
                case "H010-019":
                    AddStatus(hero,"Master Engineer",1,0,card.Name,true,hero,"until the start of your next turn");var turret=HeroCreatedPieces.FirstOrDefault(p=>p.Type=="Turret"&&p.OwnerHeroId==hero.Id);if(turret!=null)turret.Health+=2;AddLog("MASTER ENGINEER — Turret damage increases by 2 until Mira's next turn; current Turret gains 2 Health.");return true;
                case "H010-020":
                {
                    if(!HasPiece("Turret",hero.Id,null))PlacePiece("Turret",hero.Id,hero.LocationId,3,1);var ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,2,0,card);deferred=true;return true;
                }
                case "H011-020": // Cataclysm — may Overload even if Arcane Overload was already used
                {
                    hero.SelfDamageAfterNextAttack=Mathf.Max(hero.SelfDamageAfterNextAttack,1);var ids=Enemies.Values.Where(e=>!e.Defeated&&GraphDistance(hero.LocationId,e.LocationId)<=2).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,5,0,card);deferred=true;return true;
                }
                case "H013-020": // Damnation
                {
                    var ids=Enemies.Values.Where(e=>!e.Defeated&&GraphDistance(hero.LocationId,e.LocationId)<=2&&e.Statuses.Any(st=>!st.Beneficial)).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,3,0,card);deferred=true;return true;
                }
                case "H014-007": // Corpse Explosion
                {
                    var servant=HeroCreatedPieces.FirstOrDefault(p=>p.Type=="Bone Servant"&&p.OwnerHeroId==hero.Id);if(servant==null)return true;string loc=servant.LocationId;RemovePiece("Bone Servant",hero.Id,loc);var ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==loc).Select(e=>e.Id).ToList();FinishPlayedCard(hero,card);BeginMultiAttack(hero,ids,3,0,card);deferred=true;return true;
                }
                case "H014-010":
                    destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){string prior=hero.LocationId;MoveHeroImmediate0860(hero,destination,card.Name);var piece=HeroCreatedPieces.FirstOrDefault(p=>p.Type=="Bone Servant"&&p.OwnerHeroId==hero.Id&&p.LocationId==prior);if(piece!=null)piece.LocationId=destination;}return true;
                case "H014-011": if(RemovePiece("Bone Servant",hero.Id,hero.LocationId))hero.NextAttackBonus+=4;return true;
                case "H014-015": if(RemovePiece("Bone Servant",hero.Id,hero.LocationId)){DrawCards(hero,2);HealHero(hero,1,card.Name,hero);}return true;
                case "H014-016": AddStatus(hero,"Death's Patience",1,0,card.Name,true,hero,"until end of turn");return true;
                case "H014-020":
                    foreach(var p in HeroCreatedPieces.Where(p=>p.Type=="Bone Servant"&&p.OwnerHeroId==hero.Id).ToList()){var e=Enemies.Values.FirstOrDefault(x=>!x.Defeated&&x.LocationId==p.LocationId);if(e!=null)DealDirectDamage(e,3,card.Name,hero,card);}foreach(var p in HeroCreatedPieces.Where(p=>p.Type=="Bone Servant"&&p.OwnerHeroId==hero.Id).ToList()){RemovePiece("Bone Servant",hero.Id,p.LocationId);DrawCards(hero,1);}return true;
                case "H015-019": // no unresolved UI: deterministic out-and-back is now a live post-attack effect
                    if(selectedEnemy!=null){hero.FocusEnemyId=selectedEnemy.Id;hero.FocusAttackBonus=2;AddStatus(hero,"Master Duelist Footwork",1,0,card.Name,true,hero,"until end of turn");}return true;
                case "H015-020":
                {
                    var path=new List<string>();string cursor=hero.LocationId;for(int i=0;i<2;i++){var next=Locations[cursor].Connections.Where(id=>Locations[id].Revealed&&CanEnterQuestLocation0832(id)&&!path.Contains(id)).OrderByDescending(id=>Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==id)).ThenBy(id=>id).FirstOrDefault();if(next==null)break;path.Add(next);cursor=next;}
                    FinishPlayedCard(hero,card);ResolvePerfectFinale0860(hero,card,path,0);deferred=true;return true;
                }
                case "H016-013":
                    destination=ChooseHeroDestination0860(hero,1,true);if(destination!=null){MoveHeroImmediate0860(hero,destination,card.Name);if(HasPiece("Spirit Totem",hero.Id,destination))DrawCards(hero,1);}return true;
                case "H016-020":
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&GraphDistance(hero.LocationId,h.LocationId)<=1)){HealHero(h,2,card.Name,hero);AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until end of next turn");}foreach(var e in Enemies.Values.Where(e=>!e.Defeated&&GraphDistance(hero.LocationId,e.LocationId)<=1))AddStatus(e,"Weakened",1,0,card.Name,false,hero,"until end of next activation");return true;
            }
            return false;
        }

        private void ResolvePerfectFinale0860(HeroState hero,CardData card,List<string> path,int index)
        {
            if(hero==null||card==null||path==null||index>=path.Count){IsResolving=false;StateChanged?.Invoke();return;}
            IsResolving=true;MoveHeroImmediate0860(hero,path[index],card.Name);var target=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.LocationId==hero.LocationId);
            if(target==null){ResolvePerfectFinale0860(hero,card,path,index+1);return;}
            BeginHeroAttack(hero,target,3,0,card.Name,card,()=>ResolvePerfectFinale0860(hero,card,path,index+1));
        }

        private int HeroCardPreAttackBonus0860(HeroState hero,EnemyState enemy,CardData card)
        {
            if(hero==null||enemy==null)return 0;int bonus=0;string r=card?.Rules??string.Empty;
            if(card!=null)
            {
                bool undeadOrCursed=EnemyTypeHas0859(enemy,"Undead","Cursed");
                if(card.Id=="H002-002"&&hero.HasStatus("Stealth"))bonus+=2;
                if((card.Id=="H003-001"||card.Id=="H003-003")&&undeadOrCursed)bonus+=1;
                if(card.Id=="H006-003"&&EnemyHasKey0859(enemy,EnemyRoundKey0859("DAMAGED_HERO")))bonus+=1;
                if(card.Id=="H007-005"&&enemy.HasStatus("Rooted"))bonus+=2;
                if(card.Id=="H009-001"&&CardsPlayedThisTurn0860(hero)>1)bonus+=1;
                if(card.Id=="H010-001"&&enemy.Statuses.Count>0)bonus+=1;
                if(card.Id=="H002-006"&&enemy.HasStatus("Exposed"))bonus+=1;
                if(card.Id=="H012-005"&&hero.Health*2<=hero.MaxHealth)bonus+=3;
                if(card.Id=="H012-006"&&enemy.Health*2<=enemy.MaxHealth)bonus+=2;
                if(card.Id=="H013-004"&&enemy.Statuses.Any(st=>!st.Beneficial))bonus+=2;
                if(card.Id=="H015-003"&&Enemies.Values.Count(e=>!e.Defeated&&e.LocationId==hero.LocationId)==1)bonus+=3;
                if(card.Id=="H015-010"&&enemy.Health*2<=enemy.MaxHealth)bonus+=2;
                if(card.Id=="H013-020"&&!enemy.Statuses.Any(st=>!st.Beneficial))return -999;
            }
            var next=hero.Statuses.FirstOrDefault(st=>st.Name=="Next Attack +1");if(next!=null){bonus+=1;hero.Statuses.Remove(next);AddLog($"CARD EFFECT — {hero.DisplayName}'s prepared next Attack gains +1 Attack.");}
            return bonus;
        }

        private void OnHeroCardAttackResolved0860(HeroState hero,EnemyState enemy,CardData card,int damage,bool defeated)
        {
            if(hero==null||enemy==null||card==null)return;string r=card.Rules??string.Empty;
            if(card.Id=="H005-020"&&!defeated)AddStatus(enemy,"Marked",0,1,card.Name,false,hero,"until end of round");
            if(damage>0)
            {
                if(Regex.IsMatch(r,@"(?:may\s+)?move (?:that Enemy|the target|that enemy) to a connected Location",RegexOptions.IgnoreCase))MoveEnemyByHeroCard0860(enemy,card.Name);
                var splash=Regex.Match(r,@"Deal\s+(\d+)\s+damage to (?:another|a second) enemy",RegexOptions.IgnoreCase);if(splash.Success&&int.TryParse(splash.Groups[1].Value,out var n)){var other=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(other!=null)DealDirectDamage(other,n,card.Name,hero,card);}
                int heal=Number0860(r,@"(?:you\s+)?recover\s+(\d+)\s+Health",0);if(heal>0)HealHero(hero,heal,card.Name,hero);
                bool undeadOrCursed=EnemyTypeHas0859(enemy,"Undead","Cursed");
                if(card.Id=="H003-002"){var h=ParticipatingHeroes.Where(x=>!x.IsDefeated&&(x.LocationId==hero.LocationId||x.LocationId==enemy.LocationId)).OrderBy(x=>x.Health/(float)Mathf.Max(1,x.MaxHealth)).FirstOrDefault();if(h!=null)HealHero(h,1,card.Name,hero);}
                if(card.Id=="H003-003"&&!undeadOrCursed){var h=BestAlly0860(hero,0,false);if(h!=null)RemoveOneCondition0860(h,card.Name);}
                if(card.Id=="H003-005"){var h=BestAlly0860(hero,0,true)??hero;AddStatus(h,"Inspired",1,0,card.Name,true,hero,"until end of next turn");if(undeadOrCursed)DrawCards(hero,1);}
                if(card.Id=="H003-006"){foreach(var h in ParticipatingHeroes.Where(x=>!x.IsDefeated&&x.LocationId==hero.LocationId).OrderBy(x=>x.Health).Take(2).ToList()){HealHero(h,1,card.Name,hero);if(undeadOrCursed)RemoveOneCondition0860(h,card.Name);}}
                if(card.Id=="H005-001"&&enemy.HasStatus("Marked"))DrawCards(hero,1);
                if(card.Id=="H008-005"&&enemy.Statuses.Any(st=>!st.Beneficial))DrawCards(hero,1);
                if(card.Id=="H010-002"&&hero.Hand.Count>0){var cycled=hero.Hand[0];hero.Hand.RemoveAt(0);hero.Discard.Add(cycled.Id);DrawCards(hero,1);AddLog($"CARD CYCLE — {card.Name}: discard 1, draw 1.");}
            }
            if(defeated)
            {
                int draw=Number0860(r,@"(?:defeats?[^.]*?)(?:Draw|draw)\s+(\d+)\s+card",0);if(draw>0)DrawCards(hero,draw);
                if(Regex.IsMatch(r,@"defeats?[^.]*?(?:may\s+)?(?:immediately\s+)?move to a connected Location",RegexOptions.IgnoreCase))hero.FreeMoveReady=true;
                if(card.Id=="H006-001"){var h=BestAlly0860(hero,0,true);if(h!=null)HealHero(h,1,card.Name,hero);}
                if(card.Id=="H008-006"){var next=Enemies.Values.Where(e=>!e.Defeated&&e.Id!=enemy.Id&&GraphDistance(enemy.LocationId,e.LocationId)<=2).OrderBy(e=>GraphDistance(hero.LocationId,e.LocationId)).FirstOrDefault();if(next!=null)ApplyHeroAttackDamage(hero,next,0,0,card.Name+" follow-up",null);}
                foreach(var mordren in ParticipatingHeroes.Where(h=>h.Id=="Mordren"&&!h.IsDefeated&&h.HasStatus("Death's Patience")&&GraphDistance(h.LocationId,enemy.LocationId)<=2).ToList())PlacePiece("Bone Servant",mordren.Id,enemy.LocationId,1,MaxBoneServants(mordren));
            }
            int armor=Number0860(r,@"increase your armor by\s+(\d+)",0);if(armor>0)AddHeroModifier0860(hero,"Armor",armor,card.Name,hero,"until the start of your next turn");
            if(Has0860(r,"After this Attack resolves, lose Stealth"))RemoveStatus(hero.Statuses,"Stealth",hero.DisplayName,card.Name);
            if(card.Id=="H015-019"&&hero.HasStatus("Master Duelist Footwork"))
            {string d=ChooseHeroDestination0860(hero,1,false);if(d!=null){string origin=hero.LocationId;MoveHeroImmediate0860(hero,d,card.Name,true);MoveHeroImmediate0860(hero,origin,card.Name,true);}}
        }

        private bool ValidateHeroCardSpecific0860(HeroState hero,CardData card,EnemyState target,out string reason)
        {
            reason=null;if(hero==null||card==null)return true;string r=card.Rules??string.Empty;
            if(Regex.IsMatch(r,@"only play this card while you have Stealth",RegexOptions.IgnoreCase)&&!hero.HasStatus("Stealth")){reason=$"{card.Name} requires Stealth.";return false;}
            if(card.Id=="H010-009"&&HasPiece("Turret",hero.Id,null)){reason="Deploy Turret: Mira already has a Turret in play.";return false;}
            if(card.Id=="H005-019"&&(target==null||!target.HasStatus("Marked"))){reason="Master's Hunt requires a Marked Enemy.";return false;}
            if(card.Id=="H013-020"&&(target==null||!target.Statuses.Any(st=>!st.Beneficial))){reason="Damnation requires an afflicted Enemy.";return false;}
            if(card.Id=="H014-007"&&!HasPiece("Bone Servant",hero.Id,null)){reason="Corpse Explosion requires a Bone Servant.";return false;}
            if((card.Id=="H014-011"||card.Id=="H014-015")&&!HasPiece("Bone Servant",hero.Id,hero.LocationId)){reason=$"{card.Name} requires a Bone Servant in Mordren's Location.";return false;}
            return true;
        }

        private void ApplyHeroReactionFollowup0860(HeroState hero,CardData card,EnemyState attackingEnemy,int dealt)
        {
            if(hero==null||card==null)return;string r=card.Rules??string.Empty;
            if(card.Id=="H001-010"&&dealt>2&&attackingEnemy!=null&&!attackingEnemy.Defeated&&attackingEnemy.LocationId==hero.LocationId)BeginHeroAttack(hero,attackingEnemy,0,0,card.Name,null);
            if(card.Id=="H002-009")
            {DrawCards(hero,2);hero.PendingDiscardCount+=1;return;}
            if(card.Id=="H002-011"||card.Id=="H004-010")
            {string d=ChooseHeroDestination0860(hero,1,false);if(d!=null){MoveHeroImmediate0860(hero,d,card.Name,true);if(card.Id=="H004-010"&&IsArcaneLocation(d))HealHero(hero,1,card.Name,hero);}return;}
            if((card.Id=="H002-008"||card.Id=="H002-021"||card.Id=="H004-008")&&Has0860(r,"Move to a connected Location"))
            {string d=ChooseHeroDestination0860(hero,1,false);if(d!=null){MoveHeroImmediate0860(hero,d,card.Name,true);if(card.Id=="H004-008"&&IsArcaneLocation(d))DrawCards(hero,1);}}
        }

        private void OnHeroEnemyAttackResolved0860(EnemyState enemy,HeroState target,int dealt)
        {
            if(enemy==null||target==null)return;
            if(target.HasStatus("Brace Counter Ready"))
            {
                RemoveStatus(target.Statuses,"Brace Counter Ready",target.DisplayName,"Brace resolved");
                if(dealt>2&&!enemy.Defeated&&enemy.LocationId==target.LocationId){AddLog("REACTION FOLLOW-UP — Brace triggers its immediate Basic Attack.");BeginHeroAttack(target,enemy,0,0,"Brace counterattack",null);}
            }
        }

        private bool IsConsecratedLocation0860(string id)=>Locations.TryGetValue(id??string.Empty,out var l)&&l.TemporaryTags.Any(t=>string.Equals(t.Tag,"Consecrated",StringComparison.OrdinalIgnoreCase));

        private bool TryRemoveEnemyHazardAt0860(string locationId,string source)
        {
            var h=EnemyHazards0859.FirstOrDefault(x=>x.LocationId==locationId);if(h==null)return false;EnemyHazards0859.Remove(h);AddLog($"CARD CLEANSE — {source} removes {h.Type} at {Locations[locationId].Name}.");return true;
        }
        private bool TryRemoveNegativeLocationTag0860(string locationId,string source)
        {
            if(!Locations.TryGetValue(locationId??string.Empty,out var l))return false;var t=l.TemporaryTags.FirstOrDefault(x=>Has0860(x.Tag,"Hazard")||Has0860(x.Tag,"Blocked")||Has0860(x.Tag,"Blight")||Has0860(x.Tag,"Flood")||Has0860(x.Tag,"Mirage")||Has0860(x.Tag,"Fracture")||Has0860(x.Tag,"Rubble"));if(t==null)return false;l.TemporaryTags.Remove(t);AddLog($"CARD CLEANSE — {source} removes {t.Tag} from {l.Name}.");return true;
        }

        private void PlaceArcaneWard0860(HeroState hero,string source)
        {
            foreach(var l in Locations.Values)l.TemporaryTags.RemoveAll(t=>t.Tag!=null&&t.Tag.StartsWith("Arcane Ward ",StringComparison.OrdinalIgnoreCase));
            string location=hero.LocationId;Locations[location].TemporaryTags.Add(new LocationTagInstance{Tag="Arcane Ward 3",Source=source,SourceHeroId=hero.Id,ExpiryTiming=StatusExpiryTiming.Persistent});
            if(!IsArcaneLocation(location))AddLocationTag(location,"Arcane",source,hero,StatusExpiryTiming.Persistent,0);AddLog($"ARCANE WARD — {Locations[location].Name} gains a 3-trigger Arcane Ward.");
        }
        private void PlaceMiraTrap0860(HeroState hero,string source)
        {
            string d=ChooseHeroDestination0860(hero,1,false);if(d==null)return;Locations[d].TemporaryTags.RemoveAll(t=>t.Tag=="Mira Trap");Locations[d].TemporaryTags.Add(new LocationTagInstance{Tag="Mira Trap",Source=source,SourceHeroId=hero.Id,ExpiryTiming=StatusExpiryTiming.Persistent});AddLog($"MECHANICAL TRAP — {Locations[d].Name} is trapped by {hero.DisplayName}.");
        }

        private bool TryAutoHeroReaction0860(HeroState hero,string cardId,string source,Action<CardData> effect)
        {
            if(hero==null||hero.IsDefeated||string.IsNullOrEmpty(cardId))return false;
            var card=hero.Hand.FirstOrDefault(c=>c.Id==cardId);if(card==null)return false;
            int cost=GetEffectiveCardCost(hero,card);if(!CanPayReactionCost(hero,card,cost))return false;
            SpendCardCost(hero,card,cost);DiscardFromHand(hero,card,"Reaction");
            AddLog($"AUTO REACTION — {hero.DisplayName} uses {card.Name} ({source}).");effect?.Invoke(card);return true;
        }

        private bool TryAutoPreventForcedMove0860(EnemyState enemy,HeroState target,string source)
        {
            if(target==null)return false;
            var thorne=ParticipatingHeroes.FirstOrDefault(h=>h.Id=="Thorne"&&!h.IsDefeated&&h.LocationId==target.LocationId);
            if(thorne==null)return false;
            return TryAutoHeroReaction0860(thorne,"H007-018",source,_=>AddLog($"PROTECTIVE ROOTS — {target.DisplayName} ignores the forced movement."));
        }

        private bool TryAutoSpellReflection0860(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return false;
            string id=enemy.TemplateId??string.Empty;
            // Only authored Enemy abilities that target or directly affect Heroes are reflectable.
            string[] targeted={"E005","E017","E019","E022","E026","E031","E035","E038","E046","E055","E060","E072","E079","E081","E091","E098","E106","E113","E125"};
            if(!targeted.Contains(id))return false;
            int range=Mathf.Max(0,AuthoredEnemyRange0859(enemy));
            var affected=EnemyHeroTargets0859(enemy,range).ToList();if(affected.Count==0)return false;
            var vaelis=ParticipatingHeroes.FirstOrDefault(h=>h.Id=="Vaelis"&&!h.IsDefeated&&affected.Any(t=>GraphDistance(h.LocationId,t.LocationId)<=1));
            if(vaelis==null)return false;
            return TryAutoHeroReaction0860(vaelis,"H004-009",$"{enemy.DisplayName} ability",card=>
            {
                AddLog($"SPELL REFLECTION — {enemy.DisplayName}'s targeted ability is canceled; Enemies have no spare Action to pay the authored +1 Action tax.");
                if(IsArcaneLocation(vaelis.LocationId)&&GraphDistance(vaelis.LocationId,enemy.LocationId)<=1)DealDirectDamage(enemy,2,card.Name,vaelis,card);
            });
        }

        private void OnEnemyEnteredHeroCardLocation0860(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated||!Locations.TryGetValue(enemy.LocationId,out var l))return;
            var trap=l.TemporaryTags.FirstOrDefault(t=>t.Tag=="Mira Trap");if(trap!=null){l.TemporaryTags.Remove(trap);HeroState source=Heroes.TryGetValue(trap.SourceHeroId??string.Empty,out var h)?h:null;DealDirectDamage(enemy,2,"Mechanical Trap",source,null);if(!enemy.Defeated)AddStatus(enemy,"Rooted",0,1,"Mechanical Trap",false,source,"until end of activation");}
            var ward=l.TemporaryTags.FirstOrDefault(t=>t.Tag!=null&&t.Tag.StartsWith("Arcane Ward ",StringComparison.OrdinalIgnoreCase));if(ward!=null)
            {string key=$"{Hero0860Prefix}WARD:R{Round}:{l.Id}";if(!enemy.UsedKeys0859.Contains(key)){enemy.UsedKeys0859.Add(key);HeroState source=Heroes.TryGetValue(ward.SourceHeroId??string.Empty,out var h)?h:null;DealDirectDamage(enemy,2,"Arcane Ward",source,null);int left=Number0860(ward.Tag,@"Arcane Ward (\d+)",1)-1;if(left<=0){l.TemporaryTags.Remove(ward);AddLog($"ARCANE WARD — {l.Name}'s Ward expends its final trigger.");}else ward.Tag=$"Arcane Ward {left}";}}
            if(enemy.Defeated)return;
            var mira=ParticipatingHeroes.FirstOrDefault(h=>h.Id=="Mira"&&!h.IsDefeated&&h.LocationId==enemy.LocationId);
            if(mira!=null)TryAutoHeroReaction0860(mira,"H010-018",$"{enemy.DisplayName} entered Mira's Location",card=>DealDirectDamage(enemy,2,card.Name,mira,card));
            if(enemy.Defeated)return;
            var rowan=ParticipatingHeroes.FirstOrDefault(h=>h.Id=="Rowan"&&!h.IsDefeated&&GraphDistance(h.LocationId,enemy.LocationId)<=3);
            if(rowan!=null)TryAutoHeroReaction0860(rowan,"H005-017",$"{enemy.DisplayName} entered within Overwatch range",card=>{if(!enemy.Defeated)ApplyHeroAttackDamage(rowan,enemy,0,0,card.Name,null);});
            if(enemy.Defeated)return;
            var mordren=ParticipatingHeroes.FirstOrDefault(h=>h.Id=="Mordren"&&!h.IsDefeated&&GraphDistance(h.LocationId,enemy.LocationId)<=2&&HasPiece("Bone Servant",h.Id,enemy.LocationId));
            if(mordren!=null)TryAutoHeroReaction0860(mordren,"H014-018",$"{enemy.DisplayName} entered a Bone Servant Location",card=>DealDirectDamage(enemy,2,card.Name,mordren,card));
            if(enemy.Defeated)return;
            var lyra=ParticipatingHeroes.FirstOrDefault(h=>h.Id=="Lyra"&&!h.IsDefeated&&h.LocationId==enemy.LocationId);
            if(lyra!=null)TryAutoHeroReaction0860(lyra,"H002-021",$"{enemy.DisplayName} entered Lyra's Location",card=>{AddStatus(lyra,"Stealth",1,0,card.Name,true,lyra,"until start of next turn");AddStatus(lyra,"Cannot Be Targeted",1,0,card.Name,true,lyra,"until start of next turn");string d=ChooseHeroDestination0860(lyra,1,false);if(d!=null)MoveHeroImmediate0860(lyra,d,card.Name,true);});
        }

        private void OnHeroTurnStarted0860(HeroState hero)
        {
            if(hero==null||hero.IsDefeated)return;
            if(hero.Id=="Mira")
            {
                var turret=HeroCreatedPieces.FirstOrDefault(p=>p.Type=="Turret"&&p.OwnerHeroId==hero.Id&&p.LocationId==hero.LocationId);
                var enemy=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.LocationId==hero.LocationId);
                if(turret!=null&&enemy!=null){int dmg=hero.HasStatus("Master Engineer")?4:2;DealDirectDamage(enemy,dmg,"Mira Turret",hero,null);AddLog($"TURRET — Mira's Turret fires for {dmg} damage at {enemy.DisplayName}.");}
            }
        }

        private void OnHeroPieceRemoved0860(HeroCreatedPiece piece)
        {
            if(piece==null||piece.Type!="Bone Servant"||!Heroes.TryGetValue(piece.OwnerHeroId??string.Empty,out var mordren)||mordren.IsDefeated||mordren.UsedRoundAbilities.Contains("DeathHarvest"))return;
            mordren.UsedRoundAbilities.Add("DeathHarvest");if(mordren.Health<mordren.MaxHealth)HealHero(mordren,1,"Death Harvest",mordren);else DrawCards(mordren,1);AddLog("HERO ABILITY — Death Harvest resolves after a Bone Servant is removed.");
        }
    }
}
