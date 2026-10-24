using System;
using UnityEngine;

namespace Ascendance
{
    public sealed class EnemyAvatar : MonoBehaviour
    {
        public string EnemyId { get; private set; }
        private WorldRing targetRing;
        private WorldRing hoverRing;
        private WorldHealthBar healthBar;
        private CharacterPresentationController presentation;
        private bool hovered;
        private bool defeatedVisual;

        public void Initialize(string enemyId, bool boss, CharacterPresentationController characterPresentation)
        {
            EnemyId = enemyId;
            presentation = characterPresentation;

            // Register first: world-space UI is presentation only and must never suppress an Enemy
            // in standalone builds if Unity strips a cosmetic shader.
            AscendanceGameManager.Instance.RegisterEnemyAvatar(this);

            try
            {
                targetRing = WorldRing.Create(transform, "Enemy Target Ring", boss ? 1.22f : 0.88f, new Color(0.95f, 0.18f, 0.12f, 0.95f), 0.10f);
                hoverRing = WorldRing.Create(transform, "Enemy Hover Ring", boss ? 1.40f : 1.04f, new Color(1f, 0.62f, 0.16f, 0.9f), 0.055f);
                targetRing.SetVisible(false);
                hoverRing.SetVisible(false);
                healthBar = WorldHealthBar.Create(transform, presentation != null ? presentation.Height + 0.30f : 2.2f, boss ? 1.62f : 1.18f, boss ? new Color(0.78f, 0.12f, 0.08f) : new Color(0.88f, 0.23f, 0.12f));
            }
            catch (Exception cosmeticException)
            {
                Debug.LogWarning($"[ASCENDANCE] Enemy cosmetics unavailable for {enemyId}; gameplay avatar remains active. {cosmeticException.Message}");
            }

            var collider = GetComponent<CapsuleCollider>();
            if (collider == null) collider = gameObject.AddComponent<CapsuleCollider>();
            float visualHeight = presentation != null ? presentation.Height : (boss ? 2.3f : 1.65f);
            collider.center = new Vector3(0f, visualHeight * 0.46f, 0f);
            collider.height = visualHeight;
            collider.radius = boss ? 0.58f : 0.40f;

        }

        public void SetHovered(bool value)
        {
            hovered = value;
            hoverRing?.SetVisible(value && !defeatedVisual);
        }

        private void Update()
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null || !gm.Enemies.TryGetValue(EnemyId, out var enemy)) return;
            bool targeted = gm.SelectedEnemyId == EnemyId;
            targetRing?.SetVisible(targeted && !enemy.Defeated);
            hoverRing?.SetVisible(hovered && !enemy.Defeated);
            healthBar?.SetFraction(enemy.MaxHealth > 0 ? (float)enemy.Health / enemy.MaxHealth : 0f);
            healthBar?.SetVisible(!enemy.Defeated);
            presentation?.Tick(0f);

            if (presentation != null && !enemy.Defeated)
            {
                if (targeted) presentation.SetHighlight(new Color(1f, 0.12f, 0.06f), 0.22f);
                else if (hovered) presentation.SetHighlight(new Color(1f, 0.55f, 0.10f), 0.14f);
                else presentation.SetHighlight(Color.black, 0f);
            }
        }

        public void PlayAttack(Vector3 target, Action onImpact, string attackStyle = "Melee", Action onFinished = null)
        {
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            presentation?.PlayAttack(attackStyle, () =>
            {
                WorldCombatFeedback.SpawnHitFlash(Vector3.Lerp(transform.position, target, 0.72f), new Color(0.90f, 0.18f, 0.06f));
                onImpact?.Invoke();
            }, onFinished);
            if (presentation == null)
            {
                onImpact?.Invoke();
                onFinished?.Invoke();
            }
        }

        public void SetDefeated(int finalDamage = 0)
        {
            if (defeatedVisual) return;
            defeatedVisual = true;
            targetRing?.SetVisible(false);
            hoverRing?.SetVisible(false);
            healthBar?.SetVisible(false);
            if (finalDamage > 0) WorldCombatFeedback.SpawnDamage(transform.position, finalDamage);
            presentation?.PlayDeath();
        }

        public void RefreshVisual(int damage)
        {
            if (defeatedVisual) return;
            if (damage > 0) WorldCombatFeedback.SpawnDamage(transform.position, damage);
            WorldCombatFeedback.SpawnHitFlash(transform.position, new Color(1f, 0.16f, 0.05f));
            presentation?.PlayHit();
        }
    }
}
