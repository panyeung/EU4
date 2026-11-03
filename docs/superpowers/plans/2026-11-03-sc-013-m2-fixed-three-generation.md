# SC-013 M2 Fixed Three-Generation Script Implementation Plan

> **Implementation status (2026-11-03, OpenCode2 worker):** All code, content, tests and documentation tasks are implemented. Static type-check of the eight assemblies (Domain, Persistence, Events, Application, Presentation, Editor, Player, EditModeTests) reports 0 errors; two text-only logic harnesses pass (30 and 33 checks). `FoundationScene.unity` is intentionally unchanged because the user's Editor is open and this task must not run Unity batchmode, a temporary project copy, GUI or Computer Use. The scene updater and validator results remain pending and must be produced from the existing Editor using the exact steps in `SC013_DEMO.md`. No Play Mode or user acceptance is claimed.

> **For agentic workers:** Implement this plan task-by-task through the supervised OpenCode2 worker on the existing feature worktree. The same worker may own the implementation sequence; a separate OpenCode2 worker performs the read-only review after implementation.

**Goal:** Deliver the approved SC-013 fixed M2 playthrough with two lawful handoffs, visible family identity, inherited relationship consequences, two distinct endings, and save/load continuity.

**Architecture:** Keep SC-010 standard and crisis sample behavior intact. `GameSession` starts the new M2 scenario by default; separate title-screen actions start the SC-010 standard sample and crisis demo. A dedicated scenario identity drives M2-only event order and succession gates; existing succession, event history, relationship, and chronicle services remain the source of truth. Add one family roster panel and an M2 ending summary beneath the existing fixed scene UI root.

**Tech Stack:** Unity 6, C#, existing Domain/Application/Events/Persistence assemblies, uGUI, Unity EditMode Test Runner.

**Spec:** `docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md`

## Global Constraints

- `GDD_v4.0.md` governs game design; this slice covers its M2 three-generation vertical script.
- Stay in `C:\Users\panye\My project`; do not create a Unity project, add a package, edit `ProjectSettings`, or touch `Library`/`Temp`.
- Preserve `.meta` files/GUIDs and all user WIP; stage only SC-013-owned files.
- Create a unique matching `.meta` for every new Unity asset through Unity's asset pipeline or the repo's established metadata pattern; never replace an existing GUID.
- All product code and Unity scene integration are implemented by OpenCode2. One designated integration worker owns `FoundationScene.unity` and its Editor generator/validator changes.
- Preserve accepted SC-011 event IDs, requirements, effects, content version, and save behavior. SC-013 adds its own event data.
- Keep the family ID, person IDs, event IDs, and option IDs stable after implementation; do not add save fields unless existing DTOs demonstrably cannot persist the approved state.
- Before implementation, Scout checks no more than three relevant GitHub projects for reusable fixed-scenario, roster, or event-order patterns. Report URL, license, Unity compatibility, activity, dependencies/security, effort, and reuse decision. Do not install, clone, or copy external code/assets. Build with existing project systems unless a suitable option is approved.
- Use the already open Unity Editor for compilation, EditMode tests, Console inspection, and scene validation. Never use batchmode against its project folder. Do not use screenshots or Computer Use with Unity while the user is awake.
- The user executes the Play Test. Record compilation, EditMode results, Console counts, FoundationScene validator, Play Mode evidence, save/load, and user acceptance separately.
- Do not commit, merge, tag, or push before the user accepts the Play Test. After acceptance, create the SC-013 checkpoint, fast-forward/push `main`, push the accepted demo tag, and remove the temporary feature ref only after ancestry and WIP checks.

## Review Focus

- **Early retirement or skipped events:** M2 retirement stays unavailable until that generation's mandatory event history is committed; pending events always block handoff. Cover in `M2StoryGateTests` and `GameSessionSuccessionTests`.
- **Wrong or skipped heir:** Each M2 head exposes only the approved next direct descendant; the second handoff reaches 信正. Cover in `M2FixedScenarioTests` and succession panel tests.
- **Unreachable cost option:** Both options in the SC-013 cost event are selectable from the intended opening state; existing SC-011 option requirements stay unchanged. Cover in `M2EventFlowTests` and `SC011EventContentTests` regression assertions.
- **Collapsed endings:** `seek-aid` is available only at Iori relationship `>=5`, and the two finale choices render distinct results. Cover in `M2EventFlowTests` and ending panel tests.
- **Save/load replay or reset:** Loading any stable M2 decision point restores pending choice, two-generation progress, family roster, and relationship history without duplicate effects. Cover in `M2ScenarioSaveTests`.

---

### Task 1: GitHub prior-art check (Scout)

**Files:**
- Report: `docs/research/SC013-fixed-scenario-prior-art.md`

**Interfaces:**
- Consumes: the approved SC-013 spec and existing project code patterns.
- Produces: a concise shortlist of at most three repositories, with license/compatibility/maintenance/dependency/effort comparison and a `Reuse`, `Adapt`, or `Build` decision for each.

- [x] Search GitHub for up to three relevant fixed-scenario/event-gating/family-roster projects; prefer Unity 6-compatible primary project documentation.
- [x] Record the report. Do not download repositories, install dependencies, or copy external code/assets.
- [x] Confirm the implementation will build from existing project code unless the user separately approves external reuse.

**Acceptance:** Research report is present and covers the required comparison fields; no package, checkout, or external source code was added.

### Task 2: Fixed M2 family and scenario progression rules

**Files:**
- Create: `Assets/Scripts/Application/M2FixedScenario.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Modify: `Assets/Scripts/Domain/SuccessionState.cs` only if a small scenario-specific read-only view is needed; do not change `SuccessionSample.CreateStandard()` semantics.
- Test: `Assets/Tests/EditMode/M2FixedScenarioTests.cs`
- Test: `Assets/Tests/EditMode/M2StoryGateTests.cs`
- Test: `Assets/Tests/EditMode/SuccessionServiceTests.cs`
- Test: `Assets/Tests/EditMode/GameSessionSuccessionTests.cs`

**Interfaces:**
- Produces `M2FixedScenario.CreateSuccession() -> SuccessionState`, `M2FixedScenario.IsActive(SuccessionState) -> bool`, `M2FixedScenario.ExpectedHeir(SuccessionState) -> PersonId`, and read-only progression queries consumed by `GameSession` and presentation.
- `GameSession.InitNewGame()` uses the M2 factory. Add `GameSession.InitStandardDemo()` for the separate SC-010 sample entry. Existing SC-010 tests/entry points may continue calling `SuccessionSample.CreateStandard()` and `StartCrisisDemo()`.
- `M2FixedScenario.CanBeginRetirement(SuccessionState, EventRuntimeState, out string reason) -> bool` derives M2 progress from stable family ID, committed event history, succession count, and pending-event state. `GameSession.CanBeginSuccessionRetirement` and `SuccessionRetirementBlockReason` expose the authoritative gate/reason; `SuccessionPanel` is not the only enforcement point. After the finale choice is committed, normal succession rules resume.

- [x] Write `M2FixedScenarioTests.CreateSuccession_HasApprovedSixMembersAndDirectLine`; assert stable IDs `family.akechi.sc013`/`person.sc013.*`, six people and roles (晴信 Head, 千恵 Spouse, 正綱/信正/時姫 kin, 佐近 Retainer), date `1560-04-01`, ages 晴信 70/千恵 66/正綱 46/信正 22/時姫 44/佐近 40, direct line 晴信→正綱→信正, and heir 正綱.
- [x] Write `M2StoryGateTests.CannotRetireBeforeRequiredChoicesOrWithPendingEvent` and `AllowsOnlyTheNextFixedHandoff`; assert early/pending attempts fail with a non-empty reason, handoff one targets 正綱, handoff two targets 信正, invalid fixed heirs block handoff with visible feedback instead of falling back to 旁系, and event choices cannot be skipped.
- [x] Extend `SuccessionServiceTests` regressions to assert `CreateStandard()` and `CreateCrisis()` roster IDs/rules and all existing succession behavior remain unchanged.
- [x] Implement the fixed roster and scenario progression queries without adding generic genealogy or heir-editing systems.
- [ ] Run the focused EditMode tests and confirm the exact M2 gates and SC-010 regressions pass.

**Acceptance:** M2 always reaches the intended three direct generations through `SuccessionService`; premature/duplicate handoffs fail with visible feedback and cannot make the script permanently miss its events.

### Task 3: SC-013 event chain, relationship cost, and endings

**Files:**
- Modify: `Assets/Resources/Events/event-catalog-v1.json` (append SC-013-only definitions; keep SC-011 entries/content version unchanged unless the file format requires an additive version decision, documented first).
- Modify: `Assets/Resources/Localization/event-text-zh-v1.json` (new SC-013 text IDs only).
- Test: `Assets/Tests/EditMode/M2EventFlowTests.cs`
- Test: `Assets/Tests/EditMode/SC011EventContentTests.cs`

**Interfaces:**
- Stable IDs are `family.akechi.sc013`; people are `person.sc013.harunobu`, `person.sc013.chie`, `person.sc013.masatsuna`, `person.sc013.nobumasa`, `person.sc013.tokihime`, and `person.sc013.sakon`. Use `event.sc013.inheritance-pledge`, `option.sc013.inheritance-pledge.keep-promise`, `option.sc013.inheritance-pledge.break-promise`, and `event.sc013.finale`; other event/option/chain/summary IDs use the `event.sc013.*`, `option.sc013.*`, `chain.sc013.*`, and `summary.sc013.*` prefixes.
- Opening M2 cost event resolves before the first handoff; the existing SC-011 rainy-arrival event and generation-two legacy variant retain their accepted data and behavior.
- The succession-related first-generation event uses two always-available choices targeting the existing `CharacterId.KurodaIori`: `keep-promise` applies `+5`, `break-promise` applies `-5`. It is the highest-priority first-month M2 event; the unchanged SC-011 rainy-arrival root follows on the next eligible month. Both must be committed before retirement is enabled.
- The third-generation finale is offered after settled handoff two. Its `seek-aid` option requires Iori relationship `>= 5`; `stand-alone` is always available. Each option has distinct ending summary text. A saved pending finale allows both endings to be compared by load/replay.

- [x] Write `M2EventFlowTests.InheritancePledge_OffersBothChoicesAndRecordsExactDeltaOnce`; assert `keep-promise` changes Kuroda Iori `0→5`, `break-promise` changes `0→-5`, both are selectable at opening, and chronicle/history records one choice and one delta.
- [x] Write `M2EventFlowTests.FixedScript_ResolvesPledgeRainyArrivalLegacyThenTwoHandoffs`; assert event order, head IDs 晴信→正綱→信正, and no pending event is dropped at a handoff.
- [x] Write `M2EventFlowTests.Finale_RequiresTwoHandoffsAndGatesAidAtFive`; assert no finale before handoff two, `seek-aid` is available at relationship `5`, unavailable at `4`, and its ending summary differs from always-available `stand-alone`.
- [x] Implement only the SC-013 event rows and localized text using existing condition/effect kinds; keep content version `2` and all accepted SC-011 entries/IDs/requirements/effects stable.
- [ ] Validate the complete catalog/localization references and run focused event/content EditMode tests.

**Acceptance:** The fixed chain is completable in order, records each real relationship change once, produces distinct endings, and the SC-011 contract tests remain green.

### Task 4: Scenario entry, family roster, and succession feedback UI

**Files:**
- Create: `Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Modify: `Assets/Scripts/Presentation/SuccessionPanel.cs`
- Create: `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`
- Create: `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scenes/FoundationScene.unity` via the transactional Editor generator only.
- Test: `Assets/Tests/EditMode/FamilyRosterPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs`
- Test: `Assets/Tests/EditMode/TitleScreenControllerTests.cs`
- Test: `Assets/Tests/EditMode/AddM6TitleScreenTests.cs` only if the existing generator contract needs a regression assertion.

**Interfaces:**
- `FamilyRosterPanel` reads `GameSession.Succession.World.Family.Members`, person/life data, `KinshipGraph`, and `DesignatedHeirId`; it never mutates game state.
- Title screen keeps `StartNewGame()` as M2 and adds independent SC-010 `StartStandardSample()` and `StartCrisisDemo()` entries; the latter initializes standard state and then starts the existing crisis demo.
- `SuccessionPanel` renders `CanBeginSuccessionRetirement` and its reason, and displays the current fixed heir. It retains existing SC-010 crisis controls.
- Both generators follow existing patterns: one Undo group each, validate-before-save, rollback Undo on exception without reloading, and fixed direct-child roots (`FamilyRosterRoot` under Canvas; scenario buttons under the existing title root). Validator checks cardinality, references, layer order, and required visible labels.

- [x] Write `FamilyRosterPanelTests.Refresh_ShowsSixIdentitiesRolesAgesAndKinship`; assert six stable rows, all approved names and exact ages, three generation labels, kinship descriptions, and visible current heir.
- [x] Write `TitleScreenControllerTests.StartsM2StandardAndCrisisEntriesIndependently`; assert new game uses `M2FixedScenario`, standard sample uses `SuccessionSample.CreateStandard()`, and crisis entry produces `SuccessionSample.CreateCrisis()` without residual M2 data.
- [x] Extend `AddSC010SuccessionPanelTests` with early-disabled/visible-reason and post-choice-enabled/heir-label assertions for M2; preserve the existing SC-010 five-button binding assertions.
- [x] Implement the roster/title/panel bindings; implement transactional generators for the two `FoundationScene` UI additions and extend the validator.
- [ ] Run focused EditMode tests, invoke the existing validator in the open Unity Editor, inspect its text report and save the scene only after validation succeeds.

**Acceptance:** The player can identify all six people and the next heir in-game, understands why retirement is blocked/enabled, and can select M2, SC-010 standard sample, or SC-010 crisis demo from the title screen.

### Task 5: Save/load, ending summary, documentation, and Play Test handoff

**Files:**
- Modify: `Assets/Scripts/Presentation/DemoSummaryPanel.cs` or create `Assets/Scripts/Presentation/M2EndingPanel.cs` under the same fixed UI root.
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`.
- Test: `Assets/Tests/EditMode/M2ScenarioSaveTests.cs`
- Test: `Assets/Tests/EditMode/M2EndingPanelTests.cs`
- Create: `SC013_DEMO.md`.
- Update: `PROJECT_STATE.md`, `ARCHITECTURE.md`, `DECISIONS.md`, and the approved SC-013 spec/plan.
- Modify: `docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md` to keep the approval/evidence state current; do not change the approved behavior baseline without returning to the user.

**Interfaces:**
- End state is derived from two settled successions and committed finale history; no terminal save field is added. Play can continue after the ending.
- Existing persistence DTOs remain the source of truth for roster/kinship, pending event, event history, relationship values, and chronicle.
- `SC013_DEMO.md` owns exact user Play Test steps and a text-only results checklist.

- [x] Write `M2ScenarioSaveTests.Load_RestoresEachStableM2DecisionPointWithoutDuplicateEffects`; cover pending first event, SC-011 root/follow-up, pending handoff, pending finale, and post-finale; assert family/kinship, head/heir/count, pending event order, relationship, history, and chronicle equality after reload.
- [x] Write `M2EndingPanelTests.FinalChoices_ShowDistinctSummariesAndAllowFurtherPlay`; assert both summaries render and time advance remains available after the ending.
- [x] Update project docs with changed files, scenario entry, stable IDs, save/load scope, known limits, and evidence fields; note that user Play Test remains pending.
- [x] Write exact Play Test steps: choose M2 new game; verify the six-person roster and heir; save before the first cost decision; choose `break-promise` (-5) to record the cost; choose SC-011 `offer-rest` (+5); perform the visible first handoff; choose the honored-legacy `renew` response (+5); perform the visible second handoff; at the pending finale save, choose `seek-aid`, inspect its ending and chronicle, reload, choose `stand-alone`, and compare the result; report any divergence.
- [x] Review final Git diff/status and confirm user WIP and `SampleScene.unity` were not staged or overwritten.
- [ ] Verify compilation, EditMode totals, Console error/warning counts, FoundationScene validator, and Play Mode evidence in separate fields. Give the text-only handoff to the user and wait for the user's Play Test result.

**Acceptance:** Stable-point save/load preserves the complete M2 script without duplicate effects; docs and manual steps match the shipped UI; Play Test status stays pending until the user runs it.

## Execution / Verification Boundary

Implementation and review use supervised Orca tasks on the existing project workspace, sequentially, with OpenCode2 as writer and a separate OpenCode2 read-only reviewer. The user has already specified this execution method. No Unity screenshot or Computer Use interaction is part of the workflow. The branch remains frozen while the user evaluates the demo. Only after explicit user acceptance may the lead create/push the checkpoint, integrate to `main`, push the accepted tag, and clean the temporary branch after ancestry/WIP checks.
