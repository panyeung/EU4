using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 6: one monthly record, one personnel record, readable labels.</summary>
    public sealed class IndustryChronicleTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();
            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitNewGame();
            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        private FamilyChroniclePanel Panel() =>
            Find(AddSC012FamilyChroniclePanel.RootName).GetComponent<FamilyChroniclePanel>();

        private GameObject Create(string name, Transform parent, params System.Type[] types)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (System.Type type in types)
                go.AddComponent(type);
            return go;
        }

        private Transform Find(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .First(transform => transform.name == name);

        private FamilyChronicleEntry MonthlyEntry() =>
            _session.ChronicleEntries.FirstOrDefault(entry =>
                entry.Category == ChronicleCategory.Industry && entry.SubjectId == "industry.monthly");

        [Test]
        public void IndustrySettlement_AppendsOneMonthlyEntryWithBeforeAfter()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            FamilyChronicleEntry entry = MonthlyEntry();
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Title, Is.EqualTo("产业月结"));
            Assert.That(entry.SourceId, Is.EqualTo("settlement"));
            Assert.That(entry.FromValue, Is.EqualTo(30));
            Assert.That(entry.ToValue, Is.EqualTo(31));
            Assert.That(entry.Year, Is.EqualTo(1560));
            Assert.That(entry.Month, Is.EqualTo(4));
            Assert.That(entry.Summary, Does.Contain("净 +1资金/+3物资"));
            Assert.That(_session.ChronicleEntries.Count(e => e.SubjectId == "industry.monthly"),
                Is.EqualTo(1));
        }

        [Test]
        public void VacantMonth_AppendsNothing()
        {
            int before = _session.ChronicleEntries.Count;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(before));
            Assert.That(_session.LastIndustryReport.AnyStaffed, Is.False);
        }

        [Test]
        public void IndustrySettlement_AtCapRecordsNetAndActualZero()
        {
            IndustryState state = IndustryState.CreateInitial(new GameDate(1560, 4, 1));
            IndustryResources.TryCreate(30, 999999, 0, out IndustryResources resources, out _);
            state.TrySetResources(resources, out _);
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            IndustryService.TryAssign(state, succession, new GameDate(1560, 4, 1),
                IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession,
                new GameDate(1560, 4, 1));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Report.Summary, Does.Contain("净产 3"));
            Assert.That(result.Report.Summary, Does.Contain("已达上限"));
            var ledger = HistoryLedger.CreateEmpty();
            Assert.That(ledger.TryAppendIndustry(1560, 4, "industry.monthly", "settlement",
                result.Report.Before.Funds, result.Report.After.Funds, "产业月结",
                result.Report.Summary, out FamilyChronicleEntry entry, out string error), Is.True, error);
            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Industry));
            Assert.That(entry.Summary, Does.Contain("已达上限"));
        }

        [Test]
        public void SuccessionVacancy_AppendsOnePersonnelEntry()
        {
            var ledger = HistoryLedger.CreateEmpty();
            Assert.That(ledger.TryAppendIndustry(1560, 4, "industry.assignments", "succession",
                0, 0, "产业人事", "晴信退休，农庄负责人自动解除；商馆仍由千恵负责。",
                out FamilyChronicleEntry entry, out string error), Is.True, error);
            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Industry));
            Assert.That(entry.SourceId, Is.EqualTo("succession"));
            Assert.That(entry.FromValue, Is.EqualTo(0));
            Assert.That(entry.ToValue, Is.EqualTo(0));
        }

        [Test]
        public void IndustryChronicle_RoundTripsAndRendersCategoryLabel()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData data = _session.CaptureSave();
            Assert.That(SaveDataMigrator.TryMigrate(data, null, out SaveData migrated,
                out SaveOperationResult result), Is.True, result.Message);
            ChronicleSaveEntry restored = migrated.chronicle
                .Where(entry => entry.category == (int)ChronicleCategory.Industry)
                .FirstOrDefault(entry => entry.subjectId == "industry.monthly");
            Assert.That(restored, Is.Not.Null);

            FamilyChroniclePanel panel = Panel();
            panel.Open();
            panel.Refresh();

            Assert.That(panel.RowText(0), Does.Contain("产业"));
            Assert.That(panel.RowText(0), Does.Contain("资金 30 → 31"));
        }

        [Test]
        public void IndustryChronicle_DoesNotExposeRawIds()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            FamilyChroniclePanel panel = Panel();
            panel.Open();
            panel.Refresh();

            Assert.That(panel.RowText(0), Does.Not.Contain("industry."));
            Assert.That(panel.RowText(0), Does.Not.Contain("person.sc013"));
        }
    }
}
