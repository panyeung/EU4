# M6 设计：标题界面与 Phase 1 演示封装（Title Screen & Demo Packaging）

日期：2026-11-01
状态：已批准（用户选择方向 1，继续使用 `feature/m5-save-slot` 分支与本工作树）
前置：M1–M5 全部已接受；`demo-M5` 已合并并推送。

## 目标

让 Phase 1 演示真正「从标题开始、可继续、有结算」：

1. Play Mode 启动时显示全屏标题界面（`M6TitleRoot`），提供「新游戏」与「继续」。
2. 「继续」直接读取 M5 单槽存档；无存档/坏档时在标题上给出清晰提示，不进入地图。
3. 三项任务全部结算后，地图上出现一行演示完成摘要（含总功绩），作为 Phase 1 的可见收尾。

## 明确不做

- 不做多槽存档、设置页、退出到主菜单、场景重载、淡入淡出动画。
- 不修改 `SaveData` 版本、`SaveSlotStore` 行为、`SaveLoadPanel`（游戏内存档按钮保持原样）。
- 不新增 package；仅 uGUI + 现有 LegacyRuntime 字体。

## 设计

### 场景结构（由 `AddM6TitleScreen` 事务式更新器生成）

```
Canvas
├─ M2WorldMapRoot（现有；新增子节点 M6DemoSummaryText，位于 TownMenuRoot 之前，默认隐藏）
└─ M6TitleRoot（默认隐藏，最后一个兄弟 → 渲染在最上层）
   ├─ M6TitleLabel          「战国立志传 · Phase 1 演示」
   ├─ M6NewGameButton       「新游戏」
   ├─ M6ContinueButton      「继续」
   └─ M6TitleStatusText     一行状态提示
GameSession 对象上新增组件：TitleScreenController、DemoSummaryPanel
```

- `M6TitleRoot` 为全屏 stretch RectTransform + 不透明 `Image`（raycastTarget=true），标题显示时天然屏蔽下层地图输入。
- 两个根在保存的场景里都是 inactive：编辑模式视野不变，Validator 布局检查不受影响；Play Mode 由控制器激活。

### TitleScreenController（Presentation）

- `Bind(session, store, mapController, titleRoot, newGameButton, continueButton, statusText)` + `SetStore()` 测试注入口；`Awake` 缺省创建 `SaveSlotStore.CreateDefault()`。
- `Start`：显示标题、绑定按钮监听、`RefreshTitleState()`。
- `RefreshTitleState()`：`store.Load()` → `NotFound` 显示「尚未找到存档。」并禁用继续；其他失败显示具体错误并禁用继续；成功显示「检测到存档，可继续上次进度。」并启用继续。
- `StartNewGame()`：`session.InitNewGame()` → `mapController.ReturnToMap()` → 隐藏标题。
- `TryContinue()`：`session.LoadFrom(store)`。失败：状态栏显示消息、标题保持、会话不变（`TryApplySave` 本身是原子的）；成功：`ReturnToMap()` 刷新全部面板后隐藏标题。
- 暴露 `Status`、`LastOperationSucceeded`、`IsTitleVisible` 供测试与诊断。

### GameSession.IsDemoComplete（纯 C#）

`SupplyShortageStage == RewardClaimed && SealedLetterStage == RewardClaimed && MarketTallyStage == RewardClaimed`。不新增状态，不写存档。

### DemoSummaryPanel（Presentation）

挂在始终活跃的 GameSession 对象上；`Update()` 每帧调用幂等 `Refresh()`（一次枚举比较，成本可忽略；规则本体在 domain 属性中）。完成时激活并显示 `M6DemoSummaryText`：「Phase 1 演示完成。总功绩：{Merit}。」未完成时隐藏。载入完成档同样会显示（读档不重复领奖的性质不变）。

### Validator / 更新器

- `AddM6TitleScreen`：一个 Undo 组、验证后才保存、异常回滚、不重载场景；`RunBatchIntegration()` 批量入口；`Validate(scene, out error)` 检查唯一性、父子与兄弟次序（标题根是 Canvas 最后一个兄弟且在地图根之后）、绑定完整、摘要文本在地图矩形内且不与 M5 控件重叠（M5 根存在时检查）。
- `FoundationSceneValidator`：新增 `FOUND M6TitleRoot / TitleScreenController / DemoSummaryPanel`、绑定检查与 `AddM6TitleScreen.Validate` 调用。

## 测试

- `TitleScreenControllerTests`：新游戏重置会话并隐藏标题；继续载入存档（日期/任务阶段还原、标题隐藏）；缺档禁用继续且提示、标题保持；坏档禁用继续、会话与槽文件均不变。
- `DemoSummaryPanelTests` + `GameSession` 属性测试：三项全部 RewardClaimed 前不显示、后显示且含总功绩 20；部分完成不显示。
- `AddM6TitleScreenTests`：preview-scene 结构/绑定/幂等/故障注入回滚，测试缝永不保存场景。

## 验收路径（用户 Play Mode）

启动 Play → 标题可见 →（无档时继续禁用）→ 新游戏进入地图 → 游玩并存档 → 退出再进入 Play → 继续 → 状态与存档一致 → 完成全部三任务 → 看到「Phase 1 演示完成」摘要行。
