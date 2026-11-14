using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 4 hub: the Orbiter's subsystems. A tick marks systems whose test has passed; a dot marks
    /// systems still to design. Next unlocks when every system is done.
    /// </summary>
    public class SystemsListScreen : ScreenBase
    {
        protected override void Build()
        {
            var data = Content.orbiterDesign;
            Header(data.title, data.subtitle);

            var panel = UIFactory.Panel("Systems", Root, Theme.Panel, 24);
            panel.raycastTarget = false;
            panel.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(1180, 140 * data.systems.Length + 60), new Vector2(0, -20));
            UIFactory.Frame(panel.transform, Theme.Line, 24, 2);

            var list = UIFactory.Rect("List", panel.transform).Stretch(30, 30, 30, 30);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            int done = 0;
            foreach (var system in data.systems)
            {
                bool complete = App.CompletedSystems.Contains(system.id);
                if (complete) done++;
                BuildRow(list, system, complete);
            }

            FooterLabel(string.Format(Ui.designedFormat, done, data.systems.Length));
            if (!string.IsNullOrEmpty(data.hint))
                UIFactory.Label("Hint", Root, data.hint, 22, Theme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic)
                    .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(120, 40), new Vector2(-420, 80));
            NextButton(App.ShowEnd).interactable = done == data.systems.Length;
        }

        void BuildRow(Transform list, DesignSystemInfo system, bool complete)
        {
            var row = UIFactory.Panel("Row " + system.id, list, Color.white, 16);
            var button = row.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Theme.PanelDark;
            colors.highlightedColor = Theme.PanelHover;
            colors.pressedColor = Theme.PanelDeep;
            colors.selectedColor = Theme.PanelDark;
            colors.disabledColor = Theme.PanelDark;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.interactable = !complete;
            button.onClick.AddListener(() => App.ShowSystem(system));

            var status = UIFactory.Picture("Status", row.transform, ProceduralArt.Icon(complete ? "check" : "pending"),
                complete ? Theme.Success : Theme.TextMuted);
            status.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(complete ? 52 : 36, complete ? 52 : 36), new Vector2(complete ? 32 : 40, 0));

            UIFactory.Label("Title", row.transform, system.title, 34, complete ? Theme.TextMuted : Theme.Text, TextAnchor.LowerLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(112, 0), new Vector2(-150, -10));
            UIFactory.Label("Subtitle", row.transform, system.subtitle, 24, Theme.TextMuted, TextAnchor.UpperLeft)
                .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(112, 8), new Vector2(-150, -2));

            if (!complete)
                UIFactory.Picture("Arrow", row.transform, ProceduralArt.Icon("play"), Theme.Accent).rectTransform
                    .Pin(new Vector2(1, 0.5f), new Vector2(30, 30), new Vector2(-36, 0));
            else if (!string.IsNullOrEmpty(system.finalDecision))
                UIFactory.Label("Decision", row.transform, system.finalDecision, 22, Theme.Success, TextAnchor.MiddleRight, FontStyle.Italic)
                    .rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(1, 1), Vector2.zero, new Vector2(-30, 0));
        }
    }
}
