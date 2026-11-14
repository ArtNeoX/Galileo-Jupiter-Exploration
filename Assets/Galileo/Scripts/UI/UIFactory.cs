using System;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.UI
{
    /// <summary>Small helpers for building uGUI hierarchies from code.</summary>
    public static class UIFactory
    {
        const int UILayer = 5;
        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UILayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Fill the parent, inset by the given margins.</summary>
        public static RectTransform Stretch(this RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>Fixed-size rect pinned to one anchor point (anchor doubles as pivot).</summary>
        public static RectTransform Pin(this RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, int radius = 18)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = ProceduralArt.RoundedRect(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Image Frame(Transform parent, Color color, int radius = 18, int thickness = 2)
        {
            var img = Rect("Frame", parent).Stretch().gameObject.AddComponent<Image>();
            img.sprite = ProceduralArt.RoundedFrame(radius, thickness);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Picture(string name, Transform parent, Sprite sprite, Color color)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Image Line(Transform parent, Color color)
        {
            var img = Rect("Line", parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var t = Rect(name, parent).gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.lineSpacing = 1.1f;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        /// <summary>Shrink the font as needed so long text always fits its box.</summary>
        public static Text BestFit(this Text t, int min, int max)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
            t.fontSize = max;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Action onClick)
        {
            var img = Panel(name, parent, Color.white, 14);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Theme.Accent;
            colors.highlightedColor = Theme.AccentBright;
            colors.pressedColor = Theme.AccentDark;
            colors.selectedColor = Theme.Accent;
            colors.disabledColor = Theme.Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            btn.colors = colors;
            Label("Label", img.transform, label, 30, Theme.ButtonText, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Stretch();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }
    }
}
