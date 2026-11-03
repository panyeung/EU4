# M5 Single-Slot Save Implementation Plan
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a versioned single-slot save/load loop that restores date, location, mission stages, derived merit, and relationships without duplicating rewards.

**Architecture:** Add a small `SengokuChronicle.Persistence` assembly with a flat `SaveData` DTO, a validator, and an atomic file store. `GameSession` captures and atomically applies the DTO. A compact uGUI control on the map screen exposes save/load, and the existing transactional scene-updater pattern integrates it.

**Tech Stack:** Unity 6000.6.6f1, C#, existing Domain/Application/Presentation/Editor assemblies, built-in `JsonUtility` and uGUI, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-01-m5-single-slot-save-design.md`

## Global Constraints

- One slot only; `saveFormatVersion` is mandatory and currently 1.
- Store only plain data and stable IDs; never serialize Unity objects or ScriptableObject references.
- Validate before writing and before applying; a failed save/load must leave the previous slot and live session unchanged.
- Do not store merit; derive it from mission stages.
- No new package, asset, scene, or generic save framework.
- Keep mission, travel, dialogue, and `PlayerMovement2D` behavior unchanged.
- Preserve `.meta` files/GUIDs and all pre-existing user work; do not modify Library or Temp.
- Use the already-open Editor for Unity compile, tests, and scene integration; never launch a second Editor or batchmode against a folder it holds.
- Do not checkpoint, merge, or tag before explicit user Play Mode acceptance.

## Review Focus

- Version, date, location, stage, and relationship validation must reject bad data with a specific error and no partial mutation.
- Save must be atomic: a failed replace cannot truncate or delete the existing slot.
- Loading must clear transient dialogue state and must not re-award any completed mission.
- Load must refresh every visible surface (date, location, mission board) and close stale NPC/dialogue UI.
- The Save/Load control must not overlap existing controls, and the updater must roll back on any validation failure.

## Planned files

- Create `Assets/Scripts/Persistence/`: assembly definition, `SaveData`, `SaveDataValidator`, `SaveSlotStore`, and result types.
- Modify `Assets/Scripts/Domain/{SupplyShortageMission,SealedLetterMission,MarketTallyMission}.cs`: validated restore factories that derive merit from stage.
- Modify `Assets/Scripts/Application/GameSession.cs`: `CaptureSave`, `TryApplySave`, `SaveTo`, `LoadFrom`.
- Create `Assets/Scripts/Presentation/SaveLoadPanel.cs`: save/load buttons, status text, refresh callback.
- Create `Assets/Scripts/Editor/AddM5SaveLoadPanel.cs` and extend `FoundationSceneValidator.cs`: transactional scene integration.
- Modify `Assets/Scenes/FoundationScene.unity` only through the validated updater.
- Create EditMode tests for the validator, store, session round trip, and panel/updater.
- Update `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `DECISIONS.md`, `ARCHITECTURE.md`, and add `M5_DEMO.md` with observed evidence only.

## Task 1: Persistence DTO, validator, and atomic store

**Files:** create `Assets/Scripts/Persistence/*` and `Assets/Tests/EditMode/SaveDataValidatorTests.cs`, `SaveSlotStoreTests.cs`; update `SengokuChronicle.Application.asmdef`, `SengokuChronicle.Presentation.asmdef`, and the EditMode test asmdef.

**Interfaces:**
- `SaveData` with `saveFormatVersion`, date fields, `locationId`, three stage ints, and `RelationshipSaveEntry[]`.
- `SaveDataValidator.Validate(SaveData)` returning `SaveOperationResult` with `SaveErrorCode`.
- `SaveSlotStore(string directory)` with `Save`, `Load`, `Exists`, `FilePath`, plus `CreateDefault()`.
- `SaveSlotStore.Save` writes a temp file and replaces the slot; `Load` distinguishes not-found, read failure, invalid JSON, unsupported version, and invalid data.

- [ ] Write validator tests for valid data, bad versions, invalid dates, unknown locations, undefined stages, unknown/player/duplicate relationships, and out-of-range values.
- [ ] Write store tests for round trip, missing file, corrupt JSON, unsupported version, invalid-data refusal, and overwrite.
- [ ] Implement the Persistence assembly and code; keep it free of scene/UI dependencies.
- [ ] Run the focused EditMode tests in the open Editor and confirm they pass.

## Task 2: Session snapshot and restore

**Files:** modify the three mission models and `GameSession.cs`; create `Assets/Tests/EditMode/GameSessionSaveTests.cs`.

**Interfaces:**
- `SupplyShortageMission.TryRestore(stage)`, `SealedLetterMission.TryRestore(stage)`, `MarketTallyMission.TryRestore(stage)` return a canonical model with merit derived from stage.
- `GameSession.CaptureSave()` returns the current state as `SaveData`.
- `GameSession.TryApplySave(data)` validates, builds replacement state locally, then swaps it in and clears dialogue state.
- `GameSession.SaveTo(store)` and `LoadFrom(store)` delegate to the store.

- [ ] Write tests for capture fidelity, full restore, rejected load leaving state unchanged, no reward duplication after load, dialogue reset, and store round trip.
- [ ] Implement restore factories and session mapping.
- [ ] Run focused tests plus all existing M4a/M4b session tests.

## Task 3: Save/Load UI and transactional scene integration

**Files:** create `Assets/Scripts/Presentation/SaveLoadPanel.cs`, `Assets/Scripts/Editor/AddM5SaveLoadPanel.cs`, and tests; modify `FoundationSceneValidator.cs` and `WorldMapController.cs` only where needed.

**Interfaces:**
- The panel binds a `GameSession`, a `SaveSlotStore`, two buttons, a status text, and a refresh callback.
- Save calls `CaptureSave`/store save and reports success or the exact error.
- Load calls store load + `TryApplySave`, then refreshes the map and closes NPC/dialogue UI.
- The updater adds exactly one fixed root, validates references and separation from existing controls, uses one Undo group, and rolls back on failure.

- [ ] Write panel tests for success, failure messaging, refresh, and missing dependencies.
- [ ] Write updater tests for wrong scene, duplicate root, missing references, and rollback.
- [ ] Implement the panel, updater, validator extension, and scene integration through the open Editor.
- [ ] Run updater/validator tests and record the fresh validation report.

## Task 4: Full verification and demo handoff

- [ ] Run the full EditMode suite from the open Editor and record totals and timestamp.
- [ ] Record compilation, EditMode, Console counters, and Play Mode evidence separately.
- [ ] Play the save/reload loop in the existing Editor and record actual values.
- [ ] Verify a corrupt and an unsupported-version save both fail without overwriting the slot or changing live state.
- [ ] Update `M5_DEMO.md`, `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `DECISIONS.md`, and `ARCHITECTURE.md` with observed results only.
- [ ] Wait for explicit user Play Mode acceptance before the final checkpoint commit, merge, or tag.
