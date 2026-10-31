using System;
using System.Collections.Generic;

namespace Ascendance
{
    [Serializable]
    public sealed class AscendanceSaveData
    {
        public string Version = AscendanceGameManager.BuildVersion;
        public string SavedAtUtc;
        // 0.8.42 — exact player-facing surface to restore on Continue. Briefing ready-up remains transient.
        public string ResumeSurface0842;
        public string QuestId = "Q001";
        public QuestRuntimeSaveData0829 QuestRuntime0829;
        // 0.8.56 generalized Q013-Q072 Quest state. Optional fields preserve older-save compatibility.
        public QuestRuntimeState0856 QuestRuntime0856;
        public List<string> QuestCampaignFlags0856 = new();
        public List<FactionReputationState0856> FactionReputation0856 = new();
        public int QuestSpawnSerial0829;
        public int NextQuestSupply0829;
        public bool ReduceFirstWE002TravelScout0830;
        public bool RemoveEliteFromQ007Setup0830;
        public int PartyMorale0831;
        public bool RemoveSiegeEngineFromQ008Setup0831;
        public bool CaptainRolandAssistsQ0100832;
        public CampaignProgressSaveData0833 CampaignProgress0833;
        public QuestResultSaveData0835 QuestResult0835;
        public int Round;
        public int FireTokens;
        public int VillagersRescued;
        public bool QuestComplete;
        public bool QuestFailed;
        // Search / Travel Encounter runtime state (0.8.20+) with active lifecycle state added in 0.8.21.
        public int PartyGold;
        public int GeneralReputation;
        public List<string> LootDeckIds = new();
        public List<string> LootDiscardIds = new();
        public List<string> EquipmentDeckIds = new();
        public List<string> EquipmentDiscardIds = new();
        public string LastSearchCardId;
        public string LastEncounterCardId;
        public int LastEncounterSeverity;
        public string PendingContentInstruction;
        public int NextTravelEncounterSeverityModifier;
        public bool SkipNextTravelEncounter;
        public bool NextGoblinEncounterMovementBonus;
        public bool NextGoblinEncounterWarDrummer;
        // 0.8.52 Campaign I Search round-scoped effects survive save/resume.
        public int SearchTravelSeverityModifier0852;
        public int SearchTravelSeverityRound0852 = -1;
        public int SearchGoblinSeverityModifier0852;
        public int SearchGoblinSeverityRound0852 = -1;
        public bool SearchConditionWard0852;
        public int SearchConditionWardRound0852 = -1;
        public List<string> DeferredPostEncounter0852 = new();
        // 0.8.55 full Crafting runtime persistence.
        public List<string> CraftingSpecialResults0855 = new();
        public List<string> CraftingUnlockFlags0855 = new();
        public List<string> DefeatedEnemyTemplateIds0855 = new();
        // 0.10.2f/g presence flags distinguish an absent choice from JsonUtility's default DTO.
        // Schema 0 remains the lossless bridge for meaningful older/extension payloads.
        public int PendingStateSchema0102f;
        public bool HasPendingEncounterDeckControl0102f;
        public bool HasPendingRewardChoice0102f;
        public EncounterDeckControlSaveData PendingEncounterDeckControl;
        public RewardChoiceSaveData PendingRewardChoice;
        public int EncounterSpawnSerial;
        // 0.8.21 active Travel Encounter lifecycle survives save/resume.
        public int EncounterInstanceSerial;
        public int CompletedEncounterCount;
        public string LastCompletedEncounterCardId;
        public List<EncounterRuntimeSaveData> ActiveEncounters = new();
        public List<EncounterObjectSaveData> EncounterObjects = new();
        public int EncounterObjectSerial;
        public List<string> SearchDeckIds = new();
        public List<string> SearchDiscardIds = new();
        public List<string> EncounterDeckIds = new();
        public List<string> EncounterDiscardIds = new();
        // 0.8.58 — active later-region Encounter deck used by Location First Visit rules.
        public string RegionalEncounterDeckKey0858;
        public List<string> RegionalEncounterDeckIds0858 = new();
        public List<string> RegionalEncounterDiscardIds0858 = new();
        // 0.8.28 — real Exploration deck ordering survives save/resume.
        public List<string> ExplorationDeckIds = new();
        public string SelectedHeroId;
        public string TurnHeroId;
        public string ActiveHeroId; // legacy 0.8.4 field; retained only so older JSON can deserialize safely.
        public string SelectedEnemyId;
        public List<string> PartyHeroIds = new();
        public List<HeroSaveData> Heroes = new();
        public List<EnemySaveData> Enemies = new();
        // 0.8.59 — authored Enemy runtime hazards and per-enemy once/focus state survive save/resume.
        public List<EnemyHazardSaveData0859> EnemyHazards0859 = new();
        public List<LocationSaveData> Locations = new();
        public List<HeroCreatedPieceSaveData> HeroCreatedPieces = new();
    }

    [Serializable]
    public sealed class CampaignProgressSaveData0833
    {
        public string CampaignId0845;
        public bool AwaitingCampaignRuntime0845;
        public List<CampaignArchiveSaveData0845> CampaignArchive0845=new();
        public string CurrentWorldEventId="WE001";
        public int WorldEventDay=1;
        public int TotalCampaignDay=1;
        public bool CampaignCompleted;
        public bool CampaignDefeated;
        public List<string> CompletedQuestIds=new();
        public List<string> FailedQuestIds=new();
        public List<string> MissedQuestIds=new();
        public List<string> CompletedWorldEventIds=new();
        public bool RemoveScoutFromQ004Setup;
        public bool AddRaiderToQ004FirstEncounter;
        public bool AddEliteToQ007Setup;
        public int WE003TravelSeverityPenalty;
        public int DamagedBuildings;
        public int Q010SiegePressureModifier;
        public bool Q010WoundPenalty;
        public bool StonebrookEquipmentPurchaseLocked;
        public bool WorldEventStateInitialized0857;
        public int Suspicion0857;
        public int Instability0857;
        public int Blight0857;
        public int Doomtide0857;
        public int Sunfall0857;
        public List<string> WorldEventFlags0857=new();
        public List<string> WorldEventOnceKeys0857=new();
    }


    [Serializable]
    public sealed class CampaignArchiveSaveData0845
    {
        public string CampaignId;
        public string CampaignTitle;
        public string Outcome;
        public string TitleUnlock;
        public int CampaignDays;
        public List<string> CompletedQuestIds=new();
        public List<string> FailedQuestIds=new();
        public List<string> MissedQuestIds=new();
    }

    [Serializable]
    public sealed class HeroXpDeltaSaveData0835
    {
        public string HeroId;
        public string HeroName;
        public int Delta;
    }

    [Serializable]
    public sealed class QuestResultSaveData0835
    {
        public string QuestId;
        public string QuestName;
        public bool Success;
        public bool CampaignVictory;
        public bool CampaignDefeat;
        public string WorldEventId;
        public string WorldEventName;
        public int WorldEventDay;
        public int TotalCampaignDay;
        public string ObjectiveText;
        public string FailureText;
        public string ResolutionText;
        public string AuthoredRewardText;
        public string ActualRewardSummary;
        public string CampaignImpact;
        public int PartyGoldDelta;
        public int GeneralReputationDelta;
        public int NextQuestSupplyDelta;
        public int PartyMoraleDelta;
        public List<HeroXpDeltaSaveData0835> HeroXpDeltas=new();
        public List<string> GrantedEquipment=new();
        public List<string> RewardLogLines=new();
    }

    [Serializable]
    public sealed class HeroSaveData
    {
        public string Id;
        public int Level;
        public int XP;
        public int Health;
        public int Actions;
        public string LocationId;
        public bool Downed;
        public bool Dead;
        public bool MovedThisTurn;
        public int HealingPotions;
        public int KillCount;
        public int BlacksmithingXp0837;
        public int AlchemyXp0837;
        public int EnchantingXp0837;
        public int ArtificingXp0837;
        public string LastDowntimeActivityKey0839;
        public string LastDowntimeActivityLabel0839;
        public int TurnAttackBonus;
        public int NextAttackBonus;
        public int NextArmorIgnore;
        public bool FirstAttackThisTurn = true;
        public bool TurnCompleted;
        public string TargetLockedEnemyId;
        public int TargetLockedAttackBonus;
        public string FocusEnemyId;
        public int FocusAttackBonus;
        public int AttackCardCostReduction;
        public int AttackCardAttackBonus;
        public bool DrawAfterAttackCard;
        public bool FreeAttackCardReady;
        public int NextAttackRangeConnections;
        public int FreeMoveAttackBonus;
        public bool FreeMoveAttackBonusRequiresEnemy;
        public int SelfDamageAfterNextAttack;
        public int AttackOnlyActions;
        public int NextTurnActionPenalty;
        public string FreeSearchLocationId;
        public string PendingLocationOption;
        public string PendingLocationOptionLocationId;
        public bool FreeMoveReady;
        public bool FreeMoveIgnoresEngagement;
        public bool FreeMoveIgnoresDifficultTerrain;
        public bool BonusAfterFreeMove;
        public bool CannotDropBelowOne;
        public int CannotDropBelowOneTurns;
        public int PendingDiscardCount;
        public bool BattleRoar;
        public bool BloodrageReady;
        public int AvatarRageTurns;
        public bool AvatarFloorActive;
        public List<string> DeckIds = new();
        public List<string> HandIds = new();
        public List<string> DiscardIds = new();
        public List<StatusSaveData> Statuses = new();
        public List<string> UsedRoundAbilities = new();
        public List<string> UsedTurnAbilities = new();
        public List<string> UsedQuestCards = new();
        public List<string> LootInventoryIds = new();
        public List<string> AcquiredEquipmentIds = new();
        public List<string> KeptSearchEffects0852 = new();
        // 0.8.27 — current equipped loadout. Older saves omit this and keep authored starting gear.
        public List<string> EquippedEquipmentIds = new();
    }

    [Serializable] public sealed class EncounterDeckControlSaveData
    {
        public string SourceCardId;
        public string SourceCardName;
        public string Mode;
        public List<string> RevealedCardIds = new();
    }
    [Serializable] public sealed class RewardChoiceSaveData
    {
        public string SourceCardId;
        public string SourceCardName;
        public string HeroId;
        public string Mode;
        public string Prompt;
        public List<string> RevealedCardIds = new();
    }
    [Serializable] public sealed class StatusSaveData
    {
        public string Id;
        public string Name;
        public int RemainingTurns;
        public int RemainingRounds;
        public string Source;
        public bool Beneficial;
        public StatusExpiryTiming ExpiryTiming = StatusExpiryTiming.Persistent;
        public int RemainingExpiryTriggers;
        public string SourceHeroId;
        public int AppliedRound;
    }
    [Serializable] public sealed class EnemySaveData
    {
        public string Id; public string TemplateId; public bool EncounterSpawned; public bool QuestSpawned; public string QuestId; public string EncounterInstanceId; public int MaxHealth; public int Health; public int Attack; public int Armor; public bool IsBoss; public bool ActivatedThisRound; public string LocationId;
        public List<string> UsedKeys0859 = new(); public int MovementStepsThisActivation0859; public string FocusHeroId0859;
        public List<StatusSaveData> Statuses = new();
    }
    [Serializable] public sealed class EnemyHazardSaveData0859
    {
        public string HazardId; public string Type; public string SourceEnemyId; public string LocationId; public int CreatedRound; public int TriggerRound;
    }
    [Serializable] public sealed class EncounterRuntimeSaveData
    {
        public string InstanceId; public string CardId; public string CardName; public string LocationId; public string SourceHeroId; public string Source;
        public int Severity; public int StartedRound; public string RewardText; public string FailureText; public string PostResolveEffect0852; public bool RewardResolved; public bool AllFireTokensRemovedBeforeClear; public bool HadEnemyLeavePlayUndefeated; public int UndefeatedEliteExitCount;
        public List<string> GainedEquipmentIds = new(); public List<string> SpawnedEnemyIds = new(); public List<string> SpawnedObjectIds = new();
    }
    [Serializable] public sealed class EncounterObjectSaveData
    {
        public string ObjectId; public string EncounterInstanceId; public string CardId; public string Type; public string DisplayName; public string LocationId;
        public bool BlocksEncounterCompletion; public bool RemoveAtEncounterEnd; public int InteractionActionCost; public string InteractionLabel; public string AffectedHeroId; public int CreatedRound;
    }
    [Serializable] public sealed class LocationTagSaveData
    {
        public string Tag;
        public string Source;
        public string SourceHeroId;
        public StatusExpiryTiming ExpiryTiming = StatusExpiryTiming.Persistent;
        public int RemainingExpiryTriggers;
    }
    [Serializable] public sealed class LocationSaveData
    {
        public string Id;
        public bool Revealed;
        public bool RevealResolved;
        public bool RevealEffectDeferred;
        public bool FirstVisitResolved;
        public bool Searched;
        public int SearchRequests;
        public int TravelEncounterRequests;
        public List<LocationTagSaveData> TemporaryTags = new();
        public List<string> UsedKeys0858 = new();
    }
    [Serializable] public sealed class HeroCreatedPieceSaveData { public string PieceId; public string Type; public string OwnerHeroId; public string LocationId; public int Health; }

    [Serializable] public sealed class QuestObjectSaveData0829 { public string ObjectId; public string QuestId; public string Type; public string DisplayName; public string LocationId; public string OwnerHeroId; public int Supply; public int Health; public int MaxHealth; public int Armor; public int Attack; public int Range; public bool Rescued; public bool Destroyed; public bool Resolved; }
    [Serializable] public sealed class QuestRuntimeSaveData0829
    {
        public string QuestId; public string QuestName; public bool RewardResolved; public bool CaravanRevealed; public int ReinforcementCount; public bool CaptainSpawned; public bool CaptainDefeated;
        public bool ManifestGranted; public bool StrongboxKeyGranted; public bool StrongboxOpened; public int DeliveredSupply;
        public bool ObjectiveLocationRevealed; public bool BeaconLit; public int ObjectiveEmptyRounds; public bool VaultKeyGranted; public bool VaultOpened; public bool InitialPatrolSpawned; public int TrailMarkers; public bool WarCampRevealed; public bool WarChiefDefeated; public int AlarmTokens; public int SupplyCratesDestroyed;
        public int PrisonersFreed; public int PrisonersRescued; public int PrisonersExecuted; public bool PrisonAlarmSpawned; public bool PrisonReinforcementSpawned;
        public int SupplyWagonsDestroyed; public int SupplyWagonsEscaped; public bool ConvoyFirstDestroyedWave; public bool ConvoySecondDestroyedWave;
        public int WallTrack; public bool Wall4Triggered; public bool Wall2Triggered; public int SiegeEnginesPlaced; public int SiegeEnginesDestroyed; public bool ChampionSpawned; public bool ChampionDefeated;
        public bool RolandRevealed; public bool RolandFreed; public bool RolandRescued; public bool RolandDefeated; public int ExecutionTrack;
        public bool FortressGateRevealed; public int GateHealth; public int GateArmor; public bool GateReinforcementTriggered; public bool GateDestroyed; public int BallistasDestroyed; public bool BallistaRepairPending; public bool BallistaRepairUsed; public string PendingBallistaObjectId; public int GateEmptyRounds;
        public int StrongholdQualifyingReveals; public int ReinforcementTokensRemaining; public bool HighShamanWaiting; public bool ThroneRoomRevealed;
        public int BannersDestroyed; public bool GoblinKingLastStandTriggered; public int GoblinKingHordeRound; public bool CampaignVictory; public bool DefenderOfStonebrookUnlocked;
        public List<string> QuestOwnedEnemyIds=new(); public List<QuestObjectSaveData0829> Objects=new();
    }
}
