# SC-010 Initial Head Retirement and Succession

## Goal

Produce a directly playable FoundationScene demo in which the player can retire the first family head, choose a lawful successor, transfer control without replacing permanent person/family identity, and save/load both the pending decision and completed handoff.

## Authority and scope

- `GDD_v4.0.md` is authoritative, especially chapters 3, 4, 11, and 19.2-19.4.
- Reuse the existing SC-003 value types and validator patterns. SC-010 cannot compile without them, so this accepted feature's checkpoint package includes `StableIds.cs`, `PersonState.cs`, `FamilyState.cs`, `KinshipGraph.cs`, `InterpersonalState.cs`, `FamilyRelationshipValidator.cs`, and their tests/metas. Do not rewrite or replace those files; preserve their current contents and GUIDs.
- Do not add packages, copy external code, change providers, or edit `Library`/`Temp`.
- Implementation owns C# scripts, data assets if needed, `.meta` files, EditMode tests, the SC-010 Editor updater/validator, and SC-010 demo instructions. It must not directly edit scenes, prefabs, or ProjectSettings. The lead will run the updater in the already open Editor after review.
- Preserve all pre-existing dirty docs and user WIP. Do not stage or commit.
- Do not run another Unity instance or batchmode. Use the existing project Editor for compilation and tests after the read-only review.

## User-visible slice

1. The FoundationScene shows the current family head and a `退休并安排继承` action.
2. Retirement enters a stable `AwaitingSuccessionChoice` state. The panel lists only living, qualified family successors and explains why an invalid designated heir needs replacement.
3. Confirming an eligible adult transfers the head role and player control exactly once. The same family/person IDs, campaign date, location, mission results, and personal relationship records remain intact.
4. If the chosen heir is a minor, the player must select an eligible adult regent before transfer. At the configured adulthood threshold the panel reports the handoff and transfers control without repeating the current month's action budget.
5. If no successor is eligible, provide one explicit crisis adoption/rebuild decision with visible condition/cost. Accepting creates a valid family member and proceeds; declining ends the family as a terminal gameplay state: no time or travel advance and no new succession action remain, a persistent modal explains the extinction, and the map HUD's save/load stays available so an earlier checkpoint can be loaded intentionally. No branch may wait or loop indefinitely.
6. Saving at a pending decision and after transfer restores the same phase, candidates/head, and one-time result markers. Existing v2 saves migrate explicitly; malformed succession data is rejected atomically.

## Proposed ownership

- New pure domain types: `Assets/Scripts/Domain/SuccessionService.cs`, `SuccessionState.cs`, and any narrowly required succession DTO/value types.
- Application/persistence integration: `Assets/Scripts/Application/GameSession.cs` and `Assets/Scripts/Persistence/SaveData.cs`, `SaveDataMigrator.cs`, `SaveDataValidator.cs`, plus required tests. Reuse stable pause/commit patterns in Events; only modify Event files if the state cannot be represented safely through the session API.
- Presentation: new `Assets/Scripts/Presentation/SuccessionPanel.cs` and a single-root Editor updater `Assets/Scripts/Editor/AddSC010SuccessionPanel.cs`; extend `FoundationSceneValidator.cs` only for the new bindings/layout checks.
- EditMode coverage in new `Assets/Tests/EditMode/SuccessionServiceTests.cs`, `GameSessionSuccessionTests.cs`, `SuccessionSaveTests.cs`, and `AddSC010SuccessionPanelTests.cs` as appropriate. Every new Unity asset gets its generated `.meta`; preserve existing GUIDs.
- Create a focused `SC010_DEMO.md` with entry instructions and expected behavior. Do not edit the already-dirty `DEMO.md` or other direction-refresh documents.

## Implementation sequence and acceptance

1. Add failing pure C# tests for lawful eligibility, invalid designated heir fallback, atomic/idempotent adult handoff, minor/regent handoff, no-candidate crisis accept/decline, and stable pending/completed states.
2. Implement a deterministic succession service over SC-003 identities/roster/kinship, with explicit result codes and no hidden random outcomes.
3. Integrate retirement and state-change notification into `GameSession` without resetting the existing campaign; add tests proving unrelated campaign and relationship state survives.
4. Extend versioned JSON save/load with a new schema version, migration from current v2, validation, and pending/completed succession round trips.
5. Build the SC-010 panel/updater with one Undo group, validation before save, rollback on failure, no scene reload, and a single fixed root. Add Editor tests for duplicate invocation, bindings, containment, and rollback.
6. Request a separate read-only OpenCode2 review after implementation. Resolve findings in the implementation task, then create the dependent lead sign-off task.
7. With the existing Unity Editor only, record compilation, EditMode results, Console error/warning counts, scene validation, Play Mode evidence, and save/load evidence separately. Do not claim user acceptance until the user completes the detailed text-only Play Test.

## Play Test finish line

The user can follow a numbered, text-only procedure with expected results at every step, including adult succession, invalid-heir fallback, minor/regent handling, no-candidate crisis resolution or terminal extinction, and save/load while a succession decision is pending. The test record must say which steps passed/failed and capture visible text/results without screenshots.

## Correction round after two read-only reviews

- Wire the five static HUD/modal buttons (retire, crisis demo, confirm, accept, decline) exactly once and unbind on disable.
- Place SuccessionRoot above M2WorldMapRoot but below the title and event-choice overlays; validate that order and the raycast targets that previously lost clicks.
- Honor a valid designated heir immediately, including minor heirs entering regent choice, and clear the previous reign's designation on every transfer.
- Route a minor heir with no eligible adult spouse/kin/retainer into the one-shot crisis decision instead of a deadlocked regent screen; restrict regent roles to GDD 4.3/19.2.
- Pause time advancement when a minor head reaches adulthood and require an explicit player handoff; expose the action through the confirm button and `MonthTurnOutcome.PendingSuccession`.
- Make crisis adoption/head transfer/allocation all-or-nothing, and reject saves whose current head is retired.
- Add regression tests for each correction plus crisis pending/accepted/extinct save/load, and update `SC010_DEMO.md` for the title-screen entry and actual controls.

## Residual correction round (accepted review follow-up)

Scope: only the three residual SC-010 findings; no scene/prefab/ProjectSettings edits and no
Unity execution. The lead still runs compilation, EditMode, scene validation, Console
inspection, and Play Mode in the already-open Editor.

- Day advance now reports the succession pause through the existing
  `MonthTurnOutcome.PendingSuccession` + `LastEventMessage` surfaces instead of returning
  `false` silently. Pending decisions and a due adult handoff produce different, actionable
  reasons. `AdvanceDays` uses the same reporting path.
- `WorldClockPanel.TryAdvanceOneDay` no longer logs "date limit" for a succession pause; it
  publishes `LastAdvanceMessage` from the session outcome. The SC-010 modal is drawn over
  the centered clock panel, so `推进一天`/`结束月份` are covered while the decision is
  open; `SC010_DEMO.md` Play Test C now states that and names `完成成年交权` as the
  required action.
- The regency HUD shows a truthful `受限决策` line naming the actions that are actually
  disabled (`退休并安排继承`, `示范：无继承人危机`) and keeps the explicit adult-handoff
  rule visible. The stale domain message that said the handoff happens "自动" now says it
  pauses and needs `完成成年交权`.
- Regression tests: `GameSessionSuccessionTests.AdvanceOneDay_WhileSuccessionChoicePending_ReportsPendingSuccessionWithoutAdvancing`,
  `..._WhileAdultHandoffAwaitsConfirmation_ReportsHandoffPauseWithoutAdvancing`,
  `SC010TimePauseTests.TryAdvanceOneDay_WhileSuccessionChoicePending_ReportsSuccessionReason`,
  `..._WhileAdultHandoffAwaitsConfirmation_ReportsHandoffAction`,
  `AddSC010SuccessionPanelTests.PanelRegencyShowsRestrictedDecisionAndDisablesRestartActions`,
  `..._PanelAdultHandoffSurfacesPauseReasonAndRequiredAction`, plus the regency-message
  assertion in `SuccessionServiceTests.MinorSuccessor_RequiresEligibleAdultRegentAndCompletesAtThreshold`.

## Terminal extinction correction (Play Test SC-010 findings)

Scope: make the visible family-extinction result an actual terminal gameplay state and keep
the valid post-handoff retirement action distinct from a repeat confirm. No scene, prefab,
ProjectSettings, package, or unrelated-doc edits; no Unity execution by the implementer.

- `GameSession.SuccessionBlocksTimeAdvance` now treats `SuccessionPhase.Extinct` as blocking,
  and day/multi-day/month requests check it before any other pending work. An extinct family
  keeps its date, location, mission state and records unchanged; the refusal reuses
  `MonthTurnOutcome.PendingSuccession` with an extinction-specific `LastEventMessage` that
  names loading an earlier save as the recovery path.
- `PreviewTravel` and `ConfirmTravel` reject an extinct family, so a stale preview cannot move
  the campaign. `WorldClockPanel` disables `推进一天`/`结束月份` and `WorldMapController`
  disables travel confirmation, cancellation and location selection by subscribing to
  `SessionStateChanged`; both re-enable when an earlier checkpoint is loaded.
- `SuccessionPanel.Refresh` treats `Extinct` as a terminal modal state: the modal stays visible,
  the extinction status and recovery text are shown, candidate/regent lists and all crisis or
  confirm buttons are hidden, and `退休并安排继承` / `示范：无继承人危机` stay disabled.
  The map HUD's existing `存档`/`读档` controls are intentionally left available.
- After a completed adult handoff the phase is `None`, so `退休并安排继承` remains enabled. That
  is intentional: it starts a *new* succession cycle for the current head; it never repeats the
  completed handoff. `SC010_DEMO.md` Play Test A step 5 now states this, and
  `AddSC010SuccessionPanelTests.PanelAdultConfirmTransfersHead` asserts that the button stays
  interactable and that using it enters `AwaitingSuccessionChoice` again.
- Regression tests:
  `GameSessionSuccessionTests.ExtinctFamily_RejectsEveryTimeAndTravelMutationAndPreservesState`,
  `GameSessionSuccessionTests.ExtinctFamily_CanStillLoadAnEarlierCheckpoint`,
  `SC010TimePauseTests.ExtinctFamily_DisablesClockControlsAndReenablesThemAfterCheckpointLoad`,
  `SC010TimePauseTests.ExtinctFamily_DisablesMapTravelAndLocationControls`,
  `AddSC010SuccessionPanelTests.PanelExtinctionStaysVisibleAsTerminalStatusWithoutSuccessionActions`.
  The lead runs them in the already-open Editor; the implementer does not claim they passed.

### Unresolved GDD decision

GDD 4.3 lists possible regency content (power struggle, influencing the minor, changed
family policy, a regent refusing to hand over) but says v1 only needs "指定摄政、受限决策
与成年交权"; GDD 19.2 requires showing restricted decisions during a regency without
defining which decisions. The prototype has no regency-specific decision content, so the
HUD only reports the restrictions that actually exist (retirement and crisis demo are
disabled, time pauses at adulthood). Defining a new regency-only decision, its cost, and its
visible choices needs an explicit design decision; it is left unimplemented rather than
invented here.

## Accepted checkpoint package (post-acceptance)

The user accepted SC-010 and requested a future GitHub checkpoint. The previous
read-only-review rule that kept the SC-003 model unstaged is superseded: the SC-003
files are now part of this package because the SC-010 source and tests reference them
directly. Include their existing content and `.meta` files without rewriting them.

Smallest source package:

- SC-003 dependency: `Assets/Scripts/Domain/StableIds.cs`, `PersonState.cs`,
  `FamilyState.cs`, `KinshipGraph.cs`, `InterpersonalState.cs`,
  `FamilyRelationshipValidator.cs`, plus `PersonFamilyIdentityTests.cs`,
  `KinshipInterpersonalTests.cs`, `FamilyRelationshipValidatorTests.cs`, and all metas.
- SC-010 domain: `SuccessionState.cs`, `SuccessionService.cs` and metas.
- SC-010 application/persistence/presentation: modified `GameSession.cs`,
  `SaveData.cs`, `SaveDataMigrator.cs`, `SaveDataValidator.cs`, `SaveSlotStore.cs`,
  `WorldClockPanel.cs`, `WorldMapController.cs`, new `SuccessionPanel.cs`, modified
  `SaveDataValidatorTests.cs`, and new `SuccessionServiceTests.cs`,
  `GameSessionSuccessionTests.cs`, `SuccessionSaveTests.cs`,
  `AddSC010SuccessionPanelTests.cs`, and `SC010TimePauseTests.cs` with their metas.
- SC-010 Editor integration: `AddSC010SuccessionPanel.cs`,
  `FoundationSceneValidator.cs`, and the existing dirty
  `Assets/Scenes/FoundationScene.unity` wiring. Scene changes remain lead-owned and
  must be included as-is when the checkpoint commit is prepared.
- Documentation: `SC010_DEMO.md` and this plan.

Preserve unrelated dirty files, including direction-refresh docs, `SampleScene.unity`,
`Assets/1.unity`, `ProjectSettings`, `graphify-out/`, `nul`, and `AGENTS.md` edits.
The implementation and review tasks do not stage, commit, push, merge, or tag. The
lead owns the accepted checkpoint delivery. The user requested delivery to GitHub
with `main` as the only remaining branch; preserve the existing tags and delete
redundant branch refs only after ancestry checks.

## Out of scope

SC-011 cross-generation event content, SC-012 full chronicle, domain-wide politics, new packages, and third-party assets are out of scope. Checkpoint delivery is a separate lead-owned post-acceptance action.
