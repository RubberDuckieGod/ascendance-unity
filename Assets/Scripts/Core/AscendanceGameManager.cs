using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    [Serializable]
    public sealed class ReactionOption
    {
        public string HeroId;
        public string CardId;
        public string Label;
    }

    public enum ReactionWindowKind
    {
        None,
        BeforeEnemyAttack,
        BeforeHeroDamage,
        AfterEnemyAttack,
        AfterHeroKill
    }

    public enum HeroDamageSourceKind
    {
        Direct,
        HeroCard,
        Condition,
        EnemyAttack,
        EnemyAbility
    }

    public sealed partial class AscendanceGameManager : MonoBehaviour
    {
        public const string BuildVersion = "0.10.2c";
        public static AscendanceGameManager Instance { get; private set; }

        public readonly Dictionary<string, HeroState> Heroes = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, EnemyState> Enemies = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, LocationState> Locations = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, HeroAvatar> HeroAvatars = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, EnemyAvatar> EnemyAvatars = new(StringComparer.OrdinalIgnoreCase);
        public readonly List<HeroCreatedPiece> HeroCreatedPieces = new();
        public readonly List<ReactionOption> PendingReactionOptions = new();

        public string SelectedHeroId { get; private set; } = "Garruk";
        public string TurnHeroId { get; private set; }
        public string SelectedEnemyId { get; private set; }
        public int Round { get; private set; } = 1;
        public int FireTokens { get; private set; }
        public int VillagersRescued { get; private set; }
        public int PartyGold { get; private set; }
        public int GeneralReputation { get; private set; }
        public readonly List<string> LootDeck = new();
        public readonly List<string> LootDiscard = new();
        public readonly List<string> EquipmentDeck = new();
        public readonly List<string> EquipmentDiscard = new();
        public string LastSearchCardId { get; private set; }
        public string LastEncounterCardId { get; private set; }
        public int LastEncounterSeverity { get; private set; }
        public string PendingContentInstruction { get; private set; }
        // 0.8.23 cross-Encounter travel-control state. These are consumed only when a later eligible Encounter actually resolves.
        public int NextTravelEncounterSeverityModifier { get; private set; }
        public bool SkipNextTravelEncounter { get; private set; }
        // 0.8.24 — EN016 can empower the next Goblin Encounter for its first round.
        public bool NextGoblinEncounterMovementBonus { get; private set; }
        public bool NextGoblinEncounterWarDrummer { get; private set; }
        public EncounterDeckControlState PendingEncounterDeckControl { get; private set; }
        public RewardChoiceState PendingRewardChoice { get; private set; }
        public bool HasPendingEncounterDeckControl => PendingEncounterDeckControl != null;
        public bool HasPendingRewardChoice => PendingRewardChoice != null;
        public string PendingEncounterDeckControlText => PendingEncounterDeckControl==null?string.Empty:$"{PendingEncounterDeckControl.SourceCardId} {PendingEncounterDeckControl.SourceCardName}: {DeckControlModeDescription(PendingEncounterDeckControl.Mode)} — {string.Join(" | ",PendingEncounterDeckControl.RevealedCardIds.Select(EncounterCardLabel))}";
        public IReadOnlyList<EncounterDeckControlOption> EncounterDeckControlOptions => BuildEncounterDeckControlOptions();
        public string PendingRewardChoiceText => PendingRewardChoice==null?string.Empty:$"{PendingRewardChoice.SourceCardId} {PendingRewardChoice.SourceCardName}: {PendingRewardChoice.Prompt}";
        // Runtime-safe accessor. Unlike the Debug* validator helpers, this must exist in standalone Player builds.
        public string PendingRewardChoiceMode => PendingRewardChoice?.Mode;
        public IReadOnlyList<RewardChoiceOption> RewardChoiceOptions => BuildRewardChoiceOptions();
        public readonly List<string> SearchDeck = new();
        public readonly List<string> SearchDiscard = new();
        public readonly List<string> EncounterDeck = new();
        public readonly List<string> EncounterDiscard = new();
        public readonly List<EncounterRuntimeState> ActiveEncounters = new();
        public readonly List<EncounterObjectState> EncounterObjects = new();
        private int encounterSpawnSerial;
        private int encounterInstanceSerial;
        private int encounterObjectSerial;
        public bool QuestComplete { get; private set; }
        public bool QuestFailed { get; private set; }
        public bool IsResolving { get; private set; }
        public bool HasPendingReactionWindow => pendingReactionKind != ReactionWindowKind.None;
        public ReactionWindowKind PendingReactionKind => pendingReactionKind;
        public string PendingReactionText
        {
            get
            {
                if (pendingReactionKind == ReactionWindowKind.BeforeEnemyAttack) return $"{pendingEnemy?.DisplayName} attacks {pendingTarget?.DisplayName}. Choose reactions, then Resolve Attack.";
                if (pendingReactionKind == ReactionWindowKind.BeforeHeroDamage) return $"{pendingTarget?.DisplayName} would suffer {pendingIncomingDamage} damage ({pendingDamageSource}). Choose reactions, then Resolve Damage.";
                if (pendingReactionKind == ReactionWindowKind.AfterEnemyAttack) return $"{pendingEnemy?.DisplayName}'s Attack against {pendingTarget?.DisplayName} resolved for {pendingLastDamage} damage. Choose any triggered reactions.";
                if (pendingReactionKind == ReactionWindowKind.AfterHeroKill) return $"{pendingEventHero?.DisplayName} defeated {pendingEnemy?.DisplayName}. Choose any triggered reactions.";
                return string.Empty;
            }
        }

        public IEnumerable<HeroState> ParticipatingHeroes => Heroes.Values.Where(h => h.Participating).OrderBy(h => h.PartyIndex);
        public int PartySize => Heroes.Values.Count(h => h.Participating);
        public List<string> PartyHeroIds => ParticipatingHeroes.Select(h => h.Id).ToList();
        public int EncounterSpawnSerialForSave => encounterSpawnSerial;
        public int EncounterInstanceSerialForSave => encounterInstanceSerial;
        public int EncounterObjectSerialForSave => encounterObjectSerial;
        public int CompletedEncounterCount { get; private set; }
        public string LastCompletedEncounterCardId { get; private set; }
        public string ActiveEncounterSummary => ActiveEncounters.Count == 0 ? string.Empty : string.Join(" | ", ActiveEncounters.Select(e => $"{e.CardId} {e.CardName}: {RemainingEncounterEnemies(e)} enemies · {RemainingEncounterBlockingObjects(e)} objectives"));
        public string EncounterObjectSummary => EncounterObjects.Count == 0 ? string.Empty : string.Join(" | ", EncounterObjects.Select(o => $"{o.DisplayName}@{(Locations.TryGetValue(o.LocationId,out var loc)?loc.Name:o.LocationId)}"));

        public event Action StateChanged;
        public event Action<string> LogAdded;
        // Compatibility hooks remain available for UI/debug subscribers. Search/Travel draw, Encounter lifecycle, and reward resolution are runtime-owned.
        public event Action<LocationState,HeroState> SearchRequested;
        public event Action<LocationState,HeroState,int> TravelEncounterRequested;

        private readonly List<string> log = new();
        public IReadOnlyList<string> Log => log;

        private EnemyState pendingEnemy;
        private HeroState pendingTarget;
        private HeroState pendingEventHero;
        private ReactionWindowKind pendingReactionKind;
        private int pendingLastDamage;
        private int pendingEnemyDie;
        private int pendingArmorBonus;
        private int pendingDamageReduction;
        private bool pendingPreventDefeat;
        private bool pendingCancelAttack;
        private bool pendingReactionResolved;
        private int pendingIncomingDamage;
        private string pendingDamageSource;
        private HeroDamageSourceKind pendingDamageSourceKind;
        private EnemyState pendingDamageSourceEnemy;
        private bool pendingGrantInspiredIfDamagePrevented;
        private Action<int> pendingDamageContinuation;
        private HeroState queuedDamageTarget;
        private int queuedDamageAmount;
        private string queuedDamageSource;
        private HeroDamageSourceKind queuedDamageSourceKind;
        private EnemyState queuedDamageSourceEnemy;
        private Action<int> queuedDamageContinuation;
        private bool queuedDamagePreventDefeat;

        private void Awake() => Instance = this;

        public void Initialize()
        {
            HeroDatabase.EnsureLoaded();
            BuildLocations();
            BuildHeroes();
            BuildEnemies();
            InitializeQuestRuntime0829();
            InitializeCampaignProgress0833();
            InitializeSearchEncounterDecks();
            InitializeRewardDecks();
            InitializeExplorationDeck0828();
            // Party selection and Quest-start hooks are intentionally deferred to
            // AscendanceSession.ApplyTo(). This prevents the default party from briefly
            // firing setup abilities before a New Game selection or saved party is applied.
            AddLog($"Ascendance Unity {BuildVersion} — Real Network Transport loaded.");
            AddLog($"HERO DATA — {Heroes.Count} authored Heroes · {HeroDatabase.CardDefinitionCount} Hero-card definitions · 30-card personal decks.");
            AddLog($"AUTHORED CONTENT — {FullContentCatalog0850.LootCount} Loot • {FullContentCatalog0850.EquipmentCount} Equipment • {FullContentCatalog0850.SearchCardCount} Search • {FullContentCatalog0850.EncounterCount} Encounters • {FullContentCatalog0850.EnemyCount} Enemies • {FullContentCatalog0850.LocationCount} Locations • {FullContentCatalog0850.CraftingRecipeCount} Crafting Recipes • {FullContentCatalog0850.QuestCount} Quests • {FullContentCatalog0850.WorldEventCount} World Events.");
            AddLog("QUEST RUNTIME — Q001-Q012 specialized scenarios preserved; Q013-Q072 generalized authored Quest execution active.");
            AddLog("Hero attacks automatically hit. Enemy attacks roll 1d6.");
            StateChanged?.Invoke();
        }

        public void SetParty(IEnumerable<string> heroIds, string startingHeroId = null, bool runQuestStartHooks = true)
        {
            var ids = (heroIds ?? HeroDatabase.DefaultParty).Where(id => Heroes.ContainsKey(id)).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();
            if (ids.Count == 0) ids.AddRange(HeroDatabase.DefaultParty.Where(Heroes.ContainsKey).Take(4));
            foreach (var hero in Heroes.Values)
            {
                hero.Participating = false;
                hero.PartyIndex = -1;
                hero.Actions = 0;
                hero.AttackOnlyActions = 0;
                hero.TurnCompleted = false;
                hero.MovedThisTurn = false;
            }
            for (int i = 0; i < ids.Count; i++)
            {
                var hero = Heroes[ids[i]];
                hero.Participating = true;
                hero.PartyIndex = i;
                HeroRulesRuntime.ResetRound(hero);
                HeroRulesRuntime.ResetTurn(hero);
                if (runQuestStartHooks) HeroRulesRuntime.OnQuestStart(this, hero);
            }
            if(runQuestStartHooks){ConfigureInitialQuestParty0850();OnLootOrEquipmentQuestStart0854();}
            SelectedHeroId = !string.IsNullOrEmpty(startingHeroId) && ids.Contains(startingHeroId, StringComparer.OrdinalIgnoreCase) ? startingHeroId : ids[0];
            TurnHeroId = null;
            SelectedEnemyId = FirstLivingEnemyAtSelectedHeroLocation()?.Id;
        }

        public void SetStartingHero(string heroId)
        {
            if (!string.IsNullOrEmpty(heroId) && Heroes.TryGetValue(heroId, out var hero) && hero.Participating)
                SelectedHeroId = heroId;
            TurnHeroId = null;
            SelectedEnemyId = FirstLivingEnemyAtSelectedHeroLocation()?.Id;
            AddLog($"HERO PHASE — Round {Round}. Players choose which unfinished Hero takes the next turn.");
            StateChanged?.Invoke();
        }

        public void ApplySaveData(AscendanceSaveData save)
        {
            if (save == null) return;
            if (save.PartyHeroIds != null && save.PartyHeroIds.Count > 0) SetParty(save.PartyHeroIds, save.SelectedHeroId, false);
            else if (save.Heroes != null && save.Heroes.Count > 0) SetParty(save.Heroes.Select(h => h.Id), save.SelectedHeroId, false);

            Round = Mathf.Max(1, save.Round);
            FireTokens = Mathf.Clamp(save.FireTokens, 0, 5);
            VillagersRescued = Mathf.Clamp(save.VillagersRescued, 0, 4);
            QuestComplete = save.QuestComplete;
            QuestFailed = !QuestComplete && save.QuestFailed;
            RestoreQuestRuntime0829(save);
            RestoreCampaignProgress0833(save);
            RestoreCraftingState0855(save);
            RestoreQuestResult0835(save);
            PartyGold = Mathf.Max(0, save.PartyGold);
            GeneralReputation = Mathf.Clamp(save.GeneralReputation,-5,6);
            LastSearchCardId = save.LastSearchCardId;
            LastEncounterCardId = save.LastEncounterCardId;
            LastEncounterSeverity = Mathf.Max(0, save.LastEncounterSeverity);
            PendingContentInstruction = save.PendingContentInstruction;
            NextTravelEncounterSeverityModifier = save.NextTravelEncounterSeverityModifier;
            SkipNextTravelEncounter = save.SkipNextTravelEncounter;
            NextGoblinEncounterMovementBonus = save.NextGoblinEncounterMovementBonus;
            NextGoblinEncounterWarDrummer = save.NextGoblinEncounterWarDrummer;
            RestoreCampaignISearchState0852(save);
            PendingEncounterDeckControl = save.PendingEncounterDeckControl==null?null:new EncounterDeckControlState
            {
                SourceCardId=save.PendingEncounterDeckControl.SourceCardId,SourceCardName=save.PendingEncounterDeckControl.SourceCardName,Mode=save.PendingEncounterDeckControl.Mode,
                RevealedCardIds=save.PendingEncounterDeckControl.RevealedCardIds==null?new List<string>():save.PendingEncounterDeckControl.RevealedCardIds.ToList()
            };
            PendingRewardChoice = save.PendingRewardChoice==null?null:new RewardChoiceState
            {
                SourceCardId=save.PendingRewardChoice.SourceCardId,SourceCardName=save.PendingRewardChoice.SourceCardName,HeroId=save.PendingRewardChoice.HeroId,Mode=save.PendingRewardChoice.Mode,Prompt=save.PendingRewardChoice.Prompt,
                RevealedCardIds=save.PendingRewardChoice.RevealedCardIds==null?new List<string>():save.PendingRewardChoice.RevealedCardIds.ToList()
            };
            // 0.8.36b — A resolved Quest save represents Stonebrook downtime, not an in-Quest modal.
            // Reward/deck-control prompts belong to live Quest resolution and must not survive as blockers
            // after QuestComplete/QuestFailed has already been committed. Mid-Quest saves still restore them.
            if(QuestComplete||QuestFailed)
            {
                PendingEncounterDeckControl=null;
                PendingRewardChoice=null;
            }
            encounterSpawnSerial = Mathf.Max(0, save.EncounterSpawnSerial);
            encounterInstanceSerial = Mathf.Max(0, save.EncounterInstanceSerial);
            encounterObjectSerial = Mathf.Max(0, save.EncounterObjectSerial);
            CompletedEncounterCount = Mathf.Max(0, save.CompletedEncounterCount);
            LastCompletedEncounterCardId = save.LastCompletedEncounterCardId;
            RestoreSearchEncounterDeckState(save);
            RestoreRegionalEncounterState0858(save);
            ClearDeferredLocationPostSearch0858();
            RestoreRewardDeckState(save);
            // 0.8.36a — Reaction windows are intentionally not serialized (saving is already blocked
            // while one is open). Loading into an existing Play Mode session must therefore clear
            // any stale in-memory reaction/queued-damage state from the pre-load runtime, otherwise
            // CanBeginCampaignQuest0833 can be invisibly blocked after returning to Stonebrook.
            CloseReactionWindow();
            IsResolving = false;

            if (save.Locations != null)
                foreach (var stored in save.Locations)
                    if (stored != null && Locations.TryGetValue(stored.Id, out var location))
                    {
                        location.Revealed = stored.Revealed;
                        location.RevealEffectDeferred = stored.RevealEffectDeferred;
                        // Older saves did not persist deferred Reveal Effects, so revealed Locations remain resolved unless 0.8.28 explicitly marked the effect deferred.
                        location.RevealResolved = stored.RevealEffectDeferred ? stored.RevealResolved : (stored.RevealResolved || stored.Revealed);
                        location.FirstVisitResolved = stored.FirstVisitResolved;
                        location.Searched = stored.Searched;
                        location.SearchRequests = Mathf.Max(0, stored.SearchRequests);
                        location.TravelEncounterRequests = Mathf.Max(0, stored.TravelEncounterRequests);
                        location.TemporaryTags.Clear();
                        location.UsedKeys0858.Clear();
                        if(stored.UsedKeys0858!=null)location.UsedKeys0858.AddRange(stored.UsedKeys0858.Where(x=>!string.IsNullOrWhiteSpace(x)));
                        if (stored.TemporaryTags != null)
                            foreach (var t in stored.TemporaryTags)
                                if (t != null && !string.IsNullOrWhiteSpace(t.Tag))
                                    location.TemporaryTags.Add(new LocationTagInstance { Tag=t.Tag, Source=t.Source, SourceHeroId=t.SourceHeroId, ExpiryTiming=t.ExpiryTiming, RemainingExpiryTriggers=Mathf.Max(0,t.RemainingExpiryTriggers) });
                    }

            RestoreExplorationDeckState0828(save);

            if (save.Heroes != null)
            {
                foreach (var stored in save.Heroes)
                {
                    if (stored == null || !Heroes.TryGetValue(stored.Id, out var hero)) continue;
                    hero.Health = Mathf.Clamp(stored.Health, 0, hero.MaxHealth);
                    hero.Actions = Mathf.Clamp(stored.Actions, 0, 3);
                    hero.XP = Mathf.Max(0, stored.XP);
                    hero.Level = Mathf.Max(1, stored.Level <= 0 ? hero.Level : stored.Level);
                    if (!string.IsNullOrEmpty(stored.LocationId) && Locations.ContainsKey(stored.LocationId)) hero.LocationId = stored.LocationId;
                    hero.PendingLocationId = null;
                    hero.Moving = false;
                    hero.Downed = stored.Downed;
                    hero.Dead = stored.Dead;
                    hero.MovedThisTurn = stored.MovedThisTurn;
                    bool legacyHeroSave = (stored.DeckIds == null || stored.DeckIds.Count == 0) && (stored.HandIds == null || stored.HandIds.Count == 0);
                    hero.HealingPotions = legacyHeroSave ? 1 : Mathf.Max(0, stored.HealingPotions);
                    hero.KillCount = Mathf.Max(0, stored.KillCount);
                    hero.BlacksmithingXp0837 = Mathf.Max(0, stored.BlacksmithingXp0837);
                    hero.AlchemyXp0837 = Mathf.Max(0, stored.AlchemyXp0837);
                    hero.EnchantingXp0837 = Mathf.Max(0, stored.EnchantingXp0837);
                    hero.ArtificingXp0837 = Mathf.Max(0, stored.ArtificingXp0837);
                    hero.LastDowntimeActivityKey0839 = stored.LastDowntimeActivityKey0839;
                    hero.LastDowntimeActivityLabel0839 = stored.LastDowntimeActivityLabel0839;
                    hero.TurnAttackBonus = stored.TurnAttackBonus;
                    hero.NextAttackBonus = stored.NextAttackBonus;
                    hero.NextArmorIgnore = stored.NextArmorIgnore;
                    hero.FirstAttackThisTurn = stored.FirstAttackThisTurn;
                    hero.TurnCompleted = stored.TurnCompleted;
                    hero.TargetLockedEnemyId = stored.TargetLockedEnemyId;
                    hero.TargetLockedAttackBonus = stored.TargetLockedAttackBonus;
                    hero.FocusEnemyId = stored.FocusEnemyId;
                    hero.FocusAttackBonus = stored.FocusAttackBonus;
                    hero.AttackCardCostReduction = stored.AttackCardCostReduction;
                    hero.AttackCardAttackBonus = stored.AttackCardAttackBonus;
                    hero.DrawAfterAttackCard = stored.DrawAfterAttackCard;
                    hero.FreeAttackCardReady = stored.FreeAttackCardReady;
                    hero.NextAttackRangeConnections = stored.NextAttackRangeConnections;
                    hero.FreeMoveReady = stored.FreeMoveReady;
                    hero.FreeMoveIgnoresEngagement = stored.FreeMoveIgnoresEngagement;
                    hero.FreeMoveIgnoresDifficultTerrain = stored.FreeMoveIgnoresDifficultTerrain;
                    hero.BonusAfterFreeMove = stored.BonusAfterFreeMove;
                    hero.FreeMoveAttackBonus = stored.FreeMoveAttackBonus;
                    hero.FreeMoveAttackBonusRequiresEnemy = stored.FreeMoveAttackBonusRequiresEnemy;
                    hero.SelfDamageAfterNextAttack = stored.SelfDamageAfterNextAttack;
                    hero.AttackOnlyActions = Mathf.Clamp(stored.AttackOnlyActions, 0, hero.Actions);
                    hero.NextTurnActionPenalty = Mathf.Max(0, stored.NextTurnActionPenalty);
                    hero.FreeSearchLocationId = stored.FreeSearchLocationId;
                    hero.PendingLocationOption = stored.PendingLocationOption;
                    hero.PendingLocationOptionLocationId = stored.PendingLocationOptionLocationId;
                    hero.CannotDropBelowOne = stored.CannotDropBelowOne;
                    hero.CannotDropBelowOneTurns = stored.CannotDropBelowOneTurns;
                    hero.PendingDiscardCount = Mathf.Max(0, stored.PendingDiscardCount);
                    hero.BattleRoar = stored.BattleRoar;
                    hero.BloodrageReady = stored.BloodrageReady;
                    hero.AvatarRageTurns = Mathf.Max(0, stored.AvatarRageTurns);
                    hero.AvatarFloorActive = stored.AvatarFloorActive;
                    hero.Deck = stored.DeckIds != null ? new List<string>(stored.DeckIds) : new List<string>();
                    hero.Discard = stored.DiscardIds != null ? new List<string>(stored.DiscardIds) : new List<string>();
                    hero.Hand.Clear();
                    if (stored.HandIds != null) foreach (var id in stored.HandIds) { var card = HeroDatabase.GetCard(id); if (card != null) hero.Hand.Add(card); }
                    if (legacyHeroSave)
                    {
                        hero.Deck = HeroDatabase.BuildPhysicalDeck(hero.Id); hero.Discard.Clear(); Shuffle(hero.Deck); DrawCards(hero, 5, false);
                        AddLog($"SAVE MIGRATION — {hero.DisplayName} upgraded to the 0.8.0 30-card deck and 5-card hand.");
                    }
                    hero.Statuses = stored.Statuses != null ? stored.Statuses.Select(s => RestoreStatusFromSave(s, false)).ToList() : new List<StatusInstance>();
                    hero.UsedRoundAbilities = stored.UsedRoundAbilities != null ? new List<string>(stored.UsedRoundAbilities) : new List<string>();
                    hero.UsedTurnAbilities = stored.UsedTurnAbilities != null ? new List<string>(stored.UsedTurnAbilities) : new List<string>();
                    hero.UsedQuestCards = stored.UsedQuestCards != null ? new List<string>(stored.UsedQuestCards) : new List<string>();
                    hero.LootInventory = stored.LootInventoryIds != null ? stored.LootInventoryIds.Where(RewardContent0826.Loot.ContainsKey).ToList() : new List<string>();
                    hero.AcquiredEquipment = stored.AcquiredEquipmentIds != null ? stored.AcquiredEquipmentIds.Where(RewardContent0826.Equipment.ContainsKey).ToList() : new List<string>();
                    hero.KeptSearchEffects0852 = stored.KeptSearchEffects0852 != null ? new List<string>(stored.KeptSearchEffects0852) : new List<string>();
                    if(stored.EquippedEquipmentIds!=null&&stored.EquippedEquipmentIds.Count>0)RestoreEquippedLoadout(hero,stored.EquippedEquipmentIds);
                }
            }

            // 0.8.13 defeat-state invariant repair. A positive-Health Hero cannot remain Downed,
            // a Dead Hero is always at 0 Health, and legacy 0-Health records are classified once.
            foreach (var hero in ParticipatingHeroes)
            {
                if (hero.Dead)
                {
                    hero.Downed = false;
                    hero.Health = 0;
                    hero.Actions = 0;
                    hero.AttackOnlyActions = 0;
                }
                else if (hero.Downed)
                {
                    hero.Health = 0;
                }
                else if (hero.Health <= 0)
                {
                    bool ally = ParticipatingHeroes.Any(h => h.Id != hero.Id && !h.IsDefeated && h.LocationId == hero.LocationId);
                    hero.Downed = ally;
                    hero.Dead = !ally;
                    if (hero.Dead) { hero.Actions = 0; hero.AttackOnlyActions = 0; }
                }
                else
                {
                    hero.Downed = false;
                    hero.Dead = false;
                }
            }

            if(!QuestComplete&&!QuestFailed&&ParticipatingHeroes.All(h=>h.IsDefeated))
            {
                QuestFailed=true;
                AddLog($"SAVE STATE REPAIR — all participating Heroes are Defeated; {CurrentQuestId0829} is marked Failed.");
            }

            // Rebuild encounter-spawned enemies from authored templates before restoring their mutable state.
            foreach (var id in Enemies.Values.Where(e=>e.EncounterSpawned||e.QuestSpawned).Select(e=>e.Id).ToList()) Enemies.Remove(id);
            if (save.Enemies != null)
                foreach (var stored in save.Enemies)
                {
                    if (stored == null) continue;
                    if (!Enemies.TryGetValue(stored.Id, out var enemy) && !string.IsNullOrEmpty(stored.TemplateId))
                    {
                        if(stored.QuestSpawned) enemy=CreateQuestEnemy0829(stored.TemplateId,stored.Id,stored.LocationId);
                        else if(stored.EncounterSpawned) enemy = CreateEncounterEnemy(stored.TemplateId, stored.Id, stored.LocationId, 0);
                        if (enemy != null) Enemies[enemy.Id] = enemy;
                    }
                    if (enemy == null) continue;
                    enemy.TemplateId = stored.TemplateId ?? enemy.TemplateId;
                    enemy.EncounterSpawned = stored.EncounterSpawned;
                    enemy.QuestSpawned = stored.QuestSpawned;
                    enemy.QuestId = stored.QuestId;
                    enemy.EncounterInstanceId = stored.EncounterInstanceId;
                    if(stored.EncounterSpawned||stored.QuestSpawned)
                    {
                        if(stored.MaxHealth>0)enemy.MaxHealth=stored.MaxHealth;
                        if(stored.Attack>0)enemy.Attack=stored.Attack;
                        enemy.Armor=Mathf.Max(0,stored.Armor);
                        enemy.IsBoss=stored.IsBoss;
                        enemy.ActivatedThisRound=stored.ActivatedThisRound;
                    }
                    enemy.Health = Mathf.Clamp(stored.Health, 0, enemy.MaxHealth);
                    if (!string.IsNullOrEmpty(stored.LocationId) && Locations.ContainsKey(stored.LocationId)) enemy.LocationId = stored.LocationId;
                    enemy.Statuses = stored.Statuses != null ? stored.Statuses.Select(st => RestoreStatusFromSave(st, true)).ToList() : new List<StatusInstance>();
                    enemy.UsedKeys0859 = stored.UsedKeys0859 != null ? stored.UsedKeys0859.ToList() : new List<string>();
                    enemy.MovementStepsThisActivation0859 = stored.MovementStepsThisActivation0859;
                    enemy.FocusHeroId0859 = stored.FocusHeroId0859;
                }

            EnemyHazards0859.Clear();
            if(save.EnemyHazards0859!=null)
                foreach(var h in save.EnemyHazards0859)if(h!=null&&!string.IsNullOrEmpty(h.LocationId))
                    EnemyHazards0859.Add(new EnemyHazardState0859{HazardId=h.HazardId,Type=h.Type,SourceEnemyId=h.SourceEnemyId,LocationId=h.LocationId,CreatedRound=h.CreatedRound,TriggerRound=h.TriggerRound});

            RestoreEncounterObjectState(save);
            RestoreActiveEncounterState(save);

            HeroCreatedPieces.Clear();
            if (save.HeroCreatedPieces != null)
                foreach (var p in save.HeroCreatedPieces) if (p != null) HeroCreatedPieces.Add(new HeroCreatedPiece { PieceId=p.PieceId, Type=p.Type, OwnerHeroId=p.OwnerHeroId, LocationId=p.LocationId, Health=p.Health });

            SelectedHeroId = !string.IsNullOrEmpty(save.SelectedHeroId) && Heroes.TryGetValue(save.SelectedHeroId, out var sh) && sh.Participating ? save.SelectedHeroId : ParticipatingHeroes.First().Id;
            TurnHeroId = null;
            if (!string.IsNullOrEmpty(save.TurnHeroId) && Heroes.TryGetValue(save.TurnHeroId, out var th) && th.Participating && !th.IsDefeated && !th.TurnCompleted)
                TurnHeroId = th.Id;
            // 0.8.12 invariant: unfinished Heroes that have not started this Hero Phase hold 0 Actions.
            // Completed Heroes keep any saved Actions for legal Reactions, and an in-progress Hero resumes exactly as saved.
            foreach (var hero in ParticipatingHeroes)
                if (!hero.TurnCompleted && !IsHeroTakingTurn(hero)) { hero.Actions = 0; hero.AttackOnlyActions = 0; }
            SelectedEnemyId = !string.IsNullOrEmpty(save.SelectedEnemyId) && Enemies.TryGetValue(save.SelectedEnemyId, out var se) && !se.Defeated ? save.SelectedEnemyId : FirstLivingEnemyAtSelectedHeroLocation()?.Id;
            // Reconcile the authoritative campaign layer after every resolved-Quest load so the
            // currently restored Quest outcome and Completed/Failed quest history cannot diverge.
            if(QuestComplete||QuestFailed)SyncCampaignProgress0833();
            AddLog($"LOAD — {CurrentQuestId0829} campaign restored from {save.SavedAtUtc ?? "save file"}. Hero hands/decks/statuses/Quest runtime restored.");
            if (TurnHero != null) AddLog($"TURN RESUME — {TurnHero.DisplayName}'s turn was in progress when the game was saved.");
            else AddLog($"HERO PHASE — choose any unfinished Hero for the next turn.");
            StateChanged?.Invoke();
        }

        private void BuildLocations()
        {
            AddLocation("L000", "Stonebrook", new Vector3(0, 0, 0), true, "L001", "L002");
            AddLocation("L001", "River Crossing", new Vector3(11, 0, 5), false, "L000", "L003");
            AddLocation("L002", "Forest Path", new Vector3(-11, 0, 6), false, "L000", "L004", "L005", "L006", "L007", "L010", "L012");
            AddLocation("L003", "Ancient Ruins", new Vector3(20, 0, 11), false, "L001", "L006", "L007", "L008");
            AddLocation("L004", "Dense Woods", new Vector3(-20, 0, 12), false, "L002", "L005", "L006", "L011", "L015");
            AddLocation("L005", "Merchant Caravan", new Vector3(-28, 0, 5), false, "L002", "L004", "L012");
            AddLocation("L006", "Woodland Clearing", new Vector3(-6, 0, 18), false, "L002", "L003", "L004", "L007", "L009", "L010", "L011", "L012", "L015", "L016");
            AddLocation("L007", "Rocky Pass", new Vector3(11, 0, 21), false, "L003", "L006", "L008", "L020");
            AddLocation("L008", "Fallen Watchtower", new Vector3(23, 0, 25), false, "L003", "L007", "L009", "L011", "L017");
            AddLocation("L009", "Hunter's Lodge", new Vector3(5, 0, 31), false, "L006", "L008", "L010");
            AddLocation("L010", "Foggy Meadow", new Vector3(-12, 0, 29), false, "L002", "L006", "L009", "L013", "L016");
            AddLocation("L011", "Goblin Lookout", new Vector3(25, 0, 10), false, "L004", "L006", "L008", "L013", "L014");
            AddLocation("L012", "Abandoned Campsite", new Vector3(-27, 0, 20), false, "L002", "L005", "L006", "L010", "L013");
            AddLocation("L013", "Goblin Encampment", new Vector3(-10, 0, 42), false, "L010", "L011", "L012", "L014", "L016", "L017");
            AddLocation("L014", "Goblin War Camp", new Vector3(9, 0, 44), false, "L011", "L013", "L015", "L016", "L017", "L018", "L019");
            AddLocation("L015", "Hidden Grove", new Vector3(28, 0, 39), false, "L004", "L006", "L014", "L019");
            AddLocation("L016", "Ancient Stone Bridge", new Vector3(-2, 0, 36), false, "L006", "L010", "L013", "L014");
            AddLocation("L017", "Goblin Fortress Gate", new Vector3(20, 0, 53), false, "L008", "L013", "L014", "L018", "L021", "L025", "L027");
            AddLocation("L018", "Prison Pens", new Vector3(31, 0, 52), false, "L014", "L017", "L019", "L025", "L027");
            AddLocation("L019", "Ritual Circle", new Vector3(31, 0, 42), false, "L014", "L015", "L018", "L027", "L028");
            AddLocation("L020", "Cave Entrance", new Vector3(18, 0, 31), false, "L007", "L021", "L022");
            AddLocation("L021", "Abandoned Mine", new Vector3(18, 0, 39), false, "L020", "L022", "L024", "L025", "L017");
            AddLocation("L022", "Crystal Cavern", new Vector3(8, 0, 36), false, "L020", "L021", "L023", "L026");
            AddLocation("L023", "Mushroom Grove", new Vector3(-2, 0, 42), false, "L022", "L024", "L026");
            AddLocation("L024", "Collapsed Tunnel", new Vector3(8, 0, 47), false, "L021", "L023", "L025", "L026");
            AddLocation("L025", "Goblin Mines", new Vector3(15, 0, 48), false, "L021", "L024", "L014", "L017", "L018", "L027", "L028");
            AddLocation("L026", "Echoing Chamber", new Vector3(-2, 0, 49), false, "L022", "L023", "L024", "L025");
            AddLocation("L027", "Goblin Throne Room", new Vector3(28, 0, 62), false, "L017", "L025", "L018", "L019", "L028");
            AddLocation("L028", "Treasure Vault", new Vector3(40, 0, 59), false, "L025", "L027", "L019");
        }

        private void AddLocation(string id, string name, Vector3 pos, bool revealed, params string[] connections)
        {
            var location = new LocationState { Id = id, Name = name, Position = pos, Revealed = revealed, RevealResolved = revealed };
            ApplyAuthoredLocationMetadata(location);
            location.Connections.AddRange(connections); Locations[id] = location;
        }

        // 0.8.18 keeps the vertical-slice graph IDs stable for save compatibility while binding
        // each visible Location name to its v10.52 authored Terrain / Traits / Keywords / timing text.
        private static void ApplyAuthoredLocationMetadata(LocationState location)
        {
            if (location == null) return;
            switch (location.Name)
            {
                case "Stonebrook":
                    location.AuthoredLocationId="L000"; location.Terrain="Village"; location.Traits="Settlement; Safe Haven; Civilized"; location.Keywords="Merchant; Quest Hub; Rest; Supplies";
                    location.RevealEffect="Home Base — The first time Stonebrook is revealed each Quest, each Hero there may recover 1 Health.";
                    location.FirstVisitEffect="None — Stonebrook is the starting hub and does not trigger a first-visit Encounter.";
                    location.OngoingEffect="Safe Haven; Village Services; Gather Information."; break;
                case "Forest Path":
                    location.AuthoredLocationId="L001"; location.Terrain="Forest"; location.Traits="Outdoor; Forest"; location.Keywords="Open Ground";
                    location.RevealEffect="Fresh Tracks"; location.FirstVisitEffect="Draw 1 Travel Encounter at Encounter Level 1."; location.OngoingEffect="Open Trail — no inherent ongoing modifier."; break;
                case "Dense Woods":
                    location.AuthoredLocationId="L002"; location.Terrain="Forest"; location.Traits="Outdoor; Forest"; location.Keywords="Difficult Terrain; Concealment";
                    location.RevealEffect="Concealing Brush — the revealing Hero may Hide."; location.FirstVisitEffect="Draw 1 Travel Encounter at Encounter Level 1."; location.OngoingEffect="Dense Undergrowth — Difficult Terrain. Limited Visibility — ranged attacks into/out suffer -1 Attack."; break;
                case "River Crossing":
                    location.AuthoredLocationId="L004"; location.Terrain="Forest"; location.Traits="Outdoor; Water"; location.Keywords="River; Difficult Terrain";
                    location.RevealEffect="Fresh Water — the revealing Hero may recover 1 Health or remove 1 Poison Token."; location.FirstVisitEffect="Draw 1 Travel Encounter at Encounter Level 1."; location.OngoingEffect="Swift Current — Difficult Terrain. Exposed Crossing — no Cover."; break;
                case "Ancient Ruins":
                    location.AuthoredLocationId="L005"; location.Terrain="Ruins"; location.Traits="Outdoor; Ruins"; location.Keywords="Ancient; Cover";
                    location.RevealEffect="Forgotten Remains — the revealing Hero may immediately Search without spending an Action."; location.FirstVisitEffect="Draw 1 Travel Encounter at Encounter Level 1; after it resolves, Forgotten Cache."; location.OngoingEffect="Crumbling Walls — Cover vs ranged. Unstable Masonry — first Push/Knockback each round is prevented."; break;
                case "Woodland Clearing":
                    location.AuthoredLocationId="L003"; location.Terrain="Forest"; location.Traits="Outdoor; Forest"; location.Keywords="Open Ground";
                    location.RevealEffect="Quiet Respite — Each Hero here may recover 1 Health."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 1."; location.OngoingEffect="Open Sightlines — ranged attacks within/into/out gain +1 Attack. No Cover — figures here cannot benefit from Cover."; break;
                case "Rocky Pass":
                    location.AuthoredLocationId="L009"; location.Terrain="Hills"; location.Traits="Outdoor; Rocky"; location.Keywords="Chokepoint; Elevated; Cover";
                    location.RevealEffect="Loose Stones — The revealing Hero may choose: suffer 1 Damage or end their movement for this turn."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 2."; location.OngoingEffect="Narrow Passage — only one Hero and one Enemy may be engaged in melee here at a time. Stone Cover — defenders gain Cover vs ranged."; break;
                case "Fallen Watchtower":
                    location.AuthoredLocationId="L017"; location.Terrain="Ruins"; location.Traits="Outdoor; Ruins"; location.Keywords="Elevated; Crumbling";
                    location.RevealEffect="Commanding View — Look at 1 unrevealed Location Tile in the Quest Location Pool. Return it unrevealed."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 2."; location.OngoingEffect="Broken High Ground — figures here gain +1 Range with ranged attacks. Unstable Structure — first Heavy attack each round, on 6, all figures here suffer 1 damage."; break;
                case "Hunter's Lodge":
                    location.AuthoredLocationId="L036"; location.Terrain="Forest"; location.Traits="Outdoor; Structure"; location.Keywords="Shelter; Supplies; Cover";
                    location.RevealEffect="Hunter's Supplies — Draw 1 source-matched Loot."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 1."; location.OngoingEffect="Well-Stocked Cabin — first Hero to search draws 1 additional Item. Defensible Position — Heroes here gain Cover vs ranged."; break;
                case "Foggy Meadow":
                    location.AuthoredLocationId="L043"; location.Terrain="Meadow"; location.Traits="Outdoor; Meadow"; location.Keywords="Fog; Low Visibility";
                    location.RevealEffect="Blanketing Fog — The revealing Hero gains +1 Armor until the start of their next turn."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 2."; location.OngoingEffect="Dense Fog — ranged attacks may only target current or adjacent Location. Hidden Movement — first Hero entering each activation may Hide if able or gain +1 Armor until start of next turn."; break;
                case "Goblin Lookout":
                    location.AuthoredLocationId="L006"; location.Terrain="Forest"; location.Traits="Outdoor; Goblin Structure"; location.Keywords="Elevated; Watch Post";
                    location.RevealEffect="Abandoned Supplies — Gain 10 Gold."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 2."; location.OngoingEffect="High Ground — ranged attacks gain +1 Attack. Raised Alarm — first Enemy revealed here places 1 Alert Token on Quest or equivalent."; break;
                case "Abandoned Campsite":
                    location.AuthoredLocationId="L007"; location.Terrain="Forest"; location.Traits="Outdoor; Campsite"; location.Keywords="Shelter; Supplies";
                    location.RevealEffect="Left-Behind Supplies — Draw 1 source-matched Loot."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 1."; location.OngoingEffect="Temporary Shelter — at end of Hero activation, remove 1 Fatigue or recover 1 Stamina (per final resource rules). Old Fire Pit — first Hero to search gains +1 Morale/equivalent."; break;
                case "Goblin Encampment":
                    location.AuthoredLocationId="L010"; location.Terrain="Forest"; location.Traits="Outdoor; Goblin Camp"; location.Keywords="Enemy Stronghold; Fortified";
                    location.RevealEffect="Scattered Supplies — Draw 1 source-matched Loot."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 3."; location.OngoingEffect="Home Territory — Goblins here gain +1 Armor. Battle Ready — first Goblin revealed here enters Ready and may immediately Move (not Attack)."; break;
                case "Goblin War Camp":
                    location.AuthoredLocationId="L019"; location.Terrain="Camp"; location.Traits="Outdoor; Goblin Stronghold"; location.Keywords="Fortified; Command Post";
                    location.RevealEffect="Battle Plans — The revealing Hero gains +1 Attack against Goblins until the end of the round."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 4."; location.OngoingEffect="Organized Defense — Goblins here gain +1 attack. Call to Arms — first time combat begins, reveal additional Encounter Enemy; place if Goblin, otherwise return to top."; break;
                case "Hidden Grove":
                    location.AuthoredLocationId="L022"; location.Terrain="Forest"; location.Traits="Outdoor; Sacred Grove"; location.Keywords="Nature; Hidden";
                    location.RevealEffect="Sanctuary Discovered — The revealing Hero may remove 1 negative Condition."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 1."; location.OngoingEffect="Nature's Blessing — first Hero ending activation here each round may remove 1 negative Condition from self or ally in Location. Peaceful Sanctuary — enemies cannot be placed here during setup unless Quest directs."; break;
                case "Ancient Stone Bridge":
                    location.AuthoredLocationId="L013"; location.Terrain="Road"; location.Traits="Outdoor; Road; Ancient"; location.Keywords="Bridge; Ancient";
                    location.RevealEffect="Ancient Crossing — the old bridge opens another route through the Stonebrook Valley."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location."; location.OngoingEffect="Ancient stonework spans the route; Quest-specific rules may reference this crossing."; break;
                case "Goblin Fortress Gate":
                    location.AuthoredLocationId="L020"; location.Terrain="Fortress"; location.Traits="Outdoor; Fortress; Goblin Stronghold"; location.Keywords="Gatehouse; Fortified";
                    location.RevealEffect="Survey the Defenses — The revealing Hero gains +1 Attack against Objectives and Structures until the end of the round."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 4."; location.OngoingEffect="Reinforced Gate — interior connection may be blocked by Quest rules. Defender's Advantage — Goblins here gain Cover and +1 Armor."; break;
                case "Prison Pens":
                    location.AuthoredLocationId="L023"; location.Terrain="Fortress"; location.Traits="Indoor; Prison; Fortress; Goblin Stronghold"; location.Keywords="Captives; Fortified";
                    location.RevealEffect="Cries for Help — The revealing Hero gains +1 Attack until the end of the round."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 3."; location.OngoingEffect="Locked Cells — cannot search while an Enemy occupies. Rescue the Prisoners — Quest rules govern captive rescue."; break;
                case "Ritual Circle":
                    location.AuthoredLocationId="L024"; location.Terrain="Ruins"; location.Traits="Outdoor; Ritual Site; Forest; Ruins; Sacred; Goblin Stronghold"; location.Keywords="Arcane; Corrupted";
                    location.RevealEffect="Unstable Magic — The revealing Hero may remove 1 Ritual Token from this Location, if present."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 4."; location.OngoingEffect="Dark Ritual — Shaman/Spellcaster presence can advance Ritual Tokens. Disrupt Ritual — a Hero may spend 1 Action here to remove 1 Ritual Token."; break;
                case "Cave Entrance":
                    location.AuthoredLocationId="L012"; location.Terrain="Cave"; location.Traits="Outdoor; Cave Entrance"; location.Keywords="Shadow; Transition";
                    location.RevealEffect="Sounds Below — Look at 1 unrevealed Cave, Mine, or Underground Location Tile in the Quest Location Pool. Return it unrevealed."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 2."; location.OngoingEffect="Dark Interior — visibility reduced by 1/equivalent. Echoing Sounds — first Enemy revealed gains +1 attack first round."; break;
                case "Abandoned Mine":
                    location.AuthoredLocationId="L018"; location.Terrain="Mine"; location.Traits="Underground; Mine"; location.Keywords="Dark; Unstable";
                    location.RevealEffect="Discarded Ore — Draw 1 source-matched Loot."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 3."; location.OngoingEffect="Weak Supports — first Heavy attack each round may place Rubble. Echoing Tunnels — reinforcements may enter connected Locations."; break;
                case "Crystal Cavern":
                    location.AuthoredLocationId="L026"; location.Terrain="Cave"; location.Traits="Underground; Crystal Cavern"; location.Keywords="Crystal; Reflective";
                    location.RevealEffect="Resonating Crystal — The revealing Hero gains +1 Attack on their next Attack this round."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 3."; location.OngoingEffect="Crystal Reflections — first ranged attack each round here gains +1 Attack. Resonating Crystals — Area Damage causes +1 shard damage."; break;
                case "Mushroom Grove":
                    location.AuthoredLocationId="L028"; location.Terrain="Cave"; location.Traits="Underground; Fungal"; location.Keywords="Spores; Bioluminescent";
                    location.RevealEffect="Restorative Fungi — The revealing Hero may recover 1 Health."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 2."; location.OngoingEffect="Glowing Fungi — considered Illuminated. Spore Clouds — first Hero entry may gain Poison."; break;
                case "Collapsed Tunnel":
                    location.AuthoredLocationId="L029"; location.Terrain="Cave"; location.Traits="Underground; Rubble"; location.Keywords="Unstable; Chokepoint";
                    location.RevealEffect="Blocked Route — The revealing Hero's next Move Action this turn costs 1 additional Action."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 3."; location.OngoingEffect="Blocked Passage — first Hero entering must spend 1 Action to clear before normal passage. Falling Debris — forced movement may deal 1 Damage."; break;
                case "Goblin Mines":
                    location.AuthoredLocationId="L031"; location.Terrain="Mine"; location.Traits="Underground; Goblin Mine; Goblin Stronghold"; location.Keywords="Mining; Fortified";
                    location.RevealEffect="Abandoned Materials — Draw 1 source-matched Loot."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 4."; location.OngoingEffect="Forced Labor — Goblins here gain +1 Movement at round start. Explosive Charges — Heavy attacks may damage all figures here."; break;
                case "Echoing Chamber":
                    location.AuthoredLocationId="L034"; location.Terrain="Cave"; location.Traits="Underground; Natural Cavern"; location.Keywords="Echoes; Acoustics";
                    location.RevealEffect="Distant Echoes — Look at 1 unrevealed Location Tile in the Quest Location Pool. Return it unrevealed."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 3."; location.OngoingEffect="Echoing Sounds — Sound/Command abilities gain +1 range. Disorienting Echoes — first ranged attack against a target here each round suffers -1 Attack."; break;
                case "Goblin Throne Room":
                    location.AuthoredLocationId="L021"; location.Terrain="Fortress"; location.Traits="Indoor; Throne Room; Goblin Stronghold"; location.Keywords="Boss; Stronghold";
                    location.RevealEffect="Seat of Power — The revealing Hero gains +1 Armor until the start of their next turn."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level Boss."; location.OngoingEffect="Seat of the Chieftain — Goblin Chieftain/High Shaman here gain +1 Armor and cannot be forcibly moved. Fight to the Last — Goblins here cannot voluntarily retreat."; break;
                case "Treasure Vault":
                    location.AuthoredLocationId="L025"; location.Terrain="Fortress"; location.Traits="Indoor; Vault; Goblin Stronghold"; location.Keywords="Treasure; Locked";
                    location.RevealEffect="Glimmering Riches — The revealing Hero gains 10 Gold. This does not count as Searching the Location."; location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 4."; location.OngoingEffect="Sealed Vault — cannot search until all Enemies are defeated. Hidden Traps — first Hero entering makes a Trap Test."; break;
                case "Merchant Caravan":
                    location.AuthoredLocationId="L047"; location.Terrain="Road"; location.Traits="Road; Forest; Plains; Meadow"; location.Keywords="Outdoor; Caravan; Traders; Supplies";
                    location.RevealEffect="Friendly Merchants — The revealing Hero gains 1 free Consumable from the available supply.";
                    location.FirstVisitEffect="Draw 1 Travel Encounter when the first Hero enters this Location. Resolve it at Encounter Level 1.";
                    location.OngoingEffect="Traveling Merchants — if revealed with no Enemies, Heroes may buy/sell/trade per Quest rules."; break;
            }
            // The stable runtime IDs above remain for save compatibility, but all printable metadata comes
            // from the v10.52 authored Location table when that Location exists in the imported catalog.
            if(!string.IsNullOrWhiteSpace(location.AuthoredLocationId)&&FullContentCatalog0850.TryGetLocation(location.AuthoredLocationId,out var authored))
            {
                location.Name=authored.name;location.Terrain=authored.terrain;location.Traits=authored.traits;location.Keywords=authored.keywords;
                location.RevealEffect=authored.revealEffect;location.FirstVisitEffect=authored.firstVisitEffect;location.OngoingEffect=authored.ongoingEffect;
            }
        }

        public bool LocationHasTag(string locationId, string tag)
        {
            if (!Locations.TryGetValue(locationId ?? string.Empty, out var location) || string.IsNullOrWhiteSpace(tag)) return false;
            return location.HasPrintedTag(tag) || location.TemporaryTags.Any(t => string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }
        public bool IsArcaneLocation(string locationId) => LocationHasTag(locationId, "Arcane");
        public bool IsDifficultTerrain(string locationId) => LocationHasTag(locationId, "Difficult Terrain") || LocationDifficultTerrain0858(locationId);

        private void AddLocationTag(string locationId,string tag,string source,HeroState sourceHero,StatusExpiryTiming timing=StatusExpiryTiming.Persistent,int triggers=0)
        {
            if(!Locations.TryGetValue(locationId??string.Empty,out var location)||string.IsNullOrWhiteSpace(tag))return;
            var existing=location.TemporaryTags.FirstOrDefault(t=>string.Equals(t.Tag,tag,StringComparison.OrdinalIgnoreCase)&&string.Equals(t.SourceHeroId,sourceHero?.Id,StringComparison.OrdinalIgnoreCase));
            if(existing==null){existing=new LocationTagInstance{Tag=tag,Source=source,SourceHeroId=sourceHero?.Id};location.TemporaryTags.Add(existing);}
            existing.Source=source;existing.ExpiryTiming=timing;existing.RemainingExpiryTriggers=triggers;
            AddLog($"LOCATION TAG — {location.Name} becomes {tag}{(timing==StatusExpiryTiming.Persistent?"":" until "+timing)} ({source}).");
        }

        private void AdvanceLocationTagClock(HeroState sourceHero,StatusExpiryTiming timing)
        {
            foreach(var location in Locations.Values)
            {
                for(int i=location.TemporaryTags.Count-1;i>=0;i--)
                {
                    var tag=location.TemporaryTags[i];
                    if(tag.ExpiryTiming!=timing)continue;
                    if(sourceHero!=null&&!string.Equals(tag.SourceHeroId,sourceHero.Id,StringComparison.OrdinalIgnoreCase))continue;
                    if(tag.RemainingExpiryTriggers>1){tag.RemainingExpiryTriggers--;continue;}
                    location.TemporaryTags.RemoveAt(i); AddLog($"LOCATION TAG EXPIRES — {location.Name} is no longer {tag.Tag} ({tag.Source}).");
                }
            }
        }

        private void ResolveLocationReveal(LocationState location,HeroState revealingHero)
        {
            if(location==null||location.RevealResolved)return;
            location.RevealResolved=true;
            AddLog($"REVEAL EFFECT — {location.Name}: {location.RevealEffect}");
            if(revealingHero==null)return;

            if(location.Name=="Ancient Ruins")
            {
                revealingHero.FreeSearchLocationId=location.Id;
                AddLog($"REVEAL OPTION — {revealingHero.DisplayName} may Search Ancient Ruins once without spending an Action (Forgotten Remains).");
            }
            else if(location.Name=="Dense Woods")
            {
                revealingHero.PendingLocationOption="Hide";
                revealingHero.PendingLocationOptionLocationId=location.Id;
                AddLog($"REVEAL OPTION — {revealingHero.DisplayName} may Hide (Concealing Brush).");
            }
            else if(location.Name=="River Crossing")
            {
                revealingHero.PendingLocationOption="Fresh Water";
                revealingHero.PendingLocationOptionLocationId=location.Id;
                AddLog($"REVEAL OPTION — {revealingHero.DisplayName} may use Fresh Water to recover 1 Health or remove Poisoned.");
            }
            else if(location.Name=="Stonebrook")
            {
                foreach(var hero in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(hero,1,"Stonebrook Home Base",revealingHero);
            }
            else if(location.Name=="Forest Path")
            {
                // Enemy Movement is not yet a moving runtime in this vertical slice. Record the exact
                // authored effect as a round marker so the movement engine can consume it when it migrates.
                var enemy=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&Locations[location.Id].Connections.Contains(e.LocationId));
                if(enemy!=null){enemy.Statuses.RemoveAll(s=>s.Name=="Fresh Tracks Movement -1");AddStatus(enemy,"Fresh Tracks Movement -1",0,1,"Forest Path",false,revealingHero,"until the end of the round");}
            }
            ResolveLocationReveal0858(location,revealingHero);
        }

        private void ResolveLocationFirstVisit(LocationState location,HeroState hero)
        {
            if(location==null||hero==null||location.FirstVisitResolved)return;
            // First Visit remains eligible until a Hero enters with no Enemy present, per the locked Ascendance rule.
            if(Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==location.Id)){AddLog($"FIRST VISIT PENDING — {location.Name} still has its First Visit because an Enemy is present.");return;}
            location.FirstVisitResolved=true;
            AddLog($"FIRST VISIT — {location.Name}: {location.FirstVisitEffect}");
            ResolveLocationFirstVisit0858(location,hero);
        }

        // ===== 0.8.22 Encounter Hazards & Persistent Objects =====
        private void InitializeSearchEncounterDecks()
        {
            SearchDeck.Clear(); SearchDiscard.Clear(); EncounterDeck.Clear(); EncounterDiscard.Clear();
            SearchDeck.AddRange(SearchEncounterContent0820.BuildSearchDeck());
            EncounterDeck.AddRange(SearchEncounterContent0820.BuildEncounterDeck());
            Shuffle(SearchDeck); Shuffle(EncounterDeck);
        }

        private void InitializeRewardDecks()
        {
            LootDeck.Clear(); LootDiscard.Clear(); EquipmentDeck.Clear(); EquipmentDiscard.Clear();
            LootDeck.AddRange(RewardContent0826.BuildLootDeck());
            EquipmentDeck.AddRange(RewardContent0826.BuildEquipmentDeck());
            Shuffle(LootDeck); Shuffle(EquipmentDeck);
        }

        private void RestoreRewardDeckState(AscendanceSaveData save)
        {
            if(save==null)return;
            bool authoredRewardSave=(save.Version??string.Empty).StartsWith("0.8.26",StringComparison.OrdinalIgnoreCase)||(save.Version??string.Empty).StartsWith("0.8.27",StringComparison.OrdinalIgnoreCase)
                ||(save.LootDeckIds!=null&&save.LootDeckIds.Count>0)||(save.LootDiscardIds!=null&&save.LootDiscardIds.Count>0)
                ||(save.EquipmentDeckIds!=null&&save.EquipmentDeckIds.Count>0)||(save.EquipmentDiscardIds!=null&&save.EquipmentDiscardIds.Count>0);
            if(!authoredRewardSave)return;
            LootDeck.Clear(); LootDiscard.Clear(); EquipmentDeck.Clear(); EquipmentDiscard.Clear();
            if(save.LootDeckIds!=null)LootDeck.AddRange(save.LootDeckIds.Where(RewardContent0826.Loot.ContainsKey));
            if(save.LootDiscardIds!=null)LootDiscard.AddRange(save.LootDiscardIds.Where(RewardContent0826.Loot.ContainsKey));
            if(save.EquipmentDeckIds!=null)EquipmentDeck.AddRange(save.EquipmentDeckIds.Where(RewardContent0826.Equipment.ContainsKey));
            if(save.EquipmentDiscardIds!=null)EquipmentDiscard.AddRange(save.EquipmentDiscardIds.Where(RewardContent0826.Equipment.ContainsKey));
        }

        private void RestoreSearchEncounterDeckState(AscendanceSaveData save)
        {
            if(save==null)return;
            if((save.SearchDeckIds!=null&&save.SearchDeckIds.Count>0)||(save.SearchDiscardIds!=null&&save.SearchDiscardIds.Count>0))
            {
                SearchDeck.Clear(); SearchDiscard.Clear();
                if(save.SearchDeckIds!=null)SearchDeck.AddRange(save.SearchDeckIds.Where(SearchEncounterContent0820.SearchCards.ContainsKey));
                if(save.SearchDiscardIds!=null)SearchDiscard.AddRange(save.SearchDiscardIds.Where(SearchEncounterContent0820.SearchCards.ContainsKey));
            }
            if((save.EncounterDeckIds!=null&&save.EncounterDeckIds.Count>0)||(save.EncounterDiscardIds!=null&&save.EncounterDiscardIds.Count>0))
            {
                EncounterDeck.Clear(); EncounterDiscard.Clear();
                if(save.EncounterDeckIds!=null)EncounterDeck.AddRange(save.EncounterDeckIds.Where(SearchEncounterContent0820.EncounterCards.ContainsKey));
                if(save.EncounterDiscardIds!=null)EncounterDiscard.AddRange(save.EncounterDiscardIds.Where(SearchEncounterContent0820.EncounterCards.ContainsKey));
            }
            // 0.8.50 content migration: older saves knew only the first 20 Search cards. Preserve every
            // already drawn/discarded card, then add only the newly authored missing physical copies.
            ReconcileAuthoredDeckCopies0850(SearchDeck,SearchDiscard,SearchEncounterContent0820.BuildSearchDeck(),"Search");
            ReconcileAuthoredDeckCopies0850(EncounterDeck,EncounterDiscard,SearchEncounterContent0820.BuildEncounterDeck(),"Stonebrook Encounter");
        }

        private void ReconcileAuthoredDeckCopies0850(List<string> deck,List<string> discard,List<string> expected,string label)
        {
            int added=0;
            foreach(var g in expected.GroupBy(x=>x,StringComparer.OrdinalIgnoreCase))
            {
                int have=deck.Count(x=>string.Equals(x,g.Key,StringComparison.OrdinalIgnoreCase))+discard.Count(x=>string.Equals(x,g.Key,StringComparison.OrdinalIgnoreCase));
                for(int i=have;i<g.Count();i++){deck.Add(g.Key);added++;}
            }
            if(added>0){Shuffle(deck);AddLog($"SAVE MIGRATION — added {added} newly authored {label} card copy/copies without resetting existing deck history.");}
        }

        private void RestoreEncounterObjectState(AscendanceSaveData save)
        {
            EncounterObjects.Clear();
            if(save?.EncounterObjects==null)return;
            foreach(var stored in save.EncounterObjects)
            {
                if(stored==null||string.IsNullOrWhiteSpace(stored.ObjectId)||string.IsNullOrWhiteSpace(stored.Type))continue;
                EncounterObjects.Add(new EncounterObjectState
                {
                    ObjectId=stored.ObjectId,EncounterInstanceId=stored.EncounterInstanceId,CardId=stored.CardId,Type=stored.Type,DisplayName=stored.DisplayName,LocationId=stored.LocationId,
                    BlocksEncounterCompletion=stored.BlocksEncounterCompletion,RemoveAtEncounterEnd=stored.RemoveAtEncounterEnd,InteractionActionCost=stored.InteractionActionCost,
                    InteractionLabel=stored.InteractionLabel,AffectedHeroId=stored.AffectedHeroId,CreatedRound=stored.CreatedRound
                });
            }
        }

        private void RestoreActiveEncounterState(AscendanceSaveData save)
        {
            ActiveEncounters.Clear();
            if(save?.ActiveEncounters==null)return;
            foreach(var stored in save.ActiveEncounters)
            {
                if(stored==null||string.IsNullOrWhiteSpace(stored.InstanceId)||string.IsNullOrWhiteSpace(stored.CardId))continue;
                var runtime=new EncounterRuntimeState
                {
                    InstanceId=stored.InstanceId,CardId=stored.CardId,CardName=stored.CardName,LocationId=stored.LocationId,SourceHeroId=stored.SourceHeroId,
                    Source=stored.Source,Severity=Mathf.Max(1,stored.Severity),StartedRound=Mathf.Max(1,stored.StartedRound),RewardText=stored.RewardText,FailureText=stored.FailureText,PostResolveEffect0852=stored.PostResolveEffect0852,RewardResolved=stored.RewardResolved,
                    AllFireTokensRemovedBeforeClear=stored.AllFireTokensRemovedBeforeClear,HadEnemyLeavePlayUndefeated=stored.HadEnemyLeavePlayUndefeated,UndefeatedEliteExitCount=Mathf.Max(0,stored.UndefeatedEliteExitCount),
                    GainedEquipmentIds=stored.GainedEquipmentIds!=null?stored.GainedEquipmentIds.Where(RewardContent0826.Equipment.ContainsKey).ToList():new List<string>(),
                    SpawnedEnemyIds=stored.SpawnedEnemyIds!=null?new List<string>(stored.SpawnedEnemyIds):new List<string>(),
                    SpawnedObjectIds=stored.SpawnedObjectIds!=null?new List<string>(stored.SpawnedObjectIds):new List<string>()
                };
                if(!runtime.RewardResolved)ActiveEncounters.Add(runtime);
            }
            foreach(var encounter in ActiveEncounters.ToList())
            {
                foreach(var id in encounter.SpawnedEnemyIds)
                    if(Enemies.TryGetValue(id,out var enemy)&&enemy.EncounterSpawned&&string.IsNullOrEmpty(enemy.EncounterInstanceId))enemy.EncounterInstanceId=encounter.InstanceId;
                TryCompleteEncounter(encounter,"save restoration");
            }
        }

        private int RemainingEncounterEnemies(EncounterRuntimeState encounter)
        {
            if(encounter==null||encounter.SpawnedEnemyIds==null)return 0;
            return encounter.SpawnedEnemyIds.Count(id=>Enemies.TryGetValue(id,out var enemy)&&!enemy.Defeated);
        }

        private int RemainingEncounterObjects(EncounterRuntimeState encounter,bool blockingOnly=false)
        {
            if(encounter==null||encounter.SpawnedObjectIds==null)return 0;
            return EncounterObjects.Count(o=>encounter.SpawnedObjectIds.Contains(o.ObjectId)&&( !blockingOnly || o.BlocksEncounterCompletion));
        }

        private int RemainingEncounterBlockingObjects(EncounterRuntimeState encounter)=>RemainingEncounterObjects(encounter,true);

        private EncounterObjectState SpawnEncounterObject(EncounterRuntimeState encounter,string type,string displayName,LocationState location,bool blocksCompletion,bool removeAtEncounterEnd,int interactionActionCost=-1,string interactionLabel=null,string affectedHeroId=null)
        {
            if(encounter==null||location==null||string.IsNullOrWhiteSpace(type))return null;
            var obj=new EncounterObjectState
            {
                ObjectId=$"EO_{++encounterObjectSerial:0000}",EncounterInstanceId=encounter.InstanceId,CardId=encounter.CardId,Type=type,DisplayName=displayName,LocationId=location.Id,
                BlocksEncounterCompletion=blocksCompletion,RemoveAtEncounterEnd=removeAtEncounterEnd,InteractionActionCost=interactionActionCost,InteractionLabel=interactionLabel,AffectedHeroId=affectedHeroId,CreatedRound=Round
            };
            EncounterObjects.Add(obj);encounter.SpawnedObjectIds.Add(obj.ObjectId);
            EncounterObjectAvatar.Create(obj);
            AddLog($"ENCOUNTER OBJECT — {obj.DisplayName} appears at {location.Name} [{encounter.InstanceId}].");
            return obj;
        }

        private bool RemoveEncounterObject(EncounterObjectState obj,string reason,bool checkCompletion=true)
        {
            if(obj==null||!EncounterObjects.Remove(obj))return false;
            var avatar=FindObjectsByType<EncounterObjectAvatar>().FirstOrDefault(a=>a.ObjectId==obj.ObjectId);
            if(avatar!=null)Destroy(avatar.gameObject);
            AddLog($"ENCOUNTER OBJECT REMOVED — {obj.DisplayName}{(string.IsNullOrWhiteSpace(reason)?string.Empty:$" ({reason})")}.");
            if(checkCompletion&&!string.IsNullOrEmpty(obj.EncounterInstanceId))
            {
                var encounter=ActiveEncounters.FirstOrDefault(e=>string.Equals(e.InstanceId,obj.EncounterInstanceId,StringComparison.OrdinalIgnoreCase));
                if(encounter!=null)TryCompleteEncounter(encounter,$"{obj.DisplayName} cleared");
            }
            return true;
        }

        private EncounterObjectState FirstInteractableEncounterObject(HeroState hero)
        {
            if(hero==null)return null;
            return EncounterObjects.Where(o=>o.LocationId==hero.LocationId&&o.InteractionActionCost>=0)
                .OrderBy(o=>o.Type=="Web"?0:o.Type=="Fire Token"?1:2).ThenBy(o=>o.ObjectId).FirstOrDefault();
        }

        public bool CanInteractEncounterObject(HeroState hero)
        {
            if(hero==null||hero.IsDefeated||hero.Moving||IsResolving||HasPendingReactionWindow||!CanUseTurnActions(hero))return false;
            var obj=FirstInteractableEncounterObject(hero);
            return obj!=null&&GeneralActions(hero)>=Mathf.Max(0,obj.InteractionActionCost);
        }

        public string EncounterObjectActionLabel(HeroState hero)
        {
            var obj=FirstInteractableEncounterObject(hero);
            if(obj==null)return "Encounter Object";
            string label=string.IsNullOrWhiteSpace(obj.InteractionLabel)?$"Interact: {obj.DisplayName}":obj.InteractionLabel;
            return obj.InteractionActionCost>0?$"{label} ({obj.InteractionActionCost}A)":label;
        }

        private bool ResolveEncounterObjectInteraction(HeroState hero,EncounterObjectState obj,bool spendAction)
        {
            if(hero==null||obj==null||hero.LocationId!=obj.LocationId)return false;
            int cost=Mathf.Max(0,obj.InteractionActionCost);
            if(spendAction&&cost>0)hero.Actions-=cost;
            if(obj.Type=="Fire Token")
            {
                AddLog($"ENCOUNTER ACTION — {hero.DisplayName} extinguishes {obj.DisplayName}{(spendAction?$" for {cost} Action{(cost==1?string.Empty:"s")}":string.Empty)}.");
                var fireEncounter=ActiveEncounters.FirstOrDefault(e=>string.Equals(e.InstanceId,obj.EncounterInstanceId,StringComparison.OrdinalIgnoreCase));
                if(fireEncounter!=null&&fireEncounter.CardId=="EN007"&&fireEncounter.Severity<=2)AddPartyGold(10,"Burning Cart extinguish");
                RemoveEncounterObject(obj,"extinguished");return true;
            }
            if(obj.Type=="Web")
            {
                AddLog($"ENCOUNTER ACTION — {hero.DisplayName} clears the webs{(spendAction?$" for {cost} Action{(cost==1?string.Empty:"s")}":string.Empty)}.");
                if(!string.IsNullOrEmpty(obj.AffectedHeroId)&&Heroes.TryGetValue(obj.AffectedHeroId,out var trapped))RemoveStatus(trapped.Statuses,"Rooted",trapped.DisplayName,"webs cleared");
                RemoveEncounterObject(obj,"cleared");return true;
            }
            AppendPendingContent(obj.CardId+" "+obj.DisplayName,"Object interaction is not yet authored for this object type.");return false;
        }

        public void InteractEncounterObject()
        {
            var hero=SelectedHero;if(!CanInteractEncounterObject(hero))return;if(!RequireHeroTurn(hero,"interact with the Encounter object"))return;
            var obj=FirstInteractableEncounterObject(hero);if(obj==null)return;
            ResolveEncounterObjectInteraction(hero,obj,true);StateChanged?.Invoke();
        }

        private void ResolveEncounterObjectsOnHeroEnter(LocationState location,HeroState hero)
        {
            if(location==null||hero==null||hero.Dead)return;
            var snare=EncounterObjects.FirstOrDefault(o=>o.LocationId==location.Id&&o.Type=="Snare");
            if(snare==null)return;
            AddStatus(hero,"Rooted",1,0,"Trapper's Snare",false,null,"Rooted until end of turn.");
            AddLog($"ENCOUNTER HAZARD — {hero.DisplayName} triggers a Snare at {location.Name} and becomes Rooted until end of turn.");
            RemoveEncounterObject(snare,"triggered",false);
        }

        private void AddPartyGold(int amount,string source)
        {
            if(amount==0)return;
            PartyGold=Mathf.Max(0,PartyGold+amount);
            AddLog($"GOLD — Party {(amount>0?"gains":"loses")} {Mathf.Abs(amount)} Gold ({source}). Total: {PartyGold}.");
        }

        private void AddGeneralReputation(int amount,string source)
        {
            if(amount==0)return;
            int before=GeneralReputation;
            GeneralReputation=Mathf.Clamp(GeneralReputation+amount,-5,6);
            int applied=GeneralReputation-before;
            AddLog($"REPUTATION — General Reputation {(applied>=0?"gains":"loses")} {Mathf.Abs(applied)} ({source}). Total: {GeneralReputation}.");
        }

        private HeroState RewardHeroFor(string heroId)
        {
            if(!string.IsNullOrEmpty(heroId)&&Heroes.TryGetValue(heroId,out var chosen)&&chosen.Participating)return chosen;
            return ParticipatingHeroes.FirstOrDefault();
        }

        private string DrawLootToHero(HeroState hero,string source)
        {
            if(hero==null)return null;
            if(LootDeck.Count==0&&LootDiscard.Count>0){LootDeck.AddRange(LootDiscard);LootDiscard.Clear();Shuffle(LootDeck);AddLog("LOOT DECK — discard reshuffled into the draw deck.");}
            if(LootDeck.Count==0){AppendPendingContent(source,"Loot Deck is empty.");return null;}
            string id=LootDeck[0];LootDeck.RemoveAt(0);hero.LootInventory.Add(id);
            AddLog($"LOOT — {hero.DisplayName} gains {RewardContent0826.LootLabel(id)} ({source}). Inventory: {hero.LootInventory.Count} Loot.");
            OnLootAcquired0854(hero,id,source);
            return id;
        }

        private string DrawEquipmentToHero(HeroState hero,string source,EncounterRuntimeState encounter=null)
        {
            if(hero==null)return null;
            if(EquipmentDeck.Count==0){AppendPendingContent(source,"Equipment Deck is empty.");return null;}
            string id=EquipmentDeck[0];EquipmentDeck.RemoveAt(0);hero.AcquiredEquipment.Add(id);encounter?.GainedEquipmentIds.Add(id);
            AddLog($"EQUIPMENT — {hero.DisplayName} gains {RewardContent0826.EquipmentLabel(id)} unequipped ({source}). Inventory Equipment: {hero.AcquiredEquipment.Count}.");
            return id;
        }

        private bool DiscardAcquiredEquipment(HeroState hero,string id,string source)
        {
            if(hero==null||string.IsNullOrEmpty(id))return false;
            int at=hero.AcquiredEquipment.FindIndex(x=>string.Equals(x,id,StringComparison.OrdinalIgnoreCase));if(at<0)return false;
            hero.AcquiredEquipment.RemoveAt(at);EquipmentDiscard.Add(id);
            AddLog($"EQUIPMENT DISCARDED — {hero.DisplayName} loses {RewardContent0826.EquipmentLabel(id)} ({source}).");
            return true;
        }

        private void BeginRewardChoice(string sourceCardId,string sourceCardName,HeroState hero,string mode,string prompt)
        {
            if(HasPendingRewardChoice){AppendPendingContent(sourceCardId+" reward choice","Resolve the existing reward choice first.");return;}
            PendingRewardChoice=new RewardChoiceState{SourceCardId=sourceCardId,SourceCardName=sourceCardName,HeroId=hero?.Id,Mode=mode,Prompt=prompt};
            AddLog($"REWARD CHOICE — {sourceCardId} {sourceCardName}: {prompt}");
            StateChanged?.Invoke();
        }

        private List<RewardChoiceOption> BuildRewardChoiceOptions()
        {
            var options=new List<RewardChoiceOption>();var state=PendingRewardChoice;if(state==null)return options;
            switch(state.Mode)
            {
                case "Gold25OrLoot1": options.Add(new RewardChoiceOption{Key="GOLD",Label="Take 25 Gold"});options.Add(new RewardChoiceOption{Key="LOOT",Label="Draw 1 Item"});break;
                case "Loot1OrGold20": options.Add(new RewardChoiceOption{Key="LOOT",Label="Draw 1 Item"});options.Add(new RewardChoiceOption{Key="GOLD",Label="Take 20 Gold"});break;
                case "Gold30OrLoot1": options.Add(new RewardChoiceOption{Key="GOLD",Label="Take 30 Gold"});options.Add(new RewardChoiceOption{Key="LOOT",Label="Draw 1 Loot"});break;
                case "Pay10RepOrContinue":
                    if(PartyGold>=10)options.Add(new RewardChoiceOption{Key="PAY",Label="Give 10 Gold • Gain +1 General Reputation"});
                    options.Add(new RewardChoiceOption{Key="CONTINUE",Label="Continue without paying"});
                    break;
            }
            Append0827RewardChoiceOptions(options,state);
            return options;
        }

        public bool ChooseRewardChoice(string optionKey)
        {
            var state=PendingRewardChoice;if(state==null)return false;
            var hero=RewardHeroFor(state.HeroId);string key=(optionKey??string.Empty).ToUpperInvariant();
            if(TryResolve0827RewardChoice(state,hero,key,out bool handledResult)){if(handledResult)TryResolveDeferredLocationPostSearch0858();return handledResult;}
            bool valid=true;
            switch(state.Mode)
            {
                case "Gold25OrLoot1":
                    if(key=="GOLD")AddPartyGold(25,state.SourceCardName);else if(key=="LOOT")DrawLootToHero(hero,state.SourceCardName);else valid=false;break;
                case "Loot1OrGold20":
                    if(key=="LOOT")DrawLootToHero(hero,state.SourceCardName);else if(key=="GOLD")AddPartyGold(20,state.SourceCardName);else valid=false;break;
                case "Gold30OrLoot1":
                    if(key=="GOLD")AddPartyGold(30,state.SourceCardName);else if(key=="LOOT")DrawLootToHero(hero,state.SourceCardName);else valid=false;break;
                case "Pay10RepOrContinue":
                    if(key=="PAY"&&PartyGold>=10){AddPartyGold(-10,state.SourceCardName);AddGeneralReputation(1,state.SourceCardName);}
                    else if(key!="CONTINUE")valid=false;
                    break;
                default: valid=false;break;
            }
            if(!valid){AddLog($"REWARD CHOICE — invalid option '{optionKey}'.");return false;}
            AddLog($"REWARD CHOICE RESOLVED — {state.SourceCardId} {state.SourceCardName}: {key}.");PendingRewardChoice=null;TryResolveDeferredPostEncounter0852();TryResolveDeferredLocationPostSearch0858();StateChanged?.Invoke();return true;
        }

        private void QueueNextTravelEncounterSeverity(int amount,string source)
        {
            if(amount==0)return;
            NextTravelEncounterSeverityModifier+=amount;
            AddLog($"TRAVEL CONTROL — next Travel Encounter Severity {(amount>0?"gains":"loses")} {Mathf.Abs(amount)} ({source}). Pending modifier: {NextTravelEncounterSeverityModifier:+#;-#;0}.");
        }

        private void CancelPendingTravelSeverity(string source)
        {
            if(NextTravelEncounterSeverityModifier==0){AddLog($"TRAVEL CONTROL — {source} finds no pending Travel Encounter Severity modifier to cancel.");return;}
            AddLog($"TRAVEL CONTROL — {source} cancels pending Travel Encounter Severity modifier {NextTravelEncounterSeverityModifier:+#;-#;0}.");
            NextTravelEncounterSeverityModifier=0;
        }

        private void QueueSkipNextTravelEncounter(string source)
        {
            SkipNextTravelEncounter=true;
            AddLog($"TRAVEL CONTROL — the next Travel Encounter will be skipped ({source}).");
        }

        private void AppendPendingContent(string source,string instruction)
        {
            if(string.IsNullOrWhiteSpace(instruction))return;
            string entry=$"{source}: {instruction}";
            PendingContentInstruction=string.IsNullOrWhiteSpace(PendingContentInstruction)?entry:PendingContentInstruction+" | "+entry;
            AddLog($"CONTENT PENDING — {entry}");
        }

        public void ClearPendingContentInstruction()
        {
            PendingContentInstruction=null;
            StateChanged?.Invoke();
        }

        private string EncounterCardLabel(string id)
        {
            return SearchEncounterContent0820.EncounterCards.TryGetValue(id??string.Empty,out var card)?$"{card.Id} {card.Name}":id??"?";
        }

        private static string DeckControlModeDescription(string mode)
        {
            return mode switch
            {
                "MayBottomTop" => "look at the top Encounter card and keep it or put it on the bottom",
                "ReorderTop2" => "look at the top 2 Encounter cards and choose their order",
                "BottomOneTop2" => "look at the top 2 Encounter cards and put one on the bottom",
                "ReorderTop3" => "look at the top 3 Encounter cards and choose their order",
                _ => "resolve Encounter deck control"
            };
        }

        private void BeginEncounterDeckControl(string sourceCardId,string sourceCardName,string mode,int revealCount)
        {
            if(HasPendingEncounterDeckControl)
            {
                AppendPendingContent(sourceCardId+" deck control","Resolve the existing Encounter deck-control choice first.");
                return;
            }
            int count=Mathf.Clamp(revealCount,0,EncounterDeck.Count);
            if(count<=0){AddLog($"ENCOUNTER DECK CONTROL — {sourceCardId} has no Encounter cards available to inspect.");return;}
            PendingEncounterDeckControl=new EncounterDeckControlState{SourceCardId=sourceCardId,SourceCardName=sourceCardName,Mode=mode,RevealedCardIds=EncounterDeck.Take(count).ToList()};
            AddLog($"ENCOUNTER DECK CONTROL — {sourceCardId} {sourceCardName}: {DeckControlModeDescription(mode)}. Revealed: {string.Join(" | ",PendingEncounterDeckControl.RevealedCardIds.Select(EncounterCardLabel))}.");
            StateChanged?.Invoke();
        }

        private List<EncounterDeckControlOption> BuildEncounterDeckControlOptions()
        {
            var options=new List<EncounterDeckControlOption>();var state=PendingEncounterDeckControl;if(state==null)return options;
            int n=state.RevealedCardIds?.Count??0;if(n<=0)return options;
            string L(int i)=>i>=0&&i<n?EncounterCardLabel(state.RevealedCardIds[i]):"?";
            if(state.Mode=="MayBottomTop")
            {
                options.Add(new EncounterDeckControlOption{Key="KEEP",Label=$"Keep {L(0)} on top"});
                options.Add(new EncounterDeckControlOption{Key="BOTTOM0",Label=$"Put {L(0)} on bottom"});
            }
            else if(state.Mode=="ReorderTop2")
            {
                options.Add(new EncounterDeckControlOption{Key="KEEP",Label=$"Keep order: {L(0)} → {(n>1?L(1):"(none)")}"});
                if(n>1)options.Add(new EncounterDeckControlOption{Key="SWAP",Label=$"Swap order: {L(1)} → {L(0)}"});
            }
            else if(state.Mode=="BottomOneTop2")
            {
                options.Add(new EncounterDeckControlOption{Key="BOTTOM0",Label=$"Bottom {L(0)}"});
                if(n>1)options.Add(new EncounterDeckControlOption{Key="BOTTOM1",Label=$"Bottom {L(1)}"});
            }
            else if(state.Mode=="ReorderTop3")
            {
                if(n==1)options.Add(new EncounterDeckControlOption{Key="KEEP",Label=$"Keep {L(0)} on top"});
                else if(n==2)
                {
                    options.Add(new EncounterDeckControlOption{Key="KEEP",Label=$"Keep order: {L(0)} → {L(1)}"});
                    options.Add(new EncounterDeckControlOption{Key="SWAP",Label=$"Swap order: {L(1)} → {L(0)}"});
                }
                else
                {
                    int[][] perms={new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}};
                    foreach(var p in perms)options.Add(new EncounterDeckControlOption{Key=$"PERM{p[0]}{p[1]}{p[2]}",Label=$"{L(p[0])} → {L(p[1])} → {L(p[2])}"});
                }
            }
            return options;
        }

        private bool DeckControlSnapshotStillMatches()
        {
            if(PendingEncounterDeckControl==null)return false;var ids=PendingEncounterDeckControl.RevealedCardIds??new List<string>();
            if(EncounterDeck.Count<ids.Count)return false;
            for(int i=0;i<ids.Count;i++)if(!string.Equals(EncounterDeck[i],ids[i],StringComparison.OrdinalIgnoreCase))return false;
            return true;
        }

        private void BottomEncounterDeckIndex(int index)
        {
            if(index<0||index>=EncounterDeck.Count)return;string id=EncounterDeck[index];EncounterDeck.RemoveAt(index);EncounterDeck.Add(id);
        }

        private void ReorderEncounterDeckTop(params int[] order)
        {
            if(order==null||order.Length==0||EncounterDeck.Count<order.Length)return;
            var top=EncounterDeck.Take(order.Length).ToList();
            if(order.Any(i=>i<0||i>=top.Count)||order.Distinct().Count()!=order.Length)return;
            EncounterDeck.RemoveRange(0,order.Length);EncounterDeck.InsertRange(0,order.Select(i=>top[i]));
        }

        public bool ChooseEncounterDeckControl(string optionKey)
        {
            if(PendingEncounterDeckControl==null)return false;
            if(!DeckControlSnapshotStillMatches())
            {
                AddLog("ENCOUNTER DECK CONTROL — deck order changed unexpectedly; canceling the stale choice rather than applying it to different cards.");PendingEncounterDeckControl=null;StateChanged?.Invoke();return false;
            }
            string mode=PendingEncounterDeckControl.Mode;string key=(optionKey??string.Empty).ToUpperInvariant();bool valid=true;
            if(mode=="MayBottomTop")
            {
                if(key=="KEEP"){}else if(key=="BOTTOM0")BottomEncounterDeckIndex(0);else valid=false;
            }
            else if(mode=="ReorderTop2")
            {
                if(key=="KEEP"){}else if(key=="SWAP"&&PendingEncounterDeckControl.RevealedCardIds.Count>1)ReorderEncounterDeckTop(1,0);else valid=false;
            }
            else if(mode=="BottomOneTop2")
            {
                if(key=="BOTTOM0")BottomEncounterDeckIndex(0);else if(key=="BOTTOM1"&&PendingEncounterDeckControl.RevealedCardIds.Count>1)BottomEncounterDeckIndex(1);else valid=false;
            }
            else if(mode=="ReorderTop3")
            {
                int n=PendingEncounterDeckControl.RevealedCardIds.Count;
                if(n==1&&key=="KEEP"){}
                else if(n==2&&key=="KEEP"){}
                else if(n==2&&key=="SWAP")ReorderEncounterDeckTop(1,0);
                else if(n>=3&&Regex.IsMatch(key,@"^PERM[0-2]{3}$"))
                {
                    var order=key.Substring(4).Select(c=>c-'0').ToArray();if(order.Distinct().Count()!=3)valid=false;else ReorderEncounterDeckTop(order);
                }
                else valid=false;
            }
            else valid=false;
            if(!valid){AddLog($"ENCOUNTER DECK CONTROL — invalid option '{optionKey}'.");return false;}
            string source=$"{PendingEncounterDeckControl.SourceCardId} {PendingEncounterDeckControl.SourceCardName}";PendingEncounterDeckControl=null;
            AddLog($"ENCOUNTER DECK CONTROL RESOLVED — {source}. New top: {string.Join(" | ",EncounterDeck.Take(3).Select(EncounterCardLabel))}.");StateChanged?.Invoke();return true;
        }

        private SearchCardDefinition DrawEligibleSearchCard(LocationState location)
        {
            SearchCardDefinition TryTake()
            {
                for(int i=0;i<SearchDeck.Count;i++)
                {
                    if(!SearchEncounterContent0820.SearchCards.TryGetValue(SearchDeck[i],out var candidate))continue;
                    if(!SearchEncounterContent0820.MatchesLocation(location,candidate.Terrain))continue;
                    SearchDeck.RemoveAt(i);return candidate;
                }
                return null;
            }
            var card=TryTake();
            if(card!=null)return card;
            if(SearchDiscard.Count>0)
            {
                SearchDeck.AddRange(SearchDiscard);SearchDiscard.Clear();Shuffle(SearchDeck);
                card=TryTake();
            }
            return card;
        }

        private EncounterCardDefinition DrawEligibleEncounterCard(LocationState location)
        {
            EncounterCardDefinition TryTake()
            {
                for(int i=0;i<EncounterDeck.Count;i++)
                {
                    if(!SearchEncounterContent0820.EncounterCards.TryGetValue(EncounterDeck[i],out var candidate))continue;
                    if(!SearchEncounterContent0820.MatchesLocation(location,candidate.Terrain))continue;
                    EncounterDeck.RemoveAt(i);return candidate;
                }
                return null;
            }
            var card=TryTake();
            if(card!=null)return card;
            if(EncounterDiscard.Count>0)
            {
                EncounterDeck.AddRange(EncounterDiscard);EncounterDiscard.Clear();Shuffle(EncounterDeck);
                card=TryTake();
            }
            return card;
        }

        private void ResolveSearchDraw(LocationState location,HeroState hero)
        {
            var card=DrawEligibleSearchCard(location);
            if(card==null){AddLog($"SEARCH DECK — no eligible Search card remains for {location.Name}.");return;}
            LastSearchCardId=card.Id;
            AddLog($"SEARCH CARD — {card.Id} {card.Name}: {card.Effect}");
            ResolveSearchCard(card,location,hero);
            if(string.Equals(card.Id,"SD045",StringComparison.OrdinalIgnoreCase)){SearchDeck.Add(card.Id);AddLog($"SEARCH DECK — {card.Id} {card.Name} placed on the bottom instead of the discard pile.");}
            else SearchDiscard.Add(card.Id);
        }

        private void RemoveFirstHarmfulStatus(HeroState hero,string source)
        {
            if(hero==null)return;
            var harmful=hero.Statuses.FirstOrDefault(st=>st!=null&&!st.Beneficial);
            if(harmful!=null)RemoveStatus(hero.Statuses,harmful.Name,hero.DisplayName,source);
            else AddLog($"{source} — {hero.DisplayName} has no harmful Condition to remove.");
        }

        private void ResolveSearchCard(SearchCardDefinition card,LocationState location,HeroState hero)
        {
            if(ResolveCampaignISearch0852(card,location,hero))return;
            if(ResolveSearchReward0827(card,location,hero))return;
            switch(card.Id)
            {
                case "SD002": AddPartyGold(15,card.Name); break;
                case "SD007": AddPartyGold(30,card.Name); AppendPendingContent(card.Name,"Draw 1 Loot card."); break;
                case "SD008": HealHero(hero,1,card.Name,hero); AppendPendingContent(card.Name,"Draw 1 Loot card."); break;
                case "SD011": AddPartyGold(10,card.Name); AppendPendingContent(card.Name,"Draw 1 Loot card."); break;
                case "SD013":
                    AddPartyGold(25,card.Name);
                    if(location.HasPrintedTag("Ancient")||location.HasPrintedTag("Sacred")||location.HasPrintedTag("Ruins"))AddPartyGold(10,card.Name+" bonus");
                    break;
                case "SD014":
                    AddPartyGold(25,card.Name);
                    AppendPendingContent(card.Name,"Draw 1 Loot card.");
                    int level=ExtractNumber(location.FirstVisitEffect,@"Encounter Level\s+(\d+)");
                    ResolveTravelEncounter(location,hero,level<=0?1:level,0,card.Name);
                    break;
                case "SD015":
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(h,1,card.Name,hero);
                    break;
                case "SD016": RemoveFirstHarmfulStatus(hero,card.Name); break;
                case "SD001": case "SD003": case "SD004": case "SD005": case "SD006": case "SD009": case "SD010": case "SD012": case "SD017": case "SD018": case "SD019": case "SD020":
                    AppendPendingContent(card.Id+" "+card.Name,card.Effect);
                    break;
                default: AppendPendingContent(card.Id+" "+card.Name,card.Effect); break;
            }
        }

        private EnemyState CreateEncounterEnemy(string templateId,string runtimeId,string locationId,int healthAdjustment,string encounterInstanceId=null)
        {
            if(!SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId??string.Empty,out var t))return null;
            int hp=Mathf.Max(1,t.Health+healthAdjustment);
            return new EnemyState{Id=runtimeId,TemplateId=t.Id,EncounterSpawned=true,EncounterInstanceId=encounterInstanceId,DisplayName=t.Name,MaxHealth=hp,Health=hp,Armor=t.Armor,Attack=t.Attack,LocationId=locationId,IsBoss=t.Boss};
        }

        private EnemyState SpawnEncounterEnemy(string templateId,LocationState location,EncounterRuntimeState encounter,int healthAdjustment=0)
        {
            string runtimeId=$"ENC_{templateId}_{++encounterSpawnSerial:0000}";
            var enemy=CreateEncounterEnemy(templateId,runtimeId,location.Id,healthAdjustment,encounter?.InstanceId);
            if(enemy==null){AppendPendingContent("Encounter spawn",$"Enemy template {templateId} is not migrated.");return null;}
            ApplyWorldEventEnemySpawn0857(enemy,encounter?.CardName??"Travel Encounter",false);
            Enemies[enemy.Id]=enemy;
            encounter?.SpawnedEnemyIds.Add(enemy.Id);
            AddLog($"ENCOUNTER SPAWN — {enemy.DisplayName} ({templateId}) enters {location.Name} at {enemy.Health}/{enemy.MaxHealth} Health{(encounter==null?string.Empty:$" [{encounter.InstanceId}]")}.");
            return enemy;
        }

        private int ResolveEncounterEnemySpawns(string effect,LocationState location,EncounterRuntimeState encounter)
        {
            if(string.IsNullOrWhiteSpace(effect))return 0;
            if(Regex.IsMatch(effect,@"\bPay\s+\d+\s+Gold\s+or\s+fight",RegexOptions.IgnoreCase))return 0;
            int spawned=0;
            foreach(Match match in Regex.Matches(effect,@"(\d+)\s+(E\d{3})",RegexOptions.IgnoreCase))
            {
                if(!int.TryParse(match.Groups[1].Value,out int count))continue;
                string template=match.Groups[2].Value.ToUpperInvariant();
                int adjustment=0;
                var tail=effect.Substring(match.Index+match.Length);
                if(Regex.IsMatch(tail,@"^\s+with\s+-1\s+Health",RegexOptions.IgnoreCase))adjustment=-1;
                else
                {
                    var plus=Regex.Match(tail,@"^\s+with\s+\+(\d+)\s+Health",RegexOptions.IgnoreCase);
                    if(plus.Success)adjustment=int.Parse(plus.Groups[1].Value);
                }
                for(int i=0;i<count;i++)if(SpawnEncounterEnemy(template,location,encounter,adjustment)!=null)spawned++;
            }
            if(spawned>0)
            {
                var builder=UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>();
                if(builder!=null)builder.EnsureRuntimeEnemySpawns();
            }
            return spawned;
        }

        private void ResolveEncounterImmediateEffect(EncounterCardDefinition card,string effect,int severity,LocationState location,HeroState hero,EncounterRuntimeState encounter)
        {
            if(ResolveCampaignIEncounterImmediate0853(card,effect,severity,location,hero,encounter))return;
            if(string.IsNullOrWhiteSpace(effect)||string.Equals(effect,"No effect",StringComparison.OrdinalIgnoreCase)||string.Equals(effect,"Cross safely",StringComparison.OrdinalIgnoreCase))return;

            // 0.8.26 — deterministic Loot/Equipment/Reputation consequences that occur before combat setup.
            if(card.Id=="EN008")
            {
                if(severity<=2){DrawLootToHero(hero,card.Name);return;}
                if(severity<=4){BeginRewardChoice(card.Id,card.Name,hero,"Pay10RepOrContinue","Give 10 Gold to gain +1 General Reputation, or continue.");return;}
            }
            if(card.Id=="EN009")
            {
                if(severity>=3&&severity<=4){DrawLootToHero(hero,card.Name);return;}
                if(severity>=5&&severity<=6){DrawLootToHero(hero,card.Name);DrawLootToHero(hero,card.Name+" Item");return;}
                if(severity>=7){DrawLootToHero(hero,card.Name);DrawEquipmentToHero(hero,card.Name,encounter);}
            }
            if(card.Id=="EN017")
            {
                if(severity<=2){DrawLootToHero(hero,card.Name);return;}
                if(severity>=5)DrawEquipmentToHero(hero,card.Name,encounter);
            }
            if(card.Id=="EN019"&&severity<=2){AddGeneralReputation(1,card.Name);return;}
            if(card.Id=="EN027")
            {
                if(severity<=2){AddPartyGold(10,card.Name);return;}
                if(severity<=4){DrawLootToHero(hero,card.Name);return;}
                if(severity<=6){DrawLootToHero(hero,card.Name);AddPartyGold(20,card.Name);return;}
                DrawEquipmentToHero(hero,card.Name,encounter);
            }

            if(Regex.IsMatch(effect,@"\bPay\s+\d+\s+Gold\s+or\s+fight",RegexOptions.IgnoreCase)||effect.IndexOf("Choose:",StringComparison.OrdinalIgnoreCase)>=0||effect.IndexOf("may avoid",StringComparison.OrdinalIgnoreCase)>=0)
            {
                AppendPendingContent(card.Id+" "+card.Name,effect);return;
            }

            // 0.8.22 persistent Encounter objects.
            if(card.Id=="EN006"&&severity>=3)
            {
                int snares=severity<=4?1:2;
                for(int i=0;i<snares;i++)SpawnEncounterObject(encounter,"Snare",$"Snare {i+1}",location,false,false,-1,null);
            }
            if(card.Id=="EN007")
            {
                int fires=severity<=2?1:(severity<=4?1:2);
                bool block=severity<=2;
                for(int i=0;i<fires;i++)SpawnEncounterObject(encounter,"Fire Token",$"Fire Token {i+1}",location,block,true,1,"Extinguish");
                if(severity<=2)return;
            }
            if(card.Id=="EN012")
            {
                var web=SpawnEncounterObject(encounter,"Web","Spider Webs",location,true,false,1,"Clear Web",hero.Id);
                AddStatus(hero,"Rooted",1,0,card.Name,false,null,"Rooted until the webs are cleared.");
                AddLog($"ENCOUNTER HAZARD — {hero.DisplayName} begins Rooted by Spider Webs; spend 1 Action to clear the Web.");
                if(severity<=2)return;
            }
            if(card.Id=="EN024"&&severity>=3)
            {
                int fires=severity<=4?1:2;
                for(int i=0;i<fires;i++)SpawnEncounterObject(encounter,"Fire Token",$"Fire Token {i+1}",location,false,true,1,"Extinguish");
            }

            if(card.Id=="EN002"&&severity>=5)
            {
                foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))ApplyHeroDamageNow(h,1,card.Name);
                AddLog("ENCOUNTER SETUP — Arrows from the Trees deals 1 damage to each Hero before setup.");
            }
            if(card.Id=="EN013"&&severity>=5)
            {
                var first=ParticipatingHeroes.Where(h=>!h.Dead).OrderBy(h=>h.PartyIndex).FirstOrDefault();
                if(first!=null)ApplyHeroDamageNow(first,1,card.Name);
            }
            int spawned=(card.Id=="EN016"&&severity>=5&&severity<=6)?0:ResolveEncounterEnemySpawns(effect,location,encounter);
            if(card.Id=="EN009"&&severity>=7)
            {
                var saboteur=encounter.SpawnedEnemyIds.Select(id=>Enemies.TryGetValue(id,out var e)?e:null).FirstOrDefault(e=>e!=null&&e.TemplateId=="E011");
                if(saboteur!=null){AddStatus(saboteur,"Encounter Attack +1",1,0,card.Name,false,null,"until end of next activation");AddLog("ENCOUNTER SETUP — Abandoned Goblin Camp Saboteur gains +1 Attack during its first activation.");}
            }
            if(card.Id=="EN006"&&severity<=2){hero.NextTurnActionPenalty+=1;return;}
            if(card.Id=="EN010"&&severity>=7)
                foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))AddStatus(h,"Slowed",1,0,card.Name,false,null,"until end of next turn");
            if(card.Id=="EN021")
            {
                if(severity>=3&&severity<=4)
                {
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))
                    {
                        int crossingRoll=UnityEngine.Random.Range(1,7);
                        AddLog($"ENCOUNTER ROLL — {h.DisplayName} rolls {crossingRoll} for Flooded Crossing.");
                        if(crossingRoll==1)ApplyHeroDamageNow(h,1,card.Name);
                    }
                    return;
                }
                else if(severity>=7)foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead))AddStatus(h,"Slowed",1,0,card.Name,false,null,"until end of next turn");
            }
            if(card.Id=="EN027"&&severity>=7)
            {
                AddLog("ENCOUNTER CHAIN — Supply Wagon Wreck immediately draws another Travel Encounter and resolves it in the Severity 3-4 band.");
                ResolveTravelEncounter(location,hero,1,0,card.Name+" follow-up",0,3,true);
                return;
            }
            if(card.Id=="EN028")
            {
                if(severity>=7){QueueSkipNextTravelEncounter(card.Name);return;}
                if(severity>=5){BeginEncounterDeckControl(card.Id,card.Name,"ReorderTop3",3);return;}
                if(severity>=3){BeginEncounterDeckControl(card.Id,card.Name,"BottomOneTop2",2);return;}
                return;
            }
            if(card.Id=="EN020")
            {
                if(severity<=2)foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(h,1,card.Name,hero);
                else if(severity>=5)foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(h,2,card.Name,hero);
                else AppendPendingContent(card.Id+" "+card.Name,effect);
                return;
            }
            if(card.Id=="EN030")
            {
                if(severity<=2)foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(h,1,card.Name,hero);
                else if(severity>=5)
                {
                    int heal=severity>=7?2:1;
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(h,heal,card.Name,hero);
                    if(severity>=7)QueueNextTravelEncounterSeverity(1,card.Name);
                }
                else AppendPendingContent(card.Id+" "+card.Name,effect);
                return;
            }
            if(card.Id=="EN009"&&severity<=2){AddPartyGold(10,card.Name);return;}
            if(card.Id=="EN017"&&severity>=3&&severity<=4){AddPartyGold(20,card.Name);return;}
            if(card.Id=="EN018"&&severity<=2){AddPartyGold(10,card.Name);return;}
            if(card.Id=="EN016"&&severity>=3&&severity<=6)return; // ApplyEncounterMovementRules owns the queued ongoing effect.

            if(spawned==0)
            {
                var gain=Regex.Match(effect,@"^\s*Gain\s+(\d+)\s+Gold\b",RegexOptions.IgnoreCase);
                if(gain.Success){AddPartyGold(int.Parse(gain.Groups[1].Value),card.Name);return;}
                if(effect.IndexOf("Recover 1 Health each",StringComparison.OrdinalIgnoreCase)>=0)
                {
                    foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==location.Id))HealHero(h,1,card.Name,hero);return;
                }
                AppendPendingContent(card.Id+" "+card.Name,effect);
            }
            else
            {
                string residue=Regex.Replace(effect,@"\b(?:spawn\s+)?\d+\s+E\d{3}(?:\s+with\s+[+-]?\d+\s+Health)?",string.Empty,RegexOptions.IgnoreCase);
                residue=Regex.Replace(residue,@"\b(?:Place\s+)?\d+\s+(?:Snares?|Fire Tokens?)\b",string.Empty,RegexOptions.IgnoreCase);
                residue=Regex.Replace(residue,@"\bClear webs;?\s*(?:no combat)?\b",string.Empty,RegexOptions.IgnoreCase).Trim(' ',';','+');
                if((card.Id=="EN010"||card.Id=="EN021")&&severity>=7)residue=string.Empty;
                if(card.Id=="EN009"&&severity>=7)residue=string.Empty;
                if(card.Id=="EN017"&&severity>=5)residue=string.Empty;
                if(card.Id=="EN008"&&severity>=5)residue=string.Empty;
                if(card.Id=="EN019"&&severity>=5)residue=string.Empty;
                if(ShouldSuppressEncounterResidue0853(card.Id,severity))residue=string.Empty;
                if(!string.IsNullOrWhiteSpace(residue)&&residue!="then"&&residue!="appears")AppendPendingContent(card.Id+" encounter rule",residue);
            }
        }

        private void ResolveEncounterReward(EncounterRuntimeState encounter)
        {
            if(encounter==null||encounter.RewardResolved)return;
            encounter.RewardResolved=true;
            if(ResolveCampaignIEncounterReward0853(encounter))return;
            string reward=encounter.RewardText?.Trim();
            if(encounter.HadEnemyLeavePlayUndefeated&&(encounter.CardId=="EN003"||encounter.CardId=="EN026"))
            {
                AddLog($"ENCOUNTER REWARD — {encounter.CardId} normal victory reward is not earned because a Scout left play without being Defeated.");
                return;
            }
            var rewardHero=RewardHeroFor(encounter.SourceHeroId);

            // 0.8.26 Campaign I tangible rewards and General Reputation.
            if(encounter.CardId=="EN008"&&encounter.Severity>=5)
            {
                AddGeneralReputation(encounter.Severity>=7?2:1,$"{encounter.CardId} {encounter.CardName} escort");
                AddPartyGold(15,$"{encounter.CardId} {encounter.CardName} escort reward");
                return;
            }
            if(encounter.CardId=="EN019"&&encounter.Severity>=5)
            {
                AddGeneralReputation(encounter.Severity>=7?2:1,$"{encounter.CardId} {encounter.CardName} rescue");
                return;
            }
            if(encounter.CardId=="EN004"){DrawLootToHero(rewardHero,$"{encounter.CardId} {encounter.CardName} victory reward");return;}
            if(encounter.CardId=="EN005"){BeginRewardChoice(encounter.CardId,encounter.CardName,rewardHero,"Gold25OrLoot1","Choose 25 Gold or draw 1 Item.");return;}
            if(encounter.CardId=="EN012")
            {
                DrawLootToHero(rewardHero,$"{encounter.CardId} {encounter.CardName} victory reward");
                if(encounter.Severity>=5)DrawLootToHero(rewardHero,$"{encounter.CardId} {encounter.CardName} Severity 5+ Item");
                return;
            }
            if(encounter.CardId=="EN013"){BeginRewardChoice(encounter.CardId,encounter.CardName,rewardHero,"Loot1OrGold20","Choose 1 Item or 20 Gold.");return;}
            if(encounter.CardId=="EN014"){BeginRewardChoice(encounter.CardId,encounter.CardName,rewardHero,"Gold30OrLoot1","Choose 30 Gold or draw 1 Loot.");return;}
            if(encounter.CardId=="EN025")
            {
                AddPartyGold(35,$"{encounter.CardId} {encounter.CardName} victory reward");
                DrawLootToHero(rewardHero,$"{encounter.CardId} {encounter.CardName} victory reward");
                return;
            }

            if(string.IsNullOrWhiteSpace(reward)||string.Equals(reward,"As listed",StringComparison.OrdinalIgnoreCase)||string.Equals(reward,"Rewards as listed",StringComparison.OrdinalIgnoreCase)||string.Equals(reward,"Recovery as listed",StringComparison.OrdinalIgnoreCase))return;

            if(encounter.CardId=="EN007")
            {
                AddPartyGold(25,$"{encounter.CardId} {encounter.CardName} victory reward");
                if(encounter.AllFireTokensRemovedBeforeClear)DrawLootToHero(rewardHero,$"{encounter.CardId} {encounter.CardName} all Fire removed");
                else AddLog("ENCOUNTER REWARD — EN007 Item bonus was not earned because Fire Tokens remained when the encounter ended.");
                return;
            }

            if(encounter.CardId=="EN024")
            {
                AddPartyGold(30,$"{encounter.CardId} {encounter.CardName} victory reward");
                if(encounter.AllFireTokensRemovedBeforeClear)AddGeneralReputation(1,$"{encounter.CardId} {encounter.CardName} all Fire removed");
                else AddLog("ENCOUNTER REWARD — EN024 Reputation bonus was not earned because Fire Tokens remained when the encounter ended.");
                return;
            }

            if(encounter.CardId=="EN026")
            {
                AddPartyGold(20,$"{encounter.CardId} {encounter.CardName} victory reward");
                CancelPendingTravelSeverity(encounter.CardName);
                return;
            }

            if(encounter.CardId=="EN001")
            {
                int eliteDefeated=encounter.SpawnedEnemyIds.Count(id=>Enemies.TryGetValue(id,out var enemy)&&enemy.Defeated&&SearchEncounterContent0820.IsEliteEnemyTemplate(enemy.TemplateId));
                int gold=15+(eliteDefeated*5);
                AddPartyGold(gold,$"{encounter.CardId} {encounter.CardName} victory reward");
                AddLog($"ENCOUNTER REWARD — {encounter.CardId} awards 15 Gold + 5 per Elite defeated ({eliteDefeated}) = {gold} Gold.");
                return;
            }

            if(encounter.CardId=="EN003")
            {
                AddPartyGold(10,$"{encounter.CardId} {encounter.CardName} victory reward");
                BeginEncounterDeckControl(encounter.CardId,encounter.CardName,"MayBottomTop",1);
                return;
            }

            if(encounter.CardId=="EN022")
            {
                AddPartyGold(25,$"{encounter.CardId} {encounter.CardName} victory reward");
                BeginEncounterDeckControl(encounter.CardId,encounter.CardName,"ReorderTop2",2);
                return;
            }

            var conditional=Regex.Match(reward,@"^(\d+)\s+Gold\s+if\s+(E\d{3})\s+is\s+defeated",RegexOptions.IgnoreCase);
            if(conditional.Success)
            {
                string templateId=conditional.Groups[2].Value.ToUpperInvariant();
                bool defeated=encounter.SpawnedEnemyIds.Any(id=>Enemies.TryGetValue(id,out var enemy)&&enemy.Defeated&&string.Equals(enemy.TemplateId,templateId,StringComparison.OrdinalIgnoreCase));
                if(defeated)AddPartyGold(int.Parse(conditional.Groups[1].Value),$"{encounter.CardId} {encounter.CardName} victory reward");
                else AddLog($"ENCOUNTER REWARD — {encounter.CardId} conditional Gold was not earned because {templateId} was not defeated in this Encounter.");
                return;
            }

            var won=Regex.Match(reward,@"^If\s+immediate\s+combat\s+is\s+won,\s*gain\s+(\d+)\s+Gold",RegexOptions.IgnoreCase);
            if(won.Success){AddPartyGold(int.Parse(won.Groups[1].Value),$"{encounter.CardId} {encounter.CardName} victory reward");return;}

            if(reward.IndexOf(" or ",StringComparison.OrdinalIgnoreCase)>=0)
            {
                AppendPendingContent(encounter.CardId+" reward choice",reward);
                return;
            }

            var direct=Regex.Match(reward,@"^(\d+)\s+Gold\b",RegexOptions.IgnoreCase);
            if(direct.Success)
            {
                AddPartyGold(int.Parse(direct.Groups[1].Value),$"{encounter.CardId} {encounter.CardName} victory reward");
                string remainder=reward.Substring(direct.Length).Trim(' ',';');
                if(!string.IsNullOrWhiteSpace(remainder))
                {
                    if(remainder.IndexOf("remove Weakened from all Heroes",StringComparison.OrdinalIgnoreCase)>=0)
                    {
                        foreach(var hero in ParticipatingHeroes)RemoveStatus(hero.Statuses,"Weakened",hero.DisplayName,encounter.CardName);
                        remainder=Regex.Replace(remainder,@"(?:;?\s*)?remove Weakened from all Heroes",string.Empty,RegexOptions.IgnoreCase).Trim(' ',';');
                    }
                    if(!string.IsNullOrWhiteSpace(remainder))AppendPendingContent(encounter.CardId+" reward remainder",remainder);
                }
                return;
            }

            AppendPendingContent(encounter.CardId+" victory reward",reward);
        }

        private void FinalizeEncounterObjects(EncounterRuntimeState encounter)
        {
            if(encounter==null)return;
            if(encounter.CardId=="EN007"||encounter.CardId=="EN024")
            {
                var fires=EncounterObjects.Where(o=>o.EncounterInstanceId==encounter.InstanceId&&o.Type=="Fire Token").ToList();
                encounter.AllFireTokensRemovedBeforeClear=fires.Count==0;
                if(fires.Count>0&&encounter.CardId=="EN007")
                {
                    foreach(var hero in ParticipatingHeroes.Where(h=>!h.Dead))ApplyHeroDamageNow(hero,fires.Count,"Burning Cart remaining Fire Tokens");
                    AddLog($"ENCOUNTER CONSEQUENCE — {fires.Count} Fire Token{(fires.Count==1?string.Empty:"s")} remained; each Hero suffers {fires.Count} damage as the Burning Cart encounter ends.");
                }
                if(fires.Count>0&&encounter.CardId=="EN024")
                {
                    int loss=fires.Count*5;
                    AddPartyGold(-loss,"Firestarter Raid remaining Fire Tokens");
                    AddLog($"ENCOUNTER CONSEQUENCE — {fires.Count} Fire Token{(fires.Count==1?string.Empty:"s")} remained; the party loses {loss} Gold in supplies.");
                }
                foreach(var fire in fires)RemoveEncounterObject(fire,"encounter ended",false);
            }
            foreach(var obj in EncounterObjects.Where(o=>o.EncounterInstanceId==encounter.InstanceId&&o.RemoveAtEncounterEnd).ToList())RemoveEncounterObject(obj,"encounter ended",false);
        }

        private bool TryCompleteEncounter(EncounterRuntimeState encounter,string reason)
        {
            if(encounter==null||encounter.RewardResolved)return false;
            int remaining=RemainingEncounterEnemies(encounter);
            int blocking=RemainingEncounterBlockingObjects(encounter);
            if(remaining>0||blocking>0)return false;
            AddLog($"ENCOUNTER CLEARED — {encounter.CardId} {encounter.CardName} at {(Locations.TryGetValue(encounter.LocationId,out var location)?location.Name:encounter.LocationId)} ({reason}).");
            FinalizeEncounterObjects(encounter);
            ResolveEncounterReward(encounter);
            ResolvePostEncounter0852(encounter);
            ResolvePostEncounter0853(encounter);
            ResolveLocationPostEncounter0858(encounter);
            LastCompletedEncounterCardId=encounter.CardId;
            CompletedEncounterCount++;
            ActiveEncounters.Remove(encounter);
            StateChanged?.Invoke();
            return true;
        }

        private bool RemoveEnemyFromPlayWithoutDefeat(EnemyState enemy,string source)
        {
            if(enemy==null||enemy.Defeated)return false;
            var encounter=EncounterForEnemy(enemy);
            bool scoutConsequence=encounter!=null&&(encounter.CardId=="EN003"||encounter.CardId=="EN026")&&string.Equals(enemy.TemplateId,"E004",StringComparison.OrdinalIgnoreCase);
            if(scoutConsequence)
            {
                encounter.HadEnemyLeavePlayUndefeated=true;
                QueueNextTravelEncounterSeverity(1,$"{encounter.CardName}: Scout escaped");
                AddLog($"ENCOUNTER FAILURE — {enemy.DisplayName} leaves play without being Defeated; next Travel Encounter Severity gains +1.");
            }
            ResolveEncounterEnemyExit0853(encounter,enemy);
            if(encounter!=null&&encounter.CardId=="EN017"&&SearchEncounterContent0820.IsEliteEnemyTemplate(enemy.TemplateId))
            {
                encounter.UndefeatedEliteExitCount++;
                if(encounter.UndefeatedEliteExitCount>=2&&encounter.GainedEquipmentIds.Count>0)
                {
                    var owner=RewardHeroFor(encounter.SourceHeroId);
                    foreach(var gainedId in encounter.GainedEquipmentIds.ToList())DiscardAcquiredEquipment(owner,gainedId,"Saboteur's Cache: both Elite Goblins left play");
                    encounter.GainedEquipmentIds.Clear();
                    AddLog("ENCOUNTER FAILURE — both Elite Goblins left Saboteur's Cache without being Defeated; the Equipment gained from this Encounter is discarded.");
                }
            }
            AddLog($"ENEMY LEAVES PLAY — {enemy.DisplayName} ({source}).");
            enemy.Health=0;
            if(EnemyAvatars.TryGetValue(enemy.Id,out var avatar))avatar.gameObject.SetActive(false);
            if(encounter!=null)TryCompleteEncounter(encounter,$"{enemy.DisplayName} left play");
            StateChanged?.Invoke();
            return true;
        }

        private void CheckEncounterCompletionForEnemy(EnemyState defeatedEnemy)
        {
            if(defeatedEnemy==null||string.IsNullOrEmpty(defeatedEnemy.EncounterInstanceId))return;
            var encounter=ActiveEncounters.FirstOrDefault(e=>string.Equals(e.InstanceId,defeatedEnemy.EncounterInstanceId,StringComparison.OrdinalIgnoreCase));
            if(encounter!=null)TryCompleteEncounter(encounter,$"{defeatedEnemy.DisplayName} defeated");
        }

        private void ResolveTravelEncounter(LocationState location,HeroState hero,int encounterLevel,int explicitSeverityModifier,string source,int forcedDie=0,int fixedSeverity=0,bool ignoreTravelControl=false)
        {
            if(location==null||hero==null)return;
            if(WorldEventRandomTravelSuppressed0857(location)){AddLog($"{CurrentWorldEventId0833} — random Travel Encounter suppressed at {location.Name} ({source}).");return;}
            if(!ignoreTravelControl&&SkipNextTravelEncounter)
            {
                SkipNextTravelEncounter=false;
                AddLog($"TRAVEL ENCOUNTER SKIPPED — {location.Name} ({source}) consumes the pending skip effect; no Encounter card is drawn.");
                StateChanged?.Invoke();
                return;
            }
            var card=DrawEligibleEncounterCard(location);
            card=AdjustEncounterDrawForSearch0852(card,location);
            if(card==null){AddLog($"TRAVEL ENCOUNTER — no eligible Campaign I encounter remains for {location.Name}.");pendingEncounterPostResolve0852=null;return;}
            explicitSeverityModifier+=ConsumeSearchEncounterSeverityModifier0852(card);
            int die=fixedSeverity>0?0:(forcedDie>=1&&forcedDie<=6?forcedDie:UnityEngine.Random.Range(1,7));
            int worldEventModifier=WorldEventTravelSeverityModifier0833+ConsumeWorldEventEncounterSeverity0857(card,location,fixedSeverity);
            int queuedSeverityModifier=fixedSeverity>0?0:NextTravelEncounterSeverityModifier;
            if(fixedSeverity<=0)NextTravelEncounterSeverityModifier=0;
            int severity=fixedSeverity>0?Mathf.Max(1,fixedSeverity):Mathf.Max(1,die+worldEventModifier+explicitSeverityModifier+queuedSeverityModifier);
            LastEncounterCardId=card.Id;LastEncounterSeverity=severity;
            string effect=card.EffectForSeverity(severity);
            var encounter=new EncounterRuntimeState
            {
                InstanceId=$"TE_{++encounterInstanceSerial:0000}",CardId=card.Id,CardName=card.Name,LocationId=location.Id,SourceHeroId=hero.Id,Source=source,
                Severity=severity,StartedRound=Round,RewardText=card.Reward,FailureText=card.Failure,PostResolveEffect0852=ConsumePendingEncounterPost0852()
            };
            ActiveEncounters.Add(encounter);
            ApplySearchEncounterPreSetup0852(card,hero);
            string modifierText=explicitSeverityModifier==0?string.Empty:$" + modifier {explicitSeverityModifier}";
            string queuedModifierText=queuedSeverityModifier==0?string.Empty:$" + pending {queuedSeverityModifier}";
            if(fixedSeverity>0)AddLog($"TRAVEL ENCOUNTER — {card.Id} {card.Name} at {location.Name} ({source}). Fixed Severity {severity} (Severity 3-4 band when fixed at 3; Location Encounter Level {encounterLevel}).");
            else AddLog($"TRAVEL ENCOUNTER — {card.Id} {card.Name} at {location.Name} ({source}). d6 {die}"+(worldEventModifier==0?string.Empty:$" + {CurrentWorldEventId0833} {worldEventModifier}")+modifierText+queuedModifierText+$" = Severity {severity} (Location Encounter Level {encounterLevel}).");
            AddLog($"ENCOUNTER EFFECT — {effect}");
            ResolveEncounterImmediateEffect(card,effect,severity,location,hero,encounter);
            ApplyWorldEventEncounterSetup0857(location,hero,encounter);
            ApplyEncounterMovementRules(card,severity,encounter);
            if(!string.IsNullOrWhiteSpace(card.Failure) && !IsEncounterFailureImplemented0853(card.Id))
                AppendPendingContent(card.Id+" consequence / ongoing rule",card.Failure);
            int blockingObjects=RemainingEncounterBlockingObjects(encounter);
            if(encounter.SpawnedEnemyIds.Count>0||blockingObjects>0)
            {
                AddLog($"ENCOUNTER ACTIVE — {encounter.InstanceId} tracks {encounter.SpawnedEnemyIds.Count} spawned {(encounter.SpawnedEnemyIds.Count==1?"Enemy":"Enemies")} and {blockingObjects} blocking object{(blockingObjects==1?string.Empty:"s")}; reward is held until all are cleared.");
                if(card.Id=="EN023"&&severity>=5)
                {
                    if(EncounterRequiresPriorityActivation(encounter)){IsResolving=true;StartCoroutine(ResolvePriorityEncounterActivation(encounter));}
                    else AddLog("ENCOUNTER INITIATIVE — Wolf Rider Sweep does not activate first because at least one Hero is Hidden.");
                }
            }
            else if(IsEncounterAwaitingChoice0853(encounter))
            {
                AddLog($"ENCOUNTER ACTIVE — {encounter.InstanceId} is awaiting its authored player choice before it can resolve.");
            }
            else
            {
                ActiveEncounters.Remove(encounter);
                encounter.RewardResolved=true;
                AddLog($"ENCOUNTER RESOLVED — {card.Id} {card.Name} has no spawned combatants; its immediate/pending effect is complete.");
                ResolvePostEncounter0852(encounter);
                ResolvePostEncounter0853(encounter);
                ResolveLocationPostEncounter0858(encounter);
            }
            EncounterDiscard.Add(card.Id);
        }

        private LocationState SearchTargetFor(HeroState hero)
        {
            if(hero==null)return null;
            // A Reveal effect that says "immediately Search this Location" targets that newly revealed
            // Location even though Explore itself does not move the Hero into it.
            if(!string.IsNullOrEmpty(hero.FreeSearchLocationId)&&Locations.TryGetValue(hero.FreeSearchLocationId,out var freeLocation)&&freeLocation.Revealed&&!freeLocation.Searched)return freeLocation;
            return Locations.TryGetValue(hero.LocationId,out var current)&&current.Revealed&&!current.Searched?current:null;
        }

        public bool CanSearchCurrentLocation(HeroState hero)
        {
            if(hero==null||hero.IsDefeated||hero.Moving)return false;
            var location=SearchTargetFor(hero);if(location==null)return false;
            bool granted=string.Equals(hero.FreeSearchLocationId,location.Id,StringComparison.OrdinalIgnoreCase);
            if(!CanSearchLocation0852(hero,location,granted)||!CanSearchLocation0858(hero,location,granted))return false;
            // 0.8.51: an explicit game effect that grants Search bypasses the normal action gate,
            // including engagement, turn ownership and Action cost. Normal player Search does not.
            if(granted)return CanResolveGrantedSearch0851(hero,location.Id);
            if(IsResolving||hero.TurnCompleted||!CanUseTurnActions(hero))return false;
            if(Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==hero.LocationId))return false;
            return GeneralActions(hero)>=1;
        }

        public void SearchCurrentLocation()
        {
            var hero=SelectedHero;var location=SearchTargetFor(hero);if(hero==null||location==null)return;
            bool granted=string.Equals(hero.FreeSearchLocationId,location.Id,StringComparison.OrdinalIgnoreCase);
            if(granted)
            {
                ResolveGrantedSearch0851(hero,$"{location.Name} effect",location.Id);
                return;
            }
            if(Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==hero.LocationId)){AddLog($"SEARCH blocked — {hero.DisplayName} is Engaged.");StateChanged?.Invoke();return;}
            if(!CanSearchCurrentLocation(hero))return;if(!RequireHeroTurn(hero,"Search"))return;
            hero.Actions--;location.Searched=true;location.SearchRequests++;
            AddLog($"SEARCH — {hero.DisplayName} searches {location.Name} (1 Action).");
            if(!TryResolveQuestSearchOverride0829(location,hero)&&!ResolveLocationSearch0858(location,hero))ResolveSearchDraw(location, hero);
            QueueLocationPostSearch0858(location,hero);
            ConsumeForgottenKeyBypassAfterSearch0852(hero,location);
            SearchRequested?.Invoke(location,hero);StateChanged?.Invoke();
        }

        public bool CanUseLocationRevealOption(HeroState hero)
        {
            if(hero==null||hero.IsDefeated)return false;
            if(string.IsNullOrEmpty(hero.PendingLocationOption)||string.IsNullOrEmpty(hero.PendingLocationOptionLocationId))return CanUseSearchUtility0852(hero);
            return Locations.TryGetValue(hero.PendingLocationOptionLocationId,out var location)&&location.Revealed;
        }

        public void UseLocationRevealOption(string choice=null)
        {
            var hero=SelectedHero;if(!CanUseLocationRevealOption(hero))return;
            if(string.IsNullOrEmpty(hero.PendingLocationOption)){UseSearchUtility0852();return;}
            string option=hero.PendingLocationOption;string locationId=hero.PendingLocationOptionLocationId;
            hero.PendingLocationOption=null;hero.PendingLocationOptionLocationId=null;
            if(option=="Hide")
            {
                AddStatus(hero,"Hidden",1,0,"Concealing Brush",true,hero,"until the start of your next turn");
                AddLog($"LOCATION EFFECT — {hero.DisplayName} Hides using Concealing Brush.");
            }
            else if(option=="Fresh Water")
            {
                bool cleanse=string.Equals(choice,"Cleanse",StringComparison.OrdinalIgnoreCase)||(!string.Equals(choice,"Heal",StringComparison.OrdinalIgnoreCase)&&hero.HasStatus("Poisoned")&&hero.Health>=hero.MaxHealth);
                if(cleanse&&hero.HasStatus("Poisoned")){RemoveStatus(hero.Statuses,"Poisoned",hero.DisplayName,"Fresh Water");AddLog($"LOCATION EFFECT — {hero.DisplayName} removes Poisoned using Fresh Water.");}
                else {HealHero(hero,1,"Fresh Water",hero);AddLog($"LOCATION EFFECT — {hero.DisplayName} uses Fresh Water to recover 1 Health.");}
            }
            AddLog($"LOCATION OPTION RESOLVED — {Locations[locationId].Name}: {option}.");StateChanged?.Invoke();
        }

        private bool IsRangedHeroAttack(HeroState hero,CardData card,string source)
        {
            if(hero==null)return false;
            if(Contains(card?.Traits,"Ranged")||Contains(card?.Keywords,"Ranged"))return true;
            return string.Equals(AttackStyleFor(hero,source??string.Empty,card),"Ranged",StringComparison.OrdinalIgnoreCase);
        }

        private int LocationAttackModifier(string attackerLocationId,string targetLocationId,bool ranged)
        {
            if(!ranged)return 0;int modifier=0;
            if(Locations.TryGetValue(attackerLocationId??string.Empty,out var from))
            {
                if(from.OngoingEffect?.IndexOf("High Ground",StringComparison.OrdinalIgnoreCase)>=0)modifier+=1;
                if(from.OngoingEffect?.IndexOf("Open Sightlines",StringComparison.OrdinalIgnoreCase)>=0)modifier+=1;
                if(from.OngoingEffect?.IndexOf("Limited Visibility",StringComparison.OrdinalIgnoreCase)>=0)modifier-=1;
            }
            if(!string.Equals(attackerLocationId,targetLocationId,StringComparison.OrdinalIgnoreCase)&&Locations.TryGetValue(targetLocationId??string.Empty,out var to))
            {
                if(to.OngoingEffect?.IndexOf("Open Sightlines",StringComparison.OrdinalIgnoreCase)>=0)modifier+=1;
                if(to.OngoingEffect?.IndexOf("Limited Visibility",StringComparison.OrdinalIgnoreCase)>=0)modifier-=1;
            }
            return modifier;
        }

        public bool LocationProvidesCoverAgainstRanged(string locationId)
        {
            if(!Locations.TryGetValue(locationId??string.Empty,out var location))return false;
            if(location.OngoingEffect?.IndexOf("no Cover",StringComparison.OrdinalIgnoreCase)>=0)return false;
            string ongoing=location.OngoingEffect??string.Empty;
            return LocationHasTag(locationId,"Cover")||ongoing.IndexOf("Cover vs ranged",StringComparison.OrdinalIgnoreCase)>=0||ongoing.IndexOf("gain Cover",StringComparison.OrdinalIgnoreCase)>=0||ongoing.IndexOf("gains Cover",StringComparison.OrdinalIgnoreCase)>=0;
        }

        private int LocationEnemyArmorModifier(EnemyState enemy)
        {
            if(enemy==null||!Locations.TryGetValue(enemy.LocationId??string.Empty,out var location))return 0;
            bool goblin=enemy.DisplayName?.IndexOf("Goblin",StringComparison.OrdinalIgnoreCase)>=0;
            bool fortressDefender=string.Equals(location.AuthoredLocationId,"L020",StringComparison.OrdinalIgnoreCase);
            return goblin&&(fortressDefender||location.OngoingEffect?.IndexOf("Goblins here gain +1 Armor",StringComparison.OrdinalIgnoreCase)>=0)?1:0;
        }

        public int DebugLocationAttackModifier(string attackerLocationId,string targetLocationId,bool ranged)=>LocationAttackModifier(attackerLocationId,targetLocationId,ranged);
        public int DebugLocationEnemyArmorModifier(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var enemy)?LocationEnemyArmorModifier(enemy):0;

        private void BuildHeroes()
        {
            foreach (var def in HeroDatabase.Heroes.OrderBy(h => h.heroCardId))
            {
                var hero = new HeroState
                {
                    Id=def.id, HeroCardId=def.heroCardId, DisplayName=def.displayName, ClassName=def.className,
                    PrimaryRole=def.primaryRole, SecondaryRole=def.secondaryRole, Traits=def.traits, CoreMechanic=def.coreMechanic,
                    Level=def.level, MaxHealth=def.maxHealth, Health=def.maxHealth, BaseAttack=def.baseAttack, BaseArmor=def.baseArmor,
                    Attack=def.attack, Armor=def.armor, LocationId="L000", Actions=0, HealingPotions=1
                };
                if (def.abilities != null) foreach (var a in def.abilities) hero.Abilities.Add(new HeroAbilityDefinition { Level=a.level, Slot=a.slot, Name=a.name, Text=a.text });
                if (def.startingEquipment != null) foreach (var e in def.startingEquipment) hero.StartingEquipment.Add(new EquipmentRuntimeData { Id=e.id, Name=e.name, Type=e.type, Copies=e.copies, Attack=e.attack, Armor=e.armor, Ability1=e.ability1, Ability2=e.ability2 });
                hero.Deck = HeroDatabase.BuildPhysicalDeck(hero.Id);
                Shuffle(hero.Deck);
                DrawCards(hero, 5, false);
                Heroes[hero.Id] = hero;
            }
        }

        private void BuildEnemies()
        {
            // 0.8.50: Q001 opening setup is party-scaled and contains no Captain.
            // Initialize() runs before party selection, so the first call intentionally does nothing;
            // SetParty() configures the live opening force after the chosen party is known.
            if(PartySize<=0||QuestRuntime0829==null||CurrentQuestId0829!="Q001")return;
            int raiders=PartySize<=1?1:PartySize==2?2:3;
            int archers=PartySize>=4?2:1;
            for(int i=0;i<raiders;i++)SpawnQuestEnemy0829("E001","L000","Q001 opening setup");
            for(int i=0;i<archers;i++)SpawnQuestEnemy0829("E002","L000","Q001 opening setup");
        }
        private void AddEnemy(EnemyState enemy) => Enemies[enemy.Id] = enemy;

        public HeroState SelectedHero => Heroes.TryGetValue(SelectedHeroId, out var hero) ? hero : null;
        public HeroState TurnHero => !string.IsNullOrEmpty(TurnHeroId) && Heroes.TryGetValue(TurnHeroId, out var hero) ? hero : null;
        public EnemyState SelectedEnemy => !string.IsNullOrEmpty(SelectedEnemyId) && Enemies.TryGetValue(SelectedEnemyId, out var enemy) ? enemy : null;

        // 0.10.0 — transport/server commands need an actor/target context without generating
        // local camera/log/UI side effects. This is intentionally not exposed to gameplay UI.
        public void SetAuthorityCommandContext0100(string heroId, string enemyId = null)
        {
            if (!string.IsNullOrWhiteSpace(heroId) && Heroes.TryGetValue(heroId, out var hero) && hero.Participating)
                SelectedHeroId = hero.Id;
            SelectedEnemyId = !string.IsNullOrWhiteSpace(enemyId) && Enemies.TryGetValue(enemyId, out var enemy) && !enemy.Defeated ? enemy.Id : null;
        }

        public bool IsHeroTakingTurn(HeroState hero) => hero != null && !string.IsNullOrEmpty(TurnHeroId) && string.Equals(hero.Id, TurnHeroId, StringComparison.OrdinalIgnoreCase);
        public bool CanStartHeroTurn(HeroState hero) => hero != null && hero.Participating && !hero.IsDefeated && !hero.TurnCompleted && TurnHero == null && !IsResolving && !HasPendingReactionWindow && !HasPendingEncounterDeckControl && !ParticipatingHeroes.Any(h => h.Moving) && !HasPendingRewardChoice;
        public bool CanUseTurnActions(HeroState hero) => hero != null && hero.Participating && !hero.IsDefeated && !hero.TurnCompleted && !hero.HasStatus("Stunned") && !HasPendingEncounterDeckControl && IsHeroTakingTurn(hero) && !HasPendingRewardChoice;
        public bool CanHeroVoluntarilyMove(HeroState hero) => hero != null && !hero.HasStatus("Rooted") && !hero.HasStatus("Knocked Down") && !hero.HasStatus("Search No Move");
        public int GeneralActions(HeroState hero) => hero == null ? 0 : Mathf.Max(0, hero.Actions - hero.AttackOnlyActions);
        private bool CanUseStandardAction(HeroState hero, int cost = 1) => CanUseTurnActions(hero) && GeneralActions(hero) >= cost;

        public void StartSelectedHeroTurn()
        {
            TryStartHeroTurn(SelectedHero);
        }

        public bool TryStartHeroTurn(HeroState hero)
        {
            if (!CanStartHeroTurn(hero))
            {
                if (hero == null) return false;
                if (hero.IsDefeated) AddLog($"TURN START blocked — {hero.DisplayName} is Downed or Dead.");
                else if (hero.TurnCompleted) AddLog($"TURN START blocked — {hero.DisplayName} already completed their turn this Hero Phase.");
                else if (TurnHero != null) AddLog($"TURN IN PROGRESS — {TurnHero.DisplayName} must finish their turn before {hero.DisplayName} can start.");
                else AddLog($"TURN START blocked — finish the current movement/reaction/resolution first.");
                StateChanged?.Invoke();
                return false;
            }

            AdvanceLocationTagClock(hero, StatusExpiryTiming.StartOfSourceTurn);
            AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.StartOfTargetTurn, hero.DisplayName);
            AdvanceSourceStatusClock(hero, StatusExpiryTiming.StartOfSourceTurn);
            if(hero.HasStatus("Regenerating"))HealHero(hero,1,"Regenerating",hero);
            TurnHeroId = hero.Id;
            HeroRulesRuntime.ResetTurn(hero);
            OnEquipmentTurnStart0854(hero);
            OnLocationHeroTurnStarted0858(hero);
            hero.MovedThisTurn = false;
            hero.Actions = 0;
            hero.AttackOnlyActions = 0;
            AddLog($"TURN START — {hero.DisplayName}. Hero activation order is player-chosen this Round.");

            // Stunned Heroes still consume their next activation, matching Enemy Stunned behavior:
            // the activation begins, start-of-activation effects can resolve, then it immediately ends.
            if (hero.HasStatus("Stunned"))
            {
                AddLog($"STATUS — {hero.DisplayName} is Stunned and immediately ends this activation with 0 Actions.");
                if(hero.HasStatus("Burning")&&!hero.Dead)ApplyHeroDamageNow(hero,1,"Burning");
                hero.TurnCompleted = true;
                AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfTargetTurn, hero.DisplayName);
                AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfTargetActivation, hero.DisplayName);
                AdvanceSourceStatusClock(hero, StatusExpiryTiming.EndOfSourceTurn);
                TurnHeroId = null;
                var stunnedRemaining = ParticipatingHeroes.Where(h=>!h.IsDefeated&&!h.TurnCompleted).OrderBy(h=>h.PartyIndex).ToList();
                if(stunnedRemaining.Count==0) StartCoroutine(ResolveRound());
                else AddLog($"HERO PHASE — choose the next Hero to take a turn ({stunnedRemaining.Count} remaining).");
                StateChanged?.Invoke();
                return true;
            }

            if (Round == 1)
                AddLog($"DRAW STEP — {hero.DisplayName} keeps the 5-card starting hand; no extra Round 1 turn draw.");
            else
                DrawCards(hero, hero.DrawValue);
            int actionGrant = hero.HasStatus("Slowed") ? Mathf.Max(1, 3 - 1) : 3;
            if(hero.NextTurnActionPenalty>0)
            {
                int penalty=hero.NextTurnActionPenalty;
                hero.NextTurnActionPenalty=0;
                actionGrant=Mathf.Max(0,actionGrant-penalty);
                AddLog($"ENCOUNTER EFFECT — {hero.DisplayName} loses {penalty} Action{(penalty==1?"":"s")} from the next-turn penalty.");
            }
            hero.Actions = actionGrant;
            OnHeroTurnStarted0860(hero);
            AddLog(hero.HasStatus("Slowed")
                ? $"ACTION STEP — {hero.DisplayName} is Slowed and gains {actionGrant} Actions after modifiers."
                : $"ACTION STEP — {hero.DisplayName} gains {actionGrant} Actions.");
            if(hero.HasStatus("Knocked Down") && hero.Actions > 0)
            {
                hero.Actions--;
                RemoveStatus(hero.Statuses,"Knocked Down",hero.DisplayName,"spends the first Action to Stand");
                AddLog($"STATUS — {hero.DisplayName} spends 1 Action to Stand and removes Knocked Down ({hero.Actions} Action{(hero.Actions==1?"":"s")} remaining).");
            }
            StateChanged?.Invoke();
            return true;
        }

        private bool RequireHeroTurn(HeroState hero, string action)
        {
            if (hero == null || hero.IsDefeated || hero.TurnCompleted) return false;
            if (TurnHero == null)
            {
                AddLog($"TURN REQUIRED — select {hero.DisplayName} and press Start Turn before attempting to {action}.");
                StateChanged?.Invoke();
                return false;
            }
            if (IsHeroTakingTurn(hero)) return true;
            AddLog($"TURN IN PROGRESS — {TurnHero.DisplayName} must finish their turn before {hero.DisplayName} can {action}.");
            return false;
        }
        private bool CanPayCardCost(HeroState hero, CardData card, int cost) => hero != null && (card != null && card.Kind == CardKind.Attack ? hero.Actions >= cost : GeneralActions(hero) >= cost);
        private bool CanPayReactionCost(HeroState hero, CardData card, int cost) =>
            hero != null && card != null && card.Kind == CardKind.Reaction && hero.Participating && !hero.IsDefeated && !hero.HasStatus("Stunned") && GeneralActions(hero) >= cost;
        private void SpendCardCost(HeroState hero, CardData card, int cost)
        {
            if (hero == null || cost <= 0) return;
            if (card != null && card.Kind == CardKind.Attack)
            {
                int restricted = Mathf.Min(hero.AttackOnlyActions, cost);
                hero.AttackOnlyActions -= restricted;
                hero.Actions -= cost;
            }
            else hero.Actions -= cost;
        }

        public int GraphDistance(string fromId, string toId)
        {
            if (string.Equals(fromId, toId, StringComparison.OrdinalIgnoreCase)) return 0;
            if (!Locations.ContainsKey(fromId) || !Locations.ContainsKey(toId)) return int.MaxValue;
            var queue = new Queue<(string id, int dist)>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { fromId }; queue.Enqueue((fromId, 0));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in Locations[current.id].Connections)
                {
                    if (!seen.Add(next)) continue;
                    if (string.Equals(next, toId, StringComparison.OrdinalIgnoreCase)) return current.dist + 1;
                    queue.Enqueue((next, current.dist + 1));
                }
            }
            return int.MaxValue;
        }

        private EncounterRuntimeState EncounterForEnemy(EnemyState enemy)
        {
            if(enemy==null||string.IsNullOrEmpty(enemy.EncounterInstanceId))return null;
            return ActiveEncounters.FirstOrDefault(e=>string.Equals(e.InstanceId,enemy.EncounterInstanceId,StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsGoblinEnemy(EnemyState enemy)=>enemy!=null&&!string.IsNullOrEmpty(enemy.DisplayName)&&enemy.DisplayName.IndexOf("Goblin",StringComparison.OrdinalIgnoreCase)>=0;
        private static bool IsBeastEnemy(EnemyState enemy)
        {
            if(enemy==null||string.IsNullOrEmpty(enemy.TemplateId))return false;
            if(!enemy.TemplateId.StartsWith("E",StringComparison.OrdinalIgnoreCase))return false;
            if(!int.TryParse(enemy.TemplateId.Substring(1),out int n))return false;
            return n>=20&&n<=29;
        }

        private int EnemyMovementAllowance(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return 0;
            if(enemy.HasStatus("Time Stopped"))return 0;
            int movement=QuestEnemyBaseMovement0830(enemy);
            if(enemy.Statuses.Any(s=>string.Equals(s.Name,"Fresh Tracks Movement -1",StringComparison.OrdinalIgnoreCase)))movement--;
            foreach(var status in enemy.Statuses.Where(s=>string.Equals(s.Name,"Encounter Movement +1",StringComparison.OrdinalIgnoreCase)))movement++;
            movement+=WorldEventEnemyMovementBonus0857(enemy)+EnemyMovementModifier0859(enemy);
            return Mathf.Max(0,movement);
        }

        private string NextEnemyStepTowardHero(EnemyState enemy)
        {
            string questStep=NextQuestEnemyStep0831(enemy);if(!string.IsNullOrEmpty(questStep))return questStep;
            if(enemy==null||EnemyShouldHoldPosition0859(enemy)||!Locations.TryGetValue(enemy.LocationId,out var current))return null;
            var targets=ParticipatingHeroes.Where(h=>!h.IsDefeated&&!h.Moving&&Locations.ContainsKey(h.LocationId)).ToList();
            if(targets.Count==0)return null;
            int currentBest=targets.Min(h=>GraphDistance(enemy.LocationId,h.LocationId));
            return current.Connections.Where(id=>Locations.TryGetValue(id,out var l)&&l.Revealed)
                .Select(id=>new{Id=id,Distance=targets.Min(h=>GraphDistance(id,h.LocationId))})
                .Where(x=>x.Distance<currentBest)
                .OrderBy(x=>x.Distance).ThenBy(x=>x.Id,StringComparer.OrdinalIgnoreCase).Select(x=>x.Id).FirstOrDefault();
        }

        private int MoveEnemyTowardNearestHero(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return 0;
            int rangedReposition=TryRangedReposition0859(enemy);if(rangedReposition>0)return rangedReposition;
            if(ChooseEnemyAttackTarget(enemy)!=null)return 0;
            int budget=EnemyMovementAllowance(enemy),steps=0;
            while(budget>0&&ChooseEnemyAttackTarget(enemy)==null)
            {
                if(!Locations.TryGetValue(enemy.LocationId,out var current))break;
                bool ignoresTerrain=WorldEventEnemyIgnoresDifficultTerrain0857(enemy)||EnemyIgnoresDifficultTerrain0859(enemy);
                int stepCost=IsDifficultTerrain(current.Id)&&!ignoresTerrain?2:1;
                if(budget<stepCost)break;
                string next=NextEnemyStepTowardHero(enemy);
                if(string.IsNullOrEmpty(next))break;
                string from=current.Name; enemy.LocationId=next; budget-=stepCost; steps++;enemy.MovementStepsThisActivation0859++;
                if(EnemyAvatars.TryGetValue(enemy.Id,out var avatar)&&Locations.TryGetValue(next,out var destination))avatar.transform.position=destination.Position;
                AddLog($"ENEMY MOVE — {enemy.DisplayName} moves {from} → {Locations[next].Name} (cost {stepCost}; {budget} Movement remaining).");
                OnEnemyEnteredHeroCardLocation0860(enemy);
                if(ResolveLootTrapOnEnemyEnter0854(enemy))break;
            }
            if(steps==0&&EnemyMovementAllowance(enemy)>0&&Locations.TryGetValue(enemy.LocationId,out var stalled)&&IsDifficultTerrain(stalled.Id)&&!WorldEventEnemyIgnoresDifficultTerrain0857(enemy)&&!EnemyIgnoresDifficultTerrain0859(enemy)&&EnemyMovementAllowance(enemy)<2)
                AddLog($"ENEMY MOVE — {enemy.DisplayName} cannot leave {stalled.Name}; Difficult Terrain requires 2 Movement.");
            return steps;
        }

        private void ApplyEncounterMovementRules(EncounterCardDefinition card,int severity,EncounterRuntimeState encounter)
        {
            if(card==null||encounter==null)return;
            if(card.Id=="EN025")
            {
                foreach(var id in encounter.SpawnedEnemyIds)
                    if(Enemies.TryGetValue(id,out var enemy)&&IsBeastEnemy(enemy))AddStatus(enemy,"Encounter Movement +1",0,1,card.Name,false,null,"until end of round");
                AddLog("ENCOUNTER MOVEMENT — Beastmaster Hunt Beasts gain +1 Movement during Round 1.");
            }
            bool goblinEncounter=encounter.SpawnedEnemyIds.Any(id=>Enemies.TryGetValue(id,out var e)&&IsGoblinEnemy(e));
            if(goblinEncounter&&NextGoblinEncounterWarDrummer)
            {
                var location=Locations.TryGetValue(encounter.LocationId,out var drumLocation)?drumLocation:null;
                if(location!=null)SpawnEncounterEnemy("E008",location,encounter);
                NextGoblinEncounterWarDrummer=false;
                var builder=UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>();if(builder!=null)builder.EnsureRuntimeEnemySpawns();
                AddLog("ENCOUNTER REINFORCEMENT — War Drums adds 1 E008 Goblin War Drummer to this Goblin Encounter.");
            }
            if(goblinEncounter&&NextGoblinEncounterMovementBonus)
            {
                foreach(var id in encounter.SpawnedEnemyIds.ToList())
                    if(Enemies.TryGetValue(id,out var enemy)&&IsGoblinEnemy(enemy))AddStatus(enemy,"Encounter Movement +1",0,1,"War Drums in the Distance",false,null,"until end of round");
                NextGoblinEncounterMovementBonus=false;
                AddLog("ENCOUNTER MOVEMENT — War Drums empowers this Goblin Encounter: Goblins gain +1 Movement during its first round.");
            }
            if(card.Id=="EN016"&&severity>=3&&severity<=4)
            {
                NextGoblinEncounterMovementBonus=true;
                AddLog("ENCOUNTER ONGOING — War Drums: the next Goblin Encounter gains +1 Movement during its first round.");
            }
            if(card.Id=="EN016"&&severity>=5&&severity<=6)
            {
                NextGoblinEncounterWarDrummer=true;
                AddLog("ENCOUNTER ONGOING — War Drums: the next Goblin Encounter adds 1 E008 Goblin War Drummer.");
            }
        }

        private bool EncounterRequiresPriorityActivation(EncounterRuntimeState encounter)
        {
            if(encounter==null||encounter.CardId!="EN023"||encounter.Severity<5)return false;
            return !ParticipatingHeroes.Any(h=>!h.IsDefeated&&h.HasStatus("Hidden"));
        }

        private IEnumerator ResolveSingleEnemyActivation(EnemyState enemy,string phaseLabel="WORLD PHASE")
        {
            if(enemy==null||enemy.Defeated||enemy.ActivatedThisRound)yield break;
            ResolveQuestEnemyStartActivation0829(enemy);
            PrepareEnemyActivation0859(enemy);
            if(ResolveEnemyStartActivationConditions(enemy)){CompleteEnemyActivation(enemy);yield return new WaitForSecondsRealtime(0.06f);yield break;}
            if(ResolveEnemyKnockedDownActivation(enemy)){yield return new WaitForSecondsRealtime(0.06f);yield break;}
            if(enemy.Statuses.Any(s=>string.Equals(s.Name,"Stunned",StringComparison.OrdinalIgnoreCase)||string.Equals(s.Name,"Frozen",StringComparison.OrdinalIgnoreCase)))
            {
                AddLog($"STATUS — {enemy.DisplayName} cannot act during this activation.");ResolveEnemyEndActivationConditions(enemy,false);CompleteEnemyActivation(enemy);yield return new WaitForSecondsRealtime(0.06f);yield break;
            }
            MoveEnemyTowardNearestHero(enemy);
            if(TryEnemyPlaceStandardHazardInsteadOfAttack0859(enemy)||TryAutoSpellReflection0860(enemy)||TryResolveEnemyReplacementAbility0859(enemy))
            {ResolveEnemyEndActivationConditions(enemy,false);CompleteEnemyActivation(enemy);yield return new WaitForSecondsRealtime(0.06f);yield break;}
            var target=ChooseEnemyAttackTarget(enemy);
            if(target==null){ResolveEnemyEndActivationConditions(enemy,false);CompleteEnemyActivation(enemy);yield break;}
            bool performedAttack=true;
            PrepareReactionWindow(enemy,target);
            if(HasPendingReactionWindow&&!pendingReactionResolved){IsResolving=false;StateChanged?.Invoke();while(HasPendingReactionWindow&&!pendingReactionResolved)yield return null;}
            IsResolving=true;ResolvePendingEnemyAttack();
            float guard=0f;while(IsResolving&&guard<2.5f){guard+=Time.unscaledDeltaTime;yield return null;}
            if(HasPendingReactionWindow&&!pendingReactionResolved){IsResolving=false;StateChanged?.Invoke();while(HasPendingReactionWindow&&!pendingReactionResolved)yield return null;}
            if(HasPendingReactionWindow&&pendingReactionResolved)CloseReactionWindow();
            guard=0f;while(IsResolving&&guard<2.5f){guard+=Time.unscaledDeltaTime;yield return null;}
            ResolveEnemyEndActivationConditions(enemy,performedAttack);CompleteEnemyActivation(enemy);yield return new WaitForSecondsRealtime(0.06f);
        }

        private IEnumerator WaitForEncounterDeckControlIfNeeded()
        {
            if(!HasPendingEncounterDeckControl&&!HasPendingRewardChoice)yield break;
            IsResolving=false;StateChanged?.Invoke();
            while(HasPendingEncounterDeckControl)yield return null;
            while(HasPendingRewardChoice)yield return null;
            IsResolving=true;
        }

        private IEnumerator ResolvePriorityEncounterActivation(EncounterRuntimeState encounter)
        {
            if(encounter==null)yield break;
            IsResolving=true;
            AddLog($"ENCOUNTER INITIATIVE — {encounter.CardId} {encounter.CardName}: Encounter Enemies activate immediately before Heroes continue.");
            foreach(var id in encounter.SpawnedEnemyIds.ToList())
                if(Enemies.TryGetValue(id,out var enemy)&&!enemy.Defeated){yield return ResolveSingleEnemyActivation(enemy,"ENCOUNTER INITIATIVE");yield return WaitForEncounterDeckControlIfNeeded();}
            IsResolving=false;StateChanged?.Invoke();
        }

        private int CardRangeConnections(CardData card)
        {
            if (card == null) return 0;
            string range = card.TargetRange ?? string.Empty;
            if (range.IndexOf("Any Location", StringComparison.OrdinalIgnoreCase) >= 0 || range.IndexOf("Battlefield", StringComparison.OrdinalIgnoreCase) >= 0) return 99;
            if (range.IndexOf("Within 3", StringComparison.OrdinalIgnoreCase) >= 0) return 3;
            if (range.IndexOf("Within 2", StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            if (range.IndexOf("Connected", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            string rules = card.Rules ?? string.Empty;
            if (rules.IndexOf("Move to a connected", StringComparison.OrdinalIgnoreCase) >= 0 && rules.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            int parsed = ExtractNumber(card.Rules, @"within\s+(\d+)\s+connections?");
            return parsed > 0 ? parsed : 0;
        }

        private bool CanTargetEnemyWithCard(HeroState hero, CardData card, EnemyState enemy)
        {
            if (hero == null || card == null || enemy == null || enemy.Defeated) return false;
            int baseRange = Mathf.Max(CardRangeConnections(card), hero.NextAttackRangeConnections);
            int allowed = baseRange + LocationRangeBonus0858(hero,baseRange);
            return GraphDistance(hero.LocationId, enemy.LocationId) <= allowed;
        }

        private EnemyState FirstLegalEnemyForCard(HeroState hero, CardData card) => Enemies.Values.Where(e => CanTargetEnemyWithCard(hero, card, e)).OrderBy(e => GraphDistance(hero.LocationId, e.LocationId)).ThenBy(e => e.Id).FirstOrDefault();

        public void SelectHero(string heroId)
        {
            if (!Heroes.TryGetValue(heroId, out var hero) || !hero.Participating) return;
            SelectedHeroId = heroId; SelectedEnemyId = FirstLivingEnemyAtSelectedHeroLocation()?.Id;
            if (IsHeroTakingTurn(hero)) AddLog($"Selected {hero.DisplayName} (turn in progress).");
            else if (TurnHero != null) AddLog($"VIEW — {hero.DisplayName}; {TurnHero.DisplayName}'s turn is still in progress.");
            else if (hero.TurnCompleted) AddLog($"VIEW — {hero.DisplayName} has already completed their turn this Hero Phase.");
            else AddLog($"Selected {hero.DisplayName}; this Hero may take the next turn.");
            if (HeroAvatars.TryGetValue(heroId, out var avatar)) Camera.main?.GetComponent<IsometricCameraController>()?.FocusOn(avatar.transform.position);
            StateChanged?.Invoke();
        }

        public void SelectEnemy(string enemyId)
        {
            if (!Enemies.TryGetValue(enemyId, out var enemy) || enemy.Defeated) return;
            SelectedEnemyId = enemyId; AddLog($"Targeted {enemy.DisplayName}."); StateChanged?.Invoke();
        }

        public void CycleEnemyTarget()
        {
            var hero=SelectedHero; if (hero==null) return;
            var targets=Enemies.Values.Where(e=>!e.Defeated&&GraphDistance(hero.LocationId,e.LocationId)<=3).OrderBy(e=>GraphDistance(hero.LocationId,e.LocationId)).ThenBy(e=>e.Id).ToList();
            if (targets.Count==0) { SelectedEnemyId=null; StateChanged?.Invoke(); return; }
            int current=targets.FindIndex(e=>e.Id==SelectedEnemyId); SelectedEnemyId=targets[(current+1+targets.Count)%targets.Count].Id;
            AddLog($"Targeted {Enemies[SelectedEnemyId].DisplayName}."); StateChanged?.Invoke();
        }

        private bool HasUnusedDifficultTerrainIgnore(HeroState hero, out string usageKey, out string label)
        {
            usageKey=null; label=null; if(hero==null)return false;
            string innate=(HeroDatabase.GetHero(hero.Id)?.innateAbility)??string.Empty;
            if(innate.IndexOf("ignore the additional Move Action required to leave Difficult Terrain",StringComparison.OrdinalIgnoreCase)>=0 && !hero.UsedRoundAbilities.Contains("DifficultTerrain:Pathfinder"))
            { usageKey="DifficultTerrain:Pathfinder"; label="Pathfinder"; return true; }
            foreach(var eq in hero.StartingEquipment)
            {
                string text=(eq.Ability1??string.Empty)+" "+(eq.Ability2??string.Empty);
                if(text.IndexOf("ignore the additional Move Action required to leave Difficult Terrain",StringComparison.OrdinalIgnoreCase)>=0)
                {
                    string key="DifficultTerrain:"+(string.IsNullOrWhiteSpace(eq.Id)?eq.Name:eq.Id);
                    if(!hero.UsedRoundAbilities.Contains(key)){usageKey=key;label=string.IsNullOrWhiteSpace(eq.Name)?"Equipment":eq.Name;return true;}
                }
            }
            return false;
        }

        private int RequiredMoveActions(HeroState hero,string destinationId, out bool ignoresDifficultTerrain, out string ignoreKey, out string ignoreLabel)
        {
            ignoresDifficultTerrain=false;ignoreKey=null;ignoreLabel=null;
            int baseCost=1+SearchMoveSurcharge0852(hero)+WorldEventMoveSurcharge0857(hero,destinationId)+LocationMoveSurcharge0858(hero,destinationId)+EnemyHeroMoveSurcharge0859(hero,destinationId);
            if(hero==null||!IsDifficultTerrain(hero.LocationId))return baseCost;
            if(hero.FreeMoveIgnoresDifficultTerrain){ignoresDifficultTerrain=true;ignoreLabel="card effect";return baseCost;}
            if(HasUnusedDifficultTerrainIgnore(hero,out ignoreKey,out ignoreLabel)){ignoresDifficultTerrain=true;return baseCost;}
            return baseCost+1;
        }

        public int DebugRequiredPaidActionsToMove(string heroId,string locationId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero)||!Locations.ContainsKey(locationId??string.Empty))return -1;
            int required=RequiredMoveActions(hero,locationId,out _,out _,out _);
            int free=hero.FreeMoveReady?1:0; return Mathf.Max(0,required-free);
        }

        public bool CanMoveTo(string locationId)
        {
            var hero=SelectedHero; if (hero==null||hero.IsDefeated||hero.Moving||IsResolving||hero.TurnCompleted) return false;
            if (!CanHeroVoluntarilyMove(hero)||hero.HasStatus("Movement Ended")||!CanUseTurnActions(hero)) return false;
            if (!Locations.TryGetValue(locationId,out var dest)||!dest.Revealed||!Locations.TryGetValue(hero.LocationId,out var current)) return false;
            if (!CanEnterQuestLocation0832(locationId)) return false;
            if (hero.LocationId==locationId||!current.Connections.Contains(locationId)) return false;
            if (!hero.FreeMoveIgnoresEngagement && Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==hero.LocationId)) return false;
            int required=RequiredMoveActions(hero,locationId,out _,out _,out _);
            int free=hero.FreeMoveReady?1:0; int paid=Mathf.Max(0,required-free);
            return GeneralActions(hero)>=paid;
        }

        public void RequestMove(string locationId)
        {
            var selected = SelectedHero;
            if (selected != null && selected.HasStatus("Rooted")) { AddLog($"MOVE blocked — {selected.DisplayName} is Rooted and cannot voluntarily Move."); StateChanged?.Invoke(); return; }
            if (!CanMoveTo(locationId)) { AddLog("Move blocked: destination must be connected/revealed; Hero must satisfy engagement and Move-Action requirements, including Difficult Terrain when leaving it."); return; }
            if (!RequireHeroTurn(selected, "Move")) return;
            var hero=SelectedHero;
            int required=RequiredMoveActions(hero,locationId,out bool ignoresTerrain,out string ignoreKey,out string ignoreLabel);
            bool free=hero.FreeMoveReady; int freeContribution=free?1:0; int paid=Mathf.Max(0,required-freeContribution);
            if(paid>0)hero.Actions-=paid;
            if(SearchMoveSurcharge0852(hero)>0)ConsumeSearchMoveSurcharge0852(hero);
            ConsumeWorldEventMoveSurcharge0857(hero,locationId);
            ConsumeLocationMoveSurcharge0858(hero);
            ConsumeEnemyHeroMoveSurcharge0859(hero);
            if(free){hero.FreeMoveReady=false;hero.FreeMoveIgnoresEngagement=false;hero.FreeMoveIgnoresDifficultTerrain=false;}
            if(ignoresTerrain&&!string.IsNullOrEmpty(ignoreKey))
            {
                hero.UsedRoundAbilities.Add(ignoreKey);
                if(ignoreKey.StartsWith("DifficultTerrain:EQ",StringComparison.OrdinalIgnoreCase))
                    AddLog($"ITEM TRIGGER — {ignoreLabel}: ignores the additional Move Action required to leave Difficult Terrain.");
            }
            hero.MovedThisTurn=true; hero.Moving=true; hero.PendingLocationId=locationId;
            string cost;
            if(required==2&&free)cost="1 free Move + 1 Action (Difficult Terrain exit)";
            else if(required==2)cost="2 Actions (Difficult Terrain exit)";
            else if(free)cost=ignoresTerrain?$"free ({ignoreLabel} ignores Difficult Terrain)":"free";
            else cost=ignoresTerrain?$"1 Action ({ignoreLabel} ignores Difficult Terrain)":"1 Action";
            AddLog($"MOVE — {hero.DisplayName}: {Locations[hero.LocationId].Name} → {Locations[locationId].Name} ({cost}). Pathing...");
            MovementPathPreview.Instance?.Hide();
            if (HeroAvatars.TryGetValue(hero.Id,out var avatar)) avatar.MoveTo(Locations[locationId].Position,()=>CompleteMove(hero.Id,locationId)); else CompleteMove(hero.Id,locationId);
            StateChanged?.Invoke();
        }

        private void CompleteMove(string heroId,string locationId)
        {
            if (!Heroes.TryGetValue(heroId,out var hero)) return;
            string priorLocation=hero.LocationId;
            hero.LocationId=locationId; hero.PendingLocationId=null; hero.Moving=false;
            if (heroId==SelectedHeroId) SelectedEnemyId=FirstLivingEnemyAtSelectedHeroLocation()?.Id;
            AddLog($"MOVE COMPLETE — {hero.DisplayName} arrives at {Locations[locationId].Name}.");
            ResolveDeferredRevealEffectOnEntry0828(Locations[locationId],hero);
            ResolveLocationFirstVisit(Locations[locationId],hero);
            ResolveEncounterObjectsOnHeroEnter(Locations[locationId],hero);
            ResolveSearchArrivalEffects0852A(Locations[locationId],hero);
            if (hero.BonusAfterFreeMove) { hero.BonusAfterFreeMove=false; hero.NextAttackBonus+=1; AddLog($"HERO ABILITY — {hero.DisplayName}'s next Attack gains +1 Attack after the free Move."); }
            if (hero.FreeMoveAttackBonus > 0)
            {
                bool qualifies = !hero.FreeMoveAttackBonusRequiresEnemy || Enemies.Values.Any(e => !e.Defeated && e.LocationId == hero.LocationId);
                if (qualifies) { hero.NextAttackBonus += hero.FreeMoveAttackBonus; AddLog($"CARD EFFECT — {hero.DisplayName}'s next Attack gains +{hero.FreeMoveAttackBonus} Attack after the Move."); }
                hero.FreeMoveAttackBonus = 0; hero.FreeMoveAttackBonusRequiresEnemy = false;
            }
            OnLocationHeroEntered0858(hero,Locations[locationId]);
            OnEnemyHeroEnteredLocation0859(hero);
            OnEnemyHazardHeroEntered0859(hero);
            OnWorldEventHeroEnteredLocation0857(hero,Locations[locationId]);
            HeroRulesRuntime.OnMoveComplete(this,hero); OnEquipmentMoveComplete0854(hero,priorLocation); HeroRulesRuntime.OnHeroEnteredLocation(this, hero); OnQuestHeroMoved0831(hero); StateChanged?.Invoke();
        }

        public void Explore()
        {
            var hero=SelectedHero; if (hero==null||!CanUseStandardAction(hero)||hero.IsDefeated||hero.Moving||IsResolving) return;
            if (Enemies.Values.Any(e=>!e.Defeated&&e.LocationId==hero.LocationId)) { AddLog("EXPLORE blocked while Engaged."); return; }
            var current=Locations[hero.LocationId];
            if(IsExploreBlocked0852(current.Id)){AddLog($"EXPLORE blocked — {current.Name} cannot be used to Explore again this round ({SearchEncounterContent0820.SearchCards["SD036"].Name}).");StateChanged?.Invoke();return;}
            if (!HasLegalExplorationLocation0828(current)) { AddLog("EXPLORE — no connected unrevealed Location is available here."); return; }
            if(!RequireHeroTurn(hero,"Explore")) return;
            hero.Actions--;
            var hidden=DrawLegalExplorationLocation0828(current,"Explore");
            if(hidden==null){hero.Actions++;AddLog("EXPLORE — no legal Exploration card could be drawn; Action refunded.");StateChanged?.Invoke();return;}
            RevealExplorationLocation0828(hidden,hero,true,"Explore");
            AddLog($"EXPLORE — {hidden.Name} is revealed. {hero.DisplayName} stays in {current.Name}.");
            TryBeginRowanTrailblazer0828(hero,"Explore");
            StateChanged?.Invoke();
        }

        public void RescueVillager() { var h=SelectedHero; if(h==null||h.LocationId!="L000"||!CanUseStandardAction(h)||h.IsDefeated||h.Moving||VillagersRescued>=4)return; if(!RequireHeroTurn(h,"Rescue a Villager"))return; h.Actions--;VillagersRescued++;AddLog($"Q001 OBJECTIVE — {h.DisplayName} rescues 1 Villager ({VillagersRescued}/4).");CheckVictory();StateChanged?.Invoke(); }
        public void ExtinguishFire() { var h=SelectedHero;if(h==null||h.LocationId!="L000"||!CanUseStandardAction(h)||h.IsDefeated||h.Moving||FireTokens<=0)return;if(!RequireHeroTurn(h,"Extinguish Fire"))return;h.Actions--;FireTokens--;AddLog($"Q001 OBJECTIVE — {h.DisplayName} removes 1 Fire Token ({FireTokens}/5).");StateChanged?.Invoke(); }

        public void BasicAttack(string enemyId) => ResolveAttack(enemyId,0,0,1,"Basic Attack",null);
        public void AttackSelectedEnemy()
        {
            var hero=SelectedHero; var target=SelectedEnemy;
            int range=hero==null?0:Mathf.Max(hero.NextAttackRangeConnections,EquipmentAttackRange0854(hero));
            if(target==null||target.Defeated||hero==null||GraphDistance(hero.LocationId,target.LocationId)>range) target=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&GraphDistance(hero.LocationId,e.LocationId)<=range);
            if(target==null){AddLog(range>0?$"Basic Attack: no legal Enemy target within {range} connection(s).":"Basic Attack: no legal Enemy target in this Location.");return;} SelectedEnemyId=target.Id; BasicAttack(target.Id);
        }

        public int GetEffectiveCardCost(HeroState hero, CardData card) => HeroCardCostModifier0860(hero,card,EquipmentCardCostModifier0854(hero,card,HeroRulesRuntime.ModifyCardCost(this,hero,card,Mathf.Max(0,card.Cost))));

        private int MaxBoneServants(HeroState hero) => hero != null && hero.Level >= 2 ? 2 : 1;
        private int MaxSpiritTotems(HeroState hero) => 1;

        private static bool IsQuestLimitedCard(CardData card)
        {
            if (card == null || string.IsNullOrEmpty(card.Rules)) return false;
            return Regex.IsMatch(card.Rules, @"\bLimit\s+1\s+per\s+(?:Quest|campaign)\b", RegexOptions.IgnoreCase);
        }

        private bool ValidateCardPlay(HeroState hero, CardData card, ref string enemyId, out string reason)
        {
            reason = null;
            if (hero == null || card == null) { reason = "No Hero/card selected."; return false; }
            if (hero.IsDefeated || hero.Moving || IsResolving || hero.TurnCompleted) { reason = $"{hero.DisplayName} cannot play that card right now."; return false; }
            if (!hero.Hand.Contains(card)) { reason = $"{card.Name}: card is not in {hero.DisplayName}'s hand."; return false; }
            if (IsQuestLimitedCard(card) && hero.UsedQuestCards.Contains(card.Id)) { reason = $"{card.Name}: Limit 1 per Quest/Campaign has already been used."; return false; }
            if (string.Equals(card.Speed, "Reaction", StringComparison.OrdinalIgnoreCase)) { reason = $"{card.Name} is a Reaction. It is available only when its printed Trigger occurs."; return false; }
            if (hero.HasStatus("Stunned")) { reason = $"{hero.DisplayName} is Stunned and cannot perform Actions."; return false; }
            if ((hero.HasStatus("Rooted") || hero.HasStatus("Knocked Down")) && (card.Kind == CardKind.Movement || card.Id == "H001-006" || card.Id == "H012-007")) { reason = $"{hero.DisplayName} cannot voluntarily Move with {card.Name} while {(hero.HasStatus("Rooted")?"Rooted":"Knocked Down")}."; return false; }
            int cost = GetEffectiveCardCost(hero, card);
            if (!CanPayCardCost(hero, card, cost)) { reason = $"{card.Name}: not enough legal Actions ({cost} required)."; return false; }

            if (card.Id == "H016-009" && HeroCreatedPieces.Count(p => p.Type == "Spirit Totem" && p.OwnerHeroId == hero.Id) >= MaxSpiritTotems(hero)) { reason = "Place Spirit Totem: Asha is already at her Spirit Totem maximum."; return false; }
            if (card.Id == "H016-019" && HeroCreatedPieces.Count(p => p.Type == "Spirit Totem" && p.OwnerHeroId == hero.Id) >= MaxSpiritTotems(hero) + 1) { reason = "Grand Totem: no temporary extra Totem slot is available."; return false; }
            if ((card.Id == "H014-009" || card.Id == "H014-019") && HeroCreatedPieces.Count(p => p.Type == "Bone Servant" && p.OwnerHeroId == hero.Id) >= MaxBoneServants(hero)) { reason = $"{card.Name}: Mordren is already at his Bone Servant maximum."; return false; }
            if (card.Id == "H014-005")
            {
                if (!HasPiece("Bone Servant", hero.Id, hero.LocationId)) { reason = "Servant Assault requires a Bone Servant in Mordren's Location."; return false; }
                var target = Enemies.Values.FirstOrDefault(e => !e.Defeated && e.LocationId == hero.LocationId); if (target == null) { reason = "Servant Assault requires an Enemy in that Location."; return false; } enemyId = target.Id; return RequireHeroTurn(hero, $"play {card.Name}");
            }
            if (card.Id == "H014-006")
            {
                bool any = HeroCreatedPieces.Where(p => p.Type == "Bone Servant" && p.OwnerHeroId == hero.Id).Any(p => Enemies.Values.Any(e => !e.Defeated && e.LocationId == p.LocationId));
                if (!any) { reason = "Bone Legion requires a Bone Servant sharing a Location with an Enemy."; return false; } return RequireHeroTurn(hero, $"play {card.Name}");
            }
            if (card.Id == "H014-014" && !HeroCreatedPieces.Any(p => p.Type == "Bone Servant" && p.OwnerHeroId == hero.Id && p.LocationId != hero.LocationId && GraphDistance(p.LocationId, hero.LocationId) <= 2)) { reason = "Grave Recall requires a Bone Servant within 2 connections outside Mordren's Location."; return false; }
            if (card.Id == "H016-014" && !HeroCreatedPieces.Any(p => p.Type == "Spirit Totem" && p.OwnerHeroId == hero.Id && p.LocationId != hero.LocationId && GraphDistance(p.LocationId, hero.LocationId) <= 2)) { reason = "Totem Recall requires one of Asha's Spirit Totems within 2 connections outside her Location."; return false; }
            if (card.Id == "H005-013" && !CanResolveGrantedExplore0851(hero)) { reason = "Scout Ahead: no connected unrevealed Location is available."; return false; }

            if (card.Kind == CardKind.Attack || card.Id == "H012-017" || card.Id == "H015-015" || card.Id == "H015-019" || card.Id == "H011-016" || card.Id == "H013-018")
            {
                EnemyState target = null;
                if (!string.IsNullOrEmpty(enemyId) && Enemies.TryGetValue(enemyId, out var selected) && CanTargetEnemyWithCard(hero, card, selected)) target = selected;
                target ??= FirstLegalEnemyForCard(hero, card);
                if (card.Id == "H001-006" || card.Id == "H012-007")
                {
                    if (target == null || GraphDistance(hero.LocationId, target.LocationId) != 1 || !Locations[target.LocationId].Revealed)
                        target = Enemies.Values.FirstOrDefault(e => !e.Defeated && GraphDistance(hero.LocationId, e.LocationId) == 1 && Locations[e.LocationId].Revealed);
                    if (target == null)
                    { reason = $"{card.Name}: choose an Enemy in a connected revealed Location so the printed Move can resolve first."; return false; }
                }
                if (target == null) { reason = $"{card.Name}: no legal Enemy target within its printed range."; return false; }
                if (card.Id == "H012-017" || card.Id == "H015-015" || card.Id == "H015-019")
                    if (target.LocationId != hero.LocationId) { reason = $"{card.Name}: choose an Enemy in {hero.DisplayName}'s Location."; return false; }
                if (card.Id == "H013-018" && target.Statuses.All(s => s.Beneficial)) { reason = "Harvest Despair requires an afflicted Enemy."; return false; }
                if (card.Id == "H005-019" && !target.Statuses.Any(s => string.Equals(s.Name, "Marked", StringComparison.OrdinalIgnoreCase))) { reason = "Master's Hunt requires a Marked Enemy."; return false; }
                enemyId = target.Id;
            }
            if(!ValidateHeroCardSpecific0860(hero,card,string.IsNullOrEmpty(enemyId)?null:(Enemies.TryGetValue(enemyId,out var specificTarget0860)?specificTarget0860:null),out var heroCardReason0860)){reason=heroCardReason0860;return false;}
            return RequireHeroTurn(hero, $"play {card.Name}");
        }

        public void PlayCard(CardData card,string enemyId=null)
        {
            var hero=SelectedHero; if(hero==null||card==null)return;
            if (hero.PendingDiscardCount>0) { DiscardFromHand(hero,card,"required discard"); hero.PendingDiscardCount--; StateChanged?.Invoke(); return; }
            if(string.IsNullOrEmpty(enemyId))enemyId=SelectedEnemyId;
            if(!ValidateCardPlay(hero,card,ref enemyId,out var reason)){if(!string.IsNullOrEmpty(reason))AddLog(reason);return;}
            int cost=GetEffectiveCardCost(hero,card); bool consumesFreeAttack = card.Kind==CardKind.Attack && hero.FreeAttackCardReady && cost==0;
            SpendCardCost(hero,card,cost); OnEquipmentCardPlayed0854(hero,card,cost); if(consumesFreeAttack) hero.FreeAttackCardReady=false; SelectedEnemyId=enemyId;
            if (IsQuestLimitedCard(card) && !hero.UsedQuestCards.Contains(card.Id)) hero.UsedQuestCards.Add(card.Id);
            ConsumeHeroCardCostState0860(hero,card);
            RegisterHeroCardPlayed0860(hero,card);
            HeroRulesRuntime.OnCardPlayed(this,hero,card);
            bool deferred=ResolveCardEffect(hero,card,enemyId);
            if(!deferred) FinishPlayedCard(hero,card);
            StateChanged?.Invoke();
        }

        private bool ResolveCardEffect(HeroState hero,CardData card,string enemyId)
        {
            EnemyState selectedEnemy = !string.IsNullOrEmpty(enemyId) && Enemies.TryGetValue(enemyId, out var selected) ? selected : null;
            if(TryResolveHeroCardSpecial0860(hero,card,selectedEnemy,out var specialDeferred0860))return specialDeferred0860;
            switch(card.Id)
            {
                case "H001-006":
                    FinishPlayedCard(hero,card); BeginCardMoveAttack(hero,selectedEnemy,0,0,card,false); return true;
                case "H012-007":
                    FinishPlayedCard(hero,card); BeginCardMoveAttack(hero,selectedEnemy,2,0,card,true); return true;
                case "H012-011":
                    hero.BattleRoar=true; hero.TurnAttackBonus+=1; AddLog("Battle Roar — Garruk's Attacks gain +1 Attack until the end of his turn."); return false;
                case "H012-020":
                    hero.AvatarRageTurns=2; hero.AvatarFloorActive=true; hero.CannotDropBelowOne=true; hero.CannotDropBelowOneTurns=2; AddLog("Avatar of Rage — +3 Attack through the end of Garruk's next turn; Garruk cannot be reduced below 1 Health."); return false;
                case "H012-008":
                {
                    var targets=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).Select(e=>e.Id).ToList();
                    FinishPlayedCard(hero,card); BeginGroundbreaker(hero,targets,card); return true;
                }
                case "H012-015":
                    hero.FreeMoveReady=true; hero.FreeMoveAttackBonus=1; hero.FreeMoveAttackBonusRequiresEnemy=true; AddLog("Warpath — click a connected revealed Location to resolve the card's Move. If an Enemy is there, Garruk's next Attack gains +1 Attack."); return false;
                case "H012-017":
                    AddStatus(selectedEnemy,"Marked",0,1,card.Name,false,hero,card.Rules); hero.TargetLockedEnemyId=selectedEnemy.Id; hero.TargetLockedAttackBonus+=2; AddLog($"Challenge the Strongest — {selectedEnemy.DisplayName} is the locked target; Garruk's next Attack against it gains +2 Attack."); return false;
                case "H007-012":
                {
                    var cleanseTarget=ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==hero.LocationId).OrderBy(h=>(float)h.Health/Mathf.Max(1,h.MaxHealth)).FirstOrDefault()??hero;
                    HealHero(cleanseTarget,1,card.Name,hero);
                    if(cleanseTarget.HasStatus("Bleeding"))RemoveStatus(cleanseTarget.Statuses,"Bleeding",cleanseTarget.DisplayName,card.Name);
                    else if(cleanseTarget.HasStatus("Burning"))RemoveStatus(cleanseTarget.Statuses,"Burning",cleanseTarget.DisplayName,card.Name);
                    AddLog($"Nature's Blessing — {cleanseTarget.DisplayName} recovers Health and removes Bleeding or Burning if present.");
                    return false;
                }
                case "H009-010":
                    AddStatus(hero,"Guarded",1,0,card.Name,true,hero,card.Rules); hero.NextAttackRangeConnections=Mathf.Max(hero.NextAttackRangeConnections,1); AddLog("Crane Stance — Kael's next Attack this turn may target an Enemy in a connected Location."); return false;
                case "H009-020":
                    hero.AttackCardCostReduction=Mathf.Max(hero.AttackCardCostReduction,1); hero.AttackCardAttackBonus=Mathf.Max(hero.AttackCardAttackBonus,1); hero.DrawAfterAttackCard=true; AddLog("Perfect Form — Attack cards cost 1 fewer Action (minimum 1), gain +1 Attack, and draw 1 after resolving until end of turn."); return false;
                case "H005-013":
                {
                    ResolveGrantedExplore0851(hero,"Scout Ahead");
                    return false;
                }
                case "H004-014":
                {
                    bool alreadyArcane=IsArcaneLocation(hero.LocationId);
                    if(alreadyArcane) foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==hero.LocationId)) DrawCards(h,1);
                    else AddLocationTag(hero.LocationId,"Arcane",card.Name,hero,StatusExpiryTiming.StartOfSourceTurn,1);
                    AddLog(alreadyArcane?"Leyline Convergence — the Location was already Arcane; Heroes there draw 1 card.":"Leyline Convergence — this Location is Arcane until the start of Vaelis's next turn.");
                    return false;
                }
                case "H011-012":
                {
                    FinishPlayedCard(hero,card);
                    BeginHeroDamage(hero,1,card.Name,HeroDamageSourceKind.HeroCard,null,_=>
                    {
                        if(!hero.IsDefeated&&hero.Actions<3)TryGrantAdditionalHeroAction(hero,1,"Mana Burn",true);
                        StateChanged?.Invoke();
                    });
                    return true;
                }
                case "H012-002":
                {
                    FinishPlayedCard(hero,card);
                    BeginHeroDamage(hero,1,card.Name,HeroDamageSourceKind.HeroCard,null,_=>
                    {
                        if(hero.IsDefeated){StateChanged?.Invoke();return;}
                        BeginHeroAttack(hero,selectedEnemy,4,0,card.Name,card);
                    });
                    return true;
                }
                case "H012-012":
                {
                    FinishPlayedCard(hero,card);
                    BeginHeroDamage(hero,1,card.Name,HeroDamageSourceKind.HeroCard,null,_=>
                    {
                        DrawCards(hero,2); hero.PendingDiscardCount+=1; AddLog($"CARD — {hero.DisplayName} resolves {card.Name}."); StateChanged?.Invoke();
                    });
                    return true;
                }
                case "H013-012":
                {
                    FinishPlayedCard(hero,card);
                    BeginHeroDamage(hero,1,card.Name,HeroDamageSourceKind.HeroCard,null,_=>
                    {
                        DrawCards(hero,3); hero.PendingDiscardCount+=1; AddLog($"CARD — {hero.DisplayName} resolves {card.Name}."); StateChanged?.Invoke();
                    });
                    return true;
                }
                case "H011-015":
                    hero.NextAttackBonus+=3; hero.SelfDamageAfterNextAttack=Mathf.Max(hero.SelfDamageAfterNextAttack,1); AddLog("Volatile Magic — next Attack gains +3 Attack; Malachar suffers 1 damage after it resolves."); return false;
                case "H011-016":
                    hero.TargetLockedEnemyId=selectedEnemy.Id; hero.TargetLockedAttackBonus+=3; AddLog($"Conduit — Malachar's next Attack against {selectedEnemy.DisplayName} gains +3 Attack."); return false;
                case "H013-018":
                    HealHero(hero,1,card.Name,hero); hero.TargetLockedEnemyId=selectedEnemy.Id; hero.TargetLockedAttackBonus+=1; AddLog($"Harvest Despair — Veyra's next Attack against {selectedEnemy.DisplayName} gains +1 Attack."); return false;
                case "H015-015":
                    AddStatus(selectedEnemy,"Marked",0,1,card.Name,false,hero,card.Rules); hero.TargetLockedEnemyId=selectedEnemy.Id; hero.TargetLockedAttackBonus+=1; AddLog($"Taunting Salute — {selectedEnemy.DisplayName} is the locked target; Lucien's next Attack against it gains +1 Attack."); return false;
                case "H015-019":
                    hero.FocusEnemyId=selectedEnemy.Id; hero.FocusAttackBonus=2; AddStatus(hero,"Master Duelist Footwork",1,0,card.Name,true,hero,"until end of turn"); AddLog($"Master Duelist — Lucien's Attacks against {selectedEnemy.DisplayName} gain +2 Attack until end of turn; footwork resolves through the 0.8.60 Hero runtime."); return false;
                case "H014-005":
                {
                    var e=Enemies.Values.First(x=>!x.Defeated&&x.LocationId==hero.LocationId); DealDirectDamage(e,2,card.Name,hero,card); return false;
                }
                case "H014-006":
                {
                    foreach(var piece in HeroCreatedPieces.Where(p=>p.Type=="Bone Servant"&&p.OwnerHeroId==hero.Id).ToList())
                    { var e=Enemies.Values.FirstOrDefault(x=>!x.Defeated&&x.LocationId==piece.LocationId); if(e!=null)DealDirectDamage(e,2,card.Name,hero,card); }
                    return false;
                }
                case "H014-009":
                    PlacePiece("Bone Servant",hero.Id,hero.LocationId,1,MaxBoneServants(hero)); return false;
                case "H014-014":
                    MoveNearestPiece("Bone Servant",hero.Id,hero.LocationId,2,card.Name); return false;
                case "H014-019":
                {
                    var locations=new List<string>{hero.LocationId}; locations.AddRange(Locations[hero.LocationId].Connections.Where(id=>Locations[id].Revealed));
                    foreach(var loc in locations){if(HeroCreatedPieces.Count(p=>p.Type=="Bone Servant"&&p.OwnerHeroId==hero.Id)>=MaxBoneServants(hero))break;PlacePiece("Bone Servant",hero.Id,loc,1,MaxBoneServants(hero));} return false;
                }
                case "H016-009":
                    PlacePiece("Spirit Totem",hero.Id,hero.LocationId,1,MaxSpiritTotems(hero)); return false;
                case "H016-014":
                    MoveNearestPiece("Spirit Totem",hero.Id,hero.LocationId,2,card.Name); return false;
                case "H016-019":
                    PlacePiece("Spirit Totem",hero.Id,hero.LocationId,1,MaxSpiritTotems(hero)+1); foreach(var h in ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==hero.LocationId))AddStatus(h,"Inspired",1,0,card.Name,true,hero,card.Rules); return false;
            }

            string rules=card.Rules??string.Empty;
            if(card.Kind==CardKind.Attack)
            {
                int bonus=GenericAttackBaseBonus0860(card,ExtractNumber(rules,@"\+(\d+)\s+Attack"));
                if(card.Kind==CardKind.Attack) bonus += hero.AttackCardAttackBonus;
                bool malacharOverload = hero.Id=="Malachar" && card.Cost>=2 && !hero.UsedRoundAbilities.Contains("ArcaneOverload");
                if (malacharOverload) { hero.UsedRoundAbilities.Add("ArcaneOverload"); bonus += 2; AddLog("HERO ABILITY — Arcane Overload: +2 Attack; self-damage resolves after the Attack."); }
                int ignore=rules.IndexOf("ignores all Armor",StringComparison.OrdinalIgnoreCase)>=0?999:ExtractNumber(rules,@"ignore(?:s)?\s+(\d+)\s+Armor");
                if(card.Id=="H006-005"&&selectedEnemy!=null&&!EnemyTypeHas0859(selectedEnemy,"Undead","Demon","Corrupted"))ignore=0;
                if(card.Id=="H009-008"&&CardsPlayedThisTurn0860(hero,true)<3)ignore=0;
                if(card.Id=="H005-019"&&selectedEnemy!=null&&!selectedEnemy.HasStatus("Marked"))ignore=0;
                if(rules.IndexOf("Spirit Totem",StringComparison.OrdinalIgnoreCase)>=0 && HasPiece("Spirit Totem",hero.Id,hero.LocationId)) bonus+=2;
                if(card.Id=="H016-007" && selectedEnemy!=null && ParticipatingHeroes.Any(h=>!h.Dead&&h.LocationId==selectedEnemy.LocationId&&h.Statuses.Any(s=>s.Beneficial))) bonus+=1;

                List<string> ids=null;
                if(card.Id=="H004-021")
                {
                    var locationIds=Enemies.Values.Where(e=>!e.Defeated).Select(e=>e.LocationId).Distinct().OrderBy(id=>id==selectedEnemy?.LocationId?0:1).ThenBy(id=>GraphDistance(hero.LocationId,id)).Take(3).ToHashSet();
                    foreach(var loc in locationIds) AddLocationTag(loc,"Arcane",card.Name,hero,StatusExpiryTiming.Persistent,0);
                    ids=Enemies.Values.Where(e=>!e.Defeated&&locationIds.Contains(e.LocationId)).Select(e=>e.Id).ToList();
                }
                else if(rules.IndexOf("each Enemy within 2 connections",StringComparison.OrdinalIgnoreCase)>=0) ids=Enemies.Values.Where(e=>!e.Defeated&&GraphDistance(hero.LocationId,e.LocationId)<=2).Select(e=>e.Id).ToList();
                else if(rules.IndexOf("up to 3 different Enemies there",StringComparison.OrdinalIgnoreCase)>=0) ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==selectedEnemy.LocationId).Take(3).Select(e=>e.Id).ToList();
                else if(rules.IndexOf("up to two Enemies in the same Location",StringComparison.OrdinalIgnoreCase)>=0) ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==selectedEnemy.LocationId).Take(2).Select(e=>e.Id).ToList();
                else if(rules.IndexOf("each Enemy there",StringComparison.OrdinalIgnoreCase)>=0||rules.IndexOf("each Enemy in the chosen Location",StringComparison.OrdinalIgnoreCase)>=0) ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==selectedEnemy.LocationId).Select(e=>e.Id).ToList();
                else if(rules.IndexOf("each Enemy in your Location",StringComparison.OrdinalIgnoreCase)>=0) ids=Enemies.Values.Where(e=>!e.Defeated&&e.LocationId==hero.LocationId).Select(e=>e.Id).ToList();

                FinishPlayedCard(hero,card);
                if(ids!=null){BeginMultiAttack(hero,ids,bonus,ignore,card);return true;}
                BeginHeroAttack(hero,selectedEnemy,bonus,ignore,card.Name,card); return true;
            }

            int nextBonus=ExtractNumber(rules,@"next Attack[^.]*?\+(\d+)\s+Attack"); if(nextBonus>0) hero.NextAttackBonus+=nextBonus;
            int heal=ExtractNumber(rules,@"recover(?:s)?\s+(\d+)\s+Health");
            if(heal>0)
            {
                HeroState target=hero;
                if(rules.IndexOf("choose one Hero",StringComparison.OrdinalIgnoreCase)>=0||rules.IndexOf("another Hero",StringComparison.OrdinalIgnoreCase)>=0)
                    target=ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==hero.LocationId).OrderBy(h=>(float)h.Health/Mathf.Max(1,h.MaxHealth)).FirstOrDefault()??hero;
                HealHero(target,heal,card.Name,hero);
            }
            int draw=ExtractNumber(rules,@"draw\s+(\d+)\s+card"); if(draw>0) DrawCards(hero,draw);
            int discard=ExtractNumber(rules,@"discard\s+(\d+)\s+card"); if(discard>0) hero.PendingDiscardCount+=discard;
            if(rules.IndexOf("Move to a connected",StringComparison.OrdinalIgnoreCase)>=0)
            {
                hero.FreeMoveReady=true;
                if(rules.IndexOf("ignore Difficult Terrain",StringComparison.OrdinalIgnoreCase)>=0||rules.IndexOf("ignoring Difficult Terrain",StringComparison.OrdinalIgnoreCase)>=0)hero.FreeMoveIgnoresDifficultTerrain=true;
                AddLog($"{card.Name} — free connected Move ready; click a legal Location marker.");
            }
            ApplyStatusesFromCard(hero,card,enemyId);
            ApplyPieceCardEffects(hero,card,enemyId);
            AddLog($"CARD — {hero.DisplayName} plays {card.Name}.");
            return false;
        }

        private void ApplyPieceCardEffects(HeroState hero,CardData card,string enemyId)
        {
            string r=card.Rules??"";
            if(r.IndexOf("Place a Bone Servant",StringComparison.OrdinalIgnoreCase)>=0) PlacePiece("Bone Servant",hero.Id,hero.LocationId,1,2);
            if(r.IndexOf("Place a Spirit Totem",StringComparison.OrdinalIgnoreCase)>=0) PlacePiece("Spirit Totem",hero.Id,hero.LocationId,1,1);
            if(r.IndexOf("Remove one Bone Servant",StringComparison.OrdinalIgnoreCase)>=0) RemovePiece("Bone Servant",hero.Id,hero.LocationId);
            if(r.IndexOf("Bone Servant",StringComparison.OrdinalIgnoreCase)>=0 && r.IndexOf("deals 2 damage",StringComparison.OrdinalIgnoreCase)>=0 && Enemies.TryGetValue(enemyId??"",out var e)&&!e.Defeated) DealDirectDamage(e,2,card.Name,hero,card);
        }

        private void ApplyStatusesFromCard(HeroState hero,CardData card,string enemyId)
        {
            string text=card.Rules??"";string[] beneficial={"Inspired","Guarded","Stealth","Hidden","Regenerating"};string[] harmful={"Burning","Poisoned","Bleeding","Stunned","Rooted","Knocked Down","Shocked","Taunted","Marked","Weakened","Exposed"};
            foreach(string name in beneficial)
            {
                if(!TextAppliesStatus(text,name))continue;
                var targets=new List<HeroState>();
                if(text.IndexOf("Choose up to 2",StringComparison.OrdinalIgnoreCase)>=0)targets.AddRange(ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==hero.LocationId).Take(2));
                else if(text.IndexOf("Choose one Hero",StringComparison.OrdinalIgnoreCase)>=0||text.IndexOf("another Hero",StringComparison.OrdinalIgnoreCase)>=0)targets.Add(ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==hero.LocationId).OrderBy(h=>h.Id==hero.Id?1:0).ThenBy(h=>(float)h.Health/Mathf.Max(1,h.MaxHealth)).FirstOrDefault()??hero);
                else if(text.IndexOf("Heroes in your Location",StringComparison.OrdinalIgnoreCase)>=0||text.IndexOf("Heroes in his Location",StringComparison.OrdinalIgnoreCase)>=0)targets.AddRange(ParticipatingHeroes.Where(h=>!h.Dead&&h.LocationId==hero.LocationId));
                else targets.Add(hero);
                foreach(var target in targets.Distinct())AddStatus(target,name,1,0,card.Name,true,hero,text);
            }
            foreach(string name in harmful)
            {
                if(!TextAppliesStatus(text,name))continue;if(!string.IsNullOrEmpty(enemyId)&&Enemies.TryGetValue(enemyId,out var enemy)&&!enemy.Defeated)AddStatus(enemy,name,1,0,card.Name,false,hero,text);
            }
            if(TextAppliesStatus(text,"Consecrated")||text.IndexOf("becomes Consecrated",StringComparison.OrdinalIgnoreCase)>=0)
                AddLocationTag(hero.LocationId,"Consecrated",card.Name,hero,StatusExpiryTiming.StartOfSourceTurn,1);
        }

        private static int ExtractNumber(string text,string pattern)
        {
            if(string.IsNullOrEmpty(text))return 0;var m=Regex.Match(text,pattern,RegexOptions.IgnoreCase);return m.Success&&int.TryParse(m.Groups[1].Value,out var n)?n:0;
        }

        private void FinishPlayedCard(HeroState hero,CardData card)
        {
            if(hero.Hand.Remove(card)) hero.Discard.Add(card.Id);
        }

        public void DiscardFromHand(HeroState hero,CardData card,string reason)
        {
            if(hero==null||card==null)return; if(hero.Hand.Remove(card)){hero.Discard.Add(card.Id);AddLog($"DISCARD — {hero.DisplayName} discards {card.Name} ({reason}).");}
        }

        public void DrawCards(HeroState hero,int count,bool logDraw=true)
        {
            if(hero==null||count<=0)return;int drawn=0;
            for(int i=0;i<count;i++)
            {
                if(hero.Deck.Count==0 && hero.Discard.Count>0){hero.Deck.AddRange(hero.Discard);hero.Discard.Clear();Shuffle(hero.Deck);if(logDraw)AddLog($"DECK — {hero.DisplayName} reshuffles the Discard Pile.");}
                if(hero.Deck.Count==0)break;string id=hero.Deck[0];hero.Deck.RemoveAt(0);var card=HeroDatabase.GetCard(id);if(card!=null){hero.Hand.Add(card);drawn++;}
            }
            if(logDraw&&drawn>0)AddLog($"DRAW — {hero.DisplayName} draws {drawn} card{(drawn==1?"":"s")}.");
        }

        private static void Shuffle(List<string> list)
        {
            for(int i=list.Count-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);(list[i],list[j])=(list[j],list[i]);}
        }

        private void ResolveAttack(string enemyId,int cardBonus,int armorIgnore,int actionCost,string source,CardData card)
        {
            var hero=SelectedHero;if(hero==null||hero.IsDefeated||hero.Moving||IsResolving||hero.TurnCompleted||!CanUseStandardAction(hero,actionCost))return;
            if(!Enemies.TryGetValue(enemyId,out var enemy)||enemy.Defeated)return;
            int baseRange = Mathf.Max(hero.NextAttackRangeConnections,EquipmentAttackRange0854(hero));
            int allowed = baseRange + LocationRangeBonus0858(hero,baseRange);
            if(GraphDistance(hero.LocationId,enemy.LocationId)>allowed){AddLog(allowed>0?$"{source}: target must be within {allowed} connection(s).":$"{source}: target must be in {Locations[hero.LocationId].Name}.");return;}
            if(!RequireHeroTurn(hero,"Attack"))return;
            hero.Actions-=actionCost;SelectedEnemyId=enemyId;BeginHeroAttack(hero,enemy,cardBonus,armorIgnore,source,card);StateChanged?.Invoke();
        }

        private void BeginHeroAttack(HeroState hero,EnemyState enemy,int cardBonus,int armorIgnore,string source,CardData card,Action afterFinished=null)
        {
            IsResolving=true;bool impact=false,finished=false;
            void Impact(){if(impact)return;impact=true;ApplyHeroAttackDamage(hero,enemy,cardBonus,armorIgnore,source,card);StateChanged?.Invoke();}
            void Finished(){if(finished)return;finished=true;if(!impact)Impact();AfterAttackResolution(hero,card,()=>{IsResolving=false;StateChanged?.Invoke();afterFinished?.Invoke();});}
            if(HeroAvatars.TryGetValue(hero.Id,out var ha)&&EnemyAvatars.TryGetValue(enemy.Id,out var ea))ha.PlayAttack(ea.transform.position,Impact,AttackStyleFor(hero,source,card),Finished);else{Impact();Finished();}
        }

        private void BeginCardMoveAttack(HeroState hero, EnemyState enemy, int bonus, int ignore, CardData card, bool rampageFollowUp)
        {
            if (hero == null || enemy == null || card == null) return;
            string destination = enemy.LocationId;
            string from = hero.LocationId;
            hero.Moving = true;
            IsResolving = true;
            void Arrive()
            {
                hero.LocationId = destination;
                hero.PendingLocationId = null;
                hero.Moving = false;
                hero.MovedThisTurn = true;
                ResolveLocationFirstVisit(Locations[destination],hero);
                ResolveEncounterObjectsOnHeroEnter(Locations[destination],hero);
                HeroRulesRuntime.OnMoveComplete(this, hero);
                HeroRulesRuntime.OnHeroEnteredLocation(this, hero);
                AddLog($"CARD MOVE — {hero.DisplayName}: {Locations[from].Name} → {Locations[destination].Name} ({card.Name}).");
                IsResolving = false;
                void FollowUp()
                {
                    if (!rampageFollowUp || !enemy.Defeated) return;
                    var next = Enemies.Values.FirstOrDefault(e => !e.Defeated && e.Id != enemy.Id && e.LocationId == hero.LocationId);
                    if (next == null) return;
                    AddLog($"RAMPAGE — {hero.DisplayName} immediately makes the granted Basic Attack against {next.DisplayName}.");
                    BeginHeroAttack(hero, next, 0, 0, "Rampage follow-up", null);
                }
                BeginHeroAttack(hero, enemy, bonus, ignore, card.Name, card, FollowUp);
            }
            if (HeroAvatars.TryGetValue(hero.Id, out var avatar)) avatar.MoveTo(Locations[destination].Position, Arrive);
            else Arrive();
            StateChanged?.Invoke();
        }

        private void BeginMultiAttack(HeroState hero,List<string> targetIds,int bonus,int ignore,CardData card)
        {
            IsResolving=true;bool impact=false,finished=false;
            void Impact(){if(impact)return;impact=true;foreach(var id in targetIds)if(Enemies.TryGetValue(id,out var e)&&!e.Defeated)ApplyHeroAttackDamage(hero,e,bonus,ignore,card.Name,card);StateChanged?.Invoke();}
            void Finished(){if(finished)return;finished=true;if(!impact)Impact();AfterAttackResolution(hero,card,()=>{IsResolving=false;StateChanged?.Invoke();});}
            var first=targetIds.Select(id=>Enemies.TryGetValue(id,out var e)?e:null).FirstOrDefault(e=>e!=null&&!e.Defeated);
            if(first!=null&&HeroAvatars.TryGetValue(hero.Id,out var ha)&&EnemyAvatars.TryGetValue(first.Id,out var ea))ha.PlayAttack(ea.transform.position,Impact,"Heavy",Finished);else{Impact();Finished();}
        }
        private void BeginGroundbreaker(HeroState hero,List<string> ids,CardData card)=>BeginMultiAttack(hero,ids,1,0,card);

        private void AfterAttackResolution(HeroState hero, CardData card, Action complete)
        {
            if (hero == null) { complete?.Invoke(); return; }
            hero.NextAttackRangeConnections = 0;
            if (card != null && card.Kind == CardKind.Attack && hero.DrawAfterAttackCard) DrawCards(hero, 1);

            int selfDamage = 0;
            var damageSources = new List<string>();
            if(hero.HasStatus("Bleeding")) { selfDamage += 1; damageSources.Add("Bleeding"); }
            if (hero.SelfDamageAfterNextAttack > 0)
            {
                selfDamage += hero.SelfDamageAfterNextAttack;
                hero.SelfDamageAfterNextAttack = 0;
                damageSources.Add("Hero card effect");
            }
            if (card != null && card.Id == "H011-017") { selfDamage += 2; damageSources.Add(card.Name); }
            if (card != null && card.Id == "H012-019") { selfDamage += 1; damageSources.Add(card.Name); }

            if (hero.Id == "Malachar" && card != null && card.Cost >= 2 && hero.UsedRoundAbilities.Contains("ArcaneOverload") && !hero.UsedTurnAbilities.Contains("ArcaneOverloadDamage"))
            {
                hero.UsedTurnAbilities.Add("ArcaneOverloadDamage");
                int overloadDamage = 1;
                if (!hero.UsedRoundAbilities.Contains("ControlledBurn"))
                {
                    hero.UsedRoundAbilities.Add("ControlledBurn");
                    overloadDamage = 0;
                    AddLog("HERO ABILITY — Controlled Burn reduces Arcane Overload self-damage by 1.");
                }
                if (overloadDamage > 0) { selfDamage += overloadDamage; damageSources.Add("Arcane Overload"); }
            }

            if (selfDamage <= 0) { complete?.Invoke(); return; }
            string source = damageSources.Count == 1 ? damageSources[0] : string.Join(" + ", damageSources.Distinct());
            var damageKind = damageSources.Count == 1 && string.Equals(damageSources[0],"Bleeding",StringComparison.OrdinalIgnoreCase)
                ? HeroDamageSourceKind.Condition : HeroDamageSourceKind.HeroCard;
            BeginHeroDamage(hero, selfDamage, source, damageKind, null, _ => complete?.Invoke());
        }

        private static bool TextAppliesStatus(string text,string name)
        {
            if(string.IsNullOrEmpty(text)||string.IsNullOrEmpty(name))return false;return Regex.IsMatch(text,$@"\b(?:gain|gains|become|becomes|apply|applies|inflict|inflicts)\s+{Regex.Escape(name)}\b",RegexOptions.IgnoreCase);
        }

        private void ApplyAttackStatusesFromCard(HeroState hero,CardData card,EnemyState enemy,int damage)
        {
            if(hero==null||card==null||enemy==null||damage<=0)return;
            string text=card.Rules??"";
            string[] harmful={"Burning","Poisoned","Bleeding","Stunned","Rooted","Knocked Down","Shocked","Taunted","Marked","Weakened","Exposed"};
            foreach(string name in harmful)
            {
                if(!TextAppliesStatus(text,name))continue;
                // Meteor Shard applies Burning only while Vaelis is actually in an Arcane Location.
                if(card.Id=="H004-006"&&string.Equals(name,"Burning",StringComparison.OrdinalIgnoreCase)&&!IsArcaneLocation(hero.LocationId))continue;
                AddStatus(enemy,name,1,0,card.Name,false,hero,text);
            }
        }

        private int MarkedAttackBonus(HeroState hero, EnemyState enemy)
        {
            if(hero==null||enemy==null)return 0;
            foreach(var status in enemy.Statuses.Where(s=>string.Equals(s.Name,"Marked",StringComparison.OrdinalIgnoreCase)))
            {
                // Predator's Mark explicitly benefits all attacks. Other Marked instances use the
                // authored status rule: the Hero who applied Marked gains +1 Attack against it.
                if(string.Equals(status.Source,"Predator's Mark",StringComparison.OrdinalIgnoreCase))return 1;
                if(!string.IsNullOrEmpty(status.SourceHeroId)&&string.Equals(status.SourceHeroId,hero.Id,StringComparison.OrdinalIgnoreCase))return 1;
            }
            return 0;
        }

        private int ConsumeTargetStatusAttackBonus(HeroState hero, EnemyState enemy, bool writeLog)
        {
            if(hero==null||enemy==null)return 0;
            int bonus=MarkedAttackBonus(hero,enemy);
            if(bonus>0&&writeLog)AddLog($"STATUS — Marked grants {hero.DisplayName} +{bonus} Attack against {enemy.DisplayName}.");
            if(enemy.Statuses.Any(s=>string.Equals(s.Name,"Exposed",StringComparison.OrdinalIgnoreCase)))
            {
                bonus+=1;
                enemy.Statuses.RemoveAll(s=>string.Equals(s.Name,"Exposed",StringComparison.OrdinalIgnoreCase));
                if(writeLog)AddLog($"STATUS — Exposed grants +1 Attack against {enemy.DisplayName}, then Exposed is removed.");
            }
            return bonus;
        }

        private void ApplyHeroAttackDamage(HeroState hero,EnemyState enemy,int cardBonus,int armorIgnore,string source,CardData card)
        {
            BreakHiddenOnAttack(hero);
            int bonus=cardBonus+hero.TurnAttackBonus+HeroRulesRuntime.BeforeAttack(this,hero,enemy,card)+HeroCardPreAttackBonus0860(hero,enemy,card);
            int encounterAttack=ConsumeEncounterAttackModifier0853(hero,enemy);
            if(encounterAttack!=0)bonus+=encounterAttack;
            bool rangedAttack=IsRangedHeroAttack(hero,card,source);
            bonus+=EquipmentAttackModifier0854(hero,enemy,card,rangedAttack);
            armorIgnore=Mathf.Max(armorIgnore,EquipmentArmorIgnore0854(hero,enemy,card));
            int locationAttack=LocationAttackModifier(hero.LocationId,enemy.LocationId,rangedAttack)+ConsumeLocationAttackModifier0858(hero,enemy,rangedAttack);
            if(locationAttack!=0){bonus+=locationAttack;AddLog($"LOCATION EFFECT — ranged Attack modifier {locationAttack:+#;-#;0} from active Location rules.");}
            if(card!=null&&card.Id=="H004-001"&&IsArcaneLocation(hero.LocationId)){bonus+=1;AddLog("LOCATION EFFECT — Arcane Bolt gains +1 Attack because Vaelis is in an Arcane Location.");}
            if(hero.BattleRoar && hero.TurnAttackBonus==0)bonus+=1;
            if(hero.AvatarRageTurns>0)bonus+=3;
            if(card!=null&&card.Id=="H013-008"&&enemy.Statuses.Any(s=>!s.Beneficial))bonus+=1;
            int enemyArmor = EffectiveEnemyArmor(enemy);
            int effectiveArmor=armorIgnore>=999?0:Mathf.Max(0,enemyArmor-armorIgnore-hero.NextArmorIgnore);hero.NextArmorIgnore=0;
            int effectiveHeroAttack = EffectiveHeroAttack(hero);
            bonus += ConsumeTargetStatusAttackBonus(hero,enemy,true);
            int total=effectiveHeroAttack+bonus;int damage=Mathf.Max(0,total-effectiveArmor);
            damage+=HeroCardPostArmorDamage0860(enemy,card);
            damage+=EquipmentFlatDamage0854(hero,enemy,card,rangedAttack);
            damage+=LootPostArmorDamage0854(hero,enemy,damage);
            if(ShouldPreventQuestBossDamage0832(enemy)){damage=0;AddLog("Q012 BANNERS OF THE HORDE — the Goblin King cannot lose Health while 2 or more Banners remain.");}
            damage=ModifyEnemyIncomingDamage0859(enemy,damage,hero,card,rangedAttack,source);
            int enemyHealthBefore0859=enemy.Health;
            enemy.Health=ApplyEnemyHealthFloor0859(enemy,Mathf.Max(0,enemy.Health-damage));
            damage=Mathf.Max(0,enemyHealthBefore0859-enemy.Health);
            OnQuestEnemyDamaged0832(enemy);OnEnemyDamaged0859(enemy,damage,hero,card,rangedAttack);
            AddLog($"ATTACK — {hero.DisplayName} → {enemy.DisplayName}: Attack {effectiveHeroAttack}{(bonus!=0?$" + {bonus}":"")} = {total} · Armor {effectiveArmor} · Damage {damage}.");
            ApplyAttackStatusesFromCard(hero,card,enemy,damage);
            if(damage>0&&card!=null&&!string.IsNullOrEmpty(card.Rules)&&card.Rules.IndexOf("becomes Consecrated",StringComparison.OrdinalIgnoreCase)>=0)
                AddLocationTag(hero.LocationId,"Consecrated",card.Name,hero,StatusExpiryTiming.StartOfSourceTurn,1);
            bool defeated=enemy.Defeated;
            if(defeated)
            {
                AddLog($"{enemy.DisplayName} is Defeated by {hero.DisplayName} ({source}).");HeroRulesRuntime.OnEnemyDefeated(this,hero,enemy,card);
                foreach(var mordren in ParticipatingHeroes.Where(h=>h.Id=="Mordren"&&!h.IsDefeated&&h.LocationId==enemy.LocationId&&!h.UsedRoundAbilities.Contains("RaiseServant")))
                { mordren.UsedRoundAbilities.Add("RaiseServant");PlacePiece("Bone Servant",mordren.Id,enemy.LocationId,1,MaxBoneServants(mordren)); }
                if(card!=null&&card.Id=="H005-019"){hero.FreeAttackCardReady=true;AddLog("Master's Hunt — next Attack card may be played without paying its Action Cost.");}
                if(card!=null&&card.Id=="H011-009")
                { var splash=Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.Id!=enemy.Id&&e.LocationId==enemy.LocationId);if(splash!=null)DealDirectDamage(splash,2,"Combustion",hero,card); }
                OnEnemyDefeated0859(enemy,hero);if(EnemyAvatars.TryGetValue(enemy.Id,out var av))av.SetDefeated(damage);if(SelectedEnemyId==enemy.Id)SelectedEnemyId=FirstLivingEnemyAtSelectedHeroLocation()?.Id;CheckEncounterCompletionForEnemy(enemy);OnQuestEnemyDefeated0829(enemy,hero);CheckVictory();PrepareHeroKillReactionWindow(hero,enemy);
            }
            else if(EnemyAvatars.TryGetValue(enemy.Id,out var av))av.RefreshVisual(damage);
            OnHeroCardAttackResolved0860(hero,enemy,card,damage,defeated);
            OnEquipmentAttackResolved0854(hero,enemy,damage,defeated,card);
            HeroRulesRuntime.AfterAttack(this,hero,enemy,damage>0);
        }

        private static string AttackStyleFor(HeroState hero,string source,CardData card)
        {
            if(source.IndexOf("Ground",StringComparison.OrdinalIgnoreCase)>=0||source.IndexOf("Crushing",StringComparison.OrdinalIgnoreCase)>=0)return "Heavy";
            if(hero!=null&&hero.StartingEquipment.Any(eq=>RewardContent0826.Equipment.TryGetValue(eq.Id??string.Empty,out var def)&&(string.Equals(def.Family,"Bow",StringComparison.OrdinalIgnoreCase)||string.Equals(def.Family,"Crossbow",StringComparison.OrdinalIgnoreCase))))return "Ranged";
            if(hero.ClassName=="Ranger"||hero.ClassName=="Artificer")return "Ranged";
            if(hero.ClassName=="Cleric"||hero.ClassName=="Wizard"||hero.ClassName=="Druid"||hero.ClassName=="Warlock"||Contains(card?.Traits,"Spell"))return "Spell";
            if(hero.Id=="Aldric"&&source.IndexOf("Shield",StringComparison.OrdinalIgnoreCase)>=0)return "Shield";return "Melee";
        }
        private static bool Contains(string value,string token)=>!string.IsNullOrEmpty(value)&&value.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0;

        public void EndHeroTurn()
        {
            if(IsResolving||HasPendingReactionWindow||HasPendingEncounterDeckControl||HasPendingRewardChoice)return;if(ParticipatingHeroes.Any(h=>h.Moving)){AddLog("End Turn blocked until movement finishes.");return;}
            var hero=SelectedHero;
            if (TurnHero == null)
            {
                AddLog("END TURN blocked — no Hero turn is currently in progress. Choose a Hero and press Start Turn.");
                StateChanged?.Invoke();
                return;
            }
            else if (!IsHeroTakingTurn(hero))
            {
                AddLog($"TURN IN PROGRESS — {TurnHero.DisplayName} must finish their turn before another Hero can end a turn.");
                return;
            }
            if(hero==null||hero.TurnCompleted)return;
            ResolveLocationEndHeroActivation0858(hero);
            int saved=GeneralActions(hero);
            hero.Actions=Mathf.Max(0,hero.Actions-hero.AttackOnlyActions);hero.AttackOnlyActions=0;
            hero.TurnCompleted=true;
            hero.BattleRoar=false;hero.TurnAttackBonus=0;hero.TargetLockedEnemyId=null;hero.TargetLockedAttackBonus=0;hero.FocusEnemyId=null;hero.FocusAttackBonus=0;hero.AttackCardCostReduction=0;hero.AttackCardAttackBonus=0;hero.DrawAfterAttackCard=false;hero.FreeAttackCardReady=false;hero.NextAttackRangeConnections=0;hero.FreeMoveReady=false;hero.FreeMoveIgnoresEngagement=false;hero.FreeMoveIgnoresDifficultTerrain=false;hero.BonusAfterFreeMove=false;hero.FreeMoveAttackBonus=0;hero.FreeMoveAttackBonusRequiresEnemy=false;hero.SelfDamageAfterNextAttack=0;
            if(!string.IsNullOrEmpty(hero.PendingLocationOption)){AddLog($"LOCATION OPTION DECLINED — {hero.DisplayName} did not use {hero.PendingLocationOption} before ending the turn.");hero.PendingLocationOption=null;hero.PendingLocationOptionLocationId=null;}
            if(!string.IsNullOrEmpty(hero.FreeSearchLocationId)){AddLog($"REVEAL SEARCH EXPIRES — {hero.DisplayName} did not use the immediate free Search before ending the turn.");hero.FreeSearchLocationId=null;}
            if(hero.AvatarRageTurns>0){hero.AvatarRageTurns--;if(hero.AvatarRageTurns<=0){hero.AvatarFloorActive=false;hero.CannotDropBelowOne=false;}}
            AddLog($"TURN END — {hero.DisplayName} ends their turn with {saved} Action{(saved==1?"":"s")} saved for legal Reactions.");
            if(hero.HasStatus("Burning")&&!hero.Dead)ApplyHeroDamageNow(hero,1,"Burning");
            AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfTargetTurn, hero.DisplayName);
            AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfTargetActivation, hero.DisplayName);
            AdvanceSourceStatusClock(hero, StatusExpiryTiming.EndOfSourceTurn);
            bool orbExtra=hero.UsedQuestCards.Remove(Item0854Prefix+"LT150:ExtraTurnPending");
            bool deathsDance=hero.UsedQuestCards.Remove(Item0854Prefix+"EQ044:ExtraTurnPending");
            if(orbExtra||deathsDance)
            {
                hero.TurnCompleted=false;TurnHeroId=null;AddLog($"ITEM EXTRA TURN — {(orbExtra?"Orb of Ages":"Night's Whisper — Death's Dance")} grants {hero.DisplayName} an immediate extra Hero turn.");
                TryStartHeroTurn(hero);return;
            }
            TurnHeroId=null;
            var remaining=ParticipatingHeroes.Where(h=>!h.IsDefeated&&!h.TurnCompleted).OrderBy(h=>h.PartyIndex).ToList();
            if(remaining.Count==0){StartCoroutine(ResolveRound());return;}
            AddLog($"HERO PHASE — choose the next Hero to take a turn ({remaining.Count} remaining).");
            StateChanged?.Invoke();
        }

        private void ResetHeroAfterEndPhase(HeroState hero)
        {
            if(hero==null)return;
            int held=GeneralActions(hero);
            if(held>0)AddLog($"END PHASE — {hero.DisplayName} loses {held} unspent Reaction Action{(held==1?"":"s")}.");
            hero.BattleRoar=false;hero.TurnAttackBonus=0;hero.TargetLockedEnemyId=null;hero.TargetLockedAttackBonus=0;hero.FocusEnemyId=null;hero.FocusAttackBonus=0;hero.AttackCardCostReduction=0;hero.AttackCardAttackBonus=0;hero.DrawAfterAttackCard=false;hero.FreeAttackCardReady=false;hero.NextAttackRangeConnections=0;hero.FreeMoveReady=false;hero.FreeMoveIgnoresEngagement=false;hero.FreeMoveIgnoresDifficultTerrain=false;hero.BonusAfterFreeMove=false;hero.FreeMoveAttackBonus=0;hero.FreeMoveAttackBonusRequiresEnemy=false;hero.SelfDamageAfterNextAttack=0;
            hero.AttackOnlyActions=0;hero.Actions=0;hero.MovedThisTurn=false;hero.TurnCompleted=false;
            HeroRulesRuntime.ResetRound(hero);
        }

        private void ResolveEndPhaseReactionReserves()
        {
            foreach(var hero in ParticipatingHeroes)ResetHeroAfterEndPhase(hero);
            AddLog("END PHASE — all held Actions expire. Every Hero begins the next Hero Phase at 0 Actions until Start Turn.");
        }

        private void BeginNextHeroPhase()
        {
            Round++;IsResolving=false;TurnHeroId=null;
            foreach(var hero in ParticipatingHeroes)
            {
                // Repair impossible carry-over instead of allowing stale round state to leak forward.
                if(hero.Actions!=0||hero.AttackOnlyActions!=0||hero.TurnCompleted)
                {
                    AddLog($"ROUND STATE REPAIR — {hero.DisplayName} had stale turn resources at Round {Round} start; resetting to 0 Actions and unfinished.");
                    hero.Actions=0;hero.AttackOnlyActions=0;hero.TurnCompleted=false;
                }
            }
            var selected=SelectedHero;
            if(selected==null||!selected.Participating){var first=ParticipatingHeroes.FirstOrDefault(h=>!h.IsDefeated);if(first!=null)SelectedHeroId=first.Id;}
            SelectedEnemyId=FirstLivingEnemyAtSelectedHeroLocation()?.Id;
            AddLog($"HERO PHASE — Round {Round}. Players choose Hero activation order. All Heroes await Start Turn at 0 Actions.");
            CheckVictory();StateChanged?.Invoke();
        }

        private IEnumerator ResolveRound()
        {
            IsResolving=true;AddLog($"WORLD PHASE — Round {Round}.");PrepareQuestWorldPhase0832();PrepareEnemyPhase0859();yield return new WaitForSeconds(0.12f);
            foreach(var enemy in Enemies.Values.Where(e=>!e.Defeated).ToList())
            {
                if(enemy.ActivatedThisRound){AddLog($"WORLD PHASE — {enemy.DisplayName} already activated earlier this Round; skipping duplicate activation.");continue;}
                yield return ResolveSingleEnemyActivation(enemy);
                yield return WaitForEncounterDeckControlIfNeeded();
            }
            yield return ResolveQuestExtraWorldPhaseActivations0832();
            ResolveQuestEndEnemyPhase0829();
            ResolveEnemyEndPhaseHazards0859();
            ResolveEncounterEndRound0853();
            ResolveSearchEndRound0852A();
            yield return ResolveEndRoundStatuses();
            ResolveEndPhaseReactionReserves();
            foreach(var enemy in Enemies.Values)enemy.ActivatedThisRound=false;
            BeginNextHeroPhase();
        }

        private int CalculateEnemyAttackDamage(EnemyState enemy,HeroState target,int die,int armorBonus=0,int reduction=0)
        {
            if(enemy==null||target==null)return 0;
            int targetEnemyBonus=EnemyAttackModifier0859(enemy,target)-EnemyAttackModifier0859(enemy,null);
            int enemyAttack=Mathf.Max(0,EffectiveEnemyAttack(enemy)+targetEnemyBonus+EquipmentEnemyAttackModifier0854(target,enemy)+LocationEnemyAttackVsTargetModifier0858(enemy,target));
            int armorIgnore=EnemyAttackArmorIgnore0859(enemy,target);
            int damage=Mathf.Max(0,die+enemyAttack-(Mathf.Max(0,EffectiveHeroArmor(target)-armorIgnore)+Mathf.Max(0,armorBonus)));
            return Mathf.Max(0,damage-Mathf.Max(0,reduction));
        }

        private bool ReactionTriggerTargetRelationshipIsLegal(HeroState reactor,HeroState target,CardData card)
        {
            if(reactor==null||target==null||card==null)return false;
            string rules=card.Rules??string.Empty;
            string targetText=card.Target??string.Empty;
            // Printed "another Hero" / adjacent-Hero protection cannot retarget the reactor onto themself.
            if(rules.IndexOf("another Hero",StringComparison.OrdinalIgnoreCase)>=0&&reactor.Id==target.Id)return false;
            if(targetText.IndexOf("Adjacent Hero",StringComparison.OrdinalIgnoreCase)>=0&&reactor.Id==target.Id)return false;
            return true;
        }

        private bool ReactionCanReach(HeroState reactor,HeroState target,CardData card)
        {
            if(reactor==null||target==null||card==null)return false;string range=card.TargetRange??"";string targetText=card.Target??"";if(range.IndexOf("Self",StringComparison.OrdinalIgnoreCase)>=0||targetText.Equals("Self",StringComparison.OrdinalIgnoreCase))return reactor.Id==target.Id;if(range.IndexOf("Any Location",StringComparison.OrdinalIgnoreCase)>=0||range.IndexOf("Battlefield",StringComparison.OrdinalIgnoreCase)>=0)return true;int d=GraphDistance(reactor.LocationId,target.LocationId);string r=(range+" "+card.Rules);if(r.IndexOf("connected Location",StringComparison.OrdinalIgnoreCase)>=0||r.IndexOf("connected location",StringComparison.OrdinalIgnoreCase)>=0)return d<=1;return d==0;
        }

        private bool IsBeforeEnemyAttackReaction(HeroState reactor,HeroState target,EnemyState enemy,CardData card)
        {
            if(reactor==null||target==null||card==null||!ReactionCanReach(reactor,target,card)||!ReactionTriggerTargetRelationshipIsLegal(reactor,target,card))return false;
            string r=card.Rules??"";
            // Pre-Attack is only for target declaration / protection / retarget effects.
            // Damage-triggered and defeat-triggered Reactions wait until the d6 has been rolled
            // and post-Armor damage is known.
            if(r.IndexOf("Play after",StringComparison.OrdinalIgnoreCase)>=0)return false;
            if(r.IndexOf("would suffer damage",StringComparison.OrdinalIgnoreCase)>=0)return false;
            if(r.IndexOf("would be defeated",StringComparison.OrdinalIgnoreCase)>=0||r.IndexOf("would be reduced to 0 Health",StringComparison.OrdinalIgnoreCase)>=0)return false;
            if(r.IndexOf("non-Attack",StringComparison.OrdinalIgnoreCase)>=0)return false;
            if(r.IndexOf("Enemy ability or Spell",StringComparison.OrdinalIgnoreCase)>=0&&r.IndexOf("Attack",StringComparison.OrdinalIgnoreCase)<0)return false;
            if(r.IndexOf("two or more allied Heroes",StringComparison.OrdinalIgnoreCase)>=0)return false;
            if(IsQuestLimitedCard(card)&&reactor.UsedQuestCards.Contains(card.Id))return false;
            if(card.Id=="H014-012"&&!HasPiece("Bone Servant",reactor.Id,reactor.LocationId))return false;
            if(card.Id=="H014-013"&&!HasPiece("Bone Servant",reactor.Id,target.LocationId))return false;
            if(r.IndexOf("targeted by an Enemy Attack",StringComparison.OrdinalIgnoreCase)>=0||r.IndexOf("targeted by an Attack",StringComparison.OrdinalIgnoreCase)>=0||r.IndexOf("targeted by Attack",StringComparison.OrdinalIgnoreCase)>=0)return true;
            if(r.IndexOf("Enemy Attack or ability",StringComparison.OrdinalIgnoreCase)>=0||r.IndexOf("ability or Attack",StringComparison.OrdinalIgnoreCase)>=0)return true;
            return false;
        }

        private bool IsBeforeHeroDamageReaction(HeroState reactor, HeroState target, CardData card)
        {
            if (reactor == null || target == null || card == null || !ReactionCanReach(reactor, target, card) || !ReactionTriggerTargetRelationshipIsLegal(reactor, target, card)) return false;
            if (IsQuestLimitedCard(card) && reactor.UsedQuestCards.Contains(card.Id)) return false;
            string rules = card.Rules ?? string.Empty;
            bool fromEnemyAttack = pendingDamageSourceKind == HeroDamageSourceKind.EnemyAttack;
            bool fromEnemyAbility = pendingDamageSourceKind == HeroDamageSourceKind.EnemyAbility;
            bool fromEnemy = fromEnemyAttack || fromEnemyAbility;

            if (rules.IndexOf("from an Enemy Attack", StringComparison.OrdinalIgnoreCase) >= 0 && !fromEnemyAttack) return false;
            if (rules.IndexOf("Enemy Attack or ability", StringComparison.OrdinalIgnoreCase) >= 0 && !fromEnemy) return false;
            if (rules.IndexOf("Enemy ability or Spell", StringComparison.OrdinalIgnoreCase) >= 0 && !fromEnemyAbility) return false;
            if (card.Id == "H004-011" && !fromEnemy) return false;
            if (card.Id == "H014-012" && !HasPiece("Bone Servant", reactor.Id, reactor.LocationId)) return false;
            if (card.Id == "H014-013" && !HasPiece("Bone Servant", reactor.Id, target.LocationId)) return false;

            int remaining = Mathf.Max(0, pendingIncomingDamage);
            bool lethal = remaining >= target.Health && !pendingPreventDefeat && !target.CannotDropBelowOne && !target.AvatarFloorActive;
            if (rules.IndexOf("would be defeated", StringComparison.OrdinalIgnoreCase) >= 0 || rules.IndexOf("would be reduced to 0 Health", StringComparison.OrdinalIgnoreCase) >= 0) return lethal;
            if (rules.IndexOf("Play after", StringComparison.OrdinalIgnoreCase) >= 0)
                return fromEnemyAttack && rules.IndexOf("suffer damage from an Enemy Attack", StringComparison.OrdinalIgnoreCase) >= 0 && remaining > 0;
            return rules.IndexOf("would suffer damage", StringComparison.OrdinalIgnoreCase) >= 0 && remaining > 0;
        }

        private void PopulateBeforeHeroDamageReactionOptions()
        {
            PendingReactionOptions.Clear();
            if (pendingTarget == null || pendingIncomingDamage <= 0) return;
            foreach (var hero in ParticipatingHeroes.Where(h => !h.IsDefeated))
                foreach (var card in hero.Hand.Where(c => string.Equals(c.Speed, "Reaction", StringComparison.OrdinalIgnoreCase)))
                {
                    int cost = GetEffectiveCardCost(hero, card);
                    if (CanPayReactionCost(hero, card, cost) && IsBeforeHeroDamageReaction(hero, pendingTarget, card))
                        PendingReactionOptions.Add(new ReactionOption { HeroId = hero.Id, CardId = card.Id, Label = $"{hero.DisplayName}: {card.Name}" });
                }
        }

        private int ApplyHeroDamageNow(HeroState target, int amount, string source, bool preventDefeat = false, EnemyState sourceEnemy = null)
        {
            if (target == null || target.IsDefeated || amount <= 0) return 0;
            int itemReduction=EquipmentDamageReduction0854(target,amount,source,sourceEnemy);
            if(itemReduction>0){amount=Mathf.Max(0,amount-itemReduction);AddLog($"ITEM DEFENSE — {target.DisplayName} reduces incoming damage by {itemReduction}.");if(amount<=0)return 0;}
            int before = target.Health;
            bool itemFloor=before>0&&before-amount<=0&&EquipmentPreventDefeat0854(target);
            if(itemFloor&&target.UsedQuestCards.Remove(Item0854Prefix+"DefeatRestoreNow")){AddLog($"DAMAGE PREVENTED — {target.DisplayName}'s Item replaces the Defeat with recovery.");return 0;}
            int floor = (target.CannotDropBelowOne || target.AvatarFloorActive || preventDefeat || itemFloor) ? 1 : 0;
            target.Health = Mathf.Max(floor, target.Health - amount);
            int dealt = before - target.Health;
            if(dealt>0&&sourceEnemy!=null&&target.HasStatus("Prepared Position"))RemoveStatus(target.Statuses,"Prepared Position",target.DisplayName,"first damaging Enemy Attack resolved");
            AddLog($"DAMAGE — {target.DisplayName} suffers {dealt} damage ({source}).");
            if (HeroAvatars.TryGetValue(target.Id, out var avatar)) avatar.ShowHit(dealt);
            if (target.Health <= 0) HandleHeroAtZero(target);
            // A Hero at 0 is already Defeated. Automatic Hero abilities cannot activate while Downed/Dead.
            if (dealt > 0 && !target.IsDefeated) { HeroRulesRuntime.OnHeroDamaged(this, target, dealt, sourceEnemy?.Id ?? source); OnEquipmentHeroDamaged0854(target,dealt,source,sourceEnemy); }
            return dealt;
        }

        private bool BeginHeroDamage(HeroState target, int amount, string source, HeroDamageSourceKind sourceKind = HeroDamageSourceKind.Direct, EnemyState sourceEnemy = null, Action<int> afterResolved = null, bool preventDefeat = false)
        {
            if (target == null || target.IsDefeated || amount <= 0)
            {
                afterResolved?.Invoke(0);
                return false;
            }
            // A second modal cannot replace an already-open Reaction window. Queue this damage
            // and start its own reactionable transaction as soon as the current window closes.
            if (HasPendingReactionWindow)
            {
                if (queuedDamageTarget == null)
                {
                    queuedDamageTarget = target; queuedDamageAmount = amount; queuedDamageSource = source;
                    queuedDamageSourceKind = sourceKind; queuedDamageSourceEnemy = sourceEnemy; queuedDamageContinuation = afterResolved; queuedDamagePreventDefeat = preventDefeat;
                    AddLog($"DAMAGE TIMING — {target.DisplayName}'s {amount} pending damage ({source}) waits until the current Reaction window resolves.");
                    return true;
                }
                // The present vertical slice should never require more than one queued Hero-damage event.
                // If it does, resolve the later event canonically rather than losing it.
                int nested = ApplyHeroDamageNow(target, amount, source, preventDefeat, sourceEnemy);
                afterResolved?.Invoke(nested);
                return false;
            }
            pendingReactionKind = ReactionWindowKind.BeforeHeroDamage;
            pendingTarget = target;
            pendingEnemy = null;
            pendingEventHero = null;
            pendingIncomingDamage = amount;
            pendingDamageSource = source ?? "damage";
            pendingDamageSourceKind = sourceKind;
            pendingDamageSourceEnemy = sourceEnemy;
            pendingPreventDefeat = preventDefeat;
            pendingGrantInspiredIfDamagePrevented = false;
            pendingDamageContinuation = afterResolved;
            pendingReactionResolved = false;
            PopulateBeforeHeroDamageReactionOptions();
            if (PendingReactionOptions.Count == 0)
            {
                ResolvePendingHeroDamage();
                return false;
            }
            AddLog($"REACTION WINDOW — {target.DisplayName} would suffer {amount} damage ({pendingDamageSource}); {PendingReactionOptions.Count} legal damage reaction option(s) available.");
            StateChanged?.Invoke();
            return true;
        }

        private bool StartQueuedHeroDamageIfAny()
        {
            if (queuedDamageTarget == null || HasPendingReactionWindow) return false;
            var target = queuedDamageTarget; int amount = queuedDamageAmount; string source = queuedDamageSource;
            var kind = queuedDamageSourceKind; var enemy = queuedDamageSourceEnemy; var continuation = queuedDamageContinuation; bool prevent = queuedDamagePreventDefeat;
            queuedDamageTarget = null; queuedDamageAmount = 0; queuedDamageSource = null; queuedDamageSourceKind = HeroDamageSourceKind.Direct; queuedDamageSourceEnemy = null; queuedDamageContinuation = null; queuedDamagePreventDefeat = false;
            BeginHeroDamage(target, amount, source, kind, enemy, continuation, prevent);
            return true;
        }

        private void ResolvePendingHeroDamage()
        {
            if (pendingReactionKind != ReactionWindowKind.BeforeHeroDamage) return;
            var target = pendingTarget;
            int amount = Mathf.Max(0, pendingIncomingDamage);
            string source = pendingDamageSource ?? "damage";
            bool prevent = pendingPreventDefeat;
            bool inspire = pendingGrantInspiredIfDamagePrevented && amount == 0;
            var inspireSource = pendingEventHero;
            var continuation = pendingDamageContinuation;
            var sourceEnemy = pendingDamageSourceEnemy;
            pendingDamageContinuation = null;
            CloseReactionWindow();
            int dealt = ApplyHeroDamageNow(target, amount, source, prevent, sourceEnemy);
            if (inspire && target != null && !target.Dead) AddStatus(target, "Inspired", 1, 0, "Guardian's Blessing", true, inspireSource);
            continuation?.Invoke(dealt);
            StateChanged?.Invoke();
        }

        private void PopulateBeforeReactionOptions()
        {
            PendingReactionOptions.Clear();if(pendingEnemy==null||pendingTarget==null||pendingCancelAttack)return;
            var aldric=ParticipatingHeroes.FirstOrDefault(h=>h.Id=="Aldric"&&!h.IsDefeated&&h.Id!=pendingTarget.Id&&h.LocationId==pendingTarget.LocationId&&!h.UsedRoundAbilities.Contains("ShieldWall"));
            if(aldric!=null)PendingReactionOptions.Add(new ReactionOption{HeroId=aldric.Id,CardId="ABILITY_SHIELD_WALL",Label="Sir Aldric: Improved Shield Wall"});
            foreach(var hero in ParticipatingHeroes.Where(h=>!h.IsDefeated))
                foreach(var card in hero.Hand.Where(c=>string.Equals(c.Speed,"Reaction",StringComparison.OrdinalIgnoreCase)))
                {
                    int cost=GetEffectiveCardCost(hero,card);if(CanPayReactionCost(hero,card,cost)&&IsBeforeEnemyAttackReaction(hero,pendingTarget,pendingEnemy,card))PendingReactionOptions.Add(new ReactionOption{HeroId=hero.Id,CardId=card.Id,Label=$"{hero.DisplayName}: {card.Name}"});
                }
        }

        private void RetargetPendingEnemyAttack(HeroState newTarget,string source)
        {
            if(pendingReactionKind!=ReactionWindowKind.BeforeEnemyAttack||newTarget==null||pendingTarget==null||pendingTarget.Id==newTarget.Id)return;
            string oldTarget=pendingTarget.DisplayName;
            pendingTarget=newTarget;
            // Protection attached to the previous target cannot migrate onto a new target.
            pendingArmorBonus=0;
            pendingDamageReduction=0;
            pendingPreventDefeat=false;
            AddLog($"REACTION RETARGET — {source}: {oldTarget} → {newTarget.DisplayName}; previous target-only protection does not transfer.");
        }

        private void RevalidateBeforeEnemyAttackReactionOptions(string reason)
        {
            if(pendingReactionKind!=ReactionWindowKind.BeforeEnemyAttack)return;
            PopulateBeforeReactionOptions();
            string targetName=pendingTarget?.DisplayName??"no target";
            AddLog($"REACTION REVALIDATE — {reason}: {PendingReactionOptions.Count} legal option(s) remain against the Attack targeting {targetName}.");
        }

        private void RevalidateBeforeHeroDamageReactionOptions(string reason)
        {
            if(pendingReactionKind!=ReactionWindowKind.BeforeHeroDamage)return;
            PopulateBeforeHeroDamageReactionOptions();
            AddLog($"REACTION REVALIDATE — {reason}: {PendingReactionOptions.Count} legal option(s) remain for {Mathf.Max(0,pendingIncomingDamage)} pending damage.");
        }

        private void PrepareReactionWindow(EnemyState enemy,HeroState target)
        {
            pendingReactionKind=ReactionWindowKind.BeforeEnemyAttack;pendingEnemy=enemy;pendingTarget=target;pendingEventHero=null;pendingLastDamage=0;pendingEnemyDie=0;pendingArmorBonus=0;pendingDamageReduction=0;pendingPreventDefeat=false;pendingCancelAttack=false;pendingReactionResolved=false;PopulateBeforeReactionOptions();
            if(PendingReactionOptions.Count==0)pendingReactionResolved=true;else AddLog($"REACTION WINDOW — {enemy.DisplayName} targets {target.DisplayName}; {PendingReactionOptions.Count} legal pre-Attack reaction option(s) available before the attack roll.");
        }

        private void PreparePostAttackReactionWindow(EnemyState enemy,HeroState target,int dealt)
        {
            PendingReactionOptions.Clear();pendingReactionKind=ReactionWindowKind.None;pendingEnemy=null;pendingTarget=null;pendingEventHero=null;pendingLastDamage=dealt;pendingReactionResolved=true;
            if(enemy==null||target==null||enemy.Defeated)return;
            foreach(var card in target.Hand.Where(c=>string.Equals(c.Speed,"Reaction",StringComparison.OrdinalIgnoreCase)))
            {
                bool eligible=(card.Id=="H009-018"&&target.Id=="Kael"&&dealt==0&&enemy.LocationId==target.LocationId)||(card.Id=="H012-018"&&target.Id=="Garruk"&&dealt>0&&enemy.LocationId==target.LocationId)||(card.Id=="H002-009"&&target.Id=="Lyra"&&dealt==0);
                int cost=GetEffectiveCardCost(target,card);if(eligible&&CanPayReactionCost(target,card,cost))PendingReactionOptions.Add(new ReactionOption{HeroId=target.Id,CardId=card.Id,Label=$"{target.DisplayName}: {card.Name}"});
            }
            if(PendingReactionOptions.Count>0){pendingReactionKind=ReactionWindowKind.AfterEnemyAttack;pendingEnemy=enemy;pendingTarget=target;pendingLastDamage=dealt;pendingReactionResolved=false;AddLog($"REACTION WINDOW — {enemy.DisplayName}'s Attack resolved; {PendingReactionOptions.Count} post-Attack reaction option(s) available.");}
        }

        private void PrepareHeroKillReactionWindow(HeroState killer,EnemyState defeatedEnemy)
        {
            if(HasPendingReactionWindow||killer==null||defeatedEnemy==null)return;PendingReactionOptions.Clear();
            foreach(var cassian in ParticipatingHeroes.Where(h=>h.Id=="Cassian"&&!h.IsDefeated&&h.LocationId==defeatedEnemy.LocationId))
                foreach(var card in cassian.Hand.Where(c=>c.Id=="H008-018"))
                {int cost=GetEffectiveCardCost(cassian,card);if(CanPayReactionCost(cassian,card,cost))PendingReactionOptions.Add(new ReactionOption{HeroId=cassian.Id,CardId=card.Id,Label=$"{cassian.DisplayName}: {card.Name}"});}
            if(PendingReactionOptions.Count>0){pendingReactionKind=ReactionWindowKind.AfterHeroKill;pendingEnemy=defeatedEnemy;pendingEventHero=killer;pendingTarget=null;pendingReactionResolved=false;AddLog($"REACTION WINDOW — {killer.DisplayName} defeated {defeatedEnemy.DisplayName}; Standing Ovation is available.");}
        }

        private void CloseReactionWindow()
        {
            pendingReactionKind=ReactionWindowKind.None;pendingEnemy=null;pendingTarget=null;pendingEventHero=null;pendingLastDamage=0;pendingIncomingDamage=0;pendingDamageSource=null;pendingDamageSourceKind=HeroDamageSourceKind.Direct;pendingDamageSourceEnemy=null;pendingGrantInspiredIfDamagePrevented=false;pendingDamageContinuation=null;PendingReactionOptions.Clear();pendingReactionResolved=true;
        }

        public void UseReaction(string heroId,string cardId)
        {
            if(!HasPendingReactionWindow||!Heroes.TryGetValue(heroId,out var hero))return;
            if(hero.IsDefeated)
            {
                AddLog($"REACTION blocked — {hero.DisplayName} is Downed or Dead and cannot perform Reactions.");
                StateChanged?.Invoke();
                return;
            }
            var chosenOption=PendingReactionOptions.FirstOrDefault(o=>o.HeroId==heroId&&o.CardId==cardId);
            if(chosenOption==null)
            {
                AddLog($"REACTION INPUT — ignored stale/unavailable option {heroId}:{cardId}.");
                StateChanged?.Invoke();
                return;
            }
            AddLog($"REACTION CHOICE — {chosenOption.Label}.");
            if(pendingReactionKind==ReactionWindowKind.BeforeEnemyAttack&&cardId=="ABILITY_SHIELD_WALL")
            {
                hero.UsedRoundAbilities.Add("ShieldWall");RetargetPendingEnemyAttack(hero,"Improved Shield Wall");AddLog("HERO ABILITY — Improved Shield Wall: Sir Aldric becomes the target.");RevalidateBeforeEnemyAttackReactionOptions("target changed by Improved Shield Wall");StateChanged?.Invoke();return;
            }
            var card=hero.Hand.FirstOrDefault(c=>c.Id==cardId);if(card==null)return;int cost=GetEffectiveCardCost(hero,card);if(!CanPayReactionCost(hero,card,cost))return;
            int heldBefore=GeneralActions(hero);
            SpendCardCost(hero,card,cost);
            if(cost>0)AddLog($"REACTION ACTION — {hero.DisplayName} spends {cost} held Action{(cost==1?"":"s")} ({heldBefore} → {GeneralActions(hero)} remaining this Round).");

            if(pendingReactionKind==ReactionWindowKind.AfterEnemyAttack&&(card.Id=="H009-018"||card.Id=="H012-018"||card.Id=="H002-009"))
            {
                var counterTarget=pendingEnemy;int dealt=pendingLastDamage;DiscardFromHand(hero,card,"Reaction");
                if(card.Id=="H002-009"){ApplyHeroReactionFollowup0860(hero,card,counterTarget,dealt);AddLog($"REACTION — {hero.DisplayName} uses {card.Name} after the zero-damage Attack.");CloseReactionWindow();StartQueuedHeroDamageIfAny();StateChanged?.Invoke();return;}
                AddLog($"REACTION — {hero.DisplayName} uses {card.Name} and immediately makes the granted Basic Attack.");CloseReactionWindow();if(counterTarget!=null&&!counterTarget.Defeated)BeginHeroAttack(hero,counterTarget,0,0,card.Name,null);StateChanged?.Invoke();return;
            }
            if(pendingReactionKind==ReactionWindowKind.AfterHeroKill&&card.Id=="H008-018")
            {
                var beneficiary=pendingEventHero;DiscardFromHand(hero,card,"Reaction");if(beneficiary!=null)DrawCards(beneficiary,1);AddLog($"REACTION — {hero.DisplayName} uses Standing Ovation; {beneficiary?.DisplayName??"the defeating Hero"} draws 1 card.");CloseReactionWindow();StartQueuedHeroDamageIfAny();StateChanged?.Invoke();return;
            }

            if (pendingReactionKind == ReactionWindowKind.BeforeHeroDamage)
            {
                string rules = card.Rules ?? string.Empty;
                if (card.Id == "H014-012" && !RemovePiece("Bone Servant", hero.Id, hero.LocationId)) { hero.Actions += cost; return; }
                if (card.Id == "H014-013" && !RemovePiece("Bone Servant", hero.Id, pendingTarget?.LocationId)) { hero.Actions += cost; return; }
                if (IsQuestLimitedCard(card) && !hero.UsedQuestCards.Contains(card.Id)) hero.UsedQuestCards.Add(card.Id);
                int reduction = card.Id == "H013-015"
                    ? (pendingDamageSourceEnemy != null && pendingDamageSourceEnemy.Statuses.Any(st => !st.Beneficial) ? 3 : 2)
                    : ExtractNumber(rules, @"Reduce (?:that )?damage by\s+(\d+)");
                if (reduction > 0) pendingIncomingDamage = Mathf.Max(0, pendingIncomingDamage - reduction);
                if (rules.IndexOf("remains at 1 Health", StringComparison.OrdinalIgnoreCase) >= 0 || rules.IndexOf("cannot be reduced below 1", StringComparison.OrdinalIgnoreCase) >= 0) pendingPreventDefeat = true;
                if (card.Id == "H003-008") { pendingGrantInspiredIfDamagePrevented = true; pendingEventHero = hero; }
                if (card.Id == "H003-010" || card.Id == "H003-020")
                {
                    pendingPreventDefeat = true;
                    if (pendingTarget != null) pendingTarget.Statuses.Clear();
                    if (card.Id == "H003-020" && pendingTarget != null)
                    {
                        hero.LocationId = pendingTarget.LocationId;
                        if (HeroAvatars.TryGetValue(hero.Id, out var elara) && Locations.TryGetValue(hero.LocationId, out var loc)) elara.transform.position = loc.Position;
                    }
                }
                ApplyHeroReactionFollowup0860(hero,card,pendingDamageSourceEnemy,Mathf.Max(0,pendingIncomingDamage));
                DiscardFromHand(hero, card, "Reaction");
                AddLog($"REACTION — {hero.DisplayName} uses {card.Name}; pending damage is now {pendingIncomingDamage}.");
                RevalidateBeforeHeroDamageReactionOptions($"{card.Name} resolved");
                StateChanged?.Invoke();
                return;
            }

            if(pendingReactionKind!=ReactionWindowKind.BeforeEnemyAttack)return;
            string r=card.Rules??"";
            if(card.Id=="H014-012"&&!RemovePiece("Bone Servant",hero.Id,hero.LocationId)){hero.Actions+=cost;return;}
            if(card.Id=="H014-013"&&!RemovePiece("Bone Servant",hero.Id,pendingTarget?.LocationId)){hero.Actions+=cost;return;}
            if(IsQuestLimitedCard(card)&&!hero.UsedQuestCards.Contains(card.Id))hero.UsedQuestCards.Add(card.Id);
            bool becomesTarget=Contains(r,"become the target")&&Contains(r,"instead")&&pendingTarget!=null&&hero.LocationId==pendingTarget.LocationId;
            if(becomesTarget)RetargetPendingEnemyAttack(hero,card.Name);
            if(card.Id=="H013-015")pendingDamageReduction+=pendingEnemy!=null&&pendingEnemy.Statuses.Any(s=>!s.Beneficial)?3:2;
            else pendingDamageReduction+=ExtractNumber(r,@"Reduce (?:that )?damage by\s+(\d+)");
            int armorIncrease=ExtractNumber(r,@"(?:armor|Armor)\s+by\s+(\d+)");if(armorIncrease==0)armorIncrease=ExtractNumber(r,@"\+(\d+)\s+armor");pendingArmorBonus+=armorIncrease;
            if(Contains(r,"remains at 1 Health")||Contains(r,"cannot be reduced below 1"))pendingPreventDefeat=true;
            if(card.Id=="H003-010"||card.Id=="H003-020"){pendingPreventDefeat=true;if(pendingTarget!=null)pendingTarget.Statuses.Clear();if(card.Id=="H003-020"&&pendingTarget!=null){hero.LocationId=pendingTarget.LocationId;if(HeroAvatars.TryGetValue(hero.Id,out var elara)&&Locations.TryGetValue(hero.LocationId,out var loc))elara.transform.position=loc.Position;}}
            if(Contains(r,"End the triggering Enemy's Attack")||Contains(r,"Attack immediately ends"))pendingCancelAttack=true;
            if(card.Id=="H002-008")pendingDamageReduction+=2;
            if(card.Id=="H001-010")AddStatus(hero,"Brace Counter Ready",0,0,card.Name,true,hero,"until this Attack resolves");
            if(Contains(r,"Gain Stealth"))AddStatus(hero,"Stealth",1,0,card.Name,true);
            ApplyHeroReactionFollowup0860(hero,card,pendingEnemy,0);
            if(card.Id=="H004-008"&&pendingEnemy!=null&&pendingTarget!=null&&AuthoredEnemyRange0859(pendingEnemy)<GraphDistance(pendingEnemy.LocationId,pendingTarget.LocationId))pendingCancelAttack=true;
            DiscardFromHand(hero,card,"Reaction");AddLog($"REACTION — {hero.DisplayName} uses {card.Name}.");RevalidateBeforeEnemyAttackReactionOptions($"{card.Name} resolved");StateChanged?.Invoke();
        }

        public void ResolveReactionWindow()
        {
            if(!HasPendingReactionWindow)return;
            if(pendingReactionKind==ReactionWindowKind.BeforeHeroDamage){ResolvePendingHeroDamage();return;}
            if(pendingReactionKind==ReactionWindowKind.BeforeEnemyAttack)pendingReactionResolved=true;else{CloseReactionWindow();StartQueuedHeroDamageIfAny();}StateChanged?.Invoke();
        }

        private void ResolvePendingEnemyAttack()
        {
            var enemy=pendingEnemy;var target=pendingTarget;int armorBonus=pendingArmorBonus;int reduction=pendingDamageReduction;bool prevent=pendingPreventDefeat;bool cancel=pendingCancelAttack;int forcedDie=pendingEnemyDie;CloseReactionWindow();
            if(enemy==null||target==null){IsResolving=false;return;}
            if(cancel){AddLog($"REACTION — {enemy.DisplayName}'s Attack is canceled before the attack roll.");IsResolving=false;StateChanged?.Invoke();return;}

            // 0.8.10 timing: the d6 is rolled only after target/retarget/protection reactions finish.
            int die=forcedDie>=1&&forcedDie<=6?forcedDie:UnityEngine.Random.Range(1,7);
            int pendingDamage=CalculateEnemyAttackDamage(enemy,target,die,armorBonus,reduction);
            int targetEnemyBonus=EnemyAttackModifier0859(enemy,target)-EnemyAttackModifier0859(enemy,null);
            int effectiveEnemyAttack=Mathf.Max(0,EffectiveEnemyAttack(enemy)+targetEnemyBonus+EquipmentEnemyAttackModifier0854(target,enemy)+LocationEnemyAttackVsTargetModifier0858(enemy,target));
            int effectiveHeroArmor=Mathf.Max(0,EffectiveHeroArmor(target)-EnemyAttackArmorIgnore0859(enemy,target))+Mathf.Max(0,armorBonus);
            AddLog($"ENEMY ATTACK ROLL — {enemy.DisplayName} → {target.DisplayName}: d6 {die} + Attack {effectiveEnemyAttack} − Armor {effectiveHeroArmor} − Reduction {Mathf.Max(0,reduction)} = {pendingDamage} pending damage.");

            bool impact=false,finished=false;
            void Impact()
            {
                if(impact)return;impact=true;
                BeginHeroDamage(target,pendingDamage,$"{enemy.DisplayName} Attack",HeroDamageSourceKind.EnemyAttack,enemy,dealt=>
                {
                    AddLog($"ENEMY ATTACK RESOLVED — {enemy.DisplayName} deals {dealt} damage to {target.DisplayName}.");
                    OnQuestEnemyAttackResolved0832(enemy,target,dealt);
                    OnEnemyAttackResolved0859(enemy,target,dealt);
                    OnHeroEnemyAttackResolved0860(enemy,target,dealt);
                    PreparePostAttackReactionWindow(enemy,target,dealt);
                    StateChanged?.Invoke();
                },prevent);
                StateChanged?.Invoke();
            }
            void Finished(){if(finished)return;finished=true;if(!impact)Impact();IsResolving=false;StateChanged?.Invoke();}
            if(EnemyAvatars.TryGetValue(enemy.Id,out var ea)&&HeroAvatars.TryGetValue(target.Id,out var ha))ea.PlayAttack(ha.transform.position,Impact,"Melee",Finished);else{Impact();Finished();}
        }

        private void HandleHeroAtZero(HeroState hero)
        {
            if(hero==null||hero.Health>0)return;
            hero.Health=0;
            // Defeat is classified once, at the instant the Hero reaches 0. A Downed Hero does not
            // later become Dead merely because the ally who witnessed the defeat leaves the Location.
            if(hero.Downed||hero.Dead)return;
            bool ally=ParticipatingHeroes.Any(h=>h.Id!=hero.Id&&!h.IsDefeated&&h.LocationId==hero.LocationId);
            hero.Downed=ally;hero.Dead=!ally;
            if(hero.Dead){hero.Actions=0;hero.AttackOnlyActions=0;}
            AddLog(hero.Downed
                ?$"DEFEAT — {hero.DisplayName} is Downed at 0 Health. Remaining Actions are retained but unusable until legally revived and their normal timing permits them."
                :$"DEFEAT — {hero.DisplayName} is Dead at 0 Health (no non-Defeated allied Hero shared the Location at defeat). Normal Healing Potions cannot revive a Dead Hero.");
            if(HeroAvatars.TryGetValue(hero.Id,out var av))av.ShowDowned();
            ResolveEncounterHeroDefeat0853(hero);
            EvaluatePartyDefeat();
        }

        private void EvaluatePartyDefeat()
        {
            if(PartySize<=0)return;
            if(QuestComplete)
            {
                if(ParticipatingHeroes.All(h=>h.IsDefeated))AddLog($"POST-QUEST DEFEAT — all participating Heroes are Defeated after {CurrentQuestId0829} completion. The completed Quest remains recorded.");
                return;
            }
            if(QuestFailed)return;
            if(ParticipatingHeroes.All(h=>h.IsDefeated))FailCurrentQuestForPartyDefeat0829();
        }

        public bool HasLegalHealingPotionTarget(HeroState actor)
        {
            if(actor==null||actor.IsDefeated||actor.HealingPotions<1)return false;
            return ParticipatingHeroes.Any(h=>!h.Dead&&h.LocationId==actor.LocationId&&(h.Downed||h.Health<h.MaxHealth));
        }

        public bool CanUseHealingPotionOn(HeroState actor,HeroState target)
        {
            return actor!=null&&target!=null&&!actor.IsDefeated&&!target.Dead&&actor.HealingPotions>0&&!IsResolving
                &&CanUseStandardAction(actor)&&actor.LocationId==target.LocationId&&(target.Downed||target.Health<target.MaxHealth);
        }

        public void UseHealingPotion()
        {
            var actor=TurnHero??SelectedHero;
            if(actor==null)return;
            // During an active turn, the selected party portrait is the explicit Potion target.
            // This preserves player choice when multiple injured/Downed Heroes share the Location.
            var target=SelectedHero;
            if(target!=null&&CanUseHealingPotionOn(actor,target)){TryUseHealingPotionOn(target.Id);return;}
            if(TurnHero!=null)
            {
                AddLog($"POTION TARGET — select an injured or Downed non-Dead Hero in {actor.DisplayName}'s Location, then use Potion / Revive.");
                StateChanged?.Invoke();
                return;
            }
            target=ParticipatingHeroes
                .Where(h=>!h.Dead&&h.LocationId==actor.LocationId&&(h.Downed||h.Health<h.MaxHealth))
                .OrderByDescending(h=>h.Downed)
                .ThenBy(h=>(float)h.Health/Mathf.Max(1,h.MaxHealth))
                .FirstOrDefault();
            if(target==null){AddLog($"POTION blocked — no injured or Downed non-Dead Hero is in {actor.DisplayName}'s Location.");StateChanged?.Invoke();return;}
            TryUseHealingPotionOn(target.Id);
        }

        public bool TryUseHealingPotionOn(string targetHeroId)
        {
            var actor=TurnHero??SelectedHero;
            if(actor==null||!Heroes.TryGetValue(targetHeroId,out var target)||!target.Participating)return false;
            if(target.Dead)
            {
                AddLog($"REVIVE blocked — {target.DisplayName} is Dead; a normal Healing Potion cannot return a Dead Hero.");
                StateChanged?.Invoke();
                return false;
            }
            if(!CanUseHealingPotionOn(actor,target))
            {
                AddLog($"POTION blocked — {actor.DisplayName} must be taking their turn, have 1 general Action and a Potion, and target an injured/Downed Hero in the same Location.");
                StateChanged?.Invoke();
                return false;
            }
            if(!RequireHeroTurn(actor,"use a Healing Potion"))return false;
            actor.Actions--; bool preservePotion=false;
            if(actor.Id=="Mira" && !actor.UsedRoundAbilities.Contains("FieldEngineer"))
            {
                actor.UsedRoundAbilities.Add("FieldEngineer"); int roll=UnityEngine.Random.Range(1,7); preservePotion=roll>=4; AddLog($"HERO ABILITY — Improved Field Engineer rolls {roll}: {(preservePotion?"Potion preserved":"Potion consumed")}."); DrawCards(actor,1); actor.PendingDiscardCount+=1;
            }
            if(!preservePotion) actor.HealingPotions--;
            bool wasDowned=target.Downed;
            HealHero(target,4,"Common Healing Potion",actor);
            AddLog(wasDowned
                ?$"POTION — {actor.DisplayName} spends 1 Action to use a Common Healing Potion on {target.DisplayName}; the Downed Hero returns at {target.Health} Health."
                :$"POTION — {actor.DisplayName} spends 1 Action to use a Common Healing Potion on {target.DisplayName}.");
            StateChanged?.Invoke();
            return true;
        }

        public void HealHero(HeroState target,int amount,string source,HeroState sourceHero=null)
        {
            if(target==null||target.Dead||amount<=0)return;
            if(target.HasStatus("No Healing")){AddLog($"HEAL BLOCKED — {target.DisplayName} cannot recover Health this turn ({source}).");return;}
            int before=target.Health;bool wasDowned=target.Downed;
            int equipmentHealBonus=EquipmentHealingBonus0854(sourceHero,target,amount);if(equipmentHealBonus>0){amount+=equipmentHealBonus;AddLog($"ITEM HEALING — {sourceHero.DisplayName}'s Equipment adds +{equipmentHealBonus} Health recovery.");}
            string consecratedKey=$"ConsecratedHeal:{target.LocationId}";
            if(before<target.MaxHealth&&LocationHasTag(target.LocationId,"Consecrated")&&!target.UsedRoundAbilities.Contains(consecratedKey))
            {
                target.UsedRoundAbilities.Add(consecratedKey); amount+=1;
                AddLog($"LOCATION EFFECT — Consecrated grants {target.DisplayName} +1 additional Health recovery (first recovery here this Round).");
            }
            amount=ModifyLocationHealing0858(target,amount,source);
            amount=ModifyWorldEventHealing0857(target,amount);
            amount=ModifyEnemyHealing0859(target,amount);
            target.Health=Mathf.Min(target.MaxHealth,target.Health+amount);
            int healed=target.Health-before;
            if(healed<=0)return;
            if(wasDowned&&target.Health>0)
            {
                target.Downed=false;
                AddLog($"REVIVE — {target.DisplayName} recovers {healed} Health ({source}) and is no longer Downed. Revival grants no Actions and does not reset turn completion.");
                if(HeroAvatars.TryGetValue(target.Id,out var av))av.ReviveVisual();
            }
            else AddLog($"HEAL — {target.DisplayName} recovers {healed} Health ({source}).");
            OnLocationHealingResolved0858(target,healed);
            if(sourceHero!=null){HeroRulesRuntime.OnHealOther(this,sourceHero,target);OnEquipmentHealingResolved0854(sourceHero,target,healed);}
            HeroRulesRuntime.OnAnyHeroHealed(this,target);
        }

#if UNITY_EDITOR
        public int DebugApplyHeroDamage(string heroId,int amount,string source="0.8.14 validation")
        {
            if(!Heroes.TryGetValue(heroId,out var hero))return 0;
            return ApplyHeroDamageNow(hero,Mathf.Max(0,amount),source,false,null);
        }

        public bool DebugGiveCardToHand(string heroId,string cardId)
        {
            if(!Heroes.TryGetValue(heroId,out var hero))return false;
            if(hero.Hand.Any(c=>c.Id==cardId))
            {
                AddLog($"TEST CARD — {hero.DisplayName} already has {HeroDatabase.GetCard(cardId)?.Name??cardId} in hand.");
                StateChanged?.Invoke();
                return true;
            }
            int deckIndex=hero.Deck.FindIndex(id=>string.Equals(id,cardId,StringComparison.OrdinalIgnoreCase));
            if(deckIndex>=0)hero.Deck.RemoveAt(deckIndex);
            else
            {
                int discardIndex=hero.Discard.FindIndex(id=>string.Equals(id,cardId,StringComparison.OrdinalIgnoreCase));
                if(discardIndex>=0)hero.Discard.RemoveAt(discardIndex);
                else return false;
            }
            var card=HeroDatabase.GetCard(cardId);if(card==null)return false;
            hero.Hand.Add(card);
            AddLog($"TEST CARD — moved {card.Name} into {hero.DisplayName}'s hand (Editor test only).");
            StateChanged?.Invoke();
            return true;
        }

        public bool DebugOpenEnemyAttackDamageWindow(string enemyId,string targetHeroId,int forcedDie=6)
        {
            if(!Enemies.TryGetValue(enemyId,out var enemy)||!Heroes.TryGetValue(targetHeroId,out var target))return false;
            if(enemy.Defeated||target.IsDefeated)return false;
            enemy.LocationId=target.LocationId;
            int die=Mathf.Clamp(forcedDie,1,6);
            int damage=CalculateEnemyAttackDamage(enemy,target,die);
            AddLog($"TEST ATTACK ROLL — {enemy.DisplayName} → {target.DisplayName}: forced d6 {die} produces {damage} post-Armor pending damage.");
            BeginHeroDamage(target,damage,$"{enemy.DisplayName} Attack (Editor test)",HeroDamageSourceKind.EnemyAttack,enemy,dealt=>
            {
                AddLog($"TEST ATTACK RESOLVED — {enemy.DisplayName} deals {dealt} damage to {target.DisplayName}.");
                StateChanged?.Invoke();
            });
            StateChanged?.Invoke();
            return true;
        }

        public int DebugEffectiveHeroAttack(string heroId)=>Heroes.TryGetValue(heroId,out var hero)?EffectiveHeroAttack(hero):-1;
        public int DebugEffectiveHeroArmor(string heroId)=>Heroes.TryGetValue(heroId,out var hero)?EffectiveHeroArmor(hero):-1;
        public int DebugEffectiveEnemyAttack(string enemyId)=>Enemies.TryGetValue(enemyId,out var enemy)?EffectiveEnemyAttack(enemy):-1;
        public int DebugEffectiveEnemyArmor(string enemyId)=>Enemies.TryGetValue(enemyId,out var enemy)?EffectiveEnemyArmor(enemy):-1;
        public int DebugMarkedAttackBonus(string heroId,string enemyId)=>Heroes.TryGetValue(heroId,out var hero)&&Enemies.TryGetValue(enemyId,out var enemy)?MarkedAttackBonus(hero,enemy):0;
        public int DebugConsumeTargetStatusAttackBonus(string heroId,string enemyId)=>Heroes.TryGetValue(heroId,out var hero)&&Enemies.TryGetValue(enemyId,out var enemy)?ConsumeTargetStatusAttackBonus(hero,enemy,true):0;
        public bool DebugTryGrantAdditionalHeroAction(string heroId,int amount=1,string source="0.8.17 test")=>Heroes.TryGetValue(heroId,out var hero)&&TryGrantAdditionalHeroAction(hero,amount,source,false);
        public string DebugChooseEnemyAttackTarget(string enemyId)=>Enemies.TryGetValue(enemyId,out var enemy)?ChooseEnemyAttackTarget(enemy)?.Id:null;
        public bool DebugBreakHiddenOnAttack(string heroId)=>Heroes.TryGetValue(heroId,out var hero)&&BreakHiddenOnAttack(hero);
        public bool DebugResolveEnemyKnockedDownActivation(string enemyId)=>Enemies.TryGetValue(enemyId,out var enemy)&&ResolveEnemyKnockedDownActivation(enemy);
        public void DebugResolveEnemyStartActivationConditions(string enemyId){if(Enemies.TryGetValue(enemyId,out var enemy))ResolveEnemyStartActivationConditions(enemy);StateChanged?.Invoke();}
        public void DebugResolveEnemyEndActivationConditions(string enemyId,bool performedAttack){if(Enemies.TryGetValue(enemyId,out var enemy))ResolveEnemyEndActivationConditions(enemy,performedAttack);StateChanged?.Invoke();}
        public void DebugApplyCardStatuses(string heroId,string cardId,string enemyId=null,bool attackDamage=true)
        {
            if(!Heroes.TryGetValue(heroId,out var hero))return;var card=HeroDatabase.GetCard(cardId);if(card==null)return;
            if(attackDamage&&card.Kind==CardKind.Attack&&Enemies.TryGetValue(enemyId??"",out var enemy))ApplyAttackStatusesFromCard(hero,card,enemy,1);
            else ApplyStatusesFromCard(hero,card,enemyId);
            StateChanged?.Invoke();
        }
        public void DebugAddLocationTag(string locationId,string tag,string sourceHeroId,string source="0.8.18 validation",StatusExpiryTiming timing=StatusExpiryTiming.Persistent,int triggers=0)
        {
            Heroes.TryGetValue(sourceHeroId??string.Empty,out var hero); AddLocationTag(locationId,tag,source,hero,timing,triggers); StateChanged?.Invoke();
        }
        public void DebugAdvanceLocationTagClock(string heroId,StatusExpiryTiming timing)
        {
            Heroes.TryGetValue(heroId??string.Empty,out var hero); AdvanceLocationTagClock(hero,timing); StateChanged?.Invoke();
        }
        public void DebugResolveLocationReveal(string locationId,string heroId)
        {
            if(Locations.TryGetValue(locationId??string.Empty,out var location)){Heroes.TryGetValue(heroId??string.Empty,out var hero);ResolveLocationReveal(location,hero);}StateChanged?.Invoke();
        }
        public void DebugResolveLocationFirstVisit(string locationId,string heroId)
        {
            if(Locations.TryGetValue(locationId??string.Empty,out var location)&&Heroes.TryGetValue(heroId??string.Empty,out var hero))ResolveLocationFirstVisit(location,hero);StateChanged?.Invoke();
        }
        public void DebugUseLocationRevealOption(string heroId,string choice=null)
        {
            if(!Heroes.ContainsKey(heroId??string.Empty))return;SelectedHeroId=heroId;UseLocationRevealOption(choice);
        }
        public void DebugResetLocationSearch(string locationId){if(Locations.TryGetValue(locationId??string.Empty,out var location)){location.Searched=false;location.SearchRequests=0;}StateChanged?.Invoke();}
        public bool DebugSetSearchDeckTop(string cardId)
        {
            if(!SearchEncounterContent0820.SearchCards.ContainsKey(cardId??string.Empty))return false;
            int i=SearchDeck.FindIndex(id=>string.Equals(id,cardId,StringComparison.OrdinalIgnoreCase));if(i>=0)SearchDeck.RemoveAt(i);
            else {i=SearchDiscard.FindIndex(id=>string.Equals(id,cardId,StringComparison.OrdinalIgnoreCase));if(i>=0)SearchDiscard.RemoveAt(i);}
            SearchDeck.Insert(0,cardId);return true;
        }
        public bool DebugSetEncounterDeckTop(string cardId)
        {
            if(!SearchEncounterContent0820.EncounterCards.ContainsKey(cardId??string.Empty))return false;
            int i=EncounterDeck.FindIndex(id=>string.Equals(id,cardId,StringComparison.OrdinalIgnoreCase));if(i>=0)EncounterDeck.RemoveAt(i);
            else {i=EncounterDiscard.FindIndex(id=>string.Equals(id,cardId,StringComparison.OrdinalIgnoreCase));if(i>=0)EncounterDiscard.RemoveAt(i);}
            EncounterDeck.Insert(0,cardId);return true;
        }
        public void DebugResolveTravelEncounter(string locationId,string heroId,int forcedDie=1,int modifier=0)
        {
            if(Locations.TryGetValue(locationId??string.Empty,out var location)&&Heroes.TryGetValue(heroId??string.Empty,out var hero))ResolveTravelEncounter(location,hero,1,modifier,"0.8.23 validation",forcedDie);
            StateChanged?.Invoke();
        }
        public void DebugResolveFixedTravelEncounter(string locationId,string heroId,int severity=3)
        {
            if(Locations.TryGetValue(locationId??string.Empty,out var location)&&Heroes.TryGetValue(heroId??string.Empty,out var hero))ResolveTravelEncounter(location,hero,1,0,"0.8.25 fixed-severity validation",0,severity,true);
            StateChanged?.Invoke();
        }
        public bool DebugMatchesSearchLocation(string cardId,string locationId)=>SearchEncounterContent0820.SearchCards.TryGetValue(cardId??string.Empty,out var card)&&Locations.TryGetValue(locationId??string.Empty,out var location)&&SearchEncounterContent0820.MatchesLocation(location,card.Terrain);
        public bool DebugMatchesEncounterLocation(string cardId,string locationId)=>SearchEncounterContent0820.EncounterCards.TryGetValue(cardId??string.Empty,out var card)&&Locations.TryGetValue(locationId??string.Empty,out var location)&&SearchEncounterContent0820.MatchesLocation(location,card.Terrain);
        public int DebugEncounterSpawnCount(string templateId=null)=>Enemies.Values.Count(e=>e.EncounterSpawned&&(string.IsNullOrEmpty(templateId)||string.Equals(e.TemplateId,templateId,StringComparison.OrdinalIgnoreCase)));
        public int DebugActiveEncounterCount()=>ActiveEncounters.Count;
        public string DebugLatestActiveEncounterId()=>ActiveEncounters.LastOrDefault()?.InstanceId;
        public int DebugEncounterRemaining(string instanceId)=>RemainingEncounterEnemies(ActiveEncounters.FirstOrDefault(e=>string.Equals(e.InstanceId,instanceId,StringComparison.OrdinalIgnoreCase)));
        public int DebugEncounterObjectCount(string instanceId,string type=null)=>EncounterObjects.Count(o=>string.Equals(o.EncounterInstanceId,instanceId,StringComparison.OrdinalIgnoreCase)&&(string.IsNullOrEmpty(type)||string.Equals(o.Type,type,StringComparison.OrdinalIgnoreCase)));
        public int DebugEncounterBlockingObjectCount(string instanceId)=>RemainingEncounterBlockingObjects(ActiveEncounters.FirstOrDefault(e=>string.Equals(e.InstanceId,instanceId,StringComparison.OrdinalIgnoreCase)));
        public EncounterObjectState DebugFirstEncounterObject(string instanceId,string type=null)=>EncounterObjects.FirstOrDefault(o=>string.Equals(o.EncounterInstanceId,instanceId,StringComparison.OrdinalIgnoreCase)&&(string.IsNullOrEmpty(type)||string.Equals(o.Type,type,StringComparison.OrdinalIgnoreCase)));
        public void DebugTriggerEncounterEntry(string heroId,string locationId){if(Heroes.TryGetValue(heroId??string.Empty,out var hero)&&Locations.TryGetValue(locationId??string.Empty,out var location))ResolveEncounterObjectsOnHeroEnter(location,hero);StateChanged?.Invoke();}
        public bool DebugInteractEncounterObject(string heroId,string objectId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero)||!EncounterObjects.Any(o=>o.ObjectId==objectId))return false;
            var obj=EncounterObjects.First(o=>o.ObjectId==objectId);
            return ResolveEncounterObjectInteraction(hero,obj,false);
        }
        public EnemyState DebugEncounterEnemy(string instanceId)=>Enemies.Values.FirstOrDefault(e=>e.EncounterSpawned&&string.Equals(e.EncounterInstanceId,instanceId,StringComparison.OrdinalIgnoreCase)&&!e.Defeated);
        public void DebugSetPartyGold(int value){PartyGold=Mathf.Max(0,value);StateChanged?.Invoke();}
        public void DebugQueueTravelSeverity(int amount){QueueNextTravelEncounterSeverity(amount,"0.8.23 validation");StateChanged?.Invoke();}
        public void DebugQueueSkipEncounter(){QueueSkipNextTravelEncounter("0.8.23 validation");StateChanged?.Invoke();}
        public int DebugEnemyMovementAllowance(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var enemy)?EnemyMovementAllowance(enemy):0;
        public int DebugMoveEnemyForActivation(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var enemy)?MoveEnemyTowardNearestHero(enemy):0;
        public bool DebugRemoveEnemyFromPlayWithoutDefeat(string enemyId)=>Enemies.TryGetValue(enemyId??string.Empty,out var enemy)&&RemoveEnemyFromPlayWithoutDefeat(enemy,"0.8.24 validation");
        public string DebugEncounterDeckTop(int count=3)=>string.Join(",",EncounterDeck.Take(Mathf.Max(0,count)));
        public string DebugPendingDeckControlMode()=>PendingEncounterDeckControl?.Mode;
        public bool DebugChooseEncounterDeckControl(string key)=>ChooseEncounterDeckControl(key);
        public string DebugPendingRewardChoiceMode()=>PendingRewardChoiceMode;
        public bool DebugChooseRewardChoice(string key)=>ChooseRewardChoice(key);
        public void DebugSetGeneralReputation(int value){GeneralReputation=Mathf.Clamp(value,-5,6);StateChanged?.Invoke();}
#endif

        public void AddStatus(HeroState target,string name,int turns,int rounds,string source,bool beneficial,HeroState sourceHero=null,string timingText=null)
        {
            if(target==null)return;
            if(beneficial&&string.Equals(name,"Inspired",StringComparison.OrdinalIgnoreCase)&&Enemies.Values.Any(e=>!e.Defeated&&e.TemplateId=="E122"&&e.LocationId==target.LocationId)){AddLog($"ENEMY AURA — {target.DisplayName} cannot gain Inspired while a Black Sun Knight is active here.");return;}
            if(!beneficial&&EquipmentBlocksCondition0854(target,name)){AddLog($"ITEM IMMUNITY — {target.DisplayName} ignores {name} from equipped gear.");return;}
            if(!beneficial&&TryBlockConditionBySearchWard0852(target,name))return;
            var current=target.Statuses.FirstOrDefault(s=>string.Equals(s.Name,name,StringComparison.OrdinalIgnoreCase));
            if(current==null){current=new StatusInstance{Name=name,Id=HeroDatabase.GetStatus(name)?.id};target.Statuses.Add(current);}
            if(sourceHero==null&&Heroes.TryGetValue(SelectedHeroId,out var selectedSource))sourceHero=selectedSource;
            current.Source=source;current.Beneficial=beneficial;current.SourceHeroId=sourceHero?.Id;current.AppliedRound=Round;
            ConfigureStatusTiming(current,name,turns,rounds,timingText,sourceHero,target,null);
            AddLog($"STATUS — {target.DisplayName} gains {name}{StatusTimingLogSuffix(current)}.");
            HeroRulesRuntime.OnStatusApplied(this,sourceHero,target,null,current);
            ResolveWorldEventBeneficialStatus0857(target,current);
            OnLocationHeroStatusAdded0858(target,name);
        }
        public void AddStatus(EnemyState target,string name,int turns,int rounds,string source,bool beneficial,HeroState sourceHero,string timingText=null)
        {
            if(target==null)return;
            var current=target.Statuses.FirstOrDefault(s=>string.Equals(s.Name,name,StringComparison.OrdinalIgnoreCase));
            if(current==null){current=new StatusInstance{Name=name,Id=HeroDatabase.GetStatus(name)?.id};target.Statuses.Add(current);}
            current.Source=source;current.Beneficial=beneficial;current.SourceHeroId=sourceHero?.Id;current.AppliedRound=Round;
            ConfigureStatusTiming(current,name,turns,rounds,timingText,sourceHero,null,target);
            AddLog($"STATUS — {target.DisplayName} gains {name}{StatusTimingLogSuffix(current)}.");
            HeroRulesRuntime.OnStatusApplied(this,sourceHero,null,target,current);
        }

        private static string StatusTimingLogSuffix(StatusInstance status)
        {
            if(status==null||status.ExpiryTiming==StatusExpiryTiming.Persistent)return string.Empty;
            return $" [{status.ExpiryTiming}, {Mathf.Max(1,status.RemainingExpiryTriggers)} trigger{(status.RemainingExpiryTriggers==1?"":"s")}]";
        }

        private bool RemoveStatus(List<StatusInstance> statuses,string name,string ownerName,string reason=null)
        {
            if(statuses==null)return false;
            int removed=statuses.RemoveAll(s=>s!=null&&string.Equals(s.Name,name,StringComparison.OrdinalIgnoreCase));
            if(removed>0&&!string.IsNullOrEmpty(reason))AddLog($"STATUS REMOVED — {ownerName} loses {name} ({reason}).");
            return removed>0;
        }

        private bool TryGrantAdditionalHeroAction(HeroState hero,int amount,string source,bool attackOnly=false)
        {
            if(hero==null||hero.IsDefeated||amount<=0)return false;
            if(hero.HasStatus("Shocked"))
            {
                RemoveStatus(hero.Statuses,"Shocked",hero.DisplayName,"prevented an additional Action");
                AddLog($"STATUS — Shocked prevents {hero.DisplayName} from gaining the additional Action from {source}.");
                return false;
            }
            hero.Actions+=amount;
            if(attackOnly)hero.AttackOnlyActions+=amount;
            AddLog(attackOnly
                ?$"{source} — {hero.DisplayName} gains {amount} Action{(amount==1?"":"s")} that may only be spent on Attack cards this turn."
                :$"{source} — {hero.DisplayName} gains {amount} additional Action{(amount==1?"":"s")}.");
            return true;
        }

        private bool BreakHiddenOnAttack(HeroState hero)
        {
            if(hero==null||!hero.HasStatus("Hidden"))return false;
            RemoveStatus(hero.Statuses,"Hidden",hero.DisplayName,"attacked");
            AddLog($"STATUS — {hero.DisplayName} attacks and loses Hidden.");
            return true;
        }

        private bool ResolveEnemyKnockedDownActivation(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated||!enemy.Statuses.Any(s=>string.Equals(s.Name,"Knocked Down",StringComparison.OrdinalIgnoreCase)))return false;
            RemoveStatus(enemy.Statuses,"Knocked Down",enemy.DisplayName,"spends its modeled Action to Stand");
            AddLog($"STATUS — {enemy.DisplayName} spends this activation standing and does not Attack.");
            ResolveEnemyEndActivationConditions(enemy,false);
            CompleteEnemyActivation(enemy);
            return true;
        }

        private HeroState ChooseEnemyAttackTarget(EnemyState enemy)
        {
            if(enemy==null)return null;
            int range=EnemyAttackRangeConnections0830(enemy);
            var taunt=enemy.Statuses.FirstOrDefault(s=>string.Equals(s.Name,"Taunted",StringComparison.OrdinalIgnoreCase));
            if(taunt!=null&&!string.IsNullOrEmpty(taunt.SourceHeroId)&&Heroes.TryGetValue(taunt.SourceHeroId,out var taunter)
                && taunter.Participating&&!taunter.IsDefeated&&!taunter.Moving&&!taunter.HasStatus("Cannot Be Targeted")&&GraphDistance(enemy.LocationId,taunter.LocationId)<=range)
            {
                AddLog($"STATUS — {enemy.DisplayName} is Taunted and must target {taunter.DisplayName} if able.");
                return taunter;
            }
            return ChooseEnemyPriorityTarget0859(enemy,ParticipatingHeroes.Where(h=>!h.IsDefeated&&!h.Moving&&!h.HasStatus("Cannot Be Targeted")&&GraphDistance(enemy.LocationId,h.LocationId)<=range));
        }

        private bool ResolveEnemyStartActivationConditions(EnemyState enemy)
        {
            if(enemy==null||enemy.Defeated)return false;
            var meteorBurn=enemy.Statuses.FirstOrDefault(s=>string.Equals(s.Name,"Burning",StringComparison.OrdinalIgnoreCase)&&string.Equals(s.Source,"Meteor Shard",StringComparison.OrdinalIgnoreCase));
            if(meteorBurn!=null)
            {
                DealDirectDamage(enemy,1,"Burning (Meteor Shard)");
                RemoveStatus(enemy.Statuses,"Burning",enemy.DisplayName,"Meteor Shard one-shot Burning resolved");
                if(enemy.Defeated)return true;
            }
            return false;
        }

        private void ResolveEnemyEndActivationConditions(EnemyState enemy,bool performedAttack)
        {
            if(enemy==null||enemy.Defeated)return;
            if(performedAttack&&enemy.Statuses.Any(s=>string.Equals(s.Name,"Bleeding",StringComparison.OrdinalIgnoreCase)))
                DealDirectDamage(enemy,1,"Bleeding");
            if(!enemy.Defeated&&enemy.Statuses.Any(s=>string.Equals(s.Name,"Burning",StringComparison.OrdinalIgnoreCase)))
                DealDirectDamage(enemy,1,"Burning");
        }

        private void ConfigureStatusTiming(StatusInstance status,string name,int legacyTurns,int legacyRounds,string timingText,HeroState sourceHero,HeroState heroTarget,EnemyState enemyTarget)
        {
            string clause=FindStatusDurationClause(timingText,name);
            string duration=!string.IsNullOrEmpty(clause)?clause:(HeroDatabase.GetStatus(name)?.duration??string.Empty);
            string normalized=duration.ToLowerInvariant();
            StatusExpiryTiming timing=StatusExpiryTiming.Persistent;
            int triggers=0;

            if(normalized.Contains("end of the round")||normalized.Contains("end of round"))
            { timing=StatusExpiryTiming.EndOfRound;triggers=Mathf.Max(1,legacyRounds>0?legacyRounds:1); }
            else if(normalized.Contains("next activation")||normalized.Contains("next enemy activation"))
            {
                timing=StatusExpiryTiming.EndOfTargetActivation;triggers=1;
                if(enemyTarget!=null&&pendingEnemy==enemyTarget&&(IsResolving||HasPendingReactionWindow))triggers=2;
            }
            else if(normalized.Contains("source's next turn")||normalized.Contains("source’s next turn"))
            {
                timing=StatusExpiryTiming.EndOfSourceTurn;triggers=sourceHero!=null&&IsHeroTakingTurn(sourceHero)?2:1;
            }
            else if((normalized.Contains("start of")||normalized.Contains("start of the"))&&normalized.Contains("next turn"))
            {
                bool targetClock=normalized.Contains("their next turn")||normalized.Contains("that hero's next turn")||normalized.Contains("that hero’s next turn")||normalized.Contains("target's next turn")||normalized.Contains("target’s next turn");
                timing=targetClock?StatusExpiryTiming.StartOfTargetTurn:StatusExpiryTiming.StartOfSourceTurn;triggers=1;
            }
            else if(normalized.Contains("end of")&&normalized.Contains("next turn"))
            {
                bool sourceClock=normalized.Contains("source")||normalized.Contains("caster");
                timing=sourceClock?StatusExpiryTiming.EndOfSourceTurn:StatusExpiryTiming.EndOfTargetTurn;
                bool current=sourceClock?(sourceHero!=null&&IsHeroTakingTurn(sourceHero)):(heroTarget!=null&&IsHeroTakingTurn(heroTarget));
                triggers=current?2:1;
            }
            else if(normalized.Contains("end of turn")||normalized.Contains("end of the turn"))
            { timing=StatusExpiryTiming.EndOfTargetTurn;triggers=1; }
            else if(normalized.Contains("until removed")||normalized.Contains("until triggered")||normalized.Contains("until you stand")||normalized.Contains("until you attack")||normalized.Contains("campaign persistent")||normalized.Contains("as specified"))
            { timing=StatusExpiryTiming.Persistent;triggers=0; }
            else if(legacyRounds>0)
            { timing=StatusExpiryTiming.EndOfRound;triggers=legacyRounds; }
            else if(legacyTurns>0)
            { timing=enemyTarget!=null?StatusExpiryTiming.EndOfTargetActivation:StatusExpiryTiming.EndOfTargetTurn;triggers=legacyTurns; }

            status.ExpiryTiming=timing;
            status.RemainingExpiryTriggers=Mathf.Max(0,triggers);
            // Keep legacy counters as readable mirrors for old save/debug tooling; they are no longer globally decremented.
            status.RemainingTurns=(timing==StatusExpiryTiming.EndOfTargetTurn||timing==StatusExpiryTiming.StartOfTargetTurn||timing==StatusExpiryTiming.EndOfSourceTurn||timing==StatusExpiryTiming.StartOfSourceTurn||timing==StatusExpiryTiming.EndOfTargetActivation)?status.RemainingExpiryTriggers:0;
            status.RemainingRounds=timing==StatusExpiryTiming.EndOfRound?status.RemainingExpiryTriggers:0;
        }

        private static string FindStatusDurationClause(string text,string statusName)
        {
            if(string.IsNullOrWhiteSpace(text)||string.IsNullOrWhiteSpace(statusName))return string.Empty;
            var clauses=Regex.Split(text,@"(?<=[.!?])\s+");
            for(int i=0;i<clauses.Length;i++)
            {
                var clause=clauses[i];
                if(clause.IndexOf(statusName,StringComparison.OrdinalIgnoreCase)<0)continue;
                if(clause.IndexOf("until",StringComparison.OrdinalIgnoreCase)>=0)return clause;
                if(i>0&&clauses[i-1].TrimStart().StartsWith("Until",StringComparison.OrdinalIgnoreCase))return clauses[i-1]+" "+clause;
            }
            return string.Empty;
        }

        private void AdvanceStatusClock(List<StatusInstance> statuses,StatusExpiryTiming timing,string ownerName,string sourceHeroId=null)
        {
            if(statuses==null||statuses.Count==0)return;
            foreach(var status in statuses.ToList())
            {
                if(status==null||status.ExpiryTiming!=timing)continue;
                if(!string.IsNullOrEmpty(sourceHeroId)&&!string.Equals(status.SourceHeroId,sourceHeroId,StringComparison.OrdinalIgnoreCase))continue;
                if(status.RemainingExpiryTriggers<=0)status.RemainingExpiryTriggers=1;
                status.RemainingExpiryTriggers--;
                status.RemainingTurns=(timing==StatusExpiryTiming.EndOfRound)?0:Mathf.Max(0,status.RemainingExpiryTriggers);
                status.RemainingRounds=(timing==StatusExpiryTiming.EndOfRound)?Mathf.Max(0,status.RemainingExpiryTriggers):0;
                if(status.RemainingExpiryTriggers<=0)
                {
                    statuses.Remove(status);
                    AddLog($"STATUS EXPIRES — {ownerName} loses {status.Name} ({timing}).");
                }
            }
        }

        private void AdvanceSourceStatusClock(HeroState sourceHero,StatusExpiryTiming timing)
        {
            if(sourceHero==null)return;
            foreach(var hero in ParticipatingHeroes)AdvanceStatusClock(hero.Statuses,timing,hero.DisplayName,sourceHero.Id);
            foreach(var enemy in Enemies.Values)AdvanceStatusClock(enemy.Statuses,timing,enemy.DisplayName,sourceHero.Id);
        }

        private void CompleteEnemyActivation(EnemyState enemy)
        {
            if(enemy==null)return;
            AdvanceStatusClock(enemy.Statuses,StatusExpiryTiming.EndOfTargetActivation,enemy.DisplayName);
            enemy.ActivatedThisRound=true;OnQuestEnemyActivationCompleted0832(enemy);OnEnemyActivationCompleted0859(enemy);
        }

        private StatusInstance RestoreStatusFromSave(StatusSaveData saved,bool enemyTarget)
        {
            if(saved==null)return null;
            var status=new StatusInstance
            {
                Id=saved.Id,Name=saved.Name,RemainingTurns=saved.RemainingTurns,RemainingRounds=saved.RemainingRounds,
                Source=saved.Source,Beneficial=saved.Beneficial,ExpiryTiming=saved.ExpiryTiming,
                RemainingExpiryTriggers=saved.RemainingExpiryTriggers,SourceHeroId=saved.SourceHeroId,AppliedRound=saved.AppliedRound
            };
            if(status.RemainingExpiryTriggers<=0&&(saved.RemainingTurns>0||saved.RemainingRounds>0))
            {
                var definition=HeroDatabase.GetStatus(status.Name);
                string duration=(definition?.duration??string.Empty).ToLowerInvariant();
                if(duration.Contains("until removed")||duration.Contains("until triggered"))
                { status.ExpiryTiming=StatusExpiryTiming.Persistent;status.RemainingExpiryTriggers=0; }
                else if(saved.RemainingRounds>0)
                { status.ExpiryTiming=StatusExpiryTiming.EndOfRound;status.RemainingExpiryTriggers=saved.RemainingRounds; }
                else
                { status.ExpiryTiming=enemyTarget?StatusExpiryTiming.EndOfTargetActivation:StatusExpiryTiming.EndOfTargetTurn;status.RemainingExpiryTriggers=saved.RemainingTurns; }
            }
            return status;
        }

#if UNITY_EDITOR
        public void DebugAdvanceHeroTurnStartStatusClock(string heroId)
        {
            if(!Heroes.TryGetValue(heroId,out var hero))return;
            AdvanceStatusClock(hero.Statuses,StatusExpiryTiming.StartOfTargetTurn,hero.DisplayName);
            AdvanceSourceStatusClock(hero,StatusExpiryTiming.StartOfSourceTurn);
            StateChanged?.Invoke();
        }
        public void DebugAdvanceHeroTurnEndStatusClock(string heroId)
        {
            if(!Heroes.TryGetValue(heroId,out var hero))return;
            AdvanceStatusClock(hero.Statuses,StatusExpiryTiming.EndOfTargetTurn,hero.DisplayName);
            AdvanceStatusClock(hero.Statuses,StatusExpiryTiming.EndOfTargetActivation,hero.DisplayName);
            AdvanceSourceStatusClock(hero,StatusExpiryTiming.EndOfSourceTurn);
            StateChanged?.Invoke();
        }
        public void DebugAdvanceEnemyActivationStatusClock(string enemyId)
        {
            if(Enemies.TryGetValue(enemyId,out var enemy))CompleteEnemyActivation(enemy);
            StateChanged?.Invoke();
        }
        public void DebugAdvanceEndRoundStatusClock()
        {
            foreach(var hero in ParticipatingHeroes)AdvanceStatusClock(hero.Statuses,StatusExpiryTiming.EndOfRound,hero.DisplayName);
            foreach(var enemy in Enemies.Values)AdvanceStatusClock(enemy.Statuses,StatusExpiryTiming.EndOfRound,enemy.DisplayName);
            StateChanged?.Invoke();
        }
#endif

        private IEnumerator ResolveEndRoundStatuses()
        {
            foreach (var hero in ParticipatingHeroes.ToList())
            {
                if (hero.Dead) continue;
                AdvanceStatusClock(hero.Statuses, StatusExpiryTiming.EndOfRound, hero.DisplayName);
            }
            foreach (var enemy in Enemies.Values)
            {
                if (enemy.Defeated) continue;
                AdvanceStatusClock(enemy.Statuses, StatusExpiryTiming.EndOfRound, enemy.DisplayName);
            }
            yield break;
        }

        public bool PlacePiece(string type,string owner,string location,int health,int maxPerOwner)
        {
            int count=HeroCreatedPieces.Count(p=>p.Type==type&&p.OwnerHeroId==owner);if(count>=maxPerOwner){AddLog($"{type}: maximum {maxPerOwner} already in play.");return false;}var p=new HeroCreatedPiece{PieceId=$"{type.Replace(" ","")}_{owner}_{Guid.NewGuid().ToString("N").Substring(0,6)}",Type=type,OwnerHeroId=owner,LocationId=location,Health=health};HeroCreatedPieces.Add(p);HeroPieceAvatar.Create(p);AddLog($"PIECE — {owner} places {type} at {Locations[location].Name}.");return true;
        }
        private bool MoveNearestPiece(string type,string owner,string destination,int maxConnections,string source)
        {
            var piece=HeroCreatedPieces.Where(p=>p.Type==type&&p.OwnerHeroId==owner&&p.LocationId!=destination&&GraphDistance(p.LocationId,destination)<=maxConnections).OrderBy(p=>GraphDistance(p.LocationId,destination)).FirstOrDefault();
            if(piece==null)return false;string from=piece.LocationId;piece.LocationId=destination;var avatar=FindObjectsByType<HeroPieceAvatar>().FirstOrDefault(a=>a.PieceId==piece.PieceId);if(avatar!=null&&Locations.TryGetValue(destination,out var loc))avatar.transform.position=loc.Position+Vector3.up*0.25f;AddLog($"PIECE — {source}: {owner}'s {type} moves from {Locations[from].Name} to {Locations[destination].Name}.");return true;
        }
        public bool RemovePiece(string type,string owner,string location){var p=HeroCreatedPieces.FirstOrDefault(x=>x.Type==type&&x.OwnerHeroId==owner&&(location==null||x.LocationId==location));if(p==null)return false;HeroCreatedPieces.Remove(p);var avatar=FindObjectsByType<HeroPieceAvatar>().FirstOrDefault(a=>a.PieceId==p.PieceId);if(avatar!=null)Destroy(avatar.gameObject);AddLog($"PIECE — {owner}'s {type} is removed.");OnHeroPieceRemoved0860(p);return true;}
        public bool HasPiece(string type,string owner,string location)=>HeroCreatedPieces.Any(p=>p.Type==type&&(owner==null||p.OwnerHeroId==owner)&&(location==null||p.LocationId==location));

        public void DealDirectDamage(EnemyState enemy,int amount,string source,HeroState sourceHero=null,CardData card=null)
        {
            if(enemy==null||enemy.Defeated||amount<=0)return;
            if(ShouldPreventQuestBossDamage0832(enemy)){AddLog($"Q012 BANNERS OF THE HORDE — {source} cannot reduce the Goblin King's Health while 2 or more Banners remain.");return;}
            int damage=ModifyEnemyIncomingDamage0859(enemy,amount,sourceHero,card,false,source);
            int before=enemy.Health;enemy.Health=ApplyEnemyHealthFloor0859(enemy,Mathf.Max(0,enemy.Health-damage));damage=Mathf.Max(0,before-enemy.Health);
            OnQuestEnemyDamaged0832(enemy);OnEnemyDamaged0859(enemy,damage,sourceHero,card,false);AddLog($"DAMAGE — {enemy.DisplayName} suffers {damage} damage ({source}).");
            if(enemy.Defeated)
            {
                if(sourceHero!=null){AddLog($"{enemy.DisplayName} is Defeated by {sourceHero.DisplayName} ({source}).");HeroRulesRuntime.OnEnemyDefeated(this,sourceHero,enemy,card);foreach(var mordren in ParticipatingHeroes.Where(h=>h.Id=="Mordren"&&!h.IsDefeated&&h.LocationId==enemy.LocationId&&!h.UsedRoundAbilities.Contains("RaiseServant"))){mordren.UsedRoundAbilities.Add("RaiseServant");PlacePiece("Bone Servant",mordren.Id,enemy.LocationId,1,MaxBoneServants(mordren));}PrepareHeroKillReactionWindow(sourceHero,enemy);}
                OnEnemyDefeated0859(enemy,sourceHero);if(EnemyAvatars.TryGetValue(enemy.Id,out var a))a.SetDefeated(damage);if(SelectedEnemyId==enemy.Id)SelectedEnemyId=FirstLivingEnemyAtSelectedHeroLocation()?.Id;CheckEncounterCompletionForEnemy(enemy);OnQuestEnemyDefeated0829(enemy,sourceHero);CheckVictory();
            }
            else if(EnemyAvatars.TryGetValue(enemy.Id,out var av))av.RefreshVisual(damage);
        }

        private int EffectiveHeroAttack(HeroState hero)
        {
            int value=hero.Attack;if(hero.HasStatus("Inspired"))value+=1;if(hero.HasStatus("Weakened"))value-=1;value+=HeroStatStatusModifier0860(hero,"Attack");return Mathf.Max(0,value);
        }
        private int EffectiveHeroArmor(HeroState hero)
        {
            int value=hero.Armor;if(hero.HasStatus("Guarded"))value+=1;if(hero.HasStatus("Vulnerable"))value-=1;if(hero.HasStatus("Prepared Position"))value+=1;value+=HeroStatStatusModifier0860(hero,"Armor")+EquipmentArmorModifier0854(hero)+LootPassiveArmor0854(hero)+LocationHeroArmorModifier0858(hero)+EnemyHeroArmorModifier0859(hero);return Mathf.Max(0,value);
        }
        private int EffectiveEnemyArmor(EnemyState enemy)
        {
            int value=enemy.Armor;if(enemy.Statuses.Any(s=>s.Name=="Vulnerable"))value-=1;if(enemy.Statuses.Any(s=>s.Name=="Guarded"))value+=1;
            value+=QuestEnemyArmorBonus0832(enemy)+EnemyArmorModifier0859(enemy);
            int location=LocationEnemyArmorModifier(enemy)+LocationEnemyArmorModifier0858(enemy);if(location!=0)value+=location;
            return Mathf.Max(0,value);
        }
        private int EffectiveEnemyAttack(EnemyState enemy)
        {
            int value=enemy.Attack;if(enemy.Statuses.Any(s=>s.Name=="Weakened"))value-=1;
            value+=QuestEnemyAttackBonus0831(enemy)+LocationEnemyAttackModifier0858(enemy)+EnemyAttackModifier0859(enemy,null);
            value+=enemy.Statuses.Count(s=>string.Equals(s.Name,"Encounter Attack +1",StringComparison.OrdinalIgnoreCase));
            if(enemy!=null&&IsGoblinEnemy(enemy)&&Locations.TryGetValue(enemy.LocationId??string.Empty,out var loc)&&loc.OngoingEffect?.IndexOf("Goblins here gain +1 attack",StringComparison.OrdinalIgnoreCase)>=0)value+=1;
            return Mathf.Max(0,value);
        }

        private void CheckVictory()=>EvaluateQuestOutcome0829();
        public EnemyState FirstLivingEnemyAtSelectedHeroLocation(){var h=SelectedHero;return h==null?null:Enemies.Values.FirstOrDefault(e=>!e.Defeated&&e.LocationId==h.LocationId);}
        public void RegisterHeroAvatar(HeroAvatar avatar){HeroAvatars[avatar.HeroId]=avatar;if(avatar.HeroId==SelectedHeroId)Camera.main?.GetComponent<IsometricCameraController>()?.FocusOn(avatar.transform.position);}
        public void RegisterEnemyAvatar(EnemyAvatar avatar)=>EnemyAvatars[avatar.EnemyId]=avatar;
        public void AddLog(string message){log.Add(message);if(log.Count>180)log.RemoveAt(0);Debug.Log("[ASCENDANCE] "+message);LogAdded?.Invoke(message);}
    }
}
