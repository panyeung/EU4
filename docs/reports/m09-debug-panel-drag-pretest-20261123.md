# M09 Debug 面板拖动预验收记录

日期：2026-11-23（UTC）

## 结果

Debug 面板在 Play Mode 中打开，显示日期 `1560-04-01`。我用 Computer Use 的合成鼠标在标题栏尝试拖动两次，面板都没有移动。关闭按钮可用。

这次结果不能证明实体鼠标是否能拖动面板。M09 仍未通过，实体鼠标拖动、松开后的停留位置以及底层控件是否误触仍待用户验收。

## 本轮环境

- Unity `6000.6.6f1`，Editor PID `68216`。
- 项目路径：`C:\Users\panye\orca\workspaces\My project\m08-100-year-review`。
- 分支：`panyeung/m08-100-year-review`，测试基线 `9aba12ad994da8371bcfbd737c5ef85075a5b21b`。
- Play Mode 前：Editor `ready`、`compiling=false`、`playMode=stopped`；`FoundationScene` 已加载且 `isDirty=false`。
- Console 基线：错误 0、警告 0。

## 操作与观察

1. 通过 Unity Pipeline 的 `editor_play` 进入 Play Mode。标题页正常显示。
2. 用 Computer Use 点击「Debug 综合测试」。面板显示标题「DEBUG / 不会保存至玩家存档」，日期为 `1560-04-01`。
3. 在标题栏尝试两次合成拖动：窗口坐标 `(790, 169) → (900, 240)` 与 `(1050, 169) → (1130, 239)`。两次操作后，面板左上角位置都未变化。
4. 点击「关闭」。面板隐藏，底层游戏画面仍可见。
5. 通过 Unity Pipeline 的 `editor_stop` 停止 Play Mode。

收尾时 Editor 为 `ready`、`compiling=false`、`playMode=stopped`。`FoundationScene` 仍为 `isDirty=false`，Console 错误与警告仍为 0。本轮没有保存场景或游戏存档，也没有运行测试。

Computer Use 截图只用于即时目视检查，没有保存到工程、备份或报告目录。会话结束时已重置 Computer Use 临时会话。

## 源码边界

`Assets/Scripts/Presentation/DebugModePanel.cs` 的 `OnBeginDrag`、`OnDrag` 和 `CreateHeader` 注册了 Unity UI 拖动回调。源码接线不能代替输入事件的运行时结果；本轮合成拖动未观察到位移，也不能据此判断实体鼠标结果。
