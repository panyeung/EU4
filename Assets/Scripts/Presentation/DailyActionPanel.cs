using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// M02 daily-action panel. It lists every registered family person in the stable
    /// ordinal order owned by <see cref="DailyActionState"/>, shows the person's known
    /// location or in-transit route and the active action/progress, and submits at most
    /// one duration-bound Rest, Training, Work or Travel command for the selected person.
    /// It never advances time itself and never invents stat, economy or action-point
    /// rewards; the existing clock and monthly transaction remain the only time owners.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DailyActionPanel : MonoBehaviour
    {
        private const string RowPrefix = "M02DailyActionRow_";

        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform content;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private Text emptyText;
        [SerializeField] private Text selectedText;
        [SerializeField] private InputField durationInput;
        [SerializeField] private Button restButton;
        [SerializeField] private Button trainingButton;
        [SerializeField] private Button workButton;
        [SerializeField] private Button travelKiyosuButton;
        [SerializeField] private Button travelAtsutaButton;
        [SerializeField] private Button travelNagoyaButton;
        [SerializeField] private Button interruptButton;
        [SerializeField] private Text feedbackText;
        [SerializeField] private CharacterDailyPageView pageView;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly List<PersonId> _rowPersonIds = new List<PersonId>();
        private readonly List<UnityAction> _rowListeners = new List<UnityAction>();
        private bool _subscribed;
        private BaseInputModule _imeSafeInputModule;
        private BaseInput _previousInputOverride;
        private BaseInput _imeSafeInput;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private UnityAction _restListener;
        private UnityAction _trainingListener;
        private UnityAction _workListener;
        private UnityAction _kiyosuListener;
        private UnityAction _atsutaListener;
        private UnityAction _nagoyaListener;
        private UnityAction _interruptListener;
        private int _staticButtonBindings;
        private PersonId _selectedPersonId;
        private string _feedback = string.Empty;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            titleText != null && summaryText != null && scrollRect != null && content != null &&
            rowTemplate != null && emptyText != null && selectedText != null &&
            durationInput != null && restButton != null && trainingButton != null &&
            workButton != null && travelKiyosuButton != null && travelAtsutaButton != null &&
            travelNagoyaButton != null && interruptButton != null && feedbackText != null &&
            pageView != null && pageView.IsBound;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int OpenCount { get; private set; }
        public int RowCount => _rows.Count;
        public int StaticButtonBindingCount => _staticButtonBindings;
        public PersonId SelectedPersonId => _selectedPersonId;
        public string SelectedLine => selectedText != null ? selectedText.text : string.Empty;
        public string ProfileLine => SelectedLine;
        public string SummaryLine => summaryText != null ? summaryText.text : string.Empty;
        public string TitleLine => titleText != null ? titleText.text : string.Empty;
        public string FeedbackLine => feedbackText != null ? feedbackText.text : string.Empty;
        public bool FeedbackVisible => feedbackText != null && feedbackText.gameObject.activeSelf;
        public bool EmptyStateVisible => emptyText != null && emptyText.gameObject.activeSelf;
        public int ActionSubmitCount { get; private set; }
        public int InterruptCount { get; private set; }
        public int ReplanCount { get; private set; }

        public string DurationText => durationInput != null ? durationInput.text : string.Empty;

        public string RowText(int index) =>
            index >= 0 && index < _rows.Count && _rows[index] != null
                ? RowLabel(_rows[index])
                : string.Empty;

        public PersonId RowPersonId(int index) =>
            index >= 0 && index < _rowPersonIds.Count ? _rowPersonIds[index] : default;

        public string RowTextFor(PersonId personId)
        {
            for (int i = 0; i < _rowPersonIds.Count; i++)
                if (_rowPersonIds[i] == personId)
                    return RowText(i);
            return string.Empty;
        }

        public bool TravelInteractable(LocationId destination)
        {
            Button button = TravelButton(destination);
            return button != null && button.interactable;
        }

        /// <summary>Editor/test binding seam; it wires every reference before runtime use.</summary>
        public void BindReferences(GameSession session, GameObject root, Button open, Button close,
            Text title, Text summary, ScrollRect scroll, Transform rows, GameObject template,
            Text empty, Text selected, InputField duration, Button rest, Button training,
            Button work, Button travelKiyosu, Button travelAtsuta, Button travelNagoya,
            Button interrupt, Text feedback, CharacterDailyPageView view)
        {
            Unsubscribe();
            ClearRows();
            OpenCount = 0;
            _feedback = string.Empty;
            _selectedPersonId = default;
            gameSession = session;
            panelRoot = root;
            openButton = open;
            closeButton = close;
            titleText = title;
            summaryText = summary;
            scrollRect = scroll;
            content = rows;
            rowTemplate = template;
            emptyText = empty;
            selectedText = selected;
            durationInput = duration;
            restButton = rest;
            trainingButton = training;
            workButton = work;
            travelKiyosuButton = travelKiyosu;
            travelAtsutaButton = travelAtsuta;
            travelNagoyaButton = travelNagoya;
            interruptButton = interrupt;
            feedbackText = feedback;
            pageView = view;
            Subscribe();
            // Do not build runtime rows here: this seam is also used by the transactional
            // scene generator and generated rows must never be serialized into FoundationScene.
            if (UnityEngine.Application.isPlaying)
                Refresh();
        }

        public void Open()
        {
            if (panelRoot == null || panelRoot.activeSelf)
                return;
            UseImeSafeInputForDuration();
            OpenCount++;
            EnsureSelection();
            // The generated field ships with no label bound (m_TextComponent is null), so the
            // field stays disabled until the panel is visible and the label is bound. Play
            // Mode defers that by one frame in BindDurationInputAfterLayout; Edit Mode does
            // not tick MonoBehaviour coroutines, so it binds synchronously instead of waiting
            // on a frame that never runs there.
            bool previewScene = !UnityEngine.Application.isPlaying;
            if (durationInput != null)
            {
                durationInput.enabled = false;
                if (previewScene)
                    durationInput.textComponent = null;
            }
            panelRoot.SetActive(true);
            if (previewScene)
                BindDurationInputForPreviewScene();
            else if (durationInput != null)
                StartCoroutine(BindDurationInputAfterLayout());
            _feedback = SessionFeedback();
            Refresh();
        }

        /// <summary>
        /// Edit Mode binding path. Edit Mode does not tick this component's coroutines, so a
        /// deferred bind would never run there: bind the label, enable the field with the
        /// value cleared, and restore the value through SetDuration's detached-label path.
        /// </summary>
        private void BindDurationInputForPreviewScene()
        {
            if (!IsOpen || durationInput == null)
                return;
            BindDurationLabel();
            string value = durationInput.text;
            durationInput.text = string.Empty;
            durationInput.enabled = true;
            SetDuration(value);
        }

        private IEnumerator BindDurationInputAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            if (!IsOpen || durationInput == null)
                yield break;

            BindDurationLabel();
            // Play Mode renders the label, so enabling it updates in place and the text is
            // left untouched; rewriting the value here would only reset the field's text
            // bookkeeping. Only the text survives this path, not the caret or selection: the
            // hide that closed the panel ran DeactivateInputField, which clears both while
            // the reopened field is unfocused, and a click places the caret again.
            durationInput.enabled = true;
        }

        private void BindDurationLabel()
        {
            // Bind the label once. Reassigning textComponent unregisters and then
            // re-registers InputField's label callbacks, so only fill a missing binding.
            if (durationInput.textComponent == null)
                durationInput.textComponent =
                    durationInput.transform.Find("Text")?.GetComponent<Text>();
        }

        public void Close()
        {
            if (panelRoot == null)
            {
                RestoreInputOverride();
                return;
            }

            // This panel root is also the Character page root, so hiding it alone would
            // leave the page area blank while the Character tab stayed selected. Let the
            // bound navigator leave the page instead; panels used without a navigator
            // keep the plain hide behaviour.
            ReferenceGalleryNavigator navigator = GetComponentInParent<ReferenceGalleryNavigator>();
            if (navigator != null && navigator.TryReturnFromCharacterPage())
            {
                RestoreInputOverride();
                return;
            }

            panelRoot.SetActive(false);
            RestoreInputOverride();
        }

        /// <summary>Selects a registered person and re-renders the row highlight.</summary>
        public bool SelectPerson(PersonId personId)
        {
            if (_rowPersonIds.Count > 0 && !_rowPersonIds.Contains(personId))
                return false;
            _selectedPersonId = personId;
            Refresh();
            return true;
        }

        /// <summary>Test/UI seam for selecting a row by its stable display index.</summary>
        public bool SelectRow(int index)
        {
            if (index < 0 || index >= _rowPersonIds.Count)
                return false;
            return SelectPerson(_rowPersonIds[index]);
        }

        public void SetDuration(string text)
        {
            if (durationInput == null)
                return;
            string value = text ?? string.Empty;
            Text textComponent = durationInput.textComponent;
            if (textComponent == null)
            {
                durationInput.text = value;
            }
            else
            {
                durationInput.textComponent = null;
                try
                {
                    durationInput.text = value;
                    textComponent.text = durationInput.text;
                }
                finally
                {
                    durationInput.textComponent = textComponent;
                }
            }
            durationInput.caretPosition = durationInput.text.Length;
            if (durationInput.placeholder != null)
                durationInput.placeholder.enabled = string.IsNullOrEmpty(durationInput.text);
        }

        public bool ClickRest() => Submit(DailyActionKind.Rest);

        public bool ClickTraining() => Submit(DailyActionKind.Training);

        public bool ClickWork() => Submit(DailyActionKind.Work);

        public bool ClickTravel(LocationId destination) => SubmitTravel(destination);

        public bool ClickInterrupt()
        {
            EnsureSelection();
            if (gameSession == null || _selectedPersonId.IsEmpty)
                return false;
            InterruptCount++;
            bool succeeded = gameSession.TryInterruptDailyAction(_selectedPersonId,
                "玩家在每日行动面板中断当前行动。");
            _feedback = SessionFeedback();
            Refresh();
            return succeeded;
        }

        /// <summary>Rebuilds only presentation state from the session's read-only snapshots.</summary>
        public void Refresh()
        {
            if (!IsBound)
            {
                ClearRows();
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            titleText.text = "人物管理 › 人物档案 · 每日行动";
            IReadOnlyList<DailyActionPersonState> persons = gameSession.DailyActions;
            EnsureSelection(persons);

            summaryText.text = "当前日期：" +
                               (gameSession.WorldClock != null
                                   ? gameSession.WorldClock.CurrentDate.ToString()
                                   : "未初始化") +
                               "　｜　家族登记 " +
                               persons.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                               " 人　｜　左侧选择人物，右侧安排行动";

            RebuildRows(persons);
            RenderSelected(persons);
            RenderTravelButtons(persons);
            RenderFeedback();
            if (pageView != null && pageView.IsBound)
                pageView.Render(gameSession, _selectedPersonId, persons);
        }

        private bool Submit(DailyActionKind kind)
        {
            EnsureSelection();
            if (gameSession == null || _selectedPersonId.IsEmpty)
            {
                _feedback = "请先选择一个已登记的人物。";
                RenderFeedback();
                return false;
            }

            if (!TryParseDuration(out int duration))
            {
                _feedback = "持续时间必须是大于 0 的整数（至少 1 天）。";
                RenderFeedback();
                return false;
            }

            bool hasActive = gameSession.HasActiveDailyAction(_selectedPersonId);
            bool succeeded;
            if (hasActive)
            {
                ReplanCount++;
                succeeded = gameSession.TryReplanDailyAction(_selectedPersonId, kind, duration,
                    default, out _);
            }
            else
            {
                succeeded = gameSession.TryPlanDailyAction(_selectedPersonId, kind, duration,
                    default, out _);
            }

            ActionSubmitCount++;
            _feedback = SessionFeedback();
            Refresh();
            return succeeded;
        }

        private bool SubmitTravel(LocationId destination)
        {
            EnsureSelection();
            if (gameSession == null || _selectedPersonId.IsEmpty)
            {
                _feedback = "请先选择一个已登记的人物。";
                RenderFeedback();
                return false;
            }

            bool hasActive = gameSession.HasActiveDailyAction(_selectedPersonId);
            bool succeeded;
            if (hasActive)
            {
                ReplanCount++;
                succeeded = gameSession.TryReplanDailyAction(_selectedPersonId,
                    DailyActionKind.Travel, 0, destination, out _);
            }
            else
            {
                succeeded = gameSession.TryPlanTravelAction(_selectedPersonId, destination,
                    out _);
            }

            ActionSubmitCount++;
            _feedback = SessionFeedback();
            Refresh();
            return succeeded;
        }

        private bool TryParseDuration(out int duration)
        {
            duration = 0;
            string text = durationInput != null ? durationInput.text : string.Empty;
            return int.TryParse(text, System.Globalization.NumberStyles.Integer,
                       System.Globalization.CultureInfo.InvariantCulture, out duration) &&
                   duration > 0;
        }

        private void RebuildRows(IReadOnlyList<DailyActionPersonState> persons)
        {
            ClearRows();
            if (content == null || rowTemplate == null)
                return;

            for (int i = 0; i < persons.Count; i++)
            {
                DailyActionPersonState person = persons[i];
                GameObject row = Instantiate(rowTemplate, content);
                row.name = RowPrefix + (person.PersonId.Value ?? string.Empty);
                Text label = row.GetComponentInChildren<Text>(true);
                if (label == null)
                {
                    DestroyGenerated(row);
                    continue;
                }

                label.text = FormatRow(person, person.PersonId == _selectedPersonId);
                label.fontStyle = person.PersonId == _selectedPersonId
                    ? FontStyle.Bold
                    : FontStyle.Normal;
                Button button = row.GetComponent<Button>();
                if (button != null)
                {
                    PersonId captured = person.PersonId;
                    UnityAction listener = () => SelectPerson(captured);
                    button.onClick.AddListener(listener);
                    _rowListeners.Add(listener);
                }

                row.SetActive(true);
                _rows.Add(row);
                _rowPersonIds.Add(person.PersonId);
            }

            emptyText.text = "尚无已登记的家族人物。";
            emptyText.gameObject.SetActive(persons.Count == 0);
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        private string FormatRow(DailyActionPersonState person, bool selected)
        {
            var builder = new StringBuilder(72);
            builder.Append(selected ? "▶ " : string.Empty);
            builder.Append(PersonName(person.PersonId));
            builder.Append("　｜　");

            if (person.IsInTransit && person.ActiveAction != null)
            {
                builder.Append("旅途中（")
                    .Append(LocationId.GetChineseName(person.ActiveAction.Origin))
                    .Append("→")
                    .Append(LocationId.GetChineseName(person.ActiveAction.Destination))
                    .Append("）");
            }
            else if (person.HasLocation)
            {
                builder.Append(LocationId.GetChineseName(person.Location));
            }
            else
            {
                builder.Append("地点未知");
            }

            builder.Append("　｜　");
            if (person.ActiveAction == null)
            {
                builder.Append("空闲");
                return builder.ToString();
            }

            DailyActionTask task = person.ActiveAction;
            builder.Append(task.KindName)
                .Append(' ')
                .Append(task.ElapsedDays.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append('/')
                .Append(task.DurationDays.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(" 天（剩 ")
                .Append(task.RemainingDays.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append('）');
            return builder.ToString();
        }

        /// <summary>
        /// Renders the selected family member as the profile focus. Every value comes from
        /// the session's current roster and daily-action snapshot; the panel never invents
        /// a stat or relationship, and this method never mutates session state.
        /// </summary>
        private void RenderSelected(IReadOnlyList<DailyActionPersonState> persons)
        {
            DailyActionPersonState selected = null;
            for (int i = 0; i < persons.Count; i++)
                if (persons[i].PersonId == _selectedPersonId)
                {
                    selected = persons[i];
                    break;
                }

            if (selected == null)
            {
                selectedText.text = "<color=#C4553B>人物档案</color>\n当前选择：未选择人物。";
                return;
            }

            var builder = new StringBuilder(220);
            builder.Append("<color=#C4553B>人物档案</color>\n");

            string displayName = PersonName(selected.PersonId);
            if (TryRosterRow(selected.PersonId, out M2RosterRow row))
            {
                if (!string.IsNullOrWhiteSpace(row.DisplayName))
                    displayName = row.DisplayName;
                builder.Append("<size=24><b>").Append(displayName).Append("</b></size>\n");
                builder.Append(row.Age > 0 ? row.Age.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "岁"
                    : "年龄未知")
                    .Append("　｜　").Append(row.RoleName)
                    .Append("　｜　").Append(row.GenerationLabel).Append('\n');
                if (!string.IsNullOrWhiteSpace(row.Kinship))
                    builder.Append("亲族：").Append(row.Kinship).Append('\n');
            }
            else
            {
                builder.Append("<size=24><b>").Append(displayName).Append("</b></size>\n");
                builder.Append("人物详情暂未登记在家族名册中。\n");
            }

            if (selected.IsInTransit && selected.ActiveAction != null)
            {
                builder.Append("旅途中：")
                    .Append(LocationId.GetChineseName(selected.ActiveAction.Origin))
                    .Append("→")
                    .Append(LocationId.GetChineseName(selected.ActiveAction.Destination))
                    .Append("　剩余 ")
                    .Append(selected.ActiveAction.RemainingDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" 天\n");
            }
            else if (selected.HasLocation)
            {
                builder.Append("所在：").Append(LocationId.GetChineseName(selected.Location));
                builder.Append("　｜　可前往：").Append(KnownDestinations(selected.Location))
                    .Append('\n');
            }
            else
            {
                builder.Append("所在：地点未知\n");
            }

            if (selected.ActiveAction != null)
            {
                builder.Append("行动：").Append(selected.ActiveAction.KindName)
                    .Append(" 进度 ")
                    .Append(selected.ActiveAction.ElapsedDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append('/')
                    .Append(selected.ActiveAction.DurationDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" 天（剩 ")
                    .Append(selected.ActiveAction.RemainingDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append('）');
            }
            else
            {
                builder.Append("行动：空闲");
            }

            selectedText.text = builder.ToString();
        }

        private bool TryRosterRow(PersonId personId, out M2RosterRow row)
        {
            row = default;
            if (gameSession == null)
                return false;
            IReadOnlyList<M2RosterRow> roster = gameSession.FamilyRoster;
            if (roster == null)
                return false;
            for (int i = 0; i < roster.Count; i++)
                if (roster[i].PersonId == personId)
                {
                    row = roster[i];
                    return true;
                }
            return false;
        }

        private static string KnownDestinations(LocationId location)
        {
            var destinations = new List<string>();
            foreach (LocationId destination in LocationId.All)
                if (WorldMapRules.TryGetTravelDays(location, destination, out _))
                    destinations.Add(LocationId.GetChineseName(destination));
            return destinations.Count == 0 ? "无直达路线" : string.Join("、", destinations);
        }

        private void RenderTravelButtons(IReadOnlyList<DailyActionPersonState> persons)
        {
            LocationId origin = default;
            bool hasOrigin = false;
            for (int i = 0; i < persons.Count; i++)
                if (persons[i].PersonId == _selectedPersonId)
                {
                    hasOrigin = !persons[i].IsInTransit && persons[i].HasLocation;
                    origin = persons[i].Location;
                    break;
                }

            SetTravelButton(travelKiyosuButton, origin, hasOrigin, LocationId.Kiyosu);
            SetTravelButton(travelAtsutaButton, origin, hasOrigin, LocationId.Atsuta);
            SetTravelButton(travelNagoyaButton, origin, hasOrigin, LocationId.Nagoya);
        }

        private static void SetTravelButton(Button button, LocationId origin, bool hasOrigin,
            LocationId destination)
        {
            if (button == null)
                return;
            button.interactable = hasOrigin &&
                WorldMapRules.TryGetTravelDays(origin, destination, out _);
        }

        private void RenderFeedback()
        {
            if (feedbackText == null)
                return;
            string message = _feedback ?? string.Empty;
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        private void EnsureSelection()
        {
            EnsureSelection(gameSession != null ? gameSession.DailyActions : null);
        }

        private void EnsureSelection(IReadOnlyList<DailyActionPersonState> persons)
        {
            if (persons == null || persons.Count == 0)
            {
                _selectedPersonId = default;
                return;
            }

            for (int i = 0; i < persons.Count; i++)
                if (persons[i].PersonId == _selectedPersonId)
                    return;

            PersonId headId = gameSession != null && gameSession.Succession != null
                ? gameSession.Succession.HeadPersonId
                : default;
            for (int i = 0; i < persons.Count; i++)
                if (persons[i].PersonId == headId)
                {
                    _selectedPersonId = headId;
                    return;
                }

            _selectedPersonId = persons[0].PersonId;
        }

        private string SessionFeedback()
        {
            if (gameSession == null)
                return string.Empty;

            string message = gameSession.LastDailyActionMessage ?? string.Empty;
            IReadOnlyList<DailyActionResult> outcomes = gameSession.LastDailyActionOutcomes;
            if (outcomes == null || outcomes.Count == 0)
                return message;

            var builder = new StringBuilder(128);
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (i > 0)
                    builder.Append("；");
                DailyActionResult outcome = outcomes[i];
                builder.Append(OutcomeName(outcome.Outcome)).Append("：")
                    .Append(PersonName(outcome.PersonId));
                if (outcome.Outcome != DailyActionOutcomeKind.Completed)
                    builder.Append(' ').Append(DailyActionState.KindName(outcome.Kind));
            }
            if (!string.IsNullOrWhiteSpace(message))
                builder.Append("。").Append(message);
            return builder.ToString();
        }

        private static string OutcomeName(DailyActionOutcomeKind outcome)
        {
            switch (outcome)
            {
                case DailyActionOutcomeKind.Planned: return "已安排";
                case DailyActionOutcomeKind.Completed: return "已完成";
                case DailyActionOutcomeKind.Interrupted: return "已中断";
                case DailyActionOutcomeKind.Replanned: return "已重新规划";
                case DailyActionOutcomeKind.Rejected: return "已拒绝";
                default: return "结果";
            }
        }

        private Button TravelButton(LocationId destination)
        {
            if (destination == LocationId.Kiyosu) return travelKiyosuButton;
            if (destination == LocationId.Atsuta) return travelAtsutaButton;
            if (destination == LocationId.Nagoya) return travelNagoyaButton;
            return null;
        }

        private string PersonName(PersonId personId)
        {
            if (personId.IsEmpty)
                return "未知人物";
            if (gameSession != null && gameSession.Succession != null &&
                gameSession.Succession.World != null &&
                gameSession.Succession.World.TryGetPerson(personId, out PersonState person) &&
                !string.IsNullOrWhiteSpace(person.DisplayName))
                return person.DisplayName;
            return personId.Value;
        }

        private static string RowLabel(GameObject row)
        {
            Text text = row != null ? row.GetComponentInChildren<Text>(true) : null;
            return text != null ? text.text : string.Empty;
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
            WireStaticButtons();
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            UnwireStaticButtons();
        }

        private void WireStaticButtons()
        {
            if (openButton != null && _openListener == null)
            {
                _openListener = Open;
                openButton.onClick.AddListener(_openListener);
                _staticButtonBindings++;
            }
            if (closeButton != null && _closeListener == null)
            {
                _closeListener = Close;
                closeButton.onClick.AddListener(_closeListener);
                _staticButtonBindings++;
            }
            if (restButton != null && _restListener == null)
            {
                _restListener = () => ClickRest();
                restButton.onClick.AddListener(_restListener);
                _staticButtonBindings++;
            }
            if (trainingButton != null && _trainingListener == null)
            {
                _trainingListener = () => ClickTraining();
                trainingButton.onClick.AddListener(_trainingListener);
                _staticButtonBindings++;
            }
            if (workButton != null && _workListener == null)
            {
                _workListener = () => ClickWork();
                workButton.onClick.AddListener(_workListener);
                _staticButtonBindings++;
            }
            if (travelKiyosuButton != null && _kiyosuListener == null)
            {
                _kiyosuListener = () => ClickTravel(LocationId.Kiyosu);
                travelKiyosuButton.onClick.AddListener(_kiyosuListener);
                _staticButtonBindings++;
            }
            if (travelAtsutaButton != null && _atsutaListener == null)
            {
                _atsutaListener = () => ClickTravel(LocationId.Atsuta);
                travelAtsutaButton.onClick.AddListener(_atsutaListener);
                _staticButtonBindings++;
            }
            if (travelNagoyaButton != null && _nagoyaListener == null)
            {
                _nagoyaListener = () => ClickTravel(LocationId.Nagoya);
                travelNagoyaButton.onClick.AddListener(_nagoyaListener);
                _staticButtonBindings++;
            }
            if (interruptButton != null && _interruptListener == null)
            {
                _interruptListener = () => ClickInterrupt();
                interruptButton.onClick.AddListener(_interruptListener);
                _staticButtonBindings++;
            }
        }

        private void UnwireStaticButtons()
        {
            if (_openListener != null)
            {
                if (openButton != null) openButton.onClick.RemoveListener(_openListener);
                _openListener = null;
                _staticButtonBindings--;
            }
            if (_closeListener != null)
            {
                if (closeButton != null) closeButton.onClick.RemoveListener(_closeListener);
                _closeListener = null;
                _staticButtonBindings--;
            }
            if (_restListener != null)
            {
                if (restButton != null) restButton.onClick.RemoveListener(_restListener);
                _restListener = null;
                _staticButtonBindings--;
            }
            if (_trainingListener != null)
            {
                if (trainingButton != null) trainingButton.onClick.RemoveListener(_trainingListener);
                _trainingListener = null;
                _staticButtonBindings--;
            }
            if (_workListener != null)
            {
                if (workButton != null) workButton.onClick.RemoveListener(_workListener);
                _workListener = null;
                _staticButtonBindings--;
            }
            if (_kiyosuListener != null)
            {
                if (travelKiyosuButton != null)
                    travelKiyosuButton.onClick.RemoveListener(_kiyosuListener);
                _kiyosuListener = null;
                _staticButtonBindings--;
            }
            if (_atsutaListener != null)
            {
                if (travelAtsutaButton != null)
                    travelAtsutaButton.onClick.RemoveListener(_atsutaListener);
                _atsutaListener = null;
                _staticButtonBindings--;
            }
            if (_nagoyaListener != null)
            {
                if (travelNagoyaButton != null)
                    travelNagoyaButton.onClick.RemoveListener(_nagoyaListener);
                _nagoyaListener = null;
                _staticButtonBindings--;
            }
            if (_interruptListener != null)
            {
                if (interruptButton != null)
                    interruptButton.onClick.RemoveListener(_interruptListener);
                _interruptListener = null;
                _staticButtonBindings--;
            }
            if (_staticButtonBindings < 0)
                _staticButtonBindings = 0;
        }

        private void OnSessionStateChanged()
        {
            if (IsOpen)
            {
                _feedback = SessionFeedback();
                Refresh();
            }
            else if (pageView != null && pageView.IsBound)
            {
                IReadOnlyList<DailyActionPersonState> persons = gameSession.DailyActions;
                EnsureSelection(persons);
                DailyActionPersonState selected = null;
                foreach (DailyActionPersonState person in persons)
                    if (person.PersonId == _selectedPersonId)
                    {
                        selected = person;
                        break;
                    }
                pageView.RenderHeader(gameSession, selected);
            }
        }

        private void OnEnable()
        {
            Subscribe();
            if (UnityEngine.Application.isPlaying && IsOpen)
                Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearRows();
            RestoreInputOverride();
            if (IsOpen && panelRoot != gameObject)
                panelRoot.SetActive(false);
        }

        private void UseImeSafeInputForDuration()
        {
            if (Input.compositionString.Length == 0 || _imeSafeInputModule != null)
                return;
            _imeSafeInputModule = EventSystem.current != null
                ? EventSystem.current.currentInputModule ??
                  EventSystem.current.GetComponent<BaseInputModule>()
                : null;
            if (_imeSafeInputModule == null)
                return;

            _previousInputOverride = _imeSafeInputModule.inputOverride;
            _imeSafeInput = _imeSafeInputModule.gameObject.GetComponent<NumericDurationBaseInput>();
            if (_imeSafeInput == null)
                _imeSafeInput = _imeSafeInputModule.gameObject.AddComponent<NumericDurationBaseInput>();
            _imeSafeInputModule.inputOverride = _imeSafeInput;
        }

        private void RestoreInputOverride()
        {
            if (_imeSafeInputModule != null && _imeSafeInputModule.inputOverride == _imeSafeInput)
                _imeSafeInputModule.inputOverride = _previousInputOverride;
            _imeSafeInputModule = null;
            _previousInputOverride = null;
            _imeSafeInput = null;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null || i >= _rowListeners.Count ||
                    _rowListeners[i] == null)
                    continue;
                Button button = _rows[i].GetComponent<Button>();
                if (button != null)
                    button.onClick.RemoveListener(_rowListeners[i]);
            }
            for (int i = _rows.Count - 1; i >= 0; i--)
                DestroyGenerated(_rows[i]);
            _rows.Clear();
            _rowListeners.Clear();
            _rowPersonIds.Clear();
        }

        private static void DestroyGenerated(GameObject generated)
        {
            if (generated == null)
                return;
            generated.SetActive(false);
            if (UnityEngine.Application.isPlaying)
                Destroy(generated);
            else
                DestroyImmediate(generated);
        }
    }

    // Numeric duration input must ignore stale global IME text while it is open.
    internal sealed class NumericDurationBaseInput : BaseInput
    {
        public override string compositionString => string.Empty;
    }
}
