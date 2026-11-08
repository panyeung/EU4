using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Shared visual language for the M02 character/daily-action page: Japanese-paper
    /// parchment, deep indigo and vermilion. Colours are plain uGUI values so the scene
    /// generator can assign them without importing a theme asset. The serif font is an
    /// operating-system font resolved at runtime (no font file is redistributed); it falls
    /// back to Unity's built-in LegacyRuntime font when the host has no CJK serif family.
    /// </summary>
    public static class SengokuUiTheme
    {
        public static readonly Color Parchment = new Color32(0xEF, 0xE4, 0xCC, 0xFF);
        public static readonly Color ParchmentLight = new Color32(0xF7, 0xEF, 0xDD, 0xFF);
        public static readonly Color ParchmentDeep = new Color32(0xE2, 0xD4, 0xB8, 0xFF);
        public static readonly Color PaperLine = new Color32(0xC9, 0xB6, 0x92, 0xFF);

        public static readonly Color Ink = new Color32(0x33, 0x2E, 0x26, 0xFF);
        public static readonly Color InkSoft = new Color32(0x5E, 0x55, 0x46, 0xFF);
        public static readonly Color InkFaint = new Color32(0x8A, 0x7D, 0x67, 0xFF);

        public static readonly Color IndigoBackdrop = new Color32(0x25, 0x2D, 0x3C, 0xFF);
        public static readonly Color IndigoDeep = new Color32(0x18, 0x20, 0x2E, 0xFF);
        public static readonly Color IndigoRail = new Color32(0x1B, 0x23, 0x33, 0xFF);
        public static readonly Color IndigoPanel = new Color32(0x22, 0x2B, 0x3D, 0xFF);
        public static readonly Color IndigoLine = new Color32(0x42, 0x4F, 0x66, 0xFF);

        public static readonly Color Gold = new Color32(0xC3, 0xA4, 0x69, 0xFF);
        public static readonly Color GoldDim = new Color32(0x8E, 0x7A, 0x51, 0xFF);
        public static readonly Color Vermilion = new Color32(0xA5, 0x39, 0x2B, 0xFF);
        public static readonly Color VermilionDark = new Color32(0x82, 0x2B, 0x21, 0xFF);
        public static readonly Color Jade = new Color32(0x5C, 0x7A, 0x4F, 0xFF);
        public static readonly Color JadeLight = new Color32(0x86, 0xA0, 0x6A, 0xFF);
        public static readonly Color SelectedRow = new Color32(0x3A, 0x2B, 0x25, 0xFF);

        private static Font _serif;
        private static bool _serifResolved;

        /// <summary>
        /// Runtime serif/CJK face. Prefers a traditional Kai/Song family available on the
        /// Windows host, then falls back to the built-in legacy font. Never returns null.
        /// </summary>
        public static Font Serif
        {
            get
            {
                if (_serifResolved)
                    return _serif;
                _serifResolved = true;
                try
                {
                    _serif = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "KaiTi", "STKaiti", "FangSong", "SimSun", "Noto Serif CJK SC",
                        "Microsoft YaHei"
                    }, 32);
                }
                catch (System.Exception)
                {
                    _serif = null;
                }
                if (_serif == null)
                    _serif = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _serif;
            }
        }

        /// <summary>Applies the serif face to every Text under the supplied root.</summary>
        public static void ApplyFont(Transform root)
        {
            if (root == null)
                return;
            Font font = Serif;
            if (font == null)
                return;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].font = font;
        }

        /// <summary>Builds a soft paper grain tinted for a panel; null-safe.</summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
