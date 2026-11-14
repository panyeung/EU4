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
    /// Transactional generator for the M05 shadow-escort commission page under the
    /// existing town menu. It creates one fixed root, one hidden page, the living-family
    /// assignee list and the close/accept controls in a single Undo group, validates the
    /// result before saving, and rolls the whole group back on any failure. It never
    /// reloads the scene and never rebuilds unknown existing UI.
    /// </summary>
    public static class AddM5ShadowEscortCommissionPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = ShadowEscortCommissionPanel.RootObjectName;
        public const string PanelName = ShadowEscortCommissionPanel.PageObjectName;
        public const string OpenButtonName = "M5CommissionOpenButton";
        public const string CloseButtonName = "M5CommissionCloseButton";
        public const string AcceptButtonName = "M5CommissionAcceptButton";
        public const string TitleTextName = "M5CommissionTitleText";
        public const string StateTextName = "M5CommissionStateText";
        public const string DateTextName = "M5CommissionDateText";
        public const string ResourceTextName = "M5CommissionResourceText";
        public const string CreedTextName = "M5CommissionCreedText";
        public const string OfferTextName = "M5CommissionOfferText";
        public const string ConsequenceTextName = "M5CommissionConsequenceText";
        public const string AssigneeTextName = "M5CommissionAssigneeText";
        public const string OutcomeTextName = "M5CommissionOutcomeText";
        public const string MessageTextName = "M5CommissionMessageText";
        public const string ChronicleTextName = "M5CommissionChronicleText";
        public const string CandidateScrollName = "M5CommissionCandidateScroll";
        public const string CandidateViewportName = "M5CommissionCandidateViewport";
        public const string CandidateContentName = "M5CommissionCandidateContent";
        public const string CandidateTemplateName = "M5CommissionCandidateTemplate";

        private const string MenuPath = "Tools/Sengoku Chronicle/Add M5 Shadow Escort Commission Panel";
        private const string UndoName = "Add M5 Shadow Escort Commission Panel";

        private static readonly Vector2 PanelSize = new Vector2(1160f, 260f);
        private static readonly Vector2 OpenPosition = new Vector2(-24f, -16f);
        private static readonly Vector2 OpenSize = new Vector2(220f, 40f);

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene, MissingOrDuplicateObjects,
            InvalidHierarchyOrBindings, ExistingCommissionPanel, InsufficientSpace,
            ValidationFailed, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.EditorPlaying
                : CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("M5 commission panel validated; the generated page was saved to FoundationScene.");
            else
                Debug.LogError($"M5 commission panel updater refused or rolled back: {result}.");
        }

        /// <summary>Shared menu guard, exposed so tests need not save or replace FoundationScene.</summary>
        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam for isolated scenes. Never saves.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault injection runs inside the Undo transaction, immediately before validation.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation);

        private static UpdateResult Execute(Scene scene, bool save,
            Action<GameObject> beforeValidation)
        {
            UpdateResult preflight = Resolve(scene, out Transform town, out WorldMapController controller,
                out GameSession session);
            if (preflight != UpdateResult.Success)
                return preflight;

            Transform[] existingRoots = Named(scene, RootName);
            ShadowEscortCommissionPanel[] existingPanels =
                Components<ShadowEscortCommissionPanel>(scene);
            if (existingRoots.Length != 0 || existingPanels.Length != 0)
            {
                // A complete, valid page is a no-op; any other existing shape is never rebuilt.
                return Validate(scene, out _) ? UpdateResult.Success : UpdateResult.ExistingCommissionPanel;
            }

            SerializedProperty controllerField =
                new SerializedObject(controller).FindProperty("shadowEscortCommissionPanel");
            if (controllerField == null)
                return UpdateResult.InvalidHierarchyOrBindings;
            if (controllerField.objectReferenceValue != null ||
                !controllerField.objectReferenceEntityIdValue.Equals(EntityId.None))
                return UpdateResult.ExistingCommissionPanel;

            RectTransform townRect = town as RectTransform;
            if (townRect.rect.width < 1200f || townRect.rect.height < 260f)
                return UpdateResult.InsufficientSpace;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                GameObject root = Create(scene, town, RootName);
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null)
                    throw new InvalidOperationException("Missing built-in UI font.");

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "护送委托",
                    OpenSize, 16);
                RectTransform openRect = open.GetComponent<RectTransform>();
                openRect.anchorMin = openRect.anchorMax = openRect.pivot = new Vector2(1f, 1f);
                openRect.anchoredPosition = OpenPosition;

                GameObject pageObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform page = pageObject.GetComponent<RectTransform>();
                page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
                page.anchoredPosition = Vector2.zero;
                page.sizeDelta = PanelSize;
                pageObject.GetComponent<Image>().color = SengokuUiTheme.IndigoDeep;
                pageObject.SetActive(false);

                Text title = CreateText(scene, page, TitleTextName, font, 22, new Vector2(-280f, 106f),
                    new Vector2(540f, 28f), TextAnchor.MiddleLeft, SengokuUiTheme.Gold, "护送委托");
                Text state = CreateText(scene, page, StateTextName, font, 15, new Vector2(-450f, 78f),
                    new Vector2(250f, 24f), TextAnchor.MiddleLeft, SengokuUiTheme.ParchmentLight);
                Text date = CreateText(scene, page, DateTextName, font, 13, new Vector2(-450f, 52f),
                    new Vector2(250f, 20f), TextAnchor.MiddleLeft, SengokuUiTheme.Parchment);
                Text resources = CreateText(scene, page, ResourceTextName, font, 13,
                    new Vector2(-450f, 32f), new Vector2(250f, 20f), TextAnchor.MiddleLeft,
                    SengokuUiTheme.Parchment);
                Text creed = CreateText(scene, page, CreedTextName, font, 12, new Vector2(-450f, 2f),
                    new Vector2(250f, 52f), TextAnchor.UpperLeft, SengokuUiTheme.Parchment);
                Text offer = CreateText(scene, page, OfferTextName, font, 12, new Vector2(-160f, 24f),
                    new Vector2(250f, 84f), TextAnchor.UpperLeft, SengokuUiTheme.Parchment);
                Text consequence = CreateText(scene, page, ConsequenceTextName, font, 12,
                    new Vector2(130f, 24f), new Vector2(250f, 88f), TextAnchor.UpperLeft,
                    SengokuUiTheme.Parchment);
                Text assignee = CreateText(scene, page, AssigneeTextName, font, 12,
                    new Vector2(420f, 52f), new Vector2(250f, 24f), TextAnchor.MiddleLeft,
                    SengokuUiTheme.ParchmentLight);
                Text outcome = CreateText(scene, page, OutcomeTextName, font, 12,
                    new Vector2(130f, -46f), new Vector2(250f, 44f), TextAnchor.UpperLeft,
                    SengokuUiTheme.ParchmentLight);
                Text message = CreateText(scene, page, MessageTextName, font, 12,
                    new Vector2(0f, -82f), new Vector2(1120f, 16f), TextAnchor.MiddleLeft,
                    SengokuUiTheme.Parchment);
                Text chronicle = CreateText(scene, page, ChronicleTextName, font, 11,
                    new Vector2(0f, -106f), new Vector2(1120f, 16f), TextAnchor.MiddleLeft,
                    SengokuUiTheme.GoldDim);

                CreateColumnHeader(scene, page, "M5CommissionOfferHeader", font, -160f);
                CreateColumnHeader(scene, page, "M5CommissionConsequenceHeader", font, 130f);
                CreateColumnHeader(scene, page, "M5CommissionAssigneeHeader", font, 420f);

                Button accept = CreateButton(scene, page, AcceptButtonName, font, "接取委托",
                    new Vector2(240f, 40f), 15);
                RectTransform acceptRect = accept.GetComponent<RectTransform>();
                acceptRect.anchorMin = acceptRect.anchorMax = acceptRect.pivot = new Vector2(0.5f, 0.5f);
                acceptRect.anchoredPosition = new Vector2(-450f, -50f);

                Button close = CreateButton(scene, page, CloseButtonName, font, "关闭",
                    new Vector2(140f, 36f), 14);
                RectTransform closeRect = close.GetComponent<RectTransform>();
                closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0.5f);
                closeRect.anchoredPosition = new Vector2(505f, 106f);

                GameObject scrollObject = Create(scene, page, CandidateScrollName,
                    typeof(Image), typeof(ScrollRect));
                RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
                scrollRect.anchorMin = scrollRect.anchorMax = scrollRect.pivot =
                    new Vector2(0.5f, 0.5f);
                scrollRect.anchoredPosition = new Vector2(420f, -8f);
                scrollRect.sizeDelta = new Vector2(250f, 84f);
                Image scrollImage = scrollObject.GetComponent<Image>();
                scrollImage.color = new Color(0.12f, 0.15f, 0.2f, 1f);
                scrollImage.raycastTarget = true;

                GameObject viewportObject = Create(scene, scrollRect, CandidateViewportName,
                    typeof(Image), typeof(RectMask2D));
                RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = new Vector2(4f, 4f);
                viewportRect.offsetMax = new Vector2(-4f, -4f);
                Image viewportImage = viewportObject.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
                viewportImage.raycastTarget = true;

                GameObject contentObject = Create(scene, viewportRect, CandidateContentName,
                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                VerticalLayoutGroup contentLayout =
                    contentObject.GetComponent<VerticalLayoutGroup>();
                contentLayout.padding = new RectOffset(2, 2, 2, 2);
                contentLayout.spacing = 2f;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
                ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
                scroll.content = contentRect;
                scroll.viewport = viewportRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;

                GameObject template = Create(scene, contentRect, CandidateTemplateName,
                    typeof(Image), typeof(Button), typeof(LayoutElement));
                RectTransform templateRect = template.GetComponent<RectTransform>();
                templateRect.anchoredPosition = Vector2.zero;
                templateRect.sizeDelta = new Vector2(240f, 28f);
                Image templateImage = template.GetComponent<Image>();
                templateImage.color = new Color(0.2f, 0.3f, 0.4f, 1f);
                Button templateButton = template.GetComponent<Button>();
                templateButton.targetGraphic = templateImage;
                LayoutElement templateLayout = template.GetComponent<LayoutElement>();
                templateLayout.preferredWidth = 240f;
                templateLayout.preferredHeight = 28f;
                Text templateText = CreateText(scene, templateRect, "Label", font, 11,
                    Vector2.zero, new Vector2(232f, 26f), TextAnchor.MiddleLeft,
                    SengokuUiTheme.Parchment);
                templateText.raycastTarget = false;
                template.SetActive(false);

                ShadowEscortCommissionPanel panel =
                    Undo.AddComponent<ShadowEscortCommissionPanel>(root);
                panel.BindReferences(session, pageObject, open, close, accept, title, state, date,
                    resources, creed, offer, consequence, assignee, outcome, message, chronicle,
                    contentRect, template);
                Undo.RecordObject(controller, UndoName);
                controller.BindShadowEscortCommissionPanel(panel);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(panel);

                beforeValidation?.Invoke(root);
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
                // Unity exposes no public clean-state setter; retain the dirty state produced by Undo.
                Debug.LogWarning($"M5 commission updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            UpdateResult result = Resolve(scene, out Transform town, out WorldMapController controller,
                out GameSession session);
            if (result != UpdateResult.Success)
                return Invalid(out error, $"Commission hierarchy/session preflight: {result}.");

            Transform[] roots = Named(scene, RootName);
            ShadowEscortCommissionPanel[] panels = Components<ShadowEscortCommissionPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1)
                return Invalid(out error,
                    "Expected exactly one commission root and one ShadowEscortCommissionPanel.");

            Transform root = roots[0];
            ShadowEscortCommissionPanel panel = panels[0];
            RectTransform rootRect = root as RectTransform;
            RectTransform townRect = town as RectTransform;
            if (root.parent != town || panel.transform != root || rootRect == null ||
                !Contained(rootRect, townRect))
                return Invalid(out error,
                    "Commission root must be a RectTransform directly under TownMenuRoot.");

            Transform page = root.Find(PanelName);
            Transform open = root.Find(OpenButtonName);
            if (page == null || open == null || page.parent != root || open.parent != root ||
                !Bound(controller, "shadowEscortCommissionPanel", panel) ||
                !Bound(panel, "gameSession", session) ||
                !Bound(panel, "panelRoot", page.gameObject) ||
                !Bound(panel, "openButton", open.GetComponent<Button>()))
                return Invalid(out error,
                    "Commission controller, session, open button, or page reference is unbound.");

            RectTransform pageRect = page as RectTransform;
            RectTransform openRect = open as RectTransform;
            if (pageRect == null || openRect == null || page.GetComponent<Image>() == null ||
                pageRect.sizeDelta.x > townRect.rect.width ||
                pageRect.sizeDelta.y > townRect.rect.height ||
                !Contained(pageRect, rootRect) || !Contained(openRect, rootRect))
                return Invalid(out error,
                    "Commission page or open button must stay inside its root and the town rectangle.");

            string[] textNames =
            {
                TitleTextName, StateTextName, DateTextName, ResourceTextName, CreedTextName,
                OfferTextName, ConsequenceTextName, AssigneeTextName, OutcomeTextName,
                MessageTextName, ChronicleTextName
            };
            string[] textFields =
            {
                "titleText", "stateText", "dateText", "resourceText", "creedText", "offerText",
                "consequenceText", "assigneeText", "outcomeText", "messageText", "chronicleText"
            };
            RectTransform[] textRects = new RectTransform[textNames.Length];
            for (int i = 0; i < textNames.Length; i++)
            {
                Transform found = page.Find(textNames[i]);
                Text text = found != null ? found.GetComponent<Text>() : null;
                if (text == null || text.font == null || !Bound(panel, textFields[i], text) ||
                    !Contained((RectTransform)found, pageRect))
                    return Invalid(out error,
                        "Commission text is missing, unbound, fontless, or outside the page.");
                textRects[i] = (RectTransform)found;
            }

            if (!TransportButton(panel, page, pageRect, AcceptButtonName, "acceptButton",
                    out RectTransform acceptRect, out error))
                return false;
            if (!TransportButton(panel, page, pageRect, CloseButtonName, "closeButton",
                    out RectTransform closeRect, out error))
                return false;

            Transform scroll = page.Find(CandidateScrollName);
            Transform viewport = scroll != null ? scroll.Find(CandidateViewportName) : null;
            Transform content = viewport != null ? viewport.Find(CandidateContentName) : null;
            Transform template = content != null ? content.Find(CandidateTemplateName) : null;
            if (scroll == null || viewport == null || content == null || template == null)
                return Invalid(out error,
                    "Commission assignee list must use a scroll, viewport, content, and template.");
            ScrollRect scrollComponent = scroll.GetComponent<ScrollRect>();
            RectMask2D mask = viewport.GetComponent<RectMask2D>();
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (scrollComponent == null || mask == null || layout == null || fitter == null ||
                scrollComponent.content != (RectTransform)content ||
                scrollComponent.viewport != (RectTransform)viewport ||
                scrollComponent.horizontal || !scrollComponent.vertical ||
                fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                return Invalid(out error,
                    "Commission assignee list must be a vertical ScrollRect with a masked " +
                    "viewport, vertical layout, and preferred-size content.");
            if (!Bound(panel, "candidateContainer", content) ||
                !Bound(panel, "candidateTemplate", template.gameObject) ||
                template.gameObject.activeSelf || template.GetComponent<Button>() == null ||
                template.GetComponent<LayoutElement>() == null)
                return Invalid(out error,
                    "Commission candidate container or inactive template row is unbound or invalid.");
            if (!Contained((RectTransform)scroll, pageRect) ||
                !Contained((RectTransform)content, (RectTransform)viewport))
                return Invalid(out error,
                    "Commission assignee list extends outside the page or its viewport.");

            RectTransform pageTitle = textRects[0];
            RectTransform offer = textRects[5];
            RectTransform consequence = textRects[6];
            RectTransform assignee = textRects[7];
            RectTransform outcome = textRects[8];
            RectTransform message = textRects[9];
            RectTransform chronicle = textRects[10];
            if (Overlaps(pageTitle, closeRect, pageRect) ||
                Overlaps(pageTitle, acceptRect, pageRect) ||
                Overlaps(offer, consequence, pageRect) ||
                Overlaps(assignee, (RectTransform)scroll, pageRect) ||
                Overlaps(consequence, outcome, pageRect) ||
                Overlaps(outcome, message, pageRect) ||
                Overlaps(message, chronicle, pageRect))
                return Invalid(out error,
                    "Commission text, footer, or buttons overlap on the fixed layout.");

            Transform townTitle = Named(scene, "TownMenuTitleText").FirstOrDefault();
            Transform returnButton = town.Find("ReturnToMapButton");
            if ((townTitle != null && Overlaps(openRect, (RectTransform)townTitle, townRect)) ||
                (returnButton != null &&
                 Overlaps(openRect, (RectTransform)returnButton, townRect)))
                return Invalid(out error,
                    "Commission open button must not cover the town title or return control.");
            if (string.IsNullOrWhiteSpace(panel.OfferLine) || !panel.OfferLine.Contains("武艺"))
                return Invalid(out error,
                    "Commission offer text must name the attribute check basis.");
            return true;
        }

        private static bool TransportButton(ShadowEscortCommissionPanel panel, Transform page,
            RectTransform pageRect, string name, string field, out RectTransform rect,
            out string error)
        {
            error = null;
            rect = null;
            Transform found = page.Find(name);
            Button button = found != null ? found.GetComponent<Button>() : null;
            Text[] labels = found != null ? found.GetComponentsInChildren<Text>(true) : null;
            if (button == null || button.targetGraphic == null ||
                button.targetGraphic.gameObject != button.gameObject ||
                labels == null || labels.Length != 1 || labels[0].font == null ||
                labels[0].transform.parent != button.transform ||
                string.IsNullOrWhiteSpace(labels[0].text) || button.transform.childCount != 1 ||
                !Bound(panel, field, button) ||
                !Contained((RectTransform)found, pageRect))
            {
                error = $"Commission button '{name}' is missing, unbound, styling-less, or outside the page.";
                return false;
            }
            rect = (RectTransform)found;
            return true;
        }

        private static void CreateColumnHeader(Scene scene, Transform parent, string name, Font font,
            float x)
        {
            CreateText(scene, parent, name, font, 15, new Vector2(x, 78f), new Vector2(250f, 22f),
                TextAnchor.MiddleLeft, SengokuUiTheme.Gold,
                name.Contains("Offer") ? "委托条款"
                : name.Contains("Consequence") ? "结果与后果" : "执行人");
        }

        private static UpdateResult Resolve(Scene scene, out Transform town,
            out WorldMapController controller, out GameSession session)
        {
            town = null; controller = null; session = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return UpdateResult.InvalidScene;
            Transform[] canvas = Named(scene, "Canvas");
            Transform[] map = Named(scene, "M2WorldMapRoot");
            Transform[] towns = Named(scene, "TownMenuRoot");
            Canvas[] canvases = Components<Canvas>(scene);
            WorldMapController[] controllers = Components<WorldMapController>(scene);
            GameSession[] sessions = Components<GameSession>(scene);
            if (canvas.Length != 1 || map.Length != 1 || towns.Length != 1 ||
                canvases.Length != 1 || controllers.Length != 1 || sessions.Length != 1)
                return UpdateResult.MissingOrDuplicateObjects;
            town = towns[0];
            controller = controllers[0];
            session = sessions[0];
            if (canvases[0].transform != canvas[0] || !(canvas[0] is RectTransform) ||
                map[0].parent != canvas[0] || town.parent != map[0] ||
                controller.transform != map[0] || !(town is RectTransform) ||
                !(map[0] is RectTransform) ||
                !Bound(controller, "gameSession", session) ||
                !Bound(controller, "townMenuRoot", town.gameObject))
                return UpdateResult.InvalidHierarchyOrBindings;
            return UpdateResult.Success;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }

        private static Transform[] Named(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name).ToArray();

        private static T[] Components<T>(Scene scene) where T : Component => scene
            .GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        private static GameObject Create(Scene scene, Transform parent, string name,
            params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            // Move before registering Undo so preview-scene tests do not dirty the user's active scene.
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

        private static Text CreateText(Scene scene, Transform parent, string name, Font font,
            int size, Vector2 position, Vector2 dimensions, TextAnchor alignment, Color color,
            string value = "")
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font,
            string label, Vector2 size, int fontSize)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.24f, 0.32f, 0.42f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(scene, rect, "Label", font, fontSize, Vector2.zero,
                new Vector2(size.x - 12f, size.y - 6f), TextAnchor.MiddleCenter,
                SengokuUiTheme.ParchmentLight, label);
            text.raycastTarget = false;
            return button;
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            if (child == null || parent == null)
                return false;
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(c =>
            {
                Vector3 point = parent.InverseTransformPoint(c);
                return point.x >= bounds.xMin - 0.1f && point.x <= bounds.xMax + 0.1f &&
                       point.y >= bounds.yMin - 0.1f && point.y <= bounds.yMax + 0.1f;
            });
        }

        private static bool Overlaps(RectTransform first, RectTransform second, RectTransform space)
        {
            if (first == null || second == null || space == null)
                return false;
            Rect a = LocalRect(first, space);
            Rect b = LocalRect(second, space);
            float width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return width > 0.5f && height > 0.5f;
        }

        private static Rect LocalRect(RectTransform rect, RectTransform space)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 min = space.InverseTransformPoint(corners[0]);
            Vector3 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 point = space.InverseTransformPoint(corners[i]);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
