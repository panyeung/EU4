# Month turn and daily clock

Time in a campaign moves one day at a time or by ending the month. The clock controls sit on
the main HUD, and they refuse to advance while a decision is pending, so the refusals are
user-visible behavior worth proving.

Status: `CLOCK-1` and `CLOCK-2` proven on 2026-11-10 (`1560 年 4 月 1 日` → `推进一天` →
`1560 年 4 月 2 日` → `结束月份` → `1560 年 5 月 1 日`). `CLOCK-3` and `CLOCK-4` are still mapped
from the code and older playtest guides, not driven.

## Sub-features

- `CLOCK-1` — `推进一天` advances the campaign date by one day and re-renders the date HUD.
- `CLOCK-2` — `结束月份` advances to the next month.
- `CLOCK-3` — while a succession choice is pending, time does not advance and the HUD explains
  why.
- `CLOCK-4` — while a minor heir awaits the adult handoff, time does not advance and the HUD
  asks for `完成成年交权`.

## How to get to it (user POV)

Start a campaign. `推进一天` sits on the clock panel and `结束月份` inside the month-turn
panel beneath it; both are on the main HUD once the title and scenario screens are behind you.

## Driving it with VerifyUi

Preconditions: a running campaign (see `title-new-game.md`, steps through
`固定三代剧本（SC-013）`).

- List the clock controls: `python .opencode/skills/verify/scripts/unity-run.py buttons`
  → expect `推进一天 | Canvas/WorldClockPanel/AdvanceButton` and
  `结束月份 | Canvas/WorldClockPanel/MonthTurnPanel/EndMonthButton`. If they are absent, the
  current page hides them: select the matching nav entry first and re-list, then record which
  page they live on.
- Record the date:
  `... probe Canvas/M02DailyActionRoot/M02TopBar/M02HeaderDateText` → `1560 年 4 月 1 日`.
- Advance one day: `... click Canvas/WorldClockPanel/AdvanceButton`
  → the date HUD shows the next calendar day.
- End the month: `... click Canvas/WorldClockPanel/MonthTurnPanel/EndMonthButton`
  → the date HUD moves to the following month.
- Refusal path: make a succession decision pending, then click `推进一天`
  → the date is unchanged. Quote the date before and after, plus the clock panel's status
  text and the console entry that names the reason.

## Gotchas

- The date a player reads is `Canvas/M02DailyActionRoot/M02TopBar/M02HeaderDateText`, not the
  `Canvas/M2WorldMapRoot/CurrentDateText` world-map line, which only exists on the map page.
- A refusal is a `PASS` only when the date is provably unchanged. Quote the date, not just the
  message.
- Advancing a month runs settlement work (industry, education, research). A failed settlement
  rolls the month back and warns in the console; treat the warning as a finding.
- The succession modal draws over the clock panel, so a click while it is open proves nothing
  about a refusal.
- `unity command editor_focus` before any capture, or the image can be a stale frame.
