# M5: Versioned Single-Slot Save Design

## Intent

Add the Phase 1 single-slot save/load loop so the accepted M1-M4b demo can be saved, reloaded, and replayed without duplicating mission rewards. This is a small, explicit persistence layer, not a save framework, migration system, or multi-slot feature.

## Current state and sequence

- M1, M2, M3, M4a, and M4b are accepted and merged into `main`; the latest checkpoint is `demo-M4b`.
- Runtime state currently lives in `GameSession`: `WorldClock`, `CurrentLocation`, three fixed mission models, and `CharacterRelationships`. Dialogue instances are transient.
- Mission models expose stages and once-only merit awards (+10, +5, +5). Merit is derived from the three models.
- `GameSession.InitNewGame()` resets everything to 1560-04-01 in Kiyosu.
- No persistence code, package, or asset exists yet.

## Goals

- Define a versioned `SaveData` DTO with `saveFormatVersion` and only plain, stable-ID data.
- Save and restore: current date, current location, all three mission stages (which imply claimed rewards), and all NPC relationship values.
- Write one slot atomically so a failed write never destroys the previous save.
- Reject unsupported versions, corrupt JSON, and invalid data with a clear, non-destructive error.
- Ensure loading a completed mission cannot award its merit again.
- Provide a minimal in-game Save/Load control and a visible result message.
- Cover the DTO, validator, file store, and session snapshot/restore with EditMode tests.
- Keep the full 10-15 minute demo loop playable after a save/reload.

## Save format (version 1)

```json
{
  "saveFormatVersion": 1,
  "year": 1560,
  "month": 4,
  "day": 2,
  "locationId": "owari.atsuta",
  "supplyShortageStage": 2,
  "sealedLetterStage": 1,
  "marketTallyStage": 0,
  "relationships": [
    { "characterId": "npc.kuroda.iori", "value": 10 }
  ]
}
```

Rules:

- `saveFormatVersion` must equal `SaveDataValidator.CurrentVersion` (1). Anything else is rejected as unsupported.
- Date must satisfy `GameDate.IsValidDate` (1560-01-01 through 2000-12-31).
- `locationId` must be one of the three stable location IDs.
- Mission stages are stored as their integer enum values and must be defined stages of their model.
- Relationship entries must use known NPC IDs (never the player ID), appear at most once, and stay within [-100, 100]. Missing NPCs default to 0.
- Merit is not stored. It is derived from mission stages, so a tampered or duplicated merit value cannot exist.
- Unknown JSON fields are ignored; missing optional fields default safely.

## Persistence behavior

- Slot path: `<Application.persistentDataPath>/SengokuChronicle/save-slot-1.json`.
- Save: validate first, serialize with Unity's built-in `JsonUtility`, write to a temporary file in the same directory, then replace/move it over the slot. On any failure, delete the temporary file and leave the existing slot untouched.
- Load: missing file, unreadable file, invalid JSON, unsupported version, and invalid data each produce a distinct error code and message. Loading never writes to the slot.
- Errors are returned as results, not thrown to the UI.

## Session mapping

- `GameSession.CaptureSave()` builds a `SaveData` from current state.
- `GameSession.TryApplySave(data)` validates first, builds all replacement state in local variables, and only then assigns it. A rejected load leaves the live session untouched.
- Applying a save replaces the clock, location, missions, and relationships, and clears transient dialogue instances.
- `GameSession.SaveTo(store)` / `LoadFrom(store)` wrap the store for UI use.

## UI and scene integration

- Add one compact Save/Load control with a status line to the existing map screen. Two buttons (存档 / 读档) plus one feedback text are enough; no new panel hierarchy.
- Save reports success or a specific failure. Load refreshes the date, location marker, destination panel, mission board, and closes any open NPC/dialogue UI.
- The control must not overlap existing map, town, mission, or NPC controls. Scene integration follows the existing single-writer, one-Undo-group, validate-before-save, rollback-on-failure updater pattern.
- Generated UI stays under one fixed root.

## Non-goals

- No multiple slots, autosave, cloud sync, encryption, compression, or migration tooling.
- No generic serializer abstraction, repository pattern, event bus, or new package.
- No serialization of Unity objects, `MonoBehaviour`, or ScriptableObject references.
- No changes to mission rules, rewards, travel rules, or `PlayerMovement2D`.
- No edits to `SampleScene`, Build Settings, or unrelated `ProjectSettings`.

## Reuse research

Unity's built-in `JsonUtility` already covers this flat DTO, so no serializer dependency is needed. Adding Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) would require package approval and offers no benefit for one flat object. No external save-system repository is proportionate to one slot and roughly ten fields; the existing plain-C# + asmdef structure is reused instead.

## Acceptance evidence

- Unity compilation succeeds with zero new Console errors.
- Full EditMode suite passes, including all existing M1-M4b regressions and the new persistence tests.
- FoundationScene validation passes with the Save/Load control bound exactly once and no overlap with existing controls.
- In Play Mode: play part of the loop, save, exit and re-enter Play Mode, load, and confirm date, location, mission stages, merit, and relationships match; a completed mission cannot pay out again.
- Loading a corrupt or unsupported-version file shows a clear error and leaves the existing slot and live session unchanged.
- User Play Mode acceptance is required before the final M5 checkpoint, merge, or tag.
