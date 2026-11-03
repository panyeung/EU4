# SC-004 Monthly Time Implementation Plan

> 最终状态（2026-11-02）：SC-004 与 SC-005–SC-009 已集成；用户确认 Edit Mode 全部通过并接受 Play Test，最新 `TestResults.xml` 为 548/548 passed，OpenCode2 只读审查 ACCEPT。`c29d24a` 已推送到 `main`，`demo-SC004-SC009` 已推送。下面的未勾选项来自原始计划快照，不表示实现未完成。仍待取得的证据：FoundationScene 反馈条仍序列化为旧位置，本次未执行场景更新器或 `FoundationSceneValidator`，编译与 Console 计数也未单独核实。当前状态以 `PROJECT_STATE.md` 为准。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a bounded month-end clock action and a directly testable FoundationScene control.

**Architecture:** `WorldClock` owns the calendar transition; `GameSession` exposes it. The existing clock panel keeps day advancement and gains a separate month-end button that refreshes both displayed dates. A transactional Editor updater adds the control to FoundationScene.

**Tech Stack:** Unity 6000.6.6f1, pure C# Domain, existing uGUI, NUnit EditMode tests, Orca supervised OpenCode implementation and independent review.

**Spec:** `docs/superpowers/specs/2026-11-02-sc-004-monthly-time-design.md`

## Global Constraints

- Do not modify any pre-existing dirty or unrelated files; preserve all `.meta` GUIDs.
- Do not edit `Library`, `Temp`, packages, or `ProjectSettings`.
- Do not run batchmode or launch a second Unity Editor for this project.
- Keep Domain free of Unity APIs; retain `AdvanceOneDay`, `AdvanceDays`, and travel behavior.
- Scene changes must use one fixed generated root, one Undo group, validation before save, and Undo rollback on exceptions.
- No commit, push, merge, tag, or demo acceptance before user Play Mode acceptance.

## Review Focus

1. At a month boundary, next month begins on day 1; test Jan/Feb, leap February, and Dec/year rollover.
2. At the maximum supported year, month advance must fail without mutation; test final month and exact maximum date.
3. Existing day advancement and travel retain current semantics; test their regression paths.
4. Month button invocation updates both visible date labels and does not double-subscribe; test callback lifecycle and refresh.
5. Editor updater must be idempotent and roll back invalid/exceptional updates; test repeat invocation, validation failure, and save path.

---

### Task 1: Month transition and application API

**Files:**
- Modify: `Assets/Scripts/Domain/WorldClock.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs`

**Interfaces:**
- Produces `WorldClock.TryAdvanceToNextMonth()` and `GameSession.TryAdvanceToNextMonth()`, both returning `bool` and leaving state unchanged on failure.

- [x] Add failing tests for first-of-next-month, December/year rollover, leap-year February, and range rejection without mutation.
- [x] Implement the smallest transition preserving all existing day-based APIs.
- [ ] Run these tests in the existing Editor Test Runner; retain evidence separately from compile and Console evidence.

### Task 2: Clock UI control

**Files:**
- Modify: `Assets/Scripts/Presentation/WorldClockPanel.cs`
- Modify: `Assets/Scripts/Presentation/WorldMapController.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs` or an existing focused panel test file.

**Interfaces:**
- Adds an optional serialized month button and an explicit dependency setter; keeps the existing daily button.
- Exposes a narrow public date-refresh entry point from `WorldMapController` for clock changes.

- [x] Add failing tests for successful click, failed click at max date, refresh of both labels, and listener cleanup/re-enable.
- [x] Implement month-button binding and refresh without changing travel behavior.
- [ ] Run focused EditMode tests in the existing Editor.

### Task 3: FoundationScene updater and validation

**Files:**
- Create: `Assets/Scripts/Editor/AddSC004MonthTurnControl.cs` and `.meta` (Unity-generated)
- Modify: existing FoundationScene validator only if needed
- Test: `Assets/Tests/EditMode/AddSC004MonthTurnControlTests.cs` and Unity-generated `.meta`

- [x] Add failing tests for creation, idempotence, correct button binding/label, and invalid-scene rollback.
- [x] Implement a menu-driven transactional updater with one Undo group, one fixed root, validation before save, and rollback by Undo on exceptions without reloading the scene.
- [x] Root-caused the live-Editor console rollback («Clock panel endMonthButton is not bound to the month button»): the logged warning was emitted by `Updater_RollsBackWhenValidationFails`'s intentional fault injection (the accepted M7 updater logs the same class of warning in its twin test), no live menu/batch run of the SC-004 updater appears in any Editor log, and all eight updater tests passed in the 16:57 Editor run. The run's only genuine failure was the boundary sweep in `WorldClockTests` whose expected constants were computed from 2000−1560 = 40 years (488 month turns); the true span is 440 years = 5288 month turns, so the guard fired on still-legal clicks. Constants corrected; domain boundary behavior is correct and unchanged.
- [x] Hardened the flagged BindMonthTurn → SerializedObject path for the live scene's condition: FoundationScene's clock panel YAML predates `endMonthButton`/`worldMapController`, so a managed-only assignment can leave the serialized state that Validate reads and SaveScene writes unbound. The updater now re-applies both references through `SerializedObject.ApplyModifiedPropertiesWithoutUndo()` inside the same Undo group; validate-before-save, rollback, idempotence, and day/travel behavior are unchanged.
- [x] Added `AddSC004MonthTurnControlStaleSceneTests`: runs the never-save updater seam against an additive temporary copy of the real FoundationScene (the exact on-disk layout the live repro used), asserting SerializedObject-readable bindings, preserved day binding, idempotence, and that neither the temp copy nor FoundationScene.unity is written.
- [ ] Run it in the already-open Editor on FoundationScene; inspect the scene diff and validator output.

### Task 4: Independent review and Play Mode handoff

**Files:** no additional source files.

- [ ] Request independent read-only OpenCode review through Orca after implementation verification.
- [ ] Fix and re-review actionable findings on the same task.
- [ ] Compile and run EditMode tests in the already-open Editor; record Console counts separately if accessible.
- [ ] Enter Play Mode in FoundationScene and check the exact path in the spec; report evidence without screenshots.
- [ ] Freeze the worktree for the user's acceptance. Do not merge/tag before that acceptance.
