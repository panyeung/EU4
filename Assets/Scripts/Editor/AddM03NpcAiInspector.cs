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
    /// Transactional FoundationScene updater for the M03 read-only NPC Inspector. Every
    /// generated object lives beneath one fixed root in a single Undo group; validation
    /// runs before saving, and any failure reverts the group without reloading the scene.
    /// </summary>
    public static class AddM03NpcAiInspector
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "M03NpcAiInspectorRoot";
        public const string OpenButtonName = "M03NpcAiInspectorOpenButton";
        public const string PanelName = "M03NpcAiInspectorPanel";
        public const string TitleName = "M03NpcAiInspectorTitleText";
        public const string SummaryName = "M03NpcAiInspectorSummaryText";
        public const string MasatsunaButtonName = "M03NpcAiInspectorMasatsunaButton";
        public const string NobumasaButtonName = "M03NpcAiInspectorNobumasaButton";
        public const string NeedsName = "M03NpcAiInspectorNeedsText";
        public const string StatusName = "M03NpcAiInspectorStatusText";
        public const string CandidatesName = "M03NpcAiInspectorCandidatesText";
        public const string LogName = "M03NpcAiInspectorLogText";
        public const string ScrollName = "M03NpcAiInspectorScroll";
        public const string ViewportName = "M03NpcAiInspectorViewport";
        public const string ContentName = "M03NpcAiInspectorContent";
        public const string CloseButtonName = "M03NpcAiInspectorCloseButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add M03 NPC AI Inspector";
        public const string DirtyMenuPath =
            "Tools/Sengoku Chronicle/Integrate M03 NPC AI Inspector (dirty workspace)";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string IndustryRootName = "IndustryRoot";
        private const string EducationRootName = "SC020EducationRoot";
        private const string ResearchRootName = "SC021ResearchRoot";
        private const string FormationRootName = "SC022FamilyFormationRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventRootName = "EventChoiceRoot";
        private const string UndoName = "Add M03 NPC AI Inspector";

        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, -606f);
        private static readonly Vector2 OpenButtonSize = new Vector2(220f, 44f);
        private static readonly Vector2 PanelSize = new Vector2(980f, 700f);

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, ExistingPartialControl, ValidationFailed,
            OriginalObjectsChanged, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty,
                EditorApplication.isPlayingOrWillChangePlaymode);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            Report(result, "M03 NPC AI Inspector");
        }

        /// <summary>
        /// Explicit integration entry for the authorized dirty shared workspace. It keeps
        /// every structural/preservation check; it only skips the clean-scene convenience
        /// gate because the existing workspace scene may already be dirty.
        /// </summary>
        [MenuItem(DirtyMenuPath)]
        public static void IntegrateDirtyWorkspace()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                Report(EditorApplication.isPlayingOrWillChangePlaymode
                    ? UpdateResult.EditorPlaying
                    : UpdateResult.SceneMismatch, "M03 NPC AI Inspector");
                return;
            }
            Report(Execute(scene, true, null), "M03 NPC AI Inspector (dirty workspace)");
        }

        private static void Report(UpdateResult result, string label)
        {
            if (result == UpdateResult.Success)
                Debug.Log($"{label} validated and saved into FoundationScene.");
            else
                Debug.LogError($"{label} updater refused or rolled back: {result}.");
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
            NpcAiInspectorPanel[] panels = Components<NpcAiInspectorPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            Transform[] opens = Named(scene, OpenButtonName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 ||
                roots.Length != 1 || opens.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, NpcAiInspectorPanel, root, and open button.");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform open = opens[0];
            NpcAiInspectorPanel panel = panels[0];
            if (root.parent != canvas || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "M03NpcAiInspectorRoot must be an active direct child of Canvas.");
            if (panel.transform != root || panel.gameObject != root.gameObject)
                return Invalid(out error, "NpcAiInspectorPanel must live on M03NpcAiInspectorRoot.");
            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error,
                    "M03NpcAiInspectorOpenButton must be an active direct child of the root.");
            if (!HasButtonVisual(open.GetComponent<Button>()) ||
                !HasRaycastTarget(open.GetComponent<Button>()))
                return Invalid(out error, "The M03 Inspector open button is invalid.");

            Transform panelRoot = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform summary = Require(scene, SummaryName);
            Transform masatsuna = Require(scene, MasatsunaButtonName);
            Transform nobumasa = Require(scene, NobumasaButtonName);
            Transform needs = Require(scene, NeedsName);
            Transform status = Require(scene, StatusName);
            Transform candidates = Require(scene, CandidatesName);
            Transform log = Require(scene, LogName);
            Transform scroll = Require(scene, ScrollName);
            Transform viewport = Require(scene, ViewportName);
            Transform content = Require(scene, ContentName);
            Transform close = Require(scene, CloseButtonName);
            if (panelRoot == null || title == null || summary == null || masatsuna == null ||
                nobumasa == null || needs == null || status == null || candidates == null ||
                log == null || scroll == null || viewport == null || content == null ||
                close == null)
                return Invalid(out error, "M03 Inspector hierarchy is incomplete or ambiguous.");

            if (panelRoot.parent != root || panelRoot.gameObject.activeSelf)
                return Invalid(out error,
                    "M03NpcAiInspectorPanel must be a hidden direct child of the root.");
            Transform[] panelChildren =
            {
                title, summary, masatsuna, nobumasa, needs, status, scroll, close
            };
            foreach (Transform child in panelChildren)
                if (child.parent != panelRoot)
                    return Invalid(out error,
                        "All M03 Inspector controls must be direct children of the panel.");
            if (viewport.parent != scroll || content.parent != viewport)
                return Invalid(out error,
                    "The M03 diagnostics viewport and content must nest beneath the scroll area.");
            if (candidates.parent != content || log.parent != content)
                return Invalid(out error,
                    "M03 candidate and decision-log text must live inside the scroll content.");

            Text titleText = title.GetComponent<Text>();
            Text summaryText = summary.GetComponent<Text>();
            Text needsText = needs.GetComponent<Text>();
            Text statusText = status.GetComponent<Text>();
            Text candidatesText = candidates.GetComponent<Text>();
            Text logText = log.GetComponent<Text>();
            Button masatsunaButton = masatsuna.GetComponent<Button>();
            Button nobumasaButton = nobumasa.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            RectMask2D mask = viewport.GetComponent<RectMask2D>();
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (titleText == null || summaryText == null || needsText == null ||
                statusText == null || candidatesText == null || logText == null ||
                masatsunaButton == null || nobumasaButton == null || closeButton == null ||
                titleText.font == null || summaryText.font == null || needsText.font == null ||
                statusText.font == null || candidatesText.font == null || logText.font == null ||
                !HasButtonVisual(masatsunaButton) || !HasButtonVisual(nobumasaButton) ||
                !HasButtonVisual(closeButton) || !HasRaycastTarget(masatsunaButton) ||
                !HasRaycastTarget(nobumasaButton) || !HasRaycastTarget(closeButton) ||
                panelRoot.GetComponent<Image>() == null ||
                !panelRoot.GetComponent<Image>().raycastTarget)
                return Invalid(out error,
                    "M03 text, button, background, or raycast components are invalid.");
            if (scrollRect == null || mask == null || layout == null || fitter == null ||
                scrollRect.content != (RectTransform)content ||
                scrollRect.viewport != (RectTransform)viewport ||
                !scrollRect.vertical || scrollRect.horizontal ||
                fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                return Invalid(out error,
                    "M03 diagnostics must use a vertical ScrollRect with a masked viewport, " +
                    "vertical layout and preferred-size content.");
            if (!titleText.text.Contains("NPC"))
                return Invalid(out error, "The M03 title must contain NPC.");
            if (!summaryText.text.Contains("只读"))
                return Invalid(out error, "The M03 summary must state that the panel is read-only.");

            if (!Bound(panel, "gameSession", sessions[0]) ||
                !Bound(panel, "panelRoot", panelRoot.gameObject) ||
                !Bound(panel, "openButton", open.GetComponent<Button>()) ||
                !Bound(panel, "closeButton", closeButton) ||
                !Bound(panel, "masatsunaButton", masatsunaButton) ||
                !Bound(panel, "nobumasaButton", nobumasaButton) ||
                !Bound(panel, "titleText", titleText) ||
                !Bound(panel, "summaryText", summaryText) ||
                !Bound(panel, "needsText", needsText) ||
                !Bound(panel, "statusText", statusText) ||
                !Bound(panel, "candidatesText", candidatesText) ||
                !Bound(panel, "logText", logText) ||
                !Bound(panel, "diagnosticsScroll", scrollRect) ||
                !Bound(panel, "diagnosticsContent", (RectTransform)content))
                return Invalid(out error, "NpcAiInspectorPanel serialized references are not bound.");

            RectTransform canvasRect = canvas as RectTransform;
            RectTransform panelRect = panelRoot as RectTransform;
            RectTransform openRect = open as RectTransform;
            if (canvasRect == null || panelRect == null || openRect == null ||
                !Contained(panelRect, canvasRect) || !Contained(openRect, canvasRect))
                return Invalid(out error, "The M03 panel or open button lies outside the Canvas.");

            RectTransform[] children =
            {
                title as RectTransform, summary as RectTransform, masatsuna as RectTransform,
                nobumasa as RectTransform, needs as RectTransform, status as RectTransform,
                scroll as RectTransform, close as RectTransform
            };
            foreach (RectTransform child in children)
                if (!Contained(child, panelRect))
                    return Invalid(out error,
                        "An M03 Inspector control lies outside the panel bounds.");
            if (!Contained(viewport as RectTransform, scroll as RectTransform))
                return Invalid(out error,
                    "The M03 diagnostics viewport lies outside the scroll area.");

            if (!ContainedInRenderBand(canvas, root))
                return Invalid(out error,
                    "M03NpcAiInspectorRoot must render above the map HUD and below the title/event overlays.");
            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save,
            Action<GameObject> beforeValidation)
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
                Components<NpcAiInspectorPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            Transform[] existingRoots = Named(scene, RootName).ToArray();
            if (existingRoots.Length > 1)
                return UpdateResult.ExistingPartialControl;
            Transform existingRoot = existingRoots.Length == 1 ? existingRoots[0] : null;
            if (existingRoot != null && !IsReplaceableGeneratedRoot(scene, existingRoot))
                return UpdateResult.ExistingPartialControl;
            if (existingRoot == null && HasAnyM03Object(scene))
                return UpdateResult.ExistingPartialControl;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                if (existingRoot != null)
                    Undo.DestroyObjectImmediate(existingRoot.gameObject);
                ObjectSnapshot[] originals = Snapshot(scene);
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject rootObject = Create(scene, canvas, RootName);
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Button open = CreateButton(scene, rootRect, OpenButtonName, font,
                    "NPC 自主性", OpenButtonSize.x, OpenButtonSize.y, 16);
                SetTopLeftRect(open.transform, OpenButtonPosition, OpenButtonSize);

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot =
                    new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = PanelSize;
                Image background = panelObject.GetComponent<Image>();
                background.color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                background.raycastTarget = true;
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font,
                    "NPC 自主性（只读）", 0f, 318f, 920f, 34f, 22, TextAnchor.MiddleLeft);
                Text summary = CreateText(scene, panelRect, SummaryName, font,
                    "只读面板：展示需求、候选分数或排除原因、当前行动与最近决策。",
                    0f, 282f, 920f, 28f, 14, TextAnchor.MiddleLeft);
                summary.color = new Color(0.82f, 0.88f, 0.95f, 1f);

                Button masatsuna = CreateButton(scene, panelRect, MasatsunaButtonName, font,
                    "明智 正綱", 240f, 40f, 16);
                SetCenteredRect(masatsuna.transform, new Vector2(-130f, 238f),
                    new Vector2(240f, 40f));
                Button nobumasa = CreateButton(scene, panelRect, NobumasaButtonName, font,
                    "明智 信正", 240f, 40f, 16);
                SetCenteredRect(nobumasa.transform, new Vector2(130f, 238f),
                    new Vector2(240f, 40f));

                Text needs = CreateText(scene, panelRect, NeedsName, font,
                    "需求（0..100）：休息 —／学艺 —／家务 —", 0f, 186f, 920f, 50f, 15,
                    TextAnchor.MiddleLeft);
                Text status = CreateText(scene, panelRect, StatusName, font, "状态：空闲",
                    0f, 142f, 920f, 30f, 15, TextAnchor.MiddleLeft);

                GameObject scrollObject = Create(scene, panelRect, ScrollName,
                    typeof(Image), typeof(ScrollRect));
                RectTransform scrollRectTransform =
                    scrollObject.GetComponent<RectTransform>();
                scrollRectTransform.anchorMin = scrollRectTransform.anchorMax =
                    scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
                scrollRectTransform.anchoredPosition = new Vector2(0f, -42f);
                scrollRectTransform.sizeDelta = new Vector2(920f, 330f);
                Image scrollBackground = scrollObject.GetComponent<Image>();
                scrollBackground.color = new Color(0.05f, 0.07f, 0.1f, 1f);
                scrollBackground.raycastTarget = true;

                GameObject viewportObject = Create(scene, scrollRectTransform, ViewportName,
                    typeof(Image), typeof(RectMask2D));
                RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = new Vector2(6f, 6f);
                viewportRect.offsetMax = new Vector2(-6f, -6f);
                Image viewportImage = viewportObject.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
                viewportImage.raycastTarget = true;

                GameObject contentObject = Create(scene, viewportRect, ContentName,
                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                VerticalLayoutGroup contentLayout =
                    contentObject.GetComponent<VerticalLayoutGroup>();
                contentLayout.padding = new RectOffset(8, 8, 8, 8);
                contentLayout.spacing = 6f;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
                ContentSizeFitter contentFitter =
                    contentObject.GetComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
                scroll.content = contentRect;
                scroll.viewport = viewportRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 24f;

                Text candidates = CreateText(scene, contentRect, CandidatesName, font,
                    "最近目标评估：暂无。\n当前诊断：暂无。", 0f, 0f, 880f, 180f, 13,
                    TextAnchor.UpperLeft);
                candidates.verticalOverflow = VerticalWrapMode.Overflow;
                Text log = CreateText(scene, contentRect, LogName, font, "暂无决策记录。",
                    0f, 0f, 880f, 240f, 12, TextAnchor.UpperLeft);
                log.verticalOverflow = VerticalWrapMode.Overflow;
                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    160f, 32f, 15);
                SetCenteredRect(close.transform, new Vector2(0f, -320f), new Vector2(160f, 32f));

                PlaceAboveMapHud(canvas, rootObject.transform);

                NpcAiInspectorPanel panel = Undo.AddComponent<NpcAiInspectorPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, panelObject, open, close, masatsuna, nobumasa,
                    title, summary, needs, status, candidates, log, scroll, contentRect);
                SetSerializedReferences(panel, session, panelObject, open, close, masatsuna,
                    nobumasa, title, summary, needs, status, candidates, log, scroll,
                    contentRect);
                EditorUtility.SetDirty(panel);

                beforeValidation?.Invoke(rootObject);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);

                failure = UpdateResult.OriginalObjectsChanged;
                if (!Preserved(scene, originals))
                    throw new InvalidOperationException(
                        "An original scene object was removed or changed.");

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
                Debug.LogWarning($"M03 NPC AI Inspector updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool ContainedInRenderBand(Transform canvas, Transform target)
        {
            int index = target.GetSiblingIndex();
            string[] above = { MapRootName, IndustryRootName, EducationRootName, ResearchRootName,
                FormationRootName };
            string[] below = { TitleRootName, EventRootName };
            foreach (string name in above)
            {
                Transform other = canvas.Find(name);
                if (other != null && index <= other.GetSiblingIndex())
                    return false;
            }
            foreach (string name in below)
            {
                Transform other = canvas.Find(name);
                if (other != null && index >= other.GetSiblingIndex())
                    return false;
            }
            return true;
        }

        private static void PlaceAboveMapHud(Transform canvas, Transform target)
        {
            Transform title = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventRootName);
            int? anchorIndex = null;
            if (title != null)
                anchorIndex = title.GetSiblingIndex();
            if (eventRoot != null)
                anchorIndex = anchorIndex.HasValue
                    ? Mathf.Min(anchorIndex.Value, eventRoot.GetSiblingIndex())
                    : eventRoot.GetSiblingIndex();
            if (anchorIndex.HasValue)
                target.SetSiblingIndex(anchorIndex.Value);
            else
                target.SetAsLastSibling();
        }

        private static readonly string[] GeneratedNames =
        {
            RootName, OpenButtonName, PanelName, TitleName, SummaryName,
            MasatsunaButtonName, NobumasaButtonName, NeedsName, StatusName,
            CandidatesName, LogName, ScrollName, ViewportName, ContentName,
            CloseButtonName
        };

        /// <summary>
        /// A previously generated M03 subtree may be replaced in one Undo transaction.
        /// All M03-named objects must live under exactly one root and the panel component
        /// must be on that root; foreign or duplicated controls are still refused.
        /// </summary>
        private static bool IsReplaceableGeneratedRoot(Scene scene, Transform root)
        {
            NpcAiInspectorPanel[] panels = Components<NpcAiInspectorPanel>(scene);
            if (panels.Length > 1)
                return false;
            if (panels.Length == 1 && panels[0].transform != root)
                return false;

            foreach (string name in GeneratedNames)
            {
                Transform[] matches = Named(scene, name).ToArray();
                if (matches.Length > 1)
                    return false;
                foreach (Transform match in matches)
                {
                    if (match == root)
                        continue;
                    if (!match.IsChildOf(root))
                        return false;
                }
            }
            return true;
        }

        private static bool HasAnyM03Object(Scene scene)
        {
            if (Components<NpcAiInspectorPanel>(scene).Length != 0)
                return true;
            foreach (string name in GeneratedNames)
                if (Named(scene, name).Any())
                    return true;
            return false;
        }

        private static bool Preserved(Scene scene, ObjectSnapshot[] originals)
        {
            foreach (ObjectSnapshot original in originals)
            {
                Transform transform = original.Transform;
                if (transform == null)
                    return false;
                if (!string.Equals(transform.name, original.Name, StringComparison.Ordinal))
                    return false;
                if (transform.gameObject.activeSelf != original.Active)
                    return false;
                if (transform.parent != original.Parent)
                    return false;
            }
            return true;
        }

        private static ObjectSnapshot[] Snapshot(Scene scene)
        {
            Transform[] all = AllTransforms(scene);
            var snapshots = new ObjectSnapshot[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                Transform transform = all[i];
                snapshots[i] = new ObjectSnapshot
                {
                    Transform = transform,
                    Name = transform.name,
                    Parent = transform.parent,
                    Active = transform.gameObject.activeSelf
                };
            }
            return snapshots;
        }

        private struct ObjectSnapshot
        {
            public Transform Transform;
            public string Name;
            public Transform Parent;
            public bool Active;
        }

        private static void SetSerializedReferences(NpcAiInspectorPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Button masatsuna, Button nobumasa,
            Text title, Text summary, Text needs, Text status, Text candidates, Text log,
            ScrollRect scroll, RectTransform content)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("masatsunaButton").objectReferenceValue = masatsuna;
            serialized.FindProperty("nobumasaButton").objectReferenceValue = nobumasa;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("summaryText").objectReferenceValue = summary;
            serialized.FindProperty("needsText").objectReferenceValue = needs;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("candidatesText").objectReferenceValue = candidates;
            serialized.FindProperty("logText").objectReferenceValue = log;
            serialized.FindProperty("diagnosticsScroll").objectReferenceValue = scroll;
            serialized.FindProperty("diagnosticsContent").objectReferenceValue = content;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Create(Scene scene, Transform parent, string name,
            params Type[] components)
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

        private static Text CreateText(Scene scene, Transform parent, string name, Font font,
            string value, float x, float y, float width, float height, int fontSize,
            TextAnchor alignment)
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
            string label, float width, float height, int fontSize)
        {
            GameObject go = Create(scene, parent, name,
                typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
            CreateText(scene, rect, "Label", font, label, 0f, 0f, width - 16f, height - 10f,
                fontSize, TextAnchor.MiddleCenter);
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

        private static void SetTopLeftRect(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect))
                return;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static bool HasButtonVisual(Button button) =>
            button != null && button.targetGraphic != null &&
            button.GetComponentInChildren<Text>(true) != null;

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

        private static bool Bound(UnityEngine.Object target, string field,
            UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static Transform Require(Scene scene, string name)
        {
            Transform[] matches = Named(scene, name).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static IEnumerable<Transform> Named(Scene scene, string name) =>
            AllTransforms(scene).Where(transform => transform.name == name);

        private static Transform[] AllTransforms(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

        private static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();

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

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
