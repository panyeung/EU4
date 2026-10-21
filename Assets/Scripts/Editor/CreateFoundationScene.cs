using System;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// 通过 Unity Editor API 创建独立的 FoundationScene。
    /// 不手改 .unity / .prefab YAML；不触碰 SampleScene 与 Build Settings。
    /// 在 Unity 菜单 Tools → Sengoku Chronicle → Create Foundation Scene 执行。
    /// </summary>
    public static class CreateFoundationScene
    {
        private const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        private const string MenuPath = "Tools/Sengoku Chronicle/Create Foundation Scene";

        [MenuItem(MenuPath, priority = 1)]
        public static void Create()
        {
            // 1. 新建空白场景（EmptyScene，脚本自建相机；不修改 SampleScene）
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (scene == null)
            {
                Debug.LogError("CreateFoundationScene: 无法新建场景。");
                return;
            }

            // 2. 创建相机（正交，便于 2D 预览）
            var cameraGo = new GameObject("Main Camera");
            var cam = cameraGo.AddComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("CreateFoundationScene: AddComponent<Camera>() 返回 null，无法继续。");
                return;
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.transform.position = new Vector3(0, 0, -10f);
            cameraGo.tag = "MainCamera";
            if (cameraGo.GetComponent<AudioListener>() == null)
                cameraGo.AddComponent<AudioListener>();

            // 3. 创建 EventSystem（uGUI 必需）。
            // 项目 activeInputHandler = 1（仅 Input System），因此必须使用
            // InputSystemUIInputModule；StandaloneInputModule 在 Input System
            // 独占模式下读取 UnityEngine.Input 会抛异常。
            var eventSystemGo = new GameObject("EventSystem");
            if (eventSystemGo.GetComponent<EventSystem>() == null)
                eventSystemGo.AddComponent<EventSystem>();
            if (eventSystemGo.GetComponent<InputSystemUIInputModule>() == null)
                eventSystemGo.AddComponent<InputSystemUIInputModule>();

            // 4. 创建 Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.transform.SetAsLastSibling();

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 5. 创建 GameSession（新局初始化，日期 1560-04-01）
            var sessionGo = new GameObject("GameSession");
            var session = sessionGo.AddComponent<GameSession>();

            // 6. 创建 WorldClockPanel 及其 UI
            var panelGo = new GameObject("WorldClockPanel");
            panelGo.transform.parent = canvasGo.transform;
            panelGo.transform.localScale = Vector3.one;
            panelGo.AddComponent<RectTransform>();
            var panel = panelGo.AddComponent<WorldClockPanel>();
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0, 40f);
            panelRect.sizeDelta = new Vector2(560, 200);

            // 按钮
            var buttonGo = new GameObject("AdvanceButton");
            buttonGo.transform.parent = panelGo.transform;
            buttonGo.transform.localScale = Vector3.one;
            buttonGo.AddComponent<RectTransform>();
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0, -8f);
            buttonRect.sizeDelta = new Vector2(300, 80);

            var button = buttonGo.AddComponent<Button>();
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.18f, 0.18f, 0.2f, 1f);

            var labelGo = new GameObject("Label");
            labelGo.transform.parent = buttonGo.transform;
            labelGo.transform.localScale = Vector3.one;
            labelGo.AddComponent<RectTransform>();
            var label = labelGo.AddComponent<Text>();
            label.text = "推进一天";
            label.color = Color.white;
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            // 日期显示
            var dateGo = new GameObject("DateText");
            dateGo.transform.parent = panelGo.transform;
            dateGo.transform.localScale = Vector3.one;
            dateGo.AddComponent<RectTransform>();
            var dateText = dateGo.AddComponent<Text>();
            dateText.color = Color.white;
            dateText.fontSize = 32;
            dateText.alignment = TextAnchor.UpperCenter;
            dateText.font = GetDefaultFont();
            var dateRt = dateGo.GetComponent<RectTransform>();
            dateRt.anchorMin = new Vector2(0.5f, 1f);
            dateRt.anchorMax = new Vector2(0.5f, 1f);
            dateRt.pivot = new Vector2(0.5f, 1f);
            dateRt.anchoredPosition = new Vector2(0, 0);
            dateRt.sizeDelta = new Vector2(400, 60);

            // 一次性注入面板依赖（避免直接访问私有字段）
            panel.SetDependencies(session, button, dateText);

            // 7. 保存场景（仅 FoundationScene，不改 Build Settings / SampleScene）
            EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.Refresh();
            Debug.Log($"CreateFoundationScene: 已创建 {ScenePath}。在 Project 窗口双击即可打开并运行 Play Mode。");
        }

        private static Font GetDefaultFont()
        {
            // Unity 6000.6 no longer ships Arial.ttf as a built-in font resource;
            // requesting it throws ArgumentException instead of returning null.
            // Use the supported LegacyRuntime.ttf directly.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }

        [MenuItem(MenuPath, priority = 1, validate = true)]
        private static bool CreateValidate() => true;
    }
}
