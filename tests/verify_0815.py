from pathlib import Path
import re, sys, json
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text()
models=(root/'Assets/Scripts/Core/AscendanceModels.cs').read_text()
save=(root/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text()
savesys=(root/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text()
test=(root/'Assets/Editor/StatusDurationLifecycleTestTools0815.cs').read_text()
hero_rules=(root/'Assets/Scripts/Core/HeroRulesRuntime.cs').read_text()
checks=[]
def check(label, cond): checks.append((label,bool(cond)))

check('BuildVersion 0.8.15', 'public const string BuildVersion = "0.8.15";' in gm)
check('0.8.15 load banner', 'Status Duration Lifecycle loaded.' in gm)
for name in ['Persistent','StartOfTargetTurn','EndOfTargetTurn','StartOfSourceTurn','EndOfSourceTurn','EndOfTargetActivation','EndOfRound']:
    check('StatusExpiryTiming has '+name, name in models)
check('StatusInstance stores explicit clock', 'public StatusExpiryTiming ExpiryTiming' in models and 'RemainingExpiryTriggers' in models and 'SourceHeroId' in models)
check('legacy status counters retained', 'public int RemainingTurns;' in models and 'public int RemainingRounds;' in models)
check('turn start advances target/source clocks', 'AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.StartOfTargetTurn' in gm and 'AdvanceSourceStatusClock(hero, StatusExpiryTiming.StartOfSourceTurn)' in gm)
check('turn end advances target/source clocks', 'AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfTargetTurn' in gm and 'AdvanceSourceStatusClock(hero, StatusExpiryTiming.EndOfSourceTurn)' in gm)
check('hero turn end advances activation clock', 'AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfTargetActivation' in gm)
check('enemy activation advances activation clock', 'CompleteEnemyActivation(enemy)' in gm and 'StatusExpiryTiming.EndOfTargetActivation' in gm)
check('Stunned/Frozen enemy activation is skipped', 'cannot act during this activation' in gm and '"Stunned"' in gm and '"Frozen"' in gm)
check('round clock replaces generic TickStatuses', 'TickStatuses(' not in gm and 'AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfRound' in gm and 'AdvanceStatusClock(enemy.Statuses, StatusExpiryTiming.EndOfRound' in gm)
check('persistent authored statuses recognized', 'normalized.Contains("until removed")' in gm and 'StatusExpiryTiming.Persistent' in gm)
check('next activation authored timing recognized', 'normalized.Contains("next activation")' in gm)
check('source next turn authored timing recognized', 'normalized.Contains("source\'s next turn")' in gm)
check('next turn start authored timing recognized', 'StatusExpiryTiming.StartOfSourceTurn' in gm and 'StatusExpiryTiming.StartOfTargetTurn' in gm)
check('card status application passes authored rules', 'AddStatus(target,name,1,0,card.Name,true,hero,text)' in gm and 'AddStatus(enemy,name,1,0,card.Name,false,hero,text)' in gm)
check('direct Marked cards pass authored rules', gm.count('AddStatus(selectedEnemy,"Marked",0,1,card.Name,false,hero,card.Rules)') >= 2)
check('Grand Totem Inspired passes authored rules', 'AddStatus(h,"Inspired",1,0,card.Name,true,hero,card.Rules)' in gm)
check('Improved Rousing Verse has explicit target-turn timing', 'Improved Rousing Verse", true, hero, "gains Inspired until the end of their next turn"' in hero_rules)
check('Improved Wild Growth has explicit target-turn timing', 'Improved Wild Growth", true, thorne, "gains Regenerating until the end of their next turn"' in hero_rules)
check('Regenerating recovery moved to Hero turn start', 'if(hero.HasStatus("Regenerating"))HealHero(hero,1,"Regenerating",hero);' in gm)
check('Regenerating no longer ticks generically at End Phase', 'if (hero.HasStatus("Regenerating")) HealHero' not in gm)
check('save data stores status clock metadata', 'StatusExpiryTiming ExpiryTiming' in save and 'RemainingExpiryTriggers' in save and 'SourceHeroId' in save)
check('save capture stores status clock metadata', 'ExpiryTiming=s.ExpiryTiming' in savesys and 'RemainingExpiryTriggers=s.RemainingExpiryTriggers' in savesys and 'SourceHeroId=s.SourceHeroId' in savesys)
check('load restores/migrates status clock metadata', 'RestoreStatusFromSave(s, false)' in gm and 'RestoreStatusFromSave(s, true)' in gm and 'SAVE' not in '')
check('runtime test target-turn expiry', "Target-turn status expires exactly when that Hero ends the turn." in test)
check('runtime test source-start expiry', "Source-start status expires when the source Hero's next turn starts." in test)
check('runtime test activation expiry', 'Activation-timed status expires at Enemy activation end.' in test)
check('runtime test round expiry', 'Round-timed status expires at End of Round.' in test)
check('runtime test persistent status', 'Persistent status survives activation and round clocks.' in test)
check('runtime test source-end fallback', 'Source-end status expires when the source Hero ends the relevant turn.' in test)

# Data sanity: the authored source really contains each duration family this build handles.
data=json.loads((root/'Assets/Resources/Ascendance/Data/status_effects.json').read_text())['statuses']
durations='\n'.join(x.get('duration','') for x in data).lower()
check('status data contains activation durations', 'next activation' in durations)
check('status data contains source-next-turn durations', "source's next turn" in durations)
check('status data contains round durations', 'end of round' in durations)
check('status data contains persistent durations', 'until removed' in durations)

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

# Strip comments/strings approximately and verify delimiter balance. This catches most packaging-time edit accidents.
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
sys.exit(1 if failed else 0)
