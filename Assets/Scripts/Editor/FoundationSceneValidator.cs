using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>Writes a text-only hierarchy, binding, and RectTransform report for FoundationScene.</summary>
    public static class FoundationSceneValidator
    {
        private const string ReportRelativePath = "Logs/SceneValidationReport.txt";

        [MenuItem("Tools/Sengoku Chronicle/Validate Foundation Scene")]
        public static void ValidateActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            var report = new StringBuilder(8192);
            int errors = 0;
            report.AppendLine("FOUNDATION SCENE VALIDATION");
            report.AppendLine($"Scene: {scene.path}");
            report.AppendLine($"Loaded: {scene.isLoaded}; Dirty: {scene.isDirty}");

            if (scene.path != "Assets/Scenes/FoundationScene.unity")
                AddError(report, ref errors, "Active scene is not FoundationScene.");

            GameObject canvas = GameObject.Find("Canvas");
            Transform mapRoot = canvas != null ? canvas.transform.Find("M2WorldMapRoot") : null;
            Transform townRoot = mapRoot != null ? mapRoot.Find("TownMenuRoot") : null;
            WorldMapController controller = mapRoot != null ? mapRoot.GetComponent<WorldMapController>() : null;
            TownNpcDialoguePanel panel = FindComponent<TownNpcDialoguePanel>(scene);
            SupplyShortageMissionPanel missionPanel = FindComponent<SupplyShortageMissionPanel>(scene);
            SaveLoadPanel saveLoadPanel = FindComponent<SaveLoadPanel>(scene);
            TitleScreenController titleController = FindComponent<TitleScreenController>(scene);
            DemoSummaryPanel demoPanel = FindComponent<DemoSummaryPanel>(scene);
            RankPanel rankPanel = FindComponent<RankPanel>(scene);

            CheckFound(report, ref errors, "Canvas", canvas);
            CheckFound(report, ref errors, "Canvas/M2WorldMapRoot", mapRoot);
            CheckFound(report, ref errors, "Canvas/M2WorldMapRoot/TownMenuRoot", townRoot);
            CheckFound(report, ref errors, "WorldMapController", controller);
            CheckFound(report, ref errors, "TownNpcDialoguePanel", panel);
            CheckFound(report, ref errors, "SupplyShortageMissionPanel", missionPanel);
            CheckFound(report, ref errors, "SaveLoadPanel", saveLoadPanel);
            CheckFound(report, ref errors, "TitleScreenController", titleController);
            CheckFound(report, ref errors, "DemoSummaryPanel", demoPanel);
            CheckFound(report, ref errors, "RankPanel", rankPanel);
            if (!AddM4SupplyShortageMissionPanel.Validate(scene, out string missionError))
                AddError(report, ref errors, missionError);
            else
                report.AppendLine("PASS M4a mission hierarchy, unique objects, bindings, and containment.");
            if (!AddM4bMissionBoard.Validate(scene, out string m4bError))
                AddError(report, ref errors, m4bError);
            else
                report.AppendLine("PASS M4b mission board hierarchy, unique controls, bindings, and containment.");
            if (!AddM5SaveLoadPanel.Validate(scene, out string m5Error))
                AddError(report, ref errors, m5Error);
            else
                report.AppendLine("PASS M5 save/load hierarchy, unique controls, bindings, and map separation.");
            if (!AddM6TitleScreen.Validate(scene, out string m6Error))
                AddError(report, ref errors, m6Error);
            else
                report.AppendLine("PASS M6 title screen hierarchy, bindings, gating, and demo summary.");
            if (!AddM7RankPanel.Validate(scene, out string m7Error))
                AddError(report, ref errors, m7Error);
            else
                report.AppendLine("PASS M7 rank panel hierarchy, bindings, containment, and map separation.");

            if (controller != null)
            {
                report.AppendLine("\nWORLD MAP CONTROLLER REFERENCES:");
                CheckReferences(report, ref errors, controller,
                    "gameSession", "worldClockPanel", "locationMarkerText", "feedbackText", "currentDateText",
                    "destinationNameText", "travelTimeText", "arrivalDateText", "confirmButton", "cancelButton",
                    "returnToMapButton", "kiyosuButton", "atsutaButton", "nagoyaButton", "townMenuRoot",
                    "townMenuTitleText", "facility1Text", "facility2Text", "facility3Text",
                    "townNpcDialoguePanel", "openNpcRosterButton", "supplyShortageMissionPanel");
            }

            if (panel != null)
            {
                report.AppendLine("\nTOWN NPC PANEL REFERENCES:");
                CheckReferences(report, ref errors, panel,
                    "townMenuRoot", "rosterRoot", "rosterContent", "npcRowPrefab", "rosterStatusText",
                    "dialogueRoot", "dialogueText", "optionsContent", "optionButtonPrefab",
                    "backToRosterButton", "rosterBackToTownButton", "backToTownButton");
            }

            if (missionPanel != null)
            {
                report.AppendLine("\nSUPPLY SHORTAGE MISSION PANEL REFERENCES:");
                CheckReferences(report, ref errors, missionPanel,
                    "gameSession", "panelRoot", "stageText", "objectiveText", "meritText", "feedbackText",
                    "acceptButton", "investigateButton", "negotiateButton", "reportButton",
                    "sealedLetterStageText", "sealedLetterObjectiveText", "sealedLetterFeedbackText", "sealedLetterActionButton",
                    "marketTallyStageText", "marketTallyObjectiveText", "marketTallyFeedbackText", "marketTallyActionButton");
            }

            if (saveLoadPanel != null)
            {
                report.AppendLine("\nSAVE/LOAD PANEL REFERENCES:");
                CheckReferences(report, ref errors, saveLoadPanel,
                    "gameSession", "worldMapController", "saveButton", "loadButton", "statusText");
            }

            if (titleController != null)
            {
                report.AppendLine("\nTITLE SCREEN CONTROLLER REFERENCES:");
                CheckReferences(report, ref errors, titleController,
                    "gameSession", "worldMapController", "titleRoot", "newGameButton", "continueButton", "statusText");
            }

            if (demoPanel != null)
            {
                report.AppendLine("\nDEMO SUMMARY PANEL REFERENCES:");
                CheckReferences(report, ref errors, demoPanel,
                    "gameSession", "summaryText");
            }

            if (rankPanel != null)
            {
                report.AppendLine("\nRANK PANEL REFERENCES:");
                CheckReferences(report, ref errors, rankPanel,
                    "gameSession", "rankText");
            }

            report.AppendLine("\nHIERARCHY AND RECTTRANSFORMS:");
            foreach (GameObject root in scene.GetRootGameObjects())
                AppendHierarchy(report, root.transform, 0);

            report.AppendLine($"\nRESULT: {(errors == 0 ? "PASS" : "FAIL")}; Errors: {errors}");
            string text = report.ToString();
            string path = Path.Combine(UnityEngine.Application.dataPath, "..", ReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text, new UTF8Encoding(false));
            Debug.Log(text);
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static void CheckFound(StringBuilder report, ref int errors, string name, Object value)
        {
            if (value == null)
                AddError(report, ref errors, $"Missing {name}.");
            else
                report.AppendLine($"FOUND {name}: {value.name}");
        }

        private static void CheckReferences(StringBuilder report, ref int errors, Object target, params string[] names)
        {
            var serialized = new SerializedObject(target);
            foreach (string name in names)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property == null)
                {
                    AddError(report, ref errors, $"Serialized field '{name}' was not found on {target.name}.");
                    continue;
                }

                Object reference = property.objectReferenceValue;
                if (reference == null)
                    AddError(report, ref errors, $"Unassigned reference {target.name}.{name}.");
                else
                    report.AppendLine($"BOUND {target.name}.{name} -> {reference.name} ({reference.GetType().Name})");
            }
        }

        private static void AppendHierarchy(StringBuilder report, Transform transform, int depth)
        {
            report.Append(' ', depth * 2).Append(transform.name);
            report.Append(transform.gameObject.activeInHierarchy ? " [active]" : " [inactive]");
            foreach (Component component in transform.GetComponents<Component>())
                report.Append(" | ").Append(component != null ? component.GetType().Name : "<missing>");

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                report.Append($" | anchors={rect.anchorMin:F2}..{rect.anchorMax:F2}");
                report.Append($" pivot={rect.pivot:F2} pos={rect.anchoredPosition:F1}");
                report.Append($" size={rect.sizeDelta:F1} rect={rect.rect.width:F1}x{rect.rect.height:F1}");
            }
            report.AppendLine();

            for (int i = 0; i < transform.childCount; i++)
                AppendHierarchy(report, transform.GetChild(i), depth + 1);
        }

        private static void AddError(StringBuilder report, ref int errors, string message)
        {
            errors++;
            report.AppendLine($"ERROR: {message}");
        }
    }
}
