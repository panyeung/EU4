# Sengoku Chronicle — Architecture v0.1

> 规划提案，待用户批准；不是已实现架构。目标是为 Phase 1 提供简单边界，避免过早引入框架。

## 目标与约束

Windows 单机 RPG，Unity 6000.6.6f1、URP 17.6.0、C#。UI 使用 uGUI；核心模拟/规则尽量用普通 C#；静态定义优先 ScriptableObject，运行时状态与存档 DTO 分离。不得为架构而架构；优先稳定 ID、少量接口和可独立测试的纯逻辑。

## 建议目录（待实施时按现有项目结构核实）

```text
Assets/
  Scripts/
    Presentation/       # MonoBehaviour、uGUI 绑定、地图点击与界面呈现
    Application/        # GameSession、用例编排、系统间流程
    Domain/             # WorldClock、运行时模型、任务条件/效果及规则（纯 C#）
    Definitions/        # 静态定义类型、加载/引用约定；资产建议在 Assets/GameData/
    Persistence/        # 版本化 DTO、JSON 编解码、存档槽读写与迁移
    Editor/             # 仅用于受支持的 Unity Editor 自动化
  GameData/             # ScriptableObject 定义资产
  Scenes/               # 场景；避免自动化直接改 YAML
  Tests/
    EditMode/           # 纯 C# 规则与数据验证测试
    PlayMode/           # 必要时验证 Unity 对象/UI 集成
```

项目目前已有 `Assets/Scripts/Player/PlayerMovement2D.cs`、`Assets/Scenes/SampleScene.unity` 和 Input System 设置。保留移动脚本与 `.meta`/GUID；Phase 1 暂不把角色移动接入可步行场景。上述目录为建议结构，不能在规划阶段预建代码或移动现有文件。

## 系统职责与数据流

1. **Presentation（Unity/uGUI）**：显示地图、日期、城镇菜单、对话和任务反馈；把点击转为应用层请求；不承载任务奖励/月份等规则。
2. **Application（流程编排）**：`GameSession` 管理当前运行状态并协调用例；校验输入后调用规则，再通知界面刷新。建议以简单明确的方法/事件连接，只有当需要替换实现或测试隔离时再引入接口。
3. **Domain（纯 C# 规则）**：`WorldClock` 与月份推进、地点连接/旅行时间推进、关系变化、任务状态/奖励领取规则。避免依赖 `MonoBehaviour`、场景对象或 UI。
4. **Definitions（静态内容）**：地点、角色、势力、任务等 ScriptableObject 定义；每条定义含稳定 ID。引用优先按 ID/受校验定义引用，加载时检测重复/缺失 ID。
5. **Runtime state（可变状态）**：日期、当前地点、人物关系、任务进度、已领取奖励等普通 C# 状态；不修改静态定义资产来存进度。
6. **Persistence（存档）**：把运行状态显式映射到带 `saveFormatVersion` 的 DTO，再序列化到一个槽；加载时校验版本/数据并重建运行状态。存档格式不直接序列化 Unity 对象、`MonoBehaviour` 或 ScriptableObject 引用。

**数据流：** uGUI 输入 → 应用层用例 → 纯 C# 规则修改运行状态 → 应用层返回结果/状态变化 → uGUI 刷新；存档请求由应用层取当前状态生成 DTO 并经 Persistence 写入。

## 关键概念/接口（建议，不表示现有代码）

- `GameSession`：启动新局、持有/提供运行状态、协调时间/移动/对话/任务/存档用例。
- `WorldClock`：初始日期为 1560-04-01；每次推进一天；使用简化前推公历，正确处理月份与年份切换。M1 以 EditMode 边界测试验证。
- `LocationDefinition`、`CharacterDefinition`、`FactionDefinition`、`MissionDefinition`：静态定义及稳定 ID。
- `WorldState`：可变世界状态集合；不向 UI 暴露修改内部状态的任意入口。
- `MissionState`：单任务运行进度、完成与奖励领取状态；领奖操作必须幂等/重复领取无效。
- `SaveData`：DTO 根对象，含 `saveFormatVersion` 与所需运行状态。

避免一开始建设通用事件总线或脚本语言。任务条件/效果采用受限、可枚举的数据类型，先覆盖本阶段需要的条件与效果；未知类型应安全失败并有诊断，不执行任意代码。

## 依赖方向

```text
Presentation → Application → Domain
Definitions ───────────────→ Application/Domain（只读静态数据）
Persistence ↔ Application（DTO 映射边界），Persistence 不反向依赖 UI
```

Unity 类型留在表现/定义/持久化适配边界；规则程序集可在后续需要时通过 asmdef 隔离，但 Phase 1 初期不强制增加程序集复杂度。

## 测试与验证

- EditMode：M1 验证日期递增、月末到下月首日、12 月 31 日到次年 1 月 1 日；后续阶段再验证旅行、关系、任务奖励与存档 DTO。
- PlayMode/Editor：地图点击触发正确路线与日期变化；城镇菜单/对话可操作；存档后加载恢复重点状态。
- QA 报告须区分 C# 编译、EditMode、PlayMode 与人工验证；未实际运行的检查不得报通过。

## 存档策略

单槽，格式版本字段必需；Phase 1 只保存恢复当前演示所需的最小状态。定义稳定 ID 用于跨运行时引用；读取时发现不支持版本、缺失数据或未知 ID，应提供可理解的错误并保留原存档，禁止静默覆盖。具体序列化器、位置和迁移策略留待 M5 技术任务确认；不预先增加依赖。

## 重要技术决策

- 使用 uGUI；城镇先用菜单；世界地图地点点击旅行。
- 规则与可变状态用普通 C#；静态内容用 ScriptableObject；存档使用独立版本化 DTO。
- 保留 `PlayerMovement2D.cs` 原样，不将其接入 Phase 1。
- 优先 Unity Editor 脚本和受支持 API 创建场景/Prefab；避免直接编辑 `.unity`/`.prefab` YAML，保持 `.meta`/GUID。
- 不安装新包、不加框架，除非有经评估且批准的明确需求。

## M1 架构概览（已落地）

> 实现日期：2026-10-20；Kanban 卡片 t_6ec3a019。分层 + asmdef 隔离：Presentation → Application → Domain；EditMode 测试程序集引用 Domain + Application。

### 组件

| 组件 | 所属层 / 程序集 | 路径 | 说明 |
|---|---|---|---|
| WorldClock domain | Domain | `Assets/Scripts/Domain/WorldClock.cs`、`GameDate.cs` | `SengokuChronicle.Domain` 纯 C#。`WorldClock.CurrentDate` 只读；`AdvanceOneDay/AdvanceDays` 失败返回 `false`、状态不变；`TryCreate` 非法日期返回 `null`。起始 `1560-04-01`，简化前推公历，范围 `1560-01-01~2000-12-31`。`GameDate` 不可变值类型，不持 Unity 引用。 |
| GameSession application | Application | `Assets/Scripts/Application/GameSession.cs` | `SengokuChronicle.Application`；`MonoBehaviour`，持有 `WorldClock`；`NewGame()` 在临时 GameObject 上创建实例，`InitNewGame()` 重置为 `1560-04-01`。 |
| WorldClockPresentation | Presentation | `Assets/Scripts/Presentation/WorldClockPanel.cs` | `SengokuChronicle.Presentation`；类 `WorldClockPanel : MonoBehaviour`，uGUI 面板，显示日期文本 +「推进一天」按钮，不承担日期规则；`SetDependencies(session, button, text)` 供 Editor 注入。 |
| FoundationScene | — | `Assets/Scenes/FoundationScene.unity` | 菜单 `Tools → Sengoku Chronicle → Create Foundation Scene` 生成；含正交 Main Camera、EventSystem、Canvas、GameSession 与 WorldClockPanel。 |
| CreateFoundationScene | Editor | `Assets/Scripts/Editor/CreateFoundationScene.cs` | 用 `EditorSceneManager.NewScene` 新建场景，不修改 `SampleScene`/`.unity` YAML；保存后 `AssetDatabase.Refresh()`。 |

### 程序集定义（asmdef）

- `SengokuChronicle.Domain` — `Assets/Scripts/Domain/SengokuChronicle.Domain.asmdef`，纯 C# 规则核，无引用，`autoReferenced=true`。
- `SengokuChronicle.Application` — `Assets/Scripts/Application/SengokuChronicle.Application.asmdef`，引用 `SengokuChronicle.Domain`，`autoReferenced=true`。
- `SengokuChronicle.EditModeTests` — `Assets/Tests/EditMode/SengokuChronicle.EditModeTests.asmdef`，`isTestAssembly=true`，`includePlatforms=["Editor"]`，`assemblyDefinitionReferences` 为 `SengokuChronicle.Domain` + `SengokuChronicle.Application`，`precompiledReferences` 为 `Unity.TestRunner`/`UnityEditor.TestRunner`/`nunit.framework`，`autoReferenced=false`。（自定义 asmdef 默认不可见 `Assembly-CSharp`；M1 未引用它，因 `GameSession` 归属 `SengokuChronicle.Application` 且已显式引用。）
- 变更记录：`SetDependencies` 注入 UI 字段修复 `CreateFoundationScene` 的 CS1061；测试 asmdef 引用 `SengokuChronicle.Application` + `WorldClockTests.cs` 加 `using SengokuChronicle.Application` 修复 CS0234。

### 验证

- **编译**：Unity 6000.6.6f1 batch-mode（`-batchmode -projectPath -runTests -testPlatform EditMode`）实际编译 Domain / Application / Presentation / EditModeTests 四个程序集，**0 Error、0 Warning**。
- **根因修复**：`CreateFoundationScene.cs` 已修复两处运行时/编译阻塞：(1) `GetDefaultFont()` 直接请求 `LegacyRuntime.ttf`（Unity 6000.6 不再内置 `Arial.ttf`，原调用会抛 `ArgumentException`）；(2) EventSystem 改用 `InputSystemUIInputModule`（项目 `activeInputHandler: 1`，仅 Input System；`StandaloneInputModule` 在此模式下会抛异常）。
- **EditMode 测试**：已在 Unity 6000.6.6f1 batch-mode 下实际运行，**20/20 passed，0 failed，0 skipped**（测试结果 XML 附件见实现任务 `t_6ec3a019`）；`WorldClockTests.cs` 共 **20 个 `[Test]`**，覆盖 7 类边界场景（初始/推进、月末跨月、年末跨年、闰年与平年 2 月、非法输入拒绝、超出支持范围、GameDate 输入校验），另含 3 个 `GameSession` 集成测试。
- **离线逻辑验证**：Python 端口复刻 Domain 逻辑，39/39 项检查通过，等价于 17 项纯 CLR EditMode 测试的完整覆盖。

### 已知限制

- `Assets/Scripts.meta`（GUID `24f408c36b0ccaa4097e7b53a590a719`）未暂存，保持原样；Unity 对新增 Assets 的 `.meta` 由 Editor 自动生成。
- `FoundationScene.unity` 已生成（`Assets/Scenes/FoundationScene.unity`），由 Editor 菜单 `Tools → Sengoku Chronicle → Create Foundation Scene` 创建，序列化 `InputSystemUIInputModule`（1）与 `StandaloneInputModule`（0），符合项目 `activeInputHandler: 1`。

### 人工验收步骤

1. 关闭 Unity Editor（释放项目锁）。
2. 重新打开 `C:\Users\panye\My project`。
3. 等待 Unity 编译完成（检查 Console 无 error）。
4. ~~运行菜单 `Tools → Sengoku Chronicle → Create Foundation Scene`~~（场景已生成，此步跳过）。
5. 在 Project 窗口双击 `Assets/Scenes/FoundationScene.unity` 打开。
6. 点击 Play 按钮，观察日期显示为 `1560-04-01`，点击「推进一天」按钮后日期递增。
7. 如行为符合预期，批准本卡并建立 checkpoint commit。
