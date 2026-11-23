using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    public enum JapanMapRowKind
    {
        Domain = 0,
        Province = 1,
        TravelRegion = 2,
    }

    /// <summary>One line of the collapsible catalog. Display data only.</summary>
    public sealed class JapanMapCatalogRow
    {
        public JapanMapCatalogRow(JapanMapRowKind kind, string id, string name,
            bool isExpanded, int provinceCount, int hubCount, IReadOnlyList<string> hubNames)
        {
            Kind = kind;
            Id = id;
            Name = name;
            IsExpanded = isExpanded;
            ProvinceCount = provinceCount;
            HubCount = hubCount;
            HubNames = hubNames;
        }

        public JapanMapRowKind Kind { get; }
        public string Id { get; }
        public string Name { get; }
        public bool IsExpanded { get; }
        public int ProvinceCount { get; }
        public int HubCount { get; }
        public IReadOnlyList<string> HubNames { get; }
    }

    public sealed class JapanMapHubDetail
    {
        public JapanMapHubDetail(string hubId, string hubName, string provinceName,
            string domainName, bool isLegacyLocation, IReadOnlyList<string> neighborNames)
        {
            HubId = hubId;
            HubName = hubName;
            ProvinceName = provinceName;
            DomainName = domainName;
            IsLegacyLocation = isLegacyLocation;
            NeighborNames = neighborNames;
        }

        public string HubId { get; }
        public string HubName { get; }
        public string ProvinceName { get; }
        public string DomainName { get; }
        public bool IsLegacyLocation { get; }
        public IReadOnlyList<string> NeighborNames { get; }
    }

    /// <summary>One route card, expressed from the currently selected hub.</summary>
    public sealed class JapanMapRouteCard
    {
        public JapanMapRouteCard(string otherHubId, string otherHubName, JapanMapRoute route)
        {
            OtherHubId = otherHubId;
            OtherHubName = otherHubName;
            Route = route;
        }

        public string OtherHubId { get; }
        public string OtherHubName { get; }
        public JapanMapRoute Route { get; }
    }

    /// <summary>
    /// Read-only presentation model over the authoritative current location and the
    /// static national graph. It never mutates GameSession or save data.
    /// </summary>
    public sealed class JapanMapExplorer
    {
        private readonly HashSet<string> _expandedDomains =
            new HashSet<string>(StringComparer.Ordinal);
        private string _selectedHubId;
        private string _selectedProvinceId;
        private JapanRouteKind? _filter;

        public JapanMapExplorer(LocationId currentLocation)
        {
            CurrentLocation = currentLocation;
            if (JapanTravelGraph.TryGetHubForLocation(currentLocation, out JapanMapHub hub))
            {
                _selectedHubId = hub.Id;
                _expandedDomains.Add(hub.DomainId);
            }
            else
            {
                _expandedDomains.Add("tokaido");
            }
        }

        public LocationId CurrentLocation { get; }

        public string CurrentHubId
        {
            get
            {
                return JapanTravelGraph.TryGetHubForLocation(CurrentLocation, out JapanMapHub hub)
                    ? hub.Id
                    : string.Empty;
            }
        }

        public string SelectedHubId => _selectedHubId;
        public string SelectedProvinceId => _selectedProvinceId;
        public JapanRouteKind? RouteFilter => _filter;

        public bool IsDomainExpanded(string domainId)
        {
            return !string.IsNullOrEmpty(domainId) && _expandedDomains.Contains(domainId);
        }

        public bool ToggleDomain(string domainId)
        {
            if (!JapanMapCatalog.TryGetDomain(domainId, out JapanMapDomain domain) ||
                !domain.IsProvinceDomain)
                return false;

            if (!_expandedDomains.Remove(domainId))
                _expandedDomains.Add(domainId);
            return true;
        }

        public bool ExpandDomain(string domainId)
        {
            if (!JapanMapCatalog.TryGetDomain(domainId, out JapanMapDomain domain) ||
                !domain.IsProvinceDomain)
                return false;
            _expandedDomains.Add(domainId);
            return true;
        }

        public bool CollapseDomain(string domainId)
        {
            if (!JapanMapCatalog.TryGetDomain(domainId, out JapanMapDomain domain) ||
                !domain.IsProvinceDomain)
                return false;
            _expandedDomains.Remove(domainId);
            return true;
        }

        public bool SelectHub(string hubId)
        {
            if (!JapanTravelGraph.TryGetHub(hubId, out JapanMapHub hub))
                return false;

            _selectedHubId = hub.Id;
            _selectedProvinceId = string.Empty;
            _expandedDomains.Add(hub.DomainId);
            return true;
        }

        /// <summary>
        /// Expands a province and focuses its first hub. A province with no hub stays
        /// selectable as catalog data but is never presented as a visitable place.
        /// </summary>
        public bool SelectProvince(string provinceId)
        {
            if (!JapanMapCatalog.TryGetProvince(provinceId, out JapanProvince province))
                return false;

            _selectedProvinceId = province.Id;
            _expandedDomains.Add(province.DomainId);
            IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.GetHubsForProvince(province.Id);
            _selectedHubId = hubs.Count > 0 ? hubs[0].Id : string.Empty;
            return true;
        }

        public void ClearSelection()
        {
            _selectedHubId = string.Empty;
            _selectedProvinceId = string.Empty;
        }

        public bool SetRouteFilter(JapanRouteKind? kind)
        {
            if (kind.HasValue && !Enum.IsDefined(typeof(JapanRouteKind), kind.Value))
                return false;
            _filter = kind;
            return true;
        }

        /// <summary>
        /// Separate Ezo and Ryukyu rows. They are travel regions, not provinces, so
        /// they never inflate the eight province-domain groups or the sixty-eight
        /// province count.
        /// </summary>
        public IReadOnlyList<JapanMapCatalogRow> BuildTravelRegionRows()
        {
            var rows = new List<JapanMapCatalogRow>();
            IReadOnlyList<JapanMapDomain> domains = JapanMapCatalog.Domains;
            for (int i = 0; i < domains.Count; i++)
            {
                JapanMapDomain domain = domains[i];
                if (domain.IsProvinceDomain)
                    continue;
                IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.GetHubsForDomain(domain.Id);
                var names = new string[hubs.Count];
                for (int h = 0; h < hubs.Count; h++)
                    names[h] = hubs[h].Name;
                rows.Add(new JapanMapCatalogRow(JapanMapRowKind.TravelRegion, domain.Id,
                    domain.Name, true, 0, hubs.Count, names));
            }

            return rows;
        }

        public IReadOnlyList<JapanMapCatalogRow> BuildCatalogRows()
        {
            var rows = new List<JapanMapCatalogRow>();
            IReadOnlyList<JapanMapDomain> domains = JapanMapCatalog.Domains;
            for (int i = 0; i < domains.Count; i++)
            {
                JapanMapDomain domain = domains[i];
                if (!domain.IsProvinceDomain)
                    continue;

                IReadOnlyList<JapanProvince> provinces = JapanMapCatalog.GetProvinces(domain.Id);
                bool expanded = IsDomainExpanded(domain.Id);
                rows.Add(new JapanMapCatalogRow(JapanMapRowKind.Domain, domain.Id, domain.Name,
                    expanded, provinces.Count, CountHubs(domain.Id), Array.Empty<string>()));

                if (!expanded)
                    continue;

                for (int p = 0; p < provinces.Count; p++)
                {
                    JapanProvince province = provinces[p];
                    IReadOnlyList<JapanMapHub> hubs =
                        JapanTravelGraph.GetHubsForProvince(province.Id);
                    var names = new string[hubs.Count];
                    for (int h = 0; h < hubs.Count; h++)
                        names[h] = hubs[h].Name;
                    rows.Add(new JapanMapCatalogRow(JapanMapRowKind.Province, province.Id,
                        province.Name, true, 0, hubs.Count, names));
                }
            }

            return rows;
        }

        public JapanMapHubDetail BuildSelectedHubDetail()
        {
            if (string.IsNullOrEmpty(_selectedHubId) ||
                !JapanTravelGraph.TryGetHub(_selectedHubId, out JapanMapHub hub))
                return null;

            IReadOnlyList<JapanMapRoute> routes = JapanTravelGraph.GetRoutesFrom(hub.Id);
            var neighbors = new List<string>();
            for (int i = 0; i < routes.Count; i++)
            {
                if (!JapanTravelGraph.TryGetHub(routes[i].OtherHubId(hub.Id), out JapanMapHub other))
                    continue;
                if (!neighbors.Contains(other.Name))
                    neighbors.Add(other.Name);
            }

            return new JapanMapHubDetail(hub.Id, hub.Name, ProvinceName(hub),
                DomainName(hub.DomainId), hub.IsLegacyLocation, neighbors);
        }

        public IReadOnlyList<JapanMapRouteCard> BuildRouteCards()
        {
            var cards = new List<JapanMapRouteCard>();
            if (string.IsNullOrEmpty(_selectedHubId) ||
                !JapanTravelGraph.TryGetHub(_selectedHubId, out JapanMapHub hub))
                return cards;

            IReadOnlyList<JapanMapRoute> routes = JapanTravelGraph.GetRoutesFrom(hub.Id);
            for (int i = 0; i < routes.Count; i++)
            {
                JapanMapRoute route = routes[i];
                if (_filter.HasValue && route.Kind != _filter.Value)
                    continue;
                string otherId = route.OtherHubId(hub.Id);
                if (!JapanTravelGraph.TryGetHub(otherId, out JapanMapHub other))
                    continue;
                cards.Add(new JapanMapRouteCard(other.Id, other.Name, route));
            }

            return cards;
        }

        private static int CountHubs(string domainId)
        {
            return JapanTravelGraph.GetHubsForDomain(domainId).Count;
        }

        private static string ProvinceName(JapanMapHub hub)
        {
            if (!string.IsNullOrEmpty(hub.ProvinceId) &&
                JapanMapCatalog.TryGetProvince(hub.ProvinceId, out JapanProvince province))
                return province.Name;
            return DomainName(hub.DomainId) + "（独立区域）";
        }

        public static string DomainName(string domainId)
        {
            return JapanMapCatalog.TryGetDomain(domainId, out JapanMapDomain domain)
                ? domain.Name
                : domainId;
        }
    }
}
