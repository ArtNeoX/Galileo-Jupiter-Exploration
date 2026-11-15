using System.Collections;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// The assembled orbiter (as built in the garage) in front of jupiter_close, with the atmospheric probe attached.
    /// SEPARATE releases the probe towards the planet and starts the descent.
    /// </summary>
    public class SeparationScreen : ScreenBase
    {
        RectTransform _orbiter, _probe;
        Button _separate;
        bool _released;

        protected override void Build()
        {
            var data = Content.episode2;
            UIFactory.Label("Hint", Root, data.separationHint, 34, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).BestFit(20, 34)
                .rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(160, -150), new Vector2(-160, -60));
            Root.Find("Hint").gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            _orbiter = ProbeAssembly.Composite(Content.garage, App.GaragePlaced, Root, new Vector2(1000, 620), new Vector2(-260, 80));
            _probe = AtmosProbe(Root, new Vector2(170, 170));
            _probe.anchoredPosition = _orbiter.anchoredPosition + new Vector2(0, -_orbiter.sizeDelta.y * 0.42f);
            _probe.SetSiblingIndex(_orbiter.GetSiblingIndex()); // behind the orbiter

            _separate = UIFactory.Button("Separate", Root, data.separate, Release);
            _separate.GetComponent<RectTransform>().Pin(new Vector2(0.5f, 0), new Vector2(380, 86), new Vector2(0, 70));
            _separate.transform.Find("Label").GetComponent<Text>().fontSize = 36;
        }

        /// <summary>atmospheric_probe.png from Resources/Galileo/Episode2/, or the probe icon.</summary>
        public static RectTransform AtmosProbe(Transform parent, Vector2 size)
        {
            var sprite = ContentLoader.LoadSprite("Galileo/Episode2/atmospheric_probe");
            var img = UIFactory.Picture("Atmospheric Probe", parent, sprite != null ? sprite : ProceduralArt.Icon("probe"),
                sprite != null ? Color.white : Theme.Accent);
            img.rectTransform.Pin(new Vector2(0.5f, 0.5f), size, Vector2.zero);
            if (sprite == null) img.gameObject.AddComponent<Outline>().effectColor = new Color(0.15f, 0.1f, 0.08f, 0.9f);
            return img.rectTransform;
        }

        void Update()
        {
            if (_released || _orbiter == null) return;
            // A slow drift so the scene feels alive.
            float t = Time.unscaledTime;
            _orbiter.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.5f) * 1.2f);
        }

        void Release()
        {
            if (_released) return;
            _released = true;
            _separate.interactable = false;
            StartCoroutine(Fly());
        }

        IEnumerator Fly()
        {
            GameAudio.Play("popup");
            Vector2 from = _probe.anchoredPosition, orbiterFrom = _orbiter.anchoredPosition;
            var to = new Vector2(520, -420);
            const float duration = 2.6f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                _probe.anchoredPosition = Vector2.Lerp(from, to, k * k);
                float s = Mathf.Lerp(1f, 0.25f, k);
                _probe.localScale = new Vector3(s, s, 1);
                _probe.localRotation = Quaternion.Euler(0, 0, -30f * k);
                _orbiter.anchoredPosition = orbiterFrom + new Vector2(-30f, 40f) * k;
                yield return null;
            }
            App.ShowDescent();
        }
    }
}
