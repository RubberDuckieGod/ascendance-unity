from pathlib import Path
import re,sys
root=Path(__file__).resolve().parents[1]
gm=(root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text()
rt=(root/'Assets/Scripts/Core/SearchEquipmentRuntime0827.cs').read_text()
models=(root/'Assets/Scripts/Core/AscendanceModels.cs').read_text()
save=(root/'Assets/Scripts/Shell/AscendanceSaveData.cs').read_text()
saver=(root/'Assets/Scripts/Shell/AscendanceSaveSystem.cs').read_text()
hud=(root/'Assets/Scripts/UI/AscendanceHUD.cs').read_text()
test=(root/'Assets/Editor/SearchEquipmentManagementTestTools0827.cs').read_text()
reward=(root/'Assets/Scripts/Core/RewardContent0826.cs').read_text()
content=(root/'Assets/Scripts/Core/SearchEncounterRuntime.cs').read_text()
checks=[]
def ck(label,cond):checks.append((label,bool(cond)))
ck('BuildVersion 0.8.27','public const string BuildVersion = "0.8.27";' in gm)
ck('0.8.27 load banner','Search Rewards & Equipment Management loaded.' in gm)
ck('manager is partial','public sealed partial class AscendanceGameManager' in gm)
ck('0.8.27 runtime partial exists','ResolveSearchReward0827' in rt and 'EquipAcquiredEquipment' in rt)
for cid in ['SD001','SD003','SD004','SD007','SD008','SD009','SD011','SD012','SD014','SD017','SD018','SD019']:
    ck('real Search reward '+cid,f'case "{cid}"' in rt)
ck('SD001 real Loot','case "SD001": DrawLootToHero' in rt)
ck('SD003 Loot then cost choice','SearchDiscardHandOrLose5' in rt and 'Lose 5 Gold' in rt)
ck('SD004 Gold/Loot choice','SearchGold20OrLoot1' in rt)
ck('SD007 Gold plus Loot','case "SD007": AddPartyGold(30' in rt and 'DrawLootToHero' in rt)
ck('SD008 heal plus Loot','case "SD008": HealHero' in rt and 'DrawLootToHero' in rt)
ck('SD009 holds exact Equipment','SearchEquipmentKeepGate' in rt and 'TakeTopEquipmentForChoice' in rt)
ck('SD009 rarity gate','IsEquipmentRarity(id,"Common","Uncommon")' in rt and 'BOTTOM25' in rt)
ck('SD012 top-two Loot choice','SearchKeepOneLoot2' in rt and 'DiscardHeldLoot' in rt)
ck('SD014 Loot Gold Encounter chain','case "SD014"' in rt and 'DrawLootToHero(hero,card.Name);AddPartyGold(25' in rt and 'ResolveTravelEncounter(location,hero' in rt)
ck('SD017 Gold/Common Equipment choice','SearchGold30OrCommonEquipment' in rt)
ck('SD018 real Loot','case "SD018": DrawLootToHero' in rt)
ck('SD019 top-two Equipment choice','SearchArmory2' in rt and 'qualifying.Count==0' in rt and 'qualifying.Count==1' in rt)
ck('held Loot removed from live deck','LootDeck.RemoveAt(0);return id;' in rt)
ck('held Equipment removed from live deck','EquipmentDeck.RemoveAt(0);return id;' in rt)
ck('held Loot discard enters discard','LootDiscard.Add(id)' in rt)
ck('held Equipment bottom returns to deck','EquipmentDeck.Add(id)' in rt)
ck('Search resolver delegates 0.8.27','if(ResolveSearchReward0827(card,location,hero))return;' in gm)
ck('reward choice holds revealed cards','RevealedCardIds = new()' in models)
ck('reward choice save holds revealed cards','RevealedCardIds = new()' in save and 'RevealedCardIds=gm.PendingRewardChoice.RevealedCardIds.ToList()' in saver)
ck('reward choice restore holds revealed cards','save.PendingRewardChoice.RevealedCardIds' in gm)
ck('equipped loadout save field','EquippedEquipmentIds = new()' in save)
ck('equipped loadout captured physically','SelectMany(e=>Enumerable.Repeat(e.Id,Mathf.Max(1,e.Copies)))' in saver)
ck('equipped loadout restored','RestoreEquippedLoadout(hero,stored.EquippedEquipmentIds)' in gm)
ck('loadout stats recalc','RecalculateHeroEquipmentStats' in rt and 'hero.BaseAttack+weaponBonus' in rt and 'hero.BaseArmor+armorBonus' in rt)
ck('Kael unarmed fallback','string.Equals(hero.Id,"Kael"' in rt and 'weaponBonus+=2' in rt)
ck('two-handed shield handling','WeaponOccupiesBothHands' in rt and 'occupies both hands' in rt)
ck('replaced gear returns inventory','hero.AcquiredEquipment.Add(old.Id)' in rt)
ck('accessory replacement choice','AccessoryCapacity' in rt and 'replaceAccessoryIndex' in rt and 'EQUIP{i}R{r}' in rt)
ck('Seraphine one accessory capacity','hero.Id,"Seraphine"' in rt and '?1:2' in rt)
ck('equipment management runtime','BeginEquipmentManagement' in rt and 'ManageEquipment' in rt)
ck('equipment management close','key=="CLOSE"' in rt and 'closes the loadout screen' in rt)
ck('gear open gating','CanOpenEquipmentManagement' in rt and '!IsResolving' in rt)
ck('gear HUD button','equipmentButton' in hud and 'BeginEquipmentManagement' in hud and '"GEAR"' in hud)
ck('HUD equipped summary','DebugEquippedLoadout' in hud and 'Equipped:' in hud)
ck('HUD Equipment modal title','DebugPendingRewardChoiceMode()=="ManageEquipment"?"EQUIPMENT' in hud)
ck('reward options include 0.8.27','Append0827RewardChoiceOptions(options,state)' in gm)
ck('reward resolution includes 0.8.27','TryResolve0827RewardChoice(state,hero,key' in gm)
ck('debug Search resolver exists','DebugResolveSearchCard0827' in rt)
ck('Play Mode menu','Test 0.8.27 - Search Rewards & Equipment Management (Play Mode)' in test)
for phrase in ['SD004 opens its real 20 Gold or 1 Loot choice','SD001 draws the actual top Loot','SD003 draws Loot then opens','SD009 holds the exact drawn Equipment','SD009 high-rarity Equipment cannot be kept','SD012 removes the actual top two Loot','SD017 opens the authored 30 Gold','SD019 reveals two qualifying Equipment','SD019 bottoms both ineligible','Acquired Equipment can be equipped','Kael\'s Unarmed +2 does not stack','GEAR button runtime opens','Search Rewards & Equipment Management completed']:
    ck('validator '+phrase,phrase in test)
# Existing authored data still intact.
ck('190 Loot rows',reward.count('AddLoot("LT')==190)
ck('144 Equipment rows',reward.count('AddEquipment("EQ')==144)
ck('20 Search cards',sum(1 for n in range(1,21) if f'"SD{n:03d}"' in content)==20)
# Compile-safety scanner: raw multiline normal strings + delimiter balance after comments/strings stripped.
def stripped(text):
    out=[];i=0;mode='code';esc=False
    while i<len(text):
        c=text[i];n=text[i+1] if i+1<len(text) else ''
        if mode=='code':
            if c=='/' and n=='/':mode='line';out.extend('  ');i+=2;continue
            if c=='/' and n=='*':mode='block';out.extend('  ');i+=2;continue
            if c=='@' and n=='"':mode='vstr';out.extend('  ');i+=2;continue
            if c=='"':mode='str';out.append(' ');i+=1;continue
            if c=="'":mode='char';out.append(' ');i+=1;continue
            out.append(c);i+=1;continue
        if mode=='line':
            if c=='\n':mode='code';out.append('\n')
            else:out.append(' ')
            i+=1;continue
        if mode=='block':
            if c=='*' and n=='/':mode='code';out.extend('  ');i+=2
            else:out.append('\n' if c=='\n' else ' ');i+=1
            continue
        if mode in ('str','char'):
            end='"' if mode=='str' else "'"
            if esc:esc=False
            elif c=='\\':esc=True
            elif c==end:mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1;continue
        if mode=='vstr':
            if c=='"' and n=='"':out.extend('  ');i+=2;continue
            if c=='"':mode='code'
            out.append('\n' if c=='\n' else ' ');i+=1;continue
    return ''.join(out)
csfiles=list((root/'Assets/Scripts').rglob('*.cs'))+list((root/'Assets/Editor').rglob('*.cs'))
ck('deprecated FindObjectsSortMode absent',all('FindObjectsSortMode' not in f.read_text(errors='ignore') for f in csfiles))
for rel in csfiles:
    text=rel.read_text(errors='ignore');in_str=False;verb=False;esc=False;bad=[];line=1;i=0
    while i<len(text):
        c=text[i]
        if c=='\n':
            if in_str and not verb:bad.append(line)
            line+=1
        if not in_str:
            if c=='"':verb=(i>0 and text[i-1]=='@');in_str=True;esc=False
        elif verb:
            if c=='"':
                if i+1<len(text) and text[i+1]=='"':i+=1
                else:in_str=False;verb=False
        else:
            if esc:esc=False
            elif c=='\\':esc=True
            elif c=='"':in_str=False
        i+=1
    ck('no raw newline '+str(rel.relative_to(root)),not bad)
    clean=stripped(text)
    for op,cl in [('(',')'),('[',']'),('{','}')]:
        bal=0;ok=True
        for c in clean:
            if c==op:bal+=1
            elif c==cl:
                bal-=1
                if bal<0:ok=False;break
        ck(f'balanced {op}{cl} {rel.relative_to(root)}',ok and bal==0)
method_re=re.compile(r'(?P<head>(?:public|private|protected|internal)[^\n{;]*\bIEnumerator\s+\w+\s*\([^)]*\)\s*)\{',re.M)
for rel in csfiles:
    text=rel.read_text(errors='ignore');clean=stripped(text)
    for m in method_re.finditer(clean):
        start=m.end()-1;depth=0;end=None
        for i in range(start,len(clean)):
            if clean[i]=='{':depth+=1
            elif clean[i]=='}':
                depth-=1
                if depth==0:end=i+1;break
        body=text[start:end] if end else ''
        name=re.search(r'IEnumerator\s+(\w+)',m.group('head')).group(1)
        ck(f'IEnumerator has yield {rel.relative_to(root)}::{name}','yield return' in body or 'yield break' in body)
failed=[x for x in checks if not x[1]]
for label,ok in checks:print(('PASS' if ok else 'FAIL')+' — '+label)
print(f'\n{len(checks)-len(failed)}/{len(checks)} checks passed.')
if failed:sys.exit(1)
