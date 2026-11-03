using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// SC-012 Task 1: the append-only domain history ledger rejects malformed
    /// records atomically and produces deterministic IDs and sequence numbers.
    /// </summary>
    public sealed class HistoryLedgerTests
    {
        private static FamilyChronicleEntry AppendEvent(HistoryLedger ledger, int year = 1560, int month = 4,
            string eventId = "event.rainy-arrival", string optionId = "option.rainy-arrival.offer-rest",
            string title = "雨夜来客", string summary = "请他进屋歇息。")
        {
            Assert.That(ledger.TryAppendEvent(year, month, eventId, optionId, title, summary,
                out FamilyChronicleEntry entry, out string error), Is.True, error);
            return entry;
        }

        [Test]
        public void EmptyLedgerHasNoEntries()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.Count, Is.Zero);
            Assert.That(ledger.Entries, Is.Empty);
            Assert.That(ledger.Capture(), Is.Empty);
        }

        [Test]
        public void ConsecutiveAppendsGetIncreasingSequenceAndStableIds()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            FamilyChronicleEntry first = AppendEvent(ledger);
            FamilyChronicleEntry second = AppendEvent(ledger, eventId: "event.second");

            Assert.That(ledger.Count, Is.EqualTo(2));
            Assert.That(first.Sequence, Is.EqualTo(1));
            Assert.That(second.Sequence, Is.EqualTo(2));
            Assert.That(first.EntryId, Is.EqualTo(FamilyChronicleEntry.BuildEntryId(1)));
            Assert.That(second.EntryId, Is.EqualTo(FamilyChronicleEntry.BuildEntryId(2)));
            Assert.That(first.EntryId, Is.Not.EqualTo(second.EntryId));
            Assert.That(ledger.Entries[0], Is.SameAs(first));
            Assert.That(ledger.Entries[1], Is.SameAs(second));
        }

        [Test]
        public void SameInputSequenceProducesSameOrderAndIds()
        {
            HistoryLedger first = HistoryLedger.CreateEmpty();
            HistoryLedger second = HistoryLedger.CreateEmpty();

            AppendEvent(first);
            AppendEvent(first, month: 5, eventId: "event.second");
            AppendEvent(second);
            AppendEvent(second, month: 5, eventId: "event.second");

            Assert.That(first.Capture().Length, Is.EqualTo(second.Capture().Length));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(first.Entries[i].EntryId, Is.EqualTo(second.Entries[i].EntryId));
                Assert.That(first.Entries[i].Sequence, Is.EqualTo(second.Entries[i].Sequence));
                Assert.That(first.Entries[i].SubjectId, Is.EqualTo(second.Entries[i].SubjectId));
            }
        }

        [Test]
        public void EventRecordKeepsYearMonthAndCategoryFields()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            FamilyChronicleEntry entry = AppendEvent(ledger, 1561, 11);

            Assert.That(entry.Year, Is.EqualTo(1561));
            Assert.That(entry.Month, Is.EqualTo(11));
            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Event));
            Assert.That(entry.SubjectId, Is.EqualTo("event.rainy-arrival"));
            Assert.That(entry.SourceId, Is.EqualTo("option.rainy-arrival.offer-rest"));
            Assert.That(entry.Title, Is.EqualTo("雨夜来客"));
            Assert.That(entry.Summary, Is.EqualTo("请他进屋歇息。"));
        }

        [Test]
        public void RelationshipRecordKeepsPreviousAndCurrentValues()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendRelationship(1560, 4, "npc.kuroda.iori", "dialogue", 0, 5,
                "关系变化：黒田 伊織", "关系 0 → 5。", out FamilyChronicleEntry entry, out string error),
                Is.True, error);

            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Relationship));
            Assert.That(entry.FromValue, Is.Zero);
            Assert.That(entry.ToValue, Is.EqualTo(5));
            Assert.That(entry.SubjectId, Is.EqualTo("npc.kuroda.iori"));
            Assert.That(entry.SourceId, Is.EqualTo("dialogue"));
        }

        [Test]
        public void MissionRecordKeepsStageTransition()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendMission(1560, 4, "mission.supply-shortage", "accept", 0, 1,
                "任务：补给短缺", "已领取补给短缺任务。", out FamilyChronicleEntry entry, out string error),
                Is.True, error);

            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Mission));
            Assert.That(entry.FromStage, Is.Zero);
            Assert.That(entry.ToStage, Is.EqualTo(1));
        }

        [Test]
        public void SuccessionRecordKeepsPhaseTransition()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendSuccession(1560, 4, "person.akechi.masatsuna", "succession.1",
                (int)SuccessionPhase.AwaitingSuccessionChoice, (int)SuccessionPhase.None,
                "继承：明智 正綱", "家督已交接。", out FamilyChronicleEntry entry, out string error),
                Is.True, error);

            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Succession));
            Assert.That(entry.FromPhase, Is.EqualTo((int)SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(entry.ToPhase, Is.EqualTo((int)SuccessionPhase.None));
        }

        [TestCase(1559, 1)]
        [TestCase(1560, 0)]
        [TestCase(1560, 13)]
        [TestCase(2001, 1)]
        public void InvalidYearOrMonthIsRejected(int year, int month)
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendEvent(year, month, "event.x", "option.x", "标题", "摘要",
                out FamilyChronicleEntry entry, out string error), Is.False);

            Assert.That(entry, Is.Null);
            Assert.That(error, Is.Not.Empty);
            Assert.That(ledger.Count, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void EmptySubjectOrSourceIdIsRejected(string value)
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendEvent(1560, 4, value, "option.x", "标题", "摘要",
                out _, out string subjectError), Is.False);
            Assert.That(subjectError, Is.Not.Empty);

            Assert.That(ledger.TryAppendEvent(1560, 4, "event.x", value, "标题", "摘要",
                out _, out string sourceError), Is.False);
            Assert.That(sourceError, Is.Not.Empty);
            Assert.That(ledger.Count, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void EmptyTitleIsRejected(string value)
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendEvent(1560, 4, "event.x", "option.x", value, "摘要",
                out _, out string error), Is.False);

            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void NullSummaryIsStoredAsEmptyText()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendEvent(1560, 4, "event.x", "option.x", "标题", null,
                out FamilyChronicleEntry entry, out string error), Is.True, error);

            Assert.That(entry.Summary, Is.Empty);
        }

        [Test]
        public void RelationshipWithEqualValuesIsRejected()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendRelationship(1560, 4, "npc.kuroda.iori", "direct", 5, 5,
                "关系变化", "无变化", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("关系"));
        }

        [TestCase(-101, 0)]
        [TestCase(0, 101)]
        [TestCase(-101, 101)]
        public void RelationshipOutsideSupportedRangeIsRejected(int previous, int current)
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendRelationship(1560, 4, "npc.kuroda.iori", "direct", previous, current,
                "关系变化", "越界", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("关系"));
        }

        [Test]
        public void MissionWithEqualStagesIsRejected()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendMission(1560, 4, "mission.supply-shortage", "accept", 2, 2,
                "任务", "无变化", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("阶段"));
        }

        [TestCase(-1, 1)]
        [TestCase(0, 5)]
        public void MissionStageOutsideSupportedRangeIsRejected(int from, int to)
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendMission(1560, 4, "mission.supply-shortage", "accept", from, to,
                "任务", "越界", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("阶段"));
        }

        [Test]
        public void MissionStageOutsideItsOwnMissionRangeIsRejected()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            // Sealed letter has only NotAccepted/Accepted/RewardClaimed (0..2); the old
            // union 0..4 wrongly accepted stage 3.
            Assert.That(ledger.TryAppendMission(1560, 4, "mission.sealed-letter", "deliver", 2, 3,
                "密信", "不可能的阶段", out _, out string sealedError), Is.False);
            Assert.That(sealedError, Does.Contain("阶段"));

            // Market tally has NotAccepted/Accepted/Investigated/RewardClaimed (0..3).
            Assert.That(ledger.TryAppendMission(1560, 4, "mission.market-tally", "report", 3, 4,
                "市集账目", "不可能的阶段", out _, out string marketError), Is.False);
            Assert.That(marketError, Does.Contain("阶段"));
            Assert.That(ledger.Count, Is.Zero);
        }

        [Test]
        public void MissionRecordAcceptsEachKnownMissionRange()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendMission(1560, 4, "mission.supply-shortage", "report", 3, 4,
                "补给短缺", "已领取报酬", out _, out string supplyError), Is.True, supplyError);
            Assert.That(ledger.TryAppendMission(1560, 4, "mission.sealed-letter", "deliver", 1, 2,
                "密信", "已领取报酬", out _, out string sealedError), Is.True, sealedError);
            Assert.That(ledger.TryAppendMission(1560, 4, "mission.market-tally", "report", 2, 3,
                "市集账目", "已领取报酬", out _, out string marketError), Is.True, marketError);
        }

        [Test]
        public void MissionRecordWithUnknownMissionIdIsRejected()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendMission(1560, 4, "mission.unknown", "accept", 0, 1,
                "未知任务", "未知任务 ID", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("未知任务"));
            Assert.That(ledger.Count, Is.Zero);
        }

        [Test]
        public void SuccessionWithEqualPhasesIsRejected()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendSuccession(1560, 4, "person.akechi.harunobu", "result", 0, 0,
                "继承", "无变化", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("继承"));
        }

        [Test]
        public void SamePhaseHeadHandoffIsRecordedOnlyWithDifferentHeadIds()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendSuccession(1560, 4, "person.akechi.kageyu",
                "succession.1.person.akechi.kageyu", 0, 0, "明智 景祐", "指定继承人已继位。",
                "person.akechi.harunobu", "person.akechi.kageyu",
                out FamilyChronicleEntry entry, out string error), Is.True, error);

            Assert.That(entry.FromPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(entry.ToPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(entry.FromHeadId, Is.EqualTo("person.akechi.harunobu"));
            Assert.That(entry.ToHeadId, Is.EqualTo("person.akechi.kageyu"));
            Assert.That(entry.HasHeadChange, Is.True);

            Assert.That(ledger.TryAppendSuccession(1560, 4, "person.akechi.harunobu", "result",
                0, 0, "继承", "无变化", "person.akechi.harunobu", "person.akechi.harunobu",
                out _, out string noChangeError), Is.False);
            Assert.That(noChangeError, Does.Contain("继承"));
            Assert.That(ledger.Count, Is.EqualTo(1));
        }

        [TestCase(-1, 0)]
        [TestCase(0, 99)]
        public void SuccessionUndefinedPhaseIsRejected(int from, int to)
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();

            Assert.That(ledger.TryAppendSuccession(1560, 4, "person.akechi.harunobu", "result", from, to,
                "继承", "非法阶段", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("继承"));
        }

        [Test]
        public void CaptureThenRestoreKeepsEveryRecord()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();
            AppendEvent(ledger);
            Assert.That(ledger.TryAppendRelationship(1560, 4, "npc.kuroda.iori", "event", 0, 5,
                "关系", "关系 0 → 5", out _, out string error), Is.True, error);

            Assert.That(HistoryLedger.TryRestore(ledger.Capture(), out HistoryLedger restored,
                out string restoreError), Is.True, restoreError);

            Assert.That(restored.Count, Is.EqualTo(2));
            Assert.That(restored.Entries[0].EntryId, Is.EqualTo(ledger.Entries[0].EntryId));
            Assert.That(restored.Entries[1].FromValue, Is.Zero);
            Assert.That(restored.Entries[1].ToValue, Is.EqualTo(5));
        }

        [Test]
        public void RestoreRejectsDuplicateIds()
        {
            Assert.That(FamilyChronicleEntry.TryCreate(1, "chronicle.dup", 1560, 4,
                ChronicleCategory.Event, "event.a", "option.a", "标题", "摘要", 0, 0, 0, 0, 0, 0,
                out FamilyChronicleEntry first, out string firstError), Is.True, firstError);
            Assert.That(FamilyChronicleEntry.TryCreate(2, "chronicle.dup", 1560, 4,
                ChronicleCategory.Event, "event.b", "option.b", "标题", "摘要", 0, 0, 0, 0, 0, 0,
                out FamilyChronicleEntry second, out string secondError), Is.True, secondError);

            Assert.That(HistoryLedger.TryRestore(new[] { first, second }, out HistoryLedger ledger,
                out string error), Is.False);

            Assert.That(ledger, Is.Null);
            Assert.That(error, Does.Contain("重复"));
        }

        [Test]
        public void RestoreRejectsOutOfOrderOrDuplicateSequence()
        {
            Assert.That(FamilyChronicleEntry.TryCreate(2, "chronicle.000002", 1560, 4,
                ChronicleCategory.Event, "event.a", "option.a", "标题", "摘要", 0, 0, 0, 0, 0, 0,
                out FamilyChronicleEntry first, out string firstError), Is.True, firstError);
            Assert.That(FamilyChronicleEntry.TryCreate(2, "chronicle.000003", 1560, 4,
                ChronicleCategory.Event, "event.b", "option.b", "标题", "摘要", 0, 0, 0, 0, 0, 0,
                out FamilyChronicleEntry second, out string secondError), Is.True, secondError);

            Assert.That(HistoryLedger.TryRestore(new[] { first, second }, out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void RestoreRejectsUniqueIdThatDoesNotMatchItsSequence()
        {
            Assert.That(FamilyChronicleEntry.TryCreate(1, FamilyChronicleEntry.BuildEntryId(2), 1560, 4,
                ChronicleCategory.Event, "event.a", "option.a", "标题", "摘要", 0, 0, 0, 0, 0, 0,
                out FamilyChronicleEntry entry, out string createError), Is.True, createError);

            Assert.That(HistoryLedger.TryRestore(new[] { entry }, out HistoryLedger ledger,
                out string error), Is.False);

            Assert.That(ledger, Is.Null);
            Assert.That(error, Does.Contain("不一致"));
        }

        [Test]
        public void RestoreRejectsNullEntry()
        {
            Assert.That(HistoryLedger.TryRestore(new FamilyChronicleEntry[] { null }, out HistoryLedger ledger,
                out string error), Is.False);
            Assert.That(ledger, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void InvalidAppendDoesNotChangeExistingLedger()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();
            AppendEvent(ledger);

            Assert.That(ledger.TryAppendRelationship(1560, 4, "npc.kuroda.iori", "direct", 3, 3,
                "关系", "无变化", out _, out _), Is.False);

            Assert.That(ledger.Count, Is.EqualTo(1));
            Assert.That(ledger.Entries[0].Sequence, Is.EqualTo(1));
        }
    }
}
