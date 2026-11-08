# 五张 UI 参考图实现计划

> **For agentic workers:** Execute this plan in the existing checkout. No new branch or worktree. Each page must preserve its existing domain commands and save behavior.

**Goal:** 将五张用户提供的页面设计图逐页实现为保留真实数据和交互的 Unity uGUI 页面。

**Architecture:** 共用 `SengokuUiTheme` 颜色、字体、纸纹和按钮样式；页面继续由现有 Presentation 控制器承接数据/命令，Editor 生成器负责事务性布局；增加一个轻量页面导航只负责切换已有视图。没有必要新增领域层或存档结构。

**Tech Stack:** Unity 6.6.6f1、C#、uGUI、现有 Unity Test Framework。

**Spec:** `docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`

## Global Constraints

- 保留当前 `main` 工作区；不建 branch/worktree，不覆盖既有 M04A/graphify/nul WIP。
- 不修改领域规则、存档格式、事件目录、依赖、ProjectSettings、Library 或 Temp。
- 只使用用户提供的设计图及其装饰裁切；列在 `ASSETS.md`。
- Unity 仅使用 PID 91392（若仍指向本项目）；每次交互前重新核验 Editor PID 和 `-projectPath`。
- 修改场景时使用单一固定根、单一 Undo 组、保存前验证和异常回滚。
- 每个页面分别记录编译、EditMode、Validator、Console 与 Play Mode 证据；截图立即检查后删除。

## Review Focus

- 页面切换不留下多个模态层互相拦截输入；由共享导航测试切换后只有目标页面接收射线。
- 无真实数据的示例字段不应显示伪造值；逐页检查绑定来源。
- 既有按钮命令和存档路径保持原样；用对应的既有 Panel tests 覆盖。
- 1280×720 下文本/按钮不裁切且布局不重叠；Validator 和尺寸测试逐页覆盖。
- 引用图裁切不可带入残留文案/边框；在 Unity Editor 内实际检查每个导入 Sprite。

---

### Task 1: 完成人物档案与日常行动页（参考图 1）

**Files:** `Assets/Scripts/Presentation/DailyActionPanel.cs`, `CharacterDailyPageView.cs`, `SengokuUiTheme.cs`, `Assets/Scripts/Editor/AddM02DailyActionPanel.cs`, `Assets/Tests/EditMode/M02DailyActionPanelTests.cs`, `ASSETS.md`。

- [ ] 修复/完成现有 M02 WIP；正确绑定真实会话字段和当前命令。
- [ ] 使用参考图布局：档案、关系、经历标签；人物/关系区；行动卡、进度、目的地和确认操作；独立 AI 调试栏。
- [ ] 裁切并检查人物/旅行装饰图；不把参考图中的姓名或数值当真实游戏状态。
- [ ] 在 EditMode 覆盖布局绑定、动态刷新、操作回归、生成器幂等/回滚/边界。
- [ ] 运行本页 focused EditMode 与场景 Validator。

### Task 2: 家谱与继承页（参考图 2）

**Files:** `Assets/Scripts/Presentation/FamilyRosterPanel.cs`, `SuccessionPanel.cs`, `FamilyTreeGraphBuilder.cs`, `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`, `AddSC010SuccessionPanel.cs`, 对应测试。

- [ ] 将真实家谱连线/世代卡与现有继承候选、摄政/让位决策组合到同一页面构图。
- [ ] 保留多人/无上限亲属图的滚动、关系类型、当前继承人和全部既有继承命令。
- [ ] 在 EditMode 覆盖图布局、选中继承人联动、决策调用及画布边界。

### Task 3: 家族内务页（参考图 3）

**Files:** 当前家族/产业/事件/月结 Presentation 组件、对应 Editor 生成器、`FoundationSceneValidator.cs`、对应 EditMode tests、`ASSETS.md`。

- [ ] 复用真实家族名册、任命、产业和当前事件/待决视图，组成左家主信息、中部内务、本月事件、右侧动向/待决区。
- [ ] 不创建缺少领域来源的“天下动向”数值或待决事项。
- [ ] 覆盖空列表、单条/多条事件与决策卡的布局及按钮绑定。

### Task 4: 地图与出行页（参考图 4）

**Files:** `Assets/Scripts/Presentation/WorldMapController.cs`, `Assets/Scripts/Editor/CreateM2WorldMap.cs`, `UpdateM2WorldMapLayout.cs`, `WorldMapLayout.cs`, `Assets/Tests/EditMode/WorldMapUiTests.cs`, `CreateM2WorldMapGeneratorTests.cs`。

- [ ] 按左行程栏、中地图、右情报栏重排现有地图与目的地选择。
- [ ] 路线/天数/抵达日期从现有 `WorldMapRules` 与 `GameSession` 派生；路线和出行按钮真实执行。
- [ ] 覆盖目的地/无路由/地图点击/尺寸与生成器回滚。

### Task 5: 事件抉择页（参考图 5）

**Files:** `Assets/Scripts/Presentation/EventChoicePanel.cs`, `Assets/Scripts/Editor/AddSC005EventChoicePanel.cs`, `EventChoicePanelTests.cs`, `AddSC005EventChoicePanelTests.cs`, `ASSETS.md`。

- [ ] 采用左导航、中央事件插画/叙事/选项、右待处理事项构图。
- [ ] 事件内容、选项可用条件和结果继续来自事件目录与 `GameSession.TryChooseEventOption`。
- [ ] 覆盖多选项、禁用/可用状态、反馈、待决队列和输入层级。

### Task 6: 全套集成验证

**Files:** `FoundationScene.unity`, `FoundationSceneValidator.cs`, `docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`。

- [ ] 页面导航逐页切换，不改变游戏状态，也不留下隐藏但阻挡射线的面板。
- [ ] 在当前 Unity Editor 运行全量 EditMode、场景 Validator，并单独记录 Console。
- [ ] Play Mode 核验五页主要交互和视觉；清理所有 Computer Use 截图。
- [ ] 仅提交本任务文件并 push 当前分支；用户最终确认之前不打 tag、不做额外 milestone 宣称。

## Continuation log — 2026-11-08

- 修复事件空队列页的中文文字未渲染：运行时标签改用 Unity 内置 `LegacyRuntime.ttf`，避免系统动态字体在此 Canvas 中不出字；现有事件面板和领域状态未变。
- 继续收敛全局框架：生成器和页面导航运行时均将底色设为深靛色、左导航设为不透明深色栏；运行时在 Canvas 最底层放置不接收点击的共享背景，避免切页时露出旧蓝底。EditMode 覆盖运行时主题和生成器颜色。
- Unity Test Runner EditMode 再次全量通过：1293/1293，0 失败、0 跳过。Play Mode 已在五页间切换，确认共享深色背景持续显示；完整地图主视觉、家族内务的多栏信息和家谱人物肖像仍未复刻到参考稿精度。
- Unity Test Runner EditMode：1293/1293 通过，0 失败、0 跳过。Play Mode 已逐页查看人物、家谱、家族内务、地图和事件页；事件空态文字可见。
- 五页仍未达到参考稿的版式和素材精度。地图缺少完整地图画面，家族内务/事件布局及全局导航仍需继续完善；本次不标记整项完成。
- 当前场景保持未保存；工作区包含无关脏改动，不保存或提交整个场景。

### Map illustration slice — 2026-11-08

- Added the owner-provided central map crop (Assets/Resources/ReferenceArt/map_illustration.png) behind the existing map-page controls. The image does not receive raycasts; the original route and travel controls remain the interaction layer.
- EditMode coverage checks resource presence, bottom-sibling placement, and raycast transparency. Full Editor verification and a Play Mode visual check remain pending for this slice.
- This improves only the central map illustration. The reference's left travel column, right intelligence panel, map markers and route artwork are still not fully reproduced.
