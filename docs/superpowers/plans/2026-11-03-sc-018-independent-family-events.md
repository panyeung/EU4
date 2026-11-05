# SC-018 实现计划：七个独立家族事件

> **For OpenCode2:** REQUIRED SUB-SKILL: Use superpowers:executing-plans and complete the tasks in order. Work only in the current Orca task worktree. Preserve all unrelated user changes.

**Goal:** 将 M2 事件目录从 23 项增至 30 项，新增七个可独立触发、单次解决的家族事件；支持家族数值关系、存档与编年史显示，同时保持五条现有跨代事件链及 SC-011 至 SC-017 行为不变。

**Approach:** 扩展现有事件目录和中文本地化，并增加按 `(currentHeadPersonId, familyMemberPersonId)` 键控的家庭数值关系状态、独立事件效果、存档兼容和编年史双方姓名/前后值展示。新事件不设置 `chainId`，不建立 `EventOptionChosen` 后续引用。继承不复制上一任家主关系；新家主使用自己的键，旧键保留。使用现有月度事件和单次触发框架，不增加 Unity Package、场景或 Unity 项目。

**Acceptance:** 事件数为 30、链数为 5；新增七个事件均为无链、每局一次的月度事件；目录与本地化无重复 ID 或缺失引用；家族关系变化真实写入、存档/读档后保留，并在编年史显示双方姓名与精确前后值；旧存档按明确兼容规则加载；现有必经 M2 路线不被可选事件遮挡；Unity/测试证据由实现/QA worker 单独报告，用户 Play Test 仍为最后验收门槛。

**Design:** `docs/superpowers/specs/2026-11-03-sc-018-independent-family-events-design.md`

**Constraints:** OpenCode2 owns all code/content edits. Do not edit scenes, prefabs, `ProjectSettings`, existing `.meta` files/GUIDs, `Library`, `Temp`, unrelated WIP, or git integration state. New `.meta` files may accompany only new source/test files. Do not launch batchmode against the project. Do not use Computer Use or screenshots on Unity. The user performs Play Test.

**User-approved relationship semantics (2026-11-03):** each value is keyed by `(current head PersonId, family member PersonId)`. A new head uses their own pair records after succession; old-head records remain attached to that person. No self-pairs or invented values during migration.

## Task 1 — Map current event and character contracts

**Read:** `Assets/Scripts/Events/EventCatalogData.cs`, `EventCatalogValidator.cs`, `EventEngine.cs`, `EventRuleContext.cs`, `Assets/Scripts/Application/M2FixedScenario.cs`, `Assets/Scripts/Domain/CharacterRelationships.cs`, `Assets/Scripts/Domain/StableIds.cs`, save/migration/history code, current catalog/text JSON, and SC-011/SC-013 content tests.

- Verify supported event condition/effect values, chainless-event validation, scenario gates, and event selection priority behavior.
- Identify real family member IDs and roles in the M2 scenario. Do not invent IDs or reference an absent/dead/non-family character as a family relationship target.
- Record the baseline 23 event IDs and five chain IDs locally in the focused SC-018 test; do not rewrite existing entries.
- If any proposed event cannot target a present M2 family member safely, adapt its narrative/target to the existing family roster while preserving its standalone family theme.

## Task 2 — Add person-pair family relationship state and event effect

- Add a dedicated family-relationship state keyed by `(current head PersonId, target family member PersonId)`; preserve NPC-only `CharacterRelationships` behavior.
- Add a distinct family relationship event effect and typed family target field. Capture the current head as the relationship source when applying the effect; reject self-pairs and targets outside the active family.
- Apply deltas transactionally with the existing `[-100,100]` clamp and report only actual before/after changes. If any event effect or commit fails, roll back every family relationship delta.
- On succession, do not transfer the outgoing head's relationship values to the incoming head. Retain old pair records; new-head values default to zero until changed.

## Task 3 — Persist family relationships and record them in the chronicle

- Add person-pair entries to save data and bump the save schema to v5. Support v4→v5 migration (empty family relationship set) while preserving current v1–v3 migration behavior.
- Validate person IDs, non-self pairs, duplicate keys, membership, and value range. Never infer family relationship values from kinship edges.
- Append each actual change to the existing Relationship Chronicle category with both resolved person names and exact before/after values; ensure chronicle save/load and panel display do not expose raw IDs.

## Task 4 — Add the seven standalone content definitions

**Edit:** `Assets/Resources/Events/event-catalog-v1.json` and `Assets/Resources/Localization/event-text-zh-v1.json`.

Add these approved themes with stable IDs under `event.sc018.*`:

1. `sibling-duty` — sibling asks to share household/watch duties.
2. `child-study` — child asks to continue study rather than follow the expected training schedule.
3. `parent-care` — relatives disagree about care for an elder.
4. `heirloom` — two relatives disagree about custody of a family heirloom.
5. `spouse-visit` — spouse asks to visit their birth family.
6. `adoption-welcome` — adopted family member asks how the household regards them.
7. `family-assembly` — household asks to discuss future responsibilities.

- Give each event two clear Chinese options and a concise outcome summary explaining whose relationship changed and by how much.
- Use unique `event.sc018.*`, `option.sc018.*`, and `summary.sc018.*` text IDs.
- Use `trigger: Monthly`, `onceScope: PerGame`, and no `chainId` property (or an empty chain only if serializer conventions require it).
- Keep event catalog `contentVersion: 2`. Add the approved family-relationship effect kind and typed person target. Each outcome must use an existing SC-013 family member ID and respect relationship clamping.
- Assign priority below all mandatory fixed-scenario events so optional family content cannot starve a required SC-013 event. Confirm this from `EventEngine` before choosing a value.
- Do not add numeric economy, inventory, mission, growth, or succession effects.

## Task 5 — Add focused model, save, event, and content regression coverage

**Create:** `Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs` and its Unity `.meta` file.

Cover:

- Catalog loads and validates with exactly 30 events and the same five chain IDs/count as baseline.
- All seven expected IDs exist, have monthly trigger and per-game scope, and are absent from all chain `linkedEventIds`.
- Each event has two usable options, valid unique localized text IDs, and existing character targets for each relationship effect.
- Every choice has a visible relationship outcome; all deltas stay within supported limits.
- Existing 23 events and their authored chains remain semantically unchanged.
- Optional events have lower priority than fixed scenario events.
- Pair storage distinguishes current heads and preserves old-head entries after succession; no implicit copy to the new head.
- Family effects reject self/nonmember targets, clamp correctly, and leave no partial change on failure.
- Family relationship save/load round-trips; v4 migration adds no fabricated values; chronicle entries preserve both names and before/after values.

Use the project’s existing test patterns and no new test framework or package.

## Task 6 — Update the playtest and project documents

**Create:** `SC018_DEMO.md` with sequential steps in Chinese for the next user Play Test. Include setup/save guidance, exact title entry and monthly actions, expected option and before/after relationship values, steps to encounter all seven events, expected fixed-scenario route, stop/report conditions, and a results checklist. Do not use screenshots.

**Update:** `PROJECT_STATE.md` and `docs/reports/sc018-implementation.md` with changed files and separately stated static, Editor compilation, EditMode, Console, Validator, and Play Mode evidence. Keep unknown or unrun evidence explicitly unknown/unrun. Do not claim user acceptance before it is reported.

## Task 7 — Verify and hand off for read-only review

- Validate both JSON files for parse success, unique IDs, complete text references, 30/5 totals, and preservation of the prior event data.
- Run only checks that do not attach to, modify, or launch Unity against the user-held project; report exact commands and results. If Unity Editor EditMode evidence is unavailable without UI or risking the open project, mark it pending for QA/user rather than using batchmode.
- Report changed paths, test/verification evidence, and known limitations to the coordinator.
- The coordinator then dispatches a separate OpenCode worker for read-only review. No scene work or git integration in this task.

## Worker ownership

OpenCode2 may edit the event catalog, Chinese event text, relevant Events/Domain/Application/Persistence/Presentation runtime code and tests required for the approved family relationship feature, plus new source/test `.meta` files only, `SC018_DEMO.md`, `PROJECT_STATE.md`, and `docs/reports/sc018-implementation.md`. Preserve all existing `.meta` GUIDs and all other dirty files exactly. The coordinator owns this plan/spec and routes follow-up changes after review.
