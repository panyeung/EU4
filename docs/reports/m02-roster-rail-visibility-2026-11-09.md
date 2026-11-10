# M02 人物名册侧栏遮挡修复

状态：本修复的编译、EditMode、Validator、独立只读复审与 lead Play Mode 通过（2026-11-09）；M02 最终用户里程碑验收仍待办。

## 原因与修正

- 实际 Play Mode 中，人物侧栏没有可见行文字，但点击相应位置能切换到信正。现有场景验证报告中，`M02DailyActionScroll` 位于先绘制的人物页内，`M02NavRail` 则是后绘制的共享兄弟节点；不拦截鼠标的实色导航背景盖住了名册。
- 复用 `ReferenceGalleryNavigator` 已有的运行时页面组合：把既有滚动区一次性移到导航栏内、放在名册标题下方；只在人物页激活它，其他页面仍保留导航栏。重复进入复用同一对象，不新建行、场景根或组件，不修改模拟状态。
- 修改仅为 `ReferenceGalleryNavigator.ConfigureRosterRail`、两个调用点，以及 M02 回归测试。开始时已有的导航输入修复、`AddM02DailyActionPanel` WIP 均保留，提交时只纳入本轮修正。

## 验证记录

- 使用当前 `main` 与既有 Unity `6000.6.6f1` Editor PID `126040`，项目路径 `C:\Users\panye\My project`；不建项目、branch/worktree，不运行 batchmode。
- 回归覆盖：绘制层级、标题下方与导航栏内的边界、离开人物页后隐藏、返回及重复选择不产生副本，以及实际行 Button 选择正确人物。
- 修正前：`42 passed / 1 failed`，`Logs/m02-roster-red.xml`，明确失败于滚动区被较晚导航栏遮挡的父子层级。
- 首次修正后，结构与点击通过，最后一个姓名断言因“正綱/正纲”字形差异失败；测试已改为读取当前名册的真实姓名。
- 聚焦 EditMode：`43 passed / 0 failed / 0 skipped`，`2026-11-10T06:29:47Z`–`06:30:32Z`，`Logs/m02-roster-focused.xml`；Console `0 errors / 1 warning / 11 logs`。
- 全量 EditMode：`1382 passed / 0 failed / 0 skipped`，`2026-11-10T06:31:22Z`–`06:32:50Z`，`Logs/m02-roster-full.xml`；Console `2 errors / 28 warnings / 20 logs`，两条错误为通过的婚姻/收养编年史故障注入预期日志。
- 独立 QATester 首轮只读复审 ACCEPT；未自行运行 Unity。修正后的真实姓名断言直接使用现有名册数据。
- 移除本轮 `M02RosterVerificationRunner.cs` 及其新建 `.meta` 后，`Logs/Editor.log:206616` 记录最终 `Tundra build success (1.76 seconds)`，无本次编译 C# 错误。既有 M02 harness 原样保留。
- 当前 Editor 的 `FoundationSceneValidator` 于 `2026-11-09 20:34` 记录 `RESULT: PASS; Errors: 0`、`Loaded: True; Dirty: False`。静态场景保留原层级；进入页面时执行已验证的运行时组合修正，没有保存或重建场景。
- Play Mode：同一 Editor 的 FoundationScene →「继续游戏」→ 人物页，名册各行文字出现；按姓名点击正纲后档案同步，重复点击当前人物页保留选择。最后一行的 10 岁幼年继承人可点选；切到地图后名册隐藏，返回人物页后列表恢复且保留幼年继承人选择，没有重复行。运行与退出后的 Console 均为 `0 logs / 0 warnings / 0 errors`。
- 已退出 Play Mode，未推进日期、保存游戏或保存场景；active 存档与 `.bak` 的 SHA256 都与本轮前相同。每张临时截图在检查后删除，结束时专用缓存 PNG 数量为 0。
- 验证使用保留既有 WIP 的当前 Editor 工作区；没有另建清洁 checkout 复跑全量。当前七人名册的末尾成员可点选；滚轮/拖拽未产生可明确辨认的位置变化，因此不宣称超长名册滚动已经实测通过。

## 状态边界

本记录只覆盖人物名册侧栏修复，不代表世界地图新设计或完整 M02 的用户最终里程碑验收。
