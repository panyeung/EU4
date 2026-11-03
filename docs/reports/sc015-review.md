# SC-015 三代家书独立只读审阅

日期：2026-11-03  
分支：`feature/sc013-fixed-three-generation`  
`HEAD`：`88fe7c6`（SC-013/SC-014/SC-015 均未提交）  
工作区：`C:\Users\panye\My project`  
审阅对象：`chain.sc013.family-letters` 的七个事件、十个选项、二十七条中文文本，以及 SC-015 实现报告与相关文档。  
结论：**ACCEPT**

## 0. 结论摘要

七个节点构成一条完整、互斥、只由既有事件历史驱动的三代分支；第一代入口、第二代两个互斥节点、第三代四个互斥结尾的门槛、来源选项、文本引用、一次性范围与空效果声明均与设计稿、计划和实现报告一致。以实现前工作区快照为基线，SC-011、SC-013、SC-014 的既有链、事件、选项与文本逐项 JSON 相等，SC-015 为纯追加。未发现需要修正的阻断问题。

本审阅为**静态只读检查**：读取了事件目录、中文文本、SC-015 文档以及为确认规则语义所必需的现有代码。**没有**运行 Unity、编译、EditMode 测试、Console 检查、场景验证、Play Mode、存读档试玩或用户 Play Test；这些项目在本文末尾统一列为未验证。

## 1. 审阅范围与方法

已读：

- `docs/superpowers/specs/2026-11-03-sc-015-three-generation-family-letters-design.md`
- `docs/superpowers/plans/2026-11-03-sc-015-three-generation-family-letters.md`
- `docs/reports/sc015-implementation.md`
- `SC015_DEMO.md`
- `Assets/Resources/Events/event-catalog-v1.json` 的 SC-015 追加部分及既有条目
- `Assets/Resources/Localization/event-text-zh-v1.json` 的 SC-015 追加部分及既有条目
- `PROJECT_STATE.md`、`ARCHITECTURE.md`、`DECISIONS.md` 的 SC-015 小节

为确认数据语义，只读查看了以下既有实现（未审阅其无关逻辑）：

- `Assets/Scripts/Events/EventEngine.cs`：排程排序、条件求值、选项提交、一次性标记。
- `Assets/Scripts/Events/EventRuntimeState.cs`：`PerGame` 标记、历史追加、存读档字段。
- `Assets/Scripts/Events/EventCatalogValidator.cs`：允许列表条件/要求/效果、链与来源引用校验。
- `Assets/Scripts/Application/GameSession.cs`：`EventRuleContext` 的构造与 `scenario`/已结算交接数。
- `Assets/Scripts/Application/M2FixedScenario.cs`：M2 退休门禁与固定继承人。
- `Assets/Scripts/Presentation/M2EndingPanel.cs`：终局面板可关闭、不阻断时间推进。

静态检查方法：

1. Python 3.14.8 解析两个 JSON。
2. 以实现前快照为基线，按列表下标和 ID 映射做深度 JSON 相等比较。
3. `diff --strip-trailing-cr` 检查是否纯插入；`git diff --numstat` 复核相对 `HEAD` 的行数。
4. 独立脚本检查 ID 唯一性、文本引用、链成员、条件门槛、来源选项唯一性、空效果与优先级关系。
5. 独立小模拟器按 `EventEngine` 的“优先级降序、每月取最高优先级组”规则走四条分支，验证调度顺序与互斥性。该模拟不是 Unity 运行证据。
6. 文件系统 mtime 检查 SC-015 窗口内的实际改动文件。

## 2. 基线与可追溯性

实现前快照保存在工作区之外：

```text
%LOCALAPPDATA%\Temp\opencode\sc015-pre\event-catalog-v1.json
%LOCALAPPDATA%\Temp\opencode\sc015-pre\event-text-zh-v1.json
```

快照 mtime 为 `2026-11-02 17:31:57`，早于事件目录的 `17:32:40` 和文本目录的 `17:35:52`。快照本身的内容与上一轮 SC-014 审阅记录的基线一致：

| 证据 | 结果 |
|---|---|
| 快照规模 | 3 条链、9 个事件、50 条文本；无任何 `family-letters` 行 |
| 快照相对 `HEAD` | 事件目录 `286/0`、文本 `160/0`（纯追加） |
| 与 `docs/reports/sc014-review-2.md:54` 记录一致 | 是（`286/0`、`160/0`） |
| SC-011 三条事件、`chain.rainy-arrival`、18 条既有文本相对 `HEAD` | 全部 JSON 相等 |
| SC-013 承诺/终局与 SC-014 四节点的优先级、条件、效果 | 与 `sc014-review-2.md:48-51` 记录一致 |

本次审阅独立计算的 SHA-256：

```text
catalog current  = afea1fadb46b75cb5df28c33c54372faab8c01c12a11d029971bd2b4c5321e83
catalog snapshot = ae532adc353e887516678bdf24d66fd1f386c4632ecf67a6d5469cc5d120609a
text current     = f1b2dfd54bf7e5d38ccd2a404d36ef5e92f92d0c4de2c705f3ca07643d29ff05
text snapshot    = c85ea74ec45e4f999bb23f76500df0f47d3ff9083b23e53af1dc1d65582c038a
```

注：快照位于临时目录，不在版本控制中。它之所以可作为“实现前状态”的证据，是因为其 mtime 早于写入、规模与 SC-014 审阅基线一致、SC-011 与 `HEAD` 完全相等，且 SC-013/SC-014 的结构与上一轮审阅记录吻合。该限制不影响本次“当前内容 = 快照内容 + SC-015 追加”的结论。

## 3. 七个事件分支

| 世代 | 事件 | 条件 | 优先级 | 选项 | 后续 |
|---|---|---|---:|---|---|
| 第一代 | `event.sc013.family-letters.first` | `SuccessionCountEquals 0` | 6 | `...first.reveal`、`...first.seal` | 分别进入 second-open / second-sealed |
| 第二代（公开） | `event.sc013.family-letters.second-open` | 第一代 `reveal` + `SettledSuccessionCountAtLeast 1` | 60 | `...second-open.preserve`、`...second-open.summarize` | 分别进入 third-full-public / third-short-history |
| 第二代（封存） | `event.sc013.family-letters.second-sealed` | 第一代 `seal` + `SettledSuccessionCountAtLeast 1` | 60 | `...second-sealed.open`、`...second-sealed.keep` | 分别进入 third-opened-by-heir / third-sealed |
| 第三代 | `event.sc013.family-letters.third-full-public` | `second-open.preserve` + `SettledSuccessionCountAtLeast 2` | 600 | `...third-full-public.receive` | 终点 |
| 第三代 | `event.sc013.family-letters.third-short-history` | `second-open.summarize` + `SettledSuccessionCountAtLeast 2` | 600 | `...third-short-history.receive` | 终点 |
| 第三代 | `event.sc013.family-letters.third-opened-by-heir` | `second-sealed.open` + `SettledSuccessionCountAtLeast 2` | 600 | `...third-opened-by-heir.receive` | 终点 |
| 第三代 | `event.sc013.family-letters.third-sealed` | `second-sealed.keep` + `SettledSuccessionCountAtLeast 2` | 600 | `...third-sealed.receive` | 终点 |

所有七个事件均满足：

- `scenario: "sc013"`，`trigger: "Monthly"`，`onceScope: "PerGame"`，`cooldownMonths: 0`，`weight: 1`。
- `chainId: "chain.sc013.family-letters"`，且被该链的 `linkedEventIds` 按上述顺序完整列出。
- 事件/条件/选项的 JSON 键集合与既有 `event.sc013.refugee-*` 行一致，`scenario` 只是既有的可选字段；不存在 Unity `JsonUtility` 会静默忽略的拼写差异。

## 4. ID 与文本引用

| 检查 | 结果 |
|---|---|
| 新增事件 | 7，ID 全局唯一 |
| 新增选项 | 10，ID 全局唯一 |
| 新增文本 | 27（7 事件 + 10 选项 + 10 摘要），ID 全局唯一 |
| 事件 `textId` 解析 | 7/7 |
| 选项 `textId` 解析 | 10/10 |
| 选项 `summaryTextId` 解析 | 10/10 |
| 孤立 SC-015 文本 | 0 |
| 缺失 SC-015 文本 | 0 |
| 重复链成员 | 0 |
| 未链接到声明链的事件 | 0 |
| 所有新文本标题/正文非空 | 是 |
| `contentVersion` | 两文件均为 `2`，且相等 |

`chain.sc013.family-letters.linkedEventIds` 恰好包含七个预期 ID，顺序为 first → second-open → second-sealed → four third nodes，无重复、无遗漏、无额外成员。

## 5. 来源选项可达性与分支互斥

六个 `EventOptionChosen` 条件全部满足：

- 源事件存在，且不是自引用。
- 源选项存在于源事件的 `options` 中。
- 源事件与目标事件同属 `chain.sc013.family-letters`。
- 每个 `(sourceEventId, sourceOptionId)` 组合只被一个事件引用（6 个唯一组合）。
- 两个第二代节点的全部四个选项各自恰好被一个第三代节点引用，无遗漏、无复用。
- 第一代两个选项分别只进入一个第二代节点。

独立模拟按目录优先级运行四条分支，得到与 `SC015_DEMO.md` 一致的顺序：

```text
A: inheritance-pledge -> rainy-arrival -> family.first -> legacy-honored -> second-open -> finale -> third-full-public
B: inheritance-pledge -> rainy-arrival -> family.first -> legacy-honored -> second-open -> finale -> third-short-history
C: inheritance-pledge -> rainy-arrival -> family.first -> legacy-honored -> second-sealed -> finale -> third-opened-by-heir
D: inheritance-pledge -> rainy-arrival -> family.first -> legacy-honored -> second-sealed -> finale -> third-sealed
```

每条分支只出现一个第二代节点和一个第三代结尾；没有路径能同时到达两个第二代节点或两个第三代结尾。按“完成必要事件后立即交接”的流程不会出现任何 SC-015 节点，符合“可选链”的设计。

## 6. 一次性行为

- `onceScope: "PerGame"`，`EventEngine.GetEligibleEvents` 在 `HasOccurredOnce(eventId)` 为真时排除该事件（`EventEngine.cs:327`）。
- 一次性标记在提交时写入（`EventRuntimeState.TryCommit`，`EventRuntimeState.cs:136-137`），不是排程时写入；每个事件在提交后不会再次出现。
- `onceEventIds`、`history`、`committedResultIds` 均进入 `EventRuntimeSaveData` 并在读档时恢复（`EventRuntimeState.cs:46-55, 85-95`），因此静态上一次性行为不会因存读档丢失。
- 事件提交后由 `GameSession.RecordEventChoice` 追加到既有编年史（`GameSession.cs:841-851`），每个已提交事件恰好一条记录，没有 SC-015 专用过滤或伪造数值。
- 未发现重复触发、跨世代重复或“先排程后取消”导致重复的路径；退休门禁与 `TryAdvanceToNextMonth` 都会阻止在有未处理事件时推进。

## 7. 继承门槛

规则语义与数据一致：

- `SuccessionCountEquals` 比较 `EventRuleContext.SuccessionCount`；该值来自 `SuccessionState.SuccessionCount`。
- `SettledSuccessionCountAtLeast` 比较 `EventRuleContext.SettledSuccessionCount`；`GameSession.BuildEventRuleContext` 在继承决策进行中把已结算数置为 `0`，仅在 `SuccessionPhase.None` 时使用实际 `SuccessionCount`（`GameSession.cs:1002-1004`）。
- 世代门槛只在**排程时**检查，提交时不再检查（`EventEngine.cs:346-377` 的 `evaluateSuccessionGates` 参数）；这是既有设计，用于让已排定的稳定事件在后续家主变化后仍可处理，不构成本链缺陷。
- 第一代 `0`：首次交接前唯一入口；交接后 `SuccessionCount` 至少为 1，入口永久失效，符合“不会迟到启动”。
- 第二代 `≥1`：第一次交接已结算后才可能出现；第二代两节点由第一代选项互斥。
- 第三代 `≥2`：第二次交接已结算后才可能出现；四节点由第二代选项互斥。
- M2 退休门禁要求先完成承诺/雨夜来客（第一代）、旧客回访（第二代）与终局（第三代），并且有未处理事件时禁止交接（`M2FixedScenario.cs:185-274`）。因此本链节点不会在第一代/第二代门槛之前被“交接”跳过或提前触发。
- 终局提交后 `M2EndingPanel` 可关闭，且其代码明确说明关闭不改变状态、不阻断时间（`M2EndingPanel.cs` 类注释与 `Close`/`Refresh`）；静态上第三代家书节点仍可在下一个月的排程中出现。该交互本身仍需 Play Test 确认。

## 8. 空效果与文本声明

- 10 个新增选项的 `effects` 全部为 `[]`。
- 10 个新增选项的 `requirements` 全部为 `[{"type": "Always"}]`，没有关系、任务或家族数值门槛。
- 新增事件只使用 `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast` 三种既有条件，没有新增规则类型。
- 新增文本没有声称改变关系、任务、家族数值或存档字段；年龄“晴信已七十”与 SC-013 固定剧本的 70 岁设定一致。
- 分支后果完全由既有事件历史表达；没有新增人物、NPC、关系模型或存档 schema。

## 9. 优先级与降序语义

`EventEngine.GetEligibleEvents` 的排序为：

```csharp
int priority = right.priority.CompareTo(left.priority);   // 数值越大越早
return priority != 0 ? priority : string.CompareOrdinal(left.eventId, right.eventId);
```

（`EventEngine.cs:332-336`）`ScheduleMonthly` 只取最高优先级组，并在组内按权重选择（`EventEngine.cs:145-169`）。因此报告与文档中“priority 降序、数值越大越早”的表述正确。

| 世代 | SC-015 | 同世代 SC-014 | SC-013 必须事件 | SC-011 既有行 | 结论 |
|---|---:|---:|---:|---:|---|
| 第一代 | 6 | 5 | 1000 | 10 | 6 > 5，且 < 10、< 1000 |
| 第二代 | 60 | 50 | — | 100 | 60 > 50，且 < 100 |
| 第三代 | 600 | 500 / 500 | 1000 | — | 600 > 500，且 < 1000 |

- 新节点与既有事件没有相同 priority，因此不会通过权重随机抢占既有行。
- 第一代先于 SC-014 流亡入口、后于 SC-013 承诺与 SC-011 雨夜来客；第二代先于 SC-014 忠诚节点、后于 SC-011 回访；第三代先于 SC-014 援助/竞争节点、后于 SC-013 终局。
- 既有 SC-011 的确定性顺序（承诺 → 雨夜来客 → 旧客回访/门房旧雨 → 终局）未改变。
- `SC015_DEMO.md` 中“每月只排一个最高优先级事件”“需要每次交接前额外结束月份”的描述与代码一致。

## 10. 既有数据保护

以实现前快照为基线，逐项深度比较：

| 对象 | 结果 |
|---|---|
| 3 条既有链 | JSON 完全相等（按 ID 映射与下标双重比较） |
| 9 个既有事件 | JSON 完全相等 |
| 50 条既有文本 | JSON 完全相等 |
| 既有事件/选项/摘要引用 | 无变化 |
| `contentVersion` | 保持 `2` |
| 新增规模 | +1 链、+7 事件、+27 文本 |
| 快照 → 当前原始 diff | 事件目录 `224/0`、文本 `135/0`（纯插入，0 删除行） |
| 当前相对 `HEAD` | 事件目录 `510/0`、文本 `295/0`（纯追加） |
| 快照相对 `HEAD` | 事件目录 `286/0`、文本 `160/0`，与 SC-014 审阅记录一致 |

因此实现报告中“既有 9 个事件（SC-011 3、SC-013 2、SC-014 4）、3 条链、50 条文本逐项 JSON 相等；新增 1 链、7 事件、27 文本”的断言成立。SC-011 另行与 `HEAD` 独立比对，也完全相等。

SC-015 窗口内（17:30 之后）实际发生写入的文件只有：

```text
Assets/Resources/Events/event-catalog-v1.json
Assets/Resources/Localization/event-text-zh-v1.json
PROJECT_STATE.md
ARCHITECTURE.md
DECISIONS.md
SC015_DEMO.md
docs/reports/sc015-implementation.md
docs/superpowers/specs/2026-11-03-sc-015-three-generation-family-letters-design.md
docs/superpowers/plans/2026-11-03-sc-015-three-generation-family-letters.md
```

未发现 `Assets/Scripts`、`Assets/Tests`、`Assets/Scenes`、`Assets/Prefabs`、`ProjectSettings` 或 `.meta` 在该窗口内被修改。设计稿与计划属于协调方维护的规划文件，不在实现 worker 的所有权清单内；本审阅未把它们计入实现改动。工作区中既有的 SC-013/SC-014 与用户 WIP 未被本次审阅触碰。

## 11. 文档一致性

| 文档声明 | 核对结果 |
|---|---|
| `sc015-implementation.md`：7 节点、10 选项、27 文本 | 与目录一致 |
| `sc015-implementation.md`：所有事件 `sc013`/`Monthly`/`PerGame`/`cooldown 0`/正权重 | 一致 |
| `sc015-implementation.md`：选项仅 `Always`、`effects` 空 | 一致 |
| `sc015-implementation.md`：门槛 0 / ≥1 / ≥2 | 一致 |
| `sc015-implementation.md`：优先级 6/60/600，降序语义 | 与代码和数据一致 |
| `sc015-implementation.md`：SC-011/13/14 未改、纯追加 | 与快照比较一致 |
| `PROJECT_STATE.md`、`ARCHITECTURE.md`、`DECISIONS.md` 的 SC-015 小节 | 与目录一致 |
| `SC015_DEMO.md` 的四条分支步骤与预期节点 | 与独立模拟顺序一致 |
| 文档是否把未执行项写成通过 | 否；均明确标注 Unity/EditMode/Console/Play Mode/存读档/用户 Play Test 未执行 |

## 12. 非阻断观察

以下不构成 RETURN 理由，供后续整理时参考：

1. **快照位置未写入实现报告。** 报告只说“实现前工作区文件快照”，没有记录路径或哈希。本次审阅在 `%LOCALAPPDATA%\Temp\opencode\sc015-pre` 找到并复核；临时目录可能被清理，建议后续在报告中附上快照路径与哈希，或把可比对基线放入持久位置。
2. **“不修改存档状态”的措辞偏宽。** 提交 SC-015 事件会写入既有 `eventRuntime.history`、`onceEventIds` 与 `committedResultIds`，只是不新增字段、不改关系/任务/家族数值。`PROJECT_STATE.md` 与 `DECISIONS.md` 的准确表述应是“不新增存档字段，仅写既有事件历史”。
3. **“晴信在世时的样子”可能暗示晴信已故。** `event.sc013.family-letters.second-sealed` 使用该措辞，而 M2 的交接是退休而非死亡。若需严格贴合固定剧本，可改为“晴信当家时”。这是风味文本问题，不影响分支语义。
4. **第三代节点的出现依赖终局面板可关闭。** 静态代码显示 `M2EndingPanel` 可关闭且不阻断时间推进，但实际点击交互仍属于 Play Test 范围，未在本次审阅中验证。

## 13. 明确未验证的项目

本次审阅**没有**取得以下任何证据，也不对其作通过声明：

- Unity Editor 编译、脚本导入或程序集错误。
- Unity Test Runner / EditMode 测试（本任务未新增或运行测试）。
- Console error/warning 计数。
- `FoundationSceneValidator` 或场景/预制体验证。
- Play Mode 中七个节点的实际出现、UI 文本渲染、终局面板关闭与月份推进。
- 存读档后一次性标记、事件历史与编年史的行为。
- 用户 Play Test、可理解性反馈与最终验收。
- 提交、推送、合并、打标签。

## 14. 审阅结论

**ACCEPT。**

七个事件节点、十个选项、二十七条文本、六个来源选项、三档继承门槛、一次性范围、空效果声明与降序优先级均通过静态核对；以实现前快照为基线，SC-011、SC-013、SC-014 的既有数据未改变。未发现需要 RETURN 的功能性或内容性缺陷。第 12 节四项为非阻断观察，可由协调方决定是否在后续文档整理中处理。

本结论只覆盖静态数据与代码语义，不替代 Unity 编译、EditMode、Console、场景验证、Play Mode、存读档或用户 Play Test。
