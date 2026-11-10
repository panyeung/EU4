using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Explicit, stable-ID gender catalog for every authored scenario person. v12 save
    /// migration uses this table to resolve a gender only for IDs that were authored by
    /// the project; it is never used to guess a gender from a display name or from a
    /// kinship fact. Newly generated people (births) receive their gender directly.
    /// </summary>
    public static class PersonGenderCatalog
    {
        private static readonly Dictionary<string, PersonGender> Entries =
            new Dictionary<string, PersonGender>(StringComparer.Ordinal)
            {
                // SC-010 standard and crisis samples.
                { "person.akechi.harunobu", PersonGender.Male },
                { "person.akechi.chie", PersonGender.Female },
                { "person.akechi.masatsuna", PersonGender.Male },
                { "person.akechi.tokihime", PersonGender.Female },
                { "person.akechi.nobumasa", PersonGender.Male },
                { "person.akechi.kageyu", PersonGender.Male },
                { "person.retainer.sakon", PersonGender.Male },

                // SC-013 fixed three-generation roster and the SC-024-only child.
                { "person.sc013.harunobu", PersonGender.Male },
                { "person.sc013.chie", PersonGender.Female },
                { "person.sc013.masatsuna", PersonGender.Male },
                { "person.sc013.nobumasa", PersonGender.Male },
                { "person.sc013.tokihime", PersonGender.Female },
                { "person.sc013.sakon", PersonGender.Male },
                { "person.sc024.nobumasa-heir", PersonGender.Male },

                // SC-020 child-education demonstration.
                { "person.sc020.head", PersonGender.Male },
                { "person.sc020.spouse", PersonGender.Female },
                { "person.sc020.child", PersonGender.Female },

                // SC-021 family-research demonstration.
                { "person.sc021.head", PersonGender.Male },
                { "person.sc021.spouse", PersonGender.Female },
                { "person.sc021.heir", PersonGender.Male },

                // SC-022 marriage and adoption demonstration.
                { "person.sc022.head", PersonGender.Male },
                { "person.sc022.spouse", PersonGender.Female },
                { "person.sc022.child", PersonGender.Female },
                { "person.sc022.collateral", PersonGender.Male },
                { "person.sc022.candidate.saho", PersonGender.Female },
                { "person.sc022.candidate.chihiro", PersonGender.Female },
                { "person.sc022.candidate.ume", PersonGender.Female },
                { "person.sc022.candidate.sogen", PersonGender.Male },
                { "person.sc022.candidate.yoichi", PersonGender.Male },
                { "person.sc022.candidate.senmatsu", PersonGender.Male },
                { "person.sc022.candidate.sakichi", PersonGender.Male },

                // SC-023 seclusion demonstration.
                { "person.sc023.elder", PersonGender.Male },
                { "person.sc023.heir", PersonGender.Male },
                { "person.sc023.child", PersonGender.Female }
            };

        /// <summary>Number of authored stable IDs with an explicit gender.</summary>
        public static int Count => Entries.Count;

        /// <summary>True only when the exact stable ID is an authored scenario person.</summary>
        public static bool TryGet(PersonId personId, out PersonGender gender)
        {
            gender = PersonGender.Unspecified;
            return !personId.IsEmpty &&
                   Entries.TryGetValue(personId.Value, out gender);
        }

        /// <summary>
        /// Gender for an authored ID, or <see cref="PersonGender.Unspecified"/> for an
        /// unknown or generated ID. Scenario construction uses this helper so a fixed
        /// roster can never silently disagree with the migration catalog.
        /// </summary>
        public static PersonGender GenderFor(PersonId personId)
        {
            return TryGet(personId, out PersonGender gender)
                ? gender
                : PersonGender.Unspecified;
        }
    }
}
