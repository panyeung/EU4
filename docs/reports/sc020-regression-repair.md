# SC-020 回归修复报告

日期：2026-11-04  
修复者：OpenCode2（唯一写入者；未操作 Unity GUI / Computer Use / 截图）  
输入：`Logs/SC020-EditMode-results.xml`（977 EditMode，947 通过，30 失败）  
状态：源码与测试源码修复完成；**未运行 Unity Test Runner / Play Mode**，等待协调方复跑与审查。

## 1. 结论

30 个失败归并为 5 个根因，均可由源码或测试夹具错误解释：

1. **`SaveData` 构造器基线不自洽（影响 20+ 个失败）**
   `SaveData()` 默认 `succession = SuccessionSaveData.CreateDefault()`，但 `development` 被初始化为空人物集。v7 校验要求成长记录与名册**完全一致**，因此所有以 `new SaveData { ... }` 作为合法夹具的测试都在 `development` 覆盖检查处失败。修复为 `development = CharacterDevelopmentSaveData.CreateEmpty(succession)`，使构造器默认值本身是合法 v7 存档。
2. **旧版迁移错误优先级（IndustrySaveTests 3 个）**
   `SaveDataMigrator.TryMigrate` 在版本分支之前先检查“旧版含 development”，导致 v4/v5 夹具同时携带 industry（预期报 industry）时先报 development；v6 显式 null industry 又因为缺少 raw root 检查而先报日期错误。修复为：旧版先按 `ValidatePresence` 的顺序检查 industry，再检查 development；v6 在进入字段校验前拒绝缺失/显式 null 的 industry。
3. **继承收养新增人物没有成长行（SuccessionSaveTests 1 个）**
   `SuccessionService.AcceptCrisisAdoption` 会向名册加入養子 宗次，但 `GameSession` 的成长状态未同步，导致 `CaptureSave()` 生成名册与 development 不一致的存档。修复为在成功的继承命令后调用 `ReconcileDevelopmentRoster()`，只为零值新增人物补行，不重写既有能力/计划/结果；整表替换（危机示范开局）仍走全量重建。
4. **培养计划校验顺序（SC020EducationSessionTests 1 个）**
   `TrySetEducationPlan` 在人物身份/年龄之前返回 `Blocked`，儿童满 16 岁后无法得到明确的 `InvalidAge`。修复为先校验培养对象身份、存活与年龄，再在请求本身合法时由待处理事件/继承/摄政返回 `Blocked`。
5. **M2 场景夹具名册替换后未替换 development（M2ScenarioSaveTests 1 个）**
   测试刻意把 M2 待处理事件配到非 M2 名册上，但遗漏了 v7 的成长行同步，导致在到达场景门禁前被 development 覆盖检查拦截。修复测试夹具：替换 succession 时同步 `CharacterDevelopmentSaveData.CreateEmpty(mismatched.succession)`，保留原测试目标（场景/事件门禁拒绝）。

## 2. 变更文件

- `Assets/Scripts/Persistence/SaveData.cs`
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Tests/EditMode/M2ScenarioSaveTests.cs`

未修改场景、预制体、`ProjectSettings`、`Packages`、`Library`、`Temp`、`.meta`/GUID；未 stage/commit/push/merge/tag。

## 3. 失败族映射

| 失败族 | 失败数 | 根因 |
| --- | ---: | --- |
| DemoSummaryPanelTests | 3 | 1 |
| FamilyChronicleSaveTests | 5 | 1 |
| IndustrySaveTests | 3 | 2 |
| M2ScenarioSaveTests | 1 | 5（测试夹具） |
| RankPanelTests | 2 | 1 |
| SaveSlotStoreTests | 10 | 1 |
| SC020EducationSessionTests | 1 | 4 |
| SuccessionSaveTests | 1 | 3 |
| TitleScreenControllerTests | 2 | 1 |
| WorldClockTests | 2 | 1 |

## 4. 实际获得的验证证据

- **Host 侧 Roslyn 类型检查**（Unity 6000.6.6f1 随附 `csc.dll`，非 Unity Editor 编译）：
  Domain、Persistence、Events、Application、Presentation、Editor、EditModeTests 全部 exit 0，0 diagnostics。
- **纯 Persistence host harness**（引用本次编译的 Domain/Persistence，不经过 Unity）：
  `CHECKED pass=13 fail=0`。覆盖：新 `SaveData` 默认 v7 存档通过校验；默认 development 覆盖默认名册；1560-04 的四个夹具日期与 2000-12-31 最大日期夹具通过；v4/v5 同时含 industry+development 时先报 industry；v6 缺失/显式 null industry 先报 industry；v4 仅含 development 仍报 development；v6 含 development 仍拒绝。
- **未获得**：Unity Test Runner 通过、Console 计数、场景 Validator、Play Mode、用户验收。不得把 host 编译或 harness 当作 Unity 测试结果。

## 5. 复跑关注点

请协调方在已打开的 Unity 6.6.6f1 Editor 中复跑 EditMode：

- 上述 30 个用例应恢复；
- 重点确认 `SuccessionSaveTests.CaptureSave_AfterCrisisAdoption_RoundTripsAcceptedState`（新增成長行）与 `CharacterGrowthSaveTests` 全部仍通过；
- 重点确认 `SaveSlotStoreTests.Save_RecoversFromCorruptActiveSlotAndPreservesSource` 等恢复路径仍保留原 corrupt 字节；
- 已有 v6→v7 迁移与 SC-018/SC-019 数据保真测试应保持不变。

## 6. 未解决事项

本次修复范围内无已知未解决项。Unity 运行时行为仍需协调方用 Test Runner / Play Mode 验证；未提交、未合并、未打 tag。
