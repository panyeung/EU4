# Start-screen redesign — implementation report

Date: 2026-11-08
Scope: approved plan `docs/superpowers/plans/2026-11-08-start-screen.md`, Tasks 1–3 (code and art), plus the corrections requested in `2026-11-08-start-screen-review.md` and `2026-11-08-start-screen-review-2.md`.
Status: Unity compile and full EditMode suite pass (1343/1343); art import is confirmed. Scene migration/validation and Play Mode remain pending because FoundationScene has existing dirty and unsaved work.

## Changed files

- `Assets/Scripts/Presentation/TitleScreenController.cs` — two-page navigation; New Game opens the selector, with a warned direct SC-013 fallback when the selector root is still null; `IsScenarioSelectionVisible` is gated on the title overlay; `BindScenarioSelection(...)` detaches prior navigation listeners before rebinding; all existing continue/save/scenario methods unchanged.
- `Assets/Tests/EditMode/TitleScreenControllerTests.cs` — selector-open-without-session-mutation, Back, `ShowTitle()` reset, un-migrated fallback, null-rebind detachment, visibility-after-start, one click-through per fixed/SC-010/SC-020–SC-024 entry with slot and selector state assertions, and retained save/scenario tests.
- `Assets/Scripts/Editor/AddM6TitleScreen.cs` — two-page generator; Continue above New Game; indigo-pattern panel surfaces, paper-grain buttons and gold hairlines; mainline/topics selector group labels; fixed SC-013 plus seven entries; background Sprite and seal; 1280×720 bounds validation; title ordering **immediately below `EventChoiceRoot` when it exists, otherwise last**; complete-legacy-only migration with Undo rollback and validate-before-save. The migration records the root Transform/Image before mutation, records the parent Canvas before sibling reordering, and explicitly restores the original sibling index if Undo does not.
- `Assets/Tests/EditMode/AddM6TitleScreenTests.cs` — deterministic preview-Canvas geometry, hierarchy/bindings, background and themed sprites, idempotence, fresh and legacy-migration rollback (non-default RectTransform/Image, Canvas sibling, children and controller references restored), complete-legacy migration with both-root ordering, refusal of incomplete/already-page-root layouts without deleting children, negative main/selector bounds tests, negative summary-overlap test, ordering tests, and SC-013 rerun compatibility.
- `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs` — recognizes the migrated selector page; reruns succeed without recreating flat children.
- `Assets/Scripts/Editor/AddSC020CharacterEducation.cs`, `AddSC021FamilyResearch.cs`, `AddSC022FamilyFormation.cs`, `AddSC024FamilyDoctrine.cs` — title-entry validation resolves `M6ScenarioSelectRoot`; a missing migrated entry is refused with a clear error rather than silently repaired, so reruns validate in place and never add a flat child. The legacy direct-root path remains only when the selector is absent.
- `Assets/Scripts/Editor/AddSC023Seclusion.cs` — validates the title entry against the selector page and creates under it only when the selector exists; legacy path unchanged.
- `Assets/Tests/EditMode/AddSC020CharacterEducationTests.cs`, `AddSC021FamilyResearchTests.cs`, `AddSC022FamilyFormationTests.cs`, `AddSC024FamilyDoctrineTests.cs` — migrated-selector acceptance and end-to-end rerun coverage.
- `Assets/Tests/EditMode/AddSC023SeclusionTests.cs` and `.meta` — new SC-023 fixture covering legacy generation and a migrated-selector end-to-end rerun.
- `Assets/Scripts/Editor/FoundationSceneValidator.cs` — title-controller reference report now includes `mainMenuRoot`, `scenarioSelectionRoot`, `fixedScenarioButton`, `scenarioBackButton` and `familyDoctrineDemoButton`.
- `Assets/Art/UI/start_screen_background.png` and `.png.meta` — supplied original generated art placed with a hand-authored Sprite importer `.meta` and a stable GUID. The active Unity Editor imported the PNG; the GUID remains stable.
- `ASSETS.md` — source/licence row for the new art.
- `docs/superpowers/specs/2026-11-08-start-screen-design.md` — status line updated only.

## Review corrections

- High 1: SC-021, SC-022 and SC-023 now validate title entries on `M6ScenarioSelectRoot` after migration; each has a migrated-layout end-to-end rerun test, and the SC-023 updater gained its own fixture.
- High 2: un-migrated scenes keep a safe direct SC-013 New Game path with a migration warning.
- Medium 2: the ordering policy is now "title immediately below `EventChoiceRoot` when present, otherwise last Canvas child". This preserves the M02 generator's modal layering; migration and validation cover both root configurations.
- Medium/low from review 1: complete 11-child legacy recognition, refusal without deletion, `IsScenarioSelectionVisible` gating, per-entry click-through, negative bounds/order tests, report accuracy.
- Review 2 low 3–7: deterministic Canvas geometry in the M6 fixture; summary-overlap negative test restored; `BindScenarioSelection(null...)` detaches prior listeners; the page-root refusal test now exercises that guard; `FoundationSceneValidator` reference list refreshed.
- Review 3: the migration records the root Transform and Image **before** `ConfigureRoot`, records the parent Canvas before sibling reordering and explicitly restores the original sibling index if Undo does not, and a legacy-migration fault-injection test starts with `EventChoiceRoot` above `M6TitleRoot` plus non-default RectTransform values and proves Undo restores the old sprite/colour, derived and authored RectTransform values, Canvas sibling order, children and controller references; `IsMainMenuVisible` is gated on the title overlay and the click-through test asserts both pages are hidden after a start; the wrong-background test uses a real non-null sub-Sprite; the partial-panel test now uses an inactive root; and the bind-lifecycle test invokes `OnDisable`/`OnEnable` explicitly in EditMode.
- Visual: main/selector panels use the existing indigo pattern sprite, buttons use paper grain and gold hairlines, Continue precedes New Game, and the selector has group labels. Remaining visual gap for Play Mode/user sign-off: the generated texture assets are existing UI tiles rather than a bespoke 和纸 illustration, and final contrast/readability at both aspect ratios is unverified.

## Verification actually performed

- Unity Editor 6000.6.6f1 (PID 126040) is open on `C:\Users\panye\My project`; its script assemblies recompiled after the latest test fix with no C# compiler errors in `Logs/Editor.log`.
- Full EditMode suite in that Editor: **1343 passed, 0 failed, 0 skipped**, confirmed by Test Runner and `TestResults.xml` under `%LOCALAPPDATA%\..\LocalLow\DefaultCompany\My project`.
- Unity imported `Assets/Art/UI/start_screen_background.png`.
- Static Roslyn compile of all changed production and test C# files (including the new SC-023 fixture) against the Unity 6000.6.6f1 reference assemblies and current project DLLs: 0 errors. The only warnings were expected CS0436 notices caused by compiling source alongside the previous compiled project DLLs.
- `git diff --check`: clean for all changed task files.
- Read-only inspection of the on-disk `FoundationScene.unity` confirmed the 11 legacy title children, the seven controller bindings, and the current `M6TitleRoot`/`EventChoiceRoot` Canvas order. The open scene is marked unsaved and the scene file has pre-existing WIP; neither was changed for this task.
- `graphify update .` run after the edits.

## Still pending

- Generator menu run, scene save, `Validate Foundation Scene`, and Play Mode verification have not been performed.
- The M6 generator refuses a dirty scene. Preserve the existing FoundationScene WIP and resolve its ownership before migration; do not save or discard it as part of this task.
- Unity Console counters were not separately captured during this verification pass.

## Integration steps required

1. Preserve and reconcile the existing FoundationScene WIP so the loaded scene is clean without losing its changes.
2. Run `Tools > Sengoku Chronicle/Add M6 Title Screen` on the clean `FoundationScene`; it migrates the complete legacy hierarchy and places the title immediately below `EventChoiceRoot`.
3. Run `Tools > Sengoku Chronicle/Validate Foundation Scene`, then Play Mode checks for Continue state, New Game → selector without mutation, Back, SC-013, SC-010, SC-020–SC-024 at 1280×720 and 1920×1080.
