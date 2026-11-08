# For agentic workers: execute this plan on the current main checkout. Do not create a branch or worktree, and do not implement rules beyond this plan.

# Character Profile and Daily Actions UI Slice

> **范围更新（2026-11-07）：** 此计划仍记录 M02 单页首片。用户已将范围扩展为五张参考图对应页面；后续实现按 [五页实现计划](2026-11-07-ui-reference-gallery.md) 执行。

## Goal
Adapt the existing M02 daily-action screen toward design-gallery page 02: a selected-character profile alongside action planning, in the game's established widescreen Japanese-paper / indigo / vermilion visual language. Keep current M02 action behavior and the M03 Inspector read-only.

## Architecture
Reuse `DailyActionPanel`, `NpcAiInspectorPanel`, `GameSession.DailyActions`, `GameSession.FamilyRoster`, and existing transactional Unity scene generators. Profile data must come from current session/domain values; do not invent stats or relationship values. Keep the Inspector a distinct developer-only, read-only surface. Existing Unity UI and built-in font are sufficient; no new package or external art.

## Tech Stack
Unity 6.0, C#, uGUI, Unity Test Framework. Existing scene generator patterns use one Undo group, validate before save, and Undo rollback on exceptions.

## Spec
- Present the selected family member as the focus, with existing display name, age, kinship/role, location or travel state, and current action/progress where available.
- Keep family-member selection and the existing Rest, Training, Work, Travel, duration, interrupt, and replan behavior intact.
- Reshape the panel into clear character-information and action-planning areas; use the existing panel palette and typography conventions. Remain usable at the 1280x720 test canvas without clipped or overlapping controls.
- Preserve the M03 Inspector's distinct developer/debug role, existing open/close behavior, pilot selection, and read-only boundary.
- Reuse any existing portrait assets if available; repository inspection found no character portrait art. Do not create or import placeholder art as part of this slice.
- Update the M02 transactional generator and its validation for the new layout. Keep all generated objects under the existing fixed root and preserve scene objects/GUIDs.
- Add or update EditMode assertions for selected-profile rendering, action behavior regression, scene bounds, and generator rollback/idempotency as relevant.
- Update the UI design spec/implementation plan with the delivered scope and any remaining work.

## Global Constraints
- OpenCode2 owns code changes. Work only in `C:\Users\panye\My project` on the current checkout. Preserve unrelated dirty WIP (`docs/superpowers/specs/2026-11-07-m04a-personal-memories-design.md`, `graphify-out/`, `nul`).
- Before Unity interaction, inspect Unity Editor process PID and `-projectPath`; use only the already-running Editor for this project. Do not use batchmode, do not touch `Library`/`Temp`, and preserve all `.meta` files and GUIDs.
- Do not change gameplay rules, action/session/save semantics, dependencies, assets, or ProjectSettings.
- Record compile, EditMode, scene-validator, Console, Play Mode, and acceptance evidence separately. Do not claim runtime evidence without running the existing Editor.
- Do not commit until the task's applicable verification gates pass. Then commit only files for this task and push the current branch. Do not merge, tag, or claim milestone acceptance without explicit user acceptance.

## Review Focus
- Profile content is sourced from real session state and remains synchronized on selection/date/action changes.
- Existing M02 commands, duration checks, feedback, travel gating, interrupt and replan semantics are unchanged.
- No profile or Inspector path mutates game state beyond the existing M02 action commands.
- Every interactive element remains within the panel at 1280x720; generated scene structure validates and rolls back cleanly.
- M03 Inspector remains read-only, hidden by default, usable, and distinct from player-facing profile content.

## Delivery status (2026-11-07)
- Delivered on the current `main` checkout: selected-member profile column, two-column M02 panel layout, updated transactional generator/validator, and EditMode coverage.
- Verification: 23/23 focused EditMode tests pass; M02 and M03 scene validation pass; Console 0 errors; Play Mode profile, action progress, and read-only Inspector observed in the existing Editor. See `docs/superpowers/specs/2026-11-07-ui-character-action-design.md` for evidence and remaining work.
- Remaining: optional portrait art (none in the repository), the other four gallery pages, and explicit user acceptance before merge/tag.
