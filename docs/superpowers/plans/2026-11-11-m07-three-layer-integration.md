# M07 三层玩法整合实施计划

**状态：** 2026-11-11 文档复审与修复复查通过；用户已明确批准完整 M07 规则方案并批准开始代码实施。代码、测试源与文本文档已写入当前工作树；2026-11-12 在既有 Unity Editor 取得本轮编译成功、全量 EditMode `1710/1710`、`FoundationSceneValidator` PASS（Errors 0）与 M05/M06 手动/委派实况 Play Mode，M06 战术面板由 lead 在既有 `FoundationScene` 生成并保存；最终用户里程碑验收仍待办。具体值见下文。

**目标：** 让家族管理、人物日常行动、M05 护送委托和 M06 潜入共用稳定人物身份、资格检查、五类任务结果、事务式结算与可追溯存档。

**参考：** [M07 设计稿](../specs/2026-11-11-m07-three-layer-integration-design.md)、[GDD v4.0 §8、M07、§19.8](../../../GDD_v4.0.md)。

## 已批准规则与实施范围

- 用户已确认：所有任务可手动或自动委派；两种操作方式产生相同奖励和人物后果；手动与自动使用同一组结果和结算入口。
- 两种操作沿用各任务已定义的流程；M07 不增加由模式本身触发的结果修正或专属奖励。相同任务结局必须产生相同效果，模式不得进入结果效果配置。
- 伤员不能参加体力或潜入任务，并在下一次月结后恢复，无论当次月结成功或失败。只有简报事先逐名披露的严重失败风险可以造成死亡。“月结后恢复”指月界事务提交且日期推进之后，即使产业结算报告失败也要恢复；若月结事务被拒绝或回滚、日期没有推进，则不算完成下一次月结，伤员状态保持不变。
- 家族名望和潜入隐蔽度分开保存；雇主信用和势力关系用稳定 ID 保存。
- 休息、训练、工作、旅行和产业任命继续使用 `DailyActionState` 与现有行动系统。
- 用户已批准：五种结果对应的名望变化为 `+3/+2/+1/-1/-3`，适用雇主信用/势力关系变化为 `+5/+2/+1/-3/-5`；名望与隐蔽度范围为 `0..100`，关系范围为 `-100..100`。越界变化按范围截断，历史记录仍保存实际前后值。
- lead 按用户授权确定 M06 可选准备：“不准备”冻结 `PreparationBonus=0`、额外成本 0；选择“侦察路线”则冻结 `PreparationBonus=10`、额外成本 1 物资。自动解析将准备加值加入评分；手动简报在现有起始位置/朝向上补出第二个巡逻航点 `(7,0)`，完整显示 `(7,1) → (7,0)` 环线。选择与成本在两种模式中相同，效用按操作方式呈现；结果奖励/后果配置不读取模式。固定开局有 12 物资、0 情报，侦察路线开局可用。选择和启动任务时不扣资源；无准备时启动需至少 2 物资，选侦察路线时至少 3 物资。对应的 2 或 3 物资仅在共享终局结果提交时扣除。取消尚未启动的准备选择不损失物资；任务一旦启动，物资只在终局结果提交时扣除，手动和自动结算同样承担所选准备成本。
- 用户已批准本计划列出的 M06 难度、检定阈值、警戒映射、隐蔽度变化、死亡概率及初始值。其余未在批准规则和结果表中列明的效果不加入 M07。

## 实施门槛与工作区安全

1. GitHub 复用调研与玩法规则审批均已完成。实现只使用本计划已批准的任务结果与效果，不扩展未列明规则。
2. 当前 `SaveData.CurrentRulesVersion` 为 8，schema 为 v15（M07 已实现）。`SaveDataMigrator` 对 v11 以上旧存档要求规则版本精确匹配；迁移只接受 schema v11–v14 / rules 7 到 v15 / rules 8 的窄范围升级，继续拒绝其他不支持的版本组合。
3. 2026-11-12 04:29 UTC 刷新：唯一 Unity Editor PID 126040 指向本项目，进程报告响应，Pipeline `/api/status` 为 `ready`；`/api/dialog` 无活动对话框，但 `/api/progress` 报告 `active=true` 且无进度详情。Editor 窗口及无截图可访问性树显示 `FoundationScene` 和未保存标记 `*`。即使将 Editor 前置，`editor_status`、`console_status`、`list_open_scenes` 仍各自 10 秒超时；`recompile_status` 与 `console` 也超时。项目 `Logs/Editor.log` 最后写入为 2026-11-11 17:51:14 -10:00。当时编译结果、Console 错误数及场景干净状态均未验证。当时禁止保存或生成场景；该限制已随门关闭解除，且该运行验证轮已由 lead 在既有 Editor 生成并保存 M06 战术面板。仍禁止开启第二个 Editor 或在该项目上运行 batchmode。该前置已由 2026-11-12 运行验证轮满足：同一 Editor 编译成功，全量 EditMode `1710/1710`，Validator PASS（Errors 0），M06 战术面板在既有 `FoundationScene` 生成并保存，并完成 M05 委派与 M06 手动/委派 Play Mode。场景仍含此前混合 WIP，见 [M07 运行验证报告](../../reports/m07-runtime-verification.md)。
4. 当前分支为 `chore/pstack-verification`。`FoundationScene.unity`、`GameSession.cs`、`SaveData*`、Validator 及 M05/M06 代码和测试已有未提交 WIP，均须保留。OpenCode2 只在当前工作树工作；不得创建分支、worktree 或 Unity project。场景已由 lead 在 2026-11-12 运行验证轮生成并保存；保存或提交时仍须把 M07 面板差异与既有混合 WIP 分开归属，不得把整份场景差异当作 M07。
5. OpenCode2 实施后由独立 OpenCode worker 只读复审。lead 负责在现有 Editor 中整合场景、运行验证和 Play Mode。技术门通过后只提交可证明属于 M07 且已验证的文件并推送当前分支；用户明确验收前不合并 `main` 或打 tag。

## 结果映射与结算合同

共用结果枚举/记录包括：完全成功、成功但有代价、部分成功、失败并安全撤离、严重失败。共用结算接收不可变的任务请求与结果；效果只取决于任务、结果和披露过的风险，不取决于手动/委派模式。各任务可以只产生适用的结果子集。

- **M05 既有映射：** `CommissionOutcome.Success` → 完全成功；`Partial` → 部分成功；`Failure` → 失败并安全撤离。保留现有属性检定与家训值：义理至上 +10、唯才是举 +4、守秘为先投入 2 资金并把失败转为部分成功；唯才是举完全成功额外奖励 6 资金。守秘投入、基础奖励、随机流和结算结果均纳入测试。
- **M05 操作方式：** 手动模式在委托到期时由玩家点击明确的“结算”操作；委派模式在到期时自动调用同一个解析与结算入口。两者复用同一到期日、属性、家训、随机抽值和结果效果。操作方式只改变交互，不改变记录：M05 终局任务历史与编年史统一使用委托到期日（开始日 + 固定 3 天执行窗口）作为规范日期；到期后延迟点击“结算”也写入该日期，时间推进不被阻止，也不新增玩法或模式修正。该规范日期政策由 `M07MissionIntegrationTests.M05DelayedManualClick_SettlesOnTheCommissionDueDate` 固定。当前“到期自动结算”不能充当手动路径的验收证据。委派行若在到期自动结算时失败，仍保持执行中；玩家可用同一共享结算重试，重试沿用冻结的运行 ID、开始日期和委派模式，并仍写入规范到期日，不产生第二次结算。公开 M05 流程无法制造非终止的结算失败（执行人失效会进入中止终态，其余失败需要损坏状态或故障注入），因此该重试由 `ShadowEscortCommissionPanelTests.DelegatedDueRowLeftByAFailedSettlement_CanRetryOnceOnTheDueDate` 以合法存档重建“到期失败后的执行中行”，并把读档日期设到到期日之后，以证明记录日期取自到期日而非当前日期；测试不声称触发了真实失败路径。
- **M06 手动终局映射：** M07 之前的 M06 代码只在合法带密报撤离时产生 `Succeeded`、主动撤退时产生 `Withdrawn`、被抓获时产生 `Failed`，没有警戒结果分支或部分完成目标。按用户批准的 M07 规则，无警戒撤离映射为完全成功；曾触发警戒后撤离映射为成功但有代价，并额外支付 1 物资；主动撤退映射为失败并安全撤离；被抓获映射为严重失败。该映射已由 `TacticalMissionResolutionRules.TryMapManual` 接入 `GameSession.CommitTacticalOutcome`。成功但有代价沿用成功奖励，额外物资代价适用于手动和自动的同一结果。阶段/结果既有枚举整数值保持不变；任务终结状态与共享任务结果分开存储。
- **M06 自动模式：** 使用与手动结果相同的统一结算和效果表；从被冻结的任务请求计算结果。队伍能力必须由出发成员的现有属性按固定规则汇总，任务情报读取启动时值，资源就绪由任务成本与起始资源快照计算，难度读取任务定义，随机值来自可恢复的 Missions 随机流。准备选择明确为“不准备”或“侦察路线”；对应准备加值为 0 或 +10、额外成本为 0 或 1 物资。自动模式把冻结加值计入准备评分；手动模式在选择侦察路线时显示额外航点。所选准备的成本只与现有 2 物资任务成本一起在终局结果提交时扣除；启动资源门槛和终局成本均从冻结请求的准备选择计算。所有终局类型和两种模式使用相同成本，准备选择不进入结果效果配置。
- **M06 自动检定：** 难度固定为 50。两名出发成员的忍术取算术平均并向下取整；冻结的家族情报资源按 1 点换 1 分计，夹在 0 至 10；准备加值为 0 或 10；随机值为 Missions 流产生的可重现 d100（1–100）。`score = floor((忍术A + 忍术B) / 2) + clamp(冻结情报, 0, 10) + 准备加值 + d100 - 50`。分数 `>=90` 为完全成功，`65–89` 为成功但有代价，`15–64` 为失败并安全撤离，`<15` 为严重失败。M06 首切片不生成部分成功。同一冻结请求和随机流必须得到相同结果。
- **旧枚举兼容：** M06 继续保留 `TacticalMissionStage` / `TacticalMissionOutcome` 的既有整数值。不要把五类共享结果强塞进旧字段或覆盖旧值；在新的终局历史记录中保存共享结果。

## 已批准数值与结果效果

2026-11-11，用户明确批准将完整方案写入本实施计划。M05 原有结算保持不变。M06 原有完全成功奖励为资金 +4、情报 +6，所有终局支付基础任务成本 2 物资；侦察路线另支付 1 物资。成功但有代价额外支付 1 物资。选定的准备成本和结果代价都在共享终局结算时一次扣除。

| 结果 | M05 既有结算 | M06 当前行为 / M07 拟议映射 | 名望/适用关系（已批准） | 其他 M07 效果 |
|---|---|---|---|---|
| 完全成功 | 资金 +14、物资 +4；唯才是举额外资金 +6 | 无警戒撤离；资金 +4、情报 +6；支付基础 2 物资及所选准备成本。 | 名望 +3；适用信用/势力 +5 | 隐蔽度 +3；不造成伤亡 |
| 成功但有代价 | M05 当前无此结果 | 警戒后撤离，或自动检定落入该档；沿用成功资源奖励，另支付 1 物资及所选准备成本。 | 名望 +2；适用信用/势力 +2 | 隐蔽度 +1；不额外造成伤亡 |
| 部分成功 | 资金 +7、物资 +1 | M06 当前没有部分目标，不适用。 | 名望 +1；适用信用/势力 +1 | M06 不生成此结果；隐蔽度不变 |
| 失败并安全撤离 | 无报酬，已扣投入不退 | M06 主动撤退或自动检定落入此档；支付基础 2 物资及所选准备成本。 | 名望 -1；适用信用/势力 -3 | 隐蔽度 -2；不造成伤亡 |
| 严重失败 | M05 当前不适用 | M06 被抓获或自动检定落入此档；支付基础 2 物资及所选准备成本。 | 名望 -3；适用信用/势力 -5 | 隐蔽度 -5；简报逐名列出的每名队员各自独立进行 10% 死亡检定，幸存队员受伤 |

新档初始值为名望 0、隐蔽度 50；未列入具体关系记录的雇主信用或势力关系默认为 0。拟用稳定关系 ID：`employer.shadow-escort-merchant`、`faction.intercepted-dispatch-target`。稳定 ID 只用于存档、日志和记录键；任务编年史与 M05 面板展示本地化标签（`尾张商队信用`、`截获密令目标势力`），不把原始 ID 呈现给玩家。名望/隐蔽度范围为 `0..100`，关系范围为 `-100..100`，变化超界时截断并记录实际前后值。若配置缺少某结果的适用效果，应返回配置错误，不得静默按零结算。

## 实施任务

### 1. Scout 复用调研

- 2026-11-11 已完成只读调研，完整摘要和来源见 `DECISIONS.md` 的 M07 条目。检查了 Moonforge RPG Engine、Unity Tactical AI Sandbox、SharedMeta 三个 GitHub 候选的 README、许可证、兼容性、维护信号和依赖，未下载、克隆、安装或复制代码。
- 决策：以项目现有 `ShadowEscortCommission`、`TacticalMissionState`、`DailyActionState`、`RandomStateManager`、SaveData 迁移和结算计划为基础自研 M07，不引入依赖；只借鉴可重现输入、稳定排序和回放校验概念。
- 完成标准：上面的研究限制已满足；实现前若需求或候选范围改变，再执行一次最多三个候选的定向复查。

### 2. 领域请求、操作入口与资格

- 在现有领域层创建共享任务请求、操作模式、结果和结果效果类型；避免重复创建 `MissionKind` 或虚构平行 `GameSessionState`。
- 将冻结请求显式包含：稳定 run/mission ID、模式、出发队伍、队长、属性汇总、任务情报、已选准备、资源快照/成本、难度、随机输入和事先披露的风险名单。校验范围、稳定 ID、参与者唯一性和请求完整性。
- 在 `GameSession.TryAcceptCommission`、`TryAssignCommission`、M05 到期/手动结算、`TryBeginTacticalMission`、M06 自动解析、日/月推进及任务人员的保存校验处使用同一资格规则。体力/潜入行动拒绝伤员；不冲突的行政、工作和产业任命不因伤病被拒绝。
- M05 的手动按钮只在委托到期后启用；手动和自动分支必须传入等价的冻结请求，进入同一个解析器。M06 保留现有双队员选择，并按 GDD §8.2 新增从队伍中指定队长的入口；M06 当前并没有队长。手动保留现有战术操作，自动使用被冻结的数值请求，两者以同一终局共享效果合同结算。
- 完成标准：不合格人员/伤员无法加入任务；同请求/同随机值可重现；相同任务结果在两种模式下的奖励、关系、伤病和编年效果逐字段相同。

### 3. 终局记录、一次结算和继承

- 继续使用项目已有 Plan/NextState/clone、事务快照及 `MonthTurnOutcome` 模式；新结算先验证并准备所有修改，再一次提交资源、状态、随机流、任务记录、继承状态和编年。任一步失败完整回滚，可用同一请求重试。
- `TacticalMissionState` 进行中只存在于 `GameSession`；当前读档/保存会拒绝正在进行的 M06。不要设计或测试 v14 活跃 M06 存档迁移。M07 新增的队长选择只在当前会话有效，并作为终局记录的一部分保存；旧 M06 存档没有队长，迁移不得声称恢复了它。
- 新增一个 M07 终局任务历史 DTO/列表，至少保存 run ID、任务 ID、日期、模式（`Manual`、`Delegated`；仅迁移行可为 `LegacyUnknown`）、共享结果、队长/参与者稳定 ID、冻结输入、实际效果、参与者生命状态前后值和编年 EntryId。参与者经历从这些记录按 PersonId 查询，避免每人另存一份重复历史。
- M05 新 run ID 在 `TryAssignCommission` 成功开始执行时分配，并写入 `ShadowEscortCommissionSaveData.runId`；委托进行中的每次保存/读档保留同一 ID，终局历史沿用它。M06 run ID 在任务开始时分配并留在会话态，终局提交时写入历史。结算入口拒绝重复 ID。新任务只在统一终局提交时追加一条结果编年；保留现有已发生的接受/任命/撤回等行动记录，不把它们误报成重复结果。
- v14 旧存档的 M05 `commission` 是唯一当前生命周期。对 `InProgress` 和已有终局结果（`Success`/`Partial`/`Failure`）分配 `legacy.m05.current`；`Accepted` 尚未开始执行，不分配 run ID。活动委托在升级后保存、读档和之后结算均沿用同一 ID。终局结果迁移时必须用旧 commission 已保存的 assignee、结果、资源、开始日期、roll 与反馈回填一条 M07 任务历史，并标记操作模式为 `LegacyUnknown`，不得推测旧模式。若旧编年有唯一 `mission.shadow-escort` / `settle` 行，沿用其 EntryId；若没有，按存档的最终 commission 状态和日期追加一个确定性的迁移编年行并链接 EntryId；若有多条匹配行则拒绝迁移，避免重复归属。`Abandoned` 没有共享结果，不伪造结局或终局结果行，保留已有行动编年。旧 M06 仅从明确标记为 M06 终局的 `ChronicleCategory.Mission` 行生成 `legacy.m06.<EntryId>`；不从自由文本推断人员、队长或风险。
- 严重失败死亡若涉及现任家主，必须原子进入现有继承流程：有多名成年候选时进入玩家选继承人的阶段；未成年继承人要求摄政；无合法候选时进入已有 `CrisisDecision`。为这些阶段定义并测试 `headPersonId`、角色和存活状态不变量；只对指定阶段作窄范围 SaveDataValidator 放宽，危机解决前阻止要求存活家主的操作/月推进。不得自动挑选多个成年继承人中的一个。
- 完成标准：资源、人物/继承、随机流、任务历史及编年故障注入全回滚；重复提交无二次支付/伤害/关系变化/编年；家主死亡、成年继承选择、幼主摄政、危机状态均能保存并恢复。

### 4. 存档与精确迁移

- 新增 M07 存档字段：任务终局历史数组、家族名望、M06 隐蔽度、按稳定 ID 排序的雇主信用与势力关系，以及 `ShadowEscortCommissionSaveData.runId`。M05 已开始执行的委托必须有 runId；未接取或已接取但未指派的行允许为空。M06 任务运行数据仅保存会话中的冻结请求和终局记录，不保存进行中的战术局面。
- 规则版本 8 已实现，schema 从 v14 升到 v15。对旧迁移链 v1–v10 保持现状；只允许 schema v11–v14 且 rulesVersion 为 7 的已知当前载荷迁移到 v15/rules 8。schema v11–v14 中其他 rulesVersion 继续拒绝；当前 v15 校验只接受 rules 8。保留事件内容版本和已有必需字段检查。
- v14 活跃 M05 尚无保存的 roll：不扩展成“未结算但已掷骰”的非法状态。到期时在一次结算事务内抽值并同时提交结果；事务失败则恢复 Missions 随机流，使重试复用同一逻辑输入。只有项目现有保存约束允许时才为终局历史保存抽值。
- 新档初始名望为 0、隐蔽度为 50；关系记录缺省为 0。迁移校验拒绝空/重复稳定 ID、非法枚举（`LegacyUnknown` 只用于旧版导入记录）、重复 run ID、无效人物引用、越界值和不可能的死亡/继承组合。迁移失败不得覆盖原存档。
- 完成标准：v1–v15 迁移矩阵、rules v7/v8 正反例、v14 M05 未开始/进行中/成功/部分成功/失败/放弃迁移、进行中存读档 runId 稳定、终局历史/编年回填唯一、M06 终局编年迁移、家主死亡阶段和读写往返通过；逐字段确认旧状态保留且结算不重放。

### 5. 面板、构造器调用者与场景

- 更新 M05/M06 面板显示模式、队长/队伍、成本、行动冲突、五种结果说明、风险披露名单、队伍/准备输入及实际前后值；只显示任务实际改变的字段。
- 为新增 DTO 和 `SuccessionPersonLife`/任务角色字段检索并更新所有构造器、`M2FixedScenario`、SC020–SC024 场景数据、capture/clone/restore、Validator、面板、测试 fixture 与 JSON 示例。新增 `.meta` 依 Unity 规则生成并保留既有 GUID。
- OpenCode2 可改生成器代码和 Validator 测试；只有 Editor 与场景状态已刷新、现有 `FoundationScene` WIP 可归属且干净时，lead 才在该 Editor 中运行生成器。保存前检查 Undo group 与 Validator；不得用生成器覆盖当前未归属的场景差异。
- 更新 `GDD_v4.0.md` §8.3/§8.4/§19.8、`.opencode/skills/verify/features/tactical-mission.md`、M05/M06 功能说明、`docs/PLAYTEST_GUIDES.md`、`PROJECT_STATE.md`、`DECISIONS.md` 和 `docs/superpowers/specs/2026-11-10-m06-tactical-infiltration.md`；明确 M05/M06 旧字段兼容与五结果的新映射。M06 设计稿的成本说明已随 M07 实现更新为“基础 2 物资 + 可选侦察 1 物资 + 成功但有代价 1 物资”，不再只写 2 物资。
- 完成标准：Validator 对新增控件和遮挡通过；场景集成前后差异均能归属本任务；失败时不保存场景并保留用户 WIP。

### 6. 测试、Editor 与 Play Mode

- 使用当前实际存在的测试类：`M07MissionIntegrationTests`（M07 共享结算、冻结输入一致性、到期日政策、回滚快照合同）、`GameSessionMissionTests` 与 `M05CommissionSessionTests`（两者都在 `GameSessionMissionTests.cs`；M05 会话级放弃）、`M06TacticalMissionSessionTests`（在 `GameSessionTacticalMissionTests.cs`）、`ShadowEscortCommissionTests`、`ShadowEscortCommissionPanelTests`、`TacticalMissionRulesTests`、`TacticalMissionPanelTests`、`SaveDataMigratorTests`、`SaveDataValidatorTests`、`GameSessionSaveTests`、`SuccessionSaveTests`、`GameSessionSuccessionTests`、`SC019SuccessionIndustryTests`、`SaveLoadPanelTests`、`SaveLoadPanelTacticalMissionTests`、`IndustryServiceTests`、`FamilyChronicleSaveTests`、`AddM5ShadowEscortCommissionPanelTests`、`AddM06TacticalMissionPanelTests`、`AddM6TitleScreenTests`。
- Pipeline `run_tests` 的过滤类型只用 `testName`、`assembly` 或 `category`；不支持 `regex`。每条聚焦命令使用一个已存在的过滤器，例如 `unity command run_tests --mode editor --filter GameSessionMissionTests --filter_type testName --timeout 900`，其它套件逐条同样运行；全量使用 `unity command run_tests --mode editor --timeout 900`。读取本次新 XML 的 passed/failed/skipped/inconclusive；Pipeline 超时不算通过。
- Unity 验证使用唯一现有 Editor 的 Pipeline/Test Runner；禁止对其项目运行 batchmode。分别记录编译、EditMode 数量、Console 当前错误、Validator、Play Mode 和存读档，不以旧日志/XML代表当前通过。
- Play Mode 覆盖：M05 手动按钮与委派自动到期用相同冻结输入得到相同结果效果；M06 无警戒撤离、有警戒撤离、主动撤退、抓获；风险先披露；M06 进行中禁止保存/读档；终局保存/读档后 run 不重结算；伤员在下一次已提交且日期推进的月结后恢复；当次产业结算报告失败但月结已提交时仍恢复；月结被拒绝或回滚、日期未推进时不恢复；成年继承选择、幼主摄政、危机存读档；继承后原 PersonId 的任务经历仍归原人。
- 完成标准：编译通过、列明的 EditMode 通过、FoundationScene Validator PASS、Play Mode 每条路径有实际证据、结束时 Console 当前 errors 为 0、终局记录与存读档通过。技术签收不等于用户最终里程碑验收。

## 预期改动面

根据实现前的调用者检索，预计涉及 `Assets/Scripts/Domain/{SuccessionState,SuccessionService,ShadowEscortCommission,TacticalMissionState,FamilyChronicleEntry}.cs`、`Assets/Scripts/Application/GameSession.cs`、`Assets/Scripts/Persistence/{SaveData,SaveDataValidator,SaveDataMigrator}.cs`、M05/M06 Presentation 与 Editor 生成器、对应 `.meta`、本计划列出的 EditMode 测试及受影响文档。不得假定这些文件的当前脏差异属于本任务；先记录实现前基线并逐路径核对所有调用者、迁移、capture/restore、clone、Validator、UI 和 fixtures。

## 验证结果（2026-11-12）

- 编译：2026-11-12 运行验证轮的编译成功（既有 Editor PID `126040`，Unity `6000.6.6f1`，项目路径正确）。
- EditMode：全量 `1710/1710` passed；聚焦合计 `GameSessionMissionTests` 46/46、`M06TacticalMissionSessionTests` 19/19、`M07MissionIntegrationTests` 34/34、`AddM06TacticalMissionPanelTests` 48/48、`SaveDataMigratorTests` 25/25。最后的聚焦 XML 已被 `SaveDataMigratorTests` 覆盖，不存 1710 条结果。
- 场景：M06 战术面板已在既有 `FoundationScene` 生成并保存；`FoundationSceneValidator` 为 `PASS`、`Errors 0`。场景保留此前混合 WIP，差异不能整体归入 M07。
- Play Mode：M05 委派路线（新内存 SC-013 局，清洲 1560-04-01 接取尾张商队护送委托，指派千恵、默认委派、3 天，推进到 1560-04-04）结算为部分成功（总分 42/掷值 32、能力小计 0、家训 `+10`、`+7` 资金/`+1` 物资、名望 0 → 1、隐蔽度 50 → 50、尾张商队信用 0 → 1，编年史 `自动委派`，未存读档；不建立手动/委派等价或确定性重复）；M06 委派为失败并安全撤离（`SafeFailure`；`-3` 物资、隐蔽度 `-2`、势力关系 `-3`，无伤病/死亡；准备/队长未记录，`-3` 与侦察准备一致但面板默认不准备，不当作已确认输入），M06 手动战术输入后撤退结算为 `SafeFailure`、无伤病/死亡（未记录准备/队长与逐项效果，不与委派比较输入、物资成本或全部效果）；较早路线覆盖活动 M05 与终局 M05/M06 存读档；本条 M05 委派路线未做存读档；该主路线记录未捕获 M06 的冻结准备/队长选择与名望变化，两条补充路线已捕获这些输入与名望截断结果。
- Console：EditMode 的两条预期负向路径错误日志记录后清空，之后运行时为 `0` errors / `0` warnings / `0` logs。
- 补充 Play Mode（同日单独路线，`Logs/verify/20261112-1156-m07-severe-failure-injury-recovery.md`）：新内存 SC-013 局选择千恵 + 佐近、自动委派、侦察路线、队长佐近；M06 立即结算为严重失败，名望 0 → 0（`-3` 被 0 下限截断）、隐蔽度 50 → 45、势力关系 0 → `-5`、物资 12 → 9（含侦察准备 1），两名队员均负伤且无人死亡；1560-05-01 月结后二人在 M05 护送委托面板显示 `可指派`，覆盖月结伤病恢复。该路线不验证死亡检定、死亡后继承/摄政、其他结局、M05 结算、存读档或视觉。
- M06 手动补充尝试（同日文本记录，未到达目标分支）：千恵 + 佐近、亲自执行、不准备、队长千恵；两人从 `(0,0)`/`(1,0)` 出发，佐近向东疾行两次；第 2 回合巡逻进入 `Investigating`，一次 `等待` 后第 3 回合守卫转 `Alert` 并在 `(5,0)` 抓获佐近，路线立即结算为严重失败：名望 0 → 0（截断）、隐蔽度 50 → 45、势力关系 0 → `-5`、物资 12 → 10（仅基础 2）、资金/情报不变，两人负伤、无人死亡；未做存读档或截图。该尝试未到达“警戒后撤离 → 成功但有代价”分支，不得据此声明该分支通过；该分支已由随后的带密报撤离路线单独验证。
- M06 手动带密报撤离（同日文本记录，警戒后撤离 → 成功但有代价）：清洲 1560-04-01，千恵 + 佐近、亲自执行、不准备、队长千恵；佐近安静向北 5 步到 `(1,5)`，向东到 `(4,5)`、向南到 `(4,4)`、向东到 `(5,4)`，在掩体遮挡下取得密报；随后向北到 `(5,5)`，第 12 回合固定守卫在 `(6,4)` 发现并触发 `Alert`，任务仍为 `InProgress`；带报向西疾行两次到 `(1,5)`、向南疾行两次到 `(1,1)`、再向南步行到 `(1,0)`，千恵留在 `(0,0)`；第 18 回合撤离成功，结果为 `CostlySuccess` / 成功但有代价（`+4` 资金、`-3` 物资（基础 2 + 警戒代价 1）、`+6` 情报；名望 0 → 2、隐蔽度 50 → 51、目标势力关系 0 → 2；两人无伤存活），编年史标注手动、开始日期 1560-04-01、回合 18 及逐项成本/奖励；未存读档/截图。该路线不同于已记录的手动撤退（`SafeFailure`）路线。
- M06 手动无警戒带密报撤离（同日文本记录，完全成功）：清洲 1560-04-01，千恵 + 佐近、亲自执行、不准备、队长千恵；佐近向北到 `(1,5)`、向东经 `(2,5)` 到 `(4,5)`、向南到 `(4,4)`、向东到 `(5,4)`，在隐蔽状态下取得密报，守卫全程 `Patrol`；随后向西到 `(2,4)`，向南经 `(2,3)`、`(2,2)`、`(2,1)` 到 `(2,0)`，再向西到 `(1,0)`，千恵留在 `(0,0)`；第 20 回合撤离成功，结果为 `FullSuccess` / 完全成功（`+4` 资金、`-2` 物资、`+6` 情报；名望 0 → 3、隐蔽度 50 → 53、目标势力关系 0 → 5；两人存活且无伤），编年史标注手动、开始日期 1560-04-01、回合 20 及逐项效果；未存读档/截图。该路线与警戒后带密报撤离、手动撤退（`SafeFailure`）及严重失败路线独立。
- M05 手动无存读档路线（同日 03:06 独立路线，`Logs/verify/20261112-0306-m05-manual.md`，本机 Git 忽略）：Manual 选中，接取一次扣 4 资金，指派 明智 千恵；04-02 为 1/3 天、04-03 为 2/3 天、04-04 到期为 3/3 天且保持未结算并显示可点击的 `结算委托`，点击一次结算为 Partial（42/32、`+7` 资金 / `+1` 物资、名望 0 → 1、隐蔽度 50 → 50、商队信用 0 → 1，编年史 `亲自执行`）；04-05 后单条结果、编年史与资源不变。该路线未做存读档/截图/EditMode，Editor 停止后场景 clean、Console `0/0/0`；只记录本条，不建立整体手动/委派等价、确定性或 M07 整体验收。
- M05 手动终局存档/读档（同日独立路线，`Logs/verify/20261112-1324-m07-m05-save-load.md`，本机 Git 忽略）：Manual 选中的终局 Partial 经 UI `存档` 后推进到 04-05，再 `读档` 恢复 04-04、同一终局委托/分数/资源（33 资金 / 13 物资 / 0 情报）/名望/隐蔽度/关系与单条 04-04 编年史；只读 JSON 显示 Manual、settled/Partial、一条 M07 任务历史、`familyStanding` 与编年史。既有用户存档与 `.bak` 已按原 SHA-256 还原。该路线不验证损坏恢复、旧 schema 迁移、保存失败保留旧档、读档后二次结算或 EditMode，不代表 M07 整体验收。
- 原主运行轮未覆盖 M05 完全成功；该结局后来在 2026-11-12 独立委派 Play Mode 路线中通过，手动完全成功仍未覆盖。仍未覆盖：M06 死亡检定与死亡本身（补充路线只观察到负伤）、死亡后的继承选人/摄政/危机、M05 中止及 M06 候选列表滚动。以上不构成 M00–M08 全量验证或最终里程碑验收。详见 [M07 运行验证报告](../../reports/m07-runtime-verification.md)。

## 审批与交接

本计划已通过初始独立文档复审和定向修复复查；用户已批准本计划列出的完整 M07 规则方案。实施前所需的编译、Console 与 FoundationScene 健康状态已于 2026-11-12 在同一 Editor 刷新并满足。实施完成后走同任务 QATester 复审及独立只读代码复审。lead 负责场景技术门、Play Mode 与验证记录。用户保留最终 M07 里程碑验收权。
