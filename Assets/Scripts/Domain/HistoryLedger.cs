using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// SC-012 append-only family chronicle. Every successful append advances the
    /// sequence by exactly one and generates the stable record ID from that sequence.
    /// Failed appends never mutate the ledger, and restore is atomic: a malformed list
    /// (null record, duplicate ID, non-contiguous or descending sequence) leaves the
    /// caller without a partially rebuilt ledger.
    /// <para>
    /// Records are created only at application-level commit boundaries. Loading a save
    /// restores this ledger verbatim and must never append.
    /// </para>
    /// </summary>
    public sealed class HistoryLedger
    {
        private readonly List<FamilyChronicleEntry> _entries;
        private readonly HashSet<string> _entryIds;
        private readonly IReadOnlyList<FamilyChronicleEntry> _readOnlyEntries;

        private HistoryLedger()
        {
            _entries = new List<FamilyChronicleEntry>();
            _entryIds = new HashSet<string>(StringComparer.Ordinal);
            _readOnlyEntries = _entries.AsReadOnly();
        }

        private HistoryLedger(List<FamilyChronicleEntry> entries)
        {
            _entries = entries;
            _entryIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (FamilyChronicleEntry entry in entries)
                _entryIds.Add(entry.EntryId);
            _readOnlyEntries = _entries.AsReadOnly();
        }

        public int Count => _entries.Count;

        /// <summary>Read-only view in append order; records are immutable.</summary>
        public IReadOnlyList<FamilyChronicleEntry> Entries => _readOnlyEntries;

        public static HistoryLedger CreateEmpty() => new HistoryLedger();

        public bool TryAppendEvent(int year, int month, string eventId, string optionId,
            string title, string summary, out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.Event, eventId, optionId, title, summary,
                0, 0, 0, 0, 0, 0, null, null, out entry, out error);

        public bool TryAppendRelationship(int year, int month, string npcId, string channel,
            int previousValue, int currentValue, string title, string summary,
            out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.Relationship, npcId, channel, title, summary,
                previousValue, currentValue, 0, 0, 0, 0, null, null, out entry, out error);

        public bool TryAppendMission(int year, int month, string missionId, string actionId,
            int fromStage, int toStage, string title, string summary,
            out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.Mission, missionId, actionId, title, summary,
                0, 0, fromStage, toStage, 0, 0, null, null, out entry, out error);

        public bool TryAppendSuccession(int year, int month, string subjectId, string sourceId,
            int fromPhase, int toPhase, string title, string summary,
            out FamilyChronicleEntry entry, out string error) =>
            TryAppendSuccession(year, month, subjectId, sourceId, fromPhase, toPhase, title, summary,
                null, null, out entry, out error);

        /// <summary>
        /// Appends one succession record including the old/new head IDs. A same-phase
        /// adult designated-heir handoff is valid only when those IDs differ.
        /// </summary>
        public bool TryAppendSuccession(int year, int month, string subjectId, string sourceId,
            int fromPhase, int toPhase, string title, string summary,
            string fromHeadId, string toHeadId,
            out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.Succession, subjectId, sourceId, title, summary,
                0, 0, 0, 0, fromPhase, toPhase, fromHeadId, toHeadId, out entry, out error);

        /// <summary>
        /// Appends one industry record. Funds before/after are reused as the generic
        /// value pair; the summary carries supplies, intelligence and per-line detail.
        /// </summary>
        public bool TryAppendIndustry(int year, int month, string subjectId, string sourceId,
            int fromFunds, int toFunds, string title, string summary,
            out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.Industry, subjectId, sourceId, title, summary,
                fromFunds, toFunds, 0, 0, 0, 0, null, null, out entry, out error);

        /// <summary>
        /// Appends one annual education record. The value pair is the settled attribute
        /// before/after (or 0/0 for free growth; the summary carries all five values).
        /// </summary>
        public bool TryAppendEducation(int year, int month, string subjectId, string sourceId,
            int fromValue, int toValue, string title, string summary,
            out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.Education, subjectId, sourceId, title, summary,
                fromValue, toValue, 0, 0, 0, 0, null, null, out entry, out error);

        /// <summary>
        /// Appends one completed research record. Subject is the stable research project ID,
        /// source is the stable technique ID, and the value pair is the progress before/after.
        /// </summary>
        public bool TryAppendResearch(int year, int month, string researchId, string techniqueId,
            int fromProgress, int toProgress, string title, string summary,
            out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.Research, researchId, techniqueId, title, summary,
                fromProgress, toProgress, 0, 0, 0, 0, null, null, out entry, out error);

        /// <summary>
        /// Appends one committed marriage or ordinary adoption record. Subject is the
        /// initiating family member, source is <see cref="FamilyChronicleEntry.MarriageSourceId"/>
        /// or <see cref="FamilyChronicleEntry.AdoptionSourceId"/>.
        /// </summary>
        public bool TryAppendFamilyFormation(int year, int month, string subjectId, string sourceId,
            string title, string summary, out FamilyChronicleEntry entry, out string error) =>
            TryAppend(year, month, ChronicleCategory.FamilyFormation, subjectId, sourceId, title,
                summary, 0, 0, 0, 0, 0, 0, null, null, out entry, out error);

        /// <summary>
        /// Validates a family-formation record against the next append slot without
        /// mutating the ledger. The session calls this before adding any member or
        /// kinship edge so a rejected command cannot leave a partial relationship.
        /// </summary>
        internal bool TryPrepareFamilyFormation(int year, int month, string subjectId,
            string sourceId, string title, string summary, out FamilyChronicleEntry entry,
            out string error)
        {
            entry = null;
            error = string.Empty;
            int sequence = _entries.Count + 1;
            string entryId = FamilyChronicleEntry.BuildEntryId(sequence);
            if (_entryIds.Contains(entryId))
            {
                error = "编年史记录 ID 已存在。" + entryId;
                return false;
            }
            return FamilyChronicleEntry.TryCreate(sequence, entryId, year, month,
                ChronicleCategory.FamilyFormation, subjectId, sourceId, title, summary,
                0, 0, 0, 0, 0, 0, null, null, out entry, out error);
        }

        /// <summary>
        /// Appends a record previously produced by <see cref="TryPrepareFamilyFormation"/>.
        /// The entry is immutable and already validated, so this cannot create a malformed
        /// record; it still refuses a stale sequence or duplicate ID.
        /// </summary>
        internal bool TryAppendPrepared(FamilyChronicleEntry entry, out string error)
        {
            error = string.Empty;
            if (entry == null || entry.Sequence != _entries.Count + 1)
            {
                error = "编年史预备记录序号无效。";
                return false;
            }
            if (_entryIds.Contains(entry.EntryId))
            {
                error = "编年史记录 ID 已存在。" + entry.EntryId;
                return false;
            }
            _entries.Add(entry);
            _entryIds.Add(entry.EntryId);
            return true;
        }

        /// <summary>Returns a shallow copy of the record references; records are immutable.</summary>
        public FamilyChronicleEntry[] Capture() => _entries.ToArray();

        /// <summary>
        /// Rebuilds a ledger from saved records. <paramref name="saved"/> may be empty
        /// (a legacy save with no history) but every supplied record must be valid,
        /// numbered 1..n in order with unique IDs, and its ID must be the one derived
        /// from its sequence. Duplicate IDs are reported before the ID/sequence
        /// mismatch so a duplicated record keeps its specific error.
        /// </summary>
        public static bool TryRestore(FamilyChronicleEntry[] saved, out HistoryLedger ledger,
            out string error)
        {
            ledger = null;
            error = string.Empty;
            if (saved == null || saved.Length == 0)
            {
                ledger = CreateEmpty();
                return true;
            }

            var restored = new List<FamilyChronicleEntry>(saved.Length);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < saved.Length; i++)
            {
                FamilyChronicleEntry entry = saved[i];
                if (entry == null)
                {
                    error = "编年史记录为空。";
                    return false;
                }
                if (entry.Sequence != i + 1)
                {
                    error = "编年史序号必须从 1 连续递增。";
                    return false;
                }
                if (!ids.Add(entry.EntryId))
                {
                    error = "编年史记录 ID 重复。" + entry.EntryId;
                    return false;
                }
                restored.Add(entry);
            }

            for (int i = 0; i < restored.Count; i++)
            {
                FamilyChronicleEntry entry = restored[i];
                string expectedId = FamilyChronicleEntry.BuildEntryId(entry.Sequence);
                if (!string.Equals(entry.EntryId, expectedId, StringComparison.Ordinal))
                {
                    error = "编年史记录 ID 与序号不一致：" + entry.EntryId +
                            "（应为 " + expectedId + "）。";
                    return false;
                }
            }

            ledger = new HistoryLedger(restored);
            return true;
        }

        private bool TryAppend(int year, int month, ChronicleCategory category, string subjectId,
            string sourceId, string title, string summary, int fromValue, int toValue,
            int fromStage, int toStage, int fromPhase, int toPhase,
            string fromHeadId, string toHeadId,
            out FamilyChronicleEntry entry, out string error)
        {
            entry = null;
            error = string.Empty;

            int sequence = _entries.Count + 1;
            string entryId = FamilyChronicleEntry.BuildEntryId(sequence);
            if (_entryIds.Contains(entryId))
            {
                error = "编年史记录 ID 已存在。" + entryId;
                return false;
            }
            if (!FamilyChronicleEntry.TryCreate(sequence, entryId, year, month, category, subjectId,
                    sourceId, title, summary, fromValue, toValue, fromStage, toStage,
                    fromPhase, toPhase, fromHeadId, toHeadId,
                    out FamilyChronicleEntry created, out error))
                return false;

            _entries.Add(created);
            _entryIds.Add(created.EntryId);
            entry = created;
            return true;
        }
    }
}
