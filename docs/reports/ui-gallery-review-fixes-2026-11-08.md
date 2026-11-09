# UI Gallery review fixes and verification — 2026-11-08

Task: `task_ce3ab89ca1da` / Dispatch `ctx_2423fb391132`

Scope: review RETURN fixes plus the follow-up map layout and test-fixture repair. No
`FoundationScene`, `ProjectSettings`, `Library`, `Temp`, `.meta`/GUID, `graphify-out/`,
`nul`, or `Assets/Scenes/TestResults_20261108_072929.xml` changes were made in this follow-up.

## File-scoped diff

| File | Change |
|---|---|
| `Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs` | Added `_hasShownPage` early return so re-clicking the current tab is a no-op; added `MapReturnCount`, `HouseholdCompositionBuildCount`, `EventPanelRefreshCount` diagnostics; subscribed to `GameSession.SessionStateChanged` for navigator-owned household/pending refreshes only; reset `_hasShownPage` on `BindReferences`; `ResolveSession` now subscribes a discovered session; `HouseholdRosterEmpty` uses `SengokuUiTheme.ParchmentLight`; family roster remains at `x=-100` to clear the nav rail and succession HUD; map illustration safe width is reduced to clear the right-side status column. |
| `Assets/Scripts/Presentation/CharacterDailyPageView.cs` | Extracted the real relation render path to `public void RenderRelations(IReadOnlyList<M2RosterRow> roster, PersonId selectedId)`, used by `Render`. Added distinct truthful copy for an empty roster, no selection, and a selected person with no other members. Relation cards hide when there is no valid selection. |
| `Assets/Scripts/Editor/AddM02DailyActionPanel.cs` | `Validate` now requires `M02RelationsEmptyText` and a bound `CharacterDailyPageView.relationsEmptyText`; the generator creates and serializes that Text. |
| `Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs` | Fixed backdrop lookup to use the real host (`navigator.transform.parent`, or self when parentless) for C1; retained raycast/stretch assertions. Added same-tab idempotence test that asserts no extra `ReturnToMap`, household rebuild, or event refresh, and that only a real session change refreshes the composition. Corrected the map fixture to use one UI Graphic per GameObject and retained an image/control non-overlap assertion. |
| `Assets/Scripts/Presentation/FamilyRosterPanel.cs` | Changed family-tree cards and labels to readable parchment/ink contrast. |
| `Assets/Tests/EditMode/FamilyRosterPanelTests.cs` | Added assertions for parchment card background and ink label color. |
| `Assets/Tests/EditMode/M02DailyActionPanelTests.cs` | Added runtime relation-state coverage driven through the actual `RenderRelations` path for 0/1/3 other members, selection switches, empty roster, and no selection. Added a `Validate` regression that unbinds/restores `relationsEmptyText` via `SerializedObject`. Earlier tab-hit-area and generated empty-state assertions remain. |

Inspect exact hunks with:

```text
git diff -- Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs
git diff -- Assets/Scripts/Presentation/CharacterDailyPageView.cs
git diff -- Assets/Scripts/Editor/AddM02DailyActionPanel.cs
git diff -- Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs
git diff -- Assets/Tests/EditMode/M02DailyActionPanelTests.cs
```

## Review item mapping

- C1: `ReferenceGalleryNavigatorTests` backdrop lookup now goes through `BackdropHost(...)`, which resolves the actual parent used by `ReferenceGalleryNavigator.StyleSharedFrame`.
- I1/M2: `PageView_RelationsEmptyStateTracksRosterSizeAndSelection` calls the production `RenderRelations` overload with 0, 1, and 3 other members and switches the selected `PersonId`; assertions cover both card visibility and empty copy.
- I2: `ShowPage_ReclickingTheCurrentTabDoesNotReplayPageSideEffects` asserts `MapReturnCount`, `HouseholdCompositionBuildCount`, and `EventPanelRefreshCount` stay unchanged on a same-tab click; a `GameSession.InitNewGame()` state change is used to prove the shared refresh still happens on a real session event.
- M1: `HouseholdRosterEmpty` now uses light parchment text on the indigo rail.
- M3: `Validate` rejects a missing `M02RelationsEmptyText` or an unbound `CharacterDailyPageView.relationsEmptyText`; the new test unbinds and restores the reference.
- M4: untouched.

## Verification

- Unity Editor `6000.6.6f1`, PID `126040`, project path `C:\Users\panye\My project`; no
  batchmode was used.
- Compilation: Tundra build succeeded after the follow-up edits. One pre-existing obsolete API
  warning remains for `FindFirstObjectByType`; no C# compile errors were reported.
- EditMode: Unity Test Runner completed **1312/1312 passed, 0 failed**. The first rerun exposed
  the invalid test fixture (`Image` and `Text` cannot share one `Graphic` slot); after fixing it,
  the overlap assertion exposed a narrow map/status collision, fixed by reducing the map safe
  width. The final full run passed.
- Play Mode: opened the SC-010 standard sample, visually checked the family tree card contrast
  and map/status spacing, then exited Play Mode. Console counters during this pass were
  **0 logs, 0 warnings, 0 errors**.
- The scene validator passed earlier in this task; no scene content was changed by this follow-up.
- Computer Use screenshots were deleted immediately after inspection.

## Remaining

The five pages remain an iterative reference-matching task; this repair specifically resolves the
family-tree contrast, map status readability/safe width, and the EditMode fixture failure. No
milestone acceptance, merge, or tag was performed in this follow-up.
