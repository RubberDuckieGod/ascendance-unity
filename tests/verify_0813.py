#!/usr/bin/env python3
from pathlib import Path
import json, re, sys

ROOT=Path(__file__).resolve().parents[1]
read=lambda p:(ROOT/p).read_text(encoding='utf-8')
GM=read(Path('Assets/Scripts/Core/AscendanceGameManager.cs'))
MODELS=read(Path('Assets/Scripts/Core/AscendanceModels.cs'))
HUD=read(Path('Assets/Scripts/UI/AscendanceHUD.cs'))
AVATAR=read(Path('Assets/Scripts/World/HeroAvatar.cs'))
SAVE_DATA=read(Path('Assets/Scripts/Shell/AscendanceSaveData.cs'))
SAVE_SYS=read(Path('Assets/Scripts/Shell/AscendanceSaveSystem.cs'))
RUNTIME=read(Path('Assets/Scripts/Core/HeroRulesRuntime.cs'))
HELPER11=read(Path('Assets/Editor/TurnLifecycleTestTools0811.cs'))
HELPER12=read(Path('Assets/Editor/RoundTransitionTestTools0812.cs'))
HELPER13=read(Path('Assets/Editor/DefeatRevivalTestTools0813.cs'))
roster=json.loads(read(Path('Assets/Resources/Ascendance/Data/hero_roster.json')))['heroes']
cards=json.loads(read(Path('Assets/Resources/Ascendance/Data/hero_cards.json')))['cards']

results=[]
def check(label,cond): results.append((label,bool(cond)))

def method_body(text, signature):
    i=text.find(signature)
    if i<0:return ''
    b=text.find('{',i)
    if b<0:return ''
    depth=0; ins=False; esc=False; verb=False; ch=False; line=False; block=False
    j=b
    while j<len(text):
        c=text[j]; n=text[j+1] if j+1<len(text) else ''
        if line:
            if c=='\n': line=False
            j+=1; continue
        if block:
            if c=='*' and n=='/': block=False; j+=2; continue
            j+=1; continue
        if ins:
            if verb:
                if c=='"' and n=='"': j+=2; continue
                if c=='"': ins=False; verb=False
            else:
                if esc: esc=False
                elif c=='\\': esc=True
                elif c=='"': ins=False
            j+=1; continue
        if ch:
            if esc: esc=False
            elif c=='\\': esc=True
            elif c=="'": ch=False
            j+=1; continue
        if c=='/' and n=='/': line=True; j+=2; continue
        if c=='/' and n=='*': block=True; j+=2; continue
        if c=='@' and n=='"': ins=True; verb=True; j+=2; continue
        if c=='"': ins=True; j+=1; continue
        if c=="'": ch=True; j+=1; continue
        if c=='{': depth+=1
        elif c=='}':
            depth-=1
            if depth==0:return text[b:j+1]
        j+=1
    return ''

def strip_comments_strings(text):
    return re.sub(r'//.*?$|/\*.*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '', text, flags=re.M|re.S)
def balanced(text,l,r):
    x=strip_comments_strings(text); return x.count(l)==x.count(r)

start=method_body(GM,'public bool TryStartHeroTurn(HeroState hero)')
end=method_body(GM,'public void EndHeroTurn()')
resolve=method_body(GM,'private IEnumerator ResolveRound()')
use=method_body(GM,'public void UseReaction(string heroId,string cardId)')
applydmg=method_body(GM,'private int ApplyHeroDamageNow(HeroState target')
begindmg=method_body(GM,'private bool BeginHeroDamage(HeroState target')
zero=method_body(GM,'private void HandleHeroAtZero(HeroState hero)')
fail=method_body(GM,'private void EvaluatePartyDefeat()')
hastarget=method_body(GM,'public bool HasLegalHealingPotionTarget(HeroState actor)')
canpotion=method_body(GM,'public bool CanUseHealingPotionOn(HeroState actor,HeroState target)')
potion=method_body(GM,'public bool TryUseHealingPotionOn(string targetHeroId)')
heal=method_body(GM,'public void HealHero(HeroState target,int amount,string source,HeroState sourceHero=null)')
load=method_body(GM,'public void ApplySaveData(AscendanceSaveData save)')
checkvictory=method_body(GM,'private void CheckVictory()')

check('BuildVersion is 0.8.13','BuildVersion = "0.8.13"' in GM)
check('0.8.13 startup marker present','Downed / Dead / Revival Enforcement loaded' in GM)
check('16 authored Heroes',len(roster)==16)
check('323 Hero-card definitions',len(cards)==323)
by={h['id']:0 for h in roster}
for c in cards: by[c['heroId']]=by.get(c['heroId'],0)+int(c.get('copies',1))
check('every personal deck totals exactly 30 cards',len(by)==16 and all(v==30 for v in by.values()))
check('Hero IsDefeated includes Downed Dead or 0 Health','Downed || Dead || Health <= 0' in MODELS)

# 0.8.11/0.8.12 lifecycle preservation
check('Start Turn still rejects Defeated Heroes','public bool CanStartHeroTurn(HeroState hero) => hero != null && hero.Participating && !hero.IsDefeated' in GM)
check('Start Turn still grants exactly 3 Actions','hero.Actions = 3;' in start)
check('Round 1 still keeps five-card opening hand','keeps the 5-card starting hand' in start)
check('Round 2+ still draws at individual Start Turn','DrawCards(hero, hero.DrawValue);' in start)
check('End Turn still preserves general Actions','int saved=GeneralActions(hero);' in end and 'saved for legal Reactions' in end)
check('World/End round pipeline retained','ResolveEndRoundStatuses' in resolve and 'ResolveEndPhaseReactionReserves' in resolve and 'BeginNextHeroPhase' in resolve)
check('0.8.11 runtime helper retained','Test 0.8.11 - Hero Turn Start Lifecycle' in HELPER11)
check('0.8.12 runtime helper retained','Test 0.8.12 - Round Transition + Reaction Reserve' in HELPER12)
check('0.8.10 EnemyAttack damage source retained','HeroDamageSourceKind.EnemyAttack' in GM and 'ENEMY ATTACK ROLL' in GM)
check('0.8.9 Interpose retarget retained','RetargetPendingEnemyAttack' in GM and 'become the target' in GM)

# Defeat classification
check('damage ignores already Defeated Hero targets','target.IsDefeated' in applydmg)
check('queued/general damage refuses already Defeated targets','target.IsDefeated' in begindmg)
check('fatal damage classifies before Hero damage ability activation',applydmg.find('HandleHeroAtZero(target)') < applydmg.find('HeroRulesRuntime.OnHeroDamaged'))
check('fatal damage abilities blocked once Hero is Defeated','dealt > 0 && !target.IsDefeated' in applydmg)
check('HandleHeroAtZero clamps Health to zero','hero.Health=0;' in zero)
check('Downed/Dead classification is sticky','if(hero.Downed||hero.Dead)return;' in zero)
check('Downed requires same-Location non-Defeated ally','!h.IsDefeated&&h.LocationId==hero.LocationId' in zero)
check('classification sets Downed with ally and Dead without','hero.Downed=ally;hero.Dead=!ally;' in zero)
check('Dead clears general Actions','hero.Dead' in zero and 'hero.Actions=0' in zero)
check('Dead clears attack-only Actions','hero.Dead' in zero and 'hero.AttackOnlyActions=0' in zero)
check('Downed path does not zero Actions',zero.find('hero.Actions=0') > zero.find('if(hero.Dead)'))
check('defeat classification evaluates party defeat','EvaluatePartyDefeat();' in zero)

# All-Heroes defeat gate
check('QuestFailed state exists','public bool QuestFailed { get; private set; }' in GM)
check('party failure is blocked after Quest completion','QuestComplete||QuestFailed' in fail)
check('party failure requires all participating Heroes Defeated','ParticipatingHeroes.All(h=>h.IsDefeated)' in fail)
check('all-Heroes defeat logs Q001 failure','Q001 FAILED — all participating Heroes are Defeated.' in fail)
check('victory cannot resolve after Quest failure','QuestComplete||QuestFailed' in checkvictory)
check('HUD displays QUEST FAILED','QUEST FAILED' in HUD)

# Reaction/action lockout
check('normal turn actions still exclude Defeated Heroes','public bool CanUseTurnActions(HeroState hero) => hero != null && hero.Participating && !hero.IsDefeated' in GM)
check('reaction cost gate excludes Defeated Heroes','hero.Participating && !hero.IsDefeated && GeneralActions(hero) >= cost' in GM)
check('UseReaction hard-rejects Defeated Hero','hero.IsDefeated' in use and 'cannot perform Reactions' in use)
check('special Shield Wall branch occurs after Defeated guard',use.find('hero.IsDefeated') < use.find('ABILITY_SHIELD_WALL'))

# Potion/revival transaction
check('legal potion target helper exists','injured' not in hastarget.lower() or 'h.Health<h.MaxHealth' in hastarget)
check('active TurnHero remains the potion actor while another Hero portrait is selected','var actor=TurnHero??SelectedHero;' in potion)
check('potion target rejects Dead Heroes','!target.Dead' in canpotion)
check('potion actor rejects Defeated Heroes','!actor.IsDefeated' in canpotion)
check('potion requires actor general Action','CanUseStandardAction(actor)' in canpotion)
check('potion requires same Location','actor.LocationId==target.LocationId' in canpotion)
check('potion target must be injured or Downed','target.Downed||target.Health<target.MaxHealth' in canpotion)
check('normal potion cannot revive Dead target','target.Dead' in potion and 'normal Healing Potion cannot return a Dead Hero' in potion)
check('blocked Dead-target potion happens before Action spend',potion.find('target.Dead') < potion.find('actor.Actions--'))
check('revive potion spends exactly one Action','actor.Actions--' in potion)
check('Mira Field Engineer preserved','FieldEngineer' in potion and 'Potion preserved' in potion)
check('non-preserved potion is consumed','actor.HealingPotions--' in potion)
check('Common Potion heals exactly 4','HealHero(target,4,"Common Healing Potion",actor);' in potion)
check('default potion targeting prioritizes Downed','OrderByDescending(h=>h.Downed)' in method_body(GM,'public void UseHealingPotion()'))
check('default potion supports injured ally targeting','h.Health<h.MaxHealth' in method_body(GM,'public void UseHealingPotion()'))
check('HUD potion button labels selected-Hero targeting','Potion / Revive\n→ Selected Hero' in HUD)
check('HUD uses TurnHero as potion actor while selected Hero is the target','var potionActor=turnHero??hero' in HUD)
check('HUD potion enabled only for selected legal target','CanUseHealingPotionOn(potionActor,hero)' in HUD)

# Healing semantics
check('normal healing cannot heal Dead Heroes','target.Dead' in heal)
check('HealHero remembers prior Downed state','bool wasDowned=target.Downed;' in heal)
check('healing above 0 clears Downed','target.Downed=false;' in heal)
check('revival logs no Action grant','Revival grants no Actions' in heal)
check('revival does not alter TurnCompleted','TurnCompleted' not in heal)
check('revival does not alter Actions','target.Actions' not in heal)
check('revive visual fires after legal recovery','ReviveVisual' in heal)

# Save/load invariants
check('QuestFailed is persisted in save DTO','public bool QuestFailed;' in SAVE_DATA)
check('QuestFailed is captured','QuestFailed=gm.QuestFailed' in SAVE_SYS)
check('load never lets failure override completion','QuestFailed = !QuestComplete && save.QuestFailed;' in load)
check('load repair forces Dead Health to zero','hero.Dead' in load and 'hero.Health = 0;' in load)
check('load repair makes Dead not Downed','hero.Downed = false;' in load)
check('load repair clamps Downed Health to zero','else if (hero.Downed)' in load and 'hero.Health = 0;' in load)
check('legacy zero-Health save is classified','else if (hero.Health <= 0)' in load and 'hero.Downed = ally;' in load and 'hero.Dead = !ally;' in load)
check('load repair marks all-Heroes-defeated Quest failed','SAVE STATE REPAIR — all participating Heroes are Defeated' in load)

# Visual/UI defeated state
check('Hero avatar selection ring hides for any Defeated state','selected && !hero.IsDefeated' in AVATAR)
check('Hero avatar hover ring hides for any Defeated state','hovered && !hero.IsDefeated' in AVATAR)
check('Hero avatar health bar hides for any Defeated state','healthBar?.SetVisible(!hero.IsDefeated)' in AVATAR)
check('HUD distinguishes DOWNED and DEAD','DOWNED' in HUD and 'DEAD' in HUD)

# 0.8.13 runtime helper coverage
check('0.8.13 Play Mode helper present','Test 0.8.13 - Downed Dead Revival' in HELPER13)
check('helper requires at least three Heroes','party.Count < 3' in HELPER13)
check('helper checks Downed classification','becomes Downed (not Dead)' in HELPER13)
check('helper checks Downed cannot Start Turn','A Downed Hero cannot Start Turn.' in HELPER13)
check('helper checks one Downed does not fail Quest','One Downed Hero does not fail the Quest' in HELPER13)
check('helper checks sticky Downed state','remains Downed after the witnessing ally leaves' in HELPER13)
check('helper checks Downed self-potion is blocked','cannot activate their own Healing Potion' in HELPER13)
check('helper checks blocked potion consumes nothing','consumes no Potion' in HELPER13)
check('helper checks revive costs one Action','Revive costs exactly 1 general Action' in HELPER13)
check('helper checks potion is consumed','Common Healing Potion is consumed by the revive' in HELPER13)
check('helper checks 4-Health revive','revives the Downed Hero at 4 Health' in HELPER13)
check('helper checks revival grants no Actions','Revival itself grants no Actions' in HELPER13)
check('helper checks unstarted revived Hero can later Start Turn','may later Start Turn normally' in HELPER13)
check('helper checks held Actions survive Downed','preserves already-unspent held Actions' in HELPER13)
check('helper checks healing clears Downed','Healing above 0 immediately ends Downed state' in HELPER13)
check('helper checks completed turn not reset','Revival does not reset a Hero\'s completed-turn state' in HELPER13)
check('helper checks no second normal turn','cannot take a second normal turn' in HELPER13)
check('helper checks Dead classification','becomes Dead when no non-Defeated ally shares' in HELPER13)
check('helper checks Dead has no reserve','Dead Heroes retain no Action reserve' in HELPER13)
check('helper checks normal healing cannot revive Dead','Normal healing cannot revive a Dead Hero' in HELPER13)
check('helper checks all-Heroes failure gate','Q001 fails only when all participating Heroes are Defeated' in HELPER13)

# Structural checks
files=[
 ('AscendanceGameManager.cs',GM),('AscendanceModels.cs',MODELS),('AscendanceHUD.cs',HUD),('HeroAvatar.cs',AVATAR),
 ('AscendanceSaveData.cs',SAVE_DATA),('AscendanceSaveSystem.cs',SAVE_SYS),('HeroRulesRuntime.cs',RUNTIME),
 ('TurnLifecycleTestTools0811.cs',HELPER11),('RoundTransitionTestTools0812.cs',HELPER12),('DefeatRevivalTestTools0813.cs',HELPER13)
]
for name,text in files:
    check(name+' brace balance',balanced(text,'{','}'))
    check(name+' parenthesis balance',balanced(text,'(',')'))
    check(name+' bracket balance',balanced(text,'[',']'))

out=[('PASS' if ok else 'FAIL')+' — '+label for label,ok in results]
out+=['',f'SUMMARY — {sum(ok for _,ok in results)}/{len(results)} static checks passed.']
text='\n'.join(out)+'\n'
print(text,end='')
(ROOT/'VALIDATION_0.8.13.txt').write_text(text,encoding='utf-8')
(ROOT/'tests/verification-0813.txt').write_text(text,encoding='utf-8')
sys.exit(0 if all(ok for _,ok in results) else 1)
