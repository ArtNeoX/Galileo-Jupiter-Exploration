using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.UI
{
    /// <summary>A polyline drawn as a uGUI mesh (orbits, trajectories, the launch timing bar). Optionally dashed.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILine : MaskableGraphic
    {
        readonly List<Vector2> _points = new List<Vector2>();
        float _thickness = 2f;
        bool _closed;
        float _dash, _gap;

        public static UILine Create(string name, Transform parent, Color color, float thickness)
        {
            var rt = UIFactory.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            var line = rt.gameObject.AddComponent<UILine>();
            line.color = color;
            line._thickness = thickness;
            line.raycastTarget = false;
            return line;
        }

        public UILine Dashed(float dash, float gap)
        {
            _dash = dash;
            _gap = gap;
            SetVerticesDirty();
            return this;
        }

        public void SetPoints(IList<Vector2> points, bool closed = false)
        {
            _points.Clear();
            _points.AddRange(points);
            _closed = closed;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = _points.Count;
            if (n < 2) return;
            int segments = _closed ? n : n - 1;
            float travelled = 0f;
            for (int i = 0; i < segments; i++)
            {
                var a = _points[i];
                var b = _points[(i + 1) % n];
                float len = Vector2.Distance(a, b);
                if (len < 0.0001f) continue;
                if (_dash <= 0f)
                {
                    // Extend each segment by half the thickness so the joints have no gaps.
                    var dir = (b - a) / len * (_thickness * 0.5f);
                    Quad(vh, i == 0 && !_closed ? a : a - dir, i == segments - 1 && !_closed ? b : b + dir);
                    continue;
                }
                float period = _dash + _gap;
                float s = 0f;
                while (s < len)
                {
                    float phase = (travelled + s) % period;
                    if (phase < _dash)
                    {
                        float e = Mathf.Min(len, s + (_dash - phase));
                        Quad(vh, Vector2.Lerp(a, b, s / len), Vector2.Lerp(a, b, e / len));
                        s = e;
                    }
                    else s += period - phase;
                }
                travelled += len;
            }
        }

        void Quad(VertexHelper vh, Vector2 a, Vector2 b)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-8f) return;
            var n = new Vector2(-d.y, d.x).normalized * (_thickness * 0.5f);
            int start = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.color = color;
            v.position = a - n; vh.AddVert(v);
            v.position = a + n; vh.AddVert(v);
            v.position = b + n; vh.AddVert(v);
            v.position = b - n; vh.AddVert(v);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
