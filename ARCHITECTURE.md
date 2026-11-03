# 《影之一族》当前架构

> 设计权威：[GDD_v4.0.md](GDD_v4.0.md) 第 11 章。旧 Sengoku Chronicle 的 M1–M7 架构与验收材料从“历史实现记录”起保留，不代表新方向需求。

## 当前实现边界

- **Presentation**：Unity/uGUI 输入与显示；当前可玩检查点包含月末推进、事件选择、继承操作及 SC-012 只读编年史面板。游戏规则不由 UI 执行。
- **Application**：`GameSession` 编排月份推进、存档/恢复和待处理事件；在成功提交边界记录事件、实际关系变化、任务阶段和继承阶段；读档恢复不追加记录。
- **Domain**：日期、确定性随机流和追加式 `HistoryLedger` 使用普通 C# 状态与规则；保持稳定 ID，不依赖场景对象或 Persistence。
- **Events**：独立纯 C# 数据模型、内容验证和允许列表事件引擎；从 `Resources/Events` 加载事件 JSON，从 `Resources/Localization` 加载本地化文本；不执行任意脚本。SC-012 事件关系效果仅在事件提交后进入编年史。
- **Persistence**：Unity 内置 `JsonUtility` 与版本化 DTO；当前存档 schema 为 v4，保留 v1/v2/v3→v4 迁移和恢复保护；v3 事件历史迁移时保留年/月，不推造旧任务、关系或继承记录。
- **Editor**：FoundationScene 的控件更新使用事务式菜单工具；SC-012 updater 与 validator 已实现，待在现有 Editor 执行并保存/验证；`.unity`/`.prefab` YAML 不作为手工编辑入口。
- **测试**：纯逻辑、存档、内容校验与 UI 行为由 Unity Test Framework 覆盖。每次报告分别写明编译、EditMode、场景验证、Console、Play Mode 和用户验收证据。

依赖方向：`Presentation → Application → Domain/Events`；`Application ↔ Persistence` 通过显式 DTO 映射；静态 JSON 内容经验证后由事件系统消费。

## 当前 checkpoint

SC-004–SC-009 已随 `demo-SC004-SC009` 推送到 `main`，并获用户 Edit Mode 与 Play Test 验收。SC-010 以提交 `af35633`、标签 `demo-SC010` 获用户验收。SC-011 已于 2026-11-02 获用户验收：用户报告编译通过、EditMode passed、Console 无错误及 Play Test 通过；EditMode 数量、Console 警告数未提供，`FoundationSceneValidator` 结果未报告。上述 SC-011 结果是用户报告，不是本协调过程取得的 Editor 日志。其余项目证据及旧序列化位置见 [PROJECT_STATE.md](PROJECT_STATE.md)。

SC-012 代码/测试源已通过第三轮 OpenCode2 静态只读复审（ACCEPT）。用户随后报告 EditMode 通过、Play Test 通过并明确验收。具体 EditMode 数量、Console error/warning 计数及 `FoundationSceneValidator` 结果未单独提供。早期 Roslyn/harness 数值先于最后一次代码修复；最新修复仅报告 brace/whitespace 静态检查，不能把这些结果当作 Unity 运行证据。复审还记录 append 失败路径缺少应用层测试以及 v1 异常 chronicle 字段兼容边界，见 `PROJECT_STATE.md` 与 `SC012_DEMO.md`。

## SC-011 事件规则扩展

- `EventRuleContext` 是只读值类型，携带 SC-010 的原始 `SuccessionCount` 与“已落定”计数（仅在 `SuccessionPhase.None` 时等于原始计数）。
- `EventEngine` 新增三种允许列表条件：`EventOptionChosen` 读取追加式 `History` 的最后一条同源事件记录；`SuccessionCountEquals` 与 `SettledSuccessionCountAtLeast` 是仅在下月排程时检查的 offer-time gate。提交已排队事件时跳过这两种继承门，确保稳定决策不会因之后换家主而无法结算。
- `GameSession.BuildEventRuleContext()` 是唯一把 SC-010 继承状态转换为事件规则上下文的位置；事件仍只写入家族级 NPC `CharacterRelationships`，不冒充 `PersonId` 私人关系。
- 事件目录与文本内容版本升为 `2`；存档 schema 仍为 `3`，只增加内容 `1 → 2` 的单向附加兼容窗口。资源文件名保持不变。

## 当前 Play Test 准备

SC-012 家族编年史已获用户 Play Test 验收。下一候选按 GDD v4.0 为 M2 三代速通原型；先完成现有实现差距盘点、中文设计稿与计划，再由 OpenCode2 实现并安排下一轮用户 Play Test。
## 历史实现记录：M4a 补给短缺任务（旧方向）

M4a 采用固定任务模型，不引入通用任务图、事件总线或任务编辑器。`SupplyShortageMission` 在 Domain 层用普通 C# 管理阶段与 merit；`GameSession` 根据当前地点协调显式用例；uGUI 任务面板只显示进度并转发操作。流程为 `NotAccepted → Accepted → Investigated → Negotiated → RewardClaimed`：清洲接受任务、热田调查并与黑田伊织交涉、返回清洲报告；有效报告恰好增加 10 merit，后续报告不会重复奖励。任务操作不推进日期或地点，新局会重置任务和 merit。

该阶段复用现有 Atsuta NPC 黑田伊织，不增加角色定义。任务状态当前属于运行时状态；存档与恢复仍留待 M5。FoundationScene 的 UI 更新由经过预检与验证的 Unity Editor 更新器完成，不直接编辑场景 YAML。M4a 不增加包、资产或第三方运行时代码。

## 历史实现记录：M1 架构概览（旧方向）

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

## 历史实现记录：M4b 两项固定 Phase 1 任务（旧方向）

M4b 在 M4a 的固定 C# 模式上增加 `SealedLetterMission` 与 `MarketTallyMission`。各自持有明确阶段并在最终有效动作时一次性增加 5 merit；`GameSession.Merit` 汇总三项固定任务的已领取奖励。新局重置两个新模型。uGUI 保留 M4a 默认页，并通过页签切换到两项 M4b 任务行；固定根对象维持 280×240，不遮挡城镇标题、NPC 或返回按钮。Unity Editor 更新器在一个 Undo 组中增量添加控件并调整 M4a 按钮，验证引用、布局和相邻控件间距后才保存。任务动作不推进日期或地点；旅行仍由 `ConfirmTravel` 独立校验和推进。没有新增通用任务框架、外部 package、NPC 或存档逻辑；持久化仍属于 M5。

## 历史实现记录：M5/M6 存档槽与标题封装（旧方向）

M5 增加 `SengokuChronicle.Persistence`（`SaveData` v1、校验器、原子单槽存储）与 `GameSession.CaptureSave/TryApplySave/SaveTo/LoadFrom`，地图界面加入 `M5SaveLoadRoot`；merit 由任务阶段推导、不入档，读档不可能重复领奖。M6 在同一分支上增加标题封装：`TitleScreenController`（Presentation）以 `Canvas/M6TitleRoot` 全屏不透明、raycast-blocking 覆盖层门控 Play Mode 启动，「新游戏」调用 `InitNewGame`，「继续」走与 M5 相同的原子 `LoadFrom` 路径，失败仅提示不改状态；`GameSession.IsDemoComplete` 为纯域规则，`DemoSummaryPanel` 只做展示轮询。两个根（标题根与地图内 `M6DemoSummaryText`）在保存的场景中均为 inactive，由运行时激活；标题根必须是 Canvas 中地图根之后的最后一个兄弟以保证覆盖渲染次序。场景集成沿用同一事务式更新器模式（`AddM6TitleScreen`），Validator 报告唯一层级、绑定、包含与分离检查。
## 历史实现记录：M7 功名晋升（旧方向 Phase 2）

M7 引入 `Domain/PromotionRules`：以升序不可变阈值表把 `GameSession.Merit` 映射到四个游戏化军阶（足轻0、番头5、组头10、武士头20；为简化成长标签，不代表史实官职）。军阶是纯派生值：`GameSession.Rank/RankName` 不新增状态、不写入存档，读档无法伪造或重复晋升。`Presentation/RankPanel` 与 `DemoSummaryPanel` 同模式（幂等 `Refresh()` + Update 轮询，规则本体留在 Domain）。场景集成沿用事务式更新器模式（`AddM7RankPanel`）：`M7RankText` 插在 `TownMenuRoot` 之前，验证包含性与对既有地图/M5/M6 控件的分离后才保存。Phase 1 满功绩 20 恰达最高阶，作为演示完美收尾；Phase 2 后续内容（更多 merit 来源或更高层阶）须另行立项。
