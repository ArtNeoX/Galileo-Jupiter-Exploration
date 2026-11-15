using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.UI
{
    /// <summary>
    /// Decorative budget in the top-right corner. <see cref="Spend"/> counts the sum down and drops a red
    /// "−$X" under it. Below zero the sum turns red.
    /// </summary>
    public class BudgetHud : MonoBehaviour
    {
        Text _label, _sum;
        RectTransform _effects;
        CanvasGroup _group;
        double _shown;
        long _target;
        Coroutine _count;

        public static BudgetHud Create(RectTransform parent)
        {
            var rt = UIFactory.Rect("Budget", parent);
            rt.Pin(new Vector2(1, 1), new Vector2(420, 110), new Vector2(-24, -64));
            var hud = rt.gameObject.AddComponent<BudgetHud>();
            hud.Build(rt);
            return hud;
        }

        void Build(RectTransform rt)
        {
            _group = rt.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            var bg = UIFactory.Panel("Plate", rt, new Color(Theme.PanelDeep.r, Theme.PanelDeep.g, Theme.PanelDeep.b, 0.85f), 14);
            bg.raycastTarget = false;
            bg.rectTransform.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -78), Vector2.zero);
            UIFactory.Frame(bg.transform, Theme.Line, 14, 2);
            _label = UIFactory.Label("Label", bg.transform, "", 18, Theme.TextMuted, TextAnchor.UpperRight, FontStyle.Bold);
            _label.rectTransform.Stretch(18, 8, 18, 40);
            _sum = UIFactory.Label("Sum", bg.transform, "", 34, Theme.Accent, TextAnchor.LowerRight, FontStyle.Bold);
            _sum.horizontalOverflow = HorizontalWrapMode.Overflow;
            _sum.rectTransform.Stretch(18, 26, 18, 6);
            _effects = UIFactory.Rect("Effects", rt);
            _effects.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -78), new Vector2(0, -78));
            SetVisible(false);
        }

        public void SetLabel(string label) => _label.text = (label ?? "").ToUpperInvariant();

        public void SetVisible(bool visible) => _group.alpha = visible ? 1f : 0f;

        /// <summary>Jumps straight to the amount (new game).</summary>
        public void SetAmount(long amount)
        {
            if (_count != null) StopCoroutine(_count);
            _count = null;
            _target = amount;
            _shown = amount;
            Refresh();
        }

        public void Spend(long amount, long newTotal)
        {
            _target = newTotal;
            if (_count != null) StopCoroutine(_count);
            _count = StartCoroutine(CountTo(newTotal));
            StartCoroutine(Drop(amount));
        }

        IEnumerator CountTo(long target)
        {
            double from = _shown;
            const float duration = 0.9f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                _shown = from + (target - from) * Mathf.SmoothStep(0f, 1f, t / duration);
                Refresh();
                yield return null;
            }
            _shown = target;
            Refresh();
            _count = null;
        }

        IEnumerator Drop(long amount)
        {
            var label = UIFactory.Label("Spent", _effects, "−" + Money(amount), 28, Theme.Failure, TextAnchor.UpperRight, FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = label.rectTransform;
            rt.Anchor(new Vector2(0, 1), Vector2.one, new Vector2(0, -40), new Vector2(-18, 0));
            const float duration = 1.8f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                rt.anchoredPosition = new Vector2(0, -4 - 12 * k);
                var c = label.color;
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                label.color = c;
                yield return null;
            }
            Destroy(label.gameObject);
        }

        void Refresh()
        {
            long v = (long)System.Math.Round(_shown);
            _sum.text = v < 0 ? "−" + Money(-v) : Money(v);
            _sum.color = v < 0 ? Theme.Failure : Theme.Accent;
        }

        public static string Money(long amount) => "$ " + amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
