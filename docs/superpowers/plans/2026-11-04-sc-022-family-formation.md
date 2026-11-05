# SC-022 家族婚姻与日常收养实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use the OpenCode2 supervised implementation workflow described in `AGENTS.md`; implement task-by-task and request the separate read-only review after implementation.

**Goal:** 在现有 `FoundationScene` 中交付一个固定 SC-022 剧本和可复用的家族管理面板，让玩家登记多位正式配偶或普通收养，并把成员身份、亲属事实、编年史和存档一致地保存。

**Architecture:** 以 `GameSession` 作为婚姻/收养命令边界，先用纯 C# 规则验证人物资格、直系亲属限制与亲子链循环，再一次性提交成员、亲属边和编年史。只读应用视图为独立 uGUI 面板提供当前家族成员和两类候选人；SC-022 使用独立固定家族身份和标题入口，不改已有剧本。

**Tech Stack:** Unity 6、C#、现有 Domain/Application/Presentation/Editor asmdef、uGUI、NUnit EditMode tests、既有 JSON 存档和编年史。

**Spec:** `docs/superpowers/specs/2026-11-04-sc-022-family-formation-design.md`

## Global Constraints

- 所有代码由 OpenCode2 在现有 SC-018 feature 工作区实现；保留全部既有脏改动。
- 复用当前 Unity 项目与 `Assets/Scenes/FoundationScene.unity`；不建新项目或场景，不改 `Library`、`Temp`，不安装 Unity package。
- 保留 `.meta` 文件与 GUID；不复制商业游戏内容，演示人物和文案原创。
- 正式配偶不设人数上限；拒绝本人、重复配偶及直系血亲/养亲关系；兄弟姐妹不额外限制。
- 普通收养拒绝本人、正式配偶、重复收养和会形成 `ParentChild`/`Adoption` 有向亲子环的对象；保留既有血缘事实，不触发继承交接。
- 婚姻双方/收养人必须在世且成年；婚姻候选必须成年；收养候选可为儿童或成年人。非成员候选必须是当前 SC-022 剧本明确注册的人物。
- 婚姻或收养不推进日期、不运行月结、不消耗资源或 RNG；保存/读取后候选人物、成员身份、关系和编年史一致。
- 编译与 EditMode/PlayMode 使用已打开的 Unity Editor；禁止对其项目目录运行 batchmode。用户负责 Play Test；未获验收不得提交、合并、打 tag 或 push。
- GitHub 复用研究见 `docs/research/SC022-family-formation-reuse.md`；因 Orca 未配置 `scout` 执行别名，本轮由协调者按同一范围完成只读研究。优先现有代码，报告许可、Unity 6 兼容、维护状态、依赖和集成工作量；不克隆、安装或复制代码。

## Review Focus

- `KinshipGraph` 已含既有循环或重复关系时，候选筛选和命令验证必须终止且不误放行；由 Task 1 的祖先路径循环与重复边测试覆盖。
- 婚姻候选人与关系发起人存在多跳血缘/收养直系关系时，婚姻应被拒；兄弟姐妹仍按批准规则可登记；由 Task 1 覆盖多跳与同胞两种情形。
- 候选对象在展示后、确认前已死亡或加入家族时，提交必须重新校验并拒绝；由 Task 2 会话命令测试覆盖。
- `HistoryLedger` 在候选校验失败、日期无效或条目无法创建时，不得留下家族成员或亲属边；由 Task 2 的失败快照测试覆盖。
- 存档中存在未加入当前家庭的已注册候选人，或旧版本缺少 SC-022 记录时，读取仍需兼容且不能把所有非成员误列为候选；由 Task 5 往返测试及已有存档回归覆盖。

---

### Task 1: 亲属直系关系与收养环规则

**Files:**
- Create: `Assets/Scripts/Domain/FamilyFormationRules.cs`
- Modify: `Assets/Scripts/Domain/KinshipGraph.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationRulesTests.cs`

**Interfaces:**
- Produces: `FamilyFormationRules.IsDirectLineRelation(KinshipGraph graph, PersonId left, PersonId right)`，沿 `ParentChild` 与 `Adoption` 两类有向边双向判断是否处于祖先/后代直系；`FamilyFormationRules.WouldCreateAdoptionCycle(KinshipGraph graph, PersonId adopter, PersonId candidate)` 判断追加 adopter→candidate 是否会闭环。
- 两个方法对空图/空 ID 返回安全的否定结果或显式拒绝；遍历须使用 visited 集合处理既有坏环，不循环挂起。

- [ ] **Step 1: 编写失败测试**：覆盖直系血亲、直系养亲、多跳直系、方向相反、同胞关系不属于直系、候选为收养者祖先时会闭环，以及已有循环图会终止。
- [ ] **Step 2: 在 Unity Editor Test Runner 运行 `FamilyFormationRulesTests`，确认新增规则测试失败。**
- [ ] **Step 3: 实现图遍历 helper**：复用 `KinshipGraph.Links`，不改变既有事实或对外存档表示。
- [ ] **Step 4: 重跑 `FamilyFormationRulesTests`，确认全部通过。**

### Task 2: 会话命令、候选视图、固定剧本与编年史

**Files:**
- Create: `Assets/Scripts/Application/FamilyFormationView.cs`
- Create: `Assets/Scripts/Application/SC022FamilyFormationScenario.cs`
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Modify: `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- Modify: `Assets/Scripts/Domain/HistoryLedger.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationSessionTests.cs`
- Test: `Assets/Tests/EditMode/SC022FamilyFormationScenarioTests.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationChronicleTests.cs`

**Interfaces:**
- Produces: `GameSession.GetFamilyFormationView()`；`GameSession.TryRegisterMarriage(PersonId memberId, PersonId candidateId)`；`GameSession.TryAdopt(PersonId adopterId, PersonId candidateId)`。两个命令返回 `FamilyFormationCommandResult`，包含成功状态、稳定结果码、中文消息、发起人 ID 和候选人 ID。
- `FamilyFormationView` 暴露当前有效家庭成员选项、剧本允许的非成员婚姻候选与收养候选；婚姻候选按所选成员的直系亲属限制过滤。视图只读，不提交状态。
- `SC022FamilyFormationScenario` 提供稳定家族/人物 ID、原创姓名、`CreateSuccession()` 与 `IsActive(SuccessionState)`；提供至少两名成年婚姻候选和一名收养候选。

- [ ] **Step 1: 编写会话与场景失败测试**：覆盖固定 SC-022 身份、候选已注册但未入家、非场景非成员不出现、候选年龄分流、死亡/未成年发起人无资格。
- [ ] **Step 2: 编写命令失败测试**：婚姻多配偶成功、自身/重复/直系血亲或养亲被拒、同胞不额外阻止；收养儿童和成人成功、本人/配偶/重复边/亲子环被拒。
- [ ] **Step 3: 编写原子性和副作用测试**：失败前后成员、关系边、编年史、继承人、继承阶段、日期、资源和 RNG 状态一致；成功普通收养不改变继承状态。
- [ ] **Step 4: 在 Unity Editor Test Runner 运行上述新增测试并确认失败。**
- [ ] **Step 5: 实现候选投影和两个 `GameSession` 命令**：每次提交都重新验证当前状态；先完成资格、重复关系、日期、姓名和编年史条目校验，再写成员、亲属边和一条记录，保证拒绝路径不改变任何状态。
- [ ] **Step 6: 追加稳定的 `ChronicleCategory.FamilyFormation` 值与 `HistoryLedger.TryAppendFamilyFormation(...)`；摘要使用显示名，不暴露原始 ID。**
- [ ] **Step 7: 实现 SC-022 固定剧本**：不改 SC-010/013/019/020/021 的状态工厂或标题按钮。
- [ ] **Step 8: 在 Unity Editor Test Runner 重跑本任务测试，确认通过。**

### Task 3: 婚姻与收养管理面板及编年史展示

**Files:**
- Create: `Assets/Scripts/Presentation/FamilyFormationPanel.cs`
- Modify: `Assets/Scripts/Presentation/FamilyChroniclePanel.cs`
- Test: `Assets/Tests/EditMode/FamilyFormationPanelTests.cs`
- Test: `Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`

**Interfaces:**
- Consumes: Task 2 的 `FamilyFormationView`、`FamilyFormationCommandResult` 与 `GameSession` 命令。
- Produces: `FamilyFormationPanel.BindReferences(...)`、`IsOpen`、`Open()`、`Close()`、成员/候选选择与确认入口，遵循现有 `FamilyResearchPanel` 的可注入测试 seam。

- [ ] **Step 1: 编写 EditMode 测试**：面板默认关闭、开关可用；婚姻和收养候选按资格过滤；选中对象后显示既有配偶/亲属信息；取消不提交；确认只提交一次并显示成功或具体拒绝原因。
- [ ] **Step 2: 编写编年史 UI 测试**：`FamilyFormation` 类别显示中文标签，婚姻/收养标题与摘要包含人物显示名。
- [ ] **Step 3: 在 Unity Editor Test Runner 运行新增面板测试并确认失败。**
- [ ] **Step 4: 实现固定根下的独立面板**：分为婚姻和收养页；通过二次确认说明不可撤销的成员与关系变化；不改只读 `FamilyRosterPanel`。
- [ ] **Step 5: 加入 `FamilyChroniclePanel` 的家族关系类别格式化。**
- [ ] **Step 6: 重跑本任务测试，确认通过。**

### Task 4: FoundationScene 集成与标题入口

**Files:**
- Create: `Assets/Scripts/Editor/AddSC022FamilyFormation.cs`
- Modify: `Assets/Scripts/Presentation/TitleScreenController.cs`
- Modify: `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- Test: `Assets/Tests/EditMode/AddSC022FamilyFormationTests.cs`
- Modify: `Assets/Scenes/FoundationScene.unity`（仅通过事务式 Editor 生成器）

**Interfaces:**
- Produces: `AddSC022FamilyFormation.Update(Scene)`、`Validate(Scene, out string error)` 与 `CheckScenePreflight(...)`，参照 SC-021 生成器的固定 root、单 Undo group、保存前验证、异常 Undo 回滚规范。
- 标题入口调用 `TitleScreenController.StartFamilyFormationDemo()`，并启动 `GameSession.InitFamilyFormationDemoScenario()`；按钮由生成器同一事务添加与绑定。

- [ ] **Step 1: 编写 Editor EditMode 测试**：生成后只有一个固定根且为 Canvas 直接子级；面板默认隐藏、打开按钮可见；标题入口绑定 SC-022；validator 检查引用、层级和按钮；重复运行幂等；错误预检/验证失败通过 Undo 完整回滚。
- [ ] **Step 2: 在 Unity Editor Test Runner 运行 `AddSC022FamilyFormationTests` 并确认失败。**
- [ ] **Step 3: 实现生成器、标题绑定、`FoundationSceneValidator` 检查和 `TitleScreenController` 启动方法。**
- [ ] **Step 4: 运行该 Editor 测试集，确认通过；由生成器更新当前 `FoundationScene` 并保存，不手改已有对象 GUID。**
- [ ] **Step 5: 在同一 Unity Editor 运行 `FoundationSceneValidator`，确认 SC-022 集成通过。**

### Task 5: 存档往返、文档与最终验证交接

**Files:**
- Test: `Assets/Tests/EditMode/FamilyFormationSaveTests.cs`
- Create: `SC022_DEMO.md`
- Create: `docs/reports/sc022-implementation.md`
- Modify: `PROJECT_STATE.md`
- Modify: `DECISIONS.md`

**Interfaces:**
- Consumes: SC-022 场景、会话命令、编年史和现有 `SaveData` 捕获/恢复接口。
- Produces: 可按步骤操作的用户 Play Test 指南及明确区分静态检查、Unity Editor 验证和待用户验收的实现报告。

- [ ] **Step 1: 编写保存/读取测试**：未加入家庭的候选人仍被保存；成功婚姻/收养后成员身份、边方向、历史条目顺序及显示文字往返一致；旧存档仍可读取。
- [ ] **Step 2: 在 Unity Editor Test Runner 运行新增保存测试并确认失败（若现有序列化已满足则记录具体先行通过证据）。**
- [ ] **Step 3: 仅在证据表明现有结构不足时修改 `SaveData.cs`/迁移器；先说明设计差异，保持旧版本迁移兼容。**
- [ ] **Step 4: 完成 `SC022_DEMO.md`：列出启动 SC-022、登记第二位配偶、验证直系禁限/同胞规则、办理儿童或成人收养、检查编年史、保存/重载的顺序步骤和预期结果。**
- [ ] **Step 5: 更新 `PROJECT_STATE.md`、`DECISIONS.md` 与实现报告**，记录文件、规则、存档兼容、`docs/research/SC022-family-formation-reuse.md` 的复用结论、编译、EditMode 总数、Console 错误/警告、场景 Validator、Play Mode 证据；未实际执行的项目写为待验证。
- [ ] **Step 6: 在已打开的 Unity Editor 执行编译和相关 EditMode 测试，确认无编译错误、测试通过、Console 零错误，并重新运行场景 Validator；Play Mode 结果独立记录。**
- [ ] **Step 7: 交 OpenCode2 完成报告和变更列表，冻结 worktree 等待独立只读代码审查；随后提供用户 Play Test 步骤。不得提交、合并或 push，直到用户明确验收。**

