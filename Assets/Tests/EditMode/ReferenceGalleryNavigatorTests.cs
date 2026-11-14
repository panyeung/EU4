using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class ReferenceGalleryNavigatorTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void ShowPage_ActivatesOnlyTheSelectedPageAndItsPanels()
        {
            GameObject nav = Create("Navigation", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            GameObject header = Create("Header");
            Button launcher = CreateButton("Launcher");
            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);

            GameObject familyRoot = Create("FamilyRoot");
            GameObject pageHost = Create("PageHost", typeof(RectTransform));
            GameObject mapRoot = Create("MapRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            mapRoot.transform.SetParent(pageHost.transform, false);
            GameObject clockRoot = Create("WorldClockPanel", typeof(RectTransform));
            clockRoot.transform.SetParent(pageHost.transform, false);
            GameObject eventRoot = Create("EventRoot");
            eventRoot.AddComponent<EventChoicePanel>();
            GameObject characterRoot = Create("M03NpcAiInspectorRoot");
            GameObject inspectorOpener = Create("M03NpcAiInspectorOpenButton");
            inspectorOpener.transform.SetParent(characterRoot.transform);
            GameObject unassignedRoot = Create("UnassignedRoot");
            GameObject[] panels =
            {
                Create("HouseholdPanel"), Create("CharacterPanel"),
                Create("FamilyPanel"), mapRoot, Create("EventPanel")
            };
            var roots = new[]
            {
                new ReferenceGalleryPageRoot(familyRoot, ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(mapRoot, ReferenceGalleryPageMask.Map),
                new ReferenceGalleryPageRoot(characterRoot, ReferenceGalleryPageMask.Character),
                new ReferenceGalleryPageRoot(eventRoot, ReferenceGalleryPageMask.Events),
                new ReferenceGalleryPageRoot(unassignedRoot, ReferenceGalleryPageMask.None)
            };
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            GameObject backdrop = Create("M02PageBackdrop", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            backdrop.transform.SetParent(navigator.transform);
            navigator.BindReferences(roots, buttons, panels[1], panels[2], panels[0],
                panels[3], launcher, nav, header);

            navigator.ShowPage(ReferenceGalleryPage.Map);

            Assert.That(navigator.CurrentPage, Is.EqualTo(ReferenceGalleryPage.Map));
            Assert.That(nav.activeSelf, Is.True);
            Assert.That(nav.GetComponent<Image>().color, Is.EqualTo(SengokuUiTheme.IndigoRail));
            Assert.That(backdrop.GetComponent<Image>().color, Is.EqualTo(SengokuUiTheme.IndigoDeep));
            Image sharedBackdrop = BackdropHost(navigator)
                .Find("ReferenceGalleryBackdrop").GetComponent<Image>();
            Assert.That(sharedBackdrop.color, Is.EqualTo(SengokuUiTheme.IndigoDeep));
            Assert.That(sharedBackdrop.raycastTarget, Is.False,
                "the shared page background must not block page controls");
            Assert.That(header.activeSelf, Is.True);
            Assert.That(mapRoot.activeSelf, Is.True);
            Assert.That(clockRoot.transform.GetSiblingIndex(),
                Is.GreaterThan(mapRoot.transform.GetSiblingIndex()),
                "map travel overlays must not cover the month-turn controls");
            Transform mapIllustration = mapRoot.transform.Find("ReferenceMapIllustration");
            Assert.That(mapIllustration, Is.Not.Null,
                "the map reference art is attached to the map page");
            Assert.That(mapIllustration.GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(mapIllustration.GetComponent<Image>().raycastTarget, Is.False,
                "map artwork must leave route controls clickable");
            Assert.That(mapIllustration.GetSiblingIndex(), Is.Zero,
                "the map artwork sits behind the existing controls");
            Assert.That(familyRoot.activeSelf, Is.False);
            Assert.That(unassignedRoot.activeSelf, Is.False);
            Assert.That(panels[3].activeSelf, Is.True);
            Assert.That(panels[2].activeSelf, Is.False);
            Assert.That(launcher.gameObject.activeSelf, Is.False);
            Assert.That(buttons[(int)ReferenceGalleryPage.Map].interactable, Is.True,
                "the selected tab stays a valid, idempotent target");
            Assert.That((Color32)buttons[(int)ReferenceGalleryPage.Map].targetGraphic.color,
                Is.EqualTo((Color32)ReferenceGalleryNavigator.PageSelectedBackground),
                "the selected page uses the vermilion seal accent");
            Assert.That((Color32)buttons[(int)ReferenceGalleryPage.Character].targetGraphic.color,
                Is.EqualTo((Color32)ReferenceGalleryNavigator.PageIdleBackground),
                "non-selected tabs keep the indigo rail surface");

            navigator.ShowPage(ReferenceGalleryPage.Household);
            Assert.That(familyRoot.activeSelf, Is.False);
            Assert.That(panels[2].activeSelf, Is.False);
            Assert.That(panels[0].activeSelf, Is.True);

            navigator.ShowPage(ReferenceGalleryPage.Events);
            Assert.That(eventRoot.activeSelf, Is.True);
            Assert.That(mapRoot.activeSelf, Is.False);
            Assert.That(panels[0].activeSelf, Is.False);
            Assert.That(panels[3].activeSelf, Is.False);
            Transform emptyStateHost = navigator.transform.parent != null
                ? navigator.transform.parent : navigator.transform;
            Transform emptyState = emptyStateHost.Find("ReferenceEventsEmptyState");
            Assert.That(emptyState.gameObject.activeSelf,
                Is.True, "an empty event queue gets a truthful empty-state page");
            Text emptyTitle = emptyState.Find("Title").GetComponent<Text>();
            Text emptyStatus = emptyState.Find("Status").GetComponent<Text>();
            Assert.That(emptyTitle.text, Is.EqualTo("待处理事件"));
            Assert.That(emptyStatus.text, Is.EqualTo("当前没有待处理事件。"));
            Assert.That(emptyTitle.font, Is.EqualTo(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")),
                "runtime-created CJK labels use the same reliable built-in font as scene labels");
            Assert.That(emptyState.Find("PendingEventsRail").GetComponent<Image>().color,
                Is.EqualTo(SengokuUiTheme.IndigoRail));
            Assert.That(emptyState.Find("QueueTitle").GetComponent<Text>().text, Is.EqualTo("待处理事项"));
            Assert.That(emptyState.Find("QueueCount").GetComponent<Text>().text, Is.EqualTo("0"));
            Assert.That(inspectorOpener.activeSelf, Is.False,
                "the embedded read-only inspector does not keep its legacy floating opener");
        }

        [Test]
        public void ShowMapPage_ClosesTownRosterAndDialogueOverlays()
        {
            GameObject mapRoot = Create("M2WorldMapRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            GameObject townRoot = Create("TownMenuRoot");
            townRoot.transform.SetParent(mapRoot.transform);
            GameObject rosterRoot = Create("TownNpcRosterRoot");
            rosterRoot.transform.SetParent(mapRoot.transform);
            GameObject dialogueRoot = Create("TownNpcDialogueRoot");
            dialogueRoot.transform.SetParent(mapRoot.transform);
            townRoot.SetActive(true);
            rosterRoot.SetActive(true);
            dialogueRoot.SetActive(true);

            TownNpcDialoguePanel townNpcPanel = mapRoot.AddComponent<TownNpcDialoguePanel>();
            townNpcPanel.BindReferences(townRoot, rosterRoot, rosterRoot.transform, null, null,
                dialogueRoot, null, dialogueRoot.transform, null, null, null, null);
            WorldMapController worldMap = mapRoot.AddComponent<WorldMapController>();
            worldMap.BindReferences(null, null, null, null, null, null, null, null,
                null, null, null, townRoot, null, null);
            worldMap.BindTownNpcRoster(townNpcPanel, null);

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("MapButton" + i);
            GameObject[] panels =
            {
                Create("HouseholdPanel"), Create("CharacterPanel"), Create("FamilyPanel"),
                mapRoot, Create("EventPanel")
            };
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(mapRoot, ReferenceGalleryPageMask.Map)
            }, buttons, panels[1], panels[2], panels[0], mapRoot, buttons[1],
                Create("Navigation"), Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Map);

            Assert.That(townRoot.activeSelf, Is.False);
            Assert.That(rosterRoot.activeSelf, Is.False);
            Assert.That(dialogueRoot.activeSelf, Is.False);
            Assert.That(mapRoot.activeSelf, Is.True);
        }

        [Test]
        public void ShowPage_RendersBoundFeatureLaunchersAboveThePageRoot()
        {
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            var hostRect = (RectTransform)host.transform;
            hostRect.sizeDelta = new Vector2(1280f, 720f);

            GameObject pagePanel = Create("PagePanel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            pagePanel.transform.SetParent(host.transform, false);
            var pageRect = (RectTransform)pagePanel.transform;
            pageRect.anchorMin = Vector2.zero;
            pageRect.anchorMax = Vector2.one;
            pageRect.offsetMin = pageRect.offsetMax = Vector2.zero;

            GameObject featureRoot = Create("SC020EducationRoot", typeof(RectTransform));
            featureRoot.transform.SetParent(host.transform, false);
            var featureRect = (RectTransform)featureRoot.transform;
            featureRect.anchorMin = Vector2.zero;
            featureRect.anchorMax = Vector2.one;
            featureRect.offsetMin = featureRect.offsetMax = Vector2.zero;
            Button launcher = CreateButton("SC020EducationOpenButton");
            launcher.transform.SetParent(featureRoot.transform, false);

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("LauncherTab" + i);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(featureRoot, ReferenceGalleryPageMask.Character)
            }, buttons, pagePanel, pagePanel, pagePanel, pagePanel, buttons[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));
            navigator.BindFeatureLaunchers(new[]
            {
                new ReferenceGalleryPageRoot(featureRoot, ReferenceGalleryPageMask.Character)
            });

            navigator.ShowPage(ReferenceGalleryPage.Character);
            Canvas.ForceUpdateCanvases();

            Assert.That(navigator.FeatureLauncherCount, Is.EqualTo(1));
            Assert.That(featureRoot.activeSelf, Is.True);
            Assert.That(featureRoot.transform.GetSiblingIndex(),
                Is.GreaterThan(pagePanel.transform.GetSiblingIndex()),
                "a launcher registered beneath its opaque page is invisible and unclickable");
            var launcherRect = (RectTransform)launcher.transform;
            Assert.That(launcherRect.anchorMin, Is.EqualTo(Vector2.one));
            Assert.That(launcherRect.anchoredPosition,
                Is.EqualTo(new Vector2(-ReferenceGalleryNavigator.FeatureLauncherRightInset,
                    -ReferenceGalleryNavigator.FeatureLauncherCharacterTop)));
            Assert.That(launcherRect.rect.size,
                Is.EqualTo(ReferenceGalleryNavigator.FeatureLauncherSize));

            navigator.ShowPage(ReferenceGalleryPage.Household);
            Assert.That(featureRoot.activeSelf, Is.False,
                "the launcher leaves with its page tab");
        }

        [Test]
        public void ShowPage_FirstCharacterAndFamilyVisitsOrderTheShelfBelowTheOverlays()
        {
            GalleryFixture fixture = CreateGalleryFixture();
            fixture.Navigator.ShowPage(ReferenceGalleryPage.Character);
            Canvas.ForceUpdateCanvases();

            AssertShelfBand(fixture, new[] { LauncherControl(fixture.EducationRoot) });

            fixture.Navigator.ShowPage(ReferenceGalleryPage.Family);
            Canvas.ForceUpdateCanvases();

            AssertShelfBand(fixture, new[]
            {
                LauncherControl(fixture.ResearchRoot),
                LauncherControl(fixture.FormationRoot),
                LauncherControl(fixture.DoctrineRoot)
            });
        }

        [Test]
        public void ShowPage_Family_KeepsAnOpenSuccessionModalAboveTheShelf()
        {
            GalleryFixture fixture = CreateGalleryFixture();

            fixture.Navigator.ShowPage(ReferenceGalleryPage.Family);
            Canvas.ForceUpdateCanvases();

            Button doctrine = LauncherControl(fixture.DoctrineRoot);
            var confirmRect = (RectTransform)fixture.Confirm.transform;
            MoveOver(confirmRect, (RectTransform)doctrine.transform);
            Vector2 point = CanvasPoint(fixture.HostRect, confirmRect);
            point += new Vector2(-confirmRect.rect.width * 0.45f, 0f);
            Assert.That(TopmostRaycast(fixture.HostRect, point),
                Is.SameAs(fixture.DoctrineRoot.GetComponent<Image>()),
                "with the modal closed the launcher owns its own shelf point");

            // The succession HUD opens the modal after the page was already shown, so the
            // depth pass has to react to the modal becoming visible, not only to navigation.
            fixture.SuccessionModal.SetActive(true);
            InvokeLateUpdate(fixture.Navigator);

            Assert.That(fixture.SuccessionRoot.transform.GetSiblingIndex(),
                Is.GreaterThan(fixture.DoctrineRoot.transform.GetSiblingIndex()),
                "an open modal must render above the Family shelf");
            Assert.That(fixture.SuccessionRoot.transform.GetSiblingIndex(),
                Is.LessThan(fixture.InspectorRoot.transform.GetSiblingIndex()),
                "lifting the modal must not put it above the lifecycle overlays");
            Assert.That(TopmostRaycast(fixture.HostRect, point),
                Is.SameAs(fixture.Confirm.GetComponent<Image>()),
                "the modal control, not the shelf launcher, receives the pointer");
        }

        /// <summary>Places one rect exactly over another in the shared canvas plane.</summary>
        private static void MoveOver(RectTransform moving, RectTransform over)
        {
            Vector3 world = over.TransformPoint(over.rect.center);
            moving.anchoredPosition = moving.parent.InverseTransformPoint(world);
        }

        /// <summary>The launcher control of a feature root: the root itself or its first child.</summary>
        private static Button LauncherControl(GameObject root)
        {
            Button button = root.GetComponent<Button>();
            if (button != null)
                return button;
            for (int i = 0; i < root.transform.childCount; i++)
                if ((button = root.transform.GetChild(i).GetComponent<Button>()) != null)
                    return button;
            return null;
        }

        private void AssertShelfBand(GalleryFixture fixture, Button[] shelf)
        {
            int pageIndex = fixture.PageRoot.transform.GetSiblingIndex();
            int ceiling = fixture.Host.transform.childCount;
            GameObject[] overlays =
            {
                fixture.InspectorRoot, fixture.TitleRoot, fixture.EventRoot, fixture.TacticalRoot
            };
            foreach (GameObject overlay in overlays)
            {
                Assert.That(overlay.transform.GetSiblingIndex(), Is.GreaterThan(pageIndex),
                    overlay.name + " must stay above the page root");
                ceiling = Mathf.Min(ceiling, overlay.transform.GetSiblingIndex());
            }

            int previous = pageIndex;
            foreach (Button button in shelf)
            {
                Transform root = button.transform == fixture.DoctrineRoot.transform
                    ? button.transform
                    : button.transform.parent;
                int index = root.GetSiblingIndex();
                Assert.That(index, Is.GreaterThan(previous),
                    root.name + " must render above the page root and the previous launcher");
                Assert.That(index, Is.LessThan(ceiling),
                    root.name + " must render below every lifecycle overlay");
                Assert.That(index, Is.EqualTo(previous + 1),
                    root.name + " belongs in the contiguous shelf band");
                Assert.That(button.gameObject.activeInHierarchy, Is.True);
                Assert.That(button.targetGraphic.raycastTarget, Is.True);
                Assert.That(TopmostRaycast(fixture.HostRect,
                        CanvasPoint(fixture.HostRect, (RectTransform)button.transform)),
                    Is.SameAs(button.targetGraphic),
                    root.name + " must be the topmost raycast target at its own centre");
                previous = index;
            }
        }

        private sealed class GalleryFixture
        {
            public GameObject Host;
            public RectTransform HostRect;
            public GameObject PageRoot;
            public ReferenceGalleryNavigator Navigator;
            public GameObject EducationRoot;
            public GameObject ResearchRoot;
            public GameObject FormationRoot;
            public GameObject DoctrineRoot;
            public GameObject ContentRoot;
            public GameObject SuccessionRoot;
            public GameObject SuccessionModal;
            public GameObject Confirm;
            public GameObject InspectorRoot;
            public GameObject TitleRoot;
            public GameObject EventRoot;
            public GameObject TacticalRoot;
        }

        /// <summary>
        /// A Canvas ordered the way the scene updaters produce it: the page content and the
        /// launcher roots exist before M02DailyActionRoot, and the lifecycle overlays are
        /// generated after it. The shelf must land between the page root and the overlays
        /// on the very first visit, without replaying a navigation.
        /// </summary>
        private GalleryFixture CreateGalleryFixture()
        {
            var fixture = new GalleryFixture();
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            var hostRect = (RectTransform)host.transform;
            hostRect.sizeDelta = new Vector2(1280f, 720f);
            fixture.Host = host;
            fixture.HostRect = hostRect;

            fixture.ContentRoot = CreateStretched("FamilyRosterRoot", host.transform);

            fixture.SuccessionRoot = CreateStretched("SuccessionRoot", host.transform);
            GameObject hud = Create("SuccessionHud", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            hud.transform.SetParent(fixture.SuccessionRoot.transform, false);
            ((RectTransform)hud.transform).sizeDelta = new Vector2(340f, 260f);
            fixture.SuccessionModal = Create("SuccessionModal", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            fixture.SuccessionModal.transform.SetParent(fixture.SuccessionRoot.transform, false);
            var modalRect = (RectTransform)fixture.SuccessionModal.transform;
            modalRect.anchorMin = modalRect.anchorMax = modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(860f, 600f);
            fixture.Confirm = Create("SuccessionConfirmButton", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            fixture.Confirm.transform.SetParent(fixture.SuccessionModal.transform, false);
            fixture.Confirm.GetComponent<Button>().targetGraphic =
                fixture.Confirm.GetComponent<Image>();
            var confirmRect = (RectTransform)fixture.Confirm.transform;
            confirmRect.anchorMin = confirmRect.anchorMax = confirmRect.pivot =
                new Vector2(0.5f, 0.5f);
            confirmRect.sizeDelta = ReferenceGalleryNavigator.FeatureLauncherSize;
            confirmRect.anchoredPosition = Vector2.zero;

            fixture.EducationRoot = CreateLauncherRoot("SC020EducationRoot", host.transform,
                "SC020EducationOpenButton", "子女培养");
            fixture.ResearchRoot = CreateLauncherRoot("SC021ResearchRoot", host.transform,
                "SC021ResearchOpenButton", "家族研究");
            fixture.FormationRoot = CreateLauncherRoot("SC022FamilyFormationRoot",
                host.transform, "SC022FamilyFormationOpenButton", "婚姻与收养");
            fixture.DoctrineRoot = CreateLauncherRoot("FamilyDoctrineOpenButton",
                host.transform, null, "家训与抱负");

            fixture.PageRoot = Create("M02DailyActionRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            fixture.PageRoot.transform.SetParent(host.transform, false);
            CreateFullScreen((RectTransform)fixture.PageRoot.transform);
            fixture.Navigator = fixture.PageRoot.AddComponent<ReferenceGalleryNavigator>();
            fixture.InspectorRoot = CreateStretched("M03NpcAiInspectorRoot", host.transform);
            fixture.TitleRoot = CreateStretched("M6TitleRoot", host.transform);
            fixture.EventRoot = CreateStretched("EventChoiceRoot", host.transform);
            fixture.TacticalRoot = CreateStretched("M6TacticalMissionOverlay", host.transform);

            Button[] tabs = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < tabs.Length; i++)
                tabs[i] = CreateButton("GalleryTab" + i);
            GameObject householdPanel = CreateStretched("IndustryRoot", host.transform);
            GameObject mapPanel = CreateStretched("M2WorldMapRoot", host.transform);
            fixture.Navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(fixture.ContentRoot, ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(fixture.SuccessionRoot,
                    ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(fixture.EducationRoot,
                    ReferenceGalleryPageMask.Character),
                new ReferenceGalleryPageRoot(fixture.ResearchRoot, ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(fixture.FormationRoot,
                    ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(fixture.DoctrineRoot,
                    ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(fixture.InspectorRoot,
                    ReferenceGalleryPageMask.Character)
            }, tabs, fixture.PageRoot, fixture.ContentRoot, householdPanel, mapPanel, tabs[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));
            fixture.Navigator.BindFeatureLaunchers(new[]
            {
                new ReferenceGalleryPageRoot(fixture.EducationRoot,
                    ReferenceGalleryPageMask.Character),
                new ReferenceGalleryPageRoot(fixture.ResearchRoot, ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(fixture.FormationRoot,
                    ReferenceGalleryPageMask.Family),
                new ReferenceGalleryPageRoot(fixture.DoctrineRoot,
                    ReferenceGalleryPageMask.Family)
            });
            fixture.SuccessionModal.SetActive(false);
            return fixture;
        }

        private GameObject CreateLauncherRoot(string rootName, Transform parent,
            string buttonName, string label)
        {
            GameObject root = Create(rootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            CreateFullScreen((RectTransform)root.transform);
            GameObject target = buttonName == null
                ? root
                : Create(buttonName, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
            if (target != root)
            {
                target.transform.SetParent(root.transform, false);
                ((RectTransform)target.transform).sizeDelta =
                    ReferenceGalleryNavigator.FeatureLauncherSize;
            }
            else
            {
                target.AddComponent<CanvasRenderer>();
                target.AddComponent<Image>();
                target.AddComponent<Button>();
            }
            var rect = (RectTransform)target.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.sizeDelta = ReferenceGalleryNavigator.FeatureLauncherSize;
            rect.anchoredPosition = Vector2.zero;
            Image image = target.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = target.GetComponent<Button>();
            button.targetGraphic = image;
            GameObject labelObject = Create("LauncherLabel", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(target.transform, false);
            Text text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.raycastTarget = false;
            return root;
        }

        private GameObject CreateStretched(string name, Transform parent)
        {
            GameObject go = Create(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            CreateFullScreen((RectTransform)go.transform);
            return go;
        }

        private static void CreateFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void InvokeLateUpdate(ReferenceGalleryNavigator navigator) =>
            typeof(ReferenceGalleryNavigator).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(navigator, null);

        private static Vector2 CanvasPoint(RectTransform canvas, RectTransform rect) =>
            canvas.InverseTransformPoint(rect.TransformPoint(rect.rect.center));

        /// <summary>The topmost raycast target at a point in uGUI draw order.</summary>
        private static Graphic TopmostRaycast(Transform canvas, Vector2 canvasPoint)
        {
            Vector3 world = canvas.TransformPoint(canvasPoint);
            Graphic found = null;
            for (int i = 0; i < canvas.childCount; i++)
            {
                Graphic child = TopmostRaycastIn(canvas.GetChild(i), world);
                if (child != null)
                    found = child;
            }
            return found;
        }

        private static Graphic TopmostRaycastIn(Transform node, Vector3 world)
        {
            Graphic found = null;
            foreach (Graphic graphic in node.GetComponents<Graphic>())
            {
                if (!graphic.enabled || !graphic.gameObject.activeInHierarchy ||
                    !graphic.raycastTarget || graphic.color.a <= 0.01f)
                    continue;
                if (graphic is Text text && string.IsNullOrEmpty(text.text))
                    continue;
                RectTransform rect = graphic.rectTransform;
                if (rect.rect.Contains(rect.InverseTransformPoint(world)))
                    found = graphic;
            }
            for (int i = 0; i < node.childCount; i++)
            {
                Graphic child = TopmostRaycastIn(node.GetChild(i), world);
                if (child != null)
                    found = child;
            }
            return found;
        }

        [Test]
        public void ShowPage_Household_PresentsRosterIndustryMonthEventAndPendingDecisions()
        {
            GameSession session = GameSession.NewGame();
            _objects.Add(session.gameObject);
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);

            GameObject nav = Create("Navigation", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            nav.transform.SetParent(host.transform, false);
            GameObject header = Create("Header");
            Button launcher = CreateButton("Launcher");
            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);

            GameObject eventRoot = Create("EventChoiceRoot", typeof(RectTransform));
            eventRoot.transform.SetParent(host.transform, false);
            eventRoot.AddComponent<EventChoicePanel>();
            var householdPanel = Create("IndustryPanel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            householdPanel.transform.SetParent(host.transform, false);
            var roots = new[]
            {
                new ReferenceGalleryPageRoot(eventRoot,
                    ReferenceGalleryPageMask.Household | ReferenceGalleryPageMask.Events)
            };
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(roots, buttons, householdPanel, householdPanel,
                householdPanel, householdPanel, launcher, nav, header);
            navigator.BindGameSession(session);

            navigator.ShowPage(ReferenceGalleryPage.Household);

            Assert.That(navigator.HouseholdComposition, Is.Not.Null,
                "the household page composes the reference regions, not the industry panel alone");
            Assert.That(navigator.HouseholdComposition.activeSelf, Is.True);
            Transform headRail = FindDeep(navigator.HouseholdComposition.transform, "HouseholdHeadRail");
            Assert.That(headRail, Is.Not.Null, "the head/date sidebar region exists");
            Text headName = FindDeep(headRail, "HouseholdHeadName").GetComponent<Text>();
            Assert.That(headName.text, Does.Contain(session.SuccessionHeadName),
                "the sidebar shows the live head of household");
            Assert.That(FindDeep(headRail, "HouseholdHeadDate"), Is.Not.Null,
                "the sidebar shows the live game date");
            Transform rosterRow = FindDeep(headRail, "HouseholdRosterRow_0");
            Assert.That(rosterRow, Is.Not.Null, "live family roster rows are listed");
            FamilyTreeGraphSnapshot liveGraph = FamilyTreeGraphBuilder.Build(session.Succession,
                session.WorldClock.CurrentDate);
            Assert.That(rosterRow.GetComponent<Text>().text,
                Does.Contain(liveGraph.Persons[0].DisplayName),
                "the roster shows the first member from the live family graph");
            if (!string.IsNullOrWhiteSpace(session.FixedHeirSummary))
                Assert.That(FindDeep(headRail, "HouseholdHeir").GetComponent<Text>().text,
                    Is.EqualTo(session.FixedHeirSummary));

            // The industry panel keeps the centre appointments column.
            RectTransform panelRect = (RectTransform)householdPanel.transform;
            Assert.That(panelRect.anchoredPosition,
                Is.EqualTo(ReferenceGalleryNavigator.HouseholdIndustryPosition));
            Assert.That(Vector2.Scale(panelRect.sizeDelta, panelRect.localScale),
                Is.EqualTo(ReferenceGalleryNavigator.HouseholdIndustrySize));

            // Pending decisions come from the live queue; absent data stays absent.
            string[] pendingIds = session.EventRuntimeStateForSave.pendingEventIds;
            if (pendingIds != null && pendingIds.Length > 0)
            {
                Assert.That(navigator.PendingEventsRail, Is.Not.Null);
                Assert.That(navigator.PendingEventsRail.activeSelf, Is.True);
                int rows = 0;
                foreach (Transform child in navigator.PendingEventsRail.transform)
                    if (child.name.StartsWith("PendingEventRow_", StringComparison.Ordinal))
                        rows++;
                Assert.That(rows, Is.GreaterThanOrEqualTo(1).And.LessThanOrEqualTo(pendingIds.Length),
                    "the rail lists the live pending events");
                Assert.That(FindDeep(navigator.PendingEventsRail.transform,
                    "PendingEventRow_" + pendingIds[0]), Is.Not.Null,
                    "the rail lists the actual pending event, not an example");
            }
            else
            {
                Assert.That(navigator.PendingEventsRail == null ||
                    !navigator.PendingEventsRail.activeSelf, Is.True,
                    "no pending decisions means no pending rail");
            }

            // No invisible raycast blockers anywhere in the composition.
            foreach (Image image in navigator.HouseholdComposition
                         .GetComponentsInChildren<Image>(true))
                Assert.That(image.raycastTarget, Is.False,
                    "household decorations must not block raycasts");
            if (navigator.PendingEventsRail != null && navigator.PendingEventsRail.activeSelf)
                Assert.That(navigator.PendingEventsRail.GetComponent<Image>().raycastTarget,
                    Is.False);

            // Regions stay inside the 1280x720 canvas and do not overlap each other.
            Assert.That(OverlapsLocal(ReferenceGalleryNavigator.HouseholdIndustryPosition,
                ReferenceGalleryNavigator.HouseholdIndustrySize,
                ReferenceGalleryNavigator.HouseholdHeadRailPosition,
                ReferenceGalleryNavigator.HouseholdHeadRailSize), Is.False);
            Assert.That(OverlapsLocal(ReferenceGalleryNavigator.HouseholdIndustryPosition,
                ReferenceGalleryNavigator.HouseholdIndustrySize,
                ReferenceGalleryNavigator.HouseholdMonthEventPosition,
                ReferenceGalleryNavigator.HouseholdMonthEventSize), Is.False);
            Assert.That(OverlapsLocal(ReferenceGalleryNavigator.PendingRailPosition,
                ReferenceGalleryNavigator.PendingRailSize,
                ReferenceGalleryNavigator.HouseholdModalPosition,
                ReferenceGalleryNavigator.HouseholdModalSize), Is.False);
            Assert.That(Mathf.Abs(ReferenceGalleryNavigator.PendingRailPosition.x + 126f) <= 640f,
                Is.True, "the pending rail stays inside the safe canvas");

            navigator.ShowPage(ReferenceGalleryPage.Events);
            Assert.That(navigator.HouseholdComposition.activeSelf, Is.False,
                "the household composition must not linger on other pages");
            Assert.That(navigator.PendingEventsRail == null ||
                !navigator.PendingEventsRail.GetComponent<Image>().raycastTarget, Is.True);

            navigator.ShowPage(ReferenceGalleryPage.Map);
            Assert.That(navigator.HouseholdComposition.activeSelf, Is.False);
            Assert.That(navigator.PendingEventsRail == null ||
                !navigator.PendingEventsRail.activeSelf, Is.True,
                "the pending rail leaves no blocker on the map page");
        }

        [Test]
        public void ShowPage_Household_ReflowsIndustryPanelWithoutShrinkingItsFullScreenRoot()
        {
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            GameObject industryRoot = Create("IndustryRoot", typeof(RectTransform));
            industryRoot.transform.SetParent(host.transform, false);
            RectTransform rootRect = (RectTransform)industryRoot.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            GameObject industryPanel = Create("IndustryPanel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            industryPanel.transform.SetParent(industryRoot.transform, false);
            RectTransform panelRect = (RectTransform)industryPanel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 560f);

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(industryRoot, ReferenceGalleryPageMask.Household)
            }, buttons, industryRoot, industryRoot, industryRoot, industryRoot, buttons[0],
                Create("Navigation"), Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Household);

            Assert.That(rootRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rootRect.anchorMax, Is.EqualTo(Vector2.one),
                "the industry controller root remains full-screen for its child controls");
            Assert.That(rootRect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rootRect.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(Vector2.Scale(panelRect.sizeDelta, panelRect.localScale),
                Is.EqualTo(ReferenceGalleryNavigator.HouseholdIndustrySize));
            Assert.That(panelRect.anchoredPosition,
                Is.EqualTo(ReferenceGalleryNavigator.HouseholdIndustryPosition));
        }

        [Test]
        public void ShowPage_Map_KeepsIllustrationAspectFitAndClearOfTravelControls()
        {
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);

            GameObject mapRoot = Create("MapRoot", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            mapRoot.transform.SetParent(host.transform, false);
            string[] controlNames =
            {
                "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
                "DestinationNameText", "TravelTimeText", "ArrivalDateText",
                "ConfirmButton", "CancelButton",
                "DestButton_0", "DestButton_1", "DestButton_2"
            };
            var rects = new Dictionary<string, RectTransform>();
            foreach (string name in controlNames)
            {
                bool isButton = name == "ConfirmButton" || name == "CancelButton" ||
                    name.StartsWith("DestButton_", StringComparison.Ordinal);
                GameObject child = isButton
                    ? Create(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                        typeof(Button))
                    : Create(name, typeof(RectTransform), typeof(Text));
                child.transform.SetParent(mapRoot.transform, false);
                if (isButton)
                    child.GetComponent<Button>().targetGraphic = child.GetComponent<Image>();
                rects[name] = (RectTransform)child.transform;
            }

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(mapRoot, ReferenceGalleryPageMask.Map)
            }, buttons, mapRoot, mapRoot, mapRoot, mapRoot, buttons[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Map);

            Transform illustration = mapRoot.transform.Find("ReferenceMapIllustration");
            Assert.That(illustration, Is.Not.Null);
            RectTransform image = (RectTransform)illustration;
            Sprite sprite = illustration.GetComponent<Image>().sprite;
            Assert.That(sprite, Is.Not.Null);

            // Source aspect ratio preserved.
            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float imageAspect = image.sizeDelta.x / image.sizeDelta.y;
            Assert.That(imageAspect, Is.EqualTo(spriteAspect).Within(0.01f),
                "the map illustration must keep its source aspect ratio");
            Assert.That(image.sizeDelta.x, Is.GreaterThan(560f),
                "the central map crop should use the space between the side columns");

            // Inside the central map rectangle of the root.
            float rootWidth = ((RectTransform)mapRoot.transform).sizeDelta.x;
            float rootHeight = ((RectTransform)mapRoot.transform).sizeDelta.y;
            Assert.That(rootWidth, Is.EqualTo(1060f).Within(1f));
            Assert.That(mapRoot.transform.Find("CurrentDateText").GetComponent<Text>().color,
                Is.EqualTo(SengokuUiTheme.Parchment));
            Assert.That(mapRoot.transform.Find("LocationMarkerText").GetComponent<Text>().color,
                Is.EqualTo(SengokuUiTheme.Parchment));
            Assert.That(mapRoot.transform.Find("FeedbackText").GetComponent<Text>().color,
                Is.EqualTo(SengokuUiTheme.Parchment),
                "map status text stays legible over the indigo background");
            Assert.That(Mathf.Abs(image.anchoredPosition.x) + image.sizeDelta.x / 2f,
                Is.LessThanOrEqualTo(rootWidth *
                    ReferenceGalleryNavigator.MapBoxHalfWidthFraction + 0.1f));
            Assert.That(image.anchoredPosition.y + image.sizeDelta.y / 2f,
                Is.LessThanOrEqualTo(rootHeight *
                    ReferenceGalleryNavigator.MapBoxTopFraction + 0.1f));
            Assert.That(image.anchoredPosition.y - image.sizeDelta.y / 2f,
                Is.GreaterThanOrEqualTo(-rootHeight / 2f - 0.1f));

            // Location markers overlay the terrain; panels and dock remain outside it.
            foreach (KeyValuePair<string, RectTransform> control in rects)
            {
                if (control.Key.StartsWith("DestButton_", StringComparison.Ordinal)) continue;
                Assert.That(RectsOverlap(image.anchoredPosition, image.sizeDelta,
                        control.Value.anchoredPosition, control.Value.sizeDelta), Is.False,
                    "the map artwork must not cover " + control.Key);
            }
            Assert.That(illustration.GetSiblingIndex(), Is.Zero,
                "the artwork stays behind every interactive control");
            Assert.That(illustration.GetComponent<Image>().raycastTarget, Is.False);
        }

        [Test]
        public void ShowPage_Map_UsesGeneratedWorldMapSpriteWhenAvailable()
        {
            SengokuUiArt.ClearCache();
            Sprite generated = SengokuUiArt.WorldMapIllustration;
            if (generated == null)
                Assert.Ignore(
                    "Resources/Generated/world_map_illustration is not imported yet; " +
                    "the map page keeps its reference-art fallback until it is.");

            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);

            GameObject mapRoot = Create("MapRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            mapRoot.transform.SetParent(host.transform, false);

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(mapRoot, ReferenceGalleryPageMask.Map)
            }, buttons, mapRoot, mapRoot, mapRoot, mapRoot, buttons[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Map);

            Transform illustration = mapRoot.transform.Find(
                ReferenceGalleryNavigator.MapIllustrationChildName);
            Assert.That(illustration, Is.Not.Null);
            Image image = illustration.GetComponent<Image>();
            Assert.That(image.sprite, Is.SameAs(generated),
                "the map page must use the generated world map, not the " +
                "reference-art fallback");
            Assert.That(image.raycastTarget, Is.False);
            Assert.That(illustration.GetSiblingIndex(), Is.Zero,
                "the generated artwork stays behind every interactive control");
        }

        [Test]
        public void ShowPage_Map_KeepsShadowEscortCommissionColorsInsideMapRoot()
        {
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);

            GameObject mapRoot = Create("MapRoot", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            mapRoot.transform.SetParent(host.transform, false);

            Text mapHint = CreateTextChild("MapRouteHintText", mapRoot.transform);
            // Nested map-owned label: the dark-map restyle must still reach it.
            GameObject mapColumn = Create("MapRouteColumn", typeof(RectTransform));
            mapColumn.transform.SetParent(mapRoot.transform, false);
            mapHint.transform.SetParent(mapColumn.transform, false);
            Button mapButton = CreateButton("MapRouteButton");
            mapButton.transform.SetParent(mapRoot.transform, false);
            Text mapButtonLabel = CreateTextChild("Label", mapButton.transform);

            // The M5 commission panel is hosted under the map root as root -> page and
            // draws its own indigo sheet, so it keeps the light colors authored in the
            // scene instead of the recursive dark-map restyle.
            GameObject townRoot = Create("TownMenuRoot", typeof(RectTransform));
            townRoot.transform.SetParent(mapRoot.transform, false);
            GameObject commissionRoot = Create(
                ShadowEscortCommissionPanel.RootObjectName, typeof(RectTransform));
            commissionRoot.transform.SetParent(townRoot.transform, false);
            GameObject commissionPage = Create(
                ShadowEscortCommissionPanel.PageObjectName, typeof(RectTransform));
            commissionPage.transform.SetParent(commissionRoot.transform, false);
            Text commissionBody = CreateTextChild("M5CommissionOfferText",
                commissionPage.transform);
            Button commissionButton = CreateButton("M5CommissionAcceptButton");
            commissionButton.transform.SetParent(commissionPage.transform, false);
            Text commissionLabel = CreateTextChild("Label", commissionButton.transform);
            var commissionButtonBackground = new Color(0.24f, 0.32f, 0.42f, 1f);
            commissionBody.color = SengokuUiTheme.Parchment;
            commissionLabel.color = SengokuUiTheme.ParchmentLight;
            commissionButton.targetGraphic.color = commissionButtonBackground;

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Tab" + i);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(mapRoot, ReferenceGalleryPageMask.Map)
            }, buttons, mapRoot, mapRoot, mapRoot, mapRoot, buttons[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Map);

            Assert.That(commissionBody.color, Is.EqualTo(SengokuUiTheme.Parchment),
                "the map restyle must not paint M5 body text with the dark map ink");
            Assert.That(commissionLabel.color, Is.EqualTo(SengokuUiTheme.ParchmentLight),
                "the map restyle must not repaint M5 button labels");
            Assert.That(commissionButton.targetGraphic.color,
                Is.EqualTo(commissionButtonBackground),
                "the map restyle must not repaint M5 commission buttons");

            Assert.That(mapHint.color, Is.EqualTo(SengokuUiTheme.Ink),
                "map-owned text keeps the dark map ink");
            Assert.That(mapButtonLabel.color, Is.EqualTo(SengokuUiTheme.Ink),
                "map-owned button labels keep the dark map ink");
            Assert.That(mapButton.targetGraphic.color, Is.EqualTo(SengokuUiTheme.ParchmentDeep),
                "map-owned buttons keep the parchment surface");
        }

        [Test]
        public void ShowPage_Events_ReflowsLivePendingEventIntoReferenceCardGeometry()
        {
            GameSession session = GameSession.NewGame();
            session.InitStandardDemo(1234UL);
            _objects.Add(session.gameObject);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(string.IsNullOrEmpty(session.PendingEventId), Is.False,
                "the live session now has a pending event");

            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);

            GameObject eventRoot = Create("EventChoiceRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            eventRoot.transform.SetParent(host.transform, false);
            ((RectTransform)eventRoot.transform).anchorMin = Vector2.zero;
            ((RectTransform)eventRoot.transform).anchorMax = Vector2.one;
            GameObject modal = Create("EventChoiceModal", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            modal.transform.SetParent(eventRoot.transform, false);
            CreateTextChild("EventChoiceTitleText", modal.transform);
            CreateTextChild("EventChoiceBodyText", modal.transform);
            GameObject options = Create("EventChoiceOptions", typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            options.transform.SetParent(modal.transform, false);
            GameObject template = Create("EventChoiceOptionButtonTemplate", typeof(RectTransform),
                typeof(Image), typeof(Button), typeof(LayoutElement));
            template.transform.SetParent(options.transform, false);
            CreateTextChild("EventChoiceOptionLabel", template.transform);
            template.SetActive(false);
            Button templateButton = template.GetComponent<Button>();
            templateButton.targetGraphic = template.GetComponent<Image>();

            EventChoicePanel panel = eventRoot.AddComponent<EventChoicePanel>();
            panel.BindReferences(session, session.EventCatalog, session.EventText,
                modal, modal.GetComponentInChildren<Text>(true),
                FindDeep(modal.transform, "EventChoiceBodyText").GetComponent<Text>(),
                options.transform, templateButton,
                CreateTextChild("EventChoiceFeedbackText", eventRoot.transform).GetComponent<Text>());

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);
            GameObject characterPanel = Create("CharacterPanel", typeof(RectTransform));
            characterPanel.transform.SetParent(host.transform, false);
            GameObject familyPanel = Create("FamilyPanel", typeof(RectTransform));
            familyPanel.transform.SetParent(host.transform, false);
            GameObject householdPanel = Create("HouseholdPanel", typeof(RectTransform));
            householdPanel.transform.SetParent(host.transform, false);
            GameObject mapPanel = Create("MapPanel", typeof(RectTransform));
            mapPanel.transform.SetParent(host.transform, false);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(eventRoot,
                    ReferenceGalleryPageMask.Household | ReferenceGalleryPageMask.Events)
            }, buttons, characterPanel, familyPanel, householdPanel, mapPanel, buttons[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));
            navigator.BindGameSession(session);

            navigator.ShowPage(ReferenceGalleryPage.Events);

            RectTransform modalRect = (RectTransform)modal.transform;
            Assert.That(modal.activeSelf, Is.True,
                "the live pending event renders in the reference card region");
            Assert.That(modalRect.sizeDelta, Is.EqualTo(ReferenceGalleryNavigator.EventsModalSize));
            Assert.That(modalRect.anchoredPosition,
                Is.EqualTo(ReferenceGalleryNavigator.EventsModalPosition));

            // Live content: the modal title equals the actual pending event's text.
            Text title = FindDeep(modal.transform, "EventChoiceTitleText").GetComponent<Text>();
            string eventId = session.PendingEventId;
            string expectedTitle = null;
            string pendingTextId = null;
            foreach (SengokuChronicle.Events.EventDefinitionData definition in session.EventCatalog.events)
                if (definition != null && definition.eventId == eventId)
                    pendingTextId = definition.textId;
            Assert.That(pendingTextId, Is.Not.Null);
            foreach (SengokuChronicle.Events.EventTextEntry entry in session.EventText.entries)
                if (entry != null && entry.textId == pendingTextId)
                    expectedTitle = entry.title;
            Assert.That(title.text, Is.EqualTo(expectedTitle),
                "the card shows the live pending event, not an example");

            // Option buttons keep their command bindings after the reflow.
            int invocable = 0;
            foreach (Button button in options.transform.GetComponentsInChildren<Button>(true))
                if (button.gameObject.name.StartsWith("EventOption_", StringComparison.Ordinal) &&
                    button.interactable)
                    invocable++;
            Assert.That(invocable, Is.GreaterThan(0),
                "at least one live option stays invocable after the reflow");

            Assert.That(navigator.PendingEventsRail, Is.Not.Null);
            Assert.That(navigator.PendingEventsRail.activeSelf, Is.True);
            RectTransform railRect = (RectTransform)navigator.PendingEventsRail.transform;
            Assert.That(railRect.anchoredPosition,
                Is.EqualTo(ReferenceGalleryNavigator.EventsRailPosition));
            Assert.That(RectsOverlap(modalRect.anchoredPosition, modalRect.sizeDelta,
                railRect.anchoredPosition, railRect.sizeDelta), Is.False,
                "the story card and the pending-events area must not overlap");

            Assert.That(session.PendingEventId, Is.EqualTo(eventId),
                "showing the page must not commit anything by itself");
        }

        [Test]
        public void ShowPage_AllFivePagesActivateOnlyTheirOwnRootsAndSharedChrome()
        {
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            GameObject navigation = Create("Navigation", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            navigation.transform.SetParent(host.transform, false);
            GameObject header = Create("Header", typeof(RectTransform));
            header.transform.SetParent(host.transform, false);
            Button launcher = CreateButton("Launcher");
            launcher.transform.SetParent(host.transform, false);

            var roots = new ReferenceGalleryPageRoot[ReferenceGalleryNavigator.PageCount];
            var panels = new GameObject[ReferenceGalleryNavigator.PageCount];
            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject pageRoot = Create("PageRoot" + i, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                pageRoot.transform.SetParent(host.transform, false);
                roots[i] = new ReferenceGalleryPageRoot(pageRoot,
                    (ReferenceGalleryPageMask)(1 << i));
                // BindReferences carries exactly four dedicated panel roots
                // (character, family, household, map). The Events page has no
                // panel argument: its live content is the EventChoiceRoot page
                // root, whose Household|Events mask is asserted through roots[].
                if (i != (int)ReferenceGalleryPage.Events)
                {
                    panels[i] = Create("PagePanel" + i, typeof(RectTransform));
                    panels[i].transform.SetParent(host.transform, false);
                }
                buttons[i] = CreateButton("Button" + i);
            }

            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(roots, buttons, panels[1], panels[2], panels[0],
                panels[3], launcher, navigation, header);

            for (int pageIndex = 0; pageIndex < roots.Length; pageIndex++)
            {
                ReferenceGalleryPage page = (ReferenceGalleryPage)pageIndex;
                navigator.ShowPage(page);

                Assert.That(navigator.CurrentPage, Is.EqualTo(page));
                Assert.That(navigation.activeSelf, Is.True);
                Assert.That(header.activeSelf, Is.True);
                Assert.That(launcher.gameObject.activeSelf, Is.False);
                for (int i = 0; i < roots.Length; i++)
                {
                    Assert.That(roots[i].Root.activeSelf, Is.EqualTo(i == pageIndex),
                        roots[i].Root.name + " must only be active on its own page");
                    if (i == (int)ReferenceGalleryPage.Events)
                    {
                        Assert.That(panels[i], Is.Null,
                            "the Events page has no separate panel root; EventChoiceRoot is its page root");
                    }
                    else
                    {
                        Assert.That(panels[i], Is.Not.Null,
                            "every non-events page owns a dedicated panel root");
                        Assert.That(panels[i].activeSelf, Is.EqualTo(i == pageIndex),
                            panels[i].name + " must only be active on its own page");
                    }
                    Assert.That(buttons[i].interactable, Is.True,
                        "page tabs stay interactive so the selected page is explicit");
                    Assert.That((Color32)buttons[i].targetGraphic.color,
                        Is.EqualTo((Color32)(i == pageIndex
                            ? ReferenceGalleryNavigator.PageSelectedBackground
                            : ReferenceGalleryNavigator.PageIdleBackground)),
                        "only the selected tab carries the seal accent");
                }

                Image backdrop = BackdropHost(navigator)
                    .Find("ReferenceGalleryBackdrop").GetComponent<Image>();
                Assert.That(backdrop.raycastTarget, Is.False,
                    "the shared backdrop never blocks page controls");
                Assert.That(backdrop.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(backdrop.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            }
        }

        [Test]
        public void ShowPage_SwitchingAllFivePagesDoesNotCommitSessionState()
        {
            GameSession session = GameSession.NewGame();
            _objects.Add(session.gameObject);
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);

            var roots = new ReferenceGalleryPageRoot[ReferenceGalleryNavigator.PageCount];
            var panels = new GameObject[ReferenceGalleryNavigator.PageCount];
            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject pageRoot = Create("StatePageRoot" + i, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                pageRoot.transform.SetParent(host.transform, false);
                roots[i] = new ReferenceGalleryPageRoot(pageRoot,
                    (ReferenceGalleryPageMask)(1 << i));
                panels[i] = Create("StatePagePanel" + i, typeof(RectTransform));
                panels[i].transform.SetParent(host.transform, false);
                buttons[i] = CreateButton("StateButton" + i);
            }

            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(roots, buttons, panels[1], panels[2], panels[0],
                panels[3], buttons[0], Create("Navigation"), Create("Header"));
            navigator.BindGameSession(session);

            string head = session.SuccessionHeadName;
            var date = session.WorldClock.CurrentDate;
            int merit = session.Merit;
            string pendingEvent = session.PendingEventId;

            foreach (ReferenceGalleryPage page in Enum.GetValues(typeof(ReferenceGalleryPage)))
                navigator.ShowPage(page);

            Assert.That(session.SuccessionHeadName, Is.EqualTo(head));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(session.Merit, Is.EqualTo(merit));
            Assert.That(session.PendingEventId, Is.EqualTo(pendingEvent),
                "page navigation never resolves or queues a pending event");
        }

        [Test]
        public void ShowPage_ReclickingTheCurrentTabDoesNotReplayPageSideEffects()
        {
            GameSession session = GameSession.NewGame();
            _objects.Add(session.gameObject);
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);

            GameObject mapRoot = Create("MapRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            mapRoot.transform.SetParent(host.transform, false);
            Text feedback = Create("Feedback", typeof(RectTransform), typeof(Text))
                .GetComponent<Text>();
            feedback.transform.SetParent(host.transform, false);
            feedback.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            WorldMapController worldMap = mapRoot.AddComponent<WorldMapController>();
            worldMap.BindReferences(session, null, null, feedback, null, null, null, null,
                null, null, null, null, null, null);

            GameObject eventRoot = Create("EventChoiceRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            eventRoot.transform.SetParent(host.transform, false);
            eventRoot.AddComponent<EventChoicePanel>();
            GameObject characterPanel = Create("CharacterPanel", typeof(RectTransform));
            GameObject familyPanel = Create("FamilyPanel", typeof(RectTransform));
            GameObject householdPanel = Create("HouseholdPanel", typeof(RectTransform));

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(mapRoot, ReferenceGalleryPageMask.Map),
                new ReferenceGalleryPageRoot(eventRoot, ReferenceGalleryPageMask.Events)
            }, buttons, characterPanel, familyPanel, householdPanel, mapRoot, buttons[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));
            navigator.BindGameSession(session);

            navigator.ShowPage(ReferenceGalleryPage.Map);
            int mapReturns = navigator.MapReturnCount;
            Assert.That(mapReturns, Is.GreaterThan(0));
            navigator.ShowPage(ReferenceGalleryPage.Map);
            Assert.That(navigator.MapReturnCount, Is.EqualTo(mapReturns),
                "clicking the active Map tab must not return to the map again");

            navigator.ShowPage(ReferenceGalleryPage.Household);
            int builds = navigator.HouseholdCompositionBuildCount;
            Assert.That(builds, Is.GreaterThan(0));
            navigator.ShowPage(ReferenceGalleryPage.Household);
            Assert.That(navigator.HouseholdCompositionBuildCount, Is.EqualTo(builds),
                "clicking the active Household tab must not rebuild the composition");

            // A real session change may refresh the navigator-owned composition.
            session.InitNewGame();
            Assert.That(navigator.HouseholdCompositionBuildCount,
                Is.GreaterThan(builds),
                "session-state changes, not re-clicks, drive the shared refresh");

            navigator.ShowPage(ReferenceGalleryPage.Events);
            int refreshes = navigator.EventPanelRefreshCount;
            Assert.That(refreshes, Is.GreaterThan(0));
            string pending = session.PendingEventId;
            navigator.ShowPage(ReferenceGalleryPage.Events);
            Assert.That(navigator.EventPanelRefreshCount, Is.EqualTo(refreshes),
                "clicking the active Events tab must not refresh or resolve the event view");
            Assert.That(session.PendingEventId, Is.EqualTo(pending));
        }

        [Test]
        public void ShowPage_Family_CentresTheRosterBetweenTheRailAndSuccessionHud()
        {
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            GameObject familyRoot = Create("FamilyRosterRoot", typeof(RectTransform));
            familyRoot.transform.SetParent(host.transform, false);
            GameObject familyPanel = Create("FamilyRosterPanel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            familyPanel.transform.SetParent(familyRoot.transform, false);
            ((RectTransform)familyPanel.transform).sizeDelta = new Vector2(760f, 620f);
            GameObject scroll = Create("FamilyRosterScroll", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(familyPanel.transform, false);
            ((RectTransform)scroll.transform).sizeDelta = new Vector2(700, 410);
            var graph = Create("Graph", typeof(RectTransform));
            graph.transform.SetParent(scroll.transform, false);
            var graphRect = (RectTransform)graph.transform;
            graphRect.anchorMin = graphRect.anchorMax = graphRect.pivot = new Vector2(0, 1);
            graphRect.sizeDelta = new Vector2(720, 732);
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.viewport = (RectTransform)scroll.transform;
            scrollRect.content = graphRect;
            familyRoot.AddComponent<FamilyRosterPanel>();

            GameObject successionRoot = Create("SuccessionRoot", typeof(RectTransform));
            successionRoot.transform.SetParent(host.transform, false);
            GameObject successionHud = Create("SuccessionHud", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            successionHud.transform.SetParent(successionRoot.transform, false);
            GameObject characterPanel = Create("CharacterPanel", typeof(RectTransform));
            GameObject householdPanel = Create("HouseholdPanel", typeof(RectTransform));
            GameObject mapPanel = Create("MapPanel", typeof(RectTransform));

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("FamilyButton" + i);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(new[]
            {
                new ReferenceGalleryPageRoot(successionRoot, ReferenceGalleryPageMask.Family)
            }, buttons, characterPanel, familyRoot, householdPanel, mapPanel, buttons[0],
                Create("Navigation", typeof(RectTransform), typeof(Image)), Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Family);

            RectTransform roster = (RectTransform)familyPanel.transform;
            Assert.That(roster.anchoredPosition, Is.EqualTo(new Vector2(-100f, -26f)),
                "the roster sits left of the succession HUD and right of the nav rail");
            float left = roster.anchoredPosition.x - roster.rect.width / 2f;
            float right = roster.anchoredPosition.x + roster.rect.width / 2f;
            Assert.That(left, Is.GreaterThanOrEqualTo(-490f),
                "the genealogy card clears the 150px navigation rail");
            Assert.That(right, Is.LessThanOrEqualTo(284f),
                "the genealogy card clears the 340px succession column");
            RectTransform hud = (RectTransform)successionHud.transform;
            Assert.That(hud.sizeDelta, Is.EqualTo(new Vector2(340f, 260f)));
            Assert.That(hud.anchoredPosition, Is.EqualTo(new Vector2(-16f, -70f)),
                "the succession column keeps its reference top-right position");
            Assert.That(right,
                Is.LessThanOrEqualTo(640f - 16f - hud.sizeDelta.x),
                "roster and succession HUD do not overlap");
            ((RectTransform)host.transform).sizeDelta = new Vector2(1108, 831);
            typeof(ReferenceGalleryNavigator).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(navigator, null);
            Assert.That(roster.anchoredPosition.x - roster.rect.width / 2,
                Is.GreaterThanOrEqualTo(-554 + 150), "narrow genealogy must clear the navigation rail");
            Assert.That(roster.anchoredPosition.x + roster.rect.width / 2,
                Is.LessThanOrEqualTo(554 - 16 - 340), "narrow genealogy must clear the succession HUD");
            Assert.That(((RectTransform)scroll.transform).rect.width, Is.LessThan(roster.rect.width));
            Assert.That(hud.anchoredPosition.y, Is.EqualTo(-100));
            Assert.That(scrollRect.horizontalScrollbar, Is.Not.Null, "the full graph must be reachable without drag gestures");
            typeof(ReferenceGalleryNavigator).GetMethod("StyleFamilyPage",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(navigator, null);
            Assert.That(familyPanel.transform.Find("FamilyRosterHorizontalScrollbar"), Is.Not.Null,
                "re-entering the family page reuses its scrollbar");
            Assert.That(scrollRect.horizontalScrollbar.handleRect.rect.width, Is.GreaterThan(0),
                "reconfiguring the page preserves the Scrollbar-managed handle width");
            scrollRect.horizontalScrollbar.value = 1;
            Assert.That(graphRect.anchoredPosition.x, Is.LessThan(0), "scrollbar changes the existing graph offset");
        }

        [Test]
        public void PageRegions_StayInsideThe1280x720SafeCanvasAndDoNotOverlap()
        {
            const float halfWidth = 640f;
            const float halfHeight = 360f;
            var regions = new[]
            {
                new
                {
                    Name = "HouseholdIndustry",
                    Position = ReferenceGalleryNavigator.HouseholdIndustryPosition,
                    Size = ReferenceGalleryNavigator.HouseholdIndustrySize
                },
                new
                {
                    Name = "HouseholdHeadRail",
                    Position = ReferenceGalleryNavigator.HouseholdHeadRailPosition,
                    Size = ReferenceGalleryNavigator.HouseholdHeadRailSize
                },
                new
                {
                    Name = "HouseholdMonthEvent",
                    Position = ReferenceGalleryNavigator.HouseholdMonthEventPosition,
                    Size = ReferenceGalleryNavigator.HouseholdMonthEventSize
                },
                new
                {
                    Name = "PendingRail",
                    Position = ReferenceGalleryNavigator.PendingRailPosition,
                    Size = ReferenceGalleryNavigator.PendingRailSize
                },
                new
                {
                    Name = "HouseholdModal",
                    Position = ReferenceGalleryNavigator.HouseholdModalPosition,
                    Size = ReferenceGalleryNavigator.HouseholdModalSize
                },
                new
                {
                    Name = "EventsModal",
                    Position = ReferenceGalleryNavigator.EventsModalPosition,
                    Size = ReferenceGalleryNavigator.EventsModalSize
                },
                new
                {
                    Name = "EventsRail",
                    Position = ReferenceGalleryNavigator.EventsRailPosition,
                    Size = ReferenceGalleryNavigator.EventsRailSize
                },
                new
                {
                    Name = "MapRoot",
                    Position = ReferenceGalleryNavigator.MapRootPosition,
                    Size = new Vector2(
                        ReferenceGalleryNavigator.MapRootSizeFraction.x * 1280f,
                        ReferenceGalleryNavigator.MapRootSizeFraction.y * 720f)
                }
            };

            foreach (var region in regions)
            {
                Assert.That(region.Position.x - region.Size.x / 2f,
                    Is.GreaterThanOrEqualTo(-halfWidth), region.Name + " left edge");
                Assert.That(region.Position.x + region.Size.x / 2f,
                    Is.LessThanOrEqualTo(halfWidth), region.Name + " right edge");
                Assert.That(region.Position.y - region.Size.y / 2f,
                    Is.GreaterThanOrEqualTo(-halfHeight), region.Name + " bottom edge");
                Assert.That(region.Position.y + region.Size.y / 2f,
                    Is.LessThanOrEqualTo(halfHeight), region.Name + " top edge");
            }

            Assert.That(RectsOverlap(
                ReferenceGalleryNavigator.HouseholdIndustryPosition,
                ReferenceGalleryNavigator.HouseholdIndustrySize,
                ReferenceGalleryNavigator.HouseholdHeadRailPosition,
                ReferenceGalleryNavigator.HouseholdHeadRailSize), Is.False);
            Assert.That(RectsOverlap(
                ReferenceGalleryNavigator.HouseholdIndustryPosition,
                ReferenceGalleryNavigator.HouseholdIndustrySize,
                ReferenceGalleryNavigator.HouseholdMonthEventPosition,
                ReferenceGalleryNavigator.HouseholdMonthEventSize), Is.False);
            Assert.That(RectsOverlap(
                ReferenceGalleryNavigator.PendingRailPosition,
                ReferenceGalleryNavigator.PendingRailSize,
                ReferenceGalleryNavigator.HouseholdModalPosition,
                ReferenceGalleryNavigator.HouseholdModalSize), Is.False);
            Assert.That(RectsOverlap(
                ReferenceGalleryNavigator.EventsModalPosition,
                ReferenceGalleryNavigator.EventsModalSize,
                ReferenceGalleryNavigator.EventsRailPosition,
                ReferenceGalleryNavigator.EventsRailSize), Is.False);
        }

        [Test]
        public void CanvasResize_ReflowsMapAndHeaderWithoutReenteringThePage()
        {
            GameObject host = Create("CanvasHost", typeof(RectTransform));
            var canvas = (RectTransform)host.transform;
            canvas.sizeDelta = new Vector2(1280, 720);
            var navigator = Create("Navigator", typeof(RectTransform)).AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            GameObject map = Create("Map", typeof(RectTransform));
            map.transform.SetParent(host.transform, false);
            GameObject header = Create("M02TopBar", typeof(RectTransform));
            header.transform.SetParent(navigator.transform, false);
            string[] resources = { "功名", "人口", "资金", "物资", "情报" };
            var chips = new List<RectTransform>();
            for (int i = 0; i < resources.Length; i++)
            {
                var chip = Create("M02Chip_" + resources[i], typeof(RectTransform));
                chip.transform.SetParent(header.transform, false);
                var chipRect = (RectTransform)chip.transform;
                chipRect.anchorMin = chipRect.anchorMax = chipRect.pivot = new Vector2(0, 1);
                chipRect.anchoredPosition = new Vector2(662 + i * 116, -8);
                chipRect.sizeDelta = new Vector2(108, 36);
                chips.Add(chipRect);
            }
            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++) buttons[i] = CreateButton("Tab" + i);
            navigator.BindReferences(new[] { new ReferenceGalleryPageRoot(map, ReferenceGalleryPageMask.Map) },
                buttons, Create("Character"), Create("Family"), Create("Household"), map,
                buttons[0], Create("Rail", typeof(RectTransform)), header);
            navigator.ShowPage(ReferenceGalleryPage.Map);
            int returnCount = navigator.MapReturnCount;
            canvas.sizeDelta = new Vector2(1108, 831); // CanvasScaler result for a 960x720 window.
            typeof(ReferenceGalleryNavigator).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(navigator, null);
            var mapRect = (RectTransform)map.transform;
            Assert.That(mapRect.sizeDelta.x, Is.LessThan(950), "the parent resize must reach the map");
            Assert.That(mapRect.anchoredPosition.x + mapRect.sizeDelta.x / 2,
                Is.LessThanOrEqualTo(canvas.rect.xMax));
            Assert.That(map.transform.Find("MapInformationSurface").gameObject.activeSelf, Is.False);
            foreach (var chip in chips)
            {
                Assert.That(chip.anchoredPosition.x + chip.sizeDelta.x, Is.LessThan(1108), chip.name);
                Assert.That(chip.anchoredPosition.y, Is.LessThan(-40), "resources move below the date row: " + chip.name);
            }
            Assert.That(navigator.MapReturnCount, Is.EqualTo(returnCount), "resize is presentation-only");
            canvas.sizeDelta = new Vector2(1280, 720);
            typeof(ReferenceGalleryNavigator).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(navigator, null);
            Assert.That(((RectTransform)header.transform).sizeDelta, Is.EqualTo(new Vector2(1280, 52)));
            Assert.That(chips[4].anchoredPosition, Is.EqualTo(new Vector2(1126, -8)), "wide layout is restored");
            Assert.That(navigator.MapReturnCount, Is.EqualTo(returnCount));
        }

        private Text CreateTextChild(string name, Transform parent)
        {
            GameObject go = Create(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = string.Empty;
            return text;
        }

        private static bool OverlapsLocal(Vector2 firstPosition, Vector2 firstSize,
            Vector2 secondPosition, Vector2 secondSize)
        {
            return RectsOverlap(firstPosition, firstSize, secondPosition, secondSize);
        }

        private static bool RectsOverlap(Vector2 firstPosition, Vector2 firstSize,
            Vector2 secondPosition, Vector2 secondSize)
        {
            Rect first = Rect.MinMaxRect(firstPosition.x - firstSize.x / 2f,
                firstPosition.y - firstSize.y / 2f,
                firstPosition.x + firstSize.x / 2f,
                firstPosition.y + firstSize.y / 2f);
            Rect second = Rect.MinMaxRect(secondPosition.x - secondSize.x / 2f,
                secondPosition.y - secondSize.y / 2f,
                secondPosition.x + secondSize.x / 2f,
                secondPosition.y + secondSize.y / 2f);
            return first.Overlaps(second, true);
        }

        /// <summary>The navigator creates its shared backdrop under the canvas parent, not itself.</summary>
        private static Transform BackdropHost(ReferenceGalleryNavigator navigator) =>
            navigator.transform.parent != null ? navigator.transform.parent : navigator.transform;

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;
                Transform found = FindDeep(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private Button CreateButton(string name)
        {
            GameObject go = Create(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        private GameObject Create(string name, params System.Type[] components)
        {
            var go = components.Length == 0
                ? new GameObject(name)
                : new GameObject(name, components);
            _objects.Add(go);
            return go;
        }
    }
}
