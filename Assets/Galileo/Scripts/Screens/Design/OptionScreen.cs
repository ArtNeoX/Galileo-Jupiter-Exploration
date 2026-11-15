using Galileo.UI;
using UnityEngine;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 4 option: the proposing character on the left, the option card on the right (name, icon,
    /// behaviour, pros, cons) and a Test button. The "why it was/wasn't chosen" part is only revealed after the test.
    /// </summary>
    public class OptionScreen : ScreenBase
    {
        protected override void Build()
        {
            var system = App.CurrentSystem;
            var option = App.CurrentOption;
            var options = App.OptionsInOrder(system);
            int index = System.Array.IndexOf(options, option);
            string letter = ((char)('A' + Mathf.Max(0, index))).ToString();
            string speaker = system.speakers != null && index >= 0 && system.speakers.Length > index ? system.speakers[index] : null;

            BackButton(() => App.ShowSystem(system));
            Header(system.title, system.subtitle);

            PortraitCard(Root, speaker).Pin(new Vector2(0, 0), new Vector2(300, 380), new Vector2(160, 110));

            // Option card ("speech bubble")
            var card = UIFactory.Panel("Option", Root, Theme.Panel, 22);
            card.raycastTarget = false;
            card.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(500, 70), new Vector2(-160, -215));
            UIFactory.Frame(card.transform, Theme.AccentDark, 22, 2);

            UIFactory.Picture("Icon", card.transform, IconFor(option.icon), Theme.Icon).rectTransform
                .Pin(new Vector2(1, 1), new Vector2(110, 110), new Vector2(-34, -28));
            UIFactory.Label("Name", card.transform, $"{letter}.  {option.name}", 40, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold)
                .BestFit(26, 40).rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -96), new Vector2(-170, -30));
            UIFactory.Line(card.transform, Theme.Line).rectTransform
                .Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -152), new Vector2(-40, -150));

            // Systems with a pros/cons analysis show the long description instead of ready-made lists.
            var body = !string.IsNullOrEmpty(option.detailText)
                ? option.detailText
                : Section(Ui.behaviorLabel, option.behavior) +
                  Section(Ui.prosLabel, option.pros, Theme.ProsHex) +
                  Section(Ui.consLabel, option.cons, Theme.ConsHex);
            UIFactory.Label("Info", card.transform, body.TrimEnd(), 26, Theme.Text).BestFit(14, 26).rectTransform
                .Anchor(Vector2.zero, Vector2.one, new Vector2(40, 120), new Vector2(-40, -170));

            var test = UIFactory.Button("Test", card.transform, Ui.test.ToUpperInvariant() + "   ", App.RunTest);
            test.GetComponent<RectTransform>().Pin(new Vector2(1, 0), new Vector2(280, 76), new Vector2(-34, 30));
            UIFactory.Picture("Play", test.transform, ProceduralArt.Icon("play"), Theme.ButtonText).rectTransform
                .Pin(new Vector2(1, 0.5f), new Vector2(24, 24), new Vector2(-30, 0));

            // The decision waits for the system's analysis.
            if (!App.CanDecide(system))
            {
                test.interactable = false;
                UIFactory.Label("Locked", card.transform, Ui.analysisRequired, 24, Theme.Failure, TextAnchor.MiddleRight, FontStyle.Italic)
                    .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 30), new Vector2(-340, 106));
            }
        }
    }
}
