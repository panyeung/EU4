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
    /// Transactional FoundationScene updater for the SC-023 title entry. SC-023 reuses the
    /// existing <see cref="CharacterEducationPanel"/> and its fixed
    /// <c>Canvas/SC020EducationRoot</c>; this generator only adds the title entry, keeps
    /// the title status row clear, and binds the new controller field. One Undo group,
    /// validate before save, Undo rollback on exception, and no scene reload.
    /// </summary>
    public static class AddSC023Seclusion
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string TitleButtonName = "SC023SeclusionDemoButton";
        /// <summary>SC-023 intentionally reuses the accepted SC-020 education root.</summary>
        public const string RootName = "SC020EducationRoot";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-023 Seclusion";

        private const string CanvasName = "Canvas";
        private const string TitleRootName = "M6TitleRoot";
        private const string TitleStatusName = "M6TitleStatusText";
        private const string UndoName = "Add SC-023 Seclusion";
        private const string NewGameButtonName = "M6NewGameButton";
        private const string ContinueButtonName = "M6ContinueButton";
        private const string StandardButtonName = "SC013StandardSampleButton";
        private const string CrisisButtonName = "SC013CrisisDemoButton";
        private const string EducationTitleButtonName = "SC020EducationDemoButton";
        private const string ResearchTitleButtonName = "SC021ResearchDemoButton";
        private const string FamilyFormationTitleButtonName = "SC022FamilyFormationDemoButton";

        // M6TitleRoot stretches to the 1280x720 canvas, so its local rect is y -360..360.
        // The SC-020/021/022 row occupies y -298..-250 and the shared status row stays at
        // its canonical (0,-330), 700x30 position that AddSC020CharacterEducation and
        // AddSC013TitleScenarioButtons already use. There is no room for a centered fourth
        // row below the scenario row: anything under -330 leaves the root, and a -332 row
        // collides with the status row. SC-023 therefore shares the status band on the
        // right (x 370..620, y -345..-305), clear of the status rect (x -350..350) and of
        // every existing title entry, which keeps the recovery paths geometry-compatible.
        private static readonly Vector2 TitleButtonPosition = new Vector2(495f, -325f);
        private static readonly Vector2 TitleButtonSize = new Vector2(250f, 40f);
        private static readonly Vector2 StatusPosition = new Vector2(0f, -330f);
        private static readonly Vector2 StatusSize = new Vector2(700f, 30f);

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, ExistingPartialControl, ValidationFailed, SaveFailed, Failed
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
                Debug.Log("SC-023 seclusion title entry validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-023 seclusion updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty) =>
            CheckScenePreflight(path, dirty, false);

        public static UpdateResult CheckScenePreflight(string path, bool dirty, bool editorPlaying)
        {
            if (editorPlaying) return UpdateResult.EditorPlaying;
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam. It never saves, opens or reloads the supplied scene.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault-injection seam inside the same Undo transaction as construction.</summary>
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
            Transform[] titleRoots = Named(scene, TitleRootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || controllers.Length != 1 ||
                titleRoots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, TitleScreenController and M6TitleRoot.");

            Transform canvas = canvases[0];
            Transform titleRoot = titleRoots[0];
            if (titleRoot.parent != canvas)
                return Invalid(out error, "M6TitleRoot must be a direct child of Canvas.");
            if (titleRoot.gameObject.activeSelf)
                return Invalid(out error, "M6TitleRoot must be saved inactive; Play Mode shows it.");

            Transform[] buttons = Named(scene, TitleButtonName).ToArray();
            if (buttons.Length != 1)
                return Invalid(out error, "Expected exactly one SC-023 title entry.");
            Transform selector = titleRoot.Find(AddM6TitleScreen.ScenarioSelectRootName);
            Transform expectedParent = selector != null ? selector : titleRoot;
            Transform button = buttons[0];
            if (button.parent != expectedParent || !button.gameObject.activeSelf)
                return Invalid(out error,
                    "The SC-023 title entry must be active on the title scenario page.");
            if (!HasButtonVisual(button.GetComponent<Button>()) ||
                !HasRaycastTarget(button.GetComponent<Button>()))
                return Invalid(out error, "The SC-023 title entry button is invalid.");
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null || !label.text.Contains("隐居") || !label.text.Contains("后见人"))
                return Invalid(out error, "The SC-023 title entry label must contain 隐居 and 后见人.");
            if (!Bound(controllers[0], "seclusionDemoButton", button.GetComponent<Button>()) ||
                !Bound(controllers[0], "gameSession", sessions[0]))
                return Invalid(out error, "TitleScreenController SC-023 references are not bound.");

            RectTransform rootRect = titleRoot as RectTransform;
            RectTransform buttonRect = button as RectTransform;
            if (selector != null)
            {
                RectTransform selectorRect = selector as RectTransform;
                if (selectorRect == null || buttonRect == null || !Contained(buttonRect, selectorRect))
                    return Invalid(out error,
                        "The SC-023 title entry lies outside the scenario selector page.");
                return ValidateEducationDependency(scene, canvas, out error);
            }

            RectTransform statusRect = Require(scene, TitleStatusName) as RectTransform;
            if (rootRect == null || buttonRect == null || statusRect == null)
                return Invalid(out error,
                    "The SC-023 title entry or status line has no valid RectTransform.");
            if (!Contained(buttonRect, rootRect))
                return Invalid(out error, "The SC-023 title entry lies outside M6TitleRoot.");
            if (!Contained(statusRect, rootRect))
                return Invalid(out error, "The M6 title status line lies outside M6TitleRoot.");
            if (Overlaps(buttonRect, statusRect, rootRect) ||
                OverlapsNamed(scene, buttonRect, NewGameButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ContinueButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, StandardButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, CrisisButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, EducationTitleButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ResearchTitleButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, FamilyFormationTitleButtonName, rootRect) ||
                Overlaps(statusRect, Require(scene, NewGameButtonName) as RectTransform, rootRect) ||
                Overlaps(statusRect, Require(scene, ContinueButtonName) as RectTransform, rootRect) ||
                OverlapsNamed(scene, statusRect, EducationTitleButtonName, rootRect) ||
                OverlapsNamed(scene, statusRect, ResearchTitleButtonName, rootRect) ||
                OverlapsNamed(scene, statusRect, FamilyFormationTitleButtonName, rootRect))
                return Invalid(out error, "The SC-023 title entry or status line overlaps another title control.");

            return ValidateEducationDependency(scene, canvas, out error);
        }

        // SC-023 deliberately reuses the accepted SC-020 education panel instead of
        // creating a second panel or scene root.
        private static bool ValidateEducationDependency(Scene scene, Transform canvas, out string error)
        {
            Transform[] educationRoots = Named(scene, RootName).ToArray();
            CharacterEducationPanel[] panels = Components<CharacterEducationPanel>(scene);
            if (educationRoots.Length != 1 || panels.Length != 1 ||
                educationRoots[0].parent != canvas || !educationRoots[0].gameObject.activeSelf ||
                panels[0].transform != educationRoots[0] || !panels[0].IsBound)
                return Invalid(out error,
                    "SC-023 requires the existing SC020EducationRoot and its bound CharacterEducationPanel.");

            error = null;
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
            Transform titleRoot = canvas != null ? canvas.Find(TitleRootName) : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            TitleScreenController controller = controllers.Length == 1 ? controllers[0] : null;
            if (canvas == null || titleRoot == null || session == null || controller == null)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (Named(scene, TitleButtonName).Any() ||
                HasSerializedReference(controller, "seclusionDemoButton"))
                return UpdateResult.ExistingPartialControl;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                Button button = CreateTitleButton(scene,
                    titleRoot.Find(AddM6TitleScreen.ScenarioSelectRootName) ?? titleRoot,
                    TitleButtonName, font, "SC-023 隐居与后见人示范", TitleButtonPosition, TitleButtonSize);
                Transform status = titleRoot.Find(TitleStatusName);
                if (status != null)
                    SetCenteredRect(status, StatusPosition, StatusSize);

                Undo.RecordObject(controller, UndoName);
                controller.BindSeclusionScenarioButton(button);
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("seclusionDemoButton").objectReferenceValue = button;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);

                beforeValidation?.Invoke(button.gameObject);
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
                Debug.LogWarning($"SC-023 seclusion updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static Button CreateTitleButton(Scene scene, Transform parent, string name, Font font,
            string label, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            SetCenteredRect(go.transform, position, size);
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
            labelRect.sizeDelta = new Vector2(size.x, size.y - 2f);
            Text text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 17;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return button;
        }

        private static void SetCenteredRect(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect))
                return;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static bool HasButtonVisual(Button button) =>
            button != null && button.targetGraphic != null &&
            button.GetComponentInChildren<Text>(true) != null;

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

        private static bool HasSerializedReference(UnityEngine.Object target, string field)
        {
            if (target == null)
                return false;
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && property.objectReferenceValue != null;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static bool OverlapsNamed(Scene scene, RectTransform first, string secondName,
            RectTransform coordinateRoot)
        {
            Transform second = Require(scene, secondName);
            return second is RectTransform secondRect && first != null &&
                   Overlaps(first, secondRect, coordinateRoot);
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
            if (child == null || parent == null)
                return false;
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(corner =>
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                return bounds.Contains(new Vector2(point.x, point.y));
            });
        }

        private static bool Overlaps(RectTransform first, RectTransform second,
            RectTransform coordinateRoot)
        {
            if (first == null || second == null || coordinateRoot == null)
                return false;
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            Vector3 firstMin = coordinateRoot.InverseTransformPoint(firstCorners[0]);
            Vector3 firstMax = coordinateRoot.InverseTransformPoint(firstCorners[2]);
            Vector3 secondMin = coordinateRoot.InverseTransformPoint(secondCorners[0]);
            Vector3 secondMax = coordinateRoot.InverseTransformPoint(secondCorners[2]);
            var firstRect = Rect.MinMaxRect(Mathf.Min(firstMin.x, firstMax.x),
                Mathf.Min(firstMin.y, firstMax.y), Mathf.Max(firstMin.x, firstMax.x),
                Mathf.Max(firstMin.y, firstMax.y));
            var secondRect = Rect.MinMaxRect(Mathf.Min(secondMin.x, secondMax.x),
                Mathf.Min(secondMin.y, secondMax.y), Mathf.Max(secondMin.x, secondMax.x),
                Mathf.Max(secondMin.y, secondMax.y));
            return firstRect.Overlaps(secondRect, true);
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
