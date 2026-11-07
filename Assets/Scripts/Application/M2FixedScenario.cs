using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Application
{
    /// <summary>One display row of the fixed SC-013 family roster.</summary>
    public readonly struct M2RosterRow
    {
        public M2RosterRow(PersonId personId, string displayName, FamilyMemberRole role, int age,
            string generationLabel, string kinship, bool isHead, bool isDesignatedHeir,
            bool isDesignatedHeirValid)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Role = role;
            Age = age;
            GenerationLabel = generationLabel ?? string.Empty;
            Kinship = kinship ?? string.Empty;
            IsHead = isHead;
            IsDesignatedHeir = isDesignatedHeir;
            IsDesignatedHeirValid = isDesignatedHeirValid;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public FamilyMemberRole Role { get; }
        public int Age { get; }
        public string GenerationLabel { get; }
        public string Kinship { get; }
        public bool IsHead { get; }
        public bool IsDesignatedHeir { get; }
        public bool IsDesignatedHeirValid { get; }

        public string RoleName
        {
            get
            {
                switch (Role)
                {
                    case FamilyMemberRole.Head: return "家主";
                    case FamilyMemberRole.Spouse: return "配偶";
                    case FamilyMemberRole.DirectKin: return "直系亲族";
                    case FamilyMemberRole.CollateralKin: return "旁系亲族";
                    case FamilyMemberRole.AdoptedChild: return "养子/养女";
                    case FamilyMemberRole.Retainer: return "家臣";
                    case FamilyMemberRole.Disciple: return "弟子";
                    case FamilyMemberRole.Guest: return "客将";
                    default: return Role.ToString();
                }
            }
        }
    }

    /// <summary>Derived third-generation ending view; never stored as a separate save field.</summary>
    public readonly struct M2EndingView
    {
        public M2EndingView(bool isAvailable, string optionId, string title, string summary)
        {
            IsAvailable = isAvailable;
            OptionId = optionId ?? string.Empty;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
        }

        public bool IsAvailable { get; }
        public string OptionId { get; }
        public string Title { get; }
        public string Summary { get; }

        public static M2EndingView Unavailable => default;
    }

    /// <summary>
    /// Fixed three-generation M2 scenario. This type owns only the approved fixed
    /// roster, the fixed direct line, and M2-only progression gates. It reuses the
    /// existing succession, event history, relationship, chronicle and save systems.
    /// </summary>
    public static class M2FixedScenario
    {
        public const string FamilyIdValue = "family.akechi.sc013";
        public const string FamilyName = "明智家";

        public const string HarunobuIdValue = "person.sc013.harunobu";
        public const string ChieIdValue = "person.sc013.chie";
        public const string MasatsunaIdValue = "person.sc013.masatsuna";
        public const string NobumasaIdValue = "person.sc013.nobumasa";
        public const string TokihimeIdValue = "person.sc013.tokihime";
        public const string SakonIdValue = "person.sc013.sakon";

        public const int HarunobuBirthYear = 1490;
        public const int ChieBirthYear = 1494;
        public const int MasatsunaBirthYear = 1514;
        public const int NobumasaBirthYear = 1538;
        public const int TokihimeBirthYear = 1516;
        public const int SakonBirthYear = 1520;

        // The catalog owns the stable IDs; alias them here so the scenario logic and
        // the event data cannot drift into two sources of truth.
        public const string PledgeEventId = SC013EventIds.PledgeEventId;
        public const string KeepPromiseOptionId = SC013EventIds.KeepPromiseOptionId;
        public const string BreakPromiseOptionId = SC013EventIds.BreakPromiseOptionId;
        public const string PledgeChainId = SC013EventIds.ChainId;

        public const string FinaleEventId = SC013EventIds.FinaleEventId;
        public const string SeekAidOptionId = SC013EventIds.SeekAidOptionId;
        public const string StandAloneOptionId = SC013EventIds.StandAloneOptionId;
        public const int SeekAidMinimumRelationship = 5;

        public const int PledgeRelationshipDelta = 5;

        public static readonly FamilyId FamilyId = new FamilyId(FamilyIdValue);
        public static readonly PersonId HarunobuId = new PersonId(HarunobuIdValue);
        public static readonly PersonId ChieId = new PersonId(ChieIdValue);
        public static readonly PersonId MasatsunaId = new PersonId(MasatsunaIdValue);
        public static readonly PersonId NobumasaId = new PersonId(NobumasaIdValue);
        public static readonly PersonId TokihimeId = new PersonId(TokihimeIdValue);
        public static readonly PersonId SakonId = new PersonId(SakonIdValue);

        private static readonly PersonId[] DisplayOrder =
        {
            HarunobuId, ChieId, MasatsunaId, NobumasaId, TokihimeId, SakonId
        };

        /// <summary>Creates the approved six-person, three-generation fixed roster.</summary>
        public static SuccessionState CreateSuccession()
        {
            var world = new SuccessionWorld(FamilyId, FamilyName);
            Add(world, HarunobuId, "明智 晴信", HarunobuBirthYear, FamilyMemberRole.Head);
            Add(world, ChieId, "明智 千恵", ChieBirthYear, FamilyMemberRole.Spouse);
            Add(world, MasatsunaId, "明智 正綱", MasatsunaBirthYear, FamilyMemberRole.DirectKin);
            Add(world, NobumasaId, "明智 信正", NobumasaBirthYear, FamilyMemberRole.DirectKin);
            Add(world, TokihimeId, "明智 時姫", TokihimeBirthYear, FamilyMemberRole.CollateralKin);
            Add(world, SakonId, "佐近", SakonBirthYear, FamilyMemberRole.Retainer);

            Link(world, HarunobuId, ChieId, KinshipType.Spouse);
            Link(world, HarunobuId, MasatsunaId, KinshipType.ParentChild);
            Link(world, ChieId, MasatsunaId, KinshipType.ParentChild);
            Link(world, MasatsunaId, NobumasaId, KinshipType.ParentChild);
            Link(world, HarunobuId, TokihimeId, KinshipType.Sibling);

            return new SuccessionState(world)
            {
                HeadPersonId = HarunobuId,
                DesignatedHeirId = MasatsunaId
            };
        }

        /// <summary>True only for the fixed SC-013 family identity.</summary>
        public static bool IsActive(SuccessionState state) =>
            state != null && state.World != null && state.World.Family.Id == FamilyId;

        /// <summary>True for SC-013 catalog rows; used by the event scenario gate.</summary>
        public static bool IsScenarioEvent(string eventId) =>
            !string.IsNullOrEmpty(eventId) &&
            eventId.StartsWith("event.sc013.", StringComparison.Ordinal);

        /// <summary>The approved next direct heir for the current fixed head.</summary>
        public static PersonId ExpectedHeir(SuccessionState state) => ExpectedHeir(state, null);

        /// <summary>
        /// Route-aware heir lookup. The SC-024 route may designate its own
        /// fourth-generation child after the two existing successions; every other
        /// route, including ordinary SC-013, keeps the same fixed direct line.
        /// </summary>
        public static PersonId ExpectedHeir(SuccessionState state, string eventScenarioId)
        {
            if (state == null)
                return default;
            if (state.HeadPersonId == HarunobuId)
                return MasatsunaId;
            if (state.HeadPersonId == MasatsunaId)
                return NobumasaId;
            // The SC-024 child is valid only on the explicit sc024 route. A state that
            // merely carries the extra family data (for example a reused save) must not
            // change the ordinary SC-013 direct line or its "no fixed heir" finale.
            if (state.HeadPersonId == NobumasaId &&
                SC024FamilyDoctrineScenario.IsRoute(eventScenarioId))
                return SC024FamilyDoctrineScenario.ExpectedHeir(state);
            return default;
        }

        /// <summary>Restores the fixed designation after a lawful handoff clears it.</summary>
        public static void SyncDesignation(SuccessionState state) => SyncDesignation(state, null);

        /// <summary>Route-aware designation sync; ordinary SC-013 callers keep their line.</summary>
        public static void SyncDesignation(SuccessionState state, string eventScenarioId)
        {
            if (!IsActive(state) || state.Phase != SuccessionPhase.None)
                return;
            state.DesignatedHeirId = ExpectedHeir(state, eventScenarioId);
        }

        /// <summary>
        /// M2-only retirement gate. It derives progress from the stable family ID,
        /// committed event history and pending-event state; no extra save field exists.
        /// </summary>
        public static bool CanBeginRetirement(SuccessionState state, EventRuntimeState runtime,
            out string reason) => CanBeginRetirement(state, runtime, null, null, out reason);

        /// <summary>Date-aware overload used by the live session for candidate eligibility.</summary>
        public static bool CanBeginRetirement(SuccessionState state, EventRuntimeState runtime,
            GameDate? date, out string reason) =>
            CanBeginRetirement(state, runtime, date, null, out reason);

        /// <summary>
        /// Route-aware retirement gate. The SC-024 route satisfies the first-generation
        /// gate with its own creed-council event and does not wait for the SC-013-only
        /// pledge, rainy-arrival, or rainy-arrival legacy chain. With no route (ordinary
        /// SC-013) the original gates remain unchanged.
        /// </summary>
        public static bool CanBeginRetirement(SuccessionState state, EventRuntimeState runtime,
            GameDate? date, string eventScenarioId, out string reason)
        {
            reason = string.Empty;
            if (!IsActive(state))
            {
                reason = "当前不是 SC-013 固定三代剧本。";
                return false;
            }
            if (state.Phase != SuccessionPhase.None)
            {
                reason = "尚有继承决策待处理，请先完成当前选择。";
                return false;
            }
            if (runtime != null && runtime.HasPendingEvents)
            {
                reason = "尚有事件等待选择：请先解决当前事件，再交接家督。";
                return false;
            }

            bool sc024 = SC024FamilyDoctrineScenario.IsRoute(eventScenarioId);
            if (state.HeadPersonId == HarunobuId)
            {
                if (sc024)
                {
                    if (!HasCommittedSc024Council(runtime))
                    {
                        reason = "第一代尚未处理「家训议事」：请先结束月份并完成家族议事。";
                        return false;
                    }
                }
                else
                {
                    if (!HasCommitted(runtime, PledgeEventId))
                    {
                        reason = "第一代尚未处理「继承前的家议」：请先结束月份并完成承诺选择。";
                        return false;
                    }
                    if (!HasCommitted(runtime, SampleEventIds.EventId))
                    {
                        reason = "第一代尚未回应「雨夜来客」：请再结束月份，处理旧客来访。";
                        return false;
                    }
                }
            }
            else if (state.HeadPersonId == MasatsunaId)
            {
                // The SC-024 route does not gate on the SC-011 rainy-arrival legacy
                // chain, which requires an SC-013-compatible rainy choice made before
                // the first handoff. SC-013 keeps the original legacy gate unchanged.
                if (!sc024 &&
                    !HasCommitted(runtime, SC011EventIds.LegacyHonoredEventId) &&
                    !HasCommitted(runtime, SC011EventIds.LegacyDeferredEventId))
                {
                    reason = "第二代尚未回应旧客回访：请先结束月份并完成旧事选择。";
                    return false;
                }
            }
            else if (state.HeadPersonId == NobumasaId)
            {
                if (!IsFinaleCommitted(runtime))
                {
                    reason = "第三代尚未面对终局：请先结束月份并完成结局选择。";
                    return false;
                }
                return true;
            }
            else
            {
                reason = "当前家主不属于 SC-013 固定三代，无法执行固定交接。";
                return false;
            }

            PersonId heir = ExpectedHeir(state, eventScenarioId);
            if (heir.IsEmpty)
            {
                reason = "当前没有固定的下一代继承人。";
                return false;
            }
            if (!state.World.TryGetPerson(heir, out _))
            {
                reason = "固定继承人不存在，剧本无法继续。";
                return false;
            }
            if (!state.World.TryGetLife(heir, out SuccessionPersonLife life) || !life.Alive || life.Retired)
            {
                reason = "固定继承人已死亡或退休，剧本无法继续。";
                return false;
            }
            if (!state.World.Family.TryGetRole(heir, out FamilyMemberRole role) ||
                !SuccessionService.IsSuccessorRole(role))
            {
                reason = "固定继承人没有合法家族身份，剧本无法继续。";
                return false;
            }
            if (date.HasValue && !SuccessionService.IsEligibleSuccessor(state, heir, date.Value))
            {
                reason = "固定继承人当前不是合法候选（年龄或身份不符），剧本无法继续。";
                return false;
            }
            return true;
        }

        /// <summary>True when the SC-024 route has committed any of its creed-council events.</summary>
        public static bool HasCommittedSc024Council(EventRuntimeState runtime) =>
            HasCommitted(runtime, SC024EventIds.SecrecyCouncilEventId) ||
            HasCommitted(runtime, SC024EventIds.RighteousnessCouncilEventId) ||
            HasCommitted(runtime, SC024EventIds.MeritCouncilEventId);

        /// <summary>True when any committed history entry used the event.</summary>
        public static bool HasCommitted(EventRuntimeState runtime, string eventId)
        {
            if (runtime == null || string.IsNullOrEmpty(eventId))
                return false;
            IReadOnlyList<EventHistorySaveEntry> history = runtime.History;
            for (int i = 0; i < history.Count; i++)
            {
                EventHistorySaveEntry entry = history[i];
                if (entry != null && string.Equals(entry.eventId, eventId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>Option ID of the last committed entry for an event, or empty.</summary>
        public static string LastCommittedOption(EventRuntimeState runtime, string eventId)
        {
            if (runtime == null || string.IsNullOrEmpty(eventId))
                return string.Empty;
            IReadOnlyList<EventHistorySaveEntry> history = runtime.History;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                EventHistorySaveEntry entry = history[i];
                if (entry != null && string.Equals(entry.eventId, eventId, StringComparison.Ordinal))
                    return entry.optionId ?? string.Empty;
            }
            return string.Empty;
        }

        public static bool IsFinaleCommitted(EventRuntimeState runtime) =>
            HasCommitted(runtime, FinaleEventId);

        /// <summary>True when the derived ending panel should be available.</summary>
        public static bool TryBuildEnding(SuccessionState state, EventRuntimeState runtime,
            int ioriRelationship, out M2EndingView ending)
        {
            ending = M2EndingView.Unavailable;
            if (!IsActive(state) || runtime == null || state.SuccessionCount < 2)
                return false;
            string optionId = LastCommittedOption(runtime, FinaleEventId);
            if (string.IsNullOrEmpty(optionId))
                return false;

            bool seekAid = string.Equals(optionId, SeekAidOptionId, StringComparison.Ordinal);
            string title = seekAid ? "第三代结局 · 旧交来援" : "第三代结局 · 独自承担";
            var summary = new StringBuilder(320);
            if (seekAid)
            {
                summary.Append("你以两代人的信义请黑田伊织再度援手。");
            }
            else
            {
                summary.Append("你谢绝旧交，把家族的未来交回明智家自己手中。");
            }

            string pledge = LastCommittedOption(runtime, PledgeEventId);
            if (string.Equals(pledge, KeepPromiseOptionId, StringComparison.Ordinal))
                summary.Append("先代在继承前守住了对伊织的承诺。");
            else if (string.Equals(pledge, BreakPromiseOptionId, StringComparison.Ordinal))
                summary.Append("先代在继承前毁弃了旧诺，这道裂痕一直留到第三代。");

            string legacy = LastCommittedOption(runtime, SC011EventIds.LegacyHonoredEventId);
            if (string.IsNullOrEmpty(legacy))
                legacy = LastCommittedOption(runtime, SC011EventIds.LegacyDeferredEventId);
            AppendLegacyOutcome(summary, legacy);

            summary.Append("伊织与家族的信任为 ").Append(ioriRelationship).Append("。");
            if (seekAid)
                summary.Append("伊织应邀而来，第三代在家臣与旧友的见证下稳住了家业。");
            else
                summary.Append("你独自承担了旧事的重量，明智家以自己的方式继续前行。");

            ending = new M2EndingView(true, optionId, title, summary.ToString());
            return true;
        }

        /// <summary>Builds the fixed rows in a stable presentation order.</summary>
        public static IReadOnlyList<M2RosterRow> BuildRoster(SuccessionState state, GameDate date,
            PersonId designatedHeirId) => BuildRoster(state, date, designatedHeirId, null);

        /// <summary>
        /// Route-aware roster. The six fixed rows keep their order; any SC-024-only
        /// fourth-generation child is appended without changing the ordinary SC-013 view.
        /// </summary>
        public static IReadOnlyList<M2RosterRow> BuildRoster(SuccessionState state, GameDate date,
            PersonId designatedHeirId, string eventScenarioId)
        {
            var rows = new List<M2RosterRow>();
            if (state == null || state.World == null)
                return rows.AsReadOnly();

            bool m2 = IsActive(state);
            IEnumerable<PersonState> persons = m2
                ? OrderedPersons(state, SC024FamilyDoctrineScenario.IsRoute(eventScenarioId))
                : state.World.Persons;
            bool designatedValid = !designatedHeirId.IsEmpty &&
                SuccessionService.IsEligibleSuccessor(state, designatedHeirId, date);

            foreach (PersonState person in persons)
            {
                state.World.Family.TryGetRole(person.Id, out FamilyMemberRole role);
                state.World.TryGetLife(person.Id, out SuccessionPersonLife life);
                rows.Add(new M2RosterRow(person.Id, person.DisplayName, role,
                    life.BirthYear > 0 ? life.AgeAt(date) : 0,
                    GenerationLabel(person.Id, role, eventScenarioId), BuildKinship(state, person.Id),
                    person.Id == state.HeadPersonId,
                    person.Id == designatedHeirId,
                    person.Id == designatedHeirId && designatedValid));
            }
            return rows.AsReadOnly();
        }

        public static string GenerationLabel(PersonId personId, FamilyMemberRole role) =>
            GenerationLabel(personId, role, null);

        public static string GenerationLabel(PersonId personId, FamilyMemberRole role,
            string eventScenarioId)
        {
            // The child ID is SC-024-only, so it is labeled only on the explicit route.
            // Ordinary SC-013 never contains the child, and a stale state that does must
            // still present the original six-person generations.
            if (SC024FamilyDoctrineScenario.IsRoute(eventScenarioId) &&
                SC024FamilyDoctrineScenario.TryGetGenerationLabel(personId, out string sc024Label))
                return sc024Label;
            if (personId == HarunobuId || personId == ChieId || personId == TokihimeId)
                return "第一代";
            if (personId == MasatsunaId)
                return "第二代";
            if (personId == NobumasaId)
                return "第三代";
            return role == FamilyMemberRole.Retainer ? "家臣" : "当代";
        }

        private static IEnumerable<PersonState> OrderedPersons(SuccessionState state,
            bool includeRouteExtras)
        {
            var ordered = new List<PersonState>(DisplayOrder.Length);
            foreach (PersonId id in DisplayOrder)
                if (state.World.TryGetPerson(id, out PersonState person))
                    ordered.Add(person);
            if (!includeRouteExtras)
                return ordered;
            // A route may add people on top of the fixed six (SC-024's fourth-generation
            // child). Append them after the approved order; World.Persons is ordinal-sorted.
            foreach (PersonState person in state.World.Persons)
                if (!IsFixedPerson(person.Id))
                    ordered.Add(person);
            return ordered;
        }

        private static bool IsFixedPerson(PersonId personId)
        {
            for (int i = 0; i < DisplayOrder.Length; i++)
                if (DisplayOrder[i] == personId)
                    return true;
            return false;
        }

        private static string BuildKinship(SuccessionState state, PersonId personId)
        {
            IReadOnlyList<KinshipLink> links = state.World.Kinship.LinksFor(personId);
            if (links.Count == 0)
                return "无亲属记录";
            int parentCount = 0;
            foreach (KinshipLink link in links)
                if (link.Type == KinshipType.ParentChild && link.To == personId)
                    parentCount++;
            var parts = new List<string>(links.Count);
            foreach (KinshipLink link in links)
            {
                PersonId otherId = link.From == personId ? link.To : link.From;
                string otherName = state.World.TryGetPerson(otherId, out PersonState other)
                    ? other.DisplayName
                    : otherId.Value;
                switch (link.Type)
                {
                    case KinshipType.ParentChild:
                        if (link.From == personId)
                            parts.Add("子女：" + otherName);
                        else
                            // The fixed roster records a single father→child link for
                            // 信正; do not print the plural 父母 for one parent.
                            parts.Add((parentCount == 1 ? "父亲：" : "父母：") + otherName);
                        break;
                    case KinshipType.Sibling:
                        parts.Add("兄弟姐妹：" + otherName);
                        break;
                    case KinshipType.Spouse:
                        parts.Add("配偶：" + otherName);
                        break;
                    case KinshipType.Adoption:
                        parts.Add(link.From == personId ? "养子/养女：" + otherName : "养父母：" + otherName);
                        break;
                }
            }
            return string.Join("；", parts);
        }

        private static void AppendLegacyOutcome(StringBuilder summary, string legacyOptionId)
        {
            switch (legacyOptionId)
            {
                case SC011EventIds.LegacyHonoredRenewOptionId:
                case SC011EventIds.LegacyDeferredAtoneOptionId:
                    summary.Append("第二代续接了旧交。");
                    break;
                case SC011EventIds.LegacyHonoredTermsOptionId:
                case SC011EventIds.LegacyDeferredExplainOptionId:
                    summary.Append("第二代以新的条件延续了旧交。");
                    break;
                case SC011EventIds.LegacyHonoredCloseOptionId:
                case SC011EventIds.LegacyDeferredDismissOptionId:
                    summary.Append("第二代曾让旧交转冷。");
                    break;
            }
        }

        private static void Add(SuccessionWorld world, PersonId id, string name, int birthYear,
            FamilyMemberRole role)
        {
            if (!world.TryAddPerson(new PersonState(id, name),
                    new SuccessionPersonLife(birthYear, true, false), out string error))
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
