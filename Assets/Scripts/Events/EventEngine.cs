using System;
using System.Collections.Generic;
using System.Linq;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Events
{
    public sealed class EventScheduleResult
    {
        public EventScheduleResult(bool succeeded, string selectedEventId, string message)
        {
            Succeeded = succeeded;
            SelectedEventId = selectedEventId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string SelectedEventId { get; }
        public string Message { get; }
    }

    public sealed class EventChoiceResult
    {
        public EventChoiceResult(bool succeeded, string eventId, string optionId, string resultId,
            string message, string feedback)
        {
            Succeeded = succeeded;
            EventId = eventId ?? string.Empty;
            OptionId = optionId ?? string.Empty;
            ResultId = resultId ?? string.Empty;
            Message = message ?? string.Empty;
            Feedback = feedback ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string EventId { get; }
        public string OptionId { get; }
        public string ResultId { get; }
        public string Message { get; }
        public string Feedback { get; }
    }

    /// <summary>
    /// Deterministic, data-only monthly event engine. It uses a validated allowlist
    /// catalog and mutates only plain domain/runtime state supplied by the caller.
    /// </summary>
    public sealed class EventEngine
    {
        private readonly EventCatalogData _catalog;
        private readonly EventTextCatalog _text;
        private readonly Dictionary<string, EventDefinitionData> _events =
            new Dictionary<string, EventDefinitionData>(StringComparer.Ordinal);
        private readonly Dictionary<string, EventTextEntry> _texts =
            new Dictionary<string, EventTextEntry>(StringComparer.Ordinal);

        public EventEngine(EventCatalogData catalog, EventTextCatalog text)
        {
            _catalog = catalog;
            _text = text;
            EventCatalogValidationResult validation = EventCatalogValidator.Validate(catalog, text);
            IsReady = validation.Succeeded;
            ValidationMessage = validation.Message;
            if (!IsReady) return;

            foreach (EventDefinitionData definition in catalog.events)
                _events.Add(definition.eventId, definition);
            foreach (EventTextEntry entry in text.entries)
                _texts.Add(entry.textId, entry);
        }

        public bool IsReady { get; }
        public string ValidationMessage { get; }
        public EventCatalogData Catalog => _catalog;
        public EventTextCatalog TextCatalog => _text;

        public string PendingTitle(string eventId)
        {
            return TryGetEventText(eventId, out EventTextEntry entry) ? entry.title : string.Empty;
        }

        public bool TryGetEvent(string eventId, out EventDefinitionData definition)
        {
            definition = null;
            return IsReady && _events.TryGetValue(eventId ?? string.Empty, out definition);
        }

        public bool TryGetText(string textId, out EventTextEntry entry)
        {
            entry = null;
            return IsReady && _texts.TryGetValue(textId ?? string.Empty, out entry);
        }

        public bool TryGetEventText(string eventId, out EventTextEntry entry)
        {
            entry = null;
            return TryGetEvent(eventId, out EventDefinitionData definition) && TryGetText(definition.textId, out entry);
        }

        /// <summary>Returns the eligible IDs in priority-descending, ordinal-ID order.</summary>
        public IReadOnlyList<string> GetEligibleEventIds(GameDate date, CharacterRelationships relationships,
            EventRuntimeState state)
        {
            List<EventDefinitionData> candidates = GetEligibleEvents(date, relationships, state);
            return candidates.Select(candidate => candidate.eventId).ToArray();
        }

        public EventScheduleResult ScheduleMonthly(GameDate date, CharacterRelationships relationships,
            DeterministicRandomStream eventRandom, EventRuntimeState state)
        {
            if (!IsReady)
                return ScheduleFailure(ValidationMessage);
            if (!date.IsValid || relationships == null || eventRandom == null || state == null)
                return ScheduleFailure("Event scheduling received invalid runtime inputs.");
            if (state.HasPendingEvents || state.Phase != StableGamePhase.Management)
                return ScheduleFailure("A pending event must be resolved before another month is processed.");

            List<EventDefinitionData> candidates = GetEligibleEvents(date, relationships, state);
            if (candidates.Count == 0)
                return new EventScheduleResult(true, string.Empty, string.Empty);

            int priority = candidates[0].priority;
            long totalWeight = 0;
            int groupEnd = 0;
            while (groupEnd < candidates.Count && candidates[groupEnd].priority == priority)
            {
                totalWeight += candidates[groupEnd].weight;
                groupEnd++;
            }
            if (totalWeight <= 0 || totalWeight > int.MaxValue)
                return ScheduleFailure("Eligible event weight total is outside the supported range.");

            int roll = eventRandom.NextInt((int)totalWeight);
            EventDefinitionData selected = candidates[0];
            for (int i = 0; i < groupEnd; i++)
            {
                if (roll < candidates[i].weight)
                {
                    selected = candidates[i];
                    break;
                }
                roll -= candidates[i].weight;
            }

            state.Enqueue(selected.eventId);
            return new EventScheduleResult(true, selected.eventId, string.Empty);
        }

        public bool TryGetOptionAvailability(string eventId, string optionId,
            CharacterRelationships relationships, out bool enabled, out string reason)
        {
            enabled = false;
            reason = string.Empty;
            if (!TryFindOption(eventId, optionId, out _, out EventOptionData option) || relationships == null)
            {
                reason = "事件选项当前不可用。";
                return false;
            }
            enabled = AreRequirementsMet(option, relationships, out reason);
            return true;
        }

        public EventChoiceResult TryChooseEventOption(string eventId, string optionId, string committedResultId,
            GameDate currentDate, CharacterRelationships relationships, EventRuntimeState state)
        {
            if (!IsReady)
                return ChoiceFailure(eventId, optionId, committedResultId, ValidationMessage);
            if (state == null || relationships == null || !currentDate.IsValid ||
                !state.HasPendingEvents || !string.Equals(state.PendingEventId, eventId, StringComparison.Ordinal))
                return ChoiceFailure(eventId, optionId, committedResultId, "事件不再是当前等待处理的队列首项。");
            if (string.IsNullOrWhiteSpace(committedResultId) || state.HasCommittedResult(committedResultId))
                return ChoiceFailure(eventId, optionId, committedResultId, "事件结果 ID 为空或已经提交。");
            if (!TryFindOption(eventId, optionId, out EventDefinitionData definition, out EventOptionData option))
                return ChoiceFailure(eventId, optionId, committedResultId, "事件或选项 ID 无效。");
            if (!AreConditionsMet(definition, relationships))
                return ChoiceFailure(eventId, optionId, committedResultId, "事件条件已经失效，状态保持不变。");
            if (!AreRequirementsMet(option, relationships, out string requirementReason))
                return ChoiceFailure(eventId, optionId, committedResultId,
                    string.IsNullOrWhiteSpace(requirementReason) ? "当前不满足此选项条件。" : requirementReason);

            if (!TryPreflightEffects(option, relationships, out List<RelationshipCommit> effects, out string effectError))
                return ChoiceFailure(eventId, optionId, committedResultId, effectError);

            var applied = new List<RelationshipCommit>();
            foreach (RelationshipCommit effect in effects)
            {
                RelationshipResult change = relationships.TryChange(effect.CharacterId, effect.Delta);
                if (!change.Succeeded)
                {
                    for (int i = applied.Count - 1; i >= 0; i--)
                    {
                        RelationshipCommit rollback = applied[i];
                        relationships.TryChange(rollback.CharacterId, -rollback.Delta);
                    }
                    return ChoiceFailure(eventId, optionId, committedResultId,
                        "关系效果提交失败，已恢复此前效果。");
                }
                applied.Add(effect);
            }

            EventOnceScope onceScope = ParseOnceScope(definition.onceScope);
            string feedback = ResolveSummary(option);
            int monthIndex = MonthIndex(currentDate);
            if (!state.TryCommit(eventId, optionId, committedResultId, monthIndex,
                    definition.cooldownMonths, onceScope, feedback, currentDate.Year, currentDate.Month))
            {
                // The queue and result were preflighted above; this is defensive for a
                // stale caller, and rolls back the relationship delta before returning.
                for (int i = applied.Count - 1; i >= 0; i--)
                    relationships.TryChange(applied[i].CharacterId, -applied[i].Delta);
                return ChoiceFailure(eventId, optionId, committedResultId, "事件队列已变化，结果未提交。");
            }

            return new EventChoiceResult(true, eventId, optionId, committedResultId, string.Empty, feedback);
        }

        /// <summary>Validates saved IDs against the active catalog before accepting a load.</summary>
        public bool TryRestoreState(EventRuntimeSaveData save, out EventRuntimeState state, out string error)
        {
            return TryRestoreState(save, null, out state, out error);
        }

        /// <summary>
        /// Validates saved IDs against the active catalog before accepting a load.
        /// Supplying the save date also rejects a pending event still on cooldown.
        /// </summary>
        public bool TryRestoreState(EventRuntimeSaveData save, GameDate? currentDate,
            out EventRuntimeState state, out string error)
        {
            state = null;
            if (!IsReady)
            {
                error = ValidationMessage;
                return false;
            }
            if (!EventRuntimeState.TryRestore(save, currentDate, out EventRuntimeState restored, out error))
                return false;

            foreach (string pendingId in restored.PendingEventIds)
                if (!_events.ContainsKey(pendingId))
                {
                    error = "Saved pending event is not present in the active catalog: " + pendingId;
                    return false;
                }
            foreach (EventCooldownSaveEntry cooldown in save.cooldowns)
                if (!_events.ContainsKey(cooldown.eventId))
                {
                    error = "Saved cooldown event is not present in the active catalog: " + cooldown.eventId;
                    return false;
                }
            foreach (string onceEventId in save.onceEventIds)
                if (!_events.ContainsKey(onceEventId))
                {
                    error = "Saved once-only event is not present in the active catalog: " + onceEventId;
                    return false;
                }
            foreach (EventHistorySaveEntry history in save.history)
            {
                if (!_events.TryGetValue(history.eventId, out EventDefinitionData definition) ||
                    !ContainsOption(definition, history.optionId))
                {
                    error = "Saved event history refers to an unknown event or option.";
                    return false;
                }
            }

            state = restored;
            error = string.Empty;
            return true;
        }

        private List<EventDefinitionData> GetEligibleEvents(GameDate date, CharacterRelationships relationships,
            EventRuntimeState state)
        {
            var candidates = new List<EventDefinitionData>();
            if (!IsReady || !date.IsValid || relationships == null || state == null || state.HasPendingEvents ||
                state.Phase != StableGamePhase.Management)
                return candidates;

            int monthIndex = MonthIndex(date);
            foreach (EventDefinitionData definition in _catalog.events)
            {
                if (!string.Equals(definition.trigger, nameof(EventTriggerKind.Monthly), StringComparison.Ordinal) ||
                    state.IsOnCooldown(definition.eventId, monthIndex) ||
                    (ParseOnceScope(definition.onceScope) == EventOnceScope.PerGame && state.HasOccurredOnce(definition.eventId)) ||
                    !AreConditionsMet(definition, relationships))
                    continue;
                candidates.Add(definition);
            }
            candidates.Sort((left, right) =>
            {
                int priority = right.priority.CompareTo(left.priority);
                return priority != 0 ? priority : string.CompareOrdinal(left.eventId, right.eventId);
            });
            return candidates;
        }

        private bool AreConditionsMet(EventDefinitionData definition, CharacterRelationships relationships)
        {
            if (definition.conditions == null) return true;
            foreach (EventConditionData condition in definition.conditions)
            {
                if (string.Equals(condition.type, nameof(EventConditionKind.Always), StringComparison.Ordinal))
                    continue;
                if (string.Equals(condition.type, nameof(EventConditionKind.RelationshipAtLeast), StringComparison.Ordinal))
                {
                    if (!TryNpc(condition.targetCharacterId, out CharacterId id)) return false;
                    RelationshipResult relationship = relationships.TryGet(id);
                    if (!relationship.Succeeded || relationship.CurrentValue < condition.threshold) return false;
                }
                else
                    return false;
            }
            return true;
        }

        private static bool AreRequirementsMet(EventOptionData option, CharacterRelationships relationships,
            out string reason)
        {
            reason = string.Empty;
            if (option.requirements == null) return true;
            foreach (EventRequirementData requirement in option.requirements)
            {
                if (string.Equals(requirement.type, nameof(EventRequirementKind.Always), StringComparison.Ordinal))
                    continue;
                if (string.Equals(requirement.type, nameof(EventRequirementKind.RelationshipAtLeast), StringComparison.Ordinal))
                {
                    if (!TryNpc(requirement.targetCharacterId, out CharacterId id))
                    {
                        reason = "选项引用了未知人物。";
                        return false;
                    }
                    RelationshipResult relationship = relationships.TryGet(id);
                    if (!relationship.Succeeded || relationship.CurrentValue < requirement.minimumValue)
                    {
                        reason = requirement.reason;
                        return false;
                    }
                }
                else
                {
                    reason = "选项条件类型不受支持。";
                    return false;
                }
            }
            return true;
        }

        private static bool TryPreflightEffects(EventOptionData option, CharacterRelationships relationships,
            out List<RelationshipCommit> commits, out string error)
        {
            commits = new List<RelationshipCommit>();
            error = string.Empty;
            var projected = new Dictionary<CharacterId, int>();
            if (option.effects == null) return true;
            foreach (EventEffectData effect in option.effects)
            {
                if (!string.Equals(effect.type, nameof(EventEffectKind.RelationshipDelta), StringComparison.Ordinal) ||
                    !TryNpc(effect.targetCharacterId, out CharacterId target))
                {
                    error = "事件效果类型或目标人物无效。";
                    return false;
                }
                int current;
                if (!projected.TryGetValue(target, out current))
                {
                    RelationshipResult relationship = relationships.TryGet(target);
                    if (!relationship.Succeeded)
                    {
                        error = relationship.Error;
                        return false;
                    }
                    current = relationship.CurrentValue;
                }
                long next = (long)current + effect.delta;
                if (next < CharacterRelationships.MinimumValue || next > CharacterRelationships.MaximumValue)
                {
                    error = "关系效果超出允许范围，选择未提交。";
                    return false;
                }
                projected[target] = (int)next;
                commits.Add(new RelationshipCommit(target, effect.delta));
            }
            return true;
        }

        private bool TryFindOption(string eventId, string optionId,
            out EventDefinitionData definition, out EventOptionData selected)
        {
            selected = null;
            if (!TryGetEvent(eventId, out definition) || string.IsNullOrWhiteSpace(optionId))
                return false;
            foreach (EventOptionData option in definition.options)
                if (option != null && string.Equals(option.optionId, optionId, StringComparison.Ordinal))
                {
                    selected = option;
                    return true;
                }
            definition = null;
            return false;
        }

        private static bool ContainsOption(EventDefinitionData definition, string optionId)
        {
            if (definition == null || definition.options == null) return false;
            foreach (EventOptionData option in definition.options)
                if (option != null && string.Equals(option.optionId, optionId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        private string ResolveSummary(EventOptionData option)
        {
            if (!string.IsNullOrWhiteSpace(option.summaryTextId) && _texts.TryGetValue(option.summaryTextId, out EventTextEntry summary))
                return string.IsNullOrWhiteSpace(summary.body) ? summary.title : summary.body;
            return _texts.TryGetValue(option.textId, out EventTextEntry text) ? text.body : string.Empty;
        }

        private static bool TryNpc(string characterId, out CharacterId id)
        {
            return CharacterId.TryParse(characterId, out id) &&
                   CharacterCatalog.TryGet(id, out CharacterDefinition definition) &&
                   definition.Role == CharacterRole.Npc;
        }

        private static EventOnceScope ParseOnceScope(string value)
        {
            return string.Equals(value, nameof(EventOnceScope.PerGame), StringComparison.Ordinal)
                ? EventOnceScope.PerGame
                : EventOnceScope.Never;
        }

        private static int MonthIndex(GameDate date) => checked(date.Year * 12 + date.Month);

        private static EventScheduleResult ScheduleFailure(string message) =>
            new EventScheduleResult(false, string.Empty, message);

        private static EventChoiceResult ChoiceFailure(string eventId, string optionId, string resultId, string message) =>
            new EventChoiceResult(false, eventId, optionId, resultId, message, string.Empty);

        private readonly struct RelationshipCommit
        {
            public RelationshipCommit(CharacterId characterId, int delta)
            {
                CharacterId = characterId;
                Delta = delta;
            }
            public CharacterId CharacterId { get; }
            public int Delta { get; }
        }
    }
}
