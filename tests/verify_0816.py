from pathlib import Path
import re, sys
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(encoding='utf-8')
test=(root/'Assets/Editor/StatusEffectEnforcementTestTools0816.cs').read_text(encoding='utf-8')
checks=[]
def check(label,cond): checks.append((label,bool(cond)))
check('BuildVersion 0.8.16','public const string BuildVersion = "0.8.16";' in gm)
check('0.8.16 load banner','Core Status Effect Enforcement loaded.' in gm)
check('Stunned blocks turn Actions','!hero.HasStatus("Stunned") && IsHeroTakingTurn(hero)' in gm)
check('Stunned blocks Reaction action costs','!hero.HasStatus("Stunned") && GeneralActions(hero) >= cost' in gm)
check('Stunned consumes Hero activation','is Stunned and immediately ends this activation with 0 Actions' in gm)
check('Rooted movement helper','CanHeroVoluntarilyMove' in gm and '!hero.HasStatus("Rooted")' in gm)
check('Rooted card movement block','is Rooted and cannot voluntarily Move with' in gm)
check('Slowed action grant','hero.HasStatus("Slowed") ? Mathf.Max(1, 3 - 1) : 3' in gm)
check('Marked source bonus helper','MarkedAttackBonus' in gm and 'SourceHeroId' in gm)
check('Predator Mark all-attacker exception','Predator\'s Mark' in gm)
check('Exposed one-hit consume','Exposed grants +1 Attack' in gm and 'RemoveAll' in gm)
check('Inspired/Weakened effective Hero Attack','if(hero.HasStatus("Inspired"))value+=1;if(hero.HasStatus("Weakened"))value-=1' in gm)
check('Guarded/Vulnerable effective Hero Armor','if(hero.HasStatus("Guarded"))value+=1;if(hero.HasStatus("Vulnerable"))value-=1' in gm)
check('Enemy Weakened','s.Name=="Weakened"' in gm)
check('Enemy Vulnerable','s.Name=="Vulnerable"' in gm)
check('Regenerating start heal','hero.HasStatus("Regenerating")' in gm and 'HealHero(hero,1,"Regenerating",hero)' in gm)
check('0.8.16 Play Mode test menu','Test 0.8.16 - Core Status Effect Enforcement (Play Mode)' in test)
for needle in ['Rooted blocks voluntary Hero movement','Stunned immediately ends the Hero activation','Slowed reduces the normal 3-Action grant','Inspired grants +1 Hero Attack','Guarded grants +1 Hero Armor','Marked grants +1 Attack','Exposed removes itself','Stealth has no inherent Attack/Armor modifier','Regenerating recovers exactly 1 Health']:
    check('test: '+needle,needle in test)
# Conservative lexer-like scan for accidental raw newline in ordinary quoted literals.
for rel in root.rglob('*.cs'):
    text=rel.read_text(errors='ignore')
    in_str=False; esc=False; verb=False; line=1; bad=[]; i=0
    while i<len(text):
        c=text[i]
        if c=='\n':
            if in_str and not verb: bad.append(line)
            line+=1
        if not in_str:
            if c=='"':
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

def stripped(text):
    out=[]; i=0; mode='code'; esc=False
    while i<len(text):
        c=text[i]; n=text[i+1] if i+1<len(text) else ''
        if mode=='code':
            if c=='/' and n=='/': mode='line'; out.extend('  '); i+=2; continue
            if c=='/' and n=='*': mode='block'; out.extend('  '); i+=2; continue
            if c=='@' and n=='"': mode='vstr'; out.extend('  '); i+=2; continue
            if c=='"': mode='str'; out.append(' '); i+=1; continue
            if c=="'": mode='char'; out.append(' '); i+=1; continue
            out.append(c); i+=1; continue
        if mode=='line':
            if c=='\n': mode='code'; out.append('\n')
            else: out.append(' ')
            i+=1; continue
        if mode=='block':
            if c=='*' and n=='/': mode='code'; out.extend('  '); i+=2
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
        if mode=='str':
            if esc: esc=False
            elif c=='\\': esc=True
            elif c=='"': mode='code'
            out.append('\n' if c=='\n' else ' '); i+=1; continue
        if mode=='char':
            if esc: esc=False
            elif c=='\\': esc=True
            elif c=="'": mode='code'
            out.append('\n' if c=='\n' else ' '); i+=1; continue
        if mode=='vstr':
            if c=='"' and n=='"': out.extend('  '); i+=2; continue
            if c=='"': mode='code'
            out.append('\n' if c=='\n' else ' '); i+=1; continue
    return ''.join(out)
for rel in root.rglob('*.cs'):
    t=stripped(rel.read_text(errors='ignore'))
    for op,cl in [('(',')'),('[',']'),('{','}')]:
        bal=0; ok=True
        for c in t:
            if c==op: bal+=1
            elif c==cl:
                bal-=1
                if bal<0: ok=False; break
        check(f'balanced {op}{cl}: {rel.relative_to(root)}', ok and bal==0)

failed=[x for x in checks if not x[1]]
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'\n{len(checks)-len(failed)}/{len(checks)} checks passed.')
if failed: sys.exit(1)
