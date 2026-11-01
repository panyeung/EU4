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
    /// Adds the single-slot save/load control to the map screen in one Undo group.
    /// Validates every reference and the layout against existing map controls before saving.
    /// </summary>
    public static class AddM5SaveLoadPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "M5SaveLoadRoot";
        public const string SaveButtonName = "M5SaveButton";
        public const string LoadButtonName = "M5LoadButton";
        public const string StatusTextName = "M5SaveStatusText";

        private const string MapRootName = "M2WorldMapRoot";
        private const string TownRootName = "TownMenuRoot";
        private const string MenuPath = "Tools/Sengoku Chronicle/Add M5 Save Load Panel";
        private const string UndoName = "Add M5 Save Load Panel";
        private const float MapWidth = 1216f;
        private const float MapHeight = 268f;

        private static readonly string[] MapControlNames =
        {
            "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
            "DestinationNameText", "TravelTimeText", "ArrivalDateText",
            "DestButton_0", "DestButton_1", "DestButton_2", "ConfirmButton", "CancelButton"
        };

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, InvalidHierarchyOrBindings, ExistingPanel,
            InsufficientSpace, ValidationFailed, SaveFailed, Failed
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
                Debug.Log("M5 save/load panel validated; FoundationScene saved.");
            else
                Debug.LogError($"M5 save/load updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>
        /// Batch entry point: opens FoundationScene, runs the transactional updater,
        /// saves only after validation, and writes the scene validation report.
        /// </summary>
        public static void RunBatchIntegration()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);

            FoundationSceneValidator.ValidateActiveScene();
            if (result != UpdateResult.Success)
                throw new InvalidOperationException($"M5 save/load integration failed: {result}.");

            Debug.Log("M5 save/load integration completed and FoundationScene was saved.");
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

            Transform map = Named(scene, MapRootName).SingleOrDefault();
            if (map == null)
                return Invalid(out error, "Missing M2WorldMapRoot.");
            Transform town = map.Find(TownRootName);
            if (town == null)
                return Invalid(out error, "Missing TownMenuRoot.");

            Transform[] roots = Named(scene, RootName);
            SaveLoadPanel[] panels = Components<SaveLoadPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1)
                return Invalid(out error, "Expected exactly one M5 save/load root and panel.");

            Transform root = roots[0];
            SaveLoadPanel panel = panels[0];
            if (root.parent != map || panel.transform != root || root.childCount != 3)
                return Invalid(out error, "Expected the fixed save/load root with exactly three controls.");
            if (root.GetSiblingIndex() >= town.GetSiblingIndex())
                return Invalid(out error, "Save/load root must render beneath the town menu.");

            Transform save = root.Find(SaveButtonName);
            Transform load = root.Find(LoadButtonName);
            Transform status = root.Find(StatusTextName);
            if (save == null || load == null || status == null)
                return Invalid(out error, "Missing a save/load control.");

            if (!ValidButton(save) || !ValidButton(load))
                return Invalid(out error, "Save/load buttons are unbound or have invalid labels.");
            Text statusText = status.GetComponent<Text>();
            if (statusText == null || statusText.font == null)
                return Invalid(out error, "Save/load status text is invalid.");

            WorldMapController controller = map.GetComponent<WorldMapController>();
            GameSession session = Components<GameSession>(scene).SingleOrDefault();
            if (!Bound(panel, "gameSession", session) ||
                !Bound(panel, "worldMapController", controller) ||
                !Bound(panel, "saveButton", save.GetComponent<Button>()) ||
                !Bound(panel, "loadButton", load.GetComponent<Button>()) ||
                !Bound(panel, "statusText", statusText))
                return Invalid(out error, "Save/load panel references are not bound exactly once.");

            RectTransform mapRect = map as RectTransform;
            var controls = new[] { save as RectTransform, load as RectTransform, status as RectTransform };
            if (mapRect == null || controls.Any(rect => rect == null || !Contained(rect, mapRect)))
                return Invalid(out error, "Save/load controls are outside the map bounds.");

            for (int i = 0; i < controls.Length; i++)
                for (int j = i + 1; j < controls.Length; j++)
                    if (Overlaps(controls[i], controls[j], mapRect))
                        return Invalid(out error, "Save/load controls overlap each other.");

            foreach (string name in MapControlNames)
            {
                foreach (Transform sibling in map.Cast<Transform>().Where(t => t.name == name))
                {
                    var siblingRect = sibling as RectTransform;
                    if (siblingRect != null && controls.Any(control => Overlaps(control, siblingRect, mapRect)))
                        return Invalid(out error, $"Save/load control overlaps map control {name}.");
                }
            }

            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded) return UpdateResult.InvalidScene;

            Transform map = Named(scene, MapRootName).SingleOrDefault();
            if (map == null || map.Find(TownRootName) == null ||
                Components<WorldMapController>(scene).Length != 1 ||
                Components<GameSession>(scene).Length != 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (Named(scene, RootName).Length != 0 ||
                Components<SaveLoadPanel>(scene).Length != 0 ||
                Named(scene, SaveButtonName).Length != 0 ||
                Named(scene, LoadButtonName).Length != 0 ||
                Named(scene, StatusTextName).Length != 0)
                return UpdateResult.ExistingPanel;

            RectTransform mapRect = map as RectTransform;
            if (mapRect == null || mapRect.rect.width < MapWidth - 1f || mapRect.rect.height < MapHeight - 1f)
                return UpdateResult.InsufficientSpace;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject root = Create(scene, map, RootName, typeof(SaveLoadPanel));
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = Vector2.zero;
                root.transform.SetSiblingIndex(map.Find(TownRootName).GetSiblingIndex());

                Button saveButton = CreateButton(scene, root.transform, SaveButtonName, font, "存档",
                    515f, 118f, 150f, 30f, 14);
                Button loadButton = CreateButton(scene, root.transform, LoadButtonName, font, "读档",
                    515f, 82f, 150f, 30f, 14);
                Text statusText = CreateText(scene, root.transform, StatusTextName, font, "未存档",
                    515f, 50f, 150f, 22f, 11);
                statusText.alignment = TextAnchor.MiddleCenter;

                SaveLoadPanel panel = root.GetComponent<SaveLoadPanel>();
                Undo.RecordObject(panel, UndoName);
                panel.Bind(
                    Components<GameSession>(scene).Single(),
                    null,
                    saveButton,
                    loadButton,
                    statusText,
                    map.GetComponent<WorldMapController>());
                EditorUtility.SetDirty(panel);

                beforeValidation?.Invoke(root);
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
                Debug.LogWarning($"M5 updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
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

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font, string value,
            float x, float y, float width, float height, int fontSize)
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = value;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font, string label,
            float x, float y, float width, float height, int fontSize)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.32f, 0.42f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Text buttonLabel = CreateText(scene, rect, "Label", font, label, 0f, 0f, width, height - 2f, fontSize);
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            return button;
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
