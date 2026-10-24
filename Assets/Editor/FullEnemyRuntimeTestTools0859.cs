#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    public static class FullEnemyRuntimeTestTools0859
    {
        [MenuItem("Ascendance/Validation/Test 0.8.59b - Full Enemy Runtime Execution (Play Mode)")]
        public static void Test0859()
        {
            if(!Application.isPlaying){Debug.LogError("[ASCENDANCE][0.8.59b TEST] Enter Play Mode first.");return;}
            var gm=AscendanceGameManager.Instance;if(gm==null){Debug.LogError("[ASCENDANCE][0.8.59b TEST] GameManager missing.");return;}
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass=0,fail=0;
            void Check(bool ok,string label){if(ok){pass++;Debug.Log("[ASCENDANCE][0.8.59b TEST] PASS — "+label);}else{fail++;Debug.LogError("[ASCENDANCE][0.8.59b TEST] FAIL — "+label);}}
            EnemyState Spawn(string id,string loc="L052"){return gm.DebugSpawnEnemy0859(id,loc);}
            void Clean(){gm.DebugRemoveEnemies0859();foreach(var h in gm.Heroes.Values){h.Statuses.Clear();h.Health=h.MaxHealth;h.Actions=3;h.Downed=false;h.Dead=false;}}
            void Put(HeroState h,string authored){var l=gm.DebugLocationByAuthored0858(authored);if(l!=null)h.LocationId=l.Id;}

            FullContentCatalog0850.EnsureLoaded();
            Check(AscendanceGameManager.BuildVersion=="0.10.2c","Build identifies the Full Enemy Runtime Execution milestone.");
            Check(FullContentCatalog0850.EnemyCount==125,"full authored catalog contains 125 Enemy definitions.");
            Check(gm.DebugEnemyCount0859()==125,"all 125 authored Enemies have an executable Enemy runtime path.");
            Check(Enumerable.Range(1,125).Select(i=>$"E{i:000}").All(id=>FullContentCatalog0850.Root.enemies.Any(e=>e.id==id)),"Enemy IDs are contiguous E001-E125.");
            Check(FullContentCatalog0850.Root.enemies.All(e=>e.health>0&&e.attack>=0&&e.armor>=0&&e.movement>=0&&!string.IsNullOrWhiteSpace(e.range)&&!string.IsNullOrWhiteSpace(e.abilities)&&!string.IsNullOrWhiteSpace(e.behavior)),"every authored Enemy preserves stats, Range, Abilities, and Behavior text.");
            Check(FullContentCatalog0850.Root.enemies.Select(e=>e.enemyType).Distinct(StringComparer.OrdinalIgnoreCase).Count()>=20,"Enemy catalog preserves broad authored enemy-type coverage.");
            Check(FullContentCatalog0850.Root.enemies.Select(e=>e.behaviorProfile).Distinct(StringComparer.OrdinalIgnoreCase).Count()>=10,"Enemy catalog preserves authored behavior-profile diversity.");
            Check(FullContentCatalog0850.LootCount==190&&FullContentCatalog0850.EquipmentCount==144&&FullContentCatalog0850.LocationCount==236&&FullContentCatalog0850.QuestCount==72&&FullContentCatalog0850.WorldEventCount==24,"previous authored content catalogs remain intact.");

            gm.SetParty(new[]{"Garruk","Aldric"},"Garruk",false);yield return null;
            var g=gm.Heroes["Garruk"];var a=gm.Heroes["Aldric"];
            Put(g,"L052");Put(a,"L052");

            // Authored movement / range / baseline behavior.
            Clean();var e1=Spawn("E001");var e2=Spawn("E002");var e13=Spawn("E013");var e115=Spawn("E115");
            Check(gm.DebugEnemyRange0859(e1.Id)==0,"E001 Goblin Raider uses authored melee Range.");
            Check(gm.DebugEnemyRange0859(e2.Id)==1,"E002 Goblin Archer attacks current or connected Locations.");
            Check(gm.DebugEnemyAuthoredMovement0859(e13.Id)==3,"E013 Goblin Wolf Rider preserves authored Movement 3 before World Event modifiers.");
            Check(gm.DebugEnemyRange0859(e115.Id)==2,"E115 Cinder Seer uses authored Range within 2 connections.");

            // Formation, movement, and low-health modifiers.
            Clean();e1=Spawn("E001");Check(gm.DebugEnemyAttack0859(e1.Id)==e1.Attack+1,"E001 Reckless Assault grants +1 Attack while alone.");Spawn("E001");Check(gm.DebugEnemyAttack0859(e1.Id)==e1.Attack,"E001 Reckless Assault turns off when another Enemy shares its Location.");
            Clean();e2=Spawn("E002");Check(gm.DebugEnemyAttack0859(e2.Id)==e2.Attack,"E002 Volley Fire has no bonus while alone.");Spawn("E001");Check(gm.DebugEnemyAttack0859(e2.Id)==e2.Attack+1,"E002 Volley Fire gains +1 Attack beside another Goblin.");
            Clean();var e9=Spawn("E009");gm.DebugSetEnemyHealth0859(e9.Id,4);Check(gm.DebugEnemyAttack0859(e9.Id)==e9.Attack+2,"E009 Blood Frenzy grants +2 Attack at 4 or fewer Health.");
            Clean();var e20=Spawn("E020");Spawn("E021");Check(gm.DebugEnemyAttack0859(e20.Id)==e20.Attack+1,"E020 Pack Hunter gains +1 Attack beside another Beast.");
            Clean();var e25=Spawn("E025");gm.DebugSetEnemyHealth0859(e25.Id,10);Check(gm.DebugEnemyMovement0859(e25.Id)>=3,"E025 low-Health rage increases authored Movement.");
            Clean();var e67=Spawn("E067");var e62=Spawn("E062");Check(gm.DebugEnemyAttack0859(e62.Id)==e62.Attack+1,"E067 Forge-Lord aura grants +1 Attack to nearby Ashen/Construct allies.");

            // Explicit mechanics that were not represented in the old generic Enemy loop.
            Clean();var e3=Spawn("E003");Put(g,"L052");gm.DebugEnemyHeroEnteredLocation0859("Garruk");Check(gm.DebugEnemyArmor0859(e3.Id)>=e3.Armor+1,"E003 Brace for Charge gains +1 Armor when a Hero enters its Location.");
            Clean();var e27=Spawn("E027");Put(g,"L052");gm.DebugApplyEnemyPostAttack0859(e27.Id,"Garruk",2);Check(g.HasStatus("Rooted")&&g.HasStatus("Poisoned"),"E027 Giant Snake applies Constrict and Venomous Fangs after damage.");
            Clean();var e36=Spawn("E036");Put(g,"L052");Check(gm.DebugEnemyAttack0859(e36.Id,"Garruk")>=e36.Attack+2,"E036 Veiled Agent Hidden Blade gains +2 Attack against an isolated target.");
            Clean();var e64=Spawn("E064");Check(e64!=null&&gm.DebugEnemyMovement0859(e64.Id)>=0,"E064 Ashen Compact Captain is registered with authored command behavior.");
            Clean();var e102=Spawn("E102");Put(g,"L053");Put(a,"L053");Check(gm.DebugEnemyRange0859(e102.Id)==1,"E102 Kraken Spawn Reach opens connected Range when no Hero is in its Location.");gm.DebugApplyEnemyPostAttack0859(e102.Id,"Garruk",2);Check(g.HasStatus("Enemy Move Surcharge"),"E102 Grapple adds +1 Action to the Hero's next Move.");
            Clean();var e113=Spawn("E113");Put(g,"L052");gm.AddStatus(g,"Inspired",0,1,"0.8.59b validation",true,g,"until end of round");Check(gm.DebugEnemyReplacementAbility0859(e113.Id)&&!g.HasStatus("Inspired")&&g.HasStatus("Weakened"),"E113 Crownscar strips Inspired and applies its attack penalty through Crownscar.");

            // Terrain / mobility / target behavior.
            Clean();var e42=Spawn("E042");var e101=Spawn("E101");Check(gm.DebugEnemyIgnoresDifficult0859(e42.Id)&&gm.DebugEnemyIgnoresDifficult0859(e101.Id),"E042 Wraith and E101 Stormray ignore authored terrain movement costs.");
            Clean();var e84=Spawn("E084");var e123=Spawn("E123");Check(gm.DebugEnemyImmovable0859(e84.Id)&&gm.DebugEnemyImmovable0859(e123.Id),"Deep Roots / Immovable boss traits are live.");
            Clean();var e47=Spawn("E047");g.Health=4;a.Health=9;gm.DebugPrepareEnemyPhase0859();Check(gm.DebugEnemyFocusHero0859(e47.Id)==g.Id,"E047 Choose the Victim Marks the lowest-Health Hero at Enemy Phase start.");Check(gm.DebugEnemyAttack0859(e47.Id,g.Id)>=e47.Attack+2,"E047 gains +2 Attack against its Marked victim.");

            // Damage mitigation, immunity, and retaliation.
            Clean();var e15=Spawn("E015");int d1=gm.DebugModifyEnemyIncomingDamage0859(e15.Id,5);int d2=gm.DebugModifyEnemyIncomingDamage0859(e15.Id,5);Check(d1==3&&d2==5,"E015 Veteran Warrior reduces only the first attack damage each Round by 2.");
            Clean();var e31=Spawn("E031");Check(gm.DebugModifyEnemyIncomingDamage0859(e31.Id,5)==0&&gm.DebugModifyEnemyIncomingDamage0859(e31.Id,5)==5,"E031 Ethereal Form negates only the first successful hit each Round.");
            Clean();var e108=Spawn("E108");Check(gm.DebugModifyEnemyIncomingDamage0859(e108.Id,5,true)==4&&gm.DebugModifyEnemyIncomingDamage0859(e108.Id,5,true)==5,"E108 Glass Carapace reduces only the first ranged Attack each Round.");
            Clean();var e61=Spawn("E061");Check(gm.DebugModifyEnemyIncomingDamage0859(e61.Id,7,false,"Garruk",true)==0,"E061 Living Flame is immune to Fire damage.");
            Clean();var e110=Spawn("E110");Check(gm.DebugModifyEnemyIncomingDamage0859(e110.Id,5)==4&&gm.DebugModifyEnemyIncomingDamage0859(e110.Id,5)==5,"E110 Afterimage reduces the first Hero Attack each Round by 1.");
            Clean();var e116=Spawn("E116");Put(g,"L052");g.Health=g.MaxHealth;gm.DealDirectDamage(e116,5,"0.8.59b validation",g);Check(g.Health==g.MaxHealth-1,"E116 Glass Body reflects 1 damage on its first 5+ damage hit each Round.");
            Clean();var e120=Spawn("E120");g.Health=g.MaxHealth;gm.DealDirectDamage(e120,6,"0.8.59b validation",g);Check(g.Health==g.MaxHealth-1,"E120 Noon Reflection reflects 1 damage on its first 6+ damage hit each Round.");

            // Support / replacement abilities.
            Clean();var wounded=Spawn("E001");wounded.Health=Mathf.Max(1,wounded.MaxHealth-2);var e5=Spawn("E005");gm.DebugPrepareEnemyActivation0859(e5.Id);Check(wounded.Health==wounded.MaxHealth-1,"E005 Tribal Blessing heals another wounded Goblin at activation start.");Put(g,"L052");Check(gm.DebugEnemyReplacementAbility0859(e5.Id)&&g.HasStatus("Weakened"),"E005 Crude Hex replaces its basic Attack and applies Weakened.");
            Clean();var e21=Spawn("E021");var wolf=Spawn("E020");gm.DebugPrepareEnemyActivation0859(e21.Id);Check(gm.DebugEnemyAttack0859(wolf.Id)>=wolf.Attack+2,"E021 Alpha's Howl buffs nearby Beasts while preserving Pack Hunter.");
            Clean();var e72=Spawn("E072");var plant=Spawn("E069");plant.Health=Mathf.Max(1,plant.MaxHealth-3);gm.DebugPrepareEnemyActivation0859(e72.Id);Check(plant.Health==plant.MaxHealth-1,"E072 Fungal Mend heals a wounded Blighted/Plant ally by 2.");
            Clean();var e79=Spawn("E079");Put(g,"L052");Check(gm.DebugModifyEnemyHealing0859(g.Id,2)==1,"E079 Spore Field reduces Hero recovery by 1 in its Location.");
            Clean();var e91=Spawn("E091");Put(g,"L052");gm.AddStatus(g,"Guarded",0,1,"0.8.59b validation",true,g,"until end of round");Check(gm.DebugEnemyReplacementAbility0859(e91.Id)&&!g.HasStatus("Guarded")&&g.HasStatus("Enemy Healing -1"),"E091 Salt Curse strips Guarded and applies -1 healing.");
            Clean();var e122=Spawn("E122");Put(g,"L052");gm.AddStatus(g,"Inspired",0,1,"0.8.59b validation",true,g,"until end of round");Check(!g.HasStatus("Inspired"),"E122 Black Sun Aura prevents Heroes in its Location from gaining Inspired.");

            // Enemy-created hazards and route pressure.
            Clean();var e12=Spawn("E012");Check(gm.DebugEnemyHazardAbility0859(e12.Id)&&gm.DebugEnemyHazardCount0859("Snare")==1,"E012 Set Snare creates one live Snare hazard.");var snare=gm.EnemyHazards0859.FirstOrDefault(h=>h.Type=="Snare");g.Health=g.MaxHealth;if(snare!=null)g.LocationId=snare.LocationId;gm.DebugResolveEnemyHazardEntry0859(g.Id);Check(g.Health==g.MaxHealth-2&&g.HasStatus("Movement Ended")&&gm.DebugEnemyHazardCount0859("Snare")==0,"E012 Snare deals 2 damage, ends movement, and is consumed on first Hero entry.");
            Clean();var e14=Spawn("E014");Check(gm.DebugEnemyHazardAbility0859(e14.Id)&&gm.DebugEnemyHazardCount0859("Fire")==1,"E014 Fire Bomb creates a persistent Fire hazard.");
            Clean();var e16=Spawn("E016");Check(gm.DebugEnemyHazardAbility0859(e16.Id)&&gm.DebugEnemyHazardCount0859("Barricade")==1,"E016 Construct Barricade creates one Barricade in its Location.");Check(gm.DebugEnemyHeroMoveSurcharge0859(g.Id,"L052")>=1,"E016 Barricade adds +1 Action to Hero entry.");var gob=Spawn("E001");Check(gm.DebugEnemyArmor0859(gob.Id)>=gob.Armor+2,"E016 Barricade grants allied Goblins +2 Armor.");
            Clean();var e53=Spawn("E053");Put(g,"L052");Check(gm.DebugEnemyReplacementAbility0859(e53.Id)&&gm.DebugEnemyHazardCount0859("Charge")==1,"E053 Set Charge replaces an Attack with a delayed Charge hazard.");g.Health=g.MaxHealth;gm.DebugResolveEnemyEndPhaseHazards0859();Check(g.Health==g.MaxHealth,"E053 Charge waits until the authored later Enemy Phase before detonating.");

            // Post-hit debuffs / forced penalties.
            Clean();var e20b=Spawn("E020");Put(g,"L052");gm.DebugApplyEnemyPostAttack0859(e20b.Id,g.Id,2);Check(g.HasStatus("Enemy Move Surcharge"),"E020 Hamstring Bite increases the Hero's next Move cost.");
            Clean();var e21b=Spawn("E021");gm.DebugSetEnemyMovementSteps0859(e21b.Id,2);gm.DebugApplyEnemyPostAttack0859(e21b.Id,g.Id,2);Check(g.HasStatus("Knocked Down"),"E021 Savage Pounce knocks down a damaged Hero after moving 2+ Locations.");
            Clean();e108=Spawn("E108");gm.DebugApplyEnemyPostAttack0859(e108.Id,g.Id,2);Check(g.HasStatus("Enemy Healing -1"),"E108 Venom Tail applies -1 healing to a damaged Hero.");

            // Boss objective bindings from later campaigns.
            gm.DebugStartQuest0829("Q036");yield return null;var boss67=gm.DebugSpawnEnemy0859("E067","L124","DBG_Q036_E067");Check(boss67!=null&&gm.DebugEnemyHealthFloor0859(boss67.Id)==14,"E067 Worldforge Harness enforces the Q036 14-Health floor while Mountain Wards remain.");
            gm.DebugStartQuest0829("Q063");yield return null;var boss115=gm.DebugSpawnEnemy0859("E115","L215","DBG_Q063_E115");Check(boss115!=null&&gm.DebugEnemyHealthFloor0859(boss115.Id)==15,"E115 Seal-Bound enforces the Q063 15-Health floor while Solar Seals remain.");
            gm.DebugStartQuest0829("Q066");yield return null;var boss120=gm.DebugSpawnEnemy0859("E120","L223","DBG_Q066_E120");Check(boss120!=null&&gm.DebugEnemyHealthFloor0859(boss120.Id)==16,"E120 Ward-Bound enforces the Q066 16-Health floor while Mirror Wards remain.");
            gm.DebugStartQuest0829("Q069");yield return null;var boss123=gm.DebugSpawnEnemy0859("E123","L227","DBG_Q069_E123");Check(boss123!=null&&gm.DebugEnemyImmovable0859(boss123.Id),"E123 Zenith Colossus preserves its Immovable boss rule.");
            gm.DebugStartQuest0829("Q072");yield return null;var boss124=gm.DebugSpawnEnemy0859("E124","L235","DBG_Q072_E124");Check(boss124!=null&&gm.DebugEnemyHealthFloor0859(boss124.Id)==18,"E124 Crown-Bound enforces the Q072 18-Health floor while Solar Crown Wards remain.");

            // Save capture — no disk mutation.
            gm.DebugClearEnemyRuntime0859();var saveEnemy=gm.DebugSpawnEnemy0859("E012","L235","DBG_SAVE_E012");saveEnemy.UsedKeys0859.Add("ENEMY0859:VALIDATION:SAVE");saveEnemy.MovementStepsThisActivation0859=2;saveEnemy.FocusHeroId0859="Garruk";gm.DebugEnemyHazardAbility0859(saveEnemy.Id);
            var capture=typeof(AscendanceSaveSystem).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);var data=capture?.Invoke(null,new object[]{gm}) as AscendanceSaveData;var stored=data?.Enemies?.FirstOrDefault(x=>x.Id==saveEnemy.Id);
            Check(data!=null&&stored!=null&&stored.UsedKeys0859.Contains("ENEMY0859:VALIDATION:SAVE")&&stored.MovementStepsThisActivation0859==2&&stored.FocusHeroId0859=="Garruk","per-Enemy once/focus/movement runtime state is captured into campaign save data.");
            Check(data!=null&&data.EnemyHazards0859!=null&&data.EnemyHazards0859.Count>0,"Enemy-created hazard state is captured into campaign save data.");
            Check(string.IsNullOrWhiteSpace(gm.PendingContentInstruction),"0.8.59b validation path finishes without an Enemy CONTENT PENDING fallback.");

            gm.DebugRemoveEnemies0859();
            if(fail==0)Debug.Log($"[ASCENDANCE][0.8.59b TEST] PASS — Full Enemy Runtime Execution completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.8.59b TEST] FAIL — Full Enemy Runtime Execution completed: {pass} passed, {fail} failed.");
        }
    }
}
#endif
