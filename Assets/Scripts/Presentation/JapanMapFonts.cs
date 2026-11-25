using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Resolves a runtime CJK font only from the approved system-font list in
    /// <see cref="PreferredFamilies"/>. The map does not ship or redistribute a
    /// font asset: the UI plan fixes the preference order and forbids adding an
    /// unregistered font file.
    ///
    /// TextMeshPro requires its TMP Settings asset (installed through TMP Essential
    /// Resources) before a TextMeshProUGUI can finish Awake. When that asset is
    /// absent, TMP leaves its private CanvasRenderer cache unset and RectMask2D can
    /// register the text before culling, which is the null-reference path in
    /// TextMeshProUGUI.Cull. In that case the map uses uGUI Text with the same
    /// approved dynamic OS font instead of touching TMP internals.
    ///
    /// If no approved CJK font can be created, the map does not iterate other
    /// system fonts and does not substitute a TMP default or built-in font. The
    /// Status/Warning strings and Debug.LogWarning report the missing glyphs; text
    /// labels remain non-rendering placeholders so the missing-font state is
    /// explicit rather than silently masked by a font without Chinese glyphs.
    ///
    /// The cache is static, so Enter Play Mode Options with Disable Domain Reload
    /// would otherwise preserve a resolution whose native Font object Unity has
    /// already destroyed. SubsystemRegistration resets the cache for every Play
    /// Mode, and Resolve also treats a fake-null cached Unity font as absent.
    /// </summary>
    public static class JapanMapFonts
    {
        private static readonly string[] PreferredFamilies =
        {
            "KaiTi",
            "STKaiti",
            "FangSong",
            "SimSun",
            "Noto Serif CJK SC",
            "Microsoft YaHei",
        };

        private static bool _resolved;
        private static bool _fontMissing;
        private static bool _useTmp;
        private static TMP_FontAsset _font;
        private static Font _legacyFont;
        private static string _status = string.Empty;
        private static string _warning = string.Empty;

        public static bool UseTmp
        {
            get
            {
                Resolve();
                return _useTmp;
            }
        }

        public static Font LegacyFont
        {
            get
            {
                Resolve();
                return _legacyFont;
            }
        }

        public static string Status
        {
            get
            {
                Resolve();
                return _status;
            }
        }

        public static string Warning
        {
            get
            {
                Resolve();
                return _warning;
            }
        }

        /// <summary>
        /// Creates a full-stretch child label. The caller can then adjust its
        /// RectTransform offsets. TMP is used only when TMP Settings is loaded and
        /// an approved CJK font can be created; otherwise this returns a uGUI Text
        /// with the same approved font, or a non-rendering placeholder when no
        /// approved font is installed.
        /// </summary>
        public static Graphic CreateText(RectTransform parent, string name, int fontSize,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return ConfigureText(go, fontSize, alignment);
        }

        public static Graphic ConfigureText(GameObject go, int fontSize,
            TextAlignmentOptions alignment)
        {
            if (go == null)
                return null;

            if (UseTmp)
            {
                var text = go.GetComponent<TextMeshProUGUI>();
                if (text == null)
                    text = go.AddComponent<TextMeshProUGUI>();
                text.fontSize = fontSize;
                text.alignment = alignment;
                text.color = Color.white;
                text.raycastTarget = false;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Ellipsis;
                Apply(text);
                return text;
            }

            if (LegacyFont == null)
            {
                // No approved CJK font is available. Do not add a Text with a null
                // font (layout can still query preferred sizes) and do not pick a
                // built-in or arbitrary system font. A disabled placeholder keeps
                // the layout/button geometry deterministic while Status and Warning
                // explain why no glyphs are rendered.
                var placeholder = go.GetComponent<Image>();
                if (placeholder == null)
                    placeholder = go.AddComponent<Image>();
                placeholder.color = new Color(0f, 0f, 0f, 0f);
                placeholder.raycastTarget = false;
                placeholder.enabled = false;
                return placeholder;
            }

            var legacy = go.GetComponent<Text>();
            if (legacy == null)
                legacy = go.AddComponent<Text>();
            legacy.font = LegacyFont;
            legacy.fontSize = fontSize;
            legacy.alignment = ToTextAnchor(alignment);
            legacy.color = Color.white;
            legacy.raycastTarget = false;
            legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
            legacy.verticalOverflow = VerticalWrapMode.Truncate;
            legacy.resizeTextForBestFit = false;
            return legacy;
        }

        public static void Apply(Graphic graphic)
        {
            if (graphic is TMP_Text tmp)
            {
                if (UseTmp && _font != null)
                    tmp.font = _font;
                return;
            }

            if (graphic is Text legacy && LegacyFont != null)
                legacy.font = LegacyFont;
        }

        public static void SetText(Graphic graphic, string value)
        {
            if (graphic is TMP_Text tmp)
                tmp.text = value;
            else if (graphic is Text legacy)
                legacy.text = value;
        }

        public static void SetFontSize(Graphic graphic, int fontSize)
        {
            if (graphic is TMP_Text tmp)
            {
                tmp.fontSize = fontSize;
                return;
            }

            if (graphic is Text legacy)
            {
                legacy.fontSize = fontSize;
                if (legacy.resizeTextForBestFit)
                    legacy.resizeTextMaxSize = fontSize;
            }
        }

        public static void SetAlignment(Graphic graphic, TextAlignmentOptions alignment)
        {
            if (graphic is TMP_Text tmp)
                tmp.alignment = alignment;
            else if (graphic is Text legacy)
                legacy.alignment = ToTextAnchor(alignment);
        }

        public static void SetColor(Graphic graphic, Color color)
        {
            if (graphic != null)
                graphic.color = color;
        }

        public static void SetRaycastTarget(Graphic graphic, bool enabled)
        {
            if (graphic != null)
                graphic.raycastTarget = enabled;
        }

        public static void SetWrap(Graphic graphic, bool wrap)
        {
            if (graphic is TMP_Text tmp)
            {
                tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                return;
            }

            if (graphic is Text legacy)
            {
                legacy.horizontalOverflow = wrap
                    ? HorizontalWrapMode.Wrap
                    : HorizontalWrapMode.Overflow;
                legacy.resizeTextForBestFit = !wrap;
                if (!wrap)
                    legacy.resizeTextMaxSize = Mathf.Max(8, legacy.fontSize);
            }
        }

        public static void SetEllipsis(Graphic graphic, bool ellipsis)
        {
            if (graphic is TMP_Text tmp)
            {
                tmp.overflowMode = ellipsis ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
                return;
            }

            if (graphic is Text legacy)
                legacy.verticalOverflow = ellipsis ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCacheOnPlayModeEnter()
        {
            ResetCache();
        }

        private static void ResetCache()
        {
            _resolved = false;
            _fontMissing = false;
            _useTmp = false;
            _font = null;
            _legacyFont = null;
            _status = string.Empty;
            _warning = string.Empty;
        }

        private static void Resolve()
        {
            if (_resolved)
            {
                if (_fontMissing)
                    return;

                // A live cached font keeps the previous resolution. A destroyed
                // UnityEngine.Font (fake-null) must be rebuilt, because with
                // DisableDomainReload the static cache outlives the native
                // object. Without this check every map label stays on the
                // disabled placeholder while Status still names the old font.
                bool cachedFontAlive = _useTmp ? _font != null : _legacyFont != null;
                if (cachedFontAlive)
                    return;
            }

            ResetCache();
            _resolved = true;

            string[] installed = GetInstalledFontNames();
            TMP_Settings settings = TryLoadTmpSettings();

            if (settings != null)
            {
                string family = FindPreferredFamily(installed);
                TMP_FontAsset dynamicFont = family != null ? TryCreateTmpFont(family) : null;
                if (dynamicFont != null)
                {
                    dynamicFont.name = "JapanMapRuntime_" + family;
                    _useTmp = true;
                    _font = dynamicFont;
                    _fontMissing = false;
                    _status = "字体：" + family + "（系统动态 TMP）";
                    return;
                }

                UseLegacyFallback(installed,
                    family == null
                        ? "TMP Settings 存在，但没有找到批准列表中的 CJK 系统字体。"
                        : "TMP Settings 存在，但未能创建批准的 CJK 动态 TMP 字体。");
                return;
            }

            UseLegacyFallback(installed,
                "TMP Settings 资源缺失，TextMeshProUGUI 无法完成初始化。");
        }

        private static void UseLegacyFallback(string[] installed, string reason)
        {
            _useTmp = false;
            _font = null;
            _legacyFont = TryCreatePreferredLegacyFont(installed, out string family);
            _fontMissing = _legacyFont == null;
            if (family != null)
            {
                _status = "字体：" + family + "（uGUI 后备文本）";
                _warning = reason + " 当前使用批准列表字体 " + family +
                           " 的 uGUI 后备文本。";
            }
            else
            {
                _status = "字体缺口：未找到批准列表中的 CJK 系统字体；文本未渲染";
                _warning = reason + " " + BuildMissingCjkWarning();
            }

            Debug.LogWarning(_warning);
        }

        private static string BuildMissingCjkWarning()
        {
            return "批准列表：" + string.Join("、", PreferredFamilies) +
                   "。全国地图不会遍历其他系统字体，也不会使用 TMP 默认字体或内置字体替代；" +
                   "在安装上述任一中文字体前，地图文本可能不显示。";
        }

        private static TMP_Settings TryLoadTmpSettings()
        {
            try
            {
                return TMP_Settings.LoadDefaultSettings();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string[] GetInstalledFontNames()
        {
            try
            {
                return UnityEngine.Font.GetOSInstalledFontNames();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FindPreferredFamily(string[] installed)
        {
            if (installed == null)
                return null;

            for (int i = 0; i < PreferredFamilies.Length; i++)
            {
                string family = FindInstalledFamily(installed, PreferredFamilies[i]);
                if (family != null)
                    return family;
            }

            return null;
        }

        private static string FindInstalledFamily(string[] installed, string preferred)
        {
            for (int i = 0; i < installed.Length; i++)
            {
                string name = installed[i];
                if (string.IsNullOrEmpty(name))
                    continue;
                if (string.Equals(name, preferred, StringComparison.OrdinalIgnoreCase))
                    return name;
                if (name.StartsWith(preferred + " ", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(preferred + " UI", StringComparison.OrdinalIgnoreCase))
                    return name;
            }
            return null;
        }

        private static TMP_FontAsset TryCreateTmpFont(string family)
        {
            try
            {
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(family, "Regular", 90);
                if (asset != null)
                    return asset;
            }
            catch (Exception)
            {
                // Fall through to the dynamic Font overload below.
            }

            try
            {
                UnityEngine.Font osFont =
                    UnityEngine.Font.CreateDynamicFontFromOSFont(family, 32);
                if (osFont == null)
                    return null;
                return TMP_FontAsset.CreateFontAsset(osFont, 32, 4, GlyphRenderMode.SDFAA,
                    1024, 1024, AtlasPopulationMode.Dynamic, true);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Font TryCreatePreferredLegacyFont(string[] installed, out string family)
        {
            family = null;
            if (installed == null)
                return null;

            for (int i = 0; i < PreferredFamilies.Length; i++)
            {
                string preferred = FindInstalledFamily(installed, PreferredFamilies[i]);
                if (preferred == null)
                    continue;
                Font font = TryCreateOsFont(preferred);
                if (font != null)
                {
                    family = preferred;
                    return font;
                }
            }

            return null;
        }

        private static Font TryCreateOsFont(string family)
        {
            try
            {
                return UnityEngine.Font.CreateDynamicFontFromOSFont(family, 32);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static TextAnchor ToTextAnchor(TextAlignmentOptions alignment)
        {
            switch (alignment)
            {
                case TextAlignmentOptions.Top:
                    return TextAnchor.UpperCenter;
                case TextAlignmentOptions.TopRight:
                    return TextAnchor.UpperRight;
                case TextAlignmentOptions.Left:
                case TextAlignmentOptions.MidlineLeft:
                    return TextAnchor.MiddleLeft;
                case TextAlignmentOptions.Center:
                case TextAlignmentOptions.Midline:
                    return TextAnchor.MiddleCenter;
                case TextAlignmentOptions.Right:
                case TextAlignmentOptions.MidlineRight:
                    return TextAnchor.MiddleRight;
                case TextAlignmentOptions.Bottom:
                    return TextAnchor.LowerCenter;
                case TextAlignmentOptions.BottomLeft:
                    return TextAnchor.LowerLeft;
                case TextAlignmentOptions.BottomRight:
                    return TextAnchor.LowerRight;
                default:
                    return TextAnchor.UpperLeft;
            }
        }
    }
}
