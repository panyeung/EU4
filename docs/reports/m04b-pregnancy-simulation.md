# M04B 动态家族与逐月怀孕模拟实现记录

**日期：2026-11-10**
**状态：实现、编译、全量 EditMode 与 lead Play Mode/存读档已验证；用户最终验收待办。**
**依据：** [设计稿](../superpowers/specs/2026-11-10-m04b-dynamic-birth-design.md)、[实现计划](../superpowers/plans/2026-11-10-m04b-dynamic-birth.md)。

## 实现范围

- `PersonGender`（`Unspecified` / `Male` / `Female`）成为不可变的 `PersonState` 事实；两参数构造函数保持源兼容并默认 `Unspecified`。
- `PersonGenderCatalog` 只登记项目已授权剧本人物的稳定 ID。全部固定剧本（SC-010 标准/危机、SC-013、SC-020、SC-021、SC-022、SC-023、SC-024）在创建人物时显式设置性别；危机收养人物显式设为男性。
- `FamilyPregnancyRecord` / `FamilyPregnancyState`：按怀孕者稳定 ID 排序；同一女性、同一配偶对最多一条未完成孕期；受孕日与预产日必须正好相隔九个月。
- `FamilyPregnancyRules`：仅使用 `RandomStreamKind.WorldSimulation`；按稳定配偶对顺序对每个合资格且未怀孕的正式配偶对做独立 3% 月判定；女性 16–40、男性 16–60（含边界），双方在世且为当前家族成员；不合资格或已怀孕的配偶对不消耗随机数。
- `FamilyBirthNaming`：由排序后的父母 ID 与日期锚点生成稳定 ID；原创男性/女性短名表；仅在重名/重 ID 时加稳定序号。
- `FamilyBirthSettlement`：纯月计划，包含孕期开始/完成/取消、到期新生儿事实（50/50 性别、双亲、全手足）和一条家族新生编年史事实。全手足只对**同时具备双亲**的既有子女建立；M2 固定图中 Nobumasa 仅有 Masatsuna 一个已记录父母，因此测试明确断言 Masatsuna 为全手足而 Nobumasa 不是。
- `GameSession.TryAdvanceToNextMonth`：在既有月结事务内消费 `WorldSimulation` 随机流，把计划应用到从 `SuccessionSaveData` 深拷贝的工作快照；只有日期跨月且逐日更新完成后才提交。新生儿通过既有初始化路径获得增长行、每日行动行、`DirectKin` 成员身份、双亲 `ParentChild`、已有共同父母子女的 `Sibling` 和一条「家族新生」编年史；不加入 M03 试点、产业、研究或教育计划。新生儿每日行动行使用月末同步后的当前地点。
- 失败回滚：日期、产业/研究/成长/每日行动/家训、随机流、编年史、人物/成员/亲属图/性别/孕期全部恢复；`MonthSettlementFaultInjection` 在出生状态提交后触发，用于证明完整回滚，重试与无故障路径一致。若事务快照本身无法重建，错误信息明确报告家族状态可能不一致，不再声称完整回滚。
- `FamilyRosterPanel`：从 `GameSession.FamilyPregnancies` 只读投影「怀孕 · 第 n/9 月 · 预计 YYYY-MM」。卡片保持既有 196×88 布局几何；孕者卡片启用 `wrap`、`truncate` 与自动缩放（min 8），完整状态在标准卡片内换行显示，不改变家谱路由几何，非孕者卡片保持原有排版。未新增场景对象或根节点。

## 存档

- Schema `v12 → v13`；`rulesVersion` 保持 `7`。
- `SuccessionPersonSaveEntry.gender` 保存 `PersonGender` 数值；`SuccessionSaveData.pregnancies` 保存孕期日期字段。
- v12 迁移先把所有人物性别置为 `Unspecified`，再仅按 `PersonGenderCatalog` 解析已授权稳定 ID，绝不从姓名或亲属关系推断；孕期列表初始化为空。同一前置步骤也应用于 v1–v11 的所有迁移分支，未知身份保持 `Unspecified`。
- v13 校验拒绝未定义性别枚举、未知人物引用、非成员/已故怀孕者、无效或非九月孕期、重复女性/配偶对及不稳定排序；允许已故但仍是家族成员的另一位父母保留亲子事实。
- `SaveDataValidator.CurrentVersion` 与 `SaveData.CurrentGameSchemaVersion` 均为 `13`。

## 新增/修改测试（聚焦）

- 新增：`PersonGenderTests`、`FamilyPregnancyStateTests`、`FamilyPregnancyRulesTests`、`FamilyBirthNamingTests`、`FamilyPregnancySessionTests`。
- 扩展：`SuccessionSaveTests`（性别与孕期往返）、`GameSessionSaveTests`（在途孕期往返）、`SaveDataMigratorTests`（v12→v13、v9/v6→v13、仅目录解析、空孕期初始化）、`SaveDataValidatorTests`（性别、未知/非成员/已故、日期、重复、排序、缺失孕期块）、`FamilyRosterPanelTests`（怀孕状态只出现在怀孕者卡片、标准 196×88 卡片内 wrap/truncate/自动缩放）。
- schema 升级的集成修正：`NpcAutonomySessionTests.WithVersion` 由硬编码 `12` 改为 `SaveDataValidator.CurrentVersion`，否则旧的版本回退测试在 v13 后不再实际改写版本。

## 验证状态（分别记录）

- 编译：现有 Unity `6000.6.6f1` Editor（PID `126040`，`-projectPath C:\Users\panye\My project`）于 2026-11-10 10:53 触发刷新，`Logs/Editor.log` 记录 `*** Tundra build success (4.15 seconds), 18 items updated, 1975 evaluated`；该区间无 `error CS`，仅既有 `ReferenceGalleryNavigator.cs` 的 CS0618 警告（非 M04B 文件）。
- EditMode：全量 `1446/1446` 通过，`0` 失败、`0` 跳过；结果 XML `C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\TestResults.xml`，`test-run` 统计 `total="1446" passed="1446" failed="0" skipped="0"`，写入时间 2026-11-10 10:55:53。此前四项失败均复跑通过：`DuePregnancy_DoesNotReconceiveInTheSameSettlement`、`BirthMonth_DoesNotReconceiveTheSamePair`、`InjectedSettlementFailure_RestoresEveryFamilyFactAndRetryMatchesControl`、`SpouseRoute_GuaranteedOuterRouteWhenAllStandardCandidatesAreBlocked`。前三项原因为测试使用 `1559` 年受孕日，低于 `GameDate.MinYear=1560`；已改为有效日期并在会话测试中先推进到 1561 年再设置孕期，未修改日历规则。
- Console：该次全量运行的 `Logs/Editor.log` 区间记录 `2` 条 `LogError` 与 `29` 条 `LogWarning`；两条 error 均为既有 `FamilyFormationSessionTests` 故障注入路径的预期 `LogAssert` 消息（婚姻/收养编年史写入失败），对应测试通过，不是 M04B 回归。Console 未在测试前手动清零，因此不声称历史累计计数为零。
- 第四个失败的定位：`FamilyTreeLayoutTests.SpouseRoute_GuaranteedOuterRouteWhenAllStandardCandidatesAreBlocked` 是 M04B 一度将 `FamilyTreeLayout.NodeHeight` 从 88 改为 112 造成的外围车道间距变化；该生产改动已完全回退（`FamilyTreeLayout.cs` 与 HEAD 一致），复跑通过。M04B 最终不修改家谱布局几何，孕期状态在标准 196×88 卡片内使用 wrap/truncate/best-fit 投影。没有改动其他家谱生产代码。
- `FoundationSceneValidator`：没有在正确的 `FoundationScene` 上得到有效结果。本轮一次误触运行了“Create Foundation Scene”，Validator 随后对被生成器替换的最小场景报告 `FAIL; Errors: 41`；该结果不代表项目原场景验证。发现后已将磁盘 `Assets/Scenes/FoundationScene.unity` 恢复为 `HEAD` 内容并在 Editor 重新载入；正确场景 Validator 仍待运行。
- Play Mode / 存读档（lead 于 2026-11-10 执行）：从 SC-022 固定剧本开始，在备份测试存档后注入一条怀孕记录；家族名册显示「怀孕 · 第 1/9 月」与「第 9/9 月」，保存并重新载入后仍在途孕期保持。结束月份推进到 `1561-02-01` 后产生一名新生儿；再次保存并载入后孕期已清除、人物总数为 `12`，随后退出 Play Mode。Play Mode 结束后原始存档与 `.bak` 均已恢复，SHA-256 与备份一致；截图未保留，`FoundationSceneValidator` 未运行，尚无人工里程碑验收。
- 因此本记录声称编译、全量 EditMode 与 lead Play Mode/存读档通过；`FoundationSceneValidator` 与用户最终里程碑验收仍未完成。

## 已知边界与风险

- 3% 月判定会在所有合资格正式配偶对上生效。旧存档中年龄在范围内的配偶可能在首次月结后开始孕期；这是批准规则，不是迁移补算。
- 旧剧本多数配偶年龄超出范围或缺失性别，因此不会自动受孕；未登记性别的自定义人物保持可玩但不参与受孕。
- 不包含流产、并发症、婴幼儿死亡、遗传、多胎、NPC 自主婚姻或孕期编辑。
- 2026-11-10 EditMode 的第四个失败 `FamilyTreeLayoutTests.SpouseRoute_GuaranteedOuterRouteWhenAllStandardCandidatesAreBlocked` 经定位是 M04B 曾把 `FamilyTreeLayout.NodeHeight` 从 88 改为 112 造成的外围车道间距变化；该生产改动已回退，`FamilyTreeLayout.cs` 恢复为 HEAD。M04B 最终不修改家谱布局几何，孕期状态改用既有卡片的 wrap/truncate/best-fit 投影。该失败不应再归因于既有家谱代码。
- 最终 Play Mode 复验：从 SC-022 固定剧本（`1560-05-01`）注入妊娠，经游戏内保存/载入确认孕期保留；逐月推进至 `1561-02-01`，家族成员列表出现 `0岁` 新生儿、人口从 11 增至 12。出生后再次保存/载入；磁盘存档 `saveFormatVersion=13`、人物 12、孕期 0、出生人物记录为 1561 年。退出 Play Mode 后将测试期间覆盖的 `save-slot-1.json` 与 `.bak` 从事前备份原样恢复，SHA-256 分别匹配 `1E61ED9CAB4A5F3E5FBC…` 与 `F1C0630E5DD91BAA0B18…`。所有 Computer Use 截图在检查后删除。
- 验证过程事故：恢复场景文件并重载 Editor 后，原先仅存在 Editor 内存中的未保存场景改动可能已丢失；没有当前场景备份可证明其内容是否有用户工作。当前磁盘场景与 `HEAD` 一致，未把该场景纳入 M04B 变更。

## 工作区

M04B 的代码与测试文件、本报告及 `PROJECT_STATE.md` 的 M04 状态需要单独从混合工作区挑选。场景磁盘文件当前恢复为 `HEAD`；未改预制体、`ProjectSettings`、`Library` 或 `Temp` 内容。工作区仍含未提交的其他 M01/M02/M04–M08 WIP。
