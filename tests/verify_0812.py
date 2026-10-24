#!/usr/bin/env python3
from pathlib import Path
import json, re, sys

ROOT = Path(__file__).resolve().parents[1]
GM = (ROOT/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text(encoding='utf-8')
MODELS = (ROOT/'Assets/Scripts/Core/AscendanceModels.cs').read_text(encoding='utf-8')
HUD = (ROOT/'Assets/Scripts/UI/AscendanceHUD.cs').read_text(encoding='utf-8')
SAVE_DATA = (ROOT/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text(encoding='utf-8')
SAVE_SYS = (ROOT/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text(encoding='utf-8')
HELPER11 = (ROOT/'Assets/Editor/TurnLifecycleTestTools0811.cs').read_text(encoding='utf-8')
HELPER12 = (ROOT/'Assets/Editor/RoundTransitionTestTools0812.cs').read_text(encoding='utf-8')
RUNTIME = (ROOT/'Assets/Scripts/Core/HeroRulesRuntime.cs').read_text(encoding='utf-8')
roster = json.loads((ROOT/'Assets/Resources/Ascendance/Data/hero_roster.json').read_text())['heroes']
cards = json.loads((ROOT/'Assets/Resources/Ascendance/Data/hero_cards.json').read_text())['cards']

results=[]
def check(label, cond): results.append((label, bool(cond)))

def method_body(text, signature):
    i=text.find(signature)
    if i<0: return ''
    b=text.find('{',i)
    if b<0: return ''
    depth=0; in_str=False; esc=False; verb=False; ch=False; line=False; block=False
    j=b
    while j<len(text):
        c=text[j]; n=text[j+1] if j+1<len(text) else ''
        if line:
            if c=='\n': line=False
            j+=1; continue
        if block:
            if c=='*' and n=='/': block=False; j+=2; continue
            j+=1; continue
        if in_str:
            if verb:
                if c=='"' and n=='"': j+=2; continue
                if c=='"': in_str=False; verb=False
            else:
                if esc: esc=False
                elif c=='\\': esc=True
                elif c=='"': in_str=False
            j+=1; continue
        if ch:
            if esc: esc=False
            elif c=='\\': esc=True
            elif c=="'": ch=False
            j+=1; continue
        if c=='/' and n=='/': line=True; j+=2; continue
        if c=='/' and n=='*': block=True; j+=2; continue
        if c=='@' and n=='"': in_str=True; verb=True; j+=2; continue
        if c=='"': in_str=True; j+=1; continue
        if c=="'": ch=True; j+=1; continue
        if c=='{': depth+=1
        elif c=='}':
            depth-=1
            if depth==0: return text[b:j+1]
        j+=1
    return ''

def strip_comments_strings(text):
    return re.sub(r'//.*?$|/\*.*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '', text, flags=re.M|re.S)

def balanced(text,left,right):
    x=strip_comments_strings(text)
    return x.count(left)==x.count(right)

start=method_body(GM,'public bool TryStartHeroTurn(HeroState hero)')
end=method_body(GM,'public void EndHeroTurn()')
resolve=method_body(GM,'private IEnumerator ResolveRound()')
endreserve=method_body(GM,'private void ResolveEndPhaseReactionReserves()')
resetend=method_body(GM,'private void ResetHeroAfterEndPhase(HeroState hero)')
nextphase=method_body(GM,'private void BeginNextHeroPhase()')
use=method_body(GM,'public void UseReaction(string heroId,string cardId)')
popattack=method_body(GM,'private void PopulateBeforeReactionOptions()')
popdamage=method_body(GM,'private void PopulateBeforeHeroDamageReactionOptions()')
postattack=method_body(GM,'private void PreparePostAttackReactionWindow(EnemyState enemy,HeroState target,int dealt)')
killreact=method_body(GM,'private void PrepareHeroKillReactionWindow(HeroState killer,EnemyState defeatedEnemy)')
status=method_body(GM,'private IEnumerator ResolveEndRoundStatuses()')
load=method_body(GM,'public void ApplySaveData(AscendanceSaveData save)')

check('BuildVersion is 0.8.12', 'BuildVersion = "0.8.12"' in GM)
check('0.8.12 startup marker present', 'Round Transition & Reaction Reserve Enforcement loaded' in GM)
check('16 authored Heroes', len(roster)==16)
check('323 Hero-card definitions', len(cards)==323)
by={h['id']:0 for h in roster}
for c in cards: by[c['heroId']]=by.get(c['heroId'],0)+int(c.get('copies',1))
check('every personal deck totals exactly 30 cards', len(by)==16 and all(v==30 for v in by.values()))
check('HeroState defaults to 0 Actions', re.search(r'public int Actions\s*=\s*0\s*;',MODELS) is not None)
check('Start Turn grants 3 Actions', 'hero.Actions = 3;' in start)
check('Round 1 opening hand still avoids duplicate draw', 'keeps the 5-card starting hand' in start)
check('Round 2+ draw still occurs at Start Turn', 'DrawCards(hero, hero.DrawValue);' in start)
check('End Turn preserves general Actions', 'int saved=GeneralActions(hero);' in end and 'saved for legal Reactions' in end)
check('End Turn strips attack-only Actions', 'hero.AttackOnlyActions=0;' in end)
check('Reaction-specific payment gate exists', 'private bool CanPayReactionCost' in GM and 'card.Kind == CardKind.Reaction' in GM)
check('Reaction-specific gate excludes defeated/nonparticipating Heroes', 'hero.Participating && !hero.IsDefeated' in GM)
check('pre-Attack Reaction population uses reaction reserve gate', 'CanPayReactionCost' in popattack)
check('damage Reaction population uses reaction reserve gate', 'CanPayReactionCost' in popdamage)
check('post-Attack Reaction population uses reaction reserve gate', 'CanPayReactionCost' in postattack)
check('post-kill Reaction population uses reaction reserve gate', 'CanPayReactionCost' in killreact)
check('UseReaction revalidates held reserve before spending', 'CanPayReactionCost' in use)
check('UseReaction logs held Action transaction', 'REACTION ACTION' in use and 'heldBefore' in use)
check('Reaction cost is spent exactly once in UseReaction', use.count('SpendCardCost(hero,card,cost)')==1)
check('End-Phase reserve reset is explicit', 'ResolveEndPhaseReactionReserves' in resolve)
check('End-Phase reset occurs after status resolution', resolve.find('ResolveEndRoundStatuses') < resolve.find('ResolveEndPhaseReactionReserves'))
check('held Actions survive into End-Phase statuses', 'BeginHeroDamage' in status and resolve.find('ResolveEndRoundStatuses') < resolve.find('ResolveEndPhaseReactionReserves'))
check('remaining held general Actions are explicitly logged on expiry', 'loses {held} unspent Reaction Action' in resetend)
check('End Phase zeros general Actions', 'hero.Actions=0' in resetend)
check('End Phase zeros attack-only Actions', 'hero.AttackOnlyActions=0' in resetend)
check('End Phase resets TurnCompleted', 'hero.TurnCompleted=false' in resetend)
check('End Phase resets round runtime after reserve expiry', 'HeroRulesRuntime.ResetRound(hero);' in resetend)
check('all Hero reserves expire through one canonical method', 'foreach(var hero in ParticipatingHeroes)ResetHeroAfterEndPhase(hero)' in endreserve)
check('next Hero Phase increments Round exactly in canonical method', 'Round++;' in nextphase)
check('next Hero Phase has no TurnHero', 'TurnHeroId=null;' in nextphase)
check('next Hero Phase enforces 0 Actions', 'hero.Actions=0' in nextphase and 'hero.AttackOnlyActions=0' in nextphase)
check('next Hero Phase repairs stale TurnCompleted', 'hero.TurnCompleted=false' in nextphase and 'ROUND STATE REPAIR' in nextphase)
check('new Round log states all Heroes await Start Turn at 0 Actions', 'All Heroes await Start Turn at 0 Actions' in nextphase)
check('ResolveRound no longer draws cards party-wide', 'DrawCards(' not in resolve)
check('ResolveRound calls next Hero Phase only after End Phase reserve expiry', resolve.find('ResolveEndPhaseReactionReserves') < resolve.find('BeginNextHeroPhase'))
check('0.8.10 Enemy Attack damage pipeline preserved', 'HeroDamageSourceKind.EnemyAttack' in GM and 'ENEMY ATTACK ROLL' in GM)
check('0.8.9 Interpose retarget preserved', 'RetargetPendingEnemyAttack' in GM and 'become the target' in GM)
check('save/load TurnHero preservation remains present', 'save.TurnHeroId' in load and 'TurnHeroId = th.Id;' in load)
check('load still zeroes unfinished non-turn Heroes', '!hero.TurnCompleted && !IsHeroTakingTurn(hero)' in load and 'hero.Actions = 0;' in load)
check('HUD still has Start Turn and End Turn', '"START TURN"' in HUD and '"END TURN"' in HUD)
check('0.8.11 lifecycle helper retained', 'Test 0.8.11 - Hero Turn Start Lifecycle' in HELPER11)
check('0.8.12 Play Mode test helper present', 'Test 0.8.12 - Round Transition + Reaction Reserve' in HELPER12)
check('0.8.12 helper requires beginning-of-Hero-Phase state', 'beginning of a Hero Phase' in HELPER12)
check('0.8.12 helper stages Captain-only World Phase', 'only the Captain is alive' in HELPER12 and 'enemy.Id == "Captain"' in HELPER12)
check('0.8.12 helper gives Ignore the Pain', 'H012-013' in HELPER12 and 'Ignore the Pain' in HELPER12)
check('0.8.12 helper verifies saved Actions survive into World Phase', 'Held Actions are still available during World Phase' in HELPER12)
check('0.8.12 helper spends a legal held Action', 'gm.UseReaction("Garruk", "H012-013")' in HELPER12 and '3 → 2' in HELPER12)
check('0.8.12 helper verifies End Phase clears all Actions', 'End Phase clears every remaining held Action reserve' in HELPER12)
check('0.8.12 helper verifies no round-wide draw', 'Round transition does not draw cards' in HELPER12)
check('0.8.12 helper verifies Round 2+ draw at individual Start Turn', 'Round 2+ Draw Step occurs only when Garruk starts his own turn' in HELPER12)
check('0.8.12 helper verifies second Hero also draws at own Start Turn', 'draws exactly their Draw Value at their own Start Turn' in HELPER12)
check('HeroRulesRuntime keeps ResetTurn and ResetRound separate', 'public static void ResetTurn(HeroState hero)' in RUNTIME and 'public static void ResetRound(HeroState hero)' in RUNTIME)
check('Save DTO retains TurnHeroId', 'public string TurnHeroId;' in SAVE_DATA)
check('Save capture retains TurnHeroId', 'TurnHeroId=gm.TurnHeroId' in SAVE_SYS)

files=[
 ('AscendanceGameManager.cs',GM),('AscendanceModels.cs',MODELS),('AscendanceHUD.cs',HUD),
 ('AscendanceSaveData.cs',SAVE_DATA),('AscendanceSaveSystem.cs',SAVE_SYS),
 ('TurnLifecycleTestTools0811.cs',HELPER11),('RoundTransitionTestTools0812.cs',HELPER12),('HeroRulesRuntime.cs',RUNTIME)
]
for name,text in files:
    check(name+' brace balance',balanced(text,'{','}'))
    check(name+' parenthesis balance',balanced(text,'(',')'))
    check(name+' bracket balance',balanced(text,'[',']'))

out=[('PASS' if ok else 'FAIL')+' — '+label for label,ok in results]
out += ['', f'SUMMARY — {sum(ok for _,ok in results)}/{len(results)} static checks passed.']
text='\n'.join(out)+'\n'
print(text,end='')
(ROOT/'VALIDATION_0.8.12.txt').write_text(text,encoding='utf-8')
(ROOT/'tests/verification-0812.txt').write_text(text,encoding='utf-8')
sys.exit(0 if all(ok for _,ok in results) else 1)
