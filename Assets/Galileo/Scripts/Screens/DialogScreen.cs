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
            var dialog = App.PendingDialog;
            var speaker = Content.FindCharacter(dialog.speakerId);
            _lines = dialog.lines?.Invoke(Content) ?? new string[0];

            // Character bitmap, centred
            var portraitBox = UIFactory.Panel("PortraitBox", Root, Color.clear, 16);
            portraitBox.raycastTarget = false;
            if (dialog.portraitRight) // aligned with the dialog box's right edge
                portraitBox.rectTransform.Pin(new Vector2(1f, 1f), new Vector2(460, 560), new Vector2(-180, -70));
            else if (dialog.portraitLeft) // aligned with the dialog box's left edge
                portraitBox.rectTransform.Pin(new Vector2(0f, 1f), new Vector2(460, 560), new Vector2(180, -70));
            else
                portraitBox.rectTransform.Pin(new Vector2(0.5f, 1f), new Vector2(460, 560), new Vector2(0, -70));
            var portrait = UIFactory.Picture("Portrait", portraitBox.transform, null, Color.white);
            portrait.rectTransform.Stretch(14, 14, 14, 14);
            ApplyPortrait(portrait, dialog.speakerId);

            // Speaker name, just above the box on the left
            var customName = dialog.speakerName?.Invoke(Content);
            var speakerName = !string.IsNullOrEmpty(customName) ? customName : speaker != null ? speaker.name : "";
            UIFactory.Label("Speaker", Root, speakerName.ToUpperInvariant(), 26, Theme.Accent,
                    dialog.portraitRight ? TextAnchor.LowerRight : TextAnchor.LowerLeft, FontStyle.Bold)
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

            if (_lines.Length == 0) { Finish(); return; }
            // Resume on the same line after a language switch.
            _index = Mathf.Clamp(dialog.index, 0, _lines.Length - 1) - 1;
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
                Finish();
                return;
            }
            _index++;
            if (App.PendingDialog != null) App.PendingDialog.index = _index;
            _typing = StartCoroutine(Type(_lines[_index]));
        }

        void Finish()
        {
            var next = App.PendingDialog?.onFinished;
            if (next != null) next();
            else App.ShowEnd();
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
