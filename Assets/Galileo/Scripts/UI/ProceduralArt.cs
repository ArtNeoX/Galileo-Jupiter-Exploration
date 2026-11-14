using System;
using System.Collections.Generic;
using UnityEngine;

namespace Galileo.UI
{
    /// <summary>
    /// Generates the UI shapes and line-art icons at runtime, so the scenes run without any art assets.
    /// Any icon can be replaced by putting a sprite with the same name in Resources/Galileo/Icons/.
    /// </summary>
    public static class ProceduralArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite RoundedRect(int radius) => Cached("rect" + radius, () => BuildRounded(radius, 0));
        public static Sprite RoundedFrame(int radius, int thickness) =>
            Cached($"frame{radius}_{thickness}", () => BuildRounded(radius, thickness));
        public static Sprite VerticalGradient(Color top, Color bottom) =>
            Cached("grad" + top + bottom, () => BuildGradient(top, bottom));
        public static Sprite Icon(string id) => Cached("icon_" + id, () => BuildIcon(id));
        /// <summary>Same shapes as <see cref="Icon"/>, rendered at a custom resolution (for large placeholders).</summary>
        public static Sprite Shape(string id, int size) => Cached($"shape_{id}_{size}", () => BuildIcon(id, size));
        /// <summary>Soft radial falloff, white in the centre and transparent at the edge (Sun glow).</summary>
        public static Sprite Glow() => Cached("glow", BuildGlow);

        static Sprite Cached(string key, Func<Sprite> make)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null)
            {
                s = make();
                Cache[key] = s;
            }
            return s;
        }

        static Texture2D NewTexture(int w, int h) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

        // ---------- 9-sliced rounded rectangles ----------

        static Sprite BuildRounded(int radius, int thickness)
        {
            int border = radius + 2;
            int size = border * 2 + 2;
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half));
                var q = p - new Vector2(half - radius - 1, half - radius - 1);
                float sdf = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                float a = Mathf.Clamp01(0.5f - sdf);
                if (thickness > 0) a -= Mathf.Clamp01(0.5f - (sdf + thickness));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite BuildGradient(Color top, Color bottom)
        {
            const int h = 256;
            var tex = NewTexture(1, h);
            for (int y = 0; y < h; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (h - 1f)));
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------- Line-art icons (signed distance fields in [-1,1] space) ----------

        const float W = 0.07f; // stroke width

        static Sprite BuildGlow()
        {
            const int size = 128;
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite BuildIcon(string id, int size = 256)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float pixel = 2f / size;
            var shape = IconShape(id);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float a = Mathf.Clamp01(0.5f - shape(p) / pixel);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Func<Vector2, float> IconShape(string id)
        {
            switch (id)
            {
                case "atmosphere":
                {
                    var c = new Vector2(0f, -1.6f);
                    return p => Max(
                        Min(Ring(p, c, 1.95f, W), Ring(p, c, 1.7f, W * 0.6f), Ring(p, c, 1.45f, W * 0.5f), Ring(p, c, 1.2f, W * 0.4f)),
                        Box(p, 0.85f));
                }
                case "magnetosphere":
                    return p => Min(
                        Circle(p, Vector2.zero, 0.17f),
                        EllipseRing(p, new Vector2(-0.4f, 0f), 0.4f, 0.24f, W * 0.7f),
                        EllipseRing(p, new Vector2(0.4f, 0f), 0.4f, 0.24f, W * 0.7f),
                        EllipseRing(p, new Vector2(-0.52f, 0f), 0.52f, 0.44f, W * 0.7f),
                        EllipseRing(p, new Vector2(0.52f, 0f), 0.52f, 0.44f, W * 0.7f));
                case "moons":
                    return p => Min(
                        Circle(p, Vector2.zero, 0.2f),
                        Ring(p, Vector2.zero, 0.45f, W * 0.6f),
                        Ring(p, Vector2.zero, 0.75f, W * 0.6f),
                        Circle(p, Polar(0.45f, 40f), 0.075f),
                        Circle(p, Polar(0.75f, 200f), 0.09f),
                        Circle(p, Polar(0.75f, -30f), 0.06f));
                case "probe":
                {
                    var top = new Vector2(0f, 0.05f);
                    var bottom = new Vector2(0f, -0.45f);
                    return p => Min(
                        Max(Ring(p, top, 0.55f, W), top.y - p.y),
                        Segment(p, new Vector2(-0.55f, 0.05f), bottom, W * 0.5f),
                        Segment(p, new Vector2(0.55f, 0.05f), bottom, W * 0.5f),
                        Segment(p, top, bottom, W * 0.4f),
                        Circle(p, new Vector2(0f, -0.56f), 0.14f));
                }
                case "flyby":
                {
                    var c = new Vector2(0f, -1.3f);
                    return p => Min(
                        Circle(p, new Vector2(0f, -0.3f), 0.3f),
                        Max(Ring(p, c, 1.6f, W * 0.7f), Box(p, 0.85f)),
                        Circle(p, new Vector2(-0.6f, 0.183f), 0.08f));
                }
                case "orbiter":
                    return p => Min(
                        Circle(p, Vector2.zero, 0.28f),
                        Ring(p, Vector2.zero, 0.66f, W * 0.7f),
                        Circle(p, Polar(0.66f, 45f), 0.09f));
                case "orbiter_probe":
                    return p => Min(
                        Circle(p, Vector2.zero, 0.28f),
                        Ring(p, Vector2.zero, 0.66f, W * 0.7f),
                        Ring(p, Vector2.zero, 0.82f, W * 0.45f),
                        Circle(p, Polar(0.66f, 45f), 0.09f),
                        Segment(p, Polar(0.66f, 135f), Polar(0.38f, 135f), W * 0.35f),
                        Circle(p, Polar(0.42f, 135f), 0.06f));
                case "person":
                    return p => Min(
                        Circle(p, new Vector2(0f, 0.36f), 0.24f),
                        Max(Circle(p, new Vector2(0f, -0.95f), 0.72f), -(p.y + 0.92f)));
                case "triangle_down":
                    return p => ConvexPolygon(p, new Vector2(0f, -0.5f), new Vector2(0.65f, 0.4f), new Vector2(-0.65f, 0.4f));
                // ----- Scene 4 UI symbols -----
                case "check":
                    return p => Min(
                        Ring(p, Vector2.zero, 0.82f, W),
                        Segment(p, new Vector2(-0.38f, 0.02f), new Vector2(-0.1f, -0.28f), W * 1.4f),
                        Segment(p, new Vector2(-0.1f, -0.28f), new Vector2(0.42f, 0.32f), W * 1.4f));
                case "cross":
                    return p => Min(
                        Ring(p, Vector2.zero, 0.82f, W),
                        Segment(p, new Vector2(-0.32f, -0.32f), new Vector2(0.32f, 0.32f), W * 1.4f),
                        Segment(p, new Vector2(-0.32f, 0.32f), new Vector2(0.32f, -0.32f), W * 1.4f));
                case "pending":
                    return p => Circle(p, Vector2.zero, 0.42f);
                case "chevron_left":
                    return p => Min(
                        Segment(p, new Vector2(0.2f, 0.48f), new Vector2(-0.26f, 0f), W * 2f),
                        Segment(p, new Vector2(-0.26f, 0f), new Vector2(0.2f, -0.48f), W * 2f));
                case "play":
                    return p => ConvexPolygon(p, new Vector2(-0.4f, -0.5f), new Vector2(0.5f, 0f), new Vector2(-0.4f, 0.5f));
                case "spinner":
                    return p => Max(Ring(p, Vector2.zero, 0.7f, W * 1.6f), p.x > 0f && p.y < 0f ? 1f : -1f);

                // ----- Scene 4 design options -----
                case "vault": // aluminium radiation vault: box around a chip
                    return p => Min(
                        Outline(RectSdf(p, Vector2.zero, 0.68f, 0.68f), W * 1.4f),
                        RectSdf(p, Vector2.zero, 0.22f, 0.22f),
                        Segment(p, new Vector2(-0.4f, 0.12f), new Vector2(0.4f, 0.12f), W * 0.5f),
                        Segment(p, new Vector2(-0.4f, -0.12f), new Vector2(0.4f, -0.12f), W * 0.5f));
                case "dense_shield": // tantalum: heavy solid plate
                    return p => Min(
                        Outline(RectSdf(p, Vector2.zero, 0.7f, 0.5f), W),
                        RectSdf(p, Vector2.zero, 0.55f, 0.35f));
                case "layers": // multilayer insulation
                    return p => Min(
                        Segment(p, new Vector2(-0.7f, 0.5f), new Vector2(0.55f, 0.5f), W),
                        Segment(p, new Vector2(-0.6f, 0.17f), new Vector2(0.65f, 0.17f), W),
                        Segment(p, new Vector2(-0.7f, -0.17f), new Vector2(0.55f, -0.17f), W),
                        Segment(p, new Vector2(-0.6f, -0.5f), new Vector2(0.65f, -0.5f), W));
                case "coolant": // pumped liquid loop
                    return p => Min(
                        Outline(RectSdf(p, Vector2.zero, 0.6f, 0.42f) - 0.15f, W),
                        Circle(p, new Vector2(0.75f, 0f), 0.16f),
                        ConvexPolygon(p, new Vector2(-0.15f, 0.47f), new Vector2(0.05f, 0.57f), new Vector2(0.05f, 0.37f)));
                case "panels": // aluminium structural panels
                    return p => Min(
                        Outline(RectSdf(p, Vector2.zero, 0.7f, 0.6f), W),
                        Segment(p, new Vector2(0f, -0.6f), new Vector2(0f, 0.6f), W * 0.7f),
                        Segment(p, new Vector2(-0.7f, 0f), new Vector2(0.7f, 0f), W * 0.7f));
                case "truss": // titanium primary frame
                    return p => Min(
                        Outline(RectSdf(p, Vector2.zero, 0.7f, 0.6f), W * 1.3f),
                        Segment(p, new Vector2(-0.7f, -0.6f), new Vector2(0.7f, 0.6f), W),
                        Segment(p, new Vector2(-0.7f, 0.6f), new Vector2(0.7f, -0.6f), W));
                case "rtg": // finned cylinder
                    return p => Min(
                        Outline(RectSdf(p, Vector2.zero, 0.22f, 0.72f), W),
                        Segment(p, new Vector2(-0.62f, 0.4f), new Vector2(0.62f, 0.4f), W),
                        Segment(p, new Vector2(-0.62f, 0f), new Vector2(0.62f, 0f), W),
                        Segment(p, new Vector2(-0.62f, -0.4f), new Vector2(0.62f, -0.4f), W));
                case "solar": // body with two solar wings
                    return p => Min(
                        RectSdf(p, Vector2.zero, 0.13f, 0.18f),
                        Outline(RectSdf(p, new Vector2(-0.55f, 0f), 0.3f, 0.45f), W * 0.8f),
                        Outline(RectSdf(p, new Vector2(0.55f, 0f), 0.3f, 0.45f), W * 0.8f),
                        Segment(p, new Vector2(-0.55f, -0.45f), new Vector2(-0.55f, 0.45f), W * 0.5f),
                        Segment(p, new Vector2(0.55f, -0.45f), new Vector2(0.55f, 0.45f), W * 0.5f),
                        Segment(p, new Vector2(-0.25f, 0f), new Vector2(0.25f, 0f), W * 0.6f));

                // ----- Gravity assist / launch -----
                case "disc":
                    return p => Circle(p, Vector2.zero, 0.94f);
                case "ring":
                    return p => Ring(p, Vector2.zero, 0.84f, 0.14f);
                case "x_mark":
                    return p => Min(
                        Segment(p, new Vector2(-0.7f, -0.7f), new Vector2(0.7f, 0.7f), 0.3f),
                        Segment(p, new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), 0.3f));
                case "shuttle": // front view of the stack: external tank, two boosters, orbiter with wings
                    return p => Min(
                        Min(RectSdf(p, new Vector2(0f, -0.12f), 0.15f, 0.7f), Circle(p, new Vector2(0f, 0.55f), 0.15f),
                            ConvexPolygon(p, new Vector2(-0.15f, 0.55f), new Vector2(0.15f, 0.55f), new Vector2(0f, 0.95f))),
                        RectSdf(p, new Vector2(-0.3f, -0.2f), 0.075f, 0.62f),
                        ConvexPolygon(p, new Vector2(-0.375f, 0.42f), new Vector2(-0.225f, 0.42f), new Vector2(-0.3f, 0.62f)),
                        RectSdf(p, new Vector2(0.3f, -0.2f), 0.075f, 0.62f),
                        ConvexPolygon(p, new Vector2(0.225f, 0.42f), new Vector2(0.375f, 0.42f), new Vector2(0.3f, 0.62f)),
                        RectSdf(p, new Vector2(-0.3f, -0.86f), 0.06f, 0.05f),
                        RectSdf(p, new Vector2(0.3f, -0.86f), 0.06f, 0.05f),
                        ConvexPolygon(p, new Vector2(-0.62f, -0.78f), new Vector2(0.62f, -0.78f), new Vector2(0.09f, -0.05f), new Vector2(-0.09f, -0.05f)));
                case "shuttle_flames": // exhaust plumes; the top edge (y = 0.9) lines up with the bottom of "shuttle"
                    return p => Min(
                        ConvexPolygon(p, new Vector2(-0.4f, 0.92f), new Vector2(-0.3f, -0.7f), new Vector2(-0.2f, 0.92f)),
                        ConvexPolygon(p, new Vector2(0.2f, 0.92f), new Vector2(0.3f, -0.7f), new Vector2(0.4f, 0.92f)),
                        ConvexPolygon(p, new Vector2(-0.1f, 0.99f), new Vector2(0f, 0.3f), new Vector2(0.1f, 0.99f)));
                case "cloud":
                    return p => Min(
                        Circle(p, new Vector2(-0.45f, -0.15f), 0.35f),
                        Circle(p, new Vector2(0f, 0.05f), 0.48f),
                        Circle(p, new Vector2(0.48f, -0.12f), 0.38f),
                        RectSdf(p, new Vector2(0f, -0.3f), 0.5f, 0.2f));

                default:
                    return p => Circle(p, Vector2.zero, 0.4f);
            }
        }

        static Vector2 Polar(float r, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        static float Min(params float[] v)
        {
            float m = float.MaxValue;
            foreach (var x in v) m = Mathf.Min(m, x);
            return m;
        }

        static float Max(float a, float b) => Mathf.Max(a, b);
        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Ring(Vector2 p, Vector2 c, float r, float w) => Mathf.Abs((p - c).magnitude - r) - w * 0.5f;
        static float Box(Vector2 p, float half) => Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half;
        static float RectSdf(Vector2 p, Vector2 c, float hx, float hy) => Mathf.Max(Mathf.Abs(p.x - c.x) - hx, Mathf.Abs(p.y - c.y) - hy);
        static float Outline(float sdf, float w) => Mathf.Abs(sdf) - w * 0.5f;

        static float EllipseRing(Vector2 p, Vector2 c, float rx, float ry, float w)
        {
            var q = new Vector2((p.x - c.x) / rx, (p.y - c.y) / ry);
            float d = (q.magnitude - 1f) * Mathf.Min(rx, ry);
            return Mathf.Abs(d) - w * 0.5f;
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b, float w)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / ba.sqrMagnitude);
            return (pa - ba * h).magnitude - w * 0.5f;
        }

        /// <summary>Vertices in counter-clockwise order.</summary>
        static float ConvexPolygon(Vector2 p, params Vector2[] v)
        {
            float d = float.MinValue;
            for (int i = 0; i < v.Length; i++)
            {
                var e = v[(i + 1) % v.Length] - v[i];
                var n = new Vector2(e.y, -e.x).normalized;
                d = Mathf.Max(d, Vector2.Dot(p - v[i], n));
            }
            return d;
        }
    }
}
