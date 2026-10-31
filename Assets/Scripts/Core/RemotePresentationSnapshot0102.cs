using System;
using System.Collections.Generic;
using System.Linq;
using Ascendance.Online;
using UnityEngine;
using UnityEngine.AI;

namespace Ascendance
{
    public sealed partial class AscendanceGameManager
    {
        private bool applyingRemoteSnapshot0102;
        private ServerTransientState0100 remotePresentation0102;
        private readonly List<RewardChoiceOption> remoteRewardOptions0102 = new();
        private readonly List<EncounterDeckControlOption> remoteDeckOptions0102 = new();

        public void EnterRemotePresentation0102()
        {
            // Local rules coroutines must not wake after a transport switch into a replica.
            StopAllCoroutines();
            foreach (var avatar in HeroAvatars.Values)
                if (avatar != null)
                {
                    avatar.StopAllCoroutines();
                    var agent = avatar.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
                    foreach (var presentation in avatar.GetComponentsInChildren<CharacterPresentationController>(true)) presentation.CancelGameplayCallbacks0102();
                }
            foreach (var avatar in EnemyAvatars.Values)
                if (avatar != null)
                    foreach (var presentation in avatar.GetComponentsInChildren<CharacterPresentationController>(true)) presentation.CancelGameplayCallbacks0102();
            ClearRemotePresentation0102();
            CloseReactionWindow();
            queuedDamageContinuation = null;
            queuedDamageTarget = null;
        }

        public void ClearRemotePresentation0102()
        {
            remotePresentation0102 = null;
            remoteRewardOptions0102.Clear();
            remoteDeckOptions0102.Clear();
        }

        /// <summary>Installs server state for presentation. Never resumes a rule, attack, or damage callback.</summary>
        public void ApplyAuthoritativeSnapshot0102(ServerStateSnapshot0100 snapshot)
        {
            if (snapshot?.Campaign == null || snapshot.Transient == null) return;
            var campaign = AscendanceSaveSystem.CloneForAuthority0100(snapshot.Campaign);
            var transient = JsonUtility.FromJson<ServerTransientState0100>(JsonUtility.ToJson(snapshot.Transient));
            string localHero = SelectedHeroId, localEnemy = SelectedEnemyId;
            var oldHeroLocations = Heroes.Values.ToDictionary(h => h.Id, h => h.LocationId, StringComparer.OrdinalIgnoreCase);
            var oldMovingHeroes = new HashSet<string>(Heroes.Values.Where(h => h.Moving).Select(h => h.Id), StringComparer.OrdinalIgnoreCase);
            var oldHeroDefeat = Heroes.Values.ToDictionary(h => h.Id, h => h.IsDefeated, StringComparer.OrdinalIgnoreCase);
            var oldEnemyDefeat = Enemies.Values.ToDictionary(e => e.Id, e => e.Defeated, StringComparer.OrdinalIgnoreCase);
            ClearRemotePresentation0102();
            applyingRemoteSnapshot0102 = true;
            try
            {
                ApplySaveData(campaign);
                // Save loading repairs legacy decks and action counts; a live replica uses the exact
                // authoritative collections, including cards temporarily held by modal choices.
                ReplaceRemoteDeck0102(SearchDeck, campaign.SearchDeckIds);
                ReplaceRemoteDeck0102(SearchDiscard, campaign.SearchDiscardIds);
                ReplaceRemoteDeck0102(EncounterDeck, campaign.EncounterDeckIds);
                ReplaceRemoteDeck0102(EncounterDiscard, campaign.EncounterDiscardIds);
                ReplaceRemoteDeck0102(LootDeck, campaign.LootDeckIds);
                ReplaceRemoteDeck0102(LootDiscard, campaign.LootDiscardIds);
                ReplaceRemoteDeck0102(EquipmentDeck, campaign.EquipmentDeckIds);
                ReplaceRemoteDeck0102(EquipmentDiscard, campaign.EquipmentDiscardIds);
                ReplaceRemoteDeck0102(ExplorationDeck0828, campaign.ExplorationDeckIds);
                foreach (var stored in campaign.Heroes ?? new List<HeroSaveData>())
                    if (stored != null && Heroes.TryGetValue(stored.Id, out var hero))
                    {
                        hero.Actions = stored.Actions;
                        hero.AttackOnlyActions = stored.AttackOnlyActions;
                    }

                TurnHeroId = !string.IsNullOrEmpty(transient.TurnHeroId) && Heroes.TryGetValue(transient.TurnHeroId, out var turn) && turn.Participating ? turn.Id : null;
                IsResolving = transient.IsResolving;
                pendingReactionKind = transient.HasPendingReactionWindow && Enum.TryParse(transient.PendingReactionKind, out ReactionWindowKind kind) ? kind : ReactionWindowKind.None;
                pendingDamageContinuation = null;
                queuedDamageContinuation = null;
                queuedDamageTarget = null;
                queuedDamageAmount = 0;
                queuedDamageSource = null;
                queuedDamageSourceEnemy = null;
                PendingReactionOptions.Clear();
                if (transient.HasPendingReactionWindow)
                    foreach (string encoded in transient.ReactionOptions ?? new List<string>())
                    {
                        string[] parts = (encoded ?? string.Empty).Split(new[] { '|' }, 3);
                        if (parts.Length == 3) PendingReactionOptions.Add(new ReactionOption { HeroId = parts[0], CardId = parts[1], Label = parts[2] });
                    }
                foreach (string encoded in transient.RewardOptions ?? new List<string>())
                {
                    string[] parts = (encoded ?? string.Empty).Split(new[] { '|' }, 2);
                    if (parts.Length == 2) remoteRewardOptions0102.Add(new RewardChoiceOption { Key = parts[0], Label = parts[1] });
                }
                foreach (string encoded in transient.EncounterDeckControlOptions ?? new List<string>())
                {
                    string[] parts = (encoded ?? string.Empty).Split(new[] { '|' }, 2);
                    if (parts.Length == 2) remoteDeckOptions0102.Add(new EncounterDeckControlOption { Key = parts[0], Label = parts[1] });
                }
                var moving = new HashSet<string>(transient.MovingHeroIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                foreach (var hero in Heroes.Values)
                {
                    hero.Moving = hero.Participating && moving.Contains(hero.Id);
                    hero.PendingLocationId = hero.Moving ? transient.HeroMovement?.FirstOrDefault(m => string.Equals(m.HeroId, hero.Id, StringComparison.OrdinalIgnoreCase))?.PendingLocationId : null;
                }
                remotePresentation0102 = transient;
                SetAuthorityCommandContext0100(localHero, localEnemy);
                ReconcileRemoteAvatars0102(oldHeroLocations, oldMovingHeroes, oldHeroDefeat, oldEnemyDefeat);
                log.Clear();
                log.AddRange(transient.RecentLog ?? new List<string>());
            }
            finally { applyingRemoteSnapshot0102 = false; }
            // Subscribers see the complete revision, never ApplySaveData's temporary cleared windows.
            StateChanged?.Invoke();
        }

        private static void ReplaceRemoteDeck0102(List<string> destination, List<string> source)
        {
            destination.Clear();
            if (source != null) destination.AddRange(source);
        }

        private void ReconcileRemoteAvatars0102(Dictionary<string, string> oldLocations, HashSet<string> oldMovingHeroes,
            Dictionary<string, bool> oldHeroDefeat, Dictionary<string, bool> oldEnemyDefeat)
        {
            if (!ReferenceEquals(Instance, this)) return;
            var builder = GetComponent<StonebrookWorldBuilder>();
            if (builder == null || !builder.PresentationSpawnReady0862) return;
            // A restored living enemy needs a fresh visual if its previous visual had died.
            foreach (var enemy in Enemies.Values)
                if (!enemy.Defeated && oldEnemyDefeat.TryGetValue(enemy.Id, out bool dead) && dead && EnemyAvatars.TryGetValue(enemy.Id, out var stale))
                {
                    if (stale != null) Destroy(stale.gameObject);
                    EnemyAvatars.Remove(enemy.Id);
                }
            bool changed = HeroAvatars.Keys.Any(id => !Heroes.TryGetValue(id, out var h) || !h.Participating)
                || ParticipatingHeroes.Any(h => !HeroAvatars.ContainsKey(h.Id))
                || EnemyAvatars.Keys.Any(id => !Enemies.ContainsKey(id))
                || Enemies.Keys.Any(id => !EnemyAvatars.ContainsKey(id));
            if (changed) builder.EnsureRuntimeEnemySpawns();
            foreach (var hero in ParticipatingHeroes)
            {
                if (!HeroAvatars.TryGetValue(hero.Id, out var avatar) || avatar == null) continue;
                if (hero.Moving && Locations.TryGetValue(hero.PendingLocationId ?? string.Empty, out var target))
                    avatar.MoveTo(target.Position, null);
                else if (Locations.TryGetValue(hero.LocationId, out var location) && (oldMovingHeroes.Contains(hero.Id) || !oldLocations.TryGetValue(hero.Id, out var previous) || previous != hero.LocationId))
                {
                    avatar.StopAllCoroutines();
                    Vector3[] offsets = { new(-1.35f, 0f, -1f), new(-0.45f, 0f, -1.25f), new(0.45f, 0f, -1f), new(1.35f, 0f, -1.25f) };
                    Vector3 position = location.Position + offsets[Mathf.Clamp(hero.PartyIndex, 0, offsets.Length - 1)];
                    var agent = avatar.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.Warp(position); }
                    else avatar.transform.position = position;
                }
                if (hero.IsDefeated) avatar.ShowDowned();
                else if (oldHeroDefeat.TryGetValue(hero.Id, out bool wasDead) && wasDead) avatar.ReviveVisual();
            }
            foreach (var enemy in Enemies.Values)
                if (EnemyAvatars.TryGetValue(enemy.Id, out var avatar) && avatar != null)
                {
                    if (Locations.TryGetValue(enemy.LocationId, out var location)) avatar.transform.position = location.Position;
                    if (enemy.Defeated) avatar.SetDefeated();
                }
            foreach (var piece in FindObjectsByType<HeroPieceAvatar>(FindObjectsSortMode.None))
                if (!HeroCreatedPieces.Any(p => p.PieceId == piece.PieceId)) Destroy(piece.gameObject);
            foreach (var piece in HeroCreatedPieces)
                if (!FindObjectsByType<HeroPieceAvatar>(FindObjectsSortMode.None).Any(p => p.PieceId == piece.PieceId)) HeroPieceAvatar.Create(piece);
            foreach (var avatar in FindObjectsByType<EncounterObjectAvatar>(FindObjectsSortMode.None))
                if (!EncounterObjects.Any(o => o.ObjectId == avatar.ObjectId)) Destroy(avatar.gameObject);
            foreach (var obj in EncounterObjects) EncounterObjectAvatar.Create(obj);
            foreach (var obj in QuestObjects0829) if (!obj.Rescued && !obj.Destroyed) QuestObjectAvatar0829.Create(obj);
        }
    }
}
