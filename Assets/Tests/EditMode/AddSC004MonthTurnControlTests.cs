using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// SC-004 month turn updater EditMode 测试：创建、幂等、回滚、部分状态拒绝、
    /// 几何校验与场景预检。使用 preview scene，绝不保存或触碰 FoundationScene。
    /// </summary>
    public sealed class AddSC004MonthTurnControlTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _canvas;
        private Transform _panel;
        private GameSession _session;
        private WorldClockPanel _clockPanel;
        private WorldMapController _mapController;
        private Button _dayButton;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            _canvas = canvas.transform;

            GameObject panelGo = Create("WorldClockPanel", _canvas, typeof(WorldClockPanel));
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 40f);
            panelRect.sizeDelta = new Vector2(560f, 200f);
            _panel = panelGo.transform;
            _clockPanel = panelGo.GetComponent<WorldClockPanel>();

            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitNewGame();

            _mapController = Create("M2WorldMapRoot", _canvas, typeof(WorldMapController))
                .GetComponent<WorldMapController>();

            // 复刻 FoundationScene 的 M1 时钟面板控件与绑定。
            GameObject dayGo = Create("AdvanceButton", _panel, typeof(Image), typeof(Button));
            RectTransform dayRect = dayGo.GetComponent<RectTransform>();
            dayRect.anchorMin = dayRect.anchorMax = new Vector2(0.5f, 0f);
            dayRect.pivot = new Vector2(0.5f, 0f);
            dayRect.anchoredPosition = new Vector2(0f, -8f);
            dayRect.sizeDelta = new Vector2(300f, 80f);
            _dayButton = dayGo.GetComponent<Button>();
            _dayButton.targetGraphic = dayGo.GetComponent<Image>();
            CreateButtonLabel(dayGo);

            GameObject dateGo = Create("DateText", _panel, typeof(Text));
            RectTransform dateRect = dateGo.GetComponent<RectTransform>();
            dateRect.anchorMin = dateRect.anchorMax = new Vector2(0.5f, 1f);
            dateRect.pivot = new Vector2(0.5f, 1f);
            dateRect.anchoredPosition = Vector2.zero;
            dateRect.sizeDelta = new Vector2(400f, 60f);
            Text dateText = dateGo.GetComponent<Text>();
            dateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _clockPanel.SetDependencies(_session, _dayButton, dateText);
            _mapController.BindReferences(
                _session, _clockPanel,
                null, null, dateText, null, null, null,
                null, null, null, null, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        [Test]
        public void Updater_AddsAndBindsMonthTurnControl()
        {
            int count = ObjectCount();

            Assert.That(AddSC004MonthTurnControl.Update(_scene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count + 3), "固定根 + 按钮 + 标签恰好三个对象");

            Transform root = Find(AddSC004MonthTurnControl.PanelName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_panel), "生成 UI 必须挂在唯一固定根下，且根位于时钟面板之下");

            Transform button = Find(AddSC004MonthTurnControl.ButtonName);
            Assert.That(button, Is.Not.Null);
            Assert.That(button.parent, Is.EqualTo(root));
            Button monthButton = button.GetComponent<Button>();
            Assert.That(monthButton, Is.Not.Null);
            Assert.That(monthButton.targetGraphic, Is.Not.Null);

            Text label = button.GetComponentInChildren<Text>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo(AddSC004MonthTurnControl.ButtonLabelText), "按钮文本必须为「结束月份」");

            Assert.That(GetReference(_clockPanel, "endMonthButton"), Is.EqualTo(monthButton));
            Assert.That(GetReference(_clockPanel, "worldMapController"), Is.EqualTo(_mapController));
            Assert.That(GetReference(_clockPanel, "advanceButton"), Is.EqualTo(_dayButton),
                "原「推进一天」绑定必须保持不变");

            Assert.That(AddSC004MonthTurnControl.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "测试 seam 绝不保存传入场景");
        }

        [Test]
        public void Updater_IsIdempotentWhenAlreadyValid()
        {
            Assert.That(AddSC004MonthTurnControl.Update(_scene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success));

            int builtCount = ObjectCount();
            Assert.That(AddSC004MonthTurnControl.Update(_scene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount), "重复执行不得新增对象");
        }

        [Test]
        public void Updater_RollsBackWhenValidationFails()
        {
            int count = ObjectCount();

            // 注入事务内故障：月末绑定被抹掉，Validate 必然失败。
            AddSC004MonthTurnControl.UpdateResult result = AddSC004MonthTurnControl.Update(_scene, _ =>
                SetReference(_clockPanel, "endMonthButton", null));

            Assert.That(result, Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.ValidationFailed));
            Assert.That(Find(AddSC004MonthTurnControl.PanelName), Is.Null, "回滚后固定根不得残留");
            Assert.That(Find(AddSC004MonthTurnControl.ButtonName), Is.Null, "回滚后按钮不得残留");
            Assert.That(GetReference(_clockPanel, "endMonthButton"), Is.Null,
                "Undo 必须恢复面板绑定到事务前状态");
            Assert.That(GetReference(_clockPanel, "worldMapController"), Is.Null);
            Assert.That(ObjectCount(), Is.EqualTo(count), "回滚后对象数必须复原");
            Assert.That(AddSC004MonthTurnControl.Validate(_scene, out _), Is.False);
            Assert.That(_scene.path, Is.Empty);
        }

        [Test]
        public void Updater_RefusesPartialExistingControl()
        {
            // 外部手工放置的同名按钮（无根、无绑定）必须被拒绝，而不是被收编。
            Create(AddSC004MonthTurnControl.ButtonName, _panel, typeof(Image), typeof(Button));

            Assert.That(AddSC004MonthTurnControl.Update(_scene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.ExistingPartialControl));
            Assert.That(Find(AddSC004MonthTurnControl.PanelName), Is.Null, "拒绝路径不得创建任何固定根");
        }

        [Test]
        public void Updater_RefusesPreboundMonthButton()
        {
            SetReference(_clockPanel, "endMonthButton", _dayButton);

            Assert.That(AddSC004MonthTurnControl.Update(_scene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.ExistingPartialControl));
        }

        [Test]
        public void Validator_RejectsMonthButtonOverlappingDayButton()
        {
            Assert.That(AddSC004MonthTurnControl.Update(_scene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success));

            Transform button = Find(AddSC004MonthTurnControl.ButtonName);
            // 面板本地 y：日按钮 -108..-28，正常月份按钮 -22..34。
            // 下移到 -42..14：仍在面板内，只触发“与日按钮重叠”这一条校验。
            button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -14f);

            Assert.That(AddSC004MonthTurnControl.Validate(_scene, out _), Is.False,
                "仍在面板范围内但与日按钮重叠的布局必须校验失败");
        }

        [Test]
        public void Validator_RejectsWrongLabelAndMissingBinding()
        {
            Assert.That(AddSC004MonthTurnControl.Update(_scene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success));

            Transform button = Find(AddSC004MonthTurnControl.ButtonName);
            Text label = button.GetComponentInChildren<Text>();
            label.text = "推进一天";
            Assert.That(AddSC004MonthTurnControl.Validate(_scene, out _), Is.False, "错误标签必须失败");

            label.text = AddSC004MonthTurnControl.ButtonLabelText;
            SetReference(_clockPanel, "endMonthButton", null);
            Assert.That(AddSC004MonthTurnControl.Validate(_scene, out _), Is.False, "丢失绑定必须失败");
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            Assert.That(AddSC004MonthTurnControl.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.SceneMismatch));
            Assert.That(AddSC004MonthTurnControl.CheckScenePreflight(AddSC004MonthTurnControl.ScenePath, true),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.SceneDirty));
            Assert.That(AddSC004MonthTurnControl.CheckScenePreflight(AddSC004MonthTurnControl.ScenePath, false),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success));
        }

        private void CreateButtonLabel(GameObject buttonGo)
        {
            GameObject labelGo = Create("Label", buttonGo.transform, typeof(Text));
            RectTransform rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = labelGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "推进一天";
        }

        private Transform Find(string name) => _scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name)
            .SingleOrDefault();

        private int ObjectCount() =>
            _scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

        private GameObject Create(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (System.Type component in components)
                go.AddComponent(component);
            return go;
        }

        private static Object GetReference(Object target, string field)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null ? property.objectReferenceValue : null;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>
    /// SC-004 复现覆盖：真实 FoundationScene 的时钟面板 YAML 早于 endMonthButton/worldMapController
    /// 字段；对它执行更新器是 Lead 报告“保存前 endMonthButton 未绑定”回滚的同一条件。预览场景夹具
    /// 里的面板是当场 AddComponent 的新对象，覆盖不到“从磁盘序列化状态加载的面板”。
    /// 本夹具把 FoundationScene 复制为临时副本并加法打开，在完全相同的磁盘布局上运行更新器；
    /// 绝不保存副本，也绝不触碰 Assets/Scenes/FoundationScene.unity。
    /// </summary>
    public sealed class AddSC004MonthTurnControlStaleSceneTests
    {
        private const string TempScenePath = "Assets/Scenes/AddSC004StaleLayoutCopy.unity";
        private const string DayButtonName = "AdvanceButton";

        private Scene _tempScene;
        private Scene _previousActiveScene;
        private bool _previousActiveSceneDirty;
        private string _tempSceneBytesBefore;
        private string _foundationSceneBytesBefore;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TempScenePath); // 清掉上次崩溃可能残留的副本
            Assert.That(File.Exists(TempScenePath), Is.False, "临时副本必须在干净的基线上创建");
            Assert.That(AssetDatabase.CopyAsset(AddSC004MonthTurnControl.ScenePath, TempScenePath), Is.True,
                "必须能复制 FoundationScene 为临时副本");
            _tempSceneBytesBefore = File.ReadAllText(TempScenePath);
            _foundationSceneBytesBefore = File.ReadAllText(AddSC004MonthTurnControl.ScenePath);

            _previousActiveScene = SceneManager.GetActiveScene();
            _previousActiveSceneDirty = _previousActiveScene.isDirty;

            // 加法加载：编辑器活动场景与 FoundationScene 状态完全不受影响。
            _tempScene = EditorSceneManager.OpenScene(TempScenePath, OpenSceneMode.Additive);
            Assert.That(_tempScene.IsValid() && _tempScene.isLoaded);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousActiveScene));
        }

        [TearDown]
        public void TearDown()
        {
            if (_tempScene.IsValid())
                EditorSceneManager.CloseScene(_tempScene, true);
            if (File.Exists(TempScenePath))
                Assert.That(AssetDatabase.DeleteAsset(TempScenePath), Is.True, "测试必须删除临时场景副本");

            Scene active = SceneManager.GetActiveScene();
            Assert.That(active, Is.EqualTo(_previousActiveScene), "活动场景不得被改变");
            Assert.That(active.isDirty, Is.EqualTo(_previousActiveSceneDirty), "活动场景的脏状态不得被改变");
            Assert.That(File.ReadAllText(AddSC004MonthTurnControl.ScenePath), Is.EqualTo(_foundationSceneBytesBefore),
                "FoundationScene.unity 的字节必须原样保留");
        }

        [Test]
        public void Updater_BindsMonthTurnOnSceneLoadedClockPanel()
        {
            WorldClockPanel panel = ComponentIn<WorldClockPanel>(_tempScene);
            Transform dayButton = NamedIn(_tempScene, DayButtonName);

            Assert.That(AddSC004MonthTurnControl.Update(_tempScene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success),
                "更新器必须在“从磁盘序列化状态加载的时钟面板”上于同一事务内完成创建与绑定");

            Transform monthButton = NamedIn(_tempScene, AddSC004MonthTurnControl.ButtonName);
            Assert.That(GetReference(panel, "endMonthButton"), Is.EqualTo(monthButton.GetComponent<Button>()),
                "endMonthButton 必须能被验证路径经 SerializedObject 读到");
            Assert.That(GetReference(panel, "worldMapController"),
                Is.EqualTo(ComponentIn<WorldMapController>(_tempScene)));
            Assert.That(GetReference(panel, "advanceButton"), Is.EqualTo(dayButton.GetComponent<Button>()),
                "原「推进一天」绑定必须保持不变");

            Assert.That(AddSC004MonthTurnControl.Validate(_tempScene, out string error), Is.True, error);

            // 幂等：重复执行成功且不新增对象。
            int objectsAfterFirst = ObjectCountIn(_tempScene);
            Assert.That(AddSC004MonthTurnControl.Update(_tempScene),
                Is.EqualTo(AddSC004MonthTurnControl.UpdateResult.Success));
            Assert.That(ObjectCountIn(_tempScene), Is.EqualTo(objectsAfterFirst));

            // 测试 seam 绝不写盘：临时副本文件内容不变。
            Assert.That(File.ReadAllText(TempScenePath), Is.EqualTo(_tempSceneBytesBefore),
                "测试 seam 绝不保存传入场景");
        }

        private static T ComponentIn<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true))
            .Single();

        private static Transform NamedIn(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Single(t => t.name == name);

        private static int ObjectCountIn(Scene scene) => scene.GetRootGameObjects()
            .Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

        private static Object GetReference(Object target, string field)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null ? property.objectReferenceValue : null;
        }
    }
}
