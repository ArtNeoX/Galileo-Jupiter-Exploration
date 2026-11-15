using System.Collections.Generic;
using Galileo.Content;
using UnityEngine;
using UnityEngine.UI;

namespace Galileo.UI
{
    /// <summary>One part of the orbiter: its bitmap cropped to the visible pixels, and where it sits on the canvas.</summary>
    public class ProbePart
    {
        public GarageElement element;
        /// <summary>Cropped bitmap, or null while the bitmap is missing (a placeholder is drawn instead).</summary>
        public Sprite sprite;
        /// <summary>Where the part sits on the shared canvas, normalised 0..1 with y up.</summary>
        public Rect area;
        /// <summary>Centre of the part's solid pixels on the canvas (faint strokes can make <see cref="area"/> much bigger).</summary>
        public Vector2 focus;
    }

    /// <summary>
    /// The orbiter is painted as layers on one canvas size (Resources/Galileo/Garage/): every part, the outline
    /// and the main structure. Laid over each other at the same size and position they form the finished probe.
    /// Each part's visible area is measured from its alpha, so it can be cut out and dragged around on its own.
    /// </summary>
    public static class ProbeAssembly
    {
        public const string Folder = "Galileo/Garage/";
        static readonly Color PlaceholderFill = new Color(0.36f, 0.29f, 0.25f, 1f);

        static readonly Dictionary<string, ProbePart> Cache = new Dictionary<string, ProbePart>();

        public static Sprite Load(string file) =>
            string.IsNullOrEmpty(file) ? null : ContentLoader.LoadSprite(Folder + file);

        static string FileOf(GarageElement e) => string.IsNullOrEmpty(e.file) ? e.id : e.file;

        /// <summary>Pixel size of the shared canvas: taken from the first bitmap found, else 1920×1080.</summary>
        public static Vector2 CanvasSize(GarageData data)
        {
            var sprite = Load(data.outlineFile) ?? Load(data.structureFile);
            if (sprite == null)
                foreach (var e in data.elements)
                    if ((sprite = Load(FileOf(e))) != null) break;
            return sprite != null ? sprite.rect.size : new Vector2(1920, 1080);
        }

        public static ProbePart Part(GarageElement e)
        {
            var file = FileOf(e);
            if (Cache.TryGetValue(file, out var cached) && cached.sprite != null)
                return new ProbePart { element = e, sprite = cached.sprite, area = cached.area, focus = cached.focus };

            var full = Load(file);
            if (full == null)
            {
                var placeholder = new Rect(e.x - e.w / 2f, e.y - e.h / 2f, e.w, e.h);
                return new ProbePart { element = e, area = placeholder, focus = placeholder.center };
            }

            var bounds = VisibleBounds(full, out var solidCentre);
            var r = full.rect;
            var px = new Rect(Mathf.Floor(r.x + bounds.xMin * r.width), Mathf.Floor(r.y + bounds.yMin * r.height), 0, 0);
            px.xMax = Mathf.Min(r.xMax, Mathf.Ceil(r.x + bounds.xMax * r.width));
            px.yMax = Mathf.Min(r.yMax, Mathf.Ceil(r.y + bounds.yMax * r.height));
            var sprite = Sprite.Create(full.texture, px, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            var area = new Rect((px.x - r.x) / r.width, (px.y - r.y) / r.height, px.width / r.width, px.height / r.height);
            var part = new ProbePart { element = e, sprite = sprite, area = area, focus = solidCentre };
            Cache[file] = part;
            return part;
        }

        /// <summary>
        /// Normalised rect of the pixels that aren't transparent. Reads the texture back through the GPU so the
        /// bitmap doesn't need Read/Write enabled.
        /// </summary>
        public static Rect VisibleBounds(Sprite sprite) => VisibleBounds(sprite, out _);

        /// <param name="solidCentre">Centre of mass of the clearly opaque pixels (normalised like the result).</param>
        public static Rect VisibleBounds(Sprite sprite, out Vector2 solidCentre)
        {
            var tex = sprite.texture;
            var r = sprite.rect;
            float scale = Mathf.Min(1f, 512f / Mathf.Max(r.width, r.height));
            int w = Mathf.Max(4, Mathf.RoundToInt(r.width * scale));
            int h = Mathf.Max(4, Mathf.RoundToInt(r.height * scale));

            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(tex, rt, new Vector2(r.width / tex.width, r.height / tex.height), new Vector2(r.x / tex.width, r.y / tex.height));
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            read.Apply(false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            var pixels = read.GetPixels32();
            if (Application.isPlaying) Object.Destroy(read);
            else Object.DestroyImmediate(read);

            int minX = w, minY = h, maxX = -1, maxY = -1;
            double sumX = 0, sumY = 0, weight = 0;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte a = pixels[y * w + x].a;
                if (a >= 160)
                {
                    sumX += (x + 0.5) * a;
                    sumY += (y + 0.5) * a;
                    weight += a;
                }
                if (a <= 12) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            if (maxX < 0)
            {
                solidCentre = new Vector2(0.5f, 0.5f);
                return new Rect(0, 0, 1, 1);
            }
            solidCentre = weight > 0
                ? new Vector2((float)(sumX / weight) / w, (float)(sumY / weight) / h)
                : new Vector2((minX + maxX + 1) * 0.5f / w, (minY + maxY + 1) * 0.5f / h);
            // One pixel of slack on each side so antialiased edges aren't cut.
            return Rect.MinMaxRect(Mathf.Max(0, minX - 1) / (float)w, Mathf.Max(0, minY - 1) / (float)h,
                Mathf.Min(w, maxX + 2) / (float)w, Mathf.Min(h, maxY + 2) / (float)h);
        }

        /// <summary>Probe-shaped rect (the canvas aspect) fitted into maxSize.</summary>
        public static RectTransform Frame(string name, RectTransform parent, Vector2 canvasSize, Vector2 maxSize, Vector2 position)
        {
            float scale = Mathf.Min(maxSize.x / canvasSize.x, maxSize.y / canvasSize.y);
            var frame = UIFactory.Rect(name, parent);
            frame.Pin(new Vector2(0.5f, 0.5f), canvasSize * scale, position);
            return frame;
        }

        /// <summary>Centre of a canvas area relative to the frame's centre.</summary>
        public static Vector2 LocalCenter(Rect area, Vector2 frameSize) =>
            Vector2.Scale(area.center - new Vector2(0.5f, 0.5f), frameSize);

        public static Vector2 LocalSize(Rect area, Vector2 frameSize) => Vector2.Scale(area.size, frameSize);

        /// <summary>The part's picture (or a labelled placeholder), centred on the parent. Size it via the rect.</summary>
        public static Image PartImage(ProbePart part, Transform parent, string name)
        {
            if (part.sprite != null)
            {
                var img = UIFactory.Picture(name, parent, part.sprite, Color.white);
                img.preserveAspect = false;
                return img;
            }
            var panel = UIFactory.Panel(name, parent, PlaceholderFill, 8);
            panel.raycastTarget = false;
            UIFactory.Frame(panel.transform, Theme.AccentDark, 8, 2);
            UIFactory.Label("Name", panel.transform, part.element.name, 18, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .BestFit(8, 18).rectTransform.Stretch(4, 2, 4, 2);
            return panel;
        }

        /// <summary>Full-canvas layer (outline / main structure) stretched over the frame, or null if the bitmap is missing.</summary>
        public static Image FullLayer(string file, Transform frame, string name)
        {
            var sprite = Load(file);
            if (sprite == null) return null;
            var img = UIFactory.Picture(name, frame, sprite, Color.white);
            img.preserveAspect = false;
            img.rectTransform.Stretch();
            return img;
        }

        /// <summary>The main structure (bitmap, or a placeholder block) as a full-frame layer.</summary>
        public static RectTransform Structure(GarageData data, RectTransform frame)
        {
            var layer = FullLayer(data.structureFile, frame, "Main Structure");
            if (layer != null) return layer.rectTransform;
            var root = UIFactory.Rect("Main Structure", frame).Stretch();
            var area = new Rect(data.structureX - data.structureW / 2f, data.structureY - data.structureH / 2f, data.structureW, data.structureH);
            var block = UIFactory.Panel("Placeholder", root, new Color(0.62f, 0.55f, 0.47f), 10);
            block.raycastTarget = false;
            Place(block.rectTransform, area, frame.rect.size);
            UIFactory.Frame(block.transform, Theme.Accent, 10, 2);
            return root;
        }

        /// <summary>The outline (bitmap, or frames around every part and the structure) as a full-frame layer.</summary>
        public static RectTransform Outline(GarageData data, RectTransform frame)
        {
            var layer = FullLayer(data.outlineFile, frame, "Outline");
            if (layer != null) return layer.rectTransform;
            var root = UIFactory.Rect("Outline", frame).Stretch();
            var color = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.45f);
            var areas = new List<Rect>
            {
                new Rect(data.structureX - data.structureW / 2f, data.structureY - data.structureH / 2f, data.structureW, data.structureH)
            };
            foreach (var e in data.elements)
                if (!e.optional) areas.Add(Part(e).area);
            foreach (var area in areas)
            {
                var box = UIFactory.Rect("Box", root);
                Place(box, area, frame.rect.size);
                UIFactory.Frame(box, color, 8, 2);
            }
            return root;
        }

        public static void Place(RectTransform rt, Rect area, Vector2 frameSize)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = LocalSize(area, frameSize);
            rt.anchoredPosition = LocalCenter(area, frameSize);
        }

        /// <summary>The assembled orbiter: main structure plus the attached parts, in content order.</summary>
        public static RectTransform Composite(GarageData data, ICollection<string> attached, RectTransform parent, Vector2 maxSize, Vector2 position)
        {
            var frame = Frame("Orbiter", parent, CanvasSize(data), maxSize, position);
            Structure(data, frame);
            foreach (var e in data.elements)
            {
                if (!attached.Contains(e.id) || e.hideWhenComplete) continue; // the structure already shows it
                var part = Part(e);
                var img = PartImage(part, frame, e.id);
                Place(img.rectTransform, part.area, frame.rect.size);
            }
            return frame;
        }
    }
}
