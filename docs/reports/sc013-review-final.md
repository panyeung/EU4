# SC-013 M2 Fixed Three-Generation — Final Independent Read-Only Review

Date: 2026-11-03  
Reviewer: independent read-only OpenCode2 review worker (no source, test, scene, or existing-report edits)  
Branch: `feature/sc013-fixed-three-generation`  
Worktree: `C:\Users\panye\My project`  
Inputs reviewed: `docs/reports/sc013-review.md`, `docs/reports/sc013-review-2.md`, `docs/reports/sc013-review-3.md`, `docs/reports/sc013-implementation.md`, the SC-013 spec and plan, `SC013_DEMO.md`, `PROJECT_STATE.md`, and the current diff against `HEAD`.

## Verdict

**ACCEPT for the source/static correction pass.** Every finding in `docs/reports/sc013-review.md` (B1 and non-blocking findings 1–7) is corrected in source, tests, or documentation, and no new blocking defect was found. The host-side eight-assembly Roslyn build and both logic harnesses pass when I rerun them from the current sources.

This is **not** Unity Editor, Unity Test Runner, scene-validator, Play Mode, or user acceptance. `FoundationScene.unity` is still unmodified and contains no SC-013 roots/buttons, so the two updaters, the validator, focused/full EditMode execution, Console inspection, Play Mode, and the user Play Test remain unrun and are **not** claimed here. See “Unity and Play Mode gates” and “Evidence limits”.

## Disposition of `docs/reports/sc013-review.md` findings

| Prior finding | Status | Independent evidence |
|---|---|---|
| B1 — stale SC-010 time/event test fails under the M2 default | Fixed | `Assets/Tests/EditMode/WorldClockTests.cs:405-409`; `InitStandardDemo()` at `:409`; the `git diff HEAD` is only three comment lines plus that call, and the original assertions at `:410-426` are unchanged |
| 1 — roster content under-sized for six/seven rows | Fixed | `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs:280` (760×620 panel), `:292` (700×420 content at y=25), `:305` (52 px row), `:386-394` (6 px spacing, 8 px padding); capacity test `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs:138-162` |
| 2 — `PROJECT_STATE.md` carried the pre-correction harness counts | Fixed | `PROJECT_STATE.md:24` and `:54` now say 30/30 and 33/33; the old `13/13`/`25/25` strings occur only in the two historical review reports |
| 3 — demo row 6 disagreed with the rendered roster | Fixed | `Assets/Scripts/Presentation/FamilyRosterPanel.cs:138-141` suppresses the duplicate `家臣 · 家臣`; `SC013_DEMO.md:54` is `家臣 · 佐近（40岁）` |
| 4 — demo did not tell the player to clean/save the scene | Fixed | `SC013_DEMO.md:19` saves/confirms a clean `FoundationScene.unity` before menu steps 3–4 (`:20`, `:23`); preflight at `AddSC013FamilyRosterPanel.cs:68-73` and `AddSC013TitleScenarioButtons.cs:56-61` |
| 5 — no cross-scenario save rejection or active-decision M2 guard test | Fixed | `Assets/Tests/EditMode/M2ScenarioSaveTests.cs:28-68`; `Assets/Tests/EditMode/M2StoryGateTests.cs:90-121` |
| 6 — duplicated SC-013 stable-ID literals | Fixed | `Assets/Scripts/Application/M2FixedScenario.cs:103-110` aliases `SC013EventIds`; the only remaining `event.sc013.` literal is the prefix test at `:159`; the IDs are defined once at `Assets/Scripts/Events/EventCatalogData.cs:158-181` |
| 7 — demo said 信正's 父母 while one parent is recorded | Fixed | `SC013_DEMO.md:55` says 父亲; `M2FixedScenario.cs:400-425` prints `父亲：` for one parent and `父母：` for two; assertions at `FamilyRosterPanelTests.cs:64-65` and `M2FixedScenarioTests.cs:73-74` |

The three notes newly raised by `sc013-review-3.md` (unused `*TextId` constants, a layout test that hard-codes the row constants, and optional extra scenario-isolation shapes) are low-severity and remain non-blocking; they are listed under “Residual notes” rather than treated as corrections required by `sc013-review.md`.

## Independent verification of the requested checks

### 1. Stale `WorldClock` test

`WorldClockTests.GameSession_PendingEventBlocksTravelAndAdvancementUntilResolved` calls `session.InitStandardDemo()` at `WorldClockTests.cs:409`, after `GameSession.NewGame()`. `InitStandardDemo()` performs the full `InitSession` reset (`GameSession.cs:293-314`), so the SC-010 standard succession, clock, relationships, event runtime, and random stream replace the M2 default. The diff against `HEAD` adds only:

- the three comment lines at `:405-407`, which now describe the M2 schedule correctly (pledge first, then the SC-011 rainy-arrival), and
- the `InitStandardDemo()` call at `:409`.

The assertions at `:410-426` are byte-for-byte unchanged: the first month queues the sample event, day/month/travel are blocked while it is pending, date and location do not move, and the second month ends at `1560-06-01` with `PendingEventId` empty. B1 is genuinely fixed, not weakened.

### 2. Roster sizing and row label

Recomputed from `AddSC013FamilyRosterPanel.cs`:

- Panel: 760×620, centered (`:280`), panel-local y spans −310 … +310.
- Title: y=280, height 36 (`:284-285`), spans 262 … 298.
- Content: 700×420 at y=25 (`:292`), spans −185 … 235.
- Heir line: y=−225, height 34 (`:309-310`), spans −242 … −208.
- Close button: y=−283, height 42 (`:312-313`), spans −304 … −262.

Row budget with 52 px rows, 6 px spacing, and 8 px top/bottom padding (`:305`, `:386-394`):

- six M2 rows: 6×52 + 5×6 + 16 = **358 px**;
- seven SC-010 rows: 7×52 + 6×6 + 16 = **416 px**.

Both fit the 420 px content. With top alignment, the seventh row bottom lands at panel-local −173, which is 35 px above the heir-line top (−208); the six-row case leaves 93 px. The content bottom (−185) is also 23 px above the heir top, and the heir bottom (−242) is 20 px above the close top (−262). No overlap by arithmetic.

`AddSC013FamilyRosterPanelTests.RosterContentFitsSevenStandardRowsWithoutOverlappingControls` (`:138-162`) asserts the 416 px capacity and the content/heir and heir/close separations; the generator's `Validate` (`AddSC013FamilyRosterPanel.cs:193-218`) also enforces containment and the title/content, content/heir, and heir/close overlap checks.

`FamilyRosterPanel.FormatRow` (`FamilyRosterPanel.cs:129-145`) prints the generation label once when it equals the role name. For Sakon, `M2FixedScenario.GenerationLabel` returns `家臣` (`M2FixedScenario.cs:380-388`) and `RoleName` is also `家臣` (`:49`), so the rendered identity line is exactly `家臣 · 佐近（40岁）`, matching `SC013_DEMO.md:54`. This is still a static/arithmetic check; neither roster has been rendered in Play Mode.

### 3. Report counts

- `PROJECT_STATE.md:24` reports “逻辑 harness 30/30、33/33”.
- `PROJECT_STATE.md:54` reports “harness 30/30、完整事件链 harness 33/33”.
- `docs/reports/sc013-implementation.md:109,113,165,166` reports 30/30 and 33/33.
- `SC013_DEMO.md:9` reports 30 and 33 checks.
- A repository-wide search for `13/13` and `25/25` finds them only inside the historical `sc013-review.md` and `sc013-review-2.md` records.

The implementation report's correction section at `:151` now counts “one blocking and seven non-blocking findings”, matching `sc013-review.md`.

### 4. Dirty-scene instruction before the updaters

`SC013_DEMO.md:19` tells the player to open `FoundationScene.unity`, wait for compilation, and then save the scene or confirm it has no unsaved changes, explaining that both updaters refuse a dirty scene. This precedes menu step 3 (`:20`) and step 4 (`:23`). The refusal is real:

- `AddSC013FamilyRosterPanel.CheckScenePreflight` returns `SceneDirty` at `AddSC013FamilyRosterPanel.cs:68-73`.
- `AddSC013TitleScenarioButtons.CheckScenePreflight` returns `SceneDirty` at `AddSC013TitleScenarioButtons.cs:56-61`.
- Both tests exercise the refusal (`AddSC013FamilyRosterPanelTests.cs:102-116`; `AddSC013TitleScenarioButtonsTests.cs:75-93`).

### 5. Cross-scenario save rejection test

`M2ScenarioSaveTests.Load_RejectsSc013PendingEventUnderNonM2Succession` (`:28-68`):

1. Starts an M2 session, advances one month, and asserts the SC-013 pledge is pending (`:36-37`).
2. Captures that save, then replaces its succession block with a freshly captured SC-010 standard succession (`:43-44`). The payload is therefore an SC-013 pending event plus a genuinely non-M2 saved succession, not merely an M2 save loaded into a standard target.
3. Loads it into a standard target (`:46-52`) and a crisis target (`:54-60`), asserting both refuse, both stay non-M2, and both leave `PendingEventId` empty.

Traced rejection path: `GameSession.TryApplySave` validates the saved succession and derives the event rule context from it (`GameSession.cs:595-600`, `BuildEventRuleContext` at `:998-1005`). `EventEngine.TryRestoreState` rejects the pending pledge through `ScenarioMatches` (`EventEngine.cs:277-283`, `:505-510`) with the message `Saved pending event is not present in the active scenario: event.sc013.inheritance-pledge`; the test pins that message to the pledge ID, so it cannot pass for an unrelated validation failure. Assignment of live state occurs only after every validation (`GameSession.cs:631-648`), so a rejected load is atomic.

### 6. Active-decision fixed-heir guard test

`M2StoryGateTests.RejectsNonFixedHeirWhileASuccessionDecisionIsActive` (`:90-121`) captures an M2 save, sets `succession.phase = AwaitingSuccessionChoice`, clears `pendingSuccessorId`/`pendingRegentId` (`:99-103`), restores it, and asserts the restore succeeded and the phase is genuinely active (`:103-105`). That shape is legal in restore validation (`Assets/Scripts/Persistence/SaveData.cs:329-338`).

It then calls `SelectSuccessionSuccessor(person.sc013.tokihime)`. With an M2 family, a non-`None` phase, and no committed finale, `GameSession.SelectSuccessionSuccessor` takes the M2 guard at `GameSession.cs:378-391`: `ExpectedHeir` is Masatsuna, tokihime is rejected with `SC-013 固定剧本只允许选择当前固定继承人。`, and the call returns before the generic `SuccessionService.SelectSuccessor` path (whose message would be `该人物不是当前合法继承人。` at `SuccessionService.cs:202-204`). The assertion on `固定继承人` therefore discriminates the intended branch; head and phase are asserted unchanged afterward (`:113-115`).

### 7. Single source of stable IDs

`M2FixedScenario.cs:103-110` aliases `SC013EventIds` for `chain.sc013.main`, both events, and all four options. A source scan finds only one remaining `event.sc013.` literal outside `EventCatalogData.cs`, and it is the prefix test in `IsScenarioEvent` (`M2FixedScenario.cs:157-159`), not a duplicated stable ID. `SC013EventIds` is declared once at `EventCatalogData.cs:158-181`.

### 8. SC-011 event, chain, and localization preservation

I compared the working tree against `git show HEAD:...` with a JSON parser, independently of the implementation report:

- Catalog `contentVersion`: **2** at both revisions.
- Events: 3 at `HEAD` → 5 now. Removed: **none**. Added: `event.sc013.inheritance-pledge`, `event.sc013.finale`.
- `event.rainy-arrival`, `event.rainy-arrival.legacy-honored`, `event.rainy-arrival.legacy-deferred`: **JSON-equal** to `HEAD`.
- Chains: 1 at `HEAD` → 2 now. Removed: **none**. Added: `chain.sc013.main`. `chain.rainy-arrival`: **JSON-equal** to `HEAD`.
- Localization `contentVersion`: **2** at both revisions. Entries: 18 → 28. Removed: **none**. Changed rows: **none**. Added rows are exactly the ten `event.sc013.*`, `option.sc013.*`, and `summary.sc013.*` text IDs.
- Both SC-013 events carry `"scenario": "sc013"`; all three SC-011 events remain scenario-neutral (`scenario` absent).
- `git diff --numstat HEAD -- Assets/Resources` shows 86/0 and 50/0: pure insertions, no deletions.

### 9. Repository hygiene, target scene, and forbidden additions

- `Assets/Scenes/FoundationScene.unity` has **no diff** against `HEAD`, and contains **zero** occurrences of `FamilyRosterRoot`, `FamilyRosterPanel`, `M2EndingPanel`, `SC013StandardSampleButton`, or `SC013CrisisDemoButton`. Its mtime is 15:11, before the correction pass.
- `Assets/Scenes/SampleScene.unity` is modified in the working tree, but it is pre-existing user WIP (mtime 2026-10-20) and contains zero SC-013 strings.
- No tracked change under `Packages/` or `ProjectSettings/`. The only `ProjectSettings` status is the pre-existing untracked `ProjectSettings/SceneTemplateSettings.json` (mtime 2026-10-20), which contains no SC-013 strings.
- No new scene: the only untracked `.unity` file is the pre-existing `Assets/1.unity` (mtime 2026-10-21), which contains no SC-013 strings.
- Untracked additions under `Assets/` are only 13 `.cs` files and their `.meta` files; no new asset, package, prefab, project copy, or binary was introduced.
- 166 `.meta` files under `Assets` have 166 unique GUIDs, zero duplicates, and every new SC-013 `.cs` file has a matching `.meta`.
- Pre-existing WIP remains untouched: `SampleScene.unity`, old-direction dirty markdown, `Assets/1.unity`, `ProjectSettings/SceneTemplateSettings.json`, `graphify-out/`, and `nul`.

### 10. Host-side compile and harness rerun (not Unity evidence)

I independently rebuilt the eight assemblies from the current sources with Unity's bundled Roslyn (`6000.6.6f1`, `sdk/8.0.318/Roslyn/bincore/csc.dll`) using the existing `.rsp` files:

- Domain, Persistence, Events, Application, Presentation, PlayerMovement2D, Editor, EditModeTests: **8/8 exit 0, zero diagnostics**.
- I then rebuilt both harnesses against the fresh project assemblies and reran them:
  - fixed-scenario harness: **PASS 30 checks**;
  - full event-flow harness against the real catalog/localization: **PASS 33 event-flow checks**.

These are host-side Roslyn/harness results only. The harness sources cover fixed-scenario logic and the catalog event chain; they do **not** exercise the roster layout, the cross-scenario save test, the active-decision guard test, or the corrected `WorldClockTests` case. Those were judged by source tracing and arithmetic above. I did not use the implementation report's own harness statements as evidence; the results above are my reruns.

## Residual notes (non-blocking)

1. **Unused duplicate `*TextId` constants.** `EventCatalogData.cs:170-180` declares ten `*TextId` constants whose values repeat the event/option IDs at `:160-168`, and none is referenced in source or tests. This is dead duplication confined to the owning class; optional cleanup only.
2. **Roster capacity test hard-codes row metrics.** `AddSC013FamilyRosterPanelTests.cs:148-151` recomputes `7*52 + 6*6 + 8*2` from literals instead of reading the generated `LayoutElement.preferredHeight` and `VerticalLayoutGroup` spacing/padding. The current values match the generator, but the test would not automatically track a future row-metric change.
3. **Scenario-isolation coverage shape.** Both targets in `M2ScenarioSaveTests` use the same standard-saved succession; there is no case pairing a crisis-saved succession with an SC-013 pending event. The reverse direction (an SC-010 neutral event under an M2 succession) is intentionally allowed because SC-011 rows are scenario-neutral, so it is not a defect.
4. **Wide-aspect Play Mode risk.** The 620 px panel sits inside a 720 px reference canvas (1280×720, `ScaleWithScreenSize`, match 0.5). At aspect ratios wider than 16:9 the reference height shrinks; by source arithmetic the margin becomes only a few pixels around 21:9 and is insufficient around 32:9. This is a visual check for the user's actual Game-view aspect, not a source failure found here.
5. **Cosmetic duplicate parent label.** `M2FixedScenario.BuildKinship` prints the `父母：` label once per link (`M2FixedScenario.cs:418-425`), so 正綱's line shows `父母：明智 晴信；父母：明智 千恵`. The demo says only “正綱的父母”, and `FamilyRosterPanelTests.cs:64` asserts the first fragment; this is cosmetic and not a blocker.
6. **Documentation nit.** `docs/reports/sc013-implementation.md:171` says the third review confirmed “nits N1–N3 are fixed” and then lists three new residual notes also called N1–N3. The following sentence clarifies both sets, but reusing the same labels for review-2 and review-3 notes is mildly ambiguous.

## Unity and Play Mode gates (not run, not claimed)

The following are **not** evidence obtained by this review:

- Unity Editor compilation of the current sources: **not run**.
- Unity Test Runner / focused or full EditMode execution: **not run**; no Passed/Failed/Skipped totals exist for this pass.
- Unity Console error/warning counts: **not inspected**.
- `Add SC-013 Family Roster Panel` updater: **not run**.
- `Add SC-013 Title Scenario Buttons` updater: **not run**.
- `FoundationSceneValidator`: **not run**; `Logs/SceneValidationReport.txt` predates the SC-013 source changes.
- Play Mode steps 1–26 in `SC013_DEMO.md`: **not run**.
- Save/load runtime behavior, ending visuals, and roster rendering: **not exercised**.
- User Play Test acceptance: **pending**.

`FoundationScene.unity` still contains none of the SC-013 roots, open button, roster panel, ending panel, or the two title entries, so the SC-013 feature is not yet reachable from the project's playable scene. That is the expected handoff state of this slice, not a source defect, but it means no SC-013 Play Mode claim can be made until the two menu updaters, the validator, and the user Play Test are completed.

## Checks performed

1. Read both prior reviews, the implementation report, the SC-013 spec and plan, `SC013_DEMO.md`, `PROJECT_STATE.md`, and the current diff against `HEAD`.
2. Verified the B1 fix and diffed the test against `HEAD` to confirm the original assertions were not weakened.
3. Recomputed the roster panel, title, content, heir, and close rectangles and the six-/seven-row budgets; inspected the capacity test and the generator's `Validate` containment/overlap path.
4. Checked `FormatRow` output for all six demo rows, the Sakon duplicate-label suppression, and the single-parent kinship branch and its test assertions.
5. Verified the updated harness counts in `PROJECT_STATE.md`, `SC013_DEMO.md`, and the implementation report; searched for stale counts.
6. Verified the clean-scene instruction precedes both updaters and traced both `SceneDirty` preflight paths.
7. Traced the cross-scenario save test through `TryApplySave`, `BuildEventRuleContext`, `EventEngine.TryRestoreState`, and `ScenarioMatches`, including the discriminating error text and atomic assignment.
8. Traced the active-decision fixed-heir test through `SaveData` restore validation and `GameSession.SelectSuccessionSuccessor`, including the M2-only message.
9. Scanned non-test source for SC-013 stable-ID literals and confirmed the `SC013EventIds` aliasing.
10. Compared the catalog and localization JSON against `git show HEAD:...` for event/chain equality, added/removed/changed rows, ordering, scenario tags, and content versions.
11. Checked `git status`/`git diff` for `FoundationScene.unity`, `SampleScene.unity`, `Packages`, `ProjectSettings`, untracked scenes, asset types, `.meta` coverage, and GUID uniqueness.
12. Recompiled all eight assemblies and reran both harnesses from the current sources with Unity's bundled Roslyn; inspected zero-diagnostic logs and the 30/30 and 33/33 outputs.

## Evidence limits

- **Static/arithmetic only for the UI.** The roster layout conclusion is rectangle arithmetic plus the generator's static `Validate`; the six- and seven-row rosters, the ending panel, and the title entries have not been rendered or clicked in Play Mode.
- **Host-side only for compilation.** The 8/8 zero-diagnostic build and the 30/30 and 33/33 harness runs were performed by me on the host with Unity's bundled toolchain. They are not Unity Editor compilation, Unity Test Runner, Console, or Play Mode evidence, and the harnesses do not cover the new EditMode tests.
- **No Unity operation.** I did not run batchmode, the Unity Editor, its Test Runner, the scene updaters, `FoundationSceneValidator`, Play Mode, screenshots, or Computer Use; I did not operate or disturb the user's open Editor.
- **Write scope.** This report is the only file I created. I did not edit source, tests, scenes, prefabs, `ProjectSettings`, `Packages`, `Library`/`Temp`, `graphify-out`, `nul`, or any existing report, and I did not commit, merge, tag, or push.
- **Prior-review claims.** I read the prior reviews as documents but did not adopt their conclusions or the implementation worker's internal review statements as my evidence; the checks and reruns above are my own.
