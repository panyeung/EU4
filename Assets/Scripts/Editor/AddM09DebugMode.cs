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
    /// Transactional FoundationScene updater for the development-only Debug Mode window.
    /// It follows the AddM03/AddSC024 pattern: one Undo group, validation before saving, and
    /// Undo rollback on any exception without reloading the scene.
    /// <para>
    /// Nothing in this window is serialized except a fixed inactive root and the dev entry:
    /// the guarded title-screen code attaches <see cref="DebugModePanel"/> to the root at
    /// runtime and the panel builds its own controls. A release player therefore loads a
    /// scene with no reference to a type it does not compile, so it never reports a
    /// missing script, and the layout keeps one source of truth in code.
    /// </para>
    /// <para>
    /// Both authored objects are saved inactive: a release player contains neither the panel
    /// component nor any code that would activate the entry, so the development entry never
    /// appears. In the Editor and development builds the guarded title-screen code activates
    /// the entry on the title page and while a debug session is active.
    /// </para>
    /// </summary>
    public static class AddM09DebugMode
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "M09DebugModeRoot";
        public const string EntryName = "M09DebugModeEntry";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add M09 Debug Mode";
        public const string UndoName = "Add M09 Debug Mode";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventRootName = "EventChoiceRoot";
        private const string EntryLabel = "Debug 综合测试";
        private const string EntryLabelName = "Label";

        /// <summary>
        /// The window size has one authority: the runtime factory lays every control out for
        /// <see cref="DebugModePanel.WindowSize"/>, so the saved root must use exactly that.
        /// </summary>
        private static Vector2 RootSize => DebugModePanel.WindowSize;

        /// <summary>Bottom-right corner, below the window bounds and above the HUD art.</summary>
        private static readonly Vector2 EntryPosition = new Vector2(-16f, 8f);
        private static readonly Vector2 EntrySize = new Vector2(200f, 32f);

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
                Debug.Log("Debug Mode window validated; FoundationScene saved.");
            else
                Debug.LogError($"Debug Mode updater refused or rolled back: {result}.");
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

        /// <summary>
        /// Reads the Debug Mode binding without changing anything. Every failure names the
        /// object or reference an operator has to fix.
        /// </summary>
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
            DebugModePanel[] panels = Components<DebugModePanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            Transform[] entries = Named(scene, EntryName).ToArray();
            if (canvases.Length != 1)
                return Invalid(out error, "Expected exactly one Canvas root; found " +
                                           canvases.Length + ".");
            if (sessions.Length != 1)
                return Invalid(out error, "Expected exactly one GameSession; found " +
                                           sessions.Length + ".");
            if (controllers.Length != 1)
                return Invalid(out error, "Expected exactly one TitleScreenController; found " +
                                           controllers.Length + ".");
            if (panels.Length != 0)
                return Invalid(out error, "The scene must not serialize a DebugModePanel; the " +
                                           "guarded runtime code attaches it. Found " +
                                           panels.Length + ".");
            if (roots.Length != 1)
                return Invalid(out error, "Expected exactly one " + RootName + "; found " +
                                           roots.Length + ".");
            if (entries.Length != 1)
                return Invalid(out error, "Expected exactly one " + EntryName + "; found " +
                                           entries.Length + ".");

            Transform[] titleRoots = Named(scene, TitleRootName).ToArray();
            if (titleRoots.Length != 1)
                return Invalid(out error, "Expected exactly one " + TitleRootName + "; found " +
                                           titleRoots.Length + ".");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform entry = entries[0];
            Transform titleRoot = titleRoots[0];

            if (root.parent != canvas)
                return Invalid(out error, RootName + " must be a direct child of Canvas.");
            if (root.gameObject.activeSelf)
                return Invalid(out error, RootName + " must be saved inactive; only the debug " +
                                                    "entry shows it at runtime.");
            if (!(root is RectTransform rootRect) || !Contained(rootRect, canvas as RectTransform))
                return Invalid(out error, RootName + " must be a RectTransform inside the Canvas.");
            if ((rootRect.sizeDelta - DebugModePanel.WindowSize).sqrMagnitude > 0.01f)
                return Invalid(out error, RootName + " must use the runtime window size " +
                                                    DebugModePanel.WindowSize + ", not " +
                                                    rootRect.sizeDelta + ".");
            if (root.GetComponent<DebugModePanel>() != null)
                return Invalid(out error, RootName + " must not serialize a DebugModePanel; the " +
                                                    "guarded runtime code attaches it.");
            Image background = root.GetComponent<Image>();
            if (background == null || !background.raycastTarget)
                return Invalid(out error, RootName + " needs a raycast background image.");

            // The saved entry belongs to the title page. Keeping it out of the Canvas child
            // list preserves the M06 modal order (only EventChoiceRoot and the tactical
            // overlay may sit above M6TitleRoot, and the overlay must stay last); the guarded
            // runtime code re-parents it to the root Canvas on startup so close/reopen works.
            if (entry.parent != titleRoot)
                return Invalid(out error, EntryName + " must be a child of " + TitleRootName +
                                                    " so the Canvas modal order is unchanged.");
            if (entry.GetSiblingIndex() != titleRoot.childCount - 1)
                return Invalid(out error, EntryName + " must be the last child of " +
                                                    TitleRootName + ".");
            if (entry.gameObject.activeSelf)
                return Invalid(out error, EntryName + " must be saved inactive; the guarded " +
                                                    "title-screen code activates it at runtime.");
            Button entryButton = entry.GetComponent<Button>();
            if (entryButton == null || entryButton.targetGraphic == null ||
                !entryButton.targetGraphic.raycastTarget)
                return Invalid(out error, EntryName + " needs a Button with a raycast target.");
            Text label = entry.GetComponentInChildren<Text>(true);
            if (label == null || label.font == null || !label.text.Contains("Debug"))
                return Invalid(out error, EntryName + " needs a fonted Debug label.");
            if (!(entry is RectTransform entryRect) ||
                !Contained(entryRect, titleRoot as RectTransform))
                return Invalid(out error, EntryName + " must be a RectTransform inside " +
                                                    TitleRootName + ".");
            // The entry may sit over the window bounds once the runtime re-parents it: the
            // two are mutually exclusive (the controller hides the entry while the panel is
            // open), so only containment and the saved-inactive state have to hold.
            if (!AboveMapHud(canvas, root))
                return Invalid(out error, RootName + " must render above the map HUD and below " +
                                                    "the title and event overlays.");

            if (!Bound(controllers[0], "gameSession", sessions[0]) ||
                !Bound(controllers[0], "debugModeButton", entryButton) ||
                !Bound(controllers[0], "debugModeRoot", root.gameObject))
                return Invalid(out error,
                    "TitleScreenController references are not bound (gameSession, " +
                    "debugModeButton, debugModeRoot).");
            if (!Bound(controllers[0], "titleRoot", titleRoot.gameObject))
                return Invalid(out error,
                    "TitleScreenController.titleRoot must point at " + TitleRootName +
                    " so the guarded runtime can re-parent the entry.");

            return true;
        }

        /// <summary>
        /// Exposes the M6 title-screen cross-check the transaction runs, so a validation test
        /// can prove the integration cannot accept a scene that fails the M6 rules.
        /// </summary>
        public static bool ValidateTitleScreenOrdering(Scene scene, out string error) =>
            AddM6TitleScreen.Validate(scene, out error);

        /// <summary>Actionable one-line summary used by the scene validator report.</summary>
        public static string DescribeForReport(Scene scene) =>
            Validate(scene, out string error) ? "Debug Mode root, entry, and bindings." : error;

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
                Components<DebugModePanel>(scene).Length > 0 ||
                Named(scene, TitleRootName).Count() != 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            bool hasM09Objects = Named(scene, RootName).Any() || Named(scene, EntryName).Any();
            bool hasBindings = HasSerializedReference(controller, "debugModeButton") ||
                               HasSerializedReference(controller, "debugModeRoot");
            if (hasM09Objects || hasBindings)
            {
                // The previously integrated shape saved the entry as a Canvas child, which
                // changed the Canvas modal order. Repair it in place only when the existing
                // root, entry and controller references are exactly the objects this updater
                // created, so an unrelated hand-authored object is never rewritten.
                if (!TryMatchPreviousShape(scene, controller, out Transform priorRoot,
                        out Transform priorEntry))
                    return UpdateResult.ExistingPartialControl;
                return RunTransaction(scene, save, beforeValidation, () =>
                {
                    RepairEntryPlacement(priorRoot, priorEntry, controller);
                    return priorRoot.gameObject;
                }, priorEntry);
            }

            return RunTransaction(scene, save, beforeValidation, () =>
                CreateIntegration(scene, canvas, session, controller));
        }

        /// <summary>
        /// One Undo group, validation before saving, and Undo rollback on any exception
        /// without reloading the scene. The action builds or repairs the integration and
        /// returns the window root for the fault-injection seam.
        /// </summary>
        private static UpdateResult RunTransaction(Scene scene, bool save,
            Action<GameObject> beforeValidation, Func<GameObject> apply,
            Transform intentionallyMoved = null)
        {
            ObjectSnapshot[] originals = Snapshot(scene);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                GameObject rootObject = apply();
                beforeValidation?.Invoke(rootObject);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);
                // The entry lives under the M6 title root, so the transaction must also keep
                // the M6 title screen valid; otherwise it could save a scene whose modal
                // order the scene validator rejects.
                if (!AddM6TitleScreen.Validate(scene, out string titleError))
                    throw new InvalidOperationException("M6 title screen ordering broke: " +
                                                        titleError);

                failure = UpdateResult.OriginalObjectsChanged;
                if (!Preserved(originals, intentionallyMoved))
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
                Debug.LogWarning($"Debug Mode updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static GameObject CreateIntegration(Scene scene, Transform canvas,
            GameSession session, TitleScreenController controller)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                throw new InvalidOperationException("Missing built-in UI font.");
            Transform titleRoot = TitleOverlay(scene);
            if (titleRoot == null)
                throw new InvalidOperationException(
                    "The M6 title root is missing; run Tools > Sengoku Chronicle/Add M6 Title " +
                    "Screen first.");

            GameObject rootObject = Create(scene, canvas, RootName, typeof(Image));
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition3D = Vector3.zero;
            rootRect.sizeDelta = RootSize;
            rootRect.localScale = Vector3.one;
            rootRect.localRotation = Quaternion.identity;
            Image background = rootObject.GetComponent<Image>();
            background.color = SengokuUiTheme.IndigoDeep;
            background.raycastTarget = true;
            PlaceAboveMapHud(canvas, rootObject.transform);
            rootObject.SetActive(false);

            Button entry = CreateEntry(scene, titleRoot, font);
            Undo.RecordObject(entry, UndoName);
            entry.transform.SetAsLastSibling();
            entry.gameObject.SetActive(false);

            Undo.RecordObject(controller, UndoName);
            SetSerializedReference(controller, "gameSession", session);
            SetSerializedReference(controller, "debugModeButton", entry);
            SetSerializedReference(controller, "debugModeRoot", rootObject);
            SetSerializedReference(controller, "titleRoot", titleRoot.gameObject);
            EditorUtility.SetDirty(controller);
            return rootObject;
        }

        /// <summary>
        /// Moves the already-saved entry under the title root in the same Undo group as the
        /// rest of the integration. Nothing is deleted or recreated, so references and GUIDs
        /// stay stable.
        /// </summary>
        private static void RepairEntryPlacement(Transform root, Transform entry,
            TitleScreenController controller)
        {
            Transform titleRoot = TitleOverlay(entry.gameObject.scene);
            if (titleRoot == null)
                throw new InvalidOperationException("The M6 title root is missing.");
            if (entry.GetComponent<Button>() == null)
                throw new InvalidOperationException(EntryName + " has no Button component.");

            Undo.RecordObject(controller, UndoName);
            SetSerializedReference(controller, "titleRoot", titleRoot.gameObject);
            EditorUtility.SetDirty(controller);
            Undo.SetTransformParent(entry, titleRoot, UndoName);
            var rect = (RectTransform)entry;
            Undo.RecordObject(rect, UndoName);
            ApplyEntryRect(rect);
            entry.gameObject.SetActive(false);
            EditorUtility.SetDirty(entry.gameObject);
            EditorUtility.SetDirty(root.gameObject);
        }

        private static bool TryMatchPreviousShape(Scene scene, TitleScreenController controller,
            out Transform root, out Transform entry)
        {
            root = null;
            entry = null;
            Transform[] roots = Named(scene, RootName).ToArray();
            Transform[] entries = Named(scene, EntryName).ToArray();
            if (roots.Length != 1 || entries.Length != 1)
                return false;
            root = roots[0];
            entry = entries[0];
            if (root.parent == null || root.parent.name != CanvasName)
                return false;
            if (entry.parent == null || entry.parent.name != CanvasName)
                return false;
            if (TitleOverlay(scene) == null)
                return false;
            Button button = entry.GetComponent<Button>();
            if (button == null)
                return false;
            return Bound(controller, "debugModeButton", button) &&
                   Bound(controller, "debugModeRoot", root.gameObject);
        }

        /// <summary>
        /// One authoritative placement for the saved entry inside the title overlay. The
        /// guarded runtime re-parents it to the root Canvas without changing these anchors,
        /// so the same rect stays valid on the title page and over the game.
        /// </summary>
        private static void ApplyEntryRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition3D = new Vector3(EntryPosition.x, EntryPosition.y, 0f);
            rect.sizeDelta = EntrySize;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Button CreateEntry(Scene scene, Transform parent, Font font)
        {
            GameObject go = Create(scene, parent, EntryName, typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            ApplyEntryRect(rect);
            Image image = go.GetComponent<Image>();
            image.color = SengokuUiTheme.IndigoPanel;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject(EntryLabelName, typeof(RectTransform),
                typeof(Text));
            SceneManager.MoveGameObjectToScene(labelObject, scene);
            Undo.RegisterCreatedObjectUndo(labelObject, UndoName);
            Undo.SetTransformParent(labelObject.transform, go.transform, UndoName);
            labelObject.transform.localScale = Vector3.one;
            labelObject.layer = parent.gameObject.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 15;
            text.text = EntryLabel;
            text.color = SengokuUiTheme.ParchmentLight;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return button;
        }

        private static bool AboveMapHud(Transform canvas, Transform target)
        {
            int index = target.GetSiblingIndex();
            string[] above = { MapRootName };
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

        private static Transform TitleOverlay(Scene scene)
        {
            Transform[] matches = Named(scene, TitleRootName).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static bool Preserved(ObjectSnapshot[] originals, Transform intentionallyMoved)
        {
            foreach (ObjectSnapshot original in originals)
            {
                Transform transform = original.Transform;
                if (transform == null)
                    return false;
                if (!string.Equals(transform.name, original.Name, StringComparison.Ordinal))
                    return false;
                if (transform.gameObject.scene != original.Scene)
                    return false;
                if (!ComponentSetMatches(transform, original.ComponentTypes))
                    return false;
                // A repair moves exactly one existing object (the saved entry) under the title
                // root. Only its parent and active flag may change; its identity, scene and
                // component set are still checked above and its references are re-validated.
                if (transform == intentionallyMoved)
                    continue;
                if (transform.gameObject.activeSelf != original.Active)
                    return false;
                if (transform.parent != original.Parent)
                    return false;
            }
            return true;
        }

        private static bool ComponentSetMatches(Transform transform, string[] expected)
        {
            Component[] components = transform.GetComponents<Component>();
            if (components.Length != expected.Length)
                return false;
            for (int i = 0; i < components.Length; i++)
            {
                string actual = components[i] == null ? "<missing>" : components[i].GetType().Name;
                if (!string.Equals(actual, expected[i], StringComparison.Ordinal))
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
                    Active = transform.gameObject.activeSelf,
                    Scene = transform.gameObject.scene,
                    ComponentTypes = transform.GetComponents<Component>()
                        .Select(component => component == null ? "<missing>"
                            : component.GetType().Name).ToArray()
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
            public Scene Scene;
            public string[] ComponentTypes;
        }

        private static void SetSerializedReference(UnityEngine.Object target, string field,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException(
                    $"Serialized field '{field}' is missing on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasSerializedReference(UnityEngine.Object target, string field)
        {
            if (target == null)
                return false;
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && property.objectReferenceValue != null;
        }

        private static bool Bound(UnityEngine.Object target, string field,
            UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
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

        private static IEnumerable<Transform> Named(Scene scene, string name) =>
            AllTransforms(scene).Where(transform => transform.name == name);

        private static Transform[] AllTransforms(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

        private static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
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

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
