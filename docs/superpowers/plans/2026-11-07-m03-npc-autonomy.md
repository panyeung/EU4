# M03 NPC Autonomy and Read-Only Inspector Implementation Plan

> **For agentic workers:** OpenCode2 implements this plan task-by-task on the current worktree after user approval. A separate OpenCode reviewer performs read-only review after implementation.

**Goal:** Add deterministic daily goal selection and execution for Masatsuna and Nobumasa, reusing M02 actions, with a read-only Inspector in `FoundationScene`.

**Architecture:** Plain C# domain rules own the two authored profiles, needs, stable Utility scores, and candidate explanations. `GameSession` advances and saves needs alongside existing `DailyActionState`, and plans only when a pilot is idle at a known location. A read-only uGUI panel projects those results; a transactional Editor generator integrates it into the existing scene.

**Tech Stack:** Unity 6.6.6f1, C#, existing Unity uGUI, `JsonUtility`, NUnit EditMode tests, existing FoundationScene validator and Editor generator patterns. No new package.

**Spec:** `docs/superpowers/specs/2026-11-07-m03-npc-autonomy-design.md`

## Global Constraints

- Stay in `C:\Users\panye\My project`; use current `main` worktree, and do not create a project, branch, or worktree.
- OpenCode2 owns all code and scene changes; do not modify `.meta` GUIDs, `Library`, or `Temp`.
- Reuse `GameSession.TryPlanDailyAction`, `DailyActionState`, the existing daily/monthly transaction, and the current scene generator/validator patterns.
- Limit autonomy to `M2FixedScenario.MasatsunaId` and `M2FixedScenario.NobumasaId`; consider only `Rest`, `Training`, and `Work`.
- Before first code change, complete the design's GitHub-first check: up to three Unity 6-compatible precedents, license/maintenance/dependency comparison, and a reuse-vs-build decision. Do not install or copy external code. Scout worker startup previously failed because its alias was unconfigured; do not retry the same failed worker-start request. Report the blocker and use an allowed read-only research route or ask for Scout configuration before implementation.
- Use the already-running project Unity Editor after verifying its `-projectPath`; never run batchmode against the open project.
- Keep compilation, EditMode, validator, Console, Play Mode, and acceptance evidence separate. No final M03 checkpoint, main merge, or tag until explicit user acceptance.

## Review Focus

- A pilot missing from a roster or at an unknown location must not receive a task; show a clear exclusion/rejection reason.
- Existing player-planned actions must not be overwritten or replanned every day; test active and interrupted actions.
- Equal Utility scores and repeated runs must choose in stable order without random or floating-point variation.
- v10 migration must preserve all existing M02 in-flight actions while initializing only valid pilot rows.
- Malformed/missing v11 autonomy rows must be rejected or safely defaulted according to current save validation conventions; no partial mutation on failed restore.

---

### Task 1: Deterministic autonomy rules

**Files:**
- Create: `Assets/Scripts/Domain/NpcAutonomy.cs` and its Unity `.meta` file.
- Create: `Assets/Tests/EditMode/NpcAutonomyTests.cs` and its Unity `.meta` file.

**Interfaces:**
- `NpcNeedValues` stores `Rest`, `Learning`, and `Duty` as validated integers from 0 through 100, with immutable `WithDailyGrowth()` and `WithCompletedAction(DailyActionKind)` results.
- `NpcAutonomyProfile` pairs a stable `PersonId` with fixed integer weights for Rest, Training, and Work.
- `NpcActionCandidate` exposes kind, eligibility, integer score, and a Chinese explanation/exclusion reason.
- `NpcAutonomyRules.Evaluate(PersonId personId, NpcNeedValues needs, bool isRegistered, bool hasKnownLocation, bool hasActiveAction)` returns all three candidates in enum order and a selected candidate only when eligible.
- `NpcAutonomyCommandOutcomeKind` has `Accepted`, `Deferred`, `Rejected`, and `Interrupted`; `NpcAutonomyDecisionRecord` stores date, pilot ID, outcome, action, trigger, and reason for the bounded in-session log.
- `NpcAutonomySnapshot` exposes the pilot's durable needs, fixed profile, current task/status, latest evaluation, and in-session decision log as read-only values.

- [ ] **Step 1: Add domain tests** for profile membership, need bounds, each action matching its need, excluded active/unknown/unregistered pilots, and deterministic score ties.
- [ ] **Step 2: Run the focused EditMode tests** in the open Unity Editor and confirm the new cases fail before implementation.
- [ ] **Step 3: Implement the pure rules** with these initial constants, all covered by tests:
  - Daily growth before action resolution: Rest `+4`, Learning `+3`, Duty `+3`, each capped at 100.
  - Completed action reduction after that day's growth: matching need `-40`, floored at 0.
  - Profiles: Masatsuna weights Rest/Training/Work `1/2/1`; Nobumasa `1/1/2`.
  - Initial needs: Masatsuna `40/75/45`; Nobumasa `45/40/75` (Rest/Learning/Duty).
  - Score: matching need × profile weight. Equal scores break by `DailyActionKind` ascending.
  - Planned durations: Rest `1`, Training `2`, Work `3` days.
  - These constants are proposal details delegated by the approved design and should not become serialized user configuration.
- [ ] **Step 4: Run focused tests again**; assert identical candidate ordering and scores for identical inputs.

### Task 2: GameSession execution and v11 persistence

**Files:**
- Modify: `Assets/Scripts/Application/GameSession.cs` to own pilot need state, initialize pilots only when registered, update needs in the existing successful day-advance path, plan idle pilots through `TryPlanDailyAction`, and notify observers after committed changes.
- Modify: `Assets/Scripts/Persistence/SaveData.cs` to add the v11 autonomy DTO and capture/restore helpers.
- Modify: `SaveDataPresenceProbe` in `Assets/Scripts/Persistence/SaveData.cs` so v11 JSON root-field presence is checked before deserialization defaults can hide an omitted autonomy block.
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs` to migrate v10 saves to v11 without changing `dailyActions`.
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs` to validate v11 IDs, bounds, duplicates, and roster membership; bump `CurrentVersion` from 10 to 11.
- Modify: `Assets/Tests/EditMode/GameSessionSaveTests.cs`, `SaveDataValidatorTests.cs`, and create `Assets/Tests/EditMode/NpcAutonomySessionTests.cs` with Unity `.meta`.

**Interfaces:**
- `GameSession.TryGetNpcAutonomy(PersonId personId, out NpcAutonomySnapshot snapshot)` returns needs, profile, current action/state, latest candidates, and recent decision records.
- `NpcAutonomyStateSaveData.Capture(...)` and `.TryRestore(...)` serialize only durable need values and pilot IDs; recent decision logs are session-only.

- [ ] **Step 1: Add tests** for distinct initial selections, the four command outcome kinds, no override of player-planned actions, one plan per idle/completed pilot, interruption-triggered re-evaluation, no replan on ordinary day while active, and no mutation on rejected planning.
- [ ] **Step 2: Add save tests**: v10 migration initializes registered pilot needs while byte-for-byte logical M02 active-task fields stay the same; v11 roundtrip restores needs plus in-flight tasks once; malformed IDs/ranges/duplicates fail validation.
- [ ] **Step 3: Integrate autonomy in `GameSession`** after existing day/action resolution succeeds; order is daily need growth → action progression → matching need reduction for completed actions → evaluate idle pilots in ordinal `PersonId` order → schedule with the existing public action API. Failed date/month transactions must restore autonomy needs, decisions, and actions together.
- [ ] **Step 4: Capture and restore v11 state** through `GameSession.CaptureSave()` / `TryApplySave(...)` and `SaveDataMigrator`; older saves get default needs only for registered pilots, and an existing action remains authoritative so migration/load never schedules a duplicate.
- [ ] **Step 5: Run focused EditMode suites** (`NpcAutonomySessionTests`, `GameSessionSaveTests`, `SaveDataValidatorTests`, and `M02DailyActionTests`).

### Task 3: Read-only Inspector and scene integration

**Files:**
- Create: `Assets/Scripts/Presentation/NpcAiInspectorPanel.cs` and `.meta`.
- Create: `Assets/Scripts/Editor/AddM03NpcAiInspector.cs` and `.meta` using existing transactional scene-generator conventions.
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs` to require/validate the single M03 root and its bindings.
- Create: `Assets/Tests/EditMode/NpcAiInspectorPanelTests.cs` and `AddM03NpcAiInspectorTests.cs`, with `.meta` files.
- Modify: `Assets/Scenes/FoundationScene.unity` and its existing `.meta` must retain their GUIDs.

**Interfaces:**
- `NpcAiInspectorPanel.Bind(GameSession session)` subscribes to `GameSession.SessionStateChanged` and renders `NpcAutonomySnapshot`; `Open`, `Close`, and `Refresh` only affect view state.
- `AddM03NpcAiInspector.Update(Scene scene)` adds or repairs exactly one `Canvas/M03NpcAiInspectorRoot`; `Validate(Scene scene, out string error)` checks all serialized references.

- [ ] **Step 1: Add tests** proving both pilots render needs/state, all three candidates render score or exclusion reason, logs show trigger/outcome, UI controls do not call schedule/interrupt/replan, and repeated scene integration keeps exactly one root.
- [ ] **Step 2: Implement panel and generator** with a pilot selector, scrollable diagnostics, current-action state and recent-log list; only selector/expand/close controls may mutate presentation state.
- [ ] **Step 3: Integrate into `FoundationScene`** through the Editor generator, then run `FoundationSceneValidator` and focused panel/generator EditMode tests.

### Task 4: Review, Unity verification, and handoff

**Files:**
- Modify: `PROJECT_STATE.md` and add `docs/reports/m03-npc-autonomy-implementation.md` after evidence is collected.
- No final milestone checkpoint, merge, or tag in this task.

- [x] **Step 1: Complete the pre-implementation GitHub report** in `docs/research/M03-npc-autonomy-prior-art.md` (three precedents, URLs, licenses, Unity compatibility, activity, dependencies, integration effort, reuse/adapt/build choice). Decision: build on existing code; no package or external code. No `DECISIONS.md` package entry is needed.
- [ ] **Step 2: Request sequential read-only OpenCode review** after OpenCode2 completes; address actionable defects on the same task and re-review.
- [ ] **Step 3: Verify in the already-running Unity Editor** after confirming its project path: compile, focused EditMode tests, full EditMode tests, scene validator, Console counts, then Play Mode in `FoundationScene` for both distinct action paths, continued action, interruption/replan explanation, and read-only Inspector.
- [ ] **Step 4: Delete temporary Computer Use screenshots immediately after inspection** and remove any remaining screenshots from the dedicated temporary cache.
- [ ] **Step 5: Record exact files, tests/results, Editor PID/project path, Console counts, validator result, Play Mode path/evidence, and remaining M01/M02 acceptance status**; wait for explicit M03 acceptance before final milestone checkpoint, main merge, or tag.
