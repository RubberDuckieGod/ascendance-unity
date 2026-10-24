from pathlib import Path
import re,sys
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text()
rt=(root/'Assets/Scripts/Core/ExplorationRuntime0828.cs').read_text()
sr=(root/'Assets/Scripts/Core/SearchEquipmentRuntime0827.cs').read_text()
models=(root/'Assets/Scripts/Core/AscendanceModels.cs').read_text()
save=(root/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text()
saver=(root/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text()
hud=(root/'Assets/Scripts/UI/AscendanceHUD.cs').read_text()
test=(root/'Assets/Editor/ExplorationDeckSearchTestTools0828.cs').read_text()
content=(root/'Assets/Scripts/Core/SearchEncounterRuntime.cs').read_text()
roster=(root/'Assets/Resources/Ascendance/Data/hero_roster.json').read_text()
checks=[]
def ck(label,cond): checks.append((label,bool(cond)))
ck('BuildVersion 0.8.28','public const string BuildVersion = "0.8.28";' in gm)
ck('0.8.28 banner','Exploration Deck Preview, Reorder & Search Integration loaded.' in gm)
ck('exploration partial exists','ExplorationDeck0828' in rt and 'DrawLegalExplorationLocation0828' in rt)
ck('initializes exploration deck','InitializeExplorationDeck0828();' in gm)
ck('deck uses hidden graph locations','Locations.Values.Where(l=>!l.Revealed)' in rt)
ck('Explore uses legal deck draw','DrawLegalExplorationLocation0828(current,"Explore")' in gm)
ck('Explore no longer FirstOrDefault hidden direct','var current=Locations[hero.LocationId]; var hidden=current.Connections' not in gm)
ck('illegal cards set aside','var setAside=new List<string>()' in rt and 'illegal draw(s) set aside' in rt)
ck('illegal cards returned shuffled','ExplorationDeck0828.AddRange(setAside' in rt and 'Shuffle(ExplorationDeck0828)' in rt)
ck('SD005 real implementation','case "SD005"' in rt and 'RevealExplorationLocation0828(revealed,hero,false' in rt)
ck('SD005 deferred reveal field','RevealEffectDeferred' in models and 'REVEAL DEFERRED' in rt)
ck('deferred reveal resolves on entry','ResolveDeferredRevealEffectOnEntry0828' in gm and 'REVEAL DEFERRED RESOLVES' in rt)
ck('SD006 real implementation','case "SD006"' in rt and 'ExplorationInspectConnected' in rt)
ck('SD006 leave option','Key="LEAVE"' in rt and 'remains unrevealed' in rt)
ck('SD006 reveal option','Key="REVEAL"' in rt and 'RevealExplorationLocation0828(l,hero,true' in rt)
ck('SD006 scalable choose inspect','ExplorationChooseInspect' in rt and 'Choose one connected unrevealed Location to inspect' in rt)
ck('SD010 real implementation','case "SD010"' in rt and 'ExplorationTopTwoOrder' in rt)
ck('SD010 both orders','ORDER01' in rt and 'ORDER10' in rt)
ck('SD010 edits live deck','ExplorationDeck0828.InsertRange(0,order)' in rt)
ck('Rowan authored ability text', 'Improved Trailblazer — Once during your turn, after you Explore or Reveal a Location' in roster)
ck('Rowan real Trailblazer','TryBeginRowanTrailblazer0828' in rt and 'ExplorationTrailblazer' in rt)
ck('Rowan top and bottom choices','TOP0' in rt and 'TOP1' in rt and 'places' in rt and 'bottom' in rt)
ck('Scout Ahead uses exploration deck','DrawLegalExplorationLocation0828(Locations[hero.LocationId],"Scout Ahead")' in gm)
ck('Search 0827 delegates exploration','ResolveSearchExplorationReward0828(card,location,hero)' in sr)
ck('reward UI options delegates exploration','Append0828ExplorationChoiceOptions(options,state)' in sr)
ck('reward resolution delegates exploration','TryResolve0828ExplorationChoice(state,hero,key' in sr)
ck('exploration deck save field','ExplorationDeckIds = new()' in save)
ck('exploration deck captured','ExplorationDeckIds=gm.ExplorationDeck0828.ToList()' in saver)
ck('exploration deck restored after locations','RestoreExplorationDeckState0828(save);' in gm and gm.find('RestoreExplorationDeckState0828(save);')>gm.find('if (save.Locations != null)'))
ck('deferred reveal save field','RevealEffectDeferred' in save)
ck('deferred reveal captured','RevealEffectDeferred=l.RevealEffectDeferred' in saver)
ck('deferred reveal restored compatibly','stored.RevealEffectDeferred ? stored.RevealResolved' in gm)
ck('choice state already persists IDs','RevealedCardIds=gm.PendingRewardChoice.RevealedCardIds.ToList()' in saver)
ck('HUD exploration modal title','StartsWith("Exploration")' in hud and '"EXPLORATION\\n"' in hud)
ck('HUD deck remaining','EXPLORATION DECK:' in hud)
for cid,text in [('SD005','Reveal 1 unrevealed Location connected'),('SD006','Look at one unrevealed connected Location'),('SD010','Look at the top 2 Exploration cards')]:
    ck('authored '+cid,cid in content and text in content)
ck('Play Mode menu','Test 0.8.28 - Exploration Deck Preview Reorder & Search (Play Mode)' in test)
for phrase in ['Exploration deck can hold the real hidden graph Location cards','SD010 exposes the actual top two Exploration cards','SD010 reorders the live Exploration deck','SD005 skips the illegal top card','Entering an SD005-revealed Location resolves','SD006 inspects a connected unrevealed Location','SD006 Leave keeps the Location unrevealed','SD006 Reveal resolves the Location','Rowan Improved Trailblazer now opens','Improved Trailblazer keeps the chosen card','Exploration Deck Preview, Reorder & Search Integration completed']:
    ck('validator '+phrase,phrase in test)
# compile-safety scans
def strip_code(text):
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
            out.append('\n' if c=='\n' else ' ');mode='code' if c=='\n' else mode;i+=1;continue
        if mode=='block':
            if c=='*' and n=='/':mode='code';out.extend('  ');i+=2;continue
            out.append('\n' if c=='\n' else ' ');i+=1;continue
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
ck('deprecated FindObjectsSortMode absent',all('FindObjectsSortMode' not in f.read_text(errors='ignore') for f in csfiles))
for f in csfiles:
    text=f.read_text(errors='ignore');clean=strip_code(text)
    for op,cl in [('(',')'),('[',']'),('{','}')]:
        bal=0;ok=True
        for c in clean:
            if c==op:bal+=1
            elif c==cl:
                bal-=1
                if bal<0:ok=False;break
        ck(f'delimiter {op}{cl} '+str(f.relative_to(root)),ok and bal==0)
    # raw newline normal C# string scanner
    in_str=False;verb=False;esc=False;bad=False;i=0
    while i<len(text):
        c=text[i]
        if c=='\n' and in_str and not verb: bad=True;break
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
    ck('no raw newline '+str(f.relative_to(root)),not bad)
# IEnumerator methods must contain yield (simple targeted audit of Editor tests and runtime coroutine files)
for f in csfiles:
    text=f.read_text(errors='ignore')
    for m in re.finditer(r'\bIEnumerator\s+\w+\s*\([^)]*\)\s*\{',text):
        start=m.end();depth=1;i=start
        while i<len(text) and depth:
            if text[i]=='{':depth+=1
            elif text[i]=='}':depth-=1
            i+=1
        body=text[start:i-1]
        ck('IEnumerator yield '+str(f.relative_to(root))+':'+m.group(0).split('(')[0].strip(),'yield ' in body)
passed=sum(v for _,v in checks);failed=[k for k,v in checks if not v]
print(f'{passed}/{len(checks)} checks passed.')
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
if failed: sys.exit(1)
