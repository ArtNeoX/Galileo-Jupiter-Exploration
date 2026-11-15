using System.Collections;
using System.Collections.Generic;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 1: the three main characters, one at a time. Bitmap on the left, name and biography on the right.
    /// Click anywhere to show the next character.
    /// </summary>
    public class CharacterIntroScreen : ScreenBase
    {
        readonly List<Image> _dots = new List<Image>();
        CanvasGroup _card;
        Image _portrait;
        Text _name, _role, _bio;
        int _index;
        bool _switching;

        protected override void Build()
        {
            MakeClickable(Advance);
            Header(Ui.teamTitle, Ui.teamSubtitle);

            var card = UIFactory.Panel("Card", Root, Theme.Panel, 24);
            card.raycastTarget = false;
            card.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 1), new Vector2(180, 170), new Vector2(-180, -215));
            UIFactory.Frame(card.transform, Theme.Line, 24, 2);
            _card = card.gameObject.AddComponent<CanvasGroup>();

            // Left: character bitmap
            var portraitBox = UIFactory.Panel("PortraitBox", card.transform, Color.clear, 16);
            portraitBox.raycastTarget = false;
            portraitBox.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(40, 40), new Vector2(500, -40));
            _portrait = UIFactory.Picture("Portrait", portraitBox.transform, null, Color.white);
            _portrait.rectTransform.Stretch(14, 14, 14, 14);

            // Right: name + biography
            var info = UIFactory.Rect("Info", card.transform)
                .Anchor(Vector2.zero, Vector2.one, new Vector2(560, 40), new Vector2(-50, -40));
            _name = UIFactory.Label("Name", info, "", 52, Theme.Accent, TextAnchor.LowerLeft, FontStyle.Bold);
            _name.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -66), Vector2.zero);
            _role = UIFactory.Label("Role", info, "", 28, Theme.TextMuted, TextAnchor.UpperLeft).BestFit(20, 28);
            _role.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), new Vector2(0, -74));
            UIFactory.Line(info, Theme.Line).rectTransform
                .Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -134), new Vector2(0, -132));
            _bio = UIFactory.Label("Biography", info, "", 26, Theme.Text).BestFit(16, 26);
            _bio.rectTransform.Anchor(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -156));

            // Bottom: progress dots + hint
            var dots = UIFactory.Rect("Dots", Root).Pin(new Vector2(0.5f, 0), new Vector2(300, 20), new Vector2(0, 118));
            var layout = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            for (int i = 0; i < Content.characters.Length; i++)
            {
                var dot = UIFactory.Panel("Dot", dots, Theme.Line, 9);
                dot.raycastTarget = false;
                dot.rectTransform.sizeDelta = new Vector2(18, 18);
                _dots.Add(dot);
            }
            UIFactory.Label("Hint", Root, Ui.clickToContinue, 24, Theme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic)
                .rectTransform.Pin(new Vector2(0.5f, 0), new Vector2(1200, 40), new Vector2(0, 60));

            if (Content.characters.Length == 0) { App.ShowObjectives(); return; }
            Apply(Mathf.Clamp(App.CharacterIndex, 0, Content.characters.Length - 1));
        }

        void Update()
        {
            if (AdvancePressed()) Advance();
        }

        void Advance()
        {
            if (_switching) return;
            if (_index >= Content.characters.Length - 1)
            {
                App.ShowObjectives();
                return;
            }
            StartCoroutine(SwitchTo(_index + 1));
        }

        IEnumerator SwitchTo(int index)
        {
            _switching = true;
            yield return Fade(_card, 1f, 0f, 0.18f);
            Apply(index);
            yield return Fade(_card, 0f, 1f, 0.25f);
            _switching = false;
        }

        void Apply(int index)
        {
            _index = index;
            App.CharacterIndex = index;
            var c = Content.characters[index];
            ApplyPortrait(_portrait, c.id);
            _name.text = c.name;
            _role.text = c.role;
            _bio.text = FormatBio(c);
            for (int i = 0; i < _dots.Count; i++)
                _dots[i].color = i == index ? Theme.Accent : Theme.Line;
        }

        string FormatBio(CharacterProfile c)
        {
            var text = c.bio;
            text += Section(Ui.educationLabel, c.education);
            text += Section(Ui.specializationLabel, c.specialization);
            text += Section(Ui.teamRoleLabel, c.teamRole);
            return text;
        }

        static string Section(string label, string value) =>
            string.IsNullOrEmpty(value) ? "" : $"\n\n<color={Theme.AccentHex}><b>{label}:</b></color> {value}";
    }
}
