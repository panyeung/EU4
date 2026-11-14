using System;
using System.Collections.Generic;
using System.Globalization;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// M08A family demography integration. These helpers only ever touch a working
    /// succession snapshot and the family chronicle prepared by
    /// <see cref="GameSession.TryAdvanceToNextMonth"/>; the monthly transaction owns
    /// commit and rollback of the date, RNG stream and every sibling ledger.
    /// </summary>
    public sealed partial class GameSession
    {
        public const string NaturalDeathChronicleTitle = "家人离世";

        /// <summary>
        /// The single reason text shared by the day-step rule and the annual-death
        /// boundary close, so an abandoned commission reads the same in the panel and in
        /// the action chronicle wherever the death wins.
        /// </summary>
        internal const string CommissionAbandonedByDeathReason =
            "委托执行人已离世或离开家族，委托失败。";

        /// <summary>
        /// Test-only hook invoked after the death/marriage facts are prepared but before
        /// the first chronicle append. Tests advance the ledger so the real append fails
        /// and prove the whole month rolls back. Never set by production code.
        /// </summary>
        internal Action<HistoryLedger> FamilyLifeCycleChronicleFaultInjection { get; set; }

        /// <summary>
        /// Applies the annual natural-death list to a cloned succession snapshot. A
        /// dead person keeps every recorded fact but stops being a valid worker: the
        /// commit path reconciles industry posts and daily actions. An unfinished
        /// pregnancy carried by a dead person is cancelled in the same snapshot. The
        /// existing death-succession flow runs at most once, for the current head.
        /// </summary>
        private static bool TryApplyNaturalDeaths(SuccessionState succession,
            IReadOnlyList<PersonId> deaths, GameDate date, out bool headDied,
            out string headDeathMessage, out string error)
        {
            headDied = false;
            headDeathMessage = string.Empty;
            error = string.Empty;
            if (succession == null || succession.World == null)
            {
                error = "自然死亡结算状态为空。";
                return false;
            }
            if (deaths == null)
                return true;

            for (int i = 0; i < deaths.Count; i++)
            {
                PersonId personId = deaths[i];
                if (!succession.World.TryGetLife(personId, out SuccessionPersonLife life))
                {
                    error = "自然死亡人物的生命状态缺失：" + personId.Value;
                    return false;
                }
                if (!life.Alive)
                    continue;

                if (!succession.World.TrySetLife(personId,
                        new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                        out error))
                    return false;

                if (personId == succession.HeadPersonId)
                    headDied = true;

                if (succession.World.Pregnancies.TryGet(personId,
                        out FamilyPregnancyRecord record) &&
                    !succession.World.Pregnancies.TryRemove(record, out error))
                    return false;
            }

            if (!headDied)
                return true;

            SuccessionResult transition = SuccessionService.BeginDeathSuccession(succession, date);
            if (!transition.Succeeded)
            {
                error = transition.Message;
                return false;
            }
            headDeathMessage = transition.Message;
            return true;
        }

        /// <summary>
        /// Appends the death and marriage facts in one append-only block. Every entry is
        /// validated against the next ledger slot before the first write, so a failure
        /// cannot leave a partially written block; the caller restores the pre-month
        /// ledger snapshot on any failure. Deaths come first so the ledger reads in
        /// application order.
        /// </summary>
        private bool TryAppendFamilyLifeCycleChronicles(SuccessionState succession,
            IReadOnlyList<PersonId> deaths, IReadOnlyList<FamilyMarriageChronicleFact> marriages,
            GameDate date, out string error)
        {
            error = string.Empty;
            int deathCount = deaths != null ? deaths.Count : 0;
            int marriageCount = marriages != null ? marriages.Count : 0;
            if (deathCount == 0 && marriageCount == 0)
                return true;

            HistoryLedger ledger = EnsureHistoryLedger();
            var prepared = new List<FamilyChronicleEntry>(deathCount + marriageCount);
            int sequence = ledger.Count;

            for (int i = 0; i < deathCount; i++)
            {
                PersonId personId = deaths[i];
                if (succession == null || succession.World == null ||
                    !succession.World.TryGetPerson(personId, out PersonState person) ||
                    !succession.World.TryGetLife(personId, out SuccessionPersonLife life))
                {
                    error = "自然死亡人物不在家族名册中：" + personId.Value;
                    return false;
                }

                int age = life.AgeAt(date);
                string summary = "「" + person.DisplayName + "」于 " +
                    date.Year.ToString("0000", CultureInfo.InvariantCulture) + " 年 " +
                    date.Month.ToString(CultureInfo.InvariantCulture) + " 月自然离世，享年 " +
                    age.ToString(CultureInfo.InvariantCulture) + " 岁。";
                sequence++;
                if (!TryPrepareLifeCycleChronicle(sequence, date,
                        ChronicleCategory.NaturalDeath, personId.Value,
                        FamilyChronicleEntry.NaturalDeathSourceId,
                        NaturalDeathChronicleTitle, summary, age,
                        out FamilyChronicleEntry deathEntry, out error))
                    return false;
                prepared.Add(deathEntry);
            }

            for (int i = 0; i < marriageCount; i++)
            {
                FamilyMarriageChronicleFact marriage = marriages[i];
                sequence++;
                if (!TryPrepareLifeCycleChronicle(sequence, date,
                        ChronicleCategory.FamilyFormation, marriage.CandidateId.Value,
                        FamilyChronicleEntry.MarriageSourceId, marriage.Title,
                        marriage.Summary, 0, out FamilyChronicleEntry marriageEntry, out error))
                    return false;
                prepared.Add(marriageEntry);
            }

            FamilyLifeCycleChronicleFaultInjection?.Invoke(ledger);
            for (int i = 0; i < prepared.Count; i++)
            {
                if (!ledger.TryAppendPrepared(prepared[i], out string appendError))
                {
                    error = appendError;
                    return false;
                }
            }

            return true;
        }

        private static bool TryPrepareLifeCycleChronicle(int sequence, GameDate date,
            ChronicleCategory category, string subjectId, string sourceId, string title,
            string summary, int fromValue, out FamilyChronicleEntry entry, out string error)
        {
            entry = null;
            string entryId = FamilyChronicleEntry.BuildEntryId(sequence);
            return FamilyChronicleEntry.TryCreate(sequence, entryId, date.Year, date.Month,
                category, subjectId, sourceId, title, summary, fromValue, 0, 0, 0, 0, 0,
                null, null, out entry, out error);
        }

        /// <summary>
        /// M08A: a natural death takes effect exactly at the January 1 annual boundary.
        /// The month transaction calls this for the boundary day <em>before</em> that day
        /// advances or resolves the commission, so death wins over a trip that would have
        /// become due on January 1. A row already terminal on December 31 is left alone,
        /// and this close rolls no Missions value, awards nothing and writes no
        /// shared-result history.
        /// </summary>
        private bool TryCloseCommissionForAnnualDeath(GameDate simulatedDate,
            IReadOnlyList<PersonId> naturalDeaths, out string error)
        {
            error = string.Empty;
            if (_commission.Stage != ShadowEscortStage.InProgress || naturalDeaths == null)
                return true;

            bool assigneeDied = false;
            for (int i = 0; i < naturalDeaths.Count; i++)
            {
                if (naturalDeaths[i] != _commission.AssigneeId)
                    continue;
                assigneeDied = true;
                break;
            }
            if (!assigneeDied)
                return true;

            if (TryAbandonCommission(simulatedDate, CommissionAbandonedByDeathReason))
                return true;
            error = "年度自然死亡后无法关闭进行中的护送委托：" +
                _commission.AssigneeId.Value;
            return false;
        }

        /// <summary>
        /// Post-commit death cleanup: vacate industry posts held by the dead and interrupt
        /// their in-flight daily actions. The M05 commission close here is only a backstop;
        /// a natural-death assignee was already closed on January 1 before commit by
        /// <see cref="TryCloseCommissionForAnnualDeath"/>. All helpers are total operations
        /// and the month's exception handler restores the pre-month industry,
        /// daily-action, commission and mission-history snapshots.
        /// </summary>
        private void ReconcileFamilyLifeCycleAfterCommit(GameDate date)
        {
            EnsureIndustryAssignmentsValid();
            ReconcileDailyActions();
            if (!TryCloseCommissionAfterFamilyDeath(date, out string error))
                throw new InvalidOperationException(error);
        }

        /// <summary>
        /// Backstop close for a row the month left without an executable assignee. This is
        /// not the natural-death path: <see cref="TryCloseCommissionForAnnualDeath"/> already
        /// closed a natural-death assignee on January 1 before the day advanced or settled,
        /// and that pre-commit Abandoned row is what keeps the committed state saveable.
        /// What can still reach this pass is a surviving assignee whom another monthly phase
        /// left blocked (for example a post or action applied during settlement), plus a
        /// pre-fix row. The existing M05/M07 rule for an assignee who can no longer execute
        /// is the Abandoned terminal state, so this pass uses the same semantics as a
        /// day-step boundary. The close must succeed: a false result aborts the month
        /// transaction instead of silently committing a row that cannot be saved.
        /// </summary>
        private bool TryCloseCommissionAfterFamilyDeath(GameDate date, out string error)
        {
            error = string.Empty;
            if (_commission.Stage != ShadowEscortStage.InProgress)
                return true;
            string block = CommissionAssigneeBlockReason();
            if (block == null)
                return true;
            if (TryAbandonCommission(date, block))
                return true;
            error = "自然死亡后无法关闭进行中的护送委托：" + block;
            return false;
        }
    }
}
