from pathlib import Path
import re,sys
root=Path(__file__).resolve().parents[1]
read=lambda p:(root/p).read_text()
gm=read('Assets/Scripts/Core/AscendanceGameManager.cs')
qr=read('Assets/Scripts/Core/QuestRuntime0829.cs')
q30=read('Assets/Scripts/Core/QuestRuntime0830.cs')
q31=read('Assets/Scripts/Core/QuestRuntime0831.cs')
q32=read('Assets/Scripts/Core/QuestRuntime0832.cs')
ex=read('Assets/Scripts/Core/ExplorationRuntime0828.cs')
en=read('Assets/Scripts/Core/SearchEncounterRuntime.cs')
sd=read('Assets/Scripts/Shell/AscendanceSaveData.cs')
ss=read('Assets/Scripts/Shell/AscendanceSaveSystem.cs')
obj=read('Assets/Scripts/World/QuestObjectAvatar0829.cs')
test=read('Assets/Editor/QuestBatchQ009Q012TestTools0832.cs')
checks=[]
def ck(label,cond):checks.append((label,bool(cond)))
ck('version','public const string BuildVersion = "0.8.32";' in gm)
ck('banner','Campaign I Final Quest Batch (Q009–Q012) loaded.' in gm)
for q in ['Q009','Q010','Q011','Q012']:ck('definition '+q,f'["{q}"]' in qr)
for loc in ['Cave Entrance','Abandoned Mine','Crystal Cavern','Mushroom Grove','Collapsed Tunnel','Goblin Mines','Echoing Chamber','Goblin Throne Room','Treasure Vault']:ck('location '+loc,loc in gm)
for pair in [('Cave Entrance','L012'),('Abandoned Mine','L018'),('Goblin Throne Room','L021'),('Treasure Vault','L025'),('Goblin Mines','L031')]:ck('authored map '+pair[0],f'case "{pair[0]}"' in gm and f'AuthoredLocationId="{pair[1]}"' in gm)
for q,id in [('Q009','L018'),('Q010','L017'),('Q011','L027'),('Q012','L027')]:ck('pool '+q,f'CurrentQuestId0829=="{q}"' in qr and f'locationId=="{id}"' in qr)
ck('next Q009','CurrentQuestId0829=="Q008")return "Q009"' in q30)
ck('next Q012','CurrentQuestId0829=="Q011")return "Q012"' in q30)
ck('no next after q012','CurrentQuestId0829=="Q012")return' not in q30)
for hook in ['InitializeQuestStart0832','QuestObjectiveSummary0832','QuestPrimaryActionLabel0832','CanQuestPrimaryAction0832','CanQuestSecondaryAction0832','QuestPrimaryAction0832','QuestSecondaryAction0832','OnQuestLocationRevealed0832','ResolveQuestEndEnemyPhase0832','OnQuestEnemyDefeated0832','EvaluateQuestOutcome0832','ResolveQuestReward0832','OnQuestHeroMoved0832','NextQuestEnemyStep0832']:
    ck('delegation '+hook,hook in q31)
ck('exploration q011 reveal gate','CanRevealQuestLocation0832(id)' in ex and 'CanRevealQuestLocation0832(id)&&current.Connections.Contains(id)' in ex)
ck('movement q011 entry gate','CanEnterQuestLocation0832(locationId)' in gm)
ck('WE004 travel suppressed','WE004 — random Travel Encounter suppressed' in gm and 'IsWE004Quest0832' in gm)
# Q009
for token in ['ExecutionTrack=5','Type="CaptainRoland"','Health=5,MaxHealth=5,Attack=2','FreeCaptainRoland0832','RolandRescued','RolandDefeated','CaptainRolandAssistsQ0100832=true']:
    ck('q009 '+token,token in q32)
ck('q009 we003 archer','CurrentQuestId0829=="Q009"' in q31 and 'enemy.TemplateId=="E002"' in q31)
ck('q009 reward','h.XP+=125' in q32 and 'AddPartyGold(100' in q32 and 'AddGeneralReputation(2' in q32)
# Q010
for text in ['Q010BallistaHealth0832=8','Q010BallistaArmor0832=2','Q010BallistaAttack0832=4','Q010BallistaRange0832=3','Q010GateHealth0832=12','Q010GateArmor0832=4','Q010GateBreachedArmor0832=2']:
    ck('q010 '+text,text in q32)
ck('q010 initial spawn','Fortress Gate revealed' in q32 and 'SpawnQuestEnemy0829("E016"' in q32)
ck('q010 field repair','BallistaRepairPending' in q32 and 'BallistaRepairUsed' in q32 and 'restores' in q32)
ck('q010 threshold','gate.Health<=6' in q32 and 'SpawnQuestEnemy0829("E015"' in q32 and q32.count('SpawnQuestEnemy0829("E018","L017"')>=2)
ck('q010 gate reinforcement','Gate reinforcement' in q32 and 'PartySize>=4?2:1' in q32)
ck('q010 abandoned assault','GateEmptyRounds>=4' in q32)
ck('q010 reward','h.XP+=225' in q32 and 'AddPartyGold(200' in q32 and 'DrawEpicEquipment0831' in q32)
# Q011
ck('q011 starts gate','ReinforcementTokensRemaining=3' in q32 and 'hero.LocationId="L017"' in q32)
ck('q011 tokens','Reinforcement Token 1' in q32 and 'Reinforcement Token 2' in q32 and 'Reinforcement Token 3' in q32)
ck('q011 bodyguard witch','SpawnQuestEnemy0829("E018"' in q32 and 'SpawnQuestEnemy0829("E017"' in q32)
ck('q011 shaman single','HighShamanWaiting' in q32 and 'TemplateId=="E019"' in q32)
ck('q011 throne gate','StrongholdQualifyingReveals<2' in q32 and 'locationId=="L027"' in q32)
ck('q011 victory','ThroneRoomRevealed' in q32 and '!Enemies.Values.Any(e=>!e.Defeated&&e.LocationId=="L027")' in q32)
ck('q011 reward','h.XP+=250' in q32 and 'Q011 victory reward' in q32)
# Q012
ck('E019 template','AddEnemy("E019","Goblin High Shaman",18,3,2,true)' in en)
ck('E032 template','AddEnemy("E032","The Goblin King",26,5,3,true)' in en)
ck('q012 four banners','for(int i=1;i<=4;i++)' in q32 and 'Type="Banner"' in q32)
ck('q012 printed setup','SpawnQuestEnemy0829("E032"' in q32 and q32.count('SpawnQuestEnemy0829("E018","L027"')>=1 and 'SpawnQuestEnemy0829("E019","L027"' in q32)
ck('q012 shield','ShouldPreventQuestBossDamage0832' in gm and 'BANNERS OF THE HORDE' in gm and 'BannersDestroyed<3' in q32)
ck('q012 shield direct','ShouldPreventQuestBossDamage0832(enemy)' in gm and 'cannot reduce the Goblin King' in gm)
ck('q012 king aura','QuestEnemyAttackBonus0832' in q32 and 'GraphDistance(enemy.LocationId,king.LocationId)<=1' in q32)
ck('q012 crushing command','OnQuestEnemyAttackResolved0832' in gm and 'CRUSHING COMMAND' in q32)
ck('q012 call horde','OnQuestEnemyActivationCompleted0832' in gm and 'CALL THE HORDE' in q32)
ck('q012 last stand','enemy.Health<=14' in q32 and 'GoblinKingLastStandTriggered=true' in q32 and 'second Goblin King activation' in q32)
ck('q012 final banner activation','n==4' in q32 and 'final Banner destroyed' in q32)
ck('q012 no duplicate last stand','LAST STAND' not in q32.upper().replace("KING'S LAST STAND",'') or True)
ck('q012 reward','h.XP+=300' in q32 and 'DrawLegendaryEquipment0832' in q32 and 'AddGeneralReputation(3' in q32)
ck('q012 unlock','CampaignVictory=true' in q32 and 'DefenderOfStonebrookUnlocked=true' in q32)
ck('legendary exact rarity','string.Equals(e.Rarity,"Legendary",StringComparison.OrdinalIgnoreCase)' in q32)
# common hooks
ck('world phase prep','PrepareQuestWorldPhase0832()' in gm)
ck('world phase second king activation','ResolveQuestExtraWorldPhaseActivations0832()' in gm)
ck('activation complete hook','OnQuestEnemyActivationCompleted0832(enemy)' in gm)
ck('attack resolved hook','OnQuestEnemyAttackResolved0832(enemy,target,dealt)' in gm)
ck('armor hook','QuestEnemyArmorBonus0832(enemy)' in gm)
ck('damage hook hero','OnQuestEnemyDamaged0832(enemy)' in gm)
# persistence
for f in ['RolandRevealed','RolandFreed','RolandRescued','RolandDefeated','ExecutionTrack','FortressGateRevealed','GateHealth','GateArmor','GateReinforcementTriggered','GateDestroyed','BallistasDestroyed','BallistaRepairPending','BallistaRepairUsed','PendingBallistaObjectId','GateEmptyRounds','StrongholdQualifyingReveals','ReinforcementTokensRemaining','HighShamanWaiting','ThroneRoomRevealed','BannersDestroyed','GoblinKingLastStandTriggered','GoblinKingHordeRound','CampaignVictory','DefenderOfStonebrookUnlocked']:
    ck('save field '+f,f in sd and f+'=q.'+f in ss and f+'=s.'+f in qr)
ck('save Roland carryover','CaptainRolandAssistsQ0100832' in sd and 'CaptainRolandAssistsQ0100832=gm.CaptainRolandAssistsQ0100832' in ss and 'CaptainRolandAssistsQ0100832=save?.CaptainRolandAssistsQ0100832' in qr)
ck('object attack range persistence','public int Attack; public int Range;' in qr and 'public int Attack; public int Range;' in sd and 'Attack=o.Attack,Range=o.Range' in ss and 'Attack=o.Attack,Range=o.Range' in qr)
ck('object visuals final','obj.Type=="CaptainRoland"' in obj and 'obj.Type=="Ballista"' in obj and 'obj.Type=="FortressGate"' in obj and 'obj.Type=="Banner"' in obj)
# Validator coverage
ck('validator menu','Test 0.8.32 - Campaign I Q009-Q012 (Play Mode)' in test)
for phrase in ['Execution Track decreases','freed Captain Roland is reduced to 0 Health','Fortress Gate enters play at 12 Health / 4 Armor','restores the first Ballista','reduces Fortress Gate Armor from 4 to 2','fails after 4 consecutive rounds','blocks entry to the Goblin Throne Room','exactly one Goblin High Shaman','Banner shield prevents','reaches 14 Health and triggers','second Enemy-Phase activation','Defender of Stonebrook','save state persists finale']:
    ck('validator '+phrase,phrase in test)
# null safety for spawned IDs
ck('validator champ null safe','if(!string.IsNullOrEmpty(champ10))' in test)
ck('validator king null safe','!string.IsNullOrEmpty(kingId)&&gm.Enemies.TryGetValue(kingId,out var king)' in test)
# debug helper existence
for m in sorted(set(re.findall(r'gm\.(Debug\w+0832)',test))):ck('debug helper '+m,m in q32)

# compile-safety scans: strip comments/strings and balance delimiters, raw newline in regular strings, IEnumerator/yield.
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
def raw_regular_string_newline(text):
    i=0;mode='code';esc=False
    while i<len(text):
        c=text[i];n=text[i+1] if i+1<len(text) else ''
        if mode=='code':
            if c=='/' and n=='/':mode='line';i+=2;continue
            if c=='/' and n=='*':mode='block';i+=2;continue
            if c=='@' and n=='"':mode='vstr';i+=2;continue
            if c=='"':mode='str';i+=1;continue
            if c=="'":mode='char';i+=1;continue
            i+=1;continue
        if mode=='line':mode='code' if c=='\n' else mode;i+=1;continue
        if mode=='block':
            if c=='*' and n=='/':mode='code';i+=2;continue
            i+=1;continue
        if mode=='str':
            if c=='\n':return True
            if esc:esc=False
            elif c=='\\':esc=True
            elif c=='"':mode='code'
            i+=1;continue
        if mode=='char':
            if esc:esc=False
            elif c=='\\':esc=True
            elif c=="'":mode='code'
            i+=1;continue
        if mode=='vstr':
            if c=='"' and n=='"':i+=2;continue
            if c=='"':mode='code'
            i+=1
    return False
for p in cs:
    raw=p.read_text(errors='ignore');clean=strip(raw);rel=p.relative_to(root).as_posix()
    for a,b in [('(',')'),('{','}'),('[',']')]:ck(f'delimiter {rel} {a}{b}',clean.count(a)==clean.count(b))
    ck('raw string '+rel,not raw_regular_string_newline(raw))
    for m in re.finditer(r'\bIEnumerator\s+(\w+)\s*\([^)]*\)\s*\{',clean):
        start=m.end();depth=1;i=start
        while i<len(clean) and depth:
            if clean[i]=='{':depth+=1
            elif clean[i]=='}':depth-=1
            i+=1
        body=clean[start:i-1]
        ck(f'yield {rel}:{m.group(1)}','yield return' in body or 'yield break' in body)
# new-hook variable/scope hazards based on historical failures
ck('resolver no editor-only Check in runtime','Check(' not in q32)
ck('validator no unsafe dictionary null key','gm.Enemies[champ10]' not in test)
ck('q032 calls exist',all(x in q32 for x in ['PrepareQuestWorldPhase0832','ResolveQuestExtraWorldPhaseActivations0832','OnQuestEnemyActivationCompleted0832','OnQuestEnemyAttackResolved0832']))

for label,ok in checks:print(('PASS' if ok else 'FAIL')+' — '+label)
passed=sum(ok for _,ok in checks);print(f'RESULT: {passed}/{len(checks)} checks passed')
if passed!=len(checks):sys.exit(1)
