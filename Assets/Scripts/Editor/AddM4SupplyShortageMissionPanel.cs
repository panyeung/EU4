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
    public static class AddM4SupplyShortageMissionPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "M4SupplyShortageMissionRoot";
        private const string MenuPath = "Tools/Sengoku Chronicle/Add M4a Supply Shortage Mission Panel";
        private const string UndoName = "Add M4a Supply Shortage Mission Panel";
        private static readonly string[] TextNames = { "StageText", "ObjectiveText", "MeritText", "FeedbackText" };
        private static readonly string[] TextFields = { "stageText", "objectiveText", "meritText", "feedbackText" };
        private static readonly string[] ButtonNames = { "AcceptButton", "InvestigateButton", "NegotiateButton", "ReportButton" };
        private static readonly string[] ButtonFields = { "acceptButton", "investigateButton", "negotiateButton", "reportButton" };

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene, MissingOrDuplicateObjects,
            InvalidHierarchyOrBindings, ExistingMissionPanel, InsufficientSpace,
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
                Debug.Log("M4a mission panel validated and FoundationScene saved.");
            else
                Debug.LogError($"M4a mission panel updater refused or rolled back: {result}.");
        }

        // Shared menu guard, exposed so tests need not save or replace FoundationScene.
        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam for isolated scenes. Never saves; menu also checks path/clean state.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault injection runs inside the Undo transaction, immediately before validation.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) => Execute(scene, false, beforeValidation);

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            UpdateResult preflight = Resolve(scene, out Transform town, out WorldMapController controller, out GameSession session);
            if (preflight != UpdateResult.Success) return preflight;
            if (Components<SupplyShortageMissionPanel>(scene).Length != 0 || Named(scene, RootName).Length != 0)
                return UpdateResult.ExistingMissionPanel;
            SerializedProperty existingPanel = new SerializedObject(controller).FindProperty("supplyShortageMissionPanel");
            if (existingPanel == null) return UpdateResult.InvalidHierarchyOrBindings;
            if (existingPanel.objectReferenceValue != null || !existingPanel.objectReferenceEntityIdValue.Equals(EntityId.None))
                return UpdateResult.ExistingMissionPanel;
            // Leave the existing central town controls and NPC navigation untouched.
            RectTransform townRect = town as RectTransform;
            if (townRect.rect.width < 1200f || townRect.rect.height < 260f)
                return UpdateResult.InsufficientSpace;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                GameObject root = Create(scene, town, RootName, typeof(Image));
                root.SetActive(false);
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(12f, 0f);
                rect.sizeDelta = new Vector2(280f, 240f);
                root.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.17f, 1f);
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");
                Text stage = CreateText(scene, rect, TextNames[0], font, "", 94f, 28f);
                Text objective = CreateText(scene, rect, TextNames[1], font, "", 46f, 62f);
                Text merit = CreateText(scene, rect, TextNames[2], font, "", 0f, 26f);
                Text feedback = CreateText(scene, rect, TextNames[3], font, "", -43f, 56f);
                string[] labels = { "领取任务", "调查补给", "与黑田伊织交涉", "报告并领取奖励" };
                var buttons = new Button[4];
                for (int i = 0; i < buttons.Length; i++)
                {
                    GameObject buttonObject = Create(scene, rect, ButtonNames[i], typeof(Image), typeof(Button));
                    RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                    Center(buttonRect, -94f, 32f);
                    Image image = buttonObject.GetComponent<Image>();
                    image.color = new Color(0.22f, 0.32f, 0.42f, 1f);
                    buttons[i] = buttonObject.GetComponent<Button>();
                    buttons[i].targetGraphic = image;
                    Text label = CreateText(scene, buttonRect, "Label", font, labels[i], 0f, 32f);
                    label.alignment = TextAnchor.MiddleCenter;
                }
                SupplyShortageMissionPanel panel = Undo.AddComponent<SupplyShortageMissionPanel>(root);
                panel.BindReferences(root, stage, objective, merit, feedback, buttons[0], buttons[1], buttons[2], buttons[3]);
                // The controller binding also binds the panel to its already validated session.
                Undo.RecordObject(controller, UndoName);
                controller.BindSupplyShortageMissionPanel(panel);
                EditorUtility.SetDirty(controller);
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
                // Unity exposes no public clean-state setter; retain the dirty state produced by Undo.
                Debug.LogWarning($"M4a updater rolled back: {exception.Message}");
                return failure;
            }
        }

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            UpdateResult result = Resolve(scene, out Transform town, out WorldMapController controller, out GameSession session);
            if (result != UpdateResult.Success) return Invalid(out error, $"Mission hierarchy/session preflight: {result}.");
            Transform[] roots = Named(scene, RootName);
            SupplyShortageMissionPanel[] panels = Components<SupplyShortageMissionPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1)
                return Invalid(out error, "Expected exactly one mission root and one SupplyShortageMissionPanel.");
            Transform root = roots[0];
            SupplyShortageMissionPanel panel = panels[0];
            if (root.parent != town || panel.transform != root || !(root is RectTransform) || root.GetComponent<Image>() == null)
                return Invalid(out error, "Mission panel must be on the fixed root directly under M2WorldMapRoot/TownMenuRoot.");
            if (!Bound(controller, "supplyShortageMissionPanel", panel) ||
                !Bound(panel, "gameSession", session) || !Bound(panel, "panelRoot", root.gameObject))
                return Invalid(out error, "Mission controller, session, or panelRoot reference is unbound or points outside the mission hierarchy.");
            if (root.childCount != 8)
                return Invalid(out error, "Mission root must contain exactly four texts and four buttons.");
            for (int i = 0; i < 4; i++)
            {
                Transform[] texts = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == TextNames[i]).ToArray();
                Transform[] buttons = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == ButtonNames[i]).ToArray();
                if (texts.Length != 1 || buttons.Length != 1 || texts[0].parent != root || buttons[0].parent != root)
                    return Invalid(out error, "Missing, duplicate, or misplaced mission text/button.");
                Text text = texts[0].GetComponent<Text>();
                Button button = buttons[0].GetComponent<Button>();
                Text[] labels = buttons[0].GetComponentsInChildren<Text>(true);
                if (text == null || text.font == null || button == null || button.targetGraphic == null ||
                    button.targetGraphic.gameObject != button.gameObject || labels.Length != 1 || labels[0].font == null ||
                    labels[0].transform.parent != button.transform || string.IsNullOrWhiteSpace(labels[0].text) || buttons[0].childCount != 1 ||
                    !Bound(panel, TextFields[i], text) || !Bound(panel, ButtonFields[i], button))
                    return Invalid(out error, "Mission serialized references, text fonts, button graphics, or labels are invalid.");
            }
            RectTransform panelRect = (RectTransform)root;
            if (!Contained(panelRect, (RectTransform)town) || root.GetComponentsInChildren<RectTransform>(true)
                    .Any(r => r != panelRect && !Contained(r, panelRect)))
                return Invalid(out error, "Mission UI extends outside its fixed root or town rectangle.");
            return true;
        }

        private static UpdateResult Resolve(Scene scene, out Transform town, out WorldMapController controller, out GameSession session)
        {
            town = null; controller = null; session = null;
            if (!scene.IsValid() || !scene.isLoaded) return UpdateResult.InvalidScene;
            Transform[] canvas = Named(scene, "Canvas"), map = Named(scene, "M2WorldMapRoot"), towns = Named(scene, "TownMenuRoot");
            Canvas[] canvases = Components<Canvas>(scene);
            WorldMapController[] controllers = Components<WorldMapController>(scene);
            GameSession[] sessions = Components<GameSession>(scene);
            if (canvas.Length != 1 || map.Length != 1 || towns.Length != 1 || canvases.Length != 1 || controllers.Length != 1 || sessions.Length != 1)
                return UpdateResult.MissingOrDuplicateObjects;
            town = towns[0]; controller = controllers[0]; session = sessions[0];
            if (canvases[0].transform != canvas[0] || !(canvas[0] is RectTransform) || map[0].parent != canvas[0] || town.parent != map[0] ||
                controller.transform != map[0] || !(town is RectTransform) || !(map[0] is RectTransform) ||
                !Bound(controller, "gameSession", session) || !Bound(controller, "townMenuRoot", town.gameObject))
                return UpdateResult.InvalidHierarchyOrBindings;
            return UpdateResult.Success;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static bool Invalid(out string error, string message) { error = message; return false; }
        private static Transform[] Named(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name == name).ToArray();
        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        private static GameObject Create(Scene scene, Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            // Move before registering Undo so preview-scene tests do not dirty the user's active scene.
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            foreach (Type component in components) Undo.AddComponent(go, component);
            return go;
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font, string value, float y, float height)
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            Center(go.GetComponent<RectTransform>(), y, height);
            Text text = go.GetComponent<Text>();
            text.font = font; text.fontSize = 18; text.text = value;
            text.color = Color.white; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Center(RectTransform rect, float y, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(256f, height);
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(c => { Vector3 p = parent.InverseTransformPoint(c);
                return p.x >= bounds.xMin - 0.1f && p.x <= bounds.xMax + 0.1f && p.y >= bounds.yMin - 0.1f && p.y <= bounds.yMax + 0.1f; });
        }
    }
}
