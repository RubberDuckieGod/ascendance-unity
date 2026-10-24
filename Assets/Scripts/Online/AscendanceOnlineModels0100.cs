using System;
using System.Collections.Generic;

namespace Ascendance.Online
{
    public enum ServerCommandType0100
    {
        StartTurn,
        EndTurn,
        Move,
        BasicAttack,
        PlayCard,
        Search,
        Explore,
        QuestPrimaryAction,
        QuestSecondaryAction,
        UseHealingPotion,
        UseReaction,
        ResolveReaction,
        ChooseRewardChoice,
        ChooseEncounterDeckControl,
        SetQuestReady,
        ConfirmQuestBriefing,
        BeginCampaignQuest,
        AdvanceWorldEvent,
        UseLocationRevealOption,
        InteractEncounterObject,
        BeginInventoryManagement,
        BeginEquipmentManagement,
        OpenQuestBriefing,
        CancelQuestBriefing,
        RecoverHero,
        RecoverParty,
        WorkDowntime,
        GatherResources,
        EquipDowntimeEquipment,
        UnequipDowntimeEquipment,
        PurchaseMerchantEquipment,
        CraftRecipe,
        TransferHealingPotion,
        TransferHeroLoot,
        TransferAllHeroLoot,
        TransferEquipment,
        ContinueCampaign
    }

    [Serializable]
    public sealed class ServerCommandEnvelope0100
    {
        public string CommandId;
        public string RoomId;
        public string PlayerId;
        public long ExpectedRevision;
        // Presence changes do not invalidate gameplay decisions made from the same game state.
        public bool HasExpectedGameplayRevision;
        public long ExpectedGameplayRevision;
        public string ExpectedServerInstanceId;
        public ServerCommandType0100 Type;
        public string ActorHeroId;
        public string TargetId;
        public string SecondaryTargetId;
        public string CardId;
        public string OptionKey;
        public string StringValue;
        public int IntValue;
        public bool BoolValue;
    }

    [Serializable]
    public sealed class ServerCommandResult0100
    {
        public bool Accepted;
        public bool Duplicate;
        public string Code;
        public string Message;
        public long Revision;
        public ServerStateSnapshot0100 Snapshot;
    }

    [Serializable]
    public sealed class ServerPlayerSession0100
    {
        public string PlayerId;
        public string DisplayName;
        public string HeroId;
        public string ReconnectToken;
        public bool Connected;
        public string JoinedAtUtc;
        public string LastSeenUtc;
    }

    [Serializable]
    public sealed class ServerPlayerView0100
    {
        public string PlayerId;
        public string DisplayName;
        public string HeroId;
        public bool Connected;
    }

    [Serializable]
    public sealed class ServerHeroMovement0102
    {
        public string HeroId;
        public string PendingLocationId;
    }

    [Serializable]
    public sealed class ServerTransientState0100
    {
        public string TurnHeroId;
        public string SelectedHeroId;
        public string SelectedEnemyId;
        public bool IsResolving;
        public bool HasPendingReactionWindow;
        public string PendingReactionKind;
        public string PendingReactionText;
        public List<string> ReactionOptions = new();
        public bool HasPendingRewardChoice;
        public string PendingRewardChoiceText;
        public List<string> RewardOptions = new();
        public bool HasPendingEncounterDeckControl;
        public string PendingEncounterDeckControlText;
        public List<string> EncounterDeckControlOptions = new();
        public List<string> MovingHeroIds = new();
        public List<ServerHeroMovement0102> HeroMovement = new();
        public List<string> RecentLog = new();
    }

    [Serializable]
    public sealed class ServerStateSnapshot0100
    {
        public string RoomId;
        public string BuildVersion;
        public long Revision;
        public long GameplayRevision;
        public string ServerInstanceId;
        public AscendanceSaveData Campaign;
        public ServerTransientState0100 Transient;
        public List<ServerPlayerView0100> Players = new();
    }

    [Serializable]
    public sealed class PersistedServerRoom0100
    {
        public string RoomId;
        public string BuildVersion;
        public long Revision;
        public long GameplayRevision;
        public string CreatedAtUtc;
        public string UpdatedAtUtc;
        public AscendanceSaveData DurableCampaign;
        public List<ServerPlayerSession0100> Players = new();
    }

    public sealed class ServerJoinResult0100
    {
        public bool Accepted;
        public string Code;
        public string Message;
        public ServerPlayerSession0100 Player;
        public ServerStateSnapshot0100 Snapshot;
    }
}
