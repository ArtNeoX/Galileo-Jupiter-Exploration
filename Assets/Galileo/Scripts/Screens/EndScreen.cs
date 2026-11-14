using Galileo.UI;
using UnityEngine;

namespace Galileo.Screens
{
    /// <summary>Temporary stop after the last finished scene until the next scenes are added.</summary>
    public class EndScreen : ScreenBase
    {
        protected override void Build()
        {
            Header(Ui.endTitle, Ui.endBody);
            var restart = UIFactory.Button("Restart", Root, Ui.restart.ToUpperInvariant(), App.Restart);
            restart.GetComponent<RectTransform>().Pin(new Vector2(0.5f, 0.5f), new Vector2(360, 76), Vector2.zero);
        }
    }
}
