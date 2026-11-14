# M08A 家族人口动态实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** 在实际月结中接入确定性自然死亡与 NPC 自主婚姻，使家族人口、继承、怀孕、编年史和存档一致。

**Architecture:** 纯 C# 规则先生成稳定计划；`GameSession.TryAdvanceToNextMonth` 在现有月结事务克隆中按出生、死亡、婚姻顺序应用计划。死亡、出生和婚姻共用已保存的 `WorldSimulation` 随机流，失败时恢复整个月结快照。

**Tech Stack:** Unity 6.6.6f1、C#、现有 Domain/Application/Persistence/uGUI、Unity EditMode Test Runner、Unity Pipeline bridge；不增加依赖。

**Spec:** [M08A 家族人口动态设计](../specs/2026-11-12-m08-demography-design.md)

> **状态（2026-11-14 lead 追补）：**设计稿已获用户批准，M08A 实现与既有 EditMode 回归证据不变。2026-11-14 的真实 SC-013 Play Mode 补充验证了自然死亡、HUD 人口下降、两条死亡编年史及名册“已故”标记，并观察到第二次自主婚姻；记录在 `Logs/verify/20261114-m08a-natural-death-playtest.md`。本轮没有使用存档槽、注入状态或保存场景；Editor 停止后 FoundationScene clean；succession sweep 返回 scenarios=2、sequences=680、steps=2504、violations=0、self-test=PASS (4/4 broken traces caught)。最后一次 Console ground-truth 样本为 0 errors/0 warnings/0 new logs，缓冲区保留 14 条此前日志。家主仍存活，故家主死亡继承未验证；也未观察到出生。配偶字段/未完成怀孕/RNG 的存档恢复、随机 seed 记录、Task 5 QA request、精确隔离文件的提交推送及用户最终验收仍未完成。此前 2026-11-13 路线报告中的存档哈希缺失问题保持不变。M08A 仍未完成，不能创建最终 checkpoint、合并 `main` 或打 tag。

## 全局约束

- 只在 `C:\Users\panye\My project` 的现有 `chore/pstack-verification` 工作树工作；不创建 Unity 项目、分支或 worktree。
- OpenCode2 实施全部 C# 改动；随后由独立 OpenCode worker 对同一任务只读复审；Mochi 负责集成与验收。
- 保留所有已有未提交/未跟踪 WIP；每次只提交能与旧改动精确隔离的已验证文件；任何隔离不清的共享文件不得整文件暂存。
- 保留 `.meta` 与 GUID；不改 `Library` 或 `Temp`，不安装 Unity 包，不使用付费服务/素材，不复制商业游戏内容。
- 使用当前已打开且 `-projectPath` 指向本项目的 Unity Editor；不对被该 Editor 占用的目录运行 batchmode。编译、EditMode、Console、场景验证、Play Mode 分开记录。
- 保持 M03 自主试点仍限正綱与信正；新生成配偶不得自动加入其试点集合。
- M08B 价格指数与长期模拟不属于本计划。M08A 完成不代表 M08 或整项目完成。
- 只有用户明确接受 M08A 可玩演示后，才创建最终里程碑 checkpoint、合并到 `main` 或 tag；验收前经验证的任务提交仍按项目规则推送当前分支。

## 复审重点

- **年度边界与随机性：**同一人在一个年界最多检定一次；稳定 ID 顺序与流消耗必须令相同存档/seed 可重放。由 Task 1 的纯规则测试覆盖。
- **出生与死亡同月：**到期分娩先应用，随后死亡；死亡者未到期的怀孕取消；新生儿不参与同一次年界死亡列表。由 Task 3 的月结测试覆盖。
- **玩家控制边界：**当前家主不会自动择偶；未成年人、性别未知者、有在世配偶者不参与；新生成配偶本年不再次参与。由 Task 1/2 测试覆盖。
- **死亡继承：**家主自然死亡只触发一次既有死亡继承流程；没有合法继承人时沿用既有危机/灭亡决策，不静默跳过。由 Task 3 的会话测试和 succession sweep 覆盖。
- **跨系统原子性与旧档：**写编年史失败不能残留死亡、婚姻、家族成员、子状态、怀孕变化或 RNG 消耗；v16 与迁移后存档仍可往返。由 Task 3/4 测试覆盖。

---

### Task 1：确定性年度死亡与婚姻规划规则

**文件：**
- 新建：`Assets/Scripts/Domain/FamilyLifeCycleRules.cs` 和 `.meta`
- 新建：`Assets/Tests/EditMode/FamilyLifeCycleRulesTests.cs` 和 `.meta`
- 修改：`Assets/Scripts/Domain/SuccessionState.cs` 仅在规则需要稳定的在世成员读取接口且现有 API 不足时

**接口：**
- `FamilyLifeCycleRules.PlanNaturalDeaths(SuccessionState state, GameDate currentDate, GameDate nextDate, DeterministicRandomStream random) -> IReadOnlyList<PersonId>`
- `FamilyLifeCycleRules.PlanNpcMarriages(SuccessionState state, GameDate currentDate, GameDate nextDate, DeterministicRandomStream random) -> IReadOnlyList<NpcMarriageProposal>`
- 年龄区间概率固定为设计稿中的 `0.5/0.5/1/4/12/25%`；婚姻概率 `15%`/合格人物/年。

- [x] 为年度死亡边界、6 个年龄段概率边界、稳定 ID 检定顺序、非年界不消耗 RNG 和同 seed 可重放写 EditMode 测试。
- [x] 为婚姻候选测试：仅 12 月到 1 月年界；排除当前家主、未成年、Unspecified、已有在世配偶和死亡成员；丧偶者可参与；婚姻不按生育年龄过滤。
- [x] 测试生成配偶当年不会再次参加检定，且同一输入总是生成相同 proposal 顺序和随机消耗。
- [x] 在 Domain 中添加最小不可变 proposal/result 类型与纯规则；不修改存档、场景或 UI。
- [x] 运行聚焦 `FamilyLifeCycleRulesTests`；检查源状态未被 Plan 修改。

### Task 2：稳定配偶身份与完整家族成员初始化

**文件：**
- 新建：`Assets/Scripts/Application/FamilyMarriageSettlement.cs` 和 `.meta`
- 修改：`Assets/Scripts/Application/FamilyBirthNaming.cs` 仅当能无损复用现有唯一 ID/名字 helper；否则在新 settlement 内加最小专用命名函数
- 修改：`Assets/Scripts/Domain/DailyActionState.cs`、`CharacterDevelopmentState.cs` 或 `SuccessionState.cs` 仅在现有添加成员 API 不足时
- 新建：`Assets/Tests/EditMode/FamilyMarriageSettlementTests.cs` 和 `.meta`

**接口：**
- `FamilyMarriageSettlement.Plan(...)` 将 Task 1 proposal 转为稳定人物 ID、姓名、相反性别、生年、配偶边和婚姻编年史事实；不修改输入状态。
- 应用结果复用现有人物、家庭成员、生命周期、成长和每日行动注册路径。不给新配偶创建 M03 自主试点，也不自动创建家主个人关系值。

- [x] 为确定性 ID、名字去重、性别相反、成年生年、±5 岁年龄差（成年边界可夹取）、已有 ID/name 冲突和 proposal 顺序写测试。
- [x] 为生成的人物写完整性测试：属于当前家庭、角色为 `Spouse`、正式 `KinshipType.Spouse` 边对称可读、生命周期存活且年龄正确、成长行与日行动行存在。
- [x] 验证生成配偶只通过下一次月结进入 M04B 怀孕候选；本月已经完成的 BirthSettlement 不因新婚配偶被重跑。
- [x] 实现纯命名/事实规划和克隆状态应用，不加入 UI 或新保存字段。
- [x] 运行 `FamilyMarriageSettlementTests` 与相关 `FamilyPregnancyRulesTests`。

### Task 3：把出生、年度死亡和自主婚姻并入原子月结

**文件：**
- 修改：`Assets/Scripts/Application/GameSession.cs`，或新增 `GameSession.M08.cs` partial 并让月结只调用单一 helper
- 修改：`Assets/Scripts/Application/GameSession.M07.cs` 仅在需要把现有死亡副作用提取成共享 helper 时
- 修改：`Assets/Scripts/Domain/FamilyLifeCycleRules.cs`（婚姻候选快照接口）
- 修改：`Assets/Scripts/Domain/FamilyPregnancyState.cs` 仅在需要按死亡孕育者原子取消已有 pregnancy 且当前 API 不足时
- 修改：`Assets/Scripts/Domain/HistoryLedger.cs`、`Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- 新建：`Assets/Tests/EditMode/GameSessionFamilyLifeCycleTests.cs` 和 `.meta`

**事务顺序：**
1. 在年界前从当前在世成员快照死亡候选；保持所有候选规则稳定排序。
2. 使用现有 M04B 计划和 WorldSimulation 流计划本月分娩/怀孕，先把到期出生应用到 succession 克隆。
3. 应用 Task 1 年度死亡结果；对死亡人物结束日行动、撤销产业任命、取消其未到期怀孕并追加死亡事实；若家主死亡，调用一次 `BeginDeathSuccession`。
4. **年界委托顺序（已批准含义）：**自然死亡在 1 月 1 日年界生效。月结模拟 `nextDate` 当日时，若执行人在本次 `naturalDeaths` 中且委托仍为 `InProgress`，先按既有 `Abandoned` 规则关闭，再推进该日——死亡优先，绝不产生任务结果、名望、任务历史或 `Missions` 抽取；12 月 31 日已终态的委托保持不变。该关闭若意外返回失败，月结事务整体失败，避免提交无法保存的进行中行。提交后仍对失去可执行执行人的进行中 M05 委托做同一 `Abandoned` 收尾，使同一日期存档立即可校验。
5. 若本月产生待处理继承决策，则跳过自主婚姻；否则把月结开始时冻结的婚姻候选快照应用到死亡后的克隆状态，新生儿不加入本轮死亡候选，新配偶不加入本轮婚姻候选，同一月结丧偶者本轮不参与。
6. 只在上述家庭计划和现有月结其它阶段全部成功后提交存档/日期/RNG/编年史状态；失败还原完整快照。

- [x] 写失败测试：月结非年界不做死亡/婚姻检定；年界到期出生先于死亡；孕育者死亡取消未到期 pregnancy；新生儿不被年界 mortality roll 误杀。
- [x] 写年界委托边界测试：执行人本次死亡且委托 1 月 1 日仍进行中时先按 `Abandoned` 关闭（无任务结果/报酬/名望/任务历史/`Missions` 抽取，同日存档可校验并往返）；12 月 31 日已终态的委托不被 1 月 1 日死亡改写；非执行人离世时同一到期委托仍正常结算并留下任务历史。
- [x] 写会话测试：家主死亡触发一次既有 succession；非家主死亡清理有效岗位；家主死亡且出现待决 succession 时不自动婚姻；零候选/危机仍能按既有决策继续。
- [x] 写婚姻会话测试：15% 命中后生成可见的正式配偶和记录；失败 roll 不新增人物；死者不再婚配；新配偶读档后保持相同 ID。
- [x] 为死亡/婚姻编年史准备失败注入；断言日期、全部人员/家庭/亲属、成长、日行动、产业、妊娠、RNG、编年史与其余月结快照逐项回滚。
- [x] 把 Task 1/2/出生事务的随机流消费顺序固定并测试失败后重试与无失败路径相同。
- [x] 运行 `GameSessionFamilyLifeCycleTests`、`GameSessionSaveTests`、`FamilyPregnancySessionTests`、`SuccessionSaveTests` 和 `GameSessionSuccessionTests`。

### Task 4：编年史、家族回顾与存档兼容

**文件：**
- 修改：`Assets/Scripts/Domain/FamilyChronicleEntry.cs`、`HistoryLedger.cs`（新增 append-only category/source 值，不改已有 enum 数字）
- 修改：`Assets/Scripts/Presentation/FamilyChroniclePanel.cs`
- 修改：`Assets/Scripts/Application/CampaignReviewView.cs`
- 修改：`Assets/Scripts/Presentation/FamilyRosterPanel.cs` 或 `FamilyTreeGraph.cs` 仅当动态节点/已故标签实测缺失
- 修改：`Assets/Tests/EditMode/FamilyChroniclePanelTests.cs`、`CampaignMilestoneTests.cs` 或新建最小专项测试
- 修改：`Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs` **仅当**实现发现现有人员/成员/生命周期/亲属/成长/行动字段无法容纳生成配偶或死亡事实

- [x] 测试婚姻与自然死亡记录在家族编年史按日期/追加序正确显示，Campaign Review「人物死亡与重要经历」只读出真实历史。
- [x] 测试家谱将生成配偶列为同代正式配偶、将死者显示为已故，并保留历史婚姻/亲子边。
- [x] 测试 v16 及一份现有旧版本迁移样例可保存、读回新生成配偶/死亡状态，随机流相同；不新增保存字段时 schema 仍为 v16，迁移链保持逐版本兼容。
- [x] 只有确实新增持久字段才升版本；若需要升版，为每条旧版本支路补缺省值、验证非法值、旧档回归和 v16→新版本迁移测试，并在提交前更新设计/计划说明。
- [x] 运行相关 `FamilyChroniclePanelTests`、`CampaignMilestoneTests`、`SaveDataMigratorTests`、`SaveDataValidatorTests`、`GameSessionSaveTests`。

### Task 5：项目记录和任务复审

**文件：**
- 修改：`GDD_v4.0.md`，在 M08/设计决策区记录用户批准的规则与日期
- 修改：`DECISIONS.md`，记录死亡率、NPC 婚姻策略、与 M04B 顺序及存档决策
- 修改：`PROJECT_STATE.md`，仅在证据产生后更新 M08A 状态
- 修改：`docs/reports/m08-campaign-review.md`，在现有 M08 报告中追加本切片验证，不另建重复报告
- 修改：`.opencode/skills/verify/features/README.md`，添加 M08A 实际 Play Mode 入口与证据文件索引

- [ ] 实施者只在本人负责的代码与测试上工作；完成后在同一 Orca 任务调用 `kanban_request_review(reviewer="qatester")`，报告确切变更文件、聚焦测试与证据。
- [x] QA 对具体缺陷返回同一任务修复，复审通过后再进入 lead sign-off；不另建平行 QA 卡。
- [x] lead 检查代码 review verdict 与实际 Editor 结果，分别记录编译、EditMode、Console、场景 Validator、Play Mode；任何一项未运行都明确标为未验证。
- [ ] 仅对精确隔离的已验证任务文件自动提交并推送当前分支；无法从混合 WIP 隔离的文件不提交，记录具体阻碍。

> **Task 5 未勾选项说明：**同一任务已有只读 review 并完成整改（见 `Logs/verify/20261112-161222-m08a-review-fixes.md`），但 `kanban_request_review` 的调用记录未落盘，故第 1 项仍未勾选。第 3 项已完成并记录于本次 Editor/Play Mode 报告。第 4 项尚未对 M08A 文件提交或推送，因为共享源码与其余混合 WIP 尚未精确隔离。

### Task 6：Unity Editor、场景和 Play Mode 验收

**文件：**
- 仅在 Validator 报告现有层级缺失且设计约束允许时修改 `Assets/Scenes/FoundationScene.unity` 和对应唯一场景生成器；不得新增 Canvas/root。
- 验证文档：`Logs/verify/<stamp>-m08a-demography.md`（Git-ignored）；如截图，仅用于即时目视并按项目规则立即删除。

- [x] Unity 工作前执行 `unity pipeline list`、`unity command editor_status`、`unity command console_status`、`unity command list_open_scenes`；核实唯一 Editor `-projectPath` 指向当前项目、场景是 `FoundationScene`、无编译错误、场景未 dirty。dirty 时停止，不覆盖场景。
- [x] 用当前 Editor 的 Pipeline 完成 `unity command recompile`，并读取 `console_status`；编译通过不代表运行时通过。
- [x] 按项目已有命令运行聚焦 EditMode：
  `unity command run_tests --mode editor --filter FamilyLifeCycleRulesTests --filter_type testName --timeout 900`
  对 `FamilyMarriageSettlementTests`、`GameSessionFamilyLifeCycleTests`、相关存档/回顾测试逐条运行；最终运行 `unity command run_tests --mode editor --timeout 900`，读取本轮新 XML 的 passed/failed/skipped/inconclusive。超时不算通过。
- [x] 运行 succession sweep：
  `unity command run_script --file .opencode/skills/succession-invariants/scripts/SuccessionSweep.cs --entry SuccessionSweep.Run`
  需要 `violations=0` 且 `self-test=PASS`。
- [x] 停止 Play Mode 时运行 `Tools/Sengoku Chronicle/Validate Foundation Scene`；要求 `Loaded: True; Dirty: False`、清单全 PASS、0 errors。若未改场景，仍验证现有家谱/编年史面板绑定。
- [x] 在既有 Editor 中经标题 UI 新开 SC-013，以真实月结推进到 1561、1562、1563 和 1564 年界。观察自主婚姻、自然死亡、家谱已故标记及两次家主死亡继承。第二次继承由明智 信正（26）接任；详见 `Logs/verify/20261114-m08a-natural-death-playtest.md`。
- [x] 经 SC-022 标题入口执行生育观察路线。1560-10 至 1561-12 的名册采样无可见怀孕状态；1562-01 家主与配偶均已故，路线未观察到出生。该结果记录观察边界，不证明生育规则成功。
- [ ] 在有效配偶存活时，通过真实月结观察一次怀孕与出生，并确认新生儿进入家谱、人口和编年史。当前两条固定路线均未观察到出生。
- [ ] 记录真实标题页路线的随机 seed。不要向运行中的会话注入 seed、日期或私有字段状态。
- [ ] 检查自动配偶 ID、性别、生年、婚姻、生命状态、未完成怀孕及 RNG 流的序列化和读档恢复。现有 UI 往返只验证了日期、人口、已故名册标记、继承人和编年史行。
- [x] 记录 Play Mode 的标题页入口、月结与继承 UI 读数、最终 Console 状态和 teardown。`editor_status` 为 `ready/stopped`，FoundationScene `dirty=false`，Console 0 errors / 0 warnings；最新 ground-truth 样本有 1 条 log。随机 seed 未记录。无截图、未保存场景。
- [x] Through the real map UI, save a fresh SC-013 route at 1562-01-01, advance one month, load, and confirm date, population, deceased roster markers, heir and lifecycle Chronicle rows. Restore both original slot files and verify exact SHA-256 matches; see `Logs/verify/20261114-m08a-natural-death-playtest.md`.
- [ ] 完成只读 OpenCode reviewer，修正可修复发现并同任务复审；编译、EditMode、Validator、Console、Play Mode 和用户最终验收分别报告。

> **Task 6 状态：**SC-013 的重复继承路线已通过真实 UI 验证两次家主死亡继承。SC-022 路线观察到家主与配偶在同一年度年界已故，并通过 UI 完成成年旁系继承选择；两条路线都未观察到出生。此前 SC-013 UI Save/Load 已验证日期、人口、已故标记、继承人和编年史投影，且恢复了原存档。随机 seed、出生运行时、完整配偶与怀孕/RNG 存档契约、Task 5 QA request 和用户最终验收仍未完成。本次没有重跑 EditMode 或 FoundationScene Validator。详见 `Logs/verify/20261114-m08a-natural-death-playtest.md`。

> **2026-11-14 后续更正：**同日另执行 UI Save/Load 往返并恢复原存档，证实日期、人口、死亡名册标记、指定继承人和相关编年史行可往返；不再将 Save/Load 整体标为未执行。自动配偶序列化字段、未完成怀孕、RNG 恢复仍未验证。无截图；Play Mode 后 Editor `ready/stopped`、`compilationFailed=false`、Console 0 errors/0 warnings，FoundationScene `dirty=false`。完整 Task 6 与用户最终验收仍未完成。

> **2026-11-14 后续补测：**单独一条 SC-013 重复继承路线通过真实 UI 连续完成两次家主交接，并显示继承编年史；本次在 `1564-01-01` 由明智 信正继任，未观察到出生。另启动 SC-022 生育观察路线，名册在 `1561-12-01` 前的采样没有怀孕状态；`1562-01-01` 家主与配偶均标为已故，继承 UI 暂停月结，选择成年旁系后 HUD 更新。该路线未观察到出生，也未检查死因编年史类别。两次测试均无截图、无场景保存、未访问存档；存档与备份哈希仍与测试前一致。Editor 停止后 `ready/stopped`、FoundationScene `dirty=false`、Console 0 errors/0 warnings（最新 ground-truth 有 1 条 log）。详见 `Logs/verify/20261114-m08a-natural-death-playtest.md`。家主死亡继承现已在 SC-013 Play Mode 验证；出生、随机 seed、自动配偶序列化字段及未完成怀孕/RNG 读档恢复、Task 5 QA request、用户最终验收仍待完成。M08A 不能创建最终 checkpoint、合并 `main` 或打 tag。

## 提交与验收门

- 每个可独立隔离的验证门只提交其已验证文件，立即推送 `origin/chore/pstack-verification`；不要把 M02–M08 混合 WIP 一起 stage。
- 编辑器/测试失败按根因修复并重跑对应门；全量 EditMode 运行若仍出现 M02 用户 WIP 相关旧失败，记录准确测试名和证据，不把历史结果冒充本轮结果。
- M08A 最终用户 Play Test/验收通过前，不创建最终 M08A checkpoint、不合并 `main`、不打 tag。用户接受后再执行整合流程。
