using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>Adds a single town overview beneath the existing town root.</summary>
    public static class UpdateTownOverview
    {
        private const string MenuPath = "Tools/Sengoku Chronicle/Update Town Overview";
        private const string RootName = "TownOverviewRoot";
        private const string UndoName = "Update Town Overview";

        public enum UpdateResult { Success, WrongScene, DirtyScene, Playing, MissingBindings, AlreadyExists, Invalid, Failed }

        [MenuItem(MenuPath)]
        private static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.Playing
                : scene.path != "Assets/Scenes/FoundationScene.unity" ? UpdateResult.WrongScene
                : scene.isDirty ? UpdateResult.DirtyScene : Execute(scene, true);
            if (result == UpdateResult.Success)
                Debug.Log("Town overview was validated and saved under the existing TownMenuRoot.");
            else
                Debug.LogError($"Town overview update refused or rolled back: {result}.");
        }

        public static UpdateResult Update(Scene scene) => Execute(scene, false);

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            if (!scene.IsValid()) { error = "Scene is invalid."; return false; }
            GameObject[] roots = scene.GetRootGameObjects();
            Transform town = roots.SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "TownMenuRoot");
            Transform overview = town != null ? town.Find(RootName) : null;
            TownOverviewPanel panel = overview != null ? overview.GetComponent<TownOverviewPanel>() : null;
            if (town == null || overview == null || panel == null || overview.parent != town ||
                overview.Find("TownArt") == null || overview.Find("TownName") == null ||
                overview.Find("TownFeature") == null || overview.Find("Residents") == null ||
                overview.Find("Rumors") == null || overview.Find("RosterButton") == null ||
                overview.Find("MissionButton") == null)
            {
                error = "Town overview root, required labels, art, or actions are missing.";
                return false;
            }
            var controller = town.parent != null ? town.parent.GetComponent<WorldMapController>() : null;
            if (controller == null || !Bound(controller, "townOverviewPanel", panel))
            {
                error = "Town overview is not bound to its WorldMapController.";
                return false;
            }
            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save)
        {
            if (!scene.IsValid()) return UpdateResult.Invalid;
            Transform town = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "TownMenuRoot");
            Transform map = town != null ? town.parent : null;
            WorldMapController controller = map != null ? map.GetComponent<WorldMapController>() : null;
            Button returnButton = town != null ? town.Find("ReturnToMapButton")?.GetComponent<Button>() : null;
            GameObject sessionObject = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "GameSession")?.gameObject;
            if (town == null || controller == null || returnButton == null || sessionObject == null)
                return UpdateResult.MissingBindings;
            Transform existing = town.Find(RootName);
            if (existing != null)
            {
                if (!Validate(scene, out _)) return UpdateResult.AlreadyExists;
                return UpdateExisting(scene, existing.gameObject, returnButton.gameObject, save);
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Built-in UI font is unavailable.");
                GameObject root = Create(scene, town, RootName, typeof(Image), typeof(TownOverviewPanel));
                RectTransform rootRect = (RectTransform)root.transform;
                Stretch(rootRect, 0f, 0f, 0f, 0f);
                Image background = root.GetComponent<Image>();
                background.color = new Color(0.88f, 0.83f, 0.70f, 0.98f);
                SengokuUiArt.ApplyParchmentSurface(background);

                Text title = CreateText(scene, rootRect, "TownName", font, 34, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(520f, 56f), TextAnchor.MiddleCenter);
                Text feature = CreateText(scene, rootRect, "TownFeature", font, 20, new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(740f, 42f), TextAnchor.MiddleCenter);
                Image art = CreateImage(scene, rootRect, "TownArt", new Vector2(0.5f, 0.51f),
                    new Vector2(0.5f, 0.51f), new Vector2(0f, 50f), new Vector2(920f, 420f));
                art.preserveAspect = true;
                art.transform.SetAsFirstSibling();
                Text residents = CreateText(scene, rootRect, "Residents", font, 20, new Vector2(0f, 0f),
                    new Vector2(0f, 0f), new Vector2(28f, 80f), new Vector2(300f, 80f), TextAnchor.UpperLeft);
                Text rumors = CreateText(scene, rootRect, "Rumors", font, 19, new Vector2(1f, 0f),
                    new Vector2(1f, 0f), new Vector2(-28f, 80f), new Vector2(300f, 80f), TextAnchor.UpperLeft);
                Button roster = CreateButton(scene, rootRect, "RosterButton", "当地人物", font,
                    new Vector2(0.5f, 0f), new Vector2(-300f, 42f));
                Button mission = CreateButton(scene, rootRect, "MissionButton", "任务委托", font,
                    new Vector2(0.5f, 0f), new Vector2(0f, 42f));
                ApplyLayout(root, returnButton.gameObject);

                TownOverviewPanel panel = root.GetComponent<TownOverviewPanel>();
                panel.BindReferences(art, title, feature, residents, rumors, roster, mission, returnButton);
                Undo.RecordObject(controller, UndoName);
                controller.BindTownOverviewPanel(panel);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(panel);
                if (!Validate(scene, out string error)) throw new InvalidOperationException(error);
                EditorSceneManager.MarkSceneDirty(scene);
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");
                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogWarning($"Town overview rolled back: {exception.Message}");
                return UpdateResult.Failed;
            }
        }

        private static UpdateResult UpdateExisting(Scene scene, GameObject root, GameObject returnButton, bool save)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                Undo.RegisterFullObjectHierarchyUndo(root, UndoName);
                Undo.RegisterFullObjectHierarchyUndo(returnButton, UndoName);
                ApplyLayout(root, returnButton);
                if (!Validate(scene, out string error)) throw new InvalidOperationException(error);
                EditorSceneManager.MarkSceneDirty(scene);
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");
                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogWarning($"Town overview layout rolled back: {exception.Message}");
                return UpdateResult.Failed;
            }
        }

        private static void ApplyLayout(GameObject root, GameObject returnButton)
        {
            RectTransform art = (RectTransform)root.transform.Find("TownArt");
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.51f);
            art.anchoredPosition = new Vector2(0f, 50f);
            art.sizeDelta = new Vector2(920f, 420f);

            RectTransform residents = (RectTransform)root.transform.Find("Residents");
            residents.anchoredPosition = new Vector2(28f, 80f);
            residents.sizeDelta = new Vector2(300f, 80f);
            RectTransform rumors = (RectTransform)root.transform.Find("Rumors");
            rumors.anchoredPosition = new Vector2(-28f, 80f);
            rumors.sizeDelta = new Vector2(300f, 80f);

            PlaceAction(root.transform.Find("RosterButton").GetComponent<RectTransform>(), -300f);
            PlaceAction(root.transform.Find("MissionButton").GetComponent<RectTransform>(), 0f);
            PlaceAction(returnButton.GetComponent<RectTransform>(), 300f);
            Text returnLabel = returnButton.GetComponentInChildren<Text>(true);
            if (returnLabel != null) returnLabel.text = "返回地图";
            returnButton.transform.SetAsLastSibling();
        }

        private static void PlaceAction(RectTransform rect, float x)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, 42f);
            rect.sizeDelta = new Vector2(260f, 60f);
        }

        private static GameObject Create(Scene scene, Transform parent, string name, params Type[] components)
        {
            var obj = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(obj, scene);
            Undo.RegisterCreatedObjectUndo(obj, UndoName);
            Undo.SetTransformParent(obj.transform, parent, UndoName);
            return obj;
        }

        private static Image CreateImage(Scene scene, Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject obj = Create(scene, parent, name, typeof(Image));
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>(); image.color = Color.white; image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font, int size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions, TextAnchor alignment)
        {
            GameObject obj = Create(scene, parent, name, typeof(Text));
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            Text text = obj.GetComponent<Text>(); text.font = font; text.fontSize = size;
            text.color = new Color(0.13f, 0.16f, 0.19f, 1f); text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, string label, Font font,
            Vector2 anchor, Vector2 position)
        {
            GameObject obj = Create(scene, parent, name, typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(300f, 68f);
            Image image = obj.GetComponent<Image>(); image.color = new Color(0.20f, 0.24f, 0.29f, 1f);
            Button button = obj.GetComponent<Button>(); button.targetGraphic = image;
            Text text = CreateText(scene, rect, "Label", font, 22, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            text.color = new Color(0.92f, 0.86f, 0.69f, 1f); text.text = label;
            return button;
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }

        private static bool Bound(UnityEngine.Object owner, string fieldName, UnityEngine.Object expected)
        {
            var property = new SerializedObject(owner).FindProperty(fieldName);
            return property != null && property.objectReferenceValue == expected;
        }
    }
}
