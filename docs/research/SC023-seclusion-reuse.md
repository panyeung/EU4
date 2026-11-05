# SC-023 隐居与后见人复用调研

**调研日期：** 2026-11-05  
**范围：** 最多三个 GitHub 候选；只读调研，没有克隆、下载、安装或复制代码。

## 项目内现状

- `SuccessionService.PerformHandoff` 会把前任家主改为 `DirectKin`，并将其 `SuccessionPersonLife.Retired` 设为 `true`；稳定人物 ID 和成员记录继续保留。
- `SuccessionPersonLife` 已说明退休家主可继续为家族提供建议，但当前没有对应的可操作指导规则。
- SC-020 的年度培养计划已保存导师 ID；现有资格规则拒绝退休导师，存档校验也验证导师成员、生命状态与年龄。
- 退休影响的最窄纵向切片可复用成员生命周期、年度教育计划、现有成长结算、编年史和存档，不必新增关系或资源模型。

## 候选比较

| 仓库 | License | 兼容性与维护 | 集成成本与决定 |
|---|---|---|---|
| [lylelove/Dynasty](https://github.com/lylelove/Dynasty) | MIT | Python 3.8+ / PySide6；约 51 次提交、1 个 Star；不是 Unity/C# 项目 | 领域机制有少量参考价值；移植需重写，**Build**，只参考设计概念。 |
| [CtxPilot/Late-Eastern-Han-Dynasty](https://github.com/CtxPilot/Late-Eastern-Han-Dynasty) | MIT | TypeScript 网页项目；约 245 次提交、23 个 Star；使用 npm/pnpm workspace，不含 Unity 实现 | 有官员/顾问角色概念，但不覆盖退休家主与继承人教育；改造成本高，**不复用**。 |

未发现适合直接接入的 Unity 6 方案。Unity 序列化和项目现有存档采用纯 C# DTO/校验，因此新规则应沿用当前持久化边界：[Unity Script Serialization](https://docs.unity3d.com/Manual/script-serialization.html)。

## 决定

**Build：** 在现有 Unity/C# 领域内扩展退休前任家主的年度导师资格，不安装包、不引入外部代码或资产。当前家主交接后，前任家主仍为退休的直系亲属；可在 SC-023 中指导新家主的子女，并通过已有年度培养结果和编年史体现影响。SC-020 原有固定剧本行为保持不变。
