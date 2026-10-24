#!/usr/bin/env python3
from pathlib import Path
import json, re, sys

ROOT = Path(__file__).resolve().parents[1]
GM = (ROOT/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(encoding='utf-8')
MODELS = (ROOT/'Assets/Scripts/Core/AscendanceModels.cs').read_text(encoding='utf-8')
HUD = (ROOT/'Assets/Scripts/UI/AscendanceHUD.cs').read_text(encoding='utf-8')
SAVE_DATA = (ROOT/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text(encoding='utf-8')
SAVE_SYS = (ROOT/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text(encoding='utf-8')
HELPER = (ROOT/'Assets/Editor/TurnLifecycleTestTools0811.cs').read_text(encoding='utf-8')
RUNTIME = (ROOT/'Assets/Scripts/Core/HeroRulesRuntime.cs').read_text(encoding='utf-8')
roster = json.loads((ROOT/'Assets/Resources/Ascendance/Data/hero_roster.json').read_text())['heroes']
cards = json.loads((ROOT/'Assets/Resources/Ascendance/Data/hero_cards.json').read_text())['cards']

results=[]
def check(label, cond):
    results.append((label, bool(cond)))

def method_body(text, signature):
    i=text.find(signature)
    if i<0: return ''
    b=text.find('{',i)
    if b<0: return ''
    depth=0
    in_str=False; esc=False; verb=False; ch=False; line=False; block=False
    for j in range(b,len(text)):
        c=text[j]; n=text[j+1] if j+1<len(text) else ''
        if line:
            if c=='\n': line=False
            continue
        if block:
            if c=='*' and n=='/': block=False
            continue
        if in_str:
            if verb:
                if c=='"' and n=='"': continue
                if c=='"': in_str=False; verb=False
            else:
                if esc: esc=False
                elif c=='\\': esc=True
                elif c=='"': in_str=False
            continue
        if ch:
            if esc: esc=False
            elif c=='\\': esc=True
            elif c=="'": ch=False
            continue
        if c=='/' and n=='/': line=True; continue
        if c=='/' and n=='*': block=True; continue
        if c=='@' and n=='"': in_str=True; verb=True; continue
        if c=='"': in_str=True; continue
        if c=="'": ch=True; continue
        if c=='{': depth+=1
        elif c=='}':
            depth-=1
            if depth==0: return text[b:j+1]
    return ''

def balanced(text, left, right):
    # lightweight structural sanity check after stripping comments/strings
    stripped=re.sub(r'//.*?$|/\*.*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '', text, flags=re.M|re.S)
    return stripped.count(left)==stripped.count(right)

start = method_body(GM, 'public bool TryStartHeroTurn(HeroState hero)')
req = method_body(GM, 'private bool RequireHeroTurn(HeroState hero, string action)')
end = method_body(GM, 'public void EndHeroTurn()')
resolve = method_body(GM, 'private IEnumerator ResolveRound()')
load = method_body(GM, 'public void ApplySaveData(AscendanceSaveData save)')
party = method_body(GM, 'public void SetParty(IEnumerable<string> heroIds')

check('BuildVersion is 0.8.11', 'BuildVersion = "0.8.11"' in GM)
check('16 authored Heroes', len(roster)==16)
check('323 Hero-card definitions', len(cards)==323)
by={h['id']:0 for h in roster}
for c in cards: by[c['heroId']]=by.get(c['heroId'],0)+int(c.get('copies',1))
check('every personal deck totals exactly 30 cards', all(v==30 for v in by.values()) and len(by)==16)
check('HeroState defaults to 0 Actions', re.search(r'public int Actions\s*=\s*0\s*;', MODELS) is not None)
check('BuildHeroes creates Heroes with 0 Actions', re.search(r'LocationId\s*=\s*"L000"\s*,\s*Actions\s*=\s*0', GM) is not None)
check('SetParty clears Actions for the whole roster', 'hero.Actions = 0;' in party and 'hero.AttackOnlyActions = 0;' in party)
check('SetParty begins with no turn owner', 'TurnHeroId = null;' in party)
check('explicit CanStartHeroTurn authority exists', 'public bool CanStartHeroTurn(HeroState hero)' in GM)
check('normal turn actions require current TurnHero', re.search(r'CanUseTurnActions\(HeroState hero\).*IsHeroTakingTurn\(hero\)', GM) is not None)
check('normal actions no longer implicitly claim an empty turn', 'TurnHeroId = hero.Id;' not in req and 'press Start Turn' in req)
check('Start Turn assigns TurnHero before resources', start.find('TurnHeroId = hero.Id;') >= 0 and start.find('TurnHeroId = hero.Id;') < start.find('DrawCards(hero, hero.DrawValue)'))
check('Start Turn resets per-turn runtime at actual start', 'HeroRulesRuntime.ResetTurn(hero);' in start and 'hero.MovedThisTurn = false;' in start)
check('Round 1 opening hand is not drawn again', 'if (Round == 1)' in start and 'keeps the 5-card starting hand' in start)
check('Round 2+ draw uses Hero DrawValue at Start Turn', 'DrawCards(hero, hero.DrawValue);' in start)
check('Start Turn grants exactly 3 Actions after Draw Step', start.find('hero.Actions = 3;') > start.find('DrawCards(hero, hero.DrawValue)'))
check('End Turn preserves unused general Actions', 'int saved=GeneralActions(hero);' in end and 'hero.Actions=Mathf.Max(0,hero.Actions-hero.AttackOnlyActions)' in end)
check('End Turn strips attack-only temporary Actions', 'hero.AttackOnlyActions=0;' in end)
check('End Turn marks Hero complete and releases turn owner', 'hero.TurnCompleted=true;' in end and 'TurnHeroId=null;' in end)
check('World Phase starts only when no eligible unfinished Heroes remain', 'remaining.Count==0' in end and 'StartCoroutine(ResolveRound())' in end)
check('ResolveRound no longer performs party-wide Hero draws', 'DrawCards(' not in resolve)
check('End Phase clears held Actions', 'hero.Actions=0;' in resolve and 'hero.AttackOnlyActions=0;' in resolve)
check('End Phase resets TurnCompleted for next Hero Phase', 'hero.TurnCompleted=false;' in resolve)
check('new Round returns to no turn owner', 'Round++;' in resolve and 'TurnHeroId=null;' in resolve)
check('Save DTO stores TurnHeroId and Hero Actions/TurnCompleted', 'public string TurnHeroId;' in SAVE_DATA and 'public int Actions;' in SAVE_DATA and 'public bool TurnCompleted;' in SAVE_DATA)
check('Save capture persists TurnHeroId', 'TurnHeroId=gm.TurnHeroId' in SAVE_SYS)
check('Load restores valid in-progress TurnHeroId', 'save.TurnHeroId' in load and 'TurnHeroId = th.Id;' in load)
check('Load normalizes unfinished non-turn Heroes to 0 Actions', '!hero.TurnCompleted && !IsHeroTakingTurn(hero)' in load and 'hero.Actions = 0;' in load)
check('HUD exposes Start Turn / End Turn control', '"START TURN"' in HUD and '"END TURN"' in HUD and 'StartSelectedHeroTurn()' in HUD)
check('HUD shows explicit choose/start state', 'CHOOSE HERO • START TURN' in HUD)
check('0.8.10 EnemyAttack damage source preserved', 'EnemyAttack' in GM and 'BeginHeroDamage(target,pendingDamage' in GM)
check('0.8.9 Interpose retarget parser preserved', 'become the target' in GM and 'instead' in GM)
check('0.8.11 Play Mode lifecycle helper present', 'Test 0.8.11 - Hero Turn Start Lifecycle' in HELPER)
check('helper checks Round 1 no duplicate draw', 'Round 1 keeps the 5-card opening hand' in HELPER)
check('helper checks second Hero cannot steal turn', 'cannot steal an in-progress turn' in HELPER)
check('HeroRulesRuntime ResetTurn remains separate from ResetRound', 'public static void ResetTurn(HeroState hero)' in RUNTIME and 'public static void ResetRound(HeroState hero)' in RUNTIME)
for name,text in [('AscendanceGameManager.cs',GM),('AscendanceModels.cs',MODELS),('AscendanceHUD.cs',HUD),('AscendanceSaveData.cs',SAVE_DATA),('AscendanceSaveSystem.cs',SAVE_SYS),('TurnLifecycleTestTools0811.cs',HELPER)]:
    check(name+' brace balance', balanced(text,'{','}'))
    check(name+' parenthesis balance', balanced(text,'(',')'))
    check(name+' bracket balance', balanced(text,'[',']'))

out=[]
for label, ok in results: out.append(('PASS' if ok else 'FAIL')+' — '+label)
out.append('')
out.append(f'SUMMARY — {sum(ok for _,ok in results)}/{len(results)} static checks passed.')
text='\n'.join(out)+'\n'
print(text,end='')
(ROOT/'VALIDATION_0.8.11.txt').write_text(text,encoding='utf-8')
(ROOT/'tests/verification-0811.txt').write_text(text,encoding='utf-8')
sys.exit(0 if all(ok for _,ok in results) else 1)
