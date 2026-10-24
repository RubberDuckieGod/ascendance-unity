using UnityEngine;
using UnityEngine.EventSystems;

namespace Ascendance
{
    /// <summary>
    /// Tactical CRPG camera tuned for a closer, lower presentation while preserving
    /// enough zoom-out range for Location-to-Location planning.
    /// </summary>
    public sealed class IsometricCameraController : MonoBehaviour
    {
        public float PanSpeed = 13.5f;
        public float ZoomSpeed = 2.35f;
        public float RotateSpeed = 0.18f;
        public float EdgeScrollPixels = 18f;
        public float SmoothTime = 0.085f;
        public float MinDistance = 9.5f;
        public float MaxDistance = 31f;
        public float FocusDistance = 13.5f;
        public float FocusHeight = 1.15f;

        private float yaw = 38f;
        private float pitch = 34f;
        private float distance = 16.5f;
        private Vector3 focus = new(0f, 1.15f, 1.5f);
        private Vector3 desiredFocus = new(0f, 1.15f, 1.5f);
        private Vector3 focusVelocity;
        private float desiredYaw = 38f;
        private float desiredDistance = 16.5f;
        private float shakeRemaining;
        private float shakeDuration;
        private float shakeStrength;

        private void Start()
        {
            yaw = desiredYaw = transform.eulerAngles.y;
            distance = desiredDistance = 16.5f;
            ApplyImmediate();
        }

        private void Update()
        {
            Vector2 move = ReadMove();
            Vector2 pointer = PointerPosition();
            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!pointerOverUI)
            {
                if (pointer.x <= EdgeScrollPixels) move.x -= 1f;
                if (pointer.x >= Screen.width - EdgeScrollPixels) move.x += 1f;
                if (pointer.y <= EdgeScrollPixels) move.y -= 1f;
                if (pointer.y >= Screen.height - EdgeScrollPixels) move.y += 1f;
            }
            move = Vector2.ClampMagnitude(move, 1f);

            Vector3 forward = Quaternion.Euler(0f, desiredYaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, desiredYaw, 0f) * Vector3.right;
            float panMultiplier = Mathf.Lerp(0.78f, 1.32f, Mathf.InverseLerp(MinDistance, MaxDistance, desiredDistance));
            desiredFocus += (right * move.x + forward * move.y) * PanSpeed * panMultiplier * Time.deltaTime;

            if (MiddleHeld()) desiredYaw += PointerDelta().x * RotateSpeed;
            desiredYaw += ReadRotateKeys() * 72f * Time.deltaTime;

            desiredDistance = Mathf.Clamp(desiredDistance - ReadScroll() * ZoomSpeed, MinDistance, MaxDistance);

            if (FocusPressed()) FocusSelectedHero(true);
        }

        private void LateUpdate()
        {
            focus = Vector3.SmoothDamp(focus, desiredFocus, ref focusVelocity, SmoothTime);
            yaw = Mathf.LerpAngle(yaw, desiredYaw, 1f - Mathf.Exp(-13f * Time.deltaTime));
            distance = Mathf.Lerp(distance, desiredDistance, 1f - Mathf.Exp(-13f * Time.deltaTime));
            ApplyImmediate();
        }

        /// <summary>
        /// Follow selection changes without destroying the player's chosen zoom level.
        /// </summary>
        public void FocusOn(Vector3 worldPosition)
        {
            desiredFocus = new Vector3(worldPosition.x, worldPosition.y + FocusHeight, worldPosition.z);
        }

        /// <summary>
        /// F-key framing: center the Hero and return to a readable character-scale view.
        /// </summary>
        public void FocusSelectedHero(bool frame = true)
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null || !gm.HeroAvatars.TryGetValue(gm.SelectedHeroId, out var hero)) return;
            FocusOn(hero.transform.position);
            if (frame) desiredDistance = FocusDistance;
        }

        public void AddImpulse(float strength = 0.14f, float duration = 0.12f)
        {
            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeRemaining = Mathf.Max(shakeRemaining, duration);
        }

        private void ApplyImmediate()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = focus - rot * Vector3.forward * distance;

            if (shakeRemaining > 0f)
            {
                float normalized = shakeDuration > 0f ? shakeRemaining / shakeDuration : 0f;
                Vector2 jitter = Random.insideUnitCircle * shakeStrength * normalized;
                position += transform.right * jitter.x + transform.up * jitter.y;
                shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
                if (shakeRemaining <= 0f)
                {
                    shakeStrength = 0f;
                    shakeDuration = 0f;
                }
            }

            transform.position = position;
            transform.rotation = rot;
        }

        private static Vector2 ReadMove()
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            return new Vector2(x, y);
        }

        private static float ReadRotateKeys()
        {
            return (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        }

        private static bool MiddleHeld() => Input.GetMouseButton(2);

        private static Vector2 PointerDelta()
        {
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
        }

        private static Vector2 PointerPosition() => Input.mousePosition;
        private static float ReadScroll() => Input.mouseScrollDelta.y;
        private static bool FocusPressed() => Input.GetKeyDown(KeyCode.F);
    }
}
