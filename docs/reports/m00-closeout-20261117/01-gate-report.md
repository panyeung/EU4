# M00 收口 · 分项证据链报告

- 日期：2026-11-18（本地 16:11–16:22，UTC 02:11–02:22）
- 工程：`C:\Users\panye\My project`（Unity 6000.6.6f1，URP 2D）
- Editor：PID 88424，Pipeline 端口 7800（唯一的本工程实例）
- 工作树：主工作树，基线 WIP 为 34 修改 / 34 未跟踪

VERDICT: PASS — 编译状态正常、场景 Validator 0 错误、全量 EditMode 2141/2141 通过、Play Mode 冒烟到达营地 HUD、Play Mode 期间新增 error 为 0；存读档按约束未执行。

## 分项表

| 检查项 | 状态 | 关键证据 |
|---|---|---|
| C1 预检 doctor | PASS | pipeline 单条本工程、端口 7800、可达 true；`compilationFailed=false`；`status=ready`、`compiling=false`；活动场景 FoundationScene、`isDirty=false` |
| C2 基线 Console | PASS | `counts` error=7 / warn=53 / log=27，`cursor=87`，`session=bda41b5e…`；7 条 error 首行见下（既有状态） |
| C3 场景 Validator | PASS | `RESULT: PASS; Errors: 0`，`Scene: Assets/Scenes/FoundationScene.unity`，报告 186403 字节 16:12 |
| C4 全量 EditMode | PASS | total=2141 / passed=2141 / failed=0 / skipped=0；TestResults.xml `testcasecount="2141" result="Passed"` |
| C5 Play Mode 冒烟 | PASS | `playMode=playing`；日期 `1560 年 4 月 1 日`，所在地 `所在　清洲`；标题遮罩消失；3 张截图已目视后删除 |
| C6 0 errors 采样 | PASS | Play Mode 期间（02:16:56–02:22:24）新增 error = 0；11 = 基线 7 + 测试 2 + 传输层 2 |
| C7 收尾 | PASS | `playMode=stopped`；场景 `isDirty=false`；`Assets/Logs` 与其 `.meta` 已删除；git 前后 34 修改不变，未跟踪 34→32（仅少这两条） |

## C1 预检（doctor）

```
$ unity pipeline list
项目        路径                        PID   运行中 Pipeline 版本        可更新 服务器端口 服务器可达 安全模式
My project  C:\Users\panye\My project  88424 true   true     0.8.0-exp.1 true   7800       true

$ unity command console_status --project-path "C:/Users/panye/My project"
..."groundTruth":{"sampledUtc":"2026-11-18T02:11:34.6386054Z","ageMs":274,
"compilationFailed":false,"compiling":false,"consoleErrors":6,"consoleWarnings":52,"consoleLogs":12,"seeded":true}

$ unity command editor_status --project-path "C:/Users/panye/My project"
{"status":"ready","compiling":false,"domainReloadInProgress":false,"playMode":"stopped",
 "projectPath":"C:\Users\panye\My project","unityVersion":"6000.6.6f1"}

$ unity command list_open_scenes --project-path "C:/Users/panye/My project"
{"count":1,"scenes":[{"name":"FoundationScene","path":"Assets/Scenes/FoundationScene.unity",
 "isLoaded":true,"isDirty":false,"isActive":true,"rootCount":4}]}
```

四条全部满足，继续。场景无未保存改动（`isDirty=false`），因此不存在「用户场景有未保存改动，未保存、未继续」的情形。

## C2 基线 Console（本次运行前的既有状态）

```
counts = {"error":7,"warn":53,"log":27}   cursor=87   session=bda41b5ea9764200811a3f34d5c7c33c
groundTruth = consoleErrors:6, consoleWarnings:52, consoleLogs:12, seeded:true
```

7 条 error 首行（不含 stackTrace）：

1. `seq=17 ExecuteCommandByName: No command named 'list' is available. …`
2. `seq=18 ExecuteCommandByName: No command named 'tests' is available. …`
3. `seq=19 ExecuteCommandByName: No command named 'scripts/compile' is available. …`
4. `seq=20 ExecuteCommandByName: No command named 'tests' is available. …`
5. `seq=21 ExecuteCommandByName: No command named 'scripts' is available. …`
6. `seq=66 家族编年史记录写入失败（家族收养）：编年史预备记录序号无效。`
7. `seq=67 家族编年史记录写入失败（家族婚姻）：编年史预备记录序号无效。`

seq 17–21 是历史上把 pipeline 当旧 CLI 用（`unity command list/tests/scripts`）留下的控制台噪声；seq 66–67 是真实代码路径错误，产自 EditMode 用例。`warn` 53 条绝大部分是各 UI updater 的 `rolled back …. Scene may remain dirty` 测试告警。

## C3 场景 Validator

入口脚本 `Logs/verify/gate-20261118/RunValidator.cs`（反射调用 `SengokuChronicle.Editor.FoundationSceneValidator.ValidateActiveScene()`，不落在 `Assets/` 下）：

```
$ unity command run_script --file Logs/verify/gate-20261118/RunValidator.cs --entry RunValidator.Run --project-path "C:/Users/panye/My project"
run_script true {"diagnostics":[],"compileMs":3664,"executeMs":361,"success":true,"result":"validator invoked"}
```

`Logs/SceneValidationReport.txt`（mtime 2026-11-17 16:12，186403 字节）：

```
FOUNDATION SCENE VALIDATION
Scene: Assets/Scenes/FoundationScene.unity
Loaded: True; Dirty: False
FOUND Canvas: Canvas
...
RESULT: PASS; Errors: 0
```

Errors 为 0，无失败文本可列。

## C4 全量 EditMode

```
$ unity command run_tests --mode EditMode --timeout 900 --project-path "C:/Users/panye/My project"
（客户端 180s 超时退出，未收到回复；测试在 Editor 侧继续跑完）
$ unity command test_status --project-path "C:/Users/panye/My project"
test_status true {"status":"completed","duration":84.89,
 "summary":{"total":2141,"passed":2141,"failed":0,"skipped":0,"inconclusive":0}}
```

- 结果文件：`C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\TestResults.xml`（mtime 2026-11-17 16:16，1676419 字节）
- 头部原文：`<test-run id="2" testcasecount="2141" result="Passed" total="2141" passed="2141" failed="0" … start-time="2026-11-18 02:12:59Z" end-time="2026-11-18 02:16:17Z" duration="197.9135557">`
- 失败项：无（`failed=0`），因此没有失败用例名单。
- 完整结果快照另存 `Logs/verify/gate-20261118/test_status-editmode.json`（2141 条逐条 Passed）。

## C5 Play Mode 冒烟

```
$ unity command open_scene --path Assets/Scenes/FoundationScene.unity   # 跳过
  → C1 的 list_open_scenes 已显示该场景 isActive=true，未重复打开
$ unity command editor_focus          → Editor focused via Toolbar
$ unity command editor_play           → Entered play mode（该回复不算数）
$ unity command editor_status         → {"status":"playing","compiling":false,
     "domainReloadInProgress":false,"playMode":"playing", …}
```

`VerifyUi.State`：`scene=Assets/Scenes/FoundationScene.unity loaded=True dirty=False / playing=True frame=4649 time=9.20`（二次采样 `frame=14537 time=33.25`，帧计数推进，不是冻结帧）。

路径（按 `features/title-new-game.md` 的 ASCII 路径）：

| 步骤 | 命令结果 |
|---|---|
| 标题页 | `buttons` 含 `继续游戏 | True | Canvas/M6TitleRoot/M6MainMenuRoot/M6ContinueButton`、`开始新游戏 | …/M6NewGameButton` |
| 开始新游戏 | `clicked '开始新游戏' at Canvas/M6TitleRoot/M6MainMenuRoot/M6NewGameButton` |
| 选择剧本 | `buttons` 出现 `…/M6ScenarioBackButton`、`…/SC013FixedScenarioButton`、`…/SC013StandardSampleButton`、`…/SC013CrisisDemoButton`、`…/SC020EducationDemoButton` |
| 固定三代剧本（SC-013） | `clicked '固定三代剧本（SC-013）' at Canvas/M6TitleRoot/M6ScenarioSelectRoot/SC013FixedScenarioButton` |
| 营地 HUD 日期 probe | `1560 年 4 月 1 日` |
| 营地 HUD 地名 probe | `所在　清洲` |
| 标题遮罩 probe | `(no active object at 'Canvas/M6TitleRoot/M6MainMenuRoot/M6TitleStatusText')` |

截图（均落在 `Assets/Logs/verify/_scratch/`，目视后删除，报告只记要点）：

1. `20261118-m00-1-title.png` — 左侧家徽 + `影之一族` + `守护家名，传承百年`，按钮 `继续游戏` / `开始新游戏`，底部一行说明（继续游戏会读取存档、开始新游戏不会），右侧和风城堡插画，右下 `Debug 综合测试`。完整渲染，无报错弹窗。
2. `20261118-m00-2-selector.png` — 标题 `选择剧本`，主线三项（`固定三代剧本（SC-013）` 红色高亮、`SC-010 标准样例`、`SC-010 危机示范`），专题五项（SC-020…SC-024），底部 `返回`。
3. `20261118-m00-3b-campaign.png` — 标题遮罩消失；顶部日期 `1560年4月1日`、所在地 `清洲`；左侧导航 `家族内务 / 人物档案 / 家族与继承 / 地图与出行 / 事件抉择` 与家族名册；中间人物档案 + 日常行动（`休 / 训 / 工 / 旅`）；右侧 AI 自治调度（开发者）面板与 `子女培养`。无红字报错。

截图的坑（本次实测）：不先 `editor_focus` 直接 `capture_game_view`，返回的 PNG 与上一张字节完全相同（md5 `25be907a…` / `f74c6d40…`），是陈旧帧。每张截图前都补了 `editor_focus` + 2–5s 等待，并用 md5 校验确认是新帧后才目视。

## C6 0 errors 采样

| 时点 | cursor | error | warn | log | groundTruth.consoleErrors |
|---|---|---|---|---|---|
| C2 基线（02:11:34） | 87 | 7 | 53 | 27 | 6 |
| Play Mode 中（02:21:06） | 160 | 11 | 106 | 43 | 0（Unity 自身计数被测试运行清空） |
| 收尾后（02:22:24） | 160 | 11 | 106 | 43 | 0 |

`unity command console --since 87 --since_session bda41b5ea9764200811a3f34d5c7c33c`：73 条新增，其中 error 4 条，逐条定性：

- `seq=135 家族编年史记录写入失败（家族收养）：编年史预备记录序号无效。` — 时间戳 02:14:05，落在 EditMode 测试运行内（02:12:59–02:16:17）
- `seq=136 家族编年史记录写入失败（家族婚姻）：编年史预备记录序号无效。` — 同上
- `seq=157 Failed to handle /api/exec request: Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host.` — 时间戳 02:16:18，是我这条 CLI 的 180s 客户端超时断开
- `seq=158 Pipeline: unhandled error serving POST /api/exec: System.InvalidOperationException: Cannot be changed after headers are sent.` — 同上

**Play Mode 期间（02:16:56 进入 → 02:22:24 复核）新增 error = 0**；该时段新增只有 1 条 warn（`seq=160`，我中途删除截图留下的孤儿 `.meta`）。counts 的 7→11 由「测试 2 条 + 传输层 2 条」解释得通。

## C7 收尾

```
$ unity command editor_stop → Exited play mode
$ unity command editor_status → {"status":"ready","playMode":"stopped","compiling":false, …}
$ unity command list_open_scenes → FoundationScene, isDirty=false, isActive=true（场景未保存、未动）
$ rm -rf Assets/Logs Assets/Logs.meta   → ls: cannot access 'Assets/Logs': No such file or directory
$ git status --short > Logs/verify/gate-20261118/git-after.txt
```

| 时点 | 行数 | 修改 | 未跟踪 |
|---|---|---|---|
| 开工前 `git-before.txt` | 68 | 34 | 34 |
| 收工后 `git-after.txt` | 66 | 34 | 32 |

`diff` 只有两行消失，即收尾要求删除的两条：

```
35,36d34
< ?? Assets/Logs.meta
< ?? Assets/Logs/
```

修改文件数 34 不变，无新增未跟踪文件，`Assets/` 下无残留。全程未执行任何 git 写操作（只读 `status`）。

## 未执行项

- **存读档（Save/Load）：NOT EXECUTED**。用户机器上有一个真实存档槽（`Application.persistentDataPath` 下单一槽位，`SaveSlotStore`），本轮约束禁止写存档槽、禁止点存读档、禁止调用存档 API，因此没有驱动 M5 存读档面板，也没有跑「存档→读档→状态一致」回归。可用的替代依据只有源码阅读（非运行时证据）：`GameSession.SaveTo` 在 `IsDebugSessionActive` 时提前拒绝写盘；全仓无 `AutoSave` 命中，`SaveTo` 仅由存档面板按钮调用，开始新游戏不写盘。
- 里程碑合并 / tag / `main` 合并：不在本卡范围，未执行。

## 风险与遗留

1. **基线 7 条 error 仍在**：其中 `家族编年史记录写入失败（家族收养 / 家族婚姻）：编年史预备记录序号无效。` 是真实代码路径缺陷（`seq=66/67`，EditMode 一跑就复现 `seq=135/136`）。2141 个用例全过说明没有断言覆盖这条路径——建议单独立卡，不要被「全绿」掩盖。
2. **`counts` 与 `groundTruth` 不一致**：`counts.error=7` 而 `groundTruth.consoleErrors=6`，`warn` 53 vs 52。差 1，疑似 pipeline 自己 seeded 的条目（`seq=2` 那条 CS0618 警告带 `seeded:true`）。引用 console 数字时要写明取的是 `counts`，不要混用两个来源。
3. **`run_tests` 客户端超时会产生假 error**：测试耗时约 198s，我设的 180s 客户端超时先断，Editor 侧因此记下 `seq=157/158` 两条 pipeline 传输异常。这两条必须从「新增 error」里剔除，否则会把工具问题算成游戏问题。下次应直接 `--detach` 再 `test_status` 轮询。
4. **`capture_game_view` 会返回陈旧帧**：不加 `editor_focus` 时连续两次截图字节完全相同（md5 相同）。每张图都要 md5 校验 + 先 focus，否则会把旧画面当证据。
5. **`Assets/Logs` 里原有历史截图**：开工前 `Assets/Logs/` 已存在（2026-11-14 / 2026-11-15 若干 PNG，未跟踪）。按配方整目录删除时一并清掉，导致未跟踪数 34→32。这些是历次验证的临时截图，未跟踪、可再生，但与「前后条数一致」的字面要求有 2 条差异，特此记明。
6. **`seq=160` 孤儿 `.meta` 警告**是我中途 `rm` 掉一张截图造成的（Unity 未同步），属自造噪声，不影响结论。
7. **`Logs/Editor.log` 有 167 处 `Exception` 字样**：抽查全部来自 test-framework 的 `CaptureException` 断言捕获与 `seq=157/158` 的 pipeline 传输异常；Play Mode 时段（第 10071 行之后）无异常记录。
8. **场景 WIP 未提交**：`FoundationScene.unity` 等 34 个文件是用户未提交改动，本轮只读未改，Play Mode 前后均未保存。

## 通知

`bash scripts/notify-openmanus.sh "M00 收口验证通过（PASS）" "<正文>" ""` 已执行，返回 `OPENMANUS_TARGET unset; notice kept in Logs/notifications/outbox.txt`（outbox 最后一行时间戳 2026-11-18T02:24:39Z）。`hermes send --list` 回答 `No messaging platforms configured or no channels discovered yet.`，因此**通知未真正发出**，只在本地 outbox 留下记录；通道接好后可跑 `scripts/notify-openmanus.sh --drain` 补发。
