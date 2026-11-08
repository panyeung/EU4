using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Runtime access to the original generated UI art delivered under
    /// <c>Assets/Resources/Generated</c> (see docs/ART_ASSET_WORKFLOW.md): a 4x4 icon
    /// atlas, a 2x3 clan portrait atlas, the rainy-visitor event illustration, and the
    /// parchment/indigo surface tiles. Every load is cached and null-safe: when an
    /// asset is missing, callers keep their flat theme colours and pages still
    /// render. The artwork is decorative only — all readable text and game data stay
    /// in separate <see cref="Text"/> components, never baked into an image.
    /// </summary>
    public static class SengokuUiArt
    {
        public const string IconsAtlasPath = "Generated/ui_icons_atlas";
        public const string PortraitsAtlasPath = "Generated/clan_portraits_atlas";
        public const string ParchmentPath = "Generated/parchment_tile";
        public const string IndigoPatternPath = "Generated/indigo_pattern_tile";
        public const string EventIllustrationPath = "Generated/event_rainy_visitor";
        public const string WorldMapIllustrationPath = "Generated/world_map_illustration";

        // Sprite indices inside the generated 4x4 icon atlas (row-major, top-left
        // first). The names are the importer's stable ui_icons_atlas_N entries.
        public const int IconCrest = 0;
        public const int IconLord = 1;
        public const int IconFamilyTree = 2;
        public const int IconFarm = 4;
        public const int IconRest = 6;
        public const int IconTraining = 7;
        public const int IconWork = 8;
        public const int IconHorseTravel = 9;
        public const int IconCompass = 10;
        public const int IconSealedLetter = 11;

        // The portrait atlas is sliced into six deliberate archetypes named
        // portrait_<key> in the importer metadata; the roster keys (master, elder,
        // youth, main) address four of them, lady/girl complete the clan set.
        public const string PortraitSpritePrefix = "portrait_";

        private static readonly Dictionary<string, Sprite[]> AtlasCache =
            new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        /// <summary>All sprites of one generated atlas; empty when the asset is absent.</summary>
        public static Sprite[] LoadAtlas(string resourcePath)
        {
            if (resourcePath == null)
                return Array.Empty<Sprite>();
            if (!AtlasCache.TryGetValue(resourcePath, out Sprite[] sprites))
            {
                sprites = Resources.LoadAll<Sprite>(resourcePath) ?? Array.Empty<Sprite>();
                AtlasCache[resourcePath] = sprites;
            }
            return sprites;
        }

        /// <summary>One sprite by atlas path and exact name; null when absent.</summary>
        public static Sprite FindSprite(string resourcePath, string name)
        {
            string cacheKey = resourcePath + "/" + name;
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cached))
                return cached;
            Sprite found = null;
            foreach (Sprite sprite in LoadAtlas(resourcePath))
                if (sprite != null && sprite.name == name)
                {
                    found = sprite;
                    break;
                }
            SpriteCache[cacheKey] = found;
            return found;
        }

        /// <summary>The generated icon at one atlas index; null when absent.</summary>
        public static Sprite Icon(int index) =>
            FindSprite(IconsAtlasPath, "ui_icons_atlas_" + index);

        /// <summary>The generated clan portrait for one roster key; null when absent.</summary>
        public static Sprite Portrait(string key) =>
            key == null ? null : FindSprite(PortraitsAtlasPath, PortraitSpritePrefix + key);

        public static Sprite Parchment => FindSprite(ParchmentPath, "parchment_tile_0");

        public static Sprite IndigoPattern =>
            FindSprite(IndigoPatternPath, "indigo_pattern_tile_0");

        public static Sprite EventIllustration =>
            FindSprite(EventIllustrationPath, "event_rainy_visitor_0");

        /// <summary>
        /// The generated full-region map for the reference gallery's map page;
        /// null when the asset is absent, so callers keep their reference-art
        /// fallback. Accepts both importer shapes: the sliced
        /// <c>world_map_illustration_0</c> name (Multiple mode) and the
        /// single-sprite <c>world_map_illustration</c> name (Single mode).
        /// </summary>
        public static Sprite WorldMapIllustration =>
            FindSprite(WorldMapIllustrationPath, "world_map_illustration_0")
            ?? FindSprite(WorldMapIllustrationPath, "world_map_illustration");

        /// <summary>
        /// Gives a light panel its parchment grain: the tile is drawn as a repeating
        /// surface under the panel's existing tint so colours and validation stay the
        /// same. No-op when the texture is absent.
        /// </summary>
        public static void ApplyParchmentSurface(Image surface)
        {
            if (surface == null)
                return;
            Sprite tile = Parchment;
            if (tile == null)
                return;
            surface.sprite = tile;
            surface.type = Image.Type.Tiled;
        }

        /// <summary>
        /// Adds (or refreshes) a stretched, non-interactive textile overlay on a dark
        /// rail or bar, so the indigo pattern reads without changing the surface's
        /// own colour bindings. Returns null when the texture is absent.
        /// </summary>
        public static Image ApplyIndigoPatternOverlay(Transform surface, string overlayName)
        {
            if (surface == null)
                return null;
            Sprite tile = IndigoPattern;
            if (tile == null)
                return null;

            Transform existing = surface.Find(overlayName);
            GameObject overlayObject;
            if (existing != null)
            {
                overlayObject = existing.gameObject;
            }
            else
            {
                overlayObject = new GameObject(overlayName, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                overlayObject.transform.SetParent(surface, false);
            }
            RectTransform rect = (RectTransform)overlayObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = overlayObject.GetComponent<Image>();
            image.sprite = tile;
            image.type = Image.Type.Tiled;
            image.color = Color.white;
            image.raycastTarget = false;
            overlayObject.transform.SetAsFirstSibling();
            return image;
        }

        /// <summary>Test seam: forget cached atlas lookups (used if assets change).</summary>
        public static void ClearCache()
        {
            AtlasCache.Clear();
            SpriteCache.Clear();
        }
    }
}
