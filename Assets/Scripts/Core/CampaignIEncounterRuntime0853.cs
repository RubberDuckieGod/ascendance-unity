using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.53 — closes the remaining authored execution gaps in the Campaign I
    // Travel Encounter deck (EN001-EN030). Later-region Encounter cards remain
    // catalogued by 0.8.50 but are intentionally excluded from Stonebrook's live deck.
    public sealed partial class AscendanceGameManager
    {
        private const string Encounter0853MarkerPrefix = "EN0853:";

        public bool IsEncounterCardExecutable0853(string cardId)
        {
            if(string.IsNullOrWhiteSpace(cardId)||cardId.Length!=5||!cardId.StartsWith("EN",StringComparison.OrdinalIgnoreCase))return false;
            return int.TryParse(cardId.Substring(2),out int n)&&n>=1&&n<=30&&SearchEncounterContent0820.EncounterCards.ContainsKey(cardId);
        }

        private bool IsEncounterFailureImplemented0853(string cardId)=>IsEncounterCardExecutable0853(cardId);

        private bool IsEncounterAwaitingChoice0853(EncounterRuntimeState encounter)
        {
            if(encounter==null||PendingRewardChoice==null)return false;
            if(!string.Equals(encounter.CardId,"EN015",StringComparison.OrdinalIgnoreCase))return false;
            return PendingRewardChoice.Mode?.StartsWith("EncounterToll0853",StringComparison.OrdinalIgnoreCase)==true
                && PendingRewardChoice.RevealedCardIds?.Any(x=>string.Equals(x,encounter.InstanceId,StringComparison.OrdinalIgnoreCase))==true;
        }

        private bool ResolveCampaignIEncounterImmediate0853(EncounterCardDefinition card,string effect,int severity,LocationState location,HeroState hero,EncounterRuntimeState encounter)
        {
            if(card==null||location==null||hero==null||encounter==null)return false;
            switch(card.Id)
            {
                case "EN010":
                    if(severity<=2)return true;
                    if(severity<=4){BeginBrokenBridgePerHeroChoice0853(card,hero);return true;}
                    if(severity<=6){BeginRewardChoice0827(card.Id,card.Name,hero,"EncounterBrokenBridgeSupply0853","Choose: lose 15 Gold in supplies, or each participating Hero suffers 1 damage.");return true;}
                    return false; // Severity 7 combat + Slowed is owned by the existing runtime.

                case "EN011":
                    if(severity<=2){BeginEncounterMerchant0853(card,hero,1,10,false,true);return true;}
                    return false; // Severity 3+ merchant access is earned only after combat clears.

                case "EN014":
                    if(severity<=2){AddLog("ENCOUNTER CHOICE — Bear Territory shows only signs at this Severity; the party avoids the encounter without combat or reward.");return true;}
                    return false;

                case "EN015":
                    if(severity<=6)
                    {
                        int toll=EncounterTollGold0853(severity);
                        BeginRewardChoice0827(card.Id,card.Name,hero,$"EncounterToll0853:{toll}",$"Choose: pay {toll} Gold to pass, or fight the Toll Collectors.",new[]{encounter.InstanceId});
                        return true;
                    }
                    return false; // Severity 7: no payment accepted; existing spawn parser owns combat.

                case "EN019":
                    if(severity>=3&&severity<=4)
                    {
                        AddNextQuestMarker0853(hero,$"EN019_LOST_CHILD:{CurrentQuestId0829}");
                        AddLog($"ENCOUNTER STORY — {hero.DisplayName} may spend 1 Action during the next Quest to search for the Lost Child and gain +1 Reputation and 10 Gold.");
                        return true;
                    }
                    return false;

                case "EN020":
                    ResolveForestShrine0853(card,location,hero,severity);return true;

                case "EN021":
                    if(severity>=5&&severity<=6){BeginFloodedCrossingSupplyChoice0853(card,hero);return true;}
                    return false;

                case "EN029":
                    BeginTravelingMerchant0853(card,hero,severity);return true;

                case "EN030":
                    ResolveQuietRoad0853(card,location,hero,severity);return true;
            }
            return false;
        }

        private bool ResolveCampaignIEncounterReward0853(EncounterRuntimeState encounter)
        {
            if(encounter==null)return false;
            var hero=RewardHeroFor(encounter.SourceHeroId);

            if((encounter.CardId=="EN008"||encounter.CardId=="EN019")&&encounter.HadEnemyLeavePlayUndefeated)
            {
                AddGeneralReputation(-1,$"{encounter.CardId} {encounter.CardName} failed escort/rescue");
                AddLog($"ENCOUNTER FAILURE — {encounter.CardName} ends without a successful rescue; lose 1 General Reputation.");
                return true;
            }

            if(encounter.CardId=="EN011"&&encounter.Severity>=3)
            {
                if(encounter.HadEnemyLeavePlayUndefeated){AddLog("ENCOUNTER REWARD — Merchant Under Guard benefits are lost because a listed Enemy left play without being Defeated.");return true;}
                if(encounter.Severity<=4)BeginEncounterMerchant0853(SearchEncounterContent0820.EncounterCards[encounter.CardId],hero,5,0,false,true);
                else if(encounter.Severity<=6)BeginEncounterMerchant0853(SearchEncounterContent0820.EncounterCards[encounter.CardId],hero,2,20,false,true);
                else BeginEncounterMerchant0853(SearchEncounterContent0820.EncounterCards[encounter.CardId],hero,5,25,false,true);
                return true;
            }

            if(encounter.CardId=="EN013")
            {
                if(encounter.Severity>=3&&encounter.Severity<=4)
                {
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.IsDefeated&&h.LocationId==encounter.LocationId))h.FreeMoveReady=true;
                    AddLog("ENCOUNTER REWARD — Boar Charge cleared; each surviving Hero here may make one free connected Move.");
                }
                return false; // Existing Item-or-20-Gold reward still resolves.
            }

            if(encounter.CardId=="EN015")
            {
                bool fought=encounter.Severity>=7||(encounter.Source?.IndexOf("EN015_FOUGHT",StringComparison.OrdinalIgnoreCase)>=0);
                if(fought)
                {
                    int gold=EncounterTollGold0853(encounter.Severity);
                    AddPartyGold(gold,$"{encounter.CardId} {encounter.CardName} fought reward");
                    AddLog($"ENCOUNTER REWARD — the party fought the Toll Collectors and gains the demanded {gold} Gold instead.");
                }
                return true;
            }
            return false;
        }

        private bool ShouldSuppressEncounterResidue0853(string cardId,int severity)
        {
            if(cardId=="EN011"&&severity>=3)return true;
            if(cardId=="EN013"&&severity>=3&&severity<=4)return true;
            if(cardId=="EN015"&&severity>=7)return true;
            if(cardId=="EN019"&&severity>=5)return true;
            return false;
        }

        private void BeginBrokenBridgePerHeroChoice0853(EncounterCardDefinition card,HeroState sourceHero)
        {
            var ids=ParticipatingHeroes.Where(h=>!h.IsDefeated).OrderBy(h=>h.PartyIndex).Select(h=>h.Id).ToList();
            if(ids.Count==0)return;
            BeginRewardChoice0827(card.Id,card.Name,sourceHero,"EncounterBrokenBridgeHeroes0853","For each Hero choose: +1 Action cost on their next turn, or suffer 1 damage now.",ids);
        }

        private void BeginFloodedCrossingSupplyChoice0853(EncounterCardDefinition card,HeroState sourceHero)
        {
            BeginRewardChoice0827(card.Id,card.Name,sourceHero,"EncounterFloodSupplies0853","Discard 1 carried Item total as rope/supplies, or each participating Hero suffers 1 damage.");
        }

        private void ResolveForestShrine0853(EncounterCardDefinition card,LocationState location,HeroState sourceHero,int severity)
        {
            var here=ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id).OrderBy(h=>h.PartyIndex).ToList();
            if(severity<=2){foreach(var h in here)HealHero(h,1,card.Name,sourceHero);return;}
            if(severity<=4)
            {
                var eligible=here.Where(HasHarmfulCondition0853).Select(h=>h.Id).ToList();
                if(eligible.Count>0)BeginRewardChoice0827(card.Id,card.Name,sourceHero,"EncounterCleanseHero0853","Choose one Hero here to remove 1 negative Condition.",eligible);
                else AddLog("ENCOUNTER RECOVERY — Forest Shrine finds no negative Condition to remove.");
                return;
            }
            foreach(var h in here)HealHero(h,2,card.Name,sourceHero);
            if(severity>=7)
            {
                foreach(var h in here)AddNextQuestMarker0853(h,$"EN020_SHRINE:{CurrentQuestId0829}");
                AddLog("ENCOUNTER BLESSING — each Hero healed by Forest Shrine will begin the next Quest Inspired through its first Round.");
            }
        }

        private void ResolveQuietRoad0853(EncounterCardDefinition card,LocationState location,HeroState sourceHero,int severity)
        {
            var here=ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id).OrderBy(h=>h.PartyIndex).ToList();
            if(severity<=2){foreach(var h in here)HealHero(h,1,card.Name,sourceHero);return;}
            if(severity<=4)
            {
                if(here.Count>0)BeginRewardChoice0827(card.Id,card.Name,sourceHero,"EncounterFreeRestHero0853","Choose one Hero here to Rest without spending an Action.",here.Select(h=>h.Id));
                return;
            }
            if(severity<=6)
            {
                foreach(var h in here)HealHero(h,1,card.Name,sourceHero);
                var eligible=here.Where(HasHarmfulCondition0853).Select(h=>h.Id).ToList();
                if(eligible.Count>0)BeginRewardChoice0827(card.Id,card.Name,sourceHero,"EncounterCleanseHero0853","Choose one Hero here to remove 1 negative Condition.",eligible);
                return;
            }
            foreach(var h in here)HealHero(h,2,card.Name,sourceHero);
            QueueNextTravelEncounterSeverity(1,card.Name);
        }

        private bool HasHarmfulCondition0853(HeroState hero)=>hero?.Statuses?.Any(s=>s!=null&&!s.Beneficial)==true;

        // Recovery is not otherwise represented as a standalone runtime action yet. The current
        // Campaign I digital baseline resolves a Rest as 1 Health recovered, then applies authored
        // Rest bonuses that are already present in carried/equipped content.
        private void ResolveFreeRest0853(HeroState hero,string source)
        {
            if(hero==null||hero.Dead)return;
            int heal=1;
            if(hero.StartingEquipment.Any(e=>string.Equals(e.Id,"EQ099",StringComparison.OrdinalIgnoreCase)))heal++;
            bool rations=hero.LootInventory.Any(id=>string.Equals(id,"LT012",StringComparison.OrdinalIgnoreCase));
            if(rations)heal++;
            bool chalice=ParticipatingHeroes.Any(h=>h.LocationId==hero.LocationId&&h.LootInventory.Any(id=>string.Equals(id,"LT141",StringComparison.OrdinalIgnoreCase)));
            if(chalice)heal++;
            HealHero(hero,heal,source,hero);
            if(rations){int at=hero.LootInventory.FindIndex(id=>string.Equals(id,"LT012",StringComparison.OrdinalIgnoreCase));if(at>=0){hero.LootInventory.RemoveAt(at);LootDiscard.Add("LT012");AddLog($"REST — {hero.DisplayName} consumes Traveler's Rations for +1 recovery.");}}
            if(hero.StartingEquipment.Any(e=>string.Equals(e.Id,"EQ089",StringComparison.OrdinalIgnoreCase)))RemoveFirstHarmfulStatus(hero,"Blessed Chain Rest");
            AddLog($"REST — {hero.DisplayName} completes the free Rest granted by {source}.");
        }

        private void BeginTravelingMerchant0853(EncounterCardDefinition card,HeroState hero,int severity)
        {
            if(severity<=2)BeginEncounterMerchant0853(card,hero,1,0,false,false);
            else if(severity<=4)BeginEncounterMerchant0853(card,hero,2,0,false,false);
            else if(severity<=6)BeginEncounterMerchant0853(card,hero,3,0,true,false);
            else BeginEncounterMerchant0853(card,hero,5,25,false,false);
        }

        private void BeginEncounterMerchant0853(EncounterCardDefinition card,HeroState hero,int maxRarityRank,int discount,bool freeItem,bool singlePurchase)
        {
            string mode=$"EncounterMerchant0853:{maxRarityRank}:{discount}:{(freeItem?1:0)}:{(singlePurchase?1:0)}:0";
            string rarity=maxRarityRank<=1?"Common":maxRarityRank==2?"Common/Uncommon":maxRarityRank==3?"up to Rare":"non-Unique";
            string extra=freeItem?" One non-Quest Item may be taken free.":discount>0?$" The first purchase costs {discount} Gold less.":string.Empty;
            BeginRewardChoice0827(card.Id,card.Name,hero,mode,$"Merchant access: buy/sell {rarity} Equipment.{extra} Select Close when finished.");
        }

        private static int EquipmentRarityRank0853(string rarity)
        {
            if(string.Equals(rarity,"Common",StringComparison.OrdinalIgnoreCase))return 1;
            if(string.Equals(rarity,"Uncommon",StringComparison.OrdinalIgnoreCase))return 2;
            if(string.Equals(rarity,"Rare",StringComparison.OrdinalIgnoreCase))return 3;
            if(string.Equals(rarity,"Epic",StringComparison.OrdinalIgnoreCase))return 4;
            if(string.Equals(rarity,"Legendary",StringComparison.OrdinalIgnoreCase))return 5;
            if(string.Equals(rarity,"Unique",StringComparison.OrdinalIgnoreCase))return 6;
            return 99;
        }

        private bool ParseMerchantMode0853(string mode,out int maxRank,out int discount,out bool freeItem,out bool single,out bool specialUsed)
        {
            maxRank=discount=0;freeItem=single=specialUsed=false;
            if(string.IsNullOrEmpty(mode)||!mode.StartsWith("EncounterMerchant0853:",StringComparison.OrdinalIgnoreCase))return false;
            var p=mode.Split(':');if(p.Length<6)return false;
            int.TryParse(p[1],out maxRank);int.TryParse(p[2],out discount);freeItem=p[3]=="1";single=p[4]=="1";specialUsed=p[5]=="1";return true;
        }

        private void Append0853EncounterChoiceOptions(List<RewardChoiceOption> options,RewardChoiceState state)
        {
            if(state==null)return;var hero=RewardHeroFor(state.HeroId);var ids=state.RevealedCardIds??new List<string>();
            if(state.Mode=="EncounterBrokenBridgeHeroes0853")
            {
                int n=Mathf.Min(ids.Count,4);int combos=1<<n;
                for(int mask=0;mask<combos;mask++)
                {
                    var parts=new List<string>();var key="BR";
                    for(int i=0;i<n;i++)
                    {
                        bool damage=(mask&(1<<i))!=0;key+=damage?'D':'A';
                        string name=Heroes.TryGetValue(ids[i],out var h)?h.DisplayName:ids[i];parts.Add($"{name}: {(damage?"suffer 1 damage":"+1 Action next turn")}");
                    }
                    options.Add(new RewardChoiceOption{Key=key,Label=string.Join(" • ",parts)});
                }
                return;
            }
            if(state.Mode=="EncounterBrokenBridgeSupply0853")
            {
                if(PartyGold>=15)options.Add(new RewardChoiceOption{Key="LOSE15",Label="Lose 15 Gold in supplies"});
                options.Add(new RewardChoiceOption{Key="DAMAGE",Label="Each participating Hero suffers 1 damage"});return;
            }
            if(state.Mode=="EncounterFloodSupplies0853")
            {
                foreach(var h in ParticipatingHeroes.OrderBy(h=>h.PartyIndex))for(int i=0;i<h.LootInventory.Count;i++)
                {
                    string id=h.LootInventory[i];if(!RewardContent0826.Loot.TryGetValue(id,out var d)||d.QuestItem)continue;
                    options.Add(new RewardChoiceOption{Key=$"ITEM:{h.Id}:{i}",Label=$"Discard {RewardContent0826.LootLabel(id)} from {h.DisplayName}"});
                }
                options.Add(new RewardChoiceOption{Key="DAMAGE",Label="Keep supplies • each participating Hero suffers 1 damage"});return;
            }
            if(state.Mode=="EncounterCleanseHero0853"||state.Mode=="EncounterFreeRestHero0853")
            {
                foreach(var id in ids)if(Heroes.TryGetValue(id,out var h))options.Add(new RewardChoiceOption{Key=$"HERO:{id}",Label=state.Mode=="EncounterFreeRestHero0853"?$"{h.DisplayName} takes the free Rest":$"Remove 1 negative Condition from {h.DisplayName}"});
                return;
            }
            if(state.Mode?.StartsWith("EncounterToll0853:",StringComparison.OrdinalIgnoreCase)==true)
            {
                int.TryParse(state.Mode.Split(':').ElementAtOrDefault(1),out int toll);if(PartyGold>=toll)options.Add(new RewardChoiceOption{Key="PAY",Label=$"Pay {toll} Gold and pass"});options.Add(new RewardChoiceOption{Key="FIGHT",Label="Fight the Toll Collectors"});return;
            }
            if(ParseMerchantMode0853(state.Mode,out int maxRank,out int discount,out bool freeItem,out bool single,out bool specialUsed))
            {
                int shown=0;
                foreach(var id in EquipmentDeck)
                {
                    if(shown>=6)break;if(!RewardContent0826.Equipment.TryGetValue(id,out var d)||d.Value<=0)continue;int rank=EquipmentRarityRank0853(d.Rarity);if(rank>maxRank||rank>=6)continue;
                    int price=Mathf.Max(0,d.Value-(!specialUsed?discount:0));options.Add(new RewardChoiceOption{Key=$"BUY:{id}",Label=$"Buy {EquipmentChoiceLabel0850(id)} • {price} Gold"});shown++;
                }
                if(hero!=null)for(int i=0;i<hero.AcquiredEquipment.Count;i++)if(RewardContent0826.Equipment.TryGetValue(hero.AcquiredEquipment[i],out var d)&&EquipmentRarityRank0853(d.Rarity)<=maxRank&&EquipmentRarityRank0853(d.Rarity)<6)options.Add(new RewardChoiceOption{Key=$"SELL:{i}",Label=$"Sell {RewardContent0826.EquipmentLabel(d.Id)} • gain {d.Value} Gold"});
                if(freeItem&&!specialUsed)
                {
                    string item=LootDeck.FirstOrDefault(id=>RewardContent0826.Loot.TryGetValue(id,out var d)&&!d.QuestItem);
                    if(!string.IsNullOrEmpty(item))options.Add(new RewardChoiceOption{Key=$"FREE:{item}",Label=$"Take free Item • {RewardContent0826.LootLabel(item)}"});
                }
                options.Add(new RewardChoiceOption{Key="CLOSE",Label=single?"Decline / close merchant":"Close merchant"});
            }
        }

        private bool TryResolve0853EncounterChoice(RewardChoiceState state,HeroState hero,string key,out bool result)
        {
            result=false;if(state==null)return false;var ids=state.RevealedCardIds??new List<string>();
            if(state.Mode=="EncounterBrokenBridgeHeroes0853")
            {
                if(!key.StartsWith("BR",StringComparison.OrdinalIgnoreCase)||key.Length!=2+Mathf.Min(ids.Count,4))return true;
                for(int i=0;i<Mathf.Min(ids.Count,4);i++)if(Heroes.TryGetValue(ids[i],out var h))
                {
                    if(key[2+i]=='D')ApplyHeroDamageNow(h,1,state.SourceCardName);else if(key[2+i]=='A')h.NextTurnActionPenalty+=1;else return true;
                }
                FinishChoice0852(state,key);result=true;return true;
            }
            if(state.Mode=="EncounterBrokenBridgeSupply0853")
            {
                if(key=="LOSE15"&&PartyGold>=15)AddPartyGold(-15,state.SourceCardName);else if(key=="DAMAGE")foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))ApplyHeroDamageNow(h,1,state.SourceCardName);else return true;
                FinishChoice0852(state,key);result=true;return true;
            }
            if(state.Mode=="EncounterFloodSupplies0853")
            {
                if(key=="DAMAGE")foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))ApplyHeroDamageNow(h,1,state.SourceCardName);
                else if(key.StartsWith("ITEM:",StringComparison.OrdinalIgnoreCase))
                {
                    var p=key.Split(':');if(p.Length!=3||!Heroes.TryGetValue(p[1],out var owner)||!int.TryParse(p[2],out int index)||index<0||index>=owner.LootInventory.Count)return true;string id=owner.LootInventory[index];if(!RewardContent0826.Loot.TryGetValue(id,out var d)||d.QuestItem)return true;owner.LootInventory.RemoveAt(index);LootDiscard.Add(id);AddLog($"ENCOUNTER SUPPLIES — {owner.DisplayName} discards {RewardContent0826.LootLabel(id)} as rope/supplies.");
                }
                else return true;
                FinishChoice0852(state,key);result=true;return true;
            }
            if(state.Mode=="EncounterCleanseHero0853")
            {
                if(!key.StartsWith("HERO:",StringComparison.OrdinalIgnoreCase)||!Heroes.TryGetValue(key.Substring(5),out var chosen)||!ids.Contains(chosen.Id,StringComparer.OrdinalIgnoreCase))return true;RemoveFirstHarmfulStatus(chosen,state.SourceCardName);FinishChoice0852(state,key);result=true;return true;
            }
            if(state.Mode=="EncounterFreeRestHero0853")
            {
                if(!key.StartsWith("HERO:",StringComparison.OrdinalIgnoreCase)||!Heroes.TryGetValue(key.Substring(5),out var chosen)||!ids.Contains(chosen.Id,StringComparer.OrdinalIgnoreCase))return true;ResolveFreeRest0853(chosen,state.SourceCardName);FinishChoice0852(state,key);result=true;return true;
            }
            if(state.Mode?.StartsWith("EncounterToll0853:",StringComparison.OrdinalIgnoreCase)==true)
            {
                int.TryParse(state.Mode.Split(':').ElementAtOrDefault(1),out int toll);var encounter=ids.Count>0?ActiveEncounters.FirstOrDefault(e=>string.Equals(e.InstanceId,ids[0],StringComparison.OrdinalIgnoreCase)):null;if(encounter==null)return true;
                if(key=="PAY"&&PartyGold>=toll)
                {
                    AddPartyGold(-toll,state.SourceCardName);encounter.RewardResolved=true;ActiveEncounters.Remove(encounter);LastCompletedEncounterCardId=encounter.CardId;CompletedEncounterCount++;AddLog($"ENCOUNTER RESOLVED — {encounter.CardName}: party pays {toll} Gold and passes without combat.");ResolvePostEncounter0852(encounter);ResolvePostEncounter0853(encounter);
                }
                else if(key=="FIGHT")
                {
                    encounter.Source=(encounter.Source??string.Empty)+"|EN015_FOUGHT";if(!Locations.TryGetValue(encounter.LocationId,out var loc))return true;string effect=SearchEncounterContent0820.EncounterCards[encounter.CardId].EffectForSeverity(encounter.Severity);int fightAt=effect.IndexOf("fight",StringComparison.OrdinalIgnoreCase);if(fightAt>=0)effect=effect.Substring(fightAt+5);int spawned=ResolveEncounterEnemySpawns(effect,loc,encounter);AddLog($"ENCOUNTER CHOICE — Toll Collectors fight selected; {spawned} Encounter Enem{(spawned==1?"y":"ies")} enter play.");if(spawned==0)TryCompleteEncounter(encounter,"toll fight setup");
                }
                else return true;
                FinishChoice0852(state,key);result=true;return true;
            }
            if(ParseMerchantMode0853(state.Mode,out int maxRank,out int discount,out bool freeItem,out bool single,out bool specialUsed))
            {
                if(key=="CLOSE"){FinishChoice0852(state,key);result=true;return true;}
                if(key.StartsWith("BUY:",StringComparison.OrdinalIgnoreCase))
                {
                    string id=key.Substring(4);if(hero==null||!RewardContent0826.Equipment.TryGetValue(id,out var d)||EquipmentRarityRank0853(d.Rarity)>maxRank||EquipmentRarityRank0853(d.Rarity)>=6)return true;int at=EquipmentDeck.FindIndex(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));if(at<0)return true;int price=Mathf.Max(0,d.Value-(!specialUsed?discount:0));if(PartyGold<price)return true;AddPartyGold(-price,state.SourceCardName);EquipmentDeck.RemoveAt(at);KeepHeldEquipment(hero,id,state.SourceCardName);specialUsed=specialUsed||discount>0;
                }
                else if(key.StartsWith("SELL:",StringComparison.OrdinalIgnoreCase))
                {
                    if(hero==null||!int.TryParse(key.Substring(5),out int i)||i<0||i>=hero.AcquiredEquipment.Count)return true;string id=hero.AcquiredEquipment[i];if(!RewardContent0826.Equipment.TryGetValue(id,out var d)||EquipmentRarityRank0853(d.Rarity)>maxRank||EquipmentRarityRank0853(d.Rarity)>=6)return true;hero.AcquiredEquipment.RemoveAt(i);EquipmentDeck.Add(id);AddPartyGold(d.Value,state.SourceCardName+" sale");AddLog($"MERCHANT SALE — {hero.DisplayName} sells {RewardContent0826.EquipmentLabel(id)} for {d.Value} Gold.");
                }
                else if(key.StartsWith("FREE:",StringComparison.OrdinalIgnoreCase)&&freeItem&&!specialUsed)
                {
                    string id=key.Substring(5);int at=LootDeck.FindIndex(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));if(at<0||!RewardContent0826.Loot.TryGetValue(id,out var d)||d.QuestItem)return true;LootDeck.RemoveAt(at);KeepHeldLoot(hero,id,state.SourceCardName);specialUsed=true;
                }
                else return true;
                if(single){FinishChoice0852(state,key);result=true;return true;}
                state.Mode=$"EncounterMerchant0853:{maxRank}:{discount}:{(freeItem?1:0)}:{(single?1:0)}:{(specialUsed?1:0)}";state.Prompt="Merchant remains open. Make another transaction or Close.";AddLog($"MERCHANT — {state.SourceCardName} transaction complete; merchant remains available.");StateChanged?.Invoke();result=true;return true;
            }
            return false;
        }

        private int EncounterTollGold0853(int severity)=>severity<=2?5:severity<=4?10:15;

        private int ConsumeEncounterAttackModifier0853(HeroState hero,EnemyState enemy)
        {
            if(hero==null||enemy==null)return 0;
            var encounter=ActiveEncounters.FirstOrDefault(e=>e.CardId=="EN018"&&!e.RewardResolved&&e.LocationId==hero.LocationId&&e.LocationId==enemy.LocationId);
            if(encounter==null)return 0;string token=$"EN018_ATTACK_R{Round}";if((encounter.Source??string.Empty).IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0)return 0;encounter.Source=(encounter.Source??string.Empty)+"|"+token;AddLog("ENCOUNTER HAZARD — Shamanic Totem weakens the first Hero Attack this Round by -1 Attack.");return -1;
        }

        private void ResolveEncounterHeroDefeat0853(HeroState hero)
        {
            if(hero==null)return;
            var wolf=ActiveEncounters.FirstOrDefault(e=>e.CardId=="EN004"&&!e.RewardResolved&&e.LocationId==hero.LocationId);
            if(wolf!=null)
            {
                int at=hero.LootInventory.FindIndex(id=>RewardContent0826.Loot.TryGetValue(id,out var d)&&d.Consumable);
                if(at>=0){string id=hero.LootInventory[at];hero.LootInventory.RemoveAt(at);LootDiscard.Add(id);AddLog($"ENCOUNTER FAILURE — Wolf Pack defeats {hero.DisplayName}; {RewardContent0826.LootLabel(id)} is discarded before recovery.");}
                else if(hero.HealingPotions>0){hero.HealingPotions--;AddLog($"ENCOUNTER FAILURE — Wolf Pack defeats {hero.DisplayName}; 1 carried Healing Potion is discarded before recovery.");}
                else AddLog($"ENCOUNTER FAILURE — Wolf Pack defeats {hero.DisplayName}, but they carry no Consumable to discard.");
            }
            if(ParticipatingHeroes.All(h=>h.IsDefeated))
            {
                foreach(var encounter in ActiveEncounters.Where(e=>(e.CardId=="EN008"||e.CardId=="EN019")&&!e.RewardResolved).ToList())
                {
                    if((encounter.Source??string.Empty).IndexOf("RESCUE_FAILURE_APPLIED",StringComparison.OrdinalIgnoreCase)>=0)continue;encounter.Source=(encounter.Source??string.Empty)+"|RESCUE_FAILURE_APPLIED";encounter.HadEnemyLeavePlayUndefeated=true;AddGeneralReputation(-1,$"{encounter.CardId} {encounter.CardName} failed escort/rescue");AddLog($"ENCOUNTER FAILURE — {encounter.CardName} rescue combat is lost; General Reputation decreases by 1.");
                }
            }
        }

        private void ResolveEncounterEnemyExit0853(EncounterRuntimeState encounter,EnemyState enemy)
        {
            if(encounter==null||enemy==null)return;if(encounter.CardId=="EN008"||encounter.CardId=="EN011"||encounter.CardId=="EN019")encounter.HadEnemyLeavePlayUndefeated=true;
        }

        private void ResolveEncounterEndRound0853()
        {
            foreach(var encounter in ActiveEncounters.Where(e=>e.CardId=="EN021"&&e.Severity>=7&&!e.RewardResolved&&e.StartedRound==Round).ToList())
            {
                bool crocAlive=encounter.SpawnedEnemyIds.Any(id=>Enemies.TryGetValue(id,out var e)&&!e.Defeated&&e.TemplateId=="E027");if(!crocAlive)continue;string token="EN021_FOLLOWUP";if((encounter.Source??string.Empty).IndexOf(token,StringComparison.OrdinalIgnoreCase)<0){encounter.Source=(encounter.Source??string.Empty)+"|"+token;AddLog("ENCOUNTER FAILURE — Flooded Crossing E027 survived Round 1; another Travel Encounter is queued for after this encounter resolves.");}
            }
        }

        private void ResolvePostEncounter0853(EncounterRuntimeState encounter)
        {
            if(encounter==null||encounter.CardId!="EN021"||(encounter.Source??string.Empty).IndexOf("EN021_FOLLOWUP",StringComparison.OrdinalIgnoreCase)<0)return;
            if(!Locations.TryGetValue(encounter.LocationId,out var location)||!Heroes.TryGetValue(encounter.SourceHeroId,out var hero))return;
            AddLog("ENCOUNTER CHAIN — Flooded Crossing delayed consequence draws another Travel Encounter now that the crossing has resolved.");
            ResolveTravelEncounter(location,hero,1,0,encounter.CardName+" delayed follow-up",0,0,true);
        }

        private void AddNextQuestMarker0853(HeroState hero,string marker)
        {
            if(hero==null||string.IsNullOrEmpty(marker))return;if(hero.KeptSearchEffects0852==null)hero.KeptSearchEffects0852=new List<string>();if(!hero.KeptSearchEffects0852.Contains(marker,StringComparer.OrdinalIgnoreCase))hero.KeptSearchEffects0852.Add(marker);
        }

        private void ApplyEncounterQuestStartEffects0853(HeroState hero,string nextQuestId)
        {
            if(hero?.KeptSearchEffects0852==null)return;
            foreach(var marker in hero.KeptSearchEffects0852.Where(x=>x.StartsWith("EN020_SHRINE:",StringComparison.OrdinalIgnoreCase)).ToList())
            {
                string origin=marker.Substring("EN020_SHRINE:".Length);if(string.Equals(origin,nextQuestId,StringComparison.OrdinalIgnoreCase))continue;hero.KeptSearchEffects0852.Remove(marker);AddStatus(hero,"Inspired",0,1,"EN020 Forest Shrine",true,hero,"until end of first round");AddLog($"ENCOUNTER CARRYOVER — {hero.DisplayName} begins {nextQuestId} Inspired from Forest Shrine.");
            }
        }

        private bool CanUseEncounterUtility0853(HeroState hero)
        {
            if(hero?.KeptSearchEffects0852==null||!CanUseTurnActions(hero)||GeneralActions(hero)<1)return false;
            return hero.KeptSearchEffects0852.Any(x=>x.StartsWith("EN019_LOST_CHILD:",StringComparison.OrdinalIgnoreCase)&&!string.Equals(x.Substring("EN019_LOST_CHILD:".Length),CurrentQuestId0829,StringComparison.OrdinalIgnoreCase));
        }

        private string EncounterUtilityLabel0853(HeroState hero)=>CanUseEncounterUtility0853(hero)?"Search for Lost Child (1)":null;

        private bool UseEncounterUtility0853(HeroState hero)
        {
            if(!CanUseEncounterUtility0853(hero)||!RequireHeroTurn(hero,"Search for Lost Child"))return false;string marker=hero.KeptSearchEffects0852.First(x=>x.StartsWith("EN019_LOST_CHILD:",StringComparison.OrdinalIgnoreCase)&&!string.Equals(x.Substring("EN019_LOST_CHILD:".Length),CurrentQuestId0829,StringComparison.OrdinalIgnoreCase));hero.Actions--;hero.KeptSearchEffects0852.Remove(marker);AddGeneralReputation(1,"EN019 Lost Child search");AddPartyGold(10,"EN019 Lost Child search");AddLog($"ENCOUNTER CARRYOVER — {hero.DisplayName} spends 1 Action to find the Lost Child: +1 Reputation and +10 Gold.");StateChanged?.Invoke();return true;
        }

#if UNITY_EDITOR
        public int DebugCampaignIEncounterDeckDefinitionCount0853()=>SearchEncounterContent0820.EncounterCards.Keys.Count(id=>FullContentCatalog0850.IsCampaignIEncounterId(id));
        public int DebugCampaignIEncounterPhysicalCount0853()=>SearchEncounterContent0820.BuildEncounterDeck().Count;
        public int DebugEncounterAttackModifier0853(string heroId,string enemyId)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&Enemies.TryGetValue(enemyId??string.Empty,out var e)?ConsumeEncounterAttackModifier0853(h,e):0;
        public bool DebugCanUseEncounterUtility0853(string heroId)=>Heroes.TryGetValue(heroId??string.Empty,out var h)&&CanUseEncounterUtility0853(h);
        public void DebugResolveEncounterEndRound0853()=>ResolveEncounterEndRound0853();
#endif
    }
}
