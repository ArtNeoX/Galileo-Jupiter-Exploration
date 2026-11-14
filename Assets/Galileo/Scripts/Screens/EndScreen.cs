using Galileo.UI;
using UnityEngine;

namespace Galileo.Screens
{
    /// <summary>Temporary stop after Scene 3 until the next scenes are added.</summary>
    public class EndScreen : ScreenBase
    {
        protected override void Build()
        {
            Header(Ui.endTitle, Ui.endBody);

            var chosen = App.SelectedConcept;
            if (chosen != null)
            {
                var text = string.Format(Ui.chosenFormat, $"{chosen.letter}. {chosen.title}");
                UIFactory.Label("Choice", Root, text, 36, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(1400, 120), Vector2.zero);
            }

            var restart = UIFactory.Button("Restart", Root, Ui.restart.ToUpperInvariant(), App.ShowCharacters);
            restart.GetComponent<RectTransform>().Pin(new Vector2(0.5f, 0f), new Vector2(360, 76), new Vector2(0, 120));
        }
    }
}
