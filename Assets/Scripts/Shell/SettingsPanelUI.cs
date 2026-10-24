using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ascendance
{
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        private Toggle fullscreenToggle;
        private Dropdown resolutionDropdown;
        private Slider volumeSlider;
        private List<Vector2Int> resolutions;
        private GameObject panel;
        private System.Action onClose;

        public static SettingsPanelUI Create(Transform parent, System.Action onClose)
        {
            var host = new GameObject("SettingsPanelController");
            host.transform.SetParent(parent, false);
            var ui = host.AddComponent<SettingsPanelUI>();
            ui.onClose = onClose;
            ui.Build(parent);
            return ui;
        }

        private void Build(Transform parent)
        {
            panel = ShellUIFactory.PanelRect(parent, "Settings", new Vector2(0.31f, 0.19f), new Vector2(0.69f, 0.81f), new Color(0.018f, 0.024f, 0.032f, 0.995f));
            ShellUIFactory.TextRect(panel.transform, "SETTINGS", 40, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f), ShellUIFactory.Gold);

            ShellUIFactory.TextRect(panel.transform, "Display", 24, TextAnchor.MiddleLeft, new Vector2(0.10f, 0.70f), new Vector2(0.35f, 0.78f));
            fullscreenToggle = CreateToggle(panel.transform, "Fullscreen Window", new Vector2(0.42f, 0.70f), new Vector2(0.88f, 0.78f));
            fullscreenToggle.isOn = AscendanceSettings.Fullscreen;

            ShellUIFactory.TextRect(panel.transform, "Resolution", 24, TextAnchor.MiddleLeft, new Vector2(0.10f, 0.56f), new Vector2(0.35f, 0.64f));
            resolutionDropdown = CreateDropdown(panel.transform, new Vector2(0.42f, 0.55f), new Vector2(0.88f, 0.65f));
            resolutions = AscendanceSettings.AvailableResolutions();
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(resolutions.ConvertAll(r => $"{r.x} × {r.y}"));
            int current = resolutions.FindIndex(r => r.x == AscendanceSettings.Width && r.y == AscendanceSettings.Height);
            resolutionDropdown.value = Mathf.Max(0, current >= 0 ? current : resolutions.Count - 1);

            ShellUIFactory.TextRect(panel.transform, "Master Volume", 24, TextAnchor.MiddleLeft, new Vector2(0.10f, 0.41f), new Vector2(0.35f, 0.49f));
            volumeSlider = CreateSlider(panel.transform, new Vector2(0.42f, 0.42f), new Vector2(0.88f, 0.48f));
            volumeSlider.value = AscendanceSettings.Volume;

            ShellUIFactory.ButtonRect(panel.transform, "APPLY", new Vector2(0.10f, 0.12f), new Vector2(0.46f, 0.24f), Apply);
            ShellUIFactory.ButtonRect(panel.transform, "BACK", new Vector2(0.54f, 0.12f), new Vector2(0.90f, 0.24f), Close);
        }

        public void SetVisible(bool value) => panel.SetActive(value);

        private void Apply()
        {
            if (resolutions == null || resolutions.Count == 0) return;
            int index = Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Count - 1);
            Vector2Int r = resolutions[index];
            AscendanceSettings.SaveAndApply(fullscreenToggle.isOn, r.x, r.y, volumeSlider.value);
        }

        private void Close()
        {
            Apply();
            panel.SetActive(false);
            onClose?.Invoke();
        }

        private static Toggle CreateToggle(Transform parent, string label, Vector2 min, Vector2 max)
        {
            var root = ShellUIFactory.PanelRect(parent, "Toggle", min, max, new Color(0.07f, 0.075f, 0.08f, 0.95f));
            var toggle = root.AddComponent<Toggle>();
            var check = new GameObject("Checkmark");
            check.transform.SetParent(root.transform, false);
            var crt = check.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.05f, 0.25f); crt.anchorMax = new Vector2(0.12f, 0.75f); crt.offsetMin = crt.offsetMax = Vector2.zero;
            var checkImage = check.AddComponent<Image>();
            checkImage.color = ShellUIFactory.Gold;
            toggle.graphic = checkImage;
            toggle.targetGraphic = root.GetComponent<Image>();
            ShellUIFactory.TextRect(root.transform, label, 20, TextAnchor.MiddleLeft, new Vector2(0.16f, 0.05f), new Vector2(0.96f, 0.95f));
            return toggle;
        }

        private static Dropdown CreateDropdown(Transform parent, Vector2 min, Vector2 max)
        {
            var root = ShellUIFactory.PanelRect(parent, "ResolutionDropdown", min, max, new Color(0.07f, 0.075f, 0.08f, 0.98f));
            var dropdown = root.AddComponent<Dropdown>();
            var label = ShellUIFactory.TextRect(root.transform, "Resolution", 20, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.05f), new Vector2(0.90f, 0.95f));
            dropdown.captionText = label;

            var template = ShellUIFactory.PanelRect(root.transform, "Template", new Vector2(0f, -3.2f), new Vector2(1f, 0f), new Color(0.025f, 0.032f, 0.04f, 1f));
            template.SetActive(false);
            var scroll = template.AddComponent<ScrollRect>();
            var viewport = ShellUIFactory.PanelRect(template.transform, "Viewport", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f); contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0f, 36f);
            var item = ShellUIFactory.PanelRect(content.transform, "Item", new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0.05f, 0.055f, 0.06f, 1f));
            var itemToggle = item.AddComponent<Toggle>();
            var itemLabel = ShellUIFactory.TextRect(item.transform, "Option", 18, TextAnchor.MiddleLeft, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.itemText = itemLabel;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRT;
            return dropdown;
        }

        private static Slider CreateSlider(Transform parent, Vector2 min, Vector2 max)
        {
            var root = ShellUIFactory.PanelRect(parent, "VolumeSlider", min, max, new Color(0.07f, 0.075f, 0.08f, 0.95f));
            var slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            var fill = ShellUIFactory.PanelRect(root.transform, "Fill", new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.82f), ShellUIFactory.Gold);
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            return slider;
        }
    }
}
