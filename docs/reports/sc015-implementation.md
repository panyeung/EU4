# SC-015 三代家书实现报告

日期：2026-11-03  
分支：`feature/sc013-fixed-three-generation`  
工作区：`C:\Users\panye\My project`  
设计稿：`docs/superpowers/specs/2026-11-03-sc-015-three-generation-family-letters-design.md`  
实现计划：`docs/superpowers/plans/2026-11-03-sc-015-three-generation-family-letters.md`  
试玩手册：`SC015_DEMO.md`

## 结果

SC-015 已按批准的设计与计划完成数据/文档切片：在 SC-013 固定 M2 剧本中追加 `chain.sc013.family-letters`，包含七个事件节点、十个选项和二十七条中文文本。所有节点使用 `scenario: "sc013"`、`Monthly` 触发器、`PerGame` 一次性范围和现有允许列表条件；所有新增效果数组为空，`contentVersion` 保持 `2`。提交本链选项时，选择、事件历史、一次性标记与已提交结果仅写入既有 `eventRuntime` 存档字段；不新增存档 schema 字段，也不改变关系、任务或家族数值。

本实现没有修改场景、预制体、`ProjectSettings`、存档 schema、package、测试、代码、`Library` 或 `Temp`；没有变更 SC-011、SC-013、SC-014 的既有事件、选项或文本。两轮独立 OpenCode2 只读审阅均为 `ACCEPT`，见 `docs/reports/sc015-review.md` 与 `docs/reports/sc015-review-2.md`；审阅仅覆盖静态内容与代码语义，不构成 Unity 或用户 Play Test 验收。

## 变更文件

- `Assets/Resources/Events/event-catalog-v1.json` — 追加 `chain.sc013.family-letters` 和七个 `event.sc013.family-letters.*` 事件。
- `Assets/Resources/Localization/event-text-zh-v1.json` — 追加七个事件、十个选项、十个摘要，共二十七条中文文本。
- `PROJECT_STATE.md` — 追加 SC-015 状态行与实现小节。
- `ARCHITECTURE.md` — 追加 SC-015 事件链与优先级说明。
- `DECISIONS.md` — 追加 SC-015 以事件历史承载后果、无数值变化且不新增存档 schema 字段的决策。
- `SC015_DEMO.md` — 新建纯文本交付说明、四条分支试玩步骤、失败判据和结果清单。
- `docs/reports/sc015-implementation.md` — 本报告。

## 分支结构

| 世代 | 事件 | 来源条件 | 选项/结局 |
|---|---|---|---|
| 第一代 | `event.sc013.family-letters.first` | `SuccessionCountEquals 0` | 完整公开；暂封家书 |
| 第二代 | `event.sc013.family-letters.second-open` | 第一代公开 + `SettledSuccessionCountAtLeast 1` | 保留原貌；整理简史 |
| 第二代 | `event.sc013.family-letters.second-sealed` | 第一代封存 + `SettledSuccessionCountAtLeast 1` | 正綱拆阅；继续封存 |
| 第三代 | `event.sc013.family-letters.third-full-public` | 公开后保留 + `SettledSuccessionCountAtLeast 2` | 单一接收选项 |
| 第三代 | `event.sc013.family-letters.third-short-history` | 公开后整理 + `SettledSuccessionCountAtLeast 2` | 单一接收选项 |
| 第三代 | `event.sc013.family-letters.third-opened-by-heir` | 封存后拆阅 + `SettledSuccessionCountAtLeast 2` | 单一接收选项 |
| 第三代 | `event.sc013.family-letters.third-sealed` | 封存后继续封存 + `SettledSuccessionCountAtLeast 2` | 单一接收选项 |

每个第二代选项只被一个第三代节点引用；分支互斥由 `EventOptionChosen` 的唯一来源和世代门槛共同表达。

## 优先级语义

`EventEngine.Schedule` 的候选排序为 priority 降序（`EventEngine.cs` 中 `right.priority.CompareTo(left.priority)`），因此**数值越大越早**。本链使用：

- 第一代 `6`：高于 SC-014 `event.sc013.refugee-arrival` 的 `5`，低于 SC-011 `event.rainy-arrival` 的 `10`，低于 SC-013 `event.sc013.inheritance-pledge` 的 `1000`。
- 第二代 `60`：高于 SC-014 `event.sc013.refugee-loyalty` 的 `50`，低于 SC-011 回访的 `100`。
- 第三代 `600`：高于 SC-014 援助/竞争节点的 `500`，低于 SC-013 `event.sc013.finale` 的 `1000`。

这满足“同世代先于 SC-014 可选事件、低于 SC-013 必须事件”的计划要求，同时不抢占既有 SC-011 顺序。M2 退休门禁未改变，完整链仍需要在每次交接前额外结束月份。

计划讨论中曾考虑 `4/40/400`；由于调度按降序排序，该组数值会排在 SC-014 之后，最终确认使用 `6/60/600`，以符合设计意图。

## 静态检查证据

实现侧在 Windows 主机上使用 Python 3.14.8 执行一次性 JSON 检查（未运行 Unity 或测试）：

1. 两个 JSON 文件均可解析。
2. 两个 `contentVersion` 均为 `2`，且彼此相等。
3. 纯追加比较：以实现前工作区文件快照为基线（路径与 SHA-256 见下文），3 条既有链、9 个既有事件、50 条既有文本逐项 JSON 相等；新增 1 条链、7 个事件、27 条文本。
4. 链 `chain.sc013.family-letters` 的 7 个成员存在、顺序正确、无重复、无未链接事件。
5. 所有事件/文本/选项/摘要 ID 唯一；27 个新增 `textId` 全部存在；无孤立 SC-015 文本行。
6. 所有 `EventOptionChosen` 的源事件存在、源选项存在、同一链；每个源事件/选项组合只被一个事件引用。
7. 第一代门槛为 `SuccessionCountEquals 0`；第二代为对应第一代选项加 `SettledSuccessionCountAtLeast 1`；第三代四个节点分别为对应第二代唯一选项加 `SettledSuccessionCountAtLeast 2`。
8. 所有新增事件 `scenario` 为 `sc013`、`trigger` 为 `Monthly`、`onceScope` 为 `PerGame`、`cooldownMonths` 为 `0`、`weight` 为正。
9. 所有新增选项 `requirements` 仅有 `Always`，`effects` 为空数组。
10. 优先级关系满足上文约束。

结果：10 组检查、0 错误。此为源文件静态比较，不是 Unity Test Runner、编译或运行时证据。

### 实现前快照

实现前工作区快照保存在项目之外：

```text
%LOCALAPPDATA%\Temp\opencode\sc015-pre\event-catalog-v1.json
%LOCALAPPDATA%\Temp\opencode\sc015-pre\event-text-zh-v1.json
```

快照 SHA-256（实现前基线）：

```text
catalog snapshot = ae532adc353e887516678bdf24d66fd1f386c4632ecf67a6d5469cc5d120609a
text snapshot    = c85ea74ec45e4f999bb23f76500df0f47d3ff9083b23e53af1dc1d65582c038a
```

`docs/reports/sc015-review.md` 已独立复算并记录上述两个快照值；快照位于临时目录，不纳入版本控制。

## 既有内容保护

- 与实现前快照比较，既有 9 个事件（包含 SC-011 的 3 个、SC-013 的 2 个、SC-014 的 4 个）JSON 完全相等。
- 既有 3 条链（`chain.rainy-arrival`、`chain.sc013.main`、`chain.sc013.refugee-legacy`）JSON 完全相等。
- 既有 50 条文本条目 JSON 完全相等；SC-015 只追加 27 条。
- 因 SC-013/SC-014 尚未提交，本报告不使用 `HEAD` 作为比较基线，也不声称与 `HEAD` 逐字节相等。
- 工作区中既有的其他修改（`SampleScene.unity`、旧方向文档、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 等）未被触碰。

## 未执行的验证

以下项目全部**未运行/未验证**，不得据此声称通过：

- Unity Editor 编译。
- Unity Test Runner / EditMode 测试（本任务未新增、未运行测试）。
- Console error/warning 计数。
- `FoundationSceneValidator` 与场景验证（本任务未改场景）。
- Play Mode。
- 存读档试玩（SC-015 未新增存档字段，但仍未执行）。
- 用户 Play Test 与验收。
- 提交、推送、合并、打标签。
- 独立只读审阅已完成，两轮均为 `ACCEPT`；它不替代 Unity 与用户 Play Test 验证。

## 后续步骤

1. 协调方派发独立 OpenCode2 只读审阅，复核七个节点、文本引用、分支可达性、源选项唯一性、一次性结算、世代门槛、空效果和既有数据未改；结果写入 `docs/reports/sc015-review.md`。
2. 若审阅要求修正，保持同一任务范围修正后复审。
3. 用户按 `SC015_DEMO.md` 在现有 Unity Editor 执行四条分支的 Play Test，并填写纯文本结果清单。
4. 仅在用户明确验收后，由协调方办理 checkpoint、合并、标签与推送。
