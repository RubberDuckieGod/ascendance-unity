from pathlib import Path
import re,sys
root=Path(__file__).resolve().parents[1]
read=lambda p:(root/p).read_text()
gm=read('Assets/Scripts/Core/AscendanceGameManager.cs')
qr=read('Assets/Scripts/Core/QuestRuntime0829.cs')
models=read('Assets/Scripts/Core/AscendanceModels.cs')
explore=read('Assets/Scripts/Core/ExplorationRuntime0828.cs')
sr=read('Assets/Scripts/Core/SearchEquipmentRuntime0827.cs')
save=read('Assets/Scripts/Shell/AscendanceSaveData.cs')
saver=read('Assets/Scripts/Shell/AscendanceSaveSystem.cs')
hud=read('Assets/Scripts/UI/AscendanceHUD.cs')
world=read('Assets/Scripts/World/StonebrookWorldBuilder.cs')
test=read('Assets/Editor/QuestFrameworkTestTools0829.cs')
checks=[]
def ck(label,cond):checks.append((label,bool(cond)))
ck('version','public const string BuildVersion = "0.8.29";' in gm)
ck('banner','Generalized Quest Framework & Campaign I Quest Runtime loaded.' in gm)
ck('quest definitions','Q001' in qr and 'Q002' in qr and 'QuestContent0829' in qr)
ck('quest runtime initialized','InitializeQuestRuntime0829();' in gm)
ck('locked 4 supply','Q002StartingSupply0829 = 4' in qr and 'Supply=Q002StartingSupply0829' in qr)
ck('quest owned enemy fields','QuestSpawned' in models and 'QuestId' in models)
ck('quest spawn separate','QuestSpawned=true' in qr and 'EncounterSpawned=true' not in qr.split('CreateQuestEnemy0829',1)[1].split('}',1)[0])
ck('start activation hook',gm.find('ResolveQuestEnemyStartActivation0829(enemy);')>gm.find('private IEnumerator ResolveSingleEnemyActivation') and gm.find('ResolveQuestEnemyStartActivation0829(enemy);')<gm.find('MoveEnemyTowardNearestHero(enemy)'))
ck('unattended wagon logic','UNATTENDED WAGON' in qr and 'ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.LocationId==enemy.LocationId)' in qr)
ck('end phase quest hook','ResolveQuestEndEnemyPhase0829();' in gm)
ck('three reinforcement waves','ReinforcementCount==1' in qr and 'ReinforcementCount==2' in qr and 'ReinforcementCount>=3' in qr)
ck('farthest hero spawn','OrderByDescending' in qr and 'GraphDistance(h.LocationId,w.LocationId)' in qr)
ck('wagon movement modal','QuestWagonMove' in qr and 'MOVE|' in qr and 'hero.Actions--' in qr)
ck('wagon rescue','w.Rescued=true' in qr and 'DeliveredSupply+=w.Supply' in qr)
ck('q002 outcome','wagons.All(w=>w.Destroyed)' in qr and 'wagons.All(w=>w.Rescued)' in qr and 'DeliveredSupply>=4' in qr)
ck('search override','TryResolveQuestSearchOverride0829' in gm and 'LT123' in qr)
ck('captain key','OnQuestEnemyDefeated0829' in gm and 'LT119' in qr)
ck('strongbox source match','!d.QuestItem' in qr and 'Merchant' in qr and 'Caravan' in qr and 'StrongboxOpened' in qr)
ck('quest rewards','125' in qr and 'AddPartyGold(100,"Q002 victory reward")' in qr and 'NextQuestSupply0829+=1' in qr)
ck('q001 preservation','MaxHealth=18, Health=18, Armor=2, Attack=4' in gm and 'CurrentQuestId0829=="Q001"' in qr)
ck('q001 warning preserved','Q001 WARNING — Stonebrook has reached 5 Fire Tokens.' in qr)
ck('merchant graph','AddLocation("L005", "Merchant Caravan"' in gm)
ck('q001 excludes merchant', 'return locationId=="L003"||locationId=="L004";' in qr)
ck('q002 includes merchant','locationId=="L005"' in qr)
ck('exploration quest filter','IsLocationEligibleForCurrentQuest0829' in explore)
ck('reveal hook','OnQuestLocationRevealed0829(location,hero);' in explore)
ck('reward choice quest options','AppendQuestChoiceOptions0829(options,state)' in sr and 'TryResolveQuestChoice0829(state,hero,key' in sr)
ck('save quest runtime','QuestRuntimeSaveData0829' in save and 'QuestSpawnSerial0829' in save and 'NextQuestSupply0829' in save)
ck('save quest enemy ownership','QuestSpawned=e.QuestSpawned' in saver and 'QuestId=e.QuestId' in saver)
ck('save current quest id','QuestId=gm.CurrentQuestId0829' in saver)
ck('restore quest before exploration',gm.find('RestoreQuestRuntime0829(save);')<gm.find('RestoreExplorationDeckState0828(save);'))
ck('hud generalized summary','QuestObjectiveSummary0829' in hud)
ck('hud q002 actions','QuestPrimaryAction0829' in hud and 'QuestSecondaryAction0829' in hud)
ck('hud begin q002','BEGIN Q002' in hud and 'BeginNextQuest0829' in hud)
ck('world builder removes stale party avatars','activeHeroIds' in world and 'gm.HeroAvatars.Remove(heroId)' in world and 'Destroy(staleAvatar.gameObject)' in world)
ck('validator menu','Test 0.8.29 - Generalized Quest Framework & Q002 (Play Mode)' in test)
ck('validator stale hero avatar runtime check','Quest transition presentation removes stale non-participating Hero avatars.' in test)
for phrase in ['locked 4 Supply','fails when both Wagons','unattended Wagon loses exactly 1 Supply','Move Wagon opens','LT123 Caravan Manifest','reinforcement wave 3','LT119 Merchant Strongbox Key','Strongbox consumes LT119','Q002 completes only','Save data persists']:
    ck('validator '+phrase,phrase in test)
# compile-safety scans across all cs
cs=list((root/'Assets').rglob('*.cs'))
def strip(text):
    out=[];i=0;mode='code';esc=False
    while i<len(text):
        c=text[i];n=text[i+1] if i+1<len(text) else ''
        if mode=='code':
            if c=='/' and n=='/':mode='line';out+=[' ',' '];i+=2;continue
            if c=='/' and n=='*':mode='block';out+=[' ',' '];i+=2;continue
            if c=='@' and n=='"':mode='vstr';out+=[' ',' '];i+=2;continue
            if c=='"':mode='str';out.append(' ');i+=1;continue
            if c=="'":mode='char';out.append(' ');i+=1;continue
            out.append(c);i+=1;continue
        if mode=='line':out.append('\n' if c=='\n' else ' ');mode='code' if c=='\n' else mode;i+=1;continue
        if mode=='block':
            if c=='*' and n=='/':mode='code';out+=[' ',' '];i+=2;continue
            out.append('\n' if c=='\n' else ' ');i+=1;continue
        if mode in ('str','char'):
            end='"' if mode=='str' else "'"
            if esc:esc=False
            elif c=='\\':esc=True
            elif c==end:mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1;continue
        if mode=='vstr':
            if c=='"' and n=='"':out+=[' ',' '];i+=2;continue
            if c=='"':mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1
    return ''.join(out)
for p in cs:
    raw=p.read_text(errors='ignore'); clean=strip(raw)
    ck('delimiter '+p.name, all((lambda a,b: (lambda bal: bal==0)(sum(1 if ch==a else -1 if ch==b else 0 for ch in clean)))(a,b) for a,b in [('(',')'),('{','}'),('[',']')]))
    # ordinary C# string literals may not contain a physical newline (verbatim strings may).
    in_str=False;verb=False;esc=False;bad=False;i=0
    while i<len(raw):
        c=raw[i]
        if c=='\n' and in_str and not verb: bad=True;break
        if not in_str:
            if c=='"': verb=(i>0 and raw[i-1]=='@');in_str=True;esc=False
        elif verb:
            if c=='"':
                if i+1<len(raw) and raw[i+1]=='"': i+=1
                else: in_str=False;verb=False
        else:
            if esc: esc=False
            elif c=='\\': esc=True
            elif c=='"': in_str=False
        i+=1
    ck('raw string '+p.name,not bad)
# IEnumerator methods must contain yield in their body (rough)
for p in cs:
    raw=p.read_text(errors='ignore')
    for m in re.finditer(r'\bIEnumerator\s+\w+\s*\([^)]*\)\s*\{',raw):
        start=m.end();depth=1;i=start
        while i<len(raw) and depth:
            if raw[i]=='{':depth+=1
            elif raw[i]=='}':depth-=1
            i+=1
        ck('yield '+p.name+':'+m.group(0).split('(')[0], 'yield ' in raw[start:i])
failed=[x for x in checks if not x[1]]
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'RESULT: {len(checks)-len(failed)}/{len(checks)} checks passed')
sys.exit(1 if failed else 0)
