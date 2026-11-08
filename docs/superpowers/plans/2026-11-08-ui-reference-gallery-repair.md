# UI Reference Gallery Repair Plan

> **For agentic workers:** Work in the existing `main` checkout. Do not create a branch or worktree. Preserve unrelated WIP and the current scene's user data.

**Goal:** Correct the four runtime defects found during the five-page reference gallery Play Mode check while keeping the approved 1280x720 layouts, live game data, and existing commands.

**Architecture:** Fix the shared page-to-view bindings and generated uGUI layout using the existing controllers and owner-provided UI art. Keep domain state and event outcomes unchanged; scene integration belongs to this single designated worker.

**Tech Stack:** Unity 6.6.6f1, C#, uGUI, existing Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`

## Global Constraints

- Stay in `C:\Users\panye\My project`, current `main`; no new branch/worktree/project.
- Preserve all existing dirty files, `.meta` files, GUIDs, M04A WIP, `graphify-out/`, and `nul`.
- Do not change domain rules, save format, event effects, dependencies, `ProjectSettings`, `Library`, or `Temp`.
- Use only user-provided reference artwork already in the project; do not add commercial-game assets.
- Only this designated integration worker may edit `FoundationScene.unity`; use its existing transactional generator and validation path.
- Do not commit or push until the focused/full tests and Play Mode gate pass; keep unrelated WIP out of any commit.

## Review Focus

- Page switches must activate the intended roots only and leave no invisible raycast blockers.
- Family cards must show a relevant portrait where a supplied portrait can be matched and remain readable for larger/unknown rosters.
- The map illustration must preserve its source aspect ratio, stay inside the central map region, and leave route/travel controls clickable.
- Household rows, month event, and pending decisions must use live session data; absent data stays absent.
- Event layout must render whichever event is actually pending; do not hard-code the example title or outcomes.

---

### Task 1: Repair page binding and household composition

**Files:** `Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`, `Assets/Scripts/Editor/AddM02DailyActionPanel.cs`, applicable presentation tests, `Assets/Scenes/FoundationScene.unity` only if generator output requires it.

- [ ] Make the Household page present the reference's family roster/appointments, current household industry, month event, and pending-decision regions rather than showing the Industry panel alone.
- [ ] Keep each existing button bound to its existing command; omit sections with no live data.
- [ ] Extend `ReferenceGalleryNavigatorTests.ShowPage_ActivatesOnlyTheSelectedPageAndItsPanels` or focused tests to assert the household roots and raycast ownership.
- [ ] Keep the 1280x720 regions inside the safe canvas and avoid overlaps.

### Task 2: Repair family portrait cards

**Files:** `Assets/Scripts/Presentation/FamilyRosterPanel.cs`, `Assets/Scripts/Presentation/FamilyTreeGraphBuilder.cs` or existing portrait data source only if necessary, family-roster tests, existing user-provided art only.

- [ ] Render a portrait in each family card when existing character identity data maps to an owner-provided portrait; do not assign an unrelated portrait or invent identity data.
- [ ] Keep generation spacing, relationship edges, card text, scrolling, and arbitrary family sizes intact.
- [ ] Extend `FamilyRosterPanelTests.Refresh_RendersSc013KinshipEdgesAndSeparateRetainerSection` and/or the large-roster test to assert portrait placement, fallback behavior, and no overlap.

### Task 3: Repair map scale and travel layout

**Files:** `Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`, `Assets/Scripts/Editor/CreateM2WorldMap.cs`, `UpdateM2WorldMapLayout.cs`, `WorldMapLayout.cs`, `CreateM2WorldMapGeneratorTests.cs`, `WorldMapUiTests.cs`, and the scene only through the designated generator.

- [ ] Place the map illustration in the central map rectangle with preserved aspect ratio; it must not cover title, date, route controls, destination buttons, or travel confirmation.
- [ ] Align the left travel plan, center map, and right information region to the approved reference proportions.
- [ ] Keep existing destination selection, preview, confirm, cancel, and route/date calculations unchanged.
- [ ] Add or extend layout assertions proving the image bounds and interactive control bounds do not overlap.

### Task 4: Repair event-choice composition

**Files:** `Assets/Scripts/Presentation/EventChoicePanel.cs`, `Assets/Scripts/Editor/AddSC005EventChoicePanel.cs`, event-panel tests and scene only through the designated generator.

- [ ] Reflow the event page into the reference's left navigation, central story/choice card, and right pending-event area; use existing page assets and styles.
- [ ] Continue displaying the current pending event from `GameSession`; never hard-code “雨夜来客” or image-sample consequences.
- [ ] Keep option enabled states, `TryChooseEventOption`, result feedback, and pending-queue behavior unchanged.
- [ ] Extend `EventChoicePanelTests` and `AddSC005EventChoicePanelTests` for live content, layout bounds, and option invocation.

### Task 5: Verify and hand off

- [ ] Run the focused tests for the four areas, existing scene validator, and full EditMode suite using the already-open Unity Editor (PID 91392 only if it still targets this project).
- [ ] Provide file-scoped review evidence and the Editor path/test/Console evidence separately; do not claim exact visual acceptance until the designated lead runs Play Mode.
- [ ] Leave Play Mode stopped and the scene saved only through the validated scene integration flow.
- [ ] Do not commit/push in the implementation worker; integration occurs only after review and Play Mode gate.
