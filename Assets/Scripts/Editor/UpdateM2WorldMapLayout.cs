using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Editor 菜单：Tools/Sengoku Chronicle/Update Existing World Map UI Layout。
    /// 仅修复已存在的 M2WorldMapRoot 布局；绝不创建/重建/删除/重父级化任何对象。
    /// 前置条件（全部满足才动）：活动场景 == FoundationScene、场景干净、
    /// 恰好一个 M2WorldMapRoot 且为 Canvas 直接子对象、全部必需后代唯一解析。
    /// 仅对已登记的 M2 RectTransform 执行 Undo（一个命名组）；异常时回滚该组。
    /// </summary>
    public static class UpdateM2WorldMapLayout
    {
        private const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        private const string MenuPath = "Tools/Sengoku Chronicle/Update Existing World Map UI Layout";
        private const string UndoName = "Update M2 World Map UI Layout";

        public enum UpdateResult
        {
            Success,
            SceneMismatch,
            SceneDirty,
            RootNotFound,
            MultipleRoots,
            RootNotDirectChildOfCanvas,
            MissingCanvas,
            MissingRequiredChildren,
            AmbiguousChild,
            MultipleCanvases,
            MissingM1Layout,
            InvalidCanvasGeometry,
            InsufficientM2Space,
            InvalidRequiredComponents,
            UnknownChildTouched,
            LayoutVerificationFailed,
            Unknown,
        }

        // ---- 菜单入口 ----
        [MenuItem(MenuPath, priority = 4)]
        public static void Update()
        {
            UpdateResult r = RunOnActiveScene();
            string msg = ResultMessage(r);
            if (r == UpdateResult.Success)
                Debug.Log($"UpdateM2WorldMapLayout: {msg}");
            else
                Debug.LogError($"UpdateM2WorldMapLayout: {msg}");
        }

        [MenuItem(MenuPath, true, priority = 4)]
        private static bool ValidateUpdate()
        {
            return EditorSceneManager.GetActiveScene().path == ScenePath;
        }

        /// <summary>对活跃场景执行预检 + 布局更新（不保存）。</summary>
        public static UpdateResult RunOnActiveScene()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return UpdateResult.SceneMismatch;
            if (scene.isDirty)
                return UpdateResult.SceneDirty;
            return Update(scene);
        }

        /// <summary>
        /// 测试注入点：对任意场景执行布局更新（跳过路径/脏检查）。
        /// 不保存；仅修改已登记的 M2 组件。
        /// </summary>
        public static UpdateResult Update(Scene scene)
        {
            if (!scene.IsValid())
                return UpdateResult.RootNotFound;

            // ---- 前置条件 ----
            GameObject root = FindSceneObject(scene, WorldMapLayout.RootName);
            if (root == null)
                return UpdateResult.RootNotFound;

            // 恰好一个 M2WorldMapRoot
            int rootCount = CountSceneObjects(scene, WorldMapLayout.RootName);
            if (rootCount != 1)
                return UpdateResult.MultipleRoots;

            GameObject canvas = FindSceneObject(scene, "Canvas");
            if (canvas == null)
                return UpdateResult.MissingCanvas;
            if (CountSceneObjects(scene, "Canvas") != 1)
                return UpdateResult.MultipleCanvases;
            if (root.transform.parent != canvas.transform)
                return UpdateResult.RootNotDirectChildOfCanvas;
            if (root.GetComponent<RectTransform>() == null || root.GetComponent<WorldMapController>() == null)
                return UpdateResult.InvalidRequiredComponents;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null || canvasRect.rect.width <= 0f || canvasRect.rect.height <= 0f)
                return UpdateResult.InvalidCanvasGeometry;
            if (!WorldMapLayout.TryFindM1ReservedRects(
                    canvas.transform,
                    out RectTransform worldClockPanel,
                    out RectTransform advanceButton,
                    out _))
                return UpdateResult.MissingM1Layout;
            RectTransform[] m1ReservedRects = { worldClockPanel, advanceButton };
            if (!WorldMapLayout.TryCalculateSafeArea(canvasRect, m1ReservedRects, out Rect safeArea, out string safeAreaError))
                return safeAreaError != null && safeAreaError.StartsWith("M1 对象", StringComparison.Ordinal)
                    ? UpdateResult.MissingM1Layout
                    : UpdateResult.InsufficientM2Space;

            // 全部必需后代唯一解析
            if (!WorldMapLayout.ResolveRequiredChildren(root.transform, out var children, out string error))
            {
                return error != null && error.Contains("出现")
                    ? UpdateResult.AmbiguousChild
                    : UpdateResult.MissingRequiredChildren;
            }
            if (!HasExpectedComponents(children))
                return UpdateResult.InvalidRequiredComponents;

            // ---- 执行布局 ----
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                Undo.RecordObject(rootRect, UndoName);
                WorldMapLayout.ApplyRootLayout(rootRect, canvasRect, safeArea);
                WorldMapLayout.ApplyChildLayout(root.transform,
                    recordUndo: (obj, name) => Undo.RecordObject(obj, name),
                    undoName: UndoName);

                EditorUtility.SetDirty(root);

                // 结构校验：全部子对象必须落在根矩形内
                if (!WorldMapLayout.ValidateContainment(root.transform))
                {
                    Rollback(group);
                    return UpdateResult.UnknownChildTouched;
                }
                if (!WorldMapLayout.ValidateRootSeparation(rootRect, canvasRect, m1ReservedRects))
                {
                    Rollback(group);
                    return UpdateResult.LayoutVerificationFailed;
                }

                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Rollback(group);
                return UpdateResult.Unknown;
            }
        }

        private static void Rollback(int group)
        {
            try
            {
                Undo.RevertAllDownToGroup(group);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // ---- 工具 ----
        private static GameObject FindSceneObject(Scene scene, string name)
        {
            if (!scene.IsValid())
                return null;
            return scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == name)?.gameObject;
        }

        private static int CountSceneObjects(Scene scene, string name)
        {
            if (!scene.IsValid())
                return 0;
            return scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .Count(t => t.name == name);
        }

        private static bool HasExpectedComponents(System.Collections.Generic.Dictionary<string, Transform> children)
        {
            string[] textNames =
            {
                "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
                "DestinationNameText", "TravelTimeText", "ArrivalDateText", "TownMenuTitleText",
                "Facility1Text", "Facility2Text", "Facility3Text",
            };
            foreach (string name in textNames)
            {
                if (children[name].GetComponent<Text>() == null)
                    return false;
            }

            string[] buttonNames =
            {
                "DestButton_0", "DestButton_1", "DestButton_2",
                "ConfirmButton", "CancelButton", "ReturnToMapButton",
            };
            foreach (string name in buttonNames)
            {
                Transform buttonTransform = children[name];
                if (buttonTransform.GetComponent<Button>() == null ||
                    buttonTransform.GetComponentsInChildren<Text>(true).Length != 1)
                    return false;
            }
            return children["TownMenuRoot"].GetComponent<RectTransform>() != null;
        }

        private static string ResultMessage(UpdateResult r)
        {
            switch (r)
            {
                case UpdateResult.Success:
                    return "已根据 Canvas 与 M1 实际 RectTransform 更新 M2 下方分区布局；未保存，可手动保存。";
                case UpdateResult.SceneMismatch:
                    return $"活跃场景不是 {ScenePath}，请先打开 FoundationScene。";
                case UpdateResult.SceneDirty:
                    return "FoundationScene 存在未保存更改，请先保存场景后重试（不会覆盖您的修改）。";
                case UpdateResult.RootNotFound:
                    return "场景中未找到 M2WorldMapRoot；若尚未创建，请使用 Tools/Sengoku Chronicle/Create World Map UI。";
                case UpdateResult.MultipleRoots:
                    return "场景中存在多个 M2WorldMapRoot，拒绝更新以免歧义。";
                case UpdateResult.RootNotDirectChildOfCanvas:
                    return "M2WorldMapRoot 不是 Canvas 的直接子对象，拒绝更新。";
                case UpdateResult.MissingCanvas:
                    return "场景中缺少 Canvas。";
                case UpdateResult.MultipleCanvases:
                    return "场景中存在多个 Canvas，拒绝更新以免目标歧义。";
                case UpdateResult.MissingM1Layout:
                    return "M1 WorldClockPanel / AdvanceButton 缺失、不可用或几何无效，未修改场景。";
                case UpdateResult.InvalidCanvasGeometry:
                    return "Canvas RectTransform 几何无效，未修改场景。";
                case UpdateResult.InsufficientM2Space:
                    return "M1 实际矩形下方没有足够的 M2 空间，未修改场景。";
                case UpdateResult.MissingRequiredChildren:
                    return "M2WorldMapRoot 缺少必需子对象，无法应用布局。";
                case UpdateResult.AmbiguousChild:
                    return "M2WorldMapRoot 下存在同名子对象，目标不唯一，拒绝更新。";
                case UpdateResult.InvalidRequiredComponents:
                    return "M2WorldMapRoot 或必需后代缺少预期 UI 组件，拒绝更新。";
                case UpdateResult.UnknownChildTouched:
                    return "布局校验失败：有子对象超出根面板范围，已回滚未修改。";
                case UpdateResult.LayoutVerificationFailed:
                    return "Canvas containment/M1 separation 校验失败，已通过 Undo 回滚未修改。";
                default:
                    return "未知错误，已回滚未修改。";
            }
        }
    }
}