# M04A 个人行动记忆实现与验证记录

状态：M04A lead 技术签收通过（2026-11-09）；独立复审两轮 ACCEPT，修正后编译、EditMode、Validator、Play Mode 与存读档验证通过。M04 整体与用户最终里程碑验收尚未完成。

依据：[设计稿](../superpowers/specs/2026-11-07-m04a-personal-memories-design.md)与[实施计划](../superpowers/plans/2026-11-09-m04a-personal-memories.md)。

## 执行记录

- 2026-11-09：在当前 `main`、`C:\Users\panye\My project` 接续既有未提交 M04A 实现。OpenCode2 任务 `task_aff3fb0433ba` 的执行 `ctx_8b6637b7a0c4` 中断；协调者按用户的中断接手授权执行 guarded stop，Orca 确认 `state=stopped`、`processAction=closed_agent_terminal` 后接手。
- 复用既有记忆、评分、会话事务、v12 存档和 Inspector 实现及测试；没有重新生成工程、分支、工作树或场景。既有 M02 生成器、导航与其他无关 WIP 不在提交范围。
- 本轮临时验证工具只通过当前 Unity Editor 的 `TestRunnerApi` 运行 EditMode；不运行 batchmode。工具将在最终验证后移除。
- 既有实现的测试先后顺序没有完整证据，因此不补称其为本轮 red/green。新增修复单独记录回归测试的失败与通过结果。
- 接手修复：v12 记忆缺少 `kind`、`action` 或 `reason` 时原先会被有效默认值接纳。三例真实 JSON 回归测试先得到 `147 passed / 3 failed`（`Logs/m04a-missing-fields-red.xml`）；将两个枚举 DTO 字段初始化为非法值并拒绝缺失原因字段后，聚焦 `150/150` 通过。已有有效数据保持兼容，没有新增 JSON 解析器或依赖。

## 验证记录

- Editor：Unity `6000.6.6f1`，PID `126040`，命令行项目路径为 `C:\Users\panye\My project`。
- 初始聚焦 EditMode：`147 passed / 0 failed / 0 skipped / 0 inconclusive`；结果 `Logs/m04a-focused.xml`，运行时间 `2026-11-10T05:42:17Z`–`05:42:20Z`。运行后 Console 为 `0 errors / 2 warnings / 11 logs`。
- 初始全量 EditMode：`1376 passed / 0 failed / 0 skipped / 0 inconclusive`，结果 `Logs/m04a-baseline-full.xml`；这是修复缺字段回归之前的基线。
- 修复后聚焦：`150 passed / 0 failed / 0 skipped / 0 inconclusive`，`2026-11-10T05:46:13Z`–`05:46:15Z`，结果 `Logs/m04a-focused.xml`。
- 修复后全量：`1379 passed / 0 failed / 0 skipped / 0 inconclusive`，`2026-11-10T05:46:54Z`–`05:48:13Z`，结果 `Logs/m04a-full.xml`。Console 为 `2 errors / 28 warnings / 20 logs`；两条 Error 是 `FamilyFormationSessionTests` 已用 `LogAssert.Expect` 声明的故障注入日志，对应用例均通过，不能记作零错误。
- 最终编译：移除本轮 `M04AVerificationRunner.cs` 和其新建 `.meta` 后，现有 Editor 的 `Logs/Editor.log:194144` 记录 `Tundra build success (1.73 seconds)`；该编译无 `error CS`。原有 `M02TestHarness.cs` 原样保留。
- 场景 Validator：`2026-11-09 19:49` 在现有 Editor 执行既有菜单；`Logs/SceneValidationReport.txt` 为 `RESULT: PASS; Errors: 0`，包含 M02 与 M03 Inspector 校验 PASS；场景 `Loaded: True; Dirty: False`，没有保存场景。
- 基础 checkpoint `15f5330` 已推送到 `origin/main`；独立 OpenCode 启动器未配置，改由独立 QATester 对固定提交只读复审，第一轮 ACCEPT。复审未自行运行 Unity；上述执行证据来自协调者。

## Play Mode 与修正记录

- 路径：同一 Editor 的 `FoundationScene` → 开始页「继续游戏」→ 人物页「打开完整调试面板」。v11 现有存档载入后两名试点记忆为空；推进到 1560-05-03，正纲完成工作并出现归属于本人的完成记忆与 `+12`。在人物页实际中断信正的休息行动，GUI 存档生成 v12，数据中保存信正完成/中断各一条、原始原因与日期；正纲记录未串写。
- GUI 存读档：保存 1560-05-03 状态，推进到 05-04，再用「读档」恢复，地图日期回到 05-03。原始 v11 存档与原始 `.bak` 在测试前已分别备份，首轮退出 Play Mode 后均按 SHA256 原样恢复：active `1E61ED9CAB4A5F3E5FBCCE5EDA4BD24586BE5A072EA5B749CCB3C1E280BCB92C`；bak `F1C0630E5DD91BAA0B18607CA4367956DB637A8BB4DD6DA21D1E79212BDA4CBE`。
- 边界：使用从原存档派生的临时 v12 测试存档，正纲保留 1560-04-01 的完成训练和中断工作记忆；两名试点安排长休息避免替换旧记录。Inspector 于 1560-05-01（D+30）显示 `+12/-12`；推进到 05-02（D+31）后两条仍存在，当前修正均为 0 并标为过期。该测试存档是验证夹具，不宣称是自然游玩产生的历史。
- 首轮运行 Console 为 `0 logs / 0 warnings / 0 errors`，但发现两个可见问题，因此首轮不签收：固定 30 像素状态区裁切第二条记忆与原因；读档后地图日期已回退，共享顶部栏仍显示较晚日期。
- 两例回归先得到 `179 passed / 2 failed`（`Logs/m04a-ui-red.xml`）。修正把记忆移入现有可滚动候选文本，并在人物页关闭时只刷新共享顶部栏；未改场景和导航 WIP。独立 QATester 第二轮对这五个修正文件只读复审 ACCEPT。
- 另观察到既有 M02 左侧人物列表文字未显示但对应位置可点击选人；完整 Inspector 的两名试点按钮可正常使用。这是独立 UI 问题，随后已由[名册侧栏修复](m02-roster-rail-visibility-2026-11-09.md)验证解决；没有以 M04A 数据门声称整个 M02 UI 已完成。

## 最终修正验证与签收

- 聚焦 EditMode：`181 passed / 0 failed / 0 skipped`，含 M02 人物页与 M04A 相关测试；`2026-11-10T06:07:46Z`–`06:08:30Z`，`Logs/m04a-ui-focused.xml`。Console：`0 errors / 4 warnings / 11 logs`。
- 全量 EditMode：`1381 passed / 0 failed / 0 skipped`，`2026-11-10T06:09:30Z`–`06:10:52Z`，`Logs/m04a-ui-full.xml`。Console：`2 errors / 29 warnings / 20 logs`；两条错误仍是已声明并通过的婚姻/收养编年史故障注入。
- 临时 runner 及其新建 `.meta` 再次移除后，`Logs/Editor.log:200082` 为最终 `Tundra build success (2.62 seconds)`，无本次编译 C# 错误。现有 M02 harness、生成器与导航 WIP 保持原样。
- `2026-11-09 20:11` 再跑当前场景 Validator：`RESULT: PASS; Errors: 0`，`Loaded: True; Dirty: False`，没有保存场景。
- 修正后 Play Mode：同一 Editor、FoundationScene、同一临时边界夹具。D+30 时完成训练 `+12`、中断工作 `-12`，两条的日期与中断原因都在现有滚动区域可见；GUI 保存 → 推进一天 → 读档后，地图与共享顶部栏都恢复到 1560-05-01。再次推进到 05-02（D+31），两条仍可见且均显示 `+0（已过期，仅展示）`。
- 修正后运行及退出后 Console 都为 `0 logs / 0 warnings / 0 errors`。已退出 Play Mode；active slot 与 `.bak` 第二次恢复并再次核对上述原始 SHA256。每张 Computer Use 截图在检查后删除，结束时专用缓存 PNG 数量为 0。
- 签收范围为本切片的实现与技术验证，不把 M04 动态家族、M02 侧栏显示问题或用户最终里程碑验收标为完成。当前会话没有可用的 WhatsApp 收件人/发送路由，结果通过 Orca 实现任务与依赖签收任务交接。

## 当前边界

本切片仅涉及正纲与信正的最近完成/中断各一条行动记忆、30 日内同类行动 `+12/-12`、v11→v12 空记忆迁移及只读诊断。动态家族成员和通用记忆编辑不在范围。
