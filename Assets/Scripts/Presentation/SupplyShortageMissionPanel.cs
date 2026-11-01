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
        [SerializeField] private Text sealedLetterStageText;
        [SerializeField] private Text sealedLetterObjectiveText;
        [SerializeField] private Text sealedLetterFeedbackText;
        [SerializeField] private Button sealedLetterActionButton;
        [SerializeField] private Text marketTallyStageText;
        [SerializeField] private Text marketTallyObjectiveText;
        [SerializeField] private Text marketTallyFeedbackText;
        [SerializeField] private Button marketTallyActionButton;
        [SerializeField] private Button missionBoardToggleButton;

        public bool IsOpen { get; private set; }
        public bool IsM4bPage { get; private set; }
        private string _feedback = string.Empty;
        private string _sealedLetterFeedback = string.Empty;
        private string _marketTallyFeedback = string.Empty;

        public void Bind(GameSession session)
        {
            if (gameSession != session)
            {
                _feedback = string.Empty;
                _sealedLetterFeedback = string.Empty;
                _marketTallyFeedback = string.Empty;
            }
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

        public void BindM4bReferences(Text letterStage, Text letterObjective, Text letterFeedback,
            Button letterAction, Text tallyStage, Text tallyObjective, Text tallyFeedback, Button tallyAction,
            Button pageToggle)
        {
            BindListeners(false);
            sealedLetterStageText = letterStage;
            sealedLetterObjectiveText = letterObjective;
            sealedLetterFeedbackText = letterFeedback;
            sealedLetterActionButton = letterAction;
            marketTallyStageText = tallyStage;
            marketTallyObjectiveText = tallyObjective;
            marketTallyFeedbackText = tallyFeedback;
            marketTallyActionButton = tallyAction;
            missionBoardToggleButton = pageToggle;
            BindListeners(isActiveAndEnabled);
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
            {
                stageText.text = hasSession ? $"补给短缺：{stageLabel}" : "补给短缺：不可用";
                stageText.gameObject.SetActive(!IsM4bPage);
            }
            if (objectiveText != null)
            {
                objectiveText.text = hasSession ? objective : "缺少 GameSession。";
                objectiveText.gameObject.SetActive(!IsM4bPage);
            }
            if (meritText != null)
                meritText.text = hasSession ? $"功绩：{gameSession.Merit}" : "功绩：—";
            if (feedbackText != null)
            {
                feedbackText.text = _feedback;
                feedbackText.gameObject.SetActive(!IsM4bPage);
            }

            LocationId location = hasSession ? gameSession.CurrentLocation : default;
            // Visibility is a UI hint; GameSession validates every submitted action.
            SetAction(acceptButton, hasSession && stage == SupplyShortageStage.NotAccepted,
                location == LocationId.Kiyosu, !IsM4bPage);
            SetAction(investigateButton, hasSession && stage == SupplyShortageStage.Accepted,
                location == LocationId.Atsuta, !IsM4bPage);
            SetAction(negotiateButton, hasSession && stage == SupplyShortageStage.Investigated,
                location == LocationId.Atsuta, !IsM4bPage);
            SetAction(reportButton, hasSession && stage == SupplyShortageStage.Negotiated,
                location == LocationId.Kiyosu, !IsM4bPage);

            RefreshSealedLetter(hasSession, location);
            RefreshMarketTally(hasSession, location);
            if (meritText != null) meritText.gameObject.SetActive(!IsM4bPage);
            if (missionBoardToggleButton != null)
            {
                missionBoardToggleButton.gameObject.SetActive(true);
                missionBoardToggleButton.interactable = IsOpen && isActiveAndEnabled;
                Text toggleLabel = missionBoardToggleButton.GetComponentInChildren<Text>(true);
                if (toggleLabel != null) toggleLabel.text = IsM4bPage ? "返回补给任务" : "其他任务";
            }
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

        public void HandleSealedLetterAction()
        {
            if (!HasSession()) return;
            SealedLetterActionResult result = gameSession.SealedLetterStage == SealedLetterStage.NotAccepted
                ? gameSession.TryAcceptSealedLetterMission()
                : gameSession.TryDeliverSealedLetter();
            _sealedLetterFeedback = result.Feedback;
            Refresh();
        }

        public void HandleMarketTallyAction()
        {
            if (!HasSession()) return;
            MarketTallyActionResult result;
            if (gameSession.MarketTallyStage == MarketTallyStage.NotAccepted)
                result = gameSession.TryAcceptMarketTallyMission();
            else if (gameSession.MarketTallyStage == MarketTallyStage.Accepted)
                result = gameSession.TryInvestigateMarketTally();
            else
                result = gameSession.TryReportMarketTally();
            _marketTallyFeedback = result.Feedback;
            Refresh();
        }

        public void ToggleMissionBoardPage()
        {
            IsM4bPage = !IsM4bPage;
            Refresh();
        }

        private void RefreshSealedLetter(bool hasSession, LocationId location)
        {
            SealedLetterStage stage = hasSession ? gameSession.SealedLetterStage : SealedLetterStage.NotAccepted;
            string label = stage == SealedLetterStage.RewardClaimed ? "已完成" : stage == SealedLetterStage.Accepted ? "进行中" : "未领取";
            string objective = stage == SealedLetterStage.NotAccepted ? "在清洲向橘 すず领取密信。"
                : stage == SealedLetterStage.Accepted ? "前往那古野向星野 彩交付密信。" : "密信已送达，任务完成。";
            if (sealedLetterStageText != null)
            {
                sealedLetterStageText.text = hasSession ? $"密信：{label}  功绩：{gameSession.Merit}" : "密信：不可用";
                sealedLetterStageText.gameObject.SetActive(IsM4bPage);
            }
            if (sealedLetterObjectiveText != null) sealedLetterObjectiveText.text = hasSession ? objective : "缺少 GameSession。";
            if (sealedLetterFeedbackText != null) sealedLetterFeedbackText.text = _sealedLetterFeedback;
            SetContextAction(sealedLetterActionButton, hasSession && stage != SealedLetterStage.RewardClaimed,
                hasSession && ((stage == SealedLetterStage.NotAccepted && location == LocationId.Kiyosu) ||
                    (stage == SealedLetterStage.Accepted && location == LocationId.Nagoya)),
                IsM4bPage,
                stage == SealedLetterStage.Accepted ? "交付密信" : "领取密信");
            if (sealedLetterObjectiveText != null) sealedLetterObjectiveText.gameObject.SetActive(IsM4bPage);
            if (sealedLetterFeedbackText != null) sealedLetterFeedbackText.gameObject.SetActive(IsM4bPage);
        }

        private void RefreshMarketTally(bool hasSession, LocationId location)
        {
            MarketTallyStage stage = hasSession ? gameSession.MarketTallyStage : MarketTallyStage.NotAccepted;
            string label = stage == MarketTallyStage.RewardClaimed ? "已完成" : stage == MarketTallyStage.Investigated ? "已调查" : stage == MarketTallyStage.Accepted ? "进行中" : "未领取";
            string objective = stage == MarketTallyStage.NotAccepted ? "在那古野向酒井 次郎兵卫领取任务。"
                : stage == MarketTallyStage.Accepted ? "前往清洲向森 源左衛門调查账目。"
                : stage == MarketTallyStage.Investigated ? "返回那古野向酒井 次郎兵卫报告。" : "市集账目已报告，任务完成。";
            if (marketTallyStageText != null)
            {
                marketTallyStageText.text = hasSession ? $"市集账目：{label}" : "市集账目：不可用";
                marketTallyStageText.gameObject.SetActive(IsM4bPage);
            }
            if (marketTallyObjectiveText != null) marketTallyObjectiveText.text = hasSession ? objective : "缺少 GameSession。";
            if (marketTallyFeedbackText != null) marketTallyFeedbackText.text = _marketTallyFeedback;
            bool atActionLocation = hasSession && (stage == MarketTallyStage.NotAccepted
                ? location == LocationId.Nagoya
                : stage == MarketTallyStage.Accepted ? location == LocationId.Kiyosu
                : stage == MarketTallyStage.Investigated && location == LocationId.Nagoya);
            string actionLabel = stage == MarketTallyStage.NotAccepted ? "领取账目任务"
                : stage == MarketTallyStage.Accepted ? "调查账目"
                : stage == MarketTallyStage.Investigated ? "报告账目" : string.Empty;
            SetContextAction(marketTallyActionButton, hasSession && stage != MarketTallyStage.RewardClaimed,
                atActionLocation, IsM4bPage, actionLabel);
            if (marketTallyObjectiveText != null) marketTallyObjectiveText.gameObject.SetActive(IsM4bPage);
            if (marketTallyFeedbackText != null) marketTallyFeedbackText.gameObject.SetActive(IsM4bPage);
        }

        private void SetContextAction(Button button, bool visible, bool atLocation, bool pageVisible, string label)
        {
            SetAction(button, visible, atLocation, pageVisible);
            if (button == null) return;
            Text buttonLabel = button.GetComponentInChildren<Text>(true);
            if (buttonLabel != null) buttonLabel.text = label;
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

        private void SetAction(Button button, bool visible, bool atLocation, bool pageVisible = true)
        {
            if (button == null)
                return;
            button.gameObject.SetActive(visible && pageVisible);
            button.interactable = visible && pageVisible && atLocation && IsOpen && isActiveAndEnabled;
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
            BindButton(sealedLetterActionButton, HandleSealedLetterAction, add);
            BindButton(marketTallyActionButton, HandleMarketTallyAction, add);
            BindButton(missionBoardToggleButton, ToggleMissionBoardPage, add);
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
