# SC-021 家族研究与技艺传承实施报告

日期：2026-11-04  
分支/工作区：`panyeung/sc021-scene-integration`，`C:\Users\panye\orca\workspaces\My project\sc021-scene-integration`（场景集成与复审 follow-up 均在此 worktree）
设计依据：`docs/superpowers/specs/2026-11-04-sc-021-family-research-design.md`  
实施计划：`docs/superpowers/plans/2026-11-04-sc-021-family-research.md`  
复用调研：`docs/research/SC021-family-knowledge-reuse-research.md`

## 1. 结论摘要

SC-021 的纯 C# 家族研究领域、月结事务、v8 存档、研究编年史、固定剧本、研究面板与事务式场景生成器已经写入当前工作区；前一轮修复了首次静态编译暴露的缺陷，随后按只读审查 RETURN 修复了已完成技艺的 UI 状态、剧本自动弹窗、SC-020 重叠校验、短缺文案、回滚证据、暂停计划存读档与文档清单；correction review 又补齐了读档后研究者失效时的即时暂停显示、对应视图/面板测试与证据计数。**2026-11-04 场景集成轮已在独立 worktree Editor 中运行生成器、保存 `FoundationScene`、运行 Validator 与聚焦 EditMode，并修复了 v7 载荷拒绝顺序导致的两个既有测试失败**；复审 follow-up 又补齐正向 v7 JSON 迁移回归测试，并在同一 worktree 复跑聚焦 `126/126`、全量 EditMode `1103/1103`。Unity 编译、EditMode、SC-021 Validator PASS 与 Console 计数见第 7 节。用户 Play Test 与正式验收仍 pending。

## 2. 环境与安全边界

- Unity Editor：`C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe`。
- 用户已打开的 Editor：PID `87340`，命令行 `-projectPath "C:\Users\panye\My project"`；本集成 worker 未使用、未修改该 checkout。
- 场景集成 Editor：2026-11-04 为本任务 worktree `C:\Users\panye\orca\workspaces\My project\sc021-scene-integration` 单独启动的 Editor（进程 PID `64820`，日志 `Logs/editor-sc021-integration.log`），确认该 worktree 此前没有 Editor 打开、用户 checkout 的 Editor 仍指向 `C:\Users\panye\My project` 后才启动；完成后已关闭。没有启动 batchmode，没有运行 Play Mode。
- 通过本机 Unity AI relay（`Unity_RunCommand`）执行 `AddSC021FamilyResearch.RunMenu()`、`FoundationSceneValidator.ValidateActiveScene()` 与聚焦 Test Runner；未使用截图或 Computer Use。
- 没有操作 `Library`/`Temp`（worktree 的 `Library` 为本次 Editor 首次导入生成，已被 `.gitignore` 忽略），没有安装 package，没有复制外部代码，没有修改 OpenCode provider/API key。
- 既有 SC-018/SC-019/SC-020/SC-022 场景内容保留；排序比较显示唯一被替换的基线行是 `TitleScreenController` 的 `researchDemoButton: {fileID: 0}`（改为非零引用），没有删除或改名既有场景对象。
- 未合并 `main`、未打 tag；本切片已验证文件提交并推送到 feature 分支 `panyeung/sc021-scene-integration`。

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
- `Assets/Scripts/Editor/AddM6TitleScreen.cs`：标题根允许新增 SC-021 入口，并保持既有入口计数校验。
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`：要求并检查 `FamilyResearchPanel` 与 `AddSC021FamilyResearch.Validate`。
- `Assets/Scripts/Persistence/SaveData.cs`：`research` DTO、presence probe、捕获/恢复与研究块范围校验。
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`：v7→v8 空研究迁移；v1–v6 迁移链补空研究；拒绝旧版研究载荷。
- `Assets/Scripts/Persistence/SaveDataValidator.cs`：`CurrentVersion = 8`、v8 research presence/validation。
- `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`：研究完成行可读性用例。
- `Assets/Tests/EditMode/SaveDataValidatorTests.cs`：v8 research 缺失拒绝用例。
- `PROJECT_STATE.md`、`DECISIONS.md`：SC-021 状态与决策。
- `Assets/Scenes/FoundationScene.unity`：场景集成轮由事务式生成器写入并保存 SC-021 根节点、隐藏面板、研究者模板、文本行与标题入口；未手工编辑 YAML。

未修改：`Packages/manifest.json`、`ProjectSettings`、`Library`、`Temp`、既有 `.meta` GUID、用户存档、其他场景/预制体。

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
7. **文档更正**：`SC021_DEMO.md` 的标题入口清单改为当时实际存在的入口（新游戏、继续、SC-010 标准样例、SC-010 危机示范、SC-020 子女培养示范；后续 SC-022 集成另增「SC-022 婚姻与收养示范」），补充不自动弹窗、已完成状态隐藏按钮/费用、暂停计划存读档说明；设计稿与计划中不存在的「SC-013 标准样例 / SC-019 产业样例」描述改为实际存在且受保护的示例。

本轮 RETURN 实际修改：`FamilyResearchView.cs`、`FamilyResearchPanel.cs`、`FamilyResearchSettlement.cs`、`AddSC021FamilyResearch.cs`、`SaveData.cs`、`FamilyResearchSettlementTests.cs`、`FamilyResearchViewTests.cs`、`FamilyResearchPanelTests.cs`、`FamilyResearchSessionTests.cs`、`FamilyResearchSaveTests.cs`、`AddSC021FamilyResearchTests.cs`、`SC021_DEMO.md`、SC-021 设计稿与实施计划、本报告。未修改场景、`ProjectSettings`、package、`Library`/`Temp`、既有 `.meta`/GUID 或无关 SC-018/019/020 文件。

## 6B. 第二次只读审查 RETURN 修复（2026-11-04）

本轮按 correction review 在同一任务修复：

1. **读档后立即显示暂停**：`FamilyResearchView` 新增只读 `IsPaused` / `PauseReason`；当活动计划的当前研究者不再满足共享资格规则（存活、当前家族、`16+`、未退休、未担任产业职位）时，`StatusLine` 与 `ReportLine` 在面板刷新时直接显示「研究暂停：…」，不等待下一次月结。`Build` 只调用 `FamilyResearchSettlement.IsEligibleResearcher(...)` 投影，不修改计划、不扣资源、不推进进度、不模拟月份。
2. **新增聚焦测试**：`FamilyResearchViewTests.SavedPlanWithFormerMemberResearcher_IsPausedWithoutSettlement` 覆盖研究者离开当前家族、无最后月结报告时立即暂停，并断言资源仍为 `2/2`、进度 `0/2`；`FamilyResearchPanelTests.Panel_SavedFormerMemberResearcher_ShowsImmediatePauseWithoutSpending` 通过 `CaptureSave`/`TryApplySave` 模拟存读档，断言面板立即显示暂停且不花费资源/不推进。面板用例依赖 Unity 运行时，本轮只做 host-side 编译，未在 Unity Test Runner 执行。
3. **证据计数更正**：`PROJECT_STATE.md` 的 SC-021 行与详情、`DECISIONS.md` 验证段从过时的存档 harness `20/20` 更正为实测 `21/21`，纯 C# 用例从 `25` 更新为 `26`（新增暂停视图用例）。
4. **计划架构句更正**：`docs/superpowers/plans/2026-11-04-sc-021-family-research.md` 明确 SC-021 使用独立固定影山家/剧本 `family.kageyama.sc021` 并在既有标题 UI 增加入口，不呈现或改动其他家族剧本。
5. **DEMO 期望保留并收紧**：`SC021_DEMO.md` 第 8 节要求读档后打开研究面板立即看到暂停原因，无需先结束月份；该期望现已由 UI 满足。

本轮修改：`Assets/Scripts/Application/FamilyResearchView.cs`、`Assets/Tests/EditMode/FamilyResearchViewTests.cs`、`Assets/Tests/EditMode/FamilyResearchPanelTests.cs`、`SC021_DEMO.md`、`docs/superpowers/plans/2026-11-04-sc-021-family-research.md`、`PROJECT_STATE.md`、`DECISIONS.md`、本报告。未修改场景、`ProjectSettings`、package、`Library`/`Temp`、既有 `.meta`/GUID 或无关 SC-018/019/020 文件；未 stage/commit/push。

## 6C. 场景集成与 v7 拒绝顺序修复（2026-11-04）

本轮在任务 worktree 完成场景收尾，并修复验证发现的存档迁移缺陷：

1. **FoundationScene 集成**：在独立 worktree Editor 中打开 `Assets/Scenes/FoundationScene.unity`，执行 `Tools/Sengoku Chronicle/Add SC-021 Family Research`。生成器在一个 Undo 组内创建 `Canvas/SC021ResearchRoot`、隐藏的 `SC021ResearchPanel`、`SC021ResearchOpenButton`、研究者容器/模板、全部文本行与 `M6TitleRoot/SC021ResearchDemoButton`，验证后保存。场景由 `52d49020…` 变为 `3d12886f…`；排序行比较仅 `researchDemoButton` 从 `fileID: 0` 变为非零引用，无既有对象删除/改名；`AddSC022FamilyFormationTests`、SC-020/019/013/M6 的 Validator 行仍 PASS。
2. **Validator**：`FoundationSceneValidator` 输出 `PASS SC-021 family-research panel, title entry, bindings, and layout.`，活动场景 `Loaded: True; Dirty: False`。整体 `RESULT: FAIL; Errors: 1`，唯一错误是既有 SC-009 反馈条位置检查（`Feedback text must be a top-anchored banner clear of the map HUD controls.`），不属于本切片，未修改。
3. **v7 载荷拒绝顺序**：SC-022 记录的两个既有失败用例（`FamilyResearchSaveTests.V7PayloadWithResearch_IsRejected`、`SaveDataMigratorTests.V7Migration_RejectsAConflictingResearchPayload`）根因是 v7 已拥有 `development`，但通用遗留检查先以 `development` 拒绝，导致本应报告的 `research` 消息不出现。将 `SaveDataValidator.ValidatePresence` 与 `SaveDataMigrator` 的通用 `development` 拒绝范围收窄到 v2–v6；v7 继续由版本专属分支先检查 `research`。未改变 v8 schema、研究规则或迁移数据。
4. **证据边界**：Unity 编译、EditMode 与 Validator 均由本轮 worktree Editor 实际执行；Play Mode 仍未运行，用户 Play Test 与验收仍 pending。

## 6D. 复审 follow-up：v7 正向迁移回归与证据补齐（2026-11-04）

独立复审 ACCEPT 后，本轮只处理复审指出的正向 v7 迁移测试缺口与 SC-021 文档交叉引用/措辞问题：

1. **新增正向 v7 JSON 回归测试**：`SaveDataMigratorTests.V7ToV8Migration_AcceptsLegitimateDevelopmentJsonAndPreservesIt` 先由 `InitEducationDemoScenario` 取得带真实成长值的存档，再构造与真实 v7 文件一致的 JSON（保留 `industry`、`development` 等 v7 已有块，完全没有 `research` 根属性）。测试同时走 `ValidatePresence` 与 `TryMigrate(parsed, json, ...)`，断言迁移成功、`saveFormatVersion=8`、`rulesVersion=4`、研究为空且 `lastSettledMonthIndex` 等于当前产业已结算月，并逐项比较 `development`、`industry`、`familyRelationships`、`chronicle` 的 JSON 不变。该测试在收窄 `development` 拒绝范围前会以“旧版存档包含人物成长状态（development）”失败。既有 `FamilyResearchSaveTests.V7PayloadWithResearch_IsRejected` 与 `SaveDataMigratorTests.V7Migration_RejectsAConflictingResearchPayload` 保留，v7 携带 `research` 仍被拒绝。**生产逻辑未改动**：新测试证明当前迁移路径正确，无需修代码。
2. **Unity 复跑**：在同一 worktree 上用 `C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe -batchmode -runTests -testPlatform EditMode` 执行。单测 `1/1`、聚焦套件 `126/126`、全量 EditMode `1103/1103`，均 `0 failed / 0 skipped / 0 inconclusive`；三份日志的 `error CS` 计数均为 0。结果文件：`Logs/sc021-followup-v7-test.xml`、`Logs/sc021-followup-focused.xml`、`Logs/sc021-followup-full.xml`（日志为同名 `.log`）。这是 Unity Test Runner 结果，不是 Play Mode 证据。
3. **进程与安全边界**：执行前检查到用户 Editor PID `87340` 的 `-projectPath` 仍为 `C:\Users\panye\My project`，另有其 AssetImportWorker（PID `89572`，同一 canonical 项目）。本轮只对本 worktree 启动临时 batchmode 进程，未对用户 checkout 运行 batchmode，未使用截图/Computer Use，未进入 Play Mode；batchmode 退出后没有为本 worktree 留下 Editor。batchmode 不呈现 Editor Console UI，因此本轮没有新增“Console 最近条目”计数，也不把日志行数冒充 Console 统计。
4. **文档修正**：实施报告头部改为当前 feature 分支/worktree；实施计划把过时的 `feature/sc018-independent-core-events` 与“使用已打开 Editor”改为当前分支及“先核对 PID/`-projectPath`、只使用本 worktree 的 Editor/临时 batchmode、绝不针对用户已打开项目跑 batchmode”的安全路径；设计稿月结顺序补上“研究”，并把“用户验收前不提交/push”改为与项目规则一致（可提交并推送 feature 分支，验收前不合并 `main`、不打 tag）。

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

### 7.3 Unity Editor 编译（场景集成轮）

**已运行。** 在任务 worktree 的独立 Editor（PID `64820`，`Logs/editor-sc021-integration.log`）中首次导入后，Unity Tundra 构建成功：`*** Tundra build success (40.91 seconds), 1962 items updated, 1975 evaluated`；日志中 `error CS` 计数为 0。该 Editor 同时通过 relay 成功编译并执行生成器、Validator 与测试命令脚本。**这是 worktree 的 Unity 编译证据，不是用户 checkout 的 Editor 结果。**

### 7.4 Unity EditMode Test Runner（场景集成轮）

**已运行（聚焦与全量）。** 场景集成轮在 worktree Editor 的 Test Runner 运行 SC-021 及受影响套件：`125 passed / 0 failed / 0 skipped / 0 inconclusive`。复审 follow-up 在同一 worktree 通过 batchmode Test Runner 复跑：新增单测 `1/1`、聚焦套件 `126/126`、全量 EditMode `1103/1103`，均为 `0 failed / 0 skipped / 0 inconclusive`；结果见 `Logs/sc021-followup-v7-test.xml`、`Logs/sc021-followup-focused.xml` 与 `Logs/sc021-followup-full.xml`。场景集成轮的结果文件：

- `Logs/sc021-integration-tests.txt`（`SUMMARY 125 0 0 0` 与逐项结果）
- `Logs/sc021-integration-tests.xml`（`%LOCALAPPDATA%Low\DefaultCompany\My project\TestResults.xml` 的副本）

覆盖 `FamilyResearchCatalogTests`、`FamilyResearchStateTests`、`FamilyResearchSettlementTests`、`FamilyResearchSessionTests`、`FamilyResearchSaveTests`、`FamilyResearchViewTests`、`FamilyResearchPanelTests`、`AddSC021FamilyResearchTests`、`SC021FamilyResearchScenarioTests`、`FamilyChroniclePanelTests`、`SaveDataMigratorTests`、`SaveDataValidatorTests`、`AddM6TitleScreenTests` 与 `AddSC013TitleScenarioButtonsTests`。此前 SC-022 记录的 2 个 SC-021 迁移失败已由 6C 第 3 条修复并通过。复审 follow-up 已补跑全量 EditMode `1103/1103`（含新增的正向 v7 JSON 迁移测试）。

### 7.5 Console（场景集成轮）

**已统计（最近条目，不是全量历史）。** 聚焦测试后 `Unity_GetConsoleLogs` 最近 11 条为 `0 errors / 4 warnings / 7 info`：

- 3 条 warning 是负向测试刻意触发的回滚日志（M6 updater、SC-021 updater、研究月结注入失败），对应用例均 Passed；
- 1 条 warning 是 Unity AI Toolkit 的账号/网络提示（`Account API did not become accessible…`），与项目代码无关；
- 7 条 info 含生成器成功日志、Validator 报告与 Test Runner 结果保存提示。

没有观察到未处理异常。该计数不是 Play Mode Console，也不代表全量历史为零。复审 follow-up 使用 batchmode Test Runner，不呈现 Editor Console UI，因此没有新增“Console 最近条目”计数；只记录三份 Unity 日志均为 `error CS` 0。

### 7.6 FoundationScene 生成与 Validator（场景集成轮）

**已运行并保存。** 在 worktree Editor 中打开 `Assets/Scenes/FoundationScene.unity`，执行 `AddSC021FamilyResearch.RunMenu()`；生成器在一个 Undo 组内创建 SC-021 层级、验证后保存，日志为 `SC-021 family research panel validated; FoundationScene saved.`。场景 SHA-256 由 `52d49020617fc95888c204931a00bf57a25ee421c8604018716a423445cece17` 变为 `3d12886fab9325440ad8e0fa117b7d8fd7b5461e4fb045db2c74b00d1e058096`。

`FoundationSceneValidator` 报告 `Logs/SceneValidationReport.txt`：

- `PASS SC-021 family-research panel, title entry, bindings, and layout.`
- `PASS SC-019 …`、`PASS SC-020 …`、`PASS SC-022 …`、`PASS M6 …`、`PASS SC-013 …` 保持。
- `RESULT: FAIL; Errors: 1`，唯一错误是既有 SC-009 反馈条位置：`Feedback text must be a top-anchored banner clear of the map HUD controls.` 该问题不属于本切片，未修改。

场景变更的排序行比较（忽略对象顺序）只发现一处基线差异：`TitleScreenController` 的 `researchDemoButton: {fileID: 0}` 被替换为非零引用；没有既有对象被删除或改名。普通 diff 中的大段增删是 Unity 重新序列化对象顺序所致。

### 7.7 Play Mode 与用户验收

**未运行。** 按项目规则，Play Test 由用户执行并报告；本 worker 不使用截图、Computer Use 或 UI 自动化操作 Unity，也不代替用户验收。`SC021_DEMO.md` 第 1–11 节是用户 Play Test 的纯文本步骤。

## 8. 未解决项与未验证行为

1. **全量 EditMode（已补跑）**：复审 follow-up 在同一 worktree 补跑全量 `1103/1103`（`0 failed / 0 skipped / 0 inconclusive`，结果见 `Logs/sc021-followup-full.xml`）；此前场景集成轮只运行覆盖本切片及受影响套件的 125 项。
2. **Validator 整体仍为 FAIL（1 项既有 SC-009 错误）**：SC-021 行 PASS；SC-009 反馈条位置是集成前已存在的缺口，未在本切片修复，不能声称整体 `RESULT: PASS`。
3. **Play Mode 分支未验证**：资金/物资短缺、无效研究者、完成/编年史、读档、继承保留知识均需用户按 `SC021_DEMO.md` 执行。
4. **继承 UI 在 SC-021 固定家族中的可操作性未验证**：领域/会话测试使用标准剧本覆盖了继承后知识保留，但 SC-021 固定家族的继承按钮/流程尚未在 Play Mode 实测。
5. **QATester 同任务复审未发起**：当前 OpenCode harness 未暴露 `kanban_request_review(reviewer="qatester")` / `kanban_complete` 工具，复审 follow-up 仍无法按 `AGENTS.md` 在同一任务上发起 QATester 复审；需由协调方在 Kanban 中创建/转交同任务复审，不应另建 QA 卡。本轮完成后仍需新的同任务复审与顺序只读复审。

## 9. 工作区保护

- 场景集成轮只修改 `Assets/Scenes/FoundationScene.unity`、`Assets/Scripts/Persistence/SaveDataMigrator.cs`、`Assets/Scripts/Persistence/SaveDataValidator.cs` 与相关 SC-021 文档；复审 follow-up 只修改 `Assets/Tests/EditMode/SaveDataMigratorTests.cs` 与 SC-021 文档。没有新增或删除 `.meta`/GUID，没有新增资产或 package。
- 场景由生成器的单一 Undo 事务写入并保存；没有直接编辑 YAML，没有在回滚时重载场景。
- worktree 首次导入生成的 `Library`/`Logs` 均被 `.gitignore` 忽略；Unity 自动改写的 `.vscode/settings.json`、`ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json` 与生成的 `ProjectSettings/SceneTemplateSettings.json` 已还原/删除，未纳入提交。
- 未修改用户 checkout `C:\Users\panye\My project`、其 `Library`/`Temp`、package manifest、既有 `.meta` GUID 或 OpenCode 配置。
- 未合并 `main`、未打 tag；已验证文件提交并推送到 `panyeung/sc021-scene-integration`。

## 10. 下一步（协调方）

1. 按 `SC021_DEMO.md` 第 1–11 节把纯文本 Play Test 步骤交给用户；用户明确接受前不合并 `main`、不打 tag。
2. 全量 EditMode 已在复审 follow-up 补跑（`1103/1103`）；后续若再改代码，需重新运行全量 EditMode。
3. 按 `AGENTS.md` 在同一任务上请求 QATester 复审；复审通过后再由独立的 OpenCode worker 做只读审查。
4. 记录 SC-009 反馈条位置这一既有 Validator 缺口；它不属于 SC-021，不应计入本切片失败。


## 后续 Git 集成（2026-11-04）

用户明确要求累计合并并推送后，SC-021 随 checkpoint `714cb3a` 推送到 `origin/main`；该次 Git 集成没有补做 Unity 编译、EditMode、场景接入/Validator、Console、Play Mode、QATester 复审或用户验收。

场景集成轮随后在 `panyeung/sc021-scene-integration` worktree 完成生成器、Validator、聚焦 EditMode 与 Console 记录（第 6C、7.3–7.6 节），并提交推送该 feature 分支。复审 follow-up 又补齐正向 v7 JSON 回归测试与全量 EditMode `1103/1103`（第 6D 节）；仍未合并 `main`、未打 tag。Play Mode、QATester 复审与用户验收仍 pending。
