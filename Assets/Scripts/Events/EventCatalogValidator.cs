using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Events
{
    public sealed class EventCatalogValidationResult
    {
        internal EventCatalogValidationResult(List<string> errors)
        {
            Errors = errors.AsReadOnly();
        }

        public bool Succeeded => Errors.Count == 0;
        public IReadOnlyList<string> Errors { get; }
        public string Message => Succeeded ? string.Empty : string.Join("\n", Errors);
    }

    /// <summary>Strict allowlist validator for untrusted JSON event and localization DTOs.</summary>
    public static class EventCatalogValidator
    {
        private const int MaximumSupportedMonthIndex = GameDate.MaxYear * 12 + 12;

        public static EventCatalogValidationResult Validate(EventCatalogData catalog, EventTextCatalog text)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("Event catalog is missing.");
                return new EventCatalogValidationResult(errors);
            }
            if (text == null)
                errors.Add("Event text catalog is missing.");
            if (catalog.contentVersion <= 0)
                errors.Add("Event catalog contentVersion must be explicit and positive.");
            if (text != null && (text.contentVersion <= 0 || text.contentVersion != catalog.contentVersion))
                errors.Add("Event and localization content versions must be explicit and match.");

            Dictionary<string, EventTextEntry> textById = ValidateText(text, errors);
            if (catalog.events == null || catalog.events.Length == 0)
            {
                errors.Add("Event catalog must contain at least one event.");
                return new EventCatalogValidationResult(errors);
            }

            var eventById = new Dictionary<string, EventDefinitionData>(StringComparer.Ordinal);
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            var branchPairOwners = new Dictionary<string, string>(StringComparer.Ordinal);
            var sourceReferences = new List<SourceBranchReference>();
            for (int i = 0; i < catalog.events.Length; i++)
            {
                EventDefinitionData definition = catalog.events[i];
                if (definition == null)
                {
                    errors.Add($"events[{i}] is null.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(definition.eventId))
                    errors.Add($"events[{i}] has an empty eventId.");
                else if (eventById.ContainsKey(definition.eventId))
                    errors.Add($"Duplicate eventId: {definition.eventId}.");
                else
                    eventById.Add(definition.eventId, definition);

                if (!TryEnum(definition.trigger, out EventTriggerKind _))
                    errors.Add($"Event {definition.eventId} has an unsupported trigger.");
                if (definition.weight <= 0)
                    errors.Add($"Event {definition.eventId} must have a positive weight.");
                if (definition.cooldownMonths < 0)
                    errors.Add($"Event {definition.eventId} has a negative cooldown.");
                else if (definition.cooldownMonths > int.MaxValue - MaximumSupportedMonthIndex)
                    errors.Add($"Event {definition.eventId} cooldown exceeds the supported date range.");
                if (!TryEnum(definition.onceScope, out EventOnceScope _))
                    errors.Add($"Event {definition.eventId} has an unsupported onceScope.");
                if (!string.IsNullOrWhiteSpace(definition.scenario) &&
                    !string.Equals(definition.scenario, EventScenarioIds.Sc010, StringComparison.Ordinal) &&
                    !string.Equals(definition.scenario, EventScenarioIds.Sc013, StringComparison.Ordinal) &&
                    !string.Equals(definition.scenario, EventScenarioIds.Sc024, StringComparison.Ordinal))
                    errors.Add($"Event {definition.eventId} has an unsupported scenario gate.");
                if (definition.sharedScenarios != null)
                {
                    var sharedScenarios = new HashSet<string>(StringComparer.Ordinal);
                    foreach (string sharedScenario in definition.sharedScenarios)
                    {
                        // SC-010 rows use the dedicated scenario field only; sharing is a
                        // SC-013/SC-024 multi-route mechanism and stays closed to `sc010`.
                        if (string.IsNullOrWhiteSpace(sharedScenario) ||
                            (!string.Equals(sharedScenario, EventScenarioIds.Sc013, StringComparison.Ordinal) &&
                             !string.Equals(sharedScenario, EventScenarioIds.Sc024, StringComparison.Ordinal)))
                        {
                            errors.Add($"Event {definition.eventId} has an unsupported shared scenario gate.");
                            continue;
                        }
                        if (string.Equals(sharedScenario, definition.scenario, StringComparison.Ordinal))
                            errors.Add($"Event {definition.eventId} repeats its primary scenario in sharedScenarios.");
                        else if (!sharedScenarios.Add(sharedScenario))
                            errors.Add($"Event {definition.eventId} repeats shared scenario {sharedScenario}.");
                    }
                }
                RequireText(textById, definition.textId, $"event {definition.eventId}", errors);

                if (definition.conditions != null)
                {
                    int historyConditionCount = 0;
                    for (int conditionIndex = 0; conditionIndex < definition.conditions.Length; conditionIndex++)
                    {
                        EventConditionData condition = definition.conditions[conditionIndex];
                        ValidateCondition(condition, definition, conditionIndex, errors);
                        if (condition == null ||
                            !TryEnum(condition.type, out EventConditionKind kind) ||
                            kind != EventConditionKind.EventOptionChosen)
                            continue;

                        historyConditionCount++;
                        if (historyConditionCount > 1)
                            errors.Add($"Event {definition.eventId} has more than one EventOptionChosen condition.");
                        if (string.IsNullOrWhiteSpace(condition.sourceEventId) ||
                            string.IsNullOrWhiteSpace(condition.sourceOptionId))
                            continue;

                        string pairKey = condition.sourceEventId + "\u001f" + condition.sourceOptionId;
                        if (branchPairOwners.TryGetValue(pairKey, out string owner))
                            errors.Add($"Events {owner} and {definition.eventId} both claim root branch " +
                                       $"{condition.sourceEventId}/{condition.sourceOptionId}.");
                        else
                            branchPairOwners.Add(pairKey, definition.eventId);

                        sourceReferences.Add(new SourceBranchReference(definition.eventId, definition.chainId,
                            condition.sourceEventId, condition.sourceOptionId, conditionIndex));
                    }
                }

                if (definition.options == null || definition.options.Length == 0)
                {
                    errors.Add($"Event {definition.eventId} must contain at least one option.");
                    continue;
                }
                for (int optionIndex = 0; optionIndex < definition.options.Length; optionIndex++)
                {
                    EventOptionData option = definition.options[optionIndex];
                    if (option == null)
                    {
                        errors.Add($"Event {definition.eventId} option {optionIndex} is null.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(option.optionId))
                        errors.Add($"Event {definition.eventId} has an empty optionId.");
                    else if (!optionIds.Add(option.optionId))
                        errors.Add($"Duplicate optionId: {option.optionId}.");
                    RequireText(textById, option.textId, $"option {option.optionId}", errors);
                    if (!string.IsNullOrWhiteSpace(option.summaryTextId))
                        RequireText(textById, option.summaryTextId, $"option summary {option.optionId}", errors);

                    if (option.requirements != null)
                    {
                        for (int requirementIndex = 0; requirementIndex < option.requirements.Length; requirementIndex++)
                            ValidateRequirement(option.requirements[requirementIndex], option.optionId, requirementIndex, errors);
                    }
                    if (option.effects != null)
                    {
                        for (int effectIndex = 0; effectIndex < option.effects.Length; effectIndex++)
                            ValidateEffect(option.effects[effectIndex], option.optionId, effectIndex, errors);
                    }
                }
            }

            var weightsByPriority = new Dictionary<int, long>();
            foreach (EventDefinitionData definition in catalog.events)
            {
                if (definition == null || definition.weight <= 0) continue;
                weightsByPriority.TryGetValue(definition.priority, out long currentWeight);
                long combinedWeight = currentWeight + definition.weight;
                weightsByPriority[definition.priority] = combinedWeight;
                if (combinedWeight > int.MaxValue)
                    errors.Add($"Priority {definition.priority} has too much total event weight.");
            }

            ValidateChains(catalog.chains, eventById, errors);
            foreach (EventDefinitionData definition in catalog.events)
            {
                if (definition != null && !string.IsNullOrWhiteSpace(definition.chainId) &&
                    !ContainsChainEvent(catalog.chains, definition.chainId, definition.eventId))
                    errors.Add($"Event {definition.eventId} references an unknown chain or is not linked by {definition.chainId}.");
            }
            ValidateSourceBranches(sourceReferences, eventById, errors);

            return new EventCatalogValidationResult(errors);
        }

        /// <summary>
        /// Source-reference checks run only after every event is registered, so a
        /// condition may point at an event defined later in the array.
        /// </summary>
        private static void ValidateSourceBranches(List<SourceBranchReference> references,
            Dictionary<string, EventDefinitionData> eventById, List<string> errors)
        {
            foreach (SourceBranchReference reference in references)
            {
                if (!eventById.TryGetValue(reference.SourceEventId, out EventDefinitionData source))
                {
                    errors.Add($"Event {reference.EventId} condition {reference.Index} references an unknown source event " +
                               $"{reference.SourceEventId}.");
                    continue;
                }
                if (string.Equals(reference.EventId, reference.SourceEventId, StringComparison.Ordinal))
                {
                    errors.Add($"Event {reference.EventId} condition {reference.Index} cannot reference itself.");
                    continue;
                }
                if (!ContainsOption(source, reference.SourceOptionId))
                {
                    errors.Add($"Event {reference.EventId} condition {reference.Index} references an unknown source option " +
                               $"{reference.SourceOptionId}.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(reference.ChainId) ||
                    !string.Equals(reference.ChainId, source.chainId, StringComparison.Ordinal))
                    errors.Add($"Event {reference.EventId} condition {reference.Index} must share the source event's chain " +
                               $"{source.chainId ?? "(null)"}.");
            }
        }

        private static Dictionary<string, EventTextEntry> ValidateText(EventTextCatalog text, List<string> errors)
        {
            var entries = new Dictionary<string, EventTextEntry>(StringComparer.Ordinal);
            if (text == null || text.entries == null)
            {
                errors.Add("Event text entries are missing.");
                return entries;
            }
            for (int i = 0; i < text.entries.Length; i++)
            {
                EventTextEntry entry = text.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.textId) ||
                    string.IsNullOrWhiteSpace(entry.title) || string.IsNullOrWhiteSpace(entry.body))
                {
                    errors.Add($"Event text entry {i} is incomplete.");
                    continue;
                }
                if (entries.ContainsKey(entry.textId))
                    errors.Add($"Duplicate textId: {entry.textId}.");
                else
                    entries.Add(entry.textId, entry);
            }
            return entries;
        }

        private static void ValidateCondition(EventConditionData condition, EventDefinitionData definition,
            int index, List<string> errors)
        {
            string eventId = definition.eventId;
            if (condition == null || !TryEnum(condition.type, out EventConditionKind kind))
            {
                errors.Add($"Event {eventId} condition {index} has an unsupported type.");
                return;
            }
            if (kind == EventConditionKind.RelationshipAtLeast)
            {
                if (!TryNpc(condition.targetCharacterId, out _))
                    errors.Add($"Event {eventId} condition {index} targets an unknown NPC.");
                if (condition.threshold < CharacterRelationships.MinimumValue ||
                    condition.threshold > CharacterRelationships.MaximumValue)
                    errors.Add($"Event {eventId} condition {index} has an out-of-range threshold.");
                return;
            }
            if (kind == EventConditionKind.EventOptionChosen)
            {
                if (string.IsNullOrWhiteSpace(condition.sourceEventId))
                    errors.Add($"Event {eventId} condition {index} has an empty sourceEventId.");
                if (string.IsNullOrWhiteSpace(condition.sourceOptionId))
                    errors.Add($"Event {eventId} condition {index} has an empty sourceOptionId.");
                if (!string.Equals(definition.onceScope, nameof(EventOnceScope.PerGame), StringComparison.Ordinal))
                    errors.Add($"Event {eventId} condition {index} requires onceScope PerGame.");
                return;
            }
            if (kind == EventConditionKind.SuccessionCountEquals ||
                kind == EventConditionKind.SettledSuccessionCountAtLeast)
            {
                if (condition.threshold < 0 || condition.threshold > 1000)
                    errors.Add($"Event {eventId} condition {index} has an out-of-range succession threshold.");
                if (!string.Equals(definition.trigger, nameof(EventTriggerKind.Monthly), StringComparison.Ordinal))
                    errors.Add($"Event {eventId} condition {index} is only allowed on Monthly events.");
                return;
            }
            if (kind == EventConditionKind.FamilyCreedIs)
            {
                if (!TryCreed(condition.familyCreedId, out _))
                    errors.Add($"Event {eventId} condition {index} has an invalid family creed ID.");
                if (!string.Equals(definition.scenario, EventScenarioIds.Sc024, StringComparison.Ordinal))
                    errors.Add($"Event {eventId} condition {index} is only allowed in the SC-024 scenario.");
                return;
            }
            if (kind == EventConditionKind.CampaignDateIs)
            {
                if (condition.year < GameDate.MinYear || condition.year > GameDate.MaxYear)
                    errors.Add($"Event {eventId} condition {index} has a year outside the supported game date range.");
                if (condition.month < 1 || condition.month > 12)
                    errors.Add($"Event {eventId} condition {index} has a month outside 1-12.");
                if (!string.Equals(definition.trigger, nameof(EventTriggerKind.Monthly), StringComparison.Ordinal))
                    errors.Add($"Event {eventId} condition {index} is only allowed on Monthly events.");
                if (!string.Equals(definition.scenario, EventScenarioIds.Sc010, StringComparison.Ordinal))
                    errors.Add($"Event {eventId} condition {index} is only allowed in the SC-010 scenario.");
                if (!string.Equals(definition.onceScope, nameof(EventOnceScope.PerGame), StringComparison.Ordinal))
                    errors.Add($"Event {eventId} condition {index} requires onceScope PerGame.");
            }
        }

        private static bool TryCreed(string value, out FamilyCreedKind creed)
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, false, out creed) &&
                Enum.IsDefined(typeof(FamilyCreedKind), creed) &&
                string.Equals(Enum.GetName(typeof(FamilyCreedKind), creed), value, StringComparison.Ordinal))
                return true;
            creed = default;
            return false;
        }

        private static void ValidateRequirement(EventRequirementData requirement, string optionId, int index, List<string> errors)
        {
            if (requirement == null || !TryEnum(requirement.type, out EventRequirementKind kind))
            {
                errors.Add($"Option {optionId} requirement {index} has an unsupported type.");
                return;
            }
            if (kind == EventRequirementKind.RelationshipAtLeast)
            {
                if (!TryNpc(requirement.targetCharacterId, out _))
                    errors.Add($"Option {optionId} requirement {index} targets an unknown NPC.");
                if (requirement.minimumValue < CharacterRelationships.MinimumValue ||
                    requirement.minimumValue > CharacterRelationships.MaximumValue)
                    errors.Add($"Option {optionId} requirement {index} has an out-of-range minimum.");
                if (string.IsNullOrWhiteSpace(requirement.reason))
                    errors.Add($"Option {optionId} requirement {index} has no disabled reason.");
            }
        }

        private static void ValidateEffect(EventEffectData effect, string optionId, int index, List<string> errors)
        {
            if (effect == null || !TryEnum(effect.type, out EventEffectKind kind))
            {
                errors.Add($"Option {optionId} effect {index} has an unsupported type.");
                return;
            }
            if (kind == EventEffectKind.RelationshipDelta)
            {
                if (!TryNpc(effect.targetCharacterId, out _))
                    errors.Add($"Option {optionId} effect {index} targets an unknown NPC.");
                if (!string.IsNullOrWhiteSpace(effect.targetPersonId))
                    errors.Add($"Option {optionId} effect {index} must not set a family target on an NPC effect.");
                if (!string.IsNullOrWhiteSpace(effect.familyCreedId))
                    errors.Add($"Option {optionId} effect {index} must not set a creed on an NPC effect.");
                if (effect.delta == 0 || effect.delta < CharacterRelationships.MinimumValue ||
                    effect.delta > CharacterRelationships.MaximumValue)
                    errors.Add($"Option {optionId} effect {index} has an invalid relationship delta.");
                return;
            }
            if (kind == EventEffectKind.FamilyRelationshipDelta)
            {
                if (string.IsNullOrWhiteSpace(effect.targetPersonId) ||
                    !PersonId.TryParse(effect.targetPersonId, out _))
                    errors.Add($"Option {optionId} effect {index} targets an invalid family person ID.");
                if (!string.IsNullOrWhiteSpace(effect.targetCharacterId))
                    errors.Add($"Option {optionId} effect {index} must not set an NPC target on a family effect.");
                if (!string.IsNullOrWhiteSpace(effect.familyCreedId))
                    errors.Add($"Option {optionId} effect {index} must not set a creed on a family effect.");
                if (effect.delta == 0 || effect.delta < FamilyRelationships.MinimumValue ||
                    effect.delta > FamilyRelationships.MaximumValue)
                    errors.Add($"Option {optionId} effect {index} has an invalid family relationship delta.");
                return;
            }
            if (kind == EventEffectKind.SetFamilyCreed)
            {
                if (!TryCreed(effect.familyCreedId, out _))
                    errors.Add($"Option {optionId} effect {index} has an invalid family creed ID.");
                if (!string.IsNullOrWhiteSpace(effect.targetCharacterId) ||
                    !string.IsNullOrWhiteSpace(effect.targetPersonId))
                    errors.Add($"Option {optionId} effect {index} must not set a target on a creed effect.");
                if (effect.delta != 0)
                    errors.Add($"Option {optionId} effect {index} must not set a delta on a creed effect.");
            }
        }

        private static void ValidateChains(EventChainData[] chains,
            Dictionary<string, EventDefinitionData> eventById, List<string> errors)
        {
            if (chains == null) return;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < chains.Length; i++)
            {
                EventChainData chain = chains[i];
                if (chain == null || string.IsNullOrWhiteSpace(chain.chainId))
                {
                    errors.Add($"chains[{i}] has an empty chainId.");
                    continue;
                }
                if (!seen.Add(chain.chainId))
                    errors.Add($"Duplicate chainId: {chain.chainId}.");
                if (chain.linkedEventIds == null || chain.linkedEventIds.Length == 0)
                {
                    errors.Add($"Chain {chain.chainId} must reference at least one event.");
                    continue;
                }
                var linked = new HashSet<string>(StringComparer.Ordinal);
                foreach (string eventId in chain.linkedEventIds)
                {
                    if (string.IsNullOrWhiteSpace(eventId) || !eventById.ContainsKey(eventId))
                        errors.Add($"Chain {chain.chainId} references unknown event {eventId ?? "(null)"}.");
                    else if (!linked.Add(eventId))
                        errors.Add($"Chain {chain.chainId} repeats event {eventId}.");
                }
            }
        }

        private static bool ContainsChainEvent(EventChainData[] chains, string chainId, string eventId)
        {
            if (chains == null) return false;
            for (int i = 0; i < chains.Length; i++)
            {
                EventChainData chain = chains[i];
                if (chain == null || !string.Equals(chain.chainId, chainId, StringComparison.Ordinal) ||
                    chain.linkedEventIds == null)
                    continue;
                for (int linkedIndex = 0; linkedIndex < chain.linkedEventIds.Length; linkedIndex++)
                    if (string.Equals(chain.linkedEventIds[linkedIndex], eventId, StringComparison.Ordinal))
                        return true;
            }
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

        private static void RequireText(Dictionary<string, EventTextEntry> text, string textId,
            string owner, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(textId) || !text.ContainsKey(textId))
                errors.Add($"{owner} references missing textId {textId ?? "(null)"}.");
        }

        private static bool TryNpc(string text, out CharacterId id)
        {
            return CharacterId.TryParse(text, out id) && CharacterCatalog.TryGet(id, out CharacterDefinition definition) &&
                   definition.Role == CharacterRole.Npc;
        }

        private static bool TryEnum<T>(string value, out T parsed) where T : struct
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, false, out parsed) &&
                Enum.IsDefined(typeof(T), parsed) &&
                string.Equals(Enum.GetName(typeof(T), parsed), value, StringComparison.Ordinal))
                return true;
            parsed = default;
            return false;
        }

        private readonly struct SourceBranchReference
        {
            public SourceBranchReference(string eventId, string chainId, string sourceEventId,
                string sourceOptionId, int index)
            {
                EventId = eventId;
                ChainId = chainId;
                SourceEventId = sourceEventId;
                SourceOptionId = sourceOptionId;
                Index = index;
            }

            public string EventId { get; }
            public string ChainId { get; }
            public string SourceEventId { get; }
            public string SourceOptionId { get; }
            public int Index { get; }
        }
    }
}
