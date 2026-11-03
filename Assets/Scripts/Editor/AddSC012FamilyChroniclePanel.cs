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
    /// Transactional FoundationScene updater for the SC-012 family chronicle panel.
    /// Every generated object lives beneath one fixed root in a single Undo group;
    /// validation runs before saving, and any failure reverts the group without
    /// reloading the scene.
    /// </summary>
    public static class AddSC012FamilyChroniclePanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "FamilyChronicleRoot";
        public const string OpenButtonName = "FamilyChronicleOpenButton";
        public const string PanelName = "FamilyChroniclePanel";
        public const string TitleName = "FamilyChronicleTitleText";
        public const string ScrollName = "FamilyChronicleScroll";
        public const string ContentName = "FamilyChronicleContent";
        public const string RowTemplateName = "FamilyChronicleRowTemplate";
        public const string EmptyName = "FamilyChronicleEmptyText";
        public const string CloseButtonName = "FamilyChronicleCloseButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-012 Family Chronicle Panel";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventChoiceRootName = "EventChoiceRoot";
        private const string UndoName = "Add SC-012 Family Chronicle Panel";

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
                Debug.Log("SC-012 family chronicle panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-012 family chronicle updater refused or rolled back: {result}.");
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
            FamilyChroniclePanel[] panels = Components<FamilyChroniclePanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 || roots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, FamilyChroniclePanel and chronicle root.");

            Transform root = roots[0];
            Transform open = Require(scene, OpenButtonName);
            Transform panel = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform scroll = Require(scene, ScrollName);
            Transform content = Require(scene, ContentName);
            Transform template = Require(scene, RowTemplateName);
            Transform empty = Require(scene, EmptyName);
            Transform close = Require(scene, CloseButtonName);
            if (open == null || panel == null || title == null || scroll == null || content == null ||
                template == null || empty == null || close == null)
                return Invalid(out error, "Family chronicle hierarchy is incomplete or ambiguous.");

            if (root.parent != canvases[0] || panels[0].transform != root || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "FamilyChronicleRoot must be active, be a direct Canvas child, and own the panel component.");

            Transform canvas = canvases[0];
            Transform mapRoot = canvas.Find(MapRootName);
            Transform titleRoot = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (mapRoot != null && root.GetSiblingIndex() <= mapRoot.GetSiblingIndex())
                return Invalid(out error,
                    "FamilyChronicleRoot must render above the map/town HUD so its buttons receive raycasts.");
            if (titleRoot != null && root.GetSiblingIndex() >= titleRoot.GetSiblingIndex())
                return Invalid(out error, "FamilyChronicleRoot must render below the title overlay.");
            if (eventRoot != null && root.GetSiblingIndex() >= eventRoot.GetSiblingIndex())
                return Invalid(out error, "FamilyChronicleRoot must render below the event-choice overlay.");

            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error, "The open button must be an active direct child of the chronicle root.");
            if (panel.parent != root || panel.gameObject.activeSelf)
                return Invalid(out error, "FamilyChroniclePanel must be a hidden direct child of the root.");
            if (title.parent != panel || scroll.parent != panel || empty.parent != panel || close.parent != panel)
                return Invalid(out error,
                    "Chronicle title, scroll area, empty state and close button must be direct panel children.");
            if (content.parent != scroll || template.parent != content || template.gameObject.activeSelf)
                return Invalid(out error,
                    "The row template must be an inactive direct child of the scroll content.");

            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            RectTransform contentRect = content as RectTransform;
            RectTransform scrollRectTransform = scroll as RectTransform;
            if (scrollRect == null || contentRect == null || scrollRectTransform == null ||
                scrollRect.content != contentRect || scrollRect.viewport != scrollRectTransform ||
                !scrollRect.vertical || scrollRect.horizontal)
                return Invalid(out error,
                    "The scroll area must own a vertical ScrollRect whose content is FamilyChronicleContent.");
            if (content.GetComponent<VerticalLayoutGroup>() == null ||
                content.GetComponent<ContentSizeFitter>() == null)
                return Invalid(out error, "Scroll content must have a vertical layout and a size fitter.");

            Text titleText = title.GetComponent<Text>();
            Text emptyText = empty.GetComponent<Text>();
            Text templateText = template.GetComponent<Text>();
            Button openButton = open.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            if (titleText == null || emptyText == null || templateText == null ||
                titleText.font == null || emptyText.font == null || templateText.font == null ||
                openButton == null || closeButton == null ||
                !HasButtonVisual(openButton) || !HasButtonVisual(closeButton) ||
                !HasRaycastTarget(openButton) || !HasRaycastTarget(closeButton) ||
                template.GetComponent<LayoutElement>() == null ||
                panel.GetComponent<Image>() == null || !panel.GetComponent<Image>().raycastTarget ||
                scroll.GetComponent<Image>() == null || !scroll.GetComponent<Image>().raycastTarget ||
                scroll.GetComponent<RectMask2D>() == null)
                return Invalid(out error,
                    "Chronicle text, button, background, mask, layout or raycast components are invalid.");

            FamilyChroniclePanel familyPanel = panels[0];
            if (!Bound(familyPanel, "gameSession", sessions[0]) ||
                !Bound(familyPanel, "panelRoot", panel.gameObject) ||
                !Bound(familyPanel, "openButton", openButton) ||
                !Bound(familyPanel, "closeButton", closeButton) ||
                !Bound(familyPanel, "scrollRect", scrollRect) ||
                !Bound(familyPanel, "content", content) ||
                !Bound(familyPanel, "rowTemplate", template.gameObject) ||
                !Bound(familyPanel, "emptyText", emptyText) ||
                !Bound(familyPanel, "titleText", titleText))
                return Invalid(out error, "FamilyChroniclePanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform panelRect = panel as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform titleRect = title as RectTransform;
            RectTransform scrollAreaRect = scroll as RectTransform;
            RectTransform emptyRect = empty as RectTransform;
            RectTransform closeRect = close as RectTransform;
            if (rootRect == null || panelRect == null || openRect == null || titleRect == null ||
                scrollAreaRect == null || emptyRect == null || closeRect == null ||
                !Contained(panelRect, rootRect) || !Contained(openRect, rootRect) ||
                !Contained(titleRect, panelRect) || !Contained(scrollAreaRect, panelRect) ||
                !Contained(emptyRect, panelRect) || !Contained(closeRect, panelRect) ||
                Overlaps(titleRect, scrollAreaRect, panelRect) ||
                Overlaps(scrollAreaRect, closeRect, panelRect) ||
                Overlaps(titleRect, closeRect, panelRect))
                return Invalid(out error, "Chronicle controls overlap or lie outside their bounds.");
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
            if (canvas == null || session == null || Components<FamilyChroniclePanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (Named(scene, RootName).Any() || Named(scene, OpenButtonName).Any() ||
                Named(scene, PanelName).Any() || Named(scene, TitleName).Any() ||
                Named(scene, ScrollName).Any() || Named(scene, ContentName).Any() ||
                Named(scene, RowTemplateName).Any() || Named(scene, EmptyName).Any() ||
                Named(scene, CloseButtonName).Any() || Components<FamilyChroniclePanel>(scene).Length != 0)
                return UpdateResult.ExistingPartialControl;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject rootObject = Create(scene, canvas, RootName);
                PlaceAboveMapHud(canvas, rootObject.transform);
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "家族编年史",
                    0f, 0f, 200f, 44f, 16);
                RectTransform openRect = open.GetComponent<RectTransform>();
                openRect.anchorMin = openRect.anchorMax = openRect.pivot = new Vector2(1f, 1f);
                openRect.anchoredPosition = new Vector2(-20f, -20f);

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(760f, 540f);
                panelObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font, "家族编年史",
                    0f, 235f, 720f, 36f, 22, TextAnchor.MiddleLeft);

                GameObject scrollObject = Create(scene, panelRect, ScrollName,
                    typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
                RectTransform scrollAreaRect = scrollObject.GetComponent<RectTransform>();
                scrollAreaRect.anchorMin = scrollAreaRect.anchorMax = scrollAreaRect.pivot =
                    new Vector2(0.5f, 0.5f);
                scrollAreaRect.anchoredPosition = new Vector2(0f, 10f);
                scrollAreaRect.sizeDelta = new Vector2(700f, 380f);
                scrollObject.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.9f);
                ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.viewport = scrollAreaRect;

                GameObject contentObject = Create(scene, scrollAreaRect, ContentName,
                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                ConfigureVerticalLayout(contentObject.GetComponent<VerticalLayoutGroup>(), 8);
                ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scrollRect.content = contentRect;

                GameObject templateObject = Create(scene, contentRect, RowTemplateName,
                    typeof(Text), typeof(LayoutElement));
                RectTransform templateRect = templateObject.GetComponent<RectTransform>();
                templateRect.anchorMin = new Vector2(0f, 1f);
                templateRect.anchorMax = new Vector2(1f, 1f);
                templateRect.pivot = new Vector2(0.5f, 1f);
                templateRect.sizeDelta = new Vector2(0f, 44f);
                Text templateText = templateObject.GetComponent<Text>();
                templateText.font = font;
                templateText.fontSize = 15;
                templateText.color = Color.white;
                templateText.alignment = TextAnchor.UpperLeft;
                templateText.horizontalOverflow = HorizontalWrapMode.Wrap;
                templateText.verticalOverflow = VerticalWrapMode.Overflow;
                templateText.raycastTarget = false;
                LayoutElement templateLayout = templateObject.GetComponent<LayoutElement>();
                // A negative preferred height is ignored by the layout system, so the
                // live row's wrapped text measurement decides its own height instead of
                // a fixed 86px box that clips long succession messages.
                templateLayout.preferredHeight = -1f;
                templateLayout.flexibleHeight = 0f;
                templateObject.SetActive(false);

                Text empty = CreateText(scene, panelRect, EmptyName, font,
                    "尚无家族记录。完成事件、任务、关系变化或继承后，这里会按时间列出。",
                    0f, 10f, 650f, 72f, 16, TextAnchor.MiddleCenter);

                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    0f, -235f, 170f, 42f, 16);

                FamilyChroniclePanel panel = Undo.AddComponent<FamilyChroniclePanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, panelObject, open, close, scrollRect, contentRect,
                    templateObject, empty, title);
                SetSerializedReferences(panel, session, panelObject, open, close, scrollRect,
                    contentRect, templateObject, empty, title);
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
                Debug.LogWarning($"SC-012 family chronicle updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static void PlaceAboveMapHud(Transform canvas, Transform root)
        {
            Transform title = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            int? anchorIndex = null;
            if (title != null)
                anchorIndex = title.GetSiblingIndex();
            if (eventRoot != null)
                anchorIndex = anchorIndex.HasValue
                    ? Mathf.Min(anchorIndex.Value, eventRoot.GetSiblingIndex())
                    : eventRoot.GetSiblingIndex();
            if (anchorIndex.HasValue)
                root.SetSiblingIndex(anchorIndex.Value);
            else
                root.SetAsLastSibling();
        }

        private static void ConfigureVerticalLayout(VerticalLayoutGroup layout, int spacing)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);
        }

        private static void SetSerializedReferences(FamilyChroniclePanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, ScrollRect scroll, Transform content,
            GameObject template, Text empty, Text title)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
            serialized.FindProperty("content").objectReferenceValue = content;
            serialized.FindProperty("rowTemplate").objectReferenceValue = template;
            serialized.FindProperty("emptyText").objectReferenceValue = empty;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            string label, float x, float y, float width, float height, int fontSize)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
            Text text = CreateText(scene, rect, "Label", font, label,
                0f, 0f, width - 16f, height - 10f, fontSize, TextAnchor.MiddleCenter);
            text.transform.localScale = Vector3.one;
            return button;
        }

        private static bool HasButtonVisual(Button button)
        {
            if (button == null || button.targetGraphic == null)
                return false;
            return button.GetComponentInChildren<Text>(true) != null;
        }

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

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
