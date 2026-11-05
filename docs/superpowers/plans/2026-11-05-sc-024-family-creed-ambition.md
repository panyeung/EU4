# SC-024 家训与个人抱负实施计划

**状态：** 用户已批准进入实施

> **For agentic workers:** 使用 Orca 监督流程；OpenCode2 实施代码；完成后由独立 OpenCode worker 只读复审。当前分支执行，不创建新 worktree。

**目标：** 在现有六人家族经营中加入跨代延续的家训、按家主记录的抱负进度、一次可见改革及存档/编年史支持。

**架构：** 使用独立 `FamilyDoctrineState` 保存家训与每位家主的抱负记录；`GameSession` 在既有月结事务中根据产业、研究和教育结果推进抱负。事件目录按当前家训选择议事版本，事件效果原子变更家训、关系和编年史。保存新增 v9 数据块并从 v8 迁移。

**技术栈：** Unity 6.6.6f1、C#、现有 uGUI、Unity Test Runner、当前 JSON 事件目录与存档 DTO；不增加 package。

**设计稿：** `docs/superpowers/specs/2026-11-05-sc-024-family-creed-ambition-design.md`

## 全局约束

- GDD `GDD_v4.0.md` 是设计权威；家训三项为守秘为先、义理至上、唯才是举；抱负三项为发展家业、传承家技、培养继承人。
- 使用当前 `panyeung/sc024-family-values` 分支和现有项目，不新建 Unity project 或 worktree。
- OpenCode2 负责全部 C#、JSON 事件内容、测试和 Editor 生成器代码；Scout 只做最多三项 GitHub 复用研究；不安装、克隆或复制外部代码，除非得到明确批准。
- 场景生成与保存由 lead/integration owner 在当前 Unity Editor 操作；生成器使用一个 Undo 组、保存前验证、异常时 Undo 回滚，不重载场景。
- 不运行 batchmode，不启动第二个 Unity Editor，不触碰 `Library`/`Temp`、`SampleScene.unity`、用户未跟踪文件、既有 `.meta` GUID 或 `ProjectSettings`。
- 既有 SC-010 至 SC-023 行为保持；SC-020/SC-023 教育资格仍按剧本隔离。
- 仅在当前 Editor Test Runner 验证编译/EditMode；Console、Validator、Play Mode 和用户验收分别报告。由用户执行 Play Test；未经用户明确验收不合并、不打 tag。
- 每个验证门通过后，仅提交该任务已验证文件并立即推送到 `origin/panyeung/sc024-family-values`；无关本地 WIP 不入提交。

## 复核重点

1. 家训改革与关系/编年史写入必须原子提交；编年史追加失败时，三者均回滚。由 Task 3 的 session 测试覆盖。
2. 旧家主未完成的抱负不能转给新家主；家训跨继承保留。由 Task 3 的继承测试和 Task 4 的往返存档测试覆盖。
3. 产业进度只在 `Operated` 且三类资源净和为正的月结推进；空缺、短缺或无净增长不计。由 Task 2/3 测试覆盖。
4. 研究抱负不把选择前已知的技艺计为新成果；同一技艺、重复事件或重复月结不能重复完成抱负。由 Task 2/3 测试覆盖。
5. 教育抱负只累计当前指定继承人的正向能力变化，失败/自由成长回退和非指定人物不误计；同一年龄年只结算一次。由 Task 2/3 测试覆盖。
6. v8 缺少家训数据时应确定性迁移为 `义理至上` 且无抱负；损坏、重复人物或未知家训 ID 的 v9 数据被拒绝且不覆盖原档。由 Task 4 测试覆盖。

---

### Task 1：Scout 复用研究

**文件：**
- 只读 `Assets/Scripts/Domain`、`Application`、`Events`、`Persistence`、`Presentation`、`Editor` 及相关测试。
- 只读最多三个 Unity 6 兼容的公开 GitHub 项目或官方资料；不下载整库、不安装、不复制源码。
- 研究结论写入 `docs/reports/sc024-reuse-research.md`。

**接口：**
- 输入：已批准的 SC-024 设计稿。
- 输出：按项目要求的名称/URL、许可、兼容、维护、依赖、接入成本、Reuse/Adapt/Build 决定；优先判断是否直接复用本项目现有实现。

- [ ] **步骤 1：检查当前代码与最多三个候选**

  先确认现有领域事件、状态 DTO、月结与面板是否覆盖本任务；如需外部研究，仅查看最多三个候选的 README/官方文档与许可证。

- [ ] **步骤 2：记录结论**

  验收结果：报告明确列出复用本项目或定制实现的理由；没有合适候选时说明 `Build`，不引入依赖。

- [ ] **步骤 3：由 lead 复核报告并推送**

  仅暂存报告，提交 `docs(sc024): record reuse research` 并推送当前分支。

### Task 2：领域状态与确定性抱负规则

**文件：**
- Create: `Assets/Scripts/Domain/FamilyDoctrineState.cs`
- Create: `Assets/Scripts/Domain/FamilyDoctrineState.cs.meta`
- Modify: 仅在必要时修改 `Assets/Scripts/Domain/FamilyState.cs`；不得形成两份家训事实。
- Test: `Assets/Tests/EditMode/FamilyDoctrineStateTests.cs`

**接口：**
- `FamilyCreedKind`：`SecrecyFirst`、`RighteousnessFirst`、`MeritFirst`。
- `FamilyAmbitionKind`：`DevelopHousehold`、`PassDownTechnique`、`EducateHeir`。
- `FamilyAmbitionRecord`：`PersonId HeadId`、`FamilyAmbitionKind Kind`、`int Progress`、`int Target`、`bool IsComplete`，并保留选定抱负时的已知技艺快照。
- `FamilyDoctrineState`：提供 `Creed`、`TrySetCreed(FamilyCreedKind creed, out string error)`、`TryStartAmbition(PersonId headId, FamilyAmbitionKind kind, IReadOnlyCollection<string> knownTechniqueIds, out string error)`、`TryGetAmbition(PersonId headId, out FamilyAmbitionRecord record)`、`Clone()`。
- 月结输入分别消费 `IndustryMonthReport`、新完成的 technique ID 和 `EducationYearResult` + 指定继承人 ID；只产生确定性 progress，不自行运行月结。
- 固定目标：产业 3 个符合条件的月份；新技艺 1 项；指定继承人五项能力正向增量累计 5。

- [ ] **步骤 1：写失败测试**

  fixture `FamilyDoctrineStateTests` 覆盖：三个 creed ID；初始 `义理至上`；空/未知人物和重复抱负拒绝且状态不变；每类抱负的目标进度；不符合条件的产业/研究/教育输入不推进；完成后幂等；Clone 深拷贝每代记录。

- [ ] **步骤 2：在现有 Unity Editor Test Runner 跑该 fixture，确认新测试失败**

  禁止 batchmode。预期：新增类型/成员不存在，fixture 红灯。

- [ ] **步骤 3：实现最小领域状态**

  在 `FamilyDoctrineState.cs` 实现稳定枚举、记录、验证、克隆及三个纯进度更新方法。研究抱负的技艺快照按稳定 ID 去重并序列化顺序。

- [ ] **步骤 4：重跑 fixture**

  预期 `FamilyDoctrineStateTests` 全绿，0 失败/跳过；保存 Editor Test Runner XML/日志到 `Logs/sc024-domain-tests.*`。

- [ ] **步骤 5：提交并推送**

  仅暂存领域状态、必要 `.meta` 和测试证据，提交 `feat(sc024): add family doctrine state` 并推送。

### Task 3：家族议事事件与 GameSession 月结集成

**文件：**
- Modify: `Assets/Scripts/Events/EventCatalogData.cs`
- Modify: `Assets/Scripts/Events/EventCatalogValidator.cs`
- Modify: `Assets/Scripts/Events/EventEngine.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Create: `Assets/Scripts/Application/SC024FamilyDoctrineScenario.cs` and `.meta`
- Create/Modify: `Assets/Resources/Events/event-catalog-v1.json`
- Create/Modify: `Assets/Resources/Localization/event-text-zh-v1.json`
- Test: 新增 `Assets/Tests/EditMode/FamilyDoctrineSessionTests.cs`；按需扩充既有事件目录/引擎测试。

**接口：**
- `GameSession` 暴露当前家训/当前家主抱负的只读视图，并提供 `TryStartCurrentHeadAmbition(FamilyAmbitionKind kind)`。
- `SC024FamilyDoctrineScenario.CreateSuccession()` 复用 `M2FixedScenario` 的六人 roster/日期与 ID，仅给该试玩设置独立 event scenario ID `sc024`；`GameSession.InitFamilyDoctrineDemoScenario()` 初始化该 route 和默认家训/抱负。普通 SC-013、SC-018、旧存档不触发 SC-024 事件。
- 新事件条件字段 `familyCreedId` 与条件 `FamilyCreedIs`；新事件效果字段 `familyCreedId` 与效果 `SetFamilyCreed`。非法 ID 在 catalog validation 阶段拒绝。
- 事件稳定 ID：`event.sc024.creed-council.secrecy`、`.righteousness`、`.merit`；三者以 `FamilyCreedIs` 互斥并共享 `PerGame` 一次决策语义。
- 当前 creed 的议事选项关系变化：守秘为先（正綱 +5、信正 -5）；义理至上（两人各 +5）；唯才是举（正綱 -5、信正 +5）。改革至任一其他 creed 时两人各 -5，效果同时设置新 creed。
- 各系统只在既有成功月结事务中推进当前家主抱负；新家主的进度从未选择状态开始，旧记录保留。

- [ ] **步骤 1：写失败事件和会话测试**

  覆盖三种议事事件互斥、非匹配事件不排程、只有匹配议事选项可提交、改革的 creed/关系/编年史前后值、编年史追加故障回滚、各月结系统对正确家主抱负推进、重复结算不重复计数、继承切换后家训保持而新家主不继承抱负。

- [ ] **步骤 2：在当前 Editor 跑新增 fixture，确认失败**

  预期缺少条件/effect/state 集成导致红灯；不运行全量测试。

- [ ] **步骤 3：实现事件模型、目录验证和 GameSession 事务接入**

  按设计复用 M2 六人家族做 SC-024 独立试玩 route；SC-024 事件只在 `scenario = sc024` 出现，既有 SC-013/SC-018 行为不变。维护议事选择与两个关系变更、家训变更及编年史追加保持同一回滚边界。三类抱负进度只从已有 settlement report/result 更新。

- [ ] **步骤 4：重跑事件与会话 fixture 及既有受影响回归**

  预期新增 fixture 与受影响的事件、家庭关系、产业、研究、教育和继承测试通过，0 失败/跳过；保存 XML/日志。

- [ ] **步骤 5：提交并推送**

  仅暂存事件/会话代码、JSON、测试及证据；提交 `feat(sc024): apply creed and ambition outcomes` 并推送。

### Task 4：v9 存档、迁移和往返验证

**文件：**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataValidator.cs`
- Modify: `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: 新增 `Assets/Tests/EditMode/FamilyDoctrineSaveTests.cs`，扩充迁移测试。

**接口：**
- 新 `FamilyDoctrineSaveData` 存 `creedId` 与按 head ID 排序的 `FamilyAmbitionSaveEntry[]`；每行保存稳定抱负 ID、进度/目标、完成状态和研究基线。
- `SaveData` schema 由 v8 升为 v9；规则版本按项目规则由 4 升为 5；事件内容版本按新事件内容升为 3。
- v8→v9 初始化 creed 为 `RighteousnessFirst`、抱负为空；更新 presence probe、迁移路径、深拷贝、捕获、恢复和严格校验。

- [ ] **步骤 1：写存档失败测试**

  覆盖 v8 标准存档迁移、SC-019/020/021/022/023 状态随迁移保留、v9 关系/家训/各代抱负 JSON 往返、排序稳定、未知/重复 ID/进度越界拒绝、缺块检测、读取失败保留原档。

- [ ] **步骤 2：在当前 Editor 跑存档 fixture，确认失败**

  预期 v9 数据块缺失或迁移版本不匹配。

- [ ] **步骤 3：实现 v9 DTO、验证和 v8→v9 迁移**

  保持 schema 迁移只生成默认 creed 与空抱负，不补算历史月份或添加随机结果。

- [ ] **步骤 4：重跑存档 fixture 与既有迁移回归**

  预期 v8、v9 和已有旧版 migration fixtures 全部通过，0 失败/跳过；保存 XML/日志。

- [ ] **步骤 5：提交并推送**

  仅暂存存档代码、测试及证据；提交 `feat(sc024): persist family doctrine` 并推送。

### Task 5：家族方针面板、标题入口与场景生成器

**文件：**
- Create: `Assets/Scripts/Application/FamilyDoctrineView.cs` 与 `.meta`
- Create: `Assets/Scripts/Presentation/FamilyDoctrinePanel.cs` 与 `.meta`
- Create: `Assets/Scripts/Editor/AddSC024FamilyDoctrine.cs` 与 `.meta`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Modify: `Assets/Scripts/Editor/AddM6TitleScreen.cs`
- Modify: `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Test: `Assets/Tests/EditMode/FamilyDoctrinePanelTests.cs`、`AddSC024FamilyDoctrineTests.cs` 和 Validator 测试。
- Scene integration owner: `Assets/Scenes/FoundationScene.unity`，只通过当前 Editor 的生成器写入。

**接口：**
- Panel view 至少含 creed ID/中文名/当前议事效果、当前家主姓名、三项抱负选项、进度/目标/完成态、操作可用性与原因。
- 标题新增 `SC024FamilyDoctrineDemoButton`；固定面板根 `Canvas/FamilyDoctrineRoot`，默认 inactive。
- 生成器 `AddSC024FamilyDoctrine.Execute()` 与 `Validate()`；验证绑定、唯一根/控件、固定标题按钮、Canvas containment、HUD/编年史/名册不重叠、原场景对象数量。

- [ ] **步骤 1：写 view/panel/scene-generator 失败测试**

  覆盖面板字段与错误原因、三种 creed/ambition 中文显示、重复根拒绝、绑定验证、RectTransform containment/overlap 和包含 SC-023 按钮时标题布局。

- [ ] **步骤 2：在当前 Editor 跑新增 UI fixture，确认失败**

  预期视图、面板绑定和生成器暂不存在。

- [ ] **步骤 3：实现纯视图、面板与事务式生成器**

  `OpenCode2` 只提交生成器与 C#；由 lead 确认 Unity 已退出 Play Mode 后，在当前 Editor 中运行生成器并保存 `FoundationScene`。

- [ ] **步骤 4：运行 UI fixture、`FoundationSceneValidator` 和当前 Editor 编译**

  预期 SC-024 专项 PASS、无新增 Validator 错误、标题入口和面板可见；完整报告及当前 Console 计数记录到 `Logs`。既有无关错误必须单独说明，不能归入 SC-024 通过。

- [ ] **步骤 5：提交并推送**

  仅暂存 UI、Editor 代码、对应 `.meta`、已验证 `FoundationScene` 和证据；提交 `feat(sc024): integrate family doctrine panel` 并推送。

### Task 6：演示文档、只读复审与 Play Test 交付

**文件：**
- Create/Update: `SC024_DEMO.md`
- Create/Update: `docs/reports/sc024-implementation.md`
- Update: `PROJECT_STATE.md`、`DECISIONS.md`
- Review: 所有本任务的已提交变更只读复审，不编辑同一工作区。

**接口：**
- `SC024_DEMO.md` 提供用户逐步试玩动作、每步预期、失败/停止条件、保存/读档步骤和文本结果清单。
- 复审者必须列出 changed files、测试与 Editor 证据、剩余缺陷；所有代码返修仍由 OpenCode2 承担。

- [ ] **步骤 1：核对完整试玩路径和回归证据**

  覆盖三种家训议事、改革前后关系与编年史、每类抱负推进/完成、两次交接、v8 迁移和 v9 存读档。

- [ ] **步骤 2：更新 demo、实现报告、PROJECT_STATE 和 DECISIONS**

  分开记录 compile、EditMode 数量、Console、Validator、Play Mode 与用户验收；不得把代理或自动证据写为用户接受。

- [ ] **步骤 3：OpenCode2 请求独立只读审查**

  依项目流程由 OpenCode2 对当前任务请求 QA；通过后再由 Mochi 创建唯一的最终 sign-off 任务。审查 worker 不修改代码。

- [ ] **步骤 4：准备用户 Play Test 并冻结演示**

  所有可执行门通过后推送当前分支。用户 Play Test 由用户执行；收到明确接受前不创建最终 checkpoint、不合并、不打 tag。

- [ ] **步骤 5：在明确验收后提交最终 checkpoint**

  只在用户明确接受后提交已验证任务文件、推送分支；再按用户验收完成里程碑合并和标签。

## 验证命令与预期

- Unity Test Runner：每个任务所列 fixture 单独运行；不使用 batchmode，记录 XML、通过/失败/跳过数量。
- Unity Editor：当前项目已打开的 `6000.6.6f1` Editor 编译；记录最新 `Logs/Editor.log` compile/domain reload 证据。
- Scene Validator：`FoundationSceneValidator` 输出中 SC-024 行必须 PASS；全局错误/警告单独列出。
- Play Mode：依 `SC024_DEMO.md` 用户步骤完成；不是编译或 EditMode 结果的替代。

## 计划自审

- 覆盖设计稿中的家训持久性、事件改革、关系与历史、每代抱负、月结推进、迁移、面板、场景及用户验收。
- 每个 Task 均有明确文件、接口、失败/成功检查和 checkpoint 推送。
- 代码任务交 OpenCode2；外部复用研究由 Scout 负责；场景生成由单一 integration owner 执行；代码复审串行。
