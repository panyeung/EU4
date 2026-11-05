using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>One authored, deterministic family research project.</summary>
    public readonly struct FamilyResearchDefinition
    {
        public FamilyResearchDefinition(string id, string techniqueId, string projectDisplayName,
            string techniqueDisplayName, int requiredProgress, int monthlyFundsCost,
            int monthlySuppliesCost, int monthlyProgress)
        {
            Id = id ?? string.Empty;
            TechniqueId = techniqueId ?? string.Empty;
            ProjectDisplayName = projectDisplayName ?? string.Empty;
            TechniqueDisplayName = techniqueDisplayName ?? string.Empty;
            RequiredProgress = requiredProgress;
            MonthlyFundsCost = monthlyFundsCost;
            MonthlySuppliesCost = monthlySuppliesCost;
            MonthlyProgress = monthlyProgress;
        }

        public string Id { get; }
        public string TechniqueId { get; }
        public string ProjectDisplayName { get; }
        public string TechniqueDisplayName { get; }
        public int RequiredProgress { get; }
        public int MonthlyFundsCost { get; }
        public int MonthlySuppliesCost { get; }
        public int MonthlyProgress { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(TechniqueId) &&
            RequiredProgress > 0 && MonthlyFundsCost >= 0 && MonthlySuppliesCost >= 0 &&
            MonthlyProgress > 0;
    }

    /// <summary>
    /// Stable SC-021 research content. The first slice authors exactly one original
    /// technique so progress, cost and completion remain fully deterministic. This is
    /// pure data; no ScriptableObject, package or external code is used.
    /// </summary>
    public static class FamilyResearchCatalog
    {
        public const string SmokeMixingResearchId = "research.smoke-mixing";
        public const string SmokeMixingTechniqueId = "technique.smoke-mixing";
        public const string SmokeMixingProjectDisplayName = "烟幕调制术研究";
        public const string SmokeMixingTechniqueDisplayName = "烟幕调制术";
        public const int SmokeMixingRequiredProgress = 2;
        public const int MonthlyFundsCost = 1;
        public const int MonthlySuppliesCost = 1;
        public const int MonthlyProgress = 1;

        private static readonly FamilyResearchDefinition[] Definitions =
        {
            new FamilyResearchDefinition(
                SmokeMixingResearchId,
                SmokeMixingTechniqueId,
                SmokeMixingProjectDisplayName,
                SmokeMixingTechniqueDisplayName,
                SmokeMixingRequiredProgress,
                MonthlyFundsCost,
                MonthlySuppliesCost,
                MonthlyProgress)
        };

        /// <summary>Stable, ordinal-ordered catalog snapshot.</summary>
        public static IReadOnlyList<FamilyResearchDefinition> All { get; } =
            Array.AsReadOnly(Definitions);

        public static bool TryGet(string researchId, out FamilyResearchDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].Id, researchId, StringComparison.Ordinal))
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static bool TryGetByTechnique(string techniqueId, out FamilyResearchDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].TechniqueId, techniqueId, StringComparison.Ordinal))
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static bool IsKnownTechniqueId(string techniqueId) =>
            TryGetByTechnique(techniqueId, out _);

        public static string TechniqueDisplayName(string techniqueId) =>
            TryGetByTechnique(techniqueId, out FamilyResearchDefinition definition)
                ? definition.TechniqueDisplayName
                : techniqueId ?? string.Empty;

        /// <summary>Highest authored requirement; used by save validation for progress.</summary>
        public static int MaximumRequiredProgress
        {
            get
            {
                int maximum = 0;
                for (int i = 0; i < Definitions.Length; i++)
                    if (Definitions[i].RequiredProgress > maximum)
                        maximum = Definitions[i].RequiredProgress;
                return maximum;
            }
        }
    }
}
