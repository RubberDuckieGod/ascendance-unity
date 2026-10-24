using UnityEngine;
using UnityEngine.EventSystems;
using Ascendance.Online;

namespace Ascendance
{
    public sealed class CRPGInputController : MonoBehaviour
    {
        public static CRPGInputController Instance { get; private set; }
        public string CurrentHint { get; private set; } = "";

        private HeroAvatar hoveredHero;
        private EnemyAvatar hoveredEnemy;
        private LocationMarker hoveredLocation;
        private Camera mainCamera;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            UpdateHover();
            HandleClick();
            HandleHotkeys();
        }

        private void UpdateHover()
        {
            HeroAvatar nextHero = null;
            EnemyAvatar nextEnemy = null;
            LocationMarker nextLocation = null;
            string nextHint = "";

            if (!IsPointerOverUI())
            {
                Ray ray = mainCamera.ScreenPointToRay(PointerPosition());
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    nextHero = hit.collider.GetComponentInParent<HeroAvatar>();
                    if (nextHero != null)
                    {
                        nextHint = $"Select {nextHero.DisplayName}";
                    }
                    else
                    {
                        nextEnemy = hit.collider.GetComponentInParent<EnemyAvatar>();
                        if (nextEnemy != null)
                        {
                            var gm = AscendanceGameManager.Instance;
                            var state = gm != null && gm.Enemies.TryGetValue(nextEnemy.EnemyId, out var e) ? e : null;
                            nextHint = state == null ? "Enemy" : $"Target {state.DisplayName} — HP {state.Health}/{state.MaxHealth} · Armor {state.Armor}";
                        }
                        else
                        {
                            nextLocation = hit.collider.GetComponentInParent<LocationMarker>();
                            if (nextLocation != null)
                            {
                                var gm = AscendanceGameManager.Instance;
                                bool legal = gm != null && gm.CanMoveTo(nextLocation.LocationId);
                                string name = gm != null && gm.Locations.TryGetValue(nextLocation.LocationId, out var location) ? location.Name : nextLocation.LocationId;
                                nextHint = legal ? $"Move to {name} — 1 Action" : $"{name} — movement unavailable";
                                MovementPathPreview.Instance?.ShowTo(nextLocation.transform.position, legal);
                            }
                        }
                    }
                }
            }

            SetHoveredHero(nextHero);
            SetHoveredEnemy(nextEnemy);
            SetHoveredLocation(nextLocation);
            CurrentHint = nextHint;

            if (nextLocation == null) MovementPathPreview.Instance?.Hide();
        }

        private void HandleClick()
        {
            if (!LeftPressedThisFrame() || IsPointerOverUI()) return;

            if (hoveredHero != null)
            {
                AscendanceGameManager.Instance?.SelectHero(hoveredHero.HeroId);
                return;
            }

            if (hoveredEnemy != null)
            {
                AscendanceGameManager.Instance?.SelectEnemy(hoveredEnemy.EnemyId);
                return;
            }

            if (hoveredLocation != null)
            {
                AscendanceClientCommandRouter0101.Instance?.RequestMove(hoveredLocation.LocationId);
            }
        }

        private void HandleHotkeys()
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null) return;

            if (KeyPressedThisFrame(KeyCode.Alpha1)) gm.SelectHero("Garruk");
            if (KeyPressedThisFrame(KeyCode.Alpha2)) gm.SelectHero("Rowan");
            if (KeyPressedThisFrame(KeyCode.Alpha3)) gm.SelectHero("Aldric");
            if (KeyPressedThisFrame(KeyCode.Alpha4)) gm.SelectHero("Elara");
            if (KeyPressedThisFrame(KeyCode.Space)) AscendanceClientCommandRouter0101.Instance?.RequestAttackSelectedEnemy();
            if (KeyPressedThisFrame(KeyCode.Tab)) gm.CycleEnemyTarget();
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void SetHoveredHero(HeroAvatar next)
        {
            if (hoveredHero == next) return;
            if (hoveredHero != null) hoveredHero.SetHovered(false);
            hoveredHero = next;
            if (hoveredHero != null) hoveredHero.SetHovered(true);
        }

        private void SetHoveredEnemy(EnemyAvatar next)
        {
            if (hoveredEnemy == next) return;
            if (hoveredEnemy != null) hoveredEnemy.SetHovered(false);
            hoveredEnemy = next;
            if (hoveredEnemy != null) hoveredEnemy.SetHovered(true);
        }

        private void SetHoveredLocation(LocationMarker next)
        {
            if (hoveredLocation == next) return;
            if (hoveredLocation != null) hoveredLocation.SetHovered(false);
            hoveredLocation = next;
            if (hoveredLocation != null) hoveredLocation.SetHovered(true);
        }

        private static Vector2 PointerPosition()
        {
            return Input.mousePosition;
        }

        private static bool LeftPressedThisFrame()
        {
            return Input.GetMouseButtonDown(0);
        }

        private static bool KeyPressedThisFrame(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }
    }
}
