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
    /// Orbiter assembly in the garage. The parts lie scattered around the probe's outline; dragging one shows where it
    /// belongs, and dropping it close enough snaps it there. When every essential part is attached the outline gives way
    /// to the main structure and the player can continue. Hovering a part shows its name and description on the small
    /// screen on the right, under the budget; the checklist lists the essential parts.
    /// Optional parts (the low-gain antenna) aren't on the checklist: attaching one asks first and costs money.
    /// </summary>
    public class GarageScreen : ScreenBase
    {
        const float SnapDistance = 90f;
        const float FrameFill = 0.86f;
        /// <summary>Small parts get an invisible grab area at least this big.</summary>
        const float MinGrab = 90f;

        class Piece
        {
            public ProbePart part;
            public RectTransform rect;
            public Vector2 target, home, goal;
            public float tilt;
            public bool placed, dragging;
        }

        GarageData _data;
        RectTransform _board, _frame, _placedLayer, _looseLayer, _ghost, _prompt;
        CanvasGroup _outline, _structure;
        readonly List<Piece> _pieces = new List<Piece>();
        readonly Dictionary<string, Image> _checkIcons = new Dictionary<string, Image>();
        readonly Dictionary<string, Text> _checkLabels = new Dictionary<string, Text>();
        Text _infoName, _infoText, _status;
        Button _continue;
        Piece _pending;
        Vector2 _dragOffset;
        bool _complete;

        public override bool ShowBudget => true;

        // The garage picture is bright, so the free-standing text is dark.
        static readonly Color DarkText = new Color(0.17f, 0.12f, 0.1f);
        static readonly Color DarkMuted = new Color(0.3f, 0.24f, 0.2f);
        static readonly Color DarkSuccess = new Color(0.16f, 0.42f, 0.18f);

        protected override void Build()
        {
            _data = Content.garage;

            UIFactory.Label("Title", Root, (_data.title ?? "").ToUpperInvariant(), 44, DarkText, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(60, -96), new Vector2(-560, -36));
            _status = UIFactory.Label("Hint", Root, _data.hint, 24, DarkMuted, TextAnchor.UpperLeft, FontStyle.Italic);
            _status.rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(62, -140), new Vector2(-560, -100));
            // A soft light halo so the dark text stays readable on the busier parts of the garage picture.
            foreach (var t in new[] { _status, Root.Find("Title").GetComponent<Text>() })
            {
                var halo = t.gameObject.AddComponent<Outline>();
                halo.effectColor = new Color(1f, 1f, 1f, 0.45f);
                halo.effectDistance = new Vector2(1.5f, -1.5f);
            }

            _board = UIFactory.Rect("Board", Root);
            _board.Anchor(Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(-520, -150));
            var boardSize = _board.rect.size;

            _frame = ProbeAssembly.Frame("Probe", _board, ProbeAssembly.CanvasSize(_data), boardSize * FrameFill, Vector2.zero);
            _structure = ProbeAssembly.Structure(_data, _frame).gameObject.AddComponent<CanvasGroup>();
            var outline = ProbeAssembly.Outline(_data, _frame);
            // The outline bitmap is painted white; tint it dark so it shows on the bright garage.
            var outlineImage = outline.GetComponent<Image>();
            if (outlineImage != null) outlineImage.color = new Color(0.16f, 0.11f, 0.09f, 0.9f);
            _outline = outline.gameObject.AddComponent<CanvasGroup>();
            _placedLayer = UIFactory.Rect("Placed", _frame).Stretch();
            _looseLayer = UIFactory.Rect("Loose", _board).Stretch();

            BuildSidePanel();
            BuildPieces(boardSize);

            _complete = AllEssentialPlaced();
            _structure.alpha = _complete ? 1f : 0f;
            _outline.alpha = _complete ? 0f : 1f;
            RefreshChecklist();
            UpdateStatus();
        }

        // ---------- Right side: part info, checklist, continue ----------

        void BuildSidePanel()
        {
            var side = UIFactory.Rect("Side", Root);
            side.Anchor(new Vector2(1, 0), Vector2.one, new Vector2(-484, 40), new Vector2(-24, -196));

            // Small "monitor" with the hovered part's name and description.
            var screen = UIFactory.Panel("Info", side, Theme.PanelDeep, 16);
            screen.raycastTarget = false;
            screen.rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -300), Vector2.zero);
            UIFactory.Frame(screen.transform, Theme.AccentDark, 16, 2);
            UIFactory.Label("Header", screen.transform, (_data.infoTitle ?? "").ToUpperInvariant(), 18, Theme.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(24, -44), new Vector2(-24, -16));
            UIFactory.Line(screen.transform, Theme.Line).rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(24, -50), new Vector2(-24, -48));
            _infoName = UIFactory.Label("Name", screen.transform, "", 28, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold).BestFit(18, 28);
            _infoName.rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(24, -100), new Vector2(-24, -60));
            _infoText = UIFactory.Label("Text", screen.transform, _data.infoPlaceholder, 22, Theme.Text).BestFit(14, 22);
            _infoText.rectTransform.Stretch(24, 108, 24, 20);

            // Checklist of the essential parts.
            var list = UIFactory.Panel("Checklist", side, Theme.Panel, 16);
            list.raycastTarget = false;
            list.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(0, 100), new Vector2(0, -318));
            UIFactory.Frame(list.transform, Theme.Line, 16, 2);
            UIFactory.Label("Header", list.transform, (_data.checklistTitle ?? "").ToUpperInvariant(), 20, Theme.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(24, -46), new Vector2(-24, -16));
            var rows = UIFactory.Rect("Rows", list.transform).Stretch(20, 56, 20, 14);
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            foreach (var e in _data.elements)
            {
                if (e.optional) continue;
                var row = UIFactory.Rect("Row " + e.id, rows);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                var icon = UIFactory.Picture("Icon", row, ProceduralArt.Icon("pending"), Theme.TextMuted);
                icon.rectTransform.Pin(new Vector2(0, 0.5f), new Vector2(28, 28), new Vector2(4, 0));
                var label = UIFactory.Label("Name", row, e.name, 22, Theme.Text, TextAnchor.MiddleLeft).BestFit(14, 22);
                label.rectTransform.Stretch(46, 0, 0, 0);
                _checkIcons[e.id] = icon;
                _checkLabels[e.id] = label;
            }

            _continue = UIFactory.Button("Next", side, Ui.next.ToUpperInvariant(), App.FinishGarage);
            _continue.GetComponent<RectTransform>().Anchor(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 76));
        }

        void RefreshChecklist()
        {
            foreach (var piece in _pieces)
            {
                if (!_checkIcons.TryGetValue(piece.part.element.id, out var icon)) continue;
                icon.sprite = ProceduralArt.Icon(piece.placed ? "check" : "pending");
                icon.color = piece.placed ? Theme.Success : Theme.TextMuted;
                _checkLabels[piece.part.element.id].color = piece.placed ? Theme.Success : Theme.Text;
            }
            _continue.interactable = _complete;
        }

        void UpdateStatus()
        {
            _status.text = _complete ? _data.completeMessage : _data.hint;
            _status.color = _complete ? DarkSuccess : DarkMuted;
        }

        void ShowInfo(GarageElement e)
        {
            _infoName.text = e.name;
            _infoText.text = e.info;
        }

        // ---------- Pieces ----------

        void BuildPieces(Vector2 boardSize)
        {
            var frameSize = _frame.rect.size;
            var frameRect = new Rect(-frameSize / 2f, frameSize);
            var homes = new List<Vector2>();
            foreach (var e in _data.elements)
            {
                var piece = new Piece { part = ProbeAssembly.Part(e) };
                piece.placed = App.GaragePlaced.Contains(e.id);
                piece.target = ProbeAssembly.LocalCenter(piece.part.area, frameSize);
                var size = ProbeAssembly.LocalSize(piece.part.area, frameSize);

                var img = ProbeAssembly.PartImage(piece.part, piece.placed ? _placedLayer : _looseLayer, e.id);
                img.raycastTarget = true;
                piece.rect = img.rectTransform;
                if (size.x < MinGrab || size.y < MinGrab)
                {
                    // Invisible, bigger grab area for tiny parts (pointer events bubble up to the part).
                    var hit = UIFactory.Line(img.transform, Color.clear);
                    hit.raycastTarget = true;
                    hit.rectTransform.Pin(new Vector2(0.5f, 0.5f), Vector2.Max(size, new Vector2(MinGrab, MinGrab)), Vector2.zero);
                }
                piece.rect.anchorMin = piece.rect.anchorMax = piece.rect.pivot = new Vector2(0.5f, 0.5f);
                piece.rect.sizeDelta = size;

                piece.home = ScatterPosition(size, piece.target, boardSize, frameRect, homes);
                homes.Add(piece.home);
                piece.goal = piece.placed ? piece.target : piece.home;
                piece.tilt = piece.placed ? 0f : Random.Range(-12f, 12f);
                piece.rect.anchoredPosition = piece.goal;
                piece.rect.localRotation = Quaternion.Euler(0, 0, piece.tilt);

                var handler = img.gameObject.AddComponent<HoverDragHandler>();
                handler.Entered += () => ShowInfo(e);
                handler.DragStarted += ev => BeginDrag(piece, ev);
                handler.Dragged += ev => Drag(piece, ev);
                handler.DragEnded += ev => EndDrag(piece, ev);
                _pieces.Add(piece);
            }
            foreach (var piece in _pieces)
                if (piece.placed) SortPlaced(piece);
        }

        /// <summary>A random spot on the garage floor, preferably outside the outline and away from the other parts.</summary>
        static Vector2 ScatterPosition(Vector2 size, Vector2 target, Vector2 boardSize, Rect frameRect, List<Vector2> others)
        {
            var half = boardSize / 2f - size / 2f - new Vector2(10, 10);
            half = Vector2.Max(half, Vector2.zero);
            Vector2 best = Vector2.zero;
            float bestScore = float.MinValue;
            for (int i = 0; i < 80; i++)
            {
                var p = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
                var r = new Rect(p - size / 2f, size);
                float score = 0;
                if (r.Overlaps(frameRect)) score -= 2000;
                if (Vector2.Distance(p, target) < 220) score -= 1000;
                float nearest = 600;
                foreach (var o in others) nearest = Mathf.Min(nearest, Vector2.Distance(o, p));
                score += nearest;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>Keeps the placed parts layered in content order, whatever order they were attached in.</summary>
        void SortPlaced(Piece piece)
        {
            int index = 0;
            foreach (var other in _pieces)
            {
                if (other == piece) break;
                if (other.placed) index++;
            }
            piece.rect.SetSiblingIndex(Mathf.Min(index, _placedLayer.childCount - 1));
        }

        void Update()
        {
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f);
            foreach (var piece in _pieces)
            {
                if (piece.dragging) continue;
                piece.rect.anchoredPosition = Vector2.Lerp(piece.rect.anchoredPosition, piece.goal, k);
                piece.rect.localRotation = Quaternion.Slerp(piece.rect.localRotation, Quaternion.Euler(0, 0, piece.tilt), k);
            }
            if (_ghost != null)
            {
                var dragged = _pieces.Find(p => p.dragging);
                bool near = dragged != null && Vector2.Distance(dragged.rect.anchoredPosition, dragged.target) <= SnapDistance;
                float pulse = 0.45f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                var color = near ? Theme.Success : Theme.Accent;
                foreach (var g in _ghost.GetComponentsInChildren<Graphic>())
                    g.color = new Color(color.r, color.g, color.b, (g.name == "Glow" ? 0.5f : 1f) * pulse);
            }
        }

        // ---------- Dragging ----------

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, e.position, e.pressEventCamera, out var local);
            return local;
        }

        void BeginDrag(Piece piece, PointerEventData e)
        {
            if (piece.placed || _pending != null) return;
            piece.dragging = true;
            piece.tilt = 0f;
            piece.rect.localRotation = Quaternion.identity;
            piece.rect.SetAsLastSibling();
            _dragOffset = piece.rect.anchoredPosition - Local(e);
            ShowInfo(piece.part.element);
            ShowGhost(piece);
        }

        void Drag(Piece piece, PointerEventData e)
        {
            if (!piece.dragging) return;
            piece.rect.anchoredPosition = Local(e) + _dragOffset;
        }

        void EndDrag(Piece piece, PointerEventData e)
        {
            if (!piece.dragging) return;
            piece.dragging = false;
            HideGhost();
            var p = piece.rect.anchoredPosition;
            if (Vector2.Distance(p, piece.target) <= SnapDistance)
            {
                if (piece.part.element.optional)
                {
                    piece.goal = p;
                    OpenPrompt(piece);
                }
                else Attach(piece, true);
                return;
            }
            // Dropped elsewhere: it stays where it was put (inside the garage).
            var half = _board.rect.size / 2f - piece.rect.sizeDelta / 4f;
            piece.home = new Vector2(Mathf.Clamp(p.x, -half.x, half.x), Mathf.Clamp(p.y, -half.y, half.y));
            piece.goal = piece.home;
        }

        void ShowGhost(Piece piece)
        {
            HideGhost();
            _ghost = UIFactory.Rect("Ghost", _frame);
            ProbeAssembly.Place(_ghost, piece.part.area, _frame.rect.size);
            var glow = UIFactory.Picture("Glow", _ghost, ProceduralArt.Glow(), Theme.Accent);
            glow.preserveAspect = false;
            glow.rectTransform.Stretch(-40, -40, -40, -40);
            var shape = ProbeAssembly.PartImage(piece.part, _ghost, "Shape");
            shape.rectTransform.Stretch();
            foreach (var t in shape.GetComponentsInChildren<Text>()) t.enabled = false;
        }

        void HideGhost()
        {
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
        }

        void Attach(Piece piece, bool animate)
        {
            piece.placed = true;
            piece.tilt = 0f;
            App.GaragePlaced.Add(piece.part.element.id);
            piece.rect.SetParent(_placedLayer, true); // the frame shares the board's centre, so the position carries over
            piece.goal = piece.target;
            SortPlaced(piece);
            App.Spend(piece.part.element.cost); // every part is paid for as it is installed
            if (animate)
            {
                GameAudio.Play("popup", 0.8f);
                StartCoroutine(Pop(piece.rect));
            }
            bool wasComplete = _complete;
            _complete = AllEssentialPlaced();
            if (_complete && !wasComplete) StartCoroutine(Complete(animate));
            RefreshChecklist();
            UpdateStatus();
        }

        static IEnumerator Pop(RectTransform rt)
        {
            const float duration = 0.3f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break; // the box was replaced (e.g. by the over-budget line)
                float s = 1f + 0.12f * Mathf.Sin(Mathf.PI * t / duration);
                rt.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        bool AllEssentialPlaced()
        {
            foreach (var e in _data.elements)
                if (!e.optional && !App.GaragePlaced.Contains(e.id)) return false;
            return true;
        }

        /// <summary>Outline out, main structure in behind the parts.</summary>
        IEnumerator Complete(bool animate)
        {
            if (animate) GameAudio.Play("success", 0.8f);
            const float duration = 1.2f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                _structure.alpha = k;
                _outline.alpha = 1f - k;
                yield return null;
            }
            _structure.alpha = 1f;
            _outline.alpha = 0f;
        }

        // ---------- Optional part: "Are you sure we need it?" ----------

        string _speaker;

        void OpenPrompt(Piece piece)
        {
            _pending = piece;
            _speaker = App.SpeakerOrRandom(_data.optionalSpeakerId);
            var cost = piece.part.element.cost;
            var box = Speech(_data.optionalLines);
            var attach = UIFactory.Button("Attach", box,
                (_data.attachAnyway ?? "").ToUpperInvariant() + (cost > 0 ? "  (−" + BudgetHud.Money(cost) + ")" : ""), () => ClosePrompt(true));
            attach.GetComponent<RectTransform>().Pin(new Vector2(1, 0), new Vector2(560, 70), new Vector2(-30, 26));
            attach.transform.Find("Label").GetComponent<Text>().BestFit(16, 26);
            var leave = UIFactory.Button("Leave", box, (_data.leaveIt ?? "").ToUpperInvariant(), () => ClosePrompt(false));
            leave.GetComponent<RectTransform>().Pin(new Vector2(1, 0), new Vector2(260, 70), new Vector2(-610, 26));
            leave.transform.Find("Label").GetComponent<Text>().BestFit(16, 26);
        }

        /// <summary>A team member at the bottom of the screen with a speech box; blocks the garage until closed.</summary>
        RectTransform Speech(string[] lines)
        {
            ClosePromptUi();
            GameAudio.Play("popup");
            var shade = UIFactory.Line(Root, new Color(0, 0, 0, 0.45f));
            shade.raycastTarget = true; // blocks the garage while the question is open
            _prompt = shade.rectTransform;
            _prompt.Stretch();

            var portrait = PortraitCard(_prompt, _speaker);
            portrait.Pin(new Vector2(0, 0), new Vector2(240, 300), new Vector2(120, 80));

            var box = UIFactory.Panel("Box", _prompt, Theme.Panel, 22);
            box.raycastTarget = false;
            box.rectTransform.Anchor(Vector2.zero, new Vector2(1, 0), new Vector2(400, 80), new Vector2(-520, 330));
            UIFactory.Frame(box.transform, Theme.AccentDark, 22, 2);
            var person = Content.FindCharacter(_speaker);
            UIFactory.Label("Speaker", box.transform, person != null ? person.name.ToUpperInvariant() : "", 24, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(36, -60), new Vector2(-36, -22));
            UIFactory.Label("Text", box.transform, lines != null ? string.Join("\n", lines) : "", 32, Theme.Text).BestFit(20, 32)
                .rectTransform.Stretch(36, 70, 36, 110);
            StartCoroutine(Pop(box.rectTransform));
            return box.rectTransform;
        }

        void ClosePromptUi()
        {
            if (_prompt != null) Destroy(_prompt.gameObject);
            _prompt = null;
        }

        void ClosePrompt(bool attach)
        {
            var piece = _pending;
            _pending = null;
            ClosePromptUi();
            if (piece == null) return;
            if (!attach)
            {
                piece.goal = piece.home;
                return;
            }
            Attach(piece, true);
            // The same team member scolds the player for going over budget.
            if (_data.overBudgetLines == null || _data.overBudgetLines.Length == 0) return;
            var box = Speech(_data.overBudgetLines);
            var ok = UIFactory.Button("Ok", box, Ui.next.ToUpperInvariant(), ClosePromptUi);
            ok.GetComponent<RectTransform>().Pin(new Vector2(1, 0), new Vector2(260, 70), new Vector2(-30, 26));
        }

        /// <summary>Dev shortcut L: attaches every essential part.</summary>
        public void DevPlaceAll()
        {
            if (_pending != null) return;
            foreach (var piece in _pieces)
                if (!piece.placed && !piece.part.element.optional) Attach(piece, false);
        }
    }
}
