using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// M02 keeps its three Owari location buttons untouched. This optional panel renders
    /// the national catalog and sparse travel graph through the shared text model; it
    /// creates no runtime nodes and owns no world state.
    /// </summary>
    public sealed class JapanMapPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;

        // The existing M02 scene uses uGUI Text; the planned UI target is TextMeshPro.
        // Both bindings render the same read-only text model, so the panel works before
        // the TMP font assets are imported and can move to TMP without changing data.
        [SerializeField] private Text headerLabel;
        [SerializeField] private Text catalogLabel;
        [SerializeField] private Text detailLabel;
        [SerializeField] private Text routeLabel;

        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text catalogText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text routeText;

        private JapanMapExplorer _explorer;

        public JapanMapExplorer Explorer => _explorer;

        public void BindSession(GameSession session)
        {
            gameSession = session;
            _explorer = null;
            Rebuild();
        }

        public void BindTexts(Text header, Text catalog, Text detail, Text routes)
        {
            headerLabel = header;
            catalogLabel = catalog;
            detailLabel = detail;
            routeLabel = routes;
            Rebuild();
        }

        public void BindTexts(TMP_Text header, TMP_Text catalog, TMP_Text detail, TMP_Text routes)
        {
            headerText = header;
            catalogText = catalog;
            detailText = detail;
            routeText = routes;
            Rebuild();
        }

        private void OnEnable()
        {
            Rebuild();
        }

        public void Refresh()
        {
            Rebuild();
        }

        public bool TryToggleDomain(string domainId)
        {
            if (!EnsureExplorer() || !_explorer.ToggleDomain(domainId))
                return false;
            Render();
            return true;
        }

        public void ToggleDomain(string domainId)
        {
            TryToggleDomain(domainId);
        }

        public bool TrySelectHub(string hubId)
        {
            if (!EnsureExplorer() || !_explorer.SelectHub(hubId))
                return false;
            Render();
            return true;
        }

        public void SelectHub(string hubId)
        {
            TrySelectHub(hubId);
        }

        public bool TrySelectProvince(string provinceId)
        {
            if (!EnsureExplorer() || !_explorer.SelectProvince(provinceId))
                return false;
            Render();
            return true;
        }

        public void SelectProvince(string provinceId)
        {
            TrySelectProvince(provinceId);
        }

        public void SelectCurrentLocation()
        {
            if (EnsureExplorer() && !string.IsNullOrEmpty(_explorer.CurrentHubId))
                TrySelectHub(_explorer.CurrentHubId);
        }

        public void ClearSelection()
        {
            if (!EnsureExplorer())
                return;
            _explorer.ClearSelection();
            Render();
        }

        public void ShowAllRoutes()
        {
            SetRouteFilter(null);
        }

        public void ShowLandRoutes()
        {
            SetRouteFilter(JapanRouteKind.Land);
        }

        public void ShowSeaRoutes()
        {
            SetRouteFilter(JapanRouteKind.Sea);
        }

        private void SetRouteFilter(JapanRouteKind? filter)
        {
            if (!EnsureExplorer() || !_explorer.SetRouteFilter(filter))
                return;
            Render();
        }

        private bool EnsureExplorer()
        {
            if (gameSession == null)
                return false;
            if (_explorer == null ||
                _explorer.CurrentLocation.Value != gameSession.CurrentLocation.Value)
                _explorer = new JapanMapExplorer(gameSession.CurrentLocation);
            return true;
        }

        private void Rebuild()
        {
            if (!EnsureExplorer())
            {
                SetText(headerLabel, "日本全国地图：未绑定游戏会话。");
                SetText(catalogLabel, string.Empty);
                SetText(detailLabel, string.Empty);
                SetText(routeLabel, string.Empty);
                SetText(headerText, "日本全国地图：未绑定游戏会话。");
                SetText(catalogText, string.Empty);
                SetText(detailText, string.Empty);
                SetText(routeText, string.Empty);
                return;
            }

            Render();
        }

        private void Render()
        {
            string header = JapanMapText.BuildHeader(_explorer);
            string catalog = JapanMapText.BuildCatalog(_explorer);
            string detail = JapanMapText.BuildDetail(_explorer);
            string routes = JapanMapText.BuildRoutes(_explorer);
            SetText(headerLabel, header);
            SetText(catalogLabel, catalog);
            SetText(detailLabel, detail);
            SetText(routeLabel, routes);
            SetText(headerText, header);
            SetText(catalogText, catalog);
            SetText(detailText, detail);
            SetText(routeText, routes);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }
    }
}
