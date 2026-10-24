using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    [Serializable]
    public sealed class HeroState
    {
        public string Id;
        public string HeroCardId;
        public string DisplayName;
        public string ClassName;
        public string PrimaryRole;
        public string SecondaryRole;
        public string Traits;
        public string CoreMechanic;
        public int PartyIndex;
        public bool Participating;
        public int Level = 1;
        public int XP;
        public int MaxHealth = 12;
        public int Health = 12;
        public int BaseArmor;
        public int BaseAttack;
        public int Armor = 2;
        public int Attack = 3;
        public int DrawValue = 1;
        public int Actions = 0;
        public string LocationId = "L000";
        public string PendingLocationId;
        public bool Moving;
        public bool Downed;
        public bool Dead;
        public bool MovedThisTurn;
        public int HealingPotions = 1;
        public int KillCount;
        // 0.8.37 — per-Hero profession progression. Materials are also carried per Hero in LootInventory.
        public int BlacksmithingXp0837;
        public int AlchemyXp0837;
        public int EnchantingXp0837;
        public int ArtificingXp0837;
        // 0.8.39 — one dedicated activity per Hero per resolved-Quest downtime period.
        public string LastDowntimeActivityKey0839;
        public string LastDowntimeActivityLabel0839;

        // Shared runtime modifiers. These replace one-off hero-only fields as systems migrate.
        public int TurnAttackBonus;
        public int NextAttackBonus;
        public int NextArmorIgnore;
        public bool FreeMoveReady;
        public bool FreeMoveIgnoresEngagement;
        public bool FreeMoveIgnoresDifficultTerrain;
        public bool BonusAfterFreeMove;
        public bool CannotDropBelowOne;
        public int CannotDropBelowOneTurns;
        public int PendingDiscardCount;
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
        // 0.8.20 Encounter hazards can tax the next Hero activation without spending current-turn Actions.
        public int NextTurnActionPenalty;

        // 0.8.19 Location-effect permissions. These are explicit state so optional Reveal effects
        // remain player choices and free Search survives save/load until used or invalidated.
        public string FreeSearchLocationId;
        public string PendingLocationOption;
        public string PendingLocationOptionLocationId;

        // Compatibility flags retained for the verified Garruk card pipeline.
        public bool BattleRoar;
        public bool BloodrageReady;
        public int AvatarRageTurns;
        public bool AvatarFloorActive;

        public List<string> Deck = new();
        public List<CardData> Hand = new();
        public List<string> Discard = new();
        public List<StatusInstance> Statuses = new();
        public List<string> UsedRoundAbilities = new();
        public List<string> UsedTurnAbilities = new();
        public List<string> UsedQuestCards = new();
        public List<HeroAbilityDefinition> Abilities = new();
        // Initialized from authored Starting Equipment; from 0.8.27 this list is the Hero's current equipped loadout.
        public List<EquipmentRuntimeData> StartingEquipment = new();
        // 0.8.26+ — persistent rewards gained from Loot/Equipment decks. Acquired Equipment remains unequipped until the player swaps it in.
        public List<string> LootInventory = new();
        public List<string> AcquiredEquipment = new();
        // 0.8.52 — Search cards explicitly kept for later discard/use (e.g. SD027/SD089).
        public List<string> KeptSearchEffects0852 = new();

        public bool IsDefeated => Downed || Dead || Health <= 0;
        public bool HasStatus(string name) => Statuses.Exists(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    [Serializable]
    public sealed class EnemyState
    {
        public string Id;
        // Encounter-spawned enemies retain their authored E### template separately from their unique runtime ID.
        public string TemplateId;
        public bool EncounterSpawned;
        // 0.8.29 quest-owned dynamic enemy identity.
        public bool QuestSpawned;
        public string QuestId;
        // 0.8.21 ties a spawned Enemy to the exact Travel Encounter instance that created it.
        public string EncounterInstanceId;
        public string DisplayName;
        public int MaxHealth;
        public int Health;
        public int Armor;
        public int Attack;
        public string LocationId;
        public bool IsBoss;
        public bool ActivatedThisRound;
        // 0.8.59 — authored Enemy runtime state. Used keys persist once-per-Round/Encounter mechanics across saves.
        public List<string> UsedKeys0859 = new();
        public int MovementStepsThisActivation0859;
        public string FocusHeroId0859;
        public List<StatusInstance> Statuses = new();
        public bool HasStatus(string name) => Statuses.Exists(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        public bool Defeated => Health <= 0;
    }

    [Serializable]
    public sealed class EncounterObjectState
    {
        public string ObjectId;
        public string EncounterInstanceId;
        public string CardId;
        public string Type;
        public string DisplayName;
        public string LocationId;
        public bool BlocksEncounterCompletion;
        public bool RemoveAtEncounterEnd;
        public int InteractionActionCost = -1;
        public string InteractionLabel;
        public string AffectedHeroId;
        public int CreatedRound;
    }

    [Serializable]
    public sealed class EncounterRuntimeState
    {
        public string InstanceId;
        public string CardId;
        public string CardName;
        public string LocationId;
        public string SourceHeroId;
        public string Source;
        public int Severity;
        public int StartedRound;
        public string RewardText;
        public string FailureText;
        public bool RewardResolved;
        public bool AllFireTokensRemovedBeforeClear;
        // 0.8.24 — tracks Scout Patrol/Chieftain's Scouts non-defeat exits separately from kills.
        public bool HadEnemyLeavePlayUndefeated;
        // 0.8.26 — encounter-owned reward state used by reversible rewards such as EN017.
        public int UndefeatedEliteExitCount;
        public List<string> GainedEquipmentIds = new();
        public List<string> SpawnedEnemyIds = new();
        public List<string> SpawnedObjectIds = new();
        // 0.8.52 — authored Search/Location reward that resolves only after this encounter clears.
        public string PostResolveEffect0852;
    }

    [Serializable]
    public sealed class EncounterDeckControlState
    {
        public string SourceCardId;
        public string SourceCardName;
        public string Mode;
        public List<string> RevealedCardIds = new();
    }

    public sealed class EncounterDeckControlOption
    {
        public string Key;
        public string Label;
    }

    [Serializable]
    public sealed class RewardChoiceState
    {
        public string SourceCardId;
        public string SourceCardName;
        public string HeroId;
        public string Mode;
        public string Prompt;
        // 0.8.27 — Search reward choices can hold cards removed from a live deck until the player resolves the choice.
        public List<string> RevealedCardIds = new();
    }

    public sealed class RewardChoiceOption
    {
        public string Key;
        public string Label;
    }

    [Serializable]
    public sealed class CardData
    {
        public string Id;
        public string HeroId;
        public string HeroCardId;
        public int Level;
        public int Copies = 1;
        public string Name;
        public string Category;
        public int Cost = 1;
        public string Speed;
        public string TargetRange;
        public string Target;
        public string Traits;
        public string Keywords;
        public string Rules;
        public string Flavor;

        public CardKind Kind
        {
            get
            {
                if (string.Equals(Speed, "Reaction", StringComparison.OrdinalIgnoreCase)) return CardKind.Reaction;
                if ((!string.IsNullOrEmpty(Category) && Category.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0) || (!string.IsNullOrEmpty(Rules) && Rules.IndexOf("Make an Attack", StringComparison.OrdinalIgnoreCase) >= 0)) return CardKind.Attack;
                if (!string.IsNullOrEmpty(Category) && Category.IndexOf("Movement", StringComparison.OrdinalIgnoreCase) >= 0) return CardKind.Movement;
                if (!string.IsNullOrEmpty(Category) && Category.IndexOf("Protection", StringComparison.OrdinalIgnoreCase) >= 0) return CardKind.Protection;
                return CardKind.Support;
            }
        }

        public CardData CloneRuntime()
        {
            return new CardData
            {
                Id = Id, HeroId = HeroId, HeroCardId = HeroCardId, Level = Level, Copies = Copies,
                Name = Name, Category = Category, Cost = Cost, Speed = Speed, TargetRange = TargetRange,
                Target = Target, Traits = Traits, Keywords = Keywords, Rules = Rules, Flavor = Flavor
            };
        }
    }

    public enum CardKind
    {
        Support,
        Attack,
        Movement,
        Protection,
        Reaction
    }

    public enum StatusExpiryTiming
    {
        Persistent = 0,
        StartOfTargetTurn = 1,
        EndOfTargetTurn = 2,
        StartOfSourceTurn = 3,
        EndOfSourceTurn = 4,
        EndOfTargetActivation = 5,
        EndOfRound = 6
    }

    [Serializable]
    public sealed class StatusInstance
    {
        public string Id;
        public string Name;
        // Legacy counters are retained for backward-compatible 0.8.14-and-earlier saves.
        // 0.8.15 runtime expiry uses ExpiryTiming + RemainingExpiryTriggers instead.
        public int RemainingTurns;
        public int RemainingRounds;
        public string Source;
        public bool Beneficial;
        public StatusExpiryTiming ExpiryTiming = StatusExpiryTiming.Persistent;
        public int RemainingExpiryTriggers;
        public string SourceHeroId;
        public int AppliedRound;
    }

    [Serializable]
    public sealed class HeroCreatedPiece
    {
        public string PieceId;
        public string Type;
        public string OwnerHeroId;
        public string LocationId;
        public int Health;
    }

    [Serializable]
    public sealed class EquipmentRuntimeData
    {
        public string Id;
        public string Name;
        public string Type;
        public int Copies = 1;
        public int Attack;
        public int Armor;
        public string Ability1;
        public string Ability2;
    }

    [Serializable]
    public sealed class HeroAbilityDefinition
    {
        public int Level;
        public int Slot;
        public string Name;
        public string Text;
    }

    [Serializable]
    public sealed class LocationTagInstance
    {
        public string Tag;
        public string Source;
        public string SourceHeroId;
        public StatusExpiryTiming ExpiryTiming = StatusExpiryTiming.Persistent;
        public int RemainingExpiryTriggers;
    }

    public sealed class LocationState
    {
        public string Id;
        public string Name;
        public string AuthoredLocationId;
        public string Terrain;
        public string Traits;
        public string Keywords;
        public string RevealEffect;
        public string FirstVisitEffect;
        public string OngoingEffect;
        public Vector3 Position;
        public bool Revealed;
        public bool RevealResolved;
        // 0.8.28 — SD005 may reveal a Location while deferring its authored Reveal Effect until entry.
        public bool RevealEffectDeferred;
        public bool FirstVisitResolved;
        public bool Searched;
        public int SearchRequests;
        public int TravelEncounterRequests;
        public readonly List<string> Connections = new();
        public readonly List<LocationTagInstance> TemporaryTags = new();
        // 0.8.58 — persistent once-per-Quest / once-per-Round Location rule keys.
        public readonly List<string> UsedKeys0858 = new();

        public bool HasPrintedTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            bool Match(string value) => !string.IsNullOrWhiteSpace(value) && value.Split(';').ExistsPart(tag);
            return Match(Terrain) || Match(Traits) || Match(Keywords);
        }
    }

    internal static class LocationTagStringExtensions
    {
        public static bool ExistsPart(this string[] values, string token)
        {
            foreach (var value in values) if (string.Equals(value.Trim(), token, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
