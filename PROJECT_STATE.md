# Project State — Sengoku Chronicle

Audit date: 2026-10-29 (Pacific/Honolulu)

## Current milestone

- M2 World Map, Travel and Town Menus is ready for the user's Play Demo, pending user acceptance.
- Unity Editor 6000.6.6f1 is open on `C:\Users\panye\SengokuChronicle-M2`, branch `feature/m2-world-map`, with `FoundationScene` loaded.
- The existing M3 branch remains separate. Nothing has been merged to `main`.

## Verification

- Unity Editor compiled scripts after opening the project. The EditMode Test Runner found 73 tests; all 73 passed with 0 failures.
- The Editor Console showed 0 errors and 0 warnings after tests and Play Mode.
- Play Mode verified travel from Kiyosu to Atsuta: the preview showed one day, confirmation advanced the date from 1560-04-01 to 1560-04-02, and the town menu opened. Returning to the map restored the travel view and marked Atsuta current.
- Play Mode rejected direct travel from Atsuta to Nagoya. Selecting Kiyosu showed a one-day preview; cancelling preserved the date and location.
- The test and Play Mode walkthrough ran on the M2 implementation commit `1bc086c`. No standalone player build was run.
- No scene or code edits were made during verification.

## Preserved worktree changes

The M2 worktree already contained uncommitted editor files before verification. They remain untouched: `.vscode/settings.json`, `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`, `.audit_scene_tree.json`, and `ProjectSettings/SceneTemplateSettings.json`.

## M2 scope and next step

- M2 includes the three-location map, travel preview/confirm/cancel, and a town menu with locked facility placeholders.
- Characters, missions, and save/load are later Phase 1 milestones.
- The demo worktree is now frozen for user evaluation. After the user says **Accepted**, merge and tag the M2 milestone, then begin M3. Until acceptance, preserve this branch and commit.
