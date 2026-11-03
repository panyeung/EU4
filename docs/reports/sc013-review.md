# SC-013 M2 Fixed Three-Generation — Read-Only Review

Date: 2026-11-02  
Reviewer: read-only OpenCode2 review worker (no source/scene edits)  
Branch: `feature/sc013-fixed-three-generation`  
Worktree: `C:\Users\panye\My project`  
Spec: `docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md`  
Plan: `docs/superpowers/plans/2026-11-03-sc-013-m2-fixed-three-generation.md`  
Implementation report: `docs/reports/sc013-implementation.md`  
Play script: `SC013_DEMO.md`

## Verdict

**RETURN** — the M2 source path itself is coherent and covers the approved baseline, but one existing EditMode regression test is predictably broken by the new M2 default (`Assets/Tests/EditMode/WorldClockTests.cs:403-422`). The suite cannot be green as written, so this should not be handed to Play Test acceptance as “implementation reviewed/complete” until that test is corrected and the focused/full EditMode run is green. FoundationScene generation and validation, Unity compilation, Console inspection, Play Mode, and user acceptance are still outstanding and are **not** claimed here.

No source-level blocker was found in the fixed three-generation play path itself.

## Blocking finding

### B1 — Stale SC-010 time test now fails under the M2 default

File: `Assets/Tests/EditMode/WorldClockTests.cs:403-422`, test `GameSession_PendingEventBlocksTravelAndAdvancementUntilResolved`.

- The test creates `GameSession.NewGame()` at line 405. `NewGame()` now initializes the M2 fixed scenario (`GameSession.cs:286-288`).
- It advances one month (`:406`), resolves the first pending event (`:418`), advances one month again (`:420`), and then asserts `session.PendingEventId` is empty (`:422`).
- In M2, the first month queues `event.sc013.inheritance-pledge`; after that is committed, the second month queues the still-mandatory SC-011 event `event.rainy-arrival` (priority 10; `SuccessionCountEquals 0`). Therefore `PendingEventId` is `event.rainy-arrival`, not empty.
- This is the standard-sample behavior the test was written for. The other affected suites were deliberately moved to `InitStandardDemo()` (for example `SC010TimePauseTests.cs`, `SC011LegacyEventTests.cs`, `GameSessionSaveTests.cs:296`); this test was missed.
- Fix (for the implementer, not this reviewer): call `session.InitStandardDemo()` in this test, or resolve the second event and assert the resulting state explicitly.
- Why it blocks: the project requires a green EditMode pass before acceptance. A full or focused `WorldClockTests` run will fail at line 422 even if the SC-013-specific tests pass.

## Non-blocking findings

1. **Roster content is under-sized for the row count.** `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs:289` creates `FamilyRosterContent` at 330 px high; `:302` gives each row a preferred height of 52; `ConfigureVerticalLayout` adds 6 px spacing and 8 px top/bottom padding. Six M2 rows need ≈358 px, seven SC-010 standard rows need ≈416 px. There is no `ScrollRect`/`ContentSizeFitter`. The M2 last row can overlap the heir line by roughly 2 px; the standard roster’s later rows can overlap the close-button area. Action: give the content a scroll view or enlarge/shrink the row budget, and verify both M2 and standard rosters in Play Mode.

2. **Harness counts disagree in project documentation.** `PROJECT_STATE.md:24` says “逻辑 harness 13/13、25/25”, while `PROJECT_STATE.md:54`, `docs/reports/sc013-implementation.md:105,109`, and `SC013_DEMO.md:9` say 30/30 and 33/33. Action: correct the table to 30/30 and 33/33.

3. **Demo row 6 does not match the rendered roster.** `SC013_DEMO.md:54` expects `家臣 · 佐近（40岁）`. `M2FixedScenario.GenerationLabel` returns `"家臣"` for Sakon (`M2FixedScenario.cs:378-387`) and `FamilyRosterPanel.FormatRow` prints both generation and role (`FamilyRosterPanel.cs:136-137`), producing `家臣 · 家臣 · 佐近（40岁）`. Action: suppress the duplicate label in `FormatRow`, or change Sakon’s generation label and update the demo.

4. **The updater preflight refuses a dirty scene, but the play script does not say to save first.** `AddSC013FamilyRosterPanel.cs:68-72` and `AddSC013TitleScenarioButtons.cs:56-60` return `SceneDirty` before doing anything. `SC013_DEMO.md:16-31` tells the user to open FoundationScene and run the menus but does not instruct them to save/confirm a clean scene. Action: add “save FoundationScene or confirm it has no unsaved changes” before step 3.

5. **Test coverage gaps around scenario isolation.** `M2ScenarioSaveTests.cs:13-26` covers the six M2 stable points, but no test loads a save with an SC-013 pending event under a non-M2 succession to prove the `GameSession.cs:598` / `EventEngine.cs:265-312` cross-scenario rejection. `M2StoryGateTests.cs:51-54` calls `SelectSuccessionSuccessor(tokihime)` while the succession phase is `None`, so it passes for the generic “no pending decision” reason rather than exercising the M2 fixed-heir guard (the `SuccessionCandidates` assertion does cover the filter). Action: add those two focused tests.

6. **Duplicate stable-ID constants.** `M2FixedScenario.cs:101-109` repeats the SC-013 event/option IDs already declared in `EventCatalogData.cs` (`SC013EventIds`). There is no current defect, but two sources of truth can drift. Action: reference one set from the other.

7. **Demo kinship wording is one-parent in the data.** `SC013_DEMO.md:55` says “信正的父母”; `M2FixedScenario.CreateSuccession` links only Masatsuna → Nobumasa (`M2FixedScenario.cs:140`). The fixed six-person roster contains no second parent. Action: say “父亲” or clarify the roster relationship.

## Checks performed

- Read the SC-013 spec, plan, implementation report, `SC013_DEMO.md`, prior-art report, all new SC-013 source/test files, and the tracked diff against `HEAD`.
- Confirmed the SC-011 contract is unchanged by source and content comparison:
  - `event.rainy-arrival`, `event.rainy-arrival.legacy-honored`, `event.rainy-arrival.legacy-deferred`, and `chain.rainy-arrival` are JSON-equal between `HEAD` and the worktree.
  - The old 18 localization rows are all present and unchanged; the only additions are the ten `event.sc013.*` / `option.sc013.*` / `summary.sc013.*` rows.
  - Both content versions remain `2`; the catalog/text diffs are pure additions (86 and 50 inserted lines, no deletions).
  - No SC-013 event row is scenario-neutral; both are tagged `"sc013"`; no referenced text ID is missing.
- Traced the M2 path in source:
  - `GameSession.InitNewGame` → `M2FixedScenario.CreateSuccession`; `InitStandardDemo` and `StartCrisisDemoScenario` retain separate SC-010 entries (`GameSession.cs:286-340`).
  - Six-person roster, approved ages (70/66/46/22/44/40), direct line 晴信 → 正綱 → 信正, fixed heirs, and the +5/−5 Iori pledge options (`M2FixedScenario.cs:127-148`, `:378-387`; catalog rows).
  - M2 retirement gate: pledge + rainy-arrival before first handoff; legacy event before second; finale before any third-generation handoff; pending event always blocks (`M2FixedScenario.cs:183-272`, `GameSession.cs:354-396`).
  - Finale `seek-aid` requires Iori relationship ≥ 5; `stand-alone` is always available; ending title/body are derived from committed finale and prior choices, not a save field (`M2FixedScenario.cs:308-349`).
  - Save restore validates the loaded succession before the event runtime and passes the derived scenario context (`GameSession.cs:595-600`, `EventEngine.cs:265-312`).
- Confirmed the panels and title routes are implemented: `FamilyRosterPanel`, `M2EndingPanel`, `addSC013...` generators, and `SuccessionPanel`/`TitleScreenController` bindings.
- Confirmed generator transactionality structure: one Undo group, validation before save, `Undo.RevertAllDownToGroup` on exception, no scene reload.
- Checked new `.meta` files: 166 assets under `Assets`, zero duplicate GUIDs, no `.cs` file missing a `.meta`.
- Statically checked FoundationScene UI order and positions: the planned `FamilyRosterOpenButton` at top-right (−240,−20) does not overlap the existing `FamilyChronicleOpenButton` at (−20,−20); the title status move to −260 does not overlap the two new scenario buttons; root insertion would land between `FamilyChronicleRoot` and `M6TitleRoot` as the validator expects.
- Confirmed no new package or tracked `ProjectSettings` change; no new scene. The only untracked `.unity` file is the pre-existing `Assets/1.unity` (no SC-013 strings), and `ProjectSettings/SceneTemplateSettings.json` is the pre-existing untracked WIP. `Assets/Scenes/FoundationScene.unity` is unchanged; `SampleScene.unity` is pre-existing WIP and contains no SC-013 strings.
- Confirmed the running Editor is PID 55880 with `-projectpath "C:\Users\panye\My project"`. There are asset-import worker processes belonging to that Editor; no second project Editor was used.

## Design-adherence checklist

| Requirement | Source status |
|---|---|
| Six-person roster, identities, ages, generations, kinship | Represented (`M2FixedScenario.cs`, `FamilyRosterPanel.cs`) |
| New game defaults to M2 | Represented (`GameSession.InitNewGame`) |
| SC-010 standard and crisis title routes | Represented (`TitleScreenController`, `StartCrisisDemoScenario`) |
| Fixed heirs 晴信 → 正綱 → 信正 | Represented (`ExpectedHeir`, `SyncDesignation`, candidate filter) |
| +5 / −5 Iori pledge cost, both choices always available | Represented in catalog and tested in `M2EventFlowTests` |
| `seek-aid` only at Iori trust ≥ 5; stand-alone always available | Represented in catalog requirement and ending view |
| Distinct stand-alone ending | Represented (different title/body) |
| Same-session two lawful handoffs | Represented (`BeginSuccessionRetirement` + designated heir) |
| Save/load at stable points without duplicate history/effects | Represented in `M2ScenarioSaveTests`, not run here |
| No new save field, package, project, or scene | Confirmed in tracked changes |
| Accepted SC-011 rows unchanged | Confirmed by JSON equality and pure-append diffs |
| Scene generation/validation | **Not done**; FoundationScene has none of the SC-013 roots/buttons |

## FoundationScene status and Play Test handoff

`Assets/Scenes/FoundationScene.unity` contains zero occurrences of `FamilyRosterRoot`, `FamilyRosterPanel`, `M2EndingPanel`, `SC013StandardSampleButton`, or `SC013CrisisDemoButton`. The latest `Logs/SceneValidationReport.txt` (15:04) predates the SC-013 source and contains no SC-013 PASS lines. Therefore the existing Editor still needs, in order:

1. A clean/saved `FoundationScene.unity`.
2. `Tools/Sengoku Chronicle/Add SC-013 Family Roster Panel`.
3. `Tools/Sengoku Chronicle/Add SC-013 Title Scenario Buttons`.
4. `Tools/Sengoku Chronicle/Validate Foundation Scene`; expect the two SC-013 PASS lines. The pre-existing SC-005 feedback-banner error is still expected and is not an SC-013 regression.
5. Focused EditMode tests after fixing B1, then the Play Test in `SC013_DEMO.md`.

The manual Play Test can still be run after the scene work, but acceptance should wait for a green EditMode result.

## Evidence limits

- This review is source/static only. I did **not** run Unity compilation, Unity Test Runner, the FoundationScene generators, `FoundationSceneValidator`, Play Mode, screenshots, Computer Use, or any Unity GUI operation; and I did not edit source, scenes, or documentation other than this report.
- `Library/ScriptAssemblies/SengokuChronicle.Application.dll` is timestamped 15:03, while the SC-013 sources are 15:47–16:07; the last `Logs/SceneValidationReport.txt` is 15:04. These are **not** evidence for the current sources.
- The implementer’s Roslyn/harness results (8 assemblies, 30/30, 33/33) were not independently reproduced. The counts also disagree in `PROJECT_STATE.md` (see finding 2).
- The roster layout and title layout checks above are static rectangle/layout calculations; they are not a substitute for a Play Mode visual check.
- No Unity Console error/warning count, validator overall PASS, Play Mode result, save/load runtime result, or human acceptance is claimed.
