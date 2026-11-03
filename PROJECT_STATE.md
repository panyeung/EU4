# 《影之一族》项目状态

更新时间：2026-11-03
设计权威：[GDD_v4.0.md](GDD_v4.0.md)。本文件是当前执行状态入口；旧《Sengoku Chronicle》M1–M7 文档仅作为历史记录，不代表新方向进度。

## 当前方向

- 游戏：以家族兴衰、家主角色扮演与世代传承为核心的战国人生模拟。
- 工程：复用唯一项目 `C:\Users\panye\My project`，Unity `6000.6.6f1`，不另建项目。
- 规则与范围以 GDD v4.0 的 M0–M8 路线及 SC-001–SC-017 首批任务为准；不从旧 M1–M7 代号推断新方向进度。

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
| SC-013 M2 固定三代剧本 | 代码/内容/测试/文档已实现；场景接入与验证待现有 Editor | 未运行 updater、未保存 `FoundationScene.unity`、未运行 `FoundationSceneValidator`、未取得 Unity 编译/EditMode/Console/Play Mode 证据。静态类型检查 8/8 程序集 0 错误；逻辑 harness 30/30、33/33。详见 `SC013_DEMO.md` 与 `docs/reports/sc013-implementation.md`。 |
| SC-014 流亡少年与家系遗产 | 数据/文档切片已实现并经两轮 OpenCode2 只读审阅 ACCEPT；用户 Play Test 与验收待办 | 4 个事件节点、22 条中文文本；一次性 JSON 结构/引用检查完成。未运行 Unity、测试、场景验证、存读档或 Play Mode。完整链是可选内容，需要每次交接前额外结束一个月。详见 `SC014_DEMO.md` 与 `docs/reports/sc014-review-2.md`。 |
| SC-015 三代家书 | 数据/文档切片已实现；两轮 OpenCode2 独立只读审阅均 ACCEPT；Play Mode 分支 A/B/C/D 均到达预期第三代内容；用户验收待确认 | 7 个事件节点、10 个选项、27 条中文文本；静态 JSON 检查通过。现有 Unity 6.6 Editor 中运行 Play Mode；Console 停止时 0 errors / 0 warnings / 0 logs。EditMode、场景验证、存读档与独立编译证据未取得。详见 `SC015_DEMO.md` 与 `docs/reports/sc015-playtest.md`。 |
| SC-016 M2 委托旧约 | 数据/文档切片已实现；助手已按用户临时授权完成四条 Play Mode 路线，EditMode 全量通过；用户正式验收待办 | 7 个事件节点、10 个选项、27 条中文文本；静态 JSON 检查 10 组 0 错误。现有 Unity 6.6.6f1 Editor 的 EditMode 778/778 通过、0 失败/跳过；四条 Play Mode 路线均到达对应第三代结尾。测试后 Console 显示 0 errors、14 warnings、8 logs；未运行 `FoundationSceneValidator` 或存读档验证。没有保存场景或测试进度；用户验收待确认。详见 `SC016_DEMO.md` 与 `docs/reports/sc016-implementation.md`。 |
| SC-017 通用家谱展示 | 用户已验收（2026-11-03） | 只读、数据驱动、可滚动家谱；亲族/未连接亲族/家臣分区；typed 关系线；无六人/三代硬上限。最终修正后 Roslyn 8/8 程序集 0 错误，纯图/布局 harness 86/86；Codex fallback 顺序只读复审 ACCEPT，无 Critical/Important，留一条未覆盖空路由面板分支的轻微备注。用户接受本次演示；未提供 EditMode 数量、Console 计数或 Validator 报告文本。未提交或推送；当前分支还包含未验收的 SC-013–SC-016 WIP。详见 `SC017_DEMO.md` 与 `docs/reports/sc017-implementation.md`。 |

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
- 未完成：`FoundationScene.unity` updater/保存/validator、Unity 编译、EditMode 执行、Console 计数、Play Mode 与用户验收。`SC013_DEMO.md` 第 1 节给出精确菜单与验证步骤；在这些步骤完成前不得声称场景已接入。
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

## SC-016 M2 委托旧约（2026-11-03）

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
