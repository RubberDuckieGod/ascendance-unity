using UnityEngine;
using UnityEngine.UI;

namespace Ascendance
{
    public static class ShellUIFactory
    {
        public static readonly Color Gold = new(0.78f, 0.57f, 0.20f, 1f);
        public static readonly Color Ivory = new(0.92f, 0.88f, 0.77f, 1f);
        public static readonly Color Panel = new(0.025f, 0.032f, 0.040f, 0.96f);
        public static readonly Color Panel2 = new(0.07f, 0.045f, 0.025f, 0.97f);
        public static readonly Color Button = new(0.12f, 0.075f, 0.035f, 0.98f);

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static GameObject PanelRect(Transform parent, string name, Vector2 min, Vector2 max, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color ?? Panel;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.65f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return go;
        }

        public static Text TextRect(Transform parent, string text, int size, TextAnchor anchor, Vector2 min, Vector2 max, Color? color = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color ?? Ivory;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        public static Button ButtonRect(Transform parent, string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Button_" + text.Replace(" ", "_"));
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = Button;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(1f, -1f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.24f, 0.14f, 0.055f, 1f);
            colors.pressedColor = new Color(0.36f, 0.20f, 0.06f, 1f);
            colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.72f);
            button.colors = colors;
            button.onClick.AddListener(action);
            TextRect(go.transform, text, 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Ivory);
            return button;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
