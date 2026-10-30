using System;
using System.Reflection;
using NUnit.Framework;
using System.Collections.Generic;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public class BranchingDialogueTests
    {
        private GameSession _session;

        [SetUp] public void SetUp() => _session = GameSession.NewGame();
        [TearDown] public void TearDown() { if (_session != null) UnityEngine.Object.DestroyImmediate(_session.gameObject); }

        [Test]
        public void SelectingConfiguredOptionMovesToItsTargetAndAppliesOnlyItsNpcEffectOnce()
        {
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            Assert.That(started.Succeeded, Is.True, started.Error);
            Assert.That(started.DialogueInstanceId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(started.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaOpeningNode));
            DialogueSelectionResult selected = _session.TrySelectDialogueOption(started.DialogueInstanceId, BranchingDialogueCatalog.KurodaOpeningNode, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(selected.Succeeded, Is.True, selected.Error);
            Assert.That(selected.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            // The trust target has no options, so selecting it ends the dialogue (C5/C7).
            Assert.That(selected.IsEnded, Is.True);
            Assert.That(selected.RelationshipEffectApplied, Is.True);
            Assert.That(selected.RelationshipResult.Succeeded, Is.True);
            Assert.That(selected.RelationshipResult.NpcId, Is.EqualTo(CharacterId.KurodaIori));
            Assert.That(selected.RelationshipResult.PreviousValue, Is.Zero);
            Assert.That(selected.RelationshipResult.CurrentValue, Is.EqualTo(10));
            AssertAllNpcRelationships(CharacterId.KurodaIori, 10);
            DialogueSelectionResult retry = _session.TrySelectDialogueOption(started.DialogueInstanceId, BranchingDialogueCatalog.KurodaOpeningNode, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(retry.Succeeded, Is.False);
            Assert.That(retry.Error, Is.Not.Empty);
            Assert.That(retry.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(retry.IsEnded, Is.True);
            Assert.That(retry.RelationshipEffectApplied, Is.False);
            Assert.That(retry.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            AssertAllNpcRelationships(CharacterId.KurodaIori, 10);
        }

        [Test]
        public void DifferentChoiceReachesDistinctNodeWithoutChangingRelationship()
        {
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult selected = _session.TrySelectDialogueOption(started.DialogueInstanceId, BranchingDialogueCatalog.KurodaOpeningNode, BranchingDialogueCatalog.KurodaDeclineOption);
            Assert.That(selected.Succeeded, Is.True, selected.Error);
            Assert.That(selected.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaFarewellNode));
            Assert.That(selected.IsEnded, Is.True);
            Assert.That(selected.RelationshipEffectApplied, Is.False);
            Assert.That(selected.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            AssertAllNpcRelationships(default(CharacterId), 0);
            DialogueSelectionResult later = _session.TrySelectDialogueOption(started.DialogueInstanceId, selected.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(later.Succeeded, Is.False);
            Assert.That(later.Error, Is.Not.Empty);
            Assert.That(later.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaFarewellNode));
            Assert.That(later.IsEnded, Is.True);
            Assert.That(later.RelationshipEffectApplied, Is.False);
            Assert.That(later.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            AssertAllNpcRelationships(default(CharacterId), 0);
            DialogueStartResult offerStart = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult offer = _session.TrySelectDialogueOption(offerStart.DialogueInstanceId, offerStart.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(offer.Succeeded, Is.True, offer.Error);
            Assert.That(offer.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(offer.CurrentNodeId, Is.Not.EqualTo(selected.CurrentNodeId));
            AssertAllNpcRelationships(CharacterId.KurodaIori, 10);
        }

        [Test]
        public void InvalidNpcAndOptionFailWithoutStateChanges()
        {
            DialogueStartResult badNpc = _session.TryStartDialogue(CharacterId.Player);
            Assert.That(badNpc.Succeeded, Is.False);
            Assert.That(badNpc.DialogueInstanceId, Is.EqualTo(Guid.Empty));
            Assert.That(badNpc.Error, Is.Not.Empty);
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult badOption = _session.TrySelectDialogueOption(started.DialogueInstanceId, BranchingDialogueCatalog.KurodaOpeningNode, new DialogueOptionId("missing"));
            Assert.That(badOption.Succeeded, Is.False);
            Assert.That(badOption.Error, Is.Not.Empty);
            Assert.That(badOption.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaOpeningNode));
            Assert.That(badOption.RelationshipEffectApplied, Is.False);
            Assert.That(badOption.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.Zero);
        }

        [Test]
        public void EachStartCreatesIndependentInstanceAndEffectCanApplyAgain()
        {
            DialogueStartResult first = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueStartResult second = _session.TryStartDialogue(CharacterId.KurodaIori);
            Assert.That(first.DialogueInstanceId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(second.DialogueInstanceId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(first.DialogueInstanceId, Is.Not.EqualTo(second.DialogueInstanceId));
            DialogueSelectionResult firstChoice = _session.TrySelectDialogueOption(first.DialogueInstanceId, first.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(firstChoice.Succeeded, Is.True);
            Assert.That(firstChoice.RelationshipEffectApplied, Is.True);
            DialogueSelectionResult secondChoice = _session.TrySelectDialogueOption(second.DialogueInstanceId, second.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(secondChoice.Succeeded, Is.True);
            Assert.That(secondChoice.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(secondChoice.RelationshipEffectApplied, Is.True);
            Assert.That(secondChoice.RelationshipResult.PreviousValue, Is.EqualTo(10));
            Assert.That(secondChoice.RelationshipResult.CurrentValue, Is.EqualTo(20));
            AssertAllNpcRelationships(CharacterId.KurodaIori, 20);
        }

        [Test]
        public void CatalogDefinitionAndFreshInstancesRemainStableAfterSelection()
        {
            Assert.That(BranchingDialogueCatalog.TryGet(CharacterId.KurodaIori, out DialogueDefinition definition), Is.True);
            Assert.That(definition.StartNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaOpeningNode));
            Assert.That(definition.TryGetNode(BranchingDialogueCatalog.KurodaOpeningNode, out DialogueNode opening), Is.True);
            Assert.That(definition.TryGetNode(BranchingDialogueCatalog.KurodaTrustNode, out DialogueNode trust), Is.True);
            Assert.That(definition.TryGetNode(BranchingDialogueCatalog.KurodaFarewellNode, out DialogueNode farewell), Is.True);
            Assert.That(opening.Text, Is.EqualTo("A proposition."));
            Assert.That(opening.Options.Count, Is.EqualTo(2));
            Assert.That(opening.Options[0].Id, Is.EqualTo(BranchingDialogueCatalog.KurodaOfferOption));
            Assert.That(opening.Options[0].TargetNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(opening.Options[0].EffectTarget, Is.EqualTo(CharacterId.KurodaIori));
            Assert.That(opening.Options[0].RelationshipDelta, Is.EqualTo(10));
            Assert.That(opening.Options[1].Id, Is.EqualTo(BranchingDialogueCatalog.KurodaDeclineOption));
            Assert.That(opening.Options[1].TargetNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaFarewellNode));
            Assert.That(trust.Text, Is.EqualTo("Trust is earned."));
            Assert.That(trust.Options.Count, Is.Zero);
            Assert.That(farewell.Text, Is.EqualTo("Farewell."));
            Assert.That(farewell.Options.Count, Is.Zero);

            DialogueStartResult first = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueStartResult second = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult firstChoice = _session.TrySelectDialogueOption(first.DialogueInstanceId, first.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(firstChoice.Succeeded, Is.True, firstChoice.Error);
            Assert.That(firstChoice.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(firstChoice.IsEnded, Is.True);

            Assert.That(BranchingDialogueCatalog.TryGet(CharacterId.KurodaIori, out DialogueDefinition afterSelection), Is.True);
            Assert.That(afterSelection.StartNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaOpeningNode));
            Assert.That(afterSelection.TryGetNode(BranchingDialogueCatalog.KurodaOpeningNode, out DialogueNode openingAfter), Is.True);
            Assert.That(openingAfter.Text, Is.EqualTo("A proposition."));
            Assert.That(openingAfter.Options.Count, Is.EqualTo(2));
            Assert.That(openingAfter.Options[0].Id, Is.EqualTo(BranchingDialogueCatalog.KurodaOfferOption));
            Assert.That(openingAfter.Options[0].TargetNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(openingAfter.Options[0].EffectTarget, Is.EqualTo(CharacterId.KurodaIori));
            Assert.That(openingAfter.Options[0].RelationshipDelta, Is.EqualTo(10));
            Assert.That(openingAfter.Options[1].Id, Is.EqualTo(BranchingDialogueCatalog.KurodaDeclineOption));
            Assert.That(openingAfter.Options[1].TargetNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaFarewellNode));
            Assert.That(afterSelection.TryGetNode(BranchingDialogueCatalog.KurodaTrustNode, out DialogueNode trustAfter), Is.True);
            Assert.That(trustAfter.Text, Is.EqualTo("Trust is earned."));
            Assert.That(trustAfter.Options.Count, Is.Zero);
            Assert.That(afterSelection.TryGetNode(BranchingDialogueCatalog.KurodaFarewellNode, out DialogueNode farewellAfter), Is.True);
            Assert.That(farewellAfter.Text, Is.EqualTo("Farewell."));
            Assert.That(farewellAfter.Options.Count, Is.Zero);

            DialogueSelectionResult secondChoice = _session.TrySelectDialogueOption(second.DialogueInstanceId, second.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(secondChoice.Succeeded, Is.True, secondChoice.Error);
            Assert.That(secondChoice.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(secondChoice.RelationshipEffectApplied, Is.True);
            Assert.That(secondChoice.RelationshipResult.NpcId, Is.EqualTo(CharacterId.KurodaIori));
            Assert.That(secondChoice.RelationshipResult.PreviousValue, Is.EqualTo(10));
            Assert.That(secondChoice.RelationshipResult.CurrentValue, Is.EqualTo(20));
            AssertAllNpcRelationships(CharacterId.KurodaIori, 20);
        }

        [Test]
        public void DialogueNodeDefensivelyCopiesConstructorOptionArray()
        {
            var original = new DialogueOption(new DialogueOptionId("original"), "Original", "target.original", CharacterId.KurodaIori, 10);
            var replacement = new DialogueOption(new DialogueOptionId("replacement"), "Replacement", "target.replacement", CharacterId.TachibanaSuzu, -5);
            DialogueOption[] suppliedOptions = { original };

            var node = new DialogueNode("fixture.immutable", "Immutable", suppliedOptions);
            suppliedOptions[0] = replacement;

            Assert.That(node.Options.Count, Is.EqualTo(1));
            Assert.That(node.Options[0].Id, Is.EqualTo(new DialogueOptionId("original")));
            Assert.That(node.Options[0].Text, Is.EqualTo("Original"));
            Assert.That(node.Options[0].TargetNodeId, Is.EqualTo("target.original"));
            Assert.That(node.Options[0].EffectTarget, Is.EqualTo(CharacterId.KurodaIori));
            Assert.That(node.Options[0].RelationshipDelta, Is.EqualTo(10));
        }

        [Test]
        public void DialogueNodeAndCatalogOptionsRejectIListMutation()
        {
            var original = new DialogueOption(new DialogueOptionId("original"), "Original", "target.original", CharacterId.KurodaIori, 10);
            var replacement = new DialogueOption(new DialogueOptionId("replacement"), "Replacement", "target.replacement", CharacterId.TachibanaSuzu, -5);
            var node = new DialogueNode("fixture.immutable", "Immutable", original);
            IList<DialogueOption> nodeOptions = node.Options as IList<DialogueOption>;

            Assert.That(nodeOptions, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => nodeOptions[0] = replacement);
            Assert.That(node.Options[0].Id, Is.EqualTo(new DialogueOptionId("original")));
            Assert.That(node.Options[0].RelationshipDelta, Is.EqualTo(10));

            Assert.That(BranchingDialogueCatalog.TryGet(CharacterId.KurodaIori, out DialogueDefinition definition), Is.True);
            Assert.That(definition.TryGetNode(BranchingDialogueCatalog.KurodaOpeningNode, out DialogueNode catalogNode), Is.True);
            IList<DialogueOption> catalogOptions = catalogNode.Options as IList<DialogueOption>;

            Assert.That(catalogOptions, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => catalogOptions[0] = replacement);
            Assert.That(catalogNode.Options.Count, Is.EqualTo(2));
            Assert.That(catalogNode.Options[0].Id, Is.EqualTo(BranchingDialogueCatalog.KurodaOfferOption));
            Assert.That(catalogNode.Options[0].TargetNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(catalogNode.Options[0].EffectTarget, Is.EqualTo(CharacterId.KurodaIori));
            Assert.That(catalogNode.Options[0].RelationshipDelta, Is.EqualTo(10));
        }

        [Test]
        public void TerminalCatalogNodesExposeNoOptions()
        {
            Assert.That(BranchingDialogueCatalog.TryGet(CharacterId.KurodaIori, out DialogueDefinition definition), Is.True);
            Assert.That(definition.TryGetNode(BranchingDialogueCatalog.KurodaTrustNode, out DialogueNode trust), Is.True);
            Assert.That(definition.TryGetNode(BranchingDialogueCatalog.KurodaFarewellNode, out DialogueNode farewell), Is.True);
            Assert.That(trust.Options.Count, Is.Zero);
            Assert.That(farewell.Options.Count, Is.Zero);
        }

        [Test]
        public void EmptyInstanceAndWrongNodeFailWithoutMutatingState()
        {
            DialogueSelectionResult empty = _session.TrySelectDialogueOption(System.Guid.Empty, BranchingDialogueCatalog.KurodaOpeningNode, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(empty.Succeeded, Is.False);
            Assert.That(empty.Error, Is.Not.Empty);
            Assert.That(empty.CurrentNodeId, Is.Empty);
            Assert.That(empty.IsEnded, Is.False);
            Assert.That(empty.RelationshipEffectApplied, Is.False);
            Assert.That(empty.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult wrong = _session.TrySelectDialogueOption(started.DialogueInstanceId, "not-current", BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(wrong.Succeeded, Is.False);
            Assert.That(wrong.Error, Is.Not.Empty);
            Assert.That(wrong.CurrentNodeId, Is.EqualTo(started.CurrentNodeId));
            Assert.That(wrong.IsEnded, Is.False);
            Assert.That(wrong.RelationshipEffectApplied, Is.False);
            Assert.That(wrong.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            AssertAllNpcRelationships(default(CharacterId), 0);
        }

        [Test]
        public void RandomUnknownNodeIdOnValidInstanceFailsWithoutMutation()
        {
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            Assert.That(started.Succeeded, Is.True, started.Error);

            string invalidNodeId;
            do
            {
                invalidNodeId = Guid.NewGuid().ToString("N");
            }
            while (invalidNodeId == BranchingDialogueCatalog.KurodaOpeningNode ||
                   invalidNodeId == BranchingDialogueCatalog.KurodaTrustNode ||
                   invalidNodeId == BranchingDialogueCatalog.KurodaFarewellNode);

            DialogueSelectionResult result = _session.TrySelectDialogueOption(started.DialogueInstanceId, invalidNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.Not.Empty);
            Assert.That(result.CurrentNodeId, Is.EqualTo(started.CurrentNodeId));
            Assert.That(result.IsEnded, Is.False);
            Assert.That(result.RelationshipEffectApplied, Is.False);
            Assert.That(result.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            AssertAllNpcRelationships(default(CharacterId), 0);
        }

        [Test]
        public void UnknownNpcFailsWithoutCreatingUsableInstance()
        {
            DialogueStartResult bad = _session.TryStartDialogue(new CharacterId("npc.unknown"));
            Assert.That(bad.Succeeded, Is.False);
            Assert.That(bad.DialogueInstanceId, Is.EqualTo(System.Guid.Empty));
            Assert.That(bad.Error, Is.Not.Empty);
        }

        [Test]
        public void TerminalTrustNodeRejectsFurtherSelection()
        {
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult reached = _session.TrySelectDialogueOption(started.DialogueInstanceId, started.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(reached.Succeeded, Is.True);
            Assert.That(reached.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(reached.IsEnded, Is.True);
            DialogueSelectionResult later = _session.TrySelectDialogueOption(started.DialogueInstanceId, reached.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);
            Assert.That(later.Succeeded, Is.False);
            Assert.That(later.Error, Is.Not.Empty);
            Assert.That(later.CurrentNodeId, Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(later.IsEnded, Is.True);
            Assert.That(later.RelationshipEffectApplied, Is.False);
            Assert.That(later.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            AssertAllNpcRelationships(CharacterId.KurodaIori, 10);
        }

        [Test]
        public void DeclineLeavesEveryNpcRelationshipUnchanged()
        {
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult decline = _session.TrySelectDialogueOption(started.DialogueInstanceId, started.CurrentNodeId, BranchingDialogueCatalog.KurodaDeclineOption);
            Assert.That(decline.Succeeded, Is.True);
            foreach (CharacterDefinition npc in CharacterCatalog.Npcs)
                Assert.That(_session.TryGetRelationship(npc.Id).CurrentValue, Is.Zero, npc.Id.ToString());
        }

        [Test]
        public void DanglingTargetFailsWithoutAdvancingOrApplyingRelationshipEffect()
        {
            const string startNode = "fixture.dangling.start";
            const string missingTarget = "fixture.dangling.missing";
            var optionId = new DialogueOptionId("dangling");
            var definition = new DialogueDefinition(startNode,
                new DialogueNode(startNode, "Start", new DialogueOption(optionId, "Choose", missingTarget, CharacterId.KurodaIori, 10)),
                new DialogueNode("fixture.dangling.other", "Other"));
            Guid instanceId = InstallDialogue(definition, startNode);

            DialogueSelectionResult result = _session.TrySelectDialogueOption(instanceId, startNode, optionId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.Not.Empty);
            Assert.That(result.CurrentNodeId, Is.EqualTo(startNode));
            Assert.That(result.IsEnded, Is.False);
            Assert.That(result.RelationshipEffectApplied, Is.False);
            Assert.That(result.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
            AssertAllNpcRelationships(default(CharacterId), 0);
        }

        [Test]
        public void InvalidEffectCharacterFailsWithoutAdvancingOrPartiallyCommitting()
        {
            const string startNode = "fixture.invalid-effect.start";
            const string targetNode = "fixture.invalid-effect.end";
            var optionId = new DialogueOptionId("invalid-effect");
            var invalidNpc = new CharacterId("npc.unknown");
            var definition = new DialogueDefinition(startNode,
                new DialogueNode(startNode, "Start", new DialogueOption(optionId, "Choose", targetNode, invalidNpc, 10)),
                new DialogueNode(targetNode, "End"));
            Guid instanceId = InstallDialogue(definition, startNode);

            DialogueSelectionResult result = _session.TrySelectDialogueOption(instanceId, startNode, optionId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.Not.Empty);
            Assert.That(result.CurrentNodeId, Is.EqualTo(startNode));
            Assert.That(result.IsEnded, Is.False);
            Assert.That(result.RelationshipEffectApplied, Is.False);
            Assert.That(result.RelationshipResult.Succeeded, Is.False);
            Assert.That(result.RelationshipResult.NpcId, Is.EqualTo(invalidNpc));
            Assert.That(result.RelationshipResult.PreviousValue, Is.Zero);
            Assert.That(result.RelationshipResult.CurrentValue, Is.Zero);
            Assert.That(result.RelationshipResult.Error, Is.EqualTo(result.Error));
            AssertAllNpcRelationships(default(CharacterId), 0);
        }

        [TestCase(95, 10, 100)]
        [TestCase(-95, -10, -100)]
        public void DialogueEffectsClampAtBothRelationshipBounds(int initialValue, int delta, int expectedValue)
        {
            RelationshipResult seeded = _session.TryChangeRelationship(CharacterId.KurodaIori, initialValue);
            Assert.That(seeded.Succeeded, Is.True);
            Assert.That(seeded.CurrentValue, Is.EqualTo(initialValue));

            const string startNode = "fixture.clamp.start";
            const string targetNode = "fixture.clamp.end";
            var optionId = new DialogueOptionId("clamp");
            var definition = new DialogueDefinition(startNode,
                new DialogueNode(startNode, "Start", new DialogueOption(optionId, "Choose", targetNode, CharacterId.KurodaIori, delta)),
                new DialogueNode(targetNode, "End"));
            Guid instanceId = InstallDialogue(definition, startNode);

            DialogueSelectionResult result = _session.TrySelectDialogueOption(instanceId, startNode, optionId);

            Assert.That(result.Succeeded, Is.True, result.Error);
            Assert.That(result.CurrentNodeId, Is.EqualTo(targetNode));
            Assert.That(result.IsEnded, Is.True);
            Assert.That(result.RelationshipEffectApplied, Is.True);
            Assert.That(result.RelationshipResult.PreviousValue, Is.EqualTo(initialValue));
            Assert.That(result.RelationshipResult.CurrentValue, Is.EqualTo(expectedValue));
            AssertAllNpcRelationships(CharacterId.KurodaIori, expectedValue);
        }

        private Guid InstallDialogue(DialogueDefinition definition, string currentNodeId)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo dialoguesField = typeof(GameSession).GetField("_dialogues", flags);
            Type stateType = typeof(GameSession).GetNestedType("DialogueState", BindingFlags.NonPublic);
            Assert.That(dialoguesField, Is.Not.Null);
            Assert.That(stateType, Is.Not.Null);

            object state = Activator.CreateInstance(stateType, true);
            const BindingFlags stateFlags = BindingFlags.Instance | BindingFlags.Public;
            stateType.GetField("CharacterId", stateFlags).SetValue(state, CharacterId.KurodaIori);
            stateType.GetField("Definition", stateFlags).SetValue(state, definition);
            stateType.GetField("CurrentNodeId", stateFlags).SetValue(state, currentNodeId);
            stateType.GetField("Ended", stateFlags).SetValue(state, false);

            Guid instanceId = Guid.NewGuid();
            object dialogues = dialoguesField.GetValue(_session);
            // Install malformed fixtures without changing the production catalog or adding a test-only production API.
            dialogues.GetType().GetMethod("Add").Invoke(dialogues, new object[] { instanceId, state });
            return instanceId;
        }

        private void AssertAllNpcRelationships(CharacterId changedNpc, int expectedChangedValue)
        {
            foreach (CharacterDefinition npc in CharacterCatalog.Npcs)
            {
                int expected = npc.Id == changedNpc ? expectedChangedValue : 0;
                Assert.That(_session.TryGetRelationship(npc.Id).CurrentValue, Is.EqualTo(expected), npc.Id.ToString());
            }
        }

        [TestCase(101, 100)]
        [TestCase(-101, -100)]
        [TestCase(int.MaxValue, 100)]
        [TestCase(int.MinValue, -100)]
        public void SessionRelationshipChangesDelegateToM302Clamp(int delta, int expected)
        {
            RelationshipResult changed = _session.TryChangeRelationship(CharacterId.KurodaIori, delta);
            Assert.That(changed.Succeeded, Is.True);
            Assert.That(changed.CurrentValue, Is.EqualTo(expected));
            Assert.That(changed.CurrentValue, Is.InRange(-100, 100));
        }
    }
}
