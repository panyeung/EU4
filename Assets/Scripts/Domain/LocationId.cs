using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// 地点标识符：全国旅行图中的每一处旅行枢纽都是合法地点。
    /// 三个 M02 旧地点（清洲 / 热田 / 那古野）的字符串 ID 与顺序保持不变，
    /// 新增枢纽只追加稳定 ID，因此旧存档中的 locationId 与路线端点无需改写。
    /// 中文名称是地点身份的固有数据，统一从 <see cref="JapanTravelGraph"/> 读取。
    /// </summary>
    public readonly struct LocationId : IEquatable<LocationId>
    {
        public static readonly LocationId Kiyosu = new LocationId("owari.kiyosu");
        public static readonly LocationId Atsuta = new LocationId("owari.atsuta");
        public static readonly LocationId Nagoya = new LocationId("owari.nagoya");

        private static IReadOnlyList<LocationId> _allLocations;

        private readonly string _value;

        public LocationId(string value)
        {
            _value = value;
        }

        /// <summary>稳定地点 ID 字符串（如 "owari.kiyosu"）。</summary>
        public string Value => _value;

        /// <summary>是否为全国旅行图中的已知旅行枢纽。</summary>
        public bool IsValid =>
            !string.IsNullOrEmpty(_value) && JapanTravelGraph.TryGetHub(_value, out _);

        /// <summary>全部合法地点（只读快照，顺序与旅行图枢纽顺序一致，旧三地点在前）。</summary>
        public static IReadOnlyList<LocationId> All
        {
            get
            {
                if (_allLocations == null)
                {
                    IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.Hubs;
                    var locations = new List<LocationId>(hubs.Count);
                    for (int i = 0; i < hubs.Count; i++)
                        locations.Add(new LocationId(hubs[i].Id));
                    _allLocations = locations.AsReadOnly();
                }
                return _allLocations;
            }
        }

        public static bool operator ==(LocationId a, LocationId b) => a._value == b._value;
        public static bool operator !=(LocationId a, LocationId b) => a._value != b._value;

        public bool Equals(LocationId other) => _value == other._value;
        public override bool Equals(object obj) => obj is LocationId l && Equals(l);
        public override int GetHashCode() => _value?.GetHashCode(StringComparison.Ordinal) ?? 0;
        public override string ToString() => _value;

        /// <summary>按字符串解析地点；未知 ID 返回 false 且 out 为 default。</summary>
        public static bool TryParse(string value, out LocationId result)
        {
            result = default;
            if (string.IsNullOrEmpty(value) || !JapanTravelGraph.TryGetHub(value, out _))
                return false;
            result = new LocationId(value);
            return true;
        }

        /// <summary>获取地点的中文名称；未知地点返回空字符串。</summary>
        public static string GetChineseName(LocationId id)
        {
            return JapanTravelGraph.TryGetHub(id.Value, out JapanMapHub hub)
                ? hub.Name
                : string.Empty;
        }
    }
}