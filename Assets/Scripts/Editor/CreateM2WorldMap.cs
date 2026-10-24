using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Editor 生成器：在 Assets/Scenes/FoundationScene.unity 的 Canvas 下，
    /// 新增 Canvas/M2WorldMapRoot 世界地图 UI，并绑定 WorldMapController。
    /// 仅使用 Unity Editor API，绝不手改 .unity YAML。
    /// 菜单：Tools/Sengoku Chronicle/Create World Map UI。
    /// </summary>
    public static class CreateM2WorldMap
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string MenuPath = "Tools/Sengoku Chronicle/Create World Map UI";
        public const string RootName = "M2WorldMapRoot";
        public const string UndoName = "Create M2 World Map UI";

        /// <summary>生成器返回码。</summary>
        public enum BuildResult
        {
            Success,
            AlreadyExists,
            SceneMismatch,
            SceneDirty,
            MissingCanvas,
            MissingEventSystem,
            MissingWorldClockPanel,
            MissingM1Layout,
            InvalidCanvasGeometry,
            InsufficientM2Space,
            MissingGameSession,
            BuildFailed,
            SaveFailed,
        }



        // ---- 测试注入点（仅在隔离测试场景中使用）----

        /// <summary>在对象创建+绑定完成、校验通过前触发返回 true 则模拟构建失败。</summary>
        public static Func<bool> FailAfterCreate { get; set; }

        /// <summary>在保存前触发返回 true 则模拟保存失败。</summary>
        public static Func<bool> FailOnSave { get; set; }

        /// <summary>最近一次 Build 失败阶段、原因与 Canvas/M1/M2 几何快照，供 EditMode 诊断。</summary>
        public static string LastBuildDiagnostics { get; private set; }

        // ---- 菜单入口 ----

        [MenuItem(MenuPath, priority = 3)]
        public static void Create()
        {
            BuildResult r = BuildOnActiveScene(save: true);
            LogResult(r);
        }

        [MenuItem(MenuPath, true, priority = 3)]
        private static bool ValidateCreate()
        {
            // 仅当活跃场景为 FoundationScene 时启用菜单。
            return EditorSceneManager.GetActiveScene().path == ScenePath;
        }


        /// <summary>对活跃场景执行预检 + 构建 + 保存。</summary>
        public static BuildResult BuildOnActiveScene(bool save)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return BuildResult.SceneMismatch;
            if (scene.isDirty)
                return BuildResult.SceneDirty;
            return Build(scene, saveScene: save);
        }


        // ---- 核心构建（可供隔离测试场景调用，跳过路径/脏检查）----

        /// <summary>
        /// 在给定场景上创建 M2WorldMapRoot。
        /// saveScene=true 时保存到 ScenePath；测试场景传入 false。
        /// 失败前从不保存；回滚仅撤销本生成器组，绝不删除未知对象。
        /// </summary>
        public static BuildResult Build(UnityEngine.SceneManagement.Scene scene, bool saveScene, string savePath = ScenePath)
        {
            LastBuildDiagnostics = null;
            if (!scene.IsValid())
            {
                LastBuildDiagnostics = "phase=Preflight; reason=target Scene is invalid.";
                return BuildResult.SceneMismatch;
            }

            GameObject existing = FindSceneObject(scene, RootName);
            if (existing != null)
            {
                LastBuildDiagnostics = $"phase=Preflight; reason=existing '{RootName}' prevents duplicate creation.";
                return BuildResult.AlreadyExists;
            }

            GameObject canvas = FindSceneObject(scene, "Canvas");
            if (canvas == null)
            {
                LastBuildDiagnostics = "phase=Preflight; reason=Canvas reference is missing.";
                return BuildResult.MissingCanvas;
            }
            GameObject eventSystem = FindSceneObject(scene, "EventSystem");
            if (eventSystem == null)
            {
                LastBuildDiagnostics = "phase=Preflight; reason=EventSystem reference is missing.";
                return BuildResult.MissingEventSystem;
            }
            GameObject sessionGo = FindSceneObject(scene, "GameSession");
            GameSession session = sessionGo != null ? sessionGo.GetComponent<GameSession>() : null;
            if (session == null)
            {
                LastBuildDiagnostics = "phase=Preflight; reason=GameSession reference is missing.";
                return BuildResult.MissingGameSession;
            }
            GameObject wcpGo = FindSceneObject(scene, "WorldClockPanel");
            WorldClockPanel clock = wcpGo != null ? wcpGo.GetComponent<WorldClockPanel>() : null;
            if (clock == null)
            {
                LastBuildDiagnostics = "phase=Preflight; reason=WorldClockPanel reference is missing.";
                return BuildResult.MissingWorldClockPanel;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null || canvasRect.rect.width <= 0f || canvasRect.rect.height <= 0f)
            {
                LastBuildDiagnostics = FormatBuildDiagnostics(
                    "Preflight", "Canvas RectTransform is missing or has non-positive geometry.",
                    canvasRect, null, null, default, null);
                return BuildResult.InvalidCanvasGeometry;
            }
            if (!WorldMapLayout.TryFindM1ReservedRects(
                    canvas.transform,
                    out RectTransform worldClockPanel,
                    out RectTransform advanceButton,
                    out string m1LayoutError))
            {
                LastBuildDiagnostics = FormatBuildDiagnostics(
                    "Preflight", m1LayoutError, canvasRect, worldClockPanel,
                    advanceButton, default, null);
                return BuildResult.MissingM1Layout;
            }
            RectTransform[] m1ReservedRects = { worldClockPanel, advanceButton };
            if (!WorldMapLayout.TryCalculateSafeArea(canvasRect, m1ReservedRects, out Rect safeArea, out string safeAreaError))
            {
                LastBuildDiagnostics = FormatBuildDiagnostics(
                    "Preflight", safeAreaError, canvasRect, worldClockPanel,
                    advanceButton, safeArea, null);
                return safeAreaError != null && safeAreaError.StartsWith("M1 对象", StringComparison.Ordinal)
                    ? BuildResult.MissingM1Layout
                    : BuildResult.InsufficientM2Space;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            string buildPhase = "BuildRoot";
            GameObject root = null;
            try
            {
                root = BuildRoot(canvas.transform, session, clock, canvasRect, safeArea);
                if (root == null)
                {
                    LastBuildDiagnostics = FormatBuildDiagnostics(
                        buildPhase, "BuildRoot returned null.", canvasRect, worldClockPanel,
                        advanceButton, safeArea, null);
                    Rollback(group);
                    return BuildResult.BuildFailed;
                }

                // 注入故障钩子（仅用于隔离测试场景）。
                buildPhase = "FailAfterCreate hook";
                if (FailAfterCreate != null && FailAfterCreate())
                {
                    LastBuildDiagnostics = FormatBuildDiagnostics(
                        buildPhase, "Injected failure requested rollback.", canvasRect,
                        worldClockPanel, advanceButton, safeArea, root.GetComponent<RectTransform>());
                    Rollback(group);
                    return BuildResult.BuildFailed;
                }

                buildPhase = "Validate";
                if (!Validate(root, session, canvas, canvasRect, m1ReservedRects, out string validationFailure))
                {
                    LastBuildDiagnostics = FormatBuildDiagnostics(
                        buildPhase, validationFailure, canvasRect, worldClockPanel,
                        advanceButton, safeArea, root.GetComponent<RectTransform>());
                    Rollback(group);
                    return BuildResult.BuildFailed;
                }

                if (saveScene)
                {
                    buildPhase = "Save";
                    if (FailOnSave != null && FailOnSave())
                    {
                        LastBuildDiagnostics = FormatBuildDiagnostics(
                            buildPhase, "Injected save failure requested rollback before SaveScene.",
                            canvasRect, worldClockPanel, advanceButton, safeArea,
                            root.GetComponent<RectTransform>());
                        Rollback(group);
                        return BuildResult.SaveFailed;
                    }
                    // Unity 文档不承诺保存失败时 on-disk 字节完好：失败前已执行回滚，
                    // 且跳过保存，因此场景文件不被写入。
                    if (!EditorSceneManager.SaveScene(scene, savePath, false))
                    {
                        LastBuildDiagnostics = FormatBuildDiagnostics(
                            buildPhase, $"SaveScene returned false for '{savePath}'.",
                            canvasRect, worldClockPanel, advanceButton, safeArea,
                            root.GetComponent<RectTransform>());
                        Rollback(group);
                        return BuildResult.SaveFailed;
                    }
                }

                Undo.CollapseUndoOperations(group);
                return BuildResult.Success;
            }
            catch (Exception ex)
            {
                LastBuildDiagnostics = FormatBuildDiagnostics(
                    buildPhase, $"Exception {ex.GetType().FullName}: {ex.Message}",
                    canvasRect, worldClockPanel, advanceButton, safeArea,
                    root != null ? root.GetComponent<RectTransform>() : null);
                Debug.LogException(ex);
                Rollback(group);
                return BuildResult.BuildFailed;
            }
        }

        // ---- 构建 ----

        private static GameObject BuildRoot(
            Transform canvasParent,
            GameSession session,
            WorldClockPanel clock,
            RectTransform canvasRect,
            Rect safeArea)
        {
            Font font = GetDefaultFont();

            // 根对象
            GameObject root = CreateGameObjectInScene(RootName, canvasParent.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            RectTransform rootRt = Undo.AddComponent<RectTransform>(root);
            CanvasGroup cg = Undo.AddComponent<CanvasGroup>(root); // 用于禁用互动
            Image rootImg = Undo.AddComponent<Image>(root);
            rootImg.color = new Color(0.1f, 0.1f, 0.14f, 0.92f);

            Undo.SetTransformParent(root.transform, canvasParent, "Reparent M2WorldMapRoot");
            WorldMapLayout.ApplyRootLayout(rootRt, canvasRect, safeArea);

            var controller = Undo.AddComponent<WorldMapController>(root);

            // 标题
            Text titleText = CreateUIText(root.transform, "TitleText", font, "世界地图", 28, TextAnchor.MiddleCenter,
                new Vector2(0, 255), new Vector2(400, 50));

            // 当前位置 + 当前日期
            Text markerText = CreateUIText(root.transform, "LocationMarkerText", font, "当前位置：清洲", 20, TextAnchor.MiddleCenter,
                new Vector2(0, 210), new Vector2(400, 34));
            Text currentDateText = CreateUIText(root.transform, "CurrentDateText", font, "日期：1560-04-01", 18, TextAnchor.MiddleCenter,
                new Vector2(0, 180), new Vector2(400, 30));

            // 反馈
            Text feedbackText = CreateUIText(root.transform, "FeedbackText", font, "请选择目的地。", 16, TextAnchor.MiddleCenter,
                new Vector2(0, 150), new Vector2(440, 36), new Color(0.9f, 0.9f, 0.75f, 1f));

            // 目的地信息
            Text destNameText = CreateUIText(root.transform, "DestinationNameText", font, "—", 18, TextAnchor.MiddleLeft,
                new Vector2(-170, 100), new Vector2(110, 30), new Color(0.9f, 0.9f, 0.9f, 1f));
            Text travelTimeText = CreateUIText(root.transform, "TravelTimeText", font, "—", 18, TextAnchor.MiddleCenter,
                new Vector2(0, 100), new Vector2(110, 30));
            Text arrivalDateText = CreateUIText(root.transform, "ArrivalDateText", font, "—", 18, TextAnchor.MiddleRight,
                new Vector2(170, 100), new Vector2(110, 30));

            // 三个目的地按钮（点击监听由 controller.BindLocationButtons 接管；此处仅构建按钮）
            LocationId[] ids = { LocationId.Kiyosu, LocationId.Atsuta, LocationId.Nagoya };
            string[] locLabels = { "清洲", "热田", "那古野" };
            float[] xoffs = { -170f, 0f, 170f };
            Button[] destButtons = new Button[3];
            Text[] destLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                string goName = $"DestButton_{i}";
                Button b = CreateButton(root.transform, goName, font, locLabels[i],
                    new Vector2(xoffs[i], -20), new Vector2(160, 80),
                    new Color(0.18f, 0.18f, 0.2f, 1f));
                destLabels[i] = b.GetComponentInChildren<Text>(true);
                destButtons[i] = b;
            }

            // 确认 / 取消按钮
            Button confirmBtn = CreateButton(root.transform, "ConfirmButton", font, "确认出发",
                new Vector2(-100, -90), new Vector2(180, 80),
                new Color(0.18f, 0.48f, 0.22f, 1f));
            Button cancelBtn = CreateButton(root.transform, "CancelButton", font, "取消",
                new Vector2(100, -90), new Vector2(180, 80),
                new Color(0.32f, 0.32f, 0.36f, 1f));

            // 城镇菜单（初始隐藏）
            GameObject townRoot = CreateGameObjectInScene("TownMenuRoot", root.scene);
            Undo.RegisterCreatedObjectUndo(townRoot, UndoName);
            townRoot.SetActive(false);
            Undo.SetTransformParent(townRoot.transform, root.transform, "Parent TownMenuRoot");
            RectTransform townRt = Undo.AddComponent<RectTransform>(townRoot);
            townRt.anchorMin = Vector2.zero;
            townRt.anchorMax = Vector2.one;
            townRt.offsetMin = new Vector2(12, 12);
            townRt.offsetMax = new Vector2(-12, -12);
            Image townImg = Undo.AddComponent<Image>(townRoot);
            townImg.color = new Color(0.07f, 0.07f, 0.1f, 0.96f);

            Text townTitleText = CreateUIText(townRoot.transform, "TownMenuTitleText", font, "城镇", 26, TextAnchor.MiddleCenter,
                new Vector2(0, 210), new Vector2(400, 50));
            Text fac1 = CreateUIText(townRoot.transform, "Facility1Text", font, "设施一：未开放", 18, TextAnchor.MiddleLeft,
                new Vector2(0, 90), new Vector2(380, 36), new Color(0.7f, 0.7f, 0.7f, 1f));
            Text fac2 = CreateUIText(townRoot.transform, "Facility2Text", font, "设施二：未开放", 18, TextAnchor.MiddleLeft,
                new Vector2(0, 30), new Vector2(380, 36), new Color(0.7f, 0.7f, 0.7f, 1f));
            Text fac3 = CreateUIText(townRoot.transform, "Facility3Text", font, "设施三：未开放", 18, TextAnchor.MiddleLeft,
                new Vector2(0, -30), new Vector2(380, 36), new Color(0.7f, 0.7f, 0.7f, 1f));
            Button returnBtn = CreateButton(townRoot.transform, "ReturnToMapButton", font, "返回地图",
                new Vector2(0, -150), new Vector2(220, 80),
                new Color(0.18f, 0.18f, 0.2f, 1f));

            // 绑定序列化引用（类型安全，撤销可记录）。
            Undo.RecordObject(controller, UndoName);
            controller.BindReferences(
                session, clock,
                markerText, feedbackText, currentDateText,
                destNameText, travelTimeText, arrivalDateText,
                confirmBtn, cancelBtn, returnBtn,
                townRoot, townTitleText,
                new Text[] { fac1, fac2, fac3 });
            controller.BindLocationButtons(
                destButtons[0], destLabels[0],
                destButtons[1], destLabels[1],
                destButtons[2], destLabels[2]);

            WorldMapLayout.ApplyChildLayout(root.transform);

            EditorUtility.SetDirty(controller);

            return root;
        }

        // ------------------------------------------------------------------
        // 校验：验证生成的层次结构、组件与序列化引用。
        // ------------------------------------------------------------------
        private static bool Validate(
            GameObject root,
            GameSession session,
            GameObject canvas,
            RectTransform canvasRect,
            RectTransform[] m1ReservedRects,
            out string failure)
        {
            failure = null;
            if (root == null || root.name != RootName)
            {
                failure = "Created root is null or has an unexpected name.";
                return false;
            }
            if (root.transform.parent == null || root.transform.parent != canvas.transform)
            {
                failure = "Created root is not parented directly under the target Canvas.";
                return false;
            }
            if (root.GetComponent<RectTransform>() == null)
            {
                failure = "Created root has no RectTransform.";
                return false;
            }
            var controller = root.GetComponent<WorldMapController>();
            if (controller == null)
            {
                failure = "Created root has no WorldMapController.";
                return false;
            }
            if (controller.Session == null || controller.Session != session)
            {
                failure = "WorldMapController session reference is missing or differs from the target session.";
                return false;
            }
            if (!WorldMapLayout.ValidateContainment(root.transform, out string containmentError))
            {
                failure = $"M2 containment failed: {containmentError}";
                return false;
            }
            if (!WorldMapLayout.ValidateRootSeparation(root.GetComponent<RectTransform>(), canvasRect, m1ReservedRects))
            {
                failure = "M2 root is outside the Canvas or violates separation from an M1 reserved rect.";
                return false;
            }
            if (controller.GetTownMenuRoot() == null)
            {
                failure = "WorldMapController town menu root reference is missing.";
                return false;
            }
            if (controller.GetFacilityTexts() == null || controller.GetFacilityTexts().Length != 3)
            {
                failure = "WorldMapController facility text array is missing or not length three.";
                return false;
            }
            if (controller.GetFacilityTexts().Any(t => t == null))
            {
                failure = "WorldMapController facility text array contains a missing reference.";
                return false;
            }
            // 校验所需子对象存在。
            string[] required = { "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
                "DestinationNameText", "TravelTimeText", "ArrivalDateText",
                "ConfirmButton", "CancelButton", "ReturnToMapButton",
                "TownMenuRoot" };
            foreach (string n in required)
            {
                if (FindChild(root.transform, n) == null)
                {
                    failure = $"Required descendant '{n}' is missing.";
                    return false;
                }
            }
            return true;
        }

        private static string FormatBuildDiagnostics(
            string phase,
            string reason,
            RectTransform canvas,
            RectTransform worldClockPanel,
            RectTransform advanceButton,
            Rect safeArea,
            RectTransform root)
        {
            string canvasRect = canvas != null ? canvas.rect.ToString() : "<missing>";
            return $"phase={phase}; reason={reason}; CanvasRect={canvasRect}; " +
                   $"M1.WorldClockPanel={DescribeBounds(canvas, worldClockPanel)}; " +
                   $"M1.AdvanceButton={DescribeBounds(canvas, advanceButton)}; " +
                   $"safeArea={safeArea}; M2.root={DescribeBounds(canvas, root)}; " +
                   $"spaceInsufficient={reason != null && reason.Contains("空间不足")}; " +
                   $"referencesMissing={reason != null && (reason.Contains("missing") || reason.Contains("缺失") || reason.Contains("缺少"))}; " +
                   $"containmentFailed={reason != null && reason.IndexOf("containment", StringComparison.OrdinalIgnoreCase) >= 0}";
        }

        private static string DescribeBounds(RectTransform canvas, RectTransform target)
        {
            if (canvas == null || target == null)
                return "<missing>";
            try
            {
                return WorldMapLayout.GetCanvasLocalBounds(canvas, target).ToString();
            }
            catch (Exception ex)
            {
                return $"<unavailable:{ex.GetType().Name}>";
            }
        }

        private static void Rollback(int group)
        {
            // 仅回滚本生成器组：撤销该组及其之上的所有操作（创建的对象会被销毁，修改被还原）。
            // 绝不 force-reload 场景，绝不删除未知对象。
            try
            {
                Undo.RevertAllDownToGroup(group);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static GameObject FindSceneObject(UnityEngine.SceneManagement.Scene scene, string name)
        {
            if (!scene.IsValid())
                return null;
            return scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == name)?.gameObject;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            // 递归查找后代（不含 parent 自身）。
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                Transform found = FindChild(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static GameObject CreateGameObjectInScene(string name, UnityEngine.SceneManagement.Scene scene,
            params Type[] components)
        {
            // Create transiently hidden, then move into the explicitly requested scene before
            // registering Undo or adding mutable components. This also supports non-active
            // Preview Scenes without changing the Editor's active scene.
            GameObject go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, components);
            try
            {
                if (go.scene != scene)
                    SceneManager.MoveGameObjectToScene(go, scene);
                go.hideFlags = HideFlags.None;
                return go;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(go);
                throw;
            }
        }

        private static Text CreateUIText(Transform parent, string name, Font font, string text, int size,
            TextAnchor anchor, Vector2 anchoredPos, Vector2 sizeDelta, Color? color = null)
        {
            GameObject go = CreateGameObjectInScene(name, parent.gameObject.scene, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Text t = Undo.AddComponent<Text>(go);
            if (font != null)
                t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color ?? Color.white;
            return t;
        }

        private static Button CreateButton(Transform parent, string name, Font font, string label,
            Vector2 anchoredPos, Vector2 sizeDelta, Color? bgColor = null)
        {
            GameObject go = CreateGameObjectInScene(name, parent.gameObject.scene,
                typeof(RectTransform), typeof(CanvasRenderer));
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Image img = Undo.AddComponent<Image>(go);
            img.color = bgColor ?? new Color(0.18f, 0.18f, 0.2f, 1f);
            Button b = Undo.AddComponent<Button>(go);

            GameObject labelGo = CreateGameObjectInScene("Label", go.scene, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoName);
            Undo.SetTransformParent(labelGo.transform, go.transform, UndoName);
            RectTransform lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            lrt.localScale = Vector3.one;
            Text lt = Undo.AddComponent<Text>(labelGo);
            if (font != null)
                lt.font = font;
            lt.text = label;
            lt.color = Color.white;
            lt.fontSize = 24;
            lt.alignment = TextAnchor.MiddleCenter;

            return b;
        }

        private static Font GetDefaultFont()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                return null;
            }
        }

        private static void LogResult(BuildResult r)
        {
            string msg = ResultMessage(r);
            if (r == BuildResult.Success)
                Debug.Log($"CreateM2WorldMap: {msg}");
            else
                Debug.LogError($"CreateM2WorldMap: {msg}");
        }

        private static string ResultMessage(BuildResult r)
        {
            switch (r)
            {
                case BuildResult.Success: return $"已在 {ScenePath} 的 Canvas/M2WorldMapRoot 下创建世界地图 UI。";
                case BuildResult.AlreadyExists: return $"已存在名为 {RootName} 的对象，放弃以防覆盖/删除。";
                case BuildResult.SceneMismatch: return $"活跃场景不是 {ScenePath}，请先打开 FoundationScene。";
                case BuildResult.SceneDirty: return "FoundationScene 存在未保存更改，请先保存场景后重试（不会覆盖您的修改）。";
                case BuildResult.MissingCanvas: return "场景中缺少 Canvas，无法放置 UI。";
                case BuildResult.MissingEventSystem: return "场景中缺少 EventSystem，无法响应 UI 输入。";
                case BuildResult.MissingWorldClockPanel: return "场景中缺少 WorldClockPanel（M1 依赖）。";
                case BuildResult.MissingM1Layout: return "M1 WorldClockPanel / AdvanceButton 缺失、不可用或几何无效，未创建 M2。";
                case BuildResult.InvalidCanvasGeometry: return "Canvas RectTransform 几何无效，未创建 M2。";
                case BuildResult.InsufficientM2Space: return "M1 实际矩形下方空间不足，未创建 M2。";
                case BuildResult.MissingGameSession: return "场景中缺少 GameSession。";
                case BuildResult.BuildFailed: return "构建过程中出错，已执行 Undo 回滚，未保存。";
                case BuildResult.SaveFailed: return "保存场景失败，已执行 Undo 回滚，未写入文件。";
                default: return "未知结果。";
            }
        }
    }
}
