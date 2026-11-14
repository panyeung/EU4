using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>The seven GDD 10.3 family-chronicle categories.</summary>
    public enum CampaignHistoryCategory
    {
        Rulers = 0,
        Wealth = 1,
        Ninjutsu = 2,
        People = 3,
        Relations = 4,
        HistoricalEvents = 5,
        FinalStanding = 6
    }

    /// <summary>One dated, sourced family-history line.</summary>
    public sealed class CampaignHistoryRow
    {
        public CampaignHistoryRow(int sequence, GameDate date, CampaignHistoryCategory category,
            string label, string text, string sourceId)
        {
            Sequence = sequence;
            Date = date;
            Category = category;
            Label = label ?? string.Empty;
            Text = text ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
        }

        public int Sequence { get; }
        public GameDate Date { get; }
        public CampaignHistoryCategory Category { get; }
        public string Label { get; }
        public string Text { get; }
        /// <summary>Stable source record ID (chronicle entry, run ID or relation ID).</summary>
        public string SourceId { get; }

        public override string ToString() => Text;
    }

    /// <summary>
    /// One of the five GDD 10.1 development dimensions. Each holds its own sourced
    /// fact; the view never combines them into a composite score.
    /// </summary>
    public sealed class CampaignDevelopmentDimension
    {
        public CampaignDevelopmentDimension(string name, string value, string source)
        {
            Name = name ?? string.Empty;
            Value = value ?? string.Empty;
            Source = source ?? string.Empty;
        }

        public string Name { get; }
        public string Value { get; }
        public string Source { get; }

        public string Line => "・" + Name + "：" + Value + "（来源：" + Source + "）";
    }

    /// <summary>
    /// Read-only M08 campaign review. It combines only already-persisted facts: the
    /// succession chronicle, industry settlements, research completions, mission history,
    /// family formation, committed events and family standing. Categories with no source
    /// record stay empty and are reported as missing rather than invented.
    /// </summary>
    public sealed class CampaignReviewView
    {
        private static readonly CampaignHistoryCategory[] SectionOrder =
        {
            CampaignHistoryCategory.Rulers,
            CampaignHistoryCategory.Wealth,
            CampaignHistoryCategory.Ninjutsu,
            CampaignHistoryCategory.People,
            CampaignHistoryCategory.Relations,
            CampaignHistoryCategory.HistoricalEvents,
            CampaignHistoryCategory.FinalStanding
        };

        private readonly List<CampaignHistoryRow> _timeline;
        private readonly List<CampaignDevelopmentDimension> _dimensions;
        private readonly List<string> _missingNotes;

        public CampaignReviewView()
        {
            _timeline = new List<CampaignHistoryRow>();
            _dimensions = new List<CampaignDevelopmentDimension>();
            _missingNotes = new List<string>();
            Milestone = CampaignReviewMilestone.None;
            Title = string.Empty;
            Subtitle = string.Empty;
            ContextLine = string.Empty;
            ReviewDateLine = string.Empty;
            ContinueLabel = string.Empty;
            ScorePolicyNote = string.Empty;
        }

        public bool IsAvailable { get; private set; }
        public CampaignReviewMilestone Milestone { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string ContextLine { get; private set; }
        public string ReviewDateLine { get; private set; }
        public string ContinueLabel { get; private set; }
        public string ScorePolicyNote { get; private set; }
        public IReadOnlyList<CampaignHistoryRow> Timeline => _timeline;
        public IReadOnlyList<CampaignDevelopmentDimension> Dimensions => _dimensions;
        public IReadOnlyList<string> MissingSourceNotes => _missingNotes;

        public static CampaignReviewView Unavailable => new CampaignReviewView();

        public static string MilestoneTitle(CampaignReviewMilestone milestone)
        {
            switch (milestone)
            {
                case CampaignReviewMilestone.StageReview:
                    return "五十五周年 · 阶段评价";
                case CampaignReviewMilestone.CenturyReview:
                    return "百年名门 · 百年评定";
                default:
                    return "家族回顾";
            }
        }

        public static string MilestoneContinueLabel(CampaignReviewMilestone milestone)
        {
            switch (milestone)
            {
                case CampaignReviewMilestone.StageReview:
                    return "继续家族（阶段评价不终止游戏）";
                case CampaignReviewMilestone.CenturyReview:
                    return "确认百年名门评定并继续";
                default:
                    return "继续";
            }
        }

        /// <summary>
        /// One-line origin summary used by the title and campaign headers. Unknown legacy
        /// origins are reported as unknown; nothing is inferred for display.
        /// </summary>
        public static string DescribeCampaign(CampaignProgress progress, GameDate today)
        {
            if (progress == null || !progress.StartKnown)
                return "旧档开局日期未知：不判定五十五周年评价或百年名门。";
            CampaignOriginCatalog.TryGet(progress.OriginId, out CampaignOrigin origin);
            string name = string.IsNullOrEmpty(origin.DisplayName)
                ? progress.OriginId
                : origin.DisplayName;
            return "剧本来源：" + name + "｜开局：" + progress.StartDate.ToString() +
                   "｜已历 " + progress.CompletedYears(today) + " 年";
        }

        /// <summary>Builds the review for one pending milestone.</summary>
        public static CampaignReviewView Build(CampaignReviewMilestone milestone,
            CampaignProgress progress, GameDate today, SuccessionState succession,
            IReadOnlyList<FamilyChronicleEntry> chronicle,
            IReadOnlyList<MissionRecord> missionHistory, FamilyStandingState standing,
            IndustryResources resources, IReadOnlyList<string> knownTechniqueIds,
            int totalMerit, string rankName)
        {
            var view = new CampaignReviewView();
            if (milestone == CampaignReviewMilestone.None || progress == null)
                return view;

            var accumulator = new Accumulator(view, progress, today, succession, standing);
            accumulator.AddChronicle(chronicle);
            accumulator.AddMissionHistory(missionHistory);
            accumulator.AddFinalStanding(resources, knownTechniqueIds, totalMerit, rankName);
            accumulator.Finish(milestone);
            return view;
        }

        /// <summary>
        /// Multi-line body for the uGUI panel: exactly one section per GDD 10.3 category,
        /// in GDD order, each holding its complete history in chronological order, then
        /// the five sourced dimensions and the explicit missing-source notes. Every
        /// recorded row is printed once; no second flat timeline repeats the history.
        /// </summary>
        public string FormatBody()
        {
            var builder = new StringBuilder(4096);
            for (int s = 0; s < SectionOrder.Length; s++)
            {
                CampaignHistoryCategory category = SectionOrder[s];
                builder.Append('【').Append(CategoryName(category)).Append('】');
                bool any = false;
                for (int i = 0; i < _timeline.Count; i++)
                {
                    CampaignHistoryRow row = _timeline[i];
                    if (row.Category != category)
                        continue;
                    builder.Append('\n').Append(FormatRow(row));
                    any = true;
                }
                if (!any)
                    builder.Append("\n（无权威记录）");
                builder.Append('\n');
            }

            builder.Append("【五项发展维度】");
            for (int i = 0; i < _dimensions.Count; i++)
                builder.Append('\n').Append(_dimensions[i].Line);
            builder.Append('\n');

            builder.Append("【说明】");
            builder.Append('\n').Append(ScorePolicyNote);
            for (int i = 0; i < _missingNotes.Count; i++)
                builder.Append('\n').Append("・").Append(_missingNotes[i]);
            return builder.ToString();
        }

        public static string CategoryName(CampaignHistoryCategory category)
        {
            switch (category)
            {
                case CampaignHistoryCategory.Rulers: return "历代家主";
                case CampaignHistoryCategory.Wealth: return "家族财富变化";
                case CampaignHistoryCategory.Ninjutsu: return "忍术成就";
                case CampaignHistoryCategory.People: return "人物死亡与重要经历";
                case CampaignHistoryCategory.Relations: return "家族盟友和仇敌";
                case CampaignHistoryCategory.HistoricalEvents: return "重大历史事件";
                case CampaignHistoryCategory.FinalStanding: return "家族最终地位";
                default: return category.ToString();
            }
        }

        private static string FormatRow(CampaignHistoryRow row)
        {
            var builder = new StringBuilder(96);
            if (row.Date.IsValid)
                builder.Append(row.Date.ToString()).Append('・');
            if (!string.IsNullOrEmpty(row.Label))
                builder.Append(row.Label).Append("：");
            builder.Append(row.Text);
            return builder.ToString();
        }

        /// <summary>
        /// Collects rows from the live sources. A falsy/absent source adds no row, so the
        /// panel can show an explicit missing-state instead of fabricated content.
        /// </summary>
        private sealed class Accumulator
        {
            private readonly CampaignReviewView _view;
            private readonly CampaignProgress _progress;
            private readonly GameDate _today;
            private readonly SuccessionState _succession;
            private readonly FamilyStandingState _standing;
            private readonly List<string> _knownTechniques = new List<string>();

            public Accumulator(CampaignReviewView view, CampaignProgress progress, GameDate today,
                SuccessionState succession, FamilyStandingState standing)
            {
                _view = view;
                _progress = progress;
                _today = today;
                _succession = succession;
                _standing = standing;
            }

            public void AddChronicle(IReadOnlyList<FamilyChronicleEntry> entries)
            {
                int count = entries != null ? entries.Count : 0;
                for (int i = 0; i < count; i++)
                {
                    FamilyChronicleEntry entry = entries[i];
                    if (entry == null)
                        continue;
                    switch (entry.Category)
                    {
                        case ChronicleCategory.Succession:
                            if (entry.HasHeadChange)
                                Add(YearMonth(entry), CampaignHistoryCategory.Rulers,
                                    "家主更替", HeadName(entry.FromHeadId) + " → " +
                                    HeadName(entry.ToHeadId) + "（" + entry.Summary + "）",
                                    entry.EntryId);
                            break;
                        case ChronicleCategory.Industry:
                            if (string.Equals(entry.SourceId, "settlement", StringComparison.Ordinal) ||
                                entry.FromValue != entry.ToValue)
                                Add(YearMonth(entry), CampaignHistoryCategory.Wealth,
                                    "产业结算", "资金 " + entry.FromValue + " → " +
                                    entry.ToValue + "；" + entry.Summary, entry.EntryId);
                            break;
                        case ChronicleCategory.Research:
                            Add(YearMonth(entry), CampaignHistoryCategory.Ninjutsu,
                                "完成研究", FamilyResearchCatalog.TechniqueDisplayName(entry.SourceId) +
                                "（进度 " + entry.FromValue + " → " + entry.ToValue + "）；" +
                                entry.Summary, entry.EntryId);
                            break;
                        case ChronicleCategory.Education:
                            Add(YearMonth(entry), CampaignHistoryCategory.People,
                                "成长经历", entry.Title + "；" + entry.Summary, entry.EntryId);
                            break;
                        case ChronicleCategory.FamilyFormation:
                            Add(YearMonth(entry), CampaignHistoryCategory.People,
                                FormationLabel(entry.SourceId), entry.Title + "；" + entry.Summary,
                                entry.EntryId);
                            break;
                        case ChronicleCategory.NaturalDeath:
                            Add(YearMonth(entry), CampaignHistoryCategory.People,
                                "自然死亡", entry.Title + "；" + entry.Summary, entry.EntryId);
                            break;
                        case ChronicleCategory.Event:
                            Add(YearMonth(entry), CampaignHistoryCategory.HistoricalEvents,
                                "事件抉择", entry.Title + "；" + entry.Summary, entry.EntryId);
                            break;
                        case ChronicleCategory.Relationship:
                            Add(YearMonth(entry), CampaignHistoryCategory.Relations,
                                "人物关系", NpcName(entry.SubjectId) + "（稳定 ID " +
                                entry.SubjectId + "）" + entry.FromValue + " → " +
                                entry.ToValue + "（" + SignName(entry.ToValue) + "）",
                                entry.EntryId);
                            break;
                    }
                }
            }

            public void AddMissionHistory(IReadOnlyList<MissionRecord> records)
            {
                int count = records != null ? records.Count : 0;
                for (int i = 0; i < count; i++)
                {
                    MissionRecord record = records[i];
                    if (record == null)
                        continue;
                    string kind = MissionOutcomeTable.KindName(record.Kind);
                    string outcome = MissionOutcomeTable.OutcomeName(record.Outcome);
                    Add(record.Date, CampaignHistoryCategory.People, "任务经历",
                        kind + "｜" + outcome + "｜参与：" + ParticipantNames(record) +
                        "｜" + MissionOutcomeTable.ModeName(record.Mode), record.RunId);

                    for (int p = 0; p < record.ParticipantStates.Count; p++)
                    {
                        MissionParticipantState state = record.ParticipantStates[p];
                        if (state.AliveBefore && !state.AliveAfter)
                            Add(record.Date, CampaignHistoryCategory.People, "阵亡",
                                PersonName(state.PersonId) + "（" + kind + "）", record.RunId);
                        else if (!state.InjuredBefore && state.InjuredAfter)
                            Add(record.Date, CampaignHistoryCategory.People, "负伤",
                                PersonName(state.PersonId) + "（" + kind + "）", record.RunId);
                    }

                    for (int r = 0; r < record.RelationChanges.Count; r++)
                    {
                        MissionStandingChange change = record.RelationChanges[r];
                        Add(record.Date, CampaignHistoryCategory.Relations, "势力关系变化",
                            RelationText(change.RelationId) + " " + change.Before + " → " +
                            change.After + "（" + SignName(change.After) + "）", change.RelationId);
                    }

                    // Mission settlements move the same resources the industry ledger owns,
                    // so their structured deltas are the authoritative dated wealth source
                    // for a campaign that never staffed an industry post.
                    if (record.FundsDelta != 0 || record.SuppliesDelta != 0 ||
                        record.IntelligenceDelta != 0)
                        Add(record.Date, CampaignHistoryCategory.Wealth, "任务结算资产变化",
                            "资金 " + Signed(record.FundsDelta) + "｜物资 " +
                            Signed(record.SuppliesDelta) + "｜情报 " +
                            Signed(record.IntelligenceDelta) + "（" + kind + "）", record.RunId);
                }
            }

            public void AddFinalStanding(IndustryResources resources,
                IReadOnlyList<string> knownTechniqueIds, int totalMerit, string rankName)
            {
                _viewResources = resources;
                _viewMerit = totalMerit;
                if (knownTechniqueIds != null)
                    for (int i = 0; i < knownTechniqueIds.Count; i++)
                        if (!string.IsNullOrWhiteSpace(knownTechniqueIds[i]))
                            _knownTechniques.Add(knownTechniqueIds[i]);

                string familyName = _succession != null && _succession.World != null
                    ? _succession.World.Family.DisplayName
                    : "（名册缺失）";
                string headName = _succession != null
                    ? PersonName(_succession.HeadPersonId)
                    : "（名册缺失）";
                int generation = _succession != null ? _succession.SuccessionCount + 1 : 0;
                // The current head is a recorded fact even before any handoff exists, so the
                // rulers section is never empty for a real roster.
                Add(_today, CampaignHistoryCategory.Rulers, "现任家主",
                    headName + "（第 " + generation + " 代）", "succession");
                Add(_today, CampaignHistoryCategory.FinalStanding, "家族",
                    familyName + "｜现任家主：" + headName + "（第 " + generation + " 代）", "succession");
                Add(_today, CampaignHistoryCategory.FinalStanding, "名望与隐蔽",
                    "名望 " + (_standing != null ? _standing.Fame : 0) +
                    "｜隐蔽度 " + (_standing != null ? _standing.Concealment : 0),
                    "familyStanding");
                Add(_today, CampaignHistoryCategory.FinalStanding, "资产",
                    "资金 " + resources.Funds + "｜物资 " + resources.Supplies +
                    "｜情报 " + resources.Intelligence, "industry");
                Add(_today, CampaignHistoryCategory.FinalStanding, "忍术传承",
                    _knownTechniques.Count == 0
                        ? "尚无已记录的家族忍术"
                        : "已掌握 " + _knownTechniques.Count + " 项：" + TechniqueNames(),
                    "research");
                Add(_today, CampaignHistoryCategory.FinalStanding, "功绩与军阶",
                    "总功绩 " + totalMerit + "｜" + (string.IsNullOrEmpty(rankName) ? "未定" : rankName),
                    "promotion");

                // Current external relations are reported by stable ID and sign only. The
                // game defines no formal alliance or threshold, so none is implied.
                if (_standing != null)
                {
                    IReadOnlyList<KeyValuePair<string, int>> relations = _standing.RelationsSorted();
                    for (int i = 0; i < relations.Count; i++)
                        Add(_today, CampaignHistoryCategory.Relations, "当前关系",
                            RelationText(relations[i].Key) + "：" + relations[i].Value +
                            "（" + SignName(relations[i].Value) + "）", relations[i].Key);
                }
            }

            public void Finish(CampaignReviewMilestone milestone)
            {
                _view._timeline.Sort(CompareRows);
                _view.IsAvailable = true;
                _view.Milestone = milestone;
                _view.Title = MilestoneTitle(_view.Milestone);
                _view.Subtitle = _view.Milestone == CampaignReviewMilestone.CenturyReview
                    ? "从剧本实际开局起满 " + CampaignProgress.CenturyReviewYears + " 年的长期评定"
                    : "剧本实际开局后第 " + CampaignProgress.StageReviewYears +
                      " 年的初步评价（不终止游戏）";
                _view.ContextLine = ContextLine(_progress);
                _view.ReviewDateLine = _today.IsValid ? _today.ToString() : string.Empty;
                _view.ContinueLabel = MilestoneContinueLabel(_view.Milestone);
                _view.ScorePolicyNote =
                    "五项发展维度分别列出来源事实，不生成综合评分；空项表示尚无权威记录。";
                // Missing-source notes are decided once every source has contributed, so
                // a category that only mission settlements can fill is not reported empty
                // before those dated wealth deltas are known.
                AddMissingSourceNotes();
                AddDimensions();
            }

            private void AddMissingSourceNotes()
            {
                if (!HasCategory(CampaignHistoryCategory.Wealth))
                    _view._missingNotes.Add(
                        "家族财富变化：尚无已结算的产业或任务资产记录；本评价不推算开局资产。");
                if (!HasCategory(CampaignHistoryCategory.HistoricalEvents))
                    _view._missingNotes.Add(
                        "重大历史事件：尚无已提交事件记录；世界历史事件流尚未实现，本评价不补写史实节点。");
                if (!HasCategory(CampaignHistoryCategory.People))
                    _view._missingNotes.Add(
                        "人物死亡与重要经历：尚无任务、培养或家族关系记录；本评价不虚构人物经历。");
                if (!HasCategory(CampaignHistoryCategory.Relations))
                    _view._missingNotes.Add(
                        "家族盟友和仇敌：尚无稳定 ID 的外部关系或任务关系记录；游戏未定义正式同盟或敌友阈值，本评价不作判定。");
                if (CountHandoffs() == 0)
                    _view._missingNotes.Add(_succession == null
                        ? "历代家主：缺少继承名册，无法列出家主。"
                        : "历代家主：尚无家督更替记录；本次只列出当前家主。");
                if (!HasCategory(CampaignHistoryCategory.Ninjutsu))
                    _view._missingNotes.Add("忍术成就：尚无完成研究的记录。");
            }

            private void AddDimensions()
            {
                IReadOnlyList<KeyValuePair<string, int>> relations =
                    _standing != null ? _standing.RelationsSorted() : null;
                int nonZeroRelations = 0;
                if (relations != null)
                    for (int i = 0; i < relations.Count; i++)
                        if (relations[i].Value != 0)
                            nonZeroRelations++;

                bool yearsKnown = _progress.StartKnown;
                int years = _progress.CompletedYears(_today);
                int handoffs = CountHandoffs();
                int livingMembers = CountLivingMembers();

                _view._dimensions.Add(new CampaignDevelopmentDimension("家族名望",
                    _standing != null ? _standing.Fame.ToString(CultureInfo.InvariantCulture) : "0",
                    "任务结算记录的名望值"));
                _view._dimensions.Add(new CampaignDevelopmentDimension("经济实力",
                    "资金 " + _viewAssetFunds() + "｜物资 " + _viewAssetSupplies() +
                    "｜情报 " + _viewAssetIntelligence(),
                    "产业资源记录"));
                _view._dimensions.Add(new CampaignDevelopmentDimension("忍术传承",
                    _knownTechniques.Count == 0
                        ? "尚无已记录的家族忍术"
                        : _knownTechniques.Count + " 项：" + TechniqueNames(),
                    "家族研究完成记录"));
                _view._dimensions.Add(new CampaignDevelopmentDimension("政治影响力",
                    "总功绩 " + _viewMerit + "｜非零外部关系 " + nonZeroRelations + " 条",
                    "任务功绩与稳定 ID 关系记录"));
                _view._dimensions.Add(new CampaignDevelopmentDimension("家族稳定度",
                    yearsKnown
                        ? "连续 " + years + " 年传承｜家督更替 " + handoffs + " 次｜在世成员 " +
                          livingMembers + " 人"
                        : "连续年数未知（旧档开局日期未知）｜家督更替 " + handoffs +
                          " 次｜在世成员 " + livingMembers + " 人",
                    "开局日期与继承编年史"));

                if (!yearsKnown)
                    _view._missingNotes.Add(
                        "旧档开局日期未知：不判定五十五周年阶段评价或百年名门，也不推算连续年数。");
            }

            private string _viewAssetFunds() => _viewResources.Funds.ToString(CultureInfo.InvariantCulture);
            private string _viewAssetSupplies() => _viewResources.Supplies.ToString(CultureInfo.InvariantCulture);
            private string _viewAssetIntelligence() =>
                _viewResources.Intelligence.ToString(CultureInfo.InvariantCulture);

            private IndustryResources _viewResources;
            private int _viewMerit;

            private string ContextLine(CampaignProgress progress)
            {
                var builder = new StringBuilder(96);
                if (progress.StartKnown)
                {
                    CampaignOriginCatalog.TryGet(progress.OriginId, out CampaignOrigin origin);
                    builder.Append("剧本来源：")
                        .Append(string.IsNullOrEmpty(origin.DisplayName)
                            ? progress.OriginId
                            : origin.DisplayName)
                        .Append("｜开局：").Append(progress.StartDate.ToString())
                        .Append("｜已历 ").Append(progress.CompletedYears(_today)).Append(" 年");
                    if (progress.TryGetStageReviewDate(out GameDate stage))
                        builder.Append("｜55 年评价：").Append(stage.ToString());
                    if (progress.TryGetCenturyReviewDate(out GameDate century))
                        builder.Append("｜100 年评定：").Append(century.ToString());
                }
                else
                {
                    builder.Append("开局日期未知（旧档无法证明）：不判定时代评价或百年名门");
                }
                return builder.ToString();
            }

            private int CountLivingMembers()
            {
                if (_succession == null || _succession.World == null)
                    return 0;
                int living = 0;
                foreach (FamilyMembership membership in _succession.World.Family.Members)
                    if (_succession.World.IsLiving(membership.PersonId))
                        living++;
                return living;
            }

            private string ParticipantNames(MissionRecord record)
            {
                if (record.Participants.Count == 0)
                    return "未记录（旧版导入）";
                var builder = new StringBuilder(48);
                for (int i = 0; i < record.Participants.Count; i++)
                {
                    if (i > 0)
                        builder.Append('、');
                    builder.Append(PersonName(record.Participants[i]));
                }
                return builder.ToString();
            }

            private string TechniqueNames()
            {
                var builder = new StringBuilder(48);
                for (int i = 0; i < _knownTechniques.Count; i++)
                {
                    if (i > 0)
                        builder.Append('、');
                    builder.Append(FamilyResearchCatalog.TechniqueDisplayName(_knownTechniques[i]));
                }
                return builder.ToString();
            }

            private string PersonName(PersonId personId) => PersonName(_succession, personId.Value);

            private string HeadName(string personId) => PersonName(_succession, personId);

            private static string PersonName(SuccessionState succession, string personId)
            {
                if (string.IsNullOrEmpty(personId))
                    return "未知人物";
                if (succession != null && PersonId.TryParse(personId, out PersonId id) &&
                    succession.World.TryGetPerson(id, out PersonState person))
                    return person.DisplayName;
                return "不在名册（" + personId + "）";
            }

            private static string NpcName(string characterId)
            {
                if (CharacterId.TryParse(characterId, out CharacterId id) &&
                    CharacterCatalog.TryGet(id, out CharacterDefinition definition))
                    return definition.DisplayName;
                return string.IsNullOrEmpty(characterId) ? "未知人物" : characterId;
            }

            private static string RelationText(string relationId) =>
                GameSession.RelationDisplayName(relationId) + "（稳定 ID " +
                (relationId ?? string.Empty) + "）";

            private static string Signed(int value) =>
                value > 0
                    ? "+" + value.ToString(CultureInfo.InvariantCulture)
                    : value.ToString(CultureInfo.InvariantCulture);

            private static string SignName(int value) =>
                value > 0 ? "正" : value < 0 ? "负" : "零";

            private static string FormationLabel(string sourceId)
            {
                if (string.Equals(sourceId, FamilyChronicleEntry.MarriageSourceId,
                        StringComparison.Ordinal))
                    return "婚姻登记";
                if (string.Equals(sourceId, FamilyChronicleEntry.AdoptionSourceId,
                        StringComparison.Ordinal))
                    return "普通收养";
                if (string.Equals(sourceId, FamilyChronicleEntry.BirthSourceId,
                        StringComparison.Ordinal))
                    return "新生";
                return "家族关系";
            }

            private static GameDate YearMonth(FamilyChronicleEntry entry) =>
                entry != null && GameDate.IsValidDate(entry.Year, entry.Month, 1)
                    ? new GameDate(entry.Year, entry.Month, 1)
                    : default;

            private void Add(GameDate date, CampaignHistoryCategory category, string label,
                string text, string sourceId)
            {
                _view._timeline.Add(new CampaignHistoryRow(_view._timeline.Count + 1, date,
                    category, label, text, sourceId));
            }

            private bool HasCategory(CampaignHistoryCategory category)
            {
                for (int i = 0; i < _view._timeline.Count; i++)
                    if (_view._timeline[i].Category == category)
                        return true;
                return false;
            }

            private int CountHandoffs()
            {
                int count = 0;
                for (int i = 0; i < _view._timeline.Count; i++)
                    if (_view._timeline[i].Category == CampaignHistoryCategory.Rulers &&
                        string.Equals(_view._timeline[i].Label, "家主更替", StringComparison.Ordinal))
                        count++;
                return count;
            }

            private static int CompareRows(CampaignHistoryRow left, CampaignHistoryRow right)
            {
                int byDate = left.Date.CompareTo(right.Date);
                return byDate != 0 ? byDate : left.Sequence.CompareTo(right.Sequence);
            }
        }
    }
}
