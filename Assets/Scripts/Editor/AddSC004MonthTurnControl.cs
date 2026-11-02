using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Adds the SC-004 month-turn control («结束月份») to the existing clock panel
    /// in one Undo group, under one fixed generated root (MonthTurnPanel).
    /// Validates hierarchy, unique controls, serialized bindings, preservation of the
    /// existing day button, and non-overlapping geometry before saving FoundationScene.
    /// A structural update already present counts as success (idempotent).
    /// </summary>
    public static class AddSC004MonthTurnControl
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string PanelName = "MonthTurnPanel";
        public const string ButtonName = "EndMonthButton";
        public const string LabelName = "Label";
        public const string ButtonLabelText = "结束月份";

        private const string CanvasName = "Canvas";
        private const string ClockPanelName = "WorldClockPanel";
        private const string MenuPath = "Tools/Sengoku Chronicle/Add SC-004 Month Turn Control";
        private const string UndoName = "Add SC-004 Month Turn Control";

        // Fixed layout inside the 560x200 clock panel: the transparent root sits at
        // the vertical centre (local y -60..60); the button spans y -22..34, leaving a
        // 6-unit gap to the AdvanceButton top (-28) and a 6-unit gap to DateText bottom (40).
        private const float RootWidth = 320f;
        private const float RootHeight = 120f;
        private const float ButtonWidth = 300f;
        private const float ButtonHeight = 56f;
        private const float ButtonCenterY = 6f;
        private const int ButtonFontSize = 28;

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, InvalidHierarchyOrBindings, ExistingPartialControl,
            ValidationFailed, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.EditorPlaying : CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("SC-004 month turn control validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-004 month turn updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>
        /// Batch entry point: opens FoundationScene, runs the transactional updater,
        /// validates, saves only after validation, then writes the scene report.
        /// </summary>
        public static void RunBatchIntegration()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);

            FoundationSceneValidator.ValidateActiveScene();
            if (result != UpdateResult.Success)
                throw new InvalidOperationException($"SC-004 month turn integration failed: {result}.");

            Debug.Log("SC-004 month turn integration completed and FoundationScene was saved.");
        }

        /// <summary>Structural test seam. It never saves the supplied scene.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault-injection seam, executed inside the same Undo transaction as construction.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation);

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return Invalid(out error, "Scene is invalid or not loaded.");

            Transform[] canvasAll = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            if (canvasAll.Length != 1)
                return Invalid(out error, "Expected exactly one Canvas root.");

            WorldClockPanel[] panels = Components<WorldClockPanel>(scene);
            GameSession[] sessions = Components<GameSession>(scene);
            WorldMapController[] controllers = Components<WorldMapController>(scene);
            if (panels.Length != 1 || sessions.Length != 1 || controllers.Length != 1)
                return Invalid(out error, "Expected exactly one WorldClockPanel, one GameSession, and one WorldMapController.");

            WorldClockPanel panel = panels[0];
            Transform panelTransform = panel.transform;
            if (panelTransform.name != ClockPanelName || panelTransform.parent != canvasAll[0])
                return Invalid(out error, $"WorldClockPanel must be the single {ClockPanelName} child of the Canvas.");
            RectTransform panelRect = panelTransform as RectTransform;
            if (panelRect == null)
                return Invalid(out error, "WorldClockPanel must have a RectTransform.");

            // Pre-existing M1 bindings (date text + day button) must stay intact.
            if (!Bound(panel, "gameSession", sessions[0]))
                return Invalid(out error, "Clock panel is not bound to the GameSession.");
            Text dateText = GetBound(panel, "dateText") as Text;
            Button dayButton = GetBound(panel, "advanceButton") as Button;
            if (dateText == null || dateText.font == null)
                return Invalid(out error, "Clock panel date text is invalid or unbound.");
            if (dayButton == null)
                return Invalid(out error, "The existing day button binding was lost.");
            if (dayButton.transform.parent != panelTransform || dateText.transform.parent != panelTransform)
                return Invalid(out error, "Day button and date text must remain children of the clock panel.");

            // One fixed generated root, exactly one month button beneath it.
            Transform[] roots = Named(scene, PanelName);
            if (roots.Length != 1)
                return Invalid(out error, $"Expected exactly one {PanelName} in the scene.");
            Transform root = roots[0];
            if (root.parent != panelTransform)
                return Invalid(out error, $"{PanelName} must be a direct child of the clock panel.");

            Transform[] buttons = Named(scene, ButtonName);
            if (buttons.Length != 1)
                return Invalid(out error, $"Expected exactly one {ButtonName} in the scene.");
            Transform buttonTransform = buttons[0];
            if (buttonTransform.parent != root)
                return Invalid(out error, $"{ButtonName} must be a direct child of the {PanelName} root.");
            Button monthButton = buttonTransform.GetComponent<Button>();
            Image graphic = buttonTransform.GetComponent<Image>();
            if (monthButton == null || graphic == null || monthButton.targetGraphic != graphic)
                return Invalid(out error, "Month button is invalid or has no target graphic.");

            Transform[] labels = DirectChildren(buttonTransform, LabelName);
            if (labels.Length != 1 || buttonTransform.GetComponentsInChildren<Transform>(true).Length != 2)
                return Invalid(out error, "Month button must have exactly one label child.");
            Text labelText = labels[0].GetComponent<Text>();
            if (labelText == null || labelText.font == null || labelText.text != ButtonLabelText)
                return Invalid(out error, $"Month button label must read “{ButtonLabelText}”.");

            // Panel bindings for the new monthly turn.
            if (!Bound(panel, "endMonthButton", monthButton))
                return Invalid(out error, "Clock panel endMonthButton is not bound to the month button.");
            if (!Bound(panel, "worldMapController", controllers[0]))
                return Invalid(out error, "Clock panel worldMapController is not bound for date refresh.");

            // Geometry: fixed root centered in the panel; month button clear of day controls.
            RectTransform rootRect = root as RectTransform;
            if (rootRect == null || !Contained(rootRect, panelRect))
                return Invalid(out error, "Month turn root is outside the clock panel bounds.");
            RectTransform buttonRect = buttonTransform as RectTransform;
            if (buttonRect == null || !Contained(buttonRect, panelRect))
                return Invalid(out error, "Month button is outside the clock panel bounds.");
            if (Overlaps(buttonRect, dayButton.transform as RectTransform, panelRect))
                return Invalid(out error, "Month button overlaps the existing day button.");
            if (Overlaps(buttonRect, dateText.transform as RectTransform, panelRect))
                return Invalid(out error, "Month button overlaps the date text.");

            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded) return UpdateResult.InvalidScene;

            Transform[] canvasAll = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            WorldClockPanel[] panels = Components<WorldClockPanel>(scene);
            GameSession[] sessions = Components<GameSession>(scene);
            WorldMapController[] controllers = Components<WorldMapController>(scene);
            Transform canvas = canvasAll.Length == 1 ? canvasAll[0] : null;
            WorldClockPanel panel = panels.Length == 1 ? panels[0] : null;
            WorldMapController controller = controllers.Length == 1 ? controllers[0] : null;
            if (canvas == null || panel == null || controller == null || sessions.Length != 1 ||
                panel.transform.name != ClockPanelName || panel.transform.parent != canvas ||
                panel.GetComponent<RectTransform>() == null)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            // Refuse to merge into partially applied or foreign month controls.
            if (Named(scene, PanelName).Length != 0 ||
                Named(scene, ButtonName).Length != 0 ||
                GetBound(panel, "endMonthButton") != null ||
                GetBound(panel, "worldMapController") != null)
                return UpdateResult.ExistingPartialControl;

            Button dayButton = GetBound(panel, "advanceButton") as Button;
            Text dateText = GetBound(panel, "dateText") as Text;
            if (dayButton == null || dateText == null || dayButton.transform.parent != panel.transform)
                return UpdateResult.InvalidHierarchyOrBindings;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                // One fixed generated root beneath the clock panel.
                GameObject rootGo = Create(scene, panel.transform, PanelName);
                RectTransform rootRect = rootGo.GetComponent<RectTransform>();
                rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = new Vector2(RootWidth, RootHeight);

                // Month button with its label, centred between day button and date text.
                GameObject buttonGo = Create(scene, rootRect, ButtonName, typeof(Image), typeof(Button));
                RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
                buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = new Vector2(0f, ButtonCenterY);
                buttonRect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
                Image image = buttonGo.GetComponent<Image>();
                image.color = new Color(0.22f, 0.32f, 0.42f, 1f);
                Button monthButton = buttonGo.GetComponent<Button>();
                monthButton.targetGraphic = image;

                GameObject labelGo = Create(scene, buttonRect, LabelName, typeof(Text));
                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                Text labelText = labelGo.GetComponent<Text>();
                labelText.font = font;
                labelText.fontSize = ButtonFontSize;
                labelText.text = ButtonLabelText;
                labelText.color = Color.white;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
                labelText.raycastTarget = false;

                Undo.RecordObject(panel, UndoName);
                panel.BindMonthTurn(monthButton, controller);
                // FoundationScene's clock panel was serialized before endMonthButton/worldMapController
                // existed. On a scene-loaded component a managed-only assignment can leave the serialized
                // state — what SerializedObject validation reads and SaveScene writes — unbound, which is
                // the "endMonthButton is not bound" class of failure reported for the live repro. Re-apply
                // the same references through the serialized layer so both views agree on stale-layout
                // scene objects; the pre-change Undo.RecordObject snapshot, one-group rollback, and
                // idempotence are unchanged.
                SerializedObject bindings = new SerializedObject(panel);
                bindings.FindProperty("endMonthButton").objectReferenceValue = monthButton;
                bindings.FindProperty("worldMapController").objectReferenceValue = controller;
                bindings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(panel);

                beforeValidation?.Invoke(rootGo);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error)) throw new InvalidOperationException(error);

                Undo.FlushUndoRecordObjects();
                EditorSceneManager.MarkSceneDirty(scene);
                failure = UpdateResult.SaveFailed;
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");

                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogWarning($"SC-004 month turn updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            UnityEngine.Object actual = GetBound(target, field);
            return actual != null && expected != null && actual == expected;
        }

        private static UnityEngine.Object GetBound(UnityEngine.Object target, string field)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null ? property.objectReferenceValue : null;
        }

        private static GameObject Create(Scene scene, Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            foreach (Type component in components)
                Undo.AddComponent(go, component);
            return go;
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(c =>
            {
                Vector3 point = parent.InverseTransformPoint(c);
                return bounds.Contains(new Vector2(point.x, point.y));
            });
        }

        private static bool Overlaps(RectTransform first, RectTransform second, RectTransform coordinateRoot)
        {
            if (first == null || second == null)
                return false;

            Rect ToRoot(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector3 a = coordinateRoot.InverseTransformPoint(corners[0]);
                Vector3 c = coordinateRoot.InverseTransformPoint(corners[2]);
                return Rect.MinMaxRect(
                    Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y),
                    Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
            }

            return ToRoot(first).Overlaps(ToRoot(second), true);
        }

        private static Transform[] DirectChildren(Transform parent, string name)
        {
            var matches = new List<Transform>();
            foreach (Transform child in parent)
                if (child != null && child.name == name)
                    matches.Add(child);
            return matches.ToArray();
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }

        private static Transform[] Named(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name)
            .ToArray();

        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true))
            .ToArray();
    }
}
