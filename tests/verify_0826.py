from pathlib import Path
import re,sys
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(encoding='utf-8')
content=(root/'Assets/Scripts/Core/SearchEncounterRuntime.cs').read_text(encoding='utf-8')
models=(root/'Assets/Scripts/Core/AscendanceModels.cs').read_text(encoding='utf-8')
save=(root/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text(encoding='utf-8')
saver=(root/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text(encoding='utf-8')
hud=(root/'Assets/Scripts/UI/AscendanceHUD.cs').read_text(encoding='utf-8')
world=(root/'Assets/Scripts/World/StonebrookWorldBuilder.cs').read_text(encoding='utf-8')
test=(root/'Assets/Editor/EncounterConsequencesTestTools0823.cs').read_text(encoding='utf-8')
test824=(root/'Assets/Editor/EncounterEnemyMovementInitiativeTestTools0824.cs').read_text(encoding='utf-8')
test825=(root/'Assets/Editor/EncounterDeckControlTestTools0825.cs').read_text(encoding='utf-8')
avatar=(root/'Assets/Scripts/World/EncounterObjectAvatar.cs').read_text(encoding='utf-8')
reward=(root/'Assets/Scripts/Core/RewardContent0826.cs').read_text(encoding='utf-8')
test826=(root/'Assets/Editor/LootEquipmentReputationTestTools0826.cs').read_text(encoding='utf-8')
checks=[]
def check(label,cond):checks.append((label,bool(cond)))
check('deprecated FindObjectOfType removed', 'FindObjectOfType<' not in gm)
check('BuildVersion 0.8.26','public const string BuildVersion = "0.8.26";' in gm)
check('0.8.26 load banner','Loot, Equipment & Reputation Integration loaded.' in gm)
check('Search content type','class SearchCardDefinition' in content)
check('Encounter content type','class EncounterCardDefinition' in content)
check('Enemy template type','class EncounterEnemyTemplate' in content)
for n in range(1,21):check(f'SD{n:03d} authored',f'"SD{n:03d}"' in content)
for n in range(1,31):check(f'EN{n:03d} authored',f'"EN{n:03d}"' in content)
for eid in ['E001','E002','E003','E004','E005','E006','E007','E008','E011','E012','E013','E014','E017','E018','E020','E021','E022','E023','E024','E025','E026','E027','E028','E029']:
    check('enemy template '+eid,f'AddEnemy("{eid}"' in content)
check('WE001 severity modifier','const int worldEventModifier=1' in gm and 'UnityEngine.Random.Range(1,7)' in gm)
check('severity bands','EffectForSeverity' in content and 'Severity12' in content and 'Severity34' in content and 'Severity56' in content and 'Severity7Plus' in content)
check('terrain eligibility','MatchesLocation(LocationState location,string terrainSpec)' in content and 'Any Outdoor' in content)
check('search resolves real draw','ResolveSearchDraw(location, hero)' in gm and 'SEARCH CARD —' in gm)
check('first visit resolves real encounter','ResolveTravelEncounter(location, hero, level, 0, "First Visit")' in gm)
check('compat search hook kept','SearchRequested?.Invoke(location,hero)' in gm)
check('compat travel hook kept','TravelEncounterRequested?.Invoke(location,hero,level)' in gm)
check('PartyGold runtime','public int PartyGold' in gm and 'AddPartyGold' in gm)
check('Search deck runtime','public readonly List<string> SearchDeck' in gm and 'public readonly List<string> SearchDiscard' in gm)
check('Encounter deck runtime','public readonly List<string> EncounterDeck' in gm and 'public readonly List<string> EncounterDiscard' in gm)
check('deck initialization','InitializeSearchEncounterDecks();' in gm)
check('search discard','SearchDiscard.Add(card.Id)' in gm)
check('encounter discard','EncounterDiscard.Add(card.Id)' in gm)
check('dynamic encounter spawn','SpawnEncounterEnemy' in gm and 'EncounterSpawned=true' in gm)
check('presentation uses TemplateId','enemy.TemplateId' in world and 'EnsureRuntimeEnemySpawns' in world)
check('next-turn action penalty','NextTurnActionPenalty' in models and 'NextTurnActionPenalty' in save and 'NextTurnActionPenalty' in saver and 'hero.NextTurnActionPenalty=0' in gm)
check('pending content retained','PendingContentInstruction' in gm and 'CONTENT PENDING' in gm and 'PendingContentInstruction' in save and 'PendingContentInstruction' in saver)
check('Gold visible in HUD','gm.PartyGold' in hud)
check('pending content visible in HUD','PENDING CONTENT' in hud)
for f in ['PartyGold','LastSearchCardId','LastEncounterCardId','LastEncounterSeverity','PendingContentInstruction','EncounterSpawnSerial','SearchDeckIds','SearchDiscardIds','EncounterDeckIds','EncounterDiscardIds']:
    check('save root '+f,f in save and f in saver)
for f in ['TemplateId','EncounterSpawned','MaxHealth','Attack','Armor','IsBoss']:
    check('enemy save '+f,f in save and f in saver)
check('dynamic load rebuild','CreateEncounterEnemy(stored.TemplateId' in gm and 'stored.EncounterSpawned' in gm)
check('SD002 gold implementation','case "SD002": AddPartyGold(15' in gm)
check('SD013 conditional gold','case "SD013"' in gm and 'HasPrintedTag("Ancient")' in gm)
check('SD014 encounter chain','case "SD014"' in gm and 'ResolveTravelEncounter(location,hero' in gm)
check('SD015 party heal','case "SD015"' in gm and 'HealHero(h,1,card.Name,hero)' in gm)
check('SD016 cleanse','case "SD016": RemoveFirstHarmfulStatus' in gm)
check('EN001 spawn parsing','ResolveEncounterEnemySpawns' in gm and '@"(\\d+)\\s+(E\\d{3})"' in gm)
check('EN006 action penalty','card.Id=="EN006"&&severity<=2' in gm)
check('EN030 recovery','card.Id=="EN030"' in gm)
check('Play Mode menu','Test 0.8.23 - Campaign I Encounter Consequences (Play Mode)' in test)
check('travel severity state','NextTravelEncounterSeverityModifier' in gm and 'NextTravelEncounterSeverityModifier' in save and 'NextTravelEncounterSeverityModifier' in saver)
check('skip encounter state','SkipNextTravelEncounter' in gm and 'SkipNextTravelEncounter' in save and 'SkipNextTravelEncounter' in saver)
check('queued modifier consumption','queuedSeverityModifier=fixedSeverity>0?0:NextTravelEncounterSeverityModifier' in gm and 'if(fixedSeverity<=0)NextTravelEncounterSeverityModifier=0' in gm and 'pending' in gm)
check('skip without draw','TRAVEL ENCOUNTER SKIPPED' in gm and gm.index('if(!ignoreTravelControl&&SkipNextTravelEncounter)') < gm.index('var card=DrawEligibleEncounterCard(location)'))
check('EN030 queues +1','card.Id=="EN030"' in gm and 'QueueNextTravelEncounterSeverity(1,card.Name)' in gm)
check('EN028 queues skip','card.Id=="EN028"' in gm and 'if(severity>=7){QueueSkipNextTravelEncounter(card.Name);return;}' in gm)
check('EN026 cancels queued modifier','encounter.CardId=="EN026"' in gm and 'CancelPendingTravelSeverity(encounter.CardName)' in gm)
check('EN002 party-wide setup damage','card.Id=="EN002"&&severity>=5' in gm and 'ParticipatingHeroes.Where(h=>!h.Dead))ApplyHeroDamageNow' in gm)
check('EN010 Slowed setup','card.Id=="EN010"&&severity>=7' in gm and 'AddStatus(h,"Slowed"' in gm)
check('EN021 flood roll/slowed','card.Id=="EN021"' in gm and 'Flooded Crossing' in gm and 'UnityEngine.Random.Range(1,7)' in gm)
check('EN024 fire objects','card.Id=="EN024"&&severity>=3' in gm and 'Firestarter Raid remaining Fire Tokens' in gm)
check('EN024 supply loss','int loss=fires.Count*5' in gm and 'AddPartyGold(-loss,"Firestarter Raid remaining Fire Tokens")' in gm)
check('EN024 reward gating','encounter.CardId=="EN024"' in gm and 'Reputation bonus was not earned' in gm)
for phrase in ['EN002 Severity 5+ deals its authored 1 setup damage','EN010 Severity 7+ applies Slowed','EN030 Severity 7+ queues +1 Severity','queued EN030 modifier','consumed exactly once','EN028 Severity 7+ queues Skip','next Travel Encounter is skipped','EN024 Severity 5 places two Fire Tokens','loses 5 Gold in supplies','EN026 victory cancels','Campaign I Encounter Consequences completed']:
    check('test '+phrase,phrase in test)


# 0.8.21 lifecycle/reward contract preserved.
check('EncounterRuntimeState model','class EncounterRuntimeState' in models and 'SpawnedEnemyIds' in models and 'RewardResolved' in models)
check('enemy encounter ownership','EncounterInstanceId' in models and 'EncounterInstanceId' in save and 'EncounterInstanceId=e.EncounterInstanceId' in saver)
check('active encounter collection','public readonly List<EncounterRuntimeState> ActiveEncounters' in gm)
check('encounter instance serial','encounterInstanceSerial' in gm and 'EncounterInstanceSerial' in save and 'EncounterInstanceSerialForSave' in saver)
check('completion counters saved','CompletedEncounterCount' in gm and 'CompletedEncounterCount' in save and 'LastCompletedEncounterCardId' in saver)
check('active encounters saved','List<EncounterRuntimeSaveData> ActiveEncounters' in save and 'foreach(var encounter in gm.ActiveEncounters)' in saver and 'RestoreActiveEncounterState(save);' in gm)
check('spawn binds encounter','EncounterInstanceId=encounterInstanceId' in gm and 'encounter?.SpawnedEnemyIds.Add(enemy.Id)' in gm)
check('exact encounter clear check','CheckEncounterCompletionForEnemy' in gm and 'RemainingEncounterEnemies' in gm and 'TryCompleteEncounter' in gm)
check('normal Hero attack checks encounter completion','CheckEncounterCompletionForEnemy(enemy);CheckVictory();PrepareHeroKillReactionWindow' in gm)
check('direct damage checks encounter completion','CheckEncounterCompletionForEnemy(enemy);CheckVictory();}else' in gm)
check('reward deferred while active','reward is held until all are cleared' in gm and 'ENCOUNTER ACTIVE —' in gm)
check('EN001 elite reward','eliteDefeated' in gm and '15+(eliteDefeated*5)' in gm and 'IsEliteEnemyTemplate' in content)
check('conditional E012 reward parser',r'Gold\s+if\s+(E\d{3})' in gm and 'conditional Gold' in gm)
check('direct Gold reward parser','var direct=Regex.Match(reward' in gm and 'victory reward' in gm)
check('unsupported rewards remain pending','reward choice' in gm and 'victory reward' in gm and 'AppendPendingContent' in gm)
check('active Encounter shown in HUD','TRAVEL ENCOUNTER:' in hud and 'gm.ActiveEncounterSummary' in hud)


# 0.8.22 persistent Encounter object contract.
check('EncounterObjectState model','class EncounterObjectState' in models and 'BlocksEncounterCompletion' in models and 'InteractionActionCost' in models)
check('encounter object collection','public readonly List<EncounterObjectState> EncounterObjects' in gm and 'EncounterObjectSerialForSave' in gm)
check('encounter tracks object ids','SpawnedObjectIds' in models and 'SpawnedObjectIds' in save and 'SpawnedObjectIds=encounter.SpawnedObjectIds.ToList()' in saver)
check('object save root','List<EncounterObjectSaveData> EncounterObjects' in save and 'EncounterObjectSerial' in save and 'data.EncounterObjects.Add' in saver)
check('object restore before lifecycle','RestoreEncounterObjectState(save);' in gm and gm.index('RestoreEncounterObjectState(save);') < gm.index('RestoreActiveEncounterState(save);'))
check('blocking objectives participate in clear','RemainingEncounterBlockingObjects' in gm and 'if(remaining>0||blocking>0)return false;' in gm)
check('shared object spawn','SpawnEncounterObject' in gm and 'EncounterObjectAvatar.Create(obj)' in gm)
check('shared object remove','RemoveEncounterObject' in gm and 'EncounterObjectAvatar' in gm)
check('object world restore','foreach (var obj in AscendanceGameManager.Instance.EncounterObjects) EncounterObjectAvatar.Create(obj);' in world)
check('object avatar type','class EncounterObjectAvatar' in avatar and 'Fire Token' in avatar and 'Web' in avatar and 'Snare' in avatar)
check('HUD object button','encounterObjectButton' in hud and 'InteractEncounterObject' in hud and 'EncounterObjectActionLabel' in hud)
check('HUD hazards summary','HAZARDS:' in hud and 'gm.EncounterObjectSummary' in hud)
check('EN006 snare placement','card.Id=="EN006"&&severity>=3' in gm and '"Snare"' in gm and 'snares=severity<=4?1:2' in gm)
check('snare entry trigger','ResolveEncounterObjectsOnHeroEnter' in gm and 'Trapper\'s Snare' in gm and 'Rooted until end of turn.' in gm)
check('snare movement integration','ResolveEncounterObjectsOnHeroEnter(Locations[locationId],hero);' in gm and 'ResolveEncounterObjectsOnHeroEnter(Locations[destination],hero);' in gm)
check('EN007 fire placement','card.Id=="EN007"' in gm and '"Fire Token"' in gm and 'fires=severity<=2?1:(severity<=4?1:2)' in gm)
check('fire interaction costs action','InteractionActionCost=interactionActionCost' in gm and 'hero.Actions-=cost' in gm and 'extinguishes' in gm)
check('low severity Burning Cart gold on extinguish','fireEncounter.Severity<=2' in gm and 'AddPartyGold(10,"Burning Cart extinguish")' in gm and 'if(severity<=2)return;' in gm)
check('remaining fire consequence','Burning Cart remaining Fire Tokens' in gm and 'each Hero suffers' in gm and 'RemoveEncounterObject(fire,"encounter ended",false)' in gm)
check('EN007 victory reward','encounter.CardId=="EN007"' in gm and 'AddPartyGold(25' in gm and 'AllFireTokensRemovedBeforeClear' in gm)
check('EN012 web placement','card.Id=="EN012"' in gm and '"Web","Spider Webs"' in gm and 'blocks' not in '')
check('web blocks completion','SpawnEncounterObject(encounter,"Web","Spider Webs",location,true,false,1,"Clear Web",hero.Id)' in gm)
check('web roots source Hero','Rooted until the webs are cleared.' in gm)
check('web clear removes Rooted','RemoveStatus(trapped.Statuses,"Rooted"' in gm and 'webs cleared' in gm)
check('implemented failure text no longer pending','card.Id!="EN006" && card.Id!="EN007" && card.Id!="EN012"' in gm)
check('plural typo fixed','spawned Enemyies' not in gm and 'spawned {(encounter.SpawnedEnemyIds.Count==1?"Enemy":"Enemies")}' in gm)
check('totem scope documented','EN018 Shamanic Totem' in (root/'ENCOUNTER_HAZARDS_OBJECTS_0.8.22.md').read_text(encoding='utf-8'))


# 0.8.24 Enemy movement / initiative contract.
check('0.8.24 Play Mode menu','Test 0.8.24 - Encounter Enemy Movement & Initiative (Play Mode)' in test824)
check('base enemy movement allowance','private int EnemyMovementAllowance' in gm and 'int movement=1;' in gm)
check('enemy graph movement','MoveEnemyTowardNearestHero' in gm and 'NextEnemyStepTowardHero' in gm and 'ENEMY MOVE —' in gm)
check('enemy movement targets nearest Hero','targets.Min(h=>GraphDistance' in gm and '.Where(x=>x.Distance<currentBest)' in gm)
check('revealed-only enemy pathing','l.Revealed' in gm and 'current.Connections.Where' in gm)
check('Difficult Terrain enemy exit cost','int stepCost=IsDifficultTerrain(current.Id)?2:1;' in gm)
check('Fresh Tracks movement penalty live','Fresh Tracks Movement -1' in gm and 'movement--' in gm)
check('Encounter movement bonus live','Encounter Movement +1' in gm and 'movement++' in gm)
check('EN025 Beast movement bonus','card.Id=="EN025"' in gm and 'IsBeastEnemy(enemy)' in gm and 'Beastmaster Hunt Beasts gain +1 Movement' in gm)
check('EN025 excludes Goblin Beastmaster by template filter','return n>=20&&n<=29;' in gm)
check('EN016 next Goblin movement queue','NextGoblinEncounterMovementBonus' in gm and 'card.Id=="EN016"&&severity>=3&&severity<=4' in gm)
check('EN016 queue consumed only by Goblin encounter','goblinEncounter&&NextGoblinEncounterMovementBonus' in gm and 'NextGoblinEncounterMovementBonus=false' in gm)
check('EN016 5-6 queues War Drummer','NextGoblinEncounterWarDrummer' in gm and 'card.Id=="EN016"&&severity>=5&&severity<=6' in gm and 'SpawnEncounterEnemy("E008"' in gm)
check('EN016 War Drummer saved','NextGoblinEncounterWarDrummer' in save and 'NextGoblinEncounterWarDrummer=gm.NextGoblinEncounterWarDrummer' in saver and 'NextGoblinEncounterWarDrummer = save.NextGoblinEncounterWarDrummer' in gm)
check('EN016 5-6 not spawned immediately','(card.Id=="EN016"&&severity>=5&&severity<=6)?0:ResolveEncounterEnemySpawns' in gm)
check('next Goblin movement saved','NextGoblinEncounterMovementBonus' in save and 'NextGoblinEncounterMovementBonus=gm.NextGoblinEncounterMovementBonus' in saver and 'NextGoblinEncounterMovementBonus = save.NextGoblinEncounterMovementBonus' in gm)
check('movement bonus expires end round','AddStatus(enemy,"Encounter Movement +1",0,1' in gm and '"until end of round"' in gm)
check('single enemy activation factors movement before attack','ResolveSingleEnemyActivation' in gm and gm.index('MoveEnemyTowardNearestHero(enemy);') < gm.index('var target=ChooseEnemyAttackTarget(enemy);', gm.index('private IEnumerator ResolveSingleEnemyActivation')))
check('world phase uses shared enemy activation','yield return ResolveSingleEnemyActivation(enemy);' in gm)
check('world phase skips already activated','enemy.ActivatedThisRound' in gm and 'already activated earlier this Round' in gm)
check('EN023 priority rule','EncounterRequiresPriorityActivation' in gm and 'encounter.CardId!="EN023"' in gm and 'encounter.Severity<5' in gm)
check('EN023 Hidden suppression','HasStatus("Hidden")' in gm and 'does not activate first because at least one Hero is Hidden' in gm)
check('EN023 immediate activation coroutine','ResolvePriorityEncounterActivation' in gm and 'StartCoroutine(ResolvePriorityEncounterActivation(encounter))' in gm and 'activate immediately before Heroes continue' in gm)
check('priority enemies marked to prevent double activation','CompleteEnemyActivation(enemy)' in gm and 'already activated earlier this Round' in gm)
check('ActivatedThisRound saved','ActivatedThisRound' in save and 'ActivatedThisRound=e.ActivatedThisRound' in saver and 'enemy.ActivatedThisRound=stored.ActivatedThisRound' in gm)
check('Scout non-defeat removal hook','RemoveEnemyFromPlayWithoutDefeat' in gm and 'ENEMY LEAVES PLAY' in gm)
check('Scout escape applies next severity','HadEnemyLeavePlayUndefeated=true' in gm and 'Scout escaped' in gm and 'next Travel Encounter Severity gains +1' in gm)
check('Scout escape distinguished from normal reward','HadEnemyLeavePlayUndefeated' in models and 'normal victory reward is not earned because a Scout left play' in gm)
check('Scout escape state saved','HadEnemyLeavePlayUndefeated' in save and 'HadEnemyLeavePlayUndefeated=encounter.HadEnemyLeavePlayUndefeated' in saver and 'HadEnemyLeavePlayUndefeated=stored.HadEnemyLeavePlayUndefeated' in gm)
check('implemented encounter failure text removed from pending','card.Id!="EN016" && card.Id!="EN023" && card.Id!="EN025"' in gm)
for phrase in ['Normal Enemy activation has 1 Movement','Fresh Tracks reduces a normal Enemy','cannot leave Difficult Terrain','EN025 gives its Dire Wolf +1 Movement','War Drums queues +1 Movement','EN016 Severity 5 queues a War Drummer','immediately activates every Enemy','Hidden Hero suppresses EN023','Scout leaving play without being Defeated','Encounter Enemy Movement & Initiative completed']:
    check('0.8.24 test '+phrase, phrase in test824)

# 0.8.25 Encounter deck control / remaining deterministic Campaign I rules.
check('0.8.25 Play Mode menu','Test 0.8.25 - Encounter Deck Control & Remaining Rules (Play Mode)' in test825)
check('deck control state model','class EncounterDeckControlState' in models and 'RevealedCardIds' in models and 'class EncounterDeckControlOption' in models)
check('deck control runtime property','PendingEncounterDeckControl' in gm and 'HasPendingEncounterDeckControl' in gm and 'EncounterDeckControlOptions' in gm)
check('deck control save model','EncounterDeckControlSaveData PendingEncounterDeckControl' in save and 'class EncounterDeckControlSaveData' in save)
check('deck control save capture','PendingEncounterDeckControl=gm.PendingEncounterDeckControl==null?null:new EncounterDeckControlSaveData' in saver)
check('deck control save restore','PendingEncounterDeckControl = save.PendingEncounterDeckControl==null?null:new EncounterDeckControlState' in gm)
check('deck control snapshot safety','DeckControlSnapshotStillMatches' in gm and 'canceling the stale choice' in gm)
check('deck control duplicate-safe index manipulation','BottomEncounterDeckIndex' in gm and 'EncounterDeck.RemoveAt(index)' in gm and 'ReorderEncounterDeckTop' in gm)
check('top one bottom mode','"MayBottomTop"' in gm and 'Key="BOTTOM0"' in gm)
check('top two reorder mode','"ReorderTop2"' in gm and 'Key="SWAP"' in gm)
check('top two bottom-one mode','"BottomOneTop2"' in gm and 'Key="BOTTOM1"' in gm)
check('top three six permutations','"ReorderTop3"' in gm and 'int[][] perms=' in gm and 'PERM' in gm)
check('deck control blocks Hero actions','!HasPendingEncounterDeckControl && IsHeroTakingTurn(hero)' in gm and '!HasPendingEncounterDeckControl && !ParticipatingHeroes.Any' in gm)
check('deck control blocks End Turn','IsResolving||HasPendingReactionWindow||HasPendingEncounterDeckControl' in gm)
check('deck control HUD modal','deckControlPanel' in hud and 'RefreshEncounterDeckControl' in hud and 'gm.EncounterDeckControlOptions' in hud)
check('Reaction window has UI priority over deck control','gm.HasPendingEncounterDeckControl&&!gm.HasPendingReactionWindow' in hud and 'gm.HasPendingReactionWindow?"REACTION":gm.HasPendingEncounterDeckControl?"DECK CONTROL"' in hud)
check('world resolution waits on deck control','WaitForEncounterDeckControlIfNeeded' in gm and 'while(HasPendingEncounterDeckControl)yield return null;' in gm and 'yield return WaitForEncounterDeckControlIfNeeded();' in gm)
check('deck control HUD summary','DECK CONTROL:' in hud and 'PendingEncounterDeckControlText' in hud)
check('EN003 reward deck control','encounter.CardId=="EN003"' in gm and 'BeginEncounterDeckControl(encounter.CardId,encounter.CardName,"MayBottomTop",1)' in gm)
check('EN022 reward deck control','encounter.CardId=="EN022"' in gm and 'BeginEncounterDeckControl(encounter.CardId,encounter.CardName,"ReorderTop2",2)' in gm)
check('EN028 severity 3-4 control','BeginEncounterDeckControl(card.Id,card.Name,"BottomOneTop2",2)' in gm)
check('EN028 severity 5-6 control','BeginEncounterDeckControl(card.Id,card.Name,"ReorderTop3",3)' in gm)
check('EN028 severity 7 skip preserved','if(severity>=7){QueueSkipNextTravelEncounter(card.Name);return;}' in gm)
check('EN009 first activation attack bonus','card.Id=="EN009"&&severity>=7' in gm and 'Encounter Attack +1' in gm and 'Abandoned Goblin Camp Saboteur gains +1 Attack' in gm)
check('enemy attack consumes EN009 bonus through status clock','EffectiveEnemyAttack' in gm and 'Count(s=>string.Equals(s.Name,"Encounter Attack +1"' in gm and 'EndOfTargetActivation' in gm)
check('EN027 fixed follow-up','card.Id=="EN027"&&severity>=7' in gm and 'Severity 3-4 band' in gm and 'ResolveTravelEncounter(location,hero,1,0,card.Name+" follow-up",0,3,true)' in gm)
check('fixed severity runtime option','int fixedSeverity=0,bool ignoreTravelControl=false' in gm and 'fixedSeverity>0?Mathf.Max(1,fixedSeverity)' in gm and 'DebugResolveFixedTravelEncounter' in gm)
check('fixed severity preserves queued modifier','int queuedSeverityModifier=fixedSeverity>0?0:NextTravelEncounterSeverityModifier' in gm and 'if(fixedSeverity<=0)NextTravelEncounterSeverityModifier=0' in gm)
check('implemented Scout consequence removed from pending','card.Id!="EN003"' in gm and 'card.Id!="EN026"' in gm)
check('implemented EN009 consequence removed from pending','!(card.Id=="EN009"&&severity>=7)' in gm)
check('implemented EN027 consequence removed from pending','!(card.Id=="EN027"&&severity>=7)' in gm)
for phrase in ['EN028 Severity 3-4 opens a real top-two','EN028 top-three control exposes all six','EN003 victory awards 10 Gold','EN022 victory awards 25 Gold','EN009 Severity 7 gives its spawned Saboteur','EN027 Severity 7 immediately resolves','Encounter Deck Control & Remaining Campaign I Rules completed']:
    check('0.8.25 test '+phrase,phrase in test825)


# 0.8.26 Loot, Equipment & General Reputation runtime.
check('0.8.26 Play Mode menu','Test 0.8.26 - Loot Equipment & Reputation (Play Mode)' in test826)
check('loot runtime catalog type','class LootRuntimeDefinition' in reward and 'Dictionary<string,LootRuntimeDefinition>' in reward)
check('equipment runtime catalog type','class EquipmentCatalogDefinition' in reward and 'Dictionary<string,EquipmentCatalogDefinition>' in reward)
check('190 Loot definitions constant','LootDefinitionCount = 190' in reward)
check('254 physical Loot copies constant','LootPhysicalCopyCount = 254' in reward)
check('144 Equipment definitions constant','EquipmentDefinitionCount = 144' in reward)
check('all workbook Loot rows authored',reward.count('AddLoot("LT')==190)
check('all workbook Equipment rows authored',reward.count('AddEquipment("EQ')==144)
check('Loot deck built from physical copies','for(int i=0;i<def.Copies;i++) deck.Add(def.Id)' in reward)
check('Equipment deck built from catalog','BuildEquipmentDeck()=>new List<string>(Equipment.Keys)' in reward)
check('Hero Loot inventory model','LootInventory = new()' in models)
check('Hero acquired Equipment model','AcquiredEquipment = new()' in models)
check('General Reputation runtime','GeneralReputation { get; private set; }' in gm and 'Mathf.Clamp(GeneralReputation+amount,-5,6)' in gm)
check('Loot runtime decks','public readonly List<string> LootDeck = new();' in gm and 'LootDiscard = new();' in gm)
check('Equipment runtime decks','public readonly List<string> EquipmentDeck = new();' in gm and 'EquipmentDiscard = new();' in gm)
check('reward decks initialize','InitializeRewardDecks();' in gm and 'RewardContent0826.BuildLootDeck()' in gm and 'RewardContent0826.BuildEquipmentDeck()' in gm)
check('reward decks save','LootDeckIds=gm.LootDeck.ToList()' in saver and 'EquipmentDeckIds=gm.EquipmentDeck.ToList()' in saver)
check('reward decks restore','RestoreRewardDeckState(save)' in gm and 'authoredRewardSave' in gm)
check('Hero reward inventories save','LootInventoryIds=h.LootInventory.ToList()' in saver and 'AcquiredEquipmentIds=h.AcquiredEquipment.ToList()' in saver)
check('Hero reward inventories restore','stored.LootInventoryIds' in gm and 'stored.AcquiredEquipmentIds' in gm)
check('General Reputation save','GeneralReputation=gm.GeneralReputation' in saver and 'GeneralReputation = Mathf.Clamp(save.GeneralReputation,-5,6)' in gm)
check('Loot draw owns physical card','LootDeck.RemoveAt(0);hero.LootInventory.Add(id)' in gm)
check('Equipment draw is unequipped inventory','EquipmentDeck.RemoveAt(0);hero.AcquiredEquipment.Add(id)' in gm and 'unequipped' in gm)
check('reward choice model','class RewardChoiceState' in models and 'class RewardChoiceOption' in models)
check('reward choice save','RewardChoiceSaveData PendingRewardChoice' in save and 'PendingRewardChoice=gm.PendingRewardChoice==null?null:new RewardChoiceSaveData' in saver)
check('reward choice restore','PendingRewardChoice = save.PendingRewardChoice==null?null:new RewardChoiceState' in gm)
check('reward choice blocks actions','IsHeroTakingTurn(hero) && !HasPendingRewardChoice' in gm and 'HasPendingEncounterDeckControl||HasPendingRewardChoice' in gm)
check('reward choice world pause','while(HasPendingRewardChoice)yield return null;' in gm)
check('reward choice HUD','rewardChoicePanel' in hud and 'RefreshRewardChoice' in hud and 'gm.RewardChoiceOptions' in hud)
check('reward choice UI priority','gm.HasPendingRewardChoice&&!gm.HasPendingReactionWindow&&!gm.HasPendingEncounterDeckControl' in hud)
check('General Reputation HUD','REP {gm.GeneralReputation' in hud and 'GENERAL REPUTATION:' in hud)
check('inventory HUD','hero.LootInventory.Count' in hud and 'hero.AcquiredEquipment.Count' in hud)
check('EN004 Loot reward','encounter.CardId=="EN004"' in gm and 'DrawLootToHero(rewardHero' in gm)
check('EN005 real choice','encounter.CardId=="EN005"' in gm and '"Gold25OrLoot1"' in gm)
check('EN007 conditional Item real','encounter.AllFireTokensRemovedBeforeClear)DrawLootToHero' in gm)
check('EN008 low Item real','card.Id=="EN008"' in gm and 'DrawLootToHero(hero,card.Name);return;' in gm)
check('EN008 pay Reputation choice','"Pay10RepOrContinue"' in gm and 'AddGeneralReputation(1,state.SourceCardName)' in gm)
check('EN009 Loot/Equipment immediate','card.Id=="EN009"' in gm and 'DrawEquipmentToHero(hero,card.Name,encounter)' in gm)
check('EN012 compound Loot reward','encounter.CardId=="EN012"' in gm and 'Severity 5+ Item' in gm)
check('EN013 real choice','encounter.CardId=="EN013"' in gm and '"Loot1OrGold20"' in gm)
check('EN014 real choice','encounter.CardId=="EN014"' in gm and '"Gold30OrLoot1"' in gm)
check('EN017 Equipment reward tracked','card.Id=="EN017"' in gm and 'encounter?.GainedEquipmentIds.Add(id)' in gm)
check('EN017 escape rollback','UndefeatedEliteExitCount' in gm and 'both Elite Goblins left Saboteur' in gm and 'DiscardAcquiredEquipment' in gm)
check('EN017 reward tracking saved','UndefeatedEliteExitCount' in save and 'GainedEquipmentIds' in save and 'GainedEquipmentIds=encounter.GainedEquipmentIds.ToList()' in saver)
check('EN019 Campaign I General Reputation','card.Id=="EN019"&&severity<=2' in gm and 'encounter.CardId=="EN019"&&encounter.Severity>=5' in gm)
check('EN024 conditional Reputation real','encounter.CardId=="EN024"' in gm and 'AddGeneralReputation(1,$"{encounter.CardId} {encounter.CardName} all Fire removed")' in gm)
check('EN024 validator uses legal Fire interaction location','priorFireTestLocation=hero.LocationId;hero.LocationId=forest.Id' in test826 and 'gm.DebugInteractEncounterObject(hero.Id,fire.ObjectId)' in test826 and 'hero.LocationId=priorFireTestLocation' in test826)
check('EN025 Gold plus Loot','encounter.CardId=="EN025"' in gm and 'AddPartyGold(35' in gm and 'DrawLootToHero(rewardHero' in gm)
check('EN027 Item/Equipment real','card.Id=="EN027"' in gm and 'DrawEquipmentToHero(hero,card.Name,encounter)' in gm)
check('EN002 false pending cleanup','card.Id!="EN002"' in gm)
check('EN013 false pending cleanup','card.Id!="EN013"' in gm)
for phrase in ['190 definitions / 254 physical copies','144 authored cards','EN004 victory draws the actual top Loot','EN005 opens a player-facing','EN008 payment spends 10 Party Gold','clamped at the authored +6','EN009 Severity 7 immediately draws','EN017 tracks the exact Equipment','both Elite Goblins leave play undefeated','EN024 awards +1 General Reputation','EN025 awards 35 Gold','Loot, Equipment & Reputation Integration completed']:
    check('0.8.26 test '+phrase,phrase in test826)


# Compile-safety scanners reused from prior verified suites.
def stripped(text):
    out=[];i=0;mode='code';esc=False
    while i<len(text):
        c=text[i];n=text[i+1] if i+1<len(text) else ''
        if mode=='code':
            if c=='/' and n=='/':mode='line';out.extend('  ');i+=2;continue
            if c=='/' and n=='*':mode='block';out.extend('  ');i+=2;continue
            if c=='@' and n=='"':mode='vstr';out.extend('  ');i+=2;continue
            if c=='"':mode='str';out.append(' ');i+=1;continue
            if c=="'":mode='char';out.append(' ');i+=1;continue
            out.append(c);i+=1;continue
        if mode=='line':
            if c=='\n':mode='code';out.append('\n')
            else:out.append(' ')
            i+=1;continue
        if mode=='block':
            if c=='*' and n=='/':mode='code';out.extend('  ');i+=2
            else:out.append('\n' if c=='\n' else ' ');i+=1
            continue
        if mode in ('str','char'):
            end='"' if mode=='str' else "'"
            if esc:esc=False
            elif c=='\\':esc=True
            elif c==end:mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1;continue
        if mode=='vstr':
            if c=='"' and n=='"':out.extend('  ');i+=2;continue
            if c=='"':mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1;continue
    return ''.join(out)

csfiles=list((root/'Assets/Scripts').rglob('*.cs'))+list((root/'Assets/Editor').rglob('*.cs'))
check('deprecated FindObjectsSortMode removed project-wide', all('FindObjectsSortMode' not in f.read_text(errors='ignore') for f in csfiles))
for rel in csfiles:
    text=rel.read_text(errors='ignore');in_str=False;verb=False;esc=False;bad=[];line=1;i=0
    while i<len(text):
        c=text[i]
        if c=='\n':
            if in_str and not verb:bad.append(line)
            line+=1
        if not in_str:
            if c=='"':verb=(i>0 and text[i-1]=='@');in_str=True;esc=False
        elif verb:
            if c=='"':
                if i+1<len(text) and text[i+1]=='"':i+=1
                else:in_str=False;verb=False
        else:
            if esc:esc=False
            elif c=='\\':esc=True
            elif c=='"':in_str=False
        i+=1
    check('no raw newline: '+str(rel.relative_to(root)),not bad)
    clean=stripped(text)
    for op,cl in [('(',')'),('[',']'),('{','}')]:
        bal=0;ok=True
        for c in clean:
            if c==op:bal+=1
            elif c==cl:
                bal-=1
                if bal<0:ok=False;break
        check(f'balanced {op}{cl}: {rel.relative_to(root)}',ok and bal==0)

method_re=re.compile(r'(?P<head>(?:public|private|protected|internal)[^\n{;]*\bIEnumerator\s+\w+\s*\([^)]*\)\s*)\{',re.M)
for rel in csfiles:
    text=rel.read_text(errors='ignore');clean=stripped(text)
    for m in method_re.finditer(clean):
        start=m.end()-1;depth=0;end=None
        for i in range(start,len(clean)):
            if clean[i]=='{':depth+=1
            elif clean[i]=='}':
                depth-=1
                if depth==0:end=i+1;break
        body=text[start:end] if end else ''
        name=re.search(r'IEnumerator\s+(\w+)',m.group('head')).group(1)
        check(f'IEnumerator has yield: {rel.relative_to(root)}::{name}','yield return' in body or 'yield break' in body)


check('validator uses existing enemy activation debug helper', 'DebugAdvanceEnemyActivationStatusClock' in test825 and 'DebugCompleteEnemyActivation' not in test825)
check('enemy activation debug helper exists in game manager', 'public void DebugAdvanceEnemyActivationStatusClock' in gm)

failed=[x for x in checks if not x[1]]
for label,ok in checks:print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'\n{len(checks)-len(failed)}/{len(checks)} checks passed.')
if failed:sys.exit(1)
