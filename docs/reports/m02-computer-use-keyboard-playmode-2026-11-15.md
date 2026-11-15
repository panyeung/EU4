# M02 Computer Use 键盘输入 Play Mode 记录

日期：2026-11-15
Editor：Unity 6000.6.6f1，PID 138220，`C:\Users\panye\My project`

## 结果

在新的 SC-013 游戏中，`VerifyUi.Text` 从 `Canvas/M02DailyActionRoot/M02DailyActionPanel/M02ActionPanel/M02DailyActionDurationInput` 读回行动天数初值 `1`。Computer Use 点击输入框后发送 `Ctrl+A`、字符 `2` 和 Enter。`VerifyUi.Text` 随后读回 `2`。

随后选择休息行动。日程显示“第 1–2 天，休息（2 天）”和“开始于 1560-04-01，进行中”。

## 测试后状态

- 测试使用 `1560-04-01` 的 SC-013 新局。
- 未使用默认存档槽，未存档或读档。
- 未保存 `FoundationScene`。
- 执行 `editor_stop` 后 Play Mode 已退出。Editor 为 `ready/stopped`，`compiling=false`，`compilationFailed=false`。
- `FoundationScene` 仍为活动场景，已加载且 `dirty=false`。
- 最终 Console 采样为 0 个错误、0 条警告、0 条日志。
- Computer Use 仅在工具响应中显示一张临时画面来定位输入框，未写入截图文件。

本次验证了 Unity 输入框对 Computer Use 合成键盘事件的响应，以及日程采用了输入的天数。没有单独测试实体键盘硬件。也没有重测行动完成、关闭并重开面板，或存档读档。
