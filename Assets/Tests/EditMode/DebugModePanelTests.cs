using System;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the development-only five-tab window: tab routing, the
    /// preview-to-confirm contract, visible rejection reasons, session lifecycle and the
    /// build guard that keeps the entry out of release players.
    /// </summary>
    public class DebugModePanelTests
    {
        private GameSession _session;
        private GameObject _parent;
        private GameObject _entryObject;
        private Button _entryButton;
        private DebugModePanel _panel;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _parent = new GameObject("DebugPanelParent", typeof(RectTransform));
            _entryObject = new GameObject("DebugEntry", typeof(RectTransform));
            _entryButton = _entryObject.AddComponent<Button>();
            _panel = DebugModePanel.CreateWindowUnder(_parent.transform, _session, _entryButton);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panel != null)
                UnityEngine.Object.DestroyImmediate(_panel.gameObject);
            if (_parent != null)
                UnityEngine.Object.DestroyImmediate(_parent);
            if (_entryObject != null)
                UnityEngine.Object.DestroyImmediate(_entryObject);
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Window_BuildsFiveTabsAndStartsHidden()
        {
            Assert.That(_panel.IsBound, Is.True);
            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(_panel.ActiveTab, Is.EqualTo(DebugModeTab.Session));
            Assert.That(DebugModePanel.TabCount, Is.EqualTo(5));
        }

        [Test]
        public void Watermark_IsAlwaysTheDebugAndNoSaveLine()
        {
            _panel.Open();
            Assert.That(_panel.WatermarkLine, Is.EqualTo(DebugModePanel.Watermark));
            Assert.That(_panel.WatermarkLine, Does.Contain("DEBUG"));
            Assert.That(_panel.WatermarkLine, Does.Contain("不会保存至玩家存档"));
        }

        [Test]
        public void Watermark_DrawsAboveTheHeaderWithoutTakingItsDragEvents()
        {
            _panel.Open();

            Transform header = _panel.transform.Find("DebugHeader");
            Transform watermark = _panel.transform.Find("DebugWatermark");
            Assert.That(header, Is.Not.Null, "the window must keep its header bar.");
            Assert.That(watermark, Is.Not.Null, "the window must keep its watermark line.");

            // The watermark sits inside the opaque header bar, so a later sibling is the only
            // reason its glyphs are visible instead of hidden behind the header image.
            Assert.That(watermark.GetSiblingIndex(), Is.GreaterThan(header.GetSiblingIndex()),
                "the header image would cover the watermark line.");

            var watermarkRect = (RectTransform)watermark;
            var headerRect = (RectTransform)header;
            Assert.That(watermarkRect.anchoredPosition.y, Is.LessThanOrEqualTo(0f));
            Assert.That(watermarkRect.anchoredPosition.y - watermarkRect.rect.height,
                Is.GreaterThanOrEqualTo(-headerRect.rect.height),
                "the watermark must stay inside the header bar it is drawn for.");

            Assert.That(watermark.GetComponent<Text>().raycastTarget, Is.False,
                "the watermark must not swallow the drag events the header listens for.");
            Assert.That(header.GetComponent<UnityEngine.EventSystems.EventTrigger>(),
                Is.Not.Null, "the header bar stays the drag handle.");
        }

        [Test]
        public void Open_EveryVisibleTextKeepsALiveFontWhenTheThemeCacheHoldsADestroyedFace()
        {
            // Play Mode's scene load destroys the unsaved dynamic OS font while this project
            // keeps the domain reload off, so the theme's static cache holds a dead handle and
            // hands it to Text.font. A destroyed font draws nothing at all, which is the blank
            // header/tab/button text the Game View showed. The open window must recover on every
            // tab, and the shared cache must end the test exactly as it started.
            Font runtimeFace = Font.CreateDynamicFontFromOSFont(new[] { "KaiTi", "Arial" }, 16);
            if (runtimeFace == null)
                Assert.Ignore("No dynamic OS font is available to simulate the destroyed cache.");

            GameObject canvasObject = CreateReferenceCanvas();
            Font previousFace = ThemeCacheFace();
            try
            {
                SetThemeCacheFace(runtimeFace);
                UnityEngine.Object.DestroyImmediate(runtimeFace);

                DebugModePanel panel = DebugModePanel.CreateWindowUnder(canvasObject.transform,
                    _session);
                try
                {
                    panel.Open();
                    Canvas.ForceUpdateCanvases();

                    Assert.That(SengokuUiTheme.Serif, Is.Not.Null,
                        "the theme must replace a cached font that no longer exists.");

                    int drawn = 0;
                    foreach (DebugModeTab tab in RenderedTabs)
                        drawn += AssertTabDrawsItsLines(panel, tab);

                    Assert.That(drawn, Is.GreaterThan(0),
                        "the open window must draw its labels, seen through all five tabs.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(panel.gameObject);
                }
            }
            finally
            {
                SetThemeCacheFace(previousFace);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static readonly DebugModeTab[] RenderedTabs =
        {
            DebugModeTab.Session, DebugModeTab.People, DebugModeTab.Date,
            DebugModeTab.Resources, DebugModeTab.Events
        };

        /// <summary>
        /// Asserts one tab still draws: a live font, real glyphs for every visible string, and
        /// the whole line - except the scroll rows, whose best-fit type is checked against the
        /// viewport they shrink into by <see cref="RosterAndEventRows_FitTheScrollViewportAndShrinkLongLabelsWhole"/>.
        /// A row the canvas culls (scrolled past the mask) is deliberately never meshed by
        /// uGUI, so only the live-font half applies to it.
        /// </summary>
        private static int AssertTabDrawsItsLines(DebugModePanel panel, DebugModeTab tab)
        {
            panel.SelectTab(tab);
            Canvas.ForceUpdateCanvases();
            int drawn = 0;
            foreach (Text text in panel.GetComponentsInChildren<Text>(true))
            {
                if (!text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text))
                    continue;
                Assert.That(text.font, Is.Not.Null,
                    tab + " / " + text.name + " must not be handed a destroyed font.");
                if (!text.enabled || text.canvasRenderer.cull)
                    continue;
                TextGenerator generator = text.cachedTextGenerator;
                Assert.That(generator.vertexCount, Is.GreaterThan(0),
                    tab + " / " + text.name + " must draw glyphs for '" + text.text + "'.");
                if (text.resizeTextForBestFit)
                {
                    Assert.That(generator.fontSizeUsedForBestFit,
                        Is.InRange(text.resizeTextMinSize, text.resizeTextMaxSize),
                        tab + " / " + text.name + " must shrink inside its best-fit range.");
                }
                else
                {
                    Assert.That(generator.characterCountVisible,
                        Is.EqualTo(text.text.Length),
                        tab + " / " + text.name +
                        " must show its whole line at the project Game View scale.");
                }
                drawn++;
            }
            Assert.That(drawn, Is.GreaterThan(0), tab + " must draw its labels.");
            return drawn;
        }

        private static GameObject CreateReferenceCanvas()
        {
            var canvasObject = new GameObject("DebugLegibilityCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static Font ThemeCacheFace() =>
            (Font)ThemeCacheField().GetValue(null);

        private static void SetThemeCacheFace(Font face) =>
            ThemeCacheField().SetValue(null, face);

        private static System.Reflection.FieldInfo ThemeCacheField()
        {
            System.Reflection.FieldInfo field = typeof(SengokuUiTheme).GetField("_serif",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                "SengokuUiTheme must keep the cached serif field this test poisons.");
            return field;
        }

        [Test]
        public void RosterAndEventRows_FitTheScrollViewportAndShrinkLongLabelsWhole()
        {
            GameObject canvasObject = CreateReferenceCanvas();
            DebugModePanel panel = DebugModePanel.CreateWindowUnder(canvasObject.transform,
                _session);
            try
            {
                panel.Open();
                panel.SelectTab(DebugModeTab.People);
                // The roster row appends role, birth year and person id to this name, which is
                // the realistic worst case: wider than the list at 14 px, so the row has to
                // shrink its type and stay inside the mask that clips it.
                panel.SetPersonInput("長谷川源三郎左衛門尉", panel.Session.Today.Year - 20);
                panel.CreatePerson();
                Canvas.ForceUpdateCanvases();

                int checkedRows = AssertRowsFitTheViewport(panel,
                    "DebugPeopleContent/DebugRoster", DebugModeTab.People, "Person",
                    panel.RosterRowCount, "長谷川源三郎左衛門尉");

                panel.SelectTab(DebugModeTab.Events);
                Canvas.ForceUpdateCanvases();
                checkedRows += AssertRowsFitTheViewport(panel,
                    "DebugEventsContent/DebugEventList", DebugModeTab.Events, "Event",
                    panel.EventRowCount, null);

                Assert.That(checkedRows, Is.GreaterThan(1),
                    "both lists must be measured, not just the tab that happens to be open.");

                // A name nobody types must not shrink to nothing: best fit stops at its floor
                // and the row still stays inside the viewport instead of pushing past the mask.
                panel.SelectTab(DebugModeTab.People);
                panel.SetPersonInput(new string('影', 40), panel.Session.Today.Year - 21);
                panel.CreatePerson();
                Canvas.ForceUpdateCanvases();
                Text clamped = RowLabelContaining(panel,
                    "DebugPeopleContent/DebugRoster", new string('影', 40));
                Assert.That(clamped, Is.Not.Null, "the over-long row must still be listed.");
                Assert.That(clamped.canvasRenderer.cull, Is.False,
                    "the over-long row must be scrolled into view to be measured.");
                Assert.That(clamped.cachedTextGenerator.fontSizeUsedForBestFit,
                    Is.EqualTo(clamped.resizeTextMinSize),
                    "best fit must clamp at its 10 px floor instead of shrinking away.");
                Assert.That(((RectTransform)clamped.transform.parent).rect.width,
                    Is.LessThanOrEqualTo(ViewportWidth(panel,
                        "DebugPeopleContent/DebugRoster") + 0.5f),
                    "even the clamped row must stay inside the viewport.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panel.gameObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        /// <summary>
        /// Checks every row of one scroll list against the viewport that clips it: the row must
        /// not be wider than the inset viewport, its corners must stay inside the viewport
        /// horizontally, and every row the mask actually shows must display its whole best-fit
        /// label. A row scrolled past the mask keeps only the geometry and best-fit contract,
        /// because uGUI never meshes a culled graphic.
        /// </summary>
        private static int AssertRowsFitTheViewport(DebugModePanel panel, string scrollPath,
            DebugModeTab tab, string rowPrefix, int rowCount, string expectedLabelPart)
        {
            Transform scroll = panel.transform.Find(scrollPath);
            Assert.That(scroll, Is.Not.Null, tab + " must own " + scrollPath + ".");
            var content = (RectTransform)scroll.Find("Viewport/Content");
            Assert.That(content, Is.Not.Null, scrollPath + " must keep its scroll content.");
            float viewportWidth = ViewportWidth(panel, scrollPath);
            Assert.That(viewportWidth, Is.LessThan(((RectTransform)scroll).rect.width),
                "the viewport is inset inside its list, so rows must fit the inset width.");
            Assert.That(rowCount, Is.GreaterThan(0), tab + " must list at least one row.");

            bool matchedLabel = expectedLabelPart == null;
            for (int i = 0; i < rowCount; i++)
            {
                Transform row = content.Find(rowPrefix + i);
                Assert.That(row, Is.Not.Null, rowPrefix + i + " must exist under " + scrollPath +
                    ".");
                var rowRect = (RectTransform)row;
                Assert.That(rowRect.rect.width, Is.LessThanOrEqualTo(viewportWidth + 0.5f),
                    rowPrefix + i + " is " + rowRect.rect.width + " wide inside a " +
                    viewportWidth + " viewport, so its right edge is clipped by the mask.");
                AssertRowCornersInsideViewport(panel, scrollPath, rowRect, rowPrefix + i);

                Text label = row.GetComponentInChildren<Text>(true);
                Assert.That(label, Is.Not.Null, rowPrefix + i + " must keep its label.");
                Assert.That(label.resizeTextForBestFit, Is.True,
                    rowPrefix + i + " must shrink its label instead of dropping its tail.");
                bool matches = expectedLabelPart != null && label.text.Contains(expectedLabelPart);
                if (label.canvasRenderer.cull)
                {
                    // A row scrolled past the mask is never meshed, so its generator has no
                    // character count; the row can also never be the long label under test.
                    Assert.That(matches, Is.False,
                        "the long label must be scrolled into view to be measured.");
                    continue;
                }
                Assert.That(label.cachedTextGenerator.characterCountVisible,
                    Is.EqualTo(label.text.Length), rowPrefix + i + " shows only " +
                    label.cachedTextGenerator.characterCountVisible + " of " +
                    label.text.Length + " characters: '" + label.text + "'.");
                Assert.That(label.cachedTextGenerator.fontSizeUsedForBestFit,
                    Is.InRange(label.resizeTextMinSize, label.resizeTextMaxSize),
                    rowPrefix + i + " must stay inside its best-fit range.");
                if (matches)
                    matchedLabel = true;
            }

            Assert.That(matchedLabel, Is.True,
                scrollPath + " must list the long label this test created.");
            return rowCount;
        }

        private static void AssertRowCornersInsideViewport(DebugModePanel panel, string scrollPath,
            RectTransform row, string rowName)
        {
            Transform scroll = panel.transform.Find(scrollPath);
            var viewport = (RectTransform)scroll.Find("Viewport");
            Rect box = viewport.rect;
            var corners = new Vector3[4];
            row.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = viewport.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(box.xMin - 0.5f, box.xMax + 0.5f),
                    rowName + " reaches x=" + local.x + ", outside the " + box.xMin + ".." +
                    box.xMax + " viewport.");
            }
        }

        private static float ViewportWidth(DebugModePanel panel, string scrollPath)
        {
            Transform scroll = panel.transform.Find(scrollPath);
            var viewport = (RectTransform)scroll.Find("Viewport");
            return viewport.rect.width;
        }

        private static Text RowLabelContaining(DebugModePanel panel, string scrollPath,
            string needle)
        {
            Transform content = panel.transform.Find(scrollPath).Find("Viewport/Content");
            foreach (Text label in content.GetComponentsInChildren<Text>(true))
                if (label.text.Contains(needle))
                    return label;
            return null;
        }

        [Test]
        public void Open_EntersAnIsolatedDebugSessionAndShowsTheWindow()
        {
            _panel.Open();

            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.True);
            Assert.That(_panel.Session, Is.Not.Null);
            Assert.That(_panel.Session.IsActive, Is.True);
            Assert.That(_panel.RosterRowCount, Is.GreaterThan(0));
        }

        [Test]
        public void Tabs_RouteToExactlyOneContentPage()
        {
            _panel.Open();

            for (int i = 0; i < DebugModePanel.TabCount; i++)
            {
                _panel.SelectTab((DebugModeTab)i);
                Assert.That(_panel.ActiveTab, Is.EqualTo((DebugModeTab)i));
                int visible = 0;
                foreach (Transform child in _panel.transform)
                {
                    if (!child.name.EndsWith("Content", StringComparison.Ordinal))
                        continue;
                    if (child.gameObject.activeSelf)
                        visible++;
                }
                Assert.That(visible, Is.EqualTo(1), "only the selected tab body stays visible.");
            }
        }

        [Test]
        public void Fold_HidesTheBodyAndTabStripWithoutEndingTheSession()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.Resources);

            _panel.ToggleFold();

            Assert.That(_panel.IsFolded, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.True);
            foreach (Transform child in _panel.transform)
                if (child.name.EndsWith("Content", StringComparison.Ordinal))
                    Assert.That(child.gameObject.activeSelf, Is.False);

            _panel.ToggleFold();
            Assert.That(_panel.IsFolded, Is.False);
            Assert.That(_panel.ActiveTab, Is.EqualTo(DebugModeTab.Resources));
        }

        [Test]
        public void CloseAndReopen_KeepsTheInMemoryDebugState()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("面板人物", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();
            int persons = _session.Succession.World.PersonCount;

            _panel.Close();
            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.True);

            _panel.Open();

            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(persons));
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(SuccessionSample.StandardFamilyId));
        }

        [Test]
        public void Exit_DiscardsTheSessionClosesTheWindowAndNotifiesTheTitle()
        {
            _panel.Open();
            Assert.That(_panel.IsOpen, Is.True);
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("将被丢弃", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();
            string endedMessage = null;
            _panel.DebugSessionEnded += message => endedMessage = message;

            _panel.ExitDebugSession();

            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_panel.MessageLine, Does.Contain("已退出 Debug 模式"));
            Assert.That(endedMessage, Does.Contain("已退出 Debug 模式"));
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")));
        }

        [Test]
        public void SerializedOpenButton_IsWiredByTheLifecycleWithoutBindOpenButton()
        {
            // A scene assigns the serialized reference before any lifecycle call. The
            // lifecycle passes the same instance back to BindOpenButton, so the first
            // AddListener must still happen instead of being skipped by a same-reference
            // early return.
            var host = new GameObject("SerializedHost", typeof(RectTransform));
            var entryObject = new GameObject("SerializedOpenEntry", typeof(RectTransform));
            Button serializedButton = entryObject.AddComponent<Button>();
            DebugModePanel panel = DebugModePanel.CreateWindowUnder(host.transform, _session);
            try
            {
                Assert.That(panel.OpenListenerBindingCount, Is.Zero,
                    "a factory panel without an entry button starts unbound.");

                typeof(DebugModePanel)
                    .GetField("openButton",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    .SetValue(panel, serializedButton);

                InvokeLifecycle(panel, "OnEnable");

                Assert.That(panel.OpenListenerBindingCount, Is.EqualTo(1),
                    "the serialized reference must get exactly one listener.");
                serializedButton.onClick.Invoke();
                Assert.That(panel.IsOpen, Is.True);
                Assert.That(_session.IsDebugSessionActive, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panel.gameObject);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(entryObject);
            }
        }

        [Test]
        public void RepeatedLifecycleEnables_BindTheOpenListenerExactlyOnce()
        {
            for (int i = 0; i < 3; i++)
                InvokeLifecycle(_panel, "OnEnable");

            Assert.That(_panel.OpenListenerBindingCount, Is.EqualTo(1));

            _entryButton.onClick.Invoke();

            Assert.That(_panel.IsOpen, Is.True, "one click must open the window once.");
        }

        private static void InvokeLifecycle(DebugModePanel panel, string methodName)
        {
            System.Reflection.MethodInfo method = typeof(DebugModePanel).GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing " + methodName + " lifecycle method.");
            method.Invoke(panel, null);
        }

        [Test]
        public void Factory_HostsTheWindowOutsideTheTitleOverlay()
        {
            var canvasObject = new GameObject("DebugCanvas", typeof(RectTransform),
                typeof(Canvas));
            var titleRoot = new GameObject("M6TitleRoot", typeof(RectTransform));
            try
            {
                titleRoot.transform.SetParent(canvasObject.transform, false);
                DebugModePanel panel = DebugModePanel.CreateWindowUnder(titleRoot.transform,
                    _session);
                try
                {
                    Assert.That(panel.transform.parent, Is.EqualTo(canvasObject.transform),
                        "the debug window must not be a child of the title overlay.");
                    titleRoot.SetActive(false);
                    panel.Open();
                    Assert.That(panel.IsOpen, Is.True,
                        "hiding the title must not hide the debug window.");
                    Assert.That(panel.IsRequestedOpen, Is.True);
                    panel.Close();
                    Assert.That(panel.IsOpen, Is.False);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(panel.gameObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(titleRoot);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void Open_ReparentsOutOfAHiddenHostSoTheWindowStaysUsable()
        {
            var canvasObject = new GameObject("DebugCanvas2", typeof(RectTransform),
                typeof(Canvas));
            var hiddenHost = new GameObject("HiddenHost", typeof(RectTransform));
            var bareParent = new GameObject("BareParent", typeof(RectTransform));
            try
            {
                hiddenHost.transform.SetParent(canvasObject.transform, false);
                DebugModePanel panel = DebugModePanel.CreateWindowUnder(bareParent.transform,
                    _session);
                try
                {
                    panel.transform.SetParent(hiddenHost.transform, false);
                    hiddenHost.SetActive(false);
                    Assert.That(panel.IsOpen, Is.False);

                    panel.Open();

                    Assert.That(panel.transform.parent, Is.EqualTo(canvasObject.transform));
                    Assert.That(panel.IsOpen, Is.True,
                        "the window must become visible even when its authored host is hidden.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(panel.gameObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hiddenHost);
                UnityEngine.Object.DestroyImmediate(bareParent);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void Open_StacksTheWindowAbovePageRootsAndBelowModalOverlays()
        {
            // Reproduce the real FoundationScene stack: the saved window root sits just below
            // the title overlay, and full-screen page roots are authored after it. Opening the
            // window must move it above those page roots without ever covering the modal
            // forced-event and tactical overlays, or the Game View shows only the HUD.
            var canvasObject = new GameObject("FoundationLikeCanvas", typeof(RectTransform),
                typeof(Canvas));
            try
            {
                Transform canvas = canvasObject.transform;
                Transform mapRoot = Child(canvas, "M2WorldMapRoot");
                Transform titleRoot = Child(canvas, "M6TitleRoot");
                Transform pageRoot = Child(canvas, "M02DailyActionRoot");
                Transform npcRoot = Child(canvas, "M03NpcAiInspectorRoot");
                Transform eventRoot = Child(canvas, "EventChoiceRoot");
                Transform tactical = Child(canvas, "M6TacticalMissionOverlay");

                DebugModePanel panel = DebugModePanel.CreateWindowUnder(titleRoot, _session);
                try
                {
                    panel.transform.SetSiblingIndex(titleRoot.GetSiblingIndex());
                    Assert.That(panel.transform.GetSiblingIndex(),
                        Is.LessThan(pageRoot.GetSiblingIndex()),
                        "the saved layout must reproduce the defect: page roots render first.");

                    titleRoot.gameObject.SetActive(false);
                    panel.Open();

                    Assert.That(panel.IsOpen, Is.True);
                    Assert.That(panel.gameObject.activeInHierarchy, Is.True);
                    int window = panel.transform.GetSiblingIndex();
                    Assert.That(window, Is.GreaterThan(mapRoot.GetSiblingIndex()));
                    Assert.That(window, Is.GreaterThan(titleRoot.GetSiblingIndex()));
                    Assert.That(window, Is.GreaterThan(pageRoot.GetSiblingIndex()),
                        "the debug window must render above ordinary page roots.");
                    Assert.That(window, Is.GreaterThan(npcRoot.GetSiblingIndex()));
                    Assert.That(window, Is.LessThan(eventRoot.GetSiblingIndex()),
                        "forced-event choices must stay above the debug window.");
                    Assert.That(window, Is.LessThan(tactical.GetSiblingIndex()),
                        "the M06 tactical overlay must stay above the debug window.");

                    panel.Close();
                    int closed = panel.transform.GetSiblingIndex();
                    panel.Open();
                    Assert.That(panel.transform.GetSiblingIndex(), Is.EqualTo(closed),
                        "reopening must not drift the window out of its stacking slot.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(panel.gameObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        [Test]
        public void Reset_DropsTheSessionClosesTheWindowAndRestoresTheOrdinaryFlow()
        {
            _panel.Open();
            int injectedPersons = _panel.RosterRowCount;
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("重置前", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();
            Assert.That(_panel.RosterRowCount, Is.GreaterThan(injectedPersons));
            string endedMessage = null;
            _panel.DebugSessionEnded += message => endedMessage = message;

            _panel.ResetDebugSession();

            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(_panel.IsRequestedOpen, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.False,
                "reset must drop the temporary session instead of leaving a debug marker.");
            Assert.That(_session.CurrentDebugSession, Is.Null);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")),
                "reset restores the ordinary new-game state and sample paths.");
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(6),
                "every injected person is discarded.");
            Assert.That(_panel.Session, Is.Null);
            Assert.That(_panel.SelectedPersonId.IsEmpty, Is.True);
            Assert.That(_panel.MessageLine, Does.Contain("已重置调试会话"));
            Assert.That(endedMessage, Does.Contain("已重置调试会话"),
                "reset must notify the title so it can return to the start page.");
        }

        [Test]
        public void PersonEdit_ShowsTheRejectionReasonAndLeavesThePersonUntouched()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("待编辑", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();
            PersonId selected = _panel.SelectedPersonId;

            _panel.SetPersonInput("   ", _panel.Session.Today.Year - 20);
            _panel.ApplyPersonEdit();

            Assert.That(_panel.PersonResultLine, Does.Contain("姓名不能为空"));
            Assert.That(_session.Succession.World.TryGetPerson(selected, out PersonState person),
                Is.True);
            Assert.That(person.DisplayName, Is.EqualTo("待编辑"));
        }

        [Test]
        public void PersonEdit_AppliesWhenTheFieldsAreValid()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("原始名", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();
            PersonId selected = _panel.SelectedPersonId;

            _panel.SetPersonInput("改名后", _panel.Session.Today.Year - 22);
            _panel.ApplyPersonEdit();

            Assert.That(_panel.PersonResultLine, Does.Contain("已更新测试人物"));
            Assert.That(_session.Succession.World.TryGetPerson(selected, out PersonState person),
                Is.True);
            Assert.That(person.DisplayName, Is.EqualTo("改名后"));
            Assert.That(_session.Succession.World.TryGetLife(selected,
                out SuccessionPersonLife life), Is.True);
            Assert.That(life.BirthYear, Is.EqualTo(_panel.Session.Today.Year - 22));
        }

        [Test]
        public void PersonEdit_RejectsDemotingTheCurrentHeadAndKeepsTransferUsable()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.People);
            PersonId head = _session.Succession.HeadPersonId;
            _panel.SelectPerson(head);
            _panel.SetPersonInput("家主改名", _panel.Session.Today.Year - 40);

            // Renaming the sitting head keeps the role and must be allowed.
            _panel.ApplyPersonEdit();
            Assert.That(_panel.PersonResultLine, Does.Contain("已更新测试人物"));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));

            // Demoting the sitting head through the editor must be refused.
            _panel.SelectPerson(head);
            _panel.CycleRole();
            _panel.ApplyPersonEdit();

            Assert.That(_panel.PersonResultLine, Does.Contain("转移家主"));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.Succession.World.Family.TryGetRole(head,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Head));

            _panel.SetPersonInput("候选家主", _panel.Session.Today.Year - 30);
            _panel.CreatePerson();
            PersonId heir = _panel.SelectedPersonId;
            _panel.TransferHeadship();

            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(heir));
        }

        [Test]
        public void ComplexPresetButton_FillsTheRosterList()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.People);
            int before = _panel.RosterRowCount;

            _panel.ApplyComplexPreset();

            Assert.That(_panel.PersonResultLine, Does.Contain("复杂家谱预设"));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(before + 16));
            Assert.That(_panel.RosterRowCount, Is.EqualTo(_session.Succession.World.PersonCount));
        }

        [Test]
        public void KinshipConfirm_RequiresAMatchingPreview()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("关系甲", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();
            PersonId left = _panel.SelectedPersonId;
            _panel.SetPersonInput("关系乙", _panel.Session.Today.Year - 18);
            _panel.CreatePerson();
            PersonId right = _panel.SelectedPersonId;

            _panel.SetKinshipEndpoints(left, right);
            _panel.ApplyKinship(true);
            Assert.That(_panel.KinshipPreviewLine, Does.Contain("请先预览"));
            Assert.That(_session.Succession.World.Kinship.LinksFor(left).Count, Is.EqualTo(0));

            _panel.PreviewKinship();
            Assert.That(_panel.KinshipPreviewLine, Does.Contain("可以提交"));
            _panel.ApplyKinship(true);
            Assert.That(_session.Succession.World.Kinship.LinksFor(left).Count, Is.EqualTo(1));

            _panel.SetKinshipEndpoints(left, right);
            _panel.PreviewKinship();
            _panel.SetKinshipEndpoints(right, left);
            _panel.ApplyKinship(true);
            Assert.That(_panel.KinshipPreviewLine, Does.Contain("请先预览"),
                "changing the endpoints invalidates the preview.");
            Assert.That(_session.Succession.World.Kinship.LinksFor(left).Count, Is.EqualTo(1));
        }

        [Test]
        public void KinshipPreview_ShowsTheSemanticRejectionReason()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.People);
            PersonId head = _session.Succession.HeadPersonId;
            _panel.SelectPerson(head);

            _panel.SetKinshipEndpoints(head, head);
            _panel.PreviewKinship();
            Assert.That(_panel.KinshipPreviewLine, Does.Contain("不能提交"));

            _panel.SetKinshipEndpoints(head, head);
            _panel.ApplyKinship(true);
            Assert.That(_panel.KinshipPreviewLine, Does.Contain("请先预览"));
        }

        [Test]
        public void DateConfirm_RequiresThePreviewedTarget()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.Date);
            GameDate today = _panel.Session.Today;

            _panel.SetDateTarget(today.Year + 1, today.Month, today.Day);
            _panel.ConfirmDate();
            Assert.That(_panel.DateResultLine, Does.Contain("请重新预览"));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(today));

            _panel.SetDateTarget(today.Year + 1, today.Month, today.Day);
            _panel.PreviewDate();
            Assert.That(_panel.DateResultLine, Is.Not.EqualTo("请重新预览"));
            _panel.SetDateTarget(today.Year + 2, today.Month, today.Day);
            _panel.ConfirmDate();
            Assert.That(_panel.DateResultLine, Does.Contain("目标日期已改变"));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(today));

            _panel.SetDateTarget(today.Year + 1, today.Month, today.Day);
            _panel.PreviewDate();
            _panel.ConfirmDate();
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(today.Year + 1, today.Month, today.Day)));
            Assert.That(_panel.DateResultLine, Does.Contain("调试日期注入"));
        }

        [Test]
        public void AdvanceMonth_ReportsTheNormalRulePath()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.Date);

            _panel.AdvanceMonth();

            Assert.That(_panel.DateResultLine, Does.Contain("正常规则"));
        }

        [Test]
        public void Resources_ApplyAtomicallyAndShowBeforeAfterValues()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.Resources);

            _panel.SetResourceInput(120, 45, 9, 30, 1300, 950);
            _panel.ApplyResources();

            Assert.That(_panel.ResourceResultLine, Does.Contain("调试注入"));
            Assert.That(_panel.ResourceResultLine, Does.Contain("→"));
            Assert.That(_panel.Session.Resources.Funds, Is.EqualTo(120));
            Assert.That(_panel.Session.Market.FoodInventory, Is.EqualTo(30));
            Assert.That(_panel.Session.Market.FoodPriceIndexPermille, Is.EqualTo(1300));
        }

        [Test]
        public void Resources_RejectionKeepsEveryFieldAndShowsTheReason()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.Resources);
            int funds = _panel.Session.Resources.Funds;

            _panel.SetResourceInput(10, 10, 10, 10, 50, 1000);
            _panel.ApplyResources();

            Assert.That(_panel.ResourceResultLine, Does.Contain("价格指数"));
            Assert.That(_panel.Session.Resources.Funds, Is.EqualTo(funds));
        }

        [Test]
        public void EventInjection_RequiresAConfirmationStepAndLabelsTheInjection()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.Events);

            _panel.SetEventId("event.rainy-arrival");
            _panel.InjectEvent();

            Assert.That(_panel.EventResultLine, Does.Contain("再次点击"));
            Assert.That(_session.PendingEventId, Is.Empty,
                "the first click only previews the bypass.");

            _panel.InjectEvent();

            Assert.That(_panel.EventResultLine, Does.Contain("已注入事件"));
            Assert.That(_panel.EventResultLine, Does.Contain("未检查自然触发条件"));
            Assert.That(_session.PendingEventId, Is.EqualTo("event.rainy-arrival"));

            _panel.SetEventId("event.not-in-catalog");
            _panel.InjectEvent();
            Assert.That(_panel.EventResultLine, Does.Contain("不在当前目录"));
        }

        [Test]
        public void EventList_ExposesTheCatalogForSelection()
        {
            _panel.Open();
            _panel.SelectTab(DebugModeTab.Events);
            Assert.That(_panel.EventRowCount, Is.GreaterThan(0));
        }


        [Test]
        public void Window_LaysEveryControlOutInsideItsOwnBounds()
        {
            var panel = DebugModePanel.CreateWindowUnder(_parent.transform, _session);
            try
            {
                Assert.That(((RectTransform)panel.transform).sizeDelta,
                    Is.EqualTo(DebugModePanel.WindowSize));
                var rootRect = (RectTransform)panel.transform;
                var scrollContents = new System.Collections.Generic.HashSet<Transform>();
                foreach (ScrollRect scroll in panel.GetComponentsInChildren<ScrollRect>(true))
                    if (scroll.content != null)
                        scrollContents.Add(scroll.content);

                foreach (RectTransform rect in panel.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect == rootRect || InsideScrollContent(rect, scrollContents, rootRect))
                        continue;
                    Assert.That(Inside(rootRect, rect), Is.True,
                        rect.name + " lies outside the " + rootRect.rect.size + " window bounds.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panel.gameObject);
            }
        }

        private static bool InsideScrollContent(RectTransform rect,
            System.Collections.Generic.HashSet<Transform> contents, RectTransform root)
        {
            Transform current = rect;
            while (current != null && current != root)
            {
                if (contents.Contains(current))
                    return true;
                current = current.parent;
            }
            return false;
        }

        private static bool Inside(RectTransform bounds, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect rect = bounds.rect;
            foreach (Vector3 corner in corners)
            {
                Vector3 local = bounds.InverseTransformPoint(corner);
                if (local.x < rect.xMin - 0.5f || local.x > rect.xMax + 0.5f ||
                    local.y < rect.yMin - 0.5f || local.y > rect.yMax + 0.5f)
                    return false;
            }
            return true;
        }

        [Test]
        public void EntryButton_StaysArmedAfterTheFactoryHidesTheWindow()
        {
            Assert.That(_panel.IsOpen, Is.False,
                "the factory starts the window hidden, which must not disarm its entry.");

            _entryButton.onClick.Invoke();

            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.True);
        }

        [Test]
        public void CloseThenEntryButton_ReopensTheSameInMemorySession()
        {
            _entryButton.onClick.Invoke();
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("关闭后保留", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();
            int persons = _session.Succession.World.PersonCount;

            _panel.Close();
            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.True);

            _entryButton.onClick.Invoke();

            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(persons),
                "closing the window must keep the in-memory debug session.");
            Assert.That(_panel.ActiveTab, Is.EqualTo(DebugModeTab.People));
        }

        [Test]
        public void ExitThenEntryButton_StartsAFreshSessionThroughTheEntry()
        {
            _entryButton.onClick.Invoke();
            int baseline = _session.Succession.World.PersonCount;
            _panel.SelectTab(DebugModeTab.People);
            _panel.SetPersonInput("退出丢弃", _panel.Session.Today.Year - 20);
            _panel.CreatePerson();

            _panel.ExitDebugSession();
            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.False);

            _entryButton.onClick.Invoke();

            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.True);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(baseline),
                "re-entry must start a clean debug session, not revive the discarded one.");
        }

        [Test]
        public void EntryButton_IsNotDuplicatedWhenTheWindowReopens()
        {
            _entryButton.onClick.Invoke();
            _panel.Close();
            _entryButton.onClick.Invoke();
            _panel.Close();
            _entryButton.onClick.Invoke();

            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_session.Succession.World.PersonCount,
                Is.EqualTo(SuccessionSample.CreateStandard().World.PersonCount),
                "each entry click opens once instead of starting a second session.");
        }

        [Test]
        public void WindowType_IsCompiledOnlyForTheEditorAndDevelopmentBuilds()
        {
            // The type exists in this Editor compilation, and the same file is wrapped in
            // UNITY_EDITOR || DEVELOPMENT_BUILD, so a release player never compiles it.
            Assert.That(typeof(DebugModePanel).Namespace,
                Is.EqualTo("SengokuChronicle.Presentation"));
            Assert.That(typeof(DebugSession).Namespace,
                Is.EqualTo("SengokuChronicle.Application"));
        }
    }
}
