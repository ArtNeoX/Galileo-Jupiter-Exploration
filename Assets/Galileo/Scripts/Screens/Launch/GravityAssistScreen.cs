using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Galileo.Content;
using Galileo.Space;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Gravity assist: heliocentric map of the Sun, Venus, Earth and Jupiter at their real positions on the
    /// chosen date, plus Galileo's VEEGA trajectory (Venus–Earth–Earth gravity assists). Two handles on the
    /// timeline set the start and end of the launch window; they turn greener the closer they get to the real
    /// window (12 Oct – 21 Nov 1989) and flash white when exactly right. With both right the trajectory turns
    /// green and Next appears.
    /// </summary>
    public class GravityAssistScreen : ScreenBase
    {
        // Galileo's real flybys after the 18 Oct 1989 launch. Bulge (AU) shapes the drawn arc of each leg:
        // between the two Earth flybys Galileo went out to ~2.3 AU (past the asteroid Gaspra).
        static readonly (Planet planet, DateTime date, float bulge, float minSweep)[] Legs =
        {
            (Planet.Venus, new DateTime(1990, 2, 10), -0.06f, 30f),
            (Planet.Earth, new DateTime(1990, 12, 8), 0.30f, 180f),
            (Planet.Earth, new DateTime(1992, 12, 8), 1.25f, 300f),
            (Planet.Jupiter, new DateTime(1995, 12, 7), 0.30f, 90f),
        };

        static readonly Planet[] Planets = { Planet.Venus, Planet.Earth, Planet.Jupiter };
        static readonly Color Green = new Color(0.42f, 0.89f, 0.54f);
        static readonly Color SunColor = new Color(1f, 0.85f, 0.42f);
        static readonly Color TrajectoryWhite = new Color(1f, 1f, 1f, 0.8f);

        // Map: angles are exact; distances use a log scale so Venus and Earth stay readable next to Jupiter.
        static readonly Vector2 MapCenter = new Vector2(170, 50);
        const float MapRadius = 400f;
        const float MaxAU = 5.46f;
        const float TrackWidth = 1200f;

        class Handle
        {
            public RectTransform rt;
            public Image ring, flash;
            public Text date;
        }

        GravityAssistData _data;
        DateTime _rangeStart;
        int _days, _correctStart, _correctEnd, _start, _end;
        bool _startOk, _endOk;

        RectTransform _map, _track, _earthEnd;
        readonly Dictionary<Planet, (RectTransform marker, Text label)> _planets =
            new Dictionary<Planet, (RectTransform, Text)>();
        UILine _window, _trajectory, _between;
        Text _positionsDate;
        Handle _startHandle, _endHandle;
        CanvasGroup _next;

        protected override void Build()
        {
            _data = Content.gravityAssist;
            _rangeStart = ParseDate(_data.rangeStart);
            _days = (int)(ParseDate(_data.rangeEnd) - _rangeStart).TotalDays + 1;
            _correctStart = (int)(ParseDate(_data.windowStart) - _rangeStart).TotalDays;
            _correctEnd = (int)(ParseDate(_data.windowEnd) - _rangeStart).TotalDays;
            _start = 0;
            _end = _days - 1;

            var black = Root.gameObject.AddComponent<Image>();
            black.color = Color.black;
            black.raycastTarget = false;

            BuildStars();
            BuildMap();
            BuildHeader();
            BuildTimeline();

            var next = NextButton(App.StartLaunch);
            _next = next.gameObject.AddComponent<CanvasGroup>();
            next.gameObject.SetActive(false);

            Refresh(false);
        }

        // ---------- Map ----------

        void BuildStars()
        {
            var rng = new System.Random(1989);
            var dot = ProceduralArt.Icon("disc");
            for (int i = 0; i < 170; i++)
            {
                var star = UIFactory.Picture("Star", Root, dot, new Color(1, 1, 1, 0.15f + (float)rng.NextDouble() * 0.45f));
                float size = 1.5f + (float)rng.NextDouble() * 2.5f;
                var at = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                star.rectTransform.Pin(at, new Vector2(size, size), Vector2.zero);
            }
        }

        void BuildMap()
        {
            _map = UIFactory.Rect("Map", Root);
            _map.Pin(new Vector2(0.5f, 0.5f), Vector2.zero, MapCenter);

            foreach (var p in Planets)
            {
                var orbit = UILine.Create("Orbit " + p, _map, WithAlpha(ColorOf(p), 0.55f), 2f);
                var pts = new List<Vector2>();
                double period = Ephemeris.OrbitalPeriodDays(p);
                for (int i = 0; i < 360; i++)
                    pts.Add(Project(Ephemeris.Position(p, _rangeStart.AddDays(period * i / 360.0))));
                orbit.SetPoints(pts, true);
            }

            var glow = UIFactory.Picture("SunGlow", _map, ProceduralArt.Glow(), WithAlpha(SunColor, 0.55f));
            glow.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(110, 110), Vector2.zero);
            var sun = UIFactory.Picture("Sun", _map, ProceduralArt.Icon("disc"), SunColor);
            sun.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(22, 22), Vector2.zero);
            MapLabel(_data.sun, SunColor, new Vector2(18, -22), 22, false);

            // Flyby points: where Galileo actually met each planet.
            var used = new List<(Vector2 pos, Text text)>();
            foreach (var leg in Legs)
            {
                var pos = Project(Ephemeris.Position(leg.planet, leg.date));
                var ghost = UIFactory.Picture("Flyby " + leg.planet, _map, ProceduralArt.Icon("disc"), WithAlpha(ColorOf(leg.planet), 0.75f));
                ghost.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(9, 9), pos);
                string text = FormatDate(leg.date, true);
                int near = used.FindIndex(u => (u.pos - pos).magnitude < 30f);
                if (near >= 0)
                {
                    used[near].text.text += "\n" + text;
                    continue;
                }
                bool left = pos.x < 0;
                var label = MapLabel(text, WithAlpha(ColorOf(leg.planet), 0.7f),
                    pos + pos.normalized * 14f + new Vector2(left ? -6 : 6, 0), 17, left);
                used.Add((pos, label));
            }

            _window = UILine.Create("Window", _map, ColorOf(Planet.Earth), 6f);
            _trajectory = UILine.Create("Trajectory", _map, TrajectoryWhite, 3f);

            _earthEnd = UIFactory.Picture("Earth (window end)", _map, ProceduralArt.Icon("ring"), WithAlpha(ColorOf(Planet.Earth), 0.5f)).rectTransform;
            _earthEnd.Pin(new Vector2(0.5f, 0.5f), new Vector2(18, 18), Vector2.zero);

            foreach (var p in Planets)
            {
                var marker = UIFactory.Picture(p.ToString(), _map, ProceduralArt.Icon("ring"), ColorOf(p));
                marker.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(26, 26), Vector2.zero);
                var name = p == Planet.Venus ? _data.venus : p == Planet.Earth ? _data.earth : _data.jupiter;
                _planets[p] = (marker.rectTransform, MapLabel(name, ColorOf(p), Vector2.zero, 22, false));
            }
        }

        Text MapLabel(string text, Color color, Vector2 pos, int size, bool rightAligned)
        {
            var label = UIFactory.Label("Label", _map, (text ?? "").ToUpperInvariant(), size, color, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(10, 30), pos);
            PlaceLabel(label, pos, rightAligned);
            return label;
        }

        static void PlaceLabel(Text label, Vector2 pos, bool rightAligned)
        {
            label.rectTransform.pivot = new Vector2(rightAligned ? 1f : 0f, 0.5f);
            label.alignment = rightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            label.rectTransform.anchoredPosition = pos;
        }

        void BuildHeader()
        {
            var title = UIFactory.Label("Title", Root, _data.title.ToUpperInvariant(), 40, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold)
                .BestFit(26, 40);
            title.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -110), new Vector2(660, -50));
            var body = (_data.subtitle ?? "") + (string.IsNullOrEmpty(_data.hint) ? "" : "\n\n" + _data.hint);
            UIFactory.Label("Subtitle", Root, body, 23, Theme.TextMuted).BestFit(16, 23)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -600), new Vector2(640, -125));

            _positionsDate = UIFactory.Label("PositionsDate", Root, "", 24, Theme.TextMuted, TextAnchor.UpperRight, FontStyle.Bold);
            _positionsDate.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-700, -100), new Vector2(-70, -55));
        }

        // ---------- Timeline ----------

        void BuildTimeline()
        {
            _track = UIFactory.Rect("Timeline", Root);
            _track.Pin(new Vector2(0.5f, 0f), new Vector2(TrackWidth, 80), new Vector2(-120, 125));
            _track.pivot = new Vector2(0.5f, 0.5f);
            _track.anchoredPosition = new Vector2(-120, 125);

            float half = TrackWidth / 2f;
            UILine.Create("Track", _track, new Color(1, 1, 1, 0.35f), 3f)
                .SetPoints(new[] { new Vector2(-half, 0), new Vector2(half, 0) });

            // Ticks at the range ends and at the start of each month
            for (int d = 0; d < _days; d++)
            {
                var date = _rangeStart.AddDays(d);
                if (date.Day != 1 && d != 0 && d != _days - 1) continue;
                float x = DayToX(d);
                UILine.Create("Tick", _track, new Color(1, 1, 1, 0.35f), 2f)
                    .SetPoints(new[] { new Vector2(x, -10), new Vector2(x, 10) });
                var t = UIFactory.Label("TickLabel", _track, FormatDate(date, false), 20, Theme.TextMuted, TextAnchor.UpperCenter);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(120, 30), new Vector2(x, -38));
            }

            _between = UILine.Create("Between", _track, new Color(1, 1, 1, 0.8f), 2f).Dashed(10, 8);

            _startHandle = MakeHandle("Start", d => { _start = Mathf.Clamp(d, 0, _end - 1); Refresh(true); });
            _endHandle = MakeHandle("End", d => { _end = Mathf.Clamp(d, _start + 1, _days - 1); Refresh(true); });
        }

        Handle MakeHandle(string name, Action<int> onDay)
        {
            var h = new Handle();
            var fill = UIFactory.Picture(name, _track, ProceduralArt.Icon("disc"), new Color(0.06f, 0.06f, 0.06f, 1f));
            fill.raycastTarget = true;
            h.rt = fill.rectTransform;
            h.rt.Pin(new Vector2(0.5f, 0.5f), new Vector2(46, 46), Vector2.zero);
            h.ring = UIFactory.Picture("Ring", h.rt, ProceduralArt.Icon("ring"), Color.white);
            h.ring.rectTransform.Stretch();
            h.flash = UIFactory.Picture("Flash", h.rt, ProceduralArt.Icon("disc"), new Color(1, 1, 1, 0));
            h.flash.rectTransform.Stretch();
            h.date = UIFactory.Label("Date", h.rt, "", 22, Color.white, TextAnchor.LowerCenter, FontStyle.Bold);
            h.date.horizontalOverflow = HorizontalWrapMode.Overflow;
            h.date.rectTransform.Pin(new Vector2(0.5f, 1f), new Vector2(160, 30), new Vector2(0, 36));

            fill.gameObject.AddComponent<DragHandle>().Dragged += e =>
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_track, e.position, e.pressEventCamera, out var local);
                onDay(XToDay(local.x));
            };
            return h;
        }

        float DayToX(int day) => -TrackWidth / 2f + TrackWidth * day / (_days - 1f);

        int XToDay(float x) => Mathf.Clamp(Mathf.RoundToInt((x / TrackWidth + 0.5f) * (_days - 1)), 0, _days - 1);

        /// <summary>Sets both handles at once (used by the dev shortcut).</summary>
        public void SetWindow(int startDay, int endDay)
        {
            _start = startDay;
            _end = endDay;
            Refresh(true);
        }

        public int CorrectStartDay => _correctStart;
        public int CorrectEndDay => _correctEnd;

        // ---------- State ----------

        void Refresh(bool interactive)
        {
            var startDate = _rangeStart.AddDays(_start);
            var endDate = _rangeStart.AddDays(_end);

            foreach (var kv in _planets)
            {
                var pos = Project(Ephemeris.Position(kv.Key, startDate));
                kv.Value.marker.anchoredPosition = pos;
                var dir = pos.normalized;
                PlaceLabel(kv.Value.label, pos + dir * 20f + new Vector2(dir.x >= 0 ? 8 : -8, 0), dir.x < 0);
            }
            _earthEnd.anchoredPosition = Project(Ephemeris.Position(Planet.Earth, endDate));

            var arc = new List<Vector2>();
            for (int d = 0; d <= _end - _start; d++)
                arc.Add(Project(Ephemeris.Position(Planet.Earth, startDate.AddDays(d))));
            _window.SetPoints(arc);

            _trajectory.SetPoints(Trajectory(startDate));
            _positionsDate.text = string.Format(_data.positionsFormat, FormatDate(startDate, true));

            _startHandle.rt.anchoredPosition = new Vector2(DayToX(_start), 0);
            _endHandle.rt.anchoredPosition = new Vector2(DayToX(_end), 0);
            _between.SetPoints(new[] { new Vector2(DayToX(_start) + 26, 0), new Vector2(DayToX(_end) - 26, 0) });
            _startHandle.date.text = FormatDate(startDate, false);
            _endHandle.date.text = FormatDate(endDate, false);

            bool startOk = _start == _correctStart, endOk = _end == _correctEnd;
            Tint(_startHandle, _start - _correctStart);
            Tint(_endHandle, _end - _correctEnd);
            if (interactive && startOk && !_startOk) StartCoroutine(Flash(_startHandle));
            if (interactive && endOk && !_endOk) StartCoroutine(Flash(_endHandle));
            _startOk = startOk;
            _endOk = endOk;

            bool done = startOk && endOk;
            _trajectory.color = done ? Green : TrajectoryWhite;
            if (done && !_next.gameObject.activeSelf)
            {
                _next.gameObject.SetActive(true);
                _next.alpha = 0f;
                StartCoroutine(Fade(_next, 0f, 1f, 0.4f));
            }
            else if (!done) _next.gameObject.SetActive(false);
        }

        void Tint(Handle h, int daysOff)
        {
            float closeness = 1f - Mathf.Clamp01(Mathf.Abs(daysOff) / (float)Mathf.Max(1, _data.colorRangeDays));
            var c = daysOff == 0 ? Green : Color.Lerp(Color.white, Green, closeness * 0.8f);
            h.ring.color = c;
            h.date.color = c;
        }

        static IEnumerator Flash(Handle h)
        {
            const float duration = 0.55f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                h.flash.color = new Color(1, 1, 1, 1f - k);
                h.flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 2.8f, k);
                yield return null;
            }
            h.flash.color = new Color(1, 1, 1, 0);
            h.flash.rectTransform.localScale = Vector3.one;
        }

        // ---------- Geometry ----------

        static Vector2 Project(Vector2 au)
        {
            float r = au.magnitude;
            if (r < 1e-6f) return Vector2.zero;
            return au / r * (MapRadius * Mathf.Log(1f + r) / Mathf.Log(1f + MaxAU));
        }

        /// <summary>VEEGA path from Earth on the launch date through Galileo's real flyby points.</summary>
        static List<Vector2> Trajectory(DateTime launch)
        {
            var pts = new List<Vector2>();
            var from = Ephemeris.Position(Planet.Earth, launch);
            pts.Add(Project(from));
            foreach (var leg in Legs)
            {
                var to = Ephemeris.Position(leg.planet, leg.date);
                float r0 = from.magnitude, r1 = to.magnitude;
                float a0 = Mathf.Atan2(from.y, from.x) * Mathf.Rad2Deg;
                float sweep = Mathf.Repeat(Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg - a0, 360f);
                if (sweep < leg.minSweep) sweep += 360f;
                int steps = Mathf.CeilToInt(sweep / 1.5f) + 2;
                for (int i = 1; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    float a = (a0 + sweep * t) * Mathf.Deg2Rad;
                    float r = Mathf.Lerp(r0, r1, Mathf.SmoothStep(0f, 1f, t)) + leg.bulge * Mathf.Sin(Mathf.PI * t);
                    pts.Add(Project(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r));
                }
                from = to;
            }
            return pts;
        }

        static Color ColorOf(Planet p) =>
            p == Planet.Venus ? new Color(0.9f, 0.76f, 0.48f)
            : p == Planet.Earth ? new Color(0.35f, 0.66f, 0.9f)
            : new Color(0.85f, 0.6f, 0.42f);

        static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static DateTime ParseDate(string s) => DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        string FormatDate(DateTime d, bool withYear)
        {
            var m = _data.monthsShort != null && _data.monthsShort.Length == 12 ? _data.monthsShort[d.Month - 1] : d.Month.ToString("00");
            return withYear ? $"{d.Day} {m} {d.Year}" : $"{d.Day} {m}";
        }
    }
}
