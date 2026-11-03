using System;
using System.Collections.Generic;
using System.Linq;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Transactional FoundationScene updater for the SC-017 generic family graph and
    /// the derived third-generation ending panel. Every generated object lives beneath
    /// one fixed root in a single Undo group; validation runs before saving and any
    /// failure reverts the group without reloading the scene. A recognised legacy
    /// six-row roster hierarchy is replaced inside the same transaction.
    /// </summary>
    public static class AddSC013FamilyRosterPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "FamilyRosterRoot";
        public const string OpenButtonName = "FamilyRosterOpenButton";
        public const string PanelName = "FamilyRosterPanel";
        public const string TitleName = "FamilyRosterTitleText";
        public const string LegendName = "FamilyRosterLegendText";
        public const string ScrollName = "FamilyRosterScroll";
        public const string ContentName = "FamilyRosterContent";
        public const string EdgeLayerName = "FamilyRosterEdgeLayer";
        public const string NodeLayerName = "FamilyRosterNodeLayer";
        public const string NodeTemplateName = "FamilyRosterNodeTemplate";
        public const string EdgeTemplateName = "FamilyRosterEdgeTemplate";
        public const string SectionTemplateName = "FamilyRosterSectionTemplate";
        public const string HeirName = "FamilyRosterHeirText";
        public const string CloseButtonName = "FamilyRosterCloseButton";

        public const string EndingPanelName = "M2EndingPanel";
        public const string EndingTitleName = "M2EndingTitleText";
        public const string EndingBodyName = "M2EndingBodyText";
        public const string EndingCloseName = "M2EndingCloseButton";

        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-013 Family Roster Panel";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventChoiceRootName = "EventChoiceRoot";
        private const string UndoName = "Add SC-017 Family Graph Panel";

        // Legacy SC-013 row hierarchy names, used only to recognise and replace the
        // previous fixed-row panel inside one Undo transaction.
        private const string LegacyRowTemplateName = "FamilyRosterRowTemplate";
        private const string LegacyButtonLabelName = "Label";

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
                Debug.Log("SC-017 generic family graph panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-017 family graph updater refused or rolled back: {result}.");
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
            FamilyRosterPanel[] rosters = Components<FamilyRosterPanel>(scene);
            M2EndingPanel[] endings = Components<M2EndingPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || rosters.Length != 1 ||
                endings.Length != 1 || roots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, roster panel, ending panel and roster root.");

            Transform root = roots[0];
            Transform open = Require(scene, OpenButtonName);
            Transform panel = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform legend = Require(scene, LegendName);
            Transform scroll = Require(scene, ScrollName);
            Transform content = Require(scene, ContentName);
            Transform edgeLayer = Require(scene, EdgeLayerName);
            Transform nodeLayer = Require(scene, NodeLayerName);
            Transform nodeTemplate = Require(scene, NodeTemplateName);
            Transform edgeTemplate = Require(scene, EdgeTemplateName);
            Transform sectionTemplate = Require(scene, SectionTemplateName);
            Transform heir = Require(scene, HeirName);
            Transform close = Require(scene, CloseButtonName);
            Transform ending = Require(scene, EndingPanelName);
            Transform endingTitle = Require(scene, EndingTitleName);
            Transform endingBody = Require(scene, EndingBodyName);
            Transform endingClose = Require(scene, EndingCloseName);
            if (open == null || panel == null || title == null || legend == null || scroll == null ||
                content == null || edgeLayer == null || nodeLayer == null || nodeTemplate == null ||
                edgeTemplate == null || sectionTemplate == null || heir == null || close == null ||
                ending == null || endingTitle == null || endingBody == null || endingClose == null)
                return Invalid(out error, "SC-017 roster or ending hierarchy is incomplete or ambiguous.");

            if (root.parent != canvases[0] || rosters[0].transform != root || endings[0].transform != root ||
                !root.gameObject.activeSelf)
                return Invalid(out error,
                    "FamilyRosterRoot must be active, be a direct Canvas child, and own both components.");

            Transform canvas = canvases[0];
            Transform mapRoot = canvas.Find(MapRootName);
            Transform titleRoot = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (mapRoot != null && root.GetSiblingIndex() <= mapRoot.GetSiblingIndex())
                return Invalid(out error,
                    "FamilyRosterRoot must render above the map/town HUD so its buttons receive raycasts.");
            if (titleRoot != null && root.GetSiblingIndex() >= titleRoot.GetSiblingIndex())
                return Invalid(out error, "FamilyRosterRoot must render below the title overlay.");
            if (eventRoot != null && root.GetSiblingIndex() >= eventRoot.GetSiblingIndex())
                return Invalid(out error, "FamilyRosterRoot must render below the event-choice overlay.");

            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error, "The roster open button must be an active direct child of the root.");
            if (panel.parent != root || panel.gameObject.activeSelf)
                return Invalid(out error, "FamilyRosterPanel must be a hidden direct child of the root.");
            if (ending.parent != root || ending.gameObject.activeSelf)
                return Invalid(out error, "M2EndingPanel must be a hidden direct child of the root.");
            if (title.parent != panel || legend.parent != panel || scroll.parent != panel ||
                heir.parent != panel || close.parent != panel)
                return Invalid(out error,
                    "Roster title, legend, scroll area, heir line and close button must be direct panel children.");
            if (content.parent != scroll || edgeLayer.parent != content || nodeLayer.parent != content ||
                sectionTemplate.parent != content)
                return Invalid(out error,
                    "Scroll content must own the edge layer, node layer and section-label template.");
            if (edgeTemplate.parent != edgeLayer || nodeTemplate.parent != nodeLayer)
                return Invalid(out error,
                    "Edge and node templates must be inactive children of their own layers.");
            if (edgeTemplate.gameObject.activeSelf || nodeTemplate.gameObject.activeSelf ||
                sectionTemplate.gameObject.activeSelf)
                return Invalid(out error, "All generated graph templates must be inactive.");
            if (endingTitle.parent != ending || endingBody.parent != ending || endingClose.parent != ending)
                return Invalid(out error,
                    "Ending title, body and close button must be direct ending-panel children.");

            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            RectTransform contentRect = content as RectTransform;
            RectTransform scrollRectTransform = scroll as RectTransform;
            if (scrollRect == null || contentRect == null || scrollRectTransform == null ||
                scrollRect.content != contentRect || scrollRect.viewport != scrollRectTransform ||
                !scrollRect.vertical || !scrollRect.horizontal)
                return Invalid(out error,
                    "The roster scroll area must own a two-axis ScrollRect whose content is FamilyRosterContent.");
            if (scroll.GetComponent<RectMask2D>() == null)
                return Invalid(out error, "The roster scroll area must clip its generated content.");

            Text titleText = title.GetComponent<Text>();
            Text legendText = legend.GetComponent<Text>();
            Text heirText = heir.GetComponent<Text>();
            Text sectionText = sectionTemplate.GetComponent<Text>();
            Text nodeText = nodeTemplate.GetComponentInChildren<Text>(true);
            Text endingTitleText = endingTitle.GetComponent<Text>();
            Text endingBodyText = endingBody.GetComponent<Text>();
            Button openButton = open.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            Button endingCloseButton = endingClose.GetComponent<Button>();
            if (titleText == null || legendText == null || heirText == null || sectionText == null ||
                nodeText == null || endingTitleText == null || endingBodyText == null ||
                titleText.font == null || legendText.font == null || heirText.font == null ||
                sectionText.font == null || nodeText.font == null ||
                endingTitleText.font == null || endingBodyText.font == null ||
                nodeTemplate.GetComponent<Image>() == null ||
                edgeTemplate.GetComponent<Image>() == null ||
                openButton == null || closeButton == null || endingCloseButton == null ||
                !HasButtonVisual(openButton) || !HasButtonVisual(closeButton) ||
                !HasButtonVisual(endingCloseButton) ||
                !HasRaycastTarget(openButton) || !HasRaycastTarget(closeButton) ||
                !HasRaycastTarget(endingCloseButton) ||
                panel.GetComponent<Image>() == null || !panel.GetComponent<Image>().raycastTarget ||
                scroll.GetComponent<Image>() == null || !scroll.GetComponent<Image>().raycastTarget ||
                ending.GetComponent<Image>() == null || !ending.GetComponent<Image>().raycastTarget)
                return Invalid(out error,
                    "Roster/ending text, button, background, mask or raycast components are invalid.");

            FamilyRosterPanel roster = rosters[0];
            if (!Bound(roster, "gameSession", sessions[0]) ||
                !Bound(roster, "panelRoot", panel.gameObject) ||
                !Bound(roster, "openButton", openButton) ||
                !Bound(roster, "closeButton", closeButton) ||
                !Bound(roster, "titleText", titleText) ||
                !Bound(roster, "legendText", legendText) ||
                !Bound(roster, "scrollRect", scrollRect) ||
                !Bound(roster, "content", content) ||
                !Bound(roster, "edgeLayer", edgeLayer) ||
                !Bound(roster, "nodeLayer", nodeLayer) ||
                !Bound(roster, "nodeTemplate", nodeTemplate.gameObject) ||
                !Bound(roster, "edgeTemplate", edgeTemplate.gameObject) ||
                !Bound(roster, "sectionTemplate", sectionText) ||
                !Bound(roster, "heirText", heirText))
                return Invalid(out error, "FamilyRosterPanel serialized references are not bound.");

            M2EndingPanel endingPanel = endings[0];
            if (!Bound(endingPanel, "gameSession", sessions[0]) ||
                !Bound(endingPanel, "panelRoot", ending.gameObject) ||
                !Bound(endingPanel, "titleText", endingTitleText) ||
                !Bound(endingPanel, "bodyText", endingBodyText) ||
                !Bound(endingPanel, "closeButton", endingCloseButton))
                return Invalid(out error, "M2EndingPanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform panelRect = panel as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform titleRect = title as RectTransform;
            RectTransform legendRect = legend as RectTransform;
            RectTransform scrollAreaRect = scroll as RectTransform;
            RectTransform heirRect = heir as RectTransform;
            RectTransform closeRect = close as RectTransform;
            RectTransform endingRect = ending as RectTransform;
            RectTransform endingTitleRect = endingTitle as RectTransform;
            RectTransform endingBodyRect = endingBody as RectTransform;
            RectTransform endingCloseRect = endingClose as RectTransform;
            if (rootRect == null || panelRect == null || openRect == null || titleRect == null ||
                legendRect == null || scrollAreaRect == null || heirRect == null || closeRect == null ||
                endingRect == null || endingTitleRect == null || endingBodyRect == null ||
                endingCloseRect == null ||
                !Contained(panelRect, rootRect) || !Contained(openRect, rootRect) ||
                !Contained(endingRect, rootRect) ||
                !Contained(titleRect, panelRect) || !Contained(legendRect, panelRect) ||
                !Contained(scrollAreaRect, panelRect) ||
                !Contained(heirRect, panelRect) || !Contained(closeRect, panelRect) ||
                Overlaps(titleRect, legendRect, panelRect) ||
                Overlaps(legendRect, scrollAreaRect, panelRect) ||
                Overlaps(scrollAreaRect, heirRect, panelRect) ||
                Overlaps(heirRect, closeRect, panelRect) ||
                !Contained(endingTitleRect, endingRect) || !Contained(endingBodyRect, endingRect) ||
                !Contained(endingCloseRect, endingRect) ||
                Overlaps(endingTitleRect, endingBodyRect, endingRect) ||
                Overlaps(endingBodyRect, endingCloseRect, endingRect))
                return Invalid(out error, "Roster or ending controls overlap or lie outside their bounds.");
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
            if (canvas == null || session == null ||
                Components<FamilyRosterPanel>(scene).Length > 1 ||
                Components<M2EndingPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            Transform legacyRoot = FindLegacyHierarchy(scene);
            if (legacyRoot == null && HasAnyRelevantObject(scene))
                return UpdateResult.ExistingPartialControl;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                if (legacyRoot != null)
                    Undo.DestroyObjectImmediate(legacyRoot.gameObject);

                GameObject rootObject = Create(scene, canvas, RootName);
                PlaceAboveMapHud(canvas, rootObject.transform);
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "家族名册",
                    0f, 0f, 200f, 44f, 16);
                RectTransform openRect = open.GetComponent<RectTransform>();
                openRect.anchorMin = openRect.anchorMax = openRect.pivot = new Vector2(1f, 1f);
                openRect.anchoredPosition = new Vector2(-240f, -20f);

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(760f, 620f);
                panelObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font, "家族名册",
                    0f, 280f, 720f, 36f, 22, TextAnchor.MiddleLeft);
                Text legend = CreateText(scene, panelRect, LegendName, font,
                    "家谱连线：亲子 金 · 收养 紫 · 手足 绿 · 配偶 蓝；未连接亲族与家臣分区列出。",
                    0f, 246f, 720f, 22f, 13, TextAnchor.MiddleLeft);

                GameObject scrollObject = Create(scene, panelRect, ScrollName,
                    typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
                RectTransform scrollAreaRect = scrollObject.GetComponent<RectTransform>();
                scrollAreaRect.anchorMin = scrollAreaRect.anchorMax = scrollAreaRect.pivot =
                    new Vector2(0.5f, 0.5f);
                scrollAreaRect.anchoredPosition = new Vector2(0f, 5f);
                scrollAreaRect.sizeDelta = new Vector2(700f, 410f);
                scrollObject.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.9f);
                ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
                scrollRect.horizontal = true;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.viewport = scrollAreaRect;

                GameObject contentObject = Create(scene, scrollAreaRect, ContentName);
                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = contentRect.anchorMax = new Vector2(0f, 1f);
                contentRect.pivot = new Vector2(0f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(700f, 600f);
                scrollRect.content = contentRect;

                GameObject edgeLayerObject = Create(scene, contentRect, EdgeLayerName);
                RectTransform edgeLayerRect = edgeLayerObject.GetComponent<RectTransform>();
                Stretch(edgeLayerRect);

                GameObject nodeLayerObject = Create(scene, contentRect, NodeLayerName);
                RectTransform nodeLayerRect = nodeLayerObject.GetComponent<RectTransform>();
                Stretch(nodeLayerRect);

                GameObject edgeTemplateObject = Create(scene, edgeLayerRect, EdgeTemplateName,
                    typeof(Image));
                RectTransform edgeTemplateRect = edgeTemplateObject.GetComponent<RectTransform>();
                edgeTemplateRect.anchorMin = edgeTemplateRect.anchorMax =
                    edgeTemplateRect.pivot = new Vector2(0.5f, 0.5f);
                edgeTemplateRect.sizeDelta = new Vector2(20f, 3f);
                Image edgeImage = edgeTemplateObject.GetComponent<Image>();
                edgeImage.color = Color.white;
                edgeImage.raycastTarget = false;
                edgeTemplateObject.SetActive(false);

                GameObject nodeTemplateObject = Create(scene, nodeLayerRect, NodeTemplateName,
                    typeof(Image));
                RectTransform nodeTemplateRect = nodeTemplateObject.GetComponent<RectTransform>();
                nodeTemplateRect.anchorMin = nodeTemplateRect.anchorMax =
                    nodeTemplateRect.pivot = new Vector2(0f, 1f);
                nodeTemplateRect.sizeDelta = new Vector2(FamilyTreeLayout.NodeWidth,
                    FamilyTreeLayout.NodeHeight);
                Image nodeImage = nodeTemplateObject.GetComponent<Image>();
                nodeImage.color = new Color(0.16f, 0.22f, 0.3f, 1f);
                nodeImage.raycastTarget = false;
                GameObject nodeLabelObject = Create(scene, nodeTemplateRect, "Label", typeof(Text));
                RectTransform nodeLabelRect = nodeLabelObject.GetComponent<RectTransform>();
                nodeLabelRect.anchorMin = Vector2.zero;
                nodeLabelRect.anchorMax = Vector2.one;
                nodeLabelRect.offsetMin = new Vector2(10f, 8f);
                nodeLabelRect.offsetMax = new Vector2(-10f, -8f);
                Text nodeLabel = nodeLabelObject.GetComponent<Text>();
                nodeLabel.font = font;
                nodeLabel.fontSize = 14;
                nodeLabel.color = Color.white;
                nodeLabel.alignment = TextAnchor.UpperLeft;
                nodeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                nodeLabel.verticalOverflow = VerticalWrapMode.Overflow;
                nodeLabel.raycastTarget = false;
                nodeTemplateObject.SetActive(false);

                Text sectionTemplate = CreateText(scene, contentRect, SectionTemplateName, font,
                    string.Empty, 0f, 0f, 360f, 28f, 15, TextAnchor.MiddleLeft);
                sectionTemplate.color = new Color(0.86f, 0.86f, 0.78f, 1f);
                sectionTemplate.gameObject.SetActive(false);

                Text heir = CreateText(scene, panelRect, HeirName, font, "固定继承人：",
                    0f, -225f, 700f, 34f, 16, TextAnchor.MiddleLeft);

                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    0f, -283f, 170f, 42f, 16);

                GameObject endingObject = Create(scene, rootRect, EndingPanelName, typeof(Image));
                RectTransform endingRect = endingObject.GetComponent<RectTransform>();
                endingRect.anchorMin = endingRect.anchorMax = endingRect.pivot = new Vector2(0.5f, 0.5f);
                endingRect.anchoredPosition = Vector2.zero;
                endingRect.sizeDelta = new Vector2(760f, 420f);
                endingObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                endingObject.SetActive(false);

                Text endingTitle = CreateText(scene, endingRect, EndingTitleName, font, "第三代结局",
                    0f, 165f, 700f, 40f, 22, TextAnchor.MiddleCenter);
                Text endingBody = CreateText(scene, endingRect, EndingBodyName, font, string.Empty,
                    0f, 10f, 700f, 240f, 16, TextAnchor.UpperLeft);
                Button endingClose = CreateButton(scene, endingRect, EndingCloseName, font, "继续游玩",
                    0f, -170f, 200f, 44f, 16);

                FamilyRosterPanel roster = Undo.AddComponent<FamilyRosterPanel>(rootObject);
                Undo.RecordObject(roster, UndoName);
                roster.BindReferences(session, panelObject, open, close, title, legend, scrollRect,
                    contentRect, edgeLayerRect, nodeLayerRect, nodeTemplateObject, edgeTemplateObject,
                    sectionTemplate, heir);
                SetRosterSerializedReferences(roster, session, panelObject, open, close, title,
                    legend, scrollRect, contentRect, edgeLayerRect, nodeLayerRect, nodeTemplateObject,
                    edgeTemplateObject, sectionTemplate, heir);
                EditorUtility.SetDirty(roster);

                M2EndingPanel endingPanel = Undo.AddComponent<M2EndingPanel>(rootObject);
                Undo.RecordObject(endingPanel, UndoName);
                endingPanel.BindReferences(session, endingObject, endingTitle, endingBody, endingClose);
                SetEndingSerializedReferences(endingPanel, session, endingObject, endingTitle,
                    endingBody, endingClose);
                EditorUtility.SetDirty(endingPanel);

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
                Debug.LogWarning($"SC-017 family graph updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        /// <summary>
        /// Returns the one legacy SC-013 row hierarchy when its structure is exactly the
        /// previous generated shape. Any other existing roster object is treated as an
        /// ambiguous partial control and must not be overwritten.
        /// </summary>
        private static Transform FindLegacyHierarchy(Scene scene)
        {
            Transform[] roots = Named(scene, RootName).ToArray();
            if (roots.Length != 1)
                return null;
            Transform root = roots[0];
            if (root.GetComponent<FamilyRosterPanel>() == null ||
                root.GetComponent<M2EndingPanel>() == null)
                return null;

            Transform open = Require(scene, OpenButtonName);
            Transform panel = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform content = Require(scene, ContentName);
            Transform template = Require(scene, LegacyRowTemplateName);
            Transform heir = Require(scene, HeirName);
            Transform close = Require(scene, CloseButtonName);
            Transform ending = Require(scene, EndingPanelName);
            Transform endingTitle = Require(scene, EndingTitleName);
            Transform endingBody = Require(scene, EndingBodyName);
            Transform endingClose = Require(scene, EndingCloseName);
            if (open == null || panel == null || title == null || content == null ||
                template == null || heir == null || close == null || ending == null ||
                endingTitle == null || endingBody == null || endingClose == null)
                return null;
            if (Named(scene, ScrollName).Any() || Named(scene, LegendName).Any() ||
                Named(scene, EdgeLayerName).Any() || Named(scene, NodeLayerName).Any() ||
                Named(scene, NodeTemplateName).Any() || Named(scene, EdgeTemplateName).Any() ||
                Named(scene, SectionTemplateName).Any())
                return null;
            if (open.parent != root || panel.parent != root || title.parent != panel ||
                content.parent != panel || template.parent != content || heir.parent != panel ||
                close.parent != panel || ending.parent != root || endingTitle.parent != ending ||
                endingBody.parent != ending || endingClose.parent != ending)
                return null;
            if (content.GetComponent<VerticalLayoutGroup>() == null ||
                template.GetComponent<Text>() == null || ending.GetComponent<Image>() == null)
                return null;
            if (!HasRecognizedLegacyComponents(root, open, panel, title, content, template,
                    heir, close, ending, endingTitle, endingBody, endingClose))
                return null;
            return root;
        }

        /// <summary>
        /// The legacy root may only be destroyed when every descendant and every component
        /// belongs to the hierarchy the previous SC-013 generator produced. Anything else
        /// (a user-added child, a custom component, another graphic) is treated as
        /// unrecognized data and must be preserved by refusing migration.
        /// </summary>
        private static bool HasRecognizedLegacyComponents(Transform root, Transform open,
            Transform panel, Transform title, Transform content, Transform template,
            Transform heir, Transform close, Transform ending, Transform endingTitle,
            Transform endingBody, Transform endingClose)
        {
            if (!HasOnlyComponents(root, typeof(FamilyRosterPanel), typeof(M2EndingPanel)) ||
                !HasOnlyComponents(open, typeof(Image), typeof(Button), typeof(LayoutElement)) ||
                !HasOnlyComponents(panel, typeof(Image)) ||
                !HasOnlyComponents(title, typeof(Text)) ||
                !HasOnlyComponents(content, typeof(VerticalLayoutGroup)) ||
                !HasOnlyComponents(template, typeof(Text), typeof(LayoutElement)) ||
                !HasOnlyComponents(heir, typeof(Text)) ||
                !HasOnlyComponents(close, typeof(Image), typeof(Button),
                    typeof(LayoutElement)) ||
                !HasOnlyComponents(ending, typeof(Image)) ||
                !HasOnlyComponents(endingTitle, typeof(Text)) ||
                !HasOnlyComponents(endingBody, typeof(Text)) ||
                !HasOnlyComponents(endingClose, typeof(Image), typeof(Button),
                    typeof(LayoutElement)))
                return false;

            if (open.GetComponent<Image>() == null || open.GetComponent<Button>() == null ||
                panel.GetComponent<Image>() == null || title.GetComponent<Text>() == null ||
                content.GetComponent<VerticalLayoutGroup>() == null ||
                template.GetComponent<Text>() == null || heir.GetComponent<Text>() == null ||
                close.GetComponent<Image>() == null || close.GetComponent<Button>() == null ||
                ending.GetComponent<Image>() == null ||
                endingTitle.GetComponent<Text>() == null ||
                endingBody.GetComponent<Text>() == null ||
                endingClose.GetComponent<Image>() == null ||
                endingClose.GetComponent<Button>() == null)
                return false;

            var known = new HashSet<Transform>
            {
                root, open, panel, title, content, template, heir, close, ending,
                endingTitle, endingBody, endingClose
            };
            var labels = new Dictionary<Transform, int>();
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform current = all[i];
                if (known.Contains(current))
                    continue;
                Transform parent = current.parent;
                if (current.name != LegacyButtonLabelName ||
                    (parent != open && parent != close && parent != endingClose) ||
                    !HasOnlyComponents(current, typeof(Text)) ||
                    current.GetComponent<Text>() == null)
                    return false;
                labels.TryGetValue(parent, out int labelCount);
                if (labelCount >= 1)
                    return false;
                labels[parent] = labelCount + 1;
            }
            return true;
        }

        private static bool HasOnlyComponents(Transform transform, params Type[] expected)
        {
            Component[] components = transform.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Type type = components[i].GetType();
                bool allowed = type == typeof(Transform) || type == typeof(RectTransform) ||
                               type == typeof(CanvasRenderer);
                if (!allowed)
                {
                    for (int j = 0; j < expected.Length; j++)
                    {
                        if (type != expected[j])
                            continue;
                        allowed = true;
                        break;
                    }
                }
                if (!allowed)
                    return false;
            }
            return true;
        }

        private static bool HasAnyRelevantObject(Scene scene)
        {
            return Named(scene, RootName).Any() ||
                   Named(scene, OpenButtonName).Any() ||
                   Named(scene, PanelName).Any() ||
                   Named(scene, TitleName).Any() ||
                   Named(scene, LegendName).Any() ||
                   Named(scene, ScrollName).Any() ||
                   Named(scene, ContentName).Any() ||
                   Named(scene, EdgeLayerName).Any() ||
                   Named(scene, NodeLayerName).Any() ||
                   Named(scene, NodeTemplateName).Any() ||
                   Named(scene, EdgeTemplateName).Any() ||
                   Named(scene, SectionTemplateName).Any() ||
                   Named(scene, LegacyRowTemplateName).Any() ||
                   Named(scene, HeirName).Any() ||
                   Named(scene, CloseButtonName).Any() ||
                   Named(scene, EndingPanelName).Any() ||
                   Named(scene, EndingTitleName).Any() ||
                   Named(scene, EndingBodyName).Any() ||
                   Named(scene, EndingCloseName).Any() ||
                   Components<FamilyRosterPanel>(scene).Length != 0 ||
                   Components<M2EndingPanel>(scene).Length != 0;
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRosterSerializedReferences(FamilyRosterPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Text title, Text legend,
            ScrollRect scroll, Transform content, RectTransform edgeLayer, RectTransform nodeLayer,
            GameObject nodeTemplate, GameObject edgeTemplate, Text sectionTemplate, Text heir)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("legendText").objectReferenceValue = legend;
            serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
            serialized.FindProperty("content").objectReferenceValue = content;
            serialized.FindProperty("edgeLayer").objectReferenceValue = edgeLayer;
            serialized.FindProperty("nodeLayer").objectReferenceValue = nodeLayer;
            serialized.FindProperty("nodeTemplate").objectReferenceValue = nodeTemplate;
            serialized.FindProperty("edgeTemplate").objectReferenceValue = edgeTemplate;
            serialized.FindProperty("sectionTemplate").objectReferenceValue = sectionTemplate;
            serialized.FindProperty("heirText").objectReferenceValue = heir;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEndingSerializedReferences(M2EndingPanel panel, GameSession session,
            GameObject panelRoot, Text title, Text body, Button close)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
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
