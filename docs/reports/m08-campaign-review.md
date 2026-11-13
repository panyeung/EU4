# M08 家族评价与百年名门 — 实现与验证说明

**范围：** `docs/UI_ROADMAP.md` Task 7 / GDD §10.3、§19.7 已批准部分。
**状态：** 代码评审、FoundationScene 接入与场景 Validator 已通过。2026-11-13 的 SC-013 Play Mode 通过真实月结到达 55/100 年评定，正文分块、滚动和确认继续流程通过。M08B 当前规则集的 20/1/200/1000 seed 分阶段产物已复核；无人管理基线没有样本到达第 50/100 年，经济长期稳定性仍是风险。周年磁盘存档/读档、未知来源旧档 Play Mode、M08B 端到端最终签收和最终用户里程碑验收仍待完成。

## 行为

- **55 周年阶段评价**：以战役真实开局日期为准，满 55 年的同日（1560-04-01 → 1615-04-01）出现。评价是非终局的；确认后可由「继续家族」继续游玩，不推进日期、不改变资源，也不把 Phase 1 `IsDemoComplete` 当作结局。
- **100 周年「百年名门」评定**：同一开局日期满 100 年（→ 1660-04-01）独立出现；与 55 周年评价分别持久化，可分别确认一次。
- **只触发一次**：`CampaignProgress` 为不可变对象；重复确认或提前确认返回失败且状态不变。
- **来源与开局日一致**：`CampaignProgress.CreateNew` / `TryCreate` 都按 `CampaignOriginCatalog` 校验「已知来源 ⇒ 开局日必须等于该剧本固定开局日」。`GameSession.InitSession` 使用传入的实际开局日（不再回查目录覆盖参数），因此世界时钟、战役开局日与 55/100 周年日期始终同源；矛盾组合直接拒绝，旧档「开局日期未知」路径不变。
- **回顾正文只呈现一次历史**：`CampaignReviewView.FormatBody` 按 GDD 10.3 七类各出一节，节内按日期排序并完整列出该类全部记录；不再先输出一条平铺时间线再重复分类，正文仍由可滚动 `ScrollRect` 承载，未引入截断上限。
- **旧存档迁移**：只有旧档仍携带可证明的剧本身份（家族 ID；SC-024 另需事件路线 `sc024`）且存档日期不早于该剧本固定开局日时，才推断开局来源。v1/v2 的默认名册、未知家族、或与固定开局日矛盾的日期都保留「开局日期未知」，永远不判定两个评价，也不补写历史事件。
- **时间不暂停**：评价是独立回顾覆盖层，不拦截 `AdvanceOneDay` / `TryAdvanceToNextMonth`。

## 数据流

1. 新局：`GameSession.InitSession` 把实际开局日与来源 ID 交给 `CreateCampaignProgress`，由 `CampaignProgress.TryCreate` 对 `CampaignOriginCatalog` 校验后记录；未知来源仍是显式「开局日期未知」。
2. 存档：schema 升至 v16；`SaveData.campaign` 新增 `startKnown / startYear.. / originId / stageReviewAcknowledged / centuryReviewAcknowledged`。
3. 读档：`SaveDataValidator` 校验日期与确认标记一致；`GameSession.TryApplySave` 先构建全部替换状态，失败时不改动现局。
4. 迁移：`SaveDataMigrator` 为 v1–v15 添加 `campaign`；仅在可证明时推断 origin，不重放月份、不写编年史。
5. 回顾：`GameSession.CampaignReview` → `CampaignReviewView.Build(...)`，只读取 `ChronicleEntries`、`MissionHistory`、`Succession`、`FamilyStandingState`、产业资源、已完成研究技艺、总功绩与军阶。

## GDD §10.3 类别与来源

| 类别 | 权威来源 | 缺失时 |
|---|---|---|
| 历代家主 | 继承编年史中 `HasHeadChange` 的真实交接 + 当前家主/世代数 | 明确说明「尚无家督更替记录」 |
| 家族财富变化 | 产业月结编年史（资金 before/after 与摘要中的物资/情报）+ 任务终局历史的 `FundsDelta/SuppliesDelta/IntelligenceDelta` | 说明尚无已结算产业或任务记录，不推算开局资产 |
| 忍术成就 | 研究完成编年史 + 已记录家族技艺 | 说明尚无完成研究 |
| 人物死亡与重要经历 | 任务终局历史的阵亡/负伤/任务经历、教育编年史、婚姻/收养/新生记录 | 说明不虚构人物经历 |
| 家族盟友和仇敌 | 人物关系编年史（稳定 NPC ID、前后值）+ 任务结算的关系变化（按日期）+ 家族名望状态中的当前稳定 ID 关系值；只显示稳定 ID 与正/负/零 | 说明游戏未定义正式同盟或敌友阈值，本评价不判定 |
| 重大历史事件 | 已提交事件的编年史 | 说明世界历史事件流尚未实现，不补写史实节点 |
| 家族最终地位 | 当前家族/家主/世代、名望、隐蔽度、资金/物资/情报、忍术数量、功绩/军阶 | 无 |

五项发展维度（家族名望、经济实力、忍术传承、政治影响力、家族稳定度）逐项显示来源事实。稳定度只使用「连续年数 + 已记录家督更替 + 在世成员」，不生成综合评分；`CampaignReviewView.ScorePolicyNote` 明确说明。

## 真实游玩来源逐类核对

| 类别 | 能否由实际游玩产生 | 产生入口 |
|---|---|---|
| 历代家主 | 能 | 退休/危机收养/摄政完成/继承确认都会经 `RecordSuccession` 写入带 `fromHeadId/toHeadId` 的编年史 |
| 家族财富变化 | 能 | 有任命的产业月结；或任意 M05/M06 任务终局的资金/物资/情报增量 |
| 忍术成就 | 能 | 家族研究完成（SC-021 或普通局的月度研究结算） |
| 人物死亡与重要经历 | 能 | M06 严重失败死亡/负伤、任务参与、年度培养、婚姻/收养/新生 |
| 家族盟友和仇敌 | 能 | 事件/对话改变 NPC 关系（带稳定 ID 与前后值）；M05/M06 结算改变势力关系 |
| 重大历史事件 | 能 | 月度事件提交（`ChronicleCategory.Event`） |
| 家族最终地位 | 能 | 当前名册、名望、隐蔽度、资源、技艺、功绩 |

结论：七类都能由现有权威来源在实际游玩中产生，因此本次不新增伪造内容或额外 persisted 记录；`MissingSourceNotes` 只用于确实尚无记录的空历史。世界历史事件流（1579/1600/1614 等固定史实节点）仍不在代码中，评价只列玩家已提交事件，并在缺失说明中明确这一限制。

缺失说明在 `CampaignReviewView.Finish` 一次性生成：只有当所有来源（编年史、任务终局、名望、研究）都提交后，仍没有任何「家族财富变化」事实时才写财富缺失说明；任务结算的资金/物资/情报增量因此不会被误报为缺失。其他类别说明同理在 Finish 统一判定。

## 独立评审修正（2026-11-12）

| 评审项 | 修正 | 覆盖测试 |
|---|---|---|
| 正文平铺时间线后又重复七个分类 | `FormatBody` 只保留一份按 GDD 顺序、节内按日期排序的完整分类正文 | `ReviewView_FormatBodyListsEachCategoryOnceInChronology`、`AddM08CampaignReviewPanelTests.Panel_RevealsAtMilestoneAndContinueAcknowledgesOnce` |
| 财富缺失说明早于任务增量 | 缺失说明移到 `Finish` 统一判定 | `ReviewView_WealthNoteIsDecidedAfterMissionDeltasAreKnown` |
| `TryCreate` 接受「已知来源 + 无关开局日」 | `CreateNew` / `TryCreate` 用目录校验来源与开局日一致；读档与存盘校验因此同样拒绝 | `CampaignProgress_RejectsOriginStartDateMismatch`、`CampaignSaveData_RejectsOriginAndStartDateMismatch` |
| `GameSession` 忽略 `startDate` 参数 | 改用传入的实际开局日，并经同一校验保持时钟/来源/周年一致 | `GameSession_CreateCampaignProgressUsesSuppliedValidatedStartDate`、`NonSc013Scenario_KeepsClockOriginAndAnniversariesConsistent` |

补充 EditMode 覆盖：v16 `campaign` 字段缺失拒绝（`V16Presence_RequiresTheCampaignBlock`）、标题读档提示的剧本来源/开局日交叉核对（`TitleContinueHint_ShowsPersistedOriginAndStartDate`）、1660 年阶段评价未确认时仍先于百年评定（`StageReviewPrecedesCenturyReviewAt1660WhenStillUnacknowledged`）、面板由 `SessionStateChanged` 自动显示（`CampaignReviewPanel_AppearsOnSessionStateChangeWithoutManualRefresh`）。同时删除未使用的 `CampaignDevelopmentDimension.HasFacts`。

## 变更文件

新增：

- `Assets/Scripts/Domain/CampaignProgress.cs`
- `Assets/Scripts/Application/CampaignReviewView.cs`
- `Assets/Scripts/Application/GameSession.M08.cs`
- `Assets/Scripts/Persistence/CampaignProgressSaveData.cs`
- `Assets/Scripts/Presentation/CampaignReviewPanel.cs`
- `Assets/Scripts/Editor/AddM08CampaignReviewPanel.cs`
- `Assets/Tests/EditMode/CampaignMilestoneTests.cs`
- `Assets/Tests/EditMode/AddM08CampaignReviewPanelTests.cs`
- 对应 `.meta`（GUID 见文件）

修改（增量）：

- `Assets/Scripts/Application/GameSession.cs`：新局来源、`CaptureSave`、`TryApplySave`、读档消息重置。
- `Assets/Scripts/Persistence/SaveData.cs`：`campaign` 字段、默认值、presence probe。
- `Assets/Scripts/Persistence/SaveDataValidator.cs`：schema v16、`campaign` 校验与 presence。
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`：v15 分支、v16 迁移、v1–v15 的 origin 推断、`campaign` 的旧版拒绝。
- `Assets/Scripts/Presentation/TitleScreenController.cs`：继续提示显示真实剧本来源/开局，或明确「开局日期未知」。
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`：新增 M08 面板存在性与引用检查；保留全部既有 M02–M07 检查。
- `Assets/Tests/EditMode/{SaveDataMigratorTests,FamilyDoctrineSaveTests,NpcAutonomySessionTests}.cs`：旧版 raw JSON fixture 补齐 M08 字段的移除，保持原测试意图。

## 场景接入（由指定集成者执行）

未创建或修改任何场景/Prefab。代码已提供事务式生成器：

1. 在已打开 `FoundationScene` 的 Editor 中运行 `Tools/Sengoku Chronicle/Add M08 Campaign Review Panel`。
2. 生成器在现有 `Canvas` 下单根创建 `M08CampaignReviewRoot`，包含全屏 `M08CampaignReviewPage`、居中卡片、可滚动正文、继续按钮与状态行，并在保存前验证；失败经 Undo 整体回滚，不重载场景。
3. 生成器已绑定现有 `GameSession`、面板引用、正文 `Text` 与继续按钮；页面初始隐藏，待评价出现时由 `CampaignReviewPanel` 自动显示。
4. 运行 `Tools/Sengoku Chronicle/Validate Foundation Scene`；M08 检查会验证面板唯一性、根/卡片包含关系、正文滚动结构、按钮/文本引用与字体。
5. 如场景已含同名根或面板，生成器拒绝重建；先人工检查再决定，不自动覆盖。

面板在无待确认评价时自动隐藏；确认后由 `GameSession` 持久化并关闭本评价。

## 2026-11-12 Unity Editor 验证

- Existing Editor PID `126040` runs Unity `6000.6.6f1` with `-projectPath C:\Users\panye\My project`. Recompile previously completed successfully. The current Editor process still responds to Windows, but Pipeline `editor_status` fails because the Editor main-thread operation times out after 30 seconds; recent `Logs/Editor.log` entries confirm the timeout. The non-main-thread `console_status` command returned a cached ground-truth sample at 2026-11-12 18:50:39 UTC with 2 errors, 42 warnings, 20 logs, `compilationFailed=false`, and `compiling=false`; it is stale. The `console` buffer reports 21 retained errors, 138 warnings, and 221 logs through sequence 12404, including test rollback errors, the first M08 generator validation failure, and subsequent Pipeline timeouts through 19:15:23 UTC. These retained counts do not establish current Editor Console totals. Current Editor-side scene state remains unreadable through `editor_status`.
- After explicit `unity command recompile` / `recompile_status=completed`, the focused leap-day regression passed 1/1.
- The latest full EditMode XML, `Logs/m08-full-editmode.xml`, reports 1772 passed, 0 failed, 0 inconclusive, and 0 skipped (248.29 seconds). This supersedes the earlier 1771/1771 result. The XML does not establish scene or Play Mode behavior.
- The first `Tools/Sengoku Chronicle/Add M08 Campaign Review Panel` attempt failed validation with `Campaign review continue button or label is missing, unbound or outside the card.` and rolled back without saving. The generator was then corrected to reset the newly parented root's local position; the read-only code review (`task_6b0ed041c2ed`) passed, and a regression test reproducing a Canvas pivot of (0,0) is included in the 1772 tests. The corrected menu generator has not yet been rerun because Pipeline calls are timing out.
- The saved `Assets/Scenes/FoundationScene.unity` still has no `M08CampaignReviewRoot` or `CampaignReviewPanel` entries; its size is 2,066,052 bytes and timestamp remains 2026-11-12 00:44:21 local time. It has not been saved by this verification attempt. The current disk scene therefore does not pass the M08 presence checks.
- Play Mode, user-visible review flow, save/load at the 55- and 100-year milestones, and unknown-origin save behavior remain unverified. No Play Mode claim follows from the EditMode result.
- 静态检查（非 Unity 编辑器）：用 Unity 自带 Roslyn 编译器 + 本机 Unity 6000.6.6f1 程序集与 NUnit 对 `Assets/Scripts` 与 `Assets/Tests/EditMode` 做了一次合并编译，除把全部 asmdef 合并导致的已知假阳性（`SaveSlotStore.cs` 中 `Application.persistentDataPath` 只在合并编译下与 `SengokuChronicle.Application` 命名冲突）外无语义错误；该文件单独编译通过。这不能替代 Editor 编译，最终以 Editor 结果为准。
- 本轮同时修正两处会导致 Editor 编译失败的 M08 缺陷：`CampaignReviewPanel.cs` 缺少 `using SengokuChronicle.Domain;`（`CampaignReviewResult`），`AddM08CampaignReviewPanel.cs` 的 `CreateText` 调用多传一个参数。
- 原计划的场景与 Play Mode 验收覆盖 1560 新局、1615-04-01 阶段评价、评价确认后继续、评价状态存读档、1660-04-01 百年评定、未知来源旧档不显示评价，以及 Console 计数与失败存档保留检查；以上运行时场景目前都未完成验证。

### 2026-11-12 20:01 UTC 只读诊断补记

- `unity pipeline list` 在 20:01 UTC 仍列出项目 Editor PID `126040`，端口 `7801` 可达。`editor_status` 再次因 Unity 主线程操作 30 秒超时而失败。`Get-Process` 报告进程 `Responding=True`，这不足以证明 Editor 可执行场景操作或能安全重启。
- `console_status` 返回缓存样本仍是 2026-11-12 18:50:39 UTC 的 `2 errors / 42 warnings / 20 logs`；缓存计数为 `24 errors / 138 warnings / 221 logs`，cursor `12407`。缓存样本已过期，计数不能代表当前 Console。`Logs/Editor.log` 最后写入时间为 19:56:07 UTC。
- 无截图窗口树显示标题仍带 `*`，没有显示模态对话框。磁盘 `FoundationScene.unity` 为 `2,066,052` 字节，最后写入时间为 2026-11-12 10:44:21 UTC，未找到 `M08CampaignReviewRoot` 或 `CampaignReviewPanel`。本次没有保存场景或生成截图。
- 修复后的独立复审尚未收到结果。Play Mode 和用户可见 M08 流程仍未验证。

### 2026-11-12 20:11 UTC 保存尝试

- 用户明确授权保存当前 `FoundationScene`。保存前将磁盘场景复制到 `%LOCALAPPDATA%\ShadowClanBackups\FoundationScene.pre-M08-save-20261112.unity`。备份与保存前磁盘文件的 SHA-256 都是 `8C59A5F6C36AC5944735B6E239594004FD0E1B7CF1B9730F05F0939A419DF0B1`，大小均为 `2,066,052` 字节。此备份不包含 Editor 内存中尚未保存的改动。
- `unity command save_scene --path Assets/Scenes/FoundationScene.unity` 在 30 秒后超时。无截图的 `Ctrl+S` 和 File 菜单输入没有确认保存。窗口标题仍带 `*`。
- 保存尝试后，磁盘场景和外部备份的 SHA-256 仍相同，文件时间戳仍为 2026-11-12 10:44:21 UTC。没有确认 Editor 内存改动已写入磁盘。没有运行 M08 场景生成器或 Play Mode，也没有关闭 Editor。
- 在 Unity 主线程恢复响应并确认场景保存前，不应重启 Editor 或直接用磁盘文件覆盖当前场景。M08 场景接入和 Play Mode 仍待办。

该运行验证只证明编译与 EditMode。它不构成场景接入、Play Mode 或 M08 里程碑验收通过。
## 2026-11-12 23:57 UTC 最新场景与 Play Mode 复核

本节结果更新并取代上文 20:01/20:11 UTC 关于“场景无 M08 面板、Validator 尚未重跑、Play Mode 未运行”的状态；此前记录保留作故障历史。

- 当前项目 Editor 为 PID 63756、Unity `6000.6.6f1`，`-projectPath C:\Users\panye\My project`。M08 脚本在此次运行期间可编译；此前重编译成功。最新全量 EditMode 文件 `Logs/m08-full-editmode.xml`（2026-11-12 18:43:08 UTC）记录 1772 passed、0 failed、0 inconclusive、0 skipped。
- 修复后的 M08 生成器已接入并保存到 `Assets/Scenes/FoundationScene.unity`。退出 Play Mode 后于 23:57 UTC 运行 `Tools/Sengoku Chronicle/Validate Foundation Scene`：全部清单项 PASS，含 M08 overlay hierarchy/bindings/layout；报告为 `Loaded: True; Dirty: False`，无 `ERROR:`。Play Mode 内运行该全场景 Validator 会因运行时面板隐藏状态而报告多项根对象 active 错误，因此正式 Validator 结论采用停止 Play Mode 后的结果。
- Play Mode 经标题页真实路径“开始新游戏 → 固定三代剧本（SC-013）”建立新内存局，未保存、未覆盖磁盘存档。临时运行时脚本只为测试将 `WorldClock` 日期分别设到 1615-04-01 与 1660-04-01，并刷新现有 `CampaignReviewPanel`；实际调用场景绑定继续按钮的 `onClick`。结果：`五十五周年 · 阶段评价` 与 `百年名门 · 百年评定` 分别出现；每次确认后对应 acknowledgement 标志写入 `CaptureSave()`，面板关闭，且第一次确认后仍可出现第二次评价。新局冒烟结束时 `TryApplySave()` 成功恢复测试前内存快照。此前在标题菜单点击“继续游戏”加载的 SC-024 存档上运行过一次同样的内存测试，结果确认标记通过但恢复快照返回失败；没有写入磁盘。随后停止并重启 Play Mode，选择“开始新游戏 → 固定三代剧本（SC-013）”重新验证，最终测试快照恢复成功，Editor 后续停止。
- 冒烟的日期通过内部时钟恢复 API 临时跳转；它没有真实推进 55/100 年，也没有覆盖周年点的磁盘存档/读档、未知来源旧档运行路径、长周期稳定性或内容可理解性验收。因此这些 M08 要求仍未验证。
- 首张临时 PNG 在标题主菜单仍覆盖游戏界面时捕获，未能证明回顾面板的无遮挡视觉布局。检查后，删除该精确路径的请求 `Remove-Item -LiteralPath 'C:\Users\panye\My project\Assets\Logs\verify\_scratch\m08-stage-review.png' -Force` 两次均被自动审核以 `blocked by policy` 拒绝；没有尝试绕过。该 PNG 仍留在工作树，需手动清理。后续正确 SC-013 Play Mode 复核使用文本/层级证据，不再截图。
- 当前 Editor 已退出 Play Mode，Unity `editor_status` 为 `ready`、`compiling=false`；`FoundationScene` 为 `dirty=false`。23:57:26 UTC 的 Console ground-truth 为 0 errors / 0 warnings / 2 logs（验证器输出）；冒烟后一次采样为 0/0/0。未据此声称旧 Console 缓冲区的累计计数为零。
- 实现复审 `task_0024410b82ff` 为 PASS，提出 5 项非阻断观察；完整范围和观察见本次 sign-off 记录。当前分支存在大量 M02–M08 混合 WIP，本轮未提交或推送；未执行最终 milestone merge/tag。

## M08A 家族人口动态切片（2026-11-12）

本节只记录 M08A（年度自然死亡 + NPC 自主婚姻 + 死亡/婚姻编年史）的实现与验证。它不改变上文 M08 Campaign Review 的结论，也不表示 M08 或整个项目已完成。

### 范围与规则

- 新增纯规则 `FamilyLifeCycleRules`（年度边界、六段死亡率、稳定 `PersonId` 顺序、婚姻候选过滤、15%/人/年）与 `NpcMarriageProposal`。
- 新增 `FamilyMarriageSettlement`（稳定配偶 ID/姓名/相反性别/成年生年/`Spouse` 关系与成长行）并复用 `FamilyBirthNaming` 的姓名表与唯一性规则。
- `GameSession.TryAdvanceToNextMonth` 的 RNG 顺序固定为：到期出生/受孕 → 年度死亡 → 自主婚姻。死亡候选在出生前截取，新生儿不进入同一次年界死亡列表；家主死亡只调用一次既有 `BeginDeathSuccession`，产生待决继承时跳过婚姻。
- 死者保留人员、成员、家谱与关系事实；提交后清理产业任命与进行中的日行动，未完成孕期在克隆快照内取消。死亡/婚姻编年史先按下一序号整体校验再追加；任一失败恢复日期、继承世界、成长、日行动、产业、妊娠、RNG 与编年史。
- 编年史新增 append-only `ChronicleCategory.NaturalDeath = 8` 与来源 `natural-death`，既有枚举数值不变。家族编年史显示 `人物离世 · 享年 N 岁`；Campaign Review 的「人物死亡与重要经历」读取真实死亡/婚姻行；现有家谱已显示 `配偶` 边与 `已故` 标签，未新增面板、场景根或立绘。
- 存档未新增字段：生成配偶复用既有人员/生命/成员/成长/日行动/亲属记录，死亡复用 `Alive`。schema 保持 v16，迁移链未改。

### 证据（分开记录）

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | 成功；`compilationFailed=false` | 既有 Editor（PID 63756，`-projectPath C:\Users\panye\My project`）`unity command recompile` |
| 聚焦 EditMode | `FamilyLifeCycleRulesTests` 27/27、`FamilyMarriageSettlementTests` 8/8、`GameSessionFamilyLifeCycleTests` 10/10、`FamilyLifeCycleChronicleTests` 4/4 | Unity Test Runner（Pipeline `run_tests --mode editor`） |
| 相关回归 | `FamilyPregnancyRulesTests` 16/16、`FamilyPregnancySessionTests` 5/5、`GameSessionSaveTests` 18/18、`SuccessionSaveTests` 17/17、`GameSessionSuccessionTests` 15/15、`FamilyChroniclePanelTests` 21/21、`CampaignMilestoneTests` 29/29、`SaveDataMigratorTests` 26/26、`SaveDataValidatorTests` 49/49 | 同上 |
| 全量 EditMode | 1823/1823 passed，0 failed / 0 skipped / 0 inconclusive | 本轮全量运行；原始 JSON 保存在本机忽略文件 `Logs/m08a-full-editmode.json` |
| succession sweep | `violations=0`、`self-test=PASS (4/4 broken traces caught)` | `unity command run_script --file .opencode/skills/succession-invariants/scripts/SuccessionSweep.cs --entry SuccessionSweep.Run` |
| 场景 Validator | `Loaded: True; Dirty: False`，清单全部 PASS，`RESULT: PASS; Errors: 0` | 停止 Play Mode 状态下运行 `Tools/Sengoku Chronicle/Validate Foundation Scene` |
| Console | 最新窗口 2 条 error，均为既有家族关系负向测试的预期 `编年史预备记录序号无效` 日志；`compilationFailed=false` | 全量 EditMode 之后 `console` |
| Play Mode | **未运行** | 本切片实施者未进入 Play Mode；真实月结中的可见死亡/婚姻、家谱与读档由 lead 的 Play Mode 门验证 |

### 限制

- 本轮没有推进真实 55/100 年、没有做 100 年 × 1000 种子长期模拟，也没有实现 M08B 价格指数；不声称 M08 §14.2 完成。
- 全量 EditMode 通过不等于运行时行为通过；上表把编译、EditMode、Validator、Console 与 Play Mode 分开记录。
- Play Mode 的默认种子未必在同一年界命中 15% 婚姻；确定性 EditMode 已分别覆盖命中与未命中。以代码注入种子启动的会话不得当作真实标题页 Play Mode 路线。
- 当前分支仍有 M02–M08 混合 WIP；本切片可隔离文件的提交状态见交付记录。未创建 milestone checkpoint、未合并 `main`、未打 tag。

## 2026-11-13 真实 55 周年 Play Mode 复核

本节更新 2026-11-12 的日期跳转冒烟记录。此前报告仍保留为历史，不作为本次真实月份推进的证据。

- Unity Editor PID `63756`，项目路径为 `C:\Users\panye\My project`，场景为 `Assets/Scenes/FoundationScene.unity`，处于 Play Mode。通过标题界面进入“开始新游戏 → 固定三代剧本（SC-013）”，实际开局日为 `1560-04-01`。未保存场景或写入用户存档。
- 通过游戏内月份结算按钮推进到 `1615-04-01`。运行助手对事件选项选择当前可见的首个可用项，对继承选择首个合法候选人；日期由既有月份结算改变，未调用日期跳转或模拟器继承逻辑。此路线验证真实会话状态，但并非逐项手动操作的真人试玩。
- 55 周年评价按时出现。标题为“五十五周年 · 阶段评价”，剧本来源为 SC-013，记录开局日、55 周年日、100 周年日和评价日。正文包含实际家督更替、产业结算、家人经历、关系变化、事件与当前家族状态；评价显示第 4 代家主“铃”及 10 名在世成员。
- 目视复核发现正文视口空白，标题、上下文和“继续家族”按钮可见。运行时正文长 89,212 字符，TextGenerator 产生 317,860 个顶点；正文矩形为 1,048 × 31,820，视口为 1,056 × 402。单个旧版 uGUI Text 超出可显示网格范围，故本次验收失败。修复任务 `task_c764a3661425` 已交 OpenCode2，要求保留全部记录并能滚动浏览。
- 临时画面截图位于 `Assets/Logs/verify/_scratch/m08-55yr-review.png`。删除命令被自动审核拦截；文件尚未删除，需手动处理。该 PNG 不能作为持久化验收证据。
- 55 周年确认、确认后继续、100 周年评价、周年存档/读档、未知来源旧档与测试后 Console 计数均未验证。Play Mode 会话尚未退出；没有保存场景。

## 2026-11-13 M08B 长期模拟产物复核

- `Logs/verify/m08b/summary-1-1000-100y.json` 请求种子 1–1000、每局 100 年。该产物记录 605 局灭亡、395 局停在待处理的危机继承，完成数为 0；第 10 年到达 194/1000，第 25 年 2/1000，第 50 与第 100 年均为 0/1000。记录的粮荒、经济崩溃与极端通胀各为 999/1000，关系异常和世界推进死锁为 0。
- 产物中的 runner `sourceSha256` 与当前 `M08BLongTermSimulationRunner.cs` 相符。产物记录 96 个规则输入；独立按当前 96 个输入重算的清单摘要与产物 `ruleInputSha256` 不同。该手动重算尚未由 Unity runner 复核，因此这批指标不能作为当前规则集的最终 1000 种子证据，需先用 runner 复核输入摘要并按当前规则重跑。
- 本次只读旧产物，没有启动长周期运行。M08 §14.2 的当前规则集 1000 种子验收仍未完成。

## 2026-11-13 SC-013 55 周年回顾复测（未通过）

- 复测前确认 Unity `6000.6.6f1` PID `63756` 的 `-projectPath` 为本项目；FoundationScene 已保存状态为 `dirty=false`。Play Mode 通过标题页“开始新游戏 → 固定三代剧本（SC-013）”建立，开局日 `1560-04-01`，没有读写用户存档。
- 使用 `Logs/verify/M08RuntimeCampaignProgress.cs` 的 `Run`，连续三次调用；该助手实际点击月结 UI，按当时可见的首个事件选项和首个合法继承候选人处理。分段结果：60 个月到 `1565-04-01`（20 次事件选择、2 次继承选择）；60 个月到 `1570-04-01`（0、2）；45 个月到 `1574-01-01`（0、1）。
- 第三代结局面板出现（“第三代结局 · 旧交来援”）；家族编年记录 23 岁家主“養子 宗次”于 `1574-01` 自然离世，且没有合法继承人。面板没有到达 55 周年评价，因此本次不能验证修复后的长正文运行时显示、滚动或“继续家族”。这与本报告前一节记录的 55 周年成功路线不一致；该先前结果仍是其自身运行证据，但默认 SC-013 路线的当前复现不稳定，须解释或提供可重复的存活路线。
- 本次 Editor `console_status` 最新采样：`compilationFailed=false`、`compiling=false`、`consoleErrors=0`、`consoleWarnings=0`；自本会话游标以来新增 Console 项为 0。停止 Play Mode 后 `editor_status=ready/stopped`，FoundationScene 仍 `dirty=false`。没有保存场景或截图。
- 结论：Campaign Review 长正文渲染修复仍未通过实时 55 周年验收；EditMode 与只读代码审查结果不能替代本次未到达目标面板的运行路径。

## 2026-11-13 SC-013 百年评定 Play Mode 复核（通过）

- 在同一新开 SC-013 会话中继续此前的 55 周年路线；Unity Editor 为 PID `63756`、Unity `6000.6.6f1`、项目路径 `C:\Users\panye\My project`。阶段评价已确认并继续，之后从 `1657-01-01` 的真实月份结算继续推进。此会话未写入用户存档。
- 1657 年诊断时家族有 61 名在世成员、34 名可任职成年人，家庭粮食为 0、连续短缺 14 个月，农庄空缺。先前诊断误报候选列表与在世成员不匹配，原因是临时诊断脚本去除 UI 名称前缀时多去掉了 `person.`。修正脚本后 `IndustryService.CanWork` 的 34 名成年人全部与 34 个农庄 UI 候选匹配；未发现项目 UI 缺陷。
- 通过现有产业 UI 的真实候选按钮回调重新任命农庄负责人，并点击真实月末按钮推进至 `1660-04-01`。本段为 39 次月结，触发 1 次继承选择、0 次事件选项；负责人自然死亡后通过面板重新任命共 3 次。到达“百年名门 · 百年评定”可见暂停状态，家族仍存续。
- 百年评定正文 253,167 字符，拆为 65 个 Text 块；全部块活动，按顺序拼接与原文完全相等。最大块 3,998 字符、最大 TextGenerator 顶点数 14,280，总计 884,460 顶点。ScrollRect 纵向开启、横向关闭，内容高 83,745.01、视口高 402；运行时滚动诊断确认首块和末块能分别进入视口。
- 通过游戏继续按钮“确认百年名门评定并继续”确认。确认后日期仍为 `1660-04-01`、`HasPendingCampaignReview=false`、评定面板隐藏；SaveData 中 `stageReviewAcknowledged=true` 且 `centuryReviewAcknowledged=true`。这是运行中存档对象的读回，不等同于磁盘存档/读档往返。
- `editor_stop` 后 Editor 为 `ready/stopped`；FoundationScene `dirty=false`。最终 `console_status` 报告 `compilationFailed=false`、`compiling=false`、当前采样错误/警告/日志均为 0，自游标 `5451` 起没有新增 Console 项。累计缓存计数仍包括旧历史，不代表本轮新增错误。
- 本轮没有生成新截图。已删除项目内 `Assets/Logs/verify/_scratch/20261113-m08-rendering-review.png`、`m08-55yr-review.png` 及其 `.meta`，并清理空的 `Assets/Logs` 与目录 `.meta`。Computer Use 临时缓存目录 `C:\Users\panye\AppData\Local\Temp\orca-computer-use` 中仍有 12 个 `*-screenshot.png`；批量清理被自动审核拦截，我没有改用其他路径绕过。
- 结论：本次运行通过 55 年与 100 年评定正文显示数据、滚动范围和继续确认流程。M08B 1000 seed 结果在后续章节独立复核；周年评定的磁盘保存/读档仍未验证。

## 2026-11-13 M08B 当前规则集 1000 种子阶段复核

- 使用 Unity `6000.6.6f1` 当前 Editor（PID `63756`、本项目）运行 `M08BLongTermSimulationRunner`。首次 `RunStageOneThousand` 使用默认 CLI 30 秒时限；Pipeline 请求报超时，但 Editor 继续执行并完成 1000 个实际 seed，日志记录运行 `429844ms`、`reused=0 ran=1000`。这两次请求超时分别在 Console 留下 `/api/exec` 的 30 秒主线程超时日志，已单独读取并在记录后清理临时 Console 缓冲。
- 随后用 `unity command --timeout 900 run_script ... RunStagedBatches` 成功验证整套可续跑入口；返回 20 个 5 年、1 个 100 年、200 个 100 年和 1000 个 100 年阶段摘要。所有分段摘要的 `sourceSha256` 均为当前 runner `fe7eca09…b8e87`，规则输入清单为 96 个文件、`ruleInputSha256=b3dbfb68…77bfc2`；每个阶段均通过当前 runner 校验并复用，不再重跑。seed CSV 行数为 `20/20`、`1/1`、`200/200`、`1000/1000`；检查点 CSV 为 `80/80`、`4/4`、`800/800`、`4000/4000`；摘要中终止分类分母与对应 seed 数完全一致，四类以上 §14.2 指标字段齐全。
- 阶段结果：20 个短跑（5 年）为 Completed 16、Extinction 2、PendingSuccession 2；单 seed 100 年样本灭族；200 seed 全长为灭族 121、待处理继承 79；1000 seed 全长为灭族 605、待处理继承 395，没有 Completed、事件阻塞、超时、日期越界或模拟器异常。模拟器只自动处理已批准的稳定事件选项与确定性合法继承/摄政；它没有自动任命产业负责人，结果代表无人管理基线，不是玩家策略的成功率。
- 200 seed 检查点：第 10 年 `46/200`，第 25/50/100 年均为 `0/200`；累计粮荒、经济崩溃、极端通胀均为 `200/200`，产业停摆 `0/200`，关系异常种子 `0/10`，关系不变量违规 `0/200`，推进死锁 `0/200`，停止时继承人断档 `79/79`。事件频率为 `2974/18228`，即每千月 163 个事件。
- 1000 seed 检查点：第 10 年 `194/1000` 到达且存活，第 25 年 `2/1000`，第 50/100 年 `0/1000`。seed 级累计粮荒 `999/1000`、经济崩溃 `999/1000`、极端通胀 `999/1000`；产业停摆 `0/1000`、关系异常种子 `0/48`（48 个有可采样关系的种子）、存档关系不变量违规 `0/1000`、世界推进死锁 `0/1000`。停止状态下继承人断档为 `395/395`。全程事件频率为 `14955/88836` 个已模拟月份，即每千月 168 个事件。
- 第 10 年检查点样本的家族人数范围 1–8、均值 2、中位数 1；平均年龄样本为 19.4–80.0 岁、均值 44.1 岁；财富样本均为 29。第 25 年两局样本均为 1 人，财富仍为 29。50/100 年没有存活样本，相关人口、年龄、财富及经济指标分母为 0，不得解读为零值或通过。
- 重新执行 `RunStagedBatches` 后 Editor 为 `ready/stopped`、`compilationFailed=false`。两条 30 秒 Pipeline 超时日志已留存为运行证据后清空，最终 Console 样本为 0 errors / 0 warnings / 0 logs；未运行 EditMode、场景 Validator 或另一次 Play Mode。
- 结论：分阶段批处理与当前规则摘要/CSV 数据完整性通过。结果暴露无人管理基线下极高粮荒/通胀及无 50/100 年生存样本；GDD/计划没有规定经济平衡阈值，这不是擅自改规则的授权。M08B 最终验收仍待全量 EditMode、场景 Validator、真实 UI 存读档、Play Mode 端到端和产品层生存策略决策。

## 2026-11-13 M08B 当前 Editor 端到端复核

- Unity Editor PID `63756`、Unity `6000.6.6f1`、项目路径 `C:\Users\panye\My project`。在当前 Editor 安静重跑全量 EditMode 后结果为 `1943/1943 passed`、0 failed、0 skipped、0 inconclusive，耗时约 206 秒。首轮曾因 Pipeline 主线程超时污染测试 SetUp 而得到 1942/1943；该轮不作为最终结果。后续安静重跑的全量结果为准。
- 测试结束时 Console 曾有两条由故障注入回滚测试预期产生的错误日志；记录后清空。Play Mode 停止后的最终 `console_status` 为 0 errors / 0 warnings / 0 logs，`compilationFailed=false`、`compiling=false`。这表示当前缓存计数为零，不把测试故障注入日志误报为运行期游戏错误。
- 在停止状态运行 `Tools/Sengoku Chronicle/Validate Foundation Scene`，当前 `Logs/SceneValidationReport.txt` 结尾为 `RESULT: PASS; Errors: 0`。该操作未保存或修改场景；最终 Editor 状态为 `ready/stopped`，活动场景仍是 `Assets/Scenes/FoundationScene.unity`，`dirty=false`。
- 真实 UI Play Mode 从标题页开始新局并选择 SC-013，使用地图到达热田。4 月至 5 月月结显示六名在世家族成员需粮 2、购买 2、资金支付 2，粮食库存从 2 到 0；随后通过真实保存按钮写出 v17 存档。磁盘 JSON 读到日期 `1560-05-01`，市场状态为粮食库存 0、粮食价 1000‰、物资价 900‰、已结算月索引 18723、连续短缺 0、综合指数 950。推进一天后使用真实读档按钮，游戏状态恢复到 1560-05-01，位置热田、资金 28、物资 12；保存/载入 UI 均显示成功。
- 读档后通过产业面板任命明智千恵负责农庄，再真实结束 5 月。6 月 1 日的产业报告显示农庄粮食毛产出 +4、物资毛产出 +4、维护消耗 1 物资，并结算为 +1 资金/+3 物资；六人需粮 2，市场报告列出购买 2、资金支出 2，粮食库存从 0 到 2，粮食价格 1000→900‰，物资价格 900→810‰，短缺月数保持 0。UI 资源文本同时显示资金 27、粮食 2、物资 15。该路线验证了真实月结、产业供给、家庭需求、价格更新和 v17 UI 存读档，不代表无人管理的 100 年经济平衡。
- 这是面向市场规则的短期真实运行。结束 5 月后还有一项 SC-013 事件待处理，本次未继续事件路线。此前 1000 seed 无人管理基线仍是 605/1000 灭族、395/1000 停于待选继承，0/1000 到达 50 或 100 年。测试和这段短 Play Mode 不改变该风险，也没有批准任何经济平衡阈值。
- Play Mode 结束后恢复了测试前的用户存档与备份。当前 `save-slot-1.json` SHA-256 为 `1E61ED9CAB4A5F3E5FBCCE5EDA4BD24586BE5A072EA5B749CCB3C1E280BCB92C`，`.bak` SHA-256 为 `F1C0630E5DD91BAA0B18607CA4367956DB637A8BB4DD6DA21D1E79212BDA4CBE`；二者分别与测试前复制的恢复件逐字节相同。恢复副本保留在系统临时目录 `m08b-save-backup-20261113-054506`。
- 本轮没有截屏。临时 Computer Use 缓存内此前遗留的 12 个截图未被本轮创建；批量删除曾被自动审核拦截，仍需用户手动清理。
- 结论：全量 EditMode、当前 FoundationScene Validator、短期真实 SC-013 市场/产业月结及 v17 UI 存读档通过。M08B 的无人管理长周期生存风险仍未解决；不据此宣告 M08B 最终里程碑验收或合并/tag。
