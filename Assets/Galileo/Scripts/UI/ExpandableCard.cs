using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Galileo.UI
{
    /// <summary>
    /// A block in a horizontal row that widens when hovered (or tapped) and swaps its icon view
    /// for a detail view. Used by the objectives (Scene 2) and mission concept (Scene 3) screens.
    /// </summary>
    public class ExpandableCard : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        const float CollapsedFlex = 1f;
        const float ExpandedFlex = 3.4f;

        public event Action<ExpandableCard> Hovered;
        public event Action<ExpandableCard> Clicked;

        public bool Viewed { get; private set; }

        LayoutElement _layout;
        CanvasGroup _collapsedView;
        CanvasGroup _expandedView;
        Image _frame;
        Image _selectionFrame;
        Image _viewedBadge;
        bool _expanded;

        public static ExpandableCard Create(Transform row, string tag, string caption, Sprite icon,
            Action<RectTransform> buildExpandedView)
        {
            var bg = UIFactory.Panel("Card " + tag, row, Theme.Panel, 20);
            var card = bg.gameObject.AddComponent<ExpandableCard>();

            card._layout = bg.gameObject.AddComponent<LayoutElement>();
            card._layout.minWidth = 0;
            card._layout.preferredWidth = 0;
            card._layout.flexibleWidth = CollapsedFlex;

            card._frame = UIFactory.Frame(bg.transform, Theme.Line, 20, 2);
            card._selectionFrame = UIFactory.Frame(bg.transform, Theme.Accent, 20, 6);
            card._selectionFrame.enabled = false;

            // Collapsed: big icon, tag ("01" / "A") and a short caption.
            var collapsed = UIFactory.Rect("Collapsed", bg.transform).Stretch(12, 12, 12, 12);
            card._collapsedView = collapsed.gameObject.AddComponent<CanvasGroup>();
            UIFactory.Picture("Icon", collapsed, icon, Theme.Icon).rectTransform
                .Anchor(new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.9f), Vector2.zero, Vector2.zero);
            UIFactory.Label("Tag", collapsed, tag, 44, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform
                .Anchor(new Vector2(0f, 0.2f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero);
            UIFactory.Label("Caption", collapsed, caption, 24, Theme.TextMuted, TextAnchor.UpperCenter).BestFit(16, 24).rectTransform
                .Anchor(new Vector2(0f, 0.03f), new Vector2(1f, 0.19f), Vector2.zero, Vector2.zero);

            card._viewedBadge = UIFactory.Panel("ViewedBadge", collapsed, Theme.Success, 10);
            card._viewedBadge.raycastTarget = false;
            card._viewedBadge.rectTransform.Pin(new Vector2(1f, 1f), new Vector2(20, 20), new Vector2(-10, -10));
            card._viewedBadge.enabled = false;

            // Expanded: content supplied by the screen.
            var expanded = UIFactory.Rect("Expanded", bg.transform).Stretch(36, 30, 36, 30);
            card._expandedView = expanded.gameObject.AddComponent<CanvasGroup>();
            card._expandedView.alpha = 0f;
            buildExpandedView(expanded);

            return card;
        }

        public void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            if (expanded && !Viewed)
            {
                Viewed = true;
                _viewedBadge.enabled = true;
            }
        }

        public void SetSelected(bool selected) => _selectionFrame.enabled = selected;

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this);
        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);

        void Update()
        {
            float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            float flex = Mathf.Lerp(_layout.flexibleWidth, _expanded ? ExpandedFlex : CollapsedFlex, k);
            if (Mathf.Abs(flex - _layout.flexibleWidth) > 0.0005f) _layout.flexibleWidth = flex;

            float progress = Mathf.InverseLerp(CollapsedFlex, ExpandedFlex, _layout.flexibleWidth);
            _expandedView.alpha = Mathf.Clamp01((progress - 0.6f) / 0.4f);
            _collapsedView.alpha = Mathf.Clamp01(1f - progress / 0.4f);
            _frame.color = Color.Lerp(Theme.Line, Theme.AccentDark, progress);
        }
    }
}
