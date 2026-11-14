using UnityEngine;

namespace Galileo.UI
{
    /// <summary>
    /// Colour scheme taken from refs/ui_reference.png: near-black portrait area, warm dark-brown panels
    /// and cream text/accents.
    /// </summary>
    public static class Theme
    {
        // Reference palette
        const string Charcoal = "171717";   // portrait background
        const string BrownDark = "30231F";  // name tab
        const string Brown = "362622";      // dialog body
        const string Cream = "F2E3C6";      // text and ▼ indicator

        public static readonly Color BackgroundTop = Hex("2A1E1A");
        public static readonly Color BackgroundBottom = Hex("0E0A09");
        public static readonly Color BackgroundDim = Hex("120C0A", 0.6f);
        public static readonly Color Panel = Hex(Brown, 0.96f);
        public static readonly Color PanelDark = Hex(BrownDark);
        public static readonly Color PanelDeep = Hex(Charcoal);
        public static readonly Color Line = Hex("5A483F");
        public static readonly Color Accent = Hex(Cream);
        public static readonly Color AccentBright = Hex("FFF5E2");
        public static readonly Color AccentDark = Hex("BFAE90");
        public static readonly Color Disabled = Hex("5A483F", 0.85f);
        public static readonly Color Text = Hex(Cream);
        public static readonly Color TextMuted = Hex("B3A48C");
        public static readonly Color ButtonText = Hex(BrownDark);
        public static readonly Color Icon = Hex(Cream, 0.9f);
        public static readonly Color Placeholder = Hex("4A3A33");
        public static readonly Color Success = Hex("9CC48A");

        /// <summary>Rich-text colours (title highlights, pros in green, cons in red), tuned for the brown panels.</summary>
        public const string AccentHex = "#F2E3C6";
        public const string ProsHex = "#9CC48A";
        public const string ConsHex = "#E8705F";

        static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }
    }
}
