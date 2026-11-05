# SC-020 子女培养与人物成长实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Follow this plan task by task using the existing Orca OpenCode2 task; do not create another Unity project or a parallel writer.

**Goal:** 在现有 Unity 项目交付一条 SC-020 教育示范路线，让玩家为 15 岁的青叶千景选年度培养方向和导师，经历 1560 年月结，在 1561-01-01 看到确定成长与成年状态。

**Architecture:** 纯 C# 成长状态与公式按稳定 PersonId 保存；`GameSession` 在现有事务式月结中于 12 月边界结算教育；`SaveData` v7 持久化计划、能力与结算标记；FoundationScene 通过事务式生成器新增独立标题入口和教育面板。SC-013、SC-018、SC-019 规则与样例保持原样。

**Tech Stack:** Unity 6 / C# / 既有 uGUI、JsonUtility、EditMode Test Framework、FoundationSceneValidator。

**Spec:** [SC-020 设计稿](../specs/2026-11-04-sc-020-character-growth-education-design.md)

## Global Constraints

- GDD v4.0 是设计权威；SC-020 是 M3 子切片，不代表 M3 完成。
- 仅使用当前 `C:\Users\panye\My project`；不建立 Unity 项目、不安装包、不新增外部资产或付费服务。
- 代码由 OpenCode2 实施；本 Dispatch 是唯一写入者，也是唯一场景/ProjectSettings 写入者。随后由独立 OpenCode reviewer 只读审查。
- 当前分支 `feature/sc018-independent-core-events` 含大量未提交 SC-018/SC-019 工作。不得清理、回退、覆盖或重排这些工作；修改同一文件前先检查并保留现有 diff，只做兼容的增量编辑。
- 保留所有 `.meta` / GUID；不改 `Library` 或 `Temp`；不新建或重置用户存档。
- 任何 Unity 操作前先枚举 `Unity.exe` 进程并读取其 `-projectPath`；只用此项目已经打开的 Editor，不启动第二个 Editor 或 batchmode。代码可静态检查；Play Test 留给用户，不截图、不使用 Computer Use 操作 Unity。
- EditMode 测试、编译、Console 计数、场景 Validator、Play Mode 和用户验收分别报告。未经用户 Play Test 验收不得合并、打 tag 或提交 milestone checkpoint。
- 不修改 `Packages/manifest.json`。若实现发现需要包或外部内容，停止并先报告许可证与兼容性。

## Review Focus

1. 12 月到 1 月结算期间若事件排程或编年史写入失败，成长、计划、产业、日期和事件随机态一起回滚；用年度事务注入故障测试。
2. 同一个 `(PersonId, AgeYear)` 多次提交、读档或重复结算不能多次发放；用计划幂等与年末重复调用测试。
3. 失效导师、未成年/死亡/退休/非家族导师、错误方向与缺失成长存档均明确拒绝；用 API 与 SaveDataValidator 测试覆盖。
4. v6 到 v7 必须保留 SC-018 家庭关系、编年史、事件/继承和 SC-019 产业；迁移不伪造教育历史；用完整 v6 fixture 与缺字段/空字段 fixture 测试。
5. FoundationScene 生成异常必须在同一个 Undo 组中回滚；验证器要拒绝缺组件、重复对象、画布外控件或遮挡既有 HUD 的 SC-020 控件。

---

### Task 1: 纯 C# 成长与年度培养规则

**Files:**
- Create: `Assets/Scripts/Domain/CharacterGrowth.cs`
- Create: `Assets/Scripts/Domain/CharacterDevelopmentState.cs`
- Test: `Assets/Tests/EditMode/CharacterGrowthRulesTests.cs`
- Test: `Assets/Tests/EditMode/CharacterDevelopmentStateTests.cs`

**Interfaces:**
- Produce `GrowthAttribute`（Ninjutsu、MartialArts、Scholarship、Management、Social）、`EducationDirection`（上述五类 + FreeGrowth）、`MentorSpecialty`、`CharacterGrowthValues`、`EducationYearPlan`、`EducationYearResult`、`CharacterDevelopmentState` 与 `CharacterGrowthRules`。
- `CharacterGrowthValues.TryCreate(int ninjutsu, int martialArts, int scholarship, int management, int social, out CharacterGrowthValues values, out string error)` 接受 `0..100`；`CharacterGrowthValues.Apply(...)` 对结果按 `0..100` 夹值。
- `CharacterGrowthRules.Resolve(CharacterGrowthValues before, EducationDirection direction, GrowthAttribute childAptitude, MentorSpecialty mentor, bool supportiveEnvironment)` 返回不可变 `EducationYearResult`；定向成长对所选属性 `+4`，资质匹配 `+2`，导师专长匹配 `+2`，有利家庭环境 `+1`；FreeGrowth 五项均 `+1`，不使用随机数。
- `CharacterDevelopmentState` 按 PersonId 保存能力与 `(PersonId, AgeYear)` 年度计划/结算标记，提供 `Clone()`、查询、替换未结算计划、一次性结算与验证方法；未知 ID、非法年龄/方向、重复结算均给显式结果，不部分修改状态。

- [ ] **Step 1: 先写失败测试** `CharacterGrowthRulesTests`：学问资质 + 小夜导师 + 有利环境将学问 `20→29`；同一资质由宗一郎指导为 `20→27`；FreeGrowth 的五项各 `+1`；`99` 上限夹值到 `100`；无效初值/方向被拒绝。
- [ ] **Step 2: 在 Unity Editor EditMode Test Runner 只运行这两个新测试类，确认新增行为先失败且既有代码可编译。** 不使用 batchmode。
- [ ] **Step 3: 写 `CharacterGrowth.cs`，实现固定枚举、取值校验、增量计算、解释字段与夹值。** 不引入随机数或 UnityEngine 依赖。
- [ ] **Step 4: 写 `CharacterDevelopmentState.cs`，实现稳定 ID 索引、计划可替换、年度最多结算一次及克隆。** 在克隆上先验证再提交，失败不改原对象。
- [ ] **Step 5: 运行上述新测试并确认所有指定数值与拒绝路径通过。**

### Task 2: GameSession 年度事务与 SC-020 固定剧本

**Files:**
- Create: `Assets/Scripts/Application/SC020EducationScenario.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Modify: `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- Modify: `Assets/Scripts/Domain/HistoryLedger.cs`
- Test: `Assets/Tests/EditMode/SC020EducationSessionTests.cs`
- Test: `Assets/Tests/EditMode/GameSessionChronicleTests.cs`
- Test: `Assets/Tests/EditMode/WorldClockTests.cs`

**Interfaces:**
- `SC020EducationScenario.CreateSuccession()` 创建青叶家固定成员：宗一郎 `person.sc020.head` / 1520 / Head，配偶小夜 `person.sc020.spouse` / 1522 / Spouse，千景 `person.sc020.child` / 1545 / DirectKin；创建两条 ParentChild 边与一条 Spouse 边。千景资质 Scholarship，家庭环境 supportive；宗一郎导师专长 MartialArts，小夜导师专长 Scholarship；千景初值为 `15/12/20/10/13`。
- `GameSession.InitEducationDemoScenario()` 创建上述剧本，起始日 `1560-01-01`，SC-019 初始产业资源与空岗位；保留标准/危机/SC-013 入口行为。
- `GameSession.TrySetEducationPlan(PersonId learner, EducationDirection direction, PersonId mentor)` 返回 `EducationPlanCommandResult`，在稳定管理点校验成员、存活、`AgeAt(CurrentGameDate) <= 15`、成年导师资格与方向；同年度可替换计划，但替换不立即成长或写编年史。
- `GameSession` 在关闭 12 月时，对成长状态取快照；依序完成产业结算、教育年度结算、下一月事件排程、日期提交与一次 `SessionStateChanged`。教育结果经 `HistoryLedger.TryAppendEducation(...)` 形成一条含人物姓名、方向、导师、能力前后值的编年史记录；失败时恢复日期、产业、成长、随机流、事件运行态和编年史。

- [ ] **Step 1: 写失败测试** `SC020EducationSessionTests`：固定日期/家庭图/资源/初始能力；合法方向与导师可提交，未成年/死亡/退休/非成员导师拒绝；同一年度替换计划不改属性；从 1560-01-01 结束 11 个月仍未结算教育，关闭 12 月时学问按方案增长一次且日期到 1561-01-01、年龄为 16；重复结算不重复成长/编年史。
- [ ] **Step 2: 在 EditMode Test Runner 运行该测试类，确认新规则测试先失败。**
- [ ] **Step 3: 新建 `SC020EducationScenario.cs` 并增量扩展 `GameSession`。** 新场景须使用稳定 ID 与新的 ScenarioId；不得把 SC-013 事件条件应用到 SC-020。
- [ ] **Step 4: 扩展 `FamilyChronicleEntry` / `HistoryLedger` 增加教育类别和幂等追加接口。** 文本只显示本地化方向名与姓名，不显示原始稳定 ID。
- [ ] **Step 5: 在既有 `TryAdvanceToNextMonth` 事务快照/回滚中增加 12→1 教育阶段；添加故障注入测试，证明事件排程或编年史失败后重试结果仍为同一条记录和同一组属性值。**
- [ ] **Step 6: 运行新增 session、chronicle 与 clock 测试，以及受影响的现有月结/继承/SC-010 时间暂停测试。** 报告实际 Test Runner XML 路径与各程序集 totals。

### Task 3: v7 存档、迁移与校验

**Files:**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs`
- Test: `Assets/Tests/EditMode/CharacterGrowthSaveTests.cs`
- Test: `Assets/Tests/EditMode/SaveDataValidatorTests.cs`
- Test: `Assets/Tests/EditMode/SuccessionSaveTests.cs`

**Interfaces:**
- 在 `SaveData` 增加 `CharacterDevelopmentSaveData development`，以排序后的稳定 PersonId entries 保存五项能力、年龄年度培养计划、导师 ID、已结算年度和结果；presence probe 对 v7 必须区别缺失根字段与显式 null。
- `CharacterDevelopmentSaveData.Capture(CharacterDevelopmentState)` / `TryRestore(SuccessionState, out CharacterDevelopmentState, out string error)` 只处理纯 DTO 与领域状态，不序列化 Unity 对象。
- `SaveData.CurrentRulesVersion = 3`、schema 为 v7；事件内容版本不变。

- [ ] **Step 1: 写失败测试**：v7 保存/读档逐项还原状态；v6 完整 fixture 迁移到 v7 时保留 SC-018 关系、全部 chronicle、eventRuntime、succession 与 industry，并为现存人物创建 0 能力、空培养历史；非法 ID、越界能力、重复人物/年度、未知方向、无效导师或 development 为 null 明确拒绝；相同日期重读后不重放结算。
- [ ] **Step 2: 在 EditMode Test Runner 运行新保存测试和现有 validator/succession 保存测试，确认新增断言先失败。**
- [ ] **Step 3: 扩展 SaveData/PresenceProbe/Codec，按 PersonId 排序 capture，严格验证 v7 必填对象。**
- [ ] **Step 4: 扩展 `SaveDataMigrator`：v6→v7 添加空成长/教育状态，不补算教育也不新增编年史；v1–v5 继续通过现有迁移链到 v7；含错误版本字段或重复状态的载荷拒绝且保留原档。**
- [ ] **Step 5: 运行新增测试、现有 SaveDataValidator/SuccessionSave/IndustrySave 测试与完整 EditMode 程序集。**

### Task 4: 标题入口、培养面板与事务式场景集成

**Files:**
- Create: `Assets/Scripts/Presentation/CharacterEducationPanel.cs`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Create: `Assets/Scripts/Editor/AddSC020CharacterEducation.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Modify: `Assets/Scenes/FoundationScene.unity`
- Test: `Assets/Tests/EditMode/CharacterEducationPanelTests.cs`
- Test: `Assets/Tests/EditMode/AddSC020CharacterEducationTests.cs`
- Test: `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs`

**Interfaces:**
- `CharacterEducationPanel.BindReferences(GameSession session, GameObject panelRoot, Button openButton, Button closeButton, Text overviewText, Transform directionContainer, Button directionTemplate, Transform mentorContainer, Button mentorTemplate, Text previewText, Button confirmButton, Text messageText)` 绑定场景依赖；预览由 session 的只读成长模型计算，确认只调用 `TrySetEducationPlan`。
- `TitleScreenController.BindEducationScenarioButton(Button educationDemo)` / `StartEducationDemo()` 新增一个独立入口；原有 `StartNewGame`、`StartStandardSample` 与 `StartCrisisDemo` 语义保持不变。
- `AddSC020CharacterEducation` 采用与 `AddSC019IndustryPanel` 相同的单 Undo group、场景路径/脏状态/Play Mode preflight、写入前检查、保存前完整 Validator 与异常 Undo 回滚规则；生成控件位于单个 `SC020EducationRoot`，不与 HUD、事件、产业、编年史或家谱入口重叠。

- [ ] **Step 1: 写 EditMode 失败测试**：面板显示 5 项数值与年龄年度；方向/导师改变时预览符合 `+9/+7`；确认成功后按钮/状态防重复奖励；关闭重开不丢计划；缺绑定引用时安全诊断。场景测试拒绝错误场景、dirty scene、Play Mode、重复/部分控件、遮挡与缺少引用。
- [ ] **Step 2: 在 EditMode Test Runner 运行两个新 UI/scene-generator 测试类与标题场景测试，确认新断言先失败。**
- [ ] **Step 3: 实现 `CharacterEducationPanel` 和 `TitleScreenController` 入口绑定。** 动态按钮每次刷新先移除旧行与 listener，禁止重复绑定。
- [ ] **Step 4: 实现 `AddSC020CharacterEducation`；由本 OpenCode2 Dispatch 作为唯一场景 writer 执行一次事务式生成并保存 FoundationScene。** 若生成器异常，验证 Undo 还原对象数及场景 dirty 状态；不使用直接 `DestroyImmediate` 清理既有场景对象。
- [ ] **Step 5: 扩展 `FoundationSceneValidator` 检查唯一 root、层级、所有必需绑定、画布内布局、与既有 HUD/面板入口不重叠，并验证 SC-013/SC-019 场景控件仍存在。**
- [ ] **Step 6: 在当前 Editor Test Runner 运行新增 UI/生成器/Validator 测试与受影响标题测试；读取该项目 `Editor.log` 与场景 Validator 报告。** 不进 Play Mode，不截图，不替用户点击。

### Task 5: 文档、OpenCode2 自检与审查交接

**Files:**
- Create: `SC020_DEMO.md`
- Create: `docs/reports/sc020-implementation.md`
- Modify: `PROJECT_STATE.md`
- Modify: `DECISIONS.md`

- [ ] **Step 1: 建立 `SC020_DEMO.md`**，写出准备步骤、SC-020 入口、存档前置条件、方向与导师选择、保存/读取、推进 12 个月、检查 1561-01-01 成年结果、重复结算/编年史验证、明确失败停止条件及纯文本回报清单；注明 Play Test 由用户执行。
- [ ] **Step 2: 更新 `PROJECT_STATE.md` / `DECISIONS.md`**，记录 SC-020 范围、schema/rules 版本、设计决策、当前代码与测试状态；用户验收保持 pending，不将 M3 标记完成。
- [ ] **Step 3: 写实现报告**，列出变更文件、Unity 项目与 Editor 路径、编译结果、EditMode totals、Console 错误/警告/信息数、场景 Validator 结果、未覆盖事项；不把自动化测试写成用户 Play Test。
- [ ] **Step 4: OpenCode2 完成同一任务的实现自检并通过 `kanban_request_review(reviewer="qatester")` 提交 QA。** QATester 只审查该实现任务；如有缺陷按同任务返回 OpenCode2 修正并复审。
- [ ] **Step 5: QA 通过后由 lead 派独立 OpenCode reviewer 只读审查最终 diff 与证据，再向用户交付 Demo 和逐步 Play Test 清单。** 用户 Play Test 验收前不提交、不合并、不 tag、不 push。

## 本计划不包含的验收

- OpenCode2 不运行人工 Play Test；不会操作用户 Unity UI。
- 自动化 EditMode 测试与 scene validator 不能证明 Play Mode 体验，用户需亲自执行 `SC020_DEMO.md` 并报告结果。
- 本切片验收后仍需继续完成 M3 的其他方向；此计划不包含婚姻/收养、隐居、摄政或完整人物成长。
