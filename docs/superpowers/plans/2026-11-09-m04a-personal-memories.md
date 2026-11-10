# M04A 个人行动记忆实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: 使用 `superpowers:subagent-driven-development`（推荐）或 `superpowers:executing-plans` 按任务逐项实施。步骤使用复选框跟踪。

**Goal:** 为正纲与信正增加有界、持久、可解释的行动记忆，使真实的行动完成或中断能在 30 个游戏日内改变其后续自主行动评分。

**Architecture:** 在现有 `NpcAutonomyPersonState` 中按人物保存最近一次完成与中断记忆；沿用整数 Utility 评分并通过 `NpcAutonomySnapshot` 投影给只读 Inspector。每日行动结果在已成功的状态变更路径中记录；v12 存档新增记忆字段，v11 迁移只初始化空记忆。

**Tech Stack:** Unity 6000.6.6f1、C#、现有 `GameDate`、`DailyActionState`、NPC 自主状态、JsonUtility 版本化存档与 EditMode Test Runner；不加依赖。

**Spec:** [M04A 个人行动记忆设计稿](../specs/2026-11-07-m04a-personal-memories-design.md)

**执行进度（2026-11-09）：** M04A lead 技术签收通过；最终聚焦 181/181、全量 1381/1381、编译、Validator、两轮独立复审与 Play Mode（含 30/31 日和 GUI 存读档）通过。试玩后额外修复记忆裁切与读档共享顶部栏日期刷新，复用原滚动文本和现有回调，未改场景；相关文件为 `NpcAiInspectorPanel.cs`、`CharacterDailyPageView.cs`、`DailyActionPanel.cs` 及对应测试。历史“先写测试再实现”的顺序缺少证据，不追溯勾选；本轮三例缺字段、两例 UI 修复均有明确 red→green 记录，详见 [实现报告](../../reports/m04a-personal-memories.md)。用户最终 M04 里程碑验收仍待办。

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
