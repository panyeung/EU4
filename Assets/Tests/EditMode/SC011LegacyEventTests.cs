using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Session-level SC-011 flow: the first generation chooses the rainy-arrival option,
    /// one SC-010 handoff occurs, and the successor receives the matching follow-up variant.
    /// Compilation, Console, Play Mode and user acceptance are not claimed by these tests.
    /// </summary>
    public sealed class SC011LegacyEventTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            // SC-011 is the accepted SC-010 standard-slice behavior. Keep this
            // regression suite on the explicit standard roster instead of the
            // default SC-013 fixed scenario.
            _session.InitStandardDemo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        private void CompleteRoot(string optionId)
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            EventChoiceResult choice = _session.TryChooseEventOption(SampleEventIds.EventId, optionId,
                _session.CreateEventResultId(SampleEventIds.EventId, optionId));
            Assert.That(choice.Succeeded, Is.True, choice.Message);
            Assert.That(_session.PendingEventId, Is.Empty);
        }

        private void HandOffToMasatsuna()
        {
            SuccessionResult begun = _session.BeginSuccessionRetirement();
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            SuccessionResult confirmed = _session.ConfirmSuccession();
            Assert.That(confirmed.Succeeded, Is.True, confirmed.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(1));
        }

        private void HandOffToRegency()
        {
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.tokihime")).Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionRegent(
                new PersonId("person.akechi.chie")).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
        }

        private void CompleteRegencyToAdulthood()
        {
            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(800), Is.True, _session.LastEventMessage);
            Assert.That(_session.Succession.IsRegencyHandoffDue(_session.WorldClock.CurrentDate), Is.True);
            SuccessionResult handoff = _session.TryCompleteSuccessionRegency();
            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
        }

        private string PendingTitle()
        {
            if (string.IsNullOrEmpty(_session.PendingEventId))
                return string.Empty;
            foreach (EventDefinitionData definition in _session.EventCatalog.events)
            {
                if (definition.eventId != _session.PendingEventId) continue;
                foreach (EventTextEntry entry in _session.EventText.entries)
                    if (entry.textId == definition.textId)
                        return entry.title;
            }
            return string.Empty;
        }

        [Test]
        public void AdultHandoffSchedulesHonoredVariantForOfferRestChoice()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToMasatsuna();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
            Assert.That(PendingTitle(), Is.EqualTo("旧客的雨夜回访"));
            Assert.That(_session.PendingEventId, Is.Not.EqualTo(SC011EventIds.LegacyDeferredEventId));

            EventChoiceResult choice = _session.TryChooseEventOption(SC011EventIds.LegacyHonoredEventId,
                SC011EventIds.LegacyHonoredRenewOptionId,
                _session.CreateEventResultId(SC011EventIds.LegacyHonoredEventId,
                    SC011EventIds.LegacyHonoredRenewOptionId));
            Assert.That(choice.Succeeded, Is.True, choice.Message);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(10));
            Assert.That(_session.CaptureSave().eventRuntime.history, Has.Length.EqualTo(2));
        }

        [Test]
        public void AskTomorrowChoiceRequiresTrustThenSchedulesDeferredVariant()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 1).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.TryGetEventOptionAvailability(SampleEventIds.EventId,
                SampleEventIds.AskTomorrowOptionId, out bool enabled, out string reason), Is.True, reason);
            Assert.That(enabled, Is.True, reason);

            EventChoiceResult rootChoice = _session.TryChooseEventOption(SampleEventIds.EventId,
                SampleEventIds.AskTomorrowOptionId,
                _session.CreateEventResultId(SampleEventIds.EventId, SampleEventIds.AskTomorrowOptionId));
            Assert.That(rootChoice.Succeeded, Is.True, rootChoice.Message);
            HandOffToMasatsuna();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyDeferredEventId));
            Assert.That(PendingTitle(), Is.EqualTo("门房里的旧雨"));
            EventChoiceResult choice = _session.TryChooseEventOption(SC011EventIds.LegacyDeferredEventId,
                SC011EventIds.LegacyDeferredAtoneOptionId,
                _session.CreateEventResultId(SC011EventIds.LegacyDeferredEventId,
                    SC011EventIds.LegacyDeferredAtoneOptionId));
            Assert.That(choice.Succeeded, Is.True, choice.Message);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(6));
        }

        [Test]
        public void HandoffWithoutRootHistorySchedulesNothing()
        {
            HandOffToMasatsuna();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.PendingEventId, Is.Empty);
            Assert.That(_session.CaptureSave().eventRuntime.history, Is.Empty);
        }

        [Test]
        public void RootIsNeverOfferedAfterTheFirstHandoff()
        {
            HandOffToMasatsuna();

            for (int i = 0; i < 3; i++)
            {
                Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
                Assert.That(_session.PendingEventId, Is.Not.EqualTo(SampleEventIds.EventId));
            }
        }

        [Test]
        public void RegencyDefersVariantUntilExplicitAdultHandoff()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToRegency();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty,
                "the settled succession count is zero during an active regency");

            CompleteRegencyToAdulthood();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
        }

        [Test]
        public void FollowUpDoesNotRepeatAfterAnotherHandoff()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToMasatsuna();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
            Assert.That(_session.TryChooseEventOption(SC011EventIds.LegacyHonoredEventId,
                SC011EventIds.LegacyHonoredTermsOptionId,
                _session.CreateEventResultId(SC011EventIds.LegacyHonoredEventId,
                    SC011EventIds.LegacyHonoredTermsOptionId)).Succeeded, Is.True);

            HandOffToRegency();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty);
            CompleteRegencyToAdulthood();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty,
                "a resolved once-per-game follow-up must not be scheduled again");
        }

        [Test]
        public void PostHandoffSaveReplaysSameVariant()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToMasatsuna();
            SaveData checkpoint = _session.CaptureSave();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));

            Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);
            Assert.That(_session.PendingEventId, Is.Empty);
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(1));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
        }

        [Test]
        public void RootPendingSaveRestoresSameRootAndOptions()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            SaveData checkpoint = _session.CaptureSave();

            Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);

            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            Assert.That(PendingTitle(), Is.EqualTo("雨夜来客"));
            Assert.That(_session.Succession.SuccessionCount, Is.Zero);
            Assert.That(_session.TryGetEventOptionAvailability(SampleEventIds.EventId,
                SampleEventIds.OfferRestOptionId, out bool restEnabled, out string restReason), Is.True, restReason);
            Assert.That(restEnabled, Is.True, restReason);
            Assert.That(_session.TryGetEventOptionAvailability(SampleEventIds.EventId,
                SampleEventIds.AskTomorrowOptionId, out bool askEnabled, out string askReason), Is.True, askReason);
            Assert.That(askEnabled, Is.False,
                "the accepted ask-tomorrow option keeps its RelationshipAtLeast 1 requirement");
        }

        [Test]
        public void AfterRootChoiceSaveDoesNotReofferRoot()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            SaveData checkpoint = _session.CaptureSave();

            Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);

            Assert.That(_session.CaptureSave().eventRuntime.history, Has.Length.EqualTo(1));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty,
                "the root is not re-offered and no follow-up is due before a settled handoff");
        }

        [Test]
        public void RegencyCheckpointBeforeFollowUpReplaysAfterMajority()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToRegency();
            SaveData checkpoint = _session.CaptureSave();

            Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty,
                "a regency checkpoint must not schedule the follow-up early");

            CompleteRegencyToAdulthood();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
        }

        [Test]
        public void SecondSuccessionBeforeFollowUpSchedulesItOnceAfterSuccessorSettles()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToMasatsuna();

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.tokihime")).Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionRegent(
                new PersonId("person.akechi.chie")).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(2));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty,
                "the follow-up waits while the second successor is still a regency");

            CompleteRegencyToAdulthood();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
            Assert.That(_session.CaptureSave().eventRuntime.pendingEventIds, Has.Length.EqualTo(1),
                "the follow-up must be queued exactly once after the successor settles");
            Assert.That(_session.PendingEventId, Is.Not.EqualTo(SampleEventIds.EventId),
                "a later succession must not reopen the root");

            Assert.That(_session.TryChooseEventOption(SC011EventIds.LegacyHonoredEventId,
                SC011EventIds.LegacyHonoredTermsOptionId,
                _session.CreateEventResultId(SC011EventIds.LegacyHonoredEventId,
                    SC011EventIds.LegacyHonoredTermsOptionId)).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty);
        }

        [Test]
        public void FollowUpPendingSaveKeepsVariantOptionsAndRelationshipValue()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToMasatsuna();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData checkpoint = _session.CaptureSave();

            Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);

            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
            Assert.That(PendingTitle(), Is.EqualTo("旧客的雨夜回访"));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
        }

        [Test]
        public void AfterFollowUpChoiceReloadKeepsHistoryAndOnceMarker()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToMasatsuna();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.TryChooseEventOption(SC011EventIds.LegacyHonoredEventId,
                SC011EventIds.LegacyHonoredRenewOptionId,
                _session.CreateEventResultId(SC011EventIds.LegacyHonoredEventId,
                    SC011EventIds.LegacyHonoredRenewOptionId)).Succeeded, Is.True);
            SaveData checkpoint = _session.CaptureSave();

            Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);
            Assert.That(_session.CaptureSave().eventRuntime.history, Has.Length.EqualTo(2));
            CollectionAssert.Contains(_session.CaptureSave().eventRuntime.onceEventIds,
                SC011EventIds.LegacyHonoredEventId);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Empty);
        }

        [Test]
        public void ExtinctFamilyNeverSchedulesFollowUp()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            Assert.That(_session.StartCrisisDemo(), Is.True);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.DeclineCrisis().Succeeded, Is.True);
            Assert.That(_session.IsFamilyExtinct, Is.True);
            GameDate date = _session.WorldClock.CurrentDate;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.PendingSuccession));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.PendingEventId, Is.Empty);
        }

        [Test]
        public void ContentOneSaveLoadsAdditivelyAndIsRewrittenAsCurrent()
        {
            CompleteRoot(SampleEventIds.OfferRestOptionId);
            HandOffToMasatsuna();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData legacy = _session.CaptureSave();
            legacy.eventContentVersion = 1;

            SaveOperationResult loaded = _session.TryApplySave(legacy);

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(_session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
            Assert.That(_session.CaptureSave().eventContentVersion, Is.EqualTo(4));
        }

        [Test]
        public void ContentOneSaveWithUnknownPendingEventRejectsAtomically()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData legacy = _session.CaptureSave();
            legacy.eventContentVersion = 1;
            legacy.eventRuntime.pendingEventIds = new[] { "event.not-in-catalog" };
            GameDate date = _session.WorldClock.CurrentDate;
            string pending = _session.PendingEventId;

            SaveOperationResult loaded = _session.TryApplySave(legacy);

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.PendingEventId, Is.EqualTo(pending));
        }

        [Test]
        public void UnknownFutureContentVersionRejectsWithoutChangingTheSession()
        {
            HandOffToMasatsuna();
            SaveData future = _session.CaptureSave();
            future.eventContentVersion = SaveData.CurrentEventContentVersion + 1;
            GameDate date = _session.WorldClock.CurrentDate;

            SaveOperationResult loaded = _session.TryApplySave(future);

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(1));
        }
    }
}
