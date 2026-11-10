using System;
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
                textRect.offsetMin = new Vector2(leftInset, 8f);
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
