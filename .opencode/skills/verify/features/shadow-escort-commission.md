# Shadow escort commission

The commission page is the player's one M05 job: the clan pays to escort a merchant
convoy, one family member executes it for three days, and a settled check pays funds and
supplies or records a failure in the chronicle.

Status: the M05 technical gate passed on 2026-11-11 in the existing Unity 6000.6.6f1
Editor with the pre-existing FoundationScene integration. The 2026-11-10 partial-success
route and the 2026-11-11 failure plus terminal save/load route both passed; the latter
restored the user's save files to their recorded SHA-256 values and a fresh read-only
FoundationScene Validator run passed with zero errors. A separate delegated full-success
route and settle-once check passed in Play Mode on 2026-11-12. Manual full success and
retired-assignee abandonment passed in separate Play Mode routes on 2026-11-14: SC-013
settled Manual at 92 (roll 82) and stayed settled on 04-05; SC-010 retired the assignee of an
active manual commission and abandoned it at the due date without shared rewards. Final user
milestone acceptance remains pending. See `docs/reports/m07-runtime-verification.md` and the
local records `Logs/verify/20261114-m05-manual-full-success.md` and
`Logs/verify/20261114-m05-retired-assignee-abort.md`.
The three-day window uses a close-page / return-to-map / advance / reopen round trip because
the town menu hides the clock and `VerifyUi` only lists active buttons.
The M07 delegated and manual entries were both exercised live on 2026-11-12 and each settled a
`Partial` outcome; the manual route matched the delegated route's visible result and tracked
effects for these two recorded routes only, so no general manual/delegated equivalence or
determinism is claimed. A further manual terminal save/load route saved and restored the settled
commission; final user milestone acceptance remains pending.

## Sub-features

- `COMM-1` — the offer is only acceptable at 清洲; `接取委托` deducts the creed investment once.
- `COMM-2` — the assignee list shows every living family member with a reason, and the
  work-duty rule disables minors, retired members, busy members and industry leads. Those
  disabled rows are asserted by `ShadowEscortCommissionPanelTests` in EditMode; the
  fresh-start route below starts with six adults, four eligible because NPC autonomy
  assigns two of them actions at campaign initialization.
- `COMM-3` — one eligible click starts the countdown and no second assignee is offered.
- `COMM-4` — the three-day window settles once, with score, roll, applied reward and a
  chronicle line, and never twice.
- `COMM-5` — the page never advances time, moves the party or spends resources on open,
  close or refresh.

## How to get to it (user POV)

Start a campaign, stand in 清洲, open `地图与出行` on the left nav rail, then click the
清洲 button on the map (`DestButton_*`, the row whose label reads 清洲). That opens the town
menu, where `护送委托` (`M5CommissionOpenButton`) sits at the top right of
`Canvas/M2WorldMapRoot/TownMenuRoot/M5ShadowEscortCommissionRoot`. The page
(`M5ShadowEscortCommissionPage`) carries the offer, creed, resources, assignee list,
`接取委托`, `关闭` and the outcome/chronicle lines.

The clock is not on screen while the town menu is open: `WorldMapController.OpenTownMenu`
deactivates `WorldClockPanel` and every map row, and `返回地图` restores both. So a player who
accepted the commission reads each day by closing the page, leaving the town menu, clicking
`推进一天` on the map, and walking back into 清洲 to reopen the page.

## Driving it with VerifyUi

Preconditions: a fresh `FoundationScene` Play Mode run reached through
`开始新游戏` → `固定三代剧本（SC-013）`, date `1560 年 4 月 1 日`, location 清洲, and the
commission root present in the existing scene. Do not run the scene generator: this panel is
already integrated and the prior gate made no scene edits. Run the Validator read-only before
Play Mode and record its timestamp separately from the route.
Record `<stamp>` from the first console call.

`VerifyUi` lists and clicks only active buttons, and `WorldMapController.OpenTownMenu`
deactivates `WorldClockPanel` and every map row. So `AdvanceButton` and `DestButton_*` are
absent from `buttons` while the town menu is open, and the game day can only move from the map
page. Every reading below happens with the page open, and every day is one close / return /
advance / reopen round trip. Paths used throughout, with `P` =
`Canvas/M2WorldMapRoot/TownMenuRoot/M5ShadowEscortCommissionRoot` and `Q` =
`P/M5ShadowEscortCommissionPage`:

- entry button: `P/M5CommissionOpenButton` (label `护送委托`)
- close: `Q/M5CommissionCloseButton` (label `关闭`)
- accept: `Q/M5CommissionAcceptButton` (label `接取委托`)
- candidate row: `Q/M5CommissionCandidateScroll/M5CommissionCandidateViewport/M5CommissionCandidateContent/CommissionCandidate_<PersonId>`,
  where `<PersonId>` is one of `person.sc013.harunobu`, `person.sc013.chie`,
  `person.sc013.masatsuna`, `person.sc013.nobumasa`, `person.sc013.tokihime`,
  `person.sc013.sakon`
- leave the town menu: `Canvas/M2WorldMapRoot/TownMenuRoot/ReturnToMapButton` (label `返回地图`)
- advance one day: `Canvas/WorldClockPanel/AdvanceButton` (label `推进一天`)
- time-turn refusal text: `Canvas/WorldClockPanel/MonthTurnFeedbackText`
- town row on the map: `Canvas/M2WorldMapRoot/DestButton_<n>` — click the row whose label in
  `buttons` reads `清洲`; the index is not part of the contract

- Open the map page: click
  `Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavMapButton` and confirm `buttons` now lists
  `Canvas/M2WorldMapRoot/DestButton_0`, `DestButton_1`, `DestButton_2`, `ConfirmButton` and
  `CancelButton`.
- Open the town menu: click the `DestButton_*` path whose label reads `清洲` (clicking the town
  you already stand in opens the town menu instead of previewing a trip). `buttons` now lists
  `Canvas/M2WorldMapRoot/TownMenuRoot/ReturnToMapButton` and the entry button, and no longer
  lists `AdvanceButton` or any `DestButton_*`.
- Open the page: click `P/M5CommissionOpenButton` → `Q/M5CommissionStateText` reads
  `状态：待接取（清洲本家）`, `Q/M5CommissionOfferText` names `武艺`, `管理`, `社交`, `3 个模拟日`
  and `可工作的在世家人`, and the `M5CommissionAcceptButton` row in `buttons` shows `True` in
  its interactable column. Capture `<stamp>-comm-1-offer.png`.
- Record the offer: `... probe Q/M5CommissionResourceText` (funds/supplies/intelligence),
  `... probe Q/M5CommissionCreedText` (creed and listed investment), and
  `... probe Q/M5CommissionOfferText`.
- Accept: `... click Q/M5CommissionAcceptButton` → `Q/M5CommissionStateText` reads
  `状态：已接取，等待指派执行人`, `Q/M5CommissionResourceText` funds dropped by the amount the
  creed line listed, and the candidate rows appear under
  `Q/M5CommissionCandidateScroll/M5CommissionCandidateViewport/M5CommissionCandidateContent`.
  Capture `<stamp>-comm-2-accepted.png`.
- Check the candidate rows: that content has six rows, one per living SC-013 family member
  (明智 晴信, 明智 千恵, 明智 正綱, 明智 信正, 明智 時姫, 佐近). Four are labelled
  `<name>（可指派）`; 正綱 and 信正 are disabled because `GameSession.InitializeNpcAutonomy`
  assigns them actions. `Q/M5CommissionAssigneeText` reads
  `执行人：待指派（可指派 4 人，共 6 名在世家人）`. The EditMode tests cover other
  disabled-row reasons too; do not infer those test cases from this Play Mode route. The
  disabled-row behavior is covered by the EditMode tests
  `ShadowEscortCommissionPanelTests.MinorAndRetiredMembers_ShowWorkDutyReasonsAndCannotBeAssigned`,
  `BusyMember_IsListedWithReasonAndCannotBeAssigned` and
  `NoEligibleMembers_ListsEveryReasonAndDisablesAssignment`; that is test coverage, not
  Play Mode evidence, and this route should not try to reproduce it.
- Assign: click one `CommissionCandidate_*` path whose label reads `<name>（可指派）` →
  `Q/M5CommissionStateText` reads `状态：执行中`, `Q/M5CommissionAssigneeText` reads
  `执行人：<name>　已过 0/3 天，剩余 3 天`, and no candidate row remains. Capture
  `<stamp>-comm-3-assigned.png`.
- Advance one game day: from the open page close `Q/M5CommissionCloseButton` (the page closes
  and `P/M5CommissionOpenButton` returns; the town menu stays open), click
  `Canvas/M2WorldMapRoot/TownMenuRoot/ReturnToMapButton` (the town menu closes,
  `M5CommissionOpenButton` is gone, and `Canvas/WorldClockPanel/AdvanceButton` is active),
  click `Canvas/WorldClockPanel/AdvanceButton`, and `... probe Canvas/WorldClockPanel/DateText`
  to see the next day (`1560-04-02`, then `1560-04-03`, then `1560-04-04`). If `texts` lists
  `Canvas/WorldClockPanel/MonthTurnFeedbackText`, the turn was refused: quote that line and
  stop. Then click the `清洲` `DestButton_*` path, click `P/M5CommissionOpenButton`, and read
  the page.
- Expected readings, one round trip per day:
  - Day 1 (`1560-04-02`): `状态：执行中`, `执行人：<name>　已过 1/3 天，剩余 2 天`.
  - Day 2 (`1560-04-03`): `状态：执行中`, `执行人：<name>　已过 2/3 天，剩余 1 天`.
  - Day 3 (`1560-04-04`), settled: `Q/M5CommissionStateText` reads `状态：成功，已结算`,
    `状态：部分成功，已结算` or `状态：失败，已结算`; `Q/M5CommissionAssigneeText` reads
    `执行人：<name>（3 天执行窗口已结束）`; `Q/M5CommissionOutcomeText` reads
    `结果：<成功|部分成功|失败>｜总分 <score>（掷值 <roll>）` on the first line and
    `报酬已计入：+<funds> 资金、+<supplies> 物资` or `报酬：无；预付 <investment> 资金已消耗`
    on the second; and `Q/M5CommissionChronicleText` reads `编年史：护送委托｜` plus the settled
    summary, which is `护送委托<成功|部分成功|失败>：能力合计 <n>，家训修正 <n>，检定总分 <n>，`
    and then either `获得 <funds> 资金、<supplies> 物资。` or `没有获得报酬。`, followed by the
    exact due day in parentheses, `（1560-04-04）`. Capture `<stamp>-comm-4-settled.png`.
- Settle-once check: spend a fourth day, `1560-04-05`, through the same close / return /
  advance / reopen sequence and reopen the page. `Q/M5CommissionStateText`,
  `Q/M5CommissionOutcomeText`, `Q/M5CommissionAssigneeText` and `Q/M5CommissionChronicleText`
  must be byte-identical to the day-3 readings, and `Q/M5CommissionResourceText` must not
  move; a change there means another system fired — quote it and do not attribute it to the
  commission. Capture `<stamp>-comm-5-settle-once.png`.

### M07 manual and delegated modes (both routes verified live 2026-11-12)

On 2026-11-12 the existing Unity 6000.6.6f1 Editor exercised the delegated entry. In a fresh
in-memory SC-013 run at 清洲 on 1560-04-01 the lead accepted the 尾张商队护送委托, assigned
明智 千恵 (one assignee), kept the default delegated mode and the 3-day window, and advanced
to 1560-04-04. The settlement read 部分成功, total 42 (roll 32), ability subtotal 0 and creed
modifier `+10`; the reward was `+7` funds and `+1` supply; fame went 0 → 1, concealment
50 → 50, and 尾张商队信用 (`employer.shadow-escort-merchant`) 0 → 1. The chronicle row is
labelled `自动委派`. No save/load was performed; afterward the Editor was stopped,
`FoundationScene` was clean, and Console cursor 12024 / session
`6bedd8738a2a401587b6d81b9bb84f9c` had zero new entries. This route is not a full success and
does not establish general manual/delegated equivalence, deterministic repeatability or M07
overall acceptance. A separate manual route on the same date used a fresh in-memory SC-013 run
at 清洲 on 1560-04-01: the lead selected the visibly checked `亲自执行` before accepting,
paid the 4-fund acceptance cost once, and assigned 明智 千恵. The window read 1/3 on 04-02,
2/3 on 04-03, and 3/3 on 04-04 while still unsettled, with an enabled `结算委托`; one click
settled `Partial` with total 42 (roll 32), `+7` funds and `+1` supply, fame `0 → 1`,
concealment `50 → 50`, and `尾张商队信用` `0 → 1`, with the chronicle labelled `亲自执行`.
Advancing to 04-05 left the single result, chronicle and resources unchanged. No save/load,
screenshot or EditMode was performed; the Editor was stopped with `FoundationScene` clean and
Console `0/0/0`. This manual route matches the delegated route's visible result and tracked
effects for these two recorded routes, not as general parity or repeatability. Local gitignored
record: `Logs/verify/20261112-0306-m05-manual.md`. A separate manual terminal save/load route
(another run, not the no-save route above) settled the same `Partial` result on 1560-04-04,
saved through the UI with `已保存当前进度。`, advanced to 04-05, then loaded with
`已载入存档。`, restoring 04-04 plus the same terminal commission, score, resources
(33 funds / 13 supplies / 0 intelligence), fame/concealment/merchant relation and the single
04-04 chronicle row. Read-only JSON inspection showed Manual mode, settled stage/outcome, one
M07 mission-history row, `familyStanding` values and chronicle. Existing user saves were copied
to `%TEMP%\ShadowClan-M07-save-backup-20261112-1326` before the run and restored byte-identically
with SHA-256 `1E61ED9CAB4A5F3E5FBCCE5EDA4BD24586BE5A072EA5B749CCB3C1E280BCB92C` and
`F1C0630E5DD91BAA0B18607CA4367956DB637A8BB4DD6DA21D1E79212BDA4CBE`. No screenshot was
captured; after stopping, the scene was clean and Console `0/0/0`. This is one terminal M05
Manual save/load route only: it does not establish corruption recovery, migration coverage,
failure-preserves-old-slot behavior, a second settlement after loading, overall M07
acceptance, or broader manual/delegated parity. M05 delegated full success passed a
separate Play Mode route on 2026-11-12. Manual full success and abandonment were verified in
separate 2026-11-14 Play Mode routes documented in `docs/reports/m07-runtime-verification.md`.

The open commission page builds two explicit runtime uGUI mode rows from the authored candidate
template, under `CandidateContent`. No scene or prefab change is required:

- `CommissionMode_Manual` — 亲自执行（到期后手动点击结算）
- `CommissionMode_Delegated` — 自动委派（到期时自动结算）

The selected row starts with `✔`. Both rows are interactable before assignment and disappear
once execution starts, because the choice is frozen into the commission record. Delegated play
settles once on the due day during normal day/month advancement. Manual play leaves the row at
`InProgress` with `elapsed == duration`; at that point the existing accept button is enabled and
relabelled `结算委托`. Clicking it calls `TrySettleCommissionManually`, which refuses before the
due date and refuses a second settlement.

Both paths call the same `TryCommitMissionSettlement` with the same frozen request derived from
the saved commission (assignee, start date, roll, score, creed, resources). Result mapping is
unchanged: `Success → 完全成功`, `Partial → 部分成功`, `Failure → 失败并安全撤离`. Fame,
employer credit (`employer.shadow-escort-merchant`) and concealment use the approved table.
M05 never causes death or injury.

Abandonment is distinct: when the assignee dies, leaves or acquires a conflicting duty at a day
boundary, the commission becomes `Abandoned`. It has no shared result, no terminal history row,
no fame/relation/concealment effect and no reward; only the existing `abandon` action chronicle
row remains. The manual path uses the same rule: clicking `结算委托` on the due date with an
unavailable assignee closes the row once as `Abandoned`; that closure has no shared mission
result and no terminal history row, so its failure message must not be read as a settlement
result. A pre-M07 save
whose commission was still `InProgress` migrates with the `legacy.m05.current` run ID and
`LegacyUnknown` mode; if its assignee is unavailable at the due date, that row abandons once with
the same semantics, clears the run ID and keeps `LegacyUnknown` instead of guessing the old mode.
A pre-M07 save whose commission is already `Abandoned` also imports without a shared result or
terminal history row. Migration leaves it with no run ID and the migration's default `Delegated`
mode field, not `LegacyUnknown`, because that branch creates no imported result to key; its
persisted abandon feedback still surfaces after a load.
`CommissionStatus.Feedback` surfaces the persisted abandon reason after a load.

The crisis-demo roster swap is refused, with a readable `LastSuccessionMessage`, while an M06
infiltration is active, while any injured person or any terminal-history participant is outside
the crisis roster, and while an accepted-but-unsettled escorted commission holds an old-roster
assignee. The entry stays clickable so it can explain the refusal; it never deletes an injury,
rewrites history or rewrites a running operation to force the swap.

The abandonment cases in source are `GameSessionMissionTests`
(`AssigneeRetirement_MidCommission_EndsAsSaveableAbandonment` and
`ManualSettlement_WithUnavailableAssignee_AbandonsOnceAndStaysSaveable`), the legacy migration in
`SaveDataMigratorTests`, the domain row in `ShadowEscortCommissionTests` plus the unit-level
semantics in `M07MissionIntegrationTests.CommissionAbandonment_HasNoSharedResultOrHistory`, and
the mode rows and settle label in `ShadowEscortCommissionPanelTests`. The 2026-11-12 session
compiled and passed EditMode 1710/1710, which covers these cases; no abandonment route was
exercised in Play Mode.

## Gotchas

- The button only enables at 清洲. If the `M5CommissionAcceptButton` row in `buttons` shows
  `False` in its interactable column, quote `Q/M5CommissionStateText`, which names the reason,
  and return to 清洲 before retrying.
- The clock and the map rows vanish while the town menu is open, and `VerifyUi` cannot see
  inactive objects. Do not plan an `AdvanceButton` or `DestButton_*` click during the town
  flow, and do not probe `M5Commission*` text after closing the page — the probe answers
  `(no active object at '<path>')` instead of the last value.
- If `Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavMapButton` is missing from `buttons`,
  the shared nav rail has not been shown yet: click
  `Canvas/M02DailyActionRoot/M02DailyActionOpenButton` (`人物档案 · 每日行动`) once, then
  re-list the controls.
- `返回地图` closes the commission page as part of leaving the town menu, so the entry button
  is back the next time the menu opens; no extra `关闭` click is needed after it.
- `DestButton_*` order is not the location order: click the row whose label reads 清洲, not
  `DestButton_0` by habit.
- A retired former head is still listed: retirement removes eligibility for a new
  commission, not the row, and the same rule gates `TryAssignCommission`. That path needs a
  completed succession, so it is EditMode coverage
  (`ShadowEscortCommissionPanelTests.MinorAndRetiredMembers_ShowWorkDutyReasonsAndCannotBeAssigned`),
  not a step in the fresh-start route.
- A person who retires while already executing the escort keeps a saveable `InProgress` row,
  but the due date can no longer settle with that assignee. Settlement closes the run once as
  `Abandoned`: no shared result, history, standing or reward, no investment refund, the run ID
  is cleared, and one `abandon` action row remains in the chronicle. This is the
  `AssigneeRetirement_MidCommission_EndsAsSaveableAbandonment` EditMode case; only check it in
  Play Mode if the run deliberately performs a handoff mid-countdown. Then the countdown must
  end as `已中止`, a save made between the handoff and the next day must still load, and no
  repeated settlement retry may remain.
- Save state is real user data and the project has one slot (`save-slot-1.json` under
  `<persistentDataPath>/SengokuChronicle/`). The M05 gate saves at 1/3 days and reloads it.
  Before clicking `存档`, copy `save-slot-1.json` and `save-slot-1.json.bak` outside the
  project; after the test, restore both and compare their SHA-256 hashes. Never overwrite a
  slot unless you can restore its original bytes.
- `接取委托` and the candidate rows do not move the date or the party; a date change during
  this route means something else fired, so quote the before/after HUD date.
- `unity command editor_focus` before every capture, or the image can be a stale frame.
