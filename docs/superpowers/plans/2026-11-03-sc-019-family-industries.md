# SC-019 实现计划：家族产业首切片（农庄与商馆）

> **For OpenCode2:** 本计划由协调方在后续实现 dispatch 中执行；当前文档任务只写本文件与设计稿，不实现代码。实现时按任务顺序执行，只改本计划“文件所有权”列出的路径，保留所有无关用户 WIP。

**Goal:** 在现有 Unity 项目中交付一个可完整游玩的 M3 首切片：两个初始产业（农庄、商馆）、三名资源（资金/物资/情报）、现有六人的人物任命、每月自动结算、空缺/短缺/失效处理、两次继承交接、v6 存档与迁移、现有 UI/编年史展示。

**Approach:** 纯 C# 领域模型负责产业定义、任命、月结与安全算术；`GameSession` 负责唯一月结入口、时间跨月防护、事务提交与继承协调；`SaveData`/`SaveDataMigrator`/`SaveDataValidator` 升到 v6 并保留 SC-018 家庭关系与全部历史；一个事务式 Editor 生成器在 `FoundationScene` 中增加 `IndustryRoot`/`IndustryPanel`。不引入外部代码、package、Unity 项目或动态市场。

**Acceptance:** 新局 30/12/0、两产业空缺；任命晴信/千恵后结束 1560-04 得 35/13/1；日推进与旅行不能跨月；结束月份唯一结算；空缺/短缺/失效/两次交接/保存读档/SC-018 迁移均有自动化测试；协调方可在用户当前授权下用 Computer Use 执行 Play Mode 并单独记录，用户验收另行记录。

**Design:** `docs/superpowers/specs/2026-11-03-sc-019-family-industries-design.md`

**Constraints:**

- `GDD_v4.0.md` 是设计权威；本切片不是完整 M3，不得把“两个产业 + 两次交接”报告为 M3 完成。
- 当前分支 `feature/sc018-independent-core-events` 含未提交 SC-018；不得覆盖、清理、提交或合并这些内容。SC-018 用户验收仍待完成。
- 不新增 Unity 项目、package、付费服务、外部代码或资产；不改 `ProjectSettings`、prefab、`Library`、`Temp`。
- 保留既有 `.meta` GUID；只有新增源文件/测试文件可配对新 `.meta`。
- 一个指定集成 worker 独占 `Assets/Scenes/FoundationScene.unity`、`Assets/Scripts/Editor/AddSC019IndustryPanel.cs` 和 `FoundationSceneValidator.cs` 的场景部分；其他 worker 不得并行写场景。
- 生成器使用一个 Undo group，先校验后保存；异常时 `Undo.RevertAllInCurrentGroup`，不重新加载场景。
- 当前文档任务不运行 Unity、不测试、不提交、不推送、不合并、不打 tag。后续实现使用用户已打开的 Editor；不得对同一项目启动 batchmode。
- 项目默认由用户执行 Play Test；当前用户已明确授权协调方/助手使用 Computer Use 在现有 Unity Editor 中执行 Play Mode。记录为“coordinator-run Play Mode”，与用户验收分开。不得把协调方 Play Mode 当作 user acceptance。
- 测试必须区分编译、EditMode、Console、Validator、Play Mode、存档/读档、用户验收。

## 文件所有权（后续实现 dispatch）

| 所有者 | 路径 | 说明 |
|---|---|---|
| 实现 worker（OpenCode2） | `Assets/Scripts/Domain/IndustryCatalog.cs`、`IndustryState.cs`、`IndustryCalendar.cs`、`IndustryService.cs`、`IndustrySettlement.cs` | 新领域模型 |
| 实现 worker | `Assets/Scripts/Application/GameSession.cs`、`IndustryView.cs`、`AssemblyInfo.cs` | 月结入口、任命 API、视图、内部测试 seam |
| 实现 worker | `Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs` | v6 schema、校验、迁移 |
| 实现 worker | `Assets/Scripts/Domain/FamilyChronicleEntry.cs`、`HistoryLedger.cs` | `Industry` 类别与月结/人事记录 |
| 实现 worker | `Assets/Scripts/Presentation/IndustryPanel.cs`、`WorldClockPanel.cs`、`WorldMapController.cs`、`FamilyChroniclePanel.cs` | 面板与跨月提示 |
| 实现 worker | `Assets/Tests/EditMode/Industry*.cs`、`SC019*.cs`、`AddSC019IndustryPanelTests.cs` 及 `.meta` | 新测试 |
| 实现 worker | `SC019_DEMO.md`、`PROJECT_STATE.md`、`docs/reports/sc019-implementation.md` | 试玩与证据 |
| 集成 worker（唯一） | `Assets/Scripts/Editor/AddSC019IndustryPanel.cs`、`FoundationSceneValidator.cs`、`Assets/Scenes/FoundationScene.unity` | 场景生成/验证/保存 |
| 协调方 | 本设计稿、本计划、最终签核 | 复审、路由、用户验收记录 |

`ProjectSettings` 预期零改动；若生成器意外需要，停止并报告，不擅自修改。

## Task 0 — 基线映射与契约确认

**Read:** `Assets/Scripts/Application/GameSession.cs`、`Assets/Scripts/Domain/WorldClock.cs`、`GameDate.cs`、`SuccessionState.cs`、`SuccessionService.cs`、`Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs`、`Assets/Scripts/Domain/HistoryLedger.cs`、`FamilyChronicleEntry.cs`、`Assets/Scripts/Presentation/FamilyChroniclePanel.cs`、`WorldClockPanel.cs`、`WorldMapController.cs`、`Assets/Scripts/Editor/AddSC012FamilyChroniclePanel.cs`、`AddSC013FamilyRosterPanel.cs`、`FoundationSceneValidator.cs`、`Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs`、`M2FixedScenario.cs`。

- [ ] 记录当前 schema v5、`CurrentRulesVersion = 1`、事件/编年史/继承字段。
- [ ] 记录 `AdvanceOneDay`、`AdvanceDays`、`PreviewTravel`、`ConfirmTravel`、`TryAdvanceToNextMonth` 的现有行为与失败通道。
- [ ] 用 `grep` 列出所有跨月 `AdvanceDays`/`AdvanceOneDay` 测试夹具；标出需要 internal seam 的调用。
- [ ] 记录 SC-018 `familyRelationships` 与 chronicle 的保存/迁移路径，作为 v6 回归基线。
- [ ] 记录现有 UI 根节点、面板绑定模式、生成器 Undo/Validate/Rollback 模式。
- [ ] 在 `docs/reports/sc019-implementation.md` 写基线快照；不改代码。

**Acceptance:** 基线记录覆盖所有时间入口、存档字段、UI 根与测试夹具；没有代码/场景改动。

## Task 1 — 产业目录、资源与任命状态

**Create:**

- `Assets/Scripts/Domain/IndustryCatalog.cs`
- `Assets/Scripts/Domain/IndustryCalendar.cs`
- `Assets/Scripts/Domain/IndustryState.cs`
- `Assets/Scripts/Domain/IndustryService.cs`

**Interfaces:**

```csharp
public readonly struct IndustryDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public int WorkUnitsRequired { get; }   // 3
    public int CapacityPosts { get; }       // 1
    public int UpkeepFunds { get; }         // farm 0, trading 6
    public int UpkeepSupplies { get; }      // farm 1, trading 2
    public int OutputFunds { get; }         // farm 2, trading 9
    public int OutputSupplies { get; }      // farm 4, trading 0
    public int OutputIntelligence { get; }  // farm 0, trading 1
    public bool MaintainsFromOutput { get; } // farm true, trading false
}

public static class IndustryCatalog
{
    public const string FarmId = "industry.farm";
    public const string TradingHouseId = "industry.trading-house";
    public static IReadOnlyList<IndustryDefinition> All { get; } // ordinal: farm, trading
    public static bool TryGet(string industryId, out IndustryDefinition definition);
}

public static class IndustryCalendar
{
    public static int MonthIndex(GameDate date); // year * 12 + month - 1
}

public readonly struct IndustryResources : IEquatable<IndustryResources>
{
    public const int Minimum = 0;
    public const int Maximum = 999999;
    public int Funds { get; }
    public int Supplies { get; }
    public int Intelligence { get; }
    public static bool TryCreate(int funds, int supplies, int intelligence,
        out IndustryResources resources, out string error);
}

public sealed class IndustryState
{
    public IndustryResources Resources { get; }
    public int LastSettledMonthIndex { get; }
    public IReadOnlyList<IndustryAssignment> Assignments { get; } // sorted by IndustryId
    public static IndustryState CreateInitial(GameDate date);
    public IndustryState Clone();
    public bool TryGetAssignee(string industryId, out PersonId personId);
    public bool TrySetAssignee(string industryId, PersonId personId, out string error);
    public bool TryClearAssignee(string industryId, out string error);
    public bool TrySetResources(IndustryResources resources, out string error);
    public bool TrySetLastSettledMonthIndex(int monthIndex, out string error);
}

public readonly struct IndustryAssignment
{
    public string IndustryId { get; }
    public PersonId PersonId { get; }
}

public enum IndustryAssignmentOutcome
{
    Succeeded, NoChange, UnknownIndustry, UnknownPerson, NotMember,
    Dead, Retired, Minor, NoRemainingUnits, Blocked
}

public readonly struct IndustryAssignmentResult
{
    public bool Succeeded { get; }
    public IndustryAssignmentOutcome Outcome { get; }
    public string Message { get; }
    public PersonId PreviousAssignee { get; }
    public PersonId CurrentAssignee { get; }
}

public static class IndustryService
{
    public const int MonthlyWorkUnits = 4;
    public const int PostWorkUnits = 3;

    public static bool CanWork(SuccessionState succession, PersonId personId, GameDate date,
        out string reason);
    public static int RemainingWorkUnits(SuccessionState succession, IndustryState state,
        PersonId personId, GameDate date);
    public static IndustryAssignmentResult TryAssign(IndustryState state, SuccessionState succession,
        GameDate date, string industryId, PersonId personId);
    public static IndustryAssignmentResult TryUnassign(IndustryState state, SuccessionState succession,
        GameDate date, string industryId);
    public static IReadOnlyList<IndustryVacancy> ReconcileAssignments(IndustryState state,
        SuccessionState succession, GameDate date);
}

public readonly struct IndustryVacancy
{
    public string IndustryId { get; }
    public PersonId PreviousAssignee { get; }
    public string Reason { get; }
}
```

**Rules:**

- `IndustryState.CreateInitial(1560-04-01)` → 30/12/0，两个 slot 均空，`LastSettledMonthIndex = MonthIndex(1560-03)`。
- `TrySetAssignee` 只校验产业存在和容量；人物资格由 `IndustryService` 校验。
- `TryAssign` 先校验产业、人物、成员、存活、未退休、年龄、剩余工时；同一人重复任命为 `NoChange` 成功；替换是单次赋值。
- `RemainingWorkUnits`：可工作者未任职 4，已任职 1，不可工作 0；`3 + 3 > 4` 自然阻止兼任。
- `ReconcileAssignments` 是总函数：只移除不可工作任命，返回原因；不产出、不扣费。若同一人物意外出现在两个产业，按产业 ID 升序保留第一个，清空后续重复任命并返回“重复任命已解除”。
- 所有资源范围 `0..999999`；`IndustryResources.TryCreate` 拒绝负数/越界。

**Tests:** `Assets/Tests/EditMode/IndustryCatalogTests.cs`、`IndustryStateTests.cs`、`IndustryServiceTests.cs`。

- [ ] `Catalog_HasFarmAndTradingHouseWithApprovedNumbers`
- [ ] `CreateInitial_UsesApprovedOpeningResourcesAndVacantSlots`
- [ ] `Assign_RejectsDeadRetiredMinorNonMemberAndUnknown`
- [ ] `Assign_RejectsSecondPostForSamePerson`
- [ ] `Assign_ReplacesOccupantAtomically`
- [ ] `Unassign_EmptyIsIdempotentSuccess`
- [ ] `RemainingUnits_AreFourMinusThreeWhenAssigned`
- [ ] `Reconcile_VacatesInvalidAssignmentsOnly`
- [ ] `Reconcile_ClearsDuplicateAssignmentKeepingFirstByIndustryOrder`
- [ ] `Resources_RejectNegativeAndOverCap`

**Acceptance:** 目录/初始值/任命/替换/解除/失效/工时规则全部由纯 C# 测试覆盖，不依赖 Unity。

## Task 2 — 月结纯计算与报告

**Create:** `Assets/Scripts/Domain/IndustrySettlement.cs`  
**Test:** `Assets/Tests/EditMode/IndustrySettlementTests.cs`

**Interfaces:**

```csharp
public enum IndustryLineStatus { Operated, Vacant, Stalled, InvalidAssignment }

public readonly struct IndustryLineResult
{
    public string IndustryId { get; }
    public string IndustryName { get; }
    public PersonId AssigneeId { get; }
    public string AssigneeName { get; }
    public IndustryLineStatus Status { get; }
    public string Reason { get; }
    public int UpkeepFunds { get; }
    public int UpkeepSupplies { get; }
    public int GrossOutputFunds { get; }
    public int GrossOutputSupplies { get; }
    public int GrossOutputIntelligence { get; }
    public int NetOutputFunds { get; }       // after maintenance
    public int NetOutputSupplies { get; }    // farm: 4 gross - 1 maintenance = 3
    public int NetOutputIntelligence { get; }
    public int AppliedFundsDelta { get; }    // actual change after cap clamp
    public int AppliedSuppliesDelta { get; }
    public int AppliedIntelligenceDelta { get; }
    public bool MaintenanceRetainedFromOutput { get; } // farm true, trading false
}

public sealed class IndustryMonthReport
{
    public int Year { get; }
    public int Month { get; }
    public IndustryResources Before { get; }
    public IndustryResources After { get; }
    public IReadOnlyList<IndustryLineResult> Lines { get; }
    public IReadOnlyList<IndustryVacancy> Vacated { get; }
    public bool HasShortage { get; }
    public bool AnyStaffed { get; }
    public bool Changed { get; }
    public string Summary { get; } // Chinese, invariant
}

public enum IndustrySettlementOutcome
{
    Succeeded, AlreadySettled, SettlementGap, InvalidState
}

public readonly struct IndustrySettlementResult
{
    public IndustrySettlementOutcome Outcome { get; }
    public bool Succeeded { get; }
    public string Message { get; }
    public IndustryState NextState { get; }
    public IndustryMonthReport Report { get; }
}

public static class IndustrySettlement
{
    public static IndustrySettlementResult Plan(IndustryState state, SuccessionState succession,
        GameDate closedMonth);
}
```

**Rules:**

- 检查 `LastSettledMonthIndex`：`== current` → `AlreadySettled`；`< current-1` → `SettlementGap`；`> current` → `InvalidState`；`== current-1` → 正常。
- 克隆状态；先 `ReconcileAssignments`，再按 `IndustryCatalog.All` 顺序逐产业结算。
- **农庄（收成后维护）**：加毛产出 +2 资金、+4 物资；从毛收成中保留 1 物资维护；净 +2 资金、+3 物资；再把净额加到库存并夹到 `999999`。从 0 起始物资也能运作。不得先把 4 加到满库存再扣 1。
- **商馆（维护后产出）**：先检查资金/物资是否足够支付 6 资金、2 物资；足够则扣维护费，再加 +9 资金、+1 情报；不足则 `Stalled`，不扣费、不产出、保留任命，`HasShortage = true`。农庄不适用“维护费不足停产”，因为它的维护来自自身毛收成。
- 空缺/失效 → 不扣费、不产出；失效报告保留旧负责人姓名与原因。
- 农庄先结算，其净产出的资金/物资立即可用于商馆维护费检查。
- 资源用 `long` 中间值；`ApplyDelta` 返回实际前后值；满仓时实际增量为 0，报告写“净产 X，实际 +0（已达上限）”。
- 报告文本包含逐产业维护/产出/实际入账和月末前后值；同一输入逐字节相同。
- 不消费随机流；不修改输入状态。

**Tests:**

- [ ] `Settle_BothStaffed_ProducesExpectedNetAndReport`（30/12/0 → 35/13/1）
- [ ] `Settle_FarmRetainsOneSupplyFromGrossHarvest`（毛 4 → 维护 1 → 净 3）
- [ ] `Settle_FarmWorksFromZeroStartingResources`（0/0/0 → 2/3/0）
- [ ] `Settle_TradingChecksMaintenanceBeforeOutput`
- [ ] `Settle_VacantIndustry_NoUpkeepNoOutput`
- [ ] `Settle_Shortage_StallsWithoutNegativeResources`
- [ ] `Settle_FarmCreditsBeforeTrading`
- [ ] `Settle_InvalidAssignee_VacatesAndProducesNothing`
- [ ] `Settle_IsDeterministicAndDoesNotConsumeRandom`
- [ ] `Settle_AlreadySettled_IsNoOp`
- [ ] `Settle_Gap_IsRejected`
- [ ] `Settle_FarmAtCap_NetBeforeClampKeepsFullWarehouse`（物资 999999 → 999999；净 3，实际 0；不得 999998）
- [ ] `Settle_ClampsAtResourceCap`

**Acceptance:** 月结是纯函数；正常/空缺/短缺/失效/重复/漏月/上限均有测试；报告前后值可读。

## Task 3 — 唯一月结入口、跨月防护与安全保存

**Modify:** `Assets/Scripts/Application/GameSession.cs`（含 `MonthTurnOutcome`）  
**Create:** `Assets/Scripts/Application/IndustryView.cs`、`Assets/Scripts/Application/AssemblyInfo.cs`  
**Test:** `Assets/Tests/EditMode/IndustryMonthCrossingTests.cs`

**Interfaces:**

```csharp
public enum MonthTurnOutcome
{
    Succeeded = 0, PendingEvent = 1, ContentUnavailable = 2, DateOutOfRange = 3,
    SchedulingFailed = 4, PendingSuccession = 5,
    MonthSettlementRequired = 6, IndustryStateInvalid = 7
}

public readonly struct IndustrySlotView
{
    public string IndustryId { get; }
    public string DisplayName { get; }
    public PersonId AssigneeId { get; }
    public string AssigneeName { get; }       // empty when vacant
    public int WorkUnitsRequired { get; }     // 3
    public int WorkUnitsRemaining { get; }    // 1 when assigned, 4 when vacant
    public IndustryLineStatus Status { get; } // derived from last/current state
    public string StatusReason { get; }
}

public sealed class IndustryView
{
    public IndustryResources Resources { get; }
    public int LastSettledMonthIndex { get; }
    public IReadOnlyList<IndustrySlotView> Slots { get; }
    public bool CanEditAssignments { get; }
    public string BlockReason { get; }
    public IndustryMonthReport LastReport { get; }
    public string LastChronicleSummary { get; }
}

// GameSession
public IndustryView Industry { get; }
public bool CanEditIndustryAssignments { get; }
public string IndustryBlockReason { get; }
public IndustryMonthReport LastIndustryReport { get; }
public string LastIndustryMessage { get; } // transient; e.g. safe-save cleanup
public bool CanAdvanceOneDayWithinMonth { get; }
public string LastTravelBlockReason { get; }
```

**Implementation:**

- `InitSession` 初始化 `_industryState = IndustryState.CreateInitial(WorldClock.CurrentDate)`。
- `TryAdvanceToNextMonth` 改为事务式：预检 → 快照（日期/产业/随机流/事件运行时/编年史）→ `IndustrySettlement.Plan` → `ScheduleMonthly(nextDate)` → 追加编年史（当 `Report.AnyStaffed || Report.Vacated.Count > 0`，即使满仓实际变化为 0 也记录“净产 X，实际 +0”）→ 提交产业/事件/日期 → 发布一次 `SessionStateChanged`。失败恢复快照。
- `AdvanceOneDay`/`AdvanceDays`：先检查目标日期月份；跨月或 `lastSettled != current-1` 时返回 false，设置 `MonthSettlementRequired`/`IndustryStateInvalid`，日期不变。
- `PreviewTravel`：开始时清空 `LastTravelBlockReason`；到达日期跨月 → 返回 null 并设置原因；成功预览不保留旧原因。`ConfirmTravel` 重新校验，失败不改日期/地点；成功时清空原因。
- `CaptureSave` 前调用 `EnsureIndustryAssignmentsValid()`（Task 4 的安全保存）；保存清理不写编年史。
- `TryApplySave` 先构建/校验 `IndustryState`，失败不触碰当前会话。
- `AssemblyInfo.cs` 加 `[assembly: InternalsVisibleTo("SengokuChronicle.EditModeTests")]`；`internal bool AdvanceDaysForLegacyFixtureSetup(int count)` 仅供既有跨年夹具，**不是 public API**，不供 UI/生产路径调用；它不做产业生产，只对齐 `lastSettledMonthIndex` 到新日期前一个月。

**Tests:**

- [ ] `AdvanceOneDay_OnLastDay_RefusesAndKeepsDate`
- [ ] `AdvanceDays_CrossingMonth_RefusesAtomically`
- [ ] `PreviewTravel_CrossingMonth_RefusesWithReason`
- [ ] `ConfirmTravel_CrossingMonth_RefusesAndKeepsLocation`
- [ ] `EndMonth_SettlesExactlyOnceAndAdvances`
- [ ] `EndMonth_PendingEvent_DoesNotSettle`
- [ ] `EndMonth_PendingSuccession_DoesNotSettle`
- [ ] `EndMonth_AtMaxDate_DoesNotSettle`
- [ ] `EndMonth_SettlementGap_DoesNotSettle`
- [ ] `EndMonth_DuplicateCalls_SettleDistinctMonths`
- [ ] `LegacyFixtureSeam_IsInternalAndNotUsedByProduction`

**Acceptance:** 所有生产时间入口都不能跨月跳结算；结束月份是唯一结算入口；重复/待处理/越界/失败均无半状态；内部 seam 只服务既有夹具。

## Task 4 — 任命 API、继承协调与失效安全保存

**Modify:** `Assets/Scripts/Application/GameSession.cs`、`Assets/Scripts/Domain/IndustryService.cs`  
**Test:** `Assets/Tests/EditMode/SC019SuccessionIndustryTests.cs`、`IndustryAssignmentSessionTests.cs`

**Interfaces:**

```csharp
public IndustryAssignmentResult TryAssignIndustry(string industryId, PersonId personId);
public IndustryAssignmentResult TryUnassignIndustry(string industryId);

// internal total cleanup; no production, no chronicle
private void EnsureIndustryAssignmentsValid();
```

**Rules:**

- 任命/解除只在 `CanEditIndustryAssignments` 为 true 时允许；待处理事件、继承决策、摄政到期、家族灭亡时返回 `Blocked` 与中文原因。
- `TryAssignIndustry` 调 `IndustryService.TryAssign`；替换释放旧预留；同一人 no-op。
- `ApplySuccessionResult` 成功后调用 `EnsureIndustryAssignmentsValid`；旧家主退休 → 其职位自动解除；新家主不自动接管；其他合法任命保留。
- **预算**：每个成人固定 4；任命预留 3；替换/解除释放预留；不建立“每人本月已花时间”计数器，月结只按锁定方案占用一次。继承不转移/不追加预算。新家主未任职可用 4，已任职可用 1。月结前允许用新家主自己的预算任命当月方案；月结后属于下月方案。不得同时声称“立即释放”和“已花时间不返还”。
- `EnsureIndustryAssignmentsValid` 是总函数：只移除不合法任命，不扣费、不产出、不推进时间、不追加编年史；设置瞬时 `LastIndustryMessage`。
- `CaptureSave` 前调用它，保证死亡/退休/离族/未成年不会造成“稳定点无法保存”。`SaveDataValidator` 仍拒绝含非法任命的存档（正常捕获不会产生）。
- 继承导致的自动解除追加一条 `Industry` 人事编年史；手动任命/解除不写编年史。

**Tests:**

- [ ] `FirstHandoff_VacatesOldHeadPostAndKeepsOtherAssignments`
- [ ] `SecondHandoff_VacatesOldHeadPostAndNewHeadStartsUnassigned`
- [ ] `Handoff_DoesNotTransferBudgetOrAutoAssign`
- [ ] `Handoff_AllowsSameMonthAppointmentUsingNewHeadOwnBudget`
- [ ] `Handoff_AppendsPersonnelChronicleOnce`
- [ ] `CaptureSave_RemovesInvalidAssignmentAndStillValidates`
- [ ] `Assignment_BlockedByPendingEventOrSuccession`

继承测试优先复用现有 M2 事件/交接夹具到达两次交接；领域级 `SuccessionService` 测试可直接构造 `SuccessionState`。不得绕过 M2 事件门禁来“制造”生产路径；若现有夹具不足，只补测试夹具，不改生产门禁。

**Acceptance:** 两次交接的旧/新家主职位、预算、当月任命与编年史全部确定；失效任命不会阻塞稳定保存。

## Task 5 — v6 存档、校验与迁移

**Modify:** `Assets/Scripts/Persistence/SaveData.cs`、`SaveDataValidator.cs`、`SaveDataMigrator.cs`  
**Test:** `Assets/Tests/EditMode/IndustrySaveTests.cs`

**Schema:**

```csharp
[Serializable] public sealed class IndustryStateSaveData
{
    public int funds;
    public int supplies;
    public int intelligence;
    public int lastSettledMonthIndex;
    public IndustrySlotSaveEntry[] slots;
}

[Serializable] public sealed class IndustrySlotSaveEntry
{
    public string industryId;
    public string assignedPersonId; // empty = vacant
}
```

- `SaveData.industry` 与 `SaveDataPresenceProbe.industry` 新增。
- `SaveDataValidator.CurrentVersion = 6`；`SaveData.CurrentRulesVersion = 2`。
- `SaveData` 构造函数初始化 `industry = IndustryStateSaveData.CreateInitial(WorldClock.NewGameDate)`。
- `CaptureSave` 使用 `IndustryStateSaveData.Capture(_industryState)`；slots 按产业 ID 升序。
- `TryApplySave` 先恢复 `succession`，再用已恢复的 `SuccessionState` 调 `IndustryStateSaveData.TryRestore(data.industry, succession, date, ...)`；失败不触碰当前状态。

**Validation:**

- funds/supplies/intelligence `0..999999`。
- `lastSettledMonthIndex == MonthIndex(date)-1`。
- slots 恰好两个、ID 唯一、属于目录。
- 非空 assignee 必须是 succession 中已登记、在册、存活、未退休、年龄 >= adulthood 的人物；不能重复。
- 失败返回中文原因，不抛异常。

**Migration:**

- 新增 v5→v6 分支：拒绝已含 `industry` 的 v5 载荷；保留 `familyRelationships`/`chronicle`/`succession`/事件/随机流/任务/日期/地点；加初始 industry；`rulesVersion=2`；不追加编年史。
- v4→v6：先补空 `familyRelationships`，再加初始 industry；若 v4 原始 JSON 已含 `industry` 根字段，拒绝而不是静默替换。
- v3/v2→v6：保留既有迁移语义，最后加初始 industry 与 rules 2；若原始 JSON 已含 `industry`，拒绝。
- v1→v6：保留既有迁移语义，最后加初始 industry 与 rules 2；v1 解析器没有 industry 字段，若原始 JSON 含该根字段则拒绝。
- `Clone` 复制 industry；源对象/文件不变。
- `ValidatePresence`：允许版本集合加入 5；v6 必须有 `industry`；v5 不要求 `industry`，但若含 `industry` 根字段则拒绝（迁移不静默替换）；v4 及以下按既有规则。
- 更新既有测试夹具：手工构造 `SaveData` 的 `ValidData()`/硬编码 v5 JSON 必须补 `industry`（用 `CreateInitial(new GameDate(year, month, day))` 或统一 helper），否则 v6 校验会因 `lastSettledMonthIndex` 不匹配而失败。v5 迁移测试保留“无 industry”的 v5 载荷。
- 迁移分支必须**显式覆盖** `current.industry = IndustryStateSaveData.CreateInitial(legacyDate)`，不能依赖 `SaveData` 构造函数的默认值；默认值只适用于新局 1560-04-01。
- “旧载荷是否已含 industry”必须用原始 JSON 的根属性检查（`SaveDataValidator.HasRootProperty`），不能只看反序列化对象，因为 `SaveData` 构造函数会为缺失字段生成默认 industry。

**Tests:**

- [ ] `V6RoundTrip_PreservesResourcesAssignmentsAndLastSettled`
- [ ] `V5Migration_PreservesSC018FamilyRelationshipsAndChronicle`
- [ ] `V5Migration_PreservesEventHistorySuccessionMissionsAndRelationships`
- [ ] `V5Migration_InitializesIndustriesWithoutRetroactiveProduction`
- [ ] `V5PayloadWithIndustry_IsRejected`
- [ ] `V4PayloadWithIndustry_IsRejected`
- [ ] `V6Presence_RequiresIndustry`
- [ ] `Load_DoesNotAppendOrSettle`
- [ ] `LoadThenEndMonth_DoesNotDuplicateSettlement`
- [ ] `SaveAfterInvalidAssignment_CleansAndValidatesWithoutChronicleAppend`

**Acceptance:** v6 往返稳定；v5 SC-018 关系/历史逐项保留；旧档不补算收益；读档不重复结算；非法载荷安全拒绝。

## Task 6 — 编年史与现有面板类别

**Modify:** `Assets/Scripts/Domain/FamilyChronicleEntry.cs`、`HistoryLedger.cs`、`Assets/Scripts/Presentation/FamilyChroniclePanel.cs`  
**Test:** `Assets/Tests/EditMode/IndustryChronicleTests.cs`

**Rules:**

- `ChronicleCategory.Industry = 4`。
- `FamilyChronicleEntry.TryCreate` 的 Industry 分支：`FromValue/ToValue` 在 `0..999999`，允许相等；要求摘要非空。
- `HistoryLedger.TryAppendIndustry(year, month, subjectId, sourceId, fromFunds, toFunds, title, summary, out entry, out error)`。
- 月结实际发生（有产业任职或自动解除）追加一条 `industry.monthly`/`settlement`；两产业全空且无解除不写。
- 继承自动解除追加 `industry.assignments`/`succession`；手动任命不写。
- `FamilyChroniclePanel.CategoryName` 增加“产业”；`CategoryDetail` 显示“资金 X → Y”；不显示原始 ID。
- 编年史保存/读档沿用现有 `ChronicleSaveCodec`；`Industry` 记录可往返。

**Tests:**

- [ ] `IndustrySettlement_AppendsOneMonthlyEntryWithBeforeAfter`
- [ ] `IndustrySettlement_AtCapRecordsNetAndActualZero`
- [ ] `VacantMonth_AppendsNothing`
- [ ] `SuccessionVacancy_AppendsOnePersonnelEntry`
- [ ] `IndustryChronicle_RoundTripsAndRendersCategoryLabel`
- [ ] `IndustryChronicle_DoesNotExposeRawIds`

**Acceptance:** 月结与人事在编年史可见、可保存、可读档；无重复/无原始 ID。

## Task 7 — UI 面板、场景生成器与 Validator（唯一集成 worker）

**Create:** `Assets/Scripts/Presentation/IndustryPanel.cs`、`Assets/Scripts/Editor/AddSC019IndustryPanel.cs`  
**Modify:** `Assets/Scripts/Editor/FoundationSceneValidator.cs`、`Assets/Scenes/FoundationScene.unity`（仅经生成器）  
**Modify:** `Assets/Scripts/Presentation/WorldClockPanel.cs`、`WorldMapController.cs`  
**Test:** `Assets/Tests/EditMode/IndustryPanelTests.cs`、`AddSC019IndustryPanelTests.cs`

**Scene hierarchy:**

```text
Canvas/
  IndustryRoot/                     (active, direct Canvas child, owns IndustryPanel)
    IndustryOpenButton              ("产业")
    IndustryPanel                   (inactive by default)
      IndustryTitleText             ("产业与人事")
      IndustryResourceText          ("资金 30　物资 12　情报 0")
      IndustryBlockReasonText       (hidden when empty)
      IndustryRowFarm
        RowNameText                 ("农庄（等级 1・1 个职位・3/4 工时）")
        RowStatusText
        RowAssigneeText
        IndustryUnassignButton      ("解除")
        CandidateContainer
          IndustryCandidateButtonTemplate (inactive)
      IndustryRowTradingHouse       (same children)
      IndustryReportText
      IndustryCloseButton           ("关闭")
```

- Root sibling above `M2WorldMapRoot`，below `M6TitleRoot` and `EventChoiceRoot`; validator checks.
- Open button in left management column, below chronicle/roster buttons; validator checks no overlap.
- Panel `Refresh`:
  - resources; block reason; two rows; candidate buttons for eligible adults sorted by `PersonId`;
  - candidate label `姓名（剩余 X/4）`; click `TryAssignIndustry`; unassign click `TryUnassignIndustry`;
  - report: `LastIndustryReport?.Summary` else last `Industry` chronicle summary else “本月尚未结算。”;
  - subscribe `SessionStateChanged`; open/close never mutate state.
- `WorldClockPanel`: add `MonthSettlementRequired`/`IndustryStateInvalid` cases; disable “推进一天” on last day and show “月末：请结束本月进行结算”.
- `WorldMapController.SelectDestination`: if `_preview == null` and `LastTravelBlockReason` non-empty, show it before generic “无直达路线”.
- Generator:
  - constants `RootName = "IndustryRoot"`, `PanelName = "IndustryPanel"`, `OpenButtonName = "IndustryOpenButton"`, row/template names;
  - `RunMenu`, `CheckScenePreflight`, `Update(Scene)`, `Update(Scene, Action<GameObject>)`, `Validate(Scene, out string error)`;
  - one Undo group; create all; bind; validate; save only on success; exception → `Undo.RevertAllInCurrentGroup`; no scene reload;
  - refuse existing root/partial; refuse dirty/playing/wrong scene.
- `FoundationSceneValidator`: require exactly one IndustryPanel/IndustryRoot/OpenButton; check bindings, active states, sibling order, candidate template inactive, labels; write report.
- No prefab/ProjectSettings changes.

**Tests:**

- [ ] `IndustryPanel_ShowsResourcesSlotsStatusesAndReport`
- [ ] `IndustryPanel_ShowsBlockReasonWhenPendingEvent`
- [ ] `IndustryPanel_CandidateButtonsAssignAndReplace`
- [ ] `IndustryPanel_UnassignClearsSlot`
- [ ] `IndustryPanel_ReportFallsBackToLastChronicleAfterLoad`
- [ ] `AddSC019IndustryPanel_CreatesOneRootAndBindings`
- [ ] `AddSC019IndustryPanel_RefusesExistingPartialControl`
- [ ] `AddSC019IndustryPanel_RollsBackOnValidationFailure`
- [ ] `AddSC019IndustryPanel_ValidateRejectsWrongSiblingOrder`

**Acceptance:** 面板可完成任命/解除/查看月结；生成器事务式；Validator 通过；场景只由集成 worker 修改。

## Task 8 — 中文试玩文档、项目状态与证据

**Create:** `SC019_DEMO.md`  
**Update:** `PROJECT_STATE.md`、`docs/reports/sc019-implementation.md`

- [ ] `SC019_DEMO.md` 用中文写精确步骤、预期数值、停止/报告条件、结果清单；不使用截图作为唯一证据。
- [ ] 记录新局 30/12/0、两产业空缺；任命晴信/千恵后 `3/4`；结束 1560-04 → 35/13/1；编年史“产业月结”。
- [ ] 记录月末日推进/旅行拒绝；结束月份成功；重复结束不会重复同月。
- [ ] 记录短缺路线：只任命商馆，数月后物资不足停产；再任命农庄恢复。
- [ ] 记录两次交接：晴信→正綱、正綱→信正；旧家主职位解除、新家主不自动接管、其他合法任命保留。
- [ ] 记录保存/读档：资源/任命/`lastSettledMonthIndex` 恢复；读档后结束月份只结算一次。
- [ ] 记录 SC-018 v5 迁移：`familyRelationships` 与 chronicle 保留，产业初始为空。
- [ ] `PROJECT_STATE.md` 与实现报告分开写编译、EditMode、Console、Validator、Play Mode、存档/读档、用户验收；未运行项明确写“未运行/未验证”。

**Acceptance:** 文档与实现数值一致；试玩步骤可由协调方或用户按文执行；证据边界清楚。

## Task 9 — 验证、只读复审与交接

- [ ] 在用户已打开的 Unity Editor 中编译；记录 `Editor.log` 结果，不启动 batchmode。
- [ ] 运行完整 EditMode；记录 passed/failed/skipped 数量；重点跑新增测试与 SC-018/SC-013/SC-010 回归。
- [ ] 记录 Console error/warning 计数；不把“0 errors”与“0 warnings”混为一谈。
- [ ] 运行 `FoundationSceneValidator`；保存并记录 `Logs/SceneValidationReport.txt`；未运行则明确写未运行。
- [ ] 协调方在当前用户授权下用 Computer Use 执行 Play Mode；记录日期、场景、路线、数值、Console、截图/观察；标记为 coordinator-run Play Mode。
- [ ] 执行存档/读档实测；记录槽位、恢复值、重复月结检查。
- [ ] 记录用户验收（接受/拒绝/待定）；协调方 Play Mode 不替代用户验收。
- [ ] 独立 OpenCode2 只读复审：检查文件所有权、规则一致性、测试证据、未覆盖边界；不改文件。
- [ ] 用户验收前不提交、不合并、不打 tag；验收后由协调方按项目规则处理 checkpoint/merge/tag/push。

**Acceptance:** 所有证据分项、可复现、与文档一致；无未声明的“通过”。

## 测试矩阵

| 领域 | 文件 | 必测 |
|---|---|---|
| 目录/初始状态 | `IndustryCatalogTests.cs` | 两产业 ID、数值、初始 30/12/0、空缺 |
| 任命/预算 | `IndustryStateTests.cs`、`IndustryServiceTests.cs` | 资格、4/3 工时、替换、解除、兼任拒绝、幂等 |
| 月结 | `IndustrySettlementTests.cs` | 满员 35/13/1、农庄收成后维护、从 0 资源运作、商馆先维护后产出、空缺、短缺、顺序、失效、确定性、重复/漏月、满仓上限 |
| 跨月 | `IndustryMonthCrossingTests.cs` | 日/多日/旅行拒绝、结束唯一结算、待处理事件/继承、最大日期、重复调用 |
| 继承 | `SC019SuccessionIndustryTests.cs` | 两次交接、旧/新家主、预算不转移、同月任命、编年史一次 |
| 存档 | `IndustrySaveTests.cs` | v6 往返、v5 迁移保留 SC-018、无追溯产出、非法载荷、读档不重复 |
| 编年史 | `IndustryChronicleTests.cs` | 月结一条、空缺不写、人事一条、类别/前后值、往返 |
| UI | `IndustryPanelTests.cs` | 资源/状态/阻塞/候选/替换/解除/报告回退 |
| 生成器 | `AddSC019IndustryPanelTests.cs` | 单根、绑定、部分拒绝、回滚、层级 |
| 回归 | 既有 SC-010/011/013/018 测试 | 事件顺序、继承、关系、v5 迁移、M2 剧本不破坏 |

## 中文试玩交接（摘要）

完整步骤写入 `SC019_DEMO.md`；最低限度包含：

1. 打开 `FoundationScene`，Play，选择新游戏（M2/SC-013）。
2. 打开“产业”：确认资金 30、物资 12、情报 0；农庄/商馆空缺；晴信/千恵等六人可用。
3. 任命晴信→农庄、千恵→商馆；确认两人 `3/4`、剩余 1；第三人无法再占同一产业。
4. 结束月份；确认 1560-04 月结报告：资金 35、物资 13、情报 1；编年史“产业月结”含 30→35、12→13、0→1。
5. 在月末尝试“推进一天”和旅行；确认被拒绝、日期/地点不变；再结束月份成功。
6. 只保留商馆、解除农庄，连续结束月份直到物资不足；确认商馆“资源不足…停产”、资源不为负；重新任命农庄恢复。
7. 完成第一次交接（晴信→正綱）；确认农庄空缺、商馆（千恵）保留、正綱不自动接管；用正綱自己的预算任命一个职位。
8. 完成第二次交接（正綱→信正）；确认正綱职位解除、信正不自动接管、其他合法任命保留。
9. 保存→读档→结束月份；确认资源/任命/已结算月份恢复，且只结算一次；再读同一档重复，结果一致。
10. 记录 Console、EditMode、Validator、Play Mode、存档/读档与用户验收；不把协调方 Play Mode 当作 user acceptance。

**停止/报告条件：** 资源为负、同一月重复扣费/产出、跨月未被拒绝、旧家主职位未解除、新家主自动接管、读档后重复月结、存档无法保存、场景生成器部分写入、Console 红色错误。遇到即停止并报告，不继续“试玩通过”。

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| 收紧 `AdvanceDays` 破坏既有跨年测试 | 内部 `AdvanceDaysForLegacyFixtureSetup` 只用于夹具；生产 API 不变；新增测试覆盖拒绝路径 |
| 月结/事件/日期半提交 | 先纯计算，再排事件，再提交；快照恢复；测试失败/重复/越界 |
| v5 迁移丢失 SC-018 关系/历史 | 迁移克隆并逐项保留；专项测试比较数组/顺序/值；不追加编年史 |
| 失效任命导致无法保存 | 交接后、保存前、月结时三层清理；保存清理不写编年史；读档拒绝非法存档 |
| 场景生成器部分写入 | 单 Undo group、preflight、Validate、异常回滚、不重载场景 |
| 数值过松/过紧 | 首切片暂定值；试玩后由协调方决定是否调参，不在本切片加动态市场 |
| 误报 M3 完成 | 文档与报告明确“首切片”；不把两次交接当作完整 M3 验收 |

**回滚：** 代码改动可按文件回退；v6 存档不降级，保留 v5 原文件与 `.bak`；场景只由集成 worker 的 Undo 事务修改，失败不保存；不删除既有分支/checkpoint；用户验收前不做 git 集成。
