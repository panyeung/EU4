# SC-015 修正后第二轮独立只读复审

日期：2026-11-03  
分支：`feature/sc013-fixed-three-generation`  
`HEAD`：`88fe7c6`（与首轮审阅相同，未新增提交）  
工作区：`C:\Users\panye\My project`  
审阅对象：首轮 `docs/reports/sc015-review.md` 第 12 节的四项非阻断观察，以及修正后的五个文件。  
结论：**ACCEPT**

## 0. 结论摘要

首轮审阅提出的四项非阻断观察均已逐项关闭：实现报告已记录快照路径与 SHA-256，且哈希与临时目录中的实际快照一致；SC-015 的存档措辞已改为“不新增存档 schema 字段，仅写既有事件运行时字段”；封存路径的风味文本已由“晴信在世时”改为“晴信当家时”；试玩手册 A–D 四条分支均已加入 M2 结局面板「继续游玩」关闭步骤，且按钮名与场景生成器中的实际控件一致。

修正没有改变 SC-015 的七节点结构、十个选项、二十七条文本、链成员、条件门槛、来源选项、空效果或优先级关系；既有 3 条链、9 个事件、50 条文本相对实现前快照仍逐项 JSON 相等。首轮审阅 `sc015-review.md` 未被修改；修正窗口内只写入了预期的五个文件，没有触碰代码、测试、场景、预制体、`ProjectSettings`、`.meta` 或用户既有 WIP。

本审阅仍是**静态只读检查**。没有运行 Unity、编译、EditMode、Console 检查、场景验证、Play Mode、存读档试玩或用户 Play Test；这些项目在文末统一列为未验证。第 6 节列出三项非阻断的文档状态/证据留存事项，交由协调方在集成时处理，不构成 RETURN 理由。

## 1. 输入与基线

已读：

- `docs/reports/sc015-review.md`（首轮审阅，2026-11-02 17:42:41，SHA-256 `b23aaaf7816ffc266d7dc8a6741a503d09ca1e48fc3a12e8fddd8e99cc3bec75`）
- `docs/reports/sc015-implementation.md`（修正后，17:45:28）
- `SC015_DEMO.md`（修正后，17:46:08）
- `PROJECT_STATE.md`、`DECISIONS.md`、`ARCHITECTURE.md` 的 SC-015 小节
- `Assets/Resources/Events/event-catalog-v1.json`
- `Assets/Resources/Localization/event-text-zh-v1.json`
- 为核对存读档字段与关闭按钮语义所必需的现有代码：`Assets/Scripts/Events/EventRuntimeState.cs`、`Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`、`Assets/Scripts/Presentation/M2EndingPanel.cs`

实现前快照仍位于首轮审阅记录的位置：

```text
%LOCALAPPDATA%\Temp\opencode\sc015-pre\event-catalog-v1.json
%LOCALAPPDATA%\Temp\opencode\sc015-pre\event-text-zh-v1.json
```

本轮独立复算的 SHA-256：

```text
catalog current  = afea1fadb46b75cb5df28c33c54372faab8c01c12a11d029971bd2b4c5321e83
catalog snapshot = ae532adc353e887516678bdf24d66fd1f386c4632ecf67a6d5469cc5d120609a
text current     = fb3b93dc63d06f8d1f4cb27d491c724b09efce9e642976348f39419511c73851
text snapshot    = c85ea74ec45e4f999bb23f76500df0f47d3ff9083b23e53af1dc1d65582c038a
```

快照 mtime 为 17:31:57，早于本次修正窗口。事件目录当前哈希与首轮审阅记录一致；文本当前哈希因已批准的风味文本修正由 `f1b2dfd5…d29ff05` 变为 `fb3b93dc…11c73851`，差异范围见第 3.3 节。

## 2. 方法与证据边界

1. Python 3.14.8 解析两个 JSON，检查 ID 唯一性、引用完整性、链成员、条件、选项要求、效果、优先级与内容版本。
2. 以 `%LOCALAPPDATA%\Temp\opencode\sc015-pre` 为实现前基线，按 ID 映射逐项比较既有链、事件与文本。
3. `sha256sum` 复算当前文件、快照、首轮审阅与修正后报告；`git diff --no-index --numstat` 复核快照到当前的纯追加行数。
4. 以 17:38:14 生成的修正前 SC-015 文本摘录 `%LOCALAPPDATA%\Temp\opencode\sc015-texts.txt` 为对照，逐行 diff 当前 27 条 SC-015 文本，定位本轮文本修正的实际范围。
5. 17:43 之后的全工作区 mtime 扫描，确认修正窗口内的实际写入集合。
6. 只读核对 `EventRuntimeState` 的存档字段名、`AddSC013FamilyRosterPanel` 的按钮文案与绑定、`M2EndingPanel` 的关闭语义。

以上均为源文件与文件系统静态证据，不是 Unity 运行、测试或用户验收证据。

## 3. 四项非阻断观察的关闭证据

| # | 首轮观察 | 本轮状态 | 主要证据 |
|---:|---|---|---|
| 1 | 实现报告未记录快照路径与哈希 | **已关闭** | 报告第 69–85 行新增路径与两个快照哈希，复算一致 |
| 2 | “不修改存档状态”措辞偏宽 | **已关闭** | `PROJECT_STATE.md:73`、`DECISIONS.md:8`、`SC015_DEMO.md:32,145`、实现报告第 12 行均改为“不新增 schema 字段，仅写既有事件运行时字段” |
| 3 | “晴信在世时”可能暗示死亡 | **已关闭** | 文本第 307 行改为“封蜡仍是晴信当家时的样子”；与修正前摘录逐行 diff 仅此一行不同 |
| 4 | 第三代节点依赖结局面板可关闭，试玩步骤未显式说明 | **已关闭** | `SC015_DEMO.md:75,87,108,125` 四条分支均写明先点「继续游玩」关闭面板，再结束月份；按钮文案与生成器一致 |

### 3.1 快照路径与哈希可追溯

`docs/reports/sc015-implementation.md` 第 69–85 行现已包含：

- 快照目录与两个文件路径；
- 两个快照的 SHA-256；
- 快照不纳入版本控制的说明；
- 首轮审阅已独立复算这两个值的交叉引用。

本轮重新计算的结果与报告记录逐字一致：

| 文件 | 报告记录值 | 实际复算值 | 结果 |
|---|---|---|---|
| `event-catalog-v1.json` 快照 | `ae532adc…120609a` | `ae532adc…120609a` | 一致 |
| `event-text-zh-v1.json` 快照 | `c85ea74e…582c038a` | `c85ea74e…582c038a` | 一致 |

快照 mtime `17:31:57` 早于事件目录写入 `17:32:40` 和文本目录写入，且第 4 节的既有内容 JSON 相等比较成立，因此该观察已闭合。快照位于临时目录的易失风险仍存在，见第 6 节第 2 项。

### 3.2 存档状态措辞

修正后的表述已统一为“不新增存档 schema 字段，仅使用既有事件运行时字段”，并明确列出或概括了实际写入的 `history`、`onceEventIds`、`committedResultIds`：

- `PROJECT_STATE.md:73`：“不新增存档 schema 字段……选择、事件历史、一次性标记与已提交结果仅写入既有事件运行时存档字段。”
- `DECISIONS.md:8`：列出 `history`、`onceEventIds`、`committedResultIds`，并写明“SC-015 adds no new save-schema field and changes no relationship, mission, or family numeric value”。
- `SC015_DEMO.md:32` 与 `:145`：同样区分“不新增字段”和“写入既有 `eventRuntime` 字段”。
- `docs/reports/sc015-implementation.md:12`：“不新增存档 schema 字段”，并说明选择、事件历史、一次性标记与已提交结果的写入范围。
- `ARCHITECTURE.md:40` 只声明“不改变……存档 schema”，没有声称不写存档，语义准确。

字段名与代码一致：`EventRuntimeState.cs:51-53` 将 `onceEventIds`、`committedResultIds` 与既有历史一并写入 `EventRuntimeSaveData`，读档在 `:90-93` 恢复。全仓检索“不修改存档状态”“不改变存档状态”“不写存档”等宽泛短语，除首轮审阅引用旧措辞外，没有残留。该观察已闭合。

### 3.3 “晴信当家时”风味文本

`Assets/Resources/Localization/event-text-zh-v1.json:307` 当前为：

```text
封蜡仍是晴信当家时的样子，匣中记着什么，正綱并不知道。
```

以修正前文本摘录（17:38:14）为对照，当前 27 条 SC-015 文本的归一化逐行 diff 只有一处：

```diff
- B: ……封蜡仍是晴信在世时的样子，匣中记着什么，正綱并不知道。……
+ B: ……封蜡仍是晴信当家时的样子，匣中记着什么，正綱并不知道。……
```

当前文本中“在世时”出现 0 次，“当家时”仅此 1 次；同一事件的其他文本、十个选项与十个摘要均未改变。该观察已闭合。此改动只影响风味表述，不涉及条件、效果、ID 或引用。

### 3.4 M2「继续游玩」关闭步骤

`SC015_DEMO.md` 的四条分支均明确要求先关闭 M2 结局面板，再推进月份：

| 分支 | 位置 | 修正后步骤 |
|---|---|---|
| A 公开 → 保留 → 完整记名 | `:75` | “先点击该面板上的「继续游玩」按钮关闭面板；再点击「结束月份」。” |
| B 公开 → 简史 → 简要传述 | `:87` | “完成第三代「第三代的家门」终局，然后在 M2 结局面板上点击「继续游玩」关闭面板。” |
| C 封存 → 拆阅 → 第二代打开 | `:108` | 同上，并随后 `:109` 要求再点「结束月份」 |
| D 封存 → 续封 → 木匣仍封着 | `:125` | 同上，并随后 `:126` 要求再点「结束月份」 |

静态交叉核对：

- `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs:327` 创建终局面板按钮，文案正是「继续游玩」，并在 `:335-337` 绑定到 `M2EndingPanel.BindReferences(..., endingClose)`。
- `M2EndingPanel.Close()` 将 `panelRoot` 设为不激活；类注释与 `Refresh()` 注释说明关闭不改变游戏状态、不阻断时间推进。
- 因此“先关闭面板、再结束月份”的步骤与当前代码语义一致。

实际点击、面板可见性和月份推进仍属于 Play Test 范围，本审阅未执行，见第 7 节。该观察在静态范围内已闭合。

## 4. SC-015 结构与静态引用保持

当前目录仍为 16 个事件、4 条链；SC-015 为 7 个事件、10 个选项、27 条文本。以下检查全部通过：

| 检查 | 结果 |
|---|---|
| 事件/选项/文本 ID 全局唯一 | 通过，无重复 |
| 7 个事件 `textId` 解析 | 7/7 |
| 10 个选项 `textId` 解析 | 10/10 |
| 10 个选项 `summaryTextId` 解析 | 10/10 |
| 所有新增标题/正文非空 | 是 |
| 链 `chain.sc013.family-letters.linkedEventIds` | 恰好 7 个，顺序 first → second-open → second-sealed → four third nodes，无重复/遗漏 |
| `scenario` / `trigger` / `onceScope` / `cooldownMonths` / `weight` | 均为 `sc013` / `Monthly` / `PerGame` / `0` / 正数 |
| 选项要求 | 全部仅 `Always` |
| 选项效果 | 全部 `[]` |
| 世代门槛 | `0` / `≥1` / `≥2` 与来源选项对应正确 |
| 每个 `(sourceEventId, sourceOptionId)` | 唯一被一个后续事件引用 |
| `contentVersion` | 两文件均为 `2`，彼此相等 |
| 优先级 | 第一代 `6`、第二代 `60`、第三代 `600`，未变 |

既有内容保护：

| 对象 | 相对实现前快照 |
|---|---|
| 3 条既有链 | JSON 完全相等 |
| 9 个既有事件（SC-011 3、SC-013 2、SC-014 4） | JSON 完全相等 |
| 50 条既有文本 | JSON 完全相等 |
| 新增规模 | +1 链、+7 事件、+27 文本 |
| 快照 → 当前原始 diff | 事件目录 `224/0`、文本 `135/0`（纯插入，0 删除行） |
| 文本修正 | 仅 SC-015 区块内的一行一处短语，既有 50 条文本未受影响 |

## 5. 首轮审阅与无关 WIP 保护

### 5.1 首轮审阅未被修改

- `docs/reports/sc015-review.md` 的 mtime 为 `17:42:41`，早于修正窗口；
- 17:43 之后的全工作区写入扫描不包含该文件；
- 当前 SHA-256 为 `b23aaaf7…cc3bec75`，内容仍是首轮审阅原文，第 12 节四项观察与 ACCEPT 结论均保留；
- 本轮复审单独写入 `docs/reports/sc015-review-2.md`，没有覆盖或回写首轮报告。

### 5.2 修正窗口内的实际写入

17:43 之后（排除 `.git`、`Library`、`Temp`、`graphify-out`）只发现以下五个文件：

```text
Assets/Resources/Localization/event-text-zh-v1.json   17:45:11
DECISIONS.md                                          17:45:22
PROJECT_STATE.md                                      17:45:22
docs/reports/sc015-implementation.md                  17:45:28
SC015_DEMO.md                                         17:46:08
```

没有 `Assets/Scripts`、`Assets/Tests`、`Assets/Scenes`、`Assets/Prefabs`、`ProjectSettings` 或 `.meta` 文件在该窗口内被写入。事件目录 `event-catalog-v1.json` 的 mtime 仍为 `17:32:40`，未被本次修正触碰，当前哈希与首轮审阅一致。

### 5.3 无关 WIP

`SampleScene.unity`、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、旧方向文档、`graphify-out/`、`nul` 等既有工作区改动均不在 17:43 之后的写入集合内。`git diff` 的 hunk 范围显示 `PROJECT_STATE.md` 与 `DECISIONS.md` 的改动仍位于既有 SC-013/SC-014/SC-015 小节；未发现本次修正向无关章节扩散的证据。

限制：三个 Markdown 文件（`PROJECT_STATE.md`、`DECISIONS.md`、`SC015_DEMO.md`）在修正前没有逐字节快照，因此无法像两个 JSON 那样证明“除四项修正外逐字节不变”。本轮以 mtime 集合、改动 hunk 范围、四项措辞的精确落点以及“仅五个文件被写入”作为证据；未发现越界修改，但该限制应如实保留。

## 6. 剩余问题（均为非阻断）

1. **审阅状态语句仍停留在修正前。** `PROJECT_STATE.md` 的 SC-015 表格行仍写“独立只读审阅……待办”，SC-015 小节末句仍写“独立只读审阅尚未开始”；`DECISIONS.md:8` 末句仍为“Independent read-only review is pending”；`SC015_DEMO.md:16` 仍称“独立只读审阅尚未开始”“当前的实现状态不是 ACCEPT”；`sc015-implementation.md:107,111` 仍把独立审阅列为待派发。首轮审阅已经在 `sc015-review.md` 给出 ACCEPT，这些句子已滞后。它们不影响 SC-015 数据、结构或本轮四项修正，建议协调方在采纳本复审后统一更新，不需要再开一轮修正。
2. **快照仍位于临时目录。** 报告现已记录路径与哈希，追溯性已建立；但 `%LOCALAPPDATA%\Temp` 可能被清理。若后续仍需以该快照复核，建议协调方将其复制到持久位置；若不再需要，当前哈希与首轮/本轮记录已足以固定基线。
3. **首轮审阅记录的“当前文本哈希”已过期。** 首轮的 `f1b2dfd5…d29ff05` 对应修正前文本；修正后当前值为 `fb3b93dc…11c73851`。后续比较应使用新值，或明确区分“修正前”和“修正后”。这不是数据缺陷。

## 7. 明确未验证的项目

本轮**没有**取得以下任何证据，也不对其作通过声明：

- Unity Editor 编译、脚本导入或程序集错误；
- Unity Test Runner / EditMode 测试（本任务未新增或运行测试）；
- Console error/warning 计数；
- `FoundationSceneValidator` 或场景/预制体验证；
- Play Mode 中七个节点的出现、UI 渲染、M2 结局面板「继续游玩」点击及月份推进；
- 存读档后一次性标记、事件历史与编年史行为；
- 用户 Play Test、可理解性反馈与最终验收；
- 提交、推送、合并、打标签。

本复审只创建了 `docs/reports/sc015-review-2.md`，没有修改任何功能文件、既有报告、场景、测试或项目设置。

## 8. 复审结论

**ACCEPT。**

首轮四项非阻断观察均有明确、可复核的修正证据并已关闭；SC-015 的七节点结构、十个选项、二十七条文本、链成员、静态引用、条件门槛、空效果与优先级关系保持不变；既有 3 条链、9 个事件、50 条文本相对实现前快照仍逐项 JSON 相等；首轮审阅未改，修正窗口只写入预期五个文件，无关 WIP 未被触碰。

第 6 节三项为文档状态与证据留存层面的非阻断事项，交由协调方在集成时处理。本结论仅覆盖静态数据、文本与代码语义，不替代 Unity 编译、EditMode、Console、场景验证、Play Mode、存读档或用户 Play Test；在这些证据取得前，不得把 SC-015 标记为用户已验收或创建最终 checkpoint。
