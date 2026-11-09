# Town Overview UI Implementation Plan

> **For agentic workers:** Follow the project Orca workflow; OpenCode2 owns code and scene-generator implementation. The lead owns art generation, scene integration, and Unity Play Mode sign-off.

**Goal:** Replace the placeholder town menu with a parchment-and-indigo overview that gives Kiyosu, Atsuta, and Nagoya distinct visual identities while opening existing NPC and mission interactions.

**Architecture:** Keep `Canvas/M2WorldMapRoot/TownMenuRoot` as the single town UI root. Add a small presenter bound to `GameSession` and existing controllers, and a transactional Editor updater that adds the layout beneath the current root without replacing unrelated scene content. Select one of three original text-free illustrations by current location; treat art as atmosphere only.

**Tech Stack:** Unity 6000.6.6f1, C#, uGUI, existing editor Undo/validation patterns; no new packages.

**Spec:** `docs/superpowers/specs/2026-11-08-town-overview-ui-design.md`

**Reuse research:** `docs/research/town-overview-ui-reuse-2026-11-08.md`

## Global Constraints

- Treat `GDD_v4.0.md` as authoritative; old M4a/M4b documents are archival.
- Preserve dirty user files, Unity `.meta` GUIDs, and all existing scene objects; do not touch `Library`, `Temp`, packages, or unrelated scenes.
- Keep town labels and actions tied to current session data and existing controls; show honest empty states where no source data exists.
- OpenCode2 implements code. The lead generates/records original art, integrates the scene in the existing Editor, and signs off Play Mode.
- Stay on the existing `main` worktree. The current unrelated M02 dirty files are outside this plan and must not be staged or changed.

## Review Focus

- Location changes: town heading and displayed residents must reflect `GameSession.CurrentLocation`.
- Empty or unsupported rumor/mission data: show an explicit empty state; never fabricate content.
- Open/close/back transitions: town overview is restored and return-to-map does not advance time or change location.
- Existing roster, dialogue, and mission panels: retain their current entry points and close behavior.
- 1920×1080 and 1280×720: all text, cards, and controls remain visible and clickable without overlap.

## Files and Ownership

- Create: `Assets/Scripts/Presentation/TownOverviewPanel.cs` and matching `.meta` through Unity.
- Create: `Assets/Scripts/Editor/UpdateTownOverview.cs` and matching `.meta` through Unity.
- Modify: `Assets/Scripts/Presentation/WorldMapController.cs` to refresh the overview on town entry and expose existing NPC/mission entry points without auto-opening the mission overlay.
- Modify: `Assets/Scripts/Presentation/SengokuUiArt.cs` to expose the three town illustrations through the existing null-safe generated-art loading pattern.
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs` to validate the town overview hierarchy and bindings.
- Modify: `Assets/Tests/EditMode/WorldMapUiTests.cs` and/or a focused town overview test file for source binding, navigation, and empty states.
- Modify: `ASSETS.md` to document the generated town illustration.
- Create: `Assets/Resources/Generated/town_kiyosu.png`, `town_atsuta.png`, and `town_nagoya.png`, each with Unity importer `.meta`.
- Modify `Assets/Scenes/FoundationScene.unity` only through the lead-run updater in the already-open Editor, after preflight confirms the scene is clean; keep the existing root and objects.
- Do not edit currently dirty `FoundationScene.unity`, `AddM02DailyActionPanel.cs`, `CharacterDailyPageView.cs`, `DailyActionPanel.cs`, `ReferenceGalleryNavigator.cs`, or `M02DailyActionPanelTests.cs` as part of the implementation worker task.

## Tasks

### Task 1: Generate and record distinct town illustrations

- [ ] Generate three original, text-free Sengoku town illustrations in a consistent aged-paper, indigo, vermilion, antique-gold, ink-and-watercolor style, each with a distinct composition: Kiyosu castle-town skyline; Atsuta shrine approach and waterside; Nagoya roadside market and low-hill setting.
- [ ] Place them at the three `Assets/Resources/Generated/town_*.png` paths and record individual intended uses, generation source, and license/use notes in `ASSETS.md`.
- [ ] Confirm each image has a low-detail text-safe overlay area, remains distinct at both target resolutions, and contains no embedded labels or copied commercial art.

### Task 2: Implement presentation and scene updater through OpenCode2

- [ ] Inspect `WorldMapController`, `TownNpcDialoguePanel`, existing mission panels, `CreateM2WorldMap`, and updater/test conventions; do not add an external dependency.
- [ ] Add `TownOverviewPanel` to select the matching `SengokuUiArt` illustration for `GameSession.CurrentLocation`, render residents from `TownNpcAssignments.GetNpcs(location.Value)`, display an explicit empty state when no supported local rumor data exists, open the existing NPC roster and `SupplyShortageMissionPanel`, and return through `WorldMapController.ReturnToMap()`.
- [ ] Change town entry so the overview is shown first and the existing supply/mission panel opens only from its overview button; closing NPC/mission panels leaves the overview visible.
- [ ] Add an idempotent `UpdateTownOverview` editor action under the existing `TownMenuRoot`; use one Undo group, validate before save, roll back only its own changes on failure, and never reload the scene for rollback.
- [ ] Add focused EditMode coverage for all three location-to-art mappings, resident binding, empty states, existing panel navigation, and no time/location mutation when returning to the map.
- [ ] Extend `FoundationSceneValidator` to check the fixed root, required controls, artwork mappings/import settings, and non-overlapping responsive regions.
- [ ] Run the focused EditMode tests and report compilation, test totals, Console counts, and scene-generator evidence separately. Do not edit or save `FoundationScene.unity`.

### Task 3: Lead integration and runtime sign-off

- [ ] Verify the active Unity Editor `-projectPath` is this repository and confirm the scene has no dirty user changes before integration.
- [ ] Run the updater in the existing Editor, validate the resulting scene, and save only the generated town overview under the existing root.
- [ ] Check each city's distinct artwork at 1920×1080 and 1280×720, then run Play Mode through arrival in all three towns, NPC entry and return, mission entry and return, and return to map; record runtime evidence separately from EditMode results.
- [ ] After applicable verification gates pass, commit only verified town UI files on the current branch and push to `origin/main`; do not include unrelated M02 WIP.

## Execution update — 2026-11-09

- Three city illustrations, the town overview presenter, scene updater, bindings, and scene validator are present. The existing Editor saved the overview under `TownMenuRoot`; its validator reported `PASS; Errors: 0`.
- A prior full EditMode run passed `1347/1347` at `2026-11-09 10:52:02Z`. Play Mode visually confirmed all three city identities and return-to-map. At that time, the NPC/mission panel round trips and the 1280×720 layout had not yet been runtime-checked.
- Follow-up inspection found the mission panel sat behind the full-screen overview. The controller now raises the mission panel while keeping its return-to-overview and return-to-map buttons in front; `TownOverview_MissionCanOpenAndReturnWithoutHidingItsActions` covers the round trip.
- The existing Editor compiled the follow-up successfully. The full EditMode run completed at `2026-11-09 15:31:47Z`: `1348/1348` passed, `0` failed, `0` skipped, including the mission panel round-trip regression. During Play Mode in the Game View `Full HD (1920x1080)`, continuing the demo save and traveling from Kiyosu to Atsuta showed the Atsuta title and illustration. The mission panel opened over the overview; its return-to-overview action restored the town, the local-people panel opened and returned, and return-to-map restored the map at Atsuta. Console counters showed 0 logs, 0 warnings, and 0 errors. The exact `1280x720` check remains incomplete: the available Game View sizes had no such preset, and no custom size was added. The scene remains dirty alongside unrelated M02 WIP; no scene save, commit, or push was made.

---

**Plan self-review:** Every design section maps to a task: single-root layout, honest data display, existing interactions, generated art, two-resolution readability, transactional scene integration, and runtime return flow. No gameplay or rumor system is introduced.
