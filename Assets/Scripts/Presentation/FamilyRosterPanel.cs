using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// SC-017 read-only family graph. It builds a snapshot from the live succession
    /// family and kinship graph, lays it out deterministically, and renders typed
    /// relation lines, person cards, a separate unconnected-kin band and a separate
    /// retainer/disciple/guest band inside one scrollable panel. Opening, closing or
    /// refreshing never mutates family, kinship, succession, save or event state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FamilyRosterPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text legendText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform content;
        [SerializeField] private RectTransform edgeLayer;
        [SerializeField] private RectTransform nodeLayer;
        [SerializeField] private GameObject nodeTemplate;
        [SerializeField] private GameObject edgeTemplate;
        [SerializeField] private Text sectionTemplate;
        [SerializeField] private Text heirText;

        /// <summary>运行时构建的选中人物详情栏的固定根节点。</summary>
        public const string DetailRootName = "FamilyRosterDetailPanel";
        public const string DetailHeadingName = "FamilyRosterDetailHeading";
        public const string DetailScrollName = "FamilyRosterDetailScroll";
        public const string DetailContentName = "FamilyRosterDetailContent";
        public const string DetailTextName = "FamilyRosterDetailText";
        public const string DetailScrollbarName = "FamilyRosterDetailScrollbar";
        public const string DetailCloseName = "FamilyRosterDetailCloseButton";
        public const string SelectedMarkName = "FamilyRosterSelectedMark";
        public const string CardHitName = "FamilyRosterCardHit";
        private const float DetailPadding = 8f;
        private const float ScrollbarWidth = 10f;
        // 文字「已选」印记独占卡名下方的一条带区，因此永远不会
        // 压住姓名/身份行，而这两行里第一行最长。
        private const float SelectedMarkInset = 6f;
        private const float SelectedMarkHeight = 16f;
        private const float SelectedMarkGap = 2f;

        private PersonId _selectedPersonId;
        private GameObject _detailRoot;
        private ScrollRect _detailScroll;
        private Scrollbar _detailScrollbar;
        private Text _detailText;
        private Text _detailHeading;
        private RectTransform _detailContent;
        private bool _hasDetailColumn;
        private bool _detailSheetVisible;
        private float _appliedScrollWidth;
        private float _appliedScrollCenterX;
        private float _appliedDetailWidth;
        private float _measuredDetailWidth = -1f;

        private static readonly Color ParentChildColor = new Color(0.88f, 0.73f, 0.34f, 1f);
        private static readonly Color AdoptionColor = new Color(0.68f, 0.47f, 0.86f, 1f);
        private static readonly Color SiblingColor = new Color(0.42f, 0.76f, 0.54f, 1f);
        private static readonly Color SpouseColor = new Color(0.43f, 0.65f, 0.92f, 1f);
        private static readonly Color KinCardColor = SengokuUiTheme.ParchmentDeep;
        private static readonly Color UnconnectedCardColor = SengokuUiTheme.Parchment;
        private static readonly Color HouseholdCardColor = SengokuUiTheme.ParchmentLight;

        private readonly List<GameObject> _nodes = new List<GameObject>();
        private readonly List<FamilyTreePersonNode> _nodeData = new List<FamilyTreePersonNode>();
        private static readonly Dictionary<string, Sprite> PortraitCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly List<GameObject> _edges = new List<GameObject>();
        private readonly List<FamilyTreeRelationEdge> _edgeData = new List<FamilyTreeRelationEdge>();
        private readonly List<IReadOnlyList<FamilyTreePoint>> _relationPaths =
            new List<IReadOnlyList<FamilyTreePoint>>();
        private readonly List<GameObject> _sections = new List<GameObject>();
        private readonly List<GameObject> _selectedMarks = new List<GameObject>();
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private int _staticButtonBindings;
        private int _kinCount;
        private int _unconnectedCount;
        private int _householdCount;
        private int _segmentCount;
        private FamilyTreeSize _lastContentSize;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            titleText != null && legendText != null && scrollRect != null && content != null &&
            edgeLayer != null && nodeLayer != null && nodeTemplate != null && edgeTemplate != null &&
            sectionTemplate != null && heirText != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int VisibleNodeCount => _nodes.Count;
        public int VisibleRelationCount => _edgeData.Count;
        public int VisibleEdgeSegmentCount => _segmentCount;
        public int VisibleSectionCount => _sections.Count;
        public int KinNodeCount => _kinCount;
        public int UnconnectedKinNodeCount => _unconnectedCount;
        public int HouseholdNodeCount => _householdCount;
        public bool HasUnconnectedKin => _unconnectedCount > 0;
        public bool HasHousehold => _householdCount > 0;

        /// <summary>Portrait strip width reserved inside a person card.</summary>
        public const float PortraitWidth = 58f;
        public const string PortraitChildName = "FamilyRosterPortrait";

        /// <summary>
        /// Maps existing identity data (headship and known age) onto the archetype
        /// portraits: the generated clan atlas first, then the owner-provided crops
        /// from the reference gallery. People without a usable signal fall back to
        /// the generic adult face; the mapping never invents identity data and never
        /// uses unrelated artwork.
        /// </summary>
        public static string PortraitKeyFor(FamilyTreePersonNode node)
        {
            if (node.IsHead)
                return "master";
            if (node.HasAge)
            {
                if (node.Age >= 60)
                    return "elder";
                if (node.Age <= 16)
                    return "youth";
            }
            return "main";
        }

        private static Sprite LoadPortrait(string key)
        {
            if (key == null)
                return null;
            if (PortraitCache.TryGetValue(key, out Sprite cached))
                return cached;
            // The generated clan atlas is the only portrait source. A missing atlas
            // returns null so the roster keeps its flat colour instead of falling back
            // to an owner-supplied reference crop.
            Sprite sprite = SengokuUiArt.Portrait(key);
            PortraitCache[key] = sprite;
            return sprite;
        }

        /// <summary>Read-only test seam: the portrait sprite rendered for one person.</summary>
        public Sprite NodePortraitFor(PersonId personId)
        {
            for (int i = 0; i < _nodeData.Count; i++)
                if (_nodeData[i].PersonId == personId)
                    return LoadPortrait(PortraitKeyFor(_nodeData[i]));
            return null;
        }

        public ScrollRect ScrollRect => scrollRect;
        public Vector2 ContentSize =>
            content is RectTransform rect ? rect.sizeDelta : Vector2.zero;
        public FamilyTreeSize LastLayoutContentSize => _lastContentSize;
        public string HeirSummary => heirText != null ? heirText.text : string.Empty;
        public string LegendSummary => legendText != null ? legendText.text : string.Empty;
        public int StaticButtonBindingCount => _staticButtonBindings;

        public PersonId SelectedPersonId => _selectedPersonId;
        public bool HasDetailColumn => _hasDetailColumn;
        public string DetailTextLine => _detailText != null ? _detailText.text : string.Empty;
        public ScrollRect DetailScroll => _detailScroll;
        public Scrollbar DetailScrollbar => _detailScrollbar;
        /// <summary>窄宽度下详情页覆盖图谱期间为 true。</summary>
        public bool DetailSheetVisible =>
            !_hasDetailColumn && _detailSheetVisible && _detailRoot != null &&
            _detailRoot.activeSelf;

        public string DetailHeadingLine =>
            _detailHeading != null ? _detailHeading.text : string.Empty;

        /// <summary>预留详情栏后图谱保留的视口宽度。</summary>
        public float AppliedScrollWidth => _appliedScrollWidth;
        public float AppliedScrollCenterX => _appliedScrollCenterX;

        /// <summary>Backward-compatible row view used by older presentation tests.</summary>
        public int VisibleRowCount => VisibleNodeCount;

        public string NodeText(int index) =>
            index >= 0 && index < _nodes.Count && _nodes[index] != null
                ? GetNodeText(_nodes[index])
                : string.Empty;

        public string RowText(int index) => NodeText(index);

        public string NodeTextFor(PersonId personId)
        {
            for (int i = 0; i < _nodeData.Count; i++)
                if (_nodeData[i].PersonId == personId)
                    return NodeText(i);
            return string.Empty;
        }

        /// <summary>
        /// Read-only pregnancy status for one person, projected from the game-session
        /// snapshot. Returns an empty string when nobody is gestating that person.
        /// </summary>
        public string PregnancyLabelFor(PersonId personId)
        {
            if (gameSession == null || personId.IsEmpty)
                return string.Empty;
            IReadOnlyList<FamilyPregnancyRecord> records = gameSession.FamilyPregnancies;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].GestatingPersonId == personId)
                    return FamilyPregnancyRules.StatusText(records[i], CurrentDate);
            }
            return string.Empty;
        }

        public int NodeInstanceCount(PersonId personId)
        {
            int count = 0;
            for (int i = 0; i < _nodeData.Count; i++)
                if (_nodeData[i].PersonId == personId)
                    count++;
            return count;
        }

        public string SectionText(int index)
        {
            if (index < 0 || index >= _sections.Count || _sections[index] == null)
                return string.Empty;
            Text text = _sections[index].GetComponent<Text>();
            return text != null ? text.text : string.Empty;
        }

        public bool HasSectionContaining(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < _sections.Count; i++)
            {
                Text text = _sections[i] != null ? _sections[i].GetComponent<Text>() : null;
                if (text != null && text.text != null && text.text.Contains(value))
                    return true;
            }
            return false;
        }

        public KinshipType RelationType(int index) =>
            index >= 0 && index < _edgeData.Count ? _edgeData[index].Type : default;

        /// <summary>Read-only test seam for the routed polyline of one relation.</summary>
        public IReadOnlyList<FamilyTreePoint> RelationPath(int index) =>
            index >= 0 && index < _relationPaths.Count
                ? _relationPaths[index]
                : System.Array.Empty<FamilyTreePoint>();

        /// <summary>
        /// Read-only test seam returning the routed polyline for a recorded relation.
        /// Symmetric relations match either endpoint order.
        /// </summary>
        public IReadOnlyList<FamilyTreePoint> RelationPathFor(PersonId from, PersonId to,
            KinshipType type)
        {
            for (int i = 0; i < _edgeData.Count; i++)
            {
                FamilyTreeRelationEdge edge = _edgeData[i];
                if (edge.Type != type)
                    continue;
                bool matches = edge.From == from && edge.To == to;
                if (!matches && edge.IsSymmetric)
                    matches = edge.From == to && edge.To == from;
                if (matches)
                    return _relationPaths[i];
            }
            return System.Array.Empty<FamilyTreePoint>();
        }

        public static Color RelationColor(KinshipType type)
        {
            switch (type)
            {
                case KinshipType.ParentChild: return ParentChildColor;
                case KinshipType.Adoption: return AdoptionColor;
                case KinshipType.Sibling: return SiblingColor;
                case KinshipType.Spouse: return SpouseColor;
                default: return Color.white;
            }
        }

        public void BindReferences(GameSession session, GameObject panel, Button open, Button close,
            Text title, Text legend, ScrollRect scroll, Transform graphContent,
            RectTransform edges, RectTransform cards, GameObject cardTemplate, GameObject lineTemplate,
            Text sectionLabelTemplate, Text heir)
        {
            Unsubscribe();
            ClearGenerated();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            titleText = title;
            legendText = legend;
            scrollRect = scroll;
            content = graphContent;
            edgeLayer = edges;
            nodeLayer = cards;
            nodeTemplate = cardTemplate;
            edgeTemplate = lineTemplate;
            sectionTemplate = sectionLabelTemplate;
            heirText = heir;
            Subscribe();
            // Do not build runtime clones here: this seam is also used by the
            // transactional scene generator, and generated cards/lines must never be
            // serialized into FoundationScene. Open/OnEnable refreshes in play mode.
            ClearGenerated();
            DestroyDetailRoot();
            _selectedPersonId = default;
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearGenerated();
            DestroyDetailRoot();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (panelRoot == null)
                return;
            panelRoot.SetActive(true);
            Refresh();
            FocusSelectedCard();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>Rebuilds the graph from the live read-only session state.</summary>
        public void Refresh()
        {
            ClearGenerated();
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            FamilyTreeGraphSnapshot graph =
                FamilyTreeGraphBuilder.Build(gameSession.Succession, CurrentDate);
            FamilyTreeLayoutResult layout = FamilyTreeLayout.Calculate(graph);
            _lastContentSize = layout.ContentSize;

            titleText.text = "家族名册";
            legendText.text = "家谱连线：亲子 金 · 收养 紫 · 手足 绿 · 配偶 蓝；未连接亲族与家臣分区列出。";

            if (content is RectTransform contentRect)
                contentRect.sizeDelta = new Vector2(Mathf.Max(1f, layout.ContentSize.Width),
                    Mathf.Max(1f, layout.ContentSize.Height));

            for (int i = 0; i < layout.Sections.Count; i++)
                CreateSection(layout.Sections[i]);

            var placements = new Dictionary<PersonId, FamilyTreeNodePlacement>();
            for (int i = 0; i < layout.Nodes.Count; i++)
                placements[layout.Nodes[i].PersonId] = layout.Nodes[i];

            for (int i = 0; i < graph.Persons.Count; i++)
            {
                FamilyTreePersonNode person = graph.Persons[i];
                if (!placements.TryGetValue(person.PersonId, out FamilyTreeNodePlacement placement))
                    continue;
                CreateNode(person, placement);
            }

            for (int i = 0; i < graph.Relations.Count; i++)
            {
                FamilyTreeRelationEdge relation = graph.Relations[i];
                if (!placements.TryGetValue(relation.From, out FamilyTreeNodePlacement from) ||
                    !placements.TryGetValue(relation.To, out FamilyTreeNodePlacement to))
                    continue;
                CreateRelation(relation, from, to, layout.Nodes);
            }

            _kinCount = graph.KinshipMembers.Count;
            _unconnectedCount = graph.UnconnectedKin.Count;
            _householdCount = graph.HouseholdMembers.Count;

            heirText.text = string.IsNullOrEmpty(gameSession.FixedHeirSummary)
                ? "当前模式没有固定继承人。"
                : gameSession.FixedHeirSummary;

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;

            EnsureSelection(graph);
            ApplyPageLayout();
            RefreshDetail(graph, layout);
            ApplySelectionHighlight();
        }

        /// <summary>
        /// 为详情栏选中一名已显示的人物。未知或空的 ID 会被忽略，
        /// 因此点击过期的卡片绝不会把详情栏指向当前图谱之外的人。
        /// 选中不改变任何会话状态。窄面板上详情页会打开，
        /// 使选中人物的事实不会被藏起来。
        /// </summary>
        public void SelectPerson(PersonId personId)
        {
            if (personId.IsEmpty || !IsBound)
                return;
            FamilyTreeGraphSnapshot graph =
                FamilyTreeGraphBuilder.Build(gameSession.Succession, CurrentDate);
            if (!graph.Contains(personId))
                return;
            _selectedPersonId = personId;
            FamilyTreeLayoutResult layout = FamilyTreeLayout.Calculate(graph);
            if (!_hasDetailColumn)
                _detailSheetVisible = true;
            RefreshDetail(graph, layout);
            ApplySelectionHighlight();
        }

        /// <summary>关闭窄宽度详情页并回到图谱。</summary>
        public void CloseDetailSheet()
        {
            _detailSheetVisible = false;
            if (_detailRoot != null && !_hasDetailColumn)
                _detailRoot.SetActive(false);
        }

        /// <summary>
        /// 按页面样式器实际给本面板的宽度应用页面布局。宽卡片并排显示
        /// 图谱与选中人物栏；窄卡片保留完整图谱宽度，并把同样的事实
        /// 放进从卡片选中打开的堆叠详情页。这里不恢复任何序列化几何，
        /// 重复应用也无副作用。
        /// </summary>
        public void ApplyPageLayout()
        {
            if (!IsBound || !(panelRoot.transform is RectTransform panelRect))
                return;
            if (!(scrollRect.transform is RectTransform scrollArea))
                return;

            float panelWidth = panelRect.rect.width > 1f
                ? panelRect.rect.width
                : panelRect.sizeDelta.x;
            float liveHeight = scrollArea.rect.height > 1f
                ? scrollArea.rect.height
                : scrollArea.sizeDelta.y;
            float liveY = scrollArea.anchoredPosition.y;
            float innerWidth = Mathf.Max(1f, panelWidth - FamilyRosterDetailLayout.SideInset * 2f);

            if (!FamilyRosterDetailLayout.TryCalculate(panelWidth, out FamilyRosterSplit split))
            {
                _hasDetailColumn = false;
                _appliedScrollWidth = innerWidth;
                _appliedScrollCenterX = 0f;
                scrollArea.sizeDelta = new Vector2(innerWidth, liveHeight);
                scrollArea.anchoredPosition = new Vector2(0f, liveY);

                EnsureDetailRoot();
                if (_detailRoot != null && _detailRoot.transform is RectTransform sheetRect)
                {
                    _appliedDetailWidth = innerWidth;
                    sheetRect.sizeDelta = new Vector2(innerWidth, liveHeight);
                    sheetRect.anchoredPosition = new Vector2(0f, liveY);
                    _detailRoot.SetActive(_detailSheetVisible);
                }
                if (_detailRoot != null && _detailRoot.activeSelf)
                    MeasureDetailBody(false);
                return;
            }

            _hasDetailColumn = true;
            _detailSheetVisible = false;
            _appliedScrollWidth = split.ScrollWidth;
            _appliedScrollCenterX = split.ScrollCenterX;
            _appliedDetailWidth = split.DetailWidth;
            scrollArea.sizeDelta = new Vector2(split.ScrollWidth, liveHeight);
            scrollArea.anchoredPosition = new Vector2(split.ScrollCenterX, liveY);

            EnsureDetailRoot();
            if (_detailRoot != null && _detailRoot.transform is RectTransform detailRect)
            {
                detailRect.sizeDelta = new Vector2(split.DetailWidth, liveHeight);
                detailRect.anchoredPosition = new Vector2(split.DetailCenterX, liveY);
                _detailRoot.SetActive(true);
            }
            // 正文是按先前宽度换行的；这里重新量一次，
            // 免得从窄切宽后正文仍保持过短的高度。
            MeasureDetailBody(false);
        }

        private void EnsureSelection(FamilyTreeGraphSnapshot graph)
        {
            if (!_selectedPersonId.IsEmpty && graph.Contains(_selectedPersonId))
                return;
            SuccessionState state = gameSession != null ? gameSession.Succession : null;
            PersonId headId = state != null ? state.HeadPersonId : default;
            _selectedPersonId = !headId.IsEmpty && graph.Contains(headId)
                ? headId
                : graph.Count > 0 ? graph.Persons[0].PersonId : default;
        }

        private void RefreshDetail(FamilyTreeGraphSnapshot graph, FamilyTreeLayoutResult layout)
        {
            if (_detailText == null)
                return;
            _detailText.text = BuildDetailText(graph, layout, _selectedPersonId);
            if (_detailHeading != null)
            {
                _detailHeading.text = graph != null &&
                                      graph.TryGetPerson(_selectedPersonId,
                                          out FamilyTreePersonNode selected) &&
                                      !string.IsNullOrWhiteSpace(selected.DisplayName)
                    ? "人物详情 · " + selected.DisplayName
                    : "人物详情";
            }

            if (_detailRoot != null)
                _detailRoot.SetActive(_hasDetailColumn || _detailSheetVisible);
            UpdateDetailChrome();
            // 正文可见后才量：首选高度取自 Text 自身的布局，
            // 保持“先可见再测量”的顺序，避免在停用的面板上测量。
            MeasureDetailBody(true);
            if (_detailScroll != null)
                _detailScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 按当前实时布局给出的宽度换行并测量详情正文。宽度改变
        /// （窄页切侧栏，或页面缩放）会重新测量，因为按旧宽度测得的高度
        /// 会裁切或留空。布局事件调用时 <paramref name="force"/> 为 false，
        /// 宽度未变时只做一次比较，不产生逐帧开销。
        /// </summary>
        private void MeasureDetailBody(bool force)
        {
            if (_detailText == null)
                return;
            float width = Mathf.Max(1f, _appliedDetailWidth - DetailPadding * 2f -
                                         (_detailScrollbar != null ? ScrollbarWidth + 2f : 0f));
            var textRect = _detailText.rectTransform;
            bool widthChanged = Mathf.Abs(_measuredDetailWidth - width) > 0.5f;
            if (!force && !widthChanged)
                return;

            // 先设宽度：首选高度按当前矩形宽度换行，
            // 在应用宽度之前测量会得到错误高度。
            textRect.sizeDelta = new Vector2(width, textRect.sizeDelta.y);
            textRect.anchoredPosition = Vector2.zero;
            float height = Mathf.Max(1f, _detailText.preferredHeight);
            textRect.sizeDelta = new Vector2(width, height);
            if (_detailContent != null)
                _detailContent.sizeDelta = new Vector2(width, height);
            _measuredDetailWidth = width;
        }

        /// <summary>
        /// 关闭控件只属于堆叠详情页；并排栏始终可见，
        /// 因此保留完整宽度用于事实。
        /// </summary>
        private void UpdateDetailChrome()
        {
            if (_detailRoot == null)
                return;
            Transform close = _detailRoot.transform.Find(DetailCloseName);
            if (close != null)
                close.gameObject.SetActive(!_hasDetailColumn);
        }

        /// <summary>
        /// 一名选中人物的面向玩家详情。每一行都取自图谱快照、
        /// 实时继承状态或已记录的亲属事实。不做任何推断：
        /// 缺失的生平记录、未知年龄、没有记录亲属关系的人都会如实说明，
        /// 而不是编造关系。
        /// </summary>
        public string BuildDetailText(FamilyTreeGraphSnapshot graph,
            FamilyTreeLayoutResult layout, PersonId personId)
        {
            if (graph == null || personId.IsEmpty ||
                !graph.TryGetPerson(personId, out FamilyTreePersonNode node))
                return "尚未选择人物。\n点击家族成员卡查看身份、继承资格与亲属事实。";

            FamilyTreeNodePlacement placement = default;
            bool placed = layout != null && layout.TryGetNode(personId, out placement);

            var builder = new StringBuilder(320);
            // 身份行与卡片完全一致，包括家主/继承人标记，
            // 详情栏与图谱对同一个人的描述永远不会矛盾。
            string identityLine = FamilyTreeCardText.Format(node, placement);
            int lineBreak = identityLine.IndexOf('\n');
            builder.Append(lineBreak > 0 ? identityLine.Substring(0, lineBreak) : identityLine)
                .Append('\n');
            builder.Append(node.RoleName);
            if (placed)
            {
                if (placement.Section == FamilyTreeLayoutSection.Generation)
                    builder.Append(" · 第").Append(placement.Generation).Append("代");
                else if (placement.Section == FamilyTreeLayoutSection.UnconnectedKin)
                    builder.Append(" · 未连接亲族");
                else
                    builder.Append(" · 附属成员");
            }
            builder.Append('\n');

            builder.Append("年龄：").Append(node.HasAge ? node.Age + "岁" : "不详");
            builder.Append("　状态：");
            if (!node.HasLife)
                builder.Append("生命记录未知");
            else if (!node.IsAlive)
                builder.Append("已故");
            else if (node.IsRetired)
                builder.Append("已隐居");
            else
                builder.Append("在世");
            builder.Append('\n');

            if (node.IsHead)
                builder.Append("现任家主\n");
            else if (node.IsDesignatedHeir)
                builder.Append(node.IsDesignatedHeirValid ? "指定继承人（有效）\n" : "指定继承人（已失效）\n");

            GameDate date = CurrentDate;
            SuccessionState state = gameSession != null ? gameSession.Succession : null;
            if (state != null && date.IsValid && node.IsAlive && !node.IsHead)
            {
                builder.Append("继承资格：")
                    .Append(SuccessionService.IsEligibleSuccessor(state, personId, date)
                        ? "符合"
                        : "不符合")
                    .Append("　摄政资格：")
                    .Append(SuccessionService.IsEligibleRegent(state, personId, date)
                        ? "符合"
                        : "不符合")
                    .Append('\n');
            }

            string pregnancy = PregnancyLabelFor(personId);
            if (!string.IsNullOrEmpty(pregnancy))
                builder.Append("妊娠：").Append(pregnancy).Append('\n');

            AppendKinshipFacts(builder, graph, personId);
            builder.Append("\n来源：家族成员名册与亲属关系记录（只读）");
            return builder.ToString();
        }

        private void AppendKinshipFacts(StringBuilder builder, FamilyTreeGraphSnapshot graph,
            PersonId personId)
        {
            SuccessionWorld world = gameSession != null && gameSession.Succession != null
                ? gameSession.Succession.World
                : null;
            if (world == null)
            {
                builder.Append("亲属事实：无记录");
                return;
            }

            var links = new List<KinshipLink>();
            foreach (KinshipLink link in world.Kinship.LinksFor(personId))
            {
                PersonId other = link.From == personId ? link.To : link.From;
                if (graph.Contains(other))
                    links.Add(link);
            }
            links.Sort((left, right) =>
            {
                int type = ((int)left.Type).CompareTo((int)right.Type);
                if (type != 0)
                    return type;
                string leftOther = (left.From == personId ? left.To : left.From).Value ??
                                   string.Empty;
                string rightOther = (right.From == personId ? right.To : right.From).Value ??
                                    string.Empty;
                return string.Compare(leftOther, rightOther, StringComparison.Ordinal);
            });

            builder.Append("亲属事实（").Append(links.Count).Append("）\n");
            if (links.Count == 0)
            {
                builder.Append("无记录\n");
                return;
            }
            for (int i = 0; i < links.Count; i++)
            {
                KinshipLink link = links[i];
                PersonId other = link.From == personId ? link.To : link.From;
                builder.Append("- ").Append(FamilyKinshipPath.FactLabel(link, personId))
                    .Append("：").Append(DisplayNameOf(graph, other)).Append('\n');
            }
        }

        private void ApplySelectionHighlight()
        {
            for (int i = 0; i < _nodes.Count && i < _nodeData.Count; i++)
            {
                GameObject card = _nodes[i];
                if (card == null)
                    continue;
                bool selected = _nodeData[i].PersonId == _selectedPersonId;
                Text text = card.GetComponentInChildren<Text>(true);
                if (text != null)
                    text.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                if (i < _selectedMarks.Count && _selectedMarks[i] != null)
                    _selectedMarks[i].SetActive(selected);
            }
        }

        /// <summary>
        /// 每次打开时给选中卡片一次键盘焦点，使方向键与 Submit
        /// 无需鼠标即可到达图谱。没有事件系统时什么都不做。
        /// </summary>
        public void FocusSelectedCard()
        {
            UnityEngine.EventSystems.EventSystem events =
                UnityEngine.EventSystems.EventSystem.current;
            if (events == null)
                return;
            GameObject current = events.currentSelectedGameObject;
            if (current != null && current.transform.IsChildOf(transform))
                return;
            for (int i = 0; i < _nodeData.Count && i < _nodes.Count; i++)
            {
                if (_nodeData[i].PersonId != _selectedPersonId || _nodes[i] == null)
                    continue;
                Button button = _nodes[i].GetComponent<Button>();
                if (button != null)
                    events.SetSelectedGameObject(button.gameObject);
                return;
            }
        }

        private static string DisplayNameOf(FamilyTreeGraphSnapshot graph, PersonId personId)
        {
            return graph.TryGetPerson(personId, out FamilyTreePersonNode node) &&
                   !string.IsNullOrWhiteSpace(node.DisplayName)
                ? node.DisplayName
                : personId.Value ?? string.Empty;
        }

        private void EnsureDetailRoot()
        {
            if (_detailRoot != null || panelRoot == null || sectionTemplate == null)
                return;

            _detailRoot = new GameObject(DetailRootName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            _detailRoot.transform.SetParent(panelRoot.transform, false);
            var rootRect = (RectTransform)_detailRoot.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.localScale = Vector3.one;
            Image background = _detailRoot.GetComponent<Image>();
            background.color = SengokuUiTheme.Parchment;
            background.raycastTarget = true;

            Text heading = Instantiate(sectionTemplate, _detailRoot.transform);
            heading.gameObject.name = DetailHeadingName;
            heading.text = "人物详情";
            heading.color = SengokuUiTheme.InkSoft;
            heading.alignment = TextAnchor.MiddleLeft;
            heading.raycastTarget = false;
            heading.gameObject.SetActive(true);
            if (heading.transform is RectTransform headingRect)
            {
                headingRect.anchorMin = new Vector2(0f, 1f);
                headingRect.anchorMax = new Vector2(1f, 1f);
                headingRect.pivot = new Vector2(0f, 1f);
                headingRect.offsetMin = new Vector2(8f, -28f);
                headingRect.offsetMax = new Vector2(-(DetailPadding + 54f), 0f);
            }
            _detailHeading = heading;

            // 视口本身可命中，滚轮与拖拽滚动才有效；可见的
            // 纵向滚动条让长事实列表可达、可发现。
            var scrollObject = new GameObject(DetailScrollName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollObject.transform.SetParent(_detailRoot.transform, false);
            var scrollAreaRect = (RectTransform)scrollObject.transform;
            scrollAreaRect.anchorMin = Vector2.zero;
            scrollAreaRect.anchorMax = Vector2.one;
            scrollAreaRect.pivot = new Vector2(0.5f, 0.5f);
            scrollAreaRect.offsetMin = new Vector2(DetailPadding, DetailPadding);
            scrollAreaRect.offsetMax = new Vector2(-(DetailPadding + ScrollbarWidth + 4f), -30f);
            Image scrollBackground = scrollObject.GetComponent<Image>();
            scrollBackground.color = SengokuUiTheme.ParchmentLight;
            scrollBackground.raycastTarget = true;

            var contentObject = new GameObject(DetailContentName, typeof(RectTransform));
            contentObject.transform.SetParent(scrollObject.transform, false);
            _detailContent = (RectTransform)contentObject.transform;
            _detailContent.anchorMin = _detailContent.anchorMax = new Vector2(0f, 1f);
            _detailContent.pivot = new Vector2(0f, 1f);
            _detailContent.anchoredPosition = Vector2.zero;
            _detailContent.sizeDelta = new Vector2(FamilyRosterDetailLayout.MinimumDetailWidth,
                1f);

            _detailScroll = scrollObject.GetComponent<ScrollRect>();
            _detailScroll.viewport = scrollAreaRect;
            _detailScroll.content = _detailContent;
            _detailScroll.horizontal = false;
            _detailScroll.vertical = true;
            _detailScroll.movementType = ScrollRect.MovementType.Clamped;
            _detailScroll.scrollSensitivity = 24f;

            Text body = Instantiate(sectionTemplate, _detailContent);
            body.gameObject.name = DetailTextName;
            body.text = string.Empty;
            body.color = SengokuUiTheme.Ink;
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.raycastTarget = false;
            body.gameObject.SetActive(true);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0f, 1f);
            bodyRect.pivot = new Vector2(0f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(FamilyRosterDetailLayout.MinimumDetailWidth, 1f);
            _detailText = body;

            CreateDetailScrollbar();
            CreateDetailCloseButton();

            MarkRuntimeOnly(_detailRoot);
            _detailRoot.SetActive(false);
        }

        private void CreateDetailScrollbar()
        {
            var barObject = new GameObject(DetailScrollbarName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            barObject.transform.SetParent(_detailRoot.transform, false);
            var barRect = (RectTransform)barObject.transform;
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 1f);
            barRect.offsetMin = new Vector2(-(ScrollbarWidth + DetailPadding), DetailPadding);
            barRect.offsetMax = new Vector2(-DetailPadding, -30f);
            Image track = barObject.GetComponent<Image>();
            track.color = SengokuUiTheme.PaperLine;
            track.raycastTarget = true;

            var handleObject = new GameObject("Handle", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            handleObject.transform.SetParent(barObject.transform, false);
            var handleRect = (RectTransform)handleObject.transform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = Vector2.zero;
            Image handle = handleObject.GetComponent<Image>();
            handle.color = SengokuUiTheme.GoldDim;
            handle.raycastTarget = true;

            _detailScrollbar = barObject.GetComponent<Scrollbar>();
            _detailScrollbar.handleRect = handleRect;
            _detailScrollbar.targetGraphic = handle;
            _detailScrollbar.direction = Scrollbar.Direction.BottomToTop;
            _detailScroll.verticalScrollbar = _detailScrollbar;
            _detailScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            _detailScroll.verticalScrollbarSpacing = 2f;
        }

        private void CreateDetailCloseButton()
        {
            var buttonObject = new GameObject(DetailCloseName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(_detailRoot.transform, false);
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-6f, -4f);
            buttonRect.sizeDelta = new Vector2(52f, 20f);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = SengokuUiTheme.ParchmentDeep;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;

            Text label = Instantiate(sectionTemplate, buttonObject.transform);
            label.gameObject.name = "Label";
            label.text = "族谱";
            label.fontSize = Mathf.Max(11, sectionTemplate.fontSize - 2);
            label.color = SengokuUiTheme.Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.gameObject.SetActive(true);

            button.onClick.AddListener(CloseDetailSheet);
            buttonObject.SetActive(false);
        }

        /// <summary>
        /// 运行时详情对象绝不能序列化进场景：否则编辑器侧刷新、
        /// 生成器运行或测试销毁都会在保存的层级里留下生成的克隆。
        /// </summary>
        private static void MarkRuntimeOnly(GameObject root)
        {
            if (root == null)
                return;
            root.hideFlags = HideFlags.DontSave;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.DontSave;
        }

        private void DestroyDetailRoot()
        {
            if (_detailRoot == null)
                return;
            _detailRoot.SetActive(false);
            if (UnityEngine.Application.isPlaying)
                Destroy(_detailRoot);
            else
                DestroyImmediate(_detailRoot);
            _detailRoot = null;
            _detailScroll = null;
            _detailScrollbar = null;
            _detailText = null;
            _detailHeading = null;
            _detailContent = null;
            _measuredDetailWidth = -1f;
            _hasDetailColumn = false;
            _detailSheetVisible = false;
        }

        private GameDate CurrentDate =>
            gameSession != null && gameSession.WorldClock != null
                ? gameSession.WorldClock.CurrentDate
                : WorldClock.NewGameDate;

        private void CreateNode(FamilyTreePersonNode person, FamilyTreeNodePlacement placement)
        {
            GameObject card = Instantiate(nodeTemplate, nodeLayer);
            card.name = "FamilyRosterNode_" + (person.PersonId.Value ?? string.Empty);
            RectTransform rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(placement.Position.X, -placement.Position.Y);
                rect.sizeDelta = new Vector2(placement.Size.Width, placement.Size.Height);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            Image background = card.GetComponent<Image>();
            Color cardColor = placement.Section == FamilyTreeLayoutSection.Household
                ? HouseholdCardColor
                : placement.Section == FamilyTreeLayoutSection.UnconnectedKin
                    ? UnconnectedCardColor
                    : KinCardColor;
            if (background != null)
            {
                // 卡片上方的指针接收面负责鼠标命中；卡片图形
                // 仍驱动键盘按钮的着色，使焦点保持可见。
                background.raycastTarget = false;
                background.color = cardColor;
            }

            // 键盘路径：卡片是普通 Selectable，Tab/方向键焦点与
            // Submit 无需指针即可工作。指针点击由下方命中面接收，
            // 它会拒绝实际属于滚动拖拽的点击。
            Button cardButton = card.GetComponent<Button>();
            if (cardButton == null)
                cardButton = card.AddComponent<Button>();
            cardButton.targetGraphic = background;
            cardButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            cardButton.transition = Selectable.Transition.ColorTint;
            // ColorTint 会乘上目标图形自身的颜色，因此常态必须保持白色：
            // 若常态也设成卡片色，羊皮纸会被平方、每张卡都变暗。
            // 高亮/选中时只变亮，按下时略暗，既有状态反馈又不重复着色。
            cardButton.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 0.97f, 0.9f, 1f),
                pressedColor = new Color(0.9f, 0.87f, 0.82f, 1f),
                selectedColor = new Color(1f, 0.98f, 0.93f, 1f),
                disabledColor = new Color(0.85f, 0.83f, 0.78f, 1f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            PersonId capturedPersonId = person.PersonId;
            cardButton.onClick.AddListener(() => SelectPerson(capturedPersonId));

            Sprite portrait = LoadPortrait(PortraitKeyFor(person));

            Text text = card.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                string pregnancy = PregnancyLabelFor(person.PersonId);
                text.text = FormatNode(person, placement) +
                            (pregnancy.Length > 0 ? "\n" + pregnancy : string.Empty);
                text.color = SengokuUiTheme.Ink;
                text.raycastTarget = false;
                if (pregnancy.Length > 0)
                {
                    // The full status is longer than one card line. Best fit keeps the
                    // exact text inside the standard fixed card instead of spilling
                    // over the portrait or the neighbouring node; truncate guarantees
                    // no drawing outside the card rect if a font is unavailable. Both
                    // wrap settings are pregnancy-only so every normal card keeps its
                    // original serialized template overflow mode.
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 8;
                    text.resizeTextMaxSize = Mathf.Max(16, text.fontSize);
                }
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                float leftInset = portrait != null ? PortraitWidth + 8f : 10f;
                // 底边内边距为选中印记带预留空间；没有它，
                // 下方绘制的标记与标签会占用同一个矩形。
                textRect.offsetMin = new Vector2(leftInset,
                    SelectedMarkInset + SelectedMarkHeight + SelectedMarkGap);
                textRect.offsetMax = new Vector2(-10f, -8f);
            }

            if (portrait != null)
            {
                var portraitObject = new GameObject(PortraitChildName,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                portraitObject.transform.SetParent(card.transform, false);
                RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
                portraitRect.anchorMin = new Vector2(0f, 0f);
                portraitRect.anchorMax = new Vector2(0f, 1f);
                portraitRect.pivot = new Vector2(0f, 0.5f);
                portraitRect.offsetMin = new Vector2(3f, 3f);
                portraitRect.offsetMax = new Vector2(3f + PortraitWidth, -3f);
                Image portraitImage = portraitObject.GetComponent<Image>();
                portraitImage.sprite = portrait;
                portraitImage.color = Color.white;
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;
            }

            // 文字选中标记：当前选中无需依赖颜色或悬停即可读出，
            // 且从不改写人物卡文字。它位于标签矩形预留的页脚带内，
            // 印记与姓名/身份行不会互相压字。
            Text mark = Instantiate(sectionTemplate, card.transform);
            mark.gameObject.name = SelectedMarkName;
            mark.text = "已选";
            mark.fontSize = Mathf.Max(11, sectionTemplate.fontSize - 2);
            mark.color = SengokuUiTheme.Vermilion;
            mark.alignment = TextAnchor.MiddleRight;
            mark.raycastTarget = false;
            var markRect = mark.rectTransform;
            markRect.anchorMin = markRect.anchorMax = new Vector2(1f, 0f);
            markRect.pivot = new Vector2(1f, 0f);
            markRect.anchoredPosition = new Vector2(-8f, SelectedMarkInset);
            markRect.sizeDelta = new Vector2(44f, SelectedMarkHeight);
            mark.gameObject.SetActive(false);
            mark.gameObject.hideFlags = HideFlags.DontSave;
            _selectedMarks.Add(mark.gameObject);

            // 指针接收面：从卡片上起手的拖拽只滚动图谱、绝不选中，
            // 滚轮滚动也仍能到达图谱 ScrollRect。
            var hitObject = new GameObject(CardHitName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            hitObject.transform.SetParent(card.transform, false);
            var hitRect = (RectTransform)hitObject.transform;
            hitRect.anchorMin = Vector2.zero;
            hitRect.anchorMax = Vector2.one;
            hitRect.offsetMin = Vector2.zero;
            hitRect.offsetMax = Vector2.zero;
            Image hitImage = hitObject.GetComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0f);
            hitImage.raycastTarget = true;
            hitObject.AddComponent<FamilyRosterCardInteraction>()
                .Bind(scrollRect, person.PersonId, SelectPerson);
            hitObject.hideFlags = HideFlags.DontSave;

            card.SetActive(true);
            _nodes.Add(card);
            _nodeData.Add(person);
        }

        private void CreateSection(FamilyTreeSectionPlacement section)
        {
            Text label = Instantiate(sectionTemplate, nodeLayer);
            label.gameObject.name = "FamilyRosterSection_" + section.Section;
            RectTransform rect = label.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(section.Position.X, -section.Position.Y);
                rect.sizeDelta = new Vector2(section.Size.Width, section.Size.Height);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }
            label.text = section.Title;
            label.color = SengokuUiTheme.InkSoft;
            label.raycastTarget = false;
            label.gameObject.SetActive(true);
            _sections.Add(label.gameObject);
        }

        private void CreateRelation(FamilyTreeRelationEdge relation,
            FamilyTreeNodePlacement from, FamilyTreeNodePlacement to,
            IReadOnlyList<FamilyTreeNodePlacement> placements)
        {
            Color color = RelationColor(relation.Type);
            float thickness = relation.Type == KinshipType.ParentChild ||
                              relation.Type == KinshipType.Adoption
                ? 3f
                : 2f;
            if (!FamilyTreeRouting.TryBuildPath(relation.Type, from, to, placements,
                    out IReadOnlyList<FamilyTreePoint> path))
            {
                // Explicit route failure: keep the relation data but draw no line
                // rather than falling back to a path through a person card.
                _edgeData.Add(relation);
                _relationPaths.Add(System.Array.Empty<FamilyTreePoint>());
                return;
            }
            for (int i = 0; i < path.Count - 1; i++)
                CreateSegment(path[i], path[i + 1], thickness, color,
                    "FamilyRosterEdge_" + relation.TypeName + "_" +
                    (relation.From.Value ?? string.Empty) + "_" + (relation.To.Value ?? string.Empty),
                    i);
            _edgeData.Add(relation);
            _relationPaths.Add(path);
        }

        private static string FormatNode(FamilyTreePersonNode person,
            FamilyTreeNodePlacement placement) => FamilyTreeCardText.Format(person, placement);

        private void CreateSegment(FamilyTreePoint start, FamilyTreePoint end, float thickness,
            Color color, string baseName, int index)
        {
            Vector2 anchoredStart = new Vector2(start.X, -start.Y);
            Vector2 anchoredEnd = new Vector2(end.X, -end.Y);
            Vector2 delta = anchoredEnd - anchoredStart;
            float length = delta.magnitude;
            if (length < 0.01f)
                return;

            GameObject segment = Instantiate(edgeTemplate, edgeLayer);
            segment.name = baseName + "_" + index;
            RectTransform rect = segment.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = (anchoredStart + anchoredEnd) / 2f;
                rect.sizeDelta = new Vector2(length, thickness);
                rect.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            Image image = segment.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                image.raycastTarget = false;
            }

            segment.SetActive(true);
            _edges.Add(segment);
            _segmentCount++;
        }

        private static string GetNodeText(GameObject card)
        {
            Text text = card != null ? card.GetComponentInChildren<Text>(true) : null;
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
        }

        private void UnwireStaticButtons()
        {
            if (_openListener != null)
            {
                if (openButton != null)
                    openButton.onClick.RemoveListener(_openListener);
                _openListener = null;
                _staticButtonBindings--;
            }
            if (_closeListener != null)
            {
                if (closeButton != null)
                    closeButton.onClick.RemoveListener(_closeListener);
                _closeListener = null;
                _staticButtonBindings--;
            }
            if (_staticButtonBindings < 0)
                _staticButtonBindings = 0;
        }

        private void OnSessionStateChanged()
        {
            if (IsOpen)
                Refresh();
        }

        private void ClearGenerated()
        {
            for (int i = _nodes.Count - 1; i >= 0; i--)
                DestroyGenerated(_nodes[i]);
            for (int i = _edges.Count - 1; i >= 0; i--)
                DestroyGenerated(_edges[i]);
            for (int i = _sections.Count - 1; i >= 0; i--)
                DestroyGenerated(_sections[i]);
            _nodes.Clear();
            _edges.Clear();
            _sections.Clear();
            _selectedMarks.Clear();
            _nodeData.Clear();
            _edgeData.Clear();
            _relationPaths.Clear();
            _kinCount = 0;
            _unconnectedCount = 0;
            _householdCount = 0;
            _segmentCount = 0;
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
}
