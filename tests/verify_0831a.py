from pathlib import Path
import re, sys
root=Path(__file__).resolve().parents[1]
search=(root/'Assets/Scripts/Core/SearchEncounterRuntime.cs').read_text()
q31=(root/'Assets/Scripts/Core/QuestRuntime0831.cs').read_text()
test=(root/'Assets/Editor/QuestBatchQ006Q008TestTools0831.cs').read_text()
checks=[]
def ck(label, ok): checks.append((label,bool(ok)))
ck('E015 exact runtime template','AddEnemy("E015","Goblin Champion",14,4,2);' in search)
ck('Q008 final defender uses E015','SpawnQuestEnemy0829("E015",loc,"final Siege Engine destroyed")' in q31)
ck('WE003 archer exact +1 assertion','gm.DebugQuestEnemyAttack0831(archer)==gm.Enemies[archer].Attack+1' in test)
ck('validator Champion null guard','!string.IsNullOrEmpty(champ)&&gm.Enemies.TryGetValue(champ,out var champEnemy)' in test)
ck('validator no direct nullable dictionary index','gm.Enemies[champ].LocationId' not in test)
# compile-safety focused scans on changed C# files
for rel in ['Assets/Scripts/Core/SearchEncounterRuntime.cs','Assets/Editor/QuestBatchQ006Q008TestTools0831.cs']:
    raw=(root/rel).read_text()
    # raw multiline string scan
    in_str=False; verb=False; esc=False; bad=False; i=0
    while i<len(raw):
        c=raw[i]
        if c=='\n' and in_str and not verb: bad=True; break
        if not in_str:
            if c=='"': verb=i>0 and raw[i-1]=='@'; in_str=True; esc=False
        elif verb:
            if c=='"':
                if i+1<len(raw) and raw[i+1]=='"': i+=1
                else: in_str=False; verb=False
        else:
            if esc: esc=False
            elif c=='\\': esc=True
            elif c=='"': in_str=False
        i+=1
    ck('raw string '+rel,not bad)
# ensure exact Champion stats agree in runtime surface
m=re.search(r'AddEnemy\("E015","Goblin Champion",(\d+),(\d+),(\d+)\);',search)
ck('E015 parsed HP/Attack/Armor',bool(m and m.groups()==('14','4','2')))
failed=[x for x in checks if not x[1]]
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'RESULT: {len(checks)-len(failed)}/{len(checks)} checks passed')
sys.exit(1 if failed else 0)
