from pathlib import Path
import re, sys
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text()
hud=(root/'Assets/Scripts/UI/AscendanceHUD.cs').read_text()
test=(root/'Assets/Editor/PostQuestFreeExplorationTestTools0814.cs').read_text()
checks=[]
def check(label, cond):
    checks.append((label,bool(cond)))

def body(signature, next_sig=None):
    start=gm.find(signature)
    if start<0:return ''
    if next_sig:
        end=gm.find(next_sig,start+len(signature))
        return gm[start:end if end>=0 else None]
    return gm[start:start+3000]

check('BuildVersion 0.8.14', 'public const string BuildVersion = "0.8.14";' in gm)
check('0.8.14 load banner', 'Post-Quest Free Exploration Contract loaded.' in gm)
check('victory logs free exploration', 'POST-QUEST — free exploration continues.' in gm)
check('HUD no longer says VICTORY LOCKED', 'VICTORY LOCKED' not in hud)
check('HUD says free exploration active', 'QUEST COMPLETE — FREE EXPLORATION ACTIVE' in hud)
check('post-quest defeat does not rewrite victory', 'POST-QUEST DEFEAT' in gm and 'The completed Quest remains recorded.' in gm)

start_body=body('public bool CanStartHeroTurn', 'public bool CanUseTurnActions')
use_body=body('public bool CanUseTurnActions', 'public int GeneralActions')
explore_body=body('public void Explore()', 'public void RescueVillager')
check('CanStartHeroTurn is not gated by QuestComplete/QuestFailed', 'QuestComplete' not in start_body and 'QuestFailed' not in start_body)
check('CanUseTurnActions is not gated by QuestComplete/QuestFailed', 'QuestComplete' not in use_body and 'QuestFailed' not in use_body)
check('Explore is not gated by QuestComplete/QuestFailed', 'QuestComplete' not in explore_body and 'QuestFailed' not in explore_body)
check('runtime test completes quest', 'Q001 records completion normally.' in test)
check('runtime test explores after completion', 'Explore remains legal after Q001 completion.' in test)
check('runtime test starts another turn after completion', 'Another Hero can Start Turn normally after Q001 completion.' in test)
check('runtime test proves post-quest death', 'A Hero can still become Dead during post-Quest exploration.' in test)
check('runtime test preserves completed result after death', 'does not erase or retroactively fail an already completed Q001' in test)

# Conservative lexer-like scan for accidental raw newline in ordinary quoted literals.
for rel in [p for p in root.rglob('*.cs')]:
    text=rel.read_text(errors='ignore')
    in_str=False; esc=False; verb=False; line=1; bad=[]; i=0
    while i<len(text):
        c=text[i]
        if c=='\n':
            if in_str and not verb: bad.append(line)
            line+=1
        if not in_str:
            if c=='"':
                # detect C# verbatim @"..."
                verb=(i>0 and text[i-1]=='@')
                in_str=True; esc=False
        else:
            if verb:
                if c=='"':
                    if i+1<len(text) and text[i+1]=='"': i+=1
                    else: in_str=False; verb=False
            else:
                if esc: esc=False
                elif c=='\\': esc=True
                elif c=='"': in_str=False
        i+=1
    check(f'no raw newline in string: {rel.relative_to(root)}', not bad)

failed=[x for x in checks if not x[1]]
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'\n{len(checks)-len(failed)}/{len(checks)} checks passed.')
sys.exit(1 if failed else 0)
