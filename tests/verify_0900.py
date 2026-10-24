from pathlib import Path
import json, re, sys
root=Path(__file__).resolve().parents[1]
checks=[]
def ck(label,cond):
    checks.append((label,bool(cond)))
    print(('PASS' if cond else 'FAIL')+' — '+label)

gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(errors='ignore')
master=(root/'Assets/Editor/MasterSaveLoadRegressionTestTools0862.cs').read_text(errors='ignore')
release=(root/'Assets/Editor/CoreGameCompleteTestTools0900.cs').read_text(errors='ignore')
ps=(root/'ProjectSettings/ProjectSettings.asset').read_text(errors='ignore')
ebs=(root/'ProjectSettings/EditorBuildSettings.asset').read_text(errors='ignore')
hero=json.loads((root/'Assets/Resources/Ascendance/Data/hero_roster.json').read_text())
cards=json.loads((root/'Assets/Resources/Ascendance/Data/hero_cards.json').read_text())
full=json.loads((root/'Assets/Resources/Ascendance/Data/full_content_0850.json').read_text())

ck('BuildVersion 0.9.0','public const string BuildVersion = "0.9.0";' in gm)
ck('Core Game Complete startup banner','Core Game Complete loaded.' in gm)
ck('production company','companyName: Ascendance' in ps)
ck('production product','productName: Ascendance' in ps)
ck('bundle version 0.9.0','bundleVersion: 0.9.0' in ps)
ck('MainMenu first build scene',ebs.find('Assets/Scenes/MainMenu.unity') < ebs.find('Assets/Scenes/Stonebrook.unity'))
ck('exactly two enabled build scenes',ebs.count('enabled: 1')==2)
ck('16 heroes',len(hero.get('heroes',[]))==16)
ck('323 card definitions',len(cards.get('cards',[]))==323)
counts={c['heroId']:0 for c in cards.get('cards',[]) if c.get('heroId')}
for c in cards.get('cards',[]):
    if c.get('heroId'): counts[c['heroId']]=counts.get(c['heroId'],0)+max(1,int(c.get('copies',1)))
ck('every hero physical deck is 30',all(counts.get(h.get('id'),0)==30 for h in hero.get('heroes',[])))
expected={'loot':190,'equipment':144,'searchCards':100,'encounters':105,'enemies':125,'locations':236,'craftingRecipes':200,'quests':72,'worldEvents':24}
ck('locked authored content counts',all(len(full.get(k,[]))==v for k,v in expected.items()))
for k in ['enemies','locations','craftingRecipes','quests']:
    ids=[x.get('id') for x in full.get(k,[]) if x and x.get('id')]
    ck(f'{k} IDs unique',len(ids)==expected[k] and len(set(i.lower() for i in ids))==expected[k])
ck('Q001-Q072 complete',{f'Q{i:03d}' for i in range(1,73)}=={x.get('id') for x in full.get('quests',[])})
ck('master validator promoted','[ASCENDANCE][0.9.0 TEST]' in master and 'Master Save/Load + Full Regression completed:' in master and master.count('Check(')-1==54)
ck('release gate installed','Test 0.9.0 - Core Game Complete Gate' in release and 'Core Game Complete Gate completed:' in release)
ck('release gate enforces zero nested errors','nestedErrors == 0' in release and 'LogType.Exception' in release and 'LogType.Assert' in release)
ck('release gate runs master regression','MasterSaveLoadRegressionTestTools0862.Test0862();' in release)
ck('0.9.0 changelog exists',(root/'CHANGELOG_0.9.0.md').exists())

# Lightweight balance check that ignores strings/comments well enough for promotion-edited files.
def strip_cs(s):
    s=re.sub(r'/\*.*?\*/','',s,flags=re.S)
    s=re.sub(r'//.*','',s)
    s=re.sub(r'@"(?:""|[^"])*"','""',s)
    s=re.sub(r'\$?"(?:\\.|[^"\\])*"','""',s)
    s=re.sub(r"'(?:\\.|[^'\\])'","''",s)
    return s
for rel in ['Assets/Scripts/Core/AscendanceGameManager.cs','Assets/Editor/MasterSaveLoadRegressionTestTools0862.cs','Assets/Editor/CoreGameCompleteTestTools0900.cs','Assets/Editor/FullLocationRuntimeTestTools0858.cs','Assets/Editor/FullEnemyRuntimeTestTools0859.cs','Assets/Editor/FullHeroRuntimeTestTools0860.cs','Assets/Editor/FullCampaignIntegrationTestTools0861.cs']:
    text=strip_cs((root/rel).read_text(errors='ignore'))
    ck(rel+' brace balance',text.count('{')==text.count('}'))
    ck(rel+' paren balance',text.count('(')==text.count(')'))
    ck(rel+' bracket balance',text.count('[')==text.count(']'))

failed=[label for label,ok in checks if not ok]
print(f'RESULT: {len(checks)-len(failed)}/{len(checks)} checks passed')
if failed:
    print('FAILED:',*failed,sep='\n- ')
raise SystemExit(1 if failed else 0)
