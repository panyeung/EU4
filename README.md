# 影之一族

Unity 6（URP 2D）的战国忍者家族模拟游戏。核心是一座城、一个家族、一个可以结束月份推进的循环。

**当前状态：代码与集成都已收口，只剩用户最终验收（PARTIAL）。** `main` 的代码提交是 `2bca9d1`（收口卡其后只追加文档提交），五个 UI 分支全部并入，全量 EditMode `2214/2214`、场景 Validator `PASS; Errors: 0`、独立复审 PASS、成品 Player 冒烟通过。仍然没闭合的是**用户本人的最终验收**（M00），另有 M08 战役回顾面板运行时打开与 M09 真机窗口拖动两条运行时未验证项。逐条见下方「已知缺口」，完整证据见 [docs/reports/20261118-completion-report.md](docs/reports/20261118-completion-report.md)。

## 环境要求

- Unity `6000.6.6f1`（revision `d20c9ac777da`，见 `ProjectSettings/ProjectVersion.txt`），渲染管线 URP 2D。
- Windows，构建目标 `StandaloneWindows64`。
- 依赖包已在 `Packages/manifest.json` 内，无需额外安装：URP 17.6.0、Input System 1.20.0、UGUI 2.6.0、Test Framework 1.8.0、2D Sprite/Animation/Tilemap、Timeline 6.6.0、`com.unity.pipeline` 0.8.0-exp.1（Editor 自动化桥）、AI Assistant/Inference（预览包）。
- 本机没有 .NET SDK。`dotnet` 不是构建路径，编译只通过 Unity 完成。

## 如何打开与运行

1. 用 Unity `6000.6.6f1` 打开本目录 `C:\Users\panye\My project`。`unity open` 也可以，它能拉起本工程并等到 pipeline 可连。
2. 打开 `Assets/Scenes/FoundationScene.unity`。工程设置为不自动恢复上次场景，重启 Editor 后必须显式打开。
3. 按 Play。可玩路径：标题页 →「开始新游戏」→「固定三代剧本（SC-013）」→ 营地 HUD（日期、所在、人口、资金、物资）→「结束月份」推进月结 → 名册/编年史 →「地图与出行」进城镇办当地委托。
4. 也可以直接跑成品：`Builds/Demo-v1-2bca9d1-r2/Demo-v1-2bca9d1-r2.exe`（源提交 `2bca9d1`，2026-11-18 构建，目录 183,882,557 B / 218 个文件）。`Builds/` 在 `.gitignore` 里，是本地产物，不入库。

## 如何构建

构建只能走已开 Editor 的 Pipeline bridge。本工程目录被 Editor 占用时禁止跑 batchmode。构建前先确认操作的是本工程那一个 Editor，别的目录的 Editor 不能混用。

```bash
unity pipeline list                                  # 按 -projectPath 与 PID 对号，取出端口
unity command build --target StandaloneWindows64 \
  --outputPath "Builds/StandaloneWindows64/My project.exe" --confirm true --dry_run true
unity command --timeout 120 build_status --result-only
```

dry-run 返回 `valid=true` 后再去掉 `--dry_run` 实跑。构建期间不要频繁轮询 `build_status`，主线程被占会让请求返回 `Main thread operation timed out`，那不是编译错误。

构建 post-process 会删 `Assets/Resources/PerformanceTest*.json` 与其 `.meta`（`com.unity.test-framework.performance` 的 `TestRunBuilder.Cleanup()`）。构建后 `git status` 出现这 4 项删除属已知副作用，`git checkout` 恢复即可。

## 如何跑测试

```bash
unity command run_tests --mode EditMode --detach      # 全量 EditMode
scripts/copy-testresults.sh Logs/verify/<证据目录>     # 跑完立刻复制副本
```

`TestResults.xml` 全机共用（`%LOCALLOW%\DefaultCompany\My project\`），并行跑会互相顶掉，所以测试一律串行，且必须先复制副本再引用。场景校验用 `run_script scripts/verify/RunValidator.cs RunValidator.Run`，结果看 `Logs/SceneValidationReport.txt` 末行 `RESULT: PASS; Errors: 0`。Validator 必须在 `playMode=stopped` 且场景重载后跑，`run_tests` 必须等 `compiling=false`。

编译、EditMode、Validator、Play Mode 是四份独立证据，互不替代。当前全量基线是 `2214/2214`。

## 目录结构

- `Assets/Scripts/Domain` — 玩法规则，纯 C#，asmdef 设 `noEngineReferences`。
- `Assets/Scripts/Events` — 事件目录与判定。
- `Assets/Scripts/Persistence` — 存档槽读写。
- `Assets/Scripts/Application` — `GameSession`（partial，6 个文件）与调试会话。
- `Assets/Scripts/Presentation` — 全部 UI 面板与控制器。
- `Assets/Scripts/Editor` — 场景/UI 生成器与 updater，改场景只走这里；`CanvasLayerOrder.cs` 是唯一的页面层次序来源。
- `Assets/Tests/EditMode` — EditMode 测试（全量 2214 项；`Assets/Tests/PlayMode` 为空）。
- `Assets/Resources` — `Events/event-catalog-v1.json`、`Localization/*.json`、生成图。
- `Assets/Scenes/FoundationScene.unity` — 唯一启用的构建场景。
- `docs/` 设计与证据报告，`scripts/` 验证与通知脚本，`Logs/` 运行证据，`Builds/` 构建产物（忽略）。

## 已闭环的三条 MAJOR

| 原缺口 | 实际结论 | 依据 |
|---|---|---|
| 四个任务工作树未合并进 `main` | **已合并**。`panyeung/m00-closeout`、`ui-m04-family`、`ui-m05-management`、`ui-m06-m07-missions`、`ui-m08-campaign` 逐个 `git merge-base --is-ancestor` 为真；`main`、`origin/main`、`integration/v1-20261118` 三点同为 `2bca9d1`，只留一个工作树 | 收口卡 `t_080705e2` 亲自跑 `git log`／`merge-base`／`worktree list` |
| M05 独立复审未通过 | **复审 PASS**。C1–C6 全过：三组公开成员有真实行为断言，`ScrollRect` 裁切与三条回滚用例已在树中，`skipped=0` | `t_122d4d5b` 复审结论 |
| F1 城镇设施槽不在 `main` | **已恢复**。`WorldMapController.cs` 有 `RefreshFacilitySlots`（4 处命中），两条用例 `TownMenu_FacilitySlotsListRealTownEntries`、`TownMenu_FacilitySlotsStayEmptyWhenTheTownPageRenders` 在 2214 全量里均为 Passed | 收口卡 `git grep` 与 `TestResults.xml` 逐条读回 |

## 已知缺口（Known gaps / not done）

| 严重度 | 缺口 | 为什么还在 |
|---|---|---|
| MAJOR | M00 用户最终验收未完成 | 验收权在用户。证据齐了，只差人过一遍成品 |
| MINOR | M08 战役回顾面板的运行时打开未验证 | 面板只在真实 55 年阶段回顾或 100 年结局待处理时显示，需一条长周期演示路线；按裁定不靠调试跳日期取证 |
| MINOR | M09 窗口拖动只在合成输入下验过 | 需真实硬件与可交互桌面 |
| MINOR | `Assets/Tests/PlayMode` 为空 | 运行时验证靠手工 Play Mode 配方，规格列为非目标 |
| MINOR | 成品约 22 条 URP 后处理着色器被剥离 | Bloom/景深/运动模糊在构建产物中不生效，属着色器变体裁剪 |
| MINOR | 城镇菜单三个 `Facility1..3Text` 场景节点仍在 | 内容由 `RefreshFacilitySlots` 决定，无总览页时按设计清空禁用；删节点属场景改动 |
| MINOR | 世界地图屏「NPC 自主性」按钮与「清洲」卡片局部重叠 | 目视记录，未判定是否为设计意图 |
| MINOR | 剧本选择页两条按钮显示名均为「SC-010」 | 内部标识是 SC-013 样例，命名与文案不一致，未改 |
| MINOR | 选 SC-013 后单帧读到「日期未初始化」 | 面板切换期绑定时序，未稳定复现 |
| MINOR | 无音频、无切图、无动画 | 按决策 D5 不进 v1 |
| MINOR | `Assets/Scenes/` 内有 stray `TestResults_*.xml` | 清理需批准 |
| MINOR | 无 CI/打包流水线 | 唯一构建路径是已开 Editor，v1 只验一次发行构建 |
| MINOR | SC-021 场景集成与 SC-023 仍未合入 | 分别在 `panyeung/sc021-scene-integration` 与 `origin/panyeung/sc023-seclusion` |
| 说明 | 构建只留一条警告「No RuntimePipelineConfig asset found」 | Pipeline 包自带提示，`Pipeline will be disabled in Player builds`，不影响游戏本体 |

明确不做、不算缺口的两条：SC-014/015/016 三条跨代链按规格非目标不写新运行时（事件目录与剧本门禁已具备）；长期模拟存活样本为 0 是决策 D3 已拍板的 v1 口径，不是生存通过。

## 文档

- `checkpoint.md` — 设计规划、里程碑、决策记录、当前状态与下一步。
- `docs/实现规格-v1.0.md` — v1 的通过标准与任务清单，接手先读这份。
- `docs/reports/20261118-completion-report.md` — 完成情况与逐条证据。
- `ui设计规划.md` — UI 规则与美术资产登记。
- `docs/console-基线归因表.md` — Console 里每一条 error 的归因。
- `AGENTS.md` — 代理协作规则，不属于设计文档。

## 协作注意

同一个工作树、同一个 Unity 实例，同一时刻只能有一个写者。测试结果文件全机共用，必须串行跑并立刻复制副本。场景、预制体与 `ProjectSettings` 只由主控或指定集成 agent 修改。
