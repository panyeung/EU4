# 项目状态

更新时间：2026-11-24（UTC）
状态：IN PROGRESS

本文件记录当前工作状态。设计目标、里程碑和测试口径以 [checkpoint.md](checkpoint.md) 为准。界面与美术规则以 [ui设计规划.md](ui设计规划.md) 为准。

## 当前基线

- 仓库：`https://github.com/panyeung/EU4.git`。
- `main` 与 `origin/main` 同步。游戏代码基线为 `2bca9d1`。之后的主线提交只更新文档。本文件最初在 `feature/project-state-docs` 独立工作树编写；该文档分支已合入 `main`，不代表当前分支状态。
- Unity 版本：`6000.6.6f1`，URP 2D，Windows。
- 当前主工作树：`C:\Users\panye\My project`，分支 `docs/orca-project-workflow`，跟踪 `origin/docs/orca-project-workflow`。本轮代码与测试提交为 `696af80`（关闭全国地图刷新修复，已推送 `2f7496d..696af80`），其后的文档状态同步提交紧随其上；`main` 与 `origin/main` 同步在 `65a021d`，游戏代码基线仍为 `2bca9d1`。未提交改动只有用户修改的 `AGENTS.md`，本轮未 stage、未 checkout、未覆盖。该集成分支相对 `main` 的已提交差异包含日本全境地图、场景接线、存档迁移、地图/城镇 UI、测试与验收文档，详见 PR #3。
- 本次状态核对（2026-11-24 00:21 UTC）：主工作树 Unity Editor PID `95724`，`-projectPath` 为 `C:\Users\panye\My project`，Pipeline `7804`，`editor_status=ready`、`playMode=stopped`、`compiling=false`，当前场景 `FoundationScene` 未保存更改。Canvas 启动入口 EditMode 运行后 `groundTruth.compilationFailed=false`，新增 error 为 0，4 条新增 warn 均为已知 TMP 后备字体警告；累计 1 条 error 是此前误调 `unity command list` 的历史记录。任何 Unity 操作前重查进程、`-projectPath` 与 `unity pipeline list`；已有实例时复用，不得启动第二实例，也不得据此启动 batchmode。
- 关闭全国地图刷新修复收尾（2026-11-24）：同一 Editor PID `95724` / Pipeline `7804`，收尾 `editor_status=ready`、`playMode=stopped`、`compiling=false`，`compilationFailed=false`；`FoundationScene` 为唯一加载且激活场景、`isDirty=false`，未保存场景。Console 光标 `1827` 时的未读计数为 error 0 / warning 1 / log 0（该 warning 为已知 TMP 后备字体提示），这只是未读条数，不等于本轮零错误。会话累计 `counts` 为 error 1245 / warn 273 / log 309，来源已按 `Logs/Editor.log` 与时间窗增量核对：1236 条是修复前红灯测试在 EditMode 下调用 Destroy 产生的 `Destroy may not be called from edit mode`（行号 17450–86610），1 条是改正前的 `CS0234` 编译错误，4 条是两次全量里既有负向用例的家族编年史写入失败，另 4 条是会话开头的旧记录与 CLI 误调。修复后 Play Mode 复验窗口（`console --since 1826`）新增 error 为 0。测试全程串行，未使用 batchmode；运行前已确认其它三个工作树 Editor 的 `test_status` 均为 `completed`。

## 当前阶段

**v1 Windows 演示的技术签核已通过，待用户最终验收。** `t_39a5b047` 于 2026-11-18 给出的 RETURN 针对文档过期状态；修订后，2026-11-19 的独立只读文档复审为 ACCEPT。主控基于现存构建、测试、Validator、Play Mode 与 Player 冒烟证据签核 M00 技术门。演示包可运行，但整项目尚未完成；用户试玩验收仍未完成。

演示包：

- 程序：`Builds/Demo-v1-2bca9d1-r2/Demo-v1-2bca9d1-r2.exe`。
- 来源：干净代码基线 `2bca9d1`。
- 已记录产物有 218 个文件，共 183,882,557 字节。exe 大小为 667,136 字节，SHA-256 为 `7c5b8f3ecaf9ef88a16646eacc9f817f327e3dde3244ccc627bd2e273188247d`。
- 路线：标题页 → 开始新游戏 → SC-013 → 营地 → 结束月份 → 清洲城镇 → 当地委托。

## 现有验证证据

| 范围 | 现有结果 | 证据边界 |
|---|---|---|
| EditMode | `2214/2214` 通过，源 `c291dd3` | 历史结果，不是本轮重跑 |
| FoundationScene Validator | `PASS; Errors: 0`，源 `c291dd3` | 历史结果，不是本轮重跑 |
| Play Mode 核心循环与隔离存读档 | 已记录通过，源 `c291dd3` | 详见 `Logs/verify/20261118-integration/20261118-1348-core-loop.md`。此证据不代表 Player 存读档或用户验收 |
| Windows Player 构建与冒烟 | 构建成功，报告记 0 错误。冒烟日志无 Exception、LogError、NullReference | 详见 `Logs/verify/v1-demo-build/20261118-r2-demo-build.md`。来源为 `2bca9d1` |
| Windows Player M00 试玩预验收 | 2026-11-19 本机 EXE 从标题页走到清洲护送委托详情；日期、资金、物资与事件反馈符合试玩步骤 | 详见 `docs/reports/v1-m00-unity-playtest-20261119.md`。Computer Use 输入属主控预验收；日志含 URP 后处理 Shader 警告，不等于用户最终验收 |
| M08 五十五周年与百年 PlayMode | 合并在同一端到端用例，1/1 通过；逐次断言月结成功和自然月日期变化，覆盖 660/1200 次成功月结、两次回顾与隔离存读档；独立复审 ACCEPT | PR #4 当前提交 `74f7579`；自动按钮回调测试不等于实体输入或用户最终验收 |
| 日本地图双启动入口 Canvas 修正 | `2bc3b56` 统一两个 `Start` 的 Canvas 解析入口；EditMode `JapanMapPlayableTravelTests` 68/68 通过，独立只读复审 ACCEPT | 结果副本 `Logs/verify/20261123-pr3-canvas-selection/TestResults.xml`，MD5 `e2bb562af29a5c5ed2746ca74d2143bc`，SHA-256 `C98D8412F7E423E948E2673B4AE1FE4649ABE36F29E3A7829CB8F16083472B68`。自动测试不等于 FoundationScene Play Mode 或用户试玩验收 |
| M2 全国地图关闭刷新修复 | 原主控预验收 RETURN 的缺陷已由 `696af80` 修复并获独立只读复审 ACCEPT；用户试玩验收未完成 | 红灯 5 例 2 过 3 败→绿灯 5/5；全量 EditMode 2306/2306；最终代码 Play Mode 复验关闭后即显示冈崎与 `1560-04-03`。证据 `Logs/verify/20261124-m2-overlay-close-refresh/`，XML SHA-256 红灯 `c56f17dcddc95324f0001cd5fb31a0708af63417af567f6287228b6ef7398732`、绿灯 `ee068b8eadab94ec4551b4d2bc7f317e3b03607764deea0e04d0c8c714279fe3`、全量 `2ea52b4bcf94b43944844fb12c88c162cf210d930506502f516498bfa6e963bd`。Console 累计计数含修复前红灯运行的 1236 条 `Destroy may not be called from edit mode` 与 1 条 `CS0234` 编译错误；修复后新增 error 只有两次全量里既有负向用例的 4 条，Play Mode 复验窗口新增 error 0，不能表述为全程零错误。Play Mode 操作为 VerifyUi 调用真实 `onClick` 处理器，不是实体输入，也未截图；用户试玩验收未完成 |
| 用户验收 | 未完成 | 只有用户试玩后才能闭合 |

Git 忽略 `Logs/` 与 `Builds/`。这些日志和 exe 目前只在本机工作区。GitHub 分支克隆不包含它们。交付演示时，确认用户能访问此工作区，或另行提供构建包。

不能把自动测试、Editor Play Mode 或 Player 冒烟替代用户验收，也不能把 v1 演示可玩等同于整项目完成。

## 未完成工作

1. M00 技术签核已通过；待用户试玩 v1 演示并给出最终验收结论。
2. M08 全量验证归档卡 `t_0a1e8ce8` 与其 55/100 年运行时记录已获独立复审 ACCEPT；FoundationScene 自动策略路线实际触发两次回顾，55 年确认后隔离槽 `SaveTo/LoadFrom` 往返通过。路线由 `farm-managed-v1` 自动选择，不等于玩家手动经营或用户验收。百年终态粮荒 53 个月、极端通胀 41 个月、粮食库存 0，经济健康未通过。PR #4 的阶段与百年 PlayMode 用例 `1/1` 通过，逐次检查了成功月结和自然月日期变化，独立复审 ACCEPT。证据见 `docs/reports/v1-m08-55-review-user-acceptance-20261119.md` 与 PR #4。
3. M09 窗口拖动：用户于 2026-11-23 报告实体鼠标标题栏拖动通过，面板移动并在松开后保持，底层按钮未误触，页签与关闭可用，停止 Play Mode 后场景未变脏且无新增错误。PR #6 已合并至 `docs/orca-project-workflow`（合并提交 `efe8f761451ef7ecbec71ef4864d1cf39d615b18`），关闭的是 M09 实体输入验收子项，不代表 PR #3 整体验收或项目最终验收。此前 Computer Use 追踪只证明合成 Mouse InputEvent 被 Input System 记录并派发到 EventSystem；按下与终点同帧、`pressPosition == position`，未触发 `BeginDrag`，不能据此判断 Windows 原始输入链或实体鼠标表现。自动回归 PlayMode 3/3、EditMode 2301/2301 通过，代码/报告独立复审 ACCEPT；这些证据与用户实体鼠标验收分别记录。追踪日志为 `Logs/verify/m09-input-trace/trace-20261123-132122.log`，SHA-256 `2846BF02668BA0170ED0CF6C2EB23B2D0AA52706724927962AC94083A473EF81`。详见 [M09 拖动验证报告](https://github.com/panyeung/EU4/blob/panyeung/m09-debug-drag-fix/docs/reports/m09-debug-drag-fix-20261123.md) 与 [PR #6](https://github.com/panyeung/EU4/pull/6)。
4. M08B 经济方向：用户已选择方案 B（增设第二农庄岗位，以 40 人为供给目标），并批准 1000 个固定种子、100 年策略模拟的粮食崩溃率目标为 0%。OpenCode 已受托实现双岗位分配、结算、显示、存档兼容及测试/模拟；模拟结果尚未验证，选择和门槛不等于代码已实现或经济通过。
5. `main` 中 `Assets/Tests/PlayMode` 仍为空；PR #4 分支新增 M08 阶段/百年端到端用例，PR #6 分支新增 M09 PlayMode 拖动用例，均尚未进入 `main`。运行时结果须按对应证据来源分别记录。
6. PR #3 的日本全境地图用户试玩尚未完成。主控已于 2026-11-23/24 在 FoundationScene Play Mode 预验收：清洲→冈崎取消不改变日期与地点；确认后日期 `1560-04-03`、当前位置冈崎，路线筛选与目录滚动可用；关闭全国地图覆盖层后旧地图页仍显示“当前位置：清洲”，切到家族内务再切回“地图与出行”才刷新，因此当时判预验收 RETURN。该缺陷已按根因修复（提交 `696af80`，红灯→绿灯→全量→Play Mode 复验，独立只读复审 ACCEPT），但预验收不等于用户验收通过。用户仍需按 PR #3「日本全境地图用户试玩验收」步骤用实体鼠标滚轮、键盘焦点、目标设备中文显示并确认关闭后旧地图页立即刷新到冈崎；68/68 与本轮 EditMode 结果不替代此项验收。

checkpoint 记录的 M08B D3 基线中，`unmanaged` 1000 种子均未到达 50 年或 100 年。这个结果是已接受的风险基线，不是长期生存通过。SC-021 与 SC-023 已集成进 `main`，但集成事实本身不代表所有相关运行时体验均已验收。

## Git 与协作状态

- Orca 当前为本项目登记主工作树及 M08 相关工作树；`m08-55-year-review-runtime` 含既有用户设置改动，必须保留。每项任务一个写者，Unity 测试串行执行。
- PR #3、#4、#5 仍开放；PR #6 已合并至 `docs/orca-project-workflow`（`efe8f761451ef7ecbec71ef4864d1cf39d615b18`），不等于合并到 `main`。PR #3/#4 目标为 `main`，PR #5 以 `docs/orca-project-workflow` 为基底；用户最终验收前不合并 `main`、不打里程碑 tag。
- 代码实现和测试由指定的实现 agent 负责。只读研究与独立文档复审仍可进行。
- `origin/panyeung/ui-m04-family` 的部分文件改动已在 `main`。直接合并旧分支历史会让场景和 UI 文件冲突。保留该引用，不重放旧历史。
- `checkpoint.md` 顶部原先引用旧分支和旧基线。本次文档分支已改为当前代码基线。合入前由独立复审检查这项更正。
- v1 演示运行说明见 [v1 Windows 演示包](docs/reports/v1-demo-package-20261118.md)。
- 下次启动 Unity 前，重新检查本工程 Editor 进程及 `-projectPath`，并核实 `unity pipeline list` 与 Orca 工作树状态。指定的实现 agent 执行代码实现和测试；独立复审 agent 只读审代码与证据。
