# SC-013 M2 Fixed Three-Generation — Second Read-Only Review

Date: 2026-11-02  
Reviewer: independent read-only OpenCode2 review worker (no source/scene edits)  
Branch: `feature/sc013-fixed-three-generation`  
Worktree: `C:\Users\panye\My project`  
Prior review: `docs/reports/sc013-review.md` (left untouched)  
Implementation report: `docs/reports/sc013-implementation.md`  
Play script: `SC013_DEMO.md`

## Verdict

**ACCEPT for the source/static correction pass.** No blocking defect was found in the corrections. The prior B1 and non-blocking findings 1–7 are addressed in the source, tests, or documentation as the prior review requested. A few small documentation/display nits remain below (N1–N3), but none justify another code round.

This is **not** Unity-test or Play Mode acceptance. Unity Editor compilation, Unity Test Runner/EditMode, the `FoundationScene` updaters and validator, Console counts, Play Mode visual confirmation, and user acceptance are still unrun and are **not** claimed here.

## Disposition of the prior findings

| Prior finding | Status | Evidence |
|---|---|---|
| B1 — stale SC-010 time/event test fails under the M2 default | Fixed | `Assets/Tests/EditMode/WorldClockTests.cs:403-426`; `InitStandardDemo()` at `:408`; the original assertions are unchanged |
| 1 — roster content under-sized for six/seven rows | Fixed in source arithmetic | `AddSC013FamilyRosterPanel.cs:277-293`; new capacity/separation test at `AddSC013FamilyRosterPanelTests.cs:138-162` |
| 2 — `PROJECT_STATE.md` reported 13/13 and 25/25 | Fixed | `PROJECT_STATE.md:24` and `:54` now report 30/30 and 33/33; the old counts appear only in the prior review |
| 3 — demo row 6 disagreed with the rendered roster | Fixed | `FamilyRosterPanel.cs:136-141`; `SC013_DEMO.md:54` is now `家臣 · 佐近（40岁）` |
| 4 — demo did not tell the player to save/clean the scene first | Fixed | `SC013_DEMO.md:19` |
| 5 — no cross-scenario save-rejection test or active-decision M2 guard test | Fixed | `M2ScenarioSaveTests.cs:28-68`; `M2StoryGateTests.cs:90-121` |
| 6 — duplicated SC-013 stable-ID literals | Fixed | `M2FixedScenario.cs:101-110` aliases `SC013EventIds`; only the `event.sc013.` prefix test remains at `:159` |
| 7 — demo said 信正's 父母 while only one parent is recorded | Fixed in the demo | `SC013_DEMO.md:55` now says 信正的父亲; see residual note N3 about the rendered label |

## Verification of the requested checks

### 1. B1 and the legacy SC-010 contract

`WorldClockTests.GameSession_PendingEventBlocksTravelAndAdvancementUntilResolved` now calls `session.InitStandardDemo()` at `WorldClockTests.cs:408`. `InitStandardDemo()` runs the full `InitSession` reset (`GameSession.cs:293-314`), so the test starts from the SC-010 standard succession rather than the M2 default. The test body (`:409-425`) is otherwise unchanged: it still proves that a pending event blocks day advancement, month advancement, and travel preview, then proves month advancement resumes after the event is resolved. The standard demo queues the accepted `event.rainy-arrival` once (`onceScope: PerGame` in `event-catalog-v1.json`), so the second month correctly leaves `PendingEventId` empty.

The fix therefore restores the intended legacy contract instead of weakening it. The SC-011 path is unchanged in source and content (see item 8).

### 2. Roster layout arithmetic and generator validation

`AddSC013FamilyRosterPanel.cs` now creates:

- panel: 760 × 620, centered (`:280`), so local y spans −310 … +310;
- title: y = 280, height 36 (`:284-285`), spans 262 … 298;
- content: 700 × 420 at y = 25 (`:291-292`), spans −185 … 235;
- heir line: y = −225, height 34 (`:309-310`), spans −242 … −208;
- close button: y = −283, height 42 (`:312-313`), spans −304 … −262.

Vertical layout: 52 px rows, 6 px spacing, 8 px top/bottom padding (`:293`, `:305`, `:386-394`).

- Six M2 rows: 6 × 52 + 5 × 6 + 16 = **358 px**.
- Seven SC-010 rows: 7 × 52 + 6 × 6 + 16 = **416 px**.

The 420 px content therefore fits seven rows with 4 px to spare. The gaps are 27 px between title bottom (262) and content top (235), 23 px between content bottom (−185) and heir top (−208), and 20 px between heir bottom (−242) and close top (−262). The close button bottom (−304) remains inside the panel bottom (−310).

The generator's `Validate` still checks containment and the title/content, content/heir, heir/close overlaps at `AddSC013FamilyRosterPanel.cs:204-218`, using the helpers at `:510-537`. The new test `RosterContentFitsSevenStandardRowsWithoutOverlappingControls` (`AddSC013FamilyRosterPanelTests.cs:138-162`) asserts the seven-row capacity and the two lower separations; because it calls `Update`, the generated scene is also run through the full `Validate` path. These are static/arithmetic checks; Play Mode visual confirmation of both rosters is still unrun.

### 3. Project-state counts

`PROJECT_STATE.md:24` now says “逻辑 harness 30/30、33/33” and `:54` says “harness 30/30、完整事件链 harness 33/33”. A repository-wide grep for `13/13` and `25/25` finds them only in the prior review, which must remain untouched. The historical 22/22, 17/17, 14/14 line at `PROJECT_STATE.md:40` belongs to the earlier SC-012 section, not SC-013.

### 4. Demo row 6 and parent wording

`FamilyRosterPanel.FormatRow` (`FamilyRosterPanel.cs:129-145`) now suppresses the generation label when it equals the role name (`:138-141`). For Sakon, `M2FixedScenario.GenerationLabel` returns `家臣` (`M2FixedScenario.cs:380-389`) and `RoleName` is also `家臣` (`:49`), so the identity line renders exactly:

`家臣 · 佐近（40岁）`

This matches `SC013_DEMO.md:54`. The kinship line is still appended separately (`FamilyRosterPanel.cs:142-144`), and the demo's note at `SC013_DEMO.md:55` still covers `无亲属记录`.

The demo now says 信正的父亲, matching the single Masatsuna → Nobumasa parent link (`M2FixedScenario.cs:142`). See N3 for a small residual label mismatch in the rendered kinship text.

### 5. Save-before-updater instruction

`SC013_DEMO.md:19` now tells the player to save `FoundationScene.unity` or confirm it has no unsaved changes, and explains that both updaters refuse a dirty scene. This matches the preflight at `AddSC013FamilyRosterPanel.cs:68-73` (and the equivalent in `AddSC013TitleScenarioButtons`).

### 6. Cross-scenario and M2-guard tests

**Cross-scenario test.** `M2ScenarioSaveTests.Load_RejectsSc013PendingEventUnderNonM2Succession` (`:28-68`) starts an M2 source session, advances one month, and captures the save with `event.sc013.inheritance-pledge` pending (`:36-38`). It then replaces the save's succession block with a standard SC-010 succession (`:43-44`), so the payload is genuinely an SC-013 pending event plus a **non-M2 saved succession**, not merely an M2 save loaded into a standard target. The rejection context is derived from the saved succession via `GameSession.TryApplySave` → `BuildEventRuleContext(succession)` (`GameSession.cs:595-600`, `:998-1005`) and `EventEngine.TryRestoreState` → `ScenarioMatches` (`EventEngine.cs:265-312`, `:503-511`). The test checks both a standard target (`:46-52`) and a crisis target (`:54-60`), and checks that a rejected load leaves `PendingEventId` empty. The expected message contains the pledge event ID (`:49`).

One coverage nuance: both target cases use the same standard saved succession; the test does not separately pair the SC-013 event with a crisis-saved succession. That is not required by the prior finding, and the scenario gate is generic, but it is worth noting as optional extra coverage.

**M2 guard test.** `M2StoryGateTests.RejectsNonFixedHeirWhileASuccessionDecisionIsActive` (`:90-121`) captures an M2 save, sets `phase = AwaitingSuccessionChoice`, clears the pending successor/regent (`:100-102`), restores it, and asserts `Succession.Phase == AwaitingSuccessionChoice` (`:104-105`). It then calls `SelectSuccessionSuccessor(person.sc013.tokihime)`. The guard at `GameSession.cs:374-396` is therefore reached with a real active phase, not the generic “no pending decision” branch, and the assertion on `result.Message` containing `固定继承人` (`:112`) distinguishes the M2 guard message from the generic `SuccessionService` message (`该人物不是当前合法继承人。`, `SuccessionService.cs:202-204`). The head and phase are asserted unchanged afterward (`:113-115`).

### 7. Single source of stable IDs

`M2FixedScenario.cs:101-110` now aliases `SC013EventIds` for the chain, both events, and all four options. The only remaining `event.sc013.` literal in the class is the prefix check in `IsScenarioEvent` (`:159`), which is not a duplicated stable ID. `SC013EventIds` is defined once in `EventCatalogData.cs:157-181`.

### 8. SC-011 preservation

I independently compared the worktree resources against `HEAD` with `git show HEAD:...` and a JSON comparison:

- catalog `contentVersion`: 2 at both revisions;
- removed events: none; added events: `event.sc013.inheritance-pledge`, `event.sc013.finale`;
- `event.rainy-arrival`, `event.rainy-arrival.legacy-honored`, `event.rainy-arrival.legacy-deferred`: JSON-equal to `HEAD`;
- removed chains: none; added chain: `chain.sc013.main`; `chain.rainy-arrival`: JSON-equal to `HEAD`;
- localization `contentVersion`: 2 at both revisions; 18 → 28 entries; removed: none; changed: none; added: the ten `event.sc013.*`, `option.sc013.*`, and `summary.sc013.*` rows;
- both SC-013 events remain tagged `scenario: "sc013"`; SC-011 rows remain scenario-neutral.

The `EventEngine` diff adds the scenario gate but leaves SC-011's conditions, options, effects, and neutral scenario status intact.

### 9. Repository hygiene

- `FoundationScene.unity` is unchanged: no diff against `HEAD`; its mtime (15:11) predates this correction pass; it contains zero `FamilyRosterRoot`/`FamilyRosterPanel`/`M2EndingPanel`/`SC013StandardSampleButton` strings.
- No package change: nothing under `Packages` in `git status` or the `HEAD` diff.
- No new scene: the only untracked `.unity` file is the pre-existing `Assets/1.unity` (mtime 2026-10-21).
- Pre-existing WIP was not touched by this pass: `Assets/Scenes/SampleScene.unity` (mtime 2026-10-20), `Assets/1.unity` (2026-10-21), `ProjectSettings/SceneTemplateSettings.json` (2026-10-20), `graphify-out/graph.json` (2026-11-02 07:54), `nul` (2026-11-02 10:46), and the old-direction dirty markdown files (mtime 2026-11-02 08:24).
- Correction-pass mtimes cluster at 16:27–16:30 on only the SC-013-owned files: `FamilyRosterPanel.cs`, `WorldClockTests.cs`, `AddSC013FamilyRosterPanel.cs`, `M2FixedScenario.cs`, `M2StoryGateTests.cs`, `AddSC013FamilyRosterPanelTests.cs`, `SC013_DEMO.md`, `docs/reports/sc013-implementation.md`, `M2ScenarioSaveTests.cs`, `PROJECT_STATE.md`.

## Remaining findings

No blocking findings.

### N1 — Corrected B1 test comment still describes the M2 sequence incorrectly (non-blocking)

`Assets/Tests/EditMode/WorldClockTests.cs:405-406` says the M2 default would “queue a second mandatory SC-013 follow-up after the rainy-arrival event.” That is not the sequence. In M2, month one queues the SC-013 pledge; month two queues the SC-011 `event.rainy-arrival`; there is no third mandatory event before the first handoff. The actual reason the test needed `InitStandardDemo()` is that the M2 first/second events differ from the standard sample. The code fix is correct; only the comment is misleading. Suggested wording: “the M2 default starts with the SC-013 pledge and still has the SC-011 rainy-arrival event pending after the second month.”

### N2 — Implementation report undercounts the prior findings (non-blocking)

`docs/reports/sc013-implementation.md:151` says the prior review returned “one blocking and six non-blocking findings.” The prior review contains one blocking finding and **seven** numbered non-blocking findings (1–7). The correction section also merges findings 3 and 7 into one bullet. This is a documentation traceability nit, not a code issue.

### N3 — Rendered kinship label for 信正 still says “父母” (non-blocking)

`M2FixedScenario.BuildKinship` (`M2FixedScenario.cs:400-429`) prints `父母：明智 正綱` for 信正 because the only recorded parent link is Masatsuna → Nobumasa (`:142`), while the demo now says 信正的父亲 (`SC013_DEMO.md:55`). The demo wording is no longer one-parent-inaccurate, and “父母：” can be read as a category label, but the rendered text still uses a plural label for a single recorded parent. Optional polish: use a neutral/accurate label (or clarify the demo) if the exact roster text is part of acceptance.

## Residual risks and coverage notes

- **Play Mode layout is unverified.** The layout conclusion is arithmetic plus the generator's static `Validate`; the six-row M2 and seven-row SC-010 rosters have not been rendered in Play Mode.
- The 620 px panel is close to the 720 px reference canvas height. At the standard 16:9 reference it has ~50 px top/bottom margin; very wide aspect ratios with the existing CanvasScaler (`ScaleWithScreenSize`, 1280×720, match 0.5) could reduce the canvas height in reference units. This is a Play Mode/visual-check item, not a source failure found here.
- The cross-scenario test uses a standard saved succession for both the standard and crisis target cases; a crisis-saved succession paired with an SC-013 event, and the reverse direction (SC-010 event under an M2 succession), are not separately covered.
- The implementer's harness programs (`sc013-harness/Program.cs`, `EventFlow.cs`) exercise fixed-scenario logic and the event chain, not the roster layout, scenario-aware save rejection, active-decision M2 guard, or the corrected `WorldClockTests` case.

## Checks performed

1. Read `docs/reports/sc013-review.md`, `docs/reports/sc013-implementation.md`, `SC013_DEMO.md`, `PROJECT_STATE.md`, and the changed SC-013 source/test files.
2. Traced the B1 test through `GameSession.InitStandardDemo` / `InitSession`, the standard event schedule, and `WorldClockTests.ResolvePendingEvent`; confirmed the retained assertions match the SC-010 contract.
3. Recomputed the roster panel/content/title/heir/close rectangles and the 358/416 px row budgets; inspected the generator's `Validate` containment/overlap calls and the new roster capacity test.
4. Inspected `FamilyRosterPanel.FormatRow`, `M2RosterRow` role/generation values, and the demo's six expected rows; checked the duplicate-label suppression path for Sakon.
5. Inspected `GameSession.TryApplySave` / `BuildEventRuleContext` and `EventEngine.TryRestoreState` / `ScenarioMatches`; verified that the cross-scenario test pairs an SC-013 pending event with a standard (non-M2) saved succession and that the M2 guard test restores `AwaitingSuccessionChoice` before calling `SelectSuccessionSuccessor`.
6. Confirmed `M2FixedScenario` aliases `SC013EventIds`; searched the file for remaining SC-013 event/option/chain literals.
7. Independently compared SC-011 event, chain, and localization rows against `git show HEAD:...` with a JSON comparison; verified only SC-013 additions were made and `contentVersion` remains 2.
8. Checked `git status`, `git diff --stat HEAD`, key file mtimes, the absence of `FoundationScene` SC-013 strings, the old `Logs/SceneValidationReport.txt` timestamp, and the pre-existing WIP files.
9. Confirmed `PROJECT_STATE.md` no longer contains `13/13` or `25/25`; confirmed `SC013_DEMO.md` contains the save/clean-scene instruction and the corrected row 6/parent wording.

## Evidence limits

- **Unity:** I did not run Unity compilation, batchmode, Unity Test Runner/EditMode, the `FoundationScene` updaters, `FoundationSceneValidator`, Play Mode, screenshots, or Computer Use. I did not edit implementation code, scenes, prefabs, `ProjectSettings`, `Library`/`Temp`, `graphify-out`, `nul`, or existing documentation; this report is the only file created.
- **Host-side implementer evidence:** the reported Roslyn type-check (0 errors) and harness counts (30/30, 33/33) were **not independently reproduced** here. The temp build artifacts under `%TEMP%\opencode\sc013-build` are timestamped 16:28–16:29 (consistent with a post-correction compile), but their presence is not proof of zero diagnostics, and the harness sources do not cover the corrected tests.
- **Stale Unity artifacts:** `Library/ScriptAssemblies/SengokuChronicle.*.dll` are timestamped 15:03 and `Logs/SceneValidationReport.txt` is timestamped 15:04, both before the correction pass at 16:27–16:30. They are **not** evidence for the current sources.
- **Scene integration:** `FoundationScene.unity` contains no SC-013 roster/ending roots or title entries, so the two updaters and validator still need to be run in the user's open Editor.
- **Play Mode and acceptance:** the roster layout, save/load flow, endings, and title routes have not been exercised at runtime; user Play Test acceptance remains pending.
