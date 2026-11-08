# Art Asset and Implementation Workflow

## Visual target

Use the five UI design images supplied by the project owner as references for composition and visual direction: warm aged parchment panels, dark indigo framing, restrained vermilion and antique-gold accents, and ink-and-watercolor Japanese Sengoku illustrations. Recreate the direction with original game assets; do not copy or trace commercial-game art, text, or complete mockup screens.

## Ownership and handoff

1. Mochi generates original art assets that the approved feature actually needs, using the owner's references to match style. Keep generated art free of baked-in UI labels and data unless the feature explicitly requires them.
2. Mochi records each asset's project path, intended use, generation source, and license/use note in `ASSETS.md`. Preserve the original generated file outside the repository; copy only the selected deliverable into `Assets/`.
3. OpenCode2 implements the feature and integrates the approved assets on the current task worktree. It owns code, import settings, and presentation wiring; Mochi owns art generation and review coordination.
4. If OpenCode2 is unavailable, cannot complete the implementation, or has exhausted its available balance, Mochi may take over the implementation and must report the handoff. Preserve existing user changes and keep the work on the current authorized worktree.
5. Verify imports and presentation in the existing Unity Editor after checking its `-projectPath`. Keep compilation, automated test results, Console counts, Play Mode evidence, and milestone acceptance as separate claims. Delete every Computer Use screenshot immediately after inspection.

## Generated assets in this batch

- `Assets/Resources/Generated/ui_icons_atlas.png`: original 4x4 icon atlas for family, industry, action, travel, event, and chronicle UI.
- `Assets/Resources/Generated/clan_portraits_atlas.png`: original 2x3 family portrait atlas.
- `Assets/Resources/Generated/event_rainy_visitor.png`: original rainy-night messenger event illustration.
- `Assets/Resources/Generated/parchment_tile.png`: original low-contrast parchment texture tile for light panels.
- `Assets/Resources/Generated/indigo_pattern_tile.png`: original indigo-and-gold textile pattern tile for dark bars and frames.
- `Assets/Resources/Generated/world_map_illustration.png`: original, text-free regional map artwork wired into the map panel; `SengokuUiArt.WorldMapIllustration` loads it and `ReferenceGalleryNavigator.StyleMapPage` displays it. The runtime uses this generated sprite once the Unity Editor imports it with Sprite texture settings (its importer `.meta` is still pending); until then, or whenever the asset is absent, the map page keeps the null-safe `ReferenceArt/map_illustration` texture-crop fallback.

The icon and portrait atlases are sliced in Unity and loaded from `Resources/Generated`; the reference gallery and M02 action page consume these assets. No new Unity package or external asset dependency is required.
