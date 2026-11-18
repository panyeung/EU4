# 项目审计与缺口清单（Project Audit & Gap List）

生成时间：2026-11-18。审计者：scout（只读，未修改任何项目文件）。
来源：本仓库 `C:\Users\panye\My project` 工作树 + `checkpoint.md` / `docs/实现规格-v1.0.md` / `docs/console-基线归因表.md` / `docs/reports/20261118-playable-milestone.md`。

---

## (a) 项目身份

- 项目路径：`C:\Users\panye\My project`
- Unity 版本：`6000.6.6f1`（`ProjectSettings/ProjectVersion.txt`，revision `d20c9ac777da`）
- 渲染管线：URP 2D（`com.unity.render-pipelines.universal` 17.6.0；`ProjectSettings/GraphicsSettings.asset` 引用 URP 资产；模板场景 `Assets/Settings/Scenes/URP2DSceneTemplate.unity`）
- 关键包：Input System 1.20.0、TextMeshPro（随 UGUI 2.6.0）、URP 17.6.0、2D Sprite/Animation/Tilemap/Tilemap Extras、Timeline 6.6.0、Test Framework 1.8.0、Visual Scripting 1.9.12、AI Assistant/Inference（预览）、Unity Pipeline bridge 0.8.0-exp.1
- 未包含：Cinemachine、Addressables、Localization 包（本地化走 `Resources/Localization/*.json`）、DOTS/ECS
- 程序集：8 个 asmdef（Domain / Events / Persistence / Application / Presentation / Editor / EditModeTests / Welcome）
- 规模：`Assets/Scripts` 305 个 `.cs`；场景 3 个（`FoundationScene`、`SampleScene`、`Settings/Scenes/URP2DSceneTemplate`）；预制体 0 个；EditMode 测试 2141 项全绿（2026-11-18 实测）；`Assets/Tests/PlayMode` 为空目录
- Build Settings：仅启用 `Assets/Scenes/FoundationScene.unity`（GUID `be44f8a18fed65944b8591b78cd6d930`）。`SampleScene.unity` 未启用
- 当前 git：`main` @ `a40cf5b`，工作树含 35 个已跟踪修改 + 39 个未跟踪（与 `panyeung/m00-closeout` `e57dd06` 内容等价，仅行尾差异）；另有 4 个任务工作树未合并

---

## (b) 缺口与缺陷清单（按优先级排序）

### BLOCKER（不能编译/运行/交付）

B1. **DebugSession 在正常开局里被激活，存档被面板主动拒绝**  
文件：`Assets/Scenes/FoundationScene.unity`（`M09DebugModeRoot`，场景里 `m_IsActive: 0`）；`Assets/Scripts/Application/DebugSession.cs`；`Assets/Scripts/Presentation/DebugModePanel.cs`；`Assets/Scripts/Presentation/SaveLoadPanel.cs`（SaveLoadPanel.Status）  
症状：正常开局（SC-010 标准样例）下点「存档」得到 `SaveLoadPanel.Status=调试会话不会写入玩家存档。`，`slotExists=False`；隔离槽 `ISO-Milestone-20261118` 已生效（`isDefault=False`、`titleControllersRedirected=1`），真实槽 SHA-256/mtime 全程未变。  
触发来源本轮未定位（运行期激活，非场景 YAML）。  
证据：`docs/reports/20261118-playable-milestone.md` §6 与最小复现四步。

B2. **SAVE-3 / SAVE-4 存读档验收未执行**  
`SAVE-3`：待处理的继承决定跨存读档存活（选候选人但不确认 → 存 → 改选 → 读，应读回第一次）；`SAVE-4`：空槽位干净拒绝（存储指向空目录后点读档，应明确「无存档」并保持战役）。  
证据：`docs/实现规格-v1.0.md` §S1；`Logs/verify/gate-20261118/save-isolated.md` 未覆盖这两项。  
文件参考：`Assets/Scripts/Persistence/SaveSlotStore.cs`；`Assets/Scripts/Application/GameSession.cs:2914/2933`（SaveTo/LoadFrom）；`Assets/Scripts/Presentation/SaveLoadPanel.cs:76`；`Assets/Scripts/Presentation/TitleScreenController.cs:244`。

B3. **四个任务工作树未合并进 `main`，交付停在「工作树里能跑」**  
`panyeung/ui-m04-family` @ `840b2dd`、`ui-m05-management` @ `b1d1062`、`ui-m06-m07-missions` @ `d57ea7c`、`ui-m08-campaign` @ `a25c969`；主工作树 35 修改 / 39 未跟踪未提交。  
证据：`checkpoint.md` §5；`docs/实现规格-v1.0.md` §S2–S4、风险「场景冲突」。  
注意：场景不参与三方合并，需在 main 侧重放 updater 并跑 Validator。

B4. **M05 独立复审未完成**（`.orchestrate` 里的 `dispatched` 单元随 Orca 停用失效）  
复审需重新派给 `tester`，结论走 `kanban_request_changes` 或通过。  
证据：`docs/实现规格-v1.0.md` §S3/T7；`checkpoint.md` §4（M05 行）。

B5. **M00 用户最终验收未完成**（Play Mode 已到 SC-013 营地 HUD，隔离槽存读档 SAVE-1～SAVE-4 中 SAVE-1/SAVE-2 已过，SAVE-3/SAVE-4 未验）。  
证据：`checkpoint.md` §4 M00 行；`docs/reports/20261118-playable-milestone.md`。

B6. **M09 窗口拖动输入路径未在真实硬件复验**  
M09 代码与 2092 项测试已绿，但窗口拖动输入路径在真机上可能返回 `synthetic_input`，需如实标 BLOCKED 或通过。  
证据：`docs/实现规格-v1.0.md` §S5/T16。

### FEATURE（玩法未完成）

F1. **城镇设施全部为占位「未开放」**  
文件：`Assets/Scripts/Presentation/WorldMapController.cs` ~139/581/636（`facilityTexts` 循环写 `"{label}：未开放"` 并 `enabled=false`；标签固定为 `设施一/二/三`）。  
城镇页已能列当地委托（`TownLocalActions`，`Assets/Scripts/Presentation/TownLocalActions.cs`），但设施入口未接入任何功能。  
证据：`docs/实现规格-v1.0.md` §5.3（契约）。

F2. **SC-014 / SC-015 / SC-016 三条跨代链只有事件目录与中文文本，无新运行时**  
规格明确 v1 不为它们写新运行时（引擎按现有允许列表承载），但作为 [FEATURE] 留痕：若 v1 验收要覆盖它们，需确认。  
证据：`docs/实现规格-v1.0.md` §2 非目标；`checkpoint.md` 决策记录（SC-014/SC-015/SC-016）。

F3. **战术系统正式投入未开始**（checkpoint 1.15：须在 M05 验收后）  
现有 `TacticalMissionPanel` 与 `M6Tactical*` UI 已在场景中（inactive），但战术 AI/完整地图/动画等按范围红线暂缓。  
证据：`checkpoint.md` §1.15；`Assets/Scripts/Presentation/TacticalMissionPanel.cs`。

F4. **长期模拟存活样本为 0**（100 年 × 1000 种子：灭族 605、待继承 395、完成 0；`farm-managed-v1` 为完成 870）  
按 D3 口径定为「当前基线」而非生存通过；不是缺陷，但 v1 发布口径需写明。  
证据：`checkpoint.md` §1.14；`docs/实现规格-v1.0.md` D3。

### POLISH（UX/音频/设置/清理）

P1. **无音频资源**：`Assets` 下 0 个 `.wav/.mp3/.ogg`，无 Audio 目录。按 D5 不进 v1，但作为 polish 留痕。  
证据：`docs/实现规格-v1.0.md` D5。

P2. **美术为生成图，无切图/动画/音效**：约 14 张 png，字号/行高/间距未定义，5 项 UI 决定未拍板（`ui设计规划.md` §11）。  
证据：`docs/实现规格-v1.0.md` D5。

P3. **PlayMode 测试目录为空**（`Assets/Tests/PlayMode` 0 文件）：v1 运行时验证靠手工 Play Mode 配方，无自动化。规格列为非目标，但作为 polish 留痕。  
证据：`docs/实现规格-v1.0.md` §2 非目标。

P4. ** stray 测试结果 XML 在 Assets 内**：`Assets/Scenes/TestResults_20261108_072929.xml`（+ `.meta`）与 `TestResults_20261108_114927.xml`；应移出 Assets 或纳管，避免污染工程。  
证据：`docs/实现规格-v1.0.md` §S5/T18。

P5. ** leftover 模板资产**：`Assets/SampleScene.unity`（未启用）、`Assets/Welcome/Unity.2D.Welcome`（asmdef）、`Assets/Settings/Scenes/URP2DSceneTemplate.unity`。  
证据：本审计。

P6. **Console 基线 7 条 error 全为工具噪声 / 负向测试预期输出**，无真实缺陷（seq 17–21 pipeline 子命令拼错；seq 66–67 `HistoryLedger` 负向测试预期输出）。归因表已落盘，但需每次跑证据前记录基线。  
证据：`docs/console-基线归因表.md`。

P7. **无 CI / 打包脚本**：本机无 .NET SDK，唯一构建路径是已开 Editor + Pipeline bridge；v1 只验一次发行构建入口，不建流水线。  
证据：`docs/实现规格-v1.0.md` §2 非目标。

P8. **M09 窗口拖动输入路径真机未复验**（与 B6 重叠， polish 侧记录）。  

---

## 静态检查结论（编译/结构）

- 无 `TODO/FIXME/HACK/XXX` 注释；无 `NotImplementedException`；无空 MonoBehaviour stub（检查了最小文件 `DemoSummaryPanel`/`RankPanel`，均为完整实现）。
- 无 `UnityEditor` 引用进入运行时脚本（Editor API 仅在 `Assets/Scripts/Editor`）。
- 无缺失脚本引用（`.unity`/`.prefab` 中 `m_Script` 全为合法 fileID 11500000 GUID；guid 全零仅出现在内置字体 `m_Font`，属正常）。
- 重复类名均为合法：`GameSession` 为 `partial`（6 文件）；`OwnedButton`/`UpdateResult`/`ObjectSnapshot` 为各宿主类内的 `private` 嵌套类。
- Domain asmdef `noEngineReferences: true`，Domain 目录 0 处 `UnityEngine` 引用，边界干净。
- Editor.log 最近一次编译：0 个 `error CS`、0 个 `warning CS`（仅 licensing/AI tracing 噪声）。
- 场景 Validator：`RESULT: PASS; Errors: 0`（`Logs/SceneValidationReport.txt` 末行）。
- EditMode：2141/2141 通过（2026-11-18 实测，副本 `Logs/verify/20261118-milestone/TestResults.xml`）。

---

## (c) 推荐（一段话）

要让项目算作「完成」，必须先闭合 v1 定义（`docs/实现规格-v1.0.md` §3 七条）里目前唯一 BLOCKED 的第 6 条——定位并移除正常开局里被激活的 DebugSession，使存档按钮离开 `未存档` 并完成「保存 → 推进一天 → 读回」往返（B1、B2），同时把四个任务工作树按 M04→M05→M06/M07→M08 顺序合并进 `main` 并跑全量回归（B3），完成 M05 复审（B4）与 M00 用户最终验收（B5）；在此基础上，城镇设施占位（F1）与跨代链/战术/模拟基线（F2–F4）按规格的非目标与 D3 口径如实标出，音频/美术/自动化/模板清理（P1–P8）留到 v1.1，不阻塞 v1 交付。v1 的通过标准不是「功能齐全」，而是 `main` 上一个干净提交、编译零错误、全量 EditMode 全绿、Validator 零错误、Play Mode 走完标题→选剧本→营地 HUD→月结→一次家主更替、存读档在同一会话内往返一致——其中存读档往返是当前唯一未闭合的硬门槛。

---

## 未检查项（预算/范围限制）

- 未运行 Unity 编译（Editor 已开，但本审计按只读静态方式执行，未触发 recompile）；编译状态取自最近 Editor.log 与 2026-11-18 实测记录。
- 未检查 `Packages/manifest.json` 中 AI Assistant/Inference 预览包的运行时兼容性；未检查 Addressables/Cinemachine（本项目未使用）。
- 未打开 `ui设计规划.md` §10 的素材许可登记逐条核对。
- 未检查四个任务工作树内部的未提交改动（仅记录分支与提交）。

## 检索/检查记录

- 关键词/命令：`find Assets`（结构）、`grep TODO/FIXME/HACK`、`grep NotImplementedException`、`grep UnityEditor`（runtime）、`grep class/struct/enum`（重复名）、`grep m_Script`（缺失脚本）、`grep Assets/Scenes/*.unity`（场景引用）、`find audio`、`git status/worktree`、读取 `checkpoint.md`、`docs/实现规格-v1.0.md`、`docs/console-基线归因表.md`、`docs/reports/20261118-playable-milestone.md`、`ProjectSettings/EditorBuildSettings.asset`、`Packages/manifest.json`、`ProjectSettings/ProjectVersion.txt`。
- 主要来源文件：`checkpoint.md`（§1.13/§4/§5）、`docs/实现规格-v1.0.md`（§2/§3/§6/§7）、`docs/reports/20261118-playable-milestone.md`、`docs/console-基线归因表.md`、`Logs/SceneValidationReport.txt`、`Logs/Editor.log`。
