# M09 Debug 面板拖动：诊断与回归测试

日期：2026-11-23（UTC）
基线：`docs/orca-project-workflow@47c267c`
分支 / 工作树：`panyeung/m09-debug-drag-fix`（`m09-debug-drag-fix` 隔离工作树）
Unity：`6000.6.6f1`

## 结论

拖动链路在坐标与标题栏命中两处都没有缺陷，本轮**不改产品代码**。Persistent PlayMode 测试
加载真实的 `FoundationScene`，用场景自己的 `CanvasScaler`、`GraphicRaycaster`、
`EventSystem` 与 `InputSystemUIInputModule`，配合 Input System 虚拟 `Mouse` 的按下、移动、
释放状态序列，证明指针事件一旦进入真实 UI 模块，窗口就按屏幕像素 1:1 移动，从折叠按钮按下
后拖动同样成立，松手也不会误触折叠。

仍未闭合的是“实体鼠标/OS 到 Input System 这一段”。本轮没有采集 Computer Use 合成拖动在
OS 与 Input System 交界处的事件流，因此“合成输入没有派发 EventSystem 事件”仍然只是唯一
候选假设，没有被证实。主控 2026-11-23 的两次 Computer Use 拖动没有移动面板，同一次预验收里
日期页点击有效；实体鼠标与用户最终验收都还没有完成。虚拟设备不等于实体鼠标，本轮结果不能
推断实体鼠标失效，也没有找到 OS 根因。

## 复核对象

- `Assets/Scripts/Presentation/DebugModePanel.cs`：`OnBeginDrag` / `OnDrag`（1145–1161 行）用屏幕像素位移直接写入窗口根的世界坐标；`CreateHeader`（1289–1308 行）用 `EventTrigger` 注册 `BeginDrag` / `Drag`；面板自身也实现 `IBeginDragHandler` / `IDragHandler`，作为按钮按下的冒泡兜底。
- `Assets/Scenes/FoundationScene.unity`：`Canvas` 为 Screen Space - Overlay（`m_RenderMode: 0`），`CanvasScaler` 参考分辨率 1280x720、`m_MatchWidthOrHeight: 0.5`；`EventSystem` 使用 `InputSystemUIInputModule` 与默认 UI actions。
- `ProjectSettings`：`activeInputHandler: 1`（仅新 Input System）。

## 三个假设的结论

| 假设 | 结论 | 依据 |
|---|---|---|
| 合成输入没有派发 EventSystem 事件 | 未证实，仍是唯一候选 | 虚拟鼠标状态事件进入真实 `InputSystemUIInputModule` 后窗口按屏幕像素 1:1 移动；本轮仍未采集 Computer Use 合成输入在 OS/Input System 交界的事件流 |
| 屏幕像素与 RectTransform 坐标空间不一致 | 不成立 | PlayMode 在真实画布缩放下断言屏幕位移与指针位移逐像素相等，锚点位移等于屏幕位移除以 `scaleFactor`；EditMode 手工夹具在缩放 1.5 下得到同一结果 |
| 标题栏射线被其他 UI 截获 | 不成立 | PlayMode 渲染后 `EventSystem.RaycastAll` 在标题栏抓取点的首位命中是 `DebugHeader`；折叠按钮位置的首位命中落在按钮层级内 |

Overlay 画布下画布 1 个世界单位等于 1 个屏幕像素，所以 `rect.position = eventData.position + offset`
的写法本身正确，不需要额外换算。

## 两层回归测试

### EditMode：手工夹具与手工派发

`Assets/Tests/EditMode/M09DebugPanelDragTests.cs`（3 个用例）。它的边界必须说清楚：

- `CanvasScaler` 在该夹具里被禁用，**从未执行**；画布矩形（1280x720）与局部缩放 1.5 是测试按 CanvasScaler 在 1920x1080 下的预期结果手工写入的，断言的是这份手工夹具自洽。
- 画布不渲染，`Graphic.depth` 恒为 -1，`GraphicRaycaster` 会跳过所有图形；最上层图形按绘制顺序近似取得，不是真实射线。
- `BeginDrag` / `Drag` 由测试构造 `PointerEventData` 后手工调用 `ExecuteEvents.Execute` 派发；`pointerDrag` 由 `ExecuteEvents.GetEventHandler` 解析。没有 `EventSystem.Process`、没有输入设备。
- 夹具从按下点直接派发 `BeginDrag`，没有拖动阈值台阶；生产链路的阈值位移由 PlayMode 测试处理。

3 个用例：

1. `Header_IsTheTopmostPointerTargetAcrossTheTitleBar`：绘制顺序近似下三个采样点的最上层是 `DebugHeader`，`ExecuteEvents.GetEventHandler<IDragHandler>` 解析到标题栏，`EventTrigger` 恰好注册 BeginDrag 与 Drag，序列化的 `dragHandle` 就是标题栏。
2. `Drag_MovesTheWindowOneScreenPixelPerPointerPixel_AtScaleOnePointFive`：手工铺设的缩放 1.5 下指针移动 150x-60 屏幕像素，窗口屏幕位移相同、参考单位位移为 100x-40，抓取点留在指针下方。
3. `Drag_PressesOnATitleBarControlAndStillMovesTheWindow`：手工派发时按在折叠按钮上拖动，事件冒泡到窗口根的 `OnDrag`，窗口照常移动。

### PlayMode：真实场景组件与虚拟鼠标

`Assets/Tests/PlayMode/M09DebugPanelInputChainTests.cs`（程序集 `SengokuChronicle.PlayModeTests`，
3 个用例）。这一层刻意不重复 EditMode 的手工步骤：

- 按名加载 `FoundationScene`，通过生产入口 `TitleScreenController.StartDebugSession()` 打开窗口。
- 断言场景真实组件：`CanvasScaler` 启用且为 1280x720 / match 0.5、`GraphicRaycaster` 启用、`EventSystem.currentInputModule` 是 `InputSystemUIInputModule`、画布 `scaleFactor > 0`。
- 用 Input System 的 `InputTestFixture` 隔离输入系统，新增一个虚拟 `Mouse` 设备，只做 `Move` / `Press` / `Release` 状态事件，其余交给玩家循环的输入更新与 `EventSystem.Update` 里的模块 `Process`。
- 测试不调用 `ExecuteEvents`，不调用 `OnBeginDrag` / `OnDrag`，也不手工调用 `module.Process`。
- 射线用 `EventSystem.RaycastAll` 取首位命中（真实 `GraphicRaycaster`，等待渲染分配 `Graphic.depth`）；拖动断言以越过 `EventSystem.pixelDragThreshold` 之后为基线，因为阈值内的位移由 uGUI 的 `BeginDrag` 吸收。

3 个用例：

1. `SceneWiring_RealScalerRaycasterAndInputModule_HeaderIsTopmostAtGrabPoint`：真实组件全部在线，标题栏抓取点首位射线命中 `DebugHeader`。
2. `VirtualMouse_DragOnHeader_MovesWindowByScreenPixels`：虚拟鼠标在标题栏按下并拖动 150x-60 像素，窗口屏幕位移逐像素相等，锚点位移等于屏幕位移除以 `scaleFactor`，松开后窗口保持打开。
3. `VirtualMouse_DragStartingOnFoldButton_MovesWindowWithoutClickingIt`：折叠按钮位置首位射线命中落在按钮层级内；从按钮按下后拖动 90x-45 像素窗口照常移动；松手后折叠按钮没有收到点击，窗口未折叠。

## 证据

| 项 | PlayMode | EditMode 全量 |
|---|---|---|
| 命令 | `unity command run_tests --mode playmode --filter M09DebugPanelInputChain --async_tests true --timeout 420` → `test_status` | `unity command run_tests --mode editmode --async_tests true --timeout 900` → `test_status` |
| 结果 | `3/3` 通过，失败 0，`duration=1.3884227s`（`2026-11-23 22:47:43Z`–`22:47:45Z`） | `2301/2301` 通过，失败 0，`duration=65.7882723s`（`2026-11-23 22:48:13Z`–`22:49:19Z`） |
| 副本 | `Logs/verify/20261123-m09-drag-playmode-final/TestResults.xml` | `Logs/verify/20261123-m09-drag-editmode-final/TestResults.xml` |
| SHA-256 | `73bb6d04cbd1d47d8b95359593996f719cca50e01ced71c4b554c823b445f27e` | `9b9b8490645df0c3d6829fe125b6cf86be91186f958d68d637e1188941771308` |
| Console | 跑完 `error=0`、`warn=1`（TMP 后备字体提示） | 跑完 `error=2`，两条都是故障注入用例经 `GameSession.ReportChronicleAppendFailure` 主动打印，非新缺陷 |
| Editor 状态 | `playMode=stopped`、`compiling=false` | 同左 |
| 场景 | `FoundationScene` 为活动场景，`isDirty=false` | 同左 |

PlayMode 用例逐项：

| 用例 | 结果 | 时长 |
|---|---|---|
| `SceneWiring_RealScalerRaycasterAndInputModule_HeaderIsTopmostAtGrabPoint` | Passed | 0.601091s |
| `VirtualMouse_DragOnHeader_MovesWindowByScreenPixels` | Passed | 0.4368222s |
| `VirtualMouse_DragStartingOnFoldButton_MovesWindowWithoutClickingIt` | Passed | 0.2982916s |

本机证据目录 `Logs/verify/` 被 `.gitignore` 排除，只在本机保留副本；提交里不放结果文件。

## 2026-11-23 主控 Computer Use 结果

同一台机器、同一个 M09 Editor 上，主控用 Computer Use 做过两次合成拖动：两次都没有让面板
移动；同一次预验收里日期页点击有效。结论边界：

- 点击有效、拖动无效，说明合成指针至少能到达部分 UI，但没有证据说明拖动事件流在哪一层
  丢失。本轮 PlayMode 测试只证明“事件进入真实 UI 模块后的行为正确”，不覆盖 OS 采集段。
- 虚拟鼠标设备不是实体鼠标，也不经过 Windows 的原始输入路径；不能据此说实体鼠标拖动可用或
  失效。
- 实体鼠标拖动、松开后窗口停留、底层控件未误触仍需用户在有实体鼠标的环境里验收。
- 本轮没有找到 OS 根因；Computer Use 的合成拖动仍标为未闭合。

## 下一步建议

1. 合入本分支的测试与报告，锁定坐标、命中与按钮冒泡行为；不需要场景或预制体改动。
2. 用户按 PR #3 的步骤验收实体鼠标拖动；本轮不把它标为通过。
3. 若要闭合“合成输入为何不派发事件”，下一步应采集 `InputSystemUIInputModule` 的 pointer
   事件流与 OS 侧原始输入（例如 Input System 事件日志、`InputSystem.onEvent` 记录、Windows
   原始输入计数器），而不是再改坐标代码。
4. 现在不要为“坐标空间”加防御性转换。只有将来把该画布改成 Screen Space - Camera 时，
   `rect.position` 直写才会失效，届时应改用
   `RectTransformUtility.ScreenPointToWorldPointInRectangle`。

## 未改动范围

未改 `DebugModePanel.cs` 或任何产品代码；未改场景、预制体、`ProjectSettings`、`AGENTS.md`
或 `.vscode/settings.json`。Unity 运行产生的临时差异（性能测试资源删除、IET 设置、编辑器
设置）保留在工作树但未提交。
