using System.Collections;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Small presentation-only hit pause. Rules resolution has already reached the impact callback,
    /// so this never changes combat math or turn order.
    /// </summary>
    public sealed class CombatTimeEffects : MonoBehaviour
    {
        public static CombatTimeEffects Instance { get; private set; }
        private Coroutine routine;
        private float restoreScale = 1f;

        private void Awake()
        {
            Instance = this;
        }

        public void HitStop(float realtimeSeconds = 0.035f)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                Time.timeScale = restoreScale;
            }
            routine = StartCoroutine(HitStopRoutine(Mathf.Clamp(realtimeSeconds, 0.015f, 0.08f)));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            restoreScale = Mathf.Max(0.01f, Time.timeScale);
            Time.timeScale = 0.04f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = restoreScale;
            routine = null;
        }

        private void OnDisable()
        {
            if (routine != null) Time.timeScale = restoreScale;
            if (Instance == this) Instance = null;
        }
    }
}
