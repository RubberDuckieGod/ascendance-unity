using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public static class AscendanceSaveSystem
    {
        private const string FileName = "campaign_q001.json";
        public static string SavePath => Path.Combine(AscendanceStoragePaths0102.PersistentDataPath, FileName);
        public static bool HasSave => File.Exists(SavePath);
        public static bool HasValidSave => TryLoad(out _, out _);
        public static bool CanSave(AscendanceGameManager gm) => gm != null && !gm.IsResolving && !gm.HasPendingReactionWindow && !gm.ParticipatingHeroes.Any(h => h.Moving);

        public static bool SaveCurrent(AscendanceGameManager gm, out string message)
        {
            if (!CanSave(gm)) { message = "Cannot save during movement, a Reaction window, or combat resolution."; return false; }
            try
            {
                var data = Capture(gm); Directory.CreateDirectory(AscendanceStoragePaths0102.PersistentDataPath); File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                message = "Campaign saved."; gm.AddLog($"SAVE — Campaign saved with Hero decks, hands, statuses, inventory, and pieces. Resume: {CampaignResumeRuntime0842.DescribeSurface0842(data.ResumeSurface0842)}."); return true;
            }
            catch (Exception ex) { message = "Save failed: " + ex.Message; Debug.LogException(ex); return false; }
        }

        public static AscendanceSaveData Load()
        {
            return TryLoad(out var save, out _) ? save : null;
        }

        public static bool TryLoad(out AscendanceSaveData save, out string message)
        {
            save = null;
            if (!HasSave) { message = "No campaign save found."; return false; }
            try
            {
                string json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json)) { message = "Campaign save is empty."; return false; }
                save = AscendanceOptionalSaveState0102.Normalize(JsonUtility.FromJson<AscendanceSaveData>(json));
                if (save == null || string.IsNullOrWhiteSpace(save.QuestId) || save.Heroes == null || save.Heroes.Count == 0)
                {
                    save = null; message = "Campaign save is unreadable or incomplete."; return false;
                }
                message = "Campaign save ready.";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ASCENDANCE] Continue save rejected: " + ex.Message);
                message = "Campaign save could not be read.";
                return false;
            }
        }

        public static string MainMenuSummary0842
        {
            get
            {
                return TryLoad(out var save, out _) ? CampaignResumeRuntime0842.BuildMainMenuSummary0842(save) : (HasSave ? "SAVE FILE UNREADABLE — CONTINUE DISABLED" : "NO CAMPAIGN SAVE");
            }
        }

        public static void DeleteSave() { try { if (HasSave) File.Delete(SavePath); } catch (Exception ex) { Debug.LogException(ex); } }

        // 0.10.0 — the dedicated-server authority uses the same canonical campaign
        // serializer as local saves without writing through the player save slot.
        public static AscendanceSaveData CaptureForAuthority0100(AscendanceGameManager gm) => gm == null ? null : Capture(gm);

        public static AscendanceSaveData CloneForAuthority0100(AscendanceSaveData data)
        {
            return data == null ? null : AscendanceOptionalSaveState0102.Normalize(JsonUtility.FromJson<AscendanceSaveData>(JsonUtility.ToJson(data)));
        }

        private static AscendanceSaveData Capture(AscendanceGameManager gm)
        {
            gm.SyncCampaignProgress0833();
            var data = new AscendanceSaveData
            {
                Version=AscendanceGameManager.BuildVersion, SavedAtUtc=DateTime.UtcNow.ToString("O"), ResumeSurface0842=CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm), QuestId=gm.CurrentQuestId0829, QuestSpawnSerial0829=gm.QuestSpawnSerialForSave0829, NextQuestSupply0829=gm.NextQuestSupply0829, ReduceFirstWE002TravelScout0830=gm.ReduceFirstWE002TravelScout0830, RemoveEliteFromQ007Setup0830=gm.RemoveEliteFromQ007Setup0830, PartyMorale0831=gm.PartyMorale0831, RemoveSiegeEngineFromQ008Setup0831=gm.RemoveSiegeEngineFromQ008Setup0831, CaptainRolandAssistsQ0100832=gm.CaptainRolandAssistsQ0100832, Round=gm.Round,
                FireTokens=gm.FireTokens, VillagersRescued=gm.VillagersRescued, QuestComplete=gm.QuestComplete, QuestFailed=gm.QuestFailed,
                PartyGold=gm.PartyGold, GeneralReputation=gm.GeneralReputation,
                LootDeckIds=gm.LootDeck.ToList(), LootDiscardIds=gm.LootDiscard.ToList(), EquipmentDeckIds=gm.EquipmentDeck.ToList(), EquipmentDiscardIds=gm.EquipmentDiscard.ToList(),
                LastSearchCardId=gm.LastSearchCardId, LastEncounterCardId=gm.LastEncounterCardId, LastEncounterSeverity=gm.LastEncounterSeverity,
                PendingContentInstruction=gm.PendingContentInstruction, NextTravelEncounterSeverityModifier=gm.NextTravelEncounterSeverityModifier, SkipNextTravelEncounter=gm.SkipNextTravelEncounter, NextGoblinEncounterMovementBonus=gm.NextGoblinEncounterMovementBonus, NextGoblinEncounterWarDrummer=gm.NextGoblinEncounterWarDrummer,
                SearchTravelSeverityModifier0852=gm.SearchTravelSeverityModifierForSave0852, SearchTravelSeverityRound0852=gm.SearchTravelSeverityRoundForSave0852, SearchGoblinSeverityModifier0852=gm.SearchGoblinSeverityModifierForSave0852, SearchGoblinSeverityRound0852=gm.SearchGoblinSeverityRoundForSave0852, SearchConditionWard0852=gm.SearchConditionWardForSave0852, SearchConditionWardRound0852=gm.SearchConditionWardRoundForSave0852, DeferredPostEncounter0852=gm.DeferredPostEncounterForSave0852.ToList(),
                PendingEncounterDeckControl=gm.PendingEncounterDeckControl==null?null:new EncounterDeckControlSaveData{SourceCardId=gm.PendingEncounterDeckControl.SourceCardId,SourceCardName=gm.PendingEncounterDeckControl.SourceCardName,Mode=gm.PendingEncounterDeckControl.Mode,RevealedCardIds=gm.PendingEncounterDeckControl.RevealedCardIds.ToList()},
                PendingRewardChoice=gm.PendingRewardChoice==null?null:new RewardChoiceSaveData{SourceCardId=gm.PendingRewardChoice.SourceCardId,SourceCardName=gm.PendingRewardChoice.SourceCardName,HeroId=gm.PendingRewardChoice.HeroId,Mode=gm.PendingRewardChoice.Mode,Prompt=gm.PendingRewardChoice.Prompt,RevealedCardIds=gm.PendingRewardChoice.RevealedCardIds.ToList()}, EncounterSpawnSerial=gm.EncounterSpawnSerialForSave,
                EncounterInstanceSerial=gm.EncounterInstanceSerialForSave,EncounterObjectSerial=gm.EncounterObjectSerialForSave,CompletedEncounterCount=gm.CompletedEncounterCount,LastCompletedEncounterCardId=gm.LastCompletedEncounterCardId,
                SearchDeckIds=gm.SearchDeck.ToList(), SearchDiscardIds=gm.SearchDiscard.ToList(), EncounterDeckIds=gm.EncounterDeck.ToList(), EncounterDiscardIds=gm.EncounterDiscard.ToList(), ExplorationDeckIds=gm.ExplorationDeck0828.ToList(),
                SelectedHeroId=gm.SelectedHeroId, TurnHeroId=gm.TurnHeroId, SelectedEnemyId=gm.SelectedEnemyId, PartyHeroIds=gm.PartyHeroIds
            };
            data.CraftingSpecialResults0855=gm.CraftingSpecialResults0855.ToList();
            data.CraftingUnlockFlags0855=gm.CraftingUnlockFlagsForSave0855.ToList();
            data.DefeatedEnemyTemplateIds0855=gm.DefeatedEnemyTemplateIdsForSave0855.ToList();
            data.QuestRuntime0856=gm.ExportQuestRuntime0856();
            data.QuestCampaignFlags0856=gm.QuestCampaignFlags0856.ToList();
            data.FactionReputation0856=gm.ExportFactionReputation0856();
            if(gm.CampaignProgress0833!=null)
            {
                var c=gm.CampaignProgress0833;data.CampaignProgress0833=new CampaignProgressSaveData0833{CampaignId0845=c.CampaignId0845,AwaitingCampaignRuntime0845=c.AwaitingCampaignRuntime0845,CampaignArchive0845=c.CampaignArchive0845.Select(x=>x?.ToSave0845()).Where(x=>x!=null).ToList(),CurrentWorldEventId=c.CurrentWorldEventId,WorldEventDay=c.WorldEventDay,TotalCampaignDay=c.TotalCampaignDay,CampaignCompleted=c.CampaignCompleted,CampaignDefeated=c.CampaignDefeated,CompletedQuestIds=c.CompletedQuestIds.ToList(),FailedQuestIds=c.FailedQuestIds.ToList(),MissedQuestIds=c.MissedQuestIds.ToList(),CompletedWorldEventIds=c.CompletedWorldEventIds.ToList(),RemoveScoutFromQ004Setup=c.RemoveScoutFromQ004Setup,AddRaiderToQ004FirstEncounter=c.AddRaiderToQ004FirstEncounter,AddEliteToQ007Setup=c.AddEliteToQ007Setup,WE003TravelSeverityPenalty=c.WE003TravelSeverityPenalty,DamagedBuildings=c.DamagedBuildings,Q010SiegePressureModifier=c.Q010SiegePressureModifier,Q010WoundPenalty=c.Q010WoundPenalty,StonebrookEquipmentPurchaseLocked=c.StonebrookEquipmentPurchaseLocked,WorldEventStateInitialized0857=c.WorldEventStateInitialized0857,Suspicion0857=c.Suspicion0857,Instability0857=c.Instability0857,Blight0857=c.Blight0857,Doomtide0857=c.Doomtide0857,Sunfall0857=c.Sunfall0857,WorldEventFlags0857=c.WorldEventFlags0857.ToList(),WorldEventOnceKeys0857=c.WorldEventOnceKeys0857.ToList()};
            }
            data.QuestResult0835=gm.ExportQuestResult0835();
            foreach (var h in gm.ParticipatingHeroes)
            {
                var hs = new HeroSaveData
                {
                    Id=h.Id,Level=h.Level,XP=h.XP,Health=h.Health,Actions=h.Actions,LocationId=h.LocationId,Downed=h.Downed,Dead=h.Dead,MovedThisTurn=h.MovedThisTurn,
                    HealingPotions=h.HealingPotions,KillCount=h.KillCount,BlacksmithingXp0837=h.BlacksmithingXp0837,AlchemyXp0837=h.AlchemyXp0837,EnchantingXp0837=h.EnchantingXp0837,ArtificingXp0837=h.ArtificingXp0837,LastDowntimeActivityKey0839=h.LastDowntimeActivityKey0839,LastDowntimeActivityLabel0839=h.LastDowntimeActivityLabel0839,TurnAttackBonus=h.TurnAttackBonus,NextAttackBonus=h.NextAttackBonus,NextArmorIgnore=h.NextArmorIgnore,FirstAttackThisTurn=h.FirstAttackThisTurn,TurnCompleted=h.TurnCompleted,
                    TargetLockedEnemyId=h.TargetLockedEnemyId,TargetLockedAttackBonus=h.TargetLockedAttackBonus,FocusEnemyId=h.FocusEnemyId,FocusAttackBonus=h.FocusAttackBonus,AttackCardCostReduction=h.AttackCardCostReduction,AttackCardAttackBonus=h.AttackCardAttackBonus,DrawAfterAttackCard=h.DrawAfterAttackCard,FreeAttackCardReady=h.FreeAttackCardReady,NextAttackRangeConnections=h.NextAttackRangeConnections,
                    FreeMoveReady=h.FreeMoveReady,FreeMoveIgnoresEngagement=h.FreeMoveIgnoresEngagement,FreeMoveIgnoresDifficultTerrain=h.FreeMoveIgnoresDifficultTerrain,BonusAfterFreeMove=h.BonusAfterFreeMove,FreeMoveAttackBonus=h.FreeMoveAttackBonus,FreeMoveAttackBonusRequiresEnemy=h.FreeMoveAttackBonusRequiresEnemy,SelfDamageAfterNextAttack=h.SelfDamageAfterNextAttack,AttackOnlyActions=h.AttackOnlyActions,NextTurnActionPenalty=h.NextTurnActionPenalty,
                    FreeSearchLocationId=h.FreeSearchLocationId,PendingLocationOption=h.PendingLocationOption,PendingLocationOptionLocationId=h.PendingLocationOptionLocationId,
                    CannotDropBelowOne=h.CannotDropBelowOne,CannotDropBelowOneTurns=h.CannotDropBelowOneTurns,PendingDiscardCount=h.PendingDiscardCount,
                    BattleRoar=h.BattleRoar,BloodrageReady=h.BloodrageReady,AvatarRageTurns=h.AvatarRageTurns,AvatarFloorActive=h.AvatarFloorActive,
                    DeckIds=h.Deck.ToList(),HandIds=h.Hand.Select(c=>c.Id).ToList(),DiscardIds=h.Discard.ToList(),UsedRoundAbilities=h.UsedRoundAbilities.ToList(),UsedTurnAbilities=h.UsedTurnAbilities.ToList(),UsedQuestCards=h.UsedQuestCards.ToList(),
                    LootInventoryIds=h.LootInventory.ToList(),AcquiredEquipmentIds=h.AcquiredEquipment.ToList(),KeptSearchEffects0852=h.KeptSearchEffects0852.ToList(),EquippedEquipmentIds=h.StartingEquipment.SelectMany(e=>Enumerable.Repeat(e.Id,Mathf.Max(1,e.Copies))).ToList()
                };
                foreach(var s in h.Statuses) hs.Statuses.Add(new StatusSaveData{Id=s.Id,Name=s.Name,RemainingTurns=s.RemainingTurns,RemainingRounds=s.RemainingRounds,Source=s.Source,Beneficial=s.Beneficial,ExpiryTiming=s.ExpiryTiming,RemainingExpiryTriggers=s.RemainingExpiryTriggers,SourceHeroId=s.SourceHeroId,AppliedRound=s.AppliedRound});
                data.Heroes.Add(hs);
            }
            foreach(var encounter in gm.ActiveEncounters)
                data.ActiveEncounters.Add(new EncounterRuntimeSaveData{InstanceId=encounter.InstanceId,CardId=encounter.CardId,CardName=encounter.CardName,LocationId=encounter.LocationId,SourceHeroId=encounter.SourceHeroId,Source=encounter.Source,Severity=encounter.Severity,StartedRound=encounter.StartedRound,RewardText=encounter.RewardText,FailureText=encounter.FailureText,PostResolveEffect0852=encounter.PostResolveEffect0852,RewardResolved=encounter.RewardResolved,AllFireTokensRemovedBeforeClear=encounter.AllFireTokensRemovedBeforeClear,HadEnemyLeavePlayUndefeated=encounter.HadEnemyLeavePlayUndefeated,UndefeatedEliteExitCount=encounter.UndefeatedEliteExitCount,GainedEquipmentIds=encounter.GainedEquipmentIds.ToList(),SpawnedEnemyIds=encounter.SpawnedEnemyIds.ToList(),SpawnedObjectIds=encounter.SpawnedObjectIds.ToList()});
            foreach(var obj in gm.EncounterObjects)
                data.EncounterObjects.Add(new EncounterObjectSaveData{ObjectId=obj.ObjectId,EncounterInstanceId=obj.EncounterInstanceId,CardId=obj.CardId,Type=obj.Type,DisplayName=obj.DisplayName,LocationId=obj.LocationId,BlocksEncounterCompletion=obj.BlocksEncounterCompletion,RemoveAtEncounterEnd=obj.RemoveAtEncounterEnd,InteractionActionCost=obj.InteractionActionCost,InteractionLabel=obj.InteractionLabel,AffectedHeroId=obj.AffectedHeroId,CreatedRound=obj.CreatedRound});
            foreach(var e in gm.Enemies.Values)
            {
                var es=new EnemySaveData{Id=e.Id,TemplateId=e.TemplateId,EncounterSpawned=e.EncounterSpawned,QuestSpawned=e.QuestSpawned,QuestId=e.QuestId,EncounterInstanceId=e.EncounterInstanceId,MaxHealth=e.MaxHealth,Health=e.Health,Attack=e.Attack,Armor=e.Armor,IsBoss=e.IsBoss,ActivatedThisRound=e.ActivatedThisRound,LocationId=e.LocationId,UsedKeys0859=e.UsedKeys0859?.ToList()??new List<string>(),MovementStepsThisActivation0859=e.MovementStepsThisActivation0859,FocusHeroId0859=e.FocusHeroId0859};foreach(var s in e.Statuses)es.Statuses.Add(new StatusSaveData{Id=s.Id,Name=s.Name,RemainingTurns=s.RemainingTurns,RemainingRounds=s.RemainingRounds,Source=s.Source,Beneficial=s.Beneficial,ExpiryTiming=s.ExpiryTiming,RemainingExpiryTriggers=s.RemainingExpiryTriggers,SourceHeroId=s.SourceHeroId,AppliedRound=s.AppliedRound});data.Enemies.Add(es);
            }
            foreach(var hazard in gm.EnemyHazards0859)
                data.EnemyHazards0859.Add(new EnemyHazardSaveData0859{HazardId=hazard.HazardId,Type=hazard.Type,SourceEnemyId=hazard.SourceEnemyId,LocationId=hazard.LocationId,CreatedRound=hazard.CreatedRound,TriggerRound=hazard.TriggerRound});
            data.RegionalEncounterDeckKey0858=gm.RegionalEncounterDeckKey0858;
            data.RegionalEncounterDeckIds0858=gm.RegionalEncounterDeck0858.ToList();
            data.RegionalEncounterDiscardIds0858=gm.RegionalEncounterDiscard0858.ToList();
            foreach(var l in gm.Locations.Values)
            {
                var ls=new LocationSaveData{Id=l.Id,Revealed=l.Revealed,RevealResolved=l.RevealResolved,RevealEffectDeferred=l.RevealEffectDeferred,FirstVisitResolved=l.FirstVisitResolved,Searched=l.Searched,SearchRequests=l.SearchRequests,TravelEncounterRequests=l.TravelEncounterRequests};
                foreach(var t in l.TemporaryTags) ls.TemporaryTags.Add(new LocationTagSaveData{Tag=t.Tag,Source=t.Source,SourceHeroId=t.SourceHeroId,ExpiryTiming=t.ExpiryTiming,RemainingExpiryTriggers=t.RemainingExpiryTriggers});
                ls.UsedKeys0858=l.UsedKeys0858.ToList();
                data.Locations.Add(ls);
            }
            foreach(var p in gm.HeroCreatedPieces)data.HeroCreatedPieces.Add(new HeroCreatedPieceSaveData{PieceId=p.PieceId,Type=p.Type,OwnerHeroId=p.OwnerHeroId,LocationId=p.LocationId,Health=p.Health});
            if(gm.QuestRuntime0829!=null)
            {
                var q=gm.QuestRuntime0829;data.QuestRuntime0829=new QuestRuntimeSaveData0829{QuestId=q.QuestId,QuestName=q.QuestName,RewardResolved=q.RewardResolved,CaravanRevealed=q.CaravanRevealed,ReinforcementCount=q.ReinforcementCount,CaptainSpawned=q.CaptainSpawned,CaptainDefeated=q.CaptainDefeated,ManifestGranted=q.ManifestGranted,StrongboxKeyGranted=q.StrongboxKeyGranted,StrongboxOpened=q.StrongboxOpened,DeliveredSupply=q.DeliveredSupply,ObjectiveLocationRevealed=q.ObjectiveLocationRevealed,BeaconLit=q.BeaconLit,ObjectiveEmptyRounds=q.ObjectiveEmptyRounds,VaultKeyGranted=q.VaultKeyGranted,VaultOpened=q.VaultOpened,InitialPatrolSpawned=q.InitialPatrolSpawned,TrailMarkers=q.TrailMarkers,WarCampRevealed=q.WarCampRevealed,WarChiefDefeated=q.WarChiefDefeated,AlarmTokens=q.AlarmTokens,SupplyCratesDestroyed=q.SupplyCratesDestroyed,PrisonersFreed=q.PrisonersFreed,PrisonersRescued=q.PrisonersRescued,PrisonersExecuted=q.PrisonersExecuted,PrisonAlarmSpawned=q.PrisonAlarmSpawned,PrisonReinforcementSpawned=q.PrisonReinforcementSpawned,SupplyWagonsDestroyed=q.SupplyWagonsDestroyed,SupplyWagonsEscaped=q.SupplyWagonsEscaped,ConvoyFirstDestroyedWave=q.ConvoyFirstDestroyedWave,ConvoySecondDestroyedWave=q.ConvoySecondDestroyedWave,WallTrack=q.WallTrack,Wall4Triggered=q.Wall4Triggered,Wall2Triggered=q.Wall2Triggered,SiegeEnginesPlaced=q.SiegeEnginesPlaced,SiegeEnginesDestroyed=q.SiegeEnginesDestroyed,ChampionSpawned=q.ChampionSpawned,ChampionDefeated=q.ChampionDefeated,RolandRevealed=q.RolandRevealed,RolandFreed=q.RolandFreed,RolandRescued=q.RolandRescued,RolandDefeated=q.RolandDefeated,ExecutionTrack=q.ExecutionTrack,FortressGateRevealed=q.FortressGateRevealed,GateHealth=q.GateHealth,GateArmor=q.GateArmor,GateReinforcementTriggered=q.GateReinforcementTriggered,GateDestroyed=q.GateDestroyed,BallistasDestroyed=q.BallistasDestroyed,BallistaRepairPending=q.BallistaRepairPending,BallistaRepairUsed=q.BallistaRepairUsed,PendingBallistaObjectId=q.PendingBallistaObjectId,GateEmptyRounds=q.GateEmptyRounds,StrongholdQualifyingReveals=q.StrongholdQualifyingReveals,ReinforcementTokensRemaining=q.ReinforcementTokensRemaining,HighShamanWaiting=q.HighShamanWaiting,ThroneRoomRevealed=q.ThroneRoomRevealed,BannersDestroyed=q.BannersDestroyed,GoblinKingLastStandTriggered=q.GoblinKingLastStandTriggered,GoblinKingHordeRound=q.GoblinKingHordeRound,CampaignVictory=q.CampaignVictory,DefenderOfStonebrookUnlocked=q.DefenderOfStonebrookUnlocked,QuestOwnedEnemyIds=q.QuestOwnedEnemyIds.ToList()};
                foreach(var o in q.Objects)data.QuestRuntime0829.Objects.Add(new QuestObjectSaveData0829{ObjectId=o.ObjectId,QuestId=o.QuestId,Type=o.Type,DisplayName=o.DisplayName,LocationId=o.LocationId,OwnerHeroId=o.OwnerHeroId,Supply=o.Supply,Health=o.Health,MaxHealth=o.MaxHealth,Armor=o.Armor,Attack=o.Attack,Range=o.Range,Rescued=o.Rescued,Destroyed=o.Destroyed,Resolved=o.Resolved});
            }
            return data;
        }
    }
}
