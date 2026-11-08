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
    /// Adds or migrates the M7 merit/rank line into a dedicated slot inside the
    /// SC-010 SuccessionHud in one Undo group. Validates references, hierarchy,
    /// readability, containment, and separation from HUD and feature controls
    /// before saving.
    /// </summary>
    public static class AddM7RankPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RankTextName = "M7RankText";
        public const string PanelName = "RankPanel";
        public const string RootName = "SuccessionRoot";
        public const string HudName = "SuccessionHud";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string TownRootName = "TownMenuRoot";
        private const string MenuPath = "Tools/Sengoku Chronicle/Add M7 Rank Panel";
        private const string UndoName = "Add M7 Rank Panel";
        private const string DefaultRankText = "功名：0 · 足轻";
        private const float RankWidth = 390f;
        private const float RankHeight = 20f;
        private const float RankOffsetY = -54f;
        private const int RankFontSize = 15;
        private const int MinRankFontSize = 14;
        private const float MinRankWidth = 360f;
        private const float MinRankHeight = 18f;

        private static readonly string[] NearbyControlNames =
        {
            "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
            "DestinationNameText", "TravelTimeText", "ArrivalDateText",
            "DestButton_0", "DestButton_1", "DestButton_2", "ConfirmButton", "CancelButton",
            "M5SaveButton", "M5LoadButton", "M5SaveStatusText", "M6DemoSummaryText",
            "SuccessionHeadText", "SuccessionStatusText", "SuccessionRetireButton",
            "SuccessionCrisisDemoButton", "SuccessionFeedbackText",
            "FamilyChronicleOpenButton", "FamilyRosterOpenButton", "IndustryOpenButton",
            "SC020EducationOpenButton", "SC021ResearchOpenButton",
            "SC022FamilyFormationOpenButton", "FamilyDoctrineOpenButton",
            "M02DailyActionOpenButton", "M03NpcAiInspectorOpenButton"
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
                Debug.Log("M7 rank panel validated; FoundationScene saved.");
            else
                Debug.LogError($"M7 rank updater refused or rolled back: {result}.");
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
                throw new InvalidOperationException($"M7 rank integration failed: {result}.");

            Debug.Log("M7 rank integration completed and FoundationScene was saved.");
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
            Transform[] huds = Named(scene, HudName);
            if (roots.Length != 1 || huds.Length != 1)
                return Invalid(out error, "Expected exactly one SuccessionRoot and SuccessionHud.");
            Transform root = roots[0];
            Transform hud = huds[0];
            if (root.parent != canvas || hud.parent != root || !hud.gameObject.activeSelf)
                return Invalid(out error, "SuccessionHud must be an active direct child of SuccessionRoot.");

            Transform[] texts = Named(scene, RankTextName);
            RankPanel[] panels = Components<RankPanel>(scene);
            if (texts.Length != 1 || panels.Length != 1)
                return Invalid(out error, "Expected exactly one rank text and one RankPanel.");

            Transform rankText = texts[0];
            RankPanel panel = panels[0];

            if (rankText.parent != hud)
                return Invalid(out error, "Rank text must live in the SuccessionHud slot.");
            if (!rankText.gameObject.activeSelf)
                return Invalid(out error, "Rank text must be active.");
            Text text = rankText.GetComponent<Text>();
            if (text == null || text.font == null)
                return Invalid(out error, "Rank text is invalid.");
            if (text.fontSize < MinRankFontSize)
                return Invalid(out error, $"Rank text font size must be at least {MinRankFontSize}.");
            if (text.alignment != TextAnchor.MiddleLeft)
                return Invalid(out error, "Rank text must be left-aligned.");

            var hudRect = hud as RectTransform;
            var textRect = rankText as RectTransform;
            if (hudRect == null || textRect == null ||
                textRect.rect.width < MinRankWidth - 0.5f ||
                textRect.rect.height < MinRankHeight - 0.5f ||
                !Contained(textRect, hudRect))
                return Invalid(out error, "Rank text is too narrow or outside the SuccessionHud bounds.");

            if (panel.transform != session.transform)
                return Invalid(out error, "RankPanel must live on the GameSession object.");
            if (!Bound(panel, "gameSession", session) || !Bound(panel, "rankText", text))
                return Invalid(out error, "Rank panel references are not bound exactly once.");

            var canvasRect = canvas as RectTransform;
            if (canvasRect == null)
                return Invalid(out error, "Canvas has no RectTransform.");

            foreach (string name in NearbyControlNames)
            {
                foreach (Transform sibling in scene.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                             .Where(t => t.name == name && t != rankText))
                {
                    var siblingRect = sibling as RectTransform;
                    if (siblingRect != null && Overlaps(textRect, siblingRect, canvasRect))
                        return Invalid(out error, $"Rank text overlaps {name}.");
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
            Transform hud = map != null ? FindHud(scene) : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            WorldMapController mapController = mapControllers.Length == 1 ? mapControllers[0] : null;
            if (canvas == null || map == null || map.Find(TownRootName) == null ||
                hud == null || session == null || mapController == null)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            Transform[] texts = Named(scene, RankTextName);
            RankPanel[] panels = Components<RankPanel>(scene);
            bool migrate = texts.Length != 0 || panels.Length != 0;
            Text existingText = null;
            RankPanel existingPanel = null;
            if (migrate)
            {
                // The only supported repair is the known M7 pair: one text and one
                // panel, already bound to this session, with a layout that no longer
                // passes validation. Anything else is user/partial data we refuse.
                if (texts.Length != 1 || panels.Length != 1)
                    return UpdateResult.ExistingPanel;
                existingText = texts[0].GetComponent<Text>();
                existingPanel = panels[0];
                if (existingText == null || existingPanel.transform != session.transform ||
                    !Bound(existingPanel, "gameSession", session) ||
                    !Bound(existingPanel, "rankText", existingText))
                    return UpdateResult.ExistingPanel;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                Text rankText;
                if (migrate)
                {
                    rankText = existingText;
                    Undo.SetTransformParent(rankText.transform, hud, UndoName);
                    ConfigureRankText(rankText);
                    rankText.transform.SetAsLastSibling();
                }
                else
                {
                    rankText = CreateText(scene, hud, RankTextName, font, DefaultRankText,
                        0f, RankOffsetY, RankWidth, RankHeight, RankFontSize);
                    ConfigureRankText(rankText);

                    RankPanel panel = Undo.AddComponent<RankPanel>(session.gameObject);
                    Undo.RecordObject(panel, UndoName);
                    panel.Bind(session, rankText);
                    EditorUtility.SetDirty(panel);
                }

                beforeValidation?.Invoke(rankText.gameObject);
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
                Debug.LogWarning($"M7 updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static Transform FindHud(Scene scene)
        {
            Transform[] roots = Named(scene, RootName);
            Transform[] huds = Named(scene, HudName);
            return roots.Length == 1 && huds.Length == 1 && huds[0].parent == roots[0]
                ? huds[0] : null;
        }

        private static void ConfigureRankText(Text text)
        {
            var rect = (RectTransform)text.transform;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, RankOffsetY);
            rect.sizeDelta = new Vector2(RankWidth, RankHeight);

            Undo.RecordObject(text, UndoName);
            text.fontSize = RankFontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
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
