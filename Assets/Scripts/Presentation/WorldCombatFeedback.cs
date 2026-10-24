using System.Collections;
using UnityEngine;

namespace Ascendance
{
    public sealed class WorldCombatFeedback : MonoBehaviour
    {
        public static void SpawnDamage(Vector3 worldPosition, int amount, bool healing = false)
        {
            var go = new GameObject(healing ? "Healing Text" : "Damage Text");
            go.transform.position = worldPosition + Vector3.up * 2.1f;
            var text = go.AddComponent<TextMesh>();
            text.text = healing ? "+" + amount : "-" + amount;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.075f;
            text.fontSize = 54;
            text.fontStyle = FontStyle.Bold;
            text.color = healing ? new Color(0.34f, 1f, 0.46f) : new Color(1f, 0.28f, 0.13f);
            go.AddComponent<WorldCombatFeedback>().StartCoroutine(go.GetComponent<WorldCombatFeedback>().FloatText(text));
        }

        public static void SpawnHitFlash(Vector3 worldPosition, Color color)
        {
            if (ProductionVFXResolver.TrySpawn("Hit_Impact", worldPosition + Vector3.up * 1.15f, Quaternion.identity, 1.5f)) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Hit Flash";
            go.transform.position = worldPosition + Vector3.up * 1.15f;
            go.transform.localScale = Vector3.one * 0.18f;
            Destroy(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", color * 2.0f);
            go.AddComponent<WorldCombatFeedback>().StartCoroutine(go.GetComponent<WorldCombatFeedback>().Flash(renderer));
        }

        private IEnumerator FloatText(TextMesh text)
        {
            Camera camera = Camera.main;
            Vector3 start = transform.position;
            float t = 0f;
            while (t < 0.85f)
            {
                t += Time.deltaTime;
                transform.position = start + Vector3.up * (t * 0.95f);
                if (camera != null) transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
                Color c = text.color;
                c.a = Mathf.Clamp01(1f - t / 0.85f);
                text.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator Flash(Renderer renderer)
        {
            float t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float p = t / 0.22f;
                transform.localScale = Vector3.Lerp(startScale, startScale * 3.2f, p);
                Color c = renderer.material.color;
                c.a = 1f - p;
                renderer.material.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
