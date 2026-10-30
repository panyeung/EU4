using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// 地点标识符：仅三处固定地点（清洲 / 热田 / 那古野）。
    /// 字符串常量作为稳定 ID；不存在第四地点。
    /// 中文名称是地点身份的固有数据，故在此一并提供。
    /// </summary>
    public readonly struct LocationId : IEquatable<LocationId>
    {
        public static readonly LocationId Kiyosu = new LocationId("owari.kiyosu");
        public static readonly LocationId Atsuta = new LocationId("owari.atsuta");
        public static readonly LocationId Nagoya = new LocationId("owari.nagoya");

        private static readonly LocationId[] AllLocations =
        {
            Kiyosu, Atsuta, Nagoya,
        };

        private readonly string _value;

        public LocationId(string value)
        {
            _value = value;
        }

        /// <summary>稳定地点 ID 字符串（如 "owari.kiyosu"）。</summary>
        public string Value => _value;

        /// <summary>是否为已知的三个合法地点之一。</summary>
        public bool IsValid =>
            _value == Kiyosu._value ||
            _value == Atsuta._value ||
            _value == Nagoya._value;

        /// <summary>全部合法地点（只读快照，顺序固定）。</summary>
        public static IReadOnlyList<LocationId> All => AllLocations;

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
            if (value == Kiyosu._value) { result = Kiyosu; return true; }
            if (value == Atsuta._value) { result = Atsuta; return true; }
            if (value == Nagoya._value) { result = Nagoya; return true; }
            return false;
        }

        /// <summary>获取地点的中文名称；未知地点返回空字符串。</summary>
        public static string GetChineseName(LocationId id)
        {
            if (id == Kiyosu) return "清洲";
            if (id == Atsuta) return "热田";
            if (id == Nagoya) return "那古野";
            return string.Empty;
        }
    }
}