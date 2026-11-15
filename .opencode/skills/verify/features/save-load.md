# Save and load

Saving writes the campaign and loading restores it. The user-visible promise is fidelity:
after a load, the campaign looks and behaves like it did at save time, including a pending
decision.

Status: a 2026-11-14 SC-013 Play Mode run exercised `SAVE-1` and the date/action-state
portion of `SAVE-2` through the map UI using an isolated test store. It saved at
`1560-05-02`, advanced to `1560-05-03`, then loaded back to `1560-05-02`; the head's
completed training remained idle and the panel reported `已载入存档`.
See [the M02 record](../../../../PROJECT_STATE.md#m02-每日行动与旅行基础2026-11-06).
That route did not establish every head/location/HUD field, pending succession (`SAVE-3`),
or the empty-store refusal (`SAVE-4`). Those checks remain open for this recipe.

## Sub-features

- `SAVE-1` — `存档` reports success and the status line stops reading `未存档`.
- `SAVE-2` — `读档` restores the saved campaign: head, date, location, and HUD values match.
- `SAVE-3` — a pending succession decision survives save and load.
- `SAVE-4` — loading refuses cleanly when there is no save, instead of resetting the campaign.

## How to get to it (user POV)

`存档` and `读档` sit with the map HUD under the status line
`Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5SaveStatusText`, which reads `未存档` on a fresh run.
Reach the page through `地图与出行` on the nav rail.

## Driving it with VerifyUi

Preconditions: a fresh campaign; `M5SaveStatusText` reads `未存档`.

- Open the page: `python .opencode/skills/verify/scripts/unity-run.py click
  Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavMapButton`.
- Record the state to preserve: `... probe Canvas/M2WorldMapRoot/CurrentDateText`,
  `... probe Canvas/M2WorldMapRoot/LocationMarkerText`,
  `... probe Canvas/SuccessionRoot/SuccessionHud/SuccessionHeadText` (from `家族与继承`), and
  `... probe Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5SaveStatusText`.
- Save: `... click Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5SaveButton`
  → `M5SaveStatusText` no longer reads `未存档` and reports success. Capture
  `<stamp>-save-1-saved.png`.
- Mutate something a player can see — advance a day or travel to another town — and quote the
  changed date or location.
- Load: `... click Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5LoadButton`
  → date, location, and head match the pre-save values again. Capture
  `<stamp>-save-2-loaded.png`.
- Pending-decision path: open `退休并安排继承`, select a candidate, do not confirm, `存档`,
  change the selection, `读档` → the same pending decision and selection come back.

## Gotchas

- Save state is real user data. Start from a new game; never point a verification run at a
  campaign the user cares about. Bind the panel to an isolated test store before pressing
  Save or Load; a new campaign alone does not protect the default player slot.
- `未存档` is the only trustworthy empty-state signal. A save that leaves that text did not
  save.
- Compare against values recorded earlier in the same run, never against a campaign the run did
  not create.
- Read the state fields, not just the status line: the status line can report success while
  the wrong state was restored.
- After `读档`, re-read `buttons` before the next click; a load rebuilds panels and invalidates
  every captured path.
- `unity command editor_focus` before any capture, or the image can be a stale frame.
