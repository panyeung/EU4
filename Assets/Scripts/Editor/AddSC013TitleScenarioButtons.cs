using System;
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
    /// Transactional FoundationScene updater for the two separate SC-010 title entries.
    /// The base title generator still owns the four original controls; this updater adds
    /// the standard-sample and crisis-demo buttons under the same fixed title root.
    /// </summary>
    public static class AddSC013TitleScenarioButtons
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string CanvasName = "Canvas";
        public const string RootName = "M6TitleRoot";
        public const string StandardButtonName = "SC013StandardSampleButton";
        public const string CrisisButtonName = "SC013CrisisDemoButton";
        public const string StatusTextName = "M6TitleStatusText";
        public const string EducationButtonName = "SC020EducationDemoButton";
        public const string ResearchButtonName = "SC021ResearchDemoButton";
        public const string FamilyFormationButtonName = "SC022FamilyFormationDemoButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-013 Title Scenario Buttons";

        private const string UndoName = "Add SC-013 Title Scenario Buttons";
        private const int BaseChildCount = 4;
        private const int CompleteChildCount = 6;

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
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty,
                EditorApplication.isPlayingOrWillChangePlaymode);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("SC-013 title scenario buttons validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-013 title scenario updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty) =>
            CheckScenePreflight(path, dirty, false);

        public static UpdateResult CheckScenePreflight(string path, bool dirty, bool editorPlaying)
        {
            if (editorPlaying) return UpdateResult.EditorPlaying;
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam. It never saves, opens, or reloads the supplied scene.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault-injection test seam inside the same Undo transaction as construction.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation);

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return Invalid(out error, "Scene is invalid or not loaded.");

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || controllers.Length != 1 || roots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, TitleScreenController and title root.");

            Transform root = roots[0];
            Transform standard = Require(scene, StandardButtonName);
            Transform crisis = Require(scene, CrisisButtonName);
            Transform status = Require(scene, StatusTextName);
            if (standard == null || crisis == null || status == null)
                return Invalid(out error, "SC-013 title scenario entries or status text are missing.");
            if (root.parent != canvases[0])
                return Invalid(out error, "M6TitleRoot must be a direct child of Canvas.");
            if (root.gameObject.activeSelf)
                return Invalid(out error, "M6TitleRoot must be saved inactive; Play Mode shows it.");
            int optionalEntries = (Named(scene, EducationButtonName).Any() ? 1 : 0) +
                                  (Named(scene, ResearchButtonName).Any() ? 1 : 0) +
                                  (Named(scene, FamilyFormationButtonName).Any() ? 1 : 0);
            if (root.childCount != CompleteChildCount + optionalEntries)
                return Invalid(out error,
                    "M6TitleRoot must contain the four base controls, two SC-013 entries and at most one SC-020 and one SC-021 entry.");
            if (standard.parent != root || crisis.parent != root || status.parent != root)
                return Invalid(out error, "SC-013 title entries and status text must be direct title-root children.");
            if (!standard.gameObject.activeSelf || !crisis.gameObject.activeSelf)
                return Invalid(out error, "SC-013 title entries must be active under the inactive title root.");
            if (!ValidButton(standard) || !ValidButton(crisis))
                return Invalid(out error, "SC-013 title buttons are unbound or have invalid labels.");

            TitleScreenController controller = controllers[0];
            if (!Bound(controller, "gameSession", sessions[0]) ||
                !Bound(controller, "titleRoot", root.gameObject) ||
                !Bound(controller, "standardSampleButton", standard.GetComponent<Button>()) ||
                !Bound(controller, "crisisDemoButton", crisis.GetComponent<Button>()))
                return Invalid(out error, "TitleScreenController SC-013 references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform standardRect = standard as RectTransform;
            RectTransform crisisRect = crisis as RectTransform;
            RectTransform statusRect = status as RectTransform;
            if (rootRect == null || standardRect == null || crisisRect == null || statusRect == null ||
                !Contained(standardRect, rootRect) || !Contained(crisisRect, rootRect) ||
                !Contained(statusRect, rootRect) ||
                Overlaps(standardRect, crisisRect, rootRect) ||
                Overlaps(crisisRect, statusRect, rootRect))
                return Invalid(out error, "SC-013 title controls overlap or lie outside the title bounds.");
            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return UpdateResult.InvalidScene;

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            Transform canvas = canvases.Length == 1 ? canvases[0] : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            TitleScreenController controller = controllers.Length == 1 ? controllers[0] : null;
            Transform root = canvas != null ? canvas.Find(RootName) : null;
            if (canvas == null || session == null || controller == null || root == null)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (Named(scene, StandardButtonName).Any() || Named(scene, CrisisButtonName).Any() ||
                HasSerializedReference(controller, "standardSampleButton") ||
                HasSerializedReference(controller, "crisisDemoButton"))
                return UpdateResult.ExistingPartialControl;
            int optionalEntries = (Named(scene, EducationButtonName).Any() ? 1 : 0) +
                                  (Named(scene, ResearchButtonName).Any() ? 1 : 0) +
                                  (Named(scene, FamilyFormationButtonName).Any() ? 1 : 0);
            if (root.childCount != BaseChildCount + optionalEntries)
                return UpdateResult.InvalidHierarchyOrBindings;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                Button standard = CreateButton(scene, root, StandardButtonName, font,
                    "SC-010 标准样例", 0f, -122f, 320f, 48f, 17);
                Button crisis = CreateButton(scene, root, CrisisButtonName, font,
                    "SC-010 危机示范", 0f, -198f, 320f, 48f, 17);

                Transform status = root.Find(StatusTextName);
                if (status == null)
                    throw new InvalidOperationException("Missing M6TitleStatusText.");
                RectTransform statusRect = status as RectTransform;
                if (statusRect == null)
                    throw new InvalidOperationException("M6TitleStatusText has no RectTransform.");
                Undo.RecordObject(statusRect, UndoName);
                bool hasLowerScenarioEntry = Named(scene, EducationButtonName).Any() ||
                                             Named(scene, ResearchButtonName).Any();
                statusRect.anchoredPosition = new Vector2(0f,
                    hasLowerScenarioEntry ? -330f : -260f);
                EditorUtility.SetDirty(statusRect);

                Undo.RecordObject(controller, UndoName);
                controller.BindScenarioButtons(standard, crisis);
                SetSerializedReferences(controller, standard, crisis);
                EditorUtility.SetDirty(controller);

                beforeValidation?.Invoke(root.gameObject);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);

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
                Debug.LogWarning($"SC-013 title scenario updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool HasSerializedReference(UnityEngine.Object target, string field)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && property.objectReferenceValue != null;
        }

        private static void SetSerializedReferences(TitleScreenController controller, Button standard,
            Button crisis)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("standardSampleButton").objectReferenceValue = standard;
            serialized.FindProperty("crisisDemoButton").objectReferenceValue = crisis;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool ValidButton(Transform control)
        {
            Button button = control.GetComponent<Button>();
            Text[] labels = control.GetComponentsInChildren<Text>(true);
            return button != null && button.targetGraphic != null &&
                button.targetGraphic.gameObject == control.gameObject &&
                labels.Length == 1 && labels[0].font != null && !string.IsNullOrWhiteSpace(labels[0].text) &&
                labels[0].transform.parent == control && control.childCount == 1;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font,
            string label, float x, float y, float width, float height, int fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.32f, 0.42f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            SceneManager.MoveGameObjectToScene(labelObject, scene);
            Undo.RegisterCreatedObjectUndo(labelObject, UndoName);
            Undo.SetTransformParent(labelObject.transform, go.transform, UndoName);
            labelObject.transform.localScale = Vector3.one;
            labelObject.layer = parent.gameObject.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(width, height - 2f);
            Text text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return button;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static Transform Require(Scene scene, string name)
        {
            Transform[] matches = Named(scene, name).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static System.Collections.Generic.IEnumerable<Transform> Named(Scene scene, string name) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name);

        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(corner =>
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                return bounds.Contains(new Vector2(point.x, point.y));
            });
        }

        private static bool Overlaps(RectTransform first, RectTransform second, RectTransform coordinateRoot)
        {
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            Vector3 firstMin = coordinateRoot.InverseTransformPoint(firstCorners[0]);
            Vector3 firstMax = coordinateRoot.InverseTransformPoint(firstCorners[2]);
            Vector3 secondMin = coordinateRoot.InverseTransformPoint(secondCorners[0]);
            Vector3 secondMax = coordinateRoot.InverseTransformPoint(secondCorners[2]);
            var firstRect = Rect.MinMaxRect(Mathf.Min(firstMin.x, firstMax.x), Mathf.Min(firstMin.y, firstMax.y),
                Mathf.Max(firstMin.x, firstMax.x), Mathf.Max(firstMin.y, firstMax.y));
            var secondRect = Rect.MinMaxRect(Mathf.Min(secondMin.x, secondMax.x), Mathf.Min(secondMin.y, secondMax.y),
                Mathf.Max(secondMin.x, secondMax.x), Mathf.Max(secondMin.y, secondMax.y));
            return firstRect.Overlaps(secondRect, true);
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
