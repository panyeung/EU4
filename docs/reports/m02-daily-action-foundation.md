# M02 Daily Action Foundation — Design and Implementation

Date: 2026-11-06
Scope: first M02 foundation per GDD v4.0 §13 (M02) and §19.1. This is an
implementation record, not a milestone acceptance record. M01 remains recorded as
Go/No-Go pending.

## Design

### One action per person

`DailyActionState` (Domain) owns one sorted row per registered family person and at
most one active `DailyActionTask` on each row. Planning a second action for the same
person is rejected with an explicit reason before any state is changed. There is no
frame-time update path: only `AdvanceOneDay`, `AdvanceDays`, `AdvanceActionDay`, or the
existing monthly transaction advance action days.

Action kinds:

| Kind | Duration source |
|---|---|
| Travel | `WorldMapRules.TryGetTravelDays`, including the no-direct-route rule |
| Rest | Caller-supplied positive duration |
| Training | Caller-supplied positive duration |
| Work | Caller-supplied positive duration |

Travel is the only kind with an endpoint. While a travel task is active the person's
row deliberately has no current location; `TryGetPersonLocation` returns false and
`IsPersonInTransit` is true. The action stores the departure point and destination as
route metadata only, so a person is never reported at both endpoints. On completion the
destination becomes the confirmed location; on interruption the person returns to the
departure point because this foundation has no mid-route or multi-node path model.

### Completion, interruption, replanning

`DailyActionResult` carries the person, kind, outcome (`Planned`, `Completed`,
`Interrupted`, `Replanned`, `Rejected`), date, and a readable reason.

- Completion removes the task and records the completion date.
- Interruption removes the task with the caller's reason. Travel interruption returns
  to the departure point.
- Replanning atomically records the interruption and plans the replacement. The session
  suppresses `SessionStateChanged` during the compound interrupt+plan operation and
  publishes exactly one notification after the final replacement state is committed, so
  observers never see a temporary no-action gap. If the replacement is rejected, the
  original task, outcomes, and message context are restored and no notification is
  emitted because the externally visible state did not change.
- Succession cleanup interrupts an outgoing head's unfinished action with a stated
  reason. Roster cleanup interrupts actions of people who died, left the family, or
  disappeared.

### Day/month boundary

- `AdvanceOneDay` keeps the existing rule that a single day cannot cross a month; when
  it does advance, it consumes exactly one action day.
- `AdvanceActionDay` is the explicit daily control. It uses the one-day path normally,
  and on the last day of a month calls the existing `TryAdvanceToNextMonth` exactly
  once. The monthly jump consumes the actual number of calendar days (for example,
  April 29 to May 1 is two days) for in-flight actions.
- `TryAdvanceToNextMonth` remains the single monthly transaction. Action progress and
  the completed `CurrentLocation` are part of its existing rollback snapshot, so an
  exception restores the action timeline and location together with date, industry,
  research, growth, doctrine, RNG, and chronicle state.
- No action stats, money, growth, or other reward is invented. The foundation only
  schedules, progresses, completes, interrupts, and persists time-bound activities.

### Determinism and ordering

People and outcome processing use ordinal `PersonId` order. No random stream is
consulted, so equal inputs and the same seed produce equal action state and completion
dates. Save rows are sorted by ordinal person ID.

### Persistence and migration

Schema v10 (rules v6) adds `dailyActions`, a sorted row set with one row per roster
person:

- person ID and confirmed location (empty while in transit)
- at most one active action: kind, start date, duration, elapsed days, travel origin
  and destination, and a stored reason

Validation rejects duplicate/missing people, unstable order, invalid locations,
elapsed days outside `0..duration-1`, dead or non-member active actors, non-travel
actions with travel endpoints, and travel whose duration does not match
`WorldMapRules`. `SaveDataMigrator` migrates v9 and earlier saves by adding empty
daily timelines at the saved roster/location; it never replays a day or month. v10
loads require the block, so an omitted block is rejected rather than silently
defaulted.

## Changed files

- `Assets/Scripts/Domain/DailyActionState.cs` (new)
- `Assets/Scripts/Domain/DailyActionState.cs.meta` (new)
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Persistence/SaveData.cs`
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- `Assets/Scripts/Persistence/SaveDataValidator.cs`
- `Assets/Tests/EditMode/M02DailyActionTests.cs` (new)
- `Assets/Tests/EditMode/M02DailyActionTests.cs.meta` (new)
- `Assets/Tests/EditMode/FamilyDoctrineSaveTests.cs` (v8 fixture strips the v10 block)
- `Assets/Tests/EditMode/M2ScenarioSaveTests.cs` (fixture rebuilds daily rows for the
  replaced succession roster)
- `docs/reports/m02-daily-action-foundation.md` (this report)

The pre-existing dirty SC-024 changes in `GameSession.cs`, `M2FixedScenario.cs`,
`SC024FamilyDoctrineScenario.cs`, the SC-024 tests, `FoundationScene.unity`,
`PROJECT_STATE.md`, and untracked SC-024 documentation were preserved. They were not
edited beyond the M02 integration points.

## Verification evidence

- Unity Editor used: PID 90052, `-projectPath C:\Users\panye\My project` (verified
  before operation). No batchmode and no Play Mode were started.
- Compilation: final Editor compilation after removing the temporary test-run helper
  succeeded (`*** Tundra build success`, 10 items updated).
- Focused EditMode suite: `M02DailyActionTests`, 11/11 passed after the atomic-replan
  correction. Result: `Logs/m02-replan-focused.xml`
  (`Passed total=11 passed=11 failed=0`).
- Full EditMode suite after the correction: 1196/1196 passed. Result:
  `Logs/m02-final-all-editmode.xml` (`Passed total=1196 passed=1196 failed=0`,
  skipped=0, inconclusive=0).
- Console after the final full run: 2 errors, 22 warnings, 18 logs. The two error
  entries are Unity AI Assistant `NoSubscription`/generator errors, not M02 or test
  failures.
- Runtime / Play Mode: not performed. No runtime or Play Mode claim is made here.

## Review correction: atomic replan notification

Coordinator review found that `TryReplanDailyAction` delegated to the public interrupt
and plan commands, both of which published `SessionStateChanged`; observers could
therefore see the transient interval with no active action. The correction adds an
internal notification-suppression scope around the compound operation. A successful
replan now publishes exactly one notification after the replacement is committed. A
rejected replacement restores the original task and does not publish any notification.
The focused regression `Replan_IsAtomicForObservers_AndRejectedReplanKeepsOriginal`
subscribes to `SessionStateChanged` and asserts one success notification with an active
replacement, no transient no-action observation, and zero notifications for a rejected
replan with the original task still active.

## Not part of this foundation

Numeric stat gains, economy rewards, action point budgets, multi-node pathfinding,
mid-route positions, autonomous NPC planning, UI panels, and Play Mode scene wiring
remain for later M02/M03 work.

## UI integration and scene wiring (2026-11-06)

This section records the smallest playable M02 UI integration. It is additive to the
foundation above and does not change its rules.

### Presentation

`DailyActionPanel` (`Assets/Scripts/Presentation/DailyActionPanel.cs`) projects the
existing read-only session surfaces:

- Every registered person is listed in the ordinal `PersonId` order owned by
  `DailyActionState`; the panel never re-sorts by name, location or action.
- Each row shows the person's known Chinese location, or `旅途中（出发地→目的地）`
  while a travel task is active, plus the active action kind, elapsed/duration and
  remaining days.
- Selecting a row and entering a positive integer duration schedules Rest, Training
  or Work through `GameSession.TryPlanDailyAction`.
- Travel buttons are enabled only for destinations that `WorldMapRules` accepts from
  the selected person's known location. Travel uses `TryPlanTravelAction` and its
  duration comes from the world-map rule, not from the duration field.
- A person with an active action is replanned through
  `GameSession.TryReplanDailyAction`, so the panel shows the explicit
  interrupt + replacement outcome sequence. A rejected replan keeps the original
  action and shows the session's rejection reason.
- The Interrupt control calls `GameSession.TryInterruptDailyAction` with a stated
  reason. The feedback line renders `LastDailyActionMessage` and the outcome kinds
  (`已安排`, `已完成`, `已中断`, `已重新规划`, `已拒绝`).
- No stat, economy, action-point, work reward or autonomous NPC planning was added.

### Time control

`WorldClockPanel.TryAdvanceOneDay` now calls `GameSession.AdvanceActionDay` instead of
`AdvanceOneDay`. The method advances one action day inside the month, and on the last
day of a month calls the existing `TryAdvanceToNextMonth` transaction exactly once.
The existing `EndMonthButton` and its transaction remain unchanged. After a successful
time turn (day or month) the clock panel explicitly refreshes the daily-action panel,
because ordinary day advances do not emit `SessionStateChanged`.

### Scene integration

`AddM02DailyActionPanel` (`Assets/Scripts/Editor/AddM02DailyActionPanel.cs`) is a
transactional generator following the established pattern: one Undo group, snapshot of
existing objects, validation before save, rollback with Undo on exceptions, and no
scene reload. Its ordinary menu refuses a dirty scene. The explicit
`IntegrateDirtyWorkspace` menu was used for this authorized integration because the
current shared scene already contains uncommitted SC-024 work that must remain in place.

The generated hierarchy is `Canvas/M02DailyActionRoot` with the active
`M02DailyActionOpenButton`, the hidden `M02DailyActionPanel`, a scrollable person list,
duration input, Rest/Training/Work/Travel controls, Interrupt, feedback and close
controls. The generator also binds `WorldClockPanel.dailyActionPanel` so successful
time turns refresh the panel. `FoundationSceneValidator` now finds `DailyActionPanel`,
checks every serialized reference and calls `AddM02DailyActionPanel.Validate`.

The pre/post scene comparison found no removed named objects. The existing SC-024
integration remains: 17 `SC024`/`FamilyDoctrine` matches, `Canvas/FamilyDoctrineRoot`,
`Canvas/FamilyDoctrineOpenButton` and `M6TitleRoot/SC024FamilyDoctrineDemoButton`.
The scene gained 21 `M02DailyAction` matches.

### Changed files for this integration

- `Assets/Scripts/Presentation/DailyActionPanel.cs` (new)
- `Assets/Scripts/Presentation/DailyActionPanel.cs.meta` (new)
- `Assets/Scripts/Presentation/WorldClockPanel.cs`
- `Assets/Scripts/Editor/AddM02DailyActionPanel.cs` (new)
- `Assets/Scripts/Editor/AddM02DailyActionPanel.cs.meta` (new)
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Tests/EditMode/M02DailyActionPanelTests.cs` (new)
- `Assets/Tests/EditMode/M02DailyActionPanelTests.cs.meta` (new)
- `Assets/Scenes/FoundationScene.unity` (through the generator only)
- `docs/reports/m02-daily-action-foundation.md` (this section)
- `PROJECT_STATE.md` (additive M02 status)

The pre-existing dirty SC-024 files and documentation listed above were preserved and
were not edited beyond the M02 integration points.

### Verification evidence

- Unity Editor used: PID 90052, `-projectPath C:\Users\panye\My project`, verified
  before GUI operation. No batchmode was used.
- Compilation: final post-cleanup `*** Tundra build success`, 15 items updated; no
  `error CS` after the last code change.
- Focused EditMode suite: 44/44 passed
  (`M02DailyActionPanelTests` 7, `M02DailyActionTests` 11, `SC010TimePauseTests` 5,
  `AddSC004MonthTurnControlTests` 8, `IndustryMonthCrossingTests` 13). Result:
  `Logs/m02-ui-focused.xml`, start time `2026-11-07 08:04:32Z`; summary file
  `Logs/m02-ui-focused-summary.txt` reads
  `Passed total=44 passed=44 failed=0 skipped=0 inconclusive=0`.
- Scene validation: `FoundationSceneValidator` result
  `RESULT: PASS; Errors: 0` in `Logs/SceneValidationReport.txt`, including
  `PASS M02 daily-action panel hierarchy, bindings, containment, ordering, and layout.`
  and 20 bound daily-action references.
- Play Mode: manually verified in the existing `FoundationScene` Editor session. Started
  a new game, opened the daily-action panel and confirmed all six registered people.
  Scheduled a two-day Rest task, observed `0/2`, advanced one day to `1/2`, then
  advanced again and observed completion. Scheduled Kiyosu-to-Atsuta travel, observed
  the person shown in transit, advanced one day and confirmed arrival at Atsuta.
  Saved a separate in-flight two-day Rest state, advanced one day, loaded it, and
  confirmed the date returned to 1560-04-04 and the `0/2` task remained active.
- Console: after exiting Play Mode the visible Unity Console counters were 0 logs,
  0 warnings, and 0 errors. Earlier Editor log entries include Unity AI Assistant
  `NoSubscription` errors, outside the M02 runtime path.
- Save hygiene: the test used the project's single save slot. The pre-test active save
  was recovered from the store's `.bak` file after the test; the active save and `.bak`
  now match that recovered state. The earlier `.bak` history was replaced by the save
  operation and could not be reconstructed.

### Status boundary

M01 remains Go/No-Go pending with its final milestone acceptance outstanding. The M02
daily-action slice passed the lead-run Play Mode gate, but has not received explicit user
milestone acceptance. It is not a claim that all of M02 is complete.

## 2026-11-09 current-main verification addendum

- Editor: Unity `6000.6.6f1`, PID `126040`, exact project path
  `C:\Users\panye\My project`; active scene `Assets/Scenes/FoundationScene.unity`.
- Compile/EditMode: OpenCode2 reported Tundra build success with no C# compiler errors.
  The focused caret regression passed `1/1`; the full EditMode suite passed `1374/1374`,
  with 0 failed and 0 skipped. The EditMode Console had 2 expected `LogError` entries
  from passing `FamilyFormationSessionTests` fault-injection cases, plus 28 warnings and
  18 logs; these are recorded separately from Play Mode.
- Scene validator: ran `Tools/Sengoku Chronicle/Validate Foundation Scene` in the
  existing Editor. `Logs/SceneValidationReport.txt` recorded `RESULT: PASS; Errors: 0`
  and `PASS M02 daily-action panel hierarchy, bindings, containment, ordering, and
  layout.` The scene was loaded and not dirty when the validator ran.
- Play Mode: entered `FoundationScene`, continued the existing save, opened the M02
  person/action page, changed duration to 3, closed and reopened the page, and confirmed
  it remained 3. Refocusing and typing at the end changed it to 31 without an input error;
  the temporary value was restored to 3. A two-day Rest action was scheduled; advancing
  one day changed the date from 1560-05-01 to 1560-05-02 and showed progress `1/2`;
  advancing again reached 1560-05-03 and completed the action. Travel to Atsuta showed
  an ETA of 1560-05-04; after confirming departure, the map showed current location
  Atsuta on 1560-05-04. No save or load was performed in this session.
- Play Mode Console: visible Unity counters after the route were `0 logs / 0 warnings /
  0 errors`. Play Mode was exited; no scene save was made. The existing single-slot save
  was not overwritten.
- Git: the M02 caret test commit `784190506f7ed1526d625bc48db87beaaf1a408b` was already
  present on `main` and `origin/main`. Existing unrelated M04A/M03/UI worktree changes
  were left untouched.

### Status boundary

The current-main M02 technical verification gate passed. This does not record user
milestone acceptance; M02 remains awaiting the user's explicit final acceptance. This
Play Mode session did not reverify save/load, and the separate M00 launch-scene/worktree
cleanup items remain outside this M02 acceptance pass.
