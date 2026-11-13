using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>One fixed industry definition (GDD 5.2). Values are read-only rules, not save state.</summary>
    public readonly struct IndustryDefinition
    {
        public IndustryDefinition(string id, string displayName, int workUnitsRequired, int capacityPosts,
            int upkeepFunds, int upkeepSupplies, int outputFunds, int outputSupplies,
            int outputIntelligence, bool maintainsFromOutput, int outputFood = 0)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            WorkUnitsRequired = workUnitsRequired;
            CapacityPosts = capacityPosts;
            UpkeepFunds = upkeepFunds;
            UpkeepSupplies = upkeepSupplies;
            OutputFunds = outputFunds;
            OutputSupplies = outputSupplies;
            OutputIntelligence = outputIntelligence;
            MaintainsFromOutput = maintainsFromOutput;
            OutputFood = outputFood;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int WorkUnitsRequired { get; }
        public int CapacityPosts { get; }
        public int UpkeepFunds { get; }
        public int UpkeepSupplies { get; }
        public int OutputFunds { get; }
        public int OutputSupplies { get; }
        public int OutputIntelligence { get; }
        /// <summary>Approved M08B food harvest; only the farm produces food, all others zero.</summary>
        public int OutputFood { get; }
        /// <summary>Farm retains upkeep from its own gross harvest instead of paying from stock first.</summary>
        public bool MaintainsFromOutput { get; }
    }

    /// <summary>
    /// The two approved SC-019 industries, ordered by stable ordinal ID:
    /// <c>industry.farm</c> then <c>industry.trading-house</c>.
    /// </summary>
    public static class IndustryCatalog
    {
        public const string FarmId = "industry.farm";
        public const string TradingHouseId = "industry.trading-house";

        private static readonly IndustryDefinition[] Definitions =
        {
            new IndustryDefinition(FarmId, "农庄", 3, 1, 0, 1, 2, 4, 0, true, 4),
            new IndustryDefinition(TradingHouseId, "商馆", 3, 1, 6, 2, 9, 0, 1, false)
        };

        private static readonly IReadOnlyList<IndustryDefinition> ReadOnlyDefinitions =
            Array.AsReadOnly(Definitions);

        public static IReadOnlyList<IndustryDefinition> All => ReadOnlyDefinitions;

        public static bool TryGet(string industryId, out IndustryDefinition definition)
        {
            if (!string.IsNullOrEmpty(industryId))
            {
                for (int i = 0; i < Definitions.Length; i++)
                {
                    if (string.Equals(Definitions[i].Id, industryId, StringComparison.Ordinal))
                    {
                        definition = Definitions[i];
                        return true;
                    }
                }
            }

            definition = default;
            return false;
        }
    }
}
