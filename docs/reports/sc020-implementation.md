# SC-020 子女培养与人物成长实现报告

日期：2026-11-04  
实现者：OpenCode2（唯一写入者；未使用 Unity GUI/Computer Use）  
设计：`docs/superpowers/specs/2026-11-04-sc-020-character-growth-education-design.md`  
计划：`docs/superpowers/plans/2026-11-04-sc-020-character-growth-education.md`  
试玩：`SC020_DEMO.md`

## 1. 结论与边界

已写入 SC-020 的纯 C# 成长规则、按稳定 PersonId 的成长/计划/结算状态、独立青叶家固定剧本、`GameSession` 年度月结事务、v7 存档与 v6 迁移、编年史教育类别、标题入口、培养面板、事务式场景生成器与 Validator 扩展，以及对应 EditMode 测试源码。

**未执行**：`FoundationScene` 场景生成/保存、`FoundationSceneValidator`、Unity EditMode Test Runner、Console 计数、Play Mode。原因：本 worker 没有可用的 Unity bridge/MCP，也不允许用 Computer Use/截图；`Logs/Editor.log` 最后可见 `Entering Playmode with Reload Domain disabled.`，之后无法在不碰 GUI 的前提下确认当前是否仍在 Play Mode 或场景是否 dirty。协调方 2026-11-04 明确要求此时停在 gate 并报告，不得启动第二个 Editor 或 batchmode。

因此本报告只把以下内容作为已完成证据：

- 源码与测试源码静态编译（Unity 随附 Roslyn，host-side，不是 Unity Editor 编译）；
- 纯 Domain 离线 harness 17/17；
- 数值与事务设计的人工静态核对。

**不是** EditMode 通过、场景 Validator 通过、Console 干净、Play Mode 或用户验收。

## 2. 变更文件

新增源码：

- `Assets/Scripts/Domain/CharacterGrowth.cs`（+ `.meta`）
- `Assets/Scripts/Domain/CharacterDevelopmentState.cs`（+ `.meta`）
- `Assets/Scripts/Application/SC020EducationScenario.cs`（+ `.meta`）
- `Assets/Scripts/Application/EducationView.cs`（+ `.meta`）
- `Assets/Scripts/Presentation/CharacterEducationPanel.cs`（+ `.meta`）
- `Assets/Scripts/Editor/AddSC020CharacterEducation.cs`（+ `.meta`）

修改源码：

- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- `Assets/Scripts/Domain/HistoryLedger.cs`
- `Assets/Scripts/Persistence/SaveData.cs`
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- `Assets/Scripts/Persistence/SaveDataValidator.cs`
- `Assets/Scripts/Presentation/TitleScreenController.cs`
- `Assets/Scripts/Presentation/FamilyChroniclePanel.cs`（教育类别中文名与前后值/自由成长显示）
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs`（允许新增一个 SC-020 标题按钮，不改 SC-013 行为）
- `Assets/Scripts/Editor/AddM6TitleScreen.cs`（标题子控件计数允许 SC-013/SC-020 入口）

新增测试：

- `Assets/Tests/EditMode/CharacterGrowthRulesTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/CharacterDevelopmentStateTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/SC020EducationSessionTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/CharacterGrowthSaveTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/CharacterEducationPanelTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/AddSC020CharacterEducationTests.cs`（+ `.meta`）

修改测试：

- `Assets/Tests/EditMode/SaveDataValidatorTests.cs`（ValidData 补齐 v7 development）
- `Assets/Tests/EditMode/IndustrySaveTests.cs`（v5 迁移断言改为 CurrentVersion/CurrentRulesVersion）

文档：

- `SC020_DEMO.md`（新增）
- `docs/reports/sc020-implementation.md`（本文件）
- `PROJECT_STATE.md`（追加 SC-020 状态）
- `DECISIONS.md`（追加 SC-020 决策与证据边界）

未修改：`Packages/manifest.json`、`Library`、`Temp`、任何现有 `.meta` GUID、用户存档、场景/预制体。`FoundationScene.unity` 仍是工作区原有未提交改动，未由本 worker 保存或重写。所有既有 SC-018/SC-019 脏改动均保留。

## 3. 设计与数值落地

- 五项能力：`GrowthAttribute` = 忍术/武艺/学问/经营/社交，整数 `0..100`。
- 方向：五项定向 + `FreeGrowth`；导师专长是独立枚举，不与人物身份硬编码耦合到规则层。
- 定向公式：对应属性 `+4`；资质匹配 `+2`；导师专长匹配 `+2`；有利环境 `+1`；结果夹 `0..100`。
- 自由成长：五项各 `+1`，无导师，不读取随机流。
- SC-020 固定数据：`family.aoba.sc020`，宗一郎 1520/家主/武艺导师，小夜 1522/配偶/学问导师，千景 1545/直系亲属/学问资质；开局 `1560-01-01`；千景 `15/12/20/10/13`；家庭环境有利。
- 已知组合：学问 + 小夜 = `20→29`；学问 + 宗一郎 = `20→27`。
- 计划按 `(PersonId, AgeYear)` 保存；同年度可替换，结算年度不可再改；单年只结算一次。
- 未提交计划的 0..15 岁年度按自由成长；导师失效时按自由成长结算并记录原因，避免月结永久卡死。
- `CharacterDevelopmentState` 的提交只在克隆上验证后整体替换；重复结算、未知人物、非法年龄/方向/导师均显式失败且不部分修改。

## 4. 月结事务

`GameSession.TryAdvanceToNextMonth()` 在既有事务中新增：

1. 产业结算计划（既有）；
2. 若 `previousDate.Month == 12`，在成长状态克隆上结算所有 0..15 岁年度并追加教育编年史；
3. 排程下一月事件（既有）；
4. 日期推进与提交（既有）；
5. 故障注入点之后一并提交 `_development`。

失败/异常路径恢复：日期、产业、成长状态、随机流、事件运行态、编年史、产业报告与 `LastEducationMessage`。故障后允许用相同计划重试，且只产生一条教育记录。

结算记录：`ChronicleCategory.Education`，`SubjectId = PersonId`，`SourceId = education.<direction>`，`FromValue/ToValue` 为定向属性前后值（自由成长为 0/0，摘要含五项变化），标题只显示姓名与“年度培养”，摘要含方向、导师姓名、五项前后值与原因。

## 5. 存档 v7 / rules 3

- `SaveData.saveFormatVersion` 7，`rulesVersion` 3；`development` 为必填根字段。
- `CharacterDevelopmentSaveData` 按排序后的 PersonId 保存五项能力、每年度计划与结果；`Capture`/`TryRestore` 只处理纯 DTO 与领域状态。
- `SaveDataValidator`：要求 development 人物集合与 succession 名册完全一致；拒绝未知/重复/乱序人物、越界能力、非法方向、非法导师、重复年度、计划/结果不一致、能力与最近结果不一致。
- v6→v7：保留 SC-018 家庭关系、chronicle、eventRuntime、succession、industry 全部原值；为每个已登记人物添加五项 `0` 与空教育记录；不补算、不写编年史。
- v1–v5 继续走既有迁移链到 v7；旧载荷若包含非 null `development` 字段则拒绝，不用空记录静默替换。
- v7 presence 使用 raw JSON 的 root-property 检查区分“缺字段”和“显式 null”。

## 6. 场景与 UI 设计（已写源码，未执行）

- 标题：`TitleScreenController.BindEducationScenarioButton` / `StartEducationDemo()`；原有 `StartNewGame`、`StartStandardSample`、`StartCrisisDemo` 不变。
- 面板：`CharacterEducationPanel.BindReferences(...)` 完全按计划签名；方向/导师按钮每次刷新先清理旧行与 listener；预览调用只读 `PreviewEducation`；确认只调用 `TrySetEducationPlan`。
- 生成器：`AddSC020CharacterEducation` 使用单一 Undo 组、场景路径/脏状态/Play Mode preflight、完整 Validate 后才保存、异常 `Undo.RevertAllDownToGroup`；控件位于单一 `SC020EducationRoot`；标题按钮与被移动的状态行在同一 Undo 事务内。
- 布局：培养打开按钮左上锚定 `(20,-302)`，位于 SC-010 HUD（底 `-230`）与 SC-019 产业按钮（`-250..-294`）下方；面板 760×560，方向/导师模板隐藏，消息行初始隐藏。
- `FoundationSceneValidator` 已接入 SC-020 检查与 `educationDemoButton` 引用检查；`AddSC013TitleScenarioButtons` 与 `AddM6TitleScreen` 的标题子项计数已兼容第 7 个入口。

## 7. 验证证据（host-side，明确非 Editor）

环境：

- Unity Editor 进程：PID `62256`，`C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe -projectPath "C:\Users\panye\My project"`；PID `63816` 是其 AssetImportWorker，不是第二个 Editor。
- 编辑器项目日志：`C:\Users\panye\My project\Logs\Editor.log`；标准日志：`%LOCALAPPDATA%\Unity\Editor\Editor.log`。
- 当前 `Logs/Editor.log` 最后可见 Play Mode 记录为 `Entering Playmode with Reload Domain disabled.`，之后无退出记录，无法据此证明当前 Play Mode/dirty 状态。

静态编译（host-side，使用 Unity 随附 Roslyn）：

```text
Unity 6000.6.6f1 Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll
SengokuChronicle.Domain        exit 0
SengokuChronicle.Events        exit 0
SengokuChronicle.Persistence   exit 0
SengokuChronicle.Application   exit 0
SengokuChronicle.Presentation  exit 0
SengokuChronicle.Editor        exit 0
SengokuChronicle.EditModeTests（全部现有 + 新增测试源码） exit 0
```

说明：以上为独立 host 编译，使用 Unity 安装目录的参考程序集；**不是** Unity Editor 的脚本重编译，也不证明 Console 清白。

纯 Domain 离线 harness：

```text
SC020_DOMAIN_HARNESS PASS checks=17
```

覆盖：`+9`、`+7`、自由成长五项 `+1`、`99→100` 夹值、拒绝非法初值/方向、计划创建限制、状态结算一次、重复结算拒绝且不改值、`TryValidate`、克隆隔离。

纯 Persistence 离线 harness：

```text
SC020_SAVE_HARNESS PASS checks=20
```

覆盖：v7 空成长存档通过校验、三人成长行、结算后 capture→restore（能力 29、计划导师保留）、v6 迁移到 v7 且不伪造历史、缺成长行/越界能力/null development 拒绝。该 harness 不经过 `GameSession` 或 Unity 序列化。

未执行且不得声明的证据：

- Unity EditMode Test Runner / `TestResults.xml` totals：**未运行**。
- `FoundationSceneValidator` / `Logs/SceneValidationReport.txt`：**未运行**。
- Console errors/warnings/info：**未读取**（当前 Editor 状态无法安全确认）。
- Play Mode / 用户 Play Test：**未执行**。

## 8. 复现验证命令（集成人员）

在确认 Editor 不在 Play Mode、场景无未保存用户改动且 Console 无编译错误后：

1. 菜单：`Tools/Sengoku Chronicle/Add SC-020 Character Education`。
2. 菜单：`Tools/Sengoku Chronicle/Validate Foundation Scene`，检查 `Logs/SceneValidationReport.txt` 的 SC-020 PASS 行与 `RESULT: PASS; Errors: 0`。
3. EditMode Test Runner → Run All；重点类：
   - `CharacterGrowthRulesTests`
   - `CharacterDevelopmentStateTests`
   - `SC020EducationSessionTests`
   - `CharacterGrowthSaveTests`
   - `CharacterEducationPanelTests`
   - `AddSC020CharacterEducationTests`
   - 既有 `SaveDataValidatorTests`、`SuccessionSaveTests`、`IndustrySaveTests`、`WorldClockTests`、`AddSC013TitleScenarioButtonsTests`
4. 记录实际 totals、Console 计数、场景报告路径。
5. 再按 `SC020_DEMO.md` 第 1–11 节交给用户 Play Test。

## 9. 已知风险与未覆盖

- 本 worker 环境未提供 `kanban_request_review` 工具；同任务 QA 需由协调方按 Orca/Kanban 流程路由给 QATester。QA 通过前不得把本切片视为完成。
- 场景生成器与 Validator 尚未在真实 `FoundationScene` 上执行；布局/绑定/兄弟顺序可能需要在执行后按 Validator 报告做一次事务式修复。
- EditMode 测试未运行；离线编译只能证明类型与语法，不证明 NUnit 断言在 Unity 内通过。
- `AddSC013TitleScenarioButtons` / `AddM6TitleScreen` 的计数容错已按第 7 个子项设计，但真实场景尚未验证。
- 教育结算对所有 0..15 岁、存活、名册内且已有成长记录的人物生效；SC-010 标准样例的未成年人物在跨年时也会按自由成长结算。这不会改变 SC-010/SC-013 既有入口或事件，但属于本切片新增的规则面，尚未做 Play Mode 观察。
- 年度结算的“先产业、后教育、再事件、最后日期/提交”顺序已按设计写入事务；故障回滚测试源码已覆盖，但未在 Unity 内执行。
- 用户验收、提交、推送、合并、tag 均未进行。

## 10. 工作区保护

- 未 stage、未 commit、未 push、未 merge、未 tag。
- 保留所有既有未提交 SC-018/SC-019 改动、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul`、旧文档等。
- 未修改 `Library`、`Temp`、`Packages/manifest.json`、既有 `.meta` GUID；仅为新增文件添加新 `.meta`。


## 后续 Git 集成（2026-11-04）

用户明确要求累计合并并推送后，SC-020 随 SC-018–SC-022 checkpoint `714cb3a` 推送到 `origin/main`。Editor EditMode、场景生成/Validator、Console、Play Mode 与用户验收未由这次 Git 集成补做。
