# Project State - Sengoku Chronicle

Audit date: 2026-10-29 (Pacific/Honolulu)

## Current milestone

- M2 World Map, Travel and Town Menus feedback update is ready for a revised Play Demo, pending user acceptance.
- The original `feature/m2-world-map` demo branch remains frozen and unchanged. This follow-up is isolated on `feature/m2-ui-overlap-fix`.
- The overlap fix is committed as `9c2ac69` and pushed to the matching origin branch.
- Unity Editor 6000.6.6f1 is open on `C:\Users\panye\orca\workspaces\SengokuChronicle-M2\m2-ui-overlap-fix`, with `FoundationScene` loaded.
- The existing M3 branch remains separate. Nothing has been merged to `main`.

## Verification

- Unity Editor compiled the updated scripts after an explicit Assets > Refresh. The EditMode Test Runner found 73 tests; all 73 passed with 0 failures.
- The Editor Console showed 0 errors after the updated test run and Play Mode walkthrough.
- Play Mode verified the separated world-map panel, a one-day Kiyosu-to-Atsuta preview and arrival, an opaque town screen with map controls hidden, and return to map with Atsuta marked current.
- The earlier M2 walkthrough also verified cancellation preserves date and location and direct Atsuta-to-Nagoya travel is unavailable. No standalone player build was run.

## Feedback update and next step

- Feedback: the map and town panels overlapped. The existing safe layout updater now places the map panel in the lower canvas area; the town screen hides map-only controls and uses an opaque background.
- M2 includes the three-location map, travel preview/confirm/cancel, and a town menu with locked facility placeholders.
- Characters, missions, and save/load are later Phase 1 milestones.
- This revised demo candidate is ready for user evaluation. After the user says **Accepted**, merge and tag M2, then begin M3. Preserve the original branch and both demo checkpoints.

## Preserved worktree changes

- Original M2 worktree changes remain untouched: `.vscode/settings.json`, `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`, `.audit_scene_tree.json`, and `ProjectSettings/SceneTemplateSettings.json`.
- The isolated feedback worktree retains `.vscode/settings.json`, `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`, and `ProjectSettings/SceneTemplateSettings.json`; none are included in the demo commit.
