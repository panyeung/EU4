# Succession

Retiring the head and appointing a successor is the campaign's core consequence loop. A
lawful candidate takes over exactly once, permanent person and family identities survive, and
a minor heir needs a regent plus an explicit adult handoff before gaining control.

Status: `SUCC-1` proven on 2026-11-10 — `SuccessionHeadText` read
`当代家主：明智 晴信（70岁）` on the `家族与继承` page. The handoff itself is gated by pending
events: the HUD answered `尚有事件等待选择：请先解决当前事件，再交接家督。`, then after resolving one
event, `第一代尚未回应「雨夜来客」：请再结束月份，处理旧客来访。` Reaching a completed transfer needs
several month-ends and event resolutions (one extra month per scripted chain). Evidence:
`Logs/verify/20261110-2341-month-turn-travel-succession.md`.

## Sub-features

- `SUCC-1` — the HUD names the current head with age and offers `退休并安排继承`.
- `SUCC-2` — retiring lists only lawful candidates; dead, departed, and disqualified people
  are absent.
- `SUCC-3` — confirming a candidate transfers control once: the head text changes, the
  completed record is reported, and date, location, mission stages, merit, and NPC relations
  are unchanged.
- `SUCC-4` — a minor candidate requires a regent, and control passes only through
  `完成成年交权`.
- `SUCC-5` — `示范：无继承人危机` reaches the no-candidate path: one crisis adoption/rebuild
  choice, or an explicit terminal extinction that stops time and travel.

## How to get to it (user POV)

Select `家族与继承` on the left nav rail
(`Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavFamilyButton`). The succession HUD carries
the head line, the status line, `退休并安排继承`, and `示范：无继承人危机`.

## Driving it with VerifyUi

Preconditions: a running campaign. This feature mutates the campaign, so record the values it
promises to preserve first.

- Open the page: `python .opencode/skills/verify/scripts/unity-run.py click
  Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavFamilyButton`.
- Read the HUD: `... probe Canvas/SuccessionRoot/SuccessionHud/SuccessionHeadText`
  → `当代家主：<name>（<age>岁）`; `... probe .../SuccessionStatusText` → the current phase line;
  `... probe .../M7RankText` → `功名：<n> · <rank>（下一阶 …）`;
  `... probe Canvas/M02DailyActionRoot/M02TopBar/M02HeaderDateText` → the baseline date.
- Open the modal: `... click Canvas/SuccessionRoot/SuccessionHud/SuccessionRetireButton`
  → capture `<stamp>-succ-1-modal.png`, then `... buttons` to list the candidate buttons that
  were actually created.
- Assert the candidate set: exactly the lawful adults and minors are listed. A disqualifying
  name appearing here is a `FAIL`.
- Pick a candidate by its ASCII path from `buttons`, then confirm by label path from the same
  list (the modal's confirm control) → `SuccessionHeadText` names the new head and the status
  text reports the completed record. Capture `<stamp>-succ-2-completed.png`.
- Compare against the baseline: date, rank text, and location are unchanged by the transfer.
- Crisis path: `... click Canvas/SuccessionRoot/SuccessionHud/SuccessionCrisisDemoButton` in a
  separate fresh campaign; record which branch the modal offers.

## Gotchas

- The succession HUD is behind `家族与继承`; after `固定三代剧本（SC-013）` the default page is the
  M02 daily-action screen and `SuccessionHeadText` is not active.
- The modal covers the clock panel, so clock clicks while it is open prove nothing.
- Candidate buttons are created at runtime. Never hardcode their paths; read `buttons` after
  opening the modal.
- Confirming twice must not transfer twice.
- Older guides quote `当代家主：明智 晴信（32岁）`; this build reported the head at `70 岁` in the
  profile panel. Quote what the build shows, never the guide.
- `unity command editor_focus` before any capture, or the image can be a stale frame.
