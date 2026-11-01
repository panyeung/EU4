using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>Displays mission progress and forwards input to GameSession.</summary>
    [DisallowMultipleComponent]
    public sealed class SupplyShortageMissionPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text stageText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text meritText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button investigateButton;
        [SerializeField] private Button negotiateButton;
        [SerializeField] private Button reportButton;

        public bool IsOpen { get; private set; }
        private string _feedback = string.Empty;

        public void Bind(GameSession session)
        {
            if (gameSession != session)
                _feedback = string.Empty;
            gameSession = session;
            Refresh();
        }

        public void BindReferences(GameObject root, Text stage, Text objective,
            Text merit, Text feedback, Button accept, Button investigate,
            Button negotiate, Button report)
        {
            BindListeners(false);
            panelRoot = root;
            stageText = stage;
            objectiveText = objective;
            meritText = merit;
            feedbackText = feedback;
            acceptButton = accept;
            investigateButton = investigate;
            negotiateButton = negotiate;
            reportButton = report;
            BindListeners(isActiveAndEnabled);
            if (panelRoot != null)
                panelRoot.SetActive(IsOpen);
            Refresh();
        }

        public void Open()
        {
            IsOpen = true;
            if (panelRoot != null)
                panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            Refresh();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Refresh()
        {
            bool hasSession = gameSession != null;
            SupplyShortageStage stage = hasSession
                ? gameSession.SupplyShortageStage : SupplyShortageStage.NotAccepted;
            string stageLabel = "未领取";
            string objective = "在清洲领取补给短缺任务。";
            switch (stage)
            {
                case SupplyShortageStage.Accepted:
                    stageLabel = "已领取";
                    objective = "前往热田调查补给短缺。";
                    break;
                case SupplyShortageStage.Investigated:
                    stageLabel = "已调查";
                    objective = "在热田与黑田伊织交涉补给。";
                    break;
                case SupplyShortageStage.Negotiated:
                    stageLabel = "已交涉";
                    objective = "返回清洲报告。";
                    break;
                case SupplyShortageStage.RewardClaimed:
                    stageLabel = "已领取奖励";
                    objective = "补给短缺任务已完成。";
                    break;
            }
            if (stageText != null)
                stageText.text = hasSession ? $"补给短缺：{stageLabel}" : "补给短缺：不可用";
            if (objectiveText != null)
                objectiveText.text = hasSession ? objective : "缺少 GameSession。";
            if (meritText != null)
                meritText.text = hasSession ? $"功绩：{gameSession.Merit}" : "功绩：—";
            if (feedbackText != null)
                feedbackText.text = _feedback;

            LocationId location = hasSession ? gameSession.CurrentLocation : default;
            // Visibility is a UI hint; GameSession validates every submitted action.
            SetAction(acceptButton, hasSession && stage == SupplyShortageStage.NotAccepted,
                location == LocationId.Kiyosu);
            SetAction(investigateButton, hasSession && stage == SupplyShortageStage.Accepted,
                location == LocationId.Atsuta);
            SetAction(negotiateButton, hasSession && stage == SupplyShortageStage.Investigated,
                location == LocationId.Atsuta);
            SetAction(reportButton, hasSession && stage == SupplyShortageStage.Negotiated,
                location == LocationId.Kiyosu);
        }

        public void AcceptMission()
        {
            if (HasSession()) ShowResult(gameSession.TryAcceptSupplyShortageMission());
        }

        public void Investigate()
        {
            if (HasSession()) ShowResult(gameSession.TryInvestigateSupplyShortage());
        }

        public void Negotiate()
        {
            if (HasSession()) ShowResult(gameSession.TryNegotiateSupplyShortage(CharacterId.KurodaIori));
        }

        public void Report()
        {
            if (HasSession()) ShowResult(gameSession.TryReportSupplyShortage());
        }

        private bool HasSession()
        {
            if (gameSession != null)
                return true;
            _feedback = "缺少 GameSession，无法执行任务操作。";
            Refresh();
            return false;
        }

        private void ShowResult(SupplyShortageActionResult result)
        {
            _feedback = result.Feedback;
            Refresh();
        }

        private void SetAction(Button button, bool visible, bool atLocation)
        {
            if (button == null)
                return;
            button.gameObject.SetActive(visible);
            button.interactable = visible && atLocation && IsOpen && isActiveAndEnabled;
        }

        private void OnEnable()
        {
            BindListeners(true);
            Refresh();
        }
        private void OnDisable() => BindListeners(false);
        private void OnDestroy() => BindListeners(false);

        private void BindListeners(bool add)
        {
            BindButton(acceptButton, AcceptMission, add);
            BindButton(investigateButton, Investigate, add);
            BindButton(negotiateButton, Negotiate, add);
            BindButton(reportButton, Report, add);
        }

        private static void BindButton(Button button, UnityAction action, bool add)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            if (add) button.onClick.AddListener(action);
        }
    }
}
