# SC-004 Monthly Time Design

## Goal

Add a deterministic month-end action to the current calendar so the next FoundationScene Play Mode check can advance strategic time by one monthly turn.

## Current behavior

`GameDate` and `WorldClock` use a Gregorian calendar from 1560-01-01 through 2000-12-31. Travel and the existing clock panel advance by days. The GDD defines strategic time as one monthly turn and the next milestone as SC-004.

## Approaches

1. **Add a month-end operation and a separate UI action (recommended).** Keeps travel and the accepted day control intact; exposes the new monthly turn for a direct Play Mode check. Its cost is one small UI/scene updater.
2. Replace the day control with month-end. Fewer UI changes, but removes an existing interaction and makes day-level clock checks harder.
3. Build the entire monthly settlement pipeline now. The GDD's later work, economy, growth, and event models are not implemented; building stubs would create fake behavior and exceed SC-004's current dependencies.

Use approach 1. The end-month action moves any valid current date to day 1 of the next calendar month. It fails without changing the date when the next month is outside the supported range. Existing day advancement and travel behavior remain unchanged.

## Scope

- Add the pure-C# month transition to `WorldClock`, expose it through `GameSession`, and retain the existing date bounds.
- Add a separate “结束月份” action to the existing clock UI; on success, refresh both clock and map date displays.
- Provide a transactional Editor updater for the FoundationScene control, using one fixed generated root, one Undo group, validation before save, and Undo rollback on failure.
- Add focused EditMode coverage for month boundaries, December rollover, supported-range failure, no mutation on failure, UI callback/refresh, and updater idempotence/validation.

## Explicit deferrals

This clock-only SC-004 slice does not execute work, action budgets, missions, economy, aging/growth, succession, diplomacy, or events. Those need their domain rules and later GDD milestones. It does not modify SaveData format. The existing day-based travel API remains the only travel clock path.

## Play Mode acceptance for this slice

In the existing FoundationScene, start at 1560-04-01, click “结束月份” once, and observe both date displays become 1560-05-01. Verify the existing one-day action still moves one day, and verify travel still advances by its configured day count. No scene, Play Mode, or user-acceptance claim is implied until exercised in the already-open Editor.

## Constraints

- Unity 6000.6.6f1, C#, Universal 2D; no packages or external code.
- Keep Domain free of Unity APIs.
- Do not edit unrelated dirty files, commit, push, merge, or tag before user Play Mode acceptance.
- Use the already-running Editor for scene update and verification; never launch batchmode against this project.
