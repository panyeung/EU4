# SC-001 基线审计——规划快照

日期：2026-11-01
范围：为在现有 Unity 项目中采用《影之一族》v4.0 计划而进行的只读盘点。
状态：静态盘点完成；本地 `main` 已与 M7 远端基线对齐。当前 Editor 已确认加载指定项目，但电脑控制能力无法聚焦该窗口或操作菜单；EditMode 测试未运行。

## 项目与 Git 身份

- 当前 AGENTS 指定的项目路径：`C:\Users\panye\My project`。
- 指定项目的本地检出位于 `main`，当前提交为 `f413a8f`，与 `origin/main` 一致（`merge: M7 merit and promotion (Phase 2 start)`）。本次更新前本地 `main` 为 `24102fe`，落后 14 个提交；已通过快进更新到远端基线。
- 指定项目有用户 WIP：修改了 `Assets/Scenes/SampleScene.unity`；未跟踪 `Assets/1.unity`、`Assets/1.unity.meta`、`ProjectSettings/SceneTemplateSettings.json` 和 `nul`。这些内容均未触碰。
- 规划工作树是另一个已存在的工作树，位于 `feature/m5-save-slot`，提交 `b5c67c3`，跟踪同名远端分支。它从指定项目的本地 `main` 派生，包含 M7 代码历史和新 v4.0 设计副本，但不是指定项目路径。该工作树自身未跟踪的 `ProjectSettings/SceneTemplateSettings.json` 和 `nul` 也未触碰。
- GitHub 的 `origin/main` 指向 M7 合并提交 `f413a8f`，其中没有 `GDD_v4.0.md`。规划分支 `b5c67c3` 与 GitHub `main` 当前提交彼此都不是祖先。GDD 与 SC 计划仍位于既有规划工作树，目标检出尚无这些文件。
- 指定项目中修改过的 `Assets/Scenes/SampleScene.unity` 在本地和远端树中均受 Git 跟踪，但 `git diff main..origin/main -- Assets/Scenes/SampleScene.unity` 没有差异。后续更新分支时仍须保留用户修改；本次审计不执行合并、重置、切换分支或复制文件。

## Unity 身份与配置

- 此前 PID `56820` 是 `C:\Program Files\Unity Hub\resources\unity.exe serve`，不是 Editor。随后启动 Unity `6000.6.6f1` 时，Editor PID `22768` 的命令行为 `Unity.exe -projectPath "C:\Users\panye\My project"`，并在项目日志中确认了同一项目路径。
- 首次启动的 Editor 在完成资产导入后收到客户端关闭请求并退出。之后用户重新打开了项目；当前 Editor PID `56392` 的窗口标题为 `My project - FoundationScene - Windows, Mac, Linux - Unity 6.6 (6000.6.6f1)`，进程命令行中的 `-projectpath` 指向指定项目。
- 当前 `Logs/Editor.log` 没有匹配到 C# 编译错误，但日志只显示程序集重载请求，未确认完整编译成功。EditMode 测试未运行。
- 指定项目的 `ProjectSettings/ProjectVersion.txt` 显示 Unity `6000.6.6f1`。
- `Packages/manifest.json` 包含 URP `17.6.0`、Input System `1.20.0`、uGUI `2.6.0` 和 Unity Test Framework `1.8.0`。软件包锁定文件包含 Newtonsoft JSON `3.2.1`。manifest 中没有显式的 TextMeshPro 依赖项。
- v4.0 计划提及 Unity 6.3 LTS。遵照单项目约束，暂时沿用现有 `6000.6.6f1`，本次没有尝试升级。

## 现有代码与场景

- 运行时 asmdef：`Assets/Scripts/Domain/SengokuChronicle.Domain.asmdef`、`Application/SengokuChronicle.Application.asmdef`、`Presentation/SengokuChronicle.Presentation.asmdef`；另有 Editor asmdef 和 EditMode 测试 asmdef。指定项目中没有 Persistence asmdef。
- 可能可供评估的领域代码：`GameDate`、`WorldClock`、`CharacterId` / `CharacterDefinition`、`CharacterRelationships`、`BranchingDialogue`、`LocationId`、`WorldMapRules` 和三个固定任务模型。Application 有 `GameSession` 与 `TravelPreview`；Presentation 有地图、时钟、对话和任务面板。
- 指定项目脚本中未发现家族/亲属关系聚合、按月行动经济、固定种子随机服务、JSON 事件 Schema/引擎、继承系统或家族编年史。`BranchingDialogue` 是代码定义的对话流程，不能作为已具备数据驱动世界事件引擎的证据。
- 磁盘上有 `Assets/Scenes/FoundationScene.unity` 和 `Assets/Scenes/SampleScene.unity`。`ProjectSettings/EditorBuildSettings.asset` 只启用了 `SampleScene`。
- `Assets/Tests` 下有 EditMode 目录、16 个 `*Tests.cs` 文件和 EditMode 测试 asmdef。没有发现 PlayMode 测试目录。本次没有运行测试。

## M0 能力对照

| v4.0 能力 | 指定项目证据 | 状态 |
|---|---|---|
| 稳定 ID | `CharacterId`、`LocationId` | 部分具备；没有家族/人物专用 ID |
| 家族、人物、亲属与关系 | `CharacterDefinitions`、`CharacterRelationships` | 部分具备；有对话关系数值，缺少家族/亲属模型 |
| 按月推进时间 | `GameDate`、`WorldClock` | 部分具备；当前按日推进，没有月结或行动预算 |
| 固定种子确定性随机 | `Assets/Scripts` 下未发现对应服务 | 缺失 |
| 版本化 JSON 存档 | 指定检出中没有 Persistence asmdef 或存档 DTO；依赖锁中有 Newtonsoft | 此检出中缺失；最终迁移判断前需核对远端 M7 基线 |
| 数据驱动事件 Schema 与引擎 | `BranchingDialogue` | 缺失；现有对话不是 JSON 事件引擎 |
| 继承与跨代事件 | 未发现对应模型 | 缺失 |
| 家族编年史 | 未发现对应模型 | 缺失 |
| UI 基础 | 现有 uGUI 面板与事务式 Editor 更新器模式 | 可评估复用；家族专属界面尚未实现 |

## 验证边界与下一项决策

本报告区分静态盘点与运行证据。目标 Editor 路径已确认，完整 EditMode 测试已有新结果；Console 计数、场景验证和 Play Mode 仍未完成验证。不要对由用户打开的项目启动 batchmode 或第二个 Editor。

### 2026-11-01 复核

- 本地 `main` 已快进到 `f413a8f`，与 `origin/main` 一致；更新前后 `SampleScene.unity` 的 SHA-256 相同，所有列出的用户 WIP 均仍在。
- 当前 Unity Editor PID `56392`，标题显示 `My project - FoundationScene`，命令行中的 `-projectpath` 指向指定目录。
- 用户在该 Editor 的 Test Runner 中运行 EditMode 全部测试。`C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\TestResults.xml` 时间为 2026-11-01 14:44:33，结果 `Passed`：425 总数、425 通过、0 失败、0 跳过、0 不确定。
- 同一项目 `Logs/Editor.log` 时间为 2026-11-01 14:44:33；未发现 C# 编译错误标记，且 Test Runner 成功执行。Console 面板红色计数没有读取；日志中有 Unity Licensing 404 与 AI ModelSelector `NoSubscription` 错误，因此不能声称 Console 零错误。
- SC-001 静态盘点、Git 基线对齐及 EditMode 测试已完成；Console 计数、场景验证和 Play Mode 仍未验证。不得对由用户打开的项目启动 batchmode 或第二个 Editor。

OpenCode 后续按 `GDD_v4.0.md` 中批准的 M0 顺序推进；SC-002 的 Domain 静态边界和 EditMode 测试已有通过证据。SC-002 的 Console 计数仍需补记。不得新建 Unity 项目。

## SC-002 修正后复核 — 2026-11-01

- 独立 QA 发现 Domain asmdef 中 `useUnityAssemblies` 是无效字段；已在 `feature/sc002-domain-boundary` 将其替换为 `noEngineReferences: true`。复核接受该单行修改，未触碰场景、`.meta` 或用户 WIP。
- Unity 6000.6.6f1 重新导入后生成的 `SengokuChronicle.Domain.csproj` 不再引用 UnityEngine/UnityEditor。新 `Logs/Editor.log`（本地 15:10:53）中 `error CS` 为 0。
- 修正后 EditMode `TestResults.xml`（本地 15:10:44）报告 425/425 通过，0 失败、0 跳过、0 不确定。
- 用户报告此次 Unity Console 红色错误数为 0。日志仍有 Licensing 404 与 AI `NoSubscription` 服务消息；日志事件与用户读取的 Console 计数分别记录。
- SC-002 不是可玩演示；本次只建立并推送功能分支检查点，不合并 main、不打标签。Kanban/QATester 接口不可用；独立 OpenCode 技术复核接受修正。

## 新方向当前状态更新（2026-11-02）

本文是 SC-001 基线快照；文中旧的“下一项 SC-003”仅反映当时状态，现已被后续检查点取代。SC-004–SC-009 已合并并推送至 `main`，用户已确认 Edit Mode 与 Play Test 验收；当前下一项为 GDD v4.0 的 SC-010。SC-003 家族模型相关工作区文件未并入该 checkpoint。当前权威状态见 `PROJECT_STATE.md`。
