# 《影之一族》项目状态

更新时间：2026-11-03
设计权威：[GDD_v4.0.md](GDD_v4.0.md)。本文件是当前执行状态入口；旧《Sengoku Chronicle》M1–M7 文档仅作为历史记录，不代表新方向进度。

## 当前方向

- 游戏：以家族兴衰、家主角色扮演与世代传承为核心的战国人生模拟。
- 工程：复用唯一项目 `C:\Users\panye\My project`，Unity `6000.6.6f1`，不另建项目。
- 规则与范围以 GDD v4.0 的 M0–M8 路线及 SC-001–SC-012 首批任务为准；不从旧 M1–M7 代号推断新方向进度。

## 新方向检查点

| 检查点 | 状态 | 证据 / 边界 |
|---|---|---|
| SC-001 项目基线审计 | 已完成 | `docs/SC001_BASELINE_AUDIT.md`；此文档原始快照中的后续状态由本文件覆盖。 |
| SC-002 纯 C# 核心边界 | 已完成 | 在现有 Domain 基础上建立边界；不另建平行 Core。 |
| SC-003 人物、家族与关系模型 | 沿用现有模型；本次未纳入 SC-004–SC-009 验收 | 家族模型相关未提交文件仍是工作区内容，未合并进本 checkpoint；不得据此声称 SC-003 已完成新方向验收。 |
| SC-004 月度推进 | 已实现并随本 checkpoint 获用户试玩验收 | 月份推进与地图月末按钮接入。 |
| SC-005–SC-009 随机、版本化存档、事件数据校验、事件引擎与选择 UI | 已实现并随本 checkpoint 获用户试玩验收 | `demo-SC004-SC009`；用户确认 Edit Mode 全部通过及试玩验收。 |
| SC-010 初代家主退休与继承 | 已获用户验收 | 提交 `af35633`，标签 `demo-SC010`；用户试玩验收由协调方记录。 |
| SC-011 跨代事件链 | 用户已验收 | 验收日期 2026-11-02；用户报告编译通过、EditMode passed、Console 无错误、Play Test 通过。EditMode 数量与 Console 警告数未提供；FoundationSceneValidator 结果未报告。试玩脚本与验收记录见 `SC011_DEMO.md`。 |
| SC-012 家族编年史 | 用户已验收（2026-11-03） | 用户报告 EditMode 通过、Play Test 通过；EditMode 数量、Console 计数与 FoundationSceneValidator 结果未单独提供。checkpoint 与推送状态见本文件后续更新。详见 `SC012_DEMO.md`。 |

## SC-011 当前实现状态（2026-11-02）

- 切片：初代在 `event.rainy-arrival` 选择收留或推迟后，经一次 SC-010 交接，新家主在下一个月遇到对应后续事件 `event.rainy-arrival.legacy-honored` 或 `event.rainy-arrival.legacy-deferred`。
- 代码：新增 `EventRuleContext`、三种允许列表条件（`EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`）、内容版本 `2` 与存档 `1 → 2` 附加兼容窗口；未改场景、预制体、`ProjectSettings` 或包。
- 用户验证报告：编译通过；EditMode passed（未提供 Passed/Failed/Skipped 数量）；Console 无错误（警告数量未提供）；Play Test 通过并获用户验收。
- 证据边界：以上运行结果来自用户报告，本协调过程未操作 Unity Editor。`FoundationSceneValidator` 结果未报告，不记录为通过。
- 评审：OpenCode2 两轮独立只读静态审查均 ACCEPT；不替代 Unity 验证或用户验收。
- 验收：SC-011 已获用户明确验收；按项目规则合并 `main`、标记 `demo-SC011` 并推送。

## SC-012 当前实现状态（2026-11-03）

- 范围：在现有 Domain/Application/Persistence/Presentation/Editor 中追加家族编年史，记录已提交事件、实际关系数值变化、任务阶段变化和继承阶段变化；不新建 Unity 项目、场景或 package。
- 存档：schema 从 v3 升为 v4；v3 已保存事件历史按原年/月与顺序迁入，旧档缺失的任务、关系、继承经历保持为空，不从当前快照推算。
- 代码：OpenCode2 已完成 ledger、会话提交接线、存档迁移/校验、只读滚动面板、事务式 FoundationScene updater、validator 扩展及 EditMode 测试源文件。用户 Play Test 报告通过；FoundationScene 已包含编年史 UI。
- 静态/离线验证：实现 worker 报告过七个程序集 Roslyn 类型检查 0 错误，以及三项 harness 22/22、17/17、14/14；第三轮复审前的最后修复只报告静态 brace/whitespace 检查，旧 harness 未在后续修复后重跑。所有这些都不等同于 Unity 编译或 Unity Test Runner。
- 独立复审：第三轮 OpenCode2 只读复审 ACCEPT 当前代码/测试源；指出 FoundationScene 尚未接入会阻止面板显示和 Validator 通过，需先运行 updater、保存并验证。另有 append 失败日志/关系刷新缺少应用层测试，以及 v1 异常 chronicle 字段被忽略的低风险兼容边界。
- 用户验证报告：EditMode 通过；Play Test 通过并明确验收。具体 Passed/Failed/Skipped 数量未提供。
- 未单独报告：Console error/warning 数及 FoundationSceneValidator 报告；不据此声称错误为零或 Validator 通过。第三轮静态复审仍仅作为代码审查证据。
- 当前状态：SC-012 已获用户验收，可创建 milestone checkpoint、合并 `main`、打 `demo-SC012` 标签并推送；下一候选按 GDD v4.0 为 M2 三代速通原型。

## SC-004–SC-009 已接受 checkpoint

- 提交：`c29d24a387ef73c5d3c5d80ea9148d7a6b45af54`。
- 分支：`feature/sc004-monthly-time` 与 `main` 均已推送至 `origin`。
- 标签：`demo-SC004-SC009` 已推送。
- Edit Mode：最新 `TestResults.xml` 报告 548/548 通过，0 失败、0 跳过。
- Play Mode：用户明确报告试玩验收。
- 独立只读代码审查：OpenCode2 为 ACCEPT。
- 编译、Console 红色错误计数和场景验证分别记录；本次没有单独取得新的编译记录或 Console 计数，不能推断其为零。

### 尚待补齐的场景验证

`FoundationScene.unity` 中 `EventChoiceFeedbackText` 仍序列化为旧中心位置 `y=-322`。运行时面板刷新会应用顶部反馈条布局，用户已验收该试玩表现；但本次没有从现有 Unity Editor 执行场景更新菜单，也没有运行 `FoundationSceneValidator`。因此场景验证仍待补做，Orca 的 SC-009 最终签核项保持 blocked。此项不撤销用户的试玩验收或已推送 checkpoint。

## 工作区保护

当前工作区仍有既存用户修改，包括 `SampleScene.unity`、旧方向文档、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/` 与 `nul`。这些内容不属于 SC-012；不得清理、暂存或覆盖。SC-012 代码、测试和文档按任务范围单独核验。

其他链接 worktree `C:\Users\panye\EU4-m3` 与 `C:\Users\panye\SengokuChronicle-M2` 也保留各自工作状态，不因当前路线更新而删除或改写。

## 历史方向资料

旧 `Sengoku Chronicle` 方向的 Phase 1/Phase 2 路线、M2–M7 演示、任务计划与设计稿保留为历史交付证据。其场景流程、里程碑命名、验收状态与旧方向决策不得当作《影之一族》当前规格；新实现先检查可复用性，再以 GDD v4.0 为准。
