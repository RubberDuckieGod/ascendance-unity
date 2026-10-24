from pathlib import Path
import re, sys, zipfile
root=Path(__file__).resolve().parents[1]
read=lambda p:(root/p).read_text(errors='ignore')
gm=read('Assets/Scripts/Core/AscendanceGameManager.cs')
cr=read('Assets/Scripts/Core/CampaignRuntime0833.cs')
qr=read('Assets/Scripts/Core/QuestRuntime0829.cs')
q30=read('Assets/Scripts/Core/QuestRuntime0830.cs')
q31=read('Assets/Scripts/Core/QuestRuntime0831.cs')
sd=read('Assets/Scripts/Shell/AscendanceSaveData.cs')
ss=read('Assets/Scripts/Shell/AscendanceSaveSystem.cs')
hud=read('Assets/Scripts/UI/AscendanceHUD.cs')
test=read('Assets/Editor/CampaignProgressionTestTools0833.cs')
checks=[]
def ck(label,cond): checks.append((label,bool(cond)))

# Identity + required files
ck('version 0.8.33','public const string BuildVersion = "0.8.33";' in gm)
ck('banner','Campaign I World Event & Campaign Progression loaded.' in gm)
for rel in ['Assets/Scripts/Core/CampaignRuntime0833.cs','Assets/Scripts/Core/CampaignRuntime0833.cs.meta','Assets/Editor/CampaignProgressionTestTools0833.cs','Assets/Editor/CampaignProgressionTestTools0833.cs.meta']:
    ck('required '+rel,(root/rel).exists())

# Exact World Event campaign structure from workbook
for event,name,m1,m2,opt,nextid,limit in [
    ('WE001','Goblin Raids','Q001','Q002','Q003','WE002',5),
    ('WE002','Goblin Warbands','Q004','Q005','Q006','WE003',6),
    ('WE003','Siege of Stonebrook','Q007','Q008','Q009','WE004',6),
    ('WE004','The Goblin King','Q010','Q011','Q012','null',0)]:
    ck('event '+event, f'["{event}"]=new WorldEventDefinition0833' in cr)
    for token in [f'Name="{name}"',f'Main1="{m1}"',f'Main2="{m2}"',f'Optional="{opt}"',f'DayLimit={limit}']:
        ck(f'{event} {token}',token in cr)
    ck(f'{event} next', (f'NextId="{nextid}"' in cr) if nextid!='null' else 'NextId=null' in cr)

# Campaign state / history / persistence fields
fields=['CurrentWorldEventId','WorldEventDay','TotalCampaignDay','CampaignCompleted','CampaignDefeated','CompletedQuestIds','FailedQuestIds','MissedQuestIds','CompletedWorldEventIds','RemoveScoutFromQ004Setup','AddRaiderToQ004FirstEncounter','AddEliteToQ007Setup','WE003TravelSeverityPenalty','DamagedBuildings','Q010SiegePressureModifier','Q010WoundPenalty','StonebrookEquipmentPurchaseLocked']
for f in fields:
    ck('runtime campaign field '+f,f in cr)
    ck('save campaign field '+f,f in sd)
    ck('capture campaign field '+f,f'{f}=c.{f}' in ss)
    ck('restore campaign field '+f, (f'{f}=s.{f}' in cr or f'{f}=Mathf.Max(0,s.{f})' in cr or (f=='CurrentWorldEventId' and 'CurrentWorldEventId=CampaignContent0833.WorldEvents.ContainsKey' in cr)))
ck('save sync before capture','gm.SyncCampaignProgress0833();' in ss)
ck('save payload present','public CampaignProgressSaveData0833 CampaignProgress0833;' in sd)
ck('load restore hook','RestoreCampaignProgress0833(save);' in gm)
ck('init hook','InitializeCampaignProgress0833();' in gm)

# Quest outcome integration / natural campaign routing
ck('real quest reward records campaign completion','RecordCampaignQuestCompleted0833(CurrentQuestId0829);' in qr)
ck('debug start aligns WE','DebugAlignCampaignWorldEventForQuest0833(id);' in qr)
ck('campaign begin uses real StartQuest','StartQuest0829(questId);ApplyCampaignQuestStartEffects0833(questId);' in cr)
ck('campaign begin advances chapter day','CampaignProgress0833.WorldEventDay++;CampaignProgress0833.TotalCampaignDay++;' in cr)
ck('unresolved blocks quest begin','!CampaignQuestResolved0833' in cr)
ck('available quest list','public IReadOnlyList<string> AvailableCampaignQuests0833' in cr)
ck('main campaign begin','BeginRecommendedCampaignQuest0833' in cr)
ck('optional campaign begin','BeginOptionalCampaignQuest0833' in cr)
ck('world event advance condition','Completed0833(w.Main1)&&Completed0833(w.Main2)' in cr and 'WorldEventDay>=w.DayLimit' in cr)
ck('advance marks missed','MissedQuestIds.Add(q)' in cr)
ck('advance records WE','CompletedWorldEventIds.Add(w.Id)' in cr)
ck('WE004 no next','CurrentWorldEventId0833=="WE004"' in cr and 'return false;' in cr)
ck('q012 campaign completed','questId=="Q012"' in cr and 'CampaignProgress0833.CampaignCompleted=true' in cr)
ck('q012 campaign defeated','questId=="Q012"' in cr and 'CampaignProgress0833.CampaignDefeated=true' in cr)

# Chapter rewards / penalties exactly represented
for phrase in [
    '+50 XP each • +1 Party Morale • remove 1 Goblin Scout from Q004',
    '+1 Raider in Q004 first patrol • -1 General Reputation',
    '+75 XP each • 1 Rare Equipment • remove 1 Elite from Q007 • +1 Reputation',
    '+1 Elite in Q007 • WE003 Travel Encounter rolls +1 • -1 Reputation',
    '+100 XP each • 1 Epic Equipment • +2 Reputation • Q010 Siege Pressure -1',
    'WE004 begins with 1 Damaged Building',
    'Q010 Siege Pressure +1 banked • each Hero begins Q010 with 1 Wound Token • -2 Reputation']:
    ck('chapter text '+phrase,phrase in cr)
ck('WE1 reward mechanics','h.XP+=50' in cr and 'PartyMorale0831+=1' in cr and 'RemoveScoutFromQ004Setup=true' in cr)
ck('WE1 penalty mechanics','AddRaiderToQ004FirstEncounter=true' in cr and 'AddGeneralReputation(-1,"WE001 incomplete chapter penalty")' in cr)
ck('WE2 reward mechanics','h.XP+=75' in cr and 'DrawRareEquipment0830' in cr and 'RemoveEliteFromQ007Setup0830=true' in cr)
ck('WE2 penalty mechanics','AddEliteToQ007Setup=true' in cr and 'WE003TravelSeverityPenalty=1' in cr)
ck('WE3 reward mechanics','h.XP+=100' in cr and 'DrawEpicEquipment0831' in cr and 'Q010SiegePressureModifier=-1' in cr)
ck('WE3 required-only building','DamagedBuildings=Mathf.Max(1' in cr)
ck('WE3 penalty mechanics','Q010SiegePressureModifier=1' in cr and 'Q010WoundPenalty=true' in cr and 'StonebrookEquipmentPurchaseLocked=true' in cr)
ck('wound token exact one per hero','Name="Wound Token"' in cr and 'Statuses.RemoveAll' in cr)
ck('siege conflict preserves two Ballistas','two Ballistas remain print-locked' in cr)

# Concrete carry-forward hooks
ck('Q004 scout reduction runtime','RemoveScoutFromQ004Setup0833?1:2' in q30)
ck('Q004 penalty raider runtime','if(AddRaiderToQ004FirstEncounter0833)SpawnQuestEnemy0829("E001"' in q30)
ck('Q007 WE2 bonus runtime','if(!RemoveEliteFromQ007Setup0830)SpawnQuestEnemy0829("E013"' in q31)
ck('Q007 penalty elite runtime','if(AddEliteToQ007Setup0833)SpawnQuestEnemy0829("E013"' in q31)
ck('WE1 travel dynamic','WorldEventTravelSeverityModifier0833' in gm and 'IsWorldEventActive0833("WE001")?1' in cr)
ck('WE3 penalty travel dynamic','IsWorldEventActive0833("WE003")?WE003TravelSeverityPenalty0833' in cr)
ck('WE4 random travel suppression','RandomTravelEncountersSuppressed0833' in gm and 'public bool RandomTravelEncountersSuppressed0833' in cr)
ck('WE1 rarity exposed','MaxPurchasableEquipmentRarity0833' in cr and '"Common"' in cr)
ck('WE2 price exposed','EquipmentPriceMultiplier0833' in cr and '1.25f' in cr)

# HUD integration
for token in ['questOptionalButton','worldEventAdvanceButton','BeginRecommendedCampaignQuest0833()','BeginOptionalCampaignQuest0833()','AdvanceWorldEvent0833()','CampaignSummary0833']:
    ck('HUD '+token,token in hud)
ck('HUD main label','BEGIN {mainQuest}' in hud)
ck('HUD optional label','OPTIONAL {optionalQuest}' in hud)
ck('HUD advance gate','worldEventAdvanceButton.interactable=gm.CanAdvanceWorldEvent0833' in hud)

# Validator coverage + real campaign router
ck('validator menu','Test 0.8.33 - Campaign I World Events (Play Mode)' in test)
ck('validator no DebugStart','DebugStartQuest0829' not in test)
ck('validator starts campaign reset','DebugResetCampaignProgress0833' in test)
ck('validator uses main router','BeginRecommendedCampaignQuest0833()' in test)
ck('validator uses optional router','BeginOptionalCampaignQuest0833()' in test)
ck('validator uses WE advance','AdvanceWorldEvent0833()' in test)
ck('validator real reward chain comment','real Quest reward' in test)
for phrase in [
    'Campaign I starts in WE001 on Campaign Day 1 with Q001 active',
    'WE001 all-quests reward grants +50 XP each',
    'mechanically removes one Scout from Q004',
    "Q005's real Quest reward banks",
    'WE002 all-quests reward grants +75 XP',
    "Q009's real Quest reward banks Captain Roland",
    'mechanically removes the initial Q007 Wolf Rider/Elite',
    "Q007's real Quest reward banks",
    'WE003 all-quests reward grants +100 XP',
    "Captain Roland's Q009 assistance flag survives",
    "print-locked two Ballistas",
    'Q012 completion records Campaign I Victory',
    'persist through the real save system',
    'authored 5-Day threshold',
    'authored 6-Day threshold',
    'each Hero exactly one persistent Wound Token']:
    ck('validator phrase '+phrase,phrase in test)
actual_assertions=test.count('Check(')-1
ck('validator assertion count 45',actual_assertions==45)
ck('validator expected dynamic summary','Campaign I World Event progression completed: {pass} passed, 0 failed.' in test)
# Fresh-route isolation and real reward execution
for token in ['RemoveEliteFromQ007Setup0830=false','RemoveSiegeEngineFromQ008Setup0831=false','CaptainRolandAssistsQ0100832=false','PartyMorale0831=0']:
    ck('fresh route reset '+token,token in cr)
ck('debug success calls real reward','ResolveQuestReward0829();' in cr)

# No known unsafe / accidental shortcuts
ck('validator no direct completion list mutation','CompletedQuestIds.Add' not in test)
ck('validator no direct WE state mutation',re.search(r'CampaignProgress0833\.CurrentWorldEventId\s*=(?!=)',test) is None)
ck('runtime no editor Check','Check(' not in cr)
ck('campaign no direct Q010 Ballista count rewrite','Q010SiegePressureModifier' in cr and 'BallistasDestroyed' not in cr)

# Compile-safety scans over all project C#
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

# Debug helper calls from validator must exist in project runtime.
all_runtime='\n'.join(p.read_text(errors='ignore') for p in (root/'Assets/Scripts/Core').glob('*.cs'))
for m in sorted(set(re.findall(r'gm\.(Debug\w+08\d+)',test))): ck('debug helper '+m,m in all_runtime)

failed=[x for x in checks if not x[1]]
for label,ok in checks: print(('PASS' if ok else 'FAIL')+' — '+label)
passed=len(checks)-len(failed)
print(f'RESULT: {passed}/{len(checks)} checks passed')
if failed:
    print('\nFAILED CHECKS:')
    for label,_ in failed:print(' - '+label)
sys.exit(1 if failed else 0)
