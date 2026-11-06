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
    /// Transactional FoundationScene updater for the SC-024 family-doctrine panel and title
    /// entry. It runs in one Undo group, validates before saving, and rolls back with Undo
    /// on any exception without reloading the scene. The fixed root is saved inactive; the
    /// panel component lives on the active HUD open button so it can subscribe at load.
    /// </summary>
    public static class AddSC024FamilyDoctrine
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "FamilyDoctrineRoot";
        public const string OpenButtonName = "FamilyDoctrineOpenButton";
        public const string TitleName = "FamilyDoctrineTitleText";
        public const string CreedName = "FamilyDoctrineCreedText";
        public const string CreedChoicesName = "FamilyDoctrineCreedChoicesText";
        public const string EffectName = "FamilyDoctrineEffectText";
        public const string HeadName = "FamilyDoctrineHeadText";
        public const string AmbitionName = "FamilyDoctrineAmbitionText";
        public const string BlockReasonName = "FamilyDoctrineBlockReasonText";
        public const string MessageName = "FamilyDoctrineMessageText";
        public const string DevelopButtonName = "FamilyDoctrineDevelopButton";
        public const string TechniqueButtonName = "FamilyDoctrineTechniqueButton";
        public const string EducateButtonName = "FamilyDoctrineEducateButton";
        public const string CloseButtonName = "FamilyDoctrineCloseButton";
        public const string TitleButtonName = "SC024FamilyDoctrineDemoButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-024 Family Doctrine";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string IndustryRootName = "IndustryRoot";
        private const string EducationRootName = "SC020EducationRoot";
        private const string ResearchRootName = "SC021ResearchRoot";
        private const string FormationRootName = "SC022FamilyFormationRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventRootName = "EventChoiceRoot";
        private const string TitleStatusName = "M6TitleStatusText";
        private const string NewGameButtonName = "M6NewGameButton";
        private const string ContinueButtonName = "M6ContinueButton";
        private const string StandardButtonName = "SC013StandardSampleButton";
        private const string CrisisButtonName = "SC013CrisisDemoButton";
        private const string EducationTitleButtonName = "SC020EducationDemoButton";
        private const string ResearchTitleButtonName = "SC021ResearchDemoButton";
        private const string FormationTitleButtonName = "SC022FamilyFormationDemoButton";
        private const string SeclusionTitleButtonName = "SC023SeclusionDemoButton";
        private const string ChronicleOpenButtonName = "FamilyChronicleOpenButton";
        private const string RosterOpenButtonName = "FamilyRosterOpenButton";
        private const string IndustryOpenButtonName = "IndustryOpenButton";
        private const string EducationOpenButtonName = "SC020EducationOpenButton";
        private const string ResearchOpenButtonName = "SC021ResearchOpenButton";
        private const string FormationOpenButtonName = "SC022FamilyFormationOpenButton";
        private const string SuccessionHudName = "SuccessionHud";
        private const string UndoName = "Add SC-024 Family Doctrine";

        // Below the SC-022 formation entry (-406..-450).
        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, -458f);
        private static readonly Vector2 OpenButtonSize = new Vector2(220f, 44f);
        // Mirrors the SC-023 status-band entry on the opposite side (x -620..-370).
        private static readonly Vector2 TitleButtonPosition = new Vector2(-495f, -325f);
        private static readonly Vector2 TitleButtonSize = new Vector2(250f, 40f);

        private static readonly Vector2 RootSize = new Vector2(920f, 620f);

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
            if (result == UpdateResult.Success)
                Debug.Log("SC-024 family-doctrine panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-024 family-doctrine updater refused or rolled back: {result}.");
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
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            FamilyDoctrinePanel[] panels = Components<FamilyDoctrinePanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            Transform[] opens = Named(scene, OpenButtonName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || controllers.Length != 1 ||
                panels.Length != 1 || roots.Length != 1 || opens.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, TitleScreenController, doctrine panel, root, and open button.");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform open = opens[0];
            FamilyDoctrinePanel panel = panels[0];

            if (root.parent != canvas)
                return Invalid(out error, "FamilyDoctrineRoot must be a direct child of Canvas.");
            if (root.gameObject.activeSelf)
                return Invalid(out error, "FamilyDoctrineRoot must be saved inactive; the open button shows it.");
            if (panel.transform != open || panel.gameObject != open.gameObject)
                return Invalid(out error,
                    "FamilyDoctrinePanel must live on the active FamilyDoctrineOpenButton.");
            if (open.parent != canvas || !open.gameObject.activeSelf)
                return Invalid(out error,
                    "FamilyDoctrineOpenButton must be an active direct child of Canvas.");
            if (!HasButtonVisual(open.GetComponent<Button>()) ||
                !HasRaycastTarget(open.GetComponent<Button>()))
                return Invalid(out error, "The family-doctrine open button is invalid.");

            Transform title = Require(scene, TitleName);
            Transform creed = Require(scene, CreedName);
            Transform creedChoices = Require(scene, CreedChoicesName);
            Transform effect = Require(scene, EffectName);
            Transform head = Require(scene, HeadName);
            Transform ambition = Require(scene, AmbitionName);
            Transform blockReason = Require(scene, BlockReasonName);
            Transform message = Require(scene, MessageName);
            Transform develop = Require(scene, DevelopButtonName);
            Transform technique = Require(scene, TechniqueButtonName);
            Transform educate = Require(scene, EducateButtonName);
            Transform close = Require(scene, CloseButtonName);
            if (title == null || creed == null || creedChoices == null || effect == null ||
                head == null || ambition == null || blockReason == null || message == null ||
                develop == null || technique == null || educate == null || close == null)
                return Invalid(out error, "SC-024 family-doctrine hierarchy is incomplete or ambiguous.");

            Transform[] children =
            {
                title, creed, creedChoices, effect, head, ambition, blockReason, message,
                develop, technique, educate, close
            };
            foreach (Transform child in children)
                if (child.parent != root)
                    return Invalid(out error,
                        "All SC-024 family-doctrine controls must be direct children of the root.");

            Text titleText = title.GetComponent<Text>();
            Text creedText = creed.GetComponent<Text>();
            Text creedChoicesText = creedChoices.GetComponent<Text>();
            Text effectText = effect.GetComponent<Text>();
            Text headText = head.GetComponent<Text>();
            Text ambitionText = ambition.GetComponent<Text>();
            Text blockReasonText = blockReason.GetComponent<Text>();
            Text messageText = message.GetComponent<Text>();
            Button openButton = open.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            Button developButton = develop.GetComponent<Button>();
            Button techniqueButton = technique.GetComponent<Button>();
            Button educateButton = educate.GetComponent<Button>();
            if (titleText == null || creedText == null || creedChoicesText == null ||
                effectText == null || headText == null || ambitionText == null ||
                blockReasonText == null || messageText == null ||
                titleText.font == null || creedText.font == null || creedChoicesText.font == null ||
                effectText.font == null || headText.font == null || ambitionText.font == null ||
                blockReasonText.font == null || messageText.font == null ||
                closeButton == null || developButton == null || techniqueButton == null ||
                educateButton == null || !HasButtonVisual(closeButton) ||
                !HasButtonVisual(developButton) || !HasButtonVisual(techniqueButton) ||
                !HasButtonVisual(educateButton) || !HasRaycastTarget(closeButton) ||
                !HasRaycastTarget(developButton) || !HasRaycastTarget(techniqueButton) ||
                !HasRaycastTarget(educateButton) || root.GetComponent<Image>() == null ||
                !root.GetComponent<Image>().raycastTarget)
                return Invalid(out error,
                    "SC-024 text, buttons, background, or raycast components are invalid.");
            if (blockReason.gameObject.activeSelf || message.gameObject.activeSelf)
                return Invalid(out error,
                    "The SC-024 block-reason and message lines must start hidden.");
            if (!titleText.text.Contains("家训") || !titleText.text.Contains("抱负"))
                return Invalid(out error, "The SC-024 title must contain 家训 and 抱负.");
            if (!creedChoicesText.text.Contains("守秘为先") ||
                !creedChoicesText.text.Contains("义理至上") ||
                !creedChoicesText.text.Contains("唯才是举"))
                return Invalid(out error,
                    "The SC-024 creed line must name all three Chinese creeds.");
            if (!developButton.GetComponentInChildren<Text>(true).text.Contains("发展家业") ||
                !techniqueButton.GetComponentInChildren<Text>(true).text.Contains("传承家技") ||
                !educateButton.GetComponentInChildren<Text>(true).text.Contains("培养继承人"))
                return Invalid(out error,
                    "The SC-024 ambition buttons must use the three Chinese labels.");

            if (!Bound(panel, "gameSession", sessions[0]) ||
                !Bound(panel, "panelRoot", root.gameObject) ||
                !Bound(panel, "openButton", openButton) ||
                !Bound(panel, "closeButton", closeButton) ||
                !Bound(panel, "titleText", titleText) ||
                !Bound(panel, "creedText", creedText) ||
                !Bound(panel, "creedChoicesText", creedChoicesText) ||
                !Bound(panel, "effectText", effectText) ||
                !Bound(panel, "headText", headText) ||
                !Bound(panel, "ambitionText", ambitionText) ||
                !Bound(panel, "blockReasonText", blockReasonText) ||
                !Bound(panel, "messageText", messageText) ||
                !Bound(panel, "developHouseholdButton", developButton) ||
                !Bound(panel, "passDownTechniqueButton", techniqueButton) ||
                !Bound(panel, "educateHeirButton", educateButton))
                return Invalid(out error,
                    "FamilyDoctrinePanel serialized references are not bound.");

            RectTransform canvasRect = canvas as RectTransform;
            RectTransform rootRect = root as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform closeRect = close as RectTransform;
            RectTransform titleRect = title as RectTransform;
            if (canvasRect == null || rootRect == null || openRect == null || closeRect == null ||
                titleRect == null || !Contained(rootRect, canvasRect) ||
                !Contained(openRect, canvasRect))
                return Invalid(out error,
                    "The SC-024 panel root or open button lies outside the Canvas.");
            foreach (Transform child in children)
                if (!(child is RectTransform childRect) || !Contained(childRect, rootRect))
                    return Invalid(out error,
                        "SC-024 family-doctrine controls lie outside the panel bounds.");

            RectTransform[] rows =
            {
                titleRect, creed as RectTransform, creedChoices as RectTransform,
                effect as RectTransform, head as RectTransform, ambition as RectTransform,
                blockReason as RectTransform, develop as RectTransform, message as RectTransform,
                close as RectTransform
            };
            for (int i = 1; i < rows.Length; i++)
                if (Overlaps(rows[i - 1], rows[i], rootRect))
                    return Invalid(out error, "SC-024 family-doctrine controls overlap each other.");
            if (Overlaps(develop as RectTransform, technique as RectTransform, rootRect) ||
                Overlaps(develop as RectTransform, educate as RectTransform, rootRect) ||
                Overlaps(technique as RectTransform, educate as RectTransform, rootRect))
                return Invalid(out error, "SC-024 ambition buttons overlap each other.");

            if (Overlaps(openRect, SuccessionHud(scene), canvasRect) ||
                OverlapsNamed(scene, openRect, IndustryOpenButtonName, canvasRect) ||
                OverlapsNamed(scene, openRect, ChronicleOpenButtonName, canvasRect) ||
                OverlapsNamed(scene, openRect, RosterOpenButtonName, canvasRect) ||
                OverlapsNamed(scene, openRect, EducationOpenButtonName, canvasRect) ||
                OverlapsNamed(scene, openRect, ResearchOpenButtonName, canvasRect) ||
                OverlapsNamed(scene, openRect, FormationOpenButtonName, canvasRect))
                return Invalid(out error,
                    "The family-doctrine open button overlaps the succession HUD or another HUD entry.");

            if (!ContainedInRenderBand(canvas, root) || !ContainedInRenderBand(canvas, open))
                return Invalid(out error,
                    "FamilyDoctrineRoot and its open button must render above the HUD and below the title/event overlays.");

            return ValidateTitleEntry(scene, canvas, sessions[0], controllers[0], out error);
        }

        private static bool ValidateTitleEntry(Scene scene, Transform canvas, GameSession session,
            TitleScreenController controller, out string error)
        {
            error = null;
            Transform titleRoot = canvas.Find(TitleRootName);
            if (titleRoot == null)
                return true;

            Transform button = Require(scene, TitleButtonName);
            if (button == null)
                return Invalid(out error, "The SC-024 title entry is missing.");
            if (button.parent != titleRoot || !button.gameObject.activeSelf)
                return Invalid(out error, "The SC-024 title entry must be active under M6TitleRoot.");
            if (!HasButtonVisual(button.GetComponent<Button>()) ||
                !HasRaycastTarget(button.GetComponent<Button>()))
                return Invalid(out error, "The SC-024 title entry button is invalid.");
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null || !label.text.Contains("家训") || !label.text.Contains("抱负"))
                return Invalid(out error, "The SC-024 title entry label must contain 家训 and 抱负.");
            if (!Bound(controller, "familyDoctrineDemoButton", button.GetComponent<Button>()) ||
                !Bound(controller, "gameSession", session))
                return Invalid(out error, "TitleScreenController SC-024 references are not bound.");

            RectTransform rootRect = titleRoot as RectTransform;
            RectTransform buttonRect = button as RectTransform;
            if (rootRect == null || buttonRect == null || !Contained(buttonRect, rootRect))
                return Invalid(out error, "The SC-024 title entry lies outside M6TitleRoot.");
            if (OverlapsNamed(scene, buttonRect, TitleStatusName, rootRect) ||
                OverlapsNamed(scene, buttonRect, NewGameButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ContinueButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, StandardButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, CrisisButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, EducationTitleButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ResearchTitleButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, FormationTitleButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, SeclusionTitleButtonName, rootRect))
                return Invalid(out error,
                    "The SC-024 title entry overlaps another title control, including SC-023.");
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
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            Transform canvas = canvases.Length == 1 ? canvases[0] : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            TitleScreenController controller = controllers.Length == 1 ? controllers[0] : null;
            if (canvas == null || session == null || controller == null ||
                Components<FamilyDoctrinePanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (Named(scene, RootName).Any() || Named(scene, OpenButtonName).Any() ||
                Named(scene, TitleButtonName).Any() ||
                Components<FamilyDoctrinePanel>(scene).Length != 0 ||
                HasSerializedReference(controller, "familyDoctrineDemoButton"))
                return UpdateResult.ExistingPartialControl;

            ObjectSnapshot[] originals = Snapshot(scene);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject rootObject = Create(scene, canvas, RootName, typeof(Image));
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = RootSize;
                Image background = rootObject.GetComponent<Image>();
                background.color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                background.raycastTarget = true;
                rootObject.SetActive(false);

                Button open = CreateButton(scene, canvas, OpenButtonName, font, "家训与抱负",
                    OpenButtonSize.x, OpenButtonSize.y, 16);
                SetTopLeftRect(open.transform, OpenButtonPosition, OpenButtonSize);

                Text title = CreateText(scene, rootRect, TitleName, font, "家训与抱负",
                    0f, 278f, 860f, 36f, 22, TextAnchor.MiddleLeft);
                Text creed = CreateText(scene, rootRect, CreedName, font,
                    "当前家训：义理至上", 0f, 240f, 860f, 30f, 17, TextAnchor.MiddleLeft);
                Text creedChoices = CreateText(scene, rootRect, CreedChoicesName, font,
                    "可选家训：守秘为先 / 义理至上 / 唯才是举", 0f, 210f, 860f, 26f, 14,
                    TextAnchor.MiddleLeft);
                creedChoices.color = new Color(0.9f, 0.82f, 0.6f, 1f);
                Text effect = CreateText(scene, rootRect, EffectName, font,
                    "议事方式：各房先开口", 0f, 178f, 860f, 26f, 14, TextAnchor.MiddleLeft);
                Text head = CreateText(scene, rootRect, HeadName, font,
                    "当前家主：明智 晴信", 0f, 146f, 860f, 28f, 15, TextAnchor.MiddleLeft);
                Text ambition = CreateText(scene, rootRect, AmbitionName, font,
                    "当前抱负：发展家业 进行中 0/3", 0f, 114f, 860f, 28f, 15,
                    TextAnchor.MiddleLeft);
                Text blockReason = CreateText(scene, rootRect, BlockReasonName, font, string.Empty,
                    0f, 82f, 860f, 24f, 14, TextAnchor.MiddleLeft);
                blockReason.color = new Color(1f, 0.72f, 0.62f, 1f);
                blockReason.gameObject.SetActive(false);

                Button develop = CreateButton(scene, rootRect, DevelopButtonName, font, "发展家业",
                    260f, 40f, 15);
                SetCenteredRect(develop.transform, new Vector2(-300f, 32f), new Vector2(260f, 40f));
                Button technique = CreateButton(scene, rootRect, TechniqueButtonName, font,
                    "传承家技", 260f, 40f, 15);
                SetCenteredRect(technique.transform, new Vector2(0f, 32f), new Vector2(260f, 40f));
                Button educate = CreateButton(scene, rootRect, EducateButtonName, font, "培养继承人",
                    260f, 40f, 15);
                SetCenteredRect(educate.transform, new Vector2(300f, 32f), new Vector2(260f, 40f));

                Text message = CreateText(scene, rootRect, MessageName, font, string.Empty,
                    0f, -18f, 860f, 24f, 13, TextAnchor.MiddleLeft);
                message.color = new Color(0.65f, 0.86f, 1f, 1f);
                message.gameObject.SetActive(false);
                Text help = CreateText(scene, rootRect, "FamilyDoctrineHelpText", font,
                    "家训只能在家族议事事件中维持或改革；本面板只显示当前效果。" +
                    "每位家主一生只能选择一项抱负，选定后不能更换。",
                    0f, -72f, 860f, 52f, 13, TextAnchor.MiddleCenter);
                help.color = new Color(0.75f, 0.9f, 1f, 1f);
                Button close = CreateButton(scene, rootRect, CloseButtonName, font, "关闭",
                    160f, 36f, 16);
                SetCenteredRect(close.transform, new Vector2(0f, -270f), new Vector2(160f, 36f));

                PlaceAboveMapHud(canvas, rootObject.transform);
                PlaceAboveMapHud(canvas, open.transform);

                FamilyDoctrinePanel panel = Undo.AddComponent<FamilyDoctrinePanel>(open.gameObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, rootObject, open, close, title, creed, creedChoices,
                    effect, head, ambition, blockReason, message, develop, technique, educate);
                SetSerializedReferences(panel, session, rootObject, open, close, title, creed,
                    creedChoices, effect, head, ambition, blockReason, message, develop, technique,
                    educate);
                EditorUtility.SetDirty(panel);

                if (canvas.Find(TitleRootName) != null)
                    EnsureTitleEntry(scene, controller, font);

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
                Debug.LogWarning($"SC-024 family-doctrine updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static void EnsureTitleEntry(Scene scene, TitleScreenController controller, Font font)
        {
            Transform titleRoot = scene.GetRootGameObjects()
                .Where(root => root.name == CanvasName && root.GetComponent<Canvas>() != null)
                .Select(root => root.transform.Find(TitleRootName))
                .FirstOrDefault(root => root != null);
            if (titleRoot == null)
                return;

            Transform existing = titleRoot.Find(TitleButtonName);
            Button button;
            if (existing == null)
            {
                button = CreateTitleButton(scene, titleRoot, TitleButtonName, font,
                    "SC-024 家训与抱负示范", TitleButtonPosition, TitleButtonSize);
            }
            else
            {
                button = existing.GetComponent<Button>();
                SetCenteredRect(existing, TitleButtonPosition, TitleButtonSize);
            }
            if (button == null)
                throw new InvalidOperationException("The SC-024 title entry has no Button component.");

            Undo.RecordObject(controller, UndoName);
            controller.BindFamilyDoctrineScenarioButton(button);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("familyDoctrineDemoButton").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
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

        private static RectTransform SuccessionHud(Scene scene)
        {
            Transform hud = Require(scene, SuccessionHudName);
            return hud as RectTransform;
        }

        private static bool OverlapsNamed(Scene scene, RectTransform first, string secondName,
            RectTransform coordinateRoot)
        {
            Transform second = Require(scene, secondName);
            return second is RectTransform secondRect && first != null &&
                   Overlaps(first, secondRect, coordinateRoot);
        }

        private static Button CreateTitleButton(Scene scene, Transform parent, string name, Font font,
            string label, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            SetCenteredRect(go.transform, position, size);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.32f, 0.42f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            SceneManager.MoveGameObjectToScene(labelObject, scene);
            Undo.RegisterCreatedObjectUndo(labelObject, UndoName);
            Undo.SetTransformParent(labelObject.transform, go.transform, UndoName);
            labelObject.transform.localScale = Vector3.one;
            labelObject.layer = parent.gameObject.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(size.x, size.y - 2f);
            Text text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 17;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return button;
        }

        private static void SetSerializedReferences(FamilyDoctrinePanel panel, GameSession session,
            GameObject root, Button open, Button close, Text title, Text creed, Text creedChoices,
            Text effect, Text head, Text ambition, Text blockReason, Text message, Button develop,
            Button technique, Button educate)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = root;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("creedText").objectReferenceValue = creed;
            serialized.FindProperty("creedChoicesText").objectReferenceValue = creedChoices;
            serialized.FindProperty("effectText").objectReferenceValue = effect;
            serialized.FindProperty("headText").objectReferenceValue = head;
            serialized.FindProperty("ambitionText").objectReferenceValue = ambition;
            serialized.FindProperty("blockReasonText").objectReferenceValue = blockReason;
            serialized.FindProperty("messageText").objectReferenceValue = message;
            serialized.FindProperty("developHouseholdButton").objectReferenceValue = develop;
            serialized.FindProperty("passDownTechniqueButton").objectReferenceValue = technique;
            serialized.FindProperty("educateHeirButton").objectReferenceValue = educate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static bool HasButtonVisual(Button button) =>
            button != null && button.targetGraphic != null &&
            button.GetComponentInChildren<Text>(true) != null;

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

        private static bool HasSerializedReference(UnityEngine.Object target, string field)
        {
            if (target == null)
                return false;
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && property.objectReferenceValue != null;
        }

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

        private static IEnumerable<Transform> Named(Scene scene, string name) =>
            AllTransforms(scene).Where(transform => transform.name == name);

        private static Transform[] AllTransforms(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

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

        private static bool Overlaps(RectTransform first, RectTransform second,
            RectTransform coordinateRoot)
        {
            if (first == null || second == null || coordinateRoot == null)
                return false;
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            Vector3 firstMin = coordinateRoot.InverseTransformPoint(firstCorners[0]);
            Vector3 firstMax = coordinateRoot.InverseTransformPoint(firstCorners[2]);
            Vector3 secondMin = coordinateRoot.InverseTransformPoint(secondCorners[0]);
            Vector3 secondMax = coordinateRoot.InverseTransformPoint(secondCorners[2]);
            var firstRect = Rect.MinMaxRect(Mathf.Min(firstMin.x, firstMax.x),
                Mathf.Min(firstMin.y, firstMax.y), Mathf.Max(firstMin.x, firstMax.x),
                Mathf.Max(firstMin.y, firstMax.y));
            var secondRect = Rect.MinMaxRect(Mathf.Min(secondMin.x, secondMax.x),
                Mathf.Min(secondMin.y, secondMax.y), Mathf.Max(secondMin.x, secondMax.x),
                Mathf.Max(secondMin.y, secondMax.y));
            return firstRect.Overlaps(secondRect, true);
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
