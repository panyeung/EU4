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
    /// Adds the Phase 1 title screen and demo summary to FoundationScene in one Undo group.
    /// Validates every reference, the sibling order, and the demo-text layout before saving.
    /// </summary>
    public static class AddM6TitleScreen
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string CanvasName = "Canvas";
        public const string RootName = "M6TitleRoot";
        public const string LabelName = "M6TitleLabel";
        public const string NewGameButtonName = "M6NewGameButton";
        public const string ContinueButtonName = "M6ContinueButton";
        public const string StatusTextName = "M6TitleStatusText";
        public const string DemoSummaryTextName = "M6DemoSummaryText";

        private const string MapRootName = "M2WorldMapRoot";
        private const string TownRootName = "TownMenuRoot";
        private const string MenuPath = "Tools/Sengoku Chronicle/Add M6 Title Screen";
        private const string UndoName = "Add M6 Title Screen";
        private const float MapWidth = 1216f;
        private const float MapHeight = 268f;

        // Overlap guard: the demo summary must not cover these M5 controls when they exist.
        private static readonly string[] NearbyControlNames =
        {
            "M5SaveButton", "M5LoadButton", "M5SaveStatusText", "ConfirmButton", "CancelButton"
        };

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, InvalidHierarchyOrBindings, ExistingPanel,
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
                Debug.Log("M6 title screen validated; FoundationScene saved.");
            else
                Debug.LogError($"M6 title updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>
        /// Batch entry point: opens FoundationScene, runs the transactional updater,
        /// validates, and saves only after validation; then writes the scene report.
        /// </summary>
        public static void RunBatchIntegration()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);

            FoundationSceneValidator.ValidateActiveScene();
            if (result != UpdateResult.Success)
                throw new InvalidOperationException($"M6 title integration failed: {result}.");

            Debug.Log("M6 title integration completed and FoundationScene was saved.");
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
            Transform canvas = canvasAll[0];

            Transform map = canvas.Find(MapRootName);
            Transform town = map != null ? map.Find(TownRootName) : null;
            if (map == null || town == null)
                return Invalid(out error, "Missing M2WorldMapRoot or TownMenuRoot.");
            if (Components<WorldMapController>(scene).Length != 1)
                return Invalid(out error, "Expected exactly one WorldMapController.");

            GameSession[] sessions = Components<GameSession>(scene);
            if (sessions.Length != 1)
                return Invalid(out error, "Expected exactly one GameSession.");
            GameSession session = sessions[0];

            Transform[] roots = Named(scene, RootName);
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            DemoSummaryPanel[] panels = Components<DemoSummaryPanel>(scene);
            Transform[] summaryTexts = Named(scene, DemoSummaryTextName);
            if (roots.Length != 1 || controllers.Length != 1 || panels.Length != 1 || summaryTexts.Length != 1)
                return Invalid(out error, "Expected exactly one M6 title root, controller, panel, and summary text.");

            Transform root = roots[0];
            TitleScreenController controller = controllers[0];
            DemoSummaryPanel panel = panels[0];
            Transform summary = summaryTexts[0];

            if (root.parent != canvas)
                return Invalid(out error, "M6 title root must be a direct child of Canvas.");
            if (root.GetSiblingIndex() <= map.GetSiblingIndex())
                return Invalid(out error, "M6 title root must render above the map (last sibling after it).");
            bool hasStandardSample = root.Find("SC013StandardSampleButton") != null;
            bool hasCrisisDemo = root.Find("SC013CrisisDemoButton") != null;
            if (hasStandardSample != hasCrisisDemo)
                return Invalid(out error, "SC-013 title scenario buttons must be added as a pair.");
            if (root.childCount != 4 && root.childCount != 6)
                return Invalid(out error, "M6 title root must have the four base controls or six with SC-013 entries.");
            if (root.childCount == 6 && (!hasStandardSample || !hasCrisisDemo))
                return Invalid(out error, "M6 title root with six controls must include both SC-013 entries.");
            if (root.gameObject.activeSelf)
                return Invalid(out error, "M6 title root must be saved inactive; Play Mode shows it.");

            Image background = root.GetComponent<Image>();
            if (background == null || !background.raycastTarget || background.color.a < 1f)
                return Invalid(out error, "M6 title background must be an opaque raycast-blocking full-screen Image.");
            RectTransform rootRect = root as RectTransform;
            if (rootRect == null || rootRect.anchorMin != Vector2.zero || rootRect.anchorMax != Vector2.one ||
                rootRect.offsetMin != Vector2.zero || rootRect.offsetMax != Vector2.zero)
                return Invalid(out error, "M6 title root must stretch to the full Canvas.");

            Transform label = root.Find(LabelName);
            Transform newGame = root.Find(NewGameButtonName);
            Transform continueButton = root.Find(ContinueButtonName);
            Transform status = root.Find(StatusTextName);
            if (label == null || newGame == null || continueButton == null || status == null)
                return Invalid(out error, "Missing an M6 title control.");
            if (label.GetComponent<Text>() == null || label.GetComponent<Text>().font == null)
                return Invalid(out error, "M6 title label is invalid.");
            if (!ValidButton(newGame) || !ValidButton(continueButton))
                return Invalid(out error, "M6 title buttons are unbound or have invalid labels.");
            Text statusText = status.GetComponent<Text>();
            if (statusText == null || statusText.font == null)
                return Invalid(out error, "M6 title status text is invalid.");

            if (controller.transform != session.transform)
                return Invalid(out error, "TitleScreenController must live on the GameSession object.");
            WorldMapController mapController = map.GetComponent<WorldMapController>();
            if (!Bound(controller, "gameSession", session) ||
                !Bound(controller, "worldMapController", mapController) ||
                !Bound(controller, "titleRoot", root.gameObject) ||
                !Bound(controller, "newGameButton", newGame.GetComponent<Button>()) ||
                !Bound(controller, "continueButton", continueButton.GetComponent<Button>()) ||
                !Bound(controller, "statusText", statusText))
                return Invalid(out error, "Title screen references are not bound exactly once.");

            if (panel.transform != session.transform)
                return Invalid(out error, "DemoSummaryPanel must live on the GameSession object.");
            Text summaryText = summary.GetComponent<Text>();
            if (summaryText == null || summaryText.font == null)
                return Invalid(out error, "M6 demo summary text is invalid.");
            if (!Bound(panel, "gameSession", session) || !Bound(panel, "summaryText", summaryText))
                return Invalid(out error, "Demo summary references are not bound exactly once.");

            if (summary.parent != map)
                return Invalid(out error, "Demo summary text must live under the map root.");
            if (summary.GetSiblingIndex() >= town.GetSiblingIndex())
                return Invalid(out error, "Demo summary text must render beneath the town menu.");
            if (summary.gameObject.activeSelf)
                return Invalid(out error, "Demo summary text must be saved inactive until the demo completes.");

            RectTransform mapRect = map as RectTransform;
            var summaryRect = summary as RectTransform;
            if (mapRect == null || summaryRect == null || mapRect.rect.width < MapWidth - 1f ||
                mapRect.rect.height < MapHeight - 1f || !Contained(summaryRect, mapRect))
                return Invalid(out error, "Demo summary text is outside the map bounds.");

            foreach (string name in NearbyControlNames)
            {
                foreach (Transform sibling in scene.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                             .Where(t => t.name == name && t != summary))
                {
                    var siblingRect = sibling as RectTransform;
                    if (siblingRect != null && Overlaps(summaryRect, siblingRect, mapRect))
                        return Invalid(out error, $"Demo summary text overlaps {name}.");
                }
            }

            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded) return UpdateResult.InvalidScene;

            Transform[] canvasAll = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            WorldMapController[] mapControllers = Components<WorldMapController>(scene);
            Transform canvas = canvasAll.Length == 1 ? canvasAll[0] : null;
            Transform map = canvas != null ? canvas.Find(MapRootName) : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            WorldMapController mapController = mapControllers.Length == 1 ? mapControllers[0] : null;
            if (canvas == null || map == null || map.Find(TownRootName) == null ||
                session == null || mapController == null)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (Named(scene, RootName).Length != 0 ||
                Named(scene, LabelName).Length != 0 ||
                Named(scene, NewGameButtonName).Length != 0 ||
                Named(scene, ContinueButtonName).Length != 0 ||
                Named(scene, StatusTextName).Length != 0 ||
                Named(scene, DemoSummaryTextName).Length != 0 ||
                Components<TitleScreenController>(scene).Length != 0 ||
                Components<DemoSummaryPanel>(scene).Length != 0)
                return UpdateResult.ExistingPanel;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject root = Create(scene, canvas, RootName, typeof(Image));
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                Image background = root.GetComponent<Image>();
                background.color = new Color(0.07f, 0.08f, 0.11f, 1f);
                background.raycastTarget = true;

                Text title = CreateText(scene, root.transform, LabelName, font, "战国立志传 · Phase 1 演示",
                    0f, 180f, 900f, 70f, 44);
                title.alignment = TextAnchor.MiddleCenter;
                Button newGame = CreateButton(scene, root.transform, NewGameButtonName, font, "新游戏",
                    0f, 30f, 260f, 56f, 20);
                Button continueBtn = CreateButton(scene, root.transform, ContinueButtonName, font, "继续",
                    0f, -46f, 260f, 56f, 20);
                Text statusText = CreateText(scene, root.transform, StatusTextName, font, "",
                    0f, -120f, 700f, 30f, 14);
                statusText.alignment = TextAnchor.MiddleCenter;

                Transform town = map.Find(TownRootName);
                Text summary = CreateText(scene, map, DemoSummaryTextName, font, "", 460f, -110f, 260f, 44f, 12);
                summary.alignment = TextAnchor.MiddleCenter;
                summary.gameObject.SetActive(false);
                summary.transform.SetSiblingIndex(town.GetSiblingIndex());

                TitleScreenController controller = Undo.AddComponent<TitleScreenController>(session.gameObject);
                Undo.RecordObject(controller, UndoName);
                controller.Bind(session, null, mapController, root, newGame, continueBtn, statusText);
                EditorUtility.SetDirty(controller);

                DemoSummaryPanel panel = Undo.AddComponent<DemoSummaryPanel>(session.gameObject);
                Undo.RecordObject(panel, UndoName);
                panel.Bind(session, summary);
                EditorUtility.SetDirty(panel);

                root.SetActive(false);

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
                Debug.LogWarning($"M6 updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
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
