using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// One historical region of the Japanese geographic catalog. The five home
    /// provinces and the seven circuits are the eight catalog groups; Ezo and
    /// Ryukyu are separate travel domains and are not provinces of the sixty-eight.
    /// Display names never act as save identities.
    /// </summary>
    public sealed class JapanMapDomain
    {
        public JapanMapDomain(string id, string name, bool isProvinceDomain)
        {
            Id = id;
            Name = name;
            IsProvinceDomain = isProvinceDomain;
        }

        public string Id { get; }
        public string Name { get; }

        /// <summary>True for the five home provinces and the seven circuits.</summary>
        public bool IsProvinceDomain { get; }
    }

    /// <summary>One of the sixty-eight historical provinces outside Ezo and Ryukyu.</summary>
    public sealed class JapanProvince
    {
        public JapanProvince(string id, string name, string domainId)
        {
            Id = id;
            Name = name;
            DomainId = domainId;
        }

        public string Id { get; }
        public string Name { get; }
        public string DomainId { get; }
    }

    /// <summary>
    /// Stable geographic catalog for the national map. Province IDs are independent
    /// of travel-hub and save IDs, so renaming a province or moving it between
    /// display groups cannot change a save identity. The catalog is intentionally
    /// separate from the sparse travel graph: a province is not automatically a
    /// visitable location, and several provinces own no travel hub yet.
    /// </summary>
    public static class JapanMapCatalog
    {
        public const int ProvinceCount = 68;

        private static readonly JapanMapDomain[] DomainList =
        {
            new JapanMapDomain("kinai", "五畿", true),
            new JapanMapDomain("tokaido", "东海道", true),
            new JapanMapDomain("tosando", "东山道", true),
            new JapanMapDomain("hokurikudo", "北陆道", true),
            new JapanMapDomain("sanyodo", "山阳道", true),
            new JapanMapDomain("sanindo", "山阴道", true),
            new JapanMapDomain("nankaido", "南海道", true),
            new JapanMapDomain("saikaido", "西海道", true),
            new JapanMapDomain("ezo", "虾夷地", false),
            new JapanMapDomain("ryukyu", "琉球", false),
        };

        private static readonly JapanProvince[] ProvinceList =
        {
            new JapanProvince("province.yamashiro", "山城", "kinai"),
            new JapanProvince("province.yamato", "大和", "kinai"),
            new JapanProvince("province.kawachi", "河内", "kinai"),
            new JapanProvince("province.izumi", "和泉", "kinai"),
            new JapanProvince("province.settsu", "摄津", "kinai"),

            new JapanProvince("province.iga", "伊贺", "tokaido"),
            new JapanProvince("province.ise", "伊势", "tokaido"),
            new JapanProvince("province.shima", "志摩", "tokaido"),
            new JapanProvince("province.owari", "尾张", "tokaido"),
            new JapanProvince("province.mikawa", "三河", "tokaido"),
            new JapanProvince("province.totomi", "远江", "tokaido"),
            new JapanProvince("province.suruga", "骏河", "tokaido"),
            new JapanProvince("province.izu", "伊豆", "tokaido"),
            new JapanProvince("province.kai", "甲斐", "tokaido"),
            new JapanProvince("province.sagami", "相模", "tokaido"),
            new JapanProvince("province.musashi", "武藏", "tokaido"),
            new JapanProvince("province.awa.kanto", "安房", "tokaido"),
            new JapanProvince("province.kazusa", "上总", "tokaido"),
            new JapanProvince("province.shimosa", "下总", "tokaido"),
            new JapanProvince("province.hitachi", "常陆", "tokaido"),

            new JapanProvince("province.omi", "近江", "tosando"),
            new JapanProvince("province.mino", "美浓", "tosando"),
            new JapanProvince("province.hida", "飞驒", "tosando"),
            new JapanProvince("province.shinano", "信浓", "tosando"),
            new JapanProvince("province.kozuke", "上野", "tosando"),
            new JapanProvince("province.shimotsuke", "下野", "tosando"),
            new JapanProvince("province.dewa", "出羽", "tosando"),
            new JapanProvince("province.mutsu", "陆奥", "tosando"),

            new JapanProvince("province.wakasa", "若狭", "hokurikudo"),
            new JapanProvince("province.echizen", "越前", "hokurikudo"),
            new JapanProvince("province.kaga", "加贺", "hokurikudo"),
            new JapanProvince("province.noto", "能登", "hokurikudo"),
            new JapanProvince("province.etchu", "越中", "hokurikudo"),
            new JapanProvince("province.echigo", "越后", "hokurikudo"),
            new JapanProvince("province.sado", "佐渡", "hokurikudo"),

            new JapanProvince("province.harima", "播磨", "sanyodo"),
            new JapanProvince("province.mimasaka", "美作", "sanyodo"),
            new JapanProvince("province.bizen", "备前", "sanyodo"),
            new JapanProvince("province.bitchu", "备中", "sanyodo"),
            new JapanProvince("province.bingo", "备后", "sanyodo"),
            new JapanProvince("province.aki", "安艺", "sanyodo"),
            new JapanProvince("province.suo", "周防", "sanyodo"),
            new JapanProvince("province.nagato", "长门", "sanyodo"),

            new JapanProvince("province.tanba", "丹波", "sanindo"),
            new JapanProvince("province.tango", "丹后", "sanindo"),
            new JapanProvince("province.tajima", "但马", "sanindo"),
            new JapanProvince("province.inaba", "因幡", "sanindo"),
            new JapanProvince("province.hoki", "伯耆", "sanindo"),
            new JapanProvince("province.izumo", "出云", "sanindo"),
            new JapanProvince("province.iwami", "石见", "sanindo"),
            new JapanProvince("province.oki", "隐岐", "sanindo"),

            new JapanProvince("province.kii", "纪伊", "nankaido"),
            new JapanProvince("province.awaji", "淡路", "nankaido"),
            new JapanProvince("province.awa", "阿波", "nankaido"),
            new JapanProvince("province.sanuki", "赞岐", "nankaido"),
            new JapanProvince("province.iyo", "伊予", "nankaido"),
            new JapanProvince("province.tosa", "土佐", "nankaido"),

            new JapanProvince("province.chikuzen", "筑前", "saikaido"),
            new JapanProvince("province.chikugo", "筑后", "saikaido"),
            new JapanProvince("province.buzen", "丰前", "saikaido"),
            new JapanProvince("province.bungo", "丰后", "saikaido"),
            new JapanProvince("province.hizen", "肥前", "saikaido"),
            new JapanProvince("province.higo", "肥后", "saikaido"),
            new JapanProvince("province.hyuga", "日向", "saikaido"),
            new JapanProvince("province.osumi", "大隅", "saikaido"),
            new JapanProvince("province.satsuma", "萨摩", "saikaido"),
            new JapanProvince("province.iki", "壹岐", "saikaido"),
            new JapanProvince("province.tsushima", "对马", "saikaido"),
        };

        private static readonly Dictionary<string, JapanMapDomain> DomainsById =
            BuildDomainIndex();
        private static readonly Dictionary<string, JapanProvince> ProvincesById =
            BuildProvinceIndex();

        public static IReadOnlyList<JapanMapDomain> Domains => DomainList;
        public static IReadOnlyList<JapanProvince> Provinces => ProvinceList;

        public static bool TryGetDomain(string domainId, out JapanMapDomain domain)
        {
            if (string.IsNullOrEmpty(domainId))
            {
                domain = null;
                return false;
            }

            return DomainsById.TryGetValue(domainId, out domain);
        }

        public static bool TryGetProvince(string provinceId, out JapanProvince province)
        {
            if (string.IsNullOrEmpty(provinceId))
            {
                province = null;
                return false;
            }

            return ProvincesById.TryGetValue(provinceId, out province);
        }

        public static IReadOnlyList<JapanProvince> GetProvinces(string domainId)
        {
            var result = new List<JapanProvince>();
            if (string.IsNullOrEmpty(domainId))
                return result;

            for (int i = 0; i < ProvinceList.Length; i++)
                if (ProvinceList[i].DomainId == domainId)
                    result.Add(ProvinceList[i]);
            return result;
        }

        private static Dictionary<string, JapanMapDomain> BuildDomainIndex()
        {
            var index = new Dictionary<string, JapanMapDomain>(StringComparer.Ordinal);
            for (int i = 0; i < DomainList.Length; i++)
                index.Add(DomainList[i].Id, DomainList[i]);
            return index;
        }

        private static Dictionary<string, JapanProvince> BuildProvinceIndex()
        {
            var index = new Dictionary<string, JapanProvince>(StringComparer.Ordinal);
            for (int i = 0; i < ProvinceList.Length; i++)
                index.Add(ProvinceList[i].Id, ProvinceList[i]);
            return index;
        }
    }
}
