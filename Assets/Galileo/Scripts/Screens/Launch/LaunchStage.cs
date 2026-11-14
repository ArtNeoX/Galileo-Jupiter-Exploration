using System.Collections;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Full-screen launch pad picture made of bitmap layers from Resources/Galileo/Launch/:
    ///   background_good, background_bad   — the pad in clear and in bad weather
    ///   shuttle, shuttle_afterburner      — the shuttle without and with engines firing
    /// All four are drawn full screen (cropped to fill, like the office renders), so paint them at the same
    /// size with transparency around the shuttle. Missing bitmaps are replaced by simple placeholders.
    /// Lives under the screens so it stays visible while launch dialogs play on top of it.
    /// </summary>
    public class LaunchStage : MonoBehaviour
    {
        const string Folder = "Galileo/Launch/";

        LaunchData _data;
        CanvasGroup _bad, _burn;
        RectTransform _shuttleFrame;
        Image _cross;
        Coroutine _weatherFade, _shake;

        public static LaunchStage Create(RectTransform parent, LaunchData data)
        {
            var rt = UIFactory.Rect("Launch Stage", parent).Stretch();
            var stage = rt.gameObject.AddComponent<LaunchStage>();
            stage._data = data;
            stage.Build(rt);
            return stage;
        }

        void Build(RectTransform root)
        {
            Layer(root, "background_good", BuildGoodWeather);
            _bad = Layer(root, "background_bad", BuildBadWeather).gameObject.AddComponent<CanvasGroup>();

            var shuttleRoot = UIFactory.Rect("Shuttle", root).Stretch();
            var shuttle = LoadSprite("shuttle");
            var burning = LoadSprite("shuttle_afterburner");
            if (shuttle != null)
            {
                _shuttleFrame = FullScreen("shuttle", shuttleRoot, shuttle).rectTransform;
                var burnRoot = UIFactory.Rect("Afterburner", shuttleRoot).Stretch();
                if (burning != null) FullScreen("shuttle_afterburner", burnRoot, burning);
                _burn = burnRoot.gameObject.AddComponent<CanvasGroup>();
            }
            else
            {
                // Placeholder: a cream silhouette standing on the pad; flames appear underneath it.
                _shuttleFrame = UIFactory.Rect("Frame", shuttleRoot).Stretch();
                var body = UIFactory.Rect("Placeholder", _shuttleFrame);
                body.Pin(new Vector2(0.5f, 0f), new Vector2(380, 600), new Vector2(0, 400));
                var flames = UIFactory.Picture("Flames", body, ProceduralArt.Shape("shuttle_flames", 512), new Color(1f, 0.62f, 0.22f));
                flames.preserveAspect = false;
                flames.rectTransform.Stretch(0, 540, 0, -540);
                _burn = flames.gameObject.AddComponent<CanvasGroup>();
                var art = UIFactory.Picture("Body", body, ProceduralArt.Shape("shuttle", 512), new Color(0.95f, 0.93f, 0.88f));
                art.preserveAspect = false;
                art.rectTransform.Stretch();
                if (burning != null) // only the afterburner bitmap exists: show it instead of the placeholder flames
                {
                    var burnRoot = UIFactory.Rect("Afterburner", shuttleRoot).Stretch();
                    FullScreen("shuttle_afterburner", burnRoot, burning);
                    Destroy(_burn);
                    _burn = burnRoot.gameObject.AddComponent<CanvasGroup>();
                }
            }

            _cross = UIFactory.Picture("EngineFailure", _shuttleFrame, ProceduralArt.Icon("x_mark"), new Color(0.92f, 0.16f, 0.14f));
            _cross.rectTransform.anchorMin = _cross.rectTransform.anchorMax = new Vector2(_data.crossX, _data.crossY);
            _cross.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _cross.rectTransform.sizeDelta = new Vector2(130, 130);
            _cross.rectTransform.anchoredPosition = Vector2.zero;

            ResetState();
        }

        /// <summary>Clear weather, engines off, no failure mark.</summary>
        public void ResetState()
        {
            if (_weatherFade != null) StopCoroutine(_weatherFade);
            if (_shake != null) StopCoroutine(_shake);
            _bad.alpha = 0f;
            _burn.alpha = 0f;
            _cross.gameObject.SetActive(false);
            transform.localPosition = Vector3.zero;
        }

        public void SetBadWeather(float seconds = 1.6f)
        {
            if (_weatherFade != null) StopCoroutine(_weatherFade);
            _weatherFade = StartCoroutine(FadeGroup(_bad, 1f, seconds));
        }

        public void ShowEngineFailure() => StartCoroutine(PopCross());

        public void Ignite()
        {
            StartCoroutine(FadeGroup(_burn, 1f, 0.25f));
            if (_shake != null) StopCoroutine(_shake);
            _shake = StartCoroutine(Shake(1.8f));
        }

        IEnumerator PopCross()
        {
            _cross.gameObject.SetActive(true);
            var rt = _cross.rectTransform;
            const float duration = 0.35f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                rt.localScale = Vector3.one * (k < 0.7f ? Mathf.Lerp(0f, 1.25f, k / 0.7f) : Mathf.Lerp(1.25f, 1f, (k - 0.7f) / 0.3f));
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        static IEnumerator FadeGroup(CanvasGroup g, float to, float duration)
        {
            float from = g.alpha;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            g.alpha = to;
        }

        IEnumerator Shake(float duration)
        {
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float strength = 6f * (1f - t / duration);
                transform.localPosition = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * strength;
                yield return null;
            }
            transform.localPosition = Vector3.zero;
            _shake = null;
        }

        // ---------- Layers ----------

        static Sprite LoadSprite(string name) => ContentLoader.LoadSprite(Folder + name);

        /// <summary>A full-screen layer showing the named bitmap, or the placeholder if it doesn't exist.</summary>
        static RectTransform Layer(RectTransform parent, string name, System.Action<RectTransform> placeholder)
        {
            var layer = UIFactory.Rect(name, parent).Stretch();
            layer.gameObject.AddComponent<RectMask2D>();
            var sprite = LoadSprite(name);
            if (sprite != null) FullScreen(name, layer, sprite);
            else placeholder(layer);
            return layer;
        }

        /// <summary>Bitmap scaled to cover the whole screen, keeping its proportions.</summary>
        static Image FullScreen(string name, RectTransform parent, Sprite sprite)
        {
            var img = UIFactory.Picture(name, parent, sprite, Color.white);
            img.preserveAspect = false;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            return img;
        }

        static void BuildGoodWeather(RectTransform layer) =>
            BuildPad(layer, new Color(0.45f, 0.66f, 0.85f), new Color(0.82f, 0.89f, 0.93f), new Color(0.25f, 0.3f, 0.24f), false);

        static void BuildBadWeather(RectTransform layer) =>
            BuildPad(layer, new Color(0.2f, 0.22f, 0.25f), new Color(0.42f, 0.44f, 0.47f), new Color(0.16f, 0.18f, 0.16f), true);

        static void BuildPad(RectTransform layer, Color skyTop, Color skyBottom, Color ground, bool clouds)
        {
            var sky = UIFactory.Picture("Sky", layer, ProceduralArt.VerticalGradient(skyTop, skyBottom), Color.white);
            sky.preserveAspect = false;
            sky.rectTransform.Stretch();
            var land = UIFactory.Line(layer, ground);
            land.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 400));

            // Launch tower to the right of the shuttle
            var towerColor = Color.Lerp(ground, Color.gray, 0.5f);
            UIFactory.Line(layer, towerColor).rectTransform.Pin(new Vector2(0.5f, 0f), new Vector2(70, 660), new Vector2(250, 400));
            for (int i = 0; i < 6; i++)
                UIFactory.Line(layer, towerColor).rectTransform.Pin(new Vector2(0.5f, 0f), new Vector2(110, 8), new Vector2(200, 460 + i * 100));

            if (!clouds) return;
            var cloud = ProceduralArt.Shape("cloud", 256);
            var rng = new System.Random(17);
            for (int i = 0; i < 9; i++)
            {
                var c = UIFactory.Picture("Cloud", layer, cloud, new Color(0.13f, 0.14f, 0.16f, 0.88f));
                float w = 420 + (float)rng.NextDouble() * 360;
                c.rectTransform.Pin(new Vector2((float)rng.NextDouble(), 0.62f + (float)rng.NextDouble() * 0.4f), new Vector2(w, w * 0.6f), Vector2.zero);
            }
        }
    }
}
