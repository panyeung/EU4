# 历史设计与计划档案
本文件由原始 Markdown 文档合并而成。各节保留原文，标题中的来源路径用于追溯。
## 目录
- [DEMO.md](#demo)
- [GDD_v0.2.md](#gdd-v0-2)
- [M3_DEMO.md](#m3-demo)
- [M3_PLAN.md](#m3-plan)
- [M4A_DEMO.md](#m4a-demo)
- [M4B_DEMO.md](#m4b-demo)
- [M5_DEMO.md](#m5-demo)
- [M6_DEMO.md](#m6-demo)
- [M7_DEMO.md](#m7-demo)
- [PHASE1_ROADMAP.md](#phase1-roadmap)
- [PHASE2_ROADMAP.md](#phase2-roadmap)
- [RISKS_AND_AGENTS_PROPOSAL.md](#risks-and-agents-proposal)
- [docs/superpowers/plans/2026-10-31-m4a-supply-shortage.md](#2026-10-31-m4a-supply-shortage)
- [docs/superpowers/plans/2026-11-01-m4b-two-simple-missions.md](#2026-11-01-m4b-two-simple-missions)
- [docs/superpowers/plans/2026-11-01-m5-single-slot-save.md](#2026-11-01-m5-single-slot-save)
- [docs/superpowers/plans/2026-11-01-m6-title-demo-packaging.md](#2026-11-01-m6-title-demo-packaging)
- [docs/superpowers/plans/2026-11-01-m7-merit-promotion.md](#2026-11-01-m7-merit-promotion)
- [docs/superpowers/plans/2026-11-01-sc-001-baseline-audit.md](#2026-11-01-sc-001-baseline-audit)
- [docs/superpowers/plans/2026-11-01-sc-002-pure-csharp-core.md](#2026-11-01-sc-002-pure-csharp-core)
- [docs/superpowers/plans/2026-11-02-sc-004-monthly-time.md](#2026-11-02-sc-004-monthly-time)
- [docs/superpowers/plans/2026-11-02-sc-005-009-event-demo.md](#2026-11-02-sc-005-009-event-demo)
- [docs/superpowers/plans/2026-11-02-sc-010-succession.md](#2026-11-02-sc-010-succession)
- [docs/superpowers/plans/2026-11-02-sc-011-cross-generation-event.md](#2026-11-02-sc-011-cross-generation-event)
- [docs/superpowers/plans/2026-11-02-sc-012-family-chronicle.md](#2026-11-02-sc-012-family-chronicle)
- [docs/superpowers/plans/2026-11-03-sc-013-m2-fixed-three-generation.md](#2026-11-03-sc-013-m2-fixed-three-generation)
- [docs/superpowers/plans/2026-11-03-sc-014-refugee-legacy.md](#2026-11-03-sc-014-refugee-legacy)
- [docs/superpowers/plans/2026-11-03-sc-015-three-generation-family-letters.md](#2026-11-03-sc-015-three-generation-family-letters)
- [docs/superpowers/plans/2026-11-03-sc-016-m2-commission-legacy.md](#2026-11-03-sc-016-m2-commission-legacy)
- [docs/superpowers/plans/2026-11-03-sc-017-generic-family-tree.md](#2026-11-03-sc-017-generic-family-tree)
- [docs/superpowers/plans/2026-11-03-sc-018-independent-family-events.md](#2026-11-03-sc-018-independent-family-events)
- [docs/superpowers/plans/2026-11-03-sc-019-family-industries.md](#2026-11-03-sc-019-family-industries)
- [docs/superpowers/plans/2026-11-04-sc-020-character-growth-education.md](#2026-11-04-sc-020-character-growth-education)
- [docs/superpowers/plans/2026-11-04-sc-021-family-research.md](#2026-11-04-sc-021-family-research)
- [docs/superpowers/plans/2026-11-04-sc-022-family-formation.md](#2026-11-04-sc-022-family-formation)
- [docs/superpowers/plans/2026-11-05-sc-023-seclusion.md](#2026-11-05-sc-023-seclusion)
- [docs/superpowers/plans/2026-11-05-sc-024-family-creed-ambition.md](#2026-11-05-sc-024-family-creed-ambition)
- [docs/superpowers/plans/2026-11-07-m03-npc-autonomy.md](#2026-11-07-m03-npc-autonomy)
- [docs/superpowers/plans/2026-11-07-ui-character-action.md](#2026-11-07-ui-character-action)
- [docs/superpowers/plans/2026-11-07-ui-reference-gallery.md](#2026-11-07-ui-reference-gallery)
- [docs/superpowers/plans/2026-11-08-start-screen.md](#2026-11-08-start-screen)
- [docs/superpowers/plans/2026-11-08-town-overview-ui.md](#2026-11-08-town-overview-ui)
- [docs/superpowers/plans/2026-11-08-ui-reference-gallery-completion.md](#2026-11-08-ui-reference-gallery-completion)
- [docs/superpowers/plans/2026-11-08-ui-reference-gallery-repair.md](#2026-11-08-ui-reference-gallery-repair)
- [docs/superpowers/plans/2026-11-09-m04a-personal-memories.md](#2026-11-09-m04a-personal-memories)
- [docs/superpowers/plans/2026-11-09-world-map-immersive-ui.md](#2026-11-09-world-map-immersive-ui)
- [docs/superpowers/specs/2026-10-31-m4a-supply-shortage-design.md](#2026-10-31-m4a-supply-shortage-design)
- [docs/superpowers/specs/2026-11-01-m4b-two-simple-missions-design.md](#2026-11-01-m4b-two-simple-missions-design)
- [docs/superpowers/specs/2026-11-01-m5-single-slot-save-design.md](#2026-11-01-m5-single-slot-save-design)
- [docs/superpowers/specs/2026-11-01-m6-title-demo-packaging-design.md](#2026-11-01-m6-title-demo-packaging-design)
- [docs/superpowers/specs/2026-11-01-m7-merit-promotion-design.md](#2026-11-01-m7-merit-promotion-design)
- [docs/superpowers/specs/2026-11-02-sc-004-monthly-time-design.md](#2026-11-02-sc-004-monthly-time-design)
- [docs/superpowers/specs/2026-11-02-sc-005-009-event-demo-design.md](#2026-11-02-sc-005-009-event-demo-design)
- [docs/superpowers/specs/2026-11-02-sc-011-cross-generation-event-design.md](#2026-11-02-sc-011-cross-generation-event-design)
- [docs/superpowers/specs/2026-11-02-sc-012-family-chronicle-design.md](#2026-11-02-sc-012-family-chronicle-design)
- [docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md](#2026-11-03-sc-013-m2-three-generation-design)
- [docs/superpowers/specs/2026-11-03-sc-014-refugee-legacy-design.md](#2026-11-03-sc-014-refugee-legacy-design)
- [docs/superpowers/specs/2026-11-03-sc-015-three-generation-family-letters-design.md](#2026-11-03-sc-015-three-generation-family-letters-design)
- [docs/superpowers/specs/2026-11-03-sc-016-m2-commission-legacy-design.md](#2026-11-03-sc-016-m2-commission-legacy-design)
- [docs/superpowers/specs/2026-11-03-sc-017-family-tree-roster-design.md](#2026-11-03-sc-017-family-tree-roster-design)
- [docs/superpowers/specs/2026-11-03-sc-018-independent-family-events-design.md](#2026-11-03-sc-018-independent-family-events-design)
- [docs/superpowers/specs/2026-11-03-sc-019-family-industries-design.md](#2026-11-03-sc-019-family-industries-design)
- [docs/superpowers/specs/2026-11-04-sc-020-character-growth-education-design.md](#2026-11-04-sc-020-character-growth-education-design)
- [docs/superpowers/specs/2026-11-04-sc-021-family-research-design.md](#2026-11-04-sc-021-family-research-design)
- [docs/superpowers/specs/2026-11-04-sc-022-family-formation-design.md](#2026-11-04-sc-022-family-formation-design)
- [docs/superpowers/specs/2026-11-05-sc-023-seclusion-design.md](#2026-11-05-sc-023-seclusion-design)
- [docs/superpowers/specs/2026-11-05-sc-024-family-creed-ambition-design.md](#2026-11-05-sc-024-family-creed-ambition-design)
- [docs/superpowers/specs/2026-11-07-m03-npc-autonomy-design.md](#2026-11-07-m03-npc-autonomy-design)
- [docs/superpowers/specs/2026-11-07-m04a-personal-memories-design.md](#2026-11-07-m04a-personal-memories-design)
- [docs/superpowers/specs/2026-11-07-ui-character-action-design.md](#2026-11-07-ui-character-action-design)
- [docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md](#2026-11-07-ui-reference-gallery-design)
- [docs/superpowers/specs/2026-11-08-start-screen-design.md](#2026-11-08-start-screen-design)
- [docs/superpowers/specs/2026-11-08-town-overview-ui-design.md](#2026-11-08-town-overview-ui-design)
- [docs/superpowers/specs/2026-11-09-world-map-immersive-ui-design.md](#2026-11-09-world-map-immersive-ui-design)

---

<a id="demo"></a>
## 来源：DEMO.md

# M2 Play Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


## Start

Open `Assets/Scenes/FoundationScene.unity` in Unity 6000.6.6f1 and press **Play**. Use the mouse to select a destination (清洲, 热田, or 那古野), then click **确认出发** to travel or **取消** to return to the map. In a town, click **返回地图**.

## Try

1. Select Atsuta and inspect the travel time and arrival date, then cancel. The date and current location should stay the same.
2. Select Atsuta again and confirm. The date advances one day and the town screen opens.
3. Check that the town screen shows no world-map travel controls underneath it. Return to the map and confirm Atsuta is marked as the current location.
4. Select Nagoya from Atsuta and confirm the direct route is unavailable.
5. From Atsuta, select Kiyosu and inspect its one-day preview, then cancel. The date and location should remain unchanged.

## What's new

The M2 map and travel controls now sit in a separate lower panel. Entering a town hides the map-only controls and shows an opaque town panel, so the two screens no longer overlap. The three town facilities remain locked placeholders.

## Verification

Unity 6000.6.6f1 compiled the updated project. All 73 EditMode tests passed with 0 failures. The Editor Console showed 0 errors after the updated tests and Play Mode walkthrough. Play Mode verified Kiyosu-to-Atsuta travel, the town screen without map controls beneath it, return to map, cancellation, and the unavailable Atsuta-to-Nagoya route.

## Known limits

Town facilities, characters, missions, and save/load are not part of M2. The Phase 1 roadmap schedules those for later milestones.

## Next

- Branch: `feature/m2-ui-overlap-fix`
- Overlap fix commit: `9c2ac69`

After this demo is accepted, merge and tag M2, then continue with M3 characters and relationships.


---

<a id="gdd-v0-2"></a>
## 来源：GDD_v0.2.md

# Sengoku Chronicle（战国立志传）— GDD v0.2

> 状态：已被《影之一族》设计与 Unity 开发总计划 v4.0 取代，保留作已完成 Sengoku Chronicle 里程碑的历史设计记录。当前设计基线见 [GDD_v4.0.md](../../GDD_v4.0.md)。

## 1. 项目概览

- **工作标题：** Sengoku Chronicle（战国立志传）
- **类型：** 单人战国历史题材 RPG 与生活模拟
- **平台：** Windows PC；**引擎：** Unity 6000.6.6f1；**渲染：** Universal 2D / URP 17.6.0；**语言：** C#
- **设计参考：**《太阁立志传5》的角色成长、自由行动与生活模拟体验；本项目使用原创或获授权的素材、文字、音乐和 UI，不复制原作受版权保护内容。
- **开发原则：** 面向 Unity 初学者，优先小而可玩、易测试、可逆的迭代；以可运行里程碑衡量进展，2–3 个月只是粗略预期而非期限。

## 2. 产品意图与玩家体验

玩家扮演尾张的一名年轻武士，在「调查补给短缺—前往热田—与商人交涉—返回清洲—获得功绩」的短篇经历中，感受战国时代人物关系、行旅和任务选择。成功的 Phase 1 是一段约 10–15 分钟、能从开始玩到结算并能保存恢复的完整体验，而不是大量未完成系统的集合。

### 核心循环

查看日期与当前地点 → 选择地点/城镇行动 → 对话或完成任务步骤 → 时间与关系/功绩产生变化 → 解锁下一步 → 返回清洲领取功绩。

### Phase 1 成功标准

- 玩家可在清洲（`owari.kiyosu`）、热田（`owari.atsuta`）、那古野（`owari.nagoya`）三地之间通过地图点击旅行；旅途消耗游戏时间。
- 城镇以菜单呈现，不建可步行城镇；可访问相关城镇行动。
- 一名可玩武士、一支势力、五名虚构 NPC；原创对话与关系变化可见。
- 至少一个任务从接取至奖励完整可玩；另外两项任务纳入 Phase 1，但应在核心任务稳定后扩展。
- 日期跨月推进正确；存档后重启并载入可还原重要状态，奖励不会重复领取。
- 从开始到任务结算的演示通常可在 10–15 分钟完成。

## 3. Phase 1 范围

**地点：** 清洲（`owari.kiyosu`）、热田（`owari.atsuta`）、那古野（`owari.nagoya`）。Phase 1 共三个地点。地图只表达连接、位置和当前地点；仅允许连接地点间移动。已批准的简化旅行规则为清洲↔热田 1 天、清洲↔那古野 1 天，热田与那古野之间无直达路线；这些规则不代表历史事实。

**角色与世界：** 一名玩家武士、五名虚构 NPC、一支势力。NPC 使用稳定 ID、静态定义与运行时关系状态；Phase 1 不要求日程或自主 AI。

**内容：** 三项简单任务。首项任务的目标链为调查供给问题、在热田与商人交涉、回清洲报告并获功绩；具体条件、分支、数值奖励须在实施该任务时成为数据并可验证。避免以史实人物或未经核验的历史断言填充虚构占位内容。

**界面：** Unity uGUI；地图地点可点击；城镇菜单、日历/状态、对话及任务反馈。无需 UI Toolkit。M1 原型日历从 1560 年 4 月 1 日开始，每次操作推进一天，使用简化的前推公历（proleptic Gregorian calendar），正确处理月份与年份切换。

**存档：** 一个存档槽，明确 `saveFormatVersion`；包括恢复游戏所必需的日期、地点、玩家进度、关系/任务状态及奖励领取状态。详细字段需在 M5 实施任务中定稿。

## 4. 明确不做（Phase 1）

多职业、可步行城镇、NPC 日程/自主模拟、大型 Tilemap 世界、复杂 AI、战术战斗/攻城、复杂政治与城池经营、完整经济贸易、Mod 工具链、多个存档槽。它们属于长期愿景，不作为本阶段验收条件。

## 5. 长期愿景（不承诺 Phase 1 实现）

可扩展职业（武士、商人、忍者、海盗等）、角色成长、NPC 自主行为与关系、历史/动态事件、功绩与晋升、势力/城池管理、经济贸易、战斗/攻城、数据驱动内容和未来 Mod 支持。

## 6. 内容与版权

所有角色肖像、地图/背景、美术、音乐、对白、界面视觉及文本须原创或取得明确授权。以《太阁立志传5》为体验参考，不复制其资产、文字、音乐或界面设计。占位 NPC 使用虚构姓名与原创对白。

## 7. 用户体验与可访问性基线

首版确保核心文本清晰、按钮有明确反馈、当前日期/地点/任务目标可辨认；对话选择的关系影响须反馈在界面。键鼠可完成整个流程。更细的无障碍目标、分辨率基线、文本速度、语言支持将在 UI 实施卡中明确。

## 8. 已批准设计决策与后续保留项

已批准的设计决策包括：uGUI、菜单城镇、三个地点及稳定 ID（清洲 `owari.kiyosu`、热田 `owari.atsuta`、那古野 `owari.nagoya`）、五名虚构 NPC、一支势力、三项任务、单存档槽、先完成一个任务闭环；保留现有 `PlayerMovement2D.cs` 且不将其接入本阶段地图/城镇流程。M1 日历规则：新局从 1560-04-01 开始，每次推进一天，采用简化前推公历并正确切换月份/年份。M2 旅行规则：清洲↔热田和清洲↔那古野均为 1 天，热田与那古野无直达路线；这些是简化游戏规则，不代表历史事实。精确任务奖励延后至 M4 决定。各里程碑实施仍须单独授权；UI 视觉方向和存档失败提示在对应实施阶段选择简单可测方案，不扩展为新系统。


---

<a id="m3-demo"></a>
## 来源：M3_DEMO.md

# M3 Play Demo Candidate - Town NPC Dialogue
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


## Start

Open `Assets/Scenes/FoundationScene.unity` in Unity 6000.6.6f1 and press **Play**. Select Atsuta (熱田) on the world map and confirm travel (确认出发).

## Try

1. Confirm the travel preview shows one day and an arrival date of 1560-04-02.
2. In Atsuta, open **Characters** (人物). The roster should show Kuroda Iori (黑田 伊织) with relationship 0.
3. Start the conversation and choose **Accept**. The terminal dialogue should say “Trust is earned.” and show relationship +10.
4. Return to the town, then return to the map. Atsuta and 1560-04-02 should remain current.

The other dialogue choice is covered by EditMode tests; town-specific roster assignments cover all five NPCs across Kiyosu, Atsuta, and Nagoya.

## Verification

- Unity 6000.6.6f1 opened this worktree with `FoundationScene` loaded; script compilation completed with no C# compiler diagnostics in the current Editor log.
- `FoundationSceneValidator`: PASS, 0 errors. It checked required objects, serialized references, hierarchy, and RectTransforms.
- EditMode: 136 passed, 0 failed.
- PlayMode test tab: no tests are present.
- Manual Play Mode: the path above completed; the map retained Atsuta and the travel date after returning from town.
- Console after Play Mode: 0 errors and 0 warnings.

## Status

M3 was accepted by the user on 2026-10-31, merged to `main` at `2ca9463`, and tagged `demo-M3`. Independent read-only review found no Critical or Important findings. Formal Kanban review tools were unavailable in this session.


---

<a id="m3-plan"></a>
## 来源：M3_PLAN.md

# M3 Plan - Town NPC Dialogue
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


## Goal

Deliver the Phase 1 character interaction loop: five fictional NPCs with stable IDs, town-specific rosters, branching dialogue, a relationship change shown in the UI, and safe diagnostics for unknown NPC IDs.

## Tasks

1. Carry committed M3 character, relationship, and dialogue rules onto the accepted M2 base. **Done** on `feature/m3-accepted-integration`.
2. Integrate town rosters and dialogue UI in FoundationScene, keeping the accepted M2 map layout and return-to-map flow. **Done** in the current worktree.
3. Verify all UI references and RectTransforms with the FoundationScene text validator; update layout through the existing transactional updater if required. **Done**; validator reports PASS with 0 errors.
4. Compile in Unity 6000.6.6f1; run EditMode tests and available PlayMode tests; check Console errors. **Done**; 136 EditMode tests passed, no PlayMode tests are present, and Console showed 0 errors/warnings.
5. Walk the Play Mode demo path and perform an independent diff/evidence review. **Done**; the review found no Critical or Important findings and listed minor follow-ups in `PROJECT_STATE.md`.
6. Update state/demo docs, push the feature branch, and deliver the M3 Play Demo for user acceptance. **Done**; branch is published, the user accepted the demo, and the milestone is merged and tagged `demo-M3`.

## Acceptance

The player can travel to a town, view only that town's NPCs, start a conversation, choose a branch, see the relationship result, and return without losing the world-map state. All five NPCs are assigned, and invalid IDs produce a diagnostic instead of an exception.


---

<a id="m4a-demo"></a>
## 来源：M4A_DEMO.md

# M4a Supply Shortage — Accepted Historical Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


**Status: User accepted the M4a demo.** EditMode, scene validation, and user-reported Play Mode checks are recorded below.

Source implementation commits are based on accepted M3 `a93887c`; relevant fixes are `5fdde00` (enter current town) and `74948b9` (unique mission feedback name and safe scene migration).

## Intended playable walkthrough

1. Start a new game in Kiyosu and accept the supply-shortage mission.
2. Travel to Atsuta; investigate the shortage and negotiate with the existing NPC Kuroda Iori.
3. Return to Kiyosu and report to claim exactly +10 merit.
4. Attempt a second report and confirm merit does not increase again.

## Current implementation

- Mission-specific stages live in plain C# and are coordinated through `GameSession`.
- One uGUI panel displays mission progress and available actions.
- A transactional Editor updater and scene validator are implemented for the existing `FoundationScene`.
- No package or asset was added. Mission persistence is deferred to M5.

## Verification evidence

- Unity version: 6000.6.6f1; the single existing Editor is open on this M4a worktree.
- Compile: after the final button-label change, the existing Editor logged a successful Tundra build; no C# compiler diagnostics appear in `Logs/Editor.log`.
- EditMode (2026-11-01 16:06:30Z / 06:06:30 local): **214 passed, 0 failed, 0 skipped, 214 total**. This includes `CurrentLocationButton_EntersStartingTownWithoutTravel`, the legacy feedback migration test, and all three M2 layout regressions. Task 2 and Task 3 same-task static reviews approved their respective fixes.
- Console: the user replied `pass` after being asked to clear the Console and run the mission walkthrough. Exact Console counters and individual runtime values were not supplied, so this remains a user-reported result rather than independently verified evidence.
- Scene migration: the reviewed updater renamed the M4 child from `FeedbackText` to `MissionFeedbackText`, preserving the M2 map's unique `FeedbackText`; the saved `FoundationScene` diff contains that single object-name change.
- Project scene validation: the fresh `Logs/SceneValidationReport.txt` records `RESULT: PASS; Errors: 0`, with all M4a panel references bound.
- Play Mode walkthrough: user-reported pass after the request to play the complete Kiyosu → Atsuta → Kiyosu loop; no independent runtime capture was made.
- Visual follow-up: the supplied screenshot showed the current-town button hint wrapping and looking inconsistent beside other town buttons. All three buttons now show only their town names; the separate current-location marker identifies the current town. The user reported the visual re-check passed.
- User acceptance: explicitly accepted by the user on 2026-11-01. Console counters and individual runtime values were not provided; the user reported pass after being asked to clear Console and complete the walkthrough.

## Changed files

Source changes since accepted M3 base `a93887c`:

- `Assets/Scripts/Domain/SupplyShortageMission.cs` and `.meta`
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and `.meta`
- `Assets/Scripts/Presentation/WorldMapController.cs`
- `Assets/Scripts/Editor/AddM4SupplyShortageMissionPanel.cs` and `.meta`
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Scenes/FoundationScene.unity` (mission panel integration and reviewed feedback-name migration)
- `Assets/Tests/EditMode/SupplyShortageMissionTests.cs` and `.meta`
- `Assets/Tests/EditMode/GameSessionMissionTests.cs` and `.meta`
- `Assets/Tests/EditMode/SupplyShortageMissionPanelTests.cs` and `.meta`
- `Assets/Tests/EditMode/AddM4SupplyShortageMissionPanelTests.cs` and `.meta`
- Approved design and implementation plan under `docs/superpowers/`.

The accepted scene change is included in checkpoint `9c10959e5792777ea433a16c590034f406dccba1` and tagged `demo-M4a`.

## Final checkpoint

The accepted candidate is checkpointed on `feature/m4a-supply-shortage-task` at `9c10959e5792777ea433a16c590034f406dccba1`, tagged `demo-M4a`, and fast-forwarded to local `main`. No remote push was made.


---

<a id="m4b-demo"></a>
## 来源：M4B_DEMO.md

# M4b Two Simple Missions — Accepted Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


**Status: Accepted by the user on 2026-11-01.**
**Checkpoint:** `70ac62f0d8a9866488ae194c3cf55c68a8be2e29`, tagged `demo-M4b` and fast-forwarded into local `main`. No remote push was made.

## Scope

- Suzu's sealed letter: accept in Kiyosu, deliver to Aya in Nagoya, receive +5 merit once.
- Nagoya market tally: accept from Jirobei in Nagoya, investigate with Genzaemon in Kiyosu, report to Jirobei in Nagoya, receive +5 merit once.
- M4a remains unchanged at +10 merit. Total merit across all three Phase 1 missions is capped at 20.

## Current implementation

- Plain-C# mission models and named `GameSession` actions are implemented.
- A fixed two-card extension to the existing M4a panel and a transactional FoundationScene updater are authored.
- EditMode coverage is authored for mission rules, aggregate merit, panel behavior, and scene-updater validation/rollback.
- Static review approved the mission/session changes and the revised M4b UI geometry; the acceptance feedback is kept to one line and layout assertions cover internal separation.
- The design and plan are in `docs/superpowers/specs/2026-11-01-m4b-two-simple-missions-design.md` and `docs/superpowers/plans/2026-11-01-m4b-two-simple-missions.md`.

## Verification status

- Source checks: source-only whitespace checks pass; all six new `.meta` GUIDs occur once in Assets, Packages, and ProjectSettings. The saved Unity scene diff includes serializer-produced trailing whitespace on blank YAML fields (`m_Name:` / `m_Text:`); it was left as saved by the validated updater.
- Unity compilation: after `Assets → Refresh` in the existing Editor, `Logs/Editor.log` was updated at 2026-11-01 06:55:35 and records Tundra build successes at lines 9144 and 9150. No C# compiler diagnostics appear after those builds.
- EditMode: the existing Editor's Run All finished 2026-11-01 17:01:32–17:01:33Z. `C:/Users/panye/AppData/LocalLow/DefaultCompany/My project/TestResults.xml` reports 322 passed, 0 failed, 0 skipped; the EditMode suite lists 318 passed, and the four `TownNpcAssignmentsTests` also passed in the same run. The report includes `AddM4bMissionBoardTests` (4/4 passed).
- FoundationScene: the existing Editor logged `M4b mission board validated; FoundationScene saved.` and modified `Assets/Scenes/FoundationScene.unity` through the updater. Fresh `Logs/SceneValidationReport.txt` (2026-11-01 07:04:48 local) reports the scene loaded and clean, both M4a and M4b hierarchy/bindings/containment passing, and `RESULT: PASS; Errors: 0`.
- Console: the user reported the requested playtest passed after clearing Console, including the red-error check; no exact counter snapshot or error-line details were supplied. The Editor log contains service/licensing diagnostics and the rollback warning emitted by a passing fault-injection test; no runtime C# exception lines appear after the latest Play Mode entry.
- Play Mode: the user reported `Playtest passed` after the requested loops and checks. The Editor log records a Play Mode entry and no C# exception lines afterward, but it does not capture the observed merit/date values; those remain user-reported.
- Independent technical QA: ACCEPT; no blocking code or scene defects. The reviewer also confirmed FoundationScene preserves existing serialized object IDs.
- Kanban workflow: no Kanban review tools are configured in this session; the independent review result is recorded here.
- User acceptance: the user explicitly accepted M4b on 2026-11-01 after the reported Play Mode pass.

## Play test path after verification

1. Start a new game in Kiyosu; accept the sealed-letter mission.
2. Travel to Nagoya; deliver the letter and verify +5 merit.
3. Accept the market-tally mission from Nagoya; travel to Kiyosu and investigate.
4. Return to Nagoya and report; verify another +5 merit.
5. Complete M4a as a regression and verify aggregate merit reaches 20 at most.
6. Repeat final actions and attempt wrong-location actions; rewards, date, and location must remain unchanged.

The user accepted the playable demo on 2026-11-01 before the M4b checkpoint, merge, and tag were created.


---

<a id="m5-demo"></a>
## 来源：M5_DEMO.md

# M5 Single-Slot Save — Accepted Historical Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


**Status: Accepted by user Play Mode run on 2026-11-01.**
**Branch:** `feature/m5-save-slot`
**Design:** `docs/superpowers/specs/2026-11-01-m5-single-slot-save-design.md`
**Plan:** `docs/superpowers/plans/2026-11-01-m5-single-slot-save.md`

## Scope

- One versioned save slot with `saveFormatVersion = 1`.
- Save/restore the current date, current location, all three mission stages, and all NPC relationship values.
- Merit is derived from mission stages, not stored, so loading cannot duplicate a reward.
- Atomic save: a failed write leaves the previous slot untouched.
- Clear errors for missing, unreadable, corrupt, unsupported-version, and invalid data; loading never overwrites the slot.
- A compact Save/Load control on the map screen with a one-line status message.
- No new package, asset, scene, or generic save framework.

## Implementation

- New `Assets/Scripts/Persistence` assembly: `SaveData`, `RelationshipSaveEntry`, `SaveDataValidator`, `SaveSlotStore`, `SaveOperationResult`, `SaveLoadResult`, and `SaveErrorCode`.
- `SaveDataValidator` rejects wrong versions, invalid dates/locations/stages, unknown or player relationship IDs, duplicates, and values outside `[-100, 100]`.
- `SaveSlotStore` writes `<Application.persistentDataPath>/SengokuChronicle/save-slot-1.json` through a same-directory temporary file and `File.Replace`/`File.Move`.
- The three mission models expose validated `TryRestore(stage)` factories that derive merit (`+10`, `+5`, `+5`) from the stage.
- `GameSession` adds `CaptureSave`, `TryApplySave`, `SaveTo`, and `LoadFrom`. `TryApplySave` validates and builds all replacement state before assigning it, and clears transient dialogue instances.
- `SaveLoadPanel` binds the session, store, two buttons, a status text, and the map controller. A successful load calls `WorldMapController.ReturnToMap()`, which closes the mission/NPC panels and refreshes the map.
- `AddM5SaveLoadPanel` creates `M5SaveLoadRoot` with `M5SaveButton`, `M5LoadButton`, and `M5SaveStatusText`, inserts it before `TownMenuRoot`, validates bindings and non-overlap with all existing map controls, and rolls back on failure.
- `FoundationSceneValidator` now reports `SaveLoadPanel` and checks the M5 hierarchy, bindings, containment, and map separation.

## Verification evidence (observed)

- **Compilation:** Unity 6000.6.6f1 batchmode imported the project and compiled with no `error CS` lines in `m5-batchmode.log`. The project `Library/` was generated by this run.
- **EditMode:** the final integrated run (`m5-final-editmode.xml`) reports `total="381" passed="381" failed="0" inconclusive="0" skipped="0"` (start `2026-11-01 17:47:34Z`, duration 1.49 s); an earlier pre-scene run reported the same totals. This includes the new validator, store, session, panel, and updater tests plus all existing M1–M4b tests.
- **Scene integration:** `AddM5SaveLoadPanel.RunBatchIntegration` opened `FoundationScene`, ran the transactional updater, validated, and saved. `Assets/Scenes/FoundationScene.unity` gained 536 lines with no deletions; the added objects are `M5SaveLoadRoot`, `M5SaveButton`, `M5LoadButton`, `M5SaveStatusText`, and their labels.
- **Scene validation:** fresh `Logs/SceneValidationReport.txt` reports `FOUND SaveLoadPanel: M5SaveLoadRoot`, `PASS M5 save/load hierarchy, unique controls, bindings, and map separation.`, and `RESULT: PASS; Errors: 0`.
- **Play Mode acceptance:** the user ran the acceptance path in the Editor on this worktree and reported `pass` on 2026-11-01. The slot written during that run is `C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\SengokuChronicle\save-slot-1.json` (684 bytes, 2026-11-01 10:54 local / 20:54Z). It contains `saveFormatVersion: 1`, date `1560-04-05`, location `owari.kiyosu`, `supplyShortageStage: 4`, `sealedLetterStage: 0`, `marketTallyStage: 0`, and five relationship entries at `0`. This confirms the save path wrote a valid versioned slot after the date advanced from the `1560-04-01` default and the supply mission reached stage 4; merit is absent by design and derived from the stages.
- **Evidence gaps:** the location stayed `owari.kiyosu` and all relationships stayed `0`, so location and relationship persistence were not exercised in this run. The agent did not observe the load result, the no-duplicate-reward retry, or Console counters; acceptance rests on the user's Play Mode `pass`.
- **Console:** not captured. The batchmode log is not the Editor Console, so no Console error/warning counts are claimed here.
- **Remote state (verified 2026-11-01):** `origin/main` is `24102fe`; tags `demo-M2`, `demo-M3`, `demo-M4a`, and `demo-M4b` exist on `origin`.

## Required Play Mode acceptance path

1. Open this worktree (`C:/Users/panye/orca/workspaces/SengokuChronicle-M2/default`) in Unity 6000.6.6f1 and load `Assets/Scenes/FoundationScene.unity`.
2. Clear the Console, enter Play Mode, and advance/travel so the date and location differ from the new-game defaults.
3. Accept and partially or fully complete at least one mission, and change at least one NPC relationship through dialogue.
4. Click `存档`; confirm the status line reports success.
5. Exit Play Mode, re-enter it (fresh session), and click `读档`.
6. Confirm the date, location marker, mission stages, total merit, and relationship values match the saved state.
7. Retry the final action of any completed mission; confirm it is rejected and merit does not increase.
8. Overwrite the slot with a corrupt or unsupported-version file and confirm loading shows a clear error while the live session and the slot file remain unchanged.
9. Report the observed values and Console counters.

Accepted by the user on 2026-11-01; the final M5 checkpoint, merge to `main`, and `demo-M5` tag followed that acceptance.


---

<a id="m6-demo"></a>
## 来源：M6_DEMO.md

# M6 Title Screen & Demo Packaging — Accepted Historical Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


**Status: Accepted by user Play Mode run on 2026-11-01.**
**Branch:** `feature/m5-save-slot` (user directed continuing M6 on the same branch and worktree)
**Design:** `docs/superpowers/specs/2026-11-01-m6-title-demo-packaging-design.md`
**Plan:** `docs/superpowers/plans/2026-11-01-m6-title-demo-packaging.md`

## Scope

- Play Mode boots to a full-screen title overlay with 「新游戏」 and 「继续」.
- 继续 reads the existing M5 single slot; missing, corrupt, or unsupported-version slots show a clear message, disable the button, and never mutate the live session or the slot file.
- 新游戏 resets the session to 1560-04-01 / 清洲 before entering the map.
- Demo closing line: when all three missions reach `RewardClaimed`, the map shows 「Phase 1 演示完成。总功绩：20。」.
- No new packages, no save-format change, no change to the in-game M5 save/load panel.

## Implementation

- `TitleScreenController` (Presentation): binds session, map controller, title root, two buttons, status text; `SetStore()` test seam; default `SaveSlotStore.CreateDefault()`; `RefreshTitleState()` distinguishes NotFound / invalid / valid slots; `StartNewGame()` = `InitNewGame` + `ReturnToMap` + hide; `TryContinue()` = `LoadFrom` (atomic) + `ReturnToMap` + hide on success, status + stay on failure.
- `DemoSummaryPanel` (Presentation): idempotent `Refresh()` shown/hidden from `GameSession.IsDemoComplete` (new pure-C# property); lives on the always-active GameSession object.
- `AddM6TitleScreen` (Editor): one-Undo-group transactional updater that creates `Canvas/M6TitleRoot` (full-screen opaque raycast-blocking Image, last sibling, saved inactive) with `M6TitleLabel`, `M6NewGameButton`, `M6ContinueButton`, `M6TitleStatusText`, plus `M2WorldMapRoot/M6DemoSummaryText` (inserted before `TownMenuRoot`, saved inactive, at (460,-110) 260x44), attaches both components to the GameSession object, binds every reference, validates bindings/sibling order/containment/overlap against M5 and travel controls, and saves only after validation. Menu: `Tools/Sengoku Chronicle/Add M6 Title Screen`; batch entry `RunBatchIntegration()`.
- `FoundationSceneValidator` reports the M6 roots, the controller/panel bindings, and runs `AddM6TitleScreen.Validate`.

## Verification evidence (observed)

- **Compilation:** Unity 6000.6.6f1 batchmode; `m6-batchmode-1.log`, `m6-batchmode-2.log`, `m6-batchmode-final.log` contain no `error CS` lines.
- **EditMode (pre-integration):** `m6-editmode-1.xml` reports `total="401" passed="401" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 21:27:53Z) — the 381 existing M1–M5 tests plus the 20 new M6 tests (`TitleScreenControllerTests` 8, `DemoSummaryPanelTests` 5, `AddM6TitleScreenTests` 7).
- **Scene integration:** `RunBatchIntegration` opened FoundationScene, ran the transactional updater, and saved; batch log line `M6 title integration completed and FoundationScene was saved.` with no rollback warnings. The scene diff is +764/−12 (the removed lines are Unity re-serializing existing Image/reference blocks; no controls were deleted).
- **Scene validation:** fresh `Logs/SceneValidationReport.txt` reports `FOUND TitleScreenController: GameSession`, `PASS M6 title screen hierarchy, bindings, gating, and demo summary.`, `BOUND GameSession.titleRoot/newGameButton/continueButton/statusText/summaryText`, `M6TitleRoot [inactive]` as the last Canvas child, and `RESULT: PASS; Errors: 0`.
- **EditMode (post-integration, final):** `m6-final-editmode.xml` reports `total="401" passed="401" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 21:45:01Z).
- **Play Mode acceptance:** the user ran the acceptance path in the open Editor (6000.6.6f1, PID 44880) on this worktree and reported `pass` on 2026-11-01. Exact observed values and Console counters were not supplied by the user, so acceptance rests on their report; the agent verified the scene's M6 structure and bindings via the validator report above but did not independently record runtime results.
- **Console:** not captured. Batchmode logs are not the Editor Console, so no Console counters are claimed.

## Required Play Mode acceptance path

1. Open this worktree in Unity 6000.6.6f1 and load `Assets/Scenes/FoundationScene.unity`; clear the Console.
2. Enter Play Mode: the title overlay covers the map; the status line reflects the slot (the M5 acceptance slot `1560-04-05` / supply stage 4 exists, so 继续 should be enabled).
3. Click 继续: date/day, location, mission stages, and merit match the slot; title disappears; map is interactive.
4. Exit and re-enter Play Mode; click 新游戏: date resets to 1560-04-01, location 清洲, merit 0; title disappears.
5. With the slot renamed/deleted and Play re-entered, confirm 继续 is disabled with 「尚未找到存档，可开始新游戏。」 and 新游戏 still works.
6. Complete all three missions (or continue a completed save): the map shows 「Phase 1 演示完成。总功绩：20。」.
7. Report the observed values and Console counters.

Accepted by the user on 2026-11-01; the final M6 checkpoint, merge to `main`, and `demo-M6` tag followed that acceptance.


---

<a id="m7-demo"></a>
## 来源：M7_DEMO.md

# M7 Merit & Promotion — Accepted Historical Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


**Status: Accepted by user Play Mode run on 2026-11-01.**
**Branch:** `feature/m5-save-slot` (user directed continuing on the same branch and worktree)
**Design:** `docs/superpowers/specs/2026-11-01-m7-merit-promotion-design.md`
**Plan:** `docs/superpowers/plans/2026-11-01-m7-merit-promotion.md`

## Scope

- Pure-C# promotion rules: four game-abstracted ranks from total merit — 足轻(0) / 番头(5) / 组头(10) / 武士头(20). These are simplified growth labels, not historical office claims.
- A permanent rank line on the map screen: current merit, current rank, and the next rank's threshold (「最高」 at the top rank).
- The M6 demo-completion summary now names the final rank.
- No new merit sources, no mission reward changes, **no save-format change** (rank is derived from stages via merit and never stored), no new package.

## Implementation

- `Domain/PromotionRules.cs`: `PromotionRank` enum + ascending immutable threshold table; `RankFor` (negatives clamp to 0), `ChineseName`, `MinimumMerit`, `TryNextRank` (false at top), `TierCount`.
- `GameSession`: derived `Rank` and `RankName` properties from `Merit`.
- `Presentation/RankPanel.cs`: same idempotent `Refresh()` + `Update()` poll as `DemoSummaryPanel`; `Bind(session, text)`; no-session guard leaves the text untouched.
- `DemoSummaryPanel`: summary format extended to 「Phase 1 演示完成。总功绩：{0} · {1}。」 with the rank name.
- `Editor/AddM7RankPanel.cs`: transactional updater creating `Canvas/M2WorldMapRoot/M7RankText` at (-450,100) size 240×40 font 11, inserted before `TownMenuRoot`, attaching `RankPanel` to the GameSession object and binding it; validates uniqueness, parent/sibling order, containment, and separation from all map controls plus the M5/M6 texts; one Undo group, validate-before-save, rollback on failure. Menu `Tools/Sengoku Chronicle/Add M7 Rank Panel`; batch entry `RunBatchIntegration()`.
- `FoundationSceneValidator`: reports `FOUND RankPanel`, its bindings, and runs `AddM7RankPanel.Validate`.

## Verification evidence (observed)

- **Compilation:** Unity 6000.6.6f1 batchmode; `m7-batchmode-1.log` and `m7-batchmode-final.log` contain no `error CS` lines.
- **EditMode (pre-integration):** `m7-editmode-1.xml` reports `total="425" passed="425" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 22:19:48Z) — the 401 M1–M6 tests plus 24 new M7 tests (PromotionRules boundary/name/chain tests, RankPanel display tests, updater structure/rollback/refusal tests, and the extended DemoSummary rank assertion).
- **Scene integration:** `RunBatchIntegration` opened FoundationScene, ran the transactional updater, and saved (`m7-integration.log`: `M7 rank integration completed and FoundationScene was saved.`); no rollback warnings. The scene diff is +96/−1; the single changed line is Unity re-normalizing the dormant `m_MaxSize` (0..40→44) on `M6TitleLabel`, whose `m_BestFit: 0` — an inert serialization artifact, not a code edit.
- **Scene validation:** fresh `Logs/SceneValidationReport.txt` reports `FOUND RankPanel: GameSession`, `BOUND GameSession.rankText -> M7RankText (Text)`, `PASS M7 rank panel hierarchy, bindings, containment, and map separation.`, and `RESULT: PASS; Errors: 0`.
- **EditMode (post-integration, final):** `m7-final-editmode.xml` reports `total="425" passed="425" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 22:36:35Z).
- **Play Mode acceptance:** the user ran the acceptance path in the Editor on this worktree and reported `pass` on 2026-11-01. Exact observed rank values and Console counters were not supplied by the user; acceptance rests on their report alongside the recorded compile, EditMode 425/425 (both pre- and post-integration runs), integration, and validator evidence above.
- **Console:** not captured. Batchmode logs are not the Editor Console, so no Console counters are claimed.

## Required Play Mode acceptance path

1. Open this worktree in Unity 6000.6.6f1, load `Assets/Scenes/FoundationScene.unity`, clear the Console.
2. Play → title → 新游戏. Confirm the map's top-left rank line reads 「功名：0 · 足轻（下一阶 番头 5）」.
3. Complete the sealed-letter mission (+5 merit) → line shows 番头 (next 组头 10); complete the market tally (+5) → 组头; complete the supply shortage (+10) → 武士头（最高）and the demo summary line reads 「Phase 1 演示完成。总功绩：20 · 武士头。」.
4. 存档 → exit and re-enter Play → 继续 → the rank line and summary show the same rank; retry a completed mission's final action → merit and rank stay unchanged.
5. Report the observed values and Console counters.

Accepted by the user on 2026-11-01; the final M7 checkpoint, merge to `main`, and `demo-M7` tag followed that acceptance.


---

<a id="phase1-roadmap"></a>
## 来源：PHASE1_ROADMAP.md

# Phase 1 里程碑、验收与 Kanban 提案

> 状态：历史路线图。原 Phase 1 里程碑已完成并留档；后续游戏开发路线由 [GDD_v4.0.md](../../GDD_v4.0.md) 的 M0–M8 取代。此文件仅保留旧项目的决策与验收记录。

## 建议执行原则

每个功能卡独立可验证；先用最小 playable loop 验证架构，再扩展内容。每项 Unity 实施均由 UnityCoder 实作、同卡请求 QATester 审查；失败回到 UnityCoder 同卡修正，最多十次 QA 审查，重复同缺陷或基础架构错误则提前阻断并请人决策。QA 通过后 Mochi 最终技术签核。一次只允许一个任务编辑同一场景/Prefab。每个获批开发任务一个逻辑 commit；提交前检查状态、只暂存相关文件并取得用户批准；不自动 push。若需人工 Play Mode 验证，先等用户接受，再建最终 checkpoint commit。

## M0 — 项目基线与设计确认（规划门槛，不算玩法里程碑）

**状态：** 已完成；Phase 1 设计基线已批准。

## M1 — Game Foundation

**状态：** 已完成；人工 Play Mode 验收通过。

**Git checkpoint：** `70b03e2fcadeae9a35a28a1e20a48b4fc3e8b175`

**交付摘要：** `GameSession`、`WorldClock` 与 uGUI 日期面板；新局日期为 1560-04-01，时钟使用简化前推公历。保留 `PlayerMovement2D.cs`，未将其接入 M1 流程。

## M2 — World Map, Travel and Town Menus

**状态：** 设计已批准；实施未获授权，尚未创建 Kanban 执行卡。

**建议顺序执行卡（仅提案，尚未创建）：**
1. `实现地点规则、GameSession 旅行用例与 EditMode 测试`
2. `实现世界地图、城镇菜单与 FoundationScene 安全整合`（卡 1 通过 QA 后开始）

**地点与稳定 ID：** 清洲 `owari.kiyosu`、热田 `owari.atsuta`、那古野 `owari.nagoya`。Phase 1 共三个地点；EditMode 测试须逐字断言三个完整 ID 字符串及名称映射。

**路线与旅行：** 清洲↔热田 1 天；清洲↔那古野 1 天；热田与那古野之间无直达路线。路线为无向连接，耗时是简化游戏规则，不代表历史事实。新游戏从清洲开始。点击目的地仅显示目的地、耗时和预计到达日期；确认前不改变日期或地点。确认时先验证路线和日期范围，再推进 WorldClock 并更新地点；失败时日期和地点都保持不变。

**界面与边界：** 使用 uGUI 世界地图和城镇菜单，复用 FoundationScene 的 M1 GameSession、WorldClock、日期面板、Canvas、EventSystem 和 Input System UI Input Module。所有新增 UI 位于 `M2WorldMapRoot` 下。城镇菜单仅有标明「未开放」且不可用的占位设施及返回地图功能。不做 NPC、对话、任务、城镇自由移动或真实设施功能；不修改 `PlayerMovement2D.cs`，不安装 Packages，不修改 SampleScene、Build Settings 或 ProjectSettings。

**EditMode 验收：** 明确断言三个 ID/名称映射；新局从清洲开始；两条路线双向可达且各耗时一天；热田与那古野无直达路线；预览及取消不改变游戏状态；未知地点、非法路线、日期溢出均不改变状态；成功旅行正确推进日期与地点；跨月、跨年正确；M1 WorldClock 回归测试通过。由 Unity Test Framework 实际运行并分别报告编译与 EditMode 结果。

**Play Mode 验收：** 三地点按钮及当前地点标记正确；预览、确认和取消正常；可进入并退出城镇菜单；占位设施明确不可用；M1 日期 UI 正常；无新增 Console 错误。需人工验收的行为须由用户明确接受，QA 通过不替代用户验收。

**场景生成安全：** Editor API 仅允许在已确认的 FoundationScene 中增量创建；生成前检查活动场景、未保存状态、现有依赖和 `M2WorldMapRoot`。根对象已存在则停止。将对象创建、引用配置及必要父子关系放入单一 Undo 组；结构验证全部通过前不保存。异常时使用 Unity Undo API 回滚本次已登记操作，检查残留；不重载场景，不自动删除不确定来源对象。磁盘保存失败及无法自动验证的回滚必须如实报告。

两卡均采用 UnityCoder→QATester 同卡审查/修复，最多十次 QA 审查尝试；重复同类错误或基础架构问题提前升级。用户明确接受必要的人工 Play Mode 验收前，不做最终 checkpoint commit。

## M3 — Characters and Relationships

**建议卡：** `加入玩家与五名 NPC、关系状态和分支对话`

**范围：** 稳定角色 ID、一个玩家武士定义、五名虚构 NPC、对话选项、关系值变更与界面反馈。

**验收：** 可对话；选项走向符合配置；至少一条选择改变目标关系数值且 UI 显示前后变化；陌生/错误角色 ID 有诊断而不崩溃；原创文本。

**依赖：** M1；使用 M2 地图/城镇入口时与 M2 顺序衔接。

## M4a — 首个完整任务（M4 的硬性第一阶段）

**建议卡：** `完成补给短缺调查任务端到端闭环`

**范围：** 接取、到热田调查/商人交涉、返回清洲报告、结算功绩；只做该任务所需的受限条件/效果，不预建通用任务脚本系统。

**验收：** 从接取到结算端到端可玩；状态转换有测试；条件不满足时不能交付；功绩只发一次，重复提交不重复奖励；界面给出进度和结果。

**依赖：** M1、M2、M3。首项完整且通过 QA 后才开展 M4b。

## M4b — 其余两个简单任务

**建议卡：** `扩展另外两项 Phase 1 简单任务`

**范围：** 复用 M4a 已验证的最小规则与 UI 表现；不扩大为任意脚本系统。

**验收：** 两项任务均可从触发到结果完成；数据校验、状态与奖励单次领取测试通过。

**依赖：** M4a。

## M5 — 单槽存档与可玩演示

**建议卡：** `实现版本化单槽存档并验证完整演示恢复`

**范围：** DTO 含 `saveFormatVersion`；保存/加载日期、地点、任务/领取标志、关系及必要玩家进度；完整 10–15 分钟体验整合和回归测试。

**验收：** 保存后关闭 Play Mode/重启应用/重新载入，重要状态一致；重复载入不会重复领奖；不支持版本/损坏数据有清晰错误且不静默覆写；端到端演示无未完成阻断。

**依赖：** M1、M2、M3、M4a、M4b。

## M5 当前状态（2026-11-01）

M5 在 `feature/m5-save-slot` 实现，并已获得用户 Play Mode 接受（用户报告 `pass`）。新增 `SengokuChronicle.Persistence` 程序集（`SaveData` v1、校验器、原子单槽存储）、三个任务的 `TryRestore` 工厂、`GameSession` 的 `CaptureSave`/`TryApplySave`/`SaveTo`/`LoadFrom`，以及地图界面的 `M5SaveLoadRoot`（存档/读档/状态文本）。merit 不写入存档，而由任务阶段推导，重复载入不会重复领奖。

验证证据（2026-11-01）：Unity 6000.6.6f1 batchmode 编译无 `error CS`；EditMode 381/381 通过（0 failed / 0 skipped）；事务更新器已保存 `FoundationScene`（+536 行、无删除）；`Logs/SceneValidationReport.txt` 为 `RESULT: PASS; Errors: 0`，M5 层级/绑定/与地图控件分离检查通过。用户在编辑器 Play Mode 中执行验收并报告通过；本次保存文件为 `save-slot-1.json`（684 字节，10:54 本地时间），含 `saveFormatVersion: 1`、日期 `1560-04-05`、地点 `owari.kiyosu`、`supplyShortageStage: 4`、其余任务阶段 `0`、五条关系值均为 `0`。该次运行未改变地点与关系值，Console 计数与读档细节未由代理独立记录。

远端核对（2026-11-01）：`origin/main` 为 `24102fe`，`demo-M2`、`demo-M3`、`demo-M4a`、`demo-M4b` 标签均存在于 `origin`；此前文档中的「没有远端推送」表述已过时。

## 建议 Kanban 依赖图

```text
M0 设计批准
  └─ M1 Game Foundation
       ├─ M2 地图与旅行 ──┐
       └─ M3 角色关系 ────┼─ M4a 首个完整任务 ── M4b 另外两任务 ─┐
                         └─────────────────────────────────────┤
                            M1 + M2 + M3 + M4a + M4b ──────────┴─ M5 存档与演示
```

M3 的独立角色/关系规则可在 M2 完成后进入游戏 UI；为避免多人同时编辑场景，建议同一时间只派一个整合性 Unity 卡，其他纯规则工作若独立且不碰共用场景才并行。无需为这些卡再拆大量 QA 子卡，沿用同卡审核流程。每项需要人工 Play Mode 检查的任务，QA 审查通过后仍须用户明确接受；QA 通过不等同于用户验收。用户未批准前不要创建上述卡片。

## M4a 当前候选状态（2026-11-01）

M3 已获接受，M4a 补给短缺任务正在 `feature/m4a-supply-shortage-task` 的单一隔离工作树中实现。用户批准固定任务模型、使用 Atsuta 的黑田伊织作为商人、Kiyosu 报告后一次性增加 10 merit。任务规则保持纯 C#；不新增 package 或通用任务框架。

Unity 6000.6.6f1 EditMode 全套测试于 2026-11-01 16:06:30Z 通过：214/214，0 failed，0 skipped；包含当前城镇入口和一致按钮标签回归、反馈名称迁移和三个 M2 布局测试。Task 2、Task 3 同卡静态审核均通过。经审核的 M4a 更新器已迁移并保存 `FoundationScene`；项目级 Validator 最新报告为 `PASS; Errors: 0`。最终按钮标签修改后 Editor 日志记录成功 Tundra 构建且无 C# 编译诊断。用户报告完整任务流程和后续视觉检查均 pass，并于 2026-11-01 明确接受 M4a demo。checkpoint `9c10959e5792777ea433a16c590034f406dccba1` 已建、标记为 `demo-M4a` 并快进合并到本地 `main`；没有远端推送。

## 跨阶段交付要求

每张完成卡用中文说明系统用途和重要脚本；列变更文件、实际自动化测试及结果、相关 Play Mode 步骤、限制/未解决事项、Git diff 简述；架构变化同步 `ARCHITECTURE.md`。不得宣称未真实执行的编译、测试或 Play Mode 通过。

## Status update — 2026-11-01

Historical status snapshot (2026-11-01): this paragraph tracked the old Sengoku Chronicle M4b candidate and is superseded. Under the current GDD v4.0 direction, SC-004–SC-009 are accepted; SC-010 is next. See `PROJECT_STATE.md` for current verification and remaining scene-validation evidence.

## M4b verification update — 2026-11-01

M4b code compiled in the existing Unity Editor after Refresh. EditMode Run All passed 322/322 (0 failed, 0 skipped), including all four `AddM4bMissionBoardTests`. The validated updater saved `FoundationScene`; the fresh project validator report passes with zero errors and a clean scene. The user reported the requested mission playtest passed and explicitly accepted M4b on 2026-11-01. Independent technical QA accepted the scoped implementation with no blocking defects. Runtime values and exact Console counters were not recorded separately. Checkpoint `70ac62f0d8a9866488ae194c3cf55c68a8be2e29` is tagged `demo-M4b` and fast-forwarded into local `main`; Kanban review tools were unavailable in this session and no remote push was made.

## M5 accepted update — 2026-11-01

M5（版本化单槽存档 + 地图存档/读档控件）已获用户 Play Mode 接受（用户报告 `pass`）。存档文件 `save-slot-1.json` 记录了 `saveFormatVersion: 1`、日期 `1560-04-05`、地点 `owari.kiyosu`、`supplyShortageStage: 4` 与五条关系值；本次运行未改变地点与关系值，Console 计数与读档细节未由代理独立记录。EditMode 终验 381/381；Validator 报告 `RESULT: PASS; Errors: 0`。checkpoint 合并 `main`（`d920571`）、打 `demo-M5` 标签并已推送 GitHub。

## M6 已接受状态（2026-11-01）

M6（标题界面与 Phase 1 演示封装）在 `feature/m5-save-slot` 实现（用户指定沿用同一分支与工作树），并已获得用户 Play Mode 接受（用户报告 `pass`）。Play Mode 启动进入全屏标题覆盖层 `M6TitleRoot`（新游戏/继续/状态行）；「继续」直接读取 M5 单槽，缺档/坏档/不支持版本均清晰提示且不会改动会话或槽文件；`GameSession.IsDemoComplete`（纯 C# 规则）驱动地图上的「Phase 1 演示完成。总功绩：20。」收尾行。场景集成沿用事务式更新器 `AddM6TitleScreen`（一个 Undo 组、验证后保存、失败回滚），Validator 已加入 M6 层级/绑定检查。

验证证据（2026-11-01）：batchmode 编译无 `error CS`；EditMode 401/401 通过（集成前 21:27:53Z 与集成后 21:45:01Z 各一轮，0 failed / 0 skipped）；`RunBatchIntegration` 保存 `FoundationScene`（+764/−12，删除行均为 Unity 重序列化既有块）；`Logs/SceneValidationReport.txt` 报告 M6 PASS 且 `RESULT: PASS; Errors: 0`。用户在同一工作树的编辑器中执行 Play Mode 验收并报告 `pass`（2026-11-01）；具体运行时数值与 Console 计数未另行记录，验收以用户报告为准。

## 新方向状态（2026-11-02）

此路线仅为旧 Sengoku Chronicle 方向档案；GDD v4.0 已取代其 Phase 1 顺序。新方向 SC-004–SC-009 已获用户试玩验收并随 `demo-SC004-SC009` 推送。当前下一项按 GDD 首批任务为 SC-010。详细状态及尚待场景验证的边界见 `PROJECT_STATE.md`。


---

<a id="phase2-roadmap"></a>
## 来源：PHASE2_ROADMAP.md

# Phase 2 Roadmap — Sengoku Chronicle

> 状态：历史路线图。M7 已接受并留档；此路线已由 [GDD_v4.0.md](../../GDD_v4.0.md) 的 M0–M8 开发计划取代。以下仅保留 Sengoku Chronicle 旧方向的实施与验收证据。

## M7 — 功名晋升（Merit & Promotion）

**范围：** 纯 C# 军阶规则（足轻0/番头5/组头10/武士头20，游戏化简化设定，非史实官职）；地图常驻「功名」行（当前功绩/军阶/下一阶门槛）；M6 演示摘要行显示最终军阶。

**验收：** 新游戏显示足轻(0)；每笔任务奖励使军阶按阈值正确晋升；三项任务全结算后为武士头（最高）且摘要行同步显示；存档/继续不改变或伪造军阶；重复领取不提高功绩或军阶；端到端演示无阻断。

**依赖：** M1–M6。

## 后续候选（未排期）

- 第四任务与数据驱动任务定义（内容扩展的前置）。
- NPC 日课/自主行为（Phase 1 明确排除项，需谨慎设计规模）。
- 势力/城经营与战斗：远期，暂不立项。

## M7 已接受状态（2026-11-01）

M7 在 `feature/m5-save-slot` 实现（用户指定沿用同一分支与工作树），并已获得用户 Play Mode 接受（用户报告 `pass`）。新增 `PromotionRules`（Domain，纯静态）、`GameSession.Rank/RankName`（派生，不入存档）、`RankPanel`（Presentation，幂等刷新 + Update 轮询）、`DemoSummaryPanel` 文案升级与事务式更新器 `AddM7RankPanel`（`M7RankText` 位于地图左上安全区 (-450,100) 240×40）。Validator 已加入 M7 层级/绑定/分离检查。

验证证据（2026-11-01）：batchmode 编译无 `error CS`；EditMode 425/425 通过（集成前 22:19:48Z、集成后 22:36:35Z，0 failed / 0 skipped）；`RunBatchIntegration` 保存 `FoundationScene`（+96/−1，唯一删除行为 Unity 对 `M6TitleLabel` 惰性字段 `m_MaxSize` 的再归一化，`m_BestFit: 0`，非代码改动）；`Logs/SceneValidationReport.txt` 报告 M7 PASS 且 `RESULT: PASS; Errors: 0`。用户执行 Play Mode 验收并报告通过；具体军阶数值与 Console 计数未由代理独立记录，验收以用户报告为准。

## 新方向状态（2026-11-02）

本路线仅保留旧 Sengoku Chronicle Phase 2 / M7 历史。GDD v4.0 是当前方向；新方向当前已验收至 SC-004–SC-009，下一项为 SC-010。不要将旧 M7 晋升功能解释为新方向 M7 进度。


---

<a id="risks-and-agents-proposal"></a>
## 来源：RISKS_AND_AGENTS_PROPOSAL.md

# 项目风险与 AGENTS.md 增补提案

> 本文件是旧方向下的历史提案草稿；其中关于《太阁立志传5》、Sengoku Chronicle 与 Phase 1 的范围控制均已过时。当前设计红线与里程碑以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准，协作流程以现行 [AGENTS.md](../../AGENTS.md) 为准。

> 历史提案：其中关于《太阁立志传5》体验参考和 Sengoku Chronicle 的范围控制已被 [GDD_v4.0.md](../../GDD_v4.0.md) 取代。新方向的设计红线、范围和里程碑以该计划为准。

## 风险与缓解

| 风险 | 影响 | 缓解建议 |
|---|---|---|
| 参考《太阁立志传5》被误解为复制其表达内容 | 著作权/商标风险 | 明确仅作体验参考；使用原创或获授权素材、文字、音乐和 UI；避免复制原作名称之外的独特表达及资产。 |
| 初学者范围膨胀 | 核心演示迟迟不可玩 | 以 M1–M5 验收为边界；其余愿景明确延期；先闭环一个任务再扩内容。 |
| 规则和 UI 混写导致难测 | 编译/回归风险、迭代缓慢 | 日期/任务/关系规则放普通 C#，用 EditMode 测试；uGUI 仅负责呈现与输入。 |
| 日期、行程规则不明确 | 结果不可预测，边界 bug | M0/M1 明确日期粒度、月份规则、起始日；M2 显式配置路线与耗时，并测试非法路线和跨月旅行。 |
| 重复领取奖励/任务状态丢失 | 进度或功绩错误 | 任务状态包含已领取标记；领取用例幂等；EditMode 与存档往返测试。 |
| 静态定义改 ID 使存档失效 | 存档加载失败或引用错位 | 稳定 ID；检查重复/缺失 ID；DTO 使用 ID 而非 Unity 对象引用；未知 ID 报错并保护原文件。 |
| Unity 场景/Prefab YAML 手工冲突 | GUID 破坏、资源损坏、并行覆盖 | 优先 Editor 自动化和 Unity API；保留 `.meta`；同一场景/Prefab 同时只允许一个任务编辑。 |
| 存档版本迁移与损坏恢复被延后 | M5 超出预期 | M5 初期先定版本策略和失败行为，最小 DTO 开始；自动化往返、未知版本与损坏数据测试。 |
| 缺少自动编译/Unity Editor 环境 | 无法证明实际可运行 | 由 UnityCoder/QATester 执行真实 Unity 编译/测试；报告明确区分无法运行、编译、EditMode、PlayMode 与人工检查。 |
| 当前工作区已有未跟踪文件 | 意外纳入提交或覆盖用户工作 | 本次已观察到 `Assets/Scripts.meta` 未跟踪；所有后续任务先看 git status，仅暂存任务相关文件。 |

## 建议加入现有 AGENTS.md 的最小增补文本（已应用）

> **状态（Status）**：**Applied** — 2026-10-20 实测写入 `AGENTS.md` 第 255 行，句意与本提案一致，无需再次修改。

现有 `AGENTS.md` 已有人工 Play Mode 验收要求。按用户澄清，增补句子已于 2026-10-20 实际写入 `AGENTS.md` 第 255 行：

> QA approval does not replace required manual Play Mode acceptance; do not mark that behavior accepted or create the final checkpoint commit until the user explicitly accepts it.

```markdown
QA approval does not replace required manual Play Mode acceptance; when manual acceptance is required, do not mark that behavior accepted or create the final checkpoint until the user explicitly confirms acceptance.
```

### 和现有规则的关系

现有 `AGENTS.md` 已规定 Unity 版本大类、团队职责、GitHub-first 研究、保留 `.meta`、场景/包安全、同卡 QATester 审查、最多十次 QA 审查及最终签核。增补句子已于 2026-10-20 写入 AGENTS.md 第 255 行，与提案意图一致，无需再次修改。


---

<a id="2026-10-31-m4a-supply-shortage"></a>
## 来源：docs/superpowers/plans/2026-10-31-m4a-supply-shortage.md

# M4a Supply Shortage Mission Implementation Plan
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to execute this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a playable single mission from accepting the supply-shortage investigation in Kiyosu through investigating and negotiating in Atsuta, then reporting in Kiyosu for exactly one +10 merit reward.

**Architecture:** Keep a mission-specific state machine in plain C# Domain code, with location-checked use cases coordinated by `GameSession`. Add a small uGUI mission panel under the existing world-map/town hierarchy; use a transactional Editor updater to bind it into `FoundationScene` without direct scene YAML edits.

**Tech Stack:** Unity 6000.6.6f1, C#, existing Domain/Application/Presentation/Editor assemblies, uGUI, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-10-31-m4a-supply-shortage-design.md`

## Global Constraints

- Keep task rules in plain C# and UI in uGUI; do not add a package or generic quest framework.
- Keep `PlayerMovement2D.cs` unchanged; do not edit `SampleScene`, Build Settings, or unrelated `ProjectSettings`.
- Preserve existing `.meta` GUIDs and user changes; all work stays in `feature/m4a-supply-shortage-task`.
- The user’s Editor is open on M3 integration. Before any Unity test, build, or scene integration, verify its loaded project path. Do not launch a second Editor or batchmode. Stop Unity-dependent verification until the user opens this exact M4a worktree.
- Generated scene UI must be beneath one fixed root; scene updates use one Undo group, validate before saving, roll back with Undo on exceptions, and never reload a scene to roll back.
- Require successful compilation, zero Console errors, passing EditMode tests, and a playable Kiyosu→Atsuta→Kiyosu mission before demo handoff. User acceptance is required before the final checkpoint commit, merge, or tag.
- No new assets or packages; use original text and existing content.

## Review Focus

1. Wrong stage or town must reject an action without changing mission state, merit, date, or location. Test every action with wrong stage and wrong location in `SupplyShortageMissionTests` and `GameSessionMissionTests`.
2. Repeated reporting must not award twice. Test the first report adds exactly 10 and all later reports leave merit unchanged.
3. Negotiation must require the existing Atsuta NPC Kuroda Iori after investigation. Test another/unknown character and pre-investigation negotiation are rejected.
4. `InitNewGame` must reset mission stage and merit. Test reset after both an intermediate stage and a claimed reward.
5. Missing/ambiguous scene objects or serialized references must fail validation without partial scene edits. Test updater preflight failures and rollback in `AddM4SupplyShortageMissionPanelTests`.

---

### Task 1: Mission State and GameSession Use Cases

**Files:**
- Create: `Assets/Scripts/Domain/SupplyShortageMission.cs` and `.meta`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Create: `Assets/Tests/EditMode/SupplyShortageMissionTests.cs` and `.meta`
- Create: `Assets/Tests/EditMode/GameSessionMissionTests.cs` and `.meta`

**Interfaces:**
- `SupplyShortageMission.Stage: SupplyShortageStage`
- `SupplyShortageMission.Merit: int`
- `SupplyShortageActionResult` exposes `Succeeded: bool`, `Stage: SupplyShortageStage`, `Merit: int`, and `Feedback: string`.
- `TryAccept(LocationId location)`, `TryInvestigate(LocationId location)`, `TryNegotiate(LocationId location, CharacterId merchantId)`, `TryReport(LocationId location)`: each returns `SupplyShortageActionResult`.
- `GameSession.SupplyShortageStage`, `GameSession.Merit`, `TryAcceptSupplyShortageMission()`, `TryInvestigateSupplyShortage()`, `TryNegotiateSupplyShortage(CharacterId merchantId)`, `TryReportSupplyShortage()` delegate using `CurrentLocation`.

- [x] Write `NewMission_StartsNotAcceptedWithZeroMerit`, `Accept_SucceedsOnlyInKiyosu`, `Investigate_RequiresAcceptanceInAtsuta`, `Negotiate_RequiresInvestigationAndKurodaInAtsuta`, `Report_RequiresNegotiationAndKiyosu`, and `Report_AwardsTenMeritExactlyOnce`; assert exact stage/merit.
- [x] Write parameterized `RejectedAction_PreservesMissionAndWorldState` cases for wrong location/stage, non-Kuroda/unknown merchant, and duplicate actions; assert stage, merit, date, and location remain unchanged.
- [x] Write `InitNewGame_ResetsMissionAndMerit` after intermediate and claimed states, and `MissionActions_DoNotAdvanceClockOrChangeLocation`.
- [ ] Run focused tests before implementation and confirm they fail (not performed; the first M4a Editor run was after source implementation).
- [x] Implement the mission-specific state machine and `GameSession` APIs; the report action transitions to `RewardClaimed` and awards 10 atomically.
- [x] Run the full EditMode suite; the focused mission and GameSession tests are included in that run, with exact results recorded.
- [x] After task review passes, commit this task as `feat(m4a): add supply shortage mission state`.

### Task 2: uGUI Mission Panel and Controller Wiring

**Files:**
- Create: `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and `.meta`
- Modify: `Assets/Scripts/Presentation/WorldMapController.cs`
- Create: `Assets/Tests/EditMode/SupplyShortageMissionPanelTests.cs` and `.meta`

**Interfaces:**
- `SupplyShortageMissionPanel.Bind(GameSession session)` binds the scene session.
- `SupplyShortageMissionPanel.Open()` / `Close()` control visibility; `Refresh()` renders stage, current objective, merit, action availability, and feedback.
- The panel calls only the four `GameSession` mission use cases from Task 1. `WorldMapController` opens it on entering a town and closes it on return to map.

- [x] Add `Panel_ShowsKiyosuAcceptBeforeAcceptance`, `Panel_ShowsAtsutaInvestigationThenNegotiation`, and `Panel_ShowsKiyosuReportOnlyAfterNegotiation`; assert objective, merit, and action visibility/interactability.
- [x] Add `Panel_RefreshesAcrossTownAndReturn`, `Panel_ShowsOperationFeedback`, `Panel_DisablesRepeatedReport`, and `Panel_HandlesMissingSessionOrReferences`; assert safe UI behavior and application-level rejection.
- [x] Implement the panel and controller binding; keep mission logic out of `MonoBehaviour` and keep relationship dialogue behavior unchanged.
- [x] Run the full EditMode suite in the M4a Editor; focused panel tests are included in that run.
- [x] After task review passes, commit this task as `feat(m4a): add mission progress panel`.

### Task 3: Safe FoundationScene Integration

**Files:**
- Create: `Assets/Scripts/Editor/AddM4SupplyShortageMissionPanel.cs` and `.meta`
- Create: `Assets/Tests/EditMode/AddM4SupplyShortageMissionPanelTests.cs` and `.meta`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scenes/FoundationScene.unity` only through the validated Editor updater.

**Interfaces:**
- Editor menu: `Tools/Sengoku Chronicle/Add M4a Supply Shortage Mission Panel`.
- Public test seam: `AddM4SupplyShortageMissionPanel.Update(Scene scene)` returning a typed result without saving; menu path additionally requires the active scene to be `Assets/Scenes/FoundationScene.unity` and clean.
- The validator checks a single mission panel under the fixed `M2WorldMapRoot/TownMenuRoot` hierarchy and all serialized references.

- [x] Write `Updater_RefusesWrongOrDirtyScene`, `Updater_RefusesMissingOrDuplicateRoots`, `Updater_BuildsAndBindsMissionPanelUnderTownRoot`, and `Updater_RollsBackOnValidationFailure`; assert no partial object remains on failure.
- [x] Implement preflight, one Undo group, UI construction/binding, structural validation, rollback, and save only after validation; never modify existing object GUIDs or unknown objects.
- [x] Update `FoundationSceneValidator` to report missing/duplicate mission UI and unbound references as errors.
- [x] Run updater and validator from the open M4a Editor; inspect the saved scene and report `SceneValidationReport.txt` result.
- [x] Commit the Editor updater, validator, and tests as `feat(m4a): add safe mission panel updater`; obtain the static task review before running the updater.
- [x] The lead alone runs the reviewed updater in the open M4a Editor and saves the resulting scene; commit that generated scene as `feat(m4a): integrate mission panel in foundation scene` after Unity validation passes (`6cba4ba`).

### Task 4: Demo Evidence, Review, and Handoff

**Files:**
- Create/update: `M4A_DEMO.md`, `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `ARCHITECTURE.md`, `DECISIONS.md`.

- [x] Update architecture and decisions with the fixed state machine, one-time +10 merit, no package/reuse decision, and M4a limitations.
- [x] Run full EditMode tests and check compilation separately: 214/214 passed at 2026-11-01 16:06:30Z after the final button-label change; latest project log records a successful Tundra build with no C# compiler diagnostics.
- [x] Complete same-task static QA reviews for Task 2 and Task 3; both approved. Live Console error counters remain unverified.
- [x] Update `M4A_DEMO.md` with exact test results, scene-validation results, Play Mode walkthrough, and unresolved limitations; do not claim acceptance.
- [x] Record user-reported `pass` after request to clear Console and play the full mission; exact counters and runtime values were not supplied.
- [x] User reported the consistent town-name-only button layout passed the visual re-check.
- [x] User explicitly accepted the demo; create the final checkpoint, tag `demo-M4a`, and fast-forward local `main` without pushing.



---

<a id="2026-11-01-m4b-two-simple-missions"></a>
## 来源：docs/superpowers/plans/2026-11-01-m4b-two-simple-missions.md

# M4b Two Simple Missions Implementation Plan
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two fixed Phase 1 mission loops, show all three missions in the town UI, and award each M4b mission +5 merit exactly once.

**Architecture:** Add one plain-C# state model per mission and named `GameSession` actions that validate location and order. Adapt presentation and the transactional FoundationScene updater to display a fixed three-entry mission board while preserving M4a behavior.

**Tech Stack:** Unity 6000.6.6f1, C#, existing Domain/Application/Presentation/Editor assemblies, built-in uGUI, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-01-m4b-two-simple-missions-design.md`

## Global Constraints

- Use only existing NPCs and the three existing locations; do not add a package, asset, scene, or generic mission framework.
- Keep mission rules in plain C# and retain M4a's +10 merit behavior.
- Mission actions do not advance date or change location; map travel remains unchanged.
- Keep `PlayerMovement2D.cs`, `SampleScene`, Build Settings, and unrelated ProjectSettings unchanged.
- Preserve `.meta` files/GUIDs and all pre-existing user work; do not modify Library or Temp.
- Use the already-open Editor for Unity compile, tests, and scene integration; never launch a second Editor or batchmode.
- Do not checkpoint, merge, or tag before explicit user Play Mode acceptance.

## Review Focus

- Mission actions submitted with a wrong location or stale stage must fail without partial changes.
- Repeat final actions must not award merit more than once.
- Completing missions in either order must preserve each independent state and aggregate merit.
- Missing/duplicate board roots or unbound controls must make the scene updater refuse save and roll back.
- Location changes must refresh action availability without mutating mission state or advancing time.

## Planned files

- Create `Assets/Scripts/Domain/SealedLetterMission.cs` and `Assets/Scripts/Domain/MarketTallyMission.cs`: fixed stages, validation, once-only +5 award.
- Modify `Assets/Scripts/Application/GameSession.cs`: own/reset both models; expose named actions and aggregate `Merit`.
- Modify `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and its tests: retain the M4a default page and add a toggle to the two-row M4b page while preserving M4a actions and references.
- Modify the existing Editor updater and `FoundationSceneValidator`: transactionally add the two mission rows and page toggle to the existing fixed panel root; validate it does not cover neighboring town controls.
- Modify only `Assets/Scenes/FoundationScene.unity` through the validated updater; preserve all existing object identities and references.
- Update `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `DECISIONS.md`, and add `M4B_DEMO.md` with actual evidence after verification.

## Task 1: Mission models and GameSession actions

**Files:** create the two Domain models and corresponding EditMode tests; modify `GameSession.cs` and `GameSessionMissionTests.cs`.

**Interfaces:**
- `SealedLetterMission.Stage` has `NotAccepted`, `Accepted`, `RewardClaimed`; named methods accept current `LocationId` and enforce Kiyosu acceptance, Nagoya delivery, +5 once.
- `MarketTallyMission.Stage` has `NotAccepted`, `Accepted`, `Investigated`, `RewardClaimed`; named methods enforce Nagoya acceptance, Kiyosu investigation, Nagoya report, +5 once.
- `GameSession` exposes mission stage properties and named action methods; `Merit` sums M4a and both M4b model rewards.

- [ ] Write tests for each valid route, all wrong-location/wrong-stage rejections, unchanged state on rejection, and duplicate final action.
- [ ] Run the focused EditMode tests in the open Editor and confirm the new tests fail for the missing models/actions.
- [x] Implement the two fixed models and named `GameSession` actions; reset both in `InitNewGame`.
- [x] Add tests proving either completion order produces a maximum aggregate `Merit` of 20 including M4a, and mission actions preserve date/location.
- [ ] Run focused mission/session tests; confirm all new and M4a mission tests pass.

## Task 2: Fixed three-entry presentation

**Files:** modify `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and `Assets/Tests/EditMode/SupplyShortageMissionPanelTests.cs`; modify only necessary town-menu binding code.

**Interfaces:** extend the existing panel's serialized bindings with two status texts, two objective texts, two feedback texts, two contextual action buttons, and one page toggle. The board shows M4a by default and both M4b rows on the alternate page; it invokes named `GameSession` methods and refreshes after location changes. Preserve the existing M4a binding overload and action/text results.

- [x] Add tests for per-location action availability, objective/status updates after each action, board hiding/return behavior, missing references, and refresh after travel.
- [ ] Run focused presentation tests and confirm failures expose the missing board behavior.
- [x] Implement the fixed board using existing uGUI conventions; keep domain rules outside MonoBehaviours.
- [ ] Run presentation tests and all existing M4a panel tests.

## Task 3: Transactional scene integration

**Files:** create/modify the M4b Editor updater and updater tests; modify `FoundationSceneValidator.cs`; update `FoundationScene.unity` only via the open Editor.

**Interfaces:** one menu command adds one fixed board below `M2WorldMapRoot/TownMenuRoot`; the test seam validates a `Scene` without saving; menu execution saves only the clean, active `FoundationScene` after all references validate.

- [x] Add updater tests for wrong/dirty scenes, missing/duplicate roots, correct hierarchy/bindings, and rollback after validation failure.
- [x] Run updater tests and verify refusal paths leave no partial objects.
- [x] Implement one-Undo-group construction, binding, validation, rollback, and save-after-validation.
- [x] Update the scene validator to require exactly one board and verify all serialized controls/references.
- [x] Run updater and validator through the already-open Editor; record the fresh validation report.

## Task 4: Full verification and demo handoff

- [x] Run the full EditMode suite from the open Editor and record totals and timestamp.
- [x] Inspect the current Editor log and report compilation independently from test results.
- [x] Verify the current FoundationScene validation report independently.
- [ ] Record current Editor Console error and warning counters independently.
- [x] Play both mission loops in the existing Editor; user reported the requested walkthrough passed; exact runtime values were not independently captured.
- [ ] Record exact Editor Console counters and runtime merit/date values independently.
- [x] Update `M4B_DEMO.md`, `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, and `DECISIONS.md` with observed results only.
- [ ] Complete the Kanban same-task QATester workflow; Kanban tools are unavailable in this session. Independent technical QA review accepted with no blocking defects.
- [x] Wait for explicit user Play Mode acceptance before creating the final checkpoint commit, merge, or tag; the user accepted M4b on 2026-11-01.

Integration complete: checkpoint `70ac62f0d8a9866488ae194c3cf55c68a8be2e29` is tagged `demo-M4b` and fast-forwarded into local `main`. No remote push was made. The exact Console counters and runtime merit/date values remain unrecorded.


---

<a id="2026-11-01-m5-single-slot-save"></a>
## 来源：docs/superpowers/plans/2026-11-01-m5-single-slot-save.md

# M5 Single-Slot Save Implementation Plan
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a versioned single-slot save/load loop that restores date, location, mission stages, derived merit, and relationships without duplicating rewards.

**Architecture:** Add a small `SengokuChronicle.Persistence` assembly with a flat `SaveData` DTO, a validator, and an atomic file store. `GameSession` captures and atomically applies the DTO. A compact uGUI control on the map screen exposes save/load, and the existing transactional scene-updater pattern integrates it.

**Tech Stack:** Unity 6000.6.6f1, C#, existing Domain/Application/Presentation/Editor assemblies, built-in `JsonUtility` and uGUI, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-01-m5-single-slot-save-design.md`

## Global Constraints

- One slot only; `saveFormatVersion` is mandatory and currently 1.
- Store only plain data and stable IDs; never serialize Unity objects or ScriptableObject references.
- Validate before writing and before applying; a failed save/load must leave the previous slot and live session unchanged.
- Do not store merit; derive it from mission stages.
- No new package, asset, scene, or generic save framework.
- Keep mission, travel, dialogue, and `PlayerMovement2D` behavior unchanged.
- Preserve `.meta` files/GUIDs and all pre-existing user work; do not modify Library or Temp.
- Use the already-open Editor for Unity compile, tests, and scene integration; never launch a second Editor or batchmode against a folder it holds.
- Do not checkpoint, merge, or tag before explicit user Play Mode acceptance.

## Review Focus

- Version, date, location, stage, and relationship validation must reject bad data with a specific error and no partial mutation.
- Save must be atomic: a failed replace cannot truncate or delete the existing slot.
- Loading must clear transient dialogue state and must not re-award any completed mission.
- Load must refresh every visible surface (date, location, mission board) and close stale NPC/dialogue UI.
- The Save/Load control must not overlap existing controls, and the updater must roll back on any validation failure.

## Planned files

- Create `Assets/Scripts/Persistence/`: assembly definition, `SaveData`, `SaveDataValidator`, `SaveSlotStore`, and result types.
- Modify `Assets/Scripts/Domain/{SupplyShortageMission,SealedLetterMission,MarketTallyMission}.cs`: validated restore factories that derive merit from stage.
- Modify `Assets/Scripts/Application/GameSession.cs`: `CaptureSave`, `TryApplySave`, `SaveTo`, `LoadFrom`.
- Create `Assets/Scripts/Presentation/SaveLoadPanel.cs`: save/load buttons, status text, refresh callback.
- Create `Assets/Scripts/Editor/AddM5SaveLoadPanel.cs` and extend `FoundationSceneValidator.cs`: transactional scene integration.
- Modify `Assets/Scenes/FoundationScene.unity` only through the validated updater.
- Create EditMode tests for the validator, store, session round trip, and panel/updater.
- Update `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `DECISIONS.md`, `ARCHITECTURE.md`, and add `M5_DEMO.md` with observed evidence only.

## Task 1: Persistence DTO, validator, and atomic store

**Files:** create `Assets/Scripts/Persistence/*` and `Assets/Tests/EditMode/SaveDataValidatorTests.cs`, `SaveSlotStoreTests.cs`; update `SengokuChronicle.Application.asmdef`, `SengokuChronicle.Presentation.asmdef`, and the EditMode test asmdef.

**Interfaces:**
- `SaveData` with `saveFormatVersion`, date fields, `locationId`, three stage ints, and `RelationshipSaveEntry[]`.
- `SaveDataValidator.Validate(SaveData)` returning `SaveOperationResult` with `SaveErrorCode`.
- `SaveSlotStore(string directory)` with `Save`, `Load`, `Exists`, `FilePath`, plus `CreateDefault()`.
- `SaveSlotStore.Save` writes a temp file and replaces the slot; `Load` distinguishes not-found, read failure, invalid JSON, unsupported version, and invalid data.

- [ ] Write validator tests for valid data, bad versions, invalid dates, unknown locations, undefined stages, unknown/player/duplicate relationships, and out-of-range values.
- [ ] Write store tests for round trip, missing file, corrupt JSON, unsupported version, invalid-data refusal, and overwrite.
- [ ] Implement the Persistence assembly and code; keep it free of scene/UI dependencies.
- [ ] Run the focused EditMode tests in the open Editor and confirm they pass.

## Task 2: Session snapshot and restore

**Files:** modify the three mission models and `GameSession.cs`; create `Assets/Tests/EditMode/GameSessionSaveTests.cs`.

**Interfaces:**
- `SupplyShortageMission.TryRestore(stage)`, `SealedLetterMission.TryRestore(stage)`, `MarketTallyMission.TryRestore(stage)` return a canonical model with merit derived from stage.
- `GameSession.CaptureSave()` returns the current state as `SaveData`.
- `GameSession.TryApplySave(data)` validates, builds replacement state locally, then swaps it in and clears dialogue state.
- `GameSession.SaveTo(store)` and `LoadFrom(store)` delegate to the store.

- [ ] Write tests for capture fidelity, full restore, rejected load leaving state unchanged, no reward duplication after load, dialogue reset, and store round trip.
- [ ] Implement restore factories and session mapping.
- [ ] Run focused tests plus all existing M4a/M4b session tests.

## Task 3: Save/Load UI and transactional scene integration

**Files:** create `Assets/Scripts/Presentation/SaveLoadPanel.cs`, `Assets/Scripts/Editor/AddM5SaveLoadPanel.cs`, and tests; modify `FoundationSceneValidator.cs` and `WorldMapController.cs` only where needed.

**Interfaces:**
- The panel binds a `GameSession`, a `SaveSlotStore`, two buttons, a status text, and a refresh callback.
- Save calls `CaptureSave`/store save and reports success or the exact error.
- Load calls store load + `TryApplySave`, then refreshes the map and closes NPC/dialogue UI.
- The updater adds exactly one fixed root, validates references and separation from existing controls, uses one Undo group, and rolls back on failure.

- [ ] Write panel tests for success, failure messaging, refresh, and missing dependencies.
- [ ] Write updater tests for wrong scene, duplicate root, missing references, and rollback.
- [ ] Implement the panel, updater, validator extension, and scene integration through the open Editor.
- [ ] Run updater/validator tests and record the fresh validation report.

## Task 4: Full verification and demo handoff

- [ ] Run the full EditMode suite from the open Editor and record totals and timestamp.
- [ ] Record compilation, EditMode, Console counters, and Play Mode evidence separately.
- [ ] Play the save/reload loop in the existing Editor and record actual values.
- [ ] Verify a corrupt and an unsupported-version save both fail without overwriting the slot or changing live state.
- [ ] Update `M5_DEMO.md`, `PROJECT_STATE.md`, `PHASE1_ROADMAP.md`, `DECISIONS.md`, and `ARCHITECTURE.md` with observed results only.
- [ ] Wait for explicit user Play Mode acceptance before the final checkpoint commit, merge, or tag.


---

<a id="2026-11-01-m6-title-demo-packaging"></a>
## 来源：docs/superpowers/plans/2026-11-01-m6-title-demo-packaging.md

# M6 实施计划：标题界面与演示封装
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


设计：`docs/superpowers/specs/2026-11-01-m6-title-demo-packaging-design.md`
分支：`feature/m5-save-slot`（用户指定沿用同一分支与工作树）

## 任务

1. **纯逻辑先行**
   - `GameSession.IsDemoComplete` 属性（三任务均 RewardClaimed）。
   - 测试：完成前 false、完成 true（经 `TryApplySave` 构造完成态）。
2. **Presentation 脚本**
   - `TitleScreenController.cs`：Bind/SetStore 注入口、Start 显示标题、RefreshTitleState（缺档/坏档/有效三态）、StartNewGame、TryContinue、幂等按钮监听（OnEnable/OnDisable）。
   - `DemoSummaryPanel.cs`：幂等 `Refresh()` + Update 轮询；文案「Phase 1 演示完成。总功绩：N。」
   - `TitleScreenControllerTests.cs`、`DemoSummaryPanelTests.cs`。
3. **Editor 更新器**
   - `AddM6TitleScreen.cs`：M5 同构事务式更新器；创建 `Canvas/M6TitleRoot`（全屏不透明 Image，默认隐藏，最后兄弟）与 `map/M6DemoSummaryText`（TownMenuRoot 之前，默认隐藏，位于地图矩形内且不压 M5 控件）；在 GameSession 对象挂两个新组件并绑定；菜单 `Tools/Sengoku Chronicle/Add M6 Title Screen`；`RunBatchIntegration()`；故障注入缝。
   - `FoundationSceneValidator.cs`：新增 M6 FOUND/绑定检查并调用 `AddM6TitleScreen.Validate`。
   - `AddM6TitleScreenTests.cs`（preview-scene 结构、绑定、幂等、回滚）。
4. **Unity 验证**（受 Editor 占用约束）
   - 用户 Editor 正打开本工作树：不得 batchmode。二选一：
     a) 用户关闭 Editor → 我跑 batchmode：编译、EditMode 全量、`RunBatchIntegration`、Validator 报告；用户重开做 Play Mode 验收。
     b) 用户保持 Editor：由用户在 Test Runner 执行 Run All 并运行菜单更新器 + Validator，回报结果。
   - 记录：编译诊断、测试总数、场景 diff、Validator 报告分别留档。
5. **文档与收尾**
   - `M6_DEMO.md`（验收路径+证据）、`PROJECT_STATE.md`、`PHASE1_ROADMAP.md`、`ARCHITECTURE.md` 更新。
   - 同任务 QA 复审（QATester 流程按 AGENTS；kanban 工具不可用时由 lead 记录复审结论）。
   - 用户 Play Mode 接受后：最终 checkpoint → 合并 `main` → tag `demo-M6` → 推送 GitHub（AGENTS 新规：checkpoint 后立即推送）。

## 约束提醒

- 单写者：仅 lead/集成工作者改场景；本计划内由集成流程执行。
- `.meta`/GUID 保留；`Library`/`Temp` 不动；不装包。
- 不宣称未真实执行的编译/测试/Play Mode。


---

<a id="2026-11-01-m7-merit-promotion"></a>
## 来源：docs/superpowers/plans/2026-11-01-m7-merit-promotion.md

# M7 实施计划：功名晋升
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


设计：`docs/superpowers/specs/2026-11-01-m7-merit-promotion-design.md`
分支：`feature/m5-save-slot`（用户指定沿用同一分支与工作树）

## 任务

1. **Domain（纯 C#）**
   - `PromotionRules`：四军阶阈值表（足轻0/番头5/组头10/武士头20）、`RankFor`、`ChineseName`、`MinimumMerit`、`TryNextRank`。
   - `GameSession.Rank`/`RankName` 派生属性；存档格式不变。
   - 测试：`PromotionRulesTests`（边界 0/4/5/9/10/19/20/30、负数、名称、NextRank 链）。
2. **Presentation**
   - `RankPanel`：`Bind` + 幂等 `Refresh` + Update 轮询；空会话保护；文本含下一阶或（最高）。
   - `DemoSummaryPanel`：完成文案加入军阶名。
   - 测试：`RankPanelTests`；扩展 `DemoSummaryPanelTests` 断言军阶文案。
3. **Editor**
   - `AddM7RankPanel` 事务式更新器 + 菜单 + `RunBatchIntegration` + 故障注入缝；`M7RankText` 位于 map、TownMenuRoot 之前、(-450,100) 240×40；重叠检查覆盖 TitleText/LocationMarkerText/M5 控件/Confirm/Cancel/Feedback。
   - `FoundationSceneValidator`：FOUND RankPanel + 绑定 + `AddM7RankPanel.Validate`。
   - 测试：`AddM7RankPanelTests`。
4. **Unity 验证**
   - 用户 Editor 若仍打开本工作树：先请用户关闭 → batchmode 编译 + EditMode 全量 + `RunBatchIntegration` + Validator 报告 → 记录证据 → 提交并推送（AGENTS：checkpoint 后立即推送）。
5. **文档**
   - `M7_DEMO.md`（范围+证据+验收路径）；`PROJECT_STATE.md`、`PHASE1_ROADMAP.md`（新建 Phase 2 段落）、`ARCHITECTURE.md`、`DECISIONS.md`（军阶为游戏化简化设定的决定）。
6. **QA 与收尾**
   - 同任务复审（kanban 工具不可用时由 lead 记录静态复审结论）。
   - 用户 Play Mode 接受后：最终 checkpoint → 合并 main → tag `demo-M7` → 推送 GitHub。

## 约束提醒

- 单写者；事务式场景更新器；保留 .meta/GUID；不装包；不宣称未执行的验证。


---

<a id="2026-11-01-sc-001-baseline-audit"></a>
## 来源：docs/superpowers/plans/2026-11-01-sc-001-baseline-audit.md

# SC-001 现有项目基线审计计划

> **执行说明：** 本任务只做规划与只读盘点。后续编程由 OpenCode 负责；本次审计不得修改代码。

**目标：** 确立现有 Unity 项目和仓库为《影之一族》的唯一技术基线，并形成有证据的 M0 复用与缺口报告。

**方法：** 在现有项目内盘点，不新建或迁移 Unity 项目。将现有程序集、领域模型、软件包、场景、测试和 Git 状态与《影之一族》计划逐项对照，在开始实现前列明可复用部分和迁移风险。

**技术栈：** 现有 Unity `6000.6.6f1`、C#、URP `17.6.0`、uGUI、Unity Test Framework `1.8.0`。

**设计依据：** `GDD_v4.0.md` 第 11、12、17 章，以及用户当前提供的 AGENTS.md 中“只使用一个项目”的要求。

## 全局约束

- Unity 项目仅使用 `C:\Users\panye\My project`；不得另建 Unity 项目或同时打开第二个 Editor。
- 保留所有现有分支、提交、本地修改和未跟踪文件；审计期间不得切换分支、暂存或提交。
- 不得编辑 C#、场景、Prefab、`ProjectSettings`、软件包、`.meta` 文件、`Library` 或 `Temp`。
- 执行任何 Unity 验证前，先确认 Editor 实际加载的 `-projectPath`；不得对 Editor 已打开的项目启动 batchmode 或第二个 Editor。
- 编译、测试、Console 或 Play Mode 结果只能依据正确 Editor 中的实际证据报告。
- OpenCode 负责后续实现；本计划只授权只读盘点和报告。

## 审查重点

- 项目身份：此前观察到 PID `56820` 的进程路径位于 `Unity Hub\\resources`，但没有可解析的 `-projectPath`；任何 Unity 验证前都要确认实际 Editor 与加载项目。
- Git 身份与 WIP：指定的 `main` 落后于 `origin/main`，并有用户修改及未跟踪文件；保留这些文件，不要把独立 `feature/m5-save-slot` 工作树的内容误认为指定项目内容。
- 引擎与软件包兼容性：现有项目使用 Unity `6000.6.6f1` 和 URP `17.6.0`；v4.0 计划提及 Unity 6.3 LTS。记录版本差异，SC-001 期间不升级项目。
- 现有存档和领域系统：只检查指定项目中是否有存档 DTO、家族/人物/关系模型、按月推进、固定种子随机数、事件 Schema/引擎和编年史；不得根据其他工作树推断。
- 场景与测试覆盖：区分磁盘上的 `FoundationScene` 与 `EditorBuildSettings` 中启用的场景；区分 EditMode 源码覆盖与实际运行或人工 Play Mode 证据。

## 接口与审计交付

- **基线报告：** `docs/SC001_BASELINE_AUDIT.md`，记录项目路径、分支、提交、WIP、Editor 身份、Unity 与软件包版本、asmdef、相关脚本、场景/构建设置、测试及证据来源。
- **复用与缺口表：** 将每项 M0 能力（家族/人物 ID、关系、按月时钟、固定种子随机数、版本化 JSON 存档、事件 Schema/引擎、文字事件 UI、继承、跨代事件、编年史）标记为“复用”“调整”“缺失”或“待决策”，并附具体路径与证据。
- **后续交接：** 建议下一份由 OpenCode 实施并单独审查的计划。SC-001 本身不授权修改代码或场景。

## 任务

### 任务 1：记录 Git 与工作区状态

- [x] 在 `C:\Users\panye\My project` 记录 `git status --short --branch`、当前提交、远端跟踪状态，以及与规划工作树的差异。
- [x] 只按路径列出已有本地修改和未跟踪文件；不暂存、不清理、不 stash、不 reset、不切换分支。
- [x] 记录 `ProjectVersion.txt` 与现有里程碑历史，不把历史验收当作新方向的实现进度。

### 任务 2：确认 Unity Editor 所属项目

- [ ] 检查运行中的 Unity 进程，确认真实 Editor 可执行文件及 `-projectPath`。PID `56820` 未提供可解析的项目路径，当前加载项目仍未知。
- [x] 将 Editor 验证标记为等待确认项目身份；本次没有启动 Editor 或 batchmode。

### 任务 3：盘点项目结构与软件包

- [x] 记录 `manifest.json` 和 `packages-lock.json` 中 URP、Input System、TextMeshPro、JSON 与测试框架相关版本。
- [x] 列出运行时、Editor、测试 asmdef，以及 Domain、Application、Presentation、Persistence 目录和脚本。
- [x] 列出项目场景，并与 `EditorBuildSettings` 中启用的场景对照。
- [x] 按 EditMode/PlayMode 程序集及范围盘点测试；本次不运行测试。

### 任务 4：对照 M0 盘点现有代码

- [x] 在指定项目内检索稳定 ID、家族/人物/关系类型、按月推进、确定性随机数、存档 DTO/版本、事件定义/Schema/引擎、继承和编年史。
- [x] 为每项能力记录证据路径，并将源码证据与测试、运行证据分开。
- [x] 标出可能复用的 Presentation/更新器模式，以及不应未经审查带入新项目的旧方向假设；详见 `docs/SC001_BASELINE_AUDIT.md`。

### 任务 5：交付审计与交接

- [x] 在 `docs/SC001_BASELINE_AUDIT.md` 编写基线报告和复用/缺口表。
- [x] 为 OpenCode 补充后续 SC-002 起的建议，遵循 GDD v4.0 顺序和单项目约束。
- [x] 检查 Markdown 链接和 `git diff --check`；未修改 Unity 代码、场景、软件包或项目设置。

## 验收标准

- 报告识别指定项目、Git/WIP 状态，并说明能否证实 Unity Editor 正加载该路径。
- 每项 M0 能力都有对应路径和复用/缺口状态；未知项明确标为未知，不猜测。
- 不以静态检查声称运行时或测试结果。
- 审计只修改文档；不修改代码、场景、软件包或项目设置。


---

<a id="2026-11-01-sc-002-pure-csharp-core"></a>
## 来源：docs/superpowers/plans/2026-11-01-sc-002-pure-csharp-core.md

# SC-002：纯 C# 核心程序集计划

> **执行说明：** 本文是交给 OpenCode 的实施计划。规划阶段不改游戏代码；实施阶段由 OpenCode 在唯一指定项目中完成，并遵守 `AGENTS.md` 的项目与 Unity 约束。

**目标：** 在现有唯一 Unity 项目中确认并巩固纯 C# 领域核心边界，使核心规则不依赖 Unity 场景、`GameObject`、`MonoBehaviour` 或 `UnityEngine`。

**设计依据：** `GDD_v4.0.md` 第 11、13、17、19 章；SC-001 基线审计报告；用户指定的单项目要求。

**技术基线：** Unity `6000.6.6f1`、C#、现有 asmdef 与 Unity Test Framework。SC-002 不升级 Unity，不新增包，不另建 Unity 项目。

## 决策与现状

指定项目 `C:\Users\panye\My project` 已存在 `Assets/Scripts/Domain/SengokuChronicle.Domain.asmdef`。其 `references` 数组为空，12 个领域源码文件没有 Unity API 使用。此前 asmdef 中的 `useUnityAssemblies: false` 不是 Unity 有效字段，生成的 Domain 项目仍含 Unity 引擎引用；OpenCode 已将其替换为有效的 `noEngineReferences: true`。该修正尚待当前 Editor 导入、重新编译及 EditMode 复验。`Assets/Scripts/Application/SengokuChronicle.Application.asmdef` 依赖 Domain，但 `GameSession.cs` 当前继承 `MonoBehaviour`，应继续留在 Unity 适配层，不纳入本次纯 C# 核心。

**计划决策：** 复用并验证现有 `SengokuChronicle.Domain` 作为 SC-002 核心程序集；除非实现前检查发现它无法满足 GDD 的纯 C# 边界，否则不要另建平行的 `Core` asmdef，也不要为改名而搬迁现有领域代码。

## 前置门槛

OpenCode 开始修改前必须逐项确认（本轮已核对）：

- 目标 Unity 项目仍是 `C:\Users\panye\My project`；不得创建或打开第二个 Unity 项目。
- 当前 Unity Editor 已由用户打开；进程 PID `56392` 的 `-projectpath` 指向指定项目，窗口标题显示 `FoundationScene`。不得启动第二个 Editor 或对已打开项目运行 batchmode。
- `C:\Users\panye\My project` 的实现分支为 `feature/sc002-domain-boundary`，基于包含 GDD v4.0 的 `b5c67c3`；其上游 `origin/main` 为已接受 M7 的 `f413a8f`。既有 WIP（`Assets/Scenes/SampleScene.unity`、`Assets/1.unity`、`Assets/1.unity.meta`、`ProjectSettings/SceneTemplateSettings.json`、`nul`）均保留；分支切换前后 `SampleScene.unity` SHA-256 一致。
- `GDD_v4.0.md` 与本计划可在交接工作树中读取，且 GDD 已包含在目标实现分支；Unity 项目路径始终为唯一指定的 `C:\Users\panye\My project`。

以上门槛已满足后允许实施本计划限定的 Domain asmdef 修改；本次不得修改场景、Prefab、包或 `ProjectSettings`。

## 范围

包括：核心 asmdef 边界、依赖方向、核心命名空间/接口约束、静态依赖检查与 Unity 验证计划。

不包括：人物/家族模型（SC-003）、月度时间（SC-004）、随机数（SC-005）、存档（SC-006）、事件 Schema/引擎（SC-007/008）、UI、场景、Prefab、Unity 包或 `ProjectSettings` 改动。

## 任务 1：实现前核对和边界确认

- [x] 检查 `SengokuChronicle.Domain.asmdef`、Domain 全部 C# 文件、Application 对 Domain 的引用及现有 EditMode 测试引用。
- [x] 搜索核心目录中的 `UnityEngine`、`UnityEditor`、`MonoBehaviour`、`ScriptableObject`、`GameObject`、`PlayerPrefs` 等 Unity API 使用；未发现 Domain 源码命中。
- [x] 对照目标检出与 M7 基线；现有 `GameDate`、`WorldClock`、`CharacterId`、`LocationId`、任务/关系类型仍在 Domain 中，未扩展未批准的家族系统。
- [x] 静态结果表明可复用现有 Domain，无需增加平行程序集；OpenCode 独立 QA 发现旧 asmdef 引擎引用开关无效，已交 OpenCode 做最小修正。

## 任务 2：巩固纯 C# 边界

**预期改动范围：** 优先限于 `Assets/Scripts/Domain/**`、必要的 asmdef 引用与对应 EditMode 测试。不得改场景、Prefab、包、`.meta`、`Library` 或 `Temp`。

- [x] 保持规则与模型为普通 C# 类型；静态检查确认 Domain 源码不引用 Unity API，也不接收 Unity 对象或 Unity 生命周期回调。
- [x] 静态检查未发现需要移入 Domain 的逻辑；`GameSession` 保留在 Application，不搬入 Domain。
- [x] 依赖方向保持 Presentation → Application → Domain；Domain 不反向依赖上层程序集。
- [x] 评估依赖边界测试；已有程序集配置与 EditMode 全量结果足以支持本次复用决策，暂不复制源码扫描或新增重复测试。
- [x] 不新增第二个 Core/Domain 程序集，不做全仓命名空间或目录重构。
- [x] 将无效的 `useUnityAssemblies` 字段替换为 `noEngineReferences: true`；文件 diff 已核实仅改 Domain asmdef 一行。等待 Unity 导入与编译证据确认该限制生效。

## 任务 3：验证与交接

- [x] 在确认身份的既有 Editor 中等待编译并运行测试；实际加载项目路径为 `C:\Users\panye\My project`。日志中未发现 C# 编译错误标记。
- [x] 用户在 Test Runner 运行全部 EditMode 测试：425 总数、425 通过、0 失败、0 跳过、0 不确定；结果文件见下方执行记录。
- [x] 记录修正后 Console 错误计数：用户在同一 Editor 报告红色错误数为 0。Editor.log 同时保留 Licensing 404 与 AI ModelSelector `NoSubscription` 服务日志；这与用户读取的 Console 计数分开记录。
- [x] 技术复审：OpenCode 独立只读 QA（Task `task_f1da208a9c75`）指出并验证旧 asmdef 字段无效；实现任务 `task_b3c0e82c7a58` 完成修正后，复核任务 `task_d39b0acf9744` 接受单行变更。Kanban/QATester 接口不可用，故未声称完成 Kanban 正式审查。
- [x] 在现有 Editor 导入后确认修正生效：生成的 `SengokuChronicle.Domain.csproj` 不再含 UnityEngine/UnityEditor 引用；新 `Logs/Editor.log` 中 `error CS` 为 0。
- [x] 用户在同一 Editor 重跑 EditMode Run All：2026-11-02 01:10:42Z–01:10:44Z，425/425 通过，0 失败、0 跳过、0 不确定。`TestResults.xml` 新时间为本地 15:10:44。
- [x] 更新 `PROJECT_STATE.md` 记录新方向、编译、测试和目前的 Console/QA 证据边界；新方向路线权威仍为 `GDD_v4.0.md`，旧 Phase 路线已标记历史。
- [x] SC-002 不包含可玩纵向切片；本任务不声称新方向已可玩。本次仅建立功能分支检查点并推送，不合并到 main、不打里程碑标签；后续可玩里程碑仍须用户试玩接受。

## 验收标准

- 指定项目内只有一个可用的纯 C# 核心边界，复用现有 Domain asmdef；不存在为命名而复制出的平行核心程序集。
- Domain 的程序集配置与代码不引用 Unity API；依赖方向保持向内。
- 指定 Editor 完成编译，EditMode 测试通过且 Console 错误为零；失败或未执行的检查明确标为未验证。
- 未修改场景、Prefab、包或 `ProjectSettings`；所有既有用户 WIP 保持原样。
- 独立技术 QA 接受 asmdef 修正；Kanban/QATester 接口不可用，未声称 Kanban 状态完成。只提交并推送功能分支检查点，不合并、打标签。

## 执行记录

- 修正前 EditMode 结果：2026-11-01 14:44:33，425/425。修正后 `TestResults.xml`：`C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\TestResults.xml`，2026-11-02 01:10:42Z–01:10:44Z，`Passed`，425/425。
- 修正后 Unity 日志：`C:\Users\panye\My project\Logs\Editor.log`，本地 2026-11-01 15:10:53；`error CS` 计数为 0。生成的 `SengokuChronicle.Domain.csproj` 更新于 15:10:20，已不含 UnityEngine/UnityEditor 引用。
- 静态代码检查与 OpenCode 独立复核接受复用 Domain asmdef 及 `noEngineReferences: true` 单行修改。用户在 2026-11-01 报告本次 Console 红色错误数为 0；Editor.log 仍记录 Licensing 404 与 AI `NoSubscription` 服务日志，两类证据分别记录。场景验证和 Play Mode 不属于本 SC-002 验证。


---

<a id="2026-11-02-sc-004-monthly-time"></a>
## 来源：docs/superpowers/plans/2026-11-02-sc-004-monthly-time.md

# SC-004 Monthly Time Implementation Plan

> 最终状态（2026-11-02）：SC-004 与 SC-005–SC-009 已集成；用户确认 Edit Mode 全部通过并接受 Play Test，最新 `TestResults.xml` 为 548/548 passed，OpenCode2 只读审查 ACCEPT。`c29d24a` 已推送到 `main`，`demo-SC004-SC009` 已推送。下面的未勾选项来自原始计划快照，不表示实现未完成。仍待取得的证据：FoundationScene 反馈条仍序列化为旧位置，本次未执行场景更新器或 `FoundationSceneValidator`，编译与 Console 计数也未单独核实。当前状态以 `PROJECT_STATE.md` 为准。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a bounded month-end clock action and a directly testable FoundationScene control.

**Architecture:** `WorldClock` owns the calendar transition; `GameSession` exposes it. The existing clock panel keeps day advancement and gains a separate month-end button that refreshes both displayed dates. A transactional Editor updater adds the control to FoundationScene.

**Tech Stack:** Unity 6000.6.6f1, pure C# Domain, existing uGUI, NUnit EditMode tests, Orca supervised OpenCode implementation and independent review.

**Spec:** `docs/superpowers/specs/2026-11-02-sc-004-monthly-time-design.md`

## Global Constraints

- Do not modify any pre-existing dirty or unrelated files; preserve all `.meta` GUIDs.
- Do not edit `Library`, `Temp`, packages, or `ProjectSettings`.
- Do not run batchmode or launch a second Unity Editor for this project.
- Keep Domain free of Unity APIs; retain `AdvanceOneDay`, `AdvanceDays`, and travel behavior.
- Scene changes must use one fixed generated root, one Undo group, validation before save, and Undo rollback on exceptions.
- No commit, push, merge, tag, or demo acceptance before user Play Mode acceptance.

## Review Focus

1. At a month boundary, next month begins on day 1; test Jan/Feb, leap February, and Dec/year rollover.
2. At the maximum supported year, month advance must fail without mutation; test final month and exact maximum date.
3. Existing day advancement and travel retain current semantics; test their regression paths.
4. Month button invocation updates both visible date labels and does not double-subscribe; test callback lifecycle and refresh.
5. Editor updater must be idempotent and roll back invalid/exceptional updates; test repeat invocation, validation failure, and save path.

---

### Task 1: Month transition and application API

**Files:**
- Modify: `Assets/Scripts/Domain/WorldClock.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs`

**Interfaces:**
- Produces `WorldClock.TryAdvanceToNextMonth()` and `GameSession.TryAdvanceToNextMonth()`, both returning `bool` and leaving state unchanged on failure.

- [x] Add failing tests for first-of-next-month, December/year rollover, leap-year February, and range rejection without mutation.
- [x] Implement the smallest transition preserving all existing day-based APIs.
- [ ] Run these tests in the existing Editor Test Runner; retain evidence separately from compile and Console evidence.

### Task 2: Clock UI control

**Files:**
- Modify: `Assets/Scripts/Presentation/WorldClockPanel.cs`
- Modify: `Assets/Scripts/Presentation/WorldMapController.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs` or an existing focused panel test file.

**Interfaces:**
- Adds an optional serialized month button and an explicit dependency setter; keeps the existing daily button.
- Exposes a narrow public date-refresh entry point from `WorldMapController` for clock changes.

- [x] Add failing tests for successful click, failed click at max date, refresh of both labels, and listener cleanup/re-enable.
- [x] Implement month-button binding and refresh without changing travel behavior.
- [ ] Run focused EditMode tests in the existing Editor.

### Task 3: FoundationScene updater and validation

**Files:**
- Create: `Assets/Scripts/Editor/AddSC004MonthTurnControl.cs` and `.meta` (Unity-generated)
- Modify: existing FoundationScene validator only if needed
- Test: `Assets/Tests/EditMode/AddSC004MonthTurnControlTests.cs` and Unity-generated `.meta`

- [x] Add failing tests for creation, idempotence, correct button binding/label, and invalid-scene rollback.
- [x] Implement a menu-driven transactional updater with one Undo group, one fixed root, validation before save, and rollback by Undo on exceptions without reloading the scene.
- [x] Root-caused the live-Editor console rollback («Clock panel endMonthButton is not bound to the month button»): the logged warning was emitted by `Updater_RollsBackWhenValidationFails`'s intentional fault injection (the accepted M7 updater logs the same class of warning in its twin test), no live menu/batch run of the SC-004 updater appears in any Editor log, and all eight updater tests passed in the 16:57 Editor run. The run's only genuine failure was the boundary sweep in `WorldClockTests` whose expected constants were computed from 2000−1560 = 40 years (488 month turns); the true span is 440 years = 5288 month turns, so the guard fired on still-legal clicks. Constants corrected; domain boundary behavior is correct and unchanged.
- [x] Hardened the flagged BindMonthTurn → SerializedObject path for the live scene's condition: FoundationScene's clock panel YAML predates `endMonthButton`/`worldMapController`, so a managed-only assignment can leave the serialized state that Validate reads and SaveScene writes unbound. The updater now re-applies both references through `SerializedObject.ApplyModifiedPropertiesWithoutUndo()` inside the same Undo group; validate-before-save, rollback, idempotence, and day/travel behavior are unchanged.
- [x] Added `AddSC004MonthTurnControlStaleSceneTests`: runs the never-save updater seam against an additive temporary copy of the real FoundationScene (the exact on-disk layout the live repro used), asserting SerializedObject-readable bindings, preserved day binding, idempotence, and that neither the temp copy nor FoundationScene.unity is written.
- [ ] Run it in the already-open Editor on FoundationScene; inspect the scene diff and validator output.

### Task 4: Independent review and Play Mode handoff

**Files:** no additional source files.

- [ ] Request independent read-only OpenCode review through Orca after implementation verification.
- [ ] Fix and re-review actionable findings on the same task.
- [ ] Compile and run EditMode tests in the already-open Editor; record Console counts separately if accessible.
- [ ] Enter Play Mode in FoundationScene and check the exact path in the spec; report evidence without screenshots.
- [ ] Freeze the worktree for the user's acceptance. Do not merge/tag before that acceptance.


---

<a id="2026-11-02-sc-005-009-event-demo"></a>
## 来源：docs/superpowers/plans/2026-11-02-sc-005-009-event-demo.md

# SC-005–SC-009 可玩事件演示实施计划

> 最终状态（2026-11-02）：SC-004 与 SC-005–SC-009 已集成；用户确认 Edit Mode 全部通过并接受 Play Test，最新 `TestResults.xml` 为 548/548 passed，OpenCode2 只读审查 ACCEPT。`c29d24a` 已推送到 `main`，`demo-SC004-SC009` 已推送。下面的未勾选项来自原始计划快照，不表示实现未完成。仍待取得的证据：FoundationScene 反馈条仍序列化为旧位置，本次未执行场景更新器或 `FoundationSceneValidator`，编译与 Console 计数也未单独核实。当前状态以 `PROJECT_STATE.md` 为准。

> **For agentic workers:** 依照项目 Orca 工作流，由一个 OpenCode 实施 worker 按顺序完成本计划，再由另一个 OpenCode worker 进行只读独立审查。两者不得并行写同一 worktree。

**Goal:** 在现有 Unity 6 项目中实现可复现随机数、版本化存档、JSON 事件内容、类型化安全事件引擎和可直接选择事件的 UI，并形成待用户 Play Test 的 FoundationScene 演示。

**Architecture:** 用纯 C# 随机流和事件运行时承载确定性规则；通过 `JsonUtility` DTO、聚焦验证器和 v1→v2 迁移保存会话状态；以现有 uGUI 风格展示等待玩家选择的队列首项。场景由一个 Undo 组内验证并可回滚的 Editor updater 生成，实施 worker 不直接修改 scene/prefab/ProjectSettings。

**Tech Stack:** Unity 6、C#、现有 `JsonUtility`、uGUI、NUnit EditMode/Editor 测试；不增加包。

**Spec:** `docs/superpowers/specs/2026-11-02-sc-005-009-event-demo-design.md`

## Global Constraints

- 只在 `feature/sc004-monthly-time` 当前 worktree 工作；保留所有原有 staged/unstaged/untracked 文件、Unity `.meta` GUID 和 SC-003/SC-004 WIP。
- 不能覆盖 `GameSession.cs` 中现有的 `TryAdvanceToNextMonth` 或改写整份 dirty 文件；对共享 dirty 文件做最小上下文修改，并在开始和结束报告其 diff。
- 不修改 `Library`、`Temp`、`ProjectSettings`、`.unity` 或 prefab 文件；仅新增场景 updater 源码和验证。
- 不安装包、不改 OpenCode provider、凭据或计划；不提交、合并、打标签或推送。
- 不启动 Unity Editor 或 batchmode；等用户醒来后使用其打开的项目 Editor，分开记录编译、EditMode、Console、SceneValidation、Play Mode 和用户接受结果；不使用截图。
- 所有随机候选、事件、选项和存档数组按稳定 ID 排序；事件内容不执行任意 C#。
- 月推进在存在待选事件时被拒绝；效果、一次性/冷却标记和历史作为一个逻辑提交，只允许应用一次。

## Review Focus

1. 空/零 seed、无效或全零随机状态及 `NextInt` 边界；测试须验证重复序列、不同流隔离和状态恢复。
2. v1、当前版本、未知版本、损坏 JSON 及无效随机/事件状态；测试须证明迁移保留旧字段且失败时原文件和活跃会话不变。
3. 重复 ID、未知引用、缺失 TextId、无效权重/枚举/冷却、未知 condition/effect；测试须证明目录在排程前拒绝且不执行未识别类型。
4. 队列已待选、条件在排程后失效、重复点击、重复结果 ID、效果越界；测试须证明失败不会部分改变状态或重复写历史。
5. 缺失 UI 引用、旧选项按钮回调、等待选择时重复结束月份、updater 预检失败；测试须证明旧状态可继续、无重复按钮且异常回滚不保存场景。

---

### Task 1: SC-005 稳定且互相隔离的随机流

**Files:**
- Create: `Assets/Scripts/Domain/DeterministicRandom.cs`
- Create: `Assets/Tests/EditMode/DeterministicRandomTests.cs`
- Test before implementation; preserve the repository's existing asmdef and `.meta` conventions.

**Interfaces:**
- `DeterministicRandomStream.Create(ulong initialSeed, RandomStreamKind kind)` creates a stream from a documented fixed integer algorithm and stable stream ID.
- `RandomStreamKind` defines `CharacterGrowth`, `Events`, `Diplomacy`, `Missions`, and `WorldSimulation`; the demo consumes `Events` while proving that other streams remain isolated.
- `NextUInt64()` and `NextInt(int exclusiveMax)` are deterministic; invalid bounds return a documented failure without consuming state.
- `CaptureState()` returns a plain serializable `RandomStreamSaveEntry`; restore validates the algorithm version and rejects malformed/all-zero state.
- `RandomStateManager` stores the original seed plus one state per stream and can capture/restore the complete ordered state.

- [ ] Write tests `SameSeedAndCallsProduceSameSequence`, `AdditionalCallsOnOtherStreamDoNotChangeEventSequence`, `CaptureRestoreContinuesExactSequence`, `InvalidBoundsDoNotConsumeState`, and `InvalidStateIsRejected`.
- [ ] Run those tests in the exact Unity project Editor and confirm the new sequence tests fail for the missing service.
- [ ] Implement a fixed bitwise algorithm with stable stream seed derivation; do not use `System.Random`, `GetHashCode`, time, or collection iteration order.
- [ ] Run the tests again; record actual Editor/Test Runner result or report the Editor as unavailable.

### Task 2: SC-006 v2 JSON save, migration and recovery backup

**Files:**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs`
- Modify: `Assets/Scripts/Persistence/SaveSlotStore.cs`
- Create: `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- Modify/Create tests: `Assets/Tests/EditMode/SaveDataValidatorTests.cs`, `Assets/Tests/EditMode/SaveSlotStoreTests.cs`, `Assets/Tests/EditMode/GameSessionSaveTests.cs`
- Modify narrowly: `Assets/Scripts/Application/GameSession.cs`; preserve its current dirty SC-004 and any SC-003 changes.

**Interfaces:**
- `SaveDataMigrator.TryMigrate(SaveData legacy, out SaveData current, out SaveOperationResult result)` supports the existing v1 format to current v2 and rejects unsupported future versions.
- v2 `SaveData` stores game/rules/event-content versions, initial random seed, complete random-stream entries, stable phase, ordered pending event IDs, cooldown/once state, event history, and committed result IDs in addition to every field currently captured by `GameSession`.
- `SaveSlotStore.Load()` returns migrated current-version data without changing the source file; `SaveSlotStore.Save()` validates the temp file before replacing the current slot and retains the previous valid slot as a recovery backup.
- `GameSession.CaptureSave()`/`TryApplySave(SaveData)` include and atomically restore the current session plus event/RNG runtime state; v1 missing fields receive a documented deterministic default.
- The demo's new-game path accepts an explicit seed; the sample scene uses a fixed documented seed so a fresh run is reproducible.

- [ ] Add tests `V1MigrationPreservesExistingSessionFields`, `UnknownVersionIsRejected`, `PendingEventAndRandomStateRoundTrip`, `InvalidTempDoesNotReplaceOriginal`, `PreviousSaveRemainsRecoverable`, and `RejectedLoadLeavesLiveSessionUnchanged`.
- [ ] Run focused persistence tests before the code changes; identify existing baseline failures separately.
- [ ] Implement migration and validation before session mutation; write/parse/validate temp content, then replace with a backup; clean temp files after failure.
- [ ] Run the focused persistence tests and the existing save tests; report results separately from Play Mode.

### Task 3: SC-007 event JSON schema, DTOs and content validation

**Files:**
- Create: `Assets/Scripts/Events/EventCatalogData.cs`
- Create: `Assets/Scripts/Events/EventCatalogValidator.cs`
- Create: `Assets/Scripts/Events/SengokuChronicle.Events.asmdef`
- Modify narrowly: existing Application, Presentation, Editor and EditMode test asmdefs only where compiler references require it; preserve every existing field and reference.
- Create: `Assets/Resources/Events/event-catalog-v1.json`
- Create: `Assets/Resources/Localization/event-text-zh-v1.json`
- Create tests: `Assets/Tests/EditMode/EventCatalogValidatorTests.cs`
- Create required Unity `.meta` files with unique GUIDs; do not regenerate or alter existing GUIDs.

**Interfaces:**
- Catalog DTOs cover stable `EventId`, trigger, conditions, weight, `TextId`, options, requirements, typed effects, cooldown, once scope, `ChainId` and priority.
- `EventCatalogValidator.Validate(EventCatalogData catalog, EventTextCatalog text)` rejects duplicate/empty IDs, unresolved text/chain/option references, invalid enum values, unsupported condition/effect types, negative cooldown, non-positive weight, empty option sets, and invalid effect bounds.
- Runtime event content is loaded as text JSON and parsed into DTOs; validation occurs before the engine may schedule it.
- Keep the Events assembly dependency direction acyclic; add only the references required by the existing assembly graph and the new application/UI/editor-test call sites.
- Provide one original, localized sample event with two choices and a supported NPC relationship-delta effect; do not copy commercial game text.

- [ ] Add tests `RejectsDuplicateEventAndOptionIds`, `RejectsMissingTextAndDanglingReferences`, `RejectsUnknownConditionOrEffect`, `RejectsInvalidWeightCooldownAndBounds`, `AcceptsSampleCatalog`, and `ContentVersionIsExplicit`.
- [ ] Run focused catalog tests to establish the missing validator failures.
- [ ] Add DTOs, validator and original JSON/text content; keep effect kinds as an explicit allowlist.
- [ ] Run focused catalog tests; verify sample event/text IDs resolve and no new package or external asset is introduced.

### Task 4: SC-008 deterministic event scheduling and atomic choice resolution

**Files:**
- Create: `Assets/Scripts/Events/EventRuntimeState.cs`
- Create: `Assets/Scripts/Events/EventEngine.cs`
- Create: `Assets/Tests/EditMode/EventEngineTests.cs`
- Modify narrowly: `Assets/Scripts/Application/GameSession.cs` and the SC-004 month-turn callback owner, preserving unrelated WIP.

**Interfaces:**
- Engine accepts validated catalog, current game date/state, runtime queue/markers/history and the event random stream; it returns explicit success/failure results and never touches Unity objects.
- Candidate events are filtered by trigger, conditions, cooldown and once scope, then sorted priority descending and ordinal EventId; weighted selection consumes only the event stream.
- Month-turn processing extends the existing `GameSession.TryAdvanceToNextMonth()` path without replacing the dirty SC-004 implementation, queues eligible events in stable order, emits a session-state-changed notification and pauses further month advancement while any choice is pending.
  - 队列边界：月度触发每次最多入队一个事件（GDD 7.2 的单次加权选择），待选期间拒绝再次结束月份；`EventRuntimeState` 的多事件有序存储与“只提交队首”语义保留给未来事件链/其它触发器，本次演示不声称多事件同时待选可达。
- `TryChooseEventOption(eventId, optionId, committedResultId)` revalidates the current head event and requirements, preflights all effects, then commits effect, history, markers and queue removal once.
- Supported effect set for this demo: bounded relationship delta against a known current NPC. Unknown future effects fail catalog validation.

- [ ] Add tests `PriorityAndIdOrderingIsStable`, `WeightsAreRepeatableForSameSeed`, `NoCandidatesDoNotConsumeEventStream`, `ExpiredConditionsSkipWithoutMutation`, `PendingEventBlocksAnotherMonth`, `InvalidAndRepeatedChoiceDoNotMutate`, `ChoiceEffectAndHistoryCommitOnce`, and `ReloadResumesSamePendingQueue`.
- [ ] Run focused event tests to record the failing behavior before implementation.
- [ ] Implement pure scheduling/resolution and the narrow `GameSession` adapter; add stable result IDs so retrying a choice cannot duplicate effects.
- [ ] Run focused event tests plus existing month, relationship and save tests; identify every test result separately.

### Task 5: SC-009 playable event choice UI and scene updater

**Files:**
- Create: `Assets/Scripts/Presentation/EventChoicePanel.cs`
- Create: `Assets/Scripts/Editor/AddSC005EventChoicePanel.cs`
- Create: `Assets/Tests/EditMode/EventChoicePanelTests.cs`
- Create: `Assets/Tests/EditMode/AddSC005EventChoicePanelTests.cs`
- Modify narrowly: `Assets/Scripts/Editor/FoundationSceneValidator.cs` to invoke the event panel validation; preserve the existing SC-004 validation.
- Do not modify: `Assets/Scenes/*.unity`, prefabs, `ProjectSettings`, or existing `.meta` files.

**Interfaces:**
- Panel receives explicit session/catalog/text references, renders one pending event plus localized options, shows disabled requirement reasons, and calls the application option API with stable IDs.
- Keep the component on an active generated controller root and hide/show its modal child so it can observe session state changes while the choice UI is closed; bind the live GameSession from the updater.
- Every generated runtime choice button is owned, listener-cleaned and destroyed on refresh/disable; stale callbacks and double-submit are rejected.
- Scene updater adds all controls beneath one fixed `EventChoiceRoot` in one Undo group, validates hierarchy/references/layout before saving, and rolls back with Undo on any exception; it refuses wrong/dirty/playing scenes and never reloads a scene for rollback.
- Existing End Month control remains usable; the GameSession change notification opens the event panel after event scheduling and after save/load, and after resolution shows the committed relationship/history feedback.

- [ ] Add tests for missing bindings, option render/update, stale/double callback, disabled requirement and teardown; add updater preflight/rollback/idempotence tests using the project's current Editor test conventions.
- [ ] Run focused UI/updater EditMode tests before implementation.
- [ ] Implement the component and transactional updater; do not execute it and do not change the FoundationScene file while the user's Editor is closed/asleep.
- [ ] Run focused UI/updater tests and text-only scene validator when the user's existing Editor is available; report scene validation independently from Play Mode.

### Task 6: Sequential integration and independent review handoff

**Files:**
- Modify only files required by Tasks 1–5.
- No commit or scene/prefab mutation by the implementation worker.

- [ ] Implementation worker checks `git status --short --branch` and records baseline dirty paths before edits; compare after each task and keep every pre-existing user diff intact.
- [ ] Complete Tasks 1 through 5 sequentially in the same Orca task; report file ownership, tests run, and unverified gates.
- [ ] Request a separate read-only OpenCode review on the completed diff only after the implementation worker is done; review checks GDD requirements, dirty-file preservation, determinism, save migration/recovery, event content safety, transactional effects, and UI/updater constraints.
- [ ] Correct only actionable findings through the implementation worker, then repeat the same read-only review.
- [ ] Leave the code worktree frozen for user Play Test; only after the user's acceptance may the lead create a checkpoint commit, and then push the branch and tag per project rules.

---

## Self-review

- GDD SC-005–SC-009 each have an owning task and evidence path; the playable loop tests a pending event across save/load, rather than treating compilation as runtime proof.
- SC-006 captures only state currently connected to `GameSession`; SC-003 family files are uncommitted and not integrated, so this plan expressly preserves them and avoids claiming their persistence is complete.
- Failure paths cover invalid data, migration, rollback, stale selection, repeated effect submission and updater preflight. No user scene data is touched before the user opens the Editor.
- Research recommends building focused code on existing Unity APIs; no candidate package was approved or added.
- The only external execution blocker currently evidenced is OpenCode's upstream `Insufficient account funds`; this plan is ready for sequential Orca/OpenCode dispatch when that worker can run.


---

<a id="2026-11-02-sc-010-succession"></a>
## 来源：docs/superpowers/plans/2026-11-02-sc-010-succession.md

# SC-010 Initial Head Retirement and Succession

## Goal

Produce a directly playable FoundationScene demo in which the player can retire the first family head, choose a lawful successor, transfer control without replacing permanent person/family identity, and save/load both the pending decision and completed handoff.

## Authority and scope

- `GDD_v4.0.md` is authoritative, especially chapters 3, 4, 11, and 19.2-19.4.
- Reuse the existing SC-003 value types and validator patterns. SC-010 cannot compile without them, so this accepted feature's checkpoint package includes `StableIds.cs`, `PersonState.cs`, `FamilyState.cs`, `KinshipGraph.cs`, `InterpersonalState.cs`, `FamilyRelationshipValidator.cs`, and their tests/metas. Do not rewrite or replace those files; preserve their current contents and GUIDs.
- Do not add packages, copy external code, change providers, or edit `Library`/`Temp`.
- Implementation owns C# scripts, data assets if needed, `.meta` files, EditMode tests, the SC-010 Editor updater/validator, and SC-010 demo instructions. It must not directly edit scenes, prefabs, or ProjectSettings. The lead will run the updater in the already open Editor after review.
- Preserve all pre-existing dirty docs and user WIP. Do not stage or commit.
- Do not run another Unity instance or batchmode. Use the existing project Editor for compilation and tests after the read-only review.

## User-visible slice

1. The FoundationScene shows the current family head and a `退休并安排继承` action.
2. Retirement enters a stable `AwaitingSuccessionChoice` state. The panel lists only living, qualified family successors and explains why an invalid designated heir needs replacement.
3. Confirming an eligible adult transfers the head role and player control exactly once. The same family/person IDs, campaign date, location, mission results, and personal relationship records remain intact.
4. If the chosen heir is a minor, the player must select an eligible adult regent before transfer. At the configured adulthood threshold the panel reports the handoff and transfers control without repeating the current month's action budget.
5. If no successor is eligible, provide one explicit crisis adoption/rebuild decision with visible condition/cost. Accepting creates a valid family member and proceeds; declining ends the family as a terminal gameplay state: no time or travel advance and no new succession action remain, a persistent modal explains the extinction, and the map HUD's save/load stays available so an earlier checkpoint can be loaded intentionally. No branch may wait or loop indefinitely.
6. Saving at a pending decision and after transfer restores the same phase, candidates/head, and one-time result markers. Existing v2 saves migrate explicitly; malformed succession data is rejected atomically.

## Proposed ownership

- New pure domain types: `Assets/Scripts/Domain/SuccessionService.cs`, `SuccessionState.cs`, and any narrowly required succession DTO/value types.
- Application/persistence integration: `Assets/Scripts/Application/GameSession.cs` and `Assets/Scripts/Persistence/SaveData.cs`, `SaveDataMigrator.cs`, `SaveDataValidator.cs`, plus required tests. Reuse stable pause/commit patterns in Events; only modify Event files if the state cannot be represented safely through the session API.
- Presentation: new `Assets/Scripts/Presentation/SuccessionPanel.cs` and a single-root Editor updater `Assets/Scripts/Editor/AddSC010SuccessionPanel.cs`; extend `FoundationSceneValidator.cs` only for the new bindings/layout checks.
- EditMode coverage in new `Assets/Tests/EditMode/SuccessionServiceTests.cs`, `GameSessionSuccessionTests.cs`, `SuccessionSaveTests.cs`, and `AddSC010SuccessionPanelTests.cs` as appropriate. Every new Unity asset gets its generated `.meta`; preserve existing GUIDs.
- Create a focused `SC010_DEMO.md` with entry instructions and expected behavior. Do not edit the already-dirty `DEMO.md` or other direction-refresh documents.

## Implementation sequence and acceptance

1. Add failing pure C# tests for lawful eligibility, invalid designated heir fallback, atomic/idempotent adult handoff, minor/regent handoff, no-candidate crisis accept/decline, and stable pending/completed states.
2. Implement a deterministic succession service over SC-003 identities/roster/kinship, with explicit result codes and no hidden random outcomes.
3. Integrate retirement and state-change notification into `GameSession` without resetting the existing campaign; add tests proving unrelated campaign and relationship state survives.
4. Extend versioned JSON save/load with a new schema version, migration from current v2, validation, and pending/completed succession round trips.
5. Build the SC-010 panel/updater with one Undo group, validation before save, rollback on failure, no scene reload, and a single fixed root. Add Editor tests for duplicate invocation, bindings, containment, and rollback.
6. Request a separate read-only OpenCode2 review after implementation. Resolve findings in the implementation task, then create the dependent lead sign-off task.
7. With the existing Unity Editor only, record compilation, EditMode results, Console error/warning counts, scene validation, Play Mode evidence, and save/load evidence separately. Do not claim user acceptance until the user completes the detailed text-only Play Test.

## Play Test finish line

The user can follow a numbered, text-only procedure with expected results at every step, including adult succession, invalid-heir fallback, minor/regent handling, no-candidate crisis resolution or terminal extinction, and save/load while a succession decision is pending. The test record must say which steps passed/failed and capture visible text/results without screenshots.

## Correction round after two read-only reviews

- Wire the five static HUD/modal buttons (retire, crisis demo, confirm, accept, decline) exactly once and unbind on disable.
- Place SuccessionRoot above M2WorldMapRoot but below the title and event-choice overlays; validate that order and the raycast targets that previously lost clicks.
- Honor a valid designated heir immediately, including minor heirs entering regent choice, and clear the previous reign's designation on every transfer.
- Route a minor heir with no eligible adult spouse/kin/retainer into the one-shot crisis decision instead of a deadlocked regent screen; restrict regent roles to GDD 4.3/19.2.
- Pause time advancement when a minor head reaches adulthood and require an explicit player handoff; expose the action through the confirm button and `MonthTurnOutcome.PendingSuccession`.
- Make crisis adoption/head transfer/allocation all-or-nothing, and reject saves whose current head is retired.
- Add regression tests for each correction plus crisis pending/accepted/extinct save/load, and update `SC010_DEMO.md` for the title-screen entry and actual controls.

## Residual correction round (accepted review follow-up)

Scope: only the three residual SC-010 findings; no scene/prefab/ProjectSettings edits and no
Unity execution. The lead still runs compilation, EditMode, scene validation, Console
inspection, and Play Mode in the already-open Editor.

- Day advance now reports the succession pause through the existing
  `MonthTurnOutcome.PendingSuccession` + `LastEventMessage` surfaces instead of returning
  `false` silently. Pending decisions and a due adult handoff produce different, actionable
  reasons. `AdvanceDays` uses the same reporting path.
- `WorldClockPanel.TryAdvanceOneDay` no longer logs "date limit" for a succession pause; it
  publishes `LastAdvanceMessage` from the session outcome. The SC-010 modal is drawn over
  the centered clock panel, so `推进一天`/`结束月份` are covered while the decision is
  open; `SC010_DEMO.md` Play Test C now states that and names `完成成年交权` as the
  required action.
- The regency HUD shows a truthful `受限决策` line naming the actions that are actually
  disabled (`退休并安排继承`, `示范：无继承人危机`) and keeps the explicit adult-handoff
  rule visible. The stale domain message that said the handoff happens "自动" now says it
  pauses and needs `完成成年交权`.
- Regression tests: `GameSessionSuccessionTests.AdvanceOneDay_WhileSuccessionChoicePending_ReportsPendingSuccessionWithoutAdvancing`,
  `..._WhileAdultHandoffAwaitsConfirmation_ReportsHandoffPauseWithoutAdvancing`,
  `SC010TimePauseTests.TryAdvanceOneDay_WhileSuccessionChoicePending_ReportsSuccessionReason`,
  `..._WhileAdultHandoffAwaitsConfirmation_ReportsHandoffAction`,
  `AddSC010SuccessionPanelTests.PanelRegencyShowsRestrictedDecisionAndDisablesRestartActions`,
  `..._PanelAdultHandoffSurfacesPauseReasonAndRequiredAction`, plus the regency-message
  assertion in `SuccessionServiceTests.MinorSuccessor_RequiresEligibleAdultRegentAndCompletesAtThreshold`.

## Terminal extinction correction (Play Test SC-010 findings)

Scope: make the visible family-extinction result an actual terminal gameplay state and keep
the valid post-handoff retirement action distinct from a repeat confirm. No scene, prefab,
ProjectSettings, package, or unrelated-doc edits; no Unity execution by the implementer.

- `GameSession.SuccessionBlocksTimeAdvance` now treats `SuccessionPhase.Extinct` as blocking,
  and day/multi-day/month requests check it before any other pending work. An extinct family
  keeps its date, location, mission state and records unchanged; the refusal reuses
  `MonthTurnOutcome.PendingSuccession` with an extinction-specific `LastEventMessage` that
  names loading an earlier save as the recovery path.
- `PreviewTravel` and `ConfirmTravel` reject an extinct family, so a stale preview cannot move
  the campaign. `WorldClockPanel` disables `推进一天`/`结束月份` and `WorldMapController`
  disables travel confirmation, cancellation and location selection by subscribing to
  `SessionStateChanged`; both re-enable when an earlier checkpoint is loaded.
- `SuccessionPanel.Refresh` treats `Extinct` as a terminal modal state: the modal stays visible,
  the extinction status and recovery text are shown, candidate/regent lists and all crisis or
  confirm buttons are hidden, and `退休并安排继承` / `示范：无继承人危机` stay disabled.
  The map HUD's existing `存档`/`读档` controls are intentionally left available.
- After a completed adult handoff the phase is `None`, so `退休并安排继承` remains enabled. That
  is intentional: it starts a *new* succession cycle for the current head; it never repeats the
  completed handoff. `SC010_DEMO.md` Play Test A step 5 now states this, and
  `AddSC010SuccessionPanelTests.PanelAdultConfirmTransfersHead` asserts that the button stays
  interactable and that using it enters `AwaitingSuccessionChoice` again.
- Regression tests:
  `GameSessionSuccessionTests.ExtinctFamily_RejectsEveryTimeAndTravelMutationAndPreservesState`,
  `GameSessionSuccessionTests.ExtinctFamily_CanStillLoadAnEarlierCheckpoint`,
  `SC010TimePauseTests.ExtinctFamily_DisablesClockControlsAndReenablesThemAfterCheckpointLoad`,
  `SC010TimePauseTests.ExtinctFamily_DisablesMapTravelAndLocationControls`,
  `AddSC010SuccessionPanelTests.PanelExtinctionStaysVisibleAsTerminalStatusWithoutSuccessionActions`.
  The lead runs them in the already-open Editor; the implementer does not claim they passed.

### Unresolved GDD decision

GDD 4.3 lists possible regency content (power struggle, influencing the minor, changed
family policy, a regent refusing to hand over) but says v1 only needs "指定摄政、受限决策
与成年交权"; GDD 19.2 requires showing restricted decisions during a regency without
defining which decisions. The prototype has no regency-specific decision content, so the
HUD only reports the restrictions that actually exist (retirement and crisis demo are
disabled, time pauses at adulthood). Defining a new regency-only decision, its cost, and its
visible choices needs an explicit design decision; it is left unimplemented rather than
invented here.

## Accepted checkpoint package (post-acceptance)

The user accepted SC-010 and requested a future GitHub checkpoint. The previous
read-only-review rule that kept the SC-003 model unstaged is superseded: the SC-003
files are now part of this package because the SC-010 source and tests reference them
directly. Include their existing content and `.meta` files without rewriting them.

Smallest source package:

- SC-003 dependency: `Assets/Scripts/Domain/StableIds.cs`, `PersonState.cs`,
  `FamilyState.cs`, `KinshipGraph.cs`, `InterpersonalState.cs`,
  `FamilyRelationshipValidator.cs`, plus `PersonFamilyIdentityTests.cs`,
  `KinshipInterpersonalTests.cs`, `FamilyRelationshipValidatorTests.cs`, and all metas.
- SC-010 domain: `SuccessionState.cs`, `SuccessionService.cs` and metas.
- SC-010 application/persistence/presentation: modified `GameSession.cs`,
  `SaveData.cs`, `SaveDataMigrator.cs`, `SaveDataValidator.cs`, `SaveSlotStore.cs`,
  `WorldClockPanel.cs`, `WorldMapController.cs`, new `SuccessionPanel.cs`, modified
  `SaveDataValidatorTests.cs`, and new `SuccessionServiceTests.cs`,
  `GameSessionSuccessionTests.cs`, `SuccessionSaveTests.cs`,
  `AddSC010SuccessionPanelTests.cs`, and `SC010TimePauseTests.cs` with their metas.
- SC-010 Editor integration: `AddSC010SuccessionPanel.cs`,
  `FoundationSceneValidator.cs`, and the existing dirty
  `Assets/Scenes/FoundationScene.unity` wiring. Scene changes remain lead-owned and
  must be included as-is when the checkpoint commit is prepared.
- Documentation: `SC010_DEMO.md` and this plan.

Preserve unrelated dirty files, including direction-refresh docs, `SampleScene.unity`,
`Assets/1.unity`, `ProjectSettings`, `graphify-out/`, `nul`, and `AGENTS.md` edits.
The implementation and review tasks do not stage, commit, push, merge, or tag. The
lead owns the accepted checkpoint delivery. The user requested delivery to GitHub
with `main` as the only remaining branch; preserve the existing tags and delete
redundant branch refs only after ancestry checks.

## Out of scope

SC-011 cross-generation event content, SC-012 full chronicle, domain-wide politics, new packages, and third-party assets are out of scope. Checkpoint delivery is a separate lead-owned post-acceptance action.


---

<a id="2026-11-02-sc-011-cross-generation-event"></a>
## 来源：docs/superpowers/plans/2026-11-02-sc-011-cross-generation-event.md

# SC-011 Cross-Generation Event Implementation Plan

**Status:** user accepted, 2026-11-02. The user reports compilation passed, EditMode passed, no Console errors, and Play Test passed. EditMode totals and Console warning count were not supplied; `FoundationSceneValidator` result was not reported. This record is based on user report, not captured Editor logs.

**Audit note (2026-11-02):** GDD 7.5 text templating is out of scope for this slice (two fixed variants instead); GDD 19.1's event re-check and atomic-commit rules are preserved for history conditions and option requirements, except that succession-count conditions are offer-time only so a stable pending decision stays resolvable; GDD 19.4's three-generation M2 script is broader than SC-011's narrower acceptance criterion. The root's existing redundant `Always` condition is replaced by exactly `SuccessionCountEquals 0`. At implementation time no Editor for this project was available; the user later reported compilation and EditMode passed.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After the first generation chooses how to receive the rainy-night guest, one real SC-010 head change occurs, and the successor receives one follow-up encounter whose visible text and enabled choices depend on that saved choice.

**Architecture:** Keep the existing monthly trigger and allowlisted content engine. Add three condition kinds (one history lookup, two succession-count gates) and a small `EventRuleContext`; gate the accepted root to `SuccessionCount == 0`; add two mutually exclusive follow-up variants (one encounter); use the existing event-choice panel and SC-010 succession panel. No new trigger, save field, NPC, scene, package, or text-template subsystem.

**Tech Stack:** Unity 6000.6.6f1, existing pure C# Domain/Events/Persistence assemblies, existing uGUI, NUnit EditMode tests, Orca supervised OpenCode implementation and independent read-only review.

**Spec:** `docs/superpowers/specs/2026-11-02-sc-011-cross-generation-event-design.md`

## Global Constraints

- Work only in `C:\Users\panye\My project` on `feature/sc011-cross-generation-event`. Do not create a project, worktree, branch, package, asset, or scene.
- Do not edit scenes, prefabs, `ProjectSettings`, `Library`, `Temp`, `SampleScene.unity`, `Assets/1.unity`, `graphify-out/`, `nul`, or unrelated dirty docs. No scene updater or `FoundationSceneValidator` change is expected; the existing event-choice and succession panels are reused.
- Preserve every `.meta` file and GUID. The two JSON resources are edited in place and keep their existing metas.
- Preserve the accepted rainy-arrival text, both option definitions, their requirements and effects, weight, priority, cooldown, once scope, and chain membership. The only root change is the new `SuccessionCountEquals 0` event condition.
- Do not add a new root event, NPC, displaced-youth character, third-generation content, or external dependency.
- Do not edit `PROJECT_STATE.md` or `DECISIONS.md` in this task; the later implementation/docs task updates the checkpoint and next target.
- Do not run batchmode or a second Unity instance. Use the already-open Editor for compilation and tests. Do not use screenshots or Computer Use. The user runs the Play Test.
- No commit, merge, push, or tag. The lead owns integration and checkpoint delivery. Checkpoint merge/tag waits for explicit user acceptance.
- Keep `PersonId`/`SuccessionWorld` and `CharacterId`/`CharacterRelationships` distinct. The only cross-generation carrier is persisted event history; follow-up effects target the existing NPC `npc.kuroda.iori` as the family's standing with an old guest.

## Review Focus

1. The accepted root is unchanged in text/options/effects/requirements and is not offered after the first handoff.
2. Exactly one follow-up variant can be eligible for a given root history, with deterministic ordering and no weighted ambiguity.
3. Adult handoff schedules after the next month; regency defers until the explicit majority handoff; extinction/pending succession/regency never schedule.
4. Repeated succession cannot duplicate or reopen the chain; loading a pre-follow-up save replays only that rolled-back timeline.
5. A pending event can always be resolved after a later head change (succession gates are offer-time only).
6. Save/load restores root choice, pending follow-up, once markers, relationship delta, and succession state; content `1 → 2` is additive-only and unknown IDs still reject atomically.
7. No scene, prefab, ProjectSettings, package, or unrelated-doc change.

---

### Task 1: Content contract and validator

**Files:**
- Modify: `Assets/Scripts/Events/EventCatalogData.cs`
- Modify: `Assets/Scripts/Events/EventCatalogValidator.cs`
- Test: `Assets/Tests/EditMode/EventCatalogValidatorTests.cs`

**Interfaces:**
- Add `EventConditionKind.EventOptionChosen`, `SuccessionCountEquals`, `SettledSuccessionCountAtLeast`.
- Add `EventConditionData.sourceEventId` and `sourceOptionId`.
- Add `SC011EventIds` constants for both follow-up event IDs and all six follow-up option IDs.
- Validator rules:
  - `EventOptionChosen` requires non-empty `sourceEventId`/`sourceOptionId`, an existing source event and option, a source event different from the referencing event, the same non-empty `chainId` as the source, and at most one `EventOptionChosen` condition per event.
  - Reject two events that claim the same `(sourceEventId, sourceOptionId)` pair.
  - `SuccessionCountEquals`/`SettledSuccessionCountAtLeast` require `threshold` in `[0, 1000]`, are event-level only, and are allowed only on `Monthly` events.
  - Follow-up events with `EventOptionChosen` must be `PerGame`; the root remains `PerGame`.
  - Existing chain membership, duplicate ID, text, weight, cooldown, and enum checks stay unchanged.

- [x] Author tests for a missing source event, missing source option, cross-chain reference, self-reference, duplicate branch pair, two history conditions on one event, invalid threshold, non-monthly follow-up, and a valid two-variant chain (authored, not executed).
- [x] Extend the DTO/enums and constants without changing any existing serialized field name or order.
- [x] Implement structural validation inline and defer source-reference checks until all events are registered (a condition may reference an event later in the array).
- [x] Run the focused validator tests; the user reports EditMode passed, exact test totals were not supplied.

**Observable check:** `EventCatalogValidatorTests` accepts the SC-011 catalog shape and rejects each malformed branch with an actionable error; the existing rainy-arrival catalog still validates.

---

### Task 2: Rule context and engine semantics

**Files:**
- Create: `Assets/Scripts/Events/EventRuleContext.cs` and Unity-generated `.meta`
- Modify: `Assets/Scripts/Events/EventEngine.cs`
- Test: `Assets/Tests/EditMode/EventEngineTests.cs`

**Interfaces:**
- `public readonly struct EventRuleContext`
  - `EventRuleContext(int successionCount, int settledSuccessionCount)`
  - `int SuccessionCount`, `int SettledSuccessionCount`, `static EventRuleContext Default`
- Add optional `EventRuleContext context = default` to `GetEligibleEventIds`, `ScheduleMonthly`, and `TryChooseEventOption`; existing call sites and tests keep compiling.
- `EventOptionChosen` is true iff the **last** committed history entry whose `eventId` equals `sourceEventId` has the matching `optionId`; false when there is no entry.
- `SuccessionCountEquals` compares `context.SuccessionCount == threshold`.
- `SettledSuccessionCountAtLeast` compares `context.SettledSuccessionCount >= threshold`.
- Succession-count conditions are evaluated only during eligibility/scheduling. `EventOptionChosen` is evaluated at scheduling and again at commit. Document this in XML comments.
- Monthly ordering stays priority descending, ordinal `eventId`, then weighted roll inside the top-priority group. No queue changes.

- [x] Author tests for: root eligible at context `(0,0)`; root ineligible at `(1,0)` and `(1,1)`; follow-up A eligible only with the offer-rest history and settled `1`; follow-up B only with ask-tomorrow; no variant with no history or settled `0`; last-entry wins if history is duplicated; one follow-up candidate selects without ambiguity; a pending root still commits after a handoff; existing rainy-arrival scheduling/effects/requirements are unchanged (authored, not executed).
- [x] Implement `EventRuleContext`, history lookup, and the condition branches behind the optional parameter.
- [x] Keep `TryGetOptionAvailability` unchanged; follow-up options use `Always` requirements.
- [x] Run focused tests; the user reports EditMode passed, exact test totals were not supplied.

**Observable check:** At `(0,0)` the accepted root is the only eligible event; at `(1,1)` exactly one follow-up variant is eligible for each root option; no test relies on random selection between variants.

---

### Task 3: Content, texts, and content version

**Files:**
- Modify: `Assets/Resources/Events/event-catalog-v1.json`
- Modify: `Assets/Resources/Localization/event-text-zh-v1.json`
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Test: create `Assets/Tests/EditMode/SC011EventContentTests.cs` and Unity-generated `.meta`

**Interfaces:**
- Catalog/text `contentVersion = 2`; `SaveData.CurrentEventContentVersion = 2`.
- Root `event.rainy-arrival`: add only `{"type":"SuccessionCountEquals","threshold":0}` to `conditions`.
- Extend `chain.rainy-arrival.linkedEventIds` to include both follow-up event IDs.
- Add `event.rainy-arrival.legacy-honored` and `event.rainy-arrival.legacy-deferred` with `trigger: Monthly`, `weight: 1`, `priority: 100`, `cooldownMonths: 0`, `onceScope: PerGame`, the same `chainId`, and the conditions in §4.2 of the spec.
- Add options/effects/text exactly as the spec table; all follow-up option requirements are explicit `Always`.
- Keep resource file names and metas unchanged.

- [x] Author tests that assert contentVersion `2`, root text/options/effects/requirements/weight/priority/cooldown/once/chain remain as accepted, the root has exactly the new `SuccessionCountEquals 0` condition, each root option has exactly one follow-up variant, and the two variants use different option IDs (authored, not executed).
- [x] Edit the JSON data and add original Chinese titles/bodies/summaries from the spec. Do not copy commercial-game text.
- [x] Bump the fallback content version in `SaveData.cs`.
- [x] Validate the catalog through `EventCatalogValidator` as part of the user-reported passing EditMode run; exact test totals were not supplied.

**Observable check:** The catalog validates at version 2; the root is unchanged except for one eligibility condition; variant A has `旧客的雨夜回访` and its three options; variant B has `门房里的旧雨` and its three options; `SC011EventContentTests` asserts the preserved root fields directly from the loaded catalog.

---

### Task 4: Session integration, save compatibility, and regression coverage

**Files:**
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: create `Assets/Tests/EditMode/SC011LegacyEventTests.cs` and Unity-generated `.meta`
- Test: modify `Assets/Tests/EditMode/GameSessionSaveTests.cs` and `Assets/Tests/EditMode/SaveDataValidatorTests.cs` only as needed

**Interfaces:**
- Add a private `BuildEventRuleContext()`:
  - `SuccessionCount = Succession.SuccessionCount`
  - `SettledSuccessionCount = Succession.Phase == SuccessionPhase.None ? Succession.SuccessionCount : 0`
- Pass it to `ScheduleMonthly` in `TryAdvanceToNextMonth`, and to `TryChooseEventOption` in `GameSession.TryChooseEventOption`. Do not change `TryGetEventOptionAvailability`.
- In `TryApplySave`, accept content `1` under current content `2` only:
  - `data.eventContentVersion == _eventCatalog.contentVersion || (data.eventContentVersion == 1 && _eventCatalog.contentVersion == 2)`
  - Keep `TryRestoreState` as the authority for every referenced ID; reject unknown IDs atomically.
- No save schema change and no new `EventRuntimeSaveData` field.

- [x] Author adult-handoff test: complete rainy-arrival with offer-rest, confirm 正綱, verify `SuccessionCount == 1` and phase `None`, end month, assert the pending event is `event.rainy-arrival.legacy-honored` (`AdultHandoffSchedulesHonoredVariantForOfferRestChoice`; authored, not executed).
- [x] Author the ask-tomorrow mirror asserting `event.rainy-arrival.legacy-deferred`; raise Iori's relationship to at least `1` first so the root option remains selectable under its accepted `RelationshipAtLeast 1` requirement (`AskTomorrowChoiceRequiresTrustThenSchedulesDeferredVariant`).
- [x] Author a regency test: confirm 時姫 with regent 千恵, end months through the regency, assert no follow-up while `Regency`; complete the majority handoff, end month, assert the follow-up is eligible (`RegencyDefersVariantUntilExplicitAdultHandoff`).
- [x] Author repeated-succession tests: a second succession before the follow-up is scheduled (minor heir → regency) does not duplicate or reopen the chain, and the follow-up is queued exactly once after the successor settles (`SecondSuccessionBeforeFollowUpSchedulesItOnceAfterSuccessorSettles`); a later handoff after the follow-up is committed does not reopen the root or reschedule (`FollowUpDoesNotRepeatAfterAnotherHandoff`).
- [x] Author no-choice test: handoff without rainy-arrival history yields no follow-up and no error (`HandoffWithoutRootHistorySchedulesNothing`).
- [x] Author scheduling-failure coverage at the existing `EventEngine` seam: a failed `ScheduleMonthly` (pending/non-management readiness) leaves the runtime snapshot and the event RNG state unchanged (`SchedulingFailureDoesNotMutateRuntimeOrConsumeEventRandom`). A full session-level date/succession failure injection is **deferred/unreachable without a production hook** because a valid catalog plus the session's own guards leave no reachable `SchedulingFailed` path; no test hook was added and the existing rollback code is unchanged. That session-level check remains a Unity/manual gate.
- [x] Author save/load tests for root pending (`RootPendingSaveRestoresSameRootAndOptions`), after root choice (`AfterRootChoiceSaveDoesNotReofferRoot`), adult handoff before the next month (`PostHandoffSaveReplaysSameVariant`), follow-up pending (`FollowUpPendingSaveKeepsVariantOptionsAndRelationshipValue`), after follow-up choice (`AfterFollowUpChoiceReloadKeepsHistoryAndOnceMarker`), and during/after regency (`RegencyCheckpointBeforeFollowUpReplaysAfterMajority`). Succession-pending save/load remains covered by the existing SC-010 `SuccessionSaveTests`.
- [x] Author compatibility tests: v3 content 1 with valid IDs loads additively; v3 content 1 with an unknown pending event rejects without changing the live session; v3 content 3 rejects (`ContentOneSaveLoadsAdditivelyAndIsRewrittenAsTwo`, `ContentOneSaveWithUnknownPendingEventRejectsAtomically`, `UnknownContentVersionThreeRejectsWithoutChangingTheSession`). Migration seams: a v2 payload missing `eventContentVersion` is rejected as missing metadata, v2 content 1 migrates to schema v3 preserving content 1, and v1 without content metadata migrates to current content 2 (`SaveSlotStoreTests`, `SaveDataValidatorTests`).
- [x] Preserve the existing SC-010 tests and behavior; do not change `SuccessionService`, `SuccessionState`, `SuccessionPanel`, or `EventChoicePanel` unless a compile/test failure proves a minimal fix is required, in which case stop and report before expanding scope.
- [x] Run the full EditMode suite; the user reports it passed, exact test totals were not supplied.

**Observable check:** The user-visible sequence in the spec works at the session level without any scene change; save/load at each stable point restores the same pending variant or the same post-choice state.

---

### Task 5: Verification, independent review, and user Play Test

**Files:** no production files in this task. Do not create a demo doc unless the lead requests it; the Play Test below is the acceptance script.

- [x] Compilation: the user reports passed; an Editor log was not supplied.
- [x] EditMode: the user reports passed; exact passed/failed/skipped counts were not supplied.
- [ ] Console: the user reports no errors; warning count and exact Console totals were not supplied.
- [ ] FoundationScene validator: result/report path not supplied; do not claim a pass.
- [x] Separate read-only OpenCode2 reviews returned ACCEPT; this is static review, not runtime evidence.
- [x] User ran the Play Test and explicitly accepted SC-011 on 2026-11-02; lead checkpoint integration is authorized.

**User-run Play Test (text-only; do not run it for the user, do not use screenshots):**

1. Enter Play Mode in `FoundationScene`, click `新游戏`. Record the starting date `1560-04-01`, head `明智 晴信`, and the visible event/relationship state.
2. Click `结束月份`. Expected: `雨夜来客` appears with `请他进屋歇息` and `请他明晨再来`; the root text matches the accepted demo.
3. Choose `请他进屋歇息`. Expected feedback: the existing +5 trust summary. Save with `存档`. Record the feedback and that the choice is visible.
4. Click `退休并安排继承`, choose `明智 正綱`, then `确认交接`. Expected: head becomes `明智 正綱`, the succession modal closes, the handoff record is `succession.1.person.akechi.masatsuna`; date, location, missions, merit, and other relationship values are otherwise unchanged. Save.
5. Click `结束月份`. Expected: `旧客的雨夜回访` appears with `重申先代的门诺`, `按当下利害重议`, `止于这一夜`. The body mentions the old guest and the earlier hot tea; variant B's `门房里的旧雨` must not appear.
6. Do not choose yet: load the step-4 save (single save slot), then click `结束月份`. Expected: the same `旧客的雨夜回访` and the same three options. Then choose `重申先代的门诺`; expected feedback confirms the old pledge and the +5 trust summary. Save, click `结束月份` again, and expect no rainy-arrival repeat and no second legacy event.
7. Load the post-choice save, then click `结束月份`. Expected: no repeat, and the root is not reopened. This proves save/load at the post-choice stable point.
8. Start a new game. Before the first `结束月份`, travel to Atsuta and use 黒田伊织's town dialogue to raise his trust to at least 1 (accept his proposition), then return to the map. This is required because the accepted `请他明晨再来` option keeps its existing `RelationshipAtLeast 1` requirement and is disabled at the default value 0. Then click `结束月份`, choose `请他明晨再来`, retire to `明智 正綱`, then click `结束月份`. Expected: `门房里的旧雨` appears with `补上先代欠下的礼数`, `说明当日不得已`, `旧事不必再提`; variant A's text and options must not appear.
9. Record each step pass/fail and the visible text in the test record. Do not use screenshots.

Regency behavior is covered by authored EditMode tests; the user reports EditMode passed but did not supply per-suite counts. A separate regency Play Test would require many month turns and is not part of the smallest slice.

**Evidence boundary:**

- User-reported results: compilation passed; EditMode passed; no Console errors; Play Test passed and accepted.
- Not supplied: EditMode totals, Console warning count, and FoundationScene validator result. Do not infer these values or claim validator success.
- The read-only code review is not Play Mode evidence. The Play Test acceptance is the user's report dated 2026-11-02.
- User acceptance authorizes the lead to create the checkpoint, merge to `main`, tag `demo-SC011`, and push under project policy.

---

## Implementation status (2026-11-02)

Authored in this task, but **not verified by Unity**:

- `Assets/Scripts/Events/EventCatalogData.cs` — three condition kinds, `sourceEventId`/`sourceOptionId`, `SC011EventIds`.
- `Assets/Scripts/Events/EventCatalogValidator.cs` — structural branch checks plus deferred source-reference checks.
- `Assets/Scripts/Events/EventRuleContext.cs` (+ `.meta`) — immutable offer-time rule context.
- `Assets/Scripts/Events/EventEngine.cs` — optional context parameters, history lookup, offer-time succession gates.
- `Assets/Scripts/Application/GameSession.cs` — `BuildEventRuleContext()`, context wiring, content `1 → 2` additive load window.
- `Assets/Scripts/Persistence/SaveData.cs` — fallback content version `2`.
- `Assets/Resources/Events/event-catalog-v1.json` and `Assets/Resources/Localization/event-text-zh-v1.json` — content version `2`, root gate, two follow-up variants, original Chinese text.
- `Assets/Tests/EditMode/EventCatalogValidatorTests.cs`, `EventEngineTests.cs` — extended coverage, including a direct scheduling-failure no-mutation test.
- `Assets/Tests/EditMode/SC011EventContentTests.cs` and `SC011LegacyEventTests.cs` (+ `.meta` files) — content contract, adult/regency/deferred/second-succession/root-pending-save-load/compatibility coverage.
- `Assets/Tests/EditMode/SaveSlotStoreTests.cs`, `SaveDataValidatorTests.cs` — v2 missing-content-version rejection, v2 content-1 migration, and v1 missing-content-version migration to current content `2`.

**Verification report:** The user reports Unity compilation passed, EditMode passed, no Console errors, and Play Test passed with explicit acceptance. Exact EditMode totals and Console warning count were not supplied; `FoundationSceneValidator` result was not reported. The user-provided results are not accompanied by Editor logs in this worktree.

Offline checks actually run while reconciling this plan (not Unity evidence): both resource JSON files parse as UTF-8, `contentVersion` is `2` in each, the root has exactly `SuccessionCountEquals 0`, the two variants key `offer-rest`/`ask-tomorrow` with `SettledSuccessionCountAtLeast 1`, the chain links all three event IDs, and the two Chinese titles match the spec.

## Review follow-up (2026-11-02)

A read-only review of `task_a51cf1873fb0` found no functional defect. This follow-up corrected documentation and added regression coverage:

- Event-rule citations now point at GDD 19.1 (not GDD 18), the section that actually requires re-checking before display and re-validating before commit.
- The compatibility matrix now states that a schema-v2 payload requires `eventContentVersion` `1` and that missing required metadata is rejected by `SaveDataValidator.ValidatePresence`; only schema v1 may omit the content version and migrate to the current `2`.
- `EventRuleContext`'s comment now matches the code: `GameSession` passes a context at commit, while `EventEngine` ignores succession gates at commit and still re-checks history conditions.
- Added root-pending and post-root-choice save/load tests, a second-succession-before-scheduling test, a regency-checkpoint replay test, a direct scheduling-failure no-mutation test, and the v1/v2 migration tests described above. No production behavior changed.

---

## Rollback and ownership

- If a change requires a scene, prefab, ProjectSettings, package, or new asset beyond the two JSON resources, stop and report; do not expand scope.
- If a test exposes an SC-010 regression, prefer a minimal SC-011-only fix; do not rewrite the succession model.
- The lead owns scene validation, final integration, checkpoint commit, merge, and tag. The implementation and review tasks do not stage or commit.
- Preserve all pre-existing dirty docs and user WIP, including `PROJECT_STATE.md`, `DECISIONS.md`, `SampleScene.unity`, `Assets/1.unity`, `ProjectSettings/SceneTemplateSettings.json`, `graphify-out/`, and `nul`.


---

<a id="2026-11-02-sc-012-family-chronicle"></a>
## 来源：docs/superpowers/plans/2026-11-02-sc-012-family-chronicle.md

# SC-012 家族编年史实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在现有 Unity 项目中记录事件、继承、任务和实际关系变化，保存为可恢复的家族编年史，并在 `FoundationScene` 提供可试玩的只读面板。

**Architecture:** 在 Domain 增加纯 C# 追加式 HistoryLedger；由 `GameSession` 只在成功提交后统一记录；Persistence 将存档从 v3 升至 v4，并把旧事件历史按年/月迁入新编年史；Presentation 读取不可变记录并展示；Editor updater 以事务方式将一个固定根及 UI 接入已有 FoundationScene。

**Tech Stack:** Unity 6000.6.6f1、现有纯 C# Domain/Application/Persistence 程序集、Unity uGUI、Unity Test Framework/NUnit、Orca supervised OpenCode2 implementation and independent OpenCode2 review。

**Spec:** `docs/superpowers/specs/2026-11-02-sc-012-family-chronicle-design.md`

## 全局约束

- 只在 `C:\Users\panye\My project` 的 `feature/sc012-family-chronicle` 工作；不得新建 Unity 项目、worktree、场景或分支。
- GDD v4.0 与已确认设计稿为权威；不扩展至当前未实现的财富、死亡、势力、战斗、忍术或结局系统。
- OpenCode2 实现所有代码；另一个 OpenCode2 worker 在实现完成后只读复审。实现与复审顺序执行，同一时间不得有两个 worker 写入工作区。
- 实现 worker 的文件所有权限于本计划 Tasks 1–4 中列出的 C#、测试和 `.meta` 文件；不得直接编辑任何 `.unity`、prefab 或 `ProjectSettings`。Lead 保留实际运行 updater、验证并保存 `FoundationScene` 的场景集成所有权。
- 保留所有已存在未提交/未跟踪工作；不得改动 `Assets/Scenes/SampleScene.unity`、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 或无关历史文档。
- 不改 `Library`/`Temp`；不增包；保留既有 `.meta` 和 GUID。新 Unity 文件须配套 `.meta`。
- 对事件关系效果，只有事件事务提交成功才允许追加编年史；读档恢复不记录；关系上下限导致的实际 no-op 不记录。
- 新 UI 使用现有 uGUI，在单一固定根下生成；Editor 更新器用一个 Undo 组，先验证再保存，异常时 Undo 回滚，不重载场景回滚。
- 不运行与用户 Editor 冲突的 Unity 实例或 batchmode。使用现有 Editor 的 Test Runner、Console 和 `Editor.log`；如果本项目 Editor 未打开，报告验证阻塞并将准确步骤交给用户，不以源码检查冒充 Unity 验证。
- 试玩由用户执行。用户清醒时不使用截图或 Computer Use 操作 Unity。未获 Play Test 明确验收前，不提交 checkpoint、不合并、不打标签。

## 复审重点

1. 事件关系效果若回滚，不能留下编年史或关系记录；覆盖成功提交与事务失败两条路径。
2. 存档恢复重建当前关系和任务时不能生成新记录；v3 的事件历史必须迁入，不能虚构旧任务/关系/继承经历或事件日。
3. 同月内的事件、任务、关系和继承记录必须按稳定序号排序；重复/非法 ID 或非法记录必须原子拒绝。
4. 关系上限/下限的关系变化 no-op、失败任务和失败继承动作不得产生记录；多次实际变化必须分别显示前后值。
5. 新游戏空状态、长记录滚动、打开/关闭和存读档刷新不得造成按钮重复绑定或记录重复。

---

### Task 1：Domain 编年史记录模型

**文件：**
- 新建：`Assets/Scripts/Domain/FamilyChronicleEntry.cs`、对应 `.meta`
- 新建：`Assets/Scripts/Domain/HistoryLedger.cs`、对应 `.meta`
- 测试：`Assets/Tests/EditMode/HistoryLedgerTests.cs`、对应 `.meta`

**接口：**
- 产出不可变 entry：稳定 `entryId`、递增 `sequence`、`year`/`month`、类别、`subjectId`/`sourceId`、标题/摘要，以及任务阶段或关系前后值等类别专属字段。
- 产出 `HistoryLedger`：只追加、按序号读取、可捕获/恢复；Domain 不引用 Unity、uGUI 或 Persistence。
- 年/月是日期精度；不在 entry 中要求 day，因为 v3 事件历史未保存 day。

- [x] **Step 1：先写 HistoryLedger 的失败测试**：空 ledger；连续追加序号与稳定 ID；事件/关系/任务/继承类别字段保存；拒绝非法年月、空 ID、重复 ID、倒序/重复序号、关系前后值相同、任务 from/to 相同。测试源已写；尚未在 Unity Test Runner 执行。
- [ ] **Step 2：在 Unity EditMode Runner 运行 `HistoryLedgerTests`，确认新增行为测试先失败。** 若本项目 Editor 未打开，停止 Unity 命令并记下用户需运行的准确测试项。
- [x] **Step 3：实现最小的不可变 entry 与 ledger 接口**；追加成功才推进序号；枚举或 DTO 边界不得接受未定义类别。
- [ ] **Step 4：重跑 `HistoryLedgerTests`，确认全部通过，并检查 Domain asmdef 仍无 Unity 引用。**

**可观察结果：** 相同输入总是生成相同次序与 ID；非法条目不改变 ledger。

---

### Task 2：v4 持久化、校验与迁移

**文件：**
- 修改：`Assets/Scripts/Persistence/SaveData.cs`
- 修改：`Assets/Scripts/Persistence/SaveDataMigrator.cs`
- 修改：`Assets/Scripts/Persistence/SaveDataValidator.cs`
- 修改：`Assets/Scripts/Persistence/SaveSlotStore.cs`（仅当存在字段存在性/迁移接线需要）
- 测试：`Assets/Tests/EditMode/SaveDataValidatorTests.cs`
- 测试：`Assets/Tests/EditMode/SaveSlotStoreTests.cs`
- 测试：`Assets/Tests/EditMode/FamilyChronicleSaveTests.cs`、对应 `.meta`

**接口：**
- `SaveData` 增加 `ChronicleSaveEntry[] chronicle`；`SaveDataValidator.CurrentVersion` 从 3 调至 4。
- 更新 `SaveDataPresenceProbe` 和 JSON 根字段检查：v4 必须明确包含 `chronicle`；v1/v2/v3 按各自既有 presence 规则迁移。
- Persistence DTO 只存纯数据/稳定 ID；`HistoryLedger` 负责恢复运行时记录。
- v3 迁移保留原存档全部状态，并把 `eventRuntime.history` 按原顺序复制到 chronicle；月份保留，day 不得伪造；较旧 v1/v2 继续迁移到当前 v4。
- v1/v2/v3 不可凭当前状态重建不存在的任务、关系、继承历史。v4 若缺少 `chronicle` 根字段则按严格 presence 规则拒绝。

- [x] **Step 1：新增失败测试**：有效 v4 ledger round-trip；v3 事件记录迁移且按顺序保留年/月/summary；v3 无历史迁移为空；v1/v2 仍迁移至 v4；缺 chronicle、重复 ID/序号、非法类别/年月和非法关系/任务前后值均拒绝；拒绝加载时 live session 不变。测试源已写；尚未在 Unity Test Runner 执行。
- [ ] **Step 2：在 Unity EditMode Runner 运行新增迁移/校验测试，观察预期失败。**
- [x] **Step 3：扩展 v4 DTO、presence probe、validator 与 migrator**；保持现有原子迁移、未知 ID 拒绝与原事件运行时历史语义。
- [ ] **Step 4：运行 `SaveDataValidatorTests`、`SaveSlotStoreTests`、`FamilyChronicleSaveTests`，确认新增及旧迁移路径通过。**

**可观察结果：** 已接受的 v3 存档能无损迁入 v4；已有事件经历可读；无法恢复的旧历史保持空白而不是伪造。

---

### Task 3：GameSession 成功提交记录接线

**文件：**
- 修改：`Assets/Scripts/Application/GameSession.cs`
- 修改：`Assets/Scripts/Events/EventEngine.cs`
- 测试：`Assets/Tests/EditMode/GameSessionChronicleTests.cs`、对应 `.meta`
- 需要时扩展：`Assets/Tests/EditMode/EventEngineTests.cs`、`GameSessionMissionTests.cs`、`GameSessionSuccessionTests.cs`

**接口：**
- `GameSession` 持有 `HistoryLedger`；提供只读快照给 UI，并在 `CaptureSave`/`TryApplySave` 中捕获、原子恢复。
- 关系变更统一记录 `RelationshipResult.PreviousValue/CurrentValue`。Dialogue、直接 API、事件效果都走同一应用层记录规则；实际变化才写入。
- 三类 Mission wrapper 在动作成功且 stage 真实改变时写入 from/to 与 action feedback。
- 当前 succession expression 会在调用 `ApplySuccessionResult` 前已改变状态；先重构 wrappers，使其在调用 `SuccessionService` 前捕获阶段、家主和完成结果 ID，再将前快照与结果交给应用层记录器。只有成功且快照显示真实状态变化时记录；失败不写入。
- 事件引擎只有在 `TryCommit` 成功后向应用层返回已提交效果腿；提交失败回滚后不能向 ledger 泄漏任何关系变化。
- 每个成功的记录动作在同一应用层提交点触发一次 `SessionStateChanged`，让打开中的只读面板刷新；不得由读取/保存触发记录或重复通知。

- [x] **Step 1：先写应用层失败测试**：事件选择提交后记录事件及关系变化；使用已提交的重复 result ID 强制 `TryCommit` 失败，断言关系回滚且无记录；对白关系变化记录前后值；直接关系 API 记录实际变化；关系达上/下限 no-op 不记录；三个任务各个成功阶段变化各记录一次，错误地点/重复动作不记录；继承阶段变化按时间记录；新游戏与读档不记录；跨类别同月序号稳定。测试源已写；尚未在 Unity Test Runner 执行。
- [ ] **Step 2：运行 `GameSessionChronicleTests` 与被扩展的现有 EditMode 测试，确认新断言先失败。**
- [x] **Step 3：在 GameSession 的成功提交边界接线**；不在 Domain `TryChange` 或 mission restore 中记录；事件效果记录必须在 commit 后。
- [ ] **Step 4：重跑全部相关 EditMode 测试，确认回滚、迁移和已有 SC-010/SC-011 行为保持通过。**

**可观察结果：** 每条新记录对应一次实际成功变化，失败/加载/回滚都不生成记录；`CaptureSave` 后重建 session 得到完全相同顺序。

---

### Task 4：编年史面板与 FoundationScene 接入

**文件：**
- 新建：`Assets/Scripts/Presentation/FamilyChroniclePanel.cs`、对应 `.meta`
- 新建：`Assets/Scripts/Editor/AddSC012FamilyChroniclePanel.cs`、对应 `.meta`
- 修改：`Assets/Scripts/Editor/FoundationSceneValidator.cs`
- 测试：`Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`、对应 `.meta`
- 测试：`Assets/Tests/EditMode/AddSC012FamilyChroniclePanelTests.cs`、对应 `.meta`
- 修改：仅通过该 updater 更新 `Assets/Scenes/FoundationScene.unity`；保留既有 scene meta/GUID。绝不修改 `SampleScene.unity`。

**接口：**
- `FamilyChroniclePanel` 绑定 `GameSession`；显示年/月、类别、标题和摘要；空历史显示空状态；打开时滚到最新；关闭时不影响时间/状态。
- Editor updater 菜单使用 `Tools/Sengoku Chronicle/Add SC-012 Family Chronicle Panel`；生成一个固定根、一个可滚动内容区、一个地图开关按钮和一个关闭按钮。
- `FoundationSceneValidator` 检查唯一根、必需 UI 引用、按钮绑定及 ScrollRect/content 关系。

- [x] **Step 1：先写 EditMode 测试**：空历史占位；四类记录的行内容和日期；关系显示前后数值；刷新不重复行；打开/关闭不改 GameSession；updater 首次创建、重复执行不重复 UI、校验失败可 Undo 回滚。测试源已写；尚未在 Unity Test Runner 执行。
- [ ] **Step 2：运行 `FamilyChroniclePanelTests` 与 `AddSC012FamilyChroniclePanelTests`，观察新测试预期失败。**
- [x] **Step 3：实现面板和事务式 updater**；全部生成对象置于单一固定根下；沿用 `AddSC005EventChoicePanel`/`AddSC010SuccessionPanel` 的 UI 与 Undo 模式。
- [ ] **Step 4：交 Lead 在当前项目 Editor 执行 updater、保存 `FoundationScene` 并运行 `FoundationSceneValidator`。** 不启动第二个 Editor 或 batchmode；若没有项目 Editor，则保留 updater 和详细人工执行步骤，并将场景验证列为待用户操作。
- [ ] **Step 5：运行面板/updater EditMode 测试及全量 EditMode 套件；分别记录编译、EditMode 数量、Console error/warning 数和场景验证结果。**

**可观察结果：** 在现有 `FoundationScene` 中能打开编年史，空状态明确，新增历史按序显示且可滚动，界面不推进游戏时间。

---

### Task 5：项目文档、逐步 Play Test 与独立复审

**文件：**
- 修改：`PROJECT_STATE.md`
- 修改：`ARCHITECTURE.md`
- 修改：`DECISIONS.md`
- 新建：`SC012_DEMO.md`

- [x] 更新当前路线与 evidence 边界，标记 SC-012 为实现中/待用户试玩；记录 SC-012 不会自动改变 SC-011 验收。
- [x] 在 `SC012_DEMO.md` 写出测试前置条件、正确项目/branch/scene、场景 updater（如尚未运行）、编译/EditMode/Console/Validator 检查，以及逐步 UI 操作和每步可见预期。
- [x] Play Test 文档覆盖新游戏空历史、热田关系变化、补给任务四阶段、SC-011 雨夜事件、家主交接/后续事件、保存/读档后的记录一致性；用户于 2026-11-03 报告 EditMode 与 Play Test 通过并接受 SC-012。具体测试数量、Console 计数和 Validator 结果未单独报告。
- [x] 文档列明失败/无变化操作的测试证据边界，不要求用户通过隐藏调试入口触发失败操作。
- [x] Lead 已完成三轮顺序执行的 OpenCode2 独立只读复审；最后一轮 ACCEPT 当前源码/测试源，确认成人指定继承人阶段不变但家主变化可记录。仍有 append 失败路径缺少应用层测试与 v1 多余 chronicle 字段被忽略的边界说明。FoundationScene 未应用 updater；Unity 编译、EditMode、场景验证、Console 与 Play Mode 证据尚待取得。
- [x] 已向用户提供文档化的逐步试玩流程和纯文本结果清单；Play Test 验收前不提交 checkpoint、merge、tag 或 push。
- [x] 用户于 2026-11-03 报告 EditMode 与 Play Test 通过并明确验收；checkpoint `4443ccc` 已推送到 `main`，标签 `demo-SC012` 已推送。

**可观察结果：** 用户可以按 `SC012_DEMO.md` 单独复现整套流程，并逐步填写结果；所有验证类别保持分开记录。

---

## 设计覆盖与自检

- GDD SC-012 自动记录重要事件：Tasks 1–3 记录本项目已有的事件、任务、关系和继承变化；未来系统明确为非目标。
- GDD 12.6 编年史正确且存读档完整：Tasks 2–4 覆盖迁移、保存恢复、验证器和可视化。
- UI/场景范围：沿用现有场景，仅加一个固定 UI 根，不新建场景或项目。
- 事务与回滚：Task 3 在事件 commit 后记录；Task 4 Editor updater 验证/回滚遵循项目约束。
- 关系 no-op 与同月排序：Task 1/3 测试覆盖。
- 计划的主风险是项目 Editor 可能未打开；此时不擅自启动 batchmode，需在交付试玩步骤中标明场景生成/验证状态。

## 用户验收后的集成

- 用户明确报告 Play Test 通过后，在本 feature branch 创建 checkpoint，并立即推送 feature branch。
- 确认 checkpoint 完全合入 `main` 后 fast-forward；创建并推送 `demo-SC012` 标签及 `main`。
- 按用户“只保留 main”的既有要求，确认 feature 已合并、工作区当前为 `main` 后删除本地及远端 SC-012 feature branch；最终核实远端仅 `main`、本地仅 `main`，未提交 WIP 原样保留。


---

<a id="2026-11-03-sc-013-m2-fixed-three-generation"></a>
## 来源：docs/superpowers/plans/2026-11-03-sc-013-m2-fixed-three-generation.md

# SC-013 M2 Fixed Three-Generation Script Implementation Plan

> **Implementation status (2026-11-03, OpenCode2 worker):** All code, content, tests and documentation tasks are implemented. Static type-check of the eight assemblies (Domain, Persistence, Events, Application, Presentation, Editor, Player, EditModeTests) reports 0 errors; two text-only logic harnesses pass (30 and 33 checks). `FoundationScene.unity` is intentionally unchanged because the user's Editor is open and this task must not run Unity batchmode, a temporary project copy, GUI or Computer Use. The scene updater and validator results remain pending and must be produced from the existing Editor using the exact steps in `SC013_DEMO.md`. No Play Mode or user acceptance is claimed.

> **For agentic workers:** Implement this plan task-by-task through the supervised OpenCode2 worker on the existing feature worktree. The same worker may own the implementation sequence; a separate OpenCode2 worker performs the read-only review after implementation.

**Goal:** Deliver the approved SC-013 fixed M2 playthrough with two lawful handoffs, visible family identity, inherited relationship consequences, two distinct endings, and save/load continuity.

**Architecture:** Keep SC-010 standard and crisis sample behavior intact. `GameSession` starts the new M2 scenario by default; separate title-screen actions start the SC-010 standard sample and crisis demo. A dedicated scenario identity drives M2-only event order and succession gates; existing succession, event history, relationship, and chronicle services remain the source of truth. Add one family roster panel and an M2 ending summary beneath the existing fixed scene UI root.

**Tech Stack:** Unity 6, C#, existing Domain/Application/Events/Persistence assemblies, uGUI, Unity EditMode Test Runner.

**Spec:** `docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md`

## Global Constraints

- `GDD_v4.0.md` governs game design; this slice covers its M2 three-generation vertical script.
- Stay in `C:\Users\panye\My project`; do not create a Unity project, add a package, edit `ProjectSettings`, or touch `Library`/`Temp`.
- Preserve `.meta` files/GUIDs and all user WIP; stage only SC-013-owned files.
- Create a unique matching `.meta` for every new Unity asset through Unity's asset pipeline or the repo's established metadata pattern; never replace an existing GUID.
- All product code and Unity scene integration are implemented by OpenCode2. One designated integration worker owns `FoundationScene.unity` and its Editor generator/validator changes.
- Preserve accepted SC-011 event IDs, requirements, effects, content version, and save behavior. SC-013 adds its own event data.
- Keep the family ID, person IDs, event IDs, and option IDs stable after implementation; do not add save fields unless existing DTOs demonstrably cannot persist the approved state.
- Before implementation, Scout checks no more than three relevant GitHub projects for reusable fixed-scenario, roster, or event-order patterns. Report URL, license, Unity compatibility, activity, dependencies/security, effort, and reuse decision. Do not install, clone, or copy external code/assets. Build with existing project systems unless a suitable option is approved.
- Use the already open Unity Editor for compilation, EditMode tests, Console inspection, and scene validation. Never use batchmode against its project folder. Do not use screenshots or Computer Use with Unity while the user is awake.
- The user executes the Play Test. Record compilation, EditMode results, Console counts, FoundationScene validator, Play Mode evidence, save/load, and user acceptance separately.
- Do not commit, merge, tag, or push before the user accepts the Play Test. After acceptance, create the SC-013 checkpoint, fast-forward/push `main`, push the accepted demo tag, and remove the temporary feature ref only after ancestry and WIP checks.

## Review Focus

- **Early retirement or skipped events:** M2 retirement stays unavailable until that generation's mandatory event history is committed; pending events always block handoff. Cover in `M2StoryGateTests` and `GameSessionSuccessionTests`.
- **Wrong or skipped heir:** Each M2 head exposes only the approved next direct descendant; the second handoff reaches 信正. Cover in `M2FixedScenarioTests` and succession panel tests.
- **Unreachable cost option:** Both options in the SC-013 cost event are selectable from the intended opening state; existing SC-011 option requirements stay unchanged. Cover in `M2EventFlowTests` and `SC011EventContentTests` regression assertions.
- **Collapsed endings:** `seek-aid` is available only at Iori relationship `>=5`, and the two finale choices render distinct results. Cover in `M2EventFlowTests` and ending panel tests.
- **Save/load replay or reset:** Loading any stable M2 decision point restores pending choice, two-generation progress, family roster, and relationship history without duplicate effects. Cover in `M2ScenarioSaveTests`.

---

### Task 1: GitHub prior-art check (Scout)

**Files:**
- Report: `docs/research/SC013-fixed-scenario-prior-art.md`

**Interfaces:**
- Consumes: the approved SC-013 spec and existing project code patterns.
- Produces: a concise shortlist of at most three repositories, with license/compatibility/maintenance/dependency/effort comparison and a `Reuse`, `Adapt`, or `Build` decision for each.

- [x] Search GitHub for up to three relevant fixed-scenario/event-gating/family-roster projects; prefer Unity 6-compatible primary project documentation.
- [x] Record the report. Do not download repositories, install dependencies, or copy external code/assets.
- [x] Confirm the implementation will build from existing project code unless the user separately approves external reuse.

**Acceptance:** Research report is present and covers the required comparison fields; no package, checkout, or external source code was added.

### Task 2: Fixed M2 family and scenario progression rules

**Files:**
- Create: `Assets/Scripts/Application/M2FixedScenario.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Modify: `Assets/Scripts/Domain/SuccessionState.cs` only if a small scenario-specific read-only view is needed; do not change `SuccessionSample.CreateStandard()` semantics.
- Test: `Assets/Tests/EditMode/M2FixedScenarioTests.cs`
- Test: `Assets/Tests/EditMode/M2StoryGateTests.cs`
- Test: `Assets/Tests/EditMode/SuccessionServiceTests.cs`
- Test: `Assets/Tests/EditMode/GameSessionSuccessionTests.cs`

**Interfaces:**
- Produces `M2FixedScenario.CreateSuccession() -> SuccessionState`, `M2FixedScenario.IsActive(SuccessionState) -> bool`, `M2FixedScenario.ExpectedHeir(SuccessionState) -> PersonId`, and read-only progression queries consumed by `GameSession` and presentation.
- `GameSession.InitNewGame()` uses the M2 factory. Add `GameSession.InitStandardDemo()` for the separate SC-010 sample entry. Existing SC-010 tests/entry points may continue calling `SuccessionSample.CreateStandard()` and `StartCrisisDemo()`.
- `M2FixedScenario.CanBeginRetirement(SuccessionState, EventRuntimeState, out string reason) -> bool` derives M2 progress from stable family ID, committed event history, succession count, and pending-event state. `GameSession.CanBeginSuccessionRetirement` and `SuccessionRetirementBlockReason` expose the authoritative gate/reason; `SuccessionPanel` is not the only enforcement point. After the finale choice is committed, normal succession rules resume.

- [x] Write `M2FixedScenarioTests.CreateSuccession_HasApprovedSixMembersAndDirectLine`; assert stable IDs `family.akechi.sc013`/`person.sc013.*`, six people and roles (晴信 Head, 千恵 Spouse, 正綱/信正/時姫 kin, 佐近 Retainer), date `1560-04-01`, ages 晴信 70/千恵 66/正綱 46/信正 22/時姫 44/佐近 40, direct line 晴信→正綱→信正, and heir 正綱.
- [x] Write `M2StoryGateTests.CannotRetireBeforeRequiredChoicesOrWithPendingEvent` and `AllowsOnlyTheNextFixedHandoff`; assert early/pending attempts fail with a non-empty reason, handoff one targets 正綱, handoff two targets 信正, invalid fixed heirs block handoff with visible feedback instead of falling back to 旁系, and event choices cannot be skipped.
- [x] Extend `SuccessionServiceTests` regressions to assert `CreateStandard()` and `CreateCrisis()` roster IDs/rules and all existing succession behavior remain unchanged.
- [x] Implement the fixed roster and scenario progression queries without adding generic genealogy or heir-editing systems.
- [ ] Run the focused EditMode tests and confirm the exact M2 gates and SC-010 regressions pass.

**Acceptance:** M2 always reaches the intended three direct generations through `SuccessionService`; premature/duplicate handoffs fail with visible feedback and cannot make the script permanently miss its events.

### Task 3: SC-013 event chain, relationship cost, and endings

**Files:**
- Modify: `Assets/Resources/Events/event-catalog-v1.json` (append SC-013-only definitions; keep SC-011 entries/content version unchanged unless the file format requires an additive version decision, documented first).
- Modify: `Assets/Resources/Localization/event-text-zh-v1.json` (new SC-013 text IDs only).
- Test: `Assets/Tests/EditMode/M2EventFlowTests.cs`
- Test: `Assets/Tests/EditMode/SC011EventContentTests.cs`

**Interfaces:**
- Stable IDs are `family.akechi.sc013`; people are `person.sc013.harunobu`, `person.sc013.chie`, `person.sc013.masatsuna`, `person.sc013.nobumasa`, `person.sc013.tokihime`, and `person.sc013.sakon`. Use `event.sc013.inheritance-pledge`, `option.sc013.inheritance-pledge.keep-promise`, `option.sc013.inheritance-pledge.break-promise`, and `event.sc013.finale`; other event/option/chain/summary IDs use the `event.sc013.*`, `option.sc013.*`, `chain.sc013.*`, and `summary.sc013.*` prefixes.
- Opening M2 cost event resolves before the first handoff; the existing SC-011 rainy-arrival event and generation-two legacy variant retain their accepted data and behavior.
- The succession-related first-generation event uses two always-available choices targeting the existing `CharacterId.KurodaIori`: `keep-promise` applies `+5`, `break-promise` applies `-5`. It is the highest-priority first-month M2 event; the unchanged SC-011 rainy-arrival root follows on the next eligible month. Both must be committed before retirement is enabled.
- The third-generation finale is offered after settled handoff two. Its `seek-aid` option requires Iori relationship `>= 5`; `stand-alone` is always available. Each option has distinct ending summary text. A saved pending finale allows both endings to be compared by load/replay.

- [x] Write `M2EventFlowTests.InheritancePledge_OffersBothChoicesAndRecordsExactDeltaOnce`; assert `keep-promise` changes Kuroda Iori `0→5`, `break-promise` changes `0→-5`, both are selectable at opening, and chronicle/history records one choice and one delta.
- [x] Write `M2EventFlowTests.FixedScript_ResolvesPledgeRainyArrivalLegacyThenTwoHandoffs`; assert event order, head IDs 晴信→正綱→信正, and no pending event is dropped at a handoff.
- [x] Write `M2EventFlowTests.Finale_RequiresTwoHandoffsAndGatesAidAtFive`; assert no finale before handoff two, `seek-aid` is available at relationship `5`, unavailable at `4`, and its ending summary differs from always-available `stand-alone`.
- [x] Implement only the SC-013 event rows and localized text using existing condition/effect kinds; keep content version `2` and all accepted SC-011 entries/IDs/requirements/effects stable.
- [ ] Validate the complete catalog/localization references and run focused event/content EditMode tests.

**Acceptance:** The fixed chain is completable in order, records each real relationship change once, produces distinct endings, and the SC-011 contract tests remain green.

### Task 4: Scenario entry, family roster, and succession feedback UI

**Files:**
- Create: `Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Modify: `Assets/Scripts/Presentation/SuccessionPanel.cs`
- Create: `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`
- Create: `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scenes/FoundationScene.unity` via the transactional Editor generator only.
- Test: `Assets/Tests/EditMode/FamilyRosterPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs`
- Test: `Assets/Tests/EditMode/TitleScreenControllerTests.cs`
- Test: `Assets/Tests/EditMode/AddM6TitleScreenTests.cs` only if the existing generator contract needs a regression assertion.

**Interfaces:**
- `FamilyRosterPanel` reads `GameSession.Succession.World.Family.Members`, person/life data, `KinshipGraph`, and `DesignatedHeirId`; it never mutates game state.
- Title screen keeps `StartNewGame()` as M2 and adds independent SC-010 `StartStandardSample()` and `StartCrisisDemo()` entries; the latter initializes standard state and then starts the existing crisis demo.
- `SuccessionPanel` renders `CanBeginSuccessionRetirement` and its reason, and displays the current fixed heir. It retains existing SC-010 crisis controls.
- Both generators follow existing patterns: one Undo group each, validate-before-save, rollback Undo on exception without reloading, and fixed direct-child roots (`FamilyRosterRoot` under Canvas; scenario buttons under the existing title root). Validator checks cardinality, references, layer order, and required visible labels.

- [x] Write `FamilyRosterPanelTests.Refresh_ShowsSixIdentitiesRolesAgesAndKinship`; assert six stable rows, all approved names and exact ages, three generation labels, kinship descriptions, and visible current heir.
- [x] Write `TitleScreenControllerTests.StartsM2StandardAndCrisisEntriesIndependently`; assert new game uses `M2FixedScenario`, standard sample uses `SuccessionSample.CreateStandard()`, and crisis entry produces `SuccessionSample.CreateCrisis()` without residual M2 data.
- [x] Extend `AddSC010SuccessionPanelTests` with early-disabled/visible-reason and post-choice-enabled/heir-label assertions for M2; preserve the existing SC-010 five-button binding assertions.
- [x] Implement the roster/title/panel bindings; implement transactional generators for the two `FoundationScene` UI additions and extend the validator.
- [ ] Run focused EditMode tests, invoke the existing validator in the open Unity Editor, inspect its text report and save the scene only after validation succeeds.

**Acceptance:** The player can identify all six people and the next heir in-game, understands why retirement is blocked/enabled, and can select M2, SC-010 standard sample, or SC-010 crisis demo from the title screen.

### Task 5: Save/load, ending summary, documentation, and Play Test handoff

**Files:**
- Modify: `Assets/Scripts/Presentation/DemoSummaryPanel.cs` or create `Assets/Scripts/Presentation/M2EndingPanel.cs` under the same fixed UI root.
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`.
- Test: `Assets/Tests/EditMode/M2ScenarioSaveTests.cs`
- Test: `Assets/Tests/EditMode/M2EndingPanelTests.cs`
- Create: `SC013_DEMO.md`.
- Update: `PROJECT_STATE.md`, `ARCHITECTURE.md`, `DECISIONS.md`, and the approved SC-013 spec/plan.
- Modify: `docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md` to keep the approval/evidence state current; do not change the approved behavior baseline without returning to the user.

**Interfaces:**
- End state is derived from two settled successions and committed finale history; no terminal save field is added. Play can continue after the ending.
- Existing persistence DTOs remain the source of truth for roster/kinship, pending event, event history, relationship values, and chronicle.
- `SC013_DEMO.md` owns exact user Play Test steps and a text-only results checklist.

- [x] Write `M2ScenarioSaveTests.Load_RestoresEachStableM2DecisionPointWithoutDuplicateEffects`; cover pending first event, SC-011 root/follow-up, pending handoff, pending finale, and post-finale; assert family/kinship, head/heir/count, pending event order, relationship, history, and chronicle equality after reload.
- [x] Write `M2EndingPanelTests.FinalChoices_ShowDistinctSummariesAndAllowFurtherPlay`; assert both summaries render and time advance remains available after the ending.
- [x] Update project docs with changed files, scenario entry, stable IDs, save/load scope, known limits, and evidence fields; note that user Play Test remains pending.
- [x] Write exact Play Test steps: choose M2 new game; verify the six-person roster and heir; save before the first cost decision; choose `break-promise` (-5) to record the cost; choose SC-011 `offer-rest` (+5); perform the visible first handoff; choose the honored-legacy `renew` response (+5); perform the visible second handoff; at the pending finale save, choose `seek-aid`, inspect its ending and chronicle, reload, choose `stand-alone`, and compare the result; report any divergence.
- [x] Review final Git diff/status and confirm user WIP and `SampleScene.unity` were not staged or overwritten.
- [ ] Verify compilation, EditMode totals, Console error/warning counts, FoundationScene validator, and Play Mode evidence in separate fields. Give the text-only handoff to the user and wait for the user's Play Test result.

**Acceptance:** Stable-point save/load preserves the complete M2 script without duplicate effects; docs and manual steps match the shipped UI; Play Test status stays pending until the user runs it.

## Execution / Verification Boundary

Implementation and review use supervised Orca tasks on the existing project workspace, sequentially, with OpenCode2 as writer and a separate OpenCode2 read-only reviewer. The user has already specified this execution method. No Unity screenshot or Computer Use interaction is part of the workflow. The branch remains frozen while the user evaluates the demo. Only after explicit user acceptance may the lead create/push the checkpoint, integrate to `main`, push the accepted tag, and clean the temporary branch after ancestry/WIP checks.


---

<a id="2026-11-03-sc-014-refugee-legacy"></a>
## 来源：docs/superpowers/plans/2026-11-03-sc-014-refugee-legacy.md

# SC-014 实现计划：流亡少年与家系遗产

> **给 OpenCode2：** 按顺序执行本计划。每个实现任务只拥有下列明确文件；不得重置、格式化或顺手清理其他脏文件。

**目标：** 在已有 SC-013 M2 固定剧本中加入 GDD 7.4 第二条跨代事件链，记录第一代救助选择在第二、三代的关系后果，并更新项目状态文档。本轮不 Play Test、不碰 Unity 场景。

**设计：** [SC-014 设计稿](design-and-planning-history.md#2026-11-03-sc-014-refugee-legacy-design)

**范围依据：** GDD v4.0 §7.4、§12.2/12.3、§19.6；复用 SC-013 的 M2 事件门控和 SC-012 的编年史，不引入新的存档状态。

## 文件所有权

实现 worker 可编辑：

- `Assets/Resources/Events/event-catalog-v1.json`
- `Assets/Resources/Localization/event-text-zh-v1.json`
- `PROJECT_STATE.md`（只在当前 SC-013 状态记录后附加 SC-014 状态，不重排或重写其他历史）
- `ARCHITECTURE.md`（仅在已有事件目录/跨代链说明处追加 SC-014 数据链信息）
- `DECISIONS.md`（只追加本设计选择与兼容性记录）
- `SC014_DEMO.md`（仅记录实现状态、验证边界和之后用户可执行的文本步骤；不标记任何测试已通过）

实现 worker 不得编辑：场景、预制体、`ProjectSettings`、SC-011/SC-013 已有事件行、既有测试文件、其他 WIP 文档、`Library`、`Temp`。本轮不新增或运行测试；不启动 Unity，不使用截图或 Computer Use。

## 步骤

1. 精读现有事件目录、SC-013 场景 ID 规则、事件历史条件和本设计稿；不修改工作区中其它 SC-013/WIP 内容。
2. 只向两个 JSON 资源追加 `chain.sc013.refugee-legacy` 与四个事件节点：第一代入口、第二代忠诚选择、第三代盟友结局、第三代竞争结局。复用现有 `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`、`RelationshipAtLeast` 与 `RelationshipDelta` 规则；保留版本 `2` 和其他事件定义的字节/语义内容。
3. 按设计稿填写每个选项的中文标题、描述、结果摘要与关系数值。所有关系目标使用 `npc.kuroda.iori`；选项的效果与可用性清楚，不依赖隐式标记或新存档字段。
4. 仅追加本功能的状态、架构、决策记录及 `SC014_DEMO.md`。Demo 指南要明确这是交给用户稍后执行的文本手册，当前没有 Play Test 或 Unity 证据。
5. 交付变更摘要、修改文件、实现约束处理情况，以及明确未运行的验证项；不得自行提交、推送、合并、标记验收。

## 审阅任务（实现完成后顺序派发给独立 OpenCode2 worker）

- 只读检查允许范围内 diff；确认 ID 唯一、所有文本 ID 存在、链成员与 `EventOptionChosen` 源引用吻合，分支互斥，第二/第三代交接门槛正确，关系影响真实且可解释。
- 对比 `HEAD` 中 SC-011 与 SC-013 的既有 JSON 事件和文本条目，确认保持不变。
- 不运行 Unity、测试、Play Mode，不编辑文件。输出 `ACCEPT` 或 `RETURN`、问题位置及其证据；审阅报告写入 `docs/reports/sc014-review.md`。
- 如修正审阅发现后需要复审，保留首轮报告，并将第二轮结果另存为 `docs/reports/sc014-review-2.md`。

## 验收条件

- 四个新增节点组成一条连贯的跨三代事件链；前置条件精确地延续前代选项，第三代两条结果互斥。
- 只修改文件所有权段列出的文件；没有 Unity 场景、设置、存档 schema 或 package 改动。
- SC-011/SC-013 已有内容不改写。
- 独立 OpenCode2 静态审阅为 `ACCEPT`，或修正并复审后为 `ACCEPT`。
- Play Test、Unity 编译/EditMode/Console/Play Mode 均未声称通过；保留用户后续验证与验收权。


---

<a id="2026-11-03-sc-015-three-generation-family-letters"></a>
## 来源：docs/superpowers/plans/2026-11-03-sc-015-three-generation-family-letters.md

# SC-015 三代家书实现计划

> **给 OpenCode2：** 仅按下列文件清单编辑。当前工作区包含多项既有 SC-013/014 与用户 WIP；不要重置、整理、格式化或覆盖它们。

**目标：** 为现有 SC-013 固定 M2 剧本增加一条由家族历史选择塑造的三代家书事件链。

**架构：** 只追加数据驱动事件和中文文本，依靠现有事件选择历史与两次继承条件连接三代。选择不更改关系或任务数值；不新增任何代码或存档状态。

**技术栈：** 现有 Unity Resources JSON 事件目录与中文文本目录；无新依赖。

**设计稿：** [SC-015 三代家书设计稿](design-and-planning-history.md#2026-11-03-sc-015-three-generation-family-letters-design)

## 全局约束

- 既有 Unity 项目 `C:\Users\panye\My project`；不新建项目。
- 事件 ID 使用 `event.sc013.*`、`option.sc013.*`、`chain.sc013.*`，事件 `scenario` 为 `sc013`，`contentVersion` 保持 `2`。
- 只使用 `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`；所有新增效果数组为空。
- 不改既有事件/文本，不改场景、预制体、ProjectSettings、测试、存档 schema、包、`Library` 或 `Temp`。
- 不新增或运行测试，不操作 Unity，不提交/推送/合并/打标签。

## 文件所有权

实现 worker 可编辑：

- `Assets/Resources/Events/event-catalog-v1.json`：只追加 SC-015 的链声明和 7 个事件。
- `Assets/Resources/Localization/event-text-zh-v1.json`：只追加 SC-015 的事件、选项和摘要文本。
- `PROJECT_STATE.md`：只追加 SC-015 状态行/小节，不改写旧记录。
- `ARCHITECTURE.md`：仅追加现有事件数据说明中的 SC-015 分支与零数值效果。
- `DECISIONS.md`：仅追加本链选择历史作为后果载体、无关系/存档变化的决策。
- `SC015_DEMO.md`：交付说明、明确验证边界及之后的文本试玩步骤；不得把未执行项目写成通过。

实现 worker 不得编辑其他文件。设计稿和本计划由协调方维护。

## 步骤

1. 阅读 SC-015 设计稿、事件目录现有 schema、SC-013/014 节点与 SC-013/014 文本；确认工作区中其他差异保持不动。
2. 向事件目录追加 `chain.sc013.family-letters` 和以下七个事件。事件优先级按现有 `EventEngine` 的**从高到低**顺序设置，使 SC-015 先于同代 SC-014、但后于 SC-013 必须事件：第一代 `6`、第二代 `60`、第三代 `600`。
   - `event.sc013.family-letters.first`：首次交接前，两个选项分别公开家书与封存家书。
   - `event.sc013.family-letters.second-open`：公开路径，至少一次已结算交接；两个选项分别保留完整细节与整理简史。
   - `event.sc013.family-letters.second-sealed`：封存路径，至少一次已结算交接；两个选项分别由正綱拆阅并完整记录、继续封存。
   - `event.sc013.family-letters.third-full-public`、`...third-short-history`、`...third-opened-by-heir`、`...third-sealed`：均要求至少两次已结算交接，各自唯一引用产生该结尾的第二代事件与选项。
3. 所有事件设 `scenario: "sc013"`、`trigger: "Monthly"`、`onceScope: "PerGame"`、`cooldownMonths: 0`；沿用目录现有合法权重/结构。每个分支条件为相应 `EventOptionChosen` 加世代门槛，入口另加 `SuccessionCountEquals 0`。所有 options 使用现有 `Always` requirement、摘要文本引用和空 `effects`。
4. 为每个新增 `textId` 编写符合家书/亲情主题的简体中文标题、描述与结果摘要。文本应准确说明由晴信、千恵、正綱、信正何人作出/承接决定；不声称数值或人物状态变化。
5. 仅更新本计划所列文档。试玩手册包含入口、各分支逐步点击顺序、预期事件、失败条件、结尾核对与纯文本结果清单；注明当前尚未在 Unity 执行。
6. 做一次性静态检查：两个 JSON 可解析；所有 ID 唯一；每个新增 `textId`、`optionId`、摘要引用均存在；链成员完整；所有源事件/选项引用确实存在；三个世代门槛正确；每个事件的 effects 为空；SC-011/SC-013/SC-014 原事件/文本与实现前完全相同。记录检查方法与结果。不要运行 Unity 或测试。
7. 报告仅限 SC-015 的改动、静态检查结果、未执行验证项及任何差异；不提交/推送/合并/打标签。

## 审阅任务

实现完成后，顺序派发一名独立 OpenCode2 worker 只读审阅本计划文件清单内的 SC-015 diff，并将结果写入 `docs/reports/sc015-review.md`。复核七个节点的 ID、所有文本引用、分支可达性、源选项唯一性、一次性结算、继承门槛、空效果声明及 SC-011/013/014 的原内容未改。不得运行 Unity/测试或编辑功能文件。输出 `ACCEPT` 或 `RETURN`；若修正则另派同一或新的只读复审并保留报告。

## 验收条件

- 七个节点构成完整且互斥的三代家史分支；第一代入口、第二代回收、第三代终点的门槛准确。
- 仅修改“文件所有权”列出的功能文件，外加独立审阅报告；不影响 Unity 场景与项目配置。
- 两个资源可解析、引用与唯一性静态检查无错，独立审阅为 `ACCEPT`。
- 对 Unity 编译、EditMode、Console、场景验证、Play Mode、存读档及用户 Play Test 均保持“未执行/未验证”。


---

<a id="2026-11-03-sc-016-m2-commission-legacy"></a>
## 来源：docs/superpowers/plans/2026-11-03-sc-016-m2-commission-legacy.md

# SC-016 M2 委托旧约 Implementation Plan

> **For agentic workers:** Implement through the supervised Orca OpenCode2 worker required by this project; a separate OpenCode2 worker performs the read-only review after implementation.

**Goal:** Add the fifth GDD-aligned M2 cross-generation chain, recording the relationship consequences of a ledger-delivery commission from Harunobu's generation to Nobumasa's.

**Architecture:** Extend only the current JSON event catalog and localization data. Reuse existing event history, generation gates, relationship deltas, and chronicle recording; add no runtime code or save state.

**Tech Stack:** Unity 6.6 project; JSON event catalog and Chinese localization.

**Spec:** [SC-016 design](design-and-planning-history.md#2026-11-03-sc-016-m2-commission-legacy-design)

## Global Constraints

- Stay in `C:\Users\panye\My project`; do not create another Unity project.
- OpenCode2 owns code/data implementation; the lead owns Editor integration and Play Mode.
- Preserve all existing user worktree changes and `.meta`/GUIDs; do not touch `Library` or `Temp`.
- Do not add packages, assets, scenes, prefabs, `ProjectSettings`, code, or save fields.
- Do not change accepted SC-011/SC-013/SC-014/SC-015 event or localization rows.
- Do not commit, push, merge, or tag before explicit user acceptance.

## Review Focus

- Duplicate IDs: static validation must assert global uniqueness for all new event and option IDs.
- Missing localized strings: every new `textId`, summary ID, and option ID must resolve.
- Early or late branch availability: verify source-option gates and settled-succession thresholds for each generation.
- Wrong relationship effects: verify each of the three decision nodes (six options) targets `npc.sakai.jirobei` with the specified `+5/-5`, and the four endings have no effects.
- Schedule regression: verify priorities `8/80/800` against existing SC-011/013/014/015 order, and compare existing event/localization rows unchanged.

## Files and Ownership

- 修改 `Assets/Resources/Events/event-catalog-v1.json`：只追加新链与 7 个事件。
- 修改 `Assets/Resources/Localization/event-text-zh-v1.json`：只追加 SC-016 所需标题、正文、选项和摘要文本。
- 修改 `PROJECT_STATE.md`、`ARCHITECTURE.md`、`DECISIONS.md`：只追加 SC-016 的状态/数据约定，不改写既有历史。
- 新建 `SC016_DEMO.md`：写明 Play Mode 前置、四条路线逐步操作、预期屏幕变化、失败条件和纯文本结果清单。
- 新建 `docs/reports/sc016-implementation.md`：记录实际改动和仅已完成的静态检查。
- 不编辑其他文件。

## Do Not Edit

- 不改任何 C#、测试、场景、Prefab、`ProjectSettings`、存档 schema、包或资产。
- 不改写 SC-011/013/014/015 的现有 JSON 行或本地化文本；不得覆盖已有用户工作区改动。
- 不运行 Unity、测试、场景 updater 或保存场景；本轮实现 worker 仅负责内容和文档，Play Mode 由协调者在用户授权下执行。
- 不提交、推送、合并或打标签。

## Task 1: Add the Commission Chain and Handoff Documentation

- [x] Read the spec, the current JSON schema, and the specified file list; confirm unrelated dirty files remain untouched.
- [x] Append `chain.sc013.commission-legacy` and seven events: the first-generation commission, two mutually exclusive second-generation nodes, and four mutually exclusive third-generation endings.
- [x] Use existing `EventOptionChosen`, succession-count conditions, and `RelationshipDelta`. Apply only the six specified `+5/-5` deltas (two per decision event) to `npc.sakai.jirobei`; ending events have empty effects.
- [x] Set priorities to `8/80/800`. Confirm required M2 events and existing SC-011 ordering remain ahead, while SC-016 remains ahead of SC-015 and SC-014 within each generation.
- [x] Add localized strings and the files listed above. The handoff states the chain is optional and that missing its first-generation event does not block succession.
- [x] Parse both JSON documents and check global ID uniqueness, localized references, chain membership, source-option uniqueness, generation gates, relationship targets/effects, mutually exclusive endings, and priority ordering.
- [x] Compare the existing SC-011/013/014/015 event and localization rows against the pre-edit snapshot; they are unchanged.
- [x] Record static-check and subsequent lead verification results in `docs/reports/sc016-implementation.md`; keep Unity, tests, validator, Play Mode, save/load, and user acceptance as separate evidence for the lead.

**Acceptance:** Seven nodes form one reachable three-generation chain with four endings; the chronicle records the actual relationship deltas; the data implementation changes only the listed JSON and documentation files; static checks pass; runtime claims remain separate for the lead to verify. The later test-fixture-only adjustment is recorded below.

## Lead Verification Results (2026-11-02, local time)

- The existing Unity 6.6.6f1 Editor compiled/refreshed scripts, then its EditMode Test Runner passed 778/778 tests, with 0 failures and 0 skipped/inconclusive cases. `TestResults.xml` records the exact result; this is Editor evidence, not user acceptance.
- Under the user's temporary authorization, the lead ran four new SC-013 M2 sessions for the four SC-016 routes. The observed endings were “夜路仍然安全”, “账册见了天日”, “旧账已清”, and “商路不再相通”.
- `FoundationSceneValidator`, save/load verification, and formal user acceptance remain outstanding. No checkpoint, push, merge, or tag was created.


---

<a id="2026-11-03-sc-017-generic-family-tree"></a>
## 来源：docs/superpowers/plans/2026-11-03-sc-017-generic-family-tree.md

# SC-017 通用家谱实现计划

> **执行要求：** 按 Orca 监督流程交由 OpenCode2 实现，并在实现完成后安排独立只读复审。本计划获用户确认前不开始编码。

**目标：** 将固定六行家族名册改为只读、数据驱动、可滚动的通用家谱展示，读取任意有效家族成员和亲属关系数据。

**架构：** 纯数据构建器从 `SuccessionState` / `SuccessionWorld` 生成节点和关系快照；确定性布局器在不依赖 SC-013 人名、ID、数量的前提下计算位置；`FamilyRosterPanel` 在现有 uGUI 面板中绘制节点、关系线和滚动内容。家臣等非亲属成员显示在独立区域，本轮不提供关系编辑。

**技术：** Unity 6.6.6f1、C#、现有 uGUI、现有 Unity EditMode Test Runner；不新增 package 或场景。

**设计稿：** `docs/superpowers/specs/2026-11-03-sc-017-family-tree-roster-design.md`

## 全局约束

- 所有代码由 OpenCode2 在现有 `feature/sc013-fixed-three-generation` worktree 实现。
- 保留所有已有修改和未跟踪文件；不 reset、clean 或覆盖无关工作。
- 复用 `FoundationScene`，不新建项目/场景；保留 `.meta` 与 GUID；不碰 `Library` / `Temp`。
- 名册不修改家族成员或结构性亲属关系；不改存档格式、GDD 规则、SC-010/SC-011 或 M2 剧本行为。
- 只使用已打开的 Unity Editor 验证；不得对其项目路径运行 batchmode。醒着时不使用截图或 Computer Use 操作 Unity；Play Test 由用户执行。
- 编译、EditMode、Console、场景验证、Play Mode、用户验收分别记录。用户验收前不得提交、合并或打 tag。

## 复审重点与对应测试

1. 有效家族成员没有亲属边时仍显示为无连线成员：图构建/面板测试。
2. 家臣、弟子、客将等非亲属成员不得画入亲缘关系：图构建测试。
3. 多父母、多配偶、手足和收养关系不重复节点、不丢失关系：图构建/布局测试。
4. 超过六人、三代的图保持确定性并可滚动：布局/面板测试。
5. 缺少可选年龄或关系端点已不在成员列表时不崩溃且不隐藏有效成员：图构建测试。

## 文件职责

- 新建 `Assets/Scripts/Presentation/FamilyTreeGraph.cs`：只读图快照、节点/关系数据与构建器。
- 新建 `Assets/Scripts/Presentation/FamilyTreeLayout.cs`：与 SC-013 身份及输入顺序无关的确定性布局。
- 修改 `Assets/Scripts/Presentation/FamilyRosterPanel.cs`：消费图快照和布局，渲染关系线、人物卡、滚动内容、无连线亲属与家臣区域，保留原开关和只读行为。
- 修改 `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`：通过现有事务式 updater 建立 viewport、ScrollRect、内容/连线/节点层与模板并扩展验证。
- 修改 `Assets/Scripts/Editor/FoundationSceneValidator.cs`：检查通用家谱层级和绑定。
- 新建 `Assets/Tests/EditMode/FamilyTreeGraphTests.cs` 与 `FamilyTreeLayoutTests.cs`；扩展 `FamilyRosterPanelTests.cs`、`AddSC013FamilyRosterPanelTests.cs` 及适用的 scene validator 测试。
- 实现及验证后更新 `SC017_DEMO.md`、`PROJECT_STATE.md`、`DECISIONS.md`，新增简明 `docs/reports/sc017-implementation.md`。

## 有序任务

### 任务 1：实现前复用调研（只读）

**负责人：** Scout，通过 Orca。禁止代码和 package 变更。

- [ ] 先检查本地 `FamilyRosterPanel`、updater 与领域图，确认哪些能力可直接复用。
- [ ] GitHub 最多搜索 3 个 Unity 6 兼容的家谱/图布局项目；优先读 README 与相关文档。
- [ ] 报告 URL、许可证/商业使用、Unity/URP 兼容性、活跃情况、依赖/安全、改造估算、Reuse/Adapt/Build 结论。
- [ ] 不下载/克隆仓库、不安装 package、不复制代码或素材。Mochi 在 OpenCode2 开发前记录复用/自建决策。

### 任务 2：数据图快照与布局

**文件：** 新建 `FamilyTreeGraph.cs`、`FamilyTreeLayout.cs` 及对应测试。

**接口：**
- `FamilyTreeGraphBuilder.Build(SuccessionState state, GameDate date) -> FamilyTreeGraphSnapshot`
- 快照提供按稳定规则排序的只读 `FamilyTreePersonNode`、`FamilyTreeRelationEdge` 集合。
- `FamilyTreeLayout.Calculate(FamilyTreeGraphSnapshot graph) -> FamilyTreeLayoutResult`；结果包含每个节点位置及内容宽高。
- 节点包含 `PersonId`、姓名、可用年龄、`FamilyMemberRole`、当前家主/有效指定继承人标记、亲属/家臣分类。
- 关系边包含两端 ID 与现有 `KinshipType`，不创建或修改关系事实。

- [ ] 写 `Build_IncludesSc013MembersAndExactKinshipEdges`、`Build_KeepsUnconnectedKinVisible`、`Build_SeparatesRetainersDisciplesAndGuestsFromKinshipEdges` 失败测试，断言 SC-013 精确关系、孤立亲属可见，非亲属成员保留但无亲缘边。
- [ ] 写 `Calculate_LaysOutMoreThanSixMembersAndThreeGenerations`、`Calculate_IsDeterministicWhenInputOrderChanges`、`Calculate_PreservesCollateralSpouseAndAdoptionEdges`、`Calculate_ReturnsUniquePositionsAndFiniteBounds` 失败测试，断言多代分支、输入顺序稳定、位置不重叠且内容边界可滚动。
- [ ] 通过现有 Editor Test Runner 运行聚焦测试，确认新能力尚未实现时失败。
- [ ] 使用 `Family.Members`、`World.Persons`/`TryGetLife`、`KinshipGraph.Links`、家主与继承人 ID、当前日期实现快照与布局；只忽略端点不在成员快照的边，不丢有效成员。
- [ ] 重跑聚焦测试，确认通过。

### 任务 3：面板滚动与通用绘制

**文件：** 修改 `FamilyRosterPanel.cs`，扩展 `FamilyRosterPanelTests.cs`。

- [ ] 写 `Refresh_RendersSc013KinshipEdgesAndSeparateRetainerSection`、`Refresh_ShowsUnconnectedKinAndCurrentHeadAndHeir` 失败测试，断言节点、关系线、标记与家臣分区。
- [ ] 写 `Refresh_RendersLargeRosterInsideScrollableContent`、`CloseAndReopen_DoesNotDuplicateNodesOrEdges` 测试，断言所有节点可滚动查看且反复开关/刷新不重复生成。
- [ ] 写 `OpenScrollRefreshAndClose_DoNotMutateSuccessionOrKinship`、`SessionStateChanged_RefreshesHeadAndHeirMarkers` 测试，断言面板操作不改变状态、继承后标记更新。
- [ ] 将固定 `M2RosterRow` 行遍历替换为图快照与布局结果；在 viewport 内实例化人物卡和关系线，家臣/附属成员置于独立区。
- [ ] 用现有 Editor Test Runner 验证刷新确定性及禁用时清理。

### 任务 4：现有场景生成器与验证器接入

**文件：** 修改 `AddSC013FamilyRosterPanel.cs`、`FoundationSceneValidator.cs` 并扩展测试。

- [ ] 写 `Updater_CreatesBoundScrollableGraphLayers`、`Updater_IsIdempotentAndRollsBackInvalidHierarchy`、`Validate_RejectsMissingOrDuplicateGraphBindings` 失败测试，断言滚动结构和引用、单根、幂等及回滚。
- [ ] 事务式 updater 在现有固定根下建立通用滚动层级；遵循一个 Undo group、保存前校验、异常时 Undo 回滚，回滚不重载场景。
- [ ] validator 检查层级、绑定、viewport/content 设置、打开/关闭控件；不得依赖固定角色名或人数。
- [ ] 在现有 Unity Editor 验证 updater 幂等、回滚、preview scene 隔离和 FoundationScene validator。`FoundationScene.unity` 已有工作区修改；保存前必须证明不会覆盖原有内容，否则保留场景文件并汇报具体阻塞。

### 任务 5：文档、复审与用户试玩交接

- [ ] 更新 `SC017_DEMO.md`、`PROJECT_STATE.md`、`DECISIONS.md`，记录只读通用图、家臣区、未实现编辑操作及逐步 Play Test 指引。
- [ ] 记录 Scout 调研/自建结论、改动文件、只读复审问题、Unity 编译、EditMode 总数、Console 错误/警告、场景验证、Play Mode 和用户验收，彼此分开。
- [ ] OpenCode2 完成后，通过 Orca 顺序安排独立 OpenCode 只读复审；复审者不改文件，Mochi 逐项处理并签核。
- [ ] 向用户提供纯文字 Play Test 步骤并冻结 demo worktree。用户验收前不提交、不合并、不打 tag。

## 验证门禁

- Unity 测试与场景 validator 仅通过当前已打开 Editor 的 Test Runner 执行；检查 `%LOCALAPPDATA%\Unity\Editor\Editor.log` 与 `<project>\Logs\Editor.log` 的新鲜编译/测试证据。不得启动此项目的 batchmode。
- 静态检查使用 `git diff --check` 并逐项检查新增/修改文件；不做广泛清理。
- 可视布局、滚动、关系易读性、标准/危机场景回归及验收必须由用户 Play Test；自动化测试不能替代试玩。



---

<a id="2026-11-03-sc-018-independent-family-events"></a>
## 来源：docs/superpowers/plans/2026-11-03-sc-018-independent-family-events.md

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


---

<a id="2026-11-03-sc-019-family-industries"></a>
## 来源：docs/superpowers/plans/2026-11-03-sc-019-family-industries.md

# SC-019 实现计划：家族产业首切片（农庄与商馆）

> **For OpenCode2:** 本计划由协调方在后续实现 dispatch 中执行；当前文档任务只写本文件与设计稿，不实现代码。实现时按任务顺序执行，只改本计划“文件所有权”列出的路径，保留所有无关用户 WIP。

**Goal:** 在现有 Unity 项目中交付一个可完整游玩的 M3 首切片：两个初始产业（农庄、商馆）、三名资源（资金/物资/情报）、现有六人的人物任命、每月自动结算、空缺/短缺/失效处理、两次继承交接、v6 存档与迁移、现有 UI/编年史展示。

**Approach:** 纯 C# 领域模型负责产业定义、任命、月结与安全算术；`GameSession` 负责唯一月结入口、时间跨月防护、事务提交与继承协调；`SaveData`/`SaveDataMigrator`/`SaveDataValidator` 升到 v6 并保留 SC-018 家庭关系与全部历史；一个事务式 Editor 生成器在 `FoundationScene` 中增加 `IndustryRoot`/`IndustryPanel`。不引入外部代码、package、Unity 项目或动态市场。

**Acceptance:** 新局 30/12/0、两产业空缺；任命晴信/千恵后结束 1560-04 得 35/13/1；日推进与旅行不能跨月；结束月份唯一结算；空缺/短缺/失效/两次交接/保存读档/SC-018 迁移均有自动化测试；协调方可在用户当前授权下用 Computer Use 执行 Play Mode 并单独记录，用户验收另行记录。

**Design:** `docs/superpowers/specs/2026-11-03-sc-019-family-industries-design.md`

**Constraints:**

- `GDD_v4.0.md` 是设计权威；本切片不是完整 M3，不得把“两个产业 + 两次交接”报告为 M3 完成。
- 当前分支 `feature/sc018-independent-core-events` 含未提交 SC-018；不得覆盖、清理、提交或合并这些内容。SC-018 用户验收仍待完成。
- 不新增 Unity 项目、package、付费服务、外部代码或资产；不改 `ProjectSettings`、prefab、`Library`、`Temp`。
- 保留既有 `.meta` GUID；只有新增源文件/测试文件可配对新 `.meta`。
- 一个指定集成 worker 独占 `Assets/Scenes/FoundationScene.unity`、`Assets/Scripts/Editor/AddSC019IndustryPanel.cs` 和 `FoundationSceneValidator.cs` 的场景部分；其他 worker 不得并行写场景。
- 生成器使用一个 Undo group，先校验后保存；异常时 `Undo.RevertAllInCurrentGroup`，不重新加载场景。
- 当前文档任务不运行 Unity、不测试、不提交、不推送、不合并、不打 tag。后续实现使用用户已打开的 Editor；不得对同一项目启动 batchmode。
- 项目默认由用户执行 Play Test；当前用户已明确授权协调方/助手使用 Computer Use 在现有 Unity Editor 中执行 Play Mode。记录为“coordinator-run Play Mode”，与用户验收分开。不得把协调方 Play Mode 当作 user acceptance。
- 测试必须区分编译、EditMode、Console、Validator、Play Mode、存档/读档、用户验收。

## 文件所有权（后续实现 dispatch）

| 所有者 | 路径 | 说明 |
|---|---|---|
| 实现 worker（OpenCode2） | `Assets/Scripts/Domain/IndustryCatalog.cs`、`IndustryState.cs`、`IndustryCalendar.cs`、`IndustryService.cs`、`IndustrySettlement.cs` | 新领域模型 |
| 实现 worker | `Assets/Scripts/Application/GameSession.cs`、`IndustryView.cs`、`AssemblyInfo.cs` | 月结入口、任命 API、视图、内部测试 seam |
| 实现 worker | `Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs` | v6 schema、校验、迁移 |
| 实现 worker | `Assets/Scripts/Domain/FamilyChronicleEntry.cs`、`HistoryLedger.cs` | `Industry` 类别与月结/人事记录 |
| 实现 worker | `Assets/Scripts/Presentation/IndustryPanel.cs`、`WorldClockPanel.cs`、`WorldMapController.cs`、`FamilyChroniclePanel.cs` | 面板与跨月提示 |
| 实现 worker | `Assets/Tests/EditMode/Industry*.cs`、`SC019*.cs`、`AddSC019IndustryPanelTests.cs` 及 `.meta` | 新测试 |
| 实现 worker | `SC019_DEMO.md`、`PROJECT_STATE.md`、`docs/reports/sc019-implementation.md` | 试玩与证据 |
| 集成 worker（唯一） | `Assets/Scripts/Editor/AddSC019IndustryPanel.cs`、`FoundationSceneValidator.cs`、`Assets/Scenes/FoundationScene.unity` | 场景生成/验证/保存 |
| 协调方 | 本设计稿、本计划、最终签核 | 复审、路由、用户验收记录 |

`ProjectSettings` 预期零改动；若生成器意外需要，停止并报告，不擅自修改。

## Task 0 — 基线映射与契约确认

**Read:** `Assets/Scripts/Application/GameSession.cs`、`Assets/Scripts/Domain/WorldClock.cs`、`GameDate.cs`、`SuccessionState.cs`、`SuccessionService.cs`、`Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs`、`Assets/Scripts/Domain/HistoryLedger.cs`、`FamilyChronicleEntry.cs`、`Assets/Scripts/Presentation/FamilyChroniclePanel.cs`、`WorldClockPanel.cs`、`WorldMapController.cs`、`Assets/Scripts/Editor/AddSC012FamilyChroniclePanel.cs`、`AddSC013FamilyRosterPanel.cs`、`FoundationSceneValidator.cs`、`Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs`、`M2FixedScenario.cs`。

- [ ] 记录当前 schema v5、`CurrentRulesVersion = 1`、事件/编年史/继承字段。
- [ ] 记录 `AdvanceOneDay`、`AdvanceDays`、`PreviewTravel`、`ConfirmTravel`、`TryAdvanceToNextMonth` 的现有行为与失败通道。
- [ ] 用 `grep` 列出所有跨月 `AdvanceDays`/`AdvanceOneDay` 测试夹具；标出需要 internal seam 的调用。
- [ ] 记录 SC-018 `familyRelationships` 与 chronicle 的保存/迁移路径，作为 v6 回归基线。
- [ ] 记录现有 UI 根节点、面板绑定模式、生成器 Undo/Validate/Rollback 模式。
- [ ] 在 `docs/reports/sc019-implementation.md` 写基线快照；不改代码。

**Acceptance:** 基线记录覆盖所有时间入口、存档字段、UI 根与测试夹具；没有代码/场景改动。

## Task 1 — 产业目录、资源与任命状态

**Create:**

- `Assets/Scripts/Domain/IndustryCatalog.cs`
- `Assets/Scripts/Domain/IndustryCalendar.cs`
- `Assets/Scripts/Domain/IndustryState.cs`
- `Assets/Scripts/Domain/IndustryService.cs`

**Interfaces:**

```csharp
public readonly struct IndustryDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public int WorkUnitsRequired { get; }   // 3
    public int CapacityPosts { get; }       // 1
    public int UpkeepFunds { get; }         // farm 0, trading 6
    public int UpkeepSupplies { get; }      // farm 1, trading 2
    public int OutputFunds { get; }         // farm 2, trading 9
    public int OutputSupplies { get; }      // farm 4, trading 0
    public int OutputIntelligence { get; }  // farm 0, trading 1
    public bool MaintainsFromOutput { get; } // farm true, trading false
}

public static class IndustryCatalog
{
    public const string FarmId = "industry.farm";
    public const string TradingHouseId = "industry.trading-house";
    public static IReadOnlyList<IndustryDefinition> All { get; } // ordinal: farm, trading
    public static bool TryGet(string industryId, out IndustryDefinition definition);
}

public static class IndustryCalendar
{
    public static int MonthIndex(GameDate date); // year * 12 + month - 1
}

public readonly struct IndustryResources : IEquatable<IndustryResources>
{
    public const int Minimum = 0;
    public const int Maximum = 999999;
    public int Funds { get; }
    public int Supplies { get; }
    public int Intelligence { get; }
    public static bool TryCreate(int funds, int supplies, int intelligence,
        out IndustryResources resources, out string error);
}

public sealed class IndustryState
{
    public IndustryResources Resources { get; }
    public int LastSettledMonthIndex { get; }
    public IReadOnlyList<IndustryAssignment> Assignments { get; } // sorted by IndustryId
    public static IndustryState CreateInitial(GameDate date);
    public IndustryState Clone();
    public bool TryGetAssignee(string industryId, out PersonId personId);
    public bool TrySetAssignee(string industryId, PersonId personId, out string error);
    public bool TryClearAssignee(string industryId, out string error);
    public bool TrySetResources(IndustryResources resources, out string error);
    public bool TrySetLastSettledMonthIndex(int monthIndex, out string error);
}

public readonly struct IndustryAssignment
{
    public string IndustryId { get; }
    public PersonId PersonId { get; }
}

public enum IndustryAssignmentOutcome
{
    Succeeded, NoChange, UnknownIndustry, UnknownPerson, NotMember,
    Dead, Retired, Minor, NoRemainingUnits, Blocked
}

public readonly struct IndustryAssignmentResult
{
    public bool Succeeded { get; }
    public IndustryAssignmentOutcome Outcome { get; }
    public string Message { get; }
    public PersonId PreviousAssignee { get; }
    public PersonId CurrentAssignee { get; }
}

public static class IndustryService
{
    public const int MonthlyWorkUnits = 4;
    public const int PostWorkUnits = 3;

    public static bool CanWork(SuccessionState succession, PersonId personId, GameDate date,
        out string reason);
    public static int RemainingWorkUnits(SuccessionState succession, IndustryState state,
        PersonId personId, GameDate date);
    public static IndustryAssignmentResult TryAssign(IndustryState state, SuccessionState succession,
        GameDate date, string industryId, PersonId personId);
    public static IndustryAssignmentResult TryUnassign(IndustryState state, SuccessionState succession,
        GameDate date, string industryId);
    public static IReadOnlyList<IndustryVacancy> ReconcileAssignments(IndustryState state,
        SuccessionState succession, GameDate date);
}

public readonly struct IndustryVacancy
{
    public string IndustryId { get; }
    public PersonId PreviousAssignee { get; }
    public string Reason { get; }
}
```

**Rules:**

- `IndustryState.CreateInitial(1560-04-01)` → 30/12/0，两个 slot 均空，`LastSettledMonthIndex = MonthIndex(1560-03)`。
- `TrySetAssignee` 只校验产业存在和容量；人物资格由 `IndustryService` 校验。
- `TryAssign` 先校验产业、人物、成员、存活、未退休、年龄、剩余工时；同一人重复任命为 `NoChange` 成功；替换是单次赋值。
- `RemainingWorkUnits`：可工作者未任职 4，已任职 1，不可工作 0；`3 + 3 > 4` 自然阻止兼任。
- `ReconcileAssignments` 是总函数：只移除不可工作任命，返回原因；不产出、不扣费。若同一人物意外出现在两个产业，按产业 ID 升序保留第一个，清空后续重复任命并返回“重复任命已解除”。
- 所有资源范围 `0..999999`；`IndustryResources.TryCreate` 拒绝负数/越界。

**Tests:** `Assets/Tests/EditMode/IndustryCatalogTests.cs`、`IndustryStateTests.cs`、`IndustryServiceTests.cs`。

- [ ] `Catalog_HasFarmAndTradingHouseWithApprovedNumbers`
- [ ] `CreateInitial_UsesApprovedOpeningResourcesAndVacantSlots`
- [ ] `Assign_RejectsDeadRetiredMinorNonMemberAndUnknown`
- [ ] `Assign_RejectsSecondPostForSamePerson`
- [ ] `Assign_ReplacesOccupantAtomically`
- [ ] `Unassign_EmptyIsIdempotentSuccess`
- [ ] `RemainingUnits_AreFourMinusThreeWhenAssigned`
- [ ] `Reconcile_VacatesInvalidAssignmentsOnly`
- [ ] `Reconcile_ClearsDuplicateAssignmentKeepingFirstByIndustryOrder`
- [ ] `Resources_RejectNegativeAndOverCap`

**Acceptance:** 目录/初始值/任命/替换/解除/失效/工时规则全部由纯 C# 测试覆盖，不依赖 Unity。

## Task 2 — 月结纯计算与报告

**Create:** `Assets/Scripts/Domain/IndustrySettlement.cs`  
**Test:** `Assets/Tests/EditMode/IndustrySettlementTests.cs`

**Interfaces:**

```csharp
public enum IndustryLineStatus { Operated, Vacant, Stalled, InvalidAssignment }

public readonly struct IndustryLineResult
{
    public string IndustryId { get; }
    public string IndustryName { get; }
    public PersonId AssigneeId { get; }
    public string AssigneeName { get; }
    public IndustryLineStatus Status { get; }
    public string Reason { get; }
    public int UpkeepFunds { get; }
    public int UpkeepSupplies { get; }
    public int GrossOutputFunds { get; }
    public int GrossOutputSupplies { get; }
    public int GrossOutputIntelligence { get; }
    public int NetOutputFunds { get; }       // after maintenance
    public int NetOutputSupplies { get; }    // farm: 4 gross - 1 maintenance = 3
    public int NetOutputIntelligence { get; }
    public int AppliedFundsDelta { get; }    // actual change after cap clamp
    public int AppliedSuppliesDelta { get; }
    public int AppliedIntelligenceDelta { get; }
    public bool MaintenanceRetainedFromOutput { get; } // farm true, trading false
}

public sealed class IndustryMonthReport
{
    public int Year { get; }
    public int Month { get; }
    public IndustryResources Before { get; }
    public IndustryResources After { get; }
    public IReadOnlyList<IndustryLineResult> Lines { get; }
    public IReadOnlyList<IndustryVacancy> Vacated { get; }
    public bool HasShortage { get; }
    public bool AnyStaffed { get; }
    public bool Changed { get; }
    public string Summary { get; } // Chinese, invariant
}

public enum IndustrySettlementOutcome
{
    Succeeded, AlreadySettled, SettlementGap, InvalidState
}

public readonly struct IndustrySettlementResult
{
    public IndustrySettlementOutcome Outcome { get; }
    public bool Succeeded { get; }
    public string Message { get; }
    public IndustryState NextState { get; }
    public IndustryMonthReport Report { get; }
}

public static class IndustrySettlement
{
    public static IndustrySettlementResult Plan(IndustryState state, SuccessionState succession,
        GameDate closedMonth);
}
```

**Rules:**

- 检查 `LastSettledMonthIndex`：`== current` → `AlreadySettled`；`< current-1` → `SettlementGap`；`> current` → `InvalidState`；`== current-1` → 正常。
- 克隆状态；先 `ReconcileAssignments`，再按 `IndustryCatalog.All` 顺序逐产业结算。
- **农庄（收成后维护）**：加毛产出 +2 资金、+4 物资；从毛收成中保留 1 物资维护；净 +2 资金、+3 物资；再把净额加到库存并夹到 `999999`。从 0 起始物资也能运作。不得先把 4 加到满库存再扣 1。
- **商馆（维护后产出）**：先检查资金/物资是否足够支付 6 资金、2 物资；足够则扣维护费，再加 +9 资金、+1 情报；不足则 `Stalled`，不扣费、不产出、保留任命，`HasShortage = true`。农庄不适用“维护费不足停产”，因为它的维护来自自身毛收成。
- 空缺/失效 → 不扣费、不产出；失效报告保留旧负责人姓名与原因。
- 农庄先结算，其净产出的资金/物资立即可用于商馆维护费检查。
- 资源用 `long` 中间值；`ApplyDelta` 返回实际前后值；满仓时实际增量为 0，报告写“净产 X，实际 +0（已达上限）”。
- 报告文本包含逐产业维护/产出/实际入账和月末前后值；同一输入逐字节相同。
- 不消费随机流；不修改输入状态。

**Tests:**

- [ ] `Settle_BothStaffed_ProducesExpectedNetAndReport`（30/12/0 → 35/13/1）
- [ ] `Settle_FarmRetainsOneSupplyFromGrossHarvest`（毛 4 → 维护 1 → 净 3）
- [ ] `Settle_FarmWorksFromZeroStartingResources`（0/0/0 → 2/3/0）
- [ ] `Settle_TradingChecksMaintenanceBeforeOutput`
- [ ] `Settle_VacantIndustry_NoUpkeepNoOutput`
- [ ] `Settle_Shortage_StallsWithoutNegativeResources`
- [ ] `Settle_FarmCreditsBeforeTrading`
- [ ] `Settle_InvalidAssignee_VacatesAndProducesNothing`
- [ ] `Settle_IsDeterministicAndDoesNotConsumeRandom`
- [ ] `Settle_AlreadySettled_IsNoOp`
- [ ] `Settle_Gap_IsRejected`
- [ ] `Settle_FarmAtCap_NetBeforeClampKeepsFullWarehouse`（物资 999999 → 999999；净 3，实际 0；不得 999998）
- [ ] `Settle_ClampsAtResourceCap`

**Acceptance:** 月结是纯函数；正常/空缺/短缺/失效/重复/漏月/上限均有测试；报告前后值可读。

## Task 3 — 唯一月结入口、跨月防护与安全保存

**Modify:** `Assets/Scripts/Application/GameSession.cs`（含 `MonthTurnOutcome`）  
**Create:** `Assets/Scripts/Application/IndustryView.cs`、`Assets/Scripts/Application/AssemblyInfo.cs`  
**Test:** `Assets/Tests/EditMode/IndustryMonthCrossingTests.cs`

**Interfaces:**

```csharp
public enum MonthTurnOutcome
{
    Succeeded = 0, PendingEvent = 1, ContentUnavailable = 2, DateOutOfRange = 3,
    SchedulingFailed = 4, PendingSuccession = 5,
    MonthSettlementRequired = 6, IndustryStateInvalid = 7
}

public readonly struct IndustrySlotView
{
    public string IndustryId { get; }
    public string DisplayName { get; }
    public PersonId AssigneeId { get; }
    public string AssigneeName { get; }       // empty when vacant
    public int WorkUnitsRequired { get; }     // 3
    public int WorkUnitsRemaining { get; }    // 1 when assigned, 4 when vacant
    public IndustryLineStatus Status { get; } // derived from last/current state
    public string StatusReason { get; }
}

public sealed class IndustryView
{
    public IndustryResources Resources { get; }
    public int LastSettledMonthIndex { get; }
    public IReadOnlyList<IndustrySlotView> Slots { get; }
    public bool CanEditAssignments { get; }
    public string BlockReason { get; }
    public IndustryMonthReport LastReport { get; }
    public string LastChronicleSummary { get; }
}

// GameSession
public IndustryView Industry { get; }
public bool CanEditIndustryAssignments { get; }
public string IndustryBlockReason { get; }
public IndustryMonthReport LastIndustryReport { get; }
public string LastIndustryMessage { get; } // transient; e.g. safe-save cleanup
public bool CanAdvanceOneDayWithinMonth { get; }
public string LastTravelBlockReason { get; }
```

**Implementation:**

- `InitSession` 初始化 `_industryState = IndustryState.CreateInitial(WorldClock.CurrentDate)`。
- `TryAdvanceToNextMonth` 改为事务式：预检 → 快照（日期/产业/随机流/事件运行时/编年史）→ `IndustrySettlement.Plan` → `ScheduleMonthly(nextDate)` → 追加编年史（当 `Report.AnyStaffed || Report.Vacated.Count > 0`，即使满仓实际变化为 0 也记录“净产 X，实际 +0”）→ 提交产业/事件/日期 → 发布一次 `SessionStateChanged`。失败恢复快照。
- `AdvanceOneDay`/`AdvanceDays`：先检查目标日期月份；跨月或 `lastSettled != current-1` 时返回 false，设置 `MonthSettlementRequired`/`IndustryStateInvalid`，日期不变。
- `PreviewTravel`：开始时清空 `LastTravelBlockReason`；到达日期跨月 → 返回 null 并设置原因；成功预览不保留旧原因。`ConfirmTravel` 重新校验，失败不改日期/地点；成功时清空原因。
- `CaptureSave` 前调用 `EnsureIndustryAssignmentsValid()`（Task 4 的安全保存）；保存清理不写编年史。
- `TryApplySave` 先构建/校验 `IndustryState`，失败不触碰当前会话。
- `AssemblyInfo.cs` 加 `[assembly: InternalsVisibleTo("SengokuChronicle.EditModeTests")]`；`internal bool AdvanceDaysForLegacyFixtureSetup(int count)` 仅供既有跨年夹具，**不是 public API**，不供 UI/生产路径调用；它不做产业生产，只对齐 `lastSettledMonthIndex` 到新日期前一个月。

**Tests:**

- [ ] `AdvanceOneDay_OnLastDay_RefusesAndKeepsDate`
- [ ] `AdvanceDays_CrossingMonth_RefusesAtomically`
- [ ] `PreviewTravel_CrossingMonth_RefusesWithReason`
- [ ] `ConfirmTravel_CrossingMonth_RefusesAndKeepsLocation`
- [ ] `EndMonth_SettlesExactlyOnceAndAdvances`
- [ ] `EndMonth_PendingEvent_DoesNotSettle`
- [ ] `EndMonth_PendingSuccession_DoesNotSettle`
- [ ] `EndMonth_AtMaxDate_DoesNotSettle`
- [ ] `EndMonth_SettlementGap_DoesNotSettle`
- [ ] `EndMonth_DuplicateCalls_SettleDistinctMonths`
- [ ] `LegacyFixtureSeam_IsInternalAndNotUsedByProduction`

**Acceptance:** 所有生产时间入口都不能跨月跳结算；结束月份是唯一结算入口；重复/待处理/越界/失败均无半状态；内部 seam 只服务既有夹具。

## Task 4 — 任命 API、继承协调与失效安全保存

**Modify:** `Assets/Scripts/Application/GameSession.cs`、`Assets/Scripts/Domain/IndustryService.cs`  
**Test:** `Assets/Tests/EditMode/SC019SuccessionIndustryTests.cs`、`IndustryAssignmentSessionTests.cs`

**Interfaces:**

```csharp
public IndustryAssignmentResult TryAssignIndustry(string industryId, PersonId personId);
public IndustryAssignmentResult TryUnassignIndustry(string industryId);

// internal total cleanup; no production, no chronicle
private void EnsureIndustryAssignmentsValid();
```

**Rules:**

- 任命/解除只在 `CanEditIndustryAssignments` 为 true 时允许；待处理事件、继承决策、摄政到期、家族灭亡时返回 `Blocked` 与中文原因。
- `TryAssignIndustry` 调 `IndustryService.TryAssign`；替换释放旧预留；同一人 no-op。
- `ApplySuccessionResult` 成功后调用 `EnsureIndustryAssignmentsValid`；旧家主退休 → 其职位自动解除；新家主不自动接管；其他合法任命保留。
- **预算**：每个成人固定 4；任命预留 3；替换/解除释放预留；不建立“每人本月已花时间”计数器，月结只按锁定方案占用一次。继承不转移/不追加预算。新家主未任职可用 4，已任职可用 1。月结前允许用新家主自己的预算任命当月方案；月结后属于下月方案。不得同时声称“立即释放”和“已花时间不返还”。
- `EnsureIndustryAssignmentsValid` 是总函数：只移除不合法任命，不扣费、不产出、不推进时间、不追加编年史；设置瞬时 `LastIndustryMessage`。
- `CaptureSave` 前调用它，保证死亡/退休/离族/未成年不会造成“稳定点无法保存”。`SaveDataValidator` 仍拒绝含非法任命的存档（正常捕获不会产生）。
- 继承导致的自动解除追加一条 `Industry` 人事编年史；手动任命/解除不写编年史。

**Tests:**

- [ ] `FirstHandoff_VacatesOldHeadPostAndKeepsOtherAssignments`
- [ ] `SecondHandoff_VacatesOldHeadPostAndNewHeadStartsUnassigned`
- [ ] `Handoff_DoesNotTransferBudgetOrAutoAssign`
- [ ] `Handoff_AllowsSameMonthAppointmentUsingNewHeadOwnBudget`
- [ ] `Handoff_AppendsPersonnelChronicleOnce`
- [ ] `CaptureSave_RemovesInvalidAssignmentAndStillValidates`
- [ ] `Assignment_BlockedByPendingEventOrSuccession`

继承测试优先复用现有 M2 事件/交接夹具到达两次交接；领域级 `SuccessionService` 测试可直接构造 `SuccessionState`。不得绕过 M2 事件门禁来“制造”生产路径；若现有夹具不足，只补测试夹具，不改生产门禁。

**Acceptance:** 两次交接的旧/新家主职位、预算、当月任命与编年史全部确定；失效任命不会阻塞稳定保存。

## Task 5 — v6 存档、校验与迁移

**Modify:** `Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs`  
**Test:** `Assets/Tests/EditMode/IndustrySaveTests.cs`

**Schema:**

```csharp
[Serializable] public sealed class IndustryStateSaveData
{
    public int funds;
    public int supplies;
    public int intelligence;
    public int lastSettledMonthIndex;
    public IndustrySlotSaveEntry[] slots;
}

[Serializable] public sealed class IndustrySlotSaveEntry
{
    public string industryId;
    public string assignedPersonId; // empty = vacant
}
```

- `SaveData.industry` 与 `SaveDataPresenceProbe.industry` 新增。
- `SaveDataValidator.CurrentVersion = 6`；`SaveData.CurrentRulesVersion = 2`。
- `SaveData` 构造函数初始化 `industry = IndustryStateSaveData.CreateInitial(WorldClock.NewGameDate)`。
- `CaptureSave` 使用 `IndustryStateSaveData.Capture(_industryState)`；slots 按产业 ID 升序。
- `TryApplySave` 先恢复 `succession`，再用已恢复的 `SuccessionState` 调 `IndustryStateSaveData.TryRestore(data.industry, succession, date, ...)`；失败不触碰当前状态。

**Validation:**

- funds/supplies/intelligence `0..999999`。
- `lastSettledMonthIndex == MonthIndex(date)-1`。
- slots 恰好两个、ID 唯一、属于目录。
- 非空 assignee 必须是 succession 中已登记、在册、存活、未退休、年龄 >= adulthood 的人物；不能重复。
- 失败返回中文原因，不抛异常。

**Migration:**

- 新增 v5→v6 分支：拒绝已含 `industry` 的 v5 载荷；保留 `familyRelationships`/`chronicle`/`succession`/事件/随机流/任务/日期/地点；加初始 industry；`rulesVersion=2`；不追加编年史。
- v4→v6：先补空 `familyRelationships`，再加初始 industry；若 v4 原始 JSON 已含 `industry` 根字段，拒绝而不是静默替换。
- v3/v2→v6：保留既有迁移语义，最后加初始 industry 与 rules 2；若原始 JSON 已含 `industry`，拒绝。
- v1→v6：保留既有迁移语义，最后加初始 industry 与 rules 2；v1 解析器没有 industry 字段，若原始 JSON 含该根字段则拒绝。
- `Clone` 复制 industry；源对象/文件不变。
- `ValidatePresence`：允许版本集合加入 5；v6 必须有 `industry`；v5 不要求 `industry`，但若含 `industry` 根字段则拒绝（迁移不静默替换）；v4 及以下按既有规则。
- 更新既有测试夹具：手工构造 `SaveData` 的 `ValidData()`/硬编码 v5 JSON 必须补 `industry`（用 `CreateInitial(new GameDate(year, month, day))` 或统一 helper），否则 v6 校验会因 `lastSettledMonthIndex` 不匹配而失败。v5 迁移测试保留“无 industry”的 v5 载荷。
- 迁移分支必须**显式覆盖** `current.industry = IndustryStateSaveData.CreateInitial(legacyDate)`，不能依赖 `SaveData` 构造函数的默认值；默认值只适用于新局 1560-04-01。
- “旧载荷是否已含 industry”必须用原始 JSON 的根属性检查（`SaveDataValidator.HasRootProperty`），不能只看反序列化对象，因为 `SaveData` 构造函数会为缺失字段生成默认 industry。

**Tests:**

- [ ] `V6RoundTrip_PreservesResourcesAssignmentsAndLastSettled`
- [ ] `V5Migration_PreservesSC018FamilyRelationshipsAndChronicle`
- [ ] `V5Migration_PreservesEventHistorySuccessionMissionsAndRelationships`
- [ ] `V5Migration_InitializesIndustriesWithoutRetroactiveProduction`
- [ ] `V5PayloadWithIndustry_IsRejected`
- [ ] `V4PayloadWithIndustry_IsRejected`
- [ ] `V6Presence_RequiresIndustry`
- [ ] `Load_DoesNotAppendOrSettle`
- [ ] `LoadThenEndMonth_DoesNotDuplicateSettlement`
- [ ] `SaveAfterInvalidAssignment_CleansAndValidatesWithoutChronicleAppend`

**Acceptance:** v6 往返稳定；v5 SC-018 关系/历史逐项保留；旧档不补算收益；读档不重复结算；非法载荷安全拒绝。

## Task 6 — 编年史与现有面板类别

**Modify:** `Assets/Scripts/Domain/FamilyChronicleEntry.cs`、`HistoryLedger.cs`、`Assets/Scripts/Presentation/FamilyChroniclePanel.cs`  
**Test:** `Assets/Tests/EditMode/IndustryChronicleTests.cs`

**Rules:**

- `ChronicleCategory.Industry = 4`。
- `FamilyChronicleEntry.TryCreate` 的 Industry 分支：`FromValue/ToValue` 在 `0..999999`，允许相等；要求摘要非空。
- `HistoryLedger.TryAppendIndustry(year, month, subjectId, sourceId, fromFunds, toFunds, title, summary, out entry, out error)`。
- 月结实际发生（有产业任职或自动解除）追加一条 `industry.monthly`/`settlement`；两产业全空且无解除不写。
- 继承自动解除追加 `industry.assignments`/`succession`；手动任命不写。
- `FamilyChroniclePanel.CategoryName` 增加“产业”；`CategoryDetail` 显示“资金 X → Y”；不显示原始 ID。
- 编年史保存/读档沿用现有 `ChronicleSaveCodec`；`Industry` 记录可往返。

**Tests:**

- [ ] `IndustrySettlement_AppendsOneMonthlyEntryWithBeforeAfter`
- [ ] `IndustrySettlement_AtCapRecordsNetAndActualZero`
- [ ] `VacantMonth_AppendsNothing`
- [ ] `SuccessionVacancy_AppendsOnePersonnelEntry`
- [ ] `IndustryChronicle_RoundTripsAndRendersCategoryLabel`
- [ ] `IndustryChronicle_DoesNotExposeRawIds`

**Acceptance:** 月结与人事在编年史可见、可保存、可读档；无重复/无原始 ID。

## Task 7 — UI 面板、场景生成器与 Validator（唯一集成 worker）

**Create:** `Assets/Scripts/Presentation/IndustryPanel.cs`、`Assets/Scripts/Editor/AddSC019IndustryPanel.cs`  
**Modify:** `Assets/Scripts/Editor/FoundationSceneValidator.cs`、`Assets/Scenes/FoundationScene.unity`（仅经生成器）  
**Modify:** `Assets/Scripts/Presentation/WorldClockPanel.cs`、`WorldMapController.cs`  
**Test:** `Assets/Tests/EditMode/IndustryPanelTests.cs`、`AddSC019IndustryPanelTests.cs`

**Scene hierarchy:**

```text
Canvas/
  IndustryRoot/                     (active, direct Canvas child, owns IndustryPanel)
    IndustryOpenButton              ("产业")
    IndustryPanel                   (inactive by default)
      IndustryTitleText             ("产业与人事")
      IndustryResourceText          ("资金 30　物资 12　情报 0")
      IndustryBlockReasonText       (hidden when empty)
      IndustryRowFarm
        RowNameText                 ("农庄（等级 1・1 个职位・3/4 工时）")
        RowStatusText
        RowAssigneeText
        IndustryUnassignButton      ("解除")
        CandidateContainer
          IndustryCandidateButtonTemplate (inactive)
      IndustryRowTradingHouse       (same children)
      IndustryReportText
      IndustryCloseButton           ("关闭")
```

- Root sibling above `M2WorldMapRoot`，below `M6TitleRoot` and `EventChoiceRoot`; validator checks.
- Open button in left management column, below chronicle/roster buttons; validator checks no overlap.
- Panel `Refresh`:
  - resources; block reason; two rows; candidate buttons for eligible adults sorted by `PersonId`;
  - candidate label `姓名（剩余 X/4）`; click `TryAssignIndustry`; unassign click `TryUnassignIndustry`;
  - report: `LastIndustryReport?.Summary` else last `Industry` chronicle summary else “本月尚未结算。”;
  - subscribe `SessionStateChanged`; open/close never mutate state.
- `WorldClockPanel`: add `MonthSettlementRequired`/`IndustryStateInvalid` cases; disable “推进一天” on last day and show “月末：请结束本月进行结算”.
- `WorldMapController.SelectDestination`: if `_preview == null` and `LastTravelBlockReason` non-empty, show it before generic “无直达路线”.
- Generator:
  - constants `RootName = "IndustryRoot"`, `PanelName = "IndustryPanel"`, `OpenButtonName = "IndustryOpenButton"`, row/template names;
  - `RunMenu`, `CheckScenePreflight`, `Update(Scene)`, `Update(Scene, Action<GameObject>)`, `Validate(Scene, out string error)`;
  - one Undo group; create all; bind; validate; save only on success; exception → `Undo.RevertAllInCurrentGroup`; no scene reload;
  - refuse existing root/partial; refuse dirty/playing/wrong scene.
- `FoundationSceneValidator`: require exactly one IndustryPanel/IndustryRoot/OpenButton; check bindings, active states, sibling order, candidate template inactive, labels; write report.
- No prefab/ProjectSettings changes.

**Tests:**

- [ ] `IndustryPanel_ShowsResourcesSlotsStatusesAndReport`
- [ ] `IndustryPanel_ShowsBlockReasonWhenPendingEvent`
- [ ] `IndustryPanel_CandidateButtonsAssignAndReplace`
- [ ] `IndustryPanel_UnassignClearsSlot`
- [ ] `IndustryPanel_ReportFallsBackToLastChronicleAfterLoad`
- [ ] `AddSC019IndustryPanel_CreatesOneRootAndBindings`
- [ ] `AddSC019IndustryPanel_RefusesExistingPartialControl`
- [ ] `AddSC019IndustryPanel_RollsBackOnValidationFailure`
- [ ] `AddSC019IndustryPanel_ValidateRejectsWrongSiblingOrder`

**Acceptance:** 面板可完成任命/解除/查看月结；生成器事务式；Validator 通过；场景只由集成 worker 修改。

## Task 8 — 中文试玩文档、项目状态与证据

**Create:** `SC019_DEMO.md`  
**Update:** `PROJECT_STATE.md`、`docs/reports/sc019-implementation.md`

- [ ] `SC019_DEMO.md` 用中文写精确步骤、预期数值、停止/报告条件、结果清单；不使用截图作为唯一证据。
- [ ] 记录新局 30/12/0、两产业空缺；任命晴信/千恵后 `3/4`；结束 1560-04 → 35/13/1；编年史“产业月结”。
- [ ] 记录月末日推进/旅行拒绝；结束月份成功；重复结束不会重复同月。
- [ ] 记录短缺路线：只任命商馆，数月后物资不足停产；再任命农庄恢复。
- [ ] 记录两次交接：晴信→正綱、正綱→信正；旧家主职位解除、新家主不自动接管、其他合法任命保留。
- [ ] 记录保存/读档：资源/任命/`lastSettledMonthIndex` 恢复；读档后结束月份只结算一次。
- [ ] 记录 SC-018 v5 迁移：`familyRelationships` 与 chronicle 保留，产业初始为空。
- [ ] `PROJECT_STATE.md` 与实现报告分开写编译、EditMode、Console、Validator、Play Mode、存档/读档、用户验收；未运行项明确写“未运行/未验证”。

**Acceptance:** 文档与实现数值一致；试玩步骤可由协调方或用户按文执行；证据边界清楚。

## Task 9 — 验证、只读复审与交接

- [ ] 在用户已打开的 Unity Editor 中编译；记录 `Editor.log` 结果，不启动 batchmode。
- [ ] 运行完整 EditMode；记录 passed/failed/skipped 数量；重点跑新增测试与 SC-018/SC-013/SC-010 回归。
- [ ] 记录 Console error/warning 计数；不把“0 errors”与“0 warnings”混为一谈。
- [ ] 运行 `FoundationSceneValidator`；保存并记录 `Logs/SceneValidationReport.txt`；未运行则明确写未运行。
- [ ] 协调方在当前用户授权下用 Computer Use 执行 Play Mode；记录日期、场景、路线、数值、Console、截图/观察；标记为 coordinator-run Play Mode。
- [ ] 执行存档/读档实测；记录槽位、恢复值、重复月结检查。
- [ ] 记录用户验收（接受/拒绝/待定）；协调方 Play Mode 不替代用户验收。
- [ ] 独立 OpenCode2 只读复审：检查文件所有权、规则一致性、测试证据、未覆盖边界；不改文件。
- [ ] 用户验收前不提交、不合并、不打 tag；验收后由协调方按项目规则处理 checkpoint/merge/tag/push。

**Acceptance:** 所有证据分项、可复现、与文档一致；无未声明的“通过”。

## 测试矩阵

| 领域 | 文件 | 必测 |
|---|---|---|
| 目录/初始状态 | `IndustryCatalogTests.cs` | 两产业 ID、数值、初始 30/12/0、空缺 |
| 任命/预算 | `IndustryStateTests.cs`、`IndustryServiceTests.cs` | 资格、4/3 工时、替换、解除、兼任拒绝、幂等 |
| 月结 | `IndustrySettlementTests.cs` | 满员 35/13/1、农庄收成后维护、从 0 资源运作、商馆先维护后产出、空缺、短缺、顺序、失效、确定性、重复/漏月、满仓上限 |
| 跨月 | `IndustryMonthCrossingTests.cs` | 日/多日/旅行拒绝、结束唯一结算、待处理事件/继承、最大日期、重复调用 |
| 继承 | `SC019SuccessionIndustryTests.cs` | 两次交接、旧/新家主、预算不转移、同月任命、编年史一次 |
| 存档 | `IndustrySaveTests.cs` | v6 往返、v5 迁移保留 SC-018、无追溯产出、非法载荷、读档不重复 |
| 编年史 | `IndustryChronicleTests.cs` | 月结一条、空缺不写、人事一条、类别/前后值、往返 |
| UI | `IndustryPanelTests.cs` | 资源/状态/阻塞/候选/替换/解除/报告回退 |
| 生成器 | `AddSC019IndustryPanelTests.cs` | 单根、绑定、部分拒绝、回滚、层级 |
| 回归 | 既有 SC-010/011/013/018 测试 | 事件顺序、继承、关系、v5 迁移、M2 剧本不破坏 |

## 中文试玩交接（摘要）

完整步骤写入 `SC019_DEMO.md`；最低限度包含：

1. 打开 `FoundationScene`，Play，选择新游戏（M2/SC-013）。
2. 打开“产业”：确认资金 30、物资 12、情报 0；农庄/商馆空缺；晴信/千恵等六人可用。
3. 任命晴信→农庄、千恵→商馆；确认两人 `3/4`、剩余 1；第三人无法再占同一产业。
4. 结束月份；确认 1560-04 月结报告：资金 35、物资 13、情报 1；编年史“产业月结”含 30→35、12→13、0→1。
5. 在月末尝试“推进一天”和旅行；确认被拒绝、日期/地点不变；再结束月份成功。
6. 只保留商馆、解除农庄，连续结束月份直到物资不足；确认商馆“资源不足…停产”、资源不为负；重新任命农庄恢复。
7. 完成第一次交接（晴信→正綱）；确认农庄空缺、商馆（千恵）保留、正綱不自动接管；用正綱自己的预算任命一个职位。
8. 完成第二次交接（正綱→信正）；确认正綱职位解除、信正不自动接管、其他合法任命保留。
9. 保存→读档→结束月份；确认资源/任命/已结算月份恢复，且只结算一次；再读同一档重复，结果一致。
10. 记录 Console、EditMode、Validator、Play Mode、存档/读档与用户验收；不把协调方 Play Mode 当作 user acceptance。

**停止/报告条件：** 资源为负、同一月重复扣费/产出、跨月未被拒绝、旧家主职位未解除、新家主自动接管、读档后重复月结、存档无法保存、场景生成器部分写入、Console 红色错误。遇到即停止并报告，不继续“试玩通过”。

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| 收紧 `AdvanceDays` 破坏既有跨年测试 | 内部 `AdvanceDaysForLegacyFixtureSetup` 只用于夹具；生产 API 不变；新增测试覆盖拒绝路径 |
| 月结/事件/日期半提交 | 先纯计算，再排事件，再提交；快照恢复；测试失败/重复/越界 |
| v5 迁移丢失 SC-018 关系/历史 | 迁移克隆并逐项保留；专项测试比较数组/顺序/值；不追加编年史 |
| 失效任命导致无法保存 | 交接后、保存前、月结时三层清理；保存清理不写编年史；读档拒绝非法存档 |
| 场景生成器部分写入 | 单 Undo group、preflight、Validate、异常回滚、不重载场景 |
| 数值过松/过紧 | 首切片暂定值；试玩后由协调方决定是否调参，不在本切片加动态市场 |
| 误报 M3 完成 | 文档与报告明确“首切片”；不把两次交接当作完整 M3 验收 |

**回滚：** 代码改动可按文件回退；v6 存档不降级，保留 v5 原文件与 `.bak`；场景只由集成 worker 的 Undo 事务修改，失败不保存；不删除既有分支/checkpoint；用户验收前不做 git 集成。


---

<a id="2026-11-04-sc-020-character-growth-education"></a>
## 来源：docs/superpowers/plans/2026-11-04-sc-020-character-growth-education.md

# SC-020 子女培养与人物成长实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Follow this plan task by task using the existing Orca OpenCode2 task; do not create another Unity project or a parallel writer.

**Goal:** 在现有 Unity 项目交付一条 SC-020 教育示范路线，让玩家为 15 岁的青叶千景选年度培养方向和导师，经历 1560 年月结，在 1561-01-01 看到确定成长与成年状态。

**Architecture:** 纯 C# 成长状态与公式按稳定 PersonId 保存；`GameSession` 在现有事务式月结中于 12 月边界结算教育；`SaveData` v7 持久化计划、能力与结算标记；FoundationScene 通过事务式生成器新增独立标题入口和教育面板。SC-013、SC-018、SC-019 规则与样例保持原样。

**Tech Stack:** Unity 6 / C# / 既有 uGUI、JsonUtility、EditMode Test Framework、FoundationSceneValidator。

**Spec:** [SC-020 设计稿](design-and-planning-history.md#2026-11-04-sc-020-character-growth-education-design)

## Global Constraints

- GDD v4.0 是设计权威；SC-020 是 M3 子切片，不代表 M3 完成。
- 仅使用当前 `C:\Users\panye\My project`；不建立 Unity 项目、不安装包、不新增外部资产或付费服务。
- 代码由 OpenCode2 实施；本 Dispatch 是唯一写入者，也是唯一场景/ProjectSettings 写入者。随后由独立 OpenCode reviewer 只读审查。
- 当前分支 `feature/sc018-independent-core-events` 含大量未提交 SC-018/SC-019 工作。不得清理、回退、覆盖或重排这些工作；修改同一文件前先检查并保留现有 diff，只做兼容的增量编辑。
- 保留所有 `.meta` / GUID；不改 `Library` 或 `Temp`；不新建或重置用户存档。
- 任何 Unity 操作前先枚举 `Unity.exe` 进程并读取其 `-projectPath`；只用此项目已经打开的 Editor，不启动第二个 Editor 或 batchmode。代码可静态检查；Play Test 留给用户，不截图、不使用 Computer Use 操作 Unity。
- EditMode 测试、编译、Console 计数、场景 Validator、Play Mode 和用户验收分别报告。未经用户 Play Test 验收不得合并、打 tag 或提交 milestone checkpoint。
- 不修改 `Packages/manifest.json`。若实现发现需要包或外部内容，停止并先报告许可证与兼容性。

## Review Focus

1. 12 月到 1 月结算期间若事件排程或编年史写入失败，成长、计划、产业、日期和事件随机态一起回滚；用年度事务注入故障测试。
2. 同一个 `(PersonId, AgeYear)` 多次提交、读档或重复结算不能多次发放；用计划幂等与年末重复调用测试。
3. 失效导师、未成年/死亡/退休/非家族导师、错误方向与缺失成长存档均明确拒绝；用 API 与 SaveDataValidator 测试覆盖。
4. v6 到 v7 必须保留 SC-018 家庭关系、编年史、事件/继承和 SC-019 产业；迁移不伪造教育历史；用完整 v6 fixture 与缺字段/空字段 fixture 测试。
5. FoundationScene 生成异常必须在同一个 Undo 组中回滚；验证器要拒绝缺组件、重复对象、画布外控件或遮挡既有 HUD 的 SC-020 控件。

---

### Task 1: 纯 C# 成长与年度培养规则

**Files:**
- Create: `Assets/Scripts/Domain/CharacterGrowth.cs`
- Create: `Assets/Scripts/Domain/CharacterDevelopmentState.cs`
- Test: `Assets/Tests/EditMode/CharacterGrowthRulesTests.cs`
- Test: `Assets/Tests/EditMode/CharacterDevelopmentStateTests.cs`

**Interfaces:**
- Produce `GrowthAttribute`（Ninjutsu、MartialArts、Scholarship、Management、Social）、`EducationDirection`（上述五类 + FreeGrowth）、`MentorSpecialty`、`CharacterGrowthValues`、`EducationYearPlan`、`EducationYearResult`、`CharacterDevelopmentState` 与 `CharacterGrowthRules`。
- `CharacterGrowthValues.TryCreate(int ninjutsu, int martialArts, int scholarship, int management, int social, out CharacterGrowthValues values, out string error)` 接受 `0..100`；`CharacterGrowthValues.Apply(...)` 对结果按 `0..100` 夹值。
- `CharacterGrowthRules.Resolve(CharacterGrowthValues before, EducationDirection direction, GrowthAttribute childAptitude, MentorSpecialty mentor, bool supportiveEnvironment)` 返回不可变 `EducationYearResult`；定向成长对所选属性 `+4`，资质匹配 `+2`，导师专长匹配 `+2`，有利家庭环境 `+1`；FreeGrowth 五项均 `+1`，不使用随机数。
- `CharacterDevelopmentState` 按 PersonId 保存能力与 `(PersonId, AgeYear)` 年度计划/结算标记，提供 `Clone()`、查询、替换未结算计划、一次性结算与验证方法；未知 ID、非法年龄/方向、重复结算均给显式结果，不部分修改状态。

- [ ] **Step 1: 先写失败测试** `CharacterGrowthRulesTests`：学问资质 + 小夜导师 + 有利环境将学问 `20→29`；同一资质由宗一郎指导为 `20→27`；FreeGrowth 的五项各 `+1`；`99` 上限夹值到 `100`；无效初值/方向被拒绝。
- [ ] **Step 2: 在 Unity Editor EditMode Test Runner 只运行这两个新测试类，确认新增行为先失败且既有代码可编译。** 不使用 batchmode。
- [ ] **Step 3: 写 `CharacterGrowth.cs`，实现固定枚举、取值校验、增量计算、解释字段与夹值。** 不引入随机数或 UnityEngine 依赖。
- [ ] **Step 4: 写 `CharacterDevelopmentState.cs`，实现稳定 ID 索引、计划可替换、年度最多结算一次及克隆。** 在克隆上先验证再提交，失败不改原对象。
- [ ] **Step 5: 运行上述新测试并确认所有指定数值与拒绝路径通过。**

### Task 2: GameSession 年度事务与 SC-020 固定剧本

**Files:**
- Create: `Assets/Scripts/Application/SC020EducationScenario.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Modify: `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- Modify: `Assets/Scripts/Domain/HistoryLedger.cs`
- Test: `Assets/Tests/EditMode/SC020EducationSessionTests.cs`
- Test: `Assets/Tests/EditMode/GameSessionChronicleTests.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs`

**Interfaces:**
- `SC020EducationScenario.CreateSuccession()` 创建青叶家固定成员：宗一郎 `person.sc020.head` / 1520 / Head，配偶小夜 `person.sc020.spouse` / 1522 / Spouse，千景 `person.sc020.child` / 1545 / DirectKin；创建两条 ParentChild 边与一条 Spouse 边。千景资质 Scholarship，家庭环境 supportive；宗一郎导师专长 MartialArts，小夜导师专长 Scholarship；千景初值为 `15/12/20/10/13`。
- `GameSession.InitEducationDemoScenario()` 创建上述剧本，起始日 `1560-01-01`，SC-019 初始产业资源与空岗位；保留标准/危机/SC-013 入口行为。
- `GameSession.TrySetEducationPlan(PersonId learner, EducationDirection direction, PersonId mentor)` 返回 `EducationPlanCommandResult`，在稳定管理点校验成员、存活、`AgeAt(CurrentGameDate) <= 15`、成年导师资格与方向；同年度可替换计划，但替换不立即成长或写编年史。
- `GameSession` 在关闭 12 月时，对成长状态取快照；依序完成产业结算、教育年度结算、下一月事件排程、日期提交与一次 `SessionStateChanged`。教育结果经 `HistoryLedger.TryAppendEducation(...)` 形成一条含人物姓名、方向、导师、能力前后值的编年史记录；失败时恢复日期、产业、成长、随机流、事件运行态和编年史。

- [ ] **Step 1: 写失败测试** `SC020EducationSessionTests`：固定日期/家庭图/资源/初始能力；合法方向与导师可提交，未成年/死亡/退休/非成员导师拒绝；同一年度替换计划不改属性；从 1560-01-01 结束 11 个月仍未结算教育，关闭 12 月时学问按方案增长一次且日期到 1561-01-01、年龄为 16；重复结算不重复成长/编年史。
- [ ] **Step 2: 在 EditMode Test Runner 运行该测试类，确认新规则测试先失败。**
- [ ] **Step 3: 新建 `SC020EducationScenario.cs` 并增量扩展 `GameSession`。** 新场景须使用稳定 ID 与新的 ScenarioId；不得把 SC-013 事件条件应用到 SC-020。
- [ ] **Step 4: 扩展 `FamilyChronicleEntry` / `HistoryLedger` 增加教育类别和幂等追加接口。** 文本只显示本地化方向名与姓名，不显示原始稳定 ID。
- [ ] **Step 5: 在既有 `TryAdvanceToNextMonth` 事务快照/回滚中增加 12→1 教育阶段；添加故障注入测试，证明事件排程或编年史失败后重试结果仍为同一条记录和同一组属性值。**
- [ ] **Step 6: 运行新增 session、chronicle 与 clock 测试，以及受影响的现有月结/继承/SC-010 时间暂停测试。** 报告实际 Test Runner XML 路径与各程序集 totals。

### Task 3: v7 存档、迁移与校验

**Files:**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs`
- Test: `Assets/Tests/EditMode/CharacterGrowthSaveTests.cs`
- Test: `Assets/Tests/EditMode/SaveDataValidatorTests.cs`
- Test: `Assets/Tests/EditMode/SuccessionSaveTests.cs`

**Interfaces:**
- 在 `SaveData` 增加 `CharacterDevelopmentSaveData development`，以排序后的稳定 PersonId entries 保存五项能力、年龄年度培养计划、导师 ID、已结算年度和结果；presence probe 对 v7 必须区别缺失根字段与显式 null。
- `CharacterDevelopmentSaveData.Capture(CharacterDevelopmentState)` / `TryRestore(SuccessionState, out CharacterDevelopmentState, out string error)` 只处理纯 DTO 与领域状态，不序列化 Unity 对象。
- `SaveData.CurrentRulesVersion = 3`、schema 为 v7；事件内容版本不变。

- [ ] **Step 1: 写失败测试**：v7 保存/读档逐项还原状态；v6 完整 fixture 迁移到 v7 时保留 SC-018 关系、全部 chronicle、eventRuntime、succession 与 industry，并为现存人物创建 0 能力、空培养历史；非法 ID、越界能力、重复人物/年度、未知方向、无效导师或 development 为 null 明确拒绝；相同日期重读后不重放结算。
- [ ] **Step 2: 在 EditMode Test Runner 运行新保存测试和现有 validator/succession 保存测试，确认新增断言先失败。**
- [ ] **Step 3: 扩展 SaveData/PresenceProbe/Codec，按 PersonId 排序 capture，严格验证 v7 必填对象。**
- [ ] **Step 4: 扩展 `SaveDataMigrator`：v6→v7 添加空成长/教育状态，不补算教育也不新增编年史；v1–v5 继续通过现有迁移链到 v7；含错误版本字段或重复状态的载荷拒绝且保留原档。**
- [ ] **Step 5: 运行新增测试、现有 SaveDataValidator/SuccessionSave/IndustrySave 测试与完整 EditMode 程序集。**

### Task 4: 标题入口、培养面板与事务式场景集成

**Files:**
- Create: `Assets/Scripts/Presentation/CharacterEducationPanel.cs`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Create: `Assets/Scripts/Editor/AddSC020CharacterEducation.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scenes/FoundationScene.unity`
- Test: `Assets/Tests/EditMode/CharacterEducationPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC020CharacterEducationTests.cs`
- Test: `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs`

**Interfaces:**
- `CharacterEducationPanel.BindReferences(GameSession session, GameObject panelRoot, Button openButton, Button closeButton, Text overviewText, Transform directionContainer, Button directionTemplate, Transform mentorContainer, Button mentorTemplate, Text previewText, Button confirmButton, Text messageText)` 绑定场景依赖；预览由 session 的只读成长模型计算，确认只调用 `TrySetEducationPlan`。
- `TitleScreenController.BindEducationScenarioButton(Button educationDemo)` / `StartEducationDemo()` 新增一个独立入口；原有 `StartNewGame`、`StartStandardSample` 与 `StartCrisisDemo` 语义保持不变。
- `AddSC020CharacterEducation` 采用与 `AddSC019IndustryPanel` 相同的单 Undo group、场景路径/脏状态/Play Mode preflight、写入前检查、保存前完整 Validator 与异常 Undo 回滚规则；生成控件位于单个 `SC020EducationRoot`，不与 HUD、事件、产业、编年史或家谱入口重叠。

- [ ] **Step 1: 写 EditMode 失败测试**：面板显示 5 项数值与年龄年度；方向/导师改变时预览符合 `+9/+7`；确认成功后按钮/状态防重复奖励；关闭重开不丢计划；缺绑定引用时安全诊断。场景测试拒绝错误场景、dirty scene、Play Mode、重复/部分控件、遮挡与缺少引用。
- [ ] **Step 2: 在 EditMode Test Runner 运行两个新 UI/scene-generator 测试类与标题场景测试，确认新断言先失败。**
- [ ] **Step 3: 实现 `CharacterEducationPanel` 和 `TitleScreenController` 入口绑定。** 动态按钮每次刷新先移除旧行与 listener，禁止重复绑定。
- [ ] **Step 4: 实现 `AddSC020CharacterEducation`；由本 OpenCode2 Dispatch 作为唯一场景 writer 执行一次事务式生成并保存 FoundationScene。** 若生成器异常，验证 Undo 还原对象数及场景 dirty 状态；不使用直接 `DestroyImmediate` 清理既有场景对象。
- [ ] **Step 5: 扩展 `FoundationSceneValidator` 检查唯一 root、层级、所有必需绑定、画布内布局、与既有 HUD/面板入口不重叠，并验证 SC-013/SC-019 场景控件仍存在。**
- [ ] **Step 6: 在当前 Editor Test Runner 运行新增 UI/生成器/Validator 测试与受影响标题测试；读取该项目 `Editor.log` 与场景 Validator 报告。** 不进 Play Mode，不截图，不替用户点击。

### Task 5: 文档、OpenCode2 自检与审查交接

**Files:**
- Create: `SC020_DEMO.md`
- Create: `docs/reports/sc020-implementation.md`
- Modify: `PROJECT_STATE.md`
- Modify: `DECISIONS.md`

- [ ] **Step 1: 建立 `SC020_DEMO.md`**，写出准备步骤、SC-020 入口、存档前置条件、方向与导师选择、保存/读取、推进 12 个月、检查 1561-01-01 成年结果、重复结算/编年史验证、明确失败停止条件及纯文本回报清单；注明 Play Test 由用户执行。
- [ ] **Step 2: 更新 `PROJECT_STATE.md` / `DECISIONS.md`**，记录 SC-020 范围、schema/rules 版本、设计决策、当前代码与测试状态；用户验收保持 pending，不将 M3 标记完成。
- [ ] **Step 3: 写实现报告**，列出变更文件、Unity 项目与 Editor 路径、编译结果、EditMode totals、Console 错误/警告/信息数、场景 Validator 结果、未覆盖事项；不把自动化测试写成用户 Play Test。
- [ ] **Step 4: OpenCode2 完成同一任务的实现自检并通过 `kanban_request_review(reviewer="qatester")` 提交 QA。** QATester 只审查该实现任务；如有缺陷按同任务返回 OpenCode2 修正并复审。
- [ ] **Step 5: QA 通过后由 lead 派独立 OpenCode reviewer 只读审查最终 diff 与证据，再向用户交付 Demo 和逐步 Play Test 清单。** 用户 Play Test 验收前不提交、不合并、不 tag、不 push。

## 本计划不包含的验收

- OpenCode2 不运行人工 Play Test；不会操作用户 Unity UI。
- 自动化 EditMode 测试与 scene validator 不能证明 Play Mode 体验，用户需亲自执行 `SC020_DEMO.md` 并报告结果。
- 本切片验收后仍需继续完成 M3 的其他方向；此计划不包含婚姻/收养、隐居、摄政或完整人物成长。


---

<a id="2026-11-04-sc-021-family-research"></a>
## 来源：docs/superpowers/plans/2026-11-04-sc-021-family-research.md

# SC-021 家族研究与技艺传承实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic, saveable family research plan that records one completed technique as inheritable family knowledge without granting personal mastery.

**Architecture:** Keep research rules and state in plain C# Domain types. `GameSession.TryAdvanceToNextMonth` plans research after industry settlement and before education/event scheduling, commits all state as one transaction, and restores research/resources/history with the existing rollback path. SaveData stores stable IDs and migrates v7 saves to empty research state; the FoundationScene panel is generated through one Undo transaction and adds a standalone SC-021 demo button to the existing title UI for a separate fixed Kageyama family/scenario (`family.kageyama.sc021`); it is not part of the SC-013 standard/crisis button pair and does not present or change another family's scenario.

**Tech Stack:** Unity 6.6.6f1, C#, existing asmdefs, Unity Test Framework/NUnit EditMode tests, uGUI, `JsonUtility`, existing editor-generator and scene-validator patterns. No package or external code.

**Spec:** `docs/superpowers/specs/2026-11-04-sc-021-family-research-design.md`

## Global Constraints

- Implement code through OpenCode2 on the current task worktree `panyeung/sc021-scene-integration`; preserve every pre-existing dirty or untracked path and do not stage unrelated files.
- Use the existing Unity project. Before any Unity work, inspect every running Unity PID and its exact `-projectPath`; never target the user's open `C:\Users\panye\My project` Editor. Verify with that worktree's own Editor when one is open; otherwise a temporary batchmode run against this worktree is allowed only when no Editor holds this folder, and never against a folder held by an Editor.
- Keep all rule/state code in pure C# where practical; do not create a new Unity project or Scene, install packages, or copy external code.
- Preserve existing `.meta` files and GUIDs; do not modify `Library` or `Temp`.
- Only the designated integration worker edits `FoundationScene`, prefabs, or `ProjectSettings`. The generator uses one Undo group, validates before saving, reverts through Undo on exceptions, does not reload a scene for rollback, and places all new UI under one fixed root.
- Keep SC-010, SC-013, SC-019, and SC-020 scenario behavior intact. New research does not grant character mastery or consume random streams.
- Use Unity Editor Test Runner and fresh project-specific logs/XML for verification. Report compilation, EditMode totals, Console counts, validator results, Play Mode evidence, and user acceptance separately.
- After each verification gate passes, commit only the verified task files and push them to the feature branch (`panyeung/sc021-scene-integration`). Do not merge to `main` or tag before the user accepts the demo. The user executes Play Test; do not use screenshots or Computer Use to interact with Unity while the user is awake.

## Review Focus

1. Insufficient funds or supplies must pause research without spending either resource or changing progress; test each shortfall independently.
2. A researcher who becomes dead, retired, underage, outside the active family, or assigned to an industry slot must not progress; replacing the researcher keeps progress. Pin these cases in `FamilyResearchStateTests` and `FamilyResearchSessionTests`.
3. A research completion and subsequent exception in monthly scheduling must restore resources, progress, known techniques, date, event runtime, random state, and chronicle; test by injecting failure after research is planned and after event RNG is consumed.
4. Inheritance and save/load preserve family knowledge but never populate a person-mastery collection; test succession and v7-to-v8 migration explicitly.
5. Duplicate technique IDs, duplicate known-knowledge entries, unknown researcher IDs, negative progress, and progress beyond the research requirement must fail save validation without partial load.

---

### Task 1: Family research rules and state

**Files:**
- Create: `Assets/Scripts/Domain/FamilyResearchCatalog.cs`
- Create: `Assets/Scripts/Domain/FamilyResearchState.cs`
- Create: `Assets/Scripts/Domain/FamilyResearchSettlement.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchCatalogTests.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchStateTests.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchSettlementTests.cs`

**Interfaces:**
- Consumes: `PersonId`, `SuccessionState`, `IndustryResources`, `IndustryCalendar.MonthIndex`, and existing family eligibility rules.
- Produces: `FamilyResearchPlan`, `FamilyResearchState`, `FamilyResearchCatalog.SmokeMixingResearchId`, `FamilyResearchCatalog.SmokeMixingTechniqueId`, and immutable `FamilyResearchSettlementResult`.
- Catalog entry: research ID `research.smoke-mixing`, technique ID `technique.smoke-mixing`, display name `烟幕调制术`, required progress `2`, monthly cost `1` fund + `1` supply, monthly progress `1`.

- [ ] **Step 1: Write failing catalog tests** for the stable IDs, one authored technique, required progress `2`, and cost/progress constants `1/1/1`.
- [ ] **Step 2: Run only `FamilyResearchCatalogTests` in the current Editor Test Runner**; confirm the missing catalog members fail to compile or assert.
- [ ] **Step 3: Implement the immutable catalog entry** in `FamilyResearchCatalog.cs`; do not add external packages or ScriptableObject dependencies.
- [ ] **Step 4: Write failing state tests** for empty state, one active project/researcher, progress range `0..1`, switching researchers while preserving progress, completion adding one unique family-knowledge ID, and clone independence.
- [ ] **Step 5: Run only `FamilyResearchStateTests`** and confirm the expected failures.
- [ ] **Step 6: Implement `FamilyResearchState.TryStartOrReplacePlan`, `TryAdvance`, `TryComplete`, `Clone`, and `TryRestore`** with stable-ID validation, a maximum of one active project, and no person-mastery state.
- [ ] **Step 7: Write failing settlement tests** for exact monthly cost/progress, already-known technique, invalid researcher, resource shortfall, repeated month, future settlement gap, and input immutability.
- [ ] **Step 8: Run only `FamilyResearchSettlementTests`** and confirm the expected failures.
- [ ] **Step 9: Implement `FamilyResearchSettlement.Plan(state, resources, succession, closedMonth)`** as a pure deterministic function. Invalid researcher or insufficient resources pauses with no cost/progress; a successful month deducts exactly `1` fund and `1` supply, adds `1` progress, and records completion exactly once at progress `2`.
- [ ] **Step 10: Run all three targeted test fixtures** and confirm every rule passes.

### Task 2: Session commands and atomic monthly settlement

**Files:**
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchSessionTests.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs`

**Interfaces:**
- Consumes: `FamilyResearchState`, `FamilyResearchSettlement`, `IndustryState`, `SuccessionState`, `HistoryLedger`, and the existing month-settlement fault-injection seam.
- Produces: `GameSession.Research`, `GameSession.TrySetResearchPlan(string researchId, PersonId researcherId)`, `GameSession.LastResearchReport`, and research-specific status text.
- Settlement order: industry → research → education → next-month event scheduling → date/state commit.

- [ ] **Step 1: Add failing session tests** for adult/current-family/alive/non-retired eligibility, exclusion of current industry assignees, plan replacement preserving progress, and no advancement while an event/succession decision blocks time.
- [ ] **Step 2: Run `FamilyResearchSessionTests`** and confirm they fail before implementation.
- [ ] **Step 3: Add the research state property and plan command** to `GameSession`; validate all IDs and eligibility before mutation and expose actionable Chinese failure reasons.
- [ ] **Step 4: Add a research-success session test** that starts with enough resources, runs one month, and observes funds/supplies decrease by `1/1` and progress increase by `1`; run it and confirm failure.
- [ ] **Step 5: Integrate the research plan into `TryAdvanceToNextMonth`** after industry output has been planned and before education/event scheduling. Do not consume the event or research random streams.
- [ ] **Step 6: Add a completion test** proving the second successful month records one family-knowledge technique, one research Chronicle entry, and no individual skill/mastery; run it and confirm failure.
- [ ] **Step 7: Add rollback tests** for insufficient-resource no-op and injected post-research/post-event-scheduling exception. Assert exact restoration of date, industry resources/settled index, research plan/progress/knowledge, education, event runtime, random streams, and chronicle; assert retry succeeds once.
- [ ] **Step 8: Run `FamilyResearchSessionTests` and relevant `WorldClockTests`**; all tests pass with no duplicate monthly effects.

### Task 3: Save schema v8 and migration

**Files:**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchSaveTests.cs`
- Test: `Assets/Tests/EditMode/SaveDataValidatorTests.cs`
- Test: `Assets/Tests/EditMode/SaveDataMigratorTests.cs`

**Interfaces:**
- Consumes: `FamilyResearchState` capture/restore and existing v1–v7 migration chain.
- Produces: `FamilyResearchSaveData` with one optional plan, current progress, last settled month index, and sorted unique known technique IDs; bump save format `7→8` and rules version `3→4`.

- [ ] **Step 1: Add failing save round-trip tests** for an in-progress plan, researcher ID, progress, completed family knowledge, and absence of any personal-mastery data.
- [ ] **Step 2: Run `FamilyResearchSaveTests`** and confirm the new state is not persisted.
- [ ] **Step 3: Add the `research` DTO block and capture/restore wiring**; keep it plain serializable data with stable IDs only.
- [ ] **Step 4: Add failing validator tests** for missing v8 block, unknown technique/researcher IDs, duplicate knowledge, negative/over-cap progress, plan/knowledge conflict, and v7 block presence rules; run them and confirm failures.
- [ ] **Step 5: Implement research validation** without partially restoring `GameSession`; preserve `SaveDataPresenceProbe` behavior where absent and explicit-null blocks differ.
- [ ] **Step 6: Add migration tests** for v7→v8 initializing empty plan/knowledge with last-settled month equal to the prior research month, preserving all existing resources, family relationships, industry, education, event state, succession, and chronicle without backfilling.
- [ ] **Step 7: Implement v7→v8 migration** while retaining the v1–v6 migration chain and rejecting incompatible non-null research payloads in versions where the field is not supported.
- [ ] **Step 8: Run `FamilyResearchSaveTests`, `SaveDataValidatorTests`, and `SaveDataMigratorTests`**; verify exact v7 and v8 fixtures.

### Task 4: Chronicle and player-facing research panel

**Files:**
- Modify: `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- Modify: `Assets/Scripts/Domain/HistoryLedger.cs`
- Modify: `Assets/Scripts/Presentation/FamilyChroniclePanel.cs`
- Create: `Assets/Scripts/Application/FamilyResearchView.cs`
- Create: `Assets/Scripts/Presentation/FamilyResearchPanel.cs`
- Create: `Assets/Scripts/Editor/AddSC021FamilyResearch.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scripts/Editor/AddM6TitleScreen.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchViewTests.cs`
- Test: `Assets/Tests/EditMode/FamilyResearchPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC021FamilyResearchTests.cs`
- Test: `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`

**Interfaces:**
- Consumes: `GameSession.TrySetResearchPlan`, `FamilyResearchView.Build`, existing Chronicle and title-screen patterns.
- Produces: one research-open action, one active project/researcher choice, progress/cost/pause/completion feedback, and readable Chronicle row.
- Scene hierarchy: all new UI under one fixed `Canvas/SC021ResearchRoot`; no prefab or new Scene.

- [ ] **Step 1: Add failing chronicle tests** for category `Research`, readable technique/researcher names, progress `1→2`, completion month, and no raw `research.`/`technique.`/`person.` IDs.
- [ ] **Step 2: Run `FamilyChroniclePanelTests`** and confirm the category/rendering failures.
- [ ] **Step 3: Add `ChronicleCategory.Research`, a validated `HistoryLedger.TryAppendResearch(...)`, and Chronicle formatting** with stable, localized IDs.
- [ ] **Step 4: Add failing `FamilyResearchViewTests` and `FamilyResearchPanelTests`** for eligibility, plan progress, shortfall/invalid-worker messages, same-month plan replacement, wiring of every button listener, and exactly one panel refresh after a session command; run them and confirm failure.
- [ ] **Step 5: Implement the application view and panel** as a projection/command adapter; do not place rule calculations in MonoBehaviours and register/unregister listeners symmetrically.
- [ ] **Step 6: Add failing generator/validator tests** for one rooted hierarchy, correct bindings, no overlap with `SuccessionHud`, industry, Chronicle, or roster controls, and Undo rollback on injected validation/save failure; run them and confirm failures.
- [ ] **Step 7: Implement `AddSC021FamilyResearch`** with one Undo group, validate-before-save, Undo rollback only, no scene reload, and fixed-root placement. Extend `FoundationSceneValidator` and add one SC-021 title entry without changing existing entries or scripts.
- [ ] **Step 8: Run targeted Chronicle, view, panel, generator, and title-screen tests**; verify all listeners use real `Button.onClick` callbacks.

### Task 5: SC-021 fixed scenario and handoff documents

**Files:**
- Create: `Assets/Scripts/Application/SC021FamilyResearchScenario.cs`
- Test: `Assets/Tests/EditMode/SC021FamilyResearchScenarioTests.cs`
- Create: `SC021_DEMO.md`
- Modify: `PROJECT_STATE.md`
- Modify: `DECISIONS.md`
- Create: `docs/reports/sc021-implementation.md`
- Modify: `docs/research/SC021-family-knowledge-reuse-research.md` only if the final integration review changes the reuse decision.

**Interfaces:**
- Consumes: new research state/default resources, existing scenario-entry routing, and persistent scenario selection behavior.
- Produces: a repeatable SC-021 entry in the existing title UI that supplies enough resources for two successful research months without changing SC-010/013/019/020 scenario state.

- [ ] **Step 1: Write scenario routing tests** proving the new entry opens SC-021 in the existing project/scene, supplies exactly the two-month research costs, and leaves the existing title entries (新游戏、继续、SC-010 标准样例、SC-010 危机示范、SC-020 子女培养示范、SC-022 婚姻与收养示范) and their deterministic starting snapshots unchanged; run and confirm failure.
- [ ] **Step 2: Implement `SC021FamilyResearchScenario`** using original content and stable IDs; do not create a second Unity project, Scene, or package.
- [ ] **Step 3: Write `SC021_DEMO.md`** with setup, the SC-021 entry, plan assignment/replacement, two month endings, shortfall pause, completion/chronicle, save-load, inheritance retention, stop conditions, and a results checklist. Clearly state Play Test is user-run.
- [ ] **Step 4: Update `PROJECT_STATE.md` and `DECISIONS.md`** with scope, schema/rules version, research result, current verification status, and pending user acceptance; do not mark M3 complete.
- [ ] **Step 5: Write the implementation report template/results** with changed files, Editor/project path, compile/Test Runner/Console/validator outcomes, Play Mode owner, unverified branches, and known issues. Do not infer runtime behavior from static checks.
- [ ] **Step 6: Run `SC021FamilyResearchScenarioTests` and the full EditMode suite in the existing Unity Editor**; export fresh XML and confirm no failed/skipped tests.
- [ ] **Step 7: Run the existing `FoundationSceneValidator` from that Editor**; record the exact fresh report and zero/error totals, or stop and route corrections to OpenCode2.
- [ ] **Step 8: Stop at the user Play Test handoff** with the project on the current feature worktree. Commit and push only the verified task files; ask the user to follow `SC021_DEMO.md`. Only explicit acceptance unlocks merge to `main` and tagging.

## OpenCode2 implementation handoff

OpenCode2 owns all source, tests, and documentation edits described above. Keep this work sequential on the existing task worktree so the dirty SC-018/019/020 implementation is visible and preserved. Before scene work, prove the current Editor process targets this exact project and confirm the Editor is out of Play Mode; only the designated integration worker edits `FoundationScene`. After implementation, submit the same task for QA review per `AGENTS.md`, resolve findings on that task, then obtain an independent OpenCode read-only review. If Orca cannot launch the required agent, stop that handoff and report the exact launcher error rather than changing agent/configuration. Mochi performs final sign-off and presents the user Play Test steps; user acceptance remains a separate gate.


---

<a id="2026-11-04-sc-022-family-formation"></a>
## 来源：docs/superpowers/plans/2026-11-04-sc-022-family-formation.md

# SC-022 家族婚姻与日常收养实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use the OpenCode2 supervised implementation workflow described in `AGENTS.md`; implement task-by-task and request the separate read-only review after implementation.

**Goal:** 在现有 `FoundationScene` 中交付一个固定 SC-022 剧本和可复用的家族管理面板，让玩家登记多位正式配偶或普通收养，并把成员身份、亲属事实、编年史和存档一致地保存。

**Architecture:** 以 `GameSession` 作为婚姻/收养命令边界，先用纯 C# 规则验证人物资格、直系亲属限制与亲子链循环，再一次性提交成员、亲属边和编年史。只读应用视图为独立 uGUI 面板提供当前家族成员和两类候选人；SC-022 使用独立固定家族身份和标题入口，不改已有剧本。

**Tech Stack:** Unity 6、C#、现有 Domain/Application/Presentation/Editor asmdef、uGUI、NUnit EditMode tests、既有 JSON 存档和编年史。

**Spec:** `docs/superpowers/specs/2026-11-04-sc-022-family-formation-design.md`

## Global Constraints

- 所有代码由 OpenCode2 在现有 SC-018 feature 工作区实现；保留全部既有脏改动。
- 复用当前 Unity 项目与 `Assets/Scenes/FoundationScene.unity`；不建新项目或场景，不改 `Library`、`Temp`，不安装 Unity package。
- 保留 `.meta` 文件与 GUID；不复制商业游戏内容，演示人物和文案原创。
- 正式配偶不设人数上限；拒绝本人、重复配偶及直系血亲/养亲关系；兄弟姐妹不额外限制。
- 普通收养拒绝本人、正式配偶、重复收养和会形成 `ParentChild`/`Adoption` 有向亲子环的对象；保留既有血缘事实，不触发继承交接。
- 婚姻双方/收养人必须在世且成年；婚姻候选必须成年；收养候选可为儿童或成年人。非成员候选必须是当前 SC-022 剧本明确注册的人物。
- 婚姻或收养不推进日期、不运行月结、不消耗资源或 RNG；保存/读取后候选人物、成员身份、关系和编年史一致。
- 编译与 EditMode/PlayMode 使用已打开的 Unity Editor；禁止对其项目目录运行 batchmode。用户负责 Play Test；未获验收不得提交、合并、打 tag 或 push。
- GitHub 复用研究见 `docs/research/SC022-family-formation-reuse.md`；因 Orca 未配置 `scout` 执行别名，本轮由协调者按同一范围完成只读研究。优先现有代码，报告许可、Unity 6 兼容、维护状态、依赖和集成工作量；不克隆、安装或复制代码。

## Review Focus

- `KinshipGraph` 已含既有循环或重复关系时，候选筛选和命令验证必须终止且不误放行；由 Task 1 的祖先路径循环与重复边测试覆盖。
- 婚姻候选人与关系发起人存在多跳血缘/收养直系关系时，婚姻应被拒；兄弟姐妹仍按批准规则可登记；由 Task 1 覆盖多跳与同胞两种情形。
- 候选对象在展示后、确认前已死亡或加入家族时，提交必须重新校验并拒绝；由 Task 2 会话命令测试覆盖。
- `HistoryLedger` 在候选校验失败、日期无效或条目无法创建时，不得留下家族成员或亲属边；由 Task 2 的失败快照测试覆盖。
- 存档中存在未加入当前家庭的已注册候选人，或旧版本缺少 SC-022 记录时，读取仍需兼容且不能把所有非成员误列为候选；由 Task 5 往返测试及已有存档回归覆盖。

---

### Task 1: 亲属直系关系与收养环规则

**Files:**
- Create: `Assets/Scripts/Domain/FamilyFormationRules.cs`
- Modify: `Assets/Scripts/Domain/KinshipGraph.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationRulesTests.cs`

**Interfaces:**
- Produces: `FamilyFormationRules.IsDirectLineRelation(KinshipGraph graph, PersonId left, PersonId right)`，沿 `ParentChild` 与 `Adoption` 两类有向边双向判断是否处于祖先/后代直系；`FamilyFormationRules.WouldCreateAdoptionCycle(KinshipGraph graph, PersonId adopter, PersonId candidate)` 判断追加 adopter→candidate 是否会闭环。
- 两个方法对空图/空 ID 返回安全的否定结果或显式拒绝；遍历须使用 visited 集合处理既有坏环，不循环挂起。

- [ ] **Step 1: 编写失败测试**：覆盖直系血亲、直系养亲、多跳直系、方向相反、同胞关系不属于直系、候选为收养者祖先时会闭环，以及已有循环图会终止。
- [ ] **Step 2: 在 Unity Editor Test Runner 运行 `FamilyFormationRulesTests`，确认新增规则测试失败。**
- [ ] **Step 3: 实现图遍历 helper**：复用 `KinshipGraph.Links`，不改变既有事实或对外存档表示。
- [ ] **Step 4: 重跑 `FamilyFormationRulesTests`，确认全部通过。**

### Task 2: 会话命令、候选视图、固定剧本与编年史

**Files:**
- Create: `Assets/Scripts/Application/FamilyFormationView.cs`
- Create: `Assets/Scripts/Application/SC022FamilyFormationScenario.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Modify: `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- Modify: `Assets/Scripts/Domain/HistoryLedger.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationSessionTests.cs`
- Test: `Assets/Tests/EditMode/SC022FamilyFormationScenarioTests.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationChronicleTests.cs`

**Interfaces:**
- Produces: `GameSession.GetFamilyFormationView()`；`GameSession.TryRegisterMarriage(PersonId memberId, PersonId candidateId)`；`GameSession.TryAdopt(PersonId adopterId, PersonId candidateId)`。两个命令返回 `FamilyFormationCommandResult`，包含成功状态、稳定结果码、中文消息、发起人 ID 和候选人 ID。
- `FamilyFormationView` 暴露当前有效家庭成员选项、剧本允许的非成员婚姻候选与收养候选；婚姻候选按所选成员的直系亲属限制过滤。视图只读，不提交状态。
- `SC022FamilyFormationScenario` 提供稳定家族/人物 ID、原创姓名、`CreateSuccession()` 与 `IsActive(SuccessionState)`；提供至少两名成年婚姻候选和一名收养候选。

- [ ] **Step 1: 编写会话与场景失败测试**：覆盖固定 SC-022 身份、候选已注册但未入家、非场景非成员不出现、候选年龄分流、死亡/未成年发起人无资格。
- [ ] **Step 2: 编写命令失败测试**：婚姻多配偶成功、自身/重复/直系血亲或养亲被拒、同胞不额外阻止；收养儿童和成人成功、本人/配偶/重复边/亲子环被拒。
- [ ] **Step 3: 编写原子性和副作用测试**：失败前后成员、关系边、编年史、继承人、继承阶段、日期、资源和 RNG 状态一致；成功普通收养不改变继承状态。
- [ ] **Step 4: 在 Unity Editor Test Runner 运行上述新增测试并确认失败。**
- [ ] **Step 5: 实现候选投影和两个 `GameSession` 命令**：每次提交都重新验证当前状态；先完成资格、重复关系、日期、姓名和编年史条目校验，再写成员、亲属边和一条记录，保证拒绝路径不改变任何状态。
- [ ] **Step 6: 追加稳定的 `ChronicleCategory.FamilyFormation` 值与 `HistoryLedger.TryAppendFamilyFormation(...)`；摘要使用显示名，不暴露原始 ID。**
- [ ] **Step 7: 实现 SC-022 固定剧本**：不改 SC-010/013/019/020/021 的状态工厂或标题按钮。
- [ ] **Step 8: 在 Unity Editor Test Runner 重跑本任务测试，确认通过。**

### Task 3: 婚姻与收养管理面板及编年史展示

**Files:**
- Create: `Assets/Scripts/Presentation/FamilyFormationPanel.cs`
- Modify: `Assets/Scripts/Presentation/FamilyChroniclePanel.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationPanelTests.cs`
- Test: `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`

**Interfaces:**
- Consumes: Task 2 的 `FamilyFormationView`、`FamilyFormationCommandResult` 与 `GameSession` 命令。
- Produces: `FamilyFormationPanel.BindReferences(...)`、`IsOpen`、`Open()`、`Close()`、成员/候选选择与确认入口，遵循现有 `FamilyResearchPanel` 的可注入测试 seam。

- [ ] **Step 1: 编写 EditMode 测试**：面板默认关闭、开关可用；婚姻和收养候选按资格过滤；选中对象后显示既有配偶/亲属信息；取消不提交；确认只提交一次并显示成功或具体拒绝原因。
- [ ] **Step 2: 编写编年史 UI 测试**：`FamilyFormation` 类别显示中文标签，婚姻/收养标题与摘要包含人物显示名。
- [ ] **Step 3: 在 Unity Editor Test Runner 运行新增面板测试并确认失败。**
- [ ] **Step 4: 实现固定根下的独立面板**：分为婚姻和收养页；通过二次确认说明不可撤销的成员与关系变化；不改只读 `FamilyRosterPanel`。
- [ ] **Step 5: 加入 `FamilyChroniclePanel` 的家族关系类别格式化。**
- [ ] **Step 6: 重跑本任务测试，确认通过。**

### Task 4: FoundationScene 集成与标题入口

**Files:**
- Create: `Assets/Scripts/Editor/AddSC022FamilyFormation.cs`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Test: `Assets/Tests/EditMode/AddSC022FamilyFormationTests.cs`
- Modify: `Assets/Scenes/FoundationScene.unity`（仅通过事务式 Editor 生成器）

**Interfaces:**
- Produces: `AddSC022FamilyFormation.Update(Scene)`、`Validate(Scene, out string error)` 与 `CheckScenePreflight(...)`，参照 SC-021 生成器的固定 root、单 Undo group、保存前验证、异常 Undo 回滚规范。
- 标题入口调用 `TitleScreenController.StartFamilyFormationDemo()`，并启动 `GameSession.InitFamilyFormationDemoScenario()`；按钮由生成器同一事务添加与绑定。

- [ ] **Step 1: 编写 Editor EditMode 测试**：生成后只有一个固定根且为 Canvas 直接子级；面板默认隐藏、打开按钮可见；标题入口绑定 SC-022；validator 检查引用、层级和按钮；重复运行幂等；错误预检/验证失败通过 Undo 完整回滚。
- [ ] **Step 2: 在 Unity Editor Test Runner 运行 `AddSC022FamilyFormationTests` 并确认失败。**
- [ ] **Step 3: 实现生成器、标题绑定、`FoundationSceneValidator` 检查和 `TitleScreenController` 启动方法。**
- [ ] **Step 4: 运行该 Editor 测试集，确认通过；由生成器更新当前 `FoundationScene` 并保存，不手改已有对象 GUID。**
- [ ] **Step 5: 在同一 Unity Editor 运行 `FoundationSceneValidator`，确认 SC-022 集成通过。**

### Task 5: 存档往返、文档与最终验证交接

**Files:**
- Test: `Assets/Tests/EditMode/FamilyFormationSaveTests.cs`
- Create: `SC022_DEMO.md`
- Create: `docs/reports/sc022-implementation.md`
- Modify: `PROJECT_STATE.md`
- Modify: `DECISIONS.md`

**Interfaces:**
- Consumes: SC-022 场景、会话命令、编年史和现有 `SaveData` 捕获/恢复接口。
- Produces: 可按步骤操作的用户 Play Test 指南及明确区分静态检查、Unity Editor 验证和待用户验收的实现报告。

- [ ] **Step 1: 编写保存/读取测试**：未加入家庭的候选人仍被保存；成功婚姻/收养后成员身份、边方向、历史条目顺序及显示文字往返一致；旧存档仍可读取。
- [ ] **Step 2: 在 Unity Editor Test Runner 运行新增保存测试并确认失败（若现有序列化已满足则记录具体先行通过证据）。**
- [ ] **Step 3: 仅在证据表明现有结构不足时修改 `SaveData.cs`/迁移器；先说明设计差异，保持旧版本迁移兼容。**
- [ ] **Step 4: 完成 `SC022_DEMO.md`：列出启动 SC-022、登记第二位配偶、验证直系禁限/同胞规则、办理儿童或成人收养、检查编年史、保存/重载的顺序步骤和预期结果。**
- [ ] **Step 5: 更新 `PROJECT_STATE.md`、`DECISIONS.md` 与实现报告**，记录文件、规则、存档兼容、`docs/research/SC022-family-formation-reuse.md` 的复用结论、编译、EditMode 总数、Console 错误/警告、场景 Validator、Play Mode 证据；未实际执行的项目写为待验证。
- [ ] **Step 6: 在已打开的 Unity Editor 执行编译和相关 EditMode 测试，确认无编译错误、测试通过、Console 零错误，并重新运行场景 Validator；Play Mode 结果独立记录。**
- [ ] **Step 7: 交 OpenCode2 完成报告和变更列表，冻结 worktree 等待独立只读代码审查；随后提供用户 Play Test 步骤。不得提交、合并或 push，直到用户明确验收。**



---

<a id="2026-11-05-sc-023-seclusion"></a>
## 来源：docs/superpowers/plans/2026-11-05-sc-023-seclusion.md

# SC-023 隐居与后见人实施计划

> **For agentic workers:** OpenCode2 implements this plan task-by-task; a separate OpenCode worker performs the read-only review after implementation. Unity Play Test is performed by the user.

**Goal:** 在现有 Unity 项目中交付一个可试玩的 SC-023：前任家主退休交接后，可作为下一代子女的年度导师，其效果进入成长结果、存档与编年史。

**Architecture:** 保留 `SuccessionService` 的退休/交接事实；增加 SC-023 固定家族/成长状态，并在应用层通过共享资格规则允许退休的前任 `DirectKin` 家主成为导师。教育计划及结果继续使用已有领域类型与存档 DTO，不新增 schema；用事务式 Editor 生成器接入现有 `FoundationScene` 标题入口和教育面板。

**Tech Stack:** Unity `6000.6.6f1`、C#、现有 uGUI、现有 JSON 存档及 Unity Editor 工具；不加包或资产。

**Spec:** `docs/superpowers/specs/2026-11-05-sc-023-seclusion-design.md`

## 全局约束

- `GDD_v4.0.md` 为设计权威；复用唯一 Unity 项目及现有 `FoundationScene`。
- OpenCode2 是唯一代码实现者；本任务分支上保持 SC-021 的未验收分支不变。
- 不安装包、不新增场景/Prefab、不改 `Library` 或 `Temp`，保留所有 `.meta` GUID。
- 不修改 SC-020 既有剧本起始值、资格或年度结算结果。
- 场景修改只由指定的集成执行者完成；同一项目不启动第二个 Unity Editor，不对用户 Editor 项目运行 batchmode。
- 用户执行 Play Test；正式验收前不合并 `main`、不打 milestone tag。

## 评审重点

- SC-023 资格分支只能放行退休且为 `DirectKin` 的前任家主；必须由既有继承编年史的真实交接记录证明其曾任家主。SC-020 与其他场景旧规则保持一致。
- UI 预览、计划提交、结算和 `SaveDataValidator` 对同一导师资格给出相同结果，包括拒绝“退休但非前任家主”的 `DirectKin`。
- 交接失败或年度结算回滚时，不留下半完成的退休/导师/编年史状态。
- 退休导师死亡、离族或被移出家庭后，保存与读取继续稳定，并按设计回退。
- 场景生成器重跑幂等，验证失败时 Undo 回滚且不重载场景。

---

### Task 1：增加 SC-023 固定剧本与年度成长数据

**文件归属：** OpenCode2 可新增 `Assets/Scripts/Application/SC023SeclusionScenario.cs`、`SC023_DEMO.md`、SC-023 相关应用代码；仅触及既有教育模型所需的最小文件。

- 新建三代固定家族：前任家主、成年继承人（新家主）和未成年子女；唯一 ID 前缀为 `sc023`。
- 标明新家主指定继承人，开局由玩家执行退休交接；交接后旧家主为退休 `DirectKin`，继承人为 `Head`。
- 为培养对象建立既有 `CharacterDevelopmentState` 记录；SC-023 场景的孩子年龄在 0–15 岁范围内，旧家主具备固定的导师专长。
- 增加场景激活、角色名、导师专长、儿童资质和家庭环境的稳定查询；不得在应用层散落重复的 ID 比较。
- 只新增标题入口路由所需的独立场景入口，不改变 `InitNewGame`、SC-010/013/019/020/021/022 快照。

**可观察结果：** 从标题选择 SC-023 后，出现该三代家族；退休交接后名册显示旧家主已隐居、新家主已继位。

### Task 2：共享导师资格并保留存档兼容

**文件归属：** `GameSession.cs`、`EducationView.cs`、`SaveData.cs` 及必须的应用/领域规则文件；不得改存档 schema 版本。

- 为 SC-023 加入退休前任导师资格：存活、成年、当前家族成员、角色 `DirectKin`、非培养对象本人，且继承编年史证明其曾以旧家主身份参与真实交接；不得把任何退休 `DirectKin` 当作前任家主。
- 在职导师继续走已有规则；SC-023 的退休资格不得成为所有场景的全局放宽。
- `EducationMentors`、预览、`TrySetEducationPlan`、年结失效判断、`SaveDataValidator` 采用一致规则；存档校验从既有编年史 DTO 派生前任家主 ID，不升 schema。
- `SC020EducationScenario.SpecialtyOf` 与固定场景逻辑不再被误用来判定 SC-023 的年龄、属性、导师专长或家庭环境；以场景 provider/稳定路由承载两者。
- 年度计划使用既有 Mentor ID 持久化，schema 保持不变；旧档不出现新增必填字段。

**可观察结果：** SC-023 能选择由交接记录证明的隐居前任家主；SC-020 的既有在职导师仍可用，退休成员不会无条件出现在其他场景的导师列表；退休但非前任家主的 `DirectKin` 不会出现，相关存档计划会被拒绝。

### Task 3：教育 UI 与结算显示退休导师的贡献

**文件归属：** `CharacterEducationPanel.cs`、`EducationView.cs`、`GameSession.cs` 中必要的现有接口。

- 退休导师候选清楚显示「隐居家主／前任家主」身份，不出现原始 ID。
- 预览列出成长方向、导师姓名/专长及预期前后值；提交计划不提前修改属性或编年史。
- 年结后结果和编年史记录前任导师、培养方向和实际变化；年度结算仍只执行一次。
- 若导师在年结前失去资格，使用现有自由成长回退并给出准确说明；保存计划不会因失效导致存档无法读取。

**可观察结果：** 通过一轮年度培养能看出退休导师的贡献；同一计划存读档后导师身份和结果保持一致。

### Task 4：FoundationScene 集成与文档

**文件归属：** `Assets/Scripts/Editor/AddSC023Seclusion.cs`、`FoundationSceneValidator.cs`、指定的 `Assets/Scenes/FoundationScene.unity`、`SC023_DEMO.md`、`PROJECT_STATE.md`、`DECISIONS.md`、`ASSETS.md`（仅在确有新增资产时）。

- Editor 生成器使用固定 root、一个 Undo group、保存前验证、异常 Undo 回滚；不重载场景。
- 在现有标题 UI 新增「SC-023 隐居与后见人」入口，并连接独立场景工厂/现有教育面板。
- Validator 检查 SC-023 标题入口、家族研究 UI 绑定、引用与布局。
- 更新 `SC023_DEMO.md`：从标题开始、退休交接、指定退休导师、年度结算、编年史、存读档、失败条件及 Play Test 结果表。
- 更新项目状态和决策记录，明确本切片范围与证据状态；不声称 M3 已完成。

**可观察结果：** 现有 Unity Editor 中打开 `FoundationScene` 可从标题进入 SC-023，且验证器能分别报告 SC-023 行的结果。

### Task 5：顺序只读审查与用户验收交接

- OpenCode2 完成后，由另一 OpenCode worker 只读审查同一分支，报告代码/场景/文档问题和它实际取得的证据；不得编辑。
- 集成协调者检查差异、分支状态、审查结论和文档一致性；纠正项发回 OpenCode2 原任务。
- 按可用 Unity Editor 证据分别记录编译、EditMode、Console、Validator；不推断未运行结果。Play Test 步骤交给用户执行。
- 用户验收前仅 push 当前 feature branch 上获准的检查点；不 merge 到 `main`、不打 tag。

## 手工验收路径

1. 标题选择 SC-023，确认日期、三代成员、家主和指定继承人正确。
2. 退休交接到成年继承人；确认旧家主保留为退休亲属，新家主取得 `Head` 身份。
3. 打开年度培养面板，为儿童选一个方向并选择旧家主；确认预览显示导师专长贡献，提交时属性/编年史未提前变化。
4. 保存、读取并确认导师计划保留；推进到下一个年度结算点，检查确定成长结果、旧家主姓名和编年史。
5. 单独检查 SC-020 的固定起始值、在职导师和教育结果未改变；报告所有失败步骤及 Console 错误。


---

<a id="2026-11-05-sc-024-family-creed-ambition"></a>
## 来源：docs/superpowers/plans/2026-11-05-sc-024-family-creed-ambition.md

# SC-024 家训与个人抱负实施计划

**状态：** 用户已批准进入实施

> **For agentic workers:** 使用 Orca 监督流程；OpenCode2 实施代码；完成后由独立 OpenCode worker 只读复审。当前分支执行，不创建新 worktree。

**目标：** 在现有六人家族经营中加入跨代延续的家训、按家主记录的抱负进度、一次可见改革及存档/编年史支持。

**架构：** 使用独立 `FamilyDoctrineState` 保存家训与每位家主的抱负记录；`GameSession` 在既有月结事务中根据产业、研究和教育结果推进抱负。事件目录按当前家训选择议事版本，事件效果原子变更家训、关系和编年史。保存新增 v9 数据块并从 v8 迁移。

**技术栈：** Unity 6.6.6f1、C#、现有 uGUI、Unity Test Runner、当前 JSON 事件目录与存档 DTO；不增加 package。

**设计稿：** `docs/superpowers/specs/2026-11-05-sc-024-family-creed-ambition-design.md`

## 全局约束

- GDD `GDD_v4.0.md` 是设计权威；家训三项为守秘为先、义理至上、唯才是举；抱负三项为发展家业、传承家技、培养继承人。
- 使用当前 `panyeung/sc024-family-values` 分支和现有项目，不新建 Unity project 或 worktree。
- OpenCode2 负责全部 C#、JSON 事件内容、测试和 Editor 生成器代码；Scout 只做最多三项 GitHub 复用研究；不安装、克隆或复制外部代码，除非得到明确批准。
- 场景生成与保存由 lead/integration owner 在当前 Unity Editor 操作；生成器使用一个 Undo 组、保存前验证、异常时 Undo 回滚，不重载场景。
- 不运行 batchmode，不启动第二个 Unity Editor，不触碰 `Library`/`Temp`、`SampleScene.unity`、用户未跟踪文件、既有 `.meta` GUID 或 `ProjectSettings`。
- 既有 SC-010 至 SC-023 行为保持；SC-020/SC-023 教育资格仍按剧本隔离。
- 仅在当前 Editor Test Runner 验证编译/EditMode；Console、Validator、Play Mode 和用户验收分别报告。由用户执行 Play Test；未经用户明确验收不合并、不打 tag。
- 每个验证门通过后，仅提交该任务已验证文件并立即推送到 `origin/panyeung/sc024-family-values`；无关本地 WIP 不入提交。

## 复核重点

1. 家训改革与关系/编年史写入必须原子提交；编年史追加失败时，三者均回滚。由 Task 3 的 session 测试覆盖。
2. 旧家主未完成的抱负不能转给新家主；家训跨继承保留。由 Task 3 的继承测试和 Task 4 的往返存档测试覆盖。
3. 产业进度只在 `Operated` 且三类资源净和为正的月结推进；空缺、短缺或无净增长不计。由 Task 2/3 测试覆盖。
4. 研究抱负不把选择前已知的技艺计为新成果；同一技艺、重复事件或重复月结不能重复完成抱负。由 Task 2/3 测试覆盖。
5. 教育抱负只累计当前指定继承人的正向能力变化，失败/自由成长回退和非指定人物不误计；同一年龄年只结算一次。由 Task 2/3 测试覆盖。
6. v8 缺少家训数据时应确定性迁移为 `义理至上` 且无抱负；损坏、重复人物或未知家训 ID 的 v9 数据被拒绝且不覆盖原档。由 Task 4 测试覆盖。

---

### Task 1：Scout 复用研究

**文件：**
- 只读 `Assets/Scripts/Domain`、`Application`、`Events`、`Persistence`、`Presentation`、`Editor` 及相关测试。
- 只读最多三个 Unity 6 兼容的公开 GitHub 项目或官方资料；不下载整库、不安装、不复制源码。
- 研究结论写入 `docs/reports/sc024-reuse-research.md`。

**接口：**
- 输入：已批准的 SC-024 设计稿。
- 输出：按项目要求的名称/URL、许可、兼容、维护、依赖、接入成本、Reuse/Adapt/Build 决定；优先判断是否直接复用本项目现有实现。

- [ ] **步骤 1：检查当前代码与最多三个候选**

  先确认现有领域事件、状态 DTO、月结与面板是否覆盖本任务；如需外部研究，仅查看最多三个候选的 README/官方文档与许可证。

- [ ] **步骤 2：记录结论**

  验收结果：报告明确列出复用本项目或定制实现的理由；没有合适候选时说明 `Build`，不引入依赖。

- [ ] **步骤 3：由 lead 复核报告并推送**

  仅暂存报告，提交 `docs(sc024): record reuse research` 并推送当前分支。

### Task 2：领域状态与确定性抱负规则

**文件：**
- Create: `Assets/Scripts/Domain/FamilyDoctrineState.cs`
- Create: `Assets/Scripts/Domain/FamilyDoctrineState.cs.meta`
- Modify: 仅在必要时修改 `Assets/Scripts/Domain/FamilyState.cs`；不得形成两份家训事实。
- Test: `Assets/Tests/EditMode/FamilyDoctrineStateTests.cs`

**接口：**
- `FamilyCreedKind`：`SecrecyFirst`、`RighteousnessFirst`、`MeritFirst`。
- `FamilyAmbitionKind`：`DevelopHousehold`、`PassDownTechnique`、`EducateHeir`。
- `FamilyAmbitionRecord`：`PersonId HeadId`、`FamilyAmbitionKind Kind`、`int Progress`、`int Target`、`bool IsComplete`，并保留选定抱负时的已知技艺快照。
- `FamilyDoctrineState`：提供 `Creed`、`TrySetCreed(FamilyCreedKind creed, out string error)`、`TryStartAmbition(PersonId headId, FamilyAmbitionKind kind, IReadOnlyCollection<string> knownTechniqueIds, out string error)`、`TryGetAmbition(PersonId headId, out FamilyAmbitionRecord record)`、`Clone()`。
- 月结输入分别消费 `IndustryMonthReport`、新完成的 technique ID 和 `EducationYearResult` + 指定继承人 ID；只产生确定性 progress，不自行运行月结。
- 固定目标：产业 3 个符合条件的月份；新技艺 1 项；指定继承人五项能力正向增量累计 5。

- [ ] **步骤 1：写失败测试**

  fixture `FamilyDoctrineStateTests` 覆盖：三个 creed ID；初始 `义理至上`；空/未知人物和重复抱负拒绝且状态不变；每类抱负的目标进度；不符合条件的产业/研究/教育输入不推进；完成后幂等；Clone 深拷贝每代记录。

- [ ] **步骤 2：在现有 Unity Editor Test Runner 跑该 fixture，确认新测试失败**

  禁止 batchmode。预期：新增类型/成员不存在，fixture 红灯。

- [ ] **步骤 3：实现最小领域状态**

  在 `FamilyDoctrineState.cs` 实现稳定枚举、记录、验证、克隆及三个纯进度更新方法。研究抱负的技艺快照按稳定 ID 去重并序列化顺序。

- [ ] **步骤 4：重跑 fixture**

  预期 `FamilyDoctrineStateTests` 全绿，0 失败/跳过；保存 Editor Test Runner XML/日志到 `Logs/sc024-domain-tests.*`。

- [ ] **步骤 5：提交并推送**

  仅暂存领域状态、必要 `.meta` 和测试证据，提交 `feat(sc024): add family doctrine state` 并推送。

### Task 3：家族议事事件与 GameSession 月结集成

**文件：**
- Modify: `Assets/Scripts/Events/EventCatalogData.cs`
- Modify: `Assets/Scripts/Events/EventCatalogValidator.cs`
- Modify: `Assets/Scripts/Events/EventEngine.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Create: `Assets/Scripts/Application/SC024FamilyDoctrineScenario.cs` and `.meta`
- Create/Modify: `Assets/Resources/Events/event-catalog-v1.json`
- Create/Modify: `Assets/Resources/Localization/event-text-zh-v1.json`
- Test: 新增 `Assets/Tests/EditMode/FamilyDoctrineSessionTests.cs`；按需扩充既有事件目录/引擎测试。

**接口：**
- `GameSession` 暴露当前家训/当前家主抱负的只读视图，并提供 `TryStartCurrentHeadAmbition(FamilyAmbitionKind kind)`。
- `SC024FamilyDoctrineScenario.CreateSuccession()` 复用 `M2FixedScenario` 的六人 roster/日期与 ID，仅给该试玩设置独立 event scenario ID `sc024`；`GameSession.InitFamilyDoctrineDemoScenario()` 初始化该 route 和默认家训/抱负。普通 SC-013、SC-018、旧存档不触发 SC-024 事件。
- 新事件条件字段 `familyCreedId` 与条件 `FamilyCreedIs`；新事件效果字段 `familyCreedId` 与效果 `SetFamilyCreed`。非法 ID 在 catalog validation 阶段拒绝。
- 事件稳定 ID：`event.sc024.creed-council.secrecy`、`.righteousness`、`.merit`；三者以 `FamilyCreedIs` 互斥并共享 `PerGame` 一次决策语义。
- 当前 creed 的议事选项关系变化：守秘为先（正綱 +5、信正 -5）；义理至上（两人各 +5）；唯才是举（正綱 -5、信正 +5）。改革至任一其他 creed 时两人各 -5，效果同时设置新 creed。
- 各系统只在既有成功月结事务中推进当前家主抱负；新家主的进度从未选择状态开始，旧记录保留。

- [ ] **步骤 1：写失败事件和会话测试**

  覆盖三种议事事件互斥、非匹配事件不排程、只有匹配议事选项可提交、改革的 creed/关系/编年史前后值、编年史追加故障回滚、各月结系统对正确家主抱负推进、重复结算不重复计数、继承切换后家训保持而新家主不继承抱负。

- [ ] **步骤 2：在当前 Editor 跑新增 fixture，确认失败**

  预期缺少条件/effect/state 集成导致红灯；不运行全量测试。

- [ ] **步骤 3：实现事件模型、目录验证和 GameSession 事务接入**

  按设计复用 M2 六人家族做 SC-024 独立试玩 route；SC-024 事件只在 `scenario = sc024` 出现，既有 SC-013/SC-018 行为不变。维护议事选择与两个关系变更、家训变更及编年史追加保持同一回滚边界。三类抱负进度只从已有 settlement report/result 更新。

- [ ] **步骤 4：重跑事件与会话 fixture 及既有受影响回归**

  预期新增 fixture 与受影响的事件、家庭关系、产业、研究、教育和继承测试通过，0 失败/跳过；保存 XML/日志。

- [ ] **步骤 5：提交并推送**

  仅暂存事件/会话代码、JSON、测试及证据；提交 `feat(sc024): apply creed and ambition outcomes` 并推送。

### Task 4：v9 存档、迁移和往返验证

**文件：**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: 新增 `Assets/Tests/EditMode/FamilyDoctrineSaveTests.cs`，扩充迁移测试。

**接口：**
- 新 `FamilyDoctrineSaveData` 存 `creedId` 与按 head ID 排序的 `FamilyAmbitionSaveEntry[]`；每行保存稳定抱负 ID、进度/目标、完成状态和研究基线。
- `SaveData` 同时持久化活动事件路线 ID；SC-024 v9 存档往返必须恢复 `sc024`，否则议事后的事件历史会被 SC-013 场景验证拒绝。v8 及更早的普通存档迁移为无 SC-024 路线。
- `SaveData` schema 由 v8 升为 v9；规则版本按项目规则由 4 升为 5；事件内容版本按新事件内容升为 3。
- v8→v9 初始化 creed 为 `RighteousnessFirst`、抱负为空、事件路线为空；更新 presence probe、迁移路径、深拷贝、捕获、恢复和严格校验。

- [ ] **步骤 1：写存档失败测试**

  覆盖 v8 标准存档迁移、SC-019/020/021/022/023 状态随迁移保留、v9 议事后家训/各代抱负/SC-024 路线与事件记录往返、普通 SC-013 路线隔离、排序稳定、未知/重复 ID/进度越界拒绝、缺块检测、读取失败保留原档。

- [ ] **步骤 2：在当前 Editor 跑存档 fixture，确认失败**

  预期 v9 数据块缺失或迁移版本不匹配。

- [ ] **步骤 3：实现 v9 DTO、验证和 v8→v9 迁移**

  保持 schema 迁移只生成默认 creed 与空抱负，不补算历史月份或添加随机结果。

- [ ] **步骤 4：重跑存档 fixture 与既有迁移回归**

  预期 v8、v9 和已有旧版 migration fixtures 全部通过，0 失败/跳过；保存 XML/日志。

- [ ] **步骤 5：提交并推送**

  仅暂存存档代码、测试及证据；提交 `feat(sc024): persist family doctrine` 并推送。

### Task 5：家族方针面板、标题入口与场景生成器

**文件：**
- Create: `Assets/Scripts/Application/FamilyDoctrineView.cs` 与 `.meta`
- Create: `Assets/Scripts/Presentation/FamilyDoctrinePanel.cs` 与 `.meta`
- Create: `Assets/Scripts/Editor/AddSC024FamilyDoctrine.cs` 与 `.meta`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Modify: `Assets/Scripts/Editor/AddM6TitleScreen.cs`
- Modify: `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Test: `Assets/Tests/EditMode/FamilyDoctrinePanelTests.cs`、`AddSC024FamilyDoctrineTests.cs` 和 Validator 测试。
- Scene integration owner: `Assets/Scenes/FoundationScene.unity`，只通过当前 Editor 的生成器写入。

**接口：**
- Panel view 至少含 creed ID/中文名/当前议事效果、当前家主姓名、三项抱负选项、进度/目标/完成态、操作可用性与原因。
- 标题新增 `SC024FamilyDoctrineDemoButton`；固定面板根 `Canvas/FamilyDoctrineRoot`，默认 inactive。
- 生成器 `AddSC024FamilyDoctrine.Execute()` 与 `Validate()`；验证绑定、唯一根/控件、固定标题按钮、Canvas containment、HUD/编年史/名册不重叠、原场景对象数量。

- [ ] **步骤 1：写 view/panel/scene-generator 失败测试**

  覆盖面板字段与错误原因、三种 creed/ambition 中文显示、重复根拒绝、绑定验证、RectTransform containment/overlap 和包含 SC-023 按钮时标题布局。

- [ ] **步骤 2：在当前 Editor 跑新增 UI fixture，确认失败**

  预期视图、面板绑定和生成器暂不存在。

- [ ] **步骤 3：实现纯视图、面板与事务式生成器**

  `OpenCode2` 只提交生成器与 C#；由 lead 确认 Unity 已退出 Play Mode 后，在当前 Editor 中运行生成器并保存 `FoundationScene`。

- [ ] **步骤 4：运行 UI fixture、`FoundationSceneValidator` 和当前 Editor 编译**

  预期 SC-024 专项 PASS、无新增 Validator 错误、标题入口和面板可见；完整报告及当前 Console 计数记录到 `Logs`。既有无关错误必须单独说明，不能归入 SC-024 通过。

- [ ] **步骤 5：提交并推送**

  仅暂存 UI、Editor 代码、对应 `.meta`、已验证 `FoundationScene` 和证据；提交 `feat(sc024): integrate family doctrine panel` 并推送。

### Task 6：演示文档、只读复审与 Play Test 交付

**文件：**
- Create/Update: `SC024_DEMO.md`
- Create/Update: `docs/reports/sc024-implementation.md`
- Update: `PROJECT_STATE.md`、`DECISIONS.md`
- Review: 所有本任务的已提交变更只读复审，不编辑同一工作区。

**接口：**
- `SC024_DEMO.md` 提供用户逐步试玩动作、每步预期、失败/停止条件、保存/读档步骤和文本结果清单。
- 复审者必须列出 changed files、测试与 Editor 证据、剩余缺陷；所有代码返修仍由 OpenCode2 承担。

- [ ] **步骤 1：核对完整试玩路径和回归证据**

  覆盖三种家训议事、改革前后关系与编年史、每类抱负推进/完成、两次交接、v8 迁移和 v9 存读档。

- [ ] **步骤 2：更新 demo、实现报告、PROJECT_STATE 和 DECISIONS**

  分开记录 compile、EditMode 数量、Console、Validator、Play Mode 与用户验收；不得把代理或自动证据写为用户接受。

- [ ] **步骤 3：OpenCode2 请求独立只读审查**

  依项目流程由 OpenCode2 对当前任务请求 QA；通过后再由 Mochi 创建唯一的最终 sign-off 任务。审查 worker 不修改代码。

- [ ] **步骤 4：准备用户 Play Test 并冻结演示**

  所有可执行门通过后推送当前分支。用户 Play Test 由用户执行；收到明确接受前不创建最终 checkpoint、不合并、不打 tag。

- [ ] **步骤 5：在明确验收后提交最终 checkpoint**

  只在用户明确接受后提交已验证任务文件、推送分支；再按用户验收完成里程碑合并和标签。

## 验证命令与预期

- Unity Test Runner：每个任务所列 fixture 单独运行；不使用 batchmode，记录 XML、通过/失败/跳过数量。
- Unity Editor：当前项目已打开的 `6000.6.6f1` Editor 编译；记录最新 `Logs/Editor.log` compile/domain reload 证据。
- Scene Validator：`FoundationSceneValidator` 输出中 SC-024 行必须 PASS；全局错误/警告单独列出。
- Play Mode：依 `SC024_DEMO.md` 用户步骤完成；不是编译或 EditMode 结果的替代。

## 计划自审

- 覆盖设计稿中的家训持久性、事件改革、关系与历史、每代抱负、月结推进、迁移、面板、场景及用户验收。
- 每个 Task 均有明确文件、接口、失败/成功检查和 checkpoint 推送。
- 代码任务交 OpenCode2；外部复用研究由 Scout 负责；场景生成由单一 integration owner 执行；代码复审串行。


---

<a id="2026-11-07-m03-npc-autonomy"></a>
## 来源：docs/superpowers/plans/2026-11-07-m03-npc-autonomy.md

# M03 NPC Autonomy and Read-Only Inspector Implementation Plan

> **For agentic workers:** OpenCode2 implements this plan task-by-task on the current worktree after user approval. A separate OpenCode reviewer performs read-only review after implementation.

**Goal:** Add deterministic daily goal selection and execution for Masatsuna and Nobumasa, reusing M02 actions, with a read-only Inspector in `FoundationScene`.

**Architecture:** Plain C# domain rules own the two authored profiles, needs, stable Utility scores, and candidate explanations. `GameSession` advances and saves needs alongside existing `DailyActionState`, and plans only when a pilot is idle at a known location. A read-only uGUI panel projects those results; a transactional Editor generator integrates it into the existing scene.

**Tech Stack:** Unity 6.6.6f1, C#, existing Unity uGUI, `JsonUtility`, NUnit EditMode tests, existing FoundationScene validator and Editor generator patterns. No new package.

**Spec:** `docs/superpowers/specs/2026-11-07-m03-npc-autonomy-design.md`

## Global Constraints

- Stay in `C:\Users\panye\My project`; use current `main` worktree, and do not create a project, branch, or worktree.
- OpenCode2 owns all code and scene changes; do not modify `.meta` GUIDs, `Library`, or `Temp`.
- Reuse `GameSession.TryPlanDailyAction`, `DailyActionState`, the existing daily/monthly transaction, and the current scene generator/validator patterns.
- Limit autonomy to `M2FixedScenario.MasatsunaId` and `M2FixedScenario.NobumasaId`; consider only `Rest`, `Training`, and `Work`.
- Before first code change, complete the design's GitHub-first check: up to three Unity 6-compatible precedents, license/maintenance/dependency comparison, and a reuse-vs-build decision. Do not install or copy external code. Scout worker startup previously failed because its alias was unconfigured; do not retry the same failed worker-start request. Report the blocker and use an allowed read-only research route or ask for Scout configuration before implementation.
- Use the already-running project Unity Editor after verifying its `-projectPath`; never run batchmode against the open project.
- Keep compilation, EditMode, validator, Console, Play Mode, and acceptance evidence separate. No final M03 checkpoint, main merge, or tag until explicit user acceptance.

## Review Focus

- A pilot missing from a roster or at an unknown location must not receive a task; show a clear exclusion/rejection reason.
- Existing player-planned actions must not be overwritten or replanned every day; test active and interrupted actions.
- Equal Utility scores and repeated runs must choose in stable order without random or floating-point variation.
- v10 migration must preserve all existing M02 in-flight actions while initializing only valid pilot rows.
- Malformed/missing v11 autonomy rows must be rejected or safely defaulted according to current save validation conventions; no partial mutation on failed restore.

---

### Task 1: Deterministic autonomy rules

**Files:**
- Create: `Assets/Scripts/Domain/NpcAutonomy.cs` and its Unity `.meta` file.
- Create: `Assets/Tests/EditMode/NpcAutonomyTests.cs` and its Unity `.meta` file.

**Interfaces:**
- `NpcNeedValues` stores `Rest`, `Learning`, and `Duty` as validated integers from 0 through 100, with immutable `WithDailyGrowth()` and `WithCompletedAction(DailyActionKind)` results.
- `NpcAutonomyProfile` pairs a stable `PersonId` with fixed integer weights for Rest, Training, and Work.
- `NpcActionCandidate` exposes kind, eligibility, integer score, and a Chinese explanation/exclusion reason.
- `NpcAutonomyRules.Evaluate(PersonId personId, NpcNeedValues needs, bool isRegistered, bool hasKnownLocation, bool hasActiveAction)` returns all three candidates in enum order and a selected candidate only when eligible.
- `NpcAutonomyCommandOutcomeKind` has `Accepted`, `Deferred`, `Rejected`, and `Interrupted`; `NpcAutonomyDecisionRecord` stores date, pilot ID, outcome, action, trigger, and reason for the bounded in-session log.
- `NpcAutonomySnapshot` exposes the pilot's durable needs, fixed profile, current task/status, latest evaluation, and in-session decision log as read-only values.

- [ ] **Step 1: Add domain tests** for profile membership, need bounds, each action matching its need, excluded active/unknown/unregistered pilots, and deterministic score ties.
- [ ] **Step 2: Run the focused EditMode tests** in the open Unity Editor and confirm the new cases fail before implementation.
- [ ] **Step 3: Implement the pure rules** with these initial constants, all covered by tests:
  - Daily growth before action resolution: Rest `+4`, Learning `+3`, Duty `+3`, each capped at 100.
  - Completed action reduction after that day's growth: matching need `-40`, floored at 0.
  - Profiles: Masatsuna weights Rest/Training/Work `1/2/1`; Nobumasa `1/1/2`.
  - Initial needs: Masatsuna `40/75/45`; Nobumasa `45/40/75` (Rest/Learning/Duty).
  - Score: matching need × profile weight. Equal scores break by `DailyActionKind` ascending.
  - Planned durations: Rest `1`, Training `2`, Work `3` days.
  - These constants are proposal details delegated by the approved design and should not become serialized user configuration.
- [ ] **Step 4: Run focused tests again**; assert identical candidate ordering and scores for identical inputs.

### Task 2: GameSession execution and v11 persistence

**Files:**
- Modify: `Assets/Scripts/Application/GameSession.cs` to own pilot need state, initialize pilots only when registered, update needs in the existing successful day-advance path, plan idle pilots through `TryPlanDailyAction`, and notify observers after committed changes.
- Modify: `Assets/Scripts/Persistence/SaveData.cs` to add the v11 autonomy DTO and capture/restore helpers.
- Modify: `SaveDataPresenceProbe` in `Assets/Scripts/Persistence/SaveData.cs` so v11 JSON root-field presence is checked before deserialization defaults can hide an omitted autonomy block.
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs` to migrate v10 saves to v11 without changing `dailyActions`.
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs` to validate v11 IDs, bounds, duplicates, and roster membership; bump `CurrentVersion` from 10 to 11.
- Modify: `Assets/Tests/EditMode/GameSessionSaveTests.cs`, `SaveDataValidatorTests.cs`, and create `Assets/Tests/EditMode/NpcAutonomySessionTests.cs` with Unity `.meta`.

**Interfaces:**
- `GameSession.TryGetNpcAutonomy(PersonId personId, out NpcAutonomySnapshot snapshot)` returns needs, profile, current action/state, latest candidates, and recent decision records.
- `NpcAutonomyStateSaveData.Capture(...)` and `.TryRestore(...)` serialize only durable need values and pilot IDs; recent decision logs are session-only.

- [ ] **Step 1: Add tests** for distinct initial selections, the four command outcome kinds, no override of player-planned actions, one plan per idle/completed pilot, interruption-triggered re-evaluation, no replan on ordinary day while active, and no mutation on rejected planning.
- [ ] **Step 2: Add save tests**: v10 migration initializes registered pilot needs while byte-for-byte logical M02 active-task fields stay the same; v11 roundtrip restores needs plus in-flight tasks once; malformed IDs/ranges/duplicates fail validation.
- [ ] **Step 3: Integrate autonomy in `GameSession`** after existing day/action resolution succeeds; order is daily need growth → action progression → matching need reduction for completed actions → evaluate idle pilots in ordinal `PersonId` order → schedule with the existing public action API. Failed date/month transactions must restore autonomy needs, decisions, and actions together.
- [ ] **Step 4: Capture and restore v11 state** through `GameSession.CaptureSave()` / `TryApplySave(...)` and `SaveDataMigrator`; older saves get default needs only for registered pilots, and an existing action remains authoritative so migration/load never schedules a duplicate.
- [ ] **Step 5: Run focused EditMode suites** (`NpcAutonomySessionTests`, `GameSessionSaveTests`, `SaveDataValidatorTests`, and `M02DailyActionTests`).

### Task 3: Read-only Inspector and scene integration

**Files:**
- Create: `Assets/Scripts/Presentation/NpcAiInspectorPanel.cs` and `.meta`.
- Create: `Assets/Scripts/Editor/AddM03NpcAiInspector.cs` and `.meta` using existing transactional scene-generator conventions.
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs` to require/validate the single M03 root and its bindings.
- Create: `Assets/Tests/EditMode/NpcAiInspectorPanelTests.cs` and `AddM03NpcAiInspectorTests.cs`, with `.meta` files.
- Modify: `Assets/Scenes/FoundationScene.unity` and its existing `.meta` must retain their GUIDs.

**Interfaces:**
- `NpcAiInspectorPanel.Bind(GameSession session)` subscribes to `GameSession.SessionStateChanged` and renders `NpcAutonomySnapshot`; `Open`, `Close`, and `Refresh` only affect view state.
- `AddM03NpcAiInspector.Update(Scene scene)` adds or repairs exactly one `Canvas/M03NpcAiInspectorRoot`; `Validate(Scene scene, out string error)` checks all serialized references.

- [ ] **Step 1: Add tests** proving both pilots render needs/state, all three candidates render score or exclusion reason, logs show trigger/outcome, UI controls do not call schedule/interrupt/replan, and repeated scene integration keeps exactly one root.
- [ ] **Step 2: Implement panel and generator** with a pilot selector, scrollable diagnostics, current-action state and recent-log list; only selector/expand/close controls may mutate presentation state.
- [ ] **Step 3: Integrate into `FoundationScene`** through the Editor generator, then run `FoundationSceneValidator` and focused panel/generator EditMode tests.

### Task 4: Review, Unity verification, and handoff

**Files:**
- Modify: `PROJECT_STATE.md` and add `docs/reports/m03-npc-autonomy-implementation.md` after evidence is collected.
- No final milestone checkpoint, merge, or tag in this task.

- [x] **Step 1: Complete the pre-implementation GitHub report** in `docs/research/M03-npc-autonomy-prior-art.md` (three precedents, URLs, licenses, Unity compatibility, activity, dependencies, integration effort, reuse/adapt/build choice). Decision: build on existing code; no package or external code. No `DECISIONS.md` package entry is needed.
- [ ] **Step 2: Request sequential read-only OpenCode review** after OpenCode2 completes; address actionable defects on the same task and re-review.
- [ ] **Step 3: Verify in the already-running Unity Editor** after confirming its project path: compile, focused EditMode tests, full EditMode tests, scene validator, Console counts, then Play Mode in `FoundationScene` for both distinct action paths, continued action, interruption/replan explanation, and read-only Inspector.
- [ ] **Step 4: Delete temporary Computer Use screenshots immediately after inspection** and remove any remaining screenshots from the dedicated temporary cache.
- [ ] **Step 5: Record exact files, tests/results, Editor PID/project path, Console counts, validator result, Play Mode path/evidence, and remaining M01/M02 acceptance status**; wait for explicit M03 acceptance before final milestone checkpoint, main merge, or tag.


---

<a id="2026-11-07-ui-character-action"></a>
## 来源：docs/superpowers/plans/2026-11-07-ui-character-action.md

# For agentic workers: execute this plan on the current main checkout. Do not create a branch or worktree, and do not implement rules beyond this plan.

# Character Profile and Daily Actions UI Slice

> **范围更新（2026-11-07）：** 此计划仍记录 M02 单页首片。用户已将范围扩展为五张参考图对应页面；后续实现按 [五页实现计划](design-and-planning-history.md#2026-11-07-ui-reference-gallery) 执行。

## Goal
Adapt the existing M02 daily-action screen toward design-gallery page 02: a selected-character profile alongside action planning, in the game's established widescreen Japanese-paper / indigo / vermilion visual language. Keep current M02 action behavior and the M03 Inspector read-only.

## Architecture
Reuse `DailyActionPanel`, `NpcAiInspectorPanel`, `GameSession.DailyActions`, `GameSession.FamilyRoster`, and existing transactional Unity scene generators. Profile data must come from current session/domain values; do not invent stats or relationship values. Keep the Inspector a distinct developer-only, read-only surface. Existing Unity UI and built-in font are sufficient; no new package or external art.

## Tech Stack
Unity 6.0, C#, uGUI, Unity Test Framework. Existing scene generator patterns use one Undo group, validate before save, and Undo rollback on exceptions.

## Spec
- Present the selected family member as the focus, with existing display name, age, kinship/role, location or travel state, and current action/progress where available.
- Keep family-member selection and the existing Rest, Training, Work, Travel, duration, interrupt, and replan behavior intact.
- Reshape the panel into clear character-information and action-planning areas; use the existing panel palette and typography conventions. Remain usable at the 1280x720 test canvas without clipped or overlapping controls.
- Preserve the M03 Inspector's distinct developer/debug role, existing open/close behavior, pilot selection, and read-only boundary.
- Reuse any existing portrait assets if available; repository inspection found no character portrait art. Do not create or import placeholder art as part of this slice.
- Update the M02 transactional generator and its validation for the new layout. Keep all generated objects under the existing fixed root and preserve scene objects/GUIDs.
- Add or update EditMode assertions for selected-profile rendering, action behavior regression, scene bounds, and generator rollback/idempotency as relevant.
- Update the UI design spec/implementation plan with the delivered scope and any remaining work.

## Global Constraints
- OpenCode2 owns code changes. Work only in `C:\Users\panye\My project` on the current checkout. Preserve unrelated dirty WIP (`docs/superpowers/specs/2026-11-07-m04a-personal-memories-design.md`, `graphify-out/`, `nul`).
- Before Unity interaction, inspect Unity Editor process PID and `-projectPath`; use only the already-running Editor for this project. Do not use batchmode, do not touch `Library`/`Temp`, and preserve all `.meta` files and GUIDs.
- Do not change gameplay rules, action/session/save semantics, dependencies, assets, or ProjectSettings.
- Record compile, EditMode, scene-validator, Console, Play Mode, and acceptance evidence separately. Do not claim runtime evidence without running the existing Editor.
- Do not commit until the task's applicable verification gates pass. Then commit only files for this task and push the current branch. Do not merge, tag, or claim milestone acceptance without explicit user acceptance.

## Review Focus
- Profile content is sourced from real session state and remains synchronized on selection/date/action changes.
- Existing M02 commands, duration checks, feedback, travel gating, interrupt and replan semantics are unchanged.
- No profile or Inspector path mutates game state beyond the existing M02 action commands.
- Every interactive element remains within the panel at 1280x720; generated scene structure validates and rolls back cleanly.
- M03 Inspector remains read-only, hidden by default, usable, and distinct from player-facing profile content.

## Delivery status (2026-11-07)
- Delivered on the current `main` checkout: selected-member profile column, two-column M02 panel layout, updated transactional generator/validator, and EditMode coverage.
- Verification: 23/23 focused EditMode tests pass; M02 and M03 scene validation pass; Console 0 errors; Play Mode profile, action progress, and read-only Inspector observed in the existing Editor. See `docs/superpowers/specs/2026-11-07-ui-character-action-design.md` for evidence and remaining work.
- Remaining: optional portrait art (none in the repository), the other four gallery pages, and explicit user acceptance before merge/tag.


---

<a id="2026-11-07-ui-reference-gallery"></a>
## 来源：docs/superpowers/plans/2026-11-07-ui-reference-gallery.md

# 五张 UI 参考图实现计划

> **For agentic workers:** Execute this plan in the existing checkout. No new branch or worktree. Each page must preserve its existing domain commands and save behavior.

**Goal:** 将五张用户提供的页面设计图逐页实现为保留真实数据和交互的 Unity uGUI 页面。

**Architecture:** 共用 `SengokuUiTheme` 颜色、字体、纸纹和按钮样式；页面继续由现有 Presentation 控制器承接数据/命令，Editor 生成器负责事务性布局；增加一个轻量页面导航只负责切换已有视图。没有必要新增领域层或存档结构。

**Tech Stack:** Unity 6.6.6f1、C#、uGUI、现有 Unity Test Framework。

**Spec:** `docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`

## Global Constraints

- 保留当前 `main` 工作区；不建 branch/worktree，不覆盖既有 M04A/graphify/nul WIP。
- 不修改领域规则、存档格式、事件目录、依赖、ProjectSettings、Library 或 Temp。
- 只使用用户提供的设计图及其装饰裁切；列在 `ASSETS.md`。
- Unity 仅使用 PID 91392（若仍指向本项目）；每次交互前重新核验 Editor PID 和 `-projectPath`。
- 修改场景时使用单一固定根、单一 Undo 组、保存前验证和异常回滚。
- 每个页面分别记录编译、EditMode、Validator、Console 与 Play Mode 证据；截图立即检查后删除。

## Review Focus

- 页面切换不留下多个模态层互相拦截输入；由共享导航测试切换后只有目标页面接收射线。
- 无真实数据的示例字段不应显示伪造值；逐页检查绑定来源。
- 既有按钮命令和存档路径保持原样；用对应的既有 Panel tests 覆盖。
- 1280×720 下文本/按钮不裁切且布局不重叠；Validator 和尺寸测试逐页覆盖。
- 引用图裁切不可带入残留文案/边框；在 Unity Editor 内实际检查每个导入 Sprite。

---

### Task 1: 完成人物档案与日常行动页（参考图 1）

**Files:** `Assets/Scripts/Presentation/DailyActionPanel.cs`, `CharacterDailyPageView.cs`, `SengokuUiTheme.cs`, `Assets/Scripts/Editor/AddM02DailyActionPanel.cs`, `Assets/Tests/EditMode/M02DailyActionPanelTests.cs`, `ASSETS.md`。

- [ ] 修复/完成现有 M02 WIP；正确绑定真实会话字段和当前命令。
- [ ] 使用参考图布局：档案、关系、经历标签；人物/关系区；行动卡、进度、目的地和确认操作；独立 AI 调试栏。
- [ ] 裁切并检查人物/旅行装饰图；不把参考图中的姓名或数值当真实游戏状态。
- [ ] 在 EditMode 覆盖布局绑定、动态刷新、操作回归、生成器幂等/回滚/边界。
- [ ] 运行本页 focused EditMode 与场景 Validator。

### Task 2: 家谱与继承页（参考图 2）

**Files:** `Assets/Scripts/Presentation/FamilyRosterPanel.cs`, `SuccessionPanel.cs`, `FamilyTreeGraphBuilder.cs`, `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`, `AddSC010SuccessionPanel.cs`, 对应测试。

- [ ] 将真实家谱连线/世代卡与现有继承候选、摄政/让位决策组合到同一页面构图。
- [ ] 保留多人/无上限亲属图的滚动、关系类型、当前继承人和全部既有继承命令。
- [ ] 在 EditMode 覆盖图布局、选中继承人联动、决策调用及画布边界。

### Task 3: 家族内务页（参考图 3）

**Files:** 当前家族/产业/事件/月结 Presentation 组件、对应 Editor 生成器、`FoundationSceneValidator.cs`、对应 EditMode tests、`ASSETS.md`。

- [ ] 复用真实家族名册、任命、产业和当前事件/待决视图，组成左家主信息、中部内务、本月事件、右侧动向/待决区。
- [ ] 不创建缺少领域来源的“天下动向”数值或待决事项。
- [ ] 覆盖空列表、单条/多条事件与决策卡的布局及按钮绑定。

### Task 4: 地图与出行页（参考图 4）

**Files:** `Assets/Scripts/Presentation/WorldMapController.cs`, `Assets/Scripts/Editor/CreateM2WorldMap.cs`, `UpdateM2WorldMapLayout.cs`, `WorldMapLayout.cs`, `Assets/Tests/EditMode/WorldMapUiTests.cs`, `CreateM2WorldMapGeneratorTests.cs`。

- [ ] 按左行程栏、中地图、右情报栏重排现有地图与目的地选择。
- [ ] 路线/天数/抵达日期从现有 `WorldMapRules` 与 `GameSession` 派生；路线和出行按钮真实执行。
- [ ] 覆盖目的地/无路由/地图点击/尺寸与生成器回滚。

### Task 5: 事件抉择页（参考图 5）

**Files:** `Assets/Scripts/Presentation/EventChoicePanel.cs`, `Assets/Scripts/Editor/AddSC005EventChoicePanel.cs`, `EventChoicePanelTests.cs`, `AddSC005EventChoicePanelTests.cs`, `ASSETS.md`。

- [ ] 采用左导航、中央事件插画/叙事/选项、右待处理事项构图。
- [ ] 事件内容、选项可用条件和结果继续来自事件目录与 `GameSession.TryChooseEventOption`。
- [ ] 覆盖多选项、禁用/可用状态、反馈、待决队列和输入层级。

### Task 6: 全套集成验证

**Files:** `FoundationScene.unity`, `FoundationSceneValidator.cs`, `docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`。

- [ ] 页面导航逐页切换，不改变游戏状态，也不留下隐藏但阻挡射线的面板。
- [ ] 在当前 Unity Editor 运行全量 EditMode、场景 Validator，并单独记录 Console。
- [ ] Play Mode 核验五页主要交互和视觉；清理所有 Computer Use 截图。
- [ ] 仅提交本任务文件并 push 当前分支；用户最终确认之前不打 tag、不做额外 milestone 宣称。

## Continuation log — 2026-11-08

- 修复事件空队列页的中文文字未渲染：运行时标签改用 Unity 内置 `LegacyRuntime.ttf`，避免系统动态字体在此 Canvas 中不出字；现有事件面板和领域状态未变。
- 继续收敛全局框架：生成器和页面导航运行时均将底色设为深靛色、左导航设为不透明深色栏；运行时在 Canvas 最底层放置不接收点击的共享背景，避免切页时露出旧蓝底。EditMode 覆盖运行时主题和生成器颜色。
- Unity Test Runner EditMode 再次全量通过：1293/1293，0 失败、0 跳过。Play Mode 已在五页间切换，确认共享深色背景持续显示；完整地图主视觉、家族内务的多栏信息和家谱人物肖像仍未复刻到参考稿精度。
- Unity Test Runner EditMode：1293/1293 通过，0 失败、0 跳过。Play Mode 已逐页查看人物、家谱、家族内务、地图和事件页；事件空态文字可见。
- 五页仍未达到参考稿的版式和素材精度。地图缺少完整地图画面，家族内务/事件布局及全局导航仍需继续完善；本次不标记整项完成。
- 当前场景保持未保存；工作区包含无关脏改动，不保存或提交整个场景。

### Map illustration slice — 2026-11-08

- Added the owner-provided central map crop (Assets/Resources/ReferenceArt/map_illustration.png) behind the existing map-page controls. The image does not receive raycasts; the original route and travel controls remain the interaction layer.
- EditMode coverage checks resource presence, bottom-sibling placement, and raycast transparency. Full Editor verification and a Play Mode visual check remain pending for this slice.
- This improves only the central map illustration. The reference's left travel column, right intelligence panel, map markers and route artwork are still not fully reproduced.


---

<a id="2026-11-08-start-screen"></a>
## 来源：docs/superpowers/plans/2026-11-08-start-screen.md

# 开始页面实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Implement with OpenCode2 on the current workspace; if OpenCode2 cannot proceed, the project lead takes over. Track steps with checkboxes.

**Goal:** Replace the crowded startup overlay with an illustrated two-action main menu and move the fixed SC-013 and SC-010/SC-020–SC-024 entries to a scenario-selection page.

**Architecture:** Keep one full-screen `M6TitleRoot` overlay and existing `TitleScreenController`; add main-menu and scenario-selection subroots, preserving the existing scenario-start methods and Continue save path. Generate a clean background plate as a separate Sprite and build every label, button, and panel as live uGUI controls. Upgrade only a validated legacy title hierarchy through the existing transactional scene generator.

**Tech Stack:** Unity 6.6.6f1, C#, uGUI, existing `SengokuUiTheme`, Editor Test Runner.

**Spec:** `docs/superpowers/specs/2026-11-08-start-screen-design.md`

**Current progress (2026-11-08):** OpenCode2 implementation and original art are committed and pushed in `220b44f`. Unity 6000.6.6f1 compiled the latest source, the full EditMode suite passed 1343/1343, and the background PNG imported. The user authorized saving the current FoundationScene; M6 integration and the full scene validator passed. Play Mode launched SC-013 and SC-010 standard successfully. The saved scene stays local because it contains existing M02 WIP that cannot be safely included in the M6 push.

## Global Constraints

- Stay in `C:\Users\panye\My project`; do not create a Unity project, branch, or worktree.
- Dispatch implementation to OpenCode2 through supervised Orca orchestration; after implementation, dispatch a separate read-only OpenCode review. The project lead owns integration and Unity gates in the existing Editor.
- Reuse Unity 6.6.6f1 and uGUI; do not add a package.
- Preserve all `.meta` files/GUIDs and do not touch `Library` or `Temp`.
- Keep Continue on the existing single-slot load path; do not overwrite user saves during Play Mode verification.
- Scene generation uses one Undo group, validates before saving, rolls back on exceptions, and does not reload scenes for rollback.
- `Assets/Scenes/FoundationScene.unity` already has uncommitted WIP. Inspect and preserve that diff before integration; never discard it or bypass a dirty-scene preflight silently.
- After each applicable verification gate passes, commit and push only this task's verified files to the current branch (`main`); do not merge or tag before explicit milestone acceptance.

## Review Focus

- Missing, corrupt, or unsupported-version save: Continue remains disabled or reports the existing error without mutating the slot or session. Test in `TitleScreenControllerTests`.
- Clicking “开始新游戏” only opens scenario selection: the current date, family, and title overlay remain unchanged. Test the real button listener in `TitleScreenControllerTests`.
- Returning from scenario selection or calling `ShowTitle()` restores the main menu and hides the selector. Test both transitions in `TitleScreenControllerTests`.
- Every pre-existing scenario remains reachable once and starts its matching initialization path. Validate bindings in `AddM6TitleScreenTests`; retain direct scenario-start tests in `TitleScreenControllerTests`.
- At 1280×720 and 1920×1080 the main menu, selector, status, and button hit areas remain inside the Canvas and readable. Check generator geometry in EditMode and inspect both aspect ratios in Play Mode.

---

### Task 1: Generate and register the clean title background

**Files:**
- Create: `Assets/Art/UI/start_screen_background.png`
- Create by Unity import: `Assets/Art/UI/start_screen_background.png.meta`
- Modify: `ASSETS.md`

**Interfaces:**
- Produces a 16:9 art-only background Sprite for `AddM6TitleScreen`; reserve the left third for the indigo menu overlay, show the dusk manor/castle-town on the right, and include no text, buttons, UI, or baked-in logo.

- [x] Generate the scene plate using the approved concept as style guidance; do not use the full concept mockup as runtime art.
- [x] Import it as one UI Sprite in the existing Unity Editor and verify `AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/start_screen_background.png")` resolves.
- [x] Add its source and use to `ASSETS.md` as original AI-generated art; add no package decision to `DECISIONS.md`.

### Task 2: Separate main-menu navigation from scenario starts

**Files:**
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Test: `Assets/Tests/EditMode/TitleScreenControllerTests.cs`

**Interfaces:**
- Add `BindScenarioSelection(GameObject mainMenuRoot, GameObject scenarioSelectionRoot, Button fixedScenarioButton, Button backButton)`.
- Add `ShowScenarioSelection()` and `ShowMainMenu()`.
- Keep `StartNewGame()`, `TryContinue()`, and all existing `Start*Demo()` public methods and their initialization behavior unchanged.

- [x] Add `ScenarioSelectionButton_OpensSelectorWithoutStartingOrMutatingSession`; invoke the bound main-menu button and assert selector active, main menu visible/covered as designed, title still visible, and date/family unchanged.
- [x] Add `ScenarioSelectionBackButton_ReturnsToMainMenu` and `ShowTitle_ResetsToMainMenu`; assert the title overlay stays open and the selector is inactive.
- [x] Route the main-menu “开始新游戏” listener to `ShowScenarioSelection()`; bind a selector “固定三代剧本” button to existing `StartNewGame()`.
- [x] Bind/unbind the new fixed-scenario and back-button listeners idempotently with the existing `BindButtons` lifecycle.
- [x] Keep Continue status and `TryContinue()` semantics unchanged; run the new focused controller tests plus existing missing/corrupt/unsupported-save and scenario initialization tests.

### Task 3: Generate and safely upgrade the title UI hierarchy

**Files:**
- Modify: `Assets/Scripts/Editor/AddM6TitleScreen.cs`
- Test: `Assets/Tests/EditMode/AddM6TitleScreenTests.cs`
- Integrate: `Assets/Scenes/FoundationScene.unity` through `Tools > Sengoku Chronicle > Add M6 Title Screen`

**Interfaces:**
- Keep `AddM6TitleScreen.Update(Scene)` and `Validate(Scene, out string)` as the single generator/validator entry points.
- New stable names: `M6MainMenuRoot`, `M6ScenarioSelectRoot`, `SC013FixedScenarioButton`, and `M6ScenarioBackButton`; retain existing scenario button names and controller bindings unless validation proves a safe rename is needed.

- [x] Update `Updater_AddsAndBindsTitleAndSummary` to assert the full-screen background Sprite, menu roots, Continue/New Game controls, the new fixed SC-013 button, Back button, and all seven existing buttons (SC-010 standard/crisis plus SC-020–SC-024) are correctly parented and bound.
- [x] Update `Validator_RejectsMissing...` coverage for a missing page root, missing fixed entry, duplicate entry, wrong background Sprite, and invalid required button binding.
- [x] Add a legacy-layout migration test: a complete, valid current title hierarchy upgrades in one Undo group; a partial/unknown hierarchy still returns `ExistingPanel` without deleting or saving anything.
- [x] Generate the main menu with the approved title, proposed subtitle, two actions, `SengokuUiTheme` colors, existing seal, and separate background Sprite. Generate a scenario selector with one fixed SC-013 entry and the seven existing SC-010/SC-020–SC-024 entries, grouped by role and using their existing start methods.
- [x] Extend `Validate` to check unique required nodes/bindings, title-root sibling order, inactive boot state, Sprite assignment, no stale demo buttons on the main-menu subroot, and safe Canvas bounds.
- [x] Preserve the title root, `TitleScreenController`, `DemoSummaryPanel`, and map summary object during a validated legacy upgrade; rebuild only owned title UI children with `Undo.DestroyObjectImmediate`. Roll back the whole group on validation failure and save only after validation passes.
- [x] Run `AddM6TitleScreenTests` in EditMode, including idempotence and rollback tests. A byte-identical pre-integration scene backup was verified; after explicit user authorization, run the generator in the current project Editor and confirm `FoundationScene` validation passes.

### Task 4: Full verification and handoff

**Files:**
- Verify: all files changed by Tasks 1–3
- Update if necessary: `docs/superpowers/specs/2026-11-08-start-screen-design.md` and `ASSETS.md`

- [x] Confirm the running Unity Editor's `-projectPath` is the current project; never run batchmode against it.
- [x] Confirm Unity compilation succeeds, then run the full EditMode suite in the Editor Test Runner with zero failures.
- [x] Run `Tools > Sengoku Chronicle > Validate Foundation Scene`; record result and Console counts separately. Result: PASS, 0 validator errors. At initial Play Mode entry, visible Console counters were 0/0/0; post-launch counters were not rechecked.
- [x] In Play Mode, verify title startup, New Game to selector, Back to main menu, SC-013 launch, SC-010 standard launch, and access to all SC-020–SC-024 entries. No save was overwritten. Continue was not clicked because a save was detected; no-save Continue remains an EditMode-only check.
- [ ] Inspect 1280×720 and 1920×1080 for cropping, contrast, button hit areas, and safe margins. Full HD (1920×1080) only was inspected; Play Mode was exited and inspected screenshots deleted.
- [x] Request a separate read-only OpenCode review after implementation; address findings and rerun affected tests.
- [x] Update the design/report with actual compile, EditMode, validator, Console, and Play Mode evidence. The implementation checkpoint `220b44f` was pushed to `origin/main`. The scene integration remains unstaged to avoid mixing existing M02 WIP; do not tag before explicit milestone acceptance.

## Self-review

- Spec coverage: the two main actions, conditional Continue status, scenario selector, original background art, palette/material direction, 16:9 safe area, separate interactive uGUI controls, no settings/quit additions, and retained save/scenario behavior are each covered above.
- Scene safety: the generator upgrades only a recognized legacy layout, preserves controller/summary objects, validates before save, and rolls back without scene reload.
- No new Unity package, data model, save format, game rule, or full-screen baked UI image is introduced.


---

<a id="2026-11-08-town-overview-ui"></a>
## 来源：docs/superpowers/plans/2026-11-08-town-overview-ui.md

# Town Overview UI Implementation Plan

> **For agentic workers:** Follow the project Orca workflow; OpenCode2 owns code and scene-generator implementation. The lead owns art generation, scene integration, and Unity Play Mode sign-off.

**Goal:** Replace the placeholder town menu with a parchment-and-indigo overview that gives Kiyosu, Atsuta, and Nagoya distinct visual identities while opening existing NPC and mission interactions.

**Architecture:** Keep `Canvas/M2WorldMapRoot/TownMenuRoot` as the single town UI root. Add a small presenter bound to `GameSession` and existing controllers, and a transactional Editor updater that adds the layout beneath the current root without replacing unrelated scene content. Select one of three original text-free illustrations by current location; treat art as atmosphere only.

**Tech Stack:** Unity 6000.6.6f1, C#, uGUI, existing editor Undo/validation patterns; no new packages.

**Spec:** `docs/superpowers/specs/2026-11-08-town-overview-ui-design.md`

**Reuse research:** `docs/research/town-overview-ui-reuse-2026-11-08.md`

## Global Constraints

- Treat `GDD_v4.0.md` as authoritative; old M4a/M4b documents are archival.
- Preserve dirty user files, Unity `.meta` GUIDs, and all existing scene objects; do not touch `Library`, `Temp`, packages, or unrelated scenes.
- Keep town labels and actions tied to current session data and existing controls; show honest empty states where no source data exists.
- OpenCode2 implements code. The lead generates/records original art, integrates the scene in the existing Editor, and signs off Play Mode.
- Stay on the existing `main` worktree. The current unrelated M02 dirty files are outside this plan and must not be staged or changed.

## Review Focus

- Location changes: town heading and displayed residents must reflect `GameSession.CurrentLocation`.
- Empty or unsupported rumor/mission data: show an explicit empty state; never fabricate content.
- Open/close/back transitions: town overview is restored and return-to-map does not advance time or change location.
- Existing roster, dialogue, and mission panels: retain their current entry points and close behavior.
- 1920×1080 and 1280×720: all text, cards, and controls remain visible and clickable without overlap.

## Files and Ownership

- Create: `Assets/Scripts/Presentation/TownOverviewPanel.cs` and matching `.meta` through Unity.
- Create: `Assets/Scripts/Editor/UpdateTownOverview.cs` and matching `.meta` through Unity.
- Modify: `Assets/Scripts/Presentation/WorldMapController.cs` to refresh the overview on town entry and expose existing NPC/mission entry points without auto-opening the mission overlay.
- Modify: `Assets/Scripts/Presentation/SengokuUiArt.cs` to expose the three town illustrations through the existing null-safe generated-art loading pattern.
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs` to validate the town overview hierarchy and bindings.
- Modify: `Assets/Tests/EditMode/WorldMapUiTests.cs` and/or a focused town overview test file for source binding, navigation, and empty states.
- Modify: `ASSETS.md` to document the generated town illustration.
- Create: `Assets/Resources/Generated/town_kiyosu.png`, `town_atsuta.png`, and `town_nagoya.png`, each with Unity importer `.meta`.
- Modify `Assets/Scenes/FoundationScene.unity` only through the lead-run updater in the already-open Editor, after preflight confirms the scene is clean; keep the existing root and objects.
- Do not edit currently dirty `FoundationScene.unity`, `AddM02DailyActionPanel.cs`, `CharacterDailyPageView.cs`, `DailyActionPanel.cs`, `ReferenceGalleryNavigator.cs`, or `M02DailyActionPanelTests.cs` as part of the implementation worker task.

## Tasks

### Task 1: Generate and record distinct town illustrations

- [ ] Generate three original, text-free Sengoku town illustrations in a consistent aged-paper, indigo, vermilion, antique-gold, ink-and-watercolor style, each with a distinct composition: Kiyosu castle-town skyline; Atsuta shrine approach and waterside; Nagoya roadside market and low-hill setting.
- [ ] Place them at the three `Assets/Resources/Generated/town_*.png` paths and record individual intended uses, generation source, and license/use notes in `ASSETS.md`.
- [ ] Confirm each image has a low-detail text-safe overlay area, remains distinct at both target resolutions, and contains no embedded labels or copied commercial art.

### Task 2: Implement presentation and scene updater through OpenCode2

- [ ] Inspect `WorldMapController`, `TownNpcDialoguePanel`, existing mission panels, `CreateM2WorldMap`, and updater/test conventions; do not add an external dependency.
- [ ] Add `TownOverviewPanel` to select the matching `SengokuUiArt` illustration for `GameSession.CurrentLocation`, render residents from `TownNpcAssignments.GetNpcs(location.Value)`, display an explicit empty state when no supported local rumor data exists, open the existing NPC roster and `SupplyShortageMissionPanel`, and return through `WorldMapController.ReturnToMap()`.
- [ ] Change town entry so the overview is shown first and the existing supply/mission panel opens only from its overview button; closing NPC/mission panels leaves the overview visible.
- [ ] Add an idempotent `UpdateTownOverview` editor action under the existing `TownMenuRoot`; use one Undo group, validate before save, roll back only its own changes on failure, and never reload the scene for rollback.
- [ ] Add focused EditMode coverage for all three location-to-art mappings, resident binding, empty states, existing panel navigation, and no time/location mutation when returning to the map.
- [ ] Extend `FoundationSceneValidator` to check the fixed root, required controls, artwork mappings/import settings, and non-overlapping responsive regions.
- [ ] Run the focused EditMode tests and report compilation, test totals, Console counts, and scene-generator evidence separately. Do not edit or save `FoundationScene.unity`.

### Task 3: Lead integration and runtime sign-off

- [ ] Verify the active Unity Editor `-projectPath` is this repository and confirm the scene has no dirty user changes before integration.
- [ ] Run the updater in the existing Editor, validate the resulting scene, and save only the generated town overview under the existing root.
- [ ] Check each city's distinct artwork at 1920×1080 and 1280×720, then run Play Mode through arrival in all three towns, NPC entry and return, mission entry and return, and return to map; record runtime evidence separately from EditMode results.
- [ ] After applicable verification gates pass, commit only verified town UI files on the current branch and push to `origin/main`; do not include unrelated M02 WIP.

## Execution update — 2026-11-09

- Three city illustrations, the town overview presenter, scene updater, bindings, and scene validator are present. The existing Editor saved the overview under `TownMenuRoot`; its validator reported `PASS; Errors: 0`.
- A prior full EditMode run passed `1347/1347` at `2026-11-09 10:52:02Z`. Play Mode visually confirmed all three city identities and return-to-map. At that time, the NPC/mission panel round trips and the 1280×720 layout had not yet been runtime-checked.
- Follow-up inspection found the mission panel sat behind the full-screen overview. The controller now raises the mission panel while keeping its return-to-overview and return-to-map buttons in front; `TownOverview_MissionCanOpenAndReturnWithoutHidingItsActions` covers the round trip.
- The existing Editor compiled the follow-up successfully. The full EditMode run completed at `2026-11-09 15:31:47Z`: `1348/1348` passed, `0` failed, `0` skipped, including the mission panel round-trip regression. During Play Mode in the Game View `Full HD (1920x1080)`, continuing the demo save and traveling from Kiyosu to Atsuta showed the Atsuta title and illustration. The mission panel opened over the overview; its return-to-overview action restored the town, the local-people panel opened and returned, and return-to-map restored the map at Atsuta. Console counters showed 0 logs, 0 warnings, and 0 errors. The exact `1280x720` check remains incomplete: the available Game View sizes had no such preset, and no custom size was added. The scene remains dirty alongside unrelated M02 WIP; no scene save, commit, or push was made.

---

**Plan self-review:** Every design section maps to a task: single-root layout, honest data display, existing interactions, generated art, two-resolution readability, transactional scene integration, and runtime return flow. No gameplay or rumor system is introduced.


---

<a id="2026-11-08-ui-reference-gallery-completion"></a>
## 来源：docs/superpowers/plans/2026-11-08-ui-reference-gallery-completion.md

# 五张参考图界面完成计划

> **执行方式：**由 OpenCode2 在现有 `main` checkout 实施；不创建新项目、branch 或 worktree。Mochi 负责场景集成、Unity 验证与 Play Mode 验收。

**目标：**把五个参考页面逐页补到获批设计稿的构图与视觉精度，同时保留真实游戏数据、交互命令和存档行为。

**架构：**沿用已存在的 `ReferenceGalleryNavigator`、页面 Presentation、领域命令与事务式 Editor 生成器。只完善共享主题/导航和页面布局、绑定、素材；不另建 UI 框架或重写游戏流程。

**技术栈：**Unity 6000.6.6f1、C#、uGUI、现有 Unity Test Framework。

**设计依据：**`docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`（用户已确认五页及复刻方向）。

## 全局约束

- 只在 `C:\Users\panye\My project` 的现有 `main` 上工作，不覆盖或清理已有 WIP。
- 参考画布为 1280×720；五张原图均为 1668×941。保留统一的深靛外框、纸本内容区、金色细节、朱印强调和易读中文字体。
- 参考图提供构图与美术方向；姓名、数值、事件和人物信息一律来自实时会话。没有真实数据的区域显示空态或省略，不造假。
- 保留既有领域规则、存档格式、事件结果、旅行/日历/月结/继承命令；不增加第三方包、不改 `ProjectSettings`、`Library` 或 `Temp`。
- 素材仅用用户提供的参考图裁切和项目原创素材；新增来源、授权记入 `ASSETS.md`。保留所有 `.meta` 和 GUID。
- 仅指定集成人员修改 `FoundationScene`。使用既有事务式生成器、单一 Undo 组、保存前验证和 Undo 回滚。
- Unity 操作前核验唯一 Editor PID 与 `-projectPath`。分别记录编译、EditMode、Console、Validator、Play Mode 和存档验证；不得以 EditMode 代替运行时或视觉验收。
- 代码由 OpenCode2 通过 Orca 受监督任务实施；五页按顺序完成，一次只保留一个写入者。完成后由独立 OpenCode worker 只读审查；Mochi 负责场景集成、验证与最终签核。
- Play Mode 可用 Computer Use 和截图；截图仅用于检查，检查后立即删除，不放入仓库或备份。
- 每道验证门通过后，只提交并推送本计划的已验证文件至 `origin/main`。用户明确接受整套演示前不打 milestone tag；不宣称五页完成。

## 评审重点

- 1280×720 与其他 16:9 分辨率下文字可读、按钮可点、内容不裁切或互相遮挡。
- 页面切换后只有目标页面接收输入；隐藏层、插画和背景不阻挡射线。
- 页面状态刷新后仍来自真实数据；无人、无事件、无决策或无路线时有清楚空态。
- 五页上的保存、人物选择、行动、继承、月结、旅行和事件选择仍调用既有命令。
- 家谱和名册仍支持任意人数、未连接亲族及家臣；不得为贴合截图而恢复固定人数/世代限制。

---

## 当前基线

- `e298312` 已加入五页导航、共享主题与素材、人物日常页、家族内务组合、中央地图插画及事件/家族页面重排；这些是部分实现，不代表达到参考图精度。
- 最新提交的全量 EditMode 为 1303/1303，含当前 Reference Gallery 回归；当前 `main` 尚无该提交后的五页完整 Play Mode 与完整场景验收证据。
- 继续实现前应以现有 `FoundationScene` 和各页面当前尺寸为准；先确认最新 Validator，不要从历史场景报告推断当前状态。

## 实施任务

### Task 1：锁定共用页面框架

**文件：**
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 修改：`Assets/Scripts/Presentation/SengokuUiTheme.cs`
- 修改：`Assets/Scripts/Presentation/SengokuUiArt.cs`
- 修改：`Assets/Scripts/Editor/AddM02DailyActionPanel.cs`
- 测试：`Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs`

- [ ] 对照参考图固定顶栏、侧栏、内容安全区、共享底色和页面选中态；不重复创建现有通用控件。
- [ ] 明确五个页面根与共享区域的激活/射线规则；页面切换不能改变会话状态。
- [ ] 扩展 `ReferenceGalleryNavigatorTests.ShowPage_ActivatesOnlyTheSelectedPageAndItsPanels`，断言全部五页的唯一激活根、导航状态和射线隔离。
- [ ] 在 1280×720 布局断言中验证共享边界、页签点击区和不重叠。

### Task 2：人物档案与日常行动页

**文件：**
- 修改：`Assets/Scripts/Presentation/DailyActionPanel.cs`
- 修改：`Assets/Scripts/Presentation/CharacterDailyPageView.cs`
- 修改：`Assets/Scripts/Presentation/NpcAiInspectorPanel.cs`
- 修改：`Assets/Scripts/Editor/AddM02DailyActionPanel.cs`
- 测试：`Assets/Tests/EditMode/M02DailyActionPanelTests.cs`
- 测试：`Assets/Tests/EditMode/NpcAiInspectorPanelTests.cs`

- [ ] 按图组织人物档案/关系/经历、中部关系展示、右侧日常计划和最右 AI Inspector。
- [ ] 绑定人物姓名、年龄、关系、能力、行动进度与目的地；AI Inspector 仍只读并能滚动查看决策说明。
- [ ] 保留休息、训练、工作、旅行、重排/中断、日推进及月结行为。
- [ ] 补充布局边界、动态刷新、不同人物选择、长文本和无行动/无关系空态测试。

### Task 3：家谱与继承页

**文件：**
- 修改：`Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- 修改：`Assets/Scripts/Presentation/FamilyTreeGraph.cs`
- 修改：`Assets/Scripts/Presentation/SuccessionPanel.cs`
- 修改：`Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`
- 修改：`Assets/Scripts/Editor/AddSC010SuccessionPanel.cs`
- 测试：`Assets/Tests/EditMode/FamilyRosterPanelTests.cs`
- 测试：`Assets/Tests/EditMode/AddSC010SuccessionPanelTests.cs`
- 测试：`Assets/Tests/EditMode/GameSessionSuccessionTests.cs`

- [ ] 将家谱世代卡与亲属连线置于中央，将当前家主、继承人、支持度及继承/摄政决策放在右侧层级区。
- [ ] 区分血亲、配偶、收养、未连接亲族与家臣；卡片头像必须匹配人物身份，缺图时使用清楚的通用占位。
- [ ] 保留滚动与任意规模图布局，以及指定继承人、让位、摄政等既有命令。
- [ ] 覆盖大族谱、无配偶/无继承人、未成年继承人、滚动边界、选中联动和按钮调用。

### Task 4：家族内务页

**文件：**
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 修改：`Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- 修改：`Assets/Scripts/Presentation/IndustryPanel.cs`
- 修改：`Assets/Scripts/Presentation/EventChoicePanel.cs`
- 测试：`Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs`
- 测试：对应产业面板与事件面板测试

- [ ] 按参考图组合家主/日期侧栏、家族成员与任命、本月事件、可执行待决事项。
- [ ] 成员、职位、产业状态、事件与决策从会话快照读取；缺少世界动态或决策数据时隐藏该区或显示空态。
- [ ] 保留产业管理、任命、事件处理与结束月份的现有入口和命令。
- [ ] 覆盖零/一/多成员、零/多事件及零/多待决事项的排版和射线区域。

### Task 5：地图与出行页

**文件：**
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 修改：`Assets/Scripts/Presentation/WorldMapController.cs`
- 修改：`Assets/Scripts/Editor/CreateM2WorldMap.cs`
- 修改：`Assets/Scripts/Editor/UpdateM2WorldMapLayout.cs`
- 修改：`Assets/Scripts/Editor/WorldMapLayout.cs`
- 测试：`Assets/Tests/EditMode/WorldMapUiTests.cs`
- 测试：`Assets/Tests/EditMode/CreateM2WorldMapGeneratorTests.cs`
- 素材登记：`ASSETS.md`

- [ ] 完成左侧队伍/路线计划、中央地图、右侧情报/人物/委托三栏构图；中央地图插画按比例适配。
- [ ] 路线、天数、抵达日期和风险均从现有地图规则派生；没有可展示的数据不填示例文本。
- [ ] 保留目的地选择、路线预览、确认、取消和旅行结算；插画与装饰不阻挡点击。
- [ ] 覆盖无路线、远程目的地、长标签、不同分辨率及插画/按钮边界。

### Task 6：事件抉择页

**文件：**
- 修改：`Assets/Scripts/Presentation/EventChoicePanel.cs`
- 修改：`Assets/Scripts/Editor/AddSC005EventChoicePanel.cs`
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 测试：`Assets/Tests/EditMode/EventChoicePanelTests.cs`
- 测试：`Assets/Tests/EditMode/AddSC005EventChoicePanelTests.cs`
- 测试：`Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs`

- [ ] 按左侧导航、中央叙事/选项卡、右侧待决事项区域排版，插画与文本按实际待处理事件变化。
- [ ] 保留条件禁用状态、`TryChooseEventOption`、结果反馈、待处理队列和空态。
- [ ] 覆盖长叙事、多个选项、禁用选项、无待决事件与确认后队列刷新。

### Task 7：场景集成与五页验收

**文件：**
- 修改：`Assets/Scenes/FoundationScene.unity`（仅由指定集成人员经现有生成器更新）
- 修改：`Assets/Scripts/Editor/FoundationSceneValidator.cs`
- 文档：`ASSETS.md`、`PROJECT_STATE.md`、五页试玩报告

- [ ] 先确认启动入口与 `FoundationScene` 的关系，运行当前场景 Validator 并记录基线缺陷。
- [ ] 用事务式生成器接入所有改动页面；验证根节点唯一、引用完整、画布内布局有效且场景保存干净。
- [ ] 当前 Unity Editor 完成编译、聚焦与全量 EditMode；执行 `FoundationSceneValidator.ValidateActiveScene` 并单独记录 Validator 与 Console 结果，解释预期日志。
- [ ] 在 Play Mode 逐页检查五张参考图的整体构图、比例、素材、可读性和交互；核验切页后返回各页状态仍同步。
- [ ] 对包含在范围内的行动、旅行、继承、产业、事件与存档操作检查实际结果；退出 Play Mode 后确认 Console 与场景状态。
- [ ] 检查每页临时截图后立即删除全部 Computer Use 截图；更新项目状态和验证报告，提交并推送已验证文件。

## 整套验收条件

- 五个页面均达到设计稿规定的主区域、视觉层级和色彩/纹理方向；不以“导航可用”或“插画已显示”代替视觉完成。
- 所有展示值来自当前游戏状态；无虚构数据，真实命令仍可完成并得到清楚反馈。
- Unity 编译成功、目标 EditMode 全绿、`FoundationSceneValidator` 无新缺陷、Console 统计单独记录、五页 Play Mode 路线通过。
- Play Mode 与技术验证通过后提交推送；整套 UI 的 milestone 仍需用户明确验收后才可标记完成或打 tag。


---

<a id="2026-11-08-ui-reference-gallery-repair"></a>
## 来源：docs/superpowers/plans/2026-11-08-ui-reference-gallery-repair.md

# UI Reference Gallery Repair Plan

> **For agentic workers:** Work in the existing `main` checkout. Do not create a branch or worktree. Preserve unrelated WIP and the current scene's user data.

**Goal:** Correct the four runtime defects found during the five-page reference gallery Play Mode check while keeping the approved 1280x720 layouts, live game data, and existing commands.

**Architecture:** Fix the shared page-to-view bindings and generated uGUI layout using the existing controllers and owner-provided UI art. Keep domain state and event outcomes unchanged; scene integration belongs to this single designated worker.

**Tech Stack:** Unity 6.6.6f1, C#, uGUI, existing Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`

## Global Constraints

- Stay in `C:\Users\panye\My project`, current `main`; no new branch/worktree/project.
- Preserve all existing dirty files, `.meta` files, GUIDs, M04A WIP, `graphify-out/`, and `nul`.
- Do not change domain rules, save format, event effects, dependencies, `ProjectSettings`, `Library`, or `Temp`.
- Use only user-provided reference artwork already in the project; do not add commercial-game assets.
- Only this designated integration worker may edit `FoundationScene.unity`; use its existing transactional generator and validation path.
- Do not commit or push until the focused/full tests and Play Mode gate pass; keep unrelated WIP out of any commit.

## Review Focus

- Page switches must activate the intended roots only and leave no invisible raycast blockers.
- Family cards must show a relevant portrait where a supplied portrait can be matched and remain readable for larger/unknown rosters.
- The map illustration must preserve its source aspect ratio, stay inside the central map region, and leave route/travel controls clickable.
- Household rows, month event, and pending decisions must use live session data; absent data stays absent.
- Event layout must render whichever event is actually pending; do not hard-code the example title or outcomes.

---

### Task 1: Repair page binding and household composition

**Files:** `Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`, `Assets/Scripts/Editor/AddM02DailyActionPanel.cs`, applicable presentation tests, `Assets/Scenes/FoundationScene.unity` only if generator output requires it.

- [ ] Make the Household page present the reference's family roster/appointments, current household industry, month event, and pending-decision regions rather than showing the Industry panel alone.
- [ ] Keep each existing button bound to its existing command; omit sections with no live data.
- [ ] Extend `ReferenceGalleryNavigatorTests.ShowPage_ActivatesOnlyTheSelectedPageAndItsPanels` or focused tests to assert the household roots and raycast ownership.
- [ ] Keep the 1280x720 regions inside the safe canvas and avoid overlaps.

### Task 2: Repair family portrait cards

**Files:** `Assets/Scripts/Presentation/FamilyRosterPanel.cs`, `Assets/Scripts/Presentation/FamilyTreeGraphBuilder.cs` or existing portrait data source only if necessary, family-roster tests, existing user-provided art only.

- [ ] Render a portrait in each family card when existing character identity data maps to an owner-provided portrait; do not assign an unrelated portrait or invent identity data.
- [ ] Keep generation spacing, relationship edges, card text, scrolling, and arbitrary family sizes intact.
- [ ] Extend `FamilyRosterPanelTests.Refresh_RendersSc013KinshipEdgesAndSeparateRetainerSection` and/or the large-roster test to assert portrait placement, fallback behavior, and no overlap.

### Task 3: Repair map scale and travel layout

**Files:** `Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`, `Assets/Scripts/Editor/CreateM2WorldMap.cs`, `UpdateM2WorldMapLayout.cs`, `WorldMapLayout.cs`, `CreateM2WorldMapGeneratorTests.cs`, `WorldMapUiTests.cs`, and the scene only through the designated generator.

- [ ] Place the map illustration in the central map rectangle with preserved aspect ratio; it must not cover title, date, route controls, destination buttons, or travel confirmation.
- [ ] Align the left travel plan, center map, and right information region to the approved reference proportions.
- [ ] Keep existing destination selection, preview, confirm, cancel, and route/date calculations unchanged.
- [ ] Add or extend layout assertions proving the image bounds and interactive control bounds do not overlap.

### Task 4: Repair event-choice composition

**Files:** `Assets/Scripts/Presentation/EventChoicePanel.cs`, `Assets/Scripts/Editor/AddSC005EventChoicePanel.cs`, event-panel tests and scene only through the designated generator.

- [ ] Reflow the event page into the reference's left navigation, central story/choice card, and right pending-event area; use existing page assets and styles.
- [ ] Continue displaying the current pending event from `GameSession`; never hard-code “雨夜来客” or image-sample consequences.
- [ ] Keep option enabled states, `TryChooseEventOption`, result feedback, and pending-queue behavior unchanged.
- [ ] Extend `EventChoicePanelTests` and `AddSC005EventChoicePanelTests` for live content, layout bounds, and option invocation.

### Task 5: Verify and hand off

- [ ] Run the focused tests for the four areas, existing scene validator, and full EditMode suite using the already-open Unity Editor (PID 91392 only if it still targets this project).
- [ ] Provide file-scoped review evidence and the Editor path/test/Console evidence separately; do not claim exact visual acceptance until the designated lead runs Play Mode.
- [ ] Leave Play Mode stopped and the scene saved only through the validated scene integration flow.
- [ ] Do not commit/push in the implementation worker; integration occurs only after review and Play Mode gate.


---

<a id="2026-11-09-m04a-personal-memories"></a>
## 来源：docs/superpowers/plans/2026-11-09-m04a-personal-memories.md

# M04A 个人行动记忆实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: 使用 `superpowers:subagent-driven-development`（推荐）或 `superpowers:executing-plans` 按任务逐项实施。步骤使用复选框跟踪。

**Goal:** 为正纲与信正增加有界、持久、可解释的行动记忆，使真实的行动完成或中断能在 30 个游戏日内改变其后续自主行动评分。

**Architecture:** 在现有 `NpcAutonomyPersonState` 中按人物保存最近一次完成与中断记忆；沿用整数 Utility 评分并通过 `NpcAutonomySnapshot` 投影给只读 Inspector。每日行动结果在已成功的状态变更路径中记录；v12 存档新增记忆字段，v11 迁移只初始化空记忆。

**Tech Stack:** Unity 6000.6.6f1、C#、现有 `GameDate`、`DailyActionState`、NPC 自主状态、JsonUtility 版本化存档与 EditMode Test Runner；不加依赖。

**Spec:** [M04A 个人行动记忆设计稿](design-and-planning-history.md#2026-11-07-m04a-personal-memories-design)

**执行进度（2026-11-09）：** M04A lead 技术签收通过；最终聚焦 181/181、全量 1381/1381、编译、Validator、两轮独立复审与 Play Mode（含 30/31 日和 GUI 存读档）通过。试玩后额外修复记忆裁切与读档共享顶部栏日期刷新，复用原滚动文本和现有回调，未改场景；相关文件为 `NpcAiInspectorPanel.cs`、`CharacterDailyPageView.cs`、`DailyActionPanel.cs` 及对应测试。历史“先写测试再实现”的顺序缺少证据，不追溯勾选；本轮三例缺字段、两例 UI 修复均有明确 red→green 记录，详见 [实现报告](verification-and-research-history.md#m04a-personal-memories)。用户最终 M04 里程碑验收仍待办。

## 全局约束

- 只影响 `person.sc013.masatsuna`（正纲）与 `person.sc013.nobumasa`（信正）；记忆归属于稳定 `PersonId`。
- 每人至多两条：最近一次完成和最近一次中断各一条；新同类结果替换旧记录。
- 完成/中断记忆在发生日到第 30 日（含）对同类自主行动分别增加/减少 12 分；过期记录保留显示但不改评分。
- 继续使用整数与稳定候选顺序；不更改 M03 行动资格、行动种类、场景根、M01/SC-024 规则或关系/编年史数据。
- 在现有 `C:\Users\panye\My project` 的 `main` 工作区实施；不新建项目、分支或 worktree，不覆盖未提交 WIP，不改 `.meta` GUID、`Library` 或 `Temp`。
- 只读 Inspector 不提供记忆编辑；不增加第三方包或自由文本记忆。
- v11→v12 仅增加空记忆；不从旧决策日志重建经历，不推进时间或重放月结。
- Unity 编译、EditMode、场景 Validator、Console、Play Mode 分别记录；Play Mode 后清理所有 Computer Use 截图。

## Review Focus

- 第 30 日与第 31 日边界：第 30 日仍修改评分，第 31 日仅保留显示。
- 同类重复结果及完成/中断并存：替换同类型且不覆盖另一类型，列表与存档顺序稳定。
- 行动中断和失败事务：仅已成功提交的中断/完成结果记忆；失败或回滚不留下记忆。
- 跨月、闰日与支持日期上限：有效期计算遵循 `GameDate` 的简化公历范围，不溢出或错误过期。
- 存档引用和版本边界：v11 无记忆可迁移为空；v12 缺字段、未来日期、未知人物/行动、重复类型或超过上限必须拒绝。

---

## 文件责任

- `Assets/Scripts/Domain/NpcAutonomy.cs`：记忆值、每人物有界状态、评分修正、评分明细与只读快照。
- `Assets/Scripts/Application/GameSession.cs`：把已成功的行动结果传给自主状态，并从本人记忆评估候选。
- `Assets/Scripts/Persistence/SaveData.cs`、`SaveDataMigrator.cs`、`SaveDataValidator.cs`：v12 序列化、校验、v11 默认迁移和字段存在性检查。
- `Assets/Scripts/Presentation/NpcAiInspectorPanel.cs`、`CharacterDailyPageView.cs`：在现有只读诊断区显示记忆和评分分解，不新增场景对象。
- `Assets/Tests/EditMode/NpcAutonomyTests.cs`、`NpcAutonomySessionTests.cs`、`NpcAiInspectorPanelTests.cs`、`SaveDataMigratorTests.cs`、`SaveDataValidatorTests.cs`：领域边界、行动接线、只读呈现及存档兼容测试。
- `PROJECT_STATE.md`、`docs/reports/m04a-personal-memories.md`：实现后分别更新路线状态与分层验证证据。

## 任务

### 任务 1：领域记忆与确定性评分

**文件：** 修改 `NpcAutonomy.cs`；测试 `NpcAutonomyTests.cs`。

**接口：** 增加 `NpcActionMemoryKind`（`Completed`、`Interrupted`）和不可变 `NpcActionMemory`（人物 ID、类型、行动、日期、原因）；`NpcAutonomyPersonState` 暴露只读记忆快照；`NpcAutonomyState.RecordMemory(...)` 按类型替换；评估结果向 `NpcActionCandidate` 提供基础分、记忆修正、最终分与解释。

- [ ] 先写测试：`RecordMemory_ReplacesOnlySameKindAndKeepsStableOrder`，以及两种记忆只影响同类行动、分别为 `+12/-12`。
- [ ] 先写日历边界测试：第 30 日仍生效、第 31 日不生效；跨月/闰日和 `GameDate` 最大日期不溢出。
- [ ] 运行上述聚焦 EditMode 测试，确认新行为尚未实现时失败。
- [x] 实现有界记忆、克隆与纯整数评分；保持候选资格、稳定排序和同分枚举规则不变。
- [x] 重跑聚焦测试，确认替换、隔离、评分分解及边界通过。

### 任务 2：行动事务接线与只读应用投影

**文件：** 修改 `GameSession.cs`；测试 `NpcAutonomySessionTests.cs`。

**接口：** 现有自主快照增加本人记忆列表；所有 `NpcAutonomyRules.Evaluate` 调用使用该人物的记忆。将现有完成处理扩展为统一处理 `DailyActionResult` 中已完成/已中断的试点行动，不改变完成行动原有需求结算。

- [ ] 先写会话测试：玩家安排与自主安排的试点行动完成/中断都记录到正确人物；其他人物不受影响。
- [ ] 先写事务测试：拒绝、无结果及失败/回滚路径不产生记忆；同一结果不能造成超过每类一条的记录。
- [ ] 运行 `NpcAutonomySessionTests` 相关用例，确认失败。
- [x] 在行动状态成功变更的既有路径记录结果；覆盖每日结算、显式中断、人物离世/离族清理和继承触发的中断路径。记忆随 `NpcAutonomyState.Clone` 参与现有事务快照回滚。
- [x] 将本人记忆带入候选评估和只读 `NpcAutonomySnapshot`；确认每个评估调用点都已更新。
- [x] 重跑会话测试，确认成功结果记忆、事务失败无记忆、人物隔离和回滚通过。

### 任务 3：v12 存档与 v11 迁移

**文件：** 修改 `SaveData.cs`、`SaveDataMigrator.cs`、`SaveDataValidator.cs`；测试 `NpcAutonomySessionTests.cs`、`SaveDataMigratorTests.cs`、`SaveDataValidatorTests.cs`。

- [ ] 先写存档测试：v12 保存/读取保留两类记忆的人物 ID、日期、行动和原因；v11 载入为每名自主人物创建空记忆。
- [ ] 先写拒绝测试：v12 缺记忆字段、错误人物引用、未来/非法日期、非法枚举、同类重复或超过两条记录均被拒绝。
- [ ] 运行这些聚焦 EditMode 测试，确认新增断言失败。
- [x] 将 `SaveDataValidator.CurrentVersion` 提升到 12，给 NPC 自主存档条目增加确定排序的记忆数据；加入 v11→v12 空记忆迁移与 v12 JSON 字段存在性验证。
- [x] 确认 v10 及更旧版本既有迁移继续生成有效 v12 数据；不改规则版本，不从历史日志推导记忆。
- [x] 重跑存档/迁移/校验测试，确认旧版本兼容与非法数据拒绝通过。

### 任务 4：只读 Inspector 显示

**文件：** 修改 `NpcAiInspectorPanel.cs`、`CharacterDailyPageView.cs`；测试 `NpcAiInspectorPanelTests.cs`。

- [ ] 先写文本断言：候选显示基础分、记忆修正、最终分及记忆来源；两条记录显示类型、行动、日期、当前是否生效与中断原因。
- [x] 确认面板只读且无新编辑控件；空记忆、过期记忆和未登记人物均有清楚显示。
- [ ] 运行聚焦 Inspector EditMode 测试，确认新断言失败。
- [x] 复用现有 Inspector 滚动文本区和人物页诊断栏展示只读快照；不新增场景根、不增加交互入口。
- [x] 重跑 Inspector 测试，并运行现有 `FoundationSceneValidator`，确认既有单根结构和序列化绑定仍通过。

### 任务 5：整体验证、文档与复审交接

**文件：** 更新 `PROJECT_STATE.md`；新增 `docs/reports/m04a-personal-memories.md`。

- [x] 在目标 Unity Editor 中确认进程与 `-projectPath`；禁止对其项目路径运行 batchmode。
- [x] 运行聚焦及全量 EditMode；分别记录编译结果、测试计数、Console logs/warnings/errors、`FoundationSceneValidator` 结果。
- [x] 由 Mochi 在现有 Editor 执行 M04A Play Mode：完成与中断行动、评分解释、30/31 日边界和保存/读取；确认后退出 Play Mode，不保存无关场景状态。
- [x] 按项目流程执行独立只读复审；OpenCode 启动器不可用，独立 QATester 在同一实现任务下两轮 ACCEPT，修正后再作技术签收。
- [x] 更新状态/报告，区分实际验证与未验证项；只提交并推送已通过对应验证门的 M04A 文件，等待用户明确验收后再做最终里程碑 checkpoint、合并或 tag。

## 计划自检

- 设计稿各项目标分别由任务 1–4 覆盖；旧版迁移和验证由任务 3、5 覆盖。
- Review Focus 的边界均对应到任务 1–3 的明确测试；UI 空态和只读约束对应任务 4。
- 评分签名的输入是 `NpcAutonomyPersonState` 当前两条记忆；存档条目嵌套于其所属试点人物，并显式保留 `personId` 供一致性校验。
- 不改 `FoundationScene` 结构，只有当 Validator 发现既有场景绑定确实损坏时才回到任务计划调整；不预先生成新的场景对象。
- 计划与设计稿同范围；M04 动态成员、婚姻/收养政策及更广泛人物记忆仍不在本切片。


---

<a id="2026-11-09-world-map-immersive-ui"></a>
## 来源：docs/superpowers/plans/2026-11-09-world-map-immersive-ui.md

# 沉浸式世界地图实施计划

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. One independent read-only review follows implementation.

**Goal:** 落实用户选定的全屏地图与可收起侧栏，保留现有旅行和时间操作。

**Architecture:** 在现有地图页上增加一个只负责布局的组件，复用原按钮、文本和地图插画。导航统一控制页面显隐；不改旅行结算或存档契约。

**Tech Stack:** Unity 6、现有 uGUI、现有原创素材；无新依赖。

**Spec:** ../specs/2026-11-09-world-map-immersive-ui-design.md

## Global Constraints

- 当前 main 内开发，不创建分支、worktree 或项目；保留所有既有 WIP。
- 使用 PID 126040 的现有 Editor；不运行 batchmode。
- 不修改场景、存档或玩法规则；截图查看后立即删除。
- 既有导航、名册、编年史、结束月份均必须可达。

## Review Focus

- 反复进出地图与同页点击：不增加节点或监听、不清除有效预览。
- 侧栏收起：原文本状态保留，确认与取消始终可达。
- 城镇覆盖层：不被新增地图控件遮住，返回时正确恢复。
- 低分辨率：文字不缩小到不可读，栏宽与地图空间明确。
- 地图装饰不接收射线，背景不遮挡导航。

### Task 1: 地图布局与导航集成

**Files:** 新增 `Assets/Scripts/Presentation/ImmersiveWorldMapView.cs`；修改 `ReferenceGalleryNavigator.cs`；新增对应 EditMode 测试，调整旧地图布局断言。

**Interfaces:** `Configure()` 在组件所在地图根上幂等绑定既有子节点；`SetTravelExpanded(bool)` / `SetInformationExpanded(bool)` 只修改显示状态。

- [x] 添加折叠、重复配置、动作栏边界与状态不变回归测试并观察失败。
- [x] 复用既有地图插画，重新排版为地图主体、左右侧栏、底部固定操作。
- [x] 集成到地图页；保留原旅行按钮的监听和原生引用。
- [x] 运行聚焦 EditMode，然后独立只读 QA；修复缺陷。

### Task 2: 当前 Editor 验证与交付

- [x] 运行适当的完整 EditMode、编译与场景 Validator，分别记录结果。
- [x] Play Mode 检查侧栏开合、导航、名册/编年史、预览/取消/确认及时间按钮。
- [x] 校验用户存档未变；退出 Play Mode；检查 Console 并清理截图。
- [x] 更新项目状态和报告，只提交本轮验证文件并推送当前 main；最终里程碑验收仍由用户决定。

## Decisions

- 用户已选择方案 3 并要求直接继续开发，按该范围实施，不增加新的玩法选择。
- 沿用既有原创插画和 uGUI，无需重复外部依赖研究或新增美术素材。
- 侧栏直接开合，不添加非必要动画；没有会话数据的传闻/委托不显示。
- 窄屏实机已在 960×720 与 1280×720 Game View 预设验证，并恢复 4K UHD。详见后续验收附录。

## 2026-11-09 窄窗实机跟进

已通过 1280×720 的实际 GUI 保存/推进/读回；已将用户 active 与备份恢复到原哈希。960×720 暴露真实缺陷：地图只在进入页面时读取 Canvas 尺寸，分辨率变化后保留旧尺寸；共享顶栏也保留固定宽度，使资源和地图右侧操作裁切。

修复范围仅布局：`ReferenceGalleryNavigator` 观察真实父 Canvas 尺寸变化，在不调用 ReturnToMap 的情况下重排地图；共享顶栏在窄窗将资源移到第二排，不缩小文字，侧栏保留可用高度。`ImmersiveWorldMapView.Configure()` 识别配置后的实际尺寸变化，自动收起信息栏。沿用同一工作区、既有按钮/监听/存档契约，不调整玩法。

实机追加决定：窄窗经地图快捷入口打开家谱后，原760宽卡片仍压在导航下。该入口的验收需要家谱可读，故同一修复收窄卡片和既有 ScrollRect 视口，保留图节点字号/坐标/双轴滚动，不增加编辑功能；相应 HUD 下移到两行顶栏下方。宽窗恢复既有尺寸。新增原家族布局测试的缩窄断言并已观察明确红测。

- [x] 添加父 Canvas 动态缩窄测试，观察真实失败；保留地图返回计数/选择，检查顶栏与页面边界。
- [x] 最小修复，聚焦测试通过后安排独立只读 QA。
- [x] 当前 Editor 验证 960×720 与 1280×720，旅行预览、侧栏、名册/编年史、时间按钮；既有 GUI 存读档验证与本轮存档哈希复核有效。
- [x] 完成全量/编译/Validator，恢复用户存档和原 4K UHD 视图预设，清理临时脚本与截图，更新报告并仅提交本轮文件推送 main。

## 2026-11-10 家谱窄屏验收补充

地图入口打开家谱时，960×720 下原固定宽度卡片与导航栏/HUD 冲突。保留原 ScrollRect 与图内容，只缩窄视口并添加独立横向滚动条，让较宽家谱可以通过标准点击访问右侧成员。继承人说明移到滚动条下方；重复页面布局不得重置 Scrollbar 管理的 handle 几何。1280×720 下卡片恢复原宽度。

- [x] 横向滚动条缺失回归先红后绿；重复配置后的 handle 宽度断言通过。
- [x] 完整 EditMode `1386/1386`，独立只读 QA 第五轮 ACCEPT。
- [x] 960×720 与 1280×720 真实 Play Mode 检查；点击横向滚动条后家谱内容移至右侧成员。
- [x] FoundationScene Validator、存档哈希、4K UHD 恢复、Play Mode 退出及临时文件清理见验证报告。


---

<a id="2026-10-31-m4a-supply-shortage-design"></a>
## 来源：docs/superpowers/specs/2026-10-31-m4a-supply-shortage-design.md

# M4a Supply Shortage Mission Design
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


## Intent

Add the first complete Phase 1 mission: the player accepts a supply-shortage investigation in Kiyosu, investigates in Atsuta, negotiates with a merchant, returns to Kiyosu, reports, and receives merit once. This closes the core mission loop without building a reusable quest scripting framework.

## Current state

- M1-M3 are accepted on `main`; M4a is the next roadmap milestone.
- `GameSession` owns the current location, travel clock, and dialogue/relationship use cases.
- M3 added town NPC rosters and branching dialogue. Kuroda Iori is the only NPC assigned to Atsuta; his definition does not currently specify an occupation.
- There is no mission or merit runtime model. `ARCHITECTURE.md` recommends ordinary C# mission state and idempotent reward claims.
- `main` has preexisting local edits. M4a planning and any implementation belong on `feature/m4a-supply-shortage-task` in its clean linked worktree.

## Goals

- Make the single supply-shortage mission playable from acceptance to reward claim.
- Enforce the required order and location for each step.
- Show the current objective, available action, and result in uGUI.
- Award exactly 10 merit once after a valid report in Kiyosu.
- Test state transitions, invalid actions, and duplicate reward claims in EditMode.
- Preserve the current map, travel rules, M3 relationships/dialogue, and fictional cast.

## Non-goals and anti-goals

- No M4b missions, generic quest graph, event bus, arbitrary scripts, mission editor, or third-party package.
- No save/load work; persistent mission state belongs to M5.
- No new scene, free-roaming town, changes to `PlayerMovement2D`, Build Settings, or unrelated `ProjectSettings`.
- Do not let UI call reward mutation directly or let mission rules depend on Unity types.
- Do not award merit on acceptance, investigation, negotiation, or a repeated report.

## Approaches

1. **Focused mission state and explicit actions (recommended working assumption).** A small plain-C# state model owns this mission's ordered stages and reward claim. `GameSession` validates current location and coordinates state. A dedicated uGUI panel displays progress and invokes the explicit application actions. This keeps rules testable and matches the architecture and roadmap.
2. **Drive mission stages through NPC dialogue options.** This reuses the M3 dialogue view, but the current dialogue effect model only changes relationships. Extending it to mutate mission state risks coupling two domains and makes report/reward preconditions less obvious.
3. **Build a reusable mission framework.** This could support more content but conflicts with M4a's explicit scope and adds definitions, generic conditions/effects, and authoring infrastructure before a second mission exists.

The user approved approach 1, Kuroda Iori as the merchant, and a +10 merit reward on 2026-10-31. GitHub research found no suitable reason to import a package: one Unity 6+ MIT sample is still broader than this fixed mission; other MIT samples are older or add event/service infrastructure. Build the focused custom model.

## Proposed rules

Use a mission-specific stage enum with this progression:

`NotAccepted -> Accepted -> Investigated -> Negotiated -> RewardClaimed`

- Accept only while in Kiyosu and `NotAccepted`.
- Investigate only while in Atsuta and `Accepted`.
- Negotiate with Kuroda Iori only while in Atsuta and `Investigated`.
- Report only while in Kiyosu and `Negotiated`.
- A successful report atomically adds 10 merit and advances to `RewardClaimed`; every later report fails without changing merit.
- Mission actions do not advance the clock. Existing travel remains the only time cost in this mission.
- `InitNewGame` resets the mission stage and merit count. M5 will add persistence.
- Use Kuroda Iori as the fictional merchant for this mission, without adding a sixth NPC or changing M3's roster.

The fixed reward amount and merchant assignment were approved by the user on 2026-10-31.

## UI proposal

Add one mission panel under the existing `M2WorldMapRoot` UI hierarchy. It shows mission status/progress, the current location requirement, total merit, contextual action buttons, and result feedback. The actions are accepting in Kiyosu, investigating in Atsuta, negotiating with Kuroda in Atsuta, and reporting/claiming in Kiyosu. Invalid actions remain unavailable in the UI and are also rejected by `GameSession`.

Scene edits stay single-writer and use the existing transactional Unity Editor workflow; generated UI stays under one fixed root and preserves scene `.meta` GUIDs.

## Constraints

- Unity 6000.6.6f1, URP 2D, C#, built-in uGUI, no new package.
- Keep mission rules in plain C# under the existing Domain assembly; keep orchestration in `GameSession`; keep presentation in its own component.
- Use the current Unity Editor for compile/test/scene integration. Never launch a second Editor or batchmode against the same project.
- Preserve original fictional content and license no new assets.
- User Play Mode acceptance is required before the final M4a checkpoint commit, merge, or tag.

## Open implementation details for the builder

- Names of mission-specific result/status structs and exact `GameSession` method signatures.
- The smallest scene binding/generator change that adds the panel without replacing the existing town UI.
- Whether a PlayMode test adds value beyond EditMode UI behavior tests and the manual demo walkthrough.

## Acceptance evidence

- EditMode tests prove the valid route, every wrong-location/wrong-stage rejection, unchanged state after rejection, mission reset, and exactly-once `+10` merit.
- Unity compile succeeds with zero Console errors; available EditMode tests pass; no unrelated ProjectSettings or user WIP changes enter the feature commit.
- In Play Mode, complete the mission from Kiyosu to Atsuta and back; see progress at each step; confirm a valid report awards 10 once and a repeat report cannot award again.
- User explicitly accepts the playable demo before M4a is marked accepted or checkpointed.


---

<a id="2026-11-01-m4b-two-simple-missions-design"></a>
## 来源：docs/superpowers/specs/2026-11-01-m4b-two-simple-missions-design.md

# M4b: Two Simple Missions Design
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


## Intent

Complete the two remaining Phase 1 missions using the existing map, five NPCs, and M4a's small plain-C# mission pattern. This is a fixed content milestone, not the start of a reusable quest-authoring system.

## Current state and sequence

- M1, M2, M3, and M4a are accepted; M4a is checkpointed as `demo-M4a` and merged into local `main`.
- M4b is the next roadmap milestone and depends on M4a.
- `GameSession` currently owns the fixed M4a model and exposes its merit. The existing UI and safe scene-updater pattern are mission-specific.
- The map has Kiyosu, Atsuta, and Nagoya. Kiyosu–Nagoya and Kiyosu–Atsuta each take one day; Atsuta–Nagoya has no direct route.
- Existing NPCs are Suzu and Genzaemon in Kiyosu, Iori in Atsuta, and Aya and Jirobei in Nagoya.

## Goals

- Add exactly two short, playable mission loops with explicit, ordered stages.
- Use existing NPCs and locations only.
- Award +5 merit for each mission, once, after its final valid action. The three Phase 1 missions therefore award at most 20 total merit including M4a's +10.
- Keep mission actions free of time or location changes; only existing travel advances the date.
- Present all three fixed missions consistently in the existing town UI, with status, objective, available action, and feedback.
- Add EditMode coverage for valid paths, wrong location/stage, repeat actions/rewards, reset, and unchanged clock/location on mission actions.
- Keep persistence out of scope until M5.

## Proposed mission content

### Suzu's sealed letter

1. Accept from Tachibana Suzu in Kiyosu.
2. Deliver it to Hoshino Aya in Nagoya.
3. Complete on successful delivery and claim +5 merit once.

Stages: `NotAccepted -> Accepted -> RewardClaimed`.

### Nagoya market tally

1. Accept from Sakai Jirobei in Nagoya.
2. Travel to Kiyosu and investigate the tally with Mori Genzaemon.
3. Return to Nagoya and report to Sakai Jirobei.
4. Complete on valid report and claim +5 merit once.

Stages: `NotAccepted -> Accepted -> Investigated -> RewardClaimed`.

Each mission may be completed independently and in either order. Their state and once-only reward guards remain in their own fixed mission-specific C# models. `GameSession.Merit` reports the sum of rewards claimed by all three fixed missions.

## Interaction and time rules

- A mission action succeeds only at the specified location and in the specified stage; rejected actions leave all mission, merit, date, and location state unchanged.
- Acceptance and mission actions do not move the player or advance the date.
- Travel uses the existing validated map preview/confirmation flow and consumes its normal one-day route cost.
- Existing M4a stages, +10 award, NPC dialogue, and map behavior remain unchanged.

## UI and scene integration

Extend the existing fixed M4a mission panel in place into a three-entry mission board with two pages under `M2WorldMapRoot/TownMenuRoot`. Keep its root, controller binding, M4a text/actions, and existing serialized references stable; show M4a on the default page and add a toggle to a compact page with both M4b mission rows. Each M4b row has status, objective, feedback, and one contextual action. Every action dispatches to a named `GameSession` use case and mission-specific model. The board is hidden with the town menu and refreshes when the player changes location. Keep its existing 280-by-240 footprint so it does not cover the town title, NPC roster, or return button; validate sibling separation. Avoid duplicate roots or broad scene restructuring.

Generated UI remains under one fixed root. Scene integration is single-writer, validates all references before saving, uses one Undo group, rolls back on failure, and never reloads the scene to roll back.

## Non-goals

- No generic quest engine, quest graph, event bus, arbitrary script system, mission editor, or ScriptableObject content pipeline.
- No new NPC, package, external asset, or scene.
- No save/load work, which belongs to M5.
- No edits to `PlayerMovement2D.cs`, `SampleScene`, Build Settings, or unrelated ProjectSettings.
- No changes to M4a's approved mission arc or reward.

## Reuse research

The existing mission model and uGUI panel are the closest fit. A GitHub scan compared three Unity quest-system candidates: `ahmedAwedni/Unity-Quests-System` (MIT; README claims Unity 6+, but adds a general ScriptableObject/event setup), `mechaniqe/unity-quest-core` (MIT; targets Unity 2021.3 LTS and adds a separate event package and graph tooling), and `SomeoneWeird/UnityQuestSystem` (no clear license or Unity 6 compatibility found). None is proportionate to two fixed missions, so the decision is to build narrowly on the existing project patterns and add no dependency.

## Acceptance evidence

- Unity compilation succeeds with zero new Console errors.
- Full EditMode suite passes, including all M4a regressions and M4b model/session/UI/updater coverage.
- FoundationScene validation passes with all three mission entries bound exactly once.
- In Play Mode, both loops can be completed from their start NPCs through their final action; each awards +5 once, aggregate merit is correct, rejected actions do not change date/location, and the Console has no new red errors.
- User Play Mode acceptance is required before the final M4b checkpoint, merge, or tag.


---

<a id="2026-11-01-m5-single-slot-save-design"></a>
## 来源：docs/superpowers/specs/2026-11-01-m5-single-slot-save-design.md

# M5: Versioned Single-Slot Save Design
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


## Intent

Add the Phase 1 single-slot save/load loop so the accepted M1-M4b demo can be saved, reloaded, and replayed without duplicating mission rewards. This is a small, explicit persistence layer, not a save framework, migration system, or multi-slot feature.

## Current state and sequence

- M1, M2, M3, M4a, and M4b are accepted and merged into `main`; the latest checkpoint is `demo-M4b`.
- Runtime state currently lives in `GameSession`: `WorldClock`, `CurrentLocation`, three fixed mission models, and `CharacterRelationships`. Dialogue instances are transient.
- Mission models expose stages and once-only merit awards (+10, +5, +5). Merit is derived from the three models.
- `GameSession.InitNewGame()` resets everything to 1560-04-01 in Kiyosu.
- No persistence code, package, or asset exists yet.

## Goals

- Define a versioned `SaveData` DTO with `saveFormatVersion` and only plain, stable-ID data.
- Save and restore: current date, current location, all three mission stages (which imply claimed rewards), and all NPC relationship values.
- Write one slot atomically so a failed write never destroys the previous save.
- Reject unsupported versions, corrupt JSON, and invalid data with a clear, non-destructive error.
- Ensure loading a completed mission cannot award its merit again.
- Provide a minimal in-game Save/Load control and a visible result message.
- Cover the DTO, validator, file store, and session snapshot/restore with EditMode tests.
- Keep the full 10-15 minute demo loop playable after a save/reload.

## Save format (version 1)

```json
{
  "saveFormatVersion": 1,
  "year": 1560,
  "month": 4,
  "day": 2,
  "locationId": "owari.atsuta",
  "supplyShortageStage": 2,
  "sealedLetterStage": 1,
  "marketTallyStage": 0,
  "relationships": [
    { "characterId": "npc.kuroda.iori", "value": 10 }
  ]
}
```

Rules:

- `saveFormatVersion` must equal `SaveDataValidator.CurrentVersion` (1). Anything else is rejected as unsupported.
- Date must satisfy `GameDate.IsValidDate` (1560-01-01 through 2000-12-31).
- `locationId` must be one of the three stable location IDs.
- Mission stages are stored as their integer enum values and must be defined stages of their model.
- Relationship entries must use known NPC IDs (never the player ID), appear at most once, and stay within [-100, 100]. Missing NPCs default to 0.
- Merit is not stored. It is derived from mission stages, so a tampered or duplicated merit value cannot exist.
- Unknown JSON fields are ignored; missing optional fields default safely.

## Persistence behavior

- Slot path: `<Application.persistentDataPath>/SengokuChronicle/save-slot-1.json`.
- Save: validate first, serialize with Unity's built-in `JsonUtility`, write to a temporary file in the same directory, then replace/move it over the slot. On any failure, delete the temporary file and leave the existing slot untouched.
- Load: missing file, unreadable file, invalid JSON, unsupported version, and invalid data each produce a distinct error code and message. Loading never writes to the slot.
- Errors are returned as results, not thrown to the UI.

## Session mapping

- `GameSession.CaptureSave()` builds a `SaveData` from current state.
- `GameSession.TryApplySave(data)` validates first, builds all replacement state in local variables, and only then assigns it. A rejected load leaves the live session untouched.
- Applying a save replaces the clock, location, missions, and relationships, and clears transient dialogue instances.
- `GameSession.SaveTo(store)` / `LoadFrom(store)` wrap the store for UI use.

## UI and scene integration

- Add one compact Save/Load control with a status line to the existing map screen. Two buttons (存档 / 读档) plus one feedback text are enough; no new panel hierarchy.
- Save reports success or a specific failure. Load refreshes the date, location marker, destination panel, mission board, and closes any open NPC/dialogue UI.
- The control must not overlap existing map, town, mission, or NPC controls. Scene integration follows the existing single-writer, one-Undo-group, validate-before-save, rollback-on-failure updater pattern.
- Generated UI stays under one fixed root.

## Non-goals

- No multiple slots, autosave, cloud sync, encryption, compression, or migration tooling.
- No generic serializer abstraction, repository pattern, event bus, or new package.
- No serialization of Unity objects, `MonoBehaviour`, or ScriptableObject references.
- No changes to mission rules, rewards, travel rules, or `PlayerMovement2D`.
- No edits to `SampleScene`, Build Settings, or unrelated `ProjectSettings`.

## Reuse research

Unity's built-in `JsonUtility` already covers this flat DTO, so no serializer dependency is needed. Adding Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) would require package approval and offers no benefit for one flat object. No external save-system repository is proportionate to one slot and roughly ten fields; the existing plain-C# + asmdef structure is reused instead.

## Acceptance evidence

- Unity compilation succeeds with zero new Console errors.
- Full EditMode suite passes, including all existing M1-M4b regressions and the new persistence tests.
- FoundationScene validation passes with the Save/Load control bound exactly once and no overlap with existing controls.
- In Play Mode: play part of the loop, save, exit and re-enter Play Mode, load, and confirm date, location, mission stages, merit, and relationships match; a completed mission cannot pay out again.
- Loading a corrupt or unsupported-version file shows a clear error and leaves the existing slot and live session unchanged.
- User Play Mode acceptance is required before the final M5 checkpoint, merge, or tag.


---

<a id="2026-11-01-m6-title-demo-packaging-design"></a>
## 来源：docs/superpowers/specs/2026-11-01-m6-title-demo-packaging-design.md

# M6 设计：标题界面与 Phase 1 演示封装（Title Screen & Demo Packaging）
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


日期：2026-11-01
状态：已批准（用户选择方向 1，继续使用 `feature/m5-save-slot` 分支与本工作树）
前置：M1–M5 全部已接受；`demo-M5` 已合并并推送。

## 目标

让 Phase 1 演示真正「从标题开始、可继续、有结算」：

1. Play Mode 启动时显示全屏标题界面（`M6TitleRoot`），提供「新游戏」与「继续」。
2. 「继续」直接读取 M5 单槽存档；无存档/坏档时在标题上给出清晰提示，不进入地图。
3. 三项任务全部结算后，地图上出现一行演示完成摘要（含总功绩），作为 Phase 1 的可见收尾。

## 明确不做

- 不做多槽存档、设置页、退出到主菜单、场景重载、淡入淡出动画。
- 不修改 `SaveData` 版本、`SaveSlotStore` 行为、`SaveLoadPanel`（游戏内存档按钮保持原样）。
- 不新增 package；仅 uGUI + 现有 LegacyRuntime 字体。

## 设计

### 场景结构（由 `AddM6TitleScreen` 事务式更新器生成）

```
Canvas
├─ M2WorldMapRoot（现有；新增子节点 M6DemoSummaryText，位于 TownMenuRoot 之前，默认隐藏）
└─ M6TitleRoot（默认隐藏，最后一个兄弟 → 渲染在最上层）
   ├─ M6TitleLabel          「战国立志传 · Phase 1 演示」
   ├─ M6NewGameButton       「新游戏」
   ├─ M6ContinueButton      「继续」
   └─ M6TitleStatusText     一行状态提示
GameSession 对象上新增组件：TitleScreenController、DemoSummaryPanel
```

- `M6TitleRoot` 为全屏 stretch RectTransform + 不透明 `Image`（raycastTarget=true），标题显示时天然屏蔽下层地图输入。
- 两个根在保存的场景里都是 inactive：编辑模式视野不变，Validator 布局检查不受影响；Play Mode 由控制器激活。

### TitleScreenController（Presentation）

- `Bind(session, store, mapController, titleRoot, newGameButton, continueButton, statusText)` + `SetStore()` 测试注入口；`Awake` 缺省创建 `SaveSlotStore.CreateDefault()`。
- `Start`：显示标题、绑定按钮监听、`RefreshTitleState()`。
- `RefreshTitleState()`：`store.Load()` → `NotFound` 显示「尚未找到存档。」并禁用继续；其他失败显示具体错误并禁用继续；成功显示「检测到存档，可继续上次进度。」并启用继续。
- `StartNewGame()`：`session.InitNewGame()` → `mapController.ReturnToMap()` → 隐藏标题。
- `TryContinue()`：`session.LoadFrom(store)`。失败：状态栏显示消息、标题保持、会话不变（`TryApplySave` 本身是原子的）；成功：`ReturnToMap()` 刷新全部面板后隐藏标题。
- 暴露 `Status`、`LastOperationSucceeded`、`IsTitleVisible` 供测试与诊断。

### GameSession.IsDemoComplete（纯 C#）

`SupplyShortageStage == RewardClaimed && SealedLetterStage == RewardClaimed && MarketTallyStage == RewardClaimed`。不新增状态，不写存档。

### DemoSummaryPanel（Presentation）

挂在始终活跃的 GameSession 对象上；`Update()` 每帧调用幂等 `Refresh()`（一次枚举比较，成本可忽略；规则本体在 domain 属性中）。完成时激活并显示 `M6DemoSummaryText`：「Phase 1 演示完成。总功绩：{Merit}。」未完成时隐藏。载入完成档同样会显示（读档不重复领奖的性质不变）。

### Validator / 更新器

- `AddM6TitleScreen`：一个 Undo 组、验证后才保存、异常回滚、不重载场景；`RunBatchIntegration()` 批量入口；`Validate(scene, out error)` 检查唯一性、父子与兄弟次序（标题根是 Canvas 最后一个兄弟且在地图根之后）、绑定完整、摘要文本在地图矩形内且不与 M5 控件重叠（M5 根存在时检查）。
- `FoundationSceneValidator`：新增 `FOUND M6TitleRoot / TitleScreenController / DemoSummaryPanel`、绑定检查与 `AddM6TitleScreen.Validate` 调用。

## 测试

- `TitleScreenControllerTests`：新游戏重置会话并隐藏标题；继续载入存档（日期/任务阶段还原、标题隐藏）；缺档禁用继续且提示、标题保持；坏档禁用继续、会话与槽文件均不变。
- `DemoSummaryPanelTests` + `GameSession` 属性测试：三项全部 RewardClaimed 前不显示、后显示且含总功绩 20；部分完成不显示。
- `AddM6TitleScreenTests`：preview-scene 结构/绑定/幂等/故障注入回滚，测试缝永不保存场景。

## 验收路径（用户 Play Mode）

启动 Play → 标题可见 →（无档时继续禁用）→ 新游戏进入地图 → 游玩并存档 → 退出再进入 Play → 继续 → 状态与存档一致 → 完成全部三任务 → 看到「Phase 1 演示完成」摘要行。


---

<a id="2026-11-01-m7-merit-promotion-design"></a>
## 来源：docs/superpowers/specs/2026-11-01-m7-merit-promotion-design.md

# M7 设计：功名晋升（Merit & Promotion）
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](../../GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


日期：2026-11-01
状态：已批准（用户选择方向 1；沿用 `feature/m5-save-slot` 分支与本工作树）
前置：M1–M6 全部已接受；`demo-M6` 已合并并推送。Phase 2 首个里程碑。

## 目标

让「功绩」从单纯数字变成可见的成长反馈：

1. 纯 C# 晋升规则：按总功绩划分四个军阶（游戏化简化设定，不代表史实，不指涉具体历史官职）。
2. 地图上常驻一行「功名」文本：当前功绩、当前军阶、下一军阶及所需功绩。
3. M6 演示完成摘要行同时显示最终军阶。

## 设计

### Domain：`PromotionRules`（纯静态，无 MonoBehaviour）

| 军阶 | 枚举 | 名称 | 起始功绩 |
|---|---|---|---|
| 1 | `Ashigaru` | 足轻 | 0 |
| 2 | `Banto` | 番头 | 5 |
| 3 | `Kumigashira` | 组头 | 10 |
| 4 | `Bushogashira` | 武士头 | 20 |

- `RankFor(int merit)`：负数按 0 处理；返回达到的最高军阶。
- `ChineseName(rank)`、`MinimumMerit(rank)`。
- `TryNextRank(rank, out next, out meritNeeded)`：顶级返回 false。
- 阈值以阶段数据（不可变数组）表达，测试直接读取，不散落魔法数字。
- Phase 1 满功绩恰为 20（10+5+5）：完成三项任务即达最高阶，作为演示的完美收尾。

### `GameSession` 派生属性

`Rank => PromotionRules.RankFor(Merit)`；`RankName`。不新增状态字段，**存档格式不变**（军阶由任务阶段推导的 merit 派生，读档不可能伪造军阶或重复领奖）。

### Presentation：`RankPanel`

- 与 `DemoSummaryPanel` 同模式：挂在常活跃的 GameSession 对象上，幂等 `Refresh()` + `Update()` 轮询；`Bind(session, text)` + 空会话保护。
- 文本（单行，可换行到两行）：`功名：{Merit} · {RankName}（下一阶 {NextName} {Needed}）`；顶级显示 `功名：{Merit} · {RankName}（最高）`。
- 布局：`M2WorldMapRoot/M7RankText`，中心锚点 (-450, 100)，尺寸 240×40，字号 11——左上安全区，验证器对 TitleText/LocationMarkerText/M5 列/按钮逐一做重叠检查；插入位置在 `TownMenuRoot` 之前。

### `DemoSummaryPanel` 文案升级

`Phase 1 演示完成。总功绩：{0} · {1}。`（{1}=军阶名）。仍由 `GameSession.IsDemoComplete` 门控。

### Editor 更新器：`AddM7RankPanel`

沿用 M5/M6 事务式模式：一个 Undo 组、先验证后保存、失败回滚、不重载场景；创建 `M7RankText`、在 GameSession 对象上挂 `RankPanel` 并绑定；`Validate` 检查唯一性、父级、兄弟次序（TownMenu 之前）、包含与相邻控件重叠；菜单 `Tools/Sengoku Chronicle/Add M7 Rank Panel`；`RunBatchIntegration()` 批量入口。

### Validator

`FoundationSceneValidator` 增加 `FOUND RankPanel`、绑定检查与 `AddM7RankPanel.Validate` 调用。

## 明确不做

- 不新增 merit 来源、不改三项任务的奖励数值、不新增存档字段或版本。
- 不做晋升动画/音效/弹窗；不做历史官职考据（游戏设定，文案注明简化）。
- 不新增 package；uGUI + LegacyRuntime 字体。

## 测试

- `PromotionRulesTests`：0/4/5/9/10/19/20/30 与负数的边界映射；名称与最小值一致性；`TryNextRank` 逐级与顶级返回 false。
- `RankPanelTests`：新局文本含「足轻」；完成态（阶段 4/2/3）含「功名：20 · 武士头（最高）」；空会话不崩溃。
- `DemoSummaryPanelTests` 扩展：完成文案含军阶名。
- `AddM7RankPanelTests`：preview-scene 结构/绑定/幂等/故障注入回滚/重叠拒绝/preflight。

## 验收路径（用户 Play Mode）

1. 打开 FoundationScene，清 Console，进入 Play → 标题 → 新游戏。
2. 地图左上出现功名行：`功名：0 · 足轻（下一阶 番头 5）`。
3. 完成密书任务（+5）→ 军阶变为番头；完成市场清点（+5）→ 组头；完成补给短缺（+10）→ 武士头（最高），同时演示摘要行显示 `总功绩：20 · 武士头`。
4. 存档 → 重启 Play → 继续 → 功名行与摘要显示军阶不变；重复领取最后动作，军阶与功绩不变。
5. 报告观察值与 Console 计数。


---

<a id="2026-11-02-sc-004-monthly-time-design"></a>
## 来源：docs/superpowers/specs/2026-11-02-sc-004-monthly-time-design.md

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


---

<a id="2026-11-02-sc-005-009-event-demo-design"></a>
## 来源：docs/superpowers/specs/2026-11-02-sc-005-009-event-demo-design.md

# SC-005â€“SC-009 å¯çŽ©äº‹ä»¶æ¼”ç¤ºè®¾è®¡

**çŠ¶æ€ï¼š** è®¾è®¡ç¨¿ï¼›å°šæœªè¿›å…¥ Unity é›†æˆæˆ– Play Testã€‚
**ä¾æ®ï¼š** `GDD_v4.0.md` ç¬¬ 7.2ã€7.5ã€11.6ã€11.8ã€17ã€19.3ã€19.6ã€19.9 èŠ‚ï¼›ç”¨æˆ·ç¡®è®¤æœ¬æ¬¡æ¼”ç¤ºåŒ…å« SC-009 äº‹ä»¶é€‰é¡¹ UIã€‚

## æ„å›¾

æŠŠ SC-005 è‡³ SC-009 åšæˆä¸€ä¸ªå¯ç›´æŽ¥æ“ä½œçš„åž‚ç›´åˆ‡ç‰‡ï¼šçŽ©å®¶ç»“æŸä¸€ä¸ªæœˆåŽï¼Œæ¸¸æˆä»Žç‹¬ç«‹äº‹ä»¶éšæœºæµä¸­ç¡®å®šæ€§åœ°æŒ‘é€‰äº‹ä»¶ï¼›äº‹ä»¶è¿›å…¥å¯ä¿å­˜çš„å¾…é€‰æ‹©çŠ¶æ€ï¼›çŽ©å®¶é€‰é¡¹é€šè¿‡å—é™æ•ˆæžœç±»åž‹æ”¹å˜çŽ°æœ‰æ¸¸æˆçŠ¶æ€å¹¶å†™å…¥åŽ†å²ï¼›è¯»æ¡£åŽä»çœ‹åˆ°ç›¸åŒå¾…å¤„ç†äº‹ä»¶ã€é€‰é¡¹å’ŒåŽç»­éšæœºç»“æžœã€‚

## å½“å‰é¡¹ç›®æƒ…å†µ

- `SaveData`/`SaveDataValidator` æ˜¯ v1ï¼›`SaveSlotStore` ä½¿ç”¨ `JsonUtility` å’Œä¸´æ—¶æ–‡ä»¶æ›¿æ¢ï¼Œå°šæ— æ˜¾å¼è¿ç§»å’Œä¸Šä¸€ä»½å¤‡ä»½ã€‚
- `GameSession` æŒæœ‰æ—¥æœŸã€åœ°ç‚¹ã€ä»»åŠ¡é˜¶æ®µå’Œ NPC å…³ç³»ï¼›å½“å‰åˆ†æ”¯å·²æœ‰æœªæäº¤çš„ SC-004 æœˆæŽ¨è¿›æ”¹åŠ¨ï¼Œå¿…é¡»ä¿ç•™ã€‚
- `BranchingDialogue` ä¸Ž `TownNpcDialoguePanel` å±•ç¤ºäº†åŠ¨æ€ uGUI é€‰é¡¹çš„çŽ°æˆå†™æ³•ï¼Œä½†å®ƒä»¬æ˜¯ç¡¬ç¼–ç  NPC å¯¹è¯ï¼Œä¸åŒ…å«é€šç”¨äº‹ä»¶æ•°æ®ã€æ¡ä»¶ã€å†·å´æˆ–äº‹ä»¶åŽ†å²ã€‚
- SC-003 çš„äººç‰©/å®¶æ—é¢†åŸŸæ–‡ä»¶ä¹Ÿæœ‰æœªæäº¤æ”¹åŠ¨ï¼Œå°šæœªæŽ¥å…¥ `GameSession`ã€‚æœ¬æ¬¡ä¸å¾—é‡å†™æˆ–åˆ é™¤è¿™äº› WIPï¼›æœ¬æ¼”ç¤ºåªåºåˆ—åŒ–å½“å‰ä¼šè¯å®žé™…æ‹¥æœ‰çš„çŠ¶æ€ä»¥åŠæ–°å¢žçš„éšæœºæµå’Œäº‹ä»¶è¿è¡ŒçŠ¶æ€ï¼Œä¸å£°ç§°å®Œæˆ SC-003 é›†æˆã€‚
- å½“å‰æ²¡æœ‰è¯¥é¡¹ç›®çš„ Unity Editor è¿›ç¨‹ï¼›åªèƒ½å‡†å¤‡ä»£ç ä¸Žäº‹åŠ¡å¼åœºæ™¯æ›´æ–°å™¨ã€‚åœºæ™¯æ›´æ–°å’Œ Play Test ç•™åˆ°ç”¨æˆ·é†’æ¥åŽç”±çŽ°æœ‰ Editor å®Œæˆã€‚

## ç›®æ ‡

1. å»ºç«‹å¯å¤çŽ°ã€æŒ‰ç³»ç»Ÿéš”ç¦»çš„éšæœºæµï¼Œå¹¶èƒ½å®Œæ•´æ•èŽ·å’Œæ¢å¤å…¶çŠ¶æ€ã€‚
2. æŠŠå½“å‰ä¼šè¯ã€ç‰ˆæœ¬ä¿¡æ¯ã€å¾…å¤„ç†äº‹ä»¶é¡ºåºã€å†·å´/ä¸€æ¬¡æ€§æ ‡è®°ã€åŽ†å²ä¸Žå·²æäº¤ç»“æžœæ ‡è¯†å†™å…¥ç‰ˆæœ¬åŒ– JSON å­˜æ¡£ã€‚
3. å®šä¹‰åŸºäºŽ JSON çš„äº‹ä»¶ç›®å½•ç»“æž„ï¼Œå¹¶åœ¨åŠ è½½å‰éªŒè¯ IDã€å¼•ç”¨ã€æžšä¸¾ã€æ–‡æœ¬ã€é€‰é¡¹ã€æ¡ä»¶ã€è¦æ±‚ã€æ•ˆæžœå’Œæ•°å€¼èŒƒå›´ã€‚
4. äº‹ä»¶å¼•æ“ŽæŒ‰ç¨³å®šé¡ºåºäº§ç”Ÿã€é‡éªŒå’Œç»“ç®—äº‹ä»¶ï¼›é™åˆ¶ä¸ºé¢„å…ˆå®šä¹‰çš„å®‰å…¨æ“ä½œï¼Œç¦æ­¢ä»Žå†…å®¹æ‰§è¡Œ C#ã€‚
5. åœ¨ FoundationScene æä¾›å¯ç›´æŽ¥çŽ©çš„äº‹ä»¶é€‰é¡¹ UIï¼šç»“æŸæœˆä»½ã€æŸ¥çœ‹äº‹ä»¶ã€é€‰æ‹©ç»“æžœã€çœ‹åˆ°ç»“æžœã€ä¿å­˜å¹¶è¯»æ¡£æ¢å¤å¾…é€‰äº‹ä»¶ã€‚

## éžç›®æ ‡

- ä¸å®žçŽ°å®Œæ•´çš„è·¨ä»£äº‹ä»¶é“¾ã€ç»§æ‰¿ã€30 æ¡æ­£å¼äº‹ä»¶å†…å®¹æˆ–æ‰€æœ‰äº‹ä»¶ç±»åˆ«ã€‚
- ä¸å®žçŽ°å¯æ‰§è¡Œè„šæœ¬ã€ä»»æ„è¡¨è¾¾å¼ã€åå°„å¼æ•ˆæžœæˆ–é€šç”¨äº‹ä»¶æ€»çº¿ã€‚
- ä¸æŠŠå°šæœªé›†æˆçš„ SC-003 äººç‰©/å®¶æ—æ¨¡åž‹é¡ºæ‰‹çº³å…¥ GameSessionã€‚
- ä¸å¢žåŠ  Unity åŒ…ï¼Œä¸ç¼–è¾‘ `Library`ã€`Temp`ã€`ProjectSettings`ã€åœºæ™¯æˆ– prefab æºæ–‡ä»¶ï¼›Unity åœºæ™¯ç”±äº‹åŠ¡å¼ Editor updater åœ¨çŽ°æœ‰ Editor ä¸­ç”Ÿæˆã€‚
- ä¸åˆ›å»ºæäº¤ã€åˆå¹¶ã€æ ‡ç­¾æˆ–æŽ¨é€ï¼›å¿…é¡»ç­‰ç”¨æˆ· Play Mode æŽ¥å—æ¼”ç¤ºã€‚

## æ–¹æ¡ˆä¸Žå†³å®š

### æ–¹æ¡ˆ Aï¼šUnity åŽŸç”Ÿ JSON DTO + èšç„¦éªŒè¯å™¨ + ç±»åž‹åŒ–äº‹ä»¶è¿è¡Œæ—¶ï¼ˆé‡‡ç”¨ï¼‰

æ²¿ç”¨ `JsonUtility`ã€çŽ°æœ‰çº¯ C# é¢†åŸŸæ¨¡åž‹ã€uGUI ä¸Ž `SaveSlotStore`ã€‚å¢žåŠ æ˜Žç¡®çš„å­˜æ¡£è¿ç§»/å¤‡ä»½ã€äº‹ä»¶ JSON DTO ä¸Žè¿è¡Œæ—¶éªŒè¯å™¨ï¼Œä»¥åŠæžšä¸¾é™å®šçš„æ¡ä»¶ã€è¦æ±‚å’Œæ•ˆæžœã€‚æ”¹åŠ¨èŒƒå›´å°ï¼Œèƒ½å°†å­˜æ¡£ã€ç¡®å®šæ€§å’Œå®‰å…¨æ‰§è¡Œè§„åˆ™ç›´æŽ¥çº³å…¥çŽ°æœ‰ä¼šè¯ã€‚

### æ–¹æ¡ˆ Bï¼šå¼•å…¥å®Œæ•´å™äº‹è¿è¡Œæ—¶

Ink é›†æˆåŒ…å«è¿è¡Œæ—¶ã€ç¼–è¯‘å™¨å’Œå¯¼å…¥æµç¨‹ï¼Œé€‚åˆæ›´å¤§è§„æ¨¡åˆ†æ”¯å™äº‹ï¼Œä½†æœ¬æ¬¡ä¸»è¦éœ€æ±‚æ˜¯çŠ¶æ€åž‹äº‹ä»¶ã€ç¡®å®šæ€§æŽ’ç¨‹åŠå®‰å…¨æ•ˆæžœï¼›å®ƒä¼šæ‰©å¤§ä¾èµ–é¢ä¸”ä¸èƒ½æ›¿ä»£æ¸¸æˆçŠ¶æ€çš„éªŒè¯ä¸ŽåŽŸå­æäº¤ã€‚

### æ–¹æ¡ˆ Cï¼šå¼•å…¥å¤–éƒ¨ JSON Schema éªŒè¯åŒ…

å¯å‡å°‘éƒ¨åˆ†ç»“æž„éªŒè¯ä»£ç ï¼Œä½† Scout æŠ¥å‘ŠæŒ‡å‡º Unity/System.Text.Json å…¼å®¹æ€§å°šæœªæ ¸å®žï¼Œä¸” JsonSchema.Net çš„ NuGet äºŒè¿›åˆ¶åˆ†å‘æ¡æ¬¾å¯èƒ½å¢žåŠ å•†ä¸šè´¹ç”¨ã€‚é¡¹ç›®è§„åˆ™ç¦æ­¢æœªç»æ‰¹å‡†å®‰è£…åŒ…ï¼›æœ¬æ¬¡ä¸é‡‡ç”¨ã€‚

**æŠ€æœ¯å†³å®šï¼š** é‡‡ç”¨æ–¹æ¡ˆ Aï¼Œä¸åŠ åŒ…ã€‚äº‹ä»¶ä¼˜å…ˆçº§æŒ‰æ•°å€¼é™åºï¼Œå¹³çº§æŒ‰ `EventId` çš„ ordinal é¡ºåºç¨³å®šæŽ’åºï¼›ä»Žæœ€é«˜ä¼˜å…ˆçº§çš„å¯è§¦å‘ç»„å†…æŒ‰æƒé‡é€‰æ‹©ã€‚éšæœºæ•°ä½¿ç”¨é¡¹ç›®è‡ªæœ‰çš„å›ºå®šç®—æ³•ä¸Žæ˜Žç¡®åºåˆ—åŒ–çŠ¶æ€ï¼Œç»ä¸ä¾èµ– `System.Random` æˆ–è¿è¡Œæ—¶å“ˆå¸Œé¡ºåºã€‚äº‹ä»¶é€‰é¡¹æäº¤å‰é‡éªŒï¼›æœ¬æ¼”ç¤ºçš„å®‰å…¨æ•ˆæžœå…ˆè¦†ç›–çŽ°æœ‰ NPC å…³ç³»å¢žé‡ï¼Œå¹¶åœ¨éªŒè¯é€šè¿‡åŽä¸€æ¬¡æ€§æäº¤æ•ˆæžœã€å†·å´/ä¸€æ¬¡æ€§æ ‡è®°å’ŒåŽ†å²ç»“æžœã€‚

### é˜Ÿåˆ—è¾¹ç•Œï¼ˆP8 æ¾„æ¸…ï¼‰

GDD 7.2 çš„æœˆæœ«è§¦å‘å®šä¹‰çš„æ˜¯ä¸€æ¬¡åŠ æƒé€‰æ‹©ï¼Œ7.4 çš„è·¨ä»£äº‹ä»¶é“¾å±žäºŽæœ¬æ¬¡éžç›®æ ‡ï¼›è®¡åˆ’ Task 4 çš„â€œqueues eligible events in stable orderâ€æŒ‡å€™é€‰é›†åˆçš„ç¨³å®šæŽ’åºä¸Žå•æ¬¡å…¥é˜Ÿï¼Œè€ŒéžåŒä¸€æœˆæœ«æ‰¹é‡å…¥é˜Ÿã€‚å› æ­¤æœˆåº¦è·¯å¾„æ¯ä¸ªå›žåˆæœ€å¤šå…¥é˜Ÿä¸€ä¸ªäº‹ä»¶ï¼Œå¾…é€‰æœŸé—´æ‹’ç»å†æ¬¡ç»“æŸæœˆä»½ã€‚`EventRuntimeState` ä»æŒ‰æœ‰åºå¤šäº‹ä»¶é˜Ÿåˆ—å®žçŽ°å¹¶ä¿ç•™â€œåªæäº¤é˜Ÿé¦–â€çš„è¯­ä¹‰ï¼Œä¾›æœªæ¥äº‹ä»¶é“¾æˆ–å…¶å®ƒè§¦å‘å™¨ä½¿ç”¨ï¼›æ¼”ç¤ºä¸å£°ç§°å¤šäº‹ä»¶åŒæ—¶å¾…é€‰å¯è¾¾ã€‚è¾¹ç•Œç”± `ScheduleMonthlyRejectsSecondEventWhilePending`ã€`MonthTurnQueuesAtMostOneEventAndBlocksUntilResolved` è¦†ç›–ï¼Œé˜Ÿåˆ—å­˜å‚¨çš„é¡ºåºè¯­ä¹‰ç”± `RestoredMultiEventQueueCommitsHeadInOrder` è¦†ç›–ã€‚

## å¯çŽ©å›žè·¯

1. çŽ©å®¶åœ¨çŽ°æœ‰ FoundationScene ç‚¹å‡»â€œç»“æŸæœˆä»½â€ã€‚
2. æœˆä»½æŽ¨è¿›æˆåŠŸåŽï¼Œäº‹ä»¶å¼•æ“Žæ ¹æ®äº‹ä»¶éšæœºæµç­›é€‰å¹¶æŽ’å®šäº‹ä»¶ï¼›äº‹ä»¶æŒ‰ç¨³å®šé˜Ÿåˆ—æš‚åœåœ¨çŽ©å®¶å†³ç­–ç‚¹ã€‚
3. `EventChoicePanel` æ˜¾ç¤ºäº‹ä»¶æ–‡æœ¬å’Œå½“å‰å¯ç”¨é€‰é¡¹ï¼›æ¯ä¸ªé€‰é¡¹æ˜¾ç¤ºæ–‡å­—åŠå…¶å·²å®šä¹‰çš„ä»£ä»·/ç»“æžœæ‘˜è¦ã€‚ä¸å¯ç”¨é€‰é¡¹æ˜¾ç¤ºåŽŸå› ä¸”ä¸èƒ½ç‚¹å‡»ã€‚
4. çŽ©å®¶é€‰æ‹©åŽï¼Œå¼•æ“Žé‡æ–°éªŒè¯äº‹ä»¶ã€é€‰é¡¹è¦æ±‚å’Œæäº¤ IDï¼›ä»»ä½•å¤±è´¥å‡ä¸æ”¹å˜å…³ç³»ã€åŽ†å²æˆ–å¾…å¤„ç†é˜Ÿåˆ—ã€‚
5. æˆåŠŸåŽåº”ç”¨å®‰å…¨æ•ˆæžœã€è®°å½•äº‹ä»¶ç»“æžœã€æ›´æ–°å†·å´/ä¸€æ¬¡æ€§æ ‡è®°å¹¶æ˜¾ç¤ºåé¦ˆï¼›æœ¬æ¬¡æœˆåº¦è·¯å¾„åœ¨é˜Ÿåˆ—æ¸…ç©ºåŽå›žåˆ° Managementï¼Œæœªæ¥è§¦å‘å™¨è‹¥æŽ’å…¥æ›´å¤šäº‹ä»¶åˆ™æŒ‰é˜Ÿåˆ—é¡ºåºç»§ç»­ç­‰å¾…é€‰æ‹©ã€‚
6. åœ¨å¾…é€‰çŠ¶æ€ä¿å­˜å¹¶é‡å¯/è¯»æ¡£æ—¶ï¼Œç»§ç»­å±•ç¤ºç›¸åŒé˜Ÿåˆ—å’Œé€‰é¡¹ï¼ŒåŽç»­éšæœºç»“æžœä¸Žä¸ä¸­æ–­è¿è¡Œä¸€è‡´ã€‚

## å­˜æ¡£å…¼å®¹

å°†å½“å‰æ ¼å¼æå‡åˆ° v2ã€‚ä¿ç•™æ—§å­—æ®µå¹¶æ–°å¢žæ¸¸æˆã€è§„åˆ™ã€äº‹ä»¶å†…å®¹ç‰ˆæœ¬ï¼›åˆå§‹ seedï¼›æ¯ä¸ªéšæœºæµçš„å…¨éƒ¨å†…éƒ¨çŠ¶æ€ï¼›ç¨³å®šé˜¶æ®µæ ‡è®°ï¼›æœ‰åºå¾…å¤„ç†äº‹ä»¶ï¼›å†·å´ä¸Ž once æ ‡è®°ï¼›åŽ†å²å’Œå·²æäº¤ç»“æžœ IDã€‚æ˜Žç¡®è¿ç§»çŽ°æœ‰ v1 å­˜æ¡£å¹¶ä¸ºç¼ºå¤±å­—æ®µå¡«å…¥å›ºå®šã€æ–‡æ¡£åŒ–çš„é»˜è®¤å€¼ï¼›åªæ‰¿è¯ºå®žé™…å®žçŽ°ä¸”æµ‹è¯•é€šè¿‡çš„è¿ç§»è·¯å¾„ã€‚å†™å…¥ä¸´æ—¶æ–‡ä»¶åŽé‡æ–°è¯»å–å¹¶éªŒè¯ï¼Œå†æ›¿æ¢æ­£å¼æ–‡ä»¶å¹¶ä¿ç•™ä¸Šä¸€ä»½å¤‡ä»½ã€‚è¿ç§»ã€æ ¡éªŒã€è¯»æ¡£æˆ–æ›¿æ¢å¤±è´¥ä¸å¾—è¦†ç›–æœ‰æ•ˆåŽŸå­˜æ¡£ï¼Œä¹Ÿä¸å¾—éƒ¨åˆ†ä¿®æ”¹æ´»è·ƒä¼šè¯ã€‚v2 JSON è‹¥ç¼ºå°‘ `randomState`ã€`eventRuntime` æˆ–å¿…éœ€ç‰ˆæœ¬å…ƒæ•°æ®åˆ™æ‹’ç»ï¼Œä¸èƒ½æŽ¥å— `JsonUtility` æž„é€ å™¨é»˜è®¤å€¼ï¼›æ´»åŠ¨æ§½å·²æŸåæ—¶ï¼Œä¿å­˜ä¼šå…ˆæŠŠåŽŸå§‹å­—èŠ‚å¤åˆ¶åˆ° `save-slot-1.json.corrupt-<UTC æ—¶é—´æˆ³>`ï¼Œå†å†™å…¥æ–°çš„æœ‰æ•ˆå­˜æ¡£ï¼Œæ¢å¤å‰¯æœ¬ä¸ä¼šè¢«é™é»˜åˆ é™¤ã€‚

## å®‰å…¨ä¸Žå¤±è´¥å¤„ç†

- JSON åªæè¿°æ•°æ®ï¼Œæ¡ä»¶ä¸Žæ•ˆæžœéƒ½æ˜ å°„åˆ°æ˜¾å¼æžšä¸¾/ç±»åž‹ï¼›æœªè¯†åˆ«ç±»åž‹ã€ç¼ºå¤±æ–‡æœ¬ã€æœªçŸ¥ IDã€é‡å¤ IDã€æ–­é“¾å¼•ç”¨ã€æ— æ•ˆæƒé‡ã€éžæ³•å†·å´æˆ–è¶Šç•Œæ•°å€¼å‡åœ¨åŠ è½½é˜¶æ®µæ‹’ç»ã€‚
- å€™é€‰é›†åˆå¿…é¡»å…ˆè¿‡æ»¤æ¡ä»¶ã€å†·å´å’Œ once çº¦æŸï¼Œç¨³å®šæŽ’åºåŽå†æ¶ˆè´¹äº‹ä»¶éšæœºæµï¼›æ²¡æœ‰å€™é€‰æ—¶ä¸æŽ¨è¿›äº‹ä»¶æµã€‚
- é€‰æ‹©å‰é‡éªŒæ¡ä»¶å’Œè¦æ±‚ã€‚ç”¨å®Œæ•´çš„é¢„æ£€ä¸Žä¸´æ—¶çŠ¶æ€æž„é€ é¿å…éƒ¨åˆ†æ•ˆæžœæäº¤ï¼›ç”¨æŒä¹…åŒ–çš„æäº¤ç»“æžœ ID é˜²æ­¢é‡å¤åº”ç”¨ã€‚
- å¾…é€‰æ‹©æœŸé—´æš‚åœå†æ¬¡ç»“æŸæœˆä»½ï¼›éžæ³•è¯»æ¡£ä¿æŒå½“å‰ä¼šè¯åŽŸæ ·ã€‚

## éªŒæ”¶è¯æ®

- åŒä¸€ seed ä¸Žè°ƒç”¨åºåˆ—äº§ç”Ÿç›¸åŒåºåˆ—ï¼›ä¸€ä¸ªç³»ç»Ÿé¢å¤–æŠ½å–éšæœºæ•°ä¸ä¼šæ”¹å˜äº‹ä»¶æµï¼›æ•èŽ·/æ¢å¤åŽç»­åºåˆ—ä¸€è‡´ã€‚
- v1 åˆ° v2 è¿ç§»ä¿ç•™æ—§æ¸¸æˆçŠ¶æ€ï¼›v2 åœ¨ç¨³å®šå¾…é€‰äº‹ä»¶ç‚¹å¾€è¿”åŽäº‹ä»¶é¡ºåºã€é€‰é¡¹ã€å†·å´/æ ‡è®°ã€åŽ†å²å’Œå…¨éƒ¨éšæœºæµä¸€è‡´ï¼›æŸåæ•°æ®ä¸è¦†ç›–æ—§å­˜æ¡£ã€‚
- äº‹ä»¶ç›®å½•éªŒè¯èƒ½æ‹’ç»é‡å¤/ç¼ºå¤±/æ–­é“¾/éžæ³•å†…å®¹ï¼›æœªçŸ¥æ•ˆæžœä¸èƒ½è¢«æ‰§è¡Œã€‚
- äº‹ä»¶æŽ’ç¨‹é¡ºåºä¸Žæƒé‡é€‰æ‹©å¯ç¡®å®šæ€§é‡çŽ°ï¼›æ— æ•ˆ/è¿‡æœŸ/é‡å¤é€‰é¡¹ä¸ä¼šæ”¹å˜çŠ¶æ€ï¼›æˆåŠŸé€‰é¡¹æ•ˆæžœåŠåŽ†å²åªæäº¤ä¸€æ¬¡ã€‚
- äº‹ä»¶ UI ä¸ŽçŽ°æœ‰åœºæ™¯ç»‘å®šé€šè¿‡æ–‡æœ¬åœºæ™¯éªŒè¯å™¨ï¼›çœŸå®ž Editor ç¼–è¯‘æˆåŠŸã€EditMode å…¨é€šè¿‡ã€Console é”™è¯¯ä¸ºé›¶ã€FoundationScene Play Mode å¯ä»Žç»“æŸæœˆä»½çŽ©åˆ°äº‹ä»¶é€‰æ‹©ä¸Žç»“ç®—ï¼Œå†ä¿å­˜/è¯»æ¡£æ¢å¤å¾…é€‰äº‹ä»¶ã€‚
- ä¸Šè¿° Unity è¿è¡Œè¯æ®ç”±ç”¨æˆ·é†’æ¥åŽæ‰“å¼€çš„é¡¹ç›® Editor èŽ·å–ï¼›ä¸èƒ½ç”±æºä»£ç æ£€æŸ¥æ›¿ä»£ï¼Œä¹Ÿä¸ä½¿ç”¨æˆªå›¾ã€‚

## å·²çŸ¥çº¦æŸä¸Žå‰©ä½™å†³å®š

- OpenCode å½“å‰æ´¾å·¥å› ä¸Šæ¸¸ `Insufficient account funds` è¿žç»­å¤±è´¥ï¼›å®žçŽ°ä¸Žç‹¬ç«‹åªè¯»å®¡æŸ¥å¿…é¡»ä»ç”± Orca/OpenCode é¡ºåºå®Œæˆï¼Œä¸æ”¹ providerã€å‡­æ®æˆ–è®¡åˆ’ã€‚
- å…·ä½“ JSON å­—æ®µåã€æžšä¸¾å€¼åŠ C# ç±»åž‹ç”±å®žæ–½è®¡åˆ’å›ºå®šï¼›å®žæ–½è€…å¯åœ¨æ—¢æœ‰æŽ¥å£å†…åšå±€éƒ¨å®žçŽ°é€‰æ‹©ï¼Œä½†ä¸å¾—æ‰©å¤§æ•ˆæžœæƒé™æˆ–ç¼–è¾‘åœºæ™¯æ–‡ä»¶ã€‚
- æœ¬è®¾è®¡ä¸Žè®¡åˆ’æ–‡ä»¶å¯å…ˆå‡†å¤‡ï¼›åœ¨ OpenCode èµ„æºæ¢å¤ä¸”ç”¨æˆ·å°šæœªé†’æ¥æ—¶ï¼Œä»ä¸å¾—æ“…è‡ªæ‰“å¼€æ–°çš„ Unity Editor æˆ–åšç”¨æˆ·ä¿ç•™çš„ Play Testã€‚


---

<a id="2026-11-02-sc-011-cross-generation-event-design"></a>
## 来源：docs/superpowers/specs/2026-11-02-sc-011-cross-generation-event-design.md

# SC-011 Cross-Generation Event — Design Spec

**Status:** user accepted, 2026-11-02. The user reports compilation passed, EditMode passed, no Console errors, and Play Test passed. EditMode totals and Console warning count were not supplied; `FoundationSceneValidator` result was not reported. This record is based on user report, not captured Editor logs. Static review is not runtime evidence.

**Authority:** `GDD_v4.0.md` 7.4 (cross-generation chains preserve decision consequences, not cutscenes), 17 (SC-011 acceptance: 后代受到先代决定影响), 19.1 (re-check conditions before display and re-validate before commit), 19.2 (succession, adult/regency handoff, no looping branches), 19.3 (stable save points, compatibility, recovery), and 19.4 (first generation makes a costly choice, the second faces its consequence).

**Baseline:** SC-010 is user-accepted (commit `af35633`, tag `demo-SC010`, per coordinator). `PROJECT_STATE.md` and `DECISIONS.md` are dirty stale snapshots; this design task must not edit them. The later implementation/docs task updates the checkpoint and the next target.

## 1. Smallest complete slice

One playable vertical slice, not a chain framework:

1. The accepted first-generation event `event.rainy-arrival` is played and its chosen `optionId` is saved.
2. One real SC-010 head change occurs.
3. The successor receives one follow-up encounter whose visible text and enabled choices depend on the saved option.
4. Save/load works at every stable decision point before, during, and after the handoff.

The existing rainy-arrival event is the generation-one root and keeps its text, options, effects, requirements, weight, priority, cooldown, once scope, and chain membership. Kuroda Iori is an old guest, not a displaced youth. GDD 7.4's displaced-youth/old-retainer pattern is the narrative inspiration for the follow-up, not a claim that Iori is that youth or a family retainer.

## 2. Findings that constrain the design

- `EventRuntimeState` already persists `History` (`eventId`, `optionId`, `resultId`, year, month, summary), cooldowns, once markers, committed result IDs, and the pending queue. The first choice is already durable.
- `chainId` is validated metadata only. No chain scheduling exists.
- The only trigger is `Monthly`; the event engine sorts by priority descending, then ordinal `eventId`, and rolls weight only inside the top-priority group.
- Effects are allowlisted; there is no arbitrary C# execution and no runtime text templating.
- Succession uses `PersonId` and `SuccessionWorld`; event effects use `CharacterId` and `CharacterRelationships`. They are distinct models. The follow-up must not pretend they are interchangeable.
- Extending this local allowlisted engine is smaller and safer than adding an external narrative dependency; no researched dependency beats it for this one-event slice.

### 2.1 Design/plan audit against GDD and current source (2026-11-02)

- **GDD 7.5 "支持人物姓名、称谓、身份等变量替换"** is a general technical specification, not an SC-011 acceptance item. The current engine has no text templating, and adding one would be a separate subsystem. This slice uses two content variants with fixed original text; templating remains future work and is not claimed here.
- **GDD 19.1 "事件展示前重新检查条件" and "选项提交前再次验证"** are preserved for history conditions and option requirements. Succession-count conditions are deliberately **offer-time only** because a pending event must remain resolvable across a head change; re-checking a succession gate at commit would make a saved stable decision uncommittable. This is the one documented deviation, and it is limited to the two succession-count condition kinds. `GameSession` does pass the rule context to the commit call; `EventEngine` ignores only the succession gates there and still re-checks history conditions and option requirements.
- **GDD 19.4** requires an M2 three-generation script with at least two handoffs. SC-011's acceptance is narrower ("后代受到先代决定影响"). This slice is one two-generation component of the M2 script, not the M2 end-to-end script; the second-generation consequence is what SC-011 must prove.
- **Current source check:** `SuccessionPhase`, `SuccessionCount`, append-only event `History`, allowlisted `EventConditionKind`, and the single `Monthly` trigger all exist as the spec assumes. Before this slice, `GameSession.TryApplySave` required exact `eventContentVersion` equality; the `1 → 2` window is therefore an explicit additive change, not pre-existing behavior. Its tests are authored but not executed.
- **Root conditions:** the accepted `event.rainy-arrival` previously used a single redundant `{"type":"Always"}` condition. Task 3 replaced that array with exactly `[{"type":"SuccessionCountEquals","threshold":0}]`; removing the redundant `Always` changes no behavior and keeps the new gate the only event-level condition.

## 3. Chosen approach

**Option A (chosen): keep the monthly trigger and add three allowlisted condition kinds.**

- `EventOptionChosen` tests the **last committed history entry** for a source event and option. History is append-only, so "last" is deterministic and a duplicate entry cannot create ambiguity.
- `SuccessionCountEquals` and `SettledSuccessionCountAtLeast` read a small `EventRuleContext` supplied by `GameSession`.
- `SuccessionCount` is the raw SC-010 `SuccessionCount` (increments on any handoff, including the start of a regency).
- `SettledSuccessionCount` is `SuccessionCount` only when `SuccessionPhase.None`; it is `0` during a pending succession, a regency, or extinction. This is the "successor actually holds control" count.

**Succession-count conditions are offer-time gates only.** They are evaluated by `ScheduleMonthly`/`GetEligibleEventIds`, not re-evaluated when a pending event is committed. This guarantees that a stable saved event can always be resolved even if the head later changes or the family later ends. `EventOptionChosen` is re-checked at commit because committed history never changes.

No new trigger, no new save field, no new NPC, no new scene, no new package, and no runtime text engine.

**Rejected alternatives:**

- New `SuccessionCompleted` trigger: needs a new scheduling API, new handoff hooks in `GameSession`, and new failure/ordering paths for no visible gain.
- Dynamic text templates: requires a text-variant subsystem; two mutually exclusive content variants are smaller and easier to verify.
- A new NPC or `PersonId`/`CharacterId` bridge: would conflate the two models or expand the roster and save validation.
- External dialogue/event package: adds a dependency for one event and cannot replace the existing validation/commit path.

## 4. Content

### 4.1 Generation-one root (unchanged, one added eligibility condition)

`event.rainy-arrival` remains exactly as accepted:

| Field | Value |
|---|---|
| Trigger | `Monthly` |
| Weight / priority | `10` / `10` |
| Cooldown / once | `3` months / `PerGame` |
| Chain | `chain.rainy-arrival` |
| Options | `option.rainy-arrival.offer-rest` (+5 `npc.kuroda.iori`), `option.rainy-arrival.ask-tomorrow` (unchanged `RelationshipAtLeast 1` requirement, no effect) |
| Text | unchanged `event.rainy-arrival`, option texts and summaries |

One event-level condition is added: `SuccessionCountEquals 0`. It replaces the redundant `Always` entry, so the root's `conditions` array is exactly `[{"type":"SuccessionCountEquals","threshold":0}]`. It prevents the root from being offered after any head change. It does not alter text, options, option requirements, effects, weight, priority, cooldown, once scope, or chain membership. Before the first succession, the accepted demo behaves identically.

### 4.2 One follow-up encounter, two mutually exclusive variants

The engine has no text templating, so the single follow-up step is represented by two event definitions. They share a chain step, have distinct visible text and choice sets, and exactly one can ever be eligible. Calling this "two events" overstates the content: it is one encounter with two narrative variants.

**Variant A — sheltered (root option `offer-rest`)**

| Field | Value |
|---|---|
| Event ID | `event.rainy-arrival.legacy-honored` |
| Trigger | `Monthly` |
| Weight / priority | `1` / `100` |
| Cooldown / once | `0` / `PerGame` |
| Chain | `chain.rainy-arrival` |
| Conditions | `EventOptionChosen(event.rainy-arrival, option.rainy-arrival.offer-rest)`; `SettledSuccessionCountAtLeast 1` |
| Text | `event.rainy-arrival.legacy-honored` (new) |

Title: `旧客的雨夜回访`. Body: `又是一夜细雨。黑田伊织站在清洲院门外，手里仍是那封未拆的家书。他没有提先代的名字，只问新家主是否还记得当年那盏热茶。`

| Option | Text | Effect |
|---|---|---|
| `option.legacy-honored.renew` | `重申先代的门诺` | Iori +5 |
| `option.legacy-honored.terms` | `按当下利害重议` | none |
| `option.legacy-honored.close` | `止于这一夜` | Iori -5 |

Option bodies and summaries:

- `renew`: body `当着伊织的面承认旧约，并请他再次入屋。`; summary `伊织确认这份旧情没有随先代离去，对家族的信任增加五点。`
- `terms`: body `旧情可以续，但条件要按今日的家族处境重写。`; summary `伊织接受了新的往来条件，旧约以另一种方式延续。`
- `close`: body `以家族事务繁忙为由，结束这段旧交。`; summary `伊织收起家书离去，家族与他的旧情转冷五点。`

**Variant B — deferred (root option `ask-tomorrow`)**

| Field | Value |
|---|---|
| Event ID | `event.rainy-arrival.legacy-deferred` |
| Trigger | `Monthly` |
| Weight / priority | `1` / `100` |
| Cooldown / once | `0` / `PerGame` |
| Chain | `chain.rainy-arrival` |
| Conditions | `EventOptionChosen(event.rainy-arrival, option.rainy-arrival.ask-tomorrow)`; `SettledSuccessionCountAtLeast 1` |
| Text | `event.rainy-arrival.legacy-deferred` (new) |

Title: `门房里的旧雨`. Body: `雨声与当年一样。黑田伊织再次来到清洲，却先问起先代为何让他在门房等到天亮。新家主要给这段旧事一个答复。`

| Option | Text | Effect |
|---|---|---|
| `option.legacy-deferred.atone` | `补上先代欠下的礼数` | Iori +5 |
| `option.legacy-deferred.explain` | `说明当日不得已` | none |
| `option.legacy-deferred.dismiss` | `旧事不必再提` | Iori -5 |

Option bodies and summaries:

- `atone`: body `正式迎伊织入内，为当年那一夜的怠慢致歉。`; summary `伊织接受了迟来的礼数，对家族的信任增加五点。`
- `explain`: body `把当年的顾虑与今日的处境一并说清，请他自行判断。`; summary `伊织听完了解释，旧怨没有加深，也没有完全消散。`
- `dismiss`: body `告诉伊织，先代的做法无须由新家主补偿。`; summary `伊织记住了这次拒绝，对家族的信任下降五点。`

All follow-up option requirements are explicit `Always`. Effects stay inside the existing `RelationshipDelta` allowlist and the `[-100, 100]` range. The text frames Iori's standing with the house, not the new head's private relationship; the current model has one family-level NPC relationship scalar and this slice does not claim personal relationships migrate.

No third generation, no new NPC, no new root event, and no change to the accepted rainy-arrival text or option effects/requirements.

## 5. Exact behavior

### 5.1 Generation gating

- Root: offered only while `SuccessionCount == 0`. After any handoff (adult, regency start, crisis adoption), raw count is at least `1`, so the root is never offered again.
- Follow-up: offered only while `SettledSuccessionCount >= 1` and the matching root option is the last committed history entry for `event.rainy-arrival`.
- If the root was never chosen before the first handoff, no follow-up exists. The generation boundary is the first handoff; later choices cannot reopen the root because its offer gate is now false.
- If a root event was already offered before the handoff and is still pending, it remains resolvable after the handoff (offer-time gate). Its legacy can then appear in a later month. This is an explicit edge case, not the Play Test path.

### 5.2 Adult handoff

`ConfirmSuccession` (or a valid designated-heir handoff, or crisis adoption) succeeds with `SuccessionPhase.None` and `SuccessionCount == 1`.

- The handoff itself does not queue an event; SC-010's modal, feedback, and stable state are unchanged.
- On the next `结束月份`, `ScheduleMonthly` sees the root gate false, the follow-up gate true, and the matching history option. It schedules exactly one variant for the new head.
- The event is shown by the existing event-choice panel after the month advances. No new UI or scene binding is required.

### 5.3 Regency handoff

`ConfirmSuccession` for a minor succeeds with `SuccessionPhase.Regency` and `SuccessionCount == 1`.

- `SettledSuccessionCount` is `0` while the regency is active, so neither variant is offered during the regency. This respects GDD 19.2's restricted regency decisions and avoids treating the regent as the recipient.
- Month turns continue under the existing SC-010 rules; no follow-up is scheduled.
- `TryCompleteSuccessionRegency` at the adulthood threshold moves the phase to `None` with the same head and count. `SettledSuccessionCount` becomes `1`.
- On the next `结束月份` after that explicit handoff, the matching variant is scheduled for the now-adult head. The handoff itself still does not advance time or grant a second month budget.
- If the family becomes extinct, settled count stays `0`; no follow-up is offered and time remains blocked.

### 5.4 Duplicate and repeated succession

- The follow-up is once per campaign (`PerGame`) and keyed by the root history entry. It cannot repeat after a second or later succession.
- A second succession before the follow-up is resolved does not duplicate it. If the follow-up is already pending, it remains resolvable because succession gates are offer-time only, and the current head receives it. If it has not yet been scheduled, a pending or active succession keeps settled count at `0`; after the next handoff settles, the follow-up becomes eligible again and is scheduled at the next month-end. This is an edge case; the Play Test completes the follow-up before starting another succession.
- A second succession after the follow-up is resolved does not reopen the root and does not schedule another follow-up. The once marker and history are persisted.
- Loading a save from before the follow-up and replaying the handoff is a rolled-back timeline, not a duplicate. The once marker and history from that save govern.
- Extinction, a pending succession, and an active regency all keep settled count at `0`, so no follow-up is offered.

### 5.5 Deterministic ordering

- Monthly candidate ordering is unchanged: priority descending, then ordinal `eventId`; weighted roll only within the top-priority group.
- At count `0`, the two follow-ups are ineligible, so the accepted root remains the only top-priority candidate. The accepted demo order does not change.
- At settled count `>= 1`, exactly one variant can be eligible: the last root history entry has one option, and the two variants key different options. `weight = 1` with one candidate is a fixed selection. The catalog validator additionally rejects two variants claiming the same `(sourceEventId, sourceOptionId)` pair.
- At most one event is queued per month, as in SC-005–SC-009. No queue semantics change.
- History order is append-only and preserved by save/load; "last entry" is stable.

### 5.6 Failed scheduling

- If `ScheduleMonthly` fails, the existing `TryAdvanceToNextMonth` path restores the RNG snapshot and does not advance the clock or replace runtime state. Outcome is `SchedulingFailed` with the engine message.
- If the catalog/text is unavailable or invalid, outcome is `ContentUnavailable`; no partial event is queued.
- Because there is no persisted "due" flag, a retry on the next `结束月份` attempt is allowed. A deterministic content error keeps failing with the same message; succession state, pending decisions, and saves stay intact.
- If no variant matches (root not chosen, count not settled), scheduling succeeds with no event and the month advances. That is "not due", not a failure.
- Coverage boundary: the no-mutation guarantee is covered by an authored direct `EventEngine` test (a failed `ScheduleMonthly` from pending/non-management readiness leaves the runtime snapshot and event RNG unchanged). Full session-level date/succession failure injection is **deferred/unreachable without a production hook**, because a valid catalog plus the session's own guards leave no reachable `SchedulingFailed` path. No test hook was added and the existing rollback code is unchanged; that session-level check remains a Unity/manual gate.

### 5.7 Stable save points

All points below use the existing `StableGamePhase.Management` or `AwaitingEventChoice` model. No save is taken during command execution.

| Point | Persisted evidence | On load |
|---|---|---|
| Root pending | pending event, phase, history empty | same root and options |
| After root choice | history entry, committed result, once marker, relationship delta | same choice; root not re-offered |
| Succession pending | SC-010 succession fields | same phase/candidates/pending selection |
| Adult handoff, before next month | phase `None`, count `1`, no pending event | next `结束月份` schedules the matching variant |
| Regency active / after majority | phase, head, count, regency flag | no follow-up during regency; after majority the next month schedules it |
| Follow-up pending | pending event ID, phase, history, relationships | same variant and option set |
| After follow-up choice | history with both events, once/committed markers, relationship delta | no repeat; later succession does not reopen the chain |

Authored coverage (not executed): `RootPendingSaveRestoresSameRootAndOptions`, `AfterRootChoiceSaveDoesNotReofferRoot`, `PostHandoffSaveReplaysSameVariant`, `RegencyCheckpointBeforeFollowUpReplaysAfterMajority`, `FollowUpPendingSaveKeepsVariantOptionsAndRelationshipValue`, and `AfterFollowUpChoiceReloadKeepsHistoryAndOnceMarker`. Succession-pending save/load remains covered by the existing SC-010 `SuccessionSaveTests`.

## 6. Content version and save compatibility

- Catalog and text `contentVersion` become `2`; `SaveData.CurrentEventContentVersion` becomes `2`. The resource file names stay `event-catalog-v1.json` and `event-text-zh-v1.json` to avoid asset moves and GUID churn; the internal version is the contract.
- Save schema stays `3`: no new persisted field is added. Pending events, history, once markers, and succession state already exist.
- New saves write content `2`.

Compatibility matrix:

| Save | Saved content | Current content | Result |
|---|---|---|---|
| v3 | `1` | `2` | Accept only if `TryRestoreState` validates every referenced ID; the next save writes `2`. |
| v3 | `2` | `2` | Accept. |
| v3 | any other value | `2` | Reject atomically with the existing content-version message; the live session and the save file are unchanged. |
| v2 | `1` (required) | `2` | `SaveDataValidator.ValidatePresence` rejects a v2 payload that omits `eventContentVersion` as missing required metadata. With `1`, migrate schema to v3, then accept through the same `1 → 2` additive window if the event runtime validates. |
| v1 | missing/default | `2` | `ValidatePresence` deliberately skips the metadata check for v1, so the migrator supplies the current content version (`2`) with schema v3; accept. |

Schema v2 is a pre-content-2 format, so a genuine v2 save carries `eventContentVersion` `1` and must include that field: a v2 payload that omits it is rejected by `SaveDataValidator.ValidatePresence` as missing required metadata rather than defaulted. Schema v1 is the exception: it predates content versioning, so `ValidatePresence` skips the metadata check and the migrator writes the current content version `2`.

The `1 → 2` window is one-way and is allowed because the only previously accepted content is preserved: the root's text, options, effects, and requirements are unchanged and the new events cannot be referenced by a content-1 save. Any unknown saved event/option still fails `TryRestoreState`. There is no `2 → 1` downgrade and no promise for content `3`; a future version must define its own compatibility.

A content-1 save that has a pending root and a completed handoff can still resolve the root because succession-count gates are offer-time only. Its follow-up then appears the next month. This is an explicit migration edge case; the Play Test starts a new game and does not rely on it.

## 7. Acceptance and evidence boundary

The user reported the text-only Play Test passed and explicitly accepted this slice on 2026-11-02. Their report also says compilation passed, EditMode passed, and there were no Console errors. EditMode totals, Console warning count, and `FoundationSceneValidator` result were not provided.

The Play Test covers:

1. New game → `结束月份` → rainy-arrival → choose `请他进屋歇息` → save.
2. Retire → choose 明智 正綱 → confirm; head changes once; date/location/mission/relationship state otherwise intact; save.
3. `结束月份` → variant A (旧客的雨夜回访) → load the step-2 save before choosing (the game has one save slot), `结束月份` again → same variant A; choose `重申先代的门诺`; save; `结束月份` again → no repeat.
4. Load the post-choice save from step 3 → `结束月份` → no repeat and no reopened root.
5. New game → before the first `结束月份`, travel to Atsuta and use 黒田伊织's town dialogue to raise his trust to at least 1 (accept his proposition), then return to the map. This setup is required because the accepted `请他明晨再来` option keeps its existing `RelationshipAtLeast 1` requirement and is disabled at the default value 0. Then `结束月份` → choose `请他明晨再来` → retire to 正綱 → `结束月份` → variant B (门房里的旧雨) with a different body and different option titles; choose `补上先代欠下的礼数`.
6. Record each step pass/fail and the visible text in the test record, without screenshots.

Regency behavior is covered by authored EditMode tests; the user reports EditMode passed but did not provide per-suite counts. Do not infer the scene validator passed. User acceptance authorizes the SC-011 checkpoint integration; it does not supply missing counts or scene-validation evidence.

## 8. Out of scope

- A new monthly root event, a new NPC, a displaced-youth character, or a third-generation event.
- SC-012 chronicle, faction politics, economy costs, and regency-only decision content.
- New packages, external narrative runtimes, dynamic text templating, or arbitrary condition/effect scripts.
- Scene, prefab, `ProjectSettings`, `Library`, or `Temp` changes; no new worktree or branch in this design task.
- Rewriting SC-003/SC-010 models or changing `PlayerMovement2D`.

## 9. Risks and decisions

- **Risk:** treating two content variants as two events. **Decision:** they are one follow-up step; the spec and tests name the encounter and assert exactly one variant can be eligible.
- **Risk:** content-version churn breaking old saves. **Decision:** a narrow `1 → 2` additive window with authored (not yet executed) coverage; any unknown ID still rejects; no general downgrade.
- **Risk:** a pending event becoming uncommittable after a head change. **Decision:** succession-count gates are offer-time only; only history conditions are rechecked at commit.
- **Risk:** Iori being read as a retainer or displaced youth. **Decision:** text calls him 旧客 and the spec explicitly denies the conflation.
- **Open decision:** whether a future slice should model per-head personal relationships. This slice uses the existing family-level scalar and makes no such claim.


---

<a id="2026-11-02-sc-012-family-chronicle-design"></a>
## 来源：docs/superpowers/specs/2026-11-02-sc-012-family-chronicle-design.md

# SC-012 家族编年史设计

**状态：用户设计确认（2026-11-02）；用户于 2026-11-03 报告 EditMode 与 Play Test 通过并验收。** 范围包含任务经历，以及每次实际发生的关系数值变化，并记录变化前后的数值。测试数量、Console 计数和 Validator 结果未单独报告。

## 目标与动机

在现有 `FoundationScene` 中提供一份可读、可保存的家族经历记录，让玩家能回顾事件选择及其跨代后果、继承过程、任务进展和关系变化。

## 目标

- 只在游戏状态确实成功变化后追加编年史记录。
- 记录已提交的事件选择、成功的继承状态变化、成功的任务阶段变化，以及每次真实发生的关系数值变化。
- 关系记录显示变化前后的数值；不自行发明关系等级或阈值。
- 按确定顺序记录经历，保证同一个月内发生的多项操作也有稳定次序。
- 保存并恢复完整记录；旧版 v3 存档迁移时不伪造缺失的历史。
- 在现有 `FoundationScene` 中提供只读、可滚动的 uGUI 编年史面板。
- 使用现有 Unity 项目试玩；不新建 Unity 项目、场景、软件包或外部依赖。

## 非目标

- 记录当前尚未实现的系统，例如家产、死亡、势力关系、忍术成就、战斗和战役结算。
- 修改 `SampleScene.unity`、创建新场景或新项目、修改 `ProjectSettings`、加入软件包，或改变事件引擎已有的条件判断历史。
- 根据存档中的当前状态倒推出旧任务、关系或继承经历。
- 添加 GDD v4.0 未定义的事件重要性评分、关系等级或阈值。

## 方案选择

1. **从现有状态拼出时间线。** 重用事件历史，并读取当前任务、关系和继承快照。这无法找回过去的任务阶段、关系数值或历代家主，因此不满足用户选定的范围。
2. **新增内建的追加式编年史与 uGUI 面板（推荐）。** 在 `GameSession` 的成功提交边界记录经历，使用新的存档格式保存稳定顺序，并在一个可滚动面板中显示。这样保留现有领域边界，也能形成可试玩的记录。
3. **加入第三方历史/事件软件包。** Scout 检查了三个 GitHub 仓库，均不符合需求：Mercurius 是旧事件总线并带编辑器日志；UnityIngameDebugConsole 是开发者控制台；Archon Engine 依赖 UI Toolkit、Burst、Newtonsoft JSON 等，架构范围也更大。都不能直接提供本项目需要的持久化家族编年史，因此不加入软件包。

## 推荐行为

### 编年史记录

每条不可变记录包含稳定记录 ID、递增序号、年/月、记录类别、稳定主体/来源 ID，以及在记录时生成的中文标题和摘要。按类别保存必要数值，例如任务阶段前后值或关系数值前后值。保存生成后的文字，避免内容更新改写过去的记录。旧事件历史只有年/月没有日，因此编年史统一以月为日期精度；同月内以序号排序。

本切片记录：

- 每次事件选项仅在事件事务成功提交后记录；事件效果造成的关系变化也只能在提交成功后写入，避免把回滚事件记进历史。
- 每次成功的继承状态变化，包括退位后进入选择阶段、正式交接、摄政阶段变化、危机收养或家族灭亡；只有实际改变了阶段、家主或完成结果才记录。仅在候选名单中更换待选继承人/摄政者、但尚未改变阶段或家主，不单独生成编年史行。
- 三类现有任务（补给短缺、密信、市集账目）的每次成功阶段变化；记录任务 ID、变化前后阶段及结果文字。
- 每次通过对话、事件效果或会话直接接口成功产生的关系数值变化；记录 NPC ID 和变化前后数值。失败操作和受上下限约束而未改变数值的操作不记录。
- 新游戏初始化和读档恢复时不追加记录。

记录入口设在 `GameSession`，不放在 `CharacterRelationships` 或任务领域模型中，因为读档恢复也会调用这些模型。事件关系效果须在 `EventEngine` 的提交点成功后才暴露给编年史，回滚的效果不能留在记录中。

### 排序与显示

记录按递增序号追加，并按从早到晚显示在可滚动面板中。每行显示日期、类别、标题和摘要。面板只读，可从地图打开或关闭，不推进时间；打开时滚动到最新记录。新游戏没有记录时显示明确的空状态。使用 Unity 内建 uGUI，不新增软件包或预制体。

### 存档与兼容性

在存档根对象新增 `chronicle` 数组，将存档格式从 v3 升至 v4。保存和读档前验证日期、序号顺序、唯一 ID、记录类别及各类别专属字段。v3 迁移至 v4 时，将 v3 已保存的事件历史按原顺序复制到编年史，保留年/月精度，不虚构具体日期；v3 未保存的任务、关系与继承历史留空。现有 v1/v2 迁移继续生成当前 v4 格式。读档不得新增历史；非法或重复的编年史 ID 必须让整份读档原子失败。

## Play Test 验收结果

验收标准：玩家可以在一局新游戏中产生关系、任务、事件和继承记录；查看它们的先后次序与关系数值前后变化；保存并读档后仍看到相同记录且没有重复。失败操作和没有实际改变数值的关系操作不能新增记录。实现与静态复审已完成，逐步试玩流程在 `SC012_DEMO.md`；Unity 验证、FoundationScene 接入与用户 Play Test 仍待执行，尚无验收结果。

## GitHub 调研

只读 Scout 报告：`C:\Users\panye\AppData\Local\Temp\opencode\sc012-research.md`。

- [KornelGM/Mercurius-Event-System](https://github.com/KornelGM/Mercurius-Event-System) — MIT；最近更新记录为 2020-10；未说明 Unity 版本。编辑器日志和 ScriptableObject 事件总线无法提供玩家可读、持久化的家族经历。
- [yasirkula/UnityIngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole) — MIT；截至 2026-09 仍有更新，但用途是开发者控制台，不是游戏历史；从 README/软件包信息无法确认 Unity 6 兼容性。
- [ForgottenHistory/Archon-Engine](https://github.com/ForgottenHistory/Archon-Engine) — MIT；活跃且目标为 Unity 6000+，但依赖 UI Toolkit、Newtonsoft JSON、网络传输和 Burst，是更大型的模拟架构。

结论：使用项目现有 C# 与 uGUI 实现专用编年史，不引入上述仓库或软件包。未克隆仓库，也未复制外部代码。

## 留给实现阶段的细节

- 遵循现有严格存档校验方式，确定记录 DTO 的具体字段名与校验边界。
- 遵循 `FoundationScene` 的既有 UI 习惯，确定地图按钮位置与滚动条目布局。
- 确认面板能否在运行时生成到一个固定根下，还是需要事务式 Editor 更新器；不得为此改动无关场景内容。


---

<a id="2026-11-03-sc-013-m2-three-generation-design"></a>
## 来源：docs/superpowers/specs/2026-11-03-sc-013-m2-three-generation-design.md

# SC-013 M2 三代固定剧本设计

> 状态：用户于 2026-11-03 批准“已批准的整体方案”。代码、内容、测试与文档已由 OpenCode2 实现；`FoundationScene.unity` 尚未接入，Unity 编译/EditMode/Validator/Play Mode 与用户试玩验收均待现有 Editor 执行。批准的行为基线未改变。

## 意图

完成 GDD v4.0 M2 的第一条端到端固定试玩剧本：第一代作出有代价的选择，第二代承受其后果并再次选择，第三代面对旧事并作出结局选择；玩家在同一局中完成两次合法继承，并可在编年史中回看保存的经历。

这条切片验证跨代关系是否让玩家记住人物、理解选择后果，并愿意继续经营家族。只完成这条固定剧本，不提前扩展到 30 个事件或五条事件链。

## 依据与当前实现

- GDD v4.0：7.4 跨代事件链、12.2/12.4/12.6 三代速通、19.2 继承与无循环失败路径、19.3 稳定存档点、19.4 固定剧本、19.5 真人试玩。
- 已接受的 SC-011 提供第一代“雨夜来客”选择及第二代后果事件；SC-012 提供事件、关系、任务、继承记录和可存档的家族编年史。
- 现有 `SuccessionSample.CreateStandard()` 只有第一代及其家人，没有第二代的后代；其中可指定的继承人年龄/亲属关系也不能直接完成第二次家族直系交接。
- 继承规则已支持成年指定继承、危机收养和摄政；事件引擎已有 `EventOptionChosen`、`SettledSuccessionCountAtLeast`、关系门槛等条件。当前月推进一次只前进一个月，没有跳到重大决策点的快速推进界面。
- `DemoSummaryPanel` 当前只显示 Phase 1 三项任务全部领奖后的摘要；M2 需要第三代终局反馈。

## 方案比较

1. **固定后代路线（用户选择）**：为 M2 固定剧本定义明确的第二代与第三代人物和亲属关系，通过现有合法继承流程完成两次交接。可在两轮事件里验证血缘传承和旧事影响；新增少量固定剧本数据。
2. **危机收养路线**：以现有危机收养直接生成第三代家主。能复用规则，但本轮核心会转成收养危机，不能充分验证预设直系后代的培养和传承。
3. **分两轮试玩**：先做第二代事件，之后再做第二次交接和第三代。单轮较短，但不能作为 GDD 19.4 固定剧本的首个完整 Play Test。

选择方案 1 的原因：符合用户选择及 GDD 的两次交接标准，同时只添加固定场景数据，不实现通用生育、家谱编辑或新继承框架。

## 批准的行为基线

### 固定剧本

- 保持新局日期 `1560-04-01`，新游戏默认进入独立的 SC-013 固定剧本；使用独立固定家族身份。保留 `SuccessionSample.CreateStandard()` 及危机示范作为标题界面的独立入口，且不改写其既有规则语义。
- 初始六名家族重要人物：家主明智晴信、配偶明智千恵、第二代明智正綱、第三代明智信正、亲族明智时姫、家臣佐近。外部事件角色黑田伊织继续使用现有事件关系模型。
- 使用已批准的三代同堂年龄：晴信 70 岁、正綱 46 岁、信正 22 岁（出生年分别为 1490、1514、1538）。配偶、亲族和家臣的年龄/出生年沿用初始内容表建议值，不阻碍继承流程。
- M2 初始家主的合法指定继承人为正綱；正綱继位后的固定继承人为信正。家族面板展示六人身份、年龄与代际/关系，继承 UI 明示当前固定继承人。固定剧本不开放会跳过目标世代的旁系候选人，也不新增通用指定继承人系统。
- 仅在 SC-013 固定剧本内锁定主线顺序：必须先完成当代要求的事件选择、解决待处理事件，才能发起对应交接。标准样例/危机示范继续遵循既有继承规则。

### 三代故事与选择

1. 保留已经验收的 SC-011 事件和选项行为。SC-013 新增一个专属首代代价事件，两项选择均从场景开始即可用，清楚呈现选择的关系变化与后续含义；一次继承相关的家庭冲突决定承担真实关系损失，并记入编年史，以满足 GDD 12.6。
2. 第一代按继承 UI 交接给正綱。第二代出现现有 SC-011 后续事件，玩家可选择重申旧诺、重议条件或结束旧交。每个选项结果可见，真实关系变化写入编年史。
3. 正綱再通过相同继承 UI 交接给成年后代信正。交接保留人物/家族历史，不重置关系或已提交事件历史。
4. 第三代在第二次交接后遇到一条终局事件及结局选择。至少两种清晰不同的结局根据前代事件选项和累积关系呈现旧事回收；提交后显示阶段结局并保留对应编年史记录。结束条件由事件历史与两次交接推导，不增加终局存档字段；结局后仍可继续游玩，且不依赖未实现的家产或战斗系统。

### 存档、历史与 UI

- 在稳定决策点保存/读档：等待玩家选择的事件、继承候选决策，以及第三代结局选择前后。加载后不得重复发奖、重复写历史或改变当前选择。
- 复用现有 `SuccessionSaveData`、事件历史和 SC-012 编年史，不新增另一个项目/场景、存档字段或 Unity 包。旧存档按现有数据恢复标准/危机场景；新局使用 M2 固定剧本。
- 在现有固定 UI 根下新增家族 roster 与 M2 阶段结局显示；标题界面提供 M2 新游戏及 SC-010 危机示范独立入口。

## 范围与非目标

- 本切片：固定初始家族/三代亲属关系、两次可见继承、一条第三代后果事件、结局反馈、SC-012 编年史及稳定点存读档。
- 不实现：通用家谱/生育编辑器、30 个核心事件、五条完整事件链、经济/产业、外交、战术玩法、开放式长期模拟、第三方包或新 Unity 项目。
- 不修改 `SampleScene.unity`、`ProjectSettings` 或既有用户 WIP；所有代码由 OpenCode2 实现；Play Test 由用户执行。

## 验收标准

- 从标题界面新建固定 M2 剧本，能识别六名家族重要人物、各自身份和代际关系，并看到合法固定继承人；SC-010 危机示范仍可从独立入口进入。
- 玩家在第一代作出带关系后果的选择；第二代确实看到其后果并再次选择；第三代的结局内容因先代经历而不同。
- 同一局通过合法 UI 路径完成两次家督交接；关系、家族身份和历史不重置；正常继承不会陷入等待循环。
- 编年史正确记录第一代、第二代、第三代的选择、真实关系变化和两次继承；存档/读档后顺序、值和待处理选择一致且无重复。
- 第三代决定后出现明确阶段结局；用户完成全流程 Play Test 并给出情感/可理解性反馈后，才可视为 M2 试玩验收。
- Unity 编译、EditMode、Console 计数、FoundationScene validator、Play Mode 与用户验收分开记录。

## 用户批准记录

- 用户选择固定后代路线，并于 2026-11-03 确认“提请用户确认的推荐整体方案”。
- 已确认：固定世代顺序；显示六人家族与固定继承人；保持 SC-011 行为并增加 M2 专属代价事件；至少两个由跨代关系/选择体现的结局；一次继承选择带真实关系代价；采用晴信 70、正綱 46、信正 22 岁以缩短试玩流程。
- 已确认新游戏默认 M2 固定剧本；SC-010 标准样例/危机示范保留为独立入口。

## OpenCode2 只读构建审查

审查 Dispatch `ctx_98493f2daad9` 完成；未修改文件、未运行 Unity/测试、未操作 Git。审查补充了会让固定剧本无法完成或验收不可见的实现风险：

1. **世代顺序目前不受约束。** 玩家可以在雨夜事件前退休，或者连续交接，导致“雨夜来客”和后续结局因条件已错过而永不触发；待处理事件也可跨越交接，由新家主代替旧家主作答。
2. **游戏中没有指定继承人的操作入口。** 继承后指定值会清空；时姫与信正均可能是合法候选人，选择时姫会跳过目标第三代。当前 HUD 也未显示家族成员、代际或指定继承人。
3. **第一代代价选择并非开局可选，且净变化可能仍是正值。** “明晨再来”要求黑田伊织关系至少为 1；初始值为 0，需先完成 Atsuta 对话获得 +10。若其后再 -5，玩家最终仍为 +5。
4. **结局分支与验收矩阵未定。** 单一关系阈值可能把若干第二代选择折叠成相同结局；关系条件目前只有“至少”，没有“小于”。
5. **六名家族人物目前没有完整展示界面。** 继承面板只显示家主和交接候选人，不能展示配偶、家族成员及世代关系。
6. **GDD 12.6 的继承代价尚未落实。** 当前提案的两次自愿交接本身没有机会成本或情感冲突；SC-013 的结束状态也尚无持久化标记。

### 已批准的整体方案

为控制范围，建议把这些问题收敛为以下固定剧本规则；除标为“用户选择”的路线外，这些均是待确认提案：

- **脚本顺序锁定于 SC-013 场景内。** 必须先完成当代事件选择，才允许该代交接；交接前必须解决待处理事件。标准/危机示范模式不受影响，避免玩家无提示地错过主线。
- **固定两位直系继承人，显示而不增加通用家谱编辑器。** M2 开局预置正綱、信正两代后继；家族面板显示六名角色、身份和代际，继承 UI 明示下一位继承人。固定剧本排除会跳代的旁系候选，不添加通用指定继承人系统。
- **保留已验收的 SC-011 事件行为。** 新建 SC-013 专属的首代代价事件，避免为 M2 改写已验收选项；两项选择均可用，明确显示关系得失和后续含义。沿用现有事件历史和编年史，不升级旧事件内容。
- **结局至少呈现两个清晰可辨的结果。** 第三代在第二次交接后作最终选择，结局引用前代选择及关系记录；结束由事件历史与两次交接共同推导，不新增终局存档字段，结束后仍可继续游玩。
- **把真实关系损失放在一次继承决策中。** 建议在第一次交接前提供一次继承相关的家庭冲突选择，让玩家承担可见的关系代价，并写入编年史，以满足 GDD 12.6；不新增经济、战斗或家谱系统。
- **为快速试玩采用草案中的三代同堂年龄**（晴信 70、正綱 46、信正 22），并把新游戏默认入口设为 SC-013 固定剧本；SC-010 标准样例和危机示范保留为独立入口，旧存档继续按原样加载。

名称、月份触发时点、具体文案、关系数值、UI 落点、迁移测试及输入校验由实现计划及实现者在上述边界内确定。

## 实现与证据状态（2026-11-03）

- 已实现：`M2FixedScenario` 固定六人/三代直系与继承门；`GameSession` 新局默认 M2、独立 `InitStandardDemo`/`StartCrisisDemoScenario`；SC-013 专属承诺事件与终局事件；M2 结局派生；家族名册面板；M2 结局面板；标题界面标准/危机独立入口；两个事务式 FoundationScene 生成器与 validator 扩展；聚焦 EditMode 测试源与 `SC013_DEMO.md`。
- 内容：事件/文本仍为 `contentVersion = 2`；SC-011 的 ID、条件、效果、选项可用性与结果未改写；SC-013 事件以 `scenario: "sc013"` 隔离，SC-010 标准/危机模式不会看到它们。
- 存档：未新增终局字段；家族、亲属、待处理事件、关系、事件历史、编年史继续由现有 DTO 保存。稳定点读档由 `M2ScenarioSaveTests` 覆盖。
- 静态验证：八个程序集用 Unity 随附 Roslyn 编译，0 错误；M2 固定剧本/名册 harness 30 项通过；完整事件链 harness 33 项通过。**这些不是 Unity Editor 编译或 Unity Test Runner 结果。**
- 未完成：`FoundationScene.unity` 未运行 updater、未保存、未运行 `FoundationSceneValidator`；Unity 编译、EditMode 执行、Console 计数、Play Mode 与用户验收均未取得。生成与验证步骤见 `SC013_DEMO.md`。


---

<a id="2026-11-03-sc-014-refugee-legacy-design"></a>
## 来源：docs/superpowers/specs/2026-11-03-sc-014-refugee-legacy-design.md

# SC-014 流亡少年与家系遗产：设计稿

日期：2026-11-03  
状态：数据/文档切片已由独立 OpenCode2 静态复审 ACCEPT；Unity 场景验证、Play Test 与用户验收待后续执行。  
设计依据：[GDD_v4.0.md](../../GDD_v4.0.md) §7.4、§12.2、§12.3、§19.6。

## 目标

在 SC-013 固定 M2 剧本中增加第二条跨代事件链，让第一代对流亡少年的选择，在第二代成为家臣忠诚与新政策冲突，并在第三代分化为家系援助或竞争。关系数值和每次实际变化继续由现有事件历史/家族编年史记录。

本轮不安排 Play Test。SC-013 尚待用户现有 Unity Editor 完成场景接入与验证；本任务不触碰该场景。

## 采用的实现选择

- 将 GDD 示例中的少年设为伊织家系的一名流亡少年；第一代当前遇见并决定是否收留/拒绝/出卖他，成年伊织作为现有家臣替少年求情。少年在一次交接后的第二代便已是伊织家系的家臣，第二代家主随后处理伊织家系对先代旧约与新政策的忠诚冲突，第三代面对该少年后代的援助或竞争。当前事件不能改写成回忆/补写过去选择。继续使用现有稳定人物 ID `npc.kuroda.iori` 与家族关系标量，不把 SC-011 的成年伊织改写成少年，也不新增第二套人物/关系模型。
- 新链 ID 为 `chain.sc013.refugee-legacy`。事件行沿用 M2 已认可的 `scenario: "sc013"`，并使用 `event.sc013.refugee-*` ID，以保留 M2 场景门控和专属事件识别。
- 第一代入口提供收留、拒绝、出卖三种有明确叙事差异的选择。收留将关系 +5 并开启后代回收；拒绝与出卖分别记录其后果并终止这条链。
- 第二代节点只在收留分支、至少完成一次交接后出现。玩家可尊重旧臣忠诚（关系 +5）或以新政策压下旧约（关系 -5）。
- 第三代有两个互斥节点，分别由第二代的两个选项触发，且必须在第二次交接后出现。盟友节点允许基于关系值选择请求援助；竞争者节点呈现政策冲突的长期代价。两个节点都提供明确结局选项。
- 关系效果都指向现有 Iori 家族关系记录；内容文字明确该分值是家族与伊织及其家系的关系，不宣称为少年建立未建模的独立人物关系。

## 内容卡

| 世代 | 节点 | 前置 | 选择与结果 | 失效处理 |
|---|---|---|---|---|
| 第一代 | 流亡少年 | SC-013 M2，首次交接前 | 收留：+5 并开启链；拒绝：-5；出卖：-10。拒绝/出卖是明确终止结果。 | M2 首次交接后入口关闭；不会迟到地启动链。 |
| 第二代 | 旧臣的忠诚 | 曾选择收留；至少一次已完成交接 | 尊重旧臣旧约：+5 并通往盟友节点；服从新政策：-5 并通往竞争者节点。 | 若路径条件失效，不重复触发或改写旧选择。 |
| 第三代 | 流亡家系的援助 | 第二代选择尊重旧约；至少两次已完成交接 | 关系 ≥5 时可请求援助；也可谢绝并独自承担。 | 信任不足时援助选项隐藏/禁用，并显示已有关系理由；独立结局始终可选。 |
| 第三代 | 流亡家系的竞争 | 第二代选择服从新政策；至少两次已完成交接 | 选择修补旧怨（+5）或维持距离（-5），两者均记录结果。 | 与盟友节点互斥，不重复结算。 |

## 范围

- 在事件目录与中文文本中追加 SC-014 节点及链声明；保持 `contentVersion` 不变。
- 更新事件/关系进展文档，并创建 SC-014 设计稿与实现计划。
- 由 OpenCode2 完成代码/数据改动；之后由另一位 OpenCode2 worker 只读审阅。

## 不在本轮范围

- Unity 场景、预制体、`ProjectSettings`、存档 schema、包、通用事件规则或关系模型改动。
- 新建 Unity 项目、修改既有 SC-011/SC-013 事件文本与行为、提交/合并/推送、Play Test。
- 自动声称 Unity 编译、EditMode、Console、场景验证或运行时已通过。

## 完成证据

- SC-014 内容与链关系按上表实现，SC-011/SC-013 原有事件行与文本保持原样。
- 变更有 OpenCode2 独立只读审阅记录；任何未验证的运行时行为明确标为未验证。
- Play Test 由用户之后决定和执行。


---

<a id="2026-11-03-sc-015-three-generation-family-letters-design"></a>
## 来源：docs/superpowers/specs/2026-11-03-sc-015-three-generation-family-letters-design.md

# SC-015 三代家书：设计稿

日期：2026-11-03  
状态：获项目所有者“继续开发直到必须要 Play Test”为实现授权；设计依据 GDD v4.0 §7.4、§12.2、§12.3、§19.4、§19.6。

## 目标

在 SC-013 固定 M2 剧本中增加一条围绕千恵家书的三代家庭事件链。晴信决定怎样保存家书，正綱面对这份家史作出处理，信正最终继承一份由前两代共同塑造的家族记录。用现有事件选择历史表达分支，不新增运行时状态、关系模型或人物。

本轮继续做内容开发，不启动 Unity，不进行 Play Test。场景集成、验证和用户试玩留待事件内容就绪后集中办理。

## 设计

- 固定剧本人物仅使用已有晴信、千恵、正綱、信正及其家族关系。家书是千恵记录家族经历的原创建议，不引入新 NPC 或历史事实。
- 新链 ID：`chain.sc013.family-letters`；节点与选项使用 `event.sc013.family-letters.*`、`option.sc013.family-letters.*` 前缀，所有事件均设 `scenario: "sc013"`。
- 第一代 `event.sc013.family-letters.first`：交接次数为 0 时，晴信与千恵决定完整公开家书或暂封家书。
- 第二代分为两个互斥节点，均要求至少完成一次交接：公开路径进入 `...second-open`，暂封路径进入 `...second-sealed`。每个节点提供两个选择，分别形成不同的第三代回收方向。
- 第三代分为四个互斥结尾节点，均要求至少完成两次交接；每个节点只由对应的第二代选项触发。四种结尾分别呈现完整记名、简要传述、由第二代提前拆阅、继续封存的家史结果。
- 选项不修改关系、任务或家族数值。选择、后续节点和结尾都由现有事件历史记录；结尾文本明确指出家书如何传到信正手中。编年史只记录已有提交事件，不伪造数值变化。
- 使用现有条件 `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`；`effects` 为空。内容版本保持 `2`。
- 每个世代的链节点按优先级排在同世代 SC-014 可选流亡事件之前，但低于 SC-013 必须事件；既有 SC-011、SC-013、SC-014 定义不改写。完整体验本链需要每代结束额外月份以显示对应可选事件。

## 内容卡

| 世代 | 节点 | 前置 | 玩家决定 | 回收与失效 |
|---|---|---|---|---|
| 第一代 | 千恵的家书 | `SuccessionCountEquals 0` | 完整公开，或封存留给后代 | 若首次交接前未触发，入口不再出现；不会迟到启动 |
| 第二代 | 已公开的家书 | 第一代选择公开；`SettledSuccessionCountAtLeast 1` | 保留原始人名与细节，或整理为简要家史 | 仅对应公开分支出现一次 |
| 第二代 | 尚未拆封的家书 | 第一代选择封存；`SettledSuccessionCountAtLeast 1` | 由正綱拆阅并留下完整记录，或继续封存交给后代 | 仅对应封存分支出现一次 |
| 第三代 | 完整家书结尾 | 公开后保留原貌，或第二代拆阅 | 无额外状态效果；展示已形成的家史 | 仅对应来源选择触发 |
| 第三代 | 简史/封存结尾 | 公开后简化，或第二代继续封存 | 无额外状态效果；展示简史或封存的不同结果 | 仅对应来源选择触发 |

## 范围与边界

- 只追加事件目录和中文文本，并更新项目状态、架构/决策记录、SC-015 文本试玩手册。
- 不修改代码逻辑、测试、场景、预制体、`ProjectSettings`、存档 schema、包、SC-011/SC-013/SC-014 既有事件及其文本。
- 不新增或运行测试，不启动 Unity，不截图，不操作 Unity GUI，不提交、推送、合并或打标签。
- OpenCode2 实现；另一名 OpenCode2 worker 完成只读审阅。审阅只覆盖 SC-015 变更并确认既有事件与文本未被改写。

## 完成证据

- 7 个新增节点、对应选项和文本构成唯一可达的三代分支；源事件/选项、世代门槛和互斥结果均能由已有规则表达。
- SC-011、SC-013、SC-014 的既有内容不变；JSON 可解析、ID 唯一、文本引用完整。
- 记录静态检查、OpenCode2 独立复审及其限制。不得声称 Unity 编译、EditMode、场景验证、Console、Play Mode、存读档或用户 Play Test 已通过。


---

<a id="2026-11-03-sc-016-m2-commission-legacy-design"></a>
## 来源：docs/superpowers/specs/2026-11-03-sc-016-m2-commission-legacy-design.md

# SC-016 M2 委托旧约：设计稿

日期：2026-11-03  
状态：按用户选择的“完成第 5 条 M2 跨代链”继续设计；范围采用此前确认的“任务与关系经历”。设计决策为助手按 GDD 与现有工作区作出的可修订判断。

## 目标

在 SC-013 固定三代剧本中加入第五条跨代事件链：第一代处理一项商路委托，第二代继承其信任或失信后果，第三代决定这段委托关系最终如何留在家族历史中。每次实际关系变化都写入现有家族编年史，复用既有事件历史表达分支。

当前事件目录有 16 个事件和 4 条链；新增本链 7 个事件后为 23/30 个事件、5/5 条跨代链。后续 7 个核心事件另行安排，不扩大本次任务。

## 实现方案

1. **仅用现有 JSON 事件引擎（本次采用）**：通过现有 `EventOptionChosen`、世代门槛和 `RelationshipDelta` 表达委托选择及其后果。优点是延续已运行的 M2 纵向切片，不新增状态、代码或存档字段；代价是委托结果由叙事事件和关系变化表示，不新增通用任务面板或任务状态机。
2. **扩展任务运行时**：新增任务阶段、结果条件和存档迁移，使每个故事节点与实际任务状态联动。它更接近完整任务模拟，但扩大了 SC-016 的代码、存档和验证范围，不符合当前“完成第 5 条跨代链”的小切片。
3. **接入外部叙事框架**：Ink 提供通用分支叙事，Ink Unity Integration 支持 Unity 2022.3+；Ogham 提供 Unity 6000 图叙事，但 GitHub 仓库已迁往 Codeberg 且需要额外依赖。它们都不能直接替代现有事件历史、关系结算和编年史；本项目已有满足本链需求的系统，故本次不安装或复制外部代码。

现有 GitHub 资料：

- [inkle/ink](https://github.com/inkle/ink)：MIT；其 README 将 Ink 定位为可分支叙事语言，并把 Unity 接入指向单独的 Unity Integration。
- [inkle/ink-unity-integration](https://github.com/inkle/ink-unity-integration)：MIT；README 声明支持 Unity 2022.3 LTS 及以上，最近可见提交为 2026-07-08。它适合可编写脚本的长篇对话，不负责本项目已有的跨代事件效果、继承门槛或关系编年史。
- [Heathen Ogham Storyteller Foundation](https://github.com/heathen-engineering/Unity-Ogham-Storyteller-Foundation)：Apache-2.0，README 标示 Unity 6000；GitHub 副本已停止更新并迁往 Codeberg，且要求 GameplayTags 与 Lexicon 两项依赖。本次不采用。

## 内容卡

固定使用既有 NPC `npc.sakai.jirobei`（酒井次郎兵卫），不新增人物。故事为原创的账册递送委托；不引用旧《Sengoku Chronicle》任务文档，也不改写既有任务模块。

| 世代 | 节点 | 前置 | 玩家决定与关系变化 | 后续 |
|---|---|---|---|---|
| 第一代 | 账册托付 | SC-013；首次交接前 | 接受夜路递送：酒井关系 +5；拒绝委托：-5 | 分别开放第二代守约或失约节点 |
| 第二代（守约） | 接过的夜路 | 第一代接受；第一次交接已结算 | 继续守密：+5；公开路线真相：-5 | 分别开放第三代守约或公开结尾 |
| 第二代（失约） | 未完的交代 | 第一代拒绝；第一次交接已结算 | 主动补偿：+5；拒绝再牵连：-5 | 分别开放第三代补偿或决裂结尾 |
| 第三代（守约） | 夜路仍然安全 | 第二代继续守密；第二次交接已结算 | 单一结尾确认，无额外数值变化 | 记录委托关系得以延续 |
| 第三代（公开） | 账册见了天日 | 第二代公开真相；第二次交接已结算 | 单一结尾确认，无额外数值变化 | 记录家族以公开真相了结委托 |
| 第三代（补偿） | 旧账已清 | 第二代主动补偿；第二次交接已结算 | 单一结尾确认，无额外数值变化 | 记录家族修复早年的拒绝 |
| 第三代（决裂） | 商路不再相通 | 第二代拒绝再牵连；第二次交接已结算 | 单一结尾确认，无额外数值变化 | 记录酒井家系不再信任明智家 |

所有变化使用现有 `RelationshipDelta`，目标为酒井次郎兵卫；实际前后值由编年史记录。第三代单一选项只记录结尾，不伪造数值变化。若第一代完成交接前未触发入口，本链不迟到启动，也不阻塞 M2 交接。

## 调度与约束

- 链 ID：`chain.sc013.commission-legacy`；事件和选项 ID 使用 `event.sc013.commission-legacy.*` 与 `option.sc013.commission-legacy.*`。
- 所有事件沿用 `scenario: "sc013"`。世代门槛沿用现有继承次数条件；分支沿用选项历史条件。
- 推荐优先级为第一代 `8`、第二代 `80`、第三代 `800`：仍低于 SC-013 必须节点 `1000`；保持 SC-011 第一代 `10` 与第二代 `100` 先出现；新链在同世代 SC-015 `6/60/600` 和 SC-014 `5/50/500` 之前出现。
- 不新增代码、条件/效果类型、NPC、场景、Prefab、`ProjectSettings`、package、资源文件或存档字段；保留 `.meta`/GUID，不碰 `Library` 或 `Temp`。
- 现有 SC-011、SC-013、SC-014、SC-015 的事件、选项和文本保持不变。

## 验收

- JSON 结构、ID、文本引用、链成员、来源选项、世代门槛、互斥路由、关系目标和优先级检查无错误。
- 现有 Unity Editor 的 Play Mode 能分别走完四条第三代结果；每条路线的关系变化和编年史前后值一致、无重复记录。
- 另核对标题、新局、固定六人名册、SC-013/SC-011 必须节点仍按顺序出现，Console 计数单独记录。
- Unity 编译、EditMode、`FoundationSceneValidator`、存读档与用户验收分别记录；不能用 JSON 检查或 Play Mode 代替其他门槛。


---

<a id="2026-11-03-sc-017-family-tree-roster-design"></a>
## 来源：docs/superpowers/specs/2026-11-03-sc-017-family-tree-roster-design.md

# SC-017 通用家谱展示设计稿

日期：2026-11-03  
状态：待用户确认  
参考方向：用户提供的家族名册截图与 Crusader Kings III 家族树截图（仅参考信息层级和亲缘连线，不复用其素材）

## 意图

将 SC-013 当前逐行列出的家族名册改为可复用于不同家族和调试剧本的通用家谱展示。画面从实际家族成员与亲属关系数据生成，不写死六名角色或三代顺序；本轮只读展示，未来 debug mode 再提供数据编辑能力。

## 当前状态

`FamilyRosterPanel` 从 `GameSession.FamilyRoster` 读取六条固定名册记录并逐行生成文本。SC-013 当前亲属图包含配偶、亲子、手足关系；佐近是家臣且没有亲属连线。领域层已有 `SuccessionWorld`、成员集合和 `KinshipGraph`，存档也保存成员与亲属边。当前 `FamilyRosterPanel`/`M2RosterRow` 面向名册展示，`KinshipGraph` 是追加式结构，本项目尚无 debug mode。

## 目标

- 在现有名册面板中按实际成员与亲属数据动态构造节点和关系连线；不根据角色 ID、名字、固定成员数量或预设代数决定结构。
- 不设置 SC-013 专属的六人或三代上限；成员较多或世代较深时，家谱画布可以滚动浏览。
- 支持当前领域模型已有的配偶、亲子、手足、收养关系，并清楚区分关系类型与方向。
- 同一人物存在多条关系、旁系分支或多个有效亲属连接时，所有有效成员和关系均可见；没有亲属连接的家族成员仍显示在独立的未连接成员区域，不丢失成员。
- 非家族成员（例如家臣佐近）不伪造亲缘连线，放在家臣/附属成员区域。
- 人物卡显示可用的姓名、年龄、家族身份；当前家主与有效固定继承人状态明确显示。缺少可选资料时仍能显示该人物。
- 复用真实家族状态和关系图；开关或刷新面板不改变游戏数据。未来 debug mode 可复用同一展示结构并另行接入编辑操作。
- 保留现有标题、关闭操作、场景与游戏入口。

## 非目标

- 本轮不提供增删成员、添加/删除/编辑亲属关系的 UI，也不改变 `KinshipGraph` 的正式游戏规则或其结构性事实约束。
- 不实现 debug mode、家谱存档编辑器、无限性能承诺、人物肖像、纹章、美术资产或新 Unity package。
- 不修改 SC-010/SC-011 行为、M2 剧本数据、存档格式或其他场景。
- 不新增项目或场景；沿用 `FoundationScene` 与现有 UI 框架。

## 方案比较

1. **数据驱动的通用关系画布（推荐）**：从家族成员集合和关系图计算节点、分支和连线；画布支持滚动，佐近等非家族成员显示在独立区域。新增人物或关系数据即可扩展展示，不绑定 SC-013 固定名单。
2. **为 SC-013 单独画固定三代布局**：初期更容易精确摆位，但无法复用于其他成员数量或后续 debug mode。
3. **保留列表并增加关系文字/缩进**：最省改动，但仍无法清晰呈现多条关系与复杂分支。

推荐方案 1：它满足现有 SC-013 画面需求，也让未来调试场景能复用渲染层；编辑能力仍由未来独立的 debug mode 决定。

## 约束与实现边界

- 使用现有 Unity uGUI 和项目字体/色彩；参考 CK3 的世代层级、卡片和连线，不复制其美术内容。
- 结构必须从 `SuccessionWorld` 成员和 `KinshipGraph` 关系数据派生；不得把 `M2FixedScenario` 的静态展示行作为通用渲染数据源。
- 仅对有效成员关系绘制连线；布局需处理分支、多边和无关系成员，不得把家臣渲染成亲属。
- 尽量在现有固定面板区域内实现；内容超出可视区域时使用面板内滚动，不遮挡主界面的关闭/返回操作。
- 若场景集成需要变更，使用现有事务式 Editor updater 和 validator；不得新建场景。保留 `.meta`/GUID，不触碰 `Library`/`Temp`。
- 所有代码由 OpenCode2 实现；Unity 编译、EditMode、Console、场景验证分别记录。用户负责 Play Test；用户试玩并验收前不提交、不合并、不打 tag。

## 验收条件

- SC-013 初始家谱准确显示晴信、千惠、正綱、信正、時姫的亲属关系，佐近显示在独立家臣区域。
- 使用测试构造的成员数多于六人、世代多于三代、含旁系和多种关系的家庭状态时，所有成员和关系均能显示并可滚动查看；无需改展示代码中的 SC-013 名称/ID或数量常量。
- 未连接家族成员仍可见；非家族成员不会被误画成有亲缘关系。
- 家主与有效固定继承人状态与当前会话一致；合法继承并刷新后标记更新。
- 打开、关闭、滚动、刷新名册不改变家族/亲属/关系值/存档/事件状态；SC-010 标准与危机场景既有行为不回归。
- Unity 编译、EditMode、Console、场景 validator 与用户 Play Test 分项记录。用户 Play Test 未完成前不宣称体验验收。


---

<a id="2026-11-03-sc-018-independent-family-events-design"></a>
## 来源：docs/superpowers/specs/2026-11-03-sc-018-independent-family-events-design.md

# SC-018：七个独立家族事件设计稿

日期：2026-11-03  
状态：七个独立事件与家庭关系语义均已确认，等待完成实现（2026-11-03）  
范围来源：用户选择“做 7 个独立事件”；结合此前问题语境，本稿按独立家族事件设计。

## 意图

将当前 M2 事件目录从 23 个事件补到 30 个，同时维持 5/5 条跨代事件链。新增内容是可单独触发、单次解决的家族生活事件，不引入第六条跨代链，也不改动 SC-011 至 SC-017 的既有事件与表现行为。

## 当前基线

- `Assets/Resources/Events/event-catalog-v1.json`：`contentVersion` 为 2，23 个事件，5 条链。
- `Assets/Resources/Localization/event-text-zh-v1.json`：现有事件本地化文本。
- 当前事件效果只覆盖玩家与 NPC 的关系值变化；SC-018 增加独立的当前家主→家人关系效果，不扩展金钱、物资、任务或人物成长效果。
- GDD v4.0 §12.3 将 30 个核心事件按六类列出建议数量；新增七个家庭事件会使“家族成员与亲情”这一类高于建议数。该表是建议分布，本稿优先落实用户选择的独立家族事件；开发实现前应逐事件核实核心事件计数，避免将建议分布误报为已满足。

## 家庭数值关系语义

用户选择为关系值增加“当前家主与每位家人”这一方向，按人物对持久化。关系键由当前家主 `PersonId` 和被影响家人的 `PersonId` 共同构成；不记录家主与自己的关系值。继承后，新家主使用以自己人物 ID 为关系主体的记录；不把上一任家主的数值复制给新家主。上一任家主的关系记录仍保留并可在历史中查阅。

当前 `EventCatalogValidator.ValidateEffect` 通过 `TryNpc` 限定 `RelationshipDelta` 目标；`CharacterRelationships` 只初始化已登记 NPC，并拒绝未知角色 ID。SC-013 家族成员属于独立的 `PersonId` 名册，因此实现需要增加家庭关系状态和独立事件效果、存档兼容及编年史姓名/前后值记录，而非只追加事件 JSON。

SC-018 的事件效果只改变当时当前家主与一个或多个在册家人的关系值。关系目标必须属于该家庭，且不能等于关系主体；事件中断、目标缺失或事务失败时不得留下半次关系变更。数值沿用现有关系范围 `[-100, 100]` 并按真实前后值入编年史。

存档须持久化完整人物对；旧版本存档迁移后家庭关系默认为空/零值，不推算历史好感。读档须保留关系键中的历史家主，保证继承后当前家主切换不会覆盖前代关系。

## 提议的七个事件

以下是内容提案，尚未获用户逐项确认。每个事件提供两个可选答复；选择体现家主对家人处境的取舍，并用已有关系变化表达即时情感后果。

| ID 提案 | 事件主题 | 选择分歧 | 建议的即时后果 |
|---|---|---|---|
| `event.sc018.sibling-duty` | 手足请求分担家务与巡守 | 让其暂缓个人安排，或替其另找帮手 | 该手足对家主的关系值上升或下降 |
| `event.sc018.child-study` | 子女请求延后修习，继续读书 | 支持其选择，或要求按家训完成训练 | 该子女对家主的关系值上升或下降 |
| `event.sc018.parent-care` | 长辈需要照料，家中对人手安排有分歧 | 由家主亲自陪护，或交由其他亲属轮值 | 相关亲属对家主的关系值变化 |
| `event.sc018.heirloom` | 两名亲属都认为旧物应由自己保管 | 按长幼分配，或交由最常使用者保管 | 两名亲属的关系值呈相反变化 |
| `event.sc018.spouse-visit` | 配偶希望探望其原生家族 | 亲自陪同，或说明当前责任并改日安排 | 配偶对家主的关系值上升或下降 |
| `event.sc018.adoption-welcome` | 被收养成员询问自己在家中的位置 | 明确承诺平等相待，或强调仍需履行家规 | 被收养成员对家主的关系值上升或下降 |
| `event.sc018.family-assembly` | 家族成员希望召开例会讨论未来分工 | 开放讨论，或由家主直接定案 | 参与者或主要继承候选人的关系值变化 |

人物指向和变化数值依照现有 M2 家族 `PersonId`、关系方向与上限规则核实；不新增人物，不让非家族成员、缺席或无效目标成为关系目标。若某个主题不能由当前家族成员合理承载，调整该事件内容以使用有效亲属，不伪造亲缘关系。

## 触发与范围

- 七个事件均使用 `Monthly` 触发和 `PerGame` 单次范围，避免月月重复出现。
- 使用现有条件类型；若需要限定角色年龄或世代，而目录没有相应条件，则不为本轮新增条件类型。
- 不填写 `chainId`，不被其他事件的 `EventOptionChosen` 条件引用，也不由本事件建立后续事件。
- 事件优先级避开 SC-013 固定剧本的必经事件；必要时使用现有权重/优先级机制控制其不会遮挡必须完成的剧情节点。
- 保持事件目录 `contentVersion: 2`；保存格式升级为 v5，兼容迁移旧版存档且不推算家庭关系值。不开新 Unity package、场景或项目。
- 所有文本使用原创中文；不引用商业游戏角色、剧情或文案。

## 方案比较

1. **七个独立、数据驱动的家族事件（推荐）**：满足用户选择，保持 5 条跨代链和现有事件引擎边界，改动集中在事件目录、本地化文本及对应验证/文档。
2. **给事件增加金钱、物资或人物成长效果**：能表达更广的模拟后果，但现有事件效果模型不支持，需另做规则、保存、验证和 UI；不纳入本轮。
3. **扩写既有事件链**：能够强化跨代因果，但不符合本轮选择的独立事件目标。

## 不在本轮范围

- 不新增跨代链或事件依赖。
- 不修改 NPC 关系算法；新增家庭关系类型和效果只服务于当前家主与家人，不支持任意家人彼此的完整关系网络。除存档和编年史关系变化显示外，不改场景、标题入口或家谱 UI。
- 不补齐 M2 试玩者人数，也不代替 GDD §19.5 的项目级 Go/No-Go 决策。
- 不把达到 30 个事件自动视为 M2 完成或 M3 获准开始。

## 验收目标

- 目录总数为 30 个事件，链数仍为 5 条；新增七项均无 `chainId`，各自独立且单次触发。
- 所有事件和选项本地化 ID 唯一、完整；事件引用与角色引用通过现有验证器。
- 七个事件都有清楚的家庭处境、两种可理解的选择以及当前家主对在册家人的可观察关系前后值变化；目标不能是家主本人或非家庭成员。不可选项说明明确，结局不阻断固定剧本。
- SC-011–SC-017 数据和行为保持不变；SC-013 标准/危机路线仍可按既有路径完成。
- Unity 编译、EditMode、Console、场景 Validator 和用户 Play Test 分项记录；用户负责 Play Test。用户明确验收前不提交、合并或打 tag。

## 用户确认记录

用户于 2026-11-03 确认七个独立事件、家庭数值关系/存档/编年史扩展，并确认关系键为“当前家主人物 ID → 家人人物 ID”；继承后新家主使用自己的人物对记录。


---

<a id="2026-11-03-sc-019-family-industries-design"></a>
## 来源：docs/superpowers/specs/2026-11-03-sc-019-family-industries-design.md

# SC-019 家族产业首切片设计：农庄与商馆

日期：2026-11-03  
状态：设计定稿，进入实现计划；用户已把研究、设计与计划决策委托给 lead，本稿不再设置额外的设计审批门。  
范围来源：用户选择“产业、人员任命、每月自动工作”，并授权 lead 决定首切片内容；lead 选定“初始农庄 + 商馆”。  
设计权威：`GDD_v4.0.md`。本稿是 M3 第一个纵向切片，不是完整 M3，也不替代 GDD 的验收标准。  
交付边界：本任务只写本设计与对应实现计划两份文档；不修改代码、测试、场景、prefab、`ProjectSettings`、`Library`/`Temp` 或存档。

## 1. 意图

M3 的完整范围包括产业、人员任命、自动工作、人物成长、婚姻与收养、教育、隐居、摄政、家训与抱负。一次实现全部内容既不可验证，也不适合下一次 Play Test。

本切片只验证一个可完整游玩的闭环：

1. 玩家在现有家族成员中任命产业负责人。
2. 玩家结束月份，产业按规则自动结算维护费与产出。
3. 三个资源（资金、物资、情报）发生可观察的前后变化。
4. 玩家在面板中看到产业状态、阻塞原因和月结前后值；编年史留下月结记录。
5. 空缺、负责人失效、资源不足、继承交接都能被规则处理，不会静默跳月或重复结算。
6. 存档/读档恢复资源、任命和“已结算月份”，不会重复生产。

切片使用现有 SC-013 六人固定家族和既有 `PersonId`，不新增人物、不新增 Unity 项目或 package。

## 2. 当前基线与约束

- 工程：`C:\Users\panye\My project`，Unity `6000.6.6f1`，当前分支 `feature/sc018-independent-core-events`。
- 工作区含未提交的 SC-018 数据、运行时、测试和文档；SC-018 用户验收仍待完成。本设计不修改、覆盖或清理这些内容。
- 当前存档 schema 为 v5：包含 `familyRelationships`（SC-018 人物对关系）、`chronicle`、`succession`、事件运行时、随机流、任务阶段、日期与地点。
- 当前时间入口：`GameSession.TryAdvanceToNextMonth()`、`AdvanceOneDay()`、`AdvanceDays(int)`、`PreviewTravel()`、`ConfirmTravel()`；`WorldClock` 只负责纯日期推进。
- 当前月度顺序：`TryAdvanceToNextMonth()` 先检查待处理事件/继承暂停/内容可用性，再为下月排定事件，最后推进到下月 1 日。它目前没有产业结算。
- 当前 `AdvanceDays()` 可以跨月且不触发任何月度结算；`ConfirmTravel()` 也可以跨月。这是本切片必须收紧的缺口。
- 当前 UI 使用 Canvas 下的固定根节点（`M2WorldMapRoot`、`EventChoiceRoot`、`M6TitleRoot`、`FamilyChronicleRoot`、`FamilyRosterRoot` 等），面板由事务式 Editor 生成器创建，`FoundationSceneValidator` 检查绑定。
- 当前三项既有任务（补给短缺、密信、市集账目）不占用人物时间，也不与产业任命冲突。本切片**不假装**它们已经预留人员；这是明确保留的后续规则缺口（见 §14）。

## 3. 研究结论与来源

本轮只做有界研究，不下载仓库、不安装依赖、不复制外部代码。候选与结论如下。

| 候选 | 许可证 | 兼容性/维护 | 与切片的关系 | 决定 |
|---|---|---|---|---|
| `omikun/EconSim`（https://github.com/omikun/EconSim） | MIT | C#/Unity；仓库约 49 MB；最近推送 2025-04-17；README 未声明 Unity 6 验证 | 基于 agent 的价格信念、投入产出依赖、拍卖行/竞价与破产重生；偏重、偏市场模拟，和“两个固定产业 + 月度自动结算”的切片目标不一致 | 不复用 |
| `KrisAirdancer/ecosim`（https://github.com/KrisAirdancer/ecosim） | GitHub API 未返回许可证（`license: null`） | 单一提交（2024-06-26，`Transferred files from GitLab.`）；需要 `EcoSimBridge` 与 ScriptableObject 对接；维护与许可证风险高 | 外部桥接层和商品/价格模型超出本切片；许可证不明，不能作为商业项目依赖 | 不复用 |
| Unity `ScriptableObject` 文档（https://docs.unity3d.com/Manual/class-ScriptableObject.html） | Unity 文档 | Unity 6.6 文档明确：ScriptableObject 是项目资产；在独立 Player 运行时只能读取资产中已保存的数据 | 适合做“产业定义”的只读资产，不适合保存每次游玩的资源、任命和已结算月份；运行时可变的存档状态应放在版本化存档 DTO 中 | 只用其“定义与运行时分离”的结论，不引入资产型运行时状态 |

补充事实来源：

- `https://api.github.com/repos/omikun/EconSim`：`license.spdx_id = MIT`，`pushed_at = 2025-04-17T04:30:14Z`，`size = 49167`。
- `https://api.github.com/repos/KrisAirdancer/ecosim`：`license = null`，`pushed_at = 2024-06-26T13:01:38Z`，`size = 5235`。
- `https://api.github.com/repos/KrisAirdancer/ecosim/commits?per_page=5`：仅一条提交，消息为 `Transferred files from GitLab.`。
- `https://raw.githubusercontent.com/omikun/EconSim/master/README.md`：自述为 agent-based economy simulator，包含 AuctionHouse、商品依赖、破产等。
- `https://raw.githubusercontent.com/KrisAirdancer/ecosim/main/README.md`：要求实现 `EcoSimBridge`，通过 ScriptableObject 与模拟器对接。

**决定：自建聚焦的纯 C# 领域模型 + 版本化存档 DTO + 现有 uGUI 面板。** 不引入外部代码、package、订阅、付费服务或资产。理由：切片规则很小、需要确定性、需要与现有继承/事件/编年史/存档共用同一事务边界；外部经济引擎的拍卖、价格发现和桥接层会显著增加集成面，却不能直接满足“两个固定产业、人员时间、月度结算”的验收。

## 4. 首切片设计决策

### 4.1 两个初始产业

只实现 GDD §5.2 中的两种产业，且都是 1 级、1 个职位：

| 产业 | 稳定 ID | 定位 | 职位容量 | 每职位工时 |
|---|---|---|---:|---:|
| 农庄 | `industry.farm` | 稳定粮食与少量现金；维护从自身收成中保留，作为不依赖外部库存的保底产业 | 1 | 3 |
| 商馆 | `industry.trading-house` | 资金与情报，但消耗资金和物资，存在停产风险 | 1 | 3 |

不做建设、升级、扩建、分店、动态价格、市场竞价、贸易路线或行业竞争。

### 4.2 三个资源

沿用 GDD §5.1：

- 资金（funds）：建设、工资、雇佣、外交；本切片用于商馆维护。
- 物资（supplies）：设施维护、忍具、任务准备；本切片用于商馆维护，农庄产出物资并从收成中保留自身维护。
- 情报（intelligence）：任务准备、政治行动、情报交易；本切片由商馆产出，**尚无消耗入口**（见 §14）。

资源为非负整数，范围 `0..999999`。不使用负数、欠款、利息或通胀。

### 4.3 人物时间预算

- 每名**存活、未退休、年满 16 岁、仍属当前家族**的成员每月有 **4 个时间单位**。
- 一个产业职位**预留** **3 个时间单位**，剩 **1 个时间单位**可用。
- 因为 `3 + 3 > 4`，同一人物不能同时担任两个产业职位；这由时间预算自然推出，不需要额外的“每人只能一个职位”规则。
- 每个产业只有 1 个职位容量；一个职位只能由 1 人担任。
- 任命是**当月方案的时间预留**，不是立即消耗，也不建立“每人已花时间”持久计数器。替换或解除会释放预留；若该人物仍可工作，他在当月方案中恢复为 4 单位可用。月结时只按锁定方案占用 3 单位，不再重复扣减。
- 继承**不发放额外预算，也不转移预算**。每个成人始终只有自己的 4 单位；新家主若原本未任职则可用 4，若原本已任职则可用 1 且不能接第二职位。旧家主退休后可用 0；其旧职位在交接时解除预留，但不会把时间转给新家主。
- **当月任命规则**：在月结前的管理稳定点，允许用人物自己的当月预算任命/替换/解除；这属于被结算月份的方案。结束月份后日期已到下月 1 日，之后的任命属于下月方案。两种情况下都不产生额外预算，也不会把旧家主的时间转给新家主。
- 本切片没有其他消耗时间的行动，因此剩余的 1 单位只显示、不可花；这是明确的范围限制。
- 退休、死亡、离族、未成年的人物时间为 0，不能任命。

### 4.4 初始状态（新局，1560-04-01）

| 项目 | 初始值 |
|---|---:|
| 资金 | 30 |
| 物资 | 12 |
| 情报 | 0 |
| 农庄 | 1 级，空缺 |
| 商馆 | 1 级，空缺 |
| 已结算月份 | 1560-03（即当前月的前一个月） |
| 家族 | 现有 SC-013 六人（见下表） |

| 人物 | 稳定 `PersonId` | 1560 年年龄 | 家族角色 |
|---|---|---:|---|
| 明智 晴信 | `person.sc013.harunobu` | 70 | 家主 |
| 明智 千恵 | `person.sc013.chie` | 66 | 配偶 |
| 明智 正綱 | `person.sc013.masatsuna` | 46 | 直系亲族 |
| 明智 信正 | `person.sc013.nobumasa` | 22 | 直系亲族 |
| 明智 時姫 | `person.sc013.tokihime` | 44 | 旁系亲族 |
| 佐近 | `person.sc013.sakon` | 40 | 家臣 |

新局不自动任命任何人；玩家必须先做一次人事选择，才能看到第一笔产业产出。六人均为成年人，均可在首月被任命；不新增人物、不改名、不改 ID。

### 4.5 月度数值（暂定，可试玩后调整）

| 产业 | 维护/月 | 产出/月（满职位） | 净额/月 | 说明 |
|---|---|---|---:|---|
| 农庄 | 1 物资（任职时从当月收成中保留） | +2 资金、+4 物资（毛产出） | +2 资金、+3 物资 | 先收成、再保留 1 物资维护；从 0 起始资源也能运作；3 工时不是维护费 |
| 商馆 | 6 资金、2 物资（产出前检查并支付） | +9 资金、+1 情报 | +3 资金、-2 物资、+1 情报 | 资金引擎，但会消耗物资 |

两产业同时满员时，家庭净变化为 **+5 资金、+1 物资、+1 情报/月**。

示例（1560-04 月结，晴信任农庄、千恵任商馆）：

1. 农庄行内：毛收成 +2 资金、+4 物资；保留 1 物资维护；净 +2 资金、+3 物资。净额入账后，库存 `30/12` → `32/15`。
2. 商馆：先检查并支付维护 6 资金、2 物资 → 库存 `32/15` → `26/13`；再产出 +9 资金、+1 情报 → 资金 `26→35`，情报 `0→1`。商馆净 +3 资金、-2 物资、+1 情报。
3. 月末：资金 35、物资 13、情报 1。

### 4.6 空缺、停产与失效

- **空缺**：没有负责人。不扣维护费，不产出。状态“空缺：无人负责，本月无产出”。
- **资源不足（主要适用于商馆）**：有负责人，但结算时资金或物资不足以支付该产业维护费。该产业**不扣任何维护费、不产出**，任命保留；状态“资源不足：缺 X 资金、Y 物资，本月停产”。资源永不为负。农庄的维护来自自身毛收成，不适用“先检查库存再停产”，见下条。
- **农庄的维护顺序例外**：农庄先收成 +4 物资，再从当月收成中保留 1 物资作为维护，因此不依赖结算前的库存，从 0 起始资源也能运作。若未来收成小于维护额，则保留全部收成、净产出为 0，不为负；当前 4 > 1，不会发生。农庄的 3 工时是时间成本，不是维护费。
- **负责人失效**：负责人在结算时已死亡、已退休、已离族或未成年。该产业在结算中自动解除任命，不扣维护费、不产出；状态说明具体原因。下个月可重新任命。
- **替换**：对已占用产业任命新负责人，会原子替换旧负责人；旧负责人的 3 单位**预留**立即释放；若他仍可工作，在当月方案中恢复为 4 单位可用。这里没有“已花时间”需要返还，因为时间只在月结时按锁定方案占用，不按点击次数扣减。
- **解除**：清空负责人并释放其 3 单位预留；空产业再次解除是幂等成功，不产生状态变化。

### 4.7 结算顺序

固定按稳定 ID 升序结算：`industry.farm` → `industry.trading-house`。

- **农庄（先收成、后维护）**：校验负责人 → 在产业行内计算毛产出 +2 资金、+4 物资 → 从当月收成中保留 1 物资作为维护 → 得到净 +2 资金、+3 物资 → 只把净额入账并夹到库存上限。不能先把毛收成 4 加到库存再扣维护。
- **商馆（先维护、后产出）**：校验负责人 → 检查资金/物资是否足够支付 6 资金、2 物资 → 足够则扣维护费并加产出 +9 资金、+1 情报；不足则停产，不扣费、不产出。

农庄先结算，因此农庄当月净产出的资金/物资可以在同月用于商馆的维护费检查。这是有意的“保底产业先恢复”规则，必须写入测试。两种顺序不同，不能用同一个“先扣费后产出”函数草草套用。农庄的净产计算必须先于库存上限夹取（见 §11.2），满仓不会因为维护顺序掉 1 点。

### 4.8 资源不足不阻塞单月结束

单次“结束本月”在资源不足时仍然完成：记录停产、更新资源、推进月份、显示警告。GDD §19.1 要求“已知资源不足”在快速推进时暂停；当前项目没有连续快速推进功能，因此本切片只把 `HasShortage` 暴露给面板和未来快速推进实现。未来若加入连续推进，必须在 `HasShortage` 时暂停并说明原因。不得声称当前已实现快速推进暂停。

## 5. 月度阶段顺序（对照 GDD §19.1）

| GDD §19.1 阶段 | 本切片动作 | 说明 |
|---|---|---|
| 锁定本月安排 | 玩家在结束月份前可调整任命；按下“结束本月”时，当前任命即作为被结算月份的方案锁定 | 月份是抽象回合；本切片不模拟“月初锁定”独立阶段 |
| 完成工作和任务并计算结果 | `IndustrySettlement.Plan` 对 closing month 逐产业校验、扣费、产出 | 纯计算，不修改输入状态 |
| 处理伤亡与失能 | 只处理已存在的死亡/退休/离族/未成年任命，自动解除 | 本切片不新增死亡、疾病、衰老或失能模拟 |
| 处理继承 | 不在一笔月结内处理继承；继承决策/摄政到期会先阻塞结束月份；交接命令单独解除旧家主职位 | 保持既有继承流程 |
| 结算经济、年龄与成长 | 提交资源；年龄/成长/婚姻/教育不在本切片 | 明确缺口，不冒充完成 |
| 检查并排序事件 | 复用 `EventEngine.ScheduleMonthly(nextDate, ...)` | 不改变事件优先级、随机流或内容 |
| 处理必须回应的事件 | 复用现有待处理事件面板 | 月结后若有事件，面板显示，月份暂停 |
| 进入下月 | `WorldClock` 推进到下月 1 日 | 日期与结算同一事务提交 |

**关键顺序**：先做产业计划（纯计算），再排事件，再推进日期，最后提交；任何一步失败都回到原月份与原资源，不留下“已扣费但未推进”或“已推进但未结算”的半状态。

## 6. 时间推进与跨月防护

这是本切片最重要的正确性边界。

### 6.1 唯一结算入口

`GameSession.TryAdvanceToNextMonth()` 是**唯一**允许执行产业月结的入口。它结算“当前日期所在月份”，然后推进到下月 1 日。

### 6.2 其他时间入口

| 入口 | 允许范围 | 跨月行为 | 用户可见原因 |
|---|---|---|---|
| `AdvanceOneDay()` | 仅当前月份内 | 若目标日期进入下月，拒绝，日期不变 | `MonthTurnOutcome.MonthSettlementRequired`；“日期将跨月：请使用「结束本月」完成月度结算。” |
| `AdvanceDays(count)` | 仅当前月份内 | 若目标日期跨月，整笔拒绝，日期不变 | 同上；不得只推进一部分天数 |
| `PreviewTravel(destination)` | 到达日期必须仍在当前月 | 跨月返回 `null`，设置 `LastTravelBlockReason` | “行程将跨越月末：请先结束本月，再安排旅行。” |
| `ConfirmTravel(preview)` | 重新校验路线、天数、到达日期与当前月 | 跨月拒绝，日期与地点都不变 | 同上 |
| 任意入口检测到 `lastSettled` 与当前月不一致 | 月份一致性不变量被破坏 | 拒绝并保持原状态 | `MonthTurnOutcome.IndustryStateInvalid`；“产业状态与当前月份不一致：请读取稳定存档。” |
| `WorldClock.AdvanceOneDay/AdvanceDays` | 纯日期 API，不知道游戏月份 | 保持现状，不负责产业结算 | 仅领域测试使用；不得从 UI 直接调用 |

### 6.3 月份一致性不变量

定义 `MonthIndex(year, month) = year * 12 + (month - 1)`。

在任意稳定保存点（管理界面或等待事件选择）：

```text
industry.lastSettledMonthIndex == MonthIndex(currentDate) - 1
```

含义：当前月份尚未结算；上一个月已经结算。新局 1560-04 的 `lastSettledMonthIndex` 是 1560-03。结束 4 月后日期到 1560-05-01，`lastSettledMonthIndex` 是 1560-04，仍然满足“当前月 - 1”。

`IndustrySettlement.Plan` 的防护：

- `lastSettled == currentIndex`：视为已结算，返回 `AlreadySettled`，不重复扣费/产出；`TryAdvanceToNextMonth` 只推进日期（防御性修复）。
- `lastSettled == currentIndex - 1`：正常结算。
- `lastSettled < currentIndex - 1`：存在漏结算月份，返回 `SettlementGap`，拒绝结束月份；不得静默补算多个月。
- `lastSettled > currentIndex`：状态非法，返回 `InvalidState`，拒绝。

### 6.4 为什么不会跳月或双结算

- 日推进/旅行在目标日期跨月时**整笔拒绝**，不会只走一半。
- 结束月份是唯一结算入口，结算与日期推进在同一事务中提交。
- `lastSettledMonthIndex` 持久化；读档后不会因为日期已到下月而再次结算上月。
- 重复调用“结束本月”会先推进日期，再结算新的当前月，不会重复同一月。
- 若存档中的月份不变量被破坏，读档校验直接拒绝，不进入可玩状态。

### 6.5 既有测试夹具的内部 seam

现有若干 EditMode 测试用 `AdvanceDays(800)` 让人物跨年长大，或把日期推到月末/最大日期。收紧生产 API 后，这些调用必须改为**内部测试 seam**：

- `internal bool AdvanceDaysForLegacyFixtureSetup(int count)`，仅通过 `InternalsVisibleTo("SengokuChronicle.EditModeTests")` 对测试程序集可见。
- 它不是 public API，不被 UI、场景、生产代码或 `GameSession` 内部业务路径调用。
- 它只用于**既有测试夹具的日期/年龄布置**，不做产业生产；为保持存档不变量，它把 `lastSettledMonthIndex` 对齐到新日期前一个月（不产生资源），并在测试中明确标注“fixture setup, not production settlement”。
- 新的跨月/结算测试必须使用生产 API `TryAdvanceToNextMonth`、`AdvanceOneDay`、`AdvanceDays`、`PreviewTravel`、`ConfirmTravel`，不得用该 seam 绕过被测规则。
- 生产不变量保持不变：`AdvanceOneDay`/`AdvanceDays`/旅行永远不能跨月；`TryAdvanceToNextMonth` 永远是唯一月结入口。

## 7. 人员任命与继承交接

### 7.1 可任命条件

一名人物可被任命，当且仅当：

1. `PersonId` 存在于当前 `SuccessionState.World`。
2. 是当前家族成员（`FamilyState.IsMember`）。
3. `Alive == true`。
4. `Retired == false`。
5. 年龄 `>= AdulthoodAge`（当前 16）。
6. 剩余时间单位 `>= 3`（未任职者为 4；已任职者为 1，因此不能兼任第二职位）。

失败返回明确结果与中文原因，不修改状态。

### 7.2 任命/替换/解除

- `TryAssignIndustry(industryId, personId)`：校验产业、人物、时间；若同一人已是该产业负责人，返回成功 no-op；若产业已有其他人，原子替换；旧负责人的 3 单位预留被释放；若他仍可工作，恢复为 4 单位可用。
- 任命 API 只表达“当月方案预留”；它不扣资源、不产出、不写编年史。真正的资源变化只发生在月结。
- `TryUnassignIndustry(industryId)`：清空并释放预留；空产业返回成功 no-op。
- 任命变更只在管理稳定点允许；有待处理事件、继承决策、摄政到期或家族灭亡时拒绝，并显示原因。这样不会在事件选择中途改变当月方案。
- 不追踪“每人本月已花时间”。任命是预留，替换/解除即释放；月结只按锁定方案占用 3 单位。这样“立即释放”和“不可返还”不会同时成立：本设计选择**预留模型**，没有已消耗时间需要返还。
- 手动任命/解除不写编年史（避免每次点击刷屏）；面板和存档负责可观察性。继承导致的自动解除写编年史（见 7.4）。

### 7.3 月结时的失效处理

结算开始时，对每个已任命产业调用 `IndustryService.CanWork`。不满足条件者：

- 在 `NextState` 中清空任命；
- 本月该产业不扣维护费、不产出；
- 报告行保留失效原因与旧负责人姓名；
- 下个月可重新任命。

若同一人物意外出现在两个产业（正常任命 API 不允许，但存档损坏或旧 bug 可能造成），按产业 ID 升序保留第一个，清空后续重复任命并报告“重复任命已解除”。这保证一次月结不会让同一人产出两次。

### 7.4 继承交接

继承仍由现有 `SuccessionService` 与 `GameSession.ApplySuccessionResult` 负责。本切片只追加一个**总函数**式协调步骤：

- 交接成功后，逐产业检查负责人是否仍可工作；不可工作者自动解除。
- 旧家主退休后 `Retired=true`，其产业职位自动解除，3 单位预留释放；但他已退休，可用时间为 0，因此不会因此获得可任命时间。
- 新家主不会自动接管旧家主的职位；若新家主原本已有职位且仍合法，则保留（受“3+3>4”限制，他不可能同时持有两个职位）。
- **新家主预算规则**：每个成人始终只有自己的 4 单位；继承不发放额外预算，也不转移旧家主预算。新家主若原本未任职，可用 4；若原本已任职，可用 1，不能再接第二职位。旧家主的 3 单位只是释放预留，不会转给新家主。
- **同月任命**：交接发生在月结前的管理稳定点时，允许用新家主自己的当月预算任命/替换/解除，属于被结算月份的方案；交接发生在月结后时，任命属于下月方案。两种情况下都不产生额外预算，也不会把旧家主的时间转给新家主。
- 只有发生实际解除时才追加一条“产业人事”编年史；没有职位变化则不写。
- 协调步骤只做“移除不合法任命”，是总函数，不会失败；因此不会让继承状态与产业状态半提交。

两次固定交接的预期：

| 时点 | 家主 | 农庄 | 商馆 | 结果 |
|---|---|---|---|---|
| 开局 | 晴信 | 晴信 | 千恵 | 两产业满员 |
| 第一次交接后 | 正綱 | **空缺**（晴信退休自动解除） | 千恵（仍是成员，保留） | 正綱不自动接管农庄 |
| 玩家调整后 | 正綱 | 千恵或其他人 | 正綱（若先解除千恵） | 受时间预算限制，正綱只能占一个职位 |
| 第二次交接后 | 信正 | 保持合法任命 | **空缺**（正綱退休自动解除） | 信正不自动接管商馆 |

测试必须覆盖两次交接、旧/新家主职位、非家主职位保留、预算不转移、编年史只写一次。

## 8. 资源不足与恢复

- 商馆是主要资源风险：单独运行商馆时，每月净 `+3 资金、-2 物资、+1 情报`；物资从 12 开始，约 6 个月后归零，商馆停产。
- 农庄先收成再保留 1 物资维护，因此从 0 起始物资也能运作；只要有人任职，就能每月净 `+2 资金、+3 物资`，是明确的恢复路径。
- 资金不足时商馆同样停产；农庄继续产出资金，逐步恢复。
- 停产不解除任命；玩家可以保留任命等资源恢复，也可以解除任命以明确意图。
- 结算按农庄→商馆顺序，农庄当月净产出可以立即用于商馆维护费检查；这使“先保农庄”成为可解释的策略。
- 资源不足不导致负数、欠款或自动变卖资产。

## 9. 存档 v6 与迁移

### 9.1 Schema

存档 schema 从 v5 升为 **v6**；`rulesVersion` 从 1 升为 **2**。理由：产业规则是新增的可保存规则，旧档迁移后获得明确的初始产业状态，而不是被静默当作“已经营多年”。

新增根字段：

```json
{
  "industry": {
    "funds": 30,
    "supplies": 12,
    "intelligence": 0,
    "lastSettledMonthIndex": 18722,
    "slots": [
      { "industryId": "industry.farm", "assignedPersonId": "" },
      { "industryId": "industry.trading-house", "assignedPersonId": "" }
    ]
  }
}
```

- `slots` 必须恰好包含两个稳定产业 ID，按 ordinal 升序；`assignedPersonId` 为空表示空缺。
- 不保存“当前报告文本”；月结报告是瞬时的，读档后从最后一条 `Industry` 编年史恢复展示。
- 不保存产业等级（本切片固定 1 级）；未来升级需要新 schema 决策。

### 9.2 校验

`SaveDataValidator` 在 v6 校验：

- `funds/supplies/intelligence` 在 `0..999999`。
- `lastSettledMonthIndex == MonthIndex(saveDate) - 1`。
- `slots` 非空、恰好两个、ID 唯一且属于 `IndustryCatalog`。
- 非空 `assignedPersonId` 必须是 `succession.persons` 中存在、`succession.members` 中在册、存活、未退休、年龄 `>= adulthoodAge` 的人物。
- 同一人物不能出现在两个 slot。
- 任一条件失败都拒绝整份存档，不部分恢复。

### 9.3 迁移

- **v5 → v6**：保留日期、地点、任务阶段、NPC 关系、`familyRelationships`、`chronicle`、`succession`、事件运行时、随机流；新增初始 `industry`（30/12/0、两产业空缺、`lastSettled = MonthIndex(date)-1`）；`rulesVersion = 2`；不追加编年史，不补算历史产出。
- **v4 → v6**：先按既有 v4→v5 规则补空 `familyRelationships`，再加初始 `industry`。
- **v3/v2/v1 → v6**：保留各自既有迁移语义，最后加初始 `industry` 与 `rulesVersion = 2`。
- v5 载荷若已包含 `industry` 根字段，迁移**拒绝**，不静默替换（与 v4 已含 `familyRelationships` 的处理一致）。v2–v4 原始 JSON 若已含 `industry` 也拒绝；v1 解析器没有该字段，若原始 JSON 含该根字段同样拒绝。
- 迁移不修改源对象/源文件；失败保留原文件并说明原因。

### 9.4 稳定保存点与不重复月结

- 允许保存的稳定点：月份结算后的管理界面；重大事件/继承选择已排队并等待玩家输入的界面。
- 保存包含 `lastSettledMonthIndex`；读档只恢复，不结算、不追加编年史。
- 读档后结束月份，只会结算当前月一次；再次读取同一存档并重复操作，得到相同结果与相同编年史条数。
- 结算过程中不保存；UI 不提供“结算中保存”。

### 9.5 失效任命的安全保存

死亡、退休、离族或未成年可能在月结前让某个任命失效；不能让这种状态导致“稳定点无法保存”。

处理顺序：

1. **继承交接后立即清理**：`ApplySuccessionResult` 成功后调用 `EnsureIndustryAssignmentsValid`；旧家主退休导致的职位解除当场生效。
2. **保存前清理**：`GameSession.CaptureSave` 在构建存档前再次调用 `EnsureIndustryAssignmentsValid`。该方法是总函数，只移除不合法任命，不扣维护费、不产出、不推进时间、不追加编年史；若移除，设置瞬时的 `LastIndustryMessage`（例如“保存前移除了失效任命：农庄/晴信”）。
3. **月结时清理**：`IndustrySettlement.Plan` 在纯计算的克隆状态上再次校验；即使前两步被绕过，月结也不会让失效任命产出。
4. **读档校验**：`SaveDataValidator` 仍拒绝含失效任命的 v6 存档。正常捕获已经清理，因此不会产生这种存档；若外部篡改或旧 bug 产生，读档失败并保留原文件，不进入半可玩状态。

不把保存前清理写成编年史：保存不是游戏内动作，重复保存会造成重复记录；清理结果通过本次会话的面板消息可见，下一次月结报告会显示该产业当月空缺/无产出。这样“稳定保存点”始终可保存，且不会因为死亡/退休/离族把玩家锁在无法存档的状态。

## 10. 编年史与 UI

### 10.1 编年史

新增 `ChronicleCategory.Industry`（数值 4），复用现有 `FamilyChronicleEntry` 的 `FromValue/ToValue` 表示资金前后值，摘要文本包含物资/情报与逐产业明细。

每次实际月结（至少一个产业有人任职，或发生自动解除）追加**一条**月结记录：

- 标题：`产业月结`
- 主体：`industry.monthly`
- 来源：`settlement`
- 年份/月份：被结算月份
- 资金前后值：`FromValue`/`ToValue`
- 摘要示例：`农庄（晴信）：收成 +2资金/+4物资，保留 1物资维护，净 +2资金/+3物资；商馆（千恵）：维护 6资金/2物资，产出 +9资金/+1情报；月末 资金 30→35，物资 12→13，情报 0→1。`

继承导致自动解除时追加一条：

- 标题：`产业人事`
- 主体：`industry.assignments`
- 来源：`succession`
- 摘要：`晴信退休，农庄负责人自动解除；商馆仍由千恵负责。`

编年史面板新增类别名“产业”，详情显示“资金 X → Y”；不显示原始 ID。若两产业都空缺且无自动解除，不写月结记录，面板显示“本月无产业运作”。

### 10.2 UI 集成

新增一个 `IndustryPanel`，挂在 Canvas 下的固定根 `IndustryRoot`，与 `FamilyChronicleRoot`/`FamilyRosterRoot` 同级：

- 打开按钮：`产业`，放在现有左侧管理按钮列，位于编年史按钮附近；生成器校验不与既有按钮重叠。
- 面板：760×560，居中，标题“产业与人事”。
- 顶部资源行：`资金 30　物资 12　情报 0`。
- 阻塞行：有待处理事件/继承决策/家族灭亡时显示“任命暂停：…”；否则隐藏。
- 两个固定产业行：
  - 名称/容量：`农庄（等级 1・1 个职位・3/4 工时）`
  - 状态：`运作中` / `空缺：无人负责，本月无产出` / `资源不足：缺 2 资金、1 物资，本月停产` / `负责人失效：已死亡/已隐居/已离族/未成年，月末自动解除`
  - 负责人：`负责人：晴信（3/4 已用，1 可用）` 或 `负责人：空缺`
  - `解除` 按钮；候选按钮容器。
- 候选按钮在运行时按合格人物生成，标签 `晴信（剩余 4/4）`；点击即任命/替换。候选按 `PersonId` ordinal 排序。
- 月结报告区：
  - 有本次报告时显示完整前后值；
  - 读档后没有瞬时报告时，显示最后一条 `Industry` 编年史摘要；
  - 都没有时显示“本月尚未结算。”
- 面板只读展示 + 任命/解除两个动作；不推进时间、不修改事件/继承/存档。

### 10.3 场景生成器与验证

- 一个指定集成 worker 独占 `Assets/Scenes/FoundationScene.unity`、`Assets/Scripts/Editor/AddSC019IndustryPanel.cs` 和 `FoundationSceneValidator.cs` 的场景部分。
- 生成器菜单：`Tools/Sengoku Chronicle/Add SC-019 Industry Panel`。
- 单个 Undo group；先 preflight（场景路径、未脏、非 Play、无重复/部分根），再创建全部对象、绑定引用、`Validate`，通过后才保存；异常时 `Undo.RevertAllInCurrentGroup`，不重新加载场景。
- `Validate(Scene, out string error)` 检查：唯一 Canvas/GameSession/IndustryPanel/IndustryRoot；根是 Canvas 直接子级且 active；面板默认隐藏；两个产业行、资源行、阻塞行、报告行、关闭按钮、候选模板存在且绑定；候选模板 inactive；根位于地图之上、标题/事件覆盖层之下。
- `FoundationSceneValidator` 增加上述检查并写入 `Logs/SceneValidationReport.txt`。
- 不修改 `ProjectSettings`、prefab、package 或既有 `.meta` GUID；若生成器意外需要 `ProjectSettings`，停止并报告，不擅自修改。

## 11. 确定性与事务

### 11.1 确定性

- 产业按稳定 ID 升序；候选人物按 `PersonId` ordinal 升序；存档 slot 按产业 ID 升序。
- 月结不消费随机流；事件排程继续使用既有随机流和既有顺序。不得因为产业逻辑插入额外随机调用。
- 报告文本使用 `InvariantCulture` 和固定格式；同一输入产生逐字节相同的摘要。
- 不遍历 `Dictionary` 决定状态变更；所有顺序来自排序后的列表。
- 同一初始状态、同一任命、同一月份，`IndustrySettlement.Plan` 的结果必须完全相等。

### 11.2 安全算术

- 资源为非负 `int`，上限 `999999`；领域构造和存档校验都拒绝越界。
- 扣费前检查 `>=`，不做可能下溢的减法；产出用 `long` 中间值后 `Math.Min` 到上限。
- **农庄必须先算净产、再入账、最后夹到上限**：毛收成 4 物资 − 当月保留维护 1 物资 = 净 3 物资，然后把净 3 加到库存并夹到 `999999`。不能先把毛收成 4 加到满库存、再扣 1 维护，否则满仓会从 `999999` 掉到 `999998`。满仓时实际入账增量为 0，报告必须写“净产 3，实际 +0（已达上限）”，不能写 +3。
- 商馆仍先检查并支付维护费，再产出；满仓时同样用“实际前后值”报告，维护费真实扣除、产出可能被上限吸收。
- `MonthIndex` 最大约 24011，远小于 `int.MaxValue`；仍用 `long` 做乘加防护。
- 任何越界都返回失败/截断并记录“已达上限”，不抛异常、不产生负数。

**满仓农庄示例**：物资 `999999`，农庄满员。毛收成 4 → 维护保留 1 → 净 3；库存加 3 后夹到 `999999`，实际变化 `+0`。最终物资仍为 `999999`，不是 `999998`。

### 11.3 事务与回滚

`TryAdvanceToNextMonth` 的顺序：

1. 预检：待处理事件、继承暂停、内容可用、下一月日期合法。
2. 快照：当前日期、产业状态、随机流、事件运行时、编年史条目数组。
3. `IndustrySettlement.Plan`：纯计算；失败则原状态不变。
4. 用 `nextDate` 调用 `EventEngine.ScheduleMonthly`；失败则恢复随机流，原状态不变。
5. 追加月结编年史（若有实际结算）；失败则恢复编年史并返回失败。
6. 提交产业状态、事件运行时、日期；发布一次 `SessionStateChanged`。
7. 设置瞬时 `LastIndustryReport`；不写存档。

异常路径统一恢复快照；不允许“日期已变但产业未提交”或“产业已扣费但事件未排定”。

`TryAssignIndustry`/`TryUnassignIndustry` 先完成全部校验，再一次性修改；替换是单次赋值，不存在半替换。

`TryApplySave` 沿用现有“先构建全部替代状态、最后一次性赋值”的模式；产业状态在构建阶段校验，失败不触碰当前会话。

### 11.4 幂等

- 同一产业同一人重复任命：成功 no-op，不重复扣时间、不写编年史。
- 空产业重复解除：成功 no-op。
- 重复月结：由日期推进 + `lastSettledMonthIndex` 防护；同一月份不会结算两次。
- 重复读档：只恢复，不结算、不追加。
- 继承协调重复执行：第一次解除后第二次无变化，不重复写编年史。

## 12. GDD 对照

| GDD 条目 | 本切片落实 | 边界 |
|---|---|---|
| §2.3 统一行动经济：每名成年人每月 4 单位；持续性工作按职责占用时间；同一人不能参加冲突工作 | 4 单位；产业职位 3 单位；`3+3>4` 阻止兼任；剩余 1 单位显示 | 其他耗时行动（训练、教导、外交、任务）未接入 |
| §5.1 三个资源：资金、物资、情报 | 三者均可观察、可保存；农庄产资金/物资，商馆产资金/情报 | 情报无消耗入口 |
| §5.2 产业具有维护成本、负责人员和工作容量；升级受人员/资源制约 | 两产业 1 级、1 职位、明确维护与容量；任命受时间预算约束 | 无建设/升级/扩建 |
| §12.2 原型范围：产业 2 种 | 正好两种：农庄、商馆 | 不新增第三种 |
| §13 M3：产业、人员任命、自动工作 | 完成这三项的首个纵向切片，含月结、空缺、短缺、继承交接、存档 | 人物成长、婚姻、收养、教育、隐居、摄政、家训、抱负未实现；不声称 M3 完成 |
| §19.1 月度顺序、暂停点、行动占用 | 固定顺序；待处理事件/继承暂停；日/旅行跨月拒绝；唯一月结入口 | 无连续快速推进；资源不足只警告不暂停单月 |
| §19.2 继承保留人物 ID、家族资产、契约、历史；旧家主未完成工作按规则取消或移交 | 人物 ID 不变；合法产业任命保留；旧家主职位自动解除；新家主不自动接管；编年史记录 | 不实现死亡/失能/摄政新规则；只协调既有状态 |
| §19.3 稳定保存点、版本兼容、读档不重复提交 | v6 存档；稳定点保存；迁移保留 SC-018 关系与全部历史；读档不结算 | 不承诺旧档补算历史产业收益 |

## 13. 明确排除与未解决规则

本切片不做，且不得在实现或试玩报告中暗示已完成：

1. 不实现完整 M3；不实现人物成长、婚姻、收养、教育、隐居、摄政、家训、抱负。
2. 不实现动态市场、价格、拍卖、贸易路线、商队、行业竞争、外部经济引擎。
3. 不实现产业建设、升级、扩建、多职位、分店、地产。
4. 不实现雇佣、解雇、工资、俸禄、人物招募；只用现有六人。
5. 不实现部分工时/兼职；一个职位固定 3 单位。
6. 不实现负数、欠款、借贷、利息、破产、自动变卖。
7. 不实现自动重新任命；空缺必须由玩家处理。
8. 不实现情报消耗、任务准备、外交、政治行动；情报只积累和显示。
9. 不实现死亡、疾病、衰老、失能的新模拟；只尊重既有 `Alive`/`Retired`/成员/年龄状态。
10. 不实现连续快速推进；未来实现时必须在资源不足、待处理事件、继承暂停处停止。
11. 不修改事件目录、事件优先级、事件效果、任务阶段或 NPC 关系规则。
12. 不新增人物、场景、prefab、package、Unity 项目或 `ProjectSettings` 变更；只新增一个 UI 根和面板。
13. 不把既有三项任务视为已占用人物时间。当前任务与产业任命可以同时存在，这是已知不一致；后续规则必须让任务消耗时间并与产业职位冲突。本切片只记录该缺口，不伪造“任务已预留人员”。
14. 不把“两个产业 + 两次交接”当作 M3 验收；M3 的“稳定经营至少两个世代”仍需更完整的经济、成长与继承验证。

## 14. 验收目标

### 14.1 功能验收

- 新局资源为 30/12/0，农庄/商馆均空缺，`lastSettledMonthIndex` 为 1560-03。
- 可将晴信任命到农庄、千恵任命到商馆；面板显示 `3/4` 与剩余 1。
- 结束 1560-04 后资源为 35/13/1；编年史新增一条“产业月结”，前后值正确。
- 农庄空缺时商馆仍可按规则结算；商馆资源不足时停产、资源不为负、任命保留。
- 农庄从 0 资金/0 物资也能先收成后维护，得到净 +2 资金/+3 物资；物资满仓 999999 时，净产 3 先算再夹上限，满仓仍为 999999，不因维护顺序掉到 999998。
- 日推进/旅行在月末被拒绝，日期与地点不变；结束月份成功结算一次。
- 两次继承交接后，旧家主职位自动解除，新家主不自动接管，其他合法任命保留。
- 保存/读档恢复资源、任命、已结算月份；读档后结束月份只结算一次。
- v5→v6 迁移保留 SC-018 `familyRelationships`、全部编年史、事件历史、继承与任务状态；旧档不补算产业收益。
- 既有 SC-011–SC-018 事件、M2 固定剧本、SC-010 标准/危机路线不被破坏。

### 14.2 证据边界

必须分开记录，不得合并成“通过”：

- Unity 编译（Editor 日志）。
- EditMode 测试（通过/失败/跳过数量）。
- Console 错误/警告计数。
- `FoundationSceneValidator` 文本报告。
- Play Mode 实际路线（含月结、跨月拒绝、继承交接）。
- 存档/读档实际结果。
- **协调方/助手执行的 Play Mode**：项目默认由用户执行 Play Test；当前用户已明确授权协调方/助手使用 Computer Use 操作现有 Unity Editor。记录操作者、日期、场景、路线、Console 计数与截图/观察证据；它与用户验收分开。
- **用户验收**：用户对演示的接受/拒绝，单独记录；协调方执行的 Play Mode 不替代用户验收。

本设计文档不声称任何运行证据；当前只有静态阅读与既有文档。实现后由实现/QA worker 分别报告编译、EditMode、Console、Validator 与 Play Mode；协调方可在用户当前授权下用 Computer Use 执行 Play Mode 并记录为“coordinator-run Play Mode”，用户验收仍是独立的演示门槛。用户验收前不提交、不合并、不打 tag。

## 15. 自审：矛盾、可行性与剩余风险

| 检查 | 结论/处理 |
|---|---|
| 4 单位预算但只实现 3 单位职位，剩 1 单位不可花 | 有意保留给后续训练/教导/任务；UI 明示“本切片无其他耗时行动”，不假装已使用 |
| 农庄维护费如何体现 GDD“产业有维护成本” | 农庄维护为 1 物资，从当月毛收成中保留；3 工时只是时间成本，不作为维护费。农庄从 0 资源也能先收成后维护，净 +2 资金/+3 物资 |
| 情报只产不耗 | 明确排除；GDD 的情报用途属于任务/政治/外交，未在本切片实现 |
| 既有任务不占时间，却可与产业任命并存 | 明确列为已知缺口；不修改任务规则，不声称已解决 |
| 收紧 `AdvanceDays` 会破坏既有跨年测试 | 用 internal、仅测试程序集可见的 `AdvanceDaysForLegacyFixtureSetup` 做夹具布置；生产 API 不变量不变；新测试只用生产入口 |
| 继承发生在月结前，新家主是否重复获得预算 | 不重复：每个成人只有自己的 4 单位；旧家主职位解除的是“预留”，不是可转移预算；新家主若未任职可用 4，若已任职可用 1。允许用新家主自己的预算在当月方案中任命，但不产生额外预算 |
| “替换立即释放”与“已花时间不返还”是否矛盾 | 已消除：采用**预留模型**，没有已花时间计数器。替换/解除释放 3 单位预留；月结只按锁定方案占用一次。退休旧家主的释放不会产生可用时间，因为他已退休 |
| 死亡/退休/离族后、月结前无法保存 | `ApplySuccessionResult` 立即清理；`CaptureSave` 保存前再次清理；`IndustrySettlement.Plan` 月结时再校验；读档拒绝非法存档。稳定点始终可保存，清理不写编年史以避免重复 |
| 资源不足是否会让玩家卡死 | 农庄的维护来自自身收成，不依赖外部库存；从 0 资源也能净 +2 资金/+3 物资，只要有人任职就能恢复。玩家不任命农庄是玩家选择，不是规则死锁 |
| 月结报告不持久化，读档后看不到上次报告 | 从最后一条 `Industry` 编年史恢复摘要；资源本身持久化 |
| 场景生成器增加复杂度 | 只加一个固定根、两个固定行、运行时候选按钮；复用现有 Undo/Validate/Rollback 模式；单测覆盖 |
| 迁移把旧档初始资源设为 30/12/0 是否算“凭空收益” | 迁移不补算历史、不追加编年史；初始状态是明确的兼容默认值，已在文档和测试中声明 |
| 是否会把“两个产业 + 两次交接”误报为 M3 完成 | §12/§13 明确不是；M3 仍需成长、教育、婚姻、摄政等 |

**真实阻塞项**：无。唯一需要协调方知晓的实现边界是“既有跨年测试需要 internal fixture seam”；该 seam 不暴露为 public API，也不被生产路径调用。若实现中发现 Unity 场景生成器无法在用户打开的 Editor 中安全执行，按项目规则停止并报告，不启动 batchmode、不抢用用户 Editor。

## 16. 决策记录

- 用户选择产业、人员任命、每月自动工作，并把研究、设计、计划决策委托给 lead。
- lead 选择首切片为“农庄 + 商馆”，三资源，4 单位/成人/月，3 单位/职位，1 职位/产业。
- 农庄维护为 1 物资，从当月毛收成中保留（收成后维护）；3 工时只是时间成本，不作为维护费。商馆仍先检查并支付维护费后产出。
- lead 选择纯 C# 领域模型 + v6 存档 + 现有 uGUI 面板；不引入外部代码/package。
- lead 选择“日/旅行跨月拒绝，结束月份唯一结算”作为首切片的时间规则。
- lead 选择**预留模型**：任命预留 3 单位，替换/解除释放预留，不建立“已花时间”计数器；继承不转移预算，新家主只用自己的 4 单位。
- lead 选择**保存前安全清理**失效任命，避免死亡/退休/离族在月结前造成不可保存状态。
- 当前用户已明确授权协调方/助手使用 Computer Use 在现有 Unity Editor 中执行 Play Mode；记录为“coordinator-run Play Mode”，与用户验收分开。用户验收仍是演示接受门槛。
- 本稿不设置额外设计审批门；实现后由协调方复审。


---

<a id="2026-11-04-sc-020-character-growth-education-design"></a>
## 来源：docs/superpowers/specs/2026-11-04-sc-020-character-growth-education-design.md

# SC-020 设计稿：子女培养与人物成长

## 意图

在 SC-019 家族产业切片之后，推进 GDD v4.0 的 M3 家族经营：让玩家为未成年家族成员安排年度培养方向，并看到培养如何影响其成年时的能力与家族记录。首轮试玩使用独立示范剧本，在一个真实游戏年度内完成一名 15 岁孩子的培养与成年转变；不声称完成人物成长系统或 M3。

## 当前项目依据

- GDD v4.0 §3.4、§3.5 规定人物成长来自训练、任务、工作、师徒和事件；0–15 岁采用年度培养，方向为忍术、武艺、学问、经营、社交、自由成长，结果受指导者、家庭环境和特质影响。
- 当前 SC-013 固定剧本的六名重要人物均已成年；年龄由 `SuccessionPersonLife.AgeAt(GameDate)` 按出生年份计算。现有代码没有通用技能成长、年度教育、特质成长或儿童教育界面。
- SC-019 将月份结算做成事务，并在存档、编年史、场景生成器中纳入产业；本切片应复用现有稳定点和存档迁移规则，不改变 SC-013 / SC-019 的既有剧本。
- SC-018 / SC-019 当前仍有未提交工作；实现前须保留原工作区全部改动，不能清理、回退或覆盖。

## 目标

1. 在现有 Unity 项目里提供独立的 M3 教育示范剧本，不改 SC-013/SC-019 的剧本入口和内容。剧本开局为 `1560-01-01`，青叶家家主宗一郎（40 岁）、配偶小夜（38 岁）、直系亲属千景（15 岁）；宗一郎和小夜均可作为导师。人物 ID 固定为 `person.sc020.head`、`person.sc020.spouse`、`person.sc020.child`，并建立家主-配偶、两名父母-孩子的亲属边。
2. 标题页新增“SC-020 子女培养示范”入口。进入后打开培养面板：显示孩子姓名/年龄、五项能力、当前教育年度、六个培养方向、导师选择，以及提交前的预计变化。玩家为 15 岁年度选择一种方向及导师；同一角色同一年龄年度仅保留一份尚未结算的计划。
3. 结束 1560 年 12 月时，只结算一次该教育年度，显示方向、导师、能力前后值和简短原因，并写入一条家族编年史。
4. 日期进入 `1561-01-01` 后，孩子年龄变为 16，停止儿童年度培养并转入成年状态；继承、产业任命仍使用既有资格规则。
5. 保存/读取保留成长能力、已选计划和年度结算标记；v6→v7 迁移对旧档初始化空教育历史，不虚构历史培养结果。
6. 提供 EditMode 覆盖、场景 Validator 与一条由用户执行的 Play Test 路线。编译、EditMode、Console、场景 Validator 与 Play Mode 分开记录。

## 非目标

- 不实现婚姻、出生、收养流程、家谱编辑、导师关系的自由创建、隐居、摄政、完整战术/任务成长或长期人口模拟。
- 不改写 SC-013 固定继承路线、SC-018 事件内容、SC-019 产业规则，也不把本切片描述为 M3 完成。
- 不引入第三方包、外部资产、付费服务或新 Unity 项目；不复制商业游戏内容。
- 不在本切片内实现开放式成长属性编辑器或 Debug Mode。
- 不添加可自由创建的家族成员、外部学校、教育设施建造或导师专属剧情；示范人物、资质、导师专长和家庭环境固定于 SC-020。

## 考虑过的方案

1. **把儿童加入 SC-013 固定剧本。** 复用现有入口最直接，但会改变已验收的 M2 剧本与六人样例，扩大回归范围。
2. **独立 M3 教育示范剧本（推荐，由项目 lead 选择）。** 新增独立入口与固定未成年案例；可以完整展示年度培养和成年交接，同时让 SC-013 / SC-019 样例保持原样。代价是增加一个场景剧本配置和一条独立试玩路径。
3. **先实现通用成长模型，不提供未成年人示例。** 领域代码较窄，但无法在下一轮 Play Test 里直接验证 GDD 的年度教育体验，也会把规则与内容验证推迟。

推荐方案 2。它让下一轮 Play Test 立即验证 GDD 明确写出的教育决策，同时把变更隔离在 M3 新剧本。

## 规则与具体数值

- 成长状态按稳定 `PersonId` 保存，不以名字或显示顺序作为键。能力范围为整数 `0..100`，五项能力为忍术、武艺、学问、经营、社交。
- `person.sc020.head` 为青叶宗一郎（出生年 1520，武艺导师）；`person.sc020.spouse` 为青叶小夜（出生年 1522，学问导师）；`person.sc020.child` 为青叶千景（出生年 1545，直系亲属）。开局日期 `1560-01-01`，家主/配偶/孩子的年龄分别为 40/38/15。孩子通过 `ParentChild` 边连接两位成年人，成年人间存在 `Spouse` 边。新局其余日期/地点/初始产业资源沿用现有 SC-019 初始规则。
- 示例孩子开局能力依次为 `忍术 15 / 武艺 12 / 学问 20 / 经营 10 / 社交 13`。示范孩子固定“学问资质”；家族环境固定为“有利”。两名导师各有一个固定专长：家主擅长武艺，配偶擅长学问。以上均是 SC-020 示范剧本数据，不新增通用人物特质编辑器。
- 五种定向培养的公式为：对应能力 `+4` 基础成长；孩子资质与方向相同 `+2`；导师专长与方向相同 `+2`；有利家庭环境 `+1`。最终值夹在 `0..100`。因此本示范中选择学问并由配偶指导得到 `+9`；选择其他方向或导师会在面板显示较小的确定增量。
- “自由成长”不选导师，五项能力各 `+1`。不使用随机数。所有增量与夹值均为纯 C# 确定规则；`CharacterGrowth` 随机流状态不应被消耗。
- 0–15 岁每人每个年龄年度可提交一个方向和一名有效成年家族导师；未提交计划的年度按自由成长结算。年龄按现有 `AgeAt(GameDate)` 的年份差计算，16 岁起不再开放儿童培养。本次 Play Test 只展示千景 15→16 岁这一年。
- 玩家可在 1560 年 12 月结算前替换尚未结算的年度计划；只有 12 月到次年 1 月的年度结算会更改能力，重复提交不会即时发放奖励。
- 年度结算发生在关闭 12 月、将日期从 `12-01` 推进至下一年 `01-01` 的事务里：先完成该月产业结算，然后完成教育成长，再排程该新月份事件，最后提交日期与年度结算标记。结算失败时日期、产业、成长状态、随机流、事件运行态和编年史都必须回滚。
- 同一 `(PersonId, AgeYear)` 只可有一条计划与一个结算结果。计划提交要验证人物仍是家族成员、存活、年龄在 `0..15`、方向合法、导师是存活且未退休的成年家族成员；自由成长要求导师为空。替换计划不产生能力或编年史变化；月结事务重复执行不得重复应用教育结果。
- 存档升至 schema v7、`rulesVersion` 3。v6→v7 迁移为存档中已有的每个人物初始化五项 `0` 能力与空教育记录，保留原始人物/关系/事件/编年史/产业状态，不补算成长。v1–v5 继续通过现有迁移链到 v7。SC-020 新局使用同一 v7 数据形状并初始化千景的固定能力值。
- 若 SC-020 的年度月结失败，日期、产业、所有人物成长、教育计划、随机流、事件运行态和编年史回滚；失败后允许以相同计划重试。
- 新场景入口和 UI 应复用现有 FoundationScene 及事务式生成器模式；任何场景改动由单一指定集成人员完成。
- 不安装包；因此无需新增包决策。若实现需要外部内容，先停止并报告来源与许可证，不自行引入。

## GitHub 先例调研

最初调研仅筛选三项，读取 GitHub README / 文档；没有克隆或安装任何内容。

| 项目 | 许可证 / 兼容性 | 适配度、维护与风险 | 决策 |
|---|---|---|---|
| [LifeStepByStep](https://github.com/BenetManzanaresSalor/LifeStepByStep) | README 仅称可免费使用与修改并需署名；仓库页没有可核实的标准许可证，Unity 版本未注明 | Unity 生命模拟，44 个提交、12 stars；侧重 3D 移动、生存、繁殖和时间寿命，依赖作者的 Lost Cartographer Pack，与本项目年度家庭教育模型无关 | 不复用；许可证与资产链不清晰 |
| [Educational Gamification Systems for Unity](https://github.com/Ocean-View-Games/educational-gamification-systems-unity) | MIT；README 明确验证 Unity 6000.0.73f1+，无第三方依赖 | 7 个提交、0 stars；能力是课程目标掌握度、学习分析和自适应难度；面向现实学生/LMS，不支持角色家庭状态、培养结果存档，过于宽泛 | 不安装包；只参考其把学习结果与游戏规则分离的做法 |
| [My City](https://github.com/cesswhite/my-city) | MIT（代码范围按 README）；Godot 4.7 / GDScript，不兼容 Unity 方案 | 1 个提交、0 stars；有生活模拟、学习活动、人物关系与存档，但语言/引擎不符，含可选后端和 AI 集成 | 不复用；只作题材旁证 |

上述活跃度是本次 GitHub 页面快照，不代表完整维护审计。未发现值得引入的 Unity 6 家庭年度教育包；采用本地纯 C# 定制实现。

## 用户可见验收

- 标题界面进入独立“SC-020 子女培养示范”，不影响现有入口。
- 新局日期为 `1560-01-01`；家谱/名册显示青叶宗一郎 40、青叶小夜 38、青叶千景 15，亲属关系、两位导师专长和 5 项初始能力正确。
- 选“学问”并由小夜指导时，预计学问 `20→29`；改由宗一郎指导时，预计学问 `20→27`。选择其他方向时按资质/导师规则显示准确预览。
- 保存后读档，年度计划与原始能力值仍在；将 1560 年推进至年末，再结束 12 月只应用一次培养，1 月显示 16 岁和培养前后变化，并在编年史只有一条对应记录。
- 重复操作、重复结束月份、存档重读不能让同一年重复获得成长。
- 培养至 16 岁后显示成年并禁用儿童年度培养；现有继承与产业资格不被破坏。
- SC-013 标准样例和 SC-019 产业样例的既有入口/剧本行为不变。
- 用户 Play Test 由用户执行并报告；协调方的 EditMode、编译、Console、场景 Validator 证据分开记录。

## 授权状态

- 用户已选择“人物成长与子女教育”作为下一片 M3 主线，并先前将 M3 方向研究与计划交由项目 lead 负责。
- 独立 M3 教育示范剧本、五项能力、培养公式、导师和年度事务顺序是项目 lead 的具体设计选择，用户可在审阅时修订。
- 用户审阅本设计稿并确认前不开始 SC-020 代码实现。
- 代码实现由 OpenCode2 负责；后续由独立 OpenCode reviewer 只读审查；Play Test 由用户执行。


---

<a id="2026-11-04-sc-021-family-research-design"></a>
## 来源：docs/superpowers/specs/2026-11-04-sc-021-family-research-design.md

# SC-021 家族研究与技艺传承设计

状态：用户已批准并授权实现；本稿保留为设计依据。  
依据：`GDD_v4.0.md` §4.4、§5.2、M3；GitHub 复用调研见 `docs/research/SC021-family-knowledge-reuse-research.md`。

## 意图

让玩家能安排一名家族成员研究技艺，把完成的研究保存为跨代的家族知识。家族保存了知识，不代表每位家人自动学会或能使用该技艺。

## 当前基础

- SC-019 已有产业任命、资金/物资/情报及事务式月结。
- SC-020 已有成年判断、人物能力、年度培养、存档迁移与教育编年史。
- 月结入口 `GameSession.TryAdvanceToNextMonth` 按“产业→研究→教育→事件排程→日期/提交”执行，并显式回滚产业、研究、成长、事件态、随机态和编年史。
- 存档以纯数据与稳定 ID 表示 `industry`、`development`；UI 和 FoundationScene 通过事务式 Editor 生成器接入。
- 当前没有家族研究、已记录技艺或个人技艺掌握领域模型。

## 目标

1. 允许玩家选择一项可研究技艺并指定一名存活、未隐居的成年家族研究者。
2. 每次成功月结时确定性推进研究；资金或物资不足时暂停研究，不扣成负数、不消耗部分进度。
3. 完成后将技艺写入家族知识记录，并追加一条可读编年史，显示技艺名、研究者、完成月份和研究前后进度。
4. 通过存档、读档和继承保留家族知识；新家主不因此自动获得个人掌握状态。
5. 给玩家一个固定、可重复的 SC-021 同项目试玩入口和可操作的研究面板。

## 非目标

- 不实现战斗/任务中实际施放或装备技艺；个人掌握与师徒教授留待后续切片。
- 不实现大型分支科技树、可视化节点编辑器、无限内容作者工具、研究随机事件或多人协作。
- 不实现婚姻、收养、隐居/摄政扩展，不更改 SC-010、SC-013、SC-019、SC-020 已有剧本规则。
- 不安装 Unity package，不复制外部仓库代码，不创建 Unity project 或新 Scene。

## 构建方式

### 方案 A：定制纯 C# 研究模型（推荐）

沿用稳定 ID、纯 C# Domain、JSON SaveData、`TryAdvanceToNextMonth` 事务结算、HistoryLedger 与现有 uGUI 生成器。初始切片只放一项原创技艺，月度推进固定、可复现。与现有项目边界吻合，改动集中；后续扩充内容需自行设计数据契约。

### 方案 B：接入外部通用技能树

MIT 且支持 Unity 6 的仓库提供节点前置关系、图编辑器或 ScriptableObject 保存。可节省未来大量节点的 authoring 工作，但其角色技能点、Singleton/Unity 资产模式不包含家族知识与个人掌握的分离，也无法复用本项目的事务月结与存档规则；当前会增加集成和许可维护面。

### 方案 C：仅由事件解锁知识

把研究写为事件选项，不新增持续研究计划或月度状态。改动最少，但无法满足玩家“安排研究计划”的方向，也无法表现长期投入与资源取舍。

**推荐方案 A。** 两个开源候选及其许可证、Unity 兼容性、维护证据、依赖观察和决定见复用调研报告；不安装或复制它们。

## 玩法提案（lead 选择，供用户修订）

- SC-021 首个示范项目为一项原创家族技艺「烟幕调制术」，研究需 2 点进度。
- 每名家族最多同时进行一个项目；项目可随时更换研究者，已取得进度保留。
- 合资格研究者为存活、未隐居且成年（16 岁及以上）的当前家族成员。研究者不能同时担任产业职位，避免月度工作预算重复使用。
- 每次月结先结算产业，再结算研究；有效任命每月消耗 1 资金与 1 物资，并增加 1 点进度。资源不足时该月研究停滞但不阻止月份推进。完成后立即记录为家族知识。
- 研究者之后若失去资格（死亡、退休、未成年、离开当前家族或担任产业职位），计划保留为暂停状态并可正常存读档；月结不扣资源、不推进、不授予知识，玩家可更换研究者。
- 研究是确定性的，不使用随机流。完成知识在继承后保留；人物个人掌握表初始为空且不由知识记录自动填充。
- 默认 SaveData 迁移升为下一版，旧档导入空研究计划/空知识集，不补算历史月份。任何失败都回滚研究、产业、教育、事件态、日期、随机态和编年史。
- 单个固定示范剧本提供足以在两次月结内完成研究的初始资源，且不改变 SC-010 标准样例、SC-010 危机示范或 SC-020 教育样例。

## 验收目标

- Domain 测试覆盖开始/替换研究者、无效/隐居/未成年研究者、确定进度、资源短缺停滞、完成一次、重复月结保护、继承保留知识且不授予个人掌握。
- GameSession 测试证明研究与产业、教育、事件排程处于同一月结事务；注入结算异常后可完整回滚并重试。
- Save/Load 测试验证新状态与旧版空状态迁移，稳定 ID、研究进度和完成知识往返一致。
- EditMode 测试和 `FoundationSceneValidator` 验证面板绑定与布局；Unity 编译、测试、Console、Play Mode 分开报告。
- SC-021 DEMO 给出纯文本逐步 Play Test；由用户执行并报告。验证通过的切片按项目规则提交并推送到 feature 分支；用户正式验收前不合并 `main`、不打 tag。

## 项目约束

- Unity 6.6.6f1；保持现有项目，不新建 project/Scene，不安装 package。
- 代码由 OpenCode2 在当前任务 worktree 实现；场景由一名集成 worker 单独编辑；完成后由独立 OpenCode worker 只读审查。
- 保护当前工作区全部未提交文件及既有 `.meta` GUID；不改 `Library` / `Temp`；不运行针对已打开项目的 batchmode。
- 本设计已获用户确认并授权实现；实现与验收证据见 `SC021_DEMO.md` 与 `docs/reports/sc021-implementation.md`。


---

<a id="2026-11-04-sc-022-family-formation-design"></a>
## 来源：docs/superpowers/specs/2026-11-04-sc-022-family-formation-design.md

# SC-022 婚姻与日常收养设计

**状态：** 用户已确认设计，并批准进入实现计划  
**日期：** 2026-11-04  
**设计依据：** `GDD_v4.0.md`，M3「婚姻与收养」

## 意图

在现有家族经营框架中加入一个家族管理面板，让玩家为家族成员建立正式婚姻，或办理普通收养。婚姻与收养会修改真实的家族成员身份和亲属图，并经存档与编年史保留。普通收养只建立关系，不触发继承危机交接。

## 当前实现

- `FamilyState` 已能为成员记录 `Spouse`、`AdoptedChild` 等角色。
- `SuccessionWorld` 同时保存人物、生命状态、当前家族和 `KinshipGraph`；可以注册暂未加入本家的候选人物。
- `KinshipGraph` 已支持对称配偶关系和有向收养关系。它是追加式结构，不提供关系删除。
- `SaveData` 已捕获与恢复人物、成员和亲属边，因此本切片原则上不需要增加存档字段或升版本。
- `SuccessionService.AcceptCrisisAdoption` 是一次性的危机决策，会建立成年人并立即交接家督；普通收养必须与它分开，不能修改继承阶段、家主、继承次数或危机标记。
- 现有 `FamilyRosterPanel` 是只读展示；婚姻/收养编辑能力放在独立管理面板中。

## 目标

- 玩家能从家族管理面板选择合资格家族成员和外部候选人，确认婚姻或收养。
- 婚姻把候选人加入当前家族并写入 `FamilyMemberRole.Spouse` 与双向语义的 `KinshipType.Spouse` 结构事实。
- 正式配偶不设人数上限；同一对人物不能重复登记婚姻。
- 收养把候选人加入当前家族并写入 `FamilyMemberRole.AdoptedChild` 与有向 `KinshipType.Adoption` 关系；既有血缘事实保持不变。
- 操作是原子性的：失败时不能留下半条成员记录、亲属边、编年史或继承状态变化。
- 婚姻和收养生成可读的家族编年史；不消耗月度资源、时间预算或 RNG。
- 关系和成员状态通过现有存档模型存读往返一致。

## 非目标

- 离婚、关系解除、妾室/婚姻等级、多配偶人数限制或配偶竞争规则。
- 恋爱、好感、婚姻外交、嫁妆、子女出生、继亲或跨家族政治。
- 常驻世界人口模拟、跨家族成员迁移或多人家庭系统。
- 普通收养自动改变家主、指定继承人或摄政状态。
- 改写现有 SC-010、SC-013、SC-019、SC-020、SC-021 固定剧本。
- Play Test；由用户后续执行。

## 方案比较

1. **扩展现有事件效果。** 复用事件选项，但事件管线目前只处理数值关系变化；新增成员与亲属边需要扩展事件 JSON、校验、回滚、运行时提交和 UI 结果，范围最大。
2. **家族管理面板（选定）。** 在一个显式面板中挑选关系发起人和候选人，并在确认后调用领域命令。它复用 `FamilyState`、`SuccessionWorld`、`KinshipGraph` 和现有存档，永久变化在提交前清楚可见。**用户选择此方案。**
3. **固定脚本按钮。** 最快，但把婚姻和收养写死在单一示范路线中，难以复用，不适合作为家族经营接口。

## 设计决定

### 示范剧本与候选人

- 新增独立 SC-022 固定示范剧本和标题入口，避免改变已有剧本；复用当前 `FoundationScene`，不新建 Unity 项目或场景。
- 剧本内把候选人注册到 `SuccessionWorld.Persons`，但不加入 `FamilyState`。因此候选人也通过现有存档模型持久化；建立关系后，成员资格会使其从候选列表消失。
- 候选人物使用原创姓名和稳定 ID。剧本至少提供两名成年婚姻候选人，以证明同一人物可拥有多位正式配偶，并提供一名收养候选人。
- 这是一项本设计的实现范围选择：SC-022 先验证当前家族内的成员与非成员候选人，不建立完整的其他家族名册。

### 婚姻

- 面板允许选择一名在世、达到家族成年年龄的当前家族成员作为关系发起人，以及一名在世、已注册、尚非当前家族成员的成年候选人。
- 发起人可以已经有正式配偶；系统允许新增其他配偶。UI 显示其现有配偶，避免误以为一夫一妻。
- 不做性别筛选；当前人物模型没有性别字段，GDD 也未要求性别规则。
- 确认后将候选人加入当前家族，角色设为 `Spouse`，并为双方追加一条 `KinshipType.Spouse` 事实。既有关系不删除或改写。
- 拒绝空 ID、未注册/已入本家的候选人、死亡/未成年人物、自身关系、重复配偶边，以及与关系发起人存在直系血亲或直系养亲关系的候选人。兄弟姐妹关系不额外禁止；多位正式配偶仍允许。

### 普通收养

- 面板允许选择一名在世、达到家族成年年龄的当前家族成员作为收养者，以及一名在世、已注册、尚非当前家族成员的候选人。
- 收养候选人可以是儿童或成年人；既有 `ParentChild` 生物学事实保留。拒绝收养者本人、其正式配偶，以及会使 `ParentChild`/`Adoption` 有向亲子链形成循环的候选人；同一方向已存在的收养边也拒绝重复。候选人成为 `AdoptedChild` 并可按现行继承规则参与候选资格判断。
- 确认后只增加成员身份和 `KinshipType.Adoption` 边；不转移家督、不改变继承阶段、指定继承人或危机收养标记。
- 同一人物加入家庭后不再作为普通收养候选人。不能收养已经属于当前家庭的成员。

### 面板与操作反馈

- 增加独立 `FamilyFormationPanel`，从游戏 HUD 的「婚姻与收养」按钮打开，默认关闭；保持 `FamilyRosterPanel` 只读。
- 面板分为「婚姻」与「收养」两项操作：选择家庭成员、查看合资格的非成员候选人、查看现有关系、确认结果。
- 正式关系写入不可撤销；确认步骤明确说明将新增的成员与关系。失败时显示具体原因，不留下部分变更；成功后刷新名册/家谱/编年史。
- 编年史新增可读的家族关系类别或使用等价的既有类别；标题和摘要显示人物姓名、关系类型与结果，不显示原始 ID。

### 原子提交与存档

- 在修改之前完成候选资格、关系合法性、重复边、成员角色、日期和编年史条目的完整校验。利用 `KinshipGraph` 追加式语义，保证所有可能失败的校验发生在追加关系边之前。
- 不调用月结，不扣资源，不推进人物年龄，不消耗 RNG。
- `SuccessionSaveData` 已保存所有注册人物、生命状态、家庭成员和亲属边；实现应以测试证明 SC-022 的候选、婚姻和收养往返一致。若实现发现现有结构不足，先回报设计差异，不擅自加入存档升级。

## 验收目标

- 领域/会话测试：成功登记婚姻、同一人可登记第二位配偶、重复边和直系血亲/养亲候选被拒、兄弟姐妹关系不额外阻止登记、非成员候选成为 `Spouse`；失败时状态不变。
- 领域/会话测试：普通收养生成正确方向的关系边和 `AdoptedChild` 身份，不触发继承交接；既有生物学边保留；本人、配偶、会造成亲子链循环的候选及重复边被拒；失败时状态不变。
- 编年史与存档测试：成功关系只追加一次可读记录；保存/读取后人物、成员角色、配偶/收养边与历史完全一致。
- UI EditMode 测试：面板可开关、候选列表正确、确认按钮只提交一次、空候选和拒绝条件可读。
- 场景集成使用事务式 Editor 生成器和 `FoundationSceneValidator`；Unity 编译、EditMode、Console、场景 Validator 与 Play Mode 证据分别记录。用户 Play Test 暂缓，后续由用户执行。

## 约束

- 只在现有项目和当前 SC-018 feature 工作区开发；全部代码由 OpenCode2 实施。
- 不安装 Unity package、不另建项目/场景、不改 `Library` 或 `Temp`，保留 `.meta` GUID 与既有脏工作。
- 不复制商业游戏内容；示范人物、名称与文案均为原创。
- 不操作 Unity GUI、截图或 Play Mode；用户明确要求后再进行对应验证。

## 留给实现者的选择

- 在现有 `GameSession`/`SuccessionService` 边界内选用最小、可测试的领域命令位置。
- 为非成员候选数据选择最小的只读投影；不能把所有非成员一概视为候选，需基于在世状态、年龄、既有关系与当前剧本名单筛选。
- 选择最清楚的编年史类别和面板两页排布；保持固定根、事务式生成、验证前保存与 Undo 回滚惯例。


---

<a id="2026-11-05-sc-023-seclusion-design"></a>
## 来源：docs/superpowers/specs/2026-11-05-sc-023-seclusion-design.md

# SC-023 隐居与后见人设计

**状态：** 用户要求继续开发后进入实现；实现已提交到功能分支，Unity 验证和用户 Play Test 待办  
**依据：** `GDD_v4.0.md` §4.2、§3.5、M3  只读调研：`docs/research/SC023-seclusion-reuse.md`

## 意图

让退休前任家主在交出家督后仍能通过指导下一代影响家族；隐居应代表另一种持续贡献，而不是从家族系统中消失。首切片复用现有年度教育结算，让玩家能观察和保存这项影响。

## 现状与推荐范围

继承交接已将旧家主设为退休的 `DirectKin`。教育计划已保存导师 ID，年度结算已有导师专长加成和结果编年史，但资格检查会拒绝所有退休者。SC-023 推荐放宽为：仍在世、成年、属于当前家族的退休前任家主可以指导家中未成年继承候选人；退休导师使用与在职导师相同的年度专长规则。

新增独立 SC-023 固定剧本和标题入口：玩家先将家督交给成年继承人，再为新家主的孩子安排年度培养并选择隐居的前任家主为导师，完成一次年度结算后查看成长结果及编年史。复用现有 `FoundationScene`、继承、教育面板、成长规则和存档，不创建新项目或场景。

## 目标

- 退休前任家主仍是永久人物和当前家族成员，且不重新获得家主/继承候选资格。
- 玩家能将其选为教育导师；预览明确显示导师身份、年度方向、专长加成和预计前后值。
- 年度结算按既有确定性规则计算导师贡献，并在教育结果与编年史中显示导师姓名。
- 退休导师的 ID 可随年度计划保存/读取；计划若因导师死亡、离族或其他资格失效，沿用现有无有效导师时的自由成长回退。
- SC-020 原有开局、导师资格和培养结果不变。

## 非目标

- 不实现退休的再次任命、复职、离族、死亡或人口模拟。
- 不实现独立“后见人”职位、双重权威冲突、政治影响力数值或资源成本。
- 不改变摄政规则、婚姻/收养、研究、产业、人物属性上限或既有剧本。
- 不增加 Unity 包、第三方代码/资产、存档字段或场景。

## 方案比较

1. **允许退休前任家主担任年度导师（推荐）**：复用已有教育计划、导师专长、确定性年结、存档与编年史；玩法明确、可试玩，范围集中。
2. **建立长期顾问/后见人职位**：更贴近完整隐居制度，但需新职位状态、权限、持续效果与双重权威事件，超出首切片。
3. **只做退休家主的事件对白**：实现较轻，但不能验证退休者的持续家族贡献，也不复用已有教育系统。

选择方案 1；当前代码已有退休生命周期和年度导师计划，外部调研没有发现适合直接复用的 Unity 方案。具体将 SC-023 的教育场景接入现有应用边界由实现计划约束，避免改变 SC-020。

## 规则

- 退休导师必须满足：已登记在当前家庭、仍存活、已成年、`Retired == true`、家庭角色为 `DirectKin`，且不是培养对象本人；并且现有继承编年史必须能证明该人物曾以旧家主身份参与真实交接（`FromHeadId`）。仅“退休的直系亲族”不足以成为导师。
- 在职导师继续使用现有资格规则；不得因 SC-023 扩大到无关退休角色。
- 退休导师的年度专长由 SC-023 固定剧本数据决定；无专长的导师不能提供专长加成。
- 选择导师或提交计划不立即结算属性、资源或编年史；年结仍是唯一成长提交点。
- 年结前导师失去资格时，年度培养按既有“无有效导师则自由成长”规则处理，并在结果/编年史中如实显示。
- SC-023 使用独立家族 ID、人物 ID 和起始快照。现有 `FoundationScene` 只新增标题入口或必要的场景绑定，不改写已有入口和场景根。

## 主要风险与边界

- `GameSession`、`EducationView` 和 `SaveData` 含有 SC-020 固定场景与“非退休导师”假设；接入 SC-023 时必须把身份/资格判断限定在该剧本，不改变 SC-020。
- 存档校验、计划预览与年结必须共用同一导师资格规则，避免 UI 可选但读档/结算拒绝；退休资格的前任家主证明来自既有继承编年史，不新增存档字段。退休但非前任家主的 `DirectKin` 不得出现在导师列表，也不得通过存档计划校验。
- 只读审查不能替代 Unity 编译、EditMode、场景验证、Console 或用户 Play Test；每项证据需分别报告。用户 Play Test 仍由用户执行。


---

<a id="2026-11-05-sc-024-family-creed-ambition-design"></a>
## 来源：docs/superpowers/specs/2026-11-05-sc-024-family-creed-ambition-design.md

# SC-024 家训与个人抱负设计稿

**状态：** 用户已批准实施（2026-11-05）
**依据：** `GDD_v4.0.md` §5.4、§11.3、§12.2、§13 M3
**当前缺口：** 摄政已有领域流程、存档和测试；项目中尚未发现家训或个人抱负系统。

## 意图

补上 M3 尚未实现的家族长期方向：家训跨世代延续，每代家主各有个人抱负。让玩家能在现有家庭经营循环中看到家训对事件决策的影响，并观察一代家主的抱负如何通过产业、研究或教育推进。

## 目标

- 家族有一个持久家训，换代后继续沿用；试玩版本提供三种家训：守秘为先、义理至上、唯才是举。
- 当前家主有一个个人抱负；抱负绑定人物 ID，继位后由新家主另选，旧家主记录保留。
- 三种抱负分别连接现有产业、家族研究、子女教育，不引入尚未实现的 M4 势力或经济系统。
- 一次家族议事事件展示当前家训并允许维持或改革；改革会产生可见的家庭关系代价并进入编年史。
- 面板可查看当前家训、家主抱负及其进度；现有存档能保存并恢复这些状态。
- 在现有 `FoundationScene` 接入试玩入口和管理面板；不创建新 Unity project 或 worktree。

## 非目标

- 完整派系、忠诚度或家族投票系统。
- GDD 例子中的全部家训和抱负；此切片只使用三种家训、三种抱负。
- 新增产业、市场、外交、联盟或战术玩法。
- 改变已有 SC-010 至 SC-023 的规则；SC-020/SC-023 教育资格仍保持剧本隔离。

## 考虑过的做法

| 做法 | 取舍 |
|---|---|
| 只添加家训/抱负面板和存档字段 | 实现较快，但家训不会影响事件，抱负也不会改变玩家决策，不满足 GDD 的核心作用。 |
| **家训、改革事件与每代抱负做成一个纵向切片（推荐）** | 覆盖持久家族选择、事件后果和现有经营目标；需要一次存档迁移和场景接入，规模适合单个 Play Test。 |
| 实现完整家族派系与政治冲突模拟 | 可扩展性高，但 GDD 没有给出足以实现的派系规则，也超出 M3 第一版范围。 |

推荐第二种：用已存在的产业、研究、教育系统承载抱负进度，并用一场确定性的家族议事事件验证家训改革，不先造通用派系模拟。

## 提案规则

### 固定试玩入口

- SC-024 标题入口复用 `M2FixedScenario` 的六人家族、起始日期和现有经营状态，不复制另一套人物或亲属图。
- 新局默认家训为 `义理至上`；当前家主 `person.sc013.harunobu` 的默认抱负为 `发展家业`。
- 议事事件使用现有 SC-013 家族成员 `person.sc013.masatsuna` 与 `person.sc013.nobumasa` 作为关系变化对象。

### 家训

- 可选项采用 GDD 的三种原型额度：`守秘为先`、`义理至上`、`唯才是举`。
- 家训属于家族状态，交接后不重置。
- 固定议事事件根据当前家训提供一项对应的议事方式，以及改为其他两种家训的改革选项：

  | 当前家训 | 议事方式 | 正纲关系变化 | 信正关系变化 |
  |---|---|---:|---:|
  | 守秘为先 | 小范围议事 | +5 | -5 |
  | 义理至上 | 各房先开口 | +5 | +5 |
  | 唯才是举 | 按所长分工 | -5 | +5 |

- 议事方式保留当前家训；改革至任一其他家训时，两名议事成员对当前家主的关系各 -5。家训变更、关系变化和编年史必须作为一次原子提交。
- 只有当前家训对应的议事方式可选；改革后旧家训方式不能提交。家族方针面板展示当前家训及其选择效果。
- 首切片只展示一次议事，不允许通过面板绕过事件直接改值；不新增隐含支持率或随机检定。

### 个人抱负

- 每任家主从三项中选一项：`发展家业`（产业）、`传承家技`（家族研究）、`培养继承人`（教育）。
- `发展家业`：成功月结中至少一条产业为 `Operated` 且三类资源净变化之和大于 0 时进度 +1；累计 3 个月完成。
- `传承家技`：选择抱负时记录已知技艺集合；此后每新完成并记录一项家族技艺，进度 +1；完成目标为 1 项。研究仍由 `FamilyResearchSettlement` 唯一结算。
- `培养继承人`：当前指定继承人的成功年度教育结果中，五项能力正向变化量之和计入进度；累计 +5 完成。教育年结只计一次。
- 完成抱负不额外授予资源或能力，只记录带人物姓名和目标/进度的编年史结果；这是本切片的平衡边界。
- 抱负只属于当前家主；合法继承完成后，家族保留既有家训和旧家主抱负历史，新家主在面板指定自己的抱负。
- 面板显示目标、当前进度、完成状态和对应编年史；读档后完全恢复。

### 存档与场景

- 家训及每代抱负使用稳定 ID 持久化；SC-024 活动事件路线也要随存档恢复，保证议事后的存档仍按 SC-024 条件继续排程。新增字段时按项目存档迁移规范升级 schema，并验证既有 v8 存档迁移为默认家训、无抱负和非 SC-024 路线。
- 复用既有家族、产业、研究、教育和编年史模型，不在 `FamilyState` 与 `GameSession` 中维护重复事实。
- 在 `FoundationScene` 使用一个固定 UI 根和事务式 Editor 生成器；单 Undo 组、保存前验证、异常时 Undo 回滚。
- 新场景入口使用 SC-024 标识；不覆盖 SC-023 演示入口。

## 验收

- 三种家训及三种抱负显示清楚；非法/重复状态不能提交。
- 家训议事提供维持/改革选项；维持为两名成员关系各 +5，改革为各 -5；家训、关系前后值和编年史在一次提交中一致。
- 家训条件事件只允许匹配家训的选项，旧家训选项在改革后不可提交。
- 产业、研究、教育只推进对应抱负；未运营产业、选择前已知技艺、非指定继承人的教育、无正向教育成长和其他系统结果不会误推进。达到目标后只完成一次。
- 继位后新家主抱负独立，家训保持。
- 议事后 v9 存读档往返恢复家训、各代抱负、事件记录和 SC-024 路线；v8 存档迁移、失败回滚及既有 SC-019 至 SC-023 回归通过。
- Unity 编译、EditMode、Console、Validator、Play Mode 分开记录；Play Mode 覆盖改革分支、三类抱负至少两代交接、月结和存档。
- 用户完成 Play Test 并明确验收后，才创建里程碑 checkpoint、合并或打 tag。

## 留给实现者

- 在现有事件目录中确认最合适的议事事件挂接位置与稳定 ID。
- 选择轻量、确定性的抱负进度表示；不得另做一套月结模拟。
- 以现有面板、存档、编年史和 Editor 生成器模式实现，并为每项失败边界写回归测试。


---

<a id="2026-11-07-m03-npc-autonomy-design"></a>
## 来源：docs/superpowers/specs/2026-11-07-m03-npc-autonomy-design.md

# M03 高自主 NPC 与只读 Inspector 设计稿

**状态：** 用户已确认首片范围（2026-11-07）；实现计划待确认
**依据：** `GDD_v4.0.md` §13 M03、§19；复用现有 M02 `DailyActionState` / `GameSession`
**当前缺口：** 有确定性、可存档的每日行动和固定人物名册；尚无 NPC 自主目标评分、执行器、决策记录或 AI Inspector。

## 意图

让少量固定剧本家族成员根据简单需求自主选择并完成日常行动，同时可在只读 Inspector 中看清候选、分数、排除原因和行动状态。复用 M02 时间线，验证稳定且可解释的决策闭环。

## 目标

- 只让固定剧本的正纲（`M2FixedScenario.MasatsunaId`）与信正（`M2FixedScenario.NobumasaId`）参与自主决策；其他人物仍由既有控制流程处理。
- 两人使用小型、明确的需求值和固定人格偏好，为现有休息、训练、工作行动评分。
- 只有空闲且通过资格、地点、时间和资源检查的候选才参与评分；非法候选显示排除原因。
- 选择并安排行动后，沿用 `GameSession` 与 `DailyActionState` 推进、存档和恢复；行动中持续执行，不每日重新评分。
- 在行动完成、被中断或相关资格改变时重新规划，并记录触发原因。
- Inspector 显示每人的需求/偏好、当前有限状态、目标行动、所有候选分数与排除理由、最近决策记录。
- Inspector 无修改模拟状态的控件；M02 玩家行动面板仍按现有授权允许人工安排或中断行动。

## 非目标

- 动态生成或扩展人物、家谱和人物人格编辑器。
- GOAP、HTN、行为树、随机决策、帧驱动模拟或多步长期计划。
- 新经济、属性奖励、资源消耗、日程队列、地点寻路或新行动种类。
- M04 人物经历记忆；M03 仅记录本局内最近的决策和重规划原因，不新增持久化日志格式。
- Inspector 内调试编辑、暂停/单步工具或直接更改需求、目标与行动的入口。

## 提案规则

### 需求与人格

- 每位试点人物维护 0–100 的休息、学艺、家务三项需求。每日推进时，未被满足的需求按固定规则增加；对应行动完成后降低对应需求。数值封顶/保底于 0–100。需求值属于模拟状态，随存档保存；本局内最近决策日志不保存。
- 正纲与信正使用固定且不同的偏好权重；这些权重只影响候选排序，不改变现有属性、关系或经济。
- Utility 分数由行动匹配需求、人格偏好和行动适用性组成，纯整数计算；同分时按 `DailyActionKind` 稳定枚举顺序择一。无随机数、浮点权重或依赖集合枚举顺序。
- 精确增长率、权重、行动时长在实现计划阶段列为常量表，并由 EditMode 测试锁定；只调这两人的试玩节奏，不暴露通用编辑配置。

### 候选与执行

- 候选行动只限休息、训练、工作。旅行不纳入首片。
- 先检查人物是否属于试点名册、是否在已知地点、是否已有任务，以及行动持续时间/行动前置条件是否合法；失败候选保留在诊断输出中并附具体原因，不参与评分。
- NPC 空闲时按分数从高到低择一，通过现有 `GameSession.TryPlanDailyAction` 安排；只有成功安排才记为接受。
- 有任务时不重新评估或替换。玩家手动安排/中断仍走现有 `GameSession` API；AI 不覆盖玩家的有效任务。
- 决策命令结果为：成功排程=接受；因现有行动或短期条件不满足=延期并说明下一次检查时机；无合法候选/人物未登记=拒绝；现有行动被玩家中断时记录中断原因。拒绝不得改变原行动状态。
- 规划触发仅限首次初始化、行动完成/中断、或明确的资格变化；普通日期变化只更新需求，不打断正在执行的任务。

### 只读 Inspector

- 在 `FoundationScene` 增加一个固定 M03 入口与只读面板，沿用现有 Unity UI 和事务式场景生成器约定。
- 两人分别显示当前需求、人格偏好摘要、执行状态（空闲/执行中/已完成/被中断）、当前行动与剩余时间。
- 可展开最近一次评估：列出三个候选的资格结果、整数分数、排序和选择原因；被排除项显示原因。
- 显示有限数量的最近决策日志，至少包含日期、人物、结果、行动和重规划触发原因；不要求跨存档保留历史日志。
- Inspector 只订阅并展示领域/会话状态；所有控件仅用于选择人物、展开信息和关闭面板，不调用写状态 API。

## 验收

- 两名试点人物以相同初始存档和推进输入重复模拟时，候选集、分数、选择、时长与日志顺序完全一致。
- 两人因不同需求/偏好完成不同基础行动；通过可控测试状态覆盖休息、训练、工作三种选择。
- 无合法候选时显示排除/拒绝原因且不改变既有行动；活动任务持续执行，不被每日评分重置。
- 行动完成或被玩家中断后能按规则重新评估；日志指出实际触发原因。
- 存档/读档后进行中的 M02 行动不重复、不丢失；恢复后自主行为继续，Inspector 状态与会话一致。
- Inspector 显示所有候选的分数或排除原因，且 UI 无任何可修改模拟状态的操作。
- 编译、EditMode、场景 Validator、Console、Play Mode 分开记录；实际打开 `FoundationScene` Play Test 中验证两人的行动推进与 Inspector 可解释性。
- M03 最终里程碑仍须用户明确验收后才可创建 checkpoint、合并或打 tag。

## 实施前置

- 实施前完成 GDD/项目内模式复核和至多三个 Unity 6 兼容 GitHub 先例的轻量研究；比较许可证、维护状态、依赖和接入成本。没有明显优于现有纯 C# 模式的依赖时沿用项目代码，不安装包、不复制外部代码。
- 既有 M02 领域、应用、存档、UI 和测试文件是复用入口；实现计划需明确更新到的调用者、存档/迁移边界、场景入口和 EditMode/Play Mode 证据。
- 用户批准本设计后再写实现计划；用户批准计划后由 OpenCode2 在现有工作区实施。


---

<a id="2026-11-07-m04a-personal-memories-design"></a>
## 来源：docs/superpowers/specs/2026-11-07-m04a-personal-memories-design.md

# M04A 个人行动记忆设计稿

**状态：** 用户已批准进入实现计划；记忆规则按本稿提案执行
**依据：** `GDD_v4.0.md` §13 M04；复用 M03 `NpcAutonomy`、`GameSession`、v11 存档和只读 Inspector
**当前缺口：** 两名 NPC 已按稳定整数规则自主选择休息、训练或工作，Inspector 可解释候选分数；经历记录仅保存在本局有界决策日志中，不能影响之后的选择。

## 意图

先完成 M04 的记忆纵向切片：让两名现有试点 NPC 记住近期行动经历，并让这些记忆对之后的自主选择产生可见、可解释、确定性的影响。M04A 不改变家族名册与亲属关系。

## 目标

- 仅为正纲和信正记录行动记忆；记忆以经历者的稳定 `PersonId` 归属，不随家主/继承变化转移。
- 记忆由真实的每日行动完成或中断结果产生，玩家安排和 NPC 自主安排都按实际结果处理。
- 记忆参与现有整数 Utility 分数，并在候选解释中显示基础分、记忆修正和最终分。
- Inspector 显示最多两条记忆（最近一次完成、最近一次中断）及其来源日期、行动和当前修正；仅展示，不允许编辑。
- 存档能保留记忆及其人物归属；旧版迁移创建空记忆，不从历史日志推测或补造经历。

## 非目标

- 动态生成/成长家庭成员、婚姻、收养、离家、自主委托或继承规则变化。
- LLM、向量检索、随机偏好、自由文本生成或通用记忆编辑器。
- 将记忆当作亲属事实、关系值、编年史或世界当前状态的替代数据源。
- 改造 M03 的行动种类、资格判断、当前任务执行、UI 写操作或既有 SC-024/M01 内容。

## 方案比较

1. **外部通用 NPC 记忆框架：** 可提供检索或对话记忆，但引入额外依赖，且不匹配本项目的确定性存档和整数评分。
2. **从家族编年史临时推算偏好：** 不新增存档结构，但把叙事记录和模拟规则混为一谈，历史数量也不受本切片控制。
3. **在现有自主状态中保存两个有界行动记忆，并给候选评分加固定修正（推荐）：** 复用现有 `PersonId`、`GameDate`、行动结果、整数评分与 Inspector，数据量与行为效果都容易验证。

Scout 的 GitHub 只读筛查未发现适合直接复用的实现：`qyhanyu/unity-smart-npc-sdk` 为 MIT，但使用浮点/时间衰减并带付费 LLM 网关；`TristinOrg/unity-memory-npc-prototype` 未发现许可证且聚焦对话；`xbowen125-commits/Spatial_AI_NPC` 虽为 MIT，但依赖 Python、OpenCV、MediaPipe、Whisper/LLM。决定沿用项目现有纯 C# 模式，不安装或复制代码。详见 Orca 只读研究任务 `task_a227de3656f9`。

## 提案规则

- 每名试点人物最多保存两条：最近一次已完成行动、最近一次被中断行动。新结果替换同类旧记录，避免记忆无界增长。
- 每条记忆保存人物 ID、记忆类型、行动类型、发生日期；中断记忆另保存现有结果中的简短原因。记忆文本由类型与行动生成，不保存自由文本。
- 完成记忆在发生后 30 个游戏日内，为同类候选行动增加 12 分；中断记忆在同一 30 日窗口内减少 12 分。超期记忆仍保留并可查看，但不影响评分。边界按发生日计入第 30 天。
- 最终分数为既有 `需求 × 人物偏好` 加上所有当前有效记忆修正；同分仍沿用既有稳定枚举顺序。每个分数说明逐项列出记忆修正。
- 完成/中断结果只在实际结算成功后写入记忆；失败或被事务回滚的行动不产生记忆。内存状态与每日行动一起参与现有事务快照/回滚。
- 持久化版本从 v11 升至 v12。v11 迁移仅添加空记忆集合；v12 校验人物引用、枚举、日期、同类重复项、最多两条及稳定排序；拒绝不合法记录，不静默丢弃。
- 记忆只记录个人对行动经历的回忆，不改变关系值、编年史内容、家族身份或其他人物状态。

## 影响范围

- Domain：有界的记忆记录、状态克隆、确定性修正计算与分数说明。
- Application：在每日行动完成/中断的事务路径中记录经历；评分读取该人物记忆；存读档映射记忆状态。
- Persistence：v12 DTO、保存校验与 v11→v12 迁移。
- Presentation：将记忆列表和分数分解呈现于现有只读滚动 Inspector；如现有文本区不足，只调整既有生成器布局，不新增场景根或交互入口。
- Tests/docs：覆盖记忆触发与事务回滚、30 日边界、两个试点隔离、确定性排序、保存/读取/迁移及 Inspector 只读显示，并记录各项独立验证证据。

## 验收

- 两名试点在完成或中断休息、训练、工作后各自得到正确记忆；失败或回滚不留下记忆。
- 有效记忆只修正对应候选分数，Inspector 同时说明基础分、修正、最终分和记忆来源。
- 第 30 天仍生效，第 31 天不再影响选择；过期记忆仍可在 Inspector 查看。
- 同一初始存档与输入产生相同记忆、候选分数和选择；两个试点之间不串写。
- 保存/读取保留归属与日期；v11 迁移为空记忆；非法 v12 数据按保存验证规则拒绝。
- 编译、EditMode、场景 Validator、Console 与 Play Mode 分别记录；Play Mode 验证记忆出现、分数解释、过期边界和存读档。

## 实施前置

- 用户确认本稿的两类记忆、上限和 `+12/-12`、30 日效果后，才编写实现计划。
- 用户确认实现计划后交 OpenCode2 在当前 `main` 工作区实施；保持现有分支/工作区，不另建 Unity Project、branch 或 worktree。
- 完成实现与顺序只读复审后，Mochi 执行 Unity 验证和 Play Mode；验证通过的任务文件依项目规则提交并推送。M04 最终里程碑仍待用户明确验收。


---

<a id="2026-11-07-ui-character-action-design"></a>
## 来源：docs/superpowers/specs/2026-11-07-ui-character-action-design.md

# UI 设计图集：人物档案与每日行动首片

> **范围更新（2026-11-07）：** 用户随后明确要求五张参考图对应页面全部一比一复刻。本文件保留首片原始交付与历史验证记录；当前范围、设计约束和执行顺序以 [五张 UI 参考图设计稿](design-and-planning-history.md#2026-11-07-ui-reference-gallery-design) 和 [五页实现计划](design-and-planning-history.md#2026-11-07-ui-reference-gallery) 为准。

**状态：** 用户已将设计图集确认为设计稿并要求开始开发。本文件固定首个实现切片；视觉与交互方向已获确认，具体实现由 OpenCode2 在现有工作区完成。

## 意图

把《影之一族》当前的每日行动与 NPC 自主性展示，逐步靠近用户提供的《影之一族》UI 设计图集：宽屏 PC、和纸/墨色、深靛蓝与朱印的层级，同时让人物信息、可安排行动和开发调试信息容易区分。

设计来源：[ChatGPT Library：《影之一族》UI 设计图集](https://chatgpt.com/library/share/libfile_373db197a9bc8191985c0292264f6f58?account_id=personal)。设计图集中的姓名、数值、人物数量、地点和事件仅为视觉示例；游戏规则继续以 `GDD_v4.0.md` 为准。

## 首个切片

实现图集第 02 页“人物档案与每日行动”，沿用现有 Unity 6 uGUI、`DailyActionPanel`、`NpcAiInspectorPanel`、M02 行动状态与 M03 只读快照。不增加玩法规则或存档字段。

页面职责：

- 以当前选中人物为中心，显示项目现有数据支持的人物档案、关系和经历；信息仅来自实际领域/会话状态，不复制概念图数据。
- 在同一页面安排并查看休息、训练、工作和旅行，保留 M02 的持续时间、进度、目的地、反馈、中断与重规划语义。
- 将 NPC AI Inspector 作为开发调试视图显示；它保持只读并与玩家信息区明确区分，不把调试内容变成玩家功能。
- 复用现有头像/占位图、字体和 uGUI 组件；缺少素材时保留现有占位，不把概念图当游戏素材。

## 明确不做

- 本切片不重做图集中的另外四页：家族月度总览、剧情事件、继承与家谱、地图与旅行规划。
- 不新增第三方包、外部资产、角色属性、关系规则、任务逻辑或存档迁移。
- 不修改 M02/M03 领域行为；不把 AI Inspector 的操作权限扩展到模拟状态。

## 实现取舍

1. 一次性重做五个主要界面：覆盖面广，但会把尚待 Play Mode 验收的 M02/M03 与多套场景布局混在一起。
2. 在现有 uGUI 上先交付人物档案/每日行动首片：复用现有 M02/M03 数据和面板，最容易观察、验证并继续扩展；**采用此方案**。
3. 引入通用 UI 库或运行时检查器：Scout 评估后没有发现能显著减少本片自制代码的依赖；不采用。

## 约束与验收

- 留在唯一 Unity 工程与当前 `main` checkout；不创建 branch/worktree，不覆盖既有用户 WIP。
- 不新增依赖或素材；保留 `.meta` 文件和 GUID。若需要改场景，只由指定集成实现者写入，并沿用单一固定根、Undo group、保存前校验与失败回滚。
- 新增 EditMode 覆盖只读人物展示、面板选择联动与 Inspector 的只读边界；场景 Validator 检查绑定、容器范围、层级和面板间隔。
- Unity 验证使用当前项目 Editor，分别记录编译、EditMode、场景 Validator、Console、Play Mode 与用户验收；不得对被 Editor 占用的目录运行 batchmode。
- Play Mode 观察宽屏下人物信息可读，行动安排与进度反馈无重叠，关闭/切换人物正常，Inspector 只读且可明确隐藏；保存数据必须与测试前一致。
- 其余四个页面按后续切片规划，不据此宣称全套 UI 已实现。

## 后续路线映射

完成本首片后，再按项目 M## 路线与各自验收状态安排其余概念页；优先复用现有事件选择、继承/家谱、月结、世界地图面板，不新增平行规则系统。是否整合进主导航与各面板的最终窗口结构留给后续逐片设计。

## 交付记录（2026-11-07）

已交付范围：

- `DailyActionPanel` 把选中家族成员呈现为右侧“人物档案”焦点区，显示名册中的姓名、年龄、身份、世代、亲族关系，以及当前地点/旅途状态、可前往目的地和行动进度；所有内容在每次 `Refresh()` 时从 `GameSession.DailyActions` 与 `GameSession.FamilyRoster` 重新读取，选择、日期、行动变化后同步更新。
- 左侧保留原家族名册滚动列表与选中高亮；休息、训练、工作、旅行、持续天数、中断与重规划的提交路径和反馈语义未改。
- M02 事务生成器与场景 Validator 更新为左右两栏布局：名册列与档案列分离，档案文本区域为 400×370、至少 16 号字、自动换行并在超出时截断；所有控件保持在 1280×720 测试画布与面板边界内。生成对象仍全部位于 `M02DailyActionRoot` 下，沿用单一 Undo group、保存前校验和异常回滚。
- 新增/更新 EditMode 覆盖：档案来源与选择联动、日期/行动/旅途同步、1280×720 控件边界、生成器幂等与故障回滚。
- M03 `NpcAiInspectorPanel` 未修改：默认隐藏、可打开/关闭、试点人物选择与只读边界保持原样；Play Mode 已验证其与玩家档案区互不替代。

验证记录：

- 编译：当前 Editor（PID 91392，`-projectPath "C:\Users\panye\My project"`）重编译通过，无 `error CS`。
- EditMode：`M02DailyActionPanelTests` 与 `M02DailyActionTests` 共 23 项全部通过。
- 场景 Validator：M02 与 M03 校验 PASS；工程内既有的 SC-023/SC-024 标题项与事件选项布局错误为本次切片之外的历史问题，保持原状。
- Console：清空后重跑，0 error；1 条 warning 来自故障注入测试的预期回滚日志。
- Play Mode：在现有 Editor 内开始新游戏，打开每日行动面板，选择明智千恵（66 岁、配偶、第一代），档案显示亲族、清洲位置与行动进度；安排 2 天休息并推进一天后显示“进度 1/2（剩 1）”；M03 Inspector 默认关闭，打开并选择政纲后显示只读状态。保存槽文件哈希与测试前一致。

剩余工作：

- 尚未提供人物头像素材，本片按约束保留无头像的排版；后续若获得可用且授权明确的立绘，再单独接入。
- 图集其余四页（家族月度总览、剧情事件、继承与家谱、地图与旅行规划）不在本片范围。
- 本片仍需用户做最终视觉与交互验收；在用户明确接受前不合并、不打里程碑标签。


---

<a id="2026-11-07-ui-reference-gallery-design"></a>
## 来源：docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md

# 五张 UI 参考图的一比一功能复刻

**状态：** 用户已确认五张参考图全部纳入范围，并授权在 OpenCode2 账户余额不足时由项目负责人直接完成实现。目标是复刻页面构图、视觉层级、纸本/墨色材质、色彩、控件外观与相对位置，同时保留当前项目的真实数据和游戏操作。

## 目标

将设计图库中的五个 16:9 页面实现为《影之一族》的可交互 uGUI 屏幕：人物档案与日常行动、家谱与继承、家族内务、地图与出行、事件抉择。五页共享一致的家纹、深靛色边框、金色细节、和纸底纹、朱印强调与传统衬线字形；每页的导航和主内容分区跟随对应参考图。

## 参考图和页面职责

参考图均由用户提供，原始尺寸为 1668×941；Unity 目标画布以 1280×720 为基准并按相同比例缩放。

1. **人物档案与日常行动**：顶栏资源/日期，左侧主导航，档案/关系/经历标签，中部人物卡与关系网，右侧行动安排，最右只读 AI 调试栏。映射 `DailyActionPanel`、`CharacterDailyPageView` 与 `NpcAiInspectorPanel`。
2. **家谱与继承**：顶栏、左侧家族导航、中央按世代连线的家谱、右侧继承/家族信息与决策。映射 `FamilyRosterPanel`、`SuccessionPanel`、`FamilyTreeGraphBuilder`。
3. **家族内务**：顶栏、家主/日期侧栏、中央成员与任命表及本月事件、右侧天下动向和待决事项。只组合已存在的家族、产业、事件与月结数据；没有领域数据的字段不显示虚构值。
4. **地图与出行**：顶栏、左侧队伍/路线计划、中央地图及路线、右侧地方传闻/人物/委托。映射 `WorldMapController`、`GameSession` 的出行命令及已有委托/事件视图。
5. **事件抉择**：顶栏和左侧家族导航，中央事件插画/叙事/选项，右侧待处理事项。映射 `EventChoicePanel` 与现有事件目录、选项命令及反馈。

## 视觉验收

- 五个页面必须使用各自参考图的主构图与区域比例，不再保留当前平面蓝色 HUD 样式作为最终页面。
- 同一游戏数据在对应区域中以真实会话/领域状态渲染；截图中的姓名、数值、人物和事件只是视觉示例，不覆盖当前游戏数据。
- 页面上的现有命令仍可执行：人物选择、休息/训练/工作/旅行、日历推进、继承/摄政、家谱浏览、地图目的地/出行、事件选择、保存/结算等。
- 每个页面的按钮、面板和可滚动内容均需适配 1280×720 基准，不重叠、不越界；更宽画布保持比例和对齐。
- AI Inspector 保持独立、只读、开发者用途。

## 约束与非目标

- 不修改领域规则、存档结构、事件效果或随机结果；不新增第三方包或 ProjectSettings。
- 优先复用现有面板、数据快照和生成器；只新增共享 UI 主题/页面导航等跨屏必需组件。
- 仅将用户提供的设计图裁切为 UI 插画/纹理；不得从商业游戏复制资产。`ASSETS.md` 记录来源为用户提供的 ChatGPT Library UI 设计图及其裁切文件。
- 保留所有 `.meta` 和 GUID，场景改动仅通过单一固定根、单一 Undo 组、保存前验证和异常回滚完成。
- 保留当前已有的 M02 未提交 WIP并在其上继续；保留无关的 M04A 文档、`graphify-out/` 和 `nul`。

## 方法

按功能复用项目现有 UI，而非把整张截图铺成静态假界面。共享主题提供纸纹、色板、边框、字体与按钮样式；每个现有页面逐一映射其数据和命令，使用参考图裁切的装饰艺术做真实 uGUI 组成。每页独立更新场景生成器/绑定验证和 EditMode 覆盖，再整体核对五页导航与 Play Mode。

## 风险

参考图是合成的 UI 稿，含有仓库当前不存在的立绘/纹理和示例文案。可从用户提供的图中提取的装饰素材会被复用；不适合裁切的区域用项目内 uGUI 重建。若某个示例字段在当前会话中没有真实数据，该字段保持缺省/省略，不伪造数值。

## Runtime Chinese font

Runtime-created uGUI Chinese labels use Unity's built-in `LegacyRuntime.ttf`. The system dynamic-font path left the event empty-state labels invisible in this Canvas. This fallback does not change page data or event commands.

The shared full-page surface uses a deep indigo backdrop and an opaque indigo navigation rail, matching the dark frame in the supplied reference pages. The backdrop is a non-interactive Canvas sibling behind every page, so switching pages does not reveal the legacy blue scene background. These colors are presentation-only and do not affect game state.

## Map artwork integration — 2026-11-08

The map page uses the owner-provided central map crop as a non-raycast Image behind existing controls. It occupies the central content rectangle, while the existing route/travel interactions remain live. The crop is catalogued in ASSETS.md; this integration does not claim exact recreation of the reference's side panels, labels, route overlays, or scale decorations.


---

<a id="2026-11-08-start-screen-design"></a>
## 来源：docs/superpowers/specs/2026-11-08-start-screen-design.md

# 《影之一族》开始页面设计稿

状态（2026-11-08）：用户已批准设计；代码与原创素材已实现并推送至 `origin/main`（`220b44f`）。Unity 6000.6.6f1 编译完成，全量 EditMode 1343/1343 通过，PNG 已导入；用户授权后已保存接入 FoundationScene，场景验证 PASS（0 错误），Play Mode 已启动 SC-013 与 SC-010 标准样例。场景文件因混有既存 M02 WIP 暂未推送；1920×1080 已检查，1280×720 和无存档 Continue 路径待补。当前设计仍不改变存档格式或游戏规则。

## 目标

让玩家打开游戏时先进入有辨识度的家族世界，并能快速继续存档或开始新游戏。现有 SC 演示入口放入新游戏后的剧本选择页，不再挤在开始页。

## 现状与约束

- 现有启动页由 `TitleScreenController` 和 `AddM6TitleScreen` 生成，当前把新游戏、继续及多个 SC 演示入口放在同一层。
- 新游戏当前直接进入固定三代剧本；继续游戏读取现有单槽存档。
- 本设计保留继续游戏的存档读取行为。开始新游戏改为进入剧本选择，再由选择页启动已有剧本入口。
- 继续沿用项目的 uGUI、`SengokuUiTheme`、和纸、深靛蓝、金线与朱印视觉语言；不增加 Unity 包，不将整张概念图作为静态 UI。
- GDD 是玩法权威来源。旧 M1–M7 标题页文档只作历史记录。

## 方案比较

| 方案 | 结构 | 取舍 |
|---|---|---|
| 1. 沉浸式主菜单（选定） | 全屏原创家宅主视觉；首屏仅继续游戏、开始新游戏；示范入口在选剧本页 | 首屏清晰、有家族氛围；开始新游戏多一步选择 |
| 2. 主菜单兼演示目录 | 所有剧本入口仍在首屏，重新分组排版 | 入口直接，但容易拥挤，也弱化标题主视觉 |
| 3. 纯色底重排 | 保留纯色背景，只重排现有按钮 | 改动少，但难以达到参考图的质感与辨识度 |

已选择方案 1。

## 视觉方向

### 色彩

| 名称 | 色值 | 用途 |
|---|---|---|
| 墨夜 | `#101722` | 全屏暗部与阴影 |
| 深靛蓝 | `#18202E` | 左侧菜单底与主框架 |
| 羊皮纸 | `#EFE4CC` | 普通按钮、标题文字 |
| 古金 | `#C3A469` | 细边、家纹与焦点提示 |
| 朱印 | `#A5392B` | 主要焦点按钮与家族印记 |
| 苔玉 | `#5C7A4F` | 仅用于后续状态提示，不作大面积强调 |

### 字体与材质

- 标题采用书写感强的中文字标；正文和菜单沿用项目的中文宋/楷风格字体。运行时继续使用项目已有字体回退策略，不新增字体包。
- 菜单按钮为浅色和纸面、细金边、低圆角或直角；当前焦点使用朱印底与浅色字。
- 主视觉是战国家宅与山城黄昏场景，带少量灯火、枫叶和家纹。纹样只沿边缘出现，避免遮挡文本。

### 16:9 排布

```text
┌──────────────────────────────────────────────────────────────┐
│ 左侧约 1/3：深靛蓝织纹       │ 右侧约 2/3：家宅与山城主视觉    │
│                              │                                │
│ 家纹                         │          黄昏山城                │
│ 影之一族                     │                                │
│ 守护家名，传承百年（暂定）   │   家宅、灯火、枫叶、远山          │
│                              │                                │
│ [继续游戏]                   │                                │
│ [开始新游戏]                 │                                │
│                              │                                │
└──────────────────────────────────────────────────────────────┘
```

主视觉铺满背景；左侧菜单区用独立 uGUI 面板保证按钮清晰、可点击、可键盘聚焦。安全区按 1280×720 设计并保持宽高比适配。

### 概念稿

开始页面概念稿原文件 `docs/mockups/2026-11-08-start-screen-concept.png` 未包含在当前仓库归档中。

这是供审阅的 AI 概念合成图，不是生产 UI 素材。实现时应生成/绘制干净的场景背景素材，并用独立 uGUI 文本、按钮和家纹层组成真实界面；不直接铺整张概念图。

## 交互与文案

1. **继续游戏**：有效存档存在时可用，点击调用当前继续存档流程；无有效存档时禁用，显示现有存档状态提示，不改动槽文件或会话。
2. **开始新游戏**：打开“选择剧本”页，不直接初始化固定剧本。
3. **选择剧本**：集中展示目前分散在首屏的固定三代主线、SC-010 标准/危机入口及 SC-020–SC-024 专题示范；沿用各入口已有的剧本初始化逻辑。剧本页作为本次开始页设计的直接后续，不扩展其详情卡片或额外游戏规则。
4. 标语“守护家名，传承百年”为设计提案，可在审阅时替换。

## 非目标

- 不添加设置、退出游戏、联网、账号、主菜单动画或新存档槽。
- 不改变既有剧本内容、存档格式、单槽继续语义或游戏规则。
- 不重做已设计的五个游戏内页面。
- 不把概念合成图用作可交互界面或运行时整屏背景。

## 已确认的实现边界

- 用户已批准方案 1。实现前先批准独立的实现计划；实现交 OpenCode2，若其无法执行再由项目负责人接手。
- 背景使用独立原创场景图，标题、菜单、按钮及家纹使用现有资源或实时 uGUI 元素；按 `ASSETS.md` 记录新增素材来源与许可。


---

<a id="2026-11-08-town-overview-ui-design"></a>
## 来源：docs/superpowers/specs/2026-11-08-town-overview-ui-design.md

# 城镇总览界面设计

> 状态：城镇总览方向已按用户选择记录；用户补充要求清洲、热田、那古野各有辨识度（2026-11-08）。

## 意图

进入城镇后，玩家能快速看懂当前位置、当地人物、传闻和可处理委托，并从同一页面进入现有互动。城镇页采用与现有界面一致的藏青边框、和纸面板、朱红重点按钮与金色细节。

## 当前项目依据

- 现行设计以 `GDD_v4.0.md` 为准，其中明确暂不制作复杂村庄场景或大量美术。
- `WorldMapController` 已有城镇菜单入口、当前地点标题、返回地图行为和城镇 NPC 面板绑定。
- `TownNpcDialoguePanel` 支持显示当地人物、拜访与对话。
- 既有委托面板、产业面板和当前全局资源 UI 可复用；城镇总览不得伪造未实现的设施或经济状态。
- 旧 M4a/M4b 文档标为历史档案；其中固定委托板的尺寸和内容不构成本设计约束。

## 目标

- 用一页城镇总览承接从世界地图抵达城镇后的体验。
- 清洲、热田、那古野各有独立视觉构图与地点焦点，不以同一张通用背景换标题代替城市差异。
- 优先呈现当地人物、风闻和可处理委托。
- 复用现有会话状态、人物名单、对话、委托与导航，不新增玩法规则。
- 在 1920×1080 和 1280×720 布局下保持中文可读、按钮可点、区域不互相遮挡。
- 用三幅无文字的原创城镇插画建立地点辨识度，不创建可自由行走的村庄场景。

## 城市辨识度

地点名、居民和任务状态来自现有游戏数据；插画以同一和纸、水墨、藏青与朱红风格创作，各自使用不同构图和视觉焦点。插画属于氛围表达，不增加历史断言或玩法规则。

| 地点 | 视觉焦点 | 现有内容的呈现 |
|---|---|---|
| 清洲 | 城郭与城门构成的城镇天际线 | 显示当前分配的两名当地人物；使用现有补给任务在清洲领取/报告时显示相应状态。 |
| 热田 | 神社参道与临水街区 | 显示当前分配的当地人物；使用现有补给任务调查/交涉阶段显示相应状态。 |
| 那古野 | 沿路市集与丘陵边缘的街道 | 显示当前分配的两名当地人物；没有当前任务动作时明确显示空状态。 |

## 非目标与反目标

- 不制作可探索的村庄地图、战术地图、复杂街区模拟或新场景。
- 不新增设施经营、贸易、招募或任务系统。
- 不显示虚构的 NPC、传闻、资源、设施状态或任务进度。
- 不用“设施一/设施二”一类无实际功能的占位入口充实页面。
- 不新增 Unity 包或外部素材依赖。

## 方案比较

1. **城镇总览（选定）**：中心静态城镇插画与地点信息，侧栏展示人物和委托。它把现有功能放在同一视觉入口，且不要求增加模拟系统。
2. **设施优先**：以建筑卡片为主。当前可用设施与经营动作不足，容易变成一页不可操作的占位按钮。
3. **人物优先**：以 NPC 名册为主。适合拜访流程，但会与现有人物页重复，也不能清楚呈现委托和当地情报。

用户选择城镇总览；布局细节由实现者在不改变上述主轴的前提下完成。

## 页面结构

- **全局顶栏**：沿用现有日期、地点与资源显示，不重复创建第二套资源栏。
- **左栏：当地人物**：显示当前地点实际分配的 NPC 摘要和拜访入口。点击后打开现有 NPC 名单/对话面板。
- **中央：城镇焦点**：显示城镇名称、对应地点的无文字原创静态插画，以及来自当前会话的简短地点信息。插画仅作氛围背景，不代表可移动地图。
- **右栏：传闻与委托**：只显示已有数据支持的当地情报和固定委托摘要；委托操作打开或调用现有任务面板。
- **底部操作**：提供返回世界地图及当前已实现的主要入口。未实现功能不显示为可操作按钮。
- **空状态**：名单或委托为空时明确显示暂无内容，不填充示例数据。

## 交互与状态规则

- 城镇名称和位置始终来自 `GameSession.CurrentLocation`。
- 返回地图调用现有 `WorldMapController.ReturnToMap()`，不得推进日期或改写位置。
- 拜访 NPC 和处理委托调用现有控制器/用例；界面只展示其当前状态。
- 面板关闭后必须回到仍有内容的城镇总览，不得留下空白主区域。
- 页面打开、切换和关闭不得隐式推进时间或覆盖存档。

## 美术与接入约束

- 由 Mochi 为清洲、热田、那古野各生成一幅原创无文字插画；保持和纸肌理、藏青与朱红配色和战国水墨风格，同时以不同构图表现各地点。插画需能被 UI 文本安全覆盖。
- OpenCode2 负责实现和接入素材。将素材来源与许可记录到 `ASSETS.md`。
- 保持 `FoundationScene` 现有对象、`.meta` GUID 和单一生成根；场景变更仅由指定集成者在现有 Unity Editor 中完成，并先验证再保存。
- 不修改 `Library`、`Temp`、包清单或无关场景对象。

## 验收标准

- 结构上只存在一个城镇总览根；现有地图、人物、任务入口仍可访问。
- 1920×1080 与 1280×720 下正文、NPC 卡片、委托和底部操作无裁切或相互遮挡。
- 各地城镇标题和 NPC 名单与当前会话位置一致；空数据使用明确空状态。
- 返回地图后城镇 UI 隐藏，地图重新可用，日期与位置没有额外变化。
- 编译成功，EditMode 测试通过，Console 项目错误为零；Play Mode 验证抵达城镇、进入人物/委托、返回地图的完整流程。

## 留给实现计划的事项

- 确定最小场景生成器/更新器改动及其验证方式。
- 确定当前传闻数据的可用来源；没有结构化数据时，只呈现已有任务/人物文案，不新造传闻系统。
- 由 Mochi 生成并检查插画；由 OpenCode2 集成并由 Mochi 在 Unity Editor 验证。


---

<a id="2026-11-09-world-map-immersive-ui-design"></a>
## 来源：docs/superpowers/specs/2026-11-09-world-map-immersive-ui-design.md

# 世界地图沉浸式 UI 设计稿

**状态：** 已实现并通过 16:9 技术验证；窄窗实机待补。用户选择方案 3 后要求“继续由你开发”，按该方案推进；证据见 [验证记录](verification-and-research-history.md#world-map-immersive-ui-2026-11-09)
**选择方案：** 用户选择方案 3「沉浸式全屏地图 + 可收起侧栏」

**设计权威：** [GDD_v4.0.md](../../GDD_v4.0.md)；地图规则沿用现有领域与会话实现。
**范围：** 世界地图/出行页面布局，以及家族名册和家族编年史入口的可见性与页面切换。

## 意图

让地图成为主要可视区域，同时让出行预览、当地信息和出发操作保持可读；名册与编年史入口在地图页始终可达，不被地图画布或侧栏盖住。切换页面不应暗中推进时间、确认旅行或丢失已选目的地。

## 现状与问题

- `ReferenceGalleryNavigator` 已有地图、家族、内务、人物和事件页面状态；页面根按选中的主页面显隐。
- `WorldMapLayout` 根据 `WorldClockPanel` 和 `AdvanceButton` 计算 M2 安全区；地图内容区域没有显式的全局导航安全边界。
- 名册与编年史生成器要求对应根节点绘制在地图/城镇 HUD 上方。当前导航显隐与 Canvas 绘制顺序共同决定按钮能否显示和接收点击，存在布局责任分散的问题。
- 用户报告家族名册和家族编年史被挡住。本设计将它们作为地图页必须始终可访问的导航入口，并要求地图页切走后不保留拦截射线的全屏根。
- 上述代码检查没有确认实际遮挡发生在哪个分辨率或具体节点；实现阶段需在现有 Unity Editor 中复现并记录。

## 目标

- 地图画布占据顶部全局栏以下的主要可视区，支持平移/缩放的具体能力只在现有地图规则支持时实现。
- 两侧信息面板可独立收起；收起后地图可用面积增大，控制柄仍可见。
- 家族名册与家族编年史可从地图页固定导航区直接进入，且点击不受地图图像或透明 UI 拦截。
- 旅行选择、天数、抵达日期、风险、确认和取消均来自现有会话结果；预览不改变日期或位置。
- 地图、导航、侧栏和决定层具有明确的层级及射线边界。
- 以 1280×720、16:9 为逻辑设计基准；较窄窗口优先收起侧栏并让正文滚动，不缩小关键文字。

## 非目标

- 不新增地图地理、路线、移动方式或遭遇规则。
- 不新增队伍编成、势力战争迷雾、资源产出、委托列表或传闻数据；没有现成数据的区块不显示。
- 不重写地图控制器、旅行结算、名册模型、编年史模型或游戏主导航。
- 不把常驻地图/名册/编年史页面做成多层模态弹窗。
- 不创建 Unity 项目、分支、worktree，不安装包，不改无关场景。

## 方案比较

| 方案 | 取舍 | 结论 |
|---|---|---|
| 固定导航栏 + 独立地图页 | 导航稳定、页面边界清楚；地图画布不覆盖其他页面。 | 可行，但地图区域的沉浸感较弱。 |
| 保留当前地图叠层，只调整层级和安全区 | 改动最少；未来页面根增加时仍依赖 Canvas 子节点顺序。 | 不采用。 |
| 沉浸式地图 + 可收起左右侧栏 | 地图是内容焦点；固定导航层仍保持可达；窄屏可收起侧栏。 | **用户选择，采用。** |

## 页面布局

```text
┌──────────────────── 固定全局栏 / 页面导航 ─────────────────────┐
│ 家纹  日期·季节·地点  关键资源       家族  人物  地图  委托  编年史 │
│                                      快捷入口：家族名册  家族编年史 │
├──────────────┬────────────────────────────┬─────────────────────┤
│ 左侧出行栏   │                            │ 右侧当地信息栏      │
│ 起点/目的地  │       沉浸式世界地图       │ 情报（有数据时）    │
│ 行程/天数    │  地点标记、当前位置、路线  │ 当地人物（有数据时）│
│ 抵达日期/风险│                            │ 可用委托（有数据时）│
│ [收起]       │                            │ [收起]              │
├──────────────┴────────────────────────────┴─────────────────────┤
│ 固定出行操作栏：路线摘要 · 风险 · [取消] [确认出发]             │
└─────────────────────────────────────────────────────────────────┘
```

### 全局栏与导航

- 全局栏固定在 Canvas 顶部安全区，绘制层级高于地图及侧栏。
- 日期、季节、当前位置和资源只显示当前会话的真实值。
- 主导航沿用现有主页面；“家族名册”和“家族编年史”作为可见快捷入口保留在导航区，不放进可收起侧栏。
- 点击快捷入口切换到对应家族页面，并隐藏地图页根；返回地图后恢复之前的目的地选择和出行预览。
- 不通过把名册/编年史根简单置顶来覆盖地图页面；页面切换必须由同一导航状态管理。

### 地图画布

- 中央区域展示现有原创地图插画/地图视图；地点标记、路线和当前位置均作为独立 UI 元素叠加，地图图片不烘焙按钮或文字。
- 只呈现 `WorldMapController` 和地图领域规则支持的地点、路线与旅行预览。不可达目的地显示明确原因或不可选状态。
- 点击地点只更新选择和预览；只有点击固定操作栏的“确认出发”才提交旅行。
- 地图图像本身不拦截透明区域的 UI 射线；可交互标记有足够点击范围。

### 左侧出行栏

- 展示当前位置、已选目的地、旅行天数、抵达日期及规则提供的风险/限制。
- 未选目的地时显示简洁提示；当前位置作为目的地时说明无需旅行。
- 收起后留下窄控制柄与已选目的地缩写；再次展开恢复滚动位置。
- 不显示虚构队伍。如果现有旅行数据没有可选队员，整段队伍区不创建。

### 右侧当地信息栏

- 依次展示实际存在的当地情报、可拜访人物或可执行委托。
- 各区块只在有对应会话数据和已实现入口时出现；空列表不占据固定高度。
- 收起/展开状态不改变游戏数据；收起后保留控制柄和未读/待处理数量，但数量必须来自真实状态。

### 固定出行操作栏

- 未选择地点时主操作禁用，并显示需要选择目的地。
- 选择有效目的地后展示路线摘要、旅行时长、抵达日期及风险；成本或阻止原因由现有规则提供。
- “取消”清除当前预览；“确认出发”调用既有旅行确认路径。确认结果和失败反馈在操作栏内可见。
- 不因开合侧栏、切换导航或查看城镇而提交旅行或推进日期。

## 遮挡与层级约束

Canvas 页面内容按以下顺序管理：

1. 地图背景/地图画布（最低交互层；装饰图不接收射线）。
2. 地点标记与路线，以及左右侧栏。
3. 固定出行操作栏。
4. 固定全局导航栏，始终位于页面内容之上。
5. 需要立即决策的事件/确认弹层，位于最上层并明确遮罩背景。

地图页隐藏时，其背景、标记和侧栏均不得继续接收射线。名册和编年史以页面切换打开，打开期间地图页不活动；返回地图后恢复地图选择。不得依赖同级 Canvas 子节点顺序作为唯一显隐规则。

## 响应式与可读性

- 16:9：左右侧栏可同时展开，地图为中心视觉主体。
- 较窄窗口：地图仍保留中心区域；优先收起右侧栏，再按需收起左侧栏，不能遮住导航和底部确认操作。
- 侧栏收起按钮、名册/编年史入口、目的地标记和确认按钮始终可见并可点击；不能只依赖悬停显示说明。
- 关键文本沿用全局 UI 设计的正文与标签字号；窄屏使用滚动，不压缩到不可读。

## 视觉方向

沿用已批准的深靛框架、古金边线、羊皮纸信息卡和朱印主操作。地图区域保持大块低噪点地形插画；UI 文字、地点徽记、路线和按钮使用独立元素，保证清晰度与交互性。所有新增美术遵循 `docs/ART_ASSET_WORKFLOW.md`，记录来源/许可到 `ASSETS.md`；不复制商业游戏素材。

## 状态与交互约束

- 侧栏展开状态、地图缩放/平移、选中目的地属于页面表现状态，不在没有既有存档契约时增加存档字段。
- 导航切页不修改会话、日期、当前位置、存档或旅行状态。
- 选择地点由现有地图控制器更新预览；路线确认/取消沿用现有命令。
- 关闭/重进地图页应清除失效对象引用并刷新会话快照，同时保留仍有效的地点选择。
- 全屏装饰、折叠栏外壳和遮罩必须有明确射线目标；地图绘图层不能吞掉导航按钮输入。

## 验收方向

- 地图打开时，“家族名册”和“家族编年史”入口清楚可见、可点击；打开后地图页面不遮挡其内容。
- 在 1280×720 与较窄窗口下检查地图、侧栏、导航、底栏无重叠或裁切；侧栏折叠/展开不遮住主要操作。
- 目的地选择只产生预览；确认出发才改变地点/日期；取消不改变会话状态。
- 地图页切换到名册、编年史并返回后，页面显隐正确、射线不串层、有效选择状态恢复。
- 后续实现分别记录编译、EditMode、场景 Validator、Console 和 Play Mode 证据；Play Mode 后清理所有 Computer Use 截图。

## 留给实现计划的决定

- 侧栏的像素宽度和动画时长依据现有 CanvasScaler、地图插画比例和 1280×720 实机截图调校；不得改变上述信息层级或导航边界。
- 地点标记的图形样式沿用项目原创地图资产风格；需要新增素材时按素材工作流制作。
- 当前代码若缺少右侧某类当地数据，则隐藏该区块，不为满足版式新增领域功能。

## 2026-11-10 窄屏家谱可读性补充

从地图快捷入口打开家谱时，家谱面板也遵循窄屏安全区：窄屏缩小面板与既有 ScrollRect 视口，不缩小节点文字或改写家谱布局；当图内容宽于视口时，提供独立横向滚动条以便触达右侧成员。滚动条和把手必须幂等复用，重复排版不得重置由 uGUI Scrollbar 管理的把手尺寸。1280×720 下恢复原始面板宽度。

该补充已在 Unity Editor 的 960×720 与 1280×720 Game View 预设中完成 Play Mode 检查；实现细节、测试与剩余验收边界见 [验证记录](verification-and-research-history.md#world-map-immersive-ui-2026-11-09)。
