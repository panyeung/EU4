# M09 Debug 面板拖动：诊断与回归测试

日期：2026-11-23（UTC）
基线：`docs/orca-project-workflow@47c267c`
分支 / 工作树：`panyeung/m09-debug-drag-fix`（`m09-debug-drag-fix` 隔离工作树）
Unity：`6000.6.6f1`

## 结论

拖动链路在坐标与标题栏命中两处都没有缺陷，本轮**不改产品代码**。PlayMode 回归测试加载真实
`FoundationScene`，用场景自己的 `CanvasScaler`、`GraphicRaycaster`、`EventSystem` 与
`InputSystemUIInputModule`，配合虚拟 `Mouse` 的按下、移动、释放序列，证明指针事件一旦进入
真实 UI 模块，窗口就按屏幕像素 1:1 移动；从折叠按钮按下后拖动同样成立，松手也不会误触折叠。

2026-11-23 的 Computer Use 追踪只覆盖到 Input System 这一层：Computer Use 产生的 `Mouse`
InputEvent 被 Input System 记录（位置、delta、`leftButton` 都有），EventSystem 也派发了
`PointerDown` / `PointerUp` / `PointerClick` 与窗口根的 `InitializePotentialDrag`；但合成拖动
把 `leftButton=1` 与终点位置合并进同一帧，模块记录到的 `pressPosition` 就等于终点，uGUI 的
10px 拖动阈值永远不满足，因此整段没有 `BeginDrag` / `Drag` / `EndDrag`。可见结果是面板未移动、
「事件」页签被选中。这次观测只反映 Computer Use 合成输入把按下与位移合并到同一帧：它不能判断
产品在真实输入下的运行表现，也不能判断实体鼠标是否正常，因此本轮不能算作通过；实体鼠标拖动与
用户最终验收仍未完成。追踪从 Input System 的 `onEvent` 起记，它能证明合成事件已进入 Input
System，不能证明 Windows 原始输入链。

## 复核对象

- `Assets/Scripts/Presentation/DebugModePanel.cs`：`OnBeginDrag` / `OnDrag`（1145–1161 行）用屏幕像素位移直接写入窗口根的世界坐标；`CreateHeader`（1289–1308 行）用 `EventTrigger` 注册 `BeginDrag` / `Drag`；面板自身也实现 `IBeginDragHandler` / `IDragHandler`，作为按钮按下的冒泡兜底。
- `Assets/Scenes/FoundationScene.unity`：`Canvas` 为 Screen Space - Overlay（`m_RenderMode: 0`），`CanvasScaler` 参考分辨率 1280x720、`m_MatchWidthOrHeight: 0.5`；`EventSystem` 使用 `InputSystemUIInputModule` 与默认 UI actions。
- `ProjectSettings`：`activeInputHandler: 1`（仅新 Input System）。

## 三个假设的结论

| 假设 | 结论 | 依据 |
|---|---|---|
| 合成输入没有派发 EventSystem 事件 | 不成立 | 本轮追踪里 EventSystem 真实派发了 `PointerDown` / `PointerUp` / `PointerClick`（焦点点击）与 `InitializePotentialDrag`（拖动按下）；断点在之后的拖动阈值：按下与终点同帧，`pressPosition == position` |
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
| Console | 跑完 `error=0`、`warn=1`（TMP 后备字体提示，逐条见下表归因） | 跑完 `error=2`，两条都是故障注入用例经 `GameSession.ReportChronicleAppendFailure` 主动打印，非新缺陷 |
| XML 标识 | `<property name="platform" value="PlayMode" />`，程序集 `SengokuChronicle.PlayModeTests.dll`，3 个 M09 用例 | `<property name="platform" value="EditMode" />`，2301 个用例，其中 M09 拖动 3 个 |
| Editor 状态 | `playMode=stopped`、`compiling=false` | 同左 |
| 场景 | `FoundationScene` 为活动场景，`isDirty=false` | 同左 |

PlayMode 用例逐项：

| 用例 | 结果 | 时长 |
|---|---|---|
| `SceneWiring_RealScalerRaycasterAndInputModule_HeaderIsTopmostAtGrabPoint` | Passed | 0.601091s |
| `VirtualMouse_DragOnHeader_MovesWindowByScreenPixels` | Passed | 0.4368222s |
| `VirtualMouse_DragStartingOnFoldButton_MovesWindowWithoutClickingIt` | Passed | 0.2982916s |

本机证据目录 `Logs/verify/` 被 `.gitignore` 排除，只在本机保留副本；提交里不放结果文件。

### Console 归因（按运行分段）

编译历史（`Logs/Editor.log`）：

- 初次接入 PlayMode 程序集时出现 6 条 `CS0246: UnityTestAttribute/UnityTest`，原因是把测试程序集引用写进了 `precompiledReferences`，而不是 `references`；改正后消失。同一文件此前还有 2 条 `CS0618 FindFirstObjectByType` 弃用警告，已改用 `FindAnyObjectByType`。
- 最终编译采样记录 `compilationFailed=false`、`consoleErrors=0`、`consoleWarnings=0`；`Logs/Editor.log` 第 2738 行记录 Tundra build success，但紧随的第 2739–2740 行仍保留两条 `CS0618 FindFirstObjectByType` 弃用警告。因此不能表述为该日志尾部没有 `warning CS`。

按运行分段（`console_status.groundTruth` 与 XML 内输出对照）：

| 运行 | 紧接其后的 groundTruth | XML 内可归因内容 |
|---|---|---|
| PlayMode 3 项（22:47:43Z–22:47:45Z） | error=0、warn=1、log=4 | PlayMode XML 唯一的输出行就是 TMP 后备字体警告，挂在 `SceneWiring_...` 用例的 `<output>` 下；没有 error 行 |
| EditMode 全量（22:48:13Z–22:49:19Z） | error=2、warn=58、log=15 | 2 条 error 正是故障注入用例的「家族编年史记录写入失败（家族收养/婚姻）」；warn 噪声来自既有的各 M0x/SC-0xx updater rolled back 探针、UAC0009 与 TMP 提示 |

主控先前读到的 `error=2`、`warn=51` 属于 EditMode 全量运行的 Console，不是 PlayMode 运行；Test Runner 在每次运行开始时清空 Console，因此 PlayMode 运行后的窗口里只剩 `0 error / 1 warning`。警告没有逐条归因到每个用例，只对上述 2 条 error 与 PlayMode 的 1 条 warning 做了逐条对照。

`test_status` 读取全机共享的 `Temp/pipeline_test_status.json`，会返回最后一次完成的结果（当时是 2301/2301 EditMode）。PlayMode 结果以下列三项为准：复制副本的 `start-time=2026-11-23 22:47:43Z`（晚于修复后的编译）、`<property name="platform" value="PlayMode" />`、三个 `SengokuChronicle.Tests.PlayMode.M09DebugPanelInputChainTests` 用例。旧 22:43 副本（含 1 条失败）已重命名为 `Logs/verify/20261123-m09-drag-playmode-superseded-2243`，不再作为证据。

## 2026-11-23 Computer Use 逐层追踪

主控在同一台机器的 M09 Editor（PID `132232`，Pipeline 端口 `7803`）上做了第三次 Computer Use
追踪（此前两次合成拖动均未移动面板，日期页点击有效）：

先在 Game 视图点击 `(800,190)` 取焦点，再从 `(800,190)` 拖到 `(980,260)`。窗口坐标换算到游戏
像素的比例约 2.468、y 轴向下翻转：起点 `(1786.7,1997.9)` 落在标题栏内，终点 `(2231.0,1825.1)`
落在页签行。

### 采集方式与可信范围

临时诊断器（`Assets/Scripts/Editor/M09InputTrace/`，诊断后已删除）挂在
`EditorApplication.update` 与运行期 `EventTrigger` 观察项上，记录 `InputSystem.onEvent` 事件流、
EventSystem 首命中、标题栏与窗口根收到的指针/拖动回调、窗口屏幕位置与焦点。

可信范围有两重限制。其一，`Pointer.current.position` 在 editor 侧回调里始终读回 `(0,0)`，由它
派生的 `POINTER-MOVE` 与 `RAYCAST` 行在这段时间不可用。结论只采用两类行：`INPUT-EVENT`
（`InputSystem.onEvent` 收到的 `Mouse` 事件）与 `EVT`（EventTrigger 收到的 `PointerEventData`）。
其二，`INPUT-EVENT` 在 Input System 的 `onEvent` 回调里采集，所以它只能说明事件已进入 Input
System。这些行不能证明 Windows 原始输入链，包括 raw input 消息如何到达、是否存在中间注入或改写
层；报告不据此推断 Computer Use 到 Input System 之间的具体路径。

### 原始记录

焦点点击（帧 146796）：

- `13:25:17.908`：`/Mouse/position/x=1786.697 y=1997.897 delta=(-300,-10)`
- `13:25:17.962`：同一帧内 `/Mouse/leftButton=1` 与 `/Mouse/leftButton=0`
- 同一帧：`EVT HEADER PointerDown → InitializePotentialDrag → PointerUp → PointerClick`，
  `clickCount=1`，`pos=(1786.70,1997.90)`，`pressObject=DebugHeader`

拖动（帧 150818–150824）：

- `13:25:26.665`：`/Mouse/delta/x=270 y=-105` 与 `/Mouse/leftButton=1` 在同一条事件里；
  同一帧 `/Mouse/position/x=2231.04 y=1825.097 delta=(0,0)`
- `13:25:26.667`：`EVT WINDOW-ROOT InitializePotentialDrag pos=(2231.04,1825.10)
  press=(2231.04,1825.10) pressObject=DebugTab4 dragObject=M09DebugModeRoot`
- `13:25:26.669`：`SELECTION DebugTab4`
- `13:25:26.689`：`/Mouse/leftButton=0`
- 整段没有 `BeginDrag` / `Drag` / `EndDrag`，也没有窗口位置变化行

### 逐层判定

| 层 | 观测 | 判定 |
|---|---|---|
| Computer Use 合成输入 → Input System | Input System 的 `onEvent` 记录到 `Mouse` 事件，位置、delta、`leftButton` 变化都在（焦点点击与拖动两段都有） | 有（仅到此层） |
| Input System → `InputSystemUIInputModule` → EventSystem 射线与派发 | 焦点点击命中 `DebugHeader` 并派发 PointerDown/PointerUp/PointerClick；拖动按下命中 `DebugTab4`，`InitializePotentialDrag` 派发到 `M09DebugModeRoot` | 有 |
| uGUI 拖动阈值 → `BeginDrag` | `pressPosition` 与 `position` 完全相等，按住期间位置不再变化，10px 阈值不满足 | **首个缺失** |
| 标题栏 `BeginDrag` / `Drag` / `EndDrag` | 无 | 未到达 |
| 窗口位移 | 无 | 未发生 |

### 机制

`PointerInputModule` 用 `pressPosition` 与当前指针位置比较判断拖动阈值。合成拖动把按下与位移
压进同一帧，模块处理该帧时指针已在终点，`pressPosition` 直接等于终点；之后按住期间指针不再
移动，阈值永远不满足。松手按“点击”处理，落点是 `DebugTab4`，所以内容页切到「事件」而窗口不动，
与主控观察一致。上一轮虚拟鼠标用例能拖动，是因为状态序列分帧推进，`BeginDrag` 时 `press` 与
`pos` 相差一个台阶（`press=(1020,2001)`、`pos=(1100,1971)`）。

### 证据文件

| 文件 | 行数 | SHA-256 |
|---|---|---|
| `Logs/verify/m09-input-trace/trace-20261123-132122.log`（本轮 Computer Use 全程） | 313 | `2846bf02668ba0170ed0cf6c2eb23b2d0aa52706724927962ac94083a473ef81` |
| `Logs/verify/m09-input-trace/trace-20261123-131926.log`（仪器自检） | 27 | `6493bf176751a38154e02740946a2549e60767f8d8a786eeb311469f1dbfb326` |
| `Logs/verify/m09-input-trace/trace-20261123-131530.log`（首次安装失败） | 2 | `85a817915cb24f28732f9d9327dd3be338d9913fffa4e497d04aab5b65788011` |

日志位于被 `.gitignore` 排除的 `Logs/verify/`，只在本机保留，未提交；本轮没有留下任何截图。

### 边界

本次观测只反映 Computer Use 合成输入把按下与位移合并到同一帧。它不能判断产品在真实输入下的运行
表现，也不能判断实体鼠标是否正常；它既不能说明实体鼠标会失败，也不能算作通过。实体鼠标拖动、
松开后窗口停留、底层控件未误触仍需用户在有实体鼠标的环境里验收。主控在 `13:25:17` 的焦点点击
与 `13:25:26` 的拖动都发生在 `focusedWindow=Game`、`appFocus=True`、`devicesEnabled=2/2` 的
时段，Editor 侧记录排除了 Game 视图焦点门控这一原因。来源的边界与开头的结论相同：日志从 Input
System 的 `onEvent` 起记，不能证明 Windows 原始输入链。

## 下一步建议

1. 合入本分支的测试与报告，锁定坐标、命中与按钮冒泡行为；不需要场景或预制体改动。
2. 用户按 PR #6 的四步清单验收实体鼠标拖动；本轮不把它标为通过。
3. 合成输入这一段已经解释：Computer Use 产生的 `Mouse` 事件进入 Input System，EventSystem 也完成
   了派发，断在“按下与位移同帧”导致拖动阈值不满足。要再用合成输入复现拖动，需要把按下与移动拆到
   不同帧（分步移动并插入等待），而不是再改坐标代码。日志止于 Input System，要核查 Windows 原始
   输入链需要另用 OS 层抓取手段。
4. 现在不要为“坐标空间”加防御性转换。只有将来把该画布改成 Screen Space - Camera 时，
   `rect.position` 直写才会失效，届时应改用
   `RectTransformUtility.ScreenPointToWorldPointInRectangle`。

## 未改动范围

未改 `DebugModePanel.cs` 或任何产品代码；未改场景、预制体、`ProjectSettings`、`AGENTS.md`
或 `.vscode/settings.json`。Computer Use 追踪用的临时诊断器
`Assets/Scripts/Editor/M09InputTrace/` 及其 `.meta` 已在停止 Play Mode、确认场景
`isDirty=false` 之后整目录删除，没有留在提交里。Unity 打开工程产生的差异
（`.vscode/settings.json` 修改、`Assets/Resources/PerformanceTestRun*.json` 及其 `.meta`
删除、`ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json` 修改）保留在
工作树但未提交。
