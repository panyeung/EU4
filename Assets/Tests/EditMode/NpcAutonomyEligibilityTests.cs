using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M03 confirmed eligibility rules at session level: an injury, an existing industry
    /// duty and an empty supply stock reject the blocked candidate before ranking, keep
    /// Rest available, spend no resources, and stay visible in the read-only Inspector.
    /// </summary>
    public sealed class NpcAutonomyEligibilityTests
    {
        private readonly List<GameSession> _sessions = new List<GameSession>();
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameSession session in _sessions)
                if (session != null)
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
            _sessions.Clear();
            foreach (GameObject go in _objects)
                if (go != null)
                    UnityEngine.Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private GameSession NewSession(ulong seed = GameSession.DefaultRandomSeed)
        {
            var go = Track(new GameObject(nameof(GameSession)));
            GameSession session = go.AddComponent<GameSession>();
            session.InitNewGame(seed);
            _sessions.Add(session);
            return session;
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static NpcAutonomySnapshot Snapshot(GameSession session, PersonId personId)
        {
            Assert.That(session.TryGetNpcAutonomy(personId, out NpcAutonomySnapshot snapshot),
                Is.True, "missing snapshot for " + personId);
            return snapshot;
        }

        [Test]
        public void InjuredPilot_ReplansToRestAndRejectsTrainingAndWork()
        {
            GameSession session = NewSession();
            session.SetInjuredForTest(M2FixedScenario.NobumasaId, true);

            Assert.That(session.TryInterruptDailyAction(M2FixedScenario.NobumasaId,
                "伤势需要休养。"), Is.True, session.LastDailyActionMessage);

            NpcAutonomySnapshot snapshot = Snapshot(session, M2FixedScenario.NobumasaId);
            Assert.That(snapshot.Needs, Is.EqualTo(new NpcNeedValues(45, 40, 75)),
                "interruption does not change needs");
            Assert.That(snapshot.HasActiveAction, Is.True);
            Assert.That(snapshot.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(snapshot.Status, Is.EqualTo(NpcAutonomyStatus.Executing));

            NpcAutonomyEvaluation latest = snapshot.LatestEvaluation;
            Assert.That(latest.HasSelection, Is.True);
            Assert.That(latest.Selected.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(latest.Candidates[0].IsEligible, Is.True,
                "an injured pilot may still select Rest");
            Assert.That(latest.Candidates[1].IsEligible, Is.False);
            Assert.That(latest.Candidates[2].IsEligible, Is.False);
            Assert.That(latest.Candidates[1].Explanation, Does.Contain("伤员"));
            Assert.That(latest.Candidates[2].Explanation, Does.Contain("伤员"));
            Assert.That(snapshot.RecentDecisions[0].Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Accepted));
            Assert.That(snapshot.RecentDecisions[0].Action, Is.EqualTo(DailyActionKind.Rest));
        }

        [Test]
        public void IndustryDuty_RejectsWorkBeforeRankingAndInspectorShowsTheReason()
        {
            GameSession session = NewSession();
            Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.NobumasaId).Succeeded, Is.True, session.LastIndustryMessage);

            Assert.That(session.TryInterruptDailyAction(M2FixedScenario.NobumasaId,
                "产业职务优先。"), Is.True, session.LastDailyActionMessage);

            NpcAutonomySnapshot snapshot = Snapshot(session, M2FixedScenario.NobumasaId);
            Assert.That(snapshot.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Rest));

            NpcActionCandidate work = snapshot.LatestEvaluation.Candidates[2];
            Assert.That(work.Kind, Is.EqualTo(DailyActionKind.Work));
            Assert.That(work.IsEligible, Is.False);
            Assert.That(work.Score, Is.EqualTo(138),
                "the blocked Work candidate still reports its diagnostic score: base 150 " +
                "plus the -12 interruption memory recorded by the replan that produced it");
            Assert.That(work.MemoryAdjustment,
                Is.EqualTo(NpcAutonomyRules.InterruptedMemoryAdjustment),
                "the interruption memory applies to Work even though the duty rule blocks it");
            Assert.That(work.Explanation, Does.Contain("已担任产业职务"));
            Assert.That(snapshot.LatestEvaluation.Selected.Kind,
                Is.EqualTo(DailyActionKind.Rest));
            Assert.That(snapshot.LatestEvaluation.Selected.Score, Is.EqualTo(45));
            Assert.That(snapshot.LatestEvaluation.Selected.Score, Is.LessThan(work.Score),
                "the eligible fallback wins even though blocked Work scored higher");
            Assert.That(session.NpcAutonomySnapshots.Count, Is.EqualTo(2),
                "autonomy stays limited to exactly the two approved pilots");

            NpcAiInspectorPanel panel = NewInspectorPanel(session);
            panel.Open();
            Assert.That(panel.SelectPilot(M2FixedScenario.NobumasaId), Is.True);
            Assert.That(panel.CandidatesLine, Does.Contain("排除：已担任产业职务"),
                "the read-only Inspector must explain why Work was rejected");
            Assert.That(panel.CandidatesLine, Does.Contain("工作"));
        }

        [Test]
        public void ZeroSupplies_RejectsWorkWithoutSpendingResources()
        {
            GameSession source = NewSession();
            SaveData saved = source.CaptureSave();
            saved.industry.supplies = 0;
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True);

            GameSession restored = NewSession(11UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(restored.Industry.Resources.Supplies, Is.Zero);
            Assert.That(restored.HasActiveDailyAction(M2FixedScenario.NobumasaId), Is.True);

            Assert.That(restored.TryInterruptDailyAction(M2FixedScenario.NobumasaId,
                "物资不足，改做休整。"), Is.True, restored.LastDailyActionMessage);

            NpcAutonomySnapshot snapshot = Snapshot(restored, M2FixedScenario.NobumasaId);
            Assert.That(snapshot.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Rest));
            NpcActionCandidate work = snapshot.LatestEvaluation.Candidates[2];
            Assert.That(work.IsEligible, Is.False);
            Assert.That(work.Explanation, Does.Contain("物资为 0"));
            Assert.That(snapshot.LatestEvaluation.Selected.Score, Is.EqualTo(45),
                "Rest 45 beats Training 40 once blocked Work is out of the ranking");
            Assert.That(restored.Industry.Resources.Supplies, Is.Zero,
                "Work must never consume supplies");
            Assert.That(restored.Industry.Resources.Funds, Is.EqualTo(30),
                "no other industry resource may be charged for Work");

            Assert.That(restored.AdvanceOneDay(), Is.True);
            NpcAutonomySnapshot next = Snapshot(restored, M2FixedScenario.NobumasaId);
            Assert.That(next.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Training),
                "the next replan still falls back to an eligible action, never Work");
            Assert.That(next.LatestEvaluation.Candidates[2].IsEligible, Is.False);
            Assert.That(restored.Industry.Resources.Supplies, Is.Zero,
                "repeated replanning with zero supplies still spends nothing");
        }

        private NpcAiInspectorPanel NewInspectorPanel(GameSession session)
        {
            GameObject panelObject = Track(new GameObject("M03EligibilityPanel"));
            NpcAiInspectorPanel panel = panelObject.AddComponent<NpcAiInspectorPanel>();
            GameObject root = Track(new GameObject("M03EligibilityPanelRoot"));
            GameObject scrollObject = Track(new GameObject("Scroll", typeof(RectTransform)));
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            RectTransform content = Track(new GameObject("Content", typeof(RectTransform)))
                .GetComponent<RectTransform>();
            content.SetParent(scrollObject.transform, false);
            scroll.content = content;
            panel.BindReferences(session, root, NewButton("Open"), NewButton("Close"),
                NewButton("Masatsuna"), NewButton("Nobumasa"), NewText("Title"),
                NewText("Summary"), NewText("Needs"), NewText("Status"), NewText("Candidates"),
                NewText("Log"), scroll, content);
            Assert.That(panel.IsBound, Is.True);
            return panel;
        }

        private Button NewButton(string name)
        {
            var go = Track(new GameObject(name, typeof(RectTransform)));
            return go.AddComponent<Button>();
        }

        private Text NewText(string name)
        {
            var go = Track(new GameObject(name, typeof(RectTransform)));
            return go.AddComponent<Text>();
        }
    }
}
