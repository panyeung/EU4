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
    /// <summary>Extends the reviewed M4a panel root with the two fixed M4b mission cards.</summary>
    public static class AddM4bMissionBoard
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        private const string RootName = AddM4SupplyShortageMissionPanel.RootName;
        private const string MenuPath = "Tools/Sengoku Chronicle/Add M4b Mission Board";
        private const string UndoName = "Add M4b Mission Board";
        private const float PanelWidth = 280f;
        private static readonly string[] TextNames =
        {
            "SealedLetterStageText", "SealedLetterObjectiveText", "SealedLetterFeedbackText",
            "MarketTallyStageText", "MarketTallyObjectiveText", "MarketTallyFeedbackText"
        };
        private static readonly string[] ButtonNames =
            { "SealedLetterActionButton", "MarketTallyActionButton", "MissionBoardToggleButton" };
        private static readonly string[] M4aButtonNames =
            { "AcceptButton", "InvestigateButton", "NegotiateButton", "ReportButton" };
        private static readonly string[] TextFields =
        {
            "sealedLetterStageText", "sealedLetterObjectiveText", "sealedLetterFeedbackText",
            "marketTallyStageText", "marketTallyObjectiveText", "marketTallyFeedbackText"
        };
        private static readonly string[] ButtonFields =
            { "sealedLetterActionButton", "marketTallyActionButton", "missionBoardToggleButton" };

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, InvalidHierarchyOrBindings, ExistingBoard,
            InsufficientSpace, ValidationFailed, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.EditorPlaying : CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success) result = Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("M4b mission board validated; FoundationScene saved.");
            else
                Debug.LogError($"M4b mission board updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
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
            if (!AddM4SupplyShortageMissionPanel.Validate(scene, out string m4aError))
                return Invalid(out error, $"M4a panel validation failed: {m4aError}");

            Transform[] roots = Named(scene, RootName);
            SupplyShortageMissionPanel[] panels = Components<SupplyShortageMissionPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1)
                return Invalid(out error, "Expected exactly one existing M4a mission root and panel.");
            Transform root = roots[0];
            SupplyShortageMissionPanel panel = panels[0];
            if (root.parent == null || root.name != RootName || panel.transform != root ||
                root.childCount != 17 || ((RectTransform)root).sizeDelta.x != PanelWidth)
                return Invalid(out error, "Expected the fixed mission panel root with exactly nine M4b controls.");

            for (int i = 0; i < TextNames.Length; i++)
            {
                Transform[] matches = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == TextNames[i]).ToArray();
                if (matches.Length != 1 || matches[0].parent != root)
                    return Invalid(out error, $"Missing, duplicate, or misplaced {TextNames[i]}.");
                Text text = matches[0].GetComponent<Text>();
                if (text == null || text.font == null || !Bound(panel, TextFields[i], text))
                    return Invalid(out error, $"Unbound or invalid {TextNames[i]}.");
            }

            for (int i = 0; i < ButtonNames.Length; i++)
            {
                Transform[] matches = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == ButtonNames[i]).ToArray();
                if (matches.Length != 1 || matches[0].parent != root)
                    return Invalid(out error, $"Missing, duplicate, or misplaced {ButtonNames[i]}.");
                Button button = matches[0].GetComponent<Button>();
                Text[] labels = matches[0].GetComponentsInChildren<Text>(true);
                if (button == null || button.targetGraphic == null || button.targetGraphic.gameObject != button.gameObject ||
                    labels.Length != 1 || labels[0].font == null || string.IsNullOrWhiteSpace(labels[0].text) ||
                    labels[0].transform.parent != button.transform || button.transform.childCount != 1 ||
                    !Bound(panel, ButtonFields[i], button))
                    return Invalid(out error, $"Unbound or invalid {ButtonNames[i]}.");
            }

            RectTransform panelRect = root as RectTransform;
            RectTransform town = root.parent as RectTransform;
            if (panelRect == null || town == null || town.rect.width < PanelWidth + 24f ||
                !Contained(panelRect, town) || root.GetComponentsInChildren<RectTransform>(true)
                    .Any(r => r != panelRect && !Contained(r, panelRect)))
                return Invalid(out error, "M4b mission board is outside the fixed root or town bounds.");
            foreach (string siblingName in new[] { "TownMenuTitleText", "OpenNpcRosterButton", "ReturnToMapButton" })
            {
                Transform[] siblings = Named(scene, siblingName).Where(t => t.parent == root.parent).ToArray();
                if (siblings.Any(sibling => sibling is RectTransform siblingRect && Overlaps(panelRect, siblingRect, town)))
                    return Invalid(out error, $"M4b mission panel overlaps town control {siblingName}.");
            }

            foreach (string[] separatedPair in new[]
            {
                new[] { "SealedLetterObjectiveText", "SealedLetterFeedbackText" },
                new[] { "MarketTallyObjectiveText", "MarketTallyFeedbackText" },
                new[] { "MarketTallyActionButton", "MissionBoardToggleButton" }
            })
            {
                RectTransform first = root.Find(separatedPair[0]) as RectTransform;
                RectTransform second = root.Find(separatedPair[1]) as RectTransform;
                if (Overlaps(first, second, panelRect))
                    return Invalid(out error, $"M4b controls {separatedPair[0]} and {separatedPair[1]} overlap.");
            }
            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded) return UpdateResult.InvalidScene;
            if (Components<WorldMapController>(scene).Length != 1 || Components<GameSession>(scene).Length != 1 ||
                Named(scene, RootName).Length != 1 || Components<SupplyShortageMissionPanel>(scene).Length != 1)
                return UpdateResult.MissingOrDuplicateObjects;
            if (!AddM4SupplyShortageMissionPanel.Validate(scene, out _))
                return UpdateResult.InvalidHierarchyOrBindings;
            if (Validate(scene, out _)) return UpdateResult.Success;

            Transform root = Named(scene, RootName)[0];
            SupplyShortageMissionPanel panel = root.GetComponent<SupplyShortageMissionPanel>();
            if (TextNames.Any(name => Named(scene, name).Length != 0) ||
                ButtonNames.Any(name => Named(scene, name).Length != 0) ||
                TextFields.Concat(ButtonFields).Any(field => !IsUnassigned(panel, field)))
                return UpdateResult.ExistingBoard;
            RectTransform rootRect = root as RectTransform;
            RectTransform town = root.parent as RectTransform;
            if (rootRect == null || town == null || rootRect.sizeDelta.x != PanelWidth ||
                town.rect.width < PanelWidth + 24f || town.rect.height < 260f)
                return UpdateResult.InsufficientSpace;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                foreach (string buttonName in M4aButtonNames)
                {
                    RectTransform buttonRect = root.Find(buttonName) as RectTransform;
                    if (buttonRect == null) throw new InvalidOperationException($"Missing M4a button {buttonName}.");
                    Undo.RecordObject(buttonRect, UndoName);
                    buttonRect.anchoredPosition = new Vector2(-48f, buttonRect.anchoredPosition.y);
                    buttonRect.sizeDelta = new Vector2(160f, buttonRect.sizeDelta.y);
                    RectTransform labelRect = buttonRect.childCount == 1 ? buttonRect.GetChild(0) as RectTransform : null;
                    if (labelRect == null) throw new InvalidOperationException($"Invalid M4a button label {buttonName}.");
                    Undo.RecordObject(labelRect, UndoName);
                    labelRect.sizeDelta = new Vector2(160f, labelRect.sizeDelta.y);
                }
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                Text[] texts =
                {
                    CreateText(scene, root, TextNames[0], font, "密信：未领取", 108f, 18f, 0f, 256f, 14),
                    CreateText(scene, root, TextNames[1], font, "", 84f, 28f, 0f, 256f, 14),
                    CreateText(scene, root, TextNames[2], font, "", 55f, 22f, 0f, 256f, 12),
                    CreateText(scene, root, TextNames[3], font, "市集账目：未领取", -7f, 18f, 0f, 256f, 14),
                    CreateText(scene, root, TextNames[4], font, "", -31f, 28f, 0f, 256f, 14),
                    CreateText(scene, root, TextNames[5], font, "", -58f, 22f, 0f, 256f, 12)
                };
                Button[] buttons =
                {
                    CreateButton(scene, root, ButtonNames[0], font, "领取密信", 0f, 20f, 180f, 26f, 14),
                    CreateButton(scene, root, ButtonNames[1], font, "领取账目任务", 0f, -84f, 180f, 24f, 14),
                    CreateButton(scene, root, ButtonNames[2], font, "其他任务", 80f, -108f, 88f, 20f, 11)
                };
                Undo.RecordObject(panel, UndoName);
                panel.BindM4bReferences(texts[0], texts[1], texts[2], buttons[0], texts[3], texts[4], texts[5], buttons[1], buttons[2]);
                EditorUtility.SetDirty(panel);
                beforeValidation?.Invoke(root.gameObject);
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
                Debug.LogWarning($"M4b updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool IsUnassigned(UnityEngine.Object target, string field)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && property.objectReferenceValue == null &&
                property.objectReferenceEntityIdValue.Equals(EntityId.None);
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font, string value,
            float y, float height, float x, float width, int fontSize)
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Text text = go.GetComponent<Text>();
            text.font = font; text.fontSize = fontSize; text.text = value;
            text.color = Color.white; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.raycastTarget = false;
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
            Button button = go.GetComponent<Button>(); button.targetGraphic = image;
            Text buttonLabel = CreateText(scene, rect, "Label", font, label, 0f, height - 2f, 0f, width, fontSize);
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static GameObject Create(Scene scene, Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one; go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            foreach (Type component in components) Undo.AddComponent(go, component);
            return go;
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners); Rect bounds = parent.rect;
            return corners.All(c => { Vector3 point = parent.InverseTransformPoint(c); return bounds.Contains(new Vector2(point.x, point.y)); });
        }

        private static bool Overlaps(RectTransform first, RectTransform second, RectTransform coordinateRoot)
        {
            Rect ToRoot(RectTransform rect)
            {
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                Vector3 a = coordinateRoot.InverseTransformPoint(corners[0]);
                Vector3 c = coordinateRoot.InverseTransformPoint(corners[2]);
                return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y),
                    Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
            }
            Rect firstBounds = ToRoot(first), secondBounds = ToRoot(second);
            return firstBounds.Overlaps(secondBounds, true);
        }

        private static bool Invalid(out string error, string message) { error = message; return false; }
        private static Transform[] Named(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name == name).ToArray();
        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
