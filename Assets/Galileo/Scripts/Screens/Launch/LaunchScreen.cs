using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// One STS-34 launch attempt. A pointer sweeps along a curved bar with a yellow zone and a narrow green
    /// zone; Space / Enter / click stops it. The UTC clock runs through the real launch window.
    /// Missing both zones (on any date) brings up an angry Spehalski and the same date again. Otherwise:
    ///   engineFailure — red cross on the shuttle's right main engine, then a dialog, then the next date.
    ///   badWeather    — the pad turns stormy, then a dialog, then the next date.
    ///   success       — green: engines fire, the shuttle climbs, "Successful launch" card. Yellow: try again.
    /// </summary>
    public class LaunchScreen : ScreenBase
    {
        const float BarHalfWidth = 460f;
        const float BarSag = 42f;

        static readonly Color GreenZone = new Color(0.42f, 0.82f, 0.45f);
        static readonly Color YellowZone = new Color(0.95f, 0.8f, 0.35f);
        static readonly Color BarBase = new Color(0.93f, 0.9f, 0.84f);

        LaunchData _data;
        LaunchAttempt _attempt;
        RectTransform _bar, _pointer;
        Text _clock, _result;
        float _elapsed, _u, _greenCenter;
        bool _stopped;
        TimeSpan _utcStart;

        public override bool AllowLanguageToggle => false; // timed mini-game

        protected override void Build()
        {
            _data = Content.launch;
            _attempt = App.CurrentLaunch;
            _greenCenter = UnityEngine.Random.Range(0.35f, 0.65f);
            TimeSpan.TryParseExact(_attempt.utcStart ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out _utcStart);

            MakeClickable(Stop);
            BuildHeader();
            BuildBar();
            Place(0f);
        }

        void BuildHeader()
        {
            // Dark bands keep the text readable over any bitmap.
            var top = UIFactory.Picture("TopShade", Root, ProceduralArt.VerticalGradient(new Color(0, 0, 0, 0.6f), new Color(0, 0, 0, 0)), Color.white);
            top.preserveAspect = false;
            top.rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -230), Vector2.zero);
            var bottom = UIFactory.Picture("BottomShade", Root, ProceduralArt.VerticalGradient(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.65f)), Color.white);
            bottom.preserveAspect = false;
            bottom.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 330));

            UIFactory.Label("Date", Root, (_attempt.date ?? "").ToUpperInvariant(), 50, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(70, -120), new Vector2(0, -50));
            UIFactory.Label("Window", Root, string.Format(_data.windowFormat, _attempt.windowMinutes, _attempt.utcStart, _attempt.utcEnd),
                    26, Theme.Text, TextAnchor.UpperLeft)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(72, -170), new Vector2(0, -122));

            UIFactory.Label("UtcLabel", Root, _data.utcLabel, 24, Theme.TextMuted, TextAnchor.UpperRight, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(1, 1), Vector2.one, new Vector2(-400, -86), new Vector2(-70, -54));
            _clock = UIFactory.Label("Clock", Root, "", 50, Theme.Accent, TextAnchor.UpperRight, FontStyle.Bold);
            _clock.rectTransform.Anchor(new Vector2(1, 1), Vector2.one, new Vector2(-400, -150), new Vector2(-70, -84));
        }

        void BuildBar()
        {
            _bar = UIFactory.Rect("Bar", Root);
            _bar.Pin(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0, 150));

            Segment("Outline", 0f, 1f, new Color(0.15f, 0.12f, 0.1f), 40f);
            Segment("Base", 0f, 1f, BarBase, 30f);
            Segment("Yellow", _greenCenter - _data.yellowZone / 2f, _greenCenter + _data.yellowZone / 2f, YellowZone, 30f);
            Segment("Green", _greenCenter - _data.greenZone / 2f, _greenCenter + _data.greenZone / 2f, GreenZone, 30f);

            var pointer = UIFactory.Panel("Pointer", _bar, new Color(0.12f, 0.1f, 0.09f), 6);
            pointer.raycastTarget = false;
            _pointer = pointer.rectTransform;
            _pointer.Pin(new Vector2(0.5f, 0.5f), new Vector2(18, 76), Vector2.zero);
            UIFactory.Frame(_pointer, Theme.Accent, 6, 2);

            _result = UIFactory.Label("Result", Root, "", 40, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _result.horizontalOverflow = HorizontalWrapMode.Overflow;
            _result.rectTransform.Pin(new Vector2(0.5f, 0f), new Vector2(1200, 60), new Vector2(0, 250));

            var hint = UIFactory.Label("Hint", Root, _data.hint, 22, Theme.TextMuted, TextAnchor.MiddleCenter);
            hint.rectTransform.Pin(new Vector2(0.5f, 0f), new Vector2(1200, 40), new Vector2(0, 60));
        }

        void Segment(string name, float from, float to, Color color, float thickness)
        {
            from = Mathf.Clamp01(from);
            to = Mathf.Clamp01(to);
            var pts = new List<Vector2>();
            int steps = Mathf.Max(2, Mathf.CeilToInt((to - from) * 80));
            for (int i = 0; i <= steps; i++) pts.Add(ArcPoint(Mathf.Lerp(from, to, i / (float)steps)));
            UILine.Create(name, _bar, color, thickness).SetPoints(pts);
        }

        /// <summary>Point on the bar for u in [0,1]; the bar curves up at the ends.</summary>
        static Vector2 ArcPoint(float u)
        {
            float x = Mathf.Lerp(-BarHalfWidth, BarHalfWidth, u);
            return new Vector2(x, BarSag * (x / BarHalfWidth) * (x / BarHalfWidth));
        }

        void Place(float u)
        {
            _u = u;
            var p = ArcPoint(u);
            var tangent = ArcPoint(Mathf.Min(1f, u + 0.01f)) - ArcPoint(Mathf.Max(0f, u - 0.01f));
            _pointer.anchoredPosition = p;
            _pointer.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            var utc = _utcStart + TimeSpan.FromMinutes(Mathf.Min(_elapsed * _data.windowMinutesPerSecond, _attempt.windowMinutes));
            _clock.text = $"{utc.Hours:00}:{utc.Minutes:00}:{utc.Seconds:00}";
        }

        void Update()
        {
            if (_stopped) return;
            if (DevSkipPressed()) // dev shortcut: stop right in the green zone
            {
                Place(_greenCenter);
                Stop();
                return;
            }
            _elapsed += Time.unscaledDeltaTime;
            Place(Mathf.PingPong(_elapsed * _data.pointerSpeed * 2f, 1f));
            if (_elapsed * _data.windowMinutesPerSecond >= _attempt.windowMinutes)
            {
                WindowClosed();
                return;
            }
            if (AdvancePressed()) Stop();
        }

        static bool DevSkipPressed()
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && k.lKey.wasPressedThisFrame;
        }

        enum Zone { Green, Yellow, Miss }

        void Stop()
        {
            if (_stopped) return;
            _stopped = true;
            float d = Mathf.Abs(_u - _greenCenter);
            var zone = d <= _data.greenZone / 2f ? Zone.Green : d <= _data.yellowZone / 2f ? Zone.Yellow : Zone.Miss;
            _result.text = zone == Zone.Green ? _data.perfect : zone == Zone.Yellow ? _data.good : _data.miss;
            _result.color = zone == Zone.Green ? GreenZone : zone == Zone.Yellow ? YellowZone : Theme.Failure;
            StartCoroutine(Outcome(zone));
        }

        void WindowClosed()
        {
            _stopped = true;
            _result.text = _data.windowClosed;
            _result.color = Theme.Failure;
            StartCoroutine(Outcome(Zone.Miss, windowClosed: true));
        }

        IEnumerator Outcome(Zone zone, bool windowClosed = false)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            var stage = App.Stage;
            int attempt = App.LaunchIndex;

            // Missed both zones (or let the window close): Spehalski is not amused, and the same date is tried again.
            if (zone == Zone.Miss || windowClosed)
            {
                App.ShowDialog(_data.missSpeakerId, c => c.launch.missLines, App.RetryLaunch, false, c => c.launch.missSpeakerName, true);
                yield break;
            }

            switch (_attempt.outcome)
            {
                case "engineFailure":
                    stage.ShowEngineFailure();
                    yield return new WaitForSecondsRealtime(1.4f);
                    App.ShowDialog(App.SpeakerOrRandom(_attempt.speakerId), c => c.launch.attempts[attempt].lines, App.NextLaunchAttempt, false, null, true);
                    break;

                case "badWeather":
                    stage.SetBadWeather();
                    yield return new WaitForSecondsRealtime(2.2f);
                    App.ShowDialog(App.SpeakerOrRandom(_attempt.speakerId), c => c.launch.attempts[attempt].lines, App.NextLaunchAttempt, false, null, true);
                    break;

                default: // success
                    if (zone == Zone.Green)
                    {
                        stage.Ignite(); // engines fire and the shuttle climbs away
                        yield return new WaitForSecondsRealtime(3f);
                        App.LaunchSucceeded();
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(0.6f);
                        _result.text = _data.retry;
                        _result.color = Theme.Failure;
                        yield return new WaitForSecondsRealtime(1.6f);
                        App.RetryLaunch();
                    }
                    break;
            }
        }
    }
}
