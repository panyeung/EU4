using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    public readonly struct DialogueOptionId : IEquatable<DialogueOptionId>
    {
        private readonly string _value;
        public DialogueOptionId(string value) { _value = value; }
        public string Value => _value;
        public bool Equals(DialogueOptionId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is DialogueOptionId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(DialogueOptionId a, DialogueOptionId b) => a.Equals(b);
        public static bool operator !=(DialogueOptionId a, DialogueOptionId b) => !a.Equals(b);
    }

    public sealed class DialogueNode
    {
        private readonly IReadOnlyList<DialogueOption> _options;

        public DialogueNode(string id, string text, params DialogueOption[] options)
        {
            Id = id;
            Text = text ?? string.Empty;
            DialogueOption[] copiedOptions = options == null || options.Length == 0
                ? Array.Empty<DialogueOption>()
                : (DialogueOption[])options.Clone();
            _options = Array.AsReadOnly(copiedOptions);
        }

        public string Id { get; }
        public string Text { get; }
        public IReadOnlyList<DialogueOption> Options => _options;
    }

    public sealed class DialogueOption
    {
        public DialogueOption(DialogueOptionId id, string text, string targetNodeId, CharacterId effectTarget = default, int relationshipDelta = 0)
        { Id = id; Text = text ?? string.Empty; TargetNodeId = targetNodeId; EffectTarget = effectTarget; RelationshipDelta = relationshipDelta; }
        public DialogueOptionId Id { get; }
        public string Text { get; }
        public string TargetNodeId { get; }
        public CharacterId EffectTarget { get; }
        public int RelationshipDelta { get; }
    }

    public sealed class DialogueDefinition
    {
        private readonly Dictionary<string, DialogueNode> _nodes = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
        public DialogueDefinition(string startNodeId, params DialogueNode[] nodes)
        {
            StartNodeId = startNodeId;
            if (nodes != null) foreach (DialogueNode node in nodes) if (node != null && !string.IsNullOrEmpty(node.Id)) _nodes[node.Id] = node;
        }
        public string StartNodeId { get; }
        public bool TryGetNode(string id, out DialogueNode node) => _nodes.TryGetValue(id ?? string.Empty, out node);
    }

    public static class BranchingDialogueCatalog
    {
        public const string KurodaOpeningNode = "kuroda.opening", KurodaTrustNode = "kuroda.trust", KurodaFarewellNode = "kuroda.farewell";
        public static readonly DialogueOptionId KurodaOfferOption = new DialogueOptionId("offer"), KurodaDeclineOption = new DialogueOptionId("decline");
        private static readonly DialogueDefinition Kuroda = new DialogueDefinition(KurodaOpeningNode,
            new DialogueNode(KurodaOpeningNode, "A proposition.", new DialogueOption(KurodaOfferOption, "Accept", KurodaTrustNode, CharacterId.KurodaIori, 10), new DialogueOption(KurodaDeclineOption, "Decline", KurodaFarewellNode)),
            new DialogueNode(KurodaTrustNode, "Trust is earned."), new DialogueNode(KurodaFarewellNode, "Farewell."));
        public static bool TryGet(CharacterId characterId, out DialogueDefinition definition)
        { if (characterId == CharacterId.KurodaIori) { definition = Kuroda; return true; } definition = null; return false; }
    }
}
