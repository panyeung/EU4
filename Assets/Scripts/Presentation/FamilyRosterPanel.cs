using System.Collections.Generic;
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

        private static readonly Color ParentChildColor = new Color(0.88f, 0.73f, 0.34f, 1f);
        private static readonly Color AdoptionColor = new Color(0.68f, 0.47f, 0.86f, 1f);
        private static readonly Color SiblingColor = new Color(0.42f, 0.76f, 0.54f, 1f);
        private static readonly Color SpouseColor = new Color(0.43f, 0.65f, 0.92f, 1f);
        private static readonly Color KinCardColor = new Color(0.16f, 0.22f, 0.3f, 1f);
        private static readonly Color UnconnectedCardColor = new Color(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color HouseholdCardColor = new Color(0.3f, 0.24f, 0.18f, 1f);

        private readonly List<GameObject> _nodes = new List<GameObject>();
        private readonly List<FamilyTreePersonNode> _nodeData = new List<FamilyTreePersonNode>();
        private readonly List<GameObject> _edges = new List<GameObject>();
        private readonly List<FamilyTreeRelationEdge> _edgeData = new List<FamilyTreeRelationEdge>();
        private readonly List<IReadOnlyList<FamilyTreePoint>> _relationPaths =
            new List<IReadOnlyList<FamilyTreePoint>>();
        private readonly List<GameObject> _sections = new List<GameObject>();
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
        public ScrollRect ScrollRect => scrollRect;
        public Vector2 ContentSize =>
            content is RectTransform rect ? rect.sizeDelta : Vector2.zero;
        public FamilyTreeSize LastLayoutContentSize => _lastContentSize;
        public string HeirSummary => heirText != null ? heirText.text : string.Empty;
        public string LegendSummary => legendText != null ? legendText.text : string.Empty;
        public int StaticButtonBindingCount => _staticButtonBindings;

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
            if (panelRoot != null)
                panelRoot.SetActive(false);
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
            if (background != null)
            {
                background.raycastTarget = false;
                background.color = placement.Section == FamilyTreeLayoutSection.Household
                    ? HouseholdCardColor
                    : placement.Section == FamilyTreeLayoutSection.UnconnectedKin
                        ? UnconnectedCardColor
                        : KinCardColor;
            }

            Text text = card.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = FormatNode(person, placement);
                text.raycastTarget = false;
            }

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
