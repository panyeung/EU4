# SC-022 婚姻与日常收养实现报告

**日期：** 2026-11-04  
**状态：** 实现、P2 原子性修正、EditMode 测试、场景集成与 SC-022 Validator 通过；助手 Play Mode 测试通过；用户验收待办  
**设计：** `docs/superpowers/specs/2026-11-04-sc-022-family-formation-design.md`  
**计划：** `docs/superpowers/plans/2026-11-04-sc-022-family-formation.md`  
**复用研究：** `docs/research/SC022-family-formation-reuse.md`

## 范围与规则

- 新增纯 C# `FamilyFormationRules`：沿 `ParentChild` 与 `Adoption` 有向边双向判断直系关系；用 visited 集合处理既有坏环，不改变 `KinshipGraph` 的追加式语义。
- 新增 `FamilyFormationView` 只读投影：当前成年家族成员、按所选成员过滤的成年婚姻候选、按所选收养人过滤的儿童/成年收养候选。
- 新增 `SC022FamilyFormationScenario` 固定剧本：原创浅羽家，`1560-05-01` 开局；候选人注册到 `SuccessionWorld.Persons` 但不加入 `FamilyState`。
- `GameSession.TryRegisterMarriage` / `TryAdopt`：先完成全部资格、重复边、直系/循环、日期与编年史条目校验，再写成员、亲属边和一条编年史；不推进日期、不运行月结、不消耗资源或 RNG。
- 婚姻允许同一人有多位正式配偶；拒绝本人、重复配偶边、直系血亲/养亲；兄弟姐妹不额外阻止。
- 普通收养允许儿童和成年候选人；拒绝本人、正式配偶、重复同向收养边、会形成亲子链循环的候选人；保留既有血缘边，不改变继承状态。
- 新增 `ChronicleCategory.FamilyFormation`（稳定值 `7`）、`HistoryLedger.TryAppendFamilyFormation` 与预备/提交内部接口；标题/摘要使用显示名，不暴露原始 ID。

## P2 原子性修正（审查后）

独立只读审查发现：`TryRegisterMarriage` 与 `TryAdopt` 在写入成员与亲属边后，若 `TryAppendPrepared` 返回 false，会返回 `ChronicleFailed` 但未回滚成员/亲属边。修正内容：

- `KinshipGraph` 新增 `internal TryRemove(...)`，仅用于回滚当前应用层事务中尚未提交的追加边；公开 API 仍为追加式，存档表示与已提交事实不变。
- 两个命令在 append 失败时先移除刚追加的亲属边，再移除候选人成员身份，然后返回 `ChronicleFailed`；编年史因 append 未成功而不变。
- 新增 `internal GameSession.FamilyFormationAppendFaultInjection` 测试 seam。两个新用例在成员/亲属边写入后推进编年史，使真实 `TryAppendPrepared` 因预备序号失效而失败，并用快照断言成员、亲属边、编年史、继承、日期、资源与 RNG 全部不变。按构造，若回滚缺失，`TryGetRole`、`HasSpouse`/`HasAdoption` 与快照断言会失败；修正后两者通过。

## 变更文件

**新增**

- `Assets/Scripts/Domain/FamilyFormationRules.cs` + `.meta`
- `Assets/Scripts/Application/FamilyFormationView.cs` + `.meta`
- `Assets/Scripts/Application/SC022FamilyFormationScenario.cs` + `.meta`
- `Assets/Scripts/Presentation/FamilyFormationPanel.cs` + `.meta`
- `Assets/Scripts/Editor/AddSC022FamilyFormation.cs` + `.meta`
- `Assets/Tests/EditMode/FamilyFormationRulesTests.cs` + `.meta`
- `Assets/Tests/EditMode/SC022FamilyFormationScenarioTests.cs` + `.meta`
- `Assets/Tests/EditMode/FamilyFormationSessionTests.cs` + `.meta`
- `Assets/Tests/EditMode/FamilyFormationChronicleTests.cs` + `.meta`
- `Assets/Tests/EditMode/FamilyFormationPanelTests.cs` + `.meta`
- `Assets/Tests/EditMode/AddSC022FamilyFormationTests.cs` + `.meta`
- `Assets/Tests/EditMode/FamilyFormationSaveTests.cs` + `.meta`
- `SC022_DEMO.md`
- `docs/reports/sc022-implementation.md`

**修改**

- `Assets/Scripts/Application/GameSession.cs`：SC-022 初始化、视图、命令、暂停原因、消息重置；append 失败时回滚成员与亲属边，并加入测试 seam。
- `Assets/Scripts/Domain/KinshipGraph.cs`：新增 internal 回滚入口，仅用于未提交事务；公开追加式语义不变。
- `Assets/Scripts/Domain/FamilyChronicleEntry.cs`：新类别、来源常量与校验。
- `Assets/Scripts/Domain/HistoryLedger.cs`：家族关系追加与预备提交。
- `Assets/Scripts/Presentation/FamilyChroniclePanel.cs`：家族关系类别与详情标签。
- `Assets/Scripts/Presentation/TitleScreenController.cs`：SC-022 标题入口与启动方法。
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`：SC-022 面板/标题引用检查。
- `Assets/Scripts/Editor/AddM6TitleScreen.cs`：标题根计数接受 SC-022 入口。
- `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs`：标题根计数接受 SC-022 入口。
- `Assets/Scenes/FoundationScene.unity`：仅通过 `AddSC022FamilyFormation.RunMenu()` 事务式生成器更新。
- `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`：新增 SC-022 编年史 UI 用例。
- `PROJECT_STATE.md`、`DECISIONS.md`：记录本切片。

**SC-022 未修改**：`SaveData.cs`、`SaveDataMigrator.cs`、`SaveDataValidator.cs`、`Packages/manifest.json`、`Library`、`Temp`、既有 `.meta` GUID、既有场景/预制体对象。工作区中这些文件可能已有前序 SC-018–SC-021 的脏改动；本切片未叠加保存模型修改。

## 存档结论

现有 `SuccessionSaveData` 已保存全部注册人物、生命状态、家庭成员和亲属边；`SaveData` 已保存编年史。SC-022 不需要新增字段或升 schema，`saveFormatVersion` 保持 `8`、`rulesVersion` 保持 `4`。`FamilyFormationSaveTests` 证明：

- 未加入家庭的候选人仍在 `succession.persons` 中，且不在 `members` 中；
- 成功婚姻/收养后，成员角色、配偶/收养边、编年史顺序和显示文字经 JSON 往返一致；
- 旧标准存档仍可读取，且不会产生 `FamilyFormation` 记录。

## 场景集成

- 事务式生成器 `AddSC022FamilyFormation`：一个 Undo 组、保存前验证、异常 Undo 回滚、不重载场景。
- 固定根 `Canvas/SC022FamilyFormationRoot`；面板 `SC022FamilyFormationPanel` 默认隐藏；打开按钮可见；标题入口 `SC022FamilyFormationDemoButton` 位于 `M6TitleRoot`。
- 打开按钮位于 `(20,-406)`，在 SC-021 研究入口下方；标题入口位于 `(-340,-274)`，不覆盖 SC-020/SC-021 或状态行。
- 生成器已实际运行并保存 `Assets/Scenes/FoundationScene.unity`；随后在同一 Editor 运行 `FoundationSceneValidator`。

## 验证证据（分别记录）

### Unity 编译（P2 修正后）

- 在已打开的 Unity `6000.6.6f1` Editor 中通过 Unity MCP relay 的 `Unity_RunCommand` 触发刷新；项目脚本编译完成，`Editor.log` 的最终编译段无 `error CS`。
- 动态 `CommandScript` 编译成功并执行，返回 `isCompilationSuccessful=true`、`isExecutionSuccessful=true`；随后由同一 Editor 的 Test Runner 执行聚焦套件。
- `KinshipGraph.TryRemove`、`GameSession` 的回滚分支和两个新测试均通过编译与执行，未产生编译错误。

### EditMode（SC-022 聚焦，P2 修正后）

- 修正后重新运行：`70 passed / 0 failed / 0 skipped`。结果文件 `Logs/sc022-tests.txt` 与 `Logs/sc022-tests.xml`（XML 根节点 `total=70 passed=70 failed=0`）。日志行：`SC-022 focused EditMode tests finished: passed=70 failed=0 skipped=0`（`Logs/Editor.log`）。
- 其中 `FamilyFormationSessionTests` 从 10 增至 12，新增：
  - `Marriage_ChronicleAppendFailure_RollsBackMembershipKinshipAndState`
  - `Adoption_ChronicleAppendFailure_RollsBackMembershipKinshipAndState`
- 分套件：

| 套件 | 通过 |
|---|---:|
| `AddSC022FamilyFormationTests` | 8 |
| `AddM6TitleScreenTests`（标题计数回归） | 7 |
| `FamilyChroniclePanelTests`（含新增 SC-022 用例） | 13 |
| `FamilyFormationChronicleTests` | 6 |
| `FamilyFormationPanelTests` | 8 |
| `FamilyFormationRulesTests` | 8 |
| `FamilyFormationSaveTests` | 3 |
| `FamilyFormationSessionTests` | 12 |
| `SC022FamilyFormationScenarioTests` | 5 |

### EditMode（全量，修正前历史证据）

- `1098 passed / 2 failed / 0 skipped`。运行记录见 `Logs/Editor.log` 的 `SC-022 EditMode tests finished: passed=1098 failed=2 skipped=0`；摘录保存在 `Logs/sc022-all-tests-editor-log.txt`。
- 该次全量运行发生在 P2 修正之前，未在修正后重跑；P2 修正仅触及 SC-022 命令、`KinshipGraph` 回滚入口和 `FamilyFormationSessionTests`，已由上面的 70 项聚焦回归覆盖。全量结果保留为历史证据，不作为修正后的全量声明。
- 两项失败均为既有 SC-021 迁移用例，非 SC-022 代码路径：
  - `FamilyResearchSaveTests.V7PayloadWithResearch_IsRejected`
  - `SaveDataMigratorTests.V7Migration_RejectsAConflictingResearchPayload`
- 失败原因是 `SaveDataMigrator` 对 v7 载荷先报告 `development` 再报告 `research`，与测试期望的 `research` 消息顺序不一致；本 worker 未修改该迁移器，也未将其计入 SC-022 通过结论。

### Console（P2 修正后）

- 聚焦测试后 `Unity_GetConsoleLogs`（最近 15 条）：`2 errors / 2 warnings / 11 info`。
- 2 个 error 是新增失败路径用例刻意触发的 `Debug.LogError`，已被 `LogAssert.Expect` 匹配，测试本身通过：
  - `家族编年史记录写入失败（家族婚姻）：编年史预备记录序号无效。`
  - `家族编年史记录写入失败（家族收养）：编年史预备记录序号无效。`
- 2 个 warning 是负向 Editor 生成器测试的预期回滚日志：`AddM6TitleScreenTests.Updater_RollsBackWhenValidationFails` 与 SC-022 生成器回滚用例。
- 没有编译错误、未预期异常或 Validator 报错；上述 4 条均为测试路径的可解释日志，不是回归。

### FoundationSceneValidator（P2 修正后重跑）

- 活动场景：`Assets/Scenes/FoundationScene.unity`，`loaded=True dirty=False`；Validator 于 17:14 重跑，未修改场景。
- SC-022 行：`PASS SC-022 family-formation panel, title entry, bindings, and layout.`
- 整体报告：`RESULT: FAIL; Errors: 4`（`Logs/SceneValidationReport.txt`）。
- 4 项均为既有缺口，不是 SC-022 集成错误：
  1. `Missing FamilyResearchPanel.`
  2. `Expected exactly one Canvas, GameSession, FamilyResearchPanel and research root.`
  3. `Unassigned reference GameSession.researchDemoButton.`
  4. `Feedback text must be a top-anchored banner clear of the map HUD controls.`
- 前三项来自 SC-021 尚未写入 `FoundationScene`（本任务未运行 SC-021 生成器）；第四项是既有 SC-009 反馈条布局缺口。SC-022 自身 Validator 行通过，且本次修正未改场景或生成器。

### Play Mode / 用户验收

- 助手已执行 Play Mode 流程，证据见下方“Assistant Play Mode verification”；用户本人验收仍待办。
- 用户 Play Test 步骤见 `SC022_DEMO.md`；在用户明确验收前不得标记为已接受、合并、打 tag 或推送。

## 已知限制

- SC-022 候选来源固定为当前剧本注册名单；这是本切片的实现范围选择，不建立完整跨家族名册。
- 没有离婚、关系解除、妾室/婚姻等级、多配偶上限、恋爱/嫁妆/子女出生/继亲/跨家族政治。
- 普通收养不改变家主、继承阶段、指定继承人、继承次数或危机收养标记；它只通过既有继承候选规则间接影响候选资格。
- 全量 EditMode 的 2 个 SC-021 迁移失败与整体 Validator 的 4 个既有错误需要独立处理；它们不阻断 SC-022 聚焦测试与场景集成，但应作为项目级待办保留。

## Assistant Play Mode verification (2026-11-04)

由 Mochi 在当前已打开的 Unity Editor 中通过 Unity AI Assistant 命令入口执行；未使用截图或 Computer Use。Editor 为 Unity 6000.6.6f1，活动场景 `Assets/Scenes/FoundationScene.unity`。本次运行态操作通过实际标题按钮与地图按钮的 `Button.onClick`，并调用 UI 面板的选择/确认流程完成：

- 干净启动：标题可见；婚姻与收养面板默认关闭。点击 SC-022 标题入口后进入浅羽家示范，标题隐藏且面板仍关闭；点击地图入口后面板打开并显示 3 位初始可操作成员。
- 多配偶：政宗登记水无月佐保、白河千寻两位正式配偶，均各新增一名家族成员、一条配偶关系和一条家族婚姻编年史记录。
- 普通收养：政宗分别收养儿童小野寺千松与成年人小野寺佐吉；两次均成功并增加家族成员与家族收养编年史。原指定继承人和日期 `1560-05-01` 保持不变。
- 禁止关系：尝试政宗与直系血亲浅羽宗圆结婚时返回 `DirectLineRelation`；成员数、亲属边数和编年史条数均未变化。
- 存档数据恢复：对 `CaptureSave()` 结果执行 `TryApplySave()` 成功；恢复后家族成员为 6、配偶边 3 条（含开局配偶）、收养边 3 条（含既有养亲关系）、编年史 4 条，继承人与日期保持不变。打开家族编年史面板后，4 条记录均显示且顺序正确。此项验证的是会话存档数据捕获/恢复，不代表写入磁盘槽位。
- Unity Console（Play Mode 运行后）：`0 errors / 0 warnings / 0 info`。Editor 已退出 Play Mode；活动场景仍为 `FoundationScene`，`dirty=False`。

**结论：** 助手执行的 Play Mode 流程通过。随后用户明确要求合并和 push，累计 checkpoint `714cb3a` 已推送到 `origin/main`；这次集成指令未改写其他切片的验收记录，SC-022 正式验收状态仍待明确确认。未打 tag。
