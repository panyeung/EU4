using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Events;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Transactional FoundationScene updater for the SC-009 event-choice panel.
    /// The live controller stays active while its modal child is hidden, and every
    /// generated object is placed beneath one fixed root in a single Undo group.
    /// </summary>
    public static class AddSC005EventChoicePanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "EventChoiceRoot";
        public const string ModalName = "EventChoiceModal";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-009 Event Choice Panel";

        public const string TitleName = "EventChoiceTitleText";
        public const string BodyName = "EventChoiceBodyText";
        public const string OptionsName = "EventChoiceOptions";
        public const string OptionTemplateName = "EventChoiceOptionButtonTemplate";
        public const string FeedbackName = "EventChoiceFeedbackText";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string UndoName = "Add SC-009 Event Choice Panel";

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
                Debug.Log("SC-009 event choice panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-009 event choice updater refused or rolled back: {result}.");
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

        public static bool Validate(Scene scene, out string error) =>
            Validate(scene, true, out error);

        private static bool Validate(Scene scene, bool enforceFeedbackBanner, out string error)
        {
            error = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return Invalid(out error, "Scene is invalid or not loaded.");

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            EventChoicePanel[] panels = Components<EventChoicePanel>(scene);
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1)
                return Invalid(out error, "Expected exactly one Canvas, GameSession, and EventChoicePanel.");

            Transform root = FindNamed(scene, RootName);
            Transform modal = FindNamed(scene, ModalName);
            Transform titleTransform = FindNamed(scene, TitleName);
            Transform bodyTransform = FindNamed(scene, BodyName);
            Transform options = FindNamed(scene, OptionsName);
            Transform templateTransform = FindNamed(scene, OptionTemplateName);
            Transform feedbackTransform = FindNamed(scene, FeedbackName);
            if (root == null || modal == null || titleTransform == null || bodyTransform == null ||
                options == null || templateTransform == null || feedbackTransform == null)
                return Invalid(out error, "Event choice hierarchy is incomplete.");
            if (root.parent != canvases[0] || panels[0].transform != root || !root.gameObject.activeSelf)
                return Invalid(out error, "EventChoiceRoot must be active, be a direct Canvas child, and own the panel component.");
            if (modal.parent != root || modal.gameObject.activeSelf)
                return Invalid(out error, "The modal must be a hidden direct child of the active controller root.");
            if (titleTransform.parent != modal || bodyTransform.parent != modal || options.parent != modal)
                return Invalid(out error, "Title, body, and option list must be direct children of the modal.");
            if (templateTransform.parent != options || templateTransform.gameObject.activeSelf)
                return Invalid(out error, "The option template must be an inactive direct child of the option list.");
            if (feedbackTransform.parent != root)
                return Invalid(out error, "Feedback text must remain outside the hidden modal.");

            Text title = titleTransform.GetComponent<Text>();
            Text body = bodyTransform.GetComponent<Text>();
            Text feedback = feedbackTransform.GetComponent<Text>();
            Button buttonTemplate = templateTransform.GetComponent<Button>();
            Image buttonImage = templateTransform.GetComponent<Image>();
            if (title == null || body == null || feedback == null || title.font == null || body.font == null ||
                feedback.font == null || buttonTemplate == null || buttonImage == null ||
                buttonTemplate.targetGraphic != buttonImage ||
                templateTransform.GetComponentInChildren<Text>(true) == null ||
                modal.GetComponent<Image>() == null || options.GetComponent<VerticalLayoutGroup>() == null)
                return Invalid(out error, "Event choice text or button template is invalid.");

            EventChoicePanel panel = panels[0];
            if (!Bound(panel, "gameSession", sessions[0]) || !Bound(panel, "modalRoot", modal.gameObject) ||
                !Bound(panel, "titleText", title) || !Bound(panel, "bodyText", body) ||
                !Bound(panel, "optionsContent", options) || !Bound(panel, "optionButtonPrefab", buttonTemplate) ||
                !Bound(panel, "feedbackText", feedback))
                return Invalid(out error, "EventChoicePanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform modalRect = modal as RectTransform;
            if (rootRect == null || modalRect == null || !Contained(modalRect, rootRect))
                return Invalid(out error, "The event-choice modal is outside the generated root bounds.");
            RectTransform titleRect = titleTransform as RectTransform;
            RectTransform bodyRect = bodyTransform as RectTransform;
            RectTransform optionsRect = options as RectTransform;
            RectTransform feedbackRect = feedbackTransform as RectTransform;
            if (titleRect == null || bodyRect == null || optionsRect == null || feedbackRect == null ||
                !Contained(titleRect, modalRect) || !Contained(bodyRect, modalRect) ||
                !Contained(optionsRect, modalRect) ||
                (enforceFeedbackBanner && !Contained(feedbackRect, rootRect)) ||
                Overlaps(titleRect, bodyRect, modalRect) || Overlaps(bodyRect, optionsRect, modalRect) ||
                (enforceFeedbackBanner && Overlaps(modalRect, feedbackRect, rootRect)))
                return Invalid(out error, "Event choice controls overlap or lie outside their layout bounds.");
            if (enforceFeedbackBanner)
            {
                if (!EventChoicePanel.IsFeedbackBannerLayout(feedbackRect))
                    return Invalid(out error,
                        "Feedback text must be a top-anchored banner clear of the map HUD controls.");
                RectTransform mapHud = FindNamed(scene, MapRootName) as RectTransform;
                if (mapHud != null && Overlaps(feedbackRect, mapHud, rootRect))
                    return Invalid(out error, "Feedback text must not cover the map HUD action row.");
            }
            if (!TryLoadValidatedContent(out _, out _, out string contentError))
                return Invalid(out error, contentError);
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
            Transform canvas = canvases.Length == 1 ? canvases[0] : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            if (canvas == null || session == null || Components<EventChoicePanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            // A complete panel written by the previous updater differs only in the
            // feedback banner, which sat on top of the map HUD. Repair that one rect
            // in place instead of refusing the scene or rebuilding its objects.
            RectTransform legacyFeedback = FindNamed(scene, FeedbackName) as RectTransform;
            if (legacyFeedback != null && Validate(scene, false, out _))
                return RepairFeedbackBanner(scene, legacyFeedback, save, beforeValidation);

            if (HasNamed(scene, RootName) || HasNamed(scene, ModalName) ||
                HasNamed(scene, TitleName) || HasNamed(scene, BodyName) ||
                HasNamed(scene, OptionsName) || HasNamed(scene, OptionTemplateName) ||
                HasNamed(scene, FeedbackName) || Components<EventChoicePanel>(scene).Length != 0)
                return UpdateResult.ExistingPartialControl;
            if (!TryLoadValidatedContent(out EventCatalogData catalog, out EventTextCatalog text, out _))
                return UpdateResult.InvalidHierarchyOrBindings;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject rootObject = Create(scene, canvas, RootName);
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                GameObject modalObject = Create(scene, rootRect, ModalName, typeof(Image));
                RectTransform modalRect = modalObject.GetComponent<RectTransform>();
                modalRect.anchorMin = modalRect.anchorMax = modalRect.pivot = new Vector2(0.5f, 0.5f);
                modalRect.anchoredPosition = Vector2.zero;
                modalRect.sizeDelta = new Vector2(760f, 520f);
                modalObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.97f);
                modalObject.SetActive(false);

                Text title = CreateText(scene, modalRect, TitleName, font, string.Empty,
                    0f, 218f, 680f, 48f, 26, TextAnchor.MiddleCenter);
                Text body = CreateText(scene, modalRect, BodyName, font, string.Empty,
                    0f, 116f, 680f, 150f, 19, TextAnchor.UpperLeft);

                GameObject optionsObject = Create(scene, modalRect, OptionsName, typeof(VerticalLayoutGroup));
                RectTransform optionsRect = optionsObject.GetComponent<RectTransform>();
                optionsRect.anchorMin = optionsRect.anchorMax = optionsRect.pivot = new Vector2(0.5f, 0.5f);
                optionsRect.anchoredPosition = new Vector2(0f, -98f);
                optionsRect.sizeDelta = new Vector2(680f, 250f);
                VerticalLayoutGroup layout = optionsObject.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(8, 8, 8, 8);

                Button template = CreateButton(scene, optionsRect, OptionTemplateName, font,
                    "事件选项", 16, new Vector2(640f, 82f));
                template.gameObject.SetActive(false);
                Text feedback = CreateText(scene, rootRect, FeedbackName, font, string.Empty,
                    0f, 0f, EventChoicePanel.FeedbackWidth, EventChoicePanel.FeedbackHeight,
                    EventChoicePanel.FeedbackFontSize, TextAnchor.UpperCenter);
                EventChoicePanel.ApplyFeedbackBannerLayout(feedback.rectTransform);

                EventChoicePanel panel = Undo.AddComponent<EventChoicePanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, catalog, text, modalObject, title, body,
                    optionsRect, template, feedback);
                SetSerializedReferences(panel, session, modalObject, title, body, optionsRect, template, feedback);
                EditorUtility.SetDirty(panel);

                beforeValidation?.Invoke(rootObject);
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
                Debug.LogWarning($"SC-009 event choice updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        /// <summary>
        /// Transactional in-place repair for a complete panel whose feedback banner
        /// still uses the legacy centered-bottom layout. Uses the same single Undo
        /// group, validation gate and save gate as construction.
        /// </summary>
        private static UpdateResult RepairFeedbackBanner(Scene scene, RectTransform feedback,
            bool save, Action<GameObject> beforeValidation)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Text text = feedback.GetComponent<Text>();
                Undo.RecordObject(feedback, UndoName);
                if (text != null)
                    Undo.RecordObject(text, UndoName);
                EventChoicePanel.ApplyFeedbackBannerLayout(feedback);
                EditorUtility.SetDirty(feedback);
                if (text != null) EditorUtility.SetDirty(text);

                beforeValidation?.Invoke(feedback.gameObject);
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
                Debug.LogWarning($"SC-009 event choice feedback repair rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static void SetSerializedReferences(EventChoicePanel panel, GameSession session,
            GameObject modal, Text title, Text body, Transform options, Button template, Text feedback)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("modalRoot").objectReferenceValue = modal;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("optionsContent").objectReferenceValue = options;
            serialized.FindProperty("optionButtonPrefab").objectReferenceValue = template;
            serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool TryLoadValidatedContent(out EventCatalogData catalog,
            out EventTextCatalog text, out string error)
        {
            catalog = null;
            text = null;
            TextAsset catalogAsset = Resources.Load<TextAsset>("Events/event-catalog-v1");
            TextAsset textAsset = Resources.Load<TextAsset>("Localization/event-text-zh-v1");
            if (catalogAsset == null || textAsset == null)
            {
                error = "Event catalog or localization TextAsset is missing from Resources.";
                return false;
            }
            try
            {
                catalog = JsonUtility.FromJson<EventCatalogData>(catalogAsset.text);
                text = JsonUtility.FromJson<EventTextCatalog>(textAsset.text);
            }
            catch (Exception exception)
            {
                error = "Event JSON could not be parsed: " + exception.Message;
                return false;
            }
            EventCatalogValidationResult validation = EventCatalogValidator.Validate(catalog, text);
            error = validation.Message;
            return validation.Succeeded;
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

        private static Text CreateText(Scene scene, Transform parent, string name, Font font, string value,
            float x, float y, float width, float height, int fontSize, TextAnchor alignment)
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
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font,
            string label, int fontSize, Vector2 size)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            layout.flexibleHeight = 0f;
            Text text = CreateText(scene, rect, "Label", font, label,
                0f, 0f, size.x - 16f, size.y - 12f, fontSize, TextAnchor.MiddleLeft);
            text.transform.localScale = Vector3.one;
            return button;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static Transform FindNamed(Scene scene, string name)
        {
            Transform[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

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

        private static bool HasNamed(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Any(transform => transform.name == name);

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
