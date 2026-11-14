# M08C historical timeline implementation plan

> **For agentic workers:** Use the approved OpenCode2 implementation workflow on the current project worktree. Keep code ownership to the listed files and do not create a branch or worktree.

**Goal:** Add the five GDD historical nodes and one-year-ahead warnings to the SC-010 standard campaign through the existing event and chronicle flow.

**Architecture:** Extend the data-only monthly event condition with an exact Gregorian year/month predicate. Add ten SC-010-only, once-per-game entries across the preceding and milestone years, reusing `EventChoicePanel`, `EventRuntimeState`, `HistoryLedger` and the current save format.

**Tech Stack:** Unity 6, C#, existing event JSON, `JsonUtility`, NUnit EditMode tests, current Unity Editor and Pipeline bridge.

**Spec:** [GDD_v4.0.md](../../../GDD_v4.0.md) §9.3 and the M08C subsection. Calendar and scope decisions are recorded in [DECISIONS.md](../../../DECISIONS.md#2026-11-14---m08c-historical-timeline-dates-and-scope).

## Global constraints

- Use only the existing SC-010 standard campaign, whose stable origin ID is `sc010`.
- Treat GameDate as a Gregorian game calendar. Schedule milestones on January 1 and warnings in the preceding year. Move a warning to the next free month when another timeline entry uses its default January date. Do not label scheduler dates as historical occurrence dates.
- Add no world-state simulation, resource or relationship effect, forced battle, mortality effect, UI panel, Unity package or external asset.
- Preserve older saves. The event catalog version may advance, but the save schema does not change; old SC-010 saves receive only future entries and never get retrospective history.
- Preserve `.meta` GUIDs. Do not edit scenes, prefabs, `ProjectSettings`, `Library`, `Temp`, or unrelated WIP.
- Keep deterministic random streams and monthly transaction order intact. Fixed historical entries outrank ordinary monthly events only on their authored month.

## Review focus

- Exact date boundary: a row is eligible only in its authored year/month, not the preceding or following month; no two M08C rows share a month. Source: GDD M08C schedule rule.
- Scenario isolation: `sc010` entries never appear in SC-013 or SC-020–SC-024. Source: GDD M08C scope rule.
- Competing monthly events: a due timeline entry wins the priority group for its date and does not displace later events. Source: GDD M08C dated-entry rule and current priority scheduler behavior.
- Save/load: an unresolved entry stays pending, and an acknowledged per-game entry does not reappear. Source: GDD M08C reuse of `EventRuntimeState` and its save behavior.
- Calendar interpretation: January 1 or February 1 is a game scheduling date only. Source: GDD M08C calendar rule; do not claim lunar-day accuracy.

## Exact content schedule

| Node year | Node text | Warning date | Node date |
|---|---|---|---|
| 1579 | 第一次天正伊贺之乱 | 1578-01-01 | 1579-01-01 |
| 1581 | 第二次天正伊贺之乱 | 1580-01-01 | 1581-01-01 |
| 1600 | 关原之战 | 1599-01-01 | 1600-01-01 |
| 1614 | 大阪冬之阵 | 1613-01-01 | 1614-01-01 |
| 1615 | 大阪夏之阵 | 1614-02-01 | 1615-01-01 |

The warning for 1615 follows the 1614 node in the same year. It moves to February because the monthly scheduler holds one event at a time. These are year-level historical entries. The authored text may describe the lead-up and the known event, but it must not claim exact in-game occurrence dates beyond the scheduled year.

## File ownership

- Modify `Assets/Scripts/Events/EventCatalogData.cs`: add `EventScenarioIds.Sc010` and `EventConditionKind.CampaignDateIs` with serialized `year` and `month` fields on `EventConditionData`.
- Modify `Assets/Scripts/Events/EventCatalogValidator.cs`: allow `sc010`; validate date year against `GameDate`'s supported range, month 1–12, monthly trigger, SC-010 scenario and `PerGame` scope for `CampaignDateIs`.
- Modify `Assets/Scripts/Events/EventEngine.cs`: pass the tested `GameDate` into condition evaluation at scheduling and commit; match `CampaignDateIs` on both year and month. Keep one evaluator so scheduling and option commit use the same date rule.
- Modify `Assets/Scripts/Application/GameSession.cs`: resolve the event scenario from explicit `_eventScenarioId`, then the existing SC-013 fixed-roster rule, then `CampaignProgress.OriginId == CampaignOriginCatalog.Sc010OriginId`. Use the same resolver for live scheduling and save-load event-runtime validation. Do not persist a route ID or change save validation/schema.
- Modify `Assets/Resources/Events/event-catalog-v1.json` and `Assets/Resources/Localization/event-text-zh-v1.json`: bump both content versions and add ten stable, localized entries. Each entry has one `Always` option with no effects, `PerGame`, weight 1, SC-010 scenario, `CampaignDateIs`, and priority 2000. Use stable IDs under `event.m08c.history.*`, `option.m08c.history.*` and matching text IDs.
- Modify `Assets/Scripts/Persistence/SaveData.cs`: advance `CurrentEventContentVersion` to match the catalog. Do not change `CurrentVersion`, rules version, or add migration state.
- Modify `Assets/Scripts/Application/CampaignReviewView.cs`: replace the stale “world historical event stream is not implemented” empty-source note with wording that says past nodes are not backfilled.
- Modify `Assets/Tests/EditMode/EventCatalogValidatorTests.cs`, `EventEngineTests.cs`, `GameSessionSaveTests.cs`, `CampaignMilestoneTests.cs`, `SaveDataValidatorTests.cs`, `SC011EventContentTests.cs`, `SC011LegacyEventTests.cs`, and `SC018IndependentFamilyEventsTests.cs`: add the focused regression coverage below and update every assertion that pins the current catalog/text/save content version to 3. Preserve fixtures that intentionally represent legacy versions 1 or 2.
- Update `PROJECT_STATE.md` after verification with separate compile, EditMode, Console, scene-validator and Play Mode evidence. Do not mark M08 or M08C user-accepted.

## Tasks

### Task 1: Pin the authored-date predicate and validation

**Files:** `Assets/Tests/EditMode/EventCatalogValidatorTests.cs`, `Assets/Tests/EditMode/EventEngineTests.cs`.

- [x] Add `CampaignDateIs_EligibleOnlyForItsYearAndMonth` using the authored month, the prior December, and the following month. Assert eligibility only for the authored year/month.
- [x] Add `CampaignDateIs_RequiresValidDateAndSc010PerGameMonthlyEvent` for month 0, month 13, out-of-range year, a non-SC-010 scenario, non-monthly trigger, and non-`PerGame` scope. Assert the validator rejects each invalid catalog.
- [ ] Run the two tests in the existing Unity Editor and confirm the new tests fail against the current implementation.
- [x] Implement the enum, fields, validator checks and date comparison in the listed event files. Keep date matching in the shared condition evaluator so scheduling and option commit use one rule.
- [x] Re-run the focused tests and the existing `EventEngineTests` and `EventCatalogValidatorTests` suites; require all pass.

### Task 2: Add the historical warning and milestone content

**Files:** `Assets/Resources/Events/event-catalog-v1.json`, `Assets/Resources/Localization/event-text-zh-v1.json`, `Assets/Scripts/Persistence/SaveData.cs`, `Assets/Scripts/Application/CampaignReviewView.cs`, `Assets/Tests/EditMode/EventEngineTests.cs`, `CampaignMilestoneTests.cs`, `SaveDataValidatorTests.cs`, `SC011EventContentTests.cs`, `SC011LegacyEventTests.cs`, `SC018IndependentFamilyEventsTests.cs`, and `SC010HistoricalTimelineTests.cs` (new test and Unity-generated `.meta`).

- [x] Add tests `Sc010Timeline_ExposesEachWarningAndMilestoneAtItsAuthoredDate`, `Sc010Timeline_NoAuthoredMonthContainsTwoRows`, `Sc010Timeline_HistoricalEntryWinsAgainstOrdinaryEventOnSameDate`, `Sc010Timeline_NeverSchedulesForOtherOrigins`, and `Sc010Timeline_OnceAcknowledgedDoesNotRepeat`.
- [x] Add five warning and five milestone rows using the exact year/date table. Their one acknowledgement option has no effect. Historical priority 2000 outranks current ordinary monthly content while leaving unrelated months unchanged.
- [x] Raise catalog/text and `SaveData.CurrentEventContentVersion` from 3 to 4. Keep save schema/rules versions unchanged and preserve acceptance of event content versions 1–3. Update the assertions in `SaveDataValidatorTests`, `SC011EventContentTests`, `SC011LegacyEventTests` and `SC018IndependentFamilyEventsTests` that describe the current version; keep explicit legacy-v1/v2 fixtures unchanged.
- [x] Verify the resource JSON parses, `EventCatalogValidator.Validate` succeeds, all ten IDs are unique, and every localized text reference resolves.
- [x] Update `ReviewView_ReportsMissingFactsInsteadOfInventingThem` to assert the empty-source note says past historical nodes are not backfilled and no longer claims the world event stream is unimplemented.
- [x] Run the focused historical timeline, event, catalog validation and save compatibility EditMode suites in the current Editor: SC010HistoricalTimelineTests 9/9 and EventCatalogValidatorTests 13/13 passed; the full run below covers the remaining suites.

### Task 3: Verify pending and committed history persistence

**Files:** `Assets/Scripts/Application/GameSession.cs`, `Assets/Tests/EditMode/GameSessionSaveTests.cs` and `Assets/Tests/EditMode/SC010HistoricalTimelineTests.cs`.

- [x] Add `Sc010Timeline_PendingHistoricalEventSurvivesSaveLoad` and `Sc010Timeline_CommittedHistoricalEventSurvivesSaveLoadWithoutReoffer` using real `GameSession` capture/apply APIs. Assert the pending event ID, event history, once-per-game marker and Chronicle row match before and after reload.
- [x] Add live-session integration coverage proving `InitStandardDemo` schedules `sc010` rows through its campaign origin, while SC-013 and at least one SC-020–SC-024 demo remain isolated. Reuse the same origin-based scenario resolver during `TryApplySave` so a pending SC-010 event restores without a persisted route field.
- [x] Confirm a save captured under event content v3 can load under v4 without a schema migration and keeps its existing date, event history, once markers and random states.
- [x] Run the focused save/session tests and the full EditMode suite in the existing Editor: full Editor Test Runner 1973/1973 passed, 0 failed/skipped/inconclusive; TestResults.xml timestamp 2026-11-14 02:02:05 local.

> **Verification update (2026-11-14):** The two follow-up tests are now included in the current Unity runs: `SC010HistoricalTimelineTests` 9/9 and `EventCatalogValidatorTests` 13/13 passed; the full Editor Test Runner reported 1973/1973 passed. The earlier 1971-test XML and dirty-scene caveat above are historical worker evidence and are superseded by the current lead verification. Full evidence: `Logs/verify/20261114-m08c-historical-timeline.md` (local, gitignored).

### Task 4: Validate the existing scene and Play Mode path

**Files:** no scene or prefab changes are expected. If an existing scene reference is broken, stop and report it; do not regenerate the scene as part of this task.

- [x] Inspect the Unity process and confirm its `-projectPath` is `C:\Users\panye\My project`. Used that Editor only (PID 77116, Unity 6000.6.6f1).
- [x] Confirm compile status separately from EditMode results and record Console counts: recompile completed, compilationFailed=false; final Console ground truth 0 errors / 0 warnings / 0 logs. Two earlier Pipeline main-thread timeouts (seq 1925-1926) were inspected and recorded in the evidence file before clearing Console.
- [x] Run the existing `FoundationSceneValidator`; report was Loaded=True, Dirty=False, all checks PASS, Errors=0.
- [x] In Play Mode, start SC-010, verify the 1578-01 warning, save/load it pending in a dedicated temporary test slot, then acknowledge it. At 1579-01, save/load the node pending, acknowledge it and confirm the Family Chronicle row.
- [x] Confirm SC-013 live Play Mode and SC-020 live Play Mode had no SC-010 event at 1578-01; EditMode route isolation covers SC-013 and SC-020–SC-024. The visible node names 1579 and describes the historical year without a historical month/day claim.
- [x] Exit Play Mode and confirm the active scene is clean. The Editor stopped, `FoundationScene` remained active and clean, and Console ground truth was 0/0/0. The dedicated M08C test save remains at `%LOCALAPPDATA%\Low\DefaultCompany\My project\M08C-PlayTest-20261114`. A Game View screenshot remains at `Assets/Logs/verify/_scratch/20261114-1206-m08c-initial.png`; automatic approval review rejected its deletion command, so it still requires manual deletion. No other screenshot was created in the resumed SC-020 run.

### Task 5: Independent review and checkpoint

**Files:** all implementation-owned files above plus `GDD_v4.0.md`, `DECISIONS.md`, `PROJECT_STATE.md`.

- [x] Request a separate read-only OpenCode review after implementation; reviewer returned PASS with low-severity residuals.
- [x] Address review findings: closed the unused `sc010` shared-scenario allowlist, updated plan/state historical evidence language, and corrected the campaign review empty-source note. Independent reviewer returned PASS after those changes.
- [x] Verify `git diff --check`, inspect the final diff and `git status`, and stage only M08C files plus the documented status entry. The M08C implementation and its documentation were committed as `616f4be` and pushed to `origin/main`; later status-only commits also record its lead verification. Untracked local WIP remained unstaged.
- [x] Commit and push the verified M08C work to `origin/main` after the applicable verification gates passed. Do not tag or claim final M08 acceptance. M08C and M08 remain pending explicit user milestone acceptance.

## Acceptance evidence

- The event catalog is valid and contains five warnings plus five historical entries.
- Date, scenario, priority, once-per-game and save/load tests pass.
- Full EditMode is green in the current Editor; compile, Console, scene validation and Play Mode are reported separately.
- SC-010 displays and records the 1578 warning and 1579 node; SC-013 does not schedule those rows.
- Older saves load without a new schema migration. Already-past historical nodes are not backfilled.

## Spec probe record

| Probe | Result | Criterion or reason |
|---|---|---|
| Boundary values | resolved | Year must fit `GameDate`; month is 1–12. Validator tests cover minimum/maximum supported year and month 0/1/12/13. The content dates come from GDD §9.3. |
| Adjacency | resolved | Prior December, authored January and following February are tested. The authored entry is eligible only for the exact year/month, including the February warning exception. |
| Empty/degenerate | dismissed | The feature uses fixed non-empty catalog rows and existing validated text/option arrays; no new caller-supplied collection is introduced. Existing event catalog validation continues to reject empty or missing required data. |
| Encoding | dismissed | Text uses the existing UTF-8 JSON and `textId` lookup contract; IDs are ASCII and Chinese body text is decoded by the existing loader. |
| Ordering/stability | resolved | Priority 2000 wins over ordinary events on a scheduled month; one row per date avoids equal-priority ordering ambiguity. Asserted by `Sc010Timeline_HistoricalEntryWinsAgainstOrdinaryEventOnSameDate`. |
| Precision/overflow | dismissed | The date condition compares bounded integers and introduces no arithmetic or rounding. |
| Idempotency | resolved | `PerGame` once markers persist after acknowledgement and prevent duplicate offers. |
| Concurrency/effect order | resolved | The existing event transaction permits one pending event; authored M08C rows do not share months, and a due historical row has priority over ordinary events on that month. Save/load tests cover a pending row. |
| Example-only properties | dismissed | The date table states the complete rule; it does not imply additional ordering or world effects. |
| Variant coverage | resolved | SC-010 is tested as eligible and SC-013 plus one other demo origin are tested as ineligible. |
| Look-alike terms | resolved | “传闻” is the prior-year warning; “节点记录” is the target-year entry. Tests assert both distinct IDs/dates for all five nodes. |
| Unclassified requirement | resolved, judgment | A reviewer checks all ten strings against the GDD's year-level facts and confirms none claims a historical day or month. |
| Prohibition: false date precision | resolved, judgment | UI strings identify only the historical year. January 1 is a scheduler date, not a historical event date. |
| Prohibition: forced war or family outcome | resolved, assumption | The event options have no effects. No battle, death, territory, resource or relation change is applied by M08C. Assert option effects are empty for all ten events. |
| Prohibition: retroactive fabrication | resolved, assumption | A node whose date is already past is not backfilled. Test a post-node save and assert no old row is scheduled. |

Applicable probes: 15. Resolved: 11. Dismissed: 4. Unresolved: 0. Backstop: 0. Judgment: 2. Assumption-tagged criteria: 2.
