using System.Collections.Generic;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 2: the mission's science objectives. Hovering a block expands it to show its name and info.
    /// Next unlocks once every objective has been looked at.
    /// </summary>
    public class ObjectivesScreen : ScreenBase
    {
        readonly List<ExpandableCard> _cards = new List<ExpandableCard>();
        Text _progress;
        Button _next;

        protected override void Build()
        {
            var data = Content.objectives;
            Header(data.title, data.subtitle);

            var row = CardRow.Create(Root);
            foreach (var item in data.items)
            {
                var icon = IconFor(item.icon);
                var card = ExpandableCard.Create(row, item.number, item.title, icon, view =>
                {
                    UIFactory.Picture("Icon", view, icon, Theme.Icon).rectTransform
                        .Pin(new Vector2(1, 1), new Vector2(150, 150), Vector2.zero);
                    UIFactory.Label("Name", view, $"{item.number}  {item.title}", 42, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold)
                        .BestFit(28, 42).rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -110), new Vector2(-170, 0));
                    UIFactory.Line(view, Theme.Line).rectTransform
                        .Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -172), new Vector2(0, -170));
                    UIFactory.Label("Info", view, item.info, 36, Theme.Text).BestFit(20, 36).rectTransform
                        .Anchor(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -200));
                });
                card.Hovered += Expand;
                card.Clicked += Expand;
                _cards.Add(card);
            }

            _progress = FooterLabel("");
            if (!string.IsNullOrEmpty(data.hint))
                UIFactory.Label("Hint", Root, data.hint, 22, Theme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic)
                    .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(120, 40), new Vector2(-420, 80));
            _next = NextButton(App.ShowMissionDialog);
            Refresh();
        }

        void Expand(ExpandableCard target)
        {
            foreach (var card in _cards) card.SetExpanded(card == target);
            Refresh();
        }

        void Refresh()
        {
            int viewed = 0;
            foreach (var card in _cards) if (card.Viewed) viewed++;
            _progress.text = string.Format(Ui.viewedFormat, viewed, _cards.Count);
            _next.interactable = viewed == _cards.Count;
        }
    }

    static class CardRow
    {
        /// <summary>The horizontal strip of expandable blocks shared by Scenes 2 and 3.</summary>
        public static RectTransform Create(RectTransform parent)
        {
            var row = UIFactory.Rect("Cards", parent)
                .Anchor(Vector2.zero, Vector2.one, new Vector2(120, 175), new Vector2(-120, -225));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return row;
        }
    }
}
