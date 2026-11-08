# 五张参考图界面完成计划

> **执行方式：**由 OpenCode2 在现有 `main` checkout 实施；不创建新项目、branch 或 worktree。Mochi 负责场景集成、Unity 验证与 Play Mode 验收。

**目标：**把五个参考页面逐页补到获批设计稿的构图与视觉精度，同时保留真实游戏数据、交互命令和存档行为。

**架构：**沿用已存在的 `ReferenceGalleryNavigator`、页面 Presentation、领域命令与事务式 Editor 生成器。只完善共享主题/导航和页面布局、绑定、素材；不另建 UI 框架或重写游戏流程。

**技术栈：**Unity 6000.6.6f1、C#、uGUI、现有 Unity Test Framework。

**设计依据：**`docs/superpowers/specs/2026-11-07-ui-reference-gallery-design.md`（用户已确认五页及复刻方向）。

## 全局约束

- 只在 `C:\Users\panye\My project` 的现有 `main` 上工作，不覆盖或清理已有 WIP。
- 参考画布为 1280×720；五张原图均为 1668×941。保留统一的深靛外框、纸本内容区、金色细节、朱印强调和易读中文字体。
- 参考图提供构图与美术方向；姓名、数值、事件和人物信息一律来自实时会话。没有真实数据的区域显示空态或省略，不造假。
- 保留既有领域规则、存档格式、事件结果、旅行/日历/月结/继承命令；不增加第三方包、不改 `ProjectSettings`、`Library` 或 `Temp`。
- 素材仅用用户提供的参考图裁切和项目原创素材；新增来源、授权记入 `ASSETS.md`。保留所有 `.meta` 和 GUID。
- 仅指定集成人员修改 `FoundationScene`。使用既有事务式生成器、单一 Undo 组、保存前验证和 Undo 回滚。
- Unity 操作前核验唯一 Editor PID 与 `-projectPath`。分别记录编译、EditMode、Console、Validator、Play Mode 和存档验证；不得以 EditMode 代替运行时或视觉验收。
- 代码由 OpenCode2 通过 Orca 受监督任务实施；五页按顺序完成，一次只保留一个写入者。完成后由独立 OpenCode worker 只读审查；Mochi 负责场景集成、验证与最终签核。
- Play Mode 可用 Computer Use 和截图；截图仅用于检查，检查后立即删除，不放入仓库或备份。
- 每道验证门通过后，只提交并推送本计划的已验证文件至 `origin/main`。用户明确接受整套演示前不打 milestone tag；不宣称五页完成。

## 评审重点

- 1280×720 与其他 16:9 分辨率下文字可读、按钮可点、内容不裁切或互相遮挡。
- 页面切换后只有目标页面接收输入；隐藏层、插画和背景不阻挡射线。
- 页面状态刷新后仍来自真实数据；无人、无事件、无决策或无路线时有清楚空态。
- 五页上的保存、人物选择、行动、继承、月结、旅行和事件选择仍调用既有命令。
- 家谱和名册仍支持任意人数、未连接亲族及家臣；不得为贴合截图而恢复固定人数/世代限制。

---

## 当前基线

- `e298312` 已加入五页导航、共享主题与素材、人物日常页、家族内务组合、中央地图插画及事件/家族页面重排；这些是部分实现，不代表达到参考图精度。
- 最新提交的全量 EditMode 为 1303/1303，含当前 Reference Gallery 回归；当前 `main` 尚无该提交后的五页完整 Play Mode 与完整场景验收证据。
- 继续实现前应以现有 `FoundationScene` 和各页面当前尺寸为准；先确认最新 Validator，不要从历史场景报告推断当前状态。

## 实施任务

### Task 1：锁定共用页面框架

**文件：**
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 修改：`Assets/Scripts/Presentation/SengokuUiTheme.cs`
- 修改：`Assets/Scripts/Presentation/SengokuUiArt.cs`
- 修改：`Assets/Scripts/Editor/AddM02DailyActionPanel.cs`
- 测试：`Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs`

- [ ] 对照参考图固定顶栏、侧栏、内容安全区、共享底色和页面选中态；不重复创建现有通用控件。
- [ ] 明确五个页面根与共享区域的激活/射线规则；页面切换不能改变会话状态。
- [ ] 扩展 `ReferenceGalleryNavigatorTests.ShowPage_ActivatesOnlyTheSelectedPageAndItsPanels`，断言全部五页的唯一激活根、导航状态和射线隔离。
- [ ] 在 1280×720 布局断言中验证共享边界、页签点击区和不重叠。

### Task 2：人物档案与日常行动页

**文件：**
- 修改：`Assets/Scripts/Presentation/DailyActionPanel.cs`
- 修改：`Assets/Scripts/Presentation/CharacterDailyPageView.cs`
- 修改：`Assets/Scripts/Presentation/NpcAiInspectorPanel.cs`
- 修改：`Assets/Scripts/Editor/AddM02DailyActionPanel.cs`
- 测试：`Assets/Tests/EditMode/M02DailyActionPanelTests.cs`
- 测试：`Assets/Tests/EditMode/NpcAiInspectorPanelTests.cs`

- [ ] 按图组织人物档案/关系/经历、中部关系展示、右侧日常计划和最右 AI Inspector。
- [ ] 绑定人物姓名、年龄、关系、能力、行动进度与目的地；AI Inspector 仍只读并能滚动查看决策说明。
- [ ] 保留休息、训练、工作、旅行、重排/中断、日推进及月结行为。
- [ ] 补充布局边界、动态刷新、不同人物选择、长文本和无行动/无关系空态测试。

### Task 3：家谱与继承页

**文件：**
- 修改：`Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- 修改：`Assets/Scripts/Presentation/FamilyTreeGraph.cs`
- 修改：`Assets/Scripts/Presentation/SuccessionPanel.cs`
- 修改：`Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`
- 修改：`Assets/Scripts/Editor/AddSC010SuccessionPanel.cs`
- 测试：`Assets/Tests/EditMode/FamilyRosterPanelTests.cs`
- 测试：`Assets/Tests/EditMode/AddSC010SuccessionPanelTests.cs`
- 测试：`Assets/Tests/EditMode/GameSessionSuccessionTests.cs`

- [ ] 将家谱世代卡与亲属连线置于中央，将当前家主、继承人、支持度及继承/摄政决策放在右侧层级区。
- [ ] 区分血亲、配偶、收养、未连接亲族与家臣；卡片头像必须匹配人物身份，缺图时使用清楚的通用占位。
- [ ] 保留滚动与任意规模图布局，以及指定继承人、让位、摄政等既有命令。
- [ ] 覆盖大族谱、无配偶/无继承人、未成年继承人、滚动边界、选中联动和按钮调用。

### Task 4：家族内务页

**文件：**
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 修改：`Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- 修改：`Assets/Scripts/Presentation/IndustryPanel.cs`
- 修改：`Assets/Scripts/Presentation/EventChoicePanel.cs`
- 测试：`Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs`
- 测试：对应产业面板与事件面板测试

- [ ] 按参考图组合家主/日期侧栏、家族成员与任命、本月事件、可执行待决事项。
- [ ] 成员、职位、产业状态、事件与决策从会话快照读取；缺少世界动态或决策数据时隐藏该区或显示空态。
- [ ] 保留产业管理、任命、事件处理与结束月份的现有入口和命令。
- [ ] 覆盖零/一/多成员、零/多事件及零/多待决事项的排版和射线区域。

### Task 5：地图与出行页

**文件：**
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 修改：`Assets/Scripts/Presentation/WorldMapController.cs`
- 修改：`Assets/Scripts/Editor/CreateM2WorldMap.cs`
- 修改：`Assets/Scripts/Editor/UpdateM2WorldMapLayout.cs`
- 修改：`Assets/Scripts/Editor/WorldMapLayout.cs`
- 测试：`Assets/Tests/EditMode/WorldMapUiTests.cs`
- 测试：`Assets/Tests/EditMode/CreateM2WorldMapGeneratorTests.cs`
- 素材登记：`ASSETS.md`

- [ ] 完成左侧队伍/路线计划、中央地图、右侧情报/人物/委托三栏构图；中央地图插画按比例适配。
- [ ] 路线、天数、抵达日期和风险均从现有地图规则派生；没有可展示的数据不填示例文本。
- [ ] 保留目的地选择、路线预览、确认、取消和旅行结算；插画与装饰不阻挡点击。
- [ ] 覆盖无路线、远程目的地、长标签、不同分辨率及插画/按钮边界。

### Task 6：事件抉择页

**文件：**
- 修改：`Assets/Scripts/Presentation/EventChoicePanel.cs`
- 修改：`Assets/Scripts/Editor/AddSC005EventChoicePanel.cs`
- 修改：`Assets/Scripts/Presentation/ReferenceGalleryNavigator.cs`
- 测试：`Assets/Tests/EditMode/EventChoicePanelTests.cs`
- 测试：`Assets/Tests/EditMode/AddSC005EventChoicePanelTests.cs`
- 测试：`Assets/Tests/EditMode/ReferenceGalleryNavigatorTests.cs`

- [ ] 按左侧导航、中央叙事/选项卡、右侧待决事项区域排版，插画与文本按实际待处理事件变化。
- [ ] 保留条件禁用状态、`TryChooseEventOption`、结果反馈、待处理队列和空态。
- [ ] 覆盖长叙事、多个选项、禁用选项、无待决事件与确认后队列刷新。

### Task 7：场景集成与五页验收

**文件：**
- 修改：`Assets/Scenes/FoundationScene.unity`（仅由指定集成人员经现有生成器更新）
- 修改：`Assets/Scripts/Editor/FoundationSceneValidator.cs`
- 文档：`ASSETS.md`、`PROJECT_STATE.md`、五页试玩报告

- [ ] 先确认启动入口与 `FoundationScene` 的关系，运行当前场景 Validator 并记录基线缺陷。
- [ ] 用事务式生成器接入所有改动页面；验证根节点唯一、引用完整、画布内布局有效且场景保存干净。
- [ ] 当前 Unity Editor 完成编译、聚焦与全量 EditMode；执行 `FoundationSceneValidator.ValidateActiveScene` 并单独记录 Validator 与 Console 结果，解释预期日志。
- [ ] 在 Play Mode 逐页检查五张参考图的整体构图、比例、素材、可读性和交互；核验切页后返回各页状态仍同步。
- [ ] 对包含在范围内的行动、旅行、继承、产业、事件与存档操作检查实际结果；退出 Play Mode 后确认 Console 与场景状态。
- [ ] 检查每页临时截图后立即删除全部 Computer Use 截图；更新项目状态和验证报告，提交并推送已验证文件。

## 整套验收条件

- 五个页面均达到设计稿规定的主区域、视觉层级和色彩/纹理方向；不以“导航可用”或“插画已显示”代替视觉完成。
- 所有展示值来自当前游戏状态；无虚构数据，真实命令仍可完成并得到清楚反馈。
- Unity 编译成功、目标 EditMode 全绿、`FoundationSceneValidator` 无新缺陷、Console 统计单独记录、五页 Play Mode 路线通过。
- Play Mode 与技术验证通过后提交推送；整套 UI 的 milestone 仍需用户明确验收后才可标记完成或打 tag。
