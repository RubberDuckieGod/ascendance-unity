from pathlib import Path
import re,sys
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(encoding='utf-8')
models=(root/'Assets/Scripts/Core/AscendanceModels.cs').read_text(encoding='utf-8')
save=(root/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text(encoding='utf-8')
saver=(root/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text(encoding='utf-8')
hud=(root/'Assets/Scripts/UI/AscendanceHUD.cs').read_text(encoding='utf-8')
test=(root/'Assets/Editor/LocationEffectExecutionTestTools0819.cs').read_text(encoding='utf-8')
checks=[]
def check(label, cond): checks.append((label,bool(cond)))
check('BuildVersion 0.8.19','public const string BuildVersion = "0.8.19";' in gm)
check('0.8.19 load banner','Stonebrook Location Effect Execution loaded.' in gm)
for f in ['FreeSearchLocationId','PendingLocationOption','PendingLocationOptionLocationId']:
    check('HeroState '+f,f in models and f in save and f in saver)
for f in ['SearchRequests','TravelEncounterRequests']:
    check('LocationState '+f,f in models and f in save and f in saver)
check('Search runtime hook','event Action<LocationState,HeroState> SearchRequested' in gm)
check('Travel runtime hook','event Action<LocationState,HeroState,int> TravelEncounterRequested' in gm)
check('Ancient Ruins grants free Search','revealingHero.FreeSearchLocationId=location.Id' in gm and 'Forgotten Remains' in gm)
check('free Reveal Search targets revealed Location','SearchTargetFor' in gm and 'hero.FreeSearchLocationId' in gm)
check('Search once per Location','location.Searched=true' in gm and 'current.Revealed&&!current.Searched' in gm)
check('normal Search costs Action','if(!free)hero.Actions--' in gm)
check('Search hook invoked','SearchRequested?.Invoke(location,hero)' in gm)
check('Dense Woods optional Hide','PendingLocationOption="Hide"' in gm and 'Concealing Brush' in gm)
check('Hide applies Hidden','AddStatus(hero,"Hidden"' in gm)
check('River Fresh Water option','PendingLocationOption="Fresh Water"' in gm and 'remove Poisoned' in gm)
check('Fresh Water heal','HealHero(hero,1,"Fresh Water",hero)' in gm)
check('First Visit travel hook','TravelEncounterRequested?.Invoke(location,hero,level)' in gm and 'TravelEncounterRequests++' in gm)
check('First Visit enemy deferral preserved','FIRST VISIT PENDING' in gm)
check('Dense Woods ranged -1','Limited Visibility' in gm and 'modifier-=1' in gm)
check('High Ground generic +1','High Ground' in gm and 'modifier+=1' in gm)
check('Open Sightlines generic +1','Open Sightlines' in gm and 'modifier+=1' in gm)
check('Cover query','LocationProvidesCoverAgainstRanged' in gm and 'no Cover' in gm)
check('Goblin Home Territory armor hook','Goblins here gain +1 Armor' in gm and 'LocationEnemyArmorModifier' in gm)
check('Enemy armor consumes location hook','LocationEnemyArmorModifier(enemy)' in gm)
check('Hero attack consumes location modifier','LocationAttackModifier(hero.LocationId,enemy.LocationId' in gm)
check('HUD Search button','searchButton=MakeButton' in hud and 'SearchCurrentLocation()' in hud)
check('HUD Location Option button','locationButton=MakeButton' in hud and 'UseLocationRevealOption()' in hud)
check('Play Mode menu','Test 0.8.19 - Stonebrook Location Effects (Play Mode)' in test)
for n in ['optional Hide permission','Fresh Water can recover exactly 1 Health','Fresh Water can remove Poisoned','free one-time Search','Search deck hook','Travel Encounter','Limited Visibility applies -1','Cover vs ranged','Stonebrook Location Effect Execution completed']:
    check('test: '+n,n in test)

# Compile-safety scanners.
def stripped(text):
    out=[];i=0;mode='code';esc=False
    while i<len(text):
        c=text[i];n=text[i+1] if i+1<len(text) else ''
        if mode=='code':
            if c=='/' and n=='/': mode='line';out.extend('  ');i+=2;continue
            if c=='/' and n=='*': mode='block';out.extend('  ');i+=2;continue
            if c=='@' and n=='"': mode='vstr';out.extend('  ');i+=2;continue
            if c=='"': mode='str';out.append(' ');i+=1;continue
            if c=="'": mode='char';out.append(' ');i+=1;continue
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
    text=rel.read_text(errors='ignore'); in_str=False;verb=False;esc=False;bad=[];line=1;i=0
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
