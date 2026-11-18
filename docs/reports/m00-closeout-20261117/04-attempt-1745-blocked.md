# M00 收口 — SAVE-3 / SAVE-4 存读档补验与 C1/C2 复跑（20261117-1745）

状态：**已完成（判定 BLOCKED）**。C1/C2/C5 通过；C3/C4 与 C6 后值因 Editor pipeline 通道无响应而未执行，详见第 6 节。

## 0. 环境与分支

| 项 | 实测值 |
|---|---|
| 工作树 | `C:\Users\panye\My project` |
| 分支 | `main` |
| HEAD | `55fb764 docs: M00 存读档 SAVE-1～SAVE-4 全过并入表`（任务卡写的是 `9e78821`，实测 HEAD 已前移一格，未做任何 git 写操作） |
| `git status --porcelain \| wc -l` | 开工前 `72` → 收尾 `69`（差异来自其他并发写者的工作区变化；本轮未 add/commit/stash/reset/checkout/restore/clean） |
| Editor | `unity pipeline list` → `My project` / PID 88424 / 端口 7801 / 可达 true |
| `editor_status`（开工） | `status=ready`、`playMode=stopped`、`compiling=false`、`domainReloadInProgress=false`、`unityVersion=6000.6.6f1` |

## 1. 按顺序执行的命令

```bash
# 环境
unity pipeline list
unity command editor_status  --project-path "C:/Users/panye/My project" --timeout 120 --result-only
unity command console_status --project-path "C:/Users/panye/My project" --timeout 120 --result-only

# C1 后半段
unity command run_script --file scripts/verify/RunValidator.cs --entry RunValidator.Run --project-path "C:/Users/panye/My project" --timeout 120
tail -1 Logs/SceneValidationReport.txt

# C2 全量 EditMode
unity command run_tests --mode EditMode --timeout 900 --detach --project-path "C:/Users/panye/My project"
unity command test_status --project-path "C:/Users/panye/My project" --timeout 60 --result-only   # 每 20 s 轮询
bash scripts/copy-testresults.sh Logs/verify/20261117-1745
```

## 2. C1 后半段 — `run_script` 驱动场景校验器

`run_script` 返回：

```json
{"diagnostics":[],"compileMs":3312,"executeMs":1850,
 "assemblyName":"PipelineRunScript_RunValidator_420a66a80c06414dbb570a212fe5f711",
 "success":true,"result":"validator invoked"}
```

`Logs/SceneValidationReport.txt`（本轮 17:47 重写，186403 B）末行：

```
RESULT: PASS; Errors: 0
```

`scripts/verify/RunValidator.cs` sha256 = `0b5d036f95f326fc6f19c0a68da73aa6d2cd1d2cc127bfdc8ad849e827c367ae`，与上一轮记录的逐字节同一份（前半段脚本同一性由上一轮证过，本轮只补后半段）。

**C1：PASS**

## 3. C2 全量 EditMode

`run_tests --detach` 作业 ID `d65b81be37ca40fc9a6ac45665f8d871`，`test_status` 轮询至 `status=completed`。

`TestResults.xml` 根属性（副本：`Logs/verify/20261117-1745/TestResults.xml`，sha256 `f6bed5c1bd69050171db1f133fc3e938`）：

| Total | Passed | Failed | Skipped | Inconclusive | Result | 起止 |
|---|---|---|---|---|---|---|
| 2141 | 2141 | 0 | 0 | 0 | Passed | 03:48:00Z → 03:52:28Z（267.9 s） |

**C2：PASS**（Failed=0 且 Passed=2141 ≥ 2141 门槛）

结果文件副本：`Logs/verify/20261117-1745/TestResults.xml`、`Logs/verify/20261117-1745/SceneValidationReport.txt`

## 4. C5 真实槽指纹（前）

```bash
cd "$HOME/AppData/LocalLow/DefaultCompany/My project/SengokuChronicle"
sha256sum save-slot-1.json save-slot-1.json.bak
stat -c '%n %s %y' save-slot-1.json save-slot-1.json.bak
```

```
1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c *save-slot-1.json
f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe *save-slot-1.json.bak
save-slot-1.json     16435  2026-11-07 13:54:24.800706000 -1000
save-slot-1.json.bak  8097   2026-11-05 10:22:12.392648700 -1000
```

与卡面基线 `1e61ed9c…c92c`（16435 B）/ `f1c0630e…4cbe`（8097 B）一致。

后（18:20:58，收尾）：

```
1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c *save-slot-1.json
f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe *save-slot-1.json.bak
save-slot-1.json     16435  2026-11-07 13:54:24.800706000 -1000
save-slot-1.json.bak  8097   2026-11-05 10:22:12.392648700 -1000
```

sha256、字节数、mtime 三组前后全等：真实槽未被本轮触碰。隔离目录 `%LOCALAPPDATA%/LocalLow/DefaultCompany/My project/ISO-1745-SaveGate` 收尾时**不存在**（全程未创建），未删除任何隔离槽以外的文件。**C5：PASS**

## 5. C6 Console（开工前）

`console_status`（17:47:11 本地 / 03:47:11Z，`session=bda41b5ea9764200811a3f34d5c7c33c`）：

- `cursor` = 329，`returned` = 0，`reset` = false
- `counts` = `{error: 31, warn: 213, log: 85}`（本会话累计计数）
- `groundTruth`（`ageMs`=15, `sampledUtc`=03:47:11.808Z）= `{compilationFailed: false, compiling: false, consoleErrors: 4, consoleWarnings: 53, consoleLogs: 14}`

两组数含义不同：`counts` 是 pipeline 会话的累计计数（含历史轮次），`groundTruth` 是对当前 Console 窗口内容采样后的实际条目数。

后值：**取不到** —— 自 17:53 起 `console_status` 全部超时（见 6.2），无法给出收尾计数，**C6：BLOCKED**。

旁证（他人并发写入本证据目录的 `console-before.txt`，17:53:52Z，同一 session `bda41b5e…`）：`cursor=414`、`counts={error:45, warn:265, log:104}`、`groundTruth={consoleErrors:0}`。即在本轮 C2 结束之后、我尚未进入 Play Mode 之前，累计 error 已由 31 涨到 45（来自并发的其他写者，不属本轮）；也说明本机实际并非「只有一个写者」。

## 6. C3 SAVE-3 / C4 SAVE-4：**BLOCKED**

### 6.1 已完成的准备

- 面板层探针已写好：`scripts/verify/SaveGatePlayProbe.cs`（sha256 `076c501cb2b8c5629383da7746eb15db0741be418ef74d6c11a991ff70031035`）。它只做面板层动作：`SaveLoadPanel.SetStore` + `TitleScreenController.SetStore` 双重重定向到隔离目录 `ISO-1745-SaveGate`、按 GameObject 名点真实 `Button.onClick`（`M5SaveButton` / `M5LoadButton` / `SuccessionRetireButton` / `SuccessionConfirmButton` / `SuccessionCandidate_<personId>`）、回读 `sessionDate`、`livingMembers`、`panel.Status`、`lastOk`、`slotExists`、`phase`、`headId`、`pendingSuccessor`、`pendingRegent`、候选人稳定 ID 列表与真实槽指纹。
- 已确认的代码事实（静态读，非运行）：`SaveData.cs:2051/2116/2139` 持久化了 `pendingSuccessorId`，`SuccessionState.PendingSuccessorId` 在存读档两侧都有读写路径；空槽读档的文案来源是 `SaveSlotStore.cs:150` 的 `尚未找到存档。`，经 `SaveLoadPanel.Load()` 原样写入 `M5SaveStatusText`。

### 6.2 阻塞现场（全部为实测）

| 时刻（本地） | 命令 | 结果 |
|---|---|---|
| 17:47:11 | `console_status` | 成功（见第 5 节前值） |
| 17:52:10 | `test_status` | 成功（completed） |
| 17:53–17:54 | `console_status` ×1 | `Pipeline command 'console_status' timed out after 120000ms` |
| 17:55–18:06 | `editor_status` ×8 | 全部 `timed out after 30000–120000ms` |
| 18:07:47–18:19:36 | `editor_status` ×15 | 全部超时（30 s 窗口，间隔 12–20 s） |
| 18:21 | `editor_stop` | `Pipeline command 'editor_stop' timed out after 35000ms` |
| 18:21 | `editor_play` | 超时（20 s 窗口） |

因此 `editor_play` 从未成功、Play Mode 从未进入，**SAVE-3 / SAVE-4 的面板层与会话层都没有跑**，两项一律记 BLOCKED，不折算成 PASS。

### 6.3 现场诊断（只陈述事实）

| 探测 | 实测 |
|---|---|
| `unity pipeline list` | `My project` / PID 88424 / 端口 7801 / 服务器可达 **true** |
| `curl http://127.0.0.1:7801/` | `401`，耗时 2.1 ms（HTTP 层活着） |
| `Get-Process -Id 88424` | `Responding=True`；CPU 两次采样 541 → 541.03（5 s 内几乎不涨，主线程空闲） |
| 窗口标题 | `My project - FoundationScene ... (6000.6.6f1)*`（场景未保存的脏状态，全程未保存） |
| 并发事实 | 18:04–18:06 之间本机新起 5 个 Unity 进程（PID 44680 / 120720 / 124140 / 130352 / 140992，属 `C:/Users/panye/orca/workspaces/My project/*` 下其他工作树）；同一证据目录 `Logs/verify/20261117-1745/` 在 17:53 被其他写者写入 `SceneContextProbe.cs`、`SceneContextProbe2.cs`、`console-before.txt`、`real-slot-before.txt`，并把我 17:52 复制进去的 `SceneValidationReport.txt` 覆盖成 46 KB 版本（已另存 `SceneValidationReport-1745.txt` 保住本轮原件） |

结论只到这里：pipeline 的 HTTP 端点正常、Editor 主线程空闲且消息泵正常，但所有 `unity command` 的 exec 请求都不被执行（除 17:47–17:52 这段成功窗口外全部挂到超时）。未定位到根因，也未采取任何破坏性手段（未杀进程、未重启 Editor、未保存场景、未切分支）。

## 7. 逐条结论

| 检查 | 判定 | 实测要点 |
|---|---|---|
| C1 后半段（run_script + 报告末行） | **PASS** | `result="validator invoked"`；末行 `RESULT: PASS; Errors: 0`（186403 B 报告） |
| C2 全量 EditMode | **PASS** | Total 2141 / Passed 2141 / Failed 0 / Skipped 0 |
| C3 SAVE-3 待处理继承决定跨存读档 | **BLOCKED** | `editor_play` 至 18:21 全部 pipeline 超时，Play Mode 未进入，面板层未测 |
| C4 SAVE-4 空槽位干净拒绝 | **BLOCKED** | 同上；静态路径已确认文案 `尚未找到存档。` |
| C5 真实槽指纹前后一致 | **PASS** | sha256 / 字节数 / mtime 前后全等 |
| C6 Console 新增 error = 0 | **BLOCKED** | 前值 `counts.error=31` / `groundTruth.consoleErrors=4`；后值因 `console_status` 超时取不到 |

## 8. 文件与代码改动

- 新建（仅验证脚本，不进 `Assets/`，本轮未执行）：`scripts/verify/SaveGatePlayProbe.cs`
- 只读使用：`scripts/verify/RunValidator.cs`、`scripts/verify/IsolateSaveStore.cs`
- 新建证据：`Logs/verify/20261117-1745-save-load-pending.md`、`Logs/verify/20261117-1745/{TestResults.xml, SceneValidationReport-1745.txt}`
- **生产代码零改动**：`Assets/` 下未新增/修改/删除任何文件；未改场景、prefab、ProjectSettings；未提交 git。
- 截图：未拍摄，无需清理 `Assets/Logs`。

判定: **BLOCKED** — C1/C2/C5 实测通过（2141/2141 全绿、校验器 0 error、真实槽三值前后全等），但 C3/SAVE-3、C4/SAVE-4 与 C6 后值因 Editor 88424 的 pipeline exec 通道自 17:53 起持续无响应（HTTP 端点 401 <3 ms、主线程空闲 Responding=True、pipeline list 仍报可达）而无法执行；存读档未测即不判 PASS，需在其 pipeline 恢复后用第 6.1 节已备好的探针重跑。
