using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;
using SengokuChronicle.Editor;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M2-02 Editor 生成器 EditMode 测试。
    /// 所有测试在独立 Preview Scene 中运行，决不修改活动场景或 FoundationScene.unity。
    /// </summary>
    public class CreateM2WorldMapGeneratorTests
    {
        private Scene _scene;
        private Scene _previousActiveScene;
        private bool _previousActiveSceneWasDirty;
        private GameObject _canvas;
        private GameObject _eventSystem;

        [SetUp]
        public void SetUp()
        {
            _previousActiveScene = SceneManager.GetActiveScene();
            _previousActiveSceneWasDirty = _previousActiveScene.IsValid() && _previousActiveScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();
            try
            {
                // 清空注入钩子
                CreateM2WorldMap.FailAfterCreate = null;
                CreateM2WorldMap.FailOnSave = null;

                Assert.That(_scene.IsValid(), "preview test scene must be valid");
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousActiveScene),
                    "creating a preview scene must not change the Editor's active scene");

                // M1 依赖：Canvas / EventSystem / GameSession / WorldClockPanel
                _canvas = CreateInPreviewScene("Canvas", typeof(RectTransform));
                var canvas = _canvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var canvasScaler = _canvas.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1280f, 720f);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
                _canvas.AddComponent<GraphicRaycaster>();
                var canvasRect = _canvas.GetComponent<RectTransform>();
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.zero;
                canvasRect.pivot = new Vector2(0.5f, 0.5f);
                canvasRect.anchoredPosition = Vector2.zero;
                canvasRect.sizeDelta = new Vector2(1280f, 720f);

                _eventSystem = CreateInPreviewScene("EventSystem");
                _eventSystem.AddComponent<EventSystem>();
                _eventSystem.AddComponent<StandaloneInputModule>();

                var sessionGo = CreateInPreviewScene("GameSession");
                var session = sessionGo.AddComponent<GameSession>();
                session.InitNewGame();

                var wcpGo = CreateInPreviewScene("WorldClockPanel", typeof(RectTransform));
                wcpGo.transform.SetParent(_canvas.transform, false);
                var wcp = wcpGo.AddComponent<WorldClockPanel>();
                var wcpRect = wcpGo.GetComponent<RectTransform>();
                wcpRect.anchorMin = new Vector2(0.5f, 0.5f);
                wcpRect.anchorMax = new Vector2(0.5f, 0.5f);
                wcpRect.pivot = new Vector2(0.5f, 0.5f);
                wcpRect.anchoredPosition = new Vector2(0f, 40f);
                wcpRect.sizeDelta = new Vector2(560f, 200f);

                var dateTextGo = CreateInPreviewScene("DateText", typeof(RectTransform), typeof(CanvasRenderer));
                dateTextGo.transform.SetParent(wcpGo.transform, false);
                var dateText = dateTextGo.AddComponent<Text>();
                var dateRect = dateTextGo.GetComponent<RectTransform>();
                dateRect.anchorMin = new Vector2(0.5f, 1f);
                dateRect.anchorMax = new Vector2(0.5f, 1f);
                dateRect.pivot = new Vector2(0.5f, 1f);
                dateRect.sizeDelta = new Vector2(400f, 60f);

                var advanceButtonGo = CreateInPreviewScene("AdvanceButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                advanceButtonGo.transform.SetParent(wcpGo.transform, false);
                var advanceRect = advanceButtonGo.GetComponent<RectTransform>();
                advanceRect.anchorMin = new Vector2(0.5f, 0f);
                advanceRect.anchorMax = new Vector2(0.5f, 0f);
                advanceRect.pivot = new Vector2(0.5f, 0f);
                advanceRect.anchoredPosition = new Vector2(0f, -8f);
                advanceRect.sizeDelta = new Vector2(300f, 80f);
                var advBtn = advanceButtonGo.GetComponent<Button>();
                wcp.SetDependencies(session, advBtn, dateText);

                // Freeze this synthetic preview fixture at reference resolution; the separate
                // FoundationScene geometry tests retain and inspect their serialized CanvasScaler.
                canvasScaler.enabled = false;
                PrepareCanvasGeometry(_scene, canvasRect, canvasScaler);
            }
            catch
            {
                try
                {
                    CleanupScene();
                }
                catch (Exception cleanupException)
                {
                    Debug.LogException(cleanupException);
                }
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            CleanupScene();
        }

        private void CleanupScene()
        {
            CreateM2WorldMap.FailAfterCreate = null;
            CreateM2WorldMap.FailOnSave = null;

            try
            {
                // 清理生成器创建的对象；关闭 preview scene 会一并销毁其余 fixture 对象。
                if (_scene.IsValid())
                {
                    GameObject root = FindInScene(_scene, RootName);
                    if (root != null)
                        Object.DestroyImmediate(root);
                }
            }
            finally
            {
                try
                {
                    if (_scene.IsValid() && !EditorSceneManager.ClosePreviewScene(_scene))
                        throw new InvalidOperationException("Could not close the generator test preview scene.");
                }
                finally
                {
                    _scene = default;
                    if (_previousActiveScene.IsValid())
                    {
                        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousActiveScene),
                            "generator tests must not change the Editor's active scene");
                        Assert.That(_previousActiveScene.isDirty, Is.EqualTo(_previousActiveSceneWasDirty),
                            "generator tests must preserve the active scene's pre-test dirty state");
                    }
                    _previousActiveScene = default;
                    _previousActiveSceneWasDirty = false;
                }
            }
        }

        private GameObject CreateInPreviewScene(string name, params Type[] components)
        {
            // HideAndDontSave prevents the transient object from being serialized into the
            // user's active scene before it is moved into the isolated preview scene.
            GameObject go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, components);
            try
            {
                if (go.scene != _scene)
                    SceneManager.MoveGameObjectToScene(go, _scene);
                go.hideFlags = HideFlags.None;
                return go;
            }
            catch
            {
                Object.DestroyImmediate(go);
                throw;
            }
        }

        private const string RootName = "M2WorldMapRoot";

        private static GameObject FindInScene(Scene scene, string name)
        {
            if (!scene.IsValid())
                return null;
            return scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == name)?.gameObject;
        }

        private sealed class FoundationScenePreview : IDisposable
        {
            private readonly Scene _previousActiveScene;
            private readonly bool _previousActiveSceneWasDirty;

            public Scene Scene { get; private set; }
            public RectTransform CanvasRect { get; private set; }
            public CanvasScaler Scaler { get; private set; }

            public FoundationScenePreview()
            {
                _previousActiveScene = SceneManager.GetActiveScene();
                _previousActiveSceneWasDirty = _previousActiveScene.IsValid() && _previousActiveScene.isDirty;
                Scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/FoundationScene.unity");
                try
                {
                    Assert.That(Scene.IsValid(), Is.True, "FoundationScene must open as an isolated preview scene");
                    Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousActiveScene),
                        "opening the geometry-test preview must not change the active scene");

                    GameObject canvasObject = FindInScene(Scene, "Canvas");
                    Assert.That(canvasObject, Is.Not.Null, "FoundationScene must contain its serialized Canvas");
                    CanvasRect = canvasObject.GetComponent<RectTransform>();
                    Scaler = canvasObject.GetComponent<CanvasScaler>();
                    Assert.That(CanvasRect, Is.Not.Null, "FoundationScene Canvas must have a RectTransform");
                    Assert.That(Scaler, Is.Not.Null, "FoundationScene Canvas must have its serialized CanvasScaler");

                    // The serialized root Canvas can have a zero driven localScale before a Canvas update.
                    // Normalize only this in-memory Preview Scene before measuring its actual M1 bounds.
                    PrepareCanvasGeometry(Scene, CanvasRect, Scaler);
                }
                catch
                {
                    if (Scene.IsValid())
                        EditorSceneManager.ClosePreviewScene(Scene);
                    Scene = default;
                    throw;
                }
            }

            public void Dispose()
            {
                if (Scene.IsValid() && !EditorSceneManager.ClosePreviewScene(Scene))
                    throw new InvalidOperationException("Could not close the FoundationScene geometry-test preview.");
                Scene = default;
                if (_previousActiveScene.IsValid())
                {
                    Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousActiveScene),
                        "geometry tests must preserve the Editor's active scene");
                    Assert.That(_previousActiveScene.isDirty, Is.EqualTo(_previousActiveSceneWasDirty),
                        "geometry tests must preserve the active scene's dirty state");
                }
            }
        }

        private static RectTransform FindRectInScene(Scene scene, string name)
        {
            GameObject gameObject = FindInScene(scene, name);
            Assert.That(gameObject, Is.Not.Null, $"Test scene must contain '{name}'");
            RectTransform rect = gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
            Assert.That(rect, Is.Not.Null, $"Test object '{name}' must have a RectTransform");
            return rect;
        }

        private static void PrepareCanvasGeometry(Scene scene, RectTransform canvasRect, CanvasScaler scaler)
        {
            Assert.That(canvasRect, Is.Not.Null);
            Assert.That(scaler, Is.Not.Null);
            Assert.That(canvasRect.gameObject.scene.handle, Is.EqualTo(scene.handle),
                "Canvas must belong to the target Preview Scene");

            // A serialized root Canvas may carry Unity-driven zero scale/size before its first layout pass.
            // Set the preview copy to reference-resolution geometry and refresh only its own layout tree.
            canvasRect.localScale = Vector3.one;
            canvasRect.sizeDelta = scaler.referenceResolution;
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);

            RectTransform panel = FindRectInScene(scene, "WorldClockPanel");
            RectTransform advance = FindRectInScene(scene, "AdvanceButton");
            Assert.That(panel.parent, Is.EqualTo(canvasRect),
                $"M1 panel parent/scene mismatch: {DescribeRectTransform(canvasRect, panel)}");
            Assert.That(advance.parent, Is.EqualTo(panel),
                $"M1 button parent/scene mismatch: {DescribeRectTransform(canvasRect, advance)}");
            Assert.That(panel.gameObject.scene.handle, Is.EqualTo(scene.handle));
            Assert.That(advance.gameObject.scene.handle, Is.EqualTo(scene.handle));

            Rect panelBounds = GetRectInCanvas(canvasRect, panel);
            Rect advanceBounds = GetRectInCanvas(canvasRect, advance);
            Assert.That(panelBounds.width, Is.GreaterThan(0f),
                $"M1 panel has zero Canvas-local width: {DescribeRectTransform(canvasRect, panel)}");
            Assert.That(panelBounds.height, Is.GreaterThan(0f),
                $"M1 panel has zero Canvas-local height: {DescribeRectTransform(canvasRect, panel)}");
            Assert.That(advanceBounds.width, Is.GreaterThan(0f),
                $"M1 button has zero Canvas-local width: {DescribeRectTransform(canvasRect, advance)}");
            Assert.That(advanceBounds.height, Is.GreaterThan(0f),
                $"M1 button has zero Canvas-local height: {DescribeRectTransform(canvasRect, advance)}");
        }

        private static Rect GetRectInCanvas(RectTransform canvas, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;
            foreach (Vector3 worldCorner in corners)
            {
                Vector3 localCorner = canvas.InverseTransformPoint(worldCorner);
                xMin = Mathf.Min(xMin, localCorner.x);
                xMax = Mathf.Max(xMax, localCorner.x);
                yMin = Mathf.Min(yMin, localCorner.y);
                yMax = Mathf.Max(yMax, localCorner.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private string DescribeGeneratorFixture()
        {
            RectTransform canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
            CanvasScaler scaler = _canvas != null ? _canvas.GetComponent<CanvasScaler>() : null;
            Canvas canvas = _canvas != null ? _canvas.GetComponent<Canvas>() : null;
            GameObject panelObject = FindInScene(_scene, "WorldClockPanel");
            RectTransform panel = panelObject != null ? panelObject.GetComponent<RectTransform>() : null;
            GameObject advanceObject = FindInScene(_scene, "AdvanceButton");
            RectTransform advance = advanceObject != null ? advanceObject.GetComponent<RectTransform>() : null;
            GameObject session = FindInScene(_scene, "GameSession");
            string scalerDescription = scaler == null
                ? "<missing>"
                : $"mode={scaler.uiScaleMode}, reference={scaler.referenceResolution}, match={scaler.matchWidthOrHeight}, " +
                  $"screenMatch={scaler.screenMatchMode}";
            return $"CanvasRect={(canvasRect != null ? canvasRect.rect.ToString() : "<missing>")}; " +
                   $"CanvasScaler={scalerDescription}, scaleFactor={(canvas != null ? canvas.scaleFactor.ToString() : "<missing>")}; " +
                   $"M1Panel={DescribeRectTransform(canvasRect, panel)}; " +
                   $"M1AdvanceButton={DescribeRectTransform(canvasRect, advance)}; " +
                   $"targetScene={DescribeScene(_scene)}; canvasScene={DescribeScene(_canvas != null ? _canvas.scene : default)}; " +
                   $"sessionScene={DescribeScene(session != null ? session.scene : default)}; " +
                   $"eventSystemScene={DescribeScene(_eventSystem != null ? _eventSystem.scene : default)}";
        }

        private static string DescribeRectTransform(RectTransform canvas, RectTransform target)
        {
            if (canvas == null || target == null)
                return "<missing>";
            return $"bounds={GetRectInCanvas(canvas, target)}, anchors={target.anchorMin}..{target.anchorMax}, " +
                   $"pivot={target.pivot}, position={target.anchoredPosition}, size={target.sizeDelta}, " +
                   $"scale={target.localScale}, parent={(target.parent != null ? target.parent.name : "<root>")}, " +
                   $"scene={DescribeScene(target.gameObject.scene)}";
        }

        private static string DescribeScene(Scene scene)
        {
            return scene.IsValid() ? $"{scene.name}({scene.handle})" : "<invalid>";
        }

        private static Rect ScaleRect(Rect rect, float scale)
        {
            return Rect.MinMaxRect(rect.xMin * scale, rect.yMin * scale, rect.xMax * scale, rect.yMax * scale);
        }

        private static float CanvasScaleFor(CanvasScaler scaler, float screenWidth, float screenHeight)
        {
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            float widthLog = Mathf.Log(screenWidth / scaler.referenceResolution.x, 2f);
            float heightLog = Mathf.Log(screenHeight / scaler.referenceResolution.y, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(widthLog, heightLog, scaler.matchWidthOrHeight));
        }

        [Test]
        public void Build_DuplicateRoot_ReturnsAlreadyExists()
        {
            // 预放一个 M2WorldMapRoot 对象
            var existing = CreateInPreviewScene(RootName);

            var r = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.AlreadyExists));
            Object.DestroyImmediate(existing);
        }

        [Test]
        public void Build_MissingCanvas_ReturnsMissingCanvas()
        {
            Object.DestroyImmediate(_canvas);
            _canvas = null;

            var r = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.MissingCanvas));
        }

        [Test]
        public void Build_MissingEventSystem_ReturnsMissingEventSystem()
        {
            Object.DestroyImmediate(_eventSystem);
            _eventSystem = null;

            var r = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.MissingEventSystem));
        }

        [Test]
        public void Build_MissingWorldClockPanel_ReturnsMissingWorldClockPanel()
        {
            GameObject worldClockPanel = FindInScene(_scene, "WorldClockPanel");
            Assert.That(worldClockPanel, Is.Not.Null);
            Object.DestroyImmediate(worldClockPanel);

            var r = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.MissingWorldClockPanel));
        }

        [Test]
        public void Build_Success_CreatesValidRootUnderCanvas()
        {
            var r = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.Success),
                $"expected clean build without save in isolated scene; result={r}; " +
                $"diagnostics={CreateM2WorldMap.LastBuildDiagnostics ?? "<none>"}; " +
                $"fixture={DescribeGeneratorFixture()}");

            GameObject root = FindInScene(_scene, RootName);
            Assert.That(root, Is.Not.Null, "M2WorldMapRoot must exist in scene after success");
            Assert.That(root.transform.parent, Is.EqualTo(_canvas.transform),
                "root must be parented under Canvas");

            var controller = root.GetComponent<WorldMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.GetTownMenuRoot(), Is.Not.Null, "town menu root must be bound");
            Assert.That(controller.GetFacilityTexts().Length, Is.EqualTo(3));
            Assert.That(controller.GetFacilityTexts().All(t => t != null), Is.True);

            // 城镉菜单初始应隐藏
            Assert.That(controller.GetTownMenuRoot().activeSelf, Is.False);

            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            RectTransform rootRect = root.GetComponent<RectTransform>();
            RectTransform panelRect = FindRectInScene(_scene, "WorldClockPanel");
            RectTransform advanceRect = FindRectInScene(_scene, "AdvanceButton");
            Assert.That(canvasRect.rect.xMin, Is.LessThan(0f), "fixture must exercise a non-zero Canvas-local origin");
            Assert.That(canvasRect.rect.yMin, Is.LessThan(0f), "fixture must exercise a non-zero Canvas-local origin");
            Assert.That(CreateM2WorldMap.LastBuildDiagnostics, Is.Null,
                "a successful build must not retain failure diagnostics");

            Rect expectedSafeArea;
            Assert.That(WorldMapLayout.TryCalculateSafeArea(canvasRect,
                new[] { panelRect, advanceRect }, out expectedSafeArea, out string safeAreaError),
                Is.True, safeAreaError);
            Rect rootBounds = GetRectInCanvas(canvasRect, rootRect);
            Rect panelBounds = GetRectInCanvas(canvasRect, panelRect);
            Rect advanceBounds = GetRectInCanvas(canvasRect, advanceRect);
            Assert.That(panelBounds.width, Is.GreaterThan(0f), "M1 panel bounds must be initialized, not a zero rect");
            Assert.That(panelBounds.height, Is.GreaterThan(0f), "M1 panel bounds must be initialized, not a zero rect");
            Assert.That(advanceBounds.width, Is.GreaterThan(0f), "M1 button bounds must be initialized, not a zero rect");
            Assert.That(advanceBounds.height, Is.GreaterThan(0f), "M1 button bounds must be initialized, not a zero rect");
            Assert.That(rootBounds.xMin, Is.EqualTo(expectedSafeArea.xMin).Within(0.01f));
            Assert.That(rootBounds.yMin, Is.EqualTo(expectedSafeArea.yMin).Within(0.01f));
            Assert.That(rootBounds.xMax, Is.EqualTo(expectedSafeArea.xMax).Within(0.01f));
            Assert.That(rootBounds.yMax, Is.EqualTo(expectedSafeArea.yMax).Within(0.01f));
            Assert.That(rootBounds.Overlaps(panelBounds), Is.False, "M2 must not overlap the actual M1 date panel");
            Assert.That(rootBounds.Overlaps(advanceBounds), Is.False, "M2 must not overlap the actual M1 AdvanceButton");
            Assert.That(advanceBounds.yMin - rootBounds.yMax,
                Is.GreaterThanOrEqualTo(WorldMapLayout.M1Clearance - 0.01f));
        }

        [Test]
        public void Build_ZeroM1Bounds_RefusesBeforeCreatingRoot()
        {
            RectTransform advanceRect = FindRectInScene(_scene, "AdvanceButton");
            advanceRect.sizeDelta = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_canvas.GetComponent<RectTransform>());

            CreateM2WorldMap.BuildResult result = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(result, Is.EqualTo(CreateM2WorldMap.BuildResult.MissingM1Layout));
            Assert.That(CreateM2WorldMap.LastBuildDiagnostics, Does.Contain("bounds 无效"));
            Assert.That(FindInScene(_scene, RootName), Is.Null,
                "a zero-size M1 rect must not be treated as a valid reserved area or leave a partial root");
        }

        [Test]
        public void Build_WithInjectedFailure_BeforeSave_RollsBackNoRoot()
        {
            CreateM2WorldMap.FailAfterCreate = () => true;

            var r = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.BuildFailed));
            GameObject root = FindInScene(_scene, RootName);
            Assert.That(root, Is.Null,
                "rollback must remove M2WorldMapRoot created in this build");
        }

        [Test]
        public void Build_WithInjectedSaveFailure_RollsBackAndDoesNotWriteFile()
        {
            // 使用不覆盖现有资产的唯一路径；故障钩子应在 SaveScene 之前回滚。
            string savePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/EditMode/_GenTmpScene.unity");
            Assert.That(File.Exists(savePath), Is.False,
                "unique test save path must not already exist");

            CreateM2WorldMap.FailOnSave = () => true;
            var r = CreateM2WorldMap.Build(_scene, saveScene: true, savePath: savePath);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.SaveFailed));
            Assert.That(File.Exists(savePath), Is.False,
                "must not write scene file on injected save failure");

            // 回滚后场景中不应残留 M2WorldMapRoot
            GameObject root = FindInScene(_scene, RootName);
            Assert.That(root, Is.Null, "rollback must remove M2WorldMapRoot before save");
        }

        [Test]
        public void Build_AlreadyExists_DoesNotMutateExistingRoot()
        {
            var existing = CreateInPreviewScene(RootName);

            var r = CreateM2WorldMap.Build(_scene, saveScene: false);

            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.AlreadyExists));
            // 既有对象未被删除/替换
            Assert.That(FindInScene(_scene, RootName), Is.Not.Null);
            Object.DestroyImmediate(existing);
        }

        [Test]
        public void Update_CleanRoot_SucceedsAndAppliesLayout()
        {
            var r = CreateM2WorldMap.Build(_scene, saveScene: false);
            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.Success));

            GameObject rootBeforeUpdate = FindInScene(_scene, RootName);
            RectTransform oldRootRt = rootBeforeUpdate.GetComponent<RectTransform>();
            oldRootRt.anchorMin = new Vector2(0.5f, 0.5f);
            oldRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            oldRootRt.anchoredPosition = new Vector2(0f, -55f);
            oldRootRt.sizeDelta = new Vector2(540f, 560f);
            oldRootRt.localScale = Vector3.one * 0.5f;

            var updateRes = UpdateM2WorldMapLayout.Update(_scene);
            Assert.That(updateRes, Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.Success));

            GameObject root = FindInScene(_scene, RootName);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            RectTransform canvasRt = _canvas.GetComponent<RectTransform>();
            RectTransform advanceRt = FindRectInScene(_scene, "AdvanceButton");
            Rect rootBounds = GetRectInCanvas(canvasRt, rootRt);
            Rect advanceBounds = GetRectInCanvas(canvasRt, advanceRt);
            Assert.That(advanceBounds.yMin - rootBounds.yMax,
                Is.GreaterThanOrEqualTo(WorldMapLayout.M1Clearance - 0.01f),
                "M2 must be placed below the actual M1 button bounds");
            Assert.That(rootBounds.width, Is.GreaterThan(canvasRt.rect.width * 0.9f),
                "M2 must use most of the available Canvas width");
            Assert.That(rootRt.sizeDelta.x, Is.EqualTo(rootBounds.width).Within(0.01f));
            Assert.That(rootRt.sizeDelta.y, Is.EqualTo(rootBounds.height).Within(0.01f));
            Assert.That(rootRt.anchorMin, Is.EqualTo(rootRt.anchorMax),
                "the root should use one center anchor in Canvas-local coordinates");
            Assert.That(rootRt.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(rootRt.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void Update_16x9NonOverlapMath_MaintainsClearanceAcrossResolutions()
        {
            using (var preview = new FoundationScenePreview())
            {
                RectTransform panel = FindRectInScene(preview.Scene, "WorldClockPanel");
                RectTransform advance = FindRectInScene(preview.Scene, "AdvanceButton");
                RectTransform root = FindRectInScene(preview.Scene, RootName);
                panel.anchoredPosition += new Vector2(0f, -8f);

                Assert.That(UpdateM2WorldMapLayout.Update(preview.Scene),
                    Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.Success),
                    "the isolated FoundationScene preview must accept its actual M1 geometry");

                Rect canvasBounds = preview.CanvasRect.rect;
                float[] widths = { 1280f, 1920f, 2560f };
                float[] heights = { 720f, 1080f, 1440f };
                float[] expectedScales = { 1f, 1.5f, 2f };
                Rect panelBounds = GetRectInCanvas(preview.CanvasRect, panel);
                Rect advanceBounds = GetRectInCanvas(preview.CanvasRect, advance);
                Rect rootBounds = GetRectInCanvas(preview.CanvasRect, root);
                var advanceButton = advance.GetComponent<Button>();
                var raycaster = FindInScene(preview.Scene, "Canvas").GetComponent<GraphicRaycaster>();

                Assert.That(advanceButton, Is.Not.Null);
                Assert.That(advanceButton.isActiveAndEnabled && advanceButton.interactable, Is.True,
                    "the existing M1 AdvanceButton must remain operable");
                Assert.That(raycaster != null && raycaster.enabled, Is.True,
                    "the existing Canvas raycaster must remain enabled");

                for (int i = 0; i < widths.Length; i++)
                {
                    float scale = CanvasScaleFor(preview.Scaler, widths[i], heights[i]);
                    Assert.That(scale, Is.EqualTo(expectedScales[i]).Within(0.001f),
                        $"CanvasScaler factor at {widths[i]}x{heights[i]}");

                    Rect screenCanvas = ScaleRect(canvasBounds, scale);
                    Rect screenPanel = ScaleRect(panelBounds, scale);
                    Rect screenAdvance = ScaleRect(advanceBounds, scale);
                    Rect screenRoot = ScaleRect(rootBounds, scale);

                    Assert.That(screenRoot.xMin, Is.GreaterThanOrEqualTo(screenCanvas.xMin));
                    Assert.That(screenRoot.xMax, Is.LessThanOrEqualTo(screenCanvas.xMax));
                    Assert.That(screenRoot.yMin, Is.GreaterThanOrEqualTo(screenCanvas.yMin));
                    Assert.That(screenRoot.yMax, Is.LessThanOrEqualTo(screenCanvas.yMax));
                    Assert.That(screenRoot.Overlaps(screenPanel), Is.False,
                        $"M2 must not overlap the actual M1 date panel at {widths[i]}x{heights[i]}");
                    Assert.That(screenRoot.Overlaps(screenAdvance), Is.False,
                        $"M2 must not cover the actual M1 button at {widths[i]}x{heights[i]}");
                    Assert.That(screenAdvance.yMin - screenRoot.yMax,
                        Is.GreaterThanOrEqualTo(WorldMapLayout.M1Clearance * scale - 0.1f),
                        $"M1/M2 clearance must scale from actual scene bounds at {widths[i]}x{heights[i]}");
                }
            }
        }

        [Test]
        public void Layout_AdjacentRowsDoNotOverlap()
        {
            using (var preview = new FoundationScenePreview())
            {
                Assert.That(UpdateM2WorldMapLayout.Update(preview.Scene),
                    Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.Success));

                string[] visibleM2Elements =
                {
                    "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
                    "DestinationNameText", "TravelTimeText", "ArrivalDateText",
                    "DestButton_0", "DestButton_1", "DestButton_2", "ConfirmButton", "CancelButton",
                };
                for (int i = 0; i < visibleM2Elements.Length; i++)
                {
                    Rect first = GetRectInCanvas(preview.CanvasRect,
                        FindRectInScene(preview.Scene, visibleM2Elements[i]));
                    for (int j = i + 1; j < visibleM2Elements.Length; j++)
                    {
                        Rect second = GetRectInCanvas(preview.CanvasRect,
                            FindRectInScene(preview.Scene, visibleM2Elements[j]));
                        Assert.That(first.Overlaps(second), Is.False,
                            $"Actual M2 elements '{visibleM2Elements[i]}' and '{visibleM2Elements[j]}' must not overlap");
                    }
                }
            }
        }

        [Test]
        public void Update_ButtonsAndTextMeetReadableMinimumsAtTargetResolutions()
        {
            using (var preview = new FoundationScenePreview())
            {
                Assert.That(UpdateM2WorldMapLayout.Update(preview.Scene),
                    Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.Success));

                string[] buttonNames = { "DestButton_0", "DestButton_1", "DestButton_2", "ConfirmButton", "CancelButton" };
                string[] textNames =
                {
                    "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
                    "DestinationNameText", "TravelTimeText", "ArrivalDateText",
                };
                foreach (string name in buttonNames)
                {
                    RectTransform buttonRect = FindRectInScene(preview.Scene, name);
                    Button button = buttonRect.GetComponent<Button>();
                    Assert.That(button, Is.Not.Null, $"'{name}' must remain a Button");
                    Rect bounds = GetRectInCanvas(preview.CanvasRect, buttonRect);
                    Assert.That(bounds.width, Is.GreaterThanOrEqualTo(200f), $"'{name}' target width");
                    Assert.That(bounds.height, Is.GreaterThanOrEqualTo(48f), $"'{name}' target height");
                    Text label = button.GetComponentInChildren<Text>(true);
                    Assert.That(label, Is.Not.Null, $"'{name}' must retain its label");
                    Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(24), $"'{name}' label readability");
                }

                foreach (string name in textNames)
                {
                    Text text = FindInScene(preview.Scene, name).GetComponent<Text>();
                    Assert.That(text, Is.Not.Null, $"'{name}' must remain readable Text");
                    Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(18), $"'{name}' minimum font size");
                }

                float[] widths = { 1280f, 1920f, 2560f };
                float[] heights = { 720f, 1080f, 1440f };
                foreach (string name in buttonNames)
                {
                    Rect bounds = GetRectInCanvas(preview.CanvasRect, FindRectInScene(preview.Scene, name));
                    for (int i = 0; i < widths.Length; i++)
                    {
                        float scale = CanvasScaleFor(preview.Scaler, widths[i], heights[i]);
                        Assert.That(bounds.height * scale, Is.GreaterThanOrEqualTo(44f),
                            $"'{name}' must meet the minimum click-target height at {widths[i]}x{heights[i]}");
                    }
                }
            }
        }

        [Test]
        public void Update_InsufficientSafeArea_RefusesWithoutChangingExistingRoot()
        {
            using (var preview = new FoundationScenePreview())
            {
                RectTransform panel = FindRectInScene(preview.Scene, "WorldClockPanel");
                RectTransform root = FindRectInScene(preview.Scene, RootName);
                Vector2 originalAnchorMin = root.anchorMin;
                Vector2 originalAnchorMax = root.anchorMax;
                Vector2 originalAnchoredPosition = root.anchoredPosition;
                Vector2 originalSizeDelta = root.sizeDelta;
                Vector3 originalScale = root.localScale;

                // Move only the preview copy of M1 far enough down that the M2 minimum-height policy cannot fit.
                panel.anchoredPosition += new Vector2(0f, -60f);
                UpdateM2WorldMapLayout.UpdateResult result = UpdateM2WorldMapLayout.Update(preview.Scene);

                Assert.That(result, Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.InsufficientM2Space));
                Assert.That(root.anchorMin, Is.EqualTo(originalAnchorMin));
                Assert.That(root.anchorMax, Is.EqualTo(originalAnchorMax));
                Assert.That(root.anchoredPosition, Is.EqualTo(originalAnchoredPosition));
                Assert.That(root.sizeDelta, Is.EqualTo(originalSizeDelta));
                Assert.That(root.localScale, Is.EqualTo(originalScale));
            }
        }

        [Test]
        public void Update_UnknownChild_LeftUntouched()
        {
            var r = CreateM2WorldMap.Build(_scene, saveScene: false);
            Assert.That(r, Is.EqualTo(CreateM2WorldMap.BuildResult.Success));

            GameObject root = FindInScene(_scene, RootName);
            var unknown = CreateInPreviewScene("UnknownChild", typeof(RectTransform));
            unknown.transform.SetParent(root.transform, false);
            var unknownRt = unknown.GetComponent<RectTransform>();
            unknownRt.anchoredPosition = new Vector2(123f, 456f);

            var updateRes = UpdateM2WorldMapLayout.Update(_scene);
            Assert.That(updateRes, Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.Success));
            Assert.That(unknownRt.anchoredPosition, Is.EqualTo(new Vector2(123f, 456f)), "Unknown child transform must remain untouched");
        }

        [Test]
        public void Update_InvalidRequiredComponent_RefusesBeforeChangingRoot()
        {
            Assert.That(CreateM2WorldMap.Build(_scene, saveScene: false), Is.EqualTo(CreateM2WorldMap.BuildResult.Success));
            GameObject root = FindInScene(_scene, RootName);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMax = new Vector2(0.95f, 0.4f);

            GameObject title = FindInScene(_scene, "TitleText");
            Object.DestroyImmediate(title.GetComponent<Text>());

            var updateRes = UpdateM2WorldMapLayout.Update(_scene);

            Assert.That(updateRes, Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.InvalidRequiredComponents));
            Assert.That(rootRt.anchorMax, Is.EqualTo(new Vector2(0.95f, 0.4f)), "Preflight rejection must not alter the existing root");
        }

        [Test]
        public void Update_LayoutChangesCanBeUndone()
        {
            Assert.That(CreateM2WorldMap.Build(_scene, saveScene: false), Is.EqualTo(CreateM2WorldMap.BuildResult.Success));
            GameObject root = FindInScene(_scene, RootName);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMax = new Vector2(0.92f, 0.42f);

            var updateRes = UpdateM2WorldMapLayout.Update(_scene);
            Assert.That(updateRes, Is.EqualTo(UpdateM2WorldMapLayout.UpdateResult.Success));
            Assert.That(rootRt.anchorMax, Is.Not.EqualTo(new Vector2(0.92f, 0.42f)),
                "the updater must derive new root anchors from the actual M1 geometry");

            Undo.PerformUndo();

            Assert.That(rootRt.anchorMax, Is.EqualTo(new Vector2(0.92f, 0.42f)), "Undo must restore the root layout snapshot");
        }
    }
}
