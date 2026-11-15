using System;
using System.Collections;
using System.Collections.Generic;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Thermal control analysis (TEST 2): every pro and con of both options is a card in a pool at the bottom;
    /// the player drags each into the right column (A +, A −, B +, B −). Cards are shortened with "…" and expand
    /// on hover. CHECK sends wrong cards back to the pool; when all are right the options' Test buttons unlock.
    /// The cards are the bullet lines of each option's pros / cons. Placement is kept while the player goes back
    /// to read the options.
    /// </summary>
    public class MatchingAnalysisScreen : ScreenBase
    {
        const int Pool = -1;
        const float ColumnWidth = 430f;
        const float ChipWidth = 412f;
        const float ChipHeight = 40f;
        const float RowStep = 44f;
        const float ZonesTop = 212f;
        const float PoolTop = -76f;
        const int ChipFont = 20;
        /// <summary>Visible part of a card's text before the "…" (full text shows on hover).</summary>
        const float ShortTextWidth = 250f;

        class Chip
        {
            public int index, correctZone, zone;
            public string text;
            public RectTransform rect;
            public Image frame, flash;
            public Vector2 target;
            public bool dragging;
        }

        AnalysisData _data;
        DesignSystemInfo _system;
        readonly List<Chip> _chips = new List<Chip>();
        int[] _placement;
        int _capacity;
        bool _done;
        RectTransform _area, _tooltip;
        Text _tooltipText, _status;
        Button _check;
        Vector2 _dragOffset;

        protected override void Build()
        {
            _system = App.CurrentSystem;
            _data = _system.analysis;
            _done = App.AnalysedSystems.Contains(_system.id);
            var options = App.OptionsInOrder(_system);

            var window = MassAnalysisScreen.FullScreenPanel(Root);
            UIFactory.Label("Title", window, (_data.title ?? "").ToUpperInvariant(), 40, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(200, -100), new Vector2(-200, -36));
            UIFactory.Label("Hint", window, _data.hint, 24, Theme.TextMuted, TextAnchor.UpperCenter).BestFit(16, 24)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(200, -168), new Vector2(-200, -102));

            _area = UIFactory.Rect("Board", window);
            _area.Pin(new Vector2(0.5f, 0.5f), new Vector2(1720, 980), Vector2.zero);

            // Cards: pros then cons of each option, in display order (A, B).
            for (int o = 0; o < options.Length && o < 2; o++)
            {
                AddChips(Items(options[o].pros), o * 2);
                AddChips(Items(options[o].cons), o * 2 + 1);
            }
            var counts = new int[4];
            foreach (var c in _chips) counts[c.correctZone]++;
            _capacity = Mathf.Max(counts[0], Mathf.Max(counts[1], Mathf.Max(counts[2], counts[3])));

            for (int o = 0; o < options.Length && o < 2; o++) BuildColumns(o, options[o].name);
            BuildPool();

            // Shuffle the pool once (seeded, so it's the same every visit), then restore any earlier placement.
            var rng = new System.Random(_system.id.GetHashCode());
            for (int i = _chips.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_chips[i], _chips[j]) = (_chips[j], _chips[i]);
            }
            if (!App.MatchingPlacement.TryGetValue(_system.id, out _placement) || _placement.Length != _chips.Count)
            {
                _placement = new int[_chips.Count];
                for (int i = 0; i < _placement.Length; i++) _placement[i] = Pool;
                App.MatchingPlacement[_system.id] = _placement;
            }
            foreach (var chip in _chips)
            {
                BuildChip(chip);
                chip.zone = _done ? chip.correctZone : _placement[chip.index];
                if (chip.zone == chip.correctZone) chip.frame.color = Theme.Success;
            }

            BuildTooltip();

            _status = UIFactory.Label("Status", window, "", 26, Theme.Success, TextAnchor.MiddleCenter, FontStyle.Bold);
            _status.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(380, 30), new Vector2(-380, 96));
            var back = UIFactory.Button("Return", window, Ui.returnLabel.ToUpperInvariant(), () => App.ShowSystem(_system));
            back.GetComponent<RectTransform>().Pin(new Vector2(0, 0), new Vector2(300, 66), new Vector2(40, 30));
            _check = UIFactory.Button("Check", window, Ui.check.ToUpperInvariant(), Check);
            _check.GetComponent<RectTransform>().Pin(new Vector2(1, 0), new Vector2(300, 66), new Vector2(-40, 30));

            if (_done)
            {
                foreach (var c in _chips) c.frame.color = Theme.Success;
                _check.gameObject.SetActive(false);
                ShowStatus(_data.successMessage, Theme.Success);
            }
            Relayout(snap: true);
        }

        void AddChips(string[] items, int zone)
        {
            foreach (var item in items)
                _chips.Add(new Chip { index = _chips.Count, correctZone = zone, text = item, zone = Pool });
        }

        /// <summary>The bullet lines of a pros / cons text.</summary>
        static string[] Items(string text)
        {
            var result = new List<string>();
            foreach (var raw in (text ?? "").Split('\n'))
            {
                var line = raw.Trim().TrimStart('•', '●', '-', '–', '—').Trim();
                if (line.Length > 0) result.Add(line);
            }
            return result.ToArray();
        }

        // ---------- Layout ----------

        static float ZoneX(int column) => -2f * ColumnWidth + ColumnWidth * (column + 0.5f);

        Rect ZoneRect(int zone)
        {
            float height = _capacity * RowStep + 8f;
            return new Rect(ZoneX(zone) - ColumnWidth / 2f, ZonesTop - height, ColumnWidth, height);
        }

        Rect PoolRect()
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(_chips.Count / 4f));
            float height = rows * RowStep + 8f;
            return new Rect(-2f * ColumnWidth, PoolTop - height, 4f * ColumnWidth, height);
        }

        void BuildColumns(int option, string name)
        {
            string letter = ((char)('A' + option)).ToString();
            var header = UIFactory.Label("Option " + letter, _area, $"{letter}:  {name}", 28, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .BestFit(18, 28).rectTransform;
            header.Pin(new Vector2(0.5f, 0.5f), new Vector2(2 * ColumnWidth - 20, 44), new Vector2((ZoneX(option * 2) + ZoneX(option * 2 + 1)) / 2f, 282));
            UIFactory.Line(_area, Theme.AccentDark).rectTransform
                .Pin(new Vector2(0.5f, 0.5f), new Vector2(2 * ColumnWidth - 20, 2), new Vector2((ZoneX(option * 2) + ZoneX(option * 2 + 1)) / 2f, 258));

            for (int k = 0; k < 2; k++)
            {
                int zone = option * 2 + k;
                bool pros = k == 0;
                UIFactory.Label(pros ? "Plus" : "Minus", _area, pros ? Ui.prosLabel + "  +" : Ui.consLabel + "  −", 26,
                        pros ? Theme.Success : Theme.Failure, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(ColumnWidth, 50), new Vector2(ZoneX(zone), 235));
                var r = ZoneRect(zone);
                var body = UIFactory.Panel("Zone " + zone, _area, Theme.PanelDeep, 14);
                body.raycastTarget = false;
                body.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(r.width - 10, r.height), r.center);
                UIFactory.Frame(body.transform, pros ? new Color(Theme.Success.r, Theme.Success.g, Theme.Success.b, 0.45f)
                    : new Color(Theme.Failure.r, Theme.Failure.g, Theme.Failure.b, 0.45f), 14, 2);
            }
        }

        void BuildPool()
        {
            var r = PoolRect();
            var pool = UIFactory.Panel("Pool", _area, Theme.PanelDark, 16);
            pool.raycastTarget = false;
            pool.rectTransform.Pin(new Vector2(0.5f, 0.5f), new Vector2(r.width + 10, r.height), r.center);
            UIFactory.Frame(pool.transform, Theme.Line, 16, 2);
        }

        void BuildChip(Chip chip)
        {
            var bg = UIFactory.Panel("Card " + chip.index, _area, Theme.PanelDark, 10);
            chip.rect = bg.rectTransform;
            chip.rect.anchorMin = chip.rect.anchorMax = chip.rect.pivot = new Vector2(0.5f, 0.5f);
            chip.rect.sizeDelta = new Vector2(ChipWidth, ChipHeight);
            chip.frame = UIFactory.Frame(bg.transform, Theme.AccentDark, 10, 3);
            var label = UIFactory.Label("Text", bg.transform, "", ChipFont, Theme.Text, TextAnchor.MiddleLeft);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.rectTransform.Stretch(14, 0, 14, 0);
            label.text = Shorten(label, chip.text, ShortTextWidth);
            chip.flash = UIFactory.Panel("Flash", bg.transform, new Color(1, 1, 1, 0), 10);
            chip.flash.raycastTarget = false;
            chip.flash.rectTransform.Stretch();

            var handler = bg.gameObject.AddComponent<HoverDragHandler>();
            handler.Entered += () => ShowTooltip(chip);
            handler.Exited += HideTooltip;
            if (_done) return;
            handler.DragStarted += e => BeginDrag(chip, e);
            handler.Dragged += e => Drag(chip, e);
            handler.DragEnded += e => EndDrag(chip, e);
        }

        /// <summary>Cuts the text at a word boundary and adds "…" so it fits on one line of the given width.</summary>
        static string Shorten(Text label, string text, float width)
        {
            if (Width(label, text) <= width) return text;
            var words = text.Split(' ');
            for (int n = words.Length - 1; n > 0; n--)
            {
                var candidate = string.Join(" ", words, 0, n).TrimEnd(',', '.', ':', ';') + "…";
                if (Width(label, candidate) <= width) return candidate;
            }
            return words[0] + "…";
        }

        static float Width(Text label, string text)
        {
            var settings = label.GetGenerationSettings(Vector2.zero);
            return label.cachedTextGeneratorForLayout.GetPreferredWidth(text, settings) / label.pixelsPerUnit;
        }

        void Relayout(bool snap = false)
        {
            var columns = new int[4];
            int poolIndex = 0;
            foreach (var chip in _chips) // _chips is kept in placement order
            {
                if (chip.zone == Pool)
                {
                    int row = poolIndex / 4, col = poolIndex % 4;
                    chip.target = new Vector2(ZoneX(col), PoolTop - 4f - ChipHeight / 2f - RowStep * row);
                    poolIndex++;
                }
                else
                {
                    int k = columns[chip.zone]++;
                    chip.target = new Vector2(ZoneX(chip.zone), ZonesTop - 4f - ChipHeight / 2f - RowStep * k);
                }
                if (snap) chip.rect.anchoredPosition = chip.target;
            }
            bool allPlaced = poolIndex == 0;
            if (_check != null) _check.interactable = allPlaced && !_done;
        }

        void Update()
        {
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 16f);
            foreach (var chip in _chips)
                if (!chip.dragging)
                    chip.rect.anchoredPosition = Vector2.Lerp(chip.rect.anchoredPosition, chip.target, k);
        }

        // ---------- Dragging ----------

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, e.pressEventCamera, out var local);
            return local;
        }

        void BeginDrag(Chip chip, PointerEventData e)
        {
            HideTooltip();
            chip.dragging = true;
            chip.rect.SetAsLastSibling();
            _dragOffset = chip.rect.anchoredPosition - Local(e);
            chip.frame.color = Theme.AccentDark;
            chip.flash.color = new Color(1, 1, 1, 0);
        }

        void Drag(Chip chip, PointerEventData e) => chip.rect.anchoredPosition = Local(e) + _dragOffset;

        void EndDrag(Chip chip, PointerEventData e)
        {
            chip.dragging = false;
            var p = Local(e);
            int zone = Pool;
            for (int z = 0; z < 4; z++)
                if (ZoneRect(z).Contains(p)) zone = z;

            if (zone != chip.zone)
            {
                if (zone != Pool && CountIn(zone) >= _capacity) zone = chip.zone; // column full: put it back
                if (zone != chip.zone)
                {
                    chip.zone = zone;
                    _placement[chip.index] = zone;
                    // Newly placed cards go to the end of their column / the pool.
                    _chips.Remove(chip);
                    _chips.Add(chip);
                }
            }
            // Instant feedback: a card in its right column gets a green outline and a white flash.
            if (chip.zone == chip.correctZone)
            {
                chip.frame.color = Theme.Success;
                StartCoroutine(FlashCorrect(chip));
            }
            ShowStatus("", Theme.Success);
            Relayout();
        }

        static IEnumerator FlashCorrect(Chip chip)
        {
            const float duration = 0.55f;
            var rt = chip.flash.rectTransform;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                chip.flash.color = new Color(1, 1, 1, 0.9f * (1f - k));
                rt.localScale = new Vector3(Mathf.Lerp(1f, 1.12f, k), Mathf.Lerp(1f, 1.6f, k), 1f);
                yield return null;
            }
            chip.flash.color = new Color(1, 1, 1, 0);
            rt.localScale = Vector3.one;
        }

        int CountIn(int zone)
        {
            int n = 0;
            foreach (var c in _chips) if (c.zone == zone) n++;
            return n;
        }

        // ---------- Check ----------

        void Check()
        {
            int wrong = 0;
            foreach (var chip in _chips)
            {
                if (chip.zone == chip.correctZone) continue;
                wrong++;
                StartCoroutine(Flash(chip.frame, Theme.Failure));
                chip.zone = Pool;
                _placement[chip.index] = Pool;
            }
            if (wrong > 0)
            {
                ShowStatus(_data.errorMessage, Theme.Failure);
                Relayout();
                return;
            }
            _done = true;
            App.MarkAnalysed();
            foreach (var chip in _chips)
            {
                chip.frame.color = Theme.Success;
                var handler = chip.rect.GetComponent<HoverDragHandler>();
                handler.DragStarted = null;
                handler.Dragged = null;
                handler.DragEnded = null;
            }
            _check.gameObject.SetActive(false);
            ShowStatus(_data.successMessage, Theme.Success);
        }

        static IEnumerator Flash(Image frame, Color color)
        {
            for (float t = 0; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                frame.color = Color.Lerp(color, Theme.AccentDark, Mathf.Clamp01((t - 0.6f) / 0.6f));
                yield return null;
            }
            frame.color = Theme.AccentDark;
        }

        void ShowStatus(string text, Color color)
        {
            _status.text = text;
            _status.color = color;
        }

        // ---------- Hover: the card expands to show its whole text ----------

        void BuildTooltip()
        {
            var bg = UIFactory.Panel("Expanded", _area, Theme.PanelHover, 10);
            bg.raycastTarget = false;
            _tooltip = bg.rectTransform;
            _tooltip.anchorMin = _tooltip.anchorMax = new Vector2(0.5f, 0.5f);
            _tooltip.pivot = new Vector2(0.5f, 0f); // grows upwards from the card's bottom edge
            UIFactory.Frame(bg.transform, Theme.Accent, 10, 2);
            _tooltipText = UIFactory.Label("Text", bg.transform, "", ChipFont + 2, Theme.Text, TextAnchor.MiddleLeft);
            _tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            _tooltipText.rectTransform.Stretch(16, 10, 16, 10);
            _tooltip.gameObject.SetActive(false);
        }

        void ShowTooltip(Chip chip)
        {
            if (chip.dragging || AnyDragging()) return;
            float width = ChipWidth + 60f;
            _tooltip.gameObject.SetActive(true);
            _tooltip.SetAsLastSibling();
            _tooltip.sizeDelta = new Vector2(width, 10f);
            _tooltipText.text = chip.text;
            float textHeight = _tooltipText.preferredHeight;
            _tooltip.sizeDelta = new Vector2(width, Mathf.Max(ChipHeight + 8f, textHeight + 22f));
            // Keep it inside the board horizontally.
            float x = Mathf.Clamp(chip.target.x, -2f * ColumnWidth + width / 2f, 2f * ColumnWidth - width / 2f);
            _tooltip.anchoredPosition = new Vector2(x, chip.target.y - ChipHeight / 2f - 4f);
        }

        void HideTooltip() => _tooltip.gameObject.SetActive(false);

        bool AnyDragging()
        {
            foreach (var c in _chips) if (c.dragging) return true;
            return false;
        }
    }
}
