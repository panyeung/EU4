# SC-020 提交年度计划按钮无响应修复报告

日期：2026-11-04  
执行者：OpenCode2（唯一写入者；未操作 Unity GUI / Computer Use / 截图；未提交、未合并、未打 tag）  
范围：仅 SC-020 运行时提交按钮绑定、聚焦 EditMode 回归测试与本报告；未修改场景、预制体、`ProjectSettings`、`Packages`、`Library`、`Temp`、`.meta`/GUID 或用户存档。

## 1. 现象

在 FoundationScene 的 SC-020 示范中，选择「学问」与导师「青叶 小夜」后点击「提交年度计划」，面板没有任何反馈，总览仍显示「本年尚未提交培养计划；未提交的年度按自由成长结算。」，计划也没有被记录。

## 2. 根因

`CharacterEducationPanel.Subscribe()` 只给打开/关闭按钮注册了运行时监听器，确认按钮从未绑定：

- `Assets/Scripts/Presentation/CharacterEducationPanel.cs` 的 `Subscribe()` 仅 `AddListener` 了 `openButton` 与 `closeButton`；
- 场景 `Assets/Scenes/FoundationScene.unity` 中 `SC020EducationConfirmButton` 的 `m_OnClick.m_PersistentCalls.m_Calls` 为 `[]`，`m_Interactable` 为 `0`（运行时由 `Refresh()` 改为可交互，但没有监听器）；
- 生成器 `AddSC020CharacterEducation` 只序列化按钮引用，不写入持久监听器；
- 现有 `CharacterEducationPanelTests` 全部直接调用 `panel.ClickConfirm()`，绕过了 `Button.onClick`，因此测试通过而真实点击无效。

结论：真实点击没有执行 `ClickConfirm()`，所以既没有调用 `TrySetEducationPlan`，也没有刷新面板或显示反馈。

## 3. 修复

仅修改 `Assets/Scripts/Presentation/CharacterEducationPanel.cs`：

1. 新增 `_confirmListener` 字段。
2. `Subscribe()` 在绑定打开/关闭按钮的同时，为 `confirmButton.onClick` 注册 `ClickConfirm`（幂等，`_confirmListener == null` 才添加）。
3. `Unsubscribe()` 对称移除该监听器并清空字段，避免 `BindReferences`/重复 `OnEnable` 造成重复提交。

未改培养规则、预览公式、计划存储、结算时机或存档逻辑；能力值仍只在 12 月→次年 1 月结算时增长。

## 4. 修复后的可见状态

选择「学问」+「青叶 小夜」并点击提交后，同一个 `Refresh()` 会读到已保存的计划：

- 状态行：`本年计划：学问／导师 青叶 小夜（学问）`，不再出现「本年尚未提交培养计划」；
- 反馈行显示：`已提交 15 岁年度计划：学问／导师 青叶 小夜。`；
- 预览保持 `20 → 29`，确认按钮仍可交互（结算前允许改选并覆盖同一 pending 计划）；
- 青叶 千景的 `学问` 仍为 20，编年史无新增教育记录，直到年度结算。

## 5. 回归测试（`Assets/Tests/EditMode/CharacterEducationPanelTests.cs`）

1. `Panel_ConfirmButtonClickSubmitsPlanShowsFeedbackAndKeepsAbilities`：通过真实 `Button.onClick.Invoke()` 点击确认按钮（先真实点击「学问」方向按钮与「青叶 小夜」导师按钮），断言计划已存储为 `Scholarship`/`SpouseId`、消息含「已提交」与导师名、状态行含「本年计划：学问」且不含「尚未提交」、能力仍为 20、编年史为空、按钮仍可交互。
2. `Panel_ConfirmButtonClickUpdatesPendingPlanAndSurvivesReopen`：提交后改为「武艺」+「青叶 宗一郎」再次点击真实按钮，断言单一 pending 计划被替换、两项能力保持原值、无编年史记录；关闭再打开后面板仍显示「本年计划：武艺／青叶 宗一郎」。

## 6. 静态证据（非 Unity Editor）

使用 Unity 6000.6.6f1 随附 Roslyn（`Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll`），按依赖顺序重新编译当前源码：

| 程序集 | 结果 | 诊断数 |
| --- | --- | ---: |
| SengokuChronicle.Domain | exit 0 | 0 |
| SengokuChronicle.Persistence | exit 0 | 0 |
| SengokuChronicle.Events | exit 0 | 0 |
| SengokuChronicle.Application | exit 0 | 0 |
| SengokuChronicle.Presentation | exit 0 | 0 |
| SengokuChronicle.Editor | exit 0 | 0 |
| SengokuChronicle.EditModeTests（全部 EditMode 源码） | exit 0 | 0 |

这是宿主侧类型检查，不是 Unity Editor 编译，也不等于 Test Runner 或 Play Mode 结果。

## 7. 未验证 / 待协调方执行

- **未运行** Unity Editor 编译、EditMode Test Runner（聚焦用例与 Run All 总计）、Console 计数、Play Mode 或用户验收；这些必须由协调方在已打开的 Editor 中复跑。
- 建议复跑：上述两个新用例 + `CharacterEducationPanelTests` 全套 + `SC020EducationSessionTests`、`CharacterGrowthSaveTests`，再跑 Run All 记录总计。
- 实时 Play Test 请确认：选择「学问」+「青叶 小夜」→「提交年度计划」后状态行与反馈行按第 4 节变化，且结算前属性不变。
- 未提交、未合并、未打 tag。
