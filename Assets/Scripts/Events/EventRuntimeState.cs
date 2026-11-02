using System;
using System.Collections.Generic;
using System.Linq;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Events
{
    /// <summary>
    /// Runtime queue and durable event markers. All ID collections use ordinal
    /// comparison; capture emits marker arrays in stable order while preserving the
    /// meaningful order of pending events and history.
    /// <para>
    /// The queue can store multiple ordered entries and commit always removes the
    /// head. The current monthly trigger enqueues at most one event per month turn
    /// and blocks further turns until it is resolved; additional entries are reserved
    /// for future triggers (for example event chains) and are not reachable from the
    /// demo's monthly path.
    /// </para>
    /// </summary>
    public sealed class EventRuntimeState
    {
        private readonly List<string> _pendingEventIds = new List<string>();
        private readonly SortedDictionary<string, int> _cooldowns = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly SortedSet<string> _onceEventIds = new SortedSet<string>(StringComparer.Ordinal);
        private readonly List<EventHistorySaveEntry> _history = new List<EventHistorySaveEntry>();
        private readonly SortedSet<string> _committedResultIds = new SortedSet<string>(StringComparer.Ordinal);

        public StableGamePhase Phase { get; private set; } = StableGamePhase.Management;
        public bool HasPendingEvents => _pendingEventIds.Count > 0;
        public string PendingEventId => HasPendingEvents ? _pendingEventIds[0] : string.Empty;
        public IReadOnlyList<string> PendingEventIds => _pendingEventIds.AsReadOnly();
        public IReadOnlyList<EventHistorySaveEntry> History => _history.AsReadOnly();

        public EventRuntimeSaveData Capture()
        {
            var cooldowns = new EventCooldownSaveEntry[_cooldowns.Count];
            int cooldownIndex = 0;
            foreach (KeyValuePair<string, int> cooldown in _cooldowns)
                cooldowns[cooldownIndex++] = new EventCooldownSaveEntry
                {
                    eventId = cooldown.Key,
                    availableAtMonth = cooldown.Value
                };

            return new EventRuntimeSaveData
            {
                phase = (int)Phase,
                pendingEventIds = _pendingEventIds.ToArray(),
                cooldowns = cooldowns,
                onceEventIds = _onceEventIds.ToArray(),
                history = CloneHistory(_history),
                committedResultIds = _committedResultIds.ToArray()
            };
        }

        public EventRuntimeState Clone()
        {
            return TryRestore(Capture(), out EventRuntimeState clone, out _)
                ? clone
                : new EventRuntimeState();
        }

        public static bool TryRestore(EventRuntimeSaveData data, out EventRuntimeState state, out string error)
        {
            return TryRestore(data, null, out state, out error);
        }

        /// <summary>
        /// Restores a validated snapshot. Supplying the save date additionally rejects
        /// a pending event that is still inside its cooldown window.
        /// </summary>
        public static bool TryRestore(EventRuntimeSaveData data, GameDate? currentDate,
            out EventRuntimeState state, out string error)
        {
            state = null;
            error = string.Empty;
            SaveOperationResult validation = SaveDataValidator.ValidateEventRuntimeSnapshot(data, currentDate);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            var restored = new EventRuntimeState { Phase = (StableGamePhase)data.phase };
            foreach (string id in data.pendingEventIds)
                restored._pendingEventIds.Add(id);
            foreach (EventCooldownSaveEntry cooldown in data.cooldowns)
                restored._cooldowns.Add(cooldown.eventId, cooldown.availableAtMonth);
            foreach (string id in data.onceEventIds)
                restored._onceEventIds.Add(id);
            foreach (string id in data.committedResultIds)
                restored._committedResultIds.Add(id);
            foreach (EventHistorySaveEntry entry in data.history)
                restored._history.Add(CloneHistoryEntry(entry));

            state = restored;
            return true;
        }

        internal bool IsOnCooldown(string eventId, int monthIndex)
        {
            return _cooldowns.TryGetValue(eventId, out int availableAtMonth) && monthIndex < availableAtMonth;
        }

        internal bool HasOccurredOnce(string eventId) => _onceEventIds.Contains(eventId);
        internal bool HasCommittedResult(string resultId) => _committedResultIds.Contains(resultId);

        internal void Enqueue(string eventId)
        {
            _pendingEventIds.Add(eventId);
            Phase = StableGamePhase.AwaitingEventChoice;
        }

        internal bool TryCommit(string eventId, string optionId, string resultId, int monthIndex,
            int cooldownMonths, EventOnceScope onceScope, string summary, int year, int month)
        {
            if (!HasPendingEvents || !string.Equals(PendingEventId, eventId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(optionId) || string.IsNullOrWhiteSpace(resultId) ||
                _committedResultIds.Contains(resultId) || monthIndex < 0 || cooldownMonths < 0 ||
                !Enum.IsDefined(typeof(EventOnceScope), onceScope))
                return false;

            int cooldownAvailableAtMonth = 0;
            if (cooldownMonths > 0)
            {
                if (monthIndex > int.MaxValue - cooldownMonths)
                    return false;
                cooldownAvailableAtMonth = monthIndex + cooldownMonths;
            }

            // Preflight every value that can fail before changing any durable marker.
            _committedResultIds.Add(resultId);
            if (cooldownMonths > 0)
                _cooldowns[eventId] = cooldownAvailableAtMonth;
            if (onceScope == EventOnceScope.PerGame)
                _onceEventIds.Add(eventId);
            _history.Add(new EventHistorySaveEntry
            {
                eventId = eventId,
                optionId = optionId,
                resultId = resultId,
                year = year,
                month = month,
                summary = summary ?? string.Empty
            });
            _pendingEventIds.RemoveAt(0);
            Phase = _pendingEventIds.Count == 0
                ? StableGamePhase.Management
                : StableGamePhase.AwaitingEventChoice;
            return true;
        }

        private static EventHistorySaveEntry[] CloneHistory(List<EventHistorySaveEntry> history)
        {
            var clone = new EventHistorySaveEntry[history.Count];
            for (int i = 0; i < history.Count; i++)
                clone[i] = CloneHistoryEntry(history[i]);
            return clone;
        }

        private static EventHistorySaveEntry CloneHistoryEntry(EventHistorySaveEntry entry)
        {
            return new EventHistorySaveEntry
            {
                eventId = entry.eventId,
                optionId = entry.optionId,
                resultId = entry.resultId,
                year = entry.year,
                month = entry.month,
                summary = entry.summary
            };
        }
    }
}
