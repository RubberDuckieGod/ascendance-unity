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
test=(root/'Assets/Editor/EncounterLifecycleTestTools0821.cs').read_text(encoding='utf-8')
checks=[]
def check(label,cond):checks.append((label,bool(cond)))
check('deprecated FindObjectOfType removed', 'FindObjectOfType<' not in gm)
check('BuildVersion 0.8.21','public const string BuildVersion = "0.8.21";' in gm)
check('0.8.21 load banner','Encounter Lifecycle & Reward Resolution loaded.' in gm)
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
check('Play Mode menu','Test 0.8.21 - Encounter Lifecycle & Rewards (Play Mode)' in test)
for phrase in ['explicit active Encounter instance','victory Gold is held','unrelated Q001 Enemy','awards 20 Gold','E006 + 2 E001','Partial Encounter clearance','15 Gold + 5 Gold','E012 Goblin Trapper','conditional 15 Gold','Encounter Lifecycle & Reward Resolution completed']:
    check('test '+phrase,phrase in test)

# 0.8.21 lifecycle/reward contract.
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

failed=[x for x in checks if not x[1]]
for label,ok in checks:print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'\n{len(checks)-len(failed)}/{len(checks)} checks passed.')
if failed:sys.exit(1)
