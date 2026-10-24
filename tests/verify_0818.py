from pathlib import Path
import re, sys
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(encoding='utf-8')
models=(root/'Assets/Scripts/Core/AscendanceModels.cs').read_text(encoding='utf-8')
save=(root/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text(encoding='utf-8')
saver=(root/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text(encoding='utf-8')
test=(root/'Assets/Editor/LocationStateTestTools0818.cs').read_text(encoding='utf-8')
checks=[]
def check(label,cond): checks.append((label,bool(cond)))
check('BuildVersion 0.8.18','public const string BuildVersion = "0.8.18";' in gm)
check('0.8.18 load banner','Location Tags & Location-State Enforcement loaded.' in gm)
for field in ['AuthoredLocationId','Terrain','Traits','Keywords','RevealEffect','FirstVisitEffect','OngoingEffect','RevealResolved','FirstVisitResolved','Searched','TemporaryTags']:
    check('LocationState '+field, field in models)
check('LocationTagInstance model','class LocationTagInstance' in models and 'ExpiryTiming' in models)
check('printed tag query','HasPrintedTag' in models and 'LocationHasTag' in gm)
check('authored metadata bind by name','ApplyAuthoredLocationMetadata' in gm and 'case "Dense Woods"' in gm and 'AuthoredLocationId="L002"' in gm)
check('River Crossing authored L004','case "River Crossing"' in gm and 'AuthoredLocationId="L004"' in gm)
check('Difficult Terrain runtime query','IsDifficultTerrain' in gm)
check('Difficult Terrain requires 2 move actions','return 2;' in gm and 'Difficult Terrain exit' in gm)
check('free Move contributes to Difficult Terrain','int free=hero.FreeMoveReady?1:0' in gm and 'Mathf.Max(0,required-free)' in gm)
check('DT ignore persisted runtime field','FreeMoveIgnoresDifficultTerrain' in models and 'FreeMoveIgnoresDifficultTerrain' in save and 'FreeMoveIgnoresDifficultTerrain=h.FreeMoveIgnoresDifficultTerrain' in saver)
check('Pathfinder/equipment DT ignore','DifficultTerrain:Pathfinder' in gm and 'ignore the additional Move Action required to leave Difficult Terrain' in gm)
check('Reveal resolver','ResolveLocationReveal' in gm and 'REVEAL EFFECT' in gm)
check('Explore resolves Reveal','ResolveLocationReveal(hidden,hero);' in gm)
check('First Visit resolver','ResolveLocationFirstVisit' in gm and 'FIRST VISIT PENDING' in gm)
check('Move resolves First Visit','ResolveLocationFirstVisit(Locations[locationId],hero);' in gm)
check('card Move resolves First Visit','ResolveLocationFirstVisit(Locations[destination],hero);' in gm)
check('Arcane temporary tags','AddLocationTag(hero.LocationId,"Arcane"' in gm and 'IsArcaneLocation' in gm)
check('Arcane Bolt uses actual location state','card.Id=="H004-001"&&IsArcaneLocation(hero.LocationId)' in gm)
check('Meteor Shard uses actual Arcane prerequisite','card.Id=="H004-006"&&string.Equals(name,"Burning"' in gm and '!IsArcaneLocation(hero.LocationId)' in gm)
check('Consecrated location tag','AddLocationTag(hero.LocationId,"Consecrated"' in gm)
check('Consecrated recovery bonus','ConsecratedHeal:' in gm and 'first recovery here this Round' in gm)
check('location tags saved','LocationTagSaveData' in save and 'TemporaryTags' in save and 'TemporaryTags.Add' in saver)
check('location event flags saved','RevealResolved' in save and 'FirstVisitResolved' in save and 'Searched' in save)
check('Play Mode menu','Test 0.8.18 - Location Tags & State (Play Mode)' in test)
for needle in [
    'Legacy L004 keeps its graph ID','only 1 general Action cannot voluntarily leave Difficult Terrain',
    'Leaving Difficult Terrain requires 2 Move Actions','Entering Difficult Terrain still requires only the normal 1 Move Action',
    'Explore reveals Ancient Ruins','Reveal does not consume the separate First Visit event',
    'First Visit remains pending when the entering Hero finds an Enemy present','Temporary Arcane is a real queryable Location tag',
    'Meteor Shard now applies its Burning prerequisite from actual Arcane Location state',
    'Consecrated adds +1 Health','Location Tags & Location-State Enforcement test completed successfully'
]: check('test: '+needle,needle in test)

# Raw-newline string scan
for rel in list((root/'Assets/Scripts').rglob('*.cs'))+list((root/'Assets/Editor').rglob('*.cs')):
    text=rel.read_text(errors='ignore');in_str=False;esc=False;verb=False;line=1;bad=[];i=0
    while i<len(text):
        c=text[i]
        if c=='\n':
            if in_str and not verb: bad.append(line)
            line+=1
        if not in_str:
            if c=='"': verb=(i>0 and text[i-1]=='@');in_str=True;esc=False
        else:
            if verb:
                if c=='"':
                    if i+1<len(text) and text[i+1]=='"': i+=1
                    else: in_str=False;verb=False
            else:
                if esc: esc=False
                elif c=='\\': esc=True
                elif c=='"': in_str=False
        i+=1
    check(f'no raw newline in string: {rel.relative_to(root)}',not bad)

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
            if c=='\n': mode='code';out.append('\n')
            else: out.append(' ')
            i+=1;continue
        if mode=='block':
            if c=='*' and n=='/': mode='code';out.extend('  ');i+=2
            else: out.append('\n' if c=='\n' else ' ');i+=1
            continue
        if mode in ('str','char'):
            end='"' if mode=='str' else "'"
            if esc: esc=False
            elif c=='\\': esc=True
            elif c==end: mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1;continue
        if mode=='vstr':
            if c=='"' and n=='"': out.extend('  ');i+=2;continue
            if c=='"': mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1;continue
    return ''.join(out)

csfiles=list((root/'Assets/Scripts').rglob('*.cs'))+list((root/'Assets/Editor').rglob('*.cs'))
for rel in csfiles:
    t=stripped(rel.read_text(errors='ignore'))
    for op,cl in [('(',')'),('[',']'),('{','}')]:
        bal=0;ok=True
        for c in t:
            if c==op: bal+=1
            elif c==cl:
                bal-=1
                if bal<0:ok=False;break
        check(f'balanced {op}{cl}: {rel.relative_to(root)}',ok and bal==0)

# IEnumerator safety audit: every IEnumerator method body must contain at least one yield.
method_re=re.compile(r'(?P<head>(?:public|private|protected|internal)[^\n{;]*\bIEnumerator\s+\w+\s*\([^)]*\)\s*)\{',re.M)
for rel in csfiles:
    text=rel.read_text(errors='ignore')
    clean=stripped(text)
    for m in method_re.finditer(clean):
        start=m.end()-1;depth=0;end=None
        for i in range(start,len(clean)):
            if clean[i]=='{': depth+=1
            elif clean[i]=='}':
                depth-=1
                if depth==0: end=i+1;break
        body=text[start:end] if end else ''
        name=re.search(r'IEnumerator\s+(\w+)',m.group('head')).group(1)
        check(f'IEnumerator has yield: {rel.relative_to(root)}::{name}','yield return' in body or 'yield break' in body)

failed=[x for x in checks if not x[1]]
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'\n{len(checks)-len(failed)}/{len(checks)} checks passed.')
if failed: sys.exit(1)
