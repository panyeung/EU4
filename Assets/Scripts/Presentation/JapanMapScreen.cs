using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Runtime uGUI screen over the authoritative JapanMapExplorer and GameSession.
    /// It owns no travel state: preview and confirmation go through GameSession, so
    /// the map, the M02 buttons and save data all read the same world state.
    /// <para>
    /// 地图区域是一个裁切视口，直接子级 <c>MapContent</c> 承载路线层与节点层。
    /// 缩放与平移只改这一层 <see cref="JapanMapViewport"/> 变换；目录、筛选、
    /// 详情和按钮保持固定尺寸。旅行、日期、存档和稳定 ID 的语义不变。
    /// </para>
    /// </summary>
    public sealed class JapanMapScreen : MonoBehaviour
    {
        private const string CatalogTitle = "地理目录（五畿七道 + 独立旅行区域）";

        private GameSession _session;
        private RectTransform _root;
        private JapanMapExplorer _explorer;
        private TravelPreview? _preview;
        private string _destinationHubId = string.Empty;
        private string _feedback = string.Empty;
        private bool _built;

        private Graphic _header;
        private Graphic _catalogStatus;
        private RectTransform _catalogContent;
        private ScrollRect _catalogScroll;
        private RectTransform _mapArea;
        private RectTransform _mapContent;
        private readonly JapanMapViewport _mapViewport = new JapanMapViewport();
        private JapanMapViewportInput _mapInput;
        private Vector2 _lastMapSize;
        private RectTransform _lineLayer;
        private RectTransform _nodeLayer;
        private Graphic _detail;
        private Graphic _feedbackText;
        private Button _confirmButton;
        private Button _cancelButton;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public string SelectedHubId => _explorer != null ? _explorer.SelectedHubId : string.Empty;
        public JapanMapExplorer Explorer => _explorer;

        public void Initialize(GameSession session, RectTransform root)
        {
            _session = session;
            _root = root;
            if (_built)
            {
                EnsureExplorer();
                if (_root != null && _root.gameObject.activeInHierarchy)
                    RebuildAll();
                return;
            }

            // The bootstrap creates the root inactive. Building labels under an
            // inactive root postpones TextMeshProUGUI.Awake until Open(); the
            // RectMask2D can register those labels before their CanvasRenderer is
            // cached. Build the static UI after activation instead (see Open).
            if (_root != null && _root.gameObject.activeInHierarchy)
            {
                EnsureBuilt();
                EnsureExplorer();
                RebuildAll();
            }
        }

        public void BindSession(GameSession session)
        {
            _session = session;
            _explorer = null;
            _preview = null;
            _destinationHubId = string.Empty;
            _feedback = string.Empty;
            EnsureExplorer();
            if (_built && _root != null && _root.gameObject.activeInHierarchy)
                RebuildAll();
        }

        public void Open()
        {
            if (_root == null)
                return;
            _root.gameObject.SetActive(true);
            EnsureBuilt();
            JapanMapSceneBootstrap.PlaceBelowProtectedOverlays(_root, _root.parent);
            EnsureExplorer();
            RebuildAll();
            Canvas.ForceUpdateCanvases();
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;
            BuildStaticUi();
            _built = true;
        }

        public void Close()
        {
            if (_root != null)
                _root.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        /// 视口尺寸变化后重新适配全境。运行时由 <see cref="LateUpdate"/> 自动调用；
        /// 尺寸已知但帧回调尚未运行时（例如 EditMode 回归）可显式调用。
        /// </summary>
        public void RefreshMapViewport()
        {
            if (!_built)
                return;
            RebuildMap();
        }

        public bool SelectHub(string hubId)
        {
            if (!EnsureExplorer() || !_explorer.SelectHub(hubId))
                return false;
            TryPreviewSelectedHub();
            RebuildAll();
            return true;
        }

        public bool SelectProvince(string provinceId)
        {
            if (!EnsureExplorer() || !_explorer.SelectProvince(provinceId))
                return false;
            TryPreviewSelectedHub();
            RebuildAll();
            return true;
        }

        public void ShowAllRoutes() => SetFilter(null);
        public void ShowLandRoutes() => SetFilter(JapanRouteKind.Land);
        public void ShowSeaRoutes() => SetFilter(JapanRouteKind.Sea);

        public void ConfirmSelectedTravel()
        {
            if (_session == null || !_preview.HasValue)
                return;
            TravelPreview preview = _preview.Value;
            if (_session.ConfirmTravel(preview))
            {
                _feedback = "已抵达 " + preview.DestinationName + "。";
                _preview = null;
                _destinationHubId = string.Empty;
                _explorer = null;
                EnsureExplorer();
                _explorer.SelectHub(_explorer.CurrentHubId);
            }
            else
            {
                _feedback = string.IsNullOrEmpty(_session.LastTravelBlockReason)
                    ? "无法确认旅行：当前位置或日期已变化。"
                    : _session.LastTravelBlockReason;
            }

            RebuildAll();
        }

        private void LateUpdate()
        {
            // The map area changes size when the window is resized. Rebuild on the
            // first usable layout and whenever either dimension moves, so nodes and
            // route lines share the new adaptive grid instead of overlapping.
            if (!IsOpen || _mapArea == null || _nodeLayer == null)
                return;
            Rect size = _mapArea.rect;
            if (size.width <= 1f || size.height <= 1f)
                return;
            if (_nodeLayer.childCount == 0 ||
                Mathf.Abs(size.width - _lastMapSize.x) > 0.5f ||
                Mathf.Abs(size.height - _lastMapSize.y) > 0.5f)
                RebuildMap();
        }

        private void SetFilter(JapanRouteKind? filter)
        {
            if (!EnsureExplorer() || !_explorer.SetRouteFilter(filter))
                return;
            RebuildAll();
        }

        private bool EnsureExplorer()
        {
            if (_session == null)
                return false;
            LocationId current = _session.CurrentLocation;
            if (_explorer == null || _explorer.CurrentLocation.Value != current.Value)
            {
                _explorer = new JapanMapExplorer(current);
                _preview = null;
                _destinationHubId = string.Empty;
            }
            return true;
        }

        private void TryPreviewSelectedHub()
        {
            _preview = null;
            _destinationHubId = string.Empty;
            _feedback = string.Empty;
            if (_session == null || _explorer == null ||
                string.IsNullOrEmpty(_explorer.SelectedHubId))
                return;

            string selected = _explorer.SelectedHubId;
            if (string.Equals(selected, _explorer.CurrentHubId, StringComparison.Ordinal))
            {
                _feedback = "这是当前所在地。请选择有直达路线的其他枢纽。";
                return;
            }

            LocationId destination = new LocationId(selected);
            TravelPreview? preview = _session.PreviewTravel(destination);
            if (preview.HasValue)
            {
                _preview = preview;
                _destinationHubId = selected;
                return;
            }

            _feedback = string.IsNullOrEmpty(_session.LastTravelBlockReason)
                ? "当前位置与所选枢纽之间没有直达路线。"
                : _session.LastTravelBlockReason;
        }

        private void RebuildAll()
        {
            if (!_built || _root == null)
                return;
            if (!EnsureExplorer())
                return;

            JapanMapFonts.SetText(_header,
                JapanMapText.BuildHeader(_explorer) + "｜" + JapanMapFonts.Status);
            RebuildCatalog();
            RefreshDetail();
            RebuildMap();
        }

        private void RebuildCatalog()
        {
            DestroyChildren(_catalogContent);
            if (_explorer == null)
                return;

            IReadOnlyList<JapanMapCatalogRow> rows = _explorer.BuildCatalogRows();
            for (int i = 0; i < rows.Count; i++)
            {
                JapanMapCatalogRow row = rows[i];
                if (row.Kind == JapanMapRowKind.Domain)
                {
                    string domainId = row.Id;
                    string label = (row.IsExpanded ? "[-] " : "[+] ") + row.Name +
                                   "  " + row.ProvinceCount + "国/" + row.HubCount + "枢纽";
                    CreateCatalogRow(label, 0, () =>
                    {
                        if (_explorer != null && _explorer.ToggleDomain(domainId))
                            RebuildAll();
                    });
                    continue;
                }

                string provinceId = row.Id;
                string provinceLabel = "    " + row.Name + "  (" + row.HubCount + ")";
                if (row.HubCount > 0)
                    provinceLabel += "  " + string.Join("、", row.HubNames);
                else
                    provinceLabel += "  仅目录";
                CreateCatalogRow(provinceLabel, 1, () =>
                {
                    if (SelectProvince(provinceId))
                        return;
                });
            }

            IReadOnlyList<JapanMapCatalogRow> regions = _explorer.BuildTravelRegionRows();
            if (regions.Count > 0)
                CreateCatalogRow("独立旅行区域（不并入68旧国）", 0, () => { });
            for (int i = 0; i < regions.Count; i++)
            {
                JapanMapCatalogRow region = regions[i];
                string domainId = region.Id;
                string label = "◆ " + region.Name + "  (" + region.HubCount + ")";
                if (region.HubNames.Count > 0)
                    label += "  " + string.Join("、", region.HubNames);
                CreateCatalogRow(label, 1, () =>
                {
                    IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.GetHubsForDomain(domainId);
                    if (hubs.Count > 0)
                        SelectHub(hubs[0].Id);
                });
            }

            JapanMapFonts.SetText(_catalogStatus,
                "68旧国｜" + JapanTravelGraph.Hubs.Count + "枢纽｜" +
                JapanTravelGraph.Routes.Count + "路线");
        }

        private void RefreshDetail()
        {
            var detail = new StringBuilder(JapanMapText.BuildDetail(_explorer));
            if (_preview.HasValue)
            {
                TravelPreview preview = _preview.Value;
                detail.Append("\n\n已选行程：").Append(preview.DestinationName).Append("｜")
                    .Append(preview.TravelDays).Append("天｜到达 ")
                    .Append(preview.EstimatedArrivalDate)
                    .Append("\n该路线在相邻路线中标记为“本次可出发”。");
            }
            else
            {
                detail.Append("\n\n在地图上选择地点或从目录选择枢纽；只有图中直达路线可以旅行。");
            }

            AppendAdjacentRoutes(detail);
            JapanMapFonts.SetText(_detail, detail.ToString());
            JapanMapFonts.SetText(_feedbackText, !string.IsNullOrEmpty(_feedback)
                ? _feedback
                : (string.IsNullOrEmpty(JapanMapFonts.Warning) ? " " : JapanMapFonts.Warning));
            _confirmButton.interactable = _preview.HasValue;
            _cancelButton.interactable = _preview.HasValue;
        }

        /// <summary>
        /// The route cards are the source of truth for the selected hub. They remain
        /// readable even when the destination cannot be reached from the current hub,
        /// so the player can compare evidence, control and inferred travel time.
        /// </summary>
        private void AppendAdjacentRoutes(StringBuilder detail)
        {
            if (_explorer == null || string.IsNullOrEmpty(_explorer.SelectedHubId))
                return;

            string selected = _explorer.SelectedHubId;
            IReadOnlyList<JapanMapRoute> routes = JapanTravelGraph.GetRoutesFrom(selected);
            detail.Append("\n\n相邻路线（").Append(routes.Count).Append("）：");
            if (routes.Count == 0)
            {
                detail.Append("\n该枢纽没有配置任何路线。");
                return;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                JapanMapRoute route = routes[i];
                string otherId = route.OtherHubId(selected);
                string otherName = JapanTravelGraph.TryGetHub(otherId, out JapanMapHub other)
                    ? other.Name
                    : otherId;
                bool previewRoute = _preview.HasValue &&
                                    string.Equals(otherId, _explorer.CurrentHubId, StringComparison.Ordinal) &&
                                    string.Equals(selected, _destinationHubId, StringComparison.Ordinal);

                detail.Append("\n\n· ").Append(otherName).Append("｜")
                    .Append(JapanMapText.KindLabel(route.Kind)).Append("｜")
                    .Append(route.Days).Append("天｜")
                    .Append(JapanMapText.EvidenceLabel(route.Evidence));
                if (previewRoute)
                    detail.Append("｜本次可出发");
                detail.Append("\n  时期：").Append(route.PeriodNote)
                    .Append("\n  控制：").Append(route.ControlNote)
                    .Append("\n  证据：").Append(route.EvidenceNote)
                    .Append("\n  来源：").Append(route.SourceNote);
                if (route.Evidence == JapanRouteEvidence.DesignInference)
                    detail.Append("\n  标注：游戏设计推演，不是1560史实。");
            }
        }

        private void RebuildMap()
        {
            if (_mapArea == null || _mapContent == null || _nodeLayer == null ||
                _lineLayer == null)
                return;

            Rect size = _mapArea.rect;
            if (size.width <= 1f || size.height <= 1f)
                return;

            var extent = new Vector2(size.width, size.height);
            EnsureViewport(extent);
            DestroyChildren(_nodeLayer);
            DestroyChildren(_lineLayer);

            IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.Hubs;
            JapanMapLayoutResult layout = JapanMapLayout.ComputeLayout(hubs, extent);
            Vector2[] nodePositions = layout.Positions;

            IReadOnlyList<JapanMapRoute> routes = JapanTravelGraph.Routes;
            for (int i = 0; i < routes.Count; i++)
            {
                JapanMapRoute route = routes[i];
                if (_explorer != null && _explorer.RouteFilter.HasValue &&
                    route.Kind != _explorer.RouteFilter.Value)
                    continue;
                if (!JapanTravelGraph.TryGetHub(route.FromHubId, out JapanMapHub from) ||
                    !JapanTravelGraph.TryGetHub(route.ToHubId, out JapanMapHub to))
                    continue;
                CreateRouteLine(from, to, route,
                    NodePosition(hubs, nodePositions, route.FromHubId),
                    NodePosition(hubs, nodePositions, route.ToHubId));
            }

            for (int i = 0; i < hubs.Count; i++)
                CreateHubNode(hubs[i], nodePositions[i], layout.NodeSize);
        }

        /// <summary>
        /// 视口尺寸决定全境几何。首次绑定或尺寸变化时把 mapContent 的矩形设为
        /// 新的全境尺寸并回到 1x；尺寸不变时只按当前倍率刷新边界，因此选择枢纽
        /// 重建节点不会重置玩家的缩放和平移。
        /// </summary>
        private void EnsureViewport(Vector2 extent)
        {
            _mapContent.sizeDelta = extent;
            bool resized = !_mapViewport.IsBound ||
                           Mathf.Abs(extent.x - _lastMapSize.x) > 0.5f ||
                           Mathf.Abs(extent.y - _lastMapSize.y) > 0.5f;
            if (resized)
            {
                _lastMapSize = extent;
                _mapViewport.Bind(_mapArea, _mapContent);
                return;
            }

            _mapViewport.Refresh();
        }

        private static Vector2 NodePosition(IReadOnlyList<JapanMapHub> hubs,
            Vector2[] positions, string hubId)
        {
            for (int i = 0; i < hubs.Count; i++)
                if (string.Equals(hubs[i].Id, hubId, StringComparison.Ordinal))
                    return positions[i];
            return Vector2.zero;
        }

        private void CreateRouteLine(JapanMapHub from, JapanMapHub to, JapanMapRoute route,
            Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            var lineObject = new GameObject("Route_" + route.RouteId,
                typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)lineObject.transform;
            rect.SetParent(_lineLayer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (a + b) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, route.Kind == JapanRouteKind.Sea ? 3f : 2f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = lineObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = route.Kind == JapanRouteKind.Sea
                ? new Color(0.36f, 0.66f, 0.92f, 0.85f)
                : new Color(0.78f, 0.68f, 0.48f, 0.85f);
        }

        private void CreateHubNode(JapanMapHub hub, Vector2 position, Vector2 nodeSize)
        {
            bool current = _explorer != null &&
                           string.Equals(hub.Id, _explorer.CurrentHubId, StringComparison.Ordinal);
            bool selected = _explorer != null &&
                            string.Equals(hub.Id, _explorer.SelectedHubId, StringComparison.Ordinal);
            var nodeObject = new GameObject("Hub_" + hub.Id,
                typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)nodeObject.transform;
            rect.SetParent(_nodeLayer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = nodeSize;

            var image = nodeObject.GetComponent<Image>();
            image.color = current
                ? new Color(0.28f, 0.66f, 0.38f, 0.96f)
                : selected
                    ? new Color(0.88f, 0.72f, 0.24f, 0.96f)
                    : new Color(0.24f, 0.29f, 0.36f, 0.95f);
            var button = nodeObject.GetComponent<Button>();
            button.targetGraphic = image;
            string hubId = hub.Id;
            button.onClick.AddListener(() => SelectHubFromMap(hubId));

            Graphic label = JapanMapFonts.CreateText(rect, "Label", 11,
                TextAlignmentOptions.Center);
            var labelRect = (RectTransform)label.transform;
            labelRect.offsetMin = new Vector2(3f, 1f);
            labelRect.offsetMax = new Vector2(-3f, -1f);
            JapanMapFonts.SetColor(label, Color.white);
            JapanMapFonts.SetWrap(label, false);
            JapanMapFonts.SetEllipsis(label, true);
            string labelText = hub.Name;
            if (hub.DomainId == "ezo" || hub.DomainId == "ryukyu")
                labelText += "△";
            JapanMapFonts.SetText(label, labelText);
        }

        private void BuildStaticUi()
        {
            var background = _root.gameObject.GetComponent<Image>();
            if (background == null)
                background = _root.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.065f, 0.085f, 0.985f);
            background.raycastTarget = true;

            var headerRect = NewRect("Header", _root, new Vector2(0.31f, 0.88f),
                new Vector2(0.90f, 0.975f));
            _header = AddText(headerRect, "HeaderText", 15, TextAlignmentOptions.MidlineLeft);
            _header.color = new Color(0.93f, 0.88f, 0.72f, 1f);

            CreateButton(_root, "CloseButton", "关闭", new Vector2(0.91f, 0.92f),
                new Vector2(0.985f, 0.975f), Close, 14);

            var catalogPanel = CreatePanel("CatalogPanel", _root, new Vector2(0.015f, 0.02f),
                new Vector2(0.295f, 0.975f), new Color(0.09f, 0.11f, 0.14f, 0.98f));
            var catalogTitleRect = NewRect("CatalogTitle", catalogPanel,
                new Vector2(0.05f, 0.93f), new Vector2(0.95f, 0.99f));
            var catalogTitle = AddText(catalogTitleRect, "CatalogTitleText", 13, TextAlignmentOptions.MidlineLeft);
            JapanMapFonts.SetText(catalogTitle, CatalogTitle);
            catalogTitle.color = new Color(0.9f, 0.82f, 0.6f, 1f);

            _catalogStatus = AddText(NewRect("CatalogStatus", catalogPanel,
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.93f)),
                "CatalogStatusText", 11, TextAlignmentOptions.MidlineLeft);
            _catalogStatus.color = new Color(0.62f, 0.68f, 0.75f, 1f);

            var viewport = CreatePanel("CatalogViewport", catalogPanel,
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.875f),
                new Color(0.06f, 0.075f, 0.095f, 0.9f));
            var mask = viewport.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(0f, 0f, 0f, 0f);
            _catalogScroll = viewport.gameObject.AddComponent<ScrollRect>();
            _catalogScroll.horizontal = false;
            _catalogScroll.vertical = true;
            _catalogScroll.movementType = ScrollRect.MovementType.Clamped;
            _catalogScroll.scrollSensitivity = 28f;

            var contentObject = new GameObject("CatalogContent", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _catalogContent = (RectTransform)contentObject.transform;
            _catalogContent.SetParent(viewport, false);
            _catalogContent.anchorMin = new Vector2(0f, 1f);
            _catalogContent.anchorMax = new Vector2(1f, 1f);
            _catalogContent.pivot = new Vector2(0.5f, 1f);
            _catalogContent.sizeDelta = Vector2.zero;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _catalogScroll.viewport = viewport;
            _catalogScroll.content = _catalogContent;

            CreateButton(_root, "AllRoutesButton", "全部路线", new Vector2(0.31f, 0.82f),
                new Vector2(0.40f, 0.87f), ShowAllRoutes, 12);
            CreateButton(_root, "LandRoutesButton", "仅陆路", new Vector2(0.405f, 0.82f),
                new Vector2(0.495f, 0.87f), ShowLandRoutes, 12);
            CreateButton(_root, "SeaRoutesButton", "仅海路", new Vector2(0.50f, 0.82f),
                new Vector2(0.59f, 0.87f), ShowSeaRoutes, 12);
            CreateButton(_root, "CurrentButton", "定位当前地点", new Vector2(0.60f, 0.82f),
                new Vector2(0.72f, 0.87f), SelectCurrent, 12);

            _mapArea = CreatePanel("JapanMapArea", _root, new Vector2(0.31f, 0.36f),
                new Vector2(0.985f, 0.81f), new Color(0.075f, 0.095f, 0.12f, 1f));
            // 地图区域是唯一裁切视口：缩放和平移只改 MapContent 的变换，
            // 路线层与节点层都挂在它下面，共享同一套变换和裁切。
            _mapArea.gameObject.AddComponent<RectMask2D>();
            _mapInput = _mapArea.gameObject.AddComponent<JapanMapViewportInput>();
            _mapInput.Bind(_mapViewport);

            _mapContent = NewRect("MapContent", _mapArea,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _mapContent.sizeDelta = Vector2.zero;

            _lineLayer = NewRect("RouteLines", _mapContent, Vector2.zero, Vector2.one);
            _nodeLayer = NewRect("HubNodes", _mapContent, Vector2.zero, Vector2.one);
            _lineLayer.SetAsFirstSibling();
            _nodeLayer.SetAsLastSibling();

            var detailPanel = CreatePanel("DetailPanel", _root, new Vector2(0.31f, 0.02f),
                new Vector2(0.985f, 0.345f), new Color(0.09f, 0.11f, 0.14f, 0.98f));
            var detailViewport = CreatePanel("DetailViewport", detailPanel,
                new Vector2(0.02f, 0.27f), new Vector2(0.98f, 0.97f),
                new Color(0f, 0f, 0f, 0.001f));
            var viewportImage = detailViewport.GetComponent<Image>();
            viewportImage.raycastTarget = true;
            detailViewport.gameObject.AddComponent<RectMask2D>();
            var detailScroll = detailViewport.gameObject.AddComponent<ScrollRect>();
            detailScroll.horizontal = false;
            detailScroll.vertical = true;
            detailScroll.movementType = ScrollRect.MovementType.Clamped;
            detailScroll.scrollSensitivity = 30f;

            var detailContentObject = new GameObject("DetailContent", typeof(RectTransform),
                typeof(ContentSizeFitter));
            var detailContent = (RectTransform)detailContentObject.transform;
            detailContent.SetParent(detailViewport, false);
            detailContent.anchorMin = new Vector2(0f, 1f);
            detailContent.anchorMax = new Vector2(1f, 1f);
            detailContent.pivot = new Vector2(0.5f, 1f);
            detailContent.anchoredPosition = Vector2.zero;
            detailContent.sizeDelta = Vector2.zero;
            var detailFitter = detailContentObject.GetComponent<ContentSizeFitter>();
            detailFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            detailFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _detail = JapanMapFonts.ConfigureText(detailContentObject, 11,
                TextAlignmentOptions.TopLeft);
            JapanMapFonts.SetColor(_detail, new Color(0.82f, 0.85f, 0.9f, 1f));
            JapanMapFonts.SetWrap(_detail, true);
            JapanMapFonts.SetEllipsis(_detail, false);
            detailScroll.viewport = detailViewport;
            detailScroll.content = detailContent;

            _feedbackText = AddText(NewRect("Feedback", detailPanel, new Vector2(0.50f, 0.04f),
                new Vector2(0.975f, 0.25f)), "FeedbackText", 11, TextAlignmentOptions.MidlineLeft);
            _feedbackText.color = new Color(0.95f, 0.75f, 0.45f, 1f);

            _confirmButton = CreateButton(detailPanel, "ConfirmTravelButton", "确认出发",
                new Vector2(0.02f, 0.04f), new Vector2(0.23f, 0.24f), ConfirmSelectedTravel, 12);
            _cancelButton = CreateButton(detailPanel, "CancelTravelButton", "取消",
                new Vector2(0.25f, 0.04f), new Vector2(0.38f, 0.24f), CancelPreview, 12);
        }

        private void SelectCurrent()
        {
            if (!EnsureExplorer() || string.IsNullOrEmpty(_explorer.CurrentHubId))
                return;
            SelectHub(_explorer.CurrentHubId);
        }

        /// <summary>
        /// 地图节点的短按沿用原来的选择与旅行预览。拖动已经超过 uGUI 阈值时，
        /// 这次抬起不得当成选点，避免拖完地图误选节点或弹出行程。
        /// </summary>
        private void SelectHubFromMap(string hubId)
        {
            if (_mapInput != null && _mapInput.IsDragging)
                return;
            SelectHub(hubId);
        }

        private void CancelPreview()
        {
            _preview = null;
            _destinationHubId = string.Empty;
            _feedback = "已取消未出发的行程。";
            RebuildAll();
        }

        private void CreateCatalogRow(string label, int indent, UnityAction action)
        {
            var rowObject = new GameObject("CatalogRow", typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            var rect = (RectTransform)rowObject.transform;
            rect.SetParent(_catalogContent, false);
            var image = rowObject.GetComponent<Image>();
            image.color = indent == 0
                ? new Color(0.17f, 0.21f, 0.27f, 1f)
                : new Color(0.11f, 0.13f, 0.17f, 1f);
            var button = rowObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var element = rowObject.GetComponent<LayoutElement>();
            element.minHeight = 24f;
            element.preferredHeight = 24f;
            element.flexibleWidth = 1f;

            Graphic text = JapanMapFonts.CreateText(rect, "Label", 11,
                TextAlignmentOptions.MidlineLeft);
            var textRect = (RectTransform)text.transform;
            float left = indent == 0 ? 5f : 15f;
            textRect.offsetMin = new Vector2(left, 0f);
            textRect.offsetMax = new Vector2(-4f, 0f);
            JapanMapFonts.SetColor(text, indent == 0
                ? new Color(0.95f, 0.86f, 0.66f, 1f)
                : new Color(0.78f, 0.82f, 0.88f, 1f));
            JapanMapFonts.SetWrap(text, false);
            JapanMapFonts.SetEllipsis(text, true);
            JapanMapFonts.SetText(text, label);
        }

        private static RectTransform NewRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = NewRect(name, parent, anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, UnityAction onClick, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.21f, 0.26f, 0.33f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Graphic text = JapanMapFonts.CreateText(rect, "Label", fontSize,
                TextAlignmentOptions.Center);
            var textRect = (RectTransform)text.transform;
            textRect.offsetMin = new Vector2(3f, 1f);
            textRect.offsetMax = new Vector2(-3f, -1f);
            JapanMapFonts.SetColor(text, Color.white);
            JapanMapFonts.SetWrap(text, false);
            JapanMapFonts.SetEllipsis(text, true);
            JapanMapFonts.SetText(text, label);
            return button;
        }

        private static Graphic AddText(RectTransform rect, string name, int fontSize,
            TextAlignmentOptions alignment)
        {
            Graphic text = JapanMapFonts.CreateText(rect, name, fontSize, alignment);
            JapanMapFonts.SetColor(text, Color.white);
            JapanMapFonts.SetWrap(text, true);
            JapanMapFonts.SetEllipsis(text, true);
            return text;
        }

        private static void DestroyChildren(Transform parent)
        {
            if (parent == null)
                return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                // EditMode 回归测试也在非 Play Mode 下重建目录。
                if (UnityEngine.Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
    }
}
