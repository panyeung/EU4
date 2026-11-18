# M00 存读档隔离验证（第二轮）— SAVE-3 / SAVE-4

VERDICT: PASS
- SAVE-3 待处理继承决定跨存读档存活：**PASS**。存档时选中「明智 時姫」，改选「明智 信正」，读档后回到「明智 時姫」，phase 仍为 `AwaitingRegentChoice`，家主仍是同一人。
- SAVE-4 空槽位读取干净拒绝：**PASS**。反馈 `尚未找到存档。`，`lastOk=False`，日期 / 家主 / 待处理决定的三个 probe 值与读档前完全一致，空目录未被写入任何文件。

- 日期：2026-11-17 17:28–17:39（本地）
- 工程：`C:\Users\panye\My project`，Unity 6000.6.6f1
- Editor：PID 88424，Pipeline 端口 7800；所有命令都带 `--project-path "C:/Users/panye/My project"`
- 另一 Editor（PID 111588 / 端口 7801 / 工作树 `ui-m04-family`）**全程未被访问**
- 未运行 EditMode / PlayMode 测试集（未写 `TestResults.xml`），未保存场景，未在 `Assets/` 下新增文件，未做任何 git 写操作

## 分项表

| 检查 | 项 | 状态 | 关键数值 |
|---|---|---|---|
| C0 | 基线 | PASS | json `1e61ed9c…b92c` 16435 B / bak `f1c0630e…4cbe` 8097 B；`ISO-SaveGate-20261118` 与 `ISO-Empty-20261118` 均不存在，已新建并保持空 |
| C1 | doctor | PASS | status=ready compiling=false playMode=stopped；compilationFailed=false；cursor=**232** session=`bda41b5ea9764200811a3f34d5c7c33c`；场景 `FoundationScene` isDirty=false |
| C2 | 进 Play Mode | PASS | `Entered play mode` → 6/6 轮询 `playMode=playing`；autotick 启用（首次超时，加 `--timeout 60` 后成功） |
| C3 | 指向隔离槽 | PASS | panelStore 末段 `ISO-SaveGate-20261118`，`isDefault=False`，`fileExists=False`，`titleControllersRedirected=1` |
| C4 | SAVE-3 | PASS | 待处理决定 + 候选人选中态跨存读档一致（详见下节对比表） |
| C5 | SAVE-4 | PASS | 反馈 `尚未找到存档。`；date/sessionDate/pendingSuccessorId 三项与基线相等；空目录仍为空 |
| C6 | Console | PASS | `--since 232 --level error` → `entries=[] returned=0`；`counts.error` 13→13 未增长 |
| C7 | 收尾 | PASS | playMode=stopped；场景 isDirty=false 未保存；`Assets/Logs` 与 `.meta` 不存在；真实存档哈希与 C0 相同；隔离目录保留 |

## C0 基线

```
sha256sum .../SengokuChronicle/save-slot-1.json .../save-slot-1.json.bak
  1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c  save-slot-1.json     (16435 B, mtime 2026-11-07 13:54:24.800)
  f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe  save-slot-1.json.bak (8097 B, mtime 2026-11-05 10:22:12.392)

mkdir -p .../ISO-SaveGate-20261118 .../ISO-Empty-20261118
  → 两者建好后 ls 均为空（只有 . 与 ..）
```

`ISO-PlayTest-20261118/`（上一轮）按只读参考处理：仅 `ls` 一次，未写入。

## C1 doctor

```
unity command editor_status --project-path "C:/Users/panye/My project"
  {"status":"ready","compiling":false,"domainReloadInProgress":false,"playMode":"stopped","unityVersion":"6000.6.6f1"}

unity command console_status …
  {"cursor":232,"session":"bda41b5ea9764200811a3f34d5c7c33c",
   "counts":{"error":13,"warn":158,"log":61},
   "groundTruth":{"compilationFailed":false,"compiling":false,"consoleErrors":0,...}}

unity command list_open_scenes …
  {"count":1,"scenes":[{"name":"FoundationScene","path":"Assets/Scenes/FoundationScene.unity",
   "isLoaded":true,"isDirty":false,"isActive":true}]}
```

开工时 Editor 处于上一轮遗留的 `playMode=playing`，先 `editor_stop` 归零到 `stopped` 再取上述基线。
C6 用 `232` / `bda41b5e…c33c` 作采样基准。

## C2 进 Play Mode

```
unity command editor_focus …   → Editor focused via Toolbar
unity command editor_play  …   → Entered play mode
editor_status ×6（间隔 2s）    → "playMode":"playing" （6/6）
unity command set_autotick --enable true --timeout 60
  → Auto-tick already enabled (interval updated to 16ms)
  （首次不带 --timeout 时 30s 超时失败，重试成功）
```

界面路径：`开始新游戏` → 剧本选择。

## C3 指向隔离槽

```
script SaveGateProbe.cs SaveGateProbe.SetStore '["C:/…/ISO-SaveGate-20261118"]'
  panelStore=C:/Users/panye/AppData/LocalLow/DefaultCompany/My project/ISO-SaveGate-20261118
  isDefault=False ; filePath=…\save-slot-1.json ; fileExists=False ; backupExists=False
  titleControllersRedirected=1
```

脚本 `Logs/verify/gate-20261118/SaveGateProbe.cs`：`SaveLoadPanel.SetStore(new SaveSlotStore(目录))`，同时把场景里 1 个
`TitleScreenController` 的 store 改指同一目录（**否则「继续游戏」会读真实槽**），随后回读 `panel.Store.DirectoryPath`。
与上一轮 `IsolateSaveStore.cs` 的唯一区别是目录由参数传入，便于同一轮里再切到空目录。

## 剧本偏差（必须说明）

C2 先按卡片点击 `固定三代剧本（SC-013）`，随即被剧本门控挡住：

```
date=1560 年 4 月 1 日; headId=person.sc013.harunobu; phase=None
statusText=固定继承人：明智 正綱（46岁） / 第一代尚未处理「继承前的家议」：请先结束月份并完成承诺选择。
canRetire=False; retireInteractable=False(inactive); candidates=(none)
```

SC-013 的退休被「继承前的家议 / 雨夜来客」等事件链门控（`M2FixedScenario.CanBeginRetirement`），且其路线是**固定继承人直接继位**，
不产生「候选 A / 候选 B」的选择界面——SAVE-3 要验的正是候选选择。因此在同一场景、同一 `SaveLoadPanel` / `SuccessionPanel` /
`SuccessionService` 代码路径下改开 `SC-010 标准样例`：

```
click Canvas/M6TitleRoot/M6ScenarioSelectRoot/SC013StandardSampleButton → clicked 'SC-010 标准样例'
SuccessionProbe → familyId=family.akechi; headId=person.akechi.harunobu; headName=明智 晴信（32岁）
                  phase=None; canRetire=True; retireBlockReason=当前可按通常继承规则安排交接。
```

## C4 SAVE-3 待处理继承决定

```
click Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavFamilyButton → clicked '家族与继承'
click Canvas/SuccessionRoot/SuccessionHud/SuccessionRetireButton     → clicked '退休并安排继承'
  → phase=AwaitingSuccessionChoice
    statusText=继承待定：请从合法候选人中选择继承人。
    candidates=明智 信正（8岁，直系亲族，未成年） | 明智 時姫（14岁，直系亲族，未成年） | 明智 正綱（24岁，旁系亲族，成年）

click …/SuccessionCandidate_person.akechi.tokihime   ← 选候选 A（時姫），不点确认
  → phase=AwaitingRegentChoice; pendingSuccessorId=person.akechi.tokihime; pendingRegentId=(空)
    candidates=明智 信正（8岁） | 明智 時姫（14岁） [已选] | 明智 正綱（24岁）
    confirmInteractable=False   ← 未确认

click Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavMapButton     → clicked '地图与出行'
click Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5SaveButton             → clicked '存档'
  M5SaveStatusText → 已保存当前进度。
  …/ISO-SaveGate-20261118/save-slot-1.json 生成，15759 B，sha256 2f95e9fa…7042
  存档内：/succession/phase = 2 ; /succession/pendingSuccessorId = person.akechi.tokihime

click …/ReferenceNavFamilyButton → clicked '家族与继承'
click …/SuccessionCandidate_person.akechi.nobumasa  ← 改选候选 B（信正）
  → phase=AwaitingRegentChoice; pendingSuccessorId=person.akechi.nobumasa
    candidates=明智 信正（8岁） [已选] | 明智 時姫（14岁） | 明智 正綱（24岁）

click …/ReferenceNavMapButton → clicked '地图与出行'
click Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5LoadButton → clicked '读档'
  M5SaveStatusText → 已载入存档。
click …/ReferenceNavFamilyButton → clicked '家族与继承'
  → phase=AwaitingRegentChoice; pendingSuccessorId=person.akechi.tokihime
    candidates=明智 信正（8岁） | 明智 時姫（14岁） [已选] | 明智 正綱（24岁）
    headName=明智 晴信（32岁）; statusText=继承人未成年：请选择一名合资格成年摄政。
```

probe 前后对比：

| probe 项 | 保存时（选 A 后） | 改选 B 后 | 读档后 | 与保存时一致 |
|---|---|---|---|---|
| `phase` | AwaitingRegentChoice | AwaitingRegentChoice | **AwaitingRegentChoice** | 是 |
| `pendingSuccessorId` | person.akechi.tokihime | person.akechi.nobumasa | **person.akechi.tokihime** | 是 |
| `pendingRegentId` | （空） | （空） | （空） | 是 |
| 候选人 `[已选]` 标记 | 時姫 | 信正 | **時姫** | 是 |
| `headId` / `headName` | akechi.harunobu / 明智 晴信（32岁） | 同 | **同** | 是 |
| `statusText`（待处理提示） | 继承人未成年：请选择一名合资格成年摄政。 | 同 | **同** | 是 |

EXPECT 满足：读回的是**第一次选中的候选人 A 与同一次待处理决定**，不是 B，也不是「无待处理」。
读操作未改写存档：

```
读档前 sha256 2f95e9fa8c48b1dd89055b959a0d420699657ac05b0b7173b36b82a36eda7042  15759 B  mtime 17:36:13.619
读档后 sha256 2f95e9fa8c48b1dd89055b959a0d420699657ac05b0b7173b36b82a36eda7042  15759 B  mtime 17:36:13.619
目录内仍只有一个 save-slot-1.json（无 .bak / .tmp）
```

## C5 SAVE-4 空槽位读取

```
script SaveGateProbe.SetStore '["C:/…/ISO-Empty-20261118"]'
  panelStore=…/ISO-Empty-20261118 ; isDefault=False ; fileExists=False ; backupExists=False
基线 probe（C5-before）
  date=1560 年 4 月 1 日 ; sessionDate=1560-04-01 ; funds=30 ; supplies=12 ; people=6
  phase=AwaitingRegentChoice ; headId=person.akechi.harunobu ; pendingSuccessorId=person.akechi.tokihime

click Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5LoadButton → clicked '读档'
  M5SaveStatusText → 尚未找到存档。
读后 probe（C5-after）
  date=1560 年 4 月 1 日 ; sessionDate=1560-04-01 ; funds=30 ; supplies=12 ; people=6
  phase=AwaitingRegentChoice ; headId=person.akechi.harunobu ; pendingSuccessorId=person.akechi.tokihime
  panelStatus=尚未找到存档。 ; lastOk=False ; slotExists=False
ls …/ISO-Empty-20261118 → 仍为空（读档没有生成任何文件）
```

| probe 项 | C5 基线 | 空槽读档后 | 结论 |
|---|---|---|---|
| 反馈文本 | —（沿用上一次操作的旧文本） | **尚未找到存档。** | 明确说明「没有存档」 |
| `lastOk` | True（旧值） | **False** | 拒绝而非成功 |
| `date` / `sessionDate` | 1560 年 4 月 1 日 / 1560-04-01 | 同 | 未被重置 |
| `phase` / `pendingSuccessorId` | AwaitingRegentChoice / tokihime | 同 | 待处理决定未被清空 |
| `headId` | person.akechi.harunobu | 同 | 家主未变 |
| `ISO-Empty-20261118` 内容 | 空 | 空 | 无写入 |

消息来自 `SaveSlotStore.Load()` → `LoadPath(FilePath, SaveErrorCode.NotFound, "尚未找到存档。")`，
`SaveLoadPanel.Load()` 在 `!result.Succeeded` 时只 `SetStatus(false, result.Message)` 就返回，不改战役状态。
**注**：文案用的是「尚未找到存档」而非字面「无存档」，属于卡片要求的「无存档一类明确提示」。
另如实记录一处小瑕疵：`SetStore` 之后 panel 不会清空上一次操作的状态文本，所以 C5 基线里 `panelStatus` 仍是
`已载入存档。`/`lastOk=True`（历史残留）。它只是显示层残留，不影响判定，因为点读档后被 `尚未找到存档。` 覆盖。

## C6 Console

```
unity command console --since 232 --since_session bda41b5ea9764200811a3f34d5c7c33c --level error
  {"entries":[],"returned":0,"dropped":false,"reset":false,
   "counts":{"error":13,"warn":160,"log":71},
   "groundTruth":{"compilationFailed":false,"compiling":false,"consoleErrors":0,...}}
```

新增 error = 0，无条目需抄录；`counts.error` 与 C1 的 13 相同，未增长。SAVE-3 / SAVE-4 均未产生异常堆栈。

## C7 收尾

```
unity command editor_stop …  → Exited play mode
editor_status               → "playMode":"stopped"
list_open_scenes            → FoundationScene isLoaded=true isDirty=false
ls Assets/Logs Assets/Logs.meta → 均 No such file or directory（本轮未创建，无需删除）
```

隔离目录保留：

```
ISO-SaveGate-20261118/  save-slot-1.json  15759 B  17:36
ISO-Empty-20261118/     （空）
ISO-PlayTest-20261118/  save-slot-1.json 13146 B / save-slot-1.json.bak 13146 B（上一轮，未动）
```

## 工作树异常（外部改动，须请主控确认）

本轮在 17:28 记了 `git status --short` 基线：**68 项 = 35 个暂存 `M ` + 31 个暂存 `A ` + 2 个 `??`**
（与卡片写的「34 修改 / 32 未跟踪」已有 1 项差异）。收工时是 **71 项 = 37 个未暂存 ` M` + 34 个 `??`**。
逐项比对结果：35 个文件状态码由 `M ` 变 ` M`、29 个由 `A ` 变 `??`（**索引被重置**），另有 5 个新路径出现
（`M AGENTS.md`、`M checkpoint.md`、`?? docs/console-背景图构图.md`、`?? docs/research/`、`?? scripts/verify/`）。

同时观察到：

```
17:31:52.34–17:31:52.38  Assets/ 下 40+ 文件（含 Assets/Scenes/FoundationScene.unity、
                         GameSession.cs、SuccessionState.cs、TitleScreenController.cs …）
                         被同一秒批量改写（mtime 递增 1–2 ms，典型复制/git checkout 特征）
17:31:53.16              .git/index mtime
```

我未执行任何 git 写命令（只有 `git status` / `git diff` / `git config` 读取）、未调用任何保存场景的入口。旁证：

- `Logs/Editor.log` 无任何 `Saving scene` / `Saved scene` 记录；
- `list_open_scenes` 开工与收工均 `isDirty=false`，场景从未进入待保存状态；
- `SengokuChronicle.Application/Domain/Persistence/Events.dll` 仍是 11-14 的产物未被重编，`SengokuChronicle.Presentation.dll`
  与 `Editor.dll` 在 17:32:04 重编（早于 C4/C5 probe）。说明 **磁盘源码内容未变、只是 mtime 被重写**，
  本轮 probe 与当前磁盘代码一致，不存在「跑到旧代码」的问题。

结论：这是**另一个进程/agent 在同一工作树里做的 git 与文件操作**（索引重置 + 场景等 40+ 文件重写 + 两份文档更新），
不是本轮验证行为。本轮在 `Assets/` 下没有新增或修改任何文件，报告与临时脚本只写在被 `.gitignore` 忽略的 `Logs/verify/gate-20261118/`。
请主控确认那批外部改动的归属，再决定是否把本轮证据并入签核。

## 真实存档哈希对比

| 文件 | C0 开工前 SHA-256 | C7 收工后 SHA-256 | 一致 | 大小 / mtime（收工后） |
|---|---|---|---|---|
| `SengokuChronicle/save-slot-1.json` | `1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c` | `1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c` | 是 | 16435 B / 2026-11-07 13:54:24.800 |
| `SengokuChronicle/save-slot-1.json.bak` | `f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe` | `f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe` | 是 | 8097 B / 2026-11-05 10:22:12.392 |

运行期旁证：C4 保存前 `SaveGateProbe.RealSaveFingerprint` 回读
`save-slot-1.json=len16435@2026-11-07T23:54:24.800Z; save-slot-1.json.bak=len8097@2026-11-05T20:22:12.392Z`，
与 C0 的 len/mtime 相同；真实槽全程处于读写范围之外。
