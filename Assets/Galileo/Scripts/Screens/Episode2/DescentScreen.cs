using System.Collections;
using System.Collections.Generic;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Descent through Jupiter's atmosphere (side view: cloud bands rush upwards past the falling probe).
    /// After "3 2 1" a one-minute timer starts in the top-right corner and gas samples (H₂, He, CH₄…) pop up one by one
    /// at random places, each for about a second. A missed sample comes back later. Once all are collected,
    /// SEND DATA appears; pressing it before the time is up wins the mission, otherwise it's game over.
    /// </summary>
    public class DescentScreen : ScreenBase
    {
        const int MaxActive = 3;
        const float SampleSize = 130f;

        class Sample
        {
            public string gas;
            public RectTransform rect;
            public Image ring;
            public float age;
        }

        RawImage _clouds, _streaks;
        RectTransform _probe;
        Text _countdown, _timer, _counter;
        Button _send;
        readonly Queue<string> _queue = new Queue<string>();
        readonly List<Sample> _active = new List<Sample>();
        float _timeLeft, _spawnIn;
        int _collected, _total;
        bool _running, _finished;

        public override bool AllowLanguageToggle => false; // timed mini-game

        protected override void Build()
        {
            var data = Content.episode2;

            _clouds = UIFactory.Rect("Atmosphere", Root).Stretch().gameObject.AddComponent<RawImage>();
            _clouds.texture = CloudTexture();
            _clouds.raycastTarget = false;
            _clouds.uvRect = new Rect(0, 0, 1, 1.4f);
            _streaks = UIFactory.Rect("Streaks", Root).Stretch().gameObject.AddComponent<RawImage>();
            _streaks.texture = StreakTexture();
            _streaks.raycastTarget = false;
            _streaks.uvRect = new Rect(0, 0, 2, 1);

            _probe = SeparationScreen.AtmosProbe(Root, new Vector2(300, 300));

            var shade = UIFactory.Picture("TopShade", Root, ProceduralArt.VerticalGradient(new Color(0, 0, 0, 0.55f), new Color(0, 0, 0, 0)), Color.white);
            shade.preserveAspect = false;
            shade.rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -200), Vector2.zero);

            _counter = UIFactory.Label("Counter", Root, "", 34, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            _counter.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0.4f, 1), new Vector2(50, -100), new Vector2(0, -36));
            UIFactory.Label("Hint", Root, data.collectHint, 26, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Italic).BestFit(16, 26)
                .rectTransform.Anchor(new Vector2(0.3f, 1), new Vector2(0.7f, 1), new Vector2(0, -100), new Vector2(0, -36));
            _timer = UIFactory.Label("Timer", Root, "", 56, Theme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            _timer.rectTransform.Anchor(new Vector2(0.6f, 1), Vector2.one, new Vector2(0, -110), new Vector2(-50, -30));
            foreach (var t in new[] { _counter, _timer }) t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            _countdown = UIFactory.Label("Countdown", Root, "", 200, Theme.AccentBright, TextAnchor.MiddleCenter, FontStyle.Bold);
            _countdown.horizontalOverflow = HorizontalWrapMode.Overflow;
            _countdown.verticalOverflow = VerticalWrapMode.Overflow;
            _countdown.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(600, 300), Vector2.zero);
            _countdown.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5, -5);

            _send = UIFactory.Button("Send", Root, data.sendData, Send);
            _send.GetComponent<RectTransform>().Pin(new Vector2(0.5f, 0), new Vector2(420, 96), new Vector2(0, 70));
            _send.transform.Find("Label").GetComponent<Text>().fontSize = 40;
            _send.gameObject.SetActive(false);

            // The sample queue: the gases repeated until there are enough, shuffled.
            var gases = data.gases != null && data.gases.Length > 0 ? data.gases : new[] { "H₂" };
            _total = Mathf.Max(1, data.sampleCount);
            var list = new List<string>();
            for (int i = 0; i < _total; i++) list.Add(gases[i % gases.Length]);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            foreach (var g in list) _queue.Enqueue(g);

            _timeLeft = data.timeLimit;
            RefreshHud();
            _timer.enabled = false;
            StartCoroutine(Countdown());
        }

        IEnumerator Countdown()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            for (int n = 3; n >= 1; n--)
            {
                _countdown.text = n.ToString();
                GameAudio.Play("click");
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime)
                {
                    float s = Mathf.Lerp(1.5f, 1f, Mathf.Clamp01(t / 0.25f));
                    _countdown.rectTransform.localScale = new Vector3(s, s, 1);
                    var c = _countdown.color;
                    c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                    _countdown.color = c;
                    yield return null;
                }
            }
            _countdown.text = "";
            _timer.enabled = true;
            _running = true;
            _spawnIn = 0.2f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // The atmosphere rushes upwards past the falling probe.
            var uv = _clouds.uvRect;
            uv.y -= dt * 0.18f;
            _clouds.uvRect = uv;
            var su = _streaks.uvRect;
            su.y -= dt * 1.6f;
            _streaks.uvRect = su;
            float time = Time.unscaledTime;
            _probe.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 1.7f) * 6f);
            _probe.anchoredPosition = new Vector2(Mathf.Sin(time * 0.8f) * 24f, Mathf.Sin(time * 2.3f) * 10f);

            if (!_running || _finished) return;

            _timeLeft -= dt;
            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                RefreshHud();
                Fail();
                return;
            }

            _spawnIn -= dt;
            if (_spawnIn <= 0f && _queue.Count > 0 && _active.Count < MaxActive)
            {
                Spawn(_queue.Dequeue());
                _spawnIn = Random.Range(0.55f, 0.95f);
            }

            float life = Mathf.Max(0.2f, Content.episode2.sampleSeconds);
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var s = _active[i];
                s.age += dt;
                s.ring.fillAmount = 1f - s.age / life;
                float appear = Mathf.Clamp01(s.age / 0.12f);
                s.rect.localScale = new Vector3(appear, appear, 1);
                if (s.age >= life)
                {
                    _active.RemoveAt(i);
                    _queue.Enqueue(s.gas); // missed: it comes back later
                    StartCoroutine(Vanish(s.rect, false));
                }
            }
            RefreshHud();
        }

        void RefreshHud()
        {
            _counter.text = string.Format(Content.episode2.collectedFormat, _collected, _total);
            int seconds = Mathf.CeilToInt(_timeLeft);
            _timer.text = $"{seconds / 60:00}:{seconds % 60:00}";
            _timer.color = _timeLeft <= 10f ? Theme.Failure : Theme.Accent;
        }

        // ---------- Samples ----------

        void Spawn(string gas)
        {
            var size = Root.rect.size;
            var half = size / 2f - new Vector2(SampleSize, SampleSize);
            Vector2 pos = Vector2.zero;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                pos = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y + 60f, half.y - 120f));
                if (Vector2.Distance(pos, _probe.anchoredPosition) < 230f) continue;
                bool clear = true;
                foreach (var other in _active)
                    if (Vector2.Distance(other.rect.anchoredPosition, pos) < SampleSize * 1.3f) clear = false;
                if (clear) break;
            }

            var color = GasColor(gas);
            var disc = UIFactory.Picture("Sample " + gas, Root, ProceduralArt.Icon("disc"), new Color(color.r, color.g, color.b, 0.92f));
            disc.raycastTarget = true;
            var rt = disc.rectTransform;
            rt.Pin(new Vector2(0.5f, 0.5f), new Vector2(SampleSize, SampleSize), pos);
            var ring = UIFactory.Picture("Timer", rt, ProceduralArt.Icon("ring"), Color.white);
            ring.rectTransform.Stretch(-12, -12, -12, -12);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            var label = UIFactory.Label("Gas", rt, gas, 40, new Color(0.12f, 0.08f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.rectTransform.Stretch();
            rt.localScale = Vector3.zero;

            var sample = new Sample { gas = gas, rect = rt, ring = ring };
            disc.gameObject.AddComponent<PressCatcher>().Pressed += () => Collect(sample);
            _active.Add(sample);
        }

        void Collect(Sample sample)
        {
            if (!_running || _finished || !_active.Remove(sample)) return;
            _collected++;
            GameAudio.Play("coins", 0.8f);
            StartCoroutine(Vanish(sample.rect, true));
            RefreshHud();
            if (_collected >= _total) AllCollected();
        }

        void AllCollected()
        {
            foreach (var s in _active) StartCoroutine(Vanish(s.rect, false));
            _active.Clear();
            _queue.Clear();
            _send.gameObject.SetActive(true);
            StartCoroutine(PulseSend());
        }

        IEnumerator PulseSend()
        {
            var rt = _send.GetComponent<RectTransform>();
            while (_send != null && !_finished)
            {
                float s = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 6f);
                rt.localScale = new Vector3(s, s, 1);
                yield return null;
            }
        }

        static IEnumerator Vanish(RectTransform rt, bool collected)
        {
            foreach (var g in rt.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            const float duration = 0.25f;
            var graphics = rt.GetComponentsInChildren<Graphic>();
            float start = rt.localScale.x;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float s = collected ? Mathf.Lerp(1f, 1.6f, k) : Mathf.Lerp(start, 0f, k);
                rt.localScale = new Vector3(s, s, 1);
                foreach (var g in graphics)
                {
                    var c = g.color;
                    c.a = Mathf.Min(c.a, 1f - k);
                    g.color = c;
                }
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        static Color GasColor(string gas)
        {
            switch (gas)
            {
                case "H₂": return new Color(0.62f, 0.82f, 1f);
                case "He": return new Color(1f, 0.86f, 0.45f);
                case "CH₄": return new Color(0.55f, 0.9f, 0.62f);
                case "NH₃": return new Color(0.85f, 0.7f, 1f);
                case "H₂O": return new Color(0.45f, 0.7f, 1f);
                case "H₂S": return new Color(0.95f, 0.95f, 0.5f);
                case "C₂H₆": return new Color(1f, 0.65f, 0.5f);
                case "C₂H₂": return new Color(1f, 0.55f, 0.75f);
                default: return Theme.AccentBright;
            }
        }

        // ---------- End ----------

        void Send()
        {
            if (_finished) return;
            _finished = true;
            _running = false;
            App.MissionSuccess();
        }

        void Fail()
        {
            if (_finished) return;
            _finished = true;
            _running = false;
            App.GameOver();
        }

        /// <summary>Dev shortcut L: collects every sample.</summary>
        public void DevCollectAll()
        {
            if (!_running || _finished) return;
            _collected = _total;
            RefreshHud();
            AllCollected();
        }

        // ---------- Procedural atmosphere ----------

        /// <summary>Jupiter-like cloud bands with turbulence; tiles vertically.</summary>
        static Texture2D CloudTexture()
        {
            const int w = 128, h = 512;
            var palette = new[]
            {
                new Color(0.93f, 0.86f, 0.74f), new Color(0.82f, 0.64f, 0.46f), new Color(0.70f, 0.45f, 0.30f),
                new Color(0.96f, 0.92f, 0.84f), new Color(0.78f, 0.55f, 0.38f), new Color(0.58f, 0.36f, 0.26f),
                new Color(0.88f, 0.76f, 0.60f), new Color(0.66f, 0.50f, 0.40f)
            };
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                // Periodic in v (and u) so the texture tiles seamlessly.
                float warp = 0.035f * Mathf.Sin(2 * Mathf.PI * (u * 2f + v * 3f)) + 0.02f * Mathf.Sin(2 * Mathf.PI * (u * 5f - v * 7f));
                float band = Mathf.Repeat((v + warp) * palette.Length, palette.Length);
                int i0 = Mathf.FloorToInt(band) % palette.Length, i1 = (i0 + 1) % palette.Length;
                float f = Mathf.SmoothStep(0f, 1f, band - Mathf.Floor(band));
                var c = Color.Lerp(palette[i0], palette[i1], f);
                float grain = 0.06f * Mathf.Sin(2 * Mathf.PI * (u * 9f + v * 23f)) * Mathf.Sin(2 * Mathf.PI * (u * 4f - v * 11f));
                pixels[y * w + x] = new Color(c.r + grain, c.g + grain, c.b + grain, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }

        /// <summary>Thin light streaks that sell the speed of the fall.</summary>
        static Texture2D StreakTexture()
        {
            const int w = 256, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            var rng = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                int x = rng.Next(w), y0 = rng.Next(h), len = 30 + rng.Next(90);
                float a = 0.15f + (float)rng.NextDouble() * 0.3f;
                for (int k = 0; k < len; k++)
                {
                    int y = (y0 + k) % h;
                    float fade = Mathf.Sin(Mathf.PI * k / len);
                    pixels[y * w + x] = new Color(1f, 0.97f, 0.9f, a * fade);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }
    }
}
