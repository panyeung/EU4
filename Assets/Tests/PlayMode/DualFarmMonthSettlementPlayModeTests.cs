using System.Collections;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SengokuChronicle.PlayModeTests
{
    /// <summary>
    /// M08B option B runtime proof for the two-post farm.
    ///
    /// It loads the shipped <c>FoundationScene</c> and drives only the authored player
    /// path: 开始新游戏 → 固定三代剧本（SC-013） → 家族内务 → 产业, appoints both farm
    /// posts through the panel's own candidate/confirm buttons, returns to the map page
    /// and presses the real 结束月份 button. It then reads the committed month report and
    /// the panel's own labels to prove both farm lines harvested.
    ///
    /// Nothing here writes a save slot, saves the scene or mutates session state directly.
    /// </summary>
    public sealed class DualFarmMonthSettlementPlayModeTests
    {
        private const string FarmId = IndustryCatalog.FarmId;
        private const string SceneName = "FoundationScene";

        // SC-013 fixed new game: 1560-04-01, six living members at Kiyosu,
        // opening industry budget 30 funds / 12 supplies / 0 intelligence and
        // opening food stock 2 (one month of demand for six people).
        private const int OpeningFunds = 30;
        private const int OpeningSupplies = 12;
        private const int OpeningFood = 2;
        private const int HouseholdMembers = 6;

        [UnityTest]
        public IEnumerator BothFarmPosts_StaffedThroughPanels_ProduceThroughFormalMonthTurn()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneName),
                "PlayMode proof must run on the shipped FoundationScene.");

            GameSession session = UnityEngine.Object
                .FindObjectsByType<GameSession>(FindObjectsInactive.Include).FirstOrDefault();
            Assert.That(session, Is.Not.Null, "FoundationScene must own a GameSession.");

            // Real title entry: 开始新游戏 then 固定三代剧本（SC-013）.
            ClickActiveButton("M6NewGameButton");
            yield return null;
            ClickActiveButton("SC013FixedScenarioButton");
            yield return null;
            yield return null;

            Assert.That(session.WorldClock.CurrentDate.Year, Is.EqualTo(1560));
            Assert.That(session.WorldClock.CurrentDate.Month, Is.EqualTo(4));
            Assert.That(session.WorldClock.CurrentDate.Day, Is.EqualTo(1),
                "SC-013 must start on 1560-04-01.");

            // Real page entry: 家族内务 embeds the authored industry panel in the
            // household composition and shows it. The legacy 产业 launcher stays hidden
            // on that page by design, so the panel itself is the entry the player gets.
            ClickActiveButton("ReferenceNavHouseholdButton");
            yield return null;

            IndustryPanel panel = UnityEngine.Object
                .FindObjectsByType<IndustryPanel>(FindObjectsInactive.Include).FirstOrDefault();
            Assert.That(panel, Is.Not.Null, "FoundationScene must own the authored IndustryPanel.");
            panel.Refresh();
            Assert.That(panel.IsOpen, Is.True,
                "家族内务 must present the authored industry panel.");

            Assert.That(panel.FarmCandidateCount, Is.GreaterThanOrEqualTo(2),
                "Two farm posts need at least two workable candidates at the new game.");
            Assert.That(session.Industry.Resources.Funds, Is.EqualTo(OpeningFunds));
            Assert.That(session.Industry.Resources.Supplies, Is.EqualTo(OpeningSupplies));
            Assert.That(session.Market.FoodInventory, Is.EqualTo(OpeningFood));

            // First post through the panel's candidate button and confirm button.
            int candidatesBefore = panel.FarmCandidateCount;
            panel.ClickFarmCandidate(0);
            Assert.That(panel.HasPendingAssignment, Is.True);
            panel.ClickConfirm();
            yield return null;
            panel.Refresh();

            IndustrySlotView post0 = session.Industry.Slot(FarmId, 0);
            IndustrySlotView post1 = session.Industry.Slot(FarmId, 1);
            Assert.That(post0.AssigneeId.IsEmpty, Is.False, "First confirm must staff farm post 0.");
            Assert.That(post1.AssigneeId.IsEmpty, Is.True, "First confirm must not fill farm post 1.");
            Assert.That(panel.FarmCandidateCount, Is.EqualTo(candidatesBefore - 1));

            // Second post: the panel fills the remaining vacant farm post.
            panel.ClickFarmCandidate(0);
            Assert.That(panel.HasPendingAssignment, Is.True);
            panel.ClickConfirm();
            yield return null;
            panel.Refresh();

            post0 = session.Industry.Slot(FarmId, 0);
            post1 = session.Industry.Slot(FarmId, 1);
            Assert.That(post1.AssigneeId.IsEmpty, Is.False, "Second confirm must staff farm post 1.");
            Assert.That(post0.AssigneeId.Value, Is.Not.EqualTo(post1.AssigneeId.Value),
                "The two farm posts must be held by different people.");
            Assert.That(post0.Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(post1.Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(panel.FarmStatus, Is.EqualTo("一号：运作中；二号：运作中"));
            StringAssert.Contains(post0.AssigneeName, panel.FarmAssignee);
            StringAssert.Contains(post1.AssigneeName, panel.FarmAssignee);

            // Back to the map page, then the real 结束月份 button runs the month transaction.
            ClickActiveButton("ReferenceNavMapButton");
            yield return null;
            ClickActiveButton("EndMonthButton");
            yield return null;
            yield return null;

            Assert.That(session.WorldClock.CurrentDate.Year, Is.EqualTo(1560));
            Assert.That(session.WorldClock.CurrentDate.Month, Is.EqualTo(5));
            Assert.That(session.WorldClock.CurrentDate.Day, Is.EqualTo(1),
                "结束月份 must move SC-013 to 1560-05-01.");

            IndustryMonthReport report = session.LastIndustryReport;
            Assert.That(report, Is.Not.Null, "The month turn must commit an industry report.");
            Assert.That(report.Lines.Count, Is.EqualTo(IndustryCatalog.TotalPosts));

            IndustryLineResult[] farmLines = report.Lines
                .Where(line => line.IndustryId == FarmId)
                .OrderBy(line => line.PostIndex)
                .ToArray();
            Assert.That(farmLines.Length, Is.EqualTo(2), "The farm must settle two production lines.");
            Assert.That(farmLines[0].PostIndex, Is.EqualTo(0));
            Assert.That(farmLines[1].PostIndex, Is.EqualTo(1));
            Assert.That(farmLines[0].Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(farmLines[1].Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(farmLines[0].AssigneeId.Value, Is.EqualTo(post0.AssigneeId.Value));
            Assert.That(farmLines[1].AssigneeId.Value, Is.EqualTo(post1.AssigneeId.Value));
            Assert.That(farmLines[0].GrossOutputFood, Is.EqualTo(4),
                "Each staffed farm post harvests 4 food.");
            Assert.That(farmLines[1].GrossOutputFood, Is.EqualTo(4));
            Assert.That(farmLines[0].MaterialsCostFunds, Is.EqualTo(1));
            Assert.That(farmLines[1].MaterialsCostFunds, Is.EqualTo(1));

            // Industry resources follow the two farm lines from the true opening stock:
            // funds +2 -2 material cost, supplies +6, intelligence unchanged.
            Assert.That(report.Before.Funds, Is.EqualTo(OpeningFunds));
            Assert.That(report.Before.Supplies, Is.EqualTo(OpeningSupplies));
            Assert.That(report.Before.Intelligence, Is.EqualTo(0));
            Assert.That(report.After.Funds, Is.EqualTo(32));
            Assert.That(report.After.Supplies, Is.EqualTo(18));
            Assert.That(report.After.Intelligence, Is.EqualTo(0));

            // Market settlement consumes the month-start demand from stock + both harvests.
            MarketMonthReport market = session.LastMarketReport;
            Assert.That(market, Is.Not.Null, "The month turn must commit a market report.");
            Assert.That(market.HouseholdMembers, Is.EqualTo(HouseholdMembers));
            Assert.That(market.FoodDemand, Is.EqualTo(2));
            Assert.That(market.FoodProduced, Is.EqualTo(8), "Both farm lines must feed the market.");
            Assert.That(market.MaterialsProduced, Is.EqualTo(8));
            Assert.That(market.MaterialsConsumed, Is.EqualTo(2));
            Assert.That(market.FoodInventoryBefore, Is.EqualTo(OpeningFood));
            Assert.That(market.FoodInventoryAfter, Is.EqualTo(8));
            Assert.That(market.MaterialsInventoryAfter, Is.EqualTo(18));
            Assert.That(session.Market.FoodPriceIndexPermille, Is.EqualTo(900));
            Assert.That(session.Market.MaterialsPriceIndexPermille, Is.EqualTo(900));
            Assert.That(session.Industry.Resources.Funds, Is.EqualTo(30),
                "Two farm nets (+2) minus the 2-fund household food bill.");
            Assert.That(session.Industry.Resources.Supplies, Is.EqualTo(18));

            // Player-visible panel text after the committed month.
            ClickActiveButton("ReferenceNavHouseholdButton");
            yield return null;
            Assert.That(panel.IsOpen, Is.True,
                "家族内务 must present the embedded panel again after a page round trip.");
            panel.Refresh();

            string reportLine = panel.ReportLine;
            TestContext.WriteLine("industry.summary=" + report.Summary);
            TestContext.WriteLine("market.summary=" + market.Summary);
            TestContext.WriteLine("panel.resources=" + panel.ResourceLine);
            TestContext.WriteLine("panel.farmStatus=" + panel.FarmStatus);
            TestContext.WriteLine("panel.report=" + reportLine);
            Assert.That(CountOccurrences(reportLine, "产粮 +4"), Is.EqualTo(2),
                "The visible report must name both farm harvests.\n" + reportLine);
            StringAssert.Contains("月末 资金 30→32", reportLine);
            StringAssert.Contains("粮食 2→8", reportLine);
            StringAssert.Contains("物资 12→18", reportLine);
            StringAssert.Contains("产业需求 2、实际投入 2", reportLine);
            StringAssert.Contains("粮价 1000→900‰", reportLine);
            StringAssert.Contains("物资价 1000→900‰", reportLine);
            Assert.That(panel.FarmStatus, Is.EqualTo("一号：运作中；二号：运作中"));
            StringAssert.Contains("粮食 8", panel.ResourceLine);
            StringAssert.Contains("物资 18", panel.ResourceLine);
        }

        private static void ClickActiveButton(string name)
        {
            Button button = UnityEngine.Object
                .FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate => candidate.name == name);
            Assert.That(button, Is.Not.Null, "Missing authored button: " + name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True,
                "Button is not reachable on this page: " + name);
            Assert.That(button.interactable, Is.True, "Button is not interactable: " + name);
            button.onClick.Invoke();
        }

        private static int CountOccurrences(string text, string fragment)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(fragment))
                return 0;
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(fragment, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += fragment.Length;
            }
            return count;
        }
    }
}
