# M00 存读档隔离验证 — SaveLoadPanel 指向 ISO-PlayTest-20261118

VERDICT: PASS
隔离槽在主工作树跑通「保存 → 推进一天 → 读取 → 三值回到保存时」，真实存档 `SengokuChronicle/save-slot-1.json`
与其 `.bak` 的 SHA-256 与 mtime 全程未变，Console 新增 error = 0。

- 日期：2026-11-17 16:36–16:44（本地）
- 工程：`C:\Users\panye\My project`，Unity 6000.6.6f1，URP 2D
- Editor：PID 88424，`-projectPath "C:\Users\panye\My project"`，Pipeline 0.8.0-exp.1，端口 7800
- 旁证：`unity pipeline list` 同时列出 PID 111588 / 端口 7801 的 `ui-m04-family`。
  **该实例与 `C:\Users\panye\orca\workspaces\My project\ui-m04-family` 全程未被访问**，所有命令都带 `--project-path`。
- 驱动：`unity command … --project-path "C:/Users/panye/My project"`；UI 用 `Logs/verify/gate-20261118/unity-run-iso.py`
  （`unity-run.py` 的带 `--project-path` 版本，避免双 Editor 路由不确定）
- 本轮未运行 EditMode / PlayMode 测试集，未改动 `TestResults.xml`，未做任何 git 写操作。

## 分项表

| 检查 | 项 | 状态 | 关键数值 |
|---|---|---|---|
| C0 | 基线哈希（真实存档） | PASS | json `1e61ed9c…b92c` 16435 B / bak `f1c0630e…4cbe` 8097 B；隔离目录不存在 |
| C1 | doctor | PASS | status=ready compiling=false playMode=stopped；compilationFailed=false；cursor=160 session=`bda41b5e…c33c`；场景 dirty=false |
| C2 | 进 Play Mode | PASS | `Entered play mode` → 轮询 6 次 `playMode=playing`；autotick 已启用 |
| C3 | 到可存档状态 | PASS | 日期 `1560 年 4 月 1 日`，资金 30，物资 12，人口 6；5 张截图 md5 互不相同 |
| C4 | 指向隔离槽 | PASS | panelStore 末段 = `ISO-PlayTest-20261118`，`isDefault=False`，TitleScreenController 一并改指 1 个 |
| C5 | 保存 | PASS | 面板 `已保存当前进度。` lastOk=True；隔离目录生成 `save-slot-1.json` 13146 B；`saveFormatVersion=17` |
| C6 | 改状态再读 | PASS | 推进一天 → `1560-04-02`；读档后 `1560-04-01` 且三值与 C3 完全相等；存档文件 sha256 与 mtime 未变 |
| C7 | Console 采样 | PASS | `--since 160 --level error` → `entries=[] returned=0`，新增 error 0（累计 error 仍 11，与 C1 相同） |
| C8 | 收尾 | PASS | playMode=stopped；场景 isDirty=false 未保存；`Assets/Logs` 与 `Assets/Logs.meta` 已删；真实存档哈希与 C0 相同；隔离目录保留 |

## C0 基线哈希

```
sha256sum .../SengokuChronicle/save-slot-1.json .../SengokuChronicle/save-slot-1.json.bak
  1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c  save-slot-1.json
  f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe  save-slot-1.json.bak
ls .../ISO-PlayTest-20261118/
  ls: cannot access '...': No such file or directory      ← 开工前隔离目录不存在，符合前提
```

## C1 doctor

```
unity command editor_status --project-path "C:/Users/panye/My project"
  {"status":"ready","compiling":false,"domainReloadInProgress":false,"playMode":"stopped",
   "projectPath":"C:\Users\panye\My project","unityVersion":"6000.6.6f1"}

unity command console_status …
  {"cursor":160,"session":"bda41b5ea9764200811a3f34d5c7c33c",
   "counts":{"error":11,"warn":106,"log":43},
   "groundTruth":{"compilationFailed":false,"compiling":false,"consoleErrors":0,...}}

unity command list_open_scenes …
  {"count":1,"scenes":[{"name":"FoundationScene","path":"Assets/Scenes/FoundationScene.unity",
   "isLoaded":true,"isDirty":false,"isActive":true,"rootCount":4}]}
```

C1 的 cursor / session 记为 `160` / `bda41b5ea9764200811a3f34d5c7c33c`，C7 用它做采样基准。
场景已打开且 `isDirty=false`，未调用 `open_scene`。

## C2 Play Mode

```
unity command editor_focus …   →  Editor focused via Toolbar
unity command editor_play  …   →  Entered play mode
editor_status ×6（间隔 2s）    →  "playMode":"playing"  （6/6）
unity command set_autotick --enable true …
  → Auto-tick already enabled (interval updated to 16ms)
VerifyUi.State → scene=Assets/Scenes/FoundationScene.unity loaded=True dirty=False / playing=True
```

## C3 到可存档状态

```
click Canvas/M6TitleRoot/M6MainMenuRoot/M6NewGameButton
  → clicked '开始新游戏'
buttons → 出现 M6ScenarioSelectRoot：固定三代剧本（SC-013）/ SC-010 / SC-020…SC-024 / 返回
click Canvas/M6TitleRoot/M6ScenarioSelectRoot/SC013FixedScenarioButton
  → clicked '固定三代剧本（SC-013）'
click Canvas/M02DailyActionRoot/M02NavRail/ReferenceNavMapButton → clicked '地图与出行'
```

C3 probe（HUD 三个值，读的是顶栏 `Text` 实例，不是目视截图）：

```
date=1560 年 4 月 1 日 ; funds=30 ; supplies=12 ; people=6 ; sessionDate=1560-04-01
```

截图 5 张，每张前 `editor_focus`，md5 各不相同，目视检查后立即删除：

| 文件 | md5 | 内容 |
|---|---|---|
| c3-hud-01.png | 25be907ac89584cafa91ab2826d44d45 | 标题界面（点击后未重绘的旧帧，不计入证据） |
| c3-hud-02.png | d1e9ffc846f8cdfa36ca90786ba79e9a | 人物档案 · 每日行动 HUD，顶栏 1560年4月1日 / 清洲 |
| c3-map-01.png | 3ea6e78c0997515f309674882a4e6743 | 世界地图，标题下并排「存档」「读档」两个按钮 |
| c6-before-load.png | 6d5dace56fe29071fd7690484386c8e7 | 推进一天后（1560-04-02） |
| c6-after-load.png | af2b85a880425e6c16f8836c4bddee11 | 读档后回到 1560年4月1日，地图上显示「已载入存档。」 |

如实说明：目视截图对顶栏小号数字有过误读（把资金/物资/情报读串）。**本报告所有数值一律取自
`VerifyUi.Text` / `IsolateSaveStore.Probe` 对 UI Text 实例的直接读取**，截图只作「界面处于该状态」的旁证。

## C4 指向隔离槽

入口：`Logs/verify/gate-20261118/IsolateSaveStore.cs`，`Set()` 用
`Resources.FindObjectsOfTypeAll<SaveLoadPanel>()` 取场景内实例，调 `SetStore(new SaveSlotStore(隔离目录))`，
并把场景里 1 个 `TitleScreenController` 的 store 一并改指（防止误触「继续游戏」读真实槽），随后回读 `panel.Store.DirectoryPath`。

```
script IsolateSaveStore.cs IsolateSaveStore.Set
panelStore=C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\ISO-PlayTest-20261118
isDefault=False
filePath=…\ISO-PlayTest-20261118\save-slot-1.json
titleControllersRedirected=1
sessionDate=1560-04-01
```

EXPECT 满足：回传目录是隔离目录，`isDefault=False`（不是 `...\SengokuChronicle`），因此继续点保存。

## C5 保存

保存前先取真实存档指纹，作为「之后没被动过」的运行时旁证：

```
script IsolateSaveStore.RealSaveFingerprint
realDir=…\SengokuChronicle;save-slot-1.json=len16435@2026-11-07T23:54:24.800Z;
save-slot-1.json.bak=len8097@2026-11-05T20:22:12.392Z
```

```
click Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5SaveButton  → clicked '存档'
```

保存反馈（面板状态文本 `M5SaveStatusText` 与 `panel.Status` 双向一致）：

```
probe Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5SaveStatusText → 已保存当前进度。
script IsolateSaveStore.Probe → panelStatus=已保存当前进度。;lastOk=True;slotExists=True
```

隔离目录：

```
ls -la …/ISO-PlayTest-20261118/
  -rw-r--r-- 13146  2026-11-17 16:42:02.951  save-slot-1.json
head -c 300 save-slot-1.json
  { "saveFormatVersion": 17, "gameVersion": "0.1.0", "rulesVersion": 8, …
```

`saveFormatVersion` = 17，符合预期。首次保存时隔离目录此前无槽，因此未生成 `.bak`（与 `SaveSlotStore` 的原子替换逻辑一致）。

## C6 改状态再读

```
sha256sum 隔离槽 → a61d77de4872724f412fc295b14a5cbec4d939fbb1460d0486bdb24d4f3762f1
stat           → 2026-11-17 16:42:02.951426900  len=13146

click Canvas/WorldClockPanel/AdvanceButton → clicked '推进一天'
script Probe → date=1560 年 4 月 2 日 ; sessionDate=1560-04-02
  （资金/物资/人口仍为 30/12/6 —— 单日推进不改这三项，差异体现在日期上）

click Canvas/M2WorldMapRoot/M5SaveLoadRoot/M5LoadButton → clicked '读档'
probe M5SaveStatusText → 已载入存档。
script Probe → date=1560 年 4 月 1 日 ; funds=30 ; supplies=12 ; people=6 ;
               sessionDate=1560-04-01 ; panelStatus=已载入存档。; lastOk=True
```

三值前后对比：

| 项 | C3 保存时 | C6 推进一天后 | C6 读档后 |
|---|---|---|---|
| 日期 | 1560 年 4 月 1 日 | 1560 年 4 月 2 日 | **1560 年 4 月 1 日** |
| 资金 | 30 | 30 | **30** |
| 物资 | 12 | 12 | **12** |
| 人口 | 6 | 6 | **6** |

EXPECT 满足：读取后三值与 C3 完全相等。读操作未改写存档文件：

```
sha256sum（读档后）→ a61d77de4872724f412fc295b14a5cbec4d939fbb1460d0486bdb24d4f3762f1   ← 与保存后相同
stat              → 2026-11-17 16:42:02.951426900  len=13146                            ← mtime 与大小不变
ls 隔离目录       → 仍只有一个 save-slot-1.json（未因读档产生 .bak / .tmp）
```

## C7 Console 采样

```
unity command console --since 160 --since_session bda41b5ea9764200811a3f34d5c7c33c \
    --level error --project-path "C:/Users/panye/My project"
  {"entries":[],"cursor":160,"session":"bda41b…","returned":0,"dropped":false,"reset":false,
   "counts":{"error":11,"warn":106,"log":43},
   "groundTruth":{"compilationFailed":false,"consoleErrors":0,"consoleWarnings":0,"consoleLogs":0}}
```

新增 error = 0，无条目需抄录。`counts.error=11` 是 C1 已存在的历史累计值，与 C1 完全一致，未增长。

## C8 收尾

```
unity command editor_stop …  → Exited play mode
editor_status                → "playMode":"stopped"

unity command list_open_scenes …
  {"name":"FoundationScene","isLoaded":true,"isDirty":false,"isActive":true,"rootCount":4}
stat Assets/Scenes/FoundationScene.unity
  2026-11-15 19:37:30.613625500   ← 早于本轮开工时间（2026-11-17 16:36），场景未被保存

rm -rf Assets/Logs Assets/Logs.meta
  → Assets 下已无 Logs 项；两者在 git 中本就未跟踪（`?? Assets/Logs.meta` / `?? Assets/Logs/`），删除不影响任何已跟踪文件
```

清理后 `git status` 与开工时一致：34 个既有 ` M` 条目 + 既有未跟踪文件，**没有本轮新增的 `Assets/` 文件**。
报告与临时脚本写在 `Logs/verify/gate-20261118/`（`.gitignore` 已忽略 `Logs/`）。

## 真实存档哈希对比

| 文件 | C0 开工前 SHA-256 | C8 收工后 SHA-256 | 一致 | mtime（收工后） |
|---|---|---|---|---|
| `SengokuChronicle/save-slot-1.json` | `1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c` | `1e61ed9cab4a5f3e5fbcce5eda4bd24586be5a072ea5b749ccb3c1e280bcb92c` | 是 | 2026-11-07 13:54:24.800，len=16435 |
| `SengokuChronicle/save-slot-1.json.bak` | `f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe` | `f1c0630e5dd91baa0b18607ca4367956db637a8bb4dd6da21d1e79212bda4cbe` | 是 | 2026-11-05 10:22:12.392，len=8097 |

运行时旁证：保存前 `RealSaveFingerprint` 记录的 len/mtime（16435@2026-11-07T23:54:24.800Z、8097@2026-11-05T20:22:12.392Z）
与文件系统上的值一致，说明整轮 Play Mode 期间真实目录既未被写入，也未被 `SaveSlotStore` 的恢复逻辑触碰。
`SengokuChronicle/` 目录下仍只有这两个文件，没有 `.tmp`、`LastRecoveryFilePath` 一类残留。

## 隔离目录内容（保留，未删除）

```
C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\ISO-PlayTest-20261118\
  save-slot-1.json   13146 B   2026-11-17 16:42:02.951
  sha256 a61d77de4872724f412fc295b14a5cbec4d939fbb1460d0486bdb24d4f3762f1
  saveFormatVersion 17 / gameVersion 0.1.0 / rulesVersion 8 / eventContentVersion 4
  存档内容日期 1560-04-01，locationId owari.kiyosu
```

目录里只有这一个文件，`*.tmp` 与 `.bak` 均不存在（原子写已完成收尾）。

## 附：本轮新增/使用的文件

- `Logs/verify/gate-20261118/save-isolated.md`（本报告）
- `Logs/verify/gate-20261118/IsolateSaveStore.cs`（run_script 入口：Set / Probe / AdvanceOneDay / RealSaveFingerprint）
- `Logs/verify/gate-20261118/unity-run-iso.py`（带 `--project-path` 的 unity-run 包装）
- `Logs/verify/gate-20261118/shot.py`（截图落盘，只打印路径与 md5）
- `Logs/verify/gate-20261118/buttons-after-newgame.txt`、`buttons-map.txt`（按钮路径摘录）

未修改任何产品代码、场景、预制体、`ProjectSettings`，未新增 Unity 包，未执行 git 写操作。
