# M09 Debug 面板拖动：诊断与回归测试

日期：2026-11-23（UTC）
基线：`docs/orca-project-workflow@47c267c`
分支 / 工作树：`panyeung/m09-debug-drag-fix`（`m09-debug-drag-fix` 隔离工作树）
Unity：`6000.6.6f1`

## 结论

拖动链路在坐标、射线与事件路径三处都没有缺陷，本轮**不改产品代码**。三个待证假设中，坐标空间与标题栏射线截获均被测试与运行时证据否证；剩下唯一候选是“合成输入没有派发 EventSystem 事件”，本轮没有抓到 OS 到 Input System 的事件流，因此它仍只是假设。实体鼠标拖动继续标为未完成。

## 复核对象

- `Assets/Scripts/Presentation/DebugModePanel.cs`：`OnBeginDrag` / `OnDrag`（1145–1161 行）用屏幕像素位移直接写入窗口根的世界坐标；`CreateHeader`（1289–1308 行）用 `EventTrigger` 注册 `BeginDrag` / `Drag`；面板自身也实现 `IBeginDragHandler` / `IDragHandler`，作为按钮按下的冒泡兜底。
- `Assets/Scenes/FoundationScene.unity`：`Canvas` 为 Screen Space - Overlay（`m_RenderMode: 0`），`CanvasScaler` 参考分辨率 1280x720、`m_MatchWidthOrHeight: 0.5`。
- `ProjectSettings`：`activeInputHandler: 1`（仅新 Input System）；场景 EventSystem 使用 `InputSystemUIInputModule`。

## 三个假设的结论

| 假设 | 结论 | 依据 |
|---|---|---|
| 合成输入没有派发 EventSystem 事件 | 未证实，仍是唯一候选 | 真实 `InputSystemUIInputModule` 收到指针事件时窗口按 1:1 移动；本轮没有采集 Computer Use 合成输入在 OS/Input System 交界的事件流 |
| 屏幕像素与 RectTransform 坐标空间不一致 | 不成立 | EditMode 在画布缩放 1.5 下断言屏幕位移与指针位移逐像素相等（150,-60 → 100,-40 参考单位）；Play Mode 在 `scaleFactor=3` 的真实画布下同样逐像素相等 |
| 标题栏射线被其他 UI 截获 | 不成立 | Play Mode 渲染后 `EventSystem.RaycastAll` 首位命中 `DebugHeader`（其后才是窗口根、顶栏、页面底图、日常行动面板）；EditMode 在三个标题栏采样点断言绘制顺序最上层是标题栏 |

Overlay 画布下画布 1 个世界单位等于 1 个屏幕像素（`scaleFactor=3` 时画布矩形仍是 1280x720、局部缩放为 3），所以 `rect.position = eventData.position + offset` 的写法本身正确，不需要额外换算。

## 新增回归测试

`Assets/Tests/EditMode/M09DebugPanelDragTests.cs`（3 个用例，走 EventSystem 的生产派发链路，不直接调用回调）：

1. `Header_IsTheTopmostPointerTargetAcrossTheTitleBar`：标题栏三个采样点的最上层图形是 `DebugHeader`，`ExecuteEvents.GetEventHandler<IDragHandler>` 解析到标题栏，`EventTrigger` 恰好注册 BeginDrag 与 Drag，序列化的 `dragHandle` 就是标题栏。
2. `Drag_MovesTheWindowOneScreenPixelPerPointerPixel_AtScaleOnePointFive`：画布缩放 1.5 时指针移动 150x-60 屏幕像素，窗口屏幕位移相同、参考单位位移为 100x-40，抓取点始终留在指针下方。
3. `Drag_PressesOnATitleBarControlAndStillMovesTheWindow`：按在折叠按钮上拖动，事件冒泡到窗口根的 `OnDrag`，窗口照常移动。

EditMode 画布不渲染，`Graphic.depth` 恒为 -1，`GraphicRaycaster` 会跳过所有图形；测试因此按画布绘制顺序取最上层（并注释说明），渲染后的真实射线由本轮 Play Mode 证据覆盖。

## 证据

| 项 | 值 |
|---|---|
| 命令 | `unity command run_tests --mode EditMode --async_tests true` → `test_status` |
| 结果 | `2301/2301` 通过，失败 0，`duration=64.1191926s`（`2026-11-23 22:22:47Z`–`22:23:51Z`） |
| 副本 | `Logs/verify/20261123-m09-drag-full/TestResults.xml` |
| SHA-256 | `6620f9efdc14e95e4958decf2fd6209f60a9c18522c3f4bdd96a58aa81b40253` |
| Console | 测试前 error 0；跑完 error 2，两条都是故障注入用例经 `GameSession.ReportChronicleAppendFailure` 主动打印，非新缺陷 |
| Play Mode | `FoundationScene` 停止后 `isDirty=false`、`playMode=stopped`、`compiling=false` |

运行时细节与逐项日志见 `Logs/verify/20261123-m09-drag/evidence.md`（该目录被 `.gitignore` 排除，只作本机证据）。

## PR3 集成建议

1. 先把测试文件合入集成分支，锁定坐标与命中行为；它不需要场景或预制体改动。
2. M09 的“实体鼠标拖动标题栏、松开后停留、底层控件未误触”仍按 PR #3 现有步骤由用户验收，本轮不把它标为通过。
3. 若要闭合“合成输入为何不派发事件”，下一步应采集 `InputSystemUIInputModule` 的 pointer 事件流（PlayMode 测试骨架或 Input System 事件日志），而不是再改坐标代码；本轮没有新增 PlayMode 测试骨架，因为项目 `Assets/Tests/PlayMode` 仍为空。
4. 现在不要为“坐标空间”加防御性转换。只有将来把该画布改成 Screen Space - Camera 时，`rect.position` 直写才会失效，届时应改用 `RectTransformUtility.ScreenPointToWorldPointInRectangle`；当前没有证据支持提前改动。

## 未改动范围

未改 `DebugModePanel.cs` 或任何产品代码；未改场景、预制体、`ProjectSettings`、`AGENTS.md`。打开 Editor 产生的 Unity 侧写已全部还原，提交只含新增测试与其 `.meta`，以及本报告。
