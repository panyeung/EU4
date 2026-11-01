using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Read-only static assignment of NPCs to their towns.</summary>
    public static class TownNpcAssignments
    {
        private static readonly IReadOnlyList<CharacterId> KiyosuNpcs = Array.AsReadOnly(new[]
        {
            CharacterId.TachibanaSuzu,
            CharacterId.MoriGenzaemon
        });

        private static readonly IReadOnlyList<CharacterId> AtsutaNpcs = Array.AsReadOnly(new[]
        {
            CharacterId.KurodaIori
        });

        private static readonly IReadOnlyList<CharacterId> NagoyaNpcs = Array.AsReadOnly(new[]
        {
            CharacterId.HoshinoAya,
            CharacterId.SakaiJirobei
        });

        private static readonly IReadOnlyList<CharacterId> Empty = Array.AsReadOnly(Array.Empty<CharacterId>());

        public static IReadOnlyList<CharacterId> GetNpcs(string townId)
        {
            if (string.Equals(townId, LocationId.Kiyosu.Value, StringComparison.Ordinal)) return KiyosuNpcs;
            if (string.Equals(townId, LocationId.Atsuta.Value, StringComparison.Ordinal)) return AtsutaNpcs;
            if (string.Equals(townId, LocationId.Nagoya.Value, StringComparison.Ordinal)) return NagoyaNpcs;
            return Empty;
        }
    }
}
