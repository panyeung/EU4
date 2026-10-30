using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    public readonly struct DialogueStartResult
    {
        public DialogueStartResult(Guid id, string nodeId, bool succeeded, string error) { DialogueInstanceId = id; CurrentNodeId = nodeId ?? string.Empty; Succeeded = succeeded; Error = error ?? string.Empty; }
        public Guid DialogueInstanceId { get; }
        public string CurrentNodeId { get; }
        public bool Succeeded { get; }
        public string Error { get; }
    }
    public readonly struct DialogueSelectionResult
    {
        public DialogueSelectionResult(string nodeId, bool ended, bool succeeded, string error, bool effectApplied, RelationshipResult relationshipResult)
        { CurrentNodeId = nodeId ?? string.Empty; IsEnded = ended; Succeeded = succeeded; Error = error ?? string.Empty; RelationshipEffectApplied = effectApplied; RelationshipResult = relationshipResult; }
        public string CurrentNodeId { get; }
        public bool IsEnded { get; }
        public bool Succeeded { get; }
        public string Error { get; }
        public bool RelationshipEffectApplied { get; }
        public RelationshipResult RelationshipResult { get; }
    }
}
