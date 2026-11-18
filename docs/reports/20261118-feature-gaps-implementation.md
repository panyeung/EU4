# FEATURE 缺口处置报告（审计 F1–F4）— 2026-11-18

卡：`t_343f7b6d`「Implement the missing core gameplay/feature gaps identified in the audit」
输入：`docs/reports/20261118-project-audit-gaplist.md` §(b) FEATURE F1–F4
执行：coden（只改 C# 与文档；场景与预制体按 AGENTS.md 归主控／集成 agent）

## 1. 逐条处置

| 条目 | 审计描述 | 处置 | 依据与证据 |
|---|---|---|---|
| F1 | 城镇设施全部为占位「未开放」 | **已实现（代码侧）**：设施槽不再写假入口，内容改为真实的当地委托；城镇实际设施界面沿用已实现的 `TownOverviewPanel` | 见 §2 |
| F2 | SC-014 / SC-015 / SC-016 三条跨代链只有事件目录与中文文本 | **按设计已具备，无需新运行时** | 三条链在 `Assets/Resources/Events/event-catalog-v1.json`（`chain.sc013.refugee-legacy` / `family-letters` / `commission-legacy`），事件 ID 全部 `event.sc013.*`，由 `M2FixedScenario.IsScenarioEvent` 做剧本门禁；权重与优先级已由 `Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs` 断言（如 `event.sc013.family-letters.first|chain.sc013.family-letters|6|1|2|1`）。规格 §2 非目标与 `checkpoint.md` 决策记录（2026-11-03）明确 v1 不为它们写新运行时。 |
| F3 | 战术系统正式投入未开始 | **延后（设计门槛，不在本卡可决定范围）** | `checkpoint.md` §1.15 把战术正式投入限定在 M05 验收之后，而 M05 复审尚未通过（审计 B4）；范围红线同时暂缓战术 AI／完整地图／动画。现有 `TacticalMissionPanel`（潜入密令）已可玩，属既有范围。 |
| F4 | 长期模拟存活样本为 0 | **口径已写明，无代码缺口** | D3 已拍板「定为 v1 口径，并在文档里写明这不是生存通过」；`docs/实现规格-v1.0.md` 第 176／230 行与 `checkpoint.md` §1.14（50/100 年存活样本为零，不是通过结论）已落盘。 |

## 2. F1 的实现

### 2.1 民间行为（一句话）

城镇菜单的设施槽只列当前城镇真实可办理的当地委托；有城镇总览页时槽位留空，因为设施内容由总览页承担，界面上不再出现「设施一/二/三：未开放」这类没有功能支撑的假入口。

### 2.2 为什么不是「新做一套设施」

- `TownMenuRoot/TownOverviewRoot` 是 `UpdateTownOverview` 生成的全屏不透明页（`anchorMin 0,0`、`anchorMax 1,1`，底色 alpha 0.98）。三个 `Facility1..3Text` 位于其下层（`anchoredPosition y = 38 / 0 / -38`，尺寸 520×30），主路径下被页面完全覆盖，本来就不可见。
- 城镇的真实功能入口已经存在并被验证：`TownOverviewPanel` 的插画／地点重点／当地人物／当地委托卡／名册／全部委托，加 `TownLocalActions` 五类活动（`docs/reports/20261118-playable-milestone.md` 第 7 条 PASS，清洲四条活动卡可点开）。
- `checkpoint.md` §2 决策记录（2026-10-29）与 §3.2「只有数据或占位」写明设施为锁定占位；`ui设计规划.md`「空与未知」写明不显示没有功能的设施。因此把设施做成带名字的建筑清单会违反既有设计决定。

### 2.3 代码改动

`Assets/Scripts/Presentation/WorldMapController.cs`

- 新增 `RefreshFacilitySlots()`：有总览页或没有会话时槽位清空并禁用；没有总览页的旧场景（M2 生成的菜单）按 `TownLocalActions.GetAvailable` 顺序填真实标签，空缺槽清空并禁用。
- `OpenTownMenu()` 末尾调用它，删除原来写 `$"{label}：未开放"` 并强制 `enabled = false` 的循环。
- 同步更新类头、`OpenTownMenu`、`GetFacilityTexts` 的 XML 注释，避免注释继续描述已删除的占位行为。

`Assets/Tests/EditMode/WorldMapUiTests.cs`

- `TownMenu_FacilitiesLabeledNotOpenAndDisabled` → `TownMenu_FacilitySlotsListRealTownEntries`：断言槽位不再含「未开放」与「设施」占位名，前 N 槽等于 `TownLocalActions` 的真实标签且启用，其余槽为空且禁用。

## 3. 改动文件

- `Assets/Scripts/Presentation/WorldMapController.cs`（F1）
- `Assets/Tests/EditMode/WorldMapUiTests.cs`（F1 用例改写）
- `docs/reports/20261118-feature-gaps-implementation.md`（本文件）

未改动场景、预制体、`ProjectSettings`、依赖与 `checkpoint.md`。

## 4. 验证

| 项 | 命令 | 结果 |
|---|---|---|
| 编译 | `unity command recompile` → `recompile_status`（端口 7800，PID 96584） | `status: completed`，`failed: false`，`errors: []`，`compilationFailed: false` |
| 影响面用例 | `unity command run_tests --mode EditMode --filter WorldMapUiTests --detach` | 20/20 passed（`06:13:52Z`–`06:13:53Z`），含新用例 `TownMenu_FacilitySlotsListRealTownEntries` result=`Passed` |
| 影响面用例 | `unity command run_tests --mode EditMode --filter TownActivityRoutingTests --detach` | 7/7 passed（`06:23:18Z`–`06:23:19Z`），含新用例 `TownMenu_FacilitySlotsStayEmptyWhenTheTownPageRenders` |
| 全量回归 | `unity command run_tests --mode EditMode --detach` | `testcasecount=2142 passed=2142 failed=0 skipped=0 result=Passed`（`06:23:57Z`–`06:28:10Z`）。副本：`Logs/verify/20261118-features/TestResults-editmode-full-2142.xml` |
| Console | `unity command console --level error --tail 40` | 14 条 error，全部可归因，无游戏运行期错误：8 条 `家族编年史记录写入失败（家族收养／家族婚姻）` 是 `FamilyFormationSessionTests` 负向用例的预期输出（`docs/console-基线归因表.md` 已定性）；6 条 `Pipeline: unhandled error serving POST /api/exec`／`Failed to handle /api/exec request` 是本轮轮询 `test_status` 撞上 30 秒客户端超时后的工具侧断连（seq 326–331），不是游戏代码 |
| Play Mode | 未跑 | 本轮只改城镇菜单设施槽的文本内容，且主场景下这三个槽被 `TownOverviewRoot` 全屏页覆盖、本来就不可见；城镇页运行时行为未改（`docs/reports/20261118-playable-milestone.md` 第 7 条已 PASS）。另外同一 Editor 上并行卡 `t_dd30ab75` 正在做 Play Mode 复现，按 AGENTS.md「一张卡一个写者」不叠加第二个 Play Mode 会话 |
| 场景 Validator | 未跑 | 本轮未改场景、预制体或 `ProjectSettings`，`FoundationScene` 的 Validator 结论沿用 2026-11-18 的 `RESULT: PASS; Errors: 0` |

## 5. 核心循环（供 tester 验收）

标题页「开始新游戏」→ 选剧本（SC-010 标准样例／SC-013 固定三代）→ 进营地 HUD（日期、所在、人口、资金、物资），用「结束月份」推进月结，名册看身份／能力／关系、编年史看已发生事件，完成一次家主更替（`SuccessionHead` 换人且世界不重置）；在「地图与出行」里旅行到城镇，城镇页按当地状态列出「当地委托」卡（补给短缺／密信／市集账目／护送委托／潜入密令）并可点开办理。

存读档往返（存档 → 推进一天 → 读档回到存档时）是 v1 唯一硬门槛，当前由并行卡处理 `DebugSession` 激活缺陷（审计 B1）；本条不在本卡范围。

## 6. 未决问题

1. 场景里三个 `Facility1..3Text` 节点仍然存在（运行时已清空文本并禁用，编辑器静态场景里仍是 M2 遗留的占位字符串）。删除场景节点属场景改动，按 AGENTS.md 归主控／集成 agent；若删，需同步 `Assets/Scripts/Editor/WorldMapLayout.cs`、`Assets/Scripts/Editor/UpdateM2WorldMapLayout.cs` 的子节点清单与 `Assets/Scripts/Editor/CreateM2WorldMap.cs` 的生成逻辑。
2. 设施若要真做成一栋栋可交互建筑（如训练场、市场），需要先定义规则与资源，属新玩法设计，不在 v1 非目标之内。
3. `Assets/Tests/EditMode/TownActivityRoutingTests.cs` 在工作树里本来就是未跟踪文件（M05 未提交工作），本轮新增的用例落在该文件内；提交时需与它的既有改动一起处理，别只提交半个文件。
4. 本轮轮询造成 6 条 Pipeline 断连日志，`docs/console-基线归因表.md` 若要更新基线，需把这 6 条按「工具侧」登记。

