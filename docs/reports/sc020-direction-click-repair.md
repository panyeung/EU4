# SC-020 方向按钮点击无响应修复报告

日期：2026-11-04  
执行者：OpenCode2（唯一写入者；未操作 Unity GUI / Computer Use / 截图；未提交）  
范围：仅 C# 运行时修复与 EditMode 回归测试；未修改场景、预制体、`ProjectSettings`、`Library`、`Temp`、`.meta`/GUID。

## 1. 现象

FoundationScene 中 SC-020 示范能正确进入 1560-01-01、青叶家名册与初始属性正确；但点击可见的方向按钮（包括“学问”）后，选中方向与预览仍停在“自由成长”。打开/关闭按钮正常响应。

## 2. 根因（已在场景 YAML 中确认）

`CharacterEducationPanel.BindReferences` 无条件调用了 `Refresh()`。生成器 `AddSC020CharacterEducation` 在编辑模式下构建/修复场景时会调用 `BindReferences`，于是 `Refresh()` 在编辑模式里实例化了方向按钮克隆并保存进场景。

`FoundationScene.unity` 中 `SC020DirectionContainer`（fileID `1972456745`）下确有 6 个已序列化的克隆：

- `SC020Direction_Ninjutsu`
- `SC020Direction_MartialArts`
- `SC020Direction_Scholarship`
- `SC020Direction_Management`
- `SC020Direction_Social`
- `SC020Direction_FreeGrowth`

每个克隆的 `Button` 均为 `m_Interactable: 0`，且 `m_OnClick.m_PersistentCalls.m_Calls: []`。运行时 `_directions` 列表初始为空，`ClearButtons()` 无法追踪这些已保存的对象；重建时又生成 6 个“活”按钮。结果：

1. 前 6 个网格位置被无监听器、不可交互的死按钮占据；
2. 这些死按钮的 `Image.raycastTarget` 仍为 1，会吞掉指针点击；
3. 活按钮被排到后续网格行，因此用户点击可见的“学问”等按钮没有任何效果；
4. 打开/关闭是场景中的静态按钮，由 `Subscribe()` 绑定，所以仍正常。

## 3. 修复

生产代码仅改 `Assets/Scripts/Presentation/CharacterEducationPanel.cs`：

1. `BindReferences` 与 `OnEnable` 仅在 `UnityEngine.Application.isPlaying` 时调用 `Refresh()`，避免编辑模式下把临时按钮克隆序列化进场景。
2. `ClearButtons()` 在重建前额外清理方向/导师容器中名称匹配 `SC020Direction_` / `SC020Mentor_` 且不是模板的遗留子物体。这样无需修改场景即可在运行时自愈已存在的死按钮；在 EditMode 预览场景中同样生效。
3. 未改动 SC-020 规则、预览公式、计划提交或存档逻辑。

## 4. 回归测试（`Assets/Tests/EditMode/CharacterEducationPanelTests.cs`）

1. `Panel_UpdateKeepsContainersFreeOfSerializedRuntimeClones`：生成后方向/导师容器只序列化一个未激活模板，证明编辑模式不再烘焙按钮克隆。
2. `Panel_ClickingDirectionButtonUpdatesSelectionPreviewAndMentorChoices`：走真实 `Button.onClick` 路径点击“学问”，断言选中方向变为 `Scholarship`、标签出现 `▶`、导师列表出现 2 人且默认导师为宗一郎、预览 `20 → 27`；再点击第 2 位导师，断言预览 `20 → 29` 与 `共 +9`，确认按钮可交互。
3. `Panel_OpenRemovesSerializedDeadDirectionClones`：向容器注入 6 个不可交互的死克隆后 `Open()`，断言死克隆被清理、仅保留 6 个活按钮加模板，且活按钮均 `interactable`、`targetGraphic.raycastTarget` 为真。

## 5. 静态证据

Host 侧 Roslyn 类型检查（Unity 6000.6.6f1 随附 `csc.dll`，不是 Unity Editor 编译）：

- `SengokuChronicle.Presentation`：exit 0，0 diagnostics
- `SengokuChronicle.Editor`：exit 0，0 diagnostics
- `SengokuChronicle.EditModeTests`（全量现有 + 新增测试）：exit 0，0 diagnostics

## 6. 未验证 / 遗留

- **未运行** Unity Editor 编译、EditMode Test Runner、Console 计数、Play Mode、用户验收；这些必须由协调方复跑。
- `FoundationScene.unity` 文件内仍保留上述 6 个历史死克隆；运行时首次 `Refresh()`（场景加载的 `OnEnable` 或打开面板）会将其停用并销毁，因此本次不需要、也没有修改场景。若后续希望场景文件本身干净，可在场景干净且非 Play Mode 时运行 SC-020 生成器的修复路径，但这属于场景编辑，未在本次执行。
- 未提交、未合并、未打 tag。
