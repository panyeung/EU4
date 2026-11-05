# SC-021 家族研究与技艺传承实施报告

日期：2026-11-04  
分支/工作区：`feature/sc018-independent-core-events`，`C:\Users\panye\My project`  
设计依据：`docs/superpowers/specs/2026-11-04-sc-021-family-research-design.md`  
实施计划：`docs/superpowers/plans/2026-11-04-sc-021-family-research.md`  
复用调研：`docs/research/SC021-family-knowledge-reuse-research.md`

## 1. 结论摘要

SC-021 的纯 C# 家族研究领域、月结事务、v8 存档、研究编年史、固定剧本、研究面板与事务式场景生成器已经写入当前工作区；前一轮修复了首次静态编译暴露的缺陷，本轮按只读审查 RETURN 修复了已完成技艺的 UI 状态、剧本自动弹窗、SC-020 重叠校验、短缺文案、回滚证据、暂停计划存读档与文档清单；correction review 又补齐了读档后研究者失效时的即时暂停显示、对应视图/面板测试与证据计数。**Unity Editor 编译、EditMode Test Runner、Console 计数、`FoundationScene` 生成/Validator 与 Play Mode 均未运行**，因为本 worker 没有 Unity bridge/MCP，也不允许通过截图或 Computer Use 操作 Editor。因此场景尚未接入 SC-021，用户 Play Test 与验收仍 pending。

## 2. 环境与安全边界

- Unity Editor：`C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe`
- 进程 PID `87340`，命令行 `-projectPath "C:\Users\panye\My project"`，与当前工作区一致。
- `Logs/Editor.log` 最后记录为 `Entering Playmode with Reload Domain disabled.`，之后没有可观察的退出 Play Mode 记录；因此未执行任何场景写入、场景保存或需要 Editor 焦点的操作。
- 没有启动第二个 Editor，没有运行 batchmode，没有操作 `Library`/`Temp`，没有安装 package，没有复制外部代码，没有修改 OpenCode provider/API key。
- 既有 SC-018/SC-019/SC-020 脏文件与未跟踪文件全部保留；未清理、未 reset、未 stage 无关文件。没有 commit/push/merge/tag。

## 3. 创建的文件

源码：

- `Assets/Scripts/Domain/FamilyResearchCatalog.cs`（+ `.meta`）
- `Assets/Scripts/Domain/FamilyResearchState.cs`（+ `.meta`）
- `Assets/Scripts/Domain/FamilyResearchSettlement.cs`（+ `.meta`）
- `Assets/Scripts/Application/FamilyResearchView.cs`（+ `.meta`）
- `Assets/Scripts/Application/SC021FamilyResearchScenario.cs`（+ `.meta`）
- `Assets/Scripts/Presentation/FamilyResearchPanel.cs`（+ `.meta`）
- `Assets/Scripts/Editor/AddSC021FamilyResearch.cs`（+ `.meta`）

测试：

- `Assets/Tests/EditMode/FamilyResearchCatalogTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyResearchStateTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyResearchSettlementTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyResearchSessionTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyResearchSaveTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyResearchViewTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyResearchPanelTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/AddSC021FamilyResearchTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/SC021FamilyResearchScenarioTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/SaveDataMigratorTests.cs`（+ `.meta`）

文档：

- `SC021_DEMO.md`
- `docs/reports/sc021-implementation.md`（本文件）

## 4. 修改的既有文件

- `Assets/Scripts/Application/GameSession.cs`：研究状态、研究者候选、`TrySetResearchPlan`、月结顺序（产业→研究→教育→事件→日期/提交）、回滚、捕获/恢复、固定 SC-021 入口。
- `Assets/Scripts/Domain/FamilyChronicleEntry.cs`：`ChronicleCategory.Research = 6` 与稳定 ID/进度校验。
- `Assets/Scripts/Domain/HistoryLedger.cs`：`TryAppendResearch(...)`。
- `Assets/Scripts/Presentation/FamilyChroniclePanel.cs`：研究类别名称与「进度 1 → 2」显示。
- `Assets/Scripts/Presentation/TitleScreenController.cs`：`researchDemoButton`、`BindResearchScenarioButton`、`StartResearchDemo`。
- `Assets/Scripts/Editor/AddM6TitleScreen.cs`：标题根允许第 8 个 SC-021 入口。
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`：要求并检查 `FamilyResearchPanel` 与 `AddSC021FamilyResearch.Validate`。
- `Assets/Scripts/Persistence/SaveData.cs`：`research` DTO、presence probe、捕获/恢复、v8 校验。
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`：v7→v8 空研究迁移；v1–v6 迁移链补空研究；拒绝旧版研究载荷。
- `Assets/Scripts/Persistence/SaveDataValidator.cs`：`CurrentVersion = 8`、v8 research presence/validation。
- `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`：研究完成行可读性用例。
- `Assets/Tests/EditMode/SaveDataValidatorTests.cs`：v8 research 缺失拒绝用例。
- `PROJECT_STATE.md`、`DECISIONS.md`：SC-021 状态与决策。

未修改：`Packages/manifest.json`、`ProjectSettings`（`SceneTemplateSettings.json` 仍是既有未跟踪用户文件）、`Library`、`Temp`、既有 `.meta` GUID、用户存档、`FoundationScene.unity`。

## 5. 规则与数值落地

- 目录只有一项：`research.smoke-mixing` → `technique.smoke-mixing`，显示名「烟幕调制术」，需求进度 `2`，每月成本 `1 资金 + 1 物资`，每月进度 `1`。
- 一个家族同时只有一个活动项目；替换研究者保留同项目进度；切换项目从零开始；已完成技艺不能再次研究。
- 合格研究者：存活、当前家族成员、成年（`AdulthoodAge = 16`）、未退休、未占据产业任命。
- 月结：先产业，再研究，再教育/事件排程，最后日期/提交；研究结算不读取随机流。
- 资金或物资不足、研究者失效、已记录知识：暂停/清理且不扣资源、不推进；月份仍正常前进。
- 完成时只写一条 `ChronicleCategory.Research` 记录，摘要含技艺名、研究者姓名、`1 → 2` 与完成月份；不写个人掌握。
- 存档：`saveFormatVersion = 8`、`rulesVersion = 4`；`research` 仅保存稳定 ID、进度、最后结算月与排序去重的技艺 ID；没有 `mastery` 字段。
- 固定剧本：影山家 `family.kageyama.sc021`，`1560-01-01`，宗次 40 / 奈绪 38 / 小太郎 18，开局 `2 资金 / 2 物资 / 0 情报`，不改变 SC-010/013/019/020 剧本。

## 6. 本轮修复的缺陷

首次 host-side 编译与 harness 暴露并已修复：
1. `FamilyResearchState.TryRestore` 把 `string researcherId` 当作 `PersonId` 使用，导致 `CS1061`/`CS1503`；改为 `string.IsNullOrWhiteSpace` 并用 `PersonId.TryParse` 还原。
2. `FamilyResearchPanelTests` 使用 `SaveData` 但缺少 `using SengokuChronicle.Persistence;`；已补齐。
3. `FamilyResearchSettlementTests.SecondSuccessfulMonth_CompletesAndRecordsKnowledgeOnce` 用同一个月结算第二次，与「已结算月份不得重复」规则冲突；第二次改为 `1560-02-01`。
4. `FamilyResearchSaveTests.Validation_RejectsUnknownDuplicateNegativeAndOverCapProgress` 在没有活动计划时设置进度/知识，导致超上限与冲突用例走到错误分支或直接通过；测试现在先安排一个合法研究计划，再逐项注入非法数据。
5. 产业任命暂停原因原先只写「农庄负责人」，测试要求可读的「产业」语义；现改为「已担任产业「农庄」负责人…」，同时保留领域与 Play Mode 可读性。
6. 研究面板在尚未完成时把技艺行写成「家族技艺：烟幕调制术（已记录）」，会误导玩家；现按状态显示「可研究技艺」「研究技艺（研究中）」或「家族技艺（已记录）」，并补充测试断言。
7. 生成器写入的初始技艺占位文本同步改为「可研究技艺：烟幕调制术」。

## 6A. 只读审查 RETURN 修复（2026-11-04）

审查返回 RETURN 后，本轮在同一任务上修复：

1. **已记录技艺不再伪装成可研究**：`FamilyResearchView` 新增 `CanStartPlan`；`IsKnown` 为真时清空可研究者投影，费用行改为「已记录为家族知识，无需再次研究」，面板不再生成研究者按钮。新增视图与面板测试断言无按钮、无「每月消耗」。
2. **进入 SC-021 剧本不自动弹窗**：`FamilyResearchPanel.OnSessionStateChanged` 只刷新已打开的面板，不再在剧本切换时自动 `Open()`；新增从标准剧本切入 SC-021 后仍保持关闭的测试，并验证手动打开按钮仍有效。
3. **修正生成器 SC-020 重叠校验**：改为检查实际 HUD 入口 `SC020EducationOpenButton`；标题页仍单独检查 `SC020EducationDemoButton`。新增「SC-021 打开按钮与 SC-020 HUD 打开按钮重叠时校验失败」测试。
4. **精确资源短缺文案**：只列出实际缺少的资源与数量，不再出现「物资不足 0」这类误导内容；资金/物资短缺测试分别断言另一项不出现。
5. **强化事务回滚证据**：故障注入闭包在结算提交阶段捕获事件随机流，断言事件排程已经消耗该流；失败后断言完整随机态与事件运行态恢复到结算前，并验证重试成功。
6. **暂停计划可安全存读档**：存档校验不再要求研究者仍是当前家族成员；只要是已登记人物即可。研究者离开家族、死亡、退休或担任产业职位后，计划可保存/读取并保持暂停，月结不扣资源、不推进、不授予知识；未知人物 ID 仍拒绝。新增对应的存档往返与暂停月结测试。
7. **文档更正**：`SC021_DEMO.md` 的标题入口清单改为实际的五个入口（新游戏、继续、SC-010 标准样例、SC-010 危机示范、SC-020 子女培养示范），补充不自动弹窗、已完成状态隐藏按钮/费用、暂停计划存读档说明；设计稿与计划中不存在的「SC-013 标准样例 / SC-019 产业样例」描述改为实际存在且受保护的示例。

本轮 RETURN 实际修改：`FamilyResearchView.cs`、`FamilyResearchPanel.cs`、`FamilyResearchSettlement.cs`、`AddSC021FamilyResearch.cs`、`SaveData.cs`、`FamilyResearchSettlementTests.cs`、`FamilyResearchViewTests.cs`、`FamilyResearchPanelTests.cs`、`FamilyResearchSessionTests.cs`、`FamilyResearchSaveTests.cs`、`AddSC021FamilyResearchTests.cs`、`SC021_DEMO.md`、SC-021 设计稿与实施计划、本报告。未修改场景、`ProjectSettings`、package、`Library`/`Temp`、既有 `.meta`/GUID 或无关 SC-018/019/020 文件。

## 6B. 第二次只读审查 RETURN 修复（2026-11-04）

本轮按 correction review 在同一任务修复：

1. **读档后立即显示暂停**：`FamilyResearchView` 新增只读 `IsPaused` / `PauseReason`；当活动计划的当前研究者不再满足共享资格规则（存活、当前家族、`16+`、未退休、未担任产业职位）时，`StatusLine` 与 `ReportLine` 在面板刷新时直接显示「研究暂停：…」，不等待下一次月结。`Build` 只调用 `FamilyResearchSettlement.IsEligibleResearcher(...)` 投影，不修改计划、不扣资源、不推进进度、不模拟月份。
2. **新增聚焦测试**：`FamilyResearchViewTests.SavedPlanWithFormerMemberResearcher_IsPausedWithoutSettlement` 覆盖研究者离开当前家族、无最后月结报告时立即暂停，并断言资源仍为 `2/2`、进度 `0/2`；`FamilyResearchPanelTests.Panel_SavedFormerMemberResearcher_ShowsImmediatePauseWithoutSpending` 通过 `CaptureSave`/`TryApplySave` 模拟存读档，断言面板立即显示暂停且不花费资源/不推进。面板用例依赖 Unity 运行时，本轮只做 host-side 编译，未在 Unity Test Runner 执行。
3. **证据计数更正**：`PROJECT_STATE.md` 的 SC-021 行与详情、`DECISIONS.md` 验证段从过时的存档 harness `20/20` 更正为实测 `21/21`，纯 C# 用例从 `25` 更新为 `26`（新增暂停视图用例）。
4. **计划架构句更正**：`docs/superpowers/plans/2026-11-04-sc-021-family-research.md` 明确 SC-021 使用独立固定影山家/剧本 `family.kageyama.sc021` 并在既有标题 UI 增加入口，不呈现或改动其他家族剧本。
5. **DEMO 期望保留并收紧**：`SC021_DEMO.md` 第 8 节要求读档后打开研究面板立即看到暂停原因，无需先结束月份；该期望现已由 UI 满足。

本轮修改：`Assets/Scripts/Application/FamilyResearchView.cs`、`Assets/Tests/EditMode/FamilyResearchViewTests.cs`、`Assets/Tests/EditMode/FamilyResearchPanelTests.cs`、`SC021_DEMO.md`、`docs/superpowers/plans/2026-11-04-sc-021-family-research.md`、`PROJECT_STATE.md`、`DECISIONS.md`、本报告。未修改场景、`ProjectSettings`、package、`Library`/`Temp`、既有 `.meta`/GUID 或无关 SC-018/019/020 文件；未 stage/commit/push。

## 7. 验证证据（按类型分开）

### 7.1 Host-side 静态编译（不是 Unity Editor 编译）

命令：

```text
python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile_sc021.py
```

该方法使用 Unity 6000.6.6f1 自带的 `dotnet.exe` 与 Roslyn `csc.dll`，从项目现有 Bee `.rsp` 生成响应文件，按 Domain → Persistence → Events → Application → Presentation → Editor → EditModeTests 顺序编译，并补入全部 SC-021 源文件与测试文件。

结果：

```text
== Domain exit 0
== Persistence exit 0
== Events exit 0
== Application exit 0
== Presentation exit 0
== Editor exit 0
== EditModeTests exit 0
ALL OK
```

这是宿主侧类型检查，**不是** Unity Editor 编译、Test Runner、Console、场景或 Play Mode 证据。

本轮 correction 的复跑日志：`C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/sc021-correction-compile.log`（7 个程序集全部 exit 0，`ALL OK`）、`sc021-correction-domain.log`（`passed=70 failed=0`）、`sc021-correction-save.log`（`compile exit 0` / `run exit 0` / `passed=21 failed=0`）、`sc021-correction-pure.log`（`passed=26 failed=2`，两个失败均为宿主缺少 `UnityEngine.ScriptingModule` 的 `GameSession` 用例）。

### 7.2 Host-side 纯 C# 领域与存档 harness（不是 Unity Test Runner）

使用同一 Unity 随附 Roslyn 编译两个独立 harness，引用刚编译的 `SengokuChronicle.Domain.dll` / `SengokuChronicle.Persistence.dll`。

领域 harness 覆盖目录、状态、替换研究者、完成、克隆独立性、存档恢复拒绝、资金/物资短缺、死亡/退休/未成年/非成员/产业任命暂停、同月重复、月份缺口、未来月份与确定性输入不修改：

```text
compile exit 0
run exit 0
passed=70 failed=0
```

存档 harness 覆盖 v8 默认校验、缺失 `research`、未知/重复技艺、负数/超上限进度、计划/知识冲突、未知研究者、缺失知识列表、v7→v8 迁移、完成知识的捕获/恢复，以及研究者离开家族后的暂停计划仍可保存：

```text
compile exit 0
run exit 0
passed=21 failed=0
```

另用宿主侧反射 runner 执行纯测试夹具（不加载 Unity 运行时）：`FamilyResearchCatalogTests`、`FamilyResearchStateTests`、`FamilyResearchSettlementTests`、`FamilyResearchViewTests` 与 `SC021FamilyResearchScenarioTests` 的纯 C# 用例，`26 passed / 2 failed`；两个失败均为 `SC021FamilyResearchScenarioTests` 的 `GameSession` 用例，错误为 `Could not load file or assembly 'UnityEngine.ScriptingModule'`（宿主缺少 Unity 运行时），未计入通过数。新增的 `FamilyResearchPanelTests` 用例已通过 host-side 编译，但该夹具依赖 Unity MonoBehaviour/Editor Scene API，未在宿主 runner 中执行。

这些 harness/runner 是宿主侧逻辑验证，**不是** Unity EditMode Test Runner，也不覆盖 MonoBehaviour、场景绑定或存档 JSON 的 Unity 运行时。

### 7.3 Unity Editor 编译

**未运行。** `Logs/Editor.log` 最后一条 Play Mode 记录之后没有新的脚本刷新或编译记录；本轮新增文件在 Editor 中尚未导入。不得据 7.1 声称 Unity 编译通过。

### 7.4 Unity EditMode Test Runner

**未运行。** 没有安全、非 GUI 的路径驱动已打开的 Editor；没有 `TestResults.xml` 或 Passed/Failed/Skipped 计数。新增测试源已通过 7.1 类型检查，但没有在 Unity 中执行。

### 7.5 Console

**未运行/未统计。** 没有读取或重置 Play Mode Console；不声明 0 errors。

### 7.6 FoundationScene 生成与 Validator

**未运行，场景未修改。** `AddSC021FamilyResearch` 已实现并通过 host-side 编译，但：

- Editor 日志显示仍处于 Play Mode（无退出记录）；
- 无法在不操作 GUI 的前提下确认场景是否 dirty；
- 生成器按设计拒绝 Play Mode 或 dirty 场景；
- 因此没有执行 `Tools/Sengoku Chronicle/Add SC-021 Family Research`，没有保存场景，也没有运行 `FoundationSceneValidator`。

当前 `FoundationScene.unity` 仍不包含 `SC021ResearchRoot`/`SC021ResearchPanel`/`SC021ResearchDemoButton`；这是已知的集成缺口，不是已验证通过项。

### 7.7 Play Mode 与用户验收

**未运行。** 按项目规则，Play Test 由用户执行并报告；本 worker 不使用截图、Computer Use 或 UI 自动化操作 Unity，也不代替用户验收。`SC021_DEMO.md` 第 0 节列出了集成人员必须先完成的生成/Validator 步骤。

## 8. 未解决项与未验证行为

1. **场景未集成**：必须在当前干净的 `FoundationScene` 中由具备 Editor 操作的集成人员运行 `Add SC-021 Family Research`，保存后再运行 `Validate Foundation Scene`。在完成前，标题页不会出现 SC-021 入口，研究面板不可用。
2. **Unity 编译与 EditMode 未验证**：7.1/7.2 不能替代 Unity 的编译、Test Runner、Console 或 Play Mode 结果。
3. **运行时 UI 绑定未验证**：面板按钮、研究者克隆、标题入口、Canvas sibling 顺序、raycast 与重叠检查只在源码/测试设计层面存在，尚未在真实场景执行。
4. **存档 JSON 运行时未验证**：v8 捕获/恢复、v7→v8 迁移与 presence probe 只经过静态类型检查与领域 harness；没有 Unity `JsonUtility` 运行时证据。
5. **Play Mode 分支未验证**：资金/物资短缺、无效研究者、完成/编年史、读档、继承保留知识均需用户按 `SC021_DEMO.md` 执行。
6. **继承 UI 在 SC-021 固定家族中的可操作性未验证**：领域/会话测试使用标准剧本覆盖了继承后知识保留，但 SC-021 固定家族的继承按钮/流程尚未在 Play Mode 实测。
7. **QATester 同任务复审未发起**：当前 OpenCode harness 未暴露 `kanban_request_review(reviewer="qatester")` / `kanban_complete` 工具，无法按 `AGENTS.md` 在同一任务上发起复审；需由协调方在 Kanban 中创建/转交同任务复审，不应另建 QA 卡。本轮已按独立只读审查的 RETURN 意见修复 7 项问题，但仍需新的同任务复审与顺序只读复审。

## 9. 工作区保护

- 只新增 SC-021 文件、其 `.meta` 与三份文档；修改的既有文件均属于 SC-021 接线或测试。
- 未 stage/commit/push/merge/tag。
- 未清理或覆盖既有 SC-018/SC-019/SC-020 脏改动、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul`。
- 未修改 `Library`、`Temp`、package manifest、既有 `.meta` GUID 或 OpenCode 配置。

## 10. 下一步（协调方）

1. 让具备 Editor 操作的集成人员确认非 Play Mode、场景干净，执行 `Tools/Sengoku Chronicle/Add SC-021 Family Research`，保存 `FoundationScene`。
2. 运行 `Tools/Sengoku Chronicle/Validate Foundation Scene`，确认 `PASS SC-021 family research panel...` 与 `RESULT: PASS; Errors: 0`。
3. 在 Editor Test Runner 运行聚焦/全量 EditMode，记录实际 totals 与 Console 计数。
4. 交给用户按 `SC021_DEMO.md` 执行 Play Test；用户明确接受前不得提交、合并、打 tag 或推送。
5. 按 `AGENTS.md` 在同一任务上请求 QATester 复审；复审通过后再由独立的 OpenCode worker 做只读审查。
