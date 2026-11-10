# Title screen, scenario select, and new game

The way into a campaign: the title overlay offers `继续游戏` and `开始新游戏`, `开始新游戏`
opens the scenario selector, and choosing a scenario drops the player into a running 1560-04-01
campaign with the HUD up.

Proven on 2026-11-10 — see `Logs/verify/20261110-2332-title-new-game.md`.

## Sub-features

- `TITLE-1` — the title overlay shows the campaign mark `影之一族`, the line `守护家名，传承百年`,
  `继续游戏`, `开始新游戏`, and a save-status line.
- `TITLE-2` — `开始新游戏` opens `选择剧本`, not a campaign.
- `TITLE-3` — the selector offers three main-line entries (`固定三代剧本（SC-013）`,
  `SC-010 标准样例`, `SC-010 危机示范`), five topic demos (SC-020 … SC-024), and `返回`.
- `TITLE-4` — choosing `固定三代剧本（SC-013）` starts the campaign: the overlay is gone, the date
  reads `1560 年 4 月 1 日`, and the location reads `所在 清洲`.
- `TITLE-5` — `返回` from the selector returns to the title overlay without starting anything.

## How to get to it (user POV)

Enter Play Mode in `FoundationScene`. The overlay is the first thing drawn. There is no other
way in; the campaign is unreachable without passing the selector.

## Driving it with VerifyUi

Preconditions: doctor passes, the active scene is `FoundationScene`, and this is a fresh
Play Mode entry.

- List controls: `python .opencode/skills/verify/scripts/unity-run.py buttons`
  → `继续游戏 | Canvas/M6TitleRoot/M6MainMenuRoot/M6ContinueButton` and
  `开始新游戏 | Canvas/M6TitleRoot/M6MainMenuRoot/M6NewGameButton`. Absent means the overlay
  failed to build: record `FAIL` with `texts` output and console errors.
- Quote the overlay: `... texts`, then `... probe Canvas/M6TitleRoot/M6MainMenuRoot/M6TitleLabel`
  → `影之一族`; `... probe .../M6TitleStatusText` → the save-status line.
- Capture `<stamp>-title-1-overlay.png` (after `unity command editor_focus`).
- Open the selector: `... click Canvas/M6TitleRoot/M6MainMenuRoot/M6NewGameButton`
  → `clicked '开始新游戏'`.
- Assert the selector, not a campaign: `... buttons` lists
  `Canvas/M6TitleRoot/M6ScenarioSelectRoot/SC013FixedScenarioButton`,
  `.../SC013StandardSampleButton`, `.../SC013CrisisDemoButton`, `.../SC020EducationDemoButton` …
  `.../SC024FamilyDoctrineDemoButton`, `.../M6ScenarioBackButton`.
  Capture `<stamp>-title-2-selector.png`.
- Start the campaign: `... click Canvas/M6TitleRoot/M6ScenarioSelectRoot/SC013FixedScenarioButton`
  → `clicked '固定三代剧本（SC-013）'`.
- Read the resulting state:
  `... probe Canvas/M02DailyActionRoot/M02TopBar/M02HeaderDateText` → `1560 年 4 月 1 日`;
  `... probe Canvas/M02DailyActionRoot/M02TopBar/M02HeaderLocationText` → `所在　清洲`;
  `... probe Canvas/M6TitleRoot/M6MainMenuRoot/M6TitleStatusText` → no active object.
  Capture `<stamp>-title-3-campaign.png`.

## Gotchas

- The labels are `开始新游戏` and `继续游戏`. `新游戏` / `继续` do not exist on this build, and
  older playtest guides that quote them are stale.
- `开始新游戏` alone does not start a campaign. A run that stops at the selector has proven
  nothing about the campaign HUD.
- The overlay's buttons are created at runtime, so Edit Mode `buttons` output never contains
  them. Only judge the overlay while playing.
- `M6ScenarioSelectRoot is missing; starting SC-013 directly` is a warning from a *unit test*,
  not from Play Mode. Do not read it as evidence about this flow.
- `TownNpcButtonTemplates/TownNpcRowButtonTemplate` and `.../TownNpcOptionButtonTemplate` are
  active templates, not user controls.
- After entering the campaign, the M02 daily-action page is the default screen; the succession
  HUD is not visible until `家族与继承` is selected.
