# M03 NPC Autonomy and Read-Only Inspector — Implementation Report

**Date:** 2026-11-07 (review-correction update)
**Task:** Approved M03 scope in `docs/superpowers/specs/2026-11-07-m03-npc-autonomy-design.md` and `docs/superpowers/plans/2026-11-07-m03-npc-autonomy.md`.
**Worktree:** `C:\Users\panye\My project`, current `main` only. No branch or worktree created.
**Prior art:** `docs/research/M03-npc-autonomy-prior-art.md`; decision is custom pure-C# rules, no package or copied external code.

## 0. Review-correction round (2026-11-07)

This round addresses every finding from the M03 review on the same task; no new scope was added.

- **P1 — v10 raw migration:** `SaveDataValidator.ValidatePresence` and `SaveDataMigrator.TryMigrate` now gate `familyDoctrine` and `activeEventScenarioId` by their actual introduction version (v9), not by `CurrentVersion`. Valid v10 raw JSON is no longer rejected as carrying future fields, while `npcAutonomy` remains v11-only. New coverage starts from a captured v11 JSON, removes only the `npcAutonomy` root property, sets version 10, and proves raw presence validation plus `TryMigrate(legacy, rawJson)` succeed, initialize default pilot needs, and preserve the complete `dailyActions` snapshot. A malformed v10 payload that still carries `npcAutonomy` is rejected by both paths.
- **P2 — needs-only day refresh:** a successful `AdvanceOneDay`/`AdvanceDays` now publishes `SessionStateChanged` when pilot needs grew, even if no action completed or was replanned. Active pilots also refresh their read-only evaluation with current needs; excluded candidates keep visible integer diagnostic scores while remaining ineligible. The new Inspector regression test proves the open panel updates needs and candidate scores on the next day during an active action.
- **P3 — M02 feedback:** autonomous pilot completions once again produce a readable `LastDailyActionMessage` (pilot name + completion reason), while `LastDailyActionOutcomes` keeps its existing player-issued contract. Player interruption/replan of a pilot keeps the visible interrupted + planned outcome pair and message. Inspector diagnostics remain intact.
- **P4 — review test gaps:** added explicit coverage for Accepted, Deferred, Rejected, and Interrupted outcomes, their `StatusName`, decision-log triggers/reasons, and no-state-mutation checks for rejected and deferred commands.

## 0b. Independent-review correction round 2 (target `12b52f3`, 2026-11-07)

This round changes implementation and tests, not only evidence. It addresses the independent read-only review of `12b52f3`:

1. **Per-day autonomy progression:** `AdvanceDays` and `TryAdvanceToNextMonth` now simulate each elapsed day through the same `AdvanceAutonomyDay` primitive (need growth → action progress → completion reduction → completion/idle replan). The month path still executes exactly one monthly transaction, but commits the settled industry/event state before the final simulated day's planning. Bulk, month, and equivalent one-day paths now agree on needs, active actions, and decision records.
2. **No duplicate blocked decisions:** the completion pass and the idle/DailyCheck pass partition pilots, so a completed pilot whose replan is Deferred/Rejected records exactly one decision in that day, including the month-end pending-event case.
3. **Selection explanation preserved:** active-day refresh no longer overwrites `LatestEvaluation`. The snapshot exposes `LatestEvaluation` (goal selection/rejection rationale) and a pure `CurrentEvaluation` (current needs, scores, and active exclusion); the Inspector renders both with explicit labels.
4. **EligibilityChanged wired:** succession/roster changes and `ConfirmTravel` run an explicit eligibility pass. The formerly unreachable unregistered-pilot rejection path now records a Rejected decision with the eligibility trigger; deliberate travel also advances needs/actions before replanning from the new location.
5. **Current state plus last outcome:** completed/interrupted actions write a session-only `LastOutcome` record; immediate replanning updates current `Status` but leaves the last terminal outcome visible in the Inspector.
6. **Scrollable diagnostics:** the generator now builds `M03NpcAiInspectorScroll` → masked viewport → `VerticalLayoutGroup`/`ContentSizeFitter` content containing both candidate and decision-log text. Validator and tests check the ScrollRect wiring, hierarchy, and serialized references.
7. **Pure reads:** `NpcAutonomySnapshots`, `TryGetNpcAutonomy`, and `NpcAutonomyDecisions` no longer call `Ensure`/`Reconcile`; lifecycle commands own all autonomy mutation. A regression test compares durable save JSON and decision count before/after reads.
8. **Deferred wording:** deferred records now append the next-check timing, distinguishing a pending event (“事件处理后的下一个模拟日”) from other daily retries.
9. **Rules version:** current-version v11 migration rejects a mismatched `rulesVersion` with `UnsupportedVersion` instead of normalizing it; intended v10 migration still upgrades rules version and initializes pilot needs.
10. **Generator migration:** the transactional generator replaces an existing generated M03 subtree in the same Undo group while still refusing foreign or duplicated controls.

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

### Review-correction round changes

- `Assets/Scripts/Persistence/SaveDataValidator.cs` — v9 field-presence gates.
- `Assets/Scripts/Persistence/SaveDataMigrator.cs` — v9 raw-JSON gates for v10 migration.
- `Assets/Scripts/Application/GameSession.cs` — needs-only day notification, active diagnostic refresh, readable autonomous completion message, player feedback preserved.
- `Assets/Scripts/Domain/NpcAutonomy.cs` — excluded candidates keep integer diagnostic scores while never competing for selection.
- `Assets/Tests/EditMode/NpcAutonomySessionTests.cs` — v10 raw migration, v11-only v10 rejection, outcome/status/trigger/reason, no-state-mutation rejected/deferred, player feedback.
- `Assets/Tests/EditMode/NpcAutonomyTests.cs` — excluded-score diagnostics and outcome/status/trigger naming.
- `Assets/Tests/EditMode/NpcAiInspectorPanelTests.cs` — needs-only day refresh regression.

## 3. Unity verification

**Editor:** Unity `6000.6.6f1`, PID `90052`, `-projectPath "C:\Users\panye\My project"` verified before operation. No batchmode was run against this project and no second Editor was started for it.

### Compilation

- Review-correction round final Tundra build: `*** Tundra build success (2.68 seconds), 13 items updated, 1975 evaluated`.
- `error CS` count after that build: `0`.
- The temporary review auto-runner Editor script and its `.meta` were deleted after evidence collection.

### EditMode

Full EditMode run (review-correction round):

- Result: **1260 passed / 0 failed / 0 skipped / 0 inconclusive**, duration `15.804s`, `ResultState: Passed`.
- Summary: `Logs/m03-review-full.txt`; NUnit XML: `Logs/m03-review-full.xml`.
- Focused review run: **126/126 passed**, `ResultState: Passed`, summary `Logs/m03-review-focused.txt`; the focused set was `NpcAutonomyTests`, `NpcAutonomySessionTests`, `NpcAiInspectorPanelTests`, `AddM03NpcAiInspectorTests`, `GameSessionSaveTests`, `SaveDataValidatorTests`, `M02DailyActionTests`, and `M02DailyActionPanelTests`.
- Baseline before this round was 1243/1243; the review round adds 17 passing cases (1260 total).

New review-correction test names (all passed):

- `NpcAutonomySessionTests.V10CapturedJsonWithoutAutonomy_PassesPresenceAndMigratesWithoutDailyActionLoss`
- `NpcAutonomySessionTests.V10JsonWithV11OnlyAutonomyRoot_IsRejectedByPresenceAndMigrator`
- `NpcAutonomySessionTests.DecisionLog_RecordsAcceptedInterruptedAndReplanOutcomesWithReasons`
- `NpcAutonomySessionTests.PlayerReplan_KeepsInterruptedAndPlannedFeedbackVisible`
- `NpcAutonomySessionTests.DeferredDecision_ExplainsPendingEventAndLeavesStateUnchanged`
- `NpcAutonomySessionTests.RejectedDecision_ExplainsUnknownLocationAndOnlyGrowsNeeds`
- `NpcAiInspectorPanelTests.Panel_RefreshesNeedsAndCandidateScoresWhenAnActiveDayGrowsNeeds`
- `NpcAutonomyTests.Evaluate_ExcludedCandidatesKeepDiagnosticScoresButNeverCompete`
- `NpcAutonomyTests.DecisionRecord_ExposesOutcomeStatusAndReason` (4 cases)
- `NpcAutonomyTests.TriggerNames_AreStableChineseLabels` (5 cases)

### Independent re-verification (dispatch re-run, corrected 2026-11-07)

A separate dispatch re-ran the committed M03 code in the same open Editor on 2026-11-07:

- Editor: Unity `6000.6.6f1`, PID `90052`, `-projectPath "C:\Users\panye\My project"` reverified from the process command line; no batchmode and no second Editor for this project.
- Temporary auto-runner script added only to drive the open Editor, then deleted together with its `.meta`; post-cleanup Tundra build succeeded (`*** Tundra build success (2.64 seconds), 10 items updated, 1975 evaluated`) with no `error CS` afterward.
- Full EditMode: **1260 passed / 0 failed / 0 skipped / 0 inconclusive**, duration `15.416s`, `ResultState: Passed`; `Logs/m03-rerun-full.txt`.
- Focused EditMode: **126/126 passed**, duration `1.502s`, `ResultState: Passed`, same focused set listed above; `Logs/m03-rerun-focused.txt`.
- Scene validator: `M03Validate: PASS`, exactly one `M03NpcAiInspectorRoot`, and `RESULT: PASS; Errors: 0` in `Logs/SceneValidationReport.txt`; summary `Logs/m03-rerun-validate.txt`.
- Active scene stayed `Assets/Scenes/FoundationScene.unity`, loaded, not dirty, and not playing during the checks. Console counts were not separately re-measured in this dispatch; the review-correction counts above remain the recorded Console evidence. Play Mode was not run.
- **Evidence correction:** the earlier `Logs/m03-verify-full.txt`/`.xml` pair actually contains only the 126-test focused set, so the previously recorded "full 1260 from m03-verify-full" claim was unsupported and is superseded by `Logs/m03-rerun-full.txt`. The earlier focused result (`Logs/m03-verify-focused.txt`, 126/126) was correct. The implementation, scene, and prior `m03-review-full` 1260/1260 result were not changed by this correction.

### Independent-review correction round 2 (2026-11-07)

- Editor: Unity `6000.6.6f1`, PID `90052`, `-projectPath "C:\Users\panye\My project"` reverified from the process command line before operation; no batchmode and no second Editor for this project.
- Compilation after deleting the temporary auto-runner: `*** Tundra build success (4.74 seconds), 10 items updated, 1975 evaluated`; no `error CS` after that build.
- Focused EditMode: **140 passed / 0 failed / 0 skipped / 0 inconclusive**, `ResultState: Passed`; evidence `Logs/m03-fix-focused.txt`.
- Full EditMode: **1274 passed / 0 failed / 0 skipped / 0 inconclusive**, `ResultState: Passed`; evidence `Logs/m03-fix-full.txt` (1274 = prior 1260 + 14 new review-correction cases).
- Scene validator: `M03Validate: PASS`, exactly one `M03NpcAiInspectorRoot`, `RESULT: PASS; Errors: 0`; evidence `Logs/m03-fix-scene.txt` and `Logs/SceneValidationReport.txt`.
- Console immediately after the full run and scene validation: **2 errors / 25 warnings / 16 logs**. Both errors are the intentional chronicle-append fault-injection logs from passing `FamilyFormationSessionTests` (`Adoption_ChronicleAppendFailure_RollsBackMembershipKinshipAndState` and `Marriage_ChronicleAppendFailure_RollsBackMembershipKinshipAndState`); the warnings come from other expected rollback tests. This report does not claim a zero-error Console during negative-test execution.
- Temporary verification runner `Assets/Tests/EditMode/M03FixAutoVerify.cs` and its `.meta` were deleted after evidence collection; the post-cleanup compile above is the recorded final compile.
- No Play Mode was run by this worker. Play Mode acceptance remains for the lead gate.

### Scene integration

- The transactional generator replaced the existing generated `Canvas/M03NpcAiInspectorRoot` subtree in one Undo group and rebuilt exactly one root, open button, scroll viewport/content, candidate text, and decision-log text; `FoundationScene` was saved clean.
- `FoundationSceneValidator` rerun in the open Editor: `RESULT: PASS; Errors: 0`; evidence `Logs/SceneValidationReport.txt` and `Logs/m03-fix-scene.txt`.
- Scene GUID/`.meta` preserved; only the generated M03 subtree changed.

### Console and runtime boundary

- Before the review test runs, the interactive Console counts were **0 errors / 0 warnings / 6 logs**.
- After the scene validator rerun: **0 errors / 0 warnings / 10 logs** (`Logs/m03-review-validator.txt`).
- After the full EditMode run: **2 errors / 25 warnings / 18 logs**. Both errors are intentional fault-injection logs from passing `FamilyFormationSessionTests` (`Marriage_ChronicleAppendFailure_RollsBackMembershipKinshipAndState` and `Adoption_ChronicleAppendFailure_RollsBackMembershipKinshipAndState`); the warnings come from other expected failure/rollback tests. This report does not claim a zero-error Console during the negative-test run.
- **Play Mode was not run by this worker.** Mochi must run the lead Play Mode gate for both pilots, continued active tasks, interruption/replan explanation, and the read-only Inspector.

## 4. Known limitations / risks

- No Play Mode acceptance yet. Runtime visuals, panel readability, and actual in-scene click behavior remain unverified.
- Autonomous pilot completions are visible through `LastDailyActionMessage`; `GameSession.LastDailyActionOutcomes` intentionally keeps its player-issued contract so existing M02 outcome-list tests stay valid. Player-issued pilot interruption/replan still produces both outcome entries and the readable message.
- Idle pilots are checked on the next simulated day rather than during load, so a stable save can round-trip byte-for-byte. This means a loaded save with an idle pilot waits for the next day/action trigger before planning.
- The `WorldClockTests` month-end test was stale relative to the M02 `AdvanceActionDay` behavior; it was updated in the original implementation to assert the current intended semantics rather than changing the clock panel.
- v11 migration initializes approved default needs; it does not infer historical needs or replay any past day.
- No packages were installed, no external code copied, and no `Library`/`Temp` files were edited.

## 5. Commit state

The first M03 review-correction files were committed to the current `main` worktree as `b7c51ba` (`fix(m03): address autonomy review findings`) and pushed to `origin/main`. This second independent-review correction round commits the verified source, test, scene, and documentation changes on `main`; the exact hash is the current `HEAD` for this report. The unrelated untracked `nul` and `graphify-out/` files were preserved and not staged. No branch, worktree, merge, or tag was created, and this is not a claim of milestone acceptance.
