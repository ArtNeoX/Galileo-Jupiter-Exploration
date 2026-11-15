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
            _probe = AtmosProbe(Root, new Vector2(170, 260));
            // Under the probe body (left of the canvas centre in the orbiter art).
            _probe.anchoredPosition = _orbiter.anchoredPosition + new Vector2(-_orbiter.sizeDelta.x * 0.2f, -_orbiter.sizeDelta.y * 0.22f);
            _probe.SetSiblingIndex(_orbiter.GetSiblingIndex()); // behind the orbiter

            _separate = UIFactory.Button("Separate", Root, data.separate, Release);
            _separate.GetComponent<RectTransform>().Pin(new Vector2(0.5f, 0), new Vector2(380, 86), new Vector2(0, 70));
            _separate.transform.Find("Label").GetComponent<Text>().fontSize = 36;
        }

        static Sprite _probeSprite;

        /// <summary>
        /// atmospheric_probe.png from Resources/Galileo/Episode2/, cropped to its visible pixels (it may be painted
        /// on a large canvas), or the probe icon. <paramref name="size"/> is the box it is fitted into.
        /// </summary>
        public static RectTransform AtmosProbe(Transform parent, Vector2 size)
        {
            if (_probeSprite == null)
            {
                var full = ContentLoader.LoadSprite("Galileo/Episode2/atmospheric_probe");
                if (full != null)
                {
                    var b = ProbeAssembly.VisibleBounds(full);
                    var r = full.rect;
                    var px = Rect.MinMaxRect(Mathf.Floor(r.x + b.xMin * r.width), Mathf.Floor(r.y + b.yMin * r.height),
                        Mathf.Min(r.xMax, Mathf.Ceil(r.x + b.xMax * r.width)), Mathf.Min(r.yMax, Mathf.Ceil(r.y + b.yMax * r.height)));
                    _probeSprite = Sprite.Create(full.texture, px, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                }
            }
            var sprite = _probeSprite;
            var img = UIFactory.Picture("Atmospheric Probe", parent, sprite != null ? sprite : ProceduralArt.Icon("probe"),
                sprite != null ? Color.white : Theme.Accent);
            if (sprite != null)
            {
                // Fit the picture's own proportions into the box.
                float aspect = sprite.rect.width / sprite.rect.height;
                size = aspect > size.x / size.y ? new Vector2(size.x, size.x / aspect) : new Vector2(size.y * aspect, size.y);
            }
            img.rectTransform.Pin(new Vector2(0.5f, 0.5f), size, Vector2.zero);
            // A dark edge so the pale probe reads on the cream clouds and on the planet.
            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.1f, 0.08f, 0.85f);
            outline.effectDistance = new Vector2(2, -2);
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
