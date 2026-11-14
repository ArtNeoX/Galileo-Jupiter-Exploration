using System.Collections.Generic;
using Galileo.Content;
using Galileo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.Screens
{
    /// <summary>
    /// Scene 3: choose the mission concept. Hovering a variant expands it with its description,
    /// pros (green) and cons (red). Clicking selects it; Next unlocks once something is selected.
    /// </summary>
    public class ConceptsScreen : ScreenBase
    {
        readonly List<ExpandableCard> _cards = new List<ExpandableCard>();
        readonly Dictionary<ExpandableCard, ConceptInfo> _concepts = new Dictionary<ExpandableCard, ConceptInfo>();
        ConceptInfo _selected;
        Text _selectedLabel;
        Button _next;

        protected override void Build()
        {
            var data = Content.concepts;
            Header(data.title, data.subtitle);

            var row = CardRow.Create(Root);
            foreach (var item in data.items)
            {
                var icon = IconFor(item.icon);
                var card = ExpandableCard.Create(row, item.letter, item.title, icon, view => BuildDetails(view, item, icon));
                card.Hovered += Expand;
                card.Clicked += Select;
                _cards.Add(card);
                _concepts[card] = item;
            }

            _selectedLabel = FooterLabel(Ui.nothingSelected);
            _selectedLabel.fontStyle = FontStyle.Bold;
            if (!string.IsNullOrEmpty(data.hint))
                UIFactory.Label("Hint", Root, data.hint, 22, Theme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic)
                    .rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(120, 40), new Vector2(-420, 80));
            _next = NextButton(() => App.ConfirmConcept(_selected));
            _next.interactable = false;
        }

        void BuildDetails(RectTransform view, ConceptInfo item, Sprite icon)
        {
            UIFactory.Picture("Icon", view, icon, Theme.Icon).rectTransform
                .Pin(new Vector2(1, 1), new Vector2(120, 120), Vector2.zero);
            UIFactory.Label("Name", view, $"{item.letter}.  {item.title}", 38, Theme.Accent, TextAnchor.UpperLeft, FontStyle.Bold)
                .BestFit(26, 38).rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(-140, 0));
            UIFactory.Label("Subtitle", view, item.subtitle, 24, Theme.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic)
                .rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(-140, -64));
            UIFactory.Line(view, Theme.Line).rectTransform
                .Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -136), new Vector2(0, -134));

            var body = item.info +
                       $"\n\n<color={Theme.ProsHex}><b>{Ui.prosLabel}:</b> {item.pros}</color>" +
                       $"\n\n<color={Theme.ConsHex}><b>{Ui.consLabel}:</b> {item.cons}</color>";
            UIFactory.Label("Info", view, body, 28, Theme.Text).BestFit(15, 28).rectTransform
                .Anchor(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -156));
        }

        void Expand(ExpandableCard target)
        {
            foreach (var card in _cards) card.SetExpanded(card == target);
        }

        void Select(ExpandableCard target)
        {
            Expand(target);
            _selected = _concepts[target];
            App.SetBackground(_selected.background);
            foreach (var card in _cards) card.SetSelected(card == target);
            _selectedLabel.text = string.Format(Ui.selectedFormat, _selected.letter);
            _next.interactable = true;
        }
    }
}
