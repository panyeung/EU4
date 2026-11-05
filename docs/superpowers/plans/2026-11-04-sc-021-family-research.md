# SC-021 家族研究与技艺传承实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic, saveable family research plan that records one completed technique as inheritable family knowledge without granting personal mastery.

**Architecture:** Keep research rules and state in plain C# Domain types. `GameSession.TryAdvanceToNextMonth` plans research after industry settlement and before education/event scheduling, commits all state as one transaction, and restores research/resources/history with the existing rollback path. SaveData stores stable IDs and migrates v7 saves to empty research state; the FoundationScene panel is generated through one Undo transaction and adds a standalone SC-021 demo button to the existing title UI for a separate fixed Kageyama family/scenario (`family.kageyama.sc021`); it is not part of the SC-013 standard/crisis button pair and does not present or change another family's scenario.

**Tech Stack:** Unity 6.6.6f1, C#, existing asmdefs, Unity Test Framework/NUnit EditMode tests, uGUI, `JsonUtility`, existing editor-generator and scene-validator patterns. No package or external code.

**Spec:** `docs/superpowers/specs/2026-11-04-sc-021-family-research-design.md`

## Global Constraints

- Implement code through OpenCode2 on the current task worktree `feature/sc018-independent-core-events`; preserve every pre-existing dirty or untracked path and do not stage unrelated files.
- Use the existing Unity project and its already-open Editor; first verify the Editor PID and `-projectPath`. Never run batchmode against this project folder or open a second Editor for it.
- Keep all rule/state code in pure C# where practical; do not create a new Unity project or Scene, install packages, or copy external code.
- Preserve existing `.meta` files and GUIDs; do not modify `Library` or `Temp`.
- Only the designated integration worker edits `FoundationScene`, prefabs, or `ProjectSettings`. The generator uses one Undo group, validates before saving, reverts through Undo on exceptions, does not reload a scene for rollback, and places all new UI under one fixed root.
- Keep SC-010, SC-013, SC-019, and SC-020 scenario behavior intact. New research does not grant character mastery or consume random streams.
- Use Unity Editor Test Runner and fresh project-specific logs/XML for verification. Report compilation, EditMode totals, Console counts, validator results, Play Mode evidence, and user acceptance separately.
- After each verification gate passes, commit only the verified task files and push them to the feature branch (`panyeung/sc021-scene-integration`). Do not merge to `main` or tag before the user accepts the demo. The user executes Play Test; do not use screenshots or Computer Use to interact with Unity while the user is awake.

## Review Focus

1. Insufficient funds or supplies must pause research without spending either resource or changing progress; test each shortfall independently.
2. A researcher who becomes dead, retired, underage, outside the active family, or assigned to an industry slot must not progress; replacing the researcher keeps progress. Pin these cases in `FamilyResearchStateTests` and `FamilyResearchSessionTests`.
3. A research completion and subsequent exception in monthly scheduling must restore resources, progress, known techniques, date, event runtime, random state, and chronicle; test by injecting failure after research is planned and after event RNG is consumed.
4. Inheritance and save/load preserve family knowledge but never populate a person-mastery collection; test succession and v7-to-v8 migration explicitly.
5. Duplicate technique IDs, duplicate known-knowledge entries, unknown researcher IDs, negative progress, and progress beyond the research requirement must fail save validation without partial load.

---

### Task 1: Family research rules and state

**Files:**
- Create: `Assets/Scripts/Domain/FamilyResearchCatalog.cs`
- Create: `Assets/Scripts/Domain/FamilyResearchState.cs`
- Create: `Assets/Scripts/Domain/FamilyResearchSettlement.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchCatalogTests.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchStateTests.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchSettlementTests.cs`

**Interfaces:**
- Consumes: `PersonId`, `SuccessionState`, `IndustryResources`, `IndustryCalendar.MonthIndex`, and existing family eligibility rules.
- Produces: `FamilyResearchPlan`, `FamilyResearchState`, `FamilyResearchCatalog.SmokeMixingResearchId`, `FamilyResearchCatalog.SmokeMixingTechniqueId`, and immutable `FamilyResearchSettlementResult`.
- Catalog entry: research ID `research.smoke-mixing`, technique ID `technique.smoke-mixing`, display name `烟幕调制术`, required progress `2`, monthly cost `1` fund + `1` supply, monthly progress `1`.

- [ ] **Step 1: Write failing catalog tests** for the stable IDs, one authored technique, required progress `2`, and cost/progress constants `1/1/1`.
- [ ] **Step 2: Run only `FamilyResearchCatalogTests` in the current Editor Test Runner**; confirm the missing catalog members fail to compile or assert.
- [ ] **Step 3: Implement the immutable catalog entry** in `FamilyResearchCatalog.cs`; do not add external packages or ScriptableObject dependencies.
- [ ] **Step 4: Write failing state tests** for empty state, one active project/researcher, progress range `0..1`, switching researchers while preserving progress, completion adding one unique family-knowledge ID, and clone independence.
- [ ] **Step 5: Run only `FamilyResearchStateTests`** and confirm the expected failures.
- [ ] **Step 6: Implement `FamilyResearchState.TryStartOrReplacePlan`, `TryAdvance`, `TryComplete`, `Clone`, and `TryRestore`** with stable-ID validation, a maximum of one active project, and no person-mastery state.
- [ ] **Step 7: Write failing settlement tests** for exact monthly cost/progress, already-known technique, invalid researcher, resource shortfall, repeated month, future settlement gap, and input immutability.
- [ ] **Step 8: Run only `FamilyResearchSettlementTests`** and confirm the expected failures.
- [ ] **Step 9: Implement `FamilyResearchSettlement.Plan(state, resources, succession, closedMonth)`** as a pure deterministic function. Invalid researcher or insufficient resources pauses with no cost/progress; a successful month deducts exactly `1` fund and `1` supply, adds `1` progress, and records completion exactly once at progress `2`.
- [ ] **Step 10: Run all three targeted test fixtures** and confirm every rule passes.

### Task 2: Session commands and atomic monthly settlement

**Files:**
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchSessionTests.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs`

**Interfaces:**
- Consumes: `FamilyResearchState`, `FamilyResearchSettlement`, `IndustryState`, `SuccessionState`, `HistoryLedger`, and the existing month-settlement fault-injection seam.
- Produces: `GameSession.Research`, `GameSession.TrySetResearchPlan(string researchId, PersonId researcherId)`, `GameSession.LastResearchReport`, and research-specific status text.
- Settlement order: industry → research → education → next-month event scheduling → date/state commit.

- [ ] **Step 1: Add failing session tests** for adult/current-family/alive/non-retired eligibility, exclusion of current industry assignees, plan replacement preserving progress, and no advancement while an event/succession decision blocks time.
- [ ] **Step 2: Run `FamilyResearchSessionTests`** and confirm they fail before implementation.
- [ ] **Step 3: Add the research state property and plan command** to `GameSession`; validate all IDs and eligibility before mutation and expose actionable Chinese failure reasons.
- [ ] **Step 4: Add a research-success session test** that starts with enough resources, runs one month, and observes funds/supplies decrease by `1/1` and progress increase by `1`; run it and confirm failure.
- [ ] **Step 5: Integrate the research plan into `TryAdvanceToNextMonth`** after industry output has been planned and before education/event scheduling. Do not consume the event or research random streams.
- [ ] **Step 6: Add a completion test** proving the second successful month records one family-knowledge technique, one research Chronicle entry, and no individual skill/mastery; run it and confirm failure.
- [ ] **Step 7: Add rollback tests** for insufficient-resource no-op and injected post-research/post-event-scheduling exception. Assert exact restoration of date, industry resources/settled index, research plan/progress/knowledge, education, event runtime, random streams, and chronicle; assert retry succeeds once.
- [ ] **Step 8: Run `FamilyResearchSessionTests` and relevant `WorldClockTests`**; all tests pass with no duplicate monthly effects.

### Task 3: Save schema v8 and migration

**Files:**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchSaveTests.cs`
- Test: `Assets/Tests/EditMode/SaveDataValidatorTests.cs`
- Test: `Assets/Tests/EditMode/SaveDataMigratorTests.cs`

**Interfaces:**
- Consumes: `FamilyResearchState` capture/restore and existing v1–v7 migration chain.
- Produces: `FamilyResearchSaveData` with one optional plan, current progress, last settled month index, and sorted unique known technique IDs; bump save format `7→8` and rules version `3→4`.

- [ ] **Step 1: Add failing save round-trip tests** for an in-progress plan, researcher ID, progress, completed family knowledge, and absence of any personal-mastery data.
- [ ] **Step 2: Run `FamilyResearchSaveTests`** and confirm the new state is not persisted.
- [ ] **Step 3: Add the `research` DTO block and capture/restore wiring**; keep it plain serializable data with stable IDs only.
- [ ] **Step 4: Add failing validator tests** for missing v8 block, unknown technique/researcher IDs, duplicate knowledge, negative/over-cap progress, plan/knowledge conflict, and v7 block presence rules; run them and confirm failures.
- [ ] **Step 5: Implement research validation** without partially restoring `GameSession`; preserve `SaveDataPresenceProbe` behavior where absent and explicit-null blocks differ.
- [ ] **Step 6: Add migration tests** for v7→v8 initializing empty plan/knowledge with last-settled month equal to the prior research month, preserving all existing resources, family relationships, industry, education, event state, succession, and chronicle without backfilling.
- [ ] **Step 7: Implement v7→v8 migration** while retaining the v1–v6 migration chain and rejecting incompatible non-null research payloads in versions where the field is not supported.
- [ ] **Step 8: Run `FamilyResearchSaveTests`, `SaveDataValidatorTests`, and `SaveDataMigratorTests`**; verify exact v7 and v8 fixtures.

### Task 4: Chronicle and player-facing research panel

**Files:**
- Modify: `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- Modify: `Assets/Scripts/Domain/HistoryLedger.cs`
- Modify: `Assets/Scripts/Presentation/FamilyChroniclePanel.cs`
- Create: `Assets/Scripts/Application/FamilyResearchView.cs`
- Create: `Assets/Scripts/Presentation/FamilyResearchPanel.cs`
- Create: `Assets/Scripts/Editor/AddSC021FamilyResearch.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scripts/Editor/AddM6TitleScreen.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchViewTests.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC021FamilyResearchTests.cs`
- Test: `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`

**Interfaces:**
- Consumes: `GameSession.TrySetResearchPlan`, `FamilyResearchView.Build`, existing Chronicle and title-screen patterns.
- Produces: one research-open action, one active project/researcher choice, progress/cost/pause/completion feedback, and readable Chronicle row.
- Scene hierarchy: all new UI under one fixed `Canvas/SC021ResearchRoot`; no prefab or new Scene.

- [ ] **Step 1: Add failing chronicle tests** for category `Research`, readable technique/researcher names, progress `1→2`, completion month, and no raw `research.`/`technique.`/`person.` IDs.
- [ ] **Step 2: Run `FamilyChroniclePanelTests`** and confirm the category/rendering failures.
- [ ] **Step 3: Add `ChronicleCategory.Research`, a validated `HistoryLedger.TryAppendResearch(...)`, and Chronicle formatting** with stable, localized IDs.
- [ ] **Step 4: Add failing `FamilyResearchViewTests` and `FamilyResearchPanelTests`** for eligibility, plan progress, shortfall/invalid-worker messages, same-month plan replacement, wiring of every button listener, and exactly one panel refresh after a session command; run them and confirm failure.
- [ ] **Step 5: Implement the application view and panel** as a projection/command adapter; do not place rule calculations in MonoBehaviours and register/unregister listeners symmetrically.
- [ ] **Step 6: Add failing generator/validator tests** for one rooted hierarchy, correct bindings, no overlap with `SuccessionHud`, industry, Chronicle, or roster controls, and Undo rollback on injected validation/save failure; run them and confirm failures.
- [ ] **Step 7: Implement `AddSC021FamilyResearch`** with one Undo group, validate-before-save, Undo rollback only, no scene reload, and fixed-root placement. Extend `FoundationSceneValidator` and add one SC-021 title entry without changing existing entries or scripts.
- [ ] **Step 8: Run targeted Chronicle, view, panel, generator, and title-screen tests**; verify all listeners use real `Button.onClick` callbacks.

### Task 5: SC-021 fixed scenario and handoff documents

**Files:**
- Create: `Assets/Scripts/Application/SC021FamilyResearchScenario.cs`
- Test: `Assets/Tests/EditMode/SC021FamilyResearchScenarioTests.cs`
- Create: `SC021_DEMO.md`
- Modify: `PROJECT_STATE.md`
- Modify: `DECISIONS.md`
- Create: `docs/reports/sc021-implementation.md`
- Modify: `docs/research/SC021-family-knowledge-reuse-research.md` only if the final integration review changes the reuse decision.

**Interfaces:**
- Consumes: new research state/default resources, existing scenario-entry routing, and persistent scenario selection behavior.
- Produces: a repeatable SC-021 entry in the existing title UI that supplies enough resources for two successful research months without changing SC-010/013/019/020 scenario state.

- [ ] **Step 1: Write scenario routing tests** proving the new entry opens SC-021 in the existing project/scene, supplies exactly the two-month research costs, and leaves the existing title entries (新游戏、继续、SC-010 标准样例、SC-010 危机示范、SC-020 子女培养示范、SC-022 婚姻与收养示范) and their deterministic starting snapshots unchanged; run and confirm failure.
- [ ] **Step 2: Implement `SC021FamilyResearchScenario`** using original content and stable IDs; do not create a second Unity project, Scene, or package.
- [ ] **Step 3: Write `SC021_DEMO.md`** with setup, the SC-021 entry, plan assignment/replacement, two month endings, shortfall pause, completion/chronicle, save-load, inheritance retention, stop conditions, and a results checklist. Clearly state Play Test is user-run.
- [ ] **Step 4: Update `PROJECT_STATE.md` and `DECISIONS.md`** with scope, schema/rules version, research result, current verification status, and pending user acceptance; do not mark M3 complete.
- [ ] **Step 5: Write the implementation report template/results** with changed files, Editor/project path, compile/Test Runner/Console/validator outcomes, Play Mode owner, unverified branches, and known issues. Do not infer runtime behavior from static checks.
- [ ] **Step 6: Run `SC021FamilyResearchScenarioTests` and the full EditMode suite in the existing Unity Editor**; export fresh XML and confirm no failed/skipped tests.
- [ ] **Step 7: Run the existing `FoundationSceneValidator` from that Editor**; record the exact fresh report and zero/error totals, or stop and route corrections to OpenCode2.
- [ ] **Step 8: Stop at the user Play Test handoff** with the project on the current feature worktree. Commit and push only the verified task files; ask the user to follow `SC021_DEMO.md`. Only explicit acceptance unlocks merge to `main` and tagging.

## OpenCode2 implementation handoff

OpenCode2 owns all source, tests, and documentation edits described above. Keep this work sequential on the existing task worktree so the dirty SC-018/019/020 implementation is visible and preserved. Before scene work, prove the current Editor process targets this exact project and confirm the Editor is out of Play Mode; only the designated integration worker edits `FoundationScene`. After implementation, submit the same task for QA review per `AGENTS.md`, resolve findings on that task, then obtain an independent OpenCode read-only review. If Orca cannot launch the required agent, stop that handoff and report the exact launcher error rather than changing agent/configuration. Mochi performs final sign-off and presents the user Play Test steps; user acceptance remains a separate gate.
