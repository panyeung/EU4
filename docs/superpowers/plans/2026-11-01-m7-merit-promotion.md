# M7 实施计划：功名晋升
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


设计：`docs/superpowers/specs/2026-11-01-m7-merit-promotion-design.md`
分支：`feature/m5-save-slot`（用户指定沿用同一分支与工作树）

## 任务

1. **Domain（纯 C#）**
   - `PromotionRules`：四军阶阈值表（足轻0/番头5/组头10/武士头20）、`RankFor`、`ChineseName`、`MinimumMerit`、`TryNextRank`。
   - `GameSession.Rank`/`RankName` 派生属性；存档格式不变。
   - 测试：`PromotionRulesTests`（边界 0/4/5/9/10/19/20/30、负数、名称、NextRank 链）。
2. **Presentation**
   - `RankPanel`：`Bind` + 幂等 `Refresh` + Update 轮询；空会话保护；文本含下一阶或（最高）。
   - `DemoSummaryPanel`：完成文案加入军阶名。
   - 测试：`RankPanelTests`；扩展 `DemoSummaryPanelTests` 断言军阶文案。
3. **Editor**
   - `AddM7RankPanel` 事务式更新器 + 菜单 + `RunBatchIntegration` + 故障注入缝；`M7RankText` 位于 map、TownMenuRoot 之前、(-450,100) 240×40；重叠检查覆盖 TitleText/LocationMarkerText/M5 控件/Confirm/Cancel/Feedback。
   - `FoundationSceneValidator`：FOUND RankPanel + 绑定 + `AddM7RankPanel.Validate`。
   - 测试：`AddM7RankPanelTests`。
4. **Unity 验证**
   - 用户 Editor 若仍打开本工作树：先请用户关闭 → batchmode 编译 + EditMode 全量 + `RunBatchIntegration` + Validator 报告 → 记录证据 → 提交并推送（AGENTS：checkpoint 后立即推送）。
5. **文档**
   - `M7_DEMO.md`（范围+证据+验收路径）；`PROJECT_STATE.md`、`PHASE1_ROADMAP.md`（新建 Phase 2 段落）、`ARCHITECTURE.md`、`DECISIONS.md`（军阶为游戏化简化设定的决定）。
6. **QA 与收尾**
   - 同任务复审（kanban 工具不可用时由 lead 记录静态复审结论）。
   - 用户 Play Mode 接受后：最终 checkpoint → 合并 main → tag `demo-M7` → 推送 GitHub。

## 约束提醒

- 单写者；事务式场景更新器；保留 .meta/GUID；不装包；不宣称未执行的验证。
