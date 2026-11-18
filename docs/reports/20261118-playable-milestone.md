# v1 可玩里程碑验证（实现规格 §3 七条）— 2026-11-18

- 被测对象：主工作树 `C:\Users\panye\My project`，`main` `ca96f4f` + 未提交改动；内容与 `panyeung/m00-closeout` `e57dd06` **逐字节相同，仅行尾 LF/CRLF 不同**（`diff --strip-trailing-cr` 输出 0 行）。
- Editor：PID 96584，端口 7800，`6000.6.6f1`。开工前 PID 88424 因测试超时后 `RestoreSceneSetupTask` 抛 `IndexOutOfRangeException` 卡死主线程（pipeline 全部超时、UIA 无响应），已按 PID 精确结束并重启；场景备份存 `Logs/recovery-20261118/scene-0.backup`。
- 驱动方式：`unity command editor_play` + `run_script`（`.opencode/skills/verify/scripts/VerifyUi.cs` 与本轮新增 `Logs/kanban-20261118/MilestoneProbe.cs`）。每条判据都用同一次调用内「动作 + 前后读回」，避免跨调用状态漂移。
- 分辨率：`capture_game_view` 报 1280×720；`VerifyUi.State` 报 Game 视图 3840×2160（CanvasScaler 参考分辨率 1280×720）。

## 分项结果

| # | 判据 | 结果 | 读回证据 |
|---|---|---|---|
| 1 | 标题页文案与两个入口 | PASS | `M6TitleLabel=影之一族`、`M6TitleSubtitle=守护家名，传承百年`、`M6NewGameButton=开始新游戏`、`M6ContinueButton=继续游戏` |
| 2 | 固定三代剧本进营地 HUD | PASS | 点 `SC013FixedScenarioButton` 后 `M02HeaderDateText=1560 年 4 月 1 日`、`M02HeaderLocationText=所在　清洲`、人口 6、资金 30、物资 12、情报 0 |
| 3 | 结束月份推进并有可读变化 | PASS | `MilestoneProbe.PressAndReport(EndMonthButton)`：clock `1560-04-01`→`1560-05-01`，hud `1560 年 4 月 1 日`→`1560 年 5 月 1 日`，资金 30→28，物资 12→12，人口 6→6 |
| 4 | 名册有身份能力关系、编年史有已发生事件 | PASS | `FamilyRosterPanel` 六人跨第 1/2/3 代与附属成员，含 `家谱连线：亲子 金 · 收养 紫 · 手足 绿 · 配偶 蓝`、`固定继承人：明智 正綱（46岁）`；交接后 `FamilyChronicleRow_2=家督已交接给 明智 正綱。` |
| 5 | 一次家主更替，世界不重置 | PASS | `SuccessionHeadText` `当代家主：明智 晴信（32岁）`→`当代家主：明智 正綱（24岁）`；`SuccessionFeedbackText=家督已交接给 明智 正綱。`；同一次读回日期仍 `1560-04-01`、人口 6、资金 30、物资 12、所在清洲 |
| 6 | 存档→推进一天→读档回到存档时 | **BLOCKED** | 存档被拒：`SaveLoadPanel.Status=调试会话不会写入玩家存档。`，`slotExists=False`。隔离槽已生效（`store=…\ISO-Milestone-20261118`，`isDefault=False`，`titleControllersRedirected=1`）。同屏 `M09DebugModeRoot` 的 `DebugClose`/`DebugTab0-4`/`DebugReset`/`DebugExit` 均为可交互按钮，说明有 DebugSession 处于活动状态；而场景文件里 `M09DebugModeRoot m_IsActive: 0`（当前与 `e57dd06` 都是 0），即运行期被激活。触发来源本轮未定位 |
| 7 | 城镇页列出当地委托且能点开 | PASS | 清洲 `TownActivityButtons` 四条可交互：`补给短缺：领取`、`密信：领取`、`护送委托：领取`、`潜入密令：开始`；点 `密信：领取` 打开 `M2WorldMapRoot/TownMenuRoot/M4SupplyShortageMissionRoot`，读回 `SealedLetterStageText=密信：未领取  功绩：0`、`SealedLetterObjectiveText=在清洲向橘 すず领取密信。`、`领取密信` 可交互 |

## Console

- 开工基线：`counts` error 0 / warn 0 / log 15，`groundTruth.consoleErrors=0`。
- 本轮结束：`counts` error 12 / warn 53 / log 29，`groundTruth.consoleErrors=0`。
- 12 条 error 逐条归因，**无一条是游戏运行时错误**：
  - seq 61、62：`家族编年史记录写入失败（家族收养 / 家族婚姻）`，`FamilyFormationSessionTests` 的负向用例预期输出，`docs/console-基线归因表.md` 已定性。
  - seq 83：本轮误发 `open_scene`（Play Mode 中不可用），工具自报。
  - seq 86：`ExecuteCommandByName: No command named 'list'`，本轮探测命令拼写错误。
  - seq 87–94：进入 Play Mode 时被取消的残留测试运行（`Test tree is not available`、`cannot be used during play mode`），来自开工前那次超时测试的残留，属 Editor 工具侧，不是游戏代码。

## 存档安全

- 真实槽 `%LOCALAPPDATA%\..\LocalLow\DefaultCompany\My project\SengokuChronicle\` 的 SHA-256 前 16 位与 mtime 全程未变：`save-slot-1.json` `1e61ed9cab4a5f3e`（mtime 1794095664）、`save-slot-1.json.bak` `f1c0630e5dd91baa`（mtime 1793910132）。
- 所有存读档尝试都发生在 `ISO-Milestone-20261118` 隔离目录，`isDefault=False`。

## 判定

**PARTIAL**。§3 七条中 5 条 PASS（1、2、3、4、5、7 计 6 条，第 6 条 BLOCKED）。第 6 条阻塞原因是 DebugSession 在正常开局里被激活，存档被面板主动拒绝——这是缺陷或状态污染，不是验证方法问题。

## 编译、全量回归与场景验证（本轮实测）

- 全量 EditMode：`run_tests --mode EditMode --detach` 作业 `bc1a32ce920d461fbe4f929335f22258`，`TestResults.xml` 记 `testcasecount=2141 passed=2141 failed=0 skipped=0 result=Passed`，起止 `2026-11-18 05:21:21Z`–`05:22:31Z`。副本 `Logs/verify/20261118-milestone/TestResults.xml`。
- 场景 Validator：`run_script scripts/verify/RunValidator.cs RunValidator.Run` → `Logs/SceneValidationReport.txt` 末行 `RESULT: PASS; Errors: 0`。
- 二者与本文件的 Play Mode 证据互不替代：编译与 EditMode 不等于运行时行为，Play Mode 也不证明测试集全绿。

## 复现第 6 条的最小步骤

1. 重启 Editor，Play Mode → 标题 → 开始新游戏 → SC-010 标准样例。
2. `run_script IsolateMilestone.Set` → `isDefault=False`。
3. 点「地图与出行」→「存档」。
4. 期望「已保存」；实际 `Status=调试会话不会写入玩家存档。`，`slotExists=False`。
