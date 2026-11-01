# M7 设计：功名晋升（Merit & Promotion）

日期：2026-11-01
状态：已批准（用户选择方向 1；沿用 `feature/m5-save-slot` 分支与本工作树）
前置：M1–M6 全部已接受；`demo-M6` 已合并并推送。Phase 2 首个里程碑。

## 目标

让「功绩」从单纯数字变成可见的成长反馈：

1. 纯 C# 晋升规则：按总功绩划分四个军阶（游戏化简化设定，不代表史实，不指涉具体历史官职）。
2. 地图上常驻一行「功名」文本：当前功绩、当前军阶、下一军阶及所需功绩。
3. M6 演示完成摘要行同时显示最终军阶。

## 设计

### Domain：`PromotionRules`（纯静态，无 MonoBehaviour）

| 军阶 | 枚举 | 名称 | 起始功绩 |
|---|---|---|---|
| 1 | `Ashigaru` | 足轻 | 0 |
| 2 | `Banto` | 番头 | 5 |
| 3 | `Kumigashira` | 组头 | 10 |
| 4 | `Bushogashira` | 武士头 | 20 |

- `RankFor(int merit)`：负数按 0 处理；返回达到的最高军阶。
- `ChineseName(rank)`、`MinimumMerit(rank)`。
- `TryNextRank(rank, out next, out meritNeeded)`：顶级返回 false。
- 阈值以阶段数据（不可变数组）表达，测试直接读取，不散落魔法数字。
- Phase 1 满功绩恰为 20（10+5+5）：完成三项任务即达最高阶，作为演示的完美收尾。

### `GameSession` 派生属性

`Rank => PromotionRules.RankFor(Merit)`；`RankName`。不新增状态字段，**存档格式不变**（军阶由任务阶段推导的 merit 派生，读档不可能伪造军阶或重复领奖）。

### Presentation：`RankPanel`

- 与 `DemoSummaryPanel` 同模式：挂在常活跃的 GameSession 对象上，幂等 `Refresh()` + `Update()` 轮询；`Bind(session, text)` + 空会话保护。
- 文本（单行，可换行到两行）：`功名：{Merit} · {RankName}（下一阶 {NextName} {Needed}）`；顶级显示 `功名：{Merit} · {RankName}（最高）`。
- 布局：`M2WorldMapRoot/M7RankText`，中心锚点 (-450, 100)，尺寸 240×40，字号 11——左上安全区，验证器对 TitleText/LocationMarkerText/M5 列/按钮逐一做重叠检查；插入位置在 `TownMenuRoot` 之前。

### `DemoSummaryPanel` 文案升级

`Phase 1 演示完成。总功绩：{0} · {1}。`（{1}=军阶名）。仍由 `GameSession.IsDemoComplete` 门控。

### Editor 更新器：`AddM7RankPanel`

沿用 M5/M6 事务式模式：一个 Undo 组、先验证后保存、失败回滚、不重载场景；创建 `M7RankText`、在 GameSession 对象上挂 `RankPanel` 并绑定；`Validate` 检查唯一性、父级、兄弟次序（TownMenu 之前）、包含与相邻控件重叠；菜单 `Tools/Sengoku Chronicle/Add M7 Rank Panel`；`RunBatchIntegration()` 批量入口。

### Validator

`FoundationSceneValidator` 增加 `FOUND RankPanel`、绑定检查与 `AddM7RankPanel.Validate` 调用。

## 明确不做

- 不新增 merit 来源、不改三项任务的奖励数值、不新增存档字段或版本。
- 不做晋升动画/音效/弹窗；不做历史官职考据（游戏设定，文案注明简化）。
- 不新增 package；uGUI + LegacyRuntime 字体。

## 测试

- `PromotionRulesTests`：0/4/5/9/10/19/20/30 与负数的边界映射；名称与最小值一致性；`TryNextRank` 逐级与顶级返回 false。
- `RankPanelTests`：新局文本含「足轻」；完成态（阶段 4/2/3）含「功名：20 · 武士头（最高）」；空会话不崩溃。
- `DemoSummaryPanelTests` 扩展：完成文案含军阶名。
- `AddM7RankPanelTests`：preview-scene 结构/绑定/幂等/故障注入回滚/重叠拒绝/preflight。

## 验收路径（用户 Play Mode）

1. 打开 FoundationScene，清 Console，进入 Play → 标题 → 新游戏。
2. 地图左上出现功名行：`功名：0 · 足轻（下一阶 番头 5）`。
3. 完成密书任务（+5）→ 军阶变为番头；完成市场清点（+5）→ 组头；完成补给短缺（+10）→ 武士头（最高），同时演示摘要行显示 `总功绩：20 · 武士头`。
4. 存档 → 重启 Play → 继续 → 功名行与摘要显示军阶不变；重复领取最后动作，军阶与功绩不变。
5. 报告观察值与 Console 计数。
