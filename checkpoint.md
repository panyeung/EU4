# 《影之一族》checkpoint 与设计规划

更新时间 2026-11-17。当前工作分支 `panyeung/m00-closeout`（自 `main@86518cf` 分出），基线提交 `0b9b1a8`。
本文是设计规划、全部 checkpoint、当前状态与下一步的唯一入口。UI 与美术资产见 [ui设计规划.md](ui设计规划.md)。代理协作规则见 [AGENTS.md](AGENTS.md)。

规则：新 checkpoint 只往第 4 节表格加一行，长过程记录留在 `Logs/`，不要往本文追加叙述段。

文档地图。旧文档已删除，需要原文时从 git 提交 `d57ea7c` 取回（`git show d57ea7c:GDD_v4.0.md`）。`AGENTS.md` 已指向本文与 [ui设计规划.md](ui设计规划.md)。

---

## 1. 设计规划

### 1.1 愿景与支柱

- 玩家扮演战国忍者家族的当代家主。主角是家族，不是某个人物。
- 四大支柱共用同一个持续世界。
  - 家族经营。家产、产业、据点、人才、资金、秘术、外交。
  - 角色扮演。家主亲自旅行、修炼、交友、授业、参与事件与任务。
  - 家族传承。婚姻、子女成长、继承、隐居、摄政、分支、世代编年史。
  - 忍者战术。小队回合制潜入、情报、营救、破坏、暗杀。
- 必须提供的体验。记住家臣与子女。重要人物死亡改变规划。为长远利益放弃眼前收益。不同世代选不同路线。几十年前的行为影响后代。亲自执行或信任部下。政治变局中保护家业。
- 设计红线。
  1. 家主死亡不直接导致游戏结束。
  2. 每个重要人物有永久身份和可追溯历史。
  3. 战术服务家族成长，不取代经营。
  4. 玩家不行动，世界仍变化。
  5. 允许多种发展方式，不强迫军事征服。
  6. 中后期不得出现大量无意义重复操作。
  7. 重要行动的风险和后果必须可理解。
  8. 验证家族传承前，不大量投入战术地图和 AI。

### 1.2 核心循环与行动经济

- 三层操作模式。
  - 家族经营。成员与职位、产业与资源、忍术研究传承、派遣、教育继承、外交、编年史。
  - 家主扮演。修炼、拜师、拜访、外交、婚姻、教子、旅行、接委托。用地点选择和事件界面，不做开放世界。
  - 回合制战术。重要任务进独立场景。家主可不出战，委派用属性检定和事件结算。
- 月度循环。查看状况 → 处理事件与决定 → 安排行动 → 管理资源产业训练委托 → 执行活动或任务 → 月末结算 → 推进人物家族外交历史 → 下月。战略每月一回合。
- 时间预算。原型每名成年人每月 4 个时间单位，家主与家臣各自预算，可同时工作。不另设家族命令行动点。
- 单次消耗。个人训练 1、教导子女 1、拜访当地人物 1、外交 2、短期忍者任务 2、大型远征 4。持续管理按职责占用时间。
- 冲突工作不可同时参加。跨月任务持续占用。时间单位与天数无换算。

### 1.3 家族与人物

- 身份类型。家主、配偶、直系、旁系、养子养女、外姓家臣、弟子、客将、临时雇员。
- 永久 `PersonId` 不随更名、婚姻、继承、切场景改变。
- 能力四维忍者。潜行、体术、忍术、侦察。经营四维。统率、谋略、外交、经营。
- 人格特质。忠义、野心、谨慎、勇敢、重视亲情、利己、求知。影响数值、事件选项、行为和关系。
- 人生状态。年龄、健康、伤势、疲劳、身份、职责、关系、经历。
- 关系。父母子女、婚姻、兄弟姐妹、收养、师徒、好友仇敌、家臣主君、恩义、背叛。血缘是结构事实，好感、信任、恩义是可变状态。不得只用单一好感度。
- 成长。来自训练、任务、工作、师徒、事件。速度受能力、特质、年龄、健康、训练条件影响。重伤可留长期后遗症。
- 年龄曲线。少年重教育学习。青年重身体与任务经验。壮年能力成熟、发展事业。中年积累谋略人脉经营。老年体能可降，经验关系仍有用。渐进变化，不突然削弱。
- 子女培养。0–15 岁轻量年度培养，每年选忍术、武艺、学问、经营、社交或自由成长。不保证继承父母职业优势。
- 家族内政。家臣与继承人冲突、旁系争位、外姓能臣与血亲争权、配偶家族干涉、弟子求独立、老臣护旧政。首版用事件、野心、职位、忠诚度，不做派系模拟。

### 1.4 家督继承

- 家主退休、死亡或失能必须继承。候选为合规直系、旁系或正式收养者，初期由家主指定。
- 继位转移玩家控制权。资产、声望、产业、秘术资料、重要契约延续。个人能力不转移，私人关系不全盘继承。
- 隐居。旧家主可教导、后见、维系关系、参与事件和建议，可能形成双重权威，不是安全退休按钮。
- 幼主由配偶、亲族或重要家臣摄政。可夺权、控制培养、改政策、拒还权。首版做指定摄政、受限决策、成年交权。
- 秘术分家族知识与人物掌握。继承资料不等于学会技能。掌握者死亡且记录不全可失传。
- 灭亡条件。无可用继承人、无法建立合法延续关系、无可行重建途径。允许多次危机收养与旁系继承，避免随机强制失败。

### 1.5 经济与产业

- 资源。资金用于建设、工资、雇佣、外交。物资用于维护、忍具、准备。情报用于准备、政治、交易。粮食（M08B 起）供人口消费。
- 组织指标。稳定度、名望、隐蔽度、雇主信用、各势力关系与敌意。不合并成家族等级。
- 产业。农庄（收入与粮食）、商馆（贸易资金情报）、道场（训练传承）、药铺（药材医疗）、忍具工房（装备）、情报屋（情报）、秘密据点（潜入与地区影响）。
- 产业约束。有维护成本、负责人、工作容量。建筑不得无限升级。扩张受人员、资源、局势、经营能力限制。
- 三阶段。初期生存型（任务报酬、少成员、简陋产业）。中期经营型（稳定产业、专业家臣、多情报源）。后期名门型（家族网络、商业关系、大名委托）。后期以自动经营为主。
- 家训可选守秘为先、义理至上、唯才是举、家业兴隆、忍术至上。家训影响事件政治，个人抱负决定当代方向。可改革家训但引发内部冲突。

### 1.6 名望、隐蔽度、雇主信用（M07 已批准）

- 名望衡量知名度与地位。提高委托价值、招募和结盟机会，过度公开带来政治压力。
- 隐蔽度保护据点、身份、行动、情报网络。与名望独立，不互为反值。
- 每名雇主独立记录履约、信任、背叛、恩义、保密、与敌方合作。
- 范围。名望与隐蔽度 `0..100`，关系 `-100..100`。新档名望 0、隐蔽度 50，缺省关系 0。信用与势力关系按稳定 ID 保存。

| 五类结果 | 完全成功 | 成功但有代价 | 部分成功 | 失败并安全撤离 | 严重失败 |
|---|---|---|---|---|---|
| 名望 | +3 | +2 | +1 | -1 | -3 |
| 雇主信用 / 势力关系 | +5 | +2 | +1 | -3 | -5 |
| 隐蔽度 | +3 | +1 | 0 | -2 | -5 |

结算截断到范围并记录实际前后值。M06 单目标不产生部分成功。

### 1.7 数据驱动事件系统

- 承担人物互动、婚姻、继承、家臣冲突、历史危机、外交、任务后果。
- 结构字段。`EventId`、`Trigger`、`Conditions`、`Weight`、`TextId`、`Options`、`Requirements`、`Effects`、`Cooldown`、`OnceScope`、`ChainId`、`Priority`。
- 类型。成长、家族关系、经济产业、忍者委托、外交、继承、历史、家族危机、长期恩怨。
- 必须支持跨代事件链，持续保存决策后果，不只串故事文本。
- 技术。JSON 结构化数据，字符串 ID，姓名/称谓/身份变量替换。条件与效果只用预定义安全操作，禁止任意 C#。统一验证、提交、记史。奖励不得重复结算。种子与执行顺序可复现。
- 内容卡审阅。重要链须写人物条件、选项、可见风险代价、谁何时承担后果、跨代回收、人物死亡或离开时的替代或终止。生成内容同样要验证和试玩。

### 1.8 忍者任务

- 类型。情报收集、潜入侦察、窃取、救援、破坏、暗杀、护卫、防卫。
- 准备。选队长与成员、收集情报、准备装备忍具、选潜入方案、决定家主是否参与、手动或委派。占时间和成员预算。
- 第一阶段（属性检定，M05 承载）。能力、情报、准备、资源、难度与随机共同决定，输出五类结果。风险须提前可见，不得频繁用不可预测随机直接杀死关键继承人。
- 第二阶段（战术，M06 承载）。2D 等角方格，方格寻路。初期 3 人、后期最多 5 人，每人每回合 2 AP。视野、声音、遮蔽；巡逻、怀疑、调查、警戒。忍术含烟雾、幻惑、陷阱、支援。允许中途撤离，不强制全歼。
- 统一结算写回家族。战术场景不得直接改长期状态。手动与自动同结局同奖励，无模式专属奖励，无强制手动任务。

### 1.9 世界模拟与历史时间线

- 大名有军力、经济、领地、外交、战略目标。忍者家族有成员、产业、名望、隐蔽度、信用、委托网、抱负。两套行为不共用经济战争系统。
- AI 家族接委托、经营、招募、培养继承人、结盟竞争、换家主、受战争与危机衰落。首版目标驱动月度决策，不模拟每个人的日常动作。
- 约 1560 年开局。历史压力为 1579 第一次天正伊贺之乱、1581 第二次、1600 关原、1614 大阪冬之阵、1615 大阪夏之阵。
- 史实模式按剧本出主要节点，局部结果可变。架空模式按世界条件触发。第一阶段只做简化史实。
- M08C 首片只进 SC-010。每节点一条传闻加一条节点记录。通常前一年 1 月传闻，目标年 1 月 1 日记录。同月冲突则传闻顺延当年下一个空月，1615 夏之阵的传闻落在 1614-02-01。
- 节点只写事件历史与编年史，不改资源关系，不强制战果、战斗、死亡、领地变化、灭族。日期是游戏调度日，不冒称史实公历日期。
- 复用 `EventRuntimeState` 的单局一次标记与待处理队列，不增存档字段，不回填过去节点，读档保留待处理状态。

### 1.10 长期目标与结局

- 五维评价。名望、经济实力、忍术传承、政治影响力、稳定度。不强制全部满值。
- 五条路线。忍术宗家（独有流派）、战国豪族（财富产业据点）、大名御用家族（数代合作）、暗影之主（隐秘网络与情报）、百年名门（长久传承与稳定）。路线可并行，需取舍时间人才资源。
- 1560 开局，1615 初步阶段评价（约 55 年）。评价后可自由游玩。持续满 100 年评百年名门。
- 结算历代家主、财富、忍术、死亡经历、盟友仇敌、史事与最终地位，生成可回顾编年史。

### 1.11 技术架构原则

- 单人、Windows PC、Unity 6、C#。当前引擎 `6000.6.6f1`，升级须独立分支验证。
- 技术栈。纯 C# 模拟、uGUI + TextMeshPro、2D Sprite、Grid + Isometric Tilemap、Input System、JSON 与 ScriptableObject 静态配置、版本化 JSON DTO、NUnit。
- 一个权威世界状态供全部界面操作。规则不依赖场景、动画或 GameObject。
- 数据模型。`GameState`、`CharacterState`、`FamilyState`、`RelationshipState`、`HouseholdState`、`RetainerState`、`FactionState`、`MissionState`、`TacticalBattleState`、`WorldState`、`EventState`、`HistoryState`。模型以稳定 ID 关联，存档不写 GameObject 引用。
- 服务。`FamilyService`、`CharacterService`、`RelationshipService`、`SuccessionService`、`EconomyService`、`DiplomacyService`、`MissionService`、`TacticalService`、`EventEngine`、`TimeScheduler`、`WorldSimulationService`、`HistoryLedger`、`SaveGameService`、`SimulationRunner`。先写简单可测的类，不预建框架。
- 确定性随机。各系统独立随机流，保存初始种子、各流状态、游戏版本、规则版本、内容版本与完整世界。
- 人物分级。家族成员、继承人、主要家臣、大名、关键人物完整模拟。普通人物简化或按需生成，进入重要关系时转永久人物。
- 存档。恢复重要人物关系、家主继承人、家业资源、时间、事件标记冷却、进行中工作与委托、势力关系、历史与随机状态。支持版本迁移。

### 1.12 第一可玩原型（三代速通）

| 项目 | 数量 |
|---|---|
| 玩家家族 / 初始重要人物 / 外部势力 | 1 / 6 / 2 |
| 家训 / 主要特质 / 产业 / 任务类型 | 3 种 / 8 种 / 2 种 / 3 种 |
| 核心事件 / 跨代链 / 世代 / 家督交接 | 30 / 5 条 / 3 代 / 至少 2 次 |

- 30 事件建议分配。亲情 6、家臣师徒 5、经营财富 4、忍者委托 5、婚姻继承 5、历史危机 5。
- 一代创业、立家训、交友、养继承人。二代接手关系契约、旧臣、政治压力。三代传承或改革、解决恩怨。
- 可跳至下一个重要事件，年龄、经济、成长照常结算。一轮约 20–40 分钟。
- 验收十条。记住重要人物及经历。第一代决定改变第二代事件。第二代选择影响第三代。可主动或意外继承。继位不重置世界和关系。至少一次继承有真实机会成本或情感冲突。编年史记录全部重要事件。存读档完整。正常继承无死循环。真人有具体人物与继承反馈。
- 至少 5 位试玩者。若只感到两代数字替换，M01 不通过，先改关系、事件、继承决策。

### 1.13 里程碑定义

依赖顺序 M00 → M01 → M02 → M03 → M04 → M05 → M06 → M07 → M08。按可演示结果验收，不以日期代质量。

| 节点 | 目标 | 验收要点 |
|---|---|---|
| M00 | 可信起点，区分已实现、已验证、用户已验收、进行中 | 确定工作树与基线提交，开放切片有负责人与证据；编译、EditMode、Console、Validator、存读档、Play Mode、用户验收分项留证 |
| M01 | 固定三代故事可完整试玩 | 开局到两次交接与约定结局，关系家业资源编年史在继承后正确，用户 Play Test 接受。Go/No-Go |
| M02 | 独立于帧率的日行动时间线 | 互斥不并行、途中不同时在起终点、跨月不漏不重、中断位置原因明确、同种子可重现、读档续行 |
| M03 | 少量人物自主且可解释 | 需求与人格驱动，Utility AI + 预定义计划，不做 GOAP/HTN；受伤缺资源职责冲突符合规则；只读 Inspector 解释选择与排除 |
| M04 | 经历改变行为，脱离固定名单 | 记忆有产生条件与决策影响；动态家庭至少两次合法交接；长模拟无非法亲属或无法继续 |
| M05 | 有代价的稳定经营 | 产业任命、自动工作、成长教育、研究家训整合；至少一委托接取到回写完整；关键失败路径经存读档验证 |
| M06 | 小战术增加家族故事价值 | 小地图、明确目标、少量守卫；成功路线与失败或撤退路线都可玩；结果回写且存读档保留 |
| M07 | 三层玩法共享身份世界与存档 | 亲自执行与委派同结局同奖励；伤亡伤势奖励名望信用回写；三层连续游玩并至少完成一次家主更替 |
| M08 | 扩大内容与交付质量 | 完整多代游戏，长模拟与迁移稳定，内容理解度经试玩；扩 UI、立绘、音效、教学、可访问性、性能 |
| M08A | 家族人口动态（2026-11-12 批准） | 年界一次结算，顺序为到期出生、自然死亡、婚姻；同月结事务提交或回滚，同种子重放 |
| M08B | 多商品经济与长期模拟（2026-11-12 批准） | 双商品市场、价格压力公式、短缺死亡加成、100 年 × 1000 种子无图形模拟 |
| M08C | 史实世界时间线（2026-11-14 验收） | 五个历史节点加五条提前一年预警，只进 SC-010，不改资源关系 |
| M09 | Debug Mode 综合测试窗口 | Editor 与 Development Build 内独立内存会话，不读写玩家槽；普通发行无入口 |

### 1.14 测试与长期模拟

- 单元测试覆盖。亲属合法、继承引用、候选、死亡退休、时间冲突、收入维护、事件条件与幂等、奖励、史事前置、存读档一致。
- 长期模拟目标。每次 100 年、1000 组种子，用真实 `GameSession`。统计灭亡、继承断档、人口、平均年龄、财富、崩溃、极端通胀、事件频率、关系异常、世界死锁。
- 模拟内继承。优先有效指定继承人，否则最小稳定 ID 合法候选；摄政同样取最小合法 ID。仅模拟自动选。
- M08A 年界规则（2026-11-12 批准）。
  - 仅十二月到一月边界结算一次。顺序为到期出生与受孕 → 对月初在世成员按 `PersonId` 各检一次自然死亡 → 无在世配偶的成年 NPC 各检一次 15% 婚姻。
  - 死亡率按新日期年龄。0–14 岁 0.5%、15–39 岁 0.5%、40–59 岁 1%、60–69 岁 4%、70–79 岁 12%、80+ 岁 25%。仅初版调参，不称历史统计。
  - 死亡保留人物、家谱、关系事实，清动作与产业任命，取消未完怀孕，记一条编年。家主死亡只触发一次既有继承。新生儿不进本次死亡名单。
  - 自主婚姻不替玩家择偶。排除家主、未成年、性别未知、有在世配偶者。新配偶为异性成年人，尽量年龄差不超过 5 岁，不受生育年龄限制，当年不再配对，下月起进 M04B 怀孕检定。
- M04B 怀孕规则。正式配偶须在世且属当前家族。女性 16–40 岁、男性 16–60 岁，含边界。每对配偶每月独立检定 3%。孕期 9 次月推进，第 9 次出生一次。性别 50/50。
- M08B 市场规则（2026-11-12 批准）。
  - 商品 `market.food` 与 `market.materials`，资金唯一货币，情报不入市。基准 1 资金/单位，开局两指数 1000‰。
  - 每月按月初在世家族人数消费 `ceil(人数/5)` 粮食，按上月价格结算。购买量取需求、库存、可负担量最小值，不负债。
  - `压力‰ = roundHalfUp((需求 - 可用供给) * 1000 / max(需求, 可用供给, 1))`，`变动‰ = clamp(压力‰/4, -100, +100)`，`新指数 = clamp(roundHalfUp(旧指数 * (1000 + 变动‰) / 1000), 100, 100000)`。单月最多 ±100‰。
  - 综合指数为粮食与物资 `50/50` 基期篮子，同比翻倍即极端通胀。不称历史 CPI。
  - 每满 3 个连续短缺月，在下个年界给受影响者加 50‰ 年死亡率，最多 +250‰，完全供足一月归零。
  - 产业照旧扣生产物资，并收 `ceil(投入量 * 物资指数 / 1000)` 资金成本。Farm 自留物资也计价，另产粮 4 单位。钱或物资不足沿用停产，不部分扣费。
  - v17 存粮食库存、两指数、结算月索引、连续短缺月数、综合指数史。v16→v17 从旧物资拨至多一月口粮，两指数设 1000‰，短缺归 0，不补饥荒史。
  - 经济崩溃定义为连续 12 个月至少缺 1 粮食，或全部已任命产业连续 12 个月停摆。无人任命产业不计停摆。
- 已接受基线（2026-11-13，`unmanaged`，1000 种子 × 100 年）：灭族 605、待危机继承 395、完成 0。到达 10/25/50/100 年分别 194/2/0/0。粮荒、崩溃、极端通胀各 999/1000，产业停摆 0，世界死锁 0。
- `farm-managed-v1` 对比策略（2026-11-14，仅模拟）：完成 870、灭族 68、待继承 62，10/25/50/100 年为 1000/998/921/870，粮荒与崩溃 803/1000，极端通胀 801/1000。产物分目录 `Logs/verify/m08b/<policyId>/`，旧根 `Logs/verify/m08b/` 显式传入会被拒绝。
- SC-013 实测（2026-11-14）。任命 `person.sc013.chie` 到农庄，1560-04-01 → 1560-05-01 月结，六人需求 2，粮 2→4、产粮 +4、物资 12→15、两指数 1000→900‰、短缺 0 月。
- 边界。自动模拟不替代真人试玩。单一策略不代表平均玩家行为或生存通过。50/100 年存活样本为零，不是通过结论。

### 1.15 范围红线

- 三代验证前暂缓。大型开放世界、完整战国地图、复杂战术 AI、大量忍术动画、几十种产业、数百完整 NPC、完整战争、多人、批量低质随机事件。
- 暂缓不等于取消，战术正式投入须在 M05 验收后。
- 决策点。A 在 M01（继承无趣则改人物事件传承，不扩系统）。B 在 M05（无战术仍无策略取舍则改经济家臣外交）。C 在 M04（不能稳定数代则先修模拟继承存档）。D 在 M06–M07（战术削弱家族乐趣则加强委派、减少强制手动）。

### 1.16 月度结算与验收边界

- 固定顺序。锁安排 → 完工作任务并算结果 → 伤亡失能 → 继承 → 经济年龄成长 → 检查排序事件 → 必须回应事件 → 下月。
- 同阶段对象按稳定 ID，事件按优先级再稳定 ID。展示前重验，失效跳过并记因。
- 新家主下月才获新预算。旧家主未完工作依任务取消或移交，已成结果统一结算，不重领。
- 连续推进仍逐月完整结算。重大事件、继承摄政、任务完成、继承人重要年龄、已知资源不足时暂停并说明。
- 存档只在稳定点。月结后的管理，或重大事件与继承已完整排队待输入。结算与效果提交中不可存。
- 临时文件写验后替换正式档并保留上一份。读失败说明且不覆盖原档。恢复不重交效果奖励。
- 分项验收。编译、EditMode、Play Mode 与场景、Console、存读档、真人反馈各自记录。未执行标未验证，不用源码检查或自动模拟代替。

---

## 2. 决策记录

| 日期 | 决策 | 一行理由 | 影响 |
|---|---|---|---|
| 2026-11-18 | 集成分支整体快进 `main`，不再逐支合并 | `t_122d4d5b` 独立复审 C1–C6 全 PASS，四支源码已在集成链里逐支过回归；让 `main` 与 `integration/v1-20261118` 同点，比再造五个合并提交少一层无法验证的中间态 | 用户 2026-11-18 授权；`main`、`origin/main`、`integration/v1-20261118` 同为 `2bca9d1`；只保留一个工作树 |
| 2026-11-18 | 画布层序统一为 `CanvasLayerOrder`，删除 `CanvasOverlayOrder` | m04 与 m08 各带一套并行层序机制，合并后会同时生效；一个共享页面序才能让座次校验有唯一来源 | `Assets` 内 `CanvasOverlayOrder` 零命中；`CanvasLayerOrder.cs` 持七项页面序，`AddM08CampaignReviewPanel` 与 `AddSC010SuccessionPanel` 按共享序插入并校验，由 `CanvasLayerOrderTests` 与 `AddM08CampaignReviewPanelTests` 覆盖 |
| 2026-11-18 | 主工作树未提交项按「与 `m00-closeout` 内容等价」处理，不再重复入库 | 69 项与 `e57dd06` 逐字节相同，差异只有行尾 LF/CRLF，另开提交只会造噪声 | 不新增镜像提交；后续收口以 `panyeung/m00-closeout` 为内容基准 |
| 2026-11-18 | Editor 卡死按 PID 精确恢复，不整体杀 Unity | 同机同时跑着工作树的 Editor 实例，整体杀会连带丢掉它们的会话 | 只结束 PID 88424；重启后 96584 / 端口 7800，场景备份留 `Logs/recovery-20261118/` |
| 2026-11-18 | Editor 重启后场景恢复改为显式 `open_scene` | `Library/LastSceneManagerSetup.txt` 是 `sceneSetups: []`，不会再自动恢复上一次场景 | 每次重启先 `unity command open_scene Assets/Scenes/FoundationScene.unity`，再谈验证 |
| 2026-11-18 | Kanban 卡不得落在主工作树 | dispatcher 会立刻派工，`--workspace dir:<主工作树>` 让 5 张卡同时抢一个 Unity 实例，违反一张卡一个写者 | 本轮已 reclaim 并 block 那 5 张；规则写进 `AGENTS.md` 并发节 |
| 2026-11-14 | M08C 历史时间线日期与范围 | 五节点 1579/1581/1600/1614/1615，节点落 1 月 1 日，预警提前一年 | 事件目录与中文文本、`EventRuntimeState`；无 schema 迁移；只进 SC-010 |
| 2026-11-14 | M08B 默认产物路径改写到策略目录 | 旧根 `Logs/verify/m08b/` 是已接受的 v5 基线，重跑会覆盖证据 | runner 与 `ResolveArtifactRoot`；显式传旧根抛 `ArgumentException` |
| 2026-11-14 | M08B `farm-managed-v1` 仅作模拟对比 | 基线是产业岗位空缺而非配置错误，用确定性策略量化任职影响 | 产物 schema v6 与 `policy` 列，产物分目录 |
| 2026-11-13 | M08B 多商品经济与 1000 种子基线 | 双市场加价格压力公式，用户接受当前结果与生存风险 | GDD §14.2 权威；存档 v16→v17；面板预览与月结共用 `IndustrySettlement.Plan` |
| 2026-11-13 | M08B Task 4 修正（提供性、选择顺序、继承缺口） | 目标人物成为家主后旧场景选项全部提交失败 | `EventEngine` 提供时门禁、待处理事件阻塞继承；`heirGapAtStop` 改 395/395 |
| 2026-11-12 | M08A 年度人口放进月度事务 | 死亡出生婚配按固定顺序消耗确定性随机，任一失败整月回滚 | 月度事务、编年史、`ChronicleCategory.NaturalDeath = 8`；schema 仍 v16 |
| 2026-11-12 | M08B 长跑模拟用确定性继承 | 让 1000 种子可复现，不动玩家侧继承 | 仅模拟适配器，复用既有继承 API |
| 2026-11-12 | M03 自治资格规则 | 受伤只能休息，零补给不可选工作，产业在岗不可自选工作 | NPC 规划与 Inspector；两名 pilot 明智 正綱、明智 信正 |
| 2026-11-10 | M05 交付一个手写护送委托而非任务框架 | 一个可复审的纯 C# 类能表达资金人手家训取舍 | `mission.shadow-escort`、commission 存档块 v14、委托面板 |
| 2026-11-10 | Unity Pipeline bridge 是受支持的 agent 控制路径 | `run_script` 能对真实游戏代码跑生成输入 | 工具链与验证技能；Editor 端口 7800–7849；持有工程时禁 `-batchmode` |
| 2026-11-03 | SC-017 自建小型只读家谱图 | 候选库分别是可编辑节点器、仅支持严格树、无 LICENSE | 家谱快照、确定性布局、复用 `KinshipGraph`，不加包 |
| 2026-11-03 | SC-016 用既有关系增量记录委托旧约 | 只追加事件与文本，复用允许列表条件 | `chain.sc013.commission-legacy`，优先级 8/80/800 |
| 2026-11-03 | SC-015 三代家书链只走事件历史 | 选项全用 `Always` 且 `effects` 为空 | `chain.sc013.family-letters`，优先级 6/60/600，内容版本保持 2 |
| 2026-11-03 | SC-014 只用数据加第二条 SC-013 跨代链 | 复用允许列表与既有排程即可表达三代抉择 | `chain.sc013.refugee-legacy`，优先级 5/50/500，无代码改动 |
| 2026-11-03 | SC-012 用入档的追加式家族编年史 | 只记成功提交、真实关系变化、任务与继承阶段 | `HistoryLedger`、只读滚动面板、v3→v4 迁移 |
| 2026-11-02 | SC-011 跨代事件用允许列表条件，不建新链运行时 | 现有引擎与历史已带所需事实，最小切片够用 | 三种条件加 `EventRuleContext`；内容版本升 2，schema 仍 3 |
| 2026-11-01 | 采用 Shadow Clan 计划 v4.0 并复用现有工程 | 用户指定 v4.0 为新方向，只用一个 Unity 工程 | 设计权威与 M00–M08；M1–M7 降为历史；引擎 `6000.6.6f1` 不变 |
| 2026-11-01 | 增加两个固定 M4b 任务 (历史) | 用既有五名 NPC 与三座城凑齐三任务范围 | `SealedLetterMission`、`MarketTallyMission`，各 +5 merit 一次 |
| 2026-11-01 | 任务面板留在已验证的城镇足迹内 (历史) | 扩大根会盖住名册与返回控件 | 280×240 根内页签切换，越界回滚 |
| 2026-11-01 | M5 用内置 JSON 单槽存档 (历史) | 约十个字段，`JsonUtility` 免依赖 | `Persistence` 程序集，后由版本化 schema 取代 |
| 2026-11-01 | M6 标题覆盖层做 Play Mode 门禁 (历史) | 一个全屏遮挡兄弟节点即可拦住全部输入 | `Canvas/M6TitleRoot`、`TitleScreenController` |
| 2026-11-01 | M7 用纯阈值表从功名派生军阶 (历史) | 派生值不入档，无法伪造或重复晋升 | `PromotionRules` 足轻 0、番头 5、组头 10、武士头 20 |
| 2026-10-31 | M4a 保持一个固定 C# 任务 (历史) | 首个完整任务，固定状态机与一次性奖励可直接测试 | `SupplyShortageMission`、黑田伊织、+10 merit 只发一次 |
| 2026-10-29 | M2 沿用现有分支做演示候选 (历史) | 分支已有 M2 实现与已验证场景 | `feature/m2-world-map`，M3 单独分支 |
| 2026-10-29 | M2 用内置 UI，后续 Phase 1 系统延后 (历史) | 路线图把 M2 限定在地图旅行与城镇菜单 | uGUI 加内置字体；设施为锁定占位 |
| 2026-10-29 | 城镇界面与地图控件分离 (历史) | 演示反馈指出面板重叠，同场景分层更轻 | 打开城镇菜单隐藏地图控件，底板不透明 |
| 2026-10-29 | M3 集成与用户现有工作隔离 (历史) | 现有 M3 worktree 在同一场景有未提交改动 | `feature/m3-accepted-integration` |
| 2026-11-02 | 接受 SC-004 到 SC-009 检查点 | 用户 Play Test 验收，`TestResults.xml` 548/548 | 月度时间、确定性 RNG、存档版本迁移、事件校验与选择 UI；`c29d24a` |
| 2026-11-03 | 增加 SC-013 固定三代 M2 剧本 | 六人两次合法交接即可演示 | `M2FixedScenario`，`scenario` 字段隔离目录 |
| 2026-11-04 | SC-020 子女教育用独立 Aoba 剧本 | 独立固定剧本可精确测年度教育与 15→16 成年交接 | 五属性成长、schema v7 与 rules 3 |
| 2026-11-04 | SC-022 用原子家族组建命令做婚姻与普通收养 | 复用既有继承世界与编年史，失败必须整体回滚 | `FamilyFormationRules`、`FamilyFormationPanel`；版本保持 8 |
| 2026-11-04 | SC-021 家族知识与个人掌握分离 | 研究产出记在家族层，不引入个人掌握集合 | `research.smoke-mixing` → `technique.smoke-mixing`、schema v8 与 rules 4 |
| 2026-11-05 | SC-023 让退休前家主指导下一代 | 用编年史 `FromHeadId` 证明真实交接，不开全局退休例外 | `EducationMentorRules`、复用 SC-020 面板，schema 不变 |
| 2026-11-11 | M07 共享任务结果与结算 | 手动与委派共用五种结果与一条结算路径 | 新领域契约、schema v15 与 rules 8、M05 与 M06 模式选择 |

已被取代的决定。
- Sengoku Chronicle 与太閤方向，由 2026-11-01 采用 v4.0 取代。
- M5 的扁平 `SaveData`（`saveFormatVersion = 1`）单槽模型，由 v1–v17 迁移链取代。
- M4a/M4b 每任务固定 merit 的奖励模型，由 M05/M06 承载并经 M07 统一结算。
- 2026-11-10 的 M05 门禁结论，由 2026-11-11 失败路线条目取代。
- 「无策略默认重跑写历史根目录」与「基线未配置产业岗位」两种解读，由 2026-11-14 目录改写与 `unmanaged` 加 `farm-managed-v1` 对比取代。

---

## 3. 当前架构

### 3.1 程序集与依赖

- `SengokuChronicle.Domain` 纯 C# 规则核，无引用。持 `WorldClock`、`GameDate`、确定性随机流、`HistoryLedger`、`PromotionRules`、产业与任务规则。
- `SengokuChronicle.Events` 纯 C# 目录模型、内容校验与允许列表 `EventEngine`，从 `Resources/Events` 与 `Resources/Localization` 读 JSON，不执行任意脚本。
- `SengokuChronicle.Persistence` 用 `JsonUtility` 加版本化 DTO、校验器、原子单槽存储。
- `SengokuChronicle.Application` 引用 Domain。`GameSession` 是唯一用例编排点，管月度推进、存档捕获恢复、待处理事件、继承、任务结算。分部文件 `GameSession.M07.cs`、`.M08.cs`、`.M08A.cs`、`.M08B.cs`。
- `SengokuChronicle.Presentation` 只做 uGUI 显示与输入转发，不执行游戏规则。
- `SengokuChronicle.Editor` 放事务式场景生成器与 `FoundationSceneValidator`。
- `SengokuChronicle.EditModeTests` 是 `isTestAssembly`，仅 Editor，`autoReferenced=false`。
- 依赖方向 `Presentation → Application → Domain/Events`，`Application ↔ Persistence` 只经显式 DTO 映射。Domain 与 Events 不引用场景对象，只走稳定 ID。

### 3.2 已实现边界

有代码与测试。
- 时间与月度事务。起点 1560-04-01，范围 1560-01-01 到 2000-12-31。`TryAdvanceToNextMonth` 按产业、研究、教育事件排程、提交的顺序计划，任一步异常整体回滚。
- 确定性随机。按 `RandomStreamKind` 分流的持久流，死亡婚配任务掷骰按 `PersonId` 序消费。
- 事件系统。允许列表条件 `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`、`RelationshipAtLeast`、`RelationshipDelta`，另外 `CampaignDateIs`（`year`/`month`）供 M08C。
- 继承。`SuccessionService` 交接、危机收养、退休、待处理事件阻塞继承动作、继承缺口统计。
- 编年史。`HistoryLedger` 追加式，面板只读，读档不追加。
- 产业与市场。M08B 双商品、价格压力公式、连续短缺死亡加成。
- 人口。M08A 年度死亡、出生、自主婚配都在月度事务内。
- 任务。M05 护送委托，M06 战术任务手动与委派，M07 统一五档结果与一条结算路径。
- 剧本内容。SC-010 标准与危机、SC-013 固定三代、SC-017 只读家谱图、SC-020/021/022/023 独立剧本、SC-024 家训、M08C 五个历史节点。
- M09 Debug Mode。`DebugSession` 强类型原子命令、`DebugModePanel` 五页签，仅在 Editor 与 Development Build 可用。

只有数据或占位。
- SC-014、SC-015、SC-016 三条跨代链只有事件目录与中文文本，无代码无测试。
- 设施仍是锁定占位。
- NPC 自治只覆盖两名 pilot。
- 家谱图严格只读，无编辑 UI。
- M08B 长跑模拟器是 EditMode 与宿主侧工具，不是玩家功能。

### 3.3 存档与迁移

- 单槽原子存储，写在 `Application.persistentDataPath`，临时文件替换，保留 `save-slot-1.json` 与 `.bak`。
- 版本化 DTO 加 `SaveDataValidator`。`CurrentVersion` 以代码为准（M08B 起为 17），`CurrentRulesVersion` 为 8。
- 迁移链 v1 到 v17 逐版本补齐，每支只写纯净的新块，不伪造历史。v16→v17 只搬一个月口粮，两指数 1000‰，短缺归 0。
- 两套版本号。`saveFormatVersion` 管结构，`rulesVersion` 管规则。SC-020 是 v7/rules 3，SC-021 到 SC-023 是 v8/rules 4，M05 是 v14，M07 是 v15，M08A 停在 v16，M08B 到 v17。
- 读档拒绝不可能组合，例如阶段与结果不匹配、`roll` 超出 0..99、in-progress 指派人不合法。失败不改状态，不静默修复旧档。

### 3.4 场景与生成器

- 唯一长期场景是 `Assets/Scenes/FoundationScene.unity`，根路径固定，例如 `Canvas/M6TitleRoot`、`Canvas/SC020EducationRoot`、`Canvas/SC022FamilyFormationRoot`、`Canvas/M2WorldMapRoot/TownMenuRoot/M5ShadowEscortCommissionRoot`。
- 场景改动只走事务式 updater。单一固定根、一个 Undo 组、先验证后保存、异常用 Undo 回滚且不重载场景。
- updater 只识别并替换精确的旧层级，遇到其他部分对象直接拒绝，可重复执行，负向路径留回滚测试。
- 不手改 `.unity` 与 `.prefab` YAML。`SampleScene.unity`、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json` 属用户 WIP，保持不动。
- 生成后必须让 `FoundationSceneValidator` 复验，报告写 `Logs/SceneValidationReport.txt`，读时分行看，既有失败项会拉低总结果。

### 3.5 验证工具链

- `com.unity.pipeline` 0.8.0-exp.1 加 `unity` CLI。`unity pipeline list` 给实例与端口，Editor 端口 7800–7849。
- 已验证命令。`editor_status`、`open_scene`、`editor_play`、`editor_stop`、`editor_focus`、`recompile`、`recompile_status`、`console`、`console_status`、`run_script`、`capture_game_view`。
- `run_script` 在 `Assets/` 外编译 C# 直打项目程序集，无域重载，适合用生成输入跑真实游戏代码。
- 客户端约 30 秒超时，Editor 仍继续跑。长跑按可恢复分块推进并读批次日志，传输超时不是游戏错误。
- `capture_game_view --save_path` 只能落到 `Assets/` 下。截图写 `Assets/Logs/verify/_scratch/`，用完删除 `Assets/Logs` 与 `Assets/Logs.meta`。
- Play Mode 在 Editor 失焦时会停帧，先 `editor_focus`。
- 六类证据分写。编译、EditMode、场景验证、Console、Play Mode、用户验收互不替代。宿主侧 Roslyn 或 harness 结果不算 Unity 运行证据。

---

## 4. 全部 checkpoint

状态含义。已验收 = 用户明确接受。进行中 = 有部分证据未闭合。待验证 = 只有静态或部分证据。历史 = 已被后续取代的快照。

| 日期 | ID | 内容 | 状态 | 证据 |
|---|---|---|---|---|
| 2026-11-18 | v1 集成 | 五个 UI 分支并入 `main`：层序统一为 `CanvasLayerOrder`、场景在 main 侧重放、全量回归与演示构建全过、独立复审 PASS | 进行中 | 全量 EditMode 2214/2214、Validator `PASS; Errors: 0`（源 `c291dd3`）；`Builds/Demo-v1-2bca9d1-r2/` 由 `2bca9d1` 干净工作树构建 `Succeeded`、`totalErrors=0`；用户最终验收未完成 |
| 2026-11-18 | M00 | 主工作树未提交项与 `panyeung/m00-closeout` `e57dd06` 内容等价：35 个已跟踪修改文件 `git diff --ignore-cr-at-eol` 为 0 行，34 个未跟踪文件与同名 blob 剥 CR 后逐字节相同，仅 5 项是本地新增（合计 69 项等价） | 进行中 | `git status --porcelain -uall` 74 项（35 修改 / 39 未跟踪）；本地新增为 `Assets/Logs/verify/_scratch/t3193-00-now.png`、`Assets/Scenes/TestResults_20261108_072929.xml` 与其 `.meta`、`scripts/verify/SaveGatePlayProbe.cs`、`scripts/verify/SaveLoadPendingChecks.cs`；分支现头 `5141474` |
| 2026-11-18 | M00 | Editor 卡死恢复：PID 88424 在一次测试超时后由 `RestoreSceneSetupTask` 抛 `IndexOutOfRangeException` 卡死主线程（pipeline 全超时、UIA 不可用），按 PID 结束该进程并重启为 PID 96584 / 端口 7800 | 进行中 | `unity pipeline list` 现报 `My project` 96584 / 7800 / 可达 true；场景备份 `Logs/recovery-20261118/scene-0.backup` 与 `__Backupscenes/0.backup` 各 835,768 字节，MD5 同为 `5ca370eb0407f1d1f1ac2131063ffd1e` |
| 2026-11-18 | M00 | 场景恢复不再自动：`Library/LastSceneManagerSetup.txt` 内容为 `sceneSetups: []`，重启后须显式 `open_scene Assets/Scenes/FoundationScene.unity` | 进行中 | 核对时 `list_open_scenes` 报 FoundationScene 已加载、`isDirty=false`、`rootCount=4`；同刻 `editor_status` 报 `playMode=playing` |
| 2026-11-18 | v1 可玩里程碑 | 实现规格 §3 七条逐条跑过：1 标题、2 SC-013 营地 HUD（1560-04-01 / 清洲）、3 月结（04-01→05-01，资金 30→28）、4 名册六人跨三代 + 编年史、5 一次家主更替（晴信 32 岁→正綱 24 岁，世界不重置）、7 清洲四条活动可列并点开密信任务页，共 6 条 PASS | 进行中 | `docs/reports/20261118-playable-milestone.md`（同文在 `Logs/verify/20261118-playable-milestone.md`）；每条的读回值都记在该文件分项表 |
| 2026-11-18 | v1 可玩里程碑 | 第 6 条存读档 BLOCKED：正常开局（SC-010 标准样例）下点存档被拒，`SaveLoadPanel.Status=调试会话不会写入玩家存档。`，`slotExists=False`；同时 `M09DebugModeRoot` 的 `DebugClose`/`DebugTab0-4`/`DebugReset`/`DebugExit` 可交互，而场景文件 `m_IsActive: 0`，说明 DebugSession 是运行期被激活的，触发来源未定位 | 待验证 | 隔离槽已生效（`store=…\ISO-Milestone-20261118`、`isDefault=False`、`titleControllersRedirected=1`）；真实槽 SHA-256 `1e61ed9c…`/`f1c0630e…` 与 mtime 全程未变；最小复现四步写在证据文件末节 |
| 2026-11-18 | v1 基线 | 本轮实测全量 EditMode 2141/2141（0 失败，05:21:21Z–05:22:31Z）与场景 Validator `PASS; Errors: 0`，测试与 Validator 证据见 `Logs/verify/20261118-milestone/` | 进行中 | 作业 `bc1a32ce920d461fbe4f929335f22258`；`TestResults.xml` 副本与 `Logs/SceneValidationReport.txt` 末行 |
| 2026-11-18 | M00 | 撤回「城镇活动只有可编译骨架」：该结论已过期，行为与断言已落地 | 进行中 | 5 个 Town 测试文件共 31 个 `[Test]`（Entry 7 / Routing 6 / LocalActions 5 / OverviewResident 1 / UpdateTownOverview 12）；最新全量 `TestResults.xml` 2026-11-18T04:41:54Z–04:43:11Z，`testcasecount=2141`、`passed=2141`、`failed=0` |
| 2026-11-17 | M05 | 独立复审**未通过**：`tester` 只读复核 `panyeung/ui-m05-management@2f8b7b2` 后判定「已推送提交不包含 checkpoint 声称的实现」——`ScrollRect` / `CarryIntoReport` / `LateUpdate` / `FitReportRegion` 与三条裁切回滚回归测试全部只存在于该工作树**未提交**区；工作区实测为 45/45、2010→2012/2012，`2f8b7b2` 对应的是旧的 42/42、2008/2008 | 待验证 | 任务卡 `t_e2ddb378` 评论（C1 结构合规通过、C2 有疑点、C3 存档兼容通过、C4 既有行为不变通过、C5 证据不自洽、C6 合并风险）；另有 `docs/PLAYTEST_GUIDES.md` 在 main 已删除、工作树仍修改的 modify/delete 冲突。修正后已推送 `b1d1062`，复审待跑 |
| 2026-11-17 | M00 | 主工作树 M00 证据链全项通过：Validator `PASS; Errors: 0`、EditMode 2141/2141、Play Mode 到 SC-013 营地 HUD（1560-04-01 / 清洲）期间新增 error 0、隔离槽存读档 SAVE-1～SAVE-4 全过 | 进行中 | `Logs/verify/gate-20261118/` 下 `report.md`、`save-isolated.md`、`save-gate.md`；真实存档 `1e61ed9c…` 与备份 `f1c0630e…` 前后哈希未变；SAVE-3 改在 SC-010 标准样例验（SC-013 退休被家议链门控、无候选选择界面）；用户最终验收未完成 |
| 2026-11-17 | M04 | 收口全绿：场景层序缺陷已修（新增共享 `CanvasLayerOrder`，SuccessionRoot 6→17 压住家族名册页），Validator `PASS; Errors: 0`、全量 EditMode 2023/2023、1280×720 Play Mode 到 SC-013 期间 0 新 error | 进行中 | 分支 `94e82bf` 已推 `origin`；`ui-m04-family/Logs/verify/m04-closure-20261118/report.md`、`m04-fix-20261118/report.md`；仍差 28 个未提交文件未入分支（2026-11-18 复核 `git status --porcelain -uall`） |
| 2026-11-17 | M04 | 场景语义 diff 复核：3541 对象、`m_Name` 1908、`m_SizeDelta` 834、`m_AnchoredPosition` 多重集前后全等；真实差异仅 36 处 `LayoutElement.m_MinSize` 归一为 10 与 3 处 `-0` 旋转归一 | 待验证 | 16257 增 / 16252 删几乎全是 Unity 重序列化；该工作树的 EditMode 与 Play Mode 证据另计 |
| 2026-11-16 | M00 | 主工作树只读回归全量 EditMode 2140/2140，Validator PASS/0 errors | 进行中 | `Logs/verify/gate-20261116/report.md`；Console 6 errors / 52 warnings / 24 logs，非零错误验收 |
| 2026-11-16 | M00 | `main` 在 `d57ea7c`，四个任务工作树保留，无合并、无删除 | 历史 | `ui-m05-management` 在 `2f8b7b2`，多一个已推送提交 |
| 2026-11-16 | M04 | 补 1280×600 约束用例后聚焦 3/3、全量 2002/2002 | 进行中 | 预期面板高 440、摘要条带高 40；`ui-m04-family/Logs/m04-followup2/report.md` |
| 2026-11-16 | M04 | 场景保存后 Validator `PASS; Errors: 0`，23 项 PASS | 待验证 | 3541 文档与 837 GameObject 前后一致，GUID 未变；语义 diff 偏宽 |
| 2026-11-16 | M05 | SC-019 月报改中性文案并补回滚与裁切回归，聚焦 42/42、全量 2008/2008 | 进行中 | SC-013 月结 Console 0/0/0；独立复审未完成 |
| 2026-11-16 | M06/M07 | `CampaignRevision` 定为战役会话修订计数，换战役清战术队伍预览 | 待验证 | `ui-m06-m07-missions/.../20261116-0927-campaign-revision.md`；全量 1997/1997 |
| 2026-11-16 | M08 | 继承决策期间评估页让出焦点，逐阶段焦点 EditMode 1/1 | 进行中 | `ui-m08-campaign/Logs/verify/20261116-0953-m08-recapture-focus.md` |
| 2026-11-15 | M00 | 混合工作区全量 2094/2094，随后 2101/2101 | 进行中 | `Logs/verify/20261115-1127-current-worktree-regression.md`；Validator PASS，Console 0/0/0 |
| 2026-11-15 | M09 | Debug Mode 全量 2092/2092，Validator PASS | 待验证 | 复审 `task_00de916d4679` ACCEPT；`Logs/verify/20261115-m09-debug-mode-playmode.md` |
| 2026-11-15 | M09 | 隔离会话家谱 16 人/6 代/21 条关系，会话总览 23 人/29 条关系 | 待验证 | 日期跳转 1560-04-01 → 1561-01-01 跳过 275 天 |
| 2026-11-15 | M05 | 美术接入补 `Updater_RepairArtRollbackRestoresExistingSealAndButtonArt`，聚焦 31/31 | 待验证 | 22 张 PNG 尺寸与清单相符；`Logs/verify/20261115-2158-m05-art-compliance.md` |
| 2026-11-15 | M02 | 档案页缩放修复用 `Create` helper 建三个 body，Validator 强制 `localScale` 为 1 | 待验证 | `06a7a04`、`0ebc4cd`；1280×720 目视通过 |
| 2026-11-15 | M08A | Debug seed 39 内存局逐月结算到 1561-02-01，人口 9→10 | 进行中 | 聚焦 Pregnancy 30/30、在途妊娠 Save 2/2、性别迁移 1/1 |
| 2026-11-15 | M08A | 普通 SC-013 路线验证年度自主婚姻，v17 存读档后配偶与五条 RNG 相等 | 进行中 | `docs/reports/m08-campaign-review.md` 对应节 |
| 2026-11-15 | M08A | seed 46 路线千代 1560-06-01 受孕、1561-03-01 生頼光，人口 4→5 | 进行中 | 默认 seed `0x5343414E4B4F3031` 未验证 |
| 2026-11-14 | M08C | 用户明确验收史实世界时间线 | 已验收 | lead 技术门：聚焦 9/9 与 13/13、全量 1973/1973、Validator PASS、SC-010 Play Mode 过 1578/1579 |
| 2026-11-14 | M08C | 五个史实节点共 10 条事件，内容版本 3→4，schema 与 rules 不变 | 历史 | `TestResults.xml` 1971/1971 早于两条新增用例；提交 `616f4be` 已推 `origin/main` |
| 2026-11-14 | M07 | `main` 整合 Play Mode，M05 手动部分成功 42/32，M06 第 18 回合 `CostlySuccess` | 进行中 | M06 结算 +4 资金、+6 情报、-3 物资，名望 1→3 |
| 2026-11-14 | M06 | 三个新建内存局验证严重失败死亡风险，三次均负伤无人死亡 | 进行中 | `Logs/verify/20261114-m06-head-death-followup.md` |
| 2026-11-14 | M06 | 内存注入随机流观测家主阵亡并在继承面板确认正綱接任 | 进行中 | `docs/reports/m07-runtime-verification.md` |
| 2026-11-14 | M08 | 长周期评定到 55/100 年；1000 种子基线 605 灭亡、395 待继承、0 到达 50/100 年 | 进行中 | `farm-managed-v1` 为 870/68/62 |
| 2026-11-14 | M08A | 真实月结到 1562-01-01，人口 7→6，两人自然离世 | 进行中 | `Logs/verify/20261114-m08a-natural-death-playtest.md` |
| 2026-11-14 | M08A | 1562-01-01 存档后推进再读档，人口 6 与已故标记及编年史保留 | 进行中 | `save-slot-1.json` 与 `.bak` 哈希逐字节匹配 |
| 2026-11-14 | M08A | 重复继承到 1563-01-01 人口 4、两次确认继承；SC-022 到 1562-01-01 人口 4→2 | 进行中 | 两轮均未观察到出生 |
| 2026-11-14 | M00 | 全量 EditMode 1959/1959，89.14 s | 进行中 | `Logs/verify/20261114-1021-m00-full-editmode.md` |
| 2026-11-14 | M00 | 本地 `main`、`origin/main`、GitHub `main` 同点 `6c282ab` | 历史 | 随后提交并推送 `814bcf9` |
| 2026-11-14 | M00 | PID 77116 上 51/51，Console 0/0/17 | 历史 | `TestResults.xml` 2026-11-14T09:12:34Z |
| 2026-11-14 | M00 | 基线刷新快照 `959e59b`，提交含 160 路径 | 历史 | 全量 1947/1947 与 1949/1949；XML SHA-256 `6F7C08B7…E55E8` |
| 2026-11-14 | M00 | 项目只剩 `main` 与主工作树，SC-021 detached `66373ee` 备份后移除 | 历史 | 备份在 `Documents/Codex/worktree-backups/2026-11-14-sc021-scene-integration` |
| 2026-11-13 | M00 | 只读基线快照，Editor PID 63756，HEAD `61484ae`，Build Settings 只启用 `FoundationScene` | 历史 | GUID `be44f8a1…d930`，`isDirty=false` |
| 2026-11-13 | M08A | 年界委托先结算死亡执行人的问题修复 | 进行中 | `GameSessionFamilyLifeCycleTests` 15/15、`M05CommissionSessionTests` 35/35 |
| 2026-11-13 | M08A | 人口 HUD 改读 `MarketSettlement.CountLivingFamilyMembers` | 进行中 | `M02DailyActionPanelTests` 34/34；真实月结人口 6→7 |
| 2026-11-13 | M03 | 补伤病、零库存、产业职责冲突三条限制 | 进行中 | `NpcAutonomy` 定向 86/86；复审 `task_51001e836828` |
| 2026-11-13 | M03 | 标题页新建 SC-013 普通路线复验，1560-04-01 六人，Inspector 只读 | 进行中 | `Logs/verify/20261115-1112-m03-normal-playmode.md`；Console 0/0/0 |
| 2026-11-12 | M08A | 年度人口动态落地，全量 1823/1823，sweep `violations=0`、`self-test=PASS` | 进行中 | 当时 schema v16；Validator PASS |
| 2026-11-12 | M08B | 多商品经济与 100 年/1000 种子真实 `GameSession` 模拟获批并实施 | 进行中 | Orca Run `run_125d614abb6b` |
| 2026-11-12 | M07 | 实现与运行验证轮全量 1710/1710，覆盖四类结局与严重失败 | 进行中 | 聚焦 46/46、19/19、34/34、48/48、25/25 |
| 2026-11-12 | M06 | 战术面板在 `FoundationScene` 生成保存后全量 1710/1710，Validator PASS | 进行中 | `Logs/verify/20261111-m06-tactical-mission.md` |
| 2026-11-11 | M07 | 三层整合规则获批准，计划复审 ACCEPT 后开始实施 | 进行中 | v15/rules 8 存档 |
| 2026-11-11 | M05 | 护送委托完成代码、存档、场景接入 | 进行中 | `ReferenceGalleryNavigatorTests` 14/14、Commission 筛选 104/104 |
| 2026-11-11 | M02 | InputField 生命周期修复推送 `bc17491` | 进行中 | 全量 1645/1645 |
| 2026-11-10 | M03 | 第二轮复审修正后聚焦 140/140、全量 1274/1274 | 进行中 | Validator PASS；M03 根节点恰好 1 个 |
| 2026-11-10 | M04A | 个人行动记忆技术签收，有界记忆、评分修正、v12 迁移 | 进行中 | 聚焦 181/181、全量 1381/1381 |
| 2026-11-10 | M04B | 动态出生 lead 技术验证，全量 1446/1446，妊娠与出生存读档通过 | 进行中 | `docs/reports/m04b-pregnancy-simulation.md` |
| 2026-11-10 | M00 | HEAD `bd29863`，全量 1454/1454 | 历史 | `SampleScene` 与 `FoundationScene` 启动关系未定 |
| 2026-11-09 | M02 | 专项全量 1386/1386、Validator PASS、复审 ACCEPT、两种 Play Mode 尺寸通过 | 进行中 | 960×720 与 1280×720 |
| 2026-11-08 | M00 | HEAD `e298312`，EditMode 1303 passed | 历史 | `Assets/Scenes/TestResults_20261108_114927.xml` |
| 2026-11-07 | M01 | 用户明确验收固定三代可玩原型 | 已验收 | `main` 含 SC-024 场景接入；HUD 修复 `18c7b4c` |
| 2026-11-06 | SC-024 | 家训与个人抱负切片，聚焦 76/76，schema v9 与 rules 5 | 历史 | 场景当时无 `SC024` 对象 |
| 2026-11-06 | M02 | 每日行动与旅行基础 UI 接线，聚焦 44/44，Validator PASS | 历史 | 六人名单与两日休息 0/2→1/2→完成 |
| 2026-11-05 | SC-023 | 隐居与后见人切片，放宽退休前任家主导师资格，复审 ACCEPT | 待验证 | `a2c7a48` 推送 `origin/panyeung/sc023-seclusion`；未跑编译测试场景 |
| 2026-11-04 | SC-022 | 婚姻与普通收养切片，P2 修正后聚焦 70/70 | 进行中 | 整体 Validator `FAIL; Errors: 4` 均为既有缺口 |
| 2026-11-04 | SC-021 | 家族研究切片，聚焦 126/126、全量 1103/1103，schema v7→v8、rules 3→4 | 进行中 | SC-021 行 PASS，整体 Validator `FAIL; Errors: 1` |
| 2026-11-04 | SC-020 | 子女培养切片，Roslyn 0 诊断，纯 Domain 17/17 与存档 20/20 | 待验证 | schema v6→v7、rules 2→3；未跑 Unity EditMode |
| 2026-11-04 | SC-019 | 家族产业首切片，全量 923/923，Validator PASS | 进行中 | 修正前基线 921/921 |
| 2026-11-04 | SC-018 | 独立家族事件把目录补到 30 项并维持 5 条跨代链，EditMode 824/824 | 进行中 | schema v4→v5 |
| 2026-11-03 | SC-017 | 通用家谱改只读数据驱动可滚动展示 | 已验收 | checkpoint `3ae2ade`，标签 `demo-SC017` |
| 2026-11-03 | SC-016 | 委托旧约四条 Play Mode 路线到达第三代，EditMode 778/778 | 进行中 | Console 0 errors / 14 warnings / 8 logs |
| 2026-11-03 | SC-015 | 三代家书 A/B/C/D 均到达第三代 | 待验证 | 无 EditMode、场景验证与存读档证据 |
| 2026-11-03 | SC-014 | 流亡少年四条事件节点切片，两轮复审 ACCEPT | 待验证 | 未运行 Unity、测试、场景或 Play Mode |
| 2026-11-03 | SC-013 | 固定三代剧本，程序集 0 错误，harness 30/30 与 33/33 | 待验证 | `FoundationSceneValidator` 未专门运行 |
| 2026-11-03 | SC-012 | 家族编年史切片，schema v3→v4 | 已验收 | checkpoint `4443ccc`，标签 `demo-SC012` |
| 2026-11-02 | SC-011 | 跨代事件链，内容版本 2、存档 1→2 | 已验收 | 标签 `demo-SC011` |
| 日期未记 | SC-010 | 初代家主退休与继承 | 已验收 | `af35633`，标签 `demo-SC010` |
| 日期未记 | SC-004 | 月度推进与地图月末按钮接入 | 已验收 | `c29d24a`，标签 `demo-SC004-SC009` |
| 日期未记 | SC-005–SC-009 | 随机、版本化存档、事件校验、事件引擎、选择 UI | 已验收 | 同上提交；EditMode 548/548，复审 ACCEPT |
| 日期未记 | SC-001–SC-003 | 基线审计、纯 C# 核心边界、人物家族关系模型 | 历史 | `docs/SC001_BASELINE_AUDIT.md` |

---

## 5. 当前状态

- `main`、`origin/main`、`integration/v1-20261118` 三点同一提交 `2bca9d1`（「语言合规：修正剩余 6 处「城镉菜单」错字为「城镇菜单」」）。`git log main..integration/v1-20261118` 输出为空。本节由收口卡 `t_080705e2` 用命令逐条读回，不是转述汇报。收口卡随后只在 `main` 上追加文档提交，代码树仍是 `2bca9d1`，未合并、未打 tag。
- 只有一个 Git 工作树，就是工程根目录 `C:\Users\panye\My project`；`git status --porcelain` 为空；`git worktree list` 只有一行。
- 五个任务分支都已并入 `main`：`panyeung/m00-closeout`、`ui-m04-family`、`ui-m05-management`、`ui-m06-m07-missions`、`ui-m08-campaign` 逐个 `git merge-base --is-ancestor` 为真。相对合并起点 `d081c46` 共 51 个文件、+10077/−1821。
- 全量 EditMode `2214/2214` passed、`failed=0`、`skipped=0`（2026-11-18 13:22:34Z–13:24:40Z，源提交 `c291dd3`），证据 `Logs/verify/20261118-integration-full/TestResults.xml`。F1 两条用例 `TownMenu_FacilitySlotsListRealTownEntries` 与 `TownMenu_FacilitySlotsStayEmptyWhenTheTownPageRenders` 在该 XML 内均为 Passed。
- 场景 Validator `RESULT: PASS; Errors: 0`。证据 `Logs/verify/20261118-integration/scene-validator-c291dd3.txt` 末行，与 `Logs/SceneValidationReport.txt` 末行一致。
- `c291dd3..2bca9d1` 两个提交只改注释与错字（4 个文件、−2/+7），未动逻辑、断言与场景。这是「测试证据基于 `c291dd3` 仍能代表 `2bca9d1`」的依据，由 `git diff -U0` 逐行读回。
- Play Mode（隔离槽，源 `c291dd3`）：结束月份 1560-04-01→05-01 且资金 30→28、清洲四条当地委托可见、存读档往返、SAVE-3 改选继承读回、SAVE-4 空槽拒绝，全 PASS；真实存档槽 `sha256 1e61ed9c…` 与 mtime 未变。证据 `Logs/verify/20261118-integration/20261118-1348-core-loop.md`。
- 演示构建：`Builds/Demo-v1-2bca9d1-r2/`，源提交 `2bca9d1`（干净工作树），`Succeeded`、`totalErrors=0`、218 个文件 183,882,557 B，exe 667,136 B、`sha256 7c5b8f3e…188247d`（本卡亲跑 `sha256sum` 核对）。唯一警告是 Pipeline 包自带的「No RuntimePipelineConfig asset found」，不再是「未编译改动」。Player 冒烟走通标题→开始新游戏→SC-013→营地 HUD（1560-04-01、清洲、贯 30）→结束月份（1560-05-01、贯 28）→清洲城镇页四条当地委托→任务页，`Player.log` 0 条 Exception/LogError/NullReference。
- 画布层序已统一：`Assets` 内 `CanvasOverlayOrder` 零命中，`CanvasLayerOrder` 是唯一页面序来源。
- 独立复审 `t_122d4d5b` 判 **PASS**，C1–C6 全过。
- 仍未闭合：用户最终验收（M00）；M08 战役回顾面板的运行时打开；M09 真机窗口拖动；`Assets/Tests/PlayMode` 为空，运行时验证仍靠手工配方；其余 MINOR 见 §6。
- 本机当前没有可连接的 Unity Editor（`unity command editor_status` 报未找到实例），任何复测前先 `unity open`。
- 证据边界。M01 与 M08C 子切片的验收都不代表 M00 或 M02–M08 整体验收。

---

## 6. 下一步

按依赖顺序，前一项不闭合不做下一项。开始任何 Unity 工作前先 `unity open` 拉起本工程 Editor，`unity pipeline list` 按 `-projectPath` 与 PID 对号后再动手。

1. **用户最终验收（唯一硬门）**：跑 `Builds/Demo-v1-2bca9d1-r2/Demo-v1-2bca9d1-r2.exe`，走标题→开始新游戏→SC-013→营地 HUD→结束月份→地图→清洲城镇页→当地委托。验收通过后由用户点头，才谈里程碑 tag。
2. M08 战役回顾面板的运行时打开：面板只在真实 55 年阶段回顾或 100 年结局待处理时显示，需要一条长周期演示路线给证据。不以调试跳日期取证。
3. M09 窗口拖动：手上没有真实硬件下的窗口拖动证据，需在可交互桌面上复验。
4. `Assets/Tests/PlayMode` 目前为空，运行时验证全靠手工配方。要么补自动化，要么把「手工配方即口径」写进规格。
5. MINOR 清理（`Assets/Scenes/` 的 stray `TestResults_*.xml`、`Assets/Logs/verify/_scratch/` 截图、隔离槽目录）需要批准。
6. 未合入分支。SC-021 场景集成在 `panyeung/sc021-scene-integration`，SC-023 在 `origin/panyeung/sc023-seclusion`，都未合并未打 tag。
7. 画布层序已统一为 `CanvasLayerOrder`，后续任何新面板都必须按这套共享序插入并让 `CanvasLayerOrderTests` 覆盖，不要再引入第二套顺序表。

---

## 7. 风险与阻塞

- 工作区保护的既有 WIP 长期未收敛。`SampleScene.unity`、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 均保留未提交。
- 共享 `TestResults.xml` 被并行测试争用，M00 报告的 error 部分归因于此。并行跑测试会互相覆盖证据。
- 历史工件不能代表当前状态。M08C 的 1971/1971、M06/M07 的 1991/1991、M07 的 1710 项 XML 均已被覆盖或早于后续修改。
- 场景语义 diff 过宽。M04 保存后除预期注册外还含尺寸归一化与 sibling 顺序变化。
- 场景未接入 HEAD。`git show HEAD:Assets/Scenes/FoundationScene.unity` 中无 `SC024` 相关对象，接入只在未提交工作区。
- 测试工具收尾异常。PID 88424 的崩溃现场已被重启覆盖，现 `Logs/Editor.log` 与 `%LOCALAPPDATA%\Unity\Editor\Editor.log` 中 `IndexOutOfRange` 命中 0 次；崩溃经过只留在本节与 `Logs/recovery-20261118/` 的场景备份里，没有原始调用栈。`IPostBuildCleanup` 页脚仍会出现，属既有噪声。
- M08B 生存结论不成立。50/100 年存活样本为零，风险接受不等于生存通过。
- 输入路径未证实。窗口拖动返回 `synthetic_input`/`unverified`，坐标点击返回 `actionUnsupported`，实体键盘未单独验证。
- Pipeline 请求多次超时。30 秒客户端上限与 `run_tests` 600 秒超时都曾中断验证流程。
