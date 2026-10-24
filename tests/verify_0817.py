from pathlib import Path
import re, sys, json
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(encoding='utf-8')
test=(root/'Assets/Editor/SpecializedStatusParityTestTools0817.cs').read_text(encoding='utf-8')
status=json.loads((root/'Assets/Resources/Ascendance/Data/status_effects.json').read_text(encoding='utf-8'))['statuses']
status_by={s['name']:s for s in status}
checks=[]
def check(label,cond): checks.append((label,bool(cond)))
check('BuildVersion 0.8.17','public const string BuildVersion = "0.8.17";' in gm)
check('0.8.17 load banner','Live Specialized Condition Parity loaded.' in gm)
for name in ['Hidden','Knocked Down','Shocked','Taunted']:
    check(f'card status list includes {name}', name in gm)
check('Burning authored end activation',status_by['Burning']['effect']=='Suffer 1 damage at the end of your activation.')
check('Bleeding authored attack trigger','each time you perform an Attack' in status_by['Bleeding']['effect'])
check('Knocked Down authored stand rule','first Action on your turn must be spent to Stand' in status_by['Knocked Down']['effect'])
check('Shocked authored bonus-action prevention','next time you would gain an additional Action' in status_by['Shocked']['effect'])
check('Taunted authored target rule','next Attack must target the unit that applied Taunted' in status_by['Taunted']['effect'])
check('Hidden authored attack break','Until you Attack' in status_by['Hidden']['duration'])
check('Hero Knocked Down blocks movement','!hero.HasStatus("Rooted") && !hero.HasStatus("Knocked Down")' in gm)
check('Hero Knocked Down auto-stands','spends 1 Action to Stand and removes Knocked Down' in gm)
check('Enemy Knocked Down helper','ResolveEnemyKnockedDownActivation' in gm and 'spends this activation standing and does not Attack' in gm)
check('Taunted target helper','ChooseEnemyAttackTarget' in gm and 'is Taunted and must target' in gm)
check('Shocked additional action hook','TryGrantAdditionalHeroAction' in gm and 'Shocked prevents' in gm)
check('Hidden breaks on attack','BreakHiddenOnAttack' in gm and 'attacks and loses Hidden' in gm)
check('Persistent Burning end-activation hook','ResolveEnemyEndActivationConditions' in gm and 'DealDirectDamage(enemy,1,"Burning")' in gm)
check('Bleeding attack-only hook','performedAttack&&enemy.Statuses.Any' in gm and 'DealDirectDamage(enemy,1,"Bleeding")' in gm)
check('Meteor Shard one-shot start hook','Burning (Meteor Shard)' in gm and 'Meteor Shard one-shot Burning resolved' in gm)
check('Meteor conditional application is not falsely granted without Arcane location runtime','card.Id=="H004-006"' in gm and 'do not falsely' in gm)
check('No old end-round Burning tick','if (enemy.Statuses.Any(s => s.Name == "Burning")) DealDirectDamage(enemy, 1, "Burning")' not in gm)
check('Nature Blessing cleanse','case "H007-012"' in gm and 'removes Bleeding or Burning if present' in gm)
check('Play Mode menu','Test 0.8.17 - Live Specialized Conditions (Play Mode)' in test)
for needle in [
    'Camouflage applies Hidden','Taunted forces the Enemy','Knocked Down consumes the Enemy','Shocked prevents the next additional Action gain',
    'Persistent Burning deals exactly 1 damage','Meteor Shard Burning deals exactly 1 damage','Bleeding deals exactly 1 damage after the Enemy performs an Attack',
    'Live Specialized Conditions test completed successfully'
]: check('test: '+needle,needle in test)

# Conservative lexer-like scan for illegal raw newline in ordinary quoted strings.
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
                verb=(i>0 and text[i-1]=='@'); in_str=True; esc=False
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
for rel in root.rglob('*.cs'):
    t=stripped(rel.read_text(errors='ignore'))
    for op,cl in [('(',')'),('[',']'),('{','}')]:
        bal=0;ok=True
        for c in t:
            if c==op: bal+=1
            elif c==cl:
                bal-=1
                if bal<0:ok=False;break
        check(f'balanced {op}{cl}: {rel.relative_to(root)}',ok and bal==0)

failed=[x for x in checks if not x[1]]
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'\n{len(checks)-len(failed)}/{len(checks)} checks passed.')
if failed: sys.exit(1)
