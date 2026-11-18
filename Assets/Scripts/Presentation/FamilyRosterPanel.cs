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

        /// <summary>Fixed root for the runtime-built selected-person column.</summary>
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
        // The textual "已选" stamp owns a band under the card label so it never
        // overprints the name/role lines, whose first line is the longest.
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
            // The generated clan atlas is the primary portrait source; the older
            // owner-supplied single crops stay as a fallback so the roster renders
            // even before the atlas import is refreshed.
            Sprite sprite = SengokuUiArt.Portrait(key);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>("ReferenceArt/portrait_" + key);
                sprite = texture != null
                    ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f)
                    : null;
            }
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
        /// <summary>True while the narrow-width detail sheet covers the graph.</summary>
        public bool DetailSheetVisible =>
            !_hasDetailColumn && _detailSheetVisible && _detailRoot != null &&
            _detailRoot.activeSelf;

        public string DetailHeadingLine =>
            _detailHeading != null ? _detailHeading.text : string.Empty;

        /// <summary>Viewport width the graph keeps after the detail column is reserved.</summary>
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
        /// Selects one displayed person for the detail column. Unknown or empty IDs are
        /// ignored so a click on a stale card can never point the column at a person
        /// outside the current graph. Selection changes no session state. On a narrow
        /// panel the detail sheet opens so the selected facts are never hidden.
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

        /// <summary>Closes the narrow-width detail sheet and returns to the graph.</summary>
        public void CloseDetailSheet()
        {
            _detailSheetVisible = false;
            if (_detailRoot != null && !_hasDetailColumn)
                _detailRoot.SetActive(false);
        }

        /// <summary>
        /// Applies the page layout from the widths the page styler actually gave this
        /// panel. A wide card shows the graph and the selected-person column side by
        /// side; a narrow card keeps the full graph width and presents the same facts
        /// in a stacked detail sheet that opens from a card selection. Nothing here
        /// restores serialized geometry, and applying it twice is harmless.
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
            // The body was wrapped for the previous width; re-measure once here so a
            // narrow-to-wide switch does not keep a too-short body.
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
            // Measure only once the body is visible: preferred height is read from the
            // Text's own layout, and keeping the order visible-then-measure avoids a
            // measurement taken on a deactivated panel.
            MeasureDetailBody(true);
            if (_detailScroll != null)
                _detailScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// Wraps and measures the detail body for the width the live layout currently gives
        /// it. A width change (narrow sheet to side column, or a resized page) re-measures,
        /// because a height measured at the old width clips or leaves a gap. Layout events
        /// call it with <paramref name="force"/> false, so unchanged widths cost one
        /// comparison and no per-frame work.
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

            // Set the width first: preferred height wraps against the current rect width,
            // so measuring before the width is applied reports a wrong height.
            textRect.sizeDelta = new Vector2(width, textRect.sizeDelta.y);
            textRect.anchoredPosition = Vector2.zero;
            float height = Mathf.Max(1f, _detailText.preferredHeight);
            textRect.sizeDelta = new Vector2(width, height);
            if (_detailContent != null)
                _detailContent.sizeDelta = new Vector2(width, height);
            _measuredDetailWidth = width;
        }

        /// <summary>
        /// The close control belongs to the stacked sheet only; the side-by-side column is
        /// always visible, so it keeps its full width for facts.
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
        /// Player-facing detail for one selected person. Every line is read from the
        /// graph snapshot, the live succession state or the recorded kinship facts.
        /// Nothing is inferred: an absent life record, an unknown age or a person with
        /// no recorded kinship says so instead of claiming a relationship.
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
            // The identity line matches the card exactly, including the head/heir mark, so
            // the detail column and the graph never disagree about a person.
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
        /// Gives the selected card keyboard focus once per open so arrow keys and Submit
        /// reach the graph without a mouse. Nothing happens when no event system exists.
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

            // The viewport itself is raycastable so wheel and drag scrolling work, and a
            // visible vertical scrollbar keeps long fact lists reachable and discoverable.
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
        /// Runtime detail objects must never be serialized into a scene: an editor-side
        /// refresh, generator pass or test teardown would otherwise leave generated
        /// clones behind in the saved hierarchy.
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
                // The pointer surface above the card owns mouse hits; the card graphic
                // still drives the keyboard button's tint so focus stays visible.
                background.raycastTarget = false;
                background.color = cardColor;
            }

            // Keyboard path: the card is a normal Selectable, so Tab/arrow focus and
            // Submit work without a pointer. Pointer clicks arrive through the hit
            // surface below, which rejects clicks that were actually a scroll drag.
            Button cardButton = card.GetComponent<Button>();
            if (cardButton == null)
                cardButton = card.AddComponent<Button>();
            cardButton.targetGraphic = background;
            cardButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            cardButton.transition = Selectable.Transition.ColorTint;
            // ColorTint multiplies the target graphic's own colour, so the normal state must
            // stay white: setting it to the card colour too would square the parchment and
            // darken every card. The tint only brightens on highlight/selection and sinks
            // slightly on press, which keeps state feedback without double tinting.
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
                // The bottom inset reserves the selected-stamp band; without it the
                // marker drawn below and the label share the same rect.
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

            // Textual selected marker: the current selection is readable without
            // relying on colour or hover, and it never edits the person card text.
            // It sits in the footer band the label rect reserves, so the stamp and
            // the name/role lines cannot overprint each other.
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

            // Pointer surface: a drag that starts on a card scrolls the graph and never
            // selects, and wheel scrolling still reaches the graph ScrollRect.
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
