using System.Collections;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 2.1: character bitmap in the centre, dialog box along the bottom with the speaker's name above it.
    /// Click to finish the current line, click again for the next one.
    /// </summary>
    public class DialogScreen : ScreenBase
    {
        const float CharactersPerSecond = 45f;

        Text _line;
        Image _indicator;
        Coroutine _typing;
        string[] _lines;
        int _index = -1;

        protected override void Build()
        {
            MakeClickable(Advance);
            var dialog = Content.missionDialog;
            var speaker = Content.FindCharacter(dialog.speakerId);
            _lines = dialog.lines;

            // Character bitmap, centred
            var portraitBox = UIFactory.Panel("PortraitBox", Root, Theme.PanelDeep, 16);
            portraitBox.raycastTarget = false;
            portraitBox.rectTransform.Pin(new Vector2(0.5f, 1f), new Vector2(460, 560), new Vector2(0, -70));
            UIFactory.Frame(portraitBox.transform, Theme.Line, 16, 2);
            var portrait = UIFactory.Picture("Portrait", portraitBox.transform, null, Color.white);
            portrait.rectTransform.Stretch(14, 14, 14, 14);
            ApplyPortrait(portrait, dialog.speakerId);

            // Speaker name, just above the box on the left
            UIFactory.Label("Speaker", Root, speaker != null ? speaker.name.ToUpperInvariant() : "", 26, Theme.Accent,
                    TextAnchor.LowerLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(206, 316), new Vector2(-180, 356));

            // Dialog box
            var box = UIFactory.Panel("DialogBox", Root, Theme.Panel, 22);
            box.raycastTarget = false;
            box.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(180, 70), new Vector2(-180, 306));
            UIFactory.Frame(box.transform, Theme.AccentDark, 22, 2);
            _line = UIFactory.Label("Text", box.transform, "", 34, Theme.Text).BestFit(22, 34);
            _line.rectTransform.Stretch(44, 34, 110, 34);
            _indicator = UIFactory.Picture("Continue", box.transform, ProceduralArt.Icon("triangle_down"), Theme.Accent);
            _indicator.rectTransform.Pin(new Vector2(1, 0), new Vector2(34, 34), new Vector2(-44, 34));

            if (_lines.Length == 0) { App.ShowConcepts(); return; }
            Advance();
        }

        void Update()
        {
            if (AdvancePressed()) Advance();
            if (_indicator != null)
                _indicator.enabled = _typing == null && Mathf.Repeat(Time.unscaledTime, 1f) < 0.65f;
        }

        void Advance()
        {
            if (_typing != null)
            {
                StopCoroutine(_typing);
                _typing = null;
                _line.text = _lines[_index];
                return;
            }
            if (_index >= _lines.Length - 1)
            {
                App.ShowConcepts();
                return;
            }
            _index++;
            _typing = StartCoroutine(Type(_lines[_index]));
        }

        IEnumerator Type(string text)
        {
            float shown = 0;
            while (shown < text.Length)
            {
                shown += CharactersPerSecond * Time.unscaledDeltaTime;
                _line.text = text.Substring(0, Mathf.Min(text.Length, (int)shown));
                yield return null;
            }
            _typing = null;
        }
    }
}
