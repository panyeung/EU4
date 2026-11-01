# M6 Title Screen & Demo Packaging — Candidate

**Status: Candidate; Play Mode acceptance pending.**
**Branch:** `feature/m5-save-slot` (user directed continuing M6 on the same branch and worktree)
**Design:** `docs/superpowers/specs/2026-11-01-m6-title-demo-packaging-design.md`
**Plan:** `docs/superpowers/plans/2026-11-01-m6-title-demo-packaging.md`

## Scope

- Play Mode boots to a full-screen title overlay with 「新游戏」 and 「继续」.
- 继续 reads the existing M5 single slot; missing, corrupt, or unsupported-version slots show a clear message, disable the button, and never mutate the live session or the slot file.
- 新游戏 resets the session to 1560-04-01 / 清洲 before entering the map.
- Demo closing line: when all three missions reach `RewardClaimed`, the map shows 「Phase 1 演示完成。总功绩：20。」.
- No new packages, no save-format change, no change to the in-game M5 save/load panel.

## Implementation

- `TitleScreenController` (Presentation): binds session, map controller, title root, two buttons, status text; `SetStore()` test seam; default `SaveSlotStore.CreateDefault()`; `RefreshTitleState()` distinguishes NotFound / invalid / valid slots; `StartNewGame()` = `InitNewGame` + `ReturnToMap` + hide; `TryContinue()` = `LoadFrom` (atomic) + `ReturnToMap` + hide on success, status + stay on failure.
- `DemoSummaryPanel` (Presentation): idempotent `Refresh()` shown/hidden from `GameSession.IsDemoComplete` (new pure-C# property); lives on the always-active GameSession object.
- `AddM6TitleScreen` (Editor): one-Undo-group transactional updater that creates `Canvas/M6TitleRoot` (full-screen opaque raycast-blocking Image, last sibling, saved inactive) with `M6TitleLabel`, `M6NewGameButton`, `M6ContinueButton`, `M6TitleStatusText`, plus `M2WorldMapRoot/M6DemoSummaryText` (inserted before `TownMenuRoot`, saved inactive, at (460,-110) 260x44), attaches both components to the GameSession object, binds every reference, validates bindings/sibling order/containment/overlap against M5 and travel controls, and saves only after validation. Menu: `Tools/Sengoku Chronicle/Add M6 Title Screen`; batch entry `RunBatchIntegration()`.
- `FoundationSceneValidator` reports the M6 roots, the controller/panel bindings, and runs `AddM6TitleScreen.Validate`.

## Verification evidence (observed)

- **Compilation:** Unity 6000.6.6f1 batchmode; `m6-batchmode-1.log`, `m6-batchmode-2.log`, `m6-batchmode-final.log` contain no `error CS` lines.
- **EditMode (pre-integration):** `m6-editmode-1.xml` reports `total="401" passed="401" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 21:27:53Z) — the 381 existing M1–M5 tests plus the 20 new M6 tests (`TitleScreenControllerTests` 8, `DemoSummaryPanelTests` 5, `AddM6TitleScreenTests` 7).
- **Scene integration:** `RunBatchIntegration` opened FoundationScene, ran the transactional updater, and saved; batch log line `M6 title integration completed and FoundationScene was saved.` with no rollback warnings. The scene diff is +764/−12 (the removed lines are Unity re-serializing existing Image/reference blocks; no controls were deleted).
- **Scene validation:** fresh `Logs/SceneValidationReport.txt` reports `FOUND TitleScreenController: GameSession`, `PASS M6 title screen hierarchy, bindings, gating, and demo summary.`, `BOUND GameSession.titleRoot/newGameButton/continueButton/statusText/summaryText`, `M6TitleRoot [inactive]` as the last Canvas child, and `RESULT: PASS; Errors: 0`.
- **EditMode (post-integration, final):** `m6-final-editmode.xml` reports `total="401" passed="401" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 21:45:01Z).
- **Console:** not captured. Batchmode logs are not the Editor Console, so no Console counters are claimed.
- **Play Mode:** not run yet; see the acceptance path below.

## Required Play Mode acceptance path

1. Open this worktree in Unity 6000.6.6f1 and load `Assets/Scenes/FoundationScene.unity`; clear the Console.
2. Enter Play Mode: the title overlay covers the map; the status line reflects the slot (the M5 acceptance slot `1560-04-05` / supply stage 4 exists, so 继续 should be enabled).
3. Click 继续: date/day, location, mission stages, and merit match the slot; title disappears; map is interactive.
4. Exit and re-enter Play Mode; click 新游戏: date resets to 1560-04-01, location 清洲, merit 0; title disappears.
5. With the slot renamed/deleted and Play re-entered, confirm 继续 is disabled with 「尚未找到存档，可开始新游戏。」 and 新游戏 still works.
6. Complete all three missions (or continue a completed save): the map shows 「Phase 1 演示完成。总功绩：20。」.
7. Report the observed values and Console counters.

User Play Mode acceptance is required before the final M6 checkpoint, merge to `main`, or a `demo-M6` tag.
