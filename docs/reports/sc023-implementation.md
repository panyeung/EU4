# SC-023 隐居与后见人实现报告

**日期：** 2026-11-05  
**分支：** `panyeung/sc023-seclusion`  
**范围：** 仅实现 `docs/superpowers/specs/2026-11-05-sc-023-seclusion-design.md` 与 `docs/superpowers/plans/2026-11-05-sc-023-seclusion.md`；未合并、未提交、未推送。  
**执行边界：** 未新增或运行测试；未操作 Unity、GUI、截图、Computer Use 或 batchmode；未编辑序列化场景、Prefab 或 `ProjectSettings`；未改 `Library`/`Temp`；未安装包；未改既有 `.meta` GUID。

## 审查修正（2026-11-05）

### P2：退休前任家主必须有真实家主证明

- `EducationMentorRules.IsEligible` 新增 `hasHeldHeadRole`。SC-023 的退休 `DirectKin` 只有同时满足「继承编年史中存在以其为 `FromHeadId` 的真实交接记录」才可作为导师；退休但从未担任家主的人物不再被误判。
- 运行时由 `SuccessionHistory.HasHeldHeadRole(HistoryLedger.Entries, personId)` 派生证明，覆盖导师列表、预览、提交和年结失效判断。
- 存档校验从既有 `ChronicleSaveEntry` 的 succession 交接记录派生 `FormerHeadIds`，并传入 `CharacterDevelopmentSaveData.ValidateForSave`；不新增字段、不升 schema。保存含“退休非前任家主”计划的存档会被拒绝，而不是静默回退。
- 计划中的真实前任家主若死亡、离族或角色失效，仍按既有无有效导师规则保留计划并在年结回退。
- 对应文件：`EducationMentorRules.cs`、`GameSession.cs`、`SaveData.cs`、`SaveDataValidator.cs`。

### P3：SC-013 标题修复计数包含 SC-023

- `AddSC013TitleScenarioButtons.Execute` 的 `optionalEntries` 现在统计 `SC023SeclusionDemoButton`，与 `Validate` 一致。
- 同时在该修复路径中把标题状态行移到 `(0,-386)`，避免它落到既有 SC-023 入口上；无 SC-023 入口时仍保持原有 `-330/-260` 行为。
- 对应文件：`AddSC013TitleScenarioButtons.cs`。

## 变更摘要

### 新增

- `Assets/Scripts/Domain/EducationMentorRules.cs`：共享导师资格规则。非退休导师规则不变；仅 `family.tachibana.sc023` 允许存活、成年、当前家族、`DirectKin`、非培养对象本人、且继承编年史证明曾任旧家主（`FromHeadId`）的退休人物。SC-023 家族 ID 只在此处定义一次，应用与存档校验共用；同时提供 `SuccessionHistory.HasHeldHeadRole`。
- `Assets/Scripts/Application/SC023SeclusionScenario.cs`：固定三代橘家。
  - 起始日期 `1560-12-01`。
  - 宗严 1498（62，家主，学问专长）、赖久 1528（32，指定成年继承人）、千代 1550（10，儿童）。
  - 千代初始五项 `10/12/18/11/13`；资质学问；有利环境。
  - 旧家主为指定继承人完成交接后成为退休 `DirectKin`。
- `Assets/Scripts/Application/EducationScenarioProvider.cs`：场景路由。SC-020 保持青叶固定值；SC-023 使用橘家值；其他场景返回中性值，不再借用 SC-020 的身份/资质/环境判断。
- `Assets/Scripts/Editor/AddSC023Seclusion.cs`：事务式标题入口生成器。
  - 固定复用 `Canvas/SC020EducationRoot` 与 `CharacterEducationPanel`，不新建 UI 根。
  - 一个 Undo group、保存前 `Validate`、异常 `Undo.RevertAllDownToGroup`、不重载场景。
  - 标题入口 `SC023SeclusionDemoButton` 位于 `(0,-332)`；状态行下移到 `(0,-386)`，避免重叠。
  - `Validate` 检查标题控件、绑定、边界/重叠，以及 SC-020 教育根与面板绑定依赖。
- 新增 C# `.meta` 文件（4 个）：`EducationMentorRules.cs.meta`、`SC023SeclusionScenario.cs.meta`、`EducationScenarioProvider.cs.meta`、`AddSC023Seclusion.cs.meta`。GUID 为新生成，未改动任何既有 GUID。

### 修改

- `Assets/Scripts/Application/EducationView.cs`
  - 导师选项增加可选 `RoleLabel` 与 `DisplayLabel`，退休导师显示「隐居家主／前任家主」。
  - 计划/结算显示使用场景 provider 的专长和身份，不再硬编码 `SC020EducationScenario`。
  - 预览显示导师专长与身份，保留既有数值格式。
- `Assets/Scripts/Application/GameSession.cs`
  - 新增 `InitSeclusionDemoScenario()` / `InitSeclusionDemoScenario(ulong)`。
  - 导师列表、预览、提交、结算、成长初始值、资质与环境全部经 provider 路由。
  - `IsEligibleEducationMentor` 调用共享 `EducationMentorRules`；SC-020 仍拒绝退休导师。
- `Assets/Scripts/Presentation/CharacterEducationPanel.cs`
  - SC-020 与 SC-023 都自动打开同一教育面板；导师按钮使用 `DisplayLabel`。
- `Assets/Scripts/Presentation/TitleScreenController.cs`
  - 新增 `seclusionDemoButton`、`SeclusionScenarioBound`、`BindSeclusionScenarioButton`、`StartSeclusionDemo`。
- `Assets/Scripts/Persistence/SaveData.cs`
  - 计划校验读取成员角色，并接收从既有编年史派生的前任家主 ID，调用共享资格规则。
  - SC-023 保留真实前任家主的失效计划：死亡、离族或资格丧失时只要求导师 ID 已知；年结是唯一结算权威并回退自由成长。退休但未被编年史证明曾任家主的人物不得成为导师，引用其计划会被拒绝。SC-020 仍按原严格规则拒绝退休/失效导师。
  - 修正既有回退存档一致性：`UsedFreeGrowthFallback` 的自由成长结果可保留原定向计划，不再被「计划/结果不一致」误拒。
- `Assets/Scripts/Persistence/SaveDataValidator.cs`
  - 新增 `FormerHeadIds(ChronicleSaveEntry[])`，用共享的 `SuccessionHistory.IsRealHeadHandoff` 从 `category == Succession` 记录派生证明；当前版本与 v7 迁移校验都传入该集合。
- `Assets/Scripts/Domain/CharacterDevelopmentState.cs`
  - `TryRestorePlan` / `TryValidate` 接受上述显式回退结果。
- `Assets/Scripts/Editor/AddM6TitleScreen.cs`、`AddSC013TitleScenarioButtons.cs`
  - 标题根子节点计数接受可选 `SC023SeclusionDemoButton`，避免新增入口后既有 Validator 误报；SC-013 恢复路径的 `optionalEntries` 也统计该按钮，并在其存在时保持状态行 `(0,-386)`。
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
  - 接入 SC-023 Validator 行；标题引用清单新增 `seclusionDemoButton`。

### 文档

- 新增 `SC023_DEMO.md`：标题入口、退休交接、退休导师、年度结算、编年史、存读档、失效回退、SC-020 回归与证据边界。
- 更新 `PROJECT_STATE.md`、`DECISIONS.md`：记录范围、规则、存档策略与未验证状态；不声称 M3 完成。

## 静态源码证据

已执行且只做只读检查：

- `git status --short`：用户既有 WIP（`Assets/Scenes/SampleScene.unity`、`Assets/1.unity`、`Assets/1.unity.meta`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul`）保持不变；未新增/修改测试文件。
- `grep` 核对：SC-020 固定年龄、资质、环境、专长判断只保留在 `EducationScenarioProvider` 的 SC-020 分支与 `SC020EducationScenario` 自身；`GameSession` / `EducationView` 不再直接使用这些固定值。
- `grep` 核对：`new EducationPreview(...)` 全部调用点已匹配新签名；`EducationMentorOption` 唯一构造点在 `GameSession.EducationMentors`。
- `grep` 核对：`EducationMentorRules.IsEligible` 仅有两个调用点（`GameSession` 运行时、`SaveData` 存档校验），两者都传入 `hasHeldHeadRole`；运行时由 `SuccessionHistory.HasHeldHeadRole` 从 `HistoryLedger.Entries` 派生，存档由 `SaveDataValidator.FormerHeadIds` 从既有 `ChronicleSaveEntry` 派生。
- 手工审阅：`FormerHeadIds` 只接受 `ChronicleCategory.Succession`、`fromHeadId`/`toHeadId` 均非空且不同的记录；退休非前任家主的计划在 SC-023 存档校验中返回错误，不进入自由成长回退。
- 手工审阅：`AddSC013TitleScenarioButtons.Execute` 的 `optionalEntries` 已统计 `SeclusionButtonName`，且存在 SC-023 入口时状态行固定为 `(0,-386)`。
- 手工审阅：新增生成器未调用 `DestroyImmediate`、未重载场景、未修改 Prefab；所有场景写操作经 `Undo`；异常路径回滚到同一 Undo group。

## 未验证项（必须分别取证）

| 项目 | 状态 | 说明 |
|---|---|---|
| Unity 编译 | **pending / unverified** | 本 worker 未打开 Unity、未使用 batchmode、未运行任何编译器。 |
| EditMode 测试 | **pending / unverified** | 按要求未新增或运行测试。 |
| Console | **pending / unverified** | 未进入 Play Mode 或 Editor 运行态。 |
| 场景生成 | **pending** | `Assets/Scenes/FoundationScene.unity` 未修改；标题入口尚未写入场景。集成执行者需在现有 Editor 运行 `Tools/Sengoku Chronicle/Add SC-023 Seclusion`。 |
| `FoundationSceneValidator` | **pending / unverified** | 未运行；预期在场景生成前 SC-023 行会因缺少入口失败。 |
| Play Mode | **pending / unverified** | 由用户执行；本 worker 未操作 Unity。 |
| 用户验收 | **pending** | 需用户按 `SC023_DEMO.md` 试玩并明确接受。 |

## 已知风险

- `AddSC023Seclusion` 依赖已存在的 `SC020EducationRoot` 与绑定的 `CharacterEducationPanel`；若场景中 SC-020 尚未集成，SC-023 生成器会拒绝运行而不是创建第二套教育 UI。
- 标题状态行从 `(0,-330)` 下移到 `(0,-386)`；需在场景生成后由 Validator/Play Mode 确认不与标题底部裁切。
- SC-023 存档计划允许失效导师 ID 保留；这是为满足「导师死亡/离族后存读稳定并回退」的设计要求。预览/提交仍使用共享严格资格规则，因此不会把失效导师重新变为可选。
- 未取得任何 Unity 编译或运行证据；不得把本报告的静态检查表述为编译、EditMode、Console、Validator 或 Play Mode 通过。
