using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 编年类别到中文文案的唯一映射。SC-012 家族编年史面板与 M02 人物经历页必须对同一类别
    /// 使用同一措辞，因此两边都读这份映射，不各自保留副本。
    /// </summary>
    internal static class ChronicleCategoryLabels
    {
        internal static string Name(ChronicleCategory category)
        {
            switch (category)
            {
                case ChronicleCategory.Event: return "事件";
                case ChronicleCategory.Relationship: return "关系";
                case ChronicleCategory.Mission: return "任务";
                case ChronicleCategory.Succession: return "继承";
                case ChronicleCategory.Industry: return "产业";
                case ChronicleCategory.Education: return "教育";
                case ChronicleCategory.Research: return "研究";
                case ChronicleCategory.FamilyFormation: return "家族关系";
                case ChronicleCategory.NaturalDeath: return "人物离世";
                default: return category.ToString();
            }
        }
    }
}
