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
            : this(succeeded, eventId, optionId, resultId, message, feedback, null, null)
        {
        }

        public EventChoiceResult(bool succeeded, string eventId, string optionId, string resultId,
            string message, string feedback, IReadOnlyList<RelationshipResult> appliedRelationships)
            : this(succeeded, eventId, optionId, resultId, message, feedback, appliedRelationships, null)
        {
        }

        public EventChoiceResult(bool succeeded, string eventId, string optionId, string resultId,
            string message, string feedback, IReadOnlyList<RelationshipResult> appliedRelationships,
            IReadOnlyList<FamilyRelationshipResult> appliedFamilyRelationships)
        {
            Succeeded = succeeded;
            EventId = eventId ?? string.Empty;
            OptionId = optionId ?? string.Empty;
            ResultId = resultId ?? string.Empty;
            Message = message ?? string.Empty;
            Feedback = feedback ?? string.Empty;
            AppliedRelationships = CopyRelationships(appliedRelationships);
            AppliedFamilyRelationships = CopyFamilyRelationships(appliedFamilyRelationships);
        }

        private static IReadOnlyList<RelationshipResult> CopyRelationships(
            IReadOnlyList<RelationshipResult> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<RelationshipResult>();
            var copy = new RelationshipResult[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<FamilyRelationshipResult> CopyFamilyRelationships(
            IReadOnlyList<FamilyRelationshipResult> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<FamilyRelationshipResult>();
            var copy = new FamilyRelationshipResult[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }

        public bool Succeeded { get; }
        public string EventId { get; }
        public string OptionId { get; }
        public string ResultId { get; }
        public string Message { get; }
        public string Feedback { get; }
        /// <summary>
        /// Relationship values actually applied and committed. Empty on every failure
        /// and whenever a delta produced no real change, so callers can never record a
        /// rolled-back or no-op effect.
        /// </summary>
        public IReadOnlyList<RelationshipResult> AppliedRelationships { get; }

        /// <summary>Current-head-to-family-member values actually applied and committed.</summary>
        public IReadOnlyList<FamilyRelationshipResult> AppliedFamilyRelationships { get; }
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
            EventRuntimeState state, EventRuleContext context = default)
        {
            List<EventDefinitionData> candidates = GetEligibleEvents(date, relationships, state, context);
            return candidates.Select(candidate => candidate.eventId).ToArray();
        }

        public EventScheduleResult ScheduleMonthly(GameDate date, CharacterRelationships relationships,
            DeterministicRandomStream eventRandom, EventRuntimeState state, EventRuleContext context = default)
        {
            if (!IsReady)
                return ScheduleFailure(ValidationMessage);
            if (!date.IsValid || relationships == null || eventRandom == null || state == null)
                return ScheduleFailure("Event scheduling received invalid runtime inputs.");
            if (state.HasPendingEvents || state.Phase != StableGamePhase.Management)
                return ScheduleFailure("A pending event must be resolved before another month is processed.");

            List<EventDefinitionData> candidates = GetEligibleEvents(date, relationships, state, context);
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
            GameDate currentDate, CharacterRelationships relationships, EventRuntimeState state,
            EventRuleContext context = default, FamilyRelationships familyRelationships = null)
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
            if (!AreConditionsMet(definition, relationships, state, context, false))
                return ChoiceFailure(eventId, optionId, committedResultId, "事件条件已经失效，状态保持不变。");
            if (!AreRequirementsMet(option, relationships, out string requirementReason))
                return ChoiceFailure(eventId, optionId, committedResultId,
                    string.IsNullOrWhiteSpace(requirementReason) ? "当前不满足此选项条件。" : requirementReason);

            if (!TryPreflightEffects(option, relationships, familyRelationships, context,
                    out List<EventEffectCommit> effects, out string effectError))
                return ChoiceFailure(eventId, optionId, committedResultId, effectError);

            var appliedNpc = new List<RelationshipCommit>();
            var appliedFamily = new List<FamilyRelationshipCommit>();
            var appliedRelationships = new List<RelationshipResult>();
            var appliedFamilyRelationships = new List<FamilyRelationshipResult>();
            foreach (EventEffectCommit effect in effects)
            {
                if (effect.IsFamily)
                {
                    FamilyRelationshipResult change = familyRelationships.TryChange(
                        effect.HeadId, effect.MemberId, effect.Delta);
                    if (!change.Succeeded)
                    {
                        RollbackEffects(relationships, familyRelationships, appliedNpc, appliedFamily);
                        return ChoiceFailure(eventId, optionId, committedResultId,
                            "家庭关系效果提交失败，已恢复此前效果。");
                    }
                    appliedFamily.Add(new FamilyRelationshipCommit(
                        effect.HeadId, effect.MemberId, effect.Delta));
                    if (change.PreviousValue != change.CurrentValue)
                        appliedFamilyRelationships.Add(change);
                }
                else
                {
                    RelationshipResult change = relationships.TryChange(effect.CharacterId, effect.Delta);
                    if (!change.Succeeded)
                    {
                        RollbackEffects(relationships, familyRelationships, appliedNpc, appliedFamily);
                        return ChoiceFailure(eventId, optionId, committedResultId,
                            "关系效果提交失败，已恢复此前效果。");
                    }
                    appliedNpc.Add(new RelationshipCommit(effect.CharacterId, effect.Delta));
                    if (change.PreviousValue != change.CurrentValue)
                        appliedRelationships.Add(change);
                }
            }

            EventOnceScope onceScope = ParseOnceScope(definition.onceScope);
            string feedback = ResolveSummary(option);
            int monthIndex = MonthIndex(currentDate);
            if (!state.TryCommit(eventId, optionId, committedResultId, monthIndex,
                    definition.cooldownMonths, onceScope, feedback, currentDate.Year, currentDate.Month))
            {
                // The queue and result were preflighted above; this is defensive for a
                // stale caller, and rolls back every applied effect before returning.
                RollbackEffects(relationships, familyRelationships, appliedNpc, appliedFamily);
                return ChoiceFailure(eventId, optionId, committedResultId, "事件队列已变化，结果未提交。");
            }

            return new EventChoiceResult(true, eventId, optionId, committedResultId, string.Empty,
                feedback, appliedRelationships, appliedFamilyRelationships);
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
            return TryRestoreState(save, currentDate, EventRuleContext.Default, out state, out error);
        }

        /// <summary>
        /// Restores a snapshot and rejects events that belong to a different scenario,
        /// so an SC-013 save cannot be loaded into an SC-010 run (or the reverse).
        /// </summary>
        public bool TryRestoreState(EventRuntimeSaveData save, GameDate? currentDate,
            EventRuleContext context, out EventRuntimeState state, out string error)
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
                if (!_events.TryGetValue(pendingId, out EventDefinitionData pending) ||
                    !ScenarioMatches(pending, context))
                {
                    error = "Saved pending event is not present in the active scenario: " + pendingId;
                    return false;
                }
            foreach (EventCooldownSaveEntry cooldown in save.cooldowns)
                if (!_events.TryGetValue(cooldown.eventId, out EventDefinitionData cooled) ||
                    !ScenarioMatches(cooled, context))
                {
                    error = "Saved cooldown event is not present in the active scenario: " + cooldown.eventId;
                    return false;
                }
            foreach (string onceEventId in save.onceEventIds)
                if (!_events.TryGetValue(onceEventId, out EventDefinitionData once) ||
                    !ScenarioMatches(once, context))
                {
                    error = "Saved once-only event is not present in the active scenario: " + onceEventId;
                    return false;
                }
            foreach (EventHistorySaveEntry history in save.history)
            {
                if (!_events.TryGetValue(history.eventId, out EventDefinitionData definition) ||
                    !ContainsOption(definition, history.optionId) ||
                    !ScenarioMatches(definition, context))
                {
                    error = "Saved event history refers to an unknown event, option, or scenario.";
                    return false;
                }
            }

            state = restored;
            error = string.Empty;
            return true;
        }

        private List<EventDefinitionData> GetEligibleEvents(GameDate date, CharacterRelationships relationships,
            EventRuntimeState state, EventRuleContext context)
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
                    !AreConditionsMet(definition, relationships, state, context, true))
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

        /// <summary>
        /// Evaluates allowlisted event conditions. History conditions are re-checked at
        /// commit because committed history never changes. Succession-count conditions are
        /// offer-time gates only (<paramref name="evaluateSuccessionGates"/> false at
        /// commit), so a stable pending event stays resolvable after a later head change.
        /// </summary>
        private bool AreConditionsMet(EventDefinitionData definition, CharacterRelationships relationships,
            EventRuntimeState state, EventRuleContext context, bool evaluateSuccessionGates)
        {
            if (!ScenarioMatches(definition, context)) return false;
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
                else if (string.Equals(condition.type, nameof(EventConditionKind.EventOptionChosen), StringComparison.Ordinal))
                {
                    if (!HasLastChosenOption(state, condition.sourceEventId, condition.sourceOptionId)) return false;
                }
                else if (string.Equals(condition.type, nameof(EventConditionKind.SuccessionCountEquals), StringComparison.Ordinal))
                {
                    if (evaluateSuccessionGates && context.SuccessionCount != condition.threshold) return false;
                }
                else if (string.Equals(condition.type, nameof(EventConditionKind.SettledSuccessionCountAtLeast), StringComparison.Ordinal))
                {
                    if (evaluateSuccessionGates && context.SettledSuccessionCount < condition.threshold) return false;
                }
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// True only when the last committed history entry for the source event used the
        /// requested option. History is append-only, so "last" is deterministic.
        /// </summary>
        private static bool HasLastChosenOption(EventRuntimeState state, string sourceEventId, string sourceOptionId)
        {
            if (state == null || string.IsNullOrWhiteSpace(sourceEventId) ||
                string.IsNullOrWhiteSpace(sourceOptionId))
                return false;
            IReadOnlyList<EventHistorySaveEntry> history = state.History;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                EventHistorySaveEntry entry = history[i];
                if (entry == null || !string.Equals(entry.eventId, sourceEventId, StringComparison.Ordinal))
                    continue;
                return string.Equals(entry.optionId, sourceOptionId, StringComparison.Ordinal);
            }
            return false;
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
            FamilyRelationships familyRelationships, EventRuleContext context,
            out List<EventEffectCommit> commits, out string error)
        {
            commits = new List<EventEffectCommit>();
            error = string.Empty;
            var projectedNpc = new Dictionary<CharacterId, int>();
            var projectedFamily = new Dictionary<FamilyRelationshipKey, int>();
            if (option.effects == null) return true;
            foreach (EventEffectData effect in option.effects)
            {
                if (string.Equals(effect.type, nameof(EventEffectKind.RelationshipDelta), StringComparison.Ordinal))
                {
                    if (!TryNpc(effect.targetCharacterId, out CharacterId target))
                    {
                        error = "事件效果类型或目标人物无效。";
                        return false;
                    }
                    int current;
                    if (!projectedNpc.TryGetValue(target, out current))
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
                    projectedNpc[target] = (int)next;
                    commits.Add(EventEffectCommit.Npc(target, effect.delta));
                    continue;
                }

                if (string.Equals(effect.type, nameof(EventEffectKind.FamilyRelationshipDelta),
                        StringComparison.Ordinal))
                {
                    if (!TryFamilyTarget(effect, context, out PersonId target, out error))
                        return false;
                    if (familyRelationships == null)
                    {
                        error = "家庭关系状态不可用。";
                        return false;
                    }

                    var key = new FamilyRelationshipKey(context.HeadPersonId, target);
                    int current;
                    if (!projectedFamily.TryGetValue(key, out current))
                    {
                        FamilyRelationshipResult relationship =
                            familyRelationships.TryGet(context.HeadPersonId, target);
                        if (!relationship.Succeeded)
                        {
                            error = relationship.Error;
                            return false;
                        }
                        current = relationship.CurrentValue;
                    }
                    // Family deltas clamp per the approved plan; NPC deltas keep their
                    // existing overflow-rejection behavior. The commit carries the actual
                    // applied delta so rollback and the chronicle use exact before/after
                    // values rather than the requested amount.
                    long requested = (long)current + effect.delta;
                    int next = requested < FamilyRelationships.MinimumValue
                        ? FamilyRelationships.MinimumValue
                        : requested > FamilyRelationships.MaximumValue
                            ? FamilyRelationships.MaximumValue
                            : (int)requested;
                    projectedFamily[key] = next;
                    commits.Add(EventEffectCommit.Family(context.HeadPersonId, target, next - current));
                    continue;
                }

                error = "事件效果类型或目标人物无效。";
                return false;
            }
            return true;
        }

        private static bool TryFamilyTarget(EventEffectData effect, EventRuleContext context,
            out PersonId target, out string error)
        {
            target = default;
            error = string.Empty;
            if (context.HeadPersonId.IsEmpty)
            {
                error = "家庭关系效果缺少当前家主。";
                return false;
            }
            if (context.FamilyMemberIds == null ||
                !Contains(context.FamilyMemberIds, context.HeadPersonId))
            {
                error = "当前家主不在家庭成员名册中。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(effect.targetPersonId) ||
                !PersonId.TryParse(effect.targetPersonId, out target) || target.IsEmpty)
            {
                error = "家庭关系效果目标人物 ID 无效。";
                return false;
            }
            if (target == context.HeadPersonId)
            {
                error = "家庭关系效果不能指向家主本人。";
                return false;
            }
            if (context.FamilyMemberIds == null || !Contains(context.FamilyMemberIds, target))
            {
                error = "家庭关系效果目标不是当前家庭成员。";
                return false;
            }
            return true;
        }

        private static bool Contains(PersonId[] ids, PersonId target)
        {
            if (ids == null) return false;
            for (int i = 0; i < ids.Length; i++)
                if (ids[i] == target)
                    return true;
            return false;
        }

        private static void RollbackEffects(CharacterRelationships relationships,
            FamilyRelationships familyRelationships, List<RelationshipCommit> appliedNpc,
            List<FamilyRelationshipCommit> appliedFamily)
        {
            for (int i = appliedFamily.Count - 1; i >= 0; i--)
            {
                FamilyRelationshipCommit rollback = appliedFamily[i];
                familyRelationships.TryChange(rollback.HeadId, rollback.MemberId, -rollback.Delta);
            }
            for (int i = appliedNpc.Count - 1; i >= 0; i--)
            {
                RelationshipCommit rollback = appliedNpc[i];
                relationships.TryChange(rollback.CharacterId, -rollback.Delta);
            }
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

        /// <summary>
        /// Scenario-neutral rows are always eligible. A tagged row is eligible only in
        /// exactly that scenario, so SC-013 events never leak into the SC-010 samples.
        /// </summary>
        private static bool ScenarioMatches(EventDefinitionData definition, EventRuleContext context)
        {
            if (string.IsNullOrWhiteSpace(definition.scenario))
                return true;
            return string.Equals(definition.scenario, context.ScenarioId, StringComparison.Ordinal);
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

        private readonly struct FamilyRelationshipCommit
        {
            public FamilyRelationshipCommit(PersonId headId, PersonId memberId, int delta)
            {
                HeadId = headId;
                MemberId = memberId;
                Delta = delta;
            }
            public PersonId HeadId { get; }
            public PersonId MemberId { get; }
            public int Delta { get; }
        }

        private readonly struct EventEffectCommit
        {
            private EventEffectCommit(CharacterId characterId, PersonId headId, PersonId memberId,
                int delta, bool isFamily)
            {
                CharacterId = characterId;
                HeadId = headId;
                MemberId = memberId;
                Delta = delta;
                IsFamily = isFamily;
            }

            public static EventEffectCommit Npc(CharacterId characterId, int delta) =>
                new EventEffectCommit(characterId, default, default, delta, false);

            public static EventEffectCommit Family(PersonId headId, PersonId memberId, int delta) =>
                new EventEffectCommit(default, headId, memberId, delta, true);

            public CharacterId CharacterId { get; }
            public PersonId HeadId { get; }
            public PersonId MemberId { get; }
            public int Delta { get; }
            public bool IsFamily { get; }
        }
    }
}
