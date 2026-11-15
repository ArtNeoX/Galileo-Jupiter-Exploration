using System.Collections;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>One game "scene" (screen). Builds its own UI under a full-screen RectTransform.</summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        protected GalileoApp App { get; private set; }
        protected GalileoContent Content => App.Content;
        protected UiText Ui => App.Content.ui;
        protected RectTransform Root => (RectTransform)transform;

        public void Init(GalileoApp app)
        {
            App = app;
            Build();
        }

        protected abstract void Build();

        /// <summary>Shows the language button; switching rebuilds this screen in the new language.</summary>
        public virtual bool AllowLanguageToggle => true;

        /// <summary>Shows the budget in the top-right corner.</summary>
        public virtual bool ShowBudget => false;

        /// <summary>False for screens that handle their own fade (title cards).</summary>
        public virtual bool FadesIn => true;

        /// <summary>Space / Enter / Right arrow, for advancing without the mouse.</summary>
        protected static bool AdvancePressed()
        {
            var k = Keyboard.current;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame ||
                                 k.numpadEnterKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame);
        }

        /// <summary>Makes the whole screen clickable.</summary>
        protected void MakeClickable(System.Action onClick)
        {
            var hit = Root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Root.gameObject.AddComponent<ClickCatcher>().Clicked += onClick;
        }

        protected void Header(string title, string subtitle)
        {
            UIFactory.Label("Title", Root, title.ToUpperInvariant(), 46, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(100, -125), new Vector2(-100, -55));
            if (!string.IsNullOrEmpty(subtitle))
                UIFactory.Label("Subtitle", Root, subtitle, 26, Theme.TextMuted, TextAnchor.UpperCenter).BestFit(18, 26)
                    .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(200, -195), new Vector2(-200, -128));
        }

        protected Button NextButton(System.Action onClick)
        {
            var btn = UIFactory.Button("Next", Root, Ui.next.ToUpperInvariant(), onClick);
            btn.GetComponent<RectTransform>().Pin(new Vector2(1, 0), new Vector2(260, 76), new Vector2(-120, 70));
            return btn;
        }

        protected Button BackButton(System.Action onClick)
        {
            var btn = UIFactory.IconButton("Back", Root, ProceduralArt.Icon("chevron_left"), onClick);
            btn.GetComponent<RectTransform>().Pin(new Vector2(0, 1), new Vector2(72, 72), new Vector2(60, -55));
            return btn;
        }

        /// <summary>Framed character bitmap with the name underneath. Size/position it via the returned rect.</summary>
        protected RectTransform PortraitCard(Transform parent, string characterId)
        {
            var box = UIFactory.Panel("Portrait " + characterId, parent, Color.clear, 16);
            box.raycastTarget = false;
            var portrait = UIFactory.Picture("Portrait", box.transform, null, Color.white);
            portrait.rectTransform.Stretch(12, 12, 12, 12);
            ApplyPortrait(portrait, characterId);
            var person = Content.FindCharacter(characterId);
            UIFactory.Label("Name", box.transform, person != null ? person.name : "", 22, Theme.TextMuted, TextAnchor.UpperCenter, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(-40, -40), new Vector2(40, -8));
            return box.rectTransform;
        }

        protected static string Section(string label, string text, string colorHex = null)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var color = colorHex ?? Theme.AccentHex;
            return $"<color={color}><b>{label}:</b></color>\n{text}\n\n";
        }

        protected Text FooterLabel(string text)
        {
            var label = UIFactory.Label("Footer", Root, text, 26, Theme.TextMuted, TextAnchor.MiddleLeft);
            label.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(120, 70), new Vector2(-420, 146));
            return label;
        }

        protected static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        protected static Sprite IconFor(string id)
        {
            if (string.IsNullOrEmpty(id)) return ProceduralArt.Icon("");
            return ContentLoader.LoadSprite("Galileo/Icons/" + id) ?? ProceduralArt.Icon(id);
        }

        /// <summary>Character bitmap from Resources/Galileo/Characters/&lt;id&gt;, or a silhouette placeholder.</summary>
        protected static void ApplyPortrait(Image image, string characterId)
        {
            var sprite = string.IsNullOrEmpty(characterId) ? null : ContentLoader.LoadSprite("Galileo/Characters/" + characterId);
            image.sprite = sprite != null ? sprite : ProceduralArt.Icon("person");
            image.color = sprite != null ? Color.white : Theme.Placeholder;
        }
    }
}
