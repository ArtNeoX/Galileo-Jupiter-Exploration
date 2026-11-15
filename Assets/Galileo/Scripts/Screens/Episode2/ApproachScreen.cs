using System.Collections;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Episode 2 opens on jupiter_distanced: "150 days until the Jupiter approach, main antenna has failed", then two
    /// choices. Switching to the low-gain antenna is only possible if it was attached in the garage.
    /// </summary>
    public class ApproachScreen : ScreenBase
    {
        Image _black;
        Text _text;
        CanvasGroup _buttons;

        public override bool FadesIn => false;

        protected override void Build()
        {
            var data = Content.episode2;
            bool replay = App.ApproachShown;

            _black = UIFactory.Line(Root, Color.black);
            _black.rectTransform.Stretch();
            _black.color = new Color(0, 0, 0, replay ? 0f : 1f);

            _text = UIFactory.Label("Text", Root, data.approachText, 60, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _text.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(120, 20), new Vector2(-120, 260));
            _text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
            SetAlpha(_text, replay ? 1f : 0f);

            var column = UIFactory.Rect("Buttons", Root);
            column.Pin(new Vector2(0.5f, 0f), new Vector2(720, 250), new Vector2(0, 60));
            _buttons = column.gameObject.AddComponent<CanvasGroup>();
            _buttons.alpha = replay ? 1f : 0f;
            _buttons.interactable = _buttons.blocksRaycasts = replay;

            bool hasAntenna = App.HasLowGainAntenna;
            var switchButton = UIFactory.Button("Switch", column, data.switchAntenna, App.ShowSeparation);
            switchButton.GetComponent<RectTransform>().Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -84), Vector2.zero);
            switchButton.transform.Find("Label").GetComponent<Text>().BestFit(18, 30);
            switchButton.interactable = hasAntenna;
            var abort = UIFactory.Button("Abort", column, data.abortMission, App.GameOver);
            abort.GetComponent<RectTransform>().Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -184), new Vector2(0, -100));
            if (!hasAntenna)
            {
                var note = UIFactory.Label("Note", column, data.noAntennaNote, 24, Theme.Failure, TextAnchor.UpperCenter, FontStyle.Italic);
                note.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 56));
                note.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            }

            if (!replay) StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            App.ApproachShown = true;
            yield return Fade(_black, 0f, 1.6f);
            yield return Fade(_text, 1f, 1.2f);
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Fade(_buttons, 0f, 1f, 0.6f);
            _buttons.interactable = _buttons.blocksRaycasts = true;
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        static IEnumerator Fade(Graphic g, float to, float duration)
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
