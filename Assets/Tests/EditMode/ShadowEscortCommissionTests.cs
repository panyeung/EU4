using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Pure M05 domain rules: attribute check bands, creed consequences, lifecycle
    /// guards and save round-trip validation. No Unity scene or session is involved.
    /// </summary>
    public class ShadowEscortCommissionTests
    {
        private static readonly PersonId Assignee = new PersonId("person.test.assignee");

        private static CharacterGrowthValues Abilities(int martial, int management, int social) =>
            CharacterGrowthValues.CreateClamped(0, martial, 0, management, social);

        private static IndustryResources Resources(int funds = 30, int supplies = 12,
            int intelligence = 3)
        {
            Assert.That(IndustryResources.TryCreate(funds, supplies, intelligence,
                out IndustryResources resources, out string error), Is.True, error);
            return resources;
        }

        private static ShadowEscortCommission Accepted(FamilyCreedKind creed,
            out IndustryResources resources)
        {
            var commission = new ShadowEscortCommission();
            ShadowEscortActionResult accept = commission.TryAccept(LocationId.Kiyosu, creed,
                Resources(), out resources);
            Assert.That(accept.Succeeded, Is.True, accept.Feedback);
            return commission;
        }

        private static ShadowEscortCommission Due(FamilyCreedKind creed,
            out IndustryResources resources)
        {
            ShadowEscortCommission commission = Accepted(creed, out resources);
            Assert.That(commission.TryAssign(Assignee, false, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.True);
            for (int day = 0; day < ShadowEscortCommission.DurationDays; day++)
            {
                Assert.That(commission.AdvanceOneDay(out bool due), Is.True);
                Assert.That(due, Is.EqualTo(day == ShadowEscortCommission.DurationDays - 1));
            }
            return commission;
        }

        [Test]
        public void AdvanceAfterDue_DoesNotGrowElapsedPastDuration()
        {
            ShadowEscortCommission commission = Due(FamilyCreedKind.RighteousnessFirst, out _);
            Assert.That(commission.ElapsedDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays));
            Assert.That(commission.Stage, Is.EqualTo(ShadowEscortStage.InProgress));

            Assert.That(commission.AdvanceOneDay(out bool due), Is.True);
            Assert.That(due, Is.True);
            Assert.That(commission.ElapsedDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays),
                "A retry day must never create an elapsed-greater-than-duration save row.");
        }

        [TestCase(ShadowEscortStage.InProgress, CommissionOutcome.None, 4, 3)]
        [TestCase(ShadowEscortStage.InProgress, CommissionOutcome.None, 1, 4)]
        [TestCase(ShadowEscortStage.Succeeded, CommissionOutcome.Success, 2, 3)]
        [TestCase(ShadowEscortStage.Succeeded, CommissionOutcome.Success, 3, 3)]
        public void Restore_ValidatesElapsedAgainstTheWindow(ShadowEscortStage stage,
            CommissionOutcome outcome, int elapsedDays, int durationDays)
        {
            bool expectValid = stage == ShadowEscortStage.Succeeded &&
                               elapsedDays == durationDays &&
                               durationDays == ShadowEscortCommission.DurationDays;
            ShadowEscortCommission restored = ShadowEscortCommission.TryRestore(stage,
                "person.test.assignee", durationDays, elapsedDays, 1560, 4, 1, outcome,
                outcome == CommissionOutcome.Success ? 100 : 0, 20,
                outcome == CommissionOutcome.Success ? 14 : 0,
                outcome == CommissionOutcome.Success ? 4 : 0, 4, string.Empty,
                out string error);

            Assert.That(restored != null, Is.EqualTo(expectValid), error);
            if (!expectValid)
                Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void SettlementUsesTheCreedPresentedAtSettlement()
        {
            ShadowEscortCommission commission = Accepted(FamilyCreedKind.SecrecyFirst,
                out IndustryResources acceptedFunds);
            Assert.That(commission.InvestedFunds, Is.EqualTo(
                ShadowEscortCommission.InvestmentFundsFor(FamilyCreedKind.SecrecyFirst)),
                "The accepted creed owns the upfront cost.");
            Assert.That(commission.TryAssign(Assignee, false, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.True);
            for (int day = 0; day < ShadowEscortCommission.DurationDays; day++)
                commission.AdvanceOneDay(out _);

            // With zero attributes, roll 10 reaches the partial band only because the
            // settlement creed adds +10; dropping the creed bonus would produce failure.
            ShadowEscortActionResult result = commission.TryResolve(Abilities(0, 0, 0), 10,
                FamilyCreedKind.RighteousnessFirst, acceptedFunds, out _);

            Assert.That(result.Outcome, Is.EqualTo(CommissionOutcome.Partial));
            Assert.That(commission.InvestedFunds, Is.EqualTo(
                ShadowEscortCommission.InvestmentFundsFor(FamilyCreedKind.SecrecyFirst)));
        }

        [Test]
        public void Reward_IsClampedAtTheResourceCeilingInsteadOfFailingSettlement()
        {
            ShadowEscortCommission commission = Due(FamilyCreedKind.RighteousnessFirst, out _);
            IndustryResources full = Resources(IndustryResources.Maximum,
                IndustryResources.Maximum, 0);

            ShadowEscortActionResult result = commission.TryResolve(Abilities(60, 60, 60), 0,
                FamilyCreedKind.RighteousnessFirst, full, out IndustryResources after);

            Assert.That(result.Succeeded, Is.True, result.Feedback);
            Assert.That(result.Outcome, Is.EqualTo(CommissionOutcome.Success));
            Assert.That(after.Funds, Is.EqualTo(IndustryResources.Maximum));
            Assert.That(after.Supplies, Is.EqualTo(IndustryResources.Maximum));
            Assert.That(result.FundsDelta, Is.Zero,
                "a clamped settlement must report the applied delta, not the nominal reward.");
            Assert.That(result.SuppliesDelta, Is.Zero);
            Assert.That(commission.RewardFunds, Is.Zero);
            Assert.That(commission.RewardSupplies, Is.Zero);
        }

        [Test]
        public void Restore_RejectsInconsistentResultAndRewardRows()
        {
            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.Failed,
                Assignee.Value, 3, 3, 1560, 4, 1, CommissionOutcome.Failure, 10, 0,
                7, 1, 4, string.Empty, out string failureError), Is.Null);
            Assert.That(failureError, Does.Contain("报酬"));

            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.InProgress,
                Assignee.Value, 3, 1, 1560, 4, 1, CommissionOutcome.None, 40, 5,
                0, 0, 4, string.Empty, out string progressError), Is.Null);
            Assert.That(progressError, Does.Contain("检定"));

            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.Succeeded,
                Assignee.Value, 3, 3, 1560, 4, 1, CommissionOutcome.Success, 100, 0,
                0, 0, 4, string.Empty, out _), Is.Not.Null,
                "a fully clamped success legitimately stores zero applied rewards.");

            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.NotAccepted,
                string.Empty, 0, 0, 0, 0, 0, CommissionOutcome.None, 5, 0, 0, 0, 0,
                string.Empty, out string notAcceptedError), Is.Null);
            Assert.That(notAcceptedError, Does.Contain("检定"));
            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.Accepted,
                string.Empty, 0, 0, 0, 0, 0, CommissionOutcome.None, 0, 1, 0, 0, 4,
                string.Empty, out string acceptedError), Is.Null);
            Assert.That(acceptedError, Does.Contain("检定"));
        }

        [Test]
        public void Abandon_ClosesInProgressAsADistinctStateAndKeepsTheInvestment()
        {
            ShadowEscortCommission commission = Accepted(FamilyCreedKind.RighteousnessFirst,
                out _);
            Assert.That(commission.TryAbandon("x", out _), Is.False,
                "only an in-progress commission can be abandoned.");
            Assert.That(commission.TryAssign(Assignee, false, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.True);
            Assert.That(commission.AdvanceOneDay(out _), Is.True);

            Assert.That(commission.TryAbandon("执行人已离世，委托失败。", out ShadowEscortActionResult result),
                Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(commission.Stage, Is.EqualTo(ShadowEscortStage.Abandoned),
                "abandonment is not a shared M07 result.");
            Assert.That(commission.Outcome, Is.EqualTo(CommissionOutcome.None));
            Assert.That(commission.RunId, Is.Empty,
                "an abandoned run keeps no shared-result run ID.");
            Assert.That(commission.ElapsedDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays));
            Assert.That(commission.RewardFunds, Is.Zero);
            Assert.That(commission.RewardSupplies, Is.Zero);
            Assert.That(commission.InvestedFunds,
                Is.EqualTo(ShadowEscortCommission.InvestmentFundsFor(
                    FamilyCreedKind.RighteousnessFirst)));
            Assert.That(commission.LastFeedback, Does.Contain("离世"));

            ShadowEscortCommission restored = ShadowEscortCommission.TryRestore(
                ShadowEscortStage.Abandoned, Assignee.Value, 3, 3, 1560, 4, 1,
                CommissionOutcome.None, 0, 0, 0, 0,
                ShadowEscortCommission.InvestmentFundsFor(FamilyCreedKind.RighteousnessFirst),
                commission.LastFeedback, out string error);
            Assert.That(restored, Is.Not.Null, error);
        }

        [Test]
        public void Restore_LegacyMode_IsAllowedOnlyForMigratedExecutionAndAbandonment()
        {
            int investment =
                ShadowEscortCommission.InvestmentFundsFor(FamilyCreedKind.RighteousnessFirst);

            // A migrated in-progress row keeps its unknown mode behind the legacy run ID.
            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.InProgress,
                Assignee.Value, 3, 1, 1560, 4, 1, CommissionOutcome.None, 0, 0, 0, 0,
                investment, string.Empty, "legacy.m05.current", true,
                MissionOperationMode.LegacyUnknown, out string progressError), Is.Not.Null,
                progressError);

            // Abandonment clears the run ID but must keep the unknown mode saveable.
            ShadowEscortCommission abandoned = ShadowEscortCommission.TryRestore(
                ShadowEscortStage.Abandoned, Assignee.Value, 3, 3, 1560, 4, 1,
                CommissionOutcome.None, 0, 0, 0, 0, investment, "执行人不可用。", string.Empty,
                true, MissionOperationMode.LegacyUnknown, out string abandonError);
            Assert.That(abandoned, Is.Not.Null, abandonError);
            Assert.That(abandoned.Mode, Is.EqualTo(MissionOperationMode.LegacyUnknown),
                "the unknown mode is preserved instead of guessed.");

            // Malformed combinations stay rejected.
            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.Abandoned,
                Assignee.Value, 3, 3, 1560, 4, 1, CommissionOutcome.None, 0, 0, 0, 0,
                investment, string.Empty, "legacy.m05.current", true,
                MissionOperationMode.LegacyUnknown, out string runIdError), Is.Null,
                "an abandoned row must not keep a run ID.");
            Assert.That(runIdError, Does.Contain("运行 ID"));
            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.Accepted,
                string.Empty, 0, 0, 0, 0, 0, CommissionOutcome.None, 0, 0, 0, 0, investment,
                string.Empty, string.Empty, true, MissionOperationMode.LegacyUnknown,
                out string acceptedError), Is.Null);
            Assert.That(acceptedError, Does.Contain("未知执行方式"));
            Assert.That(ShadowEscortCommission.TryRestore(ShadowEscortStage.InProgress,
                Assignee.Value, 3, 1, 1560, 4, 1, CommissionOutcome.None, 0, 0, 0, 0,
                investment, string.Empty, "run.m05.shadow-escort", true,
                MissionOperationMode.LegacyUnknown, out string prefixError), Is.Null);
            Assert.That(prefixError, Does.Contain("旧版导入"));
        }

        [Test]
        public void TryAccept_RejectsWrongLocationAndInsufficientFunds()
        {
            var commission = new ShadowEscortCommission();
            Assert.That(commission.TryAccept(LocationId.Nagoya, FamilyCreedKind.RighteousnessFirst,
                Resources(), out IndustryResources unchanged).Succeeded, Is.False);
            Assert.That(unchanged, Is.EqualTo(Resources()));

            ShadowEscortActionResult poor = commission.TryAccept(LocationId.Kiyosu,
                FamilyCreedKind.RighteousnessFirst, Resources(2), out IndustryResources stillPoor);
            Assert.That(poor.Succeeded, Is.False);
            Assert.That(stillPoor, Is.EqualTo(Resources(2)));
            Assert.That(commission.Stage, Is.EqualTo(ShadowEscortStage.NotAccepted));
        }

        [Test]
        public void TryAccept_DeductsCreedCostOnce()
        {
            var commission = new ShadowEscortCommission();
            Assert.That(commission.TryAccept(LocationId.Kiyosu, FamilyCreedKind.RighteousnessFirst,
                Resources(), out IndustryResources after).Succeeded, Is.True);
            Assert.That(after.Funds,
                Is.EqualTo(Resources().Funds - ShadowEscortCommission.BaseInvestmentFunds));
            Assert.That(commission.TryAccept(LocationId.Kiyosu, FamilyCreedKind.RighteousnessFirst,
                after, out IndustryResources again).Succeeded, Is.False);
            Assert.That(again, Is.EqualTo(after));
        }

        [Test]
        public void TryAssign_RejectsBusyAssigneeAndSecondAssignment()
        {
            ShadowEscortCommission commission = Accepted(FamilyCreedKind.RighteousnessFirst,
                out _);
            Assert.That(commission.TryAssign(Assignee, true, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.False);
            Assert.That(commission.TryAssign(default, false, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.False);

            Assert.That(commission.TryAssign(Assignee, false, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.True);
            Assert.That(commission.TryAssign(Assignee, false, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.False);
            Assert.That(commission.Stage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(commission.AssigneeId, Is.EqualTo(Assignee));
        }

        [Test]
        public void TryResolve_RejectsBeforeDue()
        {
            ShadowEscortCommission commission = Accepted(FamilyCreedKind.RighteousnessFirst,
                out IndustryResources resources);
            Assert.That(commission.TryAssign(Assignee, false, false, true,
                new GameDate(1560, 4, 1)).Succeeded, Is.True);

            ShadowEscortActionResult early = commission.TryResolve(
                Abilities(40, 40, 40), 90, FamilyCreedKind.RighteousnessFirst, resources,
                out IndustryResources unchanged);

            Assert.That(early.Succeeded, Is.False);
            Assert.That(unchanged, Is.EqualTo(resources));
            Assert.That(commission.Stage, Is.EqualTo(ShadowEscortStage.InProgress));
        }

        [TestCase(40, 30, 20, 0, CommissionOutcome.Success)]
        [TestCase(0, 0, 0, 0, CommissionOutcome.Failure)]
        [TestCase(0, 0, 0, 5, CommissionOutcome.Partial)]
        [TestCase(0, 0, 0, 45, CommissionOutcome.Success)]
        public void AttributeCheck_BandsFollowAttributesAndRoll(int martial, int management,
            int social, int roll, CommissionOutcome expected)
        {
            // RighteousnessFirst has no failure-to-partial downgrade, so the band edge
            // is observable as the raw attribute/roll score.
            ShadowEscortCommission commission = Due(FamilyCreedKind.RighteousnessFirst,
                out IndustryResources resources);

            ShadowEscortActionResult result = commission.TryResolve(
                Abilities(martial, management, social), roll, FamilyCreedKind.RighteousnessFirst,
                resources, out _);

            Assert.That(result.Succeeded, Is.True, result.Feedback);
            Assert.That(result.Outcome, Is.EqualTo(expected));
            Assert.That(commission.Outcome, Is.EqualTo(expected));
        }

        [Test]
        public void HigherAttributes_SettleBetterThanZeroAttributes()
        {
            ShadowEscortCommission weak = Due(FamilyCreedKind.SecrecyFirst, out IndustryResources weakFunds);
            ShadowEscortCommission strong = Due(FamilyCreedKind.SecrecyFirst, out IndustryResources strongFunds);
            const int roll = 20;

            ShadowEscortActionResult weakResult = weak.TryResolve(Abilities(0, 0, 0), roll,
                FamilyCreedKind.SecrecyFirst, weakFunds, out IndustryResources _);
            ShadowEscortActionResult strongResult = strong.TryResolve(Abilities(30, 20, 15), roll,
                FamilyCreedKind.SecrecyFirst, strongFunds, out IndustryResources _);

            Assert.That(weakResult.Outcome, Is.EqualTo(CommissionOutcome.Partial));
            Assert.That(strongResult.Outcome, Is.EqualTo(CommissionOutcome.Success));
            Assert.That(strongResult.Score, Is.GreaterThan(weakResult.Score));
        }

        [Test]
        public void RighteousnessCreed_RaisesScoreWithoutChangingReward()
        {
            ShadowEscortCommission secret = Due(FamilyCreedKind.SecrecyFirst, out IndustryResources secretFunds);
            ShadowEscortCommission righteous = Due(FamilyCreedKind.RighteousnessFirst,
                out IndustryResources righteousFunds);
            // 20 attributes + 30 roll is below the success band for both; the +10 creed
            // bonus lifts only the righteous house over the partial threshold.
            ShadowEscortActionResult secretResult = secret.TryResolve(Abilities(10, 5, 5), 20,
                FamilyCreedKind.SecrecyFirst, secretFunds, out _);
            ShadowEscortActionResult righteousResult = righteous.TryResolve(Abilities(10, 5, 5), 20,
                FamilyCreedKind.RighteousnessFirst, righteousFunds, out _);

            Assert.That(righteousResult.Score,
                Is.EqualTo(secretResult.Score + ShadowEscortCommission.RighteousnessScoreBonus));
            Assert.That(righteousResult.FundsDelta, Is.EqualTo(secretResult.FundsDelta));
            Assert.That(righteousResult.SuppliesDelta, Is.EqualTo(secretResult.SuppliesDelta));
        }

        [Test]
        public void SecrecyCreed_TurnsFailureIntoPartialButCostsLessUpfront()
        {
            ShadowEscortCommission secret = Due(FamilyCreedKind.SecrecyFirst, out IndustryResources secretFunds);
            ShadowEscortCommission strict = Due(FamilyCreedKind.RighteousnessFirst,
                out IndustryResources strictFunds);

            ShadowEscortActionResult secretResult = secret.TryResolve(Abilities(0, 0, 0), 0,
                FamilyCreedKind.SecrecyFirst, secretFunds, out _);
            ShadowEscortActionResult strictResult = strict.TryResolve(Abilities(0, 0, 0), 0,
                FamilyCreedKind.RighteousnessFirst, strictFunds, out _);

            Assert.That(strictResult.Outcome, Is.EqualTo(CommissionOutcome.Failure));
            Assert.That(secretResult.Outcome, Is.EqualTo(CommissionOutcome.Partial));
            Assert.That(ShadowEscortCommission.InvestmentFundsFor(FamilyCreedKind.SecrecyFirst),
                Is.LessThan(ShadowEscortCommission.InvestmentFundsFor(
                    FamilyCreedKind.RighteousnessFirst)));
        }

        [Test]
        public void MeritFirstCreed_AddsBusinessRewardOnSuccess()
        {
            ShadowEscortCommission merit = Due(FamilyCreedKind.MeritFirst, out IndustryResources meritFunds);
            ShadowEscortCommission plain = Due(FamilyCreedKind.RighteousnessFirst,
                out IndustryResources plainFunds);

            ShadowEscortActionResult meritResult = merit.TryResolve(Abilities(60, 60, 60), 0,
                FamilyCreedKind.MeritFirst, meritFunds, out IndustryResources meritAfter);
            ShadowEscortActionResult plainResult = plain.TryResolve(Abilities(60, 60, 60), 0,
                FamilyCreedKind.RighteousnessFirst, plainFunds, out IndustryResources plainAfter);

            Assert.That(meritResult.Outcome, Is.EqualTo(CommissionOutcome.Success));
            Assert.That(plainResult.Outcome, Is.EqualTo(CommissionOutcome.Success));
            Assert.That(meritAfter.Funds - meritFunds.Funds,
                Is.EqualTo(plainAfter.Funds - plainFunds.Funds +
                           ShadowEscortCommission.MeritFirstRewardFundsBonus));
        }

        [Test]
        public void Settle_AppliesRewardOnceAndNeverAgain()
        {
            ShadowEscortCommission commission = Due(FamilyCreedKind.RighteousnessFirst,
                out IndustryResources resources);
            ShadowEscortActionResult first = commission.TryResolve(Abilities(60, 60, 60), 0,
                FamilyCreedKind.RighteousnessFirst, resources, out IndustryResources settled);
            Assert.That(first.Succeeded, Is.True);
            Assert.That(settled.Supplies - resources.Supplies,
                Is.EqualTo(ShadowEscortCommission.RewardSuccessSupplies));

            ShadowEscortActionResult second = commission.TryResolve(Abilities(60, 60, 60), 0,
                FamilyCreedKind.RighteousnessFirst, settled, out IndustryResources again);
            Assert.That(second.Succeeded, Is.False);
            Assert.That(again, Is.EqualTo(settled));
        }

        [TestCase(ShadowEscortStage.NotAccepted, CommissionOutcome.None, "", 0, 0)]
        [TestCase(ShadowEscortStage.Accepted, CommissionOutcome.None, "", 0, 0)]
        [TestCase(ShadowEscortStage.InProgress, CommissionOutcome.None, "person.test.assignee", 3, 1)]
        [TestCase(ShadowEscortStage.Succeeded, CommissionOutcome.Success, "person.test.assignee", 3, 3)]
        [TestCase(ShadowEscortStage.PartialSuccess, CommissionOutcome.Partial, "person.test.assignee", 3, 3)]
        [TestCase(ShadowEscortStage.Failed, CommissionOutcome.Failure, "person.test.assignee", 3, 3)]
        public void CaptureAndRestore_RoundTripsEveryStage(ShadowEscortStage stage,
            CommissionOutcome outcome, string assignee, int durationDays, int elapsedDays)
        {
            var source = new ShadowEscortCommission();
            if (stage == ShadowEscortStage.Accepted)
            {
                source = Accepted(FamilyCreedKind.RighteousnessFirst, out _);
            }
            else if (stage != ShadowEscortStage.NotAccepted)
            {
                ShadowEscortCommission due = Due(FamilyCreedKind.RighteousnessFirst,
                    out IndustryResources resources);
                if (stage == ShadowEscortStage.InProgress)
                {
                    ShadowEscortCommission partial = Accepted(FamilyCreedKind.RighteousnessFirst,
                        out _);
                    Assert.That(partial.TryAssign(Assignee, false, false, true,
                        new GameDate(1560, 4, 1)).Succeeded, Is.True);
                    Assert.That(partial.AdvanceOneDay(out _), Is.True);
                    source = partial;
                }
                else
                {
                    int roll = outcome == CommissionOutcome.Failure ? 0
                        : outcome == CommissionOutcome.Partial ? 5
                        : 90;
                    int attributes = outcome == CommissionOutcome.Success ? 20 : 0;
                    Assert.That(due.TryResolve(Abilities(attributes, attributes, attributes),
                        roll, FamilyCreedKind.RighteousnessFirst, resources, out _).Succeeded,
                        Is.True);
                    source = due;
                }
            }

            ShadowEscortCommissionSaveData saved = source.Capture();
            ShadowEscortCommission restored = ShadowEscortCommission.TryRestore(
                (ShadowEscortStage)saved.stage, saved.assigneePersonId, saved.durationDays,
                saved.elapsedDays, saved.startYear, saved.startMonth, saved.startDay,
                (CommissionOutcome)saved.outcome, saved.score, saved.roll, saved.rewardFunds,
                saved.rewardSupplies, saved.investedFunds, saved.lastFeedback, out string error);

            Assert.That(restored, Is.Not.Null, error);
            Assert.That(restored.Stage, Is.EqualTo(source.Stage));
            Assert.That(restored.Outcome, Is.EqualTo(source.Outcome));
            Assert.That(restored.AssigneeId, Is.EqualTo(source.AssigneeId));
            Assert.That(restored.ElapsedDays, Is.EqualTo(source.ElapsedDays));
            Assert.That(restored.DurationDaysValue, Is.EqualTo(source.DurationDaysValue));
            Assert.That(restored.RewardFunds, Is.EqualTo(source.RewardFunds));
            Assert.That(restored.RewardSupplies, Is.EqualTo(source.RewardSupplies));
            Assert.That(restored.InvestedFunds, Is.EqualTo(source.InvestedFunds));
        }

        [TestCase(ShadowEscortStage.Succeeded, CommissionOutcome.Failure)]
        [TestCase(ShadowEscortStage.InProgress, CommissionOutcome.Success)]
        [TestCase(ShadowEscortStage.NotAccepted, CommissionOutcome.None)]
        [TestCase(ShadowEscortStage.Accepted, CommissionOutcome.None)]
        public void Restore_RejectsInconsistentRows(ShadowEscortStage stage,
            CommissionOutcome outcome)
        {
            // Every case is invalid because the row also carries execution data that the
            // stage cannot own.
            ShadowEscortCommission restored = ShadowEscortCommission.TryRestore(stage,
                "person.test.assignee", 3, 3, 1560, 4, 1, outcome, 100, 20,
                outcome == CommissionOutcome.Success ? 14 : 0,
                outcome == CommissionOutcome.Success ? 4 : 0,
                stage == ShadowEscortStage.NotAccepted ? 0 : 4, string.Empty, out string error);

            Assert.That(restored, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }
    }
}
