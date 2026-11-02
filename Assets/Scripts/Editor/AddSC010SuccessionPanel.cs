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
    /// Transactional FoundationScene updater for the SC-010 succession HUD and modal.
    /// Every generated object lives beneath one fixed root in a single Undo group;
    /// validation runs before saving and any failure rolls the group back without
    /// reloading the scene.
    /// </summary>
    public static class AddSC010SuccessionPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "SuccessionRoot";
        public const string HudName = "SuccessionHud";
        public const string HeadName = "SuccessionHeadText";
        public const string StatusName = "SuccessionStatusText";
        public const string RetireName = "SuccessionRetireButton";
        public const string CrisisDemoName = "SuccessionCrisisDemoButton";
        public const string FeedbackName = "SuccessionFeedbackText";
        public const string ModalName = "SuccessionModal";
        public const string ModalStatusName = "SuccessionModalStatusText";
        public const string CandidateContentName = "SuccessionCandidates";
        public const string CandidateTemplateName = "SuccessionCandidateButtonTemplate";
        public const string RegentContentName = "SuccessionRegents";
        public const string RegentTemplateName = "SuccessionRegentButtonTemplate";
        public const string ConfirmName = "SuccessionConfirmButton";
        public const string AcceptCrisisName = "SuccessionAcceptCrisisButton";
        public const string DeclineCrisisName = "SuccessionDeclineCrisisButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-010 Succession Panel";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventChoiceRootName = "EventChoiceRoot";
        private const string UndoName = "Add SC-010 Succession Panel";

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
                Debug.Log("SC-010 succession panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-010 succession updater refused or rolled back: {result}.");
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
            SuccessionPanel[] panels = Components<SuccessionPanel>(scene);
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1)
                return Invalid(out error, "Expected exactly one Canvas, GameSession, and SuccessionPanel.");

            Transform root = FindNamed(scene, RootName);
            Transform hud = FindNamed(scene, HudName);
            Transform head = FindNamed(scene, HeadName);
            Transform status = FindNamed(scene, StatusName);
            Transform retire = FindNamed(scene, RetireName);
            Transform crisisDemo = FindNamed(scene, CrisisDemoName);
            Transform feedback = FindNamed(scene, FeedbackName);
            Transform modal = FindNamed(scene, ModalName);
            Transform modalStatus = FindNamed(scene, ModalStatusName);
            Transform candidates = FindNamed(scene, CandidateContentName);
            Transform candidateTemplate = FindNamed(scene, CandidateTemplateName);
            Transform regents = FindNamed(scene, RegentContentName);
            Transform regentTemplate = FindNamed(scene, RegentTemplateName);
            Transform confirm = FindNamed(scene, ConfirmName);
            Transform accept = FindNamed(scene, AcceptCrisisName);
            Transform decline = FindNamed(scene, DeclineCrisisName);
            if (root == null || hud == null || head == null || status == null || retire == null ||
                crisisDemo == null || feedback == null || modal == null || modalStatus == null ||
                candidates == null || candidateTemplate == null || regents == null ||
                regentTemplate == null || confirm == null || accept == null || decline == null)
                return Invalid(out error, "Succession hierarchy is incomplete.");

            if (root.parent != canvases[0] || panels[0].transform != root || !root.gameObject.activeSelf)
                return Invalid(out error, "SuccessionRoot must be active, be a direct Canvas child, and own the panel component.");

            // The first version placed SuccessionRoot behind M2WorldMapRoot, so map/town
            // graphics intercepted every click on the HUD buttons. The root must render
            // above the map/town HUD, while remaining below the full-screen title and
            // event-choice overlays.
            Transform canvas = canvases[0];
            Transform mapRoot = canvas.Find(MapRootName);
            Transform titleRoot = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (mapRoot != null && root.GetSiblingIndex() <= mapRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SuccessionRoot must render above the map/town HUD so its buttons receive raycasts.");
            if (titleRoot != null && root.GetSiblingIndex() >= titleRoot.GetSiblingIndex())
                return Invalid(out error, "SuccessionRoot must render below the title overlay.");
            if (eventRoot != null && root.GetSiblingIndex() >= eventRoot.GetSiblingIndex())
                return Invalid(out error, "SuccessionRoot must render below the event-choice overlay.");
            if (hud.parent != root || !hud.gameObject.activeSelf)
                return Invalid(out error, "SuccessionHud must be an active direct child of SuccessionRoot.");
            if (modal.parent != root || modal.gameObject.activeSelf)
                return Invalid(out error, "SuccessionModal must be a hidden direct child of SuccessionRoot.");
            if (head.parent != hud || status.parent != hud || retire.parent != hud ||
                crisisDemo.parent != hud || feedback.parent != hud)
                return Invalid(out error, "HUD text and buttons must be direct children of SuccessionHud.");
            if (modalStatus.parent != modal || candidates.parent != modal || regents.parent != modal ||
                confirm.parent != modal || accept.parent != modal || decline.parent != modal)
                return Invalid(out error, "Modal controls must be direct children of SuccessionModal.");
            if (candidateTemplate.parent != candidates || candidateTemplate.gameObject.activeSelf)
                return Invalid(out error, "Candidate template must be an inactive direct child of the candidate list.");
            if (regentTemplate.parent != regents || regentTemplate.gameObject.activeSelf)
                return Invalid(out error, "Regent template must be an inactive direct child of the regent list.");

            Text headText = head.GetComponent<Text>();
            Text statusText = status.GetComponent<Text>();
            Text feedbackText = feedback.GetComponent<Text>();
            Text modalStatusText = modalStatus.GetComponent<Text>();
            Button retireButton = retire.GetComponent<Button>();
            Button crisisDemoButton = crisisDemo.GetComponent<Button>();
            Button candidateTemplateButton = candidateTemplate.GetComponent<Button>();
            Button regentTemplateButton = regentTemplate.GetComponent<Button>();
            Button confirmButton = confirm.GetComponent<Button>();
            Button acceptButton = accept.GetComponent<Button>();
            Button declineButton = decline.GetComponent<Button>();
            if (headText == null || statusText == null || feedbackText == null ||
                modalStatusText == null || retireButton == null || crisisDemoButton == null ||
                candidateTemplateButton == null || regentTemplateButton == null ||
                confirmButton == null || acceptButton == null || declineButton == null ||
                headText.font == null || statusText.font == null || feedbackText.font == null ||
                modalStatusText.font == null || !HasButtonVisual(candidateTemplateButton) ||
                !HasButtonVisual(regentTemplateButton) || !HasButtonVisual(retireButton) ||
                !HasButtonVisual(crisisDemoButton) || !HasButtonVisual(confirmButton) ||
                !HasButtonVisual(acceptButton) || !HasButtonVisual(declineButton) ||
                !HasRaycastTarget(retireButton) || !HasRaycastTarget(crisisDemoButton) ||
                !HasRaycastTarget(confirmButton) || !HasRaycastTarget(acceptButton) ||
                !HasRaycastTarget(declineButton) || !HasRaycastTarget(candidateTemplateButton) ||
                !HasRaycastTarget(regentTemplateButton) ||
                modal.GetComponent<Image>() == null || !modal.GetComponent<Image>().raycastTarget ||
                hud.GetComponent<Image>() == null || !hud.GetComponent<Image>().raycastTarget ||
                candidates.GetComponent<VerticalLayoutGroup>() == null ||
                regents.GetComponent<VerticalLayoutGroup>() == null)
                return Invalid(out error, "Succession text, button, layout, background, or raycast components are invalid.");

            SuccessionPanel panel = panels[0];
            if (!Bound(panel, "gameSession", sessions[0]) || !Bound(panel, "modalRoot", modal.gameObject) ||
                !Bound(panel, "headText", headText) || !Bound(panel, "statusText", statusText) ||
                !Bound(panel, "retireButton", retireButton) || !Bound(panel, "crisisDemoButton", crisisDemoButton) ||
                !Bound(panel, "feedbackText", feedbackText) || !Bound(panel, "modalStatusText", modalStatusText) ||
                !Bound(panel, "candidateContent", candidates) ||
                !Bound(panel, "candidateButtonPrefab", candidateTemplateButton) ||
                !Bound(panel, "regentContent", regents) ||
                !Bound(panel, "regentButtonPrefab", regentTemplateButton) ||
                !Bound(panel, "confirmButton", confirmButton) ||
                !Bound(panel, "acceptCrisisButton", acceptButton) ||
                !Bound(panel, "declineCrisisButton", declineButton))
                return Invalid(out error, "SuccessionPanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform hudRect = hud as RectTransform;
            RectTransform modalRect = modal as RectTransform;
            if (rootRect == null || hudRect == null || modalRect == null ||
                !Contained(hudRect, rootRect) || !Contained(modalRect, rootRect))
                return Invalid(out error, "Succession HUD or modal lies outside the root bounds.");

            RectTransform headRect = head as RectTransform;
            RectTransform statusRect = status as RectTransform;
            RectTransform retireRect = retire as RectTransform;
            RectTransform crisisRect = crisisDemo as RectTransform;
            RectTransform feedbackRect = feedback as RectTransform;
            if (headRect == null || statusRect == null || retireRect == null ||
                crisisRect == null || feedbackRect == null ||
                !Contained(headRect, hudRect) || !Contained(statusRect, hudRect) ||
                !Contained(retireRect, hudRect) || !Contained(crisisRect, hudRect) ||
                !Contained(feedbackRect, hudRect) ||
                Overlaps(headRect, statusRect, hudRect) || Overlaps(statusRect, retireRect, hudRect) ||
                Overlaps(retireRect, crisisRect, hudRect) || Overlaps(crisisRect, feedbackRect, hudRect) ||
                Overlaps(retireRect, feedbackRect, hudRect))
                return Invalid(out error, "Succession HUD controls overlap or lie outside their bounds.");

            RectTransform modalStatusRect = modalStatus as RectTransform;
            RectTransform candidatesRect = candidates as RectTransform;
            RectTransform regentsRect = regents as RectTransform;
            RectTransform confirmRect = confirm as RectTransform;
            RectTransform acceptRect = accept as RectTransform;
            RectTransform declineRect = decline as RectTransform;
            if (modalStatusRect == null || candidatesRect == null || regentsRect == null ||
                confirmRect == null || acceptRect == null || declineRect == null ||
                !Contained(modalStatusRect, modalRect) || !Contained(candidatesRect, modalRect) ||
                !Contained(regentsRect, modalRect) || !Contained(confirmRect, modalRect) ||
                !Contained(acceptRect, modalRect) || !Contained(declineRect, modalRect) ||
                Overlaps(modalStatusRect, candidatesRect, modalRect) ||
                Overlaps(candidatesRect, regentsRect, modalRect) ||
                Overlaps(regentsRect, confirmRect, modalRect) ||
                Overlaps(confirmRect, acceptRect, modalRect) ||
                Overlaps(acceptRect, declineRect, modalRect))
                return Invalid(out error, "Succession modal controls overlap or lie outside their bounds.");
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
            if (canvas == null || session == null || Components<SuccessionPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (HasNamed(scene, RootName) || HasNamed(scene, HudName) || HasNamed(scene, HeadName) ||
                HasNamed(scene, StatusName) || HasNamed(scene, RetireName) ||
                HasNamed(scene, CrisisDemoName) || HasNamed(scene, FeedbackName) ||
                HasNamed(scene, ModalName) || HasNamed(scene, ModalStatusName) ||
                HasNamed(scene, CandidateContentName) || HasNamed(scene, CandidateTemplateName) ||
                HasNamed(scene, RegentContentName) || HasNamed(scene, RegentTemplateName) ||
                HasNamed(scene, ConfirmName) || HasNamed(scene, AcceptCrisisName) ||
                HasNamed(scene, DeclineCrisisName) || Components<SuccessionPanel>(scene).Length != 0)
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

                GameObject hudObject = Create(scene, rootRect, HudName, typeof(Image));
                RectTransform hudRect = hudObject.GetComponent<RectTransform>();
                hudRect.anchorMin = hudRect.anchorMax = new Vector2(0f, 1f);
                hudRect.pivot = new Vector2(0f, 1f);
                hudRect.anchoredPosition = new Vector2(20f, -20f);
                hudRect.sizeDelta = new Vector2(430f, 210f);
                hudObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.94f);

                Text head = CreateText(scene, hudRect, HeadName, font, "当代家主", 0f, 80f, 390f, 30f,
                    20, TextAnchor.MiddleLeft);
                Text status = CreateText(scene, hudRect, StatusName, font, string.Empty, 0f, 35f, 390f, 50f,
                    15, TextAnchor.UpperLeft);
                Button retire = CreateButton(scene, hudRect, RetireName, font, "退休并安排继承",
                    -100f, -25f, 190f, 36f, 15);
                Button crisisDemo = CreateButton(scene, hudRect, CrisisDemoName, font, "示范：无继承人危机",
                    105f, -25f, 200f, 36f, 14);
                Text feedback = CreateText(scene, hudRect, FeedbackName, font, string.Empty,
                    0f, -80f, 390f, 30f, 13, TextAnchor.UpperLeft);

                GameObject modalObject = Create(scene, rootRect, ModalName, typeof(Image));
                RectTransform modalRect = modalObject.GetComponent<RectTransform>();
                modalRect.anchorMin = modalRect.anchorMax = modalRect.pivot = new Vector2(0.5f, 0.5f);
                modalRect.anchoredPosition = Vector2.zero;
                modalRect.sizeDelta = new Vector2(860f, 600f);
                modalObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                modalObject.SetActive(false);

                Text modalStatus = CreateText(scene, modalRect, ModalStatusName, font, string.Empty,
                    0f, 250f, 800f, 60f, 17, TextAnchor.UpperLeft);

                GameObject candidatesObject = Create(scene, modalRect, CandidateContentName,
                    typeof(VerticalLayoutGroup));
                RectTransform candidatesRect = candidatesObject.GetComponent<RectTransform>();
                candidatesRect.anchorMin = candidatesRect.anchorMax = candidatesRect.pivot =
                    new Vector2(0.5f, 0.5f);
                candidatesRect.anchoredPosition = new Vector2(0f, 80f);
                candidatesRect.sizeDelta = new Vector2(800f, 210f);
                ConfigureVerticalLayout(candidatesObject.GetComponent<VerticalLayoutGroup>(), 6);

                Button candidateTemplate = CreateButton(scene, candidatesRect, CandidateTemplateName,
                    font, "继承人", 0f, 0f, 780f, 56f, 15);
                candidateTemplate.gameObject.SetActive(false);

                GameObject regentsObject = Create(scene, modalRect, RegentContentName,
                    typeof(VerticalLayoutGroup));
                RectTransform regentsRect = regentsObject.GetComponent<RectTransform>();
                regentsRect.anchorMin = regentsRect.anchorMax = regentsRect.pivot = new Vector2(0.5f, 0.5f);
                regentsRect.anchoredPosition = new Vector2(0f, -110f);
                regentsRect.sizeDelta = new Vector2(800f, 160f);
                ConfigureVerticalLayout(regentsObject.GetComponent<VerticalLayoutGroup>(), 4);

                Button regentTemplate = CreateButton(scene, regentsRect, RegentTemplateName,
                    font, "摄政", 0f, 0f, 780f, 40f, 14);
                regentTemplate.gameObject.SetActive(false);

                Button confirm = CreateButton(scene, modalRect, ConfirmName, font, "确认交接",
                    -230f, -260f, 210f, 44f, 15);
                Button accept = CreateButton(scene, modalRect, AcceptCrisisName, font, "接受危机收养",
                    0f, -260f, 210f, 44f, 15);
                Button decline = CreateButton(scene, modalRect, DeclineCrisisName, font, "放弃并灭亡",
                    230f, -260f, 210f, 44f, 15);

                SuccessionPanel panel = Undo.AddComponent<SuccessionPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, modalObject, head, status, retire, crisisDemo, feedback,
                    modalStatus, candidatesRect, candidateTemplate, regentsRect, regentTemplate,
                    confirm, accept, decline);
                SetSerializedReferences(panel, session, modalObject, head, status, retire, crisisDemo,
                    feedback, modalStatus, candidatesRect, candidateTemplate, regentsRect,
                    regentTemplate, confirm, accept, decline);
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
                Debug.LogWarning($"SC-010 succession updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static void PlaceAboveMapHud(Transform canvas, Transform root)
        {
            // Insert just below the first full-screen overlay so the HUD sits above the
            // map/town controls but never covers the title screen or an event modal.
            Transform title = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            Transform anchor = title != null ? title : eventRoot;
            if (anchor != null)
                root.SetSiblingIndex(anchor.GetSiblingIndex());
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

        private static void SetSerializedReferences(SuccessionPanel panel, GameSession session,
            GameObject modal, Text head, Text status, Button retire, Button crisisDemo, Text feedback,
            Text modalStatus, Transform candidates, Button candidateTemplate, Transform regents,
            Button regentTemplate, Button confirm, Button accept, Button decline)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("modalRoot").objectReferenceValue = modal;
            serialized.FindProperty("headText").objectReferenceValue = head;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("retireButton").objectReferenceValue = retire;
            serialized.FindProperty("crisisDemoButton").objectReferenceValue = crisisDemo;
            serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
            serialized.FindProperty("modalStatusText").objectReferenceValue = modalStatus;
            serialized.FindProperty("candidateContent").objectReferenceValue = candidates;
            serialized.FindProperty("candidateButtonPrefab").objectReferenceValue = candidateTemplate;
            serialized.FindProperty("regentContent").objectReferenceValue = regents;
            serialized.FindProperty("regentButtonPrefab").objectReferenceValue = regentTemplate;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            serialized.FindProperty("acceptCrisisButton").objectReferenceValue = accept;
            serialized.FindProperty("declineCrisisButton").objectReferenceValue = decline;
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
                0f, 0f, width - 16f, height - 10f, fontSize, TextAnchor.MiddleLeft);
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
