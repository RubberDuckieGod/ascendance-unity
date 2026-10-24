using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Production-ready presentation facade. It automatically uses imported Resources prefabs when
    /// present and otherwise keeps the 0.3.x procedural visual rig. Gameplay talks only to this class,
    /// so swapping art does not require changing Ascendance rules code.
    /// </summary>
    public sealed class CharacterPresentationController : MonoBehaviour
    {
        private CharacterVisualRig fallbackRig;
        private GameObject productionModel;
        private AnimatorParameterDriver animatorDriver;
        private EquipmentSocketRig sockets;
        private CharacterAnimationProfile animationProfile;
        private readonly List<Renderer> productionRenderers = new();
        private MaterialPropertyBlock propertyBlock;
        private Coroutine attackFallbackRoutine;
        private Action pendingImpact;
        private Action pendingFinished;
        private bool impactConsumed;
        private bool attackFinished;
        private int attackGeneration;
        private bool dead;

        public bool IsAttacking => !attackFinished && attackFallbackRoutine != null;

        public float Height { get; private set; } = 2.4f;
        public bool UsingProductionPrefab => productionModel != null;
        public EquipmentSocketRig Sockets => sockets;

        private void Awake()
        {
            // Unity 6.6 forbids native-backed Unity object creation from MonoBehaviour
            // constructors/field initializers. Create the property block during Awake instead.
            propertyBlock = new MaterialPropertyBlock();
        }

        public static CharacterPresentationController BuildHero(Transform parent, string heroId, Color primary)
        {
            var host = new GameObject("Character Presentation");
            host.transform.SetParent(parent, false);
            var controller = host.AddComponent<CharacterPresentationController>();
            controller.InitializeHero(heroId, primary);
            return controller;
        }

        public static CharacterPresentationController BuildEnemy(Transform parent, string enemyId, bool boss)
        {
            var host = new GameObject("Character Presentation");
            host.transform.SetParent(parent, false);
            var controller = host.AddComponent<CharacterPresentationController>();
            controller.InitializeEnemy(enemyId, boss);
            return controller;
        }

        private void InitializeHero(string heroId, Color primary)
        {
            animationProfile = Resources.Load<CharacterAnimationProfile>("Ascendance/AnimationProfiles/" + heroId);
            var prefab = ProductionAssetResolver.LoadHeroPrefab(heroId);
            if (prefab != null) BuildProduction(prefab);
            else
            {
                fallbackRig = CharacterVisualRig.BuildHero(transform, heroId, primary);
                Height = fallbackRig.Height;
            }
        }

        private void InitializeEnemy(string enemyId, bool boss)
        {
            animationProfile = Resources.Load<CharacterAnimationProfile>("Ascendance/AnimationProfiles/" + enemyId);
            if (animationProfile == null) animationProfile = Resources.Load<CharacterAnimationProfile>("Ascendance/AnimationProfiles/" + (boss ? "GoblinCaptain" : "GoblinRaider"));
            var prefab = ProductionAssetResolver.LoadEnemyPrefab(enemyId, boss);
            if (prefab != null) BuildProduction(prefab);
            else
            {
                fallbackRig = CharacterVisualRig.BuildGoblin(transform, boss);
                Height = fallbackRig.Height;
            }
        }

        private void BuildProduction(GameObject prefab)
        {
            productionModel = Instantiate(prefab, transform);
            productionModel.name = prefab.name + " (Production Runtime)";
            productionModel.transform.localPosition = Vector3.zero;
            productionModel.transform.localRotation = Quaternion.identity;

            var animator = productionModel.GetComponentInChildren<Animator>(true);
            animatorDriver = new AnimatorParameterDriver(animator);

            sockets = productionModel.GetComponent<EquipmentSocketRig>();
            if (sockets == null) sockets = productionModel.AddComponent<EquipmentSocketRig>();
            sockets.Initialize(productionModel.transform);

            var relay = productionModel.GetComponent<AnimationImpactRelay>();
            if (relay == null) relay = productionModel.AddComponent<AnimationImpactRelay>();
            relay.Bind(this);

            productionRenderers.AddRange(productionModel.GetComponentsInChildren<Renderer>(true));
            Height = CalculateHeight(productionRenderers, 2.4f);
        }

        public void Tick(float locomotionSpeed)
        {
            if (fallbackRig != null) fallbackRig.Tick(locomotionSpeed);
            animatorDriver?.SetLocomotion(locomotionSpeed * (animationProfile != null ? animationProfile.SpeedBlendScale : 1f));
        }

        public void SetHighlight(Color color, float intensity)
        {
            if (fallbackRig != null)
            {
                fallbackRig.SetHighlight(color, intensity);
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            Color emission = color * Mathf.Max(0f, intensity * 2.4f);
            foreach (var renderer in productionRenderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", emission);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        public GameObject AttachEquipmentResource(string resourceName, string slot, bool clearExisting = false)
        {
            if (sockets == null || string.IsNullOrWhiteSpace(resourceName)) return null;
            var prefab = Resources.Load<GameObject>("Ascendance/Equipment/" + resourceName);
            return sockets.Attach(prefab, slot, clearExisting);
        }

        public void PlayAttack(string style, Action onImpact, Action onFinished = null)
        {
            if (dead)
            {
                onImpact?.Invoke();
                onFinished?.Invoke();
                return;
            }

            if (attackFallbackRoutine != null)
            {
                StopCoroutine(attackFallbackRoutine);
                if (!impactConsumed) NotifyAnimationImpact();
                CompleteAttack();
            }

            pendingImpact = onImpact;
            pendingFinished = onFinished;
            impactConsumed = false;
            attackFinished = false;
            int generation = ++attackGeneration;

            fallbackRig?.PlayAttack();
            animatorDriver?.TriggerAttack(style);
            attackFallbackRoutine = StartCoroutine(AttackTimingFallback(style, generation));
        }

        public void NotifyAnimationImpact()
        {
            if (impactConsumed) return;
            impactConsumed = true;
            pendingImpact?.Invoke();
            pendingImpact = null;
            CombatTimeEffects.Instance?.HitStop();
            Camera.main?.GetComponent<IsometricCameraController>()?.AddImpulse(0.16f, 0.13f);
        }

        public void NotifyAnimationAttackFinished()
        {
            if (!impactConsumed) NotifyAnimationImpact();
            CompleteAttack();
        }

        private void CompleteAttack()
        {
            if (attackFinished) return;
            attackFinished = true;
            var finished = pendingFinished;
            pendingFinished = null;
            pendingImpact = null;
            attackFallbackRoutine = null;
            finished?.Invoke();
        }

        private IEnumerator AttackTimingFallback(string style, int generation)
        {
            float impactTime = animationProfile != null ? animationProfile.ImpactFor(style) : ImpactDelay(style);
            float finishTime = Mathf.Max(impactTime + 0.20f, animationProfile != null ? animationProfile.AttackDuration : AttackDuration(style));
            float elapsed = 0f;

            while (elapsed < finishTime)
            {
                if (generation != attackGeneration) yield break;
                elapsed += Time.unscaledDeltaTime;
                if (!impactConsumed && elapsed >= impactTime) NotifyAnimationImpact();
                yield return null;
            }

            if (generation != attackGeneration) yield break;
            if (!impactConsumed) NotifyAnimationImpact();
            NotifyAnimationAttackFinished();
        }

        public void PlayHit()
        {
            if (dead) return;
            fallbackRig?.PlayHit();
            animatorDriver?.TriggerHit();
        }

        public void PlayDeath()
        {
            if (dead) return;
            dead = true;
            fallbackRig?.PlayDeath();
            animatorDriver?.TriggerDeath();
        }

        public void PlayRevive()
        {
            dead = false;
            fallbackRig?.Revive();
            animatorDriver?.TriggerRevive();
        }

        private static float ImpactDelay(string style)
        {
            return style?.ToLowerInvariant() switch
            {
                "heavy" => 0.36f,
                "ranged" => 0.40f,
                "spell" => 0.44f,
                "shield" => 0.30f,
                _ => 0.27f
            };
        }

        private static float AttackDuration(string style)
        {
            return style?.ToLowerInvariant() switch
            {
                "heavy" => 0.72f,
                "ranged" => 0.66f,
                "spell" => 0.78f,
                _ => 0.58f
            };
        }

        private static float CalculateHeight(List<Renderer> renderers, float fallback)
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return hasBounds ? Mathf.Clamp(bounds.size.y, 1.2f, 4.5f) : fallback;
        }
        public void Revive()
        {
            PlayRevive();
        }

    }
}
