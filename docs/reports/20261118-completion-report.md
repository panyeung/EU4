# 《影之一族》完成情况报告 — 2026-11-18（收口版）

**状态：未完成（PARTIAL）。** `2bca9d1` 的 v1 Windows 演示包已构建并通过本卡范围内的 Player 冒烟，v1 最终验收仍待用户完成；这不是整项目完成的唯一条件。项目还需闭合 M08 战役回顾面板运行时打开与 M09 真机窗口拖动。`main` 的代码基线是 `2bca9d1`；后续提交只追加文档。SC-021 场景集成与 SC-023 演示接入现已在 `main`，不再列为未合入。

状态时间说明：本报告记录截至 2026-11-18 的审计结论。用户于 2026-11-24 已报告 M00 演示试玩通过；此后状态以 `PROJECT_STATE.md` 与 `checkpoint.md` 为准。

本版由收口卡 `t_080705e2` 写。上一版（`t_53b146b3`）的三条 MAJOR 已全部处置，处置结果见第 2 节。凡标「本卡亲跑」的是我这次实际执行的命令与读回值；标「上游卡汇报」的只是转述，未由我重跑。

---

## 1. 现在能做什么

- **能构建**：`Builds/ShadowClan-v1-demo-win64-2bca9d1-r2/`，源提交 `2bca9d1`（干净工作树），`Succeeded`、`totalErrors=0`、218 个文件 183,882,557 B。exe 667,136 B、`sha256 7c5b8f3e…188247d`，本卡亲跑 `sha256sum` 核对与构建卡汇报一致。唯一警告是 Pipeline 包自带的 `No RuntimePipelineConfig asset found`。
- **能启动并玩通核心循环**：Player 冒烟（上游卡汇报，真实 SendInput 鼠标输入）走通标题 →「开始新游戏」→ SC-013 → 营地 HUD（1560-04-01、清洲、贯 30）→ 结束月份（1560-05-01、贯 28）→ 清洲城镇页四条当地委托 → 点开任务页；`Player.log` 0 条 Exception/LogError/NullReference。
- **能存读档往返**：Editor Play Mode（上游卡汇报，隔离槽）1560-05-01 存档 → 推进一天 → 读档回 1560-05-01；SAVE-3 待决继承改选后读回「時姫」；SAVE-4 空槽返回「尚未找到存档。」且状态不变。真实存档槽 `sha256 1e61ed9c…` 与 mtime 全程未变。
- **全量 EditMode 全绿**：`2214/2214` passed、`failed=0`、`skipped=0`（2026-11-18 13:22:34Z–13:24:40Z，源 `c291dd3`）。本卡亲读 XML。
- **场景校验通过**：Validator `RESULT: PASS; Errors: 0`（源 `c291dd3`）。本卡亲读 `scene-validator-c291dd3.txt` 与 `Logs/SceneValidationReport.txt` 两处末行。
- **集成分支已落地**：当前 `main` 与 `origin/main` 同步，最新代码基线仍为 `2bca9d1`；`integration/v1-20261118` 留在 `2bca9d1`。五个任务分支的本地 ref 均已并入，且只剩一个工作树。

## 2. 三条 MAJOR 的处置结果

| 上版缺口 | 处置 | 证据 |
|---|---|---|
| 四个任务工作树未合并进 `main` | **已闭合**。收口卡当时核实 `panyeung/m00-closeout`、`ui-m04-family`、`ui-m05-management`、`ui-m06-m07-missions`、`ui-m08-campaign` 均已并入；主工作树当时与集成分支同点。之后 `main` 只追加文档提交；当前 `main` 与 `origin/main` 同步，代码基线仍为 `2bca9d1`。当前只有一个工作树 | 收口卡亲跑，后续文档 ref 另行核对 |
| M05 独立复审未通过 | **已闭合**。独立复审 `t_122d4d5b` 判 PASS，C1–C6 全过：三组公开成员有真实行为断言；`IndustryPanel` 裁切/滚动与三条回滚用例已在树中；未删除测试文件、无恒真断言；`skipped=0` | 复审卡结论（上游，未由我重跑） |
| F1 城镇设施槽不在 `main` | **已闭合**。`Assets/Scripts/Presentation/WorldMapController.cs` 内 `RefreshFacilitySlots` 4 处命中；两条用例 `TownMenu_FacilitySlotsListRealTownEntries`、`TownMenu_FacilitySlotsStayEmptyWhenTheTownPageRenders` 在 2214 全量 XML 中均为 Passed | 本卡亲跑 `git grep` 与 XML 逐条读回 |

另有一条上版的实例级缺口（`main` 在途副本被外部重置清掉）**已失效**：那些文件随 `ui-m04-family` 并入 `main`，现在在树里，不再是损失。

## 3. 验证证据，按可信度分四层

**我（收口卡）亲自跑的**

- 三个 ref 同点、单一工作树、`git status --porcelain` 为空。
- 收口卡当时 `git log main..integration/v1-20261118` 为空；`git diff --stat d081c46...main` 为 51 文件 +10077/−1821。此后 `main` 追加了文档提交。
- 五个分支对 `main` 的 `merge-base --is-ancestor` 全为真。
- `TestResults.xml` 首行：`testcasecount=2214 passed=2214 failed=0 skipped=0 result=Passed`。
- F1 两条用例在该 XML 里 `result="Passed"`；源码里两条用例确实存在（`WorldMapUiTests.cs:284`、`TownActivityRoutingTests.cs:159`）。
- Validator 末行 `RESULT: PASS; Errors: 0`（两处证据文件一致）。
- `Assets` 内 `CanvasOverlayOrder` 零命中。
- `c291dd3..2bca9d1` 的 diff 逐行读过：只有注释与错字（4 文件 −2/+7），所以基于 `c291dd3` 的测试与 Validator 证据可以代表 `2bca9d1`。
- exe 的 `sha256sum` 与大小，与构建卡汇报的一致。
- `gh auth status` 可用（账号 `panyeung`），远端 `https://github.com/panyeung/EU4.git`。
- `hermes send --list`：本机没有任何已配置的通知平台。

**上游卡汇报，我未重跑**

- Play Mode 的五条 PASS 表与真实存档槽指纹（`Logs/verify/20261118-integration/20261118-1348-core-loop.md`）。
- Player 冒烟路线与 `Player.log` 计数（`Logs/verify/v1-demo-build/20261118-r2-demo-build.md`）。
- 编译 0 个 `error CS`（`20261118-1348-core-loop.md` §一）。
- 复审卡 `t_122d4d5b` 的 C1–C6 判定细节。

**我推断的**

- 既然 `c291dd3..2bca9d1` 只改注释，那么 2214/2214 与 Validator PASS 对 `2bca9d1` 同样成立。这是推断，不是重跑；要坐实需要在新 Editor 上重跑一轮。
- `Builds/ShadowClan-v1-demo-win64-2bca9d1-r2/` 在 `.gitignore` 内（`git check-ignore` 命中 `/[Bb]uilds/`），因此它只是本地产物，不入库。

**没有验证的**

- 用户最终验收本身。
- M08 战役回顾面板的运行时打开。面板只在真实 55 年阶段回顾或 100 年结局待处理时显示。
- M09 真机窗口拖动。
- 成品 Player 的存读档（构建卡按卡内约束未做，沿用 Editor 隔离槽证据）。

## 4. 仍然存在的缺口

| 严重度 | 缺口 | 为什么还在 |
|---|---|---|
| MAJOR | M00 用户最终验收未完成 | 验收权在用户 |
| MINOR | M08 战役回顾面板运行时打开未验证 | 需长周期演示路线；不以调试跳日期取证 |
| MINOR | M09 窗口拖动只在合成输入下验过 | 需真实硬件与可交互桌面 |
| MINOR | `Assets/Tests/PlayMode` 为空 | 运行时验证靠手工配方 |
| MINOR | 成品约 22 条 URP 后处理着色器被剥离 | 着色器变体裁剪，Bloom/景深/运动模糊在产物中不生效 |
| MINOR | 城镇菜单三个 `Facility1..3Text` 场景节点仍在 | 内容由 `RefreshFacilitySlots` 决定；删节点属场景改动 |
| MINOR | 世界地图屏「NPC 自主性」按钮与「清洲」卡片局部重叠 | 目视记录，未判定是否为设计意图 |
| MINOR | 剧本选择页两条按钮显示名均为「SC-010」 | 命名与文案不一致，未改 |
| MINOR | 选 SC-013 后单帧读到「日期未初始化」 | 面板切换期绑定时序，未稳定复现 |
| MINOR | 无音频、无切图、无动画 | 决策 D5 不进 v1 |
| MINOR | `Assets/Scenes/` 内有 stray `TestResults_*.xml` | 清理需批准 |
| MINOR | 无 CI / 打包流水线 | 唯一构建路径是已开 Editor |

已闭合、可从缺口表移除的：`uncompiled code changes` 警告（r2 构建基于干净 `2bca9d1`）、SAVE-3 只到会话层（Play Mode 已单独跑通）、SC-021 场景集成与 SC-023 演示接入。主树含合并提交 `5885315`、集成提交 `115ffa7`，场景包含 `SC021ResearchRoot` 与 `SC023SeclusionDemoButton`；此处仅记录集成状态，不据此声称新增运行时验收通过。

## 5. 下一步

1. **v1 演示验收**：跑 `Builds/ShadowClan-v1-demo-win64-2bca9d1-r2/ShadowClan-v1-demo-win64-2bca9d1-r2.exe`，按第 1 节的路线走一遍。通过后才能标记 v1 阶段已验收；整项目仍需完成本节后续列出的工作。
2. 验收通过后，由用户点头再打里程碑 tag；本卡没有合并、没有打 tag、没有 force-push。
3. 补 M08 战役回顾面板的长周期演示路线。
4. 在可交互桌面上复验 M09 窗口拖动。
5. MINOR 清理需要批准（stray XML、`Assets/Logs/verify/_scratch/` 截图、隔离槽目录）。

## 6. 本次任务的边界

- 只改文档：`checkpoint.md`（§2 决策记录两条、§4 一行、§5 当前状态重写、§6 下一步重写）、`README.md`、本报告。未改产品代码、场景、预制体、`ProjectSettings`、包。
- 未合并 `main`、未打 tag、未 force-push。`main` 在本卡开工前就已是 `2bca9d1`。
- 未开 PR：`main` 与 `integration/v1-20261118` 同点，零差异 PR 无意义，故跳过并在阶段报告里写明原因，没有伪造链接。
- 通知脚本按项目规矩执行；本机无通知平台，只入 `Logs/notifications/outbox.txt`。

## 7. 来源

- `Logs/verify/20261118-integration-full/TestResults.xml` — 全量 EditMode 2214/2214（源 `c291dd3`）。
- `Logs/verify/20261118-integration/scene-validator-c291dd3.txt`、`Logs/SceneValidationReport.txt` — 场景校验 PASS/0。
- `Logs/verify/20261118-integration/20261118-1348-core-loop.md` — 编译、Play Mode 核心循环与存读档。
- `Logs/verify/v1-demo-build/20261118-r2-demo-build.md` 与其 `-build-report.json`、`-playerlog.log` — 演示构建与成品冒烟。
- 任务卡 `t_122d4d5b` — 集成代码与证据的独立复审（PASS）。
- 任务卡 `t_3fa4c881`、`t_6ac123be` — m05 与 m04+m08 合流、层序统一、场景重放。
- 历史档案：`docs/reports/20261118-project-audit-gaplist.md`、`-compile-console-blockers.md`、`-feature-gaps-implementation.md`、`-build-dogfood-defects.md`、`-playable-milestone.md`。
