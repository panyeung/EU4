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
