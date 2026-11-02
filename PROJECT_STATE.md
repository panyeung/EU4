# 《影之一族》项目状态

更新时间：2026-11-02
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
| SC-012 家族编年史 | 后续 | 与 SC-011 一起支撑 M2 三代纵向切片；不提前声称完成。 |

## SC-011 当前实现状态（2026-11-02）

- 切片：初代在 `event.rainy-arrival` 选择收留或推迟后，经一次 SC-010 交接，新家主在下一个月遇到对应后续事件 `event.rainy-arrival.legacy-honored` 或 `event.rainy-arrival.legacy-deferred`。
- 代码：新增 `EventRuleContext`、三种允许列表条件（`EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`）、内容版本 `2` 与存档 `1 → 2` 附加兼容窗口；未改场景、预制体、`ProjectSettings` 或包。
- 用户验证报告：编译通过；EditMode passed（未提供 Passed/Failed/Skipped 数量）；Console 无错误（警告数量未提供）；Play Test 通过并获用户验收。
- 证据边界：以上运行结果来自用户报告，本协调过程未操作 Unity Editor。`FoundationSceneValidator` 结果未报告，不记录为通过。
- 评审：OpenCode2 两轮独立只读静态审查均 ACCEPT；不替代 Unity 验证或用户验收。
- 验收：SC-011 已获用户明确验收；按项目规则合并 `main`、标记 `demo-SC011` 并推送。

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

当前工作区仍有与 SC-011 无关的未提交内容，包括 `SampleScene.unity`、SC-003 家族模型文件、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/` 与 `nul`。这些内容不纳入 SC-011 checkpoint；继续工作前先复核 Git 状态，禁止清理或覆盖。

其他链接 worktree `C:\Users\panye\EU4-m3` 与 `C:\Users\panye\SengokuChronicle-M2` 也保留各自工作状态，不因当前路线更新而删除或改写。

## 历史方向资料

旧 `Sengoku Chronicle` 方向的 Phase 1/Phase 2 路线、M2–M7 演示、任务计划与设计稿保留为历史交付证据。其场景流程、里程碑命名、验收状态与旧方向决策不得当作《影之一族》当前规格；新实现先检查可复用性，再以 GDD v4.0 为准。
