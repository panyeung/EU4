# M4b Two Simple Missions Implementation Plan
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two fixed Phase 1 mission loops, show all three missions in the town UI, and award each M4b mission +5 merit exactly once.

**Architecture:** Add one plain-C# state model per mission and named `GameSession` actions that validate location and order. Adapt presentation and the transactional FoundationScene updater to display a fixed three-entry mission board while preserving M4a behavior.

**Tech Stack:** Unity 6000.6.6f1, C#, existing Domain/Application/Presentation/Editor assemblies, built-in uGUI, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-01-m4b-two-simple-missions-design.md`

## Global Constraints

- Use only existing NPCs and the three existing locations; do not add a package, asset, scene, or generic mission framework.
- Keep mission rules in plain C# and retain M4a's +10 merit behavior.
- Mission actions do not advance date or change location; map travel remains unchanged.
- Keep `PlayerMovement2D.cs`, `SampleScene`, Build Settings, and unrelated ProjectSettings unchanged.
- Preserve `.meta` files/GUIDs and all pre-existing user work; do not modify Library or Temp.
- Use the already-open Editor for Unity compile, tests, and scene integration; never launch a second Editor or batchmode.
- Do not checkpoint, merge, or tag before explicit user Play Mode acceptance.

## Review Focus

- Mission actions submitted with a wrong location or stale stage must fail without partial changes.
- Repeat final actions must not award merit more than once.
- Completing missions in either order must preserve each independent state and aggregate merit.
- Missing/duplicate board roots or unbound controls must make the scene updater refuse save and roll back.
- Location changes must refresh action availability without mutating mission state or advancing time.

## Planned files

- Create `Assets/Scripts/Domain/SealedLetterMission.cs` and `Assets/Scripts/Domain/MarketTallyMission.cs`: fixed stages, validation, once-only +5 award.
- Modify `Assets/Scripts/Application/GameSession.cs`: own/reset both models; expose named actions and aggregate `Merit`.
- Modify `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and its tests: retain the M4a default page and add a toggle to the two-row M4b page while preserving M4a actions and references.
- Modify the existing Editor updater and `FoundationSceneValidator`: transactionally add the two mission rows and page toggle to the existing fixed panel root; validate it does not cover neighboring town controls.
- Modify only `Assets/Scenes/FoundationScene.unity` through the validated updater; preserve all existing object identities and references.
- Update `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `DECISIONS.md`, and add `M4B_DEMO.md` with actual evidence after verification.

## Task 1: Mission models and GameSession actions

**Files:** create the two Domain models and corresponding EditMode tests; modify `GameSession.cs` and `GameSessionMissionTests.cs`.

**Interfaces:**
- `SealedLetterMission.Stage` has `NotAccepted`, `Accepted`, `RewardClaimed`; named methods accept current `LocationId` and enforce Kiyosu acceptance, Nagoya delivery, +5 once.
- `MarketTallyMission.Stage` has `NotAccepted`, `Accepted`, `Investigated`, `RewardClaimed`; named methods enforce Nagoya acceptance, Kiyosu investigation, Nagoya report, +5 once.
- `GameSession` exposes mission stage properties and named action methods; `Merit` sums M4a and both M4b model rewards.

- [ ] Write tests for each valid route, all wrong-location/wrong-stage rejections, unchanged state on rejection, and duplicate final action.
- [ ] Run the focused EditMode tests in the open Editor and confirm the new tests fail for the missing models/actions.
- [x] Implement the two fixed models and named `GameSession` actions; reset both in `InitNewGame`.
- [x] Add tests proving either completion order produces a maximum aggregate `Merit` of 20 including M4a, and mission actions preserve date/location.
- [ ] Run focused mission/session tests; confirm all new and M4a mission tests pass.

## Task 2: Fixed three-entry presentation

**Files:** modify `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and `Assets/Tests/EditMode/SupplyShortageMissionPanelTests.cs`; modify only necessary town-menu binding code.

**Interfaces:** extend the existing panel's serialized bindings with two status texts, two objective texts, two feedback texts, two contextual action buttons, and one page toggle. The board shows M4a by default and both M4b rows on the alternate page; it invokes named `GameSession` methods and refreshes after location changes. Preserve the existing M4a binding overload and action/text results.

- [x] Add tests for per-location action availability, objective/status updates after each action, board hiding/return behavior, missing references, and refresh after travel.
- [ ] Run focused presentation tests and confirm failures expose the missing board behavior.
- [x] Implement the fixed board using existing uGUI conventions; keep domain rules outside MonoBehaviours.
- [ ] Run presentation tests and all existing M4a panel tests.

## Task 3: Transactional scene integration

**Files:** create/modify the M4b Editor updater and updater tests; modify `FoundationSceneValidator.cs`; update `FoundationScene.unity` only via the open Editor.

**Interfaces:** one menu command adds one fixed board below `M2WorldMapRoot/TownMenuRoot`; the test seam validates a `Scene` without saving; menu execution saves only the clean, active `FoundationScene` after all references validate.

- [x] Add updater tests for wrong/dirty scenes, missing/duplicate roots, correct hierarchy/bindings, and rollback after validation failure.
- [x] Run updater tests and verify refusal paths leave no partial objects.
- [x] Implement one-Undo-group construction, binding, validation, rollback, and save-after-validation.
- [x] Update the scene validator to require exactly one board and verify all serialized controls/references.
- [x] Run updater and validator through the already-open Editor; record the fresh validation report.

## Task 4: Full verification and demo handoff

- [x] Run the full EditMode suite from the open Editor and record totals and timestamp.
- [x] Inspect the current Editor log and report compilation independently from test results.
- [x] Verify the current FoundationScene validation report independently.
- [ ] Record current Editor Console error and warning counters independently.
- [x] Play both mission loops in the existing Editor; user reported the requested walkthrough passed; exact runtime values were not independently captured.
- [ ] Record exact Editor Console counters and runtime merit/date values independently.
- [x] Update `M4B_DEMO.md`, `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, and `DECISIONS.md` with observed results only.
- [ ] Complete the Kanban same-task QATester workflow; Kanban tools are unavailable in this session. Independent technical QA review accepted with no blocking defects.
- [x] Wait for explicit user Play Mode acceptance before creating the final checkpoint commit, merge, or tag; the user accepted M4b on 2026-11-01.

Integration complete: checkpoint `70ac62f0d8a9866488ae194c3cf55c68a8be2e29` is tagged `demo-M4b` and fast-forwarded into local `main`. No remote push was made. The exact Console counters and runtime merit/date values remain unrecorded.
