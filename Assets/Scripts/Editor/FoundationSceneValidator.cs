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
            SuccessionPanel successionPanel = FindComponent<SuccessionPanel>(scene);
            FamilyChroniclePanel chroniclePanel = FindComponent<FamilyChroniclePanel>(scene);
            FamilyRosterPanel rosterPanel = FindComponent<FamilyRosterPanel>(scene);
            M2EndingPanel endingPanel = FindComponent<M2EndingPanel>(scene);
            IndustryPanel industryPanel = FindComponent<IndustryPanel>(scene);
            CharacterEducationPanel educationPanel = FindComponent<CharacterEducationPanel>(scene);
            FamilyResearchPanel researchPanel = FindComponent<FamilyResearchPanel>(scene);
            FamilyFormationPanel formationPanel = FindComponent<FamilyFormationPanel>(scene);
            FamilyDoctrinePanel doctrinePanel = FindComponent<FamilyDoctrinePanel>(scene);
            DailyActionPanel dailyActionPanel = FindComponent<DailyActionPanel>(scene);
            NpcAiInspectorPanel npcAiInspectorPanel = FindComponent<NpcAiInspectorPanel>(scene);

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
            CheckFound(report, ref errors, "SuccessionPanel", successionPanel);
            CheckFound(report, ref errors, "FamilyChroniclePanel", chroniclePanel);
            CheckFound(report, ref errors, "FamilyRosterPanel", rosterPanel);
            CheckFound(report, ref errors, "M2EndingPanel", endingPanel);
            CheckFound(report, ref errors, "IndustryPanel", industryPanel);
            CheckFound(report, ref errors, "CharacterEducationPanel", educationPanel);
            CheckFound(report, ref errors, "FamilyResearchPanel", researchPanel);
            CheckFound(report, ref errors, "FamilyFormationPanel", formationPanel);
            CheckFound(report, ref errors, "FamilyDoctrinePanel", doctrinePanel);
            CheckFound(report, ref errors, "DailyActionPanel", dailyActionPanel);
            CheckFound(report, ref errors, "NpcAiInspectorPanel", npcAiInspectorPanel);
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
            if (!AddSC004MonthTurnControl.Validate(scene, out string sc004Error))
                AddError(report, ref errors, sc004Error);
            else
                report.AppendLine("PASS SC-004 month turn control hierarchy, bindings, and day-control preservation.");
            if (!AddSC005EventChoicePanel.Validate(scene, out string sc005Error))
                AddError(report, ref errors, sc005Error);
            else
                report.AppendLine("PASS SC-009 event-choice panel hierarchy, bindings, layout, and validated content.");
            if (!AddSC010SuccessionPanel.Validate(scene, out string sc010Error))
                AddError(report, ref errors, sc010Error);
            else
                report.AppendLine("PASS SC-010 succession panel hierarchy, bindings, ordering, raycast, and layout.");
            if (!AddSC012FamilyChroniclePanel.Validate(scene, out string sc012Error))
                AddError(report, ref errors, sc012Error);
            else
                report.AppendLine("PASS SC-012 family chronicle panel hierarchy, bindings, scroll content, ordering, and layout.");
            if (!AddSC013FamilyRosterPanel.Validate(scene, out string sc013Error))
                AddError(report, ref errors, sc013Error);
            else
                report.AppendLine("PASS SC-013 family roster and ending panel hierarchy, bindings, ordering, and layout.");
            if (!AddSC013TitleScenarioButtons.Validate(scene, out string sc013TitleError))
                AddError(report, ref errors, sc013TitleError);
            else
                report.AppendLine("PASS SC-013 title scenario entries, bindings, and layout.");
            if (!AddSC019IndustryPanel.Validate(scene, out string sc019Error))
                AddError(report, ref errors, sc019Error);
            else
                report.AppendLine("PASS SC-019 industry panel hierarchy, bindings, ordering, and layout.");
            if (!AddSC020CharacterEducation.Validate(scene, out string sc020Error))
                AddError(report, ref errors, sc020Error);
            else
                report.AppendLine("PASS SC-020 character-education panel, title entry, bindings, and layout.");
            if (!AddSC021FamilyResearch.Validate(scene, out string sc021Error))
                AddError(report, ref errors, sc021Error);
            else
                report.AppendLine("PASS SC-021 family-research panel, title entry, bindings, and layout.");
            if (!AddSC022FamilyFormation.Validate(scene, out string sc022Error))
                AddError(report, ref errors, sc022Error);
            else
                report.AppendLine("PASS SC-022 family-formation panel, title entry, bindings, and layout.");
            if (!AddSC023Seclusion.Validate(scene, out string sc023Error))
                AddError(report, ref errors, sc023Error);
            else
                report.AppendLine("PASS SC-023 seclusion title entry, education-panel dependency, bindings, and layout.");
            if (!AddSC024FamilyDoctrine.Validate(scene, out string sc024Error))
                AddError(report, ref errors, sc024Error);
            else
                report.AppendLine("PASS SC-024 family-doctrine panel, title entry, bindings, containment, and layout.");
            if (!AddM02DailyActionPanel.Validate(scene, out string m02Error))
                AddError(report, ref errors, m02Error);
            else
                report.AppendLine("PASS M02 daily-action panel hierarchy, bindings, containment, ordering, and layout.");
            if (!AddM03NpcAiInspector.Validate(scene, out string m03Error))
                AddError(report, ref errors, m03Error);
            else
                report.AppendLine("PASS M03 read-only NPC Inspector hierarchy, bindings, containment, ordering, and layout.");

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
                    "gameSession", "worldMapController", "titleRoot", "newGameButton", "continueButton",
                    "standardSampleButton", "crisisDemoButton", "educationDemoButton",
                    "researchDemoButton", "familyFormationDemoButton", "seclusionDemoButton",
                    "statusText");
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

            if (successionPanel != null)
            {
                report.AppendLine("\nSUCCESSION PANEL REFERENCES:");
                CheckReferences(report, ref errors, successionPanel,
                    "gameSession", "modalRoot", "headText", "statusText", "retireButton",
                    "crisisDemoButton", "feedbackText", "modalStatusText", "candidateContent",
                    "candidateButtonPrefab", "regentContent", "regentButtonPrefab", "confirmButton",
                    "acceptCrisisButton", "declineCrisisButton");
            }

            if (chroniclePanel != null)
            {
                report.AppendLine("\nFAMILY CHRONICLE PANEL REFERENCES:");
                CheckReferences(report, ref errors, chroniclePanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "scrollRect", "content",
                    "rowTemplate", "emptyText", "titleText");
            }

            if (rosterPanel != null)
            {
                report.AppendLine("\nFAMILY GRAPH PANEL REFERENCES:");
                CheckReferences(report, ref errors, rosterPanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "titleText", "legendText",
                    "scrollRect", "content", "edgeLayer", "nodeLayer", "nodeTemplate", "edgeTemplate",
                    "sectionTemplate", "heirText");
            }

            if (endingPanel != null)
            {
                report.AppendLine("\nM2 ENDING PANEL REFERENCES:");
                CheckReferences(report, ref errors, endingPanel,
                    "gameSession", "panelRoot", "titleText", "bodyText", "closeButton");
            }

            if (industryPanel != null)
            {
                report.AppendLine("\nINDUSTRY PANEL REFERENCES:");
                CheckReferences(report, ref errors, industryPanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "titleText",
                    "resourceText", "blockReasonText", "messageText", "reportText", "rowFarm",
                    "rowTradingHouse",
                    "farmStatusText", "farmAssigneeText", "farmUnassignButton", "farmCandidateContainer",
                    "tradingStatusText", "tradingAssigneeText", "tradingUnassignButton",
                    "tradingCandidateContainer", "candidateTemplate");
            }

            if (educationPanel != null)
            {
                report.AppendLine("\nCHARACTER EDUCATION PANEL REFERENCES:");
                CheckReferences(report, ref errors, educationPanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "overviewText",
                    "directionContainer", "directionTemplate", "mentorContainer", "mentorTemplate",
                    "previewText", "confirmButton", "messageText");
            }

            if (researchPanel != null)
            {
                report.AppendLine("\nFAMILY RESEARCH PANEL REFERENCES:");
                CheckReferences(report, ref errors, researchPanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "titleText",
                    "resourceText", "techniqueText", "statusText", "progressText", "costText",
                    "blockReasonText", "messageText", "reportText", "researcherContainer",
                    "researcherTemplate");
            }

            if (formationPanel != null)
            {
                report.AppendLine("\nFAMILY FORMATION PANEL REFERENCES:");
                CheckReferences(report, ref errors, formationPanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "titleText",
                    "blockReasonText", "marriageMemberInfo", "marriageMemberContainer",
                    "marriageMemberTemplate", "marriageCandidateInfo",
                    "marriageCandidateContainer", "marriageCandidateTemplate",
                    "marriageConfirmButton", "marriageMessage", "adoptionMemberInfo",
                    "adoptionMemberContainer", "adoptionMemberTemplate", "adoptionCandidateInfo",
                    "adoptionCandidateContainer", "adoptionCandidateTemplate",
                    "adoptionConfirmButton", "adoptionMessage");
            }

            if (doctrinePanel != null)
            {
                report.AppendLine("\nFAMILY DOCTRINE PANEL REFERENCES:");
                CheckReferences(report, ref errors, doctrinePanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "titleText",
                    "creedText", "creedChoicesText", "effectText", "headText", "ambitionText",
                    "blockReasonText", "messageText", "developHouseholdButton",
                    "passDownTechniqueButton", "educateHeirButton");
            }

            if (dailyActionPanel != null)
            {
                report.AppendLine("\nDAILY ACTION PANEL REFERENCES:");
                CheckReferences(report, ref errors, dailyActionPanel,
                    "gameSession", "panelRoot", "openButton", "closeButton", "titleText",
                    "summaryText", "scrollRect", "content", "rowTemplate", "emptyText",
                    "selectedText", "durationInput", "restButton", "trainingButton",
                    "workButton", "travelKiyosuButton", "travelAtsutaButton",
                    "travelNagoyaButton", "interruptButton", "feedbackText");
            }

            if (npcAiInspectorPanel != null)
            {
                report.AppendLine("\nNPC AI INSPECTOR REFERENCES:");
                CheckReferences(report, ref errors, npcAiInspectorPanel,
                    "gameSession", "panelRoot", "openButton", "closeButton",
                    "masatsunaButton", "nobumasaButton", "titleText", "summaryText",
                    "needsText", "statusText", "candidatesText", "logText");
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
