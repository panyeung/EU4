# Tactical mission

M06 is one optional, one-time infiltration: the player sends two eligible 清洲 family
members into a fixed 8x6 grid, takes the dispatch at (6,4), leads both operatives to the
extraction cells (0,0) and (1,0), and returns to management with the applied supplies and
the success reward recorded in the family chronicle.

Status: IMPLEMENTED, SCENE INTEGRATION COMPLETE, PLAY MODE FUNCTIONAL AND PAGE VISUAL CHECKED.
The 2026-11-11 rebuild/save is the earlier record; the current panel generation/save record is
the 2026-11-12 verification run, which records the M06 panel generated and saved in the
existing `FoundationScene`, with the Foundation Scene Validator passing with 0 errors. The current scene contains the restyled page and a live
`WorldMapController.tacticalMissionPanel` binding. After the sight-cue fix, the current source
compiled in the existing Unity Editor; EditMode passed `TacticalMission` 84/84 and
`ShadowEscortCommission` 65/65. After these EditMode runs, the Editor Console had 0 errors and
10 warnings: nine expected rollback warnings from negative-path tests, and one existing CS0618
warning in `ReferenceGalleryNavigator`. During the Play Mode session, the Console reported 0
current errors, warnings, and logs; its cumulative counters were 30 errors and 107 warnings.
In Play Mode the lead reached the SC-013 campaign, opened the 清洲 town menu, visually checked
that `潜入密令` is clear of `护送委托` and `返回地图`, and opened the page. The page returned
`状态：待出发（清洲本家）`, `日期：1560-04-01（清洲）`, `资金 30　物资 12　情报 0`, and the
complete sight legend. The lead exercised both terminal routes, active-operation save/load
blocking, success save/load persistence, and replay blocking. At 1280x720, the page fit the
Game view and its text was readable after the title/tactical-overlay ordering fix. The
candidate viewport showed a partial next row at its lower edge; candidate-list scrolling
remains unverified. On 2026-11-12 the M07 manual/delegated wrapper was exercised live: a
delegated run resolved `SafeFailure`, and a manual tactical run that withdrew also resolved
`SafeFailure`; both left every operative uninjured and alive. The records do not capture
preparation/captain for both routes, and the delegated `-3` supplies is consistent with
scouting although the panel default is no preparation, so input, supply-cost and full-effect
parity are not claimed.
Separate delegated and manual severe-failure routes injured both operatives; a further manual
route reached and verified the alert-after-dispatch extraction branch. All are recorded in the
M07 section below.

## Sub-features

- `TM-1` — the town entry `潜入密令` opens the page and `关闭` hides it without advancing a
  turn, moving the party, changing the date or spending resources.
- `TM-2` — the operative list shows the 清洲 family members with a reason on every disabled
  row, and `开始潜入` stays disabled until exactly two eligible members are selected.
- `TM-3` — one of `北`, `东`, `南`, `西`, `移动`, `疾行`, `等待` or `夺取` is one turn
  followed by one deterministic guard response; an invalid command changes nothing.
- `TM-4` — after the guard response the guard facing, sight cells, alert line, event line and
  turn counter update on the same page.
- `TM-5` — `撤离` succeeds only with the dispatch carrier and both operatives on the two
  extraction cells, and `撤退` always ends in withdrawal. The shared terminal settlement charges
  the base 2 supplies once, plus 1 more when the run selected scouting preparation, plus another
  1 when the result is a costly success (so 2, 3 or 4 supplies); the success reward
  is paid once. Nothing is
  charged at briefing or start.
- `TM-6` — the save controls are disabled for an active operation, and a terminal result
  survives an ordinary save/load; the mission cannot be replayed after it settles.

### Rendered legibility contract

The first live 1280x720 capture read as tiny pale text on one broad navy slab. The page is
now a parchment sheet with a single indigo header band, and the text scale is measured
rather than eyeballed. `AddM06TacticalMissionPanel` owns the geometry;
`TacticalMissionPanel.CellFill` owns the cell fills.

- Page surface `SengokuUiTheme.Parchment` `#EFE4CC`; a 32 unit `IndigoDeep` `#18202E` header
  band carries the title and the `开始潜入` / `关闭` buttons, and a 2 unit `Gold` `#C3A469`
  rule closes the band. No other navy area sits behind body text.
- Title 22 px `Gold` on the band, 6.88:1. Body 13 px `Ink` `#332E26` on parchment, 10.67:1.
  Cell labels 13 px `ParchmentLight` on every cell fill, at least 6.23:1. Button labels
  13 px `ParchmentLight` on `IndigoPanel`, 12.39:1. The old `GoldDim` `#8E7A51` selection and
  chronicle lines measured 3.29:1 on parchment and failed AA, which is why that colour is
  gone from this page. Gold on parchment is only 1.88:1, so every pale label must stay on
  the band; `Validate` and the contrast test both assert that containment.
- Vertical budget: the town band is 268 units, so the page is 260. The body is one 600 wide
  column: a 32 unit two-line objective plus ten 16 unit rows at a 16 unit pitch, inset 2
  units below the gold rule and 4 above the command strip. That is the largest body size
  that keeps ten one-line rows and the objective inside the band. The briefing hint and
  the operation party share one row because the panel never shows both.
- Cells keep a 54x32 grid with 2x1 spacing; the 8x6 block is 446x197 and stays inside the
  page.
- The band carries a 13 px legend, `图例　壁=墙　掩=掩体　撤=撤离　报=密报　忍=潜入者　守=守卫　目=视野`.
  Guard sight is never colour-only and never replaces what a cell already means. Every cell
  in sight composes the cue into its own label: the terrain or occupant glyph stays in front
  and `目` is appended, so a sighted wall reads `壁目`, a sighted dispatch `报目`, an
  operative `忍一目` or `忍报目`, a guard `守一目` or `守二目`, and an otherwise unmarked
  sighted cell is just `目`. `TacticalMissionPanel.CellMarker` owns that composition, so the
  whole sight boundary reads in monochrome. The glyph is the universal cue; the vermilion
  fill tint is secondary and augments the unmarked sight cells of every terrain kind (floor,
  wall, cover, extraction). A marked dispatch, operative or guard cell keeps its own occupant
  fill and carries the sight cue in its label alone.
- Type floor: the updater accepts a town only at 1200x260 or larger, and at exactly that size
  the page fits at scale 1, so the authored 13 px body type is the rendered size for every
  accepted layout. A narrower window than the accepted minimum is a runtime degradation and
  the page shrinks instead of clipping, never below `TacticalMissionPanel.MinimumPageScale`.
- The saved scene keeps `LegacyRuntime.ttf` because an OS font cannot serialize. While
  playing, `Refresh` applies `SengokuUiTheme.Serif` to the panel root, so the Play Mode
  capture should show the project's CJK serif, not the built-in fallback.

### Regenerating the saved page

With the Editor idle and the scene saved clean, run
`Tools/Sengoku Chronicle/Add M06 Tactical Mission Panel`. The updater handles three states
in one Undo group:

- no page and no live binding: builds the page.
- a valid page: no-op, so a repeat run never rewrites a current page.
- a page it generated in an older revision (complete and fully bound, but failing
  `Validate`): destroys and rebuilds it. A failed rebuild reverts the whole group and the
  old page returns. A hierarchy that is not a complete generated page is refused with
  `ExistingTacticalPanel` rather than replaced.

A binding left behind by a deleted root reads null while the YAML can hold a stale entity id;
the updater no longer treats that id as proof of a second panel, and it overwrites the
binding on rebuild. That deleted-root recovery is covered in an isolated preview-scene test.
The `FoundationScene` saved by the 2026-11-12 verification run has a valid page and passes the
Validator; a repeat generator run is a no-op. Never run the generator on a dirty scene; it refuses, and the lead
owns that save.

## How to get to it (user POV)

Start a campaign, stand in 清洲, open `地图与出行` on the left nav rail, and click the 清洲 row
on the map. In the town menu the new entry `潜入密令` sits at the right edge above the
`返回地图` row and below the top-right `护送委托` entry, clear of both. Clicking it opens
`潜入密令：夺取密报`: a parchment sheet with a navy title band across the top, the 8x6 grid
on the left, the objective and status rows with the operative list on the right, and a
horizontal command strip along the bottom.

Play Mode functional checks pass for the town entry, both terminal routes, save/load, and
guard sight labels. The town menu capture showed that `潜入密令` sits between `护送委托`
and `返回地图`. Its page opens with the expected date, resources, state, and legend. The
live sight route labels retain their cell glyph and end with `目`. The EditMode matrix also
covers walls (`壁目`), dispatch (`报目`), operatives (`忍一目`/`忍报目`), and guards
(`守一目`/`守二目`). The page-level layout and text readability passed; candidate-list scrolling remains unverified. A leftover screenshot from the 2026-11-12 M07 run, `Assets/Logs/verify/_scratch/20261112-m07-start.png`, plus its `.meta` remains in the worktree and is pending manual cleanup because automated deletion was blocked; it is not evidence for any route.

## Driving it with VerifyUi

Status: partial. On 2026-11-11 both mission outcomes, active-operation save blocking,
successful save/load persistence, and replay blocking were exercised in Play Mode. The page
layout and text were visually checked at 1280x720; candidate-list scrolling remains pending.
The leftover `Assets/Logs/verify/_scratch/20261112-m07-start.png` and its `.meta` are
pending manual cleanup; never present them as evidence.

Preconditions: a fresh `FoundationScene` Play Mode run reached through `开始新游戏` →
`固定三代剧本（SC-013）`, date `1560 年 4 月 1 日`, location 清洲, and the generated
`M6TacticalMissionRoot` present under the town menu. The lead owns the generator run; the
task worker does not run it.

Paths, with `P` = `Canvas/M2WorldMapRoot/TownMenuRoot/M6TacticalMissionRoot` and `Q` =
`P/M6TacticalMissionPage`. The lead has already generated and saved the restyled page in the
clean `FoundationScene`; use it directly for capture. If it is later intentionally removed,
the generator can recreate it from a clean scene before Play Mode.

- entry: `P/M6TacticalOpenButton` (label `潜入密令`)
- close: `Q/M6TacticalCloseButton` (label `关闭`)
- start: `Q/M6TacticalStartButton` (label `开始潜入`)
- commands: `Q/M6TacticalNorthButton`, `Q/M6TacticalEastButton`, `Q/M6TacticalSouthButton`,
  `Q/M6TacticalWestButton`, `Q/M6TacticalMoveButton`, `Q/M6TacticalSprintButton`,
  `Q/M6TacticalWaitButton`, `Q/M6TacticalInteractButton`, `Q/M6TacticalExtractButton`,
  `Q/M6TacticalWithdrawButton`, `Q/M6TacticalSwitchButton`
- candidate row: `Q/M6TacticalCandidateScroll/M6TacticalCandidateViewport/M6TacticalCandidateContent/TacticalCandidate_<PersonId>`
- readings: `Q/M6TacticalStateText`, `Q/M6TacticalDateText`, `Q/M6TacticalTeamText`,
  `Q/M6TacticalStatusText`,
  `Q/M6TacticalGuardText`, `Q/M6TacticalResultText`, `Q/M6TacticalEventText`,
  `Q/M6TacticalResourceText`, `Q/M6TacticalChronicleText`

Remaining check:

1. Exercise scrolling in the candidate roster viewport if verifying access to all eligible
   operatives is in scope. The page-level layout and legibility were checked at 1280x720.
   If a capture is needed, save it under `Assets/Logs/verify/_scratch/`, inspect it, then
   delete the image and its `.meta` immediately; do not leave a new scratch capture behind.
   The existing `20261112-m07-start.png` and `.meta` are already pending manual cleanup and
   must not be presented as this feature's evidence.

The recorded Play Mode evidence is in `Logs/verify/20261111-m06-tactical-mission.md`. It
records the success route and save/load check from the first fresh SC-013 campaign, and the
withdrawal route from a second campaign. The original save-slot files were restored and
their SHA-256 hashes match the pre-test backups.

### M07 shared settlement (verified live 2026-11-12)

On 2026-11-12 the existing Unity 6000.6.6f1 Editor exercised both entries. The delegated run
resolved 失败并安全撤离 (failure with safe extraction): `-3` supplies, concealment `-2`,
`faction.intercepted-dispatch-target` relation `-3`, and no injuries or death. The manual run
took tactical input and then withdrew; it settled `SafeFailure` with no injuries or death.
The record does not capture preparation/captain or per-effect values for the manual route, so
supply-cost and input parity with the delegated route are not claimed; only the shared
`SafeFailure` result is established for both. Earlier routes cover terminal M05/M06 save/load; a separate 2026-11-12 M05 manual route
(`Logs/verify/20261112-1324-m07-m05-save-load.md`) also covers one terminal M05 save/load, while the M06 routes above did not save or load. A separate delegated route
selected 明智 千恵 + 佐近 with scouting preparation and 佐近 as captain, and resolved
严重失败: fame `0 → 0` (the `-3` change clamped at the 0 minimum), concealment `50 → 45`,
the intercepted-dispatch relation `0 → -5`, supplies `12 → 9` (base 2 plus scouting 1), and
both operatives injured with neither dead. After the 1560-05-01 month boundary both injured
operatives showed `可指派` in the M05 escort commission panel, which is the recovery evidence. That
route did not verify the death roll, post-death succession, other M06 outcomes, M05
settlement, save/load or visuals. Full success and costly success are verified by the two
extraction routes below.
A separate manual attempt selected 明智 千恵 + 佐近, `亲自执行`, no preparation and 千恵 as
captain; the pair started at `(0,0)` / `(1,0)`, the lead switched to 佐近 and sprinted East
twice. On round 2 the patrol heard the sprint and entered `Investigating` at `(6,0)`; after
one `Wait`, round 3 moved the guard to `(5,0)`, raised `Alert` and caught 佐近, settling
严重失败 immediately. Fame went `0 → 0` (clamped), concealment `50 → 45`, the target faction
relation `0 → -5`, supplies `12 → 10` (base 2 only), and funds/intelligence stayed unchanged;
both operatives were injured and neither died. No save/load was performed and no screenshot
was captured. The Editor was stopped with
`FoundationScene` clean and zero new Console entries. This attempt did **not** reach or
validate the “alert then retreat → costly success” branch; that branch is verified separately
by the extraction route below.
The earlier two M06 routes did not capture the frozen preparation/captain choices or a fame
delta; both severe-failure supplements did. See `docs/reports/m07-runtime-verification.md` and
`Logs/verify/20261112-1156-m07-severe-failure-injury-recovery.md`.

A further manual route verified the alert-after-dispatch extraction branch. In a fresh
in-memory SC-013 run at 清洲 on 1560-04-01 the party was 明智 千恵 + 佐近 with `亲自执行`, no
preparation and 千恵 as captain. 佐近 quietly moved north five steps to `(1,5)`, east to
`(4,5)`, south to `(4,4)`, east to `(5,4)`, and took the dispatch from cover, then moved north
to `(5,5)`. On round 12 a stationary guard spotted him at `(6,4)` and raised `Alert` while the
mission stayed `InProgress`. Carrying the dispatch, he sprinted west twice to `(1,5)`, south
twice to `(1,1)`, walked south onto `(1,0)`; 千恵 stayed on `(0,0)`. Extraction succeeded on
round 18 and settled 成功但有代价 (`CostlySuccess`): `+4` funds, `-3` supplies (base 2 plus
the alert cost 1), `+6` intelligence, fame `0 → 2`, concealment `50 → 51`, target faction
relation `0 → 2`, and both operatives uninjured and alive. The chronicle names Manual, the
1560-04-01 start, round 18, and the itemized costs and rewards. No save/load or screenshot was
performed; the Editor was stopped with the scene clean and zero new Console entries. This route
is distinct from the recorded manual-withdrawal `SafeFailure` route.

A separate manual route verified the no-alert full-success branch. In a fresh in-memory
SC-013 run at 清洲 on 1560-04-01 the party was 明智 千恵 + 佐近 with `亲自执行`, no preparation
and 千恵 as captain. 佐近 moved north to `(1,5)`, east through `(2,5)` to `(4,5)`, south to
`(4,4)`, east to `(5,4)`, and took the dispatch while concealed; the guard stayed on `Patrol`
throughout. He returned west to `(2,4)`, south through `(2,3)`, `(2,2)`, `(2,1)` to `(2,0)`,
then west onto `(1,0)`, while 千恵 stayed on `(0,0)`. Extraction succeeded on round 20 and
settled 完全成功 (`FullSuccess`): `+4` funds, `-2` supplies, `+6` intelligence, fame
`0 → 3`, concealment `50 → 53`, target faction relation `0 → 5`, with both operatives alive
and uninjured. The chronicle names Manual, the 1560-04-01 start, round 20, and the itemized
effects. No save/load or screenshot was performed; the Editor was stopped with the scene clean
and zero new Console entries. This route is separate from the recorded manual-withdrawal, alert-extract and
failure routes and establishes no manual/delegated parity, determinism or M07 acceptance.

The briefing now builds four explicit runtime uGUI choice rows from the authored candidate
template, under the existing candidate container. They are not scene objects, so no scene or
prefab change is required:

- `TacticalMode_Manual` — 亲自执行（进入战术潜入）
- `TacticalMode_Delegated` — 自动委派（立即按冻结数值结算）
- `TacticalPreparation_None` — 不准备（额外 0 物资，加值 0）
- `TacticalPreparation_Scouting` — 侦察路线（额外 1 物资，加值 +10）
- `TacticalCaptain_<personId>` — one row per selected member; the first selected member is
  the default captain.

The selected rows show `✔`. Starting freezes the mode, preparation and captain into the mission
request. Starting or opening still spends nothing; the shared terminal settlement charges the applicable total once: base 2 supplies, plus
1 for scouting, plus 1 for a costly success (2, 3 or 4); it pays `+4` funds / `+6`
intelligence only on full or costly success.

Manual mapping: clean extraction → 完全成功; extraction after any guard reached alert →
成功但有代价 (extra 1 supply); withdrawal → 失败并安全撤离; capture → 严重失败. The mapping is
wired in `TacticalMissionResolutionRules.TryMapManual`. Manual play
freezes no automatic roll (`roll == 0`, `score == 0`) because the board decides the result.
Delegated play uses the approved score `floor((忍术A + 忍术B)/2) + clamp(情报,0,10) + 准备加值 +
d100 - 50` with thresholds 90/65/15 and no partial result.

The result line reads the last M06 `MissionRecord` and shows the shared result plus fame,
concealment and each participant's death/injury state. Severe failure runs one independent
10% death check per disclosed member; only listed members can die, and survivors become
injured. Injured people cannot start physical or infiltration missions and recover at the next
month boundary regardless of that settlement's success.

`M07MissionIntegrationTests` and the new panel cases run in the same EditMode suite that passed
1710/1710 on 2026-11-12. The live routes above cover the delegated safe failure, the manual
withdrawal, the delegated severe-failure/injury-recovery route, the manual severe-failure
attempt, the manual alert-after-dispatch extraction route, and the manual no-alert full-success
route; do not extend that evidence to the other M07 paths listed as unverified.

## Gotchas

- The page and every panel under it start hidden and the town root itself is inactive in the
  saved scene, so a probe on `Q/...` answers `(no active object at '<path>')` until the page is
  open. Read the town entry first, then the page.
- `VerifyUi` lists only active buttons, and the briefing keeps `M6TacticalStartButton` active but
  not interactable until two operatives are selected. Read its interactable column together with
  `Q/M6TacticalSelectionText`; a missing row means something else hid it.
- Command buttons are interactable only during an active operation. On the briefing they can be
  present but disabled, so a `False` interactable value before `开始潜入` is expected.
- `WorldMapController.ToggleTacticalMissionPanel` raises the page root to the last sibling and
  then raises `返回地图` above it. A covered entry means the rects collide, not that the raise
  order is wrong.
- Do not run the M06 generator to "check" a built scene. The generator refuses to run on a
  dirty scene, and the lead owns scene generation and saving.
- A terminal result is one-time per campaign through the existing family chronicle, so a
  replay attempt in the same campaign must be refused; use a fresh SC-013 campaign per route.
- Saving is real user data with one slot. Copy `save-slot-1.json` and `save-slot-1.json.bak`
  outside the project before the storage step, restore both afterwards, and compare SHA-256.
- `unity command editor_focus` before every capture, or the image can be a stale frame.
- Candidate rows are deactivated immediately when the briefing closes or starts. The next
  refresh/clear destroys the retired objects; do not probe a `TacticalCandidate_*` path after
  `开始潜入`.
