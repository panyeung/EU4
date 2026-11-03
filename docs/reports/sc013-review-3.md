# SC-013 M2 Fixed Three-Generation — Third Read-Only Review

Date: 2026-11-02  
Reviewer: independent read-only OpenCode2 review worker (no source/scene edits)  
Branch: `feature/sc013-fixed-three-generation`  
Worktree: `C:\Users\panye\My project`  
Prior reviews: `docs/reports/sc013-review.md`, `docs/reports/sc013-review-2.md` (both left untouched)  
Implementation report: `docs/reports/sc013-implementation.md`  
Play script: `SC013_DEMO.md`  
State doc: `PROJECT_STATE.md`

## Verdict

**ACCEPT for the source/static correction pass.** No blocking defect was found. Every prior finding and nit is resolved in source, tests, or documentation as requested, the two new regression tests genuinely reach their intended rejection paths, and the SC-011 content contract is unchanged. Three low-severity notes and one residual Play Mode risk remain below; none justify another code round.

This is **not** Unity-test or Play Mode acceptance. Unity Editor compilation, Unity Test Runner/EditMode, Console counts, the `FoundationScene` updaters/validator, Play Mode visuals, and user acceptance remain unrun and are **not** claimed here.

## Disposition of prior findings

| Prior finding | Status | Evidence |
|---|---|---|
| B1 — stale SC-010 time/event test failed under the M2 default | Fixed | `WorldClockTests.cs:409` calls `InitStandardDemo()`; assertions at `:410-426` unchanged |
| 1 — roster content under-sized for six/seven rows | Fixed | `AddSC013FamilyRosterPanel.cs:277-313`; capacity/separation test at `AddSC013FamilyRosterPanelTests.cs:138-162` |
| 2 — `PROJECT_STATE.md` carried the pre-correction harness counts | Fixed | `PROJECT_STATE.md:24,54` now 30/30 and 33/33 |
| 3 — demo row 6 disagreed with the rendered roster | Fixed | `FamilyRosterPanel.cs:138-141`; `SC013_DEMO.md:54` |
| 4 — demo did not tell the player to clean/save the scene | Fixed | `SC013_DEMO.md:19`, ahead of both updaters |
| 5 — no cross-scenario save rejection or active-decision guard test | Fixed | `M2ScenarioSaveTests.cs:28-68`; `M2StoryGateTests.cs:90-121` |
| 6 — duplicated SC-013 stable-ID literals | Fixed | `M2FixedScenario.cs:103-110` aliases `SC013EventIds`; only the prefix test at `:159` |
| 7 — demo said 信正's 父母 while one parent is recorded | Fixed | `SC013_DEMO.md:55`; rendered label now 父亲 (`M2FixedScenario.cs:422-424`) |
| N1 — B1 test comment described the M2 sequence wrongly | Fixed | `WorldClockTests.cs:405-407` now states pledge + rainy-arrival correctly |
| N2 — implementation report undercounted prior findings | Fixed | `docs/reports/sc013-implementation.md:151` says seven |
| N3 — rendered kinship label still said 父母 for 信正 | Fixed | `M2FixedScenario.cs:422-424`; asserted at `FamilyRosterPanelTests.cs:65`, `M2FixedScenarioTests.cs:74` |

## Verification of the requested checks

### 1. B1 — the SC-010 test contract

`WorldClockTests.GameSession_PendingEventBlocksTravelAndAdvancementUntilResolved` explicitly calls `session.InitStandardDemo()` at `WorldClockTests.cs:409`, after `NewGame()`. `InitStandardDemo()` runs the full `InitSession` reset (`GameSession.cs:293-314`), including a fresh clock, relationships, random stream, event runtime, and the `SuccessionSample.CreateStandard()` succession, so the M2 default cannot leak a pending SC-013 event into the test.

The diff against `HEAD` is four added lines (three comment lines plus the call). The original assertions at `:410-426` are byte-for-byte unchanged: one month queues the sample event, day/month/travel are blocked while it is pending, date/location do not move, resolution re-enables the month turn, and the second month ends at `1560-06-01` with `PendingEventId` empty. The comment at `:405-407` now describes the M2 schedule accurately, closing N1.

### 2. Roster layout arithmetic (recomputed independently)

Panel 760 × 620 centered (`AddSC013FamilyRosterPanel.cs:280`), so panel-local y spans −310 … +310.

| Element | y / height | Panel-local span |
|---|---|---|
| Title (`:284-285`) | 280 / 36 | 262 … 298 |
| Content (`:289-292`) | 25 / 420 | −185 … 235 |
| Heir (`:309-310`) | −225 / 34 | −242 … −208 |
| Close (`:312-313`) | −283 / 42 | −304 … −262 |

Gaps: 12 px panel-top→title, 27 px title→content, 23 px content→heir, 20 px heir→close, 6 px close→panel-bottom. No overlap.

Row budget with 52 px rows, 6 px spacing, 8 px top/bottom padding (`:305`, `:386-394`):

- six M2 rows: 6×52 + 5×6 + 16 = **358 px**;
- seven SC-010 rows: 7×52 + 6×6 + 16 = **416 px**.

Both fit the 420 px content; seven rows leave 4 px. Because the layout aligns top and does not force-expand height, the seventh row's bottom lands at panel-local −173, i.e. 35 px above the heir line's top (−208) and 93 px below for the six-row case. The title/content and panel-containment separations are also enforced by `Validate` (`:204-218`, helpers `:510-537`), which the test invokes through `Update`.

`RosterContentFitsSevenStandardRowsWithoutOverlappingControls` (`AddSC013FamilyRosterPanelTests.cs:138-162`) asserts `content.sizeDelta.y >= 416` and the two lower separations; `RosterPanelListsTheFixedSixAndEndingStaysHidden` (`:118-136`) asserts the six-row M2 roster. The seven-row case is arithmetic plus the generator's static `Validate`, not a rendered seven-row layout (see N2).

### 3. Project-state counts

`PROJECT_STATE.md:24` reads “逻辑 harness 30/30、33/33” and `:54` reads “harness 30/30、完整事件链 harness 33/33”. The pre-correction counts no longer appear anywhere except inside the two prior review reports, which are historical records and were left untouched. The other harness figures in the file belong to the SC-012 section, not SC-013.

### 4. Demo roster row 6 and the single-parent wording

`FamilyRosterPanel.FormatRow` (`FamilyRosterPanel.cs:129-145`) prints the generation label once when it equals the role name (`:138-141`). For Sakon, `M2FixedScenario.GenerationLabel` returns `家臣` (`:388`) and `RoleName` is `家臣` (`M2FixedScenario.cs:49`), so the identity line is exactly `家臣 · 佐近（40岁）`, matching `SC013_DEMO.md:54`. The second rendered line is `亲属：无亲属记录`, covered by the demo's next bullet (`:55`). No test asserts this exact string, but `FamilyRosterPanelTests.cs:62` asserts all five tokens.

Kinship: `M2FixedScenario.CreateSuccession` records only Masatsuna → Nobumasa (`:142`), and `BuildKinship` now selects `父亲：` when exactly one parent link targets the person (`:422-424`), keeping `父母：` for two (正綱 has both Harunobu and Chie). `SC013_DEMO.md:55` (“正綱的父母、信正的父亲”) and the rendered text now agree; this also closes N3. Assertions: `FamilyRosterPanelTests.cs:64-65`, `M2FixedScenarioTests.cs:73-74`. Minor cosmetic note: 正綱's line prints the `父母：` label twice (once per link), which the demo does not spell out.

### 5. Save/clean-scene instruction before the updaters

`SC013_DEMO.md:19` now tells the player to save `FoundationScene.unity` or confirm it has no unsaved changes, and explains that both updaters refuse a dirty scene. This precedes both menu steps (3 and 4). The refusal is real: `AddSC013FamilyRosterPanel.CheckScenePreflight` returns `SceneDirty` at `AddSC013FamilyRosterPanel.cs:68-72`; `AddSC013TitleScenarioButtons` has the equivalent at `AddSC013TitleScenarioButtons.cs:56-60`.

### 6. Do the two new tests reach the intended paths?

**Cross-scenario rejection.** `M2ScenarioSaveTests.Load_RejectsSc013PendingEventUnderNonM2Succession` (`:28-68`) advances an M2 session one month and asserts the pledge is pending (`:36-37`), then overwrites the save's succession block with a freshly captured SC-010 standard succession (`:43-44`). The payload is therefore an SC-013 pending event plus a genuinely non-M2 saved succession. `TryApplySave` derives the rule context from the saved succession (`GameSession.cs:595-600`, `:998-1005`), and `EventEngine.TryRestoreState` rejects the pending row through `ScenarioMatches` (`EventEngine.cs:279-281`, `:503-511`) with a message that contains the pledge event ID. The test checks a standard target (`:46-52`) and a crisis target (`:54-60`), asserts both refuse, asserts the target stays non-M2, and asserts the target's pending event remains empty. Because the message is pinned to the SC-013 event ID, the rejection cannot pass for an unrelated validation reason. `TryApplySave` assigns state only after every validation (`GameSession.cs:631-648`), so refusal is atomic.

**M2 fixed-heir guard with an active decision.** `M2StoryGateTests.RejectsNonFixedHeirWhileASuccessionDecisionIsActive` (`:90-121`) restores a save with `phase = AwaitingSuccessionChoice` and cleared pending successor/regent (`:100-103`). That shape is explicitly legal in restore validation (`SaveData.cs:330-338`), and the test asserts the restore succeeded and the phase is active (`:103-105`). `SelectSuccessionSuccessor(tokihime)` then hits the M2 branch at `GameSession.cs:378-391` because the saved family is the fixed SC-013 identity, the phase is not `None`, and no finale is committed; `ExpectedHeir` is Masatsuna, so the call is rejected with the M2-only message. That message contains `固定继承人`, distinguishing it from the generic `SuccessionService` message `该人物不是当前合法继承人。` (`SuccessionService.cs:202-204`). Head and phase are asserted unchanged (`:113-115`). The test therefore exercises the guard, not the generic no-decision branch.

Coverage limits: both targets share the same standard-saved succession (no crisis-saved succession paired with an SC-013 event), and “target unmutated” is checked through the pending-event ID and scenario flag only, not a full snapshot (see N3).

### 7. Single source of stable IDs

`M2FixedScenario.cs:103-110` aliases `SC013EventIds` for the chain, both events, and all four options. The only remaining `event.sc013.` literal in the class is the scenario prefix test at `:159`, which is not a stable ID. A repository scan finds no other SC-013 event/option/chain ID literals outside `EventCatalogData.cs` and the test files that intentionally use sample person IDs. See N1 for the one remaining intra-class duplication.

### 8. SC-011 preservation (independent JSON comparison)

Compared the worktree files against `git show HEAD:...`:

- Catalog `contentVersion`: 2 at both revisions.
- Removed events: none. Added: `event.sc013.inheritance-pledge`, `event.sc013.finale`.
- `event.rainy-arrival`, `event.rainy-arrival.legacy-honored`, `event.rainy-arrival.legacy-deferred`: JSON-equal.
- Chains: `chain.rainy-arrival` JSON-equal; only `chain.sc013.main` added.
- Localization `contentVersion`: 2 at both revisions; 18 → 28 entries; none removed, none changed; the ten added rows are exactly the `event.sc013.*`, `option.sc013.*`, and `summary.sc013.*` text IDs.
- Original event and localization order is preserved as a prefix; the diffs are pure insertions (86 and 50 lines, no deletions).
- Both SC-013 events carry `"scenario": "sc013"`; SC-011 rows remain scenario-neutral.

### 9. Repository hygiene

- `Assets/Scenes/FoundationScene.unity` has no diff against `HEAD` (mtime 15:11, before the correction pass) and contains zero `FamilyRosterRoot`, `FamilyRosterPanel`, `M2EndingPanel`, `SC013StandardSampleButton`, or `SC013CrisisDemoButton` strings.
- No `Packages` change and no new scene. The only untracked `.unity` files are the pre-existing `Assets/1.unity`/`.meta` (2026-10-21), which contain no SC-013 strings.
- Pre-existing WIP is untouched by this task: `SampleScene.unity` (2026-10-20, pre-existing modification), `ProjectSettings/SceneTemplateSettings.json` (2026-10-20, untracked), `graphify-out/graph.json` (07:54), `nul` (10:46). The correction-pass edits cluster at 16:27–16:38 on SC-013-owned files only.
- No SC-013 edit exists under `ProjectSettings`, `Packages`, `Library`, `Temp`, `graphify-out`, or `nul`.

### 10. Host-side static re-run (not Unity evidence)

Using Unity's bundled toolchain (`...\6000.6.6f1\Editor\Data\DotNetSdk\dotnet.exe` and `sdk\8.0.318\Roslyn\bincore\csc.dll`) with the existing `%LOCALAPPDATA%\Temp\opencode\sc013-build\*.rsp` files, I recompiled all eight assemblies in dependency order: Domain, Persistence, Events, Application, Presentation, Editor, PlayerMovement2D, EditModeTests. All exited 0 with **zero diagnostics**.

I then copied the fresh project assemblies into `%LOCALAPPDATA%\Temp\opencode\sc013-harness\`, recompiled both harnesses against them, and re-ran them: fixed-scenario harness **30/30 PASS**, full event-flow harness **33/33 PASS**. These are host-side Roslyn/harness results only; the harnesses exercise scenario logic and the catalog event chain, not the roster layout, the cross-scenario save test, the M2-guard test, or the corrected `WorldClockTests` case.

## New findings

No blocking findings.

**N1 — Unused duplicate text-ID literals inside `SC013EventIds` (low, non-blocking).** `EventCatalogData.cs:170-180` declares ten `*TextId` constants whose strings repeat the event/option IDs at `:160-168`, and none of them is referenced anywhere in source or tests. This mirrors the pre-existing `SampleEventIds` convention (`:130-133`) and is confined to the single owning class, so cross-file drift is not possible today; it is dead duplication only. Optionally alias them to the ID constants or remove them.

**N2 — Roster capacity test hard-codes the layout constants (low, non-blocking).** `AddSC013FamilyRosterPanelTests.cs:148-151` recomputes `7*52 + 6*6 + 8*2` from literals instead of reading the generated `LayoutElement.preferredHeight` and `VerticalLayoutGroup` spacing/padding. If the generator's row metrics change, the test could keep passing against stale numbers while only the content/heir/close separation assertions catch a real overflow. Reading the generated components would make the test self-consistent. The title/content separation is asserted only through `Validate`, not by explicit arithmetic in this test.

**N3 — Scenario-isolation coverage shape (low, non-blocking).** The crisis target in `M2ScenarioSaveTests` loads the same standard-saved succession as the standard target; no case pairs a crisis-saved succession with an SC-013 pending event, and the reverse direction (an SC-010 event under an M2 succession) is also untested. Both are optional extras; the scenario gate is generic. “Target unmutated” is proven for the pending-event ID and M2 flag, not for the full target snapshot (date/head/relationships).

**R1 — 620 px panel versus wide aspect ratios (residual Play Mode risk).** The CanvasScaler uses `ScaleWithScreenSize`, 1280×720, match 0.5 (`FoundationScene.unity:4550-4555`). At 16:9 the panel has 100 px of vertical margin, but wider windows shrink the reference height: roughly 621–624 units at 21:9 (fits by only a few pixels) and about 509 units at 32:9 (panel clipped). This is a visual/Play Mode check at the user's actual Game-view aspect, not a source failure found here.

## Checks performed

1. Re-read both prior reviews, the implementation report, `SC013_DEMO.md`, `PROJECT_STATE.md`, and all changed SC-013 source/test files.
2. Traced the B1 fix through `NewGame` → `InitStandardDemo` → `InitSession`, and diffed the test against `HEAD` to confirm the assertions were not weakened.
3. Recomputed the panel/title/content/heir/close rectangles and the six- and seven-row heights from the generator constants, and inspected the capacity/separation test and the `Validate` overlap path it triggers.
4. Verified the exact `FormatRow` output for Sakon and the `BuildKinship` parent-count branch against the demo’s six rows, kinship bullet, and the two tests that assert those strings.
5. Verified the dirty-scene preflight in both updaters and the demo step that precedes them.
6. Traced both new tests into `TryApplySave`, `SuccessionSaveData.ValidateForSave`, `EventEngine.TryRestoreState`/`ScenarioMatches`, and `GameSession.SelectSuccessionSuccessor`, including the messages that discriminate the intended branches.
7. Scanned all non-test source for SC-013 stable-ID literals and confirmed the `SC013EventIds` alias.
8. Compared the catalog and localization JSON against `git show HEAD:...` for event/chain equality, added/removed/changed rows, ordering, and content versions.
9. Checked `git status`, `git diff HEAD` for the scene and resources, WIP mtimes, SC-013 strings in untracked scenes, and the absence of package/scene/`ProjectSettings` additions.
10. Recompiled the eight assemblies and re-ran both harnesses with Unity's bundled Roslyn/dotnet.

## Evidence limits

- **Unity gates unrun.** I did not run Unity Editor compilation, batchmode, Unity Test Runner/EditMode, the `FoundationScene` updaters, `FoundationSceneValidator`, Play Mode, screenshots, or Computer Use. I did not operate the open Editor.
- **Host-side only.** The 8/8 zero-diagnostic Roslyn build and the 30/30 and 33/33 harness runs were reproduced by me on the host, but they are not Unity compilation or Test Runner evidence. The harness sources do not cover the corrected `WorldClockTests` case, the roster layout test, the cross-scenario save test, or the active-decision guard test; those were judged by source tracing.
- **No runtime visual evidence.** The roster layout conclusion is arithmetic plus the generator's static `Validate`; the six- and seven-row rosters, the ending panel, and the title entries have not been rendered or clicked in Play Mode.
- **Scene integration pending.** `FoundationScene.unity` still contains none of the SC-013 roots, buttons, or panels, so both updaters, the validator, focused EditMode tests, and the user Play Test remain to be done in the open Editor.
- **Write scope.** This report is the only file I created in the repository. Recompilation regenerated artifacts only under `%LOCALAPPDATA%\Temp\opencode\...`; no source, scene, prefab, `ProjectSettings`, `Library`, `Temp`, `graphify-out`, `nul`, or existing documentation was modified.
