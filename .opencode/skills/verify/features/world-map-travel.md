# World map travel

Travel is how the player moves between towns. The map page shows the current location, a
destination list, and a trip preview; committing a trip moves the location and advances the
date by the travel time.

Status: the 2026-11-14 SC-013 Play Mode follow-up verified `MAP-1` and `MAP-2`:
清洲 to 热田 previewed one day and arrival `1560-04-02`; confirming opened the 热田 town
view and advanced the HUD to that date. The run used Pipeline UI handlers and did not
check pointer hit-testing or map pixels. See the
[travel and month-boundary record](../../../../docs/archive/verification-and-research-history.md#m02-travel-and-month-boundary-play-mode-follow-up-2026-11-14).
The 2026-11-10 failure is historical. `MAP-3` still needs a recorded Play Mode check.
`MAP-4` describes the current-town control's behavior; its current source is
`WorldMapController.OnLocationClicked`, and it has not been independently replayed here.

## Sub-features

- `MAP-1` — selecting a destination shows its name, travel days, and arrival date.
- `MAP-2` — `确认出发` moves the location marker to the destination and advances the date.
- `MAP-3` — `取消` clears the preview and leaves location and date unchanged.
- `MAP-4` — clicking the town you are standing in opens its town view and clears the travel
  preview instead of starting a trip.

## How to get to it (user POV)

Select `地图与出行` on the left nav rail
(`Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavMapButton`). The map page carries the
location marker, the destination buttons, `确认出发`, and `取消`.

## Driving it with VerifyUi

Preconditions: a running campaign; the nav rail is visible.

- Open the page: `python .opencode/skills/verify/scripts/unity-run.py click
  Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavMapButton`.
- List the controls: `... buttons` → expect
  `Canvas/M2WorldMapRoot/DestButton_0` (`清洲`), `.../DestButton_1` (`热田`),
  `.../DestButton_2` (`那古野`), `.../ConfirmButton` (`确认出发`), `.../CancelButton` (`取消`).
- Record the baseline:
  `... probe Canvas/M2WorldMapRoot/LocationMarkerText` → `当前位置：<town>`;
  `... probe Canvas/M2WorldMapRoot/CurrentDateText` → `日期：1560-04-01`;
  `... probe Canvas/M2WorldMapRoot/DestinationNameText` / `...TravelTimeText` /
  `...ArrivalDateText` → `—` before a selection.
- Select a town you are not standing in, then
  `... probe Canvas/M2WorldMapRoot/FeedbackText` → the line names the town, the travel days,
  and the expected arrival date. Capture `<stamp>-map-1-preview.png`.
- Commit: `... click Canvas/M2WorldMapRoot/ConfirmButton`
  → `LocationMarkerText` names the new town and `CurrentDateText` advanced by the previewed
  number of days. Capture `<stamp>-map-2-arrived.png`.
- Cancel path: select a destination, `... click Canvas/M2WorldMapRoot/CancelButton`
  → the preview fields return to `—` and neither location nor date changed.

## Gotchas

- The preview is the proof, not the click. Quote the days shown and the date delta actually
  observed; a mismatch between them is the bug this feature exists to catch.
- Labels do not identify the index: `DestButton_0` is whichever town the scene wired. Read
  `LocationMarkerText` after arrival instead of trusting order.
- If `DestButton_*` is absent, the map page is not open — click
  `ReferenceNavMapButton` first, and re-list rather than guessing paths.
- `存档` / `读档` live under the same root (`M5SaveLoadRoot`), so a map run naturally pairs
  with `save-load.md`.
- `unity command editor_focus` before any capture, or the image can be a stale frame.
