# M08 家族回顾（五十五周年与百年名门）Play Mode 端到端验证报告（2026-11-19 起）

## 2026-11-23 15:30 / 15:33 本轮定稿：同版本两轮复跑

本节是本轮最新结果。它用同一份测试源、同一 Editor、同一固定种子，连续跑两轮完整路线，并把 33 条玩家决策逐字节比对。下方各节保留为历史证据，不替代本节。

### 运行身份与前置检查

- 工作树 `C:\Users\panye\orca\workspaces\My project\m08-100-year-review`，分支 `panyeung/m08-100-year-review`，基线 HEAD `74f7579`；本轮只改 `Assets/Tests/PlayMode/M08CampaignReviewPlayModeTests.cs` 与文档。
- Unity 6000.6.6f1，Editor PID 68216，Pipeline 端口 7801。运行前 `editor_status` 为 `ready`、`compiling=false`、`playMode=stopped`，`projectPath` 指向本树。
- 同机另有 `m08-55-success-evidence`（PID 54508 / 7800）、`m09-debug-drag-fix`（PID 132232 / 7803）与主工程（PID 95724 / 7804）的 Editor，均未操作。运行前后没有 `-runTests` 进程；`hermes kanban ls` 无匹配任务。测试串行执行。
- 共享结果文件在开工前被 m09 的 2301/2301 全量运行覆盖。本轮每次 `run_tests` 完成后立即用 `scripts/copy-testresults.sh` 复制；下文哈希均指复制副本，不引用共享源文件的旧内容。
- 编译 `recompile` 完成，`console_status` 报 `compilationFailed=false`；`Library/ScriptAssemblies/SengokuChronicle.PlayModeTests.dll` 时间晚于测试源文件。

### 命令

```
unity command recompile --project-path "$PWD"
unity command run_tests --project-path "$PWD" --mode PlayMode \
  --filter "SengokuChronicle.PlayModeTests.M08CampaignReviewPlayModeTests.ReviewMilestones_OpenAtFiftyFiveAndOneHundredYears_ConfirmAndSurviveSaveLoad" \
  --async_tests true --timeout 1800
unity command test_status --project-path "$PWD"
bash scripts/copy-testresults.sh Logs/verify/<本轮目录>
```

### 两轮结果

| 轮次 | 证据目录 | 结果 | 耗时 | 起止（UTC） | TestResults.xml |
|---|---|---|---|---|---|
| 第一轮 | `Logs/verify/m08-100-20261123-1530/` | 1/1 通过，0 失败，0 跳过 | 37.7019182 s | 01:29:03–01:29:41 | 35507 B，SHA-256 `17366bde0b28fda5bc3eb658e5dc6fa1d6e8407c11f00c0ee577ec6a66f85d68` |
| 第二轮 | `Logs/verify/m08-100-20261123-1533/` | 1/1 通过，0 失败，0 跳过 | 36.6106735 s | 01:30:00–01:30:37 | 35508 B，SHA-256 `b7c0142d6aed990c43fa481623f0394fc54abb9f5672118e8802b663b2e96fb9` |

两轮 XML 都记录 `_PID=68216`，Assembly 路径指向本树 `Library/ScriptAssemblies/SengokuChronicle.PlayModeTests.dll`。第二轮复制时与共享 `TestResults.xml` 同哈希。

### Console 与场景

| 项 | 第一轮 | 第二轮 |
|---|---|---|
| 清空前累计 | `error:0, warn:0, log:104`，cursor 259 | `error:0, warn:0, log:96`，cursor 355 |
| 清空后基线 | `error:0, warn:0, log:0` | `error:0, warn:0, log:0` |
| 本轮时间窗新增 | `error:0, warn:0, log:96`，cursor 259→355；`groundTruth.consoleLogs` 94 | `error:0, warn:0, log:98`，cursor 355→453；`groundTruth.consoleLogs` 95 |
| 时间窗内 level 计数 | error 0 / warn 0 | error 0 / warn 0 |
| `[M08-55]` 日志行 | 91 | 91 |

- 时间窗按 `console --since <baseline_cursor>` 取，累计数按 `console_status.counts` 单列；两者原文分别在 `console_entries.after.txt` 与 `console_status.after.txt`。
- `Editor.log.this-round.txt` 从运行前字节偏移切片，包含 91 条 `[M08-55]` 日志；没有 `NullReferenceException`、`AssertionException`、`InvalidOperationException`、`Argument*Exception`、`Unhandled Exception` 或测试失败行。
- 两轮结束后 `FoundationScene` 均为 `dirty=False|rootCount=4`，没有保存场景。
- `.vscode/settings.json` 运行前后 SHA-256 都是 `9432d680c0d25334199aac3191668726749cf199e20c7f57f20828bae256cc7b`，未暂存、未还原。
- Test Runner 每轮删除 `PerformanceTestRunInfo.json`、`PerformanceTestRunSettings.json` 及两个 `.meta`；开工前四项 `git diff --quiet` 为干净，跑完只对这四项执行 `git checkout --` 恢复。`ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json` 是 Editor 打开期间产生的本地改动，本轮未恢复、未暂存。

### 最小经营决策与路线

路线只做四类玩家动作，全部触发生产面板的 `Button.onClick` 回调：

- 月结。切到「地图」页，触发 `EndMonthButton`。每次先断言 `LastMonthTurnOutcome == Succeeded`，再断言日期恰好前进一个自然月，两项都成立才累计成功月结数。
- 事件。触发 `EventChoice_*` 的首个可用选项；若该事件未解决，依次尝试下一个选项，直到 `PendingEventId` 改变。
- 继承。切到「家族」页；首个合法 `SuccessionCandidate_*` 加 `SuccessionConfirmButton`；幼主时首个 `SuccessionRegent_*` 加确认；危机时 `SuccessionAcceptCrisisButton`；成年交权时确认。
- 农庄补任。切到「家计」页；农庄空缺时触发首个 `IndustryCandidate_industry.farm_*` 加 `IndustryConfirmButton`，失败再试下一个。

除此之外没有市场买卖、商馆任命、研究、教育、婚姻、任务或产业扩产操作。商馆 `industry.trading-house` 全程空缺。

### 测试夹具边界

- 唯一非按钮设置是保存槽重定向：用 `SaveSlotStore` 指向 `Path.GetTempPath()/m08-55-review-playmode-<guid>`，对标题界面与存读档面板调用公开的 `SetStore` 测试缝（源码注释为 Editor generator / test seam）。不写用户存档。
- 场景由 `SceneManager.LoadSceneAsync("Assets/Scenes/FoundationScene.unity", Single)` 加载；组件与控件按类型或名称查找。
- 不调用 `GameSession` 的写状态命令、调试跳日、`SetCampaignProgressForTest`、`Refresh`，也不直接改日期、家族、任命、确认旗标或随机状态。会话状态只读用于断言与日志。
- 月结、事件、继承、农庄任命、回顾确认、存档与读档全部触发生产 `Button.onClick` 回调。两轮产生的 33 条决策台账逐字节相同，SHA-256 都是 `4835e2e6717515058250f6f31b5619f0dfbaefdcd7702d5f38c46d17183c1648`：

```
diff -u Logs/verify/m08-100-20261123-1530/decision_ledger.txt \
        Logs/verify/m08-100-20261123-1533/decision_ledger.txt
# 无输出，退出码 0
```

台账内容为 20 次事件、5 次继承、5 次农庄任命、2 次回顾页打开与 1 条终局状态。关键节点：

| 日期 | 决策 | 结果 |
|---|---|---|
| 1560-04-01 | 农庄任命 | `person.sc013.chie` |
| 1560-05-01 至 1561-04-01 | 12 次事件 | 首个可用选项，事件 ID 与选项 ID 见台账 |
| 1570-01-01 | 农庄补任 | `person.marriage.person-sc013-sakon.1562` |
| 1578-01-01 | 第 1 次继承 | 家主 `person.sc013.masatsuna` |
| 1588-01-01 | 第 2 次继承 | 家主 `person.sc013.nobumasa` |
| 1599-01-01 | 农庄补任 | 长子系出生人物 |
| 1601-01-01 | 第 3 次继承 | 长子系出生人物 |
| 1615-04-01 | 五十五周年回顾 | 自动打开，确认后继续 |
| 1657-01-01 | 第 4 次继承 | 幼主加摄政，同日补任农庄 |
| 1660-01-01 | 第 5 次继承 | 家主交接，同日补任农庄 |
| 1660-04-01 | 百年名门评定 | 自动打开，确认后继续 |

### 五十五周年与百年的状态

| 项 | 1615-04-01（660 次成功月结） | 1660-04-01（1200 次成功月结） |
|---|---|---|
| 已历年份 | 55 | 100 |
| 待处理回顾 | `StageReview` | `CenturyReview` |
| 家族 | 未灭绝 | 未灭绝 |
| 人口 | 13 | 29 |
| 资金 / 物资 / 情报 | 673 / 1992 / 0 | 317 / 3612 / 0 |
| 粮食 / 粮价 | 1263 / 100‰ | 0 / 13910‰ |
| 连续缺粮 | 0 个月 | 53 个月 |
| 事件 / 继承 / 农庄任命 | 20 / 3 / 3 | 20 / 5 / 5 |

- 55 年时资源健康，家族 13 人；这是继续跑到百年的依据。
- 百年时家族仍存续、两处回顾都正确打开并确认，但路线已经进入饥荒：粮食 0、粮价 13910‰、连续短缺 53 个月，人口从 1655-04-01 的 73 人降到 29 人。结论只能写“这条最小路线能到百年”，不能写“这条路线经营健康”。
- 存档只在 `Path.GetTempPath()/m08-55-review-playmode-<guid>/save-slot-1.json` 的隔离槽；两轮各做一次“确认后存档 → 月结一个月 → 读档回滚”，共回滚 2 次月结，不覆盖用户存档。存档内 `randomState.initialSeedHex` 回读为 `5343414E4B4F3031`。

### 可重放性

- 两轮使用同一测试源。工作区 CRLF 字节的 SHA-256 为 `546134d22c30ad07f3d5c58119d85eb9505bcc0a9bb6c5c8f86c5c1e635eec37`；提交内按 LF 存储，`git show HEAD:Assets/Tests/PlayMode/M08CampaignReviewPlayModeTests.cs` 的 SHA-256 为 `cf9f5557d8bc6cb4b202e05b88a67f42de65a00f4b402f23da9ac4dc4d4a2900`，`diff --strip-trailing-cr` 为 0。两轮是同一 Editor 进程与会话、同一固定种子。
- 33 条决策台账、终局日期、人口、资源、产业、家主与待处理状态完全一致。
- 这是同一 Editor 会话内的重放证据，不是跨 Editor 重启、跨机器或跨 Player 的证据。

### 边界

- 「玩家操作」只指自动触发生产按钮的 `Button.onClick.Invoke()` 回调并让会话状态自然联动。没有模拟物理鼠标、触摸、EventSystem 射线命中、导航路径或键盘焦点。
- 没有构建或运行 Windows Player；没有人工逐帧检查两次回顾的滚动、排版与文字截断。
- 自动化 PlayMode 通过不等于用户实机验收。用户最终验收仍是关闭条件。
- 本轮没有改玩法、经济公式、产业规则、平衡参数、场景、预制体或 ProjectSettings 的产品内容；测试只增加日志，不改断言与路线。

---

以下三条为首轮（2026-11-21）元数据，保留作历史记录；本轮 2026-11-23 的元数据见上一节。

- 工作树 `C:\Users\panye\orca\workspaces\My project\m08-55-year-review-runtime`，分支 `feature/m08-55-year-review-runtime`。
- 引擎 Unity 6000.6.6f1，本树 Editor 的 Pipeline 端口 7803（主工程 7801，未触碰）。2026-11-21 本轮新起本树唯一 Editor，PID 79524，开工时 `console_status` 计数 `error:0, warn:0, log:6`。
- 证据副本：`Logs/verify/m08-55-20261121-2029/TestResults.xml`（6745 字节，SHA-256 `36afa5f57e372d5ab760ff0c0c8d454d37ec79882e23f4d6003ebf75f0d402fe`，`Logs/` 按仓库约定不入库）。

## 2026-11-23 本轮新增：百年名门 100 年生产路径验证

本节是本轮最新结果，把同一 SC-013 普通生产流程从 1560-04-01 延伸到 1660-04-01；下方 2026-11-23 与 2026-11-21 各节保留为历史证据，不替代本轮实测。

- 工作树 `C:\Users\panye\orca\workspaces\My project\m08-100-year-review`，分支 `panyeung/m08-100-year-review`。开工时本任务分支先快进到 PR #4 提交 `9aba12ad994da8371bcfbd737c5ef85075a5b21b`，再在其上改测试。
- Unity 6000.6.6f1，本树唯一 Editor PID 68216，Pipeline 端口 7801。另一个工作树 `m08-55-success-evidence` 的 Editor PID 54508 与端口 7800 未操作；测试串行执行，没有并行测试。
- 编译：`unity command recompile` 返回 `up_to_date：No scripts needed recompilation.`，`recompile_status` 为 `up_to_date`，`console_status` 报 `compilationFailed=false`。`Library/ScriptAssemblies/SengokuChronicle.PlayModeTests.dll` 时间 2026-11-23 10:49，晚于测试源文件的 10:47。
- 定向用例（本轮重跑）：`M08CampaignReviewPlayModeTests.ReviewMilestones_OpenAtFiftyFiveAndOneHundredYears_ConfirmAndSurviveSaveLoad`，1/1 通过，0 失败，0 跳过，0 不确定。用例耗时 35.2015 秒，运行总耗时 35.236048 秒（XML `duration=35.236048`，start `2026-11-23 21:02:41Z`，end `21:03:16Z`），XML 内 `_PID=68216` 与本树 Editor 一致。测试逻辑未改动，仍为本轮已提交的测试源文件内容。
- 证据目录 `Logs/verify/m08-100-20261123-1101/`（`Logs/` 按仓库约定不入库）内含本轮全部原始附件：`TestResults.xml`（9035 字节，SHA-256 `2ececfd13d753ab11939499efbd81d0f693d6abf5e337cac325f380a30b2213a`）、`Editor.log`（本轮结束时的全量副本，10158519 字节）、`Editor.log.this-round.txt`（自本次运行前偏移 10049835 起的本轮切片，108684 字节）、`console_status.before.txt`、`console_status.baseline.txt`、`console_status.after.txt`、`console_entries.after.txt`、`foundationscene_dirty.txt`、`run_start.txt`、`run_end.txt`、`run_meta.txt`、`SHA256SUMS.txt`。
- 运行身份：`editor_status` 在运行前与运行后均报 `projectPath=C:\Users\panye\orca\workspaces\My project\m08-100-year-review`、`status=ready`、`playMode=stopped`；Editor PID 68216，Pipeline 端口 7801，Unity 6000.6.6f1（原文见 `editor_status.before.txt`、`editor_status.after.txt`、`run_meta.txt`）。运行窗口 11:02:35 至 11:03:16（本地 HST），与 XML 起止时间一致。另一个工作树 `m08-55-success-evidence`（PID 54508、端口 7800）全程未操作，测试串行执行，运行前后均无其他 `-runTests` 测试运行者。
- Console：清空前计数 `error:1, warn:0, log:69`，那 1 条 error 是本轮开工时用不存在的 `unity command tests` 探测命令产生的 `ExecuteCommandByName: No command named 'tests'` 脚手架报错（原文存 `console_preclear_error_entry.txt`），不是编译失败也不是用例失败。清空后基线 `error:0, warn:0, log:0`（`console_status.baseline.txt`）；运行后 `error:0, warn:0, log:70`，cursor 由 80 到 150，即 0 错误、0 警告、+70 条 log，70 条原文存 `console_entries.after.txt`。`groundTruth.consoleLogs` 由 0 到 65，与 Pipeline 计数口径不同，两者原文都留在归档里。
- FoundationScene：测试后 `editor_status` 为 `playMode=stopped`；`eval` 读回 `Assets/Scenes/FoundationScene.unity|dirty=False|rootCount=4`，原文存 `foundationscene_dirty.txt`，场景文件无 git 改动。
- Editor.log 对应本轮：切片内固定种子 `5343414E4B4F3031`、成功月结 660 时日期 1615-04-01、五十五周年回顾页打开（正文 104675 字、27 区块、可见 27/27）、确认回顾、1615-04-01 存档、月结到 1615-05-01、读档回 1615-04-01 且时间线月结 660；随后 720 至 1200 的十年节点、1660-04-01 成功月结 1200、百年评定页（正文 252043 字、64 区块、可见 64/64、首块 3889 字 13804 顶点）、确认、1660-04-01 存档、月结到 1660-05-01、读档回 1660-04-01 且月结 1200，末行 `完成：成功月结=1200 … 日期=1660-04-01 待处理回顾=None`。
- 本轮副产物：PlayMode 运行触发 `.vscode/settings.json` 被 Unity/VSCode 集成改写、`Assets/Resources/PerformanceTestRun{Info,Settings}.json` 及其 `.meta` 被 Performance Testing 包删除。逐项看 diff 确认与本任务无关后，五个文件已恢复到 HEAD，`git status` 只剩测试文件与本报告两处改动。
- 历史记录：上一条证据目录 `Logs/verify/m08-100-20261123-1052/TestResults.xml`（9035 字节，SHA-256 `ba052d5ab25e3e8539a2ae9b7cc130bab00268340ec5ecb3bbf68cd9a3a6a868`，MD5 `7be8990f65f611b96c5621615b7a5d44`，总耗时 45.5334768 秒）保留作历史，不删除；它只有 XML 而没有 Editor.log 副本，故独立复审的有效依据以本轮 `m08-100-20261123-1101` 为准。

### 逐步路线

- 起点 1560-04-01，SC-013 固定种子 `5343414E4B4F3031`，开局断言 `CampaignCompletedYears=0`、`PendingCampaignReview=None`。
- 全程只触发生产 UI 回调：`M6NewGameButton` → `SC013FixedScenarioButton`；每月 `EndMonthButton`；事件 `EventChoice_*`；继承 `SuccessionCandidate_*` / `SuccessionRegent_*` / `SuccessionConfirmButton` / `SuccessionAcceptCrisisButton`；农庄空缺 `IndustryCandidate_industry.farm_*` → `IndustryConfirmButton`；回顾 `M08CampaignReviewContinueButton`；存档与读档 `M5SaveButton` / `M5LoadButton`。
- 每次月结都断言 `LastMonthTurnOutcome == Succeeded`，并断言 `CurrentGameDate` 恰好前进一个自然月；两项都通过才累加成功月结数。
- 1615-04-01：成功月结累计 660 次，`CampaignCompletedYears=55`，`PendingCampaignReview=StageReview`，家族未灭绝。五十五周年回顾的标题、上下文、正文（104675 字、27 区块、27/27 有顶点）与上一轮一致。
- 1660-04-01：成功月结累计 1200 次，`CampaignCompletedYears=100`，`PendingCampaignReview=CenturyReview`，家族未灭绝。
- 事件 20 次、继承 5 次、农庄任命 5 次，全部经生产面板按钮回调完成；没有写日期、确认标志或家庭数据，没有调用调试跳时、手工 `Refresh` 或测试夹具改状态。
- 两个阈值都按实际成功月结计数断言：1615-04-01 时断言 660，1660-04-01 时断言 1200。存档往返验证中多推的一个月被读档回滚，测试把累计数同步回存档时的值并单独记录回滚次数（本轮 2 次），不抬高时间线阈值。

### 两次回顾、确认与存读档

- 五十五周年（保留上一轮断言）：确认后 `PendingCampaignReview=None`、`StageReviewAcknowledged=true`；在 1615-04-01 存档，月结到 1615-05-01 后读档，日期回到 1615-04-01，已确认的回顾不再显示。
- 百年名门：面板自动显示标题 `百年名门 · 百年评定`，上下文含 `开局：1560-04-01｜已历 100 年｜100 年评定：1660-04-01｜评价日：1660-04-01`，正文 252043 字、64 个区块、64/64 区块生成可见文字，首块 3889 字、13804 顶点，缩放 1，继续按钮为 `确认百年名门评定并继续`；主界面 `DateText` 为 `1660-04-01`。
- 触发继续按钮后 `PendingCampaignReview=None`、`CenturyReviewAcknowledged=true`、回顾页隐藏；存档文件的 `campaign.stageReviewAcknowledged` 与 `campaign.centuryReviewAcknowledged` 均为 true，`initialSeedHex` 仍为 `5343414E4B4F3031`。
- 1660-04-01 存档 → 月结到 1660-05-01（断言成功且恰好一个月）→ 读档回到 1660-04-01；读档后 `PendingCampaignReview=None`、回顾页不显示、百年评定不重复弹出。
- 本用例没有重复输出五十五周年的逐区块长正文日志；百年回顾只打印汇总行与可见区块计数，避免日志膨胀。

### 运行时日志摘录

```
[M08-55] 固定种子=5343414E4B4F3031（GameSession.DefaultRandomSeed）
[M08-55] 五十五周年 成功月结=180 日期=1575-04-01 事件=12 继承=0 农庄任命=2
[M08-55] 五十五周年 成功月结=360 日期=1590-04-01 事件=20 继承=2 农庄任命=2
[M08-55] 五十五周年 成功月结=540 日期=1605-04-01 事件=20 继承=3 农庄任命=3
[M08-55] 五十五周年 成功月结=660 日期=1615-04-01 事件=20 继承=3 农庄任命=3
[M08-55] 五十五周年 · 阶段评价 回顾页已打开：标题=五十五周年 · 阶段评价｜…｜正文长度=104675｜区块=27｜…｜继续按钮=继续家族（阶段评价不终止游戏）
[M08-55] 确认回顾（五十五周年 · 阶段评价）：已确认五十五周年阶段评价：家族继续经营。
[M08-55] 存档后月结推进：1615-04-01 → 1615-05-01
[M08-55] 隔离存档→月结→读档通过：…｜读回日期=1615-04-01｜时间线成功月结=660（已回滚存档验证月结 1 次）
[M08-55] 百年名门 成功月结=840 日期=1630-04-01 事件=20 继承=3 农庄任命=3
[M08-55] 百年名门 成功月结=1200 日期=1660-04-01 事件=20 继承=5 农庄任命=5
[M08-55] 百年名门 · 百年评定 回顾页已打开：标题=百年名门 · 百年评定｜上下文=剧本来源：SC-013 固定三代剧本｜开局：1560-04-01｜已历 100 年｜55 年评价：1615-04-01｜100 年评定：1660-04-01｜评价日：1660-04-01｜正文长度=252043｜区块=64｜缩放=1｜继续按钮=确认百年名门评定并继续
[M08-55] 百年名门 · 百年评定 正文可见区块=64/64｜首块字符=3889｜首块顶点=13804
[M08-55] 确认回顾（百年名门 · 百年评定）：已确认百年名门评定。
[M08-55] 存档后月结推进：1660-04-01 → 1660-05-01
[M08-55] 隔离存档→月结→读档通过：…｜读回日期=1660-04-01｜时间线成功月结=1200（已回滚存档验证月结 2 次）
[M08-55] 完成：成功月结=1200（另有存档往返验证后回滚的月结 2 次） 事件=20 继承=5 农庄任命=5 日期=1660-04-01 待处理回顾=None
```

### 边界与未闭合门槛

- 本报告的「生产路径」只指自动触发场景中生产按钮的 `Button.onClick` 回调（`onClick.Invoke()`），再让会话状态自然联动；没有模拟物理鼠标、触摸、EventSystem 射线命中、导航路径或键盘焦点。
- 没有构建或运行 Windows Player；没有人工逐帧检查百年回顾 64 个区块的滚动、排版与文字截断。自动化通过不等于用户实机验收。
- 用户最终验收仍是关闭条件：需要用户在 Play Mode 或 Player 中走同一路线，亲眼确认两次回顾的显示、滚动、继续操作与存档读档。
- 本轮没有改产品代码、场景、预制体、ProjectSettings 或用户设置；只改 `Assets/Tests/PlayMode/M08CampaignReviewPlayModeTests.cs` 与本报告。

## 2026-11-23 补强：逐次证明月结成功

本节是五十五周年口径的历史验证记录，只覆盖到 1615-04-01，仍可复现；百年名门的最新结果见上一节。

- 工作树 `C:\Users\panye\orca\workspaces\My project\m08-55-success-evidence`，分支 `panyeung/m08-55-success-evidence`，基于 PR #4 原提交 `aec75e8b198808128709dfea05980f481150a00b`。Unity 6000.6.6f1 Editor PID 54508，Pipeline 端口 7800。
- 编译：`recompile` 与 `recompile_status` 均为 `up_to_date`，`compilationFailed=false`。`SengokuChronicle.PlayModeTests.dll` 时间为 2026-11-23 20:22:47 UTC，晚于测试源文件的 20:17:05 UTC。
- Play Mode：`M08CampaignReviewPlayModeTests.StageReview_OpensAtFiftyFiveYears_ConfirmsAndSurvivesSaveLoad`，1/1 通过，16.160 秒。证据副本 `Logs/verify/m08-55-20261123-2025/TestResults.xml`，6786 字节，SHA-256 `35B46F1E4096535823A4B36825FF6152AE058522A8816BDE322A17C45A39F715`。
- 测试每次触发生产 `EndMonthButton` 后都断言 `LastMonthTurnOutcome == Succeeded`，并断言日期恰好前进一个日历月；两项通过后才增加成功月结数。测试另核对 1560-04-01 至 1615-04-01 相差 660 个日历月，并在到达该日时断言恰有 660 次成功月结。
- `Editor.log` 记录固定游戏种子 `5343414E4B4F3031`、成功月结 660 时的日期 1615-04-01、阶段回顾页打开、确认后保存、月结到 1615-05-01、读档恢复 1615-04-01。最终日志为成功月结 661，因为存档验证额外推进了一个月；这不改变到达评价日时的 660 次断言。
- Console 运行前计数 `error:0, warn:3, log:0`，运行后 `error:0, warn:3, log:59`，本轮新增 0 错误、0 警告。初始 3 条警告在测试前已存在。
- Orca 独立只读复审结论：ACCEPT。复审核对了逐次断言、660 次计数及 XML 与 `Editor.log`。

此测试触发生产按钮的 `Button.onClick` 回调，不模拟物理输入；它不替代 Windows Player 试玩或用户最终验收。

## 2026-11-21 补强：记录并断言固定种子

任务要求「固定并记录种子」，上一轮只在报告里隐含这一前提，测试没有回读。本轮补上，且必须重跑证据才算数：

- 测试断言 `GameSession.DefaultRandomSeed` 的 16 位十六进制为 `5343414E4B4F3031`，SC-013 新游戏固定走这个默认种子（`GameSession.InitNewGame()` → `InitNewGame(DefaultRandomSeed)`）。
- 存档后直接解析隔离槽的 `save-slot-1.json`，断言 `randomState.initialSeedHex == 5343414E4B4F3031`，并在日志里记录，证明 660 次月结确实跑在这个固定种子上，而不是只在入口处声称。
- 私有助手 `AdvanceToStageReview` 更名为 `DriveMonthsToStageReview`，避免与产品 API `AdvanceTo` 混名，独立复审 grep 时不再产生歧义。

## 本轮修正（独立复审 RETURN）

- **唯一必须修的一项。** 存档与读档之间改为经生产按钮回调成功推进一个月，再读档断言状态回到存档时刻：先在 1615-04-01 存档，触发 `EndMonthButton.onClick` 使日期推进到 1615-05-01，再触发 `M5LoadButton.onClick`，断言日期回到 1615-04-01、`PendingCampaignReview` 仍为 `None`、回顾页不显示。
- 测试仍不直接写日期、家族、回顾标记，也不直接调用确认 API 或 `Refresh`。
- 报告措辞收窄：把原先的「真实玩家路径 / 真实按钮」改为「生产按钮回调被自动触发」，并明确不声称物理鼠标、EventSystem 导航或焦点命中已验证。

## 结论

- **正常新局路线可以走到 1615-04-01 并自动打开五十五周年阶段回顾页。** 从标题页触发「新游戏」→「SC-013 固定三代剧本」的按钮回调，随后只触发界面「结束本月」的回调推进 660 个自然月，期间每个事件、每次继承、每次农庄空缺都触发生产面板的按钮回调处理，未跳日、未写日期、未注入家族、未直接调用 `Refresh`。
- **回顾页的三项自动行为、正文渲染、继续确认、以及确认后不再复弹（含存档、月结、读档）全部通过。** 确认后在 1615-04-01 存档，经「结束本月」成功推进到 1615-05-01 且待处理回顾保持 `None`，随后在隔离槽读档，日期回到 1615-04-01、待处理回顾仍为 `None`、回顾页不显示。
- **本轮零产品代码改动。** 没有发现需要修补的产品缺陷；前一次失败是本测试自己把继承面板切到了错误的页面，属测试缺陷，已在测试内修正。

## 复现方式

Play Mode 测试文件 `Assets/Tests/PlayMode/M08CampaignReviewPlayModeTests.cs`：

```
unity command run_tests --project-path "C:\Users\panye\orca\workspaces\My project\m08-55-year-review-runtime" ^
  --mode PlayMode --filter M08CampaignReview --async_tests true --timeout 1800
```

`run_tests` 异步返回 `0/0`，随后轮询 `unity command test_status` 到 `completed`，并确认共享 `TestResults.xml` 时间戳已更新后再复制。

测试只触发生产 UI 的按钮回调：

| 环节 | 触发的生产控件 |
|---|---|
| 开局 | `M6NewGameButton` → `SC013FixedScenarioButton` |
| 月结 | 切到「地图」页 → `EndMonthButton` |
| 事件 | `EventChoice_*` 选项按钮（同目标重试直到队列排空） |
| 继承 | 切到「家族」页 → `SuccessionCandidate_*` / `SuccessionRegent_*` / `SuccessionConfirmButton` / `SuccessionAcceptCrisisButton` |
| 产业 | 切到「家计」页 → `IndustryCandidate_industry.farm_*` → `IndustryConfirmButton` |
| 回顾确认 | `M08CampaignReviewContinueButton` |
| 存档 → 月结 → 读档 | `M5SaveButton` → `EndMonthButton` → `M5LoadButton`（隔离临时槽目录） |

措辞限定：本报告说的「玩家路径」只指测试直接调用场景里生产按钮的 `Button.onClick` 回调（`onClick.Invoke()`），再让会话状态自然联动；测试不模拟物理鼠标点击，也不验证 EventSystem 指针命中、导航路径或焦点落点。这些交互仍需要人工操作或 Computer Use 单独取证，本报告不声称已验证。

## 验证（实测）

2026-11-21 本轮实测（证据目录 `Logs/verify/m08-55-20261121-2029`）：

| 项 | 命令 | 结果 |
|---|---|---|
| 编译 | `unity command recompile` + `recompile_status` | `up_to_date`（启动时已完成导入编译）；`console_status` 报 `compilationFailed=false`；`SengokuChronicle.PlayModeTests.dll` 时间晚于改后源文件且含新助手名，确认跑的是改后代码 |
| Play Mode | `unity command run_tests --project-path "$PWD" --mode PlayMode --filter M08CampaignReview --async_tests true --timeout 1800`，再轮询 `test_status` | `completed：1/1 个通过`；`total="1" passed="1" failed="0"`，时长 19.67 s |
| Console | `unity command console_status` 前后对比 | 运行前 `error:0, warn:0, log:6`，运行后 `error:0, warn:0, log:56`，同一 session 未 reset。本轮**新增 0 错误 0 警告、50 条 log** |
| 证据副本 | `bash scripts/copy-testresults.sh Logs/verify/m08-55-20261121-2029` | 6745 B，SHA-256 `36afa5f57e372d5ab760ff0c0c8d454d37ec79882e23f4d6003ebf75f0d402fe` |

上一轮（2026-11-19，证据 `Logs/verify/m08-55-20261119-2345`，6601 B，SHA-256 `bae9603864fa62c141680bd3ab7735985efcad67478365f222c2b25772ab52d0`）为 1/1 通过、11.245 s；该副本仍保留可对照。

### 运行时证据（摘自证据副本的测试日志）

```
[M08-55] 固定种子=5343414E4B4F3031（GameSession.DefaultRandomSeed）
[M08-55] 月份=60 日期=1565-04-01 事件=12 继承=0 农庄任命=1
[M08-55] 月份=180 日期=1575-04-01 事件=12 继承=0 农庄任命=2
[M08-55] 月份=360 日期=1590-04-01 事件=20 继承=2 农庄任命=2
[M08-55] 月份=660 日期=1615-04-01 事件=20 继承=3 农庄任命=3
[M08-55] 回顾页已打开：标题=五十五周年 · 阶段评价｜上下文=剧本来源：SC-013 固定三代剧本｜
         开局：1560-04-01｜已历 55 年｜55 年评价：1615-04-01｜100 年评定：1660-04-01｜
         评价日：1615-04-01｜正文长度=104675｜区块=27｜缩放=1｜
         继续按钮=继续家族（阶段评价不终止游戏）
[M08-55] 正文区块 0 字符=3907 顶点=13892 … 正文区块 26 字符=2032 顶点=7768
[M08-55] 确认阶段回顾：已确认五十五周年阶段评价：家族继续经营。
[M08-55] 存档种子=5343414E4B4F3031
[M08-55] 存档后月结推进：1615-04-01 → 1615-05-01
[M08-55] 隔离存档→月结→读档通过：...\Temp\m08-55-review-playmode-<guid>\save-slot-1.json｜读回日期=1615-04-01
[M08-55] 完成：月份=661 事件=20 继承=3 农庄任命=3 日期=1615-04-01 待处理回顾=None
```

到达 1615-04-01 时测试断言的项：

- `GameSession.WorldClock.CurrentDate == 1615-04-01`，`CampaignCompletedYears == 55`，`PendingCampaignReview == StageReview`，家族未灭绝。
- `CampaignReviewPanel.IsVisible == true`，`M08CampaignReviewPage.activeInHierarchy == true`，回顾根节点位于 M02 日常行动页之上、标题/事件选择/战术覆盖层之下（`CanvasLayerOrder` 共享座次）。
- 标题与继续按钮文案与 `CampaignReviewView` 常量一致；上下文含开局日期、已历 55 年、评价日。
- 正文 104675 字、27 个渲染区块，逐块 `Text.text` 拼接与面板 `Body` 完全一致，每块 `cachedTextGenerator.vertexCount > 0`（7768–14400 顶点），即实际生成了可见文字而不是空页。
- 主界面 `DateText` 显示 `1615-04-01`。

确认后的存档、月结与读档断言的项：

- 确认发生在 1615-04-01；`M5SaveButton` 回调报 `LastOperationSucceeded == true`，隔离槽里出现 `save-slot-1.json`，存档日期为 1615-04-01。
- 触发 `EndMonthButton` 回调后 `WorldClock.CurrentDate == 1615-05-01`，主界面 `DateText == "1615-05-01"`，`PendingCampaignReview == None`，回顾页仍不显示。这条证明月结是一次成功的一月推进，而不是被拒绝的空操作。
- 触发 `M5LoadButton` 回调后日期回到存档时的 1615-04-01，`DateText == "1615-04-01"`，`PendingCampaignReview == None`，`CampaignReviewPanel.IsVisible == false`，`M08CampaignReviewPage` 不在活动层级。

## 排查记录：继承面板在哪个页面

第一次运行在 1575-04-01 失败，日志显示 `阶段=AwaitingSuccessionChoice 候选数=1 面板可见=False`。原因是测试原先把「继承」当作「家计」页的内容，而 `AddM02DailyActionPanel.PageMaskForRoot` 把 `SuccessionRoot` 挂在**家族**页（`Family`），`IndustryRoot` 才是家计页。切错页时 `SuccessionRoot` 整棵被导航隐藏，面板自然不可见。测试改为切「家族」页后，三次继承全部由生产候选/确认按钮回调完成。

## 改动文件

- `Assets/Tests/PlayMode/M08CampaignReviewPlayModeTests.cs`（本轮修改）—— 新增固定种子断言：入口断言 `GameSession.DefaultRandomSeed` 为 `5343414E4B4F3031`，存档后回读 `save-slot-1.json` 的 `randomState.initialSeedHex` 再断言一次并写日志；私有助手 `AdvanceToStageReview` 更名为 `DriveMonthsToStageReview`。上一轮已把「确认后继续游戏」与「存档读档」合成一步：先存档，再触发 `EndMonthButton` 回调成功推进一个月，最后读档并断言日期、回顾标记与回顾页三者都恢复到存档时刻。
- `docs/reports/m08-55-review-playmode-20261119.md`（本轮修改）—— 补记种子断言、更新证据路径与实测数字。
- `Assets/Tests/PlayMode/SengokuChronicle.PlayModeTests.asmdef` 与两个 `.meta` 沿用上一提交，未再改动。
- 产品代码、场景、预制体、`ProjectSettings` 均未改动。

## 未做 / 说明

- 未截 Computer Use 截图：唯一合法路线是 660 次生产月结回调，只在自动化测试的 11 秒窗口内成立，手工截屏无法在同一时刻复现；改用面板自身报告的可见文字顶点与区块拼接作为渲染证据。
- 未跑 EditMode 全量：本轮没有产品代码改动，EditMode 覆盖面不变。
- 未合并 `main`、未打 tag；最终里程碑验收仍在用户手里。
- 主工程工作树 `C:\Users\panye\My project` 的 55/100 年预验收不属于本卡证据。
- 本轮临时探针 `scripts/verify/M08ReviewRouteProbe.cs`、`scripts/verify/M08SaveLoadStateProbe.cs` 已删除，未入库。
- Play Mode 运行会临时删除并重建 `Assets/Resources/PerformanceTestRunInfo.json` 与 `PerformanceTestRunSettings.json`；跑完已用 `git checkout --` 恢复到未改动状态，未纳入提交。
- `.vscode/settings.json`、`Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`、`ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json` 是打开 Editor 产生的本地噪声，保留未暂存。

## 遗留风险

- **继承弹窗只在「家族」页渲染。** 若家主在「地图」页月结时去世，`SuccessionRoot` 整棵被页面导航隐藏，弹窗与继承 HUD 都不会出现；此时推进时间会被挡住并提示「尚有继承决策待处理：请先完成当前继承选择，再推进时间」，但提示没有指向「家族」页。这是既有页面分层设计，不是本轮引入，也不在本卡范围；若后续要做新手引导或无软锁体验，值得单独开卡确认。
- **SC-013「继承前的家议」事件在 660 个自然月里没有触发。** 该事件权重 1、优先级 1000、`SuccessionCountEquals 0`，本轮 20 个事件中没有抽到它。因此「退休并安排继承」的手动路线在不反复重开的前提下不可用，实际三次继承都由自然死亡触发。本轮目标（走到 1615）不受影响，但手动交接路线在实际抽取下能否出现，需要独立的概率/权重专项确认。
