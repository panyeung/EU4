# SC-017 通用家谱实现计划

> **执行要求：** 按 Orca 监督流程交由 OpenCode2 实现，并在实现完成后安排独立只读复审。本计划获用户确认前不开始编码。

**目标：** 将固定六行家族名册改为只读、数据驱动、可滚动的通用家谱展示，读取任意有效家族成员和亲属关系数据。

**架构：** 纯数据构建器从 `SuccessionState` / `SuccessionWorld` 生成节点和关系快照；确定性布局器在不依赖 SC-013 人名、ID、数量的前提下计算位置；`FamilyRosterPanel` 在现有 uGUI 面板中绘制节点、关系线和滚动内容。家臣等非亲属成员显示在独立区域，本轮不提供关系编辑。

**技术：** Unity 6.6.6f1、C#、现有 uGUI、现有 Unity EditMode Test Runner；不新增 package 或场景。

**设计稿：** `docs/superpowers/specs/2026-11-03-sc-017-family-tree-roster-design.md`

## 全局约束

- 所有代码由 OpenCode2 在现有 `feature/sc013-fixed-three-generation` worktree 实现。
- 保留所有已有修改和未跟踪文件；不 reset、clean 或覆盖无关工作。
- 复用 `FoundationScene`，不新建项目/场景；保留 `.meta` 与 GUID；不碰 `Library` / `Temp`。
- 名册不修改家族成员或结构性亲属关系；不改存档格式、GDD 规则、SC-010/SC-011 或 M2 剧本行为。
- 只使用已打开的 Unity Editor 验证；不得对其项目路径运行 batchmode。醒着时不使用截图或 Computer Use 操作 Unity；Play Test 由用户执行。
- 编译、EditMode、Console、场景验证、Play Mode、用户验收分别记录。用户验收前不得提交、合并或打 tag。

## 复审重点与对应测试

1. 有效家族成员没有亲属边时仍显示为无连线成员：图构建/面板测试。
2. 家臣、弟子、客将等非亲属成员不得画入亲缘关系：图构建测试。
3. 多父母、多配偶、手足和收养关系不重复节点、不丢失关系：图构建/布局测试。
4. 超过六人、三代的图保持确定性并可滚动：布局/面板测试。
5. 缺少可选年龄或关系端点已不在成员列表时不崩溃且不隐藏有效成员：图构建测试。

## 文件职责

- 新建 `Assets/Scripts/Presentation/FamilyTreeGraph.cs`：只读图快照、节点/关系数据与构建器。
- 新建 `Assets/Scripts/Presentation/FamilyTreeLayout.cs`：与 SC-013 身份及输入顺序无关的确定性布局。
- 修改 `Assets/Scripts/Presentation/FamilyRosterPanel.cs`：消费图快照和布局，渲染关系线、人物卡、滚动内容、无连线亲属与家臣区域，保留原开关和只读行为。
- 修改 `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`：通过现有事务式 updater 建立 viewport、ScrollRect、内容/连线/节点层与模板并扩展验证。
- 修改 `Assets/Scripts/Editor/FoundationSceneValidator.cs`：检查通用家谱层级和绑定。
- 新建 `Assets/Tests/EditMode/FamilyTreeGraphTests.cs` 与 `FamilyTreeLayoutTests.cs`；扩展 `FamilyRosterPanelTests.cs`、`AddSC013FamilyRosterPanelTests.cs` 及适用的 scene validator 测试。
- 实现及验证后更新 `SC017_DEMO.md`、`PROJECT_STATE.md`、`DECISIONS.md`，新增简明 `docs/reports/sc017-implementation.md`。

## 有序任务

### 任务 1：实现前复用调研（只读）

**负责人：** Scout，通过 Orca。禁止代码和 package 变更。

- [ ] 先检查本地 `FamilyRosterPanel`、updater 与领域图，确认哪些能力可直接复用。
- [ ] GitHub 最多搜索 3 个 Unity 6 兼容的家谱/图布局项目；优先读 README 与相关文档。
- [ ] 报告 URL、许可证/商业使用、Unity/URP 兼容性、活跃情况、依赖/安全、改造估算、Reuse/Adapt/Build 结论。
- [ ] 不下载/克隆仓库、不安装 package、不复制代码或素材。Mochi 在 OpenCode2 开发前记录复用/自建决策。

### 任务 2：数据图快照与布局

**文件：** 新建 `FamilyTreeGraph.cs`、`FamilyTreeLayout.cs` 及对应测试。

**接口：**
- `FamilyTreeGraphBuilder.Build(SuccessionState state, GameDate date) -> FamilyTreeGraphSnapshot`
- 快照提供按稳定规则排序的只读 `FamilyTreePersonNode`、`FamilyTreeRelationEdge` 集合。
- `FamilyTreeLayout.Calculate(FamilyTreeGraphSnapshot graph) -> FamilyTreeLayoutResult`；结果包含每个节点位置及内容宽高。
- 节点包含 `PersonId`、姓名、可用年龄、`FamilyMemberRole`、当前家主/有效指定继承人标记、亲属/家臣分类。
- 关系边包含两端 ID 与现有 `KinshipType`，不创建或修改关系事实。

- [ ] 写 `Build_IncludesSc013MembersAndExactKinshipEdges`、`Build_KeepsUnconnectedKinVisible`、`Build_SeparatesRetainersDisciplesAndGuestsFromKinshipEdges` 失败测试，断言 SC-013 精确关系、孤立亲属可见，非亲属成员保留但无亲缘边。
- [ ] 写 `Calculate_LaysOutMoreThanSixMembersAndThreeGenerations`、`Calculate_IsDeterministicWhenInputOrderChanges`、`Calculate_PreservesCollateralSpouseAndAdoptionEdges`、`Calculate_ReturnsUniquePositionsAndFiniteBounds` 失败测试，断言多代分支、输入顺序稳定、位置不重叠且内容边界可滚动。
- [ ] 通过现有 Editor Test Runner 运行聚焦测试，确认新能力尚未实现时失败。
- [ ] 使用 `Family.Members`、`World.Persons`/`TryGetLife`、`KinshipGraph.Links`、家主与继承人 ID、当前日期实现快照与布局；只忽略端点不在成员快照的边，不丢有效成员。
- [ ] 重跑聚焦测试，确认通过。

### 任务 3：面板滚动与通用绘制

**文件：** 修改 `FamilyRosterPanel.cs`，扩展 `FamilyRosterPanelTests.cs`。

- [ ] 写 `Refresh_RendersSc013KinshipEdgesAndSeparateRetainerSection`、`Refresh_ShowsUnconnectedKinAndCurrentHeadAndHeir` 失败测试，断言节点、关系线、标记与家臣分区。
- [ ] 写 `Refresh_RendersLargeRosterInsideScrollableContent`、`CloseAndReopen_DoesNotDuplicateNodesOrEdges` 测试，断言所有节点可滚动查看且反复开关/刷新不重复生成。
- [ ] 写 `OpenScrollRefreshAndClose_DoNotMutateSuccessionOrKinship`、`SessionStateChanged_RefreshesHeadAndHeirMarkers` 测试，断言面板操作不改变状态、继承后标记更新。
- [ ] 将固定 `M2RosterRow` 行遍历替换为图快照与布局结果；在 viewport 内实例化人物卡和关系线，家臣/附属成员置于独立区。
- [ ] 用现有 Editor Test Runner 验证刷新确定性及禁用时清理。

### 任务 4：现有场景生成器与验证器接入

**文件：** 修改 `AddSC013FamilyRosterPanel.cs`、`FoundationSceneValidator.cs` 并扩展测试。

- [ ] 写 `Updater_CreatesBoundScrollableGraphLayers`、`Updater_IsIdempotentAndRollsBackInvalidHierarchy`、`Validate_RejectsMissingOrDuplicateGraphBindings` 失败测试，断言滚动结构和引用、单根、幂等及回滚。
- [ ] 事务式 updater 在现有固定根下建立通用滚动层级；遵循一个 Undo group、保存前校验、异常时 Undo 回滚，回滚不重载场景。
- [ ] validator 检查层级、绑定、viewport/content 设置、打开/关闭控件；不得依赖固定角色名或人数。
- [ ] 在现有 Unity Editor 验证 updater 幂等、回滚、preview scene 隔离和 FoundationScene validator。`FoundationScene.unity` 已有工作区修改；保存前必须证明不会覆盖原有内容，否则保留场景文件并汇报具体阻塞。

### 任务 5：文档、复审与用户试玩交接

- [ ] 更新 `SC017_DEMO.md`、`PROJECT_STATE.md`、`DECISIONS.md`，记录只读通用图、家臣区、未实现编辑操作及逐步 Play Test 指引。
- [ ] 记录 Scout 调研/自建结论、改动文件、只读复审问题、Unity 编译、EditMode 总数、Console 错误/警告、场景验证、Play Mode 和用户验收，彼此分开。
- [ ] OpenCode2 完成后，通过 Orca 顺序安排独立 OpenCode 只读复审；复审者不改文件，Mochi 逐项处理并签核。
- [ ] 向用户提供纯文字 Play Test 步骤并冻结 demo worktree。用户验收前不提交、不合并、不打 tag。

## 验证门禁

- Unity 测试与场景 validator 仅通过当前已打开 Editor 的 Test Runner 执行；检查 `%LOCALAPPDATA%\Unity\Editor\Editor.log` 与 `<project>\Logs\Editor.log` 的新鲜编译/测试证据。不得启动此项目的 batchmode。
- 静态检查使用 `git diff --check` 并逐项检查新增/修改文件；不做广泛清理。
- 可视布局、滚动、关系易读性、标准/危机场景回归及验收必须由用户 Play Test；自动化测试不能替代试玩。

