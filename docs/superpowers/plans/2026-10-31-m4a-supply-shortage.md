# M4a Supply Shortage Mission Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to execute this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a playable single mission from accepting the supply-shortage investigation in Kiyosu through investigating and negotiating in Atsuta, then reporting in Kiyosu for exactly one +10 merit reward.

**Architecture:** Keep a mission-specific state machine in plain C# Domain code, with location-checked use cases coordinated by `GameSession`. Add a small uGUI mission panel under the existing world-map/town hierarchy; use a transactional Editor updater to bind it into `FoundationScene` without direct scene YAML edits.

**Tech Stack:** Unity 6000.6.6f1, C#, existing Domain/Application/Presentation/Editor assemblies, uGUI, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-10-31-m4a-supply-shortage-design.md`

## Global Constraints

- Keep task rules in plain C# and UI in uGUI; do not add a package or generic quest framework.
- Keep `PlayerMovement2D.cs` unchanged; do not edit `SampleScene`, Build Settings, or unrelated `ProjectSettings`.
- Preserve existing `.meta` GUIDs and user changes; all work stays in `feature/m4a-supply-shortage-task`.
- The user’s Editor is open on M3 integration. Before any Unity test, build, or scene integration, verify its loaded project path. Do not launch a second Editor or batchmode. Stop Unity-dependent verification until the user opens this exact M4a worktree.
- Generated scene UI must be beneath one fixed root; scene updates use one Undo group, validate before saving, roll back with Undo on exceptions, and never reload a scene to roll back.
- Require successful compilation, zero Console errors, passing EditMode tests, and a playable Kiyosu→Atsuta→Kiyosu mission before demo handoff. User acceptance is required before the final checkpoint commit, merge, or tag.
- No new assets or packages; use original text and existing content.

## Review Focus

1. Wrong stage or town must reject an action without changing mission state, merit, date, or location. Test every action with wrong stage and wrong location in `SupplyShortageMissionTests` and `GameSessionMissionTests`.
2. Repeated reporting must not award twice. Test the first report adds exactly 10 and all later reports leave merit unchanged.
3. Negotiation must require the existing Atsuta NPC Kuroda Iori after investigation. Test another/unknown character and pre-investigation negotiation are rejected.
4. `InitNewGame` must reset mission stage and merit. Test reset after both an intermediate stage and a claimed reward.
5. Missing/ambiguous scene objects or serialized references must fail validation without partial scene edits. Test updater preflight failures and rollback in `AddM4SupplyShortageMissionPanelTests`.

---

### Task 1: Mission State and GameSession Use Cases

**Files:**
- Create: `Assets/Scripts/Domain/SupplyShortageMission.cs` and `.meta`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Create: `Assets/Tests/EditMode/SupplyShortageMissionTests.cs` and `.meta`
- Create: `Assets/Tests/EditMode/GameSessionMissionTests.cs` and `.meta`

**Interfaces:**
- `SupplyShortageMission.Stage: SupplyShortageStage`
- `SupplyShortageMission.Merit: int`
- `SupplyShortageActionResult` exposes `Succeeded: bool`, `Stage: SupplyShortageStage`, `Merit: int`, and `Feedback: string`.
- `TryAccept(LocationId location)`, `TryInvestigate(LocationId location)`, `TryNegotiate(LocationId location, CharacterId merchantId)`, `TryReport(LocationId location)`: each returns `SupplyShortageActionResult`.
- `GameSession.SupplyShortageStage`, `GameSession.Merit`, `TryAcceptSupplyShortageMission()`, `TryInvestigateSupplyShortage()`, `TryNegotiateSupplyShortage(CharacterId merchantId)`, `TryReportSupplyShortage()` delegate using `CurrentLocation`.

- [ ] Write `NewMission_StartsNotAcceptedWithZeroMerit`, `Accept_SucceedsOnlyInKiyosu`, `Investigate_RequiresAcceptanceInAtsuta`, `Negotiate_RequiresInvestigationAndKurodaInAtsuta`, `Report_RequiresNegotiationAndKiyosu`, and `Report_AwardsTenMeritExactlyOnce`; assert exact stage/merit.
- [ ] Write parameterized `RejectedAction_PreservesMissionAndWorldState` cases for wrong location/stage, non-Kuroda/unknown merchant, and duplicate actions; assert stage, merit, date, and location remain unchanged.
- [ ] Write `InitNewGame_ResetsMissionAndMerit` after intermediate and claimed states, and `MissionActions_DoNotAdvanceClockOrChangeLocation`.
- [ ] Run the focused tests in the M4a Editor once the user has opened this worktree; confirm they fail before implementation.
- [ ] Implement the mission-specific state machine and `GameSession` APIs; the report action transitions to `RewardClaimed` and awards 10 atomically.
- [ ] Run focused then full EditMode tests; report exact Unity Test Runner results.
- [ ] After task review passes, commit this task as `feat(m4a): add supply shortage mission state`.

### Task 2: uGUI Mission Panel and Controller Wiring

**Files:**
- Create: `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and `.meta`
- Modify: `Assets/Scripts/Presentation/WorldMapController.cs`
- Create: `Assets/Tests/EditMode/SupplyShortageMissionPanelTests.cs` and `.meta`

**Interfaces:**
- `SupplyShortageMissionPanel.Bind(GameSession session)` binds the scene session.
- `SupplyShortageMissionPanel.Open()` / `Close()` control visibility; `Refresh()` renders stage, current objective, merit, action availability, and feedback.
- The panel calls only the four `GameSession` mission use cases from Task 1. `WorldMapController` opens it on entering a town and closes it on return to map.

- [ ] Add `Panel_ShowsKiyosuAcceptBeforeAcceptance`, `Panel_ShowsAtsutaInvestigationThenNegotiation`, and `Panel_ShowsKiyosuReportOnlyAfterNegotiation`; assert objective, merit, and action visibility/interactability.
- [ ] Add `Panel_RefreshesAcrossTownAndReturn`, `Panel_ShowsOperationFeedback`, `Panel_DisablesRepeatedReport`, and `Panel_HandlesMissingSessionOrReferences`; assert safe UI behavior and application-level rejection.
- [ ] Implement the panel and controller binding; keep mission logic out of `MonoBehaviour` and keep relationship dialogue behavior unchanged.
- [ ] Run focused and full EditMode suites in the M4a Editor.
- [ ] After task review passes, commit this task as `feat(m4a): add mission progress panel`.

### Task 3: Safe FoundationScene Integration

**Files:**
- Create: `Assets/Scripts/Editor/AddM4SupplyShortageMissionPanel.cs` and `.meta`
- Create: `Assets/Tests/EditMode/AddM4SupplyShortageMissionPanelTests.cs` and `.meta`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scenes/FoundationScene.unity` only through the validated Editor updater.

**Interfaces:**
- Editor menu: `Tools/Sengoku Chronicle/Add M4a Supply Shortage Mission Panel`.
- Public test seam: `AddM4SupplyShortageMissionPanel.Update(Scene scene)` returning a typed result without saving; menu path additionally requires the active scene to be `Assets/Scenes/FoundationScene.unity` and clean.
- The validator checks a single mission panel under the fixed `M2WorldMapRoot/TownMenuRoot` hierarchy and all serialized references.

- [ ] Write `Updater_RefusesWrongOrDirtyScene`, `Updater_RefusesMissingOrDuplicateRoots`, `Updater_BuildsAndBindsMissionPanelUnderTownRoot`, and `Updater_RollsBackOnValidationFailure`; assert no partial object remains on failure.
- [ ] Implement preflight, one Undo group, UI construction/binding, structural validation, rollback, and save only after validation; never modify existing object GUIDs or unknown objects.
- [ ] Update `FoundationSceneValidator` to report missing/duplicate mission UI and unbound references as errors.
- [ ] Run updater and validator from the open M4a Editor; inspect the saved scene and report `SceneValidationReport.txt` result.
- [ ] After static task review passes, commit the Editor updater, validator, and tests as `feat(m4a): add safe mission panel updater`.
- [ ] The lead alone runs the reviewed updater in the open M4a Editor and saves the resulting scene; commit that generated scene as `feat(m4a): integrate mission panel in foundation scene` after Unity validation passes.

### Task 4: Demo Evidence, Review, and Handoff

**Files:**
- Create/update: `M4A_DEMO.md`, `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `ARCHITECTURE.md`, `DECISIONS.md`.

- [ ] Update architecture and decisions with the fixed state machine, one-time +10 merit, no package/reuse decision, and M4a limitations.
- [ ] Run full EditMode tests, check compile and Console separately, then manually play the entire mission from new game through one-time report.
- [ ] Complete same-task QA review with the available review route; record evidence and actionable defects. If formal Kanban tools remain unavailable, say so and use an independent diff review.
- [ ] Prepare `M4A_DEMO.md` with changed files, exact test results, scene-validation results, Play Mode walkthrough, and unresolved limitations; do not claim acceptance.
- [ ] Freeze the M4a demo worktree and ask the user to accept the playable demo. Only after explicit acceptance create the final checkpoint commit, merge, and tag.

