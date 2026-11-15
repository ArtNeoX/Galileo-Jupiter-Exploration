using Galileo.UI;
using UnityEngine;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 4 system: the engineering problem at the top, two team members below, each proposing one option
    /// in a speech bubble ("A: …", "B: …"). Clicking a bubble opens that option.
    /// Systems with an analysis get an ANALYSIS button on the bottom edge of the problem panel.
    /// </summary>
    public class SystemScreen : ScreenBase
    {
        protected override void Build()
        {
            var system = App.CurrentSystem;
            BackButton(App.ShowOrbiterDesign);
            Header(system.title, system.subtitle);

            // Problem
            var problem = UIFactory.Panel("Problem", Root, Theme.Panel, 22);
            problem.raycastTarget = false;
            problem.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(160, -540), new Vector2(-160, -215));
            UIFactory.Frame(problem.transform, Theme.Line, 22, 2);
            var text = Section(Ui.problemLabel, system.problem) +
                       (string.IsNullOrEmpty(system.task) ? "" : $"<i>{system.task}</i>");
            bool analysis = GalileoApp.HasAnalysis(system);
            UIFactory.Label("Text", problem.transform, text.TrimEnd(), 28, Theme.Text).BestFit(16, 28)
                .rectTransform.Stretch(40, 28, 40, analysis ? 52 : 28);
            if (analysis) AnalysisButton(problem.rectTransform, system);

            // Two characters, each with an option bubble
            var options = App.OptionsInOrder(system);
            for (int i = 0; i < options.Length && i < 2; i++)
            {
                var option = options[i];
                bool left = i == 0;
                string speaker = system.speakers != null && system.speakers.Length > i ? system.speakers[i] : null;
                string letter = ((char)('A' + i)).ToString();

                var portrait = PortraitCard(Root, speaker);
                portrait.Pin(new Vector2(left ? 0 : 1, 0), new Vector2(240, 300), new Vector2(left ? 160 : -160, 90));

                var bubble = UIFactory.ChoiceButton("Option " + letter, Root, $"{letter}:  {option.name}", () => App.ShowOption(option));
                bubble.GetComponent<RectTransform>()
                    .Pin(new Vector2(left ? 0 : 1, 0), new Vector2(560, 110), new Vector2(left ? 430 : -430, left ? 280 : 150));
            }
        }

        /// <summary>Sits on the panel's bottom edge, like in the reference. A tick shows once the analysis is done.</summary>
        void AnalysisButton(RectTransform panel, Galileo.Content.DesignSystemInfo system)
        {
            bool done = App.AnalysedSystems.Contains(system.id);
            var button = UIFactory.Button("Analysis", panel, Ui.analysis.ToUpperInvariant(), App.ShowAnalysis);
            button.GetComponent<RectTransform>().Pin(new Vector2(0.5f, 0f), new Vector2(done ? 300 : 260, 64), new Vector2(0, -32));
            if (done)
            {
                var label = button.transform.Find("Label").GetComponent<RectTransform>();
                label.Stretch(0, 0, 50, 0);
                UIFactory.Picture("Done", button.transform, ProceduralArt.Icon("check"), Theme.ButtonText).rectTransform
                    .Pin(new Vector2(1, 0.5f), new Vector2(34, 34), new Vector2(-30, 0));
            }
            else if (system.analysis.required)
                UIFactory.Label("Required", Root, Ui.analysisRequired, 24, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Italic)
                    .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(160, -620), new Vector2(-160, -582));
        }
    }
}
