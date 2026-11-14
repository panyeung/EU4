# M06 tactical infiltration: design and implementation plan

> For agentic workers: implement the checked tasks in order. OpenCode2 owns code; the lead owns scene integration, Unity verification, and Play Mode sign-off.

**Goal:** Add one deterministic, playable infiltration operation to the existing family campaign, then preserve its result through the current save and chronicle system.

**Architecture:** Keep map rules, unit state, guard reactions, and outcomes in plain C# under `Domain`. `GameSession` owns the active operation and atomically applies its terminal result to the existing resources and `HistoryLedger`. A uGUI panel in `FoundationScene` presents a fixed grid and sends commands through `GameSession`.

**Tech stack:** Existing Unity 6000.6.6f1 project, C#, uGUI, existing persistence and chronicle APIs. No new package or external asset.

## Authority and intent

GDD v4.0, M06 requires a small tactical map, a clear objective, a few ninjas and guards, movement, vision, alert, basic stealth, extraction, and success/failure/withdrawal writeback. At least one success route and one failure or withdrawal route must be playable. Results must return to management and survive save/load. The existing project has no tactical grid, guard vision, alert, stealth, or extraction rules; the M06 reuse survey is in `docs/archive/verification-and-research-history.md#2026-11-10-m06-reuse-scout`.

The purpose of this slice is to test whether a short tactical operation adds value to the family-management loop. It is one authored operation, not a general battle framework.

## Status and prerequisite

Status: M05 technical gate passed on 2026-11-11 after the failure route and terminal save/load were exercised in Play Mode; a fresh `FoundationScene` Validator run also passed with zero errors. Under the user's instruction to let the lead choose and execute the next roadmap step, GDD v4.0 decision point B is resolved YES: the paid commission, scarce eligible assignee, creed-based score modifier, uncertain outcome, and persistent reward/loss create an understandable management tradeoff without tactical play. M06 implementation is authorized. Final user milestone acceptance for M05 remains pending; this decision does not merge or tag a milestone.

## Design

### Mission and map

- One optional, once-per-campaign operation, `mission.m06.intercepted-dispatch`, opens from the 清洲 town menu as `潜入密令：夺取密报`.
- Select exactly two living, adult, non-retired current-family members who are idle, not assigned to an industry or another active task, and at 清洲. Reuse the existing work-eligibility and location rules. If fewer than two qualify, do not start or spend resources.
- Use one fixed 8×6 orthogonal grid with coordinates `(x,y)`, where `x` increases east and `y` increases north. Entry/extraction cells are `(0,0)` and `(1,0)`. The dispatch is at `(6,4)`. The authored wall cells are `(3,1)`, `(3,2)`, `(3,3)`, and `(5,2)`. Cover cells are `(2,2)`, `(4,4)`, and `(5,4)`.
- Two guards protect the dispatch. One follows an authored patrol loop; one remains at `(6,3)` facing north. The patrol route and waypoints are stored in the fixed scenario definition, not generated randomly.

### Player turns and movement

- One valid player command is one turn. The player chooses either operative, then issues one action. A valid command is followed by one deterministic guard response. The campaign calendar does not advance during this operation.
- `Move` enters one adjacent orthogonal walkable cell. Diagonal movement is illegal. `Sprint` moves two orthogonal cells in one direction, requires both cells to be walkable and unoccupied, and emits noise at the destination. `Wait` changes no operative position. `Interact` picks up the dispatch when the operative is orthogonally adjacent. `Extract` ends in success only when the dispatch carrier and both uncaptured operatives occupy the two extraction cells. `Withdraw` is always available and ends in withdrawal.
- No unit may enter a wall, occupied cell, or cell outside the 8×6 map. Invalid commands leave the complete battle and campaign state unchanged.

### Vision, cover, noise, and guard response

- A guard sees a 3-cell-wide, 4-cell-deep rectangle in the direction it faces. A wall blocks vision when it lies between guard and operative. Guards do not see into a cover cell unless adjacent. A sprinting operative is exposed for that guard response, including on cover.
- Sprint noise is heard by guards within Manhattan distance 3. A hearing guard investigates the noise source and raises the alert display to `调查中`. A guard that sees an operative raises it to `警戒` and pursues the nearest visible operative. A guard that reaches an operative's cell ends the operation in failure; no combat, injury, or death is simulated in M06.
- If guards cannot see or hear an operative, they continue their fixed patrol. After two response turns without sight or new noise, an investigating guard returns to its patrol. All path choices and ties use fixed N/E/S/W neighbor order, then stable unit ID order.
- A deterministic shortest-path search selects a guard's next cell. No random stream is consumed. The UI shows guard facing, sight area, current state, and the event that caused a state change.

### Outcome and campaign writeback

- The operation has terminal outcomes `Succeeded`, `Failed`, and `Withdrawn`. The mission is one-time after a terminal result; the existing saved family chronicle is the completion authority, so no new save field or schema version is required.
- The operation has an announced cost of 2 supplies. Apply that cost exactly once on a terminal result. Success additionally grants 4 funds and 6 intelligence. Failure and withdrawal grant no reward. Clamp resource results to the existing resource limits and display actual applied values.
- Append one mission chronicle entry with mission ID, outcome, campaign date at launch, the two operative IDs/names, turn count, and actual resource deltas. Append and resource changes form one transaction: if the chronicle rejects the row, restore the prior resources and do not mark the mission complete.
- Mid-operation saving/loading is unavailable. Disable the save controls while the operation is active and reject `GameSession.SaveTo` / `LoadFrom` at the application boundary. `Withdraw` is the player's exit. Once the terminal outcome returns to management, ordinary save/load must preserve resources and the chronicle and prevent replay.
- In-progress tactical state is session-only. If the process exits before a terminal result is saved in a campaign slot, the previous saved campaign remains authoritative and the operation can be attempted again from that save.

### Scope boundaries

- Do not add combat, weapons, damage, permanent capture, injury, death, character-memory integration, simultaneous hidden orders, a second scene, a new Unity project, a generic quest/battle framework, or an external package.
- M06 returns resources and a family chronicle entry. M07 owns tactical injury/death effects, character experience, and wider campaign integration.
- Use the existing scene and flat, original uGUI grid treatment. No new bitmap art is required for this functional prototype. If an art asset becomes necessary, follow `docs/ART_ASSET_WORKFLOW.md` and record its source/license in `ASSETS.md`.

### Assumptions

These details are not prescribed by GDD v4.0 and are lead decisions under the user's delegated instruction to choose the next roadmap work and execute it:

- The once-per-campaign operation is launched from 清洲 and uses two eligible family members.
- A turn is one operative action followed by one guard response. The campaign date stays fixed.
- The fixed map is 8×6; guard sight, cover, noise, patrol reset, and tie-break values are as stated above.
- The operation costs 2 supplies and success rewards 4 funds plus 6 intelligence. Failure/withdrawal keep the cost and earn no reward. M07 (2026-11-11) adds an explicit preparation choice frozen at start: “不准备” costs 0 extra and adds 0 to the auto check; “侦察路线” costs 1 extra supply and adds +10. The costly-success result also pays 1 extra supply. All of the base 2, the preparation 1 and the costly 1 are charged once at the shared terminal settlement; nothing is spent at start. Manual clean extraction is a full success, extraction after any guard reached alert is a costly success, withdrawal is a safe failure and capture is a severe failure. Severe failure runs one independent 10% death check for each briefing-listed member; survivors become injured and recover at the next month boundary.
- Mid-operation save/load is out of scope. Only terminal results must persist, as required by M06 acceptance.
- M06 failure is mission failure, not character death or injury; those consequences belong to M07.

## Spec probes

| Requirement source and shape | Edge raised | Resolution |
|---|---|---|
| GDD M06 movement; `numeric-range`, `collection` | Grid bounds, walls, diagonal/occupied cells, start or exit cells | Resolved: 8×6; coordinates are 0-based; N/E/S/W only; invalid movement leaves state unchanged. EditMode boundary tests cover all four edges and occupied cells. |
| GDD M06 small team; `collection`, `empty` | Fewer than two eligible operatives; duplicate IDs; selection order | Resolved: start requires exactly two distinct eligible members; stable roster order; no resource mutation when unavailable. |
| GDD M06 vision/alert/stealth; `stateful`, `numeric-range` | Guard visible/hidden threshold, walls, cover, noise, detection and pursuit order | Resolved by the exact sight/cover/noise rules above; tests include adjacent cover, sprint exposure, intervening wall, alert acquisition and return to patrol. |
| GDD M06 outcome routes; `variants`, `stateful` | Success, failure, withdrawal, duplicate terminal command | Resolved: all three results are distinct; the first terminal result commits once; all later commands are rejected without resource/chronicle changes. Each result has EditMode coverage; Play Mode must complete success plus failure or withdrawal. |
| GDD M06 result persistence; `io`, `stateful` | Save/load during battle versus after result | Resolved: active save/load is rejected; terminal results use existing chronicle/resource persistence; live save/load after result is required. No save-schema bump. |
| Authored map example; `example`, `ordering/stability` | Patrol order and shortest-path ties | Resolved: fixed waypoint sequence and deterministic N/E/S/W, stable-ID tie-break. A test holds a route where two shortest paths are equal length. |
| New outcome resource values; `numeric-range`, `idempotency` | Low/maximum resources and repeated result commit | Resolved: the values are explicit assumptions; use existing resource clamps and verify actual deltas. Commit the result at most once. |
| Prohibition probe | Silent expansion into a combat simulator, deaths, tactical save format, or external framework | Resolved: explicit non-goals above; code review checks these boundaries. |

Coverage: 7 applicable edge items resolved, 0 unresolved, 0 backstop, 0 judgment. GDD requirements are directly testable. All non-GDD numbers and workflow choices are marked as assumptions above.

## Implementation plan

### File ownership

- OpenCode2 owns C# rules, session integration, EditMode tests, and non-scene docs listed in its task. It must preserve unrelated changes in the shared worktree.
- The lead owns `Assets/Scenes/FoundationScene.unity`, the existing Unity Editor, scene generation/validation, Play Mode, screenshots if needed, and cleanup. Only the lead runs the scene generator.
- Review is a separate, read-only OpenCode task after implementation; the lead routes corrections and signs off.

### Tasks

1. **Domain rules.** Add `Assets/Scripts/Domain/TacticalMissionState.cs` and `TacticalMissionRules.cs`. Define stable tile/unit/guard/outcome types, the fixed 8×6 scenario, validated movement/actions, guard sight/noise/patrol/search behavior, deterministic BFS, and terminal transitions. Add `Assets/Tests/EditMode/TacticalMissionRulesTests.cs` first for the edge cases in Spec probes, then implement until the suite passes.
2. **Session and persistence boundary.** Extend `Assets/Scripts/Application/GameSession.cs` with begin/action/snapshot/withdraw/outcome APIs. Reuse `CommissionAssignees`, work eligibility, `IndustryResources`, and `HistoryLedger`; add `mission.m06.intercepted-dispatch` validation in `Assets/Scripts/Domain/FamilyChronicleEntry.cs`. Reject save/load while tactical state is active. Do not add a `SaveData` field or schema version. Add `Assets/Tests/EditMode/GameSessionTacticalMissionTests.cs` for start gating, one-time cost, each result, atomic rollback, replay prevention, active-save refusal, and terminal save/load.
3. **Presentation.** Add `Assets/Scripts/Presentation/TacticalMissionPanel.cs`. Render the grid, team, turn, guard facing/sight, alert, objective, extraction, status, result and exact applied resources. Open/close must not advance turns or mutate state. Add `Assets/Tests/EditMode/TacticalMissionPanelTests.cs` for labels, enabled actions, status changes, and no-mutation refresh/open/close.
4. **Scene integration.** Add `Assets/Scripts/Editor/AddM06TacticalMissionPanel.cs` as a transactional updater under the existing `Canvas`, reachable from `TownMenuRoot` in 清洲. Integrate the panel in `Assets/Scripts/Presentation/WorldMapController.cs`, add an M06 row to `Assets/Scripts/Editor/FoundationSceneValidator.cs`, and test generator idempotency/rollback in `Assets/Tests/EditMode/AddM06TacticalMissionPanelTests.cs`. The updater uses one Undo group, validates before saving, rolls back with Undo, and does not reload the scene. The lead runs it in the current Editor and confirms all other scene roots survive.
5. **Verification and handoff.** Update `PROJECT_STATE.md` and `DECISIONS.md` with exact evidence. Add one player recipe at `.opencode/skills/verify/features/tactical-mission.md` and link it from `features/README.md`. In the existing Unity 6000.6.6f1 Editor, recompile, run focused M06 and full EditMode tests, validate `FoundationScene`, inspect Console counts separately, then Play Test one success route and one failure or withdrawal route. Verify terminal save/load without leaving changes in the user's save slot. The lead records the results and deletes any Play Mode screenshots immediately after inspection.

### Verification gates

- Compilation: `unity command recompile`, then `unity command recompile_status`; fail on `compilationFailed=true`, a non-completed recompile, or any C# compiler error.
- Tests: Unity Editor Test Runner focused M06 suites and full EditMode; fail if any selected test is missing, skipped, or failed.
- Scene: `Tools/Sengoku Chronicle/Validate Foundation Scene`; fail unless the M06 line and overall report both pass with 0 errors.
- Play Mode: current `FoundationScene`, one fresh SC-013 campaign, explicit M06 operation start, visible guard response, terminal result, return to management, and terminal save/load. Fail if the map rules are unclear, either required route is unreachable, state changes twice, or saved resources/chronicle differ after load.
- Console: record errors, warnings, and logs separately after the run. Require zero errors. Report warnings and logs without treating them as errors.
- Git: commit/push only reviewed and verified M06 files/hunks on the current branch after applicable gates pass. Do not create a worktree/branch, merge to `main`, or tag before final user milestone acceptance.

### Review focus

- Deterministic enemy decisions and equal-cost path tie-breaks.
- Invalid action atomicity, including walls, map edges, occupied cells, insufficient supplies, and repeated terminal commands.
- Mission result atomicity when chronicle validation rejects a result.
- Active mission save/load refusal versus terminal result persistence.
- The exact scene panel root, town button visibility, and preservation of existing M05/SC-024 panels.
