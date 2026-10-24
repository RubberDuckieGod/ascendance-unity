using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Ascendance
{
    public sealed class HeroAvatar : MonoBehaviour
    {
        public string HeroId { get; private set; }
        public string DisplayName => AscendanceGameManager.Instance != null && AscendanceGameManager.Instance.Heroes.TryGetValue(HeroId, out var hero) ? hero.DisplayName : HeroId;

        private Color baseColor;
        private WorldRing selectionRing;
        private WorldRing hoverRing;
        private WorldHealthBar healthBar;
        private NavMeshAgent agent;
        private CharacterPresentationController presentation;
        private Coroutine fallbackMotion;
        private bool hovered;
        private Vector3 lastFallbackPosition;
        private float fallbackSpeed;

        public void Initialize(string heroId, Color color, CharacterPresentationController characterPresentation, NavMeshAgent navAgent)
        {
            HeroId = heroId;
            presentation = characterPresentation;
            agent = navAgent;
            baseColor = color;

            // Register the gameplay avatar before creating optional cosmetics. In a player build,
            // a missing/stripped presentation shader must never prevent a Hero from existing.
            AscendanceGameManager.Instance.RegisterHeroAvatar(this);

            try
            {
                selectionRing = WorldRing.Create(transform, "Selected Hero Ring", heroId == "Garruk" ? 1.08f : 0.94f, new Color(0.16f, 0.72f, 1f, 0.95f), 0.10f);
                hoverRing = WorldRing.Create(transform, "Hero Hover Ring", heroId == "Garruk" ? 1.23f : 1.09f, new Color(0.95f, 0.82f, 0.38f, 0.85f), 0.055f);
                hoverRing.SetVisible(false);
                healthBar = WorldHealthBar.Create(transform, presentation != null ? presentation.Height + 0.38f : 2.75f, 1.45f, new Color(0.20f, 0.76f, 0.29f));
            }
            catch (Exception cosmeticException)
            {
                Debug.LogWarning($"[ASCENDANCE] Hero cosmetics unavailable for {heroId}; gameplay avatar remains active. {cosmeticException.Message}");
            }

            var collider = GetComponent<CapsuleCollider>();
            if (collider == null) collider = gameObject.AddComponent<CapsuleCollider>();
            float visualHeight = presentation != null ? presentation.Height : (heroId == "Garruk" ? 2.65f : 2.4f);
            collider.center = new Vector3(0f, visualHeight * 0.46f, 0f);
            collider.height = visualHeight;
            collider.radius = heroId == "Garruk" ? 0.55f : 0.46f;

            SnapAgentToNavMesh();
            lastFallbackPosition = transform.position;
        }

        private void SnapAgentToNavMesh()
        {
            if (agent == null) return;
            if (NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            hoverRing?.SetVisible(value);
        }

        public void MoveTo(Vector3 destination, Action onArrived)
        {
            destination += FormationOffset(HeroId);
            if (fallbackMotion != null) StopCoroutine(fallbackMotion);
            if (agent != null && agent.isOnNavMesh && NavMesh.SamplePosition(destination, out var hit, 4f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                StartCoroutine(WaitForAgent(onArrived));
            }
            else
            {
                fallbackMotion = StartCoroutine(FallbackWalk(destination, onArrived));
            }
        }

        private IEnumerator WaitForAgent(Action onArrived)
        {
            while (agent != null && agent.enabled)
            {
                if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance + 0.06f, 0.12f))
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.02f) break;
                }
                yield return null;
            }
            onArrived?.Invoke();
        }

        private IEnumerator FallbackWalk(Vector3 destination, Action onArrived)
        {
            destination.y = transform.position.y;
            lastFallbackPosition = transform.position;
            while (Vector3.Distance(transform.position, destination) > 0.08f)
            {
                Vector3 delta = destination - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), 1f - Mathf.Exp(-14f * Time.deltaTime));
                Vector3 before = transform.position;
                transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * 5.2f);
                fallbackSpeed = Time.deltaTime > 0f ? Vector3.Distance(before, transform.position) / Time.deltaTime : 0f;
                yield return null;
            }
            transform.position = destination;
            fallbackSpeed = 0f;
            fallbackMotion = null;
            onArrived?.Invoke();
        }

        public void PlayAttack(Vector3 target, Action onImpact, string attackStyle = "Melee", Action onFinished = null)
        {
            FaceTarget(target);
            presentation?.PlayAttack(attackStyle, () =>
            {
                WorldCombatFeedback.SpawnHitFlash(Vector3.Lerp(transform.position, target, 0.72f), new Color(1f, 0.68f, 0.18f));
                onImpact?.Invoke();
            }, onFinished);
            if (presentation == null)
            {
                onImpact?.Invoke();
                onFinished?.Invoke();
            }
        }

        public void ShowHit(int damage)
        {
            if (damage <= 0) return;
            presentation?.PlayHit();
            WorldCombatFeedback.SpawnDamage(transform.position, damage);
            WorldCombatFeedback.SpawnHitFlash(transform.position, new Color(1f, 0.16f, 0.08f));
        }

        public void ShowDowned()
        {
            presentation?.PlayDeath();
            healthBar?.SetVisible(false);
            selectionRing?.SetVisible(false);
            hoverRing?.SetVisible(false);
        }

        public void ReviveVisual()
        {
            presentation?.Revive();
            healthBar?.SetVisible(true);
            selectionRing?.SetVisible(false);
            hoverRing?.SetVisible(false);
        }

        private static Vector3 FormationOffset(string heroId)
        {
            int index = 0;
            var gm = AscendanceGameManager.Instance;
            if (gm != null && gm.Heroes.TryGetValue(heroId, out var hero) && hero.PartyIndex >= 0) index = hero.PartyIndex;
            Vector3[] offsets = { new(-1.35f,0f,-1.0f), new(-0.45f,0f,-1.25f), new(0.45f,0f,-1.0f), new(1.35f,0f,-1.25f) };
            return offsets[Mathf.Clamp(index,0,offsets.Length-1)];
        }

        public void FaceTarget(Vector3 target)
        {
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private void Update()
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null || !gm.Heroes.TryGetValue(HeroId, out var hero)) return;

            bool selected = gm.SelectedHeroId == HeroId;
            selectionRing?.SetVisible(selected && !hero.IsDefeated);
            hoverRing?.SetVisible(hovered && !hero.IsDefeated);
            healthBar?.SetFraction(hero.MaxHealth > 0 ? (float)hero.Health / hero.MaxHealth : 0f);
            healthBar?.SetVisible(!hero.IsDefeated);

            float speed = agent != null && agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : fallbackSpeed;
            presentation?.Tick(speed);
            if (presentation != null)
            {
                if (selected) presentation.SetHighlight(baseColor, 0.22f);
                else if (hovered) presentation.SetHighlight(new Color(1f, 0.72f, 0.20f), 0.16f);
                else presentation.SetHighlight(Color.black, 0f);
            }
        }
    }
}
