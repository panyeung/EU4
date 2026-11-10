# 《影之一族》项目状态

更新时间：2026-11-09
设计权威：[GDD_v4.0.md](GDD_v4.0.md)。本文件是当前执行状态入口；当前开发节点采用整合后的 `M00`–`M08` 路线；旧《Sengoku Chronicle》M1–M7 文档仅作为历史记录，不代表新方向进度。

## 当前方向

- 游戏：以家族兴衰、家主角色扮演与世代传承为核心的战国人生模拟。
- 工程：复用唯一项目 `C:\Users\panye\My project`，Unity `6000.6.6f1`，不另建项目。
- 规则与范围以 GDD v4.0 设计和 `M00`–`M08` 执行路线为准；SC-001–SC-024 是纵向功能切片编号，不替代里程碑节点，也不从旧 M1–M7 代号推断新方向进度。

## 当前开发路线（2026-11-07）

用户提供的 `Shadow-Clan-Game-Design-and-Unity-Roadmap-v5.md` 已并入 GDD 第十三章：三代固定剧本收敛后，再推进每日行动、NPC 自主性与只读 Inspector、人物记忆与动态家族、经营/委托、潜入战术、系统整合和内容扩展。v5 中未确认的产品规则仍须在相关节点开始前单独确认。

| 节点 | 方向 | 当前状态 |
|---|---|---|
| M00 | 现有工程基线与验收收敛 | 进行中：已核对 main/远端、工作树、Editor 路径和最新 EditMode；仍有一个带脏改动的 detached SC-021 工作树、启动场景与演示场景不一致，且当前场景尚无本次提交后的 Validator/Play Mode 证据 |
| M01 | 固定三代家族可玩原型 | **已验收（2026-11-07，用户明确确认）**；当前 `main` 含 SC-024 场景接入。技术验证细节见下方历史报告，不从用户验收推断额外测试结果 |
| M02 | 每日行动与旅行基础 | 当前 main 技术门已通过（2026-11-09）：全量 EditMode 1374/1374、FoundationSceneValidator PASS、当前 FoundationScene Play Mode 验证了持续天数关闭重开、两日休息推进与清洲→热田旅行；本轮未重测存读档，用户最终里程碑验收待办。证据见 [M02 当前 main 验收追加记录](docs/reports/m02-daily-action-foundation.md#2026-11-09-current-main-verification-addendum) |
| M03 | 高自主 NPC 与只读 Inspector | 实现与两轮复审修正完成，待 lead Play Mode 与用户验收：纯 C# 规则、v11 存档/迁移、两名试点、逐日自主推进、只读可滚动 Inspector 与事务式场景接入；第二轮修正后历史 EditMode 1274/1274、Validator PASS；2026-11-08 全量 EditMode 1303/1303，但未取得该提交后的 M03 Validator/Play Mode 证据 |
| M04 | 人物记忆与动态家族 | 计划中 |
| M05 | 经营与委托纵向切片 | 计划中 |
| M06 | 回合制潜入原型 | 计划中 |
| M07 | 经营、个人活动与战术整合 | 计划中 |
| M08 | 内容扩展与正式体验 | 计划中 |

M01 已于 2026-11-07 由用户明确验收通过。当前 `main` 的 `Assets/Scenes/FoundationScene.unity` 已包含 SC-024 场景接入（`SC024`/`FamilyDoctrine` 匹配 18 处）；最新 HUD 修复提交为 `18c7b4c`。用户验收记录不替代也不扩写技术证据：SC-024 历史编译、EditMode、Validator 与 Play Mode 结果及各自限制见 [SC-024 实现报告](docs/reports/sc024-implementation.md)；M7 HUD 修复的检查结果单独记录于提交与任务记录。下方标注为 2026-11-06 的 SC-024、M02 内容保留为当时快照，不代表当前场景或验收状态。

## M00 基线核验（2026-11-08）

- Git：当前 checkout 为 `main`，HEAD 与 `origin/main` 同为 `e298312c5100de9f715e7d74c6ad8e0930e02a65`（`Integrate reference gallery and daily actions`）；本地/远端分支列表只有 `main`。主工作树无已跟踪文件修改，尚有旧失败报告 `Assets/Scenes/TestResults_20261108_072929.xml` 及其 `.meta`、`graphify-out/` 和 `nul` 未跟踪；均保留且未纳入已推送提交。
- 工作树：Git 与 Orca 均登记一个额外的 SC-021 场景集成工作树 `C:\Users\panye\orca\workspaces\My project\sc021-scene-integration`，HEAD `66373ee`，处于 detached 状态。该工作树的 `.vscode/settings.json` 与 `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json` 有未提交修改；本轮未触碰或清理。它是 M00 尚未收敛的开放 WIP，不等于 Git 分支。
- Unity：当前 Editor PID `126040`，命令行 `-projectPath "C:\Users\panye\My project"`，版本 `6000.6.6f1`。`ProjectSettings/EditorBuildSettings.asset` 当前启用的是 `Assets/Scenes/SampleScene.unity`；主要可玩与集成场景 `Assets/Scenes/FoundationScene.unity` 未列入启动场景。需先明确演示/启动入口并验证相应场景，不能把 FoundationScene 的历史结果当作当前启动路径证据。
- 最新自动化证据：当前提交附带 `Assets/Scenes/TestResults_20261108_114927.xml`，结果 `1303 passed / 0 failed / 0 inconclusive / 0 skipped`（2026-11-08 21:48:15Z–21:48:31Z）；此前报告中两个失败用例在此结果中均为 Passed。`Logs/Editor.log` 最新记录 `*** Tundra build success (0.71 seconds), 1 items updated, 1975 evaluated`，该日志内未检出 `error CS`。这是编译与 EditMode 证据，不代表场景 Validator 或 Play Mode。
- Console：本次 EditMode 后 Editor Console 为 `17 logs / 25 warnings / 2 errors`；两条 error 是 `FamilyFormationSessionTests` 故障注入路径预期产生的 `LogError`，对应测试通过。这里不记为零错误。当前没有本次提交后的 Validator 结果或 M03 Play Mode 结果。
- 验收状态：M01 仍为用户已接受；M02 的 2026-11-06 Play Mode/Validator 记录早于 `e298312` 对每日行动 UI 和 `FoundationScene` 的后续修改，因此只作为历史证据，当前 main 需重跑对应场景验证与 Play Mode；M02 最终用户验收待办。M03 的历史全量 EditMode 与 Validator 结果不代替当前提交后的场景/Play Mode 验证；用户验收待办。
- M00 剩余项：保护并处理 SC-021 detached 工作树中的两个脏文件；确定 SampleScene 与 FoundationScene 的启动/演示关系；对当前 `main` 记录独立的启动场景、FoundationScene Validator、Play Mode、存读档和最终用户验收证据。未解决前 M00 保持进行中。

## 新方向检查点

| 检查点 | 状态 | 证据 / 边界 |
|---|---|---|
| SC-001 项目基线审计 | 已完成 | `docs/SC001_BASELINE_AUDIT.md`；此文档原始快照中的后续状态由本文件覆盖。 |
| SC-002 纯 C# 核心边界 | 已完成 | 在现有 Domain 基础上建立边界；不另建平行 Core。 |
| SC-003 人物、家族与关系模型 | 沿用现有模型；本次未纳入 SC-004–SC-009 验收 | 家族模型相关未提交文件仍是工作区内容，未合并进本 checkpoint；不得据此声称 SC-003 已完成新方向验收。 |
| SC-004 月度推进 | 已实现并随本 checkpoint 获用户试玩验收 | 月份推进与地图月末按钮接入。 |
| SC-005–SC-009 随机、版本化存档、事件数据校验、事件引擎与选择 UI | 已实现并随本 checkpoint 获用户试玩验收 | `demo-SC004-SC009`；用户确认 Edit Mode 全部通过及试玩验收。 |
| SC-010 初代家主退休与继承 | 已获用户验收 | 提交 `af35633`，标签 `demo-SC010`；用户试玩验收由协调方记录。 |
| SC-011 跨代事件链 | 用户已验收 | 验收日期 2026-11-02；用户报告编译通过、EditMode passed、Console 无错误、Play Test 通过。EditMode 数量与 Console 警告数未提供；FoundationSceneValidator 结果未报告。试玩脚本与验收记录见 `SC011_DEMO.md`。 |
| SC-012 家族编年史 | 用户已验收并推送（2026-11-03） | checkpoint `4443ccc`，标签 `demo-SC012`；`main` 与 `origin/main` 已同步，已删除合并后的 feature refs。用户报告 EditMode 通过、Play Test 通过；数量和 Console/Validator 计数未单独提供。详见 `SC012_DEMO.md`。 |
| SC-013 固定三代剧本 | 代码/内容/测试/文档已实现；运行证据来自 SC-018 协调方共享验证；专门验收待办 | 未运行 `FoundationSceneValidator`，用户验收仍未完成。静态类型检查 8/8 程序集 0 错误；逻辑 harness 30/30、33/33。SC-018 协调方授权验证在同一 Editor 取得 Unity 6.6.6f1 成功重编译、Editor EditMode 824/824 与 SC-013 Play Mode，其中完成 SC-013「继承前的家议」并到达第三代信任门槛结尾；这些是 SC-018 运行的共享证据，不是专门的 SC-013 全量验收，也不替代用户验收。详见 `SC013_DEMO.md`、`docs/reports/sc013-implementation.md` 与 `SC018_DEMO.md` 第 0.1 节。 |
| SC-014 流亡少年与家系遗产 | 数据/文档切片已实现并经两轮 OpenCode2 只读审阅 ACCEPT；用户 Play Test 与验收待办 | 4 个事件节点、22 条中文文本；一次性 JSON 结构/引用检查完成。未运行 Unity、测试、场景验证、存读档或 Play Mode。完整链是可选内容，需要每次交接前额外结束一个月。详见 `SC014_DEMO.md` 与 `docs/reports/sc014-review-2.md`。 |
| SC-015 三代家书 | 数据/文档切片已实现；两轮 OpenCode2 独立只读审阅均 ACCEPT；Play Mode 分支 A/B/C/D 均到达预期第三代内容；用户验收待确认 | 7 个事件节点、10 个选项、27 条中文文本；静态 JSON 检查通过。现有 Unity 6.6 Editor 中运行 Play Mode；Console 停止时 0 errors / 0 warnings / 0 logs。EditMode、场景验证、存读档与独立编译证据未取得。详见 `SC015_DEMO.md` 与 `docs/reports/sc015-playtest.md`。 |
| SC-016 委托旧约 | 数据/文档切片已实现；助手已按用户临时授权完成四条 Play Mode 路线，EditMode 全量通过；用户正式验收待办 | 7 个事件节点、10 个选项、27 条中文文本；静态 JSON 检查 10 组 0 错误。现有 Unity 6.6.6f1 Editor 的 EditMode 778/778 通过、0 失败/跳过；四条 Play Mode 路线均到达对应第三代结尾。测试后 Console 显示 0 errors、14 warnings、8 logs；未运行 `FoundationSceneValidator` 或存读档验证。没有保存场景或测试进度；用户验收待确认。详见 `SC016_DEMO.md` 与 `docs/reports/sc016-implementation.md`。 |
| SC-017 通用家谱展示 | 用户已验收并已推送（2026-11-03） | 只读、数据驱动、可滚动家谱；亲族/未连接亲族/家臣分区；typed 关系线；无六人/三代硬上限。最终修正后 Roslyn 8/8 程序集 0 错误，纯图/布局 harness 86/86；Codex fallback 顺序只读复审 ACCEPT，无 Critical/Important，留一条未覆盖空路由面板分支的轻微备注。用户接受本次演示；未提供 EditMode 数量、Console 计数或 Validator 报告文本。checkpoint `3ae2ade`（`feat(sc013-017): consolidate M2 family demo`）已推送至 `origin/main`，标签 `demo-SC017` 已推送。详见 `SC017_DEMO.md` 与 `docs/reports/sc017-implementation.md`。 |
| SC-018 独立家族事件 | 数据/运行时/测试/文档已实现；协调方授权 Unity 验证通过（编译、EditMode 824/824、Play Mode、v5 存读档）；用户验收待办 | 目标从 23/30 补至 30/30，维持 5/5 条跨代链；新增七个无 `chainId`、`Monthly`/`PerGame`/`scenario: "sc013"` 的独立家庭事件，14 个选项、35 条中文文本。家庭关系按 `(当前家主 PersonId, 家人 PersonId)` 键控；继承不复制旧家主数值，旧记录保留；存档升为 v5 并支持 v4 空集迁移；编年史显示双方姓名与精确前后值。目标均为现有非家主 `person.sc013.*` 成员；当前无养子成员，`adoption-welcome` 按设计授权改写为時姫的归属询问，不新增人物或收养关系。静态 JSON 检查 0 错误、八程序集 Roslyn 0 诊断、纯 C# harness 82/82。2026-11-03 协调方授权验证：EditMode 824 passed / 0 failed / 0 skipped（含 SC018 的 20 个用例，21:12:19Z–21:12:24Z）；Play Mode 七个事件各一次、两次交接、SC-011 回访、SC-013 信任门槛结尾、编年史前后值，v5 存档含四条关系与 30 条编年史并恢复 1561-07；用户存档已备份并原样还原。`FoundationSceneValidator` 未运行；用户 Play Test 与验收仍未完成。详见 `SC018_DEMO.md` 与 `docs/reports/sc018-implementation.md`。 |
| SC-019 家族产业首切片 | 代码/测试/存档/编年史/场景 UI 已实现；EditMode 923/923；协调方在用户本次临时授权下完成 Play Mode 主路线；用户正式验收待办 | 农庄 + 商馆、资金/物资/情报、当前家族成年成员任命、4 单位预算与 3 单位预留、替换/解除、确定性月结、空缺/短缺/失效、两次继承交接、v6 存档与 v5 迁移、产业编年史、uGUI 面板与事务式生成器。新局 30/12/0，晴信+千恵满员结束 1560-04 得 35/13/1；日推进/旅行跨月拒绝，结束月份唯一结算。2026-11-04 独立审查后修正：产业打开按钮移出 SC-010 的 430×210 继承 HUD 覆盖区，改为左上锚定 `(20,-250)`；`AddSC019IndustryPanel.Validate` 新增与 `SuccessionHud` 实际矩形的重叠检测（保留画布内含/编年史/名册检查）；月结回滚测试新增「故障发生在事件排程消耗随机流之后，且随机流与事件运行态均恢复到结算前」的断言。本 worker 在普通 Unity Editor Test Runner 取得 **923/923**（0 失败/跳过，`2026-11-04 04:15:29Z–04:15:35Z`，`Logs/sc019-final-correction-all.xml`），编译 `error CS` 计数 0；修正前基线为 921/921。随后用既有事务式 `Repair` 保存 `FoundationScene`，`FoundationSceneValidator` 最终 `PASS; Errors: 0`（`Logs/sc019-final-correction-validator.log` / `Logs/SceneValidationReport.txt`；SC-019 与既有 SC-009 反馈条修复均通过）。OpenCode2 worker 未运行 Play Mode；协调方在用户本次临时授权下实测主路线至 1560-07-01，资源到 45/15/3，最终 Console 为 0 errors / 0 warnings / 1 info（Unity AI `NoSubscription`）；用户正式验收仍待办；未提交/推送/合并/打 tag。详见 `SC019_DEMO.md` 与 `docs/reports/sc019-implementation.md`。 |
| SC-021 家族研究与技艺传承 | 代码、测试源、存档迁移、编年史、固定剧本、研究面板与事务式生成器已实现；场景已接入 `FoundationScene`；Unity Tundra 编译成功、`error CS` 0，聚焦 EditMode 126/126（含新增正向 v7 JSON 迁移回归），全量 EditMode 1103/1103，SC-021 Validator PASS，Console 最近 11 条 0 errors / 4 warnings / 7 info；Play Mode 与用户验收待办 | 纯 C# 确定性项目 `research.smoke-mixing` → 家族技艺 `technique.smoke-mixing`「烟幕调制术」；需求进度 2，每次成功月结恰好消耗 1 资金 + 1 物资并推进 1；同一家族只有一个活动项目，替换研究者保留进度；研究者须为存活、当前家族、16+、未退休且未担任产业职位；短缺/失效暂停且不扣资源/不推进；完成只记录家族知识，不授予个人掌握。月结顺序为产业→研究→教育→事件→日期/提交，研究不消耗 RNG，异常时回滚资源/进度/知识/日期/事件态/随机流/编年史。存档 v7→v8、`rulesVersion` 3→4，v7 迁移初始化空研究且不补算历史。固定影山家 `1560-01-01`、`2/2/0` 资源；UI 为 `Canvas/SC021ResearchRoot` 下的 `FamilyResearchPanel` 与标题入口；载入的暂停计划若研究者已失效，面板立即显示暂停原因，无需等待月结。生成器已实际运行并保存 `FoundationScene`，SC-021 根节点、隐藏面板与标题入口均已写入；Validator 整体仍为 `FAIL; Errors: 1`，唯一错误是既有 SC-009 反馈条位置。Play Mode 由用户执行；已验证文件推送在 `panyeung/sc021-scene-integration`，未合并 `main`、未打 tag。详见 `SC021_DEMO.md` 与 `docs/reports/sc021-implementation.md`。 |

## M03 当前实现状态（2026-11-07）

- 范围：只让固定剧本的正綱（`person.sc013.masatsuna`）与信正（`person.sc013.nobumasa`）参与自主决策；候选仅限休息、训练、工作；需求、人格权重、行动时长和同分枚举顺序全部为纯整数常量。
- 领域：`NpcAutonomyRules` 负责需求增长/完成衰减、资格/地点/行动排除、Utility 分数与稳定同分顺序；`NpcAutonomyState` 保存耐用需求，评估、状态与最多 24 条决策日志仅为会话内数据。
- 会话：`GameSession` 在新局初始化、行动完成、玩家中断和下一次模拟日检查空闲试点；活动任务期间不重新评分、不覆盖玩家行动；被拒绝/延期的规划只写决策日志，不改变既有行动。
- 存档：schema 从 v10 升为 v11（rules 7），新增只包含两名试点需求的 `npcAutonomy`；v10 迁移初始化默认需求并逐字段保留 `dailyActions`；校验拒绝未知/重复/越界/缺失/非名册试点。v11 读档不重新排程，活动任务保持权威；空闲试点在下一次模拟日恢复检查。
- 只读 Inspector：`NpcAiInspectorPanel` 只订阅并展示需求、偏好、执行状态、当前行动、三个候选的分数或排除原因、最近决策；控件仅选择人物、打开/关闭/刷新视图，不调用排程、中断或重新规划 API。
- 复审修正：v10 原始 JSON 迁移按 v9 字段引入版本放行 `familyDoctrine`/`activeEventScenarioId`，并保留完整 `dailyActions`；成功推进即使无行动完成也刷新需求与 Inspector；自主行动完成恢复可读消息；活动任务期间候选保留诊断分数但不可重新排程；四种命令结果均有触发原因/状态测试。
- 第二轮独立复审修正（目标 `12b52f3`）：`AdvanceDays`/月结按每个模拟日依次推进需求、行动与决策，月结仍只有一次事务；完成与每日检查分区，阻断的重规划每日只记一条；活动日刷新不再覆盖目标选择 `LatestEvaluation`，Inspector 同时显示纯诊断 `CurrentEvaluation`；继承/名册与 `ConfirmTravel` 触发 `EligibilityChanged` 并补上未登记试点拒绝路径；完成/中断保留 `LastOutcome`；诊断区改为 `ScrollRect` + 遮罩视口 + 垂直布局/内容自适应；Inspector 读取不再调用 `Ensure`/`Reconcile`；延期记录补充下次检查时机；v11 `rulesVersion` 不匹配拒绝而非静默归一，v10 迁移仍按预期升级。
- 场景：`AddM03NpcAiInspector` 通过单个 Undo 组创建/修复 `Canvas/M03NpcAiInspectorRoot`，保存前校验，并可替换既有生成子树；`FoundationScene` 已接入一个根、一个打开按钮与滚动诊断区，`FoundationSceneValidator` 为 `RESULT: PASS; Errors: 0`。
- 证据（2026-11-07 第二轮复审修正，Unity `6000.6.6f1`，Editor PID `90052`，项目路径 `C:\Users\panye\My project`）：删除临时自动运行脚本后最终 Tundra 编译成功且 `error CS` 0；聚焦 EditMode `140/140` 通过、全量 EditMode `1274/1274` 通过、0 失败/跳过/不确定（`Logs/m03-fix-focused.txt` 与 `Logs/m03-fix-full.txt`）；`FoundationSceneValidator` 为 `RESULT: PASS; Errors: 0`，M03 根节点恰好 1 个（`Logs/m03-fix-scene.txt`、`Logs/SceneValidationReport.txt`）。全量后 Console 为 2 errors / 25 warnings / 16 logs，两条 error 均为通过的 `FamilyFormationSessionTests` 故障注入日志。未运行 Play Mode；用户最终验收仍待办。

## SC-011 当前实现状态（2026-11-02）

- 切片：初代在 `event.rainy-arrival` 选择收留或推迟后，经一次 SC-010 交接，新家主在下一个月遇到对应后续事件 `event.rainy-arrival.legacy-honored` 或 `event.rainy-arrival.legacy-deferred`。
- 代码：新增 `EventRuleContext`、三种允许列表条件（`EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`）、内容版本 `2` 与存档 `1 → 2` 附加兼容窗口；未改场景、预制体、`ProjectSettings` 或包。
- 用户验证报告：编译通过；EditMode passed（未提供 Passed/Failed/Skipped 数量）；Console 无错误（警告数量未提供）；Play Test 通过并获用户验收。
- 证据边界：以上运行结果来自用户报告，本协调过程未操作 Unity Editor。`FoundationSceneValidator` 结果未报告，不记录为通过。
- 评审：OpenCode2 两轮独立只读静态审查均 ACCEPT；不替代 Unity 验证或用户验收。
- 验收：SC-011 已获用户明确验收；按项目规则合并 `main`、标记 `demo-SC011` 并推送。

## SC-012 当前实现状态（2026-11-03）

- 范围：在现有 Domain/Application/Persistence/Presentation/Editor 中追加家族编年史，记录已提交事件、实际关系数值变化、任务阶段变化和继承阶段变化；不新建 Unity 项目、场景或 package。
- 存档：schema 从 v3 升为 v4；v3 已保存事件历史按原年/月与顺序迁入，旧档缺失的任务、关系、继承经历保持为空，不从当前快照推算。
- 代码：OpenCode2 已完成 ledger、会话提交接线、存档迁移/校验、只读滚动面板、事务式 FoundationScene updater、validator 扩展及 EditMode 测试源文件。用户 Play Test 报告通过；FoundationScene 已包含编年史 UI。
- 静态/离线验证：实现 worker 报告过七个程序集 Roslyn 类型检查 0 错误，以及三项 harness 22/22、17/17、14/14；第三轮复审前的最后修复只报告静态 brace/whitespace 检查，旧 harness 未在后续修复后重跑。所有这些都不等同于 Unity 编译或 Unity Test Runner。
- 独立复审：第三轮 OpenCode2 只读复审 ACCEPT 当前代码/测试源；指出 FoundationScene 尚未接入会阻止面板显示和 Validator 通过，需先运行 updater、保存并验证。另有 append 失败日志/关系刷新缺少应用层测试，以及 v1 异常 chronicle 字段被忽略的低风险兼容边界。
- 用户验证报告：EditMode 通过；Play Test 通过并明确验收。具体 Passed/Failed/Skipped 数量未提供。
- 未单独报告：Console error/warning 数及 FoundationSceneValidator 报告；不据此声称错误为零或 Validator 通过。第三轮静态复审仍仅作为代码审查证据。
- 集成：checkpoint `4443ccc` (`feat(sc-012): add family chronicle`)；`main` 快进并推送至 `origin/main`，标签 `demo-SC012` 已推送。`feature/sc012-family-chronicle` 在确认完全并入 `main` 后从本地与远端删除。
- 当前状态：SC-012 已获用户验收并完成集成；下一候选按 GDD v4.0 为 M2 三代速通原型。

## SC-013 当前实现状态（2026-11-03）

- 范围：在现有 Domain/Application/Events/Persistence/Presentation/Editor 中实现固定六人、三代直系、两次合法交接、SC-013 专属继承承诺事件、SC-011 旧事回收、信任门槛终局、两个派生结局、家族名册与 M2 结局面板；不新建项目/场景/package，不修改 SC-011 数据。
- 新局：`GameSession.InitNewGame()` 默认 SC-013；`InitStandardDemo()` 与 `StartCrisisDemoScenario()` 保留 SC-010 标准/危机独立入口；标题界面三个入口互不污染。
- 门禁：M2 退休由 `M2FixedScenario.CanBeginRetirement` 统一派生，要求当代必须事件已提交、无待处理事件、固定继承人合法；标准/危机模式继续走既有继承规则。
- 内容：事件/文本 `contentVersion = 2`；SC-013 两条事件使用 `scenario: "sc013"` 隔离；SC-011 的 ID/条件/效果/选项可用性未改写（资源 diff 为纯追加）。
- 存档：schema 仍为 v4，未新增终局字段；稳定点恢复由 `M2ScenarioSaveTests` 覆盖。
- 静态验证：Unity 随附 Roslyn 对 Domain/Persistence/Events/Application/Presentation/Editor/Player/EditModeTests 八个程序集编译 0 错误；SC-013 固定剧本/名册 harness 30/30、完整事件链 harness 33/33。**不是 Unity Editor 编译或 Test Runner 证据。**
- 复审修正（2026-11-03）：修复 `WorldClockTests` 的 B1；名册面板改为 620 高以容纳七行 SC-010 名册；补充跨剧本读档拒绝与 M2 固定继承人守卫测试；SC-013 稳定 ID 改为引用 `SC013EventIds`；同步文档计数。以上均为源码/静态检查，仍未运行 Unity Editor 测试。
- 复审结论（2026-11-03）：`docs/reports/sc013-review.md` 初审判定 RETURN（B1 + 7 项非阻断）；修正后 `docs/reports/sc013-review-2.md` 与全新第三轮 `docs/reports/sc013-review-3.md` 均判定 ACCEPT。第三轮独立复跑八个程序集 Roslyn 0 诊断、harness 30/30 与 33/33，并复核 SC-011 事件/链/文本行与 `HEAD` JSON 相等；仅留三项低风险备注。仍不替代 Unity 验证或用户验收。
- 未完成与共享运行证据（2026-11-03）：专门的 SC-013 全量验收仍未完成——`FoundationSceneValidator` 未运行，SC-013 的 Console 计数未单独记录，用户 Play Test 与验收仍待用户本人完成。SC-018 的协调方授权验证在同一 Unity 6.6.6f1 Editor 上取得成功重编译、Editor EditMode 824/824（0 失败/跳过）与 SC-013 Play Mode，其中完成了 SC-013「继承前的家议」并到达第三代信任门槛结尾；这些是 SC-018 运行覆盖到的共享证据，不是专门的 SC-013 验收。`SC013_DEMO.md` 第 1 节保留精确菜单与验证步骤。
- 工作区保护：`SampleScene.unity`、旧方向脏文档、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/` 与 `nul` 均未触碰。

## SC-014 流亡少年与家系遗产（2026-11-03）

- 范围：在 SC-013 固定 M2 剧本中追加第二条跨代事件链 `chain.sc013.refugee-legacy`，共四个事件节点：第一代入口 `event.sc013.refugee-arrival`、第二代 `event.sc013.refugee-loyalty`、第三代盟友 `event.sc013.refugee-aid` 与第三代竞争 `event.sc013.refugee-rival`。
- 内容：仅追加 `Assets/Resources/Events/event-catalog-v1.json` 与 `Assets/Resources/Localization/event-text-zh-v1.json` 的链、事件、选项与中文文本；`contentVersion` 保持 `2`。所有关系效果指向既有 `npc.kuroda.iori`，不新增人物、关系模型、存档字段或事件规则。
- 门控：第一代入口仅在 `SuccessionCountEquals 0` 时出现；第二代要求第一代选择收留且 `SettledSuccessionCountAtLeast 1`；第三代两个互斥节点分别要求第二代选择尊重旧约或服从新政，且 `SettledSuccessionCountAtLeast 2`。拒绝/出卖分支终止本链。
- 调度兼容：新增行优先级分别为 `5`、`50`、`500`，低于同阶段既有的继承承诺/雨夜来客、SC-011 旧客回访和终局事件，因此不改变既有确定性月度排程。玩家需要在每次交接前额外结束月份，才能看到本链节点；现有 M2 退休门禁不要求本链事件。
- 审阅与验证：首轮 OpenCode2 独立只读审阅 ACCEPT 并列出六项非阻断一致性问题；已逐项修正，第二轮 OpenCode2 只读复审 ACCEPT。SC-011 事件/链/18 条文本与 `HEAD` JSON 相等；SC-013 仍未提交，不能逐字节对比 `HEAD`，复审明确保留此限制。实现侧完成 JSON 结构/引用静态检查；未运行 Unity 编译、EditMode、Console、FoundationSceneValidator、Play Mode 或存读档试玩，未新增或运行测试，未取得用户验收。详见 `SC014_DEMO.md` 与 `docs/reports/sc014-review-2.md`。

## SC-015 三代家书（2026-11-03）

- 范围：在 SC-013 固定 M2 剧本中追加第三条跨代事件链 `chain.sc013.family-letters`，共七个事件节点：第一代入口 `event.sc013.family-letters.first`；第二代公开路径 `...second-open` 与封存路径 `...second-sealed`；第三代四个互斥结尾 `...third-full-public`、`...third-short-history`、`...third-opened-by-heir`、`...third-sealed`。
- 内容：仅追加 `Assets/Resources/Events/event-catalog-v1.json` 与 `Assets/Resources/Localization/event-text-zh-v1.json` 的链、事件、选项与中文文本（7 个事件、10 个选项、27 条文本）；`contentVersion` 保持 `2`。所有新效果数组为空，不新增存档 schema 字段，也不改变关系、任务或家族数值；选择、事件历史、一次性标记与已提交结果仅写入既有事件运行时存档字段。不新增人物、事件规则类型或条件类型。
- 门控：第一代入口要求 `SuccessionCountEquals 0`；第二代两个互斥节点分别要求第一代公开或封存选项，并加 `SettledSuccessionCountAtLeast 1`；第三代四个互斥节点分别要求对应第二代选项的唯一来源，并加 `SettledSuccessionCountAtLeast 2`。选择、后续节点和结尾全部由既有事件历史表达。
- 调度：`EventEngine` 按 priority 降序排程，priority 数值越大越早。本链使用 `6`、`60`、`600`，分别高于同世代 SC-014 的 `5`、`50`、`500`，低于 SC-013 必须事件的 `1000`，同时低于第一/二代 SC-011 行的 `10`、`100`，因此不改变既有 SC-011 事件顺序。M2 退休门禁未因本链改变；玩家需在每次交接前额外结束月份才能看到可选节点。
- 静态检查：两个 JSON 可解析；`contentVersion` 为 `2/2`；与实现前快照逐项比较，3 条既有链、9 个既有事件、50 条既有文本全部 JSON 相等（纯追加）；新增 1 条链、7 个事件、27 条文本；链成员、事件/选项/文本引用、源选项唯一性、分支互斥、世代门槛、空效果与优先级关系检查 10 组 0 错误。
- 运行证据（2026-11-03）：助手在用户临时授权下使用现有 Unity 6.6 Editor / `FoundationScene` 执行 Play Mode。分支 A（公开→保留原貌→完整记名）、B（公开→简史→简要传述）、C（封存→正纲拆阅→第三代收到完整记录）、D（封存→继续封存→第三代仍未拆阅）均到达预期第三代家书事件，家族编年史显示对应结果。D 首次操作记录未复现，之后按步骤复测通过；不据首次操作判定代码缺陷。Play Mode 停止后 Console 为 0 errors、0 warnings、0 logs。未运行 EditMode、FoundationSceneValidator 或存读档；未单独取得编译日志；未保存测试进度；用户验收待确认。详见 `docs/reports/sc015-playtest.md`。

## SC-016 委托旧约（2026-11-03）

- 范围：在 SC-013 固定 M2 剧本中追加第五条跨代事件链 `chain.sc013.commission-legacy`，共七个事件节点：第一代入口 `event.sc013.commission-legacy.first`；第二代守约 `...second-kept` 与失约 `...second-broken`；第三代四个互斥结尾 `...third-safe`、`...third-public`、`...third-cleared`、`...third-severed`。
- 内容：仅追加 `Assets/Resources/Events/event-catalog-v1.json` 与 `Assets/Resources/Localization/event-text-zh-v1.json` 的链、事件、选项与中文文本（新增 1 条链、7 个事件、10 个选项、27 条文本）；`contentVersion` 保持 `2`。三个决策事件共六个选项使用现有 `RelationshipDelta`，目标固定为既有 NPC `npc.sakai.jirobei`（酒井次郎兵卫），数值按设计为接受/守密/补偿 `+5`、拒绝/公开/拒绝再牵连 `-5`；四个第三代结尾选项 `effects` 为空。不新增人物、事件规则、条件类型、存档字段或场景资源。
- 门控：第一代入口仅在 `SuccessionCountEquals 0` 时出现；第二代两个互斥节点分别要求第一代选择接受或拒绝，并加 `SettledSuccessionCountAtLeast 1`；第三代四个互斥节点分别要求对应第二代选项的唯一来源，并加 `SettledSuccessionCountAtLeast 2`。选择、后续节点与结尾全部由既有事件历史表达。
- 调度：`EventEngine` 按 priority 降序排程（数值越大越早）。本链优先级为 `8`、`80`、`800`，低于 SC-013 必须事件的 `1000` 与 SC-011 的 `10`、`100`，高于同世代 SC-015 的 `6`、`60`、`600` 和 SC-014 的 `5`、`50`、`500`，因此不改变既有确定性月度顺序。M2 退休门禁未因本链改变；玩家需在每次交接前额外结束月份，若首次交接前未触发入口，本链不会迟到启动，也不阻塞交接。
- 静态检查：两个 JSON 可解析；`contentVersion` 为 `2/2`；与实现前快照逐项比较，4 条既有链、16 个既有事件（含 31 个选项）、77 条既有文本全部 JSON 相等（纯追加）；新增链成员顺序、事件/选项/文本引用、源选项唯一性、分支互斥、世代门槛、关系目标与 `+5/-5` 数值、空结尾效果及优先级关系共 10 组检查、0 错误。
- 运行证据（2026-11-02 本机时间）：助手在用户临时授权下使用现有 Unity 6.6.6f1 Editor / `FoundationScene` 运行四条 Play Mode 路线：接受→继续守密“夜路仍然安全”、接受→公开真相“账册见了天日”、拒绝→主动补偿“旧账已清”、拒绝→拒绝再牵连“商路不再相通”；每条路线的编年史与结尾一致。随后通过 Assets > Refresh 触发脚本重编译，并在 Test Runner 运行完整 EditMode，778/778 通过、0 失败、0 跳过。Console 当前计数 0 errors / 14 warnings / 8 logs（会话累计）。`FoundationSceneValidator`、存读档和用户正式验收仍待完成。
- 场景保护：测试后从磁盘重新打开 `FoundationScene`，未保存；场景 SHA-256 与测试前记录一致。未保存测试进度、未提交、推送、合并或打标签。详见 `SC016_DEMO.md` 与 `docs/reports/sc016-implementation.md`。
- 工作区保护：SC-016 数据切片只追加上述两个 JSON 与五份文档；后续测试夹具修正仅涉及 `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs`。`SampleScene.unity`、旧方向脏文档、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/` 与 `nul` 均未触碰；未提交、推送、合并或打标签。

## SC-017 通用家谱展示（2026-11-03）

- 范围：将 SC-013 固定六行名册替换为只读、数据驱动、可滚动的通用家谱；新增 `FamilyTreeGraph` 快照/构建器与 `FamilyTreeLayout` 确定性布局，改写 `FamilyRosterPanel` 消费图数据并绘制人物卡、typed 关系线、未连接亲族区和家臣/弟子/客将区。
- 数据来源：`SuccessionState` / `SuccessionWorld` 的成员、姓名、生活状态、家主/指定继承人，以及 `KinshipGraph` 的 `KinshipType` 结构事实；不读取 `M2FixedScenario` 的固定展示行，不写死 SC-013 人名、ID、人数或代数。
- 展示：亲子金、收养紫、手足绿、配偶蓝；家臣/弟子/客将不绘制亲缘线；无可见关系的家族成员进入「未连接亲族」区；图例、家主/有效继承人/失效继承人标记；视口内双向滚动；无六人或三代上限。
- 边界：本轮只读，不提供关系编辑、debug mode、存档编辑、人物肖像或新 package；不改 GDD、存档 schema、SC-010/SC-011/M2 规则，不新建项目/场景/预制体。
- 场景：`AddSC013FamilyRosterPanel` 已改为事务式建立 ScrollRect/内容/连线层/节点层/模板，并可在同一 Undo 事务内识别替换旧六行层级；`FoundationSceneValidator` 检查新绑定。**本 worker 未运行 updater、未保存场景、未运行 validator。**
- 静态验证：最终修正后 Unity 随附 Roslyn 对八个程序集编译 0 错误；纯图/布局 harness 86/86 通过（原 77 项 + 9 项拥挤配偶线检查）。**不是 Unity Editor 编译、Test Runner 或 Play Mode 证据。**
- 独立复审：Codex fallback 只读复审 ACCEPT；无 Critical/Important，轻微备注为空路由失败分支没有 FamilyRosterPanel 专项测试。
- 用户验收：2026-11-03 用户明确接受本次演示。用户未单独提供 EditMode 数量、Console 计数或 Validator 报告文本；实施与复审 worker 的“未运行”记录只描述各自执行范围。
- 工作区保护：未提交、未推送、未合并、未打标签；已有脏文件（`FoundationScene.unity`、`SampleScene.unity`、旧方向文档、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul`）均未清理或覆盖。

## SC-018 独立家族事件（2026-11-03）

- 范围：将 M2 事件目录从 23 项增至 30 项，维持五条既有跨代链；新增七个可独立触发、每局一次的家族生活事件，并增加按 `(当前家主 PersonId, 家人 PersonId)` 人物对键控的家庭数值关系、v5 存档兼容与编年史姓名/前后值显示。不引入第六条链、事件依赖、金钱/物资/任务/成长效果或新场景。
- 内容：`Assets/Resources/Events/event-catalog-v1.json` 新增 7 个事件、14 个选项；`Assets/Resources/Localization/event-text-zh-v1.json` 新增 35 条原创中文文本。七个事件均为 `Monthly`、`PerGame`、`scenario: "sc013"`、无 `chainId`、`cooldownMonths: 0`、`priority: 1`；每个选项使用新 `FamilyRelationshipDelta` 与 `targetPersonId`，数值为 `+5`/`-5`。
- 家庭关系语义：键为 `(当前家主 PersonId, 家人 PersonId)`。第一代键以晴信为主体；继承后新家主只读取自己的键，旧家主记录保留，不复制、不推算。值域 `[-100, 100]`，拒绝空人物、家主自身和非当前家庭成员；事件提交失败时 NPC 与家庭关系效果一起回滚。
- 人物适配：当前 M2 名册六人均为 `person.sc013.*`，没有 `AdoptedChild` 成员。七个主题保留；`adoption-welcome` 按设计授权改写为旁系亲属時姫询问自己是否算家中一员，不新增人物、收养关系或虚构身份。会指向正綱/信正的事件使用既有 `SuccessionCountEquals 0` 限定在第一代，避免后续家主成为自己的关系目标；其余事件只指向不会成为家主的千恵/時姫。
- 运行时：新增 `Assets/Scripts/Domain/FamilyRelationships.cs`；`EventEffectKind` 增加 `FamilyRelationshipDelta`，`EventEffectData` 增加 `targetPersonId`；`EventRuleContext` 携带当前家主与家庭成员；`EventEngine` 事务式应用/回滚家庭效果并报告 `AppliedFamilyRelationships`；`GameSession` 持有、捕获、恢复家庭关系并把实际变化写入编年史。
- 存档：schema 从 v4 升为 v5，新增 `familyRelationships` 人物对数组；`SaveDataValidator` 校验人物、非自身、重复键、成员关系与值域；`SaveDataMigrator` 支持 v4→v5 空集迁移，拒绝已含家庭值的 v4 载荷，v1–v3 迁移同样初始化为空；v4 presence 仍要求 `chronicle`，仅 `familyRelationships` 允许在迁移前缺省。旧存档不推算历史好感。
- 编年史：实际变化以 `Relationship` 类别追加，标题显示当前家主与家人双方姓名，摘要显示精确 `前值 → 后值`；复用现有编年史面板，不改场景 UI。NPC `CharacterRelationships` 与既有 NPC 事件行为保持不变。
- 调度：`EventEngine` 按 priority 降序排程（数值越大越早）。新事件 `priority: 1` 低于 `event.rainy-arrival` 的 `10`、SC-011 的 `100`、SC-013 必需事件的 `1000`，也低于 SC-014/SC-015/SC-016 的既有可选节点；七个同优先级事件由既有加权随机决定顺序，且每个只出现一次。`scenario: "sc013"` 使它们只出现在 M2 固定剧本，不进入 SC-010 场景中立样例。
- 测试：新增 `Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs` 及 `.meta`，覆盖 30 事件/5 链、七个新事件的无链/月度/每局一次、世代门槛、完整中文文本、非家主家庭目标、可见关系变化、既有五链与 23 个既有事件定义、优先级、人物对键控/继承不复制/边界值、事件事务与回滚、NPC 行为保持、`GameSession` 自身/非成员/非当前家主拒绝、v5 存读档、v4 迁移、v3 迁移空集、自身/未知成员/重复人物对/越界值拒绝与编年史姓名/前后值。`SaveDataValidatorTests` 的过期“v5 不支持”断言改为 v4。
- 静态/逻辑验证：两个 JSON 解析成功；`contentVersion` 为 `2/2`；30 个事件、5 条链、139 条文本；7 个新事件、14 个选项、35 条新文本；与实现前快照比较，23 个既有事件、5 条链、104 条既有文本 JSON 相等，文本前缀逐字节保持（忽略 CRLF）；引用、唯一性、家庭目标、关系数值、摘要文案、优先级和链成员检查 0 错误。八个程序集使用 Unity 6.6.6f1 随附 Roslyn 编译，全部退出码 0、0 诊断；纯 C# harness 82/82 通过（复审修正后由 67 项扩充至 82 项）。最终续作以及人物对方向措辞中性化、编年史标题改为 `A 与 B` 后，均复跑同一组检查，结果一致。**这些 host-side 检查不是 Test Runner 或 Play Mode 证据。**
- 运行证据（2026-11-03，协调方在用户临时授权下执行）：在现有 Unity 6.6.6f1 Editor 的 `FoundationScene` 中从「新游戏」开始全新 M2 剧本，按第 3.1 节顺序完成五个前置事件（SC-013 家议、雨夜来客、SC-016 账册托付、SC-015 千恵的家书、SC-014 门前的流亡少年）；七个 SC-018 事件各出现且仅出现一次；随后完成两次交接、SC-011 旧客回访与 SC-013 信任门槛的第三代结尾；核对家庭关系编年史前后值。保存并读取 schema-v5 存档槽，槽内四条家庭关系记录与 30 条编年史条目，读取后推进月份再读取，确认回到 1561 年 7 月状态与历史。首次运行因试玩步骤失误漏掉三个前置事件，该次作废且未保存；干净重跑通过。测试前备份用户存档槽 JSON 与 `.bak`，测试后按字节原样还原，未留下被改动的用户存档。
- 编译与 EditMode：`Logs/Editor.log` 最新脚本重编译成功，`*** Tundra build success` 由协调方记录在第 8280、10579 行，按 1-based grep 复核为第 8279、10578 行（同两条记录，计数基准差一行）；同一会话此前有 `AddSC013FamilyRosterPanelTests.cs` 的 `CS0117`/`CS7036` 临时编译错误，后续重编译已消除，本文档保留该先后顺序。`%USERPROFILE%\AppData\LocalLow\DefaultCompany\My project\TestResults.xml` 记录 EditMode 824 passed / 0 failed / 0 skipped，时间 `2026-11-03 21:12:19Z`–`2026-11-03 21:12:24Z`，包含 `SC018IndependentFamilyEventsTests` 的 20 个用例且全部通过。Play Mode 前 Console 为 0 errors / 0 warnings，另有 1 条信息级 `NoSubscription` 消息重复五次；测试后的最终 Console 计数未直接观察，不声明为零。
- 未完成：`FoundationSceneValidator` 未运行（本任务未改场景，不声明场景验证通过）；用户 Play Test 与验收仍未完成。协调方授权验证不替代用户验收，也不构成里程碑接受。详见 `SC018_DEMO.md` 第 0.1 节与 `docs/reports/sc018-implementation.md`。
- 工作区保护：用户明确要求累计集成 SC-018–SC-022；checkpoint `714cb3a` 已推送至 `origin/main`。SC-018 的用户 Play Test/验收状态仍按本节记录。未纳入提交的 `SampleScene.unity`、`Assets/1.unity`、`Assets/1.unity.meta`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 均保留在本地。

## SC-019 家族产业首切片（2026-11-03）

- 范围：M3 第一个纵向切片——农庄 + 商馆、资金/物资/情报、当前家族成年成员任命、4 单位月预算/3 单位职位预留、替换/解除、确定性月结、空缺/短缺/失效、两次继承交接、v6 存档与 v5 迁移、产业编年史、uGUI 面板与事务式场景生成器。不是完整家族经营系统。
- 数值：新局 `30/12/0`，两产业空缺；晴信任农庄、千恵任商馆后结束 `1560-04` 得 `35/13/1`；农庄先收成 `+2资金/+4物资` 再保留 `1物资` 维护，净 `+2资金/+3物资`；商馆先支付 `6资金/2物资` 再产出 `+9资金/+1情报`；满仓 `999999` 时净产先算再夹上限，不会掉到 `999998`。
- 时间：`TryAdvanceToNextMonth` 是唯一月结入口；`AdvanceOneDay`/`AdvanceDays`/`PreviewTravel`/`ConfirmTravel` 跨月整笔拒绝并给出中文原因；待处理事件、继承决策、幼主成年交权、家族灭亡与产业月份不一致均拒绝且保持状态。既有跨年夹具改用 internal `AdvanceDaysForLegacyFixtureSetup`，生产 API 不变量不变。
- 继承：交接成功后只移除失效任命；旧家主退休解除其职位并释放预留，新家主不自动接管，其他合法任命保留；每个成人只有自己的 4 单位，继承不转移预算；实际解除追加一条「产业人事」编年史。
- 存档：schema v5→v6、`rulesVersion` 1→2；新增 `industry`（资源、`lastSettledMonthIndex`、两个稳定槽位）；v5 迁移保留 SC-018 `familyRelationships`、chronicle、事件/继承/任务/日期/地点，产业初始化为旧档日期的 `30/12/0` 与空槽，不补算历史产出；已含 `industry` 的旧载荷拒绝。
- 测试（本 worker，普通 Unity Editor Test Runner，非 batchmode）：最终全量 `907 passed / 0 failed / 0 skipped`（`2026-11-03 23:56:29Z`，`Logs/sc019-final3.xml`）；编译 `error CS` 计数 0。领域四类在实现前以 stub 观察红：`35 total / 0 passed / 35 failed`（23:05:23Z，`Logs/sc019-red.xml`）；其余 Task 3–7 测试为实现后补充，未声称先红后绿。首次普通 Editor 聚焦 `82/81/1`（V6 presence 测试夹具），修正后 `82/82`；首次全量 `906/901/5`（4 项 M2 预览布局 + 1 项 SC-010 按钮状态），根因为 `WorldClockPanel` 在 EditMode 预览中因 `WorldClock` 未初始化而禁用 M1 按钮，修正后 `907/907`。审查修正后最终为 `923/923`，见本节末条。
- 场景：事务式生成器已写入 `IndustryRoot`/`IndustryPanel` 并保存 `FoundationScene`；SC-019 专项 PASS。随后按协调方指示用既有 `AddSC005EventChoicePanel` 事务式反馈条修复路径处理已知 SC-009 基线问题，最终 `FoundationSceneValidator` 为 `PASS; Errors: 0`（`Logs/SceneValidationReport.txt`，23:54:25Z）。生成器曾因 EditMode 绑定阶段生成候选按钮而空引用回滚（未保存），修正为绑定时不刷新候选并重建场景；最终场景无运行时候选按钮，仅保留隐藏模板。一次性 `RunMenuOnFoundationScene` / `RepairSc009FeedbackOnFoundationScene` 包装方法已从源码移除，生成器只保留 `RunMenu` 菜单。
- 场景安全说明：本 worker 曾使用一次性 `RebuildOnFoundationScene`，以 `DestroyImmediate` 在 Undo 组之外删除旧 `IndustryRoot` 后保存；这违反了项目单一 Undo 组规则，已由协调方指出并从源码移除，之后未再运行破坏性重建。独立 YAML 对比：HEAD 721 个基线块，0 个删除/改名；新增 109 个 SC-019 块；仅 3 个基线块变化（Canvas 子节点列表、`EventChoiceFeedbackText` 矩形、`EventChoicePanel` 文本对齐，后两项为既有 SC-009 修复）。
- 审查修正（2026-11-04，同一 worker 最终收敛）：独立审查阻断项为打开按钮左上锚定 `(20,-20)` 与同原点 430×210 `SuccessionHud` 重叠，覆盖家主/年龄行。现改为 HUD 下方左上锚定 `(20,-250)`（200×44，与 HUD 底边留 20 px），位于画布内且不与编年史/名册按钮、SC-009 顶部反馈条或地图带重叠；`AddSC019IndustryPanel.Validate` 新增 `SuccessionHud` 实际矩形重叠检测。新增 `AddSC019IndustryPanel_OpenButtonClearsSuccessionHud`（按钮不压 HUD 且在画布内）与 `AddSC019IndustryPanel_ValidateRejectsOpenButtonOverSuccessionHud`（validator 能抓回归，且事务式 `Repair` 会把越界按钮移回）。月结回滚测试 `EndMonth_FaultAfterDateAdvance_RollsBackDateResourcesLastSettledAndChronicle` 现在在注入异常时证明事件排程已消耗事件随机流，并在回滚后断言完整随机状态与 `EventRuntimeSaveData` 均等于结算前，随后保留恢复重试断言。全新全量 `923/923`（`2026-11-04 04:15:29Z–04:15:35Z`，`Logs/sc019-final-correction-all.xml`），`error CS` 计数 0；修正前基线 921/921。场景仅经既有 `RunMenu`→`Repair` 事务路径保存，无新包装方法、无直接 YAML 编辑、无场景重载回滚；`FoundationSceneValidator` 最终 `PASS; Errors: 0`（`Logs/SceneValidationReport.txt`）。场景 SHA-256 `39f5c829…` → `87e80d71…`，`git diff --numstat` 行数不变，仅按钮矩形属性值改变。Unity 运行期间 `ProjectSettings/ProjectSettings.asset` 的 Standalone define 顺序被重排，已在停止 Unity 后恢复原顺序并确认 `git diff` 为空。
- 未完成：OpenCode2 worker 未运行 Play Mode；协调方已在用户本次临时授权下实测主路线，Play Mode Console 为 0 errors / 0 warnings / 1 info（Unity AI `NoSubscription`）；用户正式验收、存读档、跨月日推进/旅行、资源短缺分支仍待完成。用户随后要求累计集成，SC-019 随 checkpoint `714cb3a` 推送到 `origin/main`；未打 tag。详见 `SC019_DEMO.md` 与 `docs/reports/sc019-implementation.md`。

## SC-020 子女培养与人物成长（2026-11-04）

- 范围：子女教育纵向切片——青叶家 `1560-01-01` 开局，宗一郎 40 / 小夜 38 / 千景 15；千景五项能力 `15/12/20/10/13`；选择五项定向方向或自由成长、成年家族导师，经历 1560 年 12→1 月结算，在 1561-01-01 看到成年与一次确定成长。**不是完整家族经营系统**。
- 规则：纯 C# `CharacterGrowthRules` / `CharacterDevelopmentState`；定向 `+4` 基础、资质 `+2`、导师专长 `+2`、有利环境 `+1`，夹 `0..100`；自由成长五项各 `+1`；无随机数。固定组合：学问+小夜 `20→29`，学问+宗一郎 `20→27`。计划按 `(PersonId, AgeYear)` 保存，同年度可替换、结算后不可改；单年只结算一次；未提交年度按自由成长；导师失效时自由成长兜底。
- 月结：`TryAdvanceToNextMonth` 在既有事务中按“产业→教育→下一月事件→日期/提交”执行；12→1 月只结算一次，写入 `ChronicleCategory.Education` 记录；失败回滚日期、产业、成长、计划、随机流、事件运行态与编年史，可重试。
- 存档：schema v6→v7、`rulesVersion` 2→3；新增 `development` 五项能力、年度计划与结果；v6 迁移为每个现有名册人物初始化五项 `0` 与空教育记录，保留 SC-018 关系、chronicle、eventRuntime、succession、industry，不补算历史；旧版含非 null `development` 载荷拒绝；v1–v5 继续迁移链到 v7。
- 场景/UI：新增 SC-020 标题入口、培养面板（五项值、年龄年度、六方向、导师、预览、提交/消息）与事务式 `AddSC020CharacterEducation` 生成器；`FoundationSceneValidator` 已接入，但**本 worker 未执行生成器/Validator**。SC-013 与 SC-019 入口和剧本保持不变。
- 证据：Unity 随附 Roslyn host-side 编译 Domain/Events/Persistence/Application/Presentation/Editor/EditModeTests 全部 exit 0；纯 Domain harness 17/17、Persistence/Save harness 20/20。**未运行 Unity EditMode、Console、场景 Validator 或 Play Mode**；`Logs/Editor.log` 最后可见 `Entering Playmode with Reload Domain disabled.` 且无法在无 bridge/无 GUI 条件下确认当前 Play Mode/dirty 状态，故按协调方要求停在场景写入 gate。用户 Play Test 与正式验收 pending。
- 工作区：累计 checkpoint `714cb3a` 已推送到 `origin/main`（按用户明确指示）；SC-020 的 Editor EditMode、场景生成/Validator、Console、Play Mode 与用户验收仍按上文标为未完成。`SampleScene.unity`、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 保留在本地且未纳入提交。详见 `SC020_DEMO.md` 与 `docs/reports/sc020-implementation.md`。

## SC-021 家族研究与技艺传承（2026-11-04）

- 范围：家族研究纵向切片——影山家 `1560-01-01` 开局，安排一名合格家族成员研究原创技艺「烟幕调制术」，两次成功月结后把技艺记录为家族知识。**不是完整家族经营系统**；不含战斗/任务施放、个人掌握、师徒教授、大型科技树、研究随机事件或可视化节点编辑器。
- 规则：纯 C# `FamilyResearchCatalog` / `FamilyResearchState` / `FamilyResearchSettlement`。目录只有 `research.smoke-mixing` → `technique.smoke-mixing`，需求进度 `2`，每月恰好 `1 资金 + 1 物资 → 1 进度`；同一家族只有一个活动项目，替换研究者保留进度，已完成技艺不能再次研究；研究者须为存活、当前家族、`16+`、未退休且未担任产业职位；资金/物资不足或研究者失效时暂停且不扣资源、不推进；完成只记录家族知识，不授予个人掌握；研究结算不读取随机流。
- 月结：`TryAdvanceToNextMonth` 在既有事务中按「产业→研究→教育→下一月事件→日期/提交」执行；研究计划只在本月未结算时推进；异常回滚日期、产业资源/结算索引、研究计划/进度/知识、成长、事件运行态、随机流与编年史，可重试。
- 编年史：完成时追加恰好一条 `ChronicleCategory.Research` 记录，标题「家族研究」，摘要含技艺名、研究者姓名、`1 → 2` 与完成年月；不显示 `research.`/`technique.`/`person.` 原始 ID。
- 存档：schema v7→v8、`rulesVersion` 3→4；新增 `research`（一个可选项目、进度、最后结算月、排序去重的家族技艺 ID）；v7 迁移初始化空计划/空知识且不补算历史；拒绝旧版本携带研究载荷；v1–v6 迁移链继续工作。
- 固定剧本：`family.kageyama.sc021`，宗次 40 / 奈绪 38 / 小太郎 18，开局 `2 资金 / 2 物资 / 0 情报`；不改变 SC-010/013/019/020 名册、事件、资源或场景行为。
- 场景/UI：新增 `FamilyResearchPanel`、`SC021FamilyResearchScenario`、事务式 `AddSC021FamilyResearch`（一个 Undo 组、保存前验证、仅用 Undo 回滚、不重载场景、固定在 `Canvas/SC021ResearchRoot`）；`FoundationSceneValidator` 已接入。2026-11-04 场景集成轮已在任务 worktree 的独立 Editor 中执行生成器并保存 `FoundationScene`（`SC021ResearchRoot`、隐藏 `SC021ResearchPanel`、`SC021ResearchOpenButton`、研究者模板与标题入口 `SC021ResearchDemoButton`；该按钮是标题界面的独立示范入口，启动固定影山家剧本，不属于 SC-013 标准/危机成对按钮）；排序行比较仅 `researchDemoButton` 从 `fileID: 0` 变为非零引用，无既有对象删除/改名。
- 验证：Unity 6.6.6f1 随附 Roslyn host-side 编译 Domain/Persistence/Events/Application/Presentation/Editor/EditModeTests 全部 exit 0（0 诊断）；纯 C# SC-021 harness `70/70`，存档 harness `21/21`，26 个纯 C# 用例通过（另 2 个 `GameSession` 用例因宿主缺少 Unity 运行时未执行）。2026-11-04 场景集成轮在 worktree Editor（`Logs/editor-sc021-integration.log`）取得 Unity Tundra 编译成功、`error CS` 计数 0；场景集成轮聚焦 EditMode `125 passed / 0 failed / 0 skipped / 0 inconclusive`（`Logs/sc021-integration-tests.txt` / `.xml`，复审 follow-up 后为 126/126）；`FoundationSceneValidator` 的 SC-021 行为 `PASS`，整体 `RESULT: FAIL; Errors: 1`，唯一错误是既有 SC-009 反馈条位置；Console 最近 11 条为 `0 errors / 4 warnings / 7 info`（3 条 warning 来自负向测试，1 条为 Unity AI 账号提示）。修复了 v7 载荷被 `development` 提前拒绝、导致两个既有 SC-021 迁移用例失败的问题（v7 拥有 `development`，只应拒绝 `research`），未改 schema/研究规则。复审 follow-up 新增正向 v7 JSON 迁移回归测试（合法 v7 携带既有 `development` 时接受并保留，研究初始化为空；带 `research` 的 v7 仍拒绝），生产逻辑无需再改；同一 worktree batchmode Test Runner 复跑单测 1/1、聚焦 126/126、全量 EditMode 1103/1103（`Logs/sc021-followup-*.xml`），三份日志 `error CS` 均为 0。**未运行 Play Mode，用户验收仍待办**。
- 工作区：累计 checkpoint `714cb3a` 已推送到 `origin/main`（按用户明确指示）；场景集成与验证提交推送在 feature 分支 `panyeung/sc021-scene-integration`，未合并 `main`、未打 tag。`SampleScene.unity`、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 保留在用户 checkout 且未纳入提交。详见 `SC021_DEMO.md` 与 `docs/reports/sc021-implementation.md`。

## SC-022 婚姻与日常收养（2026-11-04）

- 范围：婚姻与普通收养纵向切片——原创浅羽家 `1560-05-01` 开局，玩家从独立「婚姻与收养」面板选择成年家族成员与已注册非成员候选人，登记多位正式配偶或办理普通收养；关系写入真实成员身份、亲属事实、编年史和现有存档。**不是完整家族经营系统**；不含离婚、婚姻等级、恋爱/嫁妆、子女出生、继亲或跨家族政治。
- 规则：`FamilyFormationRules` 沿 `ParentChild`/`Adoption` 有向边双向判断直系关系，并以 visited 集合处理既有坏环；婚姻拒绝本人、重复配偶边、直系血亲/养亲，兄弟姐妹不额外阻止；普通收养允许儿童和成年候选人，拒绝本人、正式配偶、重复同向收养边和亲子链循环；收养保留既有血缘边，不改变家主、继承阶段、指定继承人、继承次数或危机标记。
- 原子性：命令先完成资格、重复边、直系/循环、日期和编年史条目校验，再写成员、亲属边和一条 `ChronicleCategory.FamilyFormation` 记录；不推进日期、不运行月结、不消耗资源或 RNG。审查后补充：若预备编年史 append 在成员/亲属边写入后失败，命令先移除刚追加的亲属边与成员身份再返回失败；`KinshipGraph` 仅新增 internal 回滚入口，公开追加式语义与存档表示不变。失败时成员、亲属边、编年史、继承、日期、资源与 RNG 保持不变。
- 固定剧本：`family.asaba.sc022`，政宗 36 / 千代 32 / 小夜 10 / 弦次郎 24；两名成年婚姻候选人、兄弟姐妹候选人、直系血亲/养亲拒绝对象、儿童与成年收养候选人；候选人均注册为人物但不加入家族。
- 存档：复用现有 `SuccessionSaveData`（全部人物、生命、成员、亲属边）与 `SaveData` 编年史，不新增字段、不升 schema；`saveFormatVersion` 保持 `8`、`rulesVersion` 保持 `4`。JSON 往返测试证明候选人、成员角色、配偶/收养边、编年史顺序与显示文字一致，旧标准存档仍可读取。
- 场景/UI：新增 `FamilyFormationPanel`、`SC022FamilyFormationScenario`、事务式 `AddSC022FamilyFormation`（一个 Undo 组、保存前验证、Undo 回滚、不重载场景）；固定根 `Canvas/SC022FamilyFormationRoot`，面板默认隐藏，标题入口 `SC022FamilyFormationDemoButton`。生成器已实际运行并保存 `FoundationScene`；SC-022 Validator 行通过。
- 验证：在已打开的 Unity `6000.6.6f1` Editor 中通过 MCP relay 完成刷新与编译，动态命令编译/执行成功；P2 修正后 SC-022 聚焦回归 `70/70`（0 失败/跳过，`Logs/sc022-tests.txt` 与 `Logs/sc022-tests.xml`），其中 `FamilyFormationSessionTests` 12 项（含两个 append 失败回滚用例）。全量 EditMode `1098/1100` 为修正前历史证据，未在修正后重跑；两项失败为既有 SC-021 迁移消息顺序用例，非 SC-022 路径。修正后 Console 最近 15 条为 `2 errors / 2 warnings / 11 info`，4 条 error/warning 均为测试路径刻意触发（两个 `LogAssert.Expect` append 失败日志、两个生成器负向回滚 warning），不是回归。`FoundationSceneValidator` 修正后重跑：活动场景 `loaded=True dirty=False`，SC-022 行 `PASS`，整体 `FAIL; Errors: 4`；4 项均为既有缺口（SC-021 面板/根/标题引用未写入场景、SC-009 反馈条布局）。助手已在现有 Editor 执行 SC-022 Play Mode：标题入口、面板开关、多配偶、儿童/成年人收养、直系关系拒绝、存档数据捕获/恢复与编年史显示均通过；Console `0/0/0`，随后退出 Play Mode，场景 `dirty=False`。未截图或使用 Computer Use；用户验收待办。
- 工作区：SC-018–SC-022 累计 checkpoint `714cb3a` 已推送至 `origin/main`；用户明确要求合并与 push。Play Mode 已通过助手运行，正式用户验收仍待明确确认。`SampleScene.unity`、`Assets/1.unity`、`Assets/1.unity.meta`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 保留在本地且未纳入提交；未改 `Library`、`Temp`、package manifest 或既有 `.meta` GUID。详见 `SC022_DEMO.md` 与 `docs/reports/sc022-implementation.md`。

## SC-023 隐居与后见人（2026-11-05）

- 范围：隐居/后见人纵向切片——原创橘家 `family.tachibana.sc023` 从 `1560-12-01` 开局，宗严 62 / 赖久 32 / 千代 10。玩家先把家督交给成年继承人赖久，再让退休为 `DirectKin` 的宗严担任千代的年度导师，经历 1560→1561 年结并查看成长与编年史。**不是完整家族经营系统**；不含复职、离族、死亡模拟、独立后见人职位、双重权威或资源成本。
- 规则：共享 `EducationMentorRules`。非退休导师保持「存活、成年、当前家族、非培养对象本人」；仅 SC-023 固定家族放宽退休前任家主，且必须已退休、家族角色 `DirectKin`、存活、成年，并由既有继承编年史的真实交接记录证明其曾任旧家主（`FromHeadId`）。退休但非前任家主的 `DirectKin` 不获得资格。SC-020 与其他场景不放宽。预览、导师列表、提交、年结和存档校验共用同一规则。
- 数据：`SC023SeclusionScenario` 固定千代五项 `10/12/18/11/13`、资质学问、有利环境，宗严固定学问专长；`EducationScenarioProvider` 路由 SC-020/SC-023 的年龄、资质、环境、初始值与专长，其他场景使用中性值。学年结算仍只发生在 12→1 月事务中。
- 存档：不新增字段、不升 schema，`saveFormatVersion` 保持 `8`、`rulesVersion` 保持 `4`。计划仍按既有 `mentorPersonId` 持久化；存档校验从既有 succession 编年史的 `fromHeadId` 派生前任家主证明。SC-023 可保留真实前任家主的失效计划并在年结按自由成长回退，但退休非前任家主的计划会被拒绝；SC-020 仍严格拒绝退休/失效导师。修正了既有回退结果与保留计划的一致性校验。
- 场景/UI：教育面板复用 SC-020 的 `Canvas/SC020EducationRoot`，不新建第二套 UI。新增事务式 `AddSC023Seclusion`，仅添加标题入口 `SC023SeclusionDemoButton` 并绑定 `TitleScreenController.seclusionDemoButton`；复用单 Undo 组、保存前验证、Undo 回滚、不重载场景。`AddM6TitleScreen`/`AddSC013TitleScenarioButtons` 的标题子节点计数已接受该可选入口，`FoundationSceneValidator` 已接入 SC-023 行。
- 验证边界：本 worker **未修改 `FoundationScene`**，未运行场景生成器、Validator、Unity 编译、EditMode、Console 或 Play Mode；独立源码复审 ACCEPT。所有编译/测试/场景/Play Mode 证据仍 pending。SC-023 标题入口和 Validator 行在场景生成前预期失败。用户 Play Test 与验收 pending。详见 `SC023_DEMO.md` 与 `docs/reports/sc023-implementation.md`。
- 工作区：代码/文档 checkpoint `a2c7a48` 已推送至 `origin/panyeung/sc023-seclusion`；尚未合并、未打 tag。`SampleScene.unity`、`Assets/1.unity`、`Assets/1.unity.meta`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 继续保留为本地用户 WIP，未纳入提交。

## SC-024 家训与个人抱负（2026-11-06 历史快照）

- 范围：M3 家训与抱负纵向切片——复用 SC-013 六人明智家与 `1560-04-01`，为家族提供跨代家训、为每任家主提供一项个人抱负、一次可见的家族议事改革，并把家训/抱负/活动事件路线写入存档 v9。**不是完整 M3/M01**；不含派系、忠诚度、家族投票、随机检定或新经济系统。
- 家训：`SecrecyFirst`（守秘为先）、`RighteousnessFirst`（义理至上）、`MeritFirst`（唯才是举）；新局默认义理至上，交接后保留。三个稳定议事事件 `event.sc024.creed-council.secrecy/righteousness/merit` 只按当前家训排程，维持/改革关系与家训变更在同一次提交中写入事件记录 + 两条关系记录；改革后旧家训版本不可提交。
- 抱负：`DevelopHousehold`（发展家业，3 个合格月，要求至少一条产业 `Operated` 且三类资源净和 > 0）、`PassDownTechnique`（传承家技，1 项选择后新完成的家族技艺）、`EducateHeir`（培养继承人，当前指定继承人五项正向成长累计 5）；只消费既有产业/研究/教育结果，不另跑月结；完成只写一条编年史，不发放额外资源或能力。
- 存档：schema v8→v9、`rulesVersion` 4→5、事件内容版本 3；新增 `familyDoctrine`（家训 + 按 head ID 排序的抱负记录与技艺快照）和 `activeEventScenarioId`；v8 迁移初始化为义理至上、无抱负、无 SC-024 路线，不补算历史。普通 SC-013 存档不获得 `sc024` 路线。
- 场景/UI：`FamilyDoctrineView`、`FamilyDoctrinePanel`、`SC024FamilyDoctrineScenario`、事务式 `AddSC024FamilyDoctrine` 与 `FoundationSceneValidator` 的 SC-024 行已实现。**HEAD 场景**（`git show HEAD:Assets/Scenes/FoundationScene.unity`）没有任何 `SC024`/`FamilyDoctrine` 匹配；**当前未提交工作区场景**已有 17 处匹配和 `Canvas/FamilyDoctrineRoot`、`Canvas/FamilyDoctrineOpenButton`（`FamilyDoctrinePanel` 组件）、`M6TitleRoot/SC024FamilyDoctrineDemoButton`。工作区场景接入早于本轮且不属于本轮编辑；`Logs/sc024-scene-integration-2026-11-05.txt` 的生成器成功与 SHA-256 是历史本地记录。场景接入要进入 `main` 仍需单独的集成提交与当前 Validator PASS。
- 证据：本轮在打开的 Unity 6000.6.6f1（PID 90052）中通过脚本刷新触发编译，`Logs/Editor.log` 记录 `*** Tundra build success`，该区间无 `error CS`；聚焦 EditMode 共 76/76 通过（`SC024M01RegressionTests` 11、`FamilyDoctrineStateTests` 20、`FamilyDoctrineSessionTests` 14、`FamilyDoctrinePanelTests` 12、`FamilyDoctrineSaveTests` 12、`M2FixedScenarioTests` 3、`M2StoryGateTests` 4），结果文件 `TestResults.xml` 的 `test-run` 为 `total="76" passed="76" failed="0"`，起始时间 `2026-11-07 00:29:32Z`。上述为本轮实际运行；历史 20/20、63/63、10/10、70/70 仍只是历史日志。本轮 Console 计数未在测试结束后单独截图记录；`Editor.log` 无 `error CS`，仅有 Unity AI Assistant `NoSubscription` 的既有追踪异常，不能当作本轮 Console 全零。Validator 与 Play Mode 均未运行。独立代理 Play Mode 记录见 `docs/reports/sc024-aid-ending-save-load-playtest-2026-11-06.md`，它不是用户 Play Test 或用户接受。
- 阻塞与状态：① HEAD 场景仍无 SC-024 对象，工作区已有未提交接入，场景独立验证与 Validator 待办；② `sc024` 路线退休只要求本路线家训议事，普通 SC-013 仍要求 pledge + 雨夜来客 + 旧客回访；③ SC-024 第四代子嗣 `person.sc024.nobumasa-heir`（明智 景继，开局 10 岁，信正之子）的指定、名册追加与“第四代”标签均以 `sc024` 路线为边界：家系数据存在但路线为 `sc013`/空时，`ExpectedHeir`、`SyncDesignation` 与 `BuildRoster` 保持普通 SC-013 规则；④ 新增回归 `Sc024_FamilyDataKeepsSc013RulesWhenTheRouteIsNotSc024` 覆盖上述边界，并验证 `sc024` 路线仍指定景继。③–④ 已随本轮 76/76 通过；当前 Validator、Play Mode 与用户验收仍待办。
- 工作区：原始文档轮次新增/更新 `SC024_DEMO.md`、`docs/reports/sc024-implementation.md` 与本节/M01 段落；M01 修正轮次修改 `M2FixedScenario.cs`、`SC024FamilyDoctrineScenario.cs`、`GameSession.cs`、`FamilyDoctrinePanelTests.cs`、`FamilyDoctrineSessionTests.cs` 与已跟踪的 `SC024M01RegressionTests.cs`，并同步更新上述文档。未修改 Unity 场景/预制体、`ProjectSettings`、`Library`、`Temp`；未运行 batchmode，未提交或推送。用户 Play Test 与明确验收仍待办；未创建最终 checkpoint、未合并、未打 tag。

## M02 每日行动与旅行基础（2026-11-06）

- 范围：在既有 `DailyActionState`、`GameSession` 日期与月结之上，补齐最小可玩 UI 接线；不新增 stat、经济、行动点或工作奖励，不实现 NPC 自主规划。
- 面板：新增 `DailyActionPanel`，按 `PersonId` 稳定顺序列出全部已登记人物，显示已知地点或 `旅途中（出发地→目的地）`、当前行动与 `已用/总天数（剩余）`。玩家选择人物后可输入正整数天数安排休息、训练或工作；旅行目的地仅在世界地图规则存在直达路线时可用，耗时由 `WorldMapRules` 决定。已有行动时通过 `TryReplanDailyAction` 原子重规划，并显示中断/重规划/拒绝原因；中断按钮提供明确原因与反馈。
- 时间：`WorldClockPanel.TryAdvanceOneDay` 改为调用 `GameSession.AdvanceActionDay`：月内推进一天；月末最后一天复用既有 `TryAdvanceToNextMonth` 恰好一次。既有「结束月份」按钮与事务保持不变，成功后同步刷新每日行动面板。
- 场景：新增事务式 `AddM02DailyActionPanel`（单一 Undo 组、保存前验证、异常 Undo 回滚、原对象快照保护），在当前已脏的工作区场景上通过授权 dirty-workspace 入口运行并保存 `FoundationScene`。新增 21 处 `M02DailyAction` 匹配、`Canvas/M02DailyActionRoot`、`Canvas/M02DailyActionOpenButton`、隐藏的 `M02DailyActionPanel` 及 `WorldClockPanel.dailyActionPanel` 绑定。`FoundationSceneValidator` 已增加 `DailyActionPanel` 查找、20 项序列化引用检查与 M02 结构校验。
- SC-024 保留：场景保存前后按对象名比对无删除；既有 17 处 `SC024`/`FamilyDoctrine` 匹配、`Canvas/FamilyDoctrineRoot`、`Canvas/FamilyDoctrineOpenButton`、`M6TitleRoot/SC024FamilyDoctrineDemoButton` 均保留。未修改 SC-024 的用户 WIP 文本。
- 证据：在 Unity 6000.6.6f1（PID 90052，`C:\Users\panye\My project`）中完成；最终 `*** Tundra build success`，无 `error CS`；聚焦 EditMode 44/44 通过（`Logs/m02-ui-focused.xml`，`M02DailyActionPanelTests` 7、`M02DailyActionTests` 11、`SC010TimePauseTests` 5、`AddSC004MonthTurnControlTests` 8、`IndustryMonthCrossingTests` 13）；`FoundationSceneValidator` 为 `RESULT: PASS; Errors: 0`（`Logs/SceneValidationReport.txt`）。Play Mode 实测六人名单、两日休息的 0/2→1/2→完成、清洲→热田途中→抵达，以及单槽存档读回未完成任务；退出后 Console 徽章为 0 logs / 0 warnings / 0 errors。详细步骤与存档恢复记录见 `docs/reports/m02-daily-action-foundation.md`。
- 状态边界（截至本节记录时）：M01 为 Go/No-Go pending；M02 每日行动切片已通过 lead Play Mode gate，仍待用户明确里程碑验收；工作区含 SC-024 WIP。本状态是 2026-11-06 历史快照，M01 已于 2026-11-07 由用户验收通过。

## SC-004–SC-009 已接受 checkpoint

- 提交：`c29d24a387ef73c5d3c5d80ea9148d7a6b45af54`。
- 分支：`feature/sc004-monthly-time` 与 `main` 均已推送至 `origin`。
- 标签：`demo-SC004-SC009` 已推送。
- Edit Mode：最新 `TestResults.xml` 报告 548/548 通过，0 失败、0 跳过。
- Play Mode：用户明确报告试玩验收。
- 独立只读代码审查：OpenCode2 为 ACCEPT。
- 编译、Console 红色错误计数和场景验证分别记录；本次没有单独取得新的编译记录或 Console 计数，不能推断其为零。

### 尚待补齐的场景验证

`FoundationScene.unity` 中 `EventChoiceFeedbackText` 仍序列化为旧中心位置 `y=-322`。运行时面板刷新会应用顶部反馈条布局，用户已验收该试玩表现；但本次没有从现有 Unity Editor 执行场景更新菜单，也没有运行 `FoundationSceneValidator`。因此场景验证仍待补做，Orca 的 SC-009 最终签核项保持 blocked。此项不撤销用户的试玩验收或已推送 checkpoint。

## 工作区保护

当前工作区仍有既存用户修改，包括 `SampleScene.unity`、旧方向文档、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/` 与 `nul`。这些内容不属于 SC-012；不得清理、暂存或覆盖。SC-012 代码、测试和文档按任务范围单独核验。

其他链接 worktree `C:\Users\panye\EU4-m3` 与 `C:\Users\panye\SengokuChronicle-M2` 也保留各自工作状态，不因当前路线更新而删除或改写。

## 历史方向资料

旧 `Sengoku Chronicle` 方向的 Phase 1/Phase 2 路线、M2–M7 演示、任务计划与设计稿保留为历史交付证据。其场景流程、里程碑命名、验收状态与旧方向决策不得当作《影之一族》当前规格；新实现先检查可复用性，再以 GDD v4.0 为准。
