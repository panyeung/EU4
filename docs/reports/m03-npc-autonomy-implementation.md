# M03 NPC Autonomy and Read-Only Inspector — Implementation Report

**Date:** 2026-11-07
**Task:** Approved M03 scope in `docs/superpowers/specs/2026-11-07-m03-npc-autonomy-design.md` and `docs/superpowers/plans/2026-11-07-m03-npc-autonomy.md`.
**Worktree:** `C:\Users\panye\My project`, current `main` only. No branch or worktree created.
**Prior art:** `docs/research/M03-npc-autonomy-prior-art.md`; decision is custom pure-C# rules, no package or copied external code.

## 1. Delivered scope

- Two fixed pilots only: `person.sc013.masatsuna` and `person.sc013.nobumasa`.
- Three candidates only: Rest, Training, Work. Travel is outside M03.
- Integer Utility score = matching need × fixed profile weight; equal scores break by `DailyActionKind` ascending (Rest, Training, Work).
- Approved constants:
  - Daily growth: Rest `+4`, Learning `+3`, Duty `+3`, capped at 100.
  - Completed action reduction: matching need `-40`, floored at 0.
  - Masatsuna weights Rest/Training/Work `1/2/1`; initial needs `40/75/45`.
  - Nobumasa weights `1/1/2`; initial needs `45/40/75`.
  - Durations: Rest `1`, Training `2`, Work `3` days.
- Durable needs persist; candidate evaluations, status and the bounded decision log are session-only.
- `GameSession` plans only idle, registered, known-location pilots; active tasks are never re-evaluated or replaced. New game initializes the two pilots; completion, player interruption, and the next simulated-day check trigger re-evaluation. A pending event or other short-term block records `Deferred`; an unknown location or unregistered pilot records `Rejected` without changing existing actions.
- Read-only Inspector projects needs, fixed preferences, execution status, current task/remaining days, all three candidate scores or exclusion reasons, and recent decisions with trigger/outcome. Its only controls select a pilot, open/close, and refresh the view; it never calls a scheduling, interruption or replanning API.
- v11 persistence adds `npcAutonomy` with only the two pilot need rows. v10 migration initializes default needs for registered pilots and preserves every `dailyActions` field. Validation rejects unknown, duplicate, out-of-order, out-of-range, non-roster, or missing pilot rows.
- Save/load never schedules on load. Existing in-flight M02 actions remain authoritative; idle pilots are checked on the next simulated day.

## 2. Changed files

### New implementation

- `Assets/Scripts/Domain/NpcAutonomy.cs` (+ `.meta`)
- `Assets/Scripts/Presentation/NpcAiInspectorPanel.cs` (+ `.meta`)
- `Assets/Scripts/Editor/AddM03NpcAiInspector.cs` (+ `.meta`)

### Modified implementation

- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Persistence/SaveData.cs`
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- `Assets/Scripts/Persistence/SaveDataValidator.cs`
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Scenes/FoundationScene.unity`

### New tests

- `Assets/Tests/EditMode/NpcAutonomyTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/NpcAutonomySessionTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/NpcAiInspectorPanelTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/AddM03NpcAiInspectorTests.cs` (+ `.meta`)

### Modified tests

- `Assets/Tests/EditMode/GameSessionSaveTests.cs` — v11 capture/round-trip coverage.
- `Assets/Tests/EditMode/SaveDataValidatorTests.cs` — missing `npcAutonomy` rejection.
- `Assets/Tests/EditMode/M02DailyActionPanelTests.cs` — pilot rows start with an autonomous action instead of “空闲”.
- `Assets/Tests/EditMode/M2ScenarioSaveTests.cs` — non-M2 fixture also resets the new `npcAutonomy` block.
- `Assets/Tests/EditMode/FamilyDoctrineSaveTests.cs` — raw v8 fixture strips the v11 block.
- `Assets/Tests/EditMode/WorldClockTests.cs` — aligned the stale SC-004 month-end test with the M02 `AdvanceActionDay` behavior: raw `AdvanceOneDay` still refuses to cross the month, while the explicit day control settles it exactly once.

### Documentation

- `docs/reports/m03-npc-autonomy-implementation.md` (this report)
- `PROJECT_STATE.md`

## 3. Unity verification

**Editor:** Unity `6000.6.6f1`, PID `90052`, `-projectPath "C:\Users\panye\My project"` verified before operation. No batchmode was run against this project and no second Editor was started for it.

### Compilation

- Final Tundra build after cleanup: `*** Tundra build success (4.85 seconds), 14 items updated, 1975 evaluated`.
- `error CS` count after that build: `0`.
- The temporary test-runner/scene-integrator Editor scripts and their `.meta` files were deleted; `SengokuChronicle.Editor.asmdef` was restored to its original empty `precompiledReferences`.

### EditMode

Full EditMode run:

- Result: **1243 passed / 0 failed / 0 skipped / 0 inconclusive**, duration `23.086s`, `ResultState: Passed`.
- Summary: `Logs/m03-full-editmode.txt`; NUnit XML: `Logs/m03-full-editmode.xml`.
- M03 focused run before the full run: **38/38 passed**, summary `Logs/m03-auto-tests.txt`.

New M03 test names (all passed):

- `AddM03NpcAiInspectorTests.Preflight_RejectsWrongSceneDirtyAndPlaying`
- `AddM03NpcAiInspectorTests.Update_CreatesOneValidRootAndSecondRunIsIdempotent`
- `AddM03NpcAiInspectorTests.Update_FaultInjectionRollsBackTheWholeUndoGroup`
- `AddM03NpcAiInspectorTests.Update_RejectsMissingGameSession`
- `NpcAiInspectorPanelTests.Generator_FaultInjectionRollsBackWithoutPartialControls`
- `NpcAiInspectorPanelTests.Generator_ProducesValidIdempotentSceneWithOneRoot`
- `NpcAiInspectorPanelTests.Panel_ReadOnlyControlsDoNotMutateSessionState`
- `NpcAiInspectorPanelTests.Panel_RendersBothPilotsNeedsCandidatesAndState`
- `NpcAiInspectorPanelTests.Panel_RendersExclusionReasonForAnUnknownLocationPilot`
- `NpcAiInspectorPanelTests.Panel_ShowsRecentDecisionOutcomeAndTrigger`
- `NpcAutonomySessionTests.CompletedAction_ReplansExactlyOnceAndReducesTheMatchingNeed`
- `NpcAutonomySessionTests.NewGame_PlansDistinctApprovedActionsForBothPilots`
- `NpcAutonomySessionTests.OrdinaryDay_DoesNotReplanAnActivePilot`
- `NpcAutonomySessionTests.PendingEvent_DefersInterruptionReplanWithoutAnAction`
- `NpcAutonomySessionTests.PlayerInterrupt_ReplansImmediatelyAndLogsTheInterruption`
- `NpcAutonomySessionTests.PlayerReplannedAction_IsNotOverwrittenByTheNextDay`
- `NpcAutonomySessionTests.SaveLoad_RestoresNeedsAndInFlightActionsExactlyOnce`
- `NpcAutonomySessionTests.UnknownLocation_RejectsPlanningAndKeepsOtherActionsUnchanged`
- `NpcAutonomySessionTests.V10Migration_InitializesPilotNeedsAndPreservesDailyActions`
- `NpcAutonomySessionTests.V11Presence_RejectsMissingAutonomyRootProperty`
- `NpcAutonomySessionTests.V11Validation_RejectsBadPilotRowsAndMissingCoverage`
- `NpcAutonomyTests.DecisionLog_IsBoundedAndReturnedNewestFirst`
- `NpcAutonomyTests.Evaluate_EachActionMatchesItsOwnNeed` (3 cases)
- `NpcAutonomyTests.Evaluate_ExcludesUnregisteredUnknownLocationAndActivePilots`
- `NpcAutonomyTests.Evaluate_IsIdenticalForIdenticalInputs`
- `NpcAutonomyTests.Evaluate_TieBreaksByActionKindAscending`
- `NpcAutonomyTests.NeedValues_GrowClampAndReduceTheMatchingNeedOnly`
- `NpcAutonomyTests.NeedValues_RejectOutOfRangeInput` (3 cases)
- `NpcAutonomyTests.PlannedDuration_IsApproved` (3 cases)
- `NpcAutonomyTests.Profiles_AreTheTwoApprovedFixedPilots`
- `NpcAutonomyTests.Profiles_UseApprovedWeightsAndInitialNeeds`
- `NpcAutonomyTests.State_KeepsOnlyTheTwoPilotRowsInOrdinalOrder`

### Scene integration

- The existing transactional generator created exactly one `Canvas/M03NpcAiInspectorRoot` and one `M03NpcAiInspectorOpenButton`, then saved `FoundationScene`.
- Save result: `Dirty=False; roots=1; opens=1`.
- `FoundationSceneValidator`: `RESULT: PASS; Errors: 0`; report updated at `Logs/SceneValidationReport.txt`.
- Scene GUID/`.meta` preserved.
- Scene diff inspection: all pre-existing `--- !u!` fileID anchors remain; the diff only adds M03 objects/components (71 new anchors for the generated hierarchy/components) plus file-order churn. No original object or scene `.meta` GUID was removed.

### Console and runtime boundary

- `Logs/Editor.log` after the final build has `0` `error CS`.
- The interactive Unity Console count was **not** separately read, and Unity AI Assistant/licensing trace noise is present in `Logs/Editor.log`; this report does not claim an interactive Console zero count.
- **Play Mode was not run by this worker.** Mochi must run the lead Play Mode gate for both pilots, continued active tasks, interruption/replan explanation, and the read-only Inspector.

## 4. Known limitations / risks

- No Play Mode acceptance yet. Runtime visuals, panel readability, and actual in-scene click behavior remain unverified.
- The shared completion channel (`GameSession.LastDailyActionOutcomes`) intentionally excludes autonomous pilot actions so existing M02 player-action tests keep their semantics; autonomous outcomes are visible through `NpcAutonomySnapshot.RecentDecisions` and the Inspector.
- Idle pilots are checked on the next simulated day rather than during load, so a stable save can round-trip byte-for-byte. This means a loaded save with an idle pilot waits for the next day/action trigger before planning.
- The `WorldClockTests` month-end test was stale relative to the M02 `AdvanceActionDay` behavior; it was updated to assert the current intended semantics rather than changing the clock panel.
- v11 migration initializes approved default needs; it does not infer historical needs or replay any past day.
- No packages were installed, no external code copied, and no `Library`/`Temp` files were edited.

## 5. Commit state

The verified M03 files were committed to the current `main` worktree and pushed to `origin/main` as the M03 implementation checkpoint. The unrelated untracked `nul` file was preserved and not staged. No branch, worktree, merge, or tag was created, and this is not a claim of milestone acceptance.
