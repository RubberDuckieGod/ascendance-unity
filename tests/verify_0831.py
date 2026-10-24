from pathlib import Path
import re,sys
root=Path(__file__).resolve().parents[1]
read=lambda p:(root/p).read_text()
gm=read('Assets/Scripts/Core/AscendanceGameManager.cs')
qr=read('Assets/Scripts/Core/QuestRuntime0829.cs')
q30=read('Assets/Scripts/Core/QuestRuntime0830.cs')
q31=read('Assets/Scripts/Core/QuestRuntime0831.cs')
sd=read('Assets/Scripts/Shell/AscendanceSaveData.cs')
ss=read('Assets/Scripts/Shell/AscendanceSaveSystem.cs')
obj=read('Assets/Scripts/World/QuestObjectAvatar0829.cs')
test=read('Assets/Editor/QuestBatchQ006Q008TestTools0831.cs')
checks=[]
def ck(label,cond):checks.append((label,bool(cond)))
ck('version','public const string BuildVersion = "0.8.31";' in gm)
ck('banner','Campaign I Quest Batch 2 (Q006–Q008) loaded.' in gm)
for q in ['Q006','Q007','Q008']:ck('definition '+q,f'["{q}"]' in qr)
for loc in ['Goblin Fortress Gate','Prison Pens','Ritual Circle']:ck('location '+loc,loc in gm)
ck('q006 pool','CurrentQuestId0829=="Q006"' in qr and 'locationId=="L018"' in qr and 'locationId=="L019"' in qr)
ck('q007 pool','CurrentQuestId0829=="Q007"' in qr and 'locationId=="L005"' in qr and 'locationId=="L016"' in qr)
ck('q008 pool','CurrentQuestId0829=="Q008"' in qr and 'locationId=="L017"' in qr)
ck('next quests','CurrentQuestId0829=="Q005")return "Q006"' in q30 and 'CurrentQuestId0829=="Q007")return "Q008"' in q30)
ck('delegation location','OnQuestLocationRevealed0831(location,hero)' in q30)
ck('delegation end phase','ResolveQuestEndEnemyPhase0831()' in q30)
ck('delegation outcome','EvaluateQuestOutcome0831()' in q30)
ck('delegation reward','ResolveQuestReward0831(receiver)' in q30)
ck('movement prisoner hook','OnQuestHeroMoved0831(hero)' in gm)
ck('q006 prisoners','Q006_PRISONER_' in q31 and 'PrisonersFreed' in q31 and 'PrisonersRescued' in q31 and 'PrisonersExecuted' in q31)
ck('q006 alarm scaling','int alarmAt=PartySize<=1?2:1' in q31 and 'E011' in q31)
ck('q006 third prisoner reinforcement','third Prisoner freed' in q31 and 'PrisonReinforcementSpawned' in q31)
ck('q006 execution','UnityEngine.Random.Range(1,7)' in q31 and 'PrisonersExecuted>=4' in q31)
ck('q006 reward','PartyMorale0831+=1' in q31 and 'AddGeneralReputation(2' in q31)
ck('q007 3 wagons','for(int i=1;i<=3;i++)' in q31 and 'Type="SupplyWagon"' in q31)
ck('q007 two sabotage','wagon.Supply=Mathf.Min(2,wagon.Supply+1)' in q31 and 'wagon.Supply>=2' in q31)
ck('q007 moving route','MoveSupplyWagons0831' in q31 and 'GraphDistance(id,"L016")<exitNow' in q31)
ck('q007 escape fail','SupplyWagonsEscaped>=2' in q31)
ck('q007 reinforcements','Convoy Reinforcement' in q31 and 'SupplyWagonsDestroyed' in q31)
ck('q007 wolf priority','NextQuestEnemyStep0831' in gm and 'enemy.TemplateId!="E013"' in q31)
ck('q005 carryover q007','RemoveEliteFromQ007Setup0830' in q31 and 'initial Goblin Wolf Rider' in q31)
ck('q007 reward q008','RemoveSiegeEngineFromQ008Setup0831=true' in q31)
ck('q008 stats','Q008SiegeEngineHealth0831=10' in q31 and 'Q008SiegeEngineArmor0831=2' in q31)
ck('q008 wall 6','QuestRuntime0829.WallTrack=6' in q31)
ck('q008 placement','PlaceSiegeEngine0831' in q31 and 'E016' in q31)
ck('q008 basic attack','AttackSiegeEngine0831' in q31 and 'Mathf.Max(1,attack-engine.Armor)' in q31)
ck('q008 bombardment','Q008 BOMBARDMENT' in q31 and 'WallTrack-engines.Count' in q31)
ck('q008 wall thresholds','WallTrack<=4' in q31 and 'WallTrack<=2' in q31)
ck('q008 champion','E015' in q31 and 'FINAL DEFENDER' in q31 and 'ChampionDefeated' in q31)
ck('q008 reward','DrawEpicEquipment0831' in q31 and 'AddPartyGold(175' in q31 and 'h.XP+=200' in q31)
ck('we003 archer modifier','QuestEnemyAttackBonus0831(enemy)' in gm and 'enemy.TemplateId=="E002"?1:0' in q31)
ck('object persistence fields','OwnerHeroId' in qr and 'MaxHealth' in qr and 'Armor' in qr)
ck('save campaign carryover','PartyMorale0831' in sd and 'RemoveSiegeEngineFromQ008Setup0831' in sd and 'PartyMorale0831=gm.PartyMorale0831' in ss)
for f in ['PrisonersFreed','PrisonersRescued','PrisonersExecuted','SupplyWagonsDestroyed','SupplyWagonsEscaped','WallTrack','SiegeEnginesPlaced','SiegeEnginesDestroyed','ChampionDefeated']:
    ck('save field '+f,f in sd and f+'=q.'+f in ss)
ck('object save health','Health=o.Health' in ss and 'MaxHealth=o.MaxHealth' in ss and 'OwnerHeroId=o.OwnerHeroId' in ss)
ck('object visuals','obj.Type=="Prisoner"' in obj and 'obj.Type=="SiegeEngine"' in obj and 'obj.Type=="SupplyWagon"' in obj)
ck('validator menu','Test 0.8.31 - Campaign I Q006-Q008 (Play Mode)' in test)
for phrase in ['Prison Pens reveal places 6 Prisoners','third freed Prisoner triggers','fails when 4 Prisoners are executed','places 3 Supply Wagons','two Sabotage Actions destroy a Wagon','fails immediately when any 2 Supply Wagons escape','Wall Track at 6','10 Health / 2 Armor','Bombardment removes 1 Wall','spawns the Goblin Champion','Epic Equipment','save state persists Q006-Q008']:
    ck('validator '+phrase,phrase in test)
# ensure debug helpers referenced by validator exist
for m in sorted(set(re.findall(r'gm\.(Debug\w+0831)',test))):ck('debug helper '+m,m in q31)
# compile-safety scans
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
    raw=p.read_text(errors='ignore');clean=strip(raw)
    for a,b in [('(',')'),('{','}'),('[',']')]:
        bal=0;bad=False
        for ch in clean:
            if ch==a:bal+=1
            elif ch==b:bal-=1
            if bal<0:bad=True;break
        ck('delimiter '+p.name+a+b,bal==0 and not bad)
    in_str=False;verb=False;esc=False;bad=False;i=0
    while i<len(raw):
        c=raw[i]
        if c=='\n' and in_str and not verb:bad=True;break
        if not in_str:
            if c=='"':verb=i>0 and raw[i-1]=='@';in_str=True;esc=False
        elif verb:
            if c=='"':
                if i+1<len(raw) and raw[i+1]=='"':i+=1
                else:in_str=False;verb=False
        else:
            if esc:esc=False
            elif c=='\\':esc=True
            elif c=='"':in_str=False
        i+=1
    ck('raw string '+p.name,not bad)
for p in cs:
    raw=p.read_text(errors='ignore')
    for m in re.finditer(r'\bIEnumerator\s+\w+\s*\([^)]*\)\s*\{',raw):
        start=m.end();d=1;i=start
        while i<len(raw) and d:
            if raw[i]=='{':d+=1
            elif raw[i]=='}':d-=1
            i+=1
        ck('yield '+p.name+':'+m.group(0).split('(')[0],'yield ' in raw[start:i])
failed=[x for x in checks if not x[1]]
for label,ok in checks:print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'RESULT: {len(checks)-len(failed)}/{len(checks)} checks passed')
sys.exit(1 if failed else 0)
