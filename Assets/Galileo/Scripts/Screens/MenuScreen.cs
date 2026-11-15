using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>Main menu over bg_menu: game name in the top-left corner, PLAY and QUIT on the right.</summary>
    public class MenuScreen : ScreenBase
    {
        protected override void Build()
        {
            var menu = Content.menu;

            // Soft shades so the title and buttons read over any picture.
            var left = UIFactory.Picture("TitleShade", Root, ProceduralArt.VerticalGradient(new Color(0, 0, 0, 0.55f), new Color(0, 0, 0, 0)), Color.white);
            left.preserveAspect = false;
            left.rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -360), Vector2.zero);

            UIFactory.Label("Title", Root, (menu.title ?? "").ToUpperInvariant(), 96, Theme.Accent, TextAnchor.LowerLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(0.7f, 1), new Vector2(90, -190), new Vector2(0, -60));
            UIFactory.Line(Root, Theme.Accent).rectTransform.Pin(new Vector2(0, 1), new Vector2(120, 4), new Vector2(94, -206));
            UIFactory.Label("Subtitle", Root, (menu.subtitle ?? "").ToUpperInvariant(), 40, Theme.Text, TextAnchor.UpperLeft)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(0.7f, 1), new Vector2(94, -280), new Vector2(0, -222));

            var column = UIFactory.Rect("Buttons", Root);
            column.Pin(new Vector2(1, 0.5f), new Vector2(400, 210), new Vector2(-140, -40));
            var play = UIFactory.Button("Play", column, (menu.play ?? "").ToUpperInvariant(), App.StartGame);
            play.GetComponent<RectTransform>().Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -92), Vector2.zero);
            play.transform.Find("Label").GetComponent<Text>().fontSize = 38;
            var quit = UIFactory.Button("Quit", column, (menu.quit ?? "").ToUpperInvariant(), Quit);
            quit.GetComponent<RectTransform>().Anchor(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 92));
            quit.transform.Find("Label").GetComponent<Text>().fontSize = 38;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
