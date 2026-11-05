# SC-021 家族研究与技艺传承复用调研

日期：2026-11-04  
范围：只读 GitHub 复用评估；未下载、安装或复制任何外部代码。

## 项目需求

GDD v4.0 将「家族知识」定义为已记录、研究并保存的技艺；「人物掌握」是某个人实际会用的技能，两者不应混同。家族知识应可经卷轴、师徒或道场延续，而新家主不能仅因继承而自动掌握全部技能。项目现有事件内容使用 JSON，规则与状态放在纯 C# Domain，存档与月结已支持产业和教育事务。

## 候选方案

| 项目 | 许可证与兼容性 | 与本项目的匹配度 | 维护和依赖观察 | 决定 |
|---|---|---|---|---|
| [Unity Modular Skill / Tech Tree System](https://github.com/ahmedAwedni/Unity-Modular-Skill-Tree-System) | README 声明 MIT、Unity 6+；以 ScriptableObject 表示技能节点，支持先决条件、UI 与 JSON 存档。 | 有用的先决条件图思路；其核心是 Singleton、角色技能点和购买后即时解锁，不能表达“家族记录知识”和“成员个人掌握”的区别，也未体现本项目的事务月结/继承契约。 | GitHub 页面显示 5 次提交、0 stars、0 forks；未能从页面确认最近提交日期。README 没有列第三方依赖；安全审计未做。 | **不直接复用**；可借鉴先决条件与状态分离的概念。 |
| [SkillTree-Graph](https://github.com/kanbarudesu/SkillTree-Graph) | README 声明 MIT、Unity 6.0+；MIT 许可文本在仓库根目录 `LICENSE`。 | 提供编辑器节点图、JSON 保存/加载、导出 ScriptableObject 和运行时样例。适合作者制作一般技能树，但初期家族知识无需编辑器图工具，且仍需自行实现纯 C# 规则、存档和代际传承语义。 | GitHub 页面显示 18 次提交、1 star、0 forks；未能从页面确认最近提交日期。README 未给出依赖清单；安全审计未做。 | **不安装**；若未来有大量研究节点和内容作者，再评估其编辑器体验。 |

## 推荐

SC-021 第一片采用**定制、纯 C#、确定性的家族研究模型**，复用项目现有 JSON 内容、存档、`TryAdvanceToNextMonth` 事务月结、产业任命、编年史与 uGUI 模式。研究完成只新增家族层面的已记录知识；人物获得实际掌握应由后续训练/师徒切片单独处理。初始片不安装第三方包、不复制以上仓库代码。

这减少了与现有 `GameSession`、JSON DTO、事务式回滚和稳定 ID 模式的集成摩擦，也避免把外部面向角色技能点/编辑器资产的架构直接带入纯 C# 领域层。

## 来源

- [Unity Modular Skill / Tech Tree System README](https://github.com/ahmedAwedni/Unity-Modular-Skill-Tree-System)
- [Unity Modular Skill / Tech Tree System LICENSE](https://github.com/ahmedAwedni/Unity-Modular-Skill-Tree-System/blob/main/LICENSE)
- [SkillTree-Graph README](https://github.com/kanbarudesu/SkillTree-Graph)
- [SkillTree-Graph LICENSE](https://github.com/kanbarudesu/SkillTree-Graph/blob/main/LICENSE)
- `GDD_v4.0.md` §4.4、§5.2；现有 `Assets/Scripts/Domain`、`Assets/Scripts/Persistence`、`Assets/Scripts/Application/GameSession.cs`。
