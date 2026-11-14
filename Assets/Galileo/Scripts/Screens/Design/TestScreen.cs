using System.Collections;
using Galileo.UI;
using UnityEngine;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 4 test: "test in progress, stand by" for a few seconds, then the result.
    /// Correct option → successful test, Next marks the system done. Wrong option → unsuccessful test, Try again.
    /// Both return to the systems list.
    /// </summary>
    public class TestScreen : ScreenBase
    {
        const float TestDuration = 3f;

        RectTransform _spinner;
        CanvasGroup _progress;

        protected override void Build()
        {
            var system = App.CurrentSystem;
            Header(system.title, system.subtitle);

            var box = UIFactory.Panel("InProgress", Root, Theme.Panel, 22);
            box.raycastTarget = false;
            box.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(760, 300), Vector2.zero);
            UIFactory.Frame(box.transform, Theme.Line, 22, 2);
            _progress = box.gameObject.AddComponent<CanvasGroup>();

            _spinner = UIFactory.Picture("Spinner", box.transform, ProceduralArt.Icon("spinner"), Theme.Accent).rectTransform;
            _spinner.Pin(new Vector2(0, 0.5f), new Vector2(110, 110), Vector2.zero);
            _spinner.pivot = new Vector2(0.5f, 0.5f); // spin around its centre
            _spinner.anchoredPosition = new Vector2(115, 0);
            UIFactory.Label("Status", box.transform, Ui.testInProgress.ToUpperInvariant(), 36, Theme.Text, TextAnchor.LowerLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(210, 4), new Vector2(-40, -40));
            UIFactory.Label("StandBy", box.transform, Ui.standBy, 26, Theme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic)
                .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(210, 40), new Vector2(-40, -6));

            StartCoroutine(Run());
        }

        void Update()
        {
            if (_spinner != null) _spinner.Rotate(0f, 0f, -300f * Time.unscaledDeltaTime);
        }

        IEnumerator Run()
        {
            for (float t = 0; t < TestDuration; t += Time.unscaledDeltaTime) yield return null;
            yield return Fade(_progress, 1f, 0f, 0.2f);
            Destroy(_progress.gameObject);
            _spinner = null;
            ShowResult();
        }

        void ShowResult()
        {
            var system = App.CurrentSystem;
            var option = App.CurrentOption;
            bool success = option.correct;

            var card = UIFactory.Panel("Result", Root, Theme.Panel, 24);
            card.raycastTarget = false;
            card.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(200, 70), new Vector2(-200, -215));
            UIFactory.Frame(card.transform, success ? Theme.Success : Theme.Failure, 24, 3);
            var group = card.gameObject.AddComponent<CanvasGroup>();

            UIFactory.Picture("Icon", card.transform, ProceduralArt.Icon(success ? "check" : "cross"), success ? Theme.Success : Theme.Failure)
                .rectTransform.Pin(new Vector2(0, 1), new Vector2(90, 90), new Vector2(40, -30));
            UIFactory.Label("Verdict", card.transform, (success ? Ui.successfulTest : Ui.unsuccessfulTest).ToUpperInvariant(), 40,
                    success ? Theme.Success : Theme.Failure, TextAnchor.LowerLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(160, -76), new Vector2(-40, -24));
            UIFactory.Label("ResultTitle", card.transform, option.resultTitle, 26, Theme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(160, -118), new Vector2(-40, -80));
            UIFactory.Line(card.transform, Theme.Line).rectTransform
                .Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -142), new Vector2(-40, -140));

            string body = option.result + "\n\n";
            if (success)
            {
                body += Section(Ui.whyChosenLabel, option.why);
                body += Section(Ui.finalDecisionLabel, system.finalDecision, Theme.ProsHex);
            }
            else
            {
                body += Section(Ui.whyNotChosenLabel, option.why);
                body += Section(Ui.recommendationLabel, option.recommendation);
            }
            UIFactory.Label("Body", card.transform, body.TrimEnd(), 26, Theme.Text).BestFit(14, 26).rectTransform
                .Anchor(Vector2.zero, Vector2.one, new Vector2(40, 120), new Vector2(-40, -160));

            var button = success
                ? UIFactory.Button("Next", card.transform, Ui.next.ToUpperInvariant(), App.CompleteCurrentSystem)
                : UIFactory.Button("TryAgain", card.transform, Ui.tryAgain.ToUpperInvariant(), App.ShowOrbiterDesign);
            button.GetComponent<RectTransform>().Pin(new Vector2(1, 0), new Vector2(success ? 260 : 440, 76), new Vector2(-34, 30));

            StartCoroutine(Fade(group, 0f, 1f, 0.3f));
        }
    }
}
