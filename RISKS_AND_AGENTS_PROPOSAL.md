# 项目风险与 AGENTS.md 增补提案

> 对现有 AGENTS.md 仅作提案；没有修改原文件。待用户批准后再用最小 diff 增补。

## 风险与缓解

| 风险 | 影响 | 缓解建议 |
|---|---|---|
| 参考《太阁立志传5》被误解为复制其表达内容 | 著作权/商标风险 | 明确仅作体验参考；使用原创或获授权素材、文字、音乐和 UI；避免复制原作名称之外的独特表达及资产。 |
| 初学者范围膨胀 | 核心演示迟迟不可玩 | 以 M1–M5 验收为边界；其余愿景明确延期；先闭环一个任务再扩内容。 |
| 规则和 UI 混写导致难测 | 编译/回归风险、迭代缓慢 | 日期/任务/关系规则放普通 C#，用 EditMode 测试；uGUI 仅负责呈现与输入。 |
| 日期、行程规则不明确 | 结果不可预测，边界 bug | M0/M1 明确日期粒度、月份规则、起始日；M2 显式配置路线与耗时，并测试非法路线和跨月旅行。 |
| 重复领取奖励/任务状态丢失 | 进度或功绩错误 | 任务状态包含已领取标记；领取用例幂等；EditMode 与存档往返测试。 |
| 静态定义改 ID 使存档失效 | 存档加载失败或引用错位 | 稳定 ID；检查重复/缺失 ID；DTO 使用 ID 而非 Unity 对象引用；未知 ID 报错并保护原文件。 |
| Unity 场景/Prefab YAML 手工冲突 | GUID 破坏、资源损坏、并行覆盖 | 优先 Editor 自动化和 Unity API；保留 `.meta`；同一场景/Prefab 同时只允许一个任务编辑。 |
| 存档版本迁移与损坏恢复被延后 | M5 超出预期 | M5 初期先定版本策略和失败行为，最小 DTO 开始；自动化往返、未知版本与损坏数据测试。 |
| 缺少自动编译/Unity Editor 环境 | 无法证明实际可运行 | 由 UnityCoder/QATester 执行真实 Unity 编译/测试；报告明确区分无法运行、编译、EditMode、PlayMode 与人工检查。 |
| 当前工作区已有未跟踪文件 | 意外纳入提交或覆盖用户工作 | 本次已观察到 `Assets/Scripts.meta` 未跟踪；所有后续任务先看 git status，仅暂存任务相关文件。 |

## 建议加入现有 AGENTS.md 的最小增补文本（已应用）

> **状态（Status）**：**Applied** — 2026-10-20 实测写入 `AGENTS.md` 第 255 行，句意与本提案一致，无需再次修改。

现有 `AGENTS.md` 已有人工 Play Mode 验收要求。按用户澄清，增补句子已于 2026-10-20 实际写入 `AGENTS.md` 第 255 行：

> QA approval does not replace required manual Play Mode acceptance; do not mark that behavior accepted or create the final checkpoint commit until the user explicitly accepts it.

```markdown
QA approval does not replace required manual Play Mode acceptance; when manual acceptance is required, do not mark that behavior accepted or create the final checkpoint until the user explicitly confirms acceptance.
```

### 和现有规则的关系

现有 `AGENTS.md` 已规定 Unity 版本大类、团队职责、GitHub-first 研究、保留 `.meta`、场景/包安全、同卡 QATester 审查、最多十次 QA 审查及最终签核。增补句子已于 2026-10-20 写入 AGENTS.md 第 255 行，与提案意图一致，无需再次修改。
