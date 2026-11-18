using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Stable phases of the SC-010 succession slice. A phase is persisted exactly as
    /// an integer; every transition is owned by <see cref="SuccessionService"/>.
    /// </summary>
    public enum SuccessionPhase
    {
        /// <summary>No succession decision is pending; the current head is in office.</summary>
        None = 0,
        /// <summary>Retirement is pending and the player must choose a lawful successor.</summary>
        AwaitingSuccessionChoice = 1,
        /// <summary>A minor successor is selected and the player must choose an adult regent.</summary>
        AwaitingRegentChoice = 2,
        /// <summary>The minor head is in office under a regent until the adulthood threshold.</summary>
        Regency = 3,
        /// <summary>No lawful successor exists; one crisis adoption/rebuild decision is offered.</summary>
        CrisisDecision = 4,
        /// <summary>The family ended because no lawful continuation was accepted.</summary>
        Extinct = 5
    }

    /// <summary>Explicit result codes for every succession request; no hidden random outcome.</summary>
    public enum SuccessionOutcome
    {
        Succeeded = 0,
        AlreadyPending = 1,
        NotPending = 2,
        InvalidHead = 3,
        InvalidCandidate = 4,
        CandidateNotEligible = 5,
        RegentRequired = 6,
        InvalidRegent = 7,
        DesignatedHeirInvalid = 8,
        NoCandidates = 9,
        CrisisUnavailable = 10,
        NotDue = 11,
        Extinction = 12,
        InvalidState = 13,
        RegentUnavailable = 14,
        /// <summary>
        /// A queued event must be chosen before a command can hand the head to a new person;
        /// otherwise the event's own options would stop addressing the current head.
        /// </summary>
        BlockedByPendingEvent = 15
    }

    /// <summary>
    /// Succession-only lifecycle facts that SC-003 deliberately does not own yet.
    /// Kept separate from the SC-003 value types so those files remain unchanged.
    /// </summary>
    public readonly struct SuccessionPersonLife
    {
        public SuccessionPersonLife(int birthYear, bool alive, bool retired)
        {
            BirthYear = birthYear;
            Alive = alive;
            Retired = retired;
        }

        public int BirthYear { get; }
        public bool Alive { get; }
        /// <summary>A retired head may advise the family but is not a candidate again in this slice.</summary>
        public bool Retired { get; }

        public int AgeAt(GameDate date) => date.Year - BirthYear;

        public bool IsAdultAt(GameDate date, int adulthoodAge) => AgeAt(date) >= adulthoodAge;
    }

    /// <summary>Deterministic view of one living, qualified successor candidate.</summary>
    public readonly struct SuccessionCandidate
    {
        public SuccessionCandidate(PersonId personId, string displayName, int age, bool isAdult,
            FamilyMemberRole role, bool isDesignated)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Age = age;
            IsAdult = isAdult;
            Role = role;
            IsDesignated = isDesignated;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public int Age { get; }
        public bool IsAdult { get; }
        public FamilyMemberRole Role { get; }
        public bool IsDesignated { get; }

        public string RoleName
        {
            get
            {
                switch (Role)
                {
                    case FamilyMemberRole.DirectKin: return "直系亲族";
                    case FamilyMemberRole.CollateralKin: return "旁系亲族";
                    case FamilyMemberRole.AdoptedChild: return "养子/养女";
                    case FamilyMemberRole.Spouse: return "配偶";
                    case FamilyMemberRole.Retainer: return "家臣";
                    case FamilyMemberRole.Disciple: return "弟子";
                    case FamilyMemberRole.Guest: return "客将";
                    default: return Role.ToString();
                }
            }
        }
    }

    /// <summary>Immutable result of one succession command.</summary>
    public readonly struct SuccessionResult
    {
        public SuccessionResult(SuccessionOutcome outcome, bool succeeded, string message,
            SuccessionPhase phase, PersonId subject)
        {
            Outcome = outcome;
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            Phase = phase;
            Subject = subject;
        }

        public SuccessionOutcome Outcome { get; }
        public bool Succeeded { get; }
        public string Message { get; }
        public SuccessionPhase Phase { get; }
        public PersonId Subject { get; }
    }

    /// <summary>
    /// Owns the SC-003 roster view used by succession: permanent person identities,
    /// one family roster, and structural kinship facts. Lifecycle facts (birth year,
    /// alive, retired) live beside them instead of modifying the SC-003 types.
    /// </summary>
    public sealed class SuccessionWorld
    {
        private readonly Dictionary<PersonId, PersonState> _persons = new Dictionary<PersonId, PersonState>();
        private readonly Dictionary<PersonId, SuccessionPersonLife> _life =
            new Dictionary<PersonId, SuccessionPersonLife>();
        private readonly FamilyState _family;
        private readonly KinshipGraph _kinship = new KinshipGraph();
        private readonly FamilyPregnancyState _pregnancies = new FamilyPregnancyState();

        public SuccessionWorld(FamilyId familyId, string familyName)
        {
            _family = new FamilyState(familyId, familyName);
        }

        public FamilyState Family => _family;
        public KinshipGraph Kinship => _kinship;
        /// <summary>Active M04B pregnancies, ordered by stable gestating-person ID.</summary>
        public FamilyPregnancyState Pregnancies => _pregnancies;
        public int PersonCount => _persons.Count;

        /// <summary>Deterministically ordered person snapshot (ordinal person ID).</summary>
        public IReadOnlyList<PersonState> Persons
        {
            get
            {
                var snapshot = new List<PersonState>(_persons.Count);
                foreach (KeyValuePair<PersonId, PersonState> entry in _persons)
                    snapshot.Add(entry.Value);
                snapshot.Sort((a, b) => string.Compare(a.Id.Value ?? string.Empty,
                    b.Id.Value ?? string.Empty, StringComparison.Ordinal));
                return snapshot.AsReadOnly();
            }
        }

        public bool TryAddPerson(PersonState person, SuccessionPersonLife life, out string error)
        {
            if (person == null || person.Id.IsEmpty)
            {
                error = "Person must have a non-empty permanent ID.";
                return false;
            }
            if (life.BirthYear <= 0)
            {
                error = "Birth year must be positive.";
                return false;
            }
            if (!Enum.IsDefined(typeof(PersonGender), person.Gender))
            {
                error = "Person gender is undefined: " + (int)person.Gender;
                return false;
            }
            if (_persons.ContainsKey(person.Id))
            {
                error = "Person ID is already registered: " + person.Id;
                return false;
            }
            _persons.Add(person.Id, person);
            _life.Add(person.Id, life);
            error = string.Empty;
            return true;
        }

        public bool TryRemovePerson(PersonId personId)
        {
            if (!_persons.Remove(personId))
                return false;
            _life.Remove(personId);
            return true;
        }

        public bool TryGetPerson(PersonId personId, out PersonState person) =>
            _persons.TryGetValue(personId, out person);

        public bool TryGetLife(PersonId personId, out SuccessionPersonLife life) =>
            _life.TryGetValue(personId, out life);

        public bool TrySetLife(PersonId personId, SuccessionPersonLife life, out string error)
        {
            if (!_persons.ContainsKey(personId))
            {
                error = "Person is not registered: " + personId;
                return false;
            }
            if (life.BirthYear <= 0)
            {
                error = "Birth year must be positive.";
                return false;
            }
            _life[personId] = life;
            error = string.Empty;
            return true;
        }

        public bool IsLiving(PersonId personId) =>
            _life.TryGetValue(personId, out SuccessionPersonLife life) && life.Alive;

        public bool TryAddMember(PersonId personId, FamilyMemberRole role, out string error)
        {
            if (!_persons.ContainsKey(personId))
            {
                error = "Family member is not a registered person: " + personId;
                return false;
            }
            return _family.TryAddMember(personId, role, out error);
        }

        public bool TrySetRole(PersonId personId, FamilyMemberRole role, out string error) =>
            _family.TrySetRole(personId, role, out error);

        public bool TryRemoveMember(PersonId personId, out string error) =>
            _family.TryRemoveMember(personId, out error);

        public bool TryAddKinship(KinshipLink link, out string error) =>
            _kinship.TryAdd(link, out error);

#pragma warning disable UAC0009 // the approved Debug Mode plan requires the Editor/development build boundary
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Replaces one registered person with a person that keeps the same permanent ID.
        /// This is the debug transaction's gender/name seam and is compiled only for the
        /// Editor and development builds. Only the display fact changes: life, membership,
        /// kinship and pregnancies are untouched, and the new gender must be a defined
        /// value. A missing person, an empty ID or an undefined gender is rejected without
        /// changing anything.
        /// </summary>
        public bool TryReplacePerson(PersonState replacement, out string error)
        {
            if (replacement == null)
            {
                error = "Replacement person must not be null.";
                return false;
            }
            if (replacement.Id.IsEmpty)
            {
                error = "Replacement person must keep a non-empty permanent ID.";
                return false;
            }
            if (!_persons.ContainsKey(replacement.Id))
            {
                error = "Person is not registered: " + replacement.Id;
                return false;
            }
            if (!Enum.IsDefined(typeof(PersonGender), replacement.Gender))
            {
                error = "Person gender is undefined: " + (int)replacement.Gender;
                return false;
            }
            _persons[replacement.Id] = replacement;
            error = string.Empty;
            return true;
        }
#endif
#pragma warning restore UAC0009

#pragma warning disable UAC0009 // the approved Debug Mode plan requires the Editor/development build boundary
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Captures every fact this world owns so a debug transaction can restore the
        /// exact pre-command state after a rejected or failed commit. The snapshot keeps
        /// person references, so a caller that mutated a person in place must not rely on
        /// it to undo that mutation.
        /// </summary>
        internal SuccessionWorldSnapshot CaptureSnapshot()
        {
            var persons = new List<PersonState>(_persons.Count);
            var lives = new List<KeyValuePair<PersonId, SuccessionPersonLife>>(_life.Count);
            foreach (KeyValuePair<PersonId, PersonState> entry in _persons)
                persons.Add(entry.Value);
            foreach (KeyValuePair<PersonId, SuccessionPersonLife> entry in _life)
                lives.Add(entry);
            // The world's own views are live wrappers over mutable collections, so a
            // snapshot must copy every list before a rollback clears them.
            var members = new List<FamilyMembership>(_family.Members);
            var links = new List<KinshipLink>(_kinship.Links);
            var pregnancies = new List<FamilyPregnancyRecord>(_pregnancies.Records);
            return new SuccessionWorldSnapshot(persons, lives, members, links, pregnancies);
        }

        /// <summary>Restores a snapshot captured by <see cref="CaptureSnapshot"/> on this world.</summary>
        internal bool TryRestoreSnapshot(SuccessionWorldSnapshot snapshot)
        {
            if (snapshot == null)
                return false;
            _persons.Clear();
            for (int i = 0; i < snapshot.Persons.Count; i++)
                _persons[snapshot.Persons[i].Id] = snapshot.Persons[i];
            _life.Clear();
            for (int i = 0; i < snapshot.Lives.Count; i++)
                _life[snapshot.Lives[i].Key] = snapshot.Lives[i].Value;
            _family.RestoreMembers(snapshot.Members);
            _kinship.RestoreLinks(snapshot.Links);
            _pregnancies.RestoreRecords(snapshot.Pregnancies);
            return true;
        }
#endif
#pragma warning restore UAC0009
    }


#pragma warning disable UAC0009 // the approved Debug Mode plan requires the Editor/development build boundary
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Detached capture of the four fact collections a <see cref="SuccessionWorld"/> owns.
    /// It is transaction support for the isolated debug session and is not persisted.
    /// </summary>
    internal sealed class SuccessionWorldSnapshot
    {
        internal SuccessionWorldSnapshot(IReadOnlyList<PersonState> persons,
            IReadOnlyList<KeyValuePair<PersonId, SuccessionPersonLife>> lives,
            IReadOnlyList<FamilyMembership> members, IReadOnlyList<KinshipLink> links,
            IReadOnlyList<FamilyPregnancyRecord> pregnancies)
        {
            Persons = persons;
            Lives = lives;
            Members = members;
            Links = links;
            Pregnancies = pregnancies;
        }

        internal IReadOnlyList<PersonState> Persons { get; }
        internal IReadOnlyList<KeyValuePair<PersonId, SuccessionPersonLife>> Lives { get; }
        internal IReadOnlyList<FamilyMembership> Members { get; }
        internal IReadOnlyList<KinshipLink> Links { get; }
        internal IReadOnlyList<FamilyPregnancyRecord> Pregnancies { get; }
    }
#endif
#pragma warning restore UAC0009

    /// <summary>
    /// Mutable succession decision state for one family. The service is the only
    /// writer; public setters exist so persistence can restore an already-validated
    /// snapshot without adding a second mutable model.
    /// </summary>
    public sealed class SuccessionState
    {
        public const int DefaultAdulthoodAge = 16;

        public SuccessionState(SuccessionWorld world)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Phase = SuccessionPhase.None;
            AdulthoodAge = DefaultAdulthoodAge;
            CompletedResultId = string.Empty;
        }

        public SuccessionWorld World { get; }

        public SuccessionPhase Phase { get; set; }
        public PersonId HeadPersonId { get; set; }
        public PersonId DesignatedHeirId { get; set; }
        public PersonId PendingSuccessorId { get; set; }
        public PersonId PendingRegentId { get; set; }
        public int AdulthoodAge { get; set; }
        public int SuccessionCount { get; set; }
        public string CompletedResultId { get; set; }
        public bool CrisisAdoptionUsed { get; set; }
        public bool RegencyCompleted { get; set; }

        public bool IsPending =>
            Phase == SuccessionPhase.AwaitingSuccessionChoice ||
            Phase == SuccessionPhase.AwaitingRegentChoice ||
            Phase == SuccessionPhase.CrisisDecision;

        public bool TryGetHead(out PersonState person) =>
            World.TryGetPerson(HeadPersonId, out person);

        public bool TryGetHeadLife(out SuccessionPersonLife life) =>
            World.TryGetLife(HeadPersonId, out life);

        public int HeadAge(GameDate date) =>
            TryGetHeadLife(out SuccessionPersonLife life) ? life.AgeAt(date) : 0;

        /// <summary>
        /// True once a minor head reaches adulthood while still in the regency phase.
        /// Time advancement pauses at this point; only an explicit player handoff
        /// completes the regency, matching GDD 19.2.
        /// </summary>
        public bool IsRegencyHandoffDue(GameDate date) =>
            Phase == SuccessionPhase.Regency &&
            TryGetHeadLife(out SuccessionPersonLife life) &&
            life.Alive && life.IsAdultAt(date, AdulthoodAge);
    }

    /// <summary>
    /// Fixed, deterministic SC-010 demo rosters. All IDs are permanent and all
    /// content is original. The standard roster intentionally designates a deceased
    /// heir so the invalid-designation fallback is visible without random input.
    /// </summary>
    public static class SuccessionSample
    {
        public static readonly FamilyId StandardFamilyId = new FamilyId("family.akechi");
        public static readonly PersonId StandardHeadId = new PersonId("person.akechi.harunobu");
        public static readonly PersonId StandardDesignatedHeirId = new PersonId("person.akechi.kageyu");

        public static SuccessionState CreateStandard()
        {
            var world = new SuccessionWorld(StandardFamilyId, "明智家");
            Add(world, StandardHeadId, "明智 晴信", 1528, true, false, FamilyMemberRole.Head);
            Add(world, new PersonId("person.akechi.chie"), "明智 千恵", 1532, true, false, FamilyMemberRole.Spouse);
            Add(world, new PersonId("person.akechi.masatsuna"), "明智 正綱", 1536, true, false, FamilyMemberRole.CollateralKin);
            Add(world, new PersonId("person.akechi.tokihime"), "明智 時姫", 1546, true, false, FamilyMemberRole.DirectKin);
            Add(world, new PersonId("person.akechi.nobumasa"), "明智 信正", 1552, true, false, FamilyMemberRole.DirectKin);
            Add(world, StandardDesignatedHeirId, "明智 景祐", 1550, false, false, FamilyMemberRole.DirectKin);
            Add(world, new PersonId("person.retainer.sakon"), "佐近", 1520, true, false, FamilyMemberRole.Retainer);

            Link(world, StandardHeadId, new PersonId("person.akechi.nobumasa"), KinshipType.ParentChild);
            Link(world, StandardHeadId, new PersonId("person.akechi.tokihime"), KinshipType.ParentChild);
            Link(world, StandardHeadId, StandardDesignatedHeirId, KinshipType.ParentChild);
            Link(world, StandardHeadId, new PersonId("person.akechi.masatsuna"), KinshipType.Sibling);
            Link(world, StandardHeadId, new PersonId("person.akechi.chie"), KinshipType.Spouse);
            Link(world, new PersonId("person.akechi.chie"), new PersonId("person.akechi.nobumasa"), KinshipType.ParentChild);
            Link(world, new PersonId("person.akechi.chie"), new PersonId("person.akechi.tokihime"), KinshipType.ParentChild);
            Link(world, new PersonId("person.akechi.chie"), StandardDesignatedHeirId, KinshipType.ParentChild);

            return new SuccessionState(world)
            {
                HeadPersonId = StandardHeadId,
                DesignatedHeirId = StandardDesignatedHeirId
            };
        }

        /// <summary>
        /// Fixed crisis demo: the head has a spouse and a retainer but no living
        /// lawful successor. Retirement therefore reaches the one crisis decision.
        /// </summary>
        public static SuccessionState CreateCrisis()
        {
            var world = new SuccessionWorld(StandardFamilyId, "明智家");
            Add(world, StandardHeadId, "明智 晴信", 1528, true, false, FamilyMemberRole.Head);
            Add(world, new PersonId("person.akechi.chie"), "明智 千恵", 1532, true, false, FamilyMemberRole.Spouse);
            Add(world, StandardDesignatedHeirId, "明智 景祐", 1550, false, false, FamilyMemberRole.DirectKin);
            Add(world, new PersonId("person.retainer.sakon"), "佐近", 1520, true, false, FamilyMemberRole.Retainer);

            Link(world, StandardHeadId, new PersonId("person.akechi.chie"), KinshipType.Spouse);
            Link(world, StandardHeadId, StandardDesignatedHeirId, KinshipType.ParentChild);
            Link(world, new PersonId("person.akechi.chie"), StandardDesignatedHeirId, KinshipType.ParentChild);

            return new SuccessionState(world)
            {
                HeadPersonId = StandardHeadId,
                DesignatedHeirId = StandardDesignatedHeirId
            };
        }

        private static void Add(SuccessionWorld world, PersonId id, string name, int birthYear,
            bool alive, bool retired, FamilyMemberRole role)
        {
            if (!world.TryAddPerson(new PersonState(id, name,
                        PersonGenderCatalog.GenderFor(id)),
                    new SuccessionPersonLife(birthYear, alive, retired), out string error))
                throw new InvalidOperationException(error);
            if (!world.TryAddMember(id, role, out error))
                throw new InvalidOperationException(error);
        }

        private static void Link(SuccessionWorld world, PersonId from, PersonId to, KinshipType type)
        {
            if (!KinshipLink.TryCreate(from, to, type, out KinshipLink link, out string error))
                throw new InvalidOperationException(error);
            if (!world.TryAddKinship(link, out error))
                throw new InvalidOperationException(error);
        }
    }
}
