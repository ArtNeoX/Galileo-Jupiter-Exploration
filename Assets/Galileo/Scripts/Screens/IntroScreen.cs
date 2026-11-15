using System.Collections;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Full-screen title card: the screen darkens, the text fades in at the centre, holds, fades out and the
    /// screen brightens again onto whatever comes next. See <see cref="IntroRequest"/>.
    /// </summary>
    public class IntroScreen : ScreenBase
    {
        const float BlackFade = 0.7f;
        const float TextFade = 0.6f;

        IntroRequest _request;
        Image _black;
        Text _text, _hint;
        bool _clicked;

        public override bool AllowLanguageToggle => false;
        public override bool FadesIn => false;

        protected override void Build()
        {
            _request = App.PendingIntro ?? new IntroRequest();
            _black = Root.gameObject.AddComponent<Image>();
            _black.color = new Color(0, 0, 0, _request.startBlack ? 1f : 0f);
            Root.gameObject.AddComponent<ClickCatcher>().Clicked += () => _clicked = true;

            _text = UIFactory.Label("Text", Root, _request.text?.Invoke(Content) ?? "", 76, _request.textColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            _text.rectTransform.Stretch(120, 0, 120, 0);
            SetAlpha(_text, 0f);
            _hint = UIFactory.Label("Hint", Root, Ui.clickToContinue, 26, Theme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
            _hint.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), new Vector2(0, 90), new Vector2(0, 140));
            SetAlpha(_hint, 0f);
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            if (!_request.startBlack) yield return FadeTo(_black, 1f, BlackFade);
            _request.onBlack?.Invoke();
            GameAudio.Play("popup", 0.5f);
            yield return FadeTo(_text, 1f, TextFade);

            _clicked = false;
            for (float t = 0; t < _request.hold && !(_clicked && !_request.waitForInput); t += Time.unscaledDeltaTime)
                yield return null;
            if (_request.waitForInput)
            {
                _clicked = false;
                StartCoroutine(FadeTo(_hint, 1f, 0.5f));
                while (!_clicked && !AdvancePressed()) yield return null;
                GameAudio.Click();
                StartCoroutine(FadeTo(_hint, 0f, 0.3f));
            }

            yield return FadeTo(_text, 0f, TextFade);
            if (!_request.stayBlack) yield return FadeTo(_black, 0f, BlackFade);
            _request.onDone?.Invoke();
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        static IEnumerator FadeTo(Graphic g, float to, float duration)
        {
            float from = g.color.a;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(g, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            SetAlpha(g, to);
        }
    }
}
