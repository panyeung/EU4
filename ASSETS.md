# Asset Sources and Licenses

Current direction: [《影之一族》GDD v4.0](GDD_v4.0.md). SC-004–SC-009 added original event JSON and Chinese text; SC-011 adds two original follow-up event variants and their Chinese text in the same JSON resources. There are no third-party or commercial-game assets and no Unity packages. The older M2 inventory below remains historical evidence.

| Asset | Source | License / use |
|---|---|---|
| uGUI `Image`, `Button`, and `Text` components | Unity 6000.6.6f1 built-in UI APIs | Used under the project's applicable Unity license; no source files copied into the project. |
| `LegacyRuntime.ttf` | Unity built-in resource, loaded with `Resources.GetBuiltinResource<Font>` | Unity-provided resource governed by Unity's applicable license; no standalone font file redistributed. |
| `Assets/Art/UI/ReferenceArt/*`, `Assets/Art/UI/paper_grain.png` | Crops/derivatives of the five UI design images provided by the project owner in the ChatGPT Library design gallery ([source gallery](https://chatgpt.com/library/share/libfile_373db197a9bc8191985c0292264f6f58?account_id=personal)) | Project owner supplied these images for this project. No third-party game art or external license is claimed. Keep crops only when they contain usable art without mockup text or data. |
| `Assets/Resources/ReferenceArt/map_illustration.png` | Map-area crop (`x=327, y=82, width=908, height=854`) from the owner's fifth-page UI reference image, 1672x941 | Owner-provided reference crop for the map page. No third-party game art or external license is claimed. |

No new Unity packages were added for M2, SC-004–SC-009, or SC-011.


## Original generated art (2026-11-08)

| Asset | Source | License / use |
|---|---|---|
| Assets/Resources/Generated/ui_icons_atlas.png | Original image generated with OpenAI image generation, using the owner's UI references only as visual-style guidance | Original generated asset for this project; no copied game art or third-party source asset. Sliced into 16 icons in Unity. |
| Assets/Resources/Generated/clan_portraits_atlas.png | Original image generated with OpenAI image generation, guided by the owner's visual references | Original generated portraits for this project; no copied game art or third-party source asset. Sliced into six portraits in Unity. |
| Assets/Resources/Generated/event_rainy_visitor.png | Original image generated with OpenAI image generation, guided by the owner's visual references | Original event illustration for this project; no copied game art or third-party source asset. |
| Assets/Resources/Generated/parchment_tile.png | Original image generated with OpenAI image generation, guided by the owner's visual references | Original low-contrast UI texture for this project; no copied game art or third-party source asset. |
| Assets/Resources/Generated/indigo_pattern_tile.png | Original image generated with OpenAI image generation, guided by the owner's visual references | Original indigo-and-gold UI texture for this project; no copied game art or third-party source asset. |
| Assets/Resources/Generated/world_map_illustration.png | Original image generated with OpenAI image generation from a text description based on the owner's reference style | Original fictional Sengoku region map art; no text, UI, copied geography, or third-party source asset. The map page now uses this sprite at runtime (`SengokuUiArt.WorldMapIllustration` loads it in `ReferenceGalleryNavigator.StyleMapPage`). Until the Unity Editor imports it with Sprite settings (the `.meta` is pending), or if the asset is ever missing, the map page falls back to the `ReferenceArt/map_illustration` texture crop. |
