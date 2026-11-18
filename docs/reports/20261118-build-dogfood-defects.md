# 构建、核心循环 dogfood 与缺陷报告 — 2026-11-18

任务：t_9857270d。执行：tester。工程：`C:\Users\panye\My project`，分支 `main` @ `a40cf5b`（工作树在起点即含 35 已跟踪修改 + 39 未跟踪，来自上游任务，本任务未提交、未清理）。

## 1. 构建设置核验（只读，未修改）

- 场景列表：`ProjectSettings/EditorBuildSettings.asset` 仅启用 `Assets/Scenes/FoundationScene.unity`，GUID `be44f8a18fed65944b8591b78cd6d930`，与 `.unity.meta` 一致。bridge `get_build_settings` 返回同一场景、enabled=true。顺序：单场景，无顺序问题。
- 目标平台：ProjectSettings 命名 `StandaloneWindows64`（`activeBuildTargetGroup=Standalone`）；实际 active target 同为 `StandaloneWindows64`。
- 产品名/公司名：`productName: My project`、`companyName: DefaultCompany`（模板默认值，未改）。
- 分辨率/aspect：`defaultScreenWidth=1920`、`defaultScreenHeight=1080`、`defaultIsNativeResolution=1`、`defaultScreenOrientation=4`。
- 缺失脚本引用：`m_Script: {fileID: 0}` 命中 0（FoundationScene、SampleScene）。
- 结论：构建设置自洽、可构建；产品名/公司名为模板默认，属待定品牌项，非阻塞。本任务按“不动配置”准则未改任何 ProjectSettings。

## 2. 构建（已实际产出）

- 方式：Unity 已开 Editor（PID 96584，端口 7803）占用本工程目录，按 AGENTS.md 禁止对该目录跑 batchmode；改用受支持的 Pipeline bridge `build` 命令（异步 + `build_status` 轮询）。
- dry-run：`unity command build --target StandaloneWindows64 --outputPath "Builds/StandaloneWindows64/My project.exe" --confirm true --dry_run true` → `status=dry_run, valid=true, validationErrors=[]`。
- 实际命令：`unity command build --target StandaloneWindows64 --outputPath "Builds/StandaloneWindows64/My project.exe" --confirm true` → `status=queued, buildId=build_c5656da0bcf5`。
- 轮询：`unity command --timeout 120 build_status --result-only` → `status=completed, result=Succeeded`。
- 输出路径：`C:/Users/panye/My project/Builds/StandaloneWindows64/My project.exe`（exe 667,136 B；总 183,831,693 B；buildTimeMs=200,474 ≈ 200s；06:38:31Z→06:41:52Z）。
- 产物核验：exe 存在且可执行；`My project_Data/` 含 `level0`、`resources.assets`、`Managed/SengokuChronicle.*.dll`（Domain/Events/Persistence/Application/Presentation）、`boot.config` 等。
- 构建报告统计：`totalErrors=3`、`totalWarnings=513`。逐条核验：
  - 3 条 error 全是 `Failed to handle /api/exec request: Main thread operation timed out after 30000ms` —— 是我在构建期间发 `build_status` 轮询被主线程阻塞的工具超时，非产品编译错误。
  - 513 warning 中 460 条为 `ConvGeneric: TRANSPOSE_OUTPUT variants not supported...`（Sentis/AI 预览包着色器），其余为 Sentis 着色器 signed/unsigned 与 loop 变量警告、`Object.FindFirstObjectByType obsolete`、1 条 `building a player, but you have uncompiled code changes`、1 条 Pipeline 运行时配置缺失提示。均为第三方包/设置类，无产品 C# 编译错误。
  - 完整报告：`Logs/build-dogfood-20261118/build-report.json`（1.9 MB）。
- 判定：**构建通过（Succeeded）**，产物路径如上。

## 3. 核心循环 dogfood

两条路径并行取证：

### 3a. 已构建 Player 运行时（真实成品）
- 启动：`cmd /c start "…\Builds\StandaloneWindows64\My project.exe"` → 进程 My project.exe (PID 126920) 正常起，DX12 + RTX 4070。
- Player.log（`C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\Player.log`，副本 `Logs/build-dogfood-20261118/Player.log`）：引擎/输入/物理初始化完成；`grep -Ei 'exception|LogError|NullReference'` 命中 **0**；仅约 22 条 URP 后处理着色器被剥离提示（Bloom/景深/动态模糊等，属裁剪告警）。
- 判定：成品能启动、进引擎、无运行时异常。GUI 点击驱动在 headless 会话被审批门控拦截，未能在成品窗口逐屏点完；运行时交互改由 3b 的 Editor Play Mode 完成。

### 3b. Editor Play Mode 交互（受支持 bridge + 只读探针）
- 进入：`unity command editor_play` → `playMode=playing`。
- 标题页：Game view 截图目视正常（标题「影之一族」、副标题、继续/开始新游戏、插画；无报错/错位/重叠）。右下角有「Debug 综合测试」标签，即 `M09DebugModeEntry` 标题按钮（`M09DebugModeRoot` 为 off）。
- 开始新游戏 → 剧本选择页（固定三代 SC-013、SC-010 标准样例、SC-010 危机示范、SC-020~024 专题）截图目视正常。
- 选「固定三代剧本（SC-013）」→ 营地 HUD：`clock=1560-04-01`、`hud=1560 年 4 月 1 日`、`loc=所在　清洲`、`people=6`、`funds=30`、`supplies=12`；`isDebugSessionActive=False`（印证上游 B1 在正常路径不可复现）。
- 结束月份：`1560-04-01 → 1560-05-01`，资金 `30 → 28`，HUD 文本可读变化。核心推进循环 PASS。
- 存档：`SaveThroughPanel`（经隔离重定向失败后误落默认槽）返回 `status=已保存当前进度。; lastOk=True; slotExists=True`。该副作用已即时回滚（见第 5 节）。独立隔离槽往返因 Editor 卡死未完成，沿用上游 t_dd30ab75 的 Play Mode 往返证据。
- 世界地图/城镇页：按钮枚举可见 `DestButton_0/1/2`（清洲/热田/那古野）、`确认出发`、`存档/读档`、`家族名册/编年史`，以及 `TownActivity_SupplyShortage / SealedLetter / EscortCommission / TacticalMission` 四条当地委托均可交互（印证 F1 已修）。

**核心循环判定：PASS（可启动、可进游戏、月份推进有可读变化、设施委托可点、无运行时异常）。** 存档往返完整闭环本轮未独立复现（Editor 卡死），引用上游证据。

## 4. 缺陷清单（按严重度）

- [MINOR] 构建报告含 1 条 `building a player, but you have uncompiled code changes`：构建时工作树有未编译改动，产物可能不含最新源码状态。证据 `Logs/build-dogfood-20261118/build-report.json`。文件参考：工作树 35 修改 + 39 未跟踪（未提交）。
- [MINOR] 成品 Player.log 约 22 条 URP 后处理着色器被剥离（Bloom/景深/运动模糊/全景投影）：`PostProcessing render passes will not execute`。这些后处理在构建中不会生效。证据 `Logs/build-dogfood-20261118/Player.log`。文件参考：URP 项目设置/着色器变体裁剪。
- [MINOR] 剧本选择页两条按钮显示名均为「SC-010 …」而内部标识为 `SC013StandardSampleButton` / `SC013CrisisDemoButton`，命名与文案不一致（可能是有意复用，仅记录）。证据 Game view 截图 + Buttons 探针输出。文件参考：`TitleScreenController` / 场景 `M6ScenarioSelectRoot`。
- [MINOR] 世界地图屏左下「NPC 自主性」按钮与「清洲」目的地卡片局部重叠（截图目视）。仅记录，未判定设计意图。
- [MINOR] 选 SC-013 后某一瞬间探针读到 `hud=日期未初始化 / people=—`（world map 打开时），随后恢复正常日期与数值；疑为面板切换期文本绑定时序，单帧观察，未稳定复现。仅记录。
- [NOT A PRODUCT DEFECT] 构建报告 3 条 error 与 513 条 warning：经逐条核验全为工具轮询超时与第三方 Sentis 着色器告警，非产品编译错误。
- 沿用上游、本轮未独立复现的已知缺口（来源 `docs/reports/20261118-project-audit-gaplist.md`、`20261118-compile-console-blockers.md`、`20261118-feature-gaps-implementation.md`）：
  - [MAJOR] B3 四个任务工作树未合并进 main（集成代理范围）。
  - [MAJOR] B5 M00 用户最终验收未完成（用户范围）。
  - [MINOR] B6/P8 M09 窗口拖动真机未复验（需真实硬件）。
  - [MINOR] F2/F3/F4 跨代链运行时/战术投入/长期模拟基线按规格非目标延后。
  - [MINOR] P1~P7 音频/美术/PlayMode 自动化/stray XML/模板/CI 等 polish。

本任务未发现可由安全单行改动修复的产品代码缺陷，故未修改任何产品代码。

## 5. 副作用与恢复（如实记录）

- 试图用 `DebugLeakProbe.Redirect`（隔离目录 `ISO-DebugLeak-20261118`）做存读档往返，返回 BLOCKED（该隔离槽已被上游任务占用）。
- 随后一次 `SaveThroughPanel` 在重定向未生效的情况下写入了**默认真实存档槽** `AppData\LocalLow\DefaultCompany\My project\SengokuChronicle\save-slot-1.json`（14000 B，hash c2cb…）。
- 已立即用同目录 `.bak`（hash `1e61ed9cab4a5f3e…`，mtime 2026-11-07 13:54）恢复该真实槽；恢复后 hash 与 mtime 与上游卡记录基线完全一致，最终复核仍为 `1e61ed9cab…`。真实玩家存档未受净影响。
- 后续 tester 自写探针 `Logs/build-dogfood-20261118/DogfoodSaveProbe.cs` 指向全新隔离目录 `ISO-Dogfood-20261118`，在 Play Mode 编译 `run_script` 时导致 Editor 主线程卡死（见第 7 节），该隔离往返未完成。

## 6. 无法在本环境验证 / 未运行

- 成品 GUI 逐屏点击：headless 单查询模式下 computer_use 的 click/focus_app 需审批，被拦截；已用 Editor Play Mode + 只读探针覆盖同类交互。需可交互桌面会话才能对成品 exe 逐屏点击。
- 完整隔离槽存/推进/读闭环：Editor 卡死未完成本轮独立复现；引用上游 t_dd30ab75 的 Play Mode 证据（1560-04-01 存 → 推进 → 1560-04-02 → 读 → 1560-04-01）。
- M09 真机窗口拖动：需真实硬件。
- 用户最终验收：需用户。

## 7. 额外观察（仅陈述）

- Editor（PID 96584，端口 7803）在本任务末段对 Pipeline 命令全部超时（editor_status/console_status/run_script），`Logs/Editor.log` 自 21:01:17 起停止增长（约 10 分钟无新增），进程仍存活、内存约 2.3 GB。触发点为本任务在 Play Mode 下用 `run_script` 编译 tester 探针；日志尾含 `[PipelineTestRunner] Invalidated previous test run` 与 `InvalidOperationException: This cannot be used during play mode, please use SceneManager.CreateScene()` 等测试运行器取消回调。**未强制结束该 Editor**（共享资源，可能含其他 agent 未保存工作）。需主控/人工检查或重启该 Editor。
- 成品 Player.log 启动阶段 3 条 `d3d12: failed to query info queue interface (0x80004002)`，为 D3D12 调试接口查询提示，游戏继续正常初始化。
- `capture_game_view` 将 `save_path` 重定向到 `Assets/Logs/...`；本任务在其中生成的 3 张截图及 .meta 已目视检查后立即删除，保留的 `Assets/Logs/verify/_scratch` 早于本任务截图、非本任务产物，未触碰。
- 全程未修改产品代码/资源/ProjectSettings，未提交、未推送、未合并。
- 任务起点 HEAD 为 `a40cf5b`（工作树 72 条未提交）；任务末 HEAD 变为 `5cd8521`（合并 `panyeung/m00-closeout`，工作树 37 条且多为 staged，文件集合已换成 M04 family-UI）。即有另一写者在本任务期间对同一共享工作树 `main` 提交了合并。本报告的构建与 dogfood 基于起点 `a40cf5b` + 当时未提交改动。Editor.log 尾部的 PipelineTestRunner 活动提示同期可能有测试运行占用同一 Editor（AGENTS.md 禁止跨卡并行写者），这可能是末段 Editor 卡死的诱因之一 —— 仅陈述时间相关性，未判定因果。

## 8. 证据与日志

- 构建报告：`Logs/build-dogfood-20261118/build-report.json`
- 成品运行日志副本：`Logs/build-dogfood-20261118/Player.log`
- tester 自写探针：`Logs/build-dogfood-20261118/DogfoodSaveProbe.cs`（编译失败/卡死前未完成往返）
- 复用只读探针：`Logs/kanban-20261118/MilestoneProbe.cs`、`Logs/debug-leak-20261118/DebugLeakProbe.cs`
- Editor 日志：`Logs/Editor.log`（21:01:17 后冻结）
