# M08 五十五周年阶段回顾 Play Mode 端到端验证报告（2026-11-19）

- 工作树 `C:\Users\panye\orca\workspaces\My project\m08-55-year-review-runtime`，分支 `feature/m08-55-year-review-runtime`，本轮回合开工 HEAD `2f7ecdf`。
- 引擎 Unity 6000.6.6f1，本树 Editor 的 Pipeline 端口 7803（主工程 7801，未触碰）。
- 证据副本：`Logs/verify/m08-55-20261119-2345/TestResults.xml`（6601 字节，SHA-256 `bae9603864fa62c141680bd3ab7735985efcad67478365f222c2b25772ab52d0`，`Logs/` 按仓库约定不入库）。

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

| 项 | 命令 | 结果 |
|---|---|---|
| 编译 | `unity command recompile` + `recompile_status` | `completed`；`console_status` 报 `compilationFailed=false` |
| Play Mode | 上表命令，`--filter M08CampaignReview` | `1/1 个通过`；`total="1" passed="1" failed="0"`，时长 11.245 s |
| Console | `unity command console_status` 前后对比 | 运行前 `error:8, warn:9, log:210`（cursor 227），运行后 `error:8, warn:9, log:261`（cursor 278），同一 session 未 reset。本轮**新增 0 错误 0 警告、51 条 log**（8 条 error 是本次会话早前编译失败与 pipeline 超时的历史累计，非本轮） |
| 证据副本 | `bash scripts/copy-testresults.sh Logs/verify/m08-55-20261119-2345` | 6601 B，SHA-256 `bae9603864fa62c141680bd3ab7735985efcad67478365f222c2b25772ab52d0` |

### 运行时证据（摘自证据副本的测试日志）

```
[M08-55] 月份=180 日期=1575-04-01 事件=12 继承=0 农庄任命=2
[M08-55] 月份=360 日期=1590-04-01 事件=20 继承=2 农庄任命=2
[M08-55] 月份=660 日期=1615-04-01 事件=20 继承=3 农庄任命=3
[M08-55] 回顾页已打开：标题=五十五周年 · 阶段评价｜上下文=剧本来源：SC-013 固定三代剧本｜
         开局：1560-04-01｜已历 55 年｜55 年评价：1615-04-01｜100 年评定：1660-04-01｜
         评价日：1615-04-01｜正文长度=104675｜区块=27｜缩放=1｜
         继续按钮=继续家族（阶段评价不终止游戏）
[M08-55] 确认阶段回顾：已确认五十五周年阶段评价：家族继续经营。
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

- `Assets/Tests/PlayMode/M08CampaignReviewPlayModeTests.cs`（本轮修改）—— 把「确认后继续游戏」与「存档读档」两步合成一步：先存档，再触发 `EndMonthButton` 回调成功推进一个月，最后读档并断言日期、回顾标记与回顾页三者都恢复到存档时刻。文件头注释同步收窄措辞。
- `docs/reports/m08-55-review-playmode-20261119.md`（本轮修改）—— 记录 RETURN 修正、收窄措辞、更新证据路径与实测数字。
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
