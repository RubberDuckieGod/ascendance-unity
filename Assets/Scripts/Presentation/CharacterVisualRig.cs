using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Lightweight articulated character presentation built entirely from Unity primitives.
    /// This is intentionally asset-free so the vertical slice opens without third-party packages,
    /// while providing real humanoid silhouettes and animation hooks that can later drive imported rigs.
    /// </summary>
    public sealed class CharacterVisualRig : MonoBehaviour
    {
        private readonly List<Renderer> renderers = new();
        private readonly List<Color> baseColors = new();

        private Transform visualRoot;
        private Transform torso;
        private Transform head;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform weaponPivot;
        private Transform cape;
        private float gaitPhase;
        private bool dead;
        private Coroutine attackRoutine;
        private Coroutine hitRoutine;
        private Coroutine deathRoutine;

        public float Height { get; private set; } = 2.4f;

        public static CharacterVisualRig BuildHero(Transform parent, string heroId, Color primary)
        {
            var host = new GameObject("Character Visual");
            host.transform.SetParent(parent, false);
            var rig = host.AddComponent<CharacterVisualRig>();
            rig.visualRoot = host.transform;
            rig.BuildHeroParts(heroId, primary);
            return rig;
        }

        public static CharacterVisualRig BuildGoblin(Transform parent, bool boss)
        {
            var host = new GameObject("Goblin Visual");
            host.transform.SetParent(parent, false);
            var rig = host.AddComponent<CharacterVisualRig>();
            rig.visualRoot = host.transform;
            rig.BuildGoblinParts(boss);
            return rig;
        }

        private void BuildHeroParts(string heroId, Color primary)
        {
            Color metal = new Color(0.34f, 0.36f, 0.39f);
            Color leather = new Color(0.18f, 0.09f, 0.045f);
            Color skin = new Color(0.70f, 0.48f, 0.32f);
            Color cloth = primary;
            Color accent = Color.Lerp(primary, Color.white, 0.28f);

            float bulk = heroId == "Garruk" ? 1.16f : heroId == "Aldric" ? 1.08f : heroId == "Elara" ? 0.92f : 0.96f;
            Height = heroId == "Garruk" ? 2.65f : 2.42f;

            var pelvis = Part("Pelvis", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0.88f, 0f), new Vector3(0.62f * bulk, 0.36f, 0.34f * bulk), leather);
            torso = Part("Torso", PrimitiveType.Capsule, visualRoot, new Vector3(0f, 1.48f, 0f), new Vector3(0.72f * bulk, 0.72f, 0.55f * bulk), cloth);
            head = Part("Head", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 2.23f, 0.02f), new Vector3(0.40f, 0.46f, 0.40f), skin);
            Part("Left Eye", PrimitiveType.Sphere, visualRoot, new Vector3(-0.12f, 2.27f, 0.37f), new Vector3(0.055f, 0.042f, 0.035f), new Color(0.035f, 0.03f, 0.025f));
            Part("Right Eye", PrimitiveType.Sphere, visualRoot, new Vector3(0.12f, 2.27f, 0.37f), new Vector3(0.055f, 0.042f, 0.035f), new Color(0.035f, 0.03f, 0.025f));
            Part("Neck", PrimitiveType.Cylinder, visualRoot, new Vector3(0f, 1.98f, 0f), new Vector3(0.15f, 0.18f, 0.15f), skin);

            leftArm = Limb("Left Arm", visualRoot, new Vector3(-0.53f * bulk, 1.55f, 0f), new Vector3(0.20f, 0.64f, 0.20f), cloth);
            rightArm = Limb("Right Arm", visualRoot, new Vector3(0.53f * bulk, 1.55f, 0f), new Vector3(0.20f, 0.64f, 0.20f), cloth);
            leftLeg = Limb("Left Leg", visualRoot, new Vector3(-0.22f, 0.45f, 0f), new Vector3(0.23f, 0.55f, 0.23f), leather);
            rightLeg = Limb("Right Leg", visualRoot, new Vector3(0.22f, 0.45f, 0f), new Vector3(0.23f, 0.55f, 0.23f), leather);

            Part("Left Boot", PrimitiveType.Cube, leftLeg, new Vector3(0f, -0.56f, 0.09f), new Vector3(0.28f, 0.18f, 0.48f), leather);
            Part("Right Boot", PrimitiveType.Cube, rightLeg, new Vector3(0f, -0.56f, 0.09f), new Vector3(0.28f, 0.18f, 0.48f), leather);

            if (heroId == "Aldric")
            {
                Part("Knight Helm", PrimitiveType.Cylinder, head, new Vector3(0f, 0.20f, 0f), new Vector3(0.50f, 0.16f, 0.50f), metal);
                Part("Helm Guard", PrimitiveType.Cube, head, new Vector3(0f, -0.01f, 0.39f), new Vector3(0.08f, 0.34f, 0.06f), metal);
                Part("Blue Plume", PrimitiveType.Capsule, head, new Vector3(0f, 0.55f, -0.03f), new Vector3(0.13f, 0.30f, 0.13f), new Color(0.08f, 0.22f, 0.52f));
                Part("Breastplate", PrimitiveType.Cube, torso, new Vector3(0f, 0.02f, 0.02f), new Vector3(0.78f, 0.76f, 0.48f), metal);
                Part("Left Pauldron", PrimitiveType.Sphere, leftArm, new Vector3(0f, 0.48f, 0f), new Vector3(0.42f, 0.30f, 0.40f), metal);
                Part("Right Pauldron", PrimitiveType.Sphere, rightArm, new Vector3(0f, 0.48f, 0f), new Vector3(0.42f, 0.30f, 0.40f), metal);
                var shield = Part("Shield", PrimitiveType.Cylinder, leftArm, new Vector3(0f, -0.20f, 0.32f), new Vector3(0.46f, 0.10f, 0.46f), new Color(0.08f, 0.20f, 0.48f));
                shield.localRotation = Quaternion.Euler(90f, 0f, 0f);
                weaponPivot = Weapon(rightArm, "Longsword", new Vector3(0f, -0.55f, 0.20f), metal, false);
            }
            else if (heroId == "Rowan")
            {
                Part("Ranger Hood", PrimitiveType.Sphere, head, new Vector3(0f, 0.04f, -0.03f), new Vector3(0.49f, 0.50f, 0.47f), new Color(0.055f, 0.20f, 0.075f));
                Part("Hood Opening", PrimitiveType.Sphere, head, new Vector3(0f, -0.02f, 0.39f), new Vector3(0.34f, 0.35f, 0.05f), skin);
                cape = Part("Ranger Cloak", PrimitiveType.Cube, torso, new Vector3(0f, 0f, -0.34f), new Vector3(0.70f, 0.88f, 0.08f), new Color(0.08f, 0.24f, 0.10f));
                cape.localRotation = Quaternion.Euler(8f, 0f, 0f);
                weaponPivot = Weapon(leftArm, "Longbow", new Vector3(0f, -0.35f, 0.28f), new Color(0.31f, 0.18f, 0.07f), true);
                Part("Quiver", PrimitiveType.Cylinder, torso, new Vector3(0.30f, 0f, -0.42f), new Vector3(0.12f, 0.48f, 0.12f), leather).localRotation = Quaternion.Euler(18f, 0f, -15f);
            }
            else if (heroId == "Elara")
            {
                Part("Elara Hair", PrimitiveType.Sphere, head, new Vector3(0f, 0.05f, -0.10f), new Vector3(0.44f, 0.48f, 0.40f), new Color(0.34f, 0.23f, 0.12f));
                Part("Cleric Robe", PrimitiveType.Cylinder, visualRoot, new Vector3(0f, 0.72f, 0f), new Vector3(0.52f, 0.72f, 0.52f), new Color(0.69f, 0.67f, 0.60f));
                Part("Cleric Mantle", PrimitiveType.Cylinder, torso, new Vector3(0f, -0.10f, 0f), new Vector3(0.82f, 0.52f, 0.82f), new Color(0.76f, 0.72f, 0.62f));
                var halo = Part("Holy Circlet", PrimitiveType.Cylinder, head, new Vector3(0f, 0.18f, 0f), new Vector3(0.47f, 0.05f, 0.47f), new Color(0.86f, 0.68f, 0.22f));
                halo.localRotation = Quaternion.identity;
                weaponPivot = Weapon(rightArm, "Mace", new Vector3(0f, -0.50f, 0.20f), metal, false);
            }
            else // Garruk
            {
                Part("Garruk Hair", PrimitiveType.Sphere, head, new Vector3(0f, 0.16f, -0.08f), new Vector3(0.43f, 0.34f, 0.39f), new Color(0.10f, 0.055f, 0.025f));
                Part("Garruk Beard", PrimitiveType.Capsule, visualRoot, new Vector3(0f, 2.02f, 0.29f), new Vector3(0.27f, 0.26f, 0.18f), new Color(0.11f, 0.058f, 0.026f));
                Part("Fur Mantle", PrimitiveType.Capsule, torso, new Vector3(0f, 0.28f, 0f), new Vector3(0.90f, 0.28f, 0.68f), new Color(0.25f, 0.14f, 0.07f));
                Part("Left Bracer", PrimitiveType.Cylinder, leftArm, new Vector3(0f, -0.18f, 0f), new Vector3(0.27f, 0.22f, 0.27f), metal);
                Part("Right Bracer", PrimitiveType.Cylinder, rightArm, new Vector3(0f, -0.18f, 0f), new Vector3(0.27f, 0.22f, 0.27f), metal);
                weaponPivot = Weapon(rightArm, "Greataxe", new Vector3(0f, -0.55f, 0.24f), metal, false);
                Part("Axe Blade", PrimitiveType.Cube, weaponPivot, new Vector3(0.26f, 0.58f, 0f), new Vector3(0.42f, 0.30f, 0.08f), metal).localRotation = Quaternion.Euler(0f, 0f, 25f);
            }

            Part("Belt", PrimitiveType.Cylinder, pelvis, new Vector3(0f, 0.06f, 0f), new Vector3(0.67f * bulk, 0.10f, 0.43f * bulk), accent);
            RemoveChildColliders();
        }

        private void BuildGoblinParts(bool boss)
        {
            Color skin = boss ? new Color(0.24f, 0.38f, 0.12f) : new Color(0.27f, 0.50f, 0.15f);
            Color cloth = boss ? new Color(0.40f, 0.06f, 0.04f) : new Color(0.24f, 0.12f, 0.05f);
            Color metal = new Color(0.30f, 0.31f, 0.28f);
            float s = boss ? 1.25f : 0.88f;
            Height = boss ? 2.25f : 1.65f;

            Part("Goblin Pelvis", PrimitiveType.Cube, visualRoot, new Vector3(0f, 0.58f * s, 0f), new Vector3(0.54f * s, 0.30f * s, 0.34f * s), cloth);
            torso = Part("Goblin Torso", PrimitiveType.Capsule, visualRoot, new Vector3(0f, 1.02f * s, 0f), new Vector3(0.62f * s, 0.58f * s, 0.46f * s), cloth);
            head = Part("Goblin Head", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 1.52f * s, 0.04f), new Vector3(0.48f * s, 0.42f * s, 0.48f * s), skin);
            Part("Goblin Left Eye", PrimitiveType.Sphere, visualRoot, new Vector3(-0.14f * s, 1.58f * s, 0.45f * s), new Vector3(0.065f * s, 0.050f * s, 0.040f * s), new Color(0.96f, 0.70f, 0.10f));
            Part("Goblin Right Eye", PrimitiveType.Sphere, visualRoot, new Vector3(0.14f * s, 1.58f * s, 0.45f * s), new Vector3(0.065f * s, 0.050f * s, 0.040f * s), new Color(0.96f, 0.70f, 0.10f));
            Part("Goblin Nose", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.48f * s, 0.48f * s), new Vector3(0.12f * s, 0.13f * s, 0.18f * s), skin);
            Part("Left Ear", PrimitiveType.Cube, head, new Vector3(-0.50f, 0.02f, 0f), new Vector3(0.48f, 0.12f, 0.20f), skin).localRotation = Quaternion.Euler(0f, 0f, -18f);
            Part("Right Ear", PrimitiveType.Cube, head, new Vector3(0.50f, 0.02f, 0f), new Vector3(0.48f, 0.12f, 0.20f), skin).localRotation = Quaternion.Euler(0f, 0f, 18f);

            leftArm = Limb("Goblin Left Arm", visualRoot, new Vector3(-0.43f * s, 1.05f * s, 0f), new Vector3(0.18f * s, 0.50f * s, 0.18f * s), skin);
            rightArm = Limb("Goblin Right Arm", visualRoot, new Vector3(0.43f * s, 1.05f * s, 0f), new Vector3(0.18f * s, 0.50f * s, 0.18f * s), skin);
            leftLeg = Limb("Goblin Left Leg", visualRoot, new Vector3(-0.18f * s, 0.30f * s, 0f), new Vector3(0.18f * s, 0.38f * s, 0.18f * s), skin);
            rightLeg = Limb("Goblin Right Leg", visualRoot, new Vector3(0.18f * s, 0.30f * s, 0f), new Vector3(0.18f * s, 0.38f * s, 0.18f * s), skin);
            weaponPivot = Weapon(rightArm, boss ? "Captain Cleaver" : "Rusty Blade", new Vector3(0f, -0.46f * s, 0.18f), metal, false);
            if (boss)
            {
                Part("Captain Shoulder", PrimitiveType.Sphere, leftArm, new Vector3(0f, 0.35f, 0f), new Vector3(0.42f, 0.32f, 0.42f), metal);
                Part("Captain Helm", PrimitiveType.Cylinder, head, new Vector3(0f, 0.22f, 0f), new Vector3(0.50f, 0.14f, 0.50f), metal);
            }
            RemoveChildColliders();
        }

        private Transform Limb(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var pivot = new GameObject(name + " Pivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            Part(name, PrimitiveType.Capsule, pivot, new Vector3(0f, -scale.y * 0.34f, 0f), scale, color);
            return pivot;
        }

        private Transform Weapon(Transform parent, string name, Vector3 position, Color color, bool bow)
        {
            var pivot = new GameObject(name + " Pivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            var shaft = Part(name, bow ? PrimitiveType.Cube : PrimitiveType.Cylinder, pivot, Vector3.zero,
                bow ? new Vector3(0.08f, 0.82f, 0.08f) : new Vector3(0.07f, 0.62f, 0.07f), color);
            shaft.localRotation = bow ? Quaternion.Euler(0f, 0f, 8f) : Quaternion.identity;
            if (!bow)
                Part(name + " Head", PrimitiveType.Cube, pivot, new Vector3(0f, 0.62f, 0f), new Vector3(0.18f, 0.28f, 0.08f), color);
            return pivot;
        }

        private Transform Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = color;
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.black);
                renderers.Add(renderer);
                baseColors.Add(color);
            }
            return go.transform;
        }

        private void RemoveChildColliders()
        {
            foreach (var collider in GetComponentsInChildren<Collider>()) Destroy(collider);
        }

        public void Tick(float locomotionSpeed)
        {
            if (dead) return;
            float moveAmount = Mathf.Clamp01(locomotionSpeed / 2.4f);
            gaitPhase += Time.deltaTime * Mathf.Lerp(1.7f, 9f, moveAmount);
            float swing = Mathf.Sin(gaitPhase) * 30f * moveAmount;
            float idle = Mathf.Sin(Time.time * 2.2f) * 1.5f * (1f - moveAmount);

            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(swing + idle, 0f, 0f);
            if (rightArm != null && attackRoutine == null) rightArm.localRotation = Quaternion.Euler(-swing - idle, 0f, 0f);
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (torso != null) torso.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(gaitPhase * 0.5f) * 1.4f * moveAmount);
            if (cape != null) cape.localRotation = Quaternion.Euler(8f + moveAmount * 8f + Mathf.Sin(gaitPhase) * 2f, 0f, 0f);
        }

        public void SetHighlight(Color color, float intensity)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                renderer.material.SetColor("_EmissionColor", color * intensity);
            }
        }

        public void PlayAttack()
        {
            if (dead || rightArm == null) return;
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(AttackMotion());
        }

        private IEnumerator AttackMotion()
        {
            Quaternion start = rightArm.localRotation;
            Quaternion windup = Quaternion.Euler(-80f, -12f, 20f);
            Quaternion strike = Quaternion.Euler(78f, 12f, -18f);
            float t = 0f;
            while (t < 0.14f)
            {
                t += Time.deltaTime;
                rightArm.localRotation = Quaternion.Slerp(start, windup, t / 0.14f);
                yield return null;
            }
            t = 0f;
            while (t < 0.11f)
            {
                t += Time.deltaTime;
                rightArm.localRotation = Quaternion.Slerp(windup, strike, t / 0.11f);
                yield return null;
            }
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                rightArm.localRotation = Quaternion.Slerp(strike, Quaternion.identity, t / 0.18f);
                yield return null;
            }
            rightArm.localRotation = Quaternion.identity;
            attackRoutine = null;
        }

        public void PlayHit()
        {
            if (dead) return;
            if (hitRoutine != null) StopCoroutine(hitRoutine);
            hitRoutine = StartCoroutine(HitMotion());
        }

        private IEnumerator HitMotion()
        {
            SetHighlight(new Color(1f, 0.12f, 0.04f), 0.9f);
            Quaternion start = visualRoot.localRotation;
            Quaternion recoil = start * Quaternion.Euler(-8f, 0f, 8f);
            float t = 0f;
            while (t < 0.09f)
            {
                t += Time.deltaTime;
                visualRoot.localRotation = Quaternion.Slerp(start, recoil, t / 0.09f);
                yield return null;
            }
            t = 0f;
            while (t < 0.13f)
            {
                t += Time.deltaTime;
                visualRoot.localRotation = Quaternion.Slerp(recoil, start, t / 0.13f);
                yield return null;
            }
            visualRoot.localRotation = start;
            SetHighlight(Color.black, 0f);
            hitRoutine = null;
        }

        public void PlayDeath()
        {
            if (dead) return;
            dead = true;
            if (deathRoutine != null) StopCoroutine(deathRoutine);
            deathRoutine = StartCoroutine(DeathMotion());
        }

        public void Revive()
        {
            dead = false;
            if (deathRoutine != null) { StopCoroutine(deathRoutine); deathRoutine = null; }
            if (visualRoot != null) { visualRoot.localRotation = Quaternion.identity; visualRoot.localPosition = Vector3.zero; }
            SetHighlight(Color.black, 0f);
        }

        private IEnumerator DeathMotion()
        {
            Quaternion start = visualRoot.localRotation;
            Quaternion end = Quaternion.Euler(0f, 0f, 78f);
            Vector3 startPos = visualRoot.localPosition;
            Vector3 endPos = startPos + new Vector3(0f, 0.18f, 0f);
            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / 0.55f);
                visualRoot.localRotation = Quaternion.Slerp(start, end, p);
                visualRoot.localPosition = Vector3.Lerp(startPos, endPos, p);
                yield return null;
            }
            SetHighlight(Color.black, 0f);
        }
    }
}
