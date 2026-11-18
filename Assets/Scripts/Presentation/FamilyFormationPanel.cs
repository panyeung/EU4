using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// SC-022 marriage and ordinary-adoption panel. It only projects
    /// <see cref="GameSession.FamilyFormation"/> and submits one explicit confirmation
    /// through the session commands. The panel starts hidden, never auto-opens on a
    /// scenario change, and does not modify the read-only family roster panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FamilyFormationPanel : MonoBehaviour
    {
        public const string MarriageMemberPrefix = "SC022MarriageMember_";
        public const string MarriageCandidatePrefix = "SC022MarriageCandidate_";
        public const string AdoptionMemberPrefix = "SC022AdoptionMember_";
        public const string AdoptionCandidatePrefix = "SC022AdoptionCandidate_";
        public const string PreviewStripName = "SC022FormationPreview";
        public const string PreviewTextName = "SC022FormationPreviewText";
        /// <summary>预览在配对完整时替换的序列化帮助行。</summary>
        public const string FamilyFormationHelpName = "SC022FamilyFormationHelpText";
        private const float PreviewHeight = 44f;
        private const float PreviewMaximumHeight = 68f;

        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text blockReasonText;

        [Header("Marriage")]
        [SerializeField] private Text marriageMemberInfo;
        [SerializeField] private Transform marriageMemberContainer;
        [SerializeField] private Button marriageMemberTemplate;
        [SerializeField] private Text marriageCandidateInfo;
        [SerializeField] private Transform marriageCandidateContainer;
        [SerializeField] private Button marriageCandidateTemplate;
        [SerializeField] private Button marriageConfirmButton;
        [SerializeField] private Text marriageMessage;

        [Header("Adoption")]
        [SerializeField] private Text adoptionMemberInfo;
        [SerializeField] private Transform adoptionMemberContainer;
        [SerializeField] private Button adoptionMemberTemplate;
        [SerializeField] private Text adoptionCandidateInfo;
        [SerializeField] private Transform adoptionCandidateContainer;
        [SerializeField] private Button adoptionCandidateTemplate;
        [SerializeField] private Button adoptionConfirmButton;
        [SerializeField] private Text adoptionMessage;

        private readonly List<GameObject> _marriageMembers = new List<GameObject>();
        private readonly List<GameObject> _marriageCandidates = new List<GameObject>();
        private readonly List<GameObject> _adoptionMembers = new List<GameObject>();
        private readonly List<GameObject> _adoptionCandidates = new List<GameObject>();
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private UnityAction _marriageConfirmListener;
        private UnityAction _adoptionConfirmListener;
        private PersonId _selectedMarriageMember;
        private PersonId _selectedMarriageCandidate;
        private PersonId _selectedAdoptionMember;
        private PersonId _selectedAdoptionCandidate;
        private GameObject _previewStrip;
        private Text _previewText;
        private Text _helpText;
        private bool _previewPrefersAdoption;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            titleText != null && blockReasonText != null &&
            marriageMemberInfo != null && marriageMemberContainer != null &&
            marriageMemberTemplate != null && marriageCandidateInfo != null &&
            marriageCandidateContainer != null && marriageCandidateTemplate != null &&
            marriageConfirmButton != null && marriageMessage != null &&
            adoptionMemberInfo != null && adoptionMemberContainer != null &&
            adoptionMemberTemplate != null && adoptionCandidateInfo != null &&
            adoptionCandidateContainer != null && adoptionCandidateTemplate != null &&
            adoptionConfirmButton != null && adoptionMessage != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int RefreshCount { get; private set; }
        public int MarriageCommitCount { get; private set; }
        public int AdoptionCommitCount { get; private set; }

        public string TitleLine => titleText != null ? titleText.text : string.Empty;
        public string BlockReasonLine => blockReasonText != null ? blockReasonText.text : string.Empty;
        public bool BlockReasonVisible =>
            blockReasonText != null && blockReasonText.gameObject.activeSelf;

        public string MarriageMemberInfoLine =>
            marriageMemberInfo != null ? marriageMemberInfo.text : string.Empty;
        public string MarriageCandidateInfoLine =>
            marriageCandidateInfo != null ? marriageCandidateInfo.text : string.Empty;
        public string MarriageMessageLine =>
            marriageMessage != null ? marriageMessage.text : string.Empty;
        public bool MarriageMessageVisible =>
            marriageMessage != null && marriageMessage.gameObject.activeSelf;
        public bool MarriageConfirmInteractable =>
            marriageConfirmButton != null && marriageConfirmButton.interactable;

        public string AdoptionMemberInfoLine =>
            adoptionMemberInfo != null ? adoptionMemberInfo.text : string.Empty;
        public string AdoptionCandidateInfoLine =>
            adoptionCandidateInfo != null ? adoptionCandidateInfo.text : string.Empty;
        public string AdoptionMessageLine =>
            adoptionMessage != null ? adoptionMessage.text : string.Empty;
        public bool AdoptionMessageVisible =>
            adoptionMessage != null && adoptionMessage.gameObject.activeSelf;
        public bool AdoptionConfirmInteractable =>
            adoptionConfirmButton != null && adoptionConfirmButton.interactable;

        public int MarriageMemberCount => _marriageMembers.Count;
        public int MarriageCandidateCount => _marriageCandidates.Count;
        public int AdoptionMemberCount => _adoptionMembers.Count;
        public int AdoptionCandidateCount => _adoptionCandidates.Count;

        public PersonId SelectedMarriageMember => _selectedMarriageMember;
        public PersonId SelectedMarriageCandidate => _selectedMarriageCandidate;
        public PersonId SelectedAdoptionMember => _selectedAdoptionMember;
        public PersonId SelectedAdoptionCandidate => _selectedAdoptionCandidate;

        public string MarriageMemberLabel(int index) => LabelOf(_marriageMembers, index);
        public string MarriageCandidateLabel(int index) => LabelOf(_marriageCandidates, index);
        public string AdoptionMemberLabel(int index) => LabelOf(_adoptionMembers, index);
        public string AdoptionCandidateLabel(int index) => LabelOf(_adoptionCandidates, index);

        /// <summary>
        /// 所选配对的一行预览：已记录的亲属路径、SC-022 规则结论
        /// 以及确认后将登记的内容。
        /// </summary>
        public string PreviewLine => _previewText != null ? _previewText.text : string.Empty;

        public bool HasPreviewStrip => _previewStrip != null && _previewStrip.activeSelf;

        public void BindReferences(GameSession session, GameObject panel, Button open, Button close,
            Text title, Text blockReason,
            Text marriageMemberText, Transform marriageMembers, Button marriageMemberButton,
            Text marriageCandidateText, Transform marriageCandidates, Button marriageCandidateButton,
            Button marriageConfirm, Text marriageMessageText,
            Text adoptionMemberText, Transform adoptionMembers, Button adoptionMemberButton,
            Text adoptionCandidateText, Transform adoptionCandidates, Button adoptionCandidateButton,
            Button adoptionConfirm, Text adoptionMessageText)
        {
            Unsubscribe();
            ClearRows();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            titleText = title;
            blockReasonText = blockReason;
            marriageMemberInfo = marriageMemberText;
            marriageMemberContainer = marriageMembers;
            marriageMemberTemplate = marriageMemberButton;
            marriageCandidateInfo = marriageCandidateText;
            marriageCandidateContainer = marriageCandidates;
            marriageCandidateTemplate = marriageCandidateButton;
            marriageConfirmButton = marriageConfirm;
            marriageMessage = marriageMessageText;
            adoptionMemberInfo = adoptionMemberText;
            adoptionMemberContainer = adoptionMembers;
            adoptionMemberTemplate = adoptionMemberButton;
            adoptionCandidateInfo = adoptionCandidateText;
            adoptionCandidateContainer = adoptionCandidates;
            adoptionCandidateTemplate = adoptionCandidateButton;
            adoptionConfirmButton = adoptionConfirm;
            adoptionMessage = adoptionMessageText;
            _selectedMarriageMember = default;
            _selectedMarriageCandidate = default;
            _selectedAdoptionMember = default;
            _selectedAdoptionCandidate = default;
            _previewPrefersAdoption = false;
            DestroyPreviewStrip();
            Subscribe();
            // Never build button clones in the editor: they would be serialized into
            // the scene without listeners and intercept pointer clicks.
            if (UnityEngine.Application.isPlaying)
                Refresh();
        }

        public void Open()
        {
            if (panelRoot == null)
                return;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Refresh()
        {
            RefreshCount++;
            ClearRows();
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            FamilyFormationView view = gameSession.FamilyFormation;
            titleText.text = "婚姻与收养";
            blockReasonText.text = view.BlockReason;
            blockReasonText.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.BlockReason));

            _selectedMarriageMember = KeepMember(view, _selectedMarriageMember);
            _selectedMarriageCandidate = KeepCandidate(view.MarriageCandidatesFor(
                _selectedMarriageMember), _selectedMarriageCandidate);
            _selectedAdoptionMember = KeepMember(view, _selectedAdoptionMember);
            _selectedAdoptionCandidate = KeepCandidate(view.AdoptionCandidatesFor(
                _selectedAdoptionMember), _selectedAdoptionCandidate);

            BuildMembers(view, marriageMemberContainer, marriageMemberTemplate, _marriageMembers,
                true);
            BuildMembers(view, adoptionMemberContainer, adoptionMemberTemplate, _adoptionMembers,
                false);
            BuildCandidates(view, marriageCandidateContainer, marriageCandidateTemplate,
                _marriageCandidates, true);
            BuildCandidates(view, adoptionCandidateContainer, adoptionCandidateTemplate,
                _adoptionCandidates, false);

            RefreshMarriageText(view);
            RefreshAdoptionText(view);
            RefreshMessages(view);
            RefreshPreview(view);
            if (marriageMemberContainer is RectTransform marriageRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(marriageRect);
            if (adoptionMemberContainer is RectTransform adoptionRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(adoptionRect);
        }

        public void ClickMarriageMember(int index) => Click(_marriageMembers, index, () =>
        {
            _selectedMarriageMember = PersonAt(_marriageMembers, index);
            _selectedMarriageCandidate = default;
        });

        public void ClickMarriageCandidate(int index) => Click(_marriageCandidates, index, () =>
        {
            _selectedMarriageCandidate = PersonAt(_marriageCandidates, index);
        });

        public void ClickAdoptionMember(int index) => Click(_adoptionMembers, index, () =>
        {
            _selectedAdoptionMember = PersonAt(_adoptionMembers, index);
            _selectedAdoptionCandidate = default;
        });

        public void ClickAdoptionCandidate(int index) => Click(_adoptionCandidates, index, () =>
        {
            _selectedAdoptionCandidate = PersonAt(_adoptionCandidates, index);
        });

        /// <summary>Submits exactly one marriage command for the current selection.</summary>
        public void ConfirmMarriage()
        {
            if (gameSession == null || _selectedMarriageMember.IsEmpty ||
                _selectedMarriageCandidate.IsEmpty)
                return;
            MarriageCommitCount++;
            FamilyFormationCommandResult result = gameSession.TryRegisterMarriage(
                _selectedMarriageMember, _selectedMarriageCandidate);
            if (result.Succeeded)
                _selectedMarriageCandidate = default;
            else if (IsOpen)
                Refresh();
        }

        /// <summary>Submits exactly one ordinary-adoption command for the current selection.</summary>
        public void ConfirmAdoption()
        {
            if (gameSession == null || _selectedAdoptionMember.IsEmpty ||
                _selectedAdoptionCandidate.IsEmpty)
                return;
            AdoptionCommitCount++;
            FamilyFormationCommandResult result = gameSession.TryAdopt(
                _selectedAdoptionMember, _selectedAdoptionCandidate);
            if (result.Succeeded)
                _selectedAdoptionCandidate = default;
            else if (IsOpen)
                Refresh();
        }

        private void RefreshMarriageText(FamilyFormationView view)
        {
            if (_selectedMarriageMember.IsEmpty)
            {
                marriageMemberInfo.text = "请选择婚姻发起人。";
                marriageCandidateInfo.text = "请先选择婚姻发起人。";
                marriageConfirmButton.interactable = false;
                return;
            }

            string memberName = view.MemberDisplayName(_selectedMarriageMember);
            string spouseLine = view.SpouseNamesFor(_selectedMarriageMember);
            marriageMemberInfo.text = "发起人：" + memberName +
                                      (string.IsNullOrEmpty(spouseLine)
                                          ? "\n现有配偶：无"
                                          : "\n现有配偶：" + spouseLine);
            if (_selectedMarriageCandidate.IsEmpty)
            {
                marriageCandidateInfo.text = view.MarriageCandidatesFor(_selectedMarriageMember)
                    .Count == 0
                    ? "没有合资格的婚姻候选人。"
                    : "请选择婚姻候选人。";
                marriageConfirmButton.interactable = false;
                return;
            }

            string candidateName = view.CandidateDisplayName(_selectedMarriageCandidate);
            marriageCandidateInfo.text = "将登记：" + memberName + " ↔ " + candidateName +
                                         "\n关系：" + view.RelationNote(_selectedMarriageMember,
                                             _selectedMarriageCandidate) +
                                         "\n确认后新增成员与配偶关系，不可撤销。";
            marriageConfirmButton.interactable = view.CanEdit;
        }

        private void RefreshAdoptionText(FamilyFormationView view)
        {
            if (_selectedAdoptionMember.IsEmpty)
            {
                adoptionMemberInfo.text = "请选择收养人。";
                adoptionCandidateInfo.text = "请先选择收养人。";
                adoptionConfirmButton.interactable = false;
                return;
            }

            string memberName = view.MemberDisplayName(_selectedAdoptionMember);
            adoptionMemberInfo.text = "收养人：" + memberName;
            if (_selectedAdoptionCandidate.IsEmpty)
            {
                adoptionCandidateInfo.text = view.AdoptionCandidatesFor(_selectedAdoptionMember)
                    .Count == 0
                    ? "没有合资格的收养候选人。"
                    : "请选择收养候选人（儿童或成年人）。";
                adoptionConfirmButton.interactable = false;
                return;
            }

            string candidateName = view.CandidateDisplayName(_selectedAdoptionCandidate);
            adoptionCandidateInfo.text = "将办理：" + memberName + " 收养 " + candidateName +
                                         "（养子/养女）\n确认后新增成员与收养关系，" +
                                         "不可撤销；被收养者按继承规则具备继承资格，" +
                                         "当前继承阶段不变。";
            adoptionConfirmButton.interactable = view.CanEdit;
        }

        /// <summary>
        /// 显示当前所选配对的已记录亲属路径、SC-022 规则结论与确认效果。
        /// 后果文案完整显示：配对完整时占用面板的帮助带，
        /// 一旦没有配对被选中，通用帮助文案立即恢复。
        /// </summary>
        private void RefreshPreview(FamilyFormationView view)
        {
            Text strip = EnsurePreviewStrip();
            if (strip == null)
                return;

            bool marriageReady = !_selectedMarriageMember.IsEmpty &&
                                 !_selectedMarriageCandidate.IsEmpty;
            bool adoptionReady = !_selectedAdoptionMember.IsEmpty &&
                                 !_selectedAdoptionCandidate.IsEmpty;
            string line = null;
            if (marriageReady && (!adoptionReady || !_previewPrefersAdoption))
                line = BuildPreviewLine(view, true, _selectedMarriageMember,
                    _selectedMarriageCandidate);
            else if (adoptionReady)
                line = BuildPreviewLine(view, false, _selectedAdoptionMember,
                    _selectedAdoptionCandidate);

            bool show = !string.IsNullOrEmpty(line);
            _previewStrip.SetActive(show);
            if (_helpText != null)
                _helpText.gameObject.SetActive(!show);
            if (!show)
            {
                strip.text = string.Empty;
                return;
            }

            strip.text = line;
            FitPreviewStrip(line);
        }

        private string BuildPreviewLine(FamilyFormationView view, bool marriage, PersonId memberId,
            PersonId candidateId)
        {
            KinshipGraph kinship = gameSession != null && gameSession.Succession != null &&
                                   gameSession.Succession.World != null
                ? gameSession.Succession.World.Kinship
                : null;
            string path = FamilyKinshipPath.Describe(kinship, memberId, candidateId,
                personId => view.CandidateDisplayName(personId));

            // 家族状态被阻断时优先于单配对规则结果：面板根本无法提交，
            // 因此预览必须说明原因，而不能报告“允许”。
            string verdict;
            if (!view.CanEdit)
            {
                verdict = string.IsNullOrWhiteSpace(view.BlockReason)
                    ? "当前不可登记"
                    : "当前不可登记 —— " + view.BlockReason.Trim();
            }
            else if (marriage)
            {
                verdict = FamilyFormationRules.IsDirectLineRelation(kinship, memberId, candidateId)
                    ? "直系血亲或养亲，禁止通婚"
                    : "非直系，允许登记配偶";
            }
            else
            {
                verdict = FamilyFormationRules.WouldCreateAdoptionCycle(kinship, memberId,
                    candidateId)
                    ? "会形成亲子环路，禁止收养"
                    : "不形成亲子环路，允许登记收养";
            }

            string member = view.CandidateDisplayName(memberId);
            string candidate = view.CandidateDisplayName(candidateId);
            // 阻断状态不能把这次写入描述成即将发生：确认按钮已禁用，
            // 因此预览陈述真实后果（待处理状态清除前不会写入），
            // 并保留规则文案不变。
            string consequence;
            if (!view.CanEdit)
            {
                consequence = "确认按钮已禁用，本次不会写入成员、亲属事实或编年史；" +
                              "解除上方提示的待处理状态后，再按同一路径重新确认。";
            }
            else if (marriage)
            {
                consequence = "确认后登记 " + member + " ↔ " + candidate +
                              " 的配偶关系并写入家族编年史，不可撤销。";
            }
            else
            {
                consequence = "确认后登记 " + member + " 收养 " + candidate +
                              " 的养子/养女关系并写入家族编年史，不可撤销；" +
                              "被收养者按继承规则具备继承资格，当前继承阶段不变。";
            }
            return "路径：" + path + "\n校验（SC-022）：" + verdict + " ｜ 预览：" + consequence;
        }

        private void FitPreviewStrip(string line)
        {
            if (_previewText == null || _previewStrip == null ||
                !(_previewStrip.transform is RectTransform stripRect))
                return;
            // 文本矩形在条带内按固定内边距拉伸，换行宽度已与条带一致。
            // 量出该宽度，再把条带增高到测得高度，而不是截断后果文案。
            float needed = _previewText.preferredHeight + 6f;
            float height = Mathf.Clamp(needed, PreviewHeight, PreviewMaximumHeight);
            stripRect.sizeDelta = new Vector2(stripRect.sizeDelta.x, height);
        }

        private Text EnsurePreviewStrip()
        {
            if (_previewText != null)
                return _previewText;
            if (!IsBound || !(panelRoot.transform is RectTransform panelRect))
                return null;

            _helpText = FindHelpText();
            RectTransform helpRect = _helpText != null
                ? _helpText.transform as RectTransform
                : null;

            _previewStrip = new GameObject(PreviewStripName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            _previewStrip.transform.SetParent(panelRoot.transform, false);
            var stripRect = (RectTransform)_previewStrip.transform;
            stripRect.anchorMin = stripRect.anchorMax = stripRect.pivot = new Vector2(0.5f, 0.5f);
            stripRect.localScale = Vector3.one;
            float panelHeight = panelRect.rect.height > 1f ? panelRect.rect.height : 620f;
            float panelWidth = panelRect.rect.width > 1f ? panelRect.rect.width : 920f;
            if (helpRect != null && helpRect.rect.height > 1f)
            {
                stripRect.sizeDelta = new Vector2(helpRect.rect.width, helpRect.rect.height);
                stripRect.anchoredPosition = helpRect.anchoredPosition;
            }
            else
            {
                stripRect.sizeDelta = new Vector2(Mathf.Max(320f, panelWidth - 60f), PreviewHeight);
                stripRect.anchoredPosition = new Vector2(0f, -panelHeight / 2f + 84f);
            }
            Image background = _previewStrip.GetComponent<Image>();
            background.color = SengokuUiTheme.ParchmentDeep;
            background.raycastTarget = false;

            var textObject = new GameObject(PreviewTextName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(_previewStrip.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 3f);
            textRect.offsetMax = new Vector2(-10f, -3f);
            _previewText = textObject.GetComponent<Text>();
            _previewText.font = UnityEngine.Application.isPlaying
                ? SengokuUiTheme.Serif
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _previewText.fontSize = 12;
            _previewText.color = SengokuUiTheme.Ink;
            _previewText.alignment = TextAnchor.UpperLeft;
            _previewText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _previewText.verticalOverflow = VerticalWrapMode.Overflow;
            _previewText.raycastTarget = false;

            _previewStrip.hideFlags = HideFlags.DontSave;
            foreach (Transform child in _previewStrip.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.DontSave;
            _previewStrip.SetActive(false);
            return _previewText;
        }

        /// <summary>
        /// 面板的静态帮助行与预览共用一条带区。它按生成器固定的子节点名查找；
        /// 若某个不同方式构建的面板没有这一行，预览保留自己的带区，
        /// 不触碰其它任何东西。
        /// </summary>
        private Text FindHelpText()
        {
            Transform help = panelRoot != null
                ? panelRoot.transform.Find(FamilyFormationHelpName)
                : null;
            return help != null ? help.GetComponent<Text>() : null;
        }

        private void RefreshMessages(FamilyFormationView view)
        {
            bool visible = !string.IsNullOrWhiteSpace(view.LastMessage);
            marriageMessage.text = view.LastMessage;
            marriageMessage.gameObject.SetActive(visible);
            adoptionMessage.text = view.LastMessage;
            adoptionMessage.gameObject.SetActive(visible);
        }

        private void BuildMembers(FamilyFormationView view, Transform container, Button template,
            List<GameObject> rows, bool marriage)
        {
            if (container == null || template == null)
                return;
            foreach (FamilyFormationMemberOption option in view.Members)
            {
                FamilyFormationMemberOption captured = option;
                GameObject buttonObject = CreateButton(container, template,
                    marriage ? MarriageMemberPrefix : AdoptionMemberPrefix, captured.PersonId,
                    captured.Label);
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        _previewPrefersAdoption = !marriage;
                        if (marriage)
                        {
                            _selectedMarriageMember = captured.PersonId;
                            _selectedMarriageCandidate = default;
                        }
                        else
                        {
                            _selectedAdoptionMember = captured.PersonId;
                            _selectedAdoptionCandidate = default;
                        }
                        if (IsOpen)
                            Refresh();
                    });
                }
                rows.Add(buttonObject);
            }
        }

        private void BuildCandidates(FamilyFormationView view, Transform container, Button template,
            List<GameObject> rows, bool marriage)
        {
            if (container == null || template == null)
                return;
            PersonId memberId = marriage ? _selectedMarriageMember : _selectedAdoptionMember;
            if (memberId.IsEmpty)
                return;
            IReadOnlyList<FamilyFormationCandidateOption> candidates = marriage
                ? view.MarriageCandidatesFor(memberId)
                : view.AdoptionCandidatesFor(memberId);
            foreach (FamilyFormationCandidateOption option in candidates)
            {
                FamilyFormationCandidateOption captured = option;
                GameObject buttonObject = CreateButton(container, template,
                    marriage ? MarriageCandidatePrefix : AdoptionCandidatePrefix,
                    captured.PersonId, captured.Label);
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        _previewPrefersAdoption = !marriage;
                        if (marriage)
                            _selectedMarriageCandidate = captured.PersonId;
                        else
                            _selectedAdoptionCandidate = captured.PersonId;
                        if (IsOpen)
                            Refresh();
                    });
                }
                rows.Add(buttonObject);
            }
        }

        private GameObject CreateButton(Transform container, Button template, string prefix,
            PersonId personId, string label)
        {
            GameObject buttonObject = Instantiate(template.gameObject, container);
            buttonObject.name = prefix + personId.Value;
            buttonObject.SetActive(true);
            Text text = buttonObject.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label;
            return buttonObject;
        }

        private static PersonId KeepMember(FamilyFormationView view, PersonId selected)
        {
            if (selected.IsEmpty)
                return default;
            foreach (FamilyFormationMemberOption option in view.Members)
                if (option.PersonId == selected)
                    return selected;
            return default;
        }

        private static PersonId KeepCandidate(IReadOnlyList<FamilyFormationCandidateOption> options,
            PersonId selected)
        {
            if (selected.IsEmpty)
                return default;
            foreach (FamilyFormationCandidateOption option in options)
                if (option.PersonId == selected)
                    return selected;
            return default;
        }

        private static void Click(List<GameObject> rows, int index, Action select)
        {
            if (index < 0 || index >= rows.Count || rows[index] == null)
                return;
            Button button = rows[index].GetComponent<Button>();
            if (button == null)
                return;
            select();
            button.onClick.Invoke();
        }

        private static PersonId PersonAt(List<GameObject> rows, int index)
        {
            if (index < 0 || index >= rows.Count || rows[index] == null)
                return default;
            string name = rows[index].name;
            int separator = name.LastIndexOf('_');
            if (separator < 0 || separator + 1 >= name.Length)
                return default;
            return PersonId.TryParse(name.Substring(separator + 1), out PersonId personId)
                ? personId
                : default;
        }

        private static string LabelOf(List<GameObject> rows, int index)
        {
            if (index < 0 || index >= rows.Count || rows[index] == null)
                return string.Empty;
            Text text = rows[index].GetComponentInChildren<Text>(true);
            return text != null ? text.text : string.Empty;
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
            if (openButton != null && _openListener == null)
            {
                _openListener = Open;
                openButton.onClick.AddListener(_openListener);
            }
            if (closeButton != null && _closeListener == null)
            {
                _closeListener = Close;
                closeButton.onClick.AddListener(_closeListener);
            }
            // The generator only serializes the confirmation buttons; their runtime click
            // handlers must be bound here, otherwise the visible submit buttons do nothing.
            if (marriageConfirmButton != null && _marriageConfirmListener == null)
            {
                _marriageConfirmListener = ConfirmMarriage;
                marriageConfirmButton.onClick.AddListener(_marriageConfirmListener);
            }
            if (adoptionConfirmButton != null && _adoptionConfirmListener == null)
            {
                _adoptionConfirmListener = ConfirmAdoption;
                adoptionConfirmButton.onClick.AddListener(_adoptionConfirmListener);
            }
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            if (openButton != null && _openListener != null)
                openButton.onClick.RemoveListener(_openListener);
            _openListener = null;
            if (closeButton != null && _closeListener != null)
                closeButton.onClick.RemoveListener(_closeListener);
            _closeListener = null;
            if (marriageConfirmButton != null && _marriageConfirmListener != null)
                marriageConfirmButton.onClick.RemoveListener(_marriageConfirmListener);
            _marriageConfirmListener = null;
            if (adoptionConfirmButton != null && _adoptionConfirmListener != null)
                adoptionConfirmButton.onClick.RemoveListener(_adoptionConfirmListener);
            _adoptionConfirmListener = null;
        }

        private void OnSessionStateChanged()
        {
            // Entering the SC-022 scenario must not steal focus; the player opens the
            // panel explicitly from the HUD.
            if (IsOpen)
                Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            if (UnityEngine.Application.isPlaying)
                Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearRows();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>
        /// 移除运行时预览并把带区还给序列化帮助行，
        /// 这样重绑定或销毁都不会让面板失去自己的文案。
        /// </summary>
        private void DestroyPreviewStrip()
        {
            if (_helpText != null)
                _helpText.gameObject.SetActive(true);
            if (_previewStrip != null)
            {
                _previewStrip.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(_previewStrip);
                else
                    DestroyImmediate(_previewStrip);
            }
            _previewStrip = null;
            _previewText = null;
            _helpText = null;
        }

        private void ClearRows()
        {
            ClearRows(_marriageMembers, marriageMemberContainer, marriageMemberTemplate,
                MarriageMemberPrefix);
            ClearRows(_marriageCandidates, marriageCandidateContainer, marriageCandidateTemplate,
                MarriageCandidatePrefix);
            ClearRows(_adoptionMembers, adoptionMemberContainer, adoptionMemberTemplate,
                AdoptionMemberPrefix);
            ClearRows(_adoptionCandidates, adoptionCandidateContainer, adoptionCandidateTemplate,
                AdoptionCandidatePrefix);
        }

        private static void ClearRows(List<GameObject> rows, Transform container, Button template,
            string prefix)
        {
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                GameObject row = rows[i];
                if (row == null)
                    continue;
                row.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(row);
                else
                    DestroyImmediate(row);
            }
            rows.Clear();
            if (container == null)
                return;
            Transform templateTransform = template != null ? template.transform : null;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child == null || child == templateTransform ||
                    !child.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                child.gameObject.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
