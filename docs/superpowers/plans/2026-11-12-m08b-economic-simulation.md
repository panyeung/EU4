# M08B 多商品经济与长期模拟实现计划

> **给编码 agent：**必须按 `superpowers:subagent-driven-development` 或 `superpowers:executing-plans` 逐任务实施。步骤用 checkbox 跟踪。

**目标：**实现粮食与生产物资的月度供需、价格与人口消费，并使用同一套真实 `GameSession` 规则完成 GDD §14.2 的长期模拟指标。

**架构：**在现有 `IndustrySettlement` 和 `TryAdvanceToNextMonth` 事务中加入纯 C# 市场结算，存档新增一版市场状态；复用 SC-019 产业面板展示市场结果；用 Editor 内的分批模拟入口驱动公开的新局入口，不另建模拟经济模型。

**技术栈：**Unity 6.3 / C# / Unity Pipeline / NUnit EditMode；不安装 package，不依赖 .NET SDK。

**设计：**[2026-11-12-m08b-economic-simulation-design.md](../specs/2026-11-12-m08b-economic-simulation-design.md)

**状态：**用户已批准本计划（2026-11-12），开始交 OpenCode2 实施。

**执行与审查：**OpenCode2 在当前工作树独占实现写入；实现完成后由独立 OpenCode worker 对本任务变更作只读复审。Lead 负责修复派发、当前 Editor 验证及最终签收；复审与实现不并行写同一工作树。

## 全局约束

- 商品 ID 固定为 `market.food`、`market.materials`；资金为货币，情报不交易。
- 价格指数基准 1000‰、合法范围 100..100000‰、每月变动范围 -100..+100‰；使用 `long` 中间运算和 half-up 舍入。
- 粮食月需求为 `ceil(月初在世家族人数/5)`；价格账单和库存不足均不得使资金/库存为负，也不得产生债务。
- 粮食短缺每满 3 个连续月使下次 M08A 年界死亡概率增加 50‰，上限 +250‰；有一个完整满足需求的月份即清零。
- Farm 保留现有资金 +2、物资产出 4、物资 upkeep 1，另产粮 4；Trading House 保持资金 upkeep 6、物资 upkeep 2、资金产出 9、情报 +1。
- 存档 schema 从 v16 升到 v17；v16 迁移只从既有物资库存拨出至多一个月需求粮食，库存总量不增加。
- 所有经济效果属于同一月结事务；失败时日期、继承/人口、产业、资金、库存、价格、连续短缺、随机状态和编年史完整回滚。
- 长期模拟从 `GameSession.InitNewGame(seed)` 的真实 SC-013 开局，事件用稳定顺序选择有效选项。模拟器只在模拟范围内调用现有 `GameSession` 继承 API。优先使用有效指定继承人，否则选稳定 ID 最小的合法继承人；摄政按稳定 ID 选择最小的合法候选人。无合法继承人时保留现有停机分类。不得改变玩家实际继承流程，也不得丢弃灭族或无法处理事件的种子。
- 只在当前工作树实施；保留既有 WIP。不得改写/重排用户已有 `.unity` 场景、Prefab、ProjectSettings；复用 `IndustryRoot` 现有 `resourceText` 和 `reportText`，不新增场景对象。
- 不运行 BatchMode、不启动第二个 Unity Editor。每个验证门通过后，只提交该任务明确拥有且刚验证的文件，并立即推送当前分支；若无法精确隔离，保留未提交并报告，不混入无关 WIP。不得合并或打 tag，等待用户最终里程碑验收。

## 验证失败判据

- Unity `run_tests` 非零退出、结果 XML 缺失或解析失败、`result` 不是 `Passed`、`failed > 0` 或 `total = 0`，均视为该验证门失败；不得只凭 worker 摘要报告通过。
- 每轮测试后单独读取当前 Editor 的编译状态及 `console_status`，记录编译、EditMode、Console 三类证据。Console 错误数不为 0 则不得进入该门的提交/推送步骤。
- 场景验证读取 `Logs/SceneValidationReport.txt`，必须明确包含 `RESULT: PASS; Errors: 0`；不得以旧报告或测试通过代替。
- Play Mode 只以当前目标 Editor 中真实开始的 SC-013 运行、可见 headline loop 与实际保存/载入结果作为证据；编译或 EditMode 通过不代表 Play Mode 通过。

## 审查重点

- 0 粮食、0 资金、0 人口、需求与供给相等时，无负数、除零或虚构消费；归 Task 1 测试。
- 价格 100/100000‰、单月变化到 ±100‰ 边界及舍入不溢出；归 Task 1 测试。
- 月初人口快照、同月出生/死亡、每月结算重复调用及任意中途失败保持已批准顺序和原子回滚；归 Task 2 测试。
- v16 存档按当前家族人数转换粮食后保留其余字段；非法商品/价格/库存不得覆盖运行中存档；归 Task 2 测试。
- 长模拟的事件自动决策、灭族、无合法继承人、超时和日期越界分别保留结果，不得当成功样本跳过；归 Task 4 测试。

---

### Task 1：领域市场与产业结算

**文件：**
- 新建：`Assets/Scripts/Domain/MarketState.cs`、`MarketSettlement.cs`、`MarketMonthReport.cs`
- 修改：`Assets/Scripts/Domain/IndustryCatalog.cs`、`IndustrySettlement.cs`
- 测试：`Assets/Tests/EditMode/MarketSettlementTests.cs`、`IndustrySettlementTests.cs`

**接口：**
- `MarketState` 负责粮食库存、粮食/物资价格指数、已结算月、连续短缺月数及近 12 月综合指数；物资库存仍是 `IndustryResources.Supplies`，不复制。
- `MarketSettlement.Plan(MarketState market, IndustryResources resources, SuccessionState succession, IndustryMonthReport industryReport, GameDate closedMonth)` 返回 `MarketSettlementResult`，含下一市场状态、下一资源值与只读 `MarketMonthReport`；结果 DTO 与 `MarketSettlement` 同文件，避免多拆一个文件。
- 当前 `IndustrySettlement.Plan(state, succession, closedMonth)` 无价格参数；增加一个材料价格指数重载，并保留现有三参数入口按基准指数 1000‰ 委派，避免改坏现存调用点。扩展 `IndustryDefinition`/`IndustryLineResult` 暴露 Farm 粮食产量 4，其他已批准数值保持不变。

- [ ] **步骤 1：写价格和人口交易失败测试**，覆盖供需相等、零供给、零需求、短缺/过剩、100‰/100000‰ 上下限、±100‰ 月变动限额、half-up 舍入，以及 0 人/1–5 人/6 人需求量。断言每个输入库存与资源未被原地修改。
- [ ] **步骤 2：运行新领域测试确认失败**

运行：`unity command run_tests --mode editor --filter MarketSettlementTests --filter_type testName --timeout 900`

预期：新测试因市场规则/类型尚未实现而失败。若引用尚不存在的公开 API 使 Unity 编译失败，编译诊断必须只指向本任务新测试所引用的缺失 API；此阶段不要求 EditMode XML。API 可用后，测试必须以行为断言失败而非编译失败。任何无关编译错误都要先停止并记录。

- [ ] **步骤 3：实现 MarketState、MarketSettlement、MarketMonthReport**，价格公式、单月限额和稳定商品顺序按设计稿；不足需求按库存/可负担金额的最小值成交，粮食消费和成本计算以整数 half-up，不修改传入状态。
- [ ] **步骤 4：写产业材料成本和粮食产出测试**，验证固定 upkeep 不变、材料成本公式、资金或材料不足整体停产，Farm 每月粮食毛产出恰为 4，未运作时不产粮。
- [ ] **步骤 5：修改 IndustryCatalog/IndustrySettlement 并运行领域测试**

运行：`unity command run_tests --mode editor --filter MarketSettlementTests --filter_type testName --timeout 900`，再运行 `unity command run_tests --mode editor --filter IndustrySettlementTests --filter_type testName --timeout 900`。

预期：两套 XML 均 `result=Passed`、`failed=0`、`total>0`；SC-019 既有资金/物资/情报输出数值测试保持通过。任一 XML 缺失/解析失败或计数不符即失败，不进入提交/推送门。

### Task 2：月结事务、M08A 食物压力与 v17 存档迁移

**文件：**
- 新建：`Assets/Scripts/Application/GameSession.M08B.cs`、`Assets/Scripts/Persistence/MarketSaveData.cs`
- 修改：`Assets/Scripts/Application/GameSession.cs`、`Assets/Scripts/Application/GameSession.M08A.cs`、`Assets/Scripts/Domain/FamilyLifeCycleRules.cs`、`Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs`
- 测试：`Assets/Tests/EditMode/GameSessionMarketTests.cs`、`SaveDataMigratorTests.cs`、`GameSessionSaveTests.cs`、`GameSessionFamilyLifeCycleTests.cs`

**接口：**
- `GameSession.Market` 暴露只读市场状态/报告；`CaptureSave`/`TryApplySave` 读写并校验 `MarketSaveData`。
- `FamilyLifeCycleRules.PlanNaturalDeaths(..., int mortalityBonusPermille)` 在原年龄概率上增加市场状态给出的修正，保留原有每人一次 `WorldSimulation` 随机消耗顺序。
- `TryAdvanceToNextMonth` 依次使用月初人口快照、上一月价格、产业结算、本月人口消费、价格更新、年界出生/死亡/婚姻；全部市场字段加入已有 snapshot/rollback。
- 当前 `SaveDataMigrator` 的现代版本分支显式接纳 v10–v15，并单独接纳当前 schema v16；升至 v17 时必须把 v16 纳入可迁移版本分支，保留 v16 原 rulesVersion 校验，再只在迁移副本上生成 v17 市场数据。测试必须覆盖 v16→v17，不能只测试新档往返。
- v17 根字段 `market` 必须在 `SaveDataPresenceProbe`/`SaveDataValidator` 中成为必需块；v16 及更早的原始 JSON 若已含 `market`，按现有未来字段保护规则拒绝迁移，不能静默丢弃。扩展 `SaveDataMigrator.Clone` 深拷贝 market 状态，并覆盖 v17 往返与 market 非法/缺失的拒绝用例。

- [ ] **步骤 1：写 session 与迁移失败测试**，覆盖真实 SC-013 月推进、食物产出/消费、资金不足、市场短缺连续数、三个月短缺死亡加值、短缺恢复、月结失败后完整回滚、重放一致；保存/载入 v17 往返。
- [ ] **步骤 2：运行相关 EditMode 测试确认缺失失败**

运行：`unity command run_tests --mode editor --filter GameSessionMarketTests --filter_type testName --timeout 900`。

预期：新行为测试因未实现而失败；现有测试仍可运行。核验 XML 与编译状态，若未能明确确认失败由缺失行为导致则停止并记录。

- [ ] **步骤 3：实现 GameSession 市场接入**，新局粮食为月初需求量、物资保持 12、价格为 1000‰；月结失败不得留下任何市场/人口/金钱副作用。
- [ ] **步骤 4：实现 v17 保存/验证/迁移**。v16→v17 从旧物资拨出 `min(旧物资, ceil(月初在世家族人数/5))` 为粮食，物资减同量；保留其它原字段，历史短缺从 0 开始，两价格 1000‰。
- [ ] **步骤 5：实现自然死亡短缺修正并跑保存、人口与完整性测试**

运行：`unity command run_tests --mode editor --filter GameSessionMarketTests --filter_type testName --timeout 900`，`unity command run_tests --mode editor --filter SaveDataMigratorTests --filter_type testName --timeout 900`，`unity command run_tests --mode editor --filter GameSessionFamilyLifeCycleTests --filter_type testName --timeout 900`。

预期：每条 XML 均 `result=Passed`、`failed=0`、`total>0`；相同存档/seed 重放相同死亡与价格序列；不新增 RNG stream；v16 的人物、产业任命、关系、编年史、任务、资金、情报完整保持。每轮另记录编译状态与 Console 错误/警告数。

### Task 3：复用产业面板显示市场状态

**文件：**
- 修改：`Assets/Scripts/Application/IndustryView.cs`、`Assets/Scripts/Presentation/IndustryPanel.cs`
- 测试：`Assets/Tests/EditMode/IndustryPanelTests.cs`
- 不改：场景、Prefab、`AddSC019IndustryPanel.cs` 的层级生成、`ProjectSettings`

**接口：**
- `IndustryView.ResourceLine` 显示资金、粮食库存/价格、生产物资库存/价格、情报。
- 月结 `LastReport.Summary` 显示本月人口粮食需求/已满足/短缺和产业材料价格成本；存档载入后从持久状态继续显示当前价格与短缺。

- [ ] **步骤 1：添加 `IndustryPanel_ShowsMarketInventoryPricesAndHouseholdShortage` 与载入刷新测试**，断言既有 SC-019 资源值仍显示、两种商品指数/库存可读，短缺文本不依赖瞬态上月对象。
- [ ] **步骤 2：运行 `IndustryPanelTests` 确认新断言失败**
- [ ] **步骤 3：在 `IndustryView`/`IndustryPanel.Refresh` 重用已有 `resourceText` 与 `reportText`，保持现有面板入口和按钮语义**
- [ ] **步骤 4：运行 `IndustryPanelTests` 与 `FoundationSceneValidator`**

运行：`unity command run_tests --mode editor --filter IndustryPanelTests --filter_type testName --timeout 900`；`unity command run_script --file Assets/Scripts/Editor/FoundationSceneValidator.cs --entry FoundationSceneValidator.ValidateActiveScene`。

预期：测试 XML 为 Passed 且 `failed=0`、`total>0`；`Logs/SceneValidationReport.txt` 包含 `RESULT: PASS; Errors: 0`。检查场景保持原样且 `isDirty=false`；若 Validator 入口调用失败或场景变脏，停止并保留现场供 lead 检查。

### Task 4：真实规则的长周期模拟器与指标导出

**文件：**
- 新建：`Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs`
- 测试：`Assets/Tests/EditMode/M08BLongTermSimulationTests.cs`
- 输出：`Logs/verify/m08b/` 下的 CSV、JSON 与批次日志（不可放截图或提交原始临时文件）

**接口：**
- `M08BLongTermSimulationRunner.RunSeed(ulong seed, int years)` 每次调用创建 `GameSession` 并调用真实 `InitNewGame(seed)`（SC-013）；返回 `M08BSeedResult`。
- `RunShortBatch(int firstSeed, int count, int years)`、`RunFullBatch(int firstSeed, int count)` 以 seed 范围可重入；20 短跑、200 全长、1000 全长可分批运行和续跑，不覆盖已完成区间。
- 固定事件 resolver 依稳定 ID 顺序选择当前可用选项；只在事件无法解析仍阻止推进时记录 deadlock。继承优先选有效指定继承人，否则按稳定 ID 选最小合法候选人；摄政也按稳定 ID 选最小合法候选人。模拟器通过现有 `GameSession` API 继续多代，不改变玩家流程；无合法继承人、灭族、日期越界各保留其停机分类。
- runner 通过 Unity Pipeline 命令触发并写文件，无需操作游戏 UI、加载特定场景或依赖显示器；所有种子仍运行 Unity 中真实 `GameSession` 规则。
- 每个 seed 在第 10、25、50、100 年记录灭族/存活、继承人断档、人口分布、在世人物平均年龄、家族财富，以及经济崩溃、极端通胀、事件频率、关系异常和推进死锁指标。财富按设计稿输出 P10/P25/P50/P75/P90；无存活家族的检查点单独计数，不伪造零值样本。
- 经济崩溃、极端通胀、关系异常、死锁的判定严格复用设计稿定义；分别输出逐 seed 状态/次数、分子、分母和比例，不能从汇总百分比反推或省略失败种子。

- [ ] **步骤 1：写 runner 纯统计测试**，以小型真实 session/短年数验证 seed 隔离、事件选择稳定、停止原因保留、10/25/50/100 年检查点、所有 §14.2 指标、财富估值与分位数、零存活检查点、事件频率、CSV/JSON 序列化稳定。
- [ ] **步骤 2：运行 `M08BLongTermSimulationTests` 确认失败**

运行：`unity command run_tests --mode editor --filter M08BLongTermSimulationTests --filter_type testName --timeout 900`

预期：若 runner API 尚不存在，允许编译器报告本任务测试引用的缺失 API；实现完成后测试应改为行为断言失败。不得把无关编译失败归类为预期。
- [ ] **步骤 3：实现可分批/续跑 Editor runner**，输出每 seed 和总体摘要；不抑制真实编年史/人口规则，先记录单 seed 耗时和内存风险。
- [x] **步骤 4：运行 20 个短跑和一个 100 年单 seed**，检查每个结果、停止原因与运行耗时；不得用“到达 100 年以外”的种子替换灭族/死锁样本。当前规则输入摘要复核通过；短跑含 16 completed、2 extinction、2 pending succession，单 seed 为 extinction。
- [x] **步骤 5：运行 200 个 100 年 seed，检查第 10/25/50/100 年全部 §14.2 指标 CSV/JSON 与逐 seed 终止分类**。CSV 为 200 seed/800 checkpoint 行，终止分类分母 200，所有指标字段存在；详见 M08 campaign review 报告。
- [x] **步骤 6：运行完整 1000 个 100 年 seed 并验证各检查点汇总分母、逐 seed 行、终止分类都与实际完成种子数完全一致**。CSV 为 1000 seed/4000 checkpoint 行，当前规则摘要哈希已通过 runner 复核；第 50/100 年无样本到达，经济结果见运行报告。

预期：重复相同 seed 范围得到字节一致的规范化结果；每个 seed 都有终止分类；所有 GDD 指标均输出，不预设 GDD 未批准的经济平衡通过阈值。

### Task 5：文档对齐、端到端复核与里程碑门

**文件：**
- 修改：`GDD_v4.0.md`、`DECISIONS.md`、`PROJECT_STATE.md`、M08B 设计稿及实现计划
- 证据：`Logs/verify/` 下新增文本报告与 Unity XML

- [ ] **步骤 1：更新 GDD M08B 和 DECISIONS**，只记录已批准规则与实际实现状态；区分未跑完的模拟阶段。
- [ ] **步骤 2：执行全量 EditMode、编译、Console、FoundationScene Validator、SC-013 Play Mode 与保存/读档验收**，各自记录独立证据；使用当前已打开 Editor，不运行 batchmode。

运行：`unity command run_tests --mode editor --timeout 1800`；场景验证按 Task 3 命令重新运行。记录每个 Pipeline job 的终态、EditMode XML 路径与汇总，以及运行前后 Editor 编译状态和 `console_status`。

预期：全量测试 XML 为 `Passed`、`failed=0`、`total>0`；当前 Editor 编译完成且 Console errors=0；场景验证报告为 PASS/0 errors；Play Mode 与保存/读档分别有真实 UI 操作证据。任何 job 超时、XML 缺失或 Editor compile 不稳定均不得记为通过。
- [x] **步骤 3：复查 1000 seed 导出**，每个 GDD 指标都有明确定义、分子/分母和结果；不把模拟器 EditMode 当作 Play Mode 证据。当前规则集的全量指标与分母已记录在 M08 campaign review 报告。
- [ ] **步骤 4：逐验证门只提交刚通过门且由该任务拥有的文件，并立即推送当前分支；若无法精确 stage，保留所有 WIP、不做混合提交并报告。**
- [ ] **步骤 5：不合并、不打 milestone tag，等待用户对 M08B demo 的显式最终验收。**


### Follow-up task: deterministic farm-managed comparison

**Scope:** Preserve the completed unmanaged baseline and add `farm-managed-v1`. This policy is for simulation only; it does not change player rules or create a survival pass threshold.

**Files:**
- Modify: `Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs`
- Modify: `Assets/Tests/EditMode/M08BLongTermSimulationTests.cs`
- Update: this plan, the M08B design, `GDD_v4.0.md` §14.2 simulation-method note, `PROJECT_STATE.md`, `docs/reports/m08-campaign-review.md`
- Output: policy-specific CSV, JSON and logs under `Logs/verify/m08b/`; preserve existing unmanaged artifacts

**Policy:** At each management-stable point after pending events and succession are resolved, if Farm is vacant, enumerate living family members by ordinal `PersonId.Value` and try `GameSession.TryAssignIndustry` until one succeeds. Leave Farm vacant when no candidate qualifies. Keep Trading House vacant. Do not replace a valid Farm appointment. If the real session rules vacate it, refill at the next stable point. The policy consumes no RNG.

- [x] **Step 1: Pin the existing default.** `DefaultRunStaysUnmanaged` proves a default run leaves Farm and Trading House vacant with zero policy counters. The unmanaged rerun under the new schema matches the accepted historical artifact on all 123 existing columns for 20, 200 and 1000 seeds (zero field differences); historical files were not overwritten.
- [x] **Step 2: Test farm assignment.** `FarmManagedV1PicksTheLowestOrdinalLegalCandidate`, `FarmManagedV1NeverReplacesAValidAssignment`, `FarmManagedV1RefillsOnlyAfterRealSessionRulesVacateThePost` and `FarmManagedV1LeavesThePostVacantWhenNobodyIsEligible` cover the four required outcomes through the public session API.
- [x] **Step 3: Test determinism and artifact separation.** `FarmManagedPolicyIsDeterministicForTheSameSeed` and `PolicyRunsWriteDistinctPolicyLabeledArtifactsAndNeverShareRows` cover byte-identical repeats, distinct policy IDs/paths and cross-policy reuse rejection.
- [x] **Step 4: Implement and verify in the current Unity Editor.** Compilation clean; targeted EditMode 50/50 and full EditMode 1958/1958 passed. EditMode aftermath had 2 intentional injected negative-path errors and 45 known warnings; the later Play Mode check cleared prior entries and ended at 0 errors / 0 warnings / 0 logs. `Logs/Editor.log` contains 52 Pipeline 30-second transport timeout lines from long batches; these are not simulation faults. No scene, prefab or `ProjectSettings` change.
- [x] **Step 5: Run both policies through 20/200/1000 seed stages.** Both policy directories hold complete, provenance-checked stages; `unmanaged` reproduces the accepted baseline and `farm-managed-v1` reports 870 completed / 68 extinct / 62 pending succession at 1000 seeds. Reported as one deterministic policy, not average player behaviour and not a pass threshold.
- [x] **Step 6: Lead Play Mode check.** In the existing Unity 6000.6.6f1 Editor (PID 77116, project `C:\Users\panye\My project`), started a fresh SC-013 game through the title UI, appointed the ordinal-lowest candidate `person.sc013.chie` (明智 千惠) to Farm through the actual Industry panel, and settled one month through the map UI. On 1560-05-01 the live report showed Farm output +4 food, household demand 2 for six people, grain 2→4, materials 12→15, both price indexes 1000→900‰, and food shortage 0 months. No explicit save was performed. After clearing prior test/cleanup entries, Play Mode Console was 0 errors / 0 warnings / 0 logs; Editor returned to ready/stopped and FoundationScene stayed clean.
- [ ] **Step 7: Independent read-only documentation recheck, then commit/push verified task files.** The implementation review passed; a separate read-only review of the final Play Mode documentation delta is pending. Commit only verified task files to the current branch and push it. Do not merge or tag before explicit final M08 acceptance.
- [x] **Step 8 (correction, 2026-11-14): Default and no-policy artifact routing.** Step 1 proved the default *policy* was unmanaged, but the no-policy entry points (`RunShortBatch`, `RunFullBatch`, `RunStagedBatches()` and the blank-root fallback of `RunBatch`) still resolved to the historical root `Logs/verify/m08b/`, so a default re-run could have overwritten the accepted v5 baseline. They now run `Unmanaged` through `RunPolicyBatch`, and the policy-aware `RunBatch` resolves a null or blank root with `ResolveArtifactRoot` to `PolicyOutputRoot(policy)`; an explicit root is used as given unless its normalized full path equals the historical root, which `ResolveArtifactRoot` rejects with `ArgumentException` before any write because the ignored tool passed `DefaultOutputRoot` explicitly. `DefaultRunRootsTargetPolicyDirectoriesAndNeverTheHistoricalRoot` writes no run file and asserts that both policies resolve to their own directory, that no default root is the historical root, that an explicit non-legacy root is preserved, and that exact, trailing-separator, `.`-normalized and case-varied spellings of the legacy root are rejected without changing that directory's entries. Evidence: compile ground truth clean (`compilationFailed=false`, 0 errors/0 warnings), focused test 1/1, `M08BLongTermSimulationTests` 51/51. No policy logic, schema, policy label or economy rule changed; the full EditMode suite was not re-run for this editor-only path change.
