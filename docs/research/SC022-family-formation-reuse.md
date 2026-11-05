# SC-022 家族管理复用研究

**日期：** 2026-11-04  
**范围：** 家族成员/亲属关系命令与 Unity 管理面板；只读 GitHub 评估，最多三项。  
**执行说明：** Orca 的 `scout` 执行别名未配置，受监督 worker 启动返回 `agent_unconfigured`，且未生成可恢复的回执。本次由协调者按已批准计划完成同范围只读核查后交 OpenCode2；没有下载、安装、克隆或复制外部代码。  
**本地优先检查：** 项目已有 `KinshipGraph`（对称配偶、定向收养、追加式边）、`FamilyState`、`FamilyRelationshipValidator`、`FamilyTreeGraph`、`FamilyRosterPanel`、`FamilyResearchPanel`，以及 SC-021 的事务式场景生成器。这些类型可作为 SC-022 的领域、uGUI 和场景集成基础。

## 候选对比

| 仓库 | 许可与 Unity 兼容 | 维护与依赖 | 与 SC-022 的匹配度、工作量 | 决策 |
|---|---|---|---|---|
| [alelievr/NodeGraphProcessor](https://github.com/alelievr/NodeGraphProcessor) | MIT，可商用，分发时保留版权与许可声明。README 写明最低 Unity 2020.2；仓库有 2025-09-22 的 Unity 6 编译修复及版本更新。 | 497 次提交；UIElements/GraphView 的 Unity Editor 节点图框架。接入需带入 Editor 图编辑器代码与其项目/包配置；本次未做代码级依赖漏洞扫描，新增第三方包会扩大依赖面。 | 适合可视化编辑器图，不提供玩家运行时的婚姻/收养表单或亲属业务校验。适配需改写节点模型/编辑器工作流，估计 2–4 天且维护成本高于复用项目现有 uGUI。 | **Build**：不引入；沿用当前项目结构。 |
| [needle-mirror/com.unity.graphtoolkit](https://github.com/needle-mirror/com.unity.graphtoolkit) | 包元数据为 `0.4.0-exp.2`，要求 Unity `6000.2`，依赖 `com.unity.modules.uielements`。仓库有 `LICENSE.md`，但本次页面读取未能取得许可正文，商业许可未核实。 | 镜像页面显示 5 次提交、3 stars；README 明确称包仍为 experimental、未准备好生产使用。未做源码/依赖漏洞扫描。 | Unity Editor 图编辑/数据模型框架，没有玩家运行时家族管理面板；与 SC-022 当前 uGUI 纵向切片不匹配。适配与验证成本高且引入实验性依赖。 | **Build**：不引入；许可与生产成熟度不足以支持选择。 |
| [Botmasher/kinship-explorer-app](https://github.com/Botmasher/kinship-explorer-app) | README 声明应用 GPLv3；嵌入的 Unity build 为 LGPLv3；具体语言数据可能另有许可。商业集成会带来许可审查义务。 | 31 次提交，页面显示 0 stars；需要 Node/npm 或 Yarn 及 React 外壳。README 表示该展示项目不会长期活跃维护。未做源码/依赖漏洞扫描。 | 有 kinship 可视化概念，实际是 React 包装 Unity WebGL、展示全球亲属称谓的应用，不是 Unity 游戏内的家族成员管理组件；提取、移植并重建数据路径估计至少 1–2 周。 | **Build**：不复制或移植；许可与技术栈均不合适。 |

## 结论

没有合适的轻量外部库可直接复用。按已批准实现计划，以项目现有 `KinshipGraph`/`FamilyState` 处理领域状态，以现有 `FamilyResearchPanel` 和 `AddSC021FamilyResearch` 模式实现 uGUI 与 `FoundationScene` 集成。SC-022 无需新增 package；不改 `Packages/manifest.json`，也不需因本研究新增 `ASSETS.md` 条目。

## 来源

- NodeGraphProcessor [README / 兼容与活动页](https://github.com/alelievr/NodeGraphProcessor) 和 [MIT License](https://github.com/alelievr/NodeGraphProcessor/blob/master/LICENSE)；提交日期从 [commit history](https://github.com/alelievr/NodeGraphProcessor/commits/master/) 核对。
- Unity Graph Toolkit [README](https://github.com/needle-mirror/com.unity.graphtoolkit)、[package.json](https://github.com/needle-mirror/com.unity.graphtoolkit/blob/master/package.json)、[LICENSE.md](https://github.com/needle-mirror/com.unity.graphtoolkit/blob/master/LICENSE.md)。本次未能读取许可正文。
- Kinship Term Explorer [README](https://github.com/Botmasher/kinship-explorer-app) 和 [LICENSE](https://github.com/Botmasher/kinship-explorer-app/blob/master/LICENSE)。
