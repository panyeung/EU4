using System;
using NUnit.Framework;
using SengokuChronicle.Presentation;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class ImmersiveWorldMapViewTests
    {
        [Test]
        public void ControllerRefreshAndTownReturn_KeepFoldStateAndPreviewWithoutAdvancingTime()
        {
            var root = new GameObject("Map", typeof(RectTransform));
            GameSession session = GameSession.NewGame();
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(1060, 636);
                Text Field(string name)
                {
                    var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    child.transform.SetParent(root.transform, false);
                    return child.GetComponent<Text>();
                }
                Text marker = Field("LocationMarkerText"), feedback = Field("FeedbackText"),
                    date = Field("CurrentDateText"), destination = Field("DestinationNameText"),
                    days = Field("TravelTimeText"), arrival = Field("ArrivalDateText");
                var town = new GameObject("TownMenuRoot", typeof(RectTransform));
                town.transform.SetParent(root.transform, false);
                var controller = root.AddComponent<WorldMapController>();
                controller.BindReferences(session, null, marker, feedback, date, destination, days,
                    arrival, null, null, null, town, null, null);
                controller.ReturnToMap();
                controller.SelectDestination(LocationId.Atsuta);
                var originalDate = session.WorldClock.CurrentDate;
                var view = root.AddComponent<ImmersiveWorldMapView>();
                view.Configure();
                view.SetTravelExpanded(false);
                view.SetInformationExpanded(false);
                controller.ReturnToMap(); // Also called by successful SaveLoadPanel.Load.
                typeof(ImmersiveWorldMapView).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
                Assert.That(days.gameObject.activeSelf, Is.False);
                Assert.That(date.gameObject.activeSelf, Is.False);
                Assert.That(feedback.gameObject.activeSelf, Is.True);
                Assert.That(controller.SelectedDestination, Is.EqualTo(LocationId.Atsuta));
                Assert.That(controller.CurrentPreview, Is.Not.Null);
                Assert.That(controller.Feedback, Does.Contain("热田"),
                    "returning to a valid preview must name its destination in the dock");
                controller.OpenTownMenu();
                typeof(ImmersiveWorldMapView).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
                Assert.That(root.transform.Find("MapTravelToggle").gameObject.activeSelf, Is.False);
                Assert.That(feedback.gameObject.activeSelf, Is.False);
                controller.ReturnToMap();
                typeof(ImmersiveWorldMapView).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
                Assert.That(days.gameObject.activeSelf, Is.False);
                Assert.That(root.transform.Find("MapTravelToggle").gameObject.activeSelf, Is.True);
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(originalDate));
                Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(session.gameObject); }
        }

        [Test]
        public void NarrowResize_HidesPanelsAndSeparatesTravelFromTimeActions()
        {
            var host = new GameObject("Host", typeof(RectTransform));
            try
            {
                var root = new GameObject("Map", typeof(RectTransform));
                root.transform.SetParent(host.transform, false);
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(1060, 636);
                var cancel = new GameObject("CancelButton", typeof(RectTransform));
                cancel.transform.SetParent(root.transform, false);
                var clock = new GameObject("WorldClockPanel", typeof(RectTransform));
                clock.transform.SetParent(host.transform, false);
                var advance = new GameObject("AdvanceButton", typeof(RectTransform));
                advance.transform.SetParent(clock.transform, false);
                var clockFeedback = new GameObject("MonthTurnFeedbackText", typeof(RectTransform), typeof(Text));
                clockFeedback.transform.SetParent(clock.transform, false);
                clockFeedback.GetComponent<Text>().text = "尚有待处理事件：无法推进。";
                var dateText = new GameObject("DateText", typeof(RectTransform), typeof(Text));
                dateText.transform.SetParent(clock.transform, false);
                var view = root.AddComponent<ImmersiveWorldMapView>();
                view.Configure();
                var dateCorners = new Vector3[4];
                var feedbackCorners = new Vector3[4];
                ((RectTransform)clockFeedback.transform).GetWorldCorners(feedbackCorners);
                ((RectTransform)dateText.transform).GetWorldCorners(dateCorners);
                Assert.That(feedbackCorners[0].y, Is.GreaterThan(dateCorners[1].y),
                    "wide-layout refusal must leave the current date readable");
                rect.sizeDelta = new Vector2(600, 636);
                typeof(ImmersiveWorldMapView).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
                Assert.That(root.transform.Find("MapInformationSurface").gameObject.activeSelf, Is.False);
                Assert.That(root.transform.Find("MapTravelSurface").gameObject.activeSelf, Is.False);
                var cancelCorners = new Vector3[4];
                var advanceCorners = new Vector3[4];
                ((RectTransform)cancel.transform).GetWorldCorners(cancelCorners);
                ((RectTransform)advance.transform).GetWorldCorners(advanceCorners);
                Assert.That(cancelCorners[0].y, Is.GreaterThan(advanceCorners[1].y));
                ((RectTransform)clockFeedback.transform).GetWorldCorners(feedbackCorners);
                Assert.That(feedbackCorners[0].y, Is.GreaterThan(cancelCorners[1].y),
                    "visible clock refusal stays above both action rows");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void FoldingKeepsActionsAndExistingControls_AndConfigurationIsIdempotent()
        {
            var root = new GameObject("Map", typeof(RectTransform));
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(1060, 636);
                foreach (string name in new[] { "DestinationNameText", "TravelTimeText",
                    "ArrivalDateText", "FeedbackText", "CurrentDateText", "LocationMarkerText",
                    "TitleText", "ConfirmButton", "CancelButton", "DestButton_0", "DestButton_1",
                    "DestButton_2" })
                {
                    var child = new GameObject(name, typeof(RectTransform));
                    child.transform.SetParent(root.transform, false);
                }
                Type type = typeof(ReferenceGalleryNavigator).Assembly.GetType(
                    "SengokuChronicle.Presentation.ImmersiveWorldMapView");
                Assert.That(type, Is.Not.Null, "map needs its dedicated folding layout");
                Component view = root.AddComponent(type);
                type.GetMethod("Configure").Invoke(view, null);
                int count = root.transform.childCount;
                type.GetMethod("SetTravelExpanded").Invoke(view, new object[] { false });
                type.GetMethod("SetInformationExpanded").Invoke(view, new object[] { false });
                Assert.That(root.transform.Find("TravelTimeText").gameObject.activeSelf, Is.False);
                Assert.That(root.transform.Find("FeedbackText").gameObject.activeSelf, Is.True,
                    "travel feedback remains in the fixed dock when information is folded");
                Assert.That(root.transform.Find("ConfirmButton").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("CancelButton").gameObject.activeSelf, Is.True);
                type.GetMethod("Configure").Invoke(view, null);
                Assert.That(root.transform.childCount, Is.EqualTo(count));
                Assert.That(root.transform.Find("TravelTimeText").gameObject.activeSelf, Is.False);
                Button toggle = root.transform.Find("MapTravelToggle").GetComponent<Button>();
                Assert.That(toggle.GetComponentInChildren<Text>(true).font, Is.Not.Null,
                    "runtime-created controls must have an available font");
                toggle.onClick.Invoke();
                Assert.That(root.transform.Find("TravelTimeText").gameObject.activeSelf, Is.True,
                    "one click after rebinding must expand exactly once");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
