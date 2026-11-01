# Phase 2 Roadmap — Sengoku Chronicle

> 状态：Phase 1（M1–M6）已全部验收并打标签 `demo-M2`…`demo-M6`。本文件跟踪 Phase 2 里程碑。每个里程碑的实施仍须用户单独授权。设计以 `GDD_v0.2.md` 为权威；Phase 2 内容以长期愿景（§5）为来源，逐步立项。

## M7 — 功名晋升（Merit & Promotion）

**范围：** 纯 C# 军阶规则（足轻0/番头5/组头10/武士头20，游戏化简化设定，非史实官职）；地图常驻「功名」行（当前功绩/军阶/下一阶门槛）；M6 演示摘要行显示最终军阶。

**验收：** 新游戏显示足轻(0)；每笔任务奖励使军阶按阈值正确晋升；三项任务全结算后为武士头（最高）且摘要行同步显示；存档/继续不改变或伪造军阶；重复领取不提高功绩或军阶；端到端演示无阻断。

**依赖：** M1–M6。

## 后续候选（未排期）

- 第四任务与数据驱动任务定义（内容扩展的前置）。
- NPC 日课/自主行为（Phase 1 明确排除项，需谨慎设计规模）。
- 势力/城经营与战斗：远期，暂不立项。

## M7 已接受状态（2026-11-01）

M7 在 `feature/m5-save-slot` 实现（用户指定沿用同一分支与工作树），并已获得用户 Play Mode 接受（用户报告 `pass`）。新增 `PromotionRules`（Domain，纯静态）、`GameSession.Rank/RankName`（派生，不入存档）、`RankPanel`（Presentation，幂等刷新 + Update 轮询）、`DemoSummaryPanel` 文案升级与事务式更新器 `AddM7RankPanel`（`M7RankText` 位于地图左上安全区 (-450,100) 240×40）。Validator 已加入 M7 层级/绑定/分离检查。

验证证据（2026-11-01）：batchmode 编译无 `error CS`；EditMode 425/425 通过（集成前 22:19:48Z、集成后 22:36:35Z，0 failed / 0 skipped）；`RunBatchIntegration` 保存 `FoundationScene`（+96/−1，唯一删除行为 Unity 对 `M6TitleLabel` 惰性字段 `m_MaxSize` 的再归一化，`m_BestFit: 0`，非代码改动）；`Logs/SceneValidationReport.txt` 报告 M7 PASS 且 `RESULT: PASS; Errors: 0`。用户执行 Play Mode 验收并报告通过；具体军阶数值与 Console 计数未由代理独立记录，验收以用户报告为准。
