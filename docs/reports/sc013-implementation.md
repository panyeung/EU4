# SC-013 M2 Fixed Three-Generation Implementation Report

Date: 2026-11-03
Branch: `feature/sc013-fixed-three-generation`
Worktree: `C:\Users\panye\My project`
Spec: `docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md`
Plan: `docs/superpowers/plans/2026-11-03-sc-013-m2-fixed-three-generation.md`
Play script: `SC013_DEMO.md`

## Outcome

The approved fixed three-generation M2 scenario is implemented in code, content, tests, and documentation. The default new game is now the SC-013 fixed family; the SC-010 standard sample and the SC-010 crisis demo remain independent title entries. Two lawful handoffs, a fixed six-person roster, an M2-only inheritance cost event, the accepted SC-011 follow-up, a trust-gated third-generation finale, and two derived endings are wired through the existing succession, event, save, and chronicle systems.

`FoundationScene.unity` is intentionally unchanged in this worker run. The two transactional editor menus still need to be executed in the user's open Editor, followed by `FoundationSceneValidator` and the user's Play Test. No Unity compilation, EditMode, Console, validator, Play Mode, or user-acceptance claim is made below.

## Implementation areas

- Fixed scenario identity: `M2FixedScenario` owns `family.akechi.sc013`, six stable `person.sc013.*` IDs, the approved ages (70/66/46/22/44/40), the direct line 晴信 → 正綱 → 信正, fixed-heir queries, the M2 retirement gate, read-only roster rows, and the derived ending view.
- Session entry: `GameSession.InitNewGame()` starts M2; `InitStandardDemo()` starts the SC-010 sample; `StartCrisisDemoScenario()` initializes the standard state and then enters the existing crisis transition. `SuccessionCandidates` filters to the approved next direct heir while the fixed script is active.
- Scenario isolation: `EventDefinitionData.scenario` plus `EventRuleContext.ScenarioId` keep `event.sc013.*` rows out of SC-010 standard/crisis play and reject cross-scenario pending events during save restore. SC-011 rows stay scenario-neutral.
- Content: two SC-013 events (`inheritance-pledge`, `finale`) and their option/summary text were appended. Content version stays `2`.
- Presentation: `FamilyRosterPanel` renders six read-only rows and the current fixed heir; `M2EndingPanel` renders the derived third-generation ending and does not freeze time; `SuccessionPanel` exposes the M2 gate reason and fixed heir; `TitleScreenController` exposes M2, SC-010 standard, and SC-010 crisis entries.
- Scene integration: `AddSC013FamilyRosterPanel` and `AddSC013TitleScenarioButtons` follow the existing transactional updater pattern (one Undo group, validate before save, rollback on exception without reloading). `FoundationSceneValidator` checks both additions.
- Documentation: SC-013 spec/plan status, `SC013_DEMO.md`, `PROJECT_STATE.md`, `ARCHITECTURE.md`, `DECISIONS.md`, and this report were updated. The prior-art report records a `Build` decision and no external code/assets.

## Changed files (SC-013 owned)

New source and tests:

- `Assets/Scripts/Application/M2FixedScenario.cs` (+ `.meta`) — fixed roster, direct line, heir queries, M2 gate, roster/ending views.
- `Assets/Scripts/Presentation/FamilyRosterPanel.cs` (+ `.meta`) — read-only six-row family panel.
- `Assets/Scripts/Presentation/M2EndingPanel.cs` (+ `.meta`) — derived third-generation ending panel.
- `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs` (+ `.meta`) — transactional roster/ending scene updater and validator.
- `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs` (+ `.meta`) — transactional SC-010 title-entry updater and validator.
- `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/FamilyRosterPanelTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/M2EndingPanelTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/M2EventFlowTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/M2FixedScenarioTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/M2ScenarioSaveTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/M2StoryGateTests.cs` (+ `.meta`)

Modified source and content:

- `Assets/Scripts/Application/GameSession.cs` — M2 default/standard/crisis entry points, authoritative retirement gate, fixed-heir filter/display, derived ending, scenario-aware save restore.
- `Assets/Scripts/Events/EventCatalogData.cs` — optional `scenario` field, scenario IDs, SC-013 stable IDs.
- `Assets/Scripts/Events/EventCatalogValidator.cs` — allow only the known `sc013` scenario tag.
- `Assets/Scripts/Events/EventEngine.cs` — scenario eligibility and scenario-aware save restore.
- `Assets/Scripts/Events/EventRuleContext.cs` — `ScenarioId`.
- `Assets/Scripts/Presentation/SuccessionPanel.cs` — gate-driven retire state, fixed heir and block reason.
- `Assets/Scripts/Presentation/TitleScreenController.cs` — independent M2/standard/crisis actions and bindings.
- `Assets/Scripts/Editor/AddM6TitleScreen.cs` — accepts the optional two SC-013 title entries.
- `Assets/Scripts/Editor/FoundationSceneValidator.cs` — requires and reports the roster/ending panels and title entries.
- `Assets/Resources/Events/event-catalog-v1.json` — appended SC-013 chain and two events only.
- `Assets/Resources/Localization/event-text-zh-v1.json` — appended ten SC-013 text rows only.

Modified tests for scenario isolation/regression:

- `Assets/Tests/EditMode/WorldClockTests.cs` — legacy time/event test now starts from `InitStandardDemo()` (B1).
- `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs` — added a roster-content capacity/separation check.
- `Assets/Tests/EditMode/M2ScenarioSaveTests.cs` — added cross-scenario save rejection coverage.
- `Assets/Tests/EditMode/M2StoryGateTests.cs` — added the fixed-heir guard test with an active succession decision.
- `Assets/Tests/EditMode/AddSC010SuccessionPanelTests.cs`
- `Assets/Tests/EditMode/EventChoicePanelTests.cs`
- `Assets/Tests/EditMode/EventEngineTests.cs`
- `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`
- `Assets/Tests/EditMode/GameSessionChronicleTests.cs`
- `Assets/Tests/EditMode/GameSessionSaveTests.cs`
- `Assets/Tests/EditMode/GameSessionSuccessionTests.cs`
- `Assets/Tests/EditMode/SC010TimePauseTests.cs`
- `Assets/Tests/EditMode/SC011EventContentTests.cs`
- `Assets/Tests/EditMode/SC011LegacyEventTests.cs`
- `Assets/Tests/EditMode/SuccessionSaveTests.cs`
- `Assets/Tests/EditMode/SuccessionServiceTests.cs`
- `Assets/Tests/EditMode/TitleScreenControllerTests.cs`

Documentation:

- `SC013_DEMO.md`
- `docs/research/SC013-fixed-scenario-prior-art.md`
- `docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md`
- `docs/superpowers/plans/2026-11-03-sc-013-m2-fixed-three-generation.md`
- `PROJECT_STATE.md`, `ARCHITECTURE.md`, `DECISIONS.md`
- `docs/reports/sc013-implementation.md`

`FoundationScene.unity` and `SampleScene.unity` were not edited. Existing unrelated dirty files (old-direction markdown, `Assets/1.unity`, `ProjectSettings/SceneTemplateSettings.json`, `graphify-out/`, `nul`) were left untouched.

## SC-011 preservation evidence

A before/after comparison of `HEAD` against the working tree found:

- SC-011 event IDs compared: `event.rainy-arrival`, `event.rainy-arrival.legacy-honored`, `event.rainy-arrival.legacy-deferred`; changed: none.
- SC-011 chain compared: `chain.rainy-arrival`; changed: none.
- SC-011 localization IDs compared: 18; changed: none.
- New catalog IDs: `event.sc013.inheritance-pledge`, `event.sc013.finale`, `chain.sc013.main`.
- New localization IDs: the ten `event.sc013.*`, `option.sc013.*`, and `summary.sc013.*` rows.
- Event/text `contentVersion` remains `2` in both revisions.

## Static verification evidence

These are host-side checks using Unity's bundled toolchain, not Unity Editor or Test Runner results.

1. **Roslyn type-check (8 assemblies, 0 errors)**
   - Compiler: `C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Data\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll`
   - References: Unity `6000.6.6f1` UnityEngine/UnityEditor reference assemblies, project packages, and built project assemblies.
   - Assemblies: Domain, Persistence, Events, Application, Presentation, Editor, PlayerMovement2D, EditModeTests.
   - Result: all eight compiled with exit code 0 and no diagnostics.
2. **Fixed-scenario logic harness: 30/30 checks passed**
   - Six people/member roles, approved ages, direct line, roster rows, generation labels, heir validity.
   - Pledge and rainy-arrival gates, pending-event block, first/second handoff targets, no fixed heir in generation three, finale-required block, normal succession after finale.
   - Seek-aid vs stand-alone ending derivation and distinct text.
3. **Full event-flow harness: 33/33 checks passed**
   - Real `event-catalog-v1.json` + localization loaded through `EventEngine`; catalog validation succeeded.
   - Order: pledge → rainy-arrival → legacy-honored → two handoffs → finale.
   - Relationship values: keep +5, rainy offer-rest +5, legacy renew +5.
   - Seek-aid unavailable at 4, available at 5; stand-alone available; ending derived from committed finale history.
   - SC-013 rows are scenario-tagged; SC-011 root remains neutral.

## Unity verification status (not claimed)

The existing Editor process (`Unity.exe`, PID 55880) is running with `-projectpath "C:\Users\panye\My project"`. `Library/ScriptAssemblies` timestamps predate this worker's source changes, so no Unity compilation or EditMode result from the current sources was obtained. This worker did not run batchmode, open a temporary project copy, use screenshots, use Computer Use, or operate the Editor GUI.

- Unity compilation: not run after the current changes.
- EditMode tests: not run.
- Console error/warning counts: not inspected.
- `FoundationScene.unity` SC-013 updater A: not run.
- `FoundationScene.unity` SC-013 updater B: not run.
- `FoundationSceneValidator`: not run; the scene does not yet contain the two new roots.
- Play Mode: not run.
- User Play Test acceptance: pending.

## Corrections made in this pass

- `M2EventFlowTests` no longer assumes the finale is entered at relationship 5; it now moves from the actual accumulated value to 4 and back to 5 before checking the gate.
- `GameSession.FixedHeirSummary` distinguishes "no next generation" from "finale completed" in generation three.
- `FamilyRosterPanel.BindReferences` no longer builds runtime row clones during the editor generator, preventing generated rows from being serialized into `FoundationScene`.
- `SC013_DEMO.md` now matches the real open-button behavior, the pre-finale heir text, and the title-return step.

## Remaining steps

1. In the existing Editor, run `Tools/Sengoku Chronicle/Add SC-013 Family Roster Panel`.
2. Run `Tools/Sengoku Chronicle/Add SC-013 Title Scenario Buttons`.
3. Run `Tools/Sengoku Chronicle/Validate Foundation Scene` and read `Logs/SceneValidationReport.txt`.
4. Run the focused EditMode tests and record Passed/Failed/Skipped totals.
5. Follow `SC013_DEMO.md` in Play Mode and return the text-only result checklist.
6. Only after user acceptance: checkpoint, merge, tag, and push (lead-owned; not done here).

## Post-review corrections (2026-11-03)

A separate read-only review (`docs/reports/sc013-review.md`) returned the implementation with one blocking and seven non-blocking findings. This pass addressed them without touching `FoundationScene.unity`:

- **B1 fixed:** `WorldClockTests.GameSession_PendingEventBlocksTravelAndAdvancementUntilResolved` now calls `session.InitStandardDemo()` before advancing, so it exercises the intended SC-010 time/event contract instead of the M2 default. SC-011 behavior is untouched.
- **Roster layout sized for seven SC-010 rows:** the roster panel is now 760×620; the content is 700×420 (seven rows = 7×52 + 6×6 + 8×2 = 416 px) at y=25, the title at y=280, the heir line at y=-225 and the close button at y=-283. The M2 six-row roster (358 px) and the SC-010 seven-row roster both fit without overlapping controls. `AddSC013FamilyRosterPanelTests` now asserts the capacity and the content/heir/close separation.
- **Documentation counts aligned:** `PROJECT_STATE.md` now reports harness 30/30 and 33/33, matching `SC013_DEMO.md` and this report.
- **Roster row display:** `FamilyRosterPanel.FormatRow` prints a generation label once when it equals the role name, so Sakon renders exactly `家臣 · 佐近（40岁）` as the demo expects.
- **One-parent kinship wording:** `SC013_DEMO.md` now says 信正's 父亲 (the fixed roster has one recorded parent for him), not 父母. `M2FixedScenario.BuildKinship` now prints `父亲：` when exactly one parent link is recorded and keeps `父母：` for two, so the rendered roster text matches the demo.
- **Dirty-scene preflight documented:** `SC013_DEMO.md` step 2 now tells the player to save FoundationScene or confirm it has no unsaved changes before running the two updaters.
- **Scenario-isolation coverage added:** `M2ScenarioSaveTests.Load_RejectsSc013PendingEventUnderNonM2Succession` proves an SC-013 pending save is rejected by both SC-010 standard and crisis sessions without mutating the target; `M2StoryGateTests.RejectsNonFixedHeirWhileASuccessionDecisionIsActive` restores an M2 save in `AwaitingSuccessionChoice` and proves the non-fixed candidate is rejected by the M2 guard, not by the generic no-pending-decision check.
- **Single source of stable IDs:** `M2FixedScenario` now aliases `SC013EventIds` instead of repeating the event/option/chain literals.

Re-verified after these edits (host-side only):

- Roslyn type-check with the same Unity-bundled compiler: Domain, Persistence, Events, Application, Presentation, Editor, PlayerMovement2D and EditModeTests all compile with 0 errors. The four changed assemblies (Application, Presentation, Editor, EditModeTests) were recompiled in this pass; the other four were unchanged.
- Fixed-scenario harness: 30/30 checks passed.
- Full event-flow harness: 33/33 checks passed against the real catalog/localization.
- SC-011 preservation re-checked against `HEAD`: all three SC-011 events, `chain.rainy-arrival`, and all 18 pre-existing localization rows are JSON-equal; the only catalog additions are the two `event.sc013.*` rows, and both remain tagged `scenario: "sc013"`.

A second independent read-only review (`docs/reports/sc013-review-2.md`) returned ACCEPT for this correction pass and raised three non-blocking notes, all of which were then addressed: the B1 test comment now describes the M2 event sequence accurately, this report now counts seven prior non-blocking findings, and the single-parent kinship label matches the demo.

A third, fresh independent read-only review (`docs/reports/sc013-review-3.md`) returned ACCEPT for the corrected source/test/documentation state. It confirmed B1, findings 1–7, and nits N1–N3 are fixed; independently reproduced the host-side eight-assembly Roslyn check (0 diagnostics) and the 30/30 and 33/33 harness results; re-verified the roster arithmetic (six rows = 358 px, seven rows = 416 px inside 420 px); and re-confirmed the SC-011 JSON equality against `HEAD`. It left three low-severity, non-blocking notes (unused duplicate `*TextId` constants inside `SC013EventIds`, a test that hard-codes layout constants, and optional extra scenario-isolation cases) plus a Play Mode wide-aspect risk. No code round was requested.

Still not run: Unity Editor compilation, Unity Test Runner/EditMode, `FoundationScene` updaters/validator, Console counts, Play Mode, and user acceptance. The roster must still be visually confirmed in Play Mode for both the M2 and SC-010 rosters.

## Review status

Source/static review is complete: review 1 returned RETURN (B1 + findings 1–7), review 2 returned ACCEPT for the correction pass, and review 3 (`docs/reports/sc013-review-3.md`) returned ACCEPT for the final corrected state. These reviews are not Unity or Play Mode evidence. The remaining gates are the Editor steps in `SC013_DEMO.md` section 1, the focused/full EditMode run, and the user's Play Test acceptance.
