# 开始页面实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Implement with OpenCode2 on the current workspace; if OpenCode2 cannot proceed, the project lead takes over. Track steps with checkboxes.

**Goal:** Replace the crowded startup overlay with an illustrated two-action main menu and move the fixed SC-013 and SC-010/SC-020–SC-024 entries to a scenario-selection page.

**Architecture:** Keep one full-screen `M6TitleRoot` overlay and existing `TitleScreenController`; add main-menu and scenario-selection subroots, preserving the existing scenario-start methods and Continue save path. Generate a clean background plate as a separate Sprite and build every label, button, and panel as live uGUI controls. Upgrade only a validated legacy title hierarchy through the existing transactional scene generator.

**Tech Stack:** Unity 6.6.6f1, C#, uGUI, existing `SengokuUiTheme`, Editor Test Runner.

**Spec:** `docs/superpowers/specs/2026-11-08-start-screen-design.md`

**Current progress (2026-11-08):** OpenCode2 implementation and original art are committed and pushed in `220b44f`. Unity 6000.6.6f1 compiled the latest source, the full EditMode suite passed 1343/1343, and the background PNG imported. The user authorized saving the current FoundationScene; M6 integration and the full scene validator passed. Play Mode launched SC-013 and SC-010 standard successfully. The saved scene stays local because it contains existing M02 WIP that cannot be safely included in the M6 push.

## Global Constraints

- Stay in `C:\Users\panye\My project`; do not create a Unity project, branch, or worktree.
- Dispatch implementation to OpenCode2 through supervised Orca orchestration; after implementation, dispatch a separate read-only OpenCode review. The project lead owns integration and Unity gates in the existing Editor.
- Reuse Unity 6.6.6f1 and uGUI; do not add a package.
- Preserve all `.meta` files/GUIDs and do not touch `Library` or `Temp`.
- Keep Continue on the existing single-slot load path; do not overwrite user saves during Play Mode verification.
- Scene generation uses one Undo group, validates before saving, rolls back on exceptions, and does not reload scenes for rollback.
- `Assets/Scenes/FoundationScene.unity` already has uncommitted WIP. Inspect and preserve that diff before integration; never discard it or bypass a dirty-scene preflight silently.
- After each applicable verification gate passes, commit and push only this task's verified files to the current branch (`main`); do not merge or tag before explicit milestone acceptance.

## Review Focus

- Missing, corrupt, or unsupported-version save: Continue remains disabled or reports the existing error without mutating the slot or session. Test in `TitleScreenControllerTests`.
- Clicking “开始新游戏” only opens scenario selection: the current date, family, and title overlay remain unchanged. Test the real button listener in `TitleScreenControllerTests`.
- Returning from scenario selection or calling `ShowTitle()` restores the main menu and hides the selector. Test both transitions in `TitleScreenControllerTests`.
- Every pre-existing scenario remains reachable once and starts its matching initialization path. Validate bindings in `AddM6TitleScreenTests`; retain direct scenario-start tests in `TitleScreenControllerTests`.
- At 1280×720 and 1920×1080 the main menu, selector, status, and button hit areas remain inside the Canvas and readable. Check generator geometry in EditMode and inspect both aspect ratios in Play Mode.

---

### Task 1: Generate and register the clean title background

**Files:**
- Create: `Assets/Art/UI/start_screen_background.png`
- Create by Unity import: `Assets/Art/UI/start_screen_background.png.meta`
- Modify: `ASSETS.md`

**Interfaces:**
- Produces a 16:9 art-only background Sprite for `AddM6TitleScreen`; reserve the left third for the indigo menu overlay, show the dusk manor/castle-town on the right, and include no text, buttons, UI, or baked-in logo.

- [x] Generate the scene plate using the approved concept as style guidance; do not use the full concept mockup as runtime art.
- [x] Import it as one UI Sprite in the existing Unity Editor and verify `AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/start_screen_background.png")` resolves.
- [x] Add its source and use to `ASSETS.md` as original AI-generated art; add no package decision to `DECISIONS.md`.

### Task 2: Separate main-menu navigation from scenario starts

**Files:**
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Test: `Assets/Tests/EditMode/TitleScreenControllerTests.cs`

**Interfaces:**
- Add `BindScenarioSelection(GameObject mainMenuRoot, GameObject scenarioSelectionRoot, Button fixedScenarioButton, Button backButton)`.
- Add `ShowScenarioSelection()` and `ShowMainMenu()`.
- Keep `StartNewGame()`, `TryContinue()`, and all existing `Start*Demo()` public methods and their initialization behavior unchanged.

- [x] Add `ScenarioSelectionButton_OpensSelectorWithoutStartingOrMutatingSession`; invoke the bound main-menu button and assert selector active, main menu visible/covered as designed, title still visible, and date/family unchanged.
- [x] Add `ScenarioSelectionBackButton_ReturnsToMainMenu` and `ShowTitle_ResetsToMainMenu`; assert the title overlay stays open and the selector is inactive.
- [x] Route the main-menu “开始新游戏” listener to `ShowScenarioSelection()`; bind a selector “固定三代剧本” button to existing `StartNewGame()`.
- [x] Bind/unbind the new fixed-scenario and back-button listeners idempotently with the existing `BindButtons` lifecycle.
- [x] Keep Continue status and `TryContinue()` semantics unchanged; run the new focused controller tests plus existing missing/corrupt/unsupported-save and scenario initialization tests.

### Task 3: Generate and safely upgrade the title UI hierarchy

**Files:**
- Modify: `Assets/Scripts/Editor/AddM6TitleScreen.cs`
- Test: `Assets/Tests/EditMode/AddM6TitleScreenTests.cs`
- Integrate: `Assets/Scenes/FoundationScene.unity` through `Tools > Sengoku Chronicle > Add M6 Title Screen`

**Interfaces:**
- Keep `AddM6TitleScreen.Update(Scene)` and `Validate(Scene, out string)` as the single generator/validator entry points.
- New stable names: `M6MainMenuRoot`, `M6ScenarioSelectRoot`, `SC013FixedScenarioButton`, and `M6ScenarioBackButton`; retain existing scenario button names and controller bindings unless validation proves a safe rename is needed.

- [x] Update `Updater_AddsAndBindsTitleAndSummary` to assert the full-screen background Sprite, menu roots, Continue/New Game controls, the new fixed SC-013 button, Back button, and all seven existing buttons (SC-010 standard/crisis plus SC-020–SC-024) are correctly parented and bound.
- [x] Update `Validator_RejectsMissing...` coverage for a missing page root, missing fixed entry, duplicate entry, wrong background Sprite, and invalid required button binding.
- [x] Add a legacy-layout migration test: a complete, valid current title hierarchy upgrades in one Undo group; a partial/unknown hierarchy still returns `ExistingPanel` without deleting or saving anything.
- [x] Generate the main menu with the approved title, proposed subtitle, two actions, `SengokuUiTheme` colors, existing seal, and separate background Sprite. Generate a scenario selector with one fixed SC-013 entry and the seven existing SC-010/SC-020–SC-024 entries, grouped by role and using their existing start methods.
- [x] Extend `Validate` to check unique required nodes/bindings, title-root sibling order, inactive boot state, Sprite assignment, no stale demo buttons on the main-menu subroot, and safe Canvas bounds.
- [x] Preserve the title root, `TitleScreenController`, `DemoSummaryPanel`, and map summary object during a validated legacy upgrade; rebuild only owned title UI children with `Undo.DestroyObjectImmediate`. Roll back the whole group on validation failure and save only after validation passes.
- [x] Run `AddM6TitleScreenTests` in EditMode, including idempotence and rollback tests. A byte-identical pre-integration scene backup was verified; after explicit user authorization, run the generator in the current project Editor and confirm `FoundationScene` validation passes.

### Task 4: Full verification and handoff

**Files:**
- Verify: all files changed by Tasks 1–3
- Update if necessary: `docs/superpowers/specs/2026-11-08-start-screen-design.md` and `ASSETS.md`

- [x] Confirm the running Unity Editor's `-projectPath` is the current project; never run batchmode against it.
- [x] Confirm Unity compilation succeeds, then run the full EditMode suite in the Editor Test Runner with zero failures.
- [x] Run `Tools > Sengoku Chronicle > Validate Foundation Scene`; record result and Console counts separately. Result: PASS, 0 validator errors. At initial Play Mode entry, visible Console counters were 0/0/0; post-launch counters were not rechecked.
- [x] In Play Mode, verify title startup, New Game to selector, Back to main menu, SC-013 launch, SC-010 standard launch, and access to all SC-020–SC-024 entries. No save was overwritten. Continue was not clicked because a save was detected; no-save Continue remains an EditMode-only check.
- [ ] Inspect 1280×720 and 1920×1080 for cropping, contrast, button hit areas, and safe margins. Full HD (1920×1080) only was inspected; Play Mode was exited and inspected screenshots deleted.
- [x] Request a separate read-only OpenCode review after implementation; address findings and rerun affected tests.
- [x] Update the design/report with actual compile, EditMode, validator, Console, and Play Mode evidence. The implementation checkpoint `220b44f` was pushed to `origin/main`. The scene integration remains unstaged to avoid mixing existing M02 WIP; do not tag before explicit milestone acceptance.

## Self-review

- Spec coverage: the two main actions, conditional Continue status, scenario selector, original background art, palette/material direction, 16:9 safe area, separate interactive uGUI controls, no settings/quit additions, and retained save/scenario behavior are each covered above.
- Scene safety: the generator upgrades only a recognized legacy layout, preserves controller/summary objects, validates before save, and rolls back without scene reload.
- No new Unity package, data model, save format, game rule, or full-screen baked UI image is introduced.
