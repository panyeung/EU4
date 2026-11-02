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
