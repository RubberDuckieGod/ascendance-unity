#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class FullHeroRuntimeTestTools0860
    {
        [MenuItem("Ascendance/Validation/Test 0.8.60c - Full Hero & Hero Card Runtime Execution (Play Mode)")]
        public static void Test0860()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.60c TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.60c TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.60c TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.60c TEST] FAIL — "+label);}}
            void Put(HeroState h,string authored){var l=gm.DebugLocationByAuthored0858(authored);if(l!=null)h.LocationId=l.Id;}
            void Ready(HeroState h){h.Statuses.Clear();h.UsedRoundAbilities.Clear();h.UsedTurnAbilities.Clear();h.Hand.Clear();h.Discard.Clear();h.Health=h.MaxHealth;h.Actions=6;h.AttackOnlyActions=0;h.Downed=false;h.Dead=false;h.PendingDiscardCount=0;h.FirstAttackThisTurn=true;h.NextAttackBonus=0;h.FocusEnemyId=null;h.FocusAttackBonus=0;}
            void Clean(){gm.DebugRemoveEnemies0859();gm.EnemyHazards0859.Clear();gm.HeroCreatedPieces.Clear();gm.ClearPendingContentInstruction();foreach(var h in gm.Heroes.Values)Ready(h);foreach(var l in gm.Locations.Values){l.TemporaryTags.RemoveAll(t=>t.Source!=null&&(t.Source.Contains("0.8.60c")||t.Source.Contains("Arcane Ward")||t.Source.Contains("Mechanical Trap")));}}
            void Give(HeroState h,string id){var c=HeroDatabase.GetCard(id);if(c!=null)h.Hand.Add(c);}
            EnemyState Spawn(string id,string loc="L052")=>gm.DebugSpawnEnemy0859(id,loc);
            (LocationState origin,LocationState neighbor) ConnectedPair()
            {
                // Validator-only setup: use a real authored edge from the active Quest graph.
                // Q001 begins with connected exploration tiles unrevealed, so reveal the chosen
                // existing pair rather than fabricating a connection or using cross-region tiles.
                var origin=gm.Locations.Values.Where(l=>!string.IsNullOrWhiteSpace(l.AuthoredLocationId)&&gm.DebugCanEnterQuestLocation0832(l.Id))
                    .OrderBy(l=>l.Id,StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(l=>l.Connections.Any(id=>gm.Locations.TryGetValue(id,out var n)&&!string.IsNullOrWhiteSpace(n.AuthoredLocationId)&&gm.DebugCanEnterQuestLocation0832(id)));
                if(origin==null)return (null,null);
                var neighbor=origin.Connections.Select(id=>gm.Locations.TryGetValue(id,out var n)?n:null)
                    .Where(n=>n!=null&&!string.IsNullOrWhiteSpace(n.AuthoredLocationId)&&gm.DebugCanEnterQuestLocation0832(n.Id))
                    .OrderBy(n=>n.Id,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if(neighbor==null)return (null,null);
                origin.Revealed=true;neighbor.Revealed=true;
                return (origin,neighbor);
            }

            HeroDatabase.EnsureLoaded();FullContentCatalog0850.EnsureLoaded();
            Check(AscendanceGameManager.BuildVersion=="0.10.2c","Build identifies the Full Hero & Hero Card Runtime Execution milestone.");
            Check(gm.DebugHeroDefinitionCount0860()==16,"authoritative Hero catalog contains 16 Heroes.");
            Check(gm.DebugHeroCardDefinitionCount0860()==323,"authoritative Hero-card catalog contains 323 definitions.");
            Check(gm.DebugHeroCardExecutableCount0860()==323,"all 323 authored Hero cards have an executable runtime path.");
            var heroCardAsset=Resources.Load<TextAsset>("Ascendance/Data/hero_cards");
            Check(heroCardAsset!=null&&Regex.Matches(heroCardAsset.text,"\\\"speed\\\"\\s*:\\s*\\\"Reaction\\\"",RegexOptions.IgnoreCase).Count==49,"all 49 authored Reaction definitions remain present.");
            Check(FullContentCatalog0850.EnemyCount==125&&FullContentCatalog0850.LocationCount==236&&FullContentCatalog0850.QuestCount==72&&FullContentCatalog0850.WorldEventCount==24,"previous Enemy/Location/Quest/World Event catalogs remain intact.");

            string[] heroIds={"Aldric","Lyra","Elara","Vaelis","Rowan","Seraphine","Thorne","Cassian","Kael","Mira","Malachar","Garruk","Veyra","Mordren","Lucien","Asha"};
            foreach(var id in heroIds)Check(gm.DebugHeroPhysicalDeckCount0860(id)==30,$"{id} retains a 30-card physical Hero Deck.");
            foreach(var id in heroIds){var d=HeroDatabase.GetHero(id);Check(d!=null&&!string.IsNullOrWhiteSpace(d.innateAbility)&&d.abilities!=null&&d.abilities.Count>0,$"{id} retains authored innate/level ability data.");}

            // Aldric / Lyra / Elara / Vaelis.
            gm.SetParty(new[]{"Aldric","Lyra","Elara","Vaelis"},"Aldric",false);yield return null;Clean();
            var aldric=gm.Heroes["Aldric"];var lyra=gm.Heroes["Lyra"];var elara=gm.Heroes["Elara"];var vaelis=gm.Heroes["Vaelis"];
            Put(aldric,"L052");Put(lyra,"L052");Put(elara,"L052");Put(vaelis,"L052");
            gm.DebugResolveHeroCardSpecial0860("Aldric","H001-014");
            Check(gm.DebugHeroArmorModifier0860("Aldric")>=1&&aldric.HasStatus("Ignore Forced Move"),"Aldric Hold Fast grants live Armor and forced-movement protection.");
            var goblin=Spawn("E001");Check(gm.DebugHeroCardPreAttackBonus0860("Lyra",goblin.Id,"H002-002")==0,"Lyra Backstab does not gain its conditional bonus without Stealth.");gm.AddStatus(lyra,"Stealth",1,0,"0.8.60c validation",true,lyra,"until end of turn");Check(gm.DebugHeroCardPreAttackBonus0860("Lyra",goblin.Id,"H002-002")==2,"Lyra Backstab gains +2 Attack while Stealthed.");
            var undead=Spawn("E040");Check(gm.DebugHeroCardPreAttackBonus0860("Elara",undead.Id,"H003-001")==1,"Elara Radiant Strike gains +1 Attack against Undead/Cursed targets.");Check(gm.DebugHeroCardPreAttackBonus0860("Elara",goblin.Id,"H003-001")==0,"Elara Radiant Strike does not gain its typed bonus against ordinary Goblins.");
            gm.DebugResolveHeroCardSpecial0860("Vaelis","H004-020");Check(undead.HasStatus("Time Stopped")&&gm.DebugEnemyMovement0859(undead.Id)==0,"Vaelis Time Stop prevents affected Enemies from moving.");Check(vaelis.HasStatus("Next Hero Card Free"),"Time Stop grants Vaelis the authored free next Hero card state.");
            Check(gm.DebugGenericAttackBaseBonus0860("H004-001",1)==0,"Arcane Bolt's conditional +1 is not treated as an unconditional base bonus.");

            // Arcane Ward and Spell Reflection.
            Clean();Put(vaelis,"L052");gm.DebugResolveHeroCardSpecial0860("Vaelis","H004-019");var wardEnemy=Spawn("E001","L052");int wardHp=wardEnemy.Health;gm.DebugEnemyEnteredHeroCardLocation0860(wardEnemy.Id);Check(wardEnemy.Health==wardHp-2,"Arcane Ward deals 2 damage when an Enemy enters its warded Location.");
            Clean();Put(vaelis,"L052");Put(elara,"L052");Give(vaelis,"H004-009");var shaman=Spawn("E005","L052");Check(gm.DebugTryAutoSpellReflection0860(shaman.Id),"Spell Reflection cancels a targeted Enemy ability when Vaelis can react.");Check(!vaelis.Hand.Any(c=>c.Id=="H004-009")&&vaelis.Discard.Contains("H004-009"),"Spell Reflection pays its Reaction cost and discards normally.");

            // Rowan / Seraphine / Thorne / Cassian.
            gm.SetParty(new[]{"Rowan","Seraphine","Thorne","Cassian"},"Rowan",false);yield return null;Clean();
            var rowan=gm.Heroes["Rowan"];var sera=gm.Heroes["Seraphine"];var thorne=gm.Heroes["Thorne"];var cassian=gm.Heroes["Cassian"];
            Put(rowan,"L053");Put(sera,"L052");Put(thorne,"L052");Put(cassian,"L052");
            var overwatchPair=ConnectedPair();
            if(overwatchPair.origin==null||overwatchPair.neighbor==null){Check(false,"Rowan Overwatch makes its granted basic Attack on Enemy entry.");Check(false,"Overwatch consumes the Reaction card and held Action normally.");}
            else{rowan.LocationId=overwatchPair.neighbor.Id;Give(rowan,"H005-017");var entering=Spawn("E001",overwatchPair.origin.AuthoredLocationId);int entryHp=entering.Health;int rowanActions=rowan.Actions;gm.DebugEnemyEnteredHeroCardLocation0860(entering.Id);Check(entering.Health<entryHp,"Rowan Overwatch makes its granted basic Attack on Enemy entry.");Check(rowan.Discard.Contains("H005-017")&&rowan.Actions==rowanActions-1,"Overwatch consumes the Reaction card and held Action normally.");}
            Clean();Put(sera,"L052");Put(thorne,"L052");sera.Health=sera.MaxHealth-3;thorne.Health=thorne.MaxHealth-3;gm.AddStatus(thorne,"Poisoned",1,0,"0.8.60c validation",false,null,"until end of turn");gm.DebugResolveHeroCardSpecial0860("Seraphine","H006-020");Check(sera.Health>=sera.MaxHealth-1&&thorne.Health>=thorne.MaxHealth-1,"Seraphine Beacon of Hope restores 2 Health to Heroes in her Location.");Check(thorne.HasStatus("Inspired")&&!thorne.HasStatus("Poisoned"),"Beacon of Hope cleanses a Condition and grants Inspired.");
            Clean();Put(thorne,"L052");Put(sera,"L052");Give(thorne,"H007-018");var brute=Spawn("E010","L052");Check(gm.DebugTryAutoPreventForcedMove0860(brute.Id,"Seraphine"),"Thorne Protective Roots cancels Enemy forced movement in his Location.");Check(thorne.Discard.Contains("H007-018"),"Protective Roots resolves as a real Reaction card.");
            Clean();Put(cassian,"L052");Put(sera,"L052");Give(cassian,"H008-009");int seraHand=sera.Hand.Count;HeroRulesRuntime.OnCardPlayed(gm,cassian,HeroDatabase.GetCard("H008-009"));Check(sera.HasStatus("Inspired"),"Cassian Improved Rousing Verse grants Inspired to an allied Hero after a Support card.");Check(sera.Hand.Count>=seraHand+2&&sera.PendingDiscardCount>=1,"Improved Rousing Verse performs its draw-2/discard-1 card cycle.");

            // Kael / Mira / Malachar / Garruk.
            gm.SetParty(new[]{"Kael","Mira","Malachar","Garruk"},"Kael",false);yield return null;Clean();
            var kael=gm.Heroes["Kael"];var mira=gm.Heroes["Mira"];var mal=gm.Heroes["Malachar"];var garruk=gm.Heroes["Garruk"];
            Put(kael,"L052");Put(mira,"L052");Put(mal,"L052");Put(garruk,"L052");var target=Spawn("E001");
            gm.DebugRegisterHeroCardPlayed0860("Kael","H009-005");gm.DebugRegisterHeroCardPlayed0860("Kael","H009-001");Check(gm.DebugHeroCardsPlayedThisTurn0860("Kael")==2&&gm.DebugHeroCardPreAttackBonus0860("Kael",target.Id,"H009-001")==1,"Kael Flying Kick detects another card played earlier this turn.");gm.DebugRegisterHeroCardPlayed0860("Kael","H009-008");Check(gm.DebugHeroCardsPlayedThisTurn0860("Kael",true)==3,"Kael tracks the third Attack card for Hundred Blows Armor-ignore timing.");
            Clean();Put(mira,"L052");gm.DebugResolveHeroCardSpecial0860("Mira","H010-009");var turret=gm.HeroCreatedPieces.FirstOrDefault(p=>p.Type=="Turret"&&p.OwnerHeroId=="Mira");Check(turret!=null&&turret.Health==3,"Mira Deploy Turret creates the persistent 3-Health Turret piece.");gm.DebugResolveHeroCardSpecial0860("Mira","H010-019");Check(turret!=null&&turret.Health==5&&mira.HasStatus("Master Engineer"),"Master Engineer upgrades the active Turret and persists its turn buff.");var turretEnemy=Spawn("E001");int turretHp=turretEnemy.Health;gm.DebugHeroTurnStarted0860("Mira");Check(turretEnemy.Health<=turretHp-4,"Master Engineer Turret auto-fire deals the upgraded 4 damage at Mira's turn start.");
            Clean();Put(mira,"L052");Give(mira,"H010-018");var counter=Spawn("E001","L052");int counterHp=counter.Health;gm.DebugEnemyEnteredHeroCardLocation0860(counter.Id);Check(counter.Health==counterHp-2&&mira.Discard.Contains("H010-018"),"Mira Automatic Countermeasure deals 2 damage and resolves as an entry Reaction.");
            Clean();Put(mal,"L052");int malHp=mal.Health;bool cataclysmStarted=gm.DebugResolveHeroCardSpecial0860("Malachar","H011-020");yield return null;Check(cataclysmStarted&&mal.Health<=malHp-1,"Malachar Cataclysm owns its full-area Attack path and resolves its authored self-damage.");
            Clean();Put(garruk,"L052");var wounded=Spawn("E001");wounded.Health=Mathf.Max(1,wounded.MaxHealth/2);Check(gm.DebugHeroCardPreAttackBonus0860("Garruk",wounded.Id,"H012-006")==2,"Garruk Executioner's Swing gains its additional +2 against a half-Health target.");wounded.Health=wounded.MaxHealth;Check(gm.DebugHeroCardPreAttackBonus0860("Garruk",wounded.Id,"H012-006")==0,"Executioner's Swing does not gain its conditional bonus against a healthy target.");

            // Veyra / Mordren / Lucien / Asha.
            gm.SetParty(new[]{"Veyra","Mordren","Lucien","Asha"},"Veyra",false);yield return null;Clean();
            var veyra=gm.Heroes["Veyra"];var mordren=gm.Heroes["Mordren"];var lucien=gm.Heroes["Lucien"];var asha=gm.Heroes["Asha"];
            Put(veyra,"L052");Put(mordren,"L052");Put(lucien,"L052");Put(asha,"L052");var curseTarget=Spawn("E001");Check(gm.DebugHeroCardPreAttackBonus0860("Veyra",curseTarget.Id,"H013-004")==0,"Veyra Agonizing Curse has no extra bonus against an unafflicted target.");gm.AddStatus(curseTarget,"Weakened",1,0,"0.8.60c validation",false,veyra,"until end of round");Check(gm.DebugHeroCardPreAttackBonus0860("Veyra",curseTarget.Id,"H013-004")==2,"Agonizing Curse gains +2 Attack against an afflicted target.");
            Clean();Put(mordren,"L052");gm.PlacePiece("Bone Servant",mordren.Id,mordren.LocationId,1,2);var deathTarget=Spawn("E001");int deathHp=deathTarget.Health;gm.DebugResolveHeroCardSpecial0860("Mordren","H014-020");Check(!gm.HeroCreatedPieces.Any(p=>p.Type=="Bone Servant"&&p.OwnerHeroId==mordren.Id),"Mordren Death Unbound removes all of his Bone Servants after they resolve.");Check(deathTarget.Health<=deathHp-3,"Death Unbound makes each Servant deal its authored 3 damage before removal.");
            Clean();Put(lucien,"L052");var duel=Spawn("E001");Check(gm.DebugHeroCardPreAttackBonus0860("Lucien",duel.Id,"H015-003")==3,"Lucien Duelist's Cut gains +3 Attack against the only Enemy in his Location.");Spawn("E002");Check(gm.DebugHeroCardPreAttackBonus0860("Lucien",duel.Id,"H015-003")==0,"Duelist's Cut loses its isolation bonus when another Enemy is present.");
            Clean();Put(asha,"L052");Put(lucien,"L052");lucien.Health=lucien.MaxHealth-3;var spiritEnemy=Spawn("E001");gm.DebugResolveHeroCardSpecial0860("Asha","H016-020");Check(lucien.Health>=lucien.MaxHealth-1&&lucien.HasStatus("Inspired"),"Asha Call of the Ancestors heals and Inspires nearby Heroes.");Check(spiritEnemy.HasStatus("Weakened"),"Call of the Ancestors Weakens nearby Enemies.");

            // Shared conditional parser / post-Armor rules.
            Clean();var rooted=Spawn("E001");gm.AddStatus(rooted,"Rooted",1,0,"0.8.60c validation",false,null,"until end of round");Check(gm.DebugHeroCardPostArmorDamage0860(rooted.Id,"H007-008")==1,"Wrath of the Forest applies +1 post-Armor damage to Rooted Enemies.");Check(gm.DebugHeroCardPostArmorDamage0860(rooted.Id,"H008-008")==1,"Final Verse applies +1 post-Armor damage to afflicted Enemies.");gm.AddStatus(rooted,"Burning",1,0,"0.8.60c validation",false,null,"until end of round");Check(gm.DebugHeroCardPostArmorDamage0860(rooted.Id,"H010-008")==1,"Overloaded Reactor applies +1 post-Armor damage to Burning Enemies.");Check(gm.DebugGenericAttackBaseBonus0860("H015-010",2)==2,"Finishing Thrust preserves its printed +2 base Attack separately from the half-Health bonus.");Check(gm.DebugGenericAttackBaseBonus0860("H003-001",1)==0,"Radiant Strike's typed +1 is condition-driven rather than unconditional.");

            // Vanish / entry sequencing.
            gm.SetParty(new[]{"Lyra","Mira","Rowan","Mordren"},"Lyra",false);yield return null;Clean();lyra=gm.Heroes["Lyra"];var vanishPair=ConnectedPair();
            if(vanishPair.origin==null||vanishPair.neighbor==null){Check(false,"Vanish Without a Trace grants Stealth and temporary untargetability on Enemy entry.");Check(false,"Vanish Without a Trace performs its connected escape and consumes the Reaction.");}
            else{lyra.LocationId=vanishPair.origin.Id;Give(lyra,"H002-021");var lyraOrigin=lyra.LocationId;int lyraActions=lyra.Actions;var entrant=Spawn("E001",vanishPair.origin.AuthoredLocationId);gm.DebugEnemyEnteredHeroCardLocation0860(entrant.Id);Check(lyra.HasStatus("Stealth")&&lyra.HasStatus("Cannot Be Targeted"),"Vanish Without a Trace grants Stealth and temporary untargetability on Enemy entry.");Check(lyra.LocationId!=lyraOrigin&&lyra.Discard.Contains("H002-021")&&lyra.Actions==lyraActions-2,"Vanish Without a Trace performs its connected escape and consumes the Reaction.");}

            // Save capture — no disk mutation.
            Clean();var saveHero=gm.Heroes["Mira"];Put(saveHero,"L052");gm.AddStatus(saveHero,"Hero Armor +2",1,0,"0.8.60c validation",true,saveHero,"until start of next turn");gm.DebugRegisterHeroCardPlayed0860("Mira","H010-001");gm.PlacePiece("Turret",saveHero.Id,saveHero.LocationId,3,1);Give(saveHero,"H010-018");saveHero.Discard.Add("H010-010");
            var capture=typeof(AscendanceSaveSystem).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);var data=capture?.Invoke(null,new object[]{gm}) as AscendanceSaveData;var stored=data?.Heroes?.FirstOrDefault(x=>x.Id=="Mira");
            Check(stored!=null&&stored.Statuses.Any(s=>s.Name=="Hero Armor +2")&&stored.UsedTurnAbilities.Any(k=>k.StartsWith("H0860:CARDPLAY:")),"Hero 0.8.60c status/card-chain runtime state is captured into campaign save data.");
            Check(stored!=null&&stored.HandIds.Contains("H010-018")&&stored.DiscardIds.Contains("H010-010"),"Hero hand/discard state remains save-persistent through the 0.8.60c runtime.");
            Check(data!=null&&data.HeroCreatedPieces.Any(p=>p.Type=="Turret"&&p.OwnerHeroId=="Mira"&&p.Health==3),"Hero-created Turret state is captured into campaign save data.");
            Check(string.IsNullOrWhiteSpace(gm.PendingContentInstruction),"0.8.60c validation path finishes without a Hero CONTENT PENDING fallback.");

            gm.DebugRemoveEnemies0859();
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.60c TEST] PASS — Full Hero & Hero Card Runtime Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.60c TEST] FAIL — Full Hero & Hero Card Runtime Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
