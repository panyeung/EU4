# 影之一族

Unity 6（URP 2D）的战国忍者家族模拟游戏。核心是一座城、一个家族、一个可以结束月份推进的循环。

**当前状态：未完成（PARTIAL）。** 可以构建、可以启动、核心循环可玩，但仍有未闭合的 [MAJOR] 缺口：四个任务工作树未合并进 `main`，用户最终验收未完成，M05 独立复审未通过，F1 城镇设施槽的实现已不在树中（唯一副本在快照提交 `f69295b`，已推送 origin）。逐条清单见下方「已知缺口」，完整证据见 [docs/reports/20261118-completion-report.md](docs/reports/20261118-completion-report.md)。

## 环境要求

- Unity `6000.6.6f1`（revision `d20c9ac777da`，见 `ProjectSettings/ProjectVersion.txt`），渲染管线 URP 2D。
- Windows，构建目标 `StandaloneWindows64`。
- 依赖包已在 `Packages/manifest.json` 内，无需额外安装：URP 17.6.0、Input System 1.20.0、UGUI 2.6.0、Test Framework 1.8.0、2D Sprite/Animation/Tilemap、Timeline 6.6.0、`com.unity.pipeline` 0.8.0-exp.1（Editor 自动化桥）、AI Assistant/Inference（预览包）。
- 本机没有 .NET SDK。`dotnet` 不是构建路径，编译只通过 Unity 完成。

## 如何打开与运行

1. 用 Unity `6000.6.6f1` 打开本目录 `C:\Users\panye\My project`。
2. 打开 `Assets/Scenes/FoundationScene.unity`。工程设置为不自动恢复上次场景，重启 Editor 后必须显式打开。
3. 按 Play。可玩路径：标题页 →「开始新游戏」→「固定三代剧本（SC-013）」→ 营地 HUD（日期、所在、人口、资金、物资）→「结束月份」推进月结 → 名册/编年史 →「地图与出行」进城镇办当地委托。
4. 也可以直接跑成品：`Builds/StandaloneWindows64/My project.exe`（183.8 MB，2026-11-18 构建）。

## 如何构建

构建只能走已开 Editor 的 Pipeline bridge。本工程目录被 Editor 占用时禁止跑 batchmode。构建前先确认操作的是本工程那一个 Editor，别的目录的 Editor 不能混用。

```bash
unity pipeline list                                  # 按 -projectPath 与 PID 对号，取出端口
unity command build --target StandaloneWindows64 \
  --outputPath "Builds/StandaloneWindows64/My project.exe" --confirm true --dry_run true
unity command --timeout 120 build_status --result-only
```

dry-run 返回 `valid=true` 后再去掉 `--dry_run` 实跑。构建期间不要频繁轮询 `build_status`，主线程被占会让请求返回 `Main thread operation timed out`，那不是编译错误。

## 如何跑测试

```bash
unity command run_tests --mode EditMode --detach      # 全量 EditMode
scripts/copy-testresults.sh Logs/verify/<证据目录>     # 跑完立刻复制副本
```

`TestResults.xml` 全机共用（`%LOCALLOW%\DefaultCompany\My project\`），并行跑会互相顶掉，所以测试一律串行，且必须先复制副本再引用。场景校验用 `run_script scripts/verify/RunValidator.cs RunValidator.Run`，结果看 `Logs/SceneValidationReport.txt` 末行 `RESULT: PASS; Errors: 0`。

编译、EditMode、Validator、Play Mode 是四份独立证据，互不替代。

## 目录结构

- `Assets/Scripts/Domain` — 玩法规则，纯 C#，asmdef 设 `noEngineReferences`。
- `Assets/Scripts/Events` — 事件目录与判定。
- `Assets/Scripts/Persistence` — 存档槽读写。
- `Assets/Scripts/Application` — `GameSession`（partial，6 个文件）与调试会话。
- `Assets/Scripts/Presentation` — 全部 UI 面板与控制器。
- `Assets/Scripts/Editor` — 场景/UI 生成器与 updater，改场景只走这里。
- `Assets/Tests/EditMode` — EditMode 测试（全量 2141~2142 项；`Assets/Tests/PlayMode` 为空）。
- `Assets/Resources` — `Events/event-catalog-v1.json`、`Localization/*.json`、生成图。
- `Assets/Scenes/FoundationScene.unity` — 唯一启用的构建场景。
- `docs/` 设计与证据报告，`scripts/` 验证与通知脚本，`Logs/` 运行证据，`Builds/` 构建产物。

## 已知缺口（Known gaps / not done）

| 严重度 | 缺口 | 为什么还在 |
|---|---|---|
| MAJOR | 四个任务工作树未合并进 `main`：`ui-m04-family` 840b2dd、`ui-m05-management` b1d1062、`ui-m06-m07-missions` d57ea7c、`ui-m08-campaign` a25c969 | 本批任务没有集成卡。场景不参与三方合并，需在 `main` 侧重放 updater 并重跑 Validator |
| MAJOR | M00 用户最终验收未完成 | 需用户本人。Play Mode 已到 SC-013 营地 HUD，存读档往返已由后续卡实测通过 |
| MAJOR | M05 独立复审未通过 | 唯一一次独立复审判 FAIL：3 处公开成员零断言，裁切/滚动实现与三条回滚测试只存在于未提交工作区 |
| MAJOR | F1 城镇设施槽的实现不在 `main`，也不在任何任务分支 | 只有快照提交 `f69295b`（已推送 origin）里有 blob `01a0b97`/`6d288c0`。恢复卡已开。恢复前城镇菜单那三个设施槽退回「未开放」占位 |
| MAJOR | `main` 工作树的在途副本在 2026-11-17 21:17:53 被外部重置清掉 | M04 的在途新增文件已不在工作树，但已在 `panyeung/ui-m04-family` 上提交，可从该分支取回，不是永久损失 |
| MINOR | SAVE-3（待决继承跨存读档）只到会话层 | 等价用例 `SuccessionSaveTests.CaptureSave_AtPendingDecision_RoundTripsPhaseAndCandidates` 覆盖，Play Mode 级未单独跑 |
| MINOR | M09 窗口拖动只在合成输入下验过 | 需真实硬件与可交互桌面 |
| MINOR | 构建含 1 条 `uncompiled code changes` 警告 | 构建基于含未提交改动的工作树，产物与 `main` HEAD 不完全对应 |
| MINOR | 成品 Player 约 22 条 URP 后处理着色器被剥离 | Bloom/景深/运动模糊在构建产物中不生效，属着色器变体裁剪 |
| MINOR | 城镇菜单三个 `Facility1..3Text` 节点仍在场景里 | 运行时文本与禁用状态跟随 F1 的实现；实现不在树里时退回「未开放」占位。删节点属场景改动 |
| MINOR | 世界地图屏「NPC 自主性」按钮与「清洲」卡片局部重叠 | 目视记录，未判定是否为设计意图 |
| MINOR | 剧本选择页两条按钮显示名均为「SC-010」 | 内部标识是 SC-013 样例，命名与文案不一致，未改 |
| MINOR | 选 SC-013 后单帧读到「日期未初始化」 | 面板切换期绑定时序，未稳定复现 |
| MINOR | 无音频、无切图、无动画 | 按决策 D5 不进 v1 |
| MINOR | `Assets/Tests/PlayMode` 为空 | v1 运行时验证靠手工 Play Mode 配方，无自动化 |
| MINOR | `Assets/Scenes/` 内有 stray `TestResults_*.xml` | 清理需批准 |
| MINOR | 无 CI/打包流水线 | 唯一构建路径是已开 Editor，v1 只验一次发行构建 |

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
