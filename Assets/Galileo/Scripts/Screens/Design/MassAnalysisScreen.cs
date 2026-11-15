using System.Globalization;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Radiation protection analysis (TEST 1): two samples of the same volume side by side; the player types the
    /// mass of each (m = ρ·V, densities are in the INFO window). The options' Test buttons unlock once both are right.
    /// Sample bitmaps: Resources/Galileo/Analysis/&lt;option.sample&gt; (square).
    /// </summary>
    public class MassAnalysisScreen : ScreenBase
    {
        static readonly CultureInfo Russian = new CultureInfo("ru-RU");

        AnalysisData _data;
        DesignOptionInfo[] _options;
        InputField[] _fields;
        bool[] _correct;
        Text _status;
        RectTransform _info;

        protected override void Build()
        {
            var system = App.CurrentSystem;
            _data = system.analysis;
            _options = App.OptionsInOrder(system);
            int count = Mathf.Min(2, _options.Length);
            _fields = new InputField[count];
            _correct = new bool[count];
            bool done = App.AnalysedSystems.Contains(system.id);

            var panel = FullScreenPanel(Root);
            UIFactory.Label("Title", panel, (_data.title ?? "").ToUpperInvariant(), 42, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(260, -110), new Vector2(-260, -40));
            UIFactory.Label("Hint", panel, _data.hint, 26, Theme.TextMuted, TextAnchor.UpperCenter).BestFit(18, 26)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(260, -190), new Vector2(-260, -116));

            var info = UIFactory.Button("Info", panel, Ui.info.ToUpperInvariant(), () => ShowInfo(true));
            info.GetComponent<RectTransform>().Pin(new Vector2(1, 1), new Vector2(170, 60), new Vector2(-40, -40));

            string volume = _data.volume.ToString("0.###", Russian);
            for (int i = 0; i < count; i++)
                BuildSample(panel, i, i == 0 ? -400f : 400f, volume, done);

            _status = UIFactory.Label("Status", panel, "", 28, Theme.Success, TextAnchor.MiddleCenter, FontStyle.Bold);
            _status.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(100, 130), new Vector2(-100, 190));

            var back = UIFactory.Button("Return", panel, Ui.returnLabel.ToUpperInvariant(), () => App.ShowSystem(system));
            back.GetComponent<RectTransform>().Pin(new Vector2(0.5f, 0f), new Vector2(320, 70), new Vector2(0, 40));

            _info = BuildInfo();
            ShowInfo(false);
            if (done) ShowStatus(_data.successMessage, Theme.Success);
        }

        void BuildSample(RectTransform panel, int index, float x, string volume, bool done)
        {
            var option = _options[index];
            var column = UIFactory.Rect("Sample " + index, panel);
            column.Pin(new Vector2(0.5f, 0.5f), new Vector2(620, 640), new Vector2(x, -40));

            // Square sample bitmap (1:1), or an outline cube until the bitmap is added.
            var sprite = string.IsNullOrEmpty(option.sample) ? null : ContentLoader.LoadSprite("Galileo/Analysis/" + option.sample);
            var box = UIFactory.Panel("Box", column, Theme.PanelDeep, 18);
            box.raycastTarget = false;
            box.rectTransform.Pin(new Vector2(0.5f, 1f), new Vector2(340, 340), Vector2.zero);
            UIFactory.Frame(box.transform, Theme.Line, 18, 2);
            var picture = UIFactory.Picture("Sample", box.transform, sprite != null ? sprite : ProceduralArt.Icon("cube"),
                sprite != null ? Color.white : Theme.Icon);
            picture.rectTransform.Stretch(sprite != null ? 10 : 60, sprite != null ? 10 : 60, sprite != null ? 10 : 60, sprite != null ? 10 : 60);

            string letter = ((char)('A' + index)).ToString();
            UIFactory.Label("Name", column, $"{letter}.  {option.name}", 30, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).BestFit(20, 30)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -410), new Vector2(0, -356));
            UIFactory.Label("Volume", column, string.Format(_data.volumeFormat, volume), 32, Theme.Text, TextAnchor.MiddleCenter)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -470), new Vector2(0, -420));

            // m = [      ] кг
            var row = UIFactory.Rect("Mass", column);
            row.Pin(new Vector2(0.5f, 1f), new Vector2(460, 72), new Vector2(0, -500));
            UIFactory.Label("Label", row, _data.massLabel, 34, Theme.Text, TextAnchor.MiddleRight, FontStyle.Bold)
                .rectTransform.Anchor(Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(90, 0));
            var field = UIFactory.InputField("Input", row, 32, "?");
            field.characterLimit = 10;
            field.GetComponent<RectTransform>().Anchor(Vector2.zero, Vector2.one, new Vector2(104, 0), new Vector2(-90, 0));
            UIFactory.Label("Unit", row, _data.massUnit, 32, Theme.Text, TextAnchor.MiddleLeft)
                .rectTransform.Anchor(new Vector2(1, 0), Vector2.one, new Vector2(-76, 0), Vector2.zero);
            _fields[index] = field;

            if (done)
            {
                field.text = Exact(index).ToString("0.###", Russian);
                MarkCorrect(index);
            }
            else
            {
                field.onValueChanged.AddListener(_ => OnValueChanged(index));
                field.onEndEdit.AddListener(_ => OnEndEdit(index));
            }
        }

        float Exact(int index) => _options[index].density * _data.volume;

        bool IsRight(int index)
        {
            var raw = (_fields[index].text ?? "").Trim().Replace(" ", "").Replace(',', '.');
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) return false;
            float exact = Exact(index);
            return Mathf.Abs(value - exact) <= Mathf.Max(0.001f, Mathf.Abs(exact) * _data.tolerance);
        }

        void OnValueChanged(int index)
        {
            if (_correct[index]) return;
            SetFrame(index, Theme.AccentDark);
            if (IsRight(index)) MarkCorrect(index);
        }

        void OnEndEdit(int index)
        {
            if (_correct[index] || string.IsNullOrWhiteSpace(_fields[index].text)) return;
            SetFrame(index, Theme.Failure);
            ShowStatus(_data.errorMessage, Theme.Failure);
        }

        void MarkCorrect(int index)
        {
            _correct[index] = true;
            _fields[index].interactable = false;
            _fields[index].textComponent.color = Theme.Success;
            SetFrame(index, Theme.Success);
            foreach (var c in _correct)
                if (!c) { ShowStatus("", Theme.Success); return; }
            App.MarkAnalysed();
            ShowStatus(_data.successMessage, Theme.Success);
        }

        void SetFrame(int index, Color color) => _fields[index].transform.Find("Frame").GetComponent<Image>().color = color;

        void ShowStatus(string text, Color color)
        {
            _status.text = text;
            _status.color = color;
        }

        // ---------- INFO window: both materials' full descriptions (with the densities) ----------

        RectTransform BuildInfo()
        {
            var panel = FullScreenPanel(Root);
            panel.parent.name = "Info";
            UIFactory.Label("Title", panel, (string.IsNullOrEmpty(_data.infoTitle) ? Ui.info : _data.infoTitle).ToUpperInvariant(), 40,
                    Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(200, -104), new Vector2(-200, -40));
            var close = UIFactory.IconButton("Close", panel, ProceduralArt.Icon("cross"), () => ShowInfo(false));
            close.GetComponent<RectTransform>().Pin(new Vector2(1, 1), new Vector2(64, 64), new Vector2(-40, -40));

            for (int i = 0; i < _fields.Length; i++)
            {
                var option = _options[i];
                var column = UIFactory.Panel("Column " + i, panel, Theme.PanelDark, 18);
                column.raycastTarget = false;
                column.rectTransform.Anchor(new Vector2(i * 0.5f, 0), new Vector2(i * 0.5f + 0.5f, 1),
                    new Vector2(i == 0 ? 50 : 15, 50), new Vector2(i == 0 ? -15 : -50, -130));
                UIFactory.Frame(column.transform, Theme.Line, 18, 2);
                string letter = ((char)('A' + i)).ToString();
                UIFactory.Label("Name", column.transform, $"{letter}:  {option.name}", 32, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold)
                    .BestFit(22, 32).rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(34, -80), new Vector2(-34, -26));
                var body = Section(Ui.behaviorLabel, option.behavior) +
                           Section(Ui.prosLabel, option.pros, Theme.ProsHex) +
                           Section(Ui.consLabel, option.cons, Theme.ConsHex);
                UIFactory.Label("Text", column.transform, body.TrimEnd(), 26, Theme.Text).BestFit(14, 26)
                    .rectTransform.Stretch(34, 96, 34, 30);
            }
            return (RectTransform)panel.parent; // the shade around the window
        }

        void ShowInfo(bool show)
        {
            _info.gameObject.SetActive(show);
            if (show) _info.SetAsLastSibling();
        }

        /// <summary>Opaque panel covering the screen, so the window reads as its own full-screen view.</summary>
        internal static RectTransform FullScreenPanel(RectTransform parent)
        {
            var shade = UIFactory.Line(parent, new Color(0.03f, 0.02f, 0.02f, 0.75f));
            shade.raycastTarget = true; // blocks clicks to whatever is underneath
            shade.rectTransform.Stretch();
            var panel = UIFactory.Panel("Window", shade.transform, Theme.Panel, 24);
            panel.color = new Color(Theme.Panel.r, Theme.Panel.g, Theme.Panel.b, 1f);
            panel.rectTransform.Stretch(60, 50, 60, 50);
            UIFactory.Frame(panel.transform, Theme.AccentDark, 24, 2);
            return panel.rectTransform;
        }
    }
}
