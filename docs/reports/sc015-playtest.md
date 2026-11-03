# SC-015 Play Mode 测试记录

- Unity：6.6 (`6000.6.6f1`)
- 项目：`C:\Users\panye\My project`
- 场景：`FoundationScene`
- 分支：`feature/sc013-fixed-three-generation`
- 执行方式：现有 Unity Editor；用户明确临时授权本次 Computer Use 操作。
- 运行范围：标题页、新游戏、M2 固定场景、家族名册、SC-013/SC-011 前置事件、SC-015 四条分支、家族编年史。

## 结果

> 下表记录最终逐步复测结果。路线 D 首次观察未能复现，后续 UI 复测通过；具体复核见文末「源码复核」。

| 路线 | 实际选择 | 观察结果 | 状态 |
|---|---|---|---|
| A | 第一代公开；第二代保留原貌；第三代接收 | 依序显示「已公开的家书」「完整记名的家书」；编年史显示三代家书事件记录。 | 通过 |
| B | 第一代公开；第二代整理简史；第三代接收 | 显示「简要传述的家书」；编年史说明部分名字未被传下。 | 通过 |
| C | 第一代封存；第二代由正纲拆阅；第三代接收 | 显示「正纲拆阅后的家书」；编年史说明完整记录传到第三代。 | 通过 |
| D | 第一代封存；第二代选择继续封存 | 复测时按步骤完成第一代暂封、第二代继续封存与交接；第三代出现「仍在封存的家书」，选项摘要确认家书仍未被阅读，编年史记录了第一代「尚未拆封的家书」与第三代封存事件。 | 通过 |

标题页进入 Play Mode 成功。新游戏显示 1560-04-01、晴信 70 岁、正纲 46 岁。家族名册显示六人及固定继承人。SC-013 家议、SC-011 雨夜来客与两代旧客回访在已走通路线中按预期出现。路线 A/B/C 的 M2 结局面板关闭后可继续推进月份。

路线 D 曾在第一次操作中未观察到预期节点；该次操作没有逐步核对事件顺序，结果只能记为未复现，不能据此判定代码缺陷。之后按 `SC015_DEMO.md` 第 6 节逐步重跑并在每次交接前核对事件：第二代出现「尚未拆封的家书」，选择继续封存后摘要显示正纲未拆匣并交给信正；第三代出现「仍在封存的家书」，编年史记录两代相关节点。路线 D 复测通过。

## 验证边界

- Play Mode：已运行，路线 A/B/C/D 均到达各自预期的第三代内容；路线 D 的首次观察为未复现，按逐步流程复测通过。
- 源码级 harness：OpenCode2 使用 Unity 附带 .NET SDK 和 NUnit，在项目外临时编译并执行真实 SC-015 目录/事件引擎路线用例，6/6 通过；临时 harness 已删除。这不是 Unity 编译或 Unity EditMode Test Runner 结果。
- Console：停止 Play Mode 后显示 0 errors、0 warnings、0 logs；该计数是退出时观察值。
- 编译：未独立读取本轮编译结果或 Editor.log 编译摘要，不单独声明编译门通过。
- EditMode：未运行。
- `FoundationSceneValidator`：未运行。
- 存档：未执行保存或读取，避免触碰既有单槽存档。
- 场景/资源：未编辑或保存场景、预制体、`ProjectSettings` 或资源。
- Git：未暂存、提交、推送、合并或打标签。
- 验收：四条路线的本次 Play Mode 执行结果通过；用户验收仍待用户确认，未创建 checkpoint、未合并或推送。

## 源码复核（2026-11-03 追加）

本次只做只读源码与数据追踪，未新增或修改任何游戏代码、事件/本地化数据、场景、预制体或测试文件，也未启动 Unity。

### 1. 目录数据与运行时代码一致

- 选择记录：`EventEngine.TryChooseEventOption` 成功后由 `EventRuntimeState.TryCommit` 把 `eventId/optionId` 追加到只增历史，同时按 `onceScope: PerGame` 写入一次性标记（`Assets/Scripts/Events/EventRuntimeState.cs` 115–152）。
- 第二代选择：`EventEngine.HasLastChosenOption` 从历史末尾反向扫描，取该 `sourceEventId` 的**最后**一条记录比较 `optionId`（`Assets/Scripts/Events/EventEngine.cs` 361–397）。公开/封存互斥来自这次比较，所以第一代封存只会放开 `second-sealed`，第一代公开只会放开 `second-open`。
- 世代门槛：`GameSession.BuildEventRuleContext` 仅在 `SuccessionPhase.None` 时把 `SettledSuccessionCount` 取为 `SuccessionCount`，否则为 0（`Assets/Scripts/Application/GameSession.cs` 992–1005）；`AreConditionsMet` 只在**排程**时比较该阈值，提交时跳过（`EventEngine.cs` 365–372）。因此第二个节点要等第一次交接结算后才可用，第三个节点要等第二次交接结算后才可用。
- 排程：候选按 priority 降序，只在一个最高优先级组内按权重抽取（`EventEngine.cs` 141–170）。SC-015 第一代 6、第二代 60、第三代 600，因此第一代依次排在 SC-013 必须事件（1000）与 SC-011 雨夜来客（10）之后、SC-014（5）之前；第二代排在 SC-011 旧客回访（100）之后；第三代排在终局（1000）之后。

### 2. 与两次运行记录的对应关系

- 上述逻辑要求的节奏是：第一次交接后**下一个**月份出现「尚未拆封的家书」；选择继续封存、第二次交接并完成终局后**下一个**月份出现「仍在封存的家书」；每次提交时 `GameSession.RecordEventChoice` 各写入一条 SC-015 编年史记录。
- 该节奏与第二次逐步 UI 复测完全一致，因此不支持「代码缺陷」的结论。
- 首次记录与该节奏不一致：它同时报告「第二代预期节点未出现」与「编年史完全没有 SC-015 条目」。若第一代封存选择确实提交，这两点无法同时成立（提交必然写历史与编年史）；因此首次记录只能判定为不可靠的观察，而不是缺陷证据。

### 3. 剩余验证要求

1. Unity 编译、Unity EditMode Test Runner、`FoundationSceneValidator`、存读档仍均未运行；本次不提供这些证据。
2. 用户复核本报告中的 Play Mode 结果并明确验收；验收完成前，SC-015 不得创建 checkpoint、合并 `main` 或打标签。
