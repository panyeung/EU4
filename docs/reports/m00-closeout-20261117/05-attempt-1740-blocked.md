# M00 收口重派 — 基线 EditMode + SAVE-3 / SAVE-4（卡 t_0d10a593）

末行判定：**BLOCKED** — C1 与 C4 已实测通过；C2/C3 未能执行，因为 Editor（PID 88424 / 端口 7801）在 2026-11-18T03:58Z 之后主线程卡死，pipeline 全部命令 30–90 s 超时，`editor_stop` 也超时，无法在规则允许的范围内恢复。

- 工程：`C:\Users\panye\My project`，Unity 6000.6.6f1
- Editor：PID 88424，`unity pipeline list` 显示端口 **7801**（卡面写的 7800；以 `pipeline list` 实际值为准），`status=ready` / `playMode=stopped`（开工时）
- 本轮没有改 `Assets/` 下任何文件、没有改场景 / prefab / `ProjectSettings/`、没有切分支、没有提交、没有推送
- 本轮没有写真实存档目录；真实槽前后哈希与 mtime 逐字相同（见 C4）

## 0. 分支与提交（开工时 / 结束时）

| 时刻 (UTC) | 分支 | HEAD | 说明 |
|---|---|---|---|
| 03:41Z（开工） | `main` | `9e78821 docs: 写入并发与共享资源规则，并加 TestResults 取证脚本` | 开工时记录 |
| 03:46Z | `main` | `55fb764 docs: M00 存读档 SAVE-1～SAVE-4 全过并入表` | 并行会话在我跑测试期间提交，非本卡操作 |
| 04:12Z（结束） | `main` | `ca96f4f docs: M04 收口全绿入表，分支 94e82bf 已推送` | 又一个并行提交 |

工作树里 34 个修改 + 若干未跟踪文件在开工前就存在（`git status --porcelain` 69 行，含他人 WIP），本卡全程未触碰、未提交。`Logs/` 被 `.gitignore` 忽略，本卡新增证据全在 `Logs/verify/` 下。

## 1. 按顺序执行的命令与结果

```
03:41:02Z  unity pipeline list
             My project / C:\Users\panye\My project / PID 88424 / 端口 7801 / 可达 true
             ui-m04-family / C:\Users\panye\orca\workspaces\My project\ui-m04-family / PID 143064 / 端口 7803
             结论：只有 PID 88424 指向本工程；7803 是另一个工作树，全程未访问
03:41:02Z  unity command editor_status --project-path "C:/Users/panye/My project"
           status=ready  compiling=false  domainReloadInProgress=false  playMode=stopped
03:41:02Z  unity command console_status        → cursor=245 session=bda41b5e…c33c
           counts={error:13, warn:161, log:71}   groundTruth={consoleErrors:0, consoleWarnings:1, consoleLogs:0}
03:41:0xZ  sha256sum/stat 真实槽（见 C4，前值）
03:41:24Z  unity command run_tests --mode EditMode --detach
           → job 3b1f4cd7257240c7bd9e3b237f6cd5fd (queued)
03:46:16Z  unity command test_status → "completed：2141/2141 个通过"
03:46:24Z  scripts/copy-testresults.sh Logs/verify/20261118-m00/
           → 已复制，md5 ad57fd438391a25504b98571709e63da，1676355 B
03:53:24Z  unity command console_status        → cursor=413 session 同上
           counts={error:45, warn:265, log:103}  groundTruth={consoleErrors:5, consoleWarnings:53, consoleLogs:16}
03:53:29Z  unity command console --since 245 --level error  → 32 条（分类见 C5）
03:53:48Z  unity command editor_play → "Entered play mode"
03:53:56Z  unity command editor_status → playMode=playing
03:55:0xZ  unity command set_autotick --enable true --timeout 60   → 超时（60 s）
03:55:xxZ  unity command run_script VerifyUi.State --timeout 60    → 超时（60 s）
03:56:xxZ  unity command editor_focus --timeout 45                 → 超时（45 s）
03:57:49Z  unity command editor_status --timeout 45                → 超时（45 s）
04:02:19Z  unity command editor_status --timeout 60                → 超时（60 s）
04:07:55Z  unity command editor_stop --timeout 45                  → 超时（45 s）
04:12:18Z  unity command editor_status --timeout 40                → 超时（40 s）
```

## 2. `editor_status` 前后值

| 时刻 | status | playMode | 备注 |
|---|---|---|---|
| 03:41:02Z | ready | stopped | 开工前置检查通过，连续只探一次即为 stopped |
| 03:53:56Z | playing | playing | `editor_play` 后确认进入 Play Mode（本卡自身操作） |
| 04:12:18Z | 无法读取 | 未知 | pipeline 超时；Editor 主线程自 03:58Z 起无响应 |

卡面要求「开工前 editor_status 若不是 stopped，等 60 秒再看，连续三次就停下报 BLOCKED」。开工时是 stopped，因此不适用；结尾这次无法读取的原因与 Play Mode 无关，见「阻塞根因」。

## 3. 阻塞根因（为什么 C2/C3 没跑）

1. 03:53:48Z 我按卡面用 `editor_play` 进入 Play Mode，03:53:56Z 确认 `playMode=playing`。
2. 之后第一条要跑的命令 `set_autotick` 就 60 s 超时。Play Mode 下没有 autotick，Editor 不会自己推进帧，这是上一轮记录过的症状（`save-gate.md` C2 也记过「首次超时，加 --timeout 60 后成功」）。
3. 03:55:58Z 共享的 `%LOCALAPPDATA%Low\DefaultCompany\My project\TestResults.xml` 被改写（1589549 B），`Logs/Editor.log` 里出现 `CallbacksDelegator:RunFailed` 与 `RestoreSceneSetupTask` 失败堆栈：**另一个并行会话在这次 Play Mode 期间提交了一次同步模式的 `run_tests`**，测试框架在 Play Mode 里做场景还原失败。
4. `Logs/Editor.log` 末尾的卡死栈是：
   ```
   Unity.Pipeline.Editor.Testing.PipelineTestRunner/<>c:<ExecuteSyncMode>b__6_0 ()
   System.Threading.CancellationTokenSource:NotifyCancellation / TimerCallbackLogic
   (Filename: ./Library/PackageCache/com.unity.pipeline@…/Editor/Testing/PipelineTestRunner.cs Line: 646)
   ```
   即「同步模式测试运行被客户端超时取消」的收尾路径。`docs/console-基线归因表.md` 第 4.2 节正是把这一类列为必须剔除的工具噪声，并建议统一用 `--detach`；这次不是本卡发的命令（本卡的测试用 `--detach`）。
5. 04:06Z 之后 `Logs/Editor.log` 只增长过一次（4500059 B / 18:06:34），`editor_status`、`editor_focus`、`editor_stop` 全部超时；`computer_use` 对 Unity 窗口的 UIA 抓取也报 `UIA provider unresponsive on hwnd 0x106a2a40, class 'UnityContainerWndClass'` → 主线程冻结，不是单纯 pipeline 通道问题。
6. 按卡面「不要抢、不要 editor_stop」与三振规则，我没有继续强行操作，也没有杀进程 / 重启 Unity（那是用户的 Editor，且另一个并行会话正在用它）。

**后果（必须让操作者知道）**：Editor 目前停在 `playMode=playing`（我进入的那次），无法通过 pipeline 退出。真实存档与本工程文件均未被写坏（C4 已验）。

## 4. C1–C5 逐条实测

| 检查 | 结论 | 实测值 |
|---|---|---|
| C1 全量 EditMode | **PASS** | Total **2141** / Passed **2141** / Failed **0**；`test-run ... result="Passed" total="2141" passed="2141" failed="0" skipped="0"`；副本 `Logs/verify/20261118-m00/TestResults.xml`（1676355 B，md5 `ad57fd438391a25504b98571709e63da`，复制于 03:46:24Z，早于共享文件被并行会话覆盖） |
| C2 SAVE-3 | **BLOCKED** | 未执行。Editor 卡死前只到 `editor_play`，隔离目录 `ISO-M00-Pending-20261118` 仍为空（`ls` 只有 `.` 与 `..`），不存在任何存档记录 |
| C3 SAVE-4 | **BLOCKED** | 未执行。`ISO-M00-Empty-20261118` 全程为空（只有 `.` 与 `..`）。`SaveSlotStore.Load()` 的缺失文案是 `尚未找到存档。`（`Assets/Scripts/Persistence/SaveSlotStore.cs:150`），但本轮**没有**实测到它对 UI 的作用 |
| C4 真实槽指纹前后一致 | **PASS** | 前后四个 sha256 与四个 mtime 逐字相同，见下节 |
| C5 Console 新增 error | **部分（仅覆盖到 Play Mode 之前）** | 开工基线 `counts.error=13`（cursor 245）；全量 EditMode 之后 `counts.error=45`（cursor 413）。增量 32 条分类见下节：28 条工具噪声 + 4 条负向测试预期输出 = **0 条真实新增**。Play Mode 之后的增量无法采样（Editor 卡死，`console_status` 超时） |

### C5 增量 32 条的逐类归因

数据：`Logs/verify/20261118-m00/console-since245-errors.json`（`console --since 245 --level error`，seq 291–412）＋ `classify_errors.py`。

| 条数 | 首行 | 定性 | 依据 |
|---|---|---|---|
| 14 | `Pipeline: unhandled error serving POST /api/exec: System.InvalidOperationException: Cannot be changed after headers are sent.` | 工具噪声（第 2 类：run_tests 客户端超时传输异常） | `docs/console-基线归因表.md` 4.2 |
| 13 | `Failed to handle /api/exec request: Unable to write data to the transport connection: An established connection was aborted by the software …` | 同上（本卡超时命令自己造成的传输层异常） | 同上；13 条与本轮 13 次超时的 pipeline 调用一一对应 |
| 1 | `Failed to handle /api/exec request: … An existing connection was forcibly closed by the remote …` | 同上 | 同上 |
| 2 | `家族编年史记录写入失败（家族收养）：编年史预备记录序号无效。` | 负向测试的预期输出 | `docs/console-基线归因表.md` 第 5 节（`FamilyFormationSessionTests.cs:271/316` 的 `LogAssert.Expect`） |
| 2 | `家族编年史记录写入失败（家族婚姻）：编年史预备记录序号无效。` | 同上 | 同上 |

剔除两类工具噪声（28）与两类负向测试预期输出（4）后，**真实新增 error = 0**。

两个来源的差：本节的条数用的是 **`counts`**（`error` 13 → 45，差 32，与 entries 数一致）。`groundTruth.consoleErrors` 同期为 0 → 5，与 `counts` 差 1 的关系仍成立（pipeline 自己写入的 seed 条目）。跨时点比较全部只用 `counts`。

## 5. C4 真实槽指纹（前 / 后）

目录 `C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\SengokuChronicle\`

```
                     开工 03:41Z（前）                                 结束 04:12Z（后）
save-slot-1.json      16435 B  2026-11-07 13:54:24.800706 -1000      16435 B  2026-11-07 13:54:24.800706 -1000
   sha256            1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c   （相同）
save-slot-1.json.bak   8097 B  2026-11-05 10:22:12.392648 -1000        8097 B  2026-11-05 10:22:12.392648 -1000
   sha256            f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe   （相同）
```

开工时与卡面给定基线（16435 B / `1e61ed9cab4a5f3e…`、8097 B）一致。

## 6. 本轮自己建的两个隔离目录（只建，未写入）

```
C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\ISO-M00-Pending-20261118\   空
C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\ISO-M00-Empty-20261118\     空
```

上一轮并行会话留下的 `ISO-PlayTest-20261118`、`ISO-SaveGate-20261118`、`ISO-Empty-20261118` 只做了一次 `ls`，未改名、未写入、未删除。`scripts/verify/IsolateSaveStore.cs` 的 `Set()` 固定指向 `ISO-PlayTest-20261118`，那里已有上一轮的 13146 B 存档，`Set()` 会按自身逻辑返回 `BLOCKED: isolated target already holds slot data`；为了不改动别卡已入档的证据目录，本轮原本计划改用参数化的 `Logs/verify/gate-20261118/SaveGateProbe.cs`（同样的两个注入点：`SaveLoadPanel.SetStore` + 场景内全部 `TitleScreenController.SetStore`，回读 `isDefault`），这也是 C2/C3 没能执行的部分原因之一——**这一点没能实测验证**。

## 7. 与并行会话已入档证据的关系（只作交叉引用，不是本卡实测）

本卡开工时（03:41Z）并不知道另一会话已在 17:28–17:39 本地时间跑过同一件事。事后核对：

- `Logs/verify/gate-20261118/save-gate.md`（17:40 写）记 SAVE-3 / SAVE-4 均 **PASS**：SC-010 标准样例里选「明智 時姫」→存档→改选「明智 信正」→读档，读回 `pendingSuccessorId=person.akechi.tokihime`、`phase=AwaitingRegentChoice`；空槽位读档反馈 `尚未找到存档。`、`lastOk=False`、日期与人数不变、空目录仍为空。
- `checkpoint.md` 的 `55fb764`（17:42）已把 SAVE-1～SAVE-4 记为全过。

本卡**没有**复验这份证据；它的可采信度由复审者判断。若操作者接受用这份证据闭合 SAVE-3/SAVE-4，本卡可以直接改判；若要独立复验，需要先重启 Editor（当前主线程卡死）。

## 8. 未决问题

1. **Editor 88424 主线程卡死**（`PipelineTestRunner.ExecuteSyncMode` 的取消路径），停在 `playMode=playing`。需要人工重启 Unity 才能继续任何 pipeline 工作；本卡按规则不杀进程、不抢。
2. 共享 `TestResults.xml` 已被并行会话覆盖多次（17:55:58 1589549 B、18:06:52 41615 B）。C1 的数字只以本卡副本 `Logs/verify/20261118-m00/TestResults.xml` 为准。
3. 并行会话在 Play Mode 期间发同步 `run_tests`，是这次卡死的直接触发条件。建议在 `AGENTS.md` 或卡面里加一条：**进入 Play Mode 前必须确认没有别的会话在跑测试；跑测试只用 `--detach`**。
4. 卡面端口（7800）与 `unity pipeline list` 实际值（7801）不一致，建议以后以 `pipeline list` 为准写卡。

末行：**BLOCKED** — C1 PASS、C4 PASS、C5 覆盖到 Play Mode 之前为 0 真实新增；C2/C3 因 Editor 主线程被并行会话的同步测试运行卡死、pipeline 与 `editor_stop` 全部超时而未执行。
