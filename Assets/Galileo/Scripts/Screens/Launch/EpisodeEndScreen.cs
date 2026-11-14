using System.Collections;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>The screen slowly darkens over the launch, then "КОНЕЦ 1 ЭПИЗОДА" fades in.</summary>
    public class EpisodeEndScreen : ScreenBase
    {
        Image _black;
        Text _label;

        protected override void Build()
        {
            _black = Root.gameObject.AddComponent<Image>();
            _black.color = new Color(0, 0, 0, 0);
            _label = UIFactory.Label("Text", Root, Content.episodeEndText, 72, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.rectTransform.Stretch();
            _label.color = new Color(_label.color.r, _label.color.g, _label.color.b, 0);
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            yield return FadeGraphic(_black, 1f, 3f);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return FadeGraphic(_label, 1f, 1.8f);
        }

        static IEnumerator FadeGraphic(Graphic g, float to, float duration)
        {
            var c = g.color;
            float from = c.a;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                g.color = c;
                yield return null;
            }
            c.a = to;
            g.color = c;
        }
    }
}
